using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Performance;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public sealed class ProductionRoleBinding
    {
        public string TemplateRoleId { get; }
        public string SourceCharacterId { get; }
        public string MovieRoleId { get; }
        internal ProductionRoleBinding(CharacterRoleBinding source, IReadOnlyDictionary<string, string> roles)
        { TemplateRoleId = source.RoleId; SourceCharacterId = source.CharacterId; MovieRoleId = roles[source.CharacterId]; }
    }

    public sealed class ProductionBeatIntent
    {
        public AuthoredBeatReference SourceBeat { get; }
        public PerformanceAction Action { get; }
        public string Direction { get; }
        public string PerformerRoleId { get; }
        public string TargetRoleId { get; }
        public string AnchorId { get; }
        public CameraSuggestion Camera { get; }
        internal ProductionBeatIntent(AuthoredBeatReference reference, ResolvedPerformanceBeat beat, IReadOnlyDictionary<string, string> roles)
        {
            SourceBeat = reference; Action = beat.Definition.Action; Direction = beat.Definition.Direction;
            PerformerRoleId = roles[beat.PerformerId];
            TargetRoleId = beat.TargetPerformerId == null ? null : roles[beat.TargetPerformerId];
            AnchorId = beat.Definition.AnchorId; Camera = beat.Definition.Camera;
        }
    }

    /// <summary>Resolved greenlight snapshot. Shared leaf values are immutable; every collection is freshly owned.</summary>
    public sealed class ProductionSceneAuthoring
    {
        public string SourceSceneId { get; }
        public string TemplateId { get; }
        public SceneDuration Duration { get; }
        public IReadOnlyList<ProductionRoleBinding> Bindings { get; }
        public IReadOnlyList<ProductionBeatIntent> Beats { get; }
        public IReadOnlyList<AuthoredDialogue> Dialogue { get; }
        public IReadOnlyList<PlannedSound> Sounds { get; }
        public IReadOnlyList<PropIntent> Props { get; }
        public IReadOnlyList<CostumeIntent> Costumes { get; }
        public IReadOnlyList<BackgroundGroup> BackgroundGroups { get; }
        public IReadOnlyList<SceneRequirement> Requirements { get; }
        public SceneSetIntent Set { get; }
        public string Direction { get; }
        public string Notes { get; }
        internal ProductionSceneAuthoring(ScreenplayScene source, IReadOnlyDictionary<string, string> roles)
        {
            var plan = source.Authoring; SourceSceneId = source.Id; TemplateId = plan.TemplateId; Duration = plan.Duration;
            Bindings = Array.AsReadOnly(plan.Bindings.Select(binding => new ProductionRoleBinding(binding, roles)).ToArray());
            var refs = plan.ResolveBeatReferences(); var performance = plan.ResolvePerformance();
            Beats = Array.AsReadOnly(performance.Beats.Select((beat, i) => new ProductionBeatIntent(refs[i], beat, roles)).ToArray());
            var active = refs.Select(reference => reference.Key).ToHashSet();
            Dialogue = Array.AsReadOnly(plan.Dialogue.Where(line => active.Contains(line.Beat.Key)).ToArray());
            Sounds = Array.AsReadOnly(SceneSoundPlanner.Resolve(plan).ToArray());
            Props = Array.AsReadOnly(plan.ResolveProps().ToArray()); Costumes = Array.AsReadOnly(plan.Costumes.ToArray());
            BackgroundGroups = Array.AsReadOnly(plan.BackgroundGroups.ToArray());
            Requirements = Array.AsReadOnly(plan.Template.Requirements.ToArray());
            Set = plan.Set; Direction = plan.Direction; Notes = plan.Notes;
        }
    }

    public sealed class ProductionAuthoringIntent
    {
        public string IntendedDirectorId { get; }
        public IReadOnlyList<IntendedPerformer> IntendedActors { get; }
        internal ProductionAuthoringIntent(ScreenplayAuthoring source)
        {
            IntendedDirectorId = source.IntendedDirectorId;
            IntendedActors = Array.AsReadOnly(source.IntendedActors.ToArray());
        }
    }
}
