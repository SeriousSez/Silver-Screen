using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public sealed class CarryTestAction : MonoBehaviour, IPersonDropAction
    {
        public string Id = "test";
        public bool Available = true;
        public string TargetId => Id;
        public bool CanExecute(PersonDropContext context) => Available;
        public bool TryExecute(PersonDropContext context) => Available;
    }
    public sealed class ContextualCarryTargetTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private Camera _camera;
        private GameObject Make(string name) { var go = new GameObject(name); _objects.Add(go); return go; }
        [SetUp] public void Setup()
        {
            _camera = Make("Target camera").AddComponent<Camera>(); _camera.orthographic = true; _camera.orthographicSize = 5;
            _camera.pixelRect = new Rect(0,0,1000,1000);
            _camera.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.Euler(90,0,0));
        }
        [TearDown] public void Cleanup() { foreach(var go in _objects) Object.DestroyImmediate(go); _objects.Clear(); }
        private ContextualDropTarget Target(string id, FloorTargetShape shape = FloorTargetShape.Zone)
        {
            var go = Make(id); go.transform.position = new Vector3(1000,0,1000);
            var action = go.AddComponent<CarryTestAction>(); action.Id = id;
            var target = go.AddComponent<ContextualDropTarget>(); target.Configure(action,go.transform,null,shape,new Vector2(2,2)); return target;
        }
        private Vector2 Point(float x,float z) => _camera.WorldToScreenPoint(new Vector3(1000+x,0,1000+z));
        [Test] public void ZoneUsesAreaAndBoundedMagnetWithCleanRelease()
        {
            var t=Target("zone");
            Assert.That(t.TryScore(_camera,Point(.9f,.9f),false,out float inside),Is.True); Assert.That(inside,Is.EqualTo(-1));
            Assert.That(t.TryScore(_camera,Point(1.15f,0),false,out _),Is.True);
            Assert.That(t.TryScore(_camera,Point(1.27f,0),false,out _),Is.False);
            Assert.That(t.TryScore(_camera,Point(1.27f,0),true,out _),Is.True);
            Assert.That(t.TryScore(_camera,Point(1.4f,0),true,out _),Is.False);
        }
        [Test] public void RoundMarkerExcludesRectangleCornersAndBehindCamera()
        {
            var t=Target("round",FloorTargetShape.Round);
            Assert.That(t.TryScore(_camera,Point(.9f,.9f),false,out _),Is.False);
            Assert.That(t.TryScore(_camera,Point(.5f,.5f),false,out _),Is.True);
            _camera.transform.rotation=Quaternion.Euler(-90,0,0);
            Assert.That(t.TryScore(_camera,new Vector2(500,500),false,out _),Is.False);
        }
        [Test] public void TiesUseStableIdAndDirectHitsBeatPreviousMargin()
        {
            var b=Target("b"); var a=Target("a"); var targets=new[]{b,a};
            Assert.That(ContextualTargetSelection.Choose(targets,_camera,Point(0,0),b),Is.SameAs(a));
            a.transform.position+=Vector3.right*2.1f;
            Assert.That(ContextualTargetSelection.Choose(targets,_camera,Point(1.2f,0),b),Is.SameAs(a));
            Assert.That(ContextualTargetSelection.Choose(targets,_camera,Point(8,8),a),Is.Null);
        }
        [Test] public void ValidityRevealAndDisableGateTargets()
        {
            var t=Target("gated"); var action=t.GetComponent<CarryTestAction>();
            var cut=Make("building").AddComponent<BuildingCutawayController>(); t.SetInterior(cut);
            Assert.That(t.IsAvailable(default,t.Placement.position),Is.False);
            cut.SetReveal(BuildingRevealReason.HeldPersonInteraction,true,_camera.transform.position);
            Assert.That(t.IsAvailable(default,t.Placement.position),Is.True);
            action.Available=false; Assert.That(t.IsAvailable(default,t.Placement.position),Is.False);
            action.Available=true; action.enabled=false; Assert.That(t.IsAvailable(default,t.Placement.position),Is.False);
            t.enabled=false; Assert.That(t.IsAvailable(default,t.Placement.position),Is.False);
            CollectionAssert.DoesNotContain(ContextualDropTarget.Active,t);
        }
        [Test] public void SolidOccluderPreventsTargetingThroughFurniture()
        {
            var t=Target("blocked"); var wall=Make("obstruction"); wall.transform.position=t.transform.position+Vector3.up*2;
            wall.AddComponent<BoxCollider>().size=new Vector3(3,1,3); Physics.SyncTransforms();
            Assert.That(t.TryScore(_camera,Point(0,0),false,out _),Is.False);
        }
        [Test] public void StudioServicesHasTwoAuthoredFloorZonesAndUnchangedPlacementAnchors()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SilverScreen/Environment/StudioServices/Resources/StudioServices_Live.prefab");
            var targets=prefab.GetComponentsInChildren<ContextualDropTarget>(); Assert.That(targets.Length,Is.EqualTo(2));
            foreach(var t in targets)
            {
                Assert.That(t.Shape,Is.EqualTo(FloorTargetShape.Zone)); Assert.That(t.Size.x*t.Size.y,Is.GreaterThan(3));
                Assert.That(t.Action,Is.Not.Null); Assert.That(t.Placement,Is.SameAs(t.transform));
                Assert.That(t.InteriorVisibility,Is.SameAs(prefab.GetComponent<BuildingCutawayController>()));
            }
        }
    }
}
