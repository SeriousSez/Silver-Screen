using System;
using System.Collections.Generic;
using System.Globalization;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.ReusableAssets
{
    /// <summary>Content-owned fields. No implicit player, studio name, or date fallback.</summary>
    public sealed class ArtworkContext
    {
        public const string StudioName = "studioName";
        public const string CurrentYear = "currentYear";
        public const string ProductionTitle = "productionTitle";
        private readonly Dictionary<string, string> _fields = new Dictionary<string, string>(StringComparer.Ordinal);
        public event Action Changed;
        public string Get(string key) => key != null && _fields.TryGetValue(key, out var value) ? value : string.Empty;

        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A template field needs a key.", nameof(key));
            value ??= string.Empty;
            if (Get(key) == value) return;
            _fields[key] = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Explicit adapter to existing game state. The owning composition disposes this binding.</summary>
    public sealed class StudioArtworkBinding : IDisposable
    {
        private readonly ArtworkContext _context;
        private readonly StudioIdentity _studio;
        private readonly ISimulationTimeService _time;
        public StudioArtworkBinding(ArtworkContext context, StudioIdentity studio, ISimulationTimeService time)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _studio = studio ?? throw new ArgumentNullException(nameof(studio));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _studio.OnNameChanged += NameChanged;
            _time.OnYearPassed += YearChanged;
            // SimulationClock.RestoreState announces restored state through OnSpeedChanged.
            _time.OnSpeedChanged += StateRestored;
            Refresh();
        }
        public void Refresh()
        {
            NameChanged(_studio.Name);
            YearChanged(_time.CurrentTime);
        }
        private void NameChanged(string value) => _context.Set(ArtworkContext.StudioName, value);
        private void YearChanged(SimulationDateTime value) => _context.Set(ArtworkContext.CurrentYear, value.Year.ToString(CultureInfo.InvariantCulture));
        private void StateRestored(SimulationSpeed _) => Refresh();
        public void Dispose()
        {
            _studio.OnNameChanged -= NameChanged;
            _time.OnYearPassed -= YearChanged;
            _time.OnSpeedChanged -= StateRestored;
        }
    }
}
