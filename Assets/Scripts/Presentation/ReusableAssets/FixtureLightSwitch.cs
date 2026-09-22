using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    /// <summary>Optional representational lighting; no circuit simulation.</summary>
    public sealed class FixtureLightSwitch : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private bool _isOn;
        public bool IsOn => _isOn;
        public void SetOn(bool on) { _isOn = on; Apply(); }
        private void OnEnable() => Apply();
        private void OnDisable() { if (_light != null) _light.enabled = false; }
        private void Apply() { if (_light != null) _light.enabled = _isOn && isActiveAndEnabled; }
    }
}
