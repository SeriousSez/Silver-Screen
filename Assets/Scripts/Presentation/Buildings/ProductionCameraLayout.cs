using UnityEngine;
using UnityEngine.AI;
using SilverScreen.Domain.Movie;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class ProductionCameraLayout : MonoBehaviour
    {
        [SerializeField] private Transform _wideCamera;
        [SerializeField] private Transform _wideFocus;
        [SerializeField] private float _wideFieldOfView = 52f;

        public void Configure(Transform camera, Transform focus) { _wideCamera = camera; _wideFocus = focus; }

        public bool TryFrame(ShotType shot, Transform subject, out Vector3 position, out Vector3 focus, out float fov)
        {
            position = focus = default; fov = _wideFieldOfView;
            if (_wideCamera == null || _wideFocus == null) return false;
            if (shot == ShotType.Wide || subject == null)
            { position = _wideCamera.position; focus = _wideFocus.position; return true; }
            // Real employee roots are capsule centres; playback roots are feet.
            var nav = subject.GetComponent<NavMeshAgent>();
            Vector3 feet = subject.position - Vector3.up * (nav != null ? nav.baseOffset : 0);
            bool close = shot == ShotType.CloseUp;
            focus = feet + Vector3.up * (close ? 1.55f : 1.1f);
            var local = transform.InverseTransformPoint(feet);
            local += new Vector3(0, close ? 1.65f : 2.2f, close ? -2.7f : -5.2f);
            local.x = Mathf.Clamp(local.x, -5.3f, 5.3f);
            local.z = Mathf.Clamp(local.z, -9.5f, 6.5f);
            position = transform.TransformPoint(local); fov = close ? 34 : 44;
            return true;
        }
    }
}
