using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.Movie;
using UnityEngine;

namespace SilverScreen.Presentation.Writing
{
    public sealed class ScreenplayWritingWorldRouter : MonoBehaviour, IScreenplayWritingWorldRouter
    {
        private StudioEmployeeManager _employees;
        private ScriptOfficeStationLayout _office;
        public int WritingStationCapacity => _office != null ? _office.Capacity : 0;

        public void Initialize(StudioEmployeeManager employees, ScriptOfficeStationLayout office)
        { _employees = employees; _office = office; }

        public bool TryReserveWritingStation(Employee writer, out int stationIndex)
        {
            if (writer == null || _office == null) { stationIndex = -1; return false; }
            return _office.TryReserve(writer.Id, out stationIndex);
        }

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
            if (writer != null) _office?.Release(writer.Id);
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
        public StoryIdeaDevelopmentCoordinator IdeaCoordinator { get; private set; }
        private AutonomousWriterIdeaCoordinator _autonomousIdeas;

        private void Awake()
        {
            _employees = FindAnyObjectByType<StudioEmployeeManager>();
            _time = FindAnyObjectByType<SimulationTimeDriver>();
            if (_employees == null || _time?.TimeService == null) return;
            var office = FindAnyObjectByType<ScriptOfficeStationLayout>() ?? CreatePrototypeOffice();
            var world = GetComponent<ScreenplayWritingWorldRouter>() ?? gameObject.AddComponent<ScreenplayWritingWorldRouter>();
            world.Initialize(_employees, office);
            Coordinator = new ScreenplayWritingCoordinator(_employees.AllEmployees, _time.TimeService, world,
                new ScreenplayTitleGenerator(new SeededScreenplayTitleRandomSource(1930)));
            var genreIds = new List<string>();
            var movieDriver = FindAnyObjectByType<MovieProductionDriver>();
            var genres = movieDriver?.ProductionService?.AvailableGenres;
            if (genres != null) foreach (var genre in genres) if (genre != null && !string.IsNullOrWhiteSpace(genre.Id)) genreIds.Add(genre.Id);
            if (genreIds.Count == 0) genreIds.AddRange(new[] { "drama", "comedy", "action", "romance", "thriller", "horror" });
            IdeaCoordinator = new StoryIdeaDevelopmentCoordinator(_employees.AllEmployees, _time.TimeService, world,
                new StoryIdeaGenerator(new SeededScreenplayTitleRandomSource(1931),
                    new ScreenplayTitleGenerator(new SeededScreenplayTitleRandomSource(1932)), genreIds));
            _autonomousIdeas = new AutonomousWriterIdeaCoordinator(_employees.AllEmployees, _time.TimeService,
                IdeaCoordinator, new SeededScreenplayTitleRandomSource(1933));
        }

        private void OnDestroy()
        {
            _autonomousIdeas?.Dispose();
            Coordinator?.Dispose();
            IdeaCoordinator?.Dispose();
        }

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
            var candidateArrival = Marker("CandidateArrival", new Vector3(-.42f, -.5f, -1.28f));
            var candidateExit = Marker("CandidateExit", new Vector3(.42f, -.5f, -1.28f));
            var candidateWaiting = new List<Transform>();
            for (int i = 0; i < 4; i++)
                candidateWaiting.Add(Marker($"CandidateWaiting{i + 1:00}", new Vector3(-.3f + i * .2f, -.5f, -1.05f)));
            var recruitmentArea = root.AddComponent<CandidateWaitingAreaView>();
            recruitmentArea.Configure(RecruitmentDestination.ScriptOffice, entrance, candidateArrival, candidateExit, candidateWaiting);
            return layout;
        }
    }
}
