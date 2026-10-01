using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SilverScreen.Domain.Characters;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Owns only Gate1 assets and isolated preview objects. Never saves Studio.</summary>
    public static class CharacterGateOne
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/Gate1";
        public const string ReviewRoot = "ArtReview/Characters/Gate1";
        public const string CatalogPath = Root + "/AdultGate1Catalog.asset";
        [Serializable] private class MaterialRecord { public string name, kind, albedo, normal, bump; public float[] color; public float roughness; public bool alpha; }
        [Serializable] private class CandidateRecord { public string id, foundation, hair, outfit, model; public float height, soleLift; }
        [Serializable] private class Report { public CandidateRecord[] candidates; public MaterialRecord[] materials; }
        private static Scene _preview;
        private static Camera _camera;
        private static readonly List<CharacterView> Views = new List<CharacterView>();
        private static readonly Vector3 PortraitOrigin = new Vector3(1000, 0, 1000);
        public static IReadOnlyList<CharacterView> Candidates => Views;

        public static void Rebuild(int index, CharacterAppearance profile)
        {
            var old = Views[index]; var catalog=AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            var replacement=CharacterBuilder.Build(catalog,profile);
            SceneManager.MoveGameObjectToScene(replacement.gameObject,_preview);
            replacement.transform.SetPositionAndRotation(old.transform.position,old.transform.rotation);
            Views[index]=replacement;UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        [MenuItem("SilverScreen/Characters/Gate 1/1 Import source builds")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var report = JsonUtility.FromJson<Report>(File.ReadAllText("ArtSource/Characters/Gate1/generation_report.json"));
            Directory.CreateDirectory(Root + "/Materials"); AssetDatabase.Refresh();
            var materials = new Dictionary<string, Material>();
            foreach (var record in report.materials)
            {
                string path = Root + "/Materials/" + record.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
                material.SetColor("_BaseColor", new Color(record.color[0], record.color[1], record.color[2], record.color[3]).gamma);
                material.SetFloat("_Smoothness", 1 - record.roughness); material.SetFloat("_Metallic", 0);
                material.SetFloat("_Cull", record.alpha || record.kind == "outfit" ? 0 : 2);
                material.SetFloat("_AlphaClip", record.alpha ? 1 : 0); material.SetFloat("_Cutoff", record.kind == "hair" ? .32f : .22f);
                if (!string.IsNullOrEmpty(record.albedo))
                {
                    ConfigureTexture(record.albedo, false); material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(record.albedo));
                }
                string normal = !string.IsNullOrEmpty(record.normal) ? record.normal : record.bump;
                if (!string.IsNullOrEmpty(normal))
                {
                    ConfigureTexture(normal, true, string.IsNullOrEmpty(record.normal));
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal)); material.SetFloat("_BumpScale", .45f);
                }
                BaseShaderGUI.SetMaterialKeywords(material, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
                EditorUtility.SetDirty(material); materials.Add(record.name, material);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<CharacterCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.calibrations = report.candidates.Select(record =>
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(record.model);
                importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
                importer.isReadable = true; importer.importAnimation = false; importer.importBlendShapes = true;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importNormals = ModelImporterNormals.Import; importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.optimizeGameObjects = false; importer.importCameras = false; importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (var pair in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
                importer.SaveAndReimport();
                return new CharacterCatalog.Calibration { id = record.id, foundationId = record.foundation,
                    outfitId = record.outfit, hairId = record.hair, referenceHeight = record.height + record.soleLift,
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(record.model) };
            }).ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Directory.CreateDirectory(ReviewRoot + "/Profiles");
            foreach (var c in catalog.calibrations) File.WriteAllText(ReviewRoot + "/Profiles/" + c.id + ".json", JsonUtility.ToJson(catalog.CreateReference(c.id), true));
            Debug.Log("Gate1 import complete: three calibrations; two anatomical foundations; adult-v1.0.0.");
        }

        private static void ConfigureTexture(string path, bool normal, bool fromHeight = false)
        {
            var t = (TextureImporter)AssetImporter.GetAtPath(path);
            if (t == null) throw new FileNotFoundException(path);
            t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.convertToNormalmap = fromHeight; t.heightmapScale = .025f;
            t.sRGBTexture = !normal; t.mipmapEnabled = true; t.maxTextureSize = 2048;
            t.alphaIsTransparency = !normal; t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.SaveAndReimport();
        }

        [MenuItem("SilverScreen/Characters/Gate 1/2 Open isolated character preview")]
        public static void Open()
        {
            Close();
            _preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Studio.unity");
            var all = _preview.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var originalCamera = all.Select(t => t.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            var cameraObject = new GameObject("Gate1_ReviewCamera"); SceneManager.MoveGameObjectToScene(cameraObject, _preview);
            _camera = cameraObject.AddComponent<Camera>();
            if (originalCamera != null) _camera.CopyFrom(originalCamera);
            _camera.enabled = false; _camera.scene = _preview; _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(.12f,.16f,.21f); _camera.nearClipPlane = .03f;
            _camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            for (int i = 0; i < 3; i++)
            {
                var view = CharacterBuilder.Build(catalog, catalog.CreateReference(catalog.calibrations[i].id));
                SceneManager.MoveGameObjectToScene(view.gameObject, _preview);
                view.transform.position = PortraitOrigin + Vector3.right * (i - 1) * 1.05f; Views.Add(view);
            }
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Gate1_PreviewFloor";
            SceneManager.MoveGameObjectToScene(ground, _preview); ground.transform.position = PortraitOrigin + Vector3.down * .06f;
            ground.transform.localScale = new Vector3(20, .1f, 20);
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
            floorMat.color = new Color(.2f,.23f,.27f); ground.GetComponent<Renderer>().sharedMaterial = floorMat;
            AddPortraitLight("Gate1_Key", new Vector3(-2,3.8f,3.5f), new Vector3(0,1.1f,0), 5, new Color(1,.88f,.77f));
            AddPortraitLight("Gate1_Fill", new Vector3(2,2,2), new Vector3(0,1.2f,0), 2, new Color(.73f,.84f,1));
            AddPortraitLight("Gate1_Rim", new Vector3(0,3,-2), new Vector3(0,1.3f,0), 4, new Color(1,.86f,.68f));
            Debug.Log("Opened isolated Studio preview; active saved scene was not edited.");
        }

        private static void AddPortraitLight(string name, Vector3 position, Vector3 target, float intensity, Color color)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, _preview);
            go.transform.position = PortraitOrigin + position; go.transform.LookAt(PortraitOrigin + target);
            var light = go.AddComponent<Light>(); light.type = LightType.Spot; light.range = 12; light.spotAngle = 65;
            light.intensity = intensity; light.color = color; light.shadows = LightShadows.Soft;
        }

        [MenuItem("SilverScreen/Characters/Gate 1/3 Capture identity review")]
        public static void CaptureIdentity()
        {
            if (!_preview.IsValid()) Open();
            foreach (var view in Views) { CharacterMotion.Sample(view, CharacterPose.Idle, 0); view.SetExpression(0,0,0,0,Vector2.zero); view.SetHairVisible(true); }
            Capture("01_lineup", PortraitOrigin + new Vector3(0,1.15f,5.8f), PortraitOrigin + Vector3.up * 1.02f, 30, 1600, 1000);
            foreach (var view in Views)
            {
                var eyes = view.Eyes; string id = view.Appearance.calibrationId;
                Capture(id + "_face", eyes + new Vector3(0,.03f,1.05f), eyes - Vector3.up * .055f, 31, 900, 1000);
                Capture(id + "_threequarter", eyes + new Vector3(.65f,.02f,.85f), eyes - Vector3.up * .055f, 31, 900, 1000);
                Capture(id + "_profile", eyes + new Vector3(1.08f,.01f,0), eyes - Vector3.up * .055f, 31, 900, 1000);
                view.SetHairVisible(false);
                Capture(id + "_identity_hair_hidden", eyes + new Vector3(0,.03f,1.05f), eyes - Vector3.up * .055f, 31, 900, 1000);
                view.SetHairVisible(true);
            }
        }

        public static void Capture(string name, Vector3 position, Vector3 target, float fov, int width = 1400, int height = 1000)
        {
            Directory.CreateDirectory(ReviewRoot + "/Captures");
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target-position));
            _camera.fieldOfView = fov; _camera.aspect = (float)width / height;
            var rt = RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; Texture2D image = null;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                for (int i=0;i<3;i++) RenderPipeline.SubmitRenderRequest(_camera,request);
                RenderTexture.active=rt;image=new Texture2D(width,height,TextureFormat.RGB24,false,false);
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(ReviewRoot+"/Captures/"+name+".png",image.EncodeToPNG());
            }
            finally { RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image); }
        }

        [MenuItem("SilverScreen/Characters/Gate 1/Close isolated preview")]
        public static void Close()
        {
            Views.Clear();
            foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>().Where(c => c.name == "Gate1_ReviewCamera" && EditorSceneManager.IsPreviewSceneObject(c)).ToArray())
                if (camera != null && camera.gameObject.scene.IsValid()) EditorSceneManager.ClosePreviewScene(camera.gameObject.scene);
            _preview=default;_camera=null;
        }
    }
}
