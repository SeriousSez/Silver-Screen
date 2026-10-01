using System.Linq;
using NUnit.Framework;
using SilverScreen.Editor.EnvironmentArt;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public class StudioServicesLodTests
    {
        static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath);
        [Test] public void ApprovedFullDetailIsRetainedAndLowerLevelsReduceCost()
        {
            var levels=Prefab.GetComponent<LODGroup>().GetLODs();
            Assert.That(levels.Length,Is.EqualTo(3));
            var counts=levels.Select((l,i)=>StudioServicesLodReview.Measure(i,l.renderers,l.screenRelativeTransitionHeight)).ToArray();
            Assert.That(counts[0].triangles,Is.EqualTo(790108),"Approved full-detail assembly");
            Assert.That(counts[1].triangles,Is.LessThan(counts[0].triangles*.65f));
            Assert.That(counts[2].triangles,Is.LessThan(counts[1].triangles*.80f));
            Assert.That(counts[1].renderers,Is.LessThan(counts[0].renderers/2));
            Assert.That(counts[1].materialSlots,Is.LessThan(counts[0].materialSlots/2));
        }
        [Test] public void EveryRendererHasExactlyOneLodOwner()
        {
            var assigned=Prefab.GetComponent<LODGroup>().GetLODs().SelectMany(l=>l.renderers).ToArray();
            Assert.That(assigned,Has.None.Null);Assert.That(assigned.Distinct().Count(),Is.EqualTo(assigned.Length));
            CollectionAssert.AreEquivalent(Prefab.GetComponentsInChildren<Renderer>(true),assigned);
            Assert.That(Prefab.GetComponentsInChildren<LODGroup>(true).Length,Is.EqualTo(1));
        }
        [Test] public void CombinedBatchesKeepOneMaterialPerRenderer()
        {
            foreach (var renderer in Prefab.GetComponent<LODGroup>().GetLODs().Skip(1).SelectMany(l => l.renderers))
            {
                Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(1), "Keep the visually validated material-homogeneous batching boundary");
                Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1));
            }
        }
        [Test] public void DerivedGeometryUsesExistingMaterialsAndValidMeshAttributes()
        {
            var levels=Prefab.GetComponent<LODGroup>().GetLODs();var original=levels[0].renderers.SelectMany(r=>r.sharedMaterials).ToHashSet();
            foreach(var r in levels.Skip(1).SelectMany(l=>l.renderers))
            {
                Assert.That(r.sharedMaterials.All(original.Contains),Is.True,"No duplicated per-LOD materials");
                var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertexCount,Is.GreaterThan(0));Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.subMeshCount,Is.EqualTo(r.sharedMaterials.Length));
                Assert.That(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),Is.True);
            }
        }
        [Test] public void RevealHidesAllRoofAndFacingWallLevelsWithoutAffectingPhysics()
        {
            var go=Object.Instantiate(Prefab);
            try
            {
                var cut=go.GetComponent<BuildingCutawayController>();var colliders=go.GetComponentsInChildren<Collider>(true);var states=colliders.Select(c=>c.enabled).ToArray();
                var retained=go.GetComponentsInChildren<Renderer>(true).Where(r=>!cut.Groups.Any(g=>g.Renderers.Contains(r))).ToArray();
                cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,true,go.transform.position+new Vector3(20,20,-20));
                Assert.That(cut.Groups.Where(g=>g.Roof||g.Id=="FrontWall"||g.Id=="RightWall").SelectMany(g=>g.Renderers).All(r=>r.forceRenderingOff),Is.True);
                Assert.That(retained.All(r=>r.enabled),Is.True,"Interior/furniture kept at every level");
                Assert.That(colliders.Select((c,i)=>c.enabled==states[i]).All(v=>v),Is.True);
                cut.Restore();Assert.That(go.GetComponentsInChildren<Renderer>(true).All(r=>r.enabled&&!r.forceRenderingOff),Is.True);
            }
            finally {Object.DestroyImmediate(go);}
        }
        [Test] public void LodsDoNotContainDuplicateGameplayOrCollision()
        {
            var node=Prefab.transform.Find(StudioServicesLodBuilder.GeneratedRoot);
            Assert.That(node,Is.Not.Null);Assert.That(node.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(node.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            Assert.That(Prefab.GetComponentsInChildren<StudioServicesFacility>(true).Length,Is.EqualTo(1));
            Assert.That(Prefab.transform.Find("SemanticAnchors").childCount,Is.EqualTo(19));
        }
        [Test] public void CanonicalLodVariantsShareApprovedBaseMeshes()
        {
            var assets=AssetDatabase.FindAssets("t:Prefab",new[]{StudioServicesLodBuilder.KitRoot+"/Prefabs"});Assert.That(assets.Length,Is.EqualTo(63));
            foreach(var id in assets)
            {
                var variant=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));
                Assert.That(PrefabUtility.GetPrefabAssetType(variant),Is.EqualTo(PrefabAssetType.Variant));
                var original=PrefabUtility.GetCorrespondingObjectFromSource(variant);
                Assert.That(original.GetComponent<LODGroup>(),Is.Null,"Canonical prefab stays unchanged");
                var level=variant.GetComponent<LODGroup>().GetLODs()[0];
                CollectionAssert.AreEquivalent(original.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh),level.renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh));
            }
        }
        [Test] public void LowerLevelsPreserveArchitecturalEnvelope()
        {
            var go=Object.Instantiate(Prefab);
            try
            {
                Bounds Bounds(Renderer[] rs){var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
                var levels=go.GetComponent<LODGroup>().GetLODs();var full=Bounds(levels[0].renderers);
                foreach(var l in levels.Skip(1))
                {
                    var b=Bounds(l.renderers);Assert.That(Vector3.Distance(full.center,b.center),Is.LessThan(.10f));
                    Assert.That(Vector3.Distance(full.size,b.size),Is.LessThan(.20f));
                }
            }
            finally {Object.DestroyImmediate(go);}
        }
        [Test] public void AutomaticTransitionsUseCrossFadeAndKeepBuildingUntilVeryDistant()
        {
            var group=Prefab.GetComponent<LODGroup>();var levels=group.GetLODs();
            Assert.That(group.fadeMode,Is.EqualTo(LODFadeMode.CrossFade));Assert.That(group.animateCrossFading,Is.False);
            Assert.That(levels[0].screenRelativeTransitionHeight,Is.GreaterThan(levels[1].screenRelativeTransitionHeight));
            Assert.That(levels[1].screenRelativeTransitionHeight,Is.GreaterThan(levels[2].screenRelativeTransitionHeight));
            Assert.That(levels[2].screenRelativeTransitionHeight,Is.LessThan(.01f));
            Assert.That(group.size,Is.InRange(5f,6f),"Reference uses architectural height; mesh culling bounds remain exact");
        }
    }
}
