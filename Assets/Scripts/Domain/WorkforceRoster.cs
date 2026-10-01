using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    /// <summary>Employment membership, independent of a person's spawned representation.</summary>
    public sealed class WorkforceRoster
    {
        public PersonPopulation Population { get; } = new PersonPopulation();
        private readonly List<Employee> _employees = new List<Employee>();
        private readonly List<Employee> _former = new List<Employee>();
        private readonly Func<Employee, bool> _reserved;
        public IReadOnlyList<Employee> Employees => _employees;
        public IReadOnlyList<Employee> FormerEmployees => _former;
        public WorkforceRoster(Func<Employee, bool> reserved = null) { _reserved = reserved; }
        public bool Add(Employee employee)
        {
            if (employee == null || !employee.IsEmployed || _employees.Exists(e => e.Id == employee.Id)) return false;
            Population.Register(employee.Person); _employees.Add(employee); return true;
        }
        public bool Contains(Employee employee) => employee != null && _employees.Contains(employee);
        public bool CanChangeEmployment(Employee employee) => Contains(employee) && employee.IsEmployed &&
            (employee.CurrentState == EmployeeState.Idle || employee.CurrentState == EmployeeState.Walking) &&
            (employee.CurrentIntent.Purpose == EmployeeIntentPurpose.None || employee.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander) &&
            !(_reserved?.Invoke(employee) ?? false);
        public bool TryReassign(Employee employee, EmployeeRole role)
        {
            if (!Enum.IsDefined(typeof(EmployeeRole), role) || !CanChangeEmployment(employee) || employee.Role == role) return false;
            employee.ChangeProfession(role); return true;
        }
        public bool TryDismiss(Employee employee)
        {
            if (!CanChangeEmployment(employee)) return false;
            _employees.Remove(employee); _former.Add(employee); employee.EndEmployment(); return true;
        }
    }
}
