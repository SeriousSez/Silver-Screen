using System;
using System.Collections.Generic;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Movie;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public sealed class ProductionSlateUI : MonoBehaviour
    {
        private readonly HashSet<MovieProject> _observedProjects = new HashSet<MovieProject>();
        private MovieProductionDriver _driver;
        private MovieCreationDialogUI _creationDialog;
        private RectTransform _content;
        private Button _newMovieButton;
        private TextMeshProUGUI _newMovieLabel;

        public event Action<MovieProject> OnProjectSelected;

        public void Initialize(MovieProductionDriver driver, MovieCreationDialogUI creationDialog)
        {
            _driver = driver;
            _creationDialog = creationDialog;
            Build();

            var service = _driver?.ProductionService;
            if (service != null)
            {
                service.Slate.OnProjectAdded += HandleProjectAdded;
                service.OnActiveMovieChanged += HandleActiveMovieChanged;
                foreach (var project in service.Slate.AllProjects)
                {
                    BindProject(project);
                }
            }

            if (_driver?.ReleaseService != null)
            {
                _driver.ReleaseService.OnMovieReleaseUpdated += HandleMovieUpdated;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            var service = _driver?.ProductionService;
            if (service != null)
            {
                service.Slate.OnProjectAdded -= HandleProjectAdded;
                service.OnActiveMovieChanged -= HandleActiveMovieChanged;
            }

            if (_driver?.ReleaseService != null)
            {
                _driver.ReleaseService.OnMovieReleaseUpdated -= HandleMovieUpdated;
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
                new Vector2(0f, 1f),
                new Vector2(190f, -38f),
                new Vector2(340f, 48f));
            var title = ManagementUIFactory.Text(
                "Text",
                titleRect,
                "PRODUCTION SLATE",
                28f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Gold);
            title.fontStyle = FontStyles.Bold;

            var newMovieRect = ManagementUIFactory.Rect(
                "NewMovie",
                transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-150f, -38f),
                new Vector2(260f, 44f));
            _newMovieButton = ManagementUIFactory.Button(
                "Button",
                newMovieRect,
                "+ NEW MOVIE",
                ManagementUIFactory.Gold,
                new Color(0.08f, 0.07f, 0.05f));
            _newMovieLabel = _newMovieButton.GetComponentInChildren<TextMeshProUGUI>();
            _newMovieButton.onClick.AddListener(() => _creationDialog?.Open());

            var scrollRect = ManagementUIFactory.Rect(
                "SlateScroll",
                transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, -28f),
                new Vector2(-36f, -110f));
            ManagementUIFactory.ScrollView("ScrollView", scrollRect, out _content);
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

        private void HandleActiveMovieChanged(MovieProject project)
        {
            RefreshNewMovieButton();
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
            if (_content == null) return;
            foreach (Transform child in _content)
            {
                Destroy(child.gameObject);
            }

            var inProduction = new List<MovieProject>();
            var inTheaters = new List<MovieProject>();
            var completed = new List<MovieProject>();
            var projects = _driver?.ProductionService?.Slate?.AllProjects;
            if (projects != null)
            {
                foreach (var project in projects)
                {
                    if (IsInProduction(project))
                    {
                        inProduction.Add(project);
                    }
                    else if (IsInTheaters(project))
                    {
                        inTheaters.Add(project);
                    }
                    else
                    {
                        completed.Add(project);
                    }
                }
            }

            CreateSection("IN PRODUCTION", inProduction);
            CreateSection("IN THEATERS", inTheaters);
            CreateSection("COMPLETED", completed);
            RefreshNewMovieButton();
        }

        private void CreateSection(string heading, List<MovieProject> projects)
        {
            var headingRect = ManagementUIFactory.Rect(
                heading.Replace(" ", string.Empty),
                _content,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, 32f));
            headingRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            var headingText = ManagementUIFactory.Text(
                "Label",
                headingRect,
                heading,
                16f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Gold);
            headingText.fontStyle = FontStyles.Bold;

            if (projects.Count == 0)
            {
                var emptyRect = ManagementUIFactory.Rect(
                    "Empty",
                    _content,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    new Vector2(0f, 38f));
                emptyRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
                ManagementUIFactory.Text(
                    "Text",
                    emptyRect,
                    "No projects",
                    14f,
                    TextAlignmentOptions.MidlineLeft,
                    ManagementUIFactory.Muted);
                return;
            }

            foreach (var project in projects)
            {
                CreateProjectRow(project);
            }
        }

        private void CreateProjectRow(MovieProject project)
        {
            var rowRect = ManagementUIFactory.Rect(
                "Project_" + project.Id,
                _content,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, 92f));
            rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
            var image = ManagementUIFactory.Background(rowRect, ManagementUIFactory.PanelRaised);
            var button = rowRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnProjectSelected?.Invoke(project));

            var titleRect = ManagementUIFactory.Rect(
                "Movie",
                rowRect,
                new Vector2(0f, 0f),
                new Vector2(0.42f, 1f),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(titleRect, 18f, 8f, 6f, 8f);
            var title = ManagementUIFactory.Text(
                "Title",
                titleRect,
                $"<b>{project.Title}</b>\n<color=#AEB7C0>{project.GenreDisplayName}</color>",
                17f,
                TextAlignmentOptions.MidlineLeft,
                Color.white);

            var statusRect = ManagementUIFactory.Rect(
                "Status",
                rowRect,
                new Vector2(0.42f, 0f),
                new Vector2(0.72f, 1f),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(statusRect, 6f, 8f, 6f, 8f);
            ManagementUIFactory.Text(
                "Text",
                statusRect,
                FormatState(project),
                15f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Muted);

            var resultsRect = ManagementUIFactory.Rect(
                "Results",
                rowRect,
                new Vector2(0.72f, 0f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(resultsRect, 6f, 8f, 18f, 8f);
            ManagementUIFactory.Text(
                "Text",
                resultsRect,
                FormatResults(project),
                14f,
                TextAlignmentOptions.MidlineRight,
                Color.white);
        }

        private void RefreshNewMovieButton()
        {
            if (_newMovieButton == null) return;
            MovieProject active = _driver?.ProductionService?.ActiveMovie;
            bool blocked = active != null && IsInProduction(active);
            _newMovieButton.interactable = !blocked;
            if (_newMovieLabel != null)
            {
                _newMovieLabel.text = blocked
                    ? "PRODUCTION ALREADY IN PROGRESS"
                    : "+ NEW MOVIE";
                _newMovieLabel.fontSize = blocked ? 12f : 15f;
            }
        }

        private static bool IsInProduction(MovieProject project)
        {
            return project != null &&
                   (project.CurrentState == MovieProductionState.Draft ||
                    project.CurrentState == MovieProductionState.Casting ||
                    project.CurrentState == MovieProductionState.ReadyToFilm ||
                    project.CurrentState == MovieProductionState.Filming);
        }

        private static bool IsInTheaters(MovieProject project)
        {
            return project?.CurrentState == MovieProductionState.Released &&
                   project.TheatricalRun != null &&
                   !project.TheatricalRun.IsCompleted;
        }

        private static string FormatState(MovieProject project)
        {
            if (IsInTheaters(project))
            {
                return $"<color=#F2B32E>Now Playing — Week {project.TheatricalRun.CurrentWeek}</color>";
            }

            if (project.CurrentState == MovieProductionState.Completed)
            {
                return "Completed — Awaiting Release";
            }

            if (project.TheatricalRun != null && project.TheatricalRun.IsCompleted)
            {
                return "Theatrical Run Complete";
            }

            return project.CurrentState.ToString();
        }

        private static string FormatResults(MovieProject project)
        {
            string budget = $"Budget  ${project.Budget:N0}";
            string quality = project.ProductionResult != null
                ? $"\nQuality  {project.ProductionResult.OverallQuality}"
                : string.Empty;
            string boxOffice = project.TheatricalRun != null
                ? $"\nBox Office  ${project.TheatricalRun.TotalBoxOfficeGross.WholeDollars:N0}"
                : string.Empty;
            return budget + quality + boxOffice;
        }
    }
}
