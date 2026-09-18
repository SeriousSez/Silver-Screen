using System;

namespace SilverScreen.Domain.Movie
{
    public enum MovieReleaseFailure
    {
        None,
        MovieMissing,
        ProductionNotCompleted,
        AlreadyReleased,
        ProductionResultMissing
    }

    public sealed class MovieReleaseResult
    {
        private MovieReleaseResult(MovieTheatricalRun run, MovieReleaseFailure failure, string message)
        {
            Run = run;
            Failure = failure;
            Message = message ?? string.Empty;
        }

        public bool Succeeded => Run != null && Failure == MovieReleaseFailure.None;
        public MovieTheatricalRun Run { get; }
        public MovieReleaseFailure Failure { get; }
        public string Message { get; }

        public static MovieReleaseResult Success(MovieTheatricalRun run)
        {
            return new MovieReleaseResult(run, MovieReleaseFailure.None, string.Empty);
        }

        public static MovieReleaseResult Failed(MovieReleaseFailure failure, string message)
        {
            return new MovieReleaseResult(null, failure, message);
        }
    }

    public interface IMovieReleaseService : IDisposable
    {
        event Action<MovieProject> OnMovieReleaseUpdated;
        MovieReleaseResult ReleaseMovie(MovieProject movie);
        MovieTheatricalRun GetTheatricalRun(string movieId);
    }
}
