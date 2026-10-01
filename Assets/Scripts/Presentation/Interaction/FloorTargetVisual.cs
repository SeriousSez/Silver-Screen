using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Disposable floor-depth-tested room treatment and text. No colliders or input ownership.</summary>
    internal sealed class FloorTargetVisual
    {
        private readonly GameObject _root;
        private readonly Material[] _materials = new Material[3];
        private readonly Mesh[] _meshes = new Mesh[3];
        private readonly Color _tint;
        private readonly TextMeshPro _label;
        private ContextualRoomState _state = (ContextualRoomState)(-1);
        private string _text;
        public FloorTargetVisual(Transform parent, FloorTargetShape shape, Vector2 size, Vector3 offset, FloorTargetSymbol symbol, Color tint,
            Rect[] rooms, Vector2 labelPosition, float labelWidth)
        {
            _tint = tint;
            _root = new GameObject("Contextual floor overlay") { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(parent, false); _root.transform.localPosition = offset + Vector3.up * .025f;
            var outline = new List<Vector2>();
            if (shape == FloorTargetShape.Room) { }
            else if (shape == FloorTargetShape.Zone)
            { outline.Add(new Vector2(-1, -1)); outline.Add(new Vector2(1, -1)); outline.Add(new Vector2(1, 1)); outline.Add(new Vector2(-1, 1)); }
            else for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48; outline.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a))); }
            var fill = new Geometry(); var edge = new Geometry(); var icon = new Geometry();
            if (shape == FloorTargetShape.Room)
                foreach (var r in rooms)
                {
                    var points = new[] { new Vector2(r.xMin,r.yMin), new Vector2(r.xMax,r.yMin), new Vector2(r.xMax,r.yMax), new Vector2(r.xMin,r.yMax) };
                    fill.Quad(points[3], points[2], points[1], points[0]);
                    for (int i = 0; i < 4; i++)
                    {
                        var a = points[i]; var b = points[(i+1)%4];
                        var outward = (a+b)*.5f + new Vector2((b-a).y, -(b-a).x).normalized * .015f;
                        bool internalEdge = false;
                        foreach (var other in rooms) if (other.Contains(outward)) { internalEdge = true; break; }
                        if (!internalEdge) edge.Stroke(a, b, .065f);
                    }
                }
            for (int i = 0; i < outline.Count; i++)
            {
                var a = Vector2.Scale(outline[i], size * .5f); var b = Vector2.Scale(outline[(i + 1) % outline.Count], size * .5f);
                fill.Triangle(Vector2.zero, b, a);
                edge.Quad(a, b, b * .96f, a * .96f);
            }
            // Restrained, replaceable floor glyphs; no profession logic lives in the renderer.
            float s = Mathf.Min(size.x, size.y) * .26f;
            if (symbol == FloorTargetSymbol.Hammer)
            { icon.Stroke(new Vector2(-.35f, -.65f) * s, new Vector2(.25f, .45f) * s, .14f * s); icon.Stroke(new Vector2(-.25f, .6f) * s, new Vector2(.7f, .08f) * s, .28f * s); }
            else if (symbol == FloorTargetSymbol.Leaf)
            {
                icon.Stroke(new Vector2(-.5f, -.7f) * s, new Vector2(.45f, .5f) * s, .08f * s);
                icon.Quad(new Vector2(-.15f, -.2f) * s, new Vector2(-.5f, .3f) * s, new Vector2(.4f, .75f) * s, new Vector2(.55f, .1f) * s);
            }
            else if (symbol == FloorTargetSymbol.Performance)
            {
                icon.Stroke(new Vector2(-.6f, .5f) * s, new Vector2(-.15f, .5f) * s, .13f * s);
                icon.Stroke(new Vector2(.15f, .5f) * s, new Vector2(.6f, .5f) * s, .13f * s);
                icon.Stroke(new Vector2(-.55f, -.15f) * s, new Vector2(0, -.5f) * s, .1f * s);
                icon.Stroke(new Vector2(0, -.5f) * s, new Vector2(.55f, -.15f) * s, .1f * s);
            }
            var geometry = new[] { fill, edge, icon };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject(new[] { "Floor wash", "Border", "Symbol" }[i]); go.transform.SetParent(_root.transform, false);
                go.transform.localPosition = Vector3.up * (i * .002f);
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.HideAndDontSave };
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = (int)RenderQueue.Transparent + i;
                _materials[i] = material; _meshes[i] = geometry[i].Build();
                go.AddComponent<MeshFilter>().sharedMesh = _meshes[i];
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            if (shape == FloorTargetShape.Room)
            {
                var labelObject = new GameObject("Room floor label"); labelObject.transform.SetParent(_root.transform, false);
                labelObject.transform.localPosition = new Vector3(labelPosition.x, .014f, labelPosition.y);
                labelObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
                _label = labelObject.AddComponent<TextMeshPro>();
                _label.rectTransform.sizeDelta = new Vector2(labelWidth, 1.15f);
                _label.alignment = TextAlignmentOptions.Center; _label.fontStyle = FontStyles.Bold;
                _label.enableAutoSizing = true; _label.fontSizeMin = 2.2f; _label.fontSizeMax = 4.8f;
                _label.textWrappingMode = TextWrappingModes.Normal; _label.raycastTarget = false;
                _label.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            SetState(ContextualRoomState.Hidden, "");
        }
        public void SetState(ContextualRoomState state, string label)
        {
            if (_state == state && _text == label) return;
            _state = state; _text = label;
            _root.SetActive(state != ContextualRoomState.Hidden);
            bool candidate = state == ContextualRoomState.Active;
            bool disabled = state == ContextualRoomState.Disabled || state == ContextualRoomState.Current;
            var color = disabled ? new Color(.47f, .49f, .50f) : candidate ? Color.Lerp(_tint, new Color(.96f, .86f, .61f), .65f) : _tint;
            for (int i = 0; i < 3; i++)
            { color.a = i == 0 ? (candidate ? .44f : disabled ? .13f : .24f) : (candidate ? 1f : disabled ? .4f : .75f); _materials[i].SetColor("_BaseColor", color); }
            if (_label != null)
            {
                _label.text = label;
                _label.color = disabled ? new Color(.67f,.68f,.69f) : candidate ? new Color(1,.96f,.78f) : new Color(.96f,.88f,.66f);
            }
        }
        public void Dispose()
        {
            Destroy(_root); foreach (var m in _materials) Destroy(m); foreach (var m in _meshes) Destroy(m);
        }
        private static void Destroy(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
        private sealed class Geometry
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();
            public void Triangle(Vector2 a, Vector2 b, Vector2 c)
            {
                int n = _vertices.Count;
                foreach (var p in new[] { a, b, c }) _vertices.Add(new Vector3(p.x, 0, p.y));
                _triangles.Add(n); _triangles.Add(n + 1); _triangles.Add(n + 2);
            }
            public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d) { Triangle(a, b, c); Triangle(a, c, d); }
            public void Stroke(Vector2 a, Vector2 b, float width)
            { var d = (b - a).normalized; var side = new Vector2(-d.y, d.x) * width * .5f; Quad(a - side, a + side, b + side, b - side); }
            public Mesh Build()
            { var mesh = new Mesh { name = "Contextual floor shape", hideFlags = HideFlags.HideAndDontSave }; mesh.SetVertices(_vertices); mesh.SetTriangles(_triangles, 0); mesh.RecalculateBounds(); return mesh; }
        }
    }
}
