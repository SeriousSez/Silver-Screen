using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Movie;

namespace SilverScreen.Presentation.UI
{
    public class MovieProjectUI : MonoBehaviour
    {
        [Header("Service Reference")]
        [SerializeField] private MovieProductionDriver _productionDriver;

        [Header("Sub Dialogs")]
        [SerializeField] private MovieCreationDialogUI _creationDialog;
        [SerializeField] private CandidatePickerUI _candidatePicker;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateRoot;
        [SerializeField] private Button _emptyCreateButton;

        [Header("Active State Card Elements")]
        [SerializeField] private GameObject _activeCardRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _genreBudgetText;
        [SerializeField] private TextMeshProUGUI _stateBadgeText;
        [SerializeField] private Image _stateBadgeBg;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Director Row")]
        [SerializeField] private TextMeshProUGUI _directorNameText;
        [SerializeField] private Button _assignDirectorButton;

        [Header("Cast Roles Container")]
        [SerializeField] private Transform _rolesContainer;
        [SerializeField] private GameObject _roleRowPrefab;

        [Header("Progress Elements")]
        [SerializeField] private Image _progressBarFill;
        [SerializeField] private TextMeshProUGUI _progressPercentText;

        [Header("Completion Button")]
        [SerializeField] private Button _newMovieButton;

        [Header("Notification Banner")]
        [SerializeField] private GameObject _notificationRoot;
        [SerializeField] private TextMeshProUGUI _notificationText;

        private float _notificationTimer;
        private MovieProject _observedMovie;
        private Vector2 _statusDefaultSize;
        private float _statusDefaultFontSize;
        private bool _statusDefaultsCaptured;
        private bool _viewVisible = true;
        private MovieProject _pinnedMovie;
        private GameObject _takeControlRoot;
        private GameObject _takeDecisionRoot;
        private Button _automaticModeButton;
        private Button _manualModeButton;
        private Button _keepTakeButton;
        private Button _shootAgainButton;
        private Transform _takeButtonsRoot;
        private string _selectedTakeId;

        public MovieProject DisplayedProject => _observedMovie;

        private void Start()
        {
            if (_productionDriver == null) _productionDriver = FindAnyObjectByType<MovieProductionDriver>(FindObjectsInactive.Include);
            if (_creationDialog == null) _creationDialog = FindAnyObjectByType<MovieCreationDialogUI>(FindObjectsInactive.Include);
            if (_candidatePicker == null) _candidatePicker = FindAnyObjectByType<CandidatePickerUI>(FindObjectsInactive.Include);

            if (_emptyCreateButton != null)
            {
                _emptyCreateButton.onClick.RemoveAllListeners();
                _emptyCreateButton.onClick.AddListener(OpenCreationDialog);
            }

            if (_assignDirectorButton != null)
            {
                _assignDirectorButton.onClick.RemoveAllListeners();
                _assignDirectorButton.onClick.AddListener(HandleAssignDirectorClicked);
            }

            if (_candidatePicker != null)
            {
                _candidatePicker.OnAssignmentCompleted -= HandleAssignmentCompleted;
                _candidatePicker.OnAssignmentCompleted += HandleAssignmentCompleted;
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                EnsureTakeControlUI();
                _productionDriver.ProductionService.OnActiveMovieChanged += HandleMovieChanged;
                _productionDriver.ProductionService.OnProductionNotification += HandleNotification;
                if (_productionDriver.ReleaseService != null)
                {
                    _productionDriver.ReleaseService.OnMovieReleaseUpdated += HandleReleaseUpdated;
                }
                HandleMovieChanged(_productionDriver.ProductionService.ActiveMovie);
            }

            if (_notificationRoot != null)
            {
                _notificationRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            ObserveMovie(null);

            if (_candidatePicker != null)
            {
                _candidatePicker.OnAssignmentCompleted -= HandleAssignmentCompleted;
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                _productionDriver.ProductionService.OnActiveMovieChanged -= HandleMovieChanged;
                _productionDriver.ProductionService.OnProductionNotification -= HandleNotification;
                if (_productionDriver.ReleaseService != null)
                {
                    _productionDriver.ReleaseService.OnMovieReleaseUpdated -= HandleReleaseUpdated;
                }
            }
        }

        private void Update()
        {
            if (_notificationRoot != null && _notificationRoot.activeSelf)
            {
                _notificationTimer -= UnityEngine.Time.unscaledDeltaTime;
                if (_notificationTimer <= 0f)
                {
                    _notificationRoot.SetActive(false);
                }
            }
        }

        private void HandleMovieChanged(MovieProject movie)
        {
            if (_pinnedMovie != null)
            {
                if (movie == _pinnedMovie) RefreshUI(_pinnedMovie);
                return;
            }

            ObserveMovie(movie);
            RefreshUI(movie);
        }

        private void HandleReleaseUpdated(MovieProject movie)
        {
            if (movie == _observedMovie)
            {
                RefreshUI(movie);
            }
        }

        private void HandleAssignmentCompleted()
        {
            RefreshUI(_observedMovie);
        }


        private void ObserveMovie(MovieProject movie)
        {
            if (_observedMovie == movie) return;

            if (_observedMovie != null)
            {
                _observedMovie.OnProgressChanged -= HandleProgressChanged;
            }

            _observedMovie = movie;

            if (_observedMovie != null)
            {
                _observedMovie.OnProgressChanged += HandleProgressChanged;
            }
        }

        private void HandleProgressChanged(MovieProject movie, float progress)
        {
            if (movie != _observedMovie) return;

            int pct = Mathf.RoundToInt(progress * 100f);
            if (_progressBarFill != null) _progressBarFill.fillAmount = progress;
            if (_progressPercentText != null) _progressPercentText.text = $"{pct}%";

            if (_statusText != null)
            {
                _statusText.text = movie.CurrentState == MovieProductionState.Filming
                    ? $"Filming — {pct}%"
                    : _productionDriver?.ProductionService?.StatusMessage;
            }
        }

        private void HandleNotification(string message)
        {
            if (_notificationRoot != null && _notificationText != null)
            {
                _notificationText.text = message;
                _notificationRoot.SetActive(true);
                _notificationTimer = 8f;
            }
        }

        public void OpenCreationDialog()
        {
            if (_creationDialog != null)
            {
                _creationDialog.Open();
            }
        }

        private void HandleReleaseClicked()
        {
            var movie = _observedMovie;
            MovieReleaseResult result = _productionDriver?.ReleaseService?.ReleaseMovie(movie);
            if (result == null || !result.Succeeded)
            {
                HandleNotification(result?.Message ?? "Movie release service is unavailable.");
                return;
            }

            RefreshUI(movie);
        }

        private void HandleAssignDirectorClicked()
        {
            var movie = _productionDriver?.ProductionService?.ActiveMovie;
            if (movie != null && _candidatePicker != null)
            {
                _candidatePicker.OpenForDirector(movie);
            }
        }

        public void RefreshUI(MovieProject movie)
        {
            if (movie == null)
            {
                if (_emptyStateRoot != null) _emptyStateRoot.SetActive(_viewVisible);
                if (_activeCardRoot != null) _activeCardRoot.SetActive(false);
                return;
            }

            if (_emptyStateRoot != null) _emptyStateRoot.SetActive(false);
            if (_activeCardRoot != null) _activeCardRoot.SetActive(_viewVisible);
            EnsureTakeControlUI();
            RefreshTakeControlUI(movie);

            if (_titleText != null) _titleText.text = movie.Title;
            if (_genreBudgetText != null) _genreBudgetText.text = $"{movie.GenreDisplayName}  •  ${movie.Budget:N0}";

            // Badge text & color
            if (_stateBadgeText != null)
            {
                _stateBadgeText.text = movie.CurrentState == MovieProductionState.Released
                    ? movie.TheatricalRun != null && movie.TheatricalRun.IsCompleted
                        ? "THEATRICAL RUN COMPLETE"
                        : "NOW PLAYING"
                    : FormatStateBadge(movie.CurrentState);
            }
            if (_stateBadgeBg != null) _stateBadgeBg.color = GetStateBadgeColor(movie.CurrentState);

            // Status message
            if (_statusText != null && _productionDriver != null)
            {
                CaptureStatusDefaults();
                if (movie.CurrentState == MovieProductionState.Completed && movie.ProductionResult != null)
                {
                    var result = movie.ProductionResult;
                    _statusText.text =
                        $"<b>Quality: {result.OverallQuality}/100</b>{Environment.NewLine}" +
                        $"Cast Performance — {result.CastPerformance}{Environment.NewLine}" +
                        $"Direction — {result.Direction}{Environment.NewLine}" +
                        $"Production Value — {result.ProductionValue}";
                    _statusText.fontSize = 14f;
                    _statusText.textWrappingMode = TextWrappingModes.Normal;
                    _statusText.rectTransform.sizeDelta = new Vector2(_statusDefaultSize.x, 94f);
                }
                else if (movie.CurrentState == MovieProductionState.Released && movie.TheatricalRun != null)
                {
                    var production = movie.ProductionResult;
                    var run = movie.TheatricalRun;
                    if (run.IsCompleted && movie.CommercialResult != null)
                    {
                        var result = movie.CommercialResult;
                        _statusText.text =
                            $"<b>Final Quality: {production?.OverallQuality ?? 0}/100</b>  •  Audience {result.AudienceReception}/100{Environment.NewLine}" +
                            $"Box Office Gross — {FormatMoney(result.TotalBoxOfficeGross)}{Environment.NewLine}" +
                            $"Studio Revenue — {FormatMoney(result.TotalStudioRevenue)}{Environment.NewLine}" +
                            $"Production Budget — {FormatMoney(result.ProductionBudget)}{Environment.NewLine}" +
                            $"<b>Profit / Loss — {FormatSignedMoney(result.CommercialProfitLoss)}</b>";
                    }
                    else
                    {
                        _statusText.text =
                            $"<b>Now Playing — Day {run.CurrentDay} of {TheatricalMarketConfiguration.TheatricalRunDays}</b>{Environment.NewLine}" +
                            $"Quality {production?.OverallQuality ?? 0}/100  •  Audience {run.AudienceReception}/100{Environment.NewLine}" +
                            $"Week {run.CurrentWeek} of 4 — {FormatMoney(run.CurrentWeekGross)}{Environment.NewLine}" +
                            $"Total Gross — {FormatMoney(run.TotalBoxOfficeGross)}{Environment.NewLine}" +
                            $"Studio Revenue — {FormatMoney(run.TotalStudioRevenue)}";
                    }

                    _statusText.fontSize = 13f;
                    _statusText.textWrappingMode = TextWrappingModes.Normal;
                    _statusText.rectTransform.sizeDelta = new Vector2(_statusDefaultSize.x, 122f);
                }
                else
                {
                    _statusText.text = _productionDriver.ProductionService.StatusMessage;
                    _statusText.fontSize = _statusDefaultFontSize;
                    _statusText.rectTransform.sizeDelta = _statusDefaultSize;
                }
            }

            // Director row
            if (_directorNameText != null)
            {
                if (movie.AssignedDirector != null)
                {
                    _directorNameText.text = $"Director: <b><color=#FFFFFF>{movie.AssignedDirector.Name}</color></b>";
                    if (_assignDirectorButton != null) _assignDirectorButton.gameObject.SetActive(false);
                }
                else
                {
                    _directorNameText.text = "Director: <color=#888888>(Unassigned)</color>";
                    if (_assignDirectorButton != null)
                    {
                        _assignDirectorButton.gameObject.SetActive(movie.CurrentState == MovieProductionState.Draft || movie.CurrentState == MovieProductionState.Casting);
                    }
                }
            }

            // Cast roles list
            PopulateRoleRows(movie);

            // Progress bar
            float displayedProgress = GetDisplayedProgress(movie);
            int pct = Mathf.RoundToInt(displayedProgress * 100f);
            if (_progressBarFill != null) _progressBarFill.fillAmount = displayedProgress;
            if (_progressPercentText != null) _progressPercentText.text = $"{pct}%";
            if (movie.CurrentState == MovieProductionState.Completed && movie.ProductionResult != null)
            {
                float qualityProgress = movie.ProductionResult.OverallQuality / 100f;
                if (_progressBarFill != null) _progressBarFill.fillAmount = qualityProgress;
                if (_progressPercentText != null)
                {
                    _progressPercentText.text = $"QUALITY {movie.ProductionResult.OverallQuality}/100";
                }
            }
            else if (movie.CurrentState == MovieProductionState.Released && movie.TheatricalRun != null)
            {
                float runProgress = movie.TheatricalRun.CurrentDay /
                    (float)TheatricalMarketConfiguration.TheatricalRunDays;
                if (_progressBarFill != null) _progressBarFill.fillAmount = runProgress;
                if (_progressPercentText != null)
                {
                    _progressPercentText.text = movie.TheatricalRun.IsCompleted
                        ? "RUN COMPLETE"
                        : $"WEEK {movie.TheatricalRun.CurrentWeek} OF 4";
                }
            }

            // Lifecycle action button
            if (_newMovieButton != null)
            {
                bool completed = movie.CurrentState == MovieProductionState.Completed;
                _newMovieButton.gameObject.SetActive(completed);
                _newMovieButton.onClick.RemoveAllListeners();
                if (completed) _newMovieButton.onClick.AddListener(HandleReleaseClicked);

                var label = _newMovieButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = "RELEASE MOVIE";
            }
        }

        private void CaptureStatusDefaults()
        {
            if (_statusDefaultsCaptured || _statusText == null) return;
            _statusDefaultSize = _statusText.rectTransform.sizeDelta;
            _statusDefaultFontSize = _statusText.fontSize;
            _statusDefaultsCaptured = true;
        }

        private void EnsureTakeControlUI()
        {
            if (_takeControlRoot != null || _activeCardRoot == null) return;

            var root = ManagementUIFactory.Rect(
                "TakeControl",
                _activeCardRoot.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 76f),
                new Vector2(-32f, 136f));
            ManagementUIFactory.Background(root, new Color(0.035f, 0.043f, 0.05f, 0.98f));
            _takeControlRoot = root.gameObject;

            var labelRect = ManagementUIFactory.Rect(
                "ModeLabel",
                root,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(72f, -22f),
                new Vector2(120f, 28f));
            ManagementUIFactory.Text(
                "Text",
                labelRect,
                "TAKE CONTROL",
                12f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Muted);

            _automaticModeButton = CreateTakeButton(
                root, "Automatic", "AUTOMATIC", new Vector2(196f, -22f), new Vector2(112f, 28f));
            _manualModeButton = CreateTakeButton(
                root, "Manual", "MANUAL", new Vector2(318f, -22f), new Vector2(112f, 28f));
            _automaticModeButton.onClick.AddListener(
                () => SetProductionControlMode(ProductionControlMode.Automatic));
            _manualModeButton.onClick.AddListener(
                () => SetProductionControlMode(ProductionControlMode.Manual));

            var decisionRect = ManagementUIFactory.Rect(
                "TakeDecision",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, -18f),
                new Vector2(-16f, -52f));
            _takeDecisionRoot = decisionRect.gameObject;
            var cutText = ManagementUIFactory.Text(
                "CutLabel",
                decisionRect,
                "CUT — CHOOSE A TAKE",
                14f,
                TextAlignmentOptions.TopLeft,
                ManagementUIFactory.Gold);
            cutText.raycastTarget = false;

            var takeButtonsRect = ManagementUIFactory.Rect(
                "Takes",
                decisionRect,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(132f, 22f),
                new Vector2(240f, 32f));
            _takeButtonsRoot = takeButtonsRect;
            _keepTakeButton = CreateTakeButton(
                decisionRect, "Keep", "KEEP SELECTED TAKE", new Vector2(330f, 22f), new Vector2(190f, 32f));
            _shootAgainButton = CreateTakeButton(
                decisionRect, "Again", "SHOOT AGAIN", new Vector2(440f, 22f), new Vector2(100f, 32f));
            _keepTakeButton.onClick.AddListener(KeepSelectedTake);
            _shootAgainButton.onClick.AddListener(ShootAgain);
        }

        private static Button CreateTakeButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size)
        {
            var rect = ManagementUIFactory.Rect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                position,
                size);
            return ManagementUIFactory.Button(
                "Button",
                rect,
                label,
                ManagementUIFactory.PanelRaised,
                Color.white);
        }

        private void RefreshTakeControlUI(MovieProject movie)
        {
            if (_takeControlRoot == null) return;

            bool show = movie != null &&
                movie.CurrentState != MovieProductionState.Completed &&
                movie.CurrentState != MovieProductionState.Released;
            _takeControlRoot.SetActive(show);
            if (!show) return;

            SetButtonSelected(
                _automaticModeButton,
                movie.ProductionControlMode == ProductionControlMode.Automatic);
            SetButtonSelected(
                _manualModeButton,
                movie.ProductionControlMode == ProductionControlMode.Manual);

            var service = _productionDriver?.ProductionService;
            bool awaiting = service != null &&
                movie == service.ActiveMovie &&
                service.CurrentProductionPhase == ProductionPhase.AwaitingTakeDecision;
            _takeDecisionRoot.SetActive(awaiting);
            if (!awaiting) return;

            PopulateCompletedTakeButtons(service.ActiveScene);
            _keepTakeButton.interactable =
                service.CanKeepTake && !string.IsNullOrWhiteSpace(_selectedTakeId);
            _shootAgainButton.interactable = service.CanShootAgain;
        }

        private void PopulateCompletedTakeButtons(MovieScene scene)
        {
            foreach (Transform child in _takeButtonsRoot)
                Destroy(child.gameObject);
            if (scene == null) return;

            MovieTake firstCompleted = null;
            foreach (var take in scene.Takes)
            {
                if (take.Status != MovieTakeStatus.Completed) continue;
                firstCompleted ??= take;
            }

            if (scene.GetTake(_selectedTakeId)?.Status != MovieTakeStatus.Completed)
                _selectedTakeId = firstCompleted?.Id;

            int index = 0;
            foreach (var take in scene.Takes)
            {
                if (take.Status != MovieTakeStatus.Completed) continue;
                var button = CreateTakeButton(
                    _takeButtonsRoot,
                    $"Take{take.TakeNumber}",
                    $"TAKE {take.TakeNumber}",
                    new Vector2(48f + index * 82f, 16f),
                    new Vector2(72f, 28f));
                string takeId = take.Id;
                button.onClick.AddListener(() => SelectTake(takeId));
                SetButtonSelected(button, takeId == _selectedTakeId);
                index++;
            }
        }

        private void SetProductionControlMode(ProductionControlMode mode)
        {
            var service = _productionDriver?.ProductionService;
            if (service?.SetProductionControlMode(_observedMovie, mode) == false)
                RefreshTakeControlUI(_observedMovie);
        }

        private void SelectTake(string takeId)
        {
            _selectedTakeId = takeId;
            RefreshTakeControlUI(_observedMovie);
        }

        private void KeepSelectedTake()
        {
            _productionDriver?.ProductionService?.KeepTake(_selectedTakeId);
        }

        private void ShootAgain()
        {
            _productionDriver?.ProductionService?.ShootAgain();
        }

        private static void SetButtonSelected(Button button, bool selected)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = selected ? ManagementUIFactory.Gold : ManagementUIFactory.PanelRaised;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.color = selected ? new Color(0.08f, 0.07f, 0.05f) : Color.white;
        }

        public void ShowProject(MovieProject movie)
        {
            _pinnedMovie = movie;
            ObserveMovie(movie);
            SetViewVisible(true);
            RefreshUI(movie);
        }

        public void ShowActiveProject()
        {
            _pinnedMovie = null;
            var movie = _productionDriver?.ProductionService?.ActiveMovie;
            ObserveMovie(movie);
            RefreshUI(movie);
        }

        public void SetViewVisible(bool visible)
        {
            _viewVisible = visible;
            if (_emptyStateRoot != null)
            {
                _emptyStateRoot.SetActive(visible && _observedMovie == null);
            }
            if (_activeCardRoot != null)
            {
                _activeCardRoot.SetActive(visible && _observedMovie != null);
            }
        }

        public void AttachToHost(Transform host)
        {
            if (host == null || _activeCardRoot == null) return;
            var rect = _activeCardRoot.GetComponent<RectTransform>();
            rect.SetParent(host, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -76f);
            rect.sizeDelta = new Vector2(560f, 580f);
        }


        private static float GetDisplayedProgress(MovieProject movie)
        {
            if (movie == null) return 0f;

            if (movie.CurrentState == MovieProductionState.Casting)
            {
                if (movie.Roles.Count == 0) return 0f;

                int elapsedMinutes = 0;
                foreach (var role in movie.Roles)
                {
                    elapsedMinutes += Mathf.Clamp(role.CastingMinutesElapsed, 0, 60);
                }

                return Mathf.Clamp01((float)elapsedMinutes / (movie.Roles.Count * 60f));
            }

            if (movie.CurrentState == MovieProductionState.ReadyToFilm ||
                movie.CurrentState == MovieProductionState.Completed)
            {
                return 1f;
            }

            return movie.ProductionProgress;
        }

        private static string FormatMoney(Domain.Finance.Money money)
        {
            return string.Format("${0:N0}", money.WholeDollars);
        }

        private static string FormatSignedMoney(Domain.Finance.Money money)
        {
            long dollars = money.WholeDollars;
            return dollars < 0
                ? string.Format("-${0:N0}", Math.Abs(dollars))
                : string.Format("${0:N0}", dollars);
        }

        private void PopulateRoleRows(MovieProject movie)
        {
            if (_rolesContainer == null) return;

            // Clear previous rows
            foreach (Transform child in _rolesContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var role in movie.Roles)
            {
                CreateRoleRow(movie, role);
            }
        }

        private void CreateRoleRow(MovieProject movie, MovieRole role)
        {
            GameObject rowObj;
            if (_roleRowPrefab != null)
            {
                rowObj = Instantiate(_roleRowPrefab, _rolesContainer);
            }
            else
            {
                rowObj = BuildFallbackRoleRow(_rolesContainer);
            }

            var charNameText = rowObj.transform.Find("CharName")?.GetComponent<TextMeshProUGUI>();
            var roleTypeText = rowObj.transform.Find("RoleType")?.GetComponent<TextMeshProUGUI>();
            var actorText = rowObj.transform.Find("Actor")?.GetComponent<TextMeshProUGUI>();
            var castBtn = rowObj.transform.Find("CastBtn")?.GetComponent<Button>();

            if (charNameText != null) charNameText.text = role.CharacterName;
            if (roleTypeText != null) roleTypeText.text = role.RoleType == MovieRoleType.Protagonist ? "Lead" : "Supporting";

            if (role.AssignedActor != null)
            {
                if (actorText != null)
                {
                    string statusSuffix = role.CastingCompleted ? " (Cast Complete)" : role.IsCastingActive ? " (Casting...)" : "";
                    actorText.text = $"<color=#FFFFFF>{role.AssignedActor.Name}</color><color=#E6B800>{statusSuffix}</color>";
                    actorText.gameObject.SetActive(true);
                }
                if (castBtn != null) castBtn.gameObject.SetActive(false);
            }
            else
            {
                if (actorText != null)
                {
                    actorText.text = "<color=#777777>Unassigned</color>";
                    actorText.gameObject.SetActive(true);
                }
                if (castBtn != null)
                {
                    bool canCast = movie.CurrentState == MovieProductionState.Draft || movie.CurrentState == MovieProductionState.Casting;
                    castBtn.gameObject.SetActive(canCast);
                    castBtn.onClick.RemoveAllListeners();
                    castBtn.onClick.AddListener(() =>
                    {
                        if (_candidatePicker != null)
                        {
                            _candidatePicker.OpenForRole(movie, role);
                        }
                    });
                }
            }
        }

        private GameObject BuildFallbackRoleRow(Transform parent)
        {
            var row = new GameObject("RoleRow");
            row.transform.SetParent(parent, false);

            var rRect = row.AddComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(0f, 26f);

            var cObj = new GameObject("CharName");
            cObj.transform.SetParent(row.transform, false);
            var cRect = cObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(0.40f, 1f);
            cRect.sizeDelta = Vector2.zero;
            var cTMP = cObj.AddComponent<TextMeshProUGUI>();
            cTMP.fontSize = 13f;
            cTMP.fontStyle = FontStyles.Bold;
            cTMP.alignment = TextAlignmentOptions.MidlineLeft;
            cTMP.color = Color.white;

            var tObj = new GameObject("RoleType");
            tObj.transform.SetParent(row.transform, false);
            var tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.40f, 0f);
            tRect.anchorMax = new Vector2(0.60f, 1f);
            tRect.sizeDelta = Vector2.zero;
            var tTMP = tObj.AddComponent<TextMeshProUGUI>();
            tTMP.fontSize = 11f;
            tTMP.alignment = TextAlignmentOptions.MidlineLeft;
            tTMP.color = new Color(0.70f, 0.75f, 0.80f);

            var aObj = new GameObject("Actor");
            aObj.transform.SetParent(row.transform, false);
            var aRect = aObj.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.60f, 0f);
            aRect.anchorMax = new Vector2(0.85f, 1f);
            aRect.sizeDelta = Vector2.zero;
            var aTMP = aObj.AddComponent<TextMeshProUGUI>();
            aTMP.fontSize = 12f;
            aTMP.alignment = TextAlignmentOptions.MidlineLeft;
            aTMP.color = Color.white;

            var bObj = new GameObject("CastBtn");
            bObj.transform.SetParent(row.transform, false);
            var bRect = bObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.85f, 0.1f);
            bRect.anchorMax = new Vector2(1f, 0.9f);
            bRect.sizeDelta = Vector2.zero;

            var bImg = bObj.AddComponent<Image>();
            bImg.color = new Color(0.92f, 0.72f, 0.20f);
            var btn = bObj.AddComponent<Button>();
            btn.targetGraphic = bImg;

            var lObj = new GameObject("Label");
            lObj.transform.SetParent(bObj.transform, false);
            var lRect = lObj.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.sizeDelta = Vector2.zero;
            var lTMP = lObj.AddComponent<TextMeshProUGUI>();
            lTMP.text = "CAST";
            lTMP.fontSize = 11f;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.color = new Color(0.08f, 0.10f, 0.12f);

            return row;
        }

        private string FormatStateBadge(MovieProductionState state)
        {
            return state switch
            {
                MovieProductionState.Draft => "DRAFT",
                MovieProductionState.Casting => "CASTING",
                MovieProductionState.ReadyToFilm => "READY TO FILM",
                MovieProductionState.Filming => "FILMING",
                MovieProductionState.Completed => "PRODUCTION COMPLETE",
                _ => state.ToString().ToUpper()
            };
        }

        private Color GetStateBadgeColor(MovieProductionState state)
        {
            return state switch
            {
                MovieProductionState.Draft => new Color(0.24f, 0.28f, 0.35f, 1f),
                MovieProductionState.Casting => new Color(0.78f, 0.48f, 0.26f, 1f), // Terracotta
                MovieProductionState.ReadyToFilm => new Color(0.20f, 0.55f, 0.75f, 1f), // Blue
                MovieProductionState.Filming => new Color(0.92f, 0.72f, 0.20f, 1f), // Gold
                MovieProductionState.Completed => new Color(0.24f, 0.70f, 0.44f, 1f), // Green
                MovieProductionState.Released => new Color(0.46f, 0.36f, 0.68f, 1f), // Premiere purple
                _ => new Color(0.3f, 0.3f, 0.3f, 1f)
            };
        }

        public void SetupReferences(
            MovieProductionDriver driver,
            MovieCreationDialogUI creationDialog,
            CandidatePickerUI candidatePicker,
            GameObject emptyRoot,
            Button emptyCreateBtn,
            GameObject activeCardRoot,
            TextMeshProUGUI titleText,
            TextMeshProUGUI genreBudgetText,
            TextMeshProUGUI stateBadgeText,
            Image stateBadgeBg,
            TextMeshProUGUI statusText,
            TextMeshProUGUI dirNameText,
            Button assignDirBtn,
            Transform rolesContainer,
            Image progressBarFill,
            TextMeshProUGUI progressPercentText,
            Button newMovieBtn,
            GameObject notificationRoot,
            TextMeshProUGUI notificationText)
        {
            _productionDriver = driver;
            _creationDialog = creationDialog;
            _candidatePicker = candidatePicker;
            _emptyStateRoot = emptyRoot;
            _emptyCreateButton = emptyCreateBtn;
            _activeCardRoot = activeCardRoot;
            _titleText = titleText;
            _genreBudgetText = genreBudgetText;
            _stateBadgeText = stateBadgeText;
            _stateBadgeBg = stateBadgeBg;
            _statusText = statusText;
            _directorNameText = dirNameText;
            _assignDirectorButton = assignDirBtn;
            _rolesContainer = rolesContainer;
            _progressBarFill = progressBarFill;
            _progressPercentText = progressPercentText;
            _newMovieButton = newMovieBtn;
            _notificationRoot = notificationRoot;
            _notificationText = notificationText;

            if (_emptyCreateButton != null)
            {
                _emptyCreateButton.onClick.RemoveAllListeners();
                _emptyCreateButton.onClick.AddListener(OpenCreationDialog);
            }

            if (_newMovieButton != null)
            {
                _newMovieButton.onClick.RemoveAllListeners();
            }

            if (_assignDirectorButton != null)
            {
                _assignDirectorButton.onClick.RemoveAllListeners();
                _assignDirectorButton.onClick.AddListener(HandleAssignDirectorClicked);
            }

            if (_candidatePicker != null)
            {
                _candidatePicker.OnAssignmentCompleted -= HandleAssignmentCompleted;
                _candidatePicker.OnAssignmentCompleted += HandleAssignmentCompleted;
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                HandleMovieChanged(_productionDriver.ProductionService.ActiveMovie);
            }
        }
    }
}
