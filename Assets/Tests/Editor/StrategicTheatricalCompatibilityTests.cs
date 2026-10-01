using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Finance;

namespace SilverScreen.Tests.EditMode
{
    public sealed class StrategicTheatricalCompatibilityTests
    {
        [Test]
        public void RealClockLargeAdvancePreservesConcurrentDailyRunsAndIdempotentWeeklyPayouts()
        {
            var clock = new SimulationClock(1930, 12, 20, 8, 0) { ProcessingBudget = 3 };
            var finances = new StudioFinances(clock.CurrentTime);
            using var releases = new MovieReleaseService(clock, finances);
            MovieProject Create(string id)
            {
                var movie = new MovieProject(id, id, "drama", "Drama", 50000, clock.CurrentTime);
                ProductionLifecycleRegressionTests.CompleteMovie(movie);
                movie.TrySetProductionResult(new MovieProductionResult(70, 70, 70, 70));
                return movie;
            }
            var a = Create("a"); var b = Create("b");
            Assert.That(releases.ReleaseMovie(a).Succeeded, Is.True);
            Assert.That(releases.ReleaseMovie(b).Succeeded, Is.True);
            var start = clock.Now;
            clock.SetUserPaused(true); clock.AdvanceTo(start + SimulationDuration.FromDays(28));
            Assert.That(a.TheatricalRun.CurrentDay, Is.Zero);
            clock.SetUserPaused(false);
            clock.AdvanceTo(start + SimulationDuration.FromDays(28));
            while (clock.HasPendingAdvance) clock.Advance(0.0);
            Assert.That(clock.AdvanceFailure, Is.Null);
            Assert.That(clock.CurrentTime.Year, Is.EqualTo(1931));
            Assert.That(a.TheatricalRun.CurrentDay, Is.EqualTo(28));
            Assert.That(b.TheatricalRun.WeeksPaid, Is.EqualTo(4));
            Assert.That(a.TheatricalRun.IsCompleted && b.TheatricalRun.IsCompleted, Is.True);
            Assert.That(finances.Transactions.Count(t => t.Category == FinancialTransactionCategory.BoxOfficeRevenue), Is.EqualTo(8));
            var paid = finances.CurrentCash;
            clock.Advance(0.0); clock.AdvanceTo(clock.Now + SimulationDuration.FromDays(28));
            while (clock.HasPendingAdvance) clock.Advance(0.0);
            Assert.That(finances.CurrentCash, Is.EqualTo(paid));
            Assert.That(releases.ReleaseMovie(a).Succeeded, Is.False);
        }
    }
}
