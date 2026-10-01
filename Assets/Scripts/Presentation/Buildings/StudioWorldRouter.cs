using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public class StudioWorldRouter : MonoBehaviour, IStudioWorldRouter, IProductionPresentation
    {
        [SerializeField] private StudioEmployeeManager _employeeManager;

        private readonly Dictionary<BuildingType, StudioBuildingView> _buildingCache =
            new Dictionary<BuildingType, StudioBuildingView>();
        private readonly Dictionary<string, StudioBuildingView> _facilityCache =
            new Dictionary<string, StudioBuildingView>(StringComparer.Ordinal);
        private IMovieProductionService _productionService;
        private readonly Dictionary<string, IPresentationSession> _presentations = new Dictionary<string, IPresentationSession>(StringComparer.Ordinal);

        public IPresentationSession Begin(TakePresentationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (_presentations.TryGetValue(snapshot.FacilityId, out var previous)) previous.Dispose();
            TryGetFacility(snapshot.FacilityId, out var facility);
            var session = new StudioTakePresentationSession(snapshot, facility?.GetComponent<PrototypeSlateSequence>(), facility?.GetComponent<PrototypeScreenplayBeatSequence>());
            _presentations[snapshot.FacilityId] = session;
            return session;
        }

        private void OnDisable()
        {
            foreach (var session in _presentations.Values) session.Dispose();
            _presentations.Clear();
        }

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
            var stageNumbers = new HashSet<int>();
            foreach (StudioBuildingView building in FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude))
            {
                // Construction can reveal real renderers while its operational view remains gated.
                if (!building.isActiveAndEnabled) continue;
                _buildingCache[building.BuildingType] = building;
                string facilityId = GetFacilityId(building);
                if (string.IsNullOrWhiteSpace(facilityId)) continue;
                var stageIdentity = building.GetComponent<SoundStageIdentity>();
                if (stageIdentity != null && !stageNumbers.Add(stageIdentity.StageNumber))
                {
                    Debug.LogError("Duplicate Sound Stage number: " + stageIdentity.StageNumber, building);
                    continue;
                }
                if (_facilityCache.ContainsKey(facilityId))
                {
                    Debug.LogError("Duplicate filming facility identity: " + facilityId, building);
                    continue;
                }
                _facilityCache.Add(facilityId, building);
                EnsureFilmingPresentation(building, facilityId);
            }
        }

        private void EnsureFilmingPresentation(StudioBuildingView building, string facilityId)
        {
            var authored = building.GetComponent<AuthoredProductionLayout>();
            var slatePositions = building.GetComponent<SlatePositionLayout>();
            if (authored != null)
            {
                if (!authored.IsComplete())
                {
                    Debug.LogError("Incomplete authored production layout: " + facilityId, building);
                    return;
                }
            }
            else
            {
                bool specialized = building.GetComponent<SpecializedSetFacilityView>() != null;
                float stageStationZ = building.transform.InverseTransformPoint(building.InteractionPosition).z - 1.5f;
                float filmingZ = specialized
                    ? building.BuildingType == BuildingType.RestaurantCafeSet ? -5.5f : 0f
                    : stageStationZ - 2.8f;
                float stationZ = specialized ? filmingZ - 2.5f : stageStationZ;
    
                var stations = building.GetComponent<ProductionStationLayout>() ??
                               building.gameObject.AddComponent<ProductionStationLayout>();
                stations.EnsurePrototypeStations(stationZ);
                var prototypeMarks = building.GetComponent<ActorSceneMarkLayout>() ??
                            building.gameObject.AddComponent<ActorSceneMarkLayout>();
                prototypeMarks.EnsurePrototypeMarks(filmingZ);
                slatePositions = building.GetComponent<SlatePositionLayout>() ??
                                     building.gameObject.AddComponent<SlatePositionLayout>();
                slatePositions.EnsurePrototypePositions(filmingZ, stationZ);
                var prototypeBlocking = building.GetComponent<SetBlockingPointLayout>() ??
                                     building.gameObject.AddComponent<SetBlockingPointLayout>();
                prototypeBlocking.EnsurePrototypePoints(filmingZ);
    
            }
            var marks = building.GetComponent<ActorSceneMarkLayout>();
            var blockingPoints = building.GetComponent<SetBlockingPointLayout>();

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
            beats.Initialize(_employeeManager, timeDriver?.TimeService, blockingPoints, camera, facilityId);
        }

        private static string GetFacilityId(StudioBuildingView building)
        {
            var specialized = building.GetComponent<SpecializedSetFacilityView>();
            if (specialized != null && !string.IsNullOrWhiteSpace(specialized.FacilityId))
                return specialized.FacilityId;
            if (building.BuildingType != BuildingType.SoundStage) return null;
            var stage = building.GetComponent<SoundStageIdentity>();
            if (stage == null || string.IsNullOrWhiteSpace(stage.FacilityId) || stage.StageNumber < 1)
            {
                Debug.LogError("Sound Stage has no configured instance identity.", building);
                return null;
            }
            return stage.FacilityId;
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
            if (layout == null || !layout.TryGetPose(stationType, out Pose pose))
                return StudioRouteResult.StationMissing;
            Action arrived = onArrival;
            if (facility.GetComponent<AuthoredProductionLayout>() != null)
                arrived = () => { agent.transform.rotation = pose.rotation; onArrival?.Invoke(); };
            return agent.TryAssignTaskDestination(pose.position, intent, arrived)
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
            if (layout == null || !layout.TryGetPose(markType, out Pose pose))
                return StudioRouteResult.MarkMissing;
            Action arrived = onArrival;
            if (facility.GetComponent<AuthoredProductionLayout>() != null)
                arrived = () => { agent.transform.rotation = pose.rotation; onArrival?.Invoke(); };
            return agent.TryAssignTaskDestination(pose.position, intent, arrived)
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
                ? sequence.TryBegin(new TakePresentationSnapshot(movie, scene, take, facilityId, 0), onCompleted, onFailed)
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
