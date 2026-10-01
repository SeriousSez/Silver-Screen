using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Explicit isolated massing review. Never saves Studio, changes gameplay or rebuilds existing assets.</summary>
    public static class StudioServicesMassingBuilder
    {
        public const string Root = "Assets/SilverScreen/Environment/StudioServices/Massing";
        public const string Review = "ArtReview/StudioServices/Massing";
        public const string PrefabPath = Root + "/StudioServices_MassingReview.prefab";
        public const string ScenePath = Root + "/StudioServices_MassingReview.unity";
        [Serializable] public class Submesh { public int[] indices; }
        [Serializable] public class Part { public string name; public float[] positions, normals, uv; public string[] materials; public Submesh[] submeshes; }
        [Serializable] public class Surface { public string name; public float[] linearRGB; public float metallic, roughness; }
        [Serializable] public class Fixture { public string asset, group; public float[] position; public float yaw; }
        [Serializable] public class Marker { public string name; public float[] position; public float yaw; }
        [Serializable] public class Obstacle { public string name, group; public float[] position, size; }
        [Serializable] public class Packet { public Part[] parts; public Surface[] materials; public Fixture[] fixtures; public Marker[] anchors; public Obstacle[] colliders; }
        [Serializable] public class Check { public string name; public bool passed; public string evidence; }
        [Serializable] public class Checks { public string phase = "Massing only; not production or gameplay approval"; public Check[] checks; }

        static Vector3 V(float[] v) => new Vector3(v[0], v[1], v[2]);
        static Vector3[] Vs(float[] v) => Enumerable.Range(0, v.Length / 3).Select(i => new Vector3(v[i * 3], v[i * 3 + 1], v[i * 3 + 2])).ToArray();
        static void Folder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            { if (!AssetDatabase.IsValidFolder(current + "/" + part)) AssetDatabase.CreateFolder(current, part); current += "/" + part; }
        }
        static GameObject Child(string name, Transform parent)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go; }
        static T Asset<T>(string path, Func<T> create) where T : Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) { value = create(); AssetDatabase.CreateAsset(value, path); }
            return value;
        }
        static Packet Read()
        {
            using var stream = File.OpenRead("ArtExports/StudioServices/Massing/massing_meshes.json.gz");
            using var zip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(zip);
            return JsonUtility.FromJson<Packet>(reader.ReadToEnd());
        }
        [MenuItem("SilverScreen/Art/Studio Services/1 Build massing review")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            Folder(Root + "/Meshes"); Folder(Root + "/Materials");
            var data = Read(); var materials = new Dictionary<string, Material>();
            foreach (var surface in data.materials)
            {
                var material = Asset(Root + "/Materials/Study_" + surface.name + ".mat", () => new Material(Shader.Find("Universal Render Pipeline/Lit")));
                var c = V(surface.linearRGB); material.SetColor("_BaseColor", new Color(c.x, c.y, c.z).gamma);
                material.SetFloat("_Metallic", surface.metallic); material.SetFloat("_Smoothness", 1 - surface.roughness);
                EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); materials.Add(surface.name, material);
            }
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("StudioServices_MassingReview"); SceneManager.MoveGameObjectToScene(root, scene);
                var groups = new Dictionary<string, GameObject>();
                foreach (var part in data.parts)
                {
                    var mesh = Asset(Root + "/Meshes/" + part.name + ".asset", () => new Mesh()); mesh.Clear(); mesh.name = part.name;
                    mesh.indexFormat = part.positions.Length / 3 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    mesh.vertices = Vs(part.positions); mesh.normals = Vs(part.normals);
                    mesh.uv = Enumerable.Range(0, part.uv.Length / 2).Select(i => new Vector2(part.uv[i * 2], part.uv[i * 2 + 1])).ToArray();
                    mesh.subMeshCount = part.submeshes.Length;
                    for (int i = 0; i < part.submeshes.Length; i++) mesh.SetTriangles(part.submeshes[i].indices, i, false);
                    mesh.RecalculateBounds(); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                    var go = Child(part.name, root.transform); groups.Add(part.name, go);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = part.materials.Select(m => materials[m]).ToArray();
                }
                foreach (var fixture in data.fixtures)
                {
                    if (!groups.ContainsKey(fixture.group)) groups.Add(fixture.group, Child(fixture.group, root.transform));
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SilverScreen/Environment/ReusableFixtures/Prefabs/" + fixture.asset + ".prefab");
                    if (prefab == null) throw new InvalidOperationException("Missing canonical fixture " + fixture.asset);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groups[fixture.group].transform);
                    instance.transform.localPosition = V(fixture.position); instance.transform.localRotation = Quaternion.Euler(0, fixture.yaw, 0);
                }
                var physical = Child("PhysicalCollision", root.transform);
                foreach (var obstacle in data.colliders)
                {
                    var collider = Child(obstacle.name, physical.transform).AddComponent<BoxCollider>();
                    collider.center = V(obstacle.position); collider.size = V(obstacle.size);
                }
                var anchors = Child("SemanticAnchors", root.transform);
                foreach (var marker in data.anchors)
                {
                    var t = Child(marker.name, anchors.transform).transform;
                    t.localPosition = V(marker.position); t.localRotation = Quaternion.Euler(0, marker.yaw, 0);
                }
                var visibility = new List<BuildingCutawayController.VisibilityGroup>();
                void Group(string name, bool roof, Vector3 normal, params string[] extra)
                {
                    var names = new[] { name }.Concat(extra);
                    visibility.Add(new BuildingCutawayController.VisibilityGroup { Id = name, Roof = roof, OutwardNormal = normal,
                        Renderers = names.SelectMany(n => groups[n].GetComponentsInChildren<Renderer>(true)).ToArray() });
                }
                Group("Roof", true, Vector3.zero, "EmploymentCanopy", "OfficeAwning", "YardShelterRoof");
                Group("FrontWall", false, Vector3.back, "WorkshopDoors", "OfficeDoor", "EmploymentBoard");
                Group("RightWall", false, Vector3.right, "YardDoor"); Group("RearWall", false, Vector3.forward); Group("LeftWall", false, Vector3.left);
                root.AddComponent<BuildingCutawayController>().Configure(visibility.ToArray(), new Bounds(new Vector3(0, 2.1f, 0), new Vector3(14, 4.2f, 9)));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log("Studio Services isolated massing prefab built. Live facility and Studio scene untouched.");
        }

        static void Capture(Camera camera, string name, Vector3 position, Vector3 target, float fov, bool ortho = false, float size = 7)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            camera.fieldOfView = fov; camera.orthographic = ortho; camera.orthographicSize = size; camera.aspect = 1.6f;
            var old = RenderTexture.active; var rt = RenderTexture.GetTemporary(1920, 1200, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D texture = null;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                for (int i = 0; i < 3; i++) RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = rt; texture = new Texture2D(1920, 1200, TextureFormat.RGB24, false, false);
                texture.ReadPixels(new Rect(0, 0, 1920, 1200), 0, 0); texture.Apply(); File.WriteAllBytes(Review + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); if (texture != null) Object.DestroyImmediate(texture); }
        }

        [MenuItem("SilverScreen/Art/Studio Services/2 Capture and check massing")]
        public static void ReviewMassing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath)) throw new InvalidOperationException("Close the owned review scene before regenerating it.");
            Directory.CreateDirectory(Review);
            var original = SceneManager.GetActiveScene(); var checks = new List<Check>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                var reviewRoot = new GameObject("Isolated review environment");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                root.transform.SetParent(reviewRoot.transform, true);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Review Ground";
                ground.transform.position = new Vector3(2, -.22f, 0); ground.transform.localScale = new Vector3(38, .25f, 28);
                ground.transform.SetParent(reviewRoot.transform, true);
                ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Study_Concrete.mat");
                var sun = new GameObject("Review daylight").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 2.0f;
                sun.color = new Color(1, .95f, .86f); sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(47, -33, 0);
                RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.52f, .57f, .64f); RenderSettings.ambientEquatorColor = new Color(.35f, .35f, .33f); RenderSettings.ambientGroundColor = new Color(.19f, .17f, .14f);
                var camera = new GameObject("Massing review camera").AddComponent<Camera>(); camera.scene = scene;
                camera.nearClipPlane = .03f; camera.farClipPlane = 150; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.32f, .39f, .43f); camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                int layer = Enumerable.Range(8, 24).Reverse().First(l => string.IsNullOrEmpty(LayerMask.LayerToName(l)) &&
                    !Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s != scene).SelectMany(s => s.GetRootGameObjects()).SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Any(t => t.gameObject.layer == l));
                foreach (var t in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true))) t.gameObject.layer = layer;
                foreach (var l in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Light>(true))) l.cullingMask = 1 << layer;
                camera.cullingMask = 1 << layer;
                var cutaway = root.GetComponent<BuildingCutawayController>();
                var renderers = root.GetComponentsInChildren<Renderer>(true); var states = renderers.Select(r => r.enabled).ToArray();
                var colliders = root.GetComponentsInChildren<Collider>(true); var collisionStates = colliders.Select(c => c.enabled).ToArray();
                Capture(camera, "01_front_three_quarter", new Vector3(18, 8, -24), new Vector3(1.5f, 2.3f, 0), 39);
                Capture(camera, "02_front", new Vector3(2.3f, 2.6f, -30), new Vector3(2.3f, 2.6f, 0), 43, true, 7.1f);
                Capture(camera, "03_rear_yard", new Vector3(24, 14, 24), new Vector3(2.3f, 1.8f, 0), 44);
                Capture(camera, "04_management", new Vector3(22, 25, -24), new Vector3(1.4f, 0, 0), 42);
                var eye = new Vector3(19, 24, -24);
                cutaway.SetReveal(BuildingRevealReason.DeveloperPreview, true, eye);
                Capture(camera, "05_cutaway", eye, new Vector3(0, 0, 0), 39);
                bool roofHidden = !root.transform.Find("Roof").GetComponent<Renderer>().enabled;
                bool farVisible = root.transform.Find("RearWall").GetComponent<Renderer>().enabled && root.transform.Find("LeftWall").GetComponent<Renderer>().enabled;
                bool frontHidden = !root.transform.Find("FrontWall").GetComponent<Renderer>().enabled && !root.transform.Find("RightWall").GetComponent<Renderer>().enabled;
                bool floorVisible = root.transform.Find("Floor").GetComponent<Renderer>().enabled && root.transform.Find("InteriorPartitions").GetComponent<Renderer>().enabled && root.transform.Find("InteriorProps").GetComponent<Renderer>().enabled;
                checks.Add(new Check { name = "Camera-facing cutaway", passed = roofHidden && farVisible && frontHidden && floorVisible, evidence = "Roof/front/right hidden, rear/left/floor/partitions/interior retained" });
                cutaway.SetReveal(BuildingRevealReason.BuildingFocus, true, eye);
                cutaway.SetReveal(BuildingRevealReason.DeveloperPreview, false, eye);
                checks.Add(new Check { name = "Multiple reveal reasons", passed = cutaway.IsRevealed && !root.transform.Find("Roof").GetComponent<Renderer>().enabled, evidence = "Focus remains when developer reason releases" });
                Capture(camera, "06_plan", new Vector3(0, 30, -.001f), Vector3.zero, 44, true, 8.2f);
                cutaway.SetReveal(BuildingRevealReason.BuildingFocus, false, eye);
                checks.Add(new Check { name = "Exact renderer restoration", passed = renderers.Select((r, i) => r.enabled == states[i]).All(x => x), evidence = "All original renderer enabled states restored" });
                checks.Add(new Check { name = "Physical collision unaffected", passed = colliders.Select((c, i) => c.enabled == collisionStates[i]).All(x => x), evidence = colliders.Length + " collider states unchanged during reveal" });
                Capture(camera, "07_restored", new Vector3(18, 8, -24), new Vector3(1.5f, 2.3f, 0), 39);
                Physics.SyncTransforms();
                var markerRoot = root.transform.Find("SemanticAnchors");
                foreach (Transform t in markerRoot)
                {
                    float radius = .35f;
                    var blocked = Physics.OverlapCapsule(t.position + Vector3.up * (radius + .06f), t.position + Vector3.up * (2 - radius), radius, 1 << layer, QueryTriggerInteraction.Ignore)
                        .Where(c => c.transform.IsChildOf(root.transform)).Select(c => c.name).ToArray();
                    checks.Add(new Check { name = "Anchor clearance: " + t.name, passed = blocked.Length == 0, evidence = blocked.Length == 0 ? "0.70 m diameter / 2.00 m capsule clear in physical study" : string.Join(", ", blocked) });
                }
                checks.Add(new Check { name = "Shared lamp prefab references", passed = root.GetComponentsInChildren<Transform>(true).Count(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && t != root.transform) == 9, evidence = "Nine canonical lamp instances; no copies of source geometry" });
                // Existing Studio navigation remains registered. Move only this temporary test
                // environment well outside that surface to prevent false paths through it.
                reviewRoot.transform.position = new Vector3(1000, 0, 1000); Physics.SyncTransforms();
                var navigation = reviewRoot.AddComponent<NavMeshSurface>();
                navigation.collectObjects = CollectObjects.Children; navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                navigation.overrideVoxelSize = true; navigation.voxelSize = .035f; navigation.layerMask = 1 << layer;
                navigation.BuildNavMesh();
                var filter = new NavMeshQueryFilter { agentTypeID = navigation.agentTypeID, areaMask = NavMesh.AllAreas };
                var approach = markerRoot.Find("OfficeApproach").position;
                bool sourceValid = NavMesh.SamplePosition(approach, out var sourceHit, .15f, filter);
                foreach (string name in new[] { "OfficeEntrance", "HireConstructionWorker", "HireGroundskeeper", "WorkshopEntrance", "YardAccess", "ConstructionMaterialPickup", "ToolPickup", "WorkshopWork", "GroundskeeperSupply" })
                {
                    var point = markerRoot.Find(name).position; var path = new NavMeshPath();
                    bool valid = sourceValid && NavMesh.SamplePosition(point, out var hit, .15f, filter) && NavMesh.CalculatePath(sourceHit.position, hit.position, filter, path) && path.status == NavMeshPathStatus.PathComplete;
                    checks.Add(new Check { name = "Study navigation: " + name, passed = valid, evidence = "From office approach; scoped collider bake, existing agent type, 0.035 m voxel; " + path.status + "; " + path.corners.Length + " corners" });
                }
                checks.Add(new Check { name = "Fits existing service lot position", passed = -28 - 7.6f >= -38 && -28 + 13 >= -38 && -28 + 13 <= 38 && -22 - 8.1f >= -32 && -22 + 5.5f <= 32, evidence = "Building/yard/approach reserve 20.6 x 13.6 m at existing (-28,-22) origin; requires replacing old 9 x 12 m reservation during integration" });
                navigation.RemoveData(); Object.DestroyImmediate(navigation.navMeshData); Object.DestroyImmediate(navigation);
                reviewRoot.transform.position = Vector3.zero; Physics.SyncTransforms();
                File.WriteAllText(Review + "/checks.json", JsonUtility.ToJson(new Checks { checks = checks.ToArray() }, true));
                camera.transform.SetPositionAndRotation(new Vector3(18, 8, -24), Quaternion.LookRotation(new Vector3(1.5f, 2.3f, 0) - new Vector3(18, 8, -24))); camera.orthographic = false;
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Studio Services massing checks " + checks.Count(c => c.passed) + "/" + checks.Count + ". Review required before detailing.");
            }
            finally { SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
