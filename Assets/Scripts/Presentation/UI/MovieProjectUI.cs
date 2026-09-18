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

        private void Start()
        {
            if (_productionDriver == null) _productionDriver = FindAnyObjectByType<MovieProductionDriver>(FindObjectsInactive.Include);
            if (_creationDialog == null) _creationDialog = FindAnyObjectByType<MovieCreationDialogUI>(FindObjectsInactive.Include);
            if (_candidatePicker == null) _candidatePicker = FindAnyObjectByType<CandidatePickerUI>(FindObjectsInactive.Include);

            if (_emptyCreateButton != null)
            {
                _emptyCreateButton.onClick.AddListener(OpenCreationDialog);
            }

            if (_newMovieButton != null)
            {
                _newMovieButton.onClick.AddListener(OpenCreationDialog);
            }

            if (_assignDirectorButton != null)
            {
                _assignDirectorButton.onClick.AddListener(HandleAssignDirectorClicked);
            }

            if (_candidatePicker != null)
            {
                _candidatePicker.OnAssignmentCompleted += () => RefreshUI(_productionDriver?.ProductionService?.ActiveMovie);
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                _productionDriver.ProductionService.OnActiveMovieChanged += HandleMovieChanged;
                _productionDriver.ProductionService.OnProductionNotification += HandleNotification;
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

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                _productionDriver.ProductionService.OnActiveMovieChanged -= HandleMovieChanged;
                _productionDriver.ProductionService.OnProductionNotification -= HandleNotification;
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
            ObserveMovie(movie);
            RefreshUI(movie);
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
                if (_emptyStateRoot != null) _emptyStateRoot.SetActive(true);
                if (_activeCardRoot != null) _activeCardRoot.SetActive(false);
                return;
            }

            if (_emptyStateRoot != null) _emptyStateRoot.SetActive(false);
            if (_activeCardRoot != null) _activeCardRoot.SetActive(true);

            if (_titleText != null) _titleText.text = movie.Title;
            if (_genreBudgetText != null) _genreBudgetText.text = $"{movie.GenreDisplayName}  •  ${movie.Budget:N0}";

            // Badge text & color
            if (_stateBadgeText != null) _stateBadgeText.text = FormatStateBadge(movie.CurrentState);
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

            // Completed button
            if (_newMovieButton != null)
            {
                _newMovieButton.gameObject.SetActive(movie.CurrentState == MovieProductionState.Completed);
            }
        }

        private void CaptureStatusDefaults()
        {
            if (_statusDefaultsCaptured || _statusText == null) return;
            _statusDefaultSize = _statusText.rectTransform.sizeDelta;
            _statusDefaultFontSize = _statusText.fontSize;
            _statusDefaultsCaptured = true;
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
                _newMovieButton.onClick.AddListener(OpenCreationDialog);
            }

            if (_assignDirectorButton != null)
            {
                _assignDirectorButton.onClick.RemoveAllListeners();
                _assignDirectorButton.onClick.AddListener(HandleAssignDirectorClicked);
            }

            if (_candidatePicker != null)
            {
                _candidatePicker.OnAssignmentCompleted += () => RefreshUI(_productionDriver?.ProductionService?.ActiveMovie);
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                HandleMovieChanged(_productionDriver.ProductionService.ActiveMovie);
            }
        }
    }
}
