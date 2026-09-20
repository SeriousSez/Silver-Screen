using System;

namespace SilverScreen.Domain.Writing
{
    public enum ScreenplayCharacterRole
    {
        Protagonist,
        Antagonist,
        Supporting
    }

    [Serializable]
    public sealed class ScreenplayCharacter
    {
        public string Id { get; }
        public string Name { get; }
        public ScreenplayCharacterRole Role { get; }
        public string ArchetypeId { get; }
        public string Description { get; }

        public ScreenplayCharacter(string id, string name, ScreenplayCharacterRole role,
            string archetypeId = null, string description = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("A screenplay character requires a stable ID.", nameof(id))
                : id.Trim();
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("A screenplay character requires a name.", nameof(name))
                : name.Trim();
            Role = role;
            ArchetypeId = NormalizeOptional(archetypeId);
            Description = NormalizeOptional(description);
        }

        private static string NormalizeOptional(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
