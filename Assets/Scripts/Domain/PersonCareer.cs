using System;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain
{
    public enum PersonWorkloadState
    {
        InsufficientHistory,
        Underused,
        Healthy,
        Overworked
    }

    [Serializable]
    public sealed class PersonCareerSnapshot
    {
        public int CareerSatisfaction = PersonCareer.DefaultSatisfaction;
        public long[] WorkloadDayKeys = Array.Empty<long>();
        public int[] WorkloadObservedMinutes = Array.Empty<int>();
        public int[] WorkloadWorkedMinutes = Array.Empty<int>();
        public decimal SatisfactionRemainder;
        public decimal EnergyPressureRemainder;
        public decimal StressPressureRemainder;
        public decimal BoredomPressureRemainder;
    }

    [Serializable]
    public sealed class PersonCareer
    {
        public const int DefaultSatisfaction = 70;

        private long[] _dayKeys = Array.Empty<long>();
        private int[] _observedMinutes = Array.Empty<int>();
        private int[] _workedMinutes = Array.Empty<int>();
        private decimal _satisfactionRemainder;
        private decimal _energyPressureRemainder;
        private decimal _stressPressureRemainder;
        private decimal _boredomPressureRemainder;
        private bool _workloadTracking;

        public int CareerSatisfaction { get; private set; } = DefaultSatisfaction;
        public PersonWorkloadState WorkloadState { get; private set; } = PersonWorkloadState.InsufficientHistory;
        public decimal RecentWorkloadPercent { get; private set; }
        public int WorkloadObservedMinutes { get; private set; }
        public int WorkloadWorkedMinutes { get; private set; }

        public void SetCareerSatisfaction(int value) => CareerSatisfaction = Math.Clamp(value, 0, 100);

        public PersonCareerSnapshot Capture() => new PersonCareerSnapshot
        {
            CareerSatisfaction = CareerSatisfaction,
            WorkloadDayKeys = (long[])_dayKeys.Clone(),
            WorkloadObservedMinutes = (int[])_observedMinutes.Clone(),
            WorkloadWorkedMinutes = (int[])_workedMinutes.Clone(),
            SatisfactionRemainder = _satisfactionRemainder,
            EnergyPressureRemainder = _energyPressureRemainder,
            StressPressureRemainder = _stressPressureRemainder,
            BoredomPressureRemainder = _boredomPressureRemainder
        };

        public void Restore(PersonCareerSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            int buckets = snapshot.WorkloadDayKeys?.Length ?? 0;
            if ((snapshot.WorkloadObservedMinutes?.Length ?? 0) != buckets ||
                (snapshot.WorkloadWorkedMinutes?.Length ?? 0) != buckets)
                throw new ArgumentException("Career workload snapshot has inconsistent day buckets.", nameof(snapshot));

            var dayKeys = snapshot.WorkloadDayKeys == null ? Array.Empty<long>() : (long[])snapshot.WorkloadDayKeys.Clone();
            var observed = snapshot.WorkloadObservedMinutes == null ? Array.Empty<int>() : (int[])snapshot.WorkloadObservedMinutes.Clone();
            var worked = snapshot.WorkloadWorkedMinutes == null ? Array.Empty<int>() : (int[])snapshot.WorkloadWorkedMinutes.Clone();
            for (int i = 0; i < buckets; i++)
            {
                if (observed[i] < 0 || worked[i] < 0 || worked[i] > observed[i])
                    throw new ArgumentException("Career workload snapshot contains invalid minute counts.", nameof(snapshot));
            }

            CareerSatisfaction = Math.Clamp(snapshot.CareerSatisfaction, 0, 100);
            _dayKeys = dayKeys;
            _observedMinutes = observed;
            _workedMinutes = worked;
            _satisfactionRemainder = snapshot.SatisfactionRemainder;
            _energyPressureRemainder = snapshot.EnergyPressureRemainder;
            _stressPressureRemainder = snapshot.StressPressureRemainder;
            _boredomPressureRemainder = snapshot.BoredomPressureRemainder;
            _workloadTracking = false;
            WorkloadState = PersonWorkloadState.InsufficientHistory;
            RecentWorkloadPercent = 0m;
            WorkloadObservedMinutes = 0;
            WorkloadWorkedMinutes = 0;
        }

        internal void SetWorkloadTracking(bool tracking, long absoluteMinute, PersonCareerSimulationRates rates)
        {
            _workloadTracking = tracking;
            if (tracking)
            {
                EnsureCapacity(rates.EvaluationDays);
                EvaluateWorkload(absoluteMinute / 1440, rates);
            }
        }

        internal bool IsWorkloadTracking => _workloadTracking;

        internal void RecordWorkMinute(long absoluteMinute, bool worked, PersonCareerSimulationRates rates)
        {
            if (!_workloadTracking) return;
            int days = rates.EvaluationDays;
            EnsureCapacity(days);
            long day = absoluteMinute / 1440;
            int index = (int)(day % days);
            if (_dayKeys[index] != day)
            {
                _dayKeys[index] = day;
                _observedMinutes[index] = 0;
                _workedMinutes[index] = 0;
            }

            _observedMinutes[index]++;
            if (worked) _workedMinutes[index]++;
            EvaluateWorkload(day, rates);
        }

        private void EnsureCapacity(int days)
        {
            if (_dayKeys.Length == days) return;
            var oldKeys = _dayKeys;
            var oldObserved = _observedMinutes;
            var oldWorked = _workedMinutes;
            _dayKeys = new long[days];
            _observedMinutes = new int[days];
            _workedMinutes = new int[days];
            Array.Fill(_dayKeys, long.MinValue);

            int copyCount = Math.Min(days, oldKeys.Length);
            for (int offset = 0; offset < copyCount; offset++)
            {
                long key = oldKeys.Length == 0 ? long.MinValue : oldKeys[offset];
                if (key == long.MinValue) continue;
                int index = (int)(key % days);
                _dayKeys[index] = key;
                _observedMinutes[index] = oldObserved[offset];
                _workedMinutes[index] = oldWorked[offset];
            }
        }

        private void EvaluateWorkload(long currentDay, PersonCareerSimulationRates rates)
        {
            long firstDay = currentDay - rates.EvaluationDays + 1;
            int observed = 0;
            int worked = 0;
            for (int i = 0; i < _dayKeys.Length; i++)
            {
                if (_dayKeys[i] < firstDay || _dayKeys[i] > currentDay) continue;
                observed += _observedMinutes[i];
                worked += _workedMinutes[i];
            }

            WorkloadObservedMinutes = observed;
            WorkloadWorkedMinutes = worked;
            RecentWorkloadPercent = observed == 0 ? 0m : worked * 100m / observed;
            WorkloadState = observed < rates.GracePeriodMinutes
                ? PersonWorkloadState.InsufficientHistory
                : RecentWorkloadPercent < rates.UnderusedThresholdPercent
                    ? PersonWorkloadState.Underused
                    : RecentWorkloadPercent <= rates.OverworkedThresholdPercent
                        ? PersonWorkloadState.Healthy
                        : PersonWorkloadState.Overworked;
        }

        internal void AdvanceWorkloadEffects(PersonWellbeing wellbeing, PersonWorkloadState state, PersonCareerSimulationRates rates)
        {
            switch (state)
            {
                case PersonWorkloadState.Underused:
                    CareerSatisfaction = ApplyDailyRate(ref _satisfactionRemainder,
                        rates.UnderusedSatisfactionPerDay, CareerSatisfaction, 0, 100);
                    _boredomPressureRemainder = ApplyPressure(wellbeing, PersonWellbeingDimension.Boredom,
                        ref _boredomPressureRemainder, rates.UnderusedBoredomPerDay);
                    break;
                case PersonWorkloadState.Healthy:
                    CareerSatisfaction = ApplyDailyRate(ref _satisfactionRemainder,
                        rates.HealthySatisfactionPerDay, CareerSatisfaction, 0,
                        (int)decimal.Floor(Math.Clamp(rates.HealthySatisfactionCeiling, 0m, 100m)));
                    break;
                case PersonWorkloadState.Overworked:
                    CareerSatisfaction = ApplyDailyRate(ref _satisfactionRemainder,
                        rates.OverworkedSatisfactionPerDay, CareerSatisfaction, 0, 100);
                    _energyPressureRemainder = ApplyPressure(wellbeing, PersonWellbeingDimension.Energy,
                        ref _energyPressureRemainder, rates.OverworkedEnergyPerDay);
                    _stressPressureRemainder = ApplyPressure(wellbeing, PersonWellbeingDimension.Stress,
                        ref _stressPressureRemainder, rates.OverworkedStressPerDay);
                    break;
            }
        }

        private static int ApplyDailyRate(ref decimal remainder, decimal rate, int value, int minimum, int maximum)
        {
            decimal total = remainder + rate / 1440m;
            int delta = (int)decimal.Truncate(total);
            remainder = total - delta;
            if (delta == 0) return value;
            int next = Math.Clamp(value + delta, minimum, maximum);
            if (next == minimum && delta < 0 || next == maximum && delta > 0) remainder = 0m;
            return next;
        }

        private static decimal ApplyPressure(PersonWellbeing wellbeing, PersonWellbeingDimension dimension,
            ref decimal remainder, decimal rate)
        {
            decimal total = remainder + rate / 1440m;
            int delta = (int)decimal.Truncate(total);
            remainder = total - delta;
            if (delta == 0) return remainder;

            int before = Value(wellbeing, dimension);
            wellbeing.ApplyDelta(dimension, delta);
            int after = Value(wellbeing, dimension);
            if (delta > 0 && after == PersonWellbeing.MaximumValue ||
                delta < 0 && after == PersonWellbeing.MinimumValue ||
                before == after)
                remainder = 0m;
            return remainder;
        }

        private static int Value(PersonWellbeing wellbeing, PersonWellbeingDimension dimension) => dimension switch
        {
            PersonWellbeingDimension.Energy => wellbeing.Energy,
            PersonWellbeingDimension.Stress => wellbeing.Stress,
            PersonWellbeingDimension.Boredom => wellbeing.Boredom,
            PersonWellbeingDimension.Mood => wellbeing.Mood,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };
    }
}
