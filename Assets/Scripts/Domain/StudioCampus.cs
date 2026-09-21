using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public static class StudioCampusIds
    {
        public const string SilverScreenMainStudio = "silverscreen-main-studio";
    }

    public sealed class StudioCampus
    {
        private readonly List<string> _parcelIds = new List<string>();

        public string Id { get; }
        public string OwningStudioCompanyId { get; }
        public string DisplayName { get; private set; }
        public string OriginalName { get; }
        public IReadOnlyList<string> ParcelIds => _parcelIds;

        public StudioCampus(
            string id,
            string owningStudioCompanyId,
            string displayName,
            string originalName = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A studio campus requires a stable ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(owningStudioCompanyId))
                throw new ArgumentException("A studio campus requires an owning company ID.", nameof(owningStudioCompanyId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A studio campus requires a display name.", nameof(displayName));

            Id = NormalizeId(id);
            OwningStudioCompanyId = NormalizeId(owningStudioCompanyId);
            DisplayName = displayName.Trim();
            OriginalName = string.IsNullOrWhiteSpace(originalName)
                ? DisplayName
                : originalName.Trim();
        }

        public bool Rename(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return false;
            DisplayName = displayName.Trim();
            return true;
        }

        internal bool AddParcel(string parcelId)
        {
            if (string.IsNullOrWhiteSpace(parcelId)) return false;
            string normalized = NormalizeId(parcelId);
            if (_parcelIds.Contains(normalized)) return false;
            _parcelIds.Add(normalized);
            return true;
        }

        private static string NormalizeId(string value) => value.Trim().ToLowerInvariant();
    }
}
