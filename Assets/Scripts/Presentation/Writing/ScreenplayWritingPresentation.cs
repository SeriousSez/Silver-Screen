using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;

namespace SilverScreen.Presentation.Writing
{
    public sealed class ScriptOfficeStationLayout : MonoBehaviour
    {
        [SerializeField] private Transform _entrance;
        [SerializeField] private List<Transform> _stations = new List<Transform>();
        public Transform Entrance => _entrance;
        public int Capacity => _stations.Count;

        public bool TryGetStation(int index, out Vector3 position)
        {
            if (index >= 0 && index < _stations.Count && _stations[index] != null)
            { position = _stations[index].position; return true; }
            position = default; return false;
        }

        public void Configure(Transform entrance, IEnumerable<Transform> stations)
        {
            _entrance = entrance; _stations.Clear();
            if (stations != null) _stations.AddRange(stations);
        }
    }

    public sealed class ScreenplayWritingWorldRouter : MonoBehaviour, IScreenplayWritingWorldRouter
    {
        private StudioEmployeeManager _employees;
        private ScriptOfficeStationLayout _office;
        public int WritingStationCapacity => _office != null ? _office.Capacity : 0;

        public void Initialize(StudioEmployeeManager employees, ScriptOfficeStationLayout office)
        { _employees = employees; _office = office; }

        public WritingRouteResult SendToScriptOffice(Employee writer, EmployeeIntent intent, Action onArrival) =>
            Route(writer, _office != null ? _office.Entrance : null, intent, onArrival, true);

        public WritingRouteResult SendToWritingStation(Employee writer, int stationIndex, EmployeeIntent intent, Action onArrival)
        {
            if (_office == null) return WritingRouteResult.OfficeMissing;
            if (!_office.TryGetStation(stationIndex, out Vector3 position)) return WritingRouteResult.StationMissing;
            return Route(writer, position, intent, onArrival);
        }

        public void ReleaseWriter(Employee writer)
        {
            var agent = _employees != null ? _employees.GetAgent(writer) : null;
            agent?.ClearTaskDestination();
        }

        private WritingRouteResult Route(Employee writer, Transform target, EmployeeIntent intent, Action onArrival, bool office = false) =>
            target == null ? WritingRouteResult.OfficeMissing : Route(writer, target.position, intent, onArrival);

        private WritingRouteResult Route(Employee writer, Vector3 target, EmployeeIntent intent, Action onArrival)
        {
            if (writer == null) return WritingRouteResult.EmployeeMissing;
            var agent = _employees != null ? _employees.GetAgent(writer) : null;
            if (agent == null) return WritingRouteResult.AgentMissing;
            return agent.TryAssignTaskDestination(target, intent, onArrival)
                ? WritingRouteResult.Started : WritingRouteResult.NavigationRejected;
        }
    }

    public sealed class ScreenplayWritingDriver : MonoBehaviour
    {
        private StudioEmployeeManager _employees;
        private SimulationTimeDriver _time;
        public ScreenplayWritingCoordinator Coordinator { get; private set; }

        private void Awake()
        {
            _employees = FindAnyObjectByType<StudioEmployeeManager>();
            _time = FindAnyObjectByType<SimulationTimeDriver>();
            if (_employees == null || _time?.TimeService == null) return;
            var office = FindAnyObjectByType<ScriptOfficeStationLayout>() ?? CreatePrototypeOffice();
            var world = GetComponent<ScreenplayWritingWorldRouter>() ?? gameObject.AddComponent<ScreenplayWritingWorldRouter>();
            world.Initialize(_employees, office);
            Coordinator = new ScreenplayWritingCoordinator(_employees.AllEmployees, _time.TimeService, world);
        }

        private void OnDestroy() => Coordinator?.Dispose();

        private static ScriptOfficeStationLayout CreatePrototypeOffice()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "ScriptOffice";
            root.transform.position = new Vector3(-15f, 2f, -9f);
            root.transform.localScale = new Vector3(10f, 4f, 7f);
            root.GetComponent<Renderer>().material.color = new Color(.64f, .50f, .38f);
            var view = root.AddComponent<StudioBuildingView>();
            Transform Marker(string name, Vector3 local)
            {
                var marker = new GameObject(name).transform;
                marker.SetParent(root.transform, false); marker.localPosition = local; return marker;
            }
            var entrance = Marker("Entrance", new Vector3(0f, -.5f, -.58f));
            view.Initialize(BuildingType.ScriptOffice, "Script Office", entrance);
            var stations = new List<Transform>();
            for (int i = 0; i < 4; i++)
                stations.Add(Marker($"WritingStation{i + 1:00}", new Vector3(-.3f + i * .2f, -.5f, -.82f)));
            var layout = root.AddComponent<ScriptOfficeStationLayout>();
            layout.Configure(entrance, stations);
            return layout;
        }
    }
}
