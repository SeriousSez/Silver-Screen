using System;

namespace SilverScreen.Domain.Time
{
    public class SimulationClock : ISimulationTimeService, IStrategicClock, ISimulationControl
    {
        private readonly LegacyCalendarEventBridge _legacy = new LegacyCalendarEventBridge();
        private readonly SimulationPauseState _pause = new SimulationPauseState();
        private SimulationSpeed _requestedSpeed = SimulationSpeed.Normal;
        private SimulationInstant _now;
        private double _fractionalSeconds;
        private bool _advancing;
        private SimulationScheduler _scheduler;
        private SimulationInstant? _pendingTarget;
        private bool _boundaryOpen;
        private int _eventsAtBoundary;
        public int ProcessingBudget { get; set; } = 4096;
        public int SameTimestampEventLimit { get; set; } = 10000;
        public bool HasPendingAdvance => _pendingTarget.HasValue;
        public bool IsBoundaryComplete => !_boundaryOpen;
        public string AdvanceFailure { get; private set; }
        public event Action OnBoundaryCompleted;

        public SimulationInstant Now => _now;
        public SimulationDateTime CurrentTime => _now.ToCalendar();
        public SimulationSpeed RequestedSpeed => _requestedSpeed;
        public SimulationSpeed CurrentSpeed => IsPaused ? SimulationSpeed.Paused : _requestedSpeed;
        public bool IsPaused => _pause.IsPaused;
        public bool IsEffectivelyPaused => IsPaused;
        public SimulationPacing Pacing { get; private set; }
        public float TimeScaleMultiplier => (float)Pacing.Multiplier(CurrentSpeed);
        public float RealSecondsPerSimulatedMinute
        {
            get => (float)(Pacing.RealSecondsPerStrategicDay / 1440);
            set => ConfigurePacing(new SimulationPacing("legacy", Math.Max(0.05f, value) * 1440.0));
        }

        public event Action<SimulationDateTime> OnMinutePassed { add => _legacy.Minute += value; remove => _legacy.Minute -= value; }
        public event Action<SimulationDateTime> OnHourPassed { add => _legacy.Hour += value; remove => _legacy.Hour -= value; }
        public event Action<SimulationDateTime> OnDayPassed { add => _legacy.Day += value; remove => _legacy.Day -= value; }
        public event Action<SimulationDateTime> OnMonthPassed { add => _legacy.Month += value; remove => _legacy.Month -= value; }
        public event Action<SimulationDateTime> OnYearPassed { add => _legacy.Year += value; remove => _legacy.Year -= value; }
        public event Action<SimulationSpeed> OnSpeedChanged;

        public SimulationClock(int startYear = 1930, int startMonth = 1, int startDay = 1, int startHour = 8, int startMinute = 0, float realSecondsPerSimulatedMinute = 1)
        {
            _now = SimulationInstant.FromCalendar(startYear, startMonth, startDay, startHour, startMinute);
            RealSecondsPerSimulatedMinute = realSecondsPerSimulatedMinute;
            _pause.Changed += () => OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        public void ConfigurePacing(SimulationPacing pacing) => Pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));

        public void Advance(float realDeltaSeconds) => Advance((double)realDeltaSeconds);
        public void Advance(double realDeltaSeconds)
        {
            if (_advancing) throw new InvalidOperationException("Recursive clock advancement is not allowed.");
            if (double.IsNaN(realDeltaSeconds) || double.IsInfinity(realDeltaSeconds) || realDeltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds));
            if (AdvanceFailure != null) return;
            if (IsPaused && !_boundaryOpen) { _pendingTarget = null; _fractionalSeconds = 0; return; }
            if (IsPaused) { ProcessPending(); return; }
            double seconds = _fractionalSeconds + realDeltaSeconds * TimeScaleMultiplier * 86400 / Pacing.RealSecondsPerStrategicDay;
            long whole = checked((long)Math.Floor(seconds));
            _fractionalSeconds = seconds - whole;
            AdvanceTo((_pendingTarget ?? _now) + new SimulationDuration(whole));
        }

        public void AdvanceTo(SimulationInstant target)
        {
            if (_advancing) throw new InvalidOperationException("Recursive clock advancement is not allowed.");
            if (target < _now) throw new ArgumentOutOfRangeException(nameof(target));
            if (AdvanceFailure != null) return;
            if (IsPaused && !_boundaryOpen) { _pendingTarget = null; _fractionalSeconds = 0; return; }
            if (!_pendingTarget.HasValue || target > _pendingTarget.Value) _pendingTarget = target;
            ProcessPending();
        }

        private void ProcessPending()
        {
            if (ProcessingBudget < 1 || SameTimestampEventLimit < 1) throw new InvalidOperationException("Processing budgets must be positive.");
            _advancing = true;
            try
            {
                int budget = ProcessingBudget;
                while (budget > 0 && _pendingTarget.HasValue)
                {
                    if (_boundaryOpen)
                    {
                        while (_scheduler?.NextDue == _now)
                        {
                            if (budget == 0) return;
                            if (++_eventsAtBoundary > SameTimestampEventLimit) throw new InvalidOperationException("Same-timestamp event chain exceeded its limit.");
                            budget--;
                            if (!_scheduler.DispatchOne()) throw new InvalidOperationException(_scheduler.Failure);
                        }
                        _boundaryOpen = false;
                        OnBoundaryCompleted?.Invoke();
                        if (IsPaused && _scheduler?.NextDue != _now) { _pendingTarget = null; _fractionalSeconds = 0; return; }
                    }
                    var due = _scheduler?.NextDue;
                    if (_now >= _pendingTarget.Value && due != _now) { _pendingTarget = null; return; }
                    var legacy = _legacy.Next(_now);
                    var next = _pendingTarget.Value;
                    if (legacy.HasValue && legacy.Value < next) next = legacy.Value;
                    if (due.HasValue && due.Value < next) next = due.Value;
                    bool moved = next > _now;
                    _now = next;
                    _boundaryOpen = true;
                    if (moved) _eventsAtBoundary = 0;
                    budget--;
                    // Legacy observers see the same boundary before scheduled work completion.
                    if (legacy == _now) _legacy.Dispatch(_now);
                }
            }
            catch (Exception exception)
            {
                AdvanceFailure = exception.ToString();
                _pendingTarget = null;
                _fractionalSeconds = 0;
                // A failed batch is frozen for diagnosis, not retried or silently skipped.
            }
            finally { _advancing = false; }
        }

        internal void AttachScheduler(SimulationScheduler scheduler)
        {
            if (_scheduler != null) throw new InvalidOperationException("A clock can own only one scheduler.");
            _scheduler = scheduler;
        }

        public void RequireConsistentBoundary()
        {
            if (AdvanceFailure != null || _boundaryOpen && !_advancing)
                throw new InvalidOperationException("Finish the current timestamp batch before reading or commanding simulation state.");
        }

        public void SetRequestedSpeed(SimulationSpeed speed)
        {
            if (speed == SimulationSpeed.Paused) throw new ArgumentException("Use SetUserPaused for pause.", nameof(speed));
            Pacing.Multiplier(speed);
            if (_requestedSpeed == speed) return;
            _requestedSpeed = speed;
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        public void SetUserPaused(bool paused) => _pause.SetUserPaused(paused);
        public IDisposable AcquirePause(string ownerId, string reason) => _pause.Acquire(ownerId, reason);
        public void SetSpeed(SimulationSpeed speed)
        {
            if (speed == SimulationSpeed.Paused) SetUserPaused(true);
            else { SetRequestedSpeed(speed); SetUserPaused(false); }
        }
        public void TogglePause() => SetUserPaused(!_pause.UserPaused);

        public void RestoreState(SimulationDateTime time, SimulationSpeed speed, SimulationSpeed prevSpeed)
        {
            if (_advancing) throw new InvalidOperationException("Cannot restore during advancement.");
            if (_scheduler?.PendingCount > 0 || _boundaryOpen || _pendingTarget.HasValue) throw new InvalidOperationException("Restore the whole simulation session when scheduled state exists.");
            _now = time.ToInstant();
            _fractionalSeconds = 0;
            _requestedSpeed = speed != SimulationSpeed.Paused ? speed : prevSpeed != SimulationSpeed.Paused ? prevSpeed : SimulationSpeed.Normal;
            Pacing.Multiplier(_requestedSpeed);
            _pause.SetUserPaused(speed == SimulationSpeed.Paused);
            // Legacy artwork/HUD bindings also refresh a restored date through this notification.
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }
    }
}
