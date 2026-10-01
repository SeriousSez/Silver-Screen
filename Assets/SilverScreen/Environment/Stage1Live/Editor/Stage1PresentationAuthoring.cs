using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor
{
    /// <summary>Reproducible presentation configuration over the approved production meshes.
    /// Does not regenerate architecture, replace door pivots, or change collision/navigation.</summary>
    public static class Stage1PresentationAuthoring
    {
        private const string MeshRoot = Stage1LiveIntegrationBuilder.Root + "/PresentationMeshes";
        private const string Sections = "AuthoredRevealSections";
        private static readonly string[] Shells = { "FrontShell", "RearShell", "LeftShell", "RightShell" };
        private static readonly float[] TrussStations = { -11.2f, -5.6f, 0, 5.6f, 11.2f };

        [MenuItem("SilverScreen/Stage 1 Live/3 Integrate presentation")]
        public static void ApplyToPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(Stage1LiveIntegrationBuilder.PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, Stage1LiveIntegrationBuilder.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("SilverScreen/Stage 1 Live/4 Repair roof reflection influence")]
        public static void RepairRoofReflectionInfluence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(Stage1LiveIntegrationBuilder.PrefabPath);
            try { ConfigureReflections(root); PrefabUtility.SaveAsPrefabAsset(root, Stage1LiveIntegrationBuilder.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Configure(GameObject root)
        {
            Directory.CreateDirectory(MeshRoot); AssetDatabase.Refresh();
            var groups = new Dictionary<string, BuildingCutawayController.VisibilityGroup>(StringComparer.Ordinal);
            var renderers = new Dictionary<string, List<Renderer>>();
            var lights = new Dictionary<string, List<Light>>();
            var construction = new Dictionary<ConstructionVisualGroup, List<Renderer>>();
            void Group(string id, string support = null, Vector3 normal = default, bool roofDirection = false)
            {
                groups.Add(id, new BuildingCutawayController.VisibilityGroup { Id = id, SupportGroupId = support,
                    OutwardNormal = normal, UseVerticalDirection = roofDirection });
                renderers.Add(id, new List<Renderer>()); lights.Add(id, new List<Light>());
            }
            Group("FrontShell", normal: Vector3.back); Group("RearShell", normal: Vector3.forward);
            Group("LeftShell", normal: Vector3.left); Group("RightShell", normal: Vector3.right);
            // Five existing barrel bands: eave, glazed ribbon, crown, glazed ribbon, eave.
            // Keep the far low barrel where it provides context; the crown opens from above.
            Vector3[] roofNormals = { new Vector3(-.92f,.39f,0), new Vector3(-.65f,.76f,0), Vector3.up,
                new Vector3(.65f,.76f,0), new Vector3(.92f,.39f,0) };
            for (int i = 0; i < 5; i++)
            {
                Group("Barrel" + i, normal: roofNormals[i], roofDirection: true);
                Group("RoofSupport" + i, "Barrel" + i);
                Group("RoofEquipment" + i, "RoofSupport" + i);
            }
            foreach (var shell in Shells)
            {
                Group(shell + "Trim", shell);
                Group(shell + "Utilities", shell);
                Group(shell + "Lamps", shell + "Utilities");
                Group(shell + "Openings", shell + "Trim");
            }
            Group("StageDoorLeft", "FrontShell"); Group("StageDoorRight", "FrontShell");
            Group("StageDoorFrame", "FrontShell"); Group("StageLettering", "FrontShellTrim");
            // A zero-normal group explicitly retains independently supported interior/floor objects.
            Group("RetainedInterior");
            for (int i = 0; i < TrussStations.Length; i++)
            {
                Group("TrussBand" + i);
                var truss = groups["TrussBand" + i];
                truss.Mode = BuildingCutawayMode.StructuralOccluder;
                truss.CutawayOpacity = .22f;
                truss.StructuralFocus = new Bounds(new Vector3(0, 1, -1.2f), new Vector3(11.6f, 0, 18.4f));
                truss.StructuralEnterCoverage = .04f; truss.StructuralExitCoverage = .015f;
                Group("TrussConnections" + i, "TrussBand" + i);
                Group("TrussHangers" + i, "TrussConnections" + i);
            }

            foreach (var source in root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.transform.parent == null || r.transform.parent.name != Sections).ToArray())
            {
                var old = source.transform.Find(Sections);
                if (old != null) { Object.DestroyImmediate(old.gameObject); source.enabled = true; }
                if (!source.enabled) continue; // obsolete personnel-door export stays disabled
                var filter = source.GetComponent<MeshFilter>();
                string fixedGroup = FixedGroup(source.name);
                if (fixedGroup != null)
                {
                    renderers[fixedGroup].Add(source); continue;
                }
                if (filter == null || filter.sharedMesh == null) continue;
                if (!NeedsSections(source.name)) { renderers["RetainedInterior"].Add(source); continue; }
                var matrix = root.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                var mesh = filter.sharedMesh; var vertices = mesh.vertices;
                bool wholeAttachments = source.name == "Export_PermanentServices" || source.name == "Export_PermanentLighting" ||
                    source.name == "LivePersonnelRemainder" || source.name == "Export_EntranceCanopies" ||
                    source.name == "Export_AcousticTreatment" || source.name == "Export_BlackoutShutters" || IsStructuralSource(source.name);
                var componentBounds = wholeAttachments ? ComponentBounds(mesh, matrix) : null;
                var buckets = new Dictionary<string, List<int>[]>();
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetTriangles(sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        var center = componentBounds != null ? componentBounds[indices[i]].center :
                            matrix.MultiplyPoint3x4((vertices[indices[i]] + vertices[indices[i+1]] + vertices[indices[i+2]]) / 3);
                        string id = IsStructuralSource(source.name) ? StructuralSectionFor(source.name, componentBounds[indices[i]]) : SectionFor(source.name, center);
                        if (!buckets.TryGetValue(id, out var lists))
                            buckets.Add(id, lists = Enumerable.Range(0, mesh.subMeshCount).Select(_ => new List<int>()).ToArray());
                        lists[sub].Add(indices[i]); lists[sub].Add(indices[i+1]); lists[sub].Add(indices[i+2]);
                    }
                }
                var sectionRoot = new GameObject(Sections).transform; sectionRoot.SetParent(source.transform, false);
                foreach (var bucket in buckets)
                {
                    var part = Compact(mesh, bucket.Value, out int[] submeshes);
                    string path = MeshRoot + "/" + source.name + "_" + bucket.Key + ".asset";
                    part.name = Path.GetFileNameWithoutExtension(path);
                    var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (asset == null) { AssetDatabase.CreateAsset(part, path); asset = part; }
                    else { EditorUtility.CopySerialized(part, asset); Object.DestroyImmediate(part); AssetDatabase.SaveAssetIfDirty(asset); }
                    var go = new GameObject(source.name + "_" + bucket.Key); go.transform.SetParent(sectionRoot, false);
                    go.AddComponent<MeshFilter>().sharedMesh = asset;
                    var renderer = go.AddComponent<MeshRenderer>(); EditorUtility.CopySerialized(source, renderer);
                    renderer.sharedMaterials = submeshes.Select(s => source.sharedMaterials[s]).ToArray();
                    renderer.enabled = true;
                    renderers[bucket.Key].Add(renderer);
                    var phase = ConstructionGroup(source.name);
                    if (!construction.TryGetValue(phase, out var bindings)) construction.Add(phase, bindings = new List<Renderer>());
                    bindings.Add(renderer);
                    if (source.name.StartsWith("Export_Roof", StringComparison.Ordinal))
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                source.enabled = false; // retain original mesh/colliders as authoritative physical geometry
            }
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                // These authored lamps are shell mounted; pendants have independent steel carriers.
                if (light.name.StartsWith("WallWorkLamp", StringComparison.Ordinal))
                    lights[Wall(root.transform.InverseTransformPoint(light.transform.position)) + "Lamps"].Add(light);
            }
            foreach (var group in groups.Values)
            {
                group.Renderers = renderers[group.Id].ToArray(); group.Lights = lights[group.Id].ToArray();
                if (group.Mode == BuildingCutawayMode.StructuralOccluder)
                {
                    if (group.Renderers.Length == 0) throw new InvalidOperationException("Missing authored truss: " + group.Id);
                    group.LocalSection = RendererBounds(root.transform, group.Renderers);
                    // Small sightline padding makes the open lattice a coherent band; geometry is unchanged.
                    group.LocalSection.Expand(new Vector3(0, .10f, .16f));
                }
                group.Occluders = root.GetComponentsInChildren<Collider>(true).Where(c =>
                {
                    if (!c.enabled) return false;
                    if (c.name == "SelectionRoof") return group.Id == "Barrel2";
                    var p = root.transform.InverseTransformPoint(c.bounds.center);
                    return p.y > .2f && (Mathf.Abs(p.x) > 7 || Mathf.Abs(p.z) > 11.5f) && group.Id == Wall(p);
                }).ToArray();
            }
            var cutaway = root.GetComponent<BuildingCutawayController>() ?? root.AddComponent<BuildingCutawayController>();
            cutaway.Configure(groups.Values.ToArray(), new Bounds(new Vector3(0,5.45f,0), new Vector3(17.2f,10.9f,26)));
            // Shell normals continue to govern the exterior. Structural bands use their own gameplay area.
            cutaway.SetInteriorFocusPoints(Array.Empty<Vector3>());
            var phases = root.GetComponent<ConstructionPhaseVisuals>() ?? root.AddComponent<ConstructionPhaseVisuals>();
            var serializedPhases = new SerializedObject(phases);
            var array = serializedPhases.FindProperty("_groups"); array.arraySize = construction.Count;
            int entry = 0;
            foreach (var pair in construction)
            {
                var binding = array.GetArrayElementAtIndex(entry++);
                binding.FindPropertyRelative("Group").enumValueIndex = (int)pair.Key;
                var items = binding.FindPropertyRelative("Renderers"); items.arraySize = pair.Value.Count;
                for (int i = 0; i < items.arraySize; i++) items.GetArrayElementAtIndex(i).objectReferenceValue = pair.Value[i];
            }
            serializedPhases.ApplyModifiedPropertiesWithoutUndo();
            ConfigureReflections(root);
            EditorUtility.SetDirty(cutaway);
        }

        private static void ConfigureReflections(GameObject root)
        {
            var roof = root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.name.StartsWith("Export_Roof", StringComparison.Ordinal)).ToArray();
            foreach (var renderer in roof) renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Forward+ selects reflection probes per pixel from the cluster atlas, bypassing
            // renderer probe anchors/Off. Keep the interior influence physically below the roof.
            // Use the authored vertex heights, not a world-aligned box that changes with placed yaw.
            var probe = root.GetComponentsInChildren<ReflectionProbe>(true).SingleOrDefault(p => p.name == "InteriorReflection");
            if (probe == null) return;
            float ceiling = probe.center.y + probe.size.y * .5f;
            foreach (var renderer in roof)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var toProbe = probe.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                foreach (var vertex in mesh.vertices)
                    ceiling = Mathf.Min(ceiling, toProbe.MultiplyPoint3x4(vertex).y - .05f);
            }
            var size = probe.size; var center = probe.center;
            float bottom = center.y - size.y * .5f;
            if (ceiling <= bottom) throw new InvalidOperationException("Stage interior reflection influence must remain above the floor.");
            size.y = ceiling - bottom; center.y = (ceiling + bottom) * .5f;
            probe.size = size; probe.center = center;
        }
        private static string FixedGroup(string name)
        {
            switch (name)
            {
                case "Export_MainDoorLeft": return "StageDoorLeft";
                case "Export_MainDoorRight": return "StageDoorRight";
                case "Export_DoorFrameHardware": return "StageDoorFrame";
                case "LiveFrontRightLeaf": return "FrontShellOpenings";
                case "InstanceDrivenStageLettering": return "StageLettering";
                default: return null;
            }
        }
        private static bool NeedsSections(string name)
        {
            switch (name)
            {
                case "Export_ExteriorWalls": case "Export_MasonryFraming": case "Export_Roof":
                case "Export_RoofSeams": case "Export_RoofGlazing": case "Export_RoofVents":
                case "Export_InteriorRoofLining": case "Export_BlackoutShutters": case "Export_Rainwater":
                case "Export_Clerestory": case "Export_WindowGlass": case "Export_RearVentilation":
                case "Export_AcousticTreatment": case "Export_PermanentServices": case "Export_PermanentLighting":
                case "Export_EntranceCanopies": case "LivePersonnelRemainder": return true;
                case "Export_PrimaryStructure": case "Export_StructuralConnections": case "Export_RiggingPrimary": return true;
                default: return false;
            }
        }
        private static bool IsStructuralSource(string name) => name == "Export_PrimaryStructure" ||
            name == "Export_StructuralConnections" || name == "Export_RiggingPrimary";

        private static string StructuralSectionFor(string name, Bounds bounds)
        {
            int station = 0;
            for (int i = 1; i < TrussStations.Length; i++)
                if (Mathf.Abs(bounds.center.z - TrussStations[i]) < Mathf.Abs(bounds.center.z - TrussStations[station])) station = i;
            if (Mathf.Abs(bounds.center.z - TrussStations[station]) > .35f || bounds.size.z > .6f)
                return "RetainedInterior"; // longitudinal purlins, bracing and independently spanning rigging
            if (name == "Export_RiggingPrimary")
                return bounds.min.y >= 6.5f && bounds.size.x <= .35f && bounds.size.z <= .35f
                    ? "TrussHangers" + station : "RetainedInterior";
            if (bounds.min.y < 6.95f) return "RetainedInterior"; // whole stanchions and foundation plates
            return (name == "Export_PrimaryStructure" ? "TrussBand" : "TrussConnections") + station;
        }

        private static Bounds RendererBounds(Transform root, Renderer[] renderers)
        {
            bool first = true; var result = default(Bounds);
            foreach (var renderer in renderers)
            {
                var bounds = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                var matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                {
                    var point = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x,y,z)));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; } else result.Encapsulate(point);
                }
            }
            return result;
        }
        private static string Wall(Vector3 p) => Mathf.Abs(p.z) / 12f > Mathf.Abs(p.x) / 7.9f
            ? (p.z < 0 ? "FrontShell" : "RearShell") : (p.x < 0 ? "LeftShell" : "RightShell");
        private static int RoofBand(Vector3 p)
        {
            // Project lining, glass, seams and sheet onto the same circular source profile.
            // This keeps the cut along the existing glazing boundaries at every shell thickness.
            const float radius = 10.196333f, circleHeight = .653667f;
            float x = p.x * radius / Mathf.Max(.01f, new Vector2(p.x, p.y - circleHeight).magnitude);
            return x < -5.78f ? 0 : x < -3.17f ? 1 : x < 3.17f ? 2 : x < 5.78f ? 3 : 4;
        }
        private static string SectionFor(string name, Vector3 p)
        {
            string wall = Wall(p); int band = RoofBand(p);
            switch (name)
            {
                case "Export_Roof": case "Export_RoofGlazing": return "Barrel" + band;
                case "Export_InteriorRoofLining": case "Export_RoofSeams": return "RoofSupport" + band;
                case "Export_RoofVents": return "RoofEquipment2";
                case "Export_BlackoutShutters": return p.y > 7 ? "RoofEquipment" + band : wall + "Openings";
                case "Export_ExteriorWalls": return wall;
                case "Export_MasonryFraming": return p.y < .16f ? "RetainedInterior" : wall + "Trim";
                case "Export_Rainwater": return p.y < .16f ? "RetainedInterior" : (p.x < 0 ? "LeftShellTrim" : "RightShellTrim");
                case "Export_PermanentLighting": return Mathf.Abs(p.x) < 6 && Mathf.Abs(p.z) < 11.8f ? "RetainedInterior" : wall + "Lamps";
                case "Export_PermanentServices": return (Mathf.Abs(p.z) < 11.65f ? (p.x < 0 ? "LeftShell" : "RightShell") : wall) + "Utilities";
                case "Export_AcousticTreatment": return wall + "Trim";
                default: return wall + "Openings";
            }
        }
        private static ConstructionVisualGroup ConstructionGroup(string name)
        {
            switch (name)
            {
                case "Export_MasonryFraming": case "Export_PrimaryStructure": case "Export_RiggingPrimary": return ConstructionVisualGroup.Structure;
                case "Export_ExteriorWalls": return ConstructionVisualGroup.Walls;
                case "Export_Roof": case "Export_RoofSeams": case "Export_RoofGlazing": case "Export_InteriorRoofLining": return ConstructionVisualGroup.Roof;
                case "Export_BlackoutShutters": case "Export_Clerestory": return ConstructionVisualGroup.Openings;
                case "Export_PermanentLighting": case "Export_PermanentServices": case "Export_AcousticTreatment": return ConstructionVisualGroup.Fixtures;
                default: return ConstructionVisualGroup.Details;
            }
        }
        private static Bounds[] ComponentBounds(Mesh mesh, Matrix4x4 matrix)
        {
            // Weld only for connectivity analysis. Exported vertex attributes are never welded/changed.
            // A complete bracket, lamp or conduit follows one support even across material/normal seams.
            var vertices = mesh.vertices; var parents = Enumerable.Range(0, vertices.Length).ToArray();
            int Find(int i) { while (parents[i] != i) { parents[i] = parents[parents[i]]; i = parents[i]; } return i; }
            void Join(int a, int b) { parents[Find(a)] = Find(b); }
            var positions = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i] * 100000f;
                var key = new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
                if (positions.TryGetValue(key, out int old)) Join(i, old); else positions.Add(key, i);
            }
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3) { Join(triangles[i], triangles[i+1]); Join(triangles[i], triangles[i+2]); }
            var bounds = new Dictionary<int, Bounds>();
            for (int i = 0; i < vertices.Length; i++)
            {
                int id = Find(i); var point = matrix.MultiplyPoint3x4(vertices[i]);
                if (!bounds.TryGetValue(id, out var b)) b = new Bounds(point, Vector3.zero);
                b.Encapsulate(point); bounds[id] = b;
            }
            return Enumerable.Range(0, vertices.Length).Select(i => bounds[Find(i)]).ToArray();
        }
        private static Mesh Compact(Mesh source, List<int>[] triangles, out int[] submeshes)
        {
            submeshes = Enumerable.Range(0, triangles.Length).Where(i => triangles[i].Count > 0).ToArray();
            var used = submeshes.SelectMany(i => triangles[i]).Distinct().ToArray();
            var map = used.Select((old, index) => (old, index)).ToDictionary(p => p.old, p => p.index);
            var mesh = new Mesh { name = source.name + " reveal section", indexFormat = used.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            var vertices = source.vertices; var normals = source.normals; var tangents = source.tangents; var colors = source.colors32;
            mesh.vertices = used.Select(i => vertices[i]).ToArray();
            if (normals.Length == vertices.Length) mesh.normals = used.Select(i => normals[i]).ToArray();
            if (tangents.Length == vertices.Length) mesh.tangents = used.Select(i => tangents[i]).ToArray();
            if (colors.Length == vertices.Length) mesh.colors32 = used.Select(i => colors[i]).ToArray();
            for (int channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); source.GetUVs(channel, uv);
                if (uv.Count == vertices.Length) mesh.SetUVs(channel, used.Select(i => uv[i]).ToList());
            }
            mesh.subMeshCount = submeshes.Length;
            for (int s = 0; s < submeshes.Length; s++) mesh.SetTriangles(triangles[submeshes[s]].Select(i => map[i]).ToArray(), s, false);
            mesh.RecalculateBounds(); return mesh;
        }

    }
}
