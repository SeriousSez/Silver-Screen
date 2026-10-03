using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Recruitment;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Transient manipulation adapter; domain identity and employment stay in their existing owners.</summary>
    public sealed class PersonDragSession
    {
        public Transform Root { get; }
        public EmployeeAgent Employee { get; }
        public CandidateAgent Candidate { get; }
        public bool IsHeld { get; private set; }
        private readonly NavMeshAgent _nav;
        private readonly float _carryLift;
        private HeldPersonPresentation _presentation;
        private readonly Vector3 _original;
        private readonly Quaternion _rotation;
        private readonly bool _updatePosition, _updateRotation;
        private readonly Collider[] _colliders;
        private readonly bool[] _enabled;
        private readonly Collider[] _placementOverlaps = new Collider[64];
        public float PlacementRadius => _nav.radius;
        public bool TryValidatePlacement(Vector3 point, StudioBuildLot lot, out Vector3 ground)
            => TryValidateGround(point, _nav, Root, lot, out ground, _placementOverlaps);
        private PersonDragSession(Transform root, EmployeeAgent employee, CandidateAgent candidate, NavMeshAgent nav)
        {
            Root = root; Employee = employee; Candidate = candidate; _nav = nav;
            var character = root.GetComponent<SilverScreen.Presentation.Characters.CharacterPresentation>();
            _carryLift = character != null && character.IsCanonical ? character.PhysicalProfile.CarryLift : 1.2f;
            _original = root.position; _rotation = root.rotation;
            _updatePosition = nav.updatePosition; _updateRotation = nav.updateRotation;
            _colliders = root.GetComponentsInChildren<Collider>(); _enabled = new bool[_colliders.Length];
        }
        public static PersonDragSession Begin(EmployeeAgent employee, CandidateAgent candidate)
        {
            var root = employee != null ? employee.transform : candidate != null ? candidate.transform : null;
            if (root == null) return null;
            var nav = root.GetComponent<NavMeshAgent>();
            if (nav == null || !nav.isActiveAndEnabled || !nav.isOnNavMesh) return null;
            var session = new PersonDragSession(root, employee, candidate, nav);
            session.IsHeld = true;
            employee?.SetHeld(true); candidate?.SetHeld(true);
            nav.isStopped = true; nav.velocity = Vector3.zero;
            nav.updatePosition = false; nav.updateRotation = false;
            for (int i = 0; i < session._colliders.Length; i++)
            { session._enabled[i] = session._colliders[i].enabled; session._colliders[i].enabled = false; }
            session._presentation = root.GetComponent<HeldPersonPresentation>() ?? root.gameObject.AddComponent<HeldPersonPresentation>();
            session._presentation.BeginHeld();
            return session;
        }
        public void Follow(Vector3 ground) { if (IsHeld && Root != null) Root.position = ground + Vector3.up * (_nav.baseOffset + _carryLift); }
        public bool TryDrop(Vector3 requested, StudioBuildLot lot, Quaternion? facing = null, PersonReleasePresentation presentation = PersonReleasePresentation.Ground)
        {
            if (!IsHeld || Root == null || !TryValidateGround(requested, _nav, Root, lot, out var ground)) return false;
            return Place(ground, facing ?? Root.rotation, presentation);
        }
        public bool TryContextualDrop(Vector3 requested, StudioBuildLot lot, Quaternion facing, System.Func<bool> action)
        {
            if (!IsHeld || Root == null || !TryValidateGround(requested, _nav, Root, lot, out var ground)) return false;
            var heldPosition = Root.position; var heldRotation = Root.rotation;
            var navigationPosition = _nav.nextPosition;
            if (!_nav.isActiveAndEnabled || !_nav.isOnNavMesh || !_nav.Warp(ground)) return false;
            Root.SetPositionAndRotation(ground + Vector3.up * _nav.baseOffset, facing);
            // Domain callbacks see the actual placement (including hiring's replacement view).
            // Failed actions leave the same session held, never silently convert into a ground drop.
            if (!action())
            {
                _nav.Warp(navigationPosition); Root.SetPositionAndRotation(heldPosition, heldRotation);
                return false;
            }
            if (Root != null) { _nav.ResetPath(); Restore(PersonReleasePresentation.Contextual); }
            else IsHeld = false;
            return true;
        }
        public void Cancel()
        {
            if (!IsHeld) return;
            if (Root != null) Place(_original - Vector3.up * _nav.baseOffset, _rotation, PersonReleasePresentation.Cancel);
            if (IsHeld)
            {
                if (Root != null) Root.SetPositionAndRotation(_original, _rotation);
                Restore(PersonReleasePresentation.Cancel);
            }
        }
        private bool Place(Vector3 ground, Quaternion rotation, PersonReleasePresentation presentation)
        {
            // Warp is a placement, never a route to the cursor. Obligations resume on a later agent update.
            if (!_nav.isActiveAndEnabled || !_nav.isOnNavMesh || !_nav.Warp(ground)) return false;
            _nav.ResetPath();
            Root.SetPositionAndRotation(ground + Vector3.up * _nav.baseOffset, rotation);
            Restore(presentation);
            return true;
        }
        private void Restore(PersonReleasePresentation presentation)
        {
            IsHeld = false;
            if (_presentation != null) _presentation.Release(presentation);
            if (_nav != null)
            {
                _nav.updatePosition = _updatePosition; _nav.updateRotation = _updateRotation;
                if (_nav.isOnNavMesh) _nav.isStopped = false;
            }
            for (int i = 0; i < _colliders.Length; i++) if (_colliders[i] != null) _colliders[i].enabled = _enabled[i];
            // A transactional hire may replace the agent while this root/session survives.
            if (Root != null)
            {
                var employee = Root.GetComponent<EmployeeAgent>();
                if (employee != null) employee.SetHeld(false);
                else Root.GetComponent<CandidateAgent>()?.SetHeld(false);
            }
        }
        public static bool TryValidateGround(Vector3 point, NavMeshAgent nav, Transform person, StudioBuildLot lot, out Vector3 ground, Collider[] overlaps = null)
        {
            ground = point;
            if (nav == null || !float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) return false;
            if (lot != null)
            {
                if (!lot.Buildable.Contains(new LotPoint(point.x, point.z))) return false;
                foreach (var forbidden in lot.Forbidden) if (forbidden.Contains(new LotPoint(point.x, point.z))) return false;
            }
            var filter = new NavMeshQueryFilter { agentTypeID = nav.agentTypeID, areaMask = nav.areaMask };
            // At most 15 cm correction, including vertical error. Never select another floor or distant island.
            if (!NavMesh.SamplePosition(point, out var hit, .15f, filter)) return false;
            ground = hit.position;
            float radius = nav.radius;
            var character = person != null ? person.GetComponent<SilverScreen.Presentation.Characters.CharacterPresentation>() : null;
            float clearance = character != null && character.IsCanonical ? character.PhysicalProfile.PlacementGroundClearance : .06f;
            var bottom = ground + Vector3.up * (radius + clearance);
            var top = ground + Vector3.up * Mathf.Max(radius + clearance, nav.height - radius);
            int count;
            if (overlaps == null) { overlaps = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore); count = overlaps.Length; }
            else
            {
                count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
                if (count == overlaps.Length) return false;
            }
            for (int i = 0; i < count; i++)
                if (person == null || !overlaps[i].transform.IsChildOf(person)) return false;
            return true;
        }
    }
}
