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
 public class StageSchoolB2Validation
 {
  const string Out="ArtReview/StageSchoolB2";
  static readonly List<string> Lines=new List<string>();
  static void Check(bool ok,string text){Lines.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Out+"/live.txt",Lines);}
  static void Note(string text){Lines.Add(text);File.WriteAllLines(Out+"/live.txt",Lines);}
  public static void Run()
  {
   var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(api));
   api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{"SilverScreen.Tests.Editor.StageSchoolB2Validation.LiveRuntime"}}));
  }
  public static void RunEditMode()
  {
   var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(api,"edit-results.xml"));
   api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{
    "SilverScreen.Tests.Editor.StageSchoolRuntimeTests","SilverScreen.Tests.EditMode.ConstructionDressingTests",
    "SilverScreen.Tests.EditMode.ConstructionV2APlacementTests","SilverScreen.Tests.EditMode.ConstructionV2BWorkforceTests"}}));
  }
  sealed class Results:ICallbacks
  {
   readonly TestRunnerApi api;readonly string file;public Results(TestRunnerApi a,string name="live-results.xml"){api=a;file=name;}
   public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor t){}
   public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,Out+"/"+file);Object.DestroyImmediate(api);}
  }
  static IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
  [UnityTest] public IEnumerator LiveRuntime()
  {
   Directory.CreateDirectory(Out);Lines.Clear();Note("One focused Play Mode session; development-controlled labor advancement.");
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False,"Preserve unsaved working scene");
   EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
   var bootstrap=new SerializedObject(Object.FindAnyObjectByType<SilverScreen.Presentation.Bootstrap.StudioBootstrap>());
   bootstrap.FindProperty("_startNewStudio").boolValue=true;bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;bootstrap.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),StageSchoolB2Builder.Root+"/StageSchool_B2IntegrationReview.unity");
   yield return new EnterPlayMode();
   // Wall-clock navigation/input deadlines require an updating Editor while Codex is foreground.
   bool previousBackground=Application.runInBackground;Application.runInBackground=true;
   yield return null;yield return null;
   try{yield return RuntimeProof();}
   finally{Application.runInBackground=previousBackground;}
   yield return new ExitPlayMode();
   var failures=File.ReadAllLines(Out+"/live.txt").Where(l=>l.StartsWith("FAIL ")).ToArray();
   Assert.That(failures,Is.Empty,string.Join("\n",failures));
   EditorSceneManager.OpenScene(StageSchoolB2Builder.Root+"/StageSchool_B2Review.unity");
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
   bool clear=true;
   foreach(var phase in building.Definition.Phases){
    var plan=ConstructionDressingGenerator.Generate(building,phase);
    foreach(var m in plan.Modules)clear&=m.GroundBounds.Corners(default).All(p=>building.Definition.PlacementAreas.Any(a=>a.Contains(p)));
   }
   Check(clear,"All construction phases keep dressing inside the reserved union at yaw37");
   var neighbour=GameObject.CreatePrimitive(PrimitiveType.Cube);neighbour.name="Legal neighbouring footprint";neighbour.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(13.1f,1.5f,-6)),root.transform.rotation);neighbour.transform.localScale=new Vector3(2,3,2);
   var camera=UnityEngine.Camera.main;foreach(var c in Object.FindObjectsByType<SilverScreen.Presentation.Camera.StudioCameraController>(FindObjectsSortMode.None))c.enabled=false;
   var focus=root.transform.TransformPoint(new Vector3(0,2,-1));camera.transform.position=focus+new Vector3(30,27,-34);camera.transform.LookAt(focus);
   yield return Wait(.3f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+"/08_compound_site.png"));yield return Wait(.3f);
   Object.Destroy(neighbour);
   float deadline=Time.realtimeSinceStartup+30;while(driver.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(!driver.NavigationUpdatePending,"Placement async navigation completes");
   var workers=Enumerable.Range(0,3).Select(i=>new Employee("b1-builder-"+i,"B2 builder "+i,EmployeeRole.ConstructionWorker,65,100)).ToArray();
   var phases=new HashSet<ConstructionPhase>();bool asyncObserved=false;double worst=0;
   var frameRecorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread",64);
   var completionFrames=new List<double>();int completionFrame=-1;
   driver.Service.Operational+=b=>{if(b==building)completionFrame=Time.frameCount;};
   driver.Service.Operational+=b=>{if(b==building)asyncObserved=driver.NavigationUpdatePending;};
   // Uses real work assignments, proficiency/diminishing returns, reservations,
   // phase work and completion event. Travel is accelerated via arrival admission.
   for(int tick=0;tick<250&&!building.IsOperational;tick++){
    if(phases.Add(building.Phase)){
     Note("Phase "+building.Phase+" capacity="+driver.Service.CurrentWorkerCapacity(building));
     if(building.Phase==ConstructionPhase.Structure){yield return Wait(.2f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+"/10_structure_progress.png"));yield return Wait(.2f);}
    }
    foreach(var worker in workers){if(!driver.Service.HasAssignment(worker.Id))driver.Service.AssignWorker(building,worker);driver.Service.WorkerArrived(worker.Id);}
    var status=driver.Service.GetStatus(building);if(tick==0)Check(status.ActiveWorkers==3&&status.RelativeProductivity==BuildingConstructionService.TeamProductivity(workers),"Three workers use real V2B productivity");
    var watch=System.Diagnostics.Stopwatch.StartNew();time.Clock.Advance(60);watch.Stop();worst=Math.Max(worst,watch.Elapsed.TotalMilliseconds);
    while(!time.Clock.IsBoundaryComplete){time.Clock.Advance(0);yield return null;}yield return null;
    if(building.IsOperational)completionFrames.Add(frameRecorder.LastValue/1000000d);
   }
   Check(building.IsOperational,"Real shared construction reaches completion");Check(phases.Count==5,"All five phases traversed");
   Check(root.GetComponent<StudioBuildingView>().enabled&&site.DressingRoot==null,"Completion enables production building and removes dressing");
   Check(workers.All(w=>!driver.Service.HasAssignment(w.Id)&&!time.Reservations.IsReserved(new SilverScreen.Domain.Resources.ResourceKey("person",w.Id))),"Builder assignments and person reservations released");
   Check(asyncObserved,"Actual completion schedules asynchronous navigation");Check(time.Clock.CurrentSpeed==speed&&Mathf.Approximately(Time.timeScale,time.Clock.TimeScaleMultiplier),"Selected simulation speed and world adapter preserved");Note("Worst clock method ms="+worst.ToString("F2"));
   for(int i=0;i<8;i++){yield return null;completionFrames.Add(frameRecorder.LastValue/1000000d);}
   Note("Completion frame="+completionFrame+"; Main Thread recorder valid="+frameRecorder.Valid+"; completion and next 8 frame ms="+string.Join(",",completionFrames.Select(v=>v.ToString("F2"))));
   frameRecorder.Dispose();
   deadline=Time.realtimeSinceStartup+40;while(driver.NavigationUpdatePending&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(!driver.NavigationUpdatePending,"Completion async navigation settles");
   yield return Wait(.4f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+"/09_real_completed.png"));yield return Wait(.3f);
   var infrastructure=root.GetComponent<StageSchoolInfrastructure>();
   foreach(var door in root.GetComponentsInChildren<AuthoredBuildingDoor>())door.SetOpen(false);
   yield return Wait(.3f);
   var person=new GameObject("B2 real navigation probe");var nav=person.AddComponent<NavMeshAgent>();EmployeeNavigationProfile.Configure(nav);nav.speed=5;nav.acceleration=30;nav.stoppingDistance=.08f;
   foreach(var id in new[]{"hiringhall.approach","audition.activity.screen-test","interview.activity"}){
    yield return Route(nav,infrastructure,"entrance.approach",id,root);
    if(id!="hiringhall.approach")yield return Route(nav,infrastructure,id,"exit",root);
   }
   Check(root.GetComponentsInChildren<AuthoredBuildingDoor>().Any(d=>d.IsOpen),"Agent automatically opened a closed authored door");
   var opposing=new GameObject("B2 opposing pedestrian");var nav2=opposing.AddComponent<NavMeshAgent>();EmployeeNavigationProfile.Configure(nav2);nav2.speed=5;nav2.acceleration=30;nav2.stoppingDistance=.08f;
   nav.Warp(infrastructure.Anchor("hiringhall.approach").position);nav2.Warp(infrastructure.Anchor("audition.activity.screen-test").position);
   foreach(var d in root.GetComponentsInChildren<AuthoredBuildingDoor>())d.SetOpen(false);yield return Wait(.4f);
   var first=nav2.transform.position;var second=nav.transform.position;nav.SetDestination(first);nav2.SetDestination(second);
   deadline=Time.realtimeSinceStartup+15;while(Time.realtimeSinceStartup<deadline&&(Vector3.Distance(nav.transform.position,first)>.3f||Vector3.Distance(nav2.transform.position,second)>.3f))yield return null;
   Check(Vector3.Distance(nav.transform.position,first)<.3f&&Vector3.Distance(nav2.transform.position,second)<.3f,"Opposing real agents both cross the audition doorway without deadlock");Object.Destroy(person);Object.Destroy(opposing);
   // Feed actual InputSystem events through the existing selection controller.
   foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
   var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();var keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();
   var reveal=root.GetComponent<BuildingCutawayController>();var selection=Object.FindAnyObjectByType<StudioSelectionController>();selection.Deselect();reveal.Restore();
   var pixel=(Vector2)camera.WorldToScreenPoint(root.transform.TransformPoint(new Vector3(0,5,-1)));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;
   Check(!reveal.IsRevealed,"Hover intent does not reveal immediately");yield return HoldPointer(mouse,pixel,.8f);Check(reveal.Reasons.HasFlag(BuildingRevealReason.PointerHover),"Actual sustained pointer hover intent and fade in");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel,buttons=1});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return Wait(.15f);
   Check(selection.SelectedBuilding==root.GetComponent<StudioBuildingView>(),"Actual mouse click pins building");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100)});yield return Wait(.8f);Check(reveal.IsRevealed,"Pinned reveal survives pointer departure");
   for(int i=0;i<12;i++){
    float h=new[]{.60f,.24f,.08f}[i%3];float distance=lod.size*QualitySettings.lodBias/(2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2)*h);
    camera.transform.position=focus+Quaternion.Euler(0,i*30,0)*new Vector3(0,.62f,-.78f).normalized*distance;camera.transform.LookAt(focus);yield return HoldPointer(mouse,new Vector2(-100,-100),.12f);
   }
   Note("Automatic LOD zoom covered adjusted heights .60/.24/.08 with lodBias="+QualitySettings.lodBias);
   Check(reveal.IsRevealed&&reveal.Groups.Where(g=>g.Roof).SelectMany(g=>g.Renderers).All(r=>r.forceRenderingOff),"Orbit/rapid zoom/automatic LOD while pinned keeps roofs hidden");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Wait(.9f);
   Check(!reveal.IsRevealed,"Actual Escape releases and fades out");
   for(int i=0;i<6;i++){camera.transform.position=focus+new Vector3(0,10+i%3*18,-18-i%3*22);camera.transform.LookAt(focus);yield return Wait(.15f);}
   Check(root.GetComponentsInChildren<Renderer>().All(r=>!r.forceRenderingOff),"Closed LOD transitions restore visibility");
   Note("Runtime checks finished; screenshots are actual site/completed state.");

  }
  static IEnumerator HoldPointer(Mouse mouse,Vector2 pixel,float seconds)
  {
   float end=Time.realtimeSinceStartup+seconds;
   while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;}
  }
  static IEnumerator Route(NavMeshAgent nav,StageSchoolInfrastructure infrastructure,string from,string to,GameObject root)
  {
   foreach(var door in root.GetComponentsInChildren<AuthoredBuildingDoor>())door.SetOpen(false);
   yield return Wait(.35f);
   var start=infrastructure.Anchor(from).position;var requested=infrastructure.Anchor(to).position;
   bool sampled=NavMesh.SamplePosition(start,out var sh,.3f,NavMesh.AllAreas);
   bool target=NavMesh.SamplePosition(requested,out var th,.3f,NavMesh.AllAreas);
   bool accepted=sampled&&target&&nav.Warp(sh.position)&&nav.SetDestination(th.position);
   int startFrame=Time.frameCount;float deadline=Time.realtimeSinceStartup+18;
   while(accepted&&Time.realtimeSinceStartup<deadline&&(nav.pathPending||Vector3.Distance(nav.transform.position,th.position)>.25f))yield return null;
   Check(accepted&&Vector3.Distance(nav.transform.position,th.position)<.25f,"Real agent "+from+" -> "+to+"; initially closed; status="+(nav.isOnNavMesh?nav.pathStatus.ToString():"off mesh")+"; frames="+(Time.frameCount-startFrame)+"; local="+root.transform.InverseTransformPoint(nav.transform.position)+"; remaining="+(nav.isOnNavMesh?nav.remainingDistance:-1));
  }

 }
}
