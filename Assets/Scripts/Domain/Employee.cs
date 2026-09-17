using System;

namespace SilverScreen.Domain
{
    public class Employee
    {
        public string Id { get; }
        public string Name { get; set; }
        public EmployeeRole Role { get; set; }
        public int Skill { get; set; }
        public int Salary { get; set; }
        public int Morale { get; set; }

        public EmployeeState CurrentState { get; private set; }
        public EmployeeIntent CurrentIntent { get; private set; }

        public event Action<Employee> OnStateChanged;
        public event Action<Employee> OnDetailsChanged;

        public Employee(string id, string name, EmployeeRole role, int skill, int salary, int morale = 80)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Name = name;
            Role = role;
            Skill = skill;
            Salary = salary;
            Morale = morale;
            CurrentState = EmployeeState.Idle;
            CurrentIntent = EmployeeIntent.None;
        }

        public void SetState(EmployeeState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(this);
        }

        public void SetIntent(EmployeeIntent intent)
        {
            CurrentIntent = intent ?? EmployeeIntent.None;
            OnDetailsChanged?.Invoke(this);
        }

        public void UpdateDetails(string name, int skill, int salary, int morale)
        {
            Name = name;
            Skill = skill;
            Salary = salary;
            Morale = morale;
            OnDetailsChanged?.Invoke(this);
        }
    }
}
