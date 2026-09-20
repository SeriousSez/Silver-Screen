using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public static class SpecializedSetFacilityIds
    {
        public const string StreetSet = "street-set";
        public const string RestaurantCafeSet = "restaurant-cafe-set";
    }

    public sealed class SpecializedSetFacilityDefinition
    {
        private readonly string[] _supportedSetDefinitionIds;
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> SupportedSetDefinitionIds => _supportedSetDefinitionIds;

        public SpecializedSetFacilityDefinition(string id, string displayName, params string[] supportedSetDefinitionIds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A facility definition requires an ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A facility definition requires a name.", nameof(displayName));
            Id = id.Trim().ToLowerInvariant();
            DisplayName = displayName.Trim();
            _supportedSetDefinitionIds = supportedSetDefinitionIds ?? Array.Empty<string>();
        }

        public static IReadOnlyList<SpecializedSetFacilityDefinition> CreatePrototypeDefinitions() => new[]
        {
            new SpecializedSetFacilityDefinition(SpecializedSetFacilityIds.StreetSet, "Street Set", SetDefinitionIds.Street),
            new SpecializedSetFacilityDefinition(SpecializedSetFacilityIds.RestaurantCafeSet, "Restaurant / Café Set", SetDefinitionIds.RestaurantCafe)
        };
    }

    public sealed class SpecializedSetFacility
    {
        public string Id { get; }
        public SpecializedSetFacilityDefinition Definition { get; }
        public FilmingFacilityCapabilities FilmingCapabilities { get; }

        public SpecializedSetFacility(string id, SpecializedSetFacilityDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A constructed facility requires a stable ID.", nameof(id));
            Id = id.Trim();
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            FilmingCapabilities = new FilmingFacilityCapabilities(
                Id, definition.SupportedSetDefinitionIds, definition.DisplayName);
        }
    }

    public sealed class SpecializedSetConstructionService
    {
        private readonly StudioFilmingCapabilities _studioCapabilities;
        private readonly List<SpecializedSetFacilityDefinition> _definitions;
        private readonly List<SpecializedSetFacility> _facilities = new List<SpecializedSetFacility>();
        public IReadOnlyList<SpecializedSetFacilityDefinition> Definitions => _definitions;
        public IReadOnlyList<SpecializedSetFacility> Facilities => _facilities;
        public event Action<SpecializedSetFacility> FacilityConstructed;
        public event Action<SpecializedSetFacility> FacilityRemoved;

        public SpecializedSetConstructionService(StudioFilmingCapabilities studioCapabilities,
            IEnumerable<SpecializedSetFacilityDefinition> definitions)
        {
            _studioCapabilities = studioCapabilities ?? throw new ArgumentNullException(nameof(studioCapabilities));
            _definitions = definitions != null ? new List<SpecializedSetFacilityDefinition>(definitions) : throw new ArgumentNullException(nameof(definitions));
        }

        public SpecializedSetFacility Construct(string definitionId, string facilityId = null)
        {
            SpecializedSetFacilityDefinition definition = FindDefinition(definitionId);
            if (definition == null) return null;
            string id = string.IsNullOrWhiteSpace(facilityId) ? Guid.NewGuid().ToString("N") : facilityId.Trim();
            if (_facilities.Exists(existing => existing.Id == id)) return null;
            var facility = new SpecializedSetFacility(id, definition);
            if (!_studioCapabilities.AddFacility(facility.FilmingCapabilities)) return null;
            _facilities.Add(facility);
            FacilityConstructed?.Invoke(facility);
            return facility;
        }

        public bool Remove(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId)) return false;
            SpecializedSetFacility facility = _facilities.Find(existing => existing.Id == facilityId.Trim());
            if (facility == null || !_studioCapabilities.RemoveFacility(facility.Id)) return false;
            _facilities.Remove(facility);
            FacilityRemoved?.Invoke(facility);
            return true;
        }

        public bool HasDefinitionConstructed(string definitionId) =>
            _facilities.Exists(facility => facility.Definition.Id == definitionId);

        private SpecializedSetFacilityDefinition FindDefinition(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) return null;
            string normalized = definitionId.Trim().ToLowerInvariant();
            return _definitions.Find(definition => definition.Id == normalized);
        }
    }
}
