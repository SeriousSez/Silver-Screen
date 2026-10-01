using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Selection;
using SilverScreen.Editor.EnvironmentArt;
using Object=UnityEngine.Object;

namespace SilverScreen.Tests.Editor
{
 public class StageSchoolB1Validation
 {
  const string Out="ArtReview/StageSchoolB1";
  static readonly List<string> Lines=new List<string>();
  static void Check(bool ok,string text){Lines.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Out+"/live.txt",Lines);}
  static void Note(string text){Lines.Add(text);File.WriteAllLines(Out+"/live.txt",Lines);}
  public static void Run()
  {
   var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(api));
   api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{"SilverScreen.Tests.Editor.StageSchoolB1Validation.LiveRuntime"}}));
  }
  sealed class Results:ICallbacks
  {
   readonly TestRunnerApi api;public Results(TestRunnerApi a){api=a;}
   public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor t){}
   public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,Out+"/live-results.xml");Object.DestroyImmediate(api);}
  }
  static IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
  [UnityTest] public IEnumerator LiveRuntime()
  {
   Directory.CreateDirectory(Out);Lines.Clear();Note("One focused Play Mode session; development-controlled labor advancement.");
   LogAssert.ignoreFailingMessages=true;
   EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
   var bootstrap=new SerializedObject(Object.FindAnyObjectByType<SilverScreen.Presentation.Bootstrap.StudioBootstrap>());
   bootstrap.FindProperty("_startNewStudio").boolValue=true;bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;bootstrap.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),StageSchoolRuntimeBuilder.Root+"/StageSchool_B1IntegrationReview.unity");
   yield return new EnterPlayMode();yield return null;yield return null;
   yield return RuntimeProof();
   yield return new ExitPlayMode();LogAssert.ignoreFailingMessages=false;
   EditorSceneManager.OpenScene(StageSchoolRuntimeBuilder.Root+"/StageSchool_RuntimeReview.unity");
  }
  static IEnumerator RuntimeProof()
  {
   Note("Entered runtime: scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" playing="+Application.isPlaying);
   var driver=Object.FindAnyObjectByType<StudioConstructionDriver>();var time=Object.FindAnyObjectByType<SimulationTimeDriver>();
   Note("Runtime dependencies: construction="+(driver!=null)+" clock="+(time!=null));
   Assert.That(driver?.Service,Is.Not.Null);time.Clock.SetSpeed(SimulationSpeed.Fast);var speed=time.Clock.CurrentSpeed;
   // Existing recruitment is unrelated to this proof. No Stage School applicants created.
   foreach(var recruitment in Object.FindObjectsByType<SilverScreen.Presentation.Recruitment.RecruitmentDriver>(FindObjectsSortMode.None))recruitment.enabled=false;
   // Advance additional domain time below without replacing the speed adapter.
   PlacedBuilding building=null;string error=null;var lot=driver.Lot.Buildable;
   for(float x=lot.X-lot.Width/2+22;x<lot.X+lot.Width/2-22&&building==null;x+=7)
    for(float z=lot.Z-lot.Depth/2+22;z<lot.Z+lot.Depth/2-22&&building==null;z+=7){var pose=new BuildingPose(x,z,37);if(driver.Service.Validate(StudioBuildingDefinitions.StageSchool,pose)==null)building=driver.Service.Place(StudioBuildingDefinitions.StageSchool,pose,out error);}
   Assert.That(building,Is.Not.Null,"No legal yaw37 placement: "+error);var site=driver.Sites[building.Id];var root=site.FinishedContent;var lod=root.GetComponent<LODGroup>();
   Check(!building.IsOperational&&!root.GetComponent<StudioBuildingView>().enabled,"Placement creates gated site; yaw 37");
   Check(lod.enabled,"LODGroup remains enabled during construction");
   time.Clock.Advance(5);while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}
   Check(driver.Service.Progress(building)==0,"Zero workers means zero progress");
   var gap=new PlacementRect(13,-6,2,2);bool clear=true;
   foreach(var col in site.transform.Find("ConstructionPublicExclusion").GetComponentsInChildren<BoxCollider>()){
    var p=site.transform.InverseTransformPoint(col.transform.TransformPoint(col.center));clear&=!PlacementRect.Overlaps(new PlacementRect(p.x,p.z,col.size.x,col.size.z),default,gap,default);
   }
   foreach(var m in site.Plan.Modules)clear&=!PlacementRect.Overlaps(m.GroundBounds,default,gap,default);
   Check(clear,"Compound construction blockers and dressing leave legal recess neighbour clear at yaw37");
   var neighbour=GameObject.CreatePrimitive(PrimitiveType.Cube);neighbour.name="Legal recess neighbour proof";neighbour.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(13,1.5f,-6)),root.transform.rotation);neighbour.transform.localScale=new Vector3(2,3,2);
   var camera=UnityEngine.Camera.main;foreach(var c in Object.FindObjectsByType<SilverScreen.Presentation.Camera.StudioCameraController>(FindObjectsSortMode.None))c.enabled=false;
   var focus=root.transform.TransformPoint(new Vector3(0,2,2));camera.transform.position=focus+new Vector3(30,27,-34);camera.transform.LookAt(focus);
   yield return Wait(.3f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+"/08_compound_site.png"));yield return Wait(.3f);
   Object.Destroy(neighbour);
   float deadline=Time.realtimeSinceStartup+30;while(driver.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(!driver.NavigationUpdatePending,"Placement async navigation completes");
   var workers=Enumerable.Range(0,4).Select(i=>new Employee("b1-builder-"+i,"B1 builder "+i,EmployeeRole.ConstructionWorker,65,100)).ToArray();
   var phases=new HashSet<ConstructionPhase>();bool asyncObserved=false;double worst=0;
   driver.Service.Operational+=b=>{if(b==building)asyncObserved=driver.NavigationUpdatePending;};
   // Uses real work assignments, proficiency/diminishing returns, reservations,
   // phase work and completion event. Travel is accelerated via arrival admission.
   for(int tick=0;tick<250&&!building.IsOperational;tick++){
    if(phases.Add(building.Phase))Note("Phase "+building.Phase+" capacity="+driver.Service.CurrentWorkerCapacity(building));
    foreach(var worker in workers){if(!driver.Service.HasAssignment(worker.Id))driver.Service.AssignWorker(building,worker);driver.Service.WorkerArrived(worker.Id);}
    var status=driver.Service.GetStatus(building);if(tick==0)Check(status.ActiveWorkers==4&&status.RelativeProductivity==BuildingConstructionService.TeamProductivity(workers),"Four workers use real V2B productivity");
    var watch=System.Diagnostics.Stopwatch.StartNew();time.Clock.Advance(60);watch.Stop();worst=Math.Max(worst,watch.Elapsed.TotalMilliseconds);
    while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}yield return null;
   }
   Check(building.IsOperational,"Real shared construction reaches completion");Check(phases.Count==5,"All five phases traversed");
   Check(root.GetComponent<StudioBuildingView>().enabled&&site.DressingRoot==null,"Completion enables production building and removes dressing");
   Check(workers.All(w=>!driver.Service.HasAssignment(w.Id)&&!time.Reservations.IsReserved(new SilverScreen.Domain.Resources.ResourceKey("person",w.Id))),"Builder assignments and person reservations released");
   Check(asyncObserved,"Actual completion schedules asynchronous navigation");Check(time.Clock.CurrentSpeed==speed&&Mathf.Approximately(Time.timeScale,time.Clock.TimeScaleMultiplier),"Selected simulation speed and world adapter preserved");Note("Worst dev clock Advance call ms="+worst.ToString("F2")+" (not a frame profiler)");
   deadline=Time.realtimeSinceStartup+40;while(driver.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(!driver.NavigationUpdatePending,"Completion async navigation settles");
   yield return Wait(.4f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+"/09_real_completed.png"));yield return Wait(.3f);
   var infrastructure=root.GetComponent<StageSchoolInfrastructure>();
   foreach(var door in root.GetComponentsInChildren<AuthoredBuildingDoor>())door.SetOpen(false);
   yield return Wait(.3f);
   var person=new GameObject("B1 real navigation probe");var nav=person.AddComponent<NavMeshAgent>();EmployeeNavigationProfile.Configure(nav);nav.speed=7;nav.acceleration=30;nav.stoppingDistance=.08f;
   string[] destinations={"reception.approach","waiting.approach.0","audition.operator","interview.candidate","evaluation.candidate","staff.approach","records.approach","equipment.approach","service.approach","exit"};
   foreach(var id in destinations){
    var start=infrastructure.Anchor("entrance.approach").position;bool sampled=NavMesh.SamplePosition(start,out var sh,.5f,NavMesh.AllAreas);if(sampled)nav.Warp(sh.position);
    var requested=infrastructure.Anchor(id).position;bool target=NavMesh.SamplePosition(requested,out var th,.25f,NavMesh.AllAreas);
    if(id=="evaluation.candidate")Check(target&&Vector3.Distance(th.position,requested)<.05f,"Flexible Evaluation precise anchor delta="+(target?Vector3.Distance(th.position,requested):-1));
    bool accepted=sampled&&target&&nav.SetDestination(th.position);deadline=Time.realtimeSinceStartup+15;
    while(accepted&&Time.realtimeSinceStartup<deadline&&(nav.pathPending||Vector3.Distance(nav.transform.position,th.position)>.25f))yield return null;
    Check(accepted&&Vector3.Distance(nav.transform.position,th.position)<.25f,"Agent exterior -> "+id+" using door interaction; status="+(nav.isOnNavMesh?nav.pathStatus.ToString():"off mesh"));
   }
   Check(root.GetComponentsInChildren<AuthoredBuildingDoor>().Any(d=>d.IsOpen),"Agent automatically opened a closed authored door");Object.Destroy(person);
   // Feed actual InputSystem events through the existing selection controller.
   foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
   var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();var keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();
   var reveal=root.GetComponent<BuildingCutawayController>();var selection=Object.FindAnyObjectByType<StudioSelectionController>();selection.Deselect();reveal.Restore();
   var pixel=(Vector2)camera.WorldToScreenPoint(root.transform.TransformPoint(new Vector3(0,5.9f,0)));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;
   Check(!reveal.IsRevealed,"Hover intent does not reveal immediately");yield return Wait(.7f);Check(reveal.IsRevealed,"Actual pointer hover intent and fade in");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel,buttons=1});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return Wait(.15f);
   Check(selection.SelectedBuilding==root.GetComponent<StudioBuildingView>(),"Actual mouse click pins building");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100)});yield return Wait(.8f);Check(reveal.IsRevealed,"Pinned reveal survives pointer departure");
   for(int i=0;i<12;i++){
    camera.transform.position=focus+Quaternion.Euler(0,i*30,0)*new Vector3(0,10+i%3*18,-18-i%3*22);camera.transform.LookAt(focus);yield return Wait(.1f);
   }
   Check(reveal.IsRevealed&&reveal.Groups.Where(g=>g.Roof).SelectMany(g=>g.Renderers).All(r=>r.forceRenderingOff),"Orbit/rapid zoom/automatic LOD while pinned keeps roofs hidden");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Wait(.9f);
   Check(!reveal.IsRevealed,"Actual Escape releases and fades out");
   for(int i=0;i<6;i++){camera.transform.position=focus+new Vector3(0,10+i%3*18,-18-i%3*22);camera.transform.LookAt(focus);yield return Wait(.15f);}
   Check(root.GetComponentsInChildren<Renderer>().All(r=>!r.forceRenderingOff),"Closed LOD transitions restore visibility");
   Note("Runtime checks finished; screenshots are actual site/completed state.");
  }
 }
}
