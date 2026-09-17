using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using SilverScreen.Presentation.Employees;

namespace SilverScreen.Presentation.Selection
{
    public class StudioSelectionController : MonoBehaviour
    {
        [Header("Layer Masks")]
        [SerializeField] private LayerMask _selectableMask = ~0;

        [Header("Drag Threshold")]
        [SerializeField] private float _maxDragDistance = 6f;

        public EmployeeAgent SelectedAgent { get; private set; }
        public event Action<EmployeeAgent> OnSelectionChanged;

        private Vector2 _mouseDownPosition;
        private bool _isMouseDown;
        private UnityEngine.Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
                if (_mainCamera == null) return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                // Check if clicking on UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                _mouseDownPosition = mouse.position.ReadValue();
                _isMouseDown = true;
            }

            if (_isMouseDown && mouse.leftButton.wasReleasedThisFrame)
            {
                _isMouseDown = false;
                Vector2 mouseUpPosition = mouse.position.ReadValue();

                // If pointer moved significantly, user was panning/dragging, ignore selection
                if (Vector2.Distance(_mouseDownPosition, mouseUpPosition) > _maxDragDistance)
                {
                    return;
                }

                // Raycast into scene
                Ray ray = _mainCamera.ScreenPointToRay(mouseUpPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 300f, _selectableMask))
                {
                    var agent = hit.collider.GetComponentInParent<EmployeeAgent>();
                    if (agent != null)
                    {
                        Select(agent);
                    }
                    else
                    {
                        // Clicked terrain or something else -> deselect
                        Deselect();
                    }
                }
                else
                {
                    Deselect();
                }
            }
        }

        public void Select(EmployeeAgent agent)
        {
            if (SelectedAgent == agent) return;

            if (SelectedAgent != null)
            {
                SelectedAgent.SetSelected(false);
            }

            SelectedAgent = agent;

            if (SelectedAgent != null)
            {
                SelectedAgent.SetSelected(true);
            }

            OnSelectionChanged?.Invoke(SelectedAgent);
        }

        public void Deselect()
        {
            if (SelectedAgent != null)
            {
                SelectedAgent.SetSelected(false);
                SelectedAgent = null;
                OnSelectionChanged?.Invoke(null);
            }
        }
    }
}
