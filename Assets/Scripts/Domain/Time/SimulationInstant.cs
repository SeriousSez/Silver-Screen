using System;

namespace SilverScreen.Domain.Time
{
    /// <summary>Whole Gregorian seconds since 0001-01-01, with no time zone.</summary>
    public readonly struct SimulationInstant : IEquatable<SimulationInstant>, IComparable<SimulationInstant>
    {
        public static readonly long MaxSeconds = DateTime.MaxValue.Ticks / TimeSpan.TicksPerSecond;
        public long Seconds { get; }

        public SimulationInstant(long seconds)
        {
            if (seconds < 0 || seconds > MaxSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
            Seconds = seconds;
        }

        public static SimulationInstant FromCalendar(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
            => new SimulationInstant(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified).Ticks / TimeSpan.TicksPerSecond);

        public SimulationDateTime ToCalendar()
        {
            var date = new DateTime(Seconds * TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);
            return new SimulationDateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute);
        }

        public int CompareTo(SimulationInstant other) => Seconds.CompareTo(other.Seconds);
        public bool Equals(SimulationInstant other) => Seconds == other.Seconds;
        public override bool Equals(object obj) => obj is SimulationInstant other && Equals(other);
        public override int GetHashCode() => Seconds.GetHashCode();
        public override string ToString() => new DateTime(Seconds * TimeSpan.TicksPerSecond).ToString("yyyy-MM-dd HH:mm:ss");
        public static SimulationInstant operator +(SimulationInstant instant, SimulationDuration duration) => new SimulationInstant(checked(instant.Seconds + duration.Seconds));
        public static SimulationDuration operator -(SimulationInstant a, SimulationInstant b) => new SimulationDuration(checked(a.Seconds - b.Seconds));
        public static bool operator <(SimulationInstant a, SimulationInstant b) => a.Seconds < b.Seconds;
        public static bool operator >(SimulationInstant a, SimulationInstant b) => a.Seconds > b.Seconds;
        public static bool operator <=(SimulationInstant a, SimulationInstant b) => a.Seconds <= b.Seconds;
        public static bool operator >=(SimulationInstant a, SimulationInstant b) => a.Seconds >= b.Seconds;
        public static bool operator ==(SimulationInstant a, SimulationInstant b) => a.Equals(b);
        public static bool operator !=(SimulationInstant a, SimulationInstant b) => !a.Equals(b);
    }
}
