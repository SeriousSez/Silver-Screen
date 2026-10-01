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
    [DefaultExecutionOrder(-100)]
    public class StudioSelectionController : MonoBehaviour
    {
        [Header("Layer Masks")]
        [SerializeField] private LayerMask _selectableMask = ~0;

        [Header("Drag Threshold")]
        [SerializeField] private float _maxDragDistance = 6f;

        public EmployeeAgent SelectedAgent { get; private set; }
        public CandidateAgent SelectedCandidate { get; private set; }
        public StageSchoolView SelectedStageSchool { get; private set; }
        public StudioBuildingView SelectedBuilding { get; private set; }

        public event Action<EmployeeAgent> OnSelectionChanged;
        public event Action<CandidateAgent> OnCandidateSelectionChanged;
        public event Action<StageSchoolView> OnStageSchoolSelectionChanged;

        public SilverScreen.Presentation.Interaction.PersonInteractionController PersonInteraction { get; private set; }
        private Vector2 _mouseDownPosition;
        private bool _isMouseDown;
        private UnityEngine.Camera _mainCamera;
        private StudioBuildingView _lastClickedBuilding;
        private float _lastBuildingClickTime = float.NegativeInfinity;
        private const float DoubleClickSeconds = 0.35f;

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
            PersonInteraction = GetComponent<SilverScreen.Presentation.Interaction.PersonInteractionController>() ?? gameObject.AddComponent<SilverScreen.Presentation.Interaction.PersonInteractionController>();
            PersonInteraction.Initialize(this);
        }

        private void Update()
        {
            UpdatePointerReveal();
            if (FindAnyObjectByType<StudioBuildMode>()?.OwnsWorldInput == true) { _isMouseDown = false; return; }
            if (PersonInteraction != null && PersonInteraction.HandleInput()) { _isMouseDown = false; return; }
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

        private void UpdatePointerReveal()
        {
            if (_mainCamera == null) _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return;
            var mouse = Mouse.current;
            BuildingCutawayController hovered = null;
            bool allowed = mouse != null && _mainCamera.isActiveAndEnabled &&
                !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) &&
                FindAnyObjectByType<StudioBuildMode>()?.IsOpen != true;
            if (allowed)
            {
                var ray = _mainCamera.ScreenPointToRay(mouse.position.ReadValue());
                float nearest = float.PositiveInfinity;
                foreach (var building in BuildingCutawayController.Active)
                    if (building != null && building.Intersects(ray, out float distance) && distance < nearest)
                    { nearest = distance; hovered = building; }
                if (hovered != null && Physics.Raycast(ray, out var hit, nearest + .01f, ~0, QueryTriggerInteraction.Ignore) &&
                    !hit.transform.IsChildOf(hovered.transform)) hovered = null;
            }
            foreach (var building in BuildingCutawayController.Active)
                if (building != null) building.UpdatePointer(building == hovered, _mainCamera.transform.position);
        }

        private void HandleBuildingClick(StudioBuildingView building)
        {
            float now = UnityEngine.Time.unscaledTime;
            bool isDoubleClick = building == _lastClickedBuilding &&
                                 now - _lastBuildingClickTime <= DoubleClickSeconds;
            _lastClickedBuilding = building;
            _lastBuildingClickTime = now;
            Deselect();
            SelectedBuilding = building;
            building.GetComponent<BuildingCutawayController>()?.SetReveal(BuildingRevealReason.BuildingFocus, true, _mainCamera.transform.position);

            if (isDoubleClick)
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
            bool changed = SelectedAgent != null || SelectedCandidate != null || SelectedStageSchool != null || SelectedBuilding != null;
            ClearSelectionVisuals();
            if (!changed) return;

            OnSelectionChanged?.Invoke(null);
            OnCandidateSelectionChanged?.Invoke(null);
            OnStageSchoolSelectionChanged?.Invoke(null);
        }

        private void ClearSelectionVisuals()
        {
            if (SelectedBuilding != null) SelectedBuilding.GetComponent<BuildingCutawayController>()?.SetReveal(
                BuildingRevealReason.BuildingFocus, false, _mainCamera != null ? _mainCamera.transform.position : Vector3.zero);
            SelectedBuilding = null;
            if (SelectedAgent != null) SelectedAgent.SetSelected(false);
            if (SelectedCandidate != null) SelectedCandidate.SetSelected(false);

            SelectedAgent = null;
            SelectedCandidate = null;
            SelectedStageSchool = null;
        }
        private void OnDisable() => ClearSelectionVisuals();
    }
}
