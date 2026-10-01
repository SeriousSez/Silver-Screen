using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using System.Collections.Generic;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Authored build rights for the existing single parcel, independent of the 500 m backdrop ground.</summary>
    public sealed class StudioBuildLot : MonoBehaviour
    {
        [SerializeField] private PlacementRect _buildable = new PlacementRect(0, 0, 76, 64);
        [SerializeField] private PlacementRect[] _forbidden = { new PlacementRect(31, 0, 14, 12) };
        [SerializeField] private Vector3 _servicePosition = new Vector3(32.5f, 0, -17);
        [SerializeField] private float _serviceYaw = 90f;
        [SerializeField] private Vector3 _applicantArrival = new Vector3(46, 0, 0);
        [SerializeField] private StudioServicesFacility _startingFacility;
        [SerializeField] private Transform _applicantSpawn;
        [SerializeField] private Collider[] _buildSurfaces;
        private readonly HashSet<Collider> _surfaces = new HashSet<Collider>();
        private readonly Collider[] _overlaps = new Collider[128];

        // Current lot ground is Y=0. Explicit surfaces can be authored for future lots.
        public void ResolveBuildSurfaces()
        {
            _surfaces.Clear();
            if (_buildSurfaces != null && _buildSurfaces.Length > 0)
            {
                foreach (var surface in _buildSurfaces) if (surface != null) _surfaces.Add(surface);
                return;
            }
            // Adapt existing broad ground slabs; roofs and small props cannot confer build rights.
            foreach (var surface in FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                var bounds = surface.bounds;
                if (!surface.isTrigger && surface is BoxCollider &&
                    Mathf.Abs(bounds.max.y) <= .08f && bounds.size.y <= 1.1f &&
                    bounds.size.x >= _buildable.Width * .5f && bounds.size.z >= _buildable.Depth * .5f)
                    _surfaces.Add(surface);
            }
        }
        public bool TryProjectGround(Ray ray, out Vector3 point)
        {
            point = default;
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }
        private bool HasBuildSurface(LotPoint point)
        {
            var ray = new Ray(new Vector3(point.X, .5f, point.Z), Vector3.down);
            foreach (var surface in _surfaces)
                if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy &&
                    surface.Raycast(ray, out var hit, .65f) && hit.normal.y > .98f && Mathf.Abs(hit.point.y) <= .08f) return true;
            return false;
        }
        public string ValidateWorld(BuildingDefinition definition, BuildingPose pose)
        {
            foreach (var area in definition.PlacementAreas)
            {
                // Include edges and sample interiors at <=2 m intervals on this flat lot.
                int nx = Mathf.CeilToInt(area.Width / 2), nz = Mathf.CeilToInt(area.Depth / 2);
                for (int x = 0; x <= nx; x++)
                    for (int z = 0; z <= nz; z++)
                        if (!HasBuildSurface(pose.Transform(new LotPoint(area.X - area.Width / 2 + area.Width * x / nx,
                            area.Z - area.Depth / 2 + area.Depth * z / nz)))) return "Ground is not a valid build surface.";
                var center = pose.Transform(new LotPoint(area.X, area.Z));
                float height = Mathf.Max(2.5f, definition.ConstructionVisuals.BuildingHeight);
                int count = Physics.OverlapBoxNonAlloc(new Vector3(center.X, .15f + height / 2, center.Z),
                    new Vector3(area.Width / 2, height / 2, area.Depth / 2), _overlaps,
                    Quaternion.Euler(0, pose.Yaw, 0), ~0, QueryTriggerInteraction.Ignore);
                if (count == _overlaps.Length) return "Insufficient clearance: dense world geometry.";
                for (int i = 0; i < count; i++)
                {
                    var collider = _overlaps[i];
                    if (_surfaces.Contains(collider) || collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue;
                    return collider.GetComponentInParent<StudioBuildingView>() != null || collider.GetComponentInParent<ConstructionSiteView>() != null
                        ? "Overlaps another building or construction site." : "Insufficient clearance: blocking world geometry.";
                }
            }
            return null;
        }
        public StudioServicesFacility StartingFacility => _startingFacility;
        public string ParcelId => PropertyParcelIds.SilverScreenMainStudio;
        public PlacementRect Buildable => _buildable;
        public float OrientationYaw => transform.eulerAngles.y;
        public PlacementRect[] Forbidden => (PlacementRect[])_forbidden.Clone();
        public Vector3 ServicePosition => _startingFacility != null ? _startingFacility.transform.position : _servicePosition;
        public Quaternion ServiceRotation => _startingFacility != null ? _startingFacility.transform.rotation : Quaternion.Euler(0, _serviceYaw, 0);
        public Vector3 ApplicantArrival => _applicantSpawn != null ? _applicantSpawn.position : _applicantArrival;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(_buildable.X, 0, _buildable.Z), new Vector3(_buildable.Width, .1f, _buildable.Depth));
        }
    }
}
