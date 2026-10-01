using UnityEngine;

namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Visual-only held/settle state. The fallback copies the current capsule mesh into a reusable child;
    /// it never rotates/scales/translates the NavMeshAgent or its collider.</summary>
    [DisallowMultipleComponent]
    public sealed class HeldPersonPresentation : MonoBehaviour
    {
        [SerializeField] private HeldPersonPoseAdapter _poseAdapter;
        [SerializeField, Range(0, 30)] private float _maximumBodyTilt = 16;
        [SerializeField, Range(0, 4)] private float _swayStrength = 1.2f;
        [SerializeField, Range(.3f, 2)] private float _damping = .85f;
        [SerializeField, Range(2, 15)] private float _settlingSpeed = 8;
        [SerializeField, Range(0, .05f)] private float _verticalResponse = .018f;
        [SerializeField, Range(2, 30)] private float _motionSmoothing = 12;
        [SerializeField, Range(.1f, .6f)] private float _groundSettleSeconds = .28f;
        [SerializeField, Range(.05f, .3f)] private float _interactionEntrySeconds = .12f;
        public HeldPersonPresentationState State { get; private set; }
        public Vector3 SmoothedVelocity { get; private set; }
        public Vector3 SmoothedAcceleration { get; private set; }
        public Vector3 SwayDegrees => _sway;
        public Transform Visual => _visual;
        private Transform _visual;
        private MeshRenderer _source;
        private bool _sourceEnabled;
        private Animator[] _animators;
        private bool[] _animatorEnabled;
        private Vector3 _previousPosition, _sway, _springVelocity;
        private Quaternion _releaseRotation = Quaternion.identity;
        private float _elapsed, _heldBlend;
        private bool _poseActive;

        public void BeginHeld(string reactionId = "Held.Neutral")
        {
            Finish();
            if (_poseAdapter == null) _poseAdapter = GetComponent<HeldPersonPoseAdapter>();
            _poseActive = true;
            if (_poseAdapter != null) _poseAdapter.BeginPose(reactionId);
            else BeginPlaceholder();
            _previousPosition = transform.position;
            SmoothedVelocity = SmoothedAcceleration = _sway = _springVelocity = Vector3.zero;
            _elapsed = _heldBlend = 0;
            State = HeldPersonPresentationState.Held;
            Apply(Quaternion.identity, Vector3.zero, 1, 0);
        }
        public void Release(PersonReleasePresentation release)
        {
            if (State != HeldPersonPresentationState.Held) return;
            if (release == PersonReleasePresentation.Cancel) { Finish(); return; }
            _releaseRotation = CurrentRotation();
            _elapsed = 0;
            State = release == PersonReleasePresentation.Contextual ? HeldPersonPresentationState.InteractionEntry : HeldPersonPresentationState.GroundSettle;
        }
        private void LateUpdate() => AdvancePresentation(Time.unscaledDeltaTime);

        /// <summary>Unscaled local presentation seconds, independent of strategic speed. No per-frame allocations.</summary>
        public void AdvancePresentation(float deltaSeconds)
        {
            if (State == HeldPersonPresentationState.Normal || !float.IsFinite(deltaSeconds) || deltaSeconds <= 0) return;
            // Ignore long stalls as impulses; bound integration work after window focus/Editor pauses.
            float dt = Mathf.Min(deltaSeconds, .1f);
            _elapsed += dt;
            if (State == HeldPersonPresentationState.Held)
            {
                var measured = Vector3.ClampMagnitude((transform.position - _previousPosition) / deltaSeconds, 12);
                _previousPosition = transform.position;
                float blend = 1 - Mathf.Exp(-_motionSmoothing * dt);
                var oldVelocity = SmoothedVelocity;
                SmoothedVelocity = Vector3.Lerp(oldVelocity, measured, blend);
                var acceleration = Vector3.ClampMagnitude((SmoothedVelocity - oldVelocity) / dt, 50);
                SmoothedAcceleration = Vector3.Lerp(SmoothedAcceleration, acceleration, blend);
                var local = transform.InverseTransformDirection(SmoothedVelocity + SmoothedAcceleration * .04f);
                var target = Vector3.ClampMagnitude(new Vector3(local.z, 0, -local.x) * _swayStrength, _maximumBodyTilt);
                // Bounded substeps keep a damped second-order response stable at low and high frame rates.
                int steps = Mathf.CeilToInt(dt * 120);
                float step = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    _springVelocity += ((_settlingSpeed * _settlingSpeed) * (target - _sway) - 2 * _damping * _settlingSpeed * _springVelocity) * step;
                    _sway += _springVelocity * step;
                }
                _sway = Vector3.ClampMagnitude(_sway, _maximumBodyTilt);
                _heldBlend = Mathf.SmoothStep(0, 1, _elapsed / .16f);
                float stretch = Mathf.Clamp(SmoothedVelocity.y * _verticalResponse, -.025f, .04f);
                Apply(CurrentRotation(), Vector3.down * (Mathf.Max(0, stretch) * .5f), 1 + stretch * _heldBlend, _heldBlend);
            }
            else
            {
                bool ground = State == HeldPersonPresentationState.GroundSettle;
                float progress = Mathf.Clamp01(_elapsed / (ground ? _groundSettleSeconds : _interactionEntrySeconds));
                float weight = 1 - Mathf.SmoothStep(0, 1, progress);
                float compression = ground ? Mathf.Sin(progress * Mathf.PI) * .04f : 0;
                Apply(Quaternion.Slerp(Quaternion.identity, _releaseRotation, weight), Vector3.down * compression, 1 - compression, weight);
                if (progress >= 1) Finish();
            }
        }
        private Quaternion CurrentRotation()
        {
            // Off-axis suspension makes the primitive read as dangling even while the pointer rests.
            var angles = Vector3.ClampMagnitude(new Vector3(9, 0, 3) + _sway, _maximumBodyTilt) * _heldBlend;
            return Quaternion.Euler(angles);
        }
        private void Apply(Quaternion rotation, Vector3 offset, float verticalScale, float weight)
        {
            var frame = new HeldPersonPoseFrame(State, rotation, offset, verticalScale, weight);
            if (_poseAdapter != null) { _poseAdapter.ApplyPose(frame); return; }
            if (_visual == null) return;
            var pivot = Vector3.up * .7f;
            _visual.localRotation = rotation;
            _visual.localPosition = pivot - rotation * pivot + offset;
            _visual.localScale = new Vector3(1, verticalScale, 1);
        }
        private void BeginPlaceholder()
        {
            _source = GetComponent<MeshRenderer>();
            var filter = GetComponent<MeshFilter>();
            if (_source != null && filter != null && filter.sharedMesh != null)
            {
                if (_visual == null)
                {
                    var go = new GameObject("HeldPersonVisual");
                    _visual = go.transform; _visual.SetParent(transform, false);
                    go.layer = gameObject.layer;
                    go.AddComponent<MeshFilter>(); go.AddComponent<MeshRenderer>();
                }
                _visual.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = _visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = _source.sharedMaterials;
                renderer.shadowCastingMode = _source.shadowCastingMode;
                renderer.receiveShadows = _source.receiveShadows;
                var properties = new MaterialPropertyBlock(); _source.GetPropertyBlock(properties); renderer.SetPropertyBlock(properties);
                _sourceEnabled = _source.enabled;
                renderer.enabled = _sourceEnabled;
                _source.enabled = false; _visual.gameObject.SetActive(true);
            }
            // Current capsules have no Animator. Preserve enabled flags for an explicitly attached future prototype.
            _animators = GetComponentsInChildren<Animator>();
            _animatorEnabled = new bool[_animators.Length];
            for (int i = 0; i < _animators.Length; i++) { _animatorEnabled[i] = _animators[i].enabled; _animators[i].enabled = false; }
        }
        private void Finish()
        {
            if (_poseActive)
            {
                if (_poseAdapter != null) _poseAdapter.EndPose();
                else
                {
                    if (_source != null) _source.enabled = _sourceEnabled;
                    if (_visual != null) { _visual.localPosition = Vector3.zero; _visual.localRotation = Quaternion.identity; _visual.localScale = Vector3.one; _visual.gameObject.SetActive(false); }
                    if (_animators != null) for (int i = 0; i < _animators.Length; i++) if (_animators[i] != null) _animators[i].enabled = _animatorEnabled[i];
                }
            }
            _poseActive = false;
            State = HeldPersonPresentationState.Normal;
            SmoothedVelocity = SmoothedAcceleration = _sway = _springVelocity = Vector3.zero;
        }
        private void OnDisable() => Finish();
        private void OnDestroy()
        {
            Finish();
            if (_visual != null)
            {
                if (Application.isPlaying) Destroy(_visual.gameObject); else DestroyImmediate(_visual.gameObject);
            }
        }
    }
}
