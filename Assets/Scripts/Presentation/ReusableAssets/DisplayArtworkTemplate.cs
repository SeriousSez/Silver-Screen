using System;
using TMPro;
using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    [Serializable]
    public sealed class ArtworkTextField
    {
        public string Key;
        [Tooltip("Used only when Key is empty. Historical artifact dates belong here, never in currentYear.")]
        public string Literal;
        [Tooltip("Pixel rectangle, measured from the lower-left of the artwork.")]
        public Rect Rect;
        public float FontSize = 40;
        public Color Color = Color.white;
    }

    [CreateAssetMenu(menuName = "SilverScreen/Reusable Assets/Display Artwork Template")]
    public sealed class DisplayArtworkTemplate : ScriptableObject
    {
        public Texture2D Background;
        [Tooltip("Use a static atlas; composing artwork must not modify shared font assets.")]
        public TMP_FontAsset Font;
        public Shader TextShader;
        public ArtworkTextField[] Fields = Array.Empty<ArtworkTextField>();
    }
}
