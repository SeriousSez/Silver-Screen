using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Selection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public sealed class StaffPanelUI : MonoBehaviour
    {
        private readonly HashSet<Employee> _observedEmployees = new HashSet<Employee>();
        private StudioEmployeeManager _employeeManager;
        private StudioSelectionController _selectionController;
        private RectTransform _content;

        public event Action<Employee> OnEmployeeSelected;

        public void Initialize(
            StudioEmployeeManager employeeManager,
            StudioSelectionController selectionController)
        {
            _employeeManager = employeeManager;
            _selectionController = selectionController;
            if (_employeeManager != null)
            {
                _employeeManager.OnEmployeeAdded += HandleEmployeeAdded;
            }
            Build();
            BindEmployees();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_employeeManager != null)
            {
                _employeeManager.OnEmployeeAdded -= HandleEmployeeAdded;
            }

            foreach (var employee in _observedEmployees)
            {
                employee.OnStateChanged -= HandleEmployeeChanged;
                employee.OnDetailsChanged -= HandleEmployeeChanged;
            }
            _observedEmployees.Clear();
        }

        private void Build()
        {
            ManagementUIFactory.Background(
                GetComponent<RectTransform>(),
                ManagementUIFactory.Panel);

            var titleRect = ManagementUIFactory.Rect(
                "Title",
                transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(190f, -38f),
                new Vector2(340f, 48f));
            var title = ManagementUIFactory.Text(
                "Text",
                titleRect,
                "STUDIO STAFF",
                28f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Gold);
            title.fontStyle = FontStyles.Bold;

            var columnsRect = ManagementUIFactory.Rect(
                "Columns",
                transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -82f),
                new Vector2(-36f, 28f));
            ManagementUIFactory.Text(
                "Text",
                columnsRect,
                "NAME / ROLE                         SKILL     MORALE       MONTHLY SALARY                     CURRENT ACTIVITY",
                13f,
                TextAlignmentOptions.MidlineLeft,
                ManagementUIFactory.Muted);

            var scrollRect = ManagementUIFactory.Rect(
                "StaffScroll",
                transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, -48f),
                new Vector2(-36f, -142f));
            ManagementUIFactory.ScrollView("ScrollView", scrollRect, out _content);
        }

        private void BindEmployees()
        {
            if (_employeeManager == null) return;
            foreach (var employee in _employeeManager.AllEmployees)
            {
                if (employee == null || !_observedEmployees.Add(employee)) continue;
                employee.OnStateChanged += HandleEmployeeChanged;
                employee.OnDetailsChanged += HandleEmployeeChanged;
            }
        }

        private void HandleEmployeeAdded(Employee employee)
        {
            BindEmployees();
            Refresh();
        }

        private void HandleEmployeeChanged(Employee employee)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_content == null) return;
            foreach (Transform child in _content)
            {
                Destroy(child.gameObject);
            }

            if (_employeeManager == null || _employeeManager.AllEmployees.Count == 0)
            {
                var emptyRect = ManagementUIFactory.Rect(
                    "Empty",
                    _content,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    new Vector2(0f, 48f));
                emptyRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
                ManagementUIFactory.Text(
                    "Text",
                    emptyRect,
                    "No employees",
                    15f,
                    TextAlignmentOptions.MidlineLeft,
                    ManagementUIFactory.Muted);
                return;
            }

            BindEmployees();
            foreach (var employee in _employeeManager.AllEmployees)
            {
                CreateEmployeeRow(employee);
            }
        }

        private void CreateEmployeeRow(Employee employee)
        {
            var row = ManagementUIFactory.Rect(
                "Employee_" + employee.Id,
                _content,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, 68f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
            var image = ManagementUIFactory.Background(row, ManagementUIFactory.PanelRaised);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                var agent = _employeeManager?.GetAgent(employee);
                if (agent != null)
                {
                    _selectionController?.Select(agent);
                    OnEmployeeSelected?.Invoke(employee);
                }
            });

            CreateCell(
                row,
                "Identity",
                0f,
                0.30f,
                $"<b>{employee.Name}</b>\n<color=#F2B32E>{employee.Role}</color>",
                TextAlignmentOptions.MidlineLeft);
            CreateCell(row, "Skill", 0.30f, 0.40f, employee.Skill.ToString(), TextAlignmentOptions.Center);
            CreateCell(row, "Morale", 0.40f, 0.51f, employee.Morale + "%", TextAlignmentOptions.Center);
            CreateCell(row, "Salary", 0.51f, 0.68f, $"${employee.Salary:N0} / month", TextAlignmentOptions.Center);

            string activity = employee.CurrentIntent != null &&
                              !string.IsNullOrWhiteSpace(employee.CurrentIntent.Description)
                ? employee.CurrentIntent.Description
                : employee.CurrentState.ToString();
            CreateCell(
                row,
                "Activity",
                0.68f,
                1f,
                $"<b>{employee.CurrentState}</b>\n<color=#AEB7C0>{activity}</color>",
                TextAlignmentOptions.MidlineLeft);
        }

        private static void CreateCell(
            Transform parent,
            string name,
            float min,
            float max,
            string value,
            TextAlignmentOptions alignment)
        {
            var rect = ManagementUIFactory.Rect(
                name,
                parent,
                new Vector2(min, 0f),
                new Vector2(max, 1f),
                Vector2.zero,
                Vector2.zero);
            ManagementUIFactory.SetOffsets(rect, 12f, 4f, 8f, 4f);
            ManagementUIFactory.Text(
                "Text",
                rect,
                value,
                14f,
                alignment,
                Color.white);
        }
    }
}
