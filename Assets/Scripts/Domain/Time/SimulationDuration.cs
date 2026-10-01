using System;

namespace SilverScreen.Domain.Time
{
    public readonly struct SimulationDuration : IEquatable<SimulationDuration>
    {
        public long Seconds { get; }
        public SimulationDuration(long seconds) { Seconds = seconds; }
        public static SimulationDuration FromMinutes(long minutes) => new SimulationDuration(checked(minutes * 60));
        public static SimulationDuration FromDays(long days) => new SimulationDuration(checked(days * 86400));
        public bool Equals(SimulationDuration other) => Seconds == other.Seconds;
        public override bool Equals(object obj) => obj is SimulationDuration other && Equals(other);
        public override int GetHashCode() => Seconds.GetHashCode();
        public static SimulationDuration operator +(SimulationDuration a, SimulationDuration b) => new SimulationDuration(checked(a.Seconds + b.Seconds));
    }
}
