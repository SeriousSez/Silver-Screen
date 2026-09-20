using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public sealed class FilmingFacilityCapabilities
    {
        private readonly List<string> _supportedSetDefinitionIds = new List<string>();

        public string FacilityId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> SupportedSetDefinitionIds => _supportedSetDefinitionIds;

        public FilmingFacilityCapabilities(string facilityId, IEnumerable<string> supportedSetDefinitionIds,
            string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
                throw new ArgumentException("A filming facility requires a stable ID.", nameof(facilityId));
            if (supportedSetDefinitionIds == null)
                throw new ArgumentNullException(nameof(supportedSetDefinitionIds));

            FacilityId = facilityId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? FacilityId : displayName.Trim();
            foreach (string setDefinitionId in supportedSetDefinitionIds)
            {
                if (string.IsNullOrWhiteSpace(setDefinitionId)) continue;
                string normalized = setDefinitionId.Trim().ToLowerInvariant();
                if (!_supportedSetDefinitionIds.Contains(normalized))
                    _supportedSetDefinitionIds.Add(normalized);
            }
        }

        public bool Supports(string setDefinitionId) =>
            !string.IsNullOrWhiteSpace(setDefinitionId) &&
            _supportedSetDefinitionIds.Contains(setDefinitionId.Trim().ToLowerInvariant());
    }

    public interface IStudioFilmingCapabilities
    {
        IReadOnlyList<FilmingFacilityCapabilities> Facilities { get; }
        IReadOnlyList<SetDefinition> AvailableDefinitions { get; }
        bool CanSatisfy(string setDefinitionId);
    }

    public sealed class StudioFilmingCapabilities : IStudioFilmingCapabilities
    {
        public const string StarterStageId = "stage-1";

        private readonly ISetDefinitionCatalog _definitions;
        private readonly List<FilmingFacilityCapabilities> _facilities =
            new List<FilmingFacilityCapabilities>();

        public IReadOnlyList<FilmingFacilityCapabilities> Facilities => _facilities;

        public IReadOnlyList<SetDefinition> AvailableDefinitions
        {
            get
            {
                var available = new List<SetDefinition>();
                foreach (SetDefinition definition in _definitions.KnownDefinitions)
                    if (CanSatisfy(definition.Id)) available.Add(definition);
                return available;
            }
        }

        public StudioFilmingCapabilities(ISetDefinitionCatalog definitions,
            IEnumerable<FilmingFacilityCapabilities> facilities = null)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            if (facilities == null) return;
            foreach (FilmingFacilityCapabilities facility in facilities) AddFacility(facility);
        }

        public bool CanSatisfy(string setDefinitionId)
        {
            if (_definitions.GetDefinition(setDefinitionId) == null) return false;
            foreach (FilmingFacilityCapabilities facility in _facilities)
                if (facility.Supports(setDefinitionId)) return true;
            return false;
        }

        public bool AddFacility(FilmingFacilityCapabilities facility)
        {
            if (facility == null || _facilities.Exists(existing => existing.FacilityId == facility.FacilityId))
                return false;
            _facilities.Add(facility);
            return true;
        }

        public bool RemoveFacility(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId)) return false;
            FilmingFacilityCapabilities facility =
                _facilities.Find(existing => existing.FacilityId == facilityId.Trim());
            return facility != null && _facilities.Remove(facility);
        }

        public static StudioFilmingCapabilities CreateStarterStudio(ISetDefinitionCatalog definitions) =>
            new StudioFilmingCapabilities(definitions, new[]
            {
                new FilmingFacilityCapabilities(StarterStageId, new[]
                {
                    SetDefinitionIds.GenericInterior,
                    SetDefinitionIds.Office,
                    SetDefinitionIds.LivingRoom,
                    SetDefinitionIds.Bedroom
                }, "Sound Stage 1")
            });
    }
}
