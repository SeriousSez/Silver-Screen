using System;

namespace SilverScreen.Domain
{
    public static class PropertyOwnerIds
    {
        public const string CityPublic = "city-public";
        public const string PrivateResidentialOwner = "private-residential-owner";
        public const string PrivateCommercialOwner = "private-commercial-owner";
        public const string PrivateIndustrialOwner = "private-industrial-owner";
    }

    public static class PropertyParcelIds
    {
        public const string SilverScreenMainStudio = "silverscreen-main-studio-property";
        public const string CarltonApartments = "carlton-apartments-property";
        public const string DowntownWarehouse = "downtown-warehouse-property";
        public const string HawthorneHouse = "hawthorne-house-property";
        public const string PacificTextileWorks = "pacific-textile-works-property";
    }

    public sealed class PropertyParcel
    {
        public string Id { get; }
        public string DisplayName { get; private set; }
        public string OriginalName { get; }
        public string OwnerId { get; private set; }
        public string StudioCampusId { get; private set; }
        public bool IsPotentiallyPurchasable { get; }

        public PropertyParcel(
            string id,
            string displayName,
            string ownerId,
            string studioCampusId = null,
            bool isPotentiallyPurchasable = false,
            string originalName = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A property parcel requires a stable ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A property parcel requires a display name.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("A property parcel requires a stable owner ID.", nameof(ownerId));

            Id = NormalizeId(id);
            DisplayName = displayName.Trim();
            OriginalName = string.IsNullOrWhiteSpace(originalName)
                ? DisplayName
                : originalName.Trim();
            OwnerId = NormalizeId(ownerId);
            StudioCampusId = NormalizeOptionalId(studioCampusId);
            IsPotentiallyPurchasable = isPotentiallyPurchasable;
        }

        public bool Rename(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return false;
            DisplayName = displayName.Trim();
            return true;
        }

        internal void SetOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("A property parcel requires a stable owner ID.", nameof(ownerId));
            OwnerId = NormalizeId(ownerId);
        }

        internal void SetStudioCampus(string studioCampusId) =>
            StudioCampusId = NormalizeOptionalId(studioCampusId);

        private static string NormalizeId(string value) => value.Trim().ToLowerInvariant();

        private static string NormalizeOptionalId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : NormalizeId(value);
    }
}
