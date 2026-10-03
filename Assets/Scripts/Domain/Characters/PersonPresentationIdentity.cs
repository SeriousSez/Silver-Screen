using System;

namespace SilverScreen.Domain.Characters
{
    // Serialized values are stable. Zero is deliberately not a canonical family.
    public enum CharacterFamily { Unassigned = 0, AdultFemale = 1, AdultMale = 2, Boy = 3, Girl = 4 }

    /// <summary>Unity-free presentation identity, independent of employment, age and appearance references.
    /// Unknown future family values remain unresolved rather than being reassigned.</summary>
    [Serializable]
    public readonly struct PersonPresentationIdentity : IEquatable<PersonPresentationIdentity>
    {
        public CharacterFamily Family { get; }
        public bool IsCanonical => Family >= CharacterFamily.AdultFemale && Family <= CharacterFamily.Girl;
        public static PersonPresentationIdentity Unassigned => default;
        public PersonPresentationIdentity(CharacterFamily family) { Family = family; }
        public bool Equals(PersonPresentationIdentity other) => Family == other.Family;
        public override bool Equals(object obj) => obj is PersonPresentationIdentity other && Equals(other);
        public override int GetHashCode() => (int)Family;
    }
}
