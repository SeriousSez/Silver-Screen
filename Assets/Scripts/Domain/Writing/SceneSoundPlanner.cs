using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Performance;

namespace SilverScreen.Domain.Writing
{
    public sealed class SoundSettings
    {
        public bool Enabled { get; }
        public string AssetReferenceId { get; }
        public double Volume { get; }
        public double TimingOffsetSeconds { get; }
        public double FadeInSeconds { get; }
        public double FadeOutSeconds { get; }
        public SoundSpatialIntent SpatialIntent { get; }
        public SoundSettings(bool enabled = true, string assetReferenceId = null, double volume = 1,
            double timingOffsetSeconds = 0, double fadeInSeconds = 0, double fadeOutSeconds = 0,
            SoundSpatialIntent spatialIntent = SoundSpatialIntent.SourceCharacter)
        {
            ValidateNumber(volume, 0, 1); ValidateNumber(timingOffsetSeconds, -600, 600);
            ValidateNumber(fadeInSeconds, 0, 60); ValidateNumber(fadeOutSeconds, 0, 60);
            TemplateValidation.EnumValue(spatialIntent);
            Enabled = enabled; AssetReferenceId = assetReferenceId == null ? null : TemplateValidation.Id(assetReferenceId);
            Volume = volume; TimingOffsetSeconds = timingOffsetSeconds;
            FadeInSeconds = fadeInSeconds; FadeOutSeconds = fadeOutSeconds; SpatialIntent = spatialIntent;
        }
        private static void ValidateNumber(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    public sealed class PlannedSound
    {
        public string Id { get; }
        public AuthoredBeatReference Beat { get; }
        public string SemanticType { get; }
        public string TimingEvent { get; }
        public bool Automatic { get; }
        public bool Optional { get; }
        public SoundSettings Settings { get; }
        private PlannedSound(string id, AuthoredBeatReference beat, string semanticType,
            string timingEvent, bool automatic, bool optional, SoundSettings settings)
        {
            Id = TemplateValidation.Id(id); Beat = beat ?? throw new ArgumentNullException(nameof(beat));
            SemanticType = TemplateValidation.Id(semanticType); TimingEvent = TemplateValidation.Id(timingEvent);
            Automatic = automatic; Optional = optional; Settings = settings ?? new SoundSettings();
            if (!optional && !Settings.Enabled) throw new ArgumentException("A required cue cannot be disabled.");
        }
        internal static PlannedSound FromCue(AuthoredBeatReference beat, SemanticSoundCue cue) =>
            new PlannedSound("auto:" + beat.Key + ":" + cue.Id, beat, cue.SemanticType, cue.TimingEvent, true, cue.Optional, null);
        public static PlannedSound Custom(string id, AuthoredBeatReference beat, string semanticType,
            SoundSettings settings = null, string timingEvent = "beat-start") =>
            new PlannedSound("custom:" + TemplateValidation.Id(id), beat, semanticType, timingEvent, false, true, settings);
        public PlannedSound WithSettings(SoundSettings settings) =>
            new PlannedSound(Id, Beat, SemanticType, TimingEvent, Automatic, Optional,
                settings ?? throw new ArgumentNullException(nameof(settings)));
    }

    public static class SceneSoundPlanner
    {
        internal static IReadOnlyList<PlannedSound> Defaults(SceneTemplate template, SceneDuration duration)
        {
            var result = new List<PlannedSound>();
            var occurrences = new Dictionary<string, int>();
            foreach (var beat in template.ResolveBeats(duration))
            {
                occurrences.TryGetValue(beat.Id, out int count); occurrences[beat.Id] = ++count;
                var reference = new AuthoredBeatReference(beat.Id, count);
                foreach (var cue in beat.SoundCues) result.Add(PlannedSound.FromCue(reference, cue));
                if ((beat.Action == PerformanceAction.Approach || beat.Action == PerformanceAction.Move || beat.Action == PerformanceAction.Exit)
                    && !beat.SoundCues.Any(cue => cue.SemanticType == "Footsteps"))
                    result.Add(PlannedSound.FromCue(reference, new SemanticSoundCue("movement-footsteps", "Footsteps", "movement")));
            }
            TemplateValidation.Unique(result.Select(sound => sound.Id));
            return result.AsReadOnly();
        }

        public static IReadOnlyList<PlannedSound> Resolve(ScreenplaySceneAuthoring plan)
        {
            var sounds = Defaults(plan.Template, plan.Duration).ToDictionary(sound => sound.Id);
            var active = plan.ResolveBeatReferences().Select(beat => beat.Key).ToHashSet();
            foreach (var edit in plan.SoundEdits)
                if (active.Contains(edit.Beat.Key)) sounds[edit.Id] = edit;
            // Keep cue order deterministic: beat sequence, then stable event ID.
            var order = plan.ResolveBeatReferences().Select((beat, i) => (beat.Key, i)).ToDictionary(item => item.Key, item => item.i);
            return Array.AsReadOnly(sounds.Values.OrderBy(sound => order[sound.Beat.Key]).ThenBy(sound => sound.Id, StringComparer.Ordinal).ToArray());
        }

        internal static void Validate(ScreenplaySceneAuthoring plan)
        {
            var defaults = Enum.GetValues(typeof(SceneDuration)).Cast<SceneDuration>()
                .SelectMany(duration => Defaults(plan.Template, duration)).GroupBy(sound => sound.Id)
                .ToDictionary(group => group.Key, group => group.First());
            TemplateValidation.Unique(plan.SoundEdits.Select(sound => sound.Id));
            foreach (var sound in plan.SoundEdits)
            {
                plan.ValidateBeatReference(sound.Beat);
                if (sound.Automatic && (!defaults.TryGetValue(sound.Id, out var original)
                    || original.Beat.Key != sound.Beat.Key || original.SemanticType != sound.SemanticType
                    || original.TimingEvent != sound.TimingEvent || original.Optional != sound.Optional))
                    throw new ArgumentException("Sound override does not belong to the selected template.");
            }
        }
    }
}
