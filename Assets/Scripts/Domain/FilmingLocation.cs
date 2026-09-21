using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public static class FilmingLocationIds
    {
        public const string DowntownMainStreet = "downtown-main-street";
        public const string DowntownCornerCafe = "downtown-corner-cafe";
    }

    public sealed class FilmingLocation
    {
        private readonly List<string> _environmentCapabilityIds = new List<string>();

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> EnvironmentCapabilityIds => _environmentCapabilityIds;

        public FilmingLocation(string id, string displayName,
            IEnumerable<string> environmentCapabilityIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A filming location requires a stable ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A filming location requires a display name.", nameof(displayName));
            if (environmentCapabilityIds == null)
                throw new ArgumentNullException(nameof(environmentCapabilityIds));

            Id = id.Trim().ToLowerInvariant();
            DisplayName = displayName.Trim();
            foreach (string capabilityId in environmentCapabilityIds)
            {
                if (string.IsNullOrWhiteSpace(capabilityId)) continue;
                string normalized = capabilityId.Trim().ToLowerInvariant();
                if (!_environmentCapabilityIds.Contains(normalized))
                    _environmentCapabilityIds.Add(normalized);
            }

            if (_environmentCapabilityIds.Count == 0)
                throw new ArgumentException(
                    "A filming location must provide at least one environment capability.",
                    nameof(environmentCapabilityIds));
        }

        public bool Supports(string environmentCapabilityId) =>
            !string.IsNullOrWhiteSpace(environmentCapabilityId) &&
            _environmentCapabilityIds.Contains(environmentCapabilityId.Trim().ToLowerInvariant());
    }
}
