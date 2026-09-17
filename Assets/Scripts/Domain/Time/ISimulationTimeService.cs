using System;

namespace SilverScreen.Domain.Time
{
    public interface ISimulationTimeService
    {
        SimulationDateTime CurrentTime { get; }
        SimulationSpeed CurrentSpeed { get; }
        bool IsPaused { get; }
        float TimeScaleMultiplier { get; }
        float RealSecondsPerSimulatedMinute { get; set; }

        event Action<SimulationDateTime> OnMinutePassed;
        event Action<SimulationDateTime> OnHourPassed;
        event Action<SimulationDateTime> OnDayPassed;
        event Action<SimulationDateTime> OnMonthPassed;
        event Action<SimulationDateTime> OnYearPassed;
        event Action<SimulationSpeed> OnSpeedChanged;

        void SetSpeed(SimulationSpeed speed);
        void TogglePause();
    }
}
