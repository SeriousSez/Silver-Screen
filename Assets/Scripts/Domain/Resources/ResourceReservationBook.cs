using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Resources
{
    public sealed class ResourceReservation
    {
        public long Id { get; }
        public string OwnerId { get; }
        public IReadOnlyList<ResourceKey> Resources { get; }
        internal ResourceReservation(long id, string owner, ResourceKey[] resources)
        {
            Id = id; OwnerId = owner; Resources = Array.AsReadOnly(resources);
        }
    }

    public sealed class ResourceConflict
    {
        public ResourceKey Resource { get; }
        public string OwnerId { get; }
        public ResourceConflict(ResourceKey resource, string owner) { Resource = resource; OwnerId = owner; }
    }

    /// <summary>Exclusive current claims, shared for a session. No future booking or profession hierarchy.</summary>
    public sealed class ResourceReservationBook
    {
        private readonly Dictionary<long, ResourceReservation> _claims = new Dictionary<long, ResourceReservation>();
        private readonly Dictionary<ResourceKey, long> _occupancy = new Dictionary<ResourceKey, long>();
        private long _nextId;
        public int Count => _claims.Count;
        public bool IsReserved(ResourceKey key) => _occupancy.ContainsKey(key);

        public bool TryAcquire(string ownerId, IEnumerable<ResourceKey> resources, out ResourceReservation reservation, out IReadOnlyList<ResourceConflict> conflicts)
            => TryChange(null, ownerId, resources, out reservation, out conflicts);

        public bool TryReplace(long reservationId, string ownerId, IEnumerable<ResourceKey> resources, out ResourceReservation reservation, out IReadOnlyList<ResourceConflict> conflicts)
        {
            if (!_claims.ContainsKey(reservationId)) throw new KeyNotFoundException("Reservation does not exist.");
            return TryChange(reservationId, ownerId, resources, out reservation, out conflicts);
        }

        private bool TryChange(long? replacing, string ownerId, IEnumerable<ResourceKey> resources, out ResourceReservation reservation, out IReadOnlyList<ResourceConflict> conflicts)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || resources == null) throw new ArgumentException("Owner and resources are required.");
            var keys = resources.Distinct().OrderBy(k => k.Kind, StringComparer.Ordinal).ThenBy(k => k.EntityId, StringComparer.Ordinal).ToArray();
            if (keys.Length == 0 || keys.Any(k => string.IsNullOrWhiteSpace(k.Kind) || string.IsNullOrWhiteSpace(k.EntityId))) throw new ArgumentException("Valid resources are required.");
            var found = new List<ResourceConflict>();
            foreach (var key in keys)
                if (_occupancy.TryGetValue(key, out long claim) && claim != replacing) found.Add(new ResourceConflict(key, _claims[claim].OwnerId));
            conflicts = found.AsReadOnly();
            reservation = null;
            if (found.Count != 0) return false;
            // Validation precedes all mutation, so a failed replacement retains every original claim.
            if (replacing.HasValue) Release(replacing.Value);
            long id = replacing ?? checked(++_nextId);
            reservation = new ResourceReservation(id, ownerId, keys);
            _claims.Add(id, reservation);
            foreach (var key in keys) _occupancy.Add(key, id);
            return true;
        }

        public bool Release(long reservationId)
        {
            if (!_claims.TryGetValue(reservationId, out var reservation)) return false;
            foreach (var key in reservation.Resources) _occupancy.Remove(key);
            _claims.Remove(reservationId);
            return true;
        }

        public IReadOnlyList<ResourceReservation> GetClaims() => _claims.Values.OrderBy(c => c.Id).ToArray();
    }
}
