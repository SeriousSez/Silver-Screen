using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Recruitment
{
 public class CandidateWaitingAreaView:MonoBehaviour
 {
  [SerializeField] private RecruitmentDestination _destination;[SerializeField] private Transform _entrance;[SerializeField] private Transform _arrivalPoint;[SerializeField] private Transform _exitPoint;[SerializeField] private List<Transform> _waitingPositions=new List<Transform>();
  private RecruitmentFacility _domainFacility;
  public RecruitmentFacility RecruitmentFacility=>_domainFacility;
  public void BindReservations(RecruitmentFacility facility)=>_domainFacility=facility;
  private readonly Dictionary<string,int> _reservations=new Dictionary<string,int>();
  public string FacilityId {get;private set;}
  public void ConfigureFacility(string id,RecruitmentDestination destination,Transform entrance,Transform arrival,Transform exit,IEnumerable<Transform> waits){FacilityId=id;Configure(destination,entrance,arrival,exit,waits);}
  public RecruitmentDestination Destination=>_destination;public Transform Entrance=>_entrance;public Transform ArrivalPoint=>_arrivalPoint;public Transform ExitPoint=>_exitPoint;public int WaitingPositionCount=>_waitingPositions.Count;
  public void Configure(RecruitmentDestination destination,Transform entrance,Transform arrival,Transform exit,IEnumerable<Transform> waits){_destination=destination;_entrance=entrance;_arrivalPoint=arrival;_exitPoint=exit;_waitingPositions.Clear();if(waits!=null)_waitingPositions.AddRange(waits);}
  public bool TryReserve(string candidateId,out int index,out Vector3 position){if(_domainFacility!=null){bool reserved=_domainFacility.TryReserve(candidateId,out index);position=reserved?_waitingPositions[index].position:transform.position;return reserved;}if(_reservations.TryGetValue(candidateId,out index)){position=_waitingPositions[index].position;return true;}for(int i=0;i<_waitingPositions.Count;i++){if(_reservations.ContainsValue(i))continue;_reservations[candidateId]=i;index=i;position=_waitingPositions[i].position;return true;}index=-1;position=transform.position;return false;}
  public void Release(string candidateId){_domainFacility?.Release(candidateId);_reservations.Remove(candidateId);}
  public Transform WaitingPosition(int index)=>index>=0&&index<_waitingPositions.Count?_waitingPositions[index]:null;
 }
 [SelectionBase] public sealed class StageSchoolView:CandidateWaitingAreaView
 {
  public void Configure(Transform entrance,Transform arrival,Transform exit,IEnumerable<Transform> waits)=>Configure(RecruitmentDestination.StageSchool,entrance,arrival,exit,waits);
 }

 [RequireComponent(typeof(NavMeshAgent)),SelectionBase] public sealed class CandidateAgent:MonoBehaviour
 {
  private NavMeshAgent _nav;private ISimulationTimeService _time;private Action<bool> _arrival;private Vector3 _destination;private float _baseSpeed=3.5f;private GameObject _selection;
  public Candidate Candidate{get;private set;}public bool IsSelected{get;private set;}
  public bool IsHeld { get; private set; }
  private bool _resumeRoute;
  private Transform _waitingVisual;private Renderer _bodyRenderer;private float _idlePhase;
  private void UpdateWaitingIdle()
  {
   if(_waitingVisual==null&&!IsHeld&&Candidate?.IsTalentApplicant==true&&Candidate.Status==CandidateStatus.WaitingForRecruitment){
    var held=GetComponent<SilverScreen.Presentation.Interaction.HeldPersonPresentation>();
    if(held!=null&&held.State!=SilverScreen.Presentation.Interaction.HeldPersonPresentationState.Normal)return;
    if(TryGetComponent<MeshFilter>(out var filter)&&TryGetComponent<Renderer>(out _bodyRenderer)){
     var visual=new GameObject("WaitingBody");_waitingVisual=visual.transform;_waitingVisual.SetParent(transform,false);
     visual.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;visual.AddComponent<MeshRenderer>().sharedMaterials=_bodyRenderer.sharedMaterials;_bodyRenderer.enabled=false;
    }
   }
   if(_waitingVisual==null)return;
   bool waiting=!IsHeld&&Candidate?.Status==CandidateStatus.WaitingForRecruitment&&_arrival==null;
   if(waiting){_idlePhase+=LocalPresentationTime.Delta(UnityEngine.Time.unscaledDeltaTime,_time);_waitingVisual.localRotation=Quaternion.Euler(0,Mathf.Sin(_idlePhase*.31f)*8,Mathf.Sin(_idlePhase*.7f)*1.2f);}
   else _waitingVisual.localRotation=Quaternion.identity;
  }
  private void RestoreBody()
  {
   if(_bodyRenderer!=null)_bodyRenderer.enabled=true;
   if(_waitingVisual!=null){Destroy(_waitingVisual.gameObject);_waitingVisual=null;}
  }
  public void SetHeld(bool held)
  {
   if(held)RestoreBody();IsHeld=held;Candidate?.SetCarried(held);
   if(!held)
   {
    // A waiting applicant retains their reserved queue destination even after
    // the original arrival callback has completed. Resume that route after drop.
    if(_arrival==null && Candidate?.Status==CandidateStatus.WaitingForRecruitment)
     _arrival=_=>{};
    _resumeRoute=_arrival!=null;
    ApplySpeed();
   }
  }
  public void PrepareConversion() { RestoreBody();Candidate?.SetCarried(false); _arrival=null; _resumeRoute=false; IsHeld=false; enabled=false; }

  private void Awake(){_nav=GetComponent<NavMeshAgent>();_baseSpeed=_nav.speed;}
  private void Start(){if(!_nav.isOnNavMesh&&NavMesh.SamplePosition(transform.position,out var hit,8f,NavMesh.AllAreas))_nav.Warp(hit.position);ApplySpeed();}
  private void Update(){UpdateWaitingIdle();if(IsHeld)return;if(_resumeRoute && _nav.isOnNavMesh){_resumeRoute=false;_nav.SetDestination(_destination);}if(_arrival==null||_time!=null&&_time.IsPaused||_nav.pathPending)return;if(!_nav.hasPath||_nav.remainingDistance<=_nav.stoppingDistance+.2f){var callback=_arrival;_arrival=null;callback(_nav.isOnNavMesh && _nav.pathStatus == NavMeshPathStatus.PathComplete && Vector3.Distance(new Vector3(transform.position.x,0,transform.position.z),new Vector3(_destination.x,0,_destination.z)) <= _nav.stoppingDistance+.3f);}}
  private void OnDestroy(){if(_time!=null)_time.OnSpeedChanged-=HandleSpeed;}
  public void Bind(Candidate candidate,ISimulationTimeService time){Candidate=candidate;
   if(_time!=null)_time.OnSpeedChanged-=HandleSpeed;_time=time;if(_time!=null)_time.OnSpeedChanged+=HandleSpeed;ApplySpeed();}
  public bool TryMove(Vector3 destination,Action<bool> arrival){if(IsHeld){_destination=destination;_arrival=arrival;return true;}if(_nav==null||!_nav.isActiveAndEnabled||!_nav.isOnNavMesh||!_nav.SetDestination(destination))return false;_destination=destination;_arrival=arrival;if(_time!=null&&_time.IsPaused)_nav.isStopped=true;return true;}
  public void SetSelectionIndicator(GameObject indicator){_selection=indicator;_selection.SetActive(IsSelected);}public void SetSelected(bool value){IsSelected=value;if(_selection!=null)_selection.SetActive(value);}
  private void HandleSpeed(SimulationSpeed unused)=>ApplySpeed();private void ApplySpeed(){if(IsHeld||_nav==null||!_nav.isOnNavMesh)return;bool paused=_time!=null&&_time.IsPaused;_nav.isStopped=paused;if(!paused)_nav.speed=_baseSpeed;}
 }

 public sealed class CandidateWorldRouter:MonoBehaviour,ICandidateWorldRouter
 {
  private readonly Dictionary<string,CandidateAgent> _agents=new Dictionary<string,CandidateAgent>();private readonly Dictionary<RecruitmentDestination,CandidateWaitingAreaView> _areas=new Dictionary<RecruitmentDestination,CandidateWaitingAreaView>();private readonly Dictionary<string,CandidateWaitingAreaView> _candidateAreas=new Dictionary<string,CandidateWaitingAreaView>();private ISimulationTimeService _time;
  private SilverScreen.Presentation.Buildings.StudioBuildLot _lot;
  private readonly Dictionary<string,CandidateWaitingAreaView> _facilityAreas=new Dictionary<string,CandidateWaitingAreaView>();
  public void RegisterArea(CandidateWaitingAreaView area){if(area==null)return;if(area.RecruitmentFacility!=null)area.RecruitmentFacility.IsReachable=()=>CanReceive(area);if(area.FacilityId!=null)_facilityAreas[area.FacilityId]=area;else _areas[area.Destination]=area;}
  public void Initialize(IEnumerable<CandidateWaitingAreaView> areas,ISimulationTimeService time){_lot=FindAnyObjectByType<SilverScreen.Presentation.Buildings.StudioBuildLot>();_areas.Clear();_facilityAreas.Clear();if(areas!=null)foreach(var area in areas)RegisterArea(area);_time=time;}
  public CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate,Action<CandidateRouteResult,int> complete){if(candidate==null)return CandidateRouteResult.CandidateMissing;CandidateWaitingAreaView area;bool found=candidate.FacilityId!=null?_facilityAreas.TryGetValue(candidate.FacilityId,out area):_areas.TryGetValue(candidate.Destination,out area);if(!found||area==null||!area.gameObject.activeInHierarchy)return CandidateRouteResult.RecruitmentLocationMissing;if(!area.TryReserve(candidate.Person.Id,out int slot,out var waiting))return CandidateRouteResult.CapacityFull;if(_candidateAreas.TryGetValue(candidate.Person.Id,out var previous)&&previous!=area&&previous!=null)previous.Release(candidate.Person.Id);_candidateAreas[candidate.Person.Id]=area;var agent=GetAgent(candidate)??Spawn(candidate,area);if(agent==null){if(area!=null)area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);return CandidateRouteResult.AgentMissing;}if(!agent.TryMove(area.Entrance.position,reached=>{if(!reached){complete(CandidateRouteResult.NavigationRejected,slot);return;}if(!agent.TryMove(waiting,ok=>complete(ok?CandidateRouteResult.Started:CandidateRouteResult.NavigationRejected,slot)))complete(CandidateRouteResult.NavigationRejected,slot);})){if(area!=null)area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);return CandidateRouteResult.NavigationRejected;}return CandidateRouteResult.Started;}
  public CandidateRouteResult RouteToExit(Candidate candidate,Action<CandidateRouteResult,int> complete){if(candidate==null)return CandidateRouteResult.CandidateMissing;if(!_candidateAreas.TryGetValue(candidate.Person.Id,out var area)||area==null)return CandidateRouteResult.RecruitmentLocationMissing;if(!_agents.TryGetValue(candidate.Person.Id,out var agent))return CandidateRouteResult.AgentMissing;area.Release(candidate.Person.Id);if(!agent.TryMove(_lot!=null?_lot.ApplicantArrival:area.ExitPoint.position,ok=>complete(ok?CandidateRouteResult.Started:CandidateRouteResult.NavigationRejected,-1)))return CandidateRouteResult.NavigationRejected;return CandidateRouteResult.Started;}
  public void ReleaseCandidate(Candidate candidate){if(candidate==null)return;if(_candidateAreas.TryGetValue(candidate.Person.Id,out var area)){if(area!=null)area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);}if(_agents.TryGetValue(candidate.Person.Id,out var agent)){_agents.Remove(candidate.Person.Id);if(agent!=null)Destroy(agent.gameObject);}}
  public CandidateAgent GetAgent(Candidate candidate)=>candidate!=null&&_agents.TryGetValue(candidate.Person.Id,out var agent)?agent:null;
  public EmployeeAgent ConvertToEmployee(Candidate candidate,Employee employee){if(!_agents.TryGetValue(candidate.Person.Id,out var candidateAgent))return null;var go=candidateAgent.gameObject;_agents.Remove(candidate.Person.Id);if(_candidateAreas.TryGetValue(candidate.Person.Id,out var area)){if(area!=null)area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);}candidateAgent.PrepareConversion();Destroy(candidateAgent);var employeeAgent=go.AddComponent<EmployeeAgent>();employeeAgent.BindDomain(employee);employeeAgent.BindTimeService(_time);var ring=go.transform.Find("SelectionRing")?.gameObject;if(ring!=null)employeeAgent.SetSelectionIndicator(ring);return employeeAgent;}
  public void UnregisterArea(string id)=>_facilityAreas.Remove(id);
  public bool CanReceive(CandidateWaitingAreaView area)
  {
   if(area==null||!area.isActiveAndEnabled||area.Entrance==null)return false;
   var source=_lot!=null?_lot.ApplicantArrival:area.ArrivalPoint.position;
   if(!TryArrivalPosition(source,area.Entrance.position,_lot,out var origin,false))return false;
   var path=new NavMeshPath();
   for(int i=0;i<area.WaitingPositionCount;i++){
    var p=area.WaitingPosition(i);if(p==null||!NavMesh.SamplePosition(p.position,out var hit,.25f,NavMesh.AllAreas)||
     !NavMesh.CalculatePath(area.Entrance.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
   }
   return true;
  }
  private int _arrivalSequence;
  private bool TryArrivalPosition(Vector3 origin,Vector3 entrance,SilverScreen.Presentation.Buildings.StudioBuildLot lot,out Vector3 position,bool advance=true)
  {
   // Rotate between separated street-side lanes rather than emitting everyone
   // from one point. Keep clearance from people and scenery before creating a body.
   var filter=new NavMeshQueryFilter {agentTypeID=0,areaMask=NavMesh.AllAreas};
   var path=new NavMeshPath();
   for(int attempt=0;attempt<15;attempt++)
   {
    int slot=(_arrivalSequence*7+attempt)%15;
    var requested=origin+new Vector3((slot/5)*1.6f,0,((slot%5)-2)*1.6f);
    if(!NavMesh.SamplePosition(requested,out var hit,.4f,filter))continue;
    var point=hit.position;
    if(lot!=null)
    {
     var bounds=lot.Buildable;
     if(Mathf.Abs(point.x-bounds.X)<bounds.Width*.5f+2f && Mathf.Abs(point.z-bounds.Z)<bounds.Depth*.5f+2f)continue;
    }
    const float clearance=.65f;
    if(Physics.CheckCapsule(point+Vector3.up*(clearance+.08f),point+Vector3.up*(EmployeeNavigationProfile.Height-clearance),clearance,~0,QueryTriggerInteraction.Ignore))continue;
    if(!NavMesh.CalculatePath(point,entrance,filter,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
    position=point;if(advance)_arrivalSequence++;return true;
   }
   position=origin;return false;
  }
  private CandidateAgent Spawn(Candidate candidate,CandidateWaitingAreaView area){var lot=_lot;var spawnPoint=lot!=null?lot.ApplicantArrival:area.ArrivalPoint.position;if(!TryArrivalPosition(spawnPoint,area.Entrance.position,lot,out var spawnPosition))return null;var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="Candidate_"+candidate.Person.Name;go.transform.position=spawnPosition;go.GetComponent<MeshFilter>().sharedMesh=SilverScreen.Presentation.Employees.EmployeeNavigationProfile.BodyMesh;go.GetComponent<CapsuleCollider>().radius=SilverScreen.Presentation.Employees.EmployeeNavigationProfile.Radius;var renderer=go.GetComponent<Renderer>();renderer.material.color=candidate.Destination==RecruitmentDestination.ScriptOffice?new Color(.48f,.62f,.82f):new Color(.82f,.55f,.25f);var nav=go.AddComponent<NavMeshAgent>();SilverScreen.Presentation.Employees.EmployeeNavigationProfile.Configure(nav,2f,1f);nav.stoppingDistance=SilverScreen.Presentation.Employees.EmployeeNavigationProfile.Radius;nav.speed=1.8f;nav.acceleration=8f;var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="SelectionRing";ring.transform.SetParent(go.transform,false);ring.transform.localPosition=new Vector3(0,-1f,0);ring.transform.localScale=new Vector3(1.1f,.03f,1.1f);Destroy(ring.GetComponent<Collider>());ring.GetComponent<Renderer>().material.color=new Color(1f,.75f,.1f);if(NavMesh.SamplePosition(go.transform.position,out var hit,8f,NavMesh.AllAreas))nav.Warp(hit.position);var agent=go.AddComponent<CandidateAgent>();agent.SetSelectionIndicator(ring);agent.Bind(candidate,_time);_agents[candidate.Person.Id]=agent;return agent;}
 }

 public sealed class RecruitmentDriver:MonoBehaviour
 {
  [SerializeField] private StageSchoolView _stageSchool;[SerializeField] private SimulationTimeDriver _timeDriver;[SerializeField] private StudioEmployeeManager _employeeManager;[SerializeField] private int _seed=1930;[SerializeField] private int _capacity=5;[SerializeField] private int _arrivalCadenceMinutes=360;[SerializeField] private int _maximumWaitingMinutes=2880;
  [SerializeField] private TalentApplicantSettings _talentApplicants=new TalentApplicantSettings();
  private CandidateWorldRouter _world;private OpeningRecruitmentWave _openingWave;public RecruitmentCoordinator Coordinator{get;private set;}public StageSchoolView StageSchool=>_stageSchool;public CandidateWorldRouter WorldRouter=>_world;public SimulationDateTime CurrentTime=>_timeDriver!=null?_timeDriver.TimeService.CurrentTime:new SimulationDateTime(1930,1,1,0,0);
  public void ConfigureGenerationSeed(int seed){if(Coordinator!=null)throw new InvalidOperationException("Configure candidate generation before RecruitmentDriver starts.");_seed=seed;}
  public void ConfigureOpeningWave(OpeningRecruitmentWave wave){if(Coordinator!=null)throw new InvalidOperationException("Configure the opening wave before RecruitmentDriver starts.");_openingWave=wave??throw new ArgumentNullException(nameof(wave));}
  private void Start()
  {
   if(_timeDriver==null)_timeDriver=FindAnyObjectByType<SimulationTimeDriver>();
   if(_employeeManager==null)_employeeManager=FindAnyObjectByType<StudioEmployeeManager>();
   var construction=GetComponent<SilverScreen.Presentation.Buildings.StudioConstructionDriver>();
   _world=GetComponent<CandidateWorldRouter>()??gameObject.AddComponent<CandidateWorldRouter>();
   if(construction?.Service!=null)
   {
    _world.Initialize(construction.ApplicantAreas,_timeDriver.TimeService);
    Coordinator=new RecruitmentCoordinator(_timeDriver.TimeService,new CandidateGenerator(new SeededRandomSource(_seed)),_world,
     new RecruitmentConfiguration(Math.Max(_capacity,_openingWave?.Roles.Count??0),_arrivalCadenceMinutes,_maximumWaitingMinutes));
    Coordinator.Facilities=construction.ApplicantFacilities;
   }
   else
   {
    if(_stageSchool==null)_stageSchool=FindAnyObjectByType<StageSchoolView>();
    if(_stageSchool==null)_stageSchool=CreatePrototype();
    _world.Initialize(FindObjectsByType<CandidateWaitingAreaView>(FindObjectsInactive.Exclude),_timeDriver.TimeService);
    Coordinator=new RecruitmentCoordinator(_timeDriver.TimeService,new CandidateGenerator(new SeededRandomSource(_seed)),_world,
     new RecruitmentConfiguration(_capacity,_arrivalCadenceMinutes,_maximumWaitingMinutes,ProfessionalRole.Writer));
   }
   Coordinator.Population=_employeeManager.Workforce.Population;
   Coordinator.ConfigureTalentApplicants(_talentApplicants);
   if(_openingWave!=null)Coordinator.ConfigureOpeningWave(_openingWave);
   Coordinator.OnCandidateAdded+=HandleCandidateAdded;
   Coordinator.OnCandidateRemoved+=HandleCandidateRemoved;
   Coordinator.OnCandidateHired+=HandleHired;
   Coordinator.OnRoutingFailed+=HandleRoutingFailed;
   gameObject.AddComponent<SilverScreen.Presentation.UI.RecruitmentPanelUI>().Initialize(this);
  }
  private void OnDestroy(){if(Coordinator!=null){Coordinator.OnCandidateAdded-=HandleCandidateAdded;Coordinator.OnCandidateRemoved-=HandleCandidateRemoved;Coordinator.OnCandidateHired-=HandleHired;Coordinator.OnRoutingFailed-=HandleRoutingFailed;Coordinator.Dispose();}}
  private void HandleCandidateAdded(Candidate candidate)=>_timeDriver.Wellbeing.Register(candidate.Person);
  private void HandleCandidateRemoved(Candidate candidate){if(candidate.Status!=CandidateStatus.Hired)_timeDriver.Wellbeing.Unregister(candidate.Person);}
  private void HandleRoutingFailed(Candidate candidate,CandidateRouteResult result)=>Debug.LogWarning($"Recruitment route failed for {candidate.Person.Id}: {result}.",this);
  private void HandleHired(Candidate candidate,Employee employee){var agent=_world.ConvertToEmployee(candidate,employee);if(agent!=null)_employeeManager.RegisterEmployee(employee,agent);}
  private static StageSchoolView CreatePrototype(){var root=GameObject.CreatePrimitive(PrimitiveType.Cube);root.name="StageSchool";root.transform.position=new Vector3(-15f,2f,9f);root.transform.localScale=new Vector3(10f,4f,7f);root.GetComponent<Renderer>().material.color=new Color(.78f,.66f,.48f);var view=root.AddComponent<StageSchoolView>();Transform Marker(string name,Vector3 local){var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=local;return go.transform;}var entrance=Marker("Entrance",new Vector3(0,-.5f,-.58f));var arrival=Marker("ArrivalPoint",new Vector3(-.8f,-.5f,-1.3f));var exit=Marker("ExitPoint",new Vector3(.8f,-.5f,-1.3f));var waits=new List<Transform>();for(int i=0;i<5;i++)waits.Add(Marker("WaitingPosition_"+(i+1),new Vector3(-.4f+i*.2f,-.5f,-.85f)));view.Configure(entrance,arrival,exit,waits);return view;}
 }
}
