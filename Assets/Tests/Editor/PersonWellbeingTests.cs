using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonWellbeingTests
    {
        [Test]
        public void NewPersonStartsWithNeutralWellbeingDefaults()
        {
            var wellbeing = Person().Wellbeing;

            Assert.That(wellbeing.Energy, Is.EqualTo(80));
            Assert.That(wellbeing.Stress, Is.EqualTo(10));
            Assert.That(wellbeing.Boredom, Is.EqualTo(10));
            Assert.That(wellbeing.Mood, Is.EqualTo(70));
        }

        [Test]
        public void PositiveDeltaChangesOnlySelectedDimension()
        {
            var wellbeing = Person().Wellbeing;

            wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, 5);

            AssertState(wellbeing, 85, 10, 10, 70);
        }

        [Test]
        public void NegativeDeltaChangesOnlySelectedDimension()
        {
            var wellbeing = Person().Wellbeing;

            wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, -5);

            AssertState(wellbeing, 80, 5, 10, 70);
        }

        [Test]
        public void DeltaClampsAtLowerBound()
        {
            var wellbeing = Person().Wellbeing;

            wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, int.MinValue);

            Assert.That(wellbeing.Energy, Is.EqualTo(0));
        }

        [Test]
        public void DeltaClampsAtUpperBound()
        {
            var wellbeing = Person().Wellbeing;

            wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, int.MaxValue);

            Assert.That(wellbeing.Mood, Is.EqualTo(100));
        }

        [Test]
        public void ChangingEnergyDoesNotChangeOtherDimensions()
        {
            AssertUnchangedDimensions(PersonWellbeingDimension.Energy);
        }

        [Test]
        public void ChangingStressDoesNotChangeOtherDimensions()
        {
            AssertUnchangedDimensions(PersonWellbeingDimension.Stress);
        }

        [Test]
        public void ChangingBoredomDoesNotChangeOtherDimensions()
        {
            AssertUnchangedDimensions(PersonWellbeingDimension.Boredom);
        }

        [Test]
        public void ChangingMoodDoesNotChangeOtherDimensions()
        {
            AssertUnchangedDimensions(PersonWellbeingDimension.Mood);
        }

        [Test]
        public void SnapshotCaptureAndRestorePreserveAllDimensions()
        {
            var original = Person();
            original.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, -37);
            original.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 41);
            original.Wellbeing.ApplyDelta(PersonWellbeingDimension.Boredom, 52);
            original.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -23);
            var snapshot = original.CaptureWellbeing();
            var restored = Person("restored");

            restored.RestoreWellbeing(snapshot);

            AssertState(restored.Wellbeing, 43, 51, 62, 47);
        }

        [Test]
        public void SnapshotRestoreClampsInvalidValues()
        {
            var wellbeing = Person().Wellbeing;

            wellbeing.Restore(new PersonWellbeingSnapshot { Energy = -1, Stress = 101, Boredom = 30, Mood = 70 });

            AssertState(wellbeing, 0, 100, 30, 70);
        }

        [Test]
        public void ApplicantSnapshotCapturesPersonWellbeing()
        {
            var setup = Recruitment();
            try
            {
                var candidate = setup.coordinator.TryGenerateArrival();
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, -20);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 30);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Boredom, 40);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -50);

                var snapshot = candidate.Capture();

                AssertState(snapshot.Wellbeing, 60, 40, 50, 20);
            }
            finally
            {
                setup.coordinator.Dispose();
            }
        }

        [Test]
        public void HiringPreservesTheSamePersonsWellbeing()
        {
            var setup = Recruitment();
            try
            {
                var candidate = setup.coordinator.TryGenerateArrival();
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, -10);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 20);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Boredom, 30);
                candidate.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -40);
                var person = candidate.Person;

                var employee = setup.coordinator.Hire(candidate, ProfessionalRole.Actor);

                Assert.That(employee, Is.Not.Null);
                Assert.That(employee.Person, Is.SameAs(person));
                AssertState(employee.Person.Wellbeing, 70, 30, 40, 30);
            }
            finally
            {
                setup.coordinator.Dispose();
            }
        }

        [Test]
        public void ProfessionChangeDoesNotResetWellbeing()
        {
            var employee = new Employee(Person(), EmployeeRole.Actor, 1000);
            employee.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, -10);
            employee.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 20);
            employee.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Boredom, 30);
            employee.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -40);

            employee.ChangeProfession(EmployeeRole.Director);

            AssertState(employee.Person.Wellbeing, 70, 30, 40, 30);
        }

        [Test]
        public void PeopleOwnIndependentWellbeingState()
        {
            var first = Person("first");
            var second = Person("second");

            first.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, -80);

            Assert.That(first.Wellbeing.Energy, Is.EqualTo(0));
            Assert.That(second.Wellbeing.Energy, Is.EqualTo(80));
        }

        [Test]
        public void PersonInformationExposesAllWellbeingDimensions()
        {
            var person = Person();
            person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Energy, 5);
            person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress, 5);
            person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Boredom, 15);
            person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Mood, -30);
            var information = new List<PersonInformation>();

            new PersonInformationService().Collect(
                new PersonInformationContext { Person = person, Date = new SimulationDateTime(1930, 1, 1, 0, 0) },
                information);

            var wellbeingCard = information.Find(card => card.Category == "Person wellbeing");
            Assert.That(wellbeingCard, Is.Not.Null);
            Assert.That(wellbeingCard.Summary, Is.EqualTo("Energy: 85/100; Stress: 15/100; Boredom: 25/100; Mood: 40/100"));
        }

        private static void AssertUnchangedDimensions(PersonWellbeingDimension changed)
        {
            var wellbeing = Person().Wellbeing;
            wellbeing.ApplyDelta(changed, 1);
            var expected = changed switch
            {
                PersonWellbeingDimension.Energy => (81, 10, 10, 70),
                PersonWellbeingDimension.Stress => (80, 11, 10, 70),
                PersonWellbeingDimension.Boredom => (80, 10, 11, 70),
                PersonWellbeingDimension.Mood => (80, 10, 10, 71),
                _ => throw new ArgumentOutOfRangeException(nameof(changed))
            };

            AssertState(wellbeing, expected.Item1, expected.Item2, expected.Item3, expected.Item4);
        }

        private static void AssertState(PersonWellbeing wellbeing, int energy, int stress, int boredom, int mood)
        {
            Assert.That(wellbeing.Energy, Is.EqualTo(energy));
            Assert.That(wellbeing.Stress, Is.EqualTo(stress));
            Assert.That(wellbeing.Boredom, Is.EqualTo(boredom));
            Assert.That(wellbeing.Mood, Is.EqualTo(mood));
        }

        private static void AssertState(PersonWellbeingSnapshot snapshot, int energy, int stress, int boredom, int mood)
        {
            Assert.That(snapshot.Energy, Is.EqualTo(energy));
            Assert.That(snapshot.Stress, Is.EqualTo(stress));
            Assert.That(snapshot.Boredom, Is.EqualTo(boredom));
            Assert.That(snapshot.Mood, Is.EqualTo(mood));
        }

        private static PersonProfile Person(string id = "wellbeing-test") =>
            new PersonProfile(id, "Test Person", new SimulationDateTime(1900, 1, 1, 0, 0), ProfessionalRole.Unassigned, new TalentProfile(50, 50));

        private static (RecruitmentCoordinator coordinator, CandidateRouter router) Recruitment()
        {
            var clock = new SimulationClock();
            var router = new CandidateRouter();
            var coordinator = new RecruitmentCoordinator(clock, new CandidateGenerator(new SeededRandomSource(1930)), router);
            var facilities = new FacilityApplicantPool();
            facilities.Register(new RecruitmentFacility("wellbeing-test", RecruitmentDestination.StageSchool, new[] { ProfessionalRole.Actor }));
            coordinator.Facilities = facilities;
            return (coordinator, router);
        }

        private sealed class CandidateRouter : ICandidateWorldRouter
        {
            public CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate, Action<CandidateRouteResult, int> onComplete)
            {
                onComplete(CandidateRouteResult.Started, 0);
                return CandidateRouteResult.Started;
            }

            public CandidateRouteResult RouteToExit(Candidate candidate, Action<CandidateRouteResult, int> onComplete)
            {
                onComplete(CandidateRouteResult.Started, -1);
                return CandidateRouteResult.Started;
            }

            public void ReleaseCandidate(Candidate candidate) { }
        }
    }
}
