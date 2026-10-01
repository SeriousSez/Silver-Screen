using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Performance;

namespace SilverScreen.Domain.Writing
{
    /// <summary>Immutable creative choices for one scene; no footage or production authority.</summary>
    public sealed class ScreenplaySceneAuthoring
    {
        public SceneTemplate Template { get; }
        public string TemplateId => Template.Id;
        public SceneDuration Duration { get; }
        public IReadOnlyList<CharacterRoleBinding> Bindings { get; }
        public SceneSetIntent Set { get; }
        public IReadOnlyList<AuthoredDialogue> Dialogue { get; }
        public IReadOnlyList<PropIntent> Props { get; }
        public IReadOnlyList<CostumeIntent> Costumes { get; }
        public IReadOnlyList<BackgroundGroup> BackgroundGroups { get; }
        public IReadOnlyList<PlannedSound> SoundEdits { get; }
        public string Direction { get; }
        public string Notes { get; }

        public ScreenplaySceneAuthoring(SceneTemplate template, SceneDuration duration,
            IEnumerable<CharacterRoleBinding> bindings, SceneSetIntent set,
            IEnumerable<AuthoredDialogue> dialogue = null, IEnumerable<PropIntent> props = null,
            IEnumerable<CostumeIntent> costumes = null, IEnumerable<BackgroundGroup> backgroundGroups = null,
            IEnumerable<PlannedSound> soundEdits = null, string direction = null, string notes = null)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            Template.ResolveBeats(duration); Duration = duration;
            Set = set ?? throw new ArgumentNullException(nameof(set));
            Bindings = TemplateValidation.Copy(bindings);
            var roles = TemplateValidation.Unique(Bindings.Select(binding => binding.RoleId));
            var characters = TemplateValidation.Unique(Bindings.Select(binding => binding.CharacterId));
            if (!roles.SetEquals(Template.Roles.Select(role => role.Id)))
                throw new ArgumentException("Map exactly one distinct screenplay character to every template role.");
            Dialogue = TemplateValidation.Copy(dialogue ?? Array.Empty<AuthoredDialogue>());
            Props = TemplateValidation.Copy(props ?? Array.Empty<PropIntent>());
            Costumes = TemplateValidation.Copy(costumes ?? Array.Empty<CostumeIntent>());
            BackgroundGroups = TemplateValidation.Copy(backgroundGroups ?? Array.Empty<BackgroundGroup>());
            SoundEdits = TemplateValidation.Copy(soundEdits ?? Array.Empty<PlannedSound>());
            Direction = direction?.Trim() ?? string.Empty; Notes = notes?.Trim() ?? string.Empty;
            TemplateValidation.Unique(Dialogue.Select(line => line.Id));
            foreach (var line in Dialogue)
            {
                ValidateBeatReference(line.Beat);
                var beat = Template.Beats.Single(item => item.Id == line.Beat.BeatId);
                if (Bindings.Single(binding => binding.RoleId == beat.RoleId).CharacterId != line.CharacterId)
                    throw new ArgumentException("Dialogue must belong to the character performing its beat.");
            }
            TemplateValidation.Unique(Props.Select(prop => prop.Id));
            TemplateValidation.Unique(Props.Where(prop => prop.RequirementId != null).Select(prop => prop.RequirementId));
            foreach (var prop in Props.Where(prop => prop.RequirementId != null))
            {
                var requirement = Template.Requirements.SingleOrDefault(item => item.Id == prop.RequirementId && item.Kind == SceneRequirementKind.Prop);
                if (requirement == null || prop.SemanticId != requirement.SemanticId || (requirement.Required && !prop.Enabled))
                    throw new ArgumentException("Preserve the template's required prop semantics.");
            }
            TemplateValidation.Unique(Costumes.Select(costume => costume.CharacterId));
            foreach (var costume in Costumes) TemplateValidation.Reference(costume.CharacterId, characters);
            TemplateValidation.Unique(BackgroundGroups.Select(group => group.Id));
            TemplateValidation.Unique(BackgroundGroups.SelectMany(group => group.SelectedPersonIds));
            SceneSoundPlanner.Validate(this);
        }

        internal void ValidateBeatReference(AuthoredBeatReference reference)
        {
            int maximum = Template.Variants.Max(variant => variant.BeatIds.Count(id => id == reference.BeatId));
            if (reference.Occurrence > maximum) throw new ArgumentException("Unknown beat occurrence: " + reference.Key);
        }

        public IReadOnlyList<AuthoredBeatReference> ResolveBeatReferences()
        {
            var counts = new Dictionary<string, int>();
            return Array.AsReadOnly(Template.ResolveBeats(Duration).Select(beat =>
            {
                counts.TryGetValue(beat.Id, out int count); counts[beat.Id] = ++count;
                return new AuthoredBeatReference(beat.Id, count);
            }).ToArray());
        }

        public ScenePerformance ResolvePerformance() => new ScenePerformance(Template, Duration,
            Bindings.Select(binding => new PerformerBinding(binding.RoleId, binding.CharacterId,
                Template.Roles.Single(role => role.Id == binding.RoleId).RequiredCapabilities)));

        public IReadOnlyList<PropIntent> ResolveProps()
        {
            var resolved = Template.Requirements.Where(requirement => requirement.Kind == SceneRequirementKind.Prop)
                .Select(requirement => Props.SingleOrDefault(prop => prop.RequirementId == requirement.Id)
                    ?? new PropIntent("requirement:" + requirement.Id, requirement.SemanticId, requirement.Id)).ToList();
            resolved.AddRange(Props.Where(prop => prop.RequirementId == null));
            return resolved.AsReadOnly();
        }

        public ScreenplaySceneAuthoring WithDuration(SceneDuration duration) => Copy(duration: duration);
        public ScreenplaySceneAuthoring WithDialogue(IEnumerable<AuthoredDialogue> dialogue) => Copy(dialogue: dialogue ?? throw new ArgumentNullException(nameof(dialogue)));
        public ScreenplaySceneAuthoring WithSound(PlannedSound sound)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            return Copy(soundEdits: SoundEdits.Where(item => item.Id != sound.Id).Concat(new[] { sound }));
        }
        public ScreenplaySceneAuthoring WithoutSoundEdit(string id) => Copy(soundEdits: SoundEdits.Where(sound => sound.Id != id));
        public ScreenplaySceneAuthoring WithProps(IEnumerable<PropIntent> props) => Copy(props: props ?? throw new ArgumentNullException(nameof(props)));
        public ScreenplaySceneAuthoring WithSet(SceneSetIntent set) => Copy(set: set ?? throw new ArgumentNullException(nameof(set)));
        private ScreenplaySceneAuthoring Copy(SceneDuration? duration = null, IEnumerable<AuthoredDialogue> dialogue = null,
            IEnumerable<PlannedSound> soundEdits = null, IEnumerable<PropIntent> props = null, SceneSetIntent set = null) =>
            new ScreenplaySceneAuthoring(Template, duration ?? Duration, Bindings, set ?? Set, dialogue ?? Dialogue,
                props ?? Props, Costumes, BackgroundGroups, soundEdits ?? SoundEdits, Direction, Notes);

        internal ScreenplayScene CreateScene(string id, int order, string title)
        {
            var scene = new ScreenplayScene(id, order, Set.SemanticId,
                Set.Selection.Kind == ContentSelectionKind.Location ? ScreenplaySceneLocation.Exterior : ScreenplaySceneLocation.Interior,
                ScreenplayTimeOfDay.Day, title, Set.SemanticId);
            foreach (var binding in Bindings) scene.AddCharacter(binding.CharacterId);
            var performance = ResolvePerformance(); var references = ResolveBeatReferences();
            for (int i = 0; i < performance.Beats.Count; i++)
            {
                var beat = performance.Beats[i]; var reference = references[i];
                var lines = Dialogue.Where(line => line.Beat.Key == reference.Key).ToArray();
                var type = lines.Length > 0 || beat.Definition.Action == PerformanceAction.Speak ? ScreenplayBeatType.Dialogue
                    : beat.Definition.Action == PerformanceAction.React ? ScreenplayBeatType.Reaction : ScreenplayBeatType.Action;
                scene.AddBeat(new ScreenplayBeat(id + ":" + reference.Key, i + 1, type,
                    lines.Length > 0 ? string.Join("\n", lines.Select(line => line.Text)) : beat.Definition.Direction,
                    beat.PerformerId, beat.TargetPerformerId, lines.FirstOrDefault()?.Emotion,
                    blockingIntentionId: beat.Definition.AnchorId));
            }
            scene.SetAuthoring(this);
            return scene;
        }
    }
}
