using SilverScreen.Domain.ReusableAssets;
using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    /// <summary>One socket, many roles. Artwork state belongs to the instance, never the shared material.</summary>
    [ExecuteAlways]
    public sealed class DisplayInsertSlot : MonoBehaviour
    {
        [SerializeField] private Vector2 _aperture = Vector2.one;
        [SerializeField] private float _insertionDepth;
        [SerializeField] private GameObject _acousticInsert, _graphicBacking;
        [SerializeField] private MeshRenderer _artworkRenderer, _matRenderer;
        [SerializeField] private Renderer _optionalGlazing;
        [SerializeField] private InsertRole _role;
        [SerializeField] private ArtworkFit _fit;
        [SerializeField] private Texture _artwork;
        [SerializeField] private Color _matColor = new Color(.16f,.15f,.12f,1);
        private MaterialPropertyBlock _artworkProperties, _matProperties;
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        public Vector2 Aperture => _aperture;
        public float InsertionDepth => _insertionDepth;
        public InsertRole Role => _role;
        public Texture Artwork => _artwork;
        public MeshRenderer ArtworkRenderer => _artworkRenderer;

        public void SetInsert(InsertRole role, Texture artwork = null, ArtworkFit fit = ArtworkFit.FitWithMat)
        { _role = role; _artwork = artwork; _fit = fit; Refresh(); }
        public void SetMatColor(Color color) { _matColor = color; Refresh(); }
        private void OnEnable() => Refresh();
        private void OnDisable()
        {
            if (_artworkRenderer != null) _artworkRenderer.SetPropertyBlock(null);
            if (_matRenderer != null) _matRenderer.SetPropertyBlock(null);
        }

        public void Refresh()
        {
            // Incomplete authoring references do not create objects or alter materials.
            if (_artworkRenderer == null || _matRenderer == null || _aperture.x <= 0 || _aperture.y <= 0) return;
            bool graphic = _role != InsertRole.AcousticFabric;
            if (_acousticInsert != null) _acousticInsert.SetActive(!graphic);
            if (_graphicBacking != null) _graphicBacking.SetActive(graphic);
            _matRenderer.gameObject.SetActive(graphic);
            _artworkRenderer.gameObject.SetActive(graphic && _artwork != null);
            if (_optionalGlazing != null) _optionalGlazing.enabled = graphic;
            _matRenderer.transform.localScale = new Vector3(_aperture.x, _aperture.y, 1);
            _matProperties ??= new MaterialPropertyBlock();
            _matProperties.Clear(); _matProperties.SetColor(BaseColor, _matColor);
            _matRenderer.SetPropertyBlock(_matProperties);
            _artworkProperties ??= new MaterialPropertyBlock();
            _artworkProperties.Clear();
            if (graphic && _artwork != null)
            {
                var layout = ArtworkLayout.Calculate(_aperture.x, _aperture.y, _artwork.width, _artwork.height, _fit);
                _artworkRenderer.transform.localScale = new Vector3(layout.Width, layout.Height, 1);
                _artworkProperties.SetTexture(BaseMap, _artwork);
                _artworkProperties.SetColor(BaseColor, Color.white);
                _artworkProperties.SetVector(BaseMapST, new Vector4(layout.U, layout.V, layout.OffsetU, layout.OffsetV));
            }
            _artworkRenderer.SetPropertyBlock(_artworkProperties);
        }
    }
}
