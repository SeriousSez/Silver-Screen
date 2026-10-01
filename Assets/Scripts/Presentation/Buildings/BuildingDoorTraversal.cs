using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Generic pedestrian use of authored door portals. Navigation retains
    /// closed-door connectivity via a link; the physical barrier opens before crossing.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BuildingDoorTraversal : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private bool _crossing;
        private bool _previousAutomatic;
        private bool _delegating;
        private void OnEnable(){_agent=GetComponent<NavMeshAgent>();_previousAutomatic=_agent.autoTraverseOffMeshLink;_agent.autoTraverseOffMeshLink=false;}
        private void OnDisable(){if(_agent!=null)_agent.autoTraverseOffMeshLink=_previousAutomatic;_crossing=false;}
        private void Update()
        {
            if(!_agent.isActiveAndEnabled||!_agent.isOnNavMesh||_agent.isStopped)return;
            foreach(var door in AuthoredBuildingDoor.Active){
                if(door.IsOpen)continue;
                var local=door.transform.InverseTransformPoint(transform.position);
                if(_agent.hasPath&&Mathf.Abs(local.x)<1.4f&&Mathf.Abs(local.z)<1.8f&&Mathf.Abs(local.y)<2.5f)door.SetOpen(true);
            }
            if(!_agent.isOnOffMeshLink){_crossing=false;if(_delegating){_agent.autoTraverseOffMeshLink=false;_delegating=false;}return;}
            var link=_agent.currentOffMeshLinkData;
            // Links other than authored doors retain their engine-default traversal.
            bool doorPortal=false;
            foreach(var door in AuthoredBuildingDoor.Active){var local=door.transform.InverseTransformPoint((link.startPos+link.endPos)*.5f);if(Mathf.Abs(local.x)<1.5f&&Mathf.Abs(local.z)<.5f&&Mathf.Abs(local.y)<.5f){doorPortal=true;door.SetOpen(true);break;}}
            if(!doorPortal){_agent.autoTraverseOffMeshLink=_previousAutomatic;_delegating=true;return;}
            var target=link.endPos+Vector3.up*_agent.baseOffset;
            if(!_crossing){_crossing=true;return;} // barrier/obstacle update before passage
            transform.position=Vector3.MoveTowards(transform.position,target,_agent.speed*Time.deltaTime);
            _agent.nextPosition=transform.position;
            if(Vector3.Distance(transform.position,target)<.025f){_agent.CompleteOffMeshLink();_crossing=false;}
        }
    }
}
