using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class StudioFinanceTests
    {
        private static readonly SimulationDateTime StartDate = new SimulationDateTime(1930, 1, 1, 8, 0);

        [Test]
        public void StudioStartsWithDefaultCapital()
        {
            var finances = new StudioFinances(StartDate);

            Assert.That(finances.CurrentCash, Is.EqualTo(Money.FromDollars(250000)));
            Assert.That(finances.TotalIncome, Is.EqualTo(Money.FromDollars(250000)));
            Assert.That(finances.TotalExpenses, Is.EqualTo(Money.Zero));
        }

        [Test]
        public void CreatingStandardBudgetMovieLeavesTwoHundredThousand()
        {
            var setup = CreateProductionSetup();

            var result = setup.Coordinator.CreateMovie(
                "Nightfall",
                "thriller",
                50000,
                "Lead",
                new List<string>());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(setup.Finances.CurrentCash, Is.EqualTo(Money.FromDollars(200000)));
            setup.Coordinator.Dispose();
        }

        [Test]
        public void MovieBudgetIsDeductedExactlyOnce()
        {
            var setup = CreateProductionSetup();

            var result = setup.Coordinator.CreateMovie(
                "One Charge",
                "drama",
                50000,
                "Lead",
                new List<string>());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                setup.Finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.ProductionBudget),
                Is.EqualTo(1));
            Assert.That(setup.Finances.TotalExpenses, Is.EqualTo(Money.FromDollars(50000)));
            setup.Coordinator.Dispose();
        }

        [Test]
        public void MovieCreationFailsWhenBudgetExceedsCash()
        {
            var setup = CreateProductionSetup(Money.FromDollars(25000));

            var result = setup.Coordinator.CreateMovie(
                "Too Expensive",
                "action",
                50000,
                "Lead",
                new List<string>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(MovieCreationFailure.InsufficientFunds));
            Assert.That(setup.Coordinator.ActiveMovie, Is.Null);
            setup.Coordinator.Dispose();
        }

        [Test]
        public void FailedMovieCreationDoesNotDeductMoney()
        {
            var setup = CreateProductionSetup(Money.FromDollars(25000));

            setup.Coordinator.CreateMovie(
                "Too Expensive",
                "action",
                100000,
                "Lead",
                new List<string>());

            Assert.That(setup.Finances.CurrentCash, Is.EqualTo(Money.FromDollars(25000)));
            Assert.That(setup.Finances.TotalExpenses, Is.EqualTo(Money.Zero));
            Assert.That(
                setup.Finances.Transactions.Any(t => t.Category == FinancialTransactionCategory.ProductionBudget),
                Is.False);
            setup.Coordinator.Dispose();
        }

        [Test]
        public void MonthlySalariesChargeEachEmployee()
        {
            var time = new FakeSimulationTimeService();
            var finances = new StudioFinances(StartDate);
            var employees = new[]
            {
                new Employee("actor", "Clara Hayes", EmployeeRole.Actor, 70, 2500),
                new Employee("director", "Stanley Hawk", EmployeeRole.Director, 80, 3000)
            };

            using var accounting = new StudioAccountingService(
                time,
                finances,
                () => employees,
                () => Array.Empty<BuildingUpkeepDefinition>());

            time.RaiseMonth(new SimulationDateTime(1930, 2, 1, 0, 0));

            Assert.That(finances.CurrentCash, Is.EqualTo(Money.FromDollars(244500)));
            Assert.That(
                finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.EmployeeSalary),
                Is.EqualTo(2));
        }

        [Test]
        public void MonthlyBuildingUpkeepUsesConfiguredDefinitions()
        {
            var time = new FakeSimulationTimeService();
            var finances = new StudioFinances(StartDate);

            using var accounting = new StudioAccountingService(
                time,
                finances,
                () => Array.Empty<Employee>(),
                () => BuildingUpkeepDefinition.Defaults);

            time.RaiseMonth(new SimulationDateTime(1930, 2, 1, 0, 0));

            Assert.That(finances.CurrentCash, Is.EqualTo(Money.FromDollars(244000)));
            Assert.That(accounting.EstimatedMonthlyBuildingUpkeep, Is.EqualTo(Money.FromDollars(6000)));
            Assert.That(
                finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.BuildingUpkeep),
                Is.EqualTo(3));
        }

        [Test]
        public void MonthlyAccountingCannotRunTwiceForSameMonth()
        {
            var time = new FakeSimulationTimeService();
            var finances = new StudioFinances(StartDate);
            var employee = new Employee("actor", "Clara Hayes", EmployeeRole.Actor, 70, 2500);

            using var accounting = new StudioAccountingService(
                time,
                finances,
                () => new[] { employee },
                () => Array.Empty<BuildingUpkeepDefinition>());

            var february = new SimulationDateTime(1930, 2, 1, 0, 0);
            time.RaiseMonth(february);
            time.RaiseMonth(february);

            Assert.That(finances.CurrentCash, Is.EqualTo(Money.FromDollars(247500)));
            Assert.That(
                finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.EmployeeSalary),
                Is.EqualTo(1));
        }

        [Test]
        public void MonthlyObligationsMayPushCashNegative()
        {
            var time = new FakeSimulationTimeService();
            var finances = new StudioFinances(StartDate, Money.FromDollars(1000));
            var employee = new Employee("actor", "Clara Hayes", EmployeeRole.Actor, 70, 2500);

            using var accounting = new StudioAccountingService(
                time,
                finances,
                () => new[] { employee },
                () => Array.Empty<BuildingUpkeepDefinition>());

            time.RaiseMonth(new SimulationDateTime(1930, 2, 1, 0, 0));

            Assert.That(finances.CurrentCash, Is.EqualTo(Money.FromDollars(-1500)));
        }

        [Test]
        public void TransactionsContainExpectedAmountsCategoriesAndReferences()
        {
            var finances = new StudioFinances(StartDate);
            var transactionDate = new SimulationDateTime(1930, 1, 3, 12, 0);

            bool recorded = finances.TryRecordExpense(
                Money.FromDollars(50000),
                FinancialTransactionCategory.ProductionBudget,
                transactionDate,
                "Production: Nightfall",
                "movie-1");

            var transaction = finances.Transactions.Last();
            Assert.That(recorded, Is.True);
            Assert.That(transaction.Id, Is.Not.Empty);
            Assert.That(transaction.Date, Is.EqualTo(transactionDate));
            Assert.That(transaction.Amount, Is.EqualTo(Money.FromDollars(-50000)));
            Assert.That(transaction.Category, Is.EqualTo(FinancialTransactionCategory.ProductionBudget));
            Assert.That(transaction.Description, Is.EqualTo("Production: Nightfall"));
            Assert.That(transaction.ReferenceId, Is.EqualTo("movie-1"));
        }

        private static ProductionSetup CreateProductionSetup(Money? startingCash = null)
        {
            var time = new FakeSimulationTimeService();
            var finances = new StudioFinances(StartDate, startingCash);
            var coordinator = new MovieProductionCoordinator(
                time,
                new ImmediateRouter(),
                finances: finances);
            return new ProductionSetup(coordinator, finances);
        }

        private sealed class ProductionSetup
        {
            public ProductionSetup(MovieProductionCoordinator coordinator, StudioFinances finances)
            {
                Coordinator = coordinator;
                Finances = finances;
            }

            public MovieProductionCoordinator Coordinator { get; }
            public StudioFinances Finances { get; }
        }

        private sealed class ImmediateRouter : IStudioWorldRouter
        {
            public StudioRouteResult SendEmployeeToBuilding(
                Employee employee,
                BuildingType buildingType,
                EmployeeIntent intent,
                Action onArrival)
            {
                onArrival?.Invoke();
                return StudioRouteResult.Started;
            }

            public StudioRouteResult SendEmployeeToProductionStation(
                Employee employee,
                BuildingType buildingType,
                ProductionStationType stationType,
                EmployeeIntent intent,
                Action onArrival)
            {
                onArrival?.Invoke();
                return StudioRouteResult.Started;
            }
            public StudioRouteResult SendEmployeeToSceneMark(
                Employee employee,
                BuildingType buildingType,
                ActorSceneMarkType markType,
                EmployeeIntent intent,
                Action onArrival)
            {
                onArrival?.Invoke();
                return StudioRouteResult.Started;
            }
            public StudioRouteResult StartSlateSequence(
                BuildingType buildingType,
                Action onCompleted,
                Action<StudioRouteResult> onFailed)
            {
                onCompleted?.Invoke();
                return StudioRouteResult.Started;
            }
            public void ReleaseEmployee(Employee employee)
            {
            }
        }

        private sealed class FakeSimulationTimeService : ISimulationTimeService
        {
            public SimulationDateTime CurrentTime { get; private set; } = StartDate;
            public SimulationSpeed CurrentSpeed { get; private set; } = SimulationSpeed.Normal;
            public bool IsPaused => CurrentSpeed == SimulationSpeed.Paused;
            public float TimeScaleMultiplier => IsPaused ? 0f : 1f;
            public float RealSecondsPerSimulatedMinute { get; set; } = 1f;

            public event Action<SimulationDateTime> OnMinutePassed { add { } remove { } }
            public event Action<SimulationDateTime> OnHourPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnDayPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnMonthPassed;
            public event Action<SimulationDateTime> OnYearPassed { add { } remove { } }
            public event Action<SimulationSpeed> OnSpeedChanged { add { } remove { } }

            public void SetSpeed(SimulationSpeed speed)
            {
                CurrentSpeed = speed;
            }

            public void TogglePause()
            {
                CurrentSpeed = IsPaused ? SimulationSpeed.Normal : SimulationSpeed.Paused;
            }

            public void RaiseMonth(SimulationDateTime date)
            {
                CurrentTime = date;
                OnMonthPassed?.Invoke(date);
            }
        }
    }
}
