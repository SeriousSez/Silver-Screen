using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Buildings;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public enum ConstructionVisualGroup { Foundation, Structure, Walls, Roof, Openings, Fixtures, Details, Signage }
    public enum ConstructionDressingKind { Scaffold, Planks, Ladder, Fence, Materials, Crates, Canvas, Debris }
    [Serializable] public sealed class ConstructionVisualBinding
    {
        public ConstructionVisualGroup Group;
        public Renderer[] Renderers=Array.Empty<Renderer>();
    }
    /// <summary>Reveals the actual final renderers. Explicit Inspector bindings win over content adapters and fallback.</summary>
    public sealed class ConstructionPhaseVisuals : MonoBehaviour
    {
        [SerializeField] private ConstructionVisualBinding[] _groups=Array.Empty<ConstructionVisualBinding>();
        private readonly Dictionary<Renderer,ConstructionVisualGroup> _resolved=new Dictionary<Renderer,ConstructionVisualGroup>();
        private readonly Dictionary<Renderer,bool> _originalHidden=new Dictionary<Renderer,bool>();
        public int VisibleRendererCount => _resolved.Keys.Count(r=>r!=null&&r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy);
        public void Initialize(GameObject content,string contentId,float authoredHeight)
        {
            if(_resolved.Count>0)return;
            foreach(var binding in content.GetComponentsInChildren<ConstructionPhaseVisuals>(true))
                foreach(var group in binding._groups)
                    foreach(var renderer in group.Renderers)
                        if(renderer!=null&&renderer.transform.IsChildOf(content.transform))_resolved[renderer]=group.Group;
            foreach(var renderer in content.GetComponentsInChildren<Renderer>(true))
            {
                _originalHidden[renderer]=renderer.forceRenderingOff;
                if(!_resolved.ContainsKey(renderer))_resolved.Add(renderer,ResolveContentGroup(renderer,content.transform,contentId,authoredHeight));
            }
        }
        public void Apply(ConstructionPhase phase)
        {
            foreach(var pair in _resolved)
            {
                if(pair.Key==null)continue;
                var group=pair.Value;
                var reveal=group==ConstructionVisualGroup.Foundation?ConstructionPhase.Foundation:
                    group==ConstructionVisualGroup.Structure?ConstructionPhase.Structure:
                    group==ConstructionVisualGroup.Walls||group==ConstructionVisualGroup.Roof?ConstructionPhase.Exterior:ConstructionPhase.Finishing;
                pair.Key.forceRenderingOff=_originalHidden[pair.Key]||phase<reveal;
            }
        }
        public void Restore()
        {foreach(var pair in _originalHidden)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;}
        private static ConstructionVisualGroup ResolveContentGroup(Renderer renderer,Transform root,string contentId,float height)
        {
            // These explicit content adapters describe the current exports only. Future architecture
            // can supply Inspector bindings; it is never classified by BuildingType.
            string name=renderer.name;
            if(contentId=="content.casting-office.1930"||contentId=="content.headquarters.1930")
            {
                if(name.EndsWith("_Body",StringComparison.Ordinal))return ConstructionVisualGroup.Structure;
                if(name.EndsWith("_Roof",StringComparison.Ordinal))return ConstructionVisualGroup.Roof;
                if(name.EndsWith("_Doors",StringComparison.Ordinal)||name.EndsWith("_Windows",StringComparison.Ordinal))return ConstructionVisualGroup.Openings;
                if(name.EndsWith("_Trim",StringComparison.Ordinal))return ConstructionVisualGroup.Details;
                return ConstructionVisualGroup.Signage;
            }
            if(contentId=="content.stage1-live.1930")
            {
                switch(name)
                {
                    case "Export_InteriorFloor": return ConstructionVisualGroup.Foundation;
                    case "Export_PrimaryStructure": case "Export_MasonryFraming": case "Export_RiggingPrimary": case "Export_CatwalkStructure": return ConstructionVisualGroup.Structure;
                    case "Export_ExteriorWalls": return ConstructionVisualGroup.Walls;
                    case "Export_Roof": case "Export_RoofSeams": case "Export_RoofGlazing": case "Export_InteriorRoofLining": return ConstructionVisualGroup.Roof;
                    case "Export_MainDoorLeft": case "Export_MainDoorRight": case "Export_PersonnelDoors": case "Export_BlackoutShutters": case "Export_Clerestory": return ConstructionVisualGroup.Openings;
                    case "Export_PermanentLighting": case "Export_PermanentServices": case "Export_AcousticTreatment": return ConstructionVisualGroup.Fixtures;
                    default:return ConstructionVisualGroup.Details;
                }
            }
            // An unsegmented mesh is revealed intact at Exterior; never cut, rescale or duplicate it.
            var bounds=renderer.localBounds;float top=float.MinValue;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                top=Mathf.Max(top,root.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)))).y);
            return top<=.3f?ConstructionVisualGroup.Foundation:top<height*.5f?ConstructionVisualGroup.Structure:ConstructionVisualGroup.Walls;
        }
    }
}
