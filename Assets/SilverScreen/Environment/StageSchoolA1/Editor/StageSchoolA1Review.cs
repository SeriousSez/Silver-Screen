using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>A1 art only. Owns no runtime building, recruitment or reveal components.</summary>
    public static class StageSchoolA1Review
    {
        public const string Root = "Assets/SilverScreen/Environment/StageSchoolA3";
        public const string Prefab = Root + "/StageSchool_A3.prefab";
        public const string ScenePath = Root + "/StageSchool_A3_Review.unity";
        public const string Review = "ArtReview/StageSchoolA1";
        const string ApprovedA1 = "Assets/SilverScreen/Environment/StageSchoolA1";
        const string Kit = "Assets/SilverScreen/Environment/PeriodEnvironment1930";
        [Serializable] public class Submesh { public int[] indices; }
        [Serializable] public class Part { public string name; public float[] positions, normals, uv, pivot; public string[] materials; public Submesh[] submeshes; }
        [Serializable] public class Surface { public string name,texture; public float[] linearRGB; public float metallic,roughness; public bool shared; }
        [Serializable] public class Fixture { public string asset,group; public float[] position; public float yaw; }
        [Serializable] public class Packet { public Part[] parts; public Surface[] materials; public Fixture[] fixtures,equipment; }
        [Serializable] public class Sanity { public string unityVersion,renderPipeline; public int renderers,vertices,triangles,materials,missingMaterials,missingMeshes; public Vector3 boundsMin,boundsMax; public bool scaleValid,noRuntimeBuildingComponents; }
        static Vector3 V(float[] a) => new Vector3(a[0],a[1],a[2]);
        static void Folder(string path) { string p="Assets";foreach(var s in path.Split('/').Skip(1)){if(!AssetDatabase.IsValidFolder(p+"/"+s))AssetDatabase.CreateFolder(p,s);p+="/"+s;} }
        static GameObject Node(Transform root,string path)
        {
            var at=root;
            foreach(var s in path.Split('/')) { var next=at.Find(s);if(next==null){var go=new GameObject(s);SceneManager.MoveGameObjectToScene(go,root.gameObject.scene);go.transform.SetParent(at,false);next=go.transform;}at=next; }
            return at.gameObject;
        }
        static T Asset<T>(string path,Func<T> factory) where T:Object { var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=factory();AssetDatabase.CreateAsset(a,path);}return a; }

        [MenuItem("SilverScreen/Art/Stage School A1/1 Import isolated candidate")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            // A2 only instances the explicitly approved equipment; never rebuild it here.
            Folder(Root+"/Materials");Folder(Root+"/Meshes");Directory.CreateDirectory(Review);
            Packet data;
            using(var file=File.OpenRead("ArtExports/StageSchoolA3/stage_school_meshes.json.gz"))
            using(var zip=new GZipStream(file,CompressionMode.Decompress))
            using(var reader=new StreamReader(zip))data=JsonUtility.FromJson<Packet>(reader.ReadToEnd());
            var mats=new Dictionary<string,Material>();
            foreach(var s in data.materials)
            {
                var approved=AssetDatabase.LoadAssetAtPath<Material>(ApprovedA1+"/Materials/"+s.name+".mat");
                if(approved!=null){mats.Add(s.name,approved);continue;}
                if(s.shared)
                {
                    var shared=AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/"+s.name+".mat");
                    if(shared==null)throw new FileNotFoundException("Shared production material "+s.name);
                    mats.Add(s.name,shared);continue;
                }
                var m=Asset(Root+"/Materials/"+s.name+".mat",()=>new Material(Shader.Find("Universal Render Pipeline/Lit")));
                var c=V(s.linearRGB);m.SetColor("_BaseColor",new Color(c.x,c.y,c.z).gamma);m.SetFloat("_Metallic",s.metallic);m.SetFloat("_Smoothness",1-s.roughness);
                string family=s.texture=="canvas"?"cloth":s.texture;
                if(family=="headshots")
                {
                    m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PerformerHeadshots.png"));
                    m.SetFloat("_Cull",0);
                }
                else if(!string.IsNullOrEmpty(family))
                {
                    string p="Assets/SilverScreen/Environment/Stage1CleanCandidate/Textures/"+family;
                    var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(p+"_albedo.png");var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(p+"_normal.png");
                    if(albedo==null||normal==null)throw new FileNotFoundException("Shared texture set "+family);
                    m.SetTexture("_BaseMap",albedo);m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.35f);m.EnableKeyword("_NORMALMAP");
                }
                if(s.name.StartsWith("SS_Tile"))
                {
                    string albedo=Root+"/Textures/RoofClay_albedo.png",packed=Root+"/Textures/RoofClay_metallicSmoothness.png";
                    var importer=(TextureImporter)AssetImporter.GetAtPath(packed);
                    if(importer.sRGBTexture){importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.SaveAndReimport();}
                    m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
                    m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
                    m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");
                }
                if(s.name=="SS_Lamp"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.72f,.38f)*1.5f);}
                if(s.name=="SS_ClearGlass")
                {
                    var glass=m.GetColor("_BaseColor");glass.a=.12f;m.SetColor("_BaseColor",glass);
                    m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");m.SetShaderPassEnabled("ShadowCaster",false);
                }
                EditorUtility.SetDirty(m);mats.Add(s.name,m);
            }
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("StageSchool_A3_REVIEW_ONLY");SceneManager.MoveGameObjectToScene(root,preview);
                foreach(var p in data.parts)
                {
                    var go=Node(root.transform,p.name);var pivot=V(p.pivot);go.transform.localPosition=pivot;
                    string name=p.name.Replace('/','_');var mesh=Asset(Root+"/Meshes/"+name+".asset",()=>new Mesh());mesh.Clear();mesh.name=name;
                    mesh.indexFormat=p.positions.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16;
                    mesh.vertices=Enumerable.Range(0,p.positions.Length/3).Select(i=>new Vector3(p.positions[i*3],p.positions[i*3+1],p.positions[i*3+2])-pivot).ToArray();
                    mesh.normals=Enumerable.Range(0,p.normals.Length/3).Select(i=>new Vector3(p.normals[i*3],p.normals[i*3+1],p.normals[i*3+2])).ToArray();
                    mesh.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[i*2],p.uv[i*2+1])).ToArray();
                    mesh.subMeshCount=p.submeshes.Length;for(int i=0;i<p.submeshes.Length;i++)mesh.SetTriangles(p.submeshes[i].indices,i,false);
                    mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=p.materials.Select(n=>mats[n]).ToArray();
                }
                foreach(var f in data.fixtures)
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/Prefabs/"+f.asset+".prefab");if(source==null)throw new FileNotFoundException(f.asset);
                    var parent=Node(root.transform,f.group);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent.transform);go.transform.localPosition=V(f.position);go.transform.localRotation=Quaternion.Euler(0,f.yaw,0);
                    // Review-only copies retain mesh/material references but no gameplay behaviour.
                    foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
                    foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                }
                foreach(var f in data.equipment)
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionEquipmentCandidateBuilder.Root+"/Prefabs/"+f.asset+".prefab");
                    if(source==null)throw new FileNotFoundException(f.asset);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(source,Node(root.transform,f.group).transform);
                    go.transform.localPosition=V(f.position);go.transform.localRotation=Quaternion.Euler(0,f.yaw,0);
                }
                // Four restrained shadowless room lights; emissive fixture surfaces supply visible glow.
                foreach(var p in new[]{new Vector3(0,3.05f,-5),new Vector3(-6.7f,3.1f,-4.8f),new Vector3(0,3.4f,8.5f),new Vector3(8.5f,4.2f,10),new Vector3(8.5f,3.8f,4),new Vector3(-10.5f,3.3f,5.6f),new Vector3(-5,3.3f,5.6f),new Vector3(-11,3.1f,12)})
                {
                    var go=Node(root.transform,"ReviewLighting/Room_"+p.x+"_"+p.z);go.transform.localPosition=p;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.79f,.56f);l.intensity=1.7f;l.range=6;l.shadows=LightShadows.None;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
                Check(root);
            }
            finally {EditorSceneManager.ClosePreviewScene(preview);}
            Debug.Log("Stage School A3 imported. Approved equipment and Studio scene untouched.");
        }
        static void Check(GameObject root)
        {
            var rs=root.GetComponentsInChildren<MeshRenderer>(true);var ms=root.GetComponentsInChildren<MeshFilter>(true);
            var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var s=new Sanity{unityVersion=Application.unityVersion,renderPipeline=GraphicsSettings.currentRenderPipeline.name,renderers=rs.Length,
                vertices=ms.Where(m=>m.sharedMesh!=null).Sum(m=>m.sharedMesh.vertexCount),triangles=ms.Where(m=>m.sharedMesh!=null).Sum(m=>m.sharedMesh.triangles.Length/3),
                materials=rs.SelectMany(r=>r.sharedMaterials).Distinct().Count(),missingMaterials=rs.Sum(r=>r.sharedMaterials.Count(m=>m==null||m.shader==null)),missingMeshes=ms.Count(m=>m.sharedMesh==null),
                boundsMin=bounds.min,boundsMax=bounds.max,scaleValid=root.GetComponentsInChildren<Transform>(true).All(t=>(t.localScale-Vector3.one).sqrMagnitude<.0001f),
                noRuntimeBuildingComponents=root.GetComponentsInChildren<MonoBehaviour>(true).Length==0};
            File.WriteAllText(Review+"/a3_unity_sanity.json",JsonUtility.ToJson(s,true));
            if(s.missingMaterials+s.missingMeshes!=0||!s.scaleValid||bounds.size.x<27||bounds.size.x>32||bounds.max.y>9||bounds.min.y<-.03f)throw new InvalidOperationException("Candidate sanity failed; inspect report.");
        }
        [MenuItem("SilverScreen/Art/Stage School A1/2 Create review scene")]
        public static void CreateReview()
        {
            var original=SceneManager.GetActiveScene();
            // Read the real Studio environment before switching active scenes.
            var sky=RenderSettings.skybox;var ambientSky=RenderSettings.ambientSkyColor;var ambientEquator=RenderSettings.ambientEquatorColor;var ambientGround=RenderSettings.ambientGroundColor;
            var originalSun=RenderSettings.sun;var sunRotation=originalSun!=null?originalSun.transform.rotation:Quaternion.Euler(50,330,0);float intensity=originalSun!=null?originalSun.intensity:1.5f;
            var profile=original.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Volume>(true)).FirstOrDefault(v=>v.isGlobal)?.sharedProfile;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Review ground only";ground.transform.position=new Vector3(0,-.13f,0);ground.transform.localScale=new Vector3(50,.25f,48);ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/Concrete.mat");
                var sun=new GameObject("Studio daylight copy").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=sunRotation;sun.intensity=intensity;sun.shadows=LightShadows.Soft;sun.shadowNormalBias=.2f;sun.color=originalSun!=null?originalSun.color:Color.white;
                RenderSettings.sun=sun;RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=ambientSky;RenderSettings.ambientEquatorColor=ambientEquator;RenderSettings.ambientGroundColor=ambientGround;RenderSettings.reflectionIntensity=.85f;
                var volume=new GameObject("Studio volume copy").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
                var camera=new GameObject("Stage School review camera").AddComponent<Camera>();camera.tag="MainCamera";camera.scene=scene;camera.nearClipPlane=.03f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.Skybox;
                var cd=camera.GetUniversalAdditionalCameraData();cd.renderPostProcessing=true;cd.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;cd.antialiasingQuality=AntialiasingQuality.High;
                camera.transform.SetPositionAndRotation(new Vector3(35,24,-39),Quaternion.LookRotation(new Vector3(0,2.3f,2)-new Vector3(35,24,-39)));camera.fieldOfView=43;
                EditorSceneManager.SaveScene(scene,ScenePath);
                BakeWashroomReflection(root);
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally {SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Capture(Camera camera,string name,Vector3 pos,Vector3 target,float fov=43,bool ortho=false,float size=12)
        {
            camera.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(target-pos));camera.fieldOfView=fov;camera.orthographic=ortho;camera.orthographicSize=size;camera.aspect=1.6f;
            var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(1920,1200,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D image=null;
            try
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;image=new Texture2D(1920,1200,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,1920,1200),0,0);image.Apply();File.WriteAllBytes(Review+"/"+name+".png",image.EncodeToPNG());
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.DestroyImmediate(image);}
        }
        // One static 128px cubemap, shared by both mirrors; no realtime reflection camera.
        static void BakeWashroomReflection(GameObject root)
        {
            string path=Root+"/Textures/WashroomReflection.exr";Folder(Root+"/Textures");
            var bakeScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try {
                var copy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),bakeScene);
                int layer=Enumerable.Range(8,24).Reverse().First(i=>string.IsNullOrEmpty(LayerMask.LayerToName(i))&&!Resources.FindObjectsOfTypeAll<GameObject>().Any(o=>o.scene.IsValid()&&o.scene!=bakeScene&&o.layer==i));
                foreach(var t in copy.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=layer;GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ReflectionProbeStatic);}
                foreach(var light in copy.GetComponentsInChildren<Light>())light.cullingMask=1<<layer;
                var probe=Node(copy.transform,"WashroomReflection").AddComponent<ReflectionProbe>();
                probe.transform.localPosition=new Vector3(9.55f,1.94f,-4.62f);probe.mode=ReflectionProbeMode.Baked;probe.resolution=128;probe.hdr=true;probe.nearClipPlane=.08f;probe.farClipPlane=25;probe.cullingMask=1<<layer;
                if(!Lightmapping.BakeReflectionProbe(probe,path))throw new IOException("Washroom reflection bake failed");
            } finally {EditorSceneManager.CloseScene(bakeScene,true);}
            AssetDatabase.ImportAsset(path);var cubemap=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(cubemap==null)throw new IOException("Washroom cubemap missing");
            foreach(var target in new[]{root})SetWashroomProbe(target,cubemap);
            var contents=PrefabUtility.LoadPrefabContents(Prefab);
            try {SetWashroomProbe(contents,cubemap);PrefabUtility.SaveAsPrefabAsset(contents,Prefab);}
            finally {PrefabUtility.UnloadPrefabContents(contents);}
        }
        static void SetWashroomProbe(GameObject root,Cubemap cubemap)
        {
            var node=Node(root.transform,"WashroomReflection");node.transform.localPosition=new Vector3(9.55f,1.94f,-4.62f);
            var probe=node.GetComponent<ReflectionProbe>();if(probe==null)probe=node.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cubemap;probe.boxProjection=true;
            probe.center=new Vector3(-.9f,-.1f,-1.75f);probe.size=new Vector3(4.3f,3.5f,4.8f);probe.blendDistance=.05f;probe.importance=10;probe.intensity=1;
        }
        public static void KnownDefectViews()
        {
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try {
                var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).First();camera.scene=scene;
                Capture(camera,"a3_07_roof",new Vector3(25,30,-29),new Vector3(0,2,2),48);
                Capture(camera,"a3_15_entrance_close",new Vector3(4,3.2f,-15.5f),new Vector3(0,2.85f,-9),60);
                Capture(camera,"a3_33_hall_ceiling",new Vector3(0,2,3.8f),new Vector3(0,4.7f,-2.8f),90);
                Capture(camera,"a3_34_rear_drainage",new Vector3(-8,5.8f,18),new Vector3(-6,3.7f,13.8f),80);
                Capture(camera,"a3_35_roof_junction",new Vector3(4,9,5),new Vector3(1,5.3f,1),80);
                Capture(camera,"a3_23_restrooms",new Vector3(8.5f,2.65f,-5.40f),new Vector3(8.6f,1.1f,-7.6f),90);
                Capture(camera,"a3_24_toilet",new Vector3(7.8f,2.2f,-6.45f),new Vector3(7.75f,.9f,-8.1f),85);
                Capture(camera,"a3_25_basins",new Vector3(8.9f,1.9f,-5.9f),new Vector3(9.5f,1.25f,-4.5f),70);
                Capture(camera,"a31_toilet_second",new Vector3(9.65f,2.2f,-6.45f),new Vector3(9.53f,.9f,-8.1f),85);
                Capture(camera,"a31_privacy_hardware",new Vector3(7.60f,1.80f,-7.10f),new Vector3(8.12f,1.60f,-6.20f),65);
                Capture(camera,"a31_pendant_audition",new Vector3(8.5f,3.3f,6.5f),new Vector3(8.5f,5,8.3f),75);
                Capture(camera,"a31_pendant_support",new Vector3(-4.5f,2.5f,10.8f),new Vector3(-4.9f,3.9f,11.8f),85);
                Capture(camera,"a31_hall_collector",new Vector3(-6,8,-1),new Vector3(-3.4f,5.4f,1.1f),70);
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
            Debug.Log("A3.1 thirteen targeted known-defect captures complete");
        }
        [MenuItem("SilverScreen/Art/Stage School A1/3 Capture exteriors")]
        public static void Exteriors()=>CaptureSet(false);
        [MenuItem("SilverScreen/Art/Stage School A1/4 Capture interiors")]
        public static void Interiors()=>CaptureSet(true);
        static void CaptureSet(bool interior)
        {
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var roots=scene.GetRootGameObjects();var root=roots.First(o=>o.name.StartsWith("StageSchool_A3"));var camera=roots.SelectMany(o=>o.GetComponentsInChildren<Camera>()).First();camera.scene=scene;
                // Review scene visibility only. No generic reveal system is added or called.
                if(interior)
                {
                    foreach(string path in new[]{"Roofs","Ceilings","Shell/Raised","Shell/Front","Doors/EntranceLeft","Doors/EntranceRight"})root.transform.Find(path)?.gameObject.SetActive(false);
                    Capture(camera,"a3_08_floor_plan",new Vector3(0,42,2.999f),new Vector3(0,0,3),43,true,14.7f);
                    Capture(camera,"a3_09_reception",new Vector3(4.8f,5.0f,-11.5f),new Vector3(0,1.1f,-4.2f),47);
                    Capture(camera,"a3_10_waiting_common",new Vector3(-2.8f,9.0f,-12.8f),new Vector3(-7.4f,1,-1.8f),50);
                    Capture(camera,"a3_14_staff_support",new Vector3(-8.5f,16,6.3f),new Vector3(-8.5f,0,8.8f),50);
                    // Actual ceiling and pendant fixtures retained in within-room close views.
                    root.transform.Find("Ceilings").gameObject.SetActive(true);
                    root.transform.Find("Shell/Raised").gameObject.SetActive(true);
                    root.transform.Find("Roofs").gameObject.SetActive(true);
                    Capture(camera,"a3_11_audition",new Vector3(4.55f,3.5f,1.7f),new Vector3(8.6f,1.6f,10.2f),75);
                    Capture(camera,"a3_12_director",new Vector3(-1.9f,2.7f,6.2f),new Vector3(.2f,1.4f,9),82);
                    Capture(camera,"a3_13_flexible",new Vector3(-12.9f,2.7f,3.85f),new Vector3(-9.5f,1.2f,6.5f),78);
                }
                else
                {
                    Capture(camera,"a3_01_front",new Vector3(0,5,-39),new Vector3(0,3,0),43,true,10.4f);
                    Capture(camera,"a3_02_left",new Vector3(-40,6,3),new Vector3(0,3,3),43,true,10.2f);
                    Capture(camera,"a3_03_right",new Vector3(40,6,3),new Vector3(0,3,3),43,true,10.2f);
                    Capture(camera,"a3_04_rear",new Vector3(0,6,44),new Vector3(0,3,3),43,true,10.4f);
                    Capture(camera,"a3_05_front_three_quarter",new Vector3(35,24,-39),new Vector3(0,2.3f,2),43);
                    Capture(camera,"a3_06_rear_three_quarter",new Vector3(-34,24,42),new Vector3(0,2.3f,3),44);
                    Capture(camera,"a3_07_roof",new Vector3(24,40,-28),new Vector3(0,2,3),43);
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            Debug.Log("Stage School A3 "+(interior?"interior":"exterior")+" captures saved to "+Review);
        }

        [MenuItem("SilverScreen/Art/Stage School A1/5 A3 production audit views")]
        public static void AuditViews()
        {
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try {
                var roots=scene.GetRootGameObjects();
                var root=roots.First(o=>o.name.StartsWith("StageSchool_A3"));
                var camera=roots.SelectMany(o=>o.GetComponentsInChildren<Camera>()).First();camera.scene=scene;
                Capture(camera,"a3_15_entrance_close",new Vector3(4.0f,3.2f,-15.5f),new Vector3(0.0f,2.85f,-9.0f),60);
                Capture(camera,"a3_16_entrance_inside",new Vector3(2.8f,2.1f,-5.8f),new Vector3(0.0f,2.3f,-9.0f),70);
                Capture(camera,"a3_17_reception_rear",new Vector3(2.0f,2.2f,-1.95f),new Vector3(0.0f,0.9f,-4.2f),70);
                Capture(camera,"a3_18_performance_end",new Vector3(8.5f,2.2f,8.1f),new Vector3(8.5f,2.3f,13.9f),70);
                Capture(camera,"a3_19_staff_office",new Vector3(-3.45f,2.45f,3.75f),new Vector3(-5.5f,1.3f,6.4f),85);
                Capture(camera,"a3_20_archive",new Vector3(-9.3f,2.3f,11.4f),new Vector3(-11.8f,1.3f,12.3f),85);
                Capture(camera,"a3_21_equipment_store",new Vector3(-5.0f,2.1f,10.6f),new Vector3(-4.8f,1.35f,12.4f),90);
                Capture(camera,"a3_22_service_passage",new Vector3(-13.1f,2.0f,9.0f),new Vector3(-4.0f,1.4f,9.1f),76);
                Capture(camera,"a3_23_restrooms",new Vector3(8.5f,2.65f,-5.40f),new Vector3(8.6f,1.1f,-7.6f),90);
                Capture(camera,"a3_24_toilet",new Vector3(7.8f,2.2f,-6.45f),new Vector3(7.75f,0.9f,-8.1f),85);
                Capture(camera,"a3_25_basins",new Vector3(8.9f,1.9f,-5.9f),new Vector3(9.5f,1.25f,-4.5f),70);
                Capture(camera,"a3_26_door_public",new Vector3(4.2f,1.9f,-1.5f),new Vector3(4.8f,1.5f,1.0f),65);
                Capture(camera,"a3_27_door_room",new Vector3(6.5f,1.9f,3.4f),new Vector3(4.4f,1.5f,1.1f),75);
                Capture(camera,"a3_28_window_inside",new Vector3(-8.7f,2.0f,-6.0f),new Vector3(-10.8f,2.4f,-6.0f),75);
                Capture(camera,"a3_29_window_outside",new Vector3(-13.8f,2.0f,-6.0f),new Vector3(-10.8f,2.4f,-6.0f),65);
                Capture(camera,"a3_30_armchair_front",new Vector3(-7.4f,1.35f,-5.2f),new Vector3(-9.1f,0.95f,-5.5f),70);
                Capture(camera,"a3_31_armchair_rear",new Vector3(-10.1f,1.25f,-5.8f),new Vector3(-9.1f,0.9f,-5.5f),70);
                Capture(camera,"a3_32_talent_lounge",new Vector3(3.5f,2.2f,-1.4f),new Vector3(4.8f,1.1f,-5.0f),75);
                Capture(camera,"a3_33_hall_ceiling",new Vector3(0.0f,2.0f,3.8f),new Vector3(0.0f,4.7f,-2.8f),90);
                Capture(camera,"a3_34_rear_drainage",new Vector3(-8.0f,5.8f,18.0f),new Vector3(-6.0f,3.7f,13.8f),80);
                Capture(camera,"a3_35_roof_junction",new Vector3(4.0f,9.0f,5.0f),new Vector3(1.0f,5.3f,1.0f),80);
                foreach(string path in new[]{"Roofs","Ceilings","Shell/Raised","Shell/Front","Doors/EntranceLeft","Doors/EntranceRight"})root.transform.Find(path)?.gameObject.SetActive(false);
                Capture(camera,"a3_36_interior_overview",new Vector3(20,27,-24),new Vector3(0,0,3),45);
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log("A3 full production audit captures saved.");
        }
    }
}
