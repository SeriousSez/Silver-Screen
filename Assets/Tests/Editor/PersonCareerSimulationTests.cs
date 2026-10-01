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
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(75));
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
            person.Career.SetCareerDrive(65);
            var information = new List<PersonInformation>();
            new PersonInformationService().Collect(
                new PersonInformationContext { Person = person, Date = new SimulationDateTime(1930, 1, 1, 0, 0) },
                information);
            var card = information.Find(item => item.Category == "Career");
            Assert.That(card, Is.Not.Null);
            StringAssert.Contains("62/100", card.Summary);
            StringAssert.Contains("Insufficient history", card.Expanded);
            StringAssert.Contains("Career drive: 65/100", card.Expanded);
            StringAssert.Contains("GetHired: Active (0%)", card.Expanded);
            StringAssert.Contains("Retention: NotEmployed", card.Expanded);
        }

        [Test]
        public void CareerDriveDefaultsToNeutralAndClampsToItsDomainRange()
        {
            var career = Person().Career;
            Assert.That(career.CareerDrive, Is.EqualTo(50));
            career.SetCareerDrive(-1);
            Assert.That(career.CareerDrive, Is.Zero);
            career.SetCareerDrive(101);
            Assert.That(career.CareerDrive, Is.EqualTo(100));
        }

        [Test]
        public void CareerDriveSurvivesHiringAndProfessionChanges()
        {
            var person = Person("drive-lifecycle", ProfessionalRole.Actor);
            person.Career.SetCareerDrive(83);
            var careerSnapshot = person.CaptureCareer();
            careerSnapshot.RetentionPressure = 37;
            person.RestoreCareer(careerSnapshot);
            using var recruitment = Recruitment(person);
            var applicant = recruitment.TryGenerateArrival();
            var employee = recruitment.Hire(applicant);
            Assert.That(employee.Person.Career.CareerDrive, Is.EqualTo(83));
            employee.ChangeProfession(EmployeeRole.Director);
            employee.ChangeProfession(EmployeeRole.Extra);
            Assert.That(person.Career.CareerDrive, Is.EqualTo(83));
            Assert.That(person.Career.RetentionPressure, Is.EqualTo(37));
        }

        [TestCase(0, 69)]
        [TestCase(50, 68)]
        [TestCase(100, 67)]
        public void UnderuseSensitivityScalesWithCareerDrive(int drive, int expectedSatisfaction)
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Career.SetCareerDrive(drive);
                AdvanceMinutes(setup.clock, 2 * 1440);
                Assert.That(setup.person.Career.CareerSatisfaction, Is.EqualTo(expectedSatisfaction));
            }
        }

        [Test]
        public void CareerDriveDoesNotChangeHealthyWorkloadRecovery()
        {
            int? expectedSatisfaction = null;
            for (int i = 0; i <= 100; i += 50)
            {
                var clock = new SimulationClock();
                var person = Person("healthy-drive-person-" + i.ToString());
                person.Career.SetCareerDrive(i);
                person.Career.SetCareerSatisfaction(60);
                using var simulation = new PersonWellbeingSimulation(clock);
                simulation.StartCareerTracking(person);
                SustainModerateWork((clock, person, simulation), 9);
                Assert.That(person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Healthy));
                expectedSatisfaction ??= person.Career.CareerSatisfaction;
                Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(expectedSatisfaction));
                Assert.That(person.Career.CareerSatisfaction, Is.GreaterThan(60));
                Assert.That(person.Career.CareerSatisfaction, Is.LessThanOrEqualTo(80));
            }
        }

        [Test]
        public void GetHiredGoalExistsForApplicantsAndHiringCompletesItOnceWithReward()
        {
            var person = Person("goal-hiring", ProfessionalRole.Actor);
            person.Career.SetCareerSatisfaction(60);
            var goal = GoalFor(person.Career, PersonCareerGoalType.GetHired);
            Assert.That(goal.Status, Is.EqualTo(PersonCareerGoalStatus.Active));
            Assert.That(goal.Progress, Is.Zero);

            using var recruitment = Recruitment(person);
            var employee = recruitment.Hire(recruitment.TryGenerateArrival());
            Assert.That(employee, Is.Not.Null);
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(65));
            Assert.That(person.Wellbeing.Mood, Is.EqualTo(PersonWellbeing.DefaultMood));
            goal = GoalFor(person.Career, PersonCareerGoalType.GetHired);
            Assert.That(goal.Status, Is.EqualTo(PersonCareerGoalStatus.Completed));
            Assert.That(goal.Progress, Is.EqualTo(100));
            Assert.That(goal.CompletedYear, Is.EqualTo(1930));

            employee.ChangeProfession(EmployeeRole.Director);
            employee.ChangeProfession(EmployeeRole.Extra);
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(65));
        }

        [Test]
        public void GoalCompletionRewardCanBeConfigured()
        {
            var person = Person("configured-goal-reward", ProfessionalRole.Actor);
            person.Career.GoalCompletionSatisfactionReward = 8;
            using var recruitment = Recruitment(person);
            Assert.That(recruitment.Hire(recruitment.TryGenerateArrival()), Is.Not.Null);
            Assert.That(person.Career.CareerSatisfaction, Is.EqualTo(78));
        }

        [Test]
        public void FirstProfessionalAssignmentGoalRemainsAnExplicitIncompleteExtensionPoint()
        {
            var setup = TrackedPerson();
            using (setup.simulation)
            {
                setup.person.Career.AddGoal(PersonCareerGoalType.GetFirstProfessionalAssignment);
                setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                AdvanceMinutes(setup.clock, 60);
                Assert.That(GoalFor(setup.person.Career, PersonCareerGoalType.GetFirstProfessionalAssignment).Status,
                    Is.EqualTo(PersonCareerGoalStatus.Active));
            }
        }

        [Test]
        public void CareerGoalsAndDriveSurvivePersonCareerSnapshot()
        {
            var person = Person("goal-snapshot", ProfessionalRole.Actor);
            person.Career.SetCareerDrive(91);
            using (var recruitment = Recruitment(person))
                Assert.That(recruitment.Hire(recruitment.TryGenerateArrival()), Is.Not.Null);
            var snapshot = person.CaptureCareer();
            var restored = Person("goal-snapshot-restored");
            restored.RestoreCareer(snapshot);
            Assert.That(restored.Career.CareerDrive, Is.EqualTo(91));
            Assert.That(GoalFor(restored.Career, PersonCareerGoalType.GetHired).Progress, Is.EqualTo(100));
            Assert.That(GoalFor(restored.Career, PersonCareerGoalType.GetHired).Status,
                Is.EqualTo(PersonCareerGoalStatus.Completed));
            Assert.That(GoalFor(restored.Career, PersonCareerGoalType.GetHired).HasCompletedYear, Is.True);
            Assert.That(restored.Career.CareerSatisfaction, Is.EqualTo(75));
        }

        [Test]
        public void ApplicantDoesNotAccumulateRetentionPressure()
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(20);
                setup.person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -50);
                setup.person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 80);
                setup.simulation.StopCareerTracking(setup.person);
                setup.simulation.Register(setup.person);
                AdvanceMinutes(setup.clock, 20 * 1440);
                Assert.That(setup.person.Career.RetentionPressure, Is.Zero);
                Assert.That(setup.person.Career.RetentionState, Is.EqualTo(PersonRetentionState.NotEmployed));
            }
        }

        [Test]
        public void HealthyEmployeeAndOneShortNegativePeriodRemainContent()
        {
            var healthy = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (healthy.simulation)
            {
                AdvanceMinutes(healthy.clock, 16 * 60);
                for (int day = 0; day < 5; day++)
                {
                    healthy.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                    AdvanceMinutes(healthy.clock, 8 * 60);
                    healthy.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                    AdvanceMinutes(healthy.clock, day == 4 ? 16 * 60 - 1 : 16 * 60);
                }
                Assert.That(healthy.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Healthy));
                Assert.That(healthy.person.Career.RetentionPressure, Is.Zero);
                Assert.That(healthy.person.Career.RetentionState, Is.EqualTo(PersonRetentionState.Content));
            }

            var shortPeriod = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (shortPeriod.simulation)
            {
                shortPeriod.person.Career.SetCareerSatisfaction(30);
                AdvanceMinutes(shortPeriod.clock, 1440);
                Assert.That(shortPeriod.person.Career.RetentionPressure, Is.LessThan(25));
                Assert.That(shortPeriod.person.Career.RetentionState, Is.EqualTo(PersonRetentionState.Content));
            }
        }

        [TestCase("satisfaction")]
        [TestCase("stress")]
        [TestCase("mood")]
        [TestCase("high-drive-underuse")]
        public void SustainedNegativeConditionsRaiseRetentionPressure(string condition)
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                switch (condition)
                {
                    case "satisfaction":
                        setup.person.Career.SetCareerSatisfaction(30);
                        break;
                    case "stress":
                        setup.person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 80);
                        break;
                    case "mood":
                        setup.person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -50);
                        break;
                    case "high-drive-underuse":
                        setup.person.Career.SetCareerDrive(90);
                        break;
                }

                AdvanceMinutes(setup.clock, 30 * 1440);
                Assert.That(setup.person.Career.RetentionPressure, Is.GreaterThanOrEqualTo(25));
                Assert.That(setup.person.Career.RetentionState, Is.EqualTo(PersonRetentionState.Restless));
            }
        }

        [Test]
        public void RetentionPressureRecoversGraduallyUnderHealthyConditions()
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                var snapshot = setup.person.CaptureCareer();
                snapshot.RetentionPressure = 40;
                setup.person.RestoreCareer(snapshot);
                setup.simulation.StartCareerTracking(setup.person);
                AdvanceMinutes(setup.clock, 16 * 60);
                for (int day = 0; day < 5; day++)
                {
                    setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
                    AdvanceMinutes(setup.clock, 8 * 60);
                    setup.person.Wellbeing.SetActivity(PersonWellbeingActivity.Idle);
                    AdvanceMinutes(setup.clock, day == 4 ? 16 * 60 - 1 : 16 * 60);
                }
                Assert.That(setup.person.Career.WorkloadState, Is.EqualTo(PersonWorkloadState.Healthy));
                Assert.That(setup.person.Career.RetentionPressure, Is.LessThan(40));
                Assert.That(setup.person.Career.RetentionPressure, Is.GreaterThan(0));
            }
        }

        [Test]
        public void RetentionPressureClampsAndStateBandsAreDerived()
        {
            var person = Person();
            var career = person.Career;
            var snapshot = career.Capture();
            using var simulation = new PersonWellbeingSimulation(new SimulationClock());
            foreach (var testCase in new[]
            {
                (pressure: -10, expected: 0, state: PersonRetentionState.Content),
                (pressure: 25, expected: 25, state: PersonRetentionState.Restless),
                (pressure: 50, expected: 50, state: PersonRetentionState.Unhappy),
                (pressure: 75, expected: 75, state: PersonRetentionState.ConsideringLeaving),
                (pressure: 110, expected: 100, state: PersonRetentionState.ConsideringLeaving)
            })
            {
                snapshot.RetentionPressure = testCase.pressure;
                career.Restore(snapshot);
                simulation.StartCareerTracking(person);
                Assert.That(career.RetentionPressure, Is.EqualTo(testCase.expected));
                Assert.That(career.RetentionState, Is.EqualTo(testCase.state));
            }
        }

        [Test]
        public void ConsideringLeavingDoesNotDismissOrDisableAnEmployee()
        {
            var person = Person("no-resignation", ProfessionalRole.Actor);
            var employee = new Employee(person, EmployeeRole.Actor, 1000);
            var workforce = new WorkforceRoster();
            Assert.That(workforce.Add(employee), Is.True);
            var snapshot = person.CaptureCareer();
            snapshot.RetentionPressure = 90;
            person.RestoreCareer(snapshot);
            using var simulation = new PersonWellbeingSimulation(new SimulationClock());
            simulation.StartCareerTracking(person);
            Assert.That(person.Career.RetentionState, Is.EqualTo(PersonRetentionState.ConsideringLeaving));
            Assert.That(employee.IsEmployed, Is.True);
            Assert.That(workforce.Contains(employee), Is.True);
        }

        [Test]
        public void CareerSnapshotPreservesRetentionPressureAndFractionalProgress()
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(30);
                AdvanceMinutes(setup.clock, 3 * 1440 + 100);
                var snapshot = setup.person.CaptureCareer();
                var restored = Person("retention-restored");
                restored.RestoreCareer(snapshot);
                using var restoredSimulation = new PersonWellbeingSimulation(
                    setup.clock, NeutralRetentionWellbeingRates(), RetentionRates());
                restoredSimulation.StartCareerTracking(restored);
                AdvanceMinutes(setup.clock, 1440);
                Assert.That(restored.Career.RetentionPressure, Is.EqualTo(setup.person.Career.RetentionPressure));
                Assert.That(restored.Career.RetentionState, Is.EqualTo(setup.person.Career.RetentionState));
            }
        }

        [Test]
        public void PauseStopsRetentionPressureProgression()
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(30);
                setup.clock.SetSpeed(SimulationSpeed.Paused);
                var start = setup.clock.Now;
                setup.clock.AdvanceTo(start + SimulationDuration.FromDays(30));
                Assert.That(setup.person.Career.RetentionPressure, Is.Zero);
            }
        }

        [TestCase(SimulationSpeed.Normal, 2880)]
        [TestCase(SimulationSpeed.Fast, 1440)]
        [TestCase(SimulationSpeed.VeryFast, 960)]
        public void EqualSimulationTimeProducesEquivalentRetentionResults(SimulationSpeed speed, double realSeconds)
        {
            var setup = TrackedPerson(RetentionRates(), NeutralRetentionWellbeingRates());
            using (setup.simulation)
            {
                setup.person.Career.SetCareerSatisfaction(30);
                setup.clock.SetSpeed(speed);
                setup.clock.Advance(realSeconds);
                Assert.That(setup.person.Career.RetentionPressure, Is.EqualTo(1));
                Assert.That(setup.person.Career.RetentionState, Is.EqualTo(PersonRetentionState.Content));
            }
        }

        [Test]
        public void TwoPeopleMaintainIndependentCareerDriveAndRetentionState()
        {
            var clock = new SimulationClock();
            var pressured = Person("pressure-person");
            var content = Person("content-person");
            pressured.Career.SetCareerSatisfaction(30);
            content.Career.SetCareerDrive(80);
            using var simulation = new PersonWellbeingSimulation(
                clock, NeutralRetentionWellbeingRates(), RetentionRates());
            simulation.StartCareerTracking(pressured);
            content.Wellbeing.SetActivity(PersonWellbeingActivity.Working);
            simulation.StartCareerTracking(content);
            AdvanceMinutes(clock, 30 * 1440);
            Assert.That(pressured.Career.RetentionPressure, Is.GreaterThan(0));
            Assert.That(content.Career.RetentionPressure, Is.Zero);
            Assert.That(content.Career.CareerDrive, Is.EqualTo(80));
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

        private static PersonCareerGoal GoalFor(PersonCareer career, PersonCareerGoalType type)
        {
            foreach (var goal in career.Goals)
                if (goal.Type == type) return goal;
            return null;
        }

        private static PersonCareerSimulationRates RetentionRates() => new PersonCareerSimulationRates
        {
            EvaluationDays = 1,
            GracePeriodMinutes = 1,
            UnderusedSatisfactionPerDay = 0m,
            HealthySatisfactionPerDay = 0m,
            OverworkedSatisfactionPerDay = 0m,
            UnderusedBoredomPerDay = 0m,
            OverworkedStressPerDay = 0m,
            OverworkedEnergyPerDay = 0m
        };

        private static PersonWellbeingRates NeutralRetentionWellbeingRates() => new PersonWellbeingRates
        {
            IdleEnergyPerHour = 0m,
            IdleStressPerHour = 0m,
            IdleBoredomPerHour = 0m,
            WorkingEnergyPerHour = 0m,
            WorkingStressPerHour = 0m,
            WorkingBoredomPerHour = 0m,
            RestingEnergyPerHour = 0m,
            RestingStressPerHour = 0m,
            RestingBoredomPerHour = 0m,
            SocializingEnergyPerHour = 0m,
            SocializingStressPerHour = 0m,
            SocializingBoredomPerHour = 0m,
            RecreationEnergyPerHour = 0m,
            RecreationStressPerHour = 0m,
            RecreationBoredomPerHour = 0m
        };

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
