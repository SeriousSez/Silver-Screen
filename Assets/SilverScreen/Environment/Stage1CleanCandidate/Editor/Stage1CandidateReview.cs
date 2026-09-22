using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SilverScreen.Editor
{
    /// <summary>Import and isolated production review. Never saves Studio.unity.</summary>
    public static class Stage1CandidateReview
    {
        public const string Root = "Assets/SilverScreen/Environment/Stage1CleanCandidate";
        public const string ModelPath = Root + "/Models/Stage1_CleanCandidate_A.fbx";
        public const string PrefabPath = Root + "/Stage1_CleanCandidate_A.prefab";
        public const string ReviewPath = Root + "/Stage1_Candidate_A_Review.unity";
        public const string CaptureRoot = "ArtReview/Stage1CleanCandidate";

        public static Scene GetReviewScene()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == ReviewPath) return active;
            var root = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(o => o.name == "Stage1_CleanCandidate_A_REVIEW_ONLY" && EditorSceneManager.IsPreviewSceneObject(o));
            if (root != null) return root.scene;
            return default;
        }

        public static void OpenPreviewReview()
        {
            if (!GetReviewScene().IsValid()) EditorSceneManager.OpenPreviewScene(ReviewPath);
        }

        public static GameObject FindReviewObject(string name)
        {
            return GetReviewScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).First(t => t.name == name).gameObject;
        }

        private static Camera ReviewCamera => GetReviewScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>(true)).First(c => c.CompareTag("MainCamera"));

        [Serializable] private class MaterialRecord
        {
            public string name;
            public float[] linear_rgb;
            public float metallic;
            public float roughness;
            public string texture_set;
        }
        [Serializable] private class Dimensions
        {
            public float sign_baseline;
            public float sign_letter_height;
            public float sign_maximum_width;
        }
        [Serializable] private class LightRecord
        {
            public string name;
            public float[] position;
            public float[] direction;
            public float[] color;
            public float intensity;
            public float range;
            public float spot_angle;
        }
        [Serializable] private class Report { public MaterialRecord[] materials; public Dimensions dimensions; public LightRecord[] permanent_lights; }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/1 Import production candidate")]
        public static void ImportCandidate()
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(Root + "/Materials"))
                AssetDatabase.CreateFolder(Root, "Materials");
            var report = JsonUtility.FromJson<Report>(File.ReadAllText("ArtSource/Stage1CleanCandidate/generation_report.json"));
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new FileNotFoundException(ModelPath);
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importCameras = false;
            importer.importLights = false;
            foreach (var record in report.materials)
            {
                string path = Root + "/Materials/" + record.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                var rgb = record.linear_rgb;
                mat.SetColor("_BaseColor", new Color(rgb[0], rgb[1], rgb[2], 1).gamma);
                mat.SetFloat("_Metallic", record.metallic);
                mat.SetFloat("_Smoothness", 1 - record.roughness);
                if (!string.IsNullOrEmpty(record.texture_set))
                {
                    string basePath = Root + "/Textures/" + record.texture_set;
                    ConfigureTexture(basePath + "_albedo.png", false);
                    ConfigureTexture(basePath + "_normal.png", true);
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_albedo.png"));
                    mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_normal.png"));
                    mat.SetFloat("_BumpScale", 1);
                    mat.SetFloat("_SmoothnessTextureChannel", 1);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                }
                if (record.name == "C1_GlassStudy")
                {
                    var glass = new Color(rgb[0], rgb[1], rgb[2], 1).gamma; glass.a = .30f;
                    mat.SetColor("_BaseColor", glass);
                    mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0);
                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0);
                    mat.SetFloat("_BlendModePreserveSpecular", 1);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    mat.SetShaderPassEnabled("ShadowCaster", false);
                }
                if (record.name == "C1_LampGlass" || record.name == "C1_LampReflector")
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, .73f, .40f) * (record.name == "C1_LampGlass" ? 6f : .6f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                BaseShaderGUI.SetMaterialKeywords(mat, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
                if (record.name == "C1_GlassStudy") mat.SetShaderPassEnabled("ShadowCaster", false);
                EditorUtility.SetDirty(mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), record.name), mat);
            }
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("Candidate A imported with source palette and retained candidate material GUIDs.");
        }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/6 Capture final correction review")]
        public static void CaptureCorrectionReview()
        {
            OpenPreviewReview();
            CaptureAll();
            Capture("23_ground_front",new Vector3(9,.22f,-29),new Vector3(5.7f,.45f,-24),58,1500,900,.02f,true);
            Capture("24_ground_rear",new Vector3(-6,.25f,6),new Vector3(0,.50f,0),60,1500,900,.02f,true);
            Capture("25_ground_hero_side",new Vector3(13,.20f,-7),new Vector3(7.9f,.40f,-14),60,1500,900,.02f,true);
            Capture("26_ground_opposite_side",new Vector3(-12,.20f,-18),new Vector3(-7.9f,.40f,-10),60,1500,900,.02f,true);
            Capture("27_personnel_frontal",new Vector3(5.70f,1.4f,-28.2f),new Vector3(5.7f,1.35f,-24.15f),42,1000,1300,.02f,true);
            Capture("28_side_door_oblique",new Vector3(10.3f,.60f,-19.7f),new Vector3(7.85f,1.15f,-22.025f),45,1000,1300,.02f,true);
            Capture("29_lamp_column_clearance",new Vector3(4.3f,2.7f,-10.1f),new Vector3(7.4f,3.0f,-10.65f),54,1500,1000,.02f,true);
            Capture("30_panel_switch_controls",new Vector3(5.60f,1.28f,-20.55f),new Vector3(7.18f,1.20f,-20.3f),48,1100,1400,.02f,true);
            Capture("31_personnel_lock_pull",new Vector3(6.45f,1.35f,-25.50f),new Vector3(6.03f,1.07f,-24.23f),38,1000,1200,.02f,true);
            Capture("32_canvas_awning",new Vector3(7.9f,3.15f,-26.4f),new Vector3(5.7f,2.85f,-24.45f),42,1500,1000,.02f,true);
            Capture("33_pilaster_crown",new Vector3(10.6f,8.0f,-28.5f),new Vector3(7.54f,7.38f,-24.04f),32,1200,1200,.02f,true);
            Capture("34_roof_grazing",new Vector3(11,10.8f,-23),new Vector3(4,10,-12),45,1500,1000,.02f,true);
            Capture("35_roof_blackout_structure",new Vector3(0,6,-17),new Vector3(4.4f,9.5f,-13),58,1500,1000,.02f,true);
            Capture("36_clerestory_roller",new Vector3(6.6f,5.55f,-9),new Vector3(7.50f,6.05f,-11),62,1500,1000,.02f,true);
            var light=GetReviewScene().GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);var rotation=light.transform.rotation;
            try {
            light.transform.rotation=Quaternion.Euler(35,200,0);
            Capture("37_roof_direct_sun",new Vector3(10.6f,12.8f,-18),new Vector3(3.3f,10,-14),52,1500,1000,.02f,true);
            light.transform.rotation=Quaternion.Euler(40,70,0);
            Capture("38_roof_shaded",new Vector3(10.6f,12.8f,-18),new Vector3(3.3f,10,-14),52,1500,1000,.02f,true);
            } finally { light.transform.rotation=rotation; }
            Capture("39_ground_front_left",new Vector3(-9,.20f,-29),new Vector3(-5.7f,.40f,-24),58,1500,900,.02f,true);
            Capture("40_front_left_door",new Vector3(-5.97f,1.4f,-28.3f),new Vector3(-5.97f,1.3f,-24.1f),42,1000,1300,.02f,true);
            Capture("41_hero_rear_service_door",new Vector3(12,1.5f,-1),new Vector3(7.9f,1.2f,-1.975f),42,1000,1300,.02f,true);
            Capture("42_opposite_front_service_door",new Vector3(-12,1.5f,-22),new Vector3(-7.9f,1.2f,-22.025f),42,1000,1300,.02f,true);
            Capture("43_opposite_rear_service_door",new Vector3(-12,1.5f,-1),new Vector3(-7.9f,1.2f,-1.975f),42,1000,1300,.02f,true);
            Capture("44_rear_service_door",new Vector3(0,1.5f,5),new Vector3(0,1.35f,.2f),42,1000,1300,.02f,true);
            Capture("45_ground_rear_right",new Vector3(6,.20f,6),new Vector3(0,.40f,0),58,1500,900,.02f,true);
            Capture("46_ground_hero_side_reverse",new Vector3(13,.20f,-18),new Vector3(7.9f,.40f,-10),60,1500,900,.02f,true);
            Capture("47_ground_opposite_side_reverse",new Vector3(-12,.20f,-7),new Vector3(-7.9f,.40f,-14),60,1500,900,.02f,true);
        }

        private static void ConfigureTexture(string path, bool normal)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool changed = importer.textureType != type || importer.sRGBTexture == normal || importer.anisoLevel != 8;
            importer.textureType = type; importer.sRGBTexture = !normal;
            importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 8;
            importer.mipmapEnabled = true; importer.maxTextureSize = 1024;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (changed) importer.SaveAndReimport();
        }

        private static void UpdatePermanentLighting(GameObject candidate, Report report)
        {
            if (report.permanent_lights == null) return;
            foreach (var old in candidate.transform.Cast<Transform>().Where(t => t.name == "PermanentWorkLighting").ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("PermanentWorkLighting");
            SceneManager.MoveGameObjectToScene(root, candidate.scene);
            root.transform.SetParent(candidate.transform, false);
            int index = 0;
            foreach (var record in report.permanent_lights)
            {
                var go = new GameObject(record.name + "_" + index++);
                SceneManager.MoveGameObjectToScene(go, candidate.scene);
                go.transform.SetParent(root.transform, false);
                var p = record.position; var d = record.direction;
                go.transform.localPosition = new Vector3(p[0], p[2], p[1]);
                go.transform.localRotation = Quaternion.LookRotation(new Vector3(d[0], d[2], d[1]), Vector3.forward);
                var light = go.AddComponent<Light>(); light.type = LightType.Spot;
                light.color = new Color(record.color[0], record.color[1], record.color[2]);
                light.intensity = record.intensity; light.range = record.range;
                light.spotAngle = record.spot_angle; light.innerSpotAngle = record.spot_angle * .65f;
                light.shadows = LightShadows.Soft;
                light.shadowBias = .025f; light.shadowNormalBias = .12f;
                var urpLight = go.AddComponent<UniversalAdditionalLightData>();
                var lightData = new SerializedObject(urpLight);
                lightData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 0;
                lightData.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/2 Create isolated review scene")]
        public static void CreateReview()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Studio.unity" || scene.isDirty || EditorApplication.isPlaying)
                throw new InvalidOperationException("Open the saved Studio scene in Edit Mode before creating the review copy.");
            if (File.Exists(ReviewPath))
                throw new InvalidOperationException("Review already exists. Open it; do not overwrite review edits.");
            ImportCandidate();
            if (!EditorSceneManager.SaveScene(scene, ReviewPath, true))
                throw new IOException("Could not create isolated Studio review copy.");
            scene = EditorSceneManager.OpenScene(ReviewPath, OpenSceneMode.Single);
            var fallback = GameObject.Find("SoundStage1");
            var candidate = new GameObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
            Undo.RegisterCreatedObjectUndo(candidate, "Place Stage massing candidate");
            candidate.transform.position = fallback.transform.position;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            model.transform.SetParent(candidate.transform, false);
            // Blender's authored X/Y ground plane uses the established project
            // model-root correction. The wrapper has Unity's ordinary Y-up pivot.
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(-90, 180, 0);
            model.transform.localScale = Vector3.one;

            // Identity is instance data. No StudioBuildingView: a review candidate
            // must never register a duplicate production facility with the router.
            var identity = candidate.AddComponent<SoundStageIdentity>();
            var letters = new GameObject("InstanceDrivenStageLettering");
            letters.transform.SetParent(candidate.transform, false);
            letters.transform.localPosition = new Vector3(0, 8.90f, -12.035f);
            var sign = letters.AddComponent<StageArchitecturalSign>();
            var signData = new SerializedObject(sign);
            signData.FindProperty("_identity").objectReferenceValue = identity;
            signData.FindProperty("_material").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/C1_Steel.mat");
            signData.FindProperty("_letterHeight").floatValue = 1.12f;
            signData.FindProperty("_maximumWidth").floatValue = 5.6f;
            const string chars = "AEGST0123456789";
            signData.FindProperty("_characters").stringValue = chars;
            var glyphs = signData.FindProperty("_glyphs");
            glyphs.arraySize = chars.Length;
            for (int i = 0; i < chars.Length; i++)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SilverScreen/Environment/ReferenceKit/StageGlyphs/Glyph_" + chars[i] + ".asset");
                if (mesh == null) throw new FileNotFoundException("Stage glyph " + chars[i]);
                glyphs.GetArrayElementAtIndex(i).objectReferenceValue = mesh;
            }
            signData.ApplyModifiedPropertiesWithoutUndo();
            var identityData = new SerializedObject(identity);
            identityData.FindProperty("_architecturalSign").objectReferenceValue = sign;
            identityData.ApplyModifiedPropertiesWithoutUndo();
            identity.Initialize("review-candidate-stage-1", 1, null);

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (new[] { "ExteriorWalls", "InteriorFloor", "MainDoorLeft", "MainDoorRight", "PersonnelDoors" }.Any(n => filter.name.Contains(n)))
                {
                    var collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                }
            }
            AddAnchor(candidate, "MainPortal", new Vector3(0, .08f, -12.05f));
            AddAnchor(candidate, "FrontPersonnelLeft", new Vector3(-5.8f, .08f, -12.5f));
            AddAnchor(candidate, "FrontPersonnelRight", new Vector3(5.8f, .08f, -12.5f));
            AddAnchor(candidate, "FilmingFloorCentre", new Vector3(0, .08f, 0));
            AddAnchor(candidate, "InteriorCameraStudy", new Vector3(0, 2.3f, -9.8f));
            PrefabUtility.SaveAsPrefabAsset(candidate, PrefabPath);

            // Only the COPY is changed. The complete fallback hierarchy remains
            // available here, and Studio.unity retains its active original stage.
            Undo.RecordObject(fallback, "Hide fallback in candidate review copy");
            fallback.SetActive(false);
            EditorSceneManager.SaveScene(scene);
            UpdateReviewCandidate();
            Selection.activeGameObject = candidate;
            Debug.Log("Review created at " + ReviewPath + "; original Studio scene untouched.");
        }

        private static void AddAnchor(GameObject root, string name, Vector3 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = position;
        }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/4 Refresh candidate from revised source")]
        public static void UpdateReviewCandidate()
        {
            var reviewScene = GetReviewScene();
            if (!reviewScene.IsValid() || EditorApplication.isPlaying)
                throw new InvalidOperationException("Open the isolated candidate review in Edit Mode.");
            ImportCandidate();
            var candidate = FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
            if (candidate == null) throw new InvalidOperationException("Candidate review root missing.");
            // Saving this instance back to its own prefab can reapply added-child
            // overrides during the save, duplicating the generated light root.
            // Keep the review wrapper independent while editing; retain nested
            // imported-model prefab links through OutermostRoot unpacking.
            if (PrefabUtility.IsPartOfPrefabInstance(candidate))
                PrefabUtility.UnpackPrefabInstance(candidate, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var report = JsonUtility.FromJson<Report>(File.ReadAllText("ArtSource/Stage1CleanCandidate/generation_report.json"));
            var sign = candidate.GetComponentInChildren<StageArchitecturalSign>();
            Undo.RecordObject(sign.transform, "Update source-driven candidate sign position");
            sign.transform.localPosition = new Vector3(0, report.dimensions.sign_baseline, -12.035f);
            sign.enabled = false;
            var data = new SerializedObject(sign);
            data.FindProperty("_letterHeight").floatValue = report.dimensions.sign_letter_height;
            data.FindProperty("_maximumWidth").floatValue = report.dimensions.sign_maximum_width;
            data.ApplyModifiedProperties();
            sign.enabled = true;
            candidate.GetComponent<SoundStageIdentity>().RefreshSign();
            UpdatePermanentLighting(candidate, report);
            var exteriorReflection = candidate.transform.Find("ExteriorReflectionAnchor");
            if (exteriorReflection == null)
            {
                AddAnchor(candidate, "ExteriorReflectionAnchor", new Vector3(0, 13, 0));
                exteriorReflection = candidate.transform.Find("ExteriorReflectionAnchor");
            }
            foreach (var renderer in candidate.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.Contains("WindowGlass")) renderer.shadowCastingMode = ShadowCastingMode.Off;
                // Roof metal/glass must sample the studio sky rather than the
                // enclosed interior probe whose volume also spans the barrel.
                if (new[] { "Export_Roof", "Export_RoofSeams", "Export_RoofGlazing", "Export_RoofVents", "Export_Rainwater" }.Contains(renderer.name))
                    renderer.probeAnchor = exteriorReflection;
            }
            candidate.transform.Find("FrontPersonnelLeft").localPosition = new Vector3(-5.97f, .08f, -12.5f);
            candidate.transform.Find("FrontPersonnelRight").localPosition = new Vector3(5.70f, .08f, -12.5f);
            foreach (var filter in candidate.GetComponentsInChildren<MeshFilter>())
            {
                if (new[] { "ExteriorWalls", "InteriorFloor", "MainDoorLeft", "MainDoorRight", "PersonnelDoors" }.Any(n => filter.name.Contains(n)))
                {
                    var collider = filter.GetComponent<MeshCollider>() ?? Undo.AddComponent<MeshCollider>(filter.gameObject);
                    collider.sharedMesh = filter.sharedMesh;
                }
            }
            SaveCandidateReview();
        }

        public static void SaveCandidateReview()
        {
            var reviewScene = GetReviewScene();
            var candidate = FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
            PrefabUtility.SaveAsPrefabAsset(candidate, PrefabPath);
            EditorSceneManager.MarkSceneDirty(reviewScene);
            if (EditorSceneManager.IsPreviewScene(reviewScene))
            {
                // Preview scenes cannot be saved by Unity. Persist only our prefab
                // into a briefly loaded additive review copy; Studio stays open.
                var savedReview = EditorSceneManager.OpenScene(ReviewPath, OpenSceneMode.Additive);
                try
                {
                    var existing = savedReview.GetRootGameObjects().First(o => o.name == candidate.name);
                    var position = existing.transform.position;
                    var rotation = existing.transform.rotation;
                    UnityEngine.Object.DestroyImmediate(existing);
                    var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), savedReview);
                    copy.name = candidate.name;
                    copy.transform.SetPositionAndRotation(position, rotation);
                    if (!EditorSceneManager.SaveScene(savedReview)) throw new IOException("Could not save isolated candidate review.");
                }
                finally { EditorSceneManager.CloseScene(savedReview, true); }
            }
            else if (!EditorSceneManager.SaveScene(reviewScene, ReviewPath)) throw new IOException("Could not save isolated candidate review.");
        }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/5 Bake interior reflection")]
        public static void BakeInteriorReflection()
        {
            var root = FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
            var child = root.transform.Find("InteriorReflection");
            if (child == null)
            {
                var go = new GameObject("InteriorReflection");
                SceneManager.MoveGameObjectToScene(go, root.scene);
                go.transform.SetParent(root.transform, false); child = go.transform;
            }
            child.localPosition = new Vector3(0, 3.3f, 0);
            var probe = child.GetComponent<ReflectionProbe>() ?? child.gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked; probe.resolution = 128; probe.hdr = true;
            // Include the floor inside the full-strength region, not the blend edge.
            probe.boxProjection = true; probe.size = new Vector3(15.6f, 11.6f, 23.8f);
            probe.center = new Vector3(0, 2, 0); probe.blendDistance = .25f; probe.importance = 10;
            probe.nearClipPlane = .15f; probe.farClipPlane = 40; probe.cullingMask = -1; probe.intensity = 1;
            string path = Root + "/Textures/Stage1InteriorReflection.exr";
            // Native baked-probe rendering excludes preview-scene/non-static
            // geometry. Bake a disposable regular-scene copy, restricted to its
            // own layer, without changing static flags or layers on live assets.
            var bakeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), bakeScene);
                copy.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
                int layer = Enumerable.Range(8, 24).Reverse().First(i => string.IsNullOrEmpty(LayerMask.LayerToName(i)) && !Resources.FindObjectsOfTypeAll<GameObject>().Any(o => o.scene.IsValid() && o.scene != bakeScene && o.layer == i));
                foreach (var t in copy.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = layer;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ReflectionProbeStatic);
                }
                var bakeProbe = copy.GetComponentInChildren<ReflectionProbe>();
                EditorUtility.CopySerialized(probe, bakeProbe);
                bakeProbe.mode = ReflectionProbeMode.Baked;
                bakeProbe.cullingMask = 1 << layer;
                foreach (var light in copy.GetComponentsInChildren<Light>()) light.cullingMask = 1 << layer;
                if (!Lightmapping.BakeReflectionProbe(bakeProbe, path)) throw new IOException("Interior reflection bake failed.");
            }
            finally { EditorSceneManager.CloseScene(bakeScene, true); }
            AssetDatabase.ImportAsset(path);
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (probe.customBakedTexture == null) throw new IOException("Interior reflection cubemap missing.");
            SaveCandidateReview();
        }

        /// <summary>Render the ACTUAL Main Camera via URP; restore all camera state.</summary>
        public static void Capture(string name, Vector3 position, Vector3 target, float fov = 50, int width = 1600, int height = 1000, float near = .1f, bool isolateCandidate = false)
        {
            var reviewScene = GetReviewScene();
            if (!reviewScene.IsValid())
                throw new InvalidOperationException("Captures must run in the isolated review scene.");
            Directory.CreateDirectory(CaptureRoot);
            // Native high-resolution review renders resolve millimetre bevels.
            // The two management views retain their normal 1600 x 1000 output.
            if (!name.Contains("management")) { width *= 2; height *= 2; }
            var camera = ReviewCamera;
            var oldCameraScene = camera.scene;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            float oldFov = camera.fieldOfView;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            float oldAspect = camera.aspect;
            float oldNear = camera.nearClipPlane;
            int oldMask = camera.cullingMask;
            var layers = new Dictionary<GameObject, int>();
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D image = null;
            try
            {
                camera.scene = reviewScene;
                if (isolateCandidate)
                {
                    // Only candidate and Ground change temporary layers. Neighbours,
                    // including Administration, are never touched, even transiently.
                    var all = reviewScene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
                    int layer = Enumerable.Range(8, 24).Reverse().First(i => string.IsNullOrEmpty(LayerMask.LayerToName(i)) && !all.Any(t => t.gameObject.layer == i));
                    var candidate = FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
                    foreach (var transform in candidate.GetComponentsInChildren<Transform>(true))
                    {
                        layers.Add(transform.gameObject, transform.gameObject.layer);
                        transform.gameObject.layer = layer;
                    }
                    var ground = FindReviewObject("Ground");
                    layers.Add(ground, ground.layer);
                    ground.layer = layer;
                    camera.cullingMask = 1 << layer;
                }
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation(target - position);
                camera.fieldOfView = fov;
                camera.aspect = (float)width / height;
                camera.nearClipPlane = near;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                // Settle camera/render state after large changes of review pose.
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = rt;
                image = new Texture2D(width, height, TextureFormat.RGB24, false, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(CaptureRoot + "/" + name + ".png", image.EncodeToPNG());
                Debug.Log("UNITY_CAPTURE " + name + " position=" + position + " target=" + target + " fov=" + fov);
            }
            finally
            {
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                camera.scene = oldCameraScene;
                camera.fieldOfView = oldFov;
                camera.aspect = oldAspect;
                camera.nearClipPlane = oldNear;
                camera.cullingMask = oldMask;
                foreach (var item in layers) item.Key.layer = item.Value;
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(rt);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
        }

        public static void CaptureMatched(string prefix = "")
        {
            Capture(prefix + "01_front_three_quarter", new Vector3(27, 3.2f, -46), new Vector3(0, 5.5f, -12), 23.5f, 1536, 515, isolateCandidate: true);
            Capture(prefix + "02_front", new Vector3(0, 5.4f, -56), new Vector3(0, 5.4f, -24), 23.5f, 1455, 717, isolateCandidate: true);
            Capture(prefix + "03_side", new Vector3(-44, 4, -33.5f), new Vector3(0, 5.2f, -12), 20.5f, 1677, 717, isolateCandidate: true);
            Capture(prefix + "04_rear", new Vector3(18, 3.7f, 40), new Vector3(0, 5.4f, -9), 20, 1392, 717, isolateCandidate: true);
        }

        [MenuItem("SilverScreen/Art/Stage 1 Candidate/3 Capture review views")]
        public static void CaptureAll()
        {
            // Candidate world centre is (0,0,-12), front facade Z=-24.
            CaptureMatched();
            Capture("05_roof", new Vector3(23, 30, -38), new Vector3(0, 5, -12), 49, isolateCandidate: true);
            var camera = ReviewCamera;
            Capture("06_management_original_pose", camera.transform.position, camera.transform.position + camera.transform.forward * 25, camera.fieldOfView);
            // Same 52-degree pitch and 60-degree FOV, panned over the candidate.
            var aim = new Vector3(0, 0, -12);
            var rotation = Quaternion.Euler(52, 0, 0);
            Capture("07_management_candidate", aim - rotation * Vector3.forward * 34, aim, 60);
            Capture("08_beside_administration", new Vector3(-15, 6, -75), new Vector3(-15, 5.4f, -17), 42);
            Capture("09_interior_wide", new Vector3(0, 2.35f, -22.4f), new Vector3(0, 5, -2), 85);
            CaptureStructure();
            CaptureProductionDetails();
        }

        public static void CaptureProductionDetails(string prefix = "")
        {
            Capture(prefix + "12_interior_reverse", new Vector3(0, 2.1f, -5.3f), new Vector3(0, 4.5f, -24), 78);
            Capture(prefix + "13_main_door_detail", new Vector3(1.15f, 1.9f, -27.3f), new Vector3(.03f, 1.75f, -24.04f), 43, isolateCandidate: true);
            Capture(prefix + "14_personnel_entrance", new Vector3(7.5f, 2.7f, -30), new Vector3(5.7f, 2.35f, -24.1f), 46, 1160, 1000, isolateCandidate: true);
            Capture(prefix + "15_truss_and_rigging", new Vector3(0, 5.25f, -19.5f), new Vector3(0, 8.4f, -10), 75);
            Capture(prefix + "16_stair_and_catwalk", new Vector3(1, 4.3f, -13), new Vector3(-.8f, 3.5f, -2), 66);
            Capture(prefix + "17_inside_stage_doors", new Vector3(-3.1f, 2.9f, -17), new Vector3(0, 3.5f, -23.55f), 70);
            Capture(prefix + "18_clerestory_detail", new Vector3(13.8f, 5.4f, -14.3f), new Vector3(7.85f, 5.65f, -14.3f), 57, isolateCandidate: true);
            Capture(prefix + "19_roof_construction", new Vector3(10.6f, 12.8f, -18), new Vector3(3.3f, 10, -14), 52, isolateCandidate: true);
            Capture(prefix + "20_door_pulls_close", new Vector3(.7f, 1.85f, -25.6f), new Vector3(0, 1.77f, -24.04f), 42, isolateCandidate: true);
            Capture(prefix + "21_service_construction", new Vector3(11.2f, 1.7f, -18.5f), new Vector3(8.10f, 1.28f, -19.6f), 48, isolateCandidate: true);
            Capture(prefix + "22_rear_services", new Vector3(-4.4f, 2.2f, 5.3f), new Vector3(-1.5f, 1.9f, .15f), 52, isolateCandidate: true);
        }

        public static void CaptureStructure()
        {
            var root = FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
            var states = root.GetComponentsInChildren<Renderer>().ToDictionary(r => r, r => r.enabled);
            var visible = new[] { "InteriorFloor", "PrimaryStructure", "StructuralConnections", "RiggingPrimary", "CirculationAllowances", "CatwalkStructure", "CirculationGuards", "Stairs", "PermanentLighting" };
            try
            {
                foreach (var r in states.Keys) r.enabled = visible.Any(n => r.name.Contains(n));
                Capture("10_structure_cutaway", new Vector3(23, 20, -40), new Vector3(0, 4.7f, -12), 48, isolateCandidate: true);
                Capture("11_rear_circulation_cutaway", new Vector3(4, 5.6f, -9.5f), new Vector3(-1, 3.2f, -3), 60, isolateCandidate: true);
            }
            finally { foreach (var state in states) state.Key.enabled = state.Value; }
        }
    }
}
