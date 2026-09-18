using SilverScreen.Domain;

namespace SilverScreen.Presentation.Buildings
{
    public sealed class StudioNameSign : BuildingSign
    {
        private StudioIdentity _identity;

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
            SetText(studioName);
        }

        private void Refresh()
        {
            SetText(_identity?.Name);
        }
    }
}