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
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ChildFoundationPrototypeTests
    {
        private const string SceneKey="SilverScreen.ChildStructural.PreviousScene";
        private readonly List<GameObject> _objects=new List<GameObject>();
        private bool _background;

        [TestCase("Boy",50,19861)]
        [TestCase("Girl",51,20207)]
        public void NativeHumanoidAndAllChannels(string family,int bones,int vertices)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Prefab(family));
            Assert.That(asset,Is.Not.Null,"Requires the locally licensed structural import.");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Model(family));
            var skin=asset.GetComponentInChildren<SkinnedMeshRenderer>();var animator=asset.GetComponent<Animator>();
            Assert.That(animator.avatar.isHuman&&animator.avatar.isValid,Is.True);
            Assert.That(asset.transform.localScale,Is.EqualTo(Vector3.one));
            Assert.That(skin.sharedMesh,Is.SameAs(model.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh));
            Assert.That(skin.bones.Length,Is.EqualTo(bones));Assert.That(skin.sharedMesh.vertexCount,Is.EqualTo(vertices));
            Assert.That(skin.sharedMesh.blendShapeCount,Is.EqualTo(29));
            var probe=JsonUtility.FromJson<ChildFoundationPrototypeTools.Inspection>(File.ReadAllText(ChildFoundationPrototypeTools.Evidence+"/"+family+"/Unity/inspection.json"));
            Assert.That(probe.shapes.All(s=>s.status=="SUPPORTED"&&s.resetExact&&s.defaultWeight==0),Is.True);
            foreach(var human in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,
                HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder,
                HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,
                HumanBodyBones.LeftToes,HumanBodyBones.RightToes}) Assert.That(animator.GetBoneTransform(human),Is.Not.Null,human.ToString());
            Assert.That(animator.GetBoneTransform(HumanBodyBones.Head).IsChildOf(animator.GetBoneTransform(HumanBodyBones.Hips)),Is.True);
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            if(!TestContext.CurrentContext.Test.Name.StartsWith("Runtime")) yield break;
            Assert.That(SceneManager.sceneCount,Is.EqualTo(1));Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            SessionState.SetString(SceneKey,SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            _background=Application.runInBackground;Application.runInBackground=true;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            foreach(var go in _objects.AsEnumerable().Reverse()) if(go!=null)Object.DestroyImmediate(go);
            _objects.Clear();
            if(Application.isPlaying){Application.runInBackground=_background;yield return new ExitPlayMode();}
            var path=SessionState.GetString(SceneKey,"");SessionState.EraseString(SceneKey);
            if(path!="")EditorSceneManager.OpenScene(path);
        }
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        private static Vector3[] Bake(SkinnedMeshRenderer skin)
        {
            var mesh=new Mesh();
            try
            {
                skin.BakeMesh(mesh,true);
                Assert.That(mesh.vertices.All(ChildFoundationPrototypeTools.Finite)&&mesh.normals.All(ChildFoundationPrototypeTools.Finite),Is.True);
                return mesh.vertices.Select(skin.transform.TransformPoint).ToArray();
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        private static void Capture(Camera camera,string family,string name)
        {
            Directory.CreateDirectory(ChildFoundationPrototypeTools.Evidence+"/"+family+"/Unity/Captures");
            HumanBasePrototypeTools.Capture(camera,"../ChildFoundation/"+family+"/Unity/Captures/"+name,1000,800);
        }
        private IEnumerator Runtime(string family)
        {
            var root=Keep(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Prefab(family))));
            var animator=root.GetComponent<Animator>();var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var camera=Keep(new GameObject("Technical camera")).AddComponent<Camera>();camera.orthographic=true;
            camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.17f,.21f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.38f,.38f);
            var key=Keep(new GameObject("Key")).AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(25,-35,0);
            var fill=Keep(new GameObject("Fill")).AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.4f;fill.transform.rotation=Quaternion.Euler(10,135,0);
            var ruler=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));ruler.name="One metre scale marker";
            ruler.transform.position=new Vector3(-.65f,.5f,0);ruler.transform.localScale=new Vector3(.025f,1,.025f);
            ruler.GetComponent<Renderer>().sharedMaterial=skin.sharedMaterial;Object.DestroyImmediate(ruler.GetComponent<Collider>());
            var report=new List<string>{"One metre marker; native import scale unchanged. Structural adult-clip retarget only."};
            var rootPosition=root.transform.position;var poses=new List<Vector3[]>();
            // Original rest-pose screenshot, followed by actual Humanoid playback samples.
            animator.enabled=false;yield return null;
            camera.orthographicSize=.84f;var center=new Vector3(0,.75f,0);camera.transform.position=center+Vector3.forward*3;camera.transform.LookAt(center);
            Capture(camera,family,"Rest-front");
            foreach(string state in new[]{"Idle","Walk","Run"}) foreach(float phase in new[]{.15f,.45f,.75f})
            {
                animator.enabled=true;animator.Play(state,0,phase);animator.Update(0);animator.enabled=false;yield return null;
                var points=Bake(skin);poses.Add(points);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state),Is.True);
                Assert.That(root.transform.position,Is.EqualTo(rootPosition));
                Assert.That(points.Max(p=>p.y)-points.Min(p=>p.y),Is.InRange(1.0f,1.8f));
                report.Add(state+" "+phase+" minY="+points.Min(p=>p.y).ToString("R")+" maxY="+points.Max(p=>p.y).ToString("R")+" root="+rootPosition);
                foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftHand,HumanBodyBones.RightHand})
                    report.Add("  "+bone+"="+animator.GetBoneTransform(bone).position.ToString("F6"));
                camera.orthographicSize=.84f;camera.transform.position=center+Vector3.forward*3;camera.transform.LookAt(center);
                Capture(camera,family,state+"-"+Mathf.RoundToInt(phase*100)+"-front");
                camera.transform.position=center+new Vector3(2,0,2);camera.transform.LookAt(center);Capture(camera,family,state+"-"+Mathf.RoundToInt(phase*100)+"-oblique");
            }
            Assert.That(poses[0].Zip(poses[3],Vector3.Distance).Max(),Is.GreaterThan(.03f));
            Assert.That(poses[3].Zip(poses[6],Vector3.Distance).Max(),Is.GreaterThan(.03f));
            animator.enabled=true;animator.Play("Idle",0,.15f);animator.Update(0);animator.enabled=false;yield return null;
            ruler.SetActive(false);
            var head=animator.GetBoneTransform(HumanBodyBones.Head).position;var face=head+Vector3.up*.07f;
            camera.orthographicSize=.16f;camera.transform.position=face+Vector3.forward*.8f;camera.transform.LookAt(face);
            var neutral=Bake(skin);Capture(camera,family,"Neutral-face");
            for(int s=0;s<skin.sharedMesh.blendShapeCount;s++)
            {
                foreach(float w in new[]{25f,50f,75f,100f})
                {
                    skin.SetBlendShapeWeight(s,w);yield return null;
                    Assert.That(Bake(skin).Zip(neutral,Vector3.Distance).Max(),Is.GreaterThan(1e-7f));
                }
                Capture(camera,family,"Shape-"+s.ToString("D2")+"-"+skin.sharedMesh.GetBlendShapeName(s));
                skin.SetBlendShapeWeight(s,0);yield return null;CollectionAssert.AreEqual(neutral,Bake(skin));
            }
            report.Add("All 29 supplied shapes finite/nonzero at 25/50/75/100 in animated Idle. Every reset exact. No child BlinkBoth added.");
            File.WriteAllLines(ChildFoundationPrototypeTools.Evidence+"/"+family+"/Unity/runtime.txt",report);
        }
        [UnityTest] public IEnumerator RuntimeBoy() => Runtime("Boy");
        [UnityTest] public IEnumerator RuntimeGirl() => Runtime("Girl");
    }
}
