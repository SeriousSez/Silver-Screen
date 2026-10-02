using System;
using System.IO;
using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Reproducible local evaluation, never changes recruitment, Studio or purchased sources.</summary>
    public static class HumanBasePrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/HumanBasePrototype";
        public const string PrefabPath = Root + "/Prefabs/SS_CharacterPrototype.prefab";
        public const string Review = "TestResults/CharacterBaseAudit";
        public const float AdultHeight = 1.75f;
        private const string Source = "C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/";
        private static Scene _reviewScene;

        [MenuItem("SilverScreen/Characters/Human Base Prototype/1 Build local prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
            foreach (var folder in new[] { "Models", "Animation", "Materials", "Prefabs" }) Directory.CreateDirectory(Root + "/" + folder);
            Copy(Source + "stylized-female-body-base/Stylized_Female_Body_Base_Shape_Keys/Shape Keys/StylizedFemaleBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx", Root + "/Models/AdultFemale.fbx");
            Copy("ExternalSourceAssets/Animations/Basic Locomotion Pack/idle.fbx", Root + "/Animation/Idle.fbx");
            Copy("ExternalSourceAssets/Animations/Basic Locomotion Pack/walking.fbx", Root + "/Animation/Walk.fbx");
            Copy("ExternalSourceAssets/Animations/Female Basic Locomotion Pack/running.fbx", Root + "/Animation/Run.fbx");
            AssetDatabase.Refresh();
            ConfigureImport(Root + "/Models/AdultFemale.fbx", false);
            foreach (var clip in new[] { "Idle", "Walk", "Run" }) ConfigureImport(Root + "/Animation/" + clip + ".fbx", true);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/AdultFemale.fbx");
            var avatar = model.GetComponent<Animator>().avatar;
            if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("A valid mapped Humanoid is required.");

            var controllerPath = Root + "/Animation/PrototypeLocomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = Clip("Idle");
                var walk = machine.AddState("Walk"); walk.motion = Clip("Walk");
                var run = machine.AddState("Run"); run.motion = Clip("Run");
                machine.defaultState = idle;
                Transition(idle, walk, AnimatorConditionMode.Greater, .05f);
                Transition(walk, idle, AnimatorConditionMode.Less, .05f);
                Transition(walk, run, AnimatorConditionMode.Greater, 2.3f);
                Transition(run, walk, AnimatorConditionMode.Less, 2.3f);
            }
            var materialPath = Root + "/Materials/SuppliedNeutralURP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Supplied FBX is untextured neutral grey, not a production skin shader.
                material.SetColor("_BaseColor", new Color(.9058823f, .9058823f, .9058823f));
                material.SetFloat("_Smoothness", .3f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var go = new GameObject("SS_CharacterPrototype"); go.SetActive(false);
            try
            {
                var nav = go.AddComponent<NavMeshAgent>();
                nav.radius = .35f; nav.height = 2; nav.baseOffset = 1;
                nav.speed = 1.35f; nav.acceleration = 8; nav.angularSpeed = 360; nav.stoppingDistance = .12f;
                var collider = go.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = .35f;
                var employee = go.AddComponent<EmployeeAgent>(); employee.SetGenericFilmingPresentationEnabled(false);
                var pivot = new GameObject("CharacterVisualRoot").transform; pivot.SetParent(go.transform, false);
                pivot.localPosition = Vector3.down;
                var visual = Object.Instantiate(model, pivot); visual.name = "AdultFemale";
                // Blender source bounds: -0.00134089 to 1.88475347 metres. Import scale is preserved.
                float scale = AdultHeight / 1.88609456f;
                visual.transform.localScale = Vector3.one * scale;
                visual.transform.localPosition = Vector3.up * (.00134089f * scale);
                var animator = visual.GetComponent<Animator>(); animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>(); skin.sharedMaterial = material;
                skin.updateWhenOffscreen = true; skin.quality = SkinQuality.Auto;
                var presentation = go.AddComponent<HumanBasePrototypeVisual>(); presentation.Configure(animator, pivot, skin);
                go.AddComponent<HeldPersonPresentation>();
                var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.name = "SelectionIndicator"; indicator.transform.SetParent(go.transform, false);
                indicator.transform.localPosition = new Vector3(0, -.98f, 0);
                indicator.transform.localScale = new Vector3(.8f, .01f, .8f);
                Object.DestroyImmediate(indicator.GetComponent<Collider>());
                indicator.GetComponent<Renderer>().sharedMaterial = material;
                employee.SetSelectionIndicator(indicator); indicator.SetActive(false);
                go.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally { Object.DestroyImmediate(go); }
            AssetDatabase.SaveAssets();
            WriteImportReport();
            Debug.Log("Human base local prototype built: " + PrefabPath);
        }

        private static void Copy(string source, string destination)
        {
            // Preserve existing GUIDs and files. Refuse silent replacement of an edited local import.
            if (!File.Exists(destination)) File.Copy(source, destination);
            else if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
                throw new InvalidOperationException("Local import differs from source: " + destination);
        }

        private static void ConfigureImport(string path, bool animation)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.globalScale = 1; importer.useFileScale = true;
            importer.importAnimation = animation; importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importCameras = false; importer.importLights = false; importer.importVisibility = false;
            importer.optimizeGameObjects = false;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 10; importer.minBoneWeight = .00001f;
            if (animation)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.name = Path.GetFileNameWithoutExtension(path);
                    clip.loopTime = true; clip.loopPose = true;
                    clip.lockRootRotation = true; clip.keepOriginalOrientation = true;
                    clip.lockRootHeightY = true; clip.heightFromFeet = true;
                    clip.lockRootPositionXZ = true; clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        public static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "/Animation/" + name + ".fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));

        private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        { var t = from.AddTransition(to); t.hasExitTime = false; t.duration = .12f; t.AddCondition(mode, threshold, "Speed"); }

        public static GameObject Spawn(Vector3 ground, string name = "Prototype employee", string prefabPath = PrefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("Build the local prototype first.");
            var go = Object.Instantiate(prefab, ground + Vector3.up, Quaternion.identity);
            go.name = name;
            go.GetComponent<EmployeeAgent>().BindDomain(new Employee(Guid.NewGuid().ToString(), name, EmployeeRole.Actor, 50, 1000));
            return go;
        }

        [MenuItem("SilverScreen/Characters/Human Base Prototype/2 Open isolated Studio review")]
        public static void OpenReview()
        {
            CloseReview();
            var active = SceneManager.GetActiveScene();
            _reviewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _reviewScene.name = "HumanBasePrototype_UnsavedReview";
            SceneManager.SetActiveScene(active);
            var go = Spawn(new Vector3(0, 0, -8));
            SceneManager.MoveGameObjectToScene(go, _reviewScene);
            go.GetComponent<NavMeshAgent>().enabled = false;
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.Frame(new Bounds(go.transform.position, Vector3.one * 2.5f), false);
        }

        [MenuItem("SilverScreen/Characters/Human Base Prototype/3 Close isolated review")]
        public static void CloseReview()
        {
            // Resolve by path-independent name as static handles are lost on domain reload.
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "HumanBasePrototype_UnsavedReview" && string.IsNullOrEmpty(scene.path))
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void Capture(Camera camera, string name, int width = 1280, int height = 960)
        {
            Directory.CreateDirectory(Review);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; Texture2D image = null;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                for (int i = 0; i < 3; i++) RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; image = new Texture2D(width, height, TextureFormat.RGB24, false, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Review + "/" + name + ".png", image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); if (image != null) Object.DestroyImmediate(image); }
        }

        private static void WriteImportReport()
        {
            Directory.CreateDirectory(Review);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/AdultFemale.fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/Models/AdultFemale.fbx");
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            var lines = importer.humanDescription.human.Select(h => h.humanName + " = " + h.boneName).ToList();
            lines.Add("Avatar valid=" + model.GetComponent<Animator>().avatar.isValid + ", human=" + model.GetComponent<Animator>().avatar.isHuman);
            lines.Add("Vertices=" + skin.sharedMesh.vertexCount + ", triangles=" + skin.sharedMesh.triangles.Length / 3 + ", bones=" + skin.bones.Length);
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) lines.Add("Shape: " + skin.sharedMesh.GetBlendShapeName(i));
            foreach (var name in new[] { "Idle", "Walk", "Run" }) lines.Add(name + ": human=" + Clip(name).humanMotion + ", seconds=" + Clip(name).length);
            File.WriteAllLines(Review + "/unity-import.txt", lines);
        }
    }
}
