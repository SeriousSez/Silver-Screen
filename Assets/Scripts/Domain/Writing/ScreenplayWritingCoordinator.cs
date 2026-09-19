using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Writing
{
    public enum WritingRouteResult { Started, EmployeeMissing, AgentMissing, OfficeMissing, StationMissing, NavigationRejected }

    public interface IScreenplayWritingWorldRouter
    {
        int WritingStationCapacity { get; }
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
        public IReadOnlyList<ScreenplayProject> Library => _library;
        public event Action<ScreenplayProject> ScreenplayAdded;
        public event Action<ScreenplayProject> ScreenplayChanged;

        public ScreenplayWritingCoordinator(IReadOnlyList<Employee> employees, ISimulationTimeService time, IScreenplayWritingWorldRouter world)
        {
            _employees = employees ?? throw new ArgumentNullException(nameof(employees));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _time.OnMinutePassed += HandleMinutePassed;
        }

        public ScreenplayProject Create(string title, IEnumerable<string> writerIds)
        {
            var ids = new List<string>();
            if (writerIds != null) foreach (string id in writerIds) if (!ids.Contains(id)) ids.Add(id);
            if (ids.Count == 0 || ids.Count > _world.WritingStationCapacity) return null;
            foreach (string id in ids)
            {
                var writer = FindWriter(id);
                if (writer == null || writer.CurrentState != EmployeeState.Idle) return null;
            }

            var screenplay = new ScreenplayProject(Guid.NewGuid().ToString(), title, ids);
            screenplay.Changed += HandleScreenplayChanged;
            _library.Add(screenplay);
            ScreenplayAdded?.Invoke(screenplay);
            for (int i = 0; i < ids.Count; i++) RouteWriter(screenplay, ids[i], i);
            return screenplay;
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
            foreach (var screenplay in _library) screenplay.Changed -= HandleScreenplayChanged;
        }
    }
}
