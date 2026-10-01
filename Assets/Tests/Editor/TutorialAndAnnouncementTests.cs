using System;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public sealed class TutorialAndAnnouncementTests
    {
        private static SimulationClock Clock() => new SimulationClock(1930, 1, 1, 8, 0);

        [Test]
        public void OffBypassesOnlyTutorialGatesAndDoesNotGrantBuildings()
        {
            using var tutorial = new TutorialSession(new NewStudioOptions { TutorialEnabled = false }, Clock());
            var buildings = new StarterEstablishmentService(tutorial, id => id != StarterFeatureIds.Stage);
            Assert.That(tutorial.Status, Is.EqualTo(TutorialStatus.Disabled));
            Assert.That(buildings.CanEstablish(StarterFeatureIds.Casting), Is.True);
            Assert.That(buildings.CanEstablish(StarterFeatureIds.Stage), Is.False, "Normal availability still applies.");
            Assert.That(buildings.IsEstablished(StarterFeatureIds.Casting), Is.False);
            Assert.That(tutorial.Allows(StarterFeatureIds.SpecializedSets), Is.True);
        }

        [Test]
        public void RealEstablishmentAdvancesAndSkipPreservesStudioAndProduction()
        {
            var clock = Clock();
            using var tutorial = new TutorialSession(new NewStudioOptions(), clock);
            var buildings = new StarterEstablishmentService(tutorial, id => true);
            var slate = new StudioProductionSlate();
            var movie = new MovieProject("existing", "Existing", "drama", "Drama", 2000, clock.CurrentTime);
            slate.AddProject(movie);
            using var events = new TutorialGameEvents(tutorial, buildings, slate, new StudioAnnouncementQueue(), clock);
            Assert.That(buildings.Establish(StarterFeatureIds.Headquarters), Is.False);
            tutorial.Continue();
            events.EmployeeHired(new Employee("builder", "Builder", EmployeeRole.ConstructionWorker, 50, 100, 80));
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("headquarters"));
            Assert.That(buildings.Establish(StarterFeatureIds.Casting), Is.False);
            Assert.That(buildings.Establish(StarterFeatureIds.Headquarters), Is.True);
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("casting"));
            tutorial.Skip();
            Assert.That(tutorial.Status, Is.EqualTo(TutorialStatus.Skipped));
            Assert.That(buildings.CanEstablish(StarterFeatureIds.Stage), Is.True);
            Assert.That(buildings.IsEstablished(StarterFeatureIds.Headquarters), Is.True);
            Assert.That(slate.ActiveMovie, Is.SameAs(movie));
            Assert.That(movie.Budget, Is.EqualTo(2000));
            Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public void FullGuideTracksOneScreenplayThroughRealCastingFootageAndRelease()
        {
            var clock = Clock();
            using var tutorial = new TutorialSession(new NewStudioOptions(), clock);
            var buildings = new StarterEstablishmentService(tutorial, id => true);
            var slate = new StudioProductionSlate();
            using var events = new TutorialGameEvents(tutorial, buildings, slate, new StudioAnnouncementQueue(), clock);
            tutorial.Continue(); events.EmployeeHired(new Employee("builder", "Builder", EmployeeRole.ConstructionWorker, 50, 100, 80)); buildings.Establish(StarterFeatureIds.Headquarters); buildings.Establish(StarterFeatureIds.Casting);
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("stage"));
            events.EmployeeHired(new Employee("writer", "Writer", EmployeeRole.Writer, 80, 100, 80));
            buildings.Establish(StarterFeatureIds.Stage);
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("screenplay"));
            var screenplay = MovieReleaseServiceTests.CreateCompletedScreenplay(1);
            events.ObserveScreenplay(screenplay);
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("prepare"));
            var movie = new ScreenplayProductionAdapter().Adapt(screenplay, clock.CurrentTime, "Drama").Movie;
            slate.AddProject(movie);
            movie.AssignDirector(new Employee("director", "Director", EmployeeRole.Director, 70, 100, 80));
            int index = 0;
            foreach (var role in movie.CastRoles)
            {
                movie.AssignActorToRole(role.Id, new Employee("actor-" + index++, "Actor", EmployeeRole.Actor, 70, 100, 80));
                role.CompleteCasting();
            }
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("film"));
            foreach (var scene in movie.Scenes) ProductionLifecycleRegressionTests.CompleteScene(scene);
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("film"), "Quality finalization is still required.");
            movie.TrySetProductionResult(new MovieProductionResult(60, 60, 60, 60));
            Assert.That(tutorial.CurrentStep.Id, Is.EqualTo("release"));
            var unrelated = new MovieProject("other", "Other", "drama", "Drama", 0, clock.CurrentTime);
            slate.AddProject(unrelated);
            ProductionLifecycleRegressionTests.CompleteMovie(unrelated);
            unrelated.TrySetProductionResult(new MovieProductionResult(50, 50, 50, 50));
            unrelated.TryRelease(clock.CurrentTime, new MovieTheatricalRun(unrelated.Id, clock.CurrentTime, 50, Money.FromDollars(10000)));
            Assert.That(tutorial.Status, Is.EqualTo(TutorialStatus.Active));
            Assert.That(movie.TryRelease(clock.CurrentTime, new MovieTheatricalRun(movie.Id, clock.CurrentTime, 60, Money.FromDollars(10000))), Is.True);
            Assert.That(tutorial.Status, Is.EqualTo(TutorialStatus.Completed));
            Assert.That(tutorial.Capture().CompletedStepIds.Count, Is.EqualTo(10));
            Assert.That(tutorial.Allows(StarterFeatureIds.SpecializedSets), Is.True);
            Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public void GuideUsesOwnedPauseLeaseAndProgressRoundTripsWithoutSharingMutableState()
        {
            var clock = Clock();
            clock.SetUserPaused(true);
            using var tutorial = new TutorialSession(new NewStudioOptions(), clock);
            tutorial.Continue(); tutorial.Continue();
            Assert.That(clock.IsPaused, Is.True, "Dismiss must preserve user pause.");
            clock.SetUserPaused(false);
            Assert.That(clock.IsPaused, Is.False);
            tutorial.ReopenMessage();
            Assert.That(clock.IsPaused, Is.True);
            var snapshot = JsonUtility.FromJson<TutorialProgress>(JsonUtility.ToJson(tutorial.Capture()));
            tutorial.Dispose();
            Assert.That(clock.IsPaused, Is.False);
            using var restored = new TutorialSession(new NewStudioOptions(), clock, snapshot);
            snapshot.UnlockedIds.Clear();
            Assert.That(restored.Allows(StarterFeatureIds.Headquarters), Is.False);
            Assert.That(restored.CurrentStep.Id, Is.EqualTo("hire-builder"));
            Assert.That(clock.IsPaused, Is.True);
            restored.Skip(); Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public void PaDeduplicatesOrdersPrioritiesExpiresCooldownAndClearsResolvedRequestsWithoutPausing()
        {
            var clock = Clock();
            var queue = new StudioAnnouncementQueue();
            long now = clock.Now.Seconds;
            var actor = new AnnouncementRequest(AnnouncementType.ProductionNeedsActor, AnnouncementPriority.Normal, "movie", now, 60);
            Assert.That(queue.Request(actor), Is.True);
            Assert.That(queue.Request(actor), Is.False);
            queue.Request(new AnnouncementRequest(AnnouncementType.ProductionNeedsDirector, AnnouncementPriority.Urgent, "movie", now));
            Assert.That(queue.TakeNext().Type, Is.EqualTo(AnnouncementType.ProductionNeedsDirector));
            Assert.That(queue.TakeNext(), Is.SameAs(actor));
            Assert.That(queue.Request(new AnnouncementRequest(actor.Type, actor.Priority, "movie", now + 59, 60)), Is.False);
            Assert.That(queue.Request(new AnnouncementRequest(actor.Type, actor.Priority, "movie", now + 60, 60)), Is.True);
            queue.Resolve(actor.Type, "movie");
            Assert.That(queue.PendingCount, Is.Zero);
            Assert.That(queue.Request(new AnnouncementRequest(actor.Type, actor.Priority, "movie", now + 61, 60)), Is.True);
            Assert.That(clock.IsPaused, Is.False);
            var before = clock.Now; clock.Advance(1);
            Assert.That(clock.Now.Seconds, Is.GreaterThan(before.Seconds));
        }

        [Test]
        public void ProductionPaUsesStateEdgesAndRemovesStaleActorRequest()
        {
            var clock = Clock();
            using var tutorial = new TutorialSession(new NewStudioOptions { TutorialEnabled = false }, clock);
            var queue = new StudioAnnouncementQueue(); var slate = new StudioProductionSlate();
            using var events = new TutorialGameEvents(tutorial, new StarterEstablishmentService(tutorial, id => true), slate, queue, clock);
            var movie = new MovieProject("movie", "Movie", "drama", "Drama", 0, clock.CurrentTime);
            movie.AddRole(new MovieRole("lead", MovieRoleType.Protagonist, "Lead"));
            slate.AddProject(movie);
            Assert.That(queue.PendingCount, Is.EqualTo(2));
            movie.SetDirectorArrivedAtStage(false);
            Assert.That(queue.PendingCount, Is.EqualTo(2));
            movie.AssignActorToRole("lead", new Employee("actor", "Actor", EmployeeRole.Actor, 60, 100, 80));
            Assert.That(queue.PendingCount, Is.EqualTo(1));
            Assert.That(queue.TakeNext().Type, Is.EqualTo(AnnouncementType.ProductionNeedsDirector));
            movie.UnassignActorFromCastRole("lead");
            Assert.That(queue.TakeNext().Type, Is.EqualTo(AnnouncementType.ProductionNeedsActor));
            Assert.That(clock.IsPaused, Is.False);
        }
    }
}
