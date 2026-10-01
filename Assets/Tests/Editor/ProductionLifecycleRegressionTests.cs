using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;
using SilverScreen.Presentation.UI;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ProductionLifecycleRegressionTests
    {
        private static MovieProject CreateReadyMovie(int sceneCount)
        {
            var movie = new MovieProject("regression", "Regression", "drama", "Drama", 25000,
                new SimulationDateTime(1930, 1, 1, 8, 0));
            for (int i = 0; i < sceneCount; i++) movie.AddScene(new MovieScene("scene-" + i, i + 1, "stage"));
            var role = new MovieRole("lead", MovieRoleType.Protagonist, "Lead");
            movie.AddRole(role);
            movie.AssignActorToRole(role.Id, new Employee("actor", "Actor", EmployeeRole.Actor, 60, 100, 80));
            role.CompleteCasting();
            movie.AssignDirector(new Employee("director", "Director", EmployeeRole.Director, 60, 100, 80));
            return movie;
        }

        [Test]
        public void EmptyProductionCannotBecomeReadyFromCastingAlone()
        {
            Assert.That(CreateReadyMovie(0).ReadyForFilming, Is.False);
        }

        [Test]
        public void SceneCannotCompleteWithoutKeptFootage()
        {
            var scene = CreateReadyMovie(1).Scenes[0];
            scene.MarkReady();
            scene.BeginFilming();
            Assert.That(scene.CompleteFilming(), Is.False);
        }

        [Test]
        public void CompletedSceneCannotLoseItsKeptTake()
        {
            var scene = CreateReadyMovie(1).Scenes[0];
            CompleteScene(scene);
            Assert.That(scene.DiscardTake(scene.SelectedTake.Id), Is.False);
        }

        [Test]
        public void FourCompletedScenesDetermineProjectCompletionWithoutExtraFlag()
        {
            var movie = CreateReadyMovie(4);
            foreach (var scene in movie.Scenes) CompleteScene(scene);
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Completed));
        }

        [Test]
        public void FilmedMovieIsNoLongerReadyToBeginFilming()
        {
            var movie = CreateReadyMovie(4);
            foreach (var scene in movie.Scenes) CompleteScene(scene);
            Assert.That(movie.ReadyForFilming, Is.False);
        }

        [Test]
        public void QualityCannotFinalizeUnfilmedProduction()
        {
            Assert.That(CreateReadyMovie(1).TrySetProductionResult(new MovieProductionResult(50, 50, 50, 50)), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public void SceneFactsDetermineReadinessAndProgress(int sceneCount)
        {
            var movie = CreateReadyMovie(sceneCount);
            Assert.That(movie.HasScenes, Is.EqualTo(sceneCount > 0));
            Assert.That(movie.ReadyForFilming, Is.EqualTo(sceneCount > 0));
            Assert.That(movie.ProductionProgress, Is.Zero);
            Assert.That(movie.CanRelease, Is.False);
            foreach (var scene in movie.Scenes)
            {
                Assert.That(scene.RequiresFilming, Is.True);
                Assert.That(scene.Takes, Is.Empty);
            }
        }

        [Test]
        public void ZeroSceneScreenplayCannotFinalizeOrGreenlight()
        {
            var screenplay = new ScreenplayProject("empty", "Empty", new[] { "writer" }, new[] { "drama" });
            screenplay.SetParticipation("writer", WriterParticipationStatus.Writing);
            for (int i = 0; i < 480; i++) screenplay.AdvanceWritingMinute(new Dictionary<string, int> { ["writer"] = 100 });
            Assert.That(screenplay.Status, Is.EqualTo(ScreenplayStatus.Completed));
            Assert.That(screenplay.TryFinalizeContent(new ScreenplayContent(
                new[] { new ScreenplayCharacter("lead", "Lead", ScreenplayCharacterRole.Protagonist) },
                Array.Empty<ScreenplayScene>())), Is.False);
            Assert.That(new ScreenplayProductionAdapter().Adapt(screenplay, new SimulationDateTime(1930, 1, 1, 8, 0), "Drama").Succeeded, Is.False);
        }

        [TestCase(1)]
        [TestCase(4)]
        public void AdaptedCopyStartsUnfilmedAndPreservesSourceRemapping(int sceneCount)
        {
            var source = MovieReleaseServiceTests.CreateCompletedScreenplay(sceneCount);
            var movie = new ScreenplayProductionAdapter().Adapt(source, new SimulationDateTime(1930, 1, 1, 8, 0), "Drama").Movie;
            Assert.That(movie.SourceScreenplayId, Is.EqualTo(source.Id));
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Draft));
            Assert.That(movie.ReadyForFilming || movie.CanRelease, Is.False);
            Assert.That(movie.Scenes.Count, Is.EqualTo(sceneCount));
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = movie.Scenes[i];
                Assert.That(scene.SourceScreenplaySceneId, Is.EqualTo(source.Scenes[i].Id));
                Assert.That(scene.Status, Is.EqualTo(MovieSceneStatus.Planned));
                Assert.That(scene.Takes, Is.Empty);
                Assert.That(scene.Beats[0], Is.Not.SameAs(source.Scenes[i].Beats[0]));
                Assert.That(movie.GetCastRole(scene.Beats[0].PerformingCharacterId).SourceScreenplayCharacterId,
                    Is.EqualTo(source.Scenes[i].Beats[0].PerformingCharacterId));
                Assert.That(scene.Shots[0].ScreenplayBeatId, Is.EqualTo(scene.Beats[0].Id));
            }
            source.Scenes[0].AddBeat(new ScreenplayBeat("later", 4, ScreenplayBeatType.Action, "Later creative change"));
            movie.Scenes[0].ReorderBeat(movie.Scenes[0].Beats[0].Id, 3);
            Assert.That(movie.Scenes[0].Beats.Count, Is.EqualTo(3));
            Assert.That(source.Scenes[0].Beats[0].Order, Is.EqualTo(1));
        }

        [Test]
        public void PartialCompletionNotifiesConsistentFactsAndRejectsEarlyRelease()
        {
            var movie = CreateReadyMovie(4);
            movie.OnProjectUpdated += observed => Assert.That(observed.IsProductionComplete, Is.EqualTo(observed.AllScenesCompleted));
            CompleteScene(movie.Scenes[0]);
            Assert.That(movie.CompletedSceneCount, Is.EqualTo(1));
            Assert.That(movie.ProductionProgress, Is.EqualTo(0.25f));
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.ReadyToFilm));
            Assert.That(movie.GetNextFilmableScene(), Is.SameAs(movie.Scenes[1]));
            Assert.That(movie.TryRelease(movie.CreatedDate, CreateRun(movie)), Is.False);
        }

        [Test]
        public void CompletionAndReleaseRequireFootageAndQualityAndAreIdempotent()
        {
            var movie = CreateReadyMovie(1);
            CompleteScene(movie.Scenes[0]);
            var take = movie.Scenes[0].SelectedTake;
            Assert.That(movie.Scenes[0].CompleteFilming(), Is.False);
            Assert.That(movie.Scenes[0].CompleteTake(take.Id, TimeSpan.FromMinutes(9)), Is.False);
            Assert.That(movie.Scenes[0].PrepareTake(), Is.Null);
            Assert.That(movie.CanRelease, Is.False);
            Assert.That(movie.TryRelease(movie.CreatedDate, CreateRun(movie)), Is.False);
            Assert.That(movie.TrySetProductionResult(new MovieProductionResult(50, 50, 50, 50)), Is.True);
            Assert.That(movie.CanRelease, Is.True);
            Assert.That(movie.TryRelease(movie.CreatedDate, CreateRun(movie)), Is.True);
            Assert.That(movie.TryRelease(movie.CreatedDate, CreateRun(movie)), Is.False);
            Assert.That(movie.CanRelease || movie.ReadyForFilming, Is.False);
            Assert.That(movie.CurrentState, Is.EqualTo(MovieProductionState.Released));
            Assert.That(movie.AddScene(new MovieScene("late", 2, "stage")), Is.False);
        }

        [Test]
        public void ReadinessAndUiCannotClaimUnfilmedScenesAreComplete()
        {
            var movie = CreateReadyMovie(4);
            var display = typeof(MovieProjectUI).GetMethod("GetDisplayedProgress", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(display.Invoke(null, new object[] { movie }), Is.EqualTo(0f));
            CompleteScene(movie.Scenes[0]);
            Assert.That(display.Invoke(null, new object[] { movie }), Is.EqualTo(0.25f));
            movie.Scenes[1].MarkReady();
            movie.Scenes[1].BeginFilming();
            Assert.That(movie.IsFilming, Is.True);
            Assert.That(movie.ReadyForFilming, Is.False);
        }

        [Test]
        public void DuplicateTakeIdsAndOverlappingRecordingsAreRejected()
        {
            var scene = CreateReadyMovie(1).Scenes[0];
            scene.MarkReady();
            var take = scene.BeginTake("take-1");
            Assert.That(scene.PrepareTake("take-2"), Is.Null);
            Assert.That(scene.CompleteTake(take.Id, TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(scene.BeginTake("take-1"), Is.Null);
            Assert.That(scene.BeginTake("take-2"), Is.Not.Null);
        }

        private static MovieTheatricalRun CreateRun(MovieProject movie) =>
            new MovieTheatricalRun(movie.Id, movie.CreatedDate, 50, Money.FromDollars(10000));

        internal static void CompleteScene(MovieScene scene)
        {
            Assert.That(scene.MarkReady(), Is.True);
            Assert.That(scene.BeginFilming(), Is.True);
            MovieTake take = scene.BeginTake();
            Assert.That(scene.CompleteTake(take.Id, TimeSpan.FromSeconds(3)), Is.True);
            Assert.That(scene.SelectTake(take.Id), Is.True);
            Assert.That(scene.CompleteFilming(), Is.True);
        }

        internal static void CompleteMovie(MovieProject movie)
        {
            if (movie.Scenes.Count == 0) movie.AddScene(new MovieScene(movie.Id + ":scene", 1, "stage"));
            foreach (var scene in movie.Scenes) CompleteScene(scene);
        }
    }
}
