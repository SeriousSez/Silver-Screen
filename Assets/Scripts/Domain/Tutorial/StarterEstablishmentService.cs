using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Tutorial
{
    /// <summary>Small normal-game establishment authority for the three existing authored facilities.
    /// No construction economy existed for these facilities; placement, cost and work duration are deferred.</summary>
    public sealed class StarterEstablishmentService : SilverScreen.Domain.Buildings.IOperationalBuildings
    {
        private readonly TutorialSession _tutorial;
        private readonly Func<string, bool> _normallyAvailable;
        private readonly HashSet<string> _established = new HashSet<string>();
        public event Action<string> Established;
        public StarterEstablishmentService(TutorialSession tutorial, Func<string, bool> normallyAvailable)
        { _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial)); _normallyAvailable = normallyAvailable ?? throw new ArgumentNullException(nameof(normallyAvailable)); }
        public bool IsEstablished(string id) => _established.Contains(id);
        public bool IsNormallyAvailable(string id) => IsStarter(id) && !_established.Contains(id) && _normallyAvailable(id);
        public bool CanEstablish(string id) => _tutorial.IsAvailable(id, IsNormallyAvailable(id));
        public bool Establish(string id)
        {
            if (!CanEstablish(id)) return false;
            _established.Add(id); Established?.Invoke(id); return true;
        }
        private static bool IsStarter(string id) => id == StarterFeatureIds.Headquarters || id == StarterFeatureIds.Casting || id == StarterFeatureIds.Stage;
    }
}
