using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Performance
{
    public enum SceneDuration { Short, Medium, Long }
    public enum PerformanceAction { Approach, Speak, React, Interrupt, Gesture, Move, Exit }
    public enum SceneRequirementKind { Prop, Environment }
    public enum CameraFraming { Establishing, Wide, TwoShot, Medium, CloseUp, OverShoulder }

    // Definitions contain semantics and stable identities, never employees or Unity objects.
    public sealed class PerformerRoleSlot
    {
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<string> RequiredCapabilities { get; }
        public PerformerRoleSlot(string id, string name, IEnumerable<string> requiredCapabilities = null)
        {
            Id = TemplateValidation.Id(id);
            Name = TemplateValidation.Id(name);
            RequiredCapabilities = TemplateValidation.Ids(requiredCapabilities ?? Array.Empty<string>());
        }
    }

    public sealed class SceneRequirement
    {
        public string Id { get; }
        public SceneRequirementKind Kind { get; }
        public string SemanticId { get; }
        public bool Required { get; }
        public SceneRequirement(string id, SceneRequirementKind kind, string semanticId, bool required = true)
        {
            Id = TemplateValidation.Id(id);
            TemplateValidation.EnumValue(kind);
            Kind = kind;
            SemanticId = TemplateValidation.Id(semanticId);
            Required = required;
        }
    }

    public sealed class InteractionAnchor
    {
        public string Id { get; }
        public string SemanticId { get; }
        public string RequirementId { get; }
        public InteractionAnchor(string id, string semanticId, string requirementId = null)
        {
            Id = TemplateValidation.Id(id);
            SemanticId = TemplateValidation.Id(semanticId);
            RequirementId = requirementId == null ? null : TemplateValidation.Id(requirementId);
        }
    }

    public sealed class CameraSuggestion
    {
        public CameraFraming Framing { get; }
        public string SubjectRoleId { get; }
        public string OtherRoleId { get; }
        public CameraSuggestion(CameraFraming framing, string subjectRoleId = null, string otherRoleId = null)
        {
            TemplateValidation.EnumValue(framing);
            Framing = framing;
            SubjectRoleId = subjectRoleId == null ? null : TemplateValidation.Id(subjectRoleId);
            OtherRoleId = otherRoleId == null ? null : TemplateValidation.Id(otherRoleId);
            if ((framing == CameraFraming.CloseUp || framing == CameraFraming.OverShoulder) && SubjectRoleId == null)
                throw new ArgumentException("This camera suggestion needs a subject role.");
            if (framing == CameraFraming.OverShoulder && (OtherRoleId == null || OtherRoleId == SubjectRoleId))
                throw new ArgumentException("Over-shoulder suggestions need two distinct roles.");
        }
    }

    public sealed class PerformanceBeat
    {
        public string Id { get; }
        public PerformanceAction Action { get; }
        public string Direction { get; }
        public string RoleId { get; }
        public string TargetRoleId { get; }
        public string AnchorId { get; }
        public CameraSuggestion Camera { get; }
        public IReadOnlyList<SemanticSoundCue> SoundCues { get; }
        public PerformanceBeat(string id, PerformanceAction action, string direction, string roleId,
            string targetRoleId = null, string anchorId = null, CameraSuggestion camera = null,
            IEnumerable<SemanticSoundCue> soundCues = null)
        {
            Id = TemplateValidation.Id(id);
            TemplateValidation.EnumValue(action);
            Action = action;
            Direction = TemplateValidation.Id(direction);
            RoleId = TemplateValidation.Id(roleId);
            TargetRoleId = targetRoleId == null ? null : TemplateValidation.Id(targetRoleId);
            AnchorId = anchorId == null ? null : TemplateValidation.Id(anchorId);
            Camera = camera;
            SoundCues = TemplateValidation.Copy(soundCues ?? Array.Empty<SemanticSoundCue>());
            TemplateValidation.Unique(SoundCues.Select(cue => cue.Id));
        }
    }

    public sealed class SceneTemplateVariant
    {
        public SceneDuration Duration { get; }
        public IReadOnlyList<string> BeatIds { get; }
        public SceneTemplateVariant(SceneDuration duration, IEnumerable<string> beatIds)
        {
            TemplateValidation.EnumValue(duration);
            Duration = duration;
            BeatIds = TemplateValidation.Ids(beatIds);
            if (BeatIds.Count == 0) throw new ArgumentException("A variant needs an ordered beat sequence.");
        }
    }

    public sealed class SceneTemplate
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyList<PerformerRoleSlot> Roles { get; }
        public IReadOnlyList<PerformanceBeat> Beats { get; }
        public IReadOnlyList<SceneTemplateVariant> Variants { get; }
        public IReadOnlyList<SceneRequirement> Requirements { get; }
        public IReadOnlyList<InteractionAnchor> Anchors { get; }

        public SceneTemplate(string id, string name, string description,
            IEnumerable<PerformerRoleSlot> roles, IEnumerable<PerformanceBeat> beats,
            IEnumerable<SceneTemplateVariant> variants, IEnumerable<string> tags = null,
            IEnumerable<SceneRequirement> requirements = null, IEnumerable<InteractionAnchor> anchors = null)
        {
            Id = TemplateValidation.Id(id);
            Name = TemplateValidation.Id(name);
            Description = description?.Trim() ?? string.Empty;
            Tags = TemplateValidation.Ids(tags ?? Array.Empty<string>());
            Roles = TemplateValidation.Copy(roles);
            Beats = TemplateValidation.Copy(beats);
            Variants = TemplateValidation.Copy(variants);
            Requirements = TemplateValidation.Copy(requirements ?? Array.Empty<SceneRequirement>());
            Anchors = TemplateValidation.Copy(anchors ?? Array.Empty<InteractionAnchor>());
            var roleIds = TemplateValidation.Unique(Roles.Select(role => role.Id));
            var beatIds = TemplateValidation.Unique(Beats.Select(beat => beat.Id));
            var requirementIds = TemplateValidation.Unique(Requirements.Select(item => item.Id));
            var anchorIds = TemplateValidation.Unique(Anchors.Select(item => item.Id));
            if (Roles.Count == 0 || Beats.Count == 0) throw new ArgumentException("Roles and beats are required.");
            if (Variants.Count != 3 || Variants.Select(variant => variant.Duration).Distinct().Count() != 3)
                throw new ArgumentException("Author exactly one Short, Medium and Long sequence.");
            foreach (var anchor in Anchors) TemplateValidation.Reference(anchor.RequirementId, requirementIds);
            foreach (var beat in Beats)
            {
                TemplateValidation.Reference(beat.RoleId, roleIds);
                TemplateValidation.Reference(beat.TargetRoleId, roleIds);
                TemplateValidation.Reference(beat.AnchorId, anchorIds);
                TemplateValidation.Reference(beat.Camera?.SubjectRoleId, roleIds);
                TemplateValidation.Reference(beat.Camera?.OtherRoleId, roleIds);
            }
            foreach (var variant in Variants)
                foreach (var beatId in variant.BeatIds) TemplateValidation.Reference(beatId, beatIds);
        }

        public IReadOnlyList<PerformanceBeat> ResolveBeats(SceneDuration duration)
        {
            TemplateValidation.EnumValue(duration);
            var variant = Variants.Single(item => item.Duration == duration);
            return Array.AsReadOnly(variant.BeatIds.Select(id => Beats.Single(beat => beat.Id == id)).ToArray());
        }
    }

    internal static class TemplateValidation
    {
        internal static string Id(string id) => !string.IsNullOrWhiteSpace(id) ? id.Trim()
            : throw new ArgumentException("A nonblank stable identifier or name is required.");
        internal static void EnumValue<T>(T value) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(nameof(value));
        }
        internal static IReadOnlyList<T> Copy<T>(IEnumerable<T> items) where T : class
        {
            var copy = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
            if (copy.Any(item => item == null)) throw new ArgumentException("Null definition entries are invalid.");
            return Array.AsReadOnly(copy);
        }
        internal static IReadOnlyList<string> Ids(IEnumerable<string> ids) =>
            Array.AsReadOnly((ids ?? throw new ArgumentNullException(nameof(ids))).Select(Id).ToArray());
        internal static HashSet<string> Unique(IEnumerable<string> ids)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (!result.Add(id)) throw new ArgumentException("Duplicate definition ID: " + id);
            return result;
        }
        internal static void Reference(string id, HashSet<string> known)
        {
            if (id != null && !known.Contains(id)) throw new ArgumentException("Unknown definition reference: " + id);
        }
    }
}
