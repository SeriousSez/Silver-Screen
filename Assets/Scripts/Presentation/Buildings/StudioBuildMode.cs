using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SilverScreen.Presentation.Buildings
{
    // Keyboard ownership is resolved before StudioSelectionController (-100).
    [DefaultExecutionOrder(-110)]
    public sealed class StudioBuildMode : MonoBehaviour
    {
        private StudioConstructionDriver _driver;
        private BuildingDefinition _selected;
        private BuildingPlacementPreview _preview;
        private BuildModeUI _ui;
        private BuildingPose _pose;
        private string _message;
        private bool _hasGround, _clickArmed, _pressed;
        private Vector2 _pressPosition;
        private float _nextValidation;
        private int _consumedFrame = -1;
        private readonly BuildingRotationGesture _rotation = new BuildingRotationGesture();
        public bool IsOpen { get; private set; }
        public bool IsPlacing => _selected != null;
        // Closing must not expose the same release/Esc to world selection or carry.
        public bool OwnsWorldInput => IsOpen || _consumedFrame == Time.frameCount;
        public BuildCategory? Category { get; private set; }
        public float PositionSnap { get; set; }
        public float RotationSnap { get; set; } = 90;
        public bool OwnsRotationGesture => IsPlacing && _rotation.IsActive;
        public bool IsFreeRotating => IsPlacing && _rotation.IsFreeRotating;
        public float RotationAngle => _pose.Yaw;
        public BuildingPose PreviewPose => _pose;
        public string Message => _message;
        public IReadOnlyList<BuildPaletteItem> Catalog { get; private set; }

        public void Initialize(StudioConstructionDriver driver)
        {
            _driver = driver;
            // Future set/decoration providers supply their own category, availability and selection action.
            Catalog = driver.Service.Definitions.Select(d => new BuildPaletteItem(d.Id, BuildCategory.Facilities,
                d.Id == StarterFeatureIds.Headquarters ? "Administration" : d.DisplayName,
                d.Cost, () => driver.Service.AvailabilityReason(d.Id), () => Select(d.Id))).ToArray();
            _ui = gameObject.AddComponent<BuildModeUI>(); _ui.Initialize(this);
        }
        public void Open()
        {
            var person = FindAnyObjectByType<PersonInteractionController>();
            if (person?.HasPendingDismissal == true) { _message = "Finish the dismissal confirmation before opening Build."; return; }
            bool held = person?.IsHolding == true;
            person?.CancelForBuildMode();
            IsOpen = true; _consumedFrame = Time.frameCount;
            _message = held ? "Carry cancelled. Person returned to their previous position." : "Choose a Build category.";
        }
        public void ChooseCategory(BuildCategory category)
        {
            if (!IsOpen) Open();
            if (!IsOpen) return;
            CancelPreview(); Category = category; _message = null;
        }
        public void Select(string id)
        {
            if (_driver?.Service == null) return;
            if (!IsOpen) Open();
            if (!IsOpen) return;
            var definition = _driver.Service.Definitions.FirstOrDefault(d => d.Id == id);
            _message = _driver.Service.AvailabilityReason(id);
            if (definition == null || _message != null) return;
            var template = _driver.PlacementTemplate(id);
            if (template == null) { _message = "Building content unavailable."; return; }
            CancelPreview(); Category = BuildCategory.Facilities; _selected = definition;
            _pose = default; _hasGround = false; _clickArmed = false; _pressed = false;
            _preview = new BuildingPlacementPreview(template, definition);
            _message = "Move the preview onto the studio lot."; _nextValidation = 0;
        }
        public void MovePreview(BuildingPose pose)
        {
            _pose = pose; _hasGround = pose.IsValid;
            _preview?.Move(pose, _hasGround); ValidatePreview();
        }
        private void ValidatePreview()
        {
            if (_selected == null) return;
            _message = _hasGround ? _driver.Service.Validate(_selected.Id, _pose) : "Point at the studio lot.";
            _preview?.SetValid(_message == null); _nextValidation = Time.unscaledTime + .10f;
        }
        public PlacedBuilding Confirm()
        {
            if (_selected == null || !_hasGround) return null;
            // Recheck current geometry, physics, gates and finances before spending.
            var placed = _driver.Service.Place(_selected.Id, _pose, out _message);
            if (placed != null) { CancelPreview(); _message = "Construction site placed. Available workers will travel there."; }
            else _preview?.SetValid(false);
            return placed;
        }
        public void CancelPreview()
        {
            _selected = null; _preview?.Dispose(); _preview = null;
            _hasGround = _pressed = _clickArmed = false; _message = null;
            _rotation.Cancel();
            _consumedFrame = Time.frameCount;
        }
        public void Back()
        {
            if (IsPlacing) { CancelPreview(); return; }
            Close();
        }
        public void Close() { CancelPreview(); IsOpen = false; Category = null; }
        private void Update()
        {
            var keyboard = Keyboard.current;
            var focus = EventSystem.current?.currentSelectedGameObject;
            bool typing = focus != null && focus.GetComponentInParent<TMP_InputField>() != null;
            if (!typing && keyboard?.bKey.wasPressedThisFrame == true) { if (IsOpen) Close(); else Open(); }
            if (IsOpen && keyboard?.escapeKey.wasPressedThisFrame == true) Back();
        }
        private void LateUpdate()
        {
            var mouse = Mouse.current; var keyboard = Keyboard.current; var camera = UnityEngine.Camera.main;
            if (!IsOpen || !IsPlacing || mouse == null || camera == null || !camera.isActiveAndEnabled) return;
            var pointer = mouse.position.ReadValue();
            bool overUi = EventSystem.current?.IsPointerOverGameObject() == true;
            bool cameraGesture = mouse.rightButton.isPressed || mouse.middleButton.isPressed ||
                mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0;
            if (keyboard != null) cameraGesture |= keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed ||
                keyboard.dKey.isPressed || keyboard.qKey.isPressed || keyboard.eKey.isPressed || keyboard.upArrowKey.isPressed ||
                keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;
            var focus = EventSystem.current?.currentSelectedGameObject;
            bool typing = focus != null && focus.GetComponentInParent<TMP_InputField>() != null;
            if (!typing && keyboard?.rKey.wasPressedThisFrame == true)
            {
                _rotation.Begin(_pose.Yaw, pointer.x, keyboard.shiftKey.isPressed, Time.unscaledTime,
                    _driver.Lot.OrientationYaw, RotationSnap);
                _pressed = false;
            }
            if (_rotation.IsActive && keyboard?.rKey.isPressed == true)
            {
                float yaw = _rotation.Update(pointer.x, Time.unscaledTime);
                if (!Mathf.Approximately(_pose.Yaw, yaw)) { _pose.Yaw = yaw; _nextValidation = 0; }
            }
            if (_rotation.IsActive && keyboard?.rKey.wasReleasedThisFrame == true)
            {
                _pose.Yaw = _rotation.End(pointer.x, Time.unscaledTime);
                _nextValidation = 0;
            }
            Vector3 point = default;
            if (!_rotation.IsActive) _hasGround = _driver.Lot.TryProjectGround(camera.ScreenPointToRay(pointer), out point);
            if (_hasGround && !_rotation.IsActive)
            {
                float Snap(float value) => PositionSnap > 0 ? Mathf.Round(value / PositionSnap) * PositionSnap : value;
                _pose.X = Snap(point.x); _pose.Z = Snap(point.z);
            }
            // Follow after Camera.Update, using the camera which will render this frame.
            _preview.Move(_pose, _hasGround && !overUi);
            if (Time.unscaledTime >= _nextValidation || !_hasGround) ValidatePreview();
            cameraGesture |= _rotation.IsActive;
            if (cameraGesture || overUi || typing || !Application.isFocused) _pressed = false;
            if (!mouse.leftButton.isPressed && !cameraGesture) _clickArmed = true;
            if (_clickArmed && mouse.leftButton.wasPressedThisFrame && !cameraGesture && !overUi && !typing)
            { _pressed = true; _pressPosition = pointer; }
            if (_pressed && Vector2.Distance(pointer, _pressPosition) > 6) _pressed = false;
            if (mouse.leftButton.wasReleasedThisFrame)
            { bool confirm = _pressed; _pressed = false; if (confirm) Confirm(); }
        }
        private void OnApplicationFocus(bool focused)
        { if (!focused) { _pressed = _clickArmed = false; _rotation.Cancel(); } }
        private void OnDisable() { Close(); if (_ui != null) _ui.enabled = false; }
        private void OnEnable() { if (_ui != null) _ui.enabled = true; }
        private void OnDestroy() { _preview?.Dispose(); if (_ui != null) Destroy(_ui); }
    }
}
