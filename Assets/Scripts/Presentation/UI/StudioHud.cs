using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SilverScreen.Presentation.Writing;

namespace SilverScreen.Presentation.UI
{
    public sealed class StudioHud : MonoBehaviour
    {
        private SimulationTimeDriver _timeDriver;
        private StudioEconomyDriver _economyDriver;
        private StudioEmployeeManager _employeeManager;
        private MovieProductionDriver _productionDriver;
        private StudioSelectionController _selectionController;
        private StudioFinanceUI _financeUI;
        private MovieProjectUI _movieProjectUI;
        private MovieCreationDialogUI _creationDialog;
        private SimulationClockUI _legacyClockUI;

        private ManagementNavigation _navigation;
        private ProductionSlateUI _productionSlate;
        private StaffPanelUI _staffPanel;
        private StudioOverviewUI _studioOverview;
        private GameObject _productionsRoot;
        private GameObject _staffRoot;
        private GameObject _studioRoot;
        private GameObject _detailRoot;
        private GameObject _screenplaysRoot;
        private ScreenplayWritingUI _screenplayWritingUI;
        private ScreenplayWritingDriver _writingDriver;
        private TextMeshProUGUI _dateText;
        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _studioNameText;
        private StudioIdentity _studioIdentity;
        private readonly Dictionary<SimulationSpeed, Button> _speedButtons =
            new Dictionary<SimulationSpeed, Button>();
        private ManagementPanel _activePanel;
        private bool _changingPanel;

        public void Initialize(StudioIdentity studioIdentity)
        {
            if (ReferenceEquals(_studioIdentity, studioIdentity)) return;
            if (_studioIdentity != null) _studioIdentity.OnNameChanged -= HandleStudioNameChanged;
            _studioIdentity = studioIdentity;
            if (_studioIdentity != null)
            {
                _studioIdentity.OnNameChanged += HandleStudioNameChanged;
                HandleStudioNameChanged(_studioIdentity.Name);
            }
        }
        private void Start()
        {
            ResolveReferences();
            if (_timeDriver == null ||
                _economyDriver == null ||
                _employeeManager == null ||
                _productionDriver == null)
            {
                Debug.LogWarning("[StudioHud] Required studio services were not found.");
                return;
            }

            Build();
            Bind();
            SetPanel(ManagementPanel.Productions);
        }

        private void OnDestroy()
        {
            if (_timeDriver?.TimeService != null)
            {
                _timeDriver.TimeService.OnMinutePassed -= HandleMinutePassed;
                _timeDriver.TimeService.OnSpeedChanged -= HandleSpeedChanged;
            }
            if (_economyDriver?.FinanceService != null)
            {
                _economyDriver.FinanceService.OnFinancesChanged -= RefreshCash;
            }
            if (_financeUI != null)
            {
                _financeUI.OnPanelVisibilityChanged -= HandleFinanceVisibilityChanged;
            }
            if (_navigation != null)
            {
                _navigation.OnPanelRequested -= HandlePanelRequested;
            }
            if (_productionSlate != null)
            {
                _productionSlate.OnProjectSelected -= ShowMovieDetails;
            }
            if (_staffPanel != null)
            {
                _staffPanel.OnEmployeeSelected -= HandleEmployeeSelected;
            }
            if (_studioIdentity != null)
            {
                _studioIdentity.OnNameChanged -= HandleStudioNameChanged;
            }
        }

        private void ResolveReferences()
        {
            _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
            _economyDriver = FindAnyObjectByType<StudioEconomyDriver>();
            _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            _productionDriver = FindAnyObjectByType<MovieProductionDriver>();
            _selectionController = FindAnyObjectByType<StudioSelectionController>();
            _financeUI = FindAnyObjectByType<StudioFinanceUI>(FindObjectsInactive.Include);
            _movieProjectUI = FindAnyObjectByType<MovieProjectUI>(FindObjectsInactive.Include);
            _creationDialog = FindAnyObjectByType<MovieCreationDialogUI>(FindObjectsInactive.Include);
            _legacyClockUI = FindAnyObjectByType<SimulationClockUI>(FindObjectsInactive.Include);
            _writingDriver = FindAnyObjectByType<ScreenplayWritingDriver>();
        }

        private void Build()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var hudRoot = ManagementUIFactory.Stretch("StudioManagementHud", canvas.transform);
            hudRoot.SetAsLastSibling();

            BuildTopBar(hudRoot);
            BuildNavigation(hudRoot);
            BuildPanels(hudRoot);

            _financeUI?.SetStandaloneCashButtonVisible(false);
            _legacyClockUI?.SetDisplayVisible(false);
            _movieProjectUI?.SetViewVisible(false);
        }

        private void BuildTopBar(Transform parent)
        {
            var topBar = ManagementUIFactory.Rect(
                "TopBar",
                parent,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -34f),
                new Vector2(0f, 68f));
            ManagementUIFactory.Background(topBar, new Color(0.04f, 0.048f, 0.057f, 0.99f));

            var studioRect = ManagementUIFactory.Rect(
                "StudioName",
                topBar,
                new Vector2(0f, 0f),
                new Vector2(0.25f, 1f),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(studioRect, 22f, 0f, 8f, 0f);
            _studioNameText = ManagementUIFactory.Text(
                "Text",
                studioRect,
                _studioIdentity?.Name ?? string.Empty,
                18f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Gold);
            _studioNameText.fontStyle = FontStyles.Bold;

            var timeRect = ManagementUIFactory.Rect(
                "Time",
                topBar,
                new Vector2(0.25f, 0f),
                new Vector2(0.58f, 1f),
                Vector2.zero,
                Vector2.zero);
            _dateText = ManagementUIFactory.Text(
                "Text",
                timeRect,
                string.Empty,
                19f,
                TextAlignmentOptions.Center,
                Color.white);
            _dateText.fontStyle = FontStyles.Bold;

            var speedRect = ManagementUIFactory.Rect(
                "SpeedControls",
                topBar,
                new Vector2(0.58f, 0f),
                new Vector2(0.80f, 1f),
                Vector2.zero,
                Vector2.zero);
            var speedLayout = speedRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            speedLayout.padding = new RectOffset(8, 8, 12, 12);
            speedLayout.spacing = 6f;
            speedLayout.childControlHeight = true;
            speedLayout.childControlWidth = true;
            speedLayout.childForceExpandHeight = true;
            speedLayout.childForceExpandWidth = true;
            CreateSpeedButton(speedRect, SimulationSpeed.Paused, "Ⅱ");
            CreateSpeedButton(speedRect, SimulationSpeed.Normal, "1x");
            CreateSpeedButton(speedRect, SimulationSpeed.Fast, "2x");
            CreateSpeedButton(speedRect, SimulationSpeed.VeryFast, "3x");

            var cashRect = ManagementUIFactory.Rect(
                "Cash",
                topBar,
                new Vector2(0.80f, 0f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            var cashButton = ManagementUIFactory.Button(
                "Button",
                cashRect,
                string.Empty,
                new Color(0.075f, 0.09f, 0.105f, 1f),
                ManagementUIFactory.Gold);
            _cashText = cashButton.GetComponentInChildren<TextMeshProUGUI>();
            _cashText.fontSize = 18f;
            cashButton.onClick.AddListener(() => HandlePanelRequested(ManagementPanel.Finances));
        }

        private void CreateSpeedButton(Transform parent, SimulationSpeed speed, string label)
        {
            var button = ManagementUIFactory.Button(
                speed + "Button",
                parent,
                label,
                ManagementUIFactory.PanelRaised,
                Color.white);
            button.onClick.AddListener(() => _timeDriver?.TimeService?.SetSpeed(speed));
            _speedButtons.Add(speed, button);
        }

        private void BuildNavigation(Transform parent)
        {
            var navRect = ManagementUIFactory.Rect(
                "PrimaryNavigation",
                parent,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(104f, -46f),
                new Vector2(192f, -92f));
            navRect.pivot = new Vector2(0.5f, 0.5f);
            ManagementUIFactory.Background(navRect, new Color(0.04f, 0.048f, 0.057f, 0.96f));
            _navigation = navRect.gameObject.AddComponent<ManagementNavigation>();
            _navigation.Build();
        }

        private void BuildPanels(Transform parent)
        {
            var content = ManagementUIFactory.Rect(
                "ManagementContent",
                parent,
                new Vector2(0f, 0f),
                Vector2.one,
                new Vector2(102f, -46f),
                new Vector2(-224f, -92f));
            ManagementUIFactory.SetOffsets(content, 224f, 18f, 18f, 92f);

            _studioRoot = CreatePanelRoot("StudioPanel", content);
            _studioOverview = _studioRoot.AddComponent<StudioOverviewUI>();
            _studioOverview.Initialize(_timeDriver, _economyDriver, _employeeManager, _productionDriver, _studioIdentity);

            _productionsRoot = CreatePanelRoot("ProductionsPanel", content);
            _productionSlate = _productionsRoot.AddComponent<ProductionSlateUI>();
            _productionSlate.Initialize(_productionDriver, _creationDialog);

            _staffRoot = CreatePanelRoot("StaffPanel", content);
            _staffPanel = _staffRoot.AddComponent<StaffPanelUI>();
            _staffPanel.Initialize(_employeeManager, _selectionController);

            _screenplaysRoot = CreatePanelRoot("ScreenplaysPanel", content);
            _screenplayWritingUI = _screenplaysRoot.AddComponent<ScreenplayWritingUI>();
            _screenplayWritingUI.Initialize(_employeeManager, _writingDriver);

            _detailRoot = CreatePanelRoot("MovieDetailPanel", content);
            ManagementUIFactory.Background(
                _detailRoot.GetComponent<RectTransform>(),
                ManagementUIFactory.Panel);
            var backRect = ManagementUIFactory.Rect(
                "Back",
                _detailRoot.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-100f, -32f),
                new Vector2(160f, 40f));
            var backButton = ManagementUIFactory.Button(
                "Button",
                backRect,
                "← BACK TO SLATE",
                ManagementUIFactory.PanelRaised,
                Color.white);
            backButton.onClick.AddListener(CloseMovieDetails);

            _movieProjectUI?.AttachToHost(_detailRoot.transform);
            _studioRoot.SetActive(false);
            _productionsRoot.SetActive(false);
            _staffRoot.SetActive(false);
            _screenplaysRoot.SetActive(false);
            _detailRoot.SetActive(false);
        }

        private static GameObject CreatePanelRoot(string name, Transform parent)
        {
            return ManagementUIFactory.Stretch(name, parent).gameObject;
        }

        private void Bind()
        {
            _navigation.OnPanelRequested += HandlePanelRequested;
            _productionSlate.OnProjectSelected += ShowMovieDetails;
            _staffPanel.OnEmployeeSelected += HandleEmployeeSelected;
            if (_financeUI != null)
            {
                _financeUI.OnPanelVisibilityChanged += HandleFinanceVisibilityChanged;
            }
            _timeDriver.TimeService.OnMinutePassed += HandleMinutePassed;
            _timeDriver.TimeService.OnSpeedChanged += HandleSpeedChanged;
            _economyDriver.FinanceService.OnFinancesChanged += RefreshCash;

            HandleMinutePassed(_timeDriver.TimeService.CurrentTime);
            HandleSpeedChanged(_timeDriver.TimeService.CurrentSpeed);
            RefreshCash();
        }

        private void HandlePanelRequested(ManagementPanel requested)
        {
            if (_detailRoot != null && _detailRoot.activeSelf && requested == ManagementPanel.Productions)
            {
                CloseMovieDetails();
                return;
            }

            SetPanel(_activePanel == requested ? ManagementPanel.None : requested);
        }

        private void SetPanel(ManagementPanel panel)
        {
            if (_changingPanel) return;
            _changingPanel = true;
            _activePanel = panel;

            if (_detailRoot != null) _detailRoot.SetActive(false);
            _movieProjectUI?.SetViewVisible(false);
            if (_studioRoot != null) _studioRoot.SetActive(panel == ManagementPanel.Studio);
            if (_productionsRoot != null) _productionsRoot.SetActive(panel == ManagementPanel.Productions);
            if (_staffRoot != null) _staffRoot.SetActive(panel == ManagementPanel.Staff);
            if (_screenplaysRoot != null) _screenplaysRoot.SetActive(panel == ManagementPanel.Screenplays);
            _financeUI?.SetPanelVisible(panel == ManagementPanel.Finances);
            _navigation?.SetActive(panel);

            if (panel == ManagementPanel.Studio) _studioOverview?.Refresh();
            if (panel == ManagementPanel.Productions) _productionSlate?.Refresh();
            if (panel == ManagementPanel.Staff) _staffPanel?.Refresh();
            if (panel == ManagementPanel.Screenplays) _screenplayWritingUI?.Refresh();
            _changingPanel = false;
        }

        private void ShowMovieDetails(MovieProject movie)
        {
            if (movie == null) return;
            _activePanel = ManagementPanel.Productions;
            _productionsRoot.SetActive(false);
            _studioRoot.SetActive(false);
            _staffRoot.SetActive(false);
            _screenplaysRoot.SetActive(false);
            _financeUI?.SetPanelVisible(false);
            _detailRoot.SetActive(true);
            _detailRoot.transform.SetAsLastSibling();
            _movieProjectUI?.ShowProject(movie);
            _navigation.SetActive(ManagementPanel.Productions);
        }

        private void CloseMovieDetails()
        {
            _movieProjectUI?.SetViewVisible(false);
            _movieProjectUI?.ShowActiveProject();
            _detailRoot.SetActive(false);
            SetPanel(ManagementPanel.Productions);
        }

        private void HandleFinanceVisibilityChanged(bool visible)
        {
            if (_changingPanel) return;
            if (visible)
            {
                SetPanel(ManagementPanel.Finances);
            }
            else if (_activePanel == ManagementPanel.Finances)
            {
                SetPanel(ManagementPanel.None);
            }
        }

        private void HandleEmployeeSelected(SilverScreen.Domain.Employee employee)
        {
            SetPanel(ManagementPanel.None);
        }

        private void HandleMinutePassed(SimulationDateTime date)
        {
            if (_dateText != null) _dateText.text = date.ToFormattedString();
        }

        private void HandleSpeedChanged(SimulationSpeed speed)
        {
            foreach (var pair in _speedButtons)
            {
                bool active = pair.Key == speed;
                var image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? ManagementUIFactory.Gold : ManagementUIFactory.PanelRaised;
                }
                var text = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = active ? new Color(0.08f, 0.07f, 0.05f) : Color.white;
                }
            }
        }

        private void RefreshCash()
        {
            if (_cashText != null && _economyDriver?.FinanceService != null)
            {
                _cashText.text = "CASH  " +
                    StudioFinanceUI.FormatMoney(_economyDriver.FinanceService.CurrentCash);
            }
        }
        private void HandleStudioNameChanged(string studioName)
        {
            if (_studioNameText != null) _studioNameText.text = studioName;
        }
    }
}
