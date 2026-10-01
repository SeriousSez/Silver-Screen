using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>
    /// Separates authored wall bands from existing render batches once per instance.
    /// Triangle positions, attributes and materials are retained; source assets and collision are untouched.
    /// Lower LODs currently batch partitions with retained furnishings, so hiding that whole batch is unsafe.
    /// </summary>
    internal sealed class BuildingCutawaySections : IDisposable
    {
        private sealed class Original
        { public MeshFilter Filter; public Mesh Mesh; public MeshRenderer Renderer; public Material[] Materials; public bool ForcedOff; }
        private readonly List<Original> _originals = new List<Original>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly LODGroup _lod;
        private readonly LOD[] _originalLods;
        public BuildingCutawayController.VisibilityGroup[] Groups { get; }

        public BuildingCutawaySections(Transform building, Bounds[] sections, MeshRenderer[] sources, Collider[] colliders)
        {
            var renderers = sections.Select(_ => new List<Renderer>()).ToArray();
            var additions = new Dictionary<Renderer, List<Renderer>>();
            foreach (var source in sources.Distinct())
            {
                if (source == null) continue;
                var filter = source.GetComponent<MeshFilter>(); var mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var matrix = building.worldToLocalMatrix * source.transform.localToWorldMatrix;
                var buckets = new List<int>[sections.Length + 1][];
                for (int b = 0; b < buckets.Length; b++) buckets[b] = Enumerable.Range(0,mesh.subMeshCount).Select(_ => new List<int>()).ToArray();
                bool found = false;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetTriangles(sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        var a = matrix.MultiplyPoint3x4(vertices[indices[i]]);
                        var b = matrix.MultiplyPoint3x4(vertices[indices[i+1]]);
                        var c = matrix.MultiplyPoint3x4(vertices[indices[i+2]]);
                        int bucket = sections.Length;
                        // Whole triangles only: never cut through furniture or deform the approved wall geometry.
                        for (int s = 0; s < sections.Length; s++)
                            if (sections[s].Contains(a) && sections[s].Contains(b) && sections[s].Contains(c)) { bucket = s; found = true; break; }
                        buckets[bucket][sub].Add(indices[i]); buckets[bucket][sub].Add(indices[i+1]); buckets[bucket][sub].Add(indices[i+2]);
                    }
                }
                if (!found) continue;
                var materials = source.sharedMaterials;
                _originals.Add(new Original { Filter = filter, Mesh = mesh, Renderer = source, Materials = materials, ForcedOff = source.forceRenderingOff });
                additions[source] = new List<Renderer>();
                for (int b = 0; b < buckets.Length; b++)
                {
                    var part = Compact(mesh, buckets[b], out var usedSubmeshes);
                    if (part == null) { if (b == sections.Length) source.forceRenderingOff = true; continue; }
                    _meshes.Add(part);
                    var partMaterials = usedSubmeshes.Select(i => materials[Mathf.Min(i,materials.Length-1)]).ToArray();
                    if (b == sections.Length) { filter.sharedMesh = part; source.sharedMaterials = partMaterials; continue; }
                    var go = new GameObject("Cutaway partition " + b) { layer = source.gameObject.layer, hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(source.transform, false); _objects.Add(go);
                    go.AddComponent<MeshFilter>().sharedMesh = part;
                    var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterials = partMaterials;
                    renderer.shadowCastingMode = source.shadowCastingMode; renderer.receiveShadows = source.receiveShadows;
                    renderer.lightProbeUsage = source.lightProbeUsage; renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                    renderer.lightmapIndex = source.lightmapIndex; renderer.lightmapScaleOffset = source.lightmapScaleOffset;
                    renderer.renderingLayerMask = source.renderingLayerMask;
                    renderers[b].Add(renderer); additions[source].Add(renderer);
                }
            }
            Groups = sections.Select((bounds,i) => new BuildingCutawayController.VisibilityGroup {
                Id = "InteriorSection" + i, SightlineOnly = true, LocalSection = bounds, Renderers = renderers[i].ToArray(),
                Occluders = colliders.Where(c => c != null && bounds.Contains(building.InverseTransformPoint(c.bounds.center))).ToArray()
            }).ToArray();
            _lod = building.GetComponent<LODGroup>();
            if (_lod != null)
            {
                _originalLods = _lod.GetLODs(); var levels = _lod.GetLODs();
                for (int i = 0; i < levels.Length; i++)
                    levels[i].renderers = levels[i].renderers.Concat(levels[i].renderers.Where(additions.ContainsKey).SelectMany(r => additions[r])).ToArray();
                _lod.SetLODs(levels); // Keep thresholds, fade settings and the authored group bounds.
            }
        }

        private static Mesh Compact(Mesh source, List<int>[] submeshes, out int[] usedSubmeshes)
        {
            usedSubmeshes = Enumerable.Range(0,submeshes.Length).Where(i => submeshes[i].Count > 0).ToArray();
            if (usedSubmeshes.Length == 0) return null;
            var used = usedSubmeshes.SelectMany(i => submeshes[i]).Distinct().ToArray();
            var remap = used.Select((old,index) => (old,index)).ToDictionary(p => p.old,p => p.index);
            var mesh = new Mesh { name = source.name + " (cutaway section)", hideFlags = HideFlags.DontSave,
                indexFormat = used.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            var vertices = source.vertices; var normals = source.normals; var tangents = source.tangents; var colors = source.colors32;
            mesh.vertices = used.Select(i => vertices[i]).ToArray();
            if (normals.Length == vertices.Length) mesh.normals = used.Select(i => normals[i]).ToArray();
            if (tangents.Length == vertices.Length) mesh.tangents = used.Select(i => tangents[i]).ToArray();
            if (colors.Length == vertices.Length) mesh.colors32 = used.Select(i => colors[i]).ToArray();
            for (int channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); source.GetUVs(channel,uv);
                if (uv.Count == vertices.Length) mesh.SetUVs(channel,used.Select(i => uv[i]).ToList());
            }
            mesh.subMeshCount = usedSubmeshes.Length;
            for (int s = 0; s < usedSubmeshes.Length; s++) mesh.SetTriangles(submeshes[usedSubmeshes[s]].Select(i => remap[i]).ToArray(),s,false);
            mesh.RecalculateBounds(); return mesh;
        }
        public void Dispose()
        {
            if (_lod != null && _originalLods != null) _lod.SetLODs(_originalLods);
            foreach (var original in _originals)
            {
                if (original.Filter != null) original.Filter.sharedMesh = original.Mesh;
                if (original.Renderer != null) { original.Renderer.sharedMaterials = original.Materials; original.Renderer.forceRenderingOff = original.ForcedOff; }
            }
            foreach (var go in _objects) Destroy(go);
            foreach (var mesh in _meshes) Destroy(mesh);
        }
        private static void Destroy(UnityEngine.Object value)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
