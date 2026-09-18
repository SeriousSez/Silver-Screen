using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    internal static class ManagementUIFactory
    {
        internal static readonly Color Panel = new Color(0.055f, 0.065f, 0.075f, 0.98f);
        internal static readonly Color PanelRaised = new Color(0.085f, 0.10f, 0.115f, 0.98f);
        internal static readonly Color Gold = new Color(0.95f, 0.70f, 0.18f, 1f);
        internal static readonly Color Muted = new Color(0.68f, 0.72f, 0.76f, 1f);

        internal static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static RectTransform Stretch(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        internal static Image Background(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static TextMeshProUGUI Text(
            string name,
            Transform parent,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            var rect = Stretch(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        internal static Button Button(
            string name,
            Transform parent,
            string label,
            Color background,
            Color foreground)
        {
            var rect = Stretch(name, parent);
            var image = Background(rect, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", rect, label, 15f, TextAlignmentOptions.Center, foreground);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        internal static void SetOffsets(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static ScrollRect ScrollView(string name, Transform parent, out RectTransform content)
        {
            var root = Stretch(name, parent);
            var rootImage = Background(root, new Color(0.025f, 0.03f, 0.035f, 0.35f));
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Stretch("Viewport", root);
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            content = Rect(
                "Content",
                viewport,
                new Vector2(0f, 1f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }
    }
}
