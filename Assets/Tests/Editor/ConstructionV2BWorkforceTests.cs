using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Domain.Work;
using SilverScreen.Presentation.Buildings;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ConstructionV2BWorkforceTests
    {
        private SimulationClock _clock;
        private WorkService _work;
        private ResourceReservationBook _resources;
        private BuildingConstructionService _service;

        [SetUp]
        public void Setup()
        {
            _clock = new SimulationClock();
            _work = new WorkService(_clock, new SimulationScheduler(_clock));
            _resources = new ResourceReservationBook();
            _service = new BuildingConstructionService(StudioBuildingDefinitions.Create(), new PlacementRect(0, 0, 300, 300),
                null, _clock, _work, new StudioFinances(_clock.CurrentTime), _resources);
        }

        [TearDown]
        public void Cleanup() { _service.Dispose(); _work.Dispose(); }

        private static Employee Worker(string id, int skill) => new Employee(id, id, EmployeeRole.ConstructionWorker, skill, 100);

        [Test]
        public void StarterRequirementsAuthorDistinctWorkPhaseWeightsAndCapacities()
        {
            var casting = _service.Definitions.Single(d => d.Id == StarterFeatureIds.Casting);
            var administration = _service.Definitions.Single(d => d.Id == StarterFeatureIds.Headquarters);
            var stage = _service.Definitions.Single(d => d.Id == StarterFeatureIds.Stage);
            Assert.That(casting.RequiredWork, Is.LessThan(administration.RequiredWork));
            Assert.That(administration.RequiredWork, Is.LessThan(stage.RequiredWork));
            Assert.That((casting.RequiredWork, casting.RecommendedWorkers, casting.WorkerCapacity), Is.EqualTo((16m, 2, 3)));
            Assert.That((administration.RequiredWork, administration.RecommendedWorkers, administration.WorkerCapacity), Is.EqualTo((30m, 3, 5)));
            Assert.That((stage.RequiredWork, stage.RecommendedWorkers, stage.WorkerCapacity), Is.EqualTo((72m, 5, 8)));
            foreach (var definition in _service.Definitions)
            {
                Assert.That(definition.Construction.Phases.Sum(p => p.WorkShare), Is.EqualTo(1m));
                Assert.That(definition.Construction.Phases.All(p => p.UsefulWorkerCapacity <= definition.WorkerCapacity), Is.True);
            }
            Assert.That(stage.Construction.CapacityFor(ConstructionPhase.Structure), Is.EqualTo(8));
            Assert.That(stage.Construction.CapacityFor(ConstructionPhase.Exterior), Is.EqualTo(5));
        }

        [Test]
        public void ProficiencyAndDiminishingReturnsProduceUsefulButBoundedCrews()
        {
            var average = Enumerable.Range(0, 4).Select(i => Worker("average-" + i, 50)).ToArray();
            decimal one = BuildingConstructionService.TeamProductivity(average.Take(1));
            decimal two = BuildingConstructionService.TeamProductivity(average.Take(2));
            decimal three = BuildingConstructionService.TeamProductivity(average.Take(3));
            decimal four = BuildingConstructionService.TeamProductivity(average);
            Assert.That(one, Is.EqualTo(1m).Within(.001m));
            Assert.That(two, Is.InRange(1.7m, 1.9m));
            Assert.That(three, Is.InRange(2.3m, 2.6m));
            Assert.That(four, Is.InRange(2.8m, 3.2m));
            Assert.That(BuildingConstructionService.TeamProductivity(new[] { Worker("novice", 0) }), Is.EqualTo(.65m).Within(.001m));
            Assert.That(BuildingConstructionService.TeamProductivity(new[] { Worker("expert", 100) }), Is.EqualTo(1.35m).Within(.001m));
        }

        [Test]
        public void PhaseCapacityReservesDistinctReusableWorkSpots()
        {
            var site = _service.Place(StarterFeatureIds.Stage, default, out _);
            var workers = Enumerable.Range(0, 6).Select(i => Worker("worker-" + i, 25)).ToArray();
            var slots = workers.Select(w => { Assert.That(_service.AssignWorker(site, w, out int slot), Is.True); return slot; }).ToArray();
            Assert.That(slots.Distinct().Count(), Is.EqualTo(6));
            Assert.That(_resources.Count, Is.EqualTo(6));
            Assert.That(_service.AssignWorker(site, Worker("excess", 25)), Is.False);
            _service.ReleaseWorker(workers[2].Id);
            Assert.That(_service.AssignWorker(site, Worker("replacement", 25), out int replacement), Is.True);
            Assert.That(replacement, Is.EqualTo(slots[2]));
            Assert.That(_resources.Count, Is.EqualTo(6));
        }

        [Test]
        public void OnlyArrivedWorkersContributeAndStatusReportsCurrentOutput()
        {
            var site = _service.Place(StarterFeatureIds.Casting, default, out _);
            var worker = Worker("builder", 0);
            Assert.That(_service.AssignWorker(site, worker, out _), Is.True);
            _clock.Advance(60d);
            Assert.That(_service.Progress(site), Is.Zero, "Travel must produce no work.");
            Assert.That(_service.WorkerArrived(worker.Id), Is.True);
            _clock.Advance(60d);
            var status = _service.GetStatus(site);
            Assert.That(status.WorkCompleted, Is.EqualTo(.65m).Within(.001m));
            Assert.That(status.ActiveWorkers, Is.EqualTo(1));
            Assert.That(status.UsefulWorkerCapacity, Is.EqualTo(3));
            Assert.That(status.RelativeProductivity, Is.EqualTo(.65m).Within(.001m));
        }

        [Test]
        public void ActiveConstructionBuildsAndPreservesConstructionProficiency()
        {
            var site = _service.Place(StarterFeatureIds.Casting, default, out _);
            var worker = Worker("learner", 0);
            _service.AssignWorker(site, worker); _service.WorkerArrived(worker.Id);
            _clock.Advance(1250d);
            int learned = worker.GetProfessionSkill(EmployeeRole.ConstructionWorker);
            Assert.That(learned, Is.GreaterThan(0));
            worker.ChangeProfession(EmployeeRole.Groundskeeper);
            Assert.That(worker.GetProfessionSkill(EmployeeRole.ConstructionWorker), Is.EqualTo(learned));
        }

        [Test]
        public void PhaseActivityPointsMatchCapacityAndChangePurposefully()
        {
            var site = _service.Place(StarterFeatureIds.Stage, default, out _);
            var foundation = ConstructionDressingGenerator.Generate(site, ConstructionPhase.Foundation).Activities;
            var structure = ConstructionDressingGenerator.Generate(site, ConstructionPhase.Structure).Activities;
            Assert.That(foundation.Count, Is.EqualTo(site.Definition.Construction.CapacityFor(ConstructionPhase.Foundation)));
            Assert.That(structure.Count, Is.EqualTo(site.Definition.Construction.CapacityFor(ConstructionPhase.Structure)));
            Assert.That(structure.Select(p => p.LocalPosition).Distinct().Count(), Is.EqualTo(structure.Count));
            Assert.That(foundation.Select(p => p.LocalPosition).SequenceEqual(structure.Take(foundation.Count).Select(p => p.LocalPosition)), Is.False);
        }
    }
}
