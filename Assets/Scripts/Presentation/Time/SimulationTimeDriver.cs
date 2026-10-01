using UnityEngine;
using UnityEngine.InputSystem;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain;

namespace SilverScreen.Presentation.SimulationTime
{
    [DisallowMultipleComponent]
    public class SimulationTimeDriver : MonoBehaviour
    {
        [Header("Time Tuning")]
        [Tooltip("How many real seconds correspond to one simulated minute at 1x speed.")]
        [SerializeField] private float _realSecondsPerSimulatedMinute = 1.0f;
        [SerializeField] private SimulationPacingSettings _pacingProfile;

        [Header("Starting Date & Time")]
        [SerializeField] private int _startYear = 1930;
        [SerializeField] private int _startMonth = 1;
        [SerializeField] private int _startDay = 1;
        [SerializeField] private int _startHour = 8;
        [SerializeField] private int _startMinute = 0;

        private SimulationClock _clock;
        private SimulationScheduler _scheduler;
        private WorkService _work;
        private PersonWellbeingSimulation _wellbeing;
        private PersonAutonomySimulation _autonomy;
        private PersonActivityOpportunityRegistry _autonomyOpportunities;
        private ResourceReservationBook _reservations;
        private bool _reportedFailure;
        private static SimulationTimeDriver _activeDriver;
        private float _previousUnityTimeScale;
        public SimulationScheduler Scheduler { get { EnsureClock(); return _scheduler; } }
        public WorkService Work { get { EnsureClock(); return _work; } }
        public ResourceReservationBook Reservations { get { EnsureClock(); return _reservations; } }
        public PersonWellbeingSimulation Wellbeing { get { EnsureClock(); return _wellbeing; } }
        public PersonAutonomySimulation Autonomy { get { EnsureClock(); return _autonomy; } }
        public PersonActivityOpportunityRegistry AutonomyOpportunities { get { EnsureClock(); return _autonomyOpportunities; } }

        public ISimulationTimeService TimeService
        {
            get
            {
                EnsureClock();
                return _clock;
            }
        }

        public SimulationClock Clock
        {
            get
            {
                EnsureClock();
                return _clock;
            }
        }

        public void InitializeNewStudioSession(SimulationDateTime start)
        {
            EnsureClock();
            _clock.RestoreState(start, SimulationSpeed.Normal, SimulationSpeed.Normal);
        }

        private void EnsureClock()
        {
            if (_clock == null)
            {
                _clock = new SimulationClock(
                    _startYear,
                    _startMonth,
                    _startDay,
                    _startHour,
                    _startMinute,
                    _realSecondsPerSimulatedMinute);
                if (_pacingProfile != null) _clock.ConfigurePacing(_pacingProfile.CreatePacing());
                _scheduler = new SimulationScheduler(_clock);
                _work = new WorkService(_clock, _scheduler);
                _reservations = new ResourceReservationBook();
                _wellbeing = new PersonWellbeingSimulation(_clock);
                _autonomyOpportunities = new PersonActivityOpportunityRegistry();
                _autonomy = new PersonAutonomySimulation(_clock, _scheduler, _autonomyOpportunities);
            }
        }

        private void Awake()
        {
            EnsureClock();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (_activeDriver != null && _activeDriver != this)
            {
                Debug.LogError("Only one active SimulationTimeDriver may own simulation speed.", this);
                enabled = false;
                return;
            }

            EnsureClock();
            _activeDriver = this;
            _previousUnityTimeScale = UnityEngine.Time.timeScale;
            _clock.OnSpeedChanged += ApplyWorldSpeed;
            ApplyWorldSpeed(_clock.CurrentSpeed);
        }

        private void ApplyWorldSpeed(SimulationSpeed unused)
        {
            // The clock owns speed and pause intent. Unity is its world-time adapter:
            // NavMesh, ordinary Animators, physics and scaled timers consume this once.
            if (_activeDriver == this) UnityEngine.Time.timeScale = _clock.TimeScaleMultiplier;
        }

        private void OnDisable()
        {
            if (_activeDriver != this) return;
            _clock.OnSpeedChanged -= ApplyWorldSpeed;
            _activeDriver = null;
            UnityEngine.Time.timeScale = _previousUnityTimeScale;
        }

        private void Update()
        {
            if (_clock == null || _clock.IsBoundaryComplete) HandleKeyboardShortcuts();

            if (_clock != null)
            {
                // Advance from real seconds: the clock applies the multiplier itself.
                // Passing deltaTime here would apply 2x/3x twice.
                _clock.Advance(UnityEngine.Time.unscaledDeltaTime);
                if (_clock.AdvanceFailure != null && !_reportedFailure)
                {
                    _reportedFailure = true;
                    Debug.LogError("Strategic simulation stopped: " + _clock.AdvanceFailure, this);
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _clock == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _clock.TogglePause();
            }
            else if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                _clock.SetSpeed(SimulationSpeed.Normal);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                _clock.SetSpeed(SimulationSpeed.Fast);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                _clock.SetSpeed(SimulationSpeed.VeryFast);
            }
        }

        private void OnDestroy()
        {
            _wellbeing?.Dispose();
            _autonomy?.Dispose();
            _work?.Dispose();
        }
    }
}
