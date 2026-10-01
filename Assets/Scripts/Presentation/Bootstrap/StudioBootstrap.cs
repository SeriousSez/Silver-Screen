using System;
using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.UI;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Writing;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Tutorial;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Camera;

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
        [Header("Player-facing session bootstrap")]
        [SerializeField] private bool _startNewStudio;
        [SerializeField] private NewStudioOptions _newStudioOptions = new NewStudioOptions();
        [Tooltip("Art-only objects belonging to the populated developer scenario. Hidden only when starting a new studio.")]
        [SerializeField] private Transform[] _populatedScenarioArt = Array.Empty<Transform>();

        public StudioIdentity StudioIdentity { get; private set; }
        public RecruitmentDriver Recruitment { get; private set; }
        public StudioConstructionDriver Construction => _construction;
        private StudioConstructionDriver _construction;

        private void Awake()
        {
            if (_startNewStudio)
                foreach (var root in _populatedScenarioArt)
                    if (root != null) root.gameObject.SetActive(false);
            StudioIdentity = new StudioIdentity();
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

            if (_startNewStudio && _timeDriver != null)
                _timeDriver.InitializeNewStudioSession(new SimulationDateTime(1930, 1, 1, 8, 0));

            InitializeEmployees();
            InitializeEconomy();
            if (_timeDriver != null)
            {
                var guidance = GetComponent<StudioGuidanceDriver>() ?? gameObject.AddComponent<StudioGuidanceDriver>();
                guidance.Initialize(_startNewStudio ? _newStudioOptions : new NewStudioOptions { TutorialEnabled = false },
                    _startNewStudio, _timeDriver, _employeeManager, GetComponent<ScreenplayWritingDriver>());
                if (_startNewStudio)
                {
                    _construction = GetComponent<StudioConstructionDriver>() ?? gameObject.AddComponent<StudioConstructionDriver>();
                    _construction.Initialize(_timeDriver, _employeeManager, GetComponent<ScreenplayWritingDriver>(), guidance);
                }
            }
            InitializeStudioIdentityPresentation();
        }

        private void Start()
        {
            if (!_startNewStudio || _construction?.Lot == null) return;
            var cameraController = FindAnyObjectByType<StudioCameraController>();
            if (cameraController == null) return;
            var lot = _construction.Lot;
            var lotCenter = new Vector3(lot.Buildable.X, 0f, lot.Buildable.Z);
            cameraController.FrameManagementView(Vector3.Lerp(lot.ServicePosition, lotCenter, .2f), 34f);
        }

        private void InitializeEmployees()
        {
            if (_employeeManager == null) return;
            if (_startNewStudio)
            {
                foreach (var config in _initialEmployees)
                    if (config.Agent != null) config.Agent.gameObject.SetActive(false);
                return;
            }

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

            var hud = GetComponent<StudioHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<StudioHud>();
            }
            hud.Initialize(StudioIdentity);

            var recruitment = GetComponent<RecruitmentDriver>() ?? gameObject.AddComponent<RecruitmentDriver>();
            Recruitment = recruitment;
            if (_startNewStudio)
            {
                recruitment.ConfigureGenerationSeed(Guid.NewGuid().GetHashCode());
                // Operational recruitment categories own their one-time starter intake.
            }
            var writingDriver = GetComponent<ScreenplayWritingDriver>();
            if (writingDriver == null) writingDriver = gameObject.AddComponent<ScreenplayWritingDriver>();
            var setConstruction = GetComponent<SpecializedSetConstructionDriver>();
            if (setConstruction == null) setConstruction = gameObject.AddComponent<SpecializedSetConstructionDriver>();
            setConstruction.Initialize(writingDriver);
            if (GetComponent<OutsideWorldPrototype>() == null)
            {
                gameObject.AddComponent<OutsideWorldPrototype>();
            }
        }

        private void InitializeStudioIdentityPresentation()
        {
            var signs = FindObjectsByType<StudioNameSign>(FindObjectsInactive.Include);
            foreach (var sign in signs)
            {
                sign.Initialize(StudioIdentity);
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
