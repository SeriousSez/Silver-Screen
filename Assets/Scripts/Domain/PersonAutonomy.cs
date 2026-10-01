using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain
{
    public enum PersonAutonomousActivity
    {
        IdleWait,
        Rest,
        Socialize,
        Recreation,
        PracticeProfession
    }

    public readonly struct AutonomyPosition
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public AutonomyPosition(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    public sealed class PersonActivityOpportunity
    {
        private readonly HashSet<string> _owners = new HashSet<string>(StringComparer.Ordinal);

        public string Id { get; }
        public string Name { get; }
        public PersonAutonomousActivity Activity { get; }
        public AutonomyPosition Position { get; }
        public int Capacity { get; }
        public int Suitability { get; }
        public bool IsAvailable { get; internal set; } = true;
        public int ReservationCount => _owners.Count;

        public PersonActivityOpportunity(string id, string name, PersonAutonomousActivity activity,
            AutonomyPosition position, int capacity = 1, int suitability = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Opportunity ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Opportunity name is required.", nameof(name));
            if (!Enum.IsDefined(typeof(PersonAutonomousActivity), activity) || activity == PersonAutonomousActivity.IdleWait)
                throw new ArgumentOutOfRangeException(nameof(activity));
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Id = id;
            Name = name;
            Activity = activity;
            Position = position;
            Capacity = capacity;
            Suitability = Math.Clamp(suitability, 0, 100);
        }

        internal bool CanReserve(string personId) =>
            IsAvailable && !_owners.Contains(personId) && _owners.Count < Capacity;

        internal bool TryReserve(string personId) =>
            !string.IsNullOrWhiteSpace(personId) && CanReserve(personId) && _owners.Add(personId);

        internal bool Release(string personId) => _owners.Remove(personId);
        internal bool HasOwner(string personId) => _owners.Contains(personId);
        internal void ClearReservations() => _owners.Clear();
    }

    public sealed class PersonActivityOpportunityRegistry
    {
        private readonly Dictionary<string, PersonActivityOpportunity> _opportunities =
            new Dictionary<string, PersonActivityOpportunity>(StringComparer.Ordinal);

        public event Action<string> OpportunityUnavailable;

        public bool Register(PersonActivityOpportunity opportunity)
        {
            if (opportunity == null) throw new ArgumentNullException(nameof(opportunity));
            if (_opportunities.ContainsKey(opportunity.Id)) return false;
            _opportunities.Add(opportunity.Id, opportunity);
            return true;
        }

        public bool Unregister(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_opportunities.Remove(id, out var opportunity)) return false;
            opportunity.IsAvailable = false;
            OpportunityUnavailable?.Invoke(id);
            return true;
        }

        public bool SetAvailable(string id, bool available)
        {
            if (!_opportunities.TryGetValue(id, out var opportunity)) return false;
            if (opportunity.IsAvailable == available) return true;
            opportunity.IsAvailable = available;
            if (!available)
            {
                opportunity.ClearReservations();
                OpportunityUnavailable?.Invoke(id);
            }
            return true;
        }

        public bool TryReserve(string id, string personId)
        {
            return id != null && _opportunities.TryGetValue(id, out var opportunity) &&
                opportunity.TryReserve(personId);
        }

        public bool IsReservedBy(string id, string personId) =>
            id != null && personId != null && _opportunities.TryGetValue(id, out var opportunity) &&
            opportunity.HasOwner(personId);

        public bool Release(string id, string personId) =>
            id != null && personId != null && _opportunities.TryGetValue(id, out var opportunity) &&
            opportunity.Release(personId);

        public void ReleaseAll(string personId)
        {
            foreach (var opportunity in _opportunities.Values) opportunity.Release(personId);
        }

        internal bool TryFindBest(PersonAutonomousActivity activity, string personId,
            out PersonActivityOpportunity best, out decimal qualityScore)
        {
            best = null;
            qualityScore = decimal.MinValue;
            foreach (var opportunity in _opportunities.Values)
            {
                if (opportunity.Activity != activity || !opportunity.CanReserve(personId)) continue;
                if (opportunity.Suitability > qualityScore ||
                    opportunity.Suitability == qualityScore &&
                    (best == null || string.CompareOrdinal(opportunity.Id, best.Id) < 0))
                {
                    best = opportunity;
                    qualityScore = opportunity.Suitability;
                }
            }
            return best != null;
        }

        public PersonActivityOpportunity Find(string id) =>
            id != null && _opportunities.TryGetValue(id, out var opportunity) ? opportunity : null;

    }

    public sealed class PersonAutonomyRates
    {
        public const int DefaultSocializeMinimumParticipants = 2;
        public const int DefaultSocializeMaximumParticipants = 4;
        public const int DefaultSocializeDurationMinutes = 45;
        public int ReevaluationMinutes { get; set; } = 20;
        public int RestDurationMinutes { get; set; } = 60;
        public int SocializeDurationMinutes { get; set; } = DefaultSocializeDurationMinutes;
        public int SocializeMinimumParticipants { get; set; } = DefaultSocializeMinimumParticipants;
        public int SocializeMaximumParticipants { get; set; } = DefaultSocializeMaximumParticipants;
        public bool SocializeAllowsJoiningAfterStart { get; set; } = true;
        public float SocializeInitiationRadius { get; set; } = 10f;
        public decimal SocializeSessionJoinPreference { get; set; } = 8m;
        public int RecreationDurationMinutes { get; set; } = 60;
        public int PracticeDurationMinutes { get; set; } = 60;
        public decimal IdleBaseline { get; set; } = 20m;
        public int RestEnergyThreshold { get; set; } = 55;
        public int RestStressThreshold { get; set; } = 40;
        public decimal RestEnergyWeight { get; set; } = 1.5m;
        public decimal RestStressWeight { get; set; } = 0.4m;
        public int SocialLowMoodThreshold { get; set; } = 50;
        public decimal SocialBoredomWeight { get; set; } = 0.4m;
        public decimal SocialLowMoodWeight { get; set; } = 0.25m;
        public int RecreationStressThreshold { get; set; } = 40;
        public decimal RecreationBoredomWeight { get; set; } = 0.65m;
        public decimal RecreationStressWeight { get; set; } = 0.35m;
        public int PracticeBoredomThreshold { get; set; } = 30;
        public int PracticeFatigueThreshold { get; set; } = 35;
        public int PracticeStressThreshold { get; set; } = 65;
        public decimal PracticeBoredomWeight { get; set; } = 0.35m;
        public decimal PracticeDriveWeight { get; set; } = 0.35m;
        public decimal PracticeLowEnergyPenalty { get; set; } = 1.5m;
        public decimal PracticeHighStressPenalty { get; set; } = 0.5m;
        public decimal OpportunitySuitabilityWeight { get; set; } = 0.05m;

        internal void Validate()
        {
            if (ReevaluationMinutes < 1 || RestDurationMinutes < 1 || SocializeDurationMinutes < 1 ||
                RecreationDurationMinutes < 1 || PracticeDurationMinutes < 1)
                throw new ArgumentOutOfRangeException(nameof(ReevaluationMinutes), "Autonomy intervals must be positive.");
            if (SocializeMinimumParticipants < 2 ||
                SocializeMaximumParticipants < SocializeMinimumParticipants ||
                SocializeInitiationRadius <= 0 || SocializeSessionJoinPreference < 0)
                throw new ArgumentOutOfRangeException(nameof(SocializeMinimumParticipants),
                    "Socialize session capacity and search settings are invalid.");
            if (IdleBaseline < 0 || RestEnergyWeight < 0 || RestStressWeight < 0 ||
                SocialBoredomWeight < 0 || SocialLowMoodWeight < 0 || RecreationBoredomWeight < 0 ||
                RecreationStressWeight < 0 || PracticeBoredomWeight < 0 || PracticeDriveWeight < 0 ||
                PracticeLowEnergyPenalty < 0 || PracticeHighStressPenalty < 0 ||
                OpportunitySuitabilityWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(IdleBaseline), "Autonomy weights cannot be negative.");
            ValidatePercent(RestEnergyThreshold, nameof(RestEnergyThreshold));
            ValidatePercent(RestStressThreshold, nameof(RestStressThreshold));
            ValidatePercent(SocialLowMoodThreshold, nameof(SocialLowMoodThreshold));
            ValidatePercent(RecreationStressThreshold, nameof(RecreationStressThreshold));
            ValidatePercent(PracticeBoredomThreshold, nameof(PracticeBoredomThreshold));
            ValidatePercent(PracticeFatigueThreshold, nameof(PracticeFatigueThreshold));
            ValidatePercent(PracticeStressThreshold, nameof(PracticeStressThreshold));
        }

        private static void ValidatePercent(int value, string name)
        {
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(name);
        }

        internal int Duration(PersonAutonomousActivity activity) => activity switch
        {
            PersonAutonomousActivity.Rest => RestDurationMinutes,
            PersonAutonomousActivity.Socialize => SocializeDurationMinutes,
            PersonAutonomousActivity.Recreation => RecreationDurationMinutes,
            PersonAutonomousActivity.PracticeProfession => PracticeDurationMinutes,
            _ => 0
        };

        internal PersonActivitySessionRules SocializeRules => new PersonActivitySessionRules(
            PersonAutonomousActivity.Socialize, SocializeMinimumParticipants,
            SocializeMaximumParticipants, SocializeAllowsJoiningAfterStart, SocializeDurationMinutes);
    }

    public sealed class PersonAutonomyDecision
    {
        public PersonAutonomousActivity Activity { get; }
        public string OpportunityId { get; }
        public string OpportunityName { get; }
        public string SessionId { get; }
        public string PartnerId { get; }
        public AutonomyPosition ParticipationPosition { get; }
        public AutonomyPosition SessionCenter { get; }
        public decimal Score { get; }
        public string Reason { get; }

        internal PersonAutonomyDecision(PersonAutonomousActivity activity, PersonActivityOpportunity opportunity,
            decimal score, string reason, string sessionId = null, string partnerId = null,
            AutonomyPosition participationPosition = default, AutonomyPosition sessionCenter = default)
        {
            Activity = activity;
            OpportunityId = sessionId ?? opportunity?.Id;
            OpportunityName = opportunity?.Name;
            SessionId = sessionId;
            PartnerId = partnerId;
            ParticipationPosition = participationPosition;
            SessionCenter = sessionCenter;
            Score = score;
            Reason = reason;
        }
    }

    public sealed class PersonAutonomySimulation : IDisposable
    {
        private const string OwnerId = "person-autonomy";
        private const string EvaluateEvent = "person-autonomy.evaluate.v1";
        private const string CompleteEvent = "person-autonomy.complete.v1";
        private const string CompleteSessionEvent = "person-autonomy.session-complete.v1";

        private enum PlanStage { Travelling, WaitingForGroup, Active }

        private sealed class Participant
        {
            public readonly Employee Employee;
            public long EventId;
            public PlanStage Stage;
            public PersonAutonomyDecision LastDecision;
            public PersonAutonomyDecision CurrentDecision;
            public bool ApplyingActivityState;
            public AutonomyPosition Position;
            public bool HasPosition;

            public Participant(Employee employee) => Employee = employee;
        }

        private readonly SimulationClock _clock;
        private readonly SimulationScheduler _scheduler;
        private readonly Dictionary<string, Participant> _participants =
            new Dictionary<string, Participant>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _sessionEventIds =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private bool _disposed;

        public PersonActivityOpportunityRegistry Opportunities { get; }
        public PersonActivitySessionRegistry Sessions { get; } = new PersonActivitySessionRegistry();
        public PersonAutonomyRates Rates { get; }
        public event Action<Employee, PersonAutonomyDecision> ActivitySelected;
        public event Action<Employee> ActivityCancelled;
        public event Action<Employee, PersonAutonomyDecision> DecisionMade;
        public int ParticipantCount => _participants.Count;

        public PersonAutonomySimulation(SimulationClock clock, SimulationScheduler scheduler,
            PersonActivityOpportunityRegistry opportunities = null, PersonAutonomyRates rates = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Opportunities = opportunities ?? new PersonActivityOpportunityRegistry();
            Rates = rates ?? new PersonAutonomyRates();
            Rates.Validate();
            _scheduler.RegisterOwner(OwnerId);
            _scheduler.RegisterHandler(EvaluateEvent, HandleScheduled);
            _scheduler.RegisterHandler(CompleteEvent, HandleScheduled);
            _scheduler.RegisterHandler(CompleteSessionEvent, HandleScheduled);
            Opportunities.OpportunityUnavailable += HandleOpportunityUnavailable;
        }

        public bool Register(Employee employee)
        {
            Guard();
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            if (!employee.IsEmployed || _participants.ContainsKey(employee.Id)) return false;
            var participant = new Participant(employee);
            _participants.Add(employee.Id, participant);
            employee.OnStateChanged += HandleEmployeeChanged;
            employee.OnDetailsChanged += HandleEmployeeChanged;
            EvaluateNow(employee);
            return true;
        }

        public bool UpdatePosition(Employee employee, AutonomyPosition position)
        {
            if (employee == null || !_participants.TryGetValue(employee.Id, out var participant)) return false;
            participant.Position = position;
            participant.HasPosition = true;
            return true;
        }

        public bool IsEligible(Employee employee) =>
            employee != null && employee.IsEmployed &&
            (employee.CurrentState == EmployeeState.Idle ||
             employee.CurrentState == EmployeeState.Walking &&
             employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander ||
             IsAutonomyState(employee.CurrentState) &&
             employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity) &&
            (employee.CurrentIntent.Purpose == EmployeeIntentPurpose.None ||
             employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander ||
             employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity);

        public PersonAutonomyDecision EvaluateNow(Employee employee)
        {
            Guard();
            if (employee == null || !_participants.TryGetValue(employee.Id, out var participant) ||
                participant.CurrentDecision != null || !IsEligible(employee))
                return null;

            var choice = Choose(employee.Person);
            participant.LastDecision = choice;
            DecisionMade?.Invoke(employee, choice);
            if (choice.Activity == PersonAutonomousActivity.Socialize &&
                TryStartOrJoinSocialSession(participant, choice))
                return participant.LastDecision;

            if (choice.Activity != PersonAutonomousActivity.IdleWait &&
                choice.Activity != PersonAutonomousActivity.Socialize &&
                Opportunities.TryReserve(choice.OpportunityId, employee.Id))
            {
                participant.CurrentDecision = choice;
                participant.Stage = PlanStage.Travelling;
                ActivitySelected?.Invoke(employee, choice);
            }
            else
            {
                participant.LastDecision = new PersonAutonomyDecision(PersonAutonomousActivity.IdleWait,
                    null, Rates.IdleBaseline, "No available opportunity exceeds the idle fallback.");
                Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
            }
            return participant.LastDecision;
        }

        public bool NavigationCompleted(Employee employee, bool succeeded)
        {
            Guard();
            if (!TryGetActive(employee, out var participant) || participant.Stage != PlanStage.Travelling) return false;
            if (participant.CurrentDecision.SessionId != null)
            {
                if (!succeeded ||
                    !OpportunitiesSessionMemberExists(participant.CurrentDecision.SessionId, employee.Id))
                {
                    LeaveSession(employee, true);
                    return false;
                }
                bool becameActive;
                Sessions.MarkArrived(participant.CurrentDecision.SessionId, employee.Id, out becameActive);
                var session = Sessions.Find(participant.CurrentDecision.SessionId);
                if (session == null) return false;
                if (becameActive)
                {
                    ScheduleSessionCompletion(session);
                    foreach (var member in session.Participants)
                        if (member.HasArrived) ActivateSessionParticipant(member.Employee, session);
                }
                else if (session.State == PersonActivitySessionState.Active)
                {
                    ActivateSessionParticipant(employee, session);
                }
                else if (session.State == PersonActivitySessionState.Forming)
                {
                    participant.Stage = PlanStage.WaitingForGroup;
                    participant.ApplyingActivityState = true;
                    try { employee.SetState(EmployeeState.Idle); }
                    finally { participant.ApplyingActivityState = false; }
                }
                return true;
            }
            if (!succeeded || !Opportunities.IsReservedBy(participant.CurrentDecision.OpportunityId, employee.Id))
            {
                CancelPlan(participant, true);
                ActivityCancelled?.Invoke(employee);
                Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
                return false;
            }

            participant.Stage = PlanStage.Active;
            participant.ApplyingActivityState = true;
            try
            {
                employee.SetState(StateFor(participant.CurrentDecision.Activity));
                employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.AutonomousActivity,
                    participant.CurrentDecision.Activity + ": " + participant.CurrentDecision.OpportunityName,
                    participant.CurrentDecision.OpportunityId));
            }
            finally
            {
                participant.ApplyingActivityState = false;
            }
            Schedule(participant, CompleteEvent, Rates.Duration(participant.CurrentDecision.Activity));
            return true;
        }

        public bool Interrupt(Employee employee, bool resetAutonomousActivity = true)
        {
            Guard();
            if (!TryGetActive(employee, out var participant)) return false;
            if (participant.CurrentDecision.SessionId != null)
                return LeaveSession(employee, resetAutonomousActivity);
            bool wasAutonomous = employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity;
            CancelPlan(participant, false);
            ActivityCancelled?.Invoke(employee);
            if (resetAutonomousActivity && wasAutonomous)
            {
                employee.SetState(EmployeeState.Idle);
                employee.SetIntent(EmployeeIntent.None);
            }
            if (IsEligible(employee)) Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
            return true;
        }

        public void Unregister(Employee employee)
        {
            if (_disposed) return;
            if (employee == null || !_participants.TryGetValue(employee.Id, out var participant)) return;
            if (participant.CurrentDecision?.SessionId != null) LeaveSession(employee, true);
            if (participant.CurrentDecision != null) ActivityCancelled?.Invoke(employee);
            CancelPlan(participant, false);
            CancelScheduled(participant);
            employee.OnStateChanged -= HandleEmployeeChanged;
            employee.OnDetailsChanged -= HandleEmployeeChanged;
            _participants.Remove(employee.Id);
        }

        public PersonAutonomyDecision LastDecision(Employee employee) =>
            employee != null && _participants.TryGetValue(employee.Id, out var participant)
                ? participant.LastDecision : null;

        public bool ShouldDeferIdleWander(Employee employee) =>
            employee != null && _participants.ContainsKey(employee.Id) && IsEligible(employee);

        private PersonAutonomyDecision Choose(PersonProfile person)
        {
            PersonAutonomyDecision best = new PersonAutonomyDecision(
                PersonAutonomousActivity.IdleWait, null, Rates.IdleBaseline, "Needs are mild; wait.");
            ScoreCandidate(person, PersonAutonomousActivity.Rest, Rates.IdleBaseline,
                Math.Max(0, Rates.RestEnergyThreshold - person.Wellbeing.Energy) * Rates.RestEnergyWeight +
                Math.Max(0, person.Wellbeing.Stress - Rates.RestStressThreshold) * Rates.RestStressWeight,
                "Low energy or elevated stress favors rest.", ref best);
            var socialTarget = FindSocializeTarget(person.Id);
            if (socialTarget != null)
            {
                decimal socialScore = person.Wellbeing.Boredom * Rates.SocialBoredomWeight +
                    Math.Max(0, Rates.SocialLowMoodThreshold - person.Wellbeing.Mood) * Rates.SocialLowMoodWeight +
                    (socialTarget.SessionId != null ? Rates.SocializeSessionJoinPreference : 0m);
                if (socialScore > Rates.IdleBaseline &&
                    (best.Activity == PersonAutonomousActivity.IdleWait || socialScore > best.Score))
                    best = new PersonAutonomyDecision(PersonAutonomousActivity.Socialize, null,
                        socialScore, "Boredom or low mood favors joining or forming a conversation.",
                        socialTarget.SessionId, socialTarget.PartnerId);
            }
            ScoreCandidate(person, PersonAutonomousActivity.Recreation, Rates.IdleBaseline,
                person.Wellbeing.Boredom * Rates.RecreationBoredomWeight +
                Math.Max(0, person.Wellbeing.Stress - Rates.RecreationStressThreshold) * Rates.RecreationStressWeight,
                "Boredom or elevated stress favors recreation.", ref best);
            ScoreCandidate(person, PersonAutonomousActivity.PracticeProfession, Rates.IdleBaseline,
                Math.Max(0, person.Wellbeing.Boredom - Rates.PracticeBoredomThreshold) * Rates.PracticeBoredomWeight +
                Math.Max(0, person.Career.CareerDrive - 50) * Rates.PracticeDriveWeight -
                Math.Max(0, Rates.PracticeFatigueThreshold - person.Wellbeing.Energy) * Rates.PracticeLowEnergyPenalty -
                Math.Max(0, person.Wellbeing.Stress - Rates.PracticeStressThreshold) * Rates.PracticeHighStressPenalty,
                "Boredom and career drive favor practice; fatigue and severe stress reduce it.", ref best);
            return best;
        }

        private sealed class SocializeTarget
        {
            public string SessionId;
            public string PartnerId;
        }

        private SocializeTarget FindSocializeTarget(string personId)
        {
            if (!_participants.TryGetValue(personId, out var seeker) || !seeker.HasPosition) return null;
            PersonActivitySession nearestSession = null;
            float nearestSessionDistance = float.MaxValue;
            foreach (var session in Sessions.Sessions)
            {
                if (session.Activity != PersonAutonomousActivity.Socialize || !session.CanJoin) continue;
                float distance = Distance(seeker.Position, session.Center);
                if (distance > Rates.SocializeInitiationRadius || distance >= nearestSessionDistance) continue;
                nearestSession = session;
                nearestSessionDistance = distance;
            }
            if (nearestSession != null) return new SocializeTarget { SessionId = nearestSession.Id };

            Participant nearestPartner = null;
            float nearestPartnerDistance = float.MaxValue;
            foreach (var candidate in _participants.Values)
            {
                if (candidate.Employee.Id == personId || candidate.CurrentDecision != null ||
                    !candidate.HasPosition || !IsEligible(candidate.Employee)) continue;
                float distance = Distance(seeker.Position, candidate.Position);
                if (distance > Rates.SocializeInitiationRadius || distance >= nearestPartnerDistance) continue;
                nearestPartner = candidate;
                nearestPartnerDistance = distance;
            }
            return nearestPartner == null ? null : new SocializeTarget { PartnerId = nearestPartner.Employee.Id };
        }

        private bool TryStartOrJoinSocialSession(Participant seeker, PersonAutonomyDecision choice)
        {
            if (!seeker.HasPosition) return false;
            PersonActivitySession session;
            if (choice.SessionId != null)
            {
                var member = Sessions.TryJoin(choice.SessionId, seeker.Employee);
                session = Sessions.Find(choice.SessionId);
                if (member == null || session == null) return false;
                CancelScheduled(seeker);
                seeker.CurrentDecision = SessionDecision(session, member);
                seeker.Stage = PlanStage.Travelling;
                ActivitySelected?.Invoke(seeker.Employee, seeker.CurrentDecision);
                return seeker.CurrentDecision != null;
            }

            if (choice.PartnerId == null ||
                !_participants.TryGetValue(choice.PartnerId, out var partner) ||
                partner.CurrentDecision != null || !partner.HasPosition || !IsEligible(partner.Employee))
                return false;

            var center = Midpoint(seeker.Position, partner.Position);
            session = Sessions.Create(Rates.SocializeRules, center, seeker.Employee, partner.Employee);
            if (session == null) return false;
            CancelScheduled(seeker);
            CancelScheduled(partner);
            seeker.CurrentDecision = SessionDecision(session, session.Participant(seeker.Employee.Id));
            partner.CurrentDecision = SessionDecision(session, session.Participant(partner.Employee.Id));
            seeker.Stage = partner.Stage = PlanStage.Travelling;
            partner.LastDecision = partner.CurrentDecision;
            ActivitySelected?.Invoke(seeker.Employee, seeker.CurrentDecision);
            if (partner.CurrentDecision?.SessionId == session.Id)
                ActivitySelected?.Invoke(partner.Employee, partner.CurrentDecision);
            return true;
        }

        private static PersonAutonomyDecision SessionDecision(PersonActivitySession session,
            PersonActivitySessionParticipant participant, decimal score = 0) =>
            new PersonAutonomyDecision(session.Activity, null, score,
                "Participating in a group activity", session.Id, null, participant.Position, session.Center);

        private bool OpportunitiesSessionMemberExists(string sessionId, string personId) =>
            Sessions.Find(sessionId)?.Participant(personId) != null;

        private void ActivateSessionParticipant(Employee employee, PersonActivitySession session)
        {
            if (!_participants.TryGetValue(employee.Id, out var participant) ||
                participant.CurrentDecision?.SessionId != session.Id) return;
            participant.Stage = PlanStage.Active;
            participant.ApplyingActivityState = true;
            try
            {
                employee.SetState(StateFor(session.Activity));
                employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.AutonomousActivity,
                    session.Activity + ": " + session.Id, session.Id));
            }
            finally
            {
                participant.ApplyingActivityState = false;
            }
        }

        private void ScheduleSessionCompletion(PersonActivitySession session)
        {
            if (_sessionEventIds.ContainsKey(session.Id)) return;
            long eventId = _scheduler.Schedule(new ScheduledEventSpec(OwnerId, CompleteSessionEvent,
                _clock.Now + SimulationDuration.FromMinutes(session.Rules.DurationMinutes), session.Id));
            _sessionEventIds.Add(session.Id, eventId);
        }

        private bool LeaveSession(Employee employee, bool resetDepartingParticipant)
        {
            if (employee == null || !_participants.TryGetValue(employee.Id, out var departing) ||
                departing.CurrentDecision?.SessionId == null) return false;
            string sessionId = departing.CurrentDecision.SessionId;
            var session = Sessions.Find(sessionId);
            bool wasAutonomous = employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                employee.CurrentIntent.TargetBuildingId == sessionId &&
                (employee.CurrentState == EmployeeState.Walking ||
                 employee.CurrentState == EmployeeState.Idle ||
                 session != null && employee.CurrentState == StateFor(session.Activity));
            ActivityCancelled?.Invoke(employee);
            departing.CurrentDecision = null;
            departing.Stage = PlanStage.Travelling;
            var changedSession = Sessions.RemoveParticipant(employee.Id);
            if (resetDepartingParticipant && wasAutonomous)
            {
                if (employee.CurrentState == EmployeeState.Walking ||
                    employee.CurrentState == EmployeeState.Idle ||
                    employee.CurrentState == StateFor(session?.Activity ?? PersonAutonomousActivity.Socialize))
                {
                    employee.SetState(EmployeeState.Idle);
                    employee.SetIntent(EmployeeIntent.None);
                }
            }
            if (changedSession == null || changedSession.State == PersonActivitySessionState.Cancelled)
            {
                if (session != null) CancelSessionCompletion(sessionId);
                if (session != null)
                {
                    foreach (var member in session.Participants)
                        ReleaseSessionParticipant(member.Employee, sessionId,
                            changedSession?.Activity ?? session.Activity);
                }
            }
            ScheduleIfEligible(departing);
            return true;
        }

        private void ReleaseSessionParticipant(Employee employee, string sessionId,
            PersonAutonomousActivity activity)
        {
            if (!_participants.TryGetValue(employee.Id, out var participant) ||
                participant.CurrentDecision?.SessionId != sessionId) return;
            ActivityCancelled?.Invoke(employee);
            participant.CurrentDecision = null;
            participant.Stage = PlanStage.Travelling;
            if (employee.IsEmployed && employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                employee.CurrentIntent.TargetBuildingId == sessionId &&
                (employee.CurrentState == EmployeeState.Walking ||
                 employee.CurrentState == EmployeeState.Idle ||
                 employee.CurrentState == StateFor(activity)))
            {
                employee.SetState(EmployeeState.Idle);
                employee.SetIntent(EmployeeIntent.None);
            }
            ScheduleIfEligible(participant);
        }

        private void CompleteSession(string sessionId)
        {
            var session = Sessions.Complete(sessionId);
            CancelSessionCompletion(sessionId);
            if (session == null) return;
            foreach (var member in session.Participants)
                ReleaseSessionParticipant(member.Employee, sessionId, session.Activity);
        }

        private void CancelSessionCompletion(string sessionId)
        {
            if (!_sessionEventIds.TryGetValue(sessionId, out var eventId)) return;
            _scheduler.Cancel(eventId);
            _sessionEventIds.Remove(sessionId);
        }

        private void ScheduleIfEligible(Participant participant)
        {
            if (participant.Employee.IsEmployed && IsEligible(participant.Employee))
                Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
        }

        private static float Distance(AutonomyPosition a, AutonomyPosition b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            float z = a.Z - b.Z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        private static AutonomyPosition Midpoint(AutonomyPosition a, AutonomyPosition b) =>
            new AutonomyPosition((a.X + b.X) / 2f, (a.Y + b.Y) / 2f, (a.Z + b.Z) / 2f);

        private void ScoreCandidate(PersonProfile person, PersonAutonomousActivity activity,
            decimal baseline, decimal desire, string reason, ref PersonAutonomyDecision best)
        {
            if (!Opportunities.TryFindBest(activity, person.Id, out var opportunity, out var suitability)) return;
            decimal score = desire + suitability * Rates.OpportunitySuitabilityWeight;
            if (score <= baseline) return;
            if (best.Activity != PersonAutonomousActivity.IdleWait && score <= best.Score) return;
            best = new PersonAutonomyDecision(activity, opportunity, score, reason);
        }

        private void HandleScheduled(SimulationEvent scheduled)
        {
            if (!_participants.TryGetValue(scheduled.Specification.Payload, out var participant) ||
                scheduled.Specification.TypeKey != CompleteSessionEvent && participant.EventId != scheduled.Id)
            {
                if (scheduled.Specification.TypeKey != CompleteSessionEvent ||
                    !_sessionEventIds.TryGetValue(scheduled.Specification.Payload, out var sessionEventId) ||
                    sessionEventId != scheduled.Id) return;
            }
            if (scheduled.Specification.TypeKey == CompleteSessionEvent)
            {
                CompleteSession(scheduled.Specification.Payload);
                return;
            }
            participant.EventId = 0;
            if (!participant.Employee.IsEmployed) { Unregister(participant.Employee); return; }
            if (scheduled.Specification.TypeKey == EvaluateEvent)
            {
                if (IsEligible(participant.Employee)) EvaluateNow(participant.Employee);
                else Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
            }
            else if (participant.CurrentDecision != null && participant.Stage == PlanStage.Active)
            {
                CancelPlan(participant, false);
                if (participant.Employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity)
                {
                    participant.Employee.SetState(EmployeeState.Idle);
                    participant.Employee.SetIntent(EmployeeIntent.None);
                }
                Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
            }
        }

        private void HandleEmployeeChanged(Employee employee)
        {
            if (employee == null || !_participants.TryGetValue(employee.Id, out var participant)) return;
            if (participant.ApplyingActivityState) return;
            if (participant.CurrentDecision == null)
            {
                if (IsEligible(employee)) Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
                return;
            }
            if (participant.CurrentDecision.SessionId != null)
            {
                var session = Sessions.Find(participant.CurrentDecision.SessionId);
                var member = session?.Participant(employee.Id);
                bool expectedSessionState = session != null && member != null &&
                    (participant.Stage == PlanStage.Travelling
                        ? employee.CurrentState == EmployeeState.Walking &&
                          (employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                           employee.CurrentIntent.TargetBuildingId == session.Id ||
                           employee.CurrentIntent.Purpose == EmployeeIntentPurpose.None ||
                           employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander)
                        : participant.Stage == PlanStage.WaitingForGroup
                            ? session.State == PersonActivitySessionState.Forming &&
                              member.HasArrived && employee.CurrentState == EmployeeState.Idle &&
                              employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                              employee.CurrentIntent.TargetBuildingId == session.Id
                        : session.State == PersonActivitySessionState.Active &&
                          employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                          employee.CurrentIntent.TargetBuildingId == session.Id &&
                          employee.CurrentState == StateFor(session.Activity));
                if (expectedSessionState) return;
                LeaveSession(employee, true);
                return;
            }
            bool expected = employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity &&
                (participant.Stage == PlanStage.Travelling
                    ? employee.CurrentState == EmployeeState.Walking
                    : employee.CurrentState == StateFor(participant.CurrentDecision.Activity));
            if (participant.Stage == PlanStage.Travelling &&
                employee.CurrentState == EmployeeState.Walking &&
                (employee.CurrentIntent.Purpose == EmployeeIntentPurpose.None ||
                 employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander))
                expected = true;
            if (expected) return;
            CancelPlan(participant, false);
            ActivityCancelled?.Invoke(employee);
            if (IsEligible(employee)) Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
        }

        private void HandleOpportunityUnavailable(string opportunityId)
        {
            foreach (var participant in _participants.Values)
            {
                if (participant.CurrentDecision?.SessionId != null) continue;
                if (participant.CurrentDecision?.OpportunityId != opportunityId) continue;
                CancelPlan(participant, false);
                ActivityCancelled?.Invoke(participant.Employee);
                if (participant.Employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity)
                {
                    participant.Employee.SetState(EmployeeState.Idle);
                    participant.Employee.SetIntent(EmployeeIntent.None);
                }
                if (IsEligible(participant.Employee))
                    Schedule(participant, EvaluateEvent, Rates.ReevaluationMinutes);
            }
        }

        private void CancelPlan(Participant participant, bool clearEmployeeIntent)
        {
            if (participant.CurrentDecision == null) return;
            CancelScheduled(participant);
            Opportunities.Release(participant.CurrentDecision.OpportunityId, participant.Employee.Id);
            participant.CurrentDecision = null;
            if (clearEmployeeIntent && participant.Employee.CurrentIntent.Purpose == EmployeeIntentPurpose.AutonomousActivity)
            {
                participant.Employee.SetState(EmployeeState.Idle);
                participant.Employee.SetIntent(EmployeeIntent.None);
            }
        }

        private void Schedule(Participant participant, string type, int minutes)
        {
            CancelScheduled(participant);
            var due = _clock.Now + SimulationDuration.FromMinutes(minutes);
            participant.EventId = _scheduler.Schedule(new ScheduledEventSpec(OwnerId, type, due, participant.Employee.Id));
        }

        private void CancelScheduled(Participant participant)
        {
            if (participant.EventId == 0) return;
            _scheduler.Cancel(participant.EventId);
            participant.EventId = 0;
        }

        private bool TryGetActive(Employee employee, out Participant participant)
        {
            if (employee == null)
            {
                participant = null;
                return false;
            }
            return _participants.TryGetValue(employee.Id, out participant) &&
                participant.CurrentDecision != null;
        }

        private static bool IsAutonomyState(EmployeeState state) =>
            state == EmployeeState.Resting || state == EmployeeState.Socializing ||
            state == EmployeeState.Recreating || state == EmployeeState.Practicing;

        private static EmployeeState StateFor(PersonAutonomousActivity activity) => activity switch
        {
            PersonAutonomousActivity.Rest => EmployeeState.Resting,
            PersonAutonomousActivity.Socialize => EmployeeState.Socializing,
            PersonAutonomousActivity.Recreation => EmployeeState.Recreating,
            PersonAutonomousActivity.PracticeProfession => EmployeeState.Practicing,
            _ => EmployeeState.Idle
        };

        private void Guard()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PersonAutonomySimulation));
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var participant in _participants.Values)
            {
                if (participant.CurrentDecision?.SessionId != null)
                    LeaveSession(participant.Employee, true);
                CancelPlan(participant, false);
                CancelScheduled(participant);
                participant.Employee.OnStateChanged -= HandleEmployeeChanged;
                participant.Employee.OnDetailsChanged -= HandleEmployeeChanged;
            }
            _participants.Clear();
            Opportunities.OpportunityUnavailable -= HandleOpportunityUnavailable;
            _scheduler.UnregisterHandler(EvaluateEvent);
            _scheduler.UnregisterHandler(CompleteEvent);
            _scheduler.UnregisterHandler(CompleteSessionEvent);
            _disposed = true;
        }
    }
}
