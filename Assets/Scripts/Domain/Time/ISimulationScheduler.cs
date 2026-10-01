using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Time
{
    public interface ISimulationScheduler
    {
        long Schedule(ScheduledEventSpec specification);
        bool Cancel(long eventId);
        bool Reschedule(long eventId, SimulationInstant due);
        IReadOnlyList<SimulationEvent> GetPending(string ownerId);
    }

    public enum EventDisclosure { Hidden, KnownToPlayer }

    /// <summary>Versioned data only. Runtime handlers live in a separate registry.</summary>
    public sealed class ScheduledEventSpec
    {
        public string OwnerId { get; }
        public string TypeKey { get; }
        public int PayloadVersion { get; }
        public string Payload { get; }
        public SimulationInstant Due { get; }
        public SimulationDuration? Recurrence { get; }
        public EventDisclosure Disclosure { get; }

        public ScheduledEventSpec(string ownerId, string typeKey, SimulationInstant due, string payload = "", int payloadVersion = 1,
            SimulationDuration? recurrence = null, EventDisclosure disclosure = EventDisclosure.Hidden)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(typeKey)) throw new ArgumentException("Events require owner and type IDs.");
            if (payloadVersion < 1 || recurrence.HasValue && recurrence.Value.Seconds <= 0) throw new ArgumentOutOfRangeException(nameof(payloadVersion));
            OwnerId = ownerId; TypeKey = typeKey; Due = due; Payload = payload ?? "";
            PayloadVersion = payloadVersion; Recurrence = recurrence; Disclosure = disclosure;
        }
    }

    public sealed class SimulationEvent
    {
        public long Id { get; }
        public long Revision { get; }
        public long EnqueueSequence { get; }
        public long Occurrence { get; }
        public ScheduledEventSpec Specification { get; }
        public SimulationInstant Due => Specification.Due;
        internal SimulationEvent(long id, long revision, long sequence, long occurrence, ScheduledEventSpec specification)
        {
            Id = id; Revision = revision; EnqueueSequence = sequence; Occurrence = occurrence; Specification = specification;
        }
    }
}
