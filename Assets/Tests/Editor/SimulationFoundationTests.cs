using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class SimulationFoundationTests
    {
        [TestCase(1900, 3, 1)]
        [TestCase(2000, 2, 29)]
        public void GregorianRollover(int year, int month, int day)
        {
            var next = SimulationInstant.FromCalendar(year, 2, 28, 23, 59, 59) + new SimulationDuration(1);
            Assert.That(next.ToCalendar(), Is.EqualTo(new SimulationDateTime(year, month, day, 0, 0)));
        }

        [Test]
        public void TimestampRejectsInvalidDatesAndOverflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromCalendar(1900, 2, 29));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationInstant(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var unused = new SimulationInstant(SimulationInstant.MaxSeconds) + new SimulationDuration(1); });
            Assert.Throws<OverflowException>(() => SimulationDuration.FromDays(long.MaxValue));
            var instant = SimulationInstant.FromCalendar(1930, 1, 1, 8, 0, 37);
            Assert.That(instant.Seconds - instant.ToCalendar().ToInstant().Seconds, Is.EqualTo(37));
        }

        [Test]
        public void LegacyBoundaryOrderAndCurrentPacingArePreserved()
        {
            var clock = new SimulationClock(1930, 12, 31, 23, 59);
            var seen = new List<string>();
            clock.OnMinutePassed += _ => seen.Add("minute");
            clock.OnHourPassed += _ => seen.Add("hour");
            clock.OnDayPassed += _ => seen.Add("day");
            clock.OnMonthPassed += _ => seen.Add("month");
            clock.OnYearPassed += _ => seen.Add("year");
            clock.Advance(1.0);
            Assert.That(seen, Is.EqualTo(new[] { "minute", "hour", "day", "month", "year" }));
            Assert.That(clock.CurrentTime, Is.EqualTo(new SimulationDateTime(1931, 1, 1, 0, 0)));
            Assert.That(clock.Pacing.RealSecondsPerStrategicDay, Is.EqualTo(1440));
        }

        [Test]
        public void PartitionedElapsedTimeKeepsFractionAndMatchesLargeAdvance()
        {
            var partitioned = new SimulationClock();
            var whole = new SimulationClock();
            for (int i = 0; i < 1024; i++) partitioned.Advance(1.0 / 1024);
            whole.Advance(1.0);
            Assert.That(partitioned.Now, Is.EqualTo(whole.Now));
            partitioned.Advance(0.125);
            Assert.That((partitioned.Now - whole.Now).Seconds, Is.EqualTo(7));
            partitioned.Advance(0.125);
            Assert.That((partitioned.Now - whole.Now).Seconds, Is.EqualTo(15));
        }

        [Test]
        public void PauseHoldsDoNotOverwriteUserIntent()
        {
            var clock = new SimulationClock();
            var hold = clock.AcquirePause("live-view", "observation");
            var other = clock.AcquirePause("decision", "attention");
            clock.SetSpeed(SimulationSpeed.VeryFast);
            clock.SetUserPaused(true);
            hold.Dispose(); hold.Dispose(); other.Dispose();
            Assert.That(clock.IsPaused, Is.True);
            clock.TogglePause();
            Assert.That(clock.CurrentSpeed, Is.EqualTo(SimulationSpeed.VeryFast));
            clock.Advance(1.0);
            Assert.That(clock.CurrentTime.Minute, Is.EqualTo(3));
        }

        [Test]
        public void PauseAtBoundaryDiscardsUnconsumedElapsedTime()
        {
            var clock = new SimulationClock();
            int minutes = 0;
            clock.OnMinutePassed += _ => { minutes++; clock.SetUserPaused(true); };
            clock.Advance(200.0);
            Assert.That(minutes, Is.EqualTo(1));
            clock.SetUserPaused(false);
            clock.Advance(0.0);
            Assert.That(minutes, Is.EqualTo(1));
        }

        [Test]
        public void FastPacingIsOnlyExplicitInMemoryConfiguration()
        {
            var clock = new SimulationClock();
            var start = clock.Now;
            clock.ConfigurePacing(new SimulationPacing("test-only-ten-seconds", 10, 2, 4));
            clock.Advance(10.0);
            Assert.That((clock.Now - start).Seconds, Is.EqualTo(86400));
            Assert.That(new SimulationClock().RealSecondsPerSimulatedMinute, Is.EqualTo(1));
        }
    }
}
