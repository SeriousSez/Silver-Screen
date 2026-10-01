using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Editor.EnvironmentArt;

namespace SilverScreen.Tests.Editor
{
 public sealed class StageSchoolRuntimeTests
 {
  [Test] public void FoundationAppearsAtEveryLodBeforeWallsAndFixtures()
  {
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab));
   try{
    var phases=root.GetComponent<ConstructionPhaseVisuals>();phases.Initialize(root,"content.stage-school.1930",5.95f);phases.Apply(ConstructionPhase.Foundation);
    for(int i=0;i<3;i++){
     var level=root.transform.Find("LOD"+i);
     Assert.That(level.Find("Structure/Foundation").GetComponent<Renderer>().forceRenderingOff,Is.False,"LOD"+i+" foundation");
     var floors=level.Find(i==0?"Interior/Floors":"Floors");
     Assert.That(floors.GetComponentsInChildren<Renderer>().All(r=>!r.forceRenderingOff),Is.True,"LOD"+i+" floors");
     Assert.That(level.Find("Shell/Front").GetComponentsInChildren<Renderer>().All(r=>r.forceRenderingOff),Is.True,"LOD"+i+" walls");
    }
   }finally{Object.DestroyImmediate(root);}
  }
  [Test] public void PlacementCreatesGatedSiteAndPreservesLodSelection()
  {
   var clock=new SilverScreen.Domain.Time.SimulationClock();
   using var work=new SilverScreen.Domain.Work.WorkService(clock,new SilverScreen.Domain.Time.SimulationScheduler(clock));
   var finance=new SilverScreen.Domain.Finance.StudioFinances(clock.CurrentTime);
   using var service=new BuildingConstructionService(StudioBuildingDefinitions.Create(),new PlacementRect(0,0,100,100),null,clock,work,finance,new SilverScreen.Domain.Resources.ResourceReservationBook());
   var cash=finance.CurrentCash;var b=service.Place(StudioBuildingDefinitions.StageSchool,new BuildingPose(0,0,37),out var error);
   Assert.That(error,Is.Null);Assert.That(b.IsOperational,Is.False);Assert.That(finance.CurrentCash,Is.EqualTo(cash-b.Definition.Cost));
   var site=new GameObject("StageSchool test construction");var content=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab),site.transform);
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
   try{var view=site.AddComponent<ConstructionSiteView>();view.Initialize(b,content,mat);
    Assert.That(content.GetComponent<LODGroup>().enabled,Is.True);Assert.That(content.GetComponent<StudioBuildingView>().enabled,Is.False);
    Assert.That(content.GetComponent<BuildingCutawayController>().enabled,Is.False);Assert.That(view.VisibleBuildingRenderers,Is.Zero);
    view.PreviewPhase(ConstructionPhase.Exterior);Assert.That(view.VisibleBuildingRenderers,Is.GreaterThan(0));Assert.That(b.IsOperational,Is.False);
    Assert.That(service.Progress(b),Is.Zero);
    var gap=new PlacementRect(13,-6,2,2);
    foreach(var collider in site.transform.Find("ConstructionPublicExclusion").GetComponentsInChildren<BoxCollider>()){
     var center=site.transform.InverseTransformPoint(collider.transform.TransformPoint(collider.center));
     Assert.That(PlacementRect.Overlaps(new PlacementRect(center.x,center.z,collider.size.x,collider.size.z),default,gap,default),Is.False);
    }
    foreach(var phase in b.Definition.Phases){
     var plan=ConstructionDressingGenerator.Generate(b,phase);
     foreach(var module in plan.Modules){
      Assert.That(PlacementRect.Overlaps(module.GroundBounds,default,gap,default),Is.False,"Dressing enters recess: "+module.Id);
      Assert.That(module.GroundBounds.Corners(default).All(p=>b.Definition.PlacementAreas.Any(a=>a.Contains(p))),Is.True,"Dressing exceeds reserved union: "+module.Id);
     }
    }
   }finally{Object.DestroyImmediate(site);Object.DestroyImmediate(mat);}
  }
  [Test] public void FrontageEndsAtBuildingPocketsWithPassivePathConnection()
  {
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab);
   var connection=root.GetComponent<StageSchoolInfrastructure>().Anchor("path.connection.main");
   Assert.That(connection,Is.Not.Null);
   Assert.That(Vector3.Distance(connection.localPosition,new Vector3(0,.05f,-11.55f)),Is.LessThan(.001f));
   Assert.That(Vector3.Dot(connection.forward,Vector3.back),Is.GreaterThan(.999f));
   foreach(var c in root.transform.Find("Navigation/Physical").GetComponentsInChildren<BoxCollider>())
    Assert.That(c.bounds.min.z,Is.GreaterThanOrEqualTo(-11.551f),c.name+" extends into removed path");
   var d=StudioBuildingDefinitions.Create().Single(x=>x.Id==StudioBuildingDefinitions.StageSchool);
   Assert.That(d.PlacementAreas.Any(a=>a.Contains(new LotPoint(8,-12.6f))),Is.False);
   foreach(var a in root.GetComponent<StageSchoolInfrastructure>().Anchors.Where(a=>a.name.StartsWith("applicant.exterior.")))
    Assert.That(d.PlacementAreas.Any(r=>r.Contains(new LotPoint(a.localPosition.x,a.localPosition.z))),Is.True);
  }
  [Test] public void DefinitionHasOneIdentityAndNoRecruitment()
  {
   var all=StudioBuildingDefinitions.Create();var d=all.Single(x=>x.Id==StudioBuildingDefinitions.StageSchool);
   Assert.That(d.DisplayName,Is.EqualTo("Stage School"));Assert.That(d.Applicants,Is.Empty);
   Assert.That(d.RequiredWork,Is.EqualTo(32));Assert.That(d.Construction.Phases.Sum(p=>p.WorkShare),Is.EqualTo(1));
   Assert.That(d.PlacementAreas.Count,Is.EqualTo(4));Assert.That(d.WorkerCapacity,Is.EqualTo(5));
  }
  [Test] public void RotatedCompoundPlacementStillExcludesArchitecture()
  {
   var d=StudioBuildingDefinitions.Create().Single(x=>x.Id==StudioBuildingDefinitions.StageSchool);var pose=new BuildingPose(45,32,37);
   var center=pose.Transform(new LotPoint(8,8));var other=new PlacementRect(0,0,2,2);
   Assert.That(d.PlacementAreas.Any(a=>PlacementRect.Overlaps(a,pose,other,new BuildingPose(center.X,center.Z,12))),Is.True);
   var gap=pose.Transform(new LotPoint(13.1f,-6));
   Assert.That(d.PlacementAreas.Any(a=>PlacementRect.Overlaps(a,pose,other,new BuildingPose(gap.X,gap.Z,37))),Is.False);
  }
  [Test] public void RuntimeLodsReduceGeometryAndRetainIndependentDoors()
  {
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab);Assert.That(root,Is.Not.Null);
   var lods=root.GetComponent<LODGroup>().GetLODs();Assert.That(lods.Length,Is.EqualTo(3));int previous=int.MaxValue;
   foreach(var l in lods){int tris=l.renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);Assert.That(tris,Is.LessThan(previous));previous=tris;}
   Assert.That(root.GetComponentsInChildren<AuthoredBuildingDoor>().Length,Is.EqualTo(4));
   Assert.That(root.GetComponent<StageSchoolInfrastructure>().Anchors.Count(a=>a.name.StartsWith("applicant.exterior.")),Is.EqualTo(6));
   Assert.That(root.GetComponent<StageSchoolInfrastructure>().Regions.Count,Is.EqualTo(4));
  }
  [Test] public void RevealPersistsAcrossLodsAndRestoresMaterials()
  {
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab));
   try{
    root.transform.rotation=Quaternion.Euler(0,37,0);var c=root.GetComponent<BuildingCutawayController>();var lod=root.GetComponent<LODGroup>();
    var renderers=root.GetComponentsInChildren<Renderer>();var original=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);
    c.SetReveal(BuildingRevealReason.BuildingFocus,true,root.transform.TransformPoint(new Vector3(24,20,-30)));
    for(int i=0;i<3;i++){lod.ForceLOD(i);Assert.That(c.IsRevealed,Is.True);Assert.That(c.Groups.Where(g=>g.Id.StartsWith("Roofs/")).SelectMany(g=>g.Renderers).All(r=>r.forceRenderingOff),Is.True);}
    c.Restore();foreach(var r in renderers){CollectionAssert.AreEqual(original[r],r.sharedMaterials);Assert.That(r.forceRenderingOff,Is.False);}
   }finally{Object.DestroyImmediate(root);}
  }
  [Test] public void DoorStateIsConsistentAcrossLods()
  {
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab));try{
    foreach(var d in root.GetComponentsInChildren<AuthoredBuildingDoor>()){d.SetOpen(false);Assert.That(d.GetComponentInChildren<BoxCollider>().enabled,Is.True);d.SetOpen(true);Assert.That(d.GetComponentInChildren<BoxCollider>().enabled,Is.False);}
   }finally{Object.DestroyImmediate(root);}
  }
  [Test] public void RuntimeAttachmentChainsAndDoorConnectivityAreExplicit()
  {
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab);
   var groups=root.GetComponent<BuildingCutawayController>().Groups.ToDictionary(g=>g.Id);
   foreach(var group in groups.Values){var current=group;var seen=new System.Collections.Generic.HashSet<string>();
    while(!string.IsNullOrEmpty(current.SupportGroupId)){Assert.That(seen.Add(current.Id),Is.True,"Cycle");Assert.That(groups.ContainsKey(current.SupportGroupId),Is.True,current.SupportGroupId);current=groups[current.SupportGroupId];}
   }
   foreach(var g in groups.Values.Where(g=>g.Id.Contains("Pendant")&&!g.Id.EndsWith("/Mount")))Assert.That(g.SupportGroupId,Does.EndWith("/Mount"));
   Assert.That(groups["Furnishings/Audition/Curtains"].SupportGroupId,Is.EqualTo("Audition/CurtainMount"));
   foreach(var d in root.GetComponentsInChildren<AuthoredBuildingDoor>())Assert.That(d.GetComponent<Unity.AI.Navigation.NavMeshLink>(),Is.Not.Null);
  }
 }
}
