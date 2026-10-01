using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
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
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SilverScreen.Tests.EditMode
{
 public sealed class RecruitmentStarterPhysicalTests
 {
  const string Out="ArtReview/RecruitmentC11";
  static void Note(string text)=>File.AppendAllText(Out+"/play.txt",text+"\n");
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(Application.isPlaying)yield return new ExitPlayMode();
   string previous=SessionState.GetString("C11.previous","");SessionState.EraseString("C11.previous");if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
  }
  [UnityTest]public IEnumerator StarterApplicantsPhysicallyArrive()
  {
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False,"Preserve unsaved scene work");
   SessionState.SetString("C11.previous",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
   File.WriteAllText(Out+"/play.txt","C1.1 physical starter intake proof\n");
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
   var arrivals=new System.Collections.Generic.List<Candidate>();
   var times=new System.Collections.Generic.List<long>();
   var sources=new System.Collections.Generic.Dictionary<string,Vector3>();
   recruitment.Coordinator.OnCandidateAdded+=c=>{if(c.IsTalentApplicant){arrivals.Add(c);times.Add(time.Clock.Now.Seconds);}};
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

   float until=Time.realtimeSinceStartup+120;
   while(Time.realtimeSinceStartup<until){
    foreach(var c in arrivals){var a=recruitment.WorldRouter.GetAgent(c);if(a!=null&&!sources.ContainsKey(c.Person.Id))sources.Add(c.Person.Id,a.transform.position);}
    if(arrivals.Count>=4&&arrivals.All(c=>c.Status==CandidateStatus.WaitingForRecruitment))break;
    yield return null;
   }
   Assert.That(arrivals.Count,Is.EqualTo(4));Assert.That(arrivals.All(c=>c.Status==CandidateStatus.WaitingForRecruitment),Is.True);
   Assert.That(arrivals.Select(c=>c.WaitingPositionIndex).Distinct().Count(),Is.EqualTo(4));
   for(int i=1;i<times.Count;i++)Assert.That(times[i]-times[i-1],Is.GreaterThanOrEqualTo(8*60));
   foreach(var c in arrivals){var a=recruitment.WorldRouter.GetAgent(c);Assert.That(Vector3.Distance(sources[c.Person.Id],a.transform.position),Is.GreaterThan(2));Note(c.Person.Id+" source="+sources[c.Person.Id]+" waiting="+a.transform.position+" slot="+c.WaitingPositionIndex);}
   Assert.That(recruitment.Coordinator.StarterRemaining(RecruitmentCategory.Talent),Is.Zero);
   Assert.That(employees.AllEmployees,Is.Empty);
   Note("Four profession-neutral people navigated from world entry into four distinct authored waiting positions. Simulation timestamps: "+string.Join(",",times));
   var camera=UnityEngine.Camera.main;foreach(var driver in Object.FindObjectsByType<SilverScreen.Presentation.Camera.StudioCameraController>(FindObjectsSortMode.None))driver.enabled=false;
   camera.transform.position=root.transform.TransformPoint(new Vector3(0,23,-16));camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,0,-5)));camera.orthographic=true;camera.orthographicSize=14;
   SilverScreen.Editor.EnvironmentArt.StudioServicesProductionReview.Capture(camera,Out+"/starter-waiting.png",camera.transform.position,root.transform.TransformPoint(new Vector3(0,0,-5)),45,true,14);
  }
 }
}
