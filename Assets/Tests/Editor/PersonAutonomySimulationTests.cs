using System;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonAutonomySimulationTests
    {
        [Test]
        public void ExistingWorkObligationBlocksOrdinaryAutonomy()
        {
            var setup = Create();
            using (setup.simulation)
            {
                setup.employee.SetState(EmployeeState.Working);
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.IsEligible(setup.employee), Is.False);
                Assert.That(setup.simulation.LastDecision(setup.employee), Is.Null);
                Assert.That(setup.registry.Find("rest")?.ReservationCount ?? 0, Is.Zero);
            }
        }

        [Test]
        public void RecruitmentApplicantIsNotRegisteredForAutonomy()
        {
            var setup = Create();
            var applicant = new Candidate(Person("applicant"), 1000);
            using (setup.simulation)
            {
                Assert.That(applicant.Status, Is.EqualTo(CandidateStatus.Arriving));
                Assert.That(setup.simulation.ParticipantCount, Is.Zero);
                Advance(setup.clock, 60);
                Assert.That(setup.registry.Find("rest")?.ReservationCount ?? 0, Is.Zero);
            }
        }

        [Test]
        public void LowEnergyStronglyFavorsAvailableRest()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, energy: 10);
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                AssertChoice(setup.simulation, setup.employee, PersonAutonomousActivity.Rest, "rest");
                StringAssert.Contains("Low energy", setup.simulation.LastDecision(setup.employee).Reason);
            }
        }

        [Test]
        public void HighBoredomFavorsAvailableRecreation()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, boredom: 90);
            setup.registry.Register(Opportunity("social", PersonAutonomousActivity.Socialize));
            setup.registry.Register(Opportunity("recreation", PersonAutonomousActivity.Recreation));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                AssertChoice(setup.simulation, setup.employee, PersonAutonomousActivity.Recreation, "recreation");
            }
        }

        [Test]
        public void SocializingCanWinWhenRecreationIsUnavailable()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, boredom: 90);
            setup.registry.Register(Opportunity("social", PersonAutonomousActivity.Socialize));
            setup.registry.Register(Opportunity("recreation", PersonAutonomousActivity.Recreation));
            setup.registry.SetAvailable("recreation", false);
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                AssertChoice(setup.simulation, setup.employee, PersonAutonomousActivity.Socialize, "social");
            }
        }

        [Test]
        public void CareerDriveIncreasesPracticePreference()
        {
            var low = Create("low-drive");
            var high = Create("high-drive");
            SetWellbeing(low.employee.Person, boredom: 80);
            SetWellbeing(high.employee.Person, boredom: 80);
            low.employee.Person.Career.SetCareerDrive(0);
            high.employee.Person.Career.SetCareerDrive(100);
            low.registry.Register(Opportunity("practice", PersonAutonomousActivity.PracticeProfession));
            high.registry.Register(Opportunity("practice", PersonAutonomousActivity.PracticeProfession));
            using (low.simulation)
            using (high.simulation)
            {
                low.simulation.Register(low.employee);
                high.simulation.Register(high.employee);
                Assert.That(low.simulation.LastDecision(low.employee).Activity,
                    Is.EqualTo(PersonAutonomousActivity.IdleWait));
                AssertChoice(high.simulation, high.employee, PersonAutonomousActivity.PracticeProfession, "practice");
            }
        }

        [Test]
        public void SevereLowEnergyOutweighsHighCareerDrive()
        {
            var setup = Create();
            setup.employee.Person.Career.SetCareerDrive(100);
            SetWellbeing(setup.employee.Person, energy: 5, boredom: 100);
            setup.registry.Register(Opportunity("practice", PersonAutonomousActivity.PracticeProfession));
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                AssertChoice(setup.simulation, setup.employee, PersonAutonomousActivity.Rest, "rest");
            }
        }

        [Test]
        public void UnavailableOpportunityIsNeverSelected()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, energy: 0);
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest));
            setup.registry.SetAvailable("rest", false);
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.LastDecision(setup.employee).Activity,
                    Is.EqualTo(PersonAutonomousActivity.IdleWait));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
            }
        }

        [Test]
        public void NoOpportunitiesProducesIdleWaitFallback()
        {
            var setup = Create();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.LastDecision(setup.employee).Activity,
                    Is.EqualTo(PersonAutonomousActivity.IdleWait));
                Assert.That(setup.simulation.LastDecision(setup.employee).Reason,
                    Is.EqualTo("No available opportunity exceeds the idle fallback."));
                Assert.That(setup.simulation.ShouldDeferIdleWander(setup.employee), Is.True);
            }
        }

        [Test]
        public void CapacityOneOpportunityCannotBeReservedByTwoPeople()
        {
            var setup = Create();
            var second = Employee(Person("second"));
            SetWellbeing(setup.employee.Person, energy: 0);
            SetWellbeing(second.Person, energy: 0);
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest, capacity: 1));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.Register(second);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.EqualTo(1));
                Assert.That(setup.simulation.LastDecision(second).Activity,
                    Is.EqualTo(PersonAutonomousActivity.IdleWait));
            }
        }

        [Test]
        public void ReservationReleasesOnActivityCompletion()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.EqualTo(1));
                Advance(setup.clock, 60);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
                Assert.That(setup.employee.CurrentState, Is.EqualTo(EmployeeState.Idle));
                Assert.That(setup.employee.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
            }
        }

        [Test]
        public void ReservationReleasesWhenNavigationFails()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.NavigationCompleted(setup.employee, false), Is.False);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
                Assert.That(setup.employee.CurrentState, Is.EqualTo(EmployeeState.Idle));
            }
        }

        [Test]
        public void ReservationReleasesWhenInterrupted()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.Interrupt(setup.employee), Is.True);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
                Assert.That(setup.employee.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
            }
        }

        [Test]
        public void PlayerPickupInterruptsAndReleasesAutonomousActivity()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                setup.simulation.Interrupt(setup.employee);
                Assert.That(setup.employee.Person.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Idle));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
            }
        }

        [Test]
        public void HigherPriorityWorkInterruptsWithoutBeingOverwritten()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                setup.employee.SetState(EmployeeState.Working);
                setup.employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.PerformTask, "Assigned work"));
                Assert.That(setup.employee.CurrentState, Is.EqualTo(EmployeeState.Working));
                Assert.That(setup.employee.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.PerformTask));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
            }
        }

        [TestCase(PersonAutonomousActivity.Rest, EmployeeState.Resting, PersonWellbeingActivity.Resting)]
        [TestCase(PersonAutonomousActivity.Socialize, EmployeeState.Socializing, PersonWellbeingActivity.Socializing)]
        [TestCase(PersonAutonomousActivity.Recreation, EmployeeState.Recreating, PersonWellbeingActivity.Recreation)]
        [TestCase(PersonAutonomousActivity.PracticeProfession, EmployeeState.Practicing, PersonWellbeingActivity.Working)]
        public void ChosenActivityMapsToBroadWellbeingState(PersonAutonomousActivity activity,
            EmployeeState expectedState, PersonWellbeingActivity expectedActivity)
        {
            var setup = Create();
            switch (activity)
            {
                case PersonAutonomousActivity.Rest: SetWellbeing(setup.employee.Person, energy: 0); break;
                case PersonAutonomousActivity.Socialize: SetWellbeing(setup.employee.Person, boredom: 100, mood: 0); break;
                case PersonAutonomousActivity.Recreation: SetWellbeing(setup.employee.Person, boredom: 100); break;
                case PersonAutonomousActivity.PracticeProfession:
                    SetWellbeing(setup.employee.Person, boredom: 100);
                    setup.employee.Person.Career.SetCareerDrive(100);
                    break;
            }
            string id = activity.ToString();
            setup.registry.Register(Opportunity(id, activity));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.LastDecision(setup.employee).Activity, Is.EqualTo(activity));
                Assert.That(setup.simulation.NavigationCompleted(setup.employee, true), Is.True);
                Assert.That(setup.employee.CurrentState, Is.EqualTo(expectedState));
                Assert.That(setup.employee.Person.Wellbeing.Activity, Is.EqualTo(expectedActivity));
            }
        }

        [Test]
        public void AutonomousPracticeDoesNotCountAsProfessionalWorkload()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, boredom: 100);
            setup.employee.Person.Career.SetCareerDrive(100);
            setup.registry.Register(Opportunity("practice", PersonAutonomousActivity.PracticeProfession));
            var careerRates = new PersonCareerSimulationRates
            {
                EvaluationDays = 1,
                GracePeriodMinutes = 1,
                UnderusedSatisfactionPerDay = 0,
                HealthySatisfactionPerDay = 0,
                OverworkedSatisfactionPerDay = 0,
                UnderusedBoredomPerDay = 0,
                OverworkedEnergyPerDay = 0,
                OverworkedStressPerDay = 0
            };
            using var wellbeing = new PersonWellbeingSimulation(setup.clock, careerRates: careerRates);
            wellbeing.StartCareerTracking(setup.employee.Person);
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                Advance(setup.clock, 60);
                Assert.That(setup.employee.Person.Career.WorkloadWorkedMinutes, Is.Zero);
            }
        }

        [Test]
        public void CompletedActivityReturnsToScheduledReevaluation()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                Advance(setup.clock, 60);
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
                Advance(setup.clock, setup.simulation.Rates.ReevaluationMinutes);
                Assert.That(setup.simulation.LastDecision(setup.employee).Activity,
                    Is.EqualTo(PersonAutonomousActivity.Rest));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void PauseStopsActivityDuration()
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                setup.clock.SetSpeed(SimulationSpeed.Paused);
                var start = setup.clock.Now;
                setup.clock.AdvanceTo(start + SimulationDuration.FromMinutes(60));
                Assert.That(setup.clock.Now, Is.EqualTo(start));
                Assert.That(setup.employee.CurrentState, Is.EqualTo(EmployeeState.Resting));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.EqualTo(1));
            }
        }

        [TestCase(SimulationSpeed.Normal, 60)]
        [TestCase(SimulationSpeed.Fast, 30)]
        [TestCase(SimulationSpeed.VeryFast, 20)]
        public void EqualSimulationTimeProducesEquivalentAutonomyResults(SimulationSpeed speed, double seconds)
        {
            var setup = CreateWithLowEnergyRest();
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.NavigationCompleted(setup.employee, true);
                setup.clock.SetSpeed(speed);
                setup.clock.Advance(seconds);
                Assert.That(setup.employee.CurrentState, Is.EqualTo(EmployeeState.Idle));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.Zero);
            }
        }

        [Test]
        public void TwoPeopleChooseIndependentAvailableOpportunities()
        {
            var setup = Create();
            var second = Employee(Person("second-independent"));
            SetWellbeing(setup.employee.Person, energy: 0);
            SetWellbeing(second.Person, boredom: 100);
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest));
            setup.registry.Register(Opportunity("recreation", PersonAutonomousActivity.Recreation));
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                setup.simulation.Register(second);
                Assert.That(setup.simulation.LastDecision(setup.employee).Activity,
                    Is.EqualTo(PersonAutonomousActivity.Rest));
                Assert.That(setup.simulation.LastDecision(second).Activity,
                    Is.EqualTo(PersonAutonomousActivity.Recreation));
                Assert.That(setup.registry.Find("rest").ReservationCount, Is.EqualTo(1));
                Assert.That(setup.registry.Find("recreation").ReservationCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void DomainAutonomyUsesExplicitRegistryWithoutSceneQueries()
        {
            var setup = Create();
            var opportunity = Opportunity("test-rest", PersonAutonomousActivity.Rest);
            setup.registry.Register(opportunity);
            SetWellbeing(setup.employee.Person, energy: 0);
            using (setup.simulation)
            {
                setup.simulation.Register(setup.employee);
                Assert.That(setup.simulation.LastDecision(setup.employee).OpportunityId, Is.EqualTo("test-rest"));
                Assert.That(setup.registry.Find("test-rest"), Is.SameAs(opportunity));
            }
        }

        private static (SimulationClock clock, SimulationScheduler scheduler,
            PersonActivityOpportunityRegistry registry, PersonAutonomySimulation simulation, Employee employee) Create(
                string id = "autonomy-test")
        {
            var clock = new SimulationClock();
            var scheduler = new SimulationScheduler(clock);
            var registry = new PersonActivityOpportunityRegistry();
            var simulation = new PersonAutonomySimulation(clock, scheduler, registry);
            return (clock, scheduler, registry, simulation, Employee(Person(id)));
        }

        private static (SimulationClock clock, SimulationScheduler scheduler,
            PersonActivityOpportunityRegistry registry, PersonAutonomySimulation simulation, Employee employee)
            CreateWithLowEnergyRest()
        {
            var setup = Create();
            SetWellbeing(setup.employee.Person, energy: 0);
            setup.registry.Register(Opportunity("rest", PersonAutonomousActivity.Rest));
            return setup;
        }

        private static void AssertChoice(PersonAutonomySimulation simulation, Employee employee,
            PersonAutonomousActivity activity, string opportunityId)
        {
            var decision = simulation.LastDecision(employee);
            Assert.That(decision, Is.Not.Null);
            Assert.That(decision.Activity, Is.EqualTo(activity));
            Assert.That(decision.OpportunityId, Is.EqualTo(opportunityId));
            Assert.That(decision.Score, Is.GreaterThan(simulation.Rates.IdleBaseline));
            Assert.That(decision.Reason, Is.Not.Empty);
        }

        private static PersonActivityOpportunity Opportunity(string id, PersonAutonomousActivity activity,
            int capacity = 1) =>
            new PersonActivityOpportunity(id, id, activity, new AutonomyPosition(1, 0, 1), capacity);

        private static void SetWellbeing(PersonProfile person, int? energy = null, int? stress = null,
            int? boredom = null, int? mood = null)
        {
            var snapshot = person.CaptureWellbeing();
            if (energy.HasValue) snapshot.Energy = energy.Value;
            if (stress.HasValue) snapshot.Stress = stress.Value;
            if (boredom.HasValue) snapshot.Boredom = boredom.Value;
            if (mood.HasValue) snapshot.Mood = mood.Value;
            person.RestoreWellbeing(snapshot);
        }

        private static PersonProfile Person(string id) =>
            new PersonProfile(id, id, new SimulationDateTime(1900, 1, 1, 0, 0), ProfessionalRole.Actor,
                new TalentProfile(50, 50));

        private static Employee Employee(PersonProfile person) => new Employee(person, EmployeeRole.Actor, 1000);

        private static void Advance(SimulationClock clock, int minutes) =>
            clock.AdvanceTo(clock.Now + SimulationDuration.FromMinutes(minutes));
    }
}
