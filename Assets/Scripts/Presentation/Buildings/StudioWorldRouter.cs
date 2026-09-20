using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public class StudioWorldRouter : MonoBehaviour, IStudioWorldRouter
    {
        [SerializeField] private StudioEmployeeManager _employeeManager;

        private readonly Dictionary<BuildingType, StudioBuildingView> _buildingCache =
            new Dictionary<BuildingType, StudioBuildingView>();
        private readonly Dictionary<string, StudioBuildingView> _facilityCache =
            new Dictionary<string, StudioBuildingView>(StringComparer.Ordinal);
        private IMovieProductionService _productionService;
        private BeatPerformanceGenerator _performanceGenerator;

        public void BindProductionService(IMovieProductionService productionService)
        {
            _productionService = productionService;
            foreach (StudioBuildingView facility in _facilityCache.Values)
                facility?.GetComponent<LiveFilmingPlayback>()?.BindProductionService(productionService);
        }

        private void Awake()
        {
            ResolveEmployeeManager();
            CacheBuildings();
        }

        public void RefreshBuildings() => CacheBuildings();

        private void CacheBuildings()
        {
            _buildingCache.Clear();
            _facilityCache.Clear();
            foreach (StudioBuildingView building in FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude))
            {
                _buildingCache[building.BuildingType] = building;
                string facilityId = GetFacilityId(building);
                if (string.IsNullOrWhiteSpace(facilityId)) continue;
                _facilityCache[facilityId] = building;
                EnsureFilmingPresentation(building, facilityId);
            }
        }

        private void EnsureFilmingPresentation(StudioBuildingView building, string facilityId)
        {
            bool specialized = building.GetComponent<SpecializedSetFacilityView>() != null;
            float filmingZ = specialized
                ? building.BuildingType == BuildingType.RestaurantCafeSet ? -5.5f : 0f
                : -11f;
            float stationZ = specialized ? filmingZ - 2.5f : -8.2f;

            var stations = building.GetComponent<ProductionStationLayout>() ??
                           building.gameObject.AddComponent<ProductionStationLayout>();
            stations.EnsurePrototypeStations(stationZ);
            var marks = building.GetComponent<ActorSceneMarkLayout>() ??
                        building.gameObject.AddComponent<ActorSceneMarkLayout>();
            marks.EnsurePrototypeMarks(filmingZ);
            var slatePositions = building.GetComponent<SlatePositionLayout>() ??
                                 building.gameObject.AddComponent<SlatePositionLayout>();
            slatePositions.EnsurePrototypePositions(filmingZ, stationZ);
            var blockingPoints = building.GetComponent<SetBlockingPointLayout>() ??
                                 building.gameObject.AddComponent<SetBlockingPointLayout>();
            blockingPoints.EnsurePrototypePoints(filmingZ);

            var timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
            var slate = building.GetComponent<PrototypeSlateSequence>() ??
                        building.gameObject.AddComponent<PrototypeSlateSequence>();
            slate.Initialize(slatePositions, timeDriver?.TimeService);
            var camera = building.GetComponent<PrototypeProductionCamera>() ??
                         building.gameObject.AddComponent<PrototypeProductionCamera>();
            camera.EnsureCamera();
            var live = building.GetComponent<LiveFilmingPlayback>() ??
                       building.gameObject.AddComponent<LiveFilmingPlayback>();
            live.Initialize(timeDriver?.TimeService, _employeeManager, marks, blockingPoints, camera, facilityId);
            live.BindProductionService(_productionService);
            var beats = building.GetComponent<PrototypeScreenplayBeatSequence>() ??
                        building.gameObject.AddComponent<PrototypeScreenplayBeatSequence>();
            _performanceGenerator ??= new BeatPerformanceGenerator(new SystemPerformanceRandomSource());
            beats.Initialize(_employeeManager, timeDriver?.TimeService, blockingPoints, _performanceGenerator, camera, facilityId);
        }

        private static string GetFacilityId(StudioBuildingView building)
        {
            var specialized = building.GetComponent<SpecializedSetFacilityView>();
            if (specialized != null && !string.IsNullOrWhiteSpace(specialized.FacilityId))
                return specialized.FacilityId;
            return building.BuildingType == BuildingType.SoundStage
                ? StudioFilmingCapabilities.StarterStageId
                : null;
        }

        public bool TryGetBuildingPosition(BuildingType buildingType, out Vector3 position)
        {
            if (_buildingCache.Count == 0) CacheBuildings();
            if (_buildingCache.TryGetValue(buildingType, out StudioBuildingView building) && building != null)
            {
                position = building.InteractionPosition;
                return true;
            }
            position = default;
            return false;
        }

        public StudioRouteResult SendEmployeeToBuilding(
            Employee employee, BuildingType buildingType, EmployeeIntent intent, Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;
            if (!TryGetAgent(employee, out EmployeeAgent agent)) return StudioRouteResult.AgentMissing;
            if (!TryGetBuildingPosition(buildingType, out Vector3 target)) return StudioRouteResult.BuildingMissing;
            return agent.TryAssignTaskDestination(target, intent, onArrival)
                ? StudioRouteResult.Started
                : StudioRouteResult.NavigationRejected;
        }

        public StudioRouteResult SendEmployeeToFacility(
            Employee employee, string facilityId, EmployeeIntent intent, Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;
            if (!TryGetAgent(employee, out EmployeeAgent agent)) return StudioRouteResult.AgentMissing;
            if (!TryGetFacility(facilityId, out StudioBuildingView facility)) return StudioRouteResult.BuildingMissing;
            return agent.TryAssignTaskDestination(facility.InteractionPosition, intent, onArrival)
                ? StudioRouteResult.Started
                : StudioRouteResult.NavigationRejected;
        }

        public StudioRouteResult SendEmployeeToProductionStation(
            Employee employee, string facilityId, ProductionStationType stationType,
            EmployeeIntent intent, Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;
            if (!TryGetAgent(employee, out EmployeeAgent agent)) return StudioRouteResult.AgentMissing;
            if (!TryGetFacility(facilityId, out StudioBuildingView facility)) return StudioRouteResult.BuildingMissing;
            var layout = facility.GetComponent<ProductionStationLayout>();
            if (layout == null || !layout.TryGetPosition(stationType, out Vector3 target))
                return StudioRouteResult.StationMissing;
            return agent.TryAssignTaskDestination(target, intent, onArrival)
                ? StudioRouteResult.Started
                : StudioRouteResult.NavigationRejected;
        }

        public StudioRouteResult SendEmployeeToSceneMark(
            Employee employee, string facilityId, ActorSceneMarkType markType,
            EmployeeIntent intent, Action onArrival)
        {
            if (employee == null) return StudioRouteResult.EmployeeMissing;
            if (!TryGetAgent(employee, out EmployeeAgent agent)) return StudioRouteResult.AgentMissing;
            if (!TryGetFacility(facilityId, out StudioBuildingView facility)) return StudioRouteResult.BuildingMissing;
            var layout = facility.GetComponent<ActorSceneMarkLayout>();
            if (layout == null || !layout.TryGetPosition(markType, out Vector3 target))
                return StudioRouteResult.MarkMissing;
            return agent.TryAssignTaskDestination(target, intent, onArrival)
                ? StudioRouteResult.Started
                : StudioRouteResult.NavigationRejected;
        }

        public Employee ResolveEmployee(string employeeId, Employee compatibilityReference)
        {
            ResolveEmployeeManager();
            return _employeeManager?.GetEmployee(employeeId);
        }

        public StudioRouteResult StartSlateSequence(
            string facilityId, Action onCompleted, Action<StudioRouteResult> onFailed)
        {
            if (!TryGetFacility(facilityId, out StudioBuildingView facility)) return StudioRouteResult.BuildingMissing;
            var sequence = facility.GetComponent<PrototypeSlateSequence>();
            if (sequence == null) return StudioRouteResult.SlateMissing;
            return sequence.TryBegin(
                _productionService?.ActiveSlateMovieTitle,
                _productionService?.ActiveSlateSceneNumber,
                _productionService?.ActiveSlateTakeNumber,
                onCompleted,
                onFailed);
        }

        public StudioRouteResult StartBeatSequence(
            string facilityId, MovieProject movie, MovieScene scene, MovieTake take,
            Action onCompleted, Action<StudioRouteResult> onFailed)
        {
            if (!TryGetFacility(facilityId, out StudioBuildingView facility)) return StudioRouteResult.BuildingMissing;
            var sequence = facility.GetComponent<PrototypeScreenplayBeatSequence>();
            return sequence != null
                ? sequence.TryBegin(movie, scene, take, onCompleted, onFailed)
                : StudioRouteResult.PerformanceMissing;
        }

        public void ReleaseEmployee(Employee employee)
        {
            if (employee == null || !TryGetAgent(employee, out EmployeeAgent agent)) return;
            agent.ClearTaskDestination();
        }

        private bool TryGetFacility(string facilityId, out StudioBuildingView facility)
        {
            if (_facilityCache.Count == 0) CacheBuildings();
            facility = null;
            return !string.IsNullOrWhiteSpace(facilityId) &&
                   _facilityCache.TryGetValue(facilityId, out facility) && facility != null;
        }

        private bool TryGetAgent(Employee employee, out EmployeeAgent agent)
        {
            ResolveEmployeeManager();
            agent = _employeeManager?.GetAgent(employee);
            return agent != null;
        }

        private void ResolveEmployeeManager()
        {
            if (_employeeManager == null)
                _employeeManager = GetComponent<StudioEmployeeManager>() ?? FindAnyObjectByType<StudioEmployeeManager>();
        }
    }
}
