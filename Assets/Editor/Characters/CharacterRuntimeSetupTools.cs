using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SilverScreen.Domain.Characters;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Extract accepted visuals only. This tool never runs family authoring, changes source imports,
    /// copies mesh buffers, edits reference prefabs, or chooses facial candidates.</summary>
    public static class CharacterRuntimeSetupTools
    {
        public const string Output = "Assets/SilverScreen/Art/Characters/CharacterRuntimeGenerated";
        public const string CatalogPath = Output + "/Resources/CharacterRuntimeV1/LocalCharacterCatalog.asset";
        public const string FingerprintsPath = "Assets/Editor/Characters/CharacterRuntimeReferenceFingerprints.json";
        [Serializable] public sealed class Fingerprint { public string path; public string sha256; }
        [Serializable] public sealed class ReferenceRecipe
        { public CharacterFamily family; public string referencePrefab; public string recipe; public Fingerprint[] files; }
        [Serializable] private sealed class Manifest { public ReferenceRecipe[] references; }
        public static ReferenceRecipe[] ReadRecipes() => JsonUtility.FromJson<Manifest>(File.ReadAllText(FingerprintsPath)).references;
        public static CharacterFamilyDefinition Definition(CharacterFamily family) => AssetDatabase.LoadAssetAtPath<CharacterFamilyDefinition>(
            "Assets/SilverScreen/Characters/Definitions/" + family + ".asset");
        public static string WrapperPath(CharacterFamily family) => Output + "/" + family + "Visual.prefab";

        [MenuItem("SilverScreen/Characters/Runtime V1/Validate accepted prerequisites")]
        public static void ValidateMenu()
        { if (!ValidatePrerequisites(out var reason)) throw new InvalidOperationException(reason); Debug.Log("All four accepted runtime reference prerequisites match their recorded fingerprints."); }

        public static bool ValidatePrerequisites(out string reason)
        {
            reason = null;
            if (!File.Exists(FingerprintsPath)) { reason = "Missing committed reference fingerprint manifest: " + FingerprintsPath; return false; }
            var recipes = ReadRecipes(); var families = new HashSet<CharacterFamily>(); var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            if (recipes == null || recipes.Length != 4) { reason = "Four explicit reference recipes required."; return false; }
            foreach (var recipe in recipes)
            {
                var definition = Definition(recipe.family);
                if (!families.Add(recipe.family) || definition == null || !definition.TryValidate(out reason))
                { reason = "Invalid/missing committed definition for " + recipe.family + ": " + reason; return false; }
                foreach (var file in recipe.files)
                {
                    if (!File.Exists(file.path)) { reason = "Missing " + recipe.family + " input: " + file.path + ". Follow accepted recipe " + recipe.recipe + "; setup does not regenerate facial art."; return false; }
                    if (!hashes.TryGetValue(file.path, out var hash))
                    { using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file.path))).Replace("-", "").ToLowerInvariant(); hashes.Add(file.path, hash); }
                    if (!string.Equals(hash, file.sha256, StringComparison.Ordinal))
                    { reason = "Accepted " + recipe.family + " reference fingerprint mismatch: " + file.path + ". Review " + recipe.recipe + "; no inputs were overwritten."; return false; }
                }
            }
            return true;
        }

        [MenuItem("SilverScreen/Characters/Runtime V1/Build local visual wrappers")]
        public static void BuildLocalWrappers()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run local setup outside Play Mode.");
            if (!ValidatePrerequisites(out var reason)) throw new InvalidOperationException(reason);
            var prepared = new List<CharacterVisualReference>(); var definitions = new List<CharacterFamilyDefinition>();
            try
            {
                // Prepare/validate every family before writing any output. Purchased and reference assets are read only.
                foreach (var recipe in ReadRecipes())
                {
                    var reference = PrefabUtility.LoadPrefabContents(recipe.referencePrefab);
                    try
                    {
                        var source = reference.GetComponent<HumanBasePrototypeVisual>();
                        if (source == null || source.Animator == null || source.Face == null) throw new InvalidOperationException("Reference contract missing: " + recipe.referencePrefab);
                        var serialized = new SerializedObject(source);
                        var sourceRoot = (Transform)serialized.FindProperty("_visualRoot").objectReferenceValue;
                        if (sourceRoot == null || sourceRoot == reference.transform) throw new InvalidOperationException("Reference requires a separate visual child.");
                        var clone = Object.Instantiate(sourceRoot.gameObject); clone.SetActive(false);
                        var visual = clone.AddComponent<CharacterVisualReference>(); prepared.Add(visual);
                        clone.name = recipe.family + "Visual"; clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        var animator = clone.transform.Find(AnimationUtility.CalculateTransformPath(source.Animator.transform, sourceRoot)).GetComponent<Animator>();
                        var face = clone.transform.Find(AnimationUtility.CalculateTransformPath(source.Face.transform, sourceRoot)).GetComponent<SkinnedMeshRenderer>();
                        var definition = Definition(recipe.family); definitions.Add(definition);
                        visual.Configure(animator, face, animator.GetBoneTransform(HumanBodyBones.Head), definition);
                        if (!visual.TryValidate(definition, out reason)) throw new InvalidOperationException(recipe.family + ": " + reason);
                        if (face.sharedMesh != source.Face.sharedMesh || animator.avatar != source.Animator.avatar || animator.runtimeAnimatorController != source.Animator.runtimeAnimatorController)
                            throw new InvalidOperationException("Extraction changed native mesh, Avatar or controller references.");
                        for (int i = 0; i < face.sharedMesh.blendShapeCount; i++)
                            if (face.GetBlendShapeWeight(i) != 0) throw new InvalidOperationException("Accepted reference must start neutral: " + recipe.family);
                        var controller = animator.runtimeAnimatorController as AnimatorController;
                        if (controller == null || !controller.parameters.Any(p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) ||
                            !new[] { "Idle", "Walk", "Run" }.All(n => controller.layers[0].stateMachine.states.Any(s => s.state.name == n)))
                            throw new InvalidOperationException("Missing Speed/Idle/Walk/Run animation contract.");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(reference); }
                }
                Directory.CreateDirectory(Output + "/Resources/CharacterRuntimeV1"); AssetDatabase.Refresh();
                var entries = new List<CharacterFamilyCatalogEntry>();
                for (int i = 0; i < prepared.Count; i++)
                {
                    var prefab = PrefabUtility.SaveAsPrefabAsset(prepared[i].gameObject, WrapperPath(definitions[i].Family));
                    entries.Add(new CharacterFamilyCatalogEntry(definitions[i], prefab.GetComponent<CharacterVisualReference>()));
                }
                var catalogue = AssetDatabase.LoadAssetAtPath<CharacterFamilyCatalog>(CatalogPath);
                if (catalogue == null) { catalogue = ScriptableObject.CreateInstance<CharacterFamilyCatalog>(); AssetDatabase.CreateAsset(catalogue, CatalogPath); }
                catalogue.Configure(entries.ToArray()); EditorUtility.SetDirty(catalogue); AssetDatabase.SaveAssetIfDirty(catalogue);
                Debug.Log("Built four visual-only wrappers and local catalogue at " + Output + ". Normal Studio spawning remains unchanged.");
            }
            finally { foreach (var visual in prepared) if (visual != null) Object.DestroyImmediate(visual.gameObject); }
        }
    }
}
