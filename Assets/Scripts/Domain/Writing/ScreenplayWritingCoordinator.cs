using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Writing
{
    public interface IScreenplayTitleRandomSource
    {
        int Next(int minimumInclusive, int maximumExclusive);
    }

    public sealed class SeededScreenplayTitleRandomSource : IScreenplayTitleRandomSource
    {
        private readonly Random _random;
        public SeededScreenplayTitleRandomSource(int seed) => _random = new Random(seed);
        public int Next(int minimumInclusive, int maximumExclusive) => _random.Next(minimumInclusive, maximumExclusive);
    }

    public sealed class ScreenplayTitleGenerator
    {
        private const int MaximumDuplicateRetries = 12;
        private readonly IScreenplayTitleRandomSource _random;
        private static readonly Dictionary<string, string[][]> Components = new Dictionary<string, string[][]>(StringComparer.OrdinalIgnoreCase)
        {
            ["action"] = new[] { new[] { "Final", "Last", "Broken", "Hidden", "Burning" }, new[] { "Pursuit", "Stand", "Line", "Target", "Frontier" } },
            ["comedy"] = new[] { new[] { "One Bad", "Almost", "Perfectly", "Unexpected", "Another" }, new[] { "Weekend", "Perfect", "Trouble", "Mix-Up", "Situation" } },
            ["drama"] = new[] { new[] { "The Long", "A Quiet", "Distant", "Fading", "The Empty" }, new[] { "Road", "Promise", "Season", "House", "Hour" } },
            ["horror"] = new[] { new[] { "Beneath the", "The Hollow", "After", "Beyond the", "Inside the" }, new[] { "Floor", "House", "Midnight", "Walls", "Dark" } },
            ["romance"] = new[] { new[] { "Summer", "Until", "A Place for", "Borrowed", "The Last" }, new[] { "Letters", "Tomorrow", "Two", "Hearts", "Dance" } },
            ["thriller"] = new[] { new[] { "The Silent", "Unknown", "Midnight", "False", "Vanishing" }, new[] { "Witness", "Signal", "Alibi", "Passenger", "Evidence" } },
            ["sci-fi"] = new[] { new[] { "Beyond", "Signal", "The Last", "Distant", "Return to" }, new[] { "Orion", "Unknown", "Horizon", "Earth", "Tomorrow" } }
        };

        public ScreenplayTitleGenerator(IScreenplayTitleRandomSource random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        public string GenerateUnique(string genreId, IEnumerable<string> existingTitles)
        {
            var existing = new HashSet<string>(existingTitles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string candidate = null;
            for (int attempt = 0; attempt < MaximumDuplicateRetries; attempt++)
            {
                candidate = Generate(genreId);
                if (!existing.Contains(candidate)) return candidate;
            }
            int suffix = 2;
            string fallback;
            do fallback = $"{candidate ?? "Untitled Screenplay"} {suffix++}";
            while (existing.Contains(fallback));
            return fallback;
        }

        private string Generate(string genreId)
        {
            if (!Components.TryGetValue(genreId ?? string.Empty, out var parts)) parts = Components["drama"];
            return $"{parts[0][_random.Next(0, parts[0].Length)]} {parts[1][_random.Next(0, parts[1].Length)]}";
        }
    }

    public enum WritingRouteResult { Started, EmployeeMissing, AgentMissing, OfficeMissing, StationMissing, NavigationRejected }

    public interface IScreenplayWritingWorldRouter
    {
        int WritingStationCapacity { get; }
        bool TryReserveWritingStation(Employee writer, out int stationIndex);
        WritingRouteResult SendToScriptOffice(Employee writer, EmployeeIntent intent, Action onArrival);
        WritingRouteResult SendToWritingStation(Employee writer, int stationIndex, EmployeeIntent intent, Action onArrival);
        void ReleaseWriter(Employee writer);
    }

    public sealed class ScreenplayWritingCoordinator : IDisposable
    {
        private readonly List<ScreenplayProject> _library = new List<ScreenplayProject>();
        private readonly IReadOnlyList<Employee> _employees;
        private readonly ISimulationTimeService _time;
        private readonly IScreenplayWritingWorldRouter _world;
        private readonly ScreenplayTitleGenerator _titleGenerator;
        public IReadOnlyList<ScreenplayProject> Library => _library;
        public event Action<ScreenplayProject> ScreenplayAdded;
        public event Action<ScreenplayProject> ScreenplayChanged;

        public bool CanAssignWriter(string writerId)
        {
            return WriterAssignmentRules.CanAssign(FindWriter(writerId));
        }

        public ScreenplayWritingCoordinator(IReadOnlyList<Employee> employees, ISimulationTimeService time,
            IScreenplayWritingWorldRouter world, ScreenplayTitleGenerator titleGenerator)
        {
            _employees = employees ?? throw new ArgumentNullException(nameof(employees));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _titleGenerator = titleGenerator ?? throw new ArgumentNullException(nameof(titleGenerator));
            _time.OnMinutePassed += HandleMinutePassed;
        }

        public ScreenplayProject CreateCommissioned(string genreId, IEnumerable<string> writerIds)
        {
            if (string.IsNullOrWhiteSpace(genreId) ||
                !TryReserveWriters(writerIds, out var ids, out var stations)) return null;

            var existingTitles = new List<string>();
            foreach (var existing in _library) existingTitles.Add(existing.Title);
            string title = _titleGenerator.GenerateUnique(genreId, existingTitles);
            var screenplay = new ScreenplayProject(Guid.NewGuid().ToString(), title, ids, new[] { genreId },
                ScreenplayAcquisitionSource.Commissioned);
            RegisterAndRoute(screenplay, ids, stations);
            return screenplay;
        }

        public ScreenplayProject CreateFromIdea(StoryIdea idea, IEnumerable<string> writerIds)
        {
            if (idea == null || idea.State != IdeaDevelopmentState.Completed ||
                idea.HasScreenplayDevelopment || HasScreenplayForIdea(idea.Id) ||
                !TryReserveWriters(writerIds, out var ids, out var stations)) return null;

            var screenplay = new ScreenplayProject(Guid.NewGuid().ToString(), idea.Title, ids,
                idea.GenreIds, ScreenplayAcquisitionSource.DevelopedFromIdea, idea.Id,
                idea.SettingId, idea.ProtagonistArchetypeId, idea.AntagonistArchetypeId,
                idea.ThemeId);
            if (!idea.TryLinkDevelopedScreenplay(screenplay.Id))
            {
                ReleaseReservations(stations);
                return null;
            }
            RegisterAndRoute(screenplay, ids, stations);
            return screenplay;
        }

        private bool TryReserveWriters(IEnumerable<string> writerIds, out List<string> ids,
            out Dictionary<string, int> stations)
        {
            ids = new List<string>();
            stations = new Dictionary<string, int>();
            if (writerIds != null)
                foreach (string id in writerIds)
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id)) ids.Add(id);
            if (ids.Count == 0 || ids.Count > 4 || ids.Count > _world.WritingStationCapacity)
                return false;

            foreach (string id in ids)
                if (!CanAssignWriter(id)) return false;

            foreach (string id in ids)
            {
                Employee writer = FindWriter(id);
                if (_world.TryReserveWritingStation(writer, out int station))
                {
                    stations[id] = station;
                    continue;
                }
                ReleaseReservations(stations);
                stations.Clear();
                return false;
            }
            return true;
        }

        private void RegisterAndRoute(ScreenplayProject screenplay, IReadOnlyList<string> ids,
            IReadOnlyDictionary<string, int> stations)
        {
            screenplay.Changed += HandleScreenplayChanged;
            _library.Add(screenplay);
            ScreenplayAdded?.Invoke(screenplay);
            for (int i = 0; i < ids.Count; i++) RouteWriter(screenplay, ids[i], stations[ids[i]]);
        }

        private bool HasScreenplayForIdea(string ideaId)
        {
            for (int i = 0; i < _library.Count; i++)
                if (_library[i].SourceStoryIdeaId == ideaId) return true;
            return false;
        }

        private void ReleaseReservations(IReadOnlyDictionary<string, int> stations)
        {
            foreach (string writerId in stations.Keys)
            {
                Employee writer = FindWriter(writerId);
                if (writer != null) _world.ReleaseWriter(writer);
            }
        }

        private void RouteWriter(ScreenplayProject screenplay, string writerId, int station)
        {
            Employee writer = FindWriter(writerId);
            screenplay.SetParticipation(writerId, WriterParticipationStatus.Traveling);
            var result = _world.SendToScriptOffice(writer,
                new EmployeeIntent(EmployeeIntentPurpose.ReportToScriptOffice, $"Travelling to write {screenplay.Title}", "ScriptOffice"),
                () => RouteToStation(screenplay, writer, station));
            if (result != WritingRouteResult.Started) MarkUnavailable(screenplay, writer);
        }

        private void RouteToStation(ScreenplayProject screenplay, Employee writer, int station)
        {
            var result = _world.SendToWritingStation(writer, station,
                new EmployeeIntent(EmployeeIntentPurpose.MovingToWritingStation, $"Finding a desk for {screenplay.Title}", "ScriptOffice"),
                () => BeginWriting(screenplay, writer));
            if (result != WritingRouteResult.Started) MarkUnavailable(screenplay, writer);
        }

        private void BeginWriting(ScreenplayProject screenplay, Employee writer)
        {
            if (screenplay.Status == ScreenplayStatus.Completed) { _world.ReleaseWriter(writer); return; }
            writer.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.WriteScreenplay, $"Writing {screenplay.Title}", "ScriptOffice"));
            writer.SetState(EmployeeState.Writing);
            screenplay.SetParticipation(writer.Id, WriterParticipationStatus.Writing);
        }

        private void HandleMinutePassed(SimulationDateTime unused)
        {
            foreach (var screenplay in _library)
            {
                if (screenplay.Status == ScreenplayStatus.Completed) continue;
                var active = new Dictionary<string, int>();
                foreach (var contributor in screenplay.Contributors)
                {
                    if (contributor.Status != WriterParticipationStatus.Writing) continue;
                    Employee writer = FindWriter(contributor.WriterId);
                    if (writer == null || writer.Role != EmployeeRole.Writer || writer.CurrentState != EmployeeState.Writing)
                    {
                        if (writer != null) MarkUnavailable(screenplay, writer);
                        else screenplay.SetParticipation(contributor.WriterId, WriterParticipationStatus.Unavailable);
                        continue;
                    }
                    active[writer.Id] = writer.Skill;
                }
                bool wasCompleted = screenplay.Status == ScreenplayStatus.Completed;
                screenplay.AdvanceWritingMinute(active);
                if (!wasCompleted && screenplay.Status == ScreenplayStatus.Completed) ReleaseAll(screenplay);
            }
        }

        private void MarkUnavailable(ScreenplayProject screenplay, Employee writer)
        {
            screenplay.SetParticipation(writer.Id, WriterParticipationStatus.Unavailable);
            _world.ReleaseWriter(writer);
        }

        private void ReleaseAll(ScreenplayProject screenplay)
        {
            foreach (var contributor in screenplay.Contributors)
            {
                Employee writer = FindWriter(contributor.WriterId);
                if (writer != null) _world.ReleaseWriter(writer);
            }
        }

        private Employee FindWriter(string id) =>
            _employees.Count == 0 ? null : FindEmployee(id);
        private Employee FindEmployee(string id)
        {
            for (int i = 0; i < _employees.Count; i++)
                if (_employees[i].Id == id && _employees[i].Role == EmployeeRole.Writer) return _employees[i];
            return null;
        }
        private void HandleScreenplayChanged(ScreenplayProject screenplay) => ScreenplayChanged?.Invoke(screenplay);
        public void Dispose()
        {
            _time.OnMinutePassed -= HandleMinutePassed;
            foreach (var screenplay in _library)
            {
                screenplay.Changed -= HandleScreenplayChanged;
                if (screenplay.Status != ScreenplayStatus.Completed) ReleaseAll(screenplay);
            }
        }
    }
}
