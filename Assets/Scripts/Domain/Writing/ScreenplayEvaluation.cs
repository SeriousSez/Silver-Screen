using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    [Serializable]
    public sealed class ScreenplayEvaluation
    {
        public double OverallQuality { get; }
        public double CharacterDevelopment { get; }
        public double StructureCoherence { get; }
        public double Dialogue { get; }
        public double Pacing { get; }
        public double GenreExecution { get; }
        public double SceneVariety { get; }
        public double ThematicExecution { get; }
        public double DisplayRating => Math.Round(OverallQuality / 10d, 1,
            MidpointRounding.AwayFromZero);

        public ScreenplayEvaluation(double characterDevelopment, double structureCoherence,
            double dialogue, double pacing, double genreExecution, double sceneVariety,
            double thematicExecution)
        {
            CharacterDevelopment = Clamp(characterDevelopment);
            StructureCoherence = Clamp(structureCoherence);
            Dialogue = Clamp(dialogue);
            Pacing = Clamp(pacing);
            GenreExecution = Clamp(genreExecution);
            SceneVariety = Clamp(sceneVariety);
            ThematicExecution = Clamp(thematicExecution);
            OverallQuality = Clamp(
                CharacterDevelopment * 0.17d +
                StructureCoherence * 0.18d +
                Dialogue * 0.14d +
                Pacing * 0.15d +
                GenreExecution * 0.16d +
                SceneVariety * 0.10d +
                ThematicExecution * 0.10d);
        }

        private static double Clamp(double value) => Math.Clamp(value, 0d, 100d);
    }

    public sealed class ScreenplayEvaluator
    {
        private static readonly double[] CollaborationWeights = { 1d, 0.55d, 0.30d, 0.15d };
        private readonly IScreenplayTitleRandomSource _random;

        public ScreenplayEvaluator(IScreenplayTitleRandomSource random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        public ScreenplayEvaluation Evaluate(ScreenplayProject screenplay,
            IReadOnlyList<Employee> contributingWriters)
        {
            if (screenplay == null) throw new ArgumentNullException(nameof(screenplay));
            if (screenplay.Status != ScreenplayStatus.Completed ||
                screenplay.ContentStatus != ScreenplayContentStatus.Ready)
                throw new InvalidOperationException("Only a completed screenplay with finalized content can be evaluated.");
            if (contributingWriters == null || contributingWriters.Count == 0)
                throw new ArgumentException("At least one contributing Writer is required.", nameof(contributingWriters));

            double writerFoundation = CalculateWriterFoundation(screenplay, contributingWriters);
            double variationRange = Math.Clamp(24d - writerFoundation * 0.14d, 9d, 24d);
            ContentSignals signals = AnalyzeContent(screenplay);

            double Dimension(double contentSignal, double emphasis = 1d) =>
                Clamp(writerFoundation * 0.66d + contentSignal * 0.34d +
                      NextVariation(variationRange * emphasis));

            return new ScreenplayEvaluation(
                Dimension(signals.Characters, 1d),
                Dimension(signals.Structure, 0.8d),
                Dimension(signals.Dialogue, 1.05d),
                Dimension(signals.Pacing, 0.9d),
                Dimension(signals.Genre, 0.9d),
                Dimension(signals.Variety, 0.85d),
                Dimension(signals.Theme, 1d));
        }

        private double CalculateWriterFoundation(ScreenplayProject screenplay,
            IReadOnlyList<Employee> writers)
        {
            var contributions = new List<double>();
            foreach (Employee writer in writers)
            {
                if (writer == null || writer.Role != EmployeeRole.Writer) continue;
                double genreFit = 0d;
                foreach (string genreId in screenplay.GenreIds)
                    genreFit += writer.Person.Talent.GetGenreExperience(genreId);
                genreFit /= screenplay.GenreIds.Count;
                contributions.Add(writer.Skill * 0.78d + genreFit * 0.22d);
            }
            if (contributions.Count == 0)
                throw new InvalidOperationException("No eligible contributing Writers were available for evaluation.");

            contributions.Sort((left, right) => right.CompareTo(left));
            double weighted = 0d;
            double weightTotal = 0d;
            for (int i = 0; i < contributions.Count; i++)
            {
                double weight = CollaborationWeights[Math.Min(i, CollaborationWeights.Length - 1)];
                weighted += contributions[i] * weight;
                weightTotal += weight;
            }
            double collaborationBonus = Math.Min(6d, Math.Sqrt(contributions.Count - 1) * 3.5d);
            return Clamp(weighted / weightTotal + collaborationBonus);
        }

        private static ContentSignals AnalyzeContent(ScreenplayProject screenplay)
        {
            int protagonistCount = 0;
            int antagonistCount = 0;
            int supportingCount = 0;
            foreach (ScreenplayCharacter character in screenplay.Characters)
            {
                switch (character.Role)
                {
                    case ScreenplayCharacterRole.Protagonist: protagonistCount++; break;
                    case ScreenplayCharacterRole.Antagonist: antagonistCount++; break;
                    case ScreenplayCharacterRole.Supporting: supportingCount++; break;
                }
            }

            var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var participants = new HashSet<string>();
            var dialoguePerformers = new HashSet<string>();
            var locationTypes = new HashSet<ScreenplaySceneLocation>();
            var times = new HashSet<ScreenplayTimeOfDay>();
            int action = 0;
            int dialogue = 0;
            int reaction = 0;
            int totalBeats = 0;
            int scenesWithCompleteBeatMix = 0;

            foreach (ScreenplayScene scene in screenplay.Scenes)
            {
                locations.Add(scene.LocationId);
                locationTypes.Add(scene.LocationType);
                times.Add(scene.TimeOfDay);
                foreach (string characterId in scene.ParticipatingCharacterIds) participants.Add(characterId);
                bool hasAction = false;
                bool hasDialogue = false;
                bool hasReaction = false;
                foreach (ScreenplayBeat beat in scene.Beats)
                {
                    totalBeats++;
                    if (beat.BeatType == ScreenplayBeatType.Action) { action++; hasAction = true; }
                    else if (beat.BeatType == ScreenplayBeatType.Dialogue)
                    {
                        dialogue++;
                        hasDialogue = true;
                        if (beat.PerformingCharacterId != null) dialoguePerformers.Add(beat.PerformingCharacterId);
                    }
                    else { reaction++; hasReaction = true; }
                }
                if (hasAction && hasDialogue && hasReaction) scenesWithCompleteBeatMix++;
            }

            double characterUse = screenplay.Characters.Count == 0 ? 0d :
                participants.Count * 100d / screenplay.Characters.Count;
            double characters = 42d + protagonistCount * 12d + Math.Min(antagonistCount, 1) * 10d +
                                Math.Min(supportingCount, 2) * 6d + characterUse * 0.18d;
            double structure = 48d + Math.Min(screenplay.Scenes.Count, 7) * 5d +
                               (screenplay.Scenes.Count == 0 ? 0d :
                                   scenesWithCompleteBeatMix * 18d / screenplay.Scenes.Count);
            double dialogueSignal = 42d + RatioScore(dialogue, totalBeats, 0.25d, 0.50d) * 34d +
                                    Math.Min(dialoguePerformers.Count, 3) * 6d;
            double pacing = 44d + BalanceScore(action, dialogue, reaction) * 44d +
                            Math.Min(totalBeats / Math.Max(1, screenplay.Scenes.Count), 6) * 2d;
            double genre = 50d + Math.Min(screenplay.GenreIds.Count, 2) * 8d +
                           (!string.IsNullOrEmpty(screenplay.SettingId) ? 8d : 0d) +
                           (antagonistCount > 0 ? 7d : 0d);
            double variety = 38d + Math.Min(locations.Count, 5) * 8d +
                             Math.Min(locationTypes.Count, 2) * 8d + Math.Min(times.Count, 3) * 6d;
            double theme = 48d + (!string.IsNullOrEmpty(screenplay.ThemeId) ? 22d : 0d) +
                           Math.Min(reaction, screenplay.Scenes.Count) * 4d;
            return new ContentSignals(characters, structure, dialogueSignal, pacing, genre, variety, theme);
        }

        private double NextVariation(double range)
        {
            double normalized = _random.Next(-1000, 1001) / 1000d;
            return normalized * range;
        }

        private static double RatioScore(int value, int total, double idealMin, double idealMax)
        {
            if (total <= 0) return 0d;
            double ratio = value / (double)total;
            if (ratio >= idealMin && ratio <= idealMax) return 1d;
            double distance = ratio < idealMin ? idealMin - ratio : ratio - idealMax;
            return Math.Clamp(1d - distance * 2.5d, 0d, 1d);
        }

        private static double BalanceScore(int action, int dialogue, int reaction)
        {
            int total = action + dialogue + reaction;
            if (total == 0) return 0d;
            double maximum = Math.Max(action, Math.Max(dialogue, reaction));
            double minimum = Math.Min(action, Math.Min(dialogue, reaction));
            return 1d - (maximum - minimum) / total;
        }

        private static double Clamp(double value) => Math.Clamp(value, 0d, 100d);

        private readonly struct ContentSignals
        {
            public ContentSignals(double characters, double structure, double dialogue,
                double pacing, double genre, double variety, double theme)
            {
                Characters = characters;
                Structure = structure;
                Dialogue = dialogue;
                Pacing = pacing;
                Genre = genre;
                Variety = variety;
                Theme = theme;
            }

            public double Characters { get; }
            public double Structure { get; }
            public double Dialogue { get; }
            public double Pacing { get; }
            public double Genre { get; }
            public double Variety { get; }
            public double Theme { get; }
        }
    }
}
