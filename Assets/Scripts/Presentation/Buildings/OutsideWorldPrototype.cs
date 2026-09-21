using SilverScreen.Domain;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class OutsideWorldPrototype : MonoBehaviour
    {
        private static readonly Color Road = new Color(.18f, .19f, .21f);
        private static readonly Color Sidewalk = new Color(.58f, .57f, .54f);
        private static readonly Color Brick = new Color(.55f, .28f, .20f);
        private static readonly Color Stucco = new Color(.72f, .61f, .45f);
        private static readonly Color Glass = new Color(.10f, .20f, .23f);
        private static readonly Color StudioLand = new Color(.34f, .36f, .31f);

        public IFilmingLocationCatalog LocationCatalog { get; private set; }
        public FilmingLocationViewRegistry LocationViews { get; } =
            new FilmingLocationViewRegistry();

        private void Awake()
        {
            if (transform.Find("OutsideWorld") != null) return;
            LocationCatalog = FilmingLocationCatalog.CreatePrototype();
            BuildWorld();
        }

        private void BuildWorld()
        {
            var world = new GameObject("OutsideWorld").transform;
            world.SetParent(transform, false);

            AddBlock(world, "ExpandedStudioProperty", new Vector3(0f, -.22f, 0f),
                new Vector3(280f, .35f, 280f), StudioLand);
            BuildStudioGateAndRoad(world);
            BuildDowntown(world);
        }

        private void BuildStudioGateAndRoad(Transform parent)
        {
            var gate = new GameObject("StudioVehicleGate").transform;
            gate.SetParent(parent, false);
            gate.localPosition = new Vector3(138f, 0f, 0f);
            AddBlock(gate, "GatePostNorth", new Vector3(0f, 1.8f, 5.5f),
                new Vector3(1f, 3.6f, 1f), new Color(.34f, .28f, .23f));
            AddBlock(gate, "GatePostSouth", new Vector3(0f, 1.8f, -5.5f),
                new Vector3(1f, 3.6f, 1f), new Color(.34f, .28f, .23f));
            AddBlock(gate, "GateHeader", new Vector3(0f, 4f, 0f),
                new Vector3(1f, .8f, 12f), new Color(.28f, .22f, .19f));

            AddBlock(parent, "FutureInternalRoadApproach", new Vector3(84f, .02f, 0f),
                new Vector3(108f, .12f, 9f), Road);
            AddBlock(parent, "StudioGateTurningApron", new Vector3(126f, .02f, 0f),
                new Vector3(24f, .13f, 18f), Road);
            AddBlock(parent, "FutureVehicleRoad_StudioToDowntown", new Vector3(214.5f, .02f, 0f),
                new Vector3(153f, .12f, 9f), Road);
            AddBlock(parent, "RoadShoulderNorth", new Vector3(214.5f, .03f, 5.2f),
                new Vector3(153f, .14f, 1.4f), Sidewalk);
            AddBlock(parent, "RoadShoulderSouth", new Vector3(214.5f, .03f, -5.2f),
                new Vector3(153f, .14f, 1.4f), Sidewalk);
        }

        private void BuildDowntown(Transform parent)
        {
            var downtown = new GameObject("DowntownDistrict").transform;
            downtown.SetParent(parent, false);
            downtown.localPosition = new Vector3(300f, 0f, 0f);

            AddBlock(downtown, "MainStreetRoad", Vector3.zero,
                new Vector3(14f, .12f, 60f), Road);
            AddBlock(downtown, "TurningIntersection", new Vector3(-9f, .01f, 0f),
                new Vector3(24f, .13f, 18f), Road);
            AddBlock(downtown, "WestSidewalk", new Vector3(-8.5f, .08f, 0f),
                new Vector3(3f, .22f, 60f), Sidewalk);
            AddBlock(downtown, "EastSidewalk", new Vector3(8.5f, .08f, 0f),
                new Vector3(3f, .22f, 60f), Sidewalk);

            FilmingLocation street = LocationCatalog.GetLocation(FilmingLocationIds.DowntownMainStreet);
            var streetRoot = new GameObject(street.DisplayName).transform;
            streetRoot.SetParent(downtown, false);
            AddBlock(streetRoot, "StorefrontNorthWest", new Vector3(-12f, 3f, 17f),
                new Vector3(6f, 6f, 10f), Brick);
            AddBlock(streetRoot, "StorefrontSouthWest", new Vector3(-12f, 2.5f, -15f),
                new Vector3(6f, 5f, 12f), Stucco);
            AddBlock(streetRoot, "StorefrontNorthEast", new Vector3(12f, 3.5f, 17f),
                new Vector3(6f, 7f, 10f), new Color(.43f, .47f, .50f));
            Transform streetEntrance = Marker(streetRoot, "Arrival", new Vector3(-6f, .15f, -24f));
            RegisterFilmingLocation(streetRoot.gameObject, street, streetEntrance, 14f, 0f);

            FilmingLocation cafe = LocationCatalog.GetLocation(FilmingLocationIds.DowntownCornerCafe);
            var cafeRoot = new GameObject(cafe.DisplayName).transform;
            cafeRoot.SetParent(downtown, false);
            cafeRoot.localPosition = new Vector3(13f, 0f, -15f);
            AddBlock(cafeRoot, "CafeFloor", new Vector3(0f, -.1f, 0f),
                new Vector3(8f, .2f, 9f), new Color(.42f, .31f, .22f));
            AddBlock(cafeRoot, "CafeBackWall", new Vector3(0f, 2.6f, 4.3f),
                new Vector3(8f, 5f, .4f), Stucco);
            AddBlock(cafeRoot, "CafeSideWall", new Vector3(3.8f, 2.6f, 0f),
                new Vector3(.4f, 5f, 9f), Stucco);
            AddBlock(cafeRoot, "CafeWindowLeft", new Vector3(-2.3f, 2.4f, -4.3f),
                new Vector3(3f, 3.2f, .2f), Glass);
            AddBlock(cafeRoot, "CafeWindowRight", new Vector3(2.3f, 2.4f, -4.3f),
                new Vector3(3f, 3.2f, .2f), Glass);
            AddBlock(cafeRoot, "CafeAwning", new Vector3(0f, 4.2f, -4.8f),
                new Vector3(8f, .3f, 1.4f), new Color(.62f, .16f, .13f));
            AddBlock(cafeRoot, "CafeCounter", new Vector3(0f, 1f, 2.5f),
                new Vector3(5f, 2f, 1f), new Color(.30f, .20f, .14f));
            Transform cafeEntrance = Marker(cafeRoot, "Arrival", new Vector3(0f, .05f, -5.2f));
            RegisterFilmingLocation(cafeRoot.gameObject, cafe, cafeEntrance, 3.2f, 0f);
        }

        private void RegisterFilmingLocation(GameObject root, FilmingLocation location,
            Transform entrance, float stationZ, float filmingZ)
        {
            var view = root.AddComponent<FilmingLocationView>();
            view.Initialize(location, entrance);
            LocationViews.Register(view);

            var stations = root.AddComponent<ProductionStationLayout>();
            stations.EnsurePrototypeStations(stationZ);
            var marks = root.AddComponent<ActorSceneMarkLayout>();
            marks.EnsurePrototypeMarks(filmingZ);
            var blocking = root.AddComponent<SetBlockingPointLayout>();
            blocking.EnsurePrototypePoints(filmingZ);
            var slate = root.AddComponent<SlatePositionLayout>();
            slate.EnsurePrototypePositions(filmingZ + 1.2f, stationZ);
        }

        private static Transform Marker(Transform parent, string name, Vector3 localPosition)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = localPosition;
            return marker;
        }

        private static void AddBlock(Transform parent, string name, Vector3 localPosition,
            Vector3 localScale, Color color)
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
