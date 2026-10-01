using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Performance;

namespace SilverScreen.Domain.Writing
{
    public enum ContentSelectionKind { Automatic, OwnedSet, SavedSet, Location, Content }
    public enum SoundSpatialIntent { SourceCharacter, Scene, NonSpatial }

    /// <summary>Semantic intent survives when the optional physical/content reference is unavailable.</summary>
    public sealed class ContentSelection
    {
        public ContentSelectionKind Kind { get; }
        public string ReferenceId { get; }
        public ContentSelection(ContentSelectionKind kind = ContentSelectionKind.Automatic, string referenceId = null)
        {
            TemplateValidation.EnumValue(kind);
            if (kind == ContentSelectionKind.Automatic && referenceId != null)
                throw new ArgumentException("Automatic selection cannot name a physical asset.");
            Kind = kind;
            ReferenceId = kind == ContentSelectionKind.Automatic ? null : TemplateValidation.Id(referenceId);
        }
    }

    public sealed class SceneSetIntent
    {
        public string SemanticId { get; }
        public ContentSelection Selection { get; }
        public SceneSetIntent(string semanticId, ContentSelection selection = null)
        {
            SemanticId = TemplateValidation.Id(semanticId);
            Selection = selection ?? new ContentSelection();
            if (Selection.Kind == ContentSelectionKind.Content) throw new ArgumentException("Use a set or location selection.");
        }
    }

    public sealed class PropIntent
    {
        public string Id { get; }
        public string RequirementId { get; }
        public string SemanticId { get; }
        public ContentSelection Selection { get; }
        public bool Enabled { get; }
        public PropIntent(string id, string semanticId, string requirementId = null,
            ContentSelection selection = null, bool enabled = true)
        {
            Id = TemplateValidation.Id(id);
            SemanticId = TemplateValidation.Id(semanticId);
            RequirementId = requirementId == null ? null : TemplateValidation.Id(requirementId);
            Selection = selection ?? new ContentSelection();
            if (Selection.Kind != ContentSelectionKind.Automatic && Selection.Kind != ContentSelectionKind.Content)
                throw new ArgumentException("A prop needs an automatic or content selection.");
            Enabled = enabled;
        }
    }

    public sealed class CostumeIntent
    {
        public string CharacterId { get; }
        public string SemanticId { get; }
        public string WardrobeReferenceId { get; }
        public bool Automatic => WardrobeReferenceId == null;
        public CostumeIntent(string characterId, string semanticId = null, string wardrobeReferenceId = null)
        {
            CharacterId = TemplateValidation.Id(characterId);
            SemanticId = semanticId == null ? null : TemplateValidation.Id(semanticId);
            WardrobeReferenceId = wardrobeReferenceId == null ? null : TemplateValidation.Id(wardrobeReferenceId);
        }
    }

    public sealed class BackgroundGroup
    {
        public string Id { get; }
        public string SemanticId { get; }
        public int MinimumCount { get; }
        public int MaximumCount { get; }
        public bool AllowAutomaticPopulation { get; }
        public IReadOnlyList<string> SelectedPersonIds { get; }
        public string CategoryRequirement { get; }
        public string WardrobeIntent { get; }
        public string BehaviorIntent { get; }
        public BackgroundGroup(string id, string semanticId, int minimumCount, int maximumCount,
            bool allowAutomaticPopulation = true, IEnumerable<string> selectedPersonIds = null,
            string categoryRequirement = null, string wardrobeIntent = null, string behaviorIntent = null)
        {
            Id = TemplateValidation.Id(id); SemanticId = TemplateValidation.Id(semanticId);
            if (minimumCount < 0 || maximumCount < minimumCount) throw new ArgumentOutOfRangeException(nameof(minimumCount));
            MinimumCount = minimumCount; MaximumCount = maximumCount;
            SelectedPersonIds = TemplateValidation.Ids(selectedPersonIds ?? Array.Empty<string>());
            TemplateValidation.Unique(SelectedPersonIds);
            if (SelectedPersonIds.Count > maximumCount || (!allowAutomaticPopulation && SelectedPersonIds.Count < minimumCount))
                throw new ArgumentException("Selected people do not meet the population bounds.");
            AllowAutomaticPopulation = allowAutomaticPopulation;
            CategoryRequirement = categoryRequirement?.Trim() ?? string.Empty;
            WardrobeIntent = wardrobeIntent?.Trim() ?? string.Empty;
            BehaviorIntent = behaviorIntent?.Trim() ?? string.Empty;
        }
    }

    public sealed class CharacterRoleBinding
    {
        public string RoleId { get; }
        public string CharacterId { get; }
        public CharacterRoleBinding(string roleId, string characterId)
        { RoleId = TemplateValidation.Id(roleId); CharacterId = TemplateValidation.Id(characterId); }
    }

    /// <summary>Occurrence distinguishes repeated uses of the same reusable beat.</summary>
    public sealed class AuthoredBeatReference
    {
        public string BeatId { get; }
        public int Occurrence { get; }
        public string Key => BeatId + "#" + Occurrence;
        public AuthoredBeatReference(string beatId, int occurrence = 1)
        {
            BeatId = TemplateValidation.Id(beatId);
            if (occurrence < 1) throw new ArgumentOutOfRangeException(nameof(occurrence));
            Occurrence = occurrence;
        }
    }

    public sealed class AuthoredDialogue
    {
        public string Id { get; }
        public AuthoredBeatReference Beat { get; }
        public string CharacterId { get; }
        public string Text { get; }
        public ScreenplayEmotion? Emotion { get; }
        public string DeliveryDirection { get; }
        public AuthoredDialogue(string id, AuthoredBeatReference beat, string characterId, string text,
            ScreenplayEmotion? emotion = null, string deliveryDirection = null)
        {
            Id = TemplateValidation.Id(id); Beat = beat ?? throw new ArgumentNullException(nameof(beat));
            CharacterId = TemplateValidation.Id(characterId); Text = TemplateValidation.Id(text);
            if (emotion.HasValue) TemplateValidation.EnumValue(emotion.Value);
            Emotion = emotion; DeliveryDirection = deliveryDirection?.Trim() ?? string.Empty;
        }
    }

    public sealed class IntendedPerformer
    {
        public string CharacterId { get; }
        public string PersonId { get; }
        public IntendedPerformer(string characterId, string personId)
        { CharacterId = TemplateValidation.Id(characterId); PersonId = TemplateValidation.Id(personId); }
    }

    /// <summary>Optional application-supplied information; no Employee or view is retained.</summary>
    public sealed class PerformerPreviewInfo
    {
        public bool IsActor { get; }
        public bool Available { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public PerformerPreviewInfo(bool isActor, bool available, IEnumerable<string> capabilities = null)
        {
            IsActor = isActor; Available = available;
            Capabilities = TemplateValidation.Ids(capabilities ?? Array.Empty<string>());
        }
    }
}
