using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace SilverScreen.Presentation.Camera
{
    public class StudioCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _panSpeed = 20f;
        [SerializeField] private float _panSmoothing = 12f;
        [SerializeField] private Vector2 _panBoundsMin = new Vector2(-45f, -45f);
        [SerializeField] private Vector2 _panBoundsMax = new Vector2(45f, 45f);

        [Header("Mouse Pan")]
        [SerializeField] private float _mouseDragPanSpeed = 0.035f;
        [SerializeField] private bool _enableEdgePan = true;
        [SerializeField] private float _edgePanWidth = 12f;

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 80f;
        [SerializeField] private float _mouseRotationSpeed = 0.25f;
        [SerializeField] private float _rotationSmoothing = 10f;

        [Header("Zoom")]
        [SerializeField] private float _minZoomDistance = 8f;
        [SerializeField] private float _maxZoomDistance = 45f;
        [SerializeField] private float _zoomSpeed = 4f;
        [SerializeField] private float _zoomSmoothing = 10f;

        [Header("Elevation Angles")]
        [SerializeField] private float _pitchAngle = 52f;
        [SerializeField] private float _closeZoomPitchAngle = 15f;
        [SerializeField] private float _pitchSmoothing = 10f;

        [Header("Target References")]
        [SerializeField] private UnityEngine.Camera _targetCamera;

        // Internal state
        private Vector3 _targetPosition;
        private Vector3 _currentPosition;

        private float _targetYaw;
        private float _currentYaw;

        private float _targetDistance = 22f;
        private float _currentDistance = 22f;
        private float _currentPitch;

        private Vector2 _lastMousePosition;
        private bool _isRightDragging;
        private bool _isMiddleDragging;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = GetComponentInChildren<UnityEngine.Camera>();
                if (_targetCamera == null)
                {
                    _targetCamera = UnityEngine.Camera.main;
                }
            }

            _targetPosition = transform.position;
            _currentPosition = _targetPosition;

            _targetYaw = transform.eulerAngles.y;
            _currentYaw = _targetYaw;

            _currentDistance = _targetDistance;
            _currentPitch = CalculatePitchForDistance(_currentDistance);
        }

        private void Update()
        {
            HandlePanInput();
            HandleRotationInput();
            HandleZoomInput();
            UpdateTransforms();
        }

        private void HandlePanInput()
        {
            if (IsTypingIntoUI()) return;

            Vector2 panInput = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) panInput.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) panInput.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) panInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) panInput.x += 1f;
            }

            var mouse = Mouse.current;
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null)
            {
                if (mouse.middleButton.wasPressedThisFrame && !pointerOverUi)
                {
                    _isMiddleDragging = true;
                    _lastMousePosition = mouse.position.ReadValue();
                }
                if (mouse.middleButton.wasReleasedThisFrame) _isMiddleDragging = false;

                Vector2 mousePosition = mouse.position.ReadValue();
                if (_isMiddleDragging)
                {
                    Vector2 delta = mousePosition - _lastMousePosition;
                    _lastMousePosition = mousePosition;
                    Quaternion yawRotation = Quaternion.Euler(0f, _targetYaw, 0f);
                    Vector3 drag = yawRotation * new Vector3(-delta.x, 0f, -delta.y);
                    _targetPosition += drag * (_mouseDragPanSpeed * ZoomSpeedModifier());
                }
                else if (_enableEdgePan && !pointerOverUi && Application.isFocused)
                {
                    if (mousePosition.x <= _edgePanWidth) panInput.x -= 1f;
                    else if (mousePosition.x >= Screen.width - _edgePanWidth) panInput.x += 1f;
                    if (mousePosition.y <= _edgePanWidth) panInput.y -= 1f;
                    else if (mousePosition.y >= Screen.height - _edgePanWidth) panInput.y += 1f;
                }
            }

            if (panInput.sqrMagnitude > 0.001f)
            {
                panInput.Normalize();

                // Compute movement relative to current yaw
                Quaternion yawRotation = Quaternion.Euler(0f, _targetYaw, 0f);
                Vector3 moveDir = yawRotation * new Vector3(panInput.x, 0f, panInput.y);

                _targetPosition += moveDir * (_panSpeed * ZoomSpeedModifier() * UnityEngine.Time.unscaledDeltaTime);
            }

            // Clamp target position within studio bounds
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, _panBoundsMin.x, _panBoundsMax.x);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, _panBoundsMin.y, _panBoundsMax.y);
        }

        private float ZoomSpeedModifier() => _targetDistance / 20f;

        private void HandleRotationInput()
        {
            float rotationInput = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) rotationInput -= 1f;
                if (keyboard.eKey.isPressed) rotationInput += 1f;
            }

            _targetYaw += rotationInput * _rotationSpeed * UnityEngine.Time.unscaledDeltaTime;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }

                    _isRightDragging = true;
                    _lastMousePosition = mouse.position.ReadValue();
                }
                else if (mouse.rightButton.wasReleasedThisFrame)
                {
                    _isRightDragging = false;
                }

                if (_isRightDragging)
                {
                    Vector2 currentMouse = mouse.position.ReadValue();
                    Vector2 delta = currentMouse - _lastMousePosition;
                    _lastMousePosition = currentMouse;

                    _targetYaw += delta.x * _mouseRotationSpeed;
                }
            }
        }

        private void HandleZoomInput()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    // Normalized scroll delta
                    float scrollDelta = Mathf.Sign(scroll) * _zoomSpeed;
                    _targetDistance -= scrollDelta;
                    _targetDistance = Mathf.Clamp(_targetDistance, _minZoomDistance, _maxZoomDistance);
                }
            }
        }

        private void UpdateTransforms()
        {
            float dt = UnityEngine.Time.unscaledDeltaTime;

            // Interpolate position
            _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, dt * _panSmoothing);
            transform.position = _currentPosition;

            // Interpolate rotation
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, dt * _rotationSmoothing);
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);

            // Interpolate zoom distance
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, dt * _zoomSmoothing);
            float targetPitch = CalculatePitchForDistance(_targetDistance);
            _currentPitch = Mathf.LerpAngle(_currentPitch, targetPitch, dt * _pitchSmoothing);

            // Position target camera
            if (_targetCamera != null)
            {
                Quaternion cameraRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                Vector3 offset = cameraRotation * new Vector3(0f, 0f, -_currentDistance);

                _targetCamera.transform.position = _currentPosition + offset;
                _targetCamera.transform.rotation = cameraRotation;
            }
        }

        public void FocusOn(Vector3 worldPosition)
        {
            _targetPosition = new Vector3(worldPosition.x, 0f, worldPosition.z);
        }

        private float CalculatePitchForDistance(float distance)
        {
            float zoomRange = Mathf.Max(0.01f, _maxZoomDistance - _minZoomDistance);
            float normalizedZoom = Mathf.Clamp01((distance - _minZoomDistance) / zoomRange);
            float easedZoom = Mathf.SmoothStep(0f, 1f, normalizedZoom);
            return Mathf.Lerp(_closeZoomPitchAngle, _pitchAngle, easedZoom);
        }

        private void OnDisable()
        {
            _isRightDragging = false;
            _isMiddleDragging = false;
        }

        private static bool IsTypingIntoUI()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponentInParent<TMP_InputField>() != null;
        }
    }
}
