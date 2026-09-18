using System;
using System.Collections.Generic;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Movie
{
    public enum TheatricalRunState
    {
        Running,
        Completed
    }

    public readonly struct WeeklyTheatricalPayout
    {
        public WeeklyTheatricalPayout(int weekNumber, Money boxOfficeGross, Money studioRevenue)
        {
            WeekNumber = weekNumber;
            BoxOfficeGross = boxOfficeGross;
            StudioRevenue = studioRevenue;
        }

        public int WeekNumber { get; }
        public Money BoxOfficeGross { get; }
        public Money StudioRevenue { get; }
        public bool HasRevenue => StudioRevenue > Money.Zero;
    }

    [Serializable]
    public sealed class MovieTheatricalRun
    {
        private SimulationDateTime? _lastProcessedDate;

        public MovieTheatricalRun(
            string movieId,
            SimulationDateTime releaseDate,
            int audienceReception,
            Money targetGross)
        {
            MovieId = movieId ?? string.Empty;
            ReleaseDate = releaseDate;
            AudienceReception = Math.Clamp(audienceReception, 0, 100);
            TargetGross = targetGross;
            WeeklyGrosses = TheatricalMarketConfiguration.CalculateWeeklyGrosses(targetGross);
            CurrentDay = 0;
            WeeksPaid = 0;
            TotalBoxOfficeGross = Money.Zero;
            TotalStudioRevenue = Money.Zero;
            State = TheatricalRunState.Running;
        }

        public string MovieId { get; }
        public SimulationDateTime ReleaseDate { get; }
        public int AudienceReception { get; }
        public Money TargetGross { get; }
        public IReadOnlyList<Money> WeeklyGrosses { get; }
        public int CurrentDay { get; private set; }
        public int WeeksPaid { get; private set; }
        public int CurrentWeek => State == TheatricalRunState.Completed
            ? 4
            : Math.Min(CurrentDay / TheatricalMarketConfiguration.DaysPerWeek + 1, 4);
        public Money CurrentWeekGross => WeeklyGrosses[Math.Min(CurrentWeek - 1, 3)];
        public Money TotalBoxOfficeGross { get; private set; }
        public Money TotalStudioRevenue { get; private set; }
        public TheatricalRunState State { get; private set; }
        public bool IsCompleted => State == TheatricalRunState.Completed;

        public WeeklyTheatricalPayout AdvanceDay(SimulationDateTime date)
        {
            if (IsCompleted) return default;
            if (_lastProcessedDate.HasValue && date.CompareTo(_lastProcessedDate.Value) <= 0) return default;

            _lastProcessedDate = date;
            CurrentDay++;
            if (CurrentDay % TheatricalMarketConfiguration.DaysPerWeek != 0)
            {
                return default;
            }

            int weekIndex = WeeksPaid;
            Money gross = WeeklyGrosses[weekIndex];
            Money studioRevenue = gross;
            TotalBoxOfficeGross += gross;
            TotalStudioRevenue += studioRevenue;
            WeeksPaid++;

            if (WeeksPaid >= 4)
            {
                State = TheatricalRunState.Completed;
            }

            return new WeeklyTheatricalPayout(WeeksPaid, gross, studioRevenue);
        }
    }
}
