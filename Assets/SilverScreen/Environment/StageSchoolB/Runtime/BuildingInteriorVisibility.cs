using System;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Cull only authored deep interiors at distance; retain window-visible rooms and close views.</summary>
    public sealed class BuildingInteriorVisibility : MonoBehaviour
    {
        [SerializeField] private BuildingCutawayController _reveal;
        [SerializeField] private Renderer[] _deepInterior=Array.Empty<Renderer>();
        [SerializeField] private float _distance=32;
        private UnityEngine.Camera _camera;
        private bool _hidden;
        public void Configure(BuildingCutawayController reveal, Renderer[] renderers) { _reveal=reveal; _deepInterior=renderers; }
        private void LateUpdate()
        {
            if(_camera==null)_camera=UnityEngine.Camera.main;
            bool hide=_camera!=null && !_reveal.IsRevealed && (_camera.transform.position-transform.position).sqrMagnitude>_distance*_distance;
            if(hide==_hidden)return;
            _hidden=hide; foreach(var r in _deepInterior)if(r!=null)r.enabled=!hide;
        }
        private void OnDisable(){if(_hidden)foreach(var r in _deepInterior)if(r!=null)r.enabled=true;_hidden=false;}
    }
}
