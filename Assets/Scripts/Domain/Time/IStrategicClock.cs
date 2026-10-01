using System;

namespace SilverScreen.Domain.Time
{
    public interface IStrategicClock
    {
        SimulationInstant Now { get; }
    }

    public interface ISimulationControl
    {
        SimulationSpeed RequestedSpeed { get; }
        bool IsEffectivelyPaused { get; }
        void SetRequestedSpeed(SimulationSpeed speed);
        void SetUserPaused(bool paused);
        IDisposable AcquirePause(string ownerId, string reason);
    }
}
