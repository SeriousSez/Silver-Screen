using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Editor.Characters;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using UnityEditor;
using UnityEditor.SceneManagement;
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
    [TestFixture]
    public sealed class BoyBlinkPersonPrototypeTests
    {
        private const string _prefabPath = BoyBlinkPrototypeTools.Prefab;
        private const string SceneKey = "SilverScreen.BoyBlinkPerson.PreviousScene";
        private NavMeshData _data;
        private NavMeshDataInstance _navigation;
        private List<GameObject> _objects;
        private bool _previousRunInBackground;

        [UnitySetUp] public IEnumerator Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath) == null)
                Assert.Fail("Requires the generated Boy BlinkBoth prefab.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1), "Close additive review scenes first.");
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _objects = new List<GameObject>();
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(16, .2f, 16),
                transform = Matrix4x4.TRS(Vector3.down * .1f, Quaternion.identity, Vector3.one) };
            _data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(20, 4, 20)), Vector3.zero, Quaternion.identity);
            _navigation = NavMesh.AddNavMeshData(_data);
        }

        [UnityTearDown] public IEnumerator Teardown()
        {
            if (_objects != null) for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            if (_navigation.valid) _navigation.Remove();
            if (_data != null) Object.DestroyImmediate(_data);
            if (Application.isPlaying) Application.runInBackground = _previousRunInBackground;
            if (Application.isPlaying) yield return new ExitPlayMode();
            var path = SessionState.GetString(SceneKey, ""); SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }

        private GameObject Keep(GameObject go) { _objects.Add(go); return go; }
        private GameObject Spawn(Vector3 ground, string name) => Keep(HumanBasePrototypeTools.Spawn(ground, name, _prefabPath));

        [UnityTest] public IEnumerator SuppliedHumanoidWalksArrivesRunsAndDeformsFace()
        {
            var go = Spawn(Vector3.left * 3, "Locomotion proof");
            var agent = go.GetComponent<EmployeeAgent>(); var nav = go.GetComponent<NavMeshAgent>();
            var visual = go.GetComponent<HumanBasePrototypeVisual>();
            yield return null; yield return null;
            Assert.That(NavMesh.SamplePosition(Vector3.left * 3, out var start, .3f, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(Vector3.right * 3, out var destination, .3f, NavMesh.AllAreas), Is.True);
            Assert.That(nav.Warp(start.position), Is.True, "Register at the freshly baked surface before requesting a path");
            yield return null;
            Assert.That(visual.Animator.avatar.isValid && visual.Animator.avatar.isHuman, Is.True);
            Assert.That(visual.Animator.applyRootMotion, Is.False);
            Assert.That(visual.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Assert.That(visual.Face.sharedMesh.blendShapeCount, Is.EqualTo(30));
            Assert.That(visual.Animator.GetBoneTransform(HumanBodyBones.UpperChest), Is.Not.Null);
            Assert.That(visual.Animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal), Is.Null);
            Assert.That(visual.SetShape("BlinkBoth", 100), Is.True);
            bool arrived = false;
            Assert.That(agent.TryAssignTaskDestination(destination.position, new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Character prototype"),
                () => { arrived = true; agent.ClearTaskDestination(); }), Is.True,
                $"Route accepted: onMesh={nav.isOnNavMesh}, enabled={nav.enabled}, position={go.transform.position}, native={nav.nextPosition}, target={destination.position}");
            // Editor focus/import stalls do not advance NavMesh by the same amount as wall time.
            bool walked = false; float deadline = Time.realtimeSinceStartup + 30;
            var trace = new List<string>(); float nextSample = 0;
            Quaternion legBefore = visual.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation;
            float greatestLegChange = 0;
            while (!arrived && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(visual.Face.GetBlendShapeWeight(29), Is.EqualTo(100));
                walked |= visual.Animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") && nav.velocity.magnitude > .1f;
                greatestLegChange = Mathf.Max(greatestLegChange, Quaternion.Angle(legBefore, visual.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation));
                if (Time.realtimeSinceStartup >= nextSample)
                {
                    float parameter = visual.Animator.GetFloat("Speed");
                    trace.Add($"position={go.transform.position} velocity={nav.velocity} speed={nav.speed} remaining={nav.remainingDistance} pending={nav.pathPending} state={visual.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash} parameter={parameter} animatorSpeed={visual.Animator.speed}");
                    nextSample = Time.realtimeSinceStartup + .25f;
                }
                yield return null;
            }
            File.WriteAllLines(BoyBlinkPrototypeTools.Review + "/person-locomotion-trace.txt", trace);
            Assert.That(arrived, Is.True, "EmployeeAgent arrival callback");
            Assert.That(walked, Is.True, "Animator Walk state while navigation moves");
            Assert.That(greatestLegChange, Is.GreaterThan(10), "Leg animation must deform the rig");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(agent.Employee.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Assert.That(visual.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Assert.That(Vector3.Distance(go.transform.position - Vector3.up * nav.baseOffset, Vector3.right * 3), Is.LessThan(.4f));
            nav.speed = 3;
            Assert.That(agent.TryAssignTaskDestination(Vector3.left * 3, new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Run proof"), agent.ClearTaskDestination), Is.True);
            bool ran = false; deadline = Time.realtimeSinceStartup + 4;
            while (!ran && Time.realtimeSinceStartup < deadline)
            { ran = visual.Animator.GetCurrentAnimatorStateInfo(0).IsName("Run"); yield return null; }
            Assert.That(ran, Is.True);
            agent.CancelPresentationMovement();
            Assert.That(visual.Face.GetBlendShapeWeight(29), Is.EqualTo(100));
            visual.ResetShapes();
            File.WriteAllText(BoyBlinkPrototypeTools.Review + "/person-navigation.txt", "EmployeeAgent walked, arrived, returned to Idle and ran; BlinkBoth remained 100 during body animation.");
        }

        [UnityTest] public IEnumerator RealVisualUsesExistingClickHoldDropCancelAndContextActions()
        {
            var inputMode = InputSystem.settings.updateMode;
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            bool runInBackground = Application.runInBackground;
            var muted = InputSystem.devices.Where(d => d.enabled && (d is Mouse || d is Keyboard)).ToArray();
            foreach (var device in muted) InputSystem.DisableDevice(device);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
                floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(20, .2f, 20);
                var camera = Keep(new GameObject("Input camera")).AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 10;
                camera.transform.SetPositionAndRotation(new Vector3(0, 20, 0), Quaternion.Euler(90, 0, 0));
                var time = Keep(new GameObject("Input time")).AddComponent<SimulationTimeDriver>(); time.enabled = false;
                var go = Spawn(Vector3.zero, "Carry prototype");
                var agent = go.GetComponent<EmployeeAgent>(); agent.enabled = false;
                var visual = go.GetComponent<HumanBasePrototypeVisual>(); var identity = agent.Employee.Person;
                Assert.That(visual.SetShape("BlinkBoth", 100), Is.True);
                var selection = Keep(new GameObject("Selection")).AddComponent<StudioSelectionController>(); selection.enabled = false;
                var controller = selection.PersonInteraction;
                yield return null; yield return null;
                var spot = Keep(new GameObject("Practice")).AddComponent<PersonInteractionSpot>();
                spot.transform.position = new Vector3(3, 0, 0);
                spot.Configure("prototype:comedy", null, PersonSpotActivity.Practice, ProfessionalRole.Actor, "Practice Comedy");
                Physics.SyncTransforms();
                void Pointer(Vector3 point, bool down)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(point), buttons = (ushort)(down ? 1 : 0) });
                    InputSystem.Update(); controller.HandleInput();
                }
                Pointer(go.transform.position, false); // Real physics hover path on the prefab collider.
                Pointer(go.transform.position, true); Pointer(go.transform.position, false);
                Assert.That(controller.IsHolding, Is.False); Assert.That(selection.SelectedAgent, Is.SameAs(agent));
                Pointer(go.transform.position, true); yield return new WaitForSecondsRealtime(.22f); Pointer(go.transform.position, true);
                Assert.That(controller.IsHolding && visual.IsPosingHeld, Is.True);
                Pointer(new Vector3(8.5f, 0, 0), false);
                Assert.That(controller.IsHolding, Is.True, "Initial release retains carry.");
                Pointer(new Vector3(8.5f, 0, 0), true);
                Assert.That(controller.IsHolding && visual.IsPosingHeld, Is.True, "Invalid drop retains visual.");
                Pointer(Vector3.left * 3, false); Pointer(Vector3.left * 3, true);
                Assert.That(controller.IsHolding, Is.False); Assert.That(agent.IsHeld, Is.False);
                yield return new WaitForSecondsRealtime(.35f);
                Assert.That(visual.IsPosingHeld, Is.False);
                Pointer(go.transform.position, false); Pointer(go.transform.position, true);
                yield return new WaitForSecondsRealtime(.22f); Pointer(go.transform.position, true);
                Assert.That(controller.IsHolding,Is.True,"Second hold must start before the contextual drop.");
                Pointer(spot.transform.position, false); Pointer(spot.transform.position, true);
                Assert.That(controller.IsHolding, Is.False);
                Assert.That(agent.Employee.CurrentState, Is.EqualTo(EmployeeState.Practicing));
                Assert.That(agent.Employee.CurrentIntent.TargetBuildingId, Is.EqualTo(spot.SemanticId));
                Pointer(go.transform.position, false); var original = go.transform.position;
                Pointer(go.transform.position, true); yield return new WaitForSecondsRealtime(.22f); Pointer(go.transform.position, true);
                Pointer(Vector3.back * 3, false);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); controller.HandleInput();
                Assert.That(controller.IsHolding || visual.IsPosingHeld, Is.False);
                Assert.That(Vector3.Distance(original, go.transform.position), Is.LessThan(.01f));
                Assert.That(agent.Employee.Person, Is.SameAs(identity));
                Assert.That(visual.Face.GetBlendShapeWeight(29), Is.EqualTo(100));
                File.WriteAllText(BoyBlinkPrototypeTools.Review + "/person-selection-carry.txt", "Real collider hover/click selected the same EmployeeAgent identity; held pose, invalid drop, valid drop, context Practice and Escape cancel all passed with BlinkBoth 100.");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
                foreach (var device in muted) if (device.added) InputSystem.EnableDevice(device);
                InputSystem.settings.updateMode = inputMode; InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; Application.runInBackground = runInBackground;
            }
        }

        [UnityTest] public IEnumerator ChildHeightAndManagementCameraRemainCompatibleWhileCarried()
        {
            var go=Spawn(Vector3.zero,"Boy height/camera proof");var agent=go.GetComponent<EmployeeAgent>();
            var nav=go.GetComponent<NavMeshAgent>();var visual=go.GetComponent<HumanBasePrototypeVisual>();
            yield return null;yield return null;agent.enabled=false;visual.Animator.enabled=false;
            var capsule=go.GetComponent<CapsuleCollider>();Physics.SyncTransforms();
            var nativePoints=visual.Face.sharedMesh.vertices.Select(BoyBlinkPrototypeTools.ModelMatrix.MultiplyPoint3x4).ToArray();
            float bodyHeight=nativePoints.Max(v=>v.y)-nativePoints.Min(v=>v.y);
            Assert.That(bodyHeight,Is.EqualTo(1.466252f).Within(.000002f),"Boy stays at his own stature.");
            Assert.That(nav.height,Is.EqualTo(EmployeeNavigationProfile.Height),"Existing Awake policy is explicitly retained.");
            Assert.That(capsule.height,Is.EqualTo(nav.height));
            Assert.That(NavMesh.SamplePosition(Vector3.zero,out var ground,.3f,NavMesh.AllAreas),Is.True);
            Assert.That(nav.baseOffset,Is.EqualTo(nav.height/2));Assert.That(capsule.bounds.min.y,Is.EqualTo(ground.position.y).Within(.002f));
            Assert.That(go.transform.Find("CharacterVisualRoot").position.y,Is.EqualTo(ground.position.y).Within(.002f),"The child visual must not float above the actual navigation surface when Awake applies the navigation profile.");
            Assert.That(go.transform.Find("SelectionIndicator").position.y,Is.EqualTo(ground.position.y+.02f).Within(.002f));
            var serialized=new SerializedObject(visual);var pivot=serialized.FindProperty("_suspensionPivot").vector3Value;
            Assert.That(pivot.y,Is.InRange(1.15f,1.3f));
            var identity=agent.Employee.Person;var original=go.transform.position;
            visual.SetShape("BlinkBoth",100);var drag=PersonDragSession.Begin(agent,null);Assert.That(drag,Is.Not.Null);
            drag.Follow(Vector3.zero);yield return new WaitForSecondsRealtime(.2f);
            Assert.That(visual.IsPosingHeld);Assert.That(go.transform.position.y-nav.baseOffset,Is.EqualTo(1.2f).Within(.001f));
            Assert.That(capsule.enabled,Is.False);
            var rig=Keep(new GameObject("Management camera rig"));var camera=Keep(new GameObject("Management camera")).AddComponent<Camera>();
            camera.transform.SetParent(rig.transform,false);camera.tag="MainCamera";camera.nearClipPlane=.05f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.17f,.21f);
            var controller=rig.AddComponent<SilverScreen.Presentation.Camera.StudioCameraController>();controller.enabled=false;
            var settings=new SerializedObject(controller);settings.FindProperty("_enableEdgePan").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();
            controller.FrameManagementView(Vector3.zero,15);
            var light=Keep(new GameObject("Presentation key")).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(25,-30,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
            var mode=InputSystem.settings.updateMode;
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            var devices=InputSystem.devices.Where(d=>d.enabled&&(d is Mouse||d is Keyboard)).ToArray();
            foreach(var d in devices)InputSystem.DisableDevice(d);
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var target=controller.ManagementTarget;var distance=controller.ManagementDistance;var rotation=camera.transform.rotation;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.E));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width/2f,Screen.height/2f),scroll=new Vector2(0,120)});InputSystem.Update();
                controller.SendMessage("Update");yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width/2f,Screen.height/2f)});InputSystem.Update();
                for(int i=0;i<8;i++){controller.SendMessage("Update");yield return null;}
                Assert.That(Vector3.Distance(controller.ManagementTarget,target),Is.GreaterThan(.01f));
                Assert.That(controller.ManagementDistance,Is.LessThan(distance));Assert.That(Quaternion.Angle(camera.transform.rotation,rotation),Is.GreaterThan(.1f));
                Assert.That(drag.IsHeld&&visual.IsPosingHeld);Assert.That(visual.Face.GetBlendShapeWeight(29),Is.EqualTo(100));
                // Editor stalls can pan well beyond the subject during the input proof.
                // Release input and explicitly frame the carried person for visual evidence.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                controller.FrameManagementView(go.transform.position,8);
                yield return null;yield return null;
                var screenPoint=camera.WorldToViewportPoint(visual.Face.bounds.center);
                Assert.That(screenPoint.z,Is.GreaterThan(camera.nearClipPlane));
                Assert.That(screenPoint.x,Is.InRange(0f,1f));Assert.That(screenPoint.y,Is.InRange(0f,1f));
                Directory.CreateDirectory(BoyBlinkPrototypeTools.Review+"/person");
                HumanBasePrototypeTools.Capture(camera,"../BoyFoundation/UnityBlink/person/held-camera",1200,800);
                File.WriteAllText(BoyBlinkPrototypeTools.Review+"/person-height-camera.txt","bodyHeight="+bodyHeight+" navigationHeight="+nav.height+" selection height excess="+(capsule.height-bodyHeight)+" radius="+nav.radius+" baseOffset="+nav.baseOffset+" suspensionPivot="+pivot+" navigationSurfaceY="+ground.position.y+" camera keyboard pan/rotate and wheel zoom passed while held; carry lift=1.2 m; visual feet and marker align with navigation ground; root and identity intact.");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);foreach(var d in devices)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.updateMode=mode;
                InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
                drag.Cancel();
            }
            Assert.That(agent.Employee.Person,Is.SameAs(identity));Assert.That(Vector3.Distance(go.transform.position,original),Is.LessThan(.01f));
            Assert.That(capsule.enabled);Assert.That(visual.IsPosingHeld,Is.False);Assert.That(visual.Face.GetBlendShapeWeight(29),Is.EqualTo(100));
        }

        [UnityTest] public IEnumerator TwoRealVisualsReachDistinctSocializePositions()
        {
            var driver = Keep(new GameObject("Social time")).AddComponent<SimulationTimeDriver>(); driver.enabled = false;
            var first = Spawn(Vector3.left * 4, "Social one").GetComponent<EmployeeAgent>();
            var second = Spawn(Vector3.right * 4, "Social two").GetComponent<EmployeeAgent>();
            foreach (var agent in new[] { first, second })
            {
                Assert.That(agent.GetComponent<HumanBasePrototypeVisual>().SetShape("BlinkBoth", 100), Is.True);
                var wellbeing = agent.Employee.Person.CaptureWellbeing(); wellbeing.Boredom = 60; wellbeing.Mood = 0;
                agent.Employee.Person.RestoreWellbeing(wellbeing);
            }
            yield return null;
            foreach (var agent in new[] { first, second })
            {
                agent.BindAutonomy(driver.Autonomy); agent.BindTimeService(driver.TimeService);
                driver.Wellbeing.Register(agent.Employee.Person); driver.Autonomy.Register(agent.Employee);
                driver.Autonomy.UpdatePosition(agent.Employee, new AutonomyPosition(agent.transform.position.x, 0, agent.transform.position.z));
            }
            Assert.That(driver.Autonomy.EvaluateNow(first.Employee).Activity, Is.EqualTo(PersonAutonomousActivity.Socialize));
            var session = driver.Autonomy.Sessions.FindForPerson(first.Employee.Id);
            Assert.That(session, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 15;
            while (session.State != PersonActivitySessionState.Active && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.State, Is.EqualTo(PersonActivitySessionState.Active));
            Assert.That(first.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
            Assert.That(second.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
            Assert.That(first.GetComponent<HumanBasePrototypeVisual>().Face.GetBlendShapeWeight(29), Is.EqualTo(100));
            Assert.That(second.GetComponent<HumanBasePrototypeVisual>().Face.GetBlendShapeWeight(29), Is.EqualTo(100));
            float separation = Vector3.Distance(first.transform.position, second.transform.position);
            Assert.That(separation, Is.GreaterThan(1));
            File.WriteAllText(BoyBlinkPrototypeTools.Review + "/person-social-proof.txt", "Existing Socialize active; actual root separation metres=" + separation);
        }
    }

}
