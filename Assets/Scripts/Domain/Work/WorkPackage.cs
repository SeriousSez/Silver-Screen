using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Work
{
    public enum WorkStatus { Pending, Blocked, Running, Suspended, Completed, Canceled }

    public sealed class WorkAssignment
    {
        public ResourceKey Resource { get; }
        public string Purpose { get; }
        public WorkAssignment(ResourceKey resource, string purpose)
        {
            if (string.IsNullOrWhiteSpace(resource.EntityId) || string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException("Resource and assignment purpose required.");
            Resource = resource; Purpose = purpose;
        }
    }

    public sealed class WorkDefinition
    {
        public string Id { get; }
        public string OwnerId { get; }
        public string KindId { get; }
        public WorkQuantity Required { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public IReadOnlyList<string> Conditions { get; }
        public WorkDefinition(string id, string ownerId, string kindId, WorkQuantity required, IEnumerable<string> dependencies = null, IEnumerable<string> conditions = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(kindId)) throw new ArgumentException("Stable work, owner and kind IDs required.");
            if (required.MicroUnits <= 0 || string.IsNullOrWhiteSpace(required.MeasureId)) throw new ArgumentException("Positive measured work required.");
            Id = id; OwnerId = ownerId; KindId = kindId; Required = required;
            Dependencies = Array.AsReadOnly((dependencies ?? Array.Empty<string>()).Distinct().ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<string>()).Distinct().ToArray());
            if (Dependencies.Concat(Conditions).Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Dependency IDs cannot be blank.");
        }
    }

    public sealed class WorkSnapshot
    {
        public WorkDefinition Definition { get; }
        public WorkQuantity Completed { get; }
        public decimal AccountingRemainder { get; }
        public WorkStatus Status { get; }
        public SimulationInstant LastEvaluated { get; }
        public long Revision { get; }
        public long CapacityVersion { get; }
        public WorkRate Capacity { get; }
        public IReadOnlyList<WorkAssignment> Assignments { get; }
        internal WorkSnapshot(WorkDefinition definition, decimal earned, WorkStatus status, SimulationInstant evaluated, long revision, long capacityVersion, WorkRate capacity, WorkAssignment[] assignments)
        {
            Definition = definition; Completed = new WorkQuantity(definition.Required.MeasureId, (long)earned);
            AccountingRemainder = earned - (long)earned; Status = status; LastEvaluated = evaluated;
            Revision = revision; CapacityVersion = capacityVersion; Capacity = capacity; Assignments = Array.AsReadOnly(assignments);
        }
    }

    public interface IWorkCapacityPolicy
    {
        WorkRate Evaluate(WorkDefinition work, IReadOnlyList<WorkAssignment> assignments, WorkCapacityContext context);
    }

    public sealed class WorkCapacityContext
    {
        public string ProductionId { get; }
        public SimulationInstant At { get; }
        public WorkCapacityContext(string productionId, SimulationInstant at) { ProductionId = productionId; At = at; }
    }
}
