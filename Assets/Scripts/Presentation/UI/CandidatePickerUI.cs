using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Movie;

namespace SilverScreen.Presentation.UI
{
    public class CandidatePickerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StudioEmployeeManager _employeeManager;
        [SerializeField] private MovieProductionDriver _productionDriver;
        [SerializeField] private GameObject _dialogRoot;
        [SerializeField] private TextMeshProUGUI _dialogTitleText;
        [SerializeField] private Transform _candidateListContainer;
        [SerializeField] private GameObject _candidateRowPrefab;
        [SerializeField] private Button _closeButton;

        private MovieProject _targetMovie;
        private MovieRole _targetRole;
        private bool _isSelectingDirector;

        public bool IsOpen => _dialogRoot != null && _dialogRoot.activeSelf;
        public event Action OnAssignmentCompleted;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        public void OpenForRole(MovieProject movie, MovieRole role)
        {
            _targetMovie = movie;
            _targetRole = role;
            _isSelectingDirector = false;

            if (_dialogTitleText != null)
            {
                _dialogTitleText.text = $"CAST ROLE: {role.CharacterName.ToUpper()} ({role.RoleType})";
            }

            PopulateCandidates();

            if (_dialogRoot != null) _dialogRoot.SetActive(true);
        }

        public void OpenForDirector(MovieProject movie)
        {
            _targetMovie = movie;
            _targetRole = null;
            _isSelectingDirector = true;

            if (_dialogTitleText != null)
            {
                _dialogTitleText.text = "ASSIGN DIRECTOR";
            }

            PopulateCandidates();

            if (_dialogRoot != null) _dialogRoot.SetActive(true);
        }

        public void Close()
        {
            if (_dialogRoot != null) _dialogRoot.SetActive(false);
            _targetMovie = null;
            _targetRole = null;
        }

        private void PopulateCandidates()
        {
            if (_employeeManager == null) _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            if (_productionDriver == null) _productionDriver = FindAnyObjectByType<MovieProductionDriver>();

            var service = _productionDriver?.ProductionService;
            if (_employeeManager == null || service == null || _targetMovie == null) return;

            // Clear existing rows
            if (_candidateListContainer != null)
            {
                foreach (Transform child in _candidateListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            var employees = _employeeManager.AllEmployees;

            foreach (var emp in employees)
            {
                if (_isSelectingDirector)
                {
                    if (emp.Role != EmployeeRole.Director) continue;
                    CreateCandidateRow(emp, () =>
                    {
                        service.AssignDirector(_targetMovie, emp);
                        Close();
                        OnAssignmentCompleted?.Invoke();
                    });
                }
                else
                {
                    if (emp.Role != EmployeeRole.Actor) continue;
                    if (!service.CanAssignActorToRole(_targetMovie, _targetRole, emp)) continue;

                    CreateCandidateRow(emp, () =>
                    {
                        service.AssignActorToRole(_targetMovie, _targetRole, emp);
                        Close();
                        OnAssignmentCompleted?.Invoke();
                    });
                }
            }
        }

        private void CreateCandidateRow(Employee employee, Action onAssign)
        {
            if (_candidateListContainer == null) return;

            GameObject rowObj;
            if (_candidateRowPrefab != null)
            {
                rowObj = Instantiate(_candidateRowPrefab, _candidateListContainer);
            }
            else
            {
                rowObj = BuildFallbackRow(_candidateListContainer);
            }

            // Bind values
            var nameText = rowObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var statsText = rowObj.transform.Find("Stats")?.GetComponent<TextMeshProUGUI>();
            var assignBtn = rowObj.transform.Find("AssignBtn")?.GetComponent<Button>();

            if (nameText != null) nameText.text = employee.Name;
            if (statsText != null)
            {
                statsText.text = $"Skill: <b>{employee.Skill}</b>   Salary: <b>${employee.Salary}</b>/d   Morale: <b>{employee.Morale}%</b>";
            }

            if (assignBtn != null)
            {
                assignBtn.onClick.RemoveAllListeners();
                assignBtn.onClick.AddListener(() => onAssign?.Invoke());
            }
        }

        private GameObject BuildFallbackRow(Transform parent)
        {
            var row = new GameObject("CandidateRow");
            row.transform.SetParent(parent, false);

            var rRect = row.AddComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(0f, 48f);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.20f);

            var nObj = new GameObject("Name");
            nObj.transform.SetParent(row.transform, false);
            var nRect = nObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 0f);
            nRect.anchorMax = new Vector2(0.40f, 1f);
            nRect.anchoredPosition = new Vector2(12f, 0f);
            nRect.sizeDelta = new Vector2(-12f, 0f);
            var nTMP = nObj.AddComponent<TextMeshProUGUI>();
            nTMP.fontSize = 15f;
            nTMP.fontStyle = FontStyles.Bold;
            nTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nTMP.color = Color.white;

            var sObj = new GameObject("Stats");
            sObj.transform.SetParent(row.transform, false);
            var sRect = sObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.40f, 0f);
            sRect.anchorMax = new Vector2(0.78f, 1f);
            sRect.sizeDelta = Vector2.zero;
            var sTMP = sObj.AddComponent<TextMeshProUGUI>();
            sTMP.fontSize = 13f;
            sTMP.alignment = TextAlignmentOptions.MidlineLeft;
            sTMP.color = new Color(0.75f, 0.80f, 0.85f);

            var bObj = new GameObject("AssignBtn");
            bObj.transform.SetParent(row.transform, false);
            var bRect = bObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.80f, 0.15f);
            bRect.anchorMax = new Vector2(0.98f, 0.85f);
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
            lTMP.text = "ASSIGN";
            lTMP.fontSize = 12f;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.color = new Color(0.08f, 0.10f, 0.12f);

            return row;
        }

        public void SetupReferences(
            StudioEmployeeManager empManager,
            MovieProductionDriver prodDriver,
            GameObject dialogRoot,
            TextMeshProUGUI titleText,
            Transform candidateContainer,
            Button closeBtn)
        {
            _employeeManager = empManager;
            _productionDriver = prodDriver;
            _dialogRoot = dialogRoot;
            _dialogTitleText = titleText;
            _candidateListContainer = candidateContainer;
            _closeButton = closeBtn;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
        }
    }
}
