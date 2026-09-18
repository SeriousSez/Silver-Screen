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
    public class MovieProductionCoordinatorTests
    {
        private FakeSimulationTimeService _time;
        private FakeStudioWorldRouter _router;
        private MovieProductionCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _time = new FakeSimulationTimeService();
            _router = new FakeStudioWorldRouter();
            _coordinator = new MovieProductionCoordinator(_time, _router);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
        }

        [Test]
        public void CreateMovie_CreatesProtagonistWithoutSupportingRoles()
        {
            var result = _coordinator.CreateMovie("First Film", "drama", 25000, "Lead", new List<string>());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Project.Roles, Has.Count.EqualTo(1));
            Assert.That(result.Project.Roles[0].RoleType, Is.EqualTo(MovieRoleType.Protagonist));
            Assert.That(result.Project.GenreId, Is.EqualTo("drama"));
            Assert.That(result.Project.GenreDisplayName, Is.EqualTo("Drama"));
        }

        [Test]
        public void CreateMovie_CreatesProtagonistAndTwoSupportingRoles()
        {
            var result = _coordinator.CreateMovie(
                "Ensemble Film",
                "comedy",
                50000,
                "Lead",
                new List<string> { "Friend", "Rival" });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Project.Roles, Has.Count.EqualTo(3));
            Assert.That(result.Project.Roles[0].RoleType, Is.EqualTo(MovieRoleType.Protagonist));
            Assert.That(result.Project.Roles[1].RoleType, Is.EqualTo(MovieRoleType.Supporting));
            Assert.That(result.Project.Roles[2].RoleType, Is.EqualTo(MovieRoleType.Supporting));
        }

        [Test]
        public void AssignActor_RejectsActorAlreadyAssignedToAnotherRole()
        {
            var movie = CreateMovieWithTwoActorsRequired();
            var actor = CreateEmployee("actor", "Alex", EmployeeRole.Actor);

            _coordinator.AssignActorToRole(movie, movie.Roles[0], actor);
            _coordinator.AssignActorToRole(movie, movie.Roles[1], actor);

            Assert.That(movie.Roles[0].AssignedActor, Is.SameAs(actor));
            Assert.That(movie.Roles[1].AssignedActor, Is.Null);
        }

        [Test]
        public void Production_WaitsForEveryCastingTaskAndDirectorBeforeReadyToFilm()
        {
            var movie = CreateMovieWithTwoActorsRequired();
            var lead = CreateEmployee("lead", "Lead Actor", EmployeeRole.Actor);
            var support = CreateEmployee("support", "Support Actor", EmployeeRole.Actor);
            var director = CreateEmployee("director", "Director", EmployeeRole.Director);

            _coordinator.AssignActorToRole(movie, movie.Roles[0], lead);
            _coordinator.AssignActorToRole(movie, movie.Roles[1], support);
            _coordinator.AssignDirector(movie, director);

            _router.CompleteRoute(lead, BuildingType.CastingOffice);
            _time.PassMinutes(60);

            Assert.That(movie.Roles[0].CastingCompleted, Is.True);
            Assert.That(movie.Roles[1].CastingCompleted, Is.False);
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Casting));

            _router.CompleteRoute(support, BuildingType.CastingOffice);
            _time.PassMinutes(60);

            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.ReadyToFilm));
        }

        [Test]
        public void Filming_WaitsUntilDirectorAndEveryActorArriveAtStage()
        {
            var setup = PrepareSingleRoleMovieForStageTravel();

            _router.CompleteRoute(setup.Director, BuildingType.SoundStage);
            Assert.That(setup.Movie.CurrentState, Is.EqualTo(MovieProductionState.ReadyToFilm));

            _router.CompleteRoute(setup.Actor, BuildingType.SoundStage);
            Assert.That(setup.Movie.CurrentState, Is.EqualTo(MovieProductionState.Filming));
        }

        [Test]
        public void Filming_CompletesAfter480SimulatedMinutes()
        {
            var setup = PrepareSingleRoleMovieForFilming();

            _time.PassMinutes(479);
            Assert.That(setup.Movie.CurrentState, Is.EqualTo(MovieProductionState.Filming));

            _time.PassMinutes(1);
            Assert.That(setup.Movie.CurrentState, Is.EqualTo(MovieProductionState.Completed));
            Assert.That(setup.Movie.ProductionProgress, Is.EqualTo(1f));
        }

        [Test]
        public void Completion_ReleasesEveryParticipantAndReturnsThemToIdle()
        {
            var setup = PrepareSingleRoleMovieForFilming();

            _time.PassMinutes(480);

            Assert.That(_router.ReleasedEmployees, Does.Contain(setup.Director));
            Assert.That(_router.ReleasedEmployees, Does.Contain(setup.Actor));
            Assert.That(setup.Director.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Assert.That(setup.Actor.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Assert.That(setup.Director.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
            Assert.That(setup.Actor.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
        }

        [Test]
        public void Completion_StoresProductionResultOnMovie()
        {
            var setup = PrepareSingleRoleMovieForFilming();

            _time.PassMinutes(480);

            Assert.That(setup.Movie.ProductionResult, Is.Not.Null);
            Assert.That(setup.Movie.ProductionResult.OverallQuality, Is.InRange(0, 100));
            Assert.That(setup.Movie.ProductionResult.CastPerformance, Is.EqualTo(setup.Actor.Skill));
            Assert.That(setup.Movie.ProductionResult.Direction, Is.EqualTo(setup.Director.Skill));
            Assert.That(setup.Movie.ProductionResult.ProductionValue, Is.EqualTo(90));
        }

        [Test]
        public void CompletionResult_DoesNotChangeWhenEmployeeSkillChangesLater()
        {
            var setup = PrepareSingleRoleMovieForFilming();
            _time.PassMinutes(480);
            var storedResult = setup.Movie.ProductionResult;
            int storedOverall = storedResult.OverallQuality;
            int storedCast = storedResult.CastPerformance;
            int storedDirection = storedResult.Direction;

            setup.Actor.UpdateDetails(setup.Actor.Name, 1, setup.Actor.Salary, setup.Actor.Morale);
            setup.Director.UpdateDetails(setup.Director.Name, 100, setup.Director.Salary, setup.Director.Morale);

            Assert.That(setup.Movie.ProductionResult, Is.SameAs(storedResult));
            Assert.That(setup.Movie.ProductionResult.OverallQuality, Is.EqualTo(storedOverall));
            Assert.That(setup.Movie.ProductionResult.CastPerformance, Is.EqualTo(storedCast));
            Assert.That(setup.Movie.ProductionResult.Direction, Is.EqualTo(storedDirection));
        }

        [Test]
        public void CreateMovie_DoesNotReplaceInProgressActiveProduction()
        {
            var first = _coordinator.CreateMovie("First", "drama", 25000, "Lead", new List<string>());
            var second = _coordinator.CreateMovie("Second", "comedy", 50000, "Lead", new List<string>());

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Failure, Is.EqualTo(MovieCreationFailure.ActiveProductionInProgress));
            Assert.That(_coordinator.ActiveMovie, Is.SameAs(first.Project));
            Assert.That(_coordinator.Slate.AllProjects, Has.Count.EqualTo(1));

            first.Project.SetState(MovieProductionState.Completed);
            var afterCompletion = _coordinator.CreateMovie("Second", "comedy", 50000, "Lead", new List<string>());

            Assert.That(afterCompletion.Succeeded, Is.True);
            Assert.That(_coordinator.ActiveMovie, Is.SameAs(afterCompletion.Project));
            Assert.That(_coordinator.Slate.AllProjects, Has.Count.EqualTo(2));
        }

        private MovieProject CreateMovieWithTwoActorsRequired()
        {
            return _coordinator.CreateMovie(
                "Two Hander",
                "drama",
                50000,
                "Lead",
                new List<string> { "Support" }).Project;
        }

        private ProductionSetup PrepareSingleRoleMovieForStageTravel()
        {
            var movie = _coordinator.CreateMovie("Production", "action", 100000, "Lead", new List<string>()).Project;
            var actor = CreateEmployee("actor", "Actor", EmployeeRole.Actor);
            var director = CreateEmployee("director", "Director", EmployeeRole.Director);

            _coordinator.AssignActorToRole(movie, movie.Roles[0], actor);
            _router.CompleteRoute(actor, BuildingType.CastingOffice);
            _time.PassMinutes(60);
            _coordinator.AssignDirector(movie, director);

            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.ReadyToFilm));
            return new ProductionSetup(movie, actor, director);
        }

        private ProductionSetup PrepareSingleRoleMovieForFilming()
        {
            var setup = PrepareSingleRoleMovieForStageTravel();
            _router.CompleteRoute(setup.Director, BuildingType.SoundStage);
            _router.CompleteRoute(setup.Actor, BuildingType.SoundStage);
            Assert.That(setup.Movie.CurrentState, Is.EqualTo(MovieProductionState.Filming));
            return setup;
        }

        private static Employee CreateEmployee(string id, string name, EmployeeRole role)
        {
            return new Employee(id, name, role, 75, 100);
        }

        private sealed class ProductionSetup
        {
            public ProductionSetup(MovieProject movie, Employee actor, Employee director)
            {
                Movie = movie;
                Actor = actor;
                Director = director;
            }

            public MovieProject Movie { get; }
            public Employee Actor { get; }
            public Employee Director { get; }
        }

        private sealed class FakeStudioWorldRouter : IStudioWorldRouter
        {
            private readonly List<RouteRequest> _requests = new List<RouteRequest>();

            public List<Employee> ReleasedEmployees { get; } = new List<Employee>();
            public StudioRouteResult NextRouteResult { get; set; } = StudioRouteResult.Started;

            public StudioRouteResult SendEmployeeToBuilding(
                Employee employee,
                BuildingType buildingType,
                EmployeeIntent intent,
                Action onArrival)
            {
                var result = NextRouteResult;
                NextRouteResult = StudioRouteResult.Started;
                if (result == StudioRouteResult.Started)
                {
                    _requests.Add(new RouteRequest(employee, buildingType, onArrival));
                }

                return result;
            }

            public void ReleaseEmployee(Employee employee)
            {
                if (employee == null) return;
                ReleasedEmployees.Add(employee);
                _requests.RemoveAll(r => r.Employee == employee);
            }

            public void CompleteRoute(Employee employee, BuildingType buildingType)
            {
                int index = _requests.FindIndex(r => r.Employee == employee && r.BuildingType == buildingType);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), $"No pending {buildingType} route for {employee.Name}.");
                var request = _requests[index];
                _requests.RemoveAt(index);
                request.OnArrival?.Invoke();
            }

            private sealed class RouteRequest
            {
                public RouteRequest(Employee employee, BuildingType buildingType, Action onArrival)
                {
                    Employee = employee;
                    BuildingType = buildingType;
                    OnArrival = onArrival;
                }

                public Employee Employee { get; }
                public BuildingType BuildingType { get; }
                public Action OnArrival { get; }
            }
        }

        private sealed class FakeSimulationTimeService : ISimulationTimeService
        {
            public SimulationDateTime CurrentTime { get; private set; } = new SimulationDateTime(1930, 1, 1, 8, 0);
            public SimulationSpeed CurrentSpeed { get; private set; } = SimulationSpeed.Normal;
            public bool IsPaused => CurrentSpeed == SimulationSpeed.Paused;
            public float TimeScaleMultiplier => CurrentSpeed == SimulationSpeed.Paused ? 0f : (float)CurrentSpeed;
            public float RealSecondsPerSimulatedMinute { get; set; } = 1f;

            public event Action<SimulationDateTime> OnMinutePassed;
            public event Action<SimulationDateTime> OnHourPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnDayPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnMonthPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnYearPassed { add { } remove { } }
            public event Action<SimulationSpeed> OnSpeedChanged;

            public void SetSpeed(SimulationSpeed speed)
            {
                CurrentSpeed = speed;
                OnSpeedChanged?.Invoke(speed);
            }

            public void TogglePause()
            {
                SetSpeed(IsPaused ? SimulationSpeed.Normal : SimulationSpeed.Paused);
            }

            public void PassMinutes(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var current = CurrentTime;
                    current.AdvanceMinute(out _, out _, out _, out _);
                    CurrentTime = current;
                    OnMinutePassed?.Invoke(CurrentTime);
                }
            }
        }
    }
}

namespace SilverScreen.Tests.EditMode
{
    public sealed class MovieReleaseServiceTests
    {
        private ReleaseTimeService _time;
        private StudioFinances _finances;
        private MovieReleaseService _service;

        [SetUp]
        public void SetUp()
        {
            _time = new ReleaseTimeService();
            _finances = new StudioFinances(_time.CurrentTime);
            _service = new MovieReleaseService(_time, _finances);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public void Release_RequiresCompletedProduction()
        {
            var movie = CreateMovie("draft", BudgetTier.Standard, 60, completed: false);

            var result = _service.ReleaseMovie(movie);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(MovieReleaseFailure.ProductionNotCompleted));
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Draft));
        }

        [Test]
        public void Release_CannotOccurTwice()
        {
            var movie = CreateMovie("once", BudgetTier.Standard, 60);

            Assert.That(_service.ReleaseMovie(movie).Succeeded, Is.True);
            var second = _service.ReleaseMovie(movie);

            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Failure, Is.EqualTo(MovieReleaseFailure.AlreadyReleased));
        }

        [Test]
        public void Release_StoresSimulationDateAndAudienceReception()
        {
            var movie = CreateMovie("dated", BudgetTier.Standard, 73);

            var result = _service.ReleaseMovie(movie);

            Assert.That(movie.ReleaseDate, Is.EqualTo(_time.CurrentTime));
            Assert.That(result.Run.ReleaseDate, Is.EqualTo(_time.CurrentTime));
            Assert.That(result.Run.AudienceReception, Is.EqualTo(73));
            Assert.That(movie.ProductionResult.OverallQuality, Is.EqualTo(73));
        }

        [Test]
        public void Release_TransitionsImmediatelyWithoutCreditingRevenue()
        {
            var movie = CreateMovie("opening-day", BudgetTier.Standard, 60);

            var result = _service.ReleaseMovie(movie);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Released));
            Assert.That(movie.TheatricalRun.CurrentDay, Is.EqualTo(0));
            Assert.That(movie.TheatricalRun.TotalStudioRevenue, Is.EqualTo(Money.Zero));
            Assert.That(_finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.BoxOfficeRevenue), Is.EqualTo(0));
        }

        [Test]
        public void MarketBases_MatchEachBudgetTier()
        {
            Assert.That(TheatricalMarketConfiguration.GetMarketBase(BudgetTier.Low.Id), Is.EqualTo(Money.FromDollars(75000)));
            Assert.That(TheatricalMarketConfiguration.GetMarketBase(BudgetTier.Standard.Id), Is.EqualTo(Money.FromDollars(175000)));
            Assert.That(TheatricalMarketConfiguration.GetMarketBase(BudgetTier.High.Id), Is.EqualTo(Money.FromDollars(350000)));
        }

        [Test]
        public void TargetGross_UsesAudienceReceptionFormula()
        {
            Assert.That(TheatricalMarketConfiguration.CalculateTargetGross(BudgetTier.Standard.Id, 0), Is.EqualTo(Money.FromDollars(43750)));
            Assert.That(TheatricalMarketConfiguration.CalculateTargetGross(BudgetTier.Standard.Id, 60), Is.EqualTo(Money.FromDollars(175000)));
            Assert.That(TheatricalMarketConfiguration.CalculateTargetGross(BudgetTier.Standard.Id, 100), Is.EqualTo(Money.FromDollars(262500)));
        }

        [Test]
        public void WeeklyGrosses_UseConfiguredDistribution()
        {
            var weeks = TheatricalMarketConfiguration.CalculateWeeklyGrosses(Money.FromDollars(100000));

            Assert.That(weeks[0], Is.EqualTo(Money.FromDollars(45000)));
            Assert.That(weeks[1], Is.EqualTo(Money.FromDollars(30000)));
            Assert.That(weeks[2], Is.EqualTo(Money.FromDollars(17000)));
            Assert.That(weeks[3], Is.EqualTo(Money.FromDollars(8000)));
        }

        [Test]
        public void WeeklyGrosses_AssignRemainderDeterministicallyAndSumExactly()
        {
            Money target = Money.FromCents(101);
            var weeks = TheatricalMarketConfiguration.CalculateWeeklyGrosses(target);

            Assert.That(weeks[3], Is.EqualTo(Money.FromCents(9)));
            Assert.That(weeks.Aggregate(Money.Zero, (sum, week) => sum + week), Is.EqualTo(target));
        }

        [Test]
        public void WeeklyRevenue_IsCreditedExactlyOncePerCalendarBoundary()
        {
            var movie = CreateMovie("exactly-once", BudgetTier.Standard, 60);
            _service.ReleaseMovie(movie);

            _time.PassDays(7);
            _time.RepeatCurrentDayEvent();

            var boxOffice = _finances.Transactions
                .Where(t => t.Category == FinancialTransactionCategory.BoxOfficeRevenue)
                .ToList();
            Assert.That(boxOffice, Has.Count.EqualTo(1));
            Assert.That(boxOffice[0].Amount, Is.EqualTo(movie.TheatricalRun.WeeklyGrosses[0]));
            Assert.That(boxOffice[0].ReferenceId, Is.EqualTo(movie.Id));
        }

        [Test]
        public void PausedSimulation_DoesNotAdvanceTheatricalRun()
        {
            var movie = CreateMovie("paused", BudgetTier.Standard, 60);
            _service.ReleaseMovie(movie);
            _time.SetSpeed(SimulationSpeed.Paused);

            _time.PassDays(7);

            Assert.That(movie.TheatricalRun.CurrentDay, Is.EqualTo(0));
            Assert.That(_finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.BoxOfficeRevenue), Is.EqualTo(0));
        }

        [Test]
        public void TheatricalRun_CompletesAfterFourWeeks()
        {
            var movie = CreateMovie("four-weeks", BudgetTier.Standard, 60);
            _service.ReleaseMovie(movie);

            _time.PassDays(27);
            Assert.That(movie.TheatricalRun.IsCompleted, Is.False);

            _time.PassDays(1);
            Assert.That(movie.TheatricalRun.IsCompleted, Is.True);
            Assert.That(movie.TheatricalRun.CurrentDay, Is.EqualTo(28));
            Assert.That(movie.TheatricalRun.WeeksPaid, Is.EqualTo(4));
        }

        [Test]
        public void Completion_StoresImmutableCommercialTotals()
        {
            var movie = CreateMovie("result", BudgetTier.High, 80);
            _service.ReleaseMovie(movie);
            _time.PassDays(28);

            Assert.That(movie.CommercialResult, Is.Not.Null);
            Assert.That(movie.CommercialResult.AudienceReception, Is.EqualTo(80));
            Assert.That(movie.CommercialResult.TotalBoxOfficeGross, Is.EqualTo(movie.TheatricalRun.TargetGross));
            Assert.That(movie.CommercialResult.TotalStudioRevenue, Is.EqualTo(movie.TheatricalRun.TargetGross));
            Assert.That(movie.CommercialResult.ProductionBudget, Is.EqualTo(Money.FromDollars(BudgetTier.High.Amount)));
        }

        [Test]
        public void CommercialProfitLoss_ExcludesUnrelatedStudioTransactions()
        {
            var movie = CreateMovie("profit", BudgetTier.Low, 60);
            _finances.RecordObligation(Money.FromDollars(9999), FinancialTransactionCategory.EmployeeSalary, _time.CurrentTime, "Unrelated payroll");
            _service.ReleaseMovie(movie);
            _time.PassDays(28);

            Money expected = movie.CommercialResult.TotalStudioRevenue - Money.FromDollars(BudgetTier.Low.Amount);
            Assert.That(movie.CommercialResult.CommercialProfitLoss, Is.EqualTo(expected));
        }

        [Test]
        public void ReleaseService_AdvancesMultipleRunsByMovieIdentity()
        {
            var first = CreateMovie("first", BudgetTier.Low, 40);
            var second = CreateMovie("second", BudgetTier.High, 90);
            _service.ReleaseMovie(first);
            _service.ReleaseMovie(second);

            _time.PassDays(7);

            Assert.That(_service.GetTheatricalRun(first.Id), Is.SameAs(first.TheatricalRun));
            Assert.That(_service.GetTheatricalRun(second.Id), Is.SameAs(second.TheatricalRun));
            Assert.That(first.TheatricalRun.WeeksPaid, Is.EqualTo(1));
            Assert.That(second.TheatricalRun.WeeksPaid, Is.EqualTo(1));
            Assert.That(_finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.BoxOfficeRevenue), Is.EqualTo(2));
        }

        private static MovieProject CreateMovie(string id, BudgetTier tier, int quality, bool completed = true)
        {
            var movie = new MovieProject(
                id,
                "Test Film " + id,
                "drama",
                "Drama",
                tier.Amount,
                new SimulationDateTime(1930, 1, 1, 8, 0),
                tier.Id);
            movie.TrySetProductionResult(new MovieProductionResult(quality, quality, quality, quality));
            if (completed) movie.SetState(MovieProductionState.Completed);
            return movie;
        }

        private sealed class ReleaseTimeService : ISimulationTimeService
        {
            public SimulationDateTime CurrentTime { get; private set; } = new SimulationDateTime(1930, 1, 1, 8, 0);
            public SimulationSpeed CurrentSpeed { get; private set; } = SimulationSpeed.Normal;
            public bool IsPaused => CurrentSpeed == SimulationSpeed.Paused;
            public float TimeScaleMultiplier => IsPaused ? 0f : (float)CurrentSpeed;
            public float RealSecondsPerSimulatedMinute { get; set; } = 1f;

            public event Action<SimulationDateTime> OnMinutePassed { add { } remove { } }
            public event Action<SimulationDateTime> OnHourPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnDayPassed;
            public event Action<SimulationDateTime> OnMonthPassed { add { } remove { } }
            public event Action<SimulationDateTime> OnYearPassed { add { } remove { } }
            public event Action<SimulationSpeed> OnSpeedChanged;

            public void SetSpeed(SimulationSpeed speed)
            {
                CurrentSpeed = speed;
                OnSpeedChanged?.Invoke(speed);
            }

            public void TogglePause()
            {
                SetSpeed(IsPaused ? SimulationSpeed.Normal : SimulationSpeed.Paused);
            }

            public void PassDays(int count)
            {
                if (IsPaused) return;

                for (int day = 0; day < count; day++)
                {
                    for (int minute = 0; minute < 1440; minute++)
                    {
                        var current = CurrentTime;
                        current.AdvanceMinute(out _, out _, out _, out _);
                        CurrentTime = current;
                    }

                    OnDayPassed?.Invoke(CurrentTime);
                }
            }

            public void RepeatCurrentDayEvent()
            {
                OnDayPassed?.Invoke(CurrentTime);
            }
        }
    }
}

namespace SilverScreen.Tests.EditMode
{
    public sealed class MovieQualityCalculatorTests
    {
        private MovieQualityCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new MovieQualityCalculator();
        }

        [Test]
        public void CastPerformance_WeightsProtagonistTwiceSupportingRole()
        {
            var movie = CreateMovie(BudgetTier.Standard);
            AddActor(movie, "lead", MovieRoleType.Protagonist, 80);
            AddActor(movie, "support", MovieRoleType.Supporting, 50);

            Assert.That(_calculator.Calculate(movie).CastPerformance, Is.EqualTo(70));
        }

        [Test]
        public void CastPerformance_HandlesMultipleSupportingActors()
        {
            var movie = CreateMovie(BudgetTier.Standard);
            AddActor(movie, "lead", MovieRoleType.Protagonist, 80);
            AddActor(movie, "support-1", MovieRoleType.Supporting, 50);
            AddActor(movie, "support-2", MovieRoleType.Supporting, 20);

            Assert.That(_calculator.Calculate(movie).CastPerformance, Is.EqualTo(58));
        }

        [Test]
        public void Direction_EqualsAssignedDirectorSkill()
        {
            var movie = CreateMovie(BudgetTier.Standard);
            movie.AssignDirector(new Employee("director", "Director", EmployeeRole.Director, 82, 3000));

            Assert.That(_calculator.Calculate(movie).Direction, Is.EqualTo(82));
        }

        [Test]
        public void LowBudget_HasProductionValueForty()
        {
            Assert.That(_calculator.Calculate(CreateMovie(BudgetTier.Low)).ProductionValue, Is.EqualTo(40));
        }

        [Test]
        public void StandardBudget_HasProductionValueSixtyFive()
        {
            Assert.That(_calculator.Calculate(CreateMovie(BudgetTier.Standard)).ProductionValue, Is.EqualTo(65));
        }

        [Test]
        public void HighBudget_HasProductionValueNinety()
        {
            Assert.That(_calculator.Calculate(CreateMovie(BudgetTier.High)).ProductionValue, Is.EqualTo(90));
        }

        [Test]
        public void OverallQuality_UsesConfiguredIntegerWeightsAndRounding()
        {
            var movie = CreateMovie(BudgetTier.Standard);
            AddActor(movie, "lead", MovieRoleType.Protagonist, 80);
            AddActor(movie, "support", MovieRoleType.Supporting, 50);
            movie.AssignDirector(new Employee("director", "Director", EmployeeRole.Director, 82, 3000));

            var result = _calculator.Calculate(movie);

            Assert.That(result.CastPerformance, Is.EqualTo(70));
            Assert.That(result.Direction, Is.EqualTo(82));
            Assert.That(result.ProductionValue, Is.EqualTo(65));
            Assert.That(result.OverallQuality, Is.EqualTo(73));
        }

        [Test]
        public void Scores_AreClampedToZeroThroughOneHundred()
        {
            var movie = CreateMovie(BudgetTier.High);
            AddActor(movie, "lead", MovieRoleType.Protagonist, 150);
            movie.AssignDirector(new Employee("director", "Director", EmployeeRole.Director, -20, 3000));

            var result = _calculator.Calculate(movie);

            Assert.That(result.CastPerformance, Is.EqualTo(100));
            Assert.That(result.Direction, Is.EqualTo(0));
            Assert.That(result.ProductionValue, Is.EqualTo(90));
            Assert.That(result.OverallQuality, Is.InRange(0, 100));
        }

        private static MovieProject CreateMovie(BudgetTier tier)
        {
            return new MovieProject(
                "movie",
                "Test Film",
                "drama",
                "Drama",
                tier.Amount,
                new SimulationDateTime(1930, 1, 1, 8, 0),
                tier.Id);
        }

        private static void AddActor(MovieProject movie, string id, MovieRoleType roleType, int skill)
        {
            var role = new MovieRole(id + "-role", roleType, id);
            movie.AddRole(role);
            movie.AssignActorToRole(
                role.Id,
                new Employee(id, id, EmployeeRole.Actor, skill, 2500));
        }
    }
}
