using System;

namespace SilverScreen.Domain.Time
{
    public sealed class SimulationPacing
    {
        public string Id { get; }
        public double RealSecondsPerStrategicDay { get; }
        public double FastMultiplier { get; }
        public double VeryFastMultiplier { get; }

        public SimulationPacing(string id, double realSecondsPerStrategicDay, double fastMultiplier = 2, double veryFastMultiplier = 3)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Pacing requires an ID.", nameof(id));
            ValidatePositive(realSecondsPerStrategicDay);
            ValidatePositive(fastMultiplier);
            ValidatePositive(veryFastMultiplier);
            Id = id;
            RealSecondsPerStrategicDay = realSecondsPerStrategicDay;
            FastMultiplier = fastMultiplier;
            VeryFastMultiplier = veryFastMultiplier;
        }

        public double Multiplier(SimulationSpeed speed) => speed switch
        {
            SimulationSpeed.Paused => 0,
            SimulationSpeed.Normal => 1,
            SimulationSpeed.Fast => FastMultiplier,
            SimulationSpeed.VeryFast => VeryFastMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(speed))
        };

        internal static void ValidatePositive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
