using System;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Work
{
    public readonly struct WorkQuantity
    {
        public const long Precision = 1000000;
        public string MeasureId { get; }
        public long MicroUnits { get; }
        public decimal Units => MicroUnits / (decimal)Precision;
        public WorkQuantity(string measureId, long microUnits)
        {
            if (string.IsNullOrWhiteSpace(measureId)) throw new ArgumentException("A work measure is required.");
            if (microUnits < 0) throw new ArgumentOutOfRangeException(nameof(microUnits));
            MeasureId = measureId; MicroUnits = microUnits;
        }
        public static WorkQuantity FromUnits(string measureId, decimal units)
        {
            if (units < 0) throw new ArgumentOutOfRangeException(nameof(units));
            return new WorkQuantity(measureId, checked((long)(units * Precision)));
        }
    }

    public readonly struct WorkRate
    {
        public WorkQuantity Quantity { get; }
        public SimulationDuration Per { get; }
        public WorkRate(WorkQuantity quantity, SimulationDuration per)
        {
            if (string.IsNullOrWhiteSpace(quantity.MeasureId) || per.Seconds <= 0) throw new ArgumentException("A measured quantity and positive period are required.");
            Quantity = quantity; Per = per;
        }
        internal decimal EarnedMicroUnits(long elapsedSeconds) => Quantity.MicroUnits * (decimal)elapsedSeconds / Per.Seconds;
    }
}
