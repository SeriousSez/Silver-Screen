using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Domain.Work;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public sealed class BuildingPlacementAndConstructionTests
    {
        private SimulationClock _clock;
        private WorkService _work;
        private BuildingConstructionService _buildings;
        private StudioFinances _finance;
        private ResourceReservationBook _resources;
        [SetUp] public void Setup()
        {
            _clock = new SimulationClock(1930, 1, 1, 8, 0);
            _work = new WorkService(_clock, new SimulationScheduler(_clock));
            _finance = new StudioFinances(_clock.CurrentTime);
            _resources = new ResourceReservationBook();
            _buildings = new BuildingConstructionService(StudioBuildingDefinitions.Create(), new PlacementRect(0,0,100,100),
                new[]{new PlacementRect(40,40,8,8)}, _clock, _work, _finance, _resources);
        }
        [TearDown] public void Cleanup() { _buildings.Dispose(); _work.Dispose(); }
        private static Employee Builder(string id) => new Employee(id, id, EmployeeRole.ConstructionWorker, 50, 100, 80);

        [Test] public void TransformsBelongToInstancesAndRotateLocalEntrance()
        {
            var definition = _buildings.Definitions.Single(d => d.Id == StarterFeatureIds.Casting);
            var first = _buildings.Place(definition.Id, new BuildingPose(-20,0,90), out _);
            var second = _buildings.Place(definition.Id, new BuildingPose(20,0,-35), out _);
            Assert.That(first.Definition, Is.SameAs(second.Definition));
            var entrance = first.Pose.Transform(definition.Entrance);
            Assert.That(entrance.X, Is.EqualTo(-24.2f).Within(.001));
            Assert.That(entrance.Z, Is.EqualTo(0).Within(.001));
            var snapshot = JsonUtility.FromJson<PlacedBuildingSnapshot>(JsonUtility.ToJson(_buildings.Capture(first)));
            Assert.That(snapshot.InstanceId, Is.EqualTo(first.Id));
            Assert.That(snapshot.Pose.Yaw, Is.EqualTo(90));
            Assert.That(snapshot.State, Is.EqualTo(BuildingLifecycle.Planned));
        }
        [Test] public void InvalidPlacementDoesNotChargeAndCommittedPlacementChargesOnce()
        {
            string id = StarterFeatureIds.Casting;
            var cash = _finance.CurrentCash;
            Assert.That(_buildings.Place(id, new BuildingPose(49,0,37), out _), Is.Null);
            Assert.That(_buildings.Place(id, new BuildingPose(40,40,45), out _), Is.Null);
            Assert.That(_buildings.Place(id, new BuildingPose(float.NaN,0,0), out _), Is.Null);
            Assert.That(_finance.CurrentCash, Is.EqualTo(cash));
            var placed = _buildings.Place(id, new BuildingPose(0,0,37), out _, "one");
            Assert.That(placed, Is.Not.Null);
            Assert.That(_buildings.Place(id, new BuildingPose(0,1,-20), out _), Is.Null);
            Assert.That(_buildings.Place(id, new BuildingPose(25,0,0), out _, "one"), Is.Null);
            Assert.That(_finance.CurrentCash, Is.EqualTo(cash - placed.Definition.Cost));
            var poor = new StudioFinances(_clock.CurrentTime, Money.Zero);
            using var unaffordable = new BuildingConstructionService(StudioBuildingDefinitions.Create(),new PlacementRect(0,0,100,100),null,_clock,_work,poor,_resources);
            Assert.That(unaffordable.Place(id, default, out _), Is.Null);
        }
        [Test] public void ConstructionRequiresArrivalAndCompletionAloneUnlocksApplicants()
        {
            var pools = new FacilityApplicantPool();
            var site = _buildings.Place(StarterFeatureIds.Casting, default, out _);
            int completions = 0;
            _buildings.Operational += b => { completions++; pools.Register(new RecruitmentFacility(b.Id, RecruitmentDestination.CastingOffice,b.Definition.Applicants)); };
            _clock.Advance(600d);
            Assert.That(_buildings.Progress(site), Is.Zero);
            Assert.That(_buildings.AssignWorker(site, Builder("a")), Is.True);
            _clock.Advance(600d);
            Assert.That(_buildings.Progress(site), Is.Zero, "Travel earns no construction work.");
            Assert.That(pools.Facilities, Is.Empty);
            _buildings.WorkerArrived("a"); _clock.Advance(60d);
            Assert.That(_buildings.Progress(site), Is.EqualTo(1m/16m).Within(.0001m));
            Assert.That(_buildings.IsEstablished(StarterFeatureIds.Casting), Is.False);
            _clock.Advance(1000d);
            Assert.That(site.IsOperational, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(_resources.Count, Is.Zero);
            Assert.That(pools.Facilities.Single().Professions, Is.EquivalentTo(new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra}));
        }
        [Test] public void CrewCapacityDiminishesAndCancellationReleasesWorkersWithoutRefund()
        {
            var site = _buildings.Place(StarterFeatureIds.Stage, default, out _);
            foreach (string id in new[]{"a","b","c","d","e","f"}) { Assert.That(_buildings.AssignWorker(site,Builder(id)),Is.True); _buildings.WorkerArrived(id); }
            Assert.That(_buildings.AssignWorker(site,Builder("g")),Is.False);
            _clock.Advance(60d);
            decimal earned = _buildings.Progress(site)*site.Definition.RequiredWork;
            Assert.That(earned, Is.GreaterThan(3).And.LessThan(5));
            var cash = _finance.CurrentCash;
            Assert.That(_buildings.Cancel(site),Is.True);
            Assert.That(_resources.Count,Is.Zero);
            Assert.That(_finance.CurrentCash,Is.EqualTo(cash));
            _clock.Advance(600d);
            Assert.That(site.IsOperational,Is.False);
            Assert.That(_buildings.Validate(StarterFeatureIds.Stage,default),Is.Null);
        }
        [Test] public void StarterPoolContainsOnlyServiceProfessionsAndHonoursFacilityCapacity()
        {
            var pool = new FacilityApplicantPool();
            pool.Register(new RecruitmentFacility("service",RecruitmentDestination.ServiceFacility,new[]{ProfessionalRole.ConstructionWorker,ProfessionalRole.Groundskeeper}));
            Assert.That(pool.Select(_=>true,out var first).Id,Is.EqualTo("service"));
            Assert.That(first,Is.EqualTo(ProfessionalRole.ConstructionWorker));
            pool.Select(_=>true,out var second);
            Assert.That(second,Is.EqualTo(ProfessionalRole.Groundskeeper));
            Assert.That(pool.Select(_=>false,out _),Is.Null);
            Assert.That(_buildings.Buildings,Is.Empty);
        }
        [Test] public void TutorialObservesRealCompletedConstruction()
        {
            using var tutorial = new TutorialSession(new NewStudioOptions(),_clock);
            using var events = new TutorialGameEvents(tutorial,_buildings,new SilverScreen.Domain.Movie.StudioProductionSlate(),new StudioAnnouncementQueue(),_clock);
            tutorial.Continue(); events.EmployeeHired(Builder("a")); tutorial.Continue();
            var site = _buildings.Place(StarterFeatureIds.Headquarters,default,out _);
            Assert.That(tutorial.CurrentStep.Id,Is.EqualTo("headquarters"));
            _buildings.AssignWorker(site,Builder("a")); _buildings.WorkerArrived("a");
            _clock.Advance(2200d);
            Assert.That(tutorial.CurrentStep.Id,Is.EqualTo("casting"));
            Assert.That(site.IsOperational,Is.True);
        }
    }
}
