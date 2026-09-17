using UnityEngine;

namespace SilverScreen.Domain.Movie
{
    [CreateAssetMenu(fileName = "NewGenre", menuName = "Silver Screen/Genre Definition")]
    public class GenreDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;

        public void Initialize(string id, string displayName, string description = "")
        {
            _id = id;
            _displayName = displayName;
            _description = description;
        }
    }
}
