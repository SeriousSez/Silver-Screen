using System;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>One renderer's cached semantic binding. Unity owns all multi-frame interpolation.</summary>
    public sealed class CharacterFacialController
    {
        private readonly SkinnedMeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly int[] _indices;
        private readonly FacialBindingEntry[] _entries;
        private readonly FacialCapability[] _capabilities;
        private readonly string[] _reasons;
        public SkinnedMeshRenderer Renderer => _renderer;
        public CharacterFacialController(SkinnedMeshRenderer renderer, CharacterFacialBinding binding)
        {
            if (renderer == null || renderer.sharedMesh == null) throw new ArgumentException("An explicit face renderer and mesh are required.");
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (!binding.TryValidate(out var reason)) throw new ArgumentException(reason);
            _renderer = renderer; _mesh = renderer.sharedMesh;
            int count = Enum.GetValues(typeof(FacialSemantic)).Length;
            _indices = new int[count]; _entries = new FacialBindingEntry[count]; _capabilities = new FacialCapability[count]; _reasons = new string[count];
            for (int i = 0; i < count; i++) { _indices[i] = -1; _reasons[i] = "Semantic not supplied by this binding."; }
            foreach (var entry in binding.Entries)
            {
                int s = (int)entry.Semantic; _entries[s] = entry; _capabilities[s] = entry.Capability; _reasons[s] = entry.Reason;
                if (entry.Capability != FacialCapability.Supported) continue;
                _indices[s] = _mesh.GetBlendShapeIndex(entry.RawChannel);
                if (_indices[s] < 0) { _capabilities[s] = FacialCapability.Missing; _reasons[s] = "Mesh channel missing: " + entry.RawChannel; }
            }
        }
        private bool Valid(FacialSemantic semantic) => (uint)semantic < _indices.Length;
        public FacialCapability Capability(FacialSemantic semantic) => Valid(semantic) ? _capabilities[(int)semantic] : FacialCapability.Missing;
        public string Diagnostic(FacialSemantic semantic) => Valid(semantic) ? _reasons[(int)semantic] : "Unknown semantic.";
        public bool Supports(FacialSemantic semantic) => Capability(semantic) == FacialCapability.Supported && _renderer != null && _renderer.sharedMesh == _mesh;
        public bool TrySet(FacialSemantic semantic, float normalized)
        {
            if (!float.IsFinite(normalized) || !Supports(semantic)) return false;
            _renderer.SetBlendShapeWeight(_indices[(int)semantic], _entries[(int)semantic].Weight(Mathf.Clamp01(normalized)));
            return true;
        }
        public bool Reset(FacialSemantic semantic) => TrySet(semantic, 0);
        public void ResetAll() { for (int i = 0; i < _indices.Length; i++) Reset((FacialSemantic)i); }
    }
}
