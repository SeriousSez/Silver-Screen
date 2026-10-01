using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain
{
    public enum PersonCareerGoalType
    {
        GetHired,
        GetFirstProfessionalAssignment
    }

    public enum PersonCareerGoalStatus
    {
        Active,
        Completed
    }

    public enum PersonRetentionState
    {
        NotEmployed,
        Content,
        Restless,
        Unhappy,
        ConsideringLeaving
    }

    [Serializable]
    public sealed class PersonCareerGoalSnapshot
    {
        public PersonCareerGoalType Type;
        public PersonCareerGoalStatus Status;
        public int Progress;
        public bool HasCompletedYear;
        public int CompletedYear;
    }

    [Serializable]
    public sealed class PersonCareerGoal
    {
        public PersonCareerGoalType Type { get; private set; }
        public PersonCareerGoalStatus Status { get; private set; }
        public int Progress { get; private set; }
        public bool HasCompletedYear { get; private set; }
        public int CompletedYear { get; private set; }

        internal PersonCareerGoal(PersonCareerGoalType type)
        {
            Type = type;
            Status = PersonCareerGoalStatus.Active;
        }

        internal PersonCareerGoal(PersonCareerGoalSnapshot snapshot)
        {
            Type = snapshot.Type;
            Status = snapshot.Status;
            Progress = Status == PersonCareerGoalStatus.Completed
                ? 100
                : Math.Clamp(snapshot.Progress, 0, 99);
            HasCompletedYear = snapshot.HasCompletedYear;
            CompletedYear = snapshot.CompletedYear;
        }

        internal void SetProgress(int progress) => Progress = Math.Clamp(progress, 0, 99);

        internal bool Complete(int? year)
        {
            if (Status == PersonCareerGoalStatus.Completed) return false;
            Status = PersonCareerGoalStatus.Completed;
            Progress = 100;
            HasCompletedYear = year.HasValue;
            CompletedYear = year.GetValueOrDefault();
            return true;
        }

        internal PersonCareerGoalSnapshot Capture() => new PersonCareerGoalSnapshot
        {
            Type = Type,
            Status = Status,
            Progress = Progress,
            HasCompletedYear = HasCompletedYear,
            CompletedYear = CompletedYear
        };
    }

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
        public int CareerDrive = PersonCareer.DefaultCareerDrive;
        public PersonCareerGoalSnapshot[] Goals = Array.Empty<PersonCareerGoalSnapshot>();
        public long[] WorkloadDayKeys = Array.Empty<long>();
        public int[] WorkloadObservedMinutes = Array.Empty<int>();
        public int[] WorkloadWorkedMinutes = Array.Empty<int>();
        public decimal SatisfactionRemainder;
        public decimal EnergyPressureRemainder;
        public decimal StressPressureRemainder;
        public decimal BoredomPressureRemainder;
        public int RetentionPressure;
        public decimal RetentionPressureRemainder;
    }

    [Serializable]
    public sealed class PersonCareer
    {
        public const int DefaultSatisfaction = 70;
        public const int DefaultCareerDrive = 50;
        public const int DefaultGoalCompletionReward = 5;

        private readonly System.Collections.Generic.List<PersonCareerGoal> _goals =
            new System.Collections.Generic.List<PersonCareerGoal>();
        private readonly ReadOnlyCollection<PersonCareerGoal> _readOnlyGoals;
        private long[] _dayKeys = Array.Empty<long>();
        private int[] _observedMinutes = Array.Empty<int>();
        private int[] _workedMinutes = Array.Empty<int>();
        private decimal _satisfactionRemainder;
        private decimal _energyPressureRemainder;
        private decimal _stressPressureRemainder;
        private decimal _boredomPressureRemainder;
        private decimal _retentionPressureRemainder;
        private bool _workloadTracking;
        private bool _retentionTracking;

        public int CareerSatisfaction { get; private set; } = DefaultSatisfaction;
        public int CareerDrive { get; private set; } = DefaultCareerDrive;
        public int GoalCompletionSatisfactionReward { get; set; } = DefaultGoalCompletionReward;
        public IReadOnlyList<PersonCareerGoal> Goals => _readOnlyGoals;
        public PersonWorkloadState WorkloadState { get; private set; } = PersonWorkloadState.InsufficientHistory;
        public decimal RecentWorkloadPercent { get; private set; }
        public int WorkloadObservedMinutes { get; private set; }
        public int WorkloadWorkedMinutes { get; private set; }
        public int RetentionPressure { get; private set; }
        public PersonRetentionState RetentionState => !_retentionTracking
            ? PersonRetentionState.NotEmployed
            : RetentionPressure < 25 ? PersonRetentionState.Content
            : RetentionPressure < 50 ? PersonRetentionState.Restless
            : RetentionPressure < 75 ? PersonRetentionState.Unhappy
            : PersonRetentionState.ConsideringLeaving;

        public PersonCareer()
        {
            _readOnlyGoals = _goals.AsReadOnly();
            AddGoal(PersonCareerGoalType.GetHired);
        }

        public void SetCareerSatisfaction(int value) => CareerSatisfaction = Math.Clamp(value, 0, 100);
        public void SetCareerDrive(int value) => CareerDrive = Math.Clamp(value, 0, 100);

        public bool AddGoal(PersonCareerGoalType type)
        {
            if (!Enum.IsDefined(typeof(PersonCareerGoalType), type))
                throw new ArgumentOutOfRangeException(nameof(type));
            if (_goals.Exists(goal => goal.Type == type)) return false;
            _goals.Add(new PersonCareerGoal(type));
            return true;
        }

        public bool SetGoalProgress(PersonCareerGoalType type, int progress)
        {
            var goal = _goals.Find(item => item.Type == type);
            if (goal == null || goal.Status == PersonCareerGoalStatus.Completed) return false;
            goal.SetProgress(progress);
            return true;
        }

        internal bool CompleteGoal(PersonCareerGoalType type, int? year = null, int? reward = null)
        {
            var goal = _goals.Find(item => item.Type == type);
            if (goal == null || !goal.Complete(year)) return false;
            SetCareerSatisfaction(CareerSatisfaction + Math.Max(0, reward ?? GoalCompletionSatisfactionReward));
            return true;
        }

        public PersonCareerSnapshot Capture() => new PersonCareerSnapshot
        {
            CareerSatisfaction = CareerSatisfaction,
            CareerDrive = CareerDrive,
            Goals = _goals.ConvertAll(goal => goal.Capture()).ToArray(),
            WorkloadDayKeys = (long[])_dayKeys.Clone(),
            WorkloadObservedMinutes = (int[])_observedMinutes.Clone(),
            WorkloadWorkedMinutes = (int[])_workedMinutes.Clone(),
            SatisfactionRemainder = _satisfactionRemainder,
            EnergyPressureRemainder = _energyPressureRemainder,
            StressPressureRemainder = _stressPressureRemainder,
            BoredomPressureRemainder = _boredomPressureRemainder,
            RetentionPressure = RetentionPressure,
            RetentionPressureRemainder = _retentionPressureRemainder
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
            CareerDrive = Math.Clamp(snapshot.CareerDrive, 0, 100);
            _goals.Clear();
            if (snapshot.Goals == null || snapshot.Goals.Length == 0)
                AddGoal(PersonCareerGoalType.GetHired);
            else
            {
                foreach (var goal in snapshot.Goals)
                {
                    if (goal == null || !Enum.IsDefined(typeof(PersonCareerGoalType), goal.Type) ||
                        !Enum.IsDefined(typeof(PersonCareerGoalStatus), goal.Status) ||
                        goal.HasCompletedYear && goal.Status != PersonCareerGoalStatus.Completed ||
                        _goals.Exists(existing => existing.Type == goal.Type))
                        throw new ArgumentException("Career snapshot contains an invalid or duplicate goal.", nameof(snapshot));
                    _goals.Add(new PersonCareerGoal(goal));
                }
            }
            _dayKeys = dayKeys;
            _observedMinutes = observed;
            _workedMinutes = worked;
            _satisfactionRemainder = snapshot.SatisfactionRemainder;
            _energyPressureRemainder = snapshot.EnergyPressureRemainder;
            _stressPressureRemainder = snapshot.StressPressureRemainder;
            _boredomPressureRemainder = snapshot.BoredomPressureRemainder;
            RetentionPressure = Math.Clamp(snapshot.RetentionPressure, 0, 100);
            _retentionPressureRemainder = snapshot.RetentionPressureRemainder;
            _workloadTracking = false;
            _retentionTracking = false;
            WorkloadState = PersonWorkloadState.InsufficientHistory;
            RecentWorkloadPercent = 0m;
            WorkloadObservedMinutes = 0;
            WorkloadWorkedMinutes = 0;
        }

        internal void SetWorkloadTracking(bool tracking, long absoluteMinute, PersonCareerSimulationRates rates)
        {
            _workloadTracking = tracking;
            _retentionTracking = tracking;
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
                        rates.UnderusedSatisfactionRate(CareerDrive), CareerSatisfaction, 0, 100);
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

        internal void AdvanceRetention(PersonWellbeing wellbeing, PersonCareerSimulationRates rates)
        {
            if (!_retentionTracking) return;

            int negativeSignals = 0;
            if (CareerSatisfaction < rates.RetentionLowSatisfactionThreshold) negativeSignals++;
            if (wellbeing.Mood < rates.RetentionLowMoodThreshold) negativeSignals++;
            if (wellbeing.Stress > rates.RetentionHighStressThreshold) negativeSignals++;
            if (CareerDrive >= rates.RetentionHighDriveThreshold && WorkloadState == PersonWorkloadState.Underused)
                negativeSignals++;

            decimal rate;
            if (negativeSignals > 0)
                rate = Math.Min(rates.RetentionPressurePerNegativeSignalDay * negativeSignals,
                    rates.RetentionMaximumPressurePerDay);
            else if (CareerSatisfaction >= rates.RetentionHealthySatisfactionThreshold &&
                     wellbeing.Mood >= rates.RetentionHealthyMoodThreshold &&
                     wellbeing.Stress <= rates.RetentionManageableStressThreshold &&
                     WorkloadState == PersonWorkloadState.Healthy)
                rate = -rates.RetentionRecoveryPerDay;
            else
                rate = 0m;

            RetentionPressure = ApplyDailyRate(ref _retentionPressureRemainder, rate,
                RetentionPressure, 0, 100);
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
