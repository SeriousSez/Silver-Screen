using UnityEngine;
using SilverScreen.Domain.Time;

namespace SilverScreen.Presentation.SimulationTime
{
    [CreateAssetMenu(menuName = "SilverScreen/Simulation Pacing")]
    public sealed class SimulationPacingSettings : ScriptableObject
    {
        [SerializeField] private string _profileId = "legacy";
        [SerializeField] private double _realSecondsPerStrategicDay = 1440;
        [SerializeField] private double _fastMultiplier = 2;
        [SerializeField] private double _veryFastMultiplier = 3;
        public SimulationPacing CreatePacing() => new SimulationPacing(_profileId, _realSecondsPerStrategicDay, _fastMultiplier, _veryFastMultiplier);
    }
}
