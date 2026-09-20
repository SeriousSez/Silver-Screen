using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Writing
{
    public sealed class StoryIdeaDevelopmentCoordinator : IDisposable
    {
        private const int PrototypeDurationMinutes = 360;
        private readonly List<StoryIdea> _library = new List<StoryIdea>();
        private readonly IReadOnlyList<Employee> _employees;
        private readonly ISimulationTimeService _time;
        private readonly IScreenplayWritingWorldRouter _world;
        private readonly StoryIdeaGenerator _generator;

        public IReadOnlyList<StoryIdea> Library => _library;
        public event Action<StoryIdea> IdeaAdded;
        public event Action<StoryIdea> IdeaChanged;

        public StoryIdeaDevelopmentCoordinator(IReadOnlyList<Employee> employees, ISimulationTimeService time,
            IScreenplayWritingWorldRouter world, StoryIdeaGenerator generator)
        {
            _employees = employees ?? throw new ArgumentNullException(nameof(employees));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _time.OnMinutePassed += HandleMinutePassed;
        }

        public StoryIdea TryBeginDevelopment(string writerId)
        {
            Employee writer = FindWriter(writerId);
            if (!WriterAssignmentRules.CanAssign(writer) ||
                !_world.TryReserveWritingStation(writer, out int station)) return null;

            var idea = new StoryIdea(Guid.NewGuid().ToString(), writer.Id);
            idea.Changed += HandleIdeaChanged;

            var result = _world.SendToScriptOffice(writer,
                new EmployeeIntent(EmployeeIntentPurpose.ReportToScriptOffice,
                    "Travelling to develop a new idea", "ScriptOffice"),
                () => RouteToStation(idea, writer, station));
            if (result != WritingRouteResult.Started)
            {
                idea.Changed -= HandleIdeaChanged;
                _world.ReleaseWriter(writer);
                return null;
            }

            _library.Add(idea);
            IdeaAdded?.Invoke(idea);
            return idea;
        }

        private void RouteToStation(StoryIdea idea, Employee writer, int station)
        {
            var result = _world.SendToWritingStation(writer, station,
                new EmployeeIntent(EmployeeIntentPurpose.MovingToWritingStation,
                    "Finding a desk for idea development", "ScriptOffice"),
                () => BeginDevelopment(idea, writer));
            if (result != WritingRouteResult.Started) Fail(idea, writer);
        }

        private void BeginDevelopment(StoryIdea idea, Employee writer)
        {
            writer.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.DevelopIdea,
                "Developing a new story idea", "ScriptOffice"));
            writer.SetState(EmployeeState.DevelopingIdea);
            idea.BeginDevelopment();
        }

        private void HandleMinutePassed(SimulationDateTime unused)
        {
            foreach (StoryIdea idea in _library)
            {
                if (idea.State != IdeaDevelopmentState.Developing) continue;
                Employee writer = FindWriter(idea.CreatorWriterId);
                if (writer == null || writer.CurrentState != EmployeeState.DevelopingIdea ||
                    writer.CurrentIntent?.Purpose != EmployeeIntentPurpose.DevelopIdea)
                {
                    Fail(idea, writer);
                    continue;
                }
                if (!idea.AdvanceDevelopmentMinute(writer.Skill, PrototypeDurationMinutes)) continue;

                var titles = new List<string>();
                foreach (StoryIdea existing in _library)
                    if (existing.State == IdeaDevelopmentState.Completed) titles.Add(existing.Title);
                StoryIdeaConcept concept = _generator.Generate(titles);
                idea.Complete(concept.Title, concept.GenreIds, concept.SettingId,
                    concept.ProtagonistId, concept.AntagonistId, concept.ThemeId);
                _world.ReleaseWriter(writer);
            }
        }

        private void Fail(StoryIdea idea, Employee writer)
        {
            idea.Fail();
            if (writer != null) _world.ReleaseWriter(writer);
        }

        private Employee FindWriter(string id)
        {
            for (int i = 0; i < _employees.Count; i++)
                if (_employees[i].Id == id && _employees[i].Role == EmployeeRole.Writer)
                    return _employees[i];
            return null;
        }

        private void HandleIdeaChanged(StoryIdea idea) => IdeaChanged?.Invoke(idea);

        public void Dispose()
        {
            _time.OnMinutePassed -= HandleMinutePassed;
            foreach (StoryIdea idea in _library)
            {
                idea.Changed -= HandleIdeaChanged;
                if (idea.State != IdeaDevelopmentState.Traveling &&
                    idea.State != IdeaDevelopmentState.Developing) continue;
                Employee writer = FindWriter(idea.CreatorWriterId);
                if (writer != null) _world.ReleaseWriter(writer);
            }
        }
    }
}
