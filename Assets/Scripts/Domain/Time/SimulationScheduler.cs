using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Time
{
    /// <summary>A queue driven exclusively by SimulationClock. Each occurrence is invoked at most once in this session.</summary>
    public sealed class SimulationScheduler : ISimulationScheduler
    {
        private readonly SimulationClock _clock;
        private readonly SortedSet<SimulationEvent> _queue = new SortedSet<SimulationEvent>(Comparer<SimulationEvent>.Create((a, b) =>
        {
            int due = a.Due.CompareTo(b.Due);
            return due != 0 ? due : a.EnqueueSequence.CompareTo(b.EnqueueSequence);
        }));
        private readonly Dictionary<long, SimulationEvent> _pending = new Dictionary<long, SimulationEvent>();
        private readonly Dictionary<string, Action<SimulationEvent>> _handlers = new Dictionary<string, Action<SimulationEvent>>(StringComparer.Ordinal);
        private readonly HashSet<string> _owners = new HashSet<string>(StringComparer.Ordinal);
        private long _nextId;
        private long _sequence;
        public string Failure { get; private set; }
        public SimulationEvent FailedEvent { get; private set; }
        public int PendingCount => _pending.Count;
        internal SimulationInstant? NextDue => _queue.Count == 0 ? (SimulationInstant?)null : _queue.Min.Due;

        public SimulationScheduler(SimulationClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            clock.AttachScheduler(this);
        }

        public void RegisterOwner(string ownerId)
        {
            _clock.RequireConsistentBoundary();
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID required.", nameof(ownerId));
            _owners.Add(ownerId);
        }

        public void RegisterHandler(string typeKey, Action<SimulationEvent> handler)
        {
            _clock.RequireConsistentBoundary();
            if (string.IsNullOrWhiteSpace(typeKey) || handler == null) throw new ArgumentException("Type and handler required.");
            _handlers.Add(typeKey, handler);
        }

        public void UnregisterHandler(string typeKey)
        {
            _clock.RequireConsistentBoundary();
            if (_pending.Values.Any(e => e.Specification.TypeKey == typeKey)) throw new InvalidOperationException("Cancel owned events before removing a handler.");
            _handlers.Remove(typeKey);
        }

        internal void RemoveHandlerForTeardown(string typeKey)
        {
            foreach (var scheduled in _pending.Values.Where(e => e.Specification.TypeKey == typeKey).ToArray())
            {
                _queue.Remove(scheduled);
                _pending.Remove(scheduled.Id);
            }
            _handlers.Remove(typeKey);
        }

        public long Schedule(ScheduledEventSpec specification)
        {
            _clock.RequireConsistentBoundary();
            Validate(specification);
            long id = checked(++_nextId);
            Insert(new SimulationEvent(id, 1, checked(++_sequence), 0, specification));
            return id;
        }

        public bool Cancel(long eventId)
        {
            _clock.RequireConsistentBoundary();
            if (!_pending.TryGetValue(eventId, out var scheduled)) return false;
            _queue.Remove(scheduled);
            _pending.Remove(eventId);
            return true;
        }

        public bool Reschedule(long eventId, SimulationInstant due)
        {
            _clock.RequireConsistentBoundary();
            if (due < _clock.Now) throw new ArgumentOutOfRangeException(nameof(due));
            if (!_pending.TryGetValue(eventId, out var scheduled)) return false;
            _queue.Remove(scheduled);
            Insert(new SimulationEvent(eventId, checked(scheduled.Revision + 1), checked(++_sequence), scheduled.Occurrence, At(scheduled.Specification, due)));
            return true;
        }

        public IReadOnlyList<SimulationEvent> GetPending(string ownerId)
        {
            _clock.RequireConsistentBoundary();
            return _pending.Values.Where(e => e.Specification.OwnerId == ownerId).OrderBy(e => e.Due).ThenBy(e => e.EnqueueSequence).ToArray();
        }

        internal bool DispatchOne()
        {
            var scheduled = _queue.Min;
            _queue.Remove(scheduled);
            try
            {
                Validate(scheduled.Specification);
                _handlers[scheduled.Specification.TypeKey](scheduled);
                // The handler may cancel/reschedule its own event. Do not overwrite that decision.
                if (_pending.TryGetValue(scheduled.Id, out var current) && ReferenceEquals(current, scheduled))
                {
                    _pending.Remove(scheduled.Id);
                    if (scheduled.Specification.Recurrence.HasValue)
                    {
                        var due = scheduled.Due + scheduled.Specification.Recurrence.Value;
                        Insert(new SimulationEvent(scheduled.Id, scheduled.Revision, checked(++_sequence), checked(scheduled.Occurrence + 1), At(scheduled.Specification, due)));
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                Cancel(scheduled.Id);
                FailedEvent = scheduled;
                Failure = "Event " + scheduled.Id + "/" + scheduled.Occurrence + " (" + scheduled.Specification.OwnerId + ", " + scheduled.Specification.TypeKey + ") failed: " + exception;
                return false;
            }
        }

        private void Validate(ScheduledEventSpec specification)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));
            if (specification.Due < _clock.Now) throw new ArgumentOutOfRangeException(nameof(specification), "Events cannot be scheduled in the past.");
            if (!_owners.Contains(specification.OwnerId)) throw new InvalidOperationException("Unknown event owner: " + specification.OwnerId);
            if (!_handlers.ContainsKey(specification.TypeKey)) throw new InvalidOperationException("Unknown event type: " + specification.TypeKey);
        }

        private void Insert(SimulationEvent scheduled)
        {
            _pending[scheduled.Id] = scheduled;
            _queue.Add(scheduled);
        }

        private static ScheduledEventSpec At(ScheduledEventSpec source, SimulationInstant due)
            => new ScheduledEventSpec(source.OwnerId, source.TypeKey, due, source.Payload, source.PayloadVersion, source.Recurrence, source.Disclosure);
    }
}
