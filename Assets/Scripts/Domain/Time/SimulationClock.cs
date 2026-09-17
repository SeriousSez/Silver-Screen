using System;

namespace SilverScreen.Domain.Time
{
    [Serializable]
    public class SimulationClock : ISimulationTimeService
    {
        private SimulationDateTime _currentTime;
        private SimulationSpeed _currentSpeed;
        private SimulationSpeed _previousActiveSpeed;
        private float _realSecondsPerSimulatedMinute;
        private float _timeAccumulator;

        public SimulationDateTime CurrentTime => _currentTime;
        public SimulationSpeed CurrentSpeed => _currentSpeed;
        public bool IsPaused => _currentSpeed == SimulationSpeed.Paused;

        public float RealSecondsPerSimulatedMinute
        {
            get => _realSecondsPerSimulatedMinute;
            set => _realSecondsPerSimulatedMinute = Math.Max(0.05f, value);
        }

        public float TimeScaleMultiplier
        {
            get
            {
                return _currentSpeed switch
                {
                    SimulationSpeed.Paused => 0f,
                    SimulationSpeed.Normal => 1f,
                    SimulationSpeed.Fast => 2f,
                    SimulationSpeed.VeryFast => 3f,
                    _ => 1f
                };
            }
        }

        public event Action<SimulationDateTime> OnMinutePassed;
        public event Action<SimulationDateTime> OnHourPassed;
        public event Action<SimulationDateTime> OnDayPassed;
        public event Action<SimulationDateTime> OnMonthPassed;
        public event Action<SimulationDateTime> OnYearPassed;
        public event Action<SimulationSpeed> OnSpeedChanged;

        public SimulationClock(
            int startYear = 1930,
            int startMonth = 1,
            int startDay = 1,
            int startHour = 8,
            int startMinute = 0,
            float realSecondsPerSimulatedMinute = 1.0f)
        {
            _currentTime = new SimulationDateTime(startYear, startMonth, startDay, startHour, startMinute);
            _currentSpeed = SimulationSpeed.Normal;
            _previousActiveSpeed = SimulationSpeed.Normal;
            _realSecondsPerSimulatedMinute = Math.Max(0.05f, realSecondsPerSimulatedMinute);
            _timeAccumulator = 0f;
        }

        public void Advance(float realDeltaSeconds)
        {
            if (IsPaused || realDeltaSeconds <= 0f) return;

            float effectiveMultiplier = TimeScaleMultiplier;
            _timeAccumulator += realDeltaSeconds * effectiveMultiplier;

            // Advance minutes based on accumulator
            while (_timeAccumulator >= _realSecondsPerSimulatedMinute)
            {
                _timeAccumulator -= _realSecondsPerSimulatedMinute;

                _currentTime.AdvanceMinute(
                    out bool hourRolled,
                    out bool dayRolled,
                    out bool monthRolled,
                    out bool yearRolled);

                OnMinutePassed?.Invoke(_currentTime);

                if (hourRolled)
                {
                    OnHourPassed?.Invoke(_currentTime);
                }

                if (dayRolled)
                {
                    OnDayPassed?.Invoke(_currentTime);
                }

                if (monthRolled)
                {
                    OnMonthPassed?.Invoke(_currentTime);
                }

                if (yearRolled)
                {
                    OnYearPassed?.Invoke(_currentTime);
                }
            }
        }

        public void SetSpeed(SimulationSpeed speed)
        {
            if (_currentSpeed == speed) return;

            if (speed != SimulationSpeed.Paused)
            {
                _previousActiveSpeed = speed;
            }

            _currentSpeed = speed;
            OnSpeedChanged?.Invoke(_currentSpeed);
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                // Resume to prior active speed
                SimulationSpeed speedToRestore = _previousActiveSpeed != SimulationSpeed.Paused
                    ? _previousActiveSpeed
                    : SimulationSpeed.Normal;

                SetSpeed(speedToRestore);
            }
            else
            {
                // Remember current speed and pause
                _previousActiveSpeed = _currentSpeed;
                SetSpeed(SimulationSpeed.Paused);
            }
        }

        public void RestoreState(SimulationDateTime time, SimulationSpeed speed, SimulationSpeed prevSpeed)
        {
            _currentTime = time;
            _currentSpeed = speed;
            _previousActiveSpeed = prevSpeed != SimulationSpeed.Paused ? prevSpeed : SimulationSpeed.Normal;
            _timeAccumulator = 0f;
            OnSpeedChanged?.Invoke(_currentSpeed);
        }
    }
}
