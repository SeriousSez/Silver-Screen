using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using SilverScreen.Editor;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using Unity.AI.Navigation;

namespace SilverScreen.Tests.EditMode
{
    public sealed class Stage1LiveIntegrationTests
    {
        private GameObject _root;
        private Scene _preview;
        [SetUp] public void SetUp()
        {
            _preview = EditorSceneManager.NewPreviewScene();
            _root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.PrefabPath), _preview);
        }
        [TearDown] public void TearDown() { if (_preview.IsValid()) EditorSceneManager.ClosePreviewScene(_preview); }

        [Test] public void LiveIdentityIsUniqueAndReviewIdentityIsPreserved()
        {
            Assert.That(PrefabUtility.GetPrefabAssetType(_root), Is.EqualTo(PrefabAssetType.Variant));
            var ids = _root.GetComponentsInChildren<SoundStageIdentity>(true);
            Assert.That(ids.Length, Is.EqualTo(1)); Assert.That(ids[0].FacilityId, Is.EqualTo("stage-1")); Assert.That(ids[0].StageNumber, Is.EqualTo(1));
            var master = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.Master);
            Assert.That(master.GetComponent<SoundStageIdentity>().FacilityId, Is.EqualTo("review-candidate-stage-1"));
            Assert.That(master.GetComponent<StudioBuildingView>(), Is.Null);
            Assert.That(_root.GetComponent<StudioBuildingView>().BuildingType, Is.EqualTo(BuildingType.SoundStage));
        }
        [Test] public void ProductionPosesAreInsideAndFaceTheCamera()
        {
            Assert.That(_root.GetComponent<AuthoredProductionLayout>().IsComplete(), Is.True);
            foreach (var t in _root.transform.Find("ProductionAnchors").Cast<Transform>())
            { Assert.That(Mathf.Abs(t.localPosition.x), Is.LessThan(6)); Assert.That(t.localPosition.z, Is.InRange(-8, 8)); Assert.That(t.localPosition.y, Is.EqualTo(.08f).Within(.001)); }
            Assert.That(_root.GetComponent<ActorSceneMarkLayout>().TryGetPose(ActorSceneMarkType.ActorMarkA, out var pose), Is.True);
            Assert.That(Vector3.Dot(pose.rotation * Vector3.forward, Vector3.back), Is.GreaterThan(.999));
        }
        [Test] public void SelectionDoesNotObstructTheFilmingFloor()
        {
            var selection = _root.transform.Find("SelectionOnly");
            Assert.That(selection.GetComponent<NavMeshModifier>().ignoreFromBuild, Is.True);
            Assert.That(selection.GetComponentsInChildren<Collider>().All(c => c.isTrigger), Is.True);
            Assert.That(_root.GetComponentsInChildren<MeshCollider>().Where(c => c.name.StartsWith("Export_")).All(c => !c.enabled), Is.True);
        }
        [Test] public void OccupiedStationsHaveIndependentCapsuleClearance()
        {
            var stations = _root.GetComponent<ProductionStationLayout>();
            var points = new List<Vector3>();
            foreach (ProductionStationType type in Enum.GetValues(typeof(ProductionStationType)))
            {
                Assert.That(stations.TryGetPosition(type, out var point), Is.True);
                foreach (var other in points)
                    Assert.That(Vector3.Distance(point, other), Is.GreaterThan(EmployeeNavigationProfile.Radius * 2 + .3f), type.ToString());
                points.Add(point);
            }
        }
        [Test] public void DoorAndThresholdFailClosedWhenPoseIsInvalid()
        {
            var access = _root.GetComponent<HeldOpenStageAccess>(); Assert.That(access.IsPassable, Is.True);
            var blocker = _root.GetComponentInChildren<NavMeshObstacle>();
            Assert.That(blocker, Is.Not.Null); Assert.That(blocker.carving, Is.True);
            Assert.That(blocker.enabled, Is.False);
            Assert.That(_root.GetComponentInChildren<NavMeshLink>(), Is.Null, "The low apron must retain ordinary walking/avoidance");
            _root.transform.Find("InteriorNavigation/FrontRightPersonnelHinge").localRotation = Quaternion.identity;
            Assert.That(access.IsPassable, Is.False);
            // Preview scenes do not support manually dispatching Unity lifecycle
            // messages. Re-evaluate through the existing authoring API instead.
            access.Configure(_root.transform.Find("InteriorNavigation/FrontRightPersonnelHinge"), blocker);
            Assert.That(blocker.enabled, Is.True);
            _root.transform.Find("InteriorNavigation/FrontRightPersonnelHinge").localRotation = Quaternion.Euler(0,95,0);
            access.Configure(_root.transform.Find("InteriorNavigation/FrontRightPersonnelHinge"), blocker);
            Assert.That(blocker.enabled, Is.False);
            access.enabled = false; Assert.That(blocker.enabled, Is.True);
        }
        [Test] public void CameraTreatsFeetAndCentrePivotsEquivalently()
        {
            var layout = _root.GetComponent<ProductionCameraLayout>();
            var real = new GameObject("Real"); SceneManager.MoveGameObjectToScene(real, _preview); real.SetActive(false);
            var nav = real.AddComponent<NavMeshAgent>(); nav.baseOffset = 1; real.transform.position = _root.transform.TransformPoint(new Vector3(0,1.08f,1.5f));
            var proxy = new GameObject("Playback"); SceneManager.MoveGameObjectToScene(proxy, _preview); proxy.transform.position = _root.transform.TransformPoint(new Vector3(0,.08f,1.5f));
            foreach (var shot in new[] { ShotType.Wide, ShotType.Medium, ShotType.CloseUp })
            {
                Assert.That(layout.TryFrame(shot, real.transform, out var a, out var af, out var av), Is.True);
                Assert.That(layout.TryFrame(shot, proxy.transform, out var b, out var bf, out var bv), Is.True);
                Assert.That(Vector3.Distance(a,b), Is.LessThan(.001)); Assert.That(Vector3.Distance(af,bf), Is.LessThan(.001)); Assert.That(av, Is.EqualTo(bv));
                Assert.That(_root.transform.InverseTransformPoint(a).z, Is.InRange(-10,8)); Assert.That(a.y, Is.LessThan(4.5));
            }
        }
        [Test] public void HumanCapsuleFitsBodyWithMarginWithoutChangingHeight()
        {
            var body = AssetDatabase.LoadAssetAtPath<Mesh>(Stage1LiveIntegrationBuilder.BodyPath);
            Assert.That(body.bounds.size.x, Is.EqualTo(.6f).Within(.001)); Assert.That(body.bounds.size.z, Is.EqualTo(.6f).Within(.001));
            Assert.That(body.bounds.size.y, Is.EqualTo(2).Within(.001));
            Assert.That(EmployeeNavigationProfile.Radius*2-body.bounds.size.x, Is.EqualTo(.1f).Within(.001));
            Assert.That(NavMesh.GetSettingsByID(0).agentRadius, Is.EqualTo(EmployeeNavigationProfile.Radius).Within(.001));
        }
        [Test] public void ClosedDoorDerivativeRetainsImportedGeometryUvsAndMaterials()
        {
            var hinge = _root.transform.Find("InteriorNavigation/FrontRightPersonnelHinge");
            Assert.That(Vector3.Distance(hinge.localPosition, new Vector3(5.138f, 0, -12.27f)), Is.LessThan(.00001f), "Approved hinge pivot");
            Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.Euler(0, 95, 0)), Is.LessThan(.001f), "Approved held-open pose");
            hinge.localRotation = Quaternion.identity;
            var original = _root.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "Export_PersonnelDoors");
            var derived = _root.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "LivePersonnelRemainder" || f.name == "LiveFrontRightLeaf").ToArray();
            Assert.That(derived.Length, Is.EqualTo(2));
            Assert.That(derived.Single(f => f.name == "LiveFrontRightLeaf").transform.parent, Is.SameAs(hinge));
            foreach (var filter in derived)
                Assert.That(filter.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(filter.sharedMesh), "Collision must follow approved geometry");

            // Only depth changed in the approved 55 mm-core derivative. Keep checking
            // every triangle's width/height, UVs and actual material asset identity.
            CollectionAssert.AreEqual(Fingerprint(new[]{original}, ignoreDepth:true), Fingerprint(derived, ignoreDepth:true));
            var unaffected = Fingerprint(new[]{original}, unaffectedOnly:true);
            Assert.That(unaffected.Count, Is.GreaterThan(40000));
            CollectionAssert.AreEqual(unaffected, Fingerprint(derived, unaffectedOnly:true), "Other personnel doors must remain exactly unchanged");

            var approved = Fingerprint(derived);
            Assert.That(approved.Count, Is.EqualTo(51912));
            // Frozen after independent source-to-FBX audit of the visually approved
            // 2026-09-27 thickness correction. Includes all local XYZ, normals, UVs,
            // triangle/material GUID assignments and closed-pose hardware positions.
            // Do not regenerate this reference from a failing asset automatically.
            Assert.That(FingerprintHash(approved), Is.EqualTo("fb42d59e75d0ed7f0234d4fed7ac57df286aac9f0a639a3828bfb559a2a133ca"), "Approved thin-door geometry/reference mismatch");
        }
        private static string FingerprintHash(List<string> records)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", records)))).Replace("-", "").ToLowerInvariant();
        }
        private List<string> Fingerprint(IEnumerable<MeshFilter> filters, bool ignoreDepth = false, bool unaffectedOnly = false)
        {
            var records = new List<string>();
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh; var vertices = mesh.vertices; var uv = mesh.uv; var normals = mesh.normals;
                var materials = filter.GetComponent<Renderer>().sharedMaterials;
                var materialGuids = materials.Select(material => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material))).ToArray();
                Assert.That(materialGuids.All(guid => !string.IsNullOrEmpty(guid)), Is.True, "Persistent material references are required");
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var indices = mesh.GetTriangles(s);
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        var corners = new string[3];
                        bool atApprovedOpening = false;
                        for (int c=0;c<3;c++)
                        {
                            int i=indices[t+c];var p=_root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                            var n=_root.transform.InverseTransformDirection(filter.transform.TransformDirection(normals[i]));var u=uv[i];
                            atApprovedOpening |= p.x > 4.8f && p.x < 6.6f && p.z > -12.8f && p.z < -11.5f;
                            var values=ignoreDepth ? new[]{p.x,p.y,u.x,u.y} : new[]{p.x,p.y,p.z,u.x,u.y,n.x,n.y,n.z};
                            corners[c]=string.Join(",",values.Select(v=>Mathf.RoundToInt(v*10000).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                        }
                        if(unaffectedOnly && atApprovedOpening) continue;
                        Array.Sort(corners,StringComparer.Ordinal);records.Add(materialGuids[s]+"|"+string.Join("|",corners));
                    }
                }
            }
            records.Sort(StringComparer.Ordinal);return records;
        }
    }

    public static class Stage1LiveValidation
    {
        public static string Output => Path.Combine(Path.GetTempPath(), "SilverScreenStage1Live");
        [MenuItem("SilverScreen/Stage 1 Live/3 Inspect authoring and routes")]
        public static void Inspect()
        {
            var stage = UnityEngine.Object.FindObjectsByType<SoundStageIdentity>().Single();
            var root = stage.transform; var report = new System.Text.StringBuilder();
            report.AppendLine("ID="+stage.FacilityId+" number="+stage.StageNumber+" position="+root.position+" complete="+stage.GetComponent<AuthoredProductionLayout>().IsComplete());
            foreach(var surface in UnityEngine.Object.FindObjectsByType<NavMeshSurface>())report.AppendLine("NAV "+surface.name+" radius="+surface.GetBuildSettings().agentRadius+" asset="+AssetDatabase.GetAssetPath(surface.navMeshData));
            foreach(var n in new[]{new Vector3(5.85f,.08f,-12.60f),new Vector3(5.82f,.08f,-11.05f),new Vector3(5.7f,.08f,-10.7f),new Vector3(0,.08f,1.5f)})
            {bool ok=NavMesh.SamplePosition(root.TransformPoint(n),out var hit,.3f,NavMesh.AllAreas);report.AppendLine("SAMPLE "+n+" "+ok+" -> "+root.InverseTransformPoint(hit.position));}
            foreach(var e in UnityEngine.Object.FindObjectsByType<EmployeeAgent>())
            {var path=new NavMeshPath();bool ok=NavMesh.CalculatePath(e.transform.position,stage.GetComponent<StudioBuildingView>().InteractionPosition,NavMesh.AllAreas,path);report.AppendLine("PATH "+e.name+" "+ok+" "+path.status+" "+string.Join(";",path.corners.Select(c=>root.InverseTransformPoint(c).ToString("F3"))));}
            File.WriteAllText(Path.Combine(Output,"navigation-initial.txt"),report.ToString());Debug.Log(report.ToString());
        }
        [MenuItem("SilverScreen/Stage 1 Live/4 Save reviewed Studio integration")]
        public static void SaveStudio()
        {var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Studio.unity"||Application.isPlaying)throw new InvalidOperationException();EditorSceneManager.SaveScene(scene);}
        [MenuItem("SilverScreen/Stage 1 Live/5 Run integration EditMode tests")]
        public static void RunTests()
        {
            var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results());
            api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{typeof(Stage1LiveIntegrationTests).FullName}}));
        }
        public sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor t){} public void TestStarted(ITestAdaptor t){} public void TestFinished(ITestResultAdaptor t){}
            public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,Path.Combine(Output,"integration-editmode.xml"));File.WriteAllText(Path.Combine(Output,"integration-editmode-summary.txt"),"Passed="+r.PassCount+" Failed="+r.FailCount+" Skipped="+r.SkipCount);}
        }
    }
}
