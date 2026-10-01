using System;
using System.Collections.Generic;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;

namespace SilverScreen.Domain.Interaction
{
    /// <summary>A normal strategic work action. Any requester may start it after placing the person.</summary>
    public sealed class PersonPracticeService : IDisposable
    {
        private sealed class Session
        {
            public Employee Employee;
            public string WorkId, SpotId, Genre;
            public long Claim;
        }
        private readonly WorkService _work;
        private readonly ResourceReservationBook _resources;
        private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();
        public PersonPracticeService(WorkService work, ResourceReservationBook resources)
        { _work = work; _resources = resources; _work.Changed += Changed; }
        public bool CanPractice(Employee employee) => employee != null &&
            (employee.Role == EmployeeRole.Actor || employee.Role == EmployeeRole.Extra) &&
            (employee.CurrentState == EmployeeState.Idle || employee.CurrentState == EmployeeState.Walking) &&
            (employee.CurrentIntent.Purpose == EmployeeIntentPurpose.None || employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander) &&
            !_sessions.ContainsKey(employee.Id);
        public bool CanPracticeAt(Employee employee, string spotId) => CanPractice(employee) && !string.IsNullOrWhiteSpace(spotId)
            && !_resources.IsReserved(new ResourceKey("person", employee.Id))
            && !_resources.IsReserved(new ResourceKey("interaction", spotId));
        public bool IsPracticing(Employee employee) => employee != null && _sessions.ContainsKey(employee.Id);
        public bool IsOccupied(string spotId)
        { foreach (var session in _sessions.Values) if (session.SpotId == spotId) return true; return false; }
        public decimal Progress(Employee employee)
        {
            if (employee == null || !_sessions.TryGetValue(employee.Id, out var s)) return 0;
            var work = _work.GetSnapshot(s.WorkId);
            return work.Completed.Units / work.Definition.Required.Units;
        }
        public bool TryStart(Employee employee, string spotId, string genre = "comedy")
        {
            if (!CanPracticeAt(employee, spotId) || string.IsNullOrWhiteSpace(genre)) return false;
            string id = "practice:" + employee.Id + ":" + Guid.NewGuid().ToString("N");
            var personKey = new ResourceKey("person", employee.Id);
            if (!_resources.TryAcquire(id, new[] { personKey, new ResourceKey("interaction", spotId) }, out var claim, out _)) return false;
            var s = new Session { Employee = employee, WorkId = id, SpotId = spotId, Genre = genre, Claim = claim.Id };
            _sessions.Add(employee.Id, s);
            employee.SetState(EmployeeState.Practicing);
            employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.Practice, "Practicing " + genre, spotId));
            employee.OnDetailsChanged += ActivityChanged;
            _work.Create(new WorkDefinition(id, employee.Id, "person.practice", WorkQuantity.FromUnits("practice-minute", 30)));
            _work.SetAssignments(id, new[] { new WorkAssignment(personKey, "practice") });
            _work.RefreshCapacity(id, new WorkRate(WorkQuantity.FromUnits("practice-minute", 1), SimulationDuration.FromMinutes(1)));
            return true;
        }
        public void Cancel(Employee employee)
        {
            if (employee == null || !_sessions.TryGetValue(employee.Id, out var s)) return;
            _sessions.Remove(employee.Id);
            if (!_work.IsDisposed) _work.Cancel(s.WorkId);
            Finish(s, false);
        }
        private void Changed(WorkSnapshot work)
        {
            if (work.Status != WorkStatus.Completed || !_sessions.TryGetValue(work.Definition.OwnerId, out var s) || s.WorkId != work.Definition.Id) return;
            _sessions.Remove(s.Employee.Id); Finish(s, true);
        }
        private void ActivityChanged(Employee employee)
        {
            if (employee.CurrentIntent.Purpose != EmployeeIntentPurpose.Practice) Cancel(employee);
        }
        private void Finish(Session s, bool earned)
        {
            s.Employee.OnDetailsChanged -= ActivityChanged;
            _resources.Release(s.Claim);
            if (earned) s.Employee.Person.Talent.AddGenreExperience(s.Genre, 1);
            if (s.Employee.CurrentIntent.Purpose == EmployeeIntentPurpose.Practice)
            { s.Employee.SetState(EmployeeState.Idle); s.Employee.SetIntent(EmployeeIntent.None); }
        }
        public void Dispose()
        {
            _work.Changed -= Changed;
            foreach (var s in new List<Session>(_sessions.Values)) Cancel(s.Employee);
        }
    }
}
