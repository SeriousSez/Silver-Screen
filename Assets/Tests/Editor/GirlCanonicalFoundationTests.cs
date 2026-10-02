using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Editor.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class GirlCanonicalFoundationTests
    {
        private const string SceneKey="SilverScreen.GirlCanonical.PreviousScene";
        private readonly List<GameObject> _objects=new List<GameObject>();
        private bool _background;
        private static int[] KeepIndices()=>JsonUtility.FromJson<GirlFoundationPrototypeTools.Partition>(File.ReadAllText(GirlFoundationPrototypeTools.Evidence+"/partition.json")).keep;

        [Test] public void NativeBaldPreservesEveryRetainedBufferAndHumanoid()
        {
            var source=GirlFoundationPrototypeTools.Original.sharedMesh;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(GirlFoundationPrototypeTools.BaldMesh);var keep=KeepIndices();
            Assert.That(mesh.vertexCount,Is.EqualTo(18429));Assert.That(mesh.triangles.Length/3,Is.EqualTo(32330));
            var p=source.vertices;var n=source.normals;var t=source.tangents;var colors=source.colors;
            CollectionAssert.AreEqual(keep.Select(i=>p[i]),mesh.vertices);CollectionAssert.AreEqual(keep.Select(i=>n[i]),mesh.normals);
            CollectionAssert.AreEqual(keep.Select(i=>t[i]),mesh.tangents);CollectionAssert.AreEqual(source.bindposes,mesh.bindposes);
            if(colors.Length>0)CollectionAssert.AreEqual(keep.Select(i=>colors[i]),mesh.colors);
            for(int c=0;c<8;c++)
            {
                var a=new List<Vector4>();var b=new List<Vector4>();source.GetUVs(c,a);mesh.GetUVs(c,b);
                if(a.Count==0)Assert.That(b,Is.Empty);else CollectionAssert.AreEqual(keep.Select(i=>a[i]),b);
            }
            using(var counts=source.GetBonesPerVertex())using(var actual=mesh.GetBonesPerVertex())
            using(var weights=source.GetAllBoneWeights())using(var actualWeights=mesh.GetAllBoneWeights())
            {
                CollectionAssert.AreEqual(keep.Select(i=>counts[i]),actual.ToArray());
                var offsets=new int[source.vertexCount];int offset=0;
                for(int i=0;i<offsets.Length;i++){offsets[i]=offset;offset+=counts[i];}
                CollectionAssert.AreEqual(keep.SelectMany(i=>Enumerable.Range(offsets[i],counts[i]).Select(j=>weights[j])),actualWeights.ToArray());
            }
            var remap=Enumerable.Repeat(-1,source.vertexCount).ToArray();for(int i=0;i<keep.Length;i++)remap[keep[i]]=i;
            Assert.That(mesh.subMeshCount,Is.EqualTo(source.subMeshCount));
            for(int s=0;s<source.subMeshCount;s++)
            {
                var indices=source.GetTriangles(s);var expected=new List<int>();
                for(int i=0;i<indices.Length;i+=3)
                {int count=Enumerable.Range(i,3).Count(j=>remap[indices[j]]>=0);Assert.That(count==0||count==3);if(count==3)expected.AddRange(Enumerable.Range(i,3).Select(j=>remap[indices[j]]));}
                CollectionAssert.AreEqual(expected,mesh.GetTriangles(s));
            }
            Assert.That(mesh.blendShapeCount,Is.EqualTo(29));
            var sv=new Vector3[source.vertexCount];var sn=new Vector3[sv.Length];var st=new Vector3[sv.Length];
            var dv=new Vector3[keep.Length];var dn=new Vector3[dv.Length];var dt=new Vector3[dv.Length];
            for(int s=0;s<29;s++)
            {
                Assert.That(mesh.GetBlendShapeName(s),Is.EqualTo(source.GetBlendShapeName(s)));
                Assert.That(mesh.GetBlendShapeFrameCount(s),Is.EqualTo(source.GetBlendShapeFrameCount(s)));
                for(int f=0;f<source.GetBlendShapeFrameCount(s);f++)
                {
                    Assert.That(mesh.GetBlendShapeFrameWeight(s,f),Is.EqualTo(source.GetBlendShapeFrameWeight(s,f)));
                    source.GetBlendShapeFrameVertices(s,f,sv,sn,st);mesh.GetBlendShapeFrameVertices(s,f,dv,dn,dt);
                    CollectionAssert.AreEqual(keep.Select(i=>sv[i]),dv);CollectionAssert.AreEqual(keep.Select(i=>sn[i]),dn);CollectionAssert.AreEqual(keep.Select(i=>st[i]),dt);
                }
            }
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Prefab("Girl"));
            var bald=AssetDatabase.LoadAssetAtPath<GameObject>(GirlFoundationPrototypeTools.Prefab);
            var animator=bald.GetComponent<Animator>();Assert.That(animator.avatar.isValid&&animator.avatar.isHuman);
            Assert.That(animator.avatar,Is.SameAs(original.GetComponent<Animator>().avatar));
            Assert.That(bald.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length,Is.EqualTo(51));
            CollectionAssert.AreEqual(original.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials,bald.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials);
            var before=original.GetComponentsInChildren<Transform>();var after=bald.GetComponentsInChildren<Transform>();Assert.That(after.Length,Is.EqualTo(before.Length));
            for(int i=0;i<before.Length;i++)
            {Assert.That(after[i].localPosition,Is.EqualTo(before[i].localPosition));Assert.That(after[i].localRotation,Is.EqualTo(before[i].localRotation));Assert.That(after[i].localScale,Is.EqualTo(before[i].localScale));if(i>0)Assert.That(AnimationUtility.CalculateTransformPath(after[i],bald.transform),Is.EqualTo(AnimationUtility.CalculateTransformPath(before[i],original.transform)));}
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            if(!TestContext.CurrentContext.Test.Name.StartsWith("Runtime"))yield break;
            Assert.That(SceneManager.sceneCount,Is.EqualTo(1));Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            SessionState.SetString(SceneKey,SceneManager.GetActiveScene().path);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();_background=Application.runInBackground;Application.runInBackground=true;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            foreach(var go in _objects.AsEnumerable().Reverse())if(go!=null)Object.DestroyImmediate(go);_objects.Clear();
            if(Application.isPlaying){Application.runInBackground=_background;yield return new ExitPlayMode();}
            var path=SessionState.GetString(SceneKey,"");SessionState.EraseString(SceneKey);if(path!="")EditorSceneManager.OpenScene(path);
        }
        private GameObject Track(GameObject go){_objects.Add(go);return go;}
        private static Vector3[] Bake(SkinnedMeshRenderer skin)
        {
            var mesh=new Mesh();try{skin.BakeMesh(mesh,true);Assert.That(mesh.vertices.All(ChildFoundationPrototypeTools.Finite)&&mesh.normals.All(ChildFoundationPrototypeTools.Finite));return mesh.vertices.Select(skin.transform.TransformPoint).ToArray();}finally{Object.DestroyImmediate(mesh);}
        }
        private static void Capture(Camera camera,string name)
        {Directory.CreateDirectory(GirlFoundationPrototypeTools.Evidence+"/Captures");HumanBasePrototypeTools.Capture(camera,"../GirlFoundation/Unity/Captures/"+name,1000,800);}

        [UnityTest] public IEnumerator RuntimeBaldScalpLocomotionAnd29ShapeParity()
        {
            var bald=Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(GirlFoundationPrototypeTools.Prefab)));
            var original=Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Prefab("Girl"))));
            var skin=bald.GetComponentInChildren<SkinnedMeshRenderer>();var reference=original.GetComponentInChildren<SkinnedMeshRenderer>();reference.enabled=false;
            var a=bald.GetComponent<Animator>();var b=original.GetComponent<Animator>();a.enabled=false;b.enabled=false;var keep=KeepIndices();
            var camera=Track(new GameObject("Girl technical camera")).AddComponent<Camera>();camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=10;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.17f,.21f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.38f,.38f);
            var key=Track(new GameObject("Key")).AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(25,-35,0);
            var fill=Track(new GameObject("Fill")).AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.4f;fill.transform.rotation=Quaternion.Euler(10,135,0);
            yield return null;
            var neutral=Bake(skin);var target=new Vector3(0,1.35f,0);camera.orthographicSize=.19f;
            foreach(var view in new[]{"front","side","crown"})
            {camera.transform.position=target+(view=="front"?Vector3.forward*.8f:view=="side"?Vector3.right*.8f:new Vector3(.3f,.6f,.3f));camera.transform.LookAt(target);Capture(camera,"Bald-"+view);}
            camera.orthographicSize=.83f;camera.transform.position=new Vector3(0,.74f,3);camera.transform.LookAt(new Vector3(0,.74f,0));Capture(camera,"Bald-full-body");
            var report=new List<string>();float maxParity=0;
            foreach(string state in new[]{"Idle","Walk","Run"})foreach(float phase in new[]{.15f,.45f,.75f})
            {
                foreach(var animator in new[]{a,b}){animator.enabled=true;animator.Play(state,0,phase);animator.Update(0);animator.enabled=false;}
                yield return null;var actual=Bake(skin);var prior=Bake(reference);float error=actual.Select((v,i)=>Vector3.Distance(v,prior[keep[i]])).Max();maxParity=Mathf.Max(error,maxParity);Assert.That(error,Is.LessThan(1e-6f));
                report.Add(state+" phase="+phase+" minimumY="+actual.Min(v=>v.y)+" parityMetres="+error);
                camera.orthographicSize=.83f;camera.transform.position=new Vector3(0,.74f,3);camera.transform.LookAt(new Vector3(0,.74f,0));Capture(camera,state+"-"+Mathf.RoundToInt(phase*100));
            }
            foreach(var animator in new[]{a,b}){animator.enabled=true;animator.Play("Idle",0,.15f);animator.Update(0);animator.enabled=false;}
            yield return null;neutral=Bake(skin);
            for(int s=0;s<29;s++)
            {
                foreach(float weight in new[]{25f,50f,75f,100f})
                {
                    skin.SetBlendShapeWeight(s,weight);reference.SetBlendShapeWeight(s,weight);yield return null;
                    var actual=Bake(skin);var prior=Bake(reference);Assert.That(actual.Zip(neutral,Vector3.Distance).Max(),Is.GreaterThan(1e-7f));
                    float error=actual.Select((v,i)=>Vector3.Distance(v,prior[keep[i]])).Max();maxParity=Mathf.Max(error,maxParity);Assert.That(error,Is.LessThan(1e-6f));
                }
                skin.SetBlendShapeWeight(s,0);reference.SetBlendShapeWeight(s,0);yield return null;CollectionAssert.AreEqual(neutral,Bake(skin));
            }
            report.Add("29 shapes at 25/50/75/100: all finite, nonzero, exact neutral reset; maximum original/bald parity metres="+maxParity);
            File.WriteAllLines(GirlFoundationPrototypeTools.Evidence+"/bald-runtime.txt",report);
        }
    }
}
