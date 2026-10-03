using System;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>Value copy per person. Metres, upright unit-scale simulation root; authored anchors are
    /// feet-local. Feet = root - world up * base offset. Native model alignment lives below the visual root.
    /// The current baked Humanoid envelope is 2 m / .35 m; smaller agents do not gain new baked routes.</summary>
    [Serializable]
    public struct CharacterPhysicalProfile
    {
        [SerializeField] private float _bodyHeight, _navigationHeight, _navigationRadius, _baseOffset;
        [SerializeField] private float _capsuleHeight, _capsuleRadius;
        [SerializeField] private Vector3 _capsuleCenter, _suspensionPivot, _informationAnchor;
        [SerializeField] private float _carryLift, _selectionGroundClearance, _placementGroundClearance;
        public float BodyHeight => _bodyHeight;
        public float NavigationHeight => _navigationHeight;
        public float NavigationRadius => _navigationRadius;
        public float BaseOffset => _baseOffset;
        public float CapsuleHeight => _capsuleHeight;
        public float CapsuleRadius => _capsuleRadius;
        public Vector3 CapsuleCenter => _capsuleCenter;
        public Vector3 SuspensionPivot => _suspensionPivot;
        public Vector3 InformationAnchor => _informationAnchor;
        public float CarryLift => _carryLift;
        public float SelectionGroundClearance => _selectionGroundClearance;
        public float PlacementGroundClearance => _placementGroundClearance;
        public Vector3 VisualLocalOrigin => Vector3.down * _baseOffset;
        public Vector3 SelectionLocalPosition => Vector3.up * (_selectionGroundClearance - _baseOffset);
        public Vector3 FeetPoint(Vector3 rootPosition) => rootPosition - Vector3.up * _baseOffset;
        public static Vector3 GroundSeatedCapsuleCenter(float height, float baseOffset) => Vector3.up * (height * .5f - baseOffset);
        public static CharacterPhysicalProfile Legacy => new CharacterPhysicalProfile(2, 2, .35f, 1, 2, .35f,
            new Vector3(0, 1.7f, 0), new Vector3(0, 2.6f, 0));

        public CharacterPhysicalProfile(float bodyHeight, float navigationHeight, float navigationRadius,
            float baseOffset, float capsuleHeight, float capsuleRadius, Vector3 suspensionPivot,
            Vector3 informationAnchor, float carryLift = 1.2f, float selectionGroundClearance = .02f,
            float placementGroundClearance = .06f, Vector3? capsuleCenter = null)
        {
            _bodyHeight = bodyHeight; _navigationHeight = navigationHeight; _navigationRadius = navigationRadius;
            _baseOffset = baseOffset; _capsuleHeight = capsuleHeight; _capsuleRadius = capsuleRadius;
            _capsuleCenter = capsuleCenter ?? GroundSeatedCapsuleCenter(capsuleHeight, baseOffset);
            _suspensionPivot = suspensionPivot; _informationAnchor = informationAnchor; _carryLift = carryLift;
            _selectionGroundClearance = selectionGroundClearance; _placementGroundClearance = placementGroundClearance;
            if (!TryValidate(out var reason)) throw new ArgumentException(reason);
        }

        public bool TryValidate(out string reason)
        {
            reason = null;
            if (!Positive(_bodyHeight) || !Positive(_navigationHeight) || !Positive(_navigationRadius) ||
                !Positive(_capsuleHeight) || !Positive(_capsuleRadius) || !Nonnegative(_baseOffset) ||
                !Nonnegative(_carryLift) || !Nonnegative(_selectionGroundClearance) || !Nonnegative(_placementGroundClearance) ||
                !Finite(_capsuleCenter) || !Finite(_suspensionPivot) || !Finite(_informationAnchor))
                reason = "Physical dimensions must be positive and all profile values finite (offsets nonnegative).";
            else if (_navigationHeight < 2 * _navigationRadius || _capsuleHeight < 2 * _capsuleRadius)
                reason = "Navigation and capsule height must be at least twice their radius.";
            else if (_navigationHeight > 2 || _navigationRadius > .35f || _capsuleHeight > 2 || _capsuleRadius > .35f)
                reason = "Profile exceeds the existing 2 m / .35 m Humanoid bake envelope.";
            else if (_baseOffset > _navigationHeight || Mathf.Abs(_capsuleCenter.y - (_capsuleHeight / 2 - _baseOffset)) > .00001f)
                reason = "Capsule must be ground seated in the centre-root convention.";
            return reason == null;
        }
        private static bool Positive(float v) => float.IsFinite(v) && v > 0;
        private static bool Nonnegative(float v) => float.IsFinite(v) && v >= 0;
        internal static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
