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
    public sealed class MaleBlinkPersonPrototypeTests
    {
        private const string _prefabPath = MaleBlinkPrototypeTools.Prefab;
        private const string SceneKey = "SilverScreen.MaleBlinkPerson.PreviousScene";
        private NavMeshData _data;
        private NavMeshDataInstance _navigation;
        private List<GameObject> _objects;
        private bool _previousRunInBackground;

        [UnitySetUp] public IEnumerator Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath) == null)
                Assert.Ignore("Requires local licensed imports: build with the Human Base Prototype menu.");
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
                File.WriteAllText(MaleBlinkPrototypeTools.Review + "/person-selection-carry.txt", "Real collider hover/click selected the same EmployeeAgent identity; held pose, invalid drop, valid drop, context Practice and Escape cancel all passed with BlinkBoth 100.");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
                foreach (var device in muted) if (device.added) InputSystem.EnableDevice(device);
                InputSystem.settings.updateMode = inputMode; InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; Application.runInBackground = runInBackground;
            }
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
            Assert.That(first.GetComponent<HumanBasePrototypeVisual>().Animator.avatar.isHuman, Is.True);
            Assert.That(second.GetComponent<HumanBasePrototypeVisual>().Animator.avatar.isValid, Is.True);
            float deadline = Time.realtimeSinceStartup + 15;
            while (session.State != PersonActivitySessionState.Active && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.State, Is.EqualTo(PersonActivitySessionState.Active));
            Assert.That(first.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
            Assert.That(second.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
            Assert.That(first.GetComponent<HumanBasePrototypeVisual>().Face.GetBlendShapeWeight(29), Is.EqualTo(100));
            Assert.That(second.GetComponent<HumanBasePrototypeVisual>().Face.GetBlendShapeWeight(29), Is.EqualTo(100));
            float separation = Vector3.Distance(first.transform.position, second.transform.position);
            Assert.That(separation, Is.GreaterThan(1));
            File.WriteAllText(MaleBlinkPrototypeTools.Review + "/person-social-proof.txt", "Existing Socialize active; actual root separation metres=" + separation);
        }
    }

}
