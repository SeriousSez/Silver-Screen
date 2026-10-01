using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain
{
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
        public int ParticipantCount => _participants.Count;

        public PersonWellbeingSimulation(ISimulationTimeService time, PersonWellbeingRates rates = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            Rates = rates ?? new PersonWellbeingRates();
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

        private void AdvanceMinute(SimulationDateTime unused)
        {
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
