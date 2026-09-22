using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    public enum FixtureAnchorKind { Mount, Service, Pivot, Socket, Latch, Insert, Light }

    /// <summary>Local +Z is the mating/axis direction. Dimensions are metres, never a collider.</summary>
    public sealed class FixtureAnchor : MonoBehaviour
    {
        [SerializeField] private FixtureAnchorKind _kind;
        [SerializeField] private float _nominalDiameter;
        public FixtureAnchorKind Kind => _kind;
        public float NominalDiameter => _nominalDiameter;
    }
}
