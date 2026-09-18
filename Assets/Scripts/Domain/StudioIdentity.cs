using System;

namespace SilverScreen.Domain
{
    public sealed class StudioIdentity
    {
        public const string DefaultName = "Silver Screen Studios";
        public const int MaximumNameLength = 32;

        public StudioIdentity(string name = DefaultName)
        {
            if (!TryNormalizeName(name, out string normalized))
            {
                throw new ArgumentException("Studio name must be non-empty and no longer than 32 characters.", nameof(name));
            }

            Name = normalized;
        }

        public string Name { get; private set; }

        public event Action<string> OnNameChanged;

        public bool TryRename(string name)
        {
            if (!TryNormalizeName(name, out string normalized)) return false;
            if (string.Equals(Name, normalized, StringComparison.Ordinal)) return true;

            Name = normalized;
            OnNameChanged?.Invoke(Name);
            return true;
        }

        private static bool TryNormalizeName(string name, out string normalized)
        {
            normalized = name?.Trim();
            return !string.IsNullOrWhiteSpace(normalized) &&
                   normalized.Length <= MaximumNameLength;
        }
    }
}
