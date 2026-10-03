using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Characters;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Writing;
using SilverScreen.Editor.Characters;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class CharacterRuntimeIntegrationTests : CharacterRuntimePlayFixture
    {
        private const string Evidence = "TestResults/CharacterRuntimeV1";
        private static readonly CharacterFamily[] Families = { CharacterFamily.AdultFemale, CharacterFamily.AdultMale, CharacterFamily.Boy, CharacterFamily.Girl };
        private GameObject Spawn(CharacterFamily family, Vector3 ground)
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CharacterFamilyCatalog>(CharacterRuntimeSetupTools.CatalogPath);
            Assert.That(catalogue, Is.Not.Null, "Licensed prerequisite absent: run SilverScreen/Characters/Runtime V1/Build local visual wrappers. Missing inputs are not a pass.");
            var root = Root(ground); root.name = "Shared runtime " + family;
            var person = CharacterPresentationIdentityTests.Person(family);
            var result = CharacterPresentationFactory.Attach(root, person, catalogue); Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            var employee = root.AddComponent<EmployeeAgent>(); employee.BindDomain(new Employee(person, EmployeeRole.Actor, 10));
            employee.SetSelectionIndicator(root.transform.Find("SelectionRing").gameObject); root.SetActive(true);
            Assert.That(result.Presentation.Definition.Family, Is.EqualTo(family));
            Assert.That(root.GetComponentsInChildren<NavMeshAgent>().Length, Is.EqualTo(1)); Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            return root;
        }
        private static Vector3[] Bake(SkinnedMeshRenderer face)
        {
            var mesh = new Mesh(); try { face.BakeMesh(mesh, true); var vertices = mesh.vertices;
                Assert.That(vertices.All(p => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z))); return vertices; }
            finally { Object.DestroyImmediate(mesh); }
        }
        private Camera Camera()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.38f, .38f, .38f);
            var key = Keep(new GameObject("Technical key")).AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 2; key.transform.rotation = Quaternion.Euler(25, -35, 0);
            var fill = Keep(new GameObject("Technical fill")).AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .5f; fill.transform.rotation = Quaternion.Euler(10, 135, 0);
            var camera = Keep(new GameObject("Technical camera")).AddComponent<Camera>(); camera.orthographic = true;
            camera.nearClipPlane = .01f; camera.farClipPlane = 30; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f, .17f, .21f); return camera;
        }
        private static void Capture(Camera camera, CharacterPresentation p, string name, bool whole = false, bool oblique = false)
        {
            var target = whole ? p.PhysicalProfile.FeetPoint(p.transform.position) + Vector3.up * p.PhysicalProfile.BodyHeight * .55f
                : p.HeadAttachment.position + Vector3.up * p.PhysicalProfile.BodyHeight * .045f;
            camera.orthographicSize = whole ? p.PhysicalProfile.BodyHeight * .75f : p.PhysicalProfile.BodyHeight * .14f;
            camera.transform.position = target + (oblique ? new Vector3(.6f, .06f, .8f) : new Vector3(0, 0, 2)); camera.transform.LookAt(target);
            Directory.CreateDirectory(Evidence + "/visual/" + p.Definition.Family);
            HumanBasePrototypeTools.Capture(camera, "../CharacterRuntimeV1/visual/" + p.Definition.Family + "/" + name, 800, 700);
        }

        [UnityTest] public IEnumerator AllFourFacialChannelsAndGirlSampledTrajectoryRemainNative()
        {
            var camera = Camera(); var report = new List<string>();
            foreach (var family in Families)
            {
                var root = Spawn(family, new Vector3(0, GroundY, 0)); var p = root.GetComponent<CharacterPresentation>(); var face = p.Visual.Face; var mesh = face.sharedMesh;
                root.GetComponent<EmployeeAgent>().enabled = false; yield return null; yield return null;
                Assert.That(p.Animator.avatar.isValid && p.Animator.avatar.isHuman); Assert.That(p.Animator.applyRootMotion, Is.False);
                p.Animator.Play("Idle", 0, 0); p.Animator.Update(0); p.Animator.enabled = false;
                int blink = mesh.GetBlendShapeIndex("BlinkBoth"); Assert.That(mesh.GetBlendShapeFrameCount(blink), Is.EqualTo(20));
                var neutral = Bake(face); Vector3[] half = null, closed = null;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterRuntimeSetupTools.ReadRecipes().Single(r => r.family == family).referencePrefab).GetComponent<HumanBasePrototypeVisual>();
                Assert.That(mesh, Is.SameAs(source.Face.sharedMesh)); Assert.That(p.Animator.avatar, Is.SameAs(source.Animator.avatar));
                Assert.That(p.Animator.runtimeAnimatorController, Is.SameAs(source.Animator.runtimeAnimatorController));
                Assert.That(face.sharedMaterials, Is.EqualTo(source.Face.sharedMaterials));
                foreach (float weight in new[] { 0, .25f, .5f, .75f, 1f })
                {
                    Assert.That(p.Face.TrySet(FacialSemantic.BlinkBoth, weight)); Assert.That(face.GetBlendShapeWeight(blink), Is.EqualTo(weight * 100));
                    var positions = Bake(face); if (weight == .5f) half = positions; if (weight == 1) closed = positions;
                    yield return null; Capture(camera, p, "blink-" + (int)(weight * 100));
                    if (weight == 0 || weight == .5f || weight == 1) Capture(camera, p, "oblique-" + (int)(weight * 100), oblique: true);
                }
                var deltas = new Vector3[mesh.vertexCount]; mesh.GetBlendShapeFrameVertices(blink, 9, deltas, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);
                Assert.That(mesh.GetBlendShapeFrameWeight(blink, 9), Is.EqualTo(50));
                var matrices = face.bones.Select((bone, i) => bone.localToWorldMatrix * mesh.bindposes[i]).ToArray();
                float parity = 0, differenceFromLinear = 0; int offset = 0;
                using (var counts = mesh.GetBonesPerVertex()) using (var weights = mesh.GetAllBoneWeights())
                    for (int i = 0; i < mesh.vertexCount; i++)
                    {
                        Vector3 expected = Vector3.zero; for (int j = 0; j < counts[i]; j++) { var w = weights[offset++]; expected += matrices[w.boneIndex].MultiplyVector(deltas[i]) * w.weight; }
                        parity = Mathf.Max(parity, Vector3.Distance(expected, face.transform.TransformVector(half[i] - neutral[i])));
                        differenceFromLinear = Mathf.Max(differenceFromLinear, face.transform.TransformVector(half[i] - Vector3.Lerp(neutral[i], closed[i], .5f)).magnitude);
                    }
                Assert.That(parity, Is.LessThan(.000002f), "Semantic 50% must match the independently skinned existing 50% frame.");
                if (family == CharacterFamily.Girl) Assert.That(differenceFromLinear, Is.GreaterThan(.0005f), "Girl must follow the accepted sampled curve, not half of the endpoint.");
                p.Face.ResetAll(); Assert.That(Bake(face), Is.EqualTo(neutral));
                foreach (var semantic in new[] { FacialSemantic.BlinkBoth, FacialSemantic.SmileLeft, FacialSemantic.SmileRight, FacialSemantic.JawOpen }) Assert.That(p.Face.TrySet(semantic, .5f));
                Assert.That(p.Face.TrySet(FacialSemantic.DimpleRight, .25f));
                Assert.That(p.Face.TrySet(FacialSemantic.DimpleLeft, .25f), Is.EqualTo(family != CharacterFamily.AdultFemale));
                foreach (var entry in p.Definition.FacialBinding.Entries.Where(e => e.Capability == FacialCapability.Rejected))
                { Assert.That(p.Face.TrySet(entry.Semantic, 1), Is.False); Assert.That(face.GetBlendShapeWeight(mesh.GetBlendShapeIndex(entry.RawChannel)), Is.Zero); }
                Bake(face); yield return null; Capture(camera, p, "simultaneous-smile-jaw-blink"); p.Face.ResetAll();
                yield return null; Capture(camera, p, "neutral-full-body", true);
                report.Add(family + ": BlinkBoth 0/25/50/75/100; frames=20; 50% world parity metres=" + parity.ToString("R") + "; difference from linear endpoint metres=" + differenceFromLinear.ToString("R") + "; shared mesh/Avatar/controller/material preserved; Smile/JawOpen coexist; DimpleLeft=" + p.Face.Capability(FacialSemantic.DimpleLeft));
                Object.Destroy(root); yield return null;
            }
            File.WriteAllLines(Evidence + "/licensed-face-results.txt", report);
        }

        [UnityTest] public IEnumerator FourFamiliesWalkRunAndKeepFaceStateWithoutCapsuleGestures()
        {
            var report = new List<string>();
            foreach (var family in Families)
            {
                var root = Spawn(family, new Vector3(-3, GroundY, 0)); var agent = root.GetComponent<EmployeeAgent>(); var p = root.GetComponent<CharacterPresentation>(); var nav = root.GetComponent<NavMeshAgent>();
                yield return null; yield return null; Assert.That(p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
                p.Face.TrySet(FacialSemantic.BlinkBoth, .5f); bool arrived = false;
                Assert.That(NavMesh.SamplePosition(new Vector3(3, GroundY, 0), out var target, .2f, NavMesh.AllAreas));
                Assert.That(agent.TryAssignTaskDestination(target.position, new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Isolated runtime walk"), () => { arrived = true; agent.ClearTaskDestination(); }));
                bool walked = false; var leg = p.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var rest = leg.localRotation; float motion = 0, deadline = Time.realtimeSinceStartup + 25;
                while (!arrived && Time.realtimeSinceStartup < deadline)
                { walked |= p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"); motion = Mathf.Max(motion, Quaternion.Angle(rest, leg.localRotation)); yield return null; }
                Assert.That(arrived && walked); Assert.That(motion, Is.GreaterThan(10)); yield return new WaitForSecondsRealtime(.3f);
                Assert.That(p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
                nav.speed = 3; Assert.That(agent.TryAssignTaskDestination(new Vector3(-3, target.position.y, 0), new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Isolated runtime run"), agent.ClearTaskDestination));
                bool ran = false; deadline = Time.realtimeSinceStartup + 5;
                while (!ran && Time.realtimeSinceStartup < deadline) { ran = p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Run"); yield return null; }
                Assert.That(ran); agent.CancelPresentationMovement();
                Assert.That(p.Visual.Face.GetBlendShapeWeight(p.Visual.Face.sharedMesh.GetBlendShapeIndex("BlinkBoth")), Is.EqualTo(50));
                agent.Employee.SetState(EmployeeState.Filming); yield return null;
                Assert.That(root.transform.Find("PrototypePerformanceGesture"), Is.Null);
                bool beatCompleted = false; var beat = new BeatPerformanceResult("beat", "character", agent.Employee.Id, ScreenplayEmotion.Happy, ScreenplayEmotion.Happy, .5, .5, .5, .5, .5);
                Assert.That(agent.TryBeginBeatPerformance(ScreenplayBeatType.Dialogue, beat, null, .1f, () => beatCompleted = true));
                deadline = Time.realtimeSinceStartup + 3; while (!beatCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(beatCompleted); Assert.That(root.transform.Find("PrototypePerformanceGesture"), Is.Null);
                report.Add(family + ": Idle/Walk/arrival/Idle/Run; leg motion degrees=" + motion + "; facial state retained; generic and explicit beat capsule geometry suppressed; callback preserved.");
                Object.Destroy(root); yield return null;
            }
            File.WriteAllLines(Evidence + "/licensed-locomotion-results.txt", report);
        }

        [UnityTest] public IEnumerator FourFamiliesUseSelectionCarryPracticeAndEscapeThroughExistingInput()
        {
            var mode = InputSystem.settings.updateMode; var background = InputSystem.settings.backgroundBehavior; var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            var muted = InputSystem.devices.Where(d => d.enabled && (d is Mouse || d is Keyboard)).ToArray(); foreach (var d in muted) InputSystem.DisableDevice(d);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.position = new Vector3(0, GroundY - .1f, 0); floor.transform.localScale = new Vector3(30, .2f, 30);
                var camera = Camera(); camera.tag = "MainCamera"; camera.orthographicSize = 10; camera.transform.SetPositionAndRotation(new Vector3(0, 23, 0), Quaternion.Euler(90, 0, 0));
                var time = Keep(new GameObject("Isolated time")).AddComponent<SimulationTimeDriver>(); time.enabled = false;
                var selection = Keep(new GameObject("Isolated selection")).AddComponent<StudioSelectionController>(); selection.enabled = false;
                var input = selection.PersonInteraction; yield return null; yield return null;
                var spot = Keep(new GameObject("Isolated practice")).AddComponent<PersonInteractionSpot>(); spot.transform.position = new Vector3(3, GroundY, 0);
                spot.Configure("runtime:practice", null, PersonSpotActivity.Practice, ProfessionalRole.Actor, "Practice Comedy");
                void Pointer(Vector3 point, bool down) { InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(point), buttons = (ushort)(down ? 1 : 0) }); InputSystem.Update(); input.HandleInput(); }
                var report = new List<string>();
                foreach (var family in Families)
                {
                    var root = Spawn(family, new Vector3(0, GroundY, 0)); var agent = root.GetComponent<EmployeeAgent>(); agent.enabled = false;
                    var p = root.GetComponent<CharacterPresentation>(); p.Face.TrySet(FacialSemantic.BlinkBoth, .5f); yield return null; yield return null; Physics.SyncTransforms();
                    Pointer(root.transform.position, false); Pointer(root.transform.position, true); Pointer(root.transform.position, false); Assert.That(selection.SelectedAgent, Is.SameAs(agent));
                    var original = root.transform.position;
                    Pointer(root.transform.position, true); yield return new WaitForSecondsRealtime(.22f); Pointer(root.transform.position, true);
                    Assert.That(input.IsHolding && p.IsPosingHeld); Assert.That(root.transform.Find("HeldPersonVisual"), Is.Null);
                    Pointer(new Vector3(14, GroundY, 0), false); Pointer(new Vector3(14, GroundY, 0), true); Assert.That(input.IsHolding, "Outside the NavMesh stays held");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); input.HandleInput();
                    Assert.That(input.IsHolding || p.IsPosingHeld, Is.False); Assert.That(root.transform.position, Is.EqualTo(original));
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                    Pointer(root.transform.position, false); Pointer(root.transform.position, true); yield return new WaitForSecondsRealtime(.22f); Pointer(root.transform.position, true);
                    Assert.That(input.IsHolding); Pointer(spot.transform.position, false); Pointer(spot.transform.position, true);
                    Assert.That(input.IsHolding, Is.False); Assert.That(agent.Employee.CurrentState, Is.EqualTo(EmployeeState.Practicing));
                    yield return new WaitForSecondsRealtime(.2f); Assert.That(p.IsPosingHeld, Is.False);
                    // Picking up cancels Practice through the existing controller; free the shared slot for the next family.
                    Pointer(root.transform.position, false); Pointer(root.transform.position, true); yield return new WaitForSecondsRealtime(.22f); Pointer(root.transform.position, true);
                    Assert.That(input.IsHolding); Pointer(new Vector3(-3, GroundY, 0), false); Pointer(new Vector3(-3, GroundY, 0), true);
                    Assert.That(input.IsHolding, Is.False); yield return new WaitForSecondsRealtime(.35f); Assert.That(p.IsPosingHeld, Is.False);
                    Assert.That(root.GetComponent<CapsuleCollider>().enabled); Assert.That(root.GetComponent<NavMeshAgent>().updatePosition);
                    var drag = PersonDragSession.Begin(agent, null); Assert.That(drag, Is.Not.Null); drag.Follow(new Vector3(0, GroundY, 0));
                    yield return new WaitForSecondsRealtime(.2f); Capture(camera, p, "held-full-body", true, true); drag.Cancel();
                    camera.orthographicSize = 10; camera.transform.SetPositionAndRotation(new Vector3(0, 23, 0), Quaternion.Euler(90, 0, 0));
                    Assert.That(p.Visual.Face.GetBlendShapeWeight(p.Visual.Face.sharedMesh.GetBlendShapeIndex("BlinkBoth")), Is.EqualTo(50));
                    report.Add(family + ": physical selection, hold, invalid drop retained, Escape exact restore, contextual Practice, valid ground drop, measured suspension pivot, collider/navigation restore; no capsule clone.");
                    Object.Destroy(root); yield return null;
                }
                File.WriteAllLines(Evidence + "/licensed-interaction-results.txt", report);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); foreach (var d in muted) if (d.added) InputSystem.EnableDevice(d);
                InputSystem.settings.updateMode = mode; InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }

        [UnityTest] public IEnumerator SharedSocializeWorksForEachFamily()
        {
            var time = Keep(new GameObject("Social time")).AddComponent<SimulationTimeDriver>(); time.enabled = false; var report = new List<string>();
            foreach (var family in Families)
            {
                var a = Spawn(family, new Vector3(-3, GroundY, 0)); var b = Spawn(family, new Vector3(3, GroundY, 0));
                var first = a.GetComponent<EmployeeAgent>(); var second = b.GetComponent<EmployeeAgent>(); yield return null;
                foreach (var agent in new[] { first, second })
                {
                    var wellbeing = agent.Employee.Person.CaptureWellbeing(); wellbeing.Boredom = 60; wellbeing.Mood = 0; agent.Employee.Person.RestoreWellbeing(wellbeing);
                    agent.BindAutonomy(time.Autonomy); agent.BindTimeService(time.TimeService); time.Wellbeing.Register(agent.Employee.Person); time.Autonomy.Register(agent.Employee);
                    var feet = agent.GetComponent<CharacterPresentation>().PhysicalProfile.FeetPoint(agent.transform.position);
                    time.Autonomy.UpdatePosition(agent.Employee, new AutonomyPosition(feet.x, feet.y, feet.z));
                }
                Assert.That(time.Autonomy.EvaluateNow(first.Employee).Activity, Is.EqualTo(PersonAutonomousActivity.Socialize));
                var session = time.Autonomy.Sessions.FindForPerson(first.Employee.Id); Assert.That(session, Is.Not.Null);
                float deadline = Time.realtimeSinceStartup + 20; while (session.State != PersonActivitySessionState.Active && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.State, Is.EqualTo(PersonActivitySessionState.Active)); Assert.That(first.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
                Assert.That(second.Employee.CurrentState, Is.EqualTo(EmployeeState.Socializing));
                float separation = Vector3.Distance(a.transform.position, b.transform.position);
                float occupiedWidth = a.GetComponent<CharacterPresentation>().PhysicalProfile.NavigationRadius + b.GetComponent<CharacterPresentation>().PhysicalProfile.NavigationRadius;
                Assert.That(separation, Is.GreaterThan(occupiedWidth + .1f), "Distinct non-overlapping participants, using their resolved profiles and existing arrival tolerance.");
                report.Add(family + ": two shared-runtime people reached separate existing Socialize positions and activated the domain session; separation=" + separation + "; combined radii=" + occupiedWidth);
                Object.Destroy(a); Object.Destroy(b); yield return null;
            }
            File.WriteAllLines(Evidence + "/licensed-social-results.txt", report);
        }
    }
}
