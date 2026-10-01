using System;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Editor;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class Stage1PresentationTests
    {
        [TestCase(0f)] [TestCase(37f)]
        public void InteriorReflectionInfluenceExcludesRoofAndRetainsOccupiedInterior(float yaw)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.PrefabPath), scene);
                root.transform.SetPositionAndRotation(new Vector3(41,0,-26), Quaternion.Euler(0,yaw,0));
                var probe = root.GetComponentsInChildren<ReflectionProbe>(true).Single(p => p.name == "InteriorReflection");
                Assert.That(probe.enabled, Is.True);
                Assert.That(probe.customBakedTexture, Is.Not.Null);
                foreach (float height in new[] { .08f, 5f })
                    Assert.That(probe.bounds.Contains(root.transform.TransformPoint(new Vector3(0,height,0))), Is.True, "Interior coverage");
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.enabled && r.name.StartsWith("Export_Roof", StringComparison.Ordinal)))
                {
                    Assert.That(renderer.reflectionProbeUsage, Is.EqualTo(ReflectionProbeUsage.Off));
                    foreach (var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)
                        Assert.That(renderer.transform.TransformPoint(vertex).y, Is.GreaterThan(probe.bounds.max.y + .04f), renderer.name);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(0f)] [TestCase(37f)] [TestCase(90f)] [TestCase(143f)]
        public void NearTrussesFadePartiallyAndFarTrussesRestoreWhenCameraChangesSide(float yaw)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.PrefabPath), scene);
                root.transform.SetPositionAndRotation(new Vector3(41,0,-26), Quaternion.Euler(0,yaw,0));
                var cut = root.GetComponent<BuildingCutawayController>();
                Assert.That(cut.Groups.Count(g => g.Mode == BuildingCutawayMode.StructuralOccluder), Is.EqualTo(5));
                void Check(int index, float alpha)
                {
                    foreach (string prefix in new[] { "TrussBand", "TrussConnections", "TrussHangers" })
                    {
                        var group = cut.Groups.Single(g => g.Id == prefix + index);
                        foreach (var renderer in group.Renderers)
                        {
                            Assert.That(renderer.forceRenderingOff, Is.False, group.Id);
                            foreach (var material in renderer.sharedMaterials)
                                Assert.That(material.HasProperty("_CutawayOpacity") ? material.GetFloat("_CutawayOpacity") : 1,
                                    Is.EqualTo(alpha).Within(.001f), group.Id);
                        }
                    }
                }
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, root.transform.TransformPoint(new Vector3(0,18,-28)));
                Check(0,.22f); Check(1,.22f); Check(2,1); Check(3,1); Check(4,1);
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, root.transform.TransformPoint(new Vector3(0,18,28)));
                Check(0,1); Check(1,1); Check(2,1); Check(3,.22f); Check(4,.22f);
                // At floor level none of the overhead bands is an obstruction to the gameplay area.
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, root.transform.TransformPoint(new Vector3(0,2,-15)));
                for (int i = 0; i < 5; i++) Check(i,1);
                Assert.That(cut.Groups.Single(g => g.Id == "RetainedInterior").Renderers.All(r => !r.forceRenderingOff), Is.True);
                cut.Restore();
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test] public void StructuralCoverageHasADeadbandForSmallCameraMovements()
        {
            var group = new BuildingCutawayController.VisibilityGroup {
                LocalSection = new Bounds(new Vector3(0,5,0), Vector3.one * .1f),
                StructuralFocus = new Bounds(Vector3.zero, new Vector3(4,0,4)),
                StructuralEnterCoverage = .10f, StructuralExitCoverage = .015f
            };
            foreach (float x in new[] { -.005f, 0, .005f })
            {
                Assert.That(BuildingCutawayController.StructuralObstructs(group,new Vector3(x,10,0),false), Is.False);
                Assert.That(BuildingCutawayController.StructuralObstructs(group,new Vector3(x,10,0),true), Is.True);
            }
            Assert.That(BuildingCutawayController.StructuralObstructs(group,new Vector3(10,10,10),true), Is.False);
        }

        [Test] public void PartialOpacityPropagatesToNestedLightsAndInteractionWithoutChangingPhysics()
        {
            var root = new GameObject("Building");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", Color.white);
            try
            {
                var lamp = new GameObject("Lamp"); lamp.transform.SetParent(root.transform);
                var renderer = lamp.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                int originalQueue = material.renderQueue;
                float originalSurface = material.GetFloat("_Surface"), originalZWrite = material.GetFloat("_ZWrite");
                bool originalShadowPass = material.GetShaderPassEnabled("ShadowCaster");
                var light = lamp.AddComponent<Light>(); light.intensity = 10;
                var collider = lamp.AddComponent<BoxCollider>();
                var cut = root.AddComponent<BuildingCutawayController>();
                cut.Configure(new[] {
                    new BuildingCutawayController.VisibilityGroup { Id="Lamp", SupportGroupId="Bracket", Renderers=new Renderer[]{renderer}, Lights=new[]{light}, Occluders=new Collider[]{collider} },
                    new BuildingCutawayController.VisibilityGroup { Id="Bracket", SupportGroupId="Beam" },
                    new BuildingCutawayController.VisibilityGroup { Id="Beam", Mode=BuildingCutawayMode.StructuralOccluder, CutawayOpacity=.22f,
                        LocalSection=new Bounds(new Vector3(0,5,0),new Vector3(5,1,5)), StructuralFocus=new Bounds(Vector3.zero,new Vector3(2,0,2)) }
                }, new Bounds(Vector3.zero,Vector3.one));
                var observer = Vector3.up * 10;
                cut.SetReveal(BuildingRevealReason.BuildingFocus,true,observer);
                cut.SetReveal(BuildingRevealReason.ProductionView,true,observer);
                var cachedVariant = renderer.sharedMaterial;
                cut.SetReveal(BuildingRevealReason.BuildingFocus,false,observer);
                Assert.That(renderer.sharedMaterial, Is.SameAs(cachedVariant));
                Assert.That(cachedVariant.GetFloat("_CutawayOpacity"), Is.EqualTo(.22f).Within(.001));
                Assert.That(cachedVariant.shader.name, Is.EqualTo("SilverScreen/Cutaway Lit"));
                Assert.That(cachedVariant.renderQueue, Is.EqualTo(originalQueue));
                Assert.That(cachedVariant.GetFloat("_Surface"), Is.EqualTo(originalSurface));
                Assert.That(cachedVariant.GetFloat("_ZWrite"), Is.EqualTo(originalZWrite));
                Assert.That(cachedVariant.GetShaderPassEnabled("ShadowCaster"), Is.EqualTo(originalShadowPass));
                Assert.That(renderer.forceRenderingOff, Is.False);
                Assert.That(light.intensity, Is.EqualTo(2.2f).Within(.001));
                Assert.That(cut.IsHiddenOccluder(collider), Is.True); Assert.That(collider.enabled, Is.True);
                cut.SetReveal(BuildingRevealReason.ProductionView,false,observer);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material)); Assert.That(light.intensity, Is.EqualTo(10));
                Assert.That(cut.IsHiddenOccluder(collider), Is.False); Assert.That(collider.enabled, Is.True);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }

        [TestCase(0f)] [TestCase(37f)] [TestCase(90f)] [TestCase(143f)]
        public void AuthoredStageCutawayIsLocalAndRetainsStructureAndCollision(float yaw)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.PrefabPath), scene);
                root.transform.SetPositionAndRotation(new Vector3(41,0,-26), Quaternion.Euler(0,yaw,0));
                var cut = root.GetComponent<BuildingCutawayController>(); Assert.That(cut, Is.Not.Null);
                var colliders = root.GetComponentsInChildren<Collider>(true);
                var enabled = colliders.Select(c => c.enabled).ToArray();
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, root.transform.TransformPoint(new Vector3(25,17,-30)));
                foreach (string id in new[] { "FrontShell", "RightShell", "Barrel2", "StageDoorLeft", "StageDoorRight", "StageLettering", "RightShellLamps" })
                    Assert.That(cut.Groups.Single(g => g.Id == id).Renderers.All(r => r.forceRenderingOff), Is.True, id);
                foreach (string id in new[] { "RearShell", "LeftShell", "Barrel0", "RetainedInterior" })
                    Assert.That(cut.Groups.Single(g => g.Id == id).Renderers.All(r => !r.forceRenderingOff), Is.True, id);
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, root.transform.TransformPoint(new Vector3(-25,17,30)));
                Assert.That(cut.Groups.Single(g => g.Id == "FrontShell").Renderers.All(r => !r.forceRenderingOff), Is.True);
                Assert.That(cut.Groups.Single(g => g.Id == "RearShell").Renderers.All(r => r.forceRenderingOff), Is.True);
                Assert.That(colliders.Select(c => c.enabled), Is.EqualTo(enabled));
                cut.Restore();
                Assert.That(cut.Groups.SelectMany(g => g.Renderers).All(r => !r.forceRenderingOff), Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test] public void AuthoredSectionsAreCompleteAndDependenciesAreAcyclic()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1LiveIntegrationBuilder.PrefabPath);
            var cut = root.GetComponent<BuildingCutawayController>();
            var ids = cut.Groups.ToDictionary(g => g.Id);
            var all = cut.Groups.SelectMany(g => g.Renderers).ToArray();
            Assert.That(all.All(r => r != null), Is.True);
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length), "One fade owner per renderer");
            foreach (var group in cut.Groups)
            {
                var visited = new System.Collections.Generic.HashSet<string>(); var current = group;
                while (!string.IsNullOrEmpty(current.SupportGroupId))
                {
                    Assert.That(visited.Add(current.Id), Is.True, "Dependency cycle: " + group.Id);
                    Assert.That(ids.ContainsKey(current.SupportGroupId), Is.True, "Missing support: " + group.Id);
                    current = ids[current.SupportGroupId];
                }
            }
            Assert.That(cut.Groups.SelectMany(g => g.Lights).Count(), Is.EqualTo(15));
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var sections = renderer.transform.Find("AuthoredRevealSections");
                if (sections == null) continue;
                Assert.That(renderer.enabled, Is.False);
                var original = renderer.GetComponent<MeshFilter>().sharedMesh;
                long count = 0;
                foreach (var filter in sections.GetComponentsInChildren<MeshFilter>())
                    for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++) count += filter.sharedMesh.GetIndexCount(sub);
                long originalCount = 0;
                for (int sub = 0; sub < original.subMeshCount; sub++) originalCount += original.GetIndexCount(sub);
                Assert.That(count, Is.EqualTo(originalCount), renderer.name + " triangles lost or duplicated");
            }
            Assert.That(root.GetComponentsInChildren<LODGroup>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("Export_Roof", StringComparison.Ordinal))
                .All(r => r.reflectionProbeUsage == ReflectionProbeUsage.Off), Is.True);
            var construction = new SerializedObject(root.GetComponent<ConstructionPhaseVisuals>()).FindProperty("_groups");
            int boundCount = 0;
            for (int i = 0; i < construction.arraySize; i++) boundCount += construction.GetArrayElementAtIndex(i).FindPropertyRelative("Renderers").arraySize;
            Assert.That(boundCount, Is.EqualTo(root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.transform.parent.name == "AuthoredRevealSections")));
        }

        [Test] public void NestedSupportsAndLightsFollowAllRevealOwnersThenRestore()
        {
            var root = new GameObject("Building");
            try
            {
                var lamp = new GameObject("Lamp"); lamp.transform.SetParent(root.transform);
                var renderer = lamp.AddComponent<MeshRenderer>(); var light = lamp.AddComponent<Light>(); light.intensity = 7;
                var cut = root.AddComponent<BuildingCutawayController>();
                cut.Configure(new[] {
                    new BuildingCutawayController.VisibilityGroup { Id="Lamp", SupportGroupId="Bracket", Renderers=new Renderer[]{renderer}, Lights=new[]{light} },
                    new BuildingCutawayController.VisibilityGroup { Id="Bracket", SupportGroupId="Wall" },
                    new BuildingCutawayController.VisibilityGroup { Id="Wall", OutwardNormal=Vector3.right }
                }, new Bounds(Vector3.zero, Vector3.one));
                cut.SetReveal(BuildingRevealReason.BuildingFocus, true, Vector3.right * 10);
                cut.SetReveal(BuildingRevealReason.ProductionView, true, Vector3.right * 10);
                cut.SetReveal(BuildingRevealReason.BuildingFocus, false, Vector3.right * 10);
                Assert.That(renderer.forceRenderingOff, Is.True); Assert.That(light.intensity, Is.Zero);
                cut.SetReveal(BuildingRevealReason.ProductionView, false, Vector3.right * 10);
                Assert.That(renderer.forceRenderingOff, Is.False); Assert.That(light.intensity, Is.EqualTo(7));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
