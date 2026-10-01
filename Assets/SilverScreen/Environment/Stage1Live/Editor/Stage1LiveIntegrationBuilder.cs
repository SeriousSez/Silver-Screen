using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;

namespace SilverScreen.Editor
{
    /// <summary>Owns only the live variant, its derived imports and navigation assets.
    /// Review source and the old fallback are never regenerated or restored here.</summary>
    public static class Stage1LiveIntegrationBuilder
    {
        public const string Root = "Assets/SilverScreen/Environment/Stage1Live";
        public const string PrefabPath = Root + "/Stage1_Live.prefab";
        public const string ModelPath = Root + "/Models/Stage1_PersonnelLiveParts.fbx";
        public const string Master = "Assets/SilverScreen/Environment/Stage1CleanCandidate/Stage1_CleanCandidate_A.prefab";
        public const string BodyPath = "Assets/SilverScreen/Art/Characters/EmployeeBody_060x200.asset";
        [Serializable] private class CollisionRecord { public string name; public string group; public float[] center; public float[] size; }
        [Serializable] private class Manifest { public CollisionRecord[] colliders; }

        [MenuItem("SilverScreen/Stage 1 Live/1 Build live variant")]
        public static void BuildVariant()
        {
            Directory.CreateDirectory(Root + "/Navigation");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.globalScale = 1; importer.useFileScale = true;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var material in AssetDatabase.FindAssets("t:Material", new[] { "Assets/SilverScreen/Environment/Stage1CleanCandidate/Materials" }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(material));
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat);
            }
            importer.SaveAndReimport();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Master), preview);
                root.name = "Stage1_Live";
                var identity = new SerializedObject(root.GetComponent<SoundStageIdentity>());
                identity.FindProperty("_facilityId").stringValue = "stage-1";
                identity.FindProperty("_stageNumber").intValue = 1;
                identity.ApplyModifiedPropertiesWithoutUndo();
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var originalDoors = root.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "Export_PersonnelDoors");
                originalDoors.enabled = false;

                var interior = Child(root.transform, "InteriorNavigation", Vector3.zero);
                var physical = Child(interior, "PhysicalObstructions", Vector3.zero);
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("ArtSource/Stage1LiveIntegration/integration_manifest.json"));
                foreach (var item in manifest.colliders)
                {
                    var size = V(item.size);
                    if (size.x <= 0 || size.y <= 0 || size.z <= 0) continue;
                    var go = Child(physical, item.name, V(item.center)).gameObject;
                    var box = go.AddComponent<BoxCollider>(); box.size = size;
                }
                // Closed scenery doors and the rear stair service band remain obstructions.
                foreach (var name in new[] { "Export_MainDoorLeft", "Export_MainDoorRight" })
                {
                    var r = root.GetComponentsInChildren<MeshRenderer>(true).Single(m => m.name == name);
                    var box = Child(physical, "Closed_" + name, root.transform.InverseTransformPoint(r.bounds.center)).gameObject.AddComponent<BoxCollider>();
                    box.size = r.bounds.size;
                }
                Box(physical, "RearStairServiceBand", new Vector3(0, 2.4f, 9.6f), new Vector3(14.8f, 4.8f, 3f));

                var doors = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), preview);
                PrefabUtility.UnpackPrefabInstance(doors, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                doors.transform.SetParent(interior, false);
                doors.transform.localRotation = Quaternion.Euler(-90, 180, 0);
                foreach (var f in doors.GetComponentsInChildren<MeshFilter>()) f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                var leaf = doors.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "LiveFrontRightLeaf");
                var hinge = Child(interior, "FrontRightPersonnelHinge", new Vector3(5.138f, 0, -12.27f));
                leaf.transform.SetParent(hinge, true);
                hinge.localRotation = Quaternion.Euler(0, 95, 0);
                EnsureNavigationClearance(root);

                var selection = Child(root.transform, "SelectionOnly", Vector3.zero);
                selection.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                // Envelope surfaces provide selection without filling the filming floor.
                Box(selection, "SelectionRoof", new Vector3(0, 10.7f, 0), new Vector3(15.8f, .15f, 24), true);
                Box(selection, "SelectionFront", new Vector3(0, 5, -12), new Vector3(15.8f, 10, .02f), true);
                Box(selection, "SelectionRear", new Vector3(0, 5, 12), new Vector3(15.8f, 10, .02f), true);
                Box(selection, "SelectionLeft", new Vector3(-7.9f, 4, 0), new Vector3(.02f, 8, 24), true);
                Box(selection, "SelectionRight", new Vector3(7.9f, 4, 0), new Vector3(.02f, 8, 24), true);

                var access = Child(root.transform, "AccessAnchors", Vector3.zero);
                Child(access, "ExteriorApproach", new Vector3(5.7f, 0, -13.8f));
                Child(access, "PersonnelThreshold", new Vector3(5.82f, .08f, -12.09f));
                var arrival = Child(access, "InteriorArrival", new Vector3(5.7f, .08f, -10.7f));
                Child(access, "FrontServiceAccess", new Vector3(4.8f, .08f, -8));
                root.AddComponent<StudioBuildingView>().Initialize(BuildingType.SoundStage, "Stage 1", arrival);
                root.AddComponent<AuthoredProductionLayout>();
                AuthorLayouts(root);
                var camera = Child(root.transform, "ProductionCameraPose", new Vector3(0, 3, -8.5f));
                var focus = Child(root.transform, "ProductionCameraFocus", new Vector3(0, 1.25f, 2.2f));
                camera.LookAt(focus);
                root.AddComponent<ProductionCameraLayout>().Configure(camera, focus);

                ConfigureContinuousEntrance(root);
                Stage1PresentationAuthoring.Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        [MenuItem("SilverScreen/Stage 1 Live/2 Promote in current Studio and bake")]
        public static void PromoteAndBake()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before promoting Stage 1.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Studio.unity") throw new InvalidOperationException("The current scene must be Studio.");
            var stages = UnityEngine.Object.FindObjectsByType<SoundStageIdentity>();
            if (stages.Length != 1) throw new InvalidOperationException("Expected exactly the existing revision 04 candidate; refusing ambiguous replacement.");
            var instance = stages[0].gameObject;
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instance, AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),
                new PrefabReplacingSettings { objectMatchMode = ObjectMatchMode.ByHierarchy,
                    prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides, changeRootNameToAssetName = true }, InteractionMode.AutomatedAction);
            var body = AssetDatabase.LoadAssetAtPath<Mesh>(BodyPath);
            if (body == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BodyPath));
                body = UnityEngine.Object.Instantiate(EmployeeNavigationProfile.BodyMesh);
                body.name = "EmployeeBody_060x200"; AssetDatabase.CreateAsset(body, BodyPath);
            }
            foreach (var employee in UnityEngine.Object.FindObjectsByType<EmployeeAgent>())
            {
                EmployeeNavigationProfile.Apply(employee.gameObject);
                employee.GetComponent<MeshFilter>().sharedMesh = body;
            }
            var lot = scene.GetRootGameObjects().Single(g => g.name == "StudioLot").GetComponent<NavMeshSurface>();
            ConfigureSurface(lot, CollectObjects.All);
            SaveBake(lot, Root + "/Navigation/Studio_Revision04.asset");
            EditorSceneManager.MarkSceneDirty(scene);
            // Caller reviews a Save-As-Copy diff before saving Studio; no global SaveAssets.
        }

        private static void SaveBake(NavMeshSurface surface, string path)
        {
            surface.RemoveData(); surface.navMeshData = null; surface.BuildNavMesh();
            var generated = surface.navMeshData;
            if (generated == null) throw new InvalidOperationException("Navigation build produced no data: " + path);
            generated.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) AssetDatabase.CreateAsset(generated, path);
            else { EditorUtility.CopySerialized(generated, existing); surface.RemoveData(); surface.navMeshData = existing; surface.AddData(); AssetDatabase.SaveAssetIfDirty(existing); }
            EditorUtility.SetDirty(surface);
        }
        private static void ConfigureSurface(NavMeshSurface surface, CollectObjects collect)
        {
            surface.agentTypeID = 0; surface.collectObjects = collect;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .05f;
        }
        public static void ConfigureContinuousEntrance(GameObject root)
        {
            // An 8 cm apron is ordinary walkable geometry. Baking it together
            // with the lot keeps local avoidance active through this narrow door.
            var interior = root.transform.Find("InteriorNavigation");
            var separateSurface = interior.GetComponent<NavMeshSurface>();
            if (separateSurface != null) UnityEngine.Object.DestroyImmediate(separateSurface);
            foreach (string legacy in new[] { "LotFootprintExclusion", "PersonnelThresholdTraversal" })
            {
                var item = root.transform.Find(legacy);
                if (item != null) UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
            var marker = root.transform.Find("PersonnelThresholdBlocker") ??
                Child(root.transform, "PersonnelThresholdBlocker", new Vector3(5.7f, 1.23f, -12.09f));
            var blocker = marker.GetComponent<NavMeshObstacle>();
            if (blocker == null) blocker = marker.gameObject.AddComponent<NavMeshObstacle>();
            blocker.shape = NavMeshObstacleShape.Box;
            blocker.center = Vector3.zero;
            blocker.size = new Vector3(1.1f, 2.3f, .48f);
            blocker.carving = true;
            blocker.carveOnlyStationary = false;
            var access = root.GetComponent<HeldOpenStageAccess>();
            if (access == null) access = root.AddComponent<HeldOpenStageAccess>();
            access.Configure(interior.Find("FrontRightPersonnelHinge"), blocker);
            var yielding = root.GetComponent<PersonnelEntranceYield>();
            if (yielding == null) yielding = root.AddComponent<PersonnelEntranceYield>();
            yielding.Configure(root.transform.Find("AccessAnchors/ExteriorApproach"),
                root.transform.Find("AccessAnchors/InteriorArrival"), .55f);
        }
        public static void EnsureNavigationClearance(GameObject root)
        {
            var interior = root.transform.Find("InteriorNavigation");
            var previous = interior.Find("NavigationClearance");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var clearance = Child(interior, "NavigationClearance", Vector3.zero);
            foreach (var box in interior.Find("PhysicalObstructions").GetComponentsInChildren<BoxCollider>())
            {
                if (!box.name.StartsWith("EndPierBase", StringComparison.Ordinal)) continue;
                var centre = root.transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
                // Preserve walkable floors and the low threshold apron. These volumes only
                // keep rasterized routes clear of standing obstructions; physical sizes stay exact.
                if (centre.y + box.size.y / 2 <= .15f || centre.y - box.size.y / 2 > 2f) continue;
                var volume = Child(clearance, box.name, centre).gameObject.AddComponent<NavMeshModifierVolume>();
                volume.center = Vector3.zero;
                // The low projecting pier needs a centre-exclusion area outside its solid
                // footprint. Include the capsule and a corner-steering margin, without
                // narrowing the separately authored personnel passage.
                float padding = 2 * (EmployeeNavigationProfile.Radius + .10f);
                volume.size = new Vector3(box.size.x + padding, 3f, box.size.z + padding);
                volume.area = 1;
            }

            EnsureHeldOpenDoorClearance(root);
        }

        public static void EnsureHeldOpenDoorClearance(GameObject root)
        {
            var interior = root.transform.Find("InteriorNavigation");
            var clearance = interior.Find("NavigationClearance") ?? Child(interior, "NavigationClearance", Vector3.zero);

            // The thin, angled held-open leaf leaves a rasterized corner that admits
            // capsule centres inside its physical clearance. Keep the full leaf and
            // hardware footprint out of the walking corridor. The bake expands this
            // exclusion by its configured agent radius, just like other obstructions.
            // This is a navigation-only envelope; door geometry and colliders stay exact.
            var leaf = interior.GetComponentsInChildren<MeshCollider>()
                .Single(c => c.name == "LiveFrontRightLeaf");
            var vertices = leaf.sharedMesh.vertices;
            var bounds = new Bounds(root.transform.InverseTransformPoint(
                leaf.transform.TransformPoint(vertices[0])), Vector3.zero);
            foreach (var vertex in vertices)
                bounds.Encapsulate(root.transform.InverseTransformPoint(leaf.transform.TransformPoint(vertex)));
            var marker = clearance.Find("HeldOpenDoorClearance") ?? Child(clearance, "HeldOpenDoorClearance", bounds.center);
            marker.localPosition = bounds.center;
            var doorClearance = marker.GetComponent<NavMeshModifierVolume>() ?? marker.gameObject.AddComponent<NavMeshModifierVolume>();
            doorClearance.center = Vector3.zero;
            doorClearance.size = new Vector3(bounds.size.x,
                EmployeeNavigationProfile.Height + bounds.size.y,
                bounds.size.z);
            doorClearance.area = 1;
        }
        private static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        private static Transform Child(Transform parent, string name, Vector3 position)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, bool trigger = false)
        { var c = Child(parent, name, position).gameObject.AddComponent<BoxCollider>(); c.size = size; c.isTrigger = trigger; }
        private static void Entry(SerializedProperty array, int index, string key, int type, Transform point)
        { var p = array.GetArrayElementAtIndex(index); p.FindPropertyRelative(key).enumValueIndex = type; p.FindPropertyRelative("Point").objectReferenceValue = point; }
        private static void AuthorLayouts(GameObject root)
        {
            var anchors = Child(root.transform, "ProductionAnchors", Vector3.zero);
            var stations = new SerializedObject(root.AddComponent<ProductionStationLayout>());
            var stationList = stations.FindProperty("_stations"); stationList.arraySize = 7;
            Vector3[] positions = { new Vector3(-4,.08f,-4), new Vector3(-2.4f,.08f,-6.2f), new Vector3(-.8f,.08f,-6.2f), new Vector3(.8f,.08f,-6.2f), new Vector3(-1.8f,.08f,-7.8f), new Vector3(3.7f,.08f,-2.7f), new Vector3(4.5f,.08f,-5) };
            for (int i = 0; i < positions.Length; i++)
            { var t = Child(anchors, ((ProductionStationType)i).ToString(), positions[i]); t.LookAt(root.transform.TransformPoint(new Vector3(0,.08f,1.5f))); Entry(stationList, i, "Type", i, t); }
            stations.ApplyModifiedPropertiesWithoutUndo();
            var marks = new SerializedObject(root.AddComponent<ActorSceneMarkLayout>()); var list = marks.FindProperty("_marks"); list.arraySize = 3;
            for (int i = 0; i < 3; i++) { var t = Child(anchors, "ActorMark" + (char)('A'+i), new Vector3(-1.8f+i*1.8f,.08f,1.5f)); t.localRotation = Quaternion.Euler(0,180,0); Entry(list,i,"Type",i,t); }
            marks.ApplyModifiedPropertiesWithoutUndo();
            var slate = new SerializedObject(root.AddComponent<SlatePositionLayout>()); list = slate.FindProperty("_positions"); list.arraySize = 2;
            var staging = Child(anchors, "SlateStaging", new Vector3(3.7f,.08f,-.8f));
            var mark = Child(anchors, "SlateMark", new Vector3(0,.08f,.2f)); mark.localRotation = Quaternion.Euler(0,180,0);
            Entry(list,0,"Type",(int)SlatePositionType.SlateStaging,staging); Entry(list,1,"Type",(int)SlatePositionType.SlateMark,mark); slate.ApplyModifiedPropertiesWithoutUndo();
            var blocking = new SerializedObject(root.AddComponent<SetBlockingPointLayout>()); list = blocking.FindProperty("_points"); list.arraySize = 3;
            string[] ids = { SceneBlockingPointIds.Center, SceneBlockingPointIds.StageLeft, SceneBlockingPointIds.StageRight };
            float[] x = { 0,3,-3 };
            for (int i = 0; i < 3; i++) { var p = list.GetArrayElementAtIndex(i); p.FindPropertyRelative("Id").stringValue = ids[i]; p.FindPropertyRelative("Point").objectReferenceValue = Child(anchors,"Blocking_"+ids[i],new Vector3(x[i],.08f,3.5f)); }
            blocking.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
