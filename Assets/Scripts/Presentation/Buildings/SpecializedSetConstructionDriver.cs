using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Presentation.Writing;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public sealed class SpecializedSetFacilityView : MonoBehaviour
    {
        public string FacilityId { get; private set; }
        public string DefinitionId { get; private set; }

        public void Initialize(SpecializedSetFacility facility)
        {
            FacilityId = facility.Id;
            DefinitionId = facility.Definition.Id;
        }
    }

    public sealed class SpecializedSetConstructionDriver : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _views = new Dictionary<string, GameObject>();
        private SpecializedSetConstructionService _service;

        public SpecializedSetConstructionService Service => _service;

        public void Initialize(ScreenplayWritingDriver writingDriver)
        {
            if (_service != null || writingDriver?.FacilityConstruction == null) return;
            _service = writingDriver.FacilityConstruction;
            _service.FacilityConstructed += HandleConstructed;
            _service.FacilityRemoved += HandleRemoved;
            foreach (SpecializedSetFacility facility in _service.Facilities) HandleConstructed(facility);
        }

        private void OnDestroy()
        {
            if (_service == null) return;
            _service.FacilityConstructed -= HandleConstructed;
            _service.FacilityRemoved -= HandleRemoved;
        }

        public bool TryConstruct(string definitionId) => _service?.Construct(definitionId) != null;
        public bool TryRemove(string facilityId) => _service != null && _service.Remove(facilityId);

        private void HandleConstructed(SpecializedSetFacility facility)
        {
            if (facility == null || _views.ContainsKey(facility.Id)) return;
            GameObject root = facility.Definition.Id == SpecializedSetFacilityIds.StreetSet
                ? CreateStreetSet(facility)
                : CreateRestaurantCafeSet(facility);
            _views.Add(facility.Id, root);
        }

        private void HandleRemoved(SpecializedSetFacility facility)
        {
            if (facility == null || !_views.TryGetValue(facility.Id, out GameObject view)) return;
            _views.Remove(facility.Id);
            if (view != null) Destroy(view);
        }

        private static GameObject CreateStreetSet(SpecializedSetFacility facility)
        {
            var root = CreateRoot(facility, BuildingType.StreetSet, new Vector3(27f, 0f, -12f));
            AddBlock(root.transform, "StreetSurface", new Vector3(0f, .1f, 0f), new Vector3(10f, .2f, 7f), new Color(.27f, .28f, .30f));
            AddBlock(root.transform, "FacadeA", new Vector3(-3.3f, 2f, 2.5f), new Vector3(3f, 4f, 1f), new Color(.63f, .42f, .30f));
            AddBlock(root.transform, "FacadeB", new Vector3(0f, 2.4f, 2.5f), new Vector3(3f, 4.8f, 1f), new Color(.72f, .62f, .47f));
            AddBlock(root.transform, "FacadeC", new Vector3(3.3f, 1.8f, 2.5f), new Vector3(3f, 3.6f, 1f), new Color(.48f, .52f, .55f));
            return root;
        }

        private static GameObject CreateRestaurantCafeSet(SpecializedSetFacility facility)
        {
            var root = CreateRoot(facility, BuildingType.RestaurantCafeSet, new Vector3(27f, 0f, 10f));
            AddBlock(root.transform, "CafeBody", new Vector3(0f, 2f, 0f), new Vector3(9f, 4f, 7f), new Color(.76f, .62f, .45f));
            AddBlock(root.transform, "CafeFront", new Vector3(0f, 2.1f, -3.55f), new Vector3(7f, 2.8f, .25f), new Color(.13f, .20f, .21f));
            AddBlock(root.transform, "Awning", new Vector3(0f, 3.5f, -4f), new Vector3(8f, .3f, 1.2f), new Color(.62f, .19f, .15f));
            AddBlock(root.transform, "Roof", new Vector3(0f, 4.2f, 0f), new Vector3(9.5f, .4f, 7.5f), new Color(.18f, .17f, .18f));
            return root;
        }

        private static GameObject CreateRoot(SpecializedSetFacility facility, BuildingType type, Vector3 position)
        {
            var root = new GameObject(facility.Definition.DisplayName);
            root.transform.position = position;
            var entrance = new GameObject("Entrance").transform;
            entrance.SetParent(root.transform, false);
            entrance.localPosition = new Vector3(0f, 0f, -4.25f);
            root.AddComponent<StudioBuildingView>().Initialize(type, facility.Definition.DisplayName, entrance);
            root.AddComponent<SpecializedSetFacilityView>().Initialize(facility);
            return root;
        }

        private static void AddBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            block.GetComponent<Renderer>().material.color = color;
        }
    }
}
