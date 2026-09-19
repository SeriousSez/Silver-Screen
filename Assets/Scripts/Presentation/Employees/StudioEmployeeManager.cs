using System;
using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain;

namespace SilverScreen.Presentation.Employees
{
    public class StudioEmployeeManager : MonoBehaviour
    {
        private readonly List<Employee> _employees = new List<Employee>();
        private readonly List<EmployeeAgent> _agents = new List<EmployeeAgent>();
        private readonly Dictionary<string, EmployeeAgent> _agentByEmployeeId = new Dictionary<string, EmployeeAgent>();

        public IReadOnlyList<Employee> AllEmployees => _employees;
        public IReadOnlyList<EmployeeAgent> AllAgents => _agents;
        public event Action<Employee> OnEmployeeAdded;

        public void RegisterEmployee(Employee employee, EmployeeAgent agent)
        {
            bool added = !_employees.Contains(employee);
            if (added) _employees.Add(employee);

            if (!_agents.Contains(agent))
            {
                _agents.Add(agent);
            }

            _agentByEmployeeId[employee.Id] = agent;
            agent.BindDomain(employee);
            if (added) OnEmployeeAdded?.Invoke(employee);
        }

        public EmployeeAgent GetAgent(string employeeId)
        {
            _agentByEmployeeId.TryGetValue(employeeId, out var agent);
            return agent;
        }

        public EmployeeAgent GetAgent(Employee employee)
        {
            if (employee == null) return null;
            return GetAgent(employee.Id);
        }
    }
}
