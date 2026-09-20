using System;

namespace SilverScreen.Domain.Movie
{
    public enum ProductionEnvironmentFulfillmentSource
    {
        OwnedStudioFacility
    }

    public sealed class ProductionEnvironmentResolution
    {
        public string RequiredSetDefinitionId { get; }
        public string FacilityId { get; }
        public string FacilityDisplayName { get; }
        public ProductionEnvironmentFulfillmentSource Source { get; }

        public ProductionEnvironmentResolution(string requiredSetDefinitionId, string facilityId,
            string facilityDisplayName, ProductionEnvironmentFulfillmentSource source)
        {
            if (string.IsNullOrWhiteSpace(requiredSetDefinitionId))
                throw new ArgumentException("A production environment requirement is required.", nameof(requiredSetDefinitionId));
            if (string.IsNullOrWhiteSpace(facilityId))
                throw new ArgumentException("A resolved facility requires a stable ID.", nameof(facilityId));
            RequiredSetDefinitionId = requiredSetDefinitionId.Trim().ToLowerInvariant();
            FacilityId = facilityId.Trim();
            FacilityDisplayName = string.IsNullOrWhiteSpace(facilityDisplayName)
                ? FacilityId
                : facilityDisplayName.Trim();
            Source = source;
        }
    }

    public interface IProductionEnvironmentResolver
    {
        ProductionEnvironmentResolution ResolveOwnedFacility(string requiredSetDefinitionId);
    }

    public sealed class OwnedStudioProductionEnvironmentResolver : IProductionEnvironmentResolver
    {
        private readonly IStudioFilmingCapabilities _capabilities;

        public OwnedStudioProductionEnvironmentResolver(IStudioFilmingCapabilities capabilities)
        {
            _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        }

        public ProductionEnvironmentResolution ResolveOwnedFacility(string requiredSetDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(requiredSetDefinitionId)) return null;
            string normalized = requiredSetDefinitionId.Trim().ToLowerInvariant();
            foreach (FilmingFacilityCapabilities facility in _capabilities.Facilities)
            {
                if (!facility.Supports(normalized)) continue;
                return new ProductionEnvironmentResolution(
                    normalized,
                    facility.FacilityId,
                    facility.DisplayName,
                    ProductionEnvironmentFulfillmentSource.OwnedStudioFacility);
            }
            return null;
        }
    }
}
