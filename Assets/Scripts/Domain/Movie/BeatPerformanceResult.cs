using System;
using SilverScreen.Domain.Writing;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public interface IPerformanceRandomSource
    {
        double NextDouble();
    }

    public sealed class SystemPerformanceRandomSource : IPerformanceRandomSource
    {
        private readonly Random _random;

        public SystemPerformanceRandomSource(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public double NextDouble() => _random.NextDouble();
    }

    public sealed class BeatPerformanceResult
    {
        public string ScreenplayBeatId { get; }
        public string PerformingCharacterId { get; }
        public string ActorPersonId { get; }
        public ScreenplayEmotion IntendedEmotion { get; }
        public ScreenplayEmotion DeliveredEmotion { get; }
        public double IntendedIntensity { get; }
        public double DeliveredIntensity { get; }
        public double Confidence { get; }
        public double Timing { get; }
        public double Expressiveness { get; }

        public BeatPerformanceResult(
            string screenplayBeatId, string performingCharacterId, string actorPersonId,
            ScreenplayEmotion intendedEmotion, ScreenplayEmotion deliveredEmotion,
            double intendedIntensity, double deliveredIntensity, double confidence,
            double timing, double expressiveness)
        {
            if (string.IsNullOrWhiteSpace(screenplayBeatId))
                throw new ArgumentException("A performance result requires a screenplay beat.", nameof(screenplayBeatId));
            if (string.IsNullOrWhiteSpace(performingCharacterId))
                throw new ArgumentException("A performance result requires a fictional character.", nameof(performingCharacterId));
            if (string.IsNullOrWhiteSpace(actorPersonId))
                throw new ArgumentException("A performance result requires an actor.", nameof(actorPersonId));

            ScreenplayBeatId = screenplayBeatId.Trim();
            PerformingCharacterId = performingCharacterId.Trim();
            ActorPersonId = actorPersonId.Trim();
            IntendedEmotion = intendedEmotion;
            DeliveredEmotion = deliveredEmotion;
            IntendedIntensity = Clamp01(intendedIntensity);
            DeliveredIntensity = Clamp01(deliveredIntensity);
            Confidence = Clamp01(confidence);
            Timing = Clamp01(timing);
            Expressiveness = Clamp01(expressiveness);
        }

        private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
    }

    public sealed class BeatPerformanceGenerator
    {
        private static readonly IReadOnlyDictionary<ScreenplayEmotion, ScreenplayEmotion[]> EmotionDrift =
            new Dictionary<ScreenplayEmotion, ScreenplayEmotion[]>
            {
                [ScreenplayEmotion.Neutral] = new[] { ScreenplayEmotion.Confident, ScreenplayEmotion.Nervous },
                [ScreenplayEmotion.Happy] = new[] { ScreenplayEmotion.Confident, ScreenplayEmotion.Nervous },
                [ScreenplayEmotion.Sad] = new[] { ScreenplayEmotion.Afraid, ScreenplayEmotion.Neutral },
                [ScreenplayEmotion.Angry] = new[] { ScreenplayEmotion.Nervous, ScreenplayEmotion.Afraid, ScreenplayEmotion.Confident },
                [ScreenplayEmotion.Afraid] = new[] { ScreenplayEmotion.Nervous, ScreenplayEmotion.Sad },
                [ScreenplayEmotion.Romantic] = new[] { ScreenplayEmotion.Nervous, ScreenplayEmotion.Happy },
                [ScreenplayEmotion.Confident] = new[] { ScreenplayEmotion.Angry, ScreenplayEmotion.Nervous },
                [ScreenplayEmotion.Nervous] = new[] { ScreenplayEmotion.Afraid, ScreenplayEmotion.Neutral }
            };

        private readonly IPerformanceRandomSource _random;

        public BeatPerformanceGenerator(IPerformanceRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public BeatPerformanceResult Generate(ScreenplayBeat beat, Employee actor, Employee director, string genreId)
        {
            if (beat == null) throw new ArgumentNullException(nameof(beat));
            if (actor == null) throw new ArgumentNullException(nameof(actor));

            double ability = actor.Person.Talent.ActingAbility / 100d;
            double experience = actor.Person.Talent.GetGenreExperience(genreId) / 100d;
            double direction = director?.Person.Talent.DirectingAbility / 100d ?? 0d;
            double consistency = Clamp01(0.12d + ability * 0.53d + experience * 0.15d + direction * 0.20d);
            double driftRange = Lerp(0.42d, 0.06d, consistency);
            bool inspired = _random.NextDouble() < Lerp(0.12d, 0.04d, consistency);

            double deliveredIntensity = Clamp01(beat.IntendedIntensity + SignedVariation() * driftRange);
            double confidence = Clamp01(0.18d + ability * 0.62d + experience * 0.15d + SignedVariation() * driftRange);
            double timing = Clamp01(0.35d + ability * 0.43d + experience * 0.17d + SignedVariation() * driftRange);
            double expressiveness = Clamp01(0.18d + ability * 0.42d + beat.IntendedIntensity * 0.20d + SignedVariation() * driftRange);

            if (inspired)
            {
                deliveredIntensity = Lerp(deliveredIntensity, beat.IntendedIntensity, 0.75d);
                confidence = Math.Max(confidence, 0.72d + _random.NextDouble() * 0.18d);
                timing = Math.Max(timing, 0.72d + _random.NextDouble() * 0.18d);
                expressiveness = Math.Max(expressiveness, 0.70d + _random.NextDouble() * 0.22d);
            }

            ScreenplayEmotion intendedEmotion = beat.EmotionalIntention ?? ScreenplayEmotion.Neutral;
            return new BeatPerformanceResult(
                beat.Id, beat.PerformingCharacterId, actor.Id, intendedEmotion,
                SelectDeliveredEmotion(intendedEmotion, consistency, inspired),
                beat.IntendedIntensity, deliveredIntensity, confidence, timing, expressiveness);
        }

        private ScreenplayEmotion SelectDeliveredEmotion(ScreenplayEmotion intended, double consistency, bool inspired)
        {
            double mismatchChance = inspired ? 0d : 0.38d * (1d - consistency) * (1d - consistency);
            if (_random.NextDouble() >= mismatchChance || !EmotionDrift.TryGetValue(intended, out var alternatives))
                return intended;

            int index = Math.Min((int)(_random.NextDouble() * alternatives.Length), alternatives.Length - 1);
            return alternatives[index];
        }

        private double SignedVariation() => _random.NextDouble() + _random.NextDouble() - 1d;
        private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
        private static double Lerp(double from, double to, double amount) => from + (to - from) * Clamp01(amount);
    }
}
