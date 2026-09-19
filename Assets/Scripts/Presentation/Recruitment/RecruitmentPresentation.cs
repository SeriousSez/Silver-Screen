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
  private readonly Dictionary<string,int> _reservations=new Dictionary<string,int>();
  public RecruitmentDestination Destination=>_destination;public Transform Entrance=>_entrance;public Transform ArrivalPoint=>_arrivalPoint;public Transform ExitPoint=>_exitPoint;public int WaitingPositionCount=>_waitingPositions.Count;
  public void Configure(RecruitmentDestination destination,Transform entrance,Transform arrival,Transform exit,IEnumerable<Transform> waits){_destination=destination;_entrance=entrance;_arrivalPoint=arrival;_exitPoint=exit;_waitingPositions.Clear();if(waits!=null)_waitingPositions.AddRange(waits);}
  public bool TryReserve(string candidateId,out int index,out Vector3 position){if(_reservations.TryGetValue(candidateId,out index)){position=_waitingPositions[index].position;return true;}for(int i=0;i<_waitingPositions.Count;i++){if(_reservations.ContainsValue(i))continue;_reservations[candidateId]=i;index=i;position=_waitingPositions[i].position;return true;}index=-1;position=transform.position;return false;}
  public void Release(string candidateId)=>_reservations.Remove(candidateId);
 }
 [SelectionBase] public sealed class StageSchoolView:CandidateWaitingAreaView
 {
  public void Configure(Transform entrance,Transform arrival,Transform exit,IEnumerable<Transform> waits)=>Configure(RecruitmentDestination.StageSchool,entrance,arrival,exit,waits);
 }

 [RequireComponent(typeof(NavMeshAgent)),SelectionBase] public sealed class CandidateAgent:MonoBehaviour
 {
  private NavMeshAgent _nav;private ISimulationTimeService _time;private Action<bool> _arrival;private float _baseSpeed=3.5f;private GameObject _selection;
  public Candidate Candidate{get;private set;}public bool IsSelected{get;private set;}
  private void Awake(){_nav=GetComponent<NavMeshAgent>();_baseSpeed=_nav.speed;}
  private void Start(){if(!_nav.isOnNavMesh&&NavMesh.SamplePosition(transform.position,out var hit,8f,NavMesh.AllAreas))_nav.Warp(hit.position);ApplySpeed();}
  private void Update(){if(_arrival==null||_time!=null&&_time.IsPaused||_nav.pathPending)return;if(!_nav.hasPath||_nav.remainingDistance<=_nav.stoppingDistance+.2f){var callback=_arrival;_arrival=null;callback(true);}}
  private void OnDestroy(){if(_time!=null)_time.OnSpeedChanged-=HandleSpeed;}
  public void Bind(Candidate candidate,ISimulationTimeService time){Candidate=candidate;if(_time!=null)_time.OnSpeedChanged-=HandleSpeed;_time=time;if(_time!=null)_time.OnSpeedChanged+=HandleSpeed;ApplySpeed();}
  public bool TryMove(Vector3 destination,Action<bool> arrival){if(_nav==null||!_nav.isActiveAndEnabled||!_nav.isOnNavMesh||!_nav.SetDestination(destination))return false;_arrival=arrival;if(_time!=null&&_time.IsPaused)_nav.isStopped=true;return true;}
  public void SetSelectionIndicator(GameObject indicator){_selection=indicator;_selection.SetActive(IsSelected);}public void SetSelected(bool value){IsSelected=value;if(_selection!=null)_selection.SetActive(value);}
  private void HandleSpeed(SimulationSpeed unused)=>ApplySpeed();private void ApplySpeed(){if(_nav==null||!_nav.isOnNavMesh)return;bool paused=_time!=null&&_time.IsPaused;_nav.isStopped=paused;if(!paused)_nav.speed=_baseSpeed*(_time?.TimeScaleMultiplier??1f);}
 }

 public sealed class CandidateWorldRouter:MonoBehaviour,ICandidateWorldRouter
 {
  private readonly Dictionary<string,CandidateAgent> _agents=new Dictionary<string,CandidateAgent>();private readonly Dictionary<RecruitmentDestination,CandidateWaitingAreaView> _areas=new Dictionary<RecruitmentDestination,CandidateWaitingAreaView>();private readonly Dictionary<string,CandidateWaitingAreaView> _candidateAreas=new Dictionary<string,CandidateWaitingAreaView>();private ISimulationTimeService _time;
  public void Initialize(IEnumerable<CandidateWaitingAreaView> areas,ISimulationTimeService time){_areas.Clear();if(areas!=null)foreach(var area in areas)if(area!=null)_areas[area.Destination]=area;_time=time;}
  public CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate,Action<CandidateRouteResult,int> complete){if(candidate==null)return CandidateRouteResult.CandidateMissing;if(!_areas.TryGetValue(candidate.Destination,out var area)||area==null)return CandidateRouteResult.RecruitmentLocationMissing;if(!area.TryReserve(candidate.Person.Id,out int slot,out var waiting))return CandidateRouteResult.CapacityFull;_candidateAreas[candidate.Person.Id]=area;var agent=Spawn(candidate,area);if(agent==null){area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);return CandidateRouteResult.AgentMissing;}if(!agent.TryMove(area.Entrance.position,_=>{if(!agent.TryMove(waiting,ok=>complete(ok?CandidateRouteResult.Started:CandidateRouteResult.NavigationRejected,slot)))complete(CandidateRouteResult.NavigationRejected,slot);})){area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);return CandidateRouteResult.NavigationRejected;}return CandidateRouteResult.Started;}
  public CandidateRouteResult RouteToExit(Candidate candidate,Action<CandidateRouteResult,int> complete){if(candidate==null)return CandidateRouteResult.CandidateMissing;if(!_candidateAreas.TryGetValue(candidate.Person.Id,out var area)||area==null)return CandidateRouteResult.RecruitmentLocationMissing;if(!_agents.TryGetValue(candidate.Person.Id,out var agent))return CandidateRouteResult.AgentMissing;area.Release(candidate.Person.Id);if(!agent.TryMove(area.ExitPoint.position,ok=>complete(ok?CandidateRouteResult.Started:CandidateRouteResult.NavigationRejected,-1)))return CandidateRouteResult.NavigationRejected;return CandidateRouteResult.Started;}
  public void ReleaseCandidate(Candidate candidate){if(candidate==null)return;if(_candidateAreas.TryGetValue(candidate.Person.Id,out var area)){area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);}if(_agents.TryGetValue(candidate.Person.Id,out var agent)){_agents.Remove(candidate.Person.Id);Destroy(agent.gameObject);}}
  public CandidateAgent GetAgent(Candidate candidate)=>candidate!=null&&_agents.TryGetValue(candidate.Person.Id,out var agent)?agent:null;
  public EmployeeAgent ConvertToEmployee(Candidate candidate,Employee employee){if(!_agents.TryGetValue(candidate.Person.Id,out var candidateAgent))return null;var go=candidateAgent.gameObject;_agents.Remove(candidate.Person.Id);if(_candidateAreas.TryGetValue(candidate.Person.Id,out var area)){area.Release(candidate.Person.Id);_candidateAreas.Remove(candidate.Person.Id);}Destroy(candidateAgent);var employeeAgent=go.AddComponent<EmployeeAgent>();employeeAgent.BindDomain(employee);employeeAgent.BindTimeService(_time);var ring=go.transform.Find("SelectionRing")?.gameObject;if(ring!=null)employeeAgent.SetSelectionIndicator(ring);return employeeAgent;}
  private CandidateAgent Spawn(Candidate candidate,CandidateWaitingAreaView area){var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="Candidate_"+candidate.Person.Name;go.transform.position=area.ArrivalPoint.position;go.transform.localScale=new Vector3(.8f,1f,.8f);var renderer=go.GetComponent<Renderer>();renderer.material.color=candidate.Destination==RecruitmentDestination.ScriptOffice?new Color(.48f,.62f,.82f):new Color(.82f,.55f,.25f);var nav=go.AddComponent<NavMeshAgent>();nav.radius=.42f;nav.height=2f;nav.speed=3.5f;nav.acceleration=14f;var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="SelectionRing";ring.transform.SetParent(go.transform,false);ring.transform.localPosition=new Vector3(0,-1f,0);ring.transform.localScale=new Vector3(1.1f,.03f,1.1f);Destroy(ring.GetComponent<Collider>());ring.GetComponent<Renderer>().material.color=new Color(1f,.75f,.1f);if(NavMesh.SamplePosition(go.transform.position,out var hit,8f,NavMesh.AllAreas))nav.Warp(hit.position);var agent=go.AddComponent<CandidateAgent>();agent.SetSelectionIndicator(ring);agent.Bind(candidate,_time);_agents[candidate.Person.Id]=agent;return agent;}
 }

 public sealed class RecruitmentDriver:MonoBehaviour
 {
  [SerializeField] private StageSchoolView _stageSchool;[SerializeField] private SimulationTimeDriver _timeDriver;[SerializeField] private StudioEmployeeManager _employeeManager;[SerializeField] private int _seed=1930;[SerializeField] private int _capacity=5;[SerializeField] private int _arrivalCadenceMinutes=360;[SerializeField] private int _maximumWaitingMinutes=2880;
  private CandidateWorldRouter _world;public RecruitmentCoordinator Coordinator{get;private set;}public StageSchoolView StageSchool=>_stageSchool;public CandidateWorldRouter WorldRouter=>_world;public SimulationDateTime CurrentTime=>_timeDriver!=null?_timeDriver.TimeService.CurrentTime:new SimulationDateTime(1930,1,1,0,0);
  private void Start(){if(_timeDriver==null)_timeDriver=FindAnyObjectByType<SimulationTimeDriver>();if(_employeeManager==null)_employeeManager=FindAnyObjectByType<StudioEmployeeManager>();if(_stageSchool==null)_stageSchool=FindAnyObjectByType<StageSchoolView>();if(_stageSchool==null)_stageSchool=CreatePrototype();var areas=FindObjectsByType<CandidateWaitingAreaView>(FindObjectsInactive.Exclude);_world=GetComponent<CandidateWorldRouter>()??gameObject.AddComponent<CandidateWorldRouter>();_world.Initialize(areas,_timeDriver.TimeService);Coordinator=new RecruitmentCoordinator(_timeDriver.TimeService,new CandidateGenerator(new SeededRandomSource(_seed)),_world,new RecruitmentConfiguration(_capacity,_arrivalCadenceMinutes,_maximumWaitingMinutes,ProfessionalRole.Writer));Coordinator.OnCandidateHired+=HandleHired;gameObject.AddComponent<SilverScreen.Presentation.UI.RecruitmentPanelUI>().Initialize(this);}
  private void OnDestroy(){if(Coordinator!=null){Coordinator.OnCandidateHired-=HandleHired;Coordinator.Dispose();}}
  private void HandleHired(Candidate candidate,Employee employee){var agent=_world.ConvertToEmployee(candidate,employee);if(agent!=null)_employeeManager.RegisterEmployee(employee,agent);}
  private static StageSchoolView CreatePrototype(){var root=GameObject.CreatePrimitive(PrimitiveType.Cube);root.name="StageSchool";root.transform.position=new Vector3(-15f,2f,9f);root.transform.localScale=new Vector3(10f,4f,7f);root.GetComponent<Renderer>().material.color=new Color(.78f,.66f,.48f);var view=root.AddComponent<StageSchoolView>();Transform Marker(string name,Vector3 local){var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=local;return go.transform;}var entrance=Marker("Entrance",new Vector3(0,-.5f,-.58f));var arrival=Marker("ArrivalPoint",new Vector3(-.8f,-.5f,-1.3f));var exit=Marker("ExitPoint",new Vector3(.8f,-.5f,-1.3f));var waits=new List<Transform>();for(int i=0;i<5;i++)waits.Add(Marker("WaitingPosition_"+(i+1),new Vector3(-.4f+i*.2f,-.5f,-.85f)));view.Configure(entrance,arrival,exit,waits);return view;}
 }
}
