using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Buildings;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    public enum ConstructionModuleType
    {
        ScaffoldVertical, ScaffoldHorizontal, ScaffoldBrace, ScaffoldPlatform, ScaffoldRail,
        Ladder, TemporaryFence, SiteMarker, TimberPile, MaterialPile, Crate, DebrisPile, CanvasOrTarp, WheelbarrowPlaceholder
    }
    public sealed class ConstructionModulePlacement
    {
        public string Id { get; }
        public ConstructionModuleType Type { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Size { get; }
        public PlacementRect GroundBounds { get; }
        public ConstructionModulePlacement(string id, ConstructionModuleType type, Vector3 position, Quaternion rotation, Vector3 size)
        {
            Id=id; Type=type; Position=position; Rotation=rotation; Size=size;
            Vector3 right=rotation*Vector3.right*size.x/2, up=rotation*Vector3.up*size.y/2, forward=rotation*Vector3.forward*size.z/2;
            float x=Mathf.Abs(right.x)+Mathf.Abs(up.x)+Mathf.Abs(forward.x), z=Mathf.Abs(right.z)+Mathf.Abs(up.z)+Mathf.Abs(forward.z);
            GroundBounds=new PlacementRect(position.x,position.z,Mathf.Max(.001f,2*x),Mathf.Max(.001f,2*z));
        }
    }
    public sealed class ConstructionActivityPoint
    {
        public string BuildingId { get; }
        public string Id { get; }
        public ConstructionActivityKind Kind { get; }
        public int Slot { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalTarget { get; }
        public ConstructionActivityPoint(string buildingId, ConstructionActivityKind kind, int slot, Vector3 position, Vector3 target)
        { BuildingId=buildingId; Id=buildingId+":"+kind+":"+slot; Kind=kind; Slot=slot; LocalPosition=position; LocalTarget=target; }
    }
    public sealed class ConstructionDressingPlan
    {
        public IReadOnlyList<ConstructionModulePlacement> Modules { get; }
        public IReadOnlyList<ConstructionActivityPoint> Activities { get; }
        public ConstructionDressingPlan(List<ConstructionModulePlacement> modules, List<ConstructionActivityPoint> activities)
        { Modules=modules.AsReadOnly(); Activities=activities.AsReadOnly(); }
    }

    /// <summary>Deterministic local-space layout. No random state, GameObjects, domain mutations or navigation baking.</summary>
    public static class ConstructionDressingGenerator
    {
        private const float Depth=.62f, BayLength=2.2f, LiftHeight=2f;
        public static ConstructionActivityKind ActivityFor(ConstructionPhase phase) => phase switch
        {
            ConstructionPhase.Foundation => ConstructionActivityKind.FoundationWork,
            ConstructionPhase.Structure => ConstructionActivityKind.ScaffoldWork,
            ConstructionPhase.Exterior => ConstructionActivityKind.WallWork,
            ConstructionPhase.Finishing => ConstructionActivityKind.Inspection,
            _ => ConstructionActivityKind.GroundWork
        };
        public static ConstructionDressingPlan Generate(PlacedBuilding building, ConstructionPhase phase)
        {
            if(building.Definition.HasCompoundFootprint)return Compound(building,phase);
            var modules=new List<ConstructionModulePlacement>(); var activities=Activities(building,phase);
            if(phase==ConstructionPhase.Complete || building.State==BuildingLifecycle.Cancelled)
                return new ConstructionDressingPlan(modules,new List<ConstructionActivityPoint>());
            var d=building.Definition; var profile=d.ConstructionVisuals;
            var exclusions=new List<PlacementRect>(profile.Exclusions) { d.EntranceClearance };
            // An open ground aisle joins the front approach to both scaffold ends.
            // Workers do not walk through posts, braces or a nominally visual fence.
            exclusions.Add(new PlacementRect(d.Footprint.X,d.Footprint.Z-d.Footprint.Depth/2-.59f,d.Footprint.Width+2,.82f));
            if(d.ServiceEntrance.HasValue) {var p=d.ServiceEntrance.Value;exclusions.Add(new PlacementRect(p.X,p.Z,2,3));}
            // Reserve human-sized activity pads in every phase so geometry never appears through a working employee.
            exclusions.AddRange(activities.Select(a=>new PlacementRect(a.LocalPosition.x,a.LocalPosition.z,.82f,.82f)));
            bool Safe(PlacementRect bounds, bool materials=false)
            {
                if(bounds.Corners(default).Any(p=>!d.ConstructionClearance.Contains(p)))return false;
                if(PlacementRect.Overlaps(bounds,default,d.Footprint,default))return false;
                if(exclusions.Any(e=>PlacementRect.Overlaps(bounds,default,e,default)))return false;
                return materials || !profile.MaterialZones.Any(e=>PlacementRect.Overlaps(bounds,default,e,default));
            }
            void Add(string id,ConstructionModuleType type,Vector3 position,Vector3 size,Quaternion rotation,bool materials=false)
            {
                var item=new ConstructionModulePlacement(id,type,position,rotation,size);
                if(Safe(item.GroundBounds,materials))modules.Add(item);
            }
            void Beam(string id,ConstructionModuleType type,Vector3 from,Vector3 to,float thickness)
            { var delta=to-from;Add(id,type,(from+to)/2,new Vector3(delta.magnitude,thickness,thickness),Quaternion.FromToRotation(Vector3.right,delta)); }
            var bounds=d.ConstructionClearance;
            var corners=bounds.Corners(default);
            for(int i=0;i<4;i++)
            {
                var p=corners[i];var toward=new Vector2(bounds.X-p.X,bounds.Z-p.Z).normalized*.2f;
                Add("stake-"+i,ConstructionModuleType.SiteMarker,new Vector3(p.X+toward.x,.35f,p.Z+toward.y),new Vector3(.1f,.7f,.1f),Quaternion.identity);
            }
            if(phase<=ConstructionPhase.Exterior)
                for(int edge=0;edge<4;edge++)
                {
                    var a=new Vector3(corners[edge].X,0,corners[edge].Z);var b=new Vector3(corners[(edge+1)%4].X,0,corners[(edge+1)%4].Z);
                    var inward=(new Vector3(bounds.X,0,bounds.Z)-(a+b)/2).normalized*.17f;
                    var axis=(b-a).normalized;int bays=Mathf.CeilToInt(Vector3.Distance(a,b)/2.4f);float length=Vector3.Distance(a,b)/bays;
                    for(int n=0;n<bays;n++)Add("fence-"+edge+"-"+n,ConstructionModuleType.TemporaryFence,a+axis*((n+.5f)*length)+inward+Vector3.up*.45f,
                        new Vector3(length-.15f,.9f,.07f),Quaternion.FromToRotation(Vector3.right,axis));
                }
            var scaffoldExclusions=exclusions.Concat(profile.MaterialZones).ToArray();
            if(phase>=ConstructionPhase.Foundation)
                for(int s=0;s<profile.Segments.Count;s++)
                {
                    var segment=profile.Segments[s];
                    // Finishing retains only every third authored run, at one lift.
                    if(phase==ConstructionPhase.Finishing && s%3!=0)continue;
                    var a=new Vector3(segment.Start.X,0,segment.Start.Z);var b=new Vector3(segment.End.X,0,segment.End.Z);float total=Vector3.Distance(a,b);
                    if(total<.65f)continue;
                    var axis=(b-a)/total;var normal=Vector3.Cross(axis,Vector3.up);var rotation=Quaternion.FromToRotation(Vector3.right,axis);
                    var intervals=new List<Vector2>{new Vector2(0,total)};
                    foreach(var exclusion in scaffoldExclusions) Cut(intervals,a,axis,total,exclusion,Depth/2+.09f);
                    float height=Mathf.Min(segment.Height,profile.BuildingHeight);
                    int lifts=phase==ConstructionPhase.Foundation||phase==ConstructionPhase.Finishing?1:Mathf.CeilToInt(height/LiftHeight);
                    float lift=Mathf.Min(LiftHeight,height/lifts);int run=0;
                    foreach(var interval in intervals)
                    {
                        float length=interval.y-interval.x-.12f;if(length<.65f)continue;
                        int bays=Mathf.CeilToInt(length/BayLength);float bay=length/bays;var origin=a+axis*(interval.x+.06f);string id="run-"+segment.Id+"-"+run++;
                        for(int p=0;p<=bays;p++)for(int side=-1;side<=1;side+=2)
                        {
                            var foot=origin+axis*(p*bay)+normal*(side*Depth/2);
                            Add(id+"-post-"+p+"-"+side,ConstructionModuleType.ScaffoldVertical,foot+Vector3.up*(lifts*lift+.9f)/2,new Vector3(.10f,lifts*lift+.9f,.10f),rotation);
                        }
                        for(int level=1;level<=lifts;level++)for(int n=0;n<bays;n++)
                        {
                            string key=id+"-"+level+"-"+n;var from=origin+axis*(n*bay)+Vector3.up*(level*lift);var to=from+axis*bay;
                            Add(key+"-platform",ConstructionModuleType.ScaffoldPlatform,(from+to)/2,new Vector3(bay+.06f,.09f,Depth),rotation);
                            for(int side=-1;side<=1;side+=2)
                            {
                                var offset=normal*(side*Depth/2);
                                Beam(key+"-beam-"+side,ConstructionModuleType.ScaffoldHorizontal,from+offset,to+offset,.08f);
                                Beam(key+"-rail-"+side,ConstructionModuleType.ScaffoldRail,from+offset+Vector3.up*.8f,to+offset+Vector3.up*.8f,.065f);
                            }
                            Beam(key+"-brace",ConstructionModuleType.ScaffoldBrace,from+normal*(Depth/2)-Vector3.up*lift,to+normal*(Depth/2),.065f);
                        }
                        Add(id+"-ladder",ConstructionModuleType.Ladder,origin+axis*(bay*.5f)+Vector3.up*(lift/2),new Vector3(.42f,lift,.12f),rotation);
                    }
                }
            for(int i=0;i<profile.MaterialZones.Count;i++)
            {
                if(phase==ConstructionPhase.Finishing && i>0)continue;
                var zone=profile.MaterialZones[i];float w=Mathf.Min(.55f,zone.Width*.85f),z=Mathf.Min(.5f,zone.Depth*.4f);
                var type=i%2==0?ConstructionModuleType.TimberPile:ConstructionModuleType.MaterialPile;
                Add("materials-"+i,type,new Vector3(zone.X,.24f,zone.Z-z*.6f),new Vector3(w,.48f,z),Quaternion.identity,true);
                if(phase!=ConstructionPhase.Finishing)
                {
                    if(phase>=ConstructionPhase.Foundation)Add("crate-"+i,ConstructionModuleType.Crate,new Vector3(zone.X,.23f,zone.Z+z*.65f),new Vector3(w,.46f,z),Quaternion.identity,true);
                    if(phase>=ConstructionPhase.Structure)Add("canvas-"+i,ConstructionModuleType.CanvasOrTarp,new Vector3(zone.X,.53f,zone.Z),new Vector3(w,.045f,z*1.8f),Quaternion.identity,true);
                }
                if(phase==ConstructionPhase.SitePreparation || phase==ConstructionPhase.Finishing)
                    Add("equipment-"+i,phase==ConstructionPhase.Finishing?ConstructionModuleType.DebrisPile:ConstructionModuleType.WheelbarrowPlaceholder,
                        new Vector3(zone.X,.22f,zone.Z+z*.65f),new Vector3(w,.44f,z),Quaternion.identity,true);
            }
            return new ConstructionDressingPlan(modules,activities);
        }
        // Use the same reserved union as placement. Boundary runs are split at every
        // rectangle coordinate, so no fence bridges a legal neighbouring recess.
        private static ConstructionDressingPlan Compound(PlacedBuilding building,ConstructionPhase phase)
        {
            var modules=new List<ConstructionModulePlacement>();var activities=new List<ConstructionActivityPoint>();
            if(phase==ConstructionPhase.Complete||building.State==BuildingLifecycle.Cancelled)return new ConstructionDressingPlan(modules,activities);
            var d=building.Definition;var cells=d.PlacementAreas;
            var xs=cells.SelectMany(r=>new[]{r.X-r.Width/2,r.X+r.Width/2}).Distinct().OrderBy(x=>x).ToArray();
            var zs=cells.SelectMany(r=>new[]{r.Z-r.Depth/2,r.Z+r.Depth/2}).Distinct().OrderBy(z=>z).ToArray();
            bool Inside(float x,float z)=>cells.Any(r=>r.Contains(new LotPoint(x,z)));
            int id=0;
            void Edge(Vector3 a,Vector3 b,Vector3 inward)
            {
                float length=Vector3.Distance(a,b);var axis=(b-a).normalized;
                if(length<.2f)return; // coincident rectangle edges can differ by float rounding
                int count=Mathf.Max(1,Mathf.CeilToInt(length/2.2f));float bay=length/count;
                for(int n=0;n<count;n++){
                    var p=a+axis*((n+.5f)*bay)+inward*.06f;
                    bool entrance=d.EntranceClearance.Contains(new LotPoint(p.x,p.z))||d.ServiceEntrance.HasValue&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(d.ServiceEntrance.Value.X,d.ServiceEntrance.Value.Z))<2;
                    if(!entrance&&phase<=ConstructionPhase.Exterior)modules.Add(new ConstructionModulePlacement("compound-fence-"+id++,ConstructionModuleType.TemporaryFence,p+Vector3.up*.45f,Quaternion.FromToRotation(Vector3.right,axis),new Vector3(Mathf.Max(.05f,bay-.1f),.9f,.07f)));
                    var work=a+axis*((n+.5f)*bay)+inward*.52f;
                    var pad=new PlacementRect(work.x,work.z,.82f,.82f);
                    if(activities.Count<d.Construction.CapacityFor(phase)&&bay>.9f&&pad.Corners(default).All(q=>Inside(q.X,q.Z))&&!entrance&&activities.All(q=>Vector3.Distance(q.LocalPosition,work)>1.2f))
                        activities.Add(new ConstructionActivityPoint(building.Id,ActivityFor(phase),activities.Count,work,work+inward+Vector3.up*1.2f));
                }
            }
            for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++){
                float mx=(xs[x]+xs[x+1])/2,mz=(zs[z]+zs[z+1])/2;if(!Inside(mx,mz))continue;
                if(!Inside(mx,zs[z]-.001f))Edge(new Vector3(xs[x],0,zs[z]),new Vector3(xs[x+1],0,zs[z]),Vector3.forward);
                if(!Inside(mx,zs[z+1]+.001f))Edge(new Vector3(xs[x],0,zs[z+1]),new Vector3(xs[x+1],0,zs[z+1]),Vector3.back);
                if(!Inside(xs[x]-.001f,mz))Edge(new Vector3(xs[x],0,zs[z]),new Vector3(xs[x],0,zs[z+1]),Vector3.right);
                if(!Inside(xs[x+1]+.001f,mz))Edge(new Vector3(xs[x+1],0,zs[z]),new Vector3(xs[x+1],0,zs[z+1]),Vector3.left);
            }
            // Use the same authored scaffold/material vocabulary as rectangular sites,
            // clipped to the reserved union. In particular, never bridge a concave gap.
            bool Safe(PlacementRect bounds)
            {
                var cutsX=xs.Where(x=>x>bounds.X-bounds.Width/2&&x<bounds.X+bounds.Width/2)
                    .Concat(new[]{bounds.X-bounds.Width/2,bounds.X+bounds.Width/2}).OrderBy(x=>x).ToArray();
                var cutsZ=zs.Where(z=>z>bounds.Z-bounds.Depth/2&&z<bounds.Z+bounds.Depth/2)
                    .Concat(new[]{bounds.Z-bounds.Depth/2,bounds.Z+bounds.Depth/2}).OrderBy(z=>z).ToArray();
                for(int x=0;x<cutsX.Length-1;x++)for(int z=0;z<cutsZ.Length-1;z++)
                    if(!Inside((cutsX[x]+cutsX[x+1])/2,(cutsZ[z]+cutsZ[z+1])/2))return false;
                if(PlacementRect.Overlaps(bounds,default,d.Footprint,default)||PlacementRect.Overlaps(bounds,default,d.EntranceClearance,default))return false;
                if(d.ConstructionVisuals.Exclusions.Any(e=>PlacementRect.Overlaps(bounds,default,e,default)))return false;
                return activities.All(a=>!PlacementRect.Overlaps(bounds,default,new PlacementRect(a.LocalPosition.x,a.LocalPosition.z,.82f,.82f),default));
            }
            void Add(string key,ConstructionModuleType type,Vector3 p,Vector3 size,Quaternion rotation)
            {
                var m=new ConstructionModulePlacement(key,type,p,rotation,size);
                if(Safe(m.GroundBounds))modules.Add(m);
            }
            if(phase>=ConstructionPhase.Foundation)
            foreach(var segment in d.ConstructionVisuals.Segments){
                var a=new Vector3(segment.Start.X,0,segment.Start.Z);var b=new Vector3(segment.End.X,0,segment.End.Z);
                var axis=(b-a).normalized;var normal=Vector3.Cross(axis,Vector3.up);float length=Vector3.Distance(a,b);
                int bays=Mathf.Max(1,Mathf.CeilToInt(length/BayLength));float bay=length/bays;
                float height=Mathf.Min(segment.Height,d.ConstructionVisuals.BuildingHeight);
                int lifts=phase==ConstructionPhase.Foundation||phase==ConstructionPhase.Finishing?1:Mathf.CeilToInt(height/LiftHeight);
                float lift=Mathf.Min(LiftHeight,height/lifts);var rotation=Quaternion.FromToRotation(Vector3.right,axis);
                for(int n=0;n<bays;n++){
                    if(phase==ConstructionPhase.Finishing&&n%3!=0)continue;
                    var center=a+axis*((n+.5f)*bay);string key="compound-scaffold-"+segment.Id+"-"+n;
                    var envelope=new ConstructionModulePlacement(key,ConstructionModuleType.ScaffoldPlatform,center,rotation,new Vector3(bay,.1f,Depth));
                    if(!Safe(envelope.GroundBounds)||d.ConstructionVisuals.MaterialZones.Any(z=>PlacementRect.Overlaps(envelope.GroundBounds,default,z,default)))continue;
                    for(int end=-1;end<=1;end+=2)for(int side=-1;side<=1;side+=2)
                        Add(key+"-post-"+end+"-"+side,ConstructionModuleType.ScaffoldVertical,center+axis*(end*(bay/2-.06f))+normal*(side*(Depth/2-.06f))+Vector3.up*(lifts*lift+.85f)/2,new Vector3(.08f,lifts*lift+.85f,.08f),rotation);
                    for(int l=1;l<=lifts;l++){
                        Add(key+"-platform-"+l,ConstructionModuleType.ScaffoldPlatform,center+Vector3.up*(l*lift),new Vector3(bay-.08f,.09f,Depth),rotation);
                        for(int side=-1;side<=1;side+=2)Add(key+"-rail-"+l+"-"+side,ConstructionModuleType.ScaffoldRail,center+Vector3.up*(l*lift+.8f)+normal*(side*(Depth/2-.04f)),new Vector3(bay-.08f,.06f,.06f),rotation);
                    }
                }
            }
            for(int i=0;i<d.ConstructionVisuals.MaterialZones.Count;i++){
                var z=d.ConstructionVisuals.MaterialZones[i];
                Add("compound-material-"+i,i%2==0?ConstructionModuleType.TimberPile:ConstructionModuleType.MaterialPile,new Vector3(z.X,.24f,z.Z),new Vector3(z.Width*.8f,.48f,z.Depth*.8f),Quaternion.identity);
            }
            // A fence's centre can miss an entrance exclusion while its end overlaps it.
            modules.RemoveAll(m=>!Safe(m.GroundBounds));
            return new ConstructionDressingPlan(modules,activities);
        }
        private static List<ConstructionActivityPoint> Activities(PlacedBuilding b,ConstructionPhase phase)
        {
            var list=new List<ConstructionActivityPoint>();
            if(phase==ConstructionPhase.Complete||b.State==BuildingLifecycle.Cancelled)return list;
            var d=b.Definition;var f=d.Footprint;int capacity=d.Construction.CapacityFor(phase);
            var kind=ActivityFor(phase);
            float phaseOffset=phase switch
            {
                ConstructionPhase.SitePreparation=>.04f,
                ConstructionPhase.Foundation=>.14f,
                ConstructionPhase.Structure=>.24f,
                ConstructionPhase.Exterior=>.34f,
                _=>.44f
            };
            float halfWidth=f.Width/2+.59f,halfDepth=f.Depth/2+.59f;
            float horizontal=halfWidth*2,vertical=halfDepth*2,perimeter=2*(horizontal+vertical);
            for(int candidate=0;candidate<capacity*4&&list.Count<capacity;candidate++)
            {
                float distance=Mathf.Repeat(phaseOffset+candidate*.618034f,1f)*perimeter;
                Vector3 position,target;
                if(distance<horizontal)
                {
                    float x=f.X-halfWidth+distance;position=new Vector3(x,0,f.Z-halfDepth);
                    target=new Vector3(Mathf.Clamp(x,f.X-f.Width/2,f.X+f.Width/2),1.2f,f.Z-f.Depth/2);
                }
                else if((distance-=horizontal)<vertical)
                {
                    float z=f.Z-halfDepth+distance;position=new Vector3(f.X+halfWidth,0,z);
                    target=new Vector3(f.X+f.Width/2,1.2f,Mathf.Clamp(z,f.Z-f.Depth/2,f.Z+f.Depth/2));
                }
                else if((distance-=vertical)<horizontal)
                {
                    float x=f.X+halfWidth-distance;position=new Vector3(x,0,f.Z+halfDepth);
                    target=new Vector3(Mathf.Clamp(x,f.X-f.Width/2,f.X+f.Width/2),1.2f,f.Z+f.Depth/2);
                }
                else
                {
                    distance-=horizontal;float z=f.Z+halfDepth-distance;position=new Vector3(f.X-halfWidth,0,z);
                    target=new Vector3(f.X-f.Width/2,1.2f,Mathf.Clamp(z,f.Z-f.Depth/2,f.Z+f.Depth/2));
                }
                var pad=new PlacementRect(position.x,position.z,.82f,.82f);
                bool reserved=pad.Corners(default).All(p=>d.ConstructionClearance.Contains(p)||d.EntranceClearance.Contains(p));
                if(reserved&&!PlacementRect.Overlaps(pad,default,f,default)&&!d.ConstructionVisuals.Exclusions.Any(e=>PlacementRect.Overlaps(pad,default,e,default)))
                    list.Add(new ConstructionActivityPoint(b.Id,kind,list.Count,position,target));
            }
            return list;
        }
        private static void Cut(List<Vector2> intervals,Vector3 start,Vector3 axis,float length,PlacementRect rect,float padding)
        {
            float enter=0,exit=length;
            bool Slab(float origin,float direction,float low,float high)
            {
                if(Mathf.Abs(direction)<.00001f)return origin>=low&&origin<=high;
                float a=(low-origin)/direction,b=(high-origin)/direction;if(a>b){float swap=a;a=b;b=swap;}
                enter=Mathf.Max(enter,a);exit=Mathf.Min(exit,b);return enter<=exit;
            }
            if(!Slab(start.x,axis.x,rect.X-rect.Width/2-padding,rect.X+rect.Width/2+padding)||
               !Slab(start.z,axis.z,rect.Z-rect.Depth/2-padding,rect.Z+rect.Depth/2+padding))return;
            for(int i=intervals.Count-1;i>=0;i--)
            {
                var span=intervals[i];if(exit<=span.x||enter>=span.y)continue;intervals.RemoveAt(i);
                if(exit<span.y)intervals.Insert(i,new Vector2(exit,span.y));if(enter>span.x)intervals.Insert(i,new Vector2(span.x,enter));
            }
        }
    }
}
