using System;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Authored local-space infrastructure only; no recruitment or activity simulation.</summary>
    public sealed class StageSchoolInfrastructure : MonoBehaviour
    {
        [Serializable] public sealed class Region
        { public string Id; public Bounds LocalBounds; public string ApplicantContext; public string EmployeeContext; }
        [SerializeField] private Transform[] _anchors = Array.Empty<Transform>();
        [SerializeField] private Region[] _regions = Array.Empty<Region>();
        public System.Collections.Generic.IReadOnlyList<Transform> Anchors => _anchors;
        public System.Collections.Generic.IReadOnlyList<Region> Regions => _regions;
        public Transform Anchor(string id) => Array.Find(_anchors, a => a != null && a.name == id);
        public void Configure(Transform[] anchors, Region[] regions) { _anchors = anchors; _regions = regions; }
    }
}
