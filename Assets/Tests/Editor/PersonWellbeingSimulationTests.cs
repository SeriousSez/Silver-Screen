using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonWellbeingSimulationTests
    {
        [Test]
        public void IdleIncreasesBoredomAndReducesStressWithoutRestoringEnergy()
        {
            var clock = new SimulationClock();
            var person = Person();
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 79, 9, 14, 70);
        }

        [Test]
        public void WorkingDecreasesEnergyAndBoredomWhileIncreasingStress()
        {
            var clock = new SimulationClock();
            var person = Person();
            person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 76, 12, 5, 70);
        }

        [Test]
        public void RestingRestoresEnergyReducesStressAndImprovesMood()
        {
            var clock = new SimulationClock();
            var person = Person();
            person.Wellbeing.SetActivity(PersonWellbeingActivity.Resting);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 88, 6, 11, 71);
        }

        [Test]
        public void SocializingReducesStressAndBoredomAndImprovesMood()
        {
            var clock = new SimulationClock();
            var person = Person();
            person.Wellbeing.SetActivity(PersonWellbeingActivity.Socializing);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 79, 7, 5, 72);
        }

        [Test]
        public void RecreationReducesStressAndBoredomAndImprovesMood()
        {
            var clock = new SimulationClock();
            var person = Person();
            person.Wellbeing.SetActivity(PersonWellbeingActivity.Recreation);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 79, 6, 3, 72);
        }

        [Test]
        public void FractionalProgressAccumulatesAcrossMinuteUpdates()
        {
            var clock = new SimulationClock();
            var person = Person();
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 15);
            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(11));
            AdvanceMinutes(clock, 45);

            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(14));
        }

        [Test]
        public void ActivityChangeAffectsOnlyProgressAfterTheChange()
        {
            var clock = new SimulationClock();
            var person = Person();
            var employee = new Employee(person, EmployeeRole.Actor, 1000);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 15);
            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(11));
            employee.SetState(EmployeeState.Working);
            AdvanceMinutes(clock, 45);

            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(8));
            Assert.That(person.Wellbeing.Energy, Is.EqualTo(77));
            Assert.That(person.Wellbeing.Stress, Is.EqualTo(11));
        }

        [Test]
        public void ActivityIsPreservedByWellbeingSnapshot()
        {
            var person = Person();
            person.Wellbeing.SetActivity(PersonWellbeingActivity.Recreation);
            var snapshot = person.CaptureWellbeing();
            var restored = Person("restored");

            restored.RestoreWellbeing(snapshot);

            Assert.That(restored.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Recreation));
        }

        [Test]
        public void PausedClockDoesNotProgressWellbeing()
        {
            var clock = new SimulationClock();
            var start = clock.Now;
            var person = Person();
            using var simulation = Simulation(clock, person);
            clock.SetSpeed(SimulationSpeed.Paused);

            clock.AdvanceTo(clock.Now + SimulationDuration.FromMinutes(60));

            AssertState(person, 80, 10, 10, 70);
            Assert.That(clock.Now, Is.EqualTo(start));
        }

        [TestCase(SimulationSpeed.Normal, 60)]
        [TestCase(SimulationSpeed.Fast, 30)]
        [TestCase(SimulationSpeed.VeryFast, 20)]
        public void EqualSimulationMinutesProduceEquivalentResults(SimulationSpeed speed, double realSeconds)
        {
            var clock = new SimulationClock();
            var person = Person();
            using var simulation = Simulation(clock, person);
            var start = clock.Now;
            clock.SetSpeed(speed);

            clock.Advance(realSeconds);

            Assert.That((clock.Now - start).Seconds, Is.EqualTo(3600));
            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(14));
            Assert.That(person.Wellbeing.Energy, Is.EqualTo(79));
        }

        [Test]
        public void ProgressionClampsAllDimensionsAtTheirBoundaries()
        {
            var clock = new SimulationClock();
            var high = Person("high");
            high.Wellbeing.Restore(new PersonWellbeingSnapshot { Energy = 99, Stress = 1, Boredom = 99, Mood = 99 });
            high.Wellbeing.SetActivity(PersonWellbeingActivity.Resting);
            var low = Person("low");
            low.Wellbeing.Restore(new PersonWellbeingSnapshot { Energy = 1, Stress = 99, Boredom = 1, Mood = 1 });
            low.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
            var rates = new PersonWellbeingRates
            {
                RestingEnergyPerHour = 1000,
                RestingStressPerHour = -1000,
                RestingBoredomPerHour = 1000,
                RestingMoodPerHour = 1000,
                WorkingEnergyPerHour = -1000,
                WorkingStressPerHour = 1000,
                WorkingBoredomPerHour = -1000,
                WorkingMoodPerHour = -1000
            };
            using var simulation = new PersonWellbeingSimulation(clock, rates);
            simulation.Register(high);
            simulation.Register(low);

            AdvanceMinutes(clock, 60);

            AssertState(high, 100, 0, 100, 100);
            AssertState(low, 0, 100, 0, 0);
        }

        [Test]
        public void ApplicantHiringPreservesWellbeingAndProgression()
        {
            var clock = new SimulationClock();
            var person = Person("applicant", ProfessionalRole.Actor);
            var candidate = new Candidate(person, 1000);
            using var recruitment = new RecruitmentCoordinator(clock,
                new FixedCandidateGenerator(candidate), new ImmediateRouter());
            var waitingCandidate = recruitment.TryGenerateArrival();
            using var simulation = Simulation(clock, person);
            AdvanceMinutes(clock, 30);
            var employee = recruitment.Hire(waitingCandidate);
            Assert.That(employee, Is.Not.Null);

            AdvanceMinutes(clock, 30);

            Assert.That(employee.Person, Is.SameAs(person));
            Assert.That(employee.Person.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Idle));
            AssertState(person, 79, 9, 14, 70);
        }

        [Test]
        public void ProfessionChangePreservesWellbeingActivityAndProgression()
        {
            var clock = new SimulationClock();
            var person = Person("employee");
            var employee = new Employee(person, EmployeeRole.Actor, 1000);
            employee.SetState(EmployeeState.Working);
            using var simulation = Simulation(clock, person);

            AdvanceMinutes(clock, 30);
            employee.ChangeProfession(EmployeeRole.Director);
            AdvanceMinutes(clock, 30);

            Assert.That(employee.Person.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Working));
            AssertState(person, 76, 12, 5, 70);
        }

        [Test]
        public void RegisteredPeopleProgressIndependently()
        {
            var clock = new SimulationClock();
            var idle = Person("idle");
            var working = Person("working");
            working.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
            using var simulation = new PersonWellbeingSimulation(clock);
            simulation.Register(idle);
            simulation.Register(working);

            AdvanceMinutes(clock, 60);

            AssertState(idle, 79, 9, 14, 70);
            AssertState(working, 76, 12, 5, 70);
        }

        [Test]
        public void UnregisteredPeopleDoNotProgress()
        {
            var clock = new SimulationClock();
            var person = Person();
            using var simulation = Simulation(clock, person);
            simulation.Unregister(person);

            AdvanceMinutes(clock, 60);

            AssertState(person, 80, 10, 10, 70);
        }

        private static PersonWellbeingSimulation Simulation(
            SimulationClock clock,
            PersonProfile person,
            PersonWellbeingRates rates = null)
        {
            var simulation = new PersonWellbeingSimulation(clock, rates);
            simulation.Register(person);
            return simulation;
        }

        private static void AdvanceMinutes(SimulationClock clock, int minutes) =>
            clock.AdvanceTo(clock.Now + SimulationDuration.FromMinutes(minutes));

        private static PersonProfile Person(string id = "wellbeing-simulation-test", ProfessionalRole role = ProfessionalRole.Unassigned) =>
            new PersonProfile(id, "Test Person", new SimulationDateTime(1900, 1, 1, 0, 0), role, new TalentProfile(50, 50));

        private static void AssertState(PersonProfile person, int energy, int stress, int boredom, int mood)
        {
            Assert.That(person.Wellbeing.Energy, Is.EqualTo(energy));
            Assert.That(person.Wellbeing.Stress, Is.EqualTo(stress));
            Assert.That(person.Wellbeing.Boredom, Is.EqualTo(boredom));
            Assert.That(person.Wellbeing.Mood, Is.EqualTo(mood));
        }

        private sealed class FixedCandidateGenerator : CandidateGenerator
        {
            private readonly Candidate _candidate;
            public FixedCandidateGenerator(Candidate candidate) : base(new SeededRandomSource(1)) => _candidate = candidate;
            public override Candidate Generate(SimulationDateTime time) => _candidate;
        }

        private sealed class ImmediateRouter : ICandidateWorldRouter
        {
            public CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate, System.Action<CandidateRouteResult, int> onComplete)
            {
                onComplete(CandidateRouteResult.Started, 0);
                return CandidateRouteResult.Started;
            }

            public CandidateRouteResult RouteToExit(Candidate candidate, System.Action<CandidateRouteResult, int> onComplete)
            {
                onComplete(CandidateRouteResult.Started, -1);
                return CandidateRouteResult.Started;
            }

            public void ReleaseCandidate(Candidate candidate) { }
        }
    }
}
