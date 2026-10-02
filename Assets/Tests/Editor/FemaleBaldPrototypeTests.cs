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
    public sealed class FemaleBaldPrototypeTests
    {
        private const string SceneKey = "SilverScreen.FemaleBald.PreviousScene";
        private readonly List<GameObject> _objects = new List<GameObject>();
        private SkinnedMeshRenderer _baseline, _bald;
        private Animator _sourceAnimator, _baldAnimator;
        private bool _background;
        private readonly List<string> _normalFailures = new List<string>();

        [UnitySetUp] public IEnumerator Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FemaleBaldPrototypeTools.Prefab) == null)
                Assert.Ignore("Requires local generated licensed bald FBX and prototype builder.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            _background = Application.runInBackground; Application.runInBackground = true;
            _baseline = Spawn(HumanBasePrototypeTools.PrefabPath, out _sourceAnimator);
            _bald = Spawn(FemaleBaldPrototypeTools.Prefab, out _baldAnimator);
            _baseline.enabled = false;
            yield return null;
        }

        private SkinnedMeshRenderer Spawn(string path, out Animator animator)
        {
            var inactive = new GameObject("Inactive bald fixture setup"); inactive.SetActive(false);
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), inactive.transform);
            go.GetComponent<NavMeshAgent>().enabled = false;
            go.GetComponent<EmployeeAgent>().enabled = false;
            go.GetComponent<HumanBasePrototypeVisual>().enabled = false;
            go.transform.SetParent(null, false); go.transform.position = Vector3.up;
            Object.DestroyImmediate(inactive); _objects.Add(go);
            animator = go.GetComponentInChildren<Animator>();
            animator.Play("Idle", 0, 0); animator.Update(0); animator.speed = 0;
            return go.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        [UnityTearDown] public IEnumerator Teardown()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
            if (Application.isPlaying) { Application.runInBackground = _background; yield return new ExitPlayMode(); }
            var path = SessionState.GetString(SceneKey, ""); SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }

        private static (Vector3[] points, Vector3[] normals) Sample(SkinnedMeshRenderer skin)
        {
            var mesh = new Mesh();
            try
            {
                skin.BakeMesh(mesh, true);
                return (mesh.vertices.Select(skin.transform.TransformPoint).ToArray(),
                    mesh.normals.Select(n => skin.transform.TransformDirection(n).normalized).ToArray());
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        private static (int, int, int) Cell(Vector3 v) =>
            (Mathf.RoundToInt(v.x * 100000), Mathf.RoundToInt(v.y * 100000), Mathf.RoundToInt(v.z * 100000));

        private int[] Map(Vector3[] source, Vector3[] candidate)
        {
            var cells = new Dictionary<(int, int, int), List<int>>();
            for (int i = 0; i < source.Length; i++)
            {
                var key = Cell(source[i]);
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<int>();
                list.Add(i);
            }
            var a = _baseline.sharedMesh.uv; var b = _bald.sharedMesh.uv;
            var a2 = _baseline.sharedMesh.uv2; var b2 = _bald.sharedMesh.uv2;
            var map = new int[candidate.Length];
            for (int i = 0; i < candidate.Length; i++)
            {
                var c = Cell(candidate[i]); int found = -1; float score = float.MaxValue;
                for (int x = -2; x <= 2; x++) for (int y = -2; y <= 2; y++) for (int z = -2; z <= 2; z++)
                    if (cells.TryGetValue((c.Item1+x,c.Item2+y,c.Item3+z), out var entries)) foreach (int j in entries)
                    {
                        float distance = (source[j]-candidate[i]).sqrMagnitude;
                        if (distance > .00003f*.00003f) continue;
                        float cost = distance + (a[j]-b[i]).sqrMagnitude + (a2[j]-b2[i]).sqrMagnitude;
                        if (cost < score) { score = cost; found = j; }
                    }
                Assert.That(found, Is.GreaterThanOrEqualTo(0), "Unmapped retained vertex " + i);
                map[i] = found;
                Assert.That(Vector2.Distance(a[found],b[i]), Is.LessThan(.000002f), "UVMap changed");
                Assert.That(Vector2.Distance(a2[found],b2[i]), Is.LessThan(.000002f), "map1 changed");
            }
            return map;
        }

        private static Dictionary<string,float>[] Weights(SkinnedMeshRenderer skin)
        {
            var counts = skin.sharedMesh.GetBonesPerVertex(); var weights = skin.sharedMesh.GetAllBoneWeights();
            try
            {
                var values = new Dictionary<string,float>[counts.Length]; int offset = 0;
                for (int i=0; i<values.Length; i++)
                {
                    values[i] = new Dictionary<string,float>();
                    for (int j=0;j<counts[i];j++) { var w=weights[offset++]; values[i][skin.bones[w.boneIndex].name]=w.weight; }
                }
                return values;
            }
            finally { counts.Dispose(); weights.Dispose(); }
        }

        private float Difference(int[] map, string label, List<string> lines, bool normals = false)
        {
            var a = Sample(_baseline); var b = Sample(_bald); float max=0,angle=0;
            for (int i=0;i<map.Length;i++)
            {
                Assert.That(float.IsFinite(b.points[i].x)&&float.IsFinite(b.points[i].y)&&float.IsFinite(b.points[i].z),Is.True);
                max=Mathf.Max(max,Vector3.Distance(a.points[map[i]],b.points[i]));
                angle=Mathf.Max(angle,Vector3.Angle(a.normals[map[i]],b.normals[i]));
            }
            lines.Add(label+" retained max world m="+max+" normal max degrees="+angle);
            Assert.That(max,Is.LessThan(.0001f),label+" changed retained geometry");
            File.WriteAllLines(FemaleBaldPrototypeTools.Review+"/unity-retained-parity.txt",lines);
            if(normals && angle >= 1) _normalFailures.Add(label+" normal degrees="+angle);
            return max;
        }

        [UnityTest] public IEnumerator PreservesHumanoidSkinUvsAndAll29ShapesThroughExport()
        {
            var lines = new List<string>();
            _normalFailures.Clear();
            Assert.That(_baldAnimator.avatar.isValid&&_baldAnimator.avatar.isHuman,Is.True);
            Assert.That(_bald.bones.Length,Is.EqualTo(51));
            Assert.That(_bald.sharedMesh.blendShapeCount,Is.EqualTo(29));
            Assert.That(_bald.sharedMesh.triangles.Length/3,Is.EqualTo(32512));
            Assert.That(_bald.sharedMaterial,Is.SameAs(_baseline.sharedMaterial));
            var spatialMap=Map(Sample(_baseline).points,Sample(_bald).points);
            var map=File.ReadAllLines(FemaleBaldPrototypeTools.Review+"/unity-original-indices.txt").Select(int.Parse).ToArray();
            Assert.That(map.Length,Is.EqualTo(spatialMap.Length));
            var sourcePoints=Sample(_baseline).points;var baldPoints=Sample(_bald).points;
            for(int i=0;i<map.Length;i++)Assert.That(Vector3.Distance(sourcePoints[map[i]],baldPoints[i]),Is.LessThan(.000001f));
            var sourceWeights=Weights(_baseline);var baldWeights=Weights(_bald);
            for(int i=0;i<map.Length;i++)
            {
                Assert.That(baldWeights[i].Keys,Is.EquivalentTo(sourceWeights[map[i]].Keys));
                foreach(var pair in baldWeights[i])Assert.That(pair.Value,Is.EqualTo(sourceWeights[map[i]][pair.Key]).Within(.000002));
            }
            Difference(map,"Neutral",lines,true);
            for(int shape=0;shape<29;shape++)
            {
                string name=_bald.sharedMesh.GetBlendShapeName(shape);
                int original=_baseline.sharedMesh.GetBlendShapeIndex(name);Assert.That(original,Is.GreaterThanOrEqualTo(0));
                var sourceMesh=_baseline.sharedMesh;var baldMesh=_bald.sharedMesh;
                Assert.That(baldMesh.GetBlendShapeFrameCount(shape),Is.EqualTo(sourceMesh.GetBlendShapeFrameCount(original)));
                for(int frame=0;frame<sourceMesh.GetBlendShapeFrameCount(original);frame++)
                {
                    Assert.That(baldMesh.GetBlendShapeFrameWeight(shape,frame),Is.EqualTo(sourceMesh.GetBlendShapeFrameWeight(original,frame)));
                    var av=new Vector3[sourceMesh.vertexCount];var an=new Vector3[av.Length];var at=new Vector3[av.Length];
                    var bv=new Vector3[baldMesh.vertexCount];var bn=new Vector3[bv.Length];var bt=new Vector3[bv.Length];
                    sourceMesh.GetBlendShapeFrameVertices(original,frame,av,an,at);baldMesh.GetBlendShapeFrameVertices(shape,frame,bv,bn,bt);
                    for(int i=0;i<map.Length;i++)
                    {
                        Assert.That(bv[i].Equals(av[map[i]])&&bn[i].Equals(an[map[i]])&&bt[i].Equals(at[map[i]]),Is.True,name+" frame data changed");
                    }
                }
                if(sourceMesh.GetBlendShapeFrameWeight(original,0)<=0)
                {
                    _baseline.SetBlendShapeWeight(original,100);_bald.SetBlendShapeWeight(shape,100);
                    var a=Sample(_baseline).points;var b=Sample(_bald).points;
                    int originalInvalid=map.Count(i=>!float.IsFinite(a[i].x)||!float.IsFinite(a[i].y)||!float.IsFinite(a[i].z));
                    int baldInvalid=b.Count(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z));
                    lines.Add(name+" preserved zero frame weight; baseline invalid vertices="+originalInvalid+" bald="+baldInvalid+". Runtime channel unaccepted.");
                    Assert.That(baldInvalid,Is.EqualTo(originalInvalid),"Zero-frame failure must be identified as baseline or regression");
                    _baseline.SetBlendShapeWeight(original,0);_bald.SetBlendShapeWeight(shape,0);
                    continue;
                }
                _baseline.SetBlendShapeWeight(original,100);_bald.SetBlendShapeWeight(shape,100);
                Difference(map,name,lines,true);
                _baseline.SetBlendShapeWeight(original,0);_bald.SetBlendShapeWeight(shape,0);
            }
            Difference(map,"Neutral restored",lines,true);
            var before=Sample(_bald).points;
            foreach(var clip in new[]{"Idle","Walk","Run"}) foreach(float phase in new[]{0f,.25f,.5f,.75f})
            {
                _sourceAnimator.Play(clip,0,phase);_sourceAnimator.Update(0);
                _baldAnimator.Play(clip,0,phase);_baldAnimator.Update(0);
                Difference(map,clip+" "+phase,lines);
            }
            Assert.That(Sample(_bald).points.Zip(before,Vector3.Distance).Max(),Is.GreaterThan(.05f),"Body animation must actually move vertices");
            Directory.CreateDirectory(FemaleBaldPrototypeTools.Review);
            File.WriteAllLines(FemaleBaldPrototypeTools.Review+"/unity-retained-parity.txt",lines);
            Assert.That(_normalFailures,Is.Empty,"Retained normal direction changed: "+string.Join("; ",_normalFailures));
            yield return null;
        }

        [UnityTest] public IEnumerator CaptureBaldScalpAndExpressionTechnicalViews()
        {
            var cameraGo=new GameObject("Bald technical camera");_objects.Add(cameraGo);
            var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.19f;
            camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.16f,.18f,.2f);
            var lightGo=new GameObject("Bald technical key");_objects.Add(lightGo);
            var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;
            var target=new Vector3(0,1.57f,0);
            var views=new[]{("front",new Vector3(0,.025f,.85f)),("three-quarter",new Vector3(.65f,.12f,.8f)),
                ("profile",new Vector3(.85f,.025f,0)),("back",new Vector3(0,.04f,-.85f)),("top",new Vector3(0,.85f,.001f))};
            foreach(var view in views)
            {
                camera.transform.position=target+view.Item2;camera.transform.LookAt(target);
                lightGo.transform.rotation=Quaternion.LookRotation(target-(camera.transform.position+new Vector3(-.25f,.25f,.1f)));
                for(int i=0;i<3;i++)yield return null;
                Capture(camera,view.Item1+"-bald");
                if(view.Item1=="three-quarter")
                {
                    _bald.enabled=false;_baseline.enabled=true;for(int i=0;i<3;i++)yield return null;
                    Capture(camera,view.Item1+"-hair");_baseline.enabled=false;_bald.enabled=true;
                }
            }
            camera.transform.position=target+views[0].Item2;camera.transform.LookAt(target);
            lightGo.transform.rotation=Quaternion.Euler(25,-145,0);
            foreach(var expression in new[]{new[]{"mouthSmileLeft","mouthSmileRight"},new[]{"mouthFrownLeft","mouthFrownRight"},new[]{"jawOpen"}})
            {
                if(expression.Any(name=>_bald.sharedMesh.GetBlendShapeFrameWeight(_bald.sharedMesh.GetBlendShapeIndex(name),0)<=0))continue;
                foreach(string name in expression)_bald.SetBlendShapeWeight(_bald.sharedMesh.GetBlendShapeIndex(name),100);
                for(int i=0;i<3;i++)yield return null;Capture(camera,expression[0]);
                foreach(string name in expression)_bald.SetBlendShapeWeight(_bald.sharedMesh.GetBlendShapeIndex(name),0);
            }
        }

        private static void Capture(Camera camera,string name) =>
            HumanBasePrototypeTools.Capture(camera,"../FemaleBaldBase/unity-"+name,850,850);
    }
}
