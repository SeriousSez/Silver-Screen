using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Buildings;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    public sealed class ConstructionSiteView : MonoBehaviour
    {
        [SerializeField] private ConstructionVisualKit _visualKit;
        public PlacedBuilding Building { get; private set; }
        public GameObject FinishedContent { get; private set; }
        public ConstructionDressingPlan Plan { get; private set; }
        public IReadOnlyList<ConstructionActivityPoint> ActivityPoints => _activities;
        public ConstructionPhase AuthoritativePhase { get; private set; }
        public ConstructionPhase PresentedPhase => _previewPhase??Building.Phase;
        public bool IsPreviewing => _previewPhase.HasValue;
        public int DressingGeneration { get; private set; }
        public int VisibleBuildingRenderers => _reveal?.VisibleRendererCount??0;
        public GameObject DressingRoot => _dressing;
        private IReadOnlyList<ConstructionActivityPoint> _activities=Array.Empty<ConstructionActivityPoint>();
        private readonly Dictionary<Behaviour,bool> _behaviours=new Dictionary<Behaviour,bool>();
        private readonly Dictionary<Collider,bool> _colliders=new Dictionary<Collider,bool>();
        private ConstructionPhaseVisuals _reveal;
        private GameObject _dressing, _blocker, _footprint;
        private Material _material;
        private ConstructionPhase? _previewPhase;
        private ConstructionPhase? _lastPresented;
        private bool _initialized, _operational;
        public void Initialize(PlacedBuilding building,GameObject finishedContent,Material material,ConstructionVisualKit kit=null)
        {
            if(_initialized)throw new InvalidOperationException("Construction presentation is initialized once.");
            Building=building;FinishedContent=finishedContent;_material=material;if(kit!=null)_visualKit=kit;
            _reveal=finishedContent.GetComponent<ConstructionPhaseVisuals>()??finishedContent.AddComponent<ConstructionPhaseVisuals>();
            // Keep the actual rendering hierarchy active, but gate gameplay, lights, navigation components
            // and all finished colliders until authoritative completion. No clone of the art is made.
            foreach(var b in finishedContent.GetComponentsInChildren<Behaviour>(true)){
                // Rendering LOD selection must stay live while gameplay is gated, otherwise
                // all three representations render simultaneously during construction.
                if(b is LODGroup)continue;
                _behaviours[b]=b.enabled;b.enabled=false;
            }
            foreach(var c in finishedContent.GetComponentsInChildren<Collider>(true)){_colliders[c]=c.enabled;c.enabled=false;}
            finishedContent.SetActive(true);
            // Awake may initialize authored signage. Reassert the gate without running Start/OnEnable.
            foreach(var b in _behaviours.Keys)if(b!=null)b.enabled=false;
            foreach(var c in _colliders.Keys)if(c!=null)c.enabled=false;
            _reveal.Initialize(finishedContent,building.Definition.ContentId,building.Definition.ConstructionVisuals.BuildingHeight);
            _blocker=new GameObject("ConstructionPublicExclusion");_blocker.transform.SetParent(transform,false);
            var areas=building.Definition.HasCompoundFootprint?building.Definition.PlacementAreas:new[]{building.Definition.Footprint};
            _footprint=new GameObject("MarkedConstructionFootprint");_footprint.transform.SetParent(transform,false);
            foreach(var f in areas){
            var cell=new GameObject("ReservedSiteCell");cell.transform.SetParent(_blocker.transform,false);
            var collider=cell.AddComponent<BoxCollider>();
            // Preserve the previous site's temporary collider dimensions and centre exactly.
            float inset=building.Definition.HasCompoundFootprint?.95f:0;
            collider.center=new Vector3(f.X,1.1f,f.Z);collider.size=new Vector3(Mathf.Max(.1f,f.Width-2*inset),10,Mathf.Max(.1f,f.Depth-2*inset));
            // Carving protects the currently active NavMesh immediately while the authoritative
            // surface update runs asynchronously. The baked site hole then remains safe through
            // completion until the final-building update finishes.
            var obstacle=cell.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=collider.center;obstacle.size=collider.size;
            obstacle.carving=true;obstacle.carveOnlyStationary=true;
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cube);mark.name="ReservedCellMark";mark.transform.SetParent(_footprint.transform,false);
            mark.transform.localPosition=new Vector3(f.X,.1f,f.Z);mark.transform.localScale=new Vector3(f.Width,.2f,f.Depth);
            mark.GetComponent<Renderer>().sharedMaterial=material;mark.GetComponent<Collider>().enabled=false;ConstructionVisualKit.Release(mark.GetComponent<Collider>());
            ConstructionVisualKit.MarkVisualOnly(mark);
            }
            _initialized=true;Refresh();
        }
        public void Refresh()
        {
            if(!_initialized)return;
            bool cancelled=Building.State==BuildingLifecycle.Cancelled;
            bool phaseChanged=AuthoritativePhase!=Building.Phase;
            AuthoritativePhase=Building.Phase;
            if(Plan==null||phaseChanged)
                _activities=ConstructionDressingGenerator.Generate(Building,Building.Phase).Activities;
            if(Building.IsOperational&&!_operational)
            {
                _previewPhase=null;_operational=true;_blocker.SetActive(false);
                foreach(var c in _colliders)if(c.Key!=null)c.Key.enabled=c.Value;
                foreach(var b in _behaviours)if(b.Key!=null&&!(b.Key is MonoBehaviour))b.Key.enabled=b.Value;
                foreach(var b in _behaviours)if(b.Key!=null&&b.Key is MonoBehaviour)b.Key.enabled=b.Value;
                _reveal.Restore();_activities=Array.Empty<ConstructionActivityPoint>();
                ConstructionVisualKit.Release(_blocker);_blocker=null;
                _footprint.SetActive(false);ConstructionVisualKit.Release(_footprint);_footprint=null;
            }
            if(cancelled){ClearDressing();FinishedContent.SetActive(false);_blocker.SetActive(false);_footprint.SetActive(false);return;}
            var phase=PresentedPhase;
            if(_lastPresented==phase)return;
            _lastPresented=phase;
            ClearDressing();Plan=ConstructionDressingGenerator.Generate(Building,phase);DressingGeneration++;
            _reveal.Apply(phase);if(_footprint!=null)_footprint.SetActive(phase<ConstructionPhase.Exterior);
            if(Plan.Modules.Count==0)return;
            _dressing=new GameObject("ReusableConstructionDressing");_dressing.SetActive(false);_dressing.transform.SetParent(transform,false);
            ConstructionVisualKit.MarkVisualOnly(_dressing);
            foreach(var module in Plan.Modules)ConstructionVisualKit.Create(module,_dressing.transform,_material,_visualKit);
            _dressing.SetActive(true);
        }
        /// <summary>Presentation-only developer inspection. Never completes work, changes collision, or grants capabilities.</summary>
        public void PreviewPhase(ConstructionPhase? phase)
        {
            if(Building==null||Building.IsOperational||Building.State==BuildingLifecycle.Cancelled)return;
            _previewPhase=phase;Refresh();
        }
        public Vector3 WorkPoint(int slot)
        {
            var point=_activities.FirstOrDefault(a=>a.Kind==ConstructionDressingGenerator.ActivityFor(Building.Phase)&&a.Slot==slot);
            if(point!=null)return transform.TransformPoint(point.LocalPosition);
            var approach=Building.Definition.ExteriorApproach;return transform.TransformPoint(new Vector3(approach.X+(slot-1)*1.2f,0,approach.Z));
        }
        public bool TryGetActivity(int slot,NavMeshAgent agent,out ConstructionActivityPoint activity,out Vector3 position)
        {
            activity=null;position=default;
            if(agent==null||!agent.isOnNavMesh)return false;
            var desired=ConstructionDressingGenerator.ActivityFor(Building.Phase);
            foreach(var point in _activities.Where(a=>a.Slot==slot).OrderBy(a=>a.Kind==desired?0:a.Kind==ConstructionActivityKind.GroundWork?1:2))
            {
                var target=transform.TransformPoint(point.LocalPosition);
                var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
                if(!NavMesh.SamplePosition(target,out var hit,.25f,filter))continue;
                float radius=agent.radius;
                if(Physics.CheckCapsule(hit.position+Vector3.up*(radius+.06f),hit.position+Vector3.up*(agent.height-radius),
                    radius,~0,QueryTriggerInteraction.Ignore))continue;
                var path=new NavMeshPath();if(!agent.CalculatePath(hit.position,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                activity=point;position=hit.position;return true;
            }
            return false;
        }
        private void ClearDressing()
        {if(_dressing==null)return;_dressing.SetActive(false);ConstructionVisualKit.Release(_dressing);_dressing=null;}
        private void OnDestroy(){ClearDressing();}
    }
}
