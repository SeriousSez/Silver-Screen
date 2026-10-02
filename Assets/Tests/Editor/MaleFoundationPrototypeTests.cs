using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Editor.Characters;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class MaleFoundationPrototypeTests
    {
        private const string Review = MaleFoundationPrototypeTools.Review;
        private const string SceneKey = "SilverScreen.MaleFoundation.PreviousScene";
        private List<GameObject> _objects;
        private bool _background;

        [Test] public void NativeBaldSubsetPreservesMaleData()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(MaleFoundationPrototypeTools.Model).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MaleFoundationPrototypeTools.BaldMesh);
            var keep = File.ReadAllLines(Review+"/unity-original-indices.txt").Select(int.Parse).ToArray();
            Assert.That(mesh.vertexCount, Is.EqualTo(17738)); Assert.That(mesh.triangles.Length/3, Is.EqualTo(30574));
            var positions=source.vertices;var normals=source.normals;var tangents=source.tangents;
            CollectionAssert.AreEqual(keep.Select(i=>positions[i]),mesh.vertices);
            CollectionAssert.AreEqual(keep.Select(i=>normals[i]),mesh.normals);
            CollectionAssert.AreEqual(keep.Select(i=>tangents[i]),mesh.tangents);
            CollectionAssert.AreEqual(source.bindposes,mesh.bindposes);
            for(int c=0;c<8;c++)
            {
                var a=new List<Vector4>();var b=new List<Vector4>();source.GetUVs(c,a);mesh.GetUVs(c,b);
                if(a.Count==0)Assert.That(b,Is.Empty);else CollectionAssert.AreEqual(keep.Select(i=>a[i]),b);
            }
            using(var counts=source.GetBonesPerVertex()) using(var actual=mesh.GetBonesPerVertex())
            using(var weights=source.GetAllBoneWeights()) using(var actualWeights=mesh.GetAllBoneWeights())
            {
                CollectionAssert.AreEqual(keep.Select(i=>counts[i]),actual.ToArray());
                var offsets=new int[source.vertexCount];int offset=0;
                for(int i=0;i<offsets.Length;i++){offsets[i]=offset;offset+=counts[i];}
                CollectionAssert.AreEqual(keep.SelectMany(i=>Enumerable.Range(offsets[i],counts[i]).Select(j=>weights[j])),actualWeights.ToArray());
            }
            var remap=Enumerable.Repeat(-1,source.vertexCount).ToArray();for(int i=0;i<keep.Length;i++)remap[keep[i]]=i;
            var triangles=source.triangles;var expected=new List<int>();
            for(int i=0;i<triangles.Length;i+=3)if(remap[triangles[i]]>=0)expected.AddRange(new[]{remap[triangles[i]],remap[triangles[i+1]],remap[triangles[i+2]]});
            CollectionAssert.AreEqual(expected,mesh.triangles);
            Assert.That(mesh.blendShapeCount,Is.EqualTo(source.blendShapeCount));
            var sv=new Vector3[source.vertexCount];var sn=new Vector3[sv.Length];var st=new Vector3[sv.Length];
            var v=new Vector3[mesh.vertexCount];var n=new Vector3[v.Length];var t=new Vector3[v.Length];
            for(int s=0;s<mesh.blendShapeCount;s++)
            {
                Assert.That(mesh.GetBlendShapeName(s),Is.EqualTo(source.GetBlendShapeName(s)));
                Assert.That(mesh.GetBlendShapeFrameCount(s),Is.EqualTo(source.GetBlendShapeFrameCount(s)));
                for(int f=0;f<mesh.GetBlendShapeFrameCount(s);f++)
                {
                    Assert.That(mesh.GetBlendShapeFrameWeight(s,f),Is.EqualTo(source.GetBlendShapeFrameWeight(s,f)));
                    source.GetBlendShapeFrameVertices(s,f,sv,sn,st);mesh.GetBlendShapeFrameVertices(s,f,v,n,t);
                    CollectionAssert.AreEqual(keep.Select(i=>sv[i]),v);CollectionAssert.AreEqual(keep.Select(i=>sn[i]),n);CollectionAssert.AreEqual(keep.Select(i=>st[i]),t);
                }
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MaleFoundationPrototypeTools.Prefab);
            var visual=prefab.GetComponent<HumanBasePrototypeVisual>();
            Assert.That(visual.Animator.avatar.isValid&&visual.Animator.avatar.isHuman,Is.True);
            Assert.That(visual.Face.bones.Length,Is.EqualTo(51));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.EqualTo(1));
            File.WriteAllText(Review+"/bald-native-preservation.txt","Exact retained vertices, topology, UVs, normals, tangents, all weights, bindposes and all 29 supplied BlendShape frames. Valid Male Humanoid, 51 bones, one active skin.");
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            if(!TestContext.CurrentContext.Test.Name.StartsWith("Runtime"))yield break;
            Assert.That(SceneManager.sceneCount,Is.EqualTo(1));Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            SessionState.SetString(SceneKey,SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            _background=Application.runInBackground;Application.runInBackground=true;_objects=new List<GameObject>();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(_objects!=null)foreach(var go in _objects.AsEnumerable().Reverse())if(go!=null)Object.DestroyImmediate(go);
            if(Application.isPlaying){Application.runInBackground=_background;yield return new ExitPlayMode();}
            var path=SessionState.GetString(SceneKey,"");SessionState.EraseString(SceneKey);
            if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
        }
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        private Camera Camera()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.38f,.38f);
            var light=Keep(new GameObject("Technical key")).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(25,-35,0);
            var fill=Keep(new GameObject("Technical fill")).AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.4f;fill.transform.rotation=Quaternion.Euler(10,135,0);
            var camera=Keep(new GameObject("Male technical camera")).AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.17f,.21f);
            camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=10;return camera;
        }
        private static Vector3[] Bake(SkinnedMeshRenderer skin)
        {
            var mesh=new Mesh();
            try{skin.BakeMesh(mesh,true);Assert.That(mesh.vertices.All(MaleFoundationPrototypeTools.Finite)&&mesh.normals.All(MaleFoundationPrototypeTools.Finite),Is.True);return mesh.vertices.Select(skin.transform.TransformPoint).ToArray();}
            finally{Object.DestroyImmediate(mesh);}
        }
        private static void Capture(Camera camera,string name)
        {
            Directory.CreateDirectory(Review+"/Captures");HumanBasePrototypeTools.Capture(camera,"../MaleFoundation/Unity/Captures/"+name,1000,750);
        }

        [UnityTest] public IEnumerator RuntimeLocomotionAndAllSuppliedShapes()
        {
            var holder=Keep(new GameObject("Inactive construction"));holder.SetActive(false);
            var root=Keep(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MaleFoundationPrototypeTools.Prefab),holder.transform));
            root.GetComponent<NavMeshAgent>().enabled=false;root.GetComponent<EmployeeAgent>().enabled=false;
            var visual=root.GetComponent<HumanBasePrototypeVisual>();visual.enabled=false;
            root.transform.position=Vector3.up;root.transform.SetParent(null,true);
            var camera=Camera();var report=new List<string>();var poses=new List<Vector3[]>();
            foreach(var state in new[]{"Idle","Walk","Run"})foreach(float phase in new[]{.15f,.45f,.75f})
            {
                visual.Animator.enabled=true;visual.Animator.Play(state,0,phase);visual.Animator.Update(0);visual.Animator.enabled=false;
                yield return null;
                var p=Bake(visual.Face);poses.Add(p);var before=root.transform.position;
                report.Add(state+" phase="+phase+" groundMinimumY="+p.Min(v=>v.y)+" root="+before+" head="+visual.Animator.GetBoneTransform(HumanBodyBones.Head).position);
                camera.orthographicSize=1.04f;var target=new Vector3(0,.91f,0);camera.transform.position=target+new Vector3(0,0,3);camera.transform.LookAt(target);
                Capture(camera,state+"-"+Mathf.RoundToInt(phase*100)+"-front");
                camera.transform.position=target+new Vector3(2,0,2);camera.transform.LookAt(target);Capture(camera,state+"-"+Mathf.RoundToInt(phase*100)+"-oblique");
                Assert.That(root.transform.position,Is.EqualTo(before));
            }
            Assert.That(poses[0].Zip(poses[3],Vector3.Distance).Max(),Is.GreaterThan(.03f));
            visual.Animator.enabled=true;visual.Animator.Play("Idle",0,.15f);visual.Animator.Update(0);visual.Animator.enabled=false;yield return null;
            var neutral=Bake(visual.Face);var head=visual.Animator.GetBoneTransform(HumanBodyBones.Head).position;
            camera.orthographicSize=.18f;var face=head+Vector3.up*.085f;camera.transform.position=face+Vector3.forward*.7f;camera.transform.LookAt(face);
            Capture(camera,"Neutral-face");
            for(int s=0;s<visual.Face.sharedMesh.blendShapeCount;s++)
            {
                foreach(float w in new[]{25f,50f,75f,100f}){visual.Face.SetBlendShapeWeight(s,w);yield return null;var p=Bake(visual.Face);Assert.That(p.Zip(neutral,Vector3.Distance).Max(),Is.GreaterThan(1e-5f));}
                Capture(camera,"Shape-"+s.ToString("D2")+"-"+visual.Face.sharedMesh.GetBlendShapeName(s));
                visual.Face.SetBlendShapeWeight(s,0);yield return null;CollectionAssert.AreEqual(neutral,Bake(visual.Face));
            }
            report.Add("All 29 Male shapes finite/nonzero at 25/50/75/100; every reset exactly restores neutral. Image semantics require separate visual review.");
            File.WriteAllLines(Review+"/runtime-body-and-shapes.txt",report);
        }
    }
}
