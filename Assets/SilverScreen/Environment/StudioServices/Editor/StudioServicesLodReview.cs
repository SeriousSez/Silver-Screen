using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Matched URP captures, actual camera-distance sweep and render representation metrics.</summary>
    public static class StudioServicesLodReview
    {
        public const string Output = "ArtReview/StudioServices/LOD";
        [Serializable] public class Level { public int level, triangles, vertices, renderers, materialSlots, uniqueMaterials, shadowCasters; public float threshold; }
        [Serializable] public class Report { public string unity; public float lodBias, referenceSize; public Vector3 referencePoint; public Level[] levels; public bool physicsUnchanged, anchorsUnchanged, cutawayRestored, allLevelsRespectReveal; public int canonicalVariants; }
        [Serializable] public class Frame { public int frame; public float distance, yaw, pitch; public int triangles, drawCalls, setPassCalls; public double renderMilliseconds; }
        [Serializable] public class Sweep { public string evidence; public Frame[] frames; }
        static Camera camera;
        static GameObject root;
        static Scene scene, original;
        static int layer, frame;
        static readonly List<Frame> frames = new List<Frame>();

        static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            Directory.CreateDirectory(Output);
            original = SceneManager.GetActiveScene();
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            layer = Enumerable.Range(8, 24).Reverse().First(l => string.IsNullOrEmpty(LayerMask.LayerToName(l)) && !Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s != scene).SelectMany(s => s.GetRootGameObjects()).SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Any(t => t.gameObject.layer == l));
            root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath), scene);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            var sun = new GameObject("LOD review sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 2; sun.color = new Color(1, .95f, .86f); sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(47, -33, 0); sun.cullingMask = 1 << layer;
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.52f, .57f, .64f); RenderSettings.ambientEquatorColor = new Color(.35f, .35f, .33f); RenderSettings.ambientGroundColor = new Color(.19f, .17f, .14f);
            camera = new GameObject("LOD review camera").AddComponent<Camera>(); camera.scene = scene; camera.cullingMask = 1 << layer; camera.nearClipPlane = .03f; camera.farClipPlane = 400; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.24f, .28f, .30f);
            var data = camera.GetUniversalAdditionalCameraData(); data.volumeLayerMask = 1 << layer; data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
        static void End()
        {
            EditorApplication.update -= SweepFrame;
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            camera = null; root = null;
        }
        static void Shot(string folder, string name, Vector3 position, Vector3 target, float fov = 39)
        { Directory.CreateDirectory(Output + "/" + folder); StudioServicesProductionReview.Capture(camera, Output + "/" + folder + "/" + name + ".png", position, target, fov); }

        public static Level Measure(int level, Renderer[] renderers, float threshold)
        {
            var filters = renderers.Select(r => r.GetComponent<MeshFilter>()).Where(f => f != null).ToArray();
            return new Level { level = level, threshold = threshold, triangles = filters.Sum(f => f.sharedMesh.triangles.Length/3), vertices = filters.Sum(f => f.sharedMesh.vertexCount), renderers = renderers.Length, materialSlots = renderers.Sum(r => r.sharedMaterials.Length), uniqueMaterials = renderers.SelectMany(r => r.sharedMaterials).Distinct().Count(), shadowCasters = renderers.Count(r => r.shadowCastingMode != ShadowCastingMode.Off) };
        }
        [MenuItem("SilverScreen/Art/Studio Services/8 Capture and validate LODs")]
        public static void Capture() => Run();
        public static void Run(bool baseline = false)
        {
            Begin();
            try
            {
                var group = root.GetComponent<LODGroup>();
                int count = baseline ? 1 : group.lodCount;
                var cut = root.GetComponent<BuildingCutawayController>();
                var colliders = root.GetComponentsInChildren<Collider>(true); var enabled = colliders.Select(c => c.enabled).ToArray();
                var anchors = root.transform.Find("SemanticAnchors").Cast<Transform>().ToArray(); var poses = anchors.Select(t => t.localToWorldMatrix).ToArray();
                bool respects = true, restored = true;
                for (int i = 0; i < count; i++)
                {
                    if (group != null) group.ForceLOD(i);
                    string folder = baseline ? "Baseline" : "LOD" + i;
                    Shot(folder, "front", new Vector3(18,8,-24), new Vector3(1.5f,2.3f,0));
                    Shot(folder, "management", new Vector3(22,25,-24), new Vector3(1.4f,0,0),42);
                    Shot(folder, "rear", new Vector3(-18,13,24), new Vector3(0,2,0),42);
                    Shot(folder, "yard", new Vector3(22,12,15), new Vector3(8,1.6f,0),48);
                    Shot(folder, "distant", new Vector3(44,46,-58), new Vector3(1.4f,0,0),42);
                    Shot(folder, "gameplay", new Vector3(1.4f,2.1f,0)+Quaternion.Euler(52,-40,0)*new Vector3(0,0,-22),new Vector3(1.4f,2.1f,0),60);
                    Shot(folder, "close", new Vector3(-6,3,-11),new Vector3(-3,1.8f,-4.5f),60);
                    var rs = root.GetComponentsInChildren<Renderer>(true); var state = rs.Select(r=>r.enabled).ToArray();
                    cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,true,new Vector3(22,25,-24));
                    cut.SetReveal(BuildingRevealReason.BuildingFocus,true,new Vector3(22,25,-24));
                    respects &= cut.Groups.Where(g=>g.Roof || g.Id=="FrontWall" || g.Id=="RightWall").All(g=>g.Renderers.All(r=>!r.enabled));
                    Shot(folder, "cutaway",new Vector3(22,25,-24),new Vector3(0,0,0),42);
                    cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,false,new Vector3(22,25,-24));
                    respects &= cut.IsRevealed;
                    cut.Restore(); restored &= rs.Select((r,n)=>r.enabled==state[n]).All(v=>v);
                }
                if (!baseline)
                {
                    var lods = group.GetLODs();
                    var report = new Report { unity=Application.unityVersion, lodBias=QualitySettings.lodBias, referenceSize=group.size, referencePoint=group.localReferencePoint,
                        levels=lods.Select((l,i)=>Measure(i,l.renderers,l.screenRelativeTransitionHeight)).ToArray(),
                        physicsUnchanged=colliders.Select((c,i)=>c.enabled==enabled[i]).All(v=>v), anchorsUnchanged=anchors.Select((a,i)=>a.localToWorldMatrix==poses[i]).All(v=>v), cutawayRestored=restored, allLevelsRespectReveal=respects,
                        canonicalVariants=AssetDatabase.FindAssets("t:Prefab",new[]{StudioServicesLodBuilder.KitRoot+"/Prefabs"}).Length };
                    File.WriteAllText(Output+"/metrics.json",JsonUtility.ToJson(report,true));
                    if (!report.physicsUnchanged || !report.anchorsUnchanged || !restored || !respects) throw new InvalidOperationException("LOD/cutaway validation failed.");
                }
            }
            finally { End(); }
        }
        // One rendered frame per editor update: tests automatic camera selection,
        // including rotation and repeated rapid zoom reversal, not just ForceLOD.
        public static void StartSweep()
        {
            Begin(); root.GetComponent<LODGroup>().ForceLOD(-1); frame=0; frames.Clear();
            Directory.CreateDirectory(Output+"/Sweep"); EditorApplication.update += SweepFrame;
        }
        static void SweepFrame()
        {
            try
            {
                float t=frame/119f;
                float distance=frame<60 ? Mathf.Lerp(12,100,frame/59f) : Mathf.Lerp(12,100,Mathf.PingPong((frame-60)/10f,1));
                float yaw=360*t, pitch=15+37*Mathf.Sin(t*Mathf.PI);
                Vector3 target=new Vector3(1.4f,2.1f,0);
                Vector3 position=target+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);
                var cut=root.GetComponent<BuildingCutawayController>();
                if(frame>=90&&frame<110)cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,true,position);else cut.Restore();
                var watch=System.Diagnostics.Stopwatch.StartNew();
                Shot("Sweep",frame.ToString("D3"),position,target,60);watch.Stop();
                frames.Add(new Frame{frame=frame,distance=distance,yaw=yaw,pitch=pitch,triangles=UnityStats.triangles,drawCalls=UnityStats.drawCalls,setPassCalls=UnityStats.setPassCalls,renderMilliseconds=watch.Elapsed.TotalMilliseconds});
                if(++frame<120)return;
                File.WriteAllText(Output+"/sweep.json",JsonUtility.ToJson(new Sweep{evidence="120 real URP camera renders over separate Editor updates. UnityStats are Editor observations, not isolated player/GPU timings; render duration includes readback and PNG encoding.",frames=frames.ToArray()},true));
                Debug.Log("Studio Services automatic LOD sweep complete: 120 frames.");End();
            }
            catch(Exception e){Debug.LogException(e);End();}
        }
    }
}
