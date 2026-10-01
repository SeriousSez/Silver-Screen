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
        public int ReevaluationMinutes { get; set; } = 20;
        public int RestDurationMinutes { get; set; } = 60;
        public int SocializeDurationMinutes { get; set; } = 45;
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
    }

    public sealed class PersonAutonomyDecision
    {
        public PersonAutonomousActivity Activity { get; }
        public string OpportunityId { get; }
        public string OpportunityName { get; }
        public decimal Score { get; }
        public string Reason { get; }

        internal PersonAutonomyDecision(PersonAutonomousActivity activity, PersonActivityOpportunity opportunity,
            decimal score, string reason)
        {
            Activity = activity;
            OpportunityId = opportunity?.Id;
            OpportunityName = opportunity?.Name;
            Score = score;
            Reason = reason;
        }
    }

    public sealed class PersonAutonomySimulation : IDisposable
    {
        private const string OwnerId = "person-autonomy";
        private const string EvaluateEvent = "person-autonomy.evaluate.v1";
        private const string CompleteEvent = "person-autonomy.complete.v1";

        private enum PlanStage { Travelling, Active }

        private sealed class Participant
        {
            public readonly Employee Employee;
            public long EventId;
            public PlanStage Stage;
            public PersonAutonomyDecision LastDecision;
            public PersonAutonomyDecision CurrentDecision;
            public bool ApplyingActivityState;

            public Participant(Employee employee) => Employee = employee;
        }

        private readonly SimulationClock _clock;
        private readonly SimulationScheduler _scheduler;
        private readonly Dictionary<string, Participant> _participants =
            new Dictionary<string, Participant>(StringComparer.Ordinal);
        private bool _disposed;

        public PersonActivityOpportunityRegistry Opportunities { get; }
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
            if (choice.Activity != PersonAutonomousActivity.IdleWait &&
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
            Guard();
            if (employee == null || !_participants.TryGetValue(employee.Id, out var participant)) return;
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
            ScoreCandidate(person, PersonAutonomousActivity.Socialize, Rates.IdleBaseline,
                person.Wellbeing.Boredom * Rates.SocialBoredomWeight +
                Math.Max(0, Rates.SocialLowMoodThreshold - person.Wellbeing.Mood) * Rates.SocialLowMoodWeight,
                "Boredom or low mood favors socializing.", ref best);
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
                participant.EventId != scheduled.Id) return;
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
                CancelPlan(participant, false);
                CancelScheduled(participant);
                participant.Employee.OnStateChanged -= HandleEmployeeChanged;
                participant.Employee.OnDetailsChanged -= HandleEmployeeChanged;
            }
            _participants.Clear();
            Opportunities.OpportunityUnavailable -= HandleOpportunityUnavailable;
            _scheduler.UnregisterHandler(EvaluateEvent);
            _scheduler.UnregisterHandler(CompleteEvent);
            _disposed = true;
        }
    }
}
