using System;

namespace SilverScreen.Domain.Time
{
    /// <summary>Compatibility boundaries only; new work must use the scheduler.</summary>
    internal sealed class LegacyCalendarEventBridge
    {
        public event Action<SimulationDateTime> Minute;
        public event Action<SimulationDateTime> Hour;
        public event Action<SimulationDateTime> Day;
        public event Action<SimulationDateTime> Month;
        public event Action<SimulationDateTime> Year;

        public SimulationInstant? Next(SimulationInstant now)
        {
            long interval = Minute != null ? 60 : Hour != null ? 3600 : Day != null ? 86400 : 0;
            if (interval != 0)
            {
                long seconds = checked((now.Seconds / interval + 1) * interval);
                return seconds <= SimulationInstant.MaxSeconds ? new SimulationInstant(seconds) : (SimulationInstant?)null;
            }
            var date = now.ToCalendar();
            if (Month != null)
                return date.Month == 12 ? (date.Year < 9999 ? SimulationInstant.FromCalendar(date.Year + 1, 1, 1) : (SimulationInstant?)null) : SimulationInstant.FromCalendar(date.Year, date.Month + 1, 1);
            return Year != null && date.Year < 9999 ? SimulationInstant.FromCalendar(date.Year + 1, 1, 1) : (SimulationInstant?)null;
        }

        public void Dispatch(SimulationInstant instant)
        {
            if (instant.Seconds % 60 != 0) return;
            var date = instant.ToCalendar();
            Minute?.Invoke(date);
            if (date.Minute != 0) return;
            Hour?.Invoke(date);
            if (date.Hour != 0) return;
            Day?.Invoke(date);
            if (date.Day != 1) return;
            Month?.Invoke(date);
            if (date.Month == 1) Year?.Invoke(date);
        }
    }
}
