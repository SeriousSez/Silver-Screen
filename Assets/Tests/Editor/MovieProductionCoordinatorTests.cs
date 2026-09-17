using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;
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
