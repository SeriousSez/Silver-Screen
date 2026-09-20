using System;

namespace SilverScreen.Domain
{
    public static class SetDefinitionIds
    {
        public const string GenericInterior = "generic-interior";
        public const string Office = "office";
        public const string LivingRoom = "living-room";
        public const string Bedroom = "bedroom";
        public const string RestaurantCafe = "restaurant-cafe";
        public const string Street = "street";
    }

    [Serializable]
    public sealed class SetDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public SetDefinition(string id, string displayName, string description = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A set definition requires a stable ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A set definition requires a display name.", nameof(displayName));

            Id = id.Trim().ToLowerInvariant();
            DisplayName = displayName.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        }
    }
}
