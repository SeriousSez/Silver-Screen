using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Bootstrap;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SilverScreen.Tests.EditMode
{
 public sealed class StageSchoolC1Validation
 {
  const string Out="ArtReview/StageSchoolC1";
  public static void RunDomain()=>Run(new[]{"SilverScreen.Tests.EditMode.StageSchoolApplicantTests","SilverScreen.Tests.RecruitmentTests","SilverScreen.Tests.EditMode.PersonAutonomySimulationTests","SilverScreen.Tests.EditMode.ContextualCarryTargetTests","SilverScreen.Tests.EditMode.PersonInteractionTests","SilverScreen.Tests.EditMode.SimulationSpeedTests"},"edit-results.xml");
  public static void RunPlay()=>Run(new[]{"SilverScreen.Tests.EditMode.StageSchoolC1Validation.RealApplicantCarryHiring","SilverScreen.Tests.EditMode.StageSchoolC1Validation.ConstructionDispatchLifecycle","SilverScreen.Tests.EditMode.PersonManipulationRuntimeTests.ConstructionRouteRecoveryKeepsSemanticAssignmentAfterPathLoss"},"play-results.xml");
  static void Run(string[] tests,string file){var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(api,file));api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=tests}));}
  sealed class Results:ICallbacks
  {
   TestRunnerApi api;string file;public Results(TestRunnerApi a,string f){api=a;file=f;}public void RunStarted(ITestAdaptor a){}public void TestStarted(ITestAdaptor a){}public void TestFinished(ITestResultAdaptor a){}
   public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,Out+"/"+file);Object.DestroyImmediate(api);}
  }
  static void Note(string text)=>File.AppendAllText(Out+"/play.txt",text+"\n");
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(Application.isPlaying)yield return new ExitPlayMode();
   string previous=SessionState.GetString("C1.previous","");SessionState.EraseString("C1.previous");if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
  }
  [UnityTest]public IEnumerator RealApplicantCarryHiring()
  {
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False,"Preserve unsaved scene work");
   SessionState.SetString("C1.previous",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
   File.WriteAllText(Out+"/play.txt","C1 focused real applicant and input proof\n");
   EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
   var bootstrap=new SerializedObject(Object.FindAnyObjectByType<StudioBootstrap>());bootstrap.FindProperty("_startNewStudio").boolValue=true;bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;bootstrap.ApplyModifiedPropertiesWithoutUndo();
   yield return new EnterPlayMode();yield return null;yield return null;
   bool run=Application.runInBackground;Application.runInBackground=true;
   try{yield return Proof();}finally{Application.runInBackground=run;}
  }
  static IEnumerator Proof()
  {
   var time=Object.FindAnyObjectByType<SimulationTimeDriver>();time.Clock.SetSpeed(SimulationSpeed.VeryFast);
   var construction=Object.FindAnyObjectByType<StudioConstructionDriver>();var recruitment=Object.FindAnyObjectByType<RecruitmentDriver>();var employees=Object.FindAnyObjectByType<StudioEmployeeManager>();
   Assert.That(employees.AllEmployees,Is.Empty);
   PlacedBuilding b=null;var lot=construction.Lot.Buildable;
   for(float x=lot.X-lot.Width/2+22;x<lot.X+lot.Width/2-22&&b==null;x+=7)
    for(float z=lot.Z-lot.Depth/2+22;z<lot.Z+lot.Depth/2-22&&b==null;z+=7){var pose=new BuildingPose(x,z,0);if(construction.Service.Validate(StudioBuildingDefinitions.StageSchool,pose)==null)b=construction.Service.Place(StudioBuildingDefinitions.StageSchool,pose,out _);}
   Assert.That(b,Is.Not.Null);var root=construction.Sites[b.Id].FinishedContent;
   Assert.That(root.GetComponent<StageSchoolApplicantFacility>(),Is.Null,"Under construction is not registered for applicants");
   // Fixture setup only: complete the existing construction service using temporary domain labor,
   // without adding employees to the studio. Arrival and hiring below use the real systems.
   var crew=Enumerable.Range(0,3).Select(i=>new Employee("c1-setup-"+i,"Setup labor",EmployeeRole.ConstructionWorker,65,100)).ToArray();
   for(int tick=0;tick<250&&!b.IsOperational;tick++){
    foreach(var worker in crew){if(!construction.Service.HasAssignment(worker.Id))construction.Service.AssignWorker(b,worker);construction.Service.WorkerArrived(worker.Id);}
    time.Clock.Advance(60);while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}yield return null;
   }
   Assert.That(b.IsOperational,Is.True);float deadline=Time.realtimeSinceStartup+45;while(construction.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That(construction.NavigationUpdatePending,Is.False);Assert.That(root.GetComponent<StageSchoolApplicantFacility>(),Is.Not.Null);Assert.That(employees.AllEmployees,Is.Empty);Note("Operational production A4/B2 school; zero studio employees.");
   var camera=UnityEngine.Camera.main;foreach(var driver in Object.FindObjectsByType<SilverScreen.Presentation.Camera.StudioCameraController>(FindObjectsSortMode.None))driver.enabled=false;
   camera.transform.position=root.transform.TransformPoint(new Vector3(0,23,-16));camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,0,-5)));camera.orthographic=true;camera.orthographicSize=14;
   var selection=Object.FindAnyObjectByType<StudioSelectionController>();var controller=selection.PersonInteraction;
   foreach(var role in new[]{ProfessionalRole.Actor,ProfessionalRole.Director}){
    time.Clock.AdvanceTo(time.Clock.Now+new SimulationDuration((recruitment.Coordinator.TalentMinutesUntilArrival+1)*60));
    while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}
    var candidate=recruitment.Coordinator.Candidates.FirstOrDefault(c=>c.IsTalentApplicant);Assert.That(candidate,Is.Not.Null,"Real simulation-clock arrival");
    var agent=recruitment.WorldRouter.GetAgent(candidate);Assert.That(agent,Is.Not.Null);var originalObject=agent.gameObject;var person=candidate.Person;var talent=person.Talent;
    bool wasArriving=candidate.Status==CandidateStatus.Arriving;var source=agent.transform.position;
    Assert.That(candidate.Status==CandidateStatus.Arriving||candidate.Status==CandidateStatus.WaitingForRecruitment,Is.True);
    if(wasArriving)Note("Spawned "+person.Id+" at world arrival source "+source);
    deadline=Time.realtimeSinceStartup+90;while(candidate.Status==CandidateStatus.Arriving&&Time.realtimeSinceStartup<deadline)yield return null;
    Assert.That(candidate.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment));
    if(wasArriving)Assert.That(Vector3.Distance(source,agent.transform.position),Is.GreaterThan(2));
    Assert.That(candidate.WaitingPositionIndex,Is.GreaterThanOrEqualTo(0));
    Note("Physically reached authored exterior slot "+candidate.WaitingPositionIndex);
    yield return null;
    Capture(camera,"waiting_"+role,root);
    using(var pause=time.Clock.AcquirePause("c1-input","Focused actual carry proof")){
     var update=InputSystem.settings.updateMode;var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
     var muted=InputSystem.devices.Where(d=>d.enabled&&(d is Mouse||d is Keyboard)).ToArray();foreach(var d in muted)InputSystem.DisableDevice(d);
     InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
     var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();var ui=Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);foreach(var e in ui)e.enabled=false;bool enabled=selection.enabled;selection.enabled=false;
     try{
      void Pointer(Vector3 p,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(p),buttons=(ushort)(down?1:0)});InputSystem.Update();controller.HandleInput();}
      Physics.SyncTransforms();var pickup=agent.transform.position;
      Pointer(pickup,false);Pointer(pickup,true);Pointer(pickup,false);Assert.That(selection.SelectedCandidate,Is.SameAs(agent));Assert.That(controller.IsHolding,Is.False,"Quick left click selects without pickup");
      Pointer(pickup,true);yield return new WaitForSecondsRealtime(.24f);Pointer(pickup,true);Assert.That(controller.IsHolding,Is.True,"Hold threshold picks up");
      var target=root.GetComponentsInChildren<ApplicantHiringAction>().Single(a=>a.Profession==role).GetComponent<ContextualDropTarget>();
      Pointer(target.Placement.position,false);Assert.That(controller.IsHolding,Is.True,"Original release retains carry");
      Assert.That(root.GetComponent<BuildingCutawayController>().IsRevealed,Is.True);Assert.That(controller.VisibleTargets.Count(t=>t.Action is ApplicantHiringAction),Is.EqualTo(3));
      var future=root.GetComponentsInChildren<ApplicantHiringAction>().Single(a=>a.FutureOnly).GetComponent<ContextualDropTarget>();
      Assert.That(future.IsPresented,Is.True,"Disabled future affordance is shown but not an eligible drop target");Assert.That(future.Action.CanExecute(new PersonDropContext(candidate,null,recruitment.Coordinator,null)),Is.False);
      root.GetComponent<LODGroup>().ForceLOD(1);Pointer(target.Placement.position,false);Assert.That(target.IsPresented,Is.True);root.GetComponent<LODGroup>().ForceLOD(-1);
      Capture(camera,"carry_"+role,root);
      Pointer(target.Placement.position,true);Assert.That(controller.IsHolding,Is.False,"Separate click physically drops into profession region");
      var employee=employees.GetEmployee(person.Id);Assert.That(employee,Is.Not.Null);Assert.That(employee.Person,Is.SameAs(person));Assert.That(employee.Person.Talent,Is.SameAs(talent));Assert.That(employee.Role.ToString(),Is.EqualTo(role.ToString()));
      Assert.That(employees.GetAgent(person.Id).gameObject,Is.SameAs(originalObject));Assert.That(candidate.Status,Is.EqualTo(CandidateStatus.Hired));Assert.That(candidate.WaitingPositionIndex,Is.EqualTo(-1));Assert.That(recruitment.Coordinator.Candidates.Contains(candidate),Is.False);
      Assert.That(construction.ApplicantFacilities.Facilities.Single(f=>f.Id==b.Id).WaitingReservations.ContainsKey(person.Id),Is.False);Note("Actual carry/drop hired same person and world object as "+role+"; slot released; original skills retained.");
     }finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);foreach(var d in muted)InputSystem.EnableDevice(d);InputSystem.settings.updateMode=update;InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;foreach(var e in ui)if(e!=null)e.enabled=true;selection.enabled=enabled;}
    }
    yield return null;var normal=employees.GetAgent(person.Id);Assert.That(normal.enabled,Is.True);Assert.That(normal.IsHeld,Is.False);Assert.That(normal.GetComponent<CandidateAgent>(),Is.Null);
    var hiringTarget=root.GetComponentsInChildren<ApplicantHiringAction>().Single(a=>a.Profession==role).GetComponent<ContextualDropTarget>();
    var placementArea=RoomBounds(hiringTarget);
    Assert.That(time.Autonomy.ParticipantCount,Is.EqualTo(employees.AllEmployees.Count),"Hired employees are registered for autonomy.");
    Assert.That(time.Autonomy.LastDecision(normal.Employee)?.Activity,Is.EqualTo(PersonAutonomousActivity.IdleWait),
     "The employee has no worthwhile autonomous activity to perform.");
    bool sawPurposefulDeparture=false;deadline=Time.realtimeSinceStartup+20;
    while(placementArea.Contains(normal.transform.position)&&Time.realtimeSinceStartup<deadline)
    {
     sawPurposefulDeparture|=normal.Employee.CurrentIntent.Purpose==EmployeeIntentPurpose.DepartingWorkforceArea;
     yield return null;
    }
    sawPurposefulDeparture|=normal.Employee.CurrentIntent.Purpose==EmployeeIntentPurpose.DepartingWorkforceArea;
    Assert.That(placementArea.Contains(normal.transform.position),Is.False,"Purposeful post-hire departure leaves the workforce placement area.");
    Assert.That(sawPurposefulDeparture,Is.True,"Departure uses an explicit non-random movement intent.");
    deadline=Time.realtimeSinceStartup+15;
    while(normal.Employee.CurrentState!=EmployeeState.Idle&&Time.realtimeSinceStartup<deadline)yield return null;
    Assert.That(normal.Employee.CurrentState,Is.EqualTo(EmployeeState.Idle),"The employee may settle after the purposeful departure.");
    Assert.That(normal.Employee.CurrentIntent.Purpose,Is.Not.EqualTo(EmployeeIntentPurpose.IdleWander),
     "Autonomy-controlled employees do not resume random idle wandering.");
    var settledPosition=normal.transform.position;
    yield return new WaitForSecondsRealtime(2f);
    Assert.That(Vector3.Distance(normal.transform.position,settledPosition),Is.LessThan(.05f),
     "An idle employee remains stationary when no activity is worthwhile.");
    Assert.That(time.Autonomy.LastDecision(normal.Employee)?.Activity,Is.EqualTo(PersonAutonomousActivity.IdleWait));
    Note(role+" completed purposeful workforce-area departure and remained idle without random wandering.");
   }
   Note("PASS: Actor and Director end-to-end input hiring; Extra covered by focused domain tests.");
  }
  [UnityTest]public IEnumerator ConstructionDispatchLifecycle()
  {
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False,"Preserve unsaved scene work");
   SessionState.SetString("C1.previous",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
   EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
   using(var bootstrap=new SerializedObject(Object.FindAnyObjectByType<StudioBootstrap>()))
   {
    bootstrap.FindProperty("_startNewStudio").boolValue=true;
    bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;
    bootstrap.ApplyModifiedPropertiesWithoutUndo();
   }
   yield return new EnterPlayMode();
   yield return null;
   yield return null;
   bool runInBackground=Application.runInBackground;
   Application.runInBackground=true;
   try{yield return ConstructionDispatchProof();}
   finally{Application.runInBackground=runInBackground;}
  }

  static IEnumerator ConstructionDispatchProof()
  {
   var driver=Object.FindAnyObjectByType<StudioConstructionDriver>();
   var time=Object.FindAnyObjectByType<SimulationTimeDriver>();
   var employees=Object.FindAnyObjectByType<StudioEmployeeManager>();
   Assert.That(driver,Is.Not.Null,"New-studio construction driver exists.");
   Assert.That(driver?.Service,Is.Not.Null,"New-studio construction driver initializes.");
   Assert.That(time,Is.Not.Null,"New-studio simulation clock initializes.");
   Assert.That(employees,Is.Not.Null,"New-studio workforce initializes.");
   yield return null;
   Assert.That(employees.AllEmployees,Is.Empty);
   foreach(var recruitment in Object.FindObjectsByType<RecruitmentDriver>(FindObjectsSortMode.None))recruitment.enabled=false;
   time.Clock.SetSpeed(SimulationSpeed.VeryFast);

   var siteA=PlaceCastingOffice(driver);
   Assert.That(siteA,Is.Not.Null,"Site A should be legally placeable.");
   yield return WaitForNavigation(driver);
   var workers=new EmployeeAgent[4];
   for(int i=0;i<workers.Length;i++)
   {
    int index=i;
    yield return AddConstructionWorker(driver,time,employees,"dispatch-worker-"+i,agent=>workers[index]=agent);
    if(i<3)
    {
     Assert.That(driver.Service.HasAssignment(workers[i].Employee.Id),Is.True,
      "Employee-added dispatch should assign each worker to an open Site A position.");
     Assert.That(AssignedSite(driver,workers[i].Employee.Id),Is.EqualTo(siteA.Id));
     if(i==0)Assert.That(driver.Service.WorkerArrived(workers[i].Employee.Id),Is.True);
    }
   }
   Assert.That(driver.Service.AssignedCount(siteA.Id),Is.EqualTo(3));
   Assert.That(driver.Service.ActiveWorkerCount(siteA.Id),Is.EqualTo(1),
    "Two reserved workers are still travelling while only one has arrived.");
   Assert.That(driver.Service.HasAssignment(workers[3].Employee.Id),Is.False,
    "A new worker must not join when all phase capacity is reserved, even with fewer arrived workers.");

   var siteB=PlaceCastingOffice(driver);
   Assert.That(siteB,Is.Not.Null,"Site B should be legally placeable.");
   yield return WaitForNavigation(driver);
   driver.DispatchWorkers();
   Assert.That(workers.Take(3).All(w=>AssignedSite(driver,w.Employee.Id)==siteA.Id),Is.True,
    "Creating Site B and running its navigation-ready dispatch must preserve Site A assignments.");
   Assert.That(AssignedSite(driver,workers[3].Employee.Id),Is.EqualTo(siteB.Id),
    "The idle worker should fill Site B without taking an assigned Site A worker.");

   var siteBAssignee=workers[3].Employee.Id;
   foreach(var worker in workers.Take(3))
    if(!driver.Service.CaptureAssignments().Single(a=>a.EmployeeId==worker.Employee.Id).Arrived)
     Assert.That(driver.Service.WorkerArrived(worker.Employee.Id),Is.True);
   var firstPhase=siteA.Phase;
   for(int tick=0;siteA.Phase==firstPhase&&tick<8;tick++)
   {
    time.Clock.Advance(60);
    while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}
    yield return null;
   }
   Assert.That(siteA.Phase,Is.Not.EqualTo(firstPhase),"Site A should progress into its next phase.");
   Assert.That(AssignedSite(driver,siteBAssignee),Is.EqualTo(siteB.Id),
    "A Site A phase transition must not clear an unrelated Site B assignment.");
   Assert.That(workers.Take(3).Any(w=>driver.Service.HasAssignment(w.Employee.Id)),Is.True,
    "Released Site A workers should be reconsidered after the phase transition.");

   for(int tick=0;!siteA.IsOperational&&tick<40;tick++)
   {
    driver.DispatchWorkers();
    foreach(var assignment in driver.Service.CaptureAssignments().Where(a=>a.BuildingId==siteA.Id&&!a.Arrived).ToArray())
     Assert.That(driver.Service.WorkerArrived(assignment.EmployeeId),Is.True);
    time.Clock.Advance(60);
    while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}
    yield return null;
   }
   Assert.That(siteA.IsOperational,Is.True,"Site A should complete through the shared construction work service.");
   yield return WaitForNavigation(driver);
   driver.DispatchWorkers();
   Assert.That(workers.Take(3).Any(w=>AssignedSite(driver,w.Employee.Id)==siteB.Id),Is.True,
    "Workers released at Site A completion should be available to active Site B.");
  }

  static PlacedBuilding PlaceCastingOffice(StudioConstructionDriver driver)
  {
   var lot=driver.Lot.Buildable;
   for(float x=lot.X-lot.Width/2+18;x<lot.X+lot.Width/2-18;x+=5)
    for(float z=lot.Z-lot.Depth/2+18;z<lot.Z+lot.Depth/2-18;z+=5)
    {
     var pose=new BuildingPose(x,z,0);
     if(driver.Service.Validate(StarterFeatureIds.Casting,pose)==null)
      return driver.Service.Place(StarterFeatureIds.Casting,pose,out _);
    }
   return null;
  }

  static IEnumerator WaitForNavigation(StudioConstructionDriver driver)
  {
   float deadline=Time.realtimeSinceStartup+45;
   while(driver.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That(driver.NavigationUpdatePending,Is.False,"Coalesced NavMesh update should complete.");
  }

  static IEnumerator AddConstructionWorker(StudioConstructionDriver driver,SimulationTimeDriver time,
   StudioEmployeeManager employees,string id,Action<EmployeeAgent> added)
  {
   var go=new GameObject(id);
   var nav=go.AddComponent<NavMeshAgent>();
   EmployeeNavigationProfile.Configure(nav,EmployeeNavigationProfile.Height,EmployeeNavigationProfile.Height/2);
   var requested=driver.Lot.ApplicantArrival+Vector3.right*(employees.AllEmployees.Count*1.25f);
   Assert.That(NavMesh.SamplePosition(requested,out var start,3f,NavMesh.AllAreas),Is.True,"Construction worker spawn must be on the lot NavMesh.");
   nav.enabled=false;go.transform.position=start.position+Vector3.up*nav.baseOffset;nav.enabled=true;
   yield return null;
   Assert.That(nav.Warp(start.position),Is.True);
   var agent=go.AddComponent<EmployeeAgent>();
   var employee=new Employee(id,id,EmployeeRole.ConstructionWorker,50,100);
   agent.BindDomain(employee);agent.BindTimeService(time.TimeService);
   employees.RegisterEmployee(employee,agent);
   added(agent);
   yield return null;
  }

  static string AssignedSite(StudioConstructionDriver driver,string employeeId)
   =>driver.Service.CaptureAssignments().SingleOrDefault(a=>a.EmployeeId==employeeId)?.BuildingId;

  static void Capture(UnityEngine.Camera camera,string name,GameObject root)
   =>SilverScreen.Editor.EnvironmentArt.StudioServicesProductionReview.Capture(camera,Out+"/"+name+".png",camera.transform.position,root.transform.TransformPoint(new Vector3(0,0,-5)),40,true,14);
  static Bounds RoomBounds(ContextualDropTarget target)
  {
   var bounds=new Bounds(target.Placement.position,Vector3.zero);
   foreach(var part in target.RoomParts)
    foreach(var corner in new[]{new Vector3(part.xMin,0,part.yMin),new Vector3(part.xMax,0,part.yMin),
     new Vector3(part.xMax,0,part.yMax),new Vector3(part.xMin,0,part.yMax)})
     bounds.Encapsulate(target.transform.TransformPoint(corner));
   bounds.Expand(new Vector3(.8f,4f,.8f));
   return bounds;
  }
 }
}
