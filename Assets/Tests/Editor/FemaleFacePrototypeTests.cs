using System;
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
    public sealed class FemaleFacePrototypeTests
    {
        private const string SceneKey="SilverScreen.FemaleFace.PreviousScene";
        private GameObject _person;
        private FemaleFacePrototype _face;
        private Animator _animator;
        private bool _previousBackground;
        [UnitySetUp] public IEnumerator Setup()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(FemaleFacePrototypeTools.Prefab)==null)
                Assert.Ignore("Requires the local licensed female facial export and isolated builder.");
            Assert.That(SceneManager.sceneCount,Is.EqualTo(1));Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            SessionState.SetString(SceneKey,SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            _previousBackground=Application.runInBackground;Application.runInBackground=true;
            var inactive=new GameObject("Inactive facial fixture setup");inactive.SetActive(false);
            _person=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FemaleFacePrototypeTools.Prefab),inactive.transform);
            _person.GetComponent<NavMeshAgent>().enabled=false;_person.GetComponent<EmployeeAgent>().enabled=false;
            _person.GetComponent<HumanBasePrototypeVisual>().enabled=false;
            _person.transform.SetParent(null,false);Object.DestroyImmediate(inactive);
            _face=_person.GetComponent<FemaleFacePrototype>();_animator=_person.GetComponentInChildren<Animator>();
            _animator.Play("Idle",0,0);_animator.Update(0);_animator.speed=0;_face.ResetNeutral();
            yield return null;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(_person!=null)Object.DestroyImmediate(_person);
            if(Application.isPlaying){Application.runInBackground=_previousBackground;yield return new ExitPlayMode();}
            string path=SessionState.GetString(SceneKey,"");SessionState.EraseString(SceneKey);
            if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
        }
        private Vector3[] Points()
        {
            var mesh=new Mesh();
            try{_face.Apply();_face.Face.BakeMesh(mesh,true);return mesh.vertices.Select(_face.Face.transform.TransformPoint).ToArray();}
            finally{Object.DestroyImmediate(mesh);}
        }
        private static float Difference(Vector3[] a,Vector3[] b)=>a.Zip(b,Vector3.Distance).Max();
        private int[] EyeVertices(string boneName)
        {
            int bone=Array.FindIndex(_face.Face.bones,b=>b.name==boneName);
            var counts=_face.Face.sharedMesh.GetBonesPerVertex();var weights=_face.Face.sharedMesh.GetAllBoneWeights();
            var indices=new List<int>();int offset=0;
            try
            {
                for(int i=0;i<counts.Length;i++)for(int j=0;j<counts[i];j++)
                {var w=weights[offset++];if(w.boneIndex==bone&&w.weight>.999f)indices.Add(i);}
            }
            finally{counts.Dispose();weights.Dispose();}
            return indices.ToArray();
        }

        [UnityTest] public IEnumerator BlinkGazeBrowAndHybridCombinationsRestoreNeutral()
        {
            Assert.That(_animator.avatar.isValid&&_animator.avatar.isHuman,Is.True);
            Assert.That(_animator.GetBoneTransform(HumanBodyBones.Jaw),Is.Null,"An eyelid must never be Humanoid Jaw.");
            Assert.That(_face.Face.sharedMesh.blendShapeCount,Is.EqualTo(29));Assert.That(_face.Bones.Length,Is.EqualTo(44));
            var neutral=Points();var lines=new List<string>();
            var left=_face.Bones.Single(b=>b.name=="SS_Eye_L");var right=_face.Bones.Single(b=>b.name=="SS_Eye_R");
            var lrot=left.localRotation;var rrot=right.localRotation;
            var eyeGroups=new[]{EyeVertices("SS_Eye_L"),EyeVertices("SS_Eye_R")};
            foreach(var indices in eyeGroups)Assert.That(indices.Length,Is.GreaterThan(300));
            var eyes=new[]{left,right};var eyeInverse=eyes.Select(t=>t.worldToLocalMatrix).ToArray();
            for(int side=0;side<2;side++)
            {
                Vector3 centre=Vector3.zero;foreach(int i in eyeGroups[side])centre+=neutral[i];centre/=eyeGroups[side].Length;
                Assert.That(Vector3.Distance(centre,eyes[side].position),Is.LessThan(.03f),"Eye geometry must belong to its own socket.");
            }
            foreach(var pose in (PrototypeFacePose[])Enum.GetValues(typeof(PrototypeFacePose)))
            {
                _face.ResetNeutral();Assert.That(_face.SetPose(pose),Is.True);var points=Points();
                float change=Difference(neutral,points);lines.Add(pose+" max world deformation m="+change);
                Assert.That(points.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),Is.True);
                Assert.That(change,Is.LessThan(.15f),"No exploding vertices");
                if(pose!=PrototypeFacePose.Neutral)Assert.That(change,Is.GreaterThan(.001f),pose.ToString());
                if(pose==PrototypeFacePose.BlinkLeft||pose==PrototypeFacePose.BlinkRight)
                {
                    var opposite=pose==PrototypeFacePose.BlinkLeft?right:left;
                    var ids=Enumerable.Range(0,neutral.Length).Where(i=>Mathf.Abs(neutral[i].x-opposite.position.x)<.027f &&
                        neutral[i].y>opposite.position.y-.01f&&neutral[i].y<opposite.position.y+.025f).ToArray();
                    Assert.That(ids.Length,Is.GreaterThan(20));
                    float oppositeChange=ids.Max(i=>Vector3.Distance(neutral[i],points[i]));
                    lines.Add(pose+" opposite eye region max m="+oppositeChange);
                    Assert.That(oppositeChange,Is.LessThan(.001f),"Opposite eye should remain open");
                }
                if(pose>=PrototypeFacePose.LookLeft&&pose<=PrototypeFacePose.LookDown)
                {
                    float a=Quaternion.Angle(lrot,left.localRotation),b=Quaternion.Angle(rrot,right.localRotation);
                    lines.Add(pose+" eye rotations degrees="+a+","+b);Assert.That(a,Is.GreaterThan(10));Assert.That(b,Is.GreaterThan(10));
                    for(int side=0;side<2;side++)
                    {
                        var indices=eyeGroups[side];Vector3 shift=Vector3.zero;float rigidError=0;
                        foreach(int i in indices)
                        {
                            shift+=points[i]-neutral[i];
                            var expected=eyes[side].localToWorldMatrix.MultiplyPoint3x4(eyeInverse[side].MultiplyPoint3x4(neutral[i]));
                            rigidError=Mathf.Max(rigidError,Vector3.Distance(expected,points[i]));
                        }
                        shift/=indices.Length;
                        lines.Add(pose+" eye centroid travel m="+shift.magnitude);
                        // The supplied asymmetrical eye mesh's centroid moves 3.36–4.66 mm in Blender.
                        // Compare actual rigid deformation, rather than assuming a centred sphere.
                        lines.Add(pose+" rigid eye transform error m="+rigidError);
                        Assert.That(rigidError,Is.LessThan(.00002f),"Eye vertices must follow their own runtime transform.");
                    }
                }
                _face.ResetNeutral();Assert.That(Difference(neutral,Points()),Is.LessThan(.000002f));
            }
            foreach(var combination in new[] {
                (PrototypeMouthPose.Smile,PrototypeFacePose.BlinkBoth), (PrototypeMouthPose.Smile,PrototypeFacePose.LookLeft),
                (PrototypeMouthPose.Frown,PrototypeFacePose.BrowLowerBoth),(PrototypeMouthPose.JawOpen,PrototypeFacePose.BlinkBoth)})
            {
                _face.ResetNeutral();_face.SetMouth(combination.Item1,1);var mouth=Points();
                Assert.That(Difference(neutral,mouth),Is.GreaterThan(.002f));
                _face.SetPose(combination.Item2);var combined=Points();
                Assert.That(Difference(mouth,combined),Is.GreaterThan(.002f));Assert.That(Difference(neutral,combined),Is.LessThan(.15f));
                lines.Add(combination+" combined max world m="+Difference(neutral,combined));
                _face.ResetNeutral();Assert.That(Difference(neutral,Points()),Is.LessThan(.000002f));
            }
            Directory.CreateDirectory(FemaleFacePrototypeTools.Review);File.WriteAllLines(FemaleFacePrototypeTools.Review+"/runtime-deformation.txt",lines);
            yield return null;
        }

        [UnityTest] public IEnumerator CaptureDeterministicTechnicalGallery()
        {
            var eye=_face.Bones.Single(b=>b.name=="SS_Eye_L");var target=eye.position;target.x=0;target.y+=.015f;
            var cameraGo=new GameObject("Technical facial camera");var camera=cameraGo.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=.19f;camera.nearClipPlane=.01f;camera.farClipPlane=10;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.20f);
            camera.transform.position=target+new Vector3(0,.025f,.85f);camera.transform.LookAt(target);
            var lightGo=new GameObject("Technical facial key");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=1.3f;lightGo.transform.rotation=Quaternion.Euler(25,-145,0);
            try
            {
                foreach(var pose in (PrototypeFacePose[])Enum.GetValues(typeof(PrototypeFacePose)))
                {
                    _face.ResetNeutral();_face.SetPose(pose);
                    for(int i=0;i<3;i++)yield return null;
                    Capture(camera,pose.ToString());
                }
                foreach(var c in new[] {(PrototypeMouthPose.Smile,PrototypeFacePose.BlinkBoth),
                    (PrototypeMouthPose.Smile,PrototypeFacePose.LookLeft),(PrototypeMouthPose.Frown,PrototypeFacePose.BrowLowerBoth),
                    (PrototypeMouthPose.JawOpen,PrototypeFacePose.BlinkBoth)})
                {
                    _face.ResetNeutral();_face.SetMouth(c.Item1,1);_face.SetPose(c.Item2);
                    for(int i=0;i<3;i++)yield return null;
                    Capture(camera,c.Item1+"-"+c.Item2);
                }
                _face.ResetNeutral();for(int i=0;i<3;i++)yield return null;Capture(camera,"NeutralRestored");
            }
            finally{Object.DestroyImmediate(cameraGo);Object.DestroyImmediate(lightGo);}
        }
        private static void Capture(Camera camera,string name)
        {
            // Existing URP capture helper writes beneath the already ignored audit evidence root.
            HumanBasePrototypeTools.Capture(camera,"../FemaleFaceInvestigation/unity-"+name,800,800);
        }
    }
}
