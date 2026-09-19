using System;
using UnityEngine;
using UnityEngine.AI;
using SilverScreen.Domain;
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

        private ISimulationTimeService _timeService;
        private Vector3 _homeCenter;
        private float _idleWaitTimer;
        private bool _hasExplicitTask;
        private Action _onArrivalCallback;

        private float _baseSpeed = 3.5f;
        private float _baseAcceleration = 14f;
        private GameObject _performanceVisual;
        private Transform _performanceLeftArm;
        private Transform _performanceRightArm;
        private float _performanceElapsed;

        private void Awake()
        {
            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();
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
            if (_timeService != null)
            {
                _timeService.OnSpeedChanged -= HandleSpeedChanged;
            }
        }

        public void BindDomain(Employee employee)
        {
            Employee = employee;
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
            if (_navAgent == null || !_navAgent.isOnNavMesh) return;

            if (speed == SimulationSpeed.Paused)
            {
                _navAgent.isStopped = true;
                _navAgent.velocity = Vector3.zero;
            }
            else
            {
                _navAgent.isStopped = false;
                float multiplier = _timeService != null ? _timeService.TimeScaleMultiplier : 1f;
                _navAgent.speed = _baseSpeed * multiplier;
                _navAgent.acceleration = _baseAcceleration * multiplier;
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
            if (Employee == null) return;

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
                CheckWanderArrival();
            }
        }

        private void UpdatePerformancePresentation()
        {
            bool performing = Employee.CurrentState == EmployeeState.Filming &&
                              Employee.Role == EmployeeRole.Actor;
            if (!performing)
            {
                _performanceElapsed = 0f;
                if (_performanceVisual != null) _performanceVisual.SetActive(false);
                return;
            }

            EnsurePerformanceVisual();
            _performanceVisual.SetActive(true);

            float speedMultiplier = _timeService != null ? _timeService.TimeScaleMultiplier : 1f;
            _performanceElapsed += UnityEngine.Time.deltaTime * speedMultiplier;
            float gesture = Mathf.Sin(_performanceElapsed * 4f);
            _performanceLeftArm.localRotation = Quaternion.Euler(0f, 0f, -35f + gesture * 28f);
            _performanceRightArm.localRotation = Quaternion.Euler(0f, 0f, 35f - gesture * 28f);
            _performanceVisual.transform.localRotation =
                Quaternion.Euler(0f, Mathf.Sin(_performanceElapsed * 2f) * 8f, 0f);
        }

        private void EnsurePerformanceVisual()
        {
            if (_performanceVisual != null) return;

            _performanceVisual = new GameObject("PrototypePerformanceGesture");
            _performanceVisual.transform.SetParent(transform, false);

            _performanceLeftArm = CreateGestureArm("LeftArm", new Vector3(-0.38f, 0.32f, 0f));
            _performanceRightArm = CreateGestureArm("RightArm", new Vector3(0.38f, 0.32f, 0f));
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
            float speedMultiplier = _timeService != null ? _timeService.TimeScaleMultiplier : 1f;
            _idleWaitTimer -= UnityEngine.Time.deltaTime * speedMultiplier;

            if (_idleWaitTimer <= 0f)
            {
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

        private void CheckWanderArrival()
        {
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.1f)
            {
                Employee.SetState(EmployeeState.Idle);
                Employee.SetIntent(EmployeeIntent.None);
                _idleWaitTimer = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
            }
        }

        public bool TryAssignTaskDestination(Vector3 destination, EmployeeIntent intent, Action onArrival = null)
        {
            if (_navAgent == null || !_navAgent.isActiveAndEnabled || !_navAgent.isOnNavMesh)
            {
                return false;
            }
            var path = new NavMeshPath();
            if (!_navAgent.CalculatePath(destination, path) ||
                path.status != NavMeshPathStatus.PathComplete ||
                !_navAgent.SetPath(path))
            {
                return false;
            }

            _hasExplicitTask = true;
            _onArrivalCallback = onArrival;

            if (Employee != null)
            {
                Employee.SetState(EmployeeState.Walking);
                Employee.SetIntent(intent);
            }

            if (_timeService != null && _timeService.IsPaused)
            {
                _navAgent.isStopped = true;
            }

            return true;
        }

        public void ClearTaskDestination()
        {
            _hasExplicitTask = false;
            _onArrivalCallback = null;

            if (Employee != null)
            {
                Employee.SetState(EmployeeState.Idle);
                Employee.SetIntent(EmployeeIntent.None);
            }

            _idleWaitTimer = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
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
                callback?.Invoke();
            }
        }

        private void CheckExplicitArrival()
        {
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.2f)
            {
                ForceCompleteArrival();
            }
        }

        private bool TryFindRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
        {
            for (int i = 0; i < 15; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = center;
            return false;
        }
    }
}

