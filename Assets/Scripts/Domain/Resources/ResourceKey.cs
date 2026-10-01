using System;

namespace SilverScreen.Domain.Resources
{
    /// <summary>Instance identity and occupancy channel; a person's job title is not a resource key.</summary>
    public readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        public string Kind { get; }
        public string EntityId { get; }
        public ResourceKey(string kind, string entityId)
        {
            if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Resource kind and instance ID are required.");
            Kind = kind; EntityId = entityId;
        }
        public bool Equals(ResourceKey other) => Kind == other.Kind && EntityId == other.EntityId;
        public override bool Equals(object obj) => obj is ResourceKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Kind, EntityId);
        public override string ToString() => Kind + ":" + EntityId;
    }
}
