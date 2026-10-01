using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public enum PersonActivitySessionState
    {
        Forming,
        Active,
        Completed,
        Cancelled
    }

    public sealed class PersonActivitySessionRules
    {
        public PersonAutonomousActivity Activity { get; }
        public int MinimumParticipants { get; }
        public int MaximumParticipants { get; }
        public bool AllowJoiningAfterStart { get; }
        public int DurationMinutes { get; }

        public PersonActivitySessionRules(PersonAutonomousActivity activity, int minimumParticipants,
            int maximumParticipants, bool allowJoiningAfterStart, int durationMinutes)
        {
            if (!Enum.IsDefined(typeof(PersonAutonomousActivity), activity) ||
                activity == PersonAutonomousActivity.IdleWait)
                throw new ArgumentOutOfRangeException(nameof(activity));
            if (minimumParticipants < 1 || maximumParticipants < minimumParticipants)
                throw new ArgumentOutOfRangeException(nameof(minimumParticipants));
            if (durationMinutes < 1) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
            Activity = activity;
            MinimumParticipants = minimumParticipants;
            MaximumParticipants = maximumParticipants;
            AllowJoiningAfterStart = allowJoiningAfterStart;
            DurationMinutes = durationMinutes;
        }
    }

    public sealed class PersonActivitySessionParticipant
    {
        public Employee Employee { get; }
        public AutonomyPosition Position { get; internal set; }
        public int PositionIndex { get; }
        public bool HasArrived { get; internal set; }

        internal PersonActivitySessionParticipant(Employee employee, AutonomyPosition position, int positionIndex)
        {
            Employee = employee ?? throw new ArgumentNullException(nameof(employee));
            Position = position;
            PositionIndex = positionIndex;
        }
    }

    public sealed class PersonActivitySession
    {
        private readonly List<PersonActivitySessionParticipant> _participants =
            new List<PersonActivitySessionParticipant>();
        private readonly HashSet<int> _usedPositions = new HashSet<int>();

        public string Id { get; }
        public PersonAutonomousActivity Activity => Rules.Activity;
        public PersonActivitySessionRules Rules { get; }
        public PersonActivitySessionState State { get; internal set; } = PersonActivitySessionState.Forming;
        public AutonomyPosition Center { get; }
        public IReadOnlyList<PersonActivitySessionParticipant> Participants => _participants;
        public int ParticipantCount => _participants.Count;
        public int ArrivedCount
        {
            get
            {
                int count = 0;
                foreach (var participant in _participants)
                    if (participant.HasArrived) count++;
                return count;
            }
        }
        public bool HasCapacity => ParticipantCount < Rules.MaximumParticipants;

        internal PersonActivitySession(string id, PersonActivitySessionRules rules, AutonomyPosition center)
        {
            Id = id;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Center = center;
        }

        public PersonActivitySessionParticipant Participant(string personId)
        {
            foreach (var participant in _participants)
                if (participant.Employee.Id == personId) return participant;
            return null;
        }

        internal PersonActivitySessionParticipant Add(Employee employee)
        {
            if (!CanJoin || employee == null || Participant(employee.Id) != null)
                return null;
            int index = NextPositionIndex();
            if (index < 0) return null;
            _usedPositions.Add(index);
            var participant = new PersonActivitySessionParticipant(employee, PositionAt(index), index);
            _participants.Add(participant);
            return participant;
        }

        public bool CanJoin =>
            HasCapacity &&
            (State == PersonActivitySessionState.Forming ||
             State == PersonActivitySessionState.Active && Rules.AllowJoiningAfterStart);

        internal bool MarkArrived(string personId)
        {
            var participant = Participant(personId);
            if (participant == null) return false;
            participant.HasArrived = true;
            if (State == PersonActivitySessionState.Forming && ArrivedCount >= Rules.MinimumParticipants)
            {
                State = PersonActivitySessionState.Active;
                return true;
            }
            return false;
        }

        internal void Remove(string personId)
        {
            var participant = Participant(personId);
            if (participant == null) return;
            _usedPositions.Remove(participant.PositionIndex);
            _participants.Remove(participant);
            if (State == PersonActivitySessionState.Forming &&
                _participants.Count < Rules.MinimumParticipants)
                State = PersonActivitySessionState.Cancelled;
            else if (State == PersonActivitySessionState.Active &&
                     _participants.Count < Rules.MinimumParticipants)
                State = PersonActivitySessionState.Cancelled;
        }

        private int NextPositionIndex()
        {
            int bestIndex = -1;
            float bestMinimumDistance = float.MinValue;
            for (int index = 0; index < Rules.MaximumParticipants; index++)
            {
                if (_usedPositions.Contains(index)) continue;
                float minimumDistance = float.MaxValue;
                foreach (int used in _usedPositions)
                    minimumDistance = Math.Min(minimumDistance, CircularIndexDistance(index, used));
                if (_usedPositions.Count == 0) minimumDistance = float.MaxValue;
                if (minimumDistance > bestMinimumDistance)
                {
                    bestMinimumDistance = minimumDistance;
                    bestIndex = index;
                }
            }
            if (_usedPositions.Count == 1 && Rules.MaximumParticipants > 1)
            {
                var first = _usedPositions.GetEnumerator();
                first.MoveNext();
                return (first.Current + Rules.MaximumParticipants / 2) % Rules.MaximumParticipants;
            }
            return bestIndex;
        }

        private AutonomyPosition PositionAt(int index)
        {
            float radius = Math.Max(.85f, Rules.MaximumParticipants * .2f);
            double angle = 2d * Math.PI * index / Rules.MaximumParticipants;
            return new AutonomyPosition(Center.X + (float)Math.Cos(angle) * radius, Center.Y,
                Center.Z + (float)Math.Sin(angle) * radius);
        }

        private float CircularIndexDistance(int first, int second)
        {
            int difference = Math.Abs(first - second);
            return Math.Min(difference, Rules.MaximumParticipants - difference);
        }
    }

    public sealed class PersonActivitySessionRegistry
    {
        private readonly Dictionary<string, PersonActivitySession> _sessions =
            new Dictionary<string, PersonActivitySession>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _sessionByPerson =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int _nextId;

        public int Count => _sessions.Count;
        public IEnumerable<PersonActivitySession> Sessions => _sessions.Values;

        public PersonActivitySession Create(PersonActivitySessionRules rules, AutonomyPosition center,
            Employee first, Employee second)
        {
            if (first == null || second == null) return null;
            return Create(rules, center, new[] { first, second });
        }

        public PersonActivitySession Create(PersonActivitySessionRules rules, AutonomyPosition center,
            IEnumerable<Employee> participants)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            var initial = new List<Employee>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var employee in participants)
            {
                if (employee == null || !employee.IsEmployed || !ids.Add(employee.Id) ||
                    IsReserved(employee.Id)) return null;
                initial.Add(employee);
            }
            if (initial.Count < rules.MinimumParticipants || initial.Count > rules.MaximumParticipants)
                return null;

            string id = "activity-session:" + rules.Activity.ToString().ToLowerInvariant() + ":" +
                (++_nextId).ToString("D6");
            var session = new PersonActivitySession(id, rules, center);
            _sessions.Add(id, session);
            foreach (var employee in initial) Add(session, employee);
            return session;
        }

        public PersonActivitySessionParticipant TryJoin(string sessionId, Employee employee)
        {
            if (employee == null || !employee.IsEmployed || IsReserved(employee.Id) ||
                !_sessions.TryGetValue(sessionId, out var session) || !session.CanJoin)
                return null;
            return Add(session, employee);
        }

        public bool MarkArrived(string sessionId, string personId, out bool becameActive)
        {
            becameActive = false;
            if (!_sessions.TryGetValue(sessionId, out var session) || session.Participant(personId) == null)
                return false;
            becameActive = session.MarkArrived(personId);
            return true;
        }

        public PersonActivitySession RemoveParticipant(string personId)
        {
            if (string.IsNullOrWhiteSpace(personId) ||
                !_sessionByPerson.TryGetValue(personId, out var sessionId) ||
                !_sessions.TryGetValue(sessionId, out var session))
                return null;
            session.Remove(personId);
            _sessionByPerson.Remove(personId);
            if (session.State == PersonActivitySessionState.Cancelled)
            {
                foreach (var participant in session.Participants)
                    _sessionByPerson.Remove(participant.Employee.Id);
                _sessions.Remove(sessionId);
            }
            return session;
        }

        public PersonActivitySession Complete(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session)) return null;
            session.State = PersonActivitySessionState.Completed;
            foreach (var participant in session.Participants)
                _sessionByPerson.Remove(participant.Employee.Id);
            _sessions.Remove(sessionId);
            return session;
        }

        public PersonActivitySession Find(string sessionId) =>
            sessionId != null && _sessions.TryGetValue(sessionId, out var session) ? session : null;

        public PersonActivitySession FindForPerson(string personId) =>
            personId != null && _sessionByPerson.TryGetValue(personId, out var id) ? Find(id) : null;

        public bool IsReserved(string personId) =>
            personId != null && _sessionByPerson.ContainsKey(personId);

        private PersonActivitySessionParticipant Add(PersonActivitySession session, Employee employee)
        {
            var participant = session.Add(employee);
            if (participant != null) _sessionByPerson.Add(employee.Id, session.Id);
            return participant;
        }
    }
}
