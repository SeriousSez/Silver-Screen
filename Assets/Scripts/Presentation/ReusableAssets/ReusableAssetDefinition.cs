using System;
using SilverScreen.Domain.ReusableAssets;
using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    [CreateAssetMenu(menuName = "SilverScreen/Reusable asset definition")]
    public sealed class ReusableAssetDefinition : ScriptableObject
    {
        [SerializeField] private string _stableId, _displayName, _category, _subcategory;
        [SerializeField] private bool _playerCatalogItem;
        [SerializeField] private int _availableFromYear = 1930;
        [SerializeField] private bool _hasAvailableUntilYear;
        [SerializeField] private int _availableUntilYear;
        [SerializeField] private FixtureSuitability _suitability;
        [SerializeField] private PlacementSurface _placementSurface;
        [SerializeField] private FixtureRole _role = FixtureRole.Both;
        [SerializeField] private FixtureHandedness _handedness;
        [SerializeField] private Bounds _visibleBounds, _functionalClearance;
        [SerializeField] private bool _supportsMaterialVariants = true;
        [SerializeField] private string[] _materialRoles = Array.Empty<string>();
        [SerializeField] private string[] _environmentTags = Array.Empty<string>();
        [SerializeField] private string _optionalInteraction;
        [SerializeField] private string _snapBehavior = "Mount plane and typed anchors; no automatic placement runtime.";
        [SerializeField] private GameObject _prefab;

        public string StableId => _stableId;
        public bool PlayerCatalogItem => _playerCatalogItem;
        public GameObject Prefab => _prefab;
        public PlacementSurface PlacementSurface => _placementSurface;
        public FixtureSuitability Suitability => _suitability;
        public FixtureRole Role => _role;
        public FixtureHandedness Handedness => _handedness;
        public Bounds VisibleBounds => _visibleBounds;
        public Bounds FunctionalClearance => _functionalClearance;
        public bool SupportsMaterialVariants => _supportsMaterialVariants;
        public string OptionalInteraction => _optionalInteraction;
        public string SnapBehavior => _snapBehavior;
        public System.Collections.Generic.IReadOnlyList<string> MaterialRoles => _materialRoles;
        public ReusableCatalogEntry CatalogEntry => new ReusableCatalogEntry(_stableId, _displayName, _category, _subcategory,
            _availableFromYear, _hasAvailableUntilYear ? _availableUntilYear : (int?)null, _environmentTags);
    }
}
