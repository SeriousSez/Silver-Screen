using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.SimulationTime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public sealed class StudioOverviewUI : MonoBehaviour
    {
        private readonly HashSet<MovieProject> _observedProjects = new HashSet<MovieProject>();
        private SimulationTimeDriver _timeDriver;
        private StudioEconomyDriver _economyDriver;
        private StudioEmployeeManager _employeeManager;
        private MovieProductionDriver _productionDriver;
        private TextMeshProUGUI _dateText;
        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _employeesText;
        private TextMeshProUGUI _moviesText;
        private TextMeshProUGUI _theatersText;
        private TextMeshProUGUI _boxOfficeText;
        private TextMeshProUGUI _operatingCostText;
        private TextMeshProUGUI _studioNameText;
        private TextMeshProUGUI _renameStatusText;
        private TMP_InputField _renameInput;
        private StudioIdentity _studioIdentity;

        public void Initialize(
            SimulationTimeDriver timeDriver,
            StudioEconomyDriver economyDriver,
            StudioEmployeeManager employeeManager,
            MovieProductionDriver productionDriver,
            StudioIdentity studioIdentity)
        {
            _timeDriver = timeDriver;
            _economyDriver = economyDriver;
            _employeeManager = employeeManager;
            _productionDriver = productionDriver;
            _studioIdentity = studioIdentity;
            Build();
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (_timeDriver?.TimeService != null)
            {
                _timeDriver.TimeService.OnMinutePassed += HandleTimeChanged;
            }

            if (_economyDriver?.FinanceService != null)
            {
                _economyDriver.FinanceService.OnFinancesChanged += Refresh;
            }

            var service = _productionDriver?.ProductionService;
            if (service != null)
            {
                service.Slate.OnProjectAdded += HandleProjectAdded;
                foreach (var project in service.Slate.AllProjects) BindProject(project);
            }

            if (_productionDriver?.ReleaseService != null)
            {
                _productionDriver.ReleaseService.OnMovieReleaseUpdated += HandleMovieUpdated;
            }
            if (_studioIdentity != null)
            {
                _studioIdentity.OnNameChanged += HandleStudioNameChanged;
            }
        }

        private void OnDestroy()
        {
            if (_timeDriver?.TimeService != null)
            {
                _timeDriver.TimeService.OnMinutePassed -= HandleTimeChanged;
            }

            if (_economyDriver?.FinanceService != null)
            {
                _economyDriver.FinanceService.OnFinancesChanged -= Refresh;
            }

            var service = _productionDriver?.ProductionService;
            if (service != null)
            {
                service.Slate.OnProjectAdded -= HandleProjectAdded;
            }

            if (_productionDriver?.ReleaseService != null)
            {
                _productionDriver.ReleaseService.OnMovieReleaseUpdated -= HandleMovieUpdated;
            }
            if (_studioIdentity != null)
            {
                _studioIdentity.OnNameChanged -= HandleStudioNameChanged;
            }

            foreach (var project in _observedProjects)
            {
                project.OnStateChanged -= HandleMovieStateChanged;
            }
            _observedProjects.Clear();
        }

        private void Build()
        {
            ManagementUIFactory.Background(
                GetComponent<RectTransform>(),
                ManagementUIFactory.Panel);

            var titleRect = ManagementUIFactory.Rect(
                "Title",
                transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -56f),
                new Vector2(-80f, 76f));
            _studioNameText = ManagementUIFactory.Text(
                "Text",
                titleRect,
                _studioIdentity?.Name ?? string.Empty,
                34f,
                TextAlignmentOptions.Center,
                ManagementUIFactory.Gold);
            _studioNameText.fontStyle = FontStyles.Bold;

            var subtitleRect = ManagementUIFactory.Rect(
                "Subtitle",
                transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -102f),
                new Vector2(-80f, 34f));
            _dateText = ManagementUIFactory.Text(
                "Text",
                subtitleRect,
                string.Empty,
                17f,
                TextAlignmentOptions.Center,
                ManagementUIFactory.Muted);

            _cashText = CreateMetric("Cash", "CURRENT CASH", 0, 0);
            _employeesText = CreateMetric("Employees", "EMPLOYEES", 1, 0);
            _moviesText = CreateMetric("Movies", "MOVIES PRODUCED", 2, 0);
            _theatersText = CreateMetric("Theaters", "IN THEATERS", 0, 1);
            _boxOfficeText = CreateMetric("BoxOffice", "LIFETIME BOX OFFICE", 1, 1);
            _operatingCostText = CreateMetric("Costs", "MONTHLY OPERATING COST", 2, 1);
            BuildRenameControls();
        }

        private void BuildRenameControls()
        {
            var root = ManagementUIFactory.Rect(
                "RenameStudio",
                transform,
                new Vector2(0.22f, 0.06f),
                new Vector2(0.78f, 0.15f),
                Vector2.zero,
                Vector2.zero);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            _renameInput = ManagementUIFactory.InputField(
                "NameInput",
                root,
                "Studio name",
                StudioIdentity.MaximumNameLength);
            var inputLayout = _renameInput.gameObject.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1f;
            inputLayout.preferredWidth = 360f;

            var renameButton = ManagementUIFactory.Button(
                "RenameButton",
                root,
                "RENAME",
                ManagementUIFactory.Gold,
                new Color(0.08f, 0.07f, 0.05f));
            renameButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
            renameButton.onClick.AddListener(HandleRenameRequested);

            var statusRect = ManagementUIFactory.Rect(
                "RenameStatus",
                transform,
                new Vector2(0.22f, 0.015f),
                new Vector2(0.78f, 0.055f),
                Vector2.zero,
                Vector2.zero);
            _renameStatusText = ManagementUIFactory.Text(
                "Text",
                statusRect,
                string.Empty,
                12f,
                TextAlignmentOptions.Center,
                ManagementUIFactory.Muted);
        }

        private void HandleRenameRequested()
        {
            if (_studioIdentity == null || _renameInput == null) return;
            if (!_studioIdentity.TryRename(_renameInput.text))
            {
                _renameStatusText.text = $"Enter 1–{StudioIdentity.MaximumNameLength} characters.";
                return;
            }
            _renameInput.text = string.Empty;
            _renameStatusText.text = "Studio name updated.";
        }

        private void HandleStudioNameChanged(string studioName)
        {
            if (_studioNameText != null) _studioNameText.text = studioName;
        }
        private TextMeshProUGUI CreateMetric(string name, string label, int column, int row)
        {
            const float width = 0.27f;
            const float gap = 0.025f;
            float minX = 0.07f + column * (width + gap);
            float maxX = minX + width;
            float maxY = row == 0 ? 0.69f : 0.39f;
            float minY = maxY - 0.20f;

            var rect = ManagementUIFactory.Rect(
                name,
                transform,
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.Background(rect, ManagementUIFactory.PanelRaised);
            var labelRect = ManagementUIFactory.Rect(
                "Label",
                rect,
                new Vector2(0f, 0.64f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(labelRect, 12f, 0f, 12f, 6f);
            ManagementUIFactory.Text(
                "Text",
                labelRect,
                label,
                13f,
                TextAlignmentOptions.Center,
                ManagementUIFactory.Muted);

            var valueRect = ManagementUIFactory.Rect(
                "Value",
                rect,
                Vector2.zero,
                new Vector2(1f, 0.68f),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(valueRect, 10f, 8f, 10f, 0f);
            var value = ManagementUIFactory.Text(
                "Text",
                valueRect,
                "—",
                27f,
                TextAlignmentOptions.Center,
                Color.white);
            value.fontStyle = FontStyles.Bold;
            return value;
        }

        private void HandleTimeChanged(SimulationDateTime date)
        {
            if (_dateText != null) _dateText.text = date.ToFormattedString();
        }

        private void HandleProjectAdded(MovieProject project)
        {
            BindProject(project);
            Refresh();
        }

        private void BindProject(MovieProject project)
        {
            if (project == null || !_observedProjects.Add(project)) return;
            project.OnStateChanged += HandleMovieStateChanged;
        }

        private void HandleMovieUpdated(MovieProject project)
        {
            Refresh();
        }

        private void HandleMovieStateChanged(MovieProject project, MovieProductionState state)
        {
            Refresh();
        }

        public void Refresh()
        {
            var finances = _economyDriver?.FinanceService;
            var accounting = _economyDriver?.AccountingService;
            var projects = _productionDriver?.ProductionService?.Slate?.AllProjects;

            if (_dateText != null && _timeDriver?.TimeService != null)
            {
                _dateText.text = _timeDriver.TimeService.CurrentTime.ToFormattedString();
            }
            if (_cashText != null) _cashText.text = finances != null ? StudioFinanceUI.FormatMoney(finances.CurrentCash) : "—";
            if (_employeesText != null) _employeesText.text = (_employeeManager?.AllEmployees.Count ?? 0).ToString();
            if (_moviesText != null) _moviesText.text = (projects?.Count ?? 0).ToString();
            if (_operatingCostText != null)
            {
                _operatingCostText.text = accounting != null
                    ? StudioFinanceUI.FormatMoney(accounting.EstimatedMonthlyOperatingCosts)
                    : "—";
            }

            int inTheaters = 0;
            Money lifetimeBoxOffice = Money.Zero;
            if (projects != null)
            {
                foreach (var project in projects)
                {
                    if (project.TheatricalRun != null)
                    {
                        lifetimeBoxOffice += project.TheatricalRun.TotalBoxOfficeGross;
                        if (!project.TheatricalRun.IsCompleted) inTheaters++;
                    }
                }
            }

            if (_theatersText != null) _theatersText.text = inTheaters.ToString();
            if (_boxOfficeText != null) _boxOfficeText.text = StudioFinanceUI.FormatMoney(lifetimeBoxOffice);
        }
    }
}
