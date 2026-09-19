using System;

namespace SilverScreen.Domain.Movie
{
    public enum ScreenplayBeatType
    {
        Action,
        Dialogue,
        Reaction
    }

    public enum ScreenplayEmotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Afraid,
        Romantic,
        Confident,
        Nervous
    }

    public sealed class ScreenplayBeat
    {
        public string Id { get; }
        public int Order { get; private set; }
        public ScreenplayBeatType BeatType { get; }
        public string PerformingCharacterId { get; }
        public string TargetCharacterId { get; }
        public string Content { get; }
        public ScreenplayEmotion? EmotionalIntention { get; }
        public string BlockingTargetId { get; }
        public double IntendedIntensity { get; }

        public ScreenplayBeat(
            string id,
            int order,
            ScreenplayBeatType beatType,
            string content,
            string performingCharacterId = null,
            string targetCharacterId = null,
            ScreenplayEmotion? emotionalIntention = null,
            string blockingTargetId = null,
            double intendedIntensity = 0.6d)
        {
            if (order < 1) throw new ArgumentOutOfRangeException(nameof(order));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("A screenplay beat requires direction or content.", nameof(content));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            Order = order;
            BeatType = beatType;
            PerformingCharacterId = NormalizeOptionalId(performingCharacterId);
            TargetCharacterId = NormalizeOptionalId(targetCharacterId);
            Content = content.Trim();
            EmotionalIntention = emotionalIntention;
            BlockingTargetId = NormalizeOptionalId(blockingTargetId);
            IntendedIntensity = Math.Clamp(intendedIntensity, 0d, 1d);
        }

        internal void SetOrder(int order)
        {
            Order = order;
        }

        private static string NormalizeOptionalId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
