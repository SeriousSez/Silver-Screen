using System;

namespace SilverScreen.Domain
{
    public static class StudioCompanyIds
    {
        public const string SilverScreenStudios = "silverscreen-studios";
        public const string MajesticPictures = "majestic-pictures";
        public const string ColumbiaHeightsStudios = "columbia-heights-studios";
    }

    public sealed class StudioCompany
    {
        public string Id { get; }
        public string DisplayName { get; }

        public StudioCompany(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A studio company requires a stable ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A studio company requires a display name.", nameof(displayName));

            Id = id.Trim().ToLowerInvariant();
            DisplayName = displayName.Trim();
        }
    }
}
