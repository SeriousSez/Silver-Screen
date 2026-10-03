using SilverScreen.Domain.Characters;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    [CreateAssetMenu(menuName = "SilverScreen/Characters/Family definition")]
    public sealed class CharacterFamilyDefinition : ScriptableObject
    {
        [SerializeField] private CharacterFamily _family;
        [SerializeField] private string _definitionId;
        [SerializeField] private int _version = 1;
        [SerializeField] private CharacterPhysicalProfile _defaultPhysicalProfile;
        [SerializeField] private CharacterFacialBinding _facialBinding;
        [SerializeField] private bool _requiresHumanoid = true;
        [SerializeField] private int _minimumBlinkFrames = 1;
        [SerializeField] private string _capabilityNotes;
        public CharacterFamily Family => _family;
        public string DefinitionId => _definitionId;
        public int Version => _version;
        public CharacterPhysicalProfile DefaultPhysicalProfile => _defaultPhysicalProfile;
        public CharacterFacialBinding FacialBinding => _facialBinding;
        public bool RequiresHumanoid => _requiresHumanoid;
        public int MinimumBlinkFrames => _minimumBlinkFrames;
        public string CapabilityNotes => _capabilityNotes;
        public const string AnimationContract = "Humanoid.Speed.Idle.Walk.Run.v1";

        public void Configure(CharacterFamily family, string definitionId, CharacterPhysicalProfile physical,
            CharacterFacialBinding facial, string notes, bool requiresHumanoid = true, int minimumBlinkFrames = 1, int version = 1)
        { _family = family; _definitionId = definitionId; _defaultPhysicalProfile = physical; _facialBinding = facial;
            _capabilityNotes = notes; _requiresHumanoid = requiresHumanoid; _minimumBlinkFrames = minimumBlinkFrames; _version = version; }
        public bool TryValidate(out string reason)
        {
            reason = null;
            if (!new PersonPresentationIdentity(_family).IsCanonical || string.IsNullOrWhiteSpace(_definitionId) || _version != 1 || _minimumBlinkFrames < 1)
                reason = "Unknown family, missing definition ID or unsupported definition version/capability.";
            else if (!_defaultPhysicalProfile.TryValidate(out reason)) return false;
            else if (_facialBinding == null) reason = "Facial binding absent.";
            else if (!_facialBinding.TryValidate(out reason)) return false;
            if (reason == null)
            {
                bool blink = false;
                foreach (var entry in _facialBinding.Entries)
                    if (entry.Semantic == FacialSemantic.BlinkBoth && entry.Capability == FacialCapability.Supported) blink = true;
                if (!blink) reason = "Canonical definition requires the validated BlinkBoth capability.";
            }
            return reason == null;
        }
    }
}
