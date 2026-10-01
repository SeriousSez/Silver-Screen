using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Render-only derivatives. Never rewrites a canonical LOD0 mesh, material or kit prefab.</summary>
    public static class StudioServicesLodBuilder
    {
        public const string KitRoot = StudioServicesProductionBuilder.Kit + "/LOD";
        public const string Root = StudioServicesProductionBuilder.Root + "/LOD";
        public const string GeneratedRoot = "GeneratedLODRepresentations";
        public const string KitPacket = "ArtExports/PeriodEnvironment1930/LOD/lod_meshes.json.gz";
        public const string BuildingPacket = "ArtExports/StudioServices/LOD/lod_meshes.json.gz";
        public static bool HasExports => File.Exists(KitPacket) && File.Exists(BuildingPacket);
        [Serializable] class Provenance { public string source, sourceSha256, generatorSha256; }
        sealed class Part { public Mesh Mesh; public Material[] Materials; }
        sealed class Placement { public Part Part; public Matrix4x4 Matrix; }

        static void Folder(string path)
        {
            string at = "Assets";
            foreach (var n in path.Split('/').Skip(1))
            { if (!AssetDatabase.IsValidFolder(at + "/" + n)) AssetDatabase.CreateFolder(at, n); at += "/" + n; }
        }
        static void Save(Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        static Mesh Store(string path, Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); Save(existing); return existing;
        }
        static void Verify(string manifest)
        {
            var data = JsonUtility.FromJson<Provenance>(File.ReadAllText(manifest));
            string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
            if (Hash(data.source) != data.sourceSha256 || Hash("ArtSource/PeriodEnvironment1930/build_lods.py") != data.generatorSha256)
                throw new InvalidOperationException("Stale LOD export: regenerate build_lods.py before importing " + manifest);
        }
        static Dictionary<string, Part> Import(string packet, string folder)
        {
            Folder(folder);
            var result = new Dictionary<string, Part>();
            foreach (var p in StudioServicesProductionBuilder.Read(packet).parts)
            {
                var mesh = new Mesh { name = p.name, indexFormat = p.positions.Length / 3 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = Enumerable.Range(0, p.positions.Length / 3).Select(i => new Vector3(p.positions[i*3], p.positions[i*3+1], p.positions[i*3+2])).ToArray();
                mesh.normals = Enumerable.Range(0, p.normals.Length / 3).Select(i => new Vector3(p.normals[i*3], p.normals[i*3+1], p.normals[i*3+2])).ToArray();
                mesh.uv = Enumerable.Range(0, p.uv.Length / 2).Select(i => new Vector2(p.uv[i*2], p.uv[i*2+1])).ToArray();
                mesh.subMeshCount = p.submeshes.Length;
                for (int i = 0; i < p.submeshes.Length; i++) mesh.SetTriangles(p.submeshes[i].indices, i, false);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var materials = p.materials.Select(n => AssetDatabase.LoadAssetAtPath<Material>(StudioServicesProductionBuilder.Kit + "/Materials/" + n + ".mat")).ToArray();
                if (materials.Any(m => m == null)) throw new InvalidOperationException("Missing shared LOD material: " + p.name);
                result.Add(p.name, new Part { Mesh = Store(folder + "/" + p.name + ".asset", mesh), Materials = materials });
            }
            return result;
        }
        static MeshRenderer Renderer(Transform parent, string name, Part part)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterials = part.Materials;
            renderer.shadowCastingMode = part.Materials.All(m => m.renderQueue >= 3000) ? ShadowCastingMode.Off : ShadowCastingMode.On;
            return renderer;
        }
        public static void ConfigureGroup(LODGroup group, Renderer[][] renderers, bool building)
        {
            float[] heights = building ? new[] { .72f, .28f, .008f } : new[] { .48f, .14f, .012f };
            group.SetLODs(renderers.Select((r, i) => new LOD(heights[i], r) { fadeTransitionWidth = i == 2 ? .12f : .20f }).ToArray());
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = false; // Camera-distance blend: rapid zoom/reverse cannot leave a time-based fade behind.
            group.RecalculateBounds();
            if (building)
            {
                // Normalize to architectural height, not the width of the attached yard.
                // MeshRenderer bounds remain exact for frustum/shadow culling.
                var b = renderers[0][0].bounds;
                foreach (var r in renderers[0].Skip(1)) b.Encapsulate(r.bounds);
                group.localReferencePoint = group.transform.InverseTransformPoint(b.center);
                group.size = b.size.y / Mathf.Abs(group.transform.lossyScale.y);
            }
        }
        static void Variants(Dictionary<string, Part> kit)
        {
            Folder(KitRoot + "/Prefabs");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
            foreach (var entry in kit.Keys.Where(n => n.EndsWith("_LOD1", StringComparison.Ordinal)).OrderBy(n => n))
            {
                string name = entry.Substring(0, entry.Length - 5);
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.Kit + "/Prefabs/" + name + ".prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(original, preview);
                try
                {
                    var full = root.GetComponentsInChildren<Renderer>(true);
                    var medium = Renderer(root.transform, "LOD1", kit[name + "_LOD1"]);
                    var far = Renderer(root.transform, "LOD2", kit[name + "_LOD2"]);
                    ConfigureGroup(root.AddComponent<LODGroup>(), new[] { full, new Renderer[] { medium }, new Renderer[] { far } }, false);
                    PrefabUtility.SaveAsPrefabAsset(root, KitRoot + "/Prefabs/" + name + "_LOD.prefab");
                }
                finally { Object.DestroyImmediate(root); }
            }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        static Part Combine(IEnumerable<Placement> input, string path)
        {
            var batches = new Dictionary<Material, List<int>>();
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uvs = new List<Vector2>(); var tangents = new List<Vector4>();
            foreach (var p in input)
            {
                int offset = vertices.Count;
                var mesh = p.Part.Mesh; var normalMatrix = p.Matrix.inverse.transpose;
                vertices.AddRange(mesh.vertices.Select(p.Matrix.MultiplyPoint3x4));
                normals.AddRange(mesh.normals.Select(n => normalMatrix.MultiplyVector(n).normalized));
                uvs.AddRange(mesh.uv);
                tangents.AddRange(mesh.tangents.Select(t => {
                    var direction = p.Matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                    return new Vector4(direction.x, direction.y, direction.z, t.w);
                }));
                for (int i = 0; i < p.Part.Materials.Length; i++)
                {
                    var m = p.Part.Materials[i];
                    if (!batches.TryGetValue(m, out var list)) batches[m] = list = new List<int>();
                    list.AddRange(mesh.GetTriangles(i).Select(index => index + offset));
                }
            }
            var combined = new Mesh { name = Path.GetFileNameWithoutExtension(path), indexFormat = IndexFormat.UInt32 };
            combined.SetVertices(vertices); combined.SetNormals(normals); combined.SetUVs(0, uvs); combined.SetTangents(tangents);
            combined.subMeshCount = batches.Count;
            int submesh = 0;
            foreach (var indices in batches.Values) combined.SetTriangles(indices, submesh++, false);
            combined.RecalculateBounds();
            return new Part { Mesh = Store(path, combined), Materials = batches.Keys.ToArray() };
        }
        static Mesh ExtractSubmesh(Mesh source, int sub)
        {
            // CombineMeshes retains unused source vertices when extracting one
            // submesh. Compact explicitly before material bucketing so a mesh
            // with many materials does not duplicate its whole vertex buffer.
            var indices = source.GetIndices(sub); var used = indices.Distinct().ToArray();
            var remap = used.Select((old, index) => (old, index)).ToDictionary(p => p.old, p => p.index);
            var positions = source.vertices; var normals = source.normals; var uv = source.uv; var tangents = source.tangents;
            var mesh = new Mesh { indexFormat = used.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = used.Select(i => positions[i]).ToArray();
            mesh.normals = used.Select(i => normals[i]).ToArray(); mesh.uv = used.Select(i => uv[i]).ToArray();
            if (tangents.Length == positions.Length) mesh.tangents = used.Select(i => tangents[i]).ToArray();
            mesh.SetIndices(indices.Select(i => remap[i]).ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds(); return mesh;
        }
        static void Apply(GameObject root, Dictionary<string, Part> kit, Dictionary<string, Part> shell)
        {
            var cut = root.GetComponent<BuildingCutawayController>();
            var old = root.transform.Find(GeneratedRoot);
            foreach (var v in cut.Groups)
                v.Renderers = v.Renderers.Where(r => r != null && (old == null || !r.transform.IsChildOf(old))).ToArray();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var full = root.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
            var generated = new GameObject(GeneratedRoot); generated.transform.SetParent(root.transform, false);
            var levels = new[] { full, Array.Empty<Renderer>(), Array.Empty<Renderer>() };
            for (int level = 1; level <= 2; level++)
            {
                var byZone = new Dictionary<string, List<Placement>>();
                foreach (var r in full)
                {
                    var filter = r.GetComponent<MeshFilter>(); if (filter == null) continue;
                    var matrix = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    string originalName = filter.sharedMesh.name;
                    Part part;
                    if (!kit.TryGetValue(originalName + "_LOD" + level, out part) && !shell.TryGetValue(originalName + "_LOD" + level, out part))
                        part = new Part { Mesh = filter.sharedMesh, Materials = r.sharedMaterials }; // Existing shared fixtures keep approved silhouettes.
                    string zone = cut.Groups.FirstOrDefault(g => g.Renderers.Contains(r))?.Id ?? "RetainedInteriorAndYard";
                    if (level == 2 && zone == "RetainedInteriorAndYard" && r.bounds.size.magnitude < .6f) continue;
                    // Transparent windows remain separate from opaque walls for sorting and shadow behavior.
                    for (int sub = 0; sub < part.Materials.Length; sub++)
                    {
                        var mat = part.Materials[sub];
                        string key = zone + (mat.renderQueue >= 3000 ? "_Transparent" : "_Opaque");
                        if (!byZone.TryGetValue(key, out var list)) byZone[key] = list = new List<Placement>();
                        // A submesh-only view avoids adding all materials on each split.
                        var submesh = ExtractSubmesh(part.Mesh, sub);
                        list.Add(new Placement { Part = new Part { Mesh = submesh, Materials = new[] { mat } }, Matrix = matrix });
                    }
                }
                var renderers = new List<Renderer>();
                try
                {
                    foreach (var pair in byZone.OrderBy(p => p.Key))
                    {
                        // Material-homogeneous batches keep the URP result
                        // equivalent to standalone props. Multi-material zone
                        // meshes produced colour artifacts in matched captures.
                        foreach (var batch in pair.Value.GroupBy(p => p.Part.Materials[0]))
                        {
                            string name = pair.Key + "_" + batch.Key.name + "_LOD" + level;
                            var renderer = Renderer(generated.transform, name, Combine(batch, Root + "/Meshes/" + name + ".asset"));
                            renderers.Add(renderer);
                            var owner = cut.Groups.FirstOrDefault(g => pair.Key.StartsWith(g.Id + "_", StringComparison.Ordinal));
                            if (owner != null) owner.Renderers = owner.Renderers.Append(renderer).ToArray();
                        }
                    }
                }
                finally { foreach (var p in byZone.Values.SelectMany(p => p)) Object.DestroyImmediate(p.Part.Mesh); }
                levels[level] = renderers.ToArray();
            }
            var lod = root.GetComponent<LODGroup>();
            if (lod == null) lod = root.AddComponent<LODGroup>();
            ConfigureGroup(lod, levels, true);
        }
        [MenuItem("SilverScreen/Art/Studio Services/7 Build render-only LOD derivatives")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            Verify("ArtExports/PeriodEnvironment1930/LOD/lod_manifest.json"); Verify("ArtExports/StudioServices/LOD/lod_manifest.json");
            var kit = Import(KitPacket, KitRoot + "/Meshes");
            var shell = Import(BuildingPacket, Root + "/SourceMeshes"); Folder(Root + "/Meshes");
            Variants(kit);
            var root = PrefabUtility.LoadPrefabContents(StudioServicesProductionBuilder.PrefabPath);
            try { Apply(root, kit, shell); PrefabUtility.SaveAsPrefabAsset(root, StudioServicesProductionBuilder.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Studio Services LODs imported; canonical LOD0 assets retained.");
        }
    }
}
