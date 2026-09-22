using System;
using SilverScreen.Domain.ReusableAssets;
using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    [Serializable]
    public struct ArtworkFieldValue { public string Key; public string Value; }

    /// <summary>Instance-only artwork. The shared frame, mesh, template, and base material stay immutable.</summary>
    [ExecuteAlways]
    public sealed class DisplayArtworkPresenter : MonoBehaviour
    {
        [SerializeField] private DisplayInsertSlot _slot;
        [SerializeField] private DisplayArtworkTemplate _template;
        [SerializeField] private InsertRole _role = InsertRole.Poster;
        [SerializeField] private ArtworkFit _fit = ArtworkFit.FitWithMat;
        [Tooltip("Explicit authoring preview/content snapshot; runtime hosts supply an ArtworkContext.")]
        [SerializeField] private ArtworkFieldValue[] _previewFields = Array.Empty<ArtworkFieldValue>();
        private ArtworkContext _context;
        private RenderTexture _rendered;
        public RenderTexture RenderedArtwork => _rendered;

        public void Configure(DisplayInsertSlot slot, DisplayArtworkTemplate template, InsertRole role, ArtworkFit fit, ArtworkFieldValue[] preview)
        {
            _slot = slot; _template = template; _role = role; _fit = fit;
            _previewFields = preview ?? Array.Empty<ArtworkFieldValue>();
            Initialize(null);
        }

        public void Initialize(ArtworkContext context)
        {
            Unsubscribe();
            _context = context;
            if (isActiveAndEnabled) { Subscribe(); Refresh(); }
        }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() { Unsubscribe(); ReleaseArtwork(); }
        private void Subscribe() { if (_context != null) { _context.Changed -= Refresh; _context.Changed += Refresh; } }
        private void Unsubscribe() { if (_context != null) _context.Changed -= Refresh; }
        public void Refresh()
        {
            if (!isActiveAndEnabled || _template == null || _slot == null) return;
            var context = _context;
            if (context == null)
            {
                context = new ArtworkContext();
                foreach (var field in _previewFields) if (!string.IsNullOrWhiteSpace(field.Key)) context.Set(field.Key, field.Value);
            }
            var next = DisplayArtworkComposer.Compose(_template, context);
            ReleaseArtwork();
            _rendered = next;
            _slot.SetInsert(_role, _rendered, _fit);
        }
        private void ReleaseArtwork()
        {
            if (_rendered == null) return;
            if (_slot != null && _slot.Artwork == _rendered) _slot.SetInsert(_role, null, _fit);
            _rendered.Release();
            if (Application.isPlaying) Destroy(_rendered); else DestroyImmediate(_rendered);
            _rendered = null;
        }
    }
}
