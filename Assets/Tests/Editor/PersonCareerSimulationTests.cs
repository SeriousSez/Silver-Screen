using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonCareerSimulationTests
    {
        [Test] public void CareerSatisfactionDefaultsToSeventy() => Assert.That(Person().Career.CareerSatisfaction, Is.EqualTo(70));

        [Test]
        public void CareerSatisfactionClampsToZeroAndOneHundred()
        {
            var career = Person().Career;
            career.SetCareerSatisfaction(-1);
            Assert.That(career.CareerSatisfaction, Is.Zero);
            career.SetCareerSatisfaction(101);
            Assert.That(career.CareerSatisfaction, Is.EqualTo(100));
        }

        [Test]
        public void CareerSatisfactionIsIndependentFromMood()
        {
            var person = Person();
            person.Career.SetCareerSatisfaction(20);
            Assert.That(person.Wellbeing.Mood, Is.EqualTo(PersonWellbeing.DefaultMood));
        }

        [Test]
        public void WorkingContributesToRecentWorkload()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 60);
                Assert.That(setup.person.Career.WorkloadWorkedMinutes, Is.EqualTo(60));
                Assert.That(setup.person.Career.WorkloadObservedMinutes, Is.EqualTo(60));
                Assert.That(setup.person.Career.RecentWorkloadPercent, Is.EqualTo(100m));
            }
        }

        [Test]
        public void IdleDoesNotCountAsMeaningfulWork()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                AdvanceMinutes(setup.clock, 60);
                Assert.That(setup.person.Career.WorkloadWorkedMinutes, Is.Zero);
                Assert.That(setup.person.Career.WorkloadObservedMinutes, Is.EqualTo(60));
            }
        }

        [Test]
        public void NewEmployeeHasGracePeriodBeforeUnderuseClassification()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.InsufficientHistory));
                AdvanceMinutes(setup.clock, 1439);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.InsufficientHistory));
                AdvanceMinutes(setup.clock, 1);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Underused));
            }
        }

        [Test]
        public void SustainedLowWorkloadBecomesUnderused()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Underused));
            }
        }

        [Test]
        public void ModerateSustainedWorkloadBecomesHealthy()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                SustainModerateWork(setup);
                Assert.That(setup.person.Career.RecentWorkloadPercent, Is.InRange(20m, 60m));
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Healthy));
            }
        }

        [Test]
        public void SustainedHighWorkloadBecomesOverworked()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Career.RecentWorkloadPercent, Is.GreaterThan(60m));
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Overworked));
            }
        }

        [Test]
        public void OneShortBusyPeriodDoesNotClassifyPersonAsOverworked()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 240);
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                AdvanceMinutes(setup.clock, 1200);
                Assert.That(setup.person.Career.WorkloadState, Is.Not.EqualTo(PersonWorkloadState.Overworked));
            }
        }

        [Test]
        public void WorkloadHistoryRollsOutAfterEvaluationWindow()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 1440);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Overworked));
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                AdvanceMinutes(setup.clock, 7 * 1440);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Underused));
                Assert.That(setup.person.Career.WorkloadWorkedMinutes, Is.Zero);
            }
        }

        [Test]
        public void UnderusedWorkloadDecreasesCareerSatisfaction()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(68));
            }
        }

        [Test]
        public void HealthyWorkloadSlowlyImprovesCareerSatisfaction()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(60);
                SustainModerateWork(setup);
                int settled = setup.person.Career.CareerSatisfaction;
                SustainModerateWork(setup, 2);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Healthy));
                Assert.That(setup.person.Career.CareerSatisfaction, Is.GreaterThan(settled));
            }
        }

        [Test]
        public void HealthyWorkloadRecoveryRespectsConfiguredCeiling()
        {
            var setup = TrackedPerson(new PersonCareerSimulationRates { HealthySatisfactionCeiling = 72 });
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(72);
                SustainModerateWork(setup, 2);
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(72));
            }
        }

        [Test]
        public void OverworkedWorkloadDecreasesCareerSatisfaction()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Overworked));
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(69));
            }
        }

        [Test]
        public void UnderuseAddsMildBoredomPressure()
        {
            var setup = TrackedPerson(wellbeingRates: new PersonWellbeingRates
            {
                IdleEnergyPerHour = 0m,
                IdleStressPerHour = 0m,
                IdleBoredomPerHour = 0m
            });
            using (setup.simulation)
            {
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Wellbeing.Boredom, Is.EqualTo(12));
            }
        }

        [Test]
        public void OverworkAddsMildStressAndEnergyPressure()
        {
            var setup = TrackedPerson(wellbeingRates: new PersonWellbeingRates
            {
                WorkingEnergyPerHour = 0m,
                WorkingStressPerHour = 0m,
                WorkingBoredomPerHour = 0m
            });
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Wellbeing.Stress, Is.EqualTo(12));
                Assert.That(setup.person.Wellbeing.Energy, Is.EqualTo(78));
            }
        }

        [Test]
        public void PauseStopsWorkloadAndCareerProgression()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                setup.clock.SetSpeed(SimulationSpeed.Paused);
                var start = setup.clock.Now;
                setup.clock.AdvanceTo(start + SimulationDuration.FromDays(2));
                Assert.That(setup.clock.Now, Is.EqualTo(start));
                Assert.That(setup.person.Career.WorkloadObservedMinutes, Is.Zero);
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(70));
            }
        }

        [TestCase(SimulationSpeed.Normal, 2880)]
        [TestCase(SimulationSpeed.Fast, 1440)]
        [TestCase(SimulationSpeed.VeryFast, 960)]
        public void EqualSimulationTimeAtEachSpeedProducesEquivalentCareerResults(SimulationSpeed speed, double realSeconds)
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.clock.SetSpeed(speed);
                setup.clock.Advance(realSeconds);
                Assert.That(setup.person.Career.WorkloadObservedMinutes, Is.EqualTo(2 * 1440));
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(68));
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Underused));
            }
        }

        [Test]
        public void ApplicantWaitingDoesNotStartWorkloadOrCareerPenalties()
        {
            var clock = new SimulationClock();
            var person = Person("applicant", ProfessionalRole.Actor);
            using var simulation = new PersonWellbeingSimulation(clock);
            simulation.Register(person);
            AdvanceMinutes(clock, 3 * 1440);
            Assert.That(person.Career.WorkloadObservedMinutes, Is.Zero);
            Assert.That(person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.InsufficientHistory));
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(70));
        }

        [Test]
        public void HiringBeginsWorkloadTrackingForTheSamePerson()
        {
            var clock = new SimulationClock();
            var person = Person("hired-applicant", ProfessionalRole.Actor);
            using var recruitment = Recruitment(person);
            var candidate = recruitment.TryGenerateArrival();
            using var simulation = new PersonWellbeingSimulation(clock);
            var employee = recruitment.Hire(candidate);
            Assert.That(employee, Is.Not.Null);
            simulation.StartCareerTracking(employee.Person);
            AdvanceMinutes(clock, 60);
            Assert.That(employee.Person, Is.SameAs(person));
            Assert.That(person.Career.WorkloadObservedMinutes, Is.EqualTo(60));
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(70));
        }

        [Test]
        public void ProfessionChangePreservesCareerSatisfactionAndWorkloadHistory()
        {
            var setup = TrackedPerson();
            var employee = new Employee(setup.person, EmployeeRole.Actor, 1000);
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(43);
                employee.SetState(EmployeeState.Working);
                AdvanceMinutes(setup.clock, 90);
                int observed = setup.person.Career.WorkloadObservedMinutes;
                int worked = setup.person.Career.WorkloadWorkedMinutes;
                employee.ChangeProfession(EmployeeRole.Director);
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(43));
                Assert.That(setup.person.Career.WorkloadObservedMinutes, Is.EqualTo(observed));
                Assert.That(setup.person.Career.WorkloadWorkedMinutes, Is.EqualTo(worked));
            }
        }

        [Test]
        public void EmploymentEndStopsTrackingButPreservesCareerAndWorkloadHistory()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 60);
                setup.simulation.StopCareerTracking(setup.person);
                int worked = setup.person.Career.WorkloadWorkedMinutes;
                int satisfaction = setup.person.Career.CareerSatisfaction;
                AdvanceMinutes(setup.clock, 60);
                Assert.That(setup.person.Career.WorkloadWorkedMinutes, Is.EqualTo(worked));
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(satisfaction));
            }
        }

        [Test]
        public void TwoPeopleMaintainIndependentWorkloadHistories()
        {
            var clock = new SimulationClock();
            var idle = Person("idle");
            var working = Person("working");
            working.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
            using var simulation = new PersonWellbeingSimulation(clock);
            simulation.StartCareerTracking(idle);
            simulation.StartCareerTracking(working);
            AdvanceMinutes(clock, 1440);
            Assert.That(idle.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Underused));
            Assert.That(idle.Career.WorkloadWorkedMinutes, Is.Zero);
            Assert.That(working.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Overworked));
            Assert.That(working.Career.WorkloadWorkedMinutes, Is.EqualTo(1440));
        }

        [Test]
        public void CareerSnapshotPreservesSatisfactionWorkloadAndFractionalProgress()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(42);
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                AdvanceMinutes(setup.clock, 1800);
                int observed = setup.person.Career.WorkloadObservedMinutes;
                int worked = setup.person.Career.WorkloadWorkedMinutes;
                var snapshot = setup.person.CaptureCareer();
                var restored = Person("restored");
                restored.RestoreCareer(snapshot);
                restored.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                using var restoredSimulation = new PersonWellbeingSimulation(setup.clock);
                restoredSimulation.StartCareerTracking(restored);
                Assert.That(restored.Career.CareerSatisfaction, Is.EqualTo(42));
                Assert.That(restored.Career.WorkloadObservedMinutes, Is.EqualTo(observed));
                Assert.That(restored.Career.WorkloadWorkedMinutes, Is.EqualTo(worked));
                AdvanceMinutes(setup.clock, 360);
                Assert.That(restored.Career.CareerSatisfaction, Is.EqualTo(41));
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(41));
                Assert.That(restored.Career.WorkloadObservedMinutes, Is.EqualTo(observed + 360));
                Assert.That(restored.Career.WorkloadWorkedMinutes, Is.EqualTo(worked));
            }
        }

        [Test]
        public void ApplicantSnapshotCapturesCareerState()
        {
            var person = Person("snapshot-applicant", ProfessionalRole.Actor);
            person.Career.SetCareerSatisfaction(55);
            var snapshot = new Candidate(person, 1000).Capture();
            Assert.That(snapshot.Career.CareerSatisfaction, Is.EqualTo(55));
            Assert.That(snapshot.Career.WorkloadDayKeys, Is.Empty);
        }

        [Test]
        public void PersonInformationShowsCareerAndWorkloadDebugState()
        {
            var person = Person();
            person.Career.SetCareerSatisfaction(62);
            var information = new List<PersonInformation>();
            new PersonInformationService().Collect(
                new PersonInformationContext { Person = person, Date = new SimulationDateTime(1930, 1, 1, 0, 0) },
                information);
            var card = information.Find(item => item.Category == "Career");
            Assert.That(card, Is.Not.Null);
            StringAssert.Contains("62/100", card.Summary);
            StringAssert.Contains("Insufficient history", card.Expanded);
        }

        private static void SustainModerateWork(
            (SimulationClock clock, PersonProfile person, PersonWellbeingSimulation simulation) setup,
            int days = 7)
        {
            for (int day = 0; day < days; day++)
            {
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 360);
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                AdvanceMinutes(setup.clock, 1080);
            }
        }

        private static (SimulationClock clock, PersonProfile person, PersonWellbeingSimulation simulation) TrackedPerson(
            PersonCareerSimulationRates careerRates = null, PersonWellbeingRates wellbeingRates = null)
        {
            var clock = new SimulationClock();
            var person = Person();
            var simulation = new PersonWellbeingSimulation(clock, wellbeingRates, careerRates);
            simulation.StartCareerTracking(person);
            return (clock, person, simulation);
        }

        private static RecruitmentCoordinator Recruitment(PersonProfile person) =>
            new RecruitmentCoordinator(new SimulationClock(), new FixedCandidateGenerator(person), new ImmediateRouter());

        private static PersonProfile Person(string id = "career-simulation-test", ProfessionalRole role = ProfessionalRole.Unassigned) =>
            new PersonProfile(id, "Test Person", new SimulationDateTime(1900, 1, 1, 0, 0), role, new TalentProfile(50, 50));

        private static void AdvanceMinutes(SimulationClock clock, int minutes)
        {
            while (minutes > 0)
            {
                int batch = Math.Min(minutes, 1440);
                clock.AdvanceTo(clock.Now + SimulationDuration.FromMinutes(batch));
                if (clock.AdvanceFailure != null) throw new InvalidOperationException(clock.AdvanceFailure);
                if (clock.HasPendingAdvance) throw new InvalidOperationException("Simulation clock did not complete the requested test advance.");
                minutes -= batch;
            }
        }

        private sealed class FixedCandidateGenerator : CandidateGenerator
        {
            private readonly PersonProfile _person;
            public FixedCandidateGenerator(PersonProfile person) : base(new SeededRandomSource(1)) => _person = person;
            public override Candidate Generate(SimulationDateTime time) => new Candidate(_person, 1000);
        }

        private sealed class ImmediateRouter : ICandidateWorldRouter
        {
            public CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate, Action<CandidateRouteResult, int> callback)
            { callback(CandidateRouteResult.Started, 0); return CandidateRouteResult.Started; }
            public CandidateRouteResult RouteToExit(Candidate candidate, Action<CandidateRouteResult, int> callback)
            { callback(CandidateRouteResult.Started, -1); return CandidateRouteResult.Started; }
            public void ReleaseCandidate(Candidate candidate) { }
        }
    }
}
