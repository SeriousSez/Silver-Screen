using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.SimulationTime;

namespace SilverScreen.Tests.EditMode
{
    /// <summary>Runs continuously across real Studio Play Mode; creates no saved scene or fixture assets.</summary>
    public sealed class Phase1StudioRuntimeTests
    {
        private static string Output => Path.Combine(Path.GetTempPath(), "SilverScreenPhase1");
        private static void Record(string message) => File.AppendAllText(Path.Combine(Output, "studio-runtime.txt"), DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine);

        [UnityTest, Timeout(240000)]
        public IEnumerator StudioProductionLiveViewRetakeAndRelease()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False, "Do not replace unsaved scene state for validation.");
            // The EditMode runner isolates the user's scene in its own empty test scene
            // and restores it afterward. Load Studio into that isolated test context.
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "studio-runtime.txt"), "Continuous Studio validation" + Environment.NewLine);
            yield return new EnterPlayMode();
            // Create captured runtime variables after the domain reload. Compiler-generated
            // closure objects are not preserved by the EditMode test continuation serializer.
            yield return RunStudioFlow();
            yield return new ExitPlayMode();
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
        }

        private static IEnumerator RunStudioFlow()
        {
            yield return null;
            yield return null;
            var time = UnityEngine.Object.FindAnyObjectByType<SimulationTimeDriver>();
            var driver = UnityEngine.Object.FindAnyObjectByType<MovieProductionDriver>();
            var people = UnityEngine.Object.FindAnyObjectByType<StudioEmployeeManager>();
            Assert.That(time, Is.Not.Null); Assert.That(driver, Is.Not.Null); Assert.That(people, Is.Not.Null);
            Record("Runtime initialized: playing=" + Application.isPlaying + " scene=" + SceneManager.GetActiveScene().path +
                " time=" + time.name + " driver=" + driver.name + " employees=" + people.AllEmployees.Count +
                " router=" + UnityEngine.Object.FindAnyObjectByType<StudioWorldRouter>());
            Assert.That(time.Clock.RealSecondsPerSimulatedMinute, Is.EqualTo(1));
            Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(1));
            var coordinator = driver.Coordinator;
            Assert.That(coordinator, Is.Not.Null, "Studio production coordinator must initialize from its existing scene dependencies.");
            Assert.That(coordinator.ActiveMovie, Is.Null);
            var actor = people.AllEmployees.First(p => p.Role == EmployeeRole.Actor);
            var director = people.AllEmployees.First(p => p.Role == EmployeeRole.Director);
            var agent = people.GetAgent(actor);
            Record("Chosen actor=" + actor.Id + " agent=" + agent);
            Assert.That(agent, Is.Not.Null, "Initial actor must have an EmployeeAgent.");
            var nav = agent.GetComponent<NavMeshAgent>();
            Assert.That(nav, Is.Not.Null, "Initial actor must have a NavMeshAgent.");
            float speed = nav.speed, acceleration = nav.acceleration;
            foreach (var strategicSpeed in new[] { SimulationSpeed.Normal, SimulationSpeed.Fast, SimulationSpeed.VeryFast })
            {
                time.Clock.SetSpeed(strategicSpeed);
                Assert.That(nav.speed, Is.EqualTo(speed));
                Assert.That(nav.acceleration, Is.EqualTo(acceleration));
            }
            time.Clock.SetSpeed(SimulationSpeed.Normal);
            Record("Natural NavMesh speed=" + speed + " acceleration=" + acceleration + " at 1x/2x/3x. Pacing unchanged.");
            var created = coordinator.CreateMovie("Phase 1 Runtime Validation", "drama", 25000, "Lead", new List<string>());
            Assert.That(created.Succeeded, Is.True);
            var movie = created.Project;
            coordinator.SetProductionControlMode(movie, ProductionControlMode.Manual);
            coordinator.AssignActorToRole(movie, movie.Roles[0], actor);
            coordinator.AssignDirector(movie, director);
            Record("Casting route started at " + agent.transform.position);
            yield return WaitFor(() => movie.Roles[0].IsCastingActive, 45, () => coordinator.StatusMessage);
            Record("Actor reached casting at " + agent.transform.position);
            time.Clock.AdvanceTo(time.Clock.Now + SimulationDuration.FromMinutes(60));
            yield return Drain(time);
            Record("Casting completed. Routing to stage: " + coordinator.StatusMessage);
            yield return WaitFor(() => coordinator.CurrentProductionPhase == ProductionPhase.Filming, 75, () => coordinator.StatusMessage);
            var take = coordinator.ActiveTake;
            var performance = take.PerformanceResults.ToArray();
            Assert.That(performance.Length, Is.GreaterThan(0));
            var facility = UnityEngine.Object.FindObjectsByType<SoundStageIdentity>(FindObjectsInactive.Exclude)
                .Single(s => s.FacilityId == coordinator.ActiveEnvironment.FacilityId);
            var slate = facility.GetComponent<PrototypeSlateSequence>();
            var beats = facility.GetComponent<PrototypeScreenplayBeatSequence>();
            var live = facility.GetComponent<LiveFilmingPlayback>();
            var camera = UnityEngine.Camera.main;
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var cameraController = camera.GetComponentInParent<SilverScreen.Presentation.Camera.StudioCameraController>();
            bool controllerEnabled = cameraController != null && cameraController.enabled;
            if (cameraController != null) cameraController.enabled = false;
            camera.transform.position = agent.transform.position + new Vector3(0, 3, -6);
            camera.transform.LookAt(agent.transform.position + Vector3.up);
            Assert.That(slate.IsRunning, Is.True, "The local slate should be active while strategic filming has already started.");
            Record("Filming started with local slate. Work=" + coordinator.ActiveTakeWork.Completed.Units + " at " + agent.transform.position);
            yield return Capture("studio-slate");
            yield return WaitFor(() => beats.IsRunning, 50, () => "slate=" + slate.IsRunning + " " + coordinator.StatusMessage);
            Record("Beat presentation started; index=" + beats.CurrentBeatIndex + " work=" + coordinator.ActiveTakeWork.Completed.Units);
            yield return Capture("studio-beat");
            yield return WaitFor(() => !beats.IsRunning, 65, () => "beat=" + beats.CurrentBeatIndex);
            Assert.That(take.Status, Is.EqualTo(MovieTakeStatus.Recording));
            Assert.That(coordinator.ActiveTakeWork.Completed.Units, Is.LessThan(480));
            Record("Local slate/beats finished while strategic take remains Recording; work=" + coordinator.ActiveTakeWork.Completed.Units);
            camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            if (cameraController != null) cameraController.enabled = controllerEnabled;
            Assert.That(live.TryEnter(), Is.True);
            var pausedAt = time.Clock.Now;
            var pausedWork = coordinator.ActiveTakeWork.Completed.Units;
            yield return Capture("studio-live-camera");
            yield return new WaitForSecondsRealtime(4);
            Assert.That(live.IsViewing, Is.True);
            Assert.That(time.Clock.Now, Is.EqualTo(pausedAt));
            Assert.That(coordinator.ActiveTakeWork.Completed.Units, Is.EqualTo(pausedWork));
            Assert.That(take.PerformanceResults, Is.EqualTo(performance));
            time.Clock.SetSpeed(SimulationSpeed.VeryFast);
            Assert.That(time.Clock.IsPaused, Is.True);
            Assert.That(nav.speed, Is.EqualTo(speed));
            time.Clock.SetUserPaused(true);
            live.Exit();
            Assert.That(time.Clock.IsPaused, Is.True, "Exiting Live View must preserve user pause.");
            time.Clock.SetSpeed(SimulationSpeed.Normal);
            Assert.That(live.TryEnter(), Is.True);
            live.enabled = false;
            Assert.That(time.Clock.IsPaused, Is.False, "Disabling Live View must release its owned hold.");
            live.enabled = true;
            Record("Live Camera animation ran under a frozen strategic clock; stored results unchanged; pause/disable ownership passed.");

            time.Clock.AdvanceTo(time.Clock.Now + SimulationDuration.FromMinutes(480));
            yield return Drain(time);
            Assert.That(coordinator.CanKeepTake, Is.True);
            Assert.That(take.FilmedDuration, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(time.Reservations.Count, Is.EqualTo(1));
            long claim = time.Reservations.GetClaims()[0].Id;
            Assert.That(coordinator.ShootAgain(), Is.True);
            yield return WaitFor(() => coordinator.CurrentProductionPhase == ProductionPhase.Filming, 40, () => coordinator.StatusMessage);
            var retake = coordinator.ActiveTake;
            Assert.That(retake.Id, Is.Not.EqualTo(take.Id));
            Assert.That(time.Reservations.GetClaims()[0].Id, Is.EqualTo(claim));
            Assert.That(coordinator.ActiveTakeWork.Completed.Units, Is.LessThan(480));
            Record("Manual retake began; same claim transferred to new take.");
            time.Clock.AdvanceTo(time.Clock.Now + SimulationDuration.FromMinutes(480));
            yield return Drain(time);
            Assert.That(coordinator.KeepTake(retake.Id), Is.True);
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Completed));
            Assert.That(time.Reservations.Count, Is.Zero);
            Assert.That(actor.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Assert.That(director.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Record("Manual keep completed the movie and released cast/facility.");

            Assert.That(driver.ReleaseService.ReleaseMovie(movie).Succeeded, Is.True);
            var comparison = new MovieProject("phase1-other-release", "Concurrent release fixture", "drama", "Drama", 25000, time.Clock.CurrentTime);
            ProductionLifecycleRegressionTests.CompleteMovie(comparison);
            comparison.TrySetProductionResult(new MovieProductionResult(60, 60, 60, 60));
            Assert.That(driver.ReleaseService.ReleaseMovie(comparison).Succeeded, Is.True);
            time.Clock.AdvanceTo(time.Clock.Now + SimulationDuration.FromDays(28));
            yield return Drain(time);
            Assert.That(movie.TheatricalRun.CurrentDay, Is.EqualTo(28));
            Assert.That(movie.TheatricalRun.WeeksPaid, Is.EqualTo(4));
            Assert.That(comparison.TheatricalRun.WeeksPaid, Is.EqualTo(4));
            var revenue = movie.TheatricalRun.TotalStudioRevenue;
            time.Clock.AdvanceTo(time.Clock.Now + SimulationDuration.FromDays(2));
            yield return Drain(time);
            Assert.That(movie.TheatricalRun.TotalStudioRevenue, Is.EqualTo(revenue));
            Assert.That(time.Clock.AdvanceFailure, Is.Null);
            Assert.That(time.Clock.RealSecondsPerSimulatedMinute, Is.EqualTo(1));
            Record("Both theatrical runs completed 28 days/four payouts; subsequent advance caused no duplicate revenue. PASS.");
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds, Func<string> diagnostic)
        {
            double start = UnityEngine.Time.realtimeSinceStartupAsDouble;
            while (!condition())
            {
                if (UnityEngine.Time.realtimeSinceStartupAsDouble - start > seconds)
                {
                    Record("TIMEOUT: " + diagnostic());
                    Assert.Fail(diagnostic());
                }
                yield return null;
            }
        }
        private static IEnumerator Drain(SimulationTimeDriver time)
        {
            int frames = 0;
            while (time.Clock.HasPendingAdvance && frames++ < 100) yield return null;
            Assert.That(time.Clock.HasPendingAdvance, Is.False);
            Assert.That(time.Clock.AdvanceFailure, Is.Null);
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + ".png"));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
