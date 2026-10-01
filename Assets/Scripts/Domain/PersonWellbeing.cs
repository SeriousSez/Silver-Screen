using System;

namespace SilverScreen.Domain
{
    public enum PersonWellbeingActivity
    {
        Idle,
        Working,
        Resting,
        Socializing,
        Recreation
    }

    public enum PersonWellbeingDimension
    {
        Energy,
        Stress,
        Boredom,
        Mood
    }

    [Serializable]
    public sealed class PersonWellbeingSnapshot
    {
        public int Energy = PersonWellbeing.DefaultEnergy;
        public int Stress = PersonWellbeing.DefaultStress;
        public int Boredom = PersonWellbeing.DefaultBoredom;
        public int Mood = PersonWellbeing.DefaultMood;
        public PersonWellbeingActivity Activity = PersonWellbeingActivity.Idle;
    }

    [Serializable]
    public sealed class PersonWellbeing
    {
        public const int MinimumValue = 0;
        public const int MaximumValue = 100;
        public const int DefaultEnergy = 80;
        public const int DefaultStress = 10;
        public const int DefaultBoredom = 10;
        public const int DefaultMood = 70;

        public int Energy { get; private set; } = DefaultEnergy;
        public int Stress { get; private set; } = DefaultStress;
        public int Boredom { get; private set; } = DefaultBoredom;
        public int Mood { get; private set; } = DefaultMood;
        public PersonWellbeingActivity Activity { get; private set; } = PersonWellbeingActivity.Idle;

        public void SetActivity(PersonWellbeingActivity activity)
        {
            if (!Enum.IsDefined(typeof(PersonWellbeingActivity), activity))
                throw new ArgumentOutOfRangeException(nameof(activity), activity, "Unknown wellbeing activity.");
            Activity = activity;
        }

        public void ApplyDelta(PersonWellbeingDimension dimension, int delta)
        {
            switch (dimension)
            {
                case PersonWellbeingDimension.Energy:
                    Energy = ClampDelta(Energy, delta);
                    break;
                case PersonWellbeingDimension.Stress:
                    Stress = ClampDelta(Stress, delta);
                    break;
                case PersonWellbeingDimension.Boredom:
                    Boredom = ClampDelta(Boredom, delta);
                    break;
                case PersonWellbeingDimension.Mood:
                    Mood = ClampDelta(Mood, delta);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown wellbeing dimension.");
            }
        }

        public PersonWellbeingSnapshot Capture() => new PersonWellbeingSnapshot
        {
            Energy = Energy,
            Stress = Stress,
            Boredom = Boredom,
            Mood = Mood,
            Activity = Activity
        };

        public void Restore(PersonWellbeingSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!Enum.IsDefined(typeof(PersonWellbeingActivity), snapshot.Activity))
                throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Activity, "Unknown wellbeing activity.");
            Energy = Clamp(snapshot.Energy);
            Stress = Clamp(snapshot.Stress);
            Boredom = Clamp(snapshot.Boredom);
            Mood = Clamp(snapshot.Mood);
            SetActivity(snapshot.Activity);
        }

        private static int ClampDelta(int current, int delta) => Clamp((long)current + delta);
        private static int Clamp(long value) => (int)Math.Clamp(value, MinimumValue, MaximumValue);
    }
}
