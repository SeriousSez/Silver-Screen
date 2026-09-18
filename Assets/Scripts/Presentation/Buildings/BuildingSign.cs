using TMPro;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public class BuildingSign : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _label;

        [Header("Responsive Fit")]
        [SerializeField, Range(0f, 0.45f)] private float _horizontalPadding = 0.08f;
        [SerializeField, Range(0f, 0.45f)] private float _verticalPadding = 0.10f;
        [SerializeField, Min(1f)] private float _referenceFontSize = 36f;

        protected virtual void Awake()
        {
            FitTextToSign();
        }

        public void Configure(TextMeshPro label)
        {
            _label = label;
            FitTextToSign();
        }

        public void SetText(string value)
        {
            if (_label != null)
            {
                _label.text = value ?? string.Empty;
                FitTextToSign();
            }
        }

        private void FitTextToSign()
        {
            if (_label == null || string.IsNullOrWhiteSpace(_label.text))
            {
                return;
            }

            Rect rect = _label.rectTransform.rect;
            float usableWidth = rect.width * (1f - (_horizontalPadding * 2f));
            float usableHeight = rect.height * (1f - (_verticalPadding * 2f));

            if (usableWidth <= 0f || usableHeight <= 0f)
            {
                return;
            }

            // TMP font-size units do not correspond to RectTransform units.
            // Measure real glyph bounds at a stable reference size, then scale
            // proportionally against both dimensions of the marquee.
            _label.enableAutoSizing = false;
            _label.fontSize = _referenceFontSize;
            _label.ForceMeshUpdate();

            Vector2 renderedSize = _label.GetPreferredValues(
                _label.text,
                float.PositiveInfinity,
                float.PositiveInfinity);
            if (renderedSize.x <= 0f || renderedSize.y <= 0f)
            {
                return;
            }

            float widthScale = usableWidth / renderedSize.x;
            float heightScale = usableHeight / renderedSize.y;
            _label.fontSize = _referenceFontSize * Mathf.Min(widthScale, heightScale);
            _label.ForceMeshUpdate();
        }

    }
}