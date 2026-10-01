using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public class Employee
    {
        public PersonProfile Person { get; }
        public string Id => Person.Id;
        public string Name { get => Person.Name; set => Person.Rename(value); }
        private EmployeeRole _role;
        private readonly Dictionary<EmployeeRole, int> _professionSkills = new Dictionary<EmployeeRole, int>();
        private readonly Dictionary<EmployeeRole, decimal> _professionExperience = new Dictionary<EmployeeRole, decimal>();
        public EmployeeRole Role { get => _role; set => ChangeProfession(value); }
        public int Skill { get => _professionSkills.TryGetValue(_role, out var skill) ? skill : 0; set => _professionSkills[_role] = Math.Clamp(value, 0, 100); }
        public bool IsEmployed { get; private set; } = true;
        public event Action<Employee, EmployeeRole, EmployeeRole> ProfessionChanged;
        public int Salary { get; set; }
        public int Morale { get; set; }

        public EmployeeState CurrentState { get; private set; }
        public EmployeeIntent CurrentIntent { get; private set; }

        public event Action<Employee> OnStateChanged;
        public event Action<Employee> OnDetailsChanged;

        public Employee(string id, string name, EmployeeRole role, int skill, int salary, int morale = 80)
            : this(new PersonProfile(id, name, new Time.SimulationDateTime(1900, 1, 1, 0, 0),
                ToProfessionalRole(role), new TalentProfile(role == EmployeeRole.Director || role == EmployeeRole.Writer ? 25 : skill,
                    role == EmployeeRole.Director ? skill : 25, null,
                    role == EmployeeRole.Writer ? skill : 25)), role, salary, morale)
        {
        }

        public Employee(PersonProfile person, EmployeeRole role, int salary, int morale = 80)
        {
            Person = person ?? throw new ArgumentNullException(nameof(person));
            _role = role;
            Skill = role == EmployeeRole.Director ? person.Talent.DirectingAbility :
                role == EmployeeRole.Writer ? person.Talent.WritingAbility : person.Talent.ActingAbility;
            Salary = salary;
            Morale = morale;
            CurrentState = EmployeeState.Idle;
            CurrentIntent = EmployeeIntent.None;
        }

        public int GetProfessionSkill(EmployeeRole role) => _professionSkills.TryGetValue(role, out var skill) ? skill : InitialSkill(role);
        public decimal GetProfessionExperience(EmployeeRole role) => _professionExperience.TryGetValue(role, out var experience) ? experience : 0;
        public int AddProfessionExperience(EmployeeRole role, decimal amount, decimal experiencePerSkillPoint)
        {
            if (amount <= 0 || experiencePerSkillPoint <= 0) return 0;
            int current = GetProfessionSkill(role);
            if (current >= 100) return 0;
            decimal accumulated = GetProfessionExperience(role) + amount;
            int gained = Math.Min(100 - current, (int)(accumulated / experiencePerSkillPoint));
            _professionExperience[role] = gained + current >= 100 ? 0 : accumulated - gained * experiencePerSkillPoint;
            if (gained == 0) return 0;
            _professionSkills[role] = current + gained;
            OnDetailsChanged?.Invoke(this);
            return gained;
        }
        private int InitialSkill(EmployeeRole role) => role == EmployeeRole.Director ? Person.Talent.DirectingAbility :
            role == EmployeeRole.Writer ? Person.Talent.WritingAbility :
            role == EmployeeRole.Actor || role == EmployeeRole.Extra ? Person.Talent.ActingAbility : 0;

        public void ChangeProfession(EmployeeRole role)
        {
            if (!Enum.IsDefined(typeof(EmployeeRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            if (_role == role) return;
            var previous = _role;
            if (!_professionSkills.ContainsKey(role)) _professionSkills.Add(role, InitialSkill(role));
            _role = role;
            Person.SetProfessionalRole(ToProfessionalRole(role));
            ProfessionChanged?.Invoke(this, previous, role);
            OnDetailsChanged?.Invoke(this);
        }

        internal void EndEmployment()
        {
            IsEmployed = false;
            SetState(EmployeeState.Idle);
            SetIntent(EmployeeIntent.None);
        }

        private static ProfessionalRole ToProfessionalRole(EmployeeRole role)
        {
            return role switch
            {
                EmployeeRole.Director => ProfessionalRole.Director,
                EmployeeRole.Extra => ProfessionalRole.Extra,
                EmployeeRole.Crew => ProfessionalRole.Crew,
                EmployeeRole.Writer => ProfessionalRole.Writer,
                EmployeeRole.ConstructionWorker => ProfessionalRole.ConstructionWorker,
                EmployeeRole.Groundskeeper => ProfessionalRole.Groundskeeper,
                _ => ProfessionalRole.Actor
            };
        }

        public void SetState(EmployeeState newState)
        {
            bool changed = CurrentState != newState;
            CurrentState = newState;
            Person.Wellbeing.SetActivity(newState switch
            {
                EmployeeState.Working or EmployeeState.Casting or EmployeeState.Rehearsing or EmployeeState.Filming or
                    EmployeeState.Writing or EmployeeState.DevelopingIdea or EmployeeState.Practicing => PersonWellbeingActivity.Working,
                EmployeeState.Resting => PersonWellbeingActivity.Resting,
                EmployeeState.Socializing => PersonWellbeingActivity.Socializing,
                _ => PersonWellbeingActivity.Idle
            });
            if (changed) OnStateChanged?.Invoke(this);
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
