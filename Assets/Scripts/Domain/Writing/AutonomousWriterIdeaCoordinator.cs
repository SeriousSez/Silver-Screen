using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Writing
{
    public sealed class AutonomousIdeaDevelopmentSettings
    {
        public int OpportunityCadenceMinutes { get; }
        public int OpportunityChancePercent { get; }

        public AutonomousIdeaDevelopmentSettings(int opportunityCadenceMinutes = 240,
            int opportunityChancePercent = 35)
        {
            OpportunityCadenceMinutes = Math.Max(1, opportunityCadenceMinutes);
            OpportunityChancePercent = Math.Clamp(opportunityChancePercent, 0, 100);
        }
    }

    public sealed class AutonomousWriterIdeaCoordinator : IDisposable
    {
        private readonly IReadOnlyList<Employee> _employees;
        private readonly ISimulationTimeService _time;
        private readonly StoryIdeaDevelopmentCoordinator _ideas;
        private readonly IScreenplayTitleRandomSource _random;
        private int _minutesUntilOpportunity;

        public AutonomousIdeaDevelopmentSettings Settings { get; }

        public AutonomousWriterIdeaCoordinator(IReadOnlyList<Employee> employees, ISimulationTimeService time,
            StoryIdeaDevelopmentCoordinator ideas, IScreenplayTitleRandomSource random,
            AutonomousIdeaDevelopmentSettings settings = null)
        {
            _employees = employees ?? throw new ArgumentNullException(nameof(employees));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _ideas = ideas ?? throw new ArgumentNullException(nameof(ideas));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Settings = settings ?? new AutonomousIdeaDevelopmentSettings();
            _minutesUntilOpportunity = Settings.OpportunityCadenceMinutes;
            _time.OnMinutePassed += HandleMinutePassed;
        }

        private void HandleMinutePassed(SimulationDateTime unused)
        {
            _minutesUntilOpportunity--;
            if (_minutesUntilOpportunity > 0) return;

            _minutesUntilOpportunity = Settings.OpportunityCadenceMinutes;
            if (_random.Next(0, 100) >= Settings.OpportunityChancePercent) return;

            var candidates = new List<Employee>();
            for (int i = 0; i < _employees.Count; i++)
            {
                Employee writer = _employees[i];
                if (WriterAssignmentRules.CanAssign(writer)) candidates.Add(writer);
            }
            if (candidates.Count == 0) return;

            Employee selected = candidates[_random.Next(0, candidates.Count)];
            _ideas.TryBeginDevelopment(selected.Id);
        }

        public void Dispose() => _time.OnMinutePassed -= HandleMinutePassed;
    }
}
