using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Tutorial;
using SilverScreen.Presentation.Writing;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    public sealed class StudioConstructionDriver : MonoBehaviour
    {
        [SerializeField] private ConstructionVisualKit _constructionVisualKit;
        private readonly Dictionary<string, StudioBuildingView> _templates = new Dictionary<string, StudioBuildingView>();
        private readonly Dictionary<string, ConstructionSiteView> _sites = new Dictionary<string, ConstructionSiteView>();
        private readonly Dictionary<string, EmployeeAgent> _workers = new Dictionary<string, EmployeeAgent>();
        private readonly List<CandidateWaitingAreaView> _areas = new List<CandidateWaitingAreaView>();
        private SimulationTimeDriver _time;
        private StudioEmployeeManager _employees;
        private ScreenplayWritingDriver _writing;
        private StudioGuidanceDriver _guidance;
        private StudioWorldRouter _router;
        private NavMeshSurface _surface;
        private NavMeshData _authoredNavigation;
        private float _initialVoxelSize;
        private bool _initialOverrideVoxelSize;
        private Material _siteMaterial;
        private bool _dispatching;
        private float _nextRetry;
        private Coroutine _navigationRoutine;
        private bool _navigationDirty;
        private readonly List<Action> _navigationReady = new List<Action>();
        private readonly Dictionary<string, int> _failedRoutePhases = new Dictionary<string, int>(StringComparer.Ordinal);
        public BuildingConstructionService Service { get; private set; }
        public FacilityApplicantPool ApplicantFacilities { get; } = new FacilityApplicantPool();
        public IReadOnlyList<CandidateWaitingAreaView> ApplicantAreas => _areas;
        public StudioBuildLot Lot { get; private set; }
        public IReadOnlyDictionary<string, ConstructionSiteView> Sites => _sites;
        public bool NavigationUpdatePending => _navigationRoutine != null || _navigationDirty;
        public StudioBuildingView PlacementTemplate(string definitionId) => _templates.TryGetValue(definitionId, out var view) ? view : null;

        public void Initialize(SimulationTimeDriver time, StudioEmployeeManager employees, ScreenplayWritingDriver writing, StudioGuidanceDriver guidance)
        {
            if (Service != null) return;
            _time = time; _employees = employees; _writing = writing; _guidance = guidance;
            _router = FindAnyObjectByType<StudioWorldRouter>();
            _surface = FindAnyObjectByType<NavMeshSurface>();
            if (_surface == null) throw new InvalidOperationException("New studio construction requires the existing lot NavMeshSurface.");
            _authoredNavigation = _surface.navMeshData;
            _initialVoxelSize = _surface.voxelSize; _initialOverrideVoxelSize = _surface.overrideVoxelSize;
            Lot = _surface.GetComponent<StudioBuildLot>() ?? _surface.gameObject.AddComponent<StudioBuildLot>();
            Lot.ResolveBuildSurfaces();
            var definitions = StudioBuildingDefinitions.Create();
            foreach (var view in FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Include))
            {
                string id = view.BuildingType == BuildingType.StudioOffice ? StarterFeatureIds.Headquarters :
                    view.BuildingType == BuildingType.CastingOffice ? StarterFeatureIds.Casting :
                    view.GetComponent<SoundStageIdentity>()?.FacilityId == StudioFilmingCapabilities.StarterStageId ? StarterFeatureIds.Stage : null;
                if (id == null || _templates.ContainsKey(id)) continue;
                _templates[id] = view; view.gameObject.SetActive(false);
            }
            // Remove prototype support facilities from a fresh lot; their systems remain reusable.
            foreach (var office in FindObjectsByType<ScriptOfficeStationLayout>(FindObjectsInactive.Include)) office.gameObject.SetActive(false);
            foreach (var school in FindObjectsByType<StageSchoolView>(FindObjectsInactive.Include)) school.gameObject.SetActive(false);
            // Resource prefab is a template only. Placement still enters the shared site lifecycle.
            var stageSchool = Resources.Load<GameObject>("StageSchool_A4_Runtime");
            if(stageSchool != null) _templates[StudioBuildingDefinitions.StageSchool] = stageSchool.GetComponent<StudioBuildingView>();
            _writing.FilmingCapabilities.RemoveFacility(StudioFilmingCapabilities.StarterStageId);
            _siteMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.65f, .47f, .20f) };
            var servicePrefab = Resources.Load<StudioServicesFacility>("StudioServices_Live");
            if (servicePrefab == null) throw new InvalidOperationException("Studio Services production prefab is missing.");
            var reserved = new List<PlacementRect>(Lot.Forbidden) { servicePrefab.ReservationAt(Lot.ServicePosition, Lot.ServiceRotation) };
            Service = new BuildingConstructionService(definitions, Lot.Buildable, reserved, _time.Clock, _time.Work,
                GetComponent<StudioEconomyDriver>().FinanceService, _time.Reservations,
                id => _templates.ContainsKey(id) && _guidance.Tutorial.Allows(id), EntranceReachable, Lot.ValidateWorld);
            Service.SitePlaced += CreateSite; Service.Changed += SiteChanged;
            Service.Operational += ActivateBuilding; Service.Cancelled += CancelSite;
            _guidance.BindConstruction(Service);
            GetComponent<StudioEconomyDriver>().BuildingProvider = () => Service.Buildings.Where(b => b.IsOperational).Select(b =>
                SilverScreen.Domain.Finance.BuildingUpkeepDefinition.ForType(b.StageNumber > 0 ? BuildingType.SoundStage :
                    b.Definition.Id == StarterFeatureIds.Casting ? BuildingType.CastingOffice :
                    b.Definition.Id == StudioBuildingDefinitions.StageSchool ? BuildingType.StageSchool : BuildingType.StudioOffice));
            CreateServiceFacility();
            RebuildNavigation(); _router?.RefreshBuildings();
            _employees.OnEmployeeAdded += EmployeeAdded;
            _employees.OnEmployeeRemoved += EmployeeRemoved;
            _employees.BeforeEmploymentChange += BeforeEmploymentChange;
            foreach (var employee in _employees.AllEmployees) SubscribeEmployee(employee);
            gameObject.AddComponent<StudioBuildMode>().Initialize(this);
        }
        public bool EntranceReachable(BuildingDefinition definition, BuildingPose pose)
        {
            var p = pose.Transform(definition.ExteriorApproach);
            if (!NavMesh.SamplePosition(new Vector3(p.X, 0, p.Z), out var target, .6f, NavMesh.AllAreas)) return false;
            var sourcePosition = Lot.StartingFacility != null ? Lot.StartingFacility.Anchor("OfficeApproach").position :
                Lot.ServicePosition + Lot.ServiceRotation * Resources.Load<StudioServicesFacility>("StudioServices_Live").Anchor("OfficeApproach").localPosition;
            if (!NavMesh.SamplePosition(sourcePosition, out var source, 1f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(source.position, target.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        private void CreateServiceFacility()
        {
            // The lot top and authored finished floor both use Y=0. Seat the slab 2 cm
            // proud of the lot to avoid coplanar flicker while its foundation stays buried.
            var facility = Lot.StartingFacility != null ? Lot.StartingFacility :
                Instantiate(Resources.Load<StudioServicesFacility>("StudioServices_Live"), Lot.ServicePosition + Vector3.up * .02f, Lot.ServiceRotation);
            facility.name = "Studio Services";
            var area = facility.Register(ApplicantFacilities);
            _areas.Add(area);
            FindAnyObjectByType<RecruitmentDriver>()?.WorldRouter?.RegisterArea(area);
        }
        private void RegisterApplicants(Transform parent, string id, LotPoint approach, RecruitmentDestination destination, IEnumerable<ProfessionalRole> professions)
        {
            var roles = professions.ToArray(); if (roles.Length == 0) return;
            var area = parent.gameObject.AddComponent<CandidateWaitingAreaView>();
            Transform Marker(string name, float x, float z)
            { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = new Vector3(x, 0, z); return t; }
            var entrance = Marker("ApplicantEntrance", approach.X, approach.Z);
            var waits = Enumerable.Range(0, 5).Select(i => Marker("ApplicantWait" + i, approach.X + (i - 2) * 1.2f, approach.Z - 1)).ToArray();
            area.ConfigureFacility(id, destination, entrance, Marker("ApplicantArrival", approach.X - 3, approach.Z - 3),
                Marker("ApplicantExit", approach.X + 3, approach.Z - 3), waits);
            SilverScreen.Presentation.Interaction.PersonInteractionSpot.CreateHiringSpots(parent, id, entrance, roles);
            _areas.Add(area);
            FindAnyObjectByType<RecruitmentDriver>()?.WorldRouter?.RegisterArea(area);
            ApplicantFacilities.Register(new RecruitmentFacility(id, destination, roles));
        }
        private void CreateSite(PlacedBuilding building)
        {
            var root = new GameObject("Construction " + building.Definition.DisplayName);
            root.transform.SetPositionAndRotation(new Vector3(building.Pose.X, 0, building.Pose.Z), Quaternion.Euler(0, building.Pose.Yaw, 0));
            var content = Instantiate(_templates[building.Definition.Id].gameObject, root.transform);
            content.transform.localPosition = Vector3.zero; content.transform.localRotation = Quaternion.identity;
            content.name = building.Definition.DisplayName;
            var identity = content.GetComponent<SoundStageIdentity>();
            if (identity != null) identity.InitializePlacedClone(building.Id, building.StageNumber);
            var view = root.AddComponent<ConstructionSiteView>(); view.Initialize(building, content, _siteMaterial, _constructionVisualKit);
            _sites.Add(building.Id, view);
            RequestNavigationUpdate(DispatchWorkers);
        }
        private void SiteChanged(PlacedBuilding b)
        {
            if (!_sites.TryGetValue(b.Id, out var site)) return;
            bool phaseChanged = site.AuthoritativePhase != b.Phase;
            site.Refresh();
            if (!phaseChanged || b.IsOperational || b.State == BuildingLifecycle.Cancelled) return;
            _failedRoutePhases.Remove(b.Id);
            // A new kind of ground-level activity means real travel, with zero work capacity
            // until the existing arrival callback admits the worker again.
            foreach (var pair in _workers.Where(p => p.Value != null && p.Value.Employee.CurrentIntent.TargetBuildingId == b.Id).ToArray())
            {
                ReleaseWorker(pair.Key);
            }
            DispatchWorkers();
        }
        private void ActivateBuilding(PlacedBuilding b)
        {
            var view = _sites[b.Id]; view.Refresh();
            if (b.StageNumber > 0)
                _writing.FilmingCapabilities.AddFacility(new FilmingFacilityCapabilities(b.Id, b.Definition.Capabilities, "Stage " + b.StageNumber));
            _router?.RefreshBuildings();
            RegisterApplicants(view.transform, b.Id, b.Definition.ExteriorApproach,
                RecruitmentDestination.CastingOffice, b.Definition.Applicants);
            if (b.Definition.Id == StarterFeatureIds.Headquarters) ActivateStarterWriting(view);
            _guidance.Announcements.Request(new AnnouncementRequest(AnnouncementType.BuildingCompleted, AnnouncementPriority.Important, b.Id, _time.Clock.Now.Seconds));
            ReleaseFinishedWorkers();
            RequestNavigationUpdate(() =>
            {
                if(b.Definition.Id==StudioBuildingDefinitions.StageSchool&&view!=null){
                    var infrastructure=view.GetComponentInChildren<StageSchoolInfrastructure>();
                    var recruitment=GetComponent<RecruitmentDriver>();
                    if(infrastructure!=null&&recruitment?.Coordinator!=null)
                        infrastructure.gameObject.AddComponent<StageSchoolApplicantFacility>().Initialize(b.Id,infrastructure,ApplicantFacilities,recruitment,()=>b.IsOperational);
                }
                _writing.MovieProductionService?.RetryUnresolvedEnvironment();
                DispatchWorkers();
            });
        }
        private void ActivateStarterWriting(ConstructionSiteView view)
        {
            // Reuse the existing writer system with local outdoor prototype desk anchors at HQ.
            var root = new GameObject("StarterWritingStations"); root.transform.SetParent(view.transform, false);
            var points = new List<Transform>(); var approach = view.Building.Definition.ExteriorApproach;
            for (int i = 0; i < 4; i++)
            { var t = new GameObject("WritingStation" + i).transform; t.SetParent(root.transform, false); t.localPosition = new Vector3(approach.X + (i - 1.5f) * 1.3f, 0, approach.Z - 1.8f); points.Add(t); }
            var layout = root.AddComponent<ScriptOfficeStationLayout>(); layout.Configure(points[0], points);
            _writing.GetComponent<ScreenplayWritingWorldRouter>().Initialize(_employees, layout);
            RegisterApplicants(view.transform, view.Building.Id + ":writing", approach, RecruitmentDestination.ScriptOffice, new[] { ProfessionalRole.Writer });
        }
        private void CancelSite(PlacedBuilding b)
        {
            if (_sites.TryGetValue(b.Id, out var site)) { site.gameObject.SetActive(false); Destroy(site.gameObject); _sites.Remove(b.Id); }
            ReleaseFinishedWorkers();
            RequestNavigationUpdate(DispatchWorkers);
        }
        private void EmployeeAdded(Employee employee) { SubscribeEmployee(employee); DispatchWorkers(); }
        private void EmployeeRemoved(Employee employee) { ReleaseWorker(employee.Id); employee.ProfessionChanged -= EmployeeProfessionChanged; DispatchWorkers(); }
        private void BeforeEmploymentChange(Employee employee) => ReleaseWorker(employee?.Id);
        private void SubscribeEmployee(Employee employee)
        {
            if (employee == null) return;
            employee.ProfessionChanged -= EmployeeProfessionChanged;
            employee.ProfessionChanged += EmployeeProfessionChanged;
        }
        private void EmployeeProfessionChanged(Employee employee, EmployeeRole previous, EmployeeRole current)
        {
            if (current != EmployeeRole.ConstructionWorker) ReleaseWorker(employee.Id);
            DispatchWorkers();
        }
        private void ReleaseWorker(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) return;
            Service?.ReleaseWorker(employeeId);
            if (_workers.TryGetValue(employeeId, out var agent))
            {
                if (agent != null) agent.ClearTaskDestination();
                _workers.Remove(employeeId);
            }
        }
        public void DispatchWorkers()
        {
            if (_dispatching || Service == null || !_time.Clock.IsBoundaryComplete) return;
            _dispatching = true;
            try
            {
                var activeSites = Service.Buildings.Where(b => !b.IsOperational && b.State != BuildingLifecycle.Cancelled &&
                    (!_failedRoutePhases.TryGetValue(b.Id, out int failedPhase) || failedPhase != b.PhaseIndex)).ToArray();
                foreach (var employee in _employees.AllEmployees)
                {
                    if (employee.Role != EmployeeRole.ConstructionWorker || Service.HasAssignment(employee.Id)) continue;
                    var agent = _employees.GetAgent(employee); if (agent == null || !agent.isActiveAndEnabled) continue;
                    var candidates = activeSites.Where(b => Service.AssignedCount(b.Id) < Service.CurrentWorkerCapacity(b))
                        .OrderBy(b => (decimal)Service.AssignedCount(b.Id) / Service.CurrentWorkerCapacity(b))
                        .ThenBy(b => b.Id, StringComparer.Ordinal).ToArray();
                    foreach (var b in candidates)
                    {
                        if (!Service.AssignWorker(b, employee, out int slot)) continue;
                        _workers[employee.Id] = agent;
                        if (!_sites.TryGetValue(b.Id, out var site)) { ReleaseWorker(employee.Id); continue; }
                        if (!site.TryGetActivity(slot, agent.GetComponent<NavMeshAgent>(), out var activity, out var target))
                        { ReleaseWorker(employee.Id); continue; }
                        var phase = b.Phase;
                        if (!agent.TryAssignTaskDestination(target, new EmployeeIntent(EmployeeIntentPurpose.PerformTask, activity.Kind + " / " + b.Definition.DisplayName, b.Id), () =>
                        {
                            if (b.Phase != phase) { ReleaseWorker(employee.Id); return; }
                            if (agent != null && Service.HasAssignment(employee.Id) && Vector3.Distance(agent.transform.position, target + Vector3.up) < 2f)
                            {
                                var direction = site.transform.TransformPoint(activity.LocalTarget) - agent.transform.position; direction.y = 0;
                                if (direction.sqrMagnitude > .001f) agent.transform.rotation = Quaternion.LookRotation(direction);
                                employee.SetState(EmployeeState.Working); Service.WorkerArrived(employee.Id);
                            }
                            else ReleaseWorker(employee.Id);
                        }, () => ConstructionRouteFailed(employee, agent, b, phase)))
                        { ReleaseWorker(employee.Id); continue; }
                        break;
                    }
                }
            }
            finally { _dispatching = false; }
        }
        private void ConstructionRouteFailed(Employee employee, EmployeeAgent agent, PlacedBuilding building, ConstructionPhase phase)
        {
            if (Service == null || employee == null || building == null ||
                building.Phase != phase || !Service.HasAssignment(employee.Id) ||
                !_workers.TryGetValue(employee.Id, out var assignedAgent) || assignedAgent != agent)
                return;
            _failedRoutePhases[building.Id] = building.PhaseIndex;
            ReleaseWorker(employee.Id);
            DispatchWorkers();
        }
        private void ReleaseFinishedWorkers()
        {
            foreach (var pair in _workers.ToArray())
                if (!Service.HasAssignment(pair.Key)) { if (pair.Value != null) pair.Value.ClearTaskDestination(); _workers.Remove(pair.Key); }
        }
        private void Update()
        {
            if (Service == null || !_time.Clock.IsBoundaryComplete || _time.Clock.IsPaused || Time.time < _nextRetry) return;
            _nextRetry = Time.time + 2;
            foreach (var pair in _workers.ToArray())
                if (pair.Value == null || !pair.Value.isActiveAndEnabled || pair.Value.Employee.Role != EmployeeRole.ConstructionWorker)
                    ReleaseWorker(pair.Key);
            ReleaseFinishedWorkers(); DispatchWorkers();
        }
        public void RebuildNavigation()
        {
            ConfigureNavigation();
            var previous = _surface.navMeshData;
            Physics.SyncTransforms(); _surface.BuildNavMesh();
            if (previous != null && previous != _authoredNavigation && previous != _surface.navMeshData) Destroy(previous);
        }
        private void ConfigureNavigation()
        {
            // Rotated thin masonry needs a finer raster: the focused 35-degree proof
            // failed the 5 mm contact limit at .05 m and passed at .025 m. Keep the
            // existing agent radius, geometry, continuous surface and no-link policy.
            bool detailedBuilding = FindAnyObjectByType<StudioServicesFacility>() != null ||
                Service.Buildings.Any(b => b.IsOperational && b.StageNumber > 0);
            // Services also has real door jambs and interior placement spots. The coarse
            // lot raster can lift the surface beyond the placement system's 15 cm tolerance.
            _surface.overrideVoxelSize = detailedBuilding || _initialOverrideVoxelSize;
            _surface.voxelSize = detailedBuilding ? Mathf.Min(_initialVoxelSize, .025f) : _initialVoxelSize;
        }
        private void RequestNavigationUpdate(Action whenReady = null)
        {
            if (whenReady != null) _navigationReady.Add(whenReady);
            _navigationDirty = true;
            if (_navigationRoutine == null) _navigationRoutine = StartCoroutine(UpdateNavigation());
        }
        private IEnumerator UpdateNavigation()
        {
            // Let all colliders/Destroy calls from the transition settle, then coalesce
            // requests made during the same frame into one update of the existing data.
            yield return null;
            while (_navigationDirty)
            {
                _navigationDirty = false;
                ConfigureNavigation();
                Physics.SyncTransforms();
                if (_surface.navMeshData == null)
                    RebuildNavigation();
                else
                {
                    var update = _surface.UpdateNavMesh(_surface.navMeshData);
                    if (update != null) yield return update;
                }
            }
            _navigationRoutine = null;
            _failedRoutePhases.Clear();
            var callbacks = _navigationReady.ToArray();
            _navigationReady.Clear();
            foreach (var callback in callbacks) callback?.Invoke();
        }
        private void OnDestroy()
        {
            if (_navigationRoutine != null) StopCoroutine(_navigationRoutine);
            _navigationRoutine = null; _navigationReady.Clear();
            if (_employees != null)
            {
                _employees.OnEmployeeAdded -= EmployeeAdded;
                _employees.OnEmployeeRemoved -= EmployeeRemoved;
                _employees.BeforeEmploymentChange -= BeforeEmploymentChange;
                foreach (var employee in _employees.AllEmployees) employee.ProfessionChanged -= EmployeeProfessionChanged;
            }
            if (Service != null) { Service.SitePlaced -= CreateSite; Service.Changed -= SiteChanged; Service.Operational -= ActivateBuilding; Service.Cancelled -= CancelSite; Service.Dispose(); }
            if (_siteMaterial != null) Destroy(_siteMaterial);
            if (_surface != null && _surface.navMeshData != null && _surface.navMeshData != _authoredNavigation)
                Destroy(_surface.navMeshData);
        }
    }
}
