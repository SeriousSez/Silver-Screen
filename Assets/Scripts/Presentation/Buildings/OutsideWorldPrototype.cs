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
        private static readonly Color Roof = new Color(.20f, .19f, .19f);
        private static readonly Color Fence = new Color(.25f, .22f, .18f);
        private static readonly Color SilverScreenLand = new Color(.34f, .36f, .31f);
        private static readonly Color MajesticLand = new Color(.32f, .34f, .38f);
        private static readonly Color ColumbiaLand = new Color(.39f, .34f, .29f);
        private static readonly Color ResidentialLand = new Color(.31f, .42f, .28f);
        private static readonly Color IndustrialLand = new Color(.38f, .37f, .33f);
        private static readonly Color Field = new Color(.34f, .46f, .27f);
        private static readonly Color TreeTrunk = new Color(.26f, .18f, .11f);
        private static readonly Color TreeCrown = new Color(.19f, .36f, .18f);

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

            BuildPublicRoadNetwork(world);
            BuildSilverScreenCampus(world);
            BuildRivalStudioLots(world);
            BuildDowntown(world);
            BuildResidentialDistrict(world);
            BuildIndustrialDistrict(world);
            BuildOpenOutskirts(world);
        }

        private static void BuildPublicRoadNetwork(Transform parent)
        {
            var roads = new GameObject("PublicRoadNetwork").transform;
            roads.SetParent(parent, false);

            AddRoad(roads, "StudioAvenue", new Vector3(145f, .02f, 0f),
                new Vector3(190f, .12f, 10f), true);
            AddRoad(roads, "TownSpine", new Vector3(105f, .02f, 0f),
                new Vector3(12f, .12f, 480f), false);
            AddRoad(roads, "NorthLotRoad", new Vector3(-20f, .02f, 70f),
                new Vector3(250f, .12f, 10f), true);
            AddRoad(roads, "SouthLotRoad", new Vector3(-20f, .02f, -75f),
                new Vector3(250f, .12f, 10f), true);
            AddRoad(roads, "IndustrialRoad", new Vector3(165f, .02f, 145f),
                new Vector3(150f, .12f, 11f), true);
            AddRoad(roads, "ResidentialRoad", new Vector3(160f, .02f, -125f),
                new Vector3(160f, .12f, 9f), true);

            AddBlock(roads, "StudioIntersection", new Vector3(105f, .025f, 0f),
                new Vector3(16f, .13f, 16f), Road);
            AddBlock(roads, "NorthIntersection", new Vector3(105f, .025f, 70f),
                new Vector3(16f, .13f, 16f), Road);
            AddBlock(roads, "SouthIntersection", new Vector3(105f, .025f, -75f),
                new Vector3(16f, .13f, 16f), Road);
        }

        private static void BuildSilverScreenCampus(Transform parent)
        {
            var campus = new GameObject("SilverScreenMainStudioBlockout").transform;
            campus.SetParent(parent, false);

            AddBlock(campus, "StudioPropertyGround", new Vector3(0f, -.18f, 0f),
                new Vector3(80f, .25f, 68f), SilverScreenLand);
            AddFenceRectangle(campus, 80f, 68f, true, false);

            var gate = new GameObject("StudioVehicleGate").transform;
            gate.SetParent(campus, false);
            gate.localPosition = new Vector3(40f, 0f, 0f);
            AddBlock(gate, "GatePostNorth", new Vector3(0f, 1.8f, 6f),
                new Vector3(1f, 3.6f, 1f), Fence);
            AddBlock(gate, "GatePostSouth", new Vector3(0f, 1.8f, -6f),
                new Vector3(1f, 3.6f, 1f), Fence);
            AddBlock(gate, "GateHeader", new Vector3(0f, 4f, 0f),
                new Vector3(1f, .8f, 13f), new Color(.28f, .22f, .19f));
            AddBlock(campus, "InternalGateApproach", new Vector3(35f, .02f, 0f),
                new Vector3(30f, .12f, 10f), Road);
        }

        private static void BuildRivalStudioLots(Transform parent)
        {
            BuildRivalLot(
                parent,
                "MajesticPicturesStartingLot",
                new Vector3(-145f, 0f, 105f),
                new Vector2(72f, 58f),
                MajesticLand,
                new Color(.42f, .48f, .58f),
                false);
            BuildRivalLot(
                parent,
                "ColumbiaHeightsStudiosStartingLot",
                new Vector3(-145f, 0f, -110f),
                new Vector2(76f, 60f),
                ColumbiaLand,
                new Color(.64f, .45f, .28f),
                true);
        }

        private static void BuildRivalLot(
            Transform parent,
            string name,
            Vector3 position,
            Vector2 size,
            Color groundColor,
            Color accent,
            bool gateFacesNorth)
        {
            var lot = new GameObject(name).transform;
            lot.SetParent(parent, false);
            lot.localPosition = position;

            AddBlock(lot, "LotGround", new Vector3(0f, -.18f, 0f),
                new Vector3(size.x, .25f, size.y), groundColor);
            AddFenceRectangle(lot, size.x, size.y, false, gateFacesNorth);

            float frontageZ = gateFacesNorth ? size.y * .5f - 8f : -size.y * .5f + 8f;
            AddBuilding(lot, "Administration", new Vector3(-15f, 0f, frontageZ),
                new Vector3(10f, 6.5f, 8f), Stucco, accent);
            AddBuilding(lot, "EarlySoundStage", new Vector3(11f, 0f, 3f),
                new Vector3(17f, 8.5f, 13f), new Color(.39f, .40f, .41f), Roof);
            AddBuilding(lot, "UtilityBuilding", new Vector3(-12f, 0f, -7f),
                new Vector3(6f, 3.8f, 5f), new Color(.48f, .45f, .40f), Roof);
            AddBlock(lot, "GateSignPlaceholder",
                new Vector3(0f, 3f, gateFacesNorth ? size.y * .5f : -size.y * .5f),
                new Vector3(14f, 2.5f, .5f), accent);
        }

        private void BuildDowntown(Transform parent)
        {
            var downtown = new GameObject("DowntownDistrict").transform;
            downtown.SetParent(parent, false);
            downtown.localPosition = new Vector3(105f, 0f, 25f);

            AddBlock(downtown, "DowntownGround", new Vector3(0f, -.17f, 0f),
                new Vector3(78f, .22f, 105f), new Color(.42f, .40f, .36f));
            AddBlock(downtown, "MainStreetRoad", Vector3.zero,
                new Vector3(12f, .12f, 105f), Road);
            AddBlock(downtown, "WestSidewalk", new Vector3(-8f, .08f, 0f),
                new Vector3(4f, .22f, 105f), Sidewalk);
            AddBlock(downtown, "EastSidewalk", new Vector3(8f, .08f, 0f),
                new Vector3(4f, .22f, 105f), Sidewalk);

            FilmingLocation street = LocationCatalog.GetLocation(FilmingLocationIds.DowntownMainStreet);
            var streetRoot = new GameObject(street.DisplayName).transform;
            streetRoot.SetParent(downtown, false);
            AddBuilding(streetRoot, "GrandPictureTheatre", new Vector3(-18f, 0f, 29f),
                new Vector3(12f, 8f, 16f), Brick, new Color(.66f, .52f, .31f));
            AddBuilding(streetRoot, "TownHotel", new Vector3(18f, 0f, 30f),
                new Vector3(12f, 10f, 16f), new Color(.48f, .44f, .40f), Roof);
            AddBuilding(streetRoot, "WestShopA", new Vector3(-17f, 0f, -31f),
                new Vector3(7f, 5f, 10f), Stucco, Brick);
            AddBuilding(streetRoot, "WestShopB", new Vector3(-17f, 0f, -19f),
                new Vector3(7f, 5.5f, 10f), new Color(.68f, .57f, .44f), Roof);
            AddBuilding(streetRoot, "WestShopC", new Vector3(-17f, 0f, -7f),
                new Vector3(7f, 4.8f, 10f), new Color(.58f, .47f, .39f), Brick);
            AddBuilding(streetRoot, "EastOfficeA", new Vector3(17f, 0f, -31f),
                new Vector3(9f, 7f, 11f), new Color(.43f, .47f, .50f), Roof);
            AddBuilding(streetRoot, "EastOfficeB", new Vector3(17f, 0f, -18f),
                new Vector3(9f, 6f, 11f), new Color(.60f, .55f, .46f), Roof);
            Transform streetEntrance = Marker(streetRoot, "Arrival", new Vector3(-6f, .15f, -46f));
            RegisterFilmingLocation(streetRoot.gameObject, street, streetEntrance, 42f, 0f);

            FilmingLocation cafe = LocationCatalog.GetLocation(FilmingLocationIds.DowntownCornerCafe);
            var cafeRoot = new GameObject(cafe.DisplayName).transform;
            cafeRoot.SetParent(downtown, false);
            cafeRoot.localPosition = new Vector3(17f, 0f, 0f);
            AddBlock(cafeRoot, "CafeFloor", new Vector3(0f, -.1f, 0f),
                new Vector3(8f, .2f, 9f), new Color(.42f, .31f, .22f));
            AddBlock(cafeRoot, "CafeBackWall", new Vector3(0f, 2.6f, 4.3f),
                new Vector3(8f, 5.2f, .4f), Stucco);
            AddBlock(cafeRoot, "CafeSideWall", new Vector3(3.8f, 2.6f, 0f),
                new Vector3(.4f, 5.2f, 9f), Stucco);
            AddBlock(cafeRoot, "CafeWindowLeft", new Vector3(-2.2f, 2.3f, -4.3f),
                new Vector3(2.7f, 3f, .2f), Glass);
            AddBlock(cafeRoot, "CafeWindowRight", new Vector3(2.2f, 2.3f, -4.3f),
                new Vector3(2.7f, 3f, .2f), Glass);
            AddBlock(cafeRoot, "CafeAwning", new Vector3(0f, 4.1f, -4.8f),
                new Vector3(8f, .3f, 1.2f), new Color(.62f, .16f, .13f));
            AddBlock(cafeRoot, "CafeCounter", new Vector3(0f, 1f, 2.8f),
                new Vector3(5f, 2f, 1f), new Color(.30f, .20f, .14f));
            Transform cafeEntrance = Marker(cafeRoot, "Arrival", new Vector3(0f, .05f, -5.2f));
            RegisterFilmingLocation(cafeRoot.gameObject, cafe, cafeEntrance, 3.2f, 0f);
        }

        private static void BuildResidentialDistrict(Transform parent)
        {
            var residential = new GameObject("ResidentialNeighborhood").transform;
            residential.SetParent(parent, false);
            residential.localPosition = new Vector3(165f, 0f, -145f);
            AddBlock(residential, "NeighborhoodGround", new Vector3(0f, -.18f, 0f),
                new Vector3(125f, .22f, 78f), ResidentialLand);

            AddBuilding(residential, "CarltonApartmentsBlockout", new Vector3(-35f, 0f, 12f),
                new Vector3(13f, 9f, 14f), new Color(.62f, .49f, .37f), Roof);
            AddHouse(residential, "HouseA", new Vector3(2f, 0f, 16f), new Color(.72f, .64f, .48f));
            AddHouse(residential, "HouseB", new Vector3(30f, 0f, 13f), new Color(.58f, .66f, .61f));
            AddHouse(residential, "HouseC", new Vector3(48f, 0f, -17f), new Color(.72f, .55f, .44f));
            AddHouse(residential, "HouseD", new Vector3(13f, 0f, -17f), new Color(.66f, .64f, .57f));
            AddHouse(residential, "HawthorneHouseBlockout", new Vector3(-23f, 0f, -18f),
                new Color(.78f, .72f, .60f));
        }

        private static void BuildIndustrialDistrict(Transform parent)
        {
            var industrial = new GameObject("IndustrialDistrict").transform;
            industrial.SetParent(parent, false);
            industrial.localPosition = new Vector3(165f, 0f, 180f);
            AddBlock(industrial, "IndustrialGround", new Vector3(0f, -.18f, 0f),
                new Vector3(135f, .22f, 72f), IndustrialLand);

            AddBuilding(industrial, "PacificTextileWorksBlockout", new Vector3(-30f, 0f, 5f),
                new Vector3(24f, 10f, 18f), Brick, Roof);
            AddBuilding(industrial, "DowntownWarehouseBlockout", new Vector3(25f, 0f, 9f),
                new Vector3(20f, 8f, 16f), new Color(.43f, .42f, .39f), Roof);
            AddBuilding(industrial, "WorkshopGarage", new Vector3(37f, 0f, -22f),
                new Vector3(10f, 5f, 8f), new Color(.50f, .46f, .38f), Roof);
            AddBlock(industrial, "IndustrialYard", new Vector3(-25f, .01f, -24f),
                new Vector3(28f, .08f, 15f), new Color(.30f, .29f, .27f));
            AddBlock(industrial, "FactoryChimney", new Vector3(-39f, 9f, 10f),
                new Vector3(2.5f, 18f, 2.5f), new Color(.33f, .24f, .20f));
        }

        private static void BuildOpenOutskirts(Transform parent)
        {
            var outskirts = new GameObject("OpenUndevelopedOutskirts").transform;
            outskirts.SetParent(parent, false);

            AddBlock(outskirts, "WestField", new Vector3(-202f, -.2f, 0f),
                new Vector3(82f, .18f, 390f), Field);
            AddBlock(outskirts, "NorthField", new Vector3(-30f, -.21f, 198f),
                new Vector3(250f, .16f, 82f), new Color(.39f, .48f, .27f));
            AddBlock(outskirts, "SouthField", new Vector3(-25f, -.21f, -198f),
                new Vector3(255f, .16f, 82f), new Color(.36f, .45f, .25f));

            Vector3[] treePositions =
            {
                new Vector3(-225f, 0f, -160f), new Vector3(-210f, 0f, -85f),
                new Vector3(-225f, 0f, 42f), new Vector3(-203f, 0f, 170f),
                new Vector3(-82f, 0f, 208f), new Vector3(22f, 0f, 218f),
                new Vector3(72f, 0f, -212f), new Vector3(-75f, 0f, -218f),
                new Vector3(222f, 0f, 75f), new Vector3(225f, 0f, -72f),
                new Vector3(-85f, 0f, 145f), new Vector3(-82f, 0f, -155f)
            };
            for (int i = 0; i < treePositions.Length; i++)
                AddTree(outskirts, "OutskirtsTree" + (i + 1), treePositions[i]);
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

        private static void AddRoad(Transform parent, string name, Vector3 position,
            Vector3 scale, bool horizontal)
        {
            AddBlock(parent, name, position, scale, Road);
            if (horizontal)
            {
                AddBlock(parent, name + "NorthShoulder",
                    position + new Vector3(0f, .02f, scale.z * .5f + 1.2f),
                    new Vector3(scale.x, .14f, 2.4f), Sidewalk);
                AddBlock(parent, name + "SouthShoulder",
                    position + new Vector3(0f, .02f, -scale.z * .5f - 1.2f),
                    new Vector3(scale.x, .14f, 2.4f), Sidewalk);
            }
            else
            {
                AddBlock(parent, name + "WestShoulder",
                    position + new Vector3(-scale.x * .5f - 1.2f, .02f, 0f),
                    new Vector3(2.4f, .14f, scale.z), Sidewalk);
                AddBlock(parent, name + "EastShoulder",
                    position + new Vector3(scale.x * .5f + 1.2f, .02f, 0f),
                    new Vector3(2.4f, .14f, scale.z), Sidewalk);
            }
        }

        private static void AddFenceRectangle(
            Transform parent,
            float width,
            float depth,
            bool gateOnEast,
            bool gateOnNorth)
        {
            const float fenceHeight = 1.4f;
            const float fenceThickness = .35f;
            AddBlock(parent, "FenceWest", new Vector3(-width * .5f, fenceHeight * .5f, 0f),
                new Vector3(fenceThickness, fenceHeight, depth), Fence);

            if (gateOnEast)
            {
                AddBlock(parent, "FenceNorth", new Vector3(0f, fenceHeight * .5f, depth * .5f),
                    new Vector3(width, fenceHeight, fenceThickness), Fence);
                AddBlock(parent, "FenceSouth", new Vector3(0f, fenceHeight * .5f, -depth * .5f),
                    new Vector3(width, fenceHeight, fenceThickness), Fence);
                float segmentDepth = (depth - 14f) * .5f;
                AddBlock(parent, "FenceEastNorth",
                    new Vector3(width * .5f, fenceHeight * .5f, segmentDepth * .5f + 7f),
                    new Vector3(fenceThickness, fenceHeight, segmentDepth), Fence);
                AddBlock(parent, "FenceEastSouth",
                    new Vector3(width * .5f, fenceHeight * .5f, -segmentDepth * .5f - 7f),
                    new Vector3(fenceThickness, fenceHeight, segmentDepth), Fence);
            }
            else
            {
                AddBlock(parent, "FenceEast", new Vector3(width * .5f, fenceHeight * .5f, 0f),
                    new Vector3(fenceThickness, fenceHeight, depth), Fence);
                float backZ = gateOnNorth ? -depth * .5f : depth * .5f;
                AddBlock(parent, "FenceBack", new Vector3(0f, fenceHeight * .5f, backZ),
                    new Vector3(width, fenceHeight, fenceThickness), Fence);
                float segmentWidth = (width - 14f) * .5f;
                float z = gateOnNorth ? depth * .5f : -depth * .5f;
                AddBlock(parent, "FenceFrontWest",
                    new Vector3(-segmentWidth * .5f - 7f, fenceHeight * .5f, z),
                    new Vector3(segmentWidth, fenceHeight, fenceThickness), Fence);
                AddBlock(parent, "FenceFrontEast",
                    new Vector3(segmentWidth * .5f + 7f, fenceHeight * .5f, z),
                    new Vector3(segmentWidth, fenceHeight, fenceThickness), Fence);
            }
        }

        private static void AddBuilding(
            Transform parent,
            string name,
            Vector3 groundPosition,
            Vector3 size,
            Color wallColor,
            Color roofColor)
        {
            var building = new GameObject(name).transform;
            building.SetParent(parent, false);
            building.localPosition = groundPosition;
            AddBlock(building, "Body", new Vector3(0f, size.y * .5f, 0f), size, wallColor);
            AddBlock(building, "Roof", new Vector3(0f, size.y + .35f, 0f),
                new Vector3(size.x + 1.2f, .7f, size.z + 1.2f), roofColor);
            float entranceWidth = Mathf.Min(2.2f, size.x * .22f);
            float entranceHeight = Mathf.Min(2.8f, size.y * .55f);
            AddBlock(building, "Entrance",
                new Vector3(0f, entranceHeight * .5f, -size.z * .5f - .12f),
                new Vector3(entranceWidth, entranceHeight, .25f), Glass);
        }

        private static void AddHouse(Transform parent, string name, Vector3 position, Color color)
        {
            AddBuilding(parent, name, position, new Vector3(7.5f, 4.5f, 8.5f), color, Roof);
        }

        private static void AddTree(Transform parent, string name, Vector3 position)
        {
            var tree = new GameObject(name).transform;
            tree.SetParent(parent, false);
            tree.localPosition = position;
            AddBlock(tree, "Trunk", new Vector3(0f, 2f, 0f),
                new Vector3(1.2f, 4f, 1.2f), TreeTrunk);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(tree, false);
            crown.transform.localPosition = new Vector3(0f, 5f, 0f);
            crown.transform.localScale = new Vector3(5f, 6f, 5f);
            crown.GetComponent<Renderer>().material.color = TreeCrown;
        }

        private static Transform Marker(Transform parent, string name, Vector3 localPosition)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = localPosition;
            return marker;
        }

        private static void AddBlock(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
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
