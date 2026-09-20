using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public interface ISetDefinitionCatalog
    {
        IReadOnlyList<SetDefinition> KnownDefinitions { get; }
        SetDefinition GetDefinition(string setDefinitionId);
    }

    public sealed class SetDefinitionCatalog : ISetDefinitionCatalog
    {
        private readonly List<SetDefinition> _knownDefinitions = new List<SetDefinition>();
        private readonly Dictionary<string, SetDefinition> _definitionsById =
            new Dictionary<string, SetDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<SetDefinition> KnownDefinitions => _knownDefinitions;

        public SetDefinitionCatalog(IEnumerable<SetDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            foreach (SetDefinition definition in definitions) Add(definition);
        }

        public SetDefinition GetDefinition(string setDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(setDefinitionId)) return null;
            _definitionsById.TryGetValue(setDefinitionId.Trim(), out SetDefinition definition);
            return definition;
        }

        public bool Add(SetDefinition definition)
        {
            if (definition == null || _definitionsById.ContainsKey(definition.Id)) return false;
            _definitionsById.Add(definition.Id, definition);
            _knownDefinitions.Add(definition);
            return true;
        }

        public static SetDefinitionCatalog CreatePrototype() => new SetDefinitionCatalog(new[]
        {
            new SetDefinition(SetDefinitionIds.GenericInterior, "Generic Interior",
                "A flexible interior environment for scenes without a specialized set."),
            new SetDefinition(SetDefinitionIds.Office, "Office"),
            new SetDefinition(SetDefinitionIds.LivingRoom, "Living Room"),
            new SetDefinition(SetDefinitionIds.Bedroom, "Bedroom"),
            new SetDefinition(SetDefinitionIds.RestaurantCafe, "Restaurant / Cafe"),
            new SetDefinition(SetDefinitionIds.Street, "Street")
        });
    }
}
