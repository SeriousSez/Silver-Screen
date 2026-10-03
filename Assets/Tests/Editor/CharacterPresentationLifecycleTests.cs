using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Characters;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Recruitment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public abstract class CharacterRuntimePlayFixture
    {
        private const string SceneKey = "SilverScreen.CharacterRuntimeV1.PreviousScene";
        private NavMeshData _data; private NavMeshDataInstance _navigation;
        private List<Object> _owned; private bool _background;
        protected const float GroundY = 3;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1)); Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            _owned = new List<Object>(); _background = Application.runInBackground; Application.runInBackground = true;
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(24, .2f, 24),
                transform = Matrix4x4.TRS(new Vector3(0, GroundY - .1f, 0), Quaternion.identity, Vector3.one) };
            _data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source },
                new Bounds(new Vector3(0, GroundY, 0), new Vector3(28, 6, 28)), Vector3.zero, Quaternion.identity);
            _navigation = NavMesh.AddNavMeshData(_data);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (_owned != null) for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
            if (_navigation.valid) _navigation.Remove(); if (_data != null) Object.DestroyImmediate(_data);
            if (Application.isPlaying) { Application.runInBackground = _background; yield return new ExitPlayMode(); }
            var path = SessionState.GetString(SceneKey, ""); SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }
        protected T Keep<T>(T value) where T : Object { _owned.Add(value); return value; }
        protected GameObject Root(Vector3? ground = null)
        {
            var root = Keep(GameObject.CreatePrimitive(PrimitiveType.Capsule)); root.SetActive(false);
            root.transform.position = (ground ?? new Vector3(0, GroundY, 0)) + Vector3.up;
            var nav = root.AddComponent<NavMeshAgent>(); EmployeeNavigationProfile.Configure(nav, 2, 1);
            nav.speed = 1.35f; nav.acceleration = 8; nav.angularSpeed = 360; nav.stoppingDistance = .12f;
            var ring = new GameObject("SelectionRing"); ring.transform.SetParent(root.transform, false);
            return root;
        }
        protected CharacterFamilyCatalog Synthetic(CharacterFamily family = CharacterFamily.Girl)
        {
            var d = Keep(ScriptableObject.CreateInstance<CharacterFamilyDefinition>());
            d.Configure(family, "synthetic:" + family, CharacterPhysicalProfileTests.Small(), new CharacterFacialBinding(
                new FacialBindingEntry(FacialSemantic.BlinkBoth, "BlinkBoth"), new FacialBindingEntry(FacialSemantic.JawOpen, "jaw")), "Synthetic, no licensed inputs", false);
            var visual = Keep(new GameObject("Synthetic visual")); visual.SetActive(false);
            var animator = visual.AddComponent<Animator>(); var face = visual.AddComponent<SkinnedMeshRenderer>();
            face.sharedMesh = Keep(CharacterFacialControllerTests.MeshWith("jaw", "BlinkBoth"));
            var anchor = new GameObject("Head anchor"); anchor.transform.SetParent(visual.transform, false);
            var reference = visual.AddComponent<CharacterVisualReference>(); reference.Configure(animator, face, anchor.transform, d);
            var catalog = Keep(ScriptableObject.CreateInstance<CharacterFamilyCatalog>()); catalog.Configure(new CharacterFamilyCatalogEntry(d, reference)); return catalog;
        }
    }

    public sealed class CharacterPresentationLifecycleTests : CharacterRuntimePlayFixture
    {
        [UnityTest] public IEnumerator TransactionalHireAndDisabledRootRestoreTheExistingCarrySession()
        {
            var root = Root(); var person = CharacterPresentationIdentityTests.Person(CharacterFamily.Boy);
            var candidate = new Candidate(person, 10); candidate.MarkWaiting(0);
            var agent = root.AddComponent<CandidateAgent>(); agent.Bind(candidate, null);
            var p = CharacterPresentationFactory.Attach(root, person, Synthetic(CharacterFamily.Boy)).Presentation;
            var visual = p.Visual; var profile = p.PhysicalProfile; p.Face.TrySet(FacialSemantic.BlinkBoth, .75f);
            var router = Keep(new GameObject("Transactional router")).AddComponent<CandidateWorldRouter>(); router.RegisterExistingCandidate(agent);
            root.SetActive(true); yield return null; yield return null;
            var drag = PersonDragSession.Begin(null, agent); Assert.That(drag, Is.Not.Null); drag.Follow(new Vector3(1, GroundY, 0));
            var heldPosition = root.transform.position;
            Assert.That(drag.TryContextualDrop(new Vector3(3, GroundY, 0), null, Quaternion.identity, () => false), Is.False);
            Assert.That(drag.IsHeld); Assert.That(root.transform.position, Is.EqualTo(heldPosition));
            EmployeeAgent employee = null;
            Assert.That(drag.TryContextualDrop(new Vector3(3, GroundY, 0), null, Quaternion.identity, () =>
            { candidate.MarkHired(); employee = router.ConvertToEmployee(candidate, new Employee(person, EmployeeRole.Actor, 10)); return employee != null; }));
            Assert.That(drag.IsHeld || employee.IsHeld, Is.False); Assert.That(p.Visual, Is.SameAs(visual)); Assert.That(p.PhysicalProfile, Is.EqualTo(profile));
            Assert.That(root.GetComponent<NavMeshAgent>().updatePosition); Assert.That(root.GetComponent<CapsuleCollider>().enabled);
            yield return new WaitForSecondsRealtime(.2f); Assert.That(p.IsPosingHeld, Is.False);
            Assert.That(visual.Face.GetBlendShapeWeight(1), Is.EqualTo(75)); Assert.That(root.GetComponents<CandidateAgent>(), Is.Empty);
            var original = root.transform.position; drag = PersonDragSession.Begin(employee, null); Assert.That(drag, Is.Not.Null);
            drag.Follow(new Vector3(0, GroundY, 0)); root.SetActive(false); drag.Cancel();
            Assert.That(drag.IsHeld || p.IsPosingHeld || employee.IsHeld, Is.False); Assert.That(root.transform.position, Is.EqualTo(original));
            Assert.That(root.GetComponent<CapsuleCollider>().enabled); Assert.That(root.GetComponent<NavMeshAgent>().updatePosition);
        }
        [UnityTest] public IEnumerator CandidateWaitingHeldHireAndCancelKeepOnePersonAndVisual()
        {
            var root = Root(); var person = CharacterPresentationIdentityTests.Person(CharacterFamily.Girl); var candidate = new Candidate(person, 10); candidate.MarkWaiting(0);
            var agent = root.AddComponent<CandidateAgent>(); agent.Bind(candidate, null); agent.SetSelectionIndicator(root.transform.Find("SelectionRing").gameObject); agent.SetSelected(true);
            var catalog = Synthetic(); var result = CharacterPresentationFactory.Attach(root, person, catalog); Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            var p = result.Presentation; var visual = p.Visual; var animator = p.Animator; var face = p.Face; var profile = p.PhysicalProfile;
            p.Face.TrySet(FacialSemantic.BlinkBoth, .5f); var mesh = visual.Face.sharedMesh; var vertices = mesh.vertices;
            root.SetActive(true); yield return null; yield return null;
            Assert.That(root.transform.Find("WaitingBody"), Is.Null); Assert.That(root.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(1.4f));
            var original = root.transform.position; var local = visual.transform.localPosition;
            var drag = PersonDragSession.Begin(null, agent); Assert.That(drag, Is.Not.Null); drag.Follow(new Vector3(2, GroundY, 0));
            root.GetComponent<HeldPersonPresentation>().AdvancePresentation(.1f);
            Assert.That(p.IsPosingHeld); Assert.That(root.transform.Find("HeldPersonVisual"), Is.Null);
            Assert.That(CharacterPresentationFactory.Attach(root, person, catalog).Succeeded, Is.False, "No rebuild while held");
            var router = Keep(new GameObject("Isolated router")).AddComponent<CandidateWorldRouter>(); router.RegisterExistingCandidate(agent);
            var employee = new Employee(person, EmployeeRole.Actor, 10); var employeeAgent = router.ConvertToEmployee(candidate, employee);
            Assert.That(employeeAgent.gameObject, Is.SameAs(root)); Assert.That(employeeAgent.IsHeld); Assert.That(employeeAgent.IsSelected);
            Assert.That(employeeAgent.Employee.Person, Is.SameAs(person)); Assert.That(p.Visual, Is.SameAs(visual)); Assert.That(p.Animator, Is.SameAs(animator));
            Assert.That(p.Face, Is.SameAs(face)); Assert.That(p.PhysicalProfile, Is.EqualTo(profile));
            Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(1.4f), "Employee Awake must not replace resolved profile");
            drag.Cancel(); Assert.That(root.transform.position, Is.EqualTo(original)); Assert.That(p.IsPosingHeld, Is.False);
            Assert.That(employeeAgent.IsHeld, Is.False); Assert.That(visual.transform.localPosition, Is.EqualTo(local));
            Assert.That(visual.Face.GetBlendShapeWeight(mesh.GetBlendShapeIndex("BlinkBoth")), Is.EqualTo(50));
            yield return null;
            Assert.That(root.GetComponents<CandidateAgent>(), Is.Empty); Assert.That(root.GetComponents<EmployeeAgent>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<NavMeshAgent>().Length, Is.EqualTo(1)); Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            Assert.That(mesh.vertices, Is.EqualTo(vertices)); Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            foreach (var role in new[] { EmployeeRole.Director, EmployeeRole.Extra, EmployeeRole.Actor })
            { employee.ChangeProfession(role); Assert.That(p.Visual, Is.SameAs(visual)); Assert.That(p.Face, Is.SameAs(face)); Assert.That(p.PhysicalProfile, Is.EqualTo(profile)); }
            var roster = new WorkforceRoster(); roster.Add(employee); employeeAgent.CancelPresentationMovement();
            Assert.That(roster.TryDismiss(employee)); Assert.That(p.Visual, Is.SameAs(visual), "Dismissal does not own the visual lifetime");
            Assert.That(CharacterPresentationFactory.Attach(root, person, catalog).Presentation, Is.SameAs(p));
            Assert.That(p.Visual, Is.SameAs(visual), "Idempotent presentation binding preserves state");
        }

        [UnityTest] public IEnumerator ExplicitProfilesSurviveAwakeAndInstanceOverrideDoesNotMutateFamily()
        {
            foreach (var family in new[] { CharacterFamily.AdultFemale, CharacterFamily.AdultMale, CharacterFamily.Boy, CharacterFamily.Girl })
            {
                var root = Root(new Vector3((int)family * 2 - 5, GroundY, -3)); var person = CharacterPresentationIdentityTests.Person(family);
                var catalog = Synthetic(family); catalog.TryResolve(family, out var entry, out _);
                var measured = CharacterPhysicalProfileTests.Definition(family).DefaultPhysicalProfile;
                var p = CharacterPresentationFactory.Attach(root, person, catalog, measured).Presentation;
                var employee = root.AddComponent<EmployeeAgent>(); employee.BindDomain(new Employee(person, EmployeeRole.Actor, 10)); root.SetActive(true);
                yield return null;
                Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(measured.NavigationHeight));
                Assert.That(root.GetComponent<CapsuleCollider>().height, Is.EqualTo(measured.CapsuleHeight));
                Assert.That(NavMesh.SamplePosition(root.transform.position, out var ground, 2, NavMesh.AllAreas));
                Assert.That(p.InformationAnchorWorld.y, Is.EqualTo(ground.position.y + measured.InformationAnchor.y).Within(.0001));
                Assert.That(entry.Definition.DefaultPhysicalProfile.BodyHeight, Is.EqualTo(1.4f), "Instance resolution is a value copy");
            }
            var legacy = Root(new Vector3(0, GroundY, 4)); legacy.AddComponent<EmployeeAgent>(); legacy.SetActive(true); yield return null;
            Assert.That(legacy.GetComponent<NavMeshAgent>().height, Is.EqualTo(2)); Assert.That(legacy.GetComponent<NavMeshAgent>().baseOffset, Is.EqualTo(1));
            Assert.That(legacy.GetComponent<CapsuleCollider>().radius, Is.EqualTo(.35f));
        }

        [UnityTest] public IEnumerator AbsentInvalidAndUnsafeCataloguesFallbackWithoutChangingIdentity()
        {
            var person = CharacterPresentationIdentityTests.Person(CharacterFamily.Boy);
            var root = Root(); var agent = root.AddComponent<CandidateAgent>(); var candidate = new Candidate(person, 10); candidate.MarkWaiting(0); agent.Bind(candidate, null);
            var missing = CharacterPresentationFactory.Attach(root, person, null); Assert.That(missing.IsCanonical, Is.False); Assert.That(missing.Diagnostic, Does.Contain("absent"));
            Assert.That(person.PresentationIdentity.Family, Is.EqualTo(CharacterFamily.Boy)); Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(2));
            root.SetActive(true); yield return null; yield return null;
            Assert.That(root.transform.Find("WaitingBody"), Is.Not.Null, "Legacy waiting behavior remains");
            agent.SetHeld(true); yield return null; Assert.That(root.GetComponent<MeshRenderer>().enabled); agent.SetHeld(false);
            var catalog = Synthetic(CharacterFamily.Boy); catalog.TryResolve(CharacterFamily.Boy, out var entry, out _);
            catalog.Configure(entry, entry);
            Assert.That(CharacterPresentationFactory.Attach(root, person, catalog).Diagnostic, Does.Contain("Duplicate"));
            catalog.Configure(new CharacterFamilyCatalogEntry(entry.Definition));
            Assert.That(CharacterPresentationFactory.Attach(root, person, catalog).Diagnostic, Does.Contain("unavailable"));
            catalog.Configure(entry); entry.Visual.gameObject.AddComponent<CapsuleCollider>();
            Assert.That(CharacterPresentationFactory.Attach(root, person, catalog).Diagnostic, Does.Contain("non-presentation"));
            Assert.That(root.GetComponentsInChildren<CharacterVisualReference>(), Is.Empty);
            Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(2)); Assert.That(person.PresentationIdentity.Family, Is.EqualTo(CharacterFamily.Boy));
            var empty = Keep(new GameObject("Missing visual root")); empty.SetActive(false); empty.transform.position = new Vector3(4, GroundY + 1, 0);
            EmployeeNavigationProfile.Configure(empty.AddComponent<NavMeshAgent>(), 2, 1); empty.AddComponent<CapsuleCollider>();
            Assert.That(CharacterPresentationFactory.Attach(empty, person, null).IsCanonical, Is.False);
            Assert.That(empty.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
            Assert.That(empty.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(EmployeeNavigationProfile.BodyMesh));
            Assert.That(empty.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator FailedRebuildAndDisableRestoreOnlyOwnedPresentation()
        {
            var root = Root(); var person = CharacterPresentationIdentityTests.Person(CharacterFamily.Girl); var catalog = Synthetic();
            var p = CharacterPresentationFactory.Attach(root, person, catalog).Presentation; var visual = p.Visual; var mesh = visual.Face.sharedMesh;
            root.SetActive(true); yield return null;
            var local = visual.transform.localPosition; var rotation = visual.transform.localRotation; var scale = visual.transform.localScale;
            var held = root.AddComponent<HeldPersonPresentation>(); held.BeginHeld(); held.AdvancePresentation(.1f); held.AdvancePresentation(.1f);
            Assert.That(p.IsPosingHeld); root.SetActive(false);
            Assert.That(p.IsPosingHeld, Is.False); Assert.That(visual.transform.localPosition, Is.EqualTo(local));
            Assert.That(visual.transform.localRotation, Is.EqualTo(rotation)); Assert.That(visual.transform.localScale, Is.EqualTo(scale));
            Assert.That(CharacterPresentationFactory.Attach(root, person, null).Presentation, Is.SameAs(p)); Assert.That(p.Visual, Is.SameAs(visual));
            Assert.That(root.GetComponent<NavMeshAgent>().height, Is.EqualTo(1.4f));
            var rebuilt = CharacterPresentationFactory.Attach(root, person, catalog, CharacterPhysicalProfileTests.Small(offset: .6f));
            Assert.That(rebuilt.Succeeded, Is.True, rebuilt.Diagnostic); Assert.That(rebuilt.Presentation, Is.SameAs(p));
            Assert.That(p.Person, Is.SameAs(person)); Assert.That(person.PresentationIdentity.Family, Is.EqualTo(CharacterFamily.Girl));
            var rebuiltVisual = p.Visual; Assert.That(rebuiltVisual, Is.Not.SameAs(visual));
            Object.Destroy(root); yield return null; Assert.That(visual == null); Assert.That(mesh != null, "Shared mesh survives owner destruction");
            Assert.That(rebuiltVisual == null);
        }
    }
}
