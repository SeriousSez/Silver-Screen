using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Movie;

namespace SilverScreen.Presentation.UI
{
    public class MovieCreationDialogUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MovieProductionDriver _productionDriver;
        [SerializeField] private GameObject _dialogRoot;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField _titleInput;
        [SerializeField] private TMP_Dropdown _genreDropdown;
        [SerializeField] private TMP_Dropdown _budgetDropdown;

        [Header("Role Inputs")]
        [SerializeField] private TMP_InputField _protagonistNameInput;
        [SerializeField] private TMP_InputField _supporting1NameInput;
        [SerializeField] private TMP_InputField _supporting2NameInput;
        [SerializeField] private GameObject _supporting1Container;
        [SerializeField] private GameObject _supporting2Container;

        [Header("Buttons")]
        [SerializeField] private Button _addSupportingButton;
        [SerializeField] private Button _removeSupportingButton;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _cancelButton;

        private int _supportingCount = 1; // Default 1 supporting character
        private readonly List<string> _genreIds = new List<string>();
        private readonly List<BudgetTier> _budgetTiers = new List<BudgetTier>();
        private bool _usableLayoutBuilt;
        private GameObject _runtimeSupporting1Row;
        private GameObject _runtimeSupporting2Row;

        public bool IsOpen => _dialogRoot != null && _dialogRoot.activeSelf;
        public event Action<MovieProject> OnMovieCreated;

        private void Awake()
        {
            EnsureUsableLayout();
            if (_createButton != null) _createButton.onClick.AddListener(HandleCreateClicked);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(Close);
            if (_addSupportingButton != null) _addSupportingButton.onClick.AddListener(AddSupportingRole);
            if (_removeSupportingButton != null) _removeSupportingButton.onClick.AddListener(RemoveSupportingRole);
        }

        public void Open()
        {
            EnsureUsableLayout();
            if (_productionDriver == null)
            {
                _productionDriver = FindAnyObjectByType<MovieProductionDriver>();
            }

            PopulateDropdowns();

            // Default values
            if (_titleInput != null) _titleInput.text = "The Last Picture";
            if (_protagonistNameInput != null) _protagonistNameInput.text = "Evelyn Hart";
            if (_supporting1NameInput != null) _supporting1NameInput.text = "Thomas Reed";
            if (_supporting2NameInput != null) _supporting2NameInput.text = "Margaret Cole";

            _supportingCount = 1;
            UpdateRoleContainersVisibility();

            if (_dialogRoot != null)
            {
                _dialogRoot.SetActive(true);
            }
        }

        public void Close()
        {
            if (_dialogRoot != null)
            {
                _dialogRoot.SetActive(false);
            }
        }

        private void PopulateDropdowns()
        {
            var service = _productionDriver?.ProductionService;
            if (service == null) return;

            // Genres
            if (_genreDropdown != null)
            {
                _genreDropdown.ClearOptions();
                _genreIds.Clear();
                var options = new List<string>();

                foreach (var g in service.AvailableGenres)
                {
                    _genreIds.Add(g.Id);
                    options.Add(g.DisplayName);
                }

                if (options.Count == 0)
                {
                    options.AddRange(new[] { "Drama", "Comedy", "Action", "Romance", "Thriller", "Horror" });
                    _genreIds.AddRange(new[] { "drama", "comedy", "action", "romance", "thriller", "horror" });
                }

                _genreDropdown.AddOptions(options);
                _genreDropdown.value = 0;
            }

            // Budgets
            if (_budgetDropdown != null)
            {
                _budgetDropdown.ClearOptions();
                _budgetTiers.Clear();
                var options = new List<string>();

                foreach (var b in service.AvailableBudgets)
                {
                    _budgetTiers.Add(b);
                    options.Add($"{b.DisplayName} (${b.Amount:N0})");
                }

                _budgetDropdown.AddOptions(options);
                _budgetDropdown.value = 1; // Default to Standard ($50,000)
            }
        }

        private void AddSupportingRole()
        {
            if (_supportingCount < 2)
            {
                _supportingCount++;
                UpdateRoleContainersVisibility();
            }
        }

        private void RemoveSupportingRole()
        {
            if (_supportingCount > 0)
            {
                _supportingCount--;
                UpdateRoleContainersVisibility();
            }
        }

        private void UpdateRoleContainersVisibility()
        {
            if (_supporting1Container != null) _supporting1Container.SetActive(_supportingCount >= 1);
            if (_supporting2Container != null) _supporting2Container.SetActive(_supportingCount >= 2);
            if (_runtimeSupporting1Row != null) _runtimeSupporting1Row.SetActive(_supportingCount >= 1);
            if (_runtimeSupporting2Row != null) _runtimeSupporting2Row.SetActive(_supportingCount >= 2);
            if (_addSupportingButton != null) _addSupportingButton.interactable = _supportingCount < 2;
            if (_removeSupportingButton != null) _removeSupportingButton.interactable = _supportingCount > 0;
        }


        private void EnsureUsableLayout()
        {
            if (_usableLayoutBuilt || _dialogRoot == null) return;

            var rootRect = _dialogRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            _usableLayoutBuilt = true;

            // The scene's original fixed offsets overlap at common Game view sizes.
            // Keep its wired controls, but move them into one predictable centered panel.
            for (int i = rootRect.childCount - 1; i >= 0; i--)
            {
                rootRect.GetChild(i).gameObject.SetActive(false);
            }

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var backdrop = CreateRect("MovieCreationBackdrop", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.018f, 0.02f, 0.84f);

            var panel = CreateRect("MovieCreationPanel", backdrop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 720f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.065f, 0.075f, 0.99f);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.65f, 0.16f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateLabel(panel, "NEW MOVIE PRODUCTION", new Vector2(0f, 318f), new Vector2(740f, 44f), 29f, TextAlignmentOptions.Center, true);
            CreateLabel(panel, "Develop a picture for your studio slate", new Vector2(0f, 285f), new Vector2(740f, 24f), 15f, TextAlignmentOptions.Center, false);
            CreateLabel(panel, "MOVIE TITLE", new Vector2(0f, 244f), new Vector2(740f, 20f), 14f, TextAlignmentOptions.MidlineLeft, false);
            MoveControl(_titleInput, panel, new Vector2(0f, 212f), new Vector2(740f, 42f));

            CreateLabel(panel, "GENRE", new Vector2(-190f, 157f), new Vector2(360f, 20f), 14f, TextAlignmentOptions.MidlineLeft, false);
            EnsureDropdownTemplate(_genreDropdown);
            MoveControl(_genreDropdown, panel, new Vector2(-190f, 125f), new Vector2(360f, 42f));
            CreateLabel(panel, "PRODUCTION BUDGET", new Vector2(190f, 157f), new Vector2(360f, 20f), 14f, TextAlignmentOptions.MidlineLeft, false);
            EnsureDropdownTemplate(_budgetDropdown);
            MoveControl(_budgetDropdown, panel, new Vector2(190f, 125f), new Vector2(360f, 42f));

            var divider = CreateRect("CastDivider", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 78f), new Vector2(740f, 30f));
            divider.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.13f, 0.15f, 1f);
            CreateLabel(divider, "CAST REQUIREMENTS", Vector2.zero, new Vector2(716f, 26f), 15f, TextAlignmentOptions.MidlineLeft, true);

            CreateLabel(panel, "PROTAGONIST / LEAD CHARACTER", new Vector2(0f, 38f), new Vector2(740f, 20f), 14f, TextAlignmentOptions.MidlineLeft, false);
            MoveControl(_protagonistNameInput, panel, new Vector2(0f, 6f), new Vector2(740f, 42f));

            _runtimeSupporting1Row = CreateFieldGroup(panel, "SUPPORTING CHARACTER 1", _supporting1NameInput, -64f);
            _runtimeSupporting2Row = CreateFieldGroup(panel, "SUPPORTING CHARACTER 2", _supporting2NameInput, -134f);

            MoveButton(_addSupportingButton, panel, new Vector2(-92f, -198f), new Vector2(170f, 38f), false);
            MoveButton(_removeSupportingButton, panel, new Vector2(92f, -198f), new Vector2(170f, 38f), false);
            MoveButton(_createButton, panel, new Vector2(-92f, -286f), new Vector2(240f, 46f), true);
            MoveButton(_cancelButton, panel, new Vector2(150f, -286f), new Vector2(170f, 46f), false);

            UpdateRoleContainersVisibility();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void CreateLabel(Transform parent, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, bool gold)
        {
            var rect = CreateRect(value.Replace(" ", string.Empty) + "Label", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = gold ? new Color(0.98f, 0.72f, 0.18f) : new Color(0.78f, 0.8f, 0.82f);
        }

        private static GameObject CreateFieldGroup(Transform parent, string label, TMP_InputField input, float y)
        {
            var group = CreateRect(label.Replace(" ", string.Empty) + "Row", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(740f, 64f));
            CreateLabel(group, label, new Vector2(0f, 21f), new Vector2(740f, 20f), 14f, TextAlignmentOptions.MidlineLeft, false);
            MoveControl(input, group, new Vector2(0f, -11f), new Vector2(740f, 42f));
            return group.gameObject;
        }


        private static void EnsureDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.template != null) return;

            var template = CreateRect("Template", dropdown.transform, Vector2.zero, Vector2.right, new Vector2(0f, -2f), new Vector2(0f, 180f));
            template.pivot = new Vector2(0.5f, 1f);
            template.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.085f, 0.1f, 1f);

            var scrollRect = template.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", template, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateRect("Content", viewport, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 34f));
            content.pivot = new Vector2(0.5f, 1f);
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = CreateRect("Item", content, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 34f));
            item.pivot = new Vector2(0.5f, 1f);
            var itemLayout = item.gameObject.AddComponent<LayoutElement>();
            itemLayout.preferredHeight = 34f;
            itemLayout.minHeight = 34f;

            var itemBackground = item.gameObject.AddComponent<Image>();
            itemBackground.color = new Color(0.1f, 0.12f, 0.14f, 1f);
            var toggle = item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = itemBackground;

            var checkmark = CreateRect("Item Checkmark", item, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, 0f), new Vector2(14f, 14f));
            var checkmarkImage = checkmark.gameObject.AddComponent<Image>();
            checkmarkImage.color = new Color(0.95f, 0.7f, 0.18f, 1f);
            toggle.graphic = checkmarkImage;

            var label = CreateRect("Item Label", item, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.offsetMin = new Vector2(34f, 2f);
            label.offsetMax = new Vector2(-10f, -2f);
            var labelText = label.gameObject.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 16f;
            labelText.color = new Color(0.94f, 0.94f, 0.92f);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            dropdown.template = template;
            dropdown.itemText = labelText;
            template.gameObject.SetActive(false);
        }
        private static void MoveControl(Component control, Transform parent, Vector2 position, Vector2 size)
        {
            if (control == null) return;

            var rect = control.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            control.gameObject.SetActive(true);

            var image = control.GetComponent<Image>();
            if (image != null) image.color = new Color(0.105f, 0.13f, 0.15f, 1f);

            foreach (var text in control.GetComponentsInChildren<TMP_Text>(true))
            {
                text.fontSize = Mathf.Max(text.fontSize, 16f);
                text.color = new Color(0.94f, 0.94f, 0.92f);
            }
        }

        private static void MoveButton(Button button, Transform parent, Vector2 position, Vector2 size, bool primary)
        {
            if (button == null) return;

            MoveControl(button, parent, position, size);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = primary ? new Color(0.92f, 0.65f, 0.12f, 1f) : new Color(0.14f, 0.17f, 0.2f, 1f);
            }

            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.fontStyle = FontStyles.Bold;
                text.color = primary ? new Color(0.08f, 0.07f, 0.05f) : Color.white;
            }
        }
        private void HandleCreateClicked()
        {
            var service = _productionDriver?.ProductionService;
            if (service == null) return;

            string title = _titleInput != null ? _titleInput.text : "Untitled";
            string genre = (_genreDropdown != null && _genreDropdown.value < _genreIds.Count) ? _genreIds[_genreDropdown.value] : "Drama";

            int budget = 50000;
            if (_budgetDropdown != null && _budgetDropdown.value < _budgetTiers.Count)
            {
                budget = _budgetTiers[_budgetDropdown.value].Amount;
            }

            string protName = _protagonistNameInput != null ? _protagonistNameInput.text : "Lead";
            var supportingNames = new List<string>();

            if (_supportingCount >= 1)
            {
                string s1 = (_supporting1NameInput != null && !string.IsNullOrWhiteSpace(_supporting1NameInput.text)) ? _supporting1NameInput.text : "Supporting Role 1";
                supportingNames.Add(s1);
            }
            if (_supportingCount >= 2)
            {
                string s2 = (_supporting2NameInput != null && !string.IsNullOrWhiteSpace(_supporting2NameInput.text)) ? _supporting2NameInput.text : "Supporting Role 2";
                supportingNames.Add(s2);
            }

            var result = service.CreateMovie(title, genre, budget, protName, supportingNames);
            if (!result.Succeeded)
            {
                Debug.LogWarning($"[MovieCreationDialogUI] {result.Message}");
                return;
            }

            Close();
            OnMovieCreated?.Invoke(result.Project);
        }

        public void SetupReferences(
            MovieProductionDriver driver,
            GameObject dialogRoot,
            TMP_InputField titleInput,
            TMP_Dropdown genreDropdown,
            TMP_Dropdown budgetDropdown,
            TMP_InputField protInput,
            TMP_InputField supp1Input,
            TMP_InputField supp2Input,
            GameObject supp1Container,
            GameObject supp2Container,
            Button addSuppBtn,
            Button removeSuppBtn,
            Button createBtn,
            Button cancelBtn)
        {
            _productionDriver = driver;
            _dialogRoot = dialogRoot;
            _titleInput = titleInput;
            _genreDropdown = genreDropdown;
            _budgetDropdown = budgetDropdown;
            _protagonistNameInput = protInput;
            _supporting1NameInput = supp1Input;
            _supporting2NameInput = supp2Input;
            _supporting1Container = supp1Container;
            _supporting2Container = supp2Container;
            _addSupportingButton = addSuppBtn;
            _removeSupportingButton = removeSuppBtn;
            _createButton = createBtn;
            _cancelButton = cancelBtn;

            if (_createButton != null)
            {
                _createButton.onClick.RemoveAllListeners();
                _createButton.onClick.AddListener(HandleCreateClicked);
            }
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(Close);
            }
            if (_addSupportingButton != null)
            {
                _addSupportingButton.onClick.RemoveAllListeners();
                _addSupportingButton.onClick.AddListener(AddSupportingRole);
            }
            if (_removeSupportingButton != null)
            {
                _removeSupportingButton.onClick.RemoveAllListeners();
                _removeSupportingButton.onClick.AddListener(RemoveSupportingRole);
            }
        }
    }
}
