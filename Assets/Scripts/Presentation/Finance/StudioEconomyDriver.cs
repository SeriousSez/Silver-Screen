using SilverScreen.Domain.Finance;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;

namespace SilverScreen.Presentation.Finance
{
    public sealed class StudioEconomyDriver : MonoBehaviour
    {
        [SerializeField] private SimulationTimeDriver _timeDriver;
        [SerializeField] private StudioEmployeeManager _employeeManager;

        private StudioFinances _finances;
        private StudioAccountingService _accounting;

        public IStudioFinanceService FinanceService
        {
            get
            {
                EnsureInitialized();
                return _finances;
            }
        }

        public StudioAccountingService AccountingService
        {
            get
            {
                EnsureInitialized();
                return _accounting;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Initialize(SimulationTimeDriver timeDriver, StudioEmployeeManager employeeManager)
        {
            if (_finances != null) return;

            _timeDriver = timeDriver;
            _employeeManager = employeeManager;
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_finances != null) return;

            if (_timeDriver == null)
            {
                _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
            }

            if (_employeeManager == null)
            {
                _employeeManager = FindAnyObjectByType<StudioEmployeeManager>();
            }

            if (_timeDriver == null || _employeeManager == null) return;

            _finances = new StudioFinances(_timeDriver.TimeService.CurrentTime);
            _accounting = new StudioAccountingService(
                _timeDriver.TimeService,
                _finances,
                () => _employeeManager.AllEmployees,
                () => BuildingUpkeepDefinition.Defaults);

            if (GetComponent<StudioFinanceUI>() == null)
            {
                gameObject.AddComponent<StudioFinanceUI>();
            }
        }

        private void OnDestroy()
        {
            _accounting?.Dispose();
        }
    }
}
