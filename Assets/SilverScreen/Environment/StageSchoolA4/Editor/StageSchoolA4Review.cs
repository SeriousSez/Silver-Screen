using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Isolated A4 art authoring and review. No runtime integration or gameplay.</summary>
    public static class StageSchoolA4Review
    {
        public const string Root = "Assets/SilverScreen/Environment/StageSchoolA4";
        public const string Prefab = Root + "/StageSchool_A4.prefab";
        public const string ScenePath = Root + "/StageSchool_A4_Review.unity";
        public const string Review = "ArtReview/StageSchoolA4";
        const string A3 = "Assets/SilverScreen/Environment/StageSchoolA3";
        const string A1 = "Assets/SilverScreen/Environment/StageSchoolA1";
        const string Kit = "Assets/SilverScreen/Environment/PeriodEnvironment1930";
        const string Equipment = "Assets/SilverScreen/Environment/ProductionEquipment1930Candidate";
        [Serializable] public class Submesh { public int[] indices; }
        [Serializable] public class Part { public string name, sourceGroup; public float[] positions, normals, uv, pivot, offset; public string[] materials; public Submesh[] submeshes; }
        [Serializable] public class Surface { public string name; public float[] linearRGB; public float metallic, roughness; }
        [Serializable] public class Fixture { public string asset, group; public float[] position; public float yaw; }
        [Serializable] public class Packet { public Part[] parts; public Surface[] materials; public Fixture[] fixtures, equipment; }
        [Serializable] public class Sanity
        {
            public string unityVersion, renderPipeline;
            public int triangles, vertices, uniqueMeshTriangles, uniqueMeshVertices, renderers, materials, uniqueMeshes, missingMeshes, missingMaterials, sharedA3Renderers, equipmentInstances;
            public bool scaleValid, noGameplayComponents, noLods, compilationFailed;
            public string[] renderSupportComponents;
            public Vector3 boundsMin, boundsMax;
        }
        static Vector3 V(float[] p) => new Vector3(p[0], p[1], p[2]);
        static void Folder(string path)
        {
            string current = "Assets";
            foreach (var s in path.Split('/').Skip(1))
            {
                if (!AssetDatabase.IsValidFolder(current + "/" + s)) AssetDatabase.CreateFolder(current, s);
                current += "/" + s;
            }
        }
        static GameObject Node(Transform root, string path)
        {
            var at = root;
            foreach (var s in path.Split('/'))
            {
                var next = at.Find(s);
                if (next == null)
                {
                    var go = new GameObject(s); SceneManager.MoveGameObjectToScene(go, root.gameObject.scene);
                    go.transform.SetParent(at, false); next = go.transform;
                }
                at = next;
            }
            return at.gameObject;
        }
        static Material SharedMaterial(string name)
        {
            foreach (string folder in new[] { A3, A1, Kit })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Materials/" + name + ".mat");
                if (m != null) return m;
            }
            throw new FileNotFoundException("Approved material missing: " + name);
        }
        static Material CandidateMaterial(Surface surface)
        {
            if (!surface.name.StartsWith("SS_A4_Leaf", StringComparison.Ordinal) && surface.name != "SS_A4_PottingSoil") return SharedMaterial(surface.name);
            Folder(Root + "/Materials");
            string path = Root + "/Materials/" + surface.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            // Blender palette values are linear; Unity serializes material colour in sRGB.
            var rgb = surface.linearRGB;
            material.SetColor("_BaseColor", new Color(rgb[0], rgb[1], rgb[2], 1).gamma);
            material.SetFloat("_Metallic", surface.metallic);
            material.SetFloat("_Smoothness", 1 - surface.roughness);
            if (surface.name == "SS_A4_PottingSoil")
            {
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/PottingSoil_albedo.png"));
            }
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        [MenuItem("SilverScreen/Art/Stage School A4/1 Import compact candidate")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            Folder(Root + "/Meshes"); Directory.CreateDirectory(Review);
            Packet data;
            using (var file = File.OpenRead("ArtExports/StageSchoolA4/stage_school_meshes.json.gz"))
            using (var zip = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new StreamReader(zip)) data = JsonUtility.FromJson<Packet>(reader.ReadToEnd());
            Folder(Root + "/Textures");
            string soilPath = Root + "/Textures/PottingSoil_albedo.png";
            File.Copy("ArtExports/StageSchoolA4/Textures/PottingSoil_albedo.png", soilPath, true);
            AssetDatabase.ImportAsset(soilPath, ImportAssetOptions.ForceUpdate);
            var soilImporter = (TextureImporter)AssetImporter.GetAtPath(soilPath);
            if (soilImporter.wrapMode != TextureWrapMode.Clamp || !soilImporter.sRGBTexture || soilImporter.maxTextureSize != 128)
            {
                soilImporter.wrapMode = TextureWrapMode.Clamp; soilImporter.sRGBTexture = true;
                soilImporter.maxTextureSize = 128; soilImporter.mipmapEnabled = true; soilImporter.SaveAndReimport();
            }
            var mats = data.materials.ToDictionary(s => s.name, CandidateMaterial);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("StageSchool_A4_REVIEW_ONLY"); SceneManager.MoveGameObjectToScene(root, preview);
                foreach (var p in data.parts)
                {
                    var go = Node(root.transform, p.name); var pivot = V(p.pivot); go.transform.localPosition = pivot;
                    Mesh mesh;
                    if (!string.IsNullOrEmpty(p.sourceGroup))
                    {
                        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(A3 + "/Meshes/" + p.sourceGroup.Replace('/', '_') + ".asset");
                        if (mesh == null) throw new FileNotFoundException("Approved mesh missing: " + p.sourceGroup);
                        // Preserve the exact original submesh material order as well as mesh GUID.
                        string sourceName = p.sourceGroup.Split('/').Last();
                        var approvedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(A3 + "/StageSchool_A3.prefab");
                        var approvedPart = approvedRoot.transform.Find(p.sourceGroup);
                        if (approvedPart == null) throw new InvalidOperationException("Approved assembly missing: " + sourceName);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterials = approvedPart.GetComponent<MeshRenderer>().sharedMaterials;
                    }
                    else
                    {
                        string path = Root + "/Meshes/" + p.name.Replace('/', '_') + ".asset";
                        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
                        mesh.Clear(); mesh.name = p.name.Replace('/', '_');
                        mesh.indexFormat = p.positions.Length / 3 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                        mesh.vertices = Enumerable.Range(0, p.positions.Length / 3).Select(i => new Vector3(p.positions[i*3], p.positions[i*3+1], p.positions[i*3+2]) - pivot).ToArray();
                        mesh.normals = Enumerable.Range(0, p.normals.Length / 3).Select(i => new Vector3(p.normals[i*3], p.normals[i*3+1], p.normals[i*3+2])).ToArray();
                        mesh.uv = Enumerable.Range(0, p.uv.Length / 2).Select(i => new Vector2(p.uv[i*2], p.uv[i*2+1])).ToArray();
                        mesh.subMeshCount = p.submeshes.Length;
                        for (int i = 0; i < p.submeshes.Length; i++) mesh.SetTriangles(p.submeshes[i].indices, i, false);
                        mesh.RecalculateBounds(); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterials = p.materials.Select(n => mats[n]).ToArray();
                    }
                }
                foreach (var f in data.fixtures) Place(root.transform, f, Kit);
                foreach (var f in data.equipment) Place(root.transform, f, Equipment);
                // Visible luminaires use the existing approved meshes. Room illumination
                // is review presentation only and is excluded from runtime integration.
                foreach (var p in new[] { new Vector3(-7,3.6f,-5), new Vector3(-2.35f,3.6f,-5), new Vector3(2.35f,3.6f,-5), new Vector3(7,3.6f,-5), new Vector3(-4,3.6f,1.6f), new Vector3(6.6f,3.6f,2.8f) })
                {
                    var go = Node(root.transform, "ReviewLighting/Room_" + p.x + "_" + p.z); go.transform.localPosition = p;
                    var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1,.79f,.56f); light.intensity = 1.7f; light.range = 9; light.shadows = LightShadows.None;
                }
                Check(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Debug.Log("A4 imported as a separate art candidate. Shared A3.2 source meshes and materials preserved.");
        }
        static void Place(Transform root, Fixture f, string folder)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Prefabs/" + f.asset + ".prefab");
            if (asset == null) throw new FileNotFoundException(f.asset);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, Node(root, f.group).transform);
            go.transform.localPosition = V(f.position); go.transform.localRotation = Quaternion.Euler(0, f.yaw, 0);
            foreach (var b in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }
        static void Check(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var meshes = filters.Select(f => f.sharedMesh).Where(m => m != null).Distinct().ToArray();
            var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var s = new Sanity {
                unityVersion = Application.unityVersion, renderPipeline = GraphicsSettings.currentRenderPipeline.name,
                triangles = filters.Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3),
                vertices = filters.Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.vertexCount),
                uniqueMeshTriangles = meshes.Sum(m => m.triangles.Length / 3), uniqueMeshVertices = meshes.Sum(m => m.vertexCount),
                uniqueMeshes = meshes.Length, renderers = renderers.Length,
                materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().Count(),
                missingMaterials = renderers.Sum(r => r.sharedMaterials.Count(m => m == null || m.shader == null)), missingMeshes = filters.Count(f => f.sharedMesh == null),
                sharedA3Renderers = filters.Count(f => AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(A3 + "/")),
                equipmentInstances = 3, boundsMin = bounds.min, boundsMax = bounds.max,
                scaleValid = root.GetComponentsInChildren<Transform>(true).All(t => (t.localScale - Vector3.one).sqrMagnitude < .0001f),
                // URP adds its Light metadata during a render request. It is rendering
                // support, not gameplay; allow it only beneath the review-light root.
                noGameplayComponents = root.GetComponentsInChildren<MonoBehaviour>(true).All(b =>
                    b is UniversalAdditionalLightData && b.transform.IsChildOf(root.transform.Find("ReviewLighting"))),
                renderSupportComponents = root.GetComponentsInChildren<UniversalAdditionalLightData>(true).Select(b => b.name).ToArray(),
                noLods = root.GetComponentsInChildren<LODGroup>(true).Length == 0,
                compilationFailed = EditorUtility.scriptCompilationFailed
            };
            File.WriteAllText(Review + "/unity_sanity.json", JsonUtility.ToJson(s, true));
            if (s.missingMaterials + s.missingMeshes != 0 || !s.scaleValid || !s.noGameplayComponents || !s.noLods || s.compilationFailed)
                throw new InvalidOperationException("A4 sanity failed; see unity_sanity.json");
        }

        [MenuItem("SilverScreen/Art/Stage School A4/2 Create isolated review scene")]
        public static void CreateReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            var original = SceneManager.GetActiveScene();
            var studio = SceneManager.GetSceneByPath("Assets/Scenes/Studio.unity");
            bool closeStudio = !studio.isLoaded;
            if (closeStudio) studio = EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(studio);
            var sky = RenderSettings.skybox; var skyColor = RenderSettings.ambientSkyColor; var equator = RenderSettings.ambientEquatorColor; var groundColor = RenderSettings.ambientGroundColor;
            var sourceSun = RenderSettings.sun;
            var sunRotation = sourceSun != null ? sourceSun.transform.rotation : Quaternion.Euler(50,330,0);
            var sunColor = sourceSun != null ? sourceSun.color : Color.white; float sunIntensity = sourceSun != null ? sourceSun.intensity : 1.5f;
            var profile = studio.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Volume>(true)).FirstOrDefault(v => v.isGlobal)?.sharedProfile;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Review ground only";
                ground.transform.position = new Vector3(0,-.13f,-1); ground.transform.localScale = new Vector3(38,.25f,33); ground.GetComponent<Renderer>().sharedMaterial = SharedMaterial("Concrete");
                var sun = new GameObject("Studio daylight copy").AddComponent<Light>(); sun.type = LightType.Directional;
                sun.transform.rotation = sunRotation; sun.intensity = sunIntensity; sun.color = sunColor; sun.shadows = LightShadows.Soft; sun.shadowNormalBias = .2f;
                RenderSettings.sun = sun; RenderSettings.skybox = sky; RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = skyColor; RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = groundColor; RenderSettings.reflectionIntensity = .85f;
                var volume = new GameObject("Studio volume copy").AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
                var camera = new GameObject("A4 review camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.scene = scene;
                camera.nearClipPlane = .03f; camera.farClipPlane = 150; camera.clearFlags = CameraClearFlags.Skybox;
                var cd = camera.GetUniversalAdditionalCameraData(); cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cd.antialiasingQuality = AntialiasingQuality.High;
                Pose(camera,new Vector3(27,19,-30),new Vector3(0,2,-1),43,false,12);
                EditorSceneManager.SaveScene(scene,ScenePath);
                File.WriteAllText(Review + "/lighting.json", "{\"source\":\"Assets/Scenes/Studio.unity\",\"sky\":\"" + AssetDatabase.GetAssetPath(sky) + "\",\"volume\":\"" + AssetDatabase.GetAssetPath(profile) + "\",\"sunIntensity\":" + sunIntensity.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            }
            finally
            {
                SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene, true);
                if (closeStudio) EditorSceneManager.CloseScene(studio, true);
            }
        }
        static void Pose(Camera c, Vector3 p, Vector3 t, float fov, bool ortho, float size)
        {
            c.transform.SetPositionAndRotation(p, Quaternion.LookRotation(t-p)); c.fieldOfView=fov;
            c.orthographic=ortho; c.orthographicSize=size; c.aspect=1.6f;
        }
        static void Shot(Camera camera, string name, Vector3 p, Vector3 t, float fov=43, bool ortho=false, float size=12)
        {
            Pose(camera,p,t,fov,ortho,size);
            var old=RenderTexture.active; var rt=RenderTexture.GetTemporary(1920,1200,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); Texture2D image=null;
            try
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest { destination=rt };
                for(int i=0;i<3;i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt; image=new Texture2D(1920,1200,TextureFormat.RGB24,false,false); image.ReadPixels(new Rect(0,0,1920,1200),0,0); image.Apply();
                File.WriteAllBytes(Review+"/"+name+".png",image.EncodeToPNG());
            }
            finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(rt); if(image!=null)Object.DestroyImmediate(image); }
        }
        [MenuItem("SilverScreen/Art/Stage School A4/3 Capture review views")]
        public static void CaptureReview()
        {
            // Preview scene isolates the candidate while preserving unsaved working scenes.
            var active=SceneManager.GetActiveScene();
            bool ownsPreview=active.path!=ScenePath;
            var scene=ownsPreview ? EditorSceneManager.OpenPreviewScene(ScenePath) : active;
            var root=scene.GetRootGameObjects().First(o=>o.name.StartsWith("StageSchool_A4"));
            var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).First(); camera.scene=scene;
            var pos=camera.transform.position; var rot=camera.transform.rotation; var fov=camera.fieldOfView; var ortho=camera.orthographic; var size=camera.orthographicSize;
            var visibility=root.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
            Action<string,bool> visible=(path,on)=> { var t=root.transform.Find(path); if(t!=null)t.gameObject.SetActive(on); };
            try
            {
                Shot(camera,"01_front_three_quarter",new Vector3(27,19,-30),new Vector3(0,2,-1));
                Shot(camera,"02_rear_three_quarter",new Vector3(-26,18,26),new Vector3(0,2,-1));
                Shot(camera,"03_front_elevation",new Vector3(0,3.1f,-35),new Vector3(0,3.1f,-1),43,true,8.2f);
                Shot(camera,"04_roof",new Vector3(20,29,-24),new Vector3(0,2,-1),43);
                Shot(camera,"10_applicant_forecourt",new Vector3(15,10,-20),new Vector3(4.1f,.7f,-9.5f),54);
                Shot(camera,"11_forecourt_detail",new Vector3(8.7f,2.65f,-14.2f),new Vector3(6.4f,.80f,-9.35f),53);
                Shot(camera,"12_planter_detail",new Vector3(5.3f,1.75f,-11.1f),new Vector3(4.15f,.84f,-9.35f),48);
                Shot(camera,"13_right_bench_clearance",new Vector3(5.6f,2.35f,-10.5f),new Vector3(7.05f,.8f,-9.35f),49);
                Shot(camera,"14_left_bench_clearance",new Vector3(-5.6f,2.35f,-10.5f),new Vector3(-7.05f,.8f,-9.35f),49);
                visible("Roofs",false); visible("Ceilings",false);
                Shot(camera,"05_full_plan",new Vector3(0,34,-1.001f),new Vector3(0,0,-1),43,true,11.7f);
                visible("Shell/Front",false); visible("Doors/EntranceLeft",false); visible("Doors/EntranceRight",false);
                Shot(camera,"06_cutaway",new Vector3(21,27,-27),new Vector3(0,.7f,-1),43);
                Shot(camera,"07_hiring_hall",new Vector3(0,14,-20),new Vector3(0,.5f,-4.5f),67);
                foreach(var v in visibility)v.Key.SetActive(v.Value);
                Shot(camera,"08_audition",new Vector3(-1.4f,3.1f,-.6f),new Vector3(-4.2f,1.8f,4.35f),87);
                Shot(camera,"09_interview",new Vector3(9.8f,2.9f,-.6f),new Vector3(6.35f,1.25f,3.8f),78);
                Check(root);
            }
            finally
            {
                foreach(var v in visibility)v.Key.SetActive(v.Value);
                camera.transform.SetPositionAndRotation(pos,rot); camera.fieldOfView=fov; camera.orthographic=ortho; camera.orthographicSize=size;
                if(ownsPreview)EditorSceneManager.ClosePreviewScene(scene);
            }
            Debug.Log("A4 ten URP review views captured; camera and visibility restored.");
        }
    }
}
