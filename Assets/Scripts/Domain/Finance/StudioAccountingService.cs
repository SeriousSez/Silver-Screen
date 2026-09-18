using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Finance
{
    public sealed class StudioAccountingService : IDisposable
    {
        private readonly ISimulationTimeService _timeService;
        private readonly IStudioFinanceService _finances;
        private readonly Func<IEnumerable<Employee>> _employeeProvider;
        private readonly Func<IEnumerable<BuildingUpkeepDefinition>> _buildingProvider;
        private readonly HashSet<string> _processedMonths = new HashSet<string>();

        public StudioAccountingService(
            ISimulationTimeService timeService,
            IStudioFinanceService finances,
            Func<IEnumerable<Employee>> employeeProvider,
            Func<IEnumerable<BuildingUpkeepDefinition>> buildingProvider)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _finances = finances ?? throw new ArgumentNullException(nameof(finances));
            _employeeProvider = employeeProvider ?? throw new ArgumentNullException(nameof(employeeProvider));
            _buildingProvider = buildingProvider ?? throw new ArgumentNullException(nameof(buildingProvider));

            _timeService.OnMonthPassed += HandleMonthPassed;
        }

        public Money EstimatedMonthlyPayroll => SumPayroll();
        public Money EstimatedMonthlyBuildingUpkeep => SumBuildingUpkeep();
        public Money EstimatedMonthlyOperatingCosts => EstimatedMonthlyPayroll + EstimatedMonthlyBuildingUpkeep;

        public bool ProcessMonthlyAccounting(SimulationDateTime date)
        {
            string monthKey = $"{date.Year:D4}-{date.Month:D2}";
            if (!_processedMonths.Add(monthKey)) return false;

            foreach (var employee in _employeeProvider())
            {
                if (employee == null || employee.Salary <= 0) continue;

                _finances.RecordObligation(
                    Money.FromDollars(employee.Salary),
                    FinancialTransactionCategory.EmployeeSalary,
                    date,
                    $"Employee Salary — {employee.Name}",
                    employee.Id);
            }

            foreach (var building in _buildingProvider())
            {
                if (building == null || building.MonthlyCost <= Money.Zero) continue;

                _finances.RecordObligation(
                    building.MonthlyCost,
                    FinancialTransactionCategory.BuildingUpkeep,
                    date,
                    $"Building Upkeep — {building.DisplayName}",
                    building.BuildingType.ToString());
            }

            return true;
        }

        public void Dispose()
        {
            _timeService.OnMonthPassed -= HandleMonthPassed;
        }

        private void HandleMonthPassed(SimulationDateTime date)
        {
            ProcessMonthlyAccounting(date);
        }

        private Money SumPayroll()
        {
            Money total = Money.Zero;
            foreach (var employee in _employeeProvider())
            {
                if (employee != null && employee.Salary > 0)
                {
                    total += Money.FromDollars(employee.Salary);
                }
            }

            return total;
        }

        private Money SumBuildingUpkeep()
        {
            Money total = Money.Zero;
            foreach (var building in _buildingProvider())
            {
                if (building != null && building.MonthlyCost > Money.Zero)
                {
                    total += building.MonthlyCost;
                }
            }

            return total;
        }
    }
}
