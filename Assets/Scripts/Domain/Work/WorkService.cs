using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Resources;

namespace SilverScreen.Domain.Work
{
    /// <summary>Session work accounting. Reads project earned work without completing it or consuming randomness.</summary>
    public sealed class WorkService : IDisposable
    {
        private const string EventType = "work.evaluate.v1";
        private sealed class State
        {
            public WorkDefinition Definition;
            public decimal Earned;
            public WorkStatus Status;
            public SimulationInstant Evaluated;
            public long Revision = 1;
            public long CapacityVersion;
            public WorkRate Capacity;
            public WorkAssignment[] Assignments = Array.Empty<WorkAssignment>();
            public long? EventId;
        }

        private readonly SimulationClock _clock;
        private readonly SimulationScheduler _scheduler;
        private readonly Dictionary<string, State> _work = new Dictionary<string, State>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _conditionUsers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _conditions = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly HashSet<ResourceKey> _unavailable = new HashSet<ResourceKey>();
        private readonly HashSet<string> _changed = new HashSet<string>(StringComparer.Ordinal);
        private bool _publishing;
        private bool _disposed;
        public bool IsDisposed => _disposed;
        public event Action<WorkSnapshot> Changed;

        public WorkService(SimulationClock clock, SimulationScheduler scheduler)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _scheduler.RegisterHandler(EventType, HandleDue);
            _clock.OnBoundaryCompleted += Publish;
        }

        public void SetResourceAvailable(ResourceKey resource, bool available)
        {
            Guard();
            if (available ? !_unavailable.Contains(resource) : _unavailable.Contains(resource)) return;
            var affected = _work.Values.Where(s => !Terminal(s) && s.Assignments.Any(a => a.Resource.Equals(resource))).ToArray();
            foreach (var state in affected) Settle(state);
            if (available) _unavailable.Remove(resource); else _unavailable.Add(resource);
            foreach (var state in affected) Reconcile(state);
            PublishIfConsistent();
        }

        public void Create(WorkDefinition definition)
        {
            Guard();
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (_work.ContainsKey(definition.Id)) throw new ArgumentException("Duplicate work ID.");
            ValidateDependencies(definition.Id, definition.Dependencies);
            _scheduler.RegisterOwner(definition.OwnerId);
            var state = new State
            {
                Definition = definition, Evaluated = _clock.Now,
                Capacity = new WorkRate(new WorkQuantity(definition.Required.MeasureId, 0), new SimulationDuration(1))
            };
            _work.Add(definition.Id, state);
            foreach (var id in definition.Dependencies) Index(_dependents, id, definition.Id);
            foreach (var id in definition.Conditions) Index(_conditionUsers, id, definition.Id);
            Reconcile(state);
            PublishIfConsistent();
        }

        public WorkSnapshot GetSnapshot(string id)
        {
            Guard();
            var state = Get(id);
            return Snapshot(state);
        }

        public void SetAssignments(string id, IEnumerable<WorkAssignment> assignments)
        {
            Guard();
            var next = (assignments ?? throw new ArgumentNullException(nameof(assignments))).ToArray();
            if (next.Any(a => a == null) || next.Select(a => a.Resource).Distinct().Count() != next.Length) throw new ArgumentException("Assignments must identify distinct valid resources.");
            var state = GetMutable(id);
            Settle(state);
            state.Assignments = next;
            // Effectiveness must be recalculated for the new team, never inherited implicitly.
            state.Capacity = new WorkRate(new WorkQuantity(state.Definition.Required.MeasureId, 0), new SimulationDuration(1));
            state.CapacityVersion++;
            Reconcile(state); PublishIfConsistent();
        }

        public void RefreshCapacity(string id, IWorkCapacityPolicy policy, WorkCapacityContext context)
        {
            Guard();
            var state = Get(id);
            RefreshCapacity(id, (policy ?? throw new ArgumentNullException(nameof(policy))).Evaluate(state.Definition, Array.AsReadOnly(state.Assignments), context));
        }

        public void RefreshCapacity(string id, WorkRate capacity)
        {
            Guard();
            var state = GetMutable(id);
            if (capacity.Quantity.MeasureId != state.Definition.Required.MeasureId || capacity.Per.Seconds <= 0) throw new ArgumentException("Capacity and work measures must match.");
            Settle(state);
            state.Capacity = capacity;
            state.CapacityVersion++;
            Reconcile(state); PublishIfConsistent();
        }

        public void SetDependencies(string id, IEnumerable<string> dependencies)
        {
            Guard();
            var state = GetMutable(id);
            var next = (dependencies ?? throw new ArgumentNullException(nameof(dependencies))).Distinct().ToArray();
            ValidateDependencies(id, next);
            Settle(state);
            foreach (var dep in state.Definition.Dependencies) _dependents[dep].Remove(id);
            var d = state.Definition;
            state.Definition = new WorkDefinition(d.Id, d.OwnerId, d.KindId, d.Required, next, d.Conditions);
            foreach (var dep in next) Index(_dependents, dep, id);
            Reconcile(state); PublishIfConsistent();
        }

        public void SetCondition(string conditionId, bool satisfied)
        {
            Guard();
            if (string.IsNullOrWhiteSpace(conditionId)) throw new ArgumentException("Condition ID required.");
            if (_conditions.TryGetValue(conditionId, out bool previous) && previous == satisfied) return;
            var users = _conditionUsers.TryGetValue(conditionId, out var ids) ? ids.ToArray() : Array.Empty<string>();
            foreach (var id in users) Settle(Get(id));
            _conditions[conditionId] = satisfied;
            foreach (var id in users) Reconcile(Get(id));
            PublishIfConsistent();
        }

        public void Suspend(string id) => SetSuspended(id, true);
        public void Resume(string id) => SetSuspended(id, false);
        private void SetSuspended(string id, bool suspended)
        {
            Guard();
            var state = GetMutable(id);
            Settle(state);
            if (state.Status != WorkStatus.Completed) state.Status = suspended ? WorkStatus.Suspended : WorkStatus.Pending;
            Reconcile(state); PublishIfConsistent();
        }

        public void Cancel(string id)
        {
            Guard();
            var state = Get(id);
            if (Terminal(state)) return;
            Settle(state);
            if (state.Status == WorkStatus.Completed) { PublishIfConsistent(); return; }
            state.Status = WorkStatus.Canceled;
            Invalidate(state); MarkChanged(state);
            RefreshDependents(state.Definition.Id);
            PublishIfConsistent();
        }

        public WorkForecast GetForecast(string id)
        {
            Guard();
            return Forecast(Get(id), new HashSet<string>());
        }

        private WorkForecast Forecast(State state, HashSet<string> visiting)
        {
            if (!visiting.Add(state.Definition.Id)) return new WorkForecast(null, "Dependency cycle", state.Revision);
            try
            {
                if (state.Status == WorkStatus.Completed) return new WorkForecast(state.Evaluated, "Completed", state.Revision);
                if (state.Status == WorkStatus.Canceled || state.Status == WorkStatus.Suspended) return new WorkForecast(null, state.Status.ToString(), state.Revision);
                if (state.Assignments.Any(a => _unavailable.Contains(a.Resource))) return new WorkForecast(null, "Assigned person is away from activity", state.Revision);
                if (state.Capacity.Quantity.MicroUnits == 0) return new WorkForecast(null, "No qualified capacity", state.Revision);
                if (state.Definition.Conditions.Any(c => !_conditions.TryGetValue(c, out bool ready) || !ready)) return new WorkForecast(null, "Readiness condition has no known completion", state.Revision);
                var start = _clock.Now;
                foreach (var dep in state.Definition.Dependencies)
                {
                    var estimate = Forecast(Get(dep), visiting);
                    if (!estimate.EstimatedCompletion.HasValue) return new WorkForecast(null, "Dependency " + dep + ": " + estimate.Reason, state.Revision);
                    if (estimate.EstimatedCompletion.Value > start) start = estimate.EstimatedCompletion.Value;
                }
                long remaining = RemainingSeconds(state, Project(state));
                if (remaining > SimulationInstant.MaxSeconds - start.Seconds) return new WorkForecast(null, "Beyond supported calendar", state.Revision);
                return new WorkForecast(start + new SimulationDuration(remaining), "Conditional on current capacity, assignments and dependencies", state.Revision);
            }
            finally { visiting.Remove(state.Definition.Id); }
        }

        private void HandleDue(SimulationEvent scheduled)
        {
            if (scheduled.Specification.PayloadVersion != 1 || !_work.TryGetValue(scheduled.Specification.Payload, out var state) || state.Definition.OwnerId != scheduled.Specification.OwnerId)
                throw new InvalidOperationException("Unknown work event payload or owner.");
            if (state.EventId != scheduled.Id) return;
            state.EventId = null;
            Settle(state);
            Reconcile(state);
        }

        private decimal Project(State state) => state.Status == WorkStatus.Running
            ? Math.Min(state.Definition.Required.MicroUnits, state.Earned + state.Capacity.EarnedMicroUnits((_clock.Now - state.Evaluated).Seconds))
            : state.Earned;

        private void Settle(State state)
        {
            if (Terminal(state)) return;
            state.Earned = Project(state);
            state.Evaluated = _clock.Now;
            if (state.Earned >= state.Definition.Required.MicroUnits)
            {
                state.Status = WorkStatus.Completed;
                Invalidate(state); MarkChanged(state);
                RefreshDependents(state.Definition.Id);
            }
        }

        private void Reconcile(State state)
        {
            if (Terminal(state)) return;
            Invalidate(state);
            if (!Terminal(state) && state.Status != WorkStatus.Suspended)
            {
                bool ready = !state.Assignments.Any(a => _unavailable.Contains(a.Resource)) && state.Definition.Dependencies.All(d => Get(d).Status == WorkStatus.Completed) &&
                    state.Definition.Conditions.All(c => _conditions.TryGetValue(c, out bool value) && value);
                state.Status = ready && state.Capacity.Quantity.MicroUnits > 0 ? WorkStatus.Running : WorkStatus.Blocked;
                state.Evaluated = _clock.Now;
                if (state.Status == WorkStatus.Running)
                {
                    long seconds = RemainingSeconds(state, state.Earned);
                    state.EventId = _scheduler.Schedule(new ScheduledEventSpec(state.Definition.OwnerId, EventType, _clock.Now + new SimulationDuration(seconds), state.Definition.Id));
                }
            }
            MarkChanged(state);
        }

        private static long RemainingSeconds(State state, decimal earned)
            => checked((long)Math.Ceiling((state.Definition.Required.MicroUnits - earned) * state.Capacity.Per.Seconds / state.Capacity.Quantity.MicroUnits));

        private void Invalidate(State state)
        {
            if (state.EventId.HasValue) _scheduler.Cancel(state.EventId.Value);
            state.EventId = null;
            state.Revision = checked(state.Revision + 1);
        }

        private void RefreshDependents(string id)
        {
            if (!_dependents.TryGetValue(id, out var users)) return;
            foreach (var dependent in users.ToArray())
            {
                var state = Get(dependent);
                Settle(state);
                Reconcile(state);
            }
        }

        private void ValidateDependencies(string id, IEnumerable<string> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency) || dependency == id) throw new ArgumentException("Invalid or cyclic dependency.");
                var todo = new Stack<string>(); var seen = new HashSet<string>(); todo.Push(dependency);
                while (todo.Count > 0)
                {
                    string current = todo.Pop();
                    if (current == id) throw new ArgumentException("Dependency cycle.");
                    if (!seen.Add(current)) continue;
                    foreach (var next in Get(current).Definition.Dependencies) todo.Push(next);
                }
            }
        }

        private static void Index(Dictionary<string, HashSet<string>> index, string key, string user)
        {
            if (!index.TryGetValue(key, out var users)) index[key] = users = new HashSet<string>(StringComparer.Ordinal);
            users.Add(user);
        }

        private State Get(string id) => _work.TryGetValue(id, out var state) ? state : throw new KeyNotFoundException("Unknown work: " + id);
        private State GetMutable(string id)
        {
            var state = Get(id);
            if (Terminal(state)) throw new InvalidOperationException("Terminal work is immutable.");
            return state;
        }
        private static bool Terminal(State state) => state.Status == WorkStatus.Completed || state.Status == WorkStatus.Canceled;
        private WorkSnapshot Snapshot(State state) => new WorkSnapshot(state.Definition, Project(state), state.Status, state.Status == WorkStatus.Completed ? state.Evaluated : _clock.Now, state.Revision, state.CapacityVersion, state.Capacity, (WorkAssignment[])state.Assignments.Clone());
        private void MarkChanged(State state) => _changed.Add(state.Definition.Id);
        private void PublishIfConsistent() { if (_clock.IsBoundaryComplete) Publish(); }
        private void Publish()
        {
            if (_publishing || _disposed) return;
            _publishing = true;
            try
            {
                // Notifications describe settled batches; callbacks cannot complete work themselves.
                while (_changed.Count > 0)
                {
                    var ids = _changed.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                    _changed.Clear();
                    foreach (var id in ids) Changed?.Invoke(Snapshot(Get(id)));
                }
            }
            finally { _publishing = false; }
        }
        private void Guard()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkService));
            _clock.RequireConsistentBoundary();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _clock.OnBoundaryCompleted -= Publish;
            _scheduler.RemoveHandlerForTeardown(EventType);
            Changed = null;
            _disposed = true;
        }
    }
}
