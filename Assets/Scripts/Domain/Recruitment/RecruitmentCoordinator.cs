using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Recruitment
{
    public enum CandidateRouteResult
    {
        Started,
        CandidateMissing,
        RecruitmentLocationMissing,
        CapacityFull,
        AgentMissing,
        NavigationRejected
    }

    public interface ICandidateWorldRouter
    {
        CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate, Action<CandidateRouteResult, int> onComplete);
        CandidateRouteResult RouteToExit(Candidate candidate, Action<CandidateRouteResult, int> onComplete);
        event Action OnRecruitmentLocationsChanged;
        bool IsRecruitmentLocationOperational(RecruitmentCategory category);
        int GetWaitingCapacity(RecruitmentCategory category);
        void ReleaseCandidate(Candidate candidate);
    }

    [Serializable]
    public sealed class RecruitmentConfiguration
    {
        public int Capacity { get; }
        public int ArrivalCadenceMinutes { get; }
        public int MaximumWaitingMinutes { get; }
        public ProfessionalRole? FirstPrototypeCandidateRole { get; }

        public RecruitmentConfiguration(int capacity = 5, int arrivalCadenceMinutes = 360,
            int maximumWaitingMinutes = 2880, ProfessionalRole? firstPrototypeCandidateRole = null)
        {
            Capacity = Math.Max(1, capacity);
            ArrivalCadenceMinutes = Math.Max(1, arrivalCadenceMinutes);
            MaximumWaitingMinutes = Math.Max(60, maximumWaitingMinutes);
            FirstPrototypeCandidateRole = firstPrototypeCandidateRole;
        }
    }

    [Serializable]
    public sealed class RecruitmentCategoryConfiguration
    {
        public RecruitmentCategory Category { get; }
        public int StarterApplicantCount { get; }
        public int FirstStarterDelayMinutes { get; }
        public int StarterSpacingMinutes { get; }
        public int ArrivalCadenceMinutes { get; }
        public int RetryDelayMinutes { get; }
        public int MaximumWaitingMinutes { get; }

        public RecruitmentCategoryConfiguration(RecruitmentCategory category, int starterApplicantCount,
            int firstStarterDelayMinutes, int starterSpacingMinutes, int arrivalCadenceMinutes,
            int retryDelayMinutes = 30, int maximumWaitingMinutes = 2880)
        {
            Category = category;
            StarterApplicantCount = Math.Max(0, starterApplicantCount);
            FirstStarterDelayMinutes = Math.Max(1, firstStarterDelayMinutes);
            StarterSpacingMinutes = Math.Max(1, starterSpacingMinutes);
            ArrivalCadenceMinutes = Math.Max(1, arrivalCadenceMinutes);
            RetryDelayMinutes = Math.Max(1, retryDelayMinutes);
            MaximumWaitingMinutes = Math.Max(60, maximumWaitingMinutes);
        }

        public static RecruitmentCategoryConfiguration[] CreateStarterIntakeDefaults()
        {
            return new[]
            {
                new RecruitmentCategoryConfiguration(RecruitmentCategory.StudioServices, 6, 1, 8, 180),
                new RecruitmentCategoryConfiguration(RecruitmentCategory.Talent, 4, 1, 8, 180),
                new RecruitmentCategoryConfiguration(RecruitmentCategory.Crew, 4, 1, 8, 180),
                new RecruitmentCategoryConfiguration(RecruitmentCategory.Writing, 3, 1, 8, 180)
            };
        }
    }

    [Serializable]
    public sealed class RecruitmentIntakeState
    {
        public RecruitmentCategory Category { get; private set; }
        public bool StarterGrantTriggered { get; private set; }
        public int StarterApplicantsRemaining { get; private set; }
        public int StarterApplicantsInFlight { get; private set; }
        public int MinutesUntilNextStarter { get; private set; }
        public int StarterRetryMinutes { get; private set; }
        public int MinutesUntilNextNormalArrival { get; private set; }

        internal RecruitmentIntakeState(RecruitmentCategory category, int starterCount)
        {
            Category = category;
            StarterGrantTriggered = starterCount == 0;
            StarterApplicantsRemaining = 0;
            MinutesUntilNextNormalArrival = starterCount == 0 ? 1 : 0;
        }

        internal void Restore(RecruitmentIntakeStateSnapshot snapshot)
        {
            Category = snapshot.Category;
            StarterGrantTriggered = snapshot.StarterGrantTriggered;
            StarterApplicantsRemaining = Math.Max(0, snapshot.StarterApplicantsRemaining);
            StarterApplicantsInFlight = 0;
            MinutesUntilNextStarter = Math.Max(0, snapshot.MinutesUntilNextStarter);
            StarterRetryMinutes = Math.Max(0, snapshot.StarterRetryMinutes);
            MinutesUntilNextNormalArrival = Math.Max(0, snapshot.MinutesUntilNextNormalArrival);
        }

        internal RecruitmentIntakeStateSnapshot Capture() => new RecruitmentIntakeStateSnapshot
        {
            Category = Category,
            StarterGrantTriggered = StarterGrantTriggered,
            StarterApplicantsRemaining = StarterApplicantsRemaining,
            MinutesUntilNextStarter = MinutesUntilNextStarter,
            StarterRetryMinutes = StarterRetryMinutes,
            MinutesUntilNextNormalArrival = MinutesUntilNextNormalArrival
        };

        internal void TriggerStarterGrant(int count, int firstDelay)
        {
            StarterGrantTriggered = true;
            StarterApplicantsRemaining = count;
            MinutesUntilNextStarter = count > 0 ? firstDelay : 0;
            MinutesUntilNextNormalArrival = count > 0 ? 0 : MinutesUntilNextNormalArrival;
        }

        internal void AdvanceStarterTimer()
        {
            if (MinutesUntilNextStarter > 0) MinutesUntilNextStarter--;
        }

        internal void AdvanceRetryTimer()
        {
            if (StarterRetryMinutes > 0) StarterRetryMinutes--;
        }

        internal void ScheduleStarter(int spacing) => MinutesUntilNextStarter = spacing;
        internal void RetryStarter(int delay) { StarterRetryMinutes = delay; MinutesUntilNextStarter = 0; }
        internal void StarterDispatched() => StarterApplicantsInFlight++;
        internal void StarterJourneyFailed()
        {
            if (StarterApplicantsInFlight > 0) StarterApplicantsInFlight--;
        }
        internal void StarterDelivered(int cadence)
        {
            if (StarterApplicantsInFlight > 0) StarterApplicantsInFlight--;
            if (StarterApplicantsRemaining > 0) StarterApplicantsRemaining--;
            if (StarterApplicantsRemaining == 0) MinutesUntilNextNormalArrival = cadence;
        }

        internal void AdvanceNormalTimer()
        {
            if (MinutesUntilNextNormalArrival > 0) MinutesUntilNextNormalArrival--;
        }

        internal void ScheduleNormal(int cadence) => MinutesUntilNextNormalArrival = cadence;
        internal void RetryNormal(int delay) => MinutesUntilNextNormalArrival = delay;
    }

    [Serializable]
    public sealed class RecruitmentCoordinatorSnapshot
    {
        public List<RecruitmentIntakeStateSnapshot> Categories = new List<RecruitmentIntakeStateSnapshot>();
    }

    [Serializable]
    public sealed class RecruitmentIntakeStateSnapshot
    {
        public RecruitmentCategory Category;
        public bool StarterGrantTriggered;
        public int StarterApplicantsRemaining;
        public int MinutesUntilNextStarter;
        public int StarterRetryMinutes;
        public int MinutesUntilNextNormalArrival;
    }

    public sealed class RecruitmentCoordinator : IDisposable
    {
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private readonly Dictionary<RecruitmentCategory, RecruitmentCategoryConfiguration> _categoryConfigurations =
            new Dictionary<RecruitmentCategory, RecruitmentCategoryConfiguration>();
        private readonly Dictionary<RecruitmentCategory, RecruitmentIntakeState> _intakeStates =
            new Dictionary<RecruitmentCategory, RecruitmentIntakeState>();
        private readonly ISimulationTimeService _time;
        private readonly CandidateGenerator _generator;
        private readonly ICandidateWorldRouter _router;
        private readonly RecruitmentConfiguration _legacyConfiguration;
        private int _minutesUntilLegacyArrival;
        private bool _generatedPrototypeCandidate;

        public RecruitmentConfiguration Configuration => _legacyConfiguration;
        public IReadOnlyList<Candidate> Candidates => _candidates;
        public IReadOnlyDictionary<RecruitmentCategory, RecruitmentIntakeState> IntakeStates => _intakeStates;
        public bool HasCapacity => _legacyConfiguration != null &&
                                   _candidates.Count < _legacyConfiguration.Capacity;

        public event Action<Candidate> OnCandidateAdded;
        public event Action<Candidate> OnCandidateChanged;
        public event Action<Candidate> OnCandidateRemoved;
        public event Action<Candidate, Employee> OnCandidateHired;
        public event Action<Candidate, CandidateRouteResult> OnRoutingFailed;

        public RecruitmentCoordinator(ISimulationTimeService time, CandidateGenerator generator,
            ICandidateWorldRouter router, RecruitmentConfiguration configuration = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _legacyConfiguration = configuration ?? new RecruitmentConfiguration();
            _minutesUntilLegacyArrival = 1;
            _time.OnMinutePassed += HandleMinutePassed;
        }

        public RecruitmentCoordinator(ISimulationTimeService time, CandidateGenerator generator,
            ICandidateWorldRouter router, IEnumerable<RecruitmentCategoryConfiguration> categoryConfigurations,
            RecruitmentCoordinatorSnapshot snapshot = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _router = router ?? throw new ArgumentNullException(nameof(router));

            if (categoryConfigurations == null) throw new ArgumentNullException(nameof(categoryConfigurations));
            foreach (var configuration in categoryConfigurations)
            {
                if (configuration == null) throw new ArgumentException("Recruitment category configuration cannot be null.", nameof(categoryConfigurations));
                if (_categoryConfigurations.ContainsKey(configuration.Category))
                    throw new ArgumentException($"Duplicate recruitment category configuration: {configuration.Category}.", nameof(categoryConfigurations));

                _categoryConfigurations.Add(configuration.Category, configuration);
                _intakeStates.Add(configuration.Category,
                    new RecruitmentIntakeState(configuration.Category, configuration.StarterApplicantCount));
            }

            if (snapshot != null) RestoreSnapshot(snapshot);
            InitializeOperationalIntakes();
            _router.OnRecruitmentLocationsChanged += HandleRecruitmentLocationsChanged;
            _time.OnMinutePassed += HandleMinutePassed;
        }

        public Candidate TryGenerateArrival()
        {
            if (_legacyConfiguration == null || !HasCapacity) return null;

            var role = !_generatedPrototypeCandidate
                ? _legacyConfiguration.FirstPrototypeCandidateRole
                : null;
            var candidate = role.HasValue
                ? _generator.GenerateForRole(_time.CurrentTime, role.Value)
                : _generator.Generate(_time.CurrentTime);
            _generatedPrototypeCandidate = true;
            AddAndRoute(candidate);
            return candidate;
        }

        public Employee Hire(Candidate candidate)
        {
            if (candidate == null || !_candidates.Contains(candidate) || !candidate.MarkHired()) return null;
            var employee = new Employee(candidate.Person, ToEmployeeRole(candidate.Person.ProfessionalRole),
                candidate.SalaryExpectation, 80);
            OnCandidateHired?.Invoke(candidate, employee);
            _router.ReleaseCandidate(candidate);
            _candidates.Remove(candidate);
            OnCandidateRemoved?.Invoke(candidate);
            return employee;
        }

        public bool Reject(Candidate candidate)
        {
            if (candidate == null || !_candidates.Contains(candidate) || !candidate.BeginDeparture()) return false;
            OnCandidateChanged?.Invoke(candidate);
            var result = _router.RouteToExit(candidate, (completed, unused) => HandleDeparture(candidate, completed));
            if (result != CandidateRouteResult.Started) HandleDeparture(candidate, result);
            return true;
        }

        public RecruitmentCoordinatorSnapshot CaptureSnapshot()
        {
            var snapshot = new RecruitmentCoordinatorSnapshot();
            foreach (var state in _intakeStates.Values) snapshot.Categories.Add(state.Capture());
            return snapshot;
        }

        public void RestoreSnapshot(RecruitmentCoordinatorSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (_candidates.Count > 0)
                throw new InvalidOperationException("Restore recruitment before restoring active applicants.");
            foreach (var category in snapshot.Categories)
            {
                if (category == null) continue;
                if (!_intakeStates.TryGetValue(category.Category, out var state))
                {
                    state = new RecruitmentIntakeState(category.Category, 0);
                    _intakeStates.Add(category.Category, state);
                }
                state.Restore(category);
            }
        }

        private void AddAndRoute(Candidate candidate)
        {
            _candidates.Add(candidate);
            OnCandidateAdded?.Invoke(candidate);
            var result = _router.RouteToRecruitmentLocation(candidate,
                (completed, position) => HandleArrival(candidate, completed, position));
            if (result != CandidateRouteResult.Started) HandleArrival(candidate, result, -1);
        }

        private void HandleMinutePassed(SimulationDateTime time)
        {
            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                var candidate = _candidates[i];
                if (candidate.Status != CandidateStatus.WaitingForRecruitment) continue;
                candidate.AdvanceWaitingMinute();
                if (candidate.WaitingMinutes >= MaximumWaitingMinutes(candidate)) Reject(candidate);
            }

            if (_legacyConfiguration != null)
            {
                _minutesUntilLegacyArrival--;
                if (_minutesUntilLegacyArrival <= 0)
                {
                    TryGenerateArrival();
                    _minutesUntilLegacyArrival = _legacyConfiguration.ArrivalCadenceMinutes;
                }
                return;
            }

            foreach (var configuration in _categoryConfigurations.Values)
                AdvanceCategory(configuration);
        }

        private void InitializeOperationalIntakes()
        {
            foreach (var configuration in _categoryConfigurations.Values)
            {
                var state = _intakeStates[configuration.Category];
                if (!state.StarterGrantTriggered &&
                    _router.IsRecruitmentLocationOperational(configuration.Category))
                    state.TriggerStarterGrant(configuration.StarterApplicantCount,
                        configuration.FirstStarterDelayMinutes);
            }
        }

        private void HandleRecruitmentLocationsChanged() => InitializeOperationalIntakes();

        private void AdvanceCategory(RecruitmentCategoryConfiguration configuration)
        {
            var state = _intakeStates[configuration.Category];
            if (!_router.IsRecruitmentLocationOperational(configuration.Category)) return;

            if (!state.StarterGrantTriggered)
            {
                state.TriggerStarterGrant(configuration.StarterApplicantCount,
                    configuration.FirstStarterDelayMinutes);
                return;
            }

            if (state.StarterApplicantsRemaining > 0)
            {
                if (state.StarterRetryMinutes > 0)
                {
                    state.AdvanceRetryTimer();
                    if (state.StarterRetryMinutes > 0) return;
                }
                else
                {
                    state.AdvanceStarterTimer();
                    if (state.MinutesUntilNextStarter > 0) return;
                }

                if (state.StarterApplicantsRemaining <= state.StarterApplicantsInFlight ||
                    !HasPhysicalCapacity(configuration.Category)) return;
                state.ScheduleStarter(configuration.StarterSpacingMinutes);
                Dispatch(configuration, starter: true);
                return;
            }

            state.AdvanceNormalTimer();
            if (state.MinutesUntilNextNormalArrival > 0 || !HasPhysicalCapacity(configuration.Category)) return;
            state.ScheduleNormal(configuration.ArrivalCadenceMinutes);
            Dispatch(configuration, starter: false);
        }

        private bool HasPhysicalCapacity(RecruitmentCategory category)
        {
            int capacity = _router.GetWaitingCapacity(category);
            return capacity > 0 && _candidates.Count(candidate => candidate.Category == category) < capacity;
        }

        private void Dispatch(RecruitmentCategoryConfiguration configuration, bool starter)
        {
            var candidate = _generator.GenerateForCategory(_time.CurrentTime, configuration.Category, starter);
            if (starter) _intakeStates[configuration.Category].StarterDispatched();
            _candidates.Add(candidate);
            OnCandidateAdded?.Invoke(candidate);

            var result = _router.RouteToRecruitmentLocation(candidate,
                (completed, position) => HandleArrival(candidate, completed, position));
            if (result != CandidateRouteResult.Started) HandleArrival(candidate, result, -1);
        }

        private void HandleArrival(Candidate candidate, CandidateRouteResult result, int position)
        {
            if (!_candidates.Contains(candidate)) return;
            if (result == CandidateRouteResult.Started && candidate.MarkWaiting(position))
            {
                if (candidate.IsStarterApplicant &&
                    _intakeStates.TryGetValue(candidate.Category, out var state) &&
                    _categoryConfigurations.TryGetValue(candidate.Category, out var configuration))
                    state.StarterDelivered(configuration.ArrivalCadenceMinutes);

                OnCandidateChanged?.Invoke(candidate);
                return;
            }

            candidate.MarkGone();
            _router.ReleaseCandidate(candidate);
            _candidates.Remove(candidate);
            if (_categoryConfigurations.TryGetValue(candidate.Category, out var failedConfiguration) &&
                _intakeStates.TryGetValue(candidate.Category, out var failedState))
            {
                if (candidate.IsStarterApplicant)
                {
                    failedState.StarterJourneyFailed();
                    failedState.RetryStarter(failedConfiguration.RetryDelayMinutes);
                }
                else
                    failedState.RetryNormal(failedConfiguration.RetryDelayMinutes);
            }
            OnRoutingFailed?.Invoke(candidate, result);
            OnCandidateRemoved?.Invoke(candidate);
        }

        private void HandleDeparture(Candidate candidate, CandidateRouteResult result)
        {
            if (!_candidates.Contains(candidate)) return;
            candidate.MarkGone();
            _router.ReleaseCandidate(candidate);
            _candidates.Remove(candidate);
            if (result != CandidateRouteResult.Started) OnRoutingFailed?.Invoke(candidate, result);
            OnCandidateRemoved?.Invoke(candidate);
        }

        private int MaximumWaitingMinutes(Candidate candidate)
        {
            return _legacyConfiguration != null
                ? _legacyConfiguration.MaximumWaitingMinutes
                : _categoryConfigurations.TryGetValue(candidate.Category, out var configuration)
                    ? configuration.MaximumWaitingMinutes
                    : 2880;
        }

        private static EmployeeRole ToEmployeeRole(ProfessionalRole role) => role switch
        {
            ProfessionalRole.Director => EmployeeRole.Director,
            ProfessionalRole.Extra => EmployeeRole.Extra,
            ProfessionalRole.Crew => EmployeeRole.Crew,
            ProfessionalRole.Writer => EmployeeRole.Writer,
            _ => EmployeeRole.Actor
        };

        public void Dispose()
        {
            _time.OnMinutePassed -= HandleMinutePassed;
            if (_legacyConfiguration == null)
                _router.OnRecruitmentLocationsChanged -= HandleRecruitmentLocationsChanged;
        }
    }
}
