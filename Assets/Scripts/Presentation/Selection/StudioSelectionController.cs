using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Buildings;

namespace SilverScreen.Presentation.Selection
{
    public class StudioSelectionController : MonoBehaviour
    {
        [Header("Layer Masks")]
        [SerializeField] private LayerMask _selectableMask = ~0;

        [Header("Drag Threshold")]
        [SerializeField] private float _maxDragDistance = 6f;

        public EmployeeAgent SelectedAgent { get; private set; }
        public CandidateAgent SelectedCandidate { get; private set; }
        public StageSchoolView SelectedStageSchool { get; private set; }

        public event Action<EmployeeAgent> OnSelectionChanged;
        public event Action<CandidateAgent> OnCandidateSelectionChanged;
        public event Action<StageSchoolView> OnStageSchoolSelectionChanged;

        private Vector2 _mouseDownPosition;
        private bool _isMouseDown;
        private UnityEngine.Camera _mainCamera;
        private StudioBuildingView _lastClickedBuilding;
        private float _lastBuildingClickTime = float.NegativeInfinity;
        private const float DoubleClickSeconds = 0.35f;

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
                if (_mainCamera == null) return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                _mouseDownPosition = mouse.position.ReadValue();
                _isMouseDown = true;
            }

            if (!_isMouseDown || !mouse.leftButton.wasReleasedThisFrame) return;

            _isMouseDown = false;
            Vector2 mouseUpPosition = mouse.position.ReadValue();
            if (Vector2.Distance(_mouseDownPosition, mouseUpPosition) > _maxDragDistance)
            {
                return;
            }

            Ray ray = _mainCamera.ScreenPointToRay(mouseUpPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f, _selectableMask))
            {
                Deselect();
                return;
            }

            var candidate = hit.collider.GetComponentInParent<CandidateAgent>();
            if (candidate != null)
            {
                Select(candidate);
                return;
            }

            var employee = hit.collider.GetComponentInParent<EmployeeAgent>();
            if (employee != null)
            {
                Select(employee);
                return;
            }

            var school = hit.collider.GetComponentInParent<StageSchoolView>();
            if (school != null)
            {
                Select(school);
                return;
            }

            var building = hit.collider.GetComponentInParent<StudioBuildingView>();
            if (building != null)
            {
                HandleBuildingClick(building);
                return;
            }

            Deselect();
        }

        private void HandleBuildingClick(StudioBuildingView building)
        {
            float now = UnityEngine.Time.unscaledTime;
            bool isDoubleClick = building == _lastClickedBuilding &&
                                 now - _lastBuildingClickTime <= DoubleClickSeconds;
            _lastClickedBuilding = building;
            _lastBuildingClickTime = now;
            Deselect();

            if (isDoubleClick && building.BuildingType == BuildingType.SoundStage)
            {
                building.GetComponent<LiveFilmingPlayback>()?.TryEnter();
                _lastClickedBuilding = null;
                _lastBuildingClickTime = float.NegativeInfinity;
            }
        }

        public void Select(EmployeeAgent agent)
        {
            if (SelectedAgent == agent && SelectedCandidate == null && SelectedStageSchool == null) return;

            ClearSelectionVisuals();
            SelectedAgent = agent;
            if (agent != null) agent.SetSelected(true);

            OnSelectionChanged?.Invoke(agent);
            OnCandidateSelectionChanged?.Invoke(null);
            OnStageSchoolSelectionChanged?.Invoke(null);
        }

        public void Select(CandidateAgent agent)
        {
            if (SelectedCandidate == agent && SelectedAgent == null && SelectedStageSchool == null) return;

            ClearSelectionVisuals();
            SelectedCandidate = agent;
            if (agent != null) agent.SetSelected(true);

            OnSelectionChanged?.Invoke(null);
            OnCandidateSelectionChanged?.Invoke(agent);
            OnStageSchoolSelectionChanged?.Invoke(null);
        }

        public void Select(StageSchoolView school)
        {
            if (SelectedStageSchool == school && SelectedAgent == null && SelectedCandidate == null) return;

            ClearSelectionVisuals();
            SelectedStageSchool = school;

            OnSelectionChanged?.Invoke(null);
            OnCandidateSelectionChanged?.Invoke(null);
            OnStageSchoolSelectionChanged?.Invoke(school);
        }

        public void Deselect()
        {
            bool changed = SelectedAgent != null || SelectedCandidate != null || SelectedStageSchool != null;
            ClearSelectionVisuals();
            if (!changed) return;

            OnSelectionChanged?.Invoke(null);
            OnCandidateSelectionChanged?.Invoke(null);
            OnStageSchoolSelectionChanged?.Invoke(null);
        }

        private void ClearSelectionVisuals()
        {
            if (SelectedAgent != null) SelectedAgent.SetSelected(false);
            if (SelectedCandidate != null) SelectedCandidate.SetSelected(false);

            SelectedAgent = null;
            SelectedCandidate = null;
            SelectedStageSchool = null;
        }
    }
}
