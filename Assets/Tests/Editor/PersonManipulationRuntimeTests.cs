using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonManipulationRuntimeTests
    {
        private const string SceneKey = "SilverScreen.PersonProof.PreviousScene";
        [UnityTearDown] public IEnumerator RestoreScene()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            string path = SessionState.GetString(SceneKey, "");
            SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }
        [UnityTest] public IEnumerator GroundDropWarpsImmediatelyRejectsWallsAndRetainsWorkObligation()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False, "Save the scene before running this isolated runtime proof.");
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            // Isolated navigation at a remote location; no user scene or existing navigation is replaced.
            var center = new Vector3(1000, 0, 1000);
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(20, .2f, 20), transform = Matrix4x4.TRS(center - Vector3.up * .1f, Quaternion.identity, Vector3.one), area = 0 };
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source }, new Bounds(center, new Vector3(22, 4, 22)), Vector3.zero, Quaternion.identity);
            var instance = NavMesh.AddNavMeshData(data);
            var go = new GameObject("Drop employee"); go.transform.position = center;
            GameObject wall = null;
            try
            {
                var nav = go.AddComponent<NavMeshAgent>(); nav.baseOffset = 1;
                Assert.That(NavMesh.SamplePosition(center, out var start, .3f, NavMesh.AllAreas), Is.True);
                // Let the native navigation world register a freshly created agent after entering Play Mode.
                nav.enabled = false; go.transform.position = start.position + Vector3.up * nav.baseOffset;
                nav.enabled = true;
                yield return null;
                Assert.That(Application.isPlaying, Is.True);
                Assert.That(nav.Warp(start.position), Is.True);
                var agent = go.AddComponent<EmployeeAgent>(); agent.BindDomain(Actor());
                var task = new EmployeeIntent(EmployeeIntentPurpose.ReportToStage, "Needed at stage", "stage-1");
                Assert.That(agent.TryAssignTaskDestination(center + Vector3.forward * 4, task), Is.True);
                var drag = PersonDragSession.Begin(agent, null); Assert.That(drag, Is.Not.Null);
                var presentation = go.GetComponent<HeldPersonPresentation>();
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Held));
                var identity = agent.Employee.Person;
                var requested = start.position + Vector3.right * 3;
                drag.Follow(requested);
                Assert.That(drag.TryDrop(requested, null), Is.True);
                Assert.That(Vector3.Distance(go.transform.position, requested + Vector3.up * nav.baseOffset), Is.LessThan(.16f));
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.GroundSettle));
                var placed = go.transform.position;
                presentation.AdvancePresentation(.1f);
                Assert.That(go.transform.position, Is.EqualTo(placed), "Visual settling must not change the chosen root position.");
                Assert.That(nav.hasPath, Is.False); Assert.That(agent.Employee.CurrentIntent, Is.SameAs(task));
                Assert.That(agent.IsReturningAfterDrop, Is.True); Assert.That(agent.IsHeld, Is.False);
                yield return null;
                Assert.That(agent.IsReturningAfterDrop || nav.hasPath, Is.True, "Autonomy must resume the original obligation.");
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = requested + Vector3.up; wall.transform.localScale = new Vector3(2, 2, 2); Physics.SyncTransforms();
                Assert.That(PersonDragSession.TryValidateGround(requested, nav, go.transform, null, out _), Is.False);
                Assert.That(PersonDragSession.TryValidateGround(center + Vector3.right * 100, nav, go.transform, null, out _), Is.False);
                UnityEngine.Object.DestroyImmediate(wall); wall = null;
                var pickupPosition = go.transform.position;
                var canceled = PersonDragSession.Begin(agent, null);
                canceled.Follow(center + Vector3.right * 100);
                Assert.That(canceled.TryDrop(center + Vector3.right * 100, null), Is.False);
                canceled.Cancel();
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Normal));
                Assert.That(Vector3.Distance(go.transform.position, pickupPosition), Is.LessThan(.01f));
                var contextual = PersonDragSession.Begin(agent, null);
                var heldPosition=go.transform.position;
                Assert.That(contextual.TryContextualDrop(start.position,null,Quaternion.identity,()=>false),Is.False);
                Assert.That(contextual.IsHeld,Is.True,"Unavailable action retains carry");
                Assert.That(go.transform.position,Is.EqualTo(heldPosition));
                Assert.That(contextual.TryDrop(start.position, null, Quaternion.Euler(0, 90, 0), PersonReleasePresentation.Contextual), Is.True);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.InteractionEntry));
                Assert.That(Quaternion.Angle(go.transform.rotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(.01f));
                Assert.That(agent.Employee.Person, Is.SameAs(identity));
                Assert.That(agent.Employee.CurrentIntent, Is.SameAs(task));
                presentation.AdvancePresentation(.1f); presentation.AdvancePresentation(.1f);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Normal));
            }
            finally { if (wall != null) UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(go); instance.Remove(); UnityEngine.Object.DestroyImmediate(data); }
        }

        [UnityTest] public IEnumerator CarryRequiresSeparateClickAndInvalidAttemptsKeepPresentation()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            var inputMode = InputSystem.settings.updateMode;
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            bool runInBackground = Application.runInBackground;
            var muted = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
                if (device.enabled && (device is Mouse || device is Keyboard)) muted.Add(device);
            foreach (var device in muted) InputSystem.DisableDevice(device);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(16, .2f, 16), transform = Matrix4x4.TRS(Vector3.down * .1f, Quaternion.identity, Vector3.one), area = 0 };
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source }, new Bounds(Vector3.zero, new Vector3(20, 4, 20)), Vector3.zero, Quaternion.identity);
            var instance = NavMesh.AddNavMeshData(data);
            var objects = new List<GameObject>();
            GameObject Keep(GameObject go) { objects.Add(go); return go; }
            try
            {
                var ground = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
                ground.transform.position = Vector3.down * .1f; ground.transform.localScale = new Vector3(20, .2f, 20);
                var camera = Keep(new GameObject("Input proof camera")).AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 10;
                camera.transform.SetPositionAndRotation(new Vector3(0, 20, 0), Quaternion.Euler(90, 0, 0));
                var time = Keep(new GameObject("Input proof time")).AddComponent<SilverScreen.Presentation.SimulationTime.SimulationTimeDriver>();
                time.enabled = false;
                var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                var nav = go.AddComponent<NavMeshAgent>(); nav.baseOffset = 1;
                Assert.That(NavMesh.SamplePosition(Vector3.zero, out var start, .3f, NavMesh.AllAreas), Is.True);
                nav.enabled = false; go.transform.position = start.position + Vector3.up; nav.enabled = true;
                yield return null;
                var agent = go.AddComponent<EmployeeAgent>(); agent.BindDomain(Actor()); agent.enabled = false;
                var identity = agent.Employee.Person;
                var selection = Keep(new GameObject("Input proof selection")).AddComponent<SilverScreen.Presentation.Selection.StudioSelectionController>();
                selection.enabled = false; // Drive the real input handler explicitly, once per synthetic event.
                var controller = selection.PersonInteraction;
                yield return null; yield return null;
                var spot = Keep(new GameObject("Input proof practice")).AddComponent<PersonInteractionSpot>();
                spot.transform.position = new Vector3(3, 0, 0);
                spot.Configure("proof:comedy", null, PersonSpotActivity.Practice, ProfessionalRole.Actor, "Practice Comedy");
                Physics.SyncTransforms();
                void HandleWithoutCursorWarp()
                {
                    var before=mouse.position.ReadValue(); controller.HandleInput();
                    Assert.That(mouse.position.ReadValue(),Is.EqualTo(before),"Targeting never changes cursor position");
                }
                void Pointer(Vector3 point, bool down)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(point), buttons = (ushort)(down ? 1 : 0) });
                    InputSystem.Update(); HandleWithoutCursorWarp();
                }
                Pointer(go.transform.position, true);
                Assert.That(controller.IsHolding, Is.False, "Mouse-down alone is pending pickup.");
                Pointer(go.transform.position, false);
                Assert.That(controller.IsHolding, Is.False, "Quick click must not pick up.");
                Assert.That(selection.SelectedAgent, Is.SameAs(agent), "Quick click still selects.");
                Pointer(go.transform.position, true);
                yield return new WaitForSecondsRealtime(.22f);
                Pointer(go.transform.position, true);
                Assert.That(controller.IsHolding, Is.True, "Holding past the configured threshold picks up.");
                var held = go.GetComponent<HeldPersonPresentation>();
                Pointer(new Vector3(0, 0, 2), false);
                Assert.That(controller.IsHolding, Is.True, "Initial mouse-up only ends the press.");
                Assert.That(held.State, Is.EqualTo(HeldPersonPresentationState.Held));
                CollectionAssert.Contains(controller.VisibleSpots, spot);
                var floorTarget=spot.GetComponent<ContextualDropTarget>();
                Assert.That(floorTarget.IsPresented,Is.True); floorTarget.enabled=false;
                Assert.That(floorTarget.IsPresented,Is.False,"Disabled target hides immediately in Play Mode");
                floorTarget.enabled=true;
                var cameraRig=Keep(new GameObject("Carry camera rig")); camera.transform.SetParent(cameraRig.transform);
                var cameraControl=cameraRig.AddComponent<SilverScreen.Presentation.Camera.StudioCameraController>();
                var cameraSettings=new SerializedObject(cameraControl); cameraSettings.FindProperty("_enableEdgePan").boolValue=false; cameraSettings.ApplyModifiedPropertiesWithoutUndo();
                foreach(var speed in new[]{SimulationSpeed.Normal,SimulationSpeed.Fast,SimulationSpeed.VeryFast,SimulationSpeed.Paused})
                {
                    time.enabled=true; time.Clock.SetSpeed(speed);
                    Pointer(new Vector3(0,0,2),false); yield return null;
                    var panStart=cameraRig.transform.position;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));InputSystem.Update();
                    yield return new WaitForSecondsRealtime(.15f);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                    Assert.That(Vector3.Distance(cameraRig.transform.position,panStart),Is.GreaterThan(.1f),"Keyboard pan while carrying at "+speed);
                    var screen=new Vector2(Screen.width/2f,Screen.height/2f);
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen,buttons=2});InputSystem.Update();HandleWithoutCursorWarp();yield return null;
                    Assert.That(controller.BlocksCameraRightDrag,Is.False);
                    var yaw=cameraRig.transform.rotation;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen+Vector2.right*100,buttons=2});InputSystem.Update();HandleWithoutCursorWarp();
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.That(Quaternion.Angle(yaw,cameraRig.transform.rotation),Is.GreaterThan(2),"Mouse rotation while carrying at "+speed);
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});InputSystem.Update();yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen,buttons=4});InputSystem.Update();HandleWithoutCursorWarp();yield return null;
                    panStart=cameraRig.transform.position;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen+Vector2.up*80,buttons=4});InputSystem.Update();HandleWithoutCursorWarp();
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.That(Vector3.Distance(panStart,cameraRig.transform.position),Is.GreaterThan(.1f),"Middle pan while carrying");
                    float distance=Vector3.Distance(camera.transform.position,cameraRig.transform.position);
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen,scroll=new Vector2(0,-120)});InputSystem.Update();HandleWithoutCursorWarp();yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});InputSystem.Update();
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.That(Vector3.Distance(camera.transform.position,cameraRig.transform.position),Is.GreaterThan(distance+.1f),"Zoom while carrying");
                    Assert.That(controller.IsHolding,Is.True);
                }
                time.Clock.SetSpeed(SimulationSpeed.Normal);time.enabled=false;cameraControl.enabled=false;
                camera.transform.SetPositionAndRotation(new Vector3(0,20,0),Quaternion.Euler(90,0,0));
                Pointer(new Vector3(8.5f, 0, 0), false);
                Assert.That(controller.IsHolding, Is.True, "Carry persists with no mouse button pressed.");
                Pointer(new Vector3(8.5f, 0, 0), true);
                Assert.That(controller.IsHolding, Is.True, "Invalid new click must not cancel or drop.");
                Assert.That(held.State, Is.EqualTo(HeldPersonPresentationState.Held));
                CollectionAssert.Contains(controller.VisibleSpots, spot);
                Assert.That(go.transform.position.x, Is.GreaterThan(8), "Invalid attempt does not return to pickup position.");
                Pointer(new Vector3(-3, 0, 0), false);
                Assert.That(controller.IsHolding, Is.True);
                Pointer(new Vector3(-3, 0, 0), true);
                Assert.That(controller.IsHolding, Is.False, "A separate valid click drops immediately.");
                Assert.That(Vector3.Distance(go.transform.position, new Vector3(-3, start.position.y + nav.baseOffset, 0)), Is.LessThan(.16f));
                Assert.That(nav.hasPath, Is.False);
                Assert.That(held.State, Is.EqualTo(HeldPersonPresentationState.GroundSettle));
                Assert.That(controller.VisibleSpots, Is.Empty);
                Pointer(go.transform.position, false);
                Pointer(go.transform.position, true);
                yield return new WaitForSecondsRealtime(.22f);
                Pointer(go.transform.position, true);
                Pointer(spot.transform.position, false);
                Assert.That(controller.IsHolding, Is.True);
                Assert.That(controller.MagneticTarget, Is.SameAs(spot.GetComponent<ContextualDropTarget>()));
                Assert.That(controller.MagneticTarget.Shape, Is.EqualTo(FloorTargetShape.Round));
                Pointer(spot.transform.position, true);
                Assert.That(controller.IsHolding, Is.False);
                Assert.That(held.State, Is.EqualTo(HeldPersonPresentationState.InteractionEntry));
                Assert.That(agent.Employee.CurrentState, Is.EqualTo(EmployeeState.Practicing), "Context click starts the actual practice service.");
                Assert.That(agent.Employee.CurrentIntent.TargetBuildingId, Is.EqualTo(spot.SemanticId));
                Assert.That(Vector3.Distance(go.transform.position, spot.transform.position + Vector3.up * nav.baseOffset), Is.LessThan(.16f));
                Assert.That(agent.Employee.Person, Is.SameAs(identity));
                Pointer(go.transform.position, false);
                var original = go.transform.position;
                Pointer(go.transform.position, true);
                yield return new WaitForSecondsRealtime(.22f);
                Pointer(go.transform.position, true);
                Pointer(new Vector3(-4, 0, 3), false);
                Assert.That(controller.IsHolding, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update(); HandleWithoutCursorWarp();
                Assert.That(controller.IsHolding, Is.False);
                Assert.That(Vector3.Distance(go.transform.position, original), Is.LessThan(.01f));
                Assert.That(held.State, Is.EqualTo(HeldPersonPresentationState.Normal));
                Assert.That(controller.VisibleSpots, Is.Empty);
                Assert.That(agent.IsHeld, Is.False);
                Assert.That(agent.Employee.Person, Is.SameAs(identity));
            }
            finally
            {
                for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
                instance.Remove(); Object.DestroyImmediate(data);
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
                foreach (var device in muted) if (device.added) InputSystem.EnableDevice(device);
                InputSystem.settings.updateMode = inputMode;
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
                Application.runInBackground = runInBackground;
            }
        }

        private static Employee Actor() => new Employee(new PersonProfile("actor", "Test Actor", new SimulationDateTime(1900, 1, 1, 0, 0), ProfessionalRole.Actor, new TalentProfile(0, 0)), EmployeeRole.Actor, 1000);
    }
}
