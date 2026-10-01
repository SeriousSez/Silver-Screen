using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Editor.EnvironmentArt;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.ReusableAssets;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class StudioServicesProductionTests
    {
        static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath);
        [TestCase("FenceGateWire_180_1930")]
        [TestCase("FenceGateWire_180_RH_1930")]
        public void GateHardwareRetainsHumanScaleAfterHandedExport(string asset)
        {
            var gate=AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.Kit+"/Prefabs/"+asset+".prefab");
            var bounds=gate.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            Assert.That(bounds.size.z,Is.LessThan(.35f),"Thin gate hardware must not contain an unscaled primitive sphere.");
            Assert.That(bounds.size.x,Is.InRange(1.85f,2.05f));
            Assert.That(bounds.size.y,Is.InRange(2.0f,2.06f));
        }
        [Test] public void ShelterGutterRunHasContinuousModuleSockets()
        {
            var go=Object.Instantiate(Prefab);
            try
            {
                var run=go.transform.Find("YardShelterRoof").GetComponentsInChildren<ReusableFixture>()
                    .Where(f=>f.name.StartsWith("GutterHalfRound")).OrderBy(f=>f.transform.position.z).ToArray();
                Assert.That(run.Length,Is.EqualTo(5));
                for(int i=0;i<run.Length-1;i++)
                {
                    var a=run[i].transform.Find("Anchors");var b=run[i+1].transform.Find("Anchors");
                    float gap=new[]{"Start","End"}.SelectMany(x=>new[]{"Start","End"}.Select(y=>Vector3.Distance(a.Find(x).position,b.Find(y).position))).Min();
                    Assert.That(gap,Is.LessThan(.001f),run[i].name+" to "+run[i+1].name);
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void IndependentCatalogHasStableIdsSharedMaterialsAndEra()
        {
            var definitions=AssetDatabase.FindAssets("t:ReusableAssetDefinition",new[]{StudioServicesProductionBuilder.Kit}).Select(g=>AssetDatabase.LoadAssetAtPath<ReusableAssetDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(definitions.Length,Is.EqualTo(63));Assert.That(definitions.Select(d=>d.StableId).Distinct().Count(),Is.EqualTo(definitions.Length));
            foreach(var d in definitions)
            {
                Assert.That(d.Prefab,Is.Not.Null,d.name);Assert.That(d.CatalogEntry.AvailableFromYear,Is.EqualTo(1930));Assert.That(d.CatalogEntry.AvailableUntilYear,Is.Null);
                Assert.That(d.Prefab.GetComponent<ReusableFixture>().Definition,Is.SameAs(d));Assert.That(d.Prefab.transform.localScale,Is.EqualTo(Vector3.one));
                Assert.That(d.Prefab.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(StudioServicesProductionBuilder.Kit+"/Materials/"))),Is.True,d.name);
            }
        }
        [Test] public void LocalAnchorsAndProductionIdentityAreComplete()
        {
            var facility=Prefab.GetComponent<StudioServicesFacility>();Assert.That(facility.FacilityId,Is.EqualTo("building.studio-service"));
            Assert.That(Prefab.GetComponent<StudioBuildingView>().BuildingType,Is.EqualTo(BuildingType.StudioServices));
            foreach(var n in new[]{"OfficeEntrance","WorkshopEntrance","ApplicantWaiting_00","HireConstructionWorker","HireGroundskeeper","ConstructionMaterialPickup","ToolPickup","WorkshopWork","GroundskeeperSupply"})Assert.That(facility.Anchor(n),Is.Not.Null,n);
            Assert.That(facility.ConstructionMetadata.Segments.Count,Is.EqualTo(2));Assert.That(facility.ConstructionMetadata.Exclusions.Count,Is.EqualTo(3));
            Assert.That(Prefab.GetComponentsInChildren<PersonInteractionSpot>().All(s=>s.InteriorVisibility==Prefab.GetComponent<BuildingCutawayController>()),Is.True);
        }
        [Test] public void CutawayRestoresInitiallyDisabledRenderersAndDoesNotDisablePhysics()
        {
            var go=Object.Instantiate(Prefab);
            try
            {
                var cut=go.GetComponent<BuildingCutawayController>();var chosen=cut.Groups.First(g=>g.Id=="FrontWall").Renderers[0];chosen.enabled=false;
                var collider=go.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger);
                cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,true,go.transform.position+new Vector3(20,20,-20));
                cut.SetReveal(BuildingRevealReason.BuildingFocus,true,go.transform.position+new Vector3(20,20,-20));
                cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,false,go.transform.position);Assert.That(cut.IsRevealed,Is.True);
                cut.SetReveal(BuildingRevealReason.BuildingFocus,false,go.transform.position);Assert.That(cut.IsRevealed,Is.False);Assert.That(chosen.enabled,Is.False);Assert.That(collider.enabled,Is.True);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ReviewPreflightHasNoBlockedAnchorsOrRoutes()
        {
            var report=JsonUtility.FromJson<StudioServicesProductionReview.Report>(File.ReadAllText(StudioServicesProductionReview.Review+"/checks.json"));
            Assert.That(report.checks.Where(c=>!c.passed).Select(c=>c.name+": "+c.evidence),Is.Empty);
        }
        [TestCase(ProfessionalRole.Actor,ProfessionalRole.ConstructionWorker)]
        [TestCase(ProfessionalRole.Director,ProfessionalRole.Groundskeeper)]
        [TestCase(ProfessionalRole.Groundskeeper,ProfessionalRole.Actor)]
        public void HiringOriginDoesNotRestrictDestinationProfession(ProfessionalRole sought,ProfessionalRole chosen)
        {
            var clock=new SimulationClock();using var recruitment=new RecruitmentCoordinator(clock,new CandidateGenerator(new SeededRandomSource(12)),new ImmediateRouter());
            recruitment.Facilities=new FacilityApplicantPool();recruitment.Facilities.Register(new RecruitmentFacility("source",RecruitmentDestination.CastingOffice,new[]{sought}));
            var candidate=recruitment.TryGenerateArrival();var source=candidate.Person;
            recruitment.Facilities.Register(new RecruitmentFacility("target",RecruitmentDestination.ServiceFacility,new[]{chosen}));
            var hired=recruitment.Hire(candidate,chosen,"target");Assert.That(hired,Is.Not.Null);Assert.That(hired.Person,Is.SameAs(source));Assert.That(candidate.JobSought,Is.EqualTo(sought));
        }
        sealed class ImmediateRouter:ICandidateWorldRouter
        {
            public CandidateRouteResult RouteToRecruitmentLocation(Candidate c,Action<CandidateRouteResult,int> callback){callback(CandidateRouteResult.Started,0);return CandidateRouteResult.Started;}
            public CandidateRouteResult RouteToExit(Candidate c,Action<CandidateRouteResult,int> callback){callback(CandidateRouteResult.Started,0);return CandidateRouteResult.Started;}
            public void ReleaseCandidate(Candidate c){}
        }
        [UnityTearDown] public IEnumerator ExitRuntime()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(SessionState.GetBool("StudioServices.NewStudioProof",false))
            {
                SessionState.EraseBool("StudioServices.NewStudioProof");
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            }
        }
        [UnityTest] public IEnumerator ActualStudioArrivalCarryCutawayHireAndInteriorRoute()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty,Is.False,"Runtime proof must not discard unsaved scene work.");
            if(SceneManager.GetActiveScene().path!="Assets/Scenes/Studio.unity")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            // Exercise the existing developer New Studio entry point in memory only.
            // The saved Studio also supports the older populated production validation setup.
            var bootstrap=new SerializedObject(Object.FindAnyObjectByType<SilverScreen.Presentation.Bootstrap.StudioBootstrap>());
            bootstrap.FindProperty("_startNewStudio").boolValue=true;
            bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;
            bootstrap.ApplyModifiedPropertiesWithoutUndo();SessionState.SetBool("StudioServices.NewStudioProof",true);
            yield return new EnterPlayMode();yield return null;yield return null;
            yield return RuntimeProof();
        }
        static IEnumerator RuntimeProof()
        {
            var facility=Object.FindAnyObjectByType<StudioServicesFacility>();Assert.That(facility,Is.Not.Null);
            var employees=Object.FindAnyObjectByType<StudioEmployeeManager>();Assert.That(employees.AllEmployees.Count,Is.Zero);
            var recruitment=Object.FindAnyObjectByType<RecruitmentDriver>();Assert.That(recruitment.Coordinator,Is.Not.Null);
            Assert.That(recruitment.Coordinator.Facilities.Facilities.Count,Is.EqualTo(1));
            var candidate=recruitment.Coordinator.TryGenerateArrival();Assert.That(candidate,Is.Not.Null);Assert.That(candidate.FacilityId,Is.EqualTo(facility.FacilityId));
            float deadline=Time.realtimeSinceStartup+20;
            while(candidate.Status==CandidateStatus.Arriving&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(candidate.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment),"Applicant must physically arrive through the real lot navigation.");
            Assert.That(recruitment,Is.Not.Null,"Recruitment survives arrival");Assert.That(recruitment.WorldRouter,Is.Not.Null,"Candidate world router");
            Debug.Log("Services proof: arrival complete; resolving applicant view. Candidate="+(candidate?.Person?.Name??"NULL")+" router="+recruitment.WorldRouter.name);
            var agent=recruitment.WorldRouter.GetAgent(candidate);
            Assert.That(agent,Is.Not.Null,"Waiting applicant view");
            var person=candidate.Person;
            Debug.Log("Services proof: applicant resolved; acquiring pause.");
            var time=Object.FindAnyObjectByType<SimulationTimeDriver>();Assert.That(time,Is.Not.Null,"Studio time driver");using var pause=time.Clock.AcquirePause("studio-services-proof","Stable input review");
            var camera=Camera.main;Assert.That(camera,Is.Not.Null,"Active Studio management camera");var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;bool orthographic=camera.orthographic;float orthoSize=camera.orthographicSize;
            var selection=Object.FindAnyObjectByType<StudioSelectionController>();Assert.That(selection,Is.Not.Null,"Studio person input owner");bool selectionEnabled=selection.enabled;selection.enabled=false;
            var controller=selection.PersonInteraction;Assert.That(controller,Is.Not.Null,"Person interaction initialized");var cut=facility.GetComponent<BuildingCutawayController>();
            Debug.Log("Services proof: input owners ready.");
            var update=InputSystem.settings.updateMode;var background=InputSystem.settings.backgroundBehavior;var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;bool run=Application.runInBackground;
            var muted=InputSystem.devices.Where(d=>d.enabled&&(d is Mouse||d is Keyboard)).ToArray();foreach(var d in muted)InputSystem.DisableDevice(d);
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
            var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
            var ui=Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);foreach(var e in ui)e.enabled=false;
            var rig=camera.transform.parent; // Freeze camera input only during the deterministic input proof.
            var cameraDrivers=rig!=null?rig.GetComponents<MonoBehaviour>().Where(c=>c.enabled).ToArray():Array.Empty<MonoBehaviour>();foreach(var c in cameraDrivers)c.enabled=false;
            try
            {
                camera.transform.SetPositionAndRotation(facility.transform.position+new Vector3(-3,21,-8),Quaternion.Euler(75,0,0));camera.orthographic=true;camera.orthographicSize=10;
                void Pointer(Vector3 p,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(p),buttons=(ushort)(down?1:0)});InputSystem.Update();controller.HandleInput();}
                Physics.SyncTransforms();var pickup=agent.transform.position;
                Pointer(pickup,true);yield return new WaitForSecondsRealtime(.24f);pickup=agent.transform.position;Pointer(pickup,true);Assert.That(controller.IsHolding,Is.True);
                var room=facility.GetComponentsInChildren<ContextualDropTarget>().Single(t=>t.Action is WorkforceRoomAction a&&!a.Dismissal&&a.Profession==ProfessionalRole.ConstructionWorker);
                var target=room.Placement;Pointer(target.position,false);Assert.That(controller.IsHolding,Is.True,"Original release retains carry");Assert.That(cut.IsRevealed,Is.True);
                Assert.That(controller.VisibleTargets.Contains(room),Is.True);
                string review=StudioServicesProductionReview.Review;Directory.CreateDirectory(review);
                StudioServicesProductionReview.Capture(camera,review+"/14_live_held_cutaway.png",camera.transform.position,facility.transform.position+new Vector3(-3,0,-2),40,true,10);
                // Escape restores both pickup position and exterior before a second complete carry.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputSystem.Update();controller.HandleInput();
                Assert.That(controller.IsHolding,Is.False);Assert.That(cut.IsRevealed,Is.False);Assert.That(Vector3.Distance(agent.transform.position,pickup),Is.LessThan(.01f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                Pointer(pickup,true);yield return new WaitForSecondsRealtime(.24f);Pointer(pickup,true);Pointer(target.position,false);yield return null;
                Pointer(target.position,true);Assert.That(controller.IsHolding,Is.False,"Separate valid click places at the authored interior spot");Assert.That(cut.IsRevealed,Is.True,"Post-interaction hold keeps the placed person visible");
                Assert.That(employees.AllEmployees.Count,Is.EqualTo(1));Assert.That(employees.AllEmployees[0].Person,Is.SameAs(person));Assert.That(employees.AllEmployees[0].Role,Is.EqualTo(EmployeeRole.ConstructionWorker));
                var employee=Object.FindObjectsByType<EmployeeAgent>(FindObjectsSortMode.None).Single(a=>a.Employee?.Id==person.Id);var nav=employee.GetComponent<NavMeshAgent>();
                Assert.That(Vector3.Distance(employee.transform.position-Vector3.up*nav.baseOffset,target.position),Is.LessThan(.16f));
                pause.Dispose();yield return null;
                foreach(var destination in new[]{facility.Anchor("OfficeApproach"),facility.Anchor("WorkshopWork"),facility.Anchor("GroundskeeperSupply")})
                {
                    bool arrived=false;Assert.That(employee.TryAssignTaskDestination(destination.position,new EmployeeIntent(EmployeeIntentPurpose.None,"Studio Services route proof",facility.FacilityId),()=>arrived=true),Is.True,destination.name);
                    deadline=Time.realtimeSinceStartup+25;
                    while(!arrived&&Time.realtimeSinceStartup<deadline)
                    {
                        var feet=employee.transform.position-Vector3.up*nav.baseOffset;
                        var obstruction=Physics.OverlapCapsule(feet+Vector3.up*.41f,feet+Vector3.up*1.65f,.345f,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(c=>c.transform.IsChildOf(facility.transform));
                        Assert.That(obstruction,Is.Null,"Physical route contacts "+obstruction?.name+" at "+feet);
                        yield return null;
                    }
                    Assert.That(arrived,Is.True,destination.name+" physical route timed out at "+employee.transform.position+" remaining "+nav.remainingDistance);
                }
                var second=recruitment.Coordinator.Candidates.FirstOrDefault(c=>c.Status==CandidateStatus.WaitingForRecruitment)??recruitment.Coordinator.TryGenerateArrival();
                deadline=Time.realtimeSinceStartup+20;while(second.Status==CandidateStatus.Arriving&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(second.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment));
                using var secondPause=time.Clock.AcquirePause("services-second-hire","Validate the second authored hiring spot");
                var secondAgent=recruitment.WorldRouter.GetAgent(second);Assert.That(secondAgent,Is.Not.Null);
                Pointer(secondAgent.transform.position,false);Pointer(secondAgent.transform.position,true);yield return new WaitForSecondsRealtime(.24f);Pointer(secondAgent.transform.position,true);
                var grounds=facility.Anchor("HireGroundskeeper");Pointer(grounds.position,false);Assert.That(cut.IsRevealed,Is.True);Pointer(grounds.position,true);
                Assert.That(controller.IsHolding,Is.False);Assert.That(cut.IsRevealed,Is.False);Assert.That(employees.AllEmployees.Any(e=>e.Person==second.Person&&e.Role==EmployeeRole.Groundskeeper),Is.True);
                StudioServicesProductionReview.Capture(camera,review+"/17_live_exterior.png",facility.transform.position+new Vector3(19,14,-23),facility.transform.position+new Vector3(1,1,0),42);
                File.WriteAllText(review+"/runtime.txt","Unity "+Application.unityVersion+" Play Mode: empty studio; physical applicant arrival; original mouse release retains carry; generic cutaway; Escape restores actual pickup/exterior; separate-click immediate authored hiring into BOTH professions; same Person identities; real NavMesh route through personnel entrance to workshop and storage; per-frame capsule checks against facility collision. Passed.\n");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);foreach(var d in muted)InputSystem.EnableDevice(d);
                InputSystem.settings.updateMode=update;InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;Application.runInBackground=run;
                foreach(var e in ui)if(e!=null)e.enabled=true;foreach(var c in cameraDrivers)if(c!=null)c.enabled=true;
                selection.enabled=selectionEnabled;camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.orthographic=orthographic;camera.orthographicSize=orthoSize;
            }
        }
    }
    public static class StudioServicesTestRunner
    {
        static TestRunnerApi _api;
        public static void Run()
        {
            _api=ScriptableObject.CreateInstance<TestRunnerApi>();_api.RegisterCallbacks(new Results());
            _api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{
                "SilverScreen.Tests.EditMode.StudioServicesProductionTests","SilverScreen.Tests.EditMode.PersonInteractionTests",
                "SilverScreen.Tests.EditMode.PersonManipulationRuntimeTests","SilverScreen.Tests.EditMode.HeldPersonPresentationTests",
                "SilverScreen.Tests.EditMode.BuildingPlacementAndConstructionTests","SilverScreen.Tests.RecruitmentTests"}}));
        }
        sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor t){}public void TestStarted(ITestAdaptor t){}public void TestFinished(ITestResultAdaptor r){}
            public void RunFinished(ITestResultAdaptor r){Directory.CreateDirectory(StudioServicesProductionReview.Review);TestRunnerApi.SaveResultToFile(r,StudioServicesProductionReview.Review+"/tests.xml");Debug.Log("Studio Services focused validation: "+r.PassCount+" passed, "+r.FailCount+" failed, "+r.SkipCount+" skipped.");Object.DestroyImmediate(_api);_api=null;}
        }
    }
}
