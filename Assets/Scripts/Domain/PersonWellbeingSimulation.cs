using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain
{
    public sealed class PersonCareerSimulationRates
    {
        public const int DefaultEvaluationDays = 7;

        public int EvaluationDays { get; set; } = DefaultEvaluationDays;
        public int GracePeriodMinutes { get; set; } = 1440;
        public decimal UnderusedThresholdPercent { get; set; } = 20m;
        public decimal OverworkedThresholdPercent { get; set; } = 60m;
        public decimal UnderusedSatisfactionPerDay { get; set; } = -2m;
        public decimal UnderuseDriveSensitivityPer50 { get; set; } = 1m;
        public decimal HealthySatisfactionPerDay { get; set; } = 1m;
        public decimal HealthySatisfactionCeiling { get; set; } = 80m;
        public decimal OverworkedSatisfactionPerDay { get; set; } = -1m;
        public decimal UnderusedBoredomPerDay { get; set; } = 2m;
        public decimal OverworkedStressPerDay { get; set; } = 2m;
        public decimal OverworkedEnergyPerDay { get; set; } = -2m;
        public int RetentionLowSatisfactionThreshold { get; set; } = 40;
        public int RetentionLowMoodThreshold { get; set; } = 35;
        public int RetentionHighStressThreshold { get; set; } = 75;
        public int RetentionHighDriveThreshold { get; set; } = 75;
        public int RetentionHealthySatisfactionThreshold { get; set; } = 60;
        public int RetentionHealthyMoodThreshold { get; set; } = 50;
        public int RetentionManageableStressThreshold { get; set; } = 50;
        public decimal RetentionPressurePerNegativeSignalDay { get; set; } = 1m;
        public decimal RetentionMaximumPressurePerDay { get; set; } = 3m;
        public decimal RetentionRecoveryPerDay { get; set; } = 1m;

        internal decimal UnderusedSatisfactionRate(int careerDrive) =>
            UnderusedSatisfactionPerDay -
            (Math.Clamp(careerDrive, 0, 100) - 50) * UnderuseDriveSensitivityPer50 / 50m;

        internal void Validate()
        {
            if (EvaluationDays < 1) throw new ArgumentOutOfRangeException(nameof(EvaluationDays));
            if (GracePeriodMinutes < 1) throw new ArgumentOutOfRangeException(nameof(GracePeriodMinutes));
            if (UnderusedThresholdPercent < 0 || OverworkedThresholdPercent > 100 ||
                UnderusedThresholdPercent >= OverworkedThresholdPercent)
                throw new ArgumentOutOfRangeException(nameof(UnderusedThresholdPercent), "Workload thresholds must be ordered within 0-100.");
            ValidatePercent(RetentionLowSatisfactionThreshold, nameof(RetentionLowSatisfactionThreshold));
            ValidatePercent(RetentionLowMoodThreshold, nameof(RetentionLowMoodThreshold));
            ValidatePercent(RetentionHighStressThreshold, nameof(RetentionHighStressThreshold));
            ValidatePercent(RetentionHighDriveThreshold, nameof(RetentionHighDriveThreshold));
            ValidatePercent(RetentionHealthySatisfactionThreshold, nameof(RetentionHealthySatisfactionThreshold));
            ValidatePercent(RetentionHealthyMoodThreshold, nameof(RetentionHealthyMoodThreshold));
            ValidatePercent(RetentionManageableStressThreshold, nameof(RetentionManageableStressThreshold));
            if (UnderuseDriveSensitivityPer50 < 0)
                throw new ArgumentOutOfRangeException(nameof(UnderuseDriveSensitivityPer50));
            if (RetentionPressurePerNegativeSignalDay < 0 ||
                RetentionMaximumPressurePerDay < 0 ||
                RetentionRecoveryPerDay < 0)
                throw new ArgumentOutOfRangeException(nameof(RetentionPressurePerNegativeSignalDay),
                    "Career sensitivity and retention rates cannot be negative.");
        }

        private static void ValidatePercent(int value, string name)
        {
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class PersonWellbeingRates
    {
        public decimal IdleEnergyPerHour { get; set; } = -1m;
        public decimal IdleStressPerHour { get; set; } = -1m;
        public decimal IdleBoredomPerHour { get; set; } = 4m;
        public decimal IdleMoodPerHour { get; set; }

        public decimal WorkingEnergyPerHour { get; set; } = -4m;
        public decimal WorkingStressPerHour { get; set; } = 2m;
        public decimal WorkingBoredomPerHour { get; set; } = -5m;
        public decimal WorkingMoodPerHour { get; set; }

        public decimal RestingEnergyPerHour { get; set; } = 8m;
        public decimal RestingStressPerHour { get; set; } = -4m;
        public decimal RestingBoredomPerHour { get; set; } = 1m;
        public decimal RestingMoodPerHour { get; set; } = 1m;

        public decimal SocializingEnergyPerHour { get; set; } = -1m;
        public decimal SocializingStressPerHour { get; set; } = -3m;
        public decimal SocializingBoredomPerHour { get; set; } = -5m;
        public decimal SocializingMoodPerHour { get; set; } = 2m;

        public decimal RecreationEnergyPerHour { get; set; } = -1m;
        public decimal RecreationStressPerHour { get; set; } = -4m;
        public decimal RecreationBoredomPerHour { get; set; } = -7m;
        public decimal RecreationMoodPerHour { get; set; } = 2m;

        internal decimal Rate(PersonWellbeingActivity activity, PersonWellbeingDimension dimension)
        {
            switch (activity)
            {
                case PersonWellbeingActivity.Idle:
                    return dimension switch
                    {
                        PersonWellbeingDimension.Energy => IdleEnergyPerHour,
                        PersonWellbeingDimension.Stress => IdleStressPerHour,
                        PersonWellbeingDimension.Boredom => IdleBoredomPerHour,
                        PersonWellbeingDimension.Mood => IdleMoodPerHour,
                        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
                    };
                case PersonWellbeingActivity.Working:
                    return dimension switch
                    {
                        PersonWellbeingDimension.Energy => WorkingEnergyPerHour,
                        PersonWellbeingDimension.Stress => WorkingStressPerHour,
                        PersonWellbeingDimension.Boredom => WorkingBoredomPerHour,
                        PersonWellbeingDimension.Mood => WorkingMoodPerHour,
                        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
                    };
                case PersonWellbeingActivity.Resting:
                    return dimension switch
                    {
                        PersonWellbeingDimension.Energy => RestingEnergyPerHour,
                        PersonWellbeingDimension.Stress => RestingStressPerHour,
                        PersonWellbeingDimension.Boredom => RestingBoredomPerHour,
                        PersonWellbeingDimension.Mood => RestingMoodPerHour,
                        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
                    };
                case PersonWellbeingActivity.Socializing:
                    return dimension switch
                    {
                        PersonWellbeingDimension.Energy => SocializingEnergyPerHour,
                        PersonWellbeingDimension.Stress => SocializingStressPerHour,
                        PersonWellbeingDimension.Boredom => SocializingBoredomPerHour,
                        PersonWellbeingDimension.Mood => SocializingMoodPerHour,
                        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
                    };
                case PersonWellbeingActivity.Recreation:
                    return dimension switch
                    {
                        PersonWellbeingDimension.Energy => RecreationEnergyPerHour,
                        PersonWellbeingDimension.Stress => RecreationStressPerHour,
                        PersonWellbeingDimension.Boredom => RecreationBoredomPerHour,
                        PersonWellbeingDimension.Mood => RecreationMoodPerHour,
                        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(activity), activity, "Unknown wellbeing activity.");
            }
        }
    }

    public sealed class PersonWellbeingSimulation : IDisposable
    {
        private sealed class Participant
        {
            public readonly PersonProfile Person;
            public readonly decimal[] Remainders = new decimal[4];

            public Participant(PersonProfile person) => Person = person;
        }

        private readonly ISimulationTimeService _time;
        private readonly Dictionary<PersonProfile, Participant> _participants = new Dictionary<PersonProfile, Participant>();

        public PersonWellbeingRates Rates { get; }
        public PersonCareerSimulationRates CareerRates { get; }
        public int ParticipantCount => _participants.Count;

        public PersonWellbeingSimulation(ISimulationTimeService time, PersonWellbeingRates rates = null,
            PersonCareerSimulationRates careerRates = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            Rates = rates ?? new PersonWellbeingRates();
            CareerRates = careerRates ?? new PersonCareerSimulationRates();
            CareerRates.Validate();
            _time.OnMinutePassed += AdvanceMinute;
        }

        public bool Register(PersonProfile person)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (_participants.ContainsKey(person)) return false;
            _participants.Add(person, new Participant(person));
            return true;
        }

        public bool Unregister(PersonProfile person) => person != null && _participants.Remove(person);

        public void StartCareerTracking(PersonProfile person)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            Register(person);
            person.Career.SetWorkloadTracking(true, _time.CurrentTime.ToInstant().Seconds / 60, CareerRates);
        }

        public void StopCareerTracking(PersonProfile person)
        {
            if (person == null) return;
            person.Career.SetWorkloadTracking(false, 0, CareerRates);
        }

        private void AdvanceMinute(SimulationDateTime time)
        {
            long absoluteMinute = time.ToInstant().Seconds / 60;
            foreach (var participant in _participants.Values)
            {
                var wellbeing = participant.Person.Wellbeing;
                var activity = wellbeing.Activity;
                for (int dimensionIndex = 0; dimensionIndex < 4; dimensionIndex++)
                {
                    var dimension = (PersonWellbeingDimension)dimensionIndex;
                    decimal total = participant.Remainders[dimensionIndex] + Rates.Rate(activity, dimension);
                    int delta = (int)decimal.Truncate(total / 60m);
                    participant.Remainders[dimensionIndex] = total - delta * 60m;
                    if (delta == 0) continue;

                    int before = Value(wellbeing, dimension);
                    wellbeing.ApplyDelta(dimension, delta);
                    int after = Value(wellbeing, dimension);
                    if (delta > 0 && after == PersonWellbeing.MaximumValue ||
                        delta < 0 && after == PersonWellbeing.MinimumValue)
                        participant.Remainders[dimensionIndex] = 0m;
                    else if (before == after)
                        participant.Remainders[dimensionIndex] = 0m;
                }

                var career = participant.Person.Career;
                if (!career.IsWorkloadTracking) continue;
                career.RecordWorkMinute(absoluteMinute, activity == PersonWellbeingActivity.Working, CareerRates);
                career.AdvanceWorkloadEffects(wellbeing, career.WorkloadState, CareerRates);
                career.AdvanceRetention(wellbeing, CareerRates);
            }
        }

        private static int Value(PersonWellbeing wellbeing, PersonWellbeingDimension dimension)
        {
            return dimension switch
            {
                PersonWellbeingDimension.Energy => wellbeing.Energy,
                PersonWellbeingDimension.Stress => wellbeing.Stress,
                PersonWellbeingDimension.Boredom => wellbeing.Boredom,
                PersonWellbeingDimension.Mood => wellbeing.Mood,
                _ => throw new ArgumentOutOfRangeException(nameof(dimension))
            };
        }

        public void Dispose() => _time.OnMinutePassed -= AdvanceMinute;
    }
}
