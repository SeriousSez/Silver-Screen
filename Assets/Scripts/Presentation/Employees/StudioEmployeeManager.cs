using System;
using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain;

namespace SilverScreen.Presentation.Employees
{
    public class StudioEmployeeManager : MonoBehaviour
    {
        private WorkforceRoster _workforce;
        public WorkforceRoster Workforce => _workforce ??= new WorkforceRoster(employee =>
            GetComponent<SilverScreen.Presentation.SimulationTime.SimulationTimeDriver>()?.Reservations.IsReserved(
                new SilverScreen.Domain.Resources.ResourceKey("person", employee.Id)) == true);
        private readonly List<EmployeeAgent> _agents = new List<EmployeeAgent>();
        private readonly Dictionary<string, EmployeeAgent> _agentByEmployeeId = new Dictionary<string, EmployeeAgent>();

        public IReadOnlyList<Employee> AllEmployees => Workforce.Employees;
        public IReadOnlyList<EmployeeAgent> AllAgents => _agents;
        public event Action<Employee> OnEmployeeAdded;
        public event Action<Employee> OnEmployeeRemoved;
        public event Action<Employee> BeforeEmploymentChange;

        public void RegisterEmployee(Employee employee, EmployeeAgent agent)
        {
            bool added = Workforce.Add(employee);

            if (!_agents.Contains(agent))
            {
                _agents.Add(agent);
            }

            _agentByEmployeeId[employee.Id] = agent;
            agent.BindDomain(employee);
            if (added)
            {
                var time = GetComponent<SilverScreen.Presentation.SimulationTime.SimulationTimeDriver>();
                time?.Wellbeing.StartCareerTracking(employee.Person);
                if (time != null) agent.AvailabilityChanged += available =>
                { if (!time.Work.IsDisposed) time.Work.SetResourceAvailable(new SilverScreen.Domain.Resources.ResourceKey("person", employee.Id), available); };
                OnEmployeeAdded?.Invoke(employee);
            }
        }

        public EmployeeAgent GetAgent(string employeeId)
        {
            _agentByEmployeeId.TryGetValue(employeeId, out var agent);
            return agent;
        }

        public Employee GetEmployee(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) return null;
            foreach (var employee in AllEmployees) if (employee.Id == employeeId) return employee;
            return null;
        }

        public EmployeeAgent GetAgent(Employee employee)
        {
            if (employee == null) return null;
            return GetAgent(employee.Id);
        }

        public bool TryReassign(Employee employee, EmployeeRole role)
        {
            if (employee == null || !Enum.IsDefined(typeof(EmployeeRole), role) || employee.Role == role) return false;
            BeforeEmploymentChange?.Invoke(employee);
            if (!Workforce.TryReassign(employee, role)) return false;
            GetAgent(employee)?.CancelPresentationMovement();
            return true;
        }

        public bool TryDismiss(Employee employee, Transform exit)
        {
            BeforeEmploymentChange?.Invoke(employee);
            if (!Workforce.TryDismiss(employee)) return false;
            var wellbeing = GetComponent<SilverScreen.Presentation.SimulationTime.SimulationTimeDriver>()?.Wellbeing;
            wellbeing?.StopCareerTracking(employee.Person);
            wellbeing?.Unregister(employee.Person);
            var agent = GetAgent(employee);
            _agentByEmployeeId.Remove(employee.Id); _agents.Remove(agent);
            OnEmployeeRemoved?.Invoke(employee);
            if (agent != null)
            {
                agent.CancelPresentationMovement(); agent.SetSelected(false);
                // Reuse the existing agent and authored recruitment exit. The domain record survives the view.
                if (exit == null || !agent.TryAssignTaskDestination(exit.position,
                    new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Leaving the studio"), () => Destroy(agent.gameObject)))
                {
                    agent.enabled = false;
                    Debug.LogWarning("Dismissed person retained at placement: no complete route to the facility exit.", agent);
                }
            }
            return true;
        }
    }
}
