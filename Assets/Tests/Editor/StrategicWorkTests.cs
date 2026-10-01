using System;
using NUnit.Framework;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;
using SilverScreen.Domain.Resources;
using SilverScreen.Presentation.SimulationTime;

namespace SilverScreen.Tests.EditMode
{
    public sealed class StrategicWorkTests
    {
        private SimulationClock _clock;
        private WorkService _work;
        private SimulationScheduler _scheduler;
        private const string Measure = "legacy-filming";
        private static WorkQuantity Q(decimal units) => WorkQuantity.FromUnits(Measure, units);
        private static WorkRate Rate(decimal units) => new WorkRate(Q(units), SimulationDuration.FromMinutes(1));

        [SetUp] public void Setup() { _clock = new SimulationClock(); _scheduler = new SimulationScheduler(_clock); _work = new WorkService(_clock, _scheduler); }
        [TearDown] public void Cleanup() => _work.Dispose();
        private void Create(string id, int units, string[] dependencies = null, string[] conditions = null)
        {
            _work.Create(new WorkDefinition(id, "movie", "test", Q(units), dependencies, conditions));
            _work.RefreshCapacity(id, Rate(1));
        }

        [Test]
        public void CapacityChangesAccountForEarnedWorkBeforeChange()
        {
            Create("take", 480);
            _clock.Advance(120.0);
            _work.RefreshCapacity("take", Rate(2));
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(120));
            Assert.That((_work.GetForecast("take").EstimatedCompletion.Value - _clock.Now).Seconds, Is.EqualTo(180 * 60));
            _clock.Advance(179.0);
            Assert.That(_work.GetSnapshot("take").Status, Is.EqualTo(WorkStatus.Running));
            _clock.Advance(1.0);
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(480));
            Assert.That(_work.GetSnapshot("take").Status, Is.EqualTo(WorkStatus.Completed));
        }

        [Test]
        public void SuspensionConditionsAndAssignmentChangesDoNotEarnUnavailableWork()
        {
            Create("take", 10, conditions: new[] { "ready" });
            _clock.Advance(10.0);
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.Zero);
            Assert.That(_work.GetForecast("take").EstimatedCompletion, Is.Null);
            _work.SetCondition("ready", true); _clock.Advance(2.0);
            _work.Suspend("take"); _clock.Advance(100.0);
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(2));
            _work.Resume("take"); _clock.Advance(2.0);
            _work.SetAssignments("take", new[] { new WorkAssignment(new ResourceKey("person", "actor"), "acting") });
            _clock.Advance(100.0);
            Assert.That(_work.GetSnapshot("take").Completed.Units, Is.EqualTo(4));
            _work.RefreshCapacity("take", Rate(1)); _clock.Advance(6.0);
            Assert.That(_work.GetSnapshot("take").Status, Is.EqualTo(WorkStatus.Completed));
        }

        [Test]
        public void ConcurrentPreparationUsesLatestDependencyAndDoesNotSumDurations()
        {
            Create("film-a", 100);
            Create("lighting-b", 40);
            Create("dressing-b", 32);
            Create("film-b", 10, new[] { "lighting-b", "dressing-b" });
            var start = _clock.Now;
            Assert.That((_work.GetForecast("film-b").EstimatedCompletion.Value - start).Seconds, Is.EqualTo(50 * 60));
            _clock.Advance(32.0);
            Assert.That(_work.GetSnapshot("film-b").Completed.Units, Is.Zero);
            _clock.Advance(8.0);
            Assert.That(_work.GetSnapshot("film-b").Status, Is.EqualTo(WorkStatus.Running));
            _clock.Advance(10.0);
            Assert.That(_work.GetSnapshot("film-b").Status, Is.EqualTo(WorkStatus.Completed));
            Assert.That(_work.GetSnapshot("film-a").Completed.Units, Is.EqualTo(50));
        }

        [Test]
        public void ReadOnlyProgressAndForecastNeverCompleteWorkOrChangeRevision()
        {
            Create("take", 1);
            int completed = 0;
            _work.Changed += s => { if (s.Status == WorkStatus.Completed) completed++; };
            _clock.Advance(0.5);
            var before = _work.GetSnapshot("take");
            for (int i = 0; i < 20; i++) { _work.GetForecast("take"); _work.GetSnapshot("take"); }
            Assert.That(_work.GetSnapshot("take").Revision, Is.EqualTo(before.Revision));
            Assert.That(completed, Is.Zero);
            _clock.Advance(10.0);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void InvalidMeasuresCyclesAndCanceledDependenciesAreSafe()
        {
            Create("a", 10); Create("b", 10, new[] { "a" });
            Assert.Throws<ArgumentException>(() => _work.SetDependencies("a", new[] { "b" }));
            Assert.Throws<ArgumentException>(() => _work.RefreshCapacity("a", new WorkRate(WorkQuantity.FromUnits("writing", 1), new SimulationDuration(60))));
            _work.Cancel("a"); _clock.Advance(100.0);
            Assert.That(_work.GetSnapshot("b").Completed.Units, Is.Zero);
            Assert.That(_work.GetForecast("b").EstimatedCompletion, Is.Null);
        }

        [Test]
        public void ReservationsAreAtomicAcrossPeopleFacilitiesAndAssignmentPurposes()
        {
            var book = new ResourceReservationBook();
            var actor = new ResourceKey("person", "actor-a"); var stage = new ResourceKey("facility", "stage-1");
            Assert.That(book.TryAcquire("movie-a", new[] { actor, stage, actor }, out var first, out _), Is.True);
            Assert.That(first.Resources.Count, Is.EqualTo(2));
            Assert.That(book.TryAcquire("movie-b-directing", new[] { actor, new ResourceKey("facility", "stage-2") }, out _, out var conflicts), Is.False);
            Assert.That(conflicts[0].OwnerId, Is.EqualTo("movie-a"));
            Assert.That(book.Count, Is.EqualTo(1));
            Assert.That(book.TryAcquire("movie-b", new[] { new ResourceKey("facility", "stage-2") }, out var second, out _), Is.True);
            Assert.That(book.TryReplace(first.Id, "retake", new[] { actor, second.Resources[0] }, out _, out _), Is.False);
            Assert.That(book.GetClaims()[0].OwnerId, Is.EqualTo("movie-a"));
            Assert.That(book.TryReplace(first.Id, "retake", new[] { actor, stage }, out var retake, out _), Is.True);
            Assert.That(retake.Id, Is.EqualTo(first.Id));
            Assert.That(book.Release(first.Id), Is.True); Assert.That(book.Release(first.Id), Is.False);
        }

        [Test]
        public void WorldActivitySecondsFollowSpeedWhileFocusedObservationStaysRealTime()
        {
            foreach (var speed in new[] { SimulationSpeed.Normal, SimulationSpeed.Fast, SimulationSpeed.VeryFast })
            {
                _clock.SetSpeed(speed);
                Assert.That(LocalPresentationTime.Delta(0.25f, _clock), Is.EqualTo(0.25f * (int)speed));
                Assert.That(LocalPresentationTime.Delta(0.25f, _clock, true), Is.EqualTo(0.25f));
            }
            using (_clock.AcquirePause("view", "observation"))
            {
                Assert.That(LocalPresentationTime.Delta(0.25f, _clock), Is.Zero);
                Assert.That(LocalPresentationTime.Delta(0.25f, _clock, true), Is.EqualTo(0.25f));
            }
        }
    }
}
