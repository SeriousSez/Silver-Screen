using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor.ReusableAssets
{
    /// <summary>Editor-only source evidence; no runtime assembly depends on it.</summary>
    public sealed class ReusableAssetProvenance : ScriptableObject
    {
        [SerializeField] private string _masterCommit, _sourcePath, _sourceSha256, _canonicalId;
        [SerializeField] private GameObject _prefab;
        [SerializeField, TextArea(5,20)] private string _recordJson;
        public string MasterCommit => _masterCommit;
        public string SourceSha256 => _sourceSha256;
        public string RecordJson => _recordJson;
        public GameObject Prefab => _prefab;
    }
}
