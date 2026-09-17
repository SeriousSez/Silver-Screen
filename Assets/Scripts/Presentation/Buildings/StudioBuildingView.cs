using UnityEngine;
using SilverScreen.Domain;

namespace SilverScreen.Presentation.Buildings
{
    [SelectionBase]
    public class StudioBuildingView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BuildingType _buildingType;
        [SerializeField] private string _displayName = "Building";
        [SerializeField] private Transform _entrancePoint;

        public BuildingType BuildingType => _buildingType;
        public string DisplayName => _displayName;
        public Transform EntrancePoint => _entrancePoint != null ? _entrancePoint : transform;

        public Vector3 InteractionPosition => EntrancePoint.position;

        public void Initialize(BuildingType type, string displayName, Transform entrancePoint)
        {
            _buildingType = type;
            _displayName = displayName;
            _entrancePoint = entrancePoint;
        }

        private void OnDrawGizmosSelected()
        {
            if (_entrancePoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_entrancePoint.position, 0.4f);
                Gizmos.DrawLine(transform.position, _entrancePoint.position);
            }
        }
    }
}
