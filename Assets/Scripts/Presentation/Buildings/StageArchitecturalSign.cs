using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Instance-driven cast lettering. No number is part of the building FBX.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StageArchitecturalSign : MonoBehaviour
    {
        [SerializeField] private SoundStageIdentity _identity;
        [SerializeField] private string _characters = "AEGST0123456789";
        [SerializeField] private Mesh[] _glyphs;
        [SerializeField] private Material _material;
        [SerializeField, Min(0.1f)] private float _letterHeight = 1.12f;
        [SerializeField, Min(0.1f)] private float _maximumWidth = 5.6f;
        private Mesh _generatedMesh;
        private int _renderedNumber;

        private void OnEnable()
        {
            if (_identity != null) SetNumber(_identity.StageNumber);
        }

        public void SetNumber(int number)
        {
            if (number < 1 || _glyphs == null || _glyphs.Length != _characters.Length || _material == null)
                return;
            if (_generatedMesh != null && _renderedNumber == number) return;
            string text = "STAGE " + number.ToString(CultureInfo.InvariantCulture);
            var pieces = new List<CombineInstance>(text.Length);
            float cursor = 0f;
            foreach (char c in text)
            {
                if (c == ' ') { cursor += 0.38f; continue; }
                int index = _characters.IndexOf(c);
                if (index < 0 || _glyphs[index] == null) return;
                Mesh glyph = _glyphs[index];
                pieces.Add(new CombineInstance { mesh = glyph, transform = Matrix4x4.Translate(new Vector3(cursor, 0f, 0f)) });
                cursor += glyph.bounds.size.x + 0.10f;
            }
            float width = cursor - 0.10f;
            float scale = Mathf.Min(_letterHeight, _maximumWidth / width);
            for (int i = 0; i < pieces.Count; i++)
            {
                CombineInstance piece = pieces[i];
                piece.transform = Matrix4x4.Scale(Vector3.one * scale) *
                    Matrix4x4.Translate(new Vector3(-width * 0.5f, 0f, 0f)) * piece.transform;
                pieces[i] = piece;
            }
            ReleaseMesh();
            _generatedMesh = new Mesh { name = "Instance stage lettering", hideFlags = HideFlags.DontSave };
            _generatedMesh.CombineMeshes(pieces.ToArray(), true, true);
            _generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _generatedMesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            _renderedNumber = number;
        }

        private void OnDisable() => ReleaseMesh();
        private void OnDestroy() => ReleaseMesh();

        private void ReleaseMesh()
        {
            if (_generatedMesh == null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == _generatedMesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(_generatedMesh);
            else DestroyImmediate(_generatedMesh);
            _generatedMesh = null;
        }
    }
}
