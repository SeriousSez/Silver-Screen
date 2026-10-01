using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Bootstrap;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Writing;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor
{
    /// <summary>One-time conversion to ordinary, scene-owned New Studio authoring.</summary>
    public static class NewStudioSceneAuthoring
    {
        [MenuItem("SilverScreen/New Studio/Author starting world in Studio scene %#j")]
        public static void ConvertScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring the starting scene.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Studio.unity") throw new InvalidOperationException("Open Assets/Scenes/Studio.unity first.");
            var bootstrap = Object.FindAnyObjectByType<StudioBootstrap>();
            var surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (bootstrap == null || surface == null) throw new InvalidOperationException("Studio bootstrap or navigation is missing.");
            var backup = Path.Combine(Path.GetTempPath(), "SilverScreen-Studio-before-authored-world-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not preserve the current scene before conversion.");
            var lot = surface.GetComponent<StudioBuildLot>() ?? Undo.AddComponent<StudioBuildLot>(surface.gameObject);
            var templates = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Development Templates (inactive)");
            if (templates == null)
            {
                templates = new GameObject("Development Templates (inactive)");
                Undo.RegisterCreatedObjectUndo(templates, "Preserve development templates");
                templates.SetActive(false);
            }
            void Preserve(GameObject go)
            {
                if (go == templates || go.transform.IsChildOf(templates.transform)) return;
                Undo.SetTransformParent(go.transform, templates.transform, "Preserve development fixture");
            }
            foreach (var view in Object.FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Include))
                if (view.GetComponent<StudioServicesFacility>() == null) Preserve(view.gameObject);
            foreach (var person in Object.FindObjectsByType<EmployeeAgent>(FindObjectsInactive.Include)) Preserve(person.gameObject);
            foreach (var school in Object.FindObjectsByType<StageSchoolView>(FindObjectsInactive.Include)) Preserve(school.gameObject);
            foreach (var office in Object.FindObjectsByType<ScriptOfficeStationLayout>(FindObjectsInactive.Include)) Preserve(office.gameObject);
            var art = new SerializedObject(bootstrap).FindProperty("_populatedScenarioArt");
            for (int i = 0; i < art.arraySize; i++)
                if (art.GetArrayElementAtIndex(i).objectReferenceValue is Transform t) Preserve(t.gameObject);

            var data = new SerializedObject(lot);
            var facility = lot.StartingFacility;
            if (facility == null)
            {
                var prefab = Resources.Load<StudioServicesFacility>("StudioServices_Live");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
                Undo.RegisterCreatedObjectUndo(go, "Place starting Studio Services");
                go.name = "Studio Services";
                go.transform.SetPositionAndRotation(lot.ServicePosition + Vector3.up * .02f, lot.ServiceRotation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                facility = go.GetComponent<StudioServicesFacility>();
                data.FindProperty("_startingFacility").objectReferenceValue = facility;
            }
            if (data.FindProperty("_applicantSpawn").objectReferenceValue == null)
            {
                var spawn = new GameObject("Applicant Arrival (outside gate)");
                Undo.RegisterCreatedObjectUndo(spawn, "Author applicant arrival");
                spawn.transform.position = lot.ApplicantArrival;
                data.FindProperty("_applicantSpawn").objectReferenceValue = spawn.transform;
            }
            data.ApplyModifiedProperties();
            var outside = bootstrap.GetComponent<OutsideWorldPrototype>() ?? Undo.AddComponent<OutsideWorldPrototype>(bootstrap.gameObject);
            outside.EnsureAuthoredWorld();
            PersistWorldMaterials(outside.transform.Find("OutsideWorld"));
            EditorUtility.SetDirty(lot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save authored Studio scene.");
            Selection.activeGameObject = facility.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(17, 0, -5), new Vector3(65, 18, 60)), false);
            Debug.Log("New Studio authored scene saved: scene-owned Services, applicant arrival, outside world; development templates preserved inactive. Backup: " + backup);
        }

        private static void PersistWorldMaterials(Transform world)
        {
            const string path = "Assets/Scenes/StudioWorldPalette.asset";
            var palette = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToList();
            var shared = new Dictionary<string, Material>();
            string Key(Material m) => m.shader.name + ":" + ColorUtility.ToHtmlStringRGBA(m.color);
            foreach (var m in palette) shared[Key(m)] = m;
            foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || EditorUtility.IsPersistent(source)) continue;
                    var key = Key(source);
                    if (!shared.TryGetValue(key, out var material))
                    {
                        material = new Material(source) { name = "World " + ColorUtility.ToHtmlStringRGBA(source.color) };
                        if (AssetDatabase.LoadMainAssetAtPath(path) == null) AssetDatabase.CreateAsset(material, path);
                        else AssetDatabase.AddObjectToAsset(material, path);
                        shared.Add(key, material);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            foreach (var material in shared.Values) AssetDatabase.SaveAssetIfDirty(material);
        }
    }
}
