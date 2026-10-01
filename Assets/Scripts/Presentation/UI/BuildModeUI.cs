using System;
using System.Collections.Generic;
using SilverScreen.Domain.Finance;
using SilverScreen.Presentation.Buildings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public enum BuildCategory { Facilities, Sets, LandscapingAndDecorations }

    /// <summary>World-tool catalog entry. Availability and selection belong to its content provider.</summary>
    public sealed class BuildPaletteItem
    {
        public string Id { get; }
        public BuildCategory Category { get; }
        public string Name { get; }
        public Money Cost { get; }
        public Func<string> UnavailableReason { get; }
        public Action Select { get; }
        public BuildPaletteItem(string id, BuildCategory category, string name, Money cost, Func<string> unavailableReason, Action select)
        { Id = id; Category = category; Name = name; Cost = cost; UnavailableReason = unavailableReason; Select = select; }
    }

    /// <summary>Compact world tools and bottom palette. No management-navigation integration.</summary>
    public sealed class BuildModeUI : MonoBehaviour
    {
        private StudioBuildMode _mode;
        private GameObject _root, _categories, _palette, _statusRoot;
        private TextMeshProUGUI _status, _title, _empty;
        private Button _build;
        private Texture2D _circleTexture;
        private Sprite _circle;
        private readonly List<(BuildPaletteItem item, Button button, TextMeshProUGUI text)> _items = new List<(BuildPaletteItem, Button, TextMeshProUGUI)>();
        private readonly Dictionary<BuildCategory, Button> _categoryButtons = new Dictionary<BuildCategory, Button>();
        private BuildCategory? _shownCategory;
        private float _nextRefresh;

        public void Initialize(StudioBuildMode mode)
        {
            _mode = mode;
            _root = new GameObject("Build World Tools", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30;
            var scaler = _root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            CreateCircle();
            _build = RoundButton(_root.transform, "Build", new Vector2(204, 65), 76, () => { if (_mode.IsOpen) _mode.Close(); else _mode.Open(); });
            _categories = ManagementUIFactory.Stretch("Build categories", _root.transform).gameObject;
            AddCategory(BuildCategory.Facilities, "Facilities", new Vector2(205, 173));
            AddCategory(BuildCategory.Sets, "Sets", new Vector2(290, 134));
            AddCategory(BuildCategory.LandscapingAndDecorations, "Landscaping\n& Decorations", new Vector2(314, 47));

            var palette = ManagementUIFactory.Rect("Contextual build palette", _root.transform,
                Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero);
            palette.offsetMin = new Vector2(374, 18); palette.offsetMax = new Vector2(-24, 184);
            ManagementUIFactory.Background(palette, ManagementUIFactory.Panel);
            _palette = palette.gameObject;
            var header = ManagementUIFactory.Rect("Category title", palette, new Vector2(0, 1), Vector2.one, new Vector2(-60, -21), new Vector2(-140, 36));
            _title = ManagementUIFactory.Text("Title", header, "", 18, TextAlignmentOptions.MidlineLeft, ManagementUIFactory.Gold);
            var back = ManagementUIFactory.Rect("Back", palette, Vector2.one, Vector2.one, new Vector2(-60, -22), new Vector2(100, 30));
            ManagementUIFactory.Button("Close", back, "Close [Esc]", ManagementUIFactory.PanelRaised, Color.white).onClick.AddListener(_mode.Close);
            var row = ManagementUIFactory.Rect("Items", palette, Vector2.zero, Vector2.one, new Vector2(0, -16), new Vector2(-24, -62));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            foreach (var item in mode.Catalog)
            {
                var slot = new GameObject(item.Id, typeof(RectTransform), typeof(LayoutElement)); slot.transform.SetParent(row, false);
                slot.GetComponent<LayoutElement>().preferredWidth = 210;
                var button = ManagementUIFactory.Button("Select", slot.transform, "", ManagementUIFactory.PanelRaised, Color.white);
                button.onClick.AddListener(() => item.Select());
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var label = button.GetComponentInChildren<TextMeshProUGUI>(); label.fontSize = 16;
                _items.Add((item, button, label));
            }
            _empty = ManagementUIFactory.Text("Empty category", row, "No placeable content is available in this category yet.", 16, TextAlignmentOptions.Center, ManagementUIFactory.Muted);
            var status = ManagementUIFactory.Rect("Build feedback", _root.transform, Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero);
            status.offsetMin = new Vector2(374, 198); status.offsetMax = new Vector2(-24, 254);
            ManagementUIFactory.Background(status, ManagementUIFactory.Panel);
            _statusRoot = status.gameObject;
            _status = ManagementUIFactory.Text("Feedback", status, "", 16, TextAlignmentOptions.Center, Color.white);
            _status.margin = new Vector4(12, 5, 12, 5);
            Refresh();
        }
        private void CreateCircle()
        {
            _circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            _circleTexture.name = "Build tool circle"; _circleTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))));
            _circleTexture.SetPixels(pixels); _circleTexture.Apply();
            _circle = Sprite.Create(_circleTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        }
        private Button RoundButton(Transform parent, string label, Vector2 position, float diameter, UnityEngine.Events.UnityAction click)
        {
            var rect = ManagementUIFactory.Rect(label, parent, Vector2.zero, Vector2.zero, position, Vector2.one * diameter);
            var button = ManagementUIFactory.Button("Button", rect, label, ManagementUIFactory.PanelRaised, Color.white);
            var image = button.GetComponent<Image>(); image.sprite = _circle; image.alphaHitTestMinimumThreshold = .1f;
            var text = button.GetComponentInChildren<TextMeshProUGUI>(); text.raycastTarget = false; text.fontSize = 13;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(click); return button;
        }
        private void AddCategory(BuildCategory category, string label, Vector2 position)
        { _categoryButtons.Add(category, RoundButton(_categories.transform, label, position, 88, () => _mode.ChooseCategory(category))); }
        private void Update()
        {
            if (_mode == null) return;
            if (_mode.Category != _shownCategory || Time.unscaledTime >= _nextRefresh) Refresh();
            _categories.SetActive(_mode.IsOpen && !_mode.IsPlacing);
            _palette.SetActive(_mode.IsOpen && _mode.Category.HasValue && !_mode.IsPlacing);
            _statusRoot.SetActive(_mode.IsOpen || !string.IsNullOrEmpty(_mode.Message));
            var rect = (RectTransform)_statusRoot.transform;
            float bottom = _mode.IsPlacing || !_palette.activeSelf ? 26 : 198;
            rect.offsetMin = new Vector2(374, bottom); rect.offsetMax = new Vector2(-24, bottom + 56);
            _status.text = _mode.IsPlacing
                ? (_mode.IsFreeRotating ? Mathf.RoundToInt(_mode.RotationAngle) + "°\n" :
                    (_mode.Message ?? "Valid placement - click to build") + "\n") +
                    "Tap R / Shift+R: 90°   |   Hold R + move mouse: free rotate   |   Esc: back"
                : _mode.Message ?? "Choose an item, or press Esc to close Build.";
        }
        private void Refresh()
        {
            _nextRefresh = Time.unscaledTime + .2f; _shownCategory = _mode.Category;
            _build.GetComponent<Image>().color = _mode.IsOpen ? new Color(.35f, .26f, .10f) : ManagementUIFactory.PanelRaised;
            _title.text = _mode.Category == BuildCategory.LandscapingAndDecorations ? "Landscaping & Decorations" : _mode.Category?.ToString() ?? "Build";
            int count = 0;
            foreach (var entry in _items)
            {
                bool shown = entry.item.Category == _mode.Category;
                entry.button.transform.parent.gameObject.SetActive(shown);
                if (!shown) continue;
                count++;
                string reason = entry.item.UnavailableReason(); entry.button.interactable = reason == null;
                entry.text.text = entry.item.Name + "\n" + entry.item.Cost + "\n<size=12>" + (reason ?? "Available") + "</size>";
                entry.text.color = reason == null ? Color.white : ManagementUIFactory.Muted;
            }
            _empty.gameObject.SetActive(count == 0);
            foreach (var pair in _categoryButtons)
                pair.Value.GetComponent<Image>().color = pair.Key == _mode.Category ? new Color(.35f, .26f, .10f) : ManagementUIFactory.PanelRaised;
        }
        private void OnDisable() { if (_root != null) _root.SetActive(false); }
        private void OnEnable() { if (_root != null) _root.SetActive(true); }
        private void OnDestroy()
        { if (_root != null) Destroy(_root); if (_circle != null) Destroy(_circle); if (_circleTexture != null) Destroy(_circleTexture); }
    }
}
