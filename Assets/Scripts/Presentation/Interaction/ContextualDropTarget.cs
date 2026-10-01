using System;
using System.Collections.Generic;
using SilverScreen.Presentation.Buildings;
using UnityEngine;

namespace SilverScreen.Presentation.Interaction
{
    public enum FloorTargetShape { Zone, Round, Room }
    public enum ContextualRoomState { Hidden, Available, Active, Disabled, Current }
    public enum FloorTargetSymbol { None, Hammer, Leaf, Performance }

    [DisallowMultipleComponent]
    public sealed class ContextualDropTarget : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _action;
        [SerializeField] private Transform _placement;
        [SerializeField] private BuildingCutawayController _interior;
        [SerializeField] private FloorTargetShape _shape;
        [SerializeField] private Vector2 _size = new Vector2(1.8f, 1.8f);
        [SerializeField] private Vector3 _regionOffset;
        [SerializeField] private Rect[] _roomParts = Array.Empty<Rect>();
        [SerializeField] private Vector2 _labelPosition;
        [SerializeField] private float _labelWidth = 2.8f;
        [SerializeField] private FloorTargetSymbol _symbol;
        [SerializeField] private Color _tint = new Color(.62f, .49f, .28f, 1);
        [SerializeField, Min(0)] private float _magnetPixels = 22;
        [SerializeField, Min(0)] private float _releasePixels = 32;
        [SerializeField, Min(0)] private float _maximumWorldReach = .65f;
        [SerializeField, Min(1)] private float _revealDistance = 12;
        private FloorTargetVisual _visual;
        private readonly RaycastHit[] _occlusion = new RaycastHit[32];
        private readonly Vector3[] _outline = new Vector3[32];
        private readonly List<Vector2> _placementSamples = new List<Vector2>();
        private float _sampleClearance = -1;
        private static readonly List<ContextualDropTarget> _active = new List<ContextualDropTarget>();
        public static IReadOnlyList<ContextualDropTarget> Active => _active;
        public IPersonDropAction Action => _action as IPersonDropAction;
        public Transform Placement => _placement != null ? _placement : transform;
        public BuildingCutawayController InteriorVisibility => _interior;
        public FloorTargetShape Shape => _shape;
        public Vector2 Size => _size;
        public IReadOnlyList<Rect> RoomParts => _roomParts;
        public bool IsPresented { get; private set; }
        public bool IsCandidate { get; private set; }
        public string StableId => Action?.TargetId ?? "";

        public void Configure(MonoBehaviour action, Transform placement, BuildingCutawayController interior,
            FloorTargetShape shape, Vector2 size, FloorTargetSymbol symbol = FloorTargetSymbol.None, Vector3 offset = default)
        {
            _action = action; _placement = placement; _interior = interior; _shape = shape;
            _size = new Vector2(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y)); _symbol = symbol; _regionOffset = offset;
            _roomParts = Array.Empty<Rect>();
            _placementSamples.Clear(); _sampleClearance = -1;
            if (_visual != null) { _visual.Dispose(); _visual = null; }
            IsPresented = IsCandidate = false;
        }
        public void SetInterior(BuildingCutawayController interior) => _interior = interior;
        // A bounded union of rectangles is one room/action, with one label and a separate placement anchor.
        public void ConfigureRoom(MonoBehaviour action, Transform placement, BuildingCutawayController interior,
            Rect[] parts, Vector2 labelPosition, float labelWidth)
        {
            if (parts == null || parts.Length == 0 || parts.Length > 8) throw new ArgumentException("A room requires 1–8 floor rectangles.");
            foreach (var part in parts) if (part.width <= 0 || part.height <= 0) throw new ArgumentException("Room rectangles need positive dimensions.");
            Configure(action, placement, interior, FloorTargetShape.Room, parts[0].size);
            _roomParts = (Rect[])parts.Clone(); _labelPosition = labelPosition; _labelWidth = labelWidth;
            _revealDistance = 24;
        }
        public bool IsRelevant(PersonDropContext context) => isActiveAndEnabled && _action != null && _action.isActiveAndEnabled
            && !string.IsNullOrWhiteSpace(StableId) && (Action is IContextualRoomAction room ? room.IsRelevant(context) : Action.CanExecute(context));
        public bool IsVisibleFor(PersonDropContext context, Vector3 pointerFloor)
            => IsRelevant(context) && (_interior == null || _interior.isActiveAndEnabled && _interior.IsRevealed)
                && Vector3.Distance(pointerFloor, Placement.position) <= _revealDistance;
        public bool IsAvailable(PersonDropContext context, Vector3 pointerFloor) => IsVisibleFor(context, pointerFloor) && Action.CanExecute(context);

        public bool TryActivate(PersonDropContext context, PersonDragSession carry, StudioBuildLot lot, Vector3? indicated = null)
        {
            if (!IsAvailable(context, Placement.position)) return false;
            Physics.SyncTransforms(); // Include the preceding employee's literal drop in clearance queries.
            if (!TryResolvePlacement(carry, lot, indicated ?? Placement.position, out var position)) return false;
            bool placed = carry.TryContextualDrop(position, lot, Placement.rotation, () => Action.TryExecute(context));
            if (placed)
            {
                _interior?.HoldAfterInteraction();
                if (Action is IPersonDropCompleted completed) completed.OnPlaced(context, WorldRoomBounds());
            }
            return placed;
        }

        public bool TryPointerFloor(Ray ray, out Vector3 point)
        {
            point = Placement.position;
            if (!new Plane(transform.up,transform.TransformPoint(_regionOffset)).Raycast(ray,out float along) || along < 0) return false;
            point = ray.GetPoint(along); return true;
        }
        public bool TryResolvePlacement(PersonDragSession carry, StudioBuildLot lot, Vector3 preferred, out Vector3 ground)
        {
            ground = Placement.position;
            if (_shape != FloorTargetShape.Room) return carry.TryValidatePlacement(ground,lot,out ground);
            Vector3 validated = default;
            float localRadius = carry.PlacementRadius / Mathf.Max(.01f,Mathf.Min(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z)));
            bool result = TryFindRoomPlacement(preferred,localRadius,point =>
                carry.TryValidatePlacement(point,lot,out validated) && ContainsLocalPoint(transform.InverseTransformPoint(validated)),out _);
            if (result) ground = validated;
            return result;
        }
        private bool ContainsLocalPoint(Vector3 p) => ContainsRoomPoint(new Vector2(p.x,p.z));
        private bool ContainsRoomPoint(Vector2 point)
        { foreach (var rect in _roomParts) if (rect.Contains(point)) return true; return false; }
        private bool FitsRoom(Vector2 point, float clearance)
        {
            if (!ContainsRoomPoint(point)) return false;
            for (int i = 0; i < 8; i++)
            { float angle = i*Mathf.PI/4; if (!ContainsRoomPoint(point + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*clearance)) return false; }
            return true;
        }
        // Deterministic fallback grid, sorted by proximity to the indicated point. No random placement retries.
        public bool TryFindRoomPlacement(Vector3 preferred, float clearance, Func<Vector3,bool> valid, out Vector3 position)
        {
            var local = transform.InverseTransformPoint(preferred); var desired = new Vector2(local.x,local.z);
            position = preferred;
            Vector3 World(Vector2 p) => transform.TransformPoint(new Vector3(p.x,_regionOffset.y,p.y));
            if (FitsRoom(desired,clearance) && valid(World(desired))) { position = World(desired); return true; }
            if (!Mathf.Approximately(_sampleClearance,clearance))
            {
                _sampleClearance = clearance; _placementSamples.Clear();
                var anchor = transform.InverseTransformPoint(Placement.position); _placementSamples.Add(new Vector2(anchor.x,anchor.z));
                float step = Mathf.Clamp(clearance,.2f,.4f);
                foreach (var rect in _roomParts)
                {
                    int nx = Mathf.CeilToInt(rect.width/step), nz = Mathf.CeilToInt(rect.height/step);
                    for (int x = 0; x <= nx; x++) for (int z = 0; z <= nz; z++)
                    {
                        var point = new Vector2(Mathf.Lerp(rect.xMin,rect.xMax,x/(float)nx),Mathf.Lerp(rect.yMin,rect.yMax,z/(float)nz));
                        if (FitsRoom(point,clearance)) _placementSamples.Add(point);
                    }
                    _placementSamples.Add(rect.center);
                }
            }
            _placementSamples.Sort((a,b) => {
                int d = (a-desired).sqrMagnitude.CompareTo((b-desired).sqrMagnitude);
                if (d != 0) return d; int x = a.x.CompareTo(b.x); return x != 0 ? x : a.y.CompareTo(b.y);
            });
            foreach (var candidate in _placementSamples)
                if (FitsRoom(candidate,clearance) && valid(World(candidate))) { position = World(candidate); return true; }
            return false;
        }
        private Bounds WorldRoomBounds()
        {
            int count = Outline(_outline); var bounds = new Bounds(Placement.position,Vector3.zero);
            for (int i = 0; i < count; i++) bounds.Encapsulate(_outline[i]);
            bounds.Expand(new Vector3(.8f,4f,.8f)); return bounds;
        }

        // Score distance to the projected FLOOR region, not the raised carried person or a floating label.
        // Both pixel and world-distance caps prevent remote snapping at extreme zoom/oblique angles.
        public bool TryScore(UnityEngine.Camera camera, Vector2 cursor, bool retain, out float score)
        {
            score = float.PositiveInfinity;
            var ray = camera.ScreenPointToRay(cursor);
            var origin = transform.TransformPoint(_regionOffset);
            if (!new Plane(transform.up, origin).Raycast(ray, out float along) || along < 0) return false;
            int hits = Physics.RaycastNonAlloc(ray, _occlusion, Mathf.Max(0, along - .1f), ~0, QueryTriggerInteraction.Ignore);
            if (hits == _occlusion.Length) return false; // Fail closed in an unusually crowded ray.
            for (int i = 0; i < hits; i++)
            {
                var obstruction = _occlusion[i].collider;
                // A room is a floor-area target even beneath its furniture. Actual placement
                // still requires a clear capsule on navigation, and searches nearby free floor.
                if (_shape == FloorTargetShape.Room && _interior != null && _interior.IsRevealed &&
                    obstruction.transform.IsChildOf(_interior.transform)) continue;
                if (_interior == null || !_interior.IsHiddenOccluder(obstruction)) return false;
            }
            var local = transform.InverseTransformPoint(ray.GetPoint(along)) - _regionOffset;
            var p = new Vector2(local.x, local.z);
            var half = _size * .5f;
            Vector2 closest;
            bool inside;
            if (_shape == FloorTargetShape.Room)
            {
                closest = p; inside = false; float distance = float.PositiveInfinity;
                foreach (var part in _roomParts)
                {
                    var c = new Vector2(Mathf.Clamp(p.x, part.xMin, part.xMax), Mathf.Clamp(p.y, part.yMin, part.yMax));
                    if (c == p) inside = true;
                    float d = (c - p).sqrMagnitude; if (d < distance) { distance = d; closest = c; }
                }
            }
            else if (_shape == FloorTargetShape.Zone)
            {
                closest = new Vector2(Mathf.Clamp(p.x, -half.x, half.x), Mathf.Clamp(p.y, -half.y, half.y));
                inside = closest == p;
            }
            else
            {
                var normalized = new Vector2(p.x / half.x, p.y / half.y);
                inside = normalized.sqrMagnitude <= 1;
                closest = inside ? p : Vector2.Scale(normalized.normalized, half);
            }
            var nearestWorld = transform.TransformPoint(_regionOffset + new Vector3(closest.x, 0, closest.y));
            if (Vector3.Distance(nearestWorld, ray.GetPoint(along)) > _maximumWorldReach) return false;
            int count = Outline(_outline);
            float edge = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var a = camera.WorldToScreenPoint(_outline[i]);
                int next = _shape == FloorTargetShape.Room ? i / 4 * 4 + (i + 1) % 4 : (i + 1) % count;
                var b = camera.WorldToScreenPoint(_outline[next]);
                if (a.z <= camera.nearClipPlane || b.z <= camera.nearClipPlane) return false;
                edge = Mathf.Min(edge, DistanceToSegment(cursor, a, b));
            }
            float pixels = inside ? 0 : edge;
            if (pixels > (retain ? Mathf.Max(_releasePixels, _magnetPixels) : _magnetPixels)) return false;
            // A direct hit always outranks an adjacent magnetic margin. Stable ID resolves ties.
            score = inside ? -1 : pixels / Mathf.Max(1, _magnetPixels);
            return true;
        }
        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var delta = b - a;
            return Vector2.Distance(p, a + delta * Mathf.Clamp01(Vector2.Dot(p - a, delta) / Mathf.Max(.0001f, delta.sqrMagnitude)));
        }
        private int Outline(Vector3[] output)
        {
            if (_shape == FloorTargetShape.Room)
            {
                for (int i = 0; i < _roomParts.Length; i++)
                {
                    var r = _roomParts[i];
                    output[i*4] = transform.TransformPoint(new Vector3(r.xMin, 0, r.yMin));
                    output[i*4+1] = transform.TransformPoint(new Vector3(r.xMax, 0, r.yMin));
                    output[i*4+2] = transform.TransformPoint(new Vector3(r.xMax, 0, r.yMax));
                    output[i*4+3] = transform.TransformPoint(new Vector3(r.xMin, 0, r.yMax));
                }
                return _roomParts.Length * 4;
            }
            int count = _shape == FloorTargetShape.Zone ? 4 : 32;
            for (int i = 0; i < count; i++)
            {
                Vector3 p;
                if (_shape == FloorTargetShape.Zone)
                    p = new Vector3(i == 0 || i == 3 ? -_size.x / 2 : _size.x / 2, 0, i < 2 ? -_size.y / 2 : _size.y / 2);
                else { float a = i * Mathf.PI * 2 / count; p = new Vector3(Mathf.Cos(a) * _size.x / 2, 0, Mathf.Sin(a) * _size.y / 2); }
                output[i] = transform.TransformPoint(_regionOffset + p);
            }
            return count;
        }
        public void Present(bool available, bool candidate)
        {
            PresentState(available ? candidate ? ContextualRoomState.Active : ContextualRoomState.Available : ContextualRoomState.Hidden, "");
        }
        public void Present(PersonDropContext context, bool visible, bool candidate)
        {
            var room = Action as IContextualRoomAction;
            var state = !visible ? ContextualRoomState.Hidden : room?.IsCurrent(context) == true ? ContextualRoomState.Current :
                !Action.CanExecute(context) ? ContextualRoomState.Disabled : candidate ? ContextualRoomState.Active : ContextualRoomState.Available;
            PresentState(state, room?.FloorLabel(context) ?? "");
        }
        private void PresentState(ContextualRoomState state, string label)
        {
            if (state != ContextualRoomState.Hidden && _visual == null)
                _visual = new FloorTargetVisual(transform, _shape, _size, _regionOffset, _symbol, _tint, _roomParts, _labelPosition, _labelWidth);
            _visual?.SetState(state, label);
            IsPresented = state != ContextualRoomState.Hidden; IsCandidate = state == ContextualRoomState.Active;
        }
        private void OnEnable() { if (!_active.Contains(this)) _active.Add(this); }
        private void OnDisable() { Present(false, false); _active.Remove(this); }
        private void OnDestroy() => _visual?.Dispose();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => _active.Clear();
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.8f, .65f, .25f); int count = Outline(_outline);
            for (int i = 0; i < count; i++) Gizmos.DrawLine(_outline[i], _outline[_shape == FloorTargetShape.Room ? i / 4 * 4 + (i + 1) % 4 : (i + 1) % count]);
            Gizmos.DrawWireSphere(Placement.position, .18f);
            Gizmos.DrawLine(Placement.position, Placement.position + Placement.forward * .6f);
        }
    }

    public static class ContextualTargetSelection
    {
        public static ContextualDropTarget Choose(IReadOnlyList<ContextualDropTarget> targets, UnityEngine.Camera camera, Vector2 cursor, ContextualDropTarget previous)
        {
            ContextualDropTarget best = null; float bestScore = float.PositiveInfinity;
            foreach (var target in targets)
                if (target != null && target.TryScore(camera, cursor, target == previous, out float score) &&
                    (score < bestScore || Mathf.Approximately(score, bestScore) && string.CompareOrdinal(target.StableId, best?.StableId) < 0))
                { best = target; bestScore = score; }
            return best;
        }
    }
}
