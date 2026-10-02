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
    public sealed class ContextualCarryStudioTests
    {
        [UnityTearDown] public IEnumerator ExitRuntime()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(SessionState.GetBool("CarryV2.NewStudioProof",false))
            {
                SessionState.EraseBool("CarryV2.NewStudioProof");
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            }
        }
        [UnityTest] public IEnumerator StudioServicesFloorHiringKeepsArrivalIdentityPlacementAndCutaway()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty,Is.False,"Runtime proof must not discard unsaved scene work.");
            if(SceneManager.GetActiveScene().path!="Assets/Scenes/Studio.unity")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            // Exercise the existing developer New Studio entry point in memory only.
            // The saved Studio also supports the older populated production validation setup.
            var bootstrap=new SerializedObject(Object.FindAnyObjectByType<SilverScreen.Presentation.Bootstrap.StudioBootstrap>());
            bootstrap.FindProperty("_startNewStudio").boolValue=true;
            bootstrap.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled").boolValue=false;
            bootstrap.ApplyModifiedPropertiesWithoutUndo();SessionState.SetBool("CarryV2.NewStudioProof",true);
            yield return new EnterPlayMode();yield return null;yield return null;
            yield return RuntimeProof();
        }
        static IEnumerator RuntimeProof()
        {
            var facility=Object.FindAnyObjectByType<StudioServicesFacility>();Assert.That(facility,Is.Not.Null);
            var employees=Object.FindAnyObjectByType<StudioEmployeeManager>();Assert.That(employees.AllEmployees.Count,Is.Zero);
            var recruitment=Object.FindAnyObjectByType<RecruitmentDriver>();Assert.That(recruitment.Coordinator,Is.Not.Null);
            Assert.That(recruitment.Coordinator.Facilities.Facilities.Count,Is.EqualTo(1));
            var time=Object.FindAnyObjectByType<SimulationTimeDriver>();Assert.That(time,Is.Not.Null,"Studio time driver");
            Assert.That(time.Clock.CurrentTime.Year,Is.EqualTo(1930));Assert.That(time.Clock.CurrentTime.Month,Is.EqualTo(1));Assert.That(time.Clock.CurrentTime.Day,Is.EqualTo(1));
            Assert.That(time.Clock.CurrentSpeed,Is.EqualTo(SimulationSpeed.Normal));Assert.That(Time.timeScale,Is.EqualTo(1f));
            var finance=Object.FindAnyObjectByType<SilverScreen.Presentation.Finance.StudioEconomyDriver>();Assert.That(finance.FinanceService.CurrentCash,Is.EqualTo(SilverScreen.Domain.Finance.StudioFinances.DefaultStartingCash));
            var hud=Object.FindAnyObjectByType<SilverScreen.Presentation.UI.StudioHud>();Assert.That(hud.ActivePanel,Is.EqualTo(SilverScreen.Presentation.UI.ManagementPanel.None));
            var managementCamera=Object.FindAnyObjectByType<SilverScreen.Presentation.Camera.StudioCameraController>();Assert.That(managementCamera,Is.Not.Null);Assert.That(managementCamera.enabled,Is.True);Assert.That(managementCamera.ManagementDistance,Is.EqualTo(34f).Within(.01f));
            var production=Object.FindAnyObjectByType<SilverScreen.Presentation.Movie.MovieProductionDriver>();Assert.That(production==null||production.Coordinator==null||production.Coordinator.Slate.AllProjects.Count==0,Is.True);
            var initialBuildings=Object.FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude);
            Assert.That(initialBuildings.Any(v=>v.BuildingType==SilverScreen.Domain.BuildingType.SoundStage||v.BuildingType==SilverScreen.Domain.BuildingType.CastingOffice||v.BuildingType==SilverScreen.Domain.BuildingType.StudioOffice),Is.False);
            string openingReview=Path.GetFullPath("Temp/NewStudioBootstrap");Directory.CreateDirectory(openingReview);ScreenCapture.CaptureScreenshot(Path.Combine(openingReview,"01_initial_world.png"));yield return new WaitForEndOfFrame();
            time.Clock.SetSpeed(SimulationSpeed.VeryFast);
            float candidateDeadline=Time.realtimeSinceStartup+10f;while(recruitment.Coordinator.Candidates.Count==0&&Time.realtimeSinceStartup<candidateDeadline)yield return null;
            var candidate=recruitment.Coordinator.Candidates.FirstOrDefault();Assert.That(candidate,Is.Not.Null,"The opening wave should create its first applicant promptly.");Assert.That(candidate.FacilityId,Is.EqualTo(facility.FacilityId));
            float deadline=Time.realtimeSinceStartup+30;
            while(candidate.Status==CandidateStatus.Arriving&&Time.realtimeSinceStartup<deadline)yield return null;
            var arrivalAgent=recruitment.WorldRouter.GetAgent(candidate);var arrivalNav=arrivalAgent!=null?arrivalAgent.GetComponent<NavMeshAgent>():null;
            Assert.That(candidate.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment),"Applicant must physically arrive through the real lot navigation. "+
                "position="+(arrivalAgent!=null?arrivalAgent.transform.position.ToString("F2"):"missing")+" destination="+(arrivalNav!=null&&arrivalNav.isOnNavMesh?arrivalNav.destination.ToString("F2"):"n/a")+
                " path="+(arrivalNav!=null&&arrivalNav.isOnNavMesh?arrivalNav.pathStatus.ToString():"n/a")+" pending="+(arrivalNav!=null&&arrivalNav.isOnNavMesh?arrivalNav.pathPending.ToString():"n/a")+
                " remaining="+(arrivalNav!=null&&arrivalNav.isOnNavMesh?arrivalNav.remainingDistance.ToString("F2"):"n/a")+" velocity="+(arrivalNav!=null?arrivalNav.velocity.ToString("F2"):"n/a"));
            Assert.That(recruitment,Is.Not.Null,"Recruitment survives arrival");Assert.That(recruitment.WorldRouter,Is.Not.Null,"Candidate world router");
            Debug.Log("Services proof: arrival complete; resolving applicant view. Candidate="+(candidate?.Person?.Name??"NULL")+" router="+recruitment.WorldRouter.name);
            var agent=recruitment.WorldRouter.GetAgent(candidate);
            Assert.That(agent,Is.Not.Null,"Waiting applicant view");
            var person=candidate.Person;
            Debug.Log("Services proof: applicant resolved; acquiring pause.");
            using var pause=time.Clock.AcquirePause("studio-services-proof","Stable input review");
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
                ContextualDropTarget HiringRoom(ProfessionalRole profession)
                {
                    var room=facility.GetComponentsInChildren<ContextualDropTarget>().Single(t=>t.Action is WorkforceRoomAction action&&
                        action.FacilityId==facility.FacilityId&&action.Profession==profession&&!action.Dismissal);
                    Assert.That(room.isActiveAndEnabled,Is.True);Assert.That(room.Shape,Is.EqualTo(FloorTargetShape.Room));
                    Assert.That(room.InteriorVisibility,Is.SameAs(cut));
                    return room;
                }
                Assert.That(facility.GetComponentsInChildren<PersonInteractionSpot>().Where(s=>s.Activity==PersonSpotActivity.Hire)
                    .All(s=>!s.enabled&&!s.GetComponent<ContextualDropTarget>().enabled),Is.True,"Legacy hiring overlays remain disabled.");
                Physics.SyncTransforms();var pickup=agent.transform.position;
                Pointer(pickup,true);yield return new WaitForSecondsRealtime(.24f);pickup=agent.transform.position;Pointer(pickup,true);Assert.That(controller.IsHolding,Is.True);
                var chosen=candidate.JobSought==ProfessionalRole.ConstructionWorker?ProfessionalRole.Groundskeeper:ProfessionalRole.ConstructionWorker;
                var target=HiringRoom(chosen);Pointer(target.Placement.position,false);Assert.That(controller.IsHolding,Is.True,"Original release retains carry");Assert.That(cut.IsRevealed,Is.True);
                Assert.That(controller.VisibleTargets.Count,Is.EqualTo(2));
                Assert.That(controller.MagneticTarget,Is.SameAs(target));
                Assert.That(target.Action.CanExecute(new PersonDropContext(candidate,null,recruitment.Coordinator,null)),Is.True);
                Assert.That(controller.MagneticTarget.IsCandidate,Is.True);
                Assert.That(controller.VisibleTargets.All(t=>t.IsPresented),Is.True);
                Assert.That(candidate.JobSought,Is.Not.EqualTo(chosen),"Actual carry hiring crosses the applicant's sought profession");
                string review=Path.Combine(Path.GetTempPath(),"SilverScreenCarryV2");Directory.CreateDirectory(review);
                StudioServicesProductionReview.Capture(camera,review+"/floor_zones_active.png",camera.transform.position,facility.transform.position+new Vector3(-3,0,-2),40,true,10);
                // Escape restores both pickup position and exterior before a second complete carry.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputSystem.Update();controller.HandleInput();
                Assert.That(controller.IsHolding,Is.False);Assert.That(cut.IsRevealed,Is.False);Assert.That(Vector3.Distance(agent.transform.position,pickup),Is.LessThan(.01f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                Pointer(pickup,true);yield return new WaitForSecondsRealtime(.24f);Pointer(pickup,true);Pointer(target.Placement.position,false);
                Pointer(target.Placement.position,true);Assert.That(controller.IsHolding,Is.False,"Separate valid click places in the semantic hiring room");
                Assert.That(cut.Reasons.HasFlag(BuildingRevealReason.PostInteractionHold),Is.True);
                Assert.That(employees.AllEmployees.Count,Is.EqualTo(1));Assert.That(employees.AllEmployees[0].Person,Is.SameAs(person));Assert.That(employees.AllEmployees[0].Role,Is.EqualTo((EmployeeRole)(int)chosen));
                var employee=Object.FindObjectsByType<EmployeeAgent>(FindObjectsSortMode.None).Single(a=>a.Employee?.Id==person.Id);var nav=employee.GetComponent<NavMeshAgent>();
                var placed=target.transform.InverseTransformPoint(employee.transform.position-Vector3.up*nav.baseOffset);
                Assert.That(target.RoomParts.Any(part=>part.Contains(new Vector2(placed.x,placed.z))),Is.True,"Employee is physically placed within the selected hiring room.");
                pause.Dispose();yield return null;
                deadline=Time.realtimeSinceStartup+20;while(!recruitment.Coordinator.Candidates.Any(c=>c.Status==CandidateStatus.WaitingForRecruitment)&&Time.realtimeSinceStartup<deadline)yield return null;
                var second=recruitment.Coordinator.Candidates.FirstOrDefault(c=>c.Status==CandidateStatus.WaitingForRecruitment);Assert.That(second,Is.Not.Null,"The next opening-wave applicant should arrive through normal recruitment.");
                deadline=Time.realtimeSinceStartup+20;while(second.Status==CandidateStatus.Arriving&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(second.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment));
                using var secondPause=time.Clock.AcquirePause("services-second-hire","Validate the second authored hiring spot");
                var secondAgent=recruitment.WorldRouter.GetAgent(second);Assert.That(secondAgent,Is.Not.Null);
                Pointer(secondAgent.transform.position,false);Pointer(secondAgent.transform.position,true);yield return new WaitForSecondsRealtime(.24f);Pointer(secondAgent.transform.position,true);
                var other=chosen==ProfessionalRole.ConstructionWorker?ProfessionalRole.Groundskeeper:ProfessionalRole.ConstructionWorker;var otherRoom=HiringRoom(other);
                Pointer(otherRoom.Placement.position,false);Assert.That(cut.IsRevealed,Is.True);Assert.That(controller.MagneticTarget,Is.SameAs(otherRoom));
                Assert.That(otherRoom.Action.CanExecute(new PersonDropContext(second,null,recruitment.Coordinator,null)),Is.True);
                Pointer(otherRoom.Placement.position,true);
                Assert.That(controller.IsHolding,Is.False);Assert.That(cut.Reasons.HasFlag(BuildingRevealReason.PostInteractionHold),Is.True);Assert.That(employees.AllEmployees.Any(e=>e.Person==second.Person&&e.Role==(EmployeeRole)(int)other),Is.True);
                secondPause.Dispose();
                deadline=Time.realtimeSinceStartup+25;while((recruitment.Coordinator.OpeningArrivalsRemaining>0||recruitment.Coordinator.Candidates.Any(c=>c.Status==CandidateStatus.Arriving))&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(recruitment.Coordinator.OpeningArrivalsRemaining,Is.Zero,"The opening wave runs once to completion.");
                Assert.That(recruitment.Coordinator.Candidates.Count,Is.EqualTo(4));
                Assert.That(recruitment.Coordinator.Candidates.All(c=>c.Status==CandidateStatus.WaitingForRecruitment),Is.True);
                var sought=recruitment.Coordinator.Candidates.Select(c=>c.JobSought).Concat(new[]{candidate.JobSought,second.JobSought}).ToArray();
                Assert.That(sought.Count(role=>role==ProfessionalRole.ConstructionWorker),Is.EqualTo(4));
                Assert.That(sought.Count(role=>role==ProfessionalRole.Groundskeeper),Is.EqualTo(2));
                Pointer(facility.transform.position+new Vector3(0,0,-20),false);yield return new WaitForSecondsRealtime(2.5f);
                Assert.That(cut.IsRevealed,Is.False,"The post-interaction reveal expires after the pointer leaves the building.");
                camera.transform.SetPositionAndRotation(facility.transform.position+new Vector3(18,22,-24),Quaternion.Euler(42,-35,0));camera.orthographic=true;camera.orthographicSize=23;
                ScreenCapture.CaptureScreenshot(Path.Combine(openingReview,"02_applicants_waiting.png"));yield return new WaitForEndOfFrame();
                StudioServicesProductionReview.Capture(camera,review+"/restored_exterior.png",facility.transform.position+new Vector3(19,14,-23),facility.transform.position+new Vector3(1,1,0),42);
                File.WriteAllText(review+"/runtime.txt","Unity "+Application.unityVersion+" Play Mode: empty studio; physical applicant arrival; original mouse release retains carry; generic cutaway; Escape restores actual pickup/exterior; separate-click hiring through BOTH semantic profession rooms; same Person identities; cross-profession floor hiring; magnetic room targeting; disabled legacy overlays; post-interaction cutaway hold and restoration. Passed.\n");
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
}
