using System;
using System.Collections.Generic;
using SilverScreen.Domain.Buildings;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Render-only mesh copy, with no gameplay scripts, colliders, lights or navigation.</summary>
    public sealed class BuildingPlacementPreview : IDisposable
    {
        public GameObject Root { get; }
        private readonly Material _ghost;
        private readonly Material _outline;
        public BuildingPlacementPreview(StudioBuildingView template, BuildingDefinition definition)
        {
            Root = new GameObject("Building Placement Preview"); Root.layer = 2;
            _ghost = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _ghost.SetFloat("_Surface", 1);
            _ghost.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _ghost.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _ghost.SetFloat("_ZWrite", 0);
            _ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _ghost.SetOverrideTag("RenderType", "Transparent");
            _ghost.renderQueue = (int)RenderQueue.Transparent;
            _outline = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var excluded = new HashSet<Renderer>();
            foreach (var group in template.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) excluded.Add(r);
                if (lods.Length > 0) foreach (var r in lods[0].renderers) excluded.Remove(r);
            }
            CopyMeshes(template.transform, Root.transform, excluded, true);
            foreach(var area in definition.PlacementAreas) Outline("Placement area", area, .10f, .16f);
            Outline("Construction clearance", definition.ConstructionClearance, .08f, .06f);
            Outline("Entrance clearance", definition.EntranceClearance, .12f, .10f);
            var a = definition.ExteriorApproach; var b = definition.Entrance;
            var start = new Vector3(a.X, .18f, a.Z); var end = new Vector3(b.X, .18f, b.Z);
            var direction = (end - start).normalized; var side = Vector3.Cross(Vector3.up, direction) * .5f;
            Line("Entrance direction", new[] { start, end, end - direction + side, end, end - direction - side }, .18f);
            Root.SetActive(false);
        }
        private void CopyMeshes(Transform source, Transform parent, HashSet<Renderer> excluded, bool root = false)
        {
            if (!root && !source.gameObject.activeSelf) return;
            var copy = new GameObject(source.name).transform; copy.gameObject.layer = 2;
            copy.SetParent(parent, false);
            copy.localPosition = root ? Vector3.zero : source.localPosition;
            copy.localRotation = root ? Quaternion.identity : source.localRotation;
            copy.localScale = source.localScale;
            var mesh = source.GetComponent<MeshFilter>(); var renderer = source.GetComponent<MeshRenderer>();
            if (mesh != null && mesh.sharedMesh != null && renderer != null && renderer.enabled && !excluded.Contains(renderer))
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var view = copy.gameObject.AddComponent<MeshRenderer>();
                var materials = new Material[mesh.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = _ghost;
                view.sharedMaterials = materials;
                view.shadowCastingMode = ShadowCastingMode.Off; view.receiveShadows = false;
                view.lightProbeUsage = LightProbeUsage.Off; view.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            for (int i = 0; i < source.childCount; i++) CopyMeshes(source.GetChild(i), copy, excluded);
        }
        private void Outline(string name, PlacementRect rect, float y, float width)
        {
            var corners = rect.Corners(default); var points = new Vector3[5];
            for (int i = 0; i < 5; i++) points[i] = new Vector3(corners[i % 4].X, y, corners[i % 4].Z);
            Line(name, points, width);
        }
        private void Line(string name, Vector3[] points, float width)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(Root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.sharedMaterial = _outline; line.widthMultiplier = width;
            line.positionCount = points.Length; line.SetPositions(points);
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        }
        public void Move(BuildingPose pose, bool visible)
        {
            Root.SetActive(visible);
            if (visible) Root.transform.SetPositionAndRotation(new Vector3(pose.X, 0, pose.Z), Quaternion.Euler(0, pose.Yaw, 0));
        }
        public void SetValid(bool valid)
        {
            var color = valid ? new Color(.22f, .85f, .55f) : new Color(1f, .28f, .18f);
            _outline.SetColor("_BaseColor", color);
            color.a = .32f; _ghost.SetColor("_BaseColor", color);
        }
        public void Dispose()
        {
            Root.SetActive(false); UnityEngine.Object.Destroy(Root);
            UnityEngine.Object.Destroy(_ghost); UnityEngine.Object.Destroy(_outline);
        }
    }
}
