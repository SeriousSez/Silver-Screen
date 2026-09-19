using System;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSlateSequence : MonoBehaviour
    {
        private enum SequenceState
        {
            Idle,
            MovingToMark,
            Clapping,
            ClearingSet
        }

        private const float BaseMoveSpeed = 3.5f;
        private const float BaseAcceleration = 14f;
        private const float ClapDurationSeconds = 1.2f;
        private const float MovementTimeoutSeconds = 20f;

        private SlatePositionLayout _layout;
        private ISimulationTimeService _timeService;
        private GameObject _operatorRoot;
        private NavMeshAgent _navAgent;
        private Transform _clapTop;
        private Vector3 _stagingPosition;
        private SequenceState _state;
        private float _stateElapsed;
        private Action _onCompleted;
        private Action<StudioRouteResult> _onFailed;

        public void Initialize(SlatePositionLayout layout, ISimulationTimeService timeService)
        {
            _layout = layout;
            _timeService = timeService;
            EnsurePrototypeOperator();
        }

        public StudioRouteResult TryBegin(Action onCompleted, Action<StudioRouteResult> onFailed)
        {
            if (_state != SequenceState.Idle) return StudioRouteResult.SequenceInProgress;
            if (_layout == null ||
                !_layout.TryGetPosition(SlatePositionType.SlateStaging, out Vector3 staging) ||
                !_layout.TryGetPosition(SlatePositionType.SlateMark, out Vector3 mark))
            {
                return StudioRouteResult.SlateMissing;
            }

            EnsurePrototypeOperator();
            if (!NavMesh.SamplePosition(staging, out NavMeshHit stagingHit, 2f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(mark, out NavMeshHit markHit, 2f, NavMesh.AllAreas))
            {
                return StudioRouteResult.NavigationRejected;
            }

            _onCompleted = onCompleted;
            _onFailed = onFailed;
            _stagingPosition = stagingHit.position;
            _operatorRoot.transform.position = _stagingPosition;
            _operatorRoot.SetActive(true);
            if (_navAgent == null || !_navAgent.isActiveAndEnabled)
            {
                ResetSequence();
                return StudioRouteResult.NavigationRejected;
            }

            if (!_navAgent.Warp(_stagingPosition) || !BeginMove(markHit.position, SequenceState.MovingToMark))
            {
                ResetSequence();
                return StudioRouteResult.NavigationRejected;
            }

            return StudioRouteResult.Started;
        }

        private void Update()
        {
            if (_state == SequenceState.Idle) return;

            bool paused = _timeService != null && _timeService.IsPaused;
            if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.isStopped = paused;
            if (paused) return;

            float speedMultiplier = _timeService != null ? _timeService.TimeScaleMultiplier : 1f;
            float delta = UnityEngine.Time.deltaTime * speedMultiplier;
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.speed = BaseMoveSpeed * speedMultiplier;
                _navAgent.acceleration = BaseAcceleration * speedMultiplier;
            }

            if (_state == SequenceState.Clapping)
            {
                UpdateClap(delta);
                return;
            }

            _stateElapsed += delta;
            if (_stateElapsed >= MovementTimeoutSeconds ||
                (_navAgent != null && !_navAgent.pathPending && _navAgent.pathStatus == NavMeshPathStatus.PathInvalid))
            {
                Fail(StudioRouteResult.NavigationRejected);
                return;
            }

            if (_navAgent != null && !_navAgent.pathPending &&
                _navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.15f)
            {
                if (_state == SequenceState.MovingToMark)
                {
                    _state = SequenceState.Clapping;
                    _stateElapsed = 0f;
                }
                else if (_state == SequenceState.ClearingSet)
                {
                    Complete();
                }
            }
        }

        private void UpdateClap(float delta)
        {
            _stateElapsed += delta;
            float normalized = Mathf.Clamp01(_stateElapsed / ClapDurationSeconds);
            float openAmount = normalized < 0.25f
                ? 1f - normalized / 0.25f
                : normalized > 0.65f ? (normalized - 0.65f) / 0.35f : 0f;
            if (_clapTop != null) _clapTop.localRotation = Quaternion.Euler(0f, 0f, 28f * openAmount);

            if (_stateElapsed < ClapDurationSeconds) return;
            if (!BeginMove(_stagingPosition, SequenceState.ClearingSet))
            {
                Fail(StudioRouteResult.NavigationRejected);
            }
        }

        private bool BeginMove(Vector3 target, SequenceState state)
        {
            if (_navAgent == null || !_navAgent.isOnNavMesh) return false;
            var path = new NavMeshPath();
            if (!_navAgent.CalculatePath(target, path) ||
                path.status != NavMeshPathStatus.PathComplete ||
                !_navAgent.SetPath(path))
            {
                return false;
            }

            _state = state;
            _stateElapsed = 0f;
            _navAgent.isStopped = _timeService != null && _timeService.IsPaused;
            return true;
        }

        private void Complete()
        {
            var callback = _onCompleted;
            ResetSequence();
            callback?.Invoke();
        }

        private void Fail(StudioRouteResult result)
        {
            var callback = _onFailed;
            ResetSequence();
            callback?.Invoke(result);
        }

        private void ResetSequence()
        {
            _state = SequenceState.Idle;
            _stateElapsed = 0f;
            _onCompleted = null;
            _onFailed = null;
            if (_clapTop != null) _clapTop.localRotation = Quaternion.Euler(0f, 0f, 28f);
            if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.ResetPath();
            if (_operatorRoot != null) _operatorRoot.SetActive(false);
        }

        private void EnsurePrototypeOperator()
        {
            if (_operatorRoot != null) return;

            _operatorRoot = new GameObject("PrototypeSlateOperator");
            _operatorRoot.SetActive(false);
            _operatorRoot.transform.SetParent(transform, true);
            _navAgent = _operatorRoot.AddComponent<NavMeshAgent>();
            _navAgent.speed = BaseMoveSpeed;
            _navAgent.acceleration = BaseAcceleration;
            _navAgent.angularSpeed = 720f;
            _navAgent.stoppingDistance = 0.1f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "OperatorVisual";
            body.transform.SetParent(_operatorRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Destroy(body.GetComponent<Collider>());

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "SlateBoard";
            board.transform.SetParent(_operatorRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, 1.25f, 0.38f);
            board.transform.localScale = new Vector3(0.75f, 0.48f, 0.08f);
            Destroy(board.GetComponent<Collider>());

            var topPivot = new GameObject("ClapTop");
            topPivot.transform.SetParent(_operatorRoot.transform, false);
            topPivot.transform.localPosition = new Vector3(-0.34f, 1.52f, 0.38f);
            _clapTop = topPivot.transform;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "ClapTopVisual";
            top.transform.SetParent(_clapTop, false);
            top.transform.localPosition = new Vector3(0.34f, 0f, 0f);
            top.transform.localScale = new Vector3(0.75f, 0.1f, 0.1f);
            Destroy(top.GetComponent<Collider>());

            _clapTop.localRotation = Quaternion.Euler(0f, 0f, 28f);
            _operatorRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            _onCompleted = null;
            _onFailed = null;
        }
    }
}
