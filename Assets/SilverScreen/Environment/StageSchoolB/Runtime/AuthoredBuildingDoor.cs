using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Authored leaf/barrier state shared by every visual LOD and generic portal users.</summary>
    public sealed class AuthoredBuildingDoor : MonoBehaviour
    {
        [SerializeField] private Transform[] _leaves = Array.Empty<Transform>();
        [SerializeField] private Quaternion[] _closed = Array.Empty<Quaternion>();
        [SerializeField] private float _openAngle = 90;
        [SerializeField] private bool _isOpen;
        [SerializeField] private BoxCollider _closedBarrier;
        [SerializeField] private NavMeshObstacle _navigationBarrier;
        public bool IsOpen => _isOpen;
        private static readonly List<AuthoredBuildingDoor> _active=new List<AuthoredBuildingDoor>();
        public static IReadOnlyList<AuthoredBuildingDoor> Active=>_active;
        private void OnEnable(){if(!_active.Contains(this))_active.Add(this);SetOpen(_isOpen);}
        private void OnDisable(){_active.Remove(this);}
        public void Configure(Transform[] leaves, float authoredAngle, float openAngle, BoxCollider barrier, NavMeshObstacle navigation)
        {
            _leaves=leaves; _openAngle=openAngle; _closedBarrier=barrier; _navigationBarrier=navigation;
            _closed=Array.ConvertAll(leaves,t=>t.localRotation*Quaternion.Euler(0,-authoredAngle,0));
            SetOpen(Mathf.Abs(authoredAngle)>1);
        }
        public void SetOpen(bool open)
        {
            _isOpen=open;
            for(int i=0;i<_leaves.Length;i++) if(_leaves[i]!=null)
                _leaves[i].localRotation=_closed[i]*Quaternion.Euler(0,open?_openAngle:0,0);
            if(_closedBarrier!=null)_closedBarrier.enabled=!open;
            if(_navigationBarrier!=null)_navigationBarrier.enabled=!open;
        }
    }
}
