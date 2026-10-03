using UnityEngine;
using UnityEngine.AI;
using SilverScreen.Presentation.Characters;

namespace SilverScreen.Presentation.Employees
{
    /// <summary>Human-scale prototype body: 0.60m width plus 0.05m clearance per side.
    /// Height and root scale are preserved. Replace this fitting policy when real character meshes arrive.</summary>
    public static class EmployeeNavigationProfile
    {
        public const float BodyDiameter = .60f;
        public const float Radius = .35f;
        public const float Height = 2f;
        private static Mesh _bodyMesh;

        public static Mesh BodyMesh
        {
            get
            {
                if (_bodyMesh != null) return _bodyMesh;
                var temporary = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                temporary.SetActive(false);
                _bodyMesh = Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
                _bodyMesh.name = "EmployeeBody_060x200";
                var vertices = _bodyMesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(vertices[i].x * BodyDiameter, vertices[i].y, vertices[i].z * BodyDiameter);
                _bodyMesh.vertices = vertices;
                _bodyMesh.RecalculateNormals(); _bodyMesh.RecalculateBounds();
                if (Application.isPlaying) Object.Destroy(temporary); else Object.DestroyImmediate(temporary);
                return _bodyMesh;
            }
        }

        public static void Configure(NavMeshAgent nav, float height = Height, float baseOffset = 0)
        {
            nav.radius = Radius; nav.height = height; nav.baseOffset = baseOffset;
            if(nav.GetComponent<SilverScreen.Presentation.Buildings.BuildingDoorTraversal>()==null)
                nav.gameObject.AddComponent<SilverScreen.Presentation.Buildings.BuildingDoorTraversal>();
        }

        public static void Apply(GameObject employee)
        {
            var presentation = employee.GetComponent<CharacterPresentation>();
            if (presentation != null && presentation.IsCanonical)
            { Apply(employee, presentation.PhysicalProfile); return; }
            var nav = employee.GetComponent<NavMeshAgent>();
            if (nav != null) Configure(nav, Height, Height / 2);
            var capsule = employee.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = Radius; capsule.height = Height;
                capsule.center = Vector3.zero; capsule.direction = 1;
            }
            var filter = employee.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null &&
                (filter.sharedMesh.name == "Capsule" || filter.sharedMesh.name == "EmployeeBody_060x200"))
                filter.sharedMesh = BodyMesh;
        }

        public static void Apply(GameObject person, CharacterPhysicalProfile profile)
        {
            if (!profile.TryValidate(out var reason)) throw new System.ArgumentException(reason);
            var nav = person.GetComponent<NavMeshAgent>();
            if (nav != null)
            { Configure(nav, profile.NavigationHeight, profile.BaseOffset); nav.radius = profile.NavigationRadius; }
            var capsule = person.GetComponent<CapsuleCollider>();
            if (capsule != null)
            { capsule.height = profile.CapsuleHeight; capsule.radius = profile.CapsuleRadius; capsule.center = profile.CapsuleCenter; capsule.direction = 1; }
        }
    }
}
