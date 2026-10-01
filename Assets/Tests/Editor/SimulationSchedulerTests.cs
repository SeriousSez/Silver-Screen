using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class SimulationSchedulerTests
    {
        private SimulationClock _clock;
        private SimulationScheduler _scheduler;
        private List<string> _seen;

        [SetUp]
        public void Setup()
        {
            _clock = new SimulationClock();
            _scheduler = new SimulationScheduler(_clock);
            _seen = new List<string>();
            _scheduler.RegisterOwner("test");
            _scheduler.RegisterHandler("record", e => _seen.Add(e.Specification.Payload));
        }

        private long At(long seconds, string payload) => _scheduler.Schedule(new ScheduledEventSpec("test", "record", _clock.Now + new SimulationDuration(seconds), payload));
        private void Drain() { while (_clock.HasPendingAdvance) _clock.Advance(0.0); }

        [Test]
        public void StableOrderCancellationAndRescheduling()
        {
            var canceled = At(10, "canceled");
            var first = At(60, "rescheduled");
            At(60, "first");
            _scheduler.Reschedule(first, _clock.Now + new SimulationDuration(60));
            Assert.That(_scheduler.Cancel(canceled), Is.True);
            Assert.That(_scheduler.Cancel(canceled), Is.False);
            _clock.OnMinutePassed += _ => _seen.Add("calendar");
            _clock.Advance(1.0);
            Assert.That(_seen, Is.EqualTo(new[] { "calendar", "first", "rescheduled" }));
            _clock.Advance(0.0);
            Assert.That(_seen.Count, Is.EqualTo(3));
        }

        [Test]
        public void EventsCreatedAtNowFollowQueuedSimultaneousEvents()
        {
            _scheduler.RegisterHandler("spawn", _ => { _seen.Add("spawn"); At(0, "child"); });
            _scheduler.Schedule(new ScheduledEventSpec("test", "spawn", _clock.Now));
            At(0, "existing");
            _clock.Advance(0.0);
            Assert.That(_seen, Is.EqualTo(new[] { "spawn", "existing", "child" }));
        }

        [Test]
        public void BoundedAdvanceRetainsTargetAndNeverRepeatsOccurrences()
        {
            _clock.ProcessingBudget = 3;
            for (int i = 0; i < 20; i++) At(60, i.ToString());
            var target = _clock.Now + SimulationDuration.FromDays(2);
            _clock.AdvanceTo(target);
            Assert.That(_clock.HasPendingAdvance, Is.True);
            Assert.Throws<InvalidOperationException>(() => _scheduler.GetPending("test"));
            Drain();
            Assert.That(_seen.Count, Is.EqualTo(20));
            Assert.That(new HashSet<string>(_seen).Count, Is.EqualTo(20));
            Assert.That(_clock.Now, Is.EqualTo(target));
        }

        [Test]
        public void RecurrenceIsAnchoredToDueAndCanCancelItself()
        {
            var times = new List<long>();
            var start = _clock.Now;
            _scheduler.RegisterHandler("recurring", e => { times.Add((_clock.Now - start).Seconds); if (e.Occurrence == 3) _scheduler.Cancel(e.Id); });
            _scheduler.Schedule(new ScheduledEventSpec("test", "recurring", start + new SimulationDuration(7), recurrence: new SimulationDuration(7)));
            _clock.Advance(60.0);
            Assert.That(times, Is.EqualTo(new long[] { 7, 14, 21, 28 }));
            Assert.That(_scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void PauseFinishesTimestampIncludingNewEventsThenDropsFutureDemand()
        {
            _scheduler.RegisterHandler("pause", _ => { _clock.SetUserPaused(true); At(0, "child"); });
            _scheduler.Schedule(new ScheduledEventSpec("test", "pause", _clock.Now + new SimulationDuration(60)));
            At(60, "same"); At(120, "later");
            _clock.Advance(50.0);
            Assert.That(_seen, Is.EqualTo(new[] { "same", "child" }));
            Assert.That(_clock.CurrentTime.Minute, Is.EqualTo(1));
            _clock.SetUserPaused(false); _clock.Advance(0.0);
            Assert.That(_seen.Count, Is.EqualTo(2));
        }

        [Test]
        public void UnknownHandlersPastEventsAndRecursiveAdvanceAreDiagnosed()
        {
            Assert.Throws<InvalidOperationException>(() => _scheduler.Schedule(new ScheduledEventSpec("unknown", "record", _clock.Now)));
            Assert.Throws<InvalidOperationException>(() => _scheduler.Schedule(new ScheduledEventSpec("test", "missing", _clock.Now)));
            Assert.Throws<ArgumentOutOfRangeException>(() => At(-1, "past"));
            _scheduler.RegisterHandler("recursive", _ => _clock.Advance(1.0));
            _scheduler.Schedule(new ScheduledEventSpec("test", "recursive", _clock.Now));
            _clock.Advance(0.0);
            Assert.That(_clock.AdvanceFailure, Does.Contain("Recursive"));
            Assert.That(_scheduler.FailedEvent, Is.Not.Null);
            Assert.That(_scheduler.PendingCount, Is.Zero);
        }

        [Test]
        public void SameTimestampLoopFaultsWithinBoundAndDoesNotRetry()
        {
            _clock.SameTimestampEventLimit = 10;
            _scheduler.RegisterHandler("loop", _ => _scheduler.Schedule(new ScheduledEventSpec("test", "loop", _clock.Now)));
            _scheduler.Schedule(new ScheduledEventSpec("test", "loop", _clock.Now));
            _clock.Advance(0.0);
            Assert.That(_clock.AdvanceFailure, Does.Contain("chain exceeded"));
            var pending = _scheduler.PendingCount;
            _clock.Advance(100.0);
            Assert.That(_scheduler.PendingCount, Is.EqualTo(pending));
        }
    }
}
