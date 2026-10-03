using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Selection;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Input/presentation adapter. Replace the prototype cards without changing hiring, work or movement.</summary>
    public sealed class PersonInteractionController : MonoBehaviour
    {
        [SerializeField, Min(.05f)] private float _pickupDelay = .18f;
        [SerializeField, Min(.1f)] private float _revealInterval = .9f;
        private StudioSelectionController _selection;
        private RecruitmentDriver _recruitment;
        private StudioEmployeeManager _workforce;
        private SimulationTimeDriver _time;
        private StudioBuildMode _build;
        private StudioBuildLot _lot;
        private UnityEngine.Camera _camera;
        private PersonPracticeService _practice;
        private PersonDragSession _drag;
        private Component _hover, _pinned, _pressed;
        private float _hoverSince, _pressedSince, _nextInfo;
        private bool _ownsLeft, _ownsRight;
        // A carry session outlives the initial press. Arm placement only after that press ends.
        private bool _carryClickArmed;
        private Vector3 _cursor;
        private bool _hasGround;
        private BuildingCutawayController _revealedBuilding;
        private float _carryRevealUntil;
        private bool _blockedRoom;
        private Employee _dismissEmployee;
        private WorkforceRoomAction _dismissAction;
        private System.IDisposable _dismissPause;
        private readonly List<PersonInteractionSpot> _visible = new List<PersonInteractionSpot>();
        private readonly List<PersonInformation> _cards = new List<PersonInformation>();
        private readonly PersonInformationService _information = new PersonInformationService();
        private string _feedback;
        private float _feedbackUntil;
        // Right-click information owns only its own gesture. Carry owns later LEFT clicks only.
        public bool BlocksCameraRightDrag => _ownsRight && _drag == null;
        private readonly List<ContextualDropTarget> _targets = new List<ContextualDropTarget>();
        public ContextualDropTarget MagneticTarget { get; private set; }
        public IReadOnlyList<ContextualDropTarget> VisibleTargets => _targets;
        private PersonDropContext DropContext => new PersonDropContext(_drag?.Candidate?.Candidate, _drag?.Employee?.Employee,
            _recruitment?.Coordinator, _practice, _workforce, RequestDismissal);
        public bool IsHolding => _drag != null;
        public bool HasPendingDismissal => _dismissEmployee != null;
        public void CancelForBuildMode()
        {
            CancelHold(); _hover = _pinned = null; _ownsRight = false;
        }
        public IReadOnlyList<PersonInteractionSpot> VisibleSpots => _visible;
        public void Initialize(StudioSelectionController selection)
        {
            _selection = selection;
            selection.OnSelectionChanged += SelectionChanged;
            selection.OnCandidateSelectionChanged += CandidateSelectionChanged;
            selection.OnStageSchoolSelectionChanged += SchoolSelectionChanged;
        }
        private void SelectionChanged(EmployeeAgent agent) { if (agent != null && agent != _pinned) _pinned = null; }
        private void CandidateSelectionChanged(CandidateAgent agent) { if (agent != null && agent != _pinned) _pinned = null; }
        private void SchoolSelectionChanged(StageSchoolView school) { if (school != null) _pinned = null; }
        private System.Collections.IEnumerator Start()
        {
            _camera = UnityEngine.Camera.main;
            _recruitment = FindAnyObjectByType<RecruitmentDriver>();
            _workforce = FindAnyObjectByType<StudioEmployeeManager>();
            _time = FindAnyObjectByType<SimulationTimeDriver>();
            _build = FindAnyObjectByType<StudioBuildMode>();
            _lot = FindAnyObjectByType<StudioBuildLot>();
            if (_lot == null)
            {
                var surface = FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
                if (surface != null) _lot = surface.gameObject.AddComponent<StudioBuildLot>();
            }
            if (_time != null) _practice = new PersonPracticeService(_time.Work, _time.Reservations);
            yield return null; // Recruitment creates its legacy waiting areas in Start.
            // Legacy prototype still uses Stage School attraction. Offer casting professions at its authored area.
            if (_recruitment?.Coordinator?.Facilities == null)
                foreach (var area in FindObjectsByType<CandidateWaitingAreaView>(FindObjectsInactive.Exclude))
                    if (area.Entrance != null)
                        PersonInteractionSpot.CreateHiringSpots(area.transform, area.FacilityId ?? area.name, area.Entrance,
                            area.Destination == RecruitmentDestination.ScriptOffice ? new[] { ProfessionalRole.Writer } :
                            new[] { ProfessionalRole.Actor, ProfessionalRole.Director, ProfessionalRole.Extra });
            if (_recruitment?.Coordinator?.Facilities == null)
                foreach (var building in FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude))
                    if (building.BuildingType == BuildingType.CastingOffice)
                        PersonInteractionSpot.CreateHiringSpots(building.transform, "legacy:" + building.name, building.EntrancePoint,
                            new[] { ProfessionalRole.Actor, ProfessionalRole.Director, ProfessionalRole.Extra });
            var prototype = new GameObject("Prototype Practice Comedy");
            prototype.transform.SetParent(transform, false);
            var service = _lot != null ? _lot.ServicePosition : Vector3.zero;
            prototype.transform.position = service + (_lot != null ? _lot.ServiceRotation : Quaternion.identity) * new Vector3(7, 0, -5);
            prototype.AddComponent<PersonInteractionSpot>().Configure("prototype:practice-comedy", null, PersonSpotActivity.Practice, ProfessionalRole.Actor, "Practice Comedy");
        }
        public bool HandleInput()
        {
            if (!isActiveAndEnabled) return false;
            if (_dismissEmployee != null)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) FinishDismissal(false);
                return true;
            }
            var mouse = Mouse.current;
            if (mouse == null || _camera == null || !_camera.isActiveAndEnabled) { CancelHold(); return false; }
            bool releaseFrame = _ownsLeft && mouse.leftButton.wasReleasedThisFrame;
            if (_ownsRight && !mouse.rightButton.isPressed && !mouse.rightButton.wasReleasedThisFrame) _ownsRight = false;
            if (_build != null && _build.IsOpen) { CancelHold(); _hover = _pinned = null; return false; }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { CancelHold(); _pinned = null; _selection.Deselect(); return true; }
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var screen = mouse.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screen);
            Component person = null;
            if (!overUi && TryVisibleWorldHit(ray, out var hit))
            {
                var candidate = hit.collider.GetComponentInParent<CandidateAgent>();
                var employee = hit.collider.GetComponentInParent<EmployeeAgent>();
                if (employee?.Employee?.IsEmployed == true) person = employee;
                else if (candidate?.Candidate != null && candidate.Candidate.Status != CandidateStatus.Hired) person = candidate;
            }
            if (_hover != person) { _hover = person; _hoverSince = Time.unscaledTime; _nextInfo = 0; }
            if (mouse.rightButton.wasPressedThisFrame && !overUi && _drag == null)
            {
                _pinned = person; _nextInfo = 0;
                if (person != null) { _ownsRight = true; _selection.Deselect(); return true; }
            }
            if (_drag == null && mouse.leftButton.wasPressedThisFrame && !overUi)
            {
                if (person != _pinned) _pinned = null;
                if (person != null) { _ownsLeft = true; _pressed = person; _pressedSince = Time.unscaledTime; }
            }
            if (_ownsLeft && _drag == null && _pressed != null && mouse.leftButton.isPressed && Time.unscaledTime - _pressedSince >= _pickupDelay)
            {
                var employee = _pressed as EmployeeAgent;
                if (employee != null && _practice?.IsPracticing(employee.Employee) == true) _practice.Cancel(employee.Employee);
                _drag = PersonDragSession.Begin(employee, _pressed as CandidateAgent);
                _carryClickArmed = false;
                _pinned = null; _selection.Deselect();
            }
            if (_drag != null)
            {
                if (_drag.Root == null || !_drag.Root.gameObject.activeInHierarchy) { CancelHold(); return true; }
                UpdateBuildingReveal(ray, !overUi);
                RaycastHit groundHit = default;
                _hasGround = !overUi && TryPlacementHit(ray, out groundHit);
                if (_hasGround) _cursor = groundHit.point;
                else if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) _cursor = ray.GetPoint(distance);
                _visible.Clear(); _targets.Clear();
                var context = DropContext;
                foreach (var target in ContextualDropTarget.Active)
                    if (target != null && !overUi && target.IsAvailable(context, _cursor))
                    {
                        _targets.Add(target);
                        var legacy = target.GetComponent<PersonInteractionSpot>();
                        if (legacy != null) _visible.Add(legacy);
                    }
                MagneticTarget = ContextualTargetSelection.Choose(_targets, _camera, screen, MagneticTarget);
                _blockedRoom = false;
                foreach (var target in ContextualDropTarget.Active)
                    if (target != null && !overUi && target.IsVisibleFor(context, _cursor) && !target.Action.CanExecute(context) &&
                        target.TryScore(_camera, screen, false, out float score) && score < 0)
                    { _blockedRoom = true; MagneticTarget = null; break; }
                foreach (var target in ContextualDropTarget.Active)
                    if (target != null) target.Present(context, !overUi && target.IsVisibleFor(context, _cursor), target == MagneticTarget);
                if (_revealedBuilding != null) _revealedBuilding.SetReveal(BuildingRevealReason.ContextualTargetActive,
                    MagneticTarget != null && MagneticTarget.InteriorVisibility == _revealedBuilding, _camera.transform.position);
                var indicated = _cursor;
                var preview = _cursor;
                if (MagneticTarget != null)
                {
                    MagneticTarget.TryPointerFloor(ray, out indicated);
                    if (!MagneticTarget.TryResolvePlacement(_drag, _lot, indicated, out preview)) preview = indicated;
                }
                _drag.Follow(preview);
                // A camera pointer gesture cannot accidentally become a contextual drop.
                bool placementClick = _carryClickArmed && mouse.leftButton.wasPressedThisFrame
                    && !mouse.rightButton.isPressed && !mouse.middleButton.isPressed;
                if (!mouse.leftButton.isPressed)
                {
                    _carryClickArmed = true;
                    _ownsLeft = false;
                    _pressed = null;
                }
                if (placementClick)
                {
                    bool placed = false;
                    if (!overUi && MagneticTarget != null)
                        placed = MagneticTarget.TryActivate(DropContext, _drag, _lot, indicated);
                    else if (!overUi && !_blockedRoom && _hasGround) placed = _drag.TryDrop(_cursor, _lot);
                    if (!placed)
                    {
                        Feedback("Invalid placement - still carrying. Choose another location or press Esc.");
                        return true;
                    }
                    _drag = null; ReleaseBuildingReveal(); ClearTargets(); _pressed = null; _ownsLeft = false; _carryClickArmed = false;
                    return true;
                }
                return true;
            }
            if (releaseFrame)
            {
                if (_pressed is EmployeeAgent employee) _selection.Select(employee);
                else if (_pressed is CandidateAgent candidate) _selection.Select(candidate);
                _pressed = null; _ownsLeft = false; return true;
            }
            return _ownsLeft;
        }
        private void ClearTargets()
        {
            foreach (var target in ContextualDropTarget.Active) if (target != null) target.Present(false, false);
            MagneticTarget = null; _targets.Clear(); _visible.Clear();
        }
        private void Feedback(string message) { _feedback = message; _feedbackUntil = Time.unscaledTime + 3; }
        private void CancelHold()
        { _drag?.Cancel(); _drag = null; ReleaseBuildingReveal(); ClearTargets(); _pressed = null; _ownsLeft = false; _carryClickArmed = false; }
        private void ReleaseBuildingReveal()
        {
            if (_revealedBuilding != null) _revealedBuilding.SetReveal(
                BuildingRevealReason.HeldPersonInteraction | BuildingRevealReason.ContextualTargetActive, false,
                _camera != null ? _camera.transform.position : Vector3.zero);
            _revealedBuilding = null;
        }
        private void UpdateBuildingReveal(Ray ray, bool allowed)
        {
            BuildingCutawayController nearest = null; float distance = float.PositiveInfinity;
            var context = DropContext;
            if (allowed)
                foreach (var target in ContextualDropTarget.Active)
                {
                    var building = target != null ? target.InteriorVisibility : null;
                    if (building == null || !building.isActiveAndEnabled || !target.IsRelevant(context)) continue;
                    var floor = new Plane(building.transform.up, building.transform.TransformPoint(
                        new Vector3(0, building.LocalInterior.min.y, 0)));
                    if (!floor.Raycast(ray, out float along) || along < 0) continue;
                    var point = ray.GetPoint(along);
                    if (!building.WithinCarryEnvelope(point, building == _revealedBuilding)) continue;
                    float d = (building.transform.InverseTransformPoint(point) - building.LocalInterior.center).sqrMagnitude;
                    if (d < distance) { nearest = building; distance = d; }
                }
            if (nearest != null) _carryRevealUntil = Time.unscaledTime + .45f;
            else if (_revealedBuilding != null && Time.unscaledTime < _carryRevealUntil) nearest = _revealedBuilding;
            if (_revealedBuilding != nearest) { ReleaseBuildingReveal(); _revealedBuilding = nearest; }
            if (_revealedBuilding != null) _revealedBuilding.SetReveal(BuildingRevealReason.HeldPersonInteraction, true, _camera.transform.position);
        }
        private static bool TryVisibleWorldHit(Ray ray, out RaycastHit hit)
        {
            hit = default;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (candidate.distance >= nearest) continue;
                bool hidden = false;
                foreach (var building in BuildingCutawayController.Active)
                    if (building != null && building.IsHiddenOccluder(candidate.collider)) { hidden = true; break; }
                if (hidden) continue;
                hit = candidate;
                nearest = candidate.distance;
            }
            return nearest < float.PositiveInfinity;
        }
        private bool TryPlacementHit(Ray ray, out RaycastHit hit)
        {
            if (_revealedBuilding == null) return Physics.Raycast(ray, out hit, 500f, ~0, QueryTriggerInteraction.Ignore);
            hit = default; float nearest = float.PositiveInfinity;
            foreach (var candidate in Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore))
                if (!_revealedBuilding.IsHiddenOccluder(candidate.collider) && candidate.distance < nearest)
                { hit = candidate; nearest = candidate.distance; }
            return nearest < float.PositiveInfinity;
        }
        private bool RequestDismissal(WorkforceRoomAction action, Employee employee)
        {
            if (_dismissEmployee != null || _workforce?.Workforce.CanChangeEmployment(employee) != true) return false;
            _dismissEmployee = employee; _dismissAction = action;
            action.GetComponent<ContextualDropTarget>().InteriorVisibility?.SetReveal(
                BuildingRevealReason.DismissalConfirmation, true, _camera.transform.position);
            // A central-clock lease releases only this pause and preserves the selected simulation speed.
            _dismissPause = _time?.Clock.AcquirePause("workforce.dismissal", "Dismiss employee confirmation");
            return true;
        }
        private void FinishDismissal(bool confirm)
        {
            if (_dismissEmployee == null) { _dismissPause?.Dispose(); _dismissPause = null; return; }
            if (confirm && _workforce != null)
            {
                if (_workforce.TryDismiss(_dismissEmployee, _dismissAction != null ? _dismissAction.Exit : null)) _selection?.Deselect();
                else Feedback("Employment could not end because this person now has an assignment.");
            }
            var building = _dismissAction != null ? _dismissAction.GetComponent<ContextualDropTarget>().InteriorVisibility : null;
            if (building != null)
            {
                building.HoldAfterInteraction();
                building.SetReveal(BuildingRevealReason.DismissalConfirmation, false, _camera != null ? _camera.transform.position : Vector3.zero);
            }
            _dismissEmployee = null; _dismissAction = null;
            _dismissPause?.Dispose(); _dismissPause = null;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelHold(); }
        private void OnDisable() { FinishDismissal(false); CancelHold(); _pinned = _hover = null; _ownsRight = false; }
        private void OnDestroy()
        {
            CancelHold(); _practice?.Dispose();
            if (_selection != null)
            {
                _selection.OnSelectionChanged -= SelectionChanged;
                _selection.OnCandidateSelectionChanged -= CandidateSelectionChanged;
                _selection.OnStageSchoolSelectionChanged -= SchoolSelectionChanged;
            }
        }
        private void LateUpdate()
        {
            var person = _pinned != null ? _pinned : _hover;
            if (_drag != null || person == null || Time.unscaledTime < _nextInfo) return;
            _nextInfo = Time.unscaledTime + .2f;
            var employee = (person as EmployeeAgent)?.Employee;
            var candidate = (person as CandidateAgent)?.Candidate;
            _information.Collect(new PersonInformationContext { Person = employee?.Person ?? candidate?.Person, Employee = employee,
                Candidate = candidate, Date = _time != null ? _time.Clock.CurrentTime : new SilverScreen.Domain.Time.SimulationDateTime(1930, 1, 1, 0, 0),
                Practice = _practice, Autonomy = _time?.Autonomy }, _cards);
        }
        private void OnGUI()
        {
            if (_camera == null) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = 13 };
            if (_dismissEmployee != null)
            {
                var box = new Rect((Screen.width - 420) / 2f, (Screen.height - 150) / 2f, 420, 150);
                GUI.Box(box, "Dismiss " + _dismissEmployee.Name + "?\n\nTheir employment will end. Their identity and skills are retained.", style);
                if (GUI.Button(new Rect(box.x + 28, box.y + 108, 165, 30), "Confirm dismissal")) FinishDismissal(true);
                if (GUI.Button(new Rect(box.x + 227, box.y + 108, 165, 30), "Cancel")) FinishDismissal(false);
                return;
            }
            if (Time.unscaledTime < _feedbackUntil) GUI.Box(new Rect(Screen.width / 2 - 230, 80, 460, 40), _feedback, style);
            if (_drag != null)
            {
                GUI.Box(new Rect(12, Screen.height - 65, 440, 45), "Click to place; release alone keeps carrying. Esc cancels.\nChoose a labelled floor region; grey regions are unavailable.", style);
                return;
            }
            var person = _pinned != null ? _pinned : _hover;
            if (person == null || !person.gameObject.activeInHierarchy) return;
            var character = person.GetComponent<SilverScreen.Presentation.Characters.CharacterPresentation>();
            var anchor = character != null && character.IsCanonical ? character.InformationAnchorWorld : person.transform.position + Vector3.up * 1.6f;
            var p = _camera.WorldToScreenPoint(anchor);
            if (p.z <= 0) return;
            var employee = (person as EmployeeAgent)?.Employee;
            var candidate = (person as CandidateAgent)?.Candidate;
            string name = employee?.Name ?? candidate?.Person.Name;
            string role = employee != null ? employee.Role.ToString() : "Unemployed";
            string activity = employee != null ? employee.CurrentState.ToString() : candidate?.Status.ToString();
            float x = Mathf.Clamp(p.x - 120, 6, Mathf.Max(6, Screen.width - 246));
            float y = Mathf.Clamp(Screen.height - p.y - 60, 6, Mathf.Max(6, Screen.height - 70));
            GUI.Box(new Rect(x, y, 240, 58), name + " â€” " + role + "\n" + activity, style);
            int count = _pinned != null ? _cards.Count : Mathf.Min(_cards.Count, 1 + (int)((Time.unscaledTime - _hoverSince) / _revealInterval));
            // Two columns around the anchor, with a bounded vertical stack for the prototype categories.
            float width = Mathf.Min(230, (Screen.width - 20) / 2f);
            float height = _pinned != null ? 132 : 70;
            int rows = (count + 1) / 2;
            y = Mathf.Clamp(y, 6, Mathf.Max(6, Screen.height - rows * (height + 5) - 70));
            GUI.Box(new Rect(x, y, 240, 58), name + " — " + role + "\n" + activity, style);
            float top = y + 64;
            for (int i = 0; i < count; i++)
            {
                float left = Mathf.Clamp(p.x - width - 4, 6, Mathf.Max(6, Screen.width - width * 2 - 14));
                var rect = new Rect(left + (i % 2) * (width + 8), top + (i / 2) * (height + 5), width, height);
                GUI.Box(rect, _cards[i].Category + "\n" + (_pinned != null ? _cards[i].Expanded : _cards[i].Summary), style);
            }
        }
    }
}
