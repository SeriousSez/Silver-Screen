using System;
using UnityEngine;
using UnityEngine.AI;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Time;

namespace SilverScreen.Presentation.Employees
{
    [RequireComponent(typeof(NavMeshAgent))]
    [SelectionBase]
    public class EmployeeAgent : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NavMeshAgent _navAgent;
        [SerializeField] private GameObject _selectionIndicator;

        [Header("Wander Settings")]
        [SerializeField] private float _wanderRadius = 14f;
        [SerializeField] private float _minWaitTime = 4f;
        [SerializeField] private float _maxWaitTime = 9f;

        public Employee Employee { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsHeld { get; private set; }
        public bool IsReturningAfterDrop { get; private set; }
        private Vector3 _taskDestination;
        private bool _hasTaskAnchor;
        private int _dropFrame = -1;
        private Bounds? _departingAssignmentArea;
        private Bounds? _idleBuildingExclusion;
        public event Action<bool> AvailabilityChanged;

        public void ResumeAfterWorkforcePlacement(Bounds assignmentArea)
        {
            // Hiring may already have dispatched real work through OnEmployeeAdded.
            // Restore its route after carry's Warp/ResetPath; otherwise depart using normal idle locomotion.
            _dropFrame = UnityEngine.Time.frameCount;
            IsReturningAfterDrop = _hasTaskAnchor;
            _homeCenter = transform.position;
            _departingAssignmentArea = _hasTaskAnchor ? (Bounds?)null : assignmentArea;
            _idleBuildingExclusion = assignmentArea;
            _idleWaitTimer = 0;
            if (!_hasTaskAnchor) { Employee?.SetState(EmployeeState.Idle); Employee?.SetIntent(EmployeeIntent.None); }
            if (_timeService != null) ApplySimulationSpeed(_timeService.CurrentSpeed);
        }

        public void SetHeld(bool held)
        {
            if (held) _autonomy?.Interrupt(Employee);
            IsHeld = held;
            if (held)
            {
                // Hide the last gesture frame without canceling the ongoing beat/production obligation.
                if (_performanceVisual != null) _performanceVisual.SetActive(false);
                AvailabilityChanged?.Invoke(false);
            }
            else
            {
                if (_timeService != null) ApplySimulationSpeed(_timeService.CurrentSpeed);
                _dropFrame = UnityEngine.Time.frameCount;
                IsReturningAfterDrop = _hasTaskAnchor;
                _homeCenter = transform.position;
                _idleWaitTimer = _minWaitTime;
                if (!_hasTaskAnchor && Employee?.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander)
                { Employee.SetState(EmployeeState.Idle); Employee.SetIntent(EmployeeIntent.None); }
                if (!IsReturningAfterDrop) AvailabilityChanged?.Invoke(true);
            }
        }
        private bool ResumeAfterDrop()
        {
            if (!IsReturningAfterDrop) return false;
            if (_navAgent == null) return true;
            var feet = transform.position - Vector3.up * _navAgent.baseOffset;
            if (_navAgent.isOnNavMesh &&
                Vector3.Distance(feet, _taskDestination) <= _navAgent.stoppingDistance + .25f)
            { IsReturningAfterDrop = false; AvailabilityChanged?.Invoke(true); return false; }
            if (!_navAgent.hasPath || _navAgent.pathStatus != NavMeshPathStatus.PathComplete)
                RecoverExplicitTaskPath();
            return IsReturningAfterDrop;
        }

        private ISimulationTimeService _timeService;
        private Vector3 _homeCenter;
        private float _idleWaitTimer;
        private bool _hasExplicitTask;
        private bool _autonomousRoute;
        private NavMeshPath _taskPath;
        private float _taskPathRetryTimer;
        private float _taskPathRetryDelay;
        private float _taskPathRecoveryElapsed;
        private PersonAutonomySimulation _autonomy;
        private Action _onArrivalCallback;
        private Action _onNavigationFailed;

        private const float InitialTaskPathRetryDelay = .5f;
        private const float MaximumTaskPathRetryDelay = 30f;
        private const float TaskPathRecoveryLimit = 600f;

        private float _baseSpeed = 3.5f;
        private float _baseAcceleration = 14f;
        private GameObject _performanceVisual;
        private Transform _performanceLeftArm;
        private Transform _performanceRightArm;
        private float _performanceElapsed;
        private bool _beatPerformanceActive;
        private ScreenplayBeatType _beatType;
        private ScreenplayEmotion _beatEmotion;
        private float _performanceIntensity = 0.6f;
        private float _performanceConfidence = 0.6f;
        private float _performanceExpressiveness = 0.6f;
        private float _performanceTiming = 0.6f;
        private Transform _beatTarget;
        private float _beatDuration;
        private Action _onBeatCompleted;
        private Quaternion _beatStartRotation;
        private bool _genericFilmingPresentationEnabled = true;

        private void Awake()
        {
            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();
            EmployeeNavigationProfile.Apply(gameObject);
            _homeCenter = transform.position;

            if (_navAgent != null)
            {
                _baseSpeed = _navAgent.speed;
                _baseAcceleration = _navAgent.acceleration;
            }

            // Stagger initial wander timer so agents don't move simultaneously
            _idleWaitTimer = UnityEngine.Random.Range(2f, 7f);

            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(false);
            }
        }

        private void Start()
        {
            if (_navAgent != null && !_navAgent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _navAgent.Warp(hit.position);
                }
            }

            if (_timeService == null)
            {
                var driver = FindAnyObjectByType<SilverScreen.Presentation.SimulationTime.SimulationTimeDriver>();
                if (driver != null && driver.TimeService != null)
                {
                    BindTimeService(driver.TimeService);
                }
            }
            else
            {
                ApplySimulationSpeed(_timeService.CurrentSpeed);
            }
        }

        private void OnDestroy()
        {
            if (_autonomy != null)
            {
                if (Application.isPlaying && Employee != null)
                    _autonomy.Unregister(Employee);
                _autonomy.ActivitySelected -= HandleAutonomousActivitySelected;
                _autonomy.ActivityCancelled -= HandleAutonomousActivityCancelled;
            }
            if (_timeService != null)
            {
                _timeService.OnSpeedChanged -= HandleSpeedChanged;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _autonomy == null || Employee == null) return;
            _autonomy.Register(Employee);
            UpdateAutonomyPosition();
        }

        private void OnDisable()
        {
            if (Application.isPlaying && _autonomy != null && Employee != null)
                _autonomy.Unregister(Employee);
        }

        public void BindDomain(Employee employee)
        {
            Employee = employee;
        }

        public void BindAutonomy(PersonAutonomySimulation autonomy)
        {
            if (_autonomy != null)
            {
                _autonomy.ActivitySelected -= HandleAutonomousActivitySelected;
                _autonomy.ActivityCancelled -= HandleAutonomousActivityCancelled;
            }
            _autonomy = autonomy;
            if (_autonomy != null)
            {
                _autonomy.ActivitySelected += HandleAutonomousActivitySelected;
                _autonomy.ActivityCancelled += HandleAutonomousActivityCancelled;
                UpdateAutonomyPosition();
            }
        }

        public void BindTimeService(ISimulationTimeService timeService)
        {
            if (_timeService != null)
            {
                _timeService.OnSpeedChanged -= HandleSpeedChanged;
            }

            _timeService = timeService;

            if (_timeService != null)
            {
                _timeService.OnSpeedChanged += HandleSpeedChanged;
                ApplySimulationSpeed(_timeService.CurrentSpeed);
            }
        }

        private void HandleSpeedChanged(SimulationSpeed newSpeed)
        {
            ApplySimulationSpeed(newSpeed);
        }

        private void ApplySimulationSpeed(SimulationSpeed speed)
        {
            if (IsHeld || _navAgent == null || !_navAgent.isOnNavMesh) return;

            if (speed == SimulationSpeed.Paused)
            {
                _navAgent.isStopped = true;
                _navAgent.velocity = Vector3.zero;
            }
            else
            {
                _navAgent.isStopped = false;
                _navAgent.speed = _baseSpeed;
                _navAgent.acceleration = _baseAcceleration;
            }
        }

        public void SetSelectionIndicator(GameObject indicator)
        {
            _selectionIndicator = indicator;
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(IsSelected);
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(selected);
            }
        }

        private void Update()
        {
            if (Employee == null || IsHeld || UnityEngine.Time.frameCount == _dropFrame) return;
            if (_autonomy != null)
                _autonomy.UpdatePosition(Employee,
                    AutonomyPositionAt(transform.position - Vector3.up * (_navAgent != null ? _navAgent.baseOffset : 0f)));

            // When simulation is paused, freeze simulation updates, preserving destinations and intent
            if (_timeService != null && _timeService.IsPaused)
            {
                if (_navAgent != null && _navAgent.isOnNavMesh && !_navAgent.isStopped)
                {
                    _navAgent.isStopped = true;
                    _navAgent.velocity = Vector3.zero;
                }
                return;
            }

            if (ResumeAfterDrop()) return;
            UpdatePerformancePresentation();

            if (_hasExplicitTask)
            {
                CheckExplicitArrival();
            }
            else if (Employee.CurrentState == EmployeeState.Idle)
            {
                UpdateIdleWander();
            }
            else if (Employee.CurrentState == EmployeeState.Walking && !_hasExplicitTask)
            {
                if (_autonomy != null && _autonomy.ShouldDeferIdleWander(Employee) &&
                    Employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander)
                {
                    if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.ResetPath();
                    Employee.SetState(EmployeeState.Idle);
                    Employee.SetIntent(EmployeeIntent.None);
                }
                else CheckWanderArrival();
            }
        }

        private void UpdatePerformancePresentation()
        {
            bool performing = Employee.CurrentState == EmployeeState.Filming &&
                              Employee.Role == EmployeeRole.Actor;
            if (!performing)
            {
                ResetPerformancePresentation();
                return;
            }

            EnsurePerformanceVisual();
            _performanceVisual.SetActive(true);

            _performanceElapsed += SilverScreen.Presentation.SimulationTime.LocalPresentationTime.Delta(UnityEngine.Time.unscaledDeltaTime, _timeService);
            if (_beatPerformanceActive)
            {
                UpdateBeatPerformance();
                return;
            }

            if (!_genericFilmingPresentationEnabled)
            {
                _performanceElapsed = 0f;
                _performanceVisual.SetActive(false);
                return;
            }

            float gesture = Mathf.Sin(_performanceElapsed * 4f);
            _performanceLeftArm.localRotation = Quaternion.Euler(0f, 0f, -35f + gesture * 28f);
            _performanceRightArm.localRotation = Quaternion.Euler(0f, 0f, 35f - gesture * 28f);
            _performanceVisual.transform.localRotation =
                Quaternion.Euler(0f, Mathf.Sin(_performanceElapsed * 2f) * 8f, 0f);
        }

        public bool TryBeginBeatPerformance(
            ScreenplayBeatType beatType,
            BeatPerformanceResult performance,
            Transform target,
            float duration,
            Action onCompleted)
        {
            if (_beatPerformanceActive ||
                Employee == null ||
                Employee.Role != EmployeeRole.Actor ||
                Employee.CurrentState != EmployeeState.Filming ||
                performance == null ||
                duration <= 0f)
            {
                return false;
            }

            EnsurePerformanceVisual();
            _performanceVisual.SetActive(true);
            _beatPerformanceActive = true;
            _beatType = beatType;
            _beatEmotion = performance.DeliveredEmotion;
            _performanceIntensity = (float)performance.DeliveredIntensity;
            _performanceConfidence = (float)performance.Confidence;
            _performanceExpressiveness = (float)performance.Expressiveness;
            _performanceTiming = (float)performance.Timing;
            _beatTarget = target;
            _beatDuration = duration;
            _performanceElapsed = 0f;
            _onBeatCompleted = onCompleted;
            _beatStartRotation = transform.rotation;
            return true;
        }

        public void SetGenericFilmingPresentationEnabled(bool enabled)
        {
            _genericFilmingPresentationEnabled = enabled;
            if (!enabled && !_beatPerformanceActive && _performanceVisual != null)
                _performanceVisual.SetActive(false);
        }

        private void UpdateBeatPerformance()
        {
            float normalized = Mathf.Clamp01(_performanceElapsed / _beatDuration);
            float hesitation = Mathf.Lerp(0.16f, 0f, _performanceConfidence);
            float performedTime = Mathf.Clamp01((normalized - hesitation) / Mathf.Max(0.01f, 1f - hesitation));
            float tempo = Mathf.Lerp(0.82f, 1.18f, _performanceTiming);
            float pulse = Mathf.Sin(performedTime * Mathf.PI * 4f * tempo);
            float amplitude = Mathf.Lerp(
                0.42f,
                1.35f,
                _performanceIntensity * 0.4f + _performanceConfidence * 0.2f + _performanceExpressiveness * 0.4f);

            switch (_beatType)
            {
                case ScreenplayBeatType.Dialogue:
                    FaceBeatTarget();
                    _performanceLeftArm.localRotation = Quaternion.Euler(0f, 0f, -28f + pulse * 18f * amplitude);
                    _performanceRightArm.localRotation = Quaternion.Euler(0f, 0f, 18f - pulse * 30f * amplitude);
                    _performanceVisual.transform.localRotation =
                        Quaternion.Euler(0f, pulse * 4f * amplitude, 0f);
                    break;

                case ScreenplayBeatType.Reaction:
                    UpdateReactionGesture(pulse, amplitude);
                    break;

                default:
                    float sweep = Mathf.Sin(performedTime * Mathf.PI);
                    _performanceLeftArm.localRotation = Quaternion.Euler(0f, 0f, -35f - sweep * 45f * amplitude);
                    _performanceRightArm.localRotation = Quaternion.Euler(0f, 0f, 35f + sweep * 45f * amplitude);
                    _performanceVisual.transform.localRotation =
                        Quaternion.Euler(0f, pulse * 14f * amplitude, sweep * 5f * amplitude);
                    break;
            }

            if (_performanceElapsed >= _beatDuration)
                CompleteBeatPerformance();
        }

        private void UpdateReactionGesture(float pulse, float amplitude)
        {
            float armSpread = 28f;
            float bodyPitch = 0f;
            float bodyTurn = pulse * 5f;
            switch (_beatEmotion)
            {
                case ScreenplayEmotion.Happy:
                    armSpread = 65f;
                    bodyPitch = -6f;
                    break;
                case ScreenplayEmotion.Sad:
                    armSpread = 8f;
                    bodyPitch = 14f;
                    break;
                case ScreenplayEmotion.Angry:
                    armSpread = 50f;
                    bodyTurn = pulse * 18f;
                    break;
                case ScreenplayEmotion.Afraid:
                    armSpread = 42f;
                    bodyPitch = -18f * Mathf.Abs(pulse);
                    break;
                case ScreenplayEmotion.Romantic:
                    armSpread = 38f;
                    bodyTurn = pulse * 3f;
                    break;
                case ScreenplayEmotion.Confident:
                    armSpread = 58f;
                    bodyPitch = -4f;
                    break;
                case ScreenplayEmotion.Nervous:
                    armSpread = 18f + Mathf.Abs(pulse) * 12f;
                    bodyTurn = pulse * 10f;
                    break;
            }

            armSpread *= amplitude;
            _performanceLeftArm.localRotation = Quaternion.Euler(0f, 0f, -armSpread);
            _performanceRightArm.localRotation = Quaternion.Euler(0f, 0f, armSpread);
            _performanceVisual.transform.localRotation =
                Quaternion.Euler(bodyPitch * amplitude, bodyTurn * amplitude, 0f);
        }

        private void FaceBeatTarget()
        {
            if (_beatTarget == null) return;
            Vector3 direction = _beatTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void CompleteBeatPerformance()
        {
            var callback = _onBeatCompleted;
            ResetPerformancePresentation();
            callback?.Invoke();
        }

        public void CancelBeatPresentation() => ResetPerformancePresentation();

        public void CancelPresentationMovement()
        {
            _autonomy?.Interrupt(Employee);
            _hasTaskAnchor = false;
            IsReturningAfterDrop = false;
            if (!IsHeld) AvailabilityChanged?.Invoke(true);
            _hasExplicitTask = false;
            _onArrivalCallback = null;
            _autonomousRoute = false;
            if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh) _navAgent.ResetPath();
        }

        private void ResetPerformancePresentation()
        {
            if (_beatPerformanceActive) transform.rotation = _beatStartRotation;
            _beatPerformanceActive = false;
            _beatTarget = null;
            _beatDuration = 0f;
            _onBeatCompleted = null;
            _performanceElapsed = 0f;
            _performanceIntensity = 0.6f;
            _performanceConfidence = 0.6f;
            _performanceExpressiveness = 0.6f;
            _performanceTiming = 0.6f;
            if (_performanceVisual != null)
            {
                _performanceVisual.transform.localPosition = Vector3.zero;
                _performanceVisual.transform.localRotation = Quaternion.identity;
                _performanceVisual.SetActive(false);
            }
        }

        private void EnsurePerformanceVisual()
        {
            if (_performanceVisual != null) return;

            _performanceVisual = new GameObject("PrototypePerformanceGesture");
            _performanceVisual.transform.SetParent(transform, false);

            _performanceLeftArm = CreateGestureArm("LeftArm", new Vector3(-0.31f, 0.32f, 0f));
            _performanceRightArm = CreateGestureArm("RightArm", new Vector3(0.31f, 0.32f, 0f));
            _performanceVisual.SetActive(false);
        }

        private Transform CreateGestureArm(string objectName, Vector3 localPosition)
        {
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = objectName;
            arm.transform.SetParent(_performanceVisual.transform, false);
            arm.transform.localPosition = localPosition;
            arm.transform.localScale = new Vector3(0.14f, 0.58f, 0.14f);
            Destroy(arm.GetComponent<Collider>());

            var sourceRenderer = GetComponent<Renderer>();
            var armRenderer = arm.GetComponent<Renderer>();
            if (sourceRenderer != null && armRenderer != null)
            {
                armRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }

            return arm.transform;
        }

        private void UpdateIdleWander()
        {
            _idleWaitTimer -= SilverScreen.Presentation.SimulationTime.LocalPresentationTime.Delta(UnityEngine.Time.unscaledDeltaTime, _timeService);

            if (_idleWaitTimer <= 0f)
            {
                if (_departingAssignmentArea.HasValue)
                {
                    if (TryDepartAssignmentArea()) _departingAssignmentArea = null;
                    else _idleWaitTimer = .5f; // Retry in simulation time if temporarily crowded.
                    return;
                }
                if (_autonomy != null && _autonomy.ShouldDeferIdleWander(Employee)) return;
                if (TryFindRandomNavMeshPoint(_homeCenter, _wanderRadius, out Vector3 destination))
                {
                    _navAgent.SetDestination(destination);
                    Employee.SetState(EmployeeState.Walking);
                    Employee.SetIntent(EmployeeIntent.IdleWander);
                }
                else
                {
                    _idleWaitTimer = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
                }
            }
        }

        private bool TryDepartAssignmentArea()
        {
            if (_navAgent == null || !_navAgent.isOnNavMesh) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = _navAgent.agentTypeID, areaMask = _navAgent.areaMask };
            var origin = transform.position - Vector3.up * _navAgent.baseOffset;
            var area = _departingAssignmentArea.Value;
            var path = new NavMeshPath();
            // Existing idle-wander state; bounded nearby destinations outside the workforce region.
            for (int ring = 1; ring <= 6; ring++) for (int direction = 0; direction < 12; direction++)
            {
                float angle = direction * Mathf.PI / 6;
                var requested = origin + new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)) * (ring*2f);
                if (!NavMesh.SamplePosition(requested,out var hit,.6f,filter)) continue;
                var point = hit.position;
                if (point.x >= area.min.x && point.x <= area.max.x && point.z >= area.min.z && point.z <= area.max.z) continue;
                if (Vector3.Distance(origin,point) < 1.5f) continue;
                float radius = _navAgent.radius;
                if (Physics.CheckCapsule(point+Vector3.up*(radius+.06f),point+Vector3.up*(_navAgent.height-radius),radius,~0,QueryTriggerInteraction.Ignore)) continue;
                if (!_navAgent.CalculatePath(point,path) || path.status != NavMeshPathStatus.PathComplete || !_navAgent.SetPath(path)) continue;
                _homeCenter = point;
                Employee.SetState(EmployeeState.Walking);
                Employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.DepartingWorkforceArea, "Leaving the hiring area"));
                return true;
            }
            return false;
        }

        private void CheckWanderArrival()
        {
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.1f)
            {
                Employee.SetState(EmployeeState.Idle);
                Employee.SetIntent(EmployeeIntent.None);
                _idleWaitTimer = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
            }
        }

        public bool TryAssignTaskDestination(Vector3 destination, EmployeeIntent intent, Action onArrival = null,
            Action onNavigationFailed = null)
        {
            bool interruptedAutonomy = _autonomy?.Interrupt(Employee, false) == true;
            bool assigned = TryAssignTaskDestinationCore(destination, intent, onArrival, false, onNavigationFailed);
            if (!assigned && interruptedAutonomy)
            {
                Employee?.SetState(EmployeeState.Idle);
                Employee?.SetIntent(EmployeeIntent.None);
            }
            return assigned;
        }

        private bool TryAssignTaskDestinationCore(Vector3 destination, EmployeeIntent intent,
            Action onArrival, bool autonomous, Action onNavigationFailed = null)
        {
            if (_navAgent == null || !_navAgent.isActiveAndEnabled || !_navAgent.isOnNavMesh)
            {
                return false;
            }
            if (IsHeld)
            {
                ResetTaskPathRecovery();
                _departingAssignmentArea = null;
                _taskDestination = destination; _hasTaskAnchor = true; _hasExplicitTask = true;
                _onArrivalCallback = onArrival;
                _onNavigationFailed = onNavigationFailed;
                _autonomousRoute = autonomous;
                Employee?.SetIntent(intent);
                return true;
            }
            var taskPath = GetTaskPath();
            bool hasRoute = _navAgent.CalculatePath(destination, taskPath) &&
                taskPath.status == NavMeshPathStatus.PathComplete &&
                _navAgent.SetPath(taskPath);
            if (!hasRoute && (autonomous || onNavigationFailed == null))
            {
                return false;
            }

            ResetTaskPathRecovery();
            _departingAssignmentArea = null; // Accepted real work takes priority over idle departure.
            _taskDestination = destination;
            _hasTaskAnchor = true;
            _hasExplicitTask = true;
            _onArrivalCallback = onArrival;
            _onNavigationFailed = onNavigationFailed;
            _autonomousRoute = autonomous;

            if (Employee != null)
            {
                Employee.SetState(EmployeeState.Walking);
                Employee.SetIntent(intent);
            }

            if (!hasRoute)
            {
                _navAgent.ResetPath();
                _taskPathRetryTimer = InitialTaskPathRetryDelay;
            }

            if (_timeService != null && _timeService.IsPaused)
            {
                _navAgent.isStopped = true;
            }

            return true;
        }

        public void ClearTaskDestination()
        {
            _autonomy?.Interrupt(Employee, false);
            _hasTaskAnchor = false;
            IsReturningAfterDrop = false;
            if (!IsHeld) AvailabilityChanged?.Invoke(true);
            _hasExplicitTask = false;
            _onArrivalCallback = null;
            _onNavigationFailed = null;

            if (Employee != null)
            {
                Employee.SetState(EmployeeState.Idle);
                Employee.SetIntent(EmployeeIntent.None);
            }

            _idleWaitTimer = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
        }

        private void ResetTaskPathRecovery()
        {
            _taskPathRetryTimer = 0;
            _taskPathRetryDelay = InitialTaskPathRetryDelay;
            _taskPathRecoveryElapsed = 0;
        }

        private NavMeshPath GetTaskPath()
        {
            if (_taskPath == null) _taskPath = new NavMeshPath();
            return _taskPath;
        }

        public void ForceCompleteArrival()
        {
            if (_hasExplicitTask)
            {
                _hasExplicitTask = false;
                if (_navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.ResetPath();
                }
                var callback = _onArrivalCallback;
                _onArrivalCallback = null;
                _onNavigationFailed = null;
                if (_autonomousRoute)
                {
                    _autonomousRoute = false;
                    if (_autonomousFacingPoint.HasValue)
                    {
                        var direction = _autonomousFacingPoint.Value -
                            (transform.position - Vector3.up * (_navAgent != null ? _navAgent.baseOffset : 0f));
                        direction.y = 0;
                        if (direction.sqrMagnitude > .001f)
                            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                        _autonomousFacingPoint = null;
                    }
                    _autonomy?.NavigationCompleted(Employee, true);
                }
                else callback?.Invoke();
            }
        }

        private void HandleAutonomousActivitySelected(Employee employee, PersonAutonomyDecision decision)
        {
            if (Employee != employee || decision == null ||
                decision.OpportunityId == null && decision.SessionId == null) return;
            if (decision.SessionId != null)
            {
                var position = decision.ParticipationPosition;
                _autonomousFacingPoint = new Vector3(decision.SessionCenter.X, decision.SessionCenter.Y,
                    decision.SessionCenter.Z);
                bool assignedSession = TryAssignTaskDestinationCore(
                    new Vector3(position.X, position.Y, position.Z),
                    new EmployeeIntent(EmployeeIntentPurpose.AutonomousActivity,
                        decision.Activity + ": " + decision.SessionId, decision.SessionId),
                    null, true);
                if (!assignedSession)
                {
                    _autonomousFacingPoint = null;
                    _autonomy.NavigationCompleted(employee, false);
                }
                return;
            }
            var opportunity = _autonomy.Opportunities.Find(decision.OpportunityId);
            if (opportunity == null)
            {
                _autonomy.NavigationCompleted(employee, false);
                return;
            }

            var target = opportunity.Position;
            bool assigned = TryAssignTaskDestinationCore(
                new Vector3(target.X, target.Y, target.Z),
                new EmployeeIntent(EmployeeIntentPurpose.AutonomousActivity,
                    decision.Activity + ": " + decision.OpportunityName, decision.OpportunityId),
                null, true);
            if (!assigned) _autonomy.NavigationCompleted(employee, false);
        }

        private Vector3? _autonomousFacingPoint;

        private static AutonomyPosition AutonomyPositionAt(Vector3 position) =>
            new AutonomyPosition(position.x, position.y, position.z);

        public void UpdateAutonomyPosition()
        {
            if (_autonomy == null || Employee == null) return;
            _autonomy.UpdatePosition(Employee,
                AutonomyPositionAt(transform.position - Vector3.up * (_navAgent != null ? _navAgent.baseOffset : 0f)));
        }

        private void HandleAutonomousActivityCancelled(Employee employee)
        {
            if (Employee != employee || !_autonomousRoute) return;
            _autonomousRoute = false;
            _autonomousFacingPoint = null;
            _hasExplicitTask = false;
            _onArrivalCallback = null;
            if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
                _navAgent.ResetPath();
        }

        private void CheckExplicitArrival()
        {
            if (_autonomousRoute && !_navAgent.pathPending &&
                (!_navAgent.hasPath || _navAgent.pathStatus == NavMeshPathStatus.PathInvalid))
            {
                _hasExplicitTask = false;
                _autonomousRoute = false;
                _autonomousFacingPoint = null;
                _navAgent.ResetPath();
                _autonomy?.NavigationCompleted(Employee, false);
                return;
            }
            if (!_autonomousRoute &&
                (!_navAgent.hasPath || _navAgent.pathStatus != NavMeshPathStatus.PathComplete))
            {
                RecoverExplicitTaskPath();
                if (!_hasExplicitTask) return;
            }
            if (!IsHeld && _navAgent.isOnNavMesh && !_navAgent.pathPending &&
                Vector3.Distance(transform.position - Vector3.up * _navAgent.baseOffset, _taskDestination) <= _navAgent.stoppingDistance + 0.25f)
            {
                ForceCompleteArrival();
            }
        }

        private void RecoverExplicitTaskPath()
        {
            float delta = SilverScreen.Presentation.SimulationTime.LocalPresentationTime.Delta(
                UnityEngine.Time.unscaledDeltaTime, _timeService);
            _taskPathRecoveryElapsed += delta;
            if (_taskPathRecoveryElapsed >= TaskPathRecoveryLimit)
            {
                FailExplicitTaskNavigation();
                return;
            }
            if (_navAgent.pathPending) return;

            _taskPathRetryTimer -= delta;
            if (_taskPathRetryTimer > 0) return;

            bool recovered = false;
            if (_navAgent.isOnNavMesh)
            {
                var taskPath = GetTaskPath();
                recovered = _navAgent.CalculatePath(_taskDestination, taskPath) &&
                    taskPath.status == NavMeshPathStatus.PathComplete &&
                    _navAgent.SetPath(taskPath);
            }
            if (recovered)
            {
                _taskPathRetryTimer = 0;
                _taskPathRetryDelay = InitialTaskPathRetryDelay;
                _taskPathRecoveryElapsed = 0;
                return;
            }

            _taskPathRetryTimer = _taskPathRetryDelay;
            _taskPathRetryDelay = Mathf.Min(_taskPathRetryDelay * 2f, MaximumTaskPathRetryDelay);
        }

        private void FailExplicitTaskNavigation()
        {
            var failedIntent = Employee?.CurrentIntent;
            var onNavigationFailed = _onNavigationFailed;
            _hasExplicitTask = false;
            _hasTaskAnchor = false;
            IsReturningAfterDrop = false;
            _autonomousRoute = false;
            _onArrivalCallback = null;
            _onNavigationFailed = null;
            if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
                _navAgent.ResetPath();
            Employee?.SetState(EmployeeState.Idle);
            Employee?.SetIntent(EmployeeIntent.None);
            if (onNavigationFailed != null)
                onNavigationFailed();
            else
                Debug.LogWarning("Explicit task navigation remained unreachable for ten simulated minutes; the task was released without an owner failure callback. " + failedIntent, this);
        }

        private bool TryFindRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
        {
            for (int i = 0; i < 15; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    if (_idleBuildingExclusion.HasValue)
                    {
                        var area = _idleBuildingExclusion.Value;
                        if (hit.position.x >= area.min.x && hit.position.x <= area.max.x &&
                            hit.position.z >= area.min.z && hit.position.z <= area.max.z) continue;
                    }
                    result = hit.position;
                    return true;
                }
            }

            result = center;
            return false;
        }
    }
}
