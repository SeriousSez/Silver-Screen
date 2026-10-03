using System;
using System.Collections.Generic;
using SilverScreen.Domain.Characters;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    [Serializable]
    public struct CharacterFamilyCatalogEntry
    {
        [SerializeField] private CharacterFamilyDefinition _definition;
        [SerializeField] private CharacterVisualReference _visual;
        public CharacterFamilyDefinition Definition => _definition;
        public CharacterVisualReference Visual => _visual;
        public CharacterFamilyCatalogEntry(CharacterFamilyDefinition definition, CharacterVisualReference visual = null)
        { _definition = definition; _visual = visual; }
    }
    [CreateAssetMenu(menuName = "SilverScreen/Characters/Local family catalogue")]
    public sealed class CharacterFamilyCatalog : ScriptableObject
    {
        [SerializeField] private CharacterFamilyCatalogEntry[] _entries = Array.Empty<CharacterFamilyCatalogEntry>();
        private Dictionary<CharacterFamily, CharacterFamilyCatalogEntry> _lookup;
        private string _validationError;
        public void Configure(params CharacterFamilyCatalogEntry[] entries) { _entries = (CharacterFamilyCatalogEntry[])entries.Clone(); Invalidate(); }
        private void OnEnable() => Invalidate();
        private void OnValidate() => Invalidate();
        private void Invalidate() { _lookup = null; _validationError = null; }
        public bool TryResolve(CharacterFamily family, out CharacterFamilyCatalogEntry entry, out string reason)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<CharacterFamily, CharacterFamilyCatalogEntry>(); var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in _entries ?? Array.Empty<CharacterFamilyCatalogEntry>())
                {
                    if (item.Definition == null) { _validationError = "Catalogue definition missing."; break; }
                    if (!item.Definition.TryValidate(out _validationError)) break;
                    if (_lookup.ContainsKey(item.Definition.Family) || !ids.Add(item.Definition.DefinitionId))
                    { _validationError = "Duplicate catalogue family or definition ID."; break; }
                    _lookup.Add(item.Definition.Family, item);
                }
            }
            entry = default; reason = _validationError;
            if (reason != null) return false;
            if (!_lookup.TryGetValue(family, out entry)) { reason = "No catalogue entry for " + family; return false; }
            if (entry.Definition == null || entry.Definition.Family != family) { reason = "Catalogue definition changed; rebuild the catalogue explicitly."; return false; }
            if (entry.Visual == null) { reason = "Local visual unavailable for " + family + "; run Character Runtime setup with its accepted reference prerequisites."; return false; }
            return entry.Visual.TryValidate(entry.Definition, out reason);
        }
    }
}
