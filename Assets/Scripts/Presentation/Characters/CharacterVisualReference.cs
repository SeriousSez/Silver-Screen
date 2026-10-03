using System;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>Explicit references inside a locally generated visual-only prefab; no person or simulation state.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisualReference : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _face;
        [SerializeField] private Transform _headAttachment;
        [SerializeField] private string _definitionId;
        [SerializeField] private int _definitionVersion;
        public Animator Animator => _animator;
        public SkinnedMeshRenderer Face => _face;
        public Transform HeadAttachment => _headAttachment;
        public void Configure(Animator animator, SkinnedMeshRenderer face, Transform headAttachment, CharacterFamilyDefinition definition)
        { _animator = animator; _face = face; _headAttachment = headAttachment; _definitionId = definition.DefinitionId; _definitionVersion = definition.Version; }
        public bool TryValidate(CharacterFamilyDefinition definition, out string reason)
        {
            reason = null;
            if (definition == null || !definition.TryValidate(out reason)) return false;
            if (_definitionId != definition.DefinitionId || _definitionVersion != definition.Version) reason = "Visual definition ID/version mismatch.";
            else if (_animator == null || _face == null || _face.sharedMesh == null || _headAttachment == null ||
                !_animator.transform.IsChildOf(transform) || !_face.transform.IsChildOf(transform) || !_headAttachment.IsChildOf(transform))
                reason = "Visual references must belong to this wrapper.";
            else if (definition.RequiresHumanoid && (_animator.avatar == null || !_animator.avatar.isValid || !_animator.avatar.isHuman || _animator.runtimeAnimatorController == null))
                reason = "Validated Humanoid Avatar/controller required.";
            if (reason != null) return false;
            // Fail closed: no scripts with hidden simulation authority, colliders, navigation, or nested metadata.
            foreach (var component in GetComponentsInChildren<Component>(true))
                if (component == null || !(component is Transform || component is Animator || component is SkinnedMeshRenderer ||
                    component is MeshFilter || component is MeshRenderer || component == this))
                { reason = "Visual wrapper contains a non-presentation component: " + (component == null ? "missing script" : component.GetType().Name); return false; }
            if (GetComponentsInChildren<Animator>(true).Length != 1) { reason = "Exactly one Animator is required."; return false; }
            foreach (var entry in definition.FacialBinding.Entries)
            {
                if (entry.Capability != FacialCapability.Supported) continue;
                int index = _face.sharedMesh.GetBlendShapeIndex(entry.RawChannel);
                if (index < 0 || (entry.Semantic == FacialSemantic.BlinkBoth && _face.sharedMesh.GetBlendShapeFrameCount(index) < definition.MinimumBlinkFrames))
                { reason = "Required facial capability missing: " + entry.Semantic; return false; }
            }
            return true;
        }
    }
}
