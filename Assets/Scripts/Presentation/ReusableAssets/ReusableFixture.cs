using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    /// <summary>Geometry identity only. Lighting, artwork and future interactions are separate components.</summary>
    public sealed class ReusableFixture : MonoBehaviour
    {
        [SerializeField] private ReusableAssetDefinition _definition;
        public ReusableAssetDefinition Definition => _definition;
    }
}
