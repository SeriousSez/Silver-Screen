using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Only this authored, held-open access is supported. Fail closed if its pose changes.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HeldOpenStageAccess : MonoBehaviour
    {
        [SerializeField] private Transform _hinge;
        [SerializeField] private NavMeshObstacle _thresholdBlocker;
        public bool IsPassable => _hinge != null && Quaternion.Angle(_hinge.localRotation, Quaternion.Euler(0, 95, 0)) < .1f;
        public void Configure(Transform hinge, NavMeshObstacle thresholdBlocker) { _hinge = hinge; _thresholdBlocker = thresholdBlocker; Refresh(); }
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void OnDisable() { if (_thresholdBlocker != null) _thresholdBlocker.enabled = true; }
        private void Refresh()
        {
            // The low threshold is part of the continuous walking NavMesh. Carve
            // the opening only when its authored held-open pose is unavailable.
            if (_thresholdBlocker != null) _thresholdBlocker.enabled = !isActiveAndEnabled || !IsPassable;
        }
    }
}
