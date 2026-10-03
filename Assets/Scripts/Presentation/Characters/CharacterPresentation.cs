using System;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>Presentation on the existing person root. Does not own destinations, employment or carry state.
    /// IsPosingHeld denotes temporary visual ownership by HeldPersonPresentation, including its settling phase.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterPresentation : HeldPersonPoseAdapter
    {
        public PersonProfile Person { get; private set; }
        public CharacterFamilyDefinition Definition { get; private set; }
        public CharacterPhysicalProfile PhysicalProfile { get; private set; }
        public CharacterVisualReference Visual { get; private set; }
        public Animator Animator => Visual != null ? Visual.Animator : null;
        public CharacterFacialController Face { get; private set; }
        public bool IsCanonical => Visual != null && Definition != null;
        public bool IsPosingHeld { get; private set; }
        public Transform AppearanceRoot => Visual != null ? Visual.transform : null;
        public Transform HeadAttachment => Visual != null ? Visual.HeadAttachment : null;
        public Vector3 InformationAnchorWorld => transform.TransformPoint(PhysicalProfile.VisualLocalOrigin + PhysicalProfile.InformationAnchor);
        private NavMeshAgent _navigation;
        private ISimulationTimeService _time;
        private bool _waiting;
        private Vector3 _restPosition, _restScale;
        private Quaternion _restRotation;
        private float _priorAnimatorSpeed;
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        internal void Initialize(PersonProfile person, CharacterFamilyDefinition definition, CharacterPhysicalProfile physical,
            CharacterVisualReference visual, CharacterFacialController face)
        {
            Person = person; Definition = definition; PhysicalProfile = physical; Visual = visual; Face = face;
            _navigation = GetComponent<NavMeshAgent>();
            visual.transform.localPosition = physical.VisualLocalOrigin;
            Animator.applyRootMotion = false; Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            ApplyPhysicalProfile();
        }
        public void BindPerson(PersonProfile person)
        {
            if (Person != null && !ReferenceEquals(Person, person)) throw new InvalidOperationException("Cannot replace a presentation's person identity.");
            if (person == null || (Definition != null && person.PresentationIdentity.Family != Definition.Family))
                throw new InvalidOperationException("Person and canonical family must agree.");
            Person = person;
        }
        public void BindTimeService(ISimulationTimeService time) => _time = time;
        public void SetWaiting(bool waiting) => _waiting = waiting;
        public void ApplyPhysicalProfile()
        {
            if (!IsCanonical) return;
            EmployeeNavigationProfile.Apply(gameObject, PhysicalProfile);
            // Existing selection objects remain owned by their candidate/employee agent.
            var marker = transform.Find("SelectionRing"); if (marker == null) marker = transform.Find("SelectionIndicator");
            if (marker != null) marker.localPosition = PhysicalProfile.SelectionLocalPosition;
        }
        private void Update()
        {
            if (!IsCanonical || IsPosingHeld || Animator.runtimeAnimatorController == null) return;
            float scale = LocalPresentationTime.Delta(1, _time);
            float velocity = !_waiting && _navigation != null && _navigation.isActiveAndEnabled && _navigation.isOnNavMesh ? _navigation.velocity.magnitude : 0;
            Animator.SetFloat(SpeedParameter, scale > .001f ? velocity / scale : 0);
            Animator.speed = scale;
        }
        public override void BeginPose(string reactionId)
        {
            if (IsPosingHeld || !IsCanonical) return;
            _restPosition = Visual.transform.localPosition; _restRotation = Visual.transform.localRotation; _restScale = Visual.transform.localScale;
            _priorAnimatorSpeed = Animator.speed; Animator.speed = 0; IsPosingHeld = true;
        }
        public override void ApplyPose(HeldPersonPoseFrame frame)
        {
            if (!IsPosingHeld || Visual == null) return;
            var pivot = Vector3.Scale(_restScale, PhysicalProfile.SuspensionPivot);
            var scale = new Vector3(1, frame.VerticalScale, 1);
            Visual.transform.localRotation = _restRotation * frame.LocalRotation;
            Visual.transform.localScale = Vector3.Scale(_restScale, scale);
            // Keep the measured suspension point fixed even when the held response stretches the visual.
            Visual.transform.localPosition = _restPosition + _restRotation * (pivot - frame.LocalRotation * Vector3.Scale(pivot, scale)) + frame.LocalOffset;
        }
        public override void EndPose()
        {
            if (!IsPosingHeld) return;
            if (Visual != null)
            { Visual.transform.SetLocalPositionAndRotation(_restPosition, _restRotation); Visual.transform.localScale = _restScale; }
            if (Animator != null) Animator.speed = _priorAnimatorSpeed;
            IsPosingHeld = false;
        }
        private void OnDisable() => EndPose();
        private void OnDestroy()
        {
            EndPose();
            if (Visual != null) DestroyOwned(Visual.gameObject);
        }
        internal static void DestroyOwned(UnityEngine.Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
