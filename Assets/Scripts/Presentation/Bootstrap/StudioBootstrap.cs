using System;
using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.UI;

namespace SilverScreen.Presentation.Bootstrap
{
    public class StudioBootstrap : MonoBehaviour
    {
        [Serializable]
        public class InitialEmployeeConfig
        {
            public string Name;
            public EmployeeRole Role;
            public int Skill;
            public int Salary;
            public int Morale = 85;
            public EmployeeAgent Agent;
        }

        [SerializeField] private StudioEmployeeManager _employeeManager;
        [SerializeField] private SimulationTimeDriver _timeDriver;
        [SerializeField] private List<InitialEmployeeConfig> _initialEmployees = new List<InitialEmployeeConfig>();

        private void Awake()
        {
            if (_employeeManager == null)
            {
                _employeeManager = GetComponent<StudioEmployeeManager>();
                if (_employeeManager == null)
                {
                    _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
                }
            }

            if (_timeDriver == null)
            {
                _timeDriver = GetComponent<SimulationTimeDriver>();
                if (_timeDriver == null)
                {
                    _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
                }
            }

            InitializeEmployees();
            InitializeEconomy();
        }

        private void InitializeEmployees()
        {
            if (_employeeManager == null) return;

            var timeService = _timeDriver != null ? _timeDriver.TimeService : null;

            foreach (var config in _initialEmployees)
            {
                if (config.Agent == null) continue;

                var domainEmployee = new Employee(
                    id: Guid.NewGuid().ToString(),
                    name: config.Name,
                    role: config.Role,
                    skill: config.Skill,
                    salary: config.Salary,
                    morale: config.Morale
                );

                _employeeManager.RegisterEmployee(domainEmployee, config.Agent);

                if (timeService != null)
                {
                    config.Agent.BindTimeService(timeService);
                }
            }
        }

        private void InitializeEconomy()
        {
            var economyDriver = GetComponent<StudioEconomyDriver>();
            if (economyDriver == null)
            {
                economyDriver = gameObject.AddComponent<StudioEconomyDriver>();
            }

            economyDriver.Initialize(_timeDriver, _employeeManager);

            if (GetComponent<StudioHud>() == null)
            {
                gameObject.AddComponent<StudioHud>();
            }
        }

        public void SetInitialEmployees(List<InitialEmployeeConfig> configs)
        {
            _initialEmployees = configs;
        }

        public void SetTimeDriver(SimulationTimeDriver timeDriver)
        {
            _timeDriver = timeDriver;
        }
    }
}
