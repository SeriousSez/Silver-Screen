using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.Movie;

namespace SilverScreen.Presentation.UI
{
    public class EmployeeInspectorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StudioSelectionController _selectionController;
        [SerializeField] private MovieProductionDriver _productionDriver;
        [SerializeField] private GameObject _panelRoot;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _roleBadgeText;
        [SerializeField] private TextMeshProUGUI _skillText;
        [SerializeField] private TextMeshProUGUI _moraleText;
        [SerializeField] private TextMeshProUGUI _salaryText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private TextMeshProUGUI _actionText;

        [Header("Context Action Button")]
        [SerializeField] private Button _contextActionButton;
        [SerializeField] private TextMeshProUGUI _contextActionButtonText;

        public GameObject PanelRoot => _panelRoot;
        public bool IsPanelVisible => _panelRoot != null && _panelRoot.activeSelf;

        private Employee _boundEmployee;

        private void Start()
        {
            if (_selectionController == null)
            {
                _selectionController = FindAnyObjectByType<StudioSelectionController>();
            }

            if (_productionDriver == null)
            {
                _productionDriver = FindAnyObjectByType<MovieProductionDriver>();
            }

            if (_selectionController != null)
            {
                _selectionController.OnSelectionChanged += HandleSelectionChanged;
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                _productionDriver.ProductionService.OnActiveMovieChanged += HandleMovieUpdated;
            }

            // Initially hidden until someone is selected
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_selectionController != null)
            {
                _selectionController.OnSelectionChanged -= HandleSelectionChanged;
            }

            if (_productionDriver != null && _productionDriver.ProductionService != null)
            {
                _productionDriver.ProductionService.OnActiveMovieChanged -= HandleMovieUpdated;
            }

            UnbindCurrentEmployee();
        }

        private void HandleMovieUpdated(MovieProject movie)
        {
            RefreshUI();
        }

        private void HandleSelectionChanged(EmployeeAgent agent)
        {
            UnbindCurrentEmployee();

            if (agent != null && agent.Employee != null)
            {
                _boundEmployee = agent.Employee;
                _boundEmployee.OnStateChanged += HandleEmployeeUpdated;
                _boundEmployee.OnDetailsChanged += HandleEmployeeUpdated;

                if (_panelRoot != null) _panelRoot.SetActive(true);
                RefreshUI();
            }
            else
            {
                if (_panelRoot != null) _panelRoot.SetActive(false);
            }
        }

        private void UnbindCurrentEmployee()
        {
            if (_boundEmployee != null)
            {
                _boundEmployee.OnStateChanged -= HandleEmployeeUpdated;
                _boundEmployee.OnDetailsChanged -= HandleEmployeeUpdated;
                _boundEmployee = null;
            }
        }

        private void HandleEmployeeUpdated(Employee emp)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_boundEmployee == null) return;

            if (_nameText != null) _nameText.text = _boundEmployee.Name;
            if (_roleBadgeText != null) _roleBadgeText.text = _boundEmployee.Role.ToString().ToUpper();
            if (_skillText != null) _skillText.text = $"{_boundEmployee.Skill} / 100";
            if (_moraleText != null) _moraleText.text = $"{_boundEmployee.Morale}%";
            if (_salaryText != null) _salaryText.text = "Salary: $" + _boundEmployee.Salary.ToString("N0") + "/month";
            if (_stateText != null) _stateText.text = _boundEmployee.CurrentState.ToString();

            if (_actionText != null)
            {
                string desc = _boundEmployee.CurrentIntent != null ? _boundEmployee.CurrentIntent.Description : "";
                _actionText.text = string.IsNullOrEmpty(desc) ? "Standing by" : desc;
            }

            RefreshActionButton();
        }

        private void RefreshActionButton()
        {
            if (_contextActionButton == null) return;

            var prodService = _productionDriver != null ? _productionDriver.ProductionService : null;
            var movie = prodService?.ActiveMovie;

            if (prodService == null || movie == null)
            {
                _contextActionButton.gameObject.SetActive(false);
                return;
            }

            if (_boundEmployee.Role == EmployeeRole.Actor)
            {
                _contextActionButton.gameObject.SetActive(true);
                if (movie.IsActorAssignedAnyRole(_boundEmployee))
                {
                    _contextActionButton.interactable = false;
                    string roleName = "Assigned";
                    foreach (var r in movie.Roles)
                    {
                        if (r.AssignedActor == _boundEmployee)
                        {
                            roleName = r.CharacterName;
                            break;
                        }
                    }
                    if (_contextActionButtonText != null) _contextActionButtonText.text = $"Role: {roleName}";
                }
                else
                {
                    MovieRole uncastRole = null;
                    foreach (var r in movie.Roles)
                    {
                        if (r.AssignedActor == null)
                        {
                            uncastRole = r;
                            break;
                        }
                    }

                    if (uncastRole != null && prodService.CanAssignActorToRole(movie, uncastRole, _boundEmployee))
                    {
                        _contextActionButton.interactable = true;
                        if (_contextActionButtonText != null) _contextActionButtonText.text = $"Cast as {uncastRole.CharacterName}";

                        _contextActionButton.onClick.RemoveAllListeners();
                        _contextActionButton.onClick.AddListener(() =>
                        {
                            prodService.AssignActorToRole(movie, uncastRole, _boundEmployee);
                            RefreshUI();
                        });
                    }
                    else
                    {
                        _contextActionButton.interactable = false;
                        if (_contextActionButtonText != null) _contextActionButtonText.text = "All Roles Cast";
                    }
                }
            }
            else if (_boundEmployee.Role == EmployeeRole.Director)
            {
                _contextActionButton.gameObject.SetActive(true);
                if (movie.AssignedDirector == _boundEmployee)
                {
                    _contextActionButton.interactable = false;
                    if (_contextActionButtonText != null) _contextActionButtonText.text = "Assigned as Director";
                }
                else if (prodService.CanAssignDirector(movie, _boundEmployee))
                {
                    _contextActionButton.interactable = true;
                    if (_contextActionButtonText != null) _contextActionButtonText.text = "Assign as Director";

                    _contextActionButton.onClick.RemoveAllListeners();
                    _contextActionButton.onClick.AddListener(() =>
                    {
                        prodService.AssignDirector(movie, _boundEmployee);
                        RefreshUI();
                    });
                }
                else
                {
                    _contextActionButton.interactable = false;
                    if (_contextActionButtonText != null) _contextActionButtonText.text = "Director Filled";
                }
            }
            else
            {
                _contextActionButton.gameObject.SetActive(false);
            }
        }

        public void SetupFields(
            StudioSelectionController selectionController,
            MovieProductionDriver productionDriver,
            GameObject panelRoot,
            TextMeshProUGUI nameText,
            TextMeshProUGUI roleBadgeText,
            TextMeshProUGUI skillText,
            TextMeshProUGUI moraleText,
            TextMeshProUGUI salaryText,
            TextMeshProUGUI stateText,
            TextMeshProUGUI actionText,
            Button contextActionButton,
            TextMeshProUGUI contextActionButtonText)
        {
            _selectionController = selectionController;
            _productionDriver = productionDriver;
            _panelRoot = panelRoot;
            _nameText = nameText;
            _roleBadgeText = roleBadgeText;
            _skillText = skillText;
            _moraleText = moraleText;
            _salaryText = salaryText;
            _stateText = stateText;
            _actionText = actionText;
            _contextActionButton = contextActionButton;
            _contextActionButtonText = contextActionButtonText;
        }
    }
}
