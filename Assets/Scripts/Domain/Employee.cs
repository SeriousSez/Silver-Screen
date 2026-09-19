using System;

namespace SilverScreen.Domain
{
    public class Employee
    {
        public PersonProfile Person { get; }
        public string Id => Person.Id;
        public string Name { get => Person.Name; set => Person.Rename(value); }
        public EmployeeRole Role { get; set; }
        public int Skill { get; set; }
        public int Salary { get; set; }
        public int Morale { get; set; }

        public EmployeeState CurrentState { get; private set; }
        public EmployeeIntent CurrentIntent { get; private set; }

        public event Action<Employee> OnStateChanged;
        public event Action<Employee> OnDetailsChanged;

        public Employee(string id, string name, EmployeeRole role, int skill, int salary, int morale = 80)
            : this(new PersonProfile(id, name, new Time.SimulationDateTime(1900, 1, 1, 0, 0),
                ToProfessionalRole(role), new TalentProfile(role == EmployeeRole.Director ? 25 : skill,
                    role == EmployeeRole.Director ? skill : 25)), role, salary, morale)
        {
        }

        public Employee(PersonProfile person, EmployeeRole role, int salary, int morale = 80)
        {
            Person = person ?? throw new ArgumentNullException(nameof(person));
            Role = role;
            Skill = role == EmployeeRole.Director ? person.Talent.DirectingAbility : person.Talent.ActingAbility;
            Salary = salary;
            Morale = morale;
            CurrentState = EmployeeState.Idle;
            CurrentIntent = EmployeeIntent.None;
        }

        private static ProfessionalRole ToProfessionalRole(EmployeeRole role)
        {
            return role switch
            {
                EmployeeRole.Director => ProfessionalRole.Director,
                EmployeeRole.Extra => ProfessionalRole.Extra,
                EmployeeRole.Crew => ProfessionalRole.Crew,
                _ => ProfessionalRole.Actor
            };
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
