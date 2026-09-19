using System;
using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;

namespace SilverScreen.Presentation.Buildings
{
    public class StudioWorldRouter : MonoBehaviour, IStudioWorldRouter
    {
        [SerializeField] private StudioEmployeeManager _employeeManager;

        private readonly Dictionary<BuildingType, StudioBuildingView> _buildingCache = new Dictionary<BuildingType, StudioBuildingView>();

        private void Awake()
        {
            if (_employeeManager == null)
            {
                _employeeManager = GetComponent<StudioEmployeeManager>();
                if (_employeeManager == null)
                {
                    _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
                }
            }

            CacheBuildings();
        }

        public void RefreshBuildings()
        {
            CacheBuildings();
        }

        private void CacheBuildings()
        {
            _buildingCache.Clear();
            var buildings = FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude);
            foreach (var b in buildings)
            {
                _buildingCache[b.BuildingType] = b;
                if (b.BuildingType == BuildingType.SoundStage)
                {
                    var stations = b.GetComponent<ProductionStationLayout>();
                    if (stations == null) stations = b.gameObject.AddComponent<ProductionStationLayout>();
                    stations.EnsurePrototypeStations();
                }
            }
        }

        public bool TryGetBuildingPosition(BuildingType buildingType, out Vector3 position)
        {
            if (_buildingCache.Count == 0) CacheBuildings();

            if (_buildingCache.TryGetValue(buildingType, out var building) && building != null)
            {
                position = building.InteractionPosition;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public StudioRouteResult SendEmployeeToBuilding(Employee employee, BuildingType buildingType, EmployeeIntent intent, Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;

            if (_employeeManager == null)
            {
                _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            }

            if (_employeeManager == null) return StudioRouteResult.AgentMissing;

            var agent = _employeeManager.GetAgent(employee);
            if (agent == null)
            {
                Debug.LogWarning($"[StudioWorldRouter] No EmployeeAgent found for {employee.Name}");
                return StudioRouteResult.AgentMissing;
            }

            if (TryGetBuildingPosition(buildingType, out Vector3 targetPos))
            {
                if (agent.TryAssignTaskDestination(targetPos, intent, onArrival))
                {
                    return StudioRouteResult.Started;
                }

                Debug.LogWarning($"[StudioWorldRouter] Navigation could not start for {employee.Name}");
                return StudioRouteResult.NavigationRejected;
            }

            Debug.LogWarning($"[StudioWorldRouter] No building found of type {buildingType}");
            return StudioRouteResult.BuildingMissing;
        }

        public StudioRouteResult SendEmployeeToProductionStation(
            Employee employee,
            BuildingType buildingType,
            ProductionStationType stationType,
            EmployeeIntent intent,
            Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;
            if (_employeeManager == null) _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            if (_employeeManager == null) return StudioRouteResult.AgentMissing;

            var agent = _employeeManager.GetAgent(employee);
            if (agent == null) return StudioRouteResult.AgentMissing;

            if (_buildingCache.Count == 0) CacheBuildings();
            if (!_buildingCache.TryGetValue(buildingType, out var building) || building == null)
                return StudioRouteResult.BuildingMissing;

            var stations = building.GetComponent<ProductionStationLayout>();
            if (stations == null || !stations.TryGetPosition(stationType, out Vector3 targetPosition))
            {
                Debug.LogWarning($"[StudioWorldRouter] No {stationType} station found at {building.DisplayName}");
                return StudioRouteResult.StationMissing;
            }

            if (!agent.TryAssignTaskDestination(targetPosition, intent, onArrival))
            {
                Debug.LogWarning($"[StudioWorldRouter] Navigation to {stationType} could not start for {employee.Name}");
                return StudioRouteResult.NavigationRejected;
            }

            return StudioRouteResult.Started;
        }
        public void ReleaseEmployee(Employee employee)
        {
            if (_employeeManager == null)
            {
                _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            }

            if (_employeeManager == null || employee == null) return;

            var agent = _employeeManager.GetAgent(employee);
            if (agent != null)
            {
                agent.ClearTaskDestination();
            }
        }
    }
}
