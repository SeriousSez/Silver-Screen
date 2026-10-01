using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Domain.Work;
using SilverScreen.Presentation.Camera;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Tutorial;
using SilverScreen.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class SimulationSpeedTests
    {
        [TestCase(SimulationSpeed.Normal, 1)]
        [TestCase(SimulationSpeed.Fast, 2)]
        [TestCase(SimulationSpeed.VeryFast, 3)]
        public void CalendarWorkAndActivityApplySelectedMultiplierExactlyOnce(SimulationSpeed speed, int multiplier)
        {
            var clock = new SimulationClock();
            var scheduler = new SimulationScheduler(clock);
            using var work = new WorkService(clock, scheduler);
            work.Create(new WorkDefinition("speed-proof", "test", "test", WorkQuantity.FromUnits("test", 1000)));
            work.RefreshCapacity("speed-proof", new WorkRate(WorkQuantity.FromUnits("test", 1), SimulationDuration.FromMinutes(1)));
            clock.SetSpeed(speed);
            var start = clock.Now;
            clock.Advance(10.0);
            Assert.That((clock.Now - start).Seconds, Is.EqualTo(600 * multiplier));
            Assert.That(work.GetSnapshot("speed-proof").Completed.Units, Is.EqualTo(10 * multiplier));
            Assert.That(LocalPresentationTime.Delta(.25f, clock), Is.EqualTo(.25f * multiplier));
            clock.TogglePause();
            var pausedAt = clock.Now;
            clock.Advance(10.0);
            Assert.That(clock.Now, Is.EqualTo(pausedAt));
            Assert.That(LocalPresentationTime.Delta(.25f, clock), Is.Zero);
            Assert.That(LocalPresentationTime.Delta(.25f, clock, true), Is.EqualTo(.25f));
            clock.TogglePause();
            Assert.That(clock.CurrentSpeed, Is.EqualTo(speed));
        }

        [Test]
        public void LegacyButtonsRebindOnceAndPauseResumesRememberedSpeed()
        {
            var root = new GameObject("Clock UI test");
            try
            {
                var ui = root.AddComponent<SimulationClockUI>();
                var buttons = Enumerable.Range(0, 4).Select(i =>
                {
                    var go = new GameObject("Speed " + i, typeof(RectTransform), typeof(Button));
                    go.transform.SetParent(root.transform); return go.GetComponent<Button>();
                }).ToArray();
                ui.SetupReferences(null, null, buttons[0], buttons[1], buttons[2], buttons[3]);
                ui.SetupReferences(null, null, buttons[0], buttons[1], buttons[2], buttons[3]);
                var clock = new SimulationClock(); ui.BindTimeService(clock);
                foreach (int speed in new[] { 1, 2, 3, 1, 3, 2 })
                {
                    buttons[speed].onClick.Invoke();
                    Assert.That(clock.CurrentSpeed, Is.EqualTo((SimulationSpeed)speed));
                    buttons[0].onClick.Invoke(); Assert.That(clock.IsPaused, Is.True);
                    buttons[0].onClick.Invoke(); Assert.That(clock.CurrentSpeed, Is.EqualTo((SimulationSpeed)speed));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator StudioHudScalesClockPeopleAndAnimationButNotCameraAndPreservesModalPause()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False, "Do not discard unsaved scene work.");
            EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            yield return new EnterPlayMode();
            yield return RunStudioProof();
            yield return new ExitPlayMode();
        }

        private static IEnumerator RunStudioProof()
        {
            yield return null; yield return null;
            var driver = Object.FindAnyObjectByType<SimulationTimeDriver>();
            Assert.That(driver, Is.Not.Null);
            Object.FindAnyObjectByType<StudioGuidanceDriver>()?.Tutorial?.Skip();
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(b => b.transform.parent.name == "SpeedControls").ToDictionary(b => b.name);
            void Click(SimulationSpeed speed) => buttons[speed + "Button"].onClick.Invoke();
            var output = Path.Combine(Path.GetTempPath(), "SilverScreenSpeedRegression");
            Directory.CreateDirectory(output); var log = Path.Combine(output, "runtime.txt");
            File.WriteAllText(log, "Actual Studio HUD, clock, employee/applicant NavMesh, Animator/Playable and camera proof\n");
            var inputMode = InputSystem.settings.updateMode;
            var background = InputSystem.settings.backgroundBehavior;
            bool run = Application.runInBackground;
            var devices = InputSystem.devices.Where(d => d.enabled && (d is Keyboard || d is Mouse)).ToArray();
            foreach (var device in devices) InputSystem.DisableDevice(device);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var objects = new List<GameObject>();
            GameObject Keep(GameObject go) { objects.Add(go); return go; }
            var center = new Vector3(1000, 0, 1000);
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(80, .2f, 20),
                transform = Matrix4x4.TRS(center + Vector3.down * .1f, Quaternion.identity, Vector3.one) };
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source },
                new Bounds(center, new Vector3(84, 4, 24)), Vector3.zero, Quaternion.identity);
            Assert.That(data, Is.Not.Null);
            var navigation = NavMesh.AddNavMeshData(data);
            var graph = PlayableGraph.Create("Simulation speed animation proof");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 100, 1));
            try
            {
                NavMeshAgent Agent(string name, float z)
                {
                    var go = Keep(GameObject.CreatePrimitive(PrimitiveType.Capsule)); go.name = name;
                    go.transform.position = center + new Vector3(-30, 1, z);
                    var nav = go.AddComponent<NavMeshAgent>(); EmployeeNavigationProfile.Configure(nav, 2, 1);
                    nav.speed = 3.5f; nav.acceleration = 14; nav.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                    Assert.That(nav.Warp(center + new Vector3(-30, 0, z)), Is.True); return nav;
                }
                var employeeNav = Agent("Speed proof employee", -3);
                var employee = employeeNav.gameObject.AddComponent<EmployeeAgent>();
                employee.BindDomain(new Employee("speed-proof-employee", "Speed Proof", EmployeeRole.Actor, 50, 100, 80));
                employee.BindTimeService(driver.Clock);
                var candidateNav = Agent("Speed proof applicant", 3);
                var candidate = candidateNav.gameObject.AddComponent<CandidateAgent>();
                candidate.Bind(new Candidate(employee.Employee.Person, 100), driver.Clock);
                var animated = Keep(new GameObject("Speed proof Animator")).AddComponent<Animator>();
                var animation = AnimationClipPlayable.Create(graph, clip);
                AnimationPlayableOutput.Create(graph, "World animation", animated).SetSourcePlayable(animation); graph.Play();
                driver.Work.Create(new WorkDefinition("speed-proof", "test", "test", WorkQuantity.FromUnits("test", 10000)));
                driver.Work.RefreshCapacity("speed-proof", new WorkRate(WorkQuantity.FromUnits("test", 1), SimulationDuration.FromMinutes(1)));

                foreach (var speed in new[] { SimulationSpeed.Normal, SimulationSpeed.Fast, SimulationSpeed.VeryFast, SimulationSpeed.Normal, SimulationSpeed.VeryFast, SimulationSpeed.Fast })
                {
                    Click(speed); Assert.That(Time.timeScale, Is.EqualTo((int)speed));
                    Assert.That(driver.Clock.RequestedSpeed, Is.EqualTo(speed));
                    Assert.That(employee.TryAssignTaskDestination(center + new Vector3(30, 0, -3), new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Speed proof")), Is.True);
                    Assert.That(candidate.TryMove(center + new Vector3(30, 0, 3), _ => { }), Is.True);
                    yield return new WaitForSeconds(.5f); // Warm native acceleration in simulation seconds.
                    var employeeStart = employeeNav.transform.position; var candidateStart = candidateNav.transform.position;
                    var timeStart = driver.Clock.Now; var workStart = driver.Work.GetSnapshot("speed-proof").Completed.Units;
                    double animationStart = animation.GetTime(); float elapsed = 0;
                    while (elapsed < .75f) { yield return null; elapsed += Time.unscaledDeltaTime; }
                    double clockRate = (driver.Clock.Now - timeStart).Seconds / (60 * elapsed);
                    double workRate = (double)(driver.Work.GetSnapshot("speed-proof").Completed.Units - workStart) / elapsed;
                    float employeeRate = Vector3.Distance(employeeStart, employeeNav.transform.position) / (3.5f * elapsed);
                    float applicantRate = Vector3.Distance(candidateStart, candidateNav.transform.position) / (3.5f * elapsed);
                    double animationRate = (animation.GetTime() - animationStart) / elapsed;
                    File.AppendAllText(log, $"{speed}: calendar={clockRate:F3} work={workRate:F3} employee={employeeRate:F3} applicant={applicantRate:F3} animation={animationRate:F3}\n");
                    foreach (double rate in new[] { clockRate, workRate, employeeRate, applicantRate, animationRate })
                        Assert.That(rate, Is.EqualTo((int)speed).Within(.18), "Selected speed must reach each simulation consumer exactly once.");
                    employee.ClearTaskDestination(); employeeNav.Warp(center + new Vector3(-30, 0, -3));
                    candidateNav.ResetPath(); candidateNav.Warp(center + new Vector3(-30, 0, 3));
                }

                Click(SimulationSpeed.VeryFast); Click(SimulationSpeed.Paused);
                var pausedAt = driver.Clock.Now; var pausedPosition = employeeNav.transform.position; var pausedAnimation = animation.GetTime();
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(Time.timeScale, Is.Zero); Assert.That(driver.Clock.Now, Is.EqualTo(pausedAt));
                Assert.That(employeeNav.transform.position, Is.EqualTo(pausedPosition)); Assert.That(animation.GetTime(), Is.EqualTo(pausedAnimation).Within(.001));
                Click(SimulationSpeed.Paused); Assert.That(driver.Clock.CurrentSpeed, Is.EqualTo(SimulationSpeed.VeryFast)); Assert.That(Time.timeScale, Is.EqualTo(3));
                using (var modal = new TutorialSession(new NewStudioOptions { TutorialEnabled = true }, driver.Clock))
                {
                    pausedAt = driver.Clock.Now; Assert.That(Time.timeScale, Is.Zero);
                    Click(SimulationSpeed.Fast); Assert.That(Time.timeScale, Is.Zero);
                    yield return new WaitForSecondsRealtime(.3f); Assert.That(driver.Clock.Now, Is.EqualTo(pausedAt));
                }
                Assert.That(Time.timeScale, Is.EqualTo(2));
                using (var hold = driver.Clock.AcquirePause("speed-proof", "Nested user pause")) Click(SimulationSpeed.Paused);
                Assert.That(Time.timeScale, Is.Zero); Click(SimulationSpeed.Paused); Assert.That(Time.timeScale, Is.EqualTo(2));
                File.AppendAllText(log, "Pause freezes calendar, animation and movement; resume restores 3x; modal selection restores 2x; user pause survives modal close.\n");

                var cameraRates = new List<float>();
                foreach (var speed in new[] { SimulationSpeed.Normal, SimulationSpeed.VeryFast, SimulationSpeed.Paused })
                {
                    if (speed == SimulationSpeed.Paused) driver.Clock.SetUserPaused(true); else Click(speed);
                    var rig = Keep(new GameObject("Speed proof camera"));
                    var cam = Keep(new GameObject("Camera")).AddComponent<Camera>(); cam.transform.SetParent(rig.transform); cam.enabled = false;
                    var control = rig.AddComponent<StudioCameraController>();
                    var settings = new SerializedObject(control); settings.FindProperty("_enableEdgePan").boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
                    float elapsed = 0; while (elapsed < .7f) { yield return null; elapsed += Time.unscaledDeltaTime; }
                    float rate = rig.transform.position.magnitude / elapsed; cameraRates.Add(rate);
                    Assert.That(rate, Is.GreaterThan(10), "Camera remains responsive even while paused.");
                    File.AppendAllText(log, $"Camera at {speed}: {rate:F3} metres/real-second\n");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); control.enabled = false;
                }
                Assert.That(cameraRates.Max() / cameraRates.Min(), Is.LessThan(1.15f));
                Click(SimulationSpeed.VeryFast); driver.enabled = false; Assert.That(Time.timeScale, Is.EqualTo(1));
                driver.enabled = true; Assert.That(Time.timeScale, Is.EqualTo(3));
                File.AppendAllText(log, "Driver disable restores Unity scale; re-enable reapplies selected speed. PASSED.\n");
            }
            finally
            {
                driver.Clock.SetSpeed(SimulationSpeed.Normal);
                graph.Destroy(); Object.DestroyImmediate(clip);
                foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
                navigation.Remove(); Object.DestroyImmediate(data);
                InputSystem.RemoveDevice(keyboard); InputSystem.settings.updateMode = inputMode;
                InputSystem.settings.backgroundBehavior = background; Application.runInBackground = run;
                foreach (var device in devices) InputSystem.EnableDevice(device);
            }
        }
    }
}
