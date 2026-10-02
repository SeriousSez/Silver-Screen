using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>Opt-in evaluation visual. EmployeeAgent owns navigation and person state.</summary>
    public sealed class HumanBasePrototypeVisual : HeldPersonPoseAdapter
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SkinnedMeshRenderer _face;
        [SerializeField] private Vector3 _suspensionPivot = new Vector3(0, 1.35f, 0);
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private NavMeshAgent _navigation;
        private SimulationTimeDriver _time;
        private Vector3 _restPosition, _restScale;
        private Quaternion _restRotation;
        private bool _held;
        private float _priorSpeed;

        public Animator Animator => _animator;
        public SkinnedMeshRenderer Face => _face;
        public bool IsPosingHeld => _held;

        public void Configure(Animator animator, Transform visualRoot, SkinnedMeshRenderer face)
        { _animator = animator; _visualRoot = visualRoot; _face = face; }

        private void Start()
        {
            _navigation = GetComponent<NavMeshAgent>();
            GetComponent<EmployeeAgent>().SetGenericFilmingPresentationEnabled(false);
            _time = FindAnyObjectByType<SimulationTimeDriver>();
            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        private void Update()
        {
            if (_animator == null || _held) return;
            float timeScale = LocalPresentationTime.Delta(1, _time != null ? _time.TimeService : null);
            float speed = _navigation != null && _navigation.isActiveAndEnabled && _navigation.isOnNavMesh
                ? _navigation.velocity.magnitude : 0;
            // Clip selection uses local walking speed; strategic speed scales presentation time.
            _animator.SetFloat(SpeedParameter, timeScale > .001f ? speed / timeScale : 0);
            _animator.speed = timeScale;
        }

        public override void BeginPose(string reactionId)
        {
            if (_held || _visualRoot == null || _animator == null) return;
            _restPosition = _visualRoot.localPosition; _restRotation = _visualRoot.localRotation;
            _restScale = _visualRoot.localScale; _priorSpeed = _animator.speed;
            _held = true;
            // A frozen idle plus the existing sway is only a carry compatibility proof.
            _animator.Play("Idle", 0, 0); _animator.Update(0); _animator.speed = 0;
        }

        public override void ApplyPose(HeldPersonPoseFrame frame)
        {
            if (!_held || _visualRoot == null) return;
            _visualRoot.localRotation = _restRotation * frame.LocalRotation;
            _visualRoot.localPosition = _restPosition + _suspensionPivot - frame.LocalRotation * _suspensionPivot + frame.LocalOffset;
            _visualRoot.localScale = Vector3.Scale(_restScale, new Vector3(1, frame.VerticalScale, 1));
        }

        public override void EndPose()
        {
            if (!_held) return;
            if (_visualRoot != null)
            { _visualRoot.localPosition = _restPosition; _visualRoot.localRotation = _restRotation; _visualRoot.localScale = _restScale; }
            if (_animator != null) _animator.speed = _priorSpeed;
            _held = false;
        }

        public bool SetShape(string exactName, float weight)
        {
            if (_face == null || _face.sharedMesh == null) return false;
            int index = _face.sharedMesh.GetBlendShapeIndex(exactName);
            if (index < 0) return false;
            _face.SetBlendShapeWeight(index, Mathf.Clamp(weight, 0, 100));
            return true;
        }

        public void ResetShapes()
        {
            if (_face == null || _face.sharedMesh == null) return;
            for (int i = 0; i < _face.sharedMesh.blendShapeCount; i++) _face.SetBlendShapeWeight(i, 0);
        }

        private void OnDisable() => EndPose();
    }
}
