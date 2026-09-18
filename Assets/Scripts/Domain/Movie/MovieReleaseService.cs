using System;
using System.Collections.Generic;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Movie
{
    public sealed class MovieReleaseService : IMovieReleaseService
    {
        private readonly ISimulationTimeService _timeService;
        private readonly IStudioFinanceService _finances;
        private readonly Dictionary<string, MovieProject> _releasedMovies =
            new Dictionary<string, MovieProject>();

        public MovieReleaseService(ISimulationTimeService timeService, IStudioFinanceService finances)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _finances = finances ?? throw new ArgumentNullException(nameof(finances));
            _timeService.OnDayPassed += HandleDayPassed;
        }

        public event Action<MovieProject> OnMovieReleaseUpdated;

        public MovieReleaseResult ReleaseMovie(MovieProject movie)
        {
            if (movie == null)
            {
                return MovieReleaseResult.Failed(MovieReleaseFailure.MovieMissing, "Movie is required.");
            }

            if (movie.TheatricalRun != null || movie.CurrentState == MovieProductionState.Released)
            {
                return MovieReleaseResult.Failed(MovieReleaseFailure.AlreadyReleased, "This movie has already been released.");
            }

            if (movie.CurrentState != MovieProductionState.Completed)
            {
                return MovieReleaseResult.Failed(MovieReleaseFailure.ProductionNotCompleted, "Only completed movies can be released.");
            }

            if (movie.ProductionResult == null)
            {
                return MovieReleaseResult.Failed(MovieReleaseFailure.ProductionResultMissing, "Production quality must be finalized before release.");
            }

            int audienceReception = movie.ProductionResult.OverallQuality;
            Money targetGross = TheatricalMarketConfiguration.CalculateTargetGross(
                movie.BudgetTierId,
                audienceReception);
            var run = new MovieTheatricalRun(
                movie.Id,
                _timeService.CurrentTime,
                audienceReception,
                targetGross);

            if (!movie.TryRelease(_timeService.CurrentTime, run))
            {
                return MovieReleaseResult.Failed(MovieReleaseFailure.AlreadyReleased, "This movie cannot be released again.");
            }

            _releasedMovies[movie.Id] = movie;
            OnMovieReleaseUpdated?.Invoke(movie);
            return MovieReleaseResult.Success(run);
        }

        public MovieTheatricalRun GetTheatricalRun(string movieId)
        {
            return movieId != null &&
                   _releasedMovies.TryGetValue(movieId, out var movie)
                ? movie.TheatricalRun
                : null;
        }

        public void Dispose()
        {
            _timeService.OnDayPassed -= HandleDayPassed;
        }

        private void HandleDayPassed(SimulationDateTime date)
        {
            foreach (var movie in _releasedMovies.Values)
            {
                var run = movie.TheatricalRun;
                if (run == null || run.IsCompleted) continue;

                WeeklyTheatricalPayout payout = run.AdvanceDay(date);
                if (payout.HasRevenue)
                {
                    _finances.RecordIncome(
                        payout.StudioRevenue,
                        FinancialTransactionCategory.BoxOfficeRevenue,
                        date,
                        $"Box Office — {movie.Title} — Week {payout.WeekNumber}",
                        movie.Id);
                }

                if (run.IsCompleted && movie.CommercialResult == null)
                {
                    movie.TrySetCommercialResult(new MovieCommercialResult(
                        run.AudienceReception,
                        run.TotalBoxOfficeGross,
                        run.TotalStudioRevenue,
                        Money.FromDollars(movie.Budget)));
                }

                OnMovieReleaseUpdated?.Invoke(movie);
            }
        }
    }
}
