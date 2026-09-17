using UnityEngine;
using UnityEngine.InputSystem;
using SilverScreen.Domain.Time;

namespace SilverScreen.Presentation.SimulationTime
{
    public class SimulationTimeDriver : MonoBehaviour
    {
        [Header("Time Tuning")]
        [Tooltip("How many real seconds correspond to one simulated minute at 1x speed.")]
        [SerializeField] private float _realSecondsPerSimulatedMinute = 1.0f;

        [Header("Starting Date & Time")]
        [SerializeField] private int _startYear = 1930;
        [SerializeField] private int _startMonth = 1;
        [SerializeField] private int _startDay = 1;
        [SerializeField] private int _startHour = 8;
        [SerializeField] private int _startMinute = 0;

        private SimulationClock _clock;

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
            }
        }

        private void Awake()
        {
            EnsureClock();
        }

        private void Update()
        {
            HandleKeyboardShortcuts();

            if (_clock != null)
            {
                _clock.RealSecondsPerSimulatedMinute = _realSecondsPerSimulatedMinute;
                _clock.Advance(UnityEngine.Time.unscaledDeltaTime);
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
    }
}

