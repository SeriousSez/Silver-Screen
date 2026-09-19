using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public enum ManagementPanel
    {
        None,
        Studio,
        Productions,
        Staff,
        Finances,
        Screenplays
    }

    public sealed class ManagementNavigation : MonoBehaviour
    {
        private readonly Dictionary<ManagementPanel, Button> _buttons =
            new Dictionary<ManagementPanel, Button>();

        public ManagementPanel ActivePanel { get; private set; }
        public event Action<ManagementPanel> OnPanelRequested;

        public void Build()
        {
            CreateButton(ManagementPanel.Studio, "STUDIO");
            CreateButton(ManagementPanel.Productions, "PRODUCTIONS");
            CreateButton(ManagementPanel.Staff, "STAFF");
            CreateButton(ManagementPanel.Finances, "FINANCES");
            CreateButton(ManagementPanel.Screenplays, "SCREENPLAYS");

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public void SetActive(ManagementPanel panel)
        {
            ActivePanel = panel;
            foreach (var pair in _buttons)
            {
                bool active = pair.Key == panel;
                var image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = active
                        ? ManagementUIFactory.Gold
                        : ManagementUIFactory.PanelRaised;
                }

                var text = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = active
                        ? new Color(0.08f, 0.07f, 0.05f)
                        : Color.white;
                }
            }
        }

        private void CreateButton(ManagementPanel panel, string label)
        {
            var button = ManagementUIFactory.Button(
                panel + "Button",
                transform,
                label,
                ManagementUIFactory.PanelRaised,
                Color.white);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 46f;
            button.onClick.AddListener(() => OnPanelRequested?.Invoke(panel));
            _buttons.Add(panel, button);
        }
    }
}
