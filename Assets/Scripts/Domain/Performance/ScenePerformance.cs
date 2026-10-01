using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Performance
{
    public sealed class PerformerBinding
    {
        public string RoleId { get; }
        public string PerformerId { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public PerformerBinding(string roleId, string performerId, IEnumerable<string> capabilities = null)
        {
            RoleId = TemplateValidation.Id(roleId);
            PerformerId = TemplateValidation.Id(performerId);
            Capabilities = TemplateValidation.Ids(capabilities ?? Array.Empty<string>());
        }
    }

    public sealed class ResolvedPerformanceBeat
    {
        public PerformanceBeat Definition { get; }
        public string PerformerId { get; }
        public string TargetPerformerId { get; }
        internal ResolvedPerformanceBeat(PerformanceBeat definition, IReadOnlyList<PerformerBinding> bindings)
        {
            Definition = definition;
            PerformerId = bindings.Single(binding => binding.RoleId == definition.RoleId).PerformerId;
            TargetPerformerId = definition.TargetRoleId == null ? null
                : bindings.Single(binding => binding.RoleId == definition.TargetRoleId).PerformerId;
        }
    }

    /// <summary>An immutable resolution. The consuming presentation resolves performer IDs to its own objects.</summary>
    public sealed class ScenePerformance
    {
        public SceneTemplate Template { get; }
        public SceneDuration Duration { get; }
        public IReadOnlyList<PerformerBinding> Bindings { get; }
        public IReadOnlyList<ResolvedPerformanceBeat> Beats { get; }

        public ScenePerformance(SceneTemplate template, SceneDuration duration, IEnumerable<PerformerBinding> bindings)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            var sequence = template.ResolveBeats(duration);
            Duration = duration;
            Bindings = TemplateValidation.Copy(bindings);
            var roleIds = TemplateValidation.Unique(Bindings.Select(binding => binding.RoleId));
            TemplateValidation.Unique(Bindings.Select(binding => binding.PerformerId));
            if (!roleIds.SetEquals(template.Roles.Select(role => role.Id)))
                throw new ArgumentException("Assign exactly one distinct performer to each declared role.");
            foreach (var role in template.Roles)
            {
                var binding = Bindings.Single(item => item.RoleId == role.Id);
                if (role.RequiredCapabilities.Any(requirement => !binding.Capabilities.Contains(requirement)))
                    throw new ArgumentException("Performer does not meet role requirements: " + role.Id);
            }
            Beats = Array.AsReadOnly(sequence.Select(beat => new ResolvedPerformanceBeat(beat, Bindings)).ToArray());
        }
    }

    /// <summary>A deterministic semantic cursor; timing, motion and animation belong to the consumer.</summary>
    public sealed class SceneSequenceCursor
    {
        public ScenePerformance Performance { get; }
        public int Index { get; private set; }
        public bool IsComplete => Index == Performance.Beats.Count;
        public ResolvedPerformanceBeat Current => IsComplete ? null : Performance.Beats[Index];
        public SceneSequenceCursor(ScenePerformance performance) =>
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
        public bool Advance()
        {
            if (IsComplete) return false;
            Index++;
            return true;
        }
        public void Reset() => Index = 0;
    }
}
