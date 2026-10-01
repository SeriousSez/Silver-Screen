using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Domain.Work;
using SilverScreen.Presentation.Buildings;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ConstructionDressingTests
    {
        private SimulationClock _clock;
        private WorkService _work;
        private BuildingConstructionService _service;
        private readonly List<Object> _owned=new List<Object>();
        [SetUp] public void Setup()
        {
            _clock=new SimulationClock();_work=new WorkService(_clock,new SimulationScheduler(_clock));
            _service=new BuildingConstructionService(StudioBuildingDefinitions.Create(),new PlacementRect(0,0,300,300),null,_clock,_work,new StudioFinances(_clock.CurrentTime),new ResourceReservationBook());
        }
        [TearDown] public void Cleanup(){foreach(var item in _owned)if(item!=null)Object.DestroyImmediate(item);_owned.Clear();_service.Dispose();_work.Dispose();}
        private PlacedBuilding Place(string id,BuildingPose pose=default)=>_service.Place(id,pose,out _);
        private ConstructionSiteView View(PlacedBuilding b)
        {
            var root=new GameObject("DressingTest");_owned.Add(root);root.transform.SetPositionAndRotation(new Vector3(b.Pose.X,0,b.Pose.Z),Quaternion.Euler(0,b.Pose.Yaw,0));
            var content=GameObject.CreatePrimitive(PrimitiveType.Cube);content.name="Casting_Body";content.transform.SetParent(root.transform,false);content.AddComponent<StudioBuildingView>();content.SetActive(false);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));_owned.Add(material);
            var view=root.AddComponent<ConstructionSiteView>();view.Initialize(b,content,material);return view;
        }
        [Test] public void OneLocalPlanFollowsArbitraryInstancesWithoutWorldCoordinateAssumptions()
        {
            var first=Place(StarterFeatureIds.Casting,new BuildingPose(-30,-10,27));var second=Place(StarterFeatureIds.Casting,new BuildingPose(30,10,-63));
            var a=ConstructionDressingGenerator.Generate(first,ConstructionPhase.Structure);var b=ConstructionDressingGenerator.Generate(second,ConstructionPhase.Structure);
            CollectionAssert.AreEqual(a.Modules.Select(m=>m.Position),b.Modules.Select(m=>m.Position));
            var view=View(first);view.PreviewPhase(ConstructionPhase.Structure);
            Assert.That(view.Plan.Modules.Count,Is.GreaterThan(0));
            var module=view.Plan.Modules.First();var actual=view.DressingRoot.transform.Find(module.Id+" ["+module.Type+"]");
            var expected=first.Pose.Transform(new LotPoint(module.Position.x,module.Position.z));
            Assert.That(actual.position.x,Is.EqualTo(expected.X).Within(.0001));Assert.That(actual.position.z,Is.EqualTo(expected.Z).Within(.0001));
            Assert.That(view.ActivityPoints.All(p=>p.BuildingId==first.Id),Is.True);
            Assert.That(a.Activities.Select(p=>p.Id).Intersect(b.Activities.Select(p=>p.Id)),Is.Empty);
        }
        [Test] public void AllStarterArchitecturesKeepModulesInsideReservationsAndOutsideAccessPads()
        {
            int index=0;int casting=0,stage=0;
            foreach(var definition in _service.Definitions)
            {
                var placed=Place(definition.Id,new BuildingPose(index++*45-45,0,31));
                var plan=ConstructionDressingGenerator.Generate(placed,ConstructionPhase.Structure);
                Assert.That(plan.Modules.Any(m=>m.Type==ConstructionModuleType.ScaffoldPlatform),Is.True,definition.Id);
                foreach(var module in plan.Modules)
                {
                    Assert.That(module.GroundBounds.Corners(default).All(p=>definition.ConstructionClearance.Contains(p)),Is.True,module.Id);
                    Assert.That(PlacementRect.Overlaps(module.GroundBounds,default,definition.EntranceClearance,default),Is.False,module.Id);
                    Assert.That(PlacementRect.Overlaps(module.GroundBounds,default,definition.Footprint,default),Is.False,module.Id);
                    foreach(var point in plan.Activities)
                        Assert.That(PlacementRect.Overlaps(module.GroundBounds,default,new PlacementRect(point.LocalPosition.x,point.LocalPosition.z,.82f,.82f),default),Is.False,module.Id);
                }
                if(definition.Id==StarterFeatureIds.Casting)casting=plan.Modules.Count;
                if(definition.Id==StarterFeatureIds.Stage)stage=plan.Modules.Count;
            }
            Assert.That(stage,Is.GreaterThan(casting),"Different authored dimensions produce a different layout.");
        }
        [Test] public void PhasesReduceDressingDeterministicallyAndCompleteHasNone()
        {
            var b=Place(StarterFeatureIds.Stage);
            var prep=ConstructionDressingGenerator.Generate(b,ConstructionPhase.SitePreparation);
            var foundation=ConstructionDressingGenerator.Generate(b,ConstructionPhase.Foundation);
            var structure=ConstructionDressingGenerator.Generate(b,ConstructionPhase.Structure);
            var finishing=ConstructionDressingGenerator.Generate(b,ConstructionPhase.Finishing);
            Assert.That(prep.Modules.Any(m=>m.Type==ConstructionModuleType.ScaffoldPlatform),Is.False);
            Assert.That(structure.Modules.Count,Is.GreaterThan(foundation.Modules.Count));
            Assert.That(finishing.Modules.Count,Is.LessThan(structure.Modules.Count));
            var again=ConstructionDressingGenerator.Generate(b,ConstructionPhase.Finishing);
            CollectionAssert.AreEqual(finishing.Modules.Select(m=>m.Id+" "+m.Position+" "+m.Size),again.Modules.Select(m=>m.Id+" "+m.Position+" "+m.Size));
            Assert.That(ConstructionDressingGenerator.Generate(b,ConstructionPhase.Complete).Modules,Is.Empty);
        }
        [Test] public void PreviewRevealsRealModelWithoutUnlockingGameplayAndCompletionRestoresIt()
        {
            var b=Place(StarterFeatureIds.Casting);var view=View(b);var content=view.FinishedContent;var original=content.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(view.VisibleBuildingRenderers,Is.Zero);
            view.PreviewPhase(ConstructionPhase.Structure);
            Assert.That(view.VisibleBuildingRenderers,Is.EqualTo(1));Assert.That(content.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(original));
            Assert.That(content.GetComponent<Collider>().enabled,Is.False);Assert.That(content.GetComponent<StudioBuildingView>().enabled,Is.False);
            int generation=view.DressingGeneration;view.Refresh();Assert.That(view.DressingGeneration,Is.EqualTo(generation));
            view.PreviewPhase(ConstructionPhase.Complete);Assert.That(view.DressingRoot,Is.Null);Assert.That(b.State,Is.EqualTo(BuildingLifecycle.Planned));Assert.That(_service.Progress(b),Is.Zero);
            view.PreviewPhase(null);_service.Changed+=_=>view.Refresh();
            _service.AssignWorker(b,new Employee("builder","Builder",EmployeeRole.ConstructionWorker,50,100,80));_service.WorkerArrived("builder");_clock.Advance(1200d);
            Assert.That(b.IsOperational,Is.True);Assert.That(view.DressingRoot,Is.Null);
            Assert.That(content.GetComponent<Collider>().enabled,Is.True);Assert.That(content.GetComponent<StudioBuildingView>().enabled,Is.True);
            Assert.That(content.GetComponent<Renderer>().forceRenderingOff,Is.False);
            Assert.That(view.transform.Find("ConstructionPublicExclusion"),Is.Null);
        }
        [Test] public void PhaseReplacementCancellationAndOwnerRemovalCleanUpGeneratedObjects()
        {
            var b=Place(StarterFeatureIds.Casting);var view=View(b);var old=view.DressingRoot;
            view.PreviewPhase(ConstructionPhase.Structure);Assert.That(old==null,Is.True);
            var current=view.DressingRoot;Assert.That(current,Is.Not.Null);
            _service.Cancel(b);view.Refresh();Assert.That(current==null,Is.True);Assert.That(view.DressingRoot,Is.Null);
            var root=view.gameObject;Object.DestroyImmediate(root);Assert.That(view==null,Is.True);
        }
    }
}
