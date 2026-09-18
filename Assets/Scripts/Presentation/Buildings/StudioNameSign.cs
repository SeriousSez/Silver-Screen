using SilverScreen.Domain;
using TMPro;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public sealed class StudioNameSign : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _label;

        private StudioIdentity _identity;

        public void Configure(TextMeshPro label)
        {
            _label = label;
        }

        public void Initialize(StudioIdentity identity)
        {
            if (ReferenceEquals(_identity, identity))
            {
                Refresh();
                return;
            }

            Unbind();
            _identity = identity;
            if (_identity != null)
            {
                _identity.OnNameChanged += HandleNameChanged;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_identity != null)
            {
                _identity.OnNameChanged -= HandleNameChanged;
            }
            _identity = null;
        }

        private void HandleNameChanged(string studioName)
        {
            if (_label != null) _label.text = studioName;
        }

        private void Refresh()
        {
            if (_label != null)
            {
                _label.text = _identity?.Name ?? string.Empty;
            }
        }
    }
}
