using System;
using System.Collections.Generic;
namespace SilverScreen.Domain
{
    /// <summary>Session population identity owner. Employment and spawned views are separate lifetimes.</summary>
    public sealed class PersonPopulation
    {
        private readonly Dictionary<string,PersonProfile> _people = new Dictionary<string,PersonProfile>();
        public IReadOnlyDictionary<string,PersonProfile> People => _people;
        public void Register(PersonProfile person)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (_people.TryGetValue(person.Id,out var known) && !ReferenceEquals(known,person))
                throw new InvalidOperationException("Duplicate person identity: " + person.Id);
            _people[person.Id] = person;
        }
    }
}
