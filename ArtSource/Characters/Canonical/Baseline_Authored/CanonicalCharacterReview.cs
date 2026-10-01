using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SilverScreen.Editor.Characters
{
    /// <summary>One visual approval candidate. Works only in an isolated Studio preview.</summary>
    public static class CanonicalCharacterReview
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/Canonical";
        public const string Review = "ArtReview/Characters/Canonical";
        private static readonly Vector3 NeutralOrigin = new Vector3(1000, 0, 1000);
        private static Scene preview;
        private static Camera camera;
        private static GameObject candidate, neutral;
        private static readonly List<Light> studioLights = new List<Light>();
        private static readonly List<string> evidence = new List<string>();

        [MenuItem("SilverScreen/Characters/Canonical/1 Import single candidate")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Root + "/Materials");
            ConfigureTexture("dif", false, true);
            ConfigureTexture("norm", true, false);
            ConfigureTexture("gloss", false, false, true);
            // URP's metallic/smoothness map stores smoothness in alpha. Preserve
            // the source gloss as linear data, rather than treating it as colour.
            var gloss = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Canonical_gloss.jpg");
            var data = gloss.GetPixels32();
            for (int i = 0; i < data.Length; i++) data[i] = new Color32(0, 0, 0, data[i].r);
            var packed = new Texture2D(gloss.width, gloss.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(data); packed.Apply();
            string packedPath = Root + "/Textures/Canonical_MetallicSmoothness.png";
            File.WriteAllBytes(packedPath, packed.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(packedPath);
            var packedImporter = (TextureImporter)AssetImporter.GetAtPath(packedPath);
            packedImporter.sRGBTexture = false; packedImporter.maxTextureSize = 4096;
            packedImporter.textureCompression = TextureImporterCompression.CompressedHQ; packedImporter.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Canonical_Authored.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Root + "/Materials/Canonical_Authored.mat");
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Canonical_dif.jpg"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Canonical_norm.jpg"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            material.SetFloat("_BumpScale", .65f); material.SetFloat("_Smoothness", .65f);
            material.SetFloat("_Metallic", 0); material.SetFloat("_Cull", 2);
            BaseShaderGUI.SetMaterialKeywords(material, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(material);
            string eyePath=Root+"/Materials/Canonical_Eyes.mat";
            var eyes=AssetDatabase.LoadAssetAtPath<Material>(eyePath);
            if(eyes==null){eyes=new Material(material);AssetDatabase.CreateAsset(eyes,eyePath);}
            eyes.CopyPropertiesFromMaterial(material); eyes.SetTexture("_BumpMap",null); eyes.SetTexture("_MetallicGlossMap",null);
            eyes.SetFloat("_Smoothness",.90f);eyes.SetFloat("_Metallic",0);
            BaseShaderGUI.SetMaterialKeywords(eyes,UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);EditorUtility.SetDirty(eyes);
            var model = (ModelImporter)AssetImporter.GetAtPath(Root + "/Models/CanonicalAdult.fbx");
            model.globalScale = 1; model.useFileScale = true; model.bakeAxisConversion = true;
            model.isReadable = true; model.importAnimation = false; model.animationType = ModelImporterAnimationType.Generic;
            model.importNormals = ModelImporterNormals.Import; model.importTangents = ModelImporterTangents.CalculateMikk;
            model.importCameras = false; model.importLights = false; model.optimizeGameObjects = false;
            model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "SS_Canonical_Authored"), material);
            model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "SS_Canonical_Eyes"), eyes);
            model.SaveAndReimport();
            var staging = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/CanonicalAdult.fbx"), staging);
                instance.name = "CanonicalAdult_VisualReviewOnly";
                var animator = instance.GetComponent<Animator>(); if (animator != null) animator.enabled = false;
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen = true;
                PrefabUtility.SaveAsPrefabAsset(instance, Root + "/CanonicalAdult_VisualReview.prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(staging); }
            AssetDatabase.SaveAssets();
            Debug.Log("Canonical: imported one authored visual candidate. No appearance catalog or gameplay assignment changed.");
        }

        private static void ConfigureTexture(string suffix, bool normal, bool srgb, bool readable = false)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Textures/Canonical_" + suffix + ".jpg");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb; importer.isReadable = readable; importer.mipmapEnabled = true;
            importer.maxTextureSize = suffix == "dif" || normal ? 8192 : 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        [MenuItem("SilverScreen/Characters/Canonical/2 Open isolated review")]
        public static void Open()
        {
            Close(); evidence.Clear(); studioLights.Clear();
            preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Studio.unity");
            var all = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
            var sourceCamera = all.Select(o => o.GetComponent<Camera>()).FirstOrDefault(o => o != null);
            var cameraObject = NewObject("Canonical_ReviewCamera"); camera = cameraObject.AddComponent<Camera>();
            if (sourceCamera != null) camera.CopyFrom(sourceCamera);
            camera.enabled = false; camera.scene = preview; camera.nearClipPlane = .02f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.28f,.29f,.30f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            candidate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/CanonicalAdult_VisualReview.prefab"), preview);
            foreach (var light in all.Select(o => o.GetComponent<Light>()).Where(l => l != null && l.enabled && l.gameObject.activeInHierarchy)) studioLights.Add(light);
            neutral = NewObject("Canonical_NeutralStudio");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, preview); floor.transform.SetParent(neutral.transform);
            floor.transform.position = NeutralOrigin + Vector3.down * .05f; floor.transform.localScale = new Vector3(200,.1f,200);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
            mat.color = new Color(.32f,.33f,.34f); mat.SetFloat("_Smoothness", .05f); floor.GetComponent<Renderer>().sharedMaterial = mat;
            AddLight("Neutral_Key", new Vector3(-2,3.5f,3), .95f, true);
            AddLight("Neutral_Fill", new Vector3(2,2.3f,2), .4f, false);
            AddLight("Neutral_Back", new Vector3(0,3,-3), .3f, false);
            Neutral();
        }

        private static GameObject NewObject(string name)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, preview); return go;
        }

        private static void AddLight(string name, Vector3 offset, float intensity, bool shadows)
        {
            var go = NewObject(name); go.transform.SetParent(neutral.transform);
            go.transform.position = NeutralOrigin + offset; go.transform.LookAt(NeutralOrigin + Vector3.up * 1.15f);
            var light = go.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = Color.white; light.intensity = intensity; light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void Neutral()
        {
            neutral.SetActive(true); foreach (var light in studioLights) light.enabled = false;
            candidate.transform.SetPositionAndRotation(NeutralOrigin, Quaternion.identity);
        }

        [MenuItem("SilverScreen/Characters/Canonical/3 Capture neutral review")]
        public static void CaptureNeutral()
        {
            if (!preview.IsValid()) Open(); Neutral();
            // Newly opened preview scenes need an initial culling/material warm-up.
            Capture("00_warmup", NeutralOrigin + new Vector3(0,1.04f,3.65f), NeutralOrigin + Vector3.up*.88f,31,320,320);
            Capture("01_full_body_front", NeutralOrigin + new Vector3(0,1.04f,3.65f), NeutralOrigin + Vector3.up*.88f, 31, 1200,1600);
            Capture("02_full_body_side", NeutralOrigin + new Vector3(3.65f,1.04f,0), NeutralOrigin + Vector3.up*.88f, 31,1200,1600);
            Capture("03_full_body_three_quarter", NeutralOrigin + new Vector3(2.25f,1.04f,2.88f), NeutralOrigin + Vector3.up*.88f,31,1200,1600);
            Capture("04_head_shoulders_front", NeutralOrigin + new Vector3(0,1.62f,1.22f), NeutralOrigin + Vector3.up*1.49f,31,1200,1400);
            Capture("05_face_three_quarter", NeutralOrigin + new Vector3(.58f,1.63f,.79f), NeutralOrigin + new Vector3(0,1.60f,.03f),28,1200,1400);
            Capture("06_face_profile", NeutralOrigin + new Vector3(.98f,1.63f,0), NeutralOrigin + new Vector3(0,1.60f,.015f),28,1200,1400);
            Capture("07_face_closeup", NeutralOrigin + new Vector3(0,1.63f,.86f), NeutralOrigin + new Vector3(0,1.60f,.045f),27,1400,1400);
            Capture("08_hands", NeutralOrigin + new Vector3(.66f,.8f,.6f), NeutralOrigin + new Vector3(.265f,.775f,0),27,1200,1400);
            WriteEvidence();
        }

        [MenuItem("SilverScreen/Characters/Canonical/4 Capture Stage 1 and Live Camera")]
        public static void CaptureStage()
        {
            if (!preview.IsValid()) Open();
            neutral.SetActive(false); foreach (var light in studioLights) light.enabled = true;
            candidate.transform.SetPositionAndRotation(new Vector3(24.1f,0,-31.3f),Quaternion.Euler(0,180,0));
            Capture("09_stage1_exterior",new Vector3(26.6f,1.6f,-35.4f),new Vector3(23.8f,1.04f,-30.9f),36,1800,1200);
            Capture("10_stage1_exterior_face",candidate.transform.position+new Vector3(-.58f,1.65f,-.85f),candidate.transform.position+Vector3.up*1.59f,30,1200,1400);
            Capture("11_management_distance",new Vector3(31.5f,12.5f,-42.5f),new Vector3(24,0,-28.5f),40,1800,1200);
            var all = preview.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();
            var mark = all.First(t=>t.name=="ActorMarkB");
            candidate.transform.SetPositionAndRotation(mark.position,Quaternion.Euler(0,180,0));
            Capture("12_stage1_interior",candidate.transform.position+new Vector3(2.8f,1.8f,-4.6f),candidate.transform.position+Vector3.up*.94f,37,1800,1200);
            var layout = all.Select(o=>o.GetComponent<ProductionCameraLayout>()).First(o=>o!=null);
            foreach (var shot in new[]{ShotType.CloseUp,ShotType.Medium,ShotType.Wide})
            {
                if (!layout.TryFrame(shot,candidate.transform,out var position,out var focus,out float fov)) throw new InvalidOperationException("Stage1 camera layout is not configured.");
                Capture("13_live_camera_"+shot,position,focus,fov,1920,1080);
                evidence.Add("Live framing: "+shot+"; position="+position.ToString("F3")+"; focus="+focus.ToString("F3")+"; fov="+fov);
            }
            WriteEvidence();
        }

        public static void Capture(string name, Vector3 position, Vector3 target, float fov, int width, int height)
        {
            Directory.CreateDirectory(Review+"/Captures");
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            camera.fieldOfView=fov;camera.aspect=(float)width/height;
            var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var prior=RenderTexture.active;Texture2D image=null;
            try
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;image=new Texture2D(width,height,TextureFormat.RGB24,false,false);
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Review+"/Captures/"+name+".png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
            evidence.Add(name+"; "+width+"x"+height+"; fov="+fov+"; position="+position.ToString("F3")+"; target="+target.ToString("F3"));
        }

        private static void WriteEvidence()
        {
            var bounds=new Bounds(candidate.transform.position,Vector3.zero);
            foreach(var renderer in candidate.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            File.WriteAllLines(Review+"/unity-capture-evidence.txt",new[]{
                "Actual Unity URP captures; one candidate; "+DateTime.UtcNow.ToString("O"),
                "Saved Studio unchanged by review. Isolated preview only. No depth-of-field added.",
                "Live shots use ProductionCameraLayout.TryFrame; runtime playback integration is not claimed.",
                "Renderer bounds size="+bounds.size.ToString("F4"),
                "Active scene dirty="+SceneManager.GetActiveScene().isDirty,
                "Render pipeline="+GraphicsSettings.currentRenderPipeline.name,
                "Candidate count="+preview.GetRootGameObjects().Count(o=>o.name==candidate.name)
            }.Concat(evidence));
        }

        [MenuItem("SilverScreen/Characters/Canonical/Close isolated review")]
        public static void Close()
        {
            foreach(var c in Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.name=="Canonical_ReviewCamera"&&EditorSceneManager.IsPreviewSceneObject(c)).ToArray())
                if(c!=null&&c.gameObject.scene.IsValid())EditorSceneManager.ClosePreviewScene(c.gameObject.scene);
            preview=default;camera=null;candidate=null;neutral=null;studioLights.Clear();
        }
    }
}
