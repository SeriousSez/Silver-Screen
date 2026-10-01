using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public sealed class TakePerformanceService
    {
        private readonly BeatPerformanceGenerator _generator;
        public TakePerformanceService(IPerformanceRandomSource random) { _generator = new BeatPerformanceGenerator(random); }

        public void Prepare(MovieProject movie, MovieScene scene, MovieTake take)
        {
            if (movie == null || scene == null || take == null || take.Status != MovieTakeStatus.Recording) throw new ArgumentException("A recording take and its production are required.");
            if (take.PerformanceResults.Count != 0)
            {
                if (take.PerformanceResults.Count != scene.Beats.Count) throw new InvalidOperationException("Incomplete performance snapshot.");
                return;
            }
            // Resolve every performer before drawing randomness or changing the take.
            var actors = new List<Employee>();
            foreach (var beat in scene.Beats)
            {
                var actor = movie.GetCastRole(beat.PerformingCharacterId)?.AssignedActor;
                if (actor == null) throw new InvalidOperationException("No assigned performer for beat " + beat.Id);
                actors.Add(actor);
            }
            var results = new List<BeatPerformanceResult>();
            for (int i = 0; i < scene.Beats.Count; i++) results.Add(_generator.Generate(scene.Beats[i], actors[i], movie.AssignedDirector, movie.GenreId));
            foreach (var result in results)
                if (!take.RecordPerformanceResult(result)) throw new InvalidOperationException("Performance results must be recorded once.");
        }
    }
}
