using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain.ReusableAssets;
using SilverScreen.Editor.ReusableAssets;
using SilverScreen.Presentation.ReusableAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ReusableFixtureTests
    {
        [TestCase(1,2,600,900)]
        [TestCase(2,1,600,900)]
        public void CropRetainsImageAspect(float w,float h,float iw,float ih)
        {
            var layout=ArtworkLayout.Calculate(w,h,iw,ih,ArtworkFit.Crop);
            // Visible window's aspect in source pixels equals aperture aspect.
            Assert.That(iw*layout.U/(ih*layout.V),Is.EqualTo(w/h).Within(.00001));
            Assert.That(layout.OffsetU,Is.EqualTo((1-layout.U)/2).Within(.00001));
            Assert.That(layout.OffsetV,Is.EqualTo((1-layout.V)/2).Within(.00001));
        }
        [TestCase(1,2,600,900)]
        [TestCase(2,1,1000,600)]
        [TestCase(2,1,600,900)]
        public void FitPreservesWholeArtworkAndDoesNotEscapeAperture(float w,float h,float iw,float ih)
        {
            var layout=ArtworkLayout.Calculate(w,h,iw,ih,ArtworkFit.FitWithMat);
            Assert.That(layout.Width/layout.Height,Is.EqualTo(iw/ih).Within(.00001));
            Assert.That(layout.Width,Is.LessThanOrEqualTo(w));Assert.That(layout.Height,Is.LessThanOrEqualTo(h));
            Assert.That(layout.U,Is.EqualTo(1));Assert.That(layout.V,Is.EqualTo(1));
        }
        [Test]
        public void InvalidArtworkDimensionsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>ArtworkLayout.Calculate(0,1,10,10,ArtworkFit.Crop));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ArtworkLayout.Calculate(1,1,float.NaN,10,ArtworkFit.Crop));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ArtworkLayout.Calculate(1,float.PositiveInfinity,10,10,ArtworkFit.Crop));
        }
        [Test]
        public void CatalogAvailabilityBeginsIn1930AndDoesNotExpire()
        {
            var defs=ReusableFixtureBuilder.ReadManifest().assets.Select(r=>AssetDatabase.LoadAssetAtPath<ReusableAssetDefinition>(ReusableFixtureBuilder.Root+"/Definitions/"+r.name+".asset")).ToArray();
            Assert.That(defs,Has.Length.EqualTo(19));Assert.That(defs.All(d=>d!=null),Is.True);
            Assert.That(defs.Select(d=>d.StableId).Distinct().Count(),Is.EqualTo(defs.Length));
            foreach(var d in defs)
            {
                Assert.That(d.CatalogEntry.IsAvailable(1929),Is.False,d.name);
                Assert.That(d.CatalogEntry.IsAvailable(1930),Is.True,d.name);
                Assert.That(d.CatalogEntry.IsAvailable(2090),Is.True,d.name);
                Assert.That(d.Prefab,Is.Not.Null,d.name);
                if(d.name.Contains("Hinge")||d.name.Contains("Lockset")||d.name.Contains("DoorPull")||d.name.Contains("LeverControl"))Assert.That(d.PlayerCatalogItem,Is.False,d.name);
            }
        }
        [Test]
        public void EveryImportedMeshMatchesPacketPositionsNormalsUvsTrianglesAndMaterials()
        {
            var packet=ReusableFixtureBuilder.ReadPacket();int triangleCount=0;
            foreach(var asset in packet.assets)
            foreach(var part in asset.parts)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(ReusableFixtureBuilder.MeshPath(asset.name,part.name));
                Assert.That(mesh,Is.Not.Null,asset.name+"/"+part.name);
                var vs=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;
                Assert.That(vs.Length,Is.EqualTo(part.positions.Length/3));
                for(int i=0;i<vs.Length;i++)
                {
                    Assert.That(Vector3.Distance(vs[i],new Vector3(part.positions[3*i],part.positions[3*i+1],part.positions[3*i+2])),Is.LessThan(.000001),asset.name);
                    Assert.That(Vector3.Distance(ns[i],new Vector3(part.normals[3*i],part.normals[3*i+1],part.normals[3*i+2])),Is.LessThan(.000001),asset.name);
                    Assert.That(Vector2.Distance(uv[i],new Vector2(part.uv[2*i],part.uv[2*i+1])),Is.LessThan(.000001),asset.name);
                }
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath(asset.name));
                var renderer=prefab.transform.Find("Geometry/"+part.name).GetComponent<MeshRenderer>();
                Assert.That(renderer.sharedMaterials.Select(m=>m.name),Is.EqualTo(part.materials));
                for(int s=0;s<part.submeshes.Length;s++) {CollectionAssert.AreEqual(part.submeshes[s].indices,mesh.GetTriangles(s));triangleCount+=part.submeshes[s].indices.Length/3;}
            }
            Assert.That(triangleCount,Is.EqualTo(27160));
        }
        [Test]
        public void PrefabsHaveUnitScaleMountAnchorsAndIndependentBounds()
        {
            foreach(var r in ReusableFixtureBuilder.ReadManifest().assets)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath(r.name));
                Assert.That(prefab.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(prefab.transform.localRotation,Is.EqualTo(Quaternion.identity));
                var mount=prefab.transform.Find("Anchors/Mount");Assert.That(mount,Is.Not.Null);Assert.That(mount.localPosition,Is.EqualTo(Vector3.zero));
                var d=prefab.GetComponent<ReusableFixture>().Definition;
                Assert.That(Vector3.Distance(d.VisibleBounds.size,ReusableFixtureBuilder.V(r.dimensions)),Is.LessThan(.00001));
                Assert.That(prefab.GetComponentsInChildren<MeshCollider>(true),Is.Empty);
                Assert.That(prefab.GetComponentsInChildren<LODGroup>(true),Is.Empty);
                if(r.clearance>0) Assert.That(d.FunctionalClearance.min.z,Is.EqualTo(d.VisibleBounds.max.z).Within(.00001));
                var provenance=AssetDatabase.LoadAssetAtPath<ReusableAssetProvenance>(ReusableFixtureBuilder.Root+"/Editor/Provenance/"+r.name+".asset");
                Assert.That(provenance.MasterCommit,Is.EqualTo("927ad371a96c4044689286473b542febac6a7265"));Assert.That(provenance.Prefab,Is.EqualTo(prefab));
            }
        }
        [Test]
        public void ArtworkIsPerInstanceAndSurvivesDisableEnableWithoutMaterialMutation()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject first=null,second=null;
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath("DisplayFrame_Single_1930"));
                first=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);second=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                var a=first.GetComponent<DisplayFrame>().Slots[0];var b=second.GetComponent<DisplayFrame>().Slots[0];
                var poster=AssetDatabase.LoadAssetAtPath<Texture2D>(ReusableFixtureBuilder.Root+"/Textures/StudioPoster.png");
                var notice=AssetDatabase.LoadAssetAtPath<Texture2D>(ReusableFixtureBuilder.Root+"/Textures/StudioNotice.png");
                var material=a.ArtworkRenderer.sharedMaterial;var oldTexture=material.GetTexture("_BaseMap");var oldColor=material.GetColor("_BaseColor");
                a.SetInsert(InsertRole.PlayerMoviePoster,poster);b.SetInsert(InsertRole.StudioNotice,notice,ArtworkFit.Crop);
                var pa=new MaterialPropertyBlock();var pb=new MaterialPropertyBlock();
                a.ArtworkRenderer.GetPropertyBlock(pa);b.ArtworkRenderer.GetPropertyBlock(pb);
                Assert.That(pa.GetTexture("_BaseMap"),Is.EqualTo(poster));Assert.That(pb.GetTexture("_BaseMap"),Is.EqualTo(notice));
                Assert.That(a.ArtworkRenderer.sharedMaterial,Is.SameAs(b.ArtworkRenderer.sharedMaterial));
                first.SetActive(false);first.SetActive(true);a.ArtworkRenderer.GetPropertyBlock(pa);Assert.That(pa.GetTexture("_BaseMap"),Is.EqualTo(poster));
                a.SetInsert(InsertRole.Photograph,notice);b.ArtworkRenderer.GetPropertyBlock(pb);Assert.That(pb.GetTexture("_BaseMap"),Is.EqualTo(notice));
                a.SetInsert(InsertRole.AcousticFabric);Assert.That(a.ArtworkRenderer.gameObject.activeSelf,Is.False);Assert.That(first.transform.Find("Geometry/Acoustic").gameObject.activeSelf,Is.True);
                a.SetInsert(InsertRole.AdvertisingSignage,null);Assert.That(a.ArtworkRenderer.gameObject.activeSelf,Is.False);
                Assert.That(material.GetTexture("_BaseMap"),Is.EqualTo(oldTexture));Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(oldColor));
                Assert.That(EditorUtility.IsDirty(material),Is.False);
            }
            finally {if(first!=null)Object.DestroyImmediate(first);if(second!=null)Object.DestroyImmediate(second);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void BollardColliderStopsAHorizontalRayButClearanceIsNotCollision()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject obj=null;
            try
            {
                obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath("Bollard_1930")),scene);
                obj.transform.position=new Vector3(1000,0,1000);Physics.SyncTransforms();
                var capsule=obj.GetComponentInChildren<CapsuleCollider>();
                Assert.That(capsule.Raycast(new Ray(new Vector3(1000,.8f,999),Vector3.forward),out var hit,2),Is.True);
                Assert.That(hit.distance,Is.EqualTo(.925f).Within(.002));
                Assert.That(obj.GetComponent<ReusableFixture>().Definition.VisibleBounds.min.y,Is.EqualTo(0).Within(.00001));
            }
            finally {if(obj!=null)Object.DestroyImmediate(obj);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
