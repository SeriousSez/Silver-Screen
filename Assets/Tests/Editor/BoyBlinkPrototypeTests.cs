using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Editor.Characters;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class BoyBlinkPrototypeTests
    {
        private const string Review = BoyBlinkPrototypeTools.Review;
        private const string SceneKey = "SilverScreen.BoyBlink.PreviousScene";
        private List<GameObject> _objects;
        private bool _background;

        [Test] public void NativeBaselineAndSelectedEndpointArePreserved()
        {
            var original = AssetDatabase.LoadAssetAtPath<Mesh>(BoyBlinkPrototypeTools.BaselineMesh);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoyBlinkPrototypeTools.MeshPath);
            Assert.That(mesh, Is.Not.Null, "Build the locally licensed candidate first.");
            Assert.That(mesh.vertexCount, Is.EqualTo(18069)); Assert.That(mesh.triangles.Length / 3, Is.EqualTo(31506));
            CollectionAssert.AreEqual(original.GetVertexAttributes(), mesh.GetVertexAttributes());
            CollectionAssert.AreEqual(original.vertices, mesh.vertices);
            CollectionAssert.AreEqual(original.normals, mesh.normals);
            CollectionAssert.AreEqual(original.tangents, mesh.tangents);
            CollectionAssert.AreEqual(original.colors, mesh.colors);
            CollectionAssert.AreEqual(original.bindposes, mesh.bindposes);
            Assert.That(mesh.bounds, Is.EqualTo(original.bounds));
            for (int c = 0; c < 8; c++)
            {
                var a = new List<Vector4>(); var b = new List<Vector4>(); original.GetUVs(c, a); mesh.GetUVs(c, b);
                CollectionAssert.AreEqual(a, b, "UV channel " + c);
            }
            Assert.That(mesh.subMeshCount, Is.EqualTo(original.subMeshCount));
            for (int s = 0; s < mesh.subMeshCount; s++) CollectionAssert.AreEqual(original.GetTriangles(s), mesh.GetTriangles(s));
            using (var a = original.GetBonesPerVertex()) using (var b = mesh.GetBonesPerVertex()) CollectionAssert.AreEqual(a.ToArray(), b.ToArray());
            using (var a = original.GetAllBoneWeights()) using (var b = mesh.GetAllBoneWeights()) CollectionAssert.AreEqual(a.ToArray(), b.ToArray());
            Assert.That(mesh.blendShapeCount, Is.EqualTo(30)); Assert.That(mesh.GetBlendShapeIndex("BlinkBoth"), Is.EqualTo(29));
            var v = new Vector3[mesh.vertexCount]; var n = new Vector3[v.Length]; var t = new Vector3[v.Length];
            var ov = new Vector3[v.Length]; var on = new Vector3[v.Length]; var ot = new Vector3[v.Length];
            for (int s = 0; s < 29; s++)
            {
                Assert.That(mesh.GetBlendShapeName(s), Is.EqualTo(original.GetBlendShapeName(s)));
                Assert.That(mesh.GetBlendShapeFrameCount(s), Is.EqualTo(original.GetBlendShapeFrameCount(s)));
                for (int f = 0; f < mesh.GetBlendShapeFrameCount(s); f++)
                {
                    Assert.That(mesh.GetBlendShapeFrameWeight(s, f), Is.EqualTo(original.GetBlendShapeFrameWeight(s, f)));
                    original.GetBlendShapeFrameVertices(s, f, ov, on, ot); mesh.GetBlendShapeFrameVertices(s, f, v, n, t);
                    CollectionAssert.AreEqual(ov, v); CollectionAssert.AreEqual(on, n); CollectionAssert.AreEqual(ot, t);
                }
            }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BoyBlinkPrototypeTools.Prefab);
            var baseline = AssetDatabase.LoadAssetAtPath<GameObject>(BoyFoundationPrototypeTools.Prefab);
            var visual = go.GetComponent<HumanBasePrototypeVisual>(); var baselineAnimator = baseline.GetComponent<Animator>(); var baselineSkin=baseline.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(visual.Animator.avatar, Is.SameAs(baselineAnimator.avatar));
            Assert.That(visual.Animator.avatar.isValid && visual.Animator.avatar.isHuman, Is.True);
            Assert.That(visual.Animator.runtimeAnimatorController, Is.SameAs(baselineAnimator.runtimeAnimatorController));
            Assert.That(visual.Face.bones.Length, Is.EqualTo(50));
            Assert.That(go.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(baselineSkin.sharedMaterials, visual.Face.sharedMaterials);
            for (int i = 0; i < 50; i++)
            {
                var a = baselineSkin.bones[i]; var b = visual.Face.bones[i];
                Assert.That(AnimationUtility.CalculateTransformPath(b, visual.Animator.transform), Is.EqualTo(AnimationUtility.CalculateTransformPath(a, baselineAnimator.transform)));
                Assert.That(b.localPosition, Is.EqualTo(a.localPosition)); Assert.That(b.localRotation, Is.EqualTo(a.localRotation));
                Assert.That(b.localScale, Is.EqualTo(a.localScale));
            }
            var packet = BoyBlinkPrototypeTools.ReadAddition(); var mat = BoyBlinkPrototypeTools.ModelMatrix;
            var positions = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
            var report = new List<string> { "Native vertices, topology, all UVs, normals, tangents, colours, all skin weights/bindposes and 29 old shape frames: exact.",
                "Humanoid Avatar/controller references and all 50 bone paths/local transforms: exact.",
                "Single added shape BlinkBoth; 20 normal/tangent sample frames with linear position deformation." };
            Assert.That(mesh.GetBlendShapeFrameCount(29), Is.EqualTo(20));
            for (int f = 0; f < 20; f++)
            {
                mesh.GetBlendShapeFrameVertices(29, f, v, n, t); var frame = packet.Frames[f];
                Assert.That(mesh.GetBlendShapeFrameWeight(29, f), Is.EqualTo(frame.Weight));
                float error = 0, deltaError = 0, blinkRegionError = 0, normalAngle = 0, tangentRelationError = 0, outside = 0;
                for (int i = 0; i < v.Length; i++)
                {
                    Assert.That(BoyBlinkPrototypeTools.Finite(positions[i]+v[i]) && BoyBlinkPrototypeTools.Finite(normals[i]+n[i]) && BoyBlinkPrototypeTools.Finite((Vector3)tangents[i]+t[i]), Is.True);
                    var expected = Vector3.Lerp(packet.ReferenceNeutral[i], packet.ReferenceClosed[i], frame.Weight/100);
                    error = Mathf.Max(error, Vector3.Distance(mat.MultiplyPoint3x4(positions[i]+v[i]), expected));
                    var wn = mat.inverse.transpose.MultiplyVector(normals[i]+n[i]).normalized;
                    bool moving=mat.MultiplyVector(v[i]).magnitude>1e-6f;
                    if(moving){normalAngle=Mathf.Max(normalAngle,Vector3.Angle(wn,frame.ReferenceNormals[i]));blinkRegionError=Mathf.Max(blinkRegionError,Vector3.Distance(mat.MultiplyPoint3x4(positions[i]+v[i]),expected));}
                    deltaError=Mathf.Max(deltaError,Vector3.Distance(mat.MultiplyVector(v[i]),(packet.ReferenceClosed[i]-packet.ReferenceNeutral[i])*(frame.Weight/100)));
                    var wt = mat.MultiplyVector((Vector3)tangents[i]+t[i]).normalized;
                    // Supplied UV-degenerate oral islands already have fallback tangents.
                    // Test preservation of the native relationship, not a new global repair.
                    float nativeDot = Vector3.Dot(normals[i].normalized, ((Vector3)tangents[i]).normalized);
                    tangentRelationError = Mathf.Max(tangentRelationError, Mathf.Abs(Vector3.Dot(wn, wt)-nativeDot));
                    if (packet.SourceVertices[i] >= 12178) outside = Mathf.Max(outside, v[i].magnitude);
                }
                Assert.That(error, Is.LessThan(.0027f), "Boy inherited evaluated arm rest offset stays within measured 2.65 mm.");
                Assert.That(blinkRegionError,Is.LessThan(.000001f)); Assert.That(deltaError,Is.LessThan(.000001f),"Selected displacement parity independently of inherited baseline offset.");
                Assert.That(normalAngle, Is.LessThan(.1f), "Closed/intermediate shading must also agree.");
                Assert.That(tangentRelationError, Is.LessThan(.00001f)); Assert.That(outside, Is.Zero, "Oral/eye islands must not move.");
                report.Add($"weight={frame.Weight} absoluteSourceErrorMm={error*1000:R} blinkRegionErrorMm={blinkRegionError*1000:R} deltaErrorMm={deltaError*1000:R} normalAngleDeg={normalAngle:R} tangentRelationError={tangentRelationError:R}");
            }
            Directory.CreateDirectory(Review); File.WriteAllLines(Review+"/native-data-validation.txt", report);
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            // Plain data tests use no scene. Unity tests enter an isolated temporary scene.
            if (!TestContext.CurrentContext.Test.Name.StartsWith("Runtime")) yield break;
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(BoyBlinkPrototypeTools.Prefab), Is.Not.Null);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1), "Preserve additive/user scenes; close them deliberately first.");
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False, "Never discard a dirty scene.");
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            _background = Application.runInBackground; Application.runInBackground = true; _objects = new List<GameObject>();
        }

        [UnityTearDown] public IEnumerator Teardown()
        {
            if (_objects != null) foreach (var go in _objects.AsEnumerable().Reverse()) if (go != null) Object.DestroyImmediate(go);
            if (Application.isPlaying) { Application.runInBackground = _background; yield return new ExitPlayMode(); }
            var path = SessionState.GetString(SceneKey, ""); SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }

        private GameObject Keep(GameObject go) { _objects.Add(go); return go; }
        private HumanBasePrototypeVisual Spawn()
        {
            var parent = Keep(new GameObject("Inactive construction")); parent.SetActive(false);
            var root = Keep(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BoyBlinkPrototypeTools.Prefab), parent.transform));
            root.GetComponent<NavMeshAgent>().enabled = false; root.GetComponent<EmployeeAgent>().enabled = false;
            var visual = root.GetComponent<HumanBasePrototypeVisual>(); visual.enabled = false;
            root.transform.position = Vector3.up*root.GetComponent<NavMeshAgent>().baseOffset; root.transform.SetParent(null, true);
            visual.Animator.Play("Idle", 0, 0); visual.Animator.Update(0); visual.Animator.speed = 0;
            return visual;
        }

        private static Vector3[] Bake(SkinnedMeshRenderer skin, out Vector3[] normals)
        {
            var mesh = new Mesh();
            try
            {
                skin.BakeMesh(mesh, true);
                var p = mesh.vertices; normals = mesh.normals;
                Assert.That(p.All(BoyBlinkPrototypeTools.Finite) && normals.All(BoyBlinkPrototypeTools.Finite), Is.True);
                Assert.That(mesh.tangents.All(t => BoyBlinkPrototypeTools.Finite(t) && float.IsFinite(t.w)), Is.True);
                return p;
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        private Camera CreateCamera()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.38f,.38f,.38f);
            var light = Keep(new GameObject("Blink technical key")).AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 2; light.transform.rotation = Quaternion.Euler(25,-35,0);
            var fill = Keep(new GameObject("Blink technical fill")).AddComponent<Light>(); fill.type = LightType.Directional;
            fill.intensity = .5f; fill.transform.rotation = Quaternion.Euler(10,135,0);
            var camera = Keep(new GameObject("Blink technical camera")).AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f,.17f,.21f);
            camera.orthographic = true; camera.nearClipPlane = .01f; camera.farClipPlane = 10;
            return camera;
        }

        private static Vector3 EyeCentre(HumanBasePrototypeVisual visual)
        {
            var p = Bake(visual.Face, out _); var ids = BoyBlinkPrototypeTools.ReadAddition().SourceVertices;
            Vector3 centre = Vector3.zero; int count = 0;
            for(int i=0;i<p.Length;i++) if(ids[i]>=12178 && ids[i]<13142) { centre += visual.Face.transform.TransformPoint(p[i]); count++; }
            return centre/count;
        }

        private static void FrameCamera(Camera camera, Vector3 centre, string view)
        {
            var target = centre; var offset = new Vector3(0,0,.65f); camera.orthographicSize = .060f;
            if(view=="face") { target -= Vector3.up*.055f; camera.orthographicSize=.18f; }
            if(view=="oblique") { offset = new Vector3(.45f,.06f,.6f); camera.orthographicSize=.09f; }
            camera.transform.position = target+offset; camera.transform.LookAt(target);
        }

        private static void Capture(Camera camera, string name, int width=1000, int height=650)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Review+"/"+name));
            HumanBasePrototypeTools.Capture(camera, "../BoyFoundation/UnityBlink/"+name, width, height);
        }

        private static float AssertSkinnedEndpoint(SkinnedMeshRenderer skin, Vector3[] open, Vector3[] closed)
        {
            var delta=BoyBlinkPrototypeTools.ReadAddition().Frames[19].Positions;
            var binds=skin.sharedMesh.bindposes;
            var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*binds[i]).ToArray();
            using var counts=skin.sharedMesh.GetBonesPerVertex();using var weights=skin.sharedMesh.GetAllBoneWeights();
            int offset=0;float maximum=0;
            for(int i=0;i<open.Length;i++)
            {
                Vector3 expected=Vector3.zero;
                for(int j=0;j<counts[i];j++){var w=weights[offset++];expected+=matrices[w.boneIndex].MultiplyVector(delta[i])*w.weight;}
                var actual=skin.transform.TransformVector(closed[i]-open[i]);
                maximum=Mathf.Max(maximum,Vector3.Distance(actual,expected));
            }
            Assert.That(maximum,Is.LessThan(.000002f),"CPU BakeMesh must reproduce independently skinned selected endpoint, within 0.002 mm.");
            return maximum;
        }

        [UnityTest] public IEnumerator RuntimeWeightsMotionAndHumanoid()
        {
            var visual = Spawn(); var skin = visual.Face; var camera = CreateCamera(); yield return null;
            visual.Animator.Play("Idle",0,0); visual.Animator.Update(0); visual.Animator.speed=0;
            visual.Animator.enabled=false; // Hold identical body matrices for exact neutral/capture comparisons across engine frames.
            var centre=EyeCentre(visual);
            var report=new List<string>();
            var candidateMesh=skin.sharedMesh;
            skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(BoyBlinkPrototypeTools.BaselineMesh);
            yield return null; // Skinned GPU buffers update once per engine frame.
            foreach(var view in new[]{"face","eyes","oblique"}){FrameCamera(camera,centre,view);Capture(camera,"baseline/"+view);}
            skin.sharedMesh=candidateMesh;
            yield return null; // Complete renderer rebinding after the baseline comparison swap.
            var neutral = Bake(skin,out var neutralNormals);
            foreach(int weight in new[]{0,25,50,75,100,0})
            {
                Assert.That(visual.SetShape("BlinkBoth",weight),Is.True); var p=Bake(skin,out var normals);
                if(weight==0){CollectionAssert.AreEqual(neutral,p);CollectionAssert.AreEqual(neutralNormals,normals);}
                if(weight==100)report.Add("Idle endpoint skinning error metres="+AssertSkinnedEndpoint(skin,neutral,p).ToString("R"));
                report.Add($"weight={weight} maxBakedDisplacement={p.Zip(neutral,Vector3.Distance).Max():R} finite=true");
                string label=weight==0&&report.Count>1?"restored":"weight-"+weight.ToString("000");
                yield return null;
                foreach(var view in new[]{"face","eyes","oblique"}){FrameCamera(camera,centre,view);Capture(camera,"stills/"+view+"-"+label);}
            }
            // Smooth key curve passes explicitly through every requested 25% step.
            var curve=new AnimationCurve(Enumerable.Range(0,9).Select(i=>new Keyframe(i*.2f,(i<=4?i:8-i)*25)).ToArray());
            for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);
            var timeline=new List<string>{"frame,time,weight,normalSpeedWeight"};
            for(int f=0;f<=48;f++)
            {
                float time=f/30f, weight=Mathf.Clamp(curve.Evaluate(time),0,100);
                visual.SetShape("BlinkBoth",weight); Bake(skin,out _); FrameCamera(camera,centre,"eyes");
                yield return null;
                Capture(camera,"sweep/frame-"+f.ToString("000"),800,450);
                timeline.Add($"{f},{time.ToString("R",CultureInfo.InvariantCulture)},{weight.ToString("R",CultureInfo.InvariantCulture)},");
            }
            // One deterministic blink, 100 ms closing / 40 ms hold / 160 ms opening.
            for(int f=0;f<=60;f++)
            {
                float t=f/60f-.3f; float w=t<0?0:t<.1f?Mathf.SmoothStep(0,100,t/.1f):t<.14f?100:t<.30f?Mathf.SmoothStep(100,0,(t-.14f)/.16f):0;
                visual.SetShape("BlinkBoth",w); Bake(skin,out _); FrameCamera(camera,centre,"eyes");
                yield return null;
                Capture(camera,"normal-speed/frame-"+f.ToString("000"),800,450);
                timeline.Add($"{f},{(f/60f).ToString("R",CultureInfo.InvariantCulture)},,{w.ToString("R",CultureInfo.InvariantCulture)}");
            }
            CollectionAssert.AreEqual(neutral,Bake(skin,out _)); File.WriteAllLines(Review+"/animation-timeline.csv",timeline);
            foreach(string state in new[]{"Idle","Walk","Run"})
            {
                Vector3[] first=null; float motion=0;
                foreach(float phase in new[]{0f,.25f,.5f,.75f})
                {
                    visual.Animator.enabled=true;visual.Animator.Play(state,0,phase); visual.Animator.Update(0); visual.Animator.speed=1;
                    visual.SetShape("BlinkBoth",100); visual.Animator.Update(.02f); visual.Animator.speed=0;
                    Assert.That(skin.GetBlendShapeWeight(29),Is.EqualTo(100),state+" must not overwrite BlinkBoth.");
                    Assert.That(visual.Animator.GetCurrentAnimatorStateInfo(0).IsName(state),Is.True);
                    visual.Animator.enabled=false;
                    visual.SetShape("BlinkBoth",0); var open=Bake(skin,out _);
                    if(first==null)first=open;else motion=Mathf.Max(motion,open.Zip(first,Vector3.Distance).Max());
                    foreach(int w in new[]{25,50,75,100})
                    {
                        visual.SetShape("BlinkBoth",w); var closed=Bake(skin,out _);
                        Assert.That(closed.Zip(open,Vector3.Distance).Max(),Is.GreaterThan(.00001f));
                        if(w==100)report.Add(state+" phase="+phase+" skinnedEndpointErrorMetres="+AssertSkinnedEndpoint(skin,open,closed).ToString("R"));
                    }
                    yield return null;
                    FrameCamera(camera,EyeCentre(visual),"face");Capture(camera,"body/"+state+"-"+phase.ToString("0.00",CultureInfo.InvariantCulture)+"-blink");
                    visual.SetShape("BlinkBoth",0); CollectionAssert.AreEqual(open,Bake(skin,out _));
                }
                Assert.That(motion,Is.GreaterThan(.00001f),state+" actually deforms the body."); report.Add(state+" maxSampledBodyMotion="+motion.ToString("R")+" animatorKeepsBlink=true");
            }
            File.WriteAllLines(Review+"/runtime-motion-validation.txt",report);
        }

        [UnityTest] public IEnumerator RuntimeLightingDiagnostic()
        {
            var visual=Spawn();var camera=CreateCamera();yield return null;
            visual.Animator.Play("Idle",0,0);visual.Animator.Update(0);visual.Animator.enabled=false;
            var centre=EyeCentre(visual);var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            File.WriteAllLines(Review+"/lighting-diagnostic.txt",lights.Select(l=>l.name+" shadows="+l.shadows+" bias="+l.shadowBias+" normalBias="+l.shadowNormalBias));
            foreach(string lighting in new[]{"default","no-shadows"})
            {
                if(lighting=="no-shadows")foreach(var light in lights)light.shadows=LightShadows.None;
                foreach(int w in new[]{0,100})
                {
                    visual.SetShape("BlinkBoth",w);Bake(visual.Face,out _);yield return null;
                    foreach(var view in new[]{"eyes","oblique"}){FrameCamera(camera,centre,view);Capture(camera,"lighting/"+lighting+"-"+view+"-"+w);}
                }
            }
            visual.ResetShapes();
        }

        [UnityTest] public IEnumerator RuntimeValidLowerFaceCoexists()
        {
            var visual=Spawn(); var camera=CreateCamera(); yield return null;
            visual.Animator.Play("Idle",0,0);visual.Animator.Update(0);visual.Animator.speed=0;
            visual.Animator.enabled=false;
            var neutral=Bake(visual.Face,out _);var centre=EyeCentre(visual);
            visual.SetShape("BlinkBoth",100);var blink=Bake(visual.Face,out _);visual.SetShape("BlinkBoth",0);
            var lines=new List<string>();
            foreach(var names in new[]{new[]{"mouthSmileLeft","mouthSmileRight"},new[]{"jawOpen"}})
            {
                visual.ResetShapes();foreach(var name in names)Assert.That(visual.SetShape(name,100),Is.True);
                var lower=Bake(visual.Face,out _);Assert.That(lower.Zip(neutral,Vector3.Distance).Max(),Is.GreaterThan(.00001f));
                visual.SetShape("BlinkBoth",100);var combined=Bake(visual.Face,out _);float error=0;
                for(int i=0;i<combined.Length;i++)error=Mathf.Max(error,Vector3.Distance(combined[i]-lower[i],blink[i]-neutral[i]));
                Assert.That(error,Is.LessThan(.000001f),"Both independent supplied/selected deltas must coexist without extra deformation.");
                string label=names.Length==2?"smile":"jaw-open";
                yield return null;
                foreach(var view in new[]{"face","eyes","oblique"}){FrameCamera(camera,centre,view);Capture(camera,"coexist/"+label+"-"+view);}
                lines.Add(label+" additiveError="+error.ToString("R")+" finite=true");
                visual.ResetShapes();CollectionAssert.AreEqual(neutral,Bake(visual.Face,out _));
            }
            File.WriteAllLines(Review+"/lower-face-validation.txt",lines);
        }
    }

}
