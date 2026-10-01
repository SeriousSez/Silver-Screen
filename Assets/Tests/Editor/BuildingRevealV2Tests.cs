using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Interaction;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class BuildingRevealV2Tests
    {
        private Employee Worker() => new Employee("worker", "Walter Hayes", EmployeeRole.ConstructionWorker, 12, 100);
        [Test] public void PartitionOcclusionDependsOnCameraSideAndHeight()
        {
            var wall = new Bounds(new Vector3(0,1.5f,0),new Vector3(.2f,3,6));
            var room = new Vector3(2,0,0);
            Assert.That(BuildingCutawayController.SegmentObstructed(wall,new Vector3(-8,5,0),room),Is.True);
            Assert.That(BuildingCutawayController.SegmentObstructed(wall,new Vector3(8,5,0),room),Is.False);
            Assert.That(BuildingCutawayController.SegmentObstructed(wall,new Vector3(-2,20,0),room),Is.False);
        }
        [Test] public void RoomPlacementUsesIndicatedPointRatherThanItsAuthoredCenter()
        {
            var go = new GameObject("room");
            try
            {
                var action = go.AddComponent<CarryTestAction>(); var target = go.AddComponent<ContextualDropTarget>();
                target.ConfigureRoom(action,go.transform,null,new[] { new Rect(-3,-3,6,6) },Vector2.zero,3);
                var preferred = new Vector3(1.3f,0,-1.6f);
                Assert.That(target.TryFindRoomPlacement(preferred,.35f,_ => true,out var position),Is.True);
                Assert.That(position,Is.EqualTo(preferred));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void OccupiedRoomPointUsesDeterministicClearFallbackAndFullRoomRejects()
        {
            var go = new GameObject("room");
            try
            {
                var action = go.AddComponent<CarryTestAction>(); var target = go.AddComponent<ContextualDropTarget>();
                target.ConfigureRoom(action,go.transform,null,new[] { new Rect(-3,-3,6,6) },Vector2.zero,3);
                bool Clear(Vector3 point) => point.sqrMagnitude > 1;
                Assert.That(target.TryFindRoomPlacement(Vector3.zero,.35f,Clear,out var first),Is.True);
                Assert.That(first.sqrMagnitude,Is.GreaterThan(1));
                Assert.That(target.TryFindRoomPlacement(Vector3.zero,.35f,Clear,out var second),Is.True);
                Assert.That(first,Is.EqualTo(second));
                Assert.That(target.TryFindRoomPlacement(Vector3.zero,.35f,_ => false,out _),Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ReassignmentPreservesIdentityTalentAndPerProfessionSkill()
        {
            var worker = Worker(); worker.Skill = 36;
            var person = worker.Person; var talent = person.Talent; talent.AddGenreExperience("comedy", 8);
            var roster = new WorkforceRoster(); roster.Add(worker);
            int changes = 0; worker.ProfessionChanged += (e, before, after) => changes++;
            Assert.That(roster.TryReassign(worker, EmployeeRole.Groundskeeper), Is.True);
            worker.Skill = 17;
            Assert.That(roster.TryReassign(worker, EmployeeRole.ConstructionWorker), Is.True);
            Assert.That(worker.Skill, Is.EqualTo(36)); Assert.That(worker.GetProfessionSkill(EmployeeRole.Groundskeeper), Is.EqualTo(17));
            Assert.That(worker.Person, Is.SameAs(person)); Assert.That(worker.Person.Talent, Is.SameAs(talent));
            Assert.That(talent.GetGenreExperience("comedy"), Is.EqualTo(8)); Assert.That(changes, Is.EqualTo(2));
            Assert.That(person.ProfessionalRole, Is.EqualTo(ProfessionalRole.ConstructionWorker));
        }
        [Test] public void DismissalRemovesEmploymentButRetainsTheSamePersonAndIsIdempotent()
        {
            var worker = Worker(); var roster = new WorkforceRoster(); roster.Add(worker);
            Assert.That(roster.TryDismiss(worker), Is.True); Assert.That(roster.Employees, Is.Empty);
            Assert.That(roster.FormerEmployees[0], Is.SameAs(worker)); Assert.That(worker.IsEmployed, Is.False);
            Assert.That(roster.TryDismiss(worker), Is.False); Assert.That(roster.TryReassign(worker, EmployeeRole.Actor), Is.False);
        }
        [Test] public void AssignmentsAndReservationsProtectBothWorkforceOperations()
        {
            bool reserved = true; var roster = new WorkforceRoster(_ => reserved); var worker = Worker(); roster.Add(worker);
            Assert.That(roster.TryReassign(worker, EmployeeRole.Groundskeeper), Is.False); Assert.That(roster.TryDismiss(worker), Is.False);
            reserved = false; worker.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.PerformTask));
            Assert.That(roster.CanChangeEmployment(worker), Is.False);
            worker.SetIntent(EmployeeIntent.None); Assert.That(roster.CanChangeEmployment(worker), Is.True);
            Assert.That(roster.TryReassign(worker, worker.Role), Is.False);
        }
        [Test] public void CurrentProfessionRemainsRelevantButIsNotAnExecutableChange()
        {
            var go = new GameObject("room");
            try
            {
                var action = go.AddComponent<WorkforceRoomAction>(); action.Configure("room", "service", ProfessionalRole.ConstructionWorker, false, null);
                var context = new PersonDropContext(null, Worker(), null, null);
                Assert.That(action.IsRelevant(context), Is.True); Assert.That(action.IsCurrent(context), Is.True);
                StringAssert.Contains("CURRENT PROFESSION", action.FloorLabel(context)); Assert.That(action.CanExecute(context), Is.False);
                action.Configure("dismiss", "service", ProfessionalRole.ConstructionWorker, true, null);
                Assert.That(action.IsRelevant(default), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void IndependentRevealReasonsPreserveCollisionLodEnablementAndMaterials()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var renderer = go.GetComponent<Renderer>(); var material = renderer.sharedMaterial;
                var cut = go.AddComponent<BuildingCutawayController>();
                cut.Configure(new[] { new BuildingCutawayController.VisibilityGroup { Roof = true, Renderers = new[] { renderer }, Occluders = new[] { go.GetComponent<Collider>() } } }, new Bounds(Vector3.zero, Vector3.one));
                cut.SetReveal(BuildingRevealReason.PointerHover | BuildingRevealReason.BuildingFocus, true, Vector3.up);
                cut.SetReveal(BuildingRevealReason.PointerHover, false, Vector3.up);
                Assert.That(cut.IsRevealed, Is.True); Assert.That(renderer.forceRenderingOff, Is.True);
                Assert.That(renderer.enabled, Is.True); Assert.That(go.GetComponent<Collider>().enabled, Is.True);
                cut.SetReveal(BuildingRevealReason.BuildingFocus, false, Vector3.up);
                Assert.That(renderer.forceRenderingOff, Is.False); Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void CarryEnvelopeStartsOutsideFootprintAndHasReleaseHysteresis()
        {
            var go = new GameObject("building");
            try
            {
                var cut = go.AddComponent<BuildingCutawayController>();
                Assert.That(cut.WithinCarryEnvelope(new Vector3(10,0,0), false), Is.True);
                Assert.That(cut.WithinCarryEnvelope(new Vector3(12,0,0), false), Is.False);
                Assert.That(cut.WithinCarryEnvelope(new Vector3(12,0,0), true), Is.True);
                Assert.That(cut.WithinCarryEnvelope(new Vector3(13,0,0), true), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void CompoundRoomTargetsBothArmsButNotTheMissingCorner()
        {
            var go = new GameObject("room"); var view = new GameObject("camera"); var placement = new GameObject("placement");
            try
            {
                go.transform.position = new Vector3(1000,0,1000);
                placement.transform.position = go.transform.position + Vector3.right;
                var action = go.AddComponent<CarryTestAction>(); var target = go.AddComponent<ContextualDropTarget>();
                target.ConfigureRoom(action, placement.transform, null, new[] { new Rect(-3,-3,6,2), new Rect(1,-1,2,4) }, Vector2.zero, 3);
                var camera = view.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6;
                camera.pixelRect = new Rect(0,0,1000,1000); camera.transform.SetPositionAndRotation(go.transform.position + Vector3.up*20, Quaternion.Euler(90,0,0));
                foreach (var point in new[] { new Vector3(-2,0,-2), new Vector3(2,0,2) })
                { Assert.That(target.TryScore(camera,camera.WorldToScreenPoint(go.transform.position+point),false,out var score),Is.True); Assert.That(score,Is.EqualTo(-1)); }
                Assert.That(target.TryScore(camera,camera.WorldToScreenPoint(go.transform.position+new Vector3(-2,0,2)),false,out _),Is.False);
                Assert.That(target.Placement,Is.SameAs(placement.transform));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(view); Object.DestroyImmediate(placement); }
        }
    }

    public static class BuildingRevealV2Validation
    {
        private static TestRunnerApi _runner;
        [MenuItem("SilverScreen/Validation/Building Reveal V2 (focused)")]
        public static void Run()
        {
            if (_runner != null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _runner = ScriptableObject.CreateInstance<TestRunnerApi>(); _runner.RegisterCallbacks(new Results());
            _runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                testNames = new[] { "SilverScreen.Tests.EditMode.BuildingRevealV2Tests", "SilverScreen.Tests.EditMode.ContextualCarryTargetTests" } }));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Temp/BuildingRevealV2"); TestRunnerApi.SaveResultToFile(result, "Temp/BuildingRevealV2/results.xml");
                Debug.Log($"Building Reveal V2: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
                Object.DestroyImmediate(_runner); _runner = null;
            }
        }
    }
}
