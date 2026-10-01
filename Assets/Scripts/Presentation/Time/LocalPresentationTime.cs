using SilverScreen.Domain.Time;

namespace SilverScreen.Presentation.SimulationTime
{
    /// <summary>Simulation-bound activity seconds from real elapsed time; focused observation stays real-time.</summary>
    public static class LocalPresentationTime
    {
        public static float Delta(float unscaledDelta, ISimulationTimeService time, bool focusedObservation = false)
            => focusedObservation || time == null ? unscaledDelta : unscaledDelta * time.TimeScaleMultiplier;
    }
}
