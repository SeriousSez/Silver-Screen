using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public interface IFilmingLocationCatalog
    {
        IReadOnlyList<FilmingLocation> Locations { get; }
        FilmingLocation GetLocation(string locationId);
        IReadOnlyList<FilmingLocation> FindByEnvironmentCapability(string environmentCapabilityId);
    }

    public sealed class FilmingLocationCatalog : IFilmingLocationCatalog
    {
        private readonly List<FilmingLocation> _locations = new List<FilmingLocation>();
        private readonly Dictionary<string, FilmingLocation> _locationsById =
            new Dictionary<string, FilmingLocation>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<FilmingLocation> Locations => _locations;

        public FilmingLocationCatalog(IEnumerable<FilmingLocation> locations)
        {
            if (locations == null) throw new ArgumentNullException(nameof(locations));
            foreach (FilmingLocation location in locations)
            {
                if (location == null || _locationsById.ContainsKey(location.Id)) continue;
                _locations.Add(location);
                _locationsById.Add(location.Id, location);
            }
        }

        public FilmingLocation GetLocation(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId)) return null;
            _locationsById.TryGetValue(locationId.Trim(), out FilmingLocation location);
            return location;
        }

        public IReadOnlyList<FilmingLocation> FindByEnvironmentCapability(
            string environmentCapabilityId)
        {
            var matches = new List<FilmingLocation>();
            if (string.IsNullOrWhiteSpace(environmentCapabilityId)) return matches;
            foreach (FilmingLocation location in _locations)
                if (location.Supports(environmentCapabilityId)) matches.Add(location);
            return matches;
        }

        public static FilmingLocationCatalog CreatePrototype() =>
            new FilmingLocationCatalog(new[]
            {
                new FilmingLocation(
                    FilmingLocationIds.DowntownMainStreet,
                    "Downtown Main Street",
                    new[] { SetDefinitionIds.Street }),
                new FilmingLocation(
                    FilmingLocationIds.DowntownCornerCafe,
                    "Corner Café",
                    new[] { SetDefinitionIds.RestaurantCafe })
            });
    }
}
