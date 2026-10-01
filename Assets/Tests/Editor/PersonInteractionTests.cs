using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonInteractionTests
    {
        private SimulationClock _clock;
        private WorkService _work;
        private ResourceReservationBook _resources;
        [SetUp] public void Setup()
        { _clock = new SimulationClock(); _work = new WorkService(_clock, new SimulationScheduler(_clock)); _resources = new ResourceReservationBook(); }
        [TearDown] public void Cleanup() => _work.Dispose();
        [TestCase(ProfessionalRole.ConstructionWorker, ProfessionalRole.Actor)]
        [TestCase(ProfessionalRole.Actor, ProfessionalRole.Groundskeeper)]
        public void FacilityOriginNeverRestrictsProfessionAndIdentitySurvives(ProfessionalRole origin, ProfessionalRole chosen)
        {
            using var recruitment = new RecruitmentCoordinator(_clock, new CandidateGenerator(new SeededRandomSource(1930)), new Router());
            var pool = new FacilityApplicantPool(); recruitment.Facilities = pool;
            pool.Register(new RecruitmentFacility("source", RecruitmentDestination.ServiceFacility, new[] { origin }));
            var candidate = recruitment.TryGenerateArrival();
            pool.Register(new RecruitmentFacility("target", RecruitmentDestination.CastingOffice, new[] { chosen }));
            var person = candidate.Person; var traits = person.TraitIds.ToArray(); var talent = person.Talent;
            talent.SetPrimaryAbility(chosen, 0);
            Assert.That(candidate.JobSought, Is.EqualTo(origin));
            var employee = recruitment.Hire(candidate, chosen, "target");
            Assert.That(employee, Is.Not.Null); Assert.That(employee.Person, Is.SameAs(person));
            Assert.That(employee.Id, Is.EqualTo(person.Id)); Assert.That(employee.Person.ProfessionalRole, Is.EqualTo(chosen));
            Assert.That((int)employee.Role, Is.EqualTo((int)chosen));
            Assert.That(employee.Person.Talent, Is.SameAs(talent)); CollectionAssert.AreEqual(traits, person.TraitIds);
            Assert.That(candidate.JobSought, Is.EqualTo(origin)); Assert.That(candidate.Capture().Profession, Is.EqualTo(origin));
            Assert.That(employee.Salary, Is.EqualTo(candidate.SalaryExpectation));
            Assert.That(recruitment.Hire(candidate, chosen, "target"), Is.Null);
        }
        [Test] public void SpotsRequireHoldProximityCompatibilityAndOperationalFacility()
        {
            using var recruitment = new RecruitmentCoordinator(_clock, new CandidateGenerator(new SeededRandomSource(3)), new Router());
            recruitment.Facilities = new FacilityApplicantPool();
            recruitment.Facilities.Register(new RecruitmentFacility("service", RecruitmentDestination.ServiceFacility, new[] { ProfessionalRole.ConstructionWorker }));
            var candidate = recruitment.TryGenerateArrival();
            recruitment.Facilities.Register(new RecruitmentFacility("casting", RecruitmentDestination.CastingOffice, new[] { ProfessionalRole.Actor }));
            using var practice = new PersonPracticeService(_work, _resources);
            var go = new GameObject("Spot test");
            try
            {
                var spot = go.AddComponent<PersonInteractionSpot>();
                spot.Configure("casting:actor", "casting", PersonSpotActivity.Hire, ProfessionalRole.Actor, "Actor");
                Assert.That(spot.IsVisible(false, Vector3.zero, candidate, null, recruitment, practice), Is.False);
                Assert.That(spot.IsVisible(true, Vector3.zero, candidate, null, recruitment, practice), Is.True);
                Assert.That(spot.IsVisible(true, Vector3.one * 100, candidate, null, recruitment, practice), Is.False);
                Assert.That(spot.IsRelevant(null, Actor(), recruitment, practice), Is.False);
                recruitment.Facilities.Remove("casting");
                Assert.That(spot.IsRelevant(candidate, null, recruitment, practice), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void PracticeTargetAvailabilityFollowsExistingResourceClaims()
        {
            using var practice=new PersonPracticeService(_work,_resources); var actor=Actor();
            Assert.That(practice.CanPracticeAt(actor,"practice:comedy"),Is.True);
            Assert.That(_resources.TryAcquire("other",new[]{new ResourceKey("person",actor.Id)},out var claim,out _),Is.True);
            Assert.That(practice.CanPracticeAt(actor,"practice:comedy"),Is.False);
            _resources.Release(claim.Id);
            Assert.That(_resources.TryAcquire("other",new[]{new ResourceKey("interaction","practice:comedy")},out claim,out _),Is.True);
            Assert.That(practice.CanPracticeAt(actor,"practice:comedy"),Is.False);
            _resources.Release(claim.Id); Assert.That(practice.CanPracticeAt(actor,"practice:comedy"),Is.True);
        }
        [Test] public void PracticeUsesNormalWorkCapacityProgressAndReleasesItsSlot()
        {
            using var practice = new PersonPracticeService(_work, _resources);
            var actor = Actor();
            Assert.That(practice.TryStart(actor, "practice:comedy"), Is.True);
            Assert.That(actor.CurrentState, Is.EqualTo(EmployeeState.Practicing));
            Assert.That(practice.TryStart(Actor("other"), "practice:comedy"), Is.False);
            _clock.AdvanceTo(_clock.Now + SimulationDuration.FromMinutes(15));
            Assert.That(practice.Progress(actor), Is.EqualTo(.5m));
            _clock.AdvanceTo(_clock.Now + SimulationDuration.FromMinutes(15));
            Assert.That(actor.Person.Talent.GetGenreExperience("comedy"), Is.EqualTo(1));
            Assert.That(actor.CurrentState, Is.EqualTo(EmployeeState.Idle)); Assert.That(_resources.Count, Is.Zero);
        }
        [Test] public void NewObligationInterruptsPracticeWithoutLosingTheAssignment()
        {
            using var practice = new PersonPracticeService(_work, _resources);
            var actor = Actor();
            Assert.That(practice.TryStart(actor, "practice:comedy"), Is.True);
            var obligation = new EmployeeIntent(EmployeeIntentPurpose.ReportToStage, "Report for filming", "stage-1");
            actor.SetState(EmployeeState.Walking); actor.SetIntent(obligation);
            Assert.That(practice.IsPracticing(actor), Is.False);
            Assert.That(practice.IsOccupied("practice:comedy"), Is.False);
            Assert.That(actor.CurrentIntent, Is.SameAs(obligation));
            Assert.That(actor.CurrentState, Is.EqualTo(EmployeeState.Walking));
            Assert.That(actor.Person.Talent.GetGenreExperience("comedy"), Is.Zero);
            Assert.That(practice.CanPractice(actor), Is.False);
            Assert.That(_resources.Count, Is.Zero);
        }
        [Test] public void DisplacedResourcePreservesWorkAndCannotEarnUntilAvailable()
        {
            var key = new ResourceKey("person", "actor");
            _work.Create(new WorkDefinition("take", "movie", "filming", WorkQuantity.FromUnits("minutes", 30)));
            _work.SetAssignments("take", new[] { new WorkAssignment(key, "acting") });
            _work.RefreshCapacity("take", new WorkRate(WorkQuantity.FromUnits("minutes", 1), SimulationDuration.FromMinutes(1)));
            _clock.AdvanceTo(_clock.Now + SimulationDuration.FromMinutes(5));
            _work.SetResourceAvailable(key, false);
            _clock.AdvanceTo(_clock.Now + SimulationDuration.FromMinutes(10));
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(5));
            Assert.That(_work.GetSnapshot("take").Assignments.Count, Is.EqualTo(1));
            _work.SetResourceAvailable(key, true);
            _clock.AdvanceTo(_clock.Now + SimulationDuration.FromMinutes(5));
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(10));
        }
        [Test] public void ExpandedInformationIsReadOnlyAndUrgencyIsContextual()
        {
            var actor = Actor(); actor.Morale = 10;
            var intent = new EmployeeIntent(EmployeeIntentPurpose.ReportToStage, "Needed for filming — Test Movie", "stage-1"); actor.SetIntent(intent);
            var context = new PersonInformationContext { Person = actor.Person, Employee = actor, Date = _clock.CurrentTime };
            var cards = new List<PersonInformation>(); var service = new PersonInformationService();
            service.Collect(context, cards);
            Assert.That(cards[0].Category, Is.EqualTo("Wellbeing"));
            Assert.That(cards.Any(c => c.Category == "Work" && c.Expanded.Contains("Test Movie")), Is.True);
            Assert.That(actor.CurrentIntent, Is.SameAs(intent)); Assert.That(actor.CurrentState, Is.EqualTo(EmployeeState.Idle));
            Assert.That(_clock.IsPaused, Is.False);
            actor.Morale = 80; service.Collect(context, cards); Assert.That(cards[0].Category, Is.EqualTo("Work"));
        }
        private static Employee Actor(string id = "actor") => new Employee(new PersonProfile(id, "Test Actor", new SimulationDateTime(1900, 1, 1, 0, 0), ProfessionalRole.Actor, new TalentProfile(0, 0)), EmployeeRole.Actor, 1000);
        private sealed class Router : ICandidateWorldRouter
        {
            public CandidateRouteResult RouteToRecruitmentLocation(Candidate c, Action<CandidateRouteResult, int> done) { done(CandidateRouteResult.Started, 0); return CandidateRouteResult.Started; }
            public CandidateRouteResult RouteToExit(Candidate c, Action<CandidateRouteResult, int> done) { done(CandidateRouteResult.Started, 0); return CandidateRouteResult.Started; }
            public void ReleaseCandidate(Candidate c) { }
        }
    }
}
