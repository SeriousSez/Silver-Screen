using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Camera;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class PrototypeProductionCamera : MonoBehaviour
    {
        private UnityEngine.Camera _productionCamera;
        private UnityEngine.Camera _managementCamera;
        private StudioCameraController _managementController;
        private bool _managementCameraWasEnabled;
        private bool _managementControllerWasEnabled;
        private bool _sequenceActive;
        private ShotType _activeShotType;
        private Transform _subject;

        public bool IsActive => _sequenceActive;

        public void EnsureCamera()
        {
            if (_productionCamera != null) return;

            var cameraObject = new GameObject("PrototypeProductionCamera");
            cameraObject.transform.SetParent(transform, false);
            _productionCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            _productionCamera.enabled = false;
            _productionCamera.fieldOfView = 48f;
            _productionCamera.nearClipPlane = 0.1f;
            _productionCamera.farClipPlane = 500f;
        }

        public bool BeginSequence()
        {
            EnsureCamera();
            if (_productionCamera == null) return false;
            if (_sequenceActive) return true;

            _managementCamera = UnityEngine.Camera.main;
            if (_managementCamera == _productionCamera) _managementCamera = null;
            if (_managementCamera == null)
            {
                foreach (var candidate in FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Exclude))
                {
                    if (candidate != _productionCamera && candidate.enabled)
                    {
                        _managementCamera = candidate;
                        break;
                    }
                }
            }

            if (_managementCamera != null)
            {
                _managementCameraWasEnabled = _managementCamera.enabled;
                _managementController = _managementCamera.GetComponentInParent<StudioCameraController>();
                if (_managementController != null)
                {
                    _managementControllerWasEnabled = _managementController.enabled;
                    _managementController.enabled = false;
                }
                _productionCamera.cullingMask = _managementCamera.cullingMask;
                _productionCamera.clearFlags = _managementCamera.clearFlags;
                _productionCamera.backgroundColor = _managementCamera.backgroundColor;
                _productionCamera.rect = _managementCamera.rect;
                _managementCamera.enabled = false;
            }

            _sequenceActive = true;
            _productionCamera.enabled = true;
            PresentWide();
            return true;
        }

        public bool Present(SceneShot shot, Transform subject)
        {
            if (!_sequenceActive || shot == null) return false;

            _activeShotType = shot.ShotType;
            _subject = subject;
            if ((_activeShotType == ShotType.Medium || _activeShotType == ShotType.CloseUp) && _subject == null)
            {
                Debug.LogWarning(
                    $"[PrototypeProductionCamera] Shot '{shot.Id}' has no resolvable subject; using Wide framing.");
                PresentWide();
                return false;
            }

            UpdateFraming();
            return true;
        }

        public void PresentWide()
        {
            _activeShotType = ShotType.Wide;
            _subject = null;
            UpdateFraming();
        }

        public void EndSequence()
        {
            if (_productionCamera != null) _productionCamera.enabled = false;
            if (_managementCamera != null) _managementCamera.enabled = _managementCameraWasEnabled;
            if (_managementController != null) _managementController.enabled = _managementControllerWasEnabled;
            _managementCamera = null;
            _managementController = null;
            _managementCameraWasEnabled = false;
            _managementControllerWasEnabled = false;
            _subject = null;
            _sequenceActive = false;
        }

        private void LateUpdate()
        {
            if (_sequenceActive && _productionCamera != null && _productionCamera.enabled)
                UpdateFraming();
        }

        private void UpdateFraming()
        {
            if (_productionCamera == null) return;

            if (_activeShotType == ShotType.Wide || _subject == null)
            {
                Vector3 focus = transform.TransformPoint(new Vector3(0f, 1.2f, -11f));
                Vector3 widePosition = transform.TransformPoint(new Vector3(0f, 7f, -19f));
                ApplyFrame(widePosition, focus, 52f);
                return;
            }

            Vector3 subjectPosition = _subject.position + Vector3.up * 0.9f;
            float distance = _activeShotType == ShotType.CloseUp ? 2.7f : 5.2f;
            float height = _activeShotType == ShotType.CloseUp ? 1.7f : 2.7f;
            float fieldOfView = _activeShotType == ShotType.CloseUp ? 34f : 44f;
            Vector3 shotPosition = subjectPosition - transform.forward * distance + Vector3.up * height;
            ApplyFrame(shotPosition, subjectPosition, fieldOfView);
        }

        private void ApplyFrame(Vector3 position, Vector3 focus, float fieldOfView)
        {
            _productionCamera.transform.position = position;
            Vector3 direction = focus - position;
            if (direction.sqrMagnitude > 0.001f)
                _productionCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _productionCamera.fieldOfView = fieldOfView;
        }

        private void OnDisable() => EndSequence();
        private void OnDestroy() => EndSequence();
    }
}
