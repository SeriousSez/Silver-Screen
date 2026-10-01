using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Local arrival-based right of way for an authored personnel passage.
    /// Normal NavMesh steering and avoidance remain responsible for all movement.</summary>
    [DisallowMultipleComponent]
    public sealed class PersonnelEntranceYield : MonoBehaviour
    {
        [SerializeField] private Transform _exterior;
        [SerializeField] private Transform _interior;
        [SerializeField] private float _halfWidth = .55f;
        private readonly List<NavMeshAgent> _approachOrder = new List<NavMeshAgent>();
        private readonly Dictionary<NavMeshAgent, int> _priorities = new Dictionary<NavMeshAgent, int>();

        public void Configure(Transform exterior, Transform interior, float halfWidth)
        { _exterior = exterior; _interior = interior; _halfWidth = halfWidth; }

        private void LateUpdate()
        {
            if (_exterior == null || _interior == null) return;
            Vector3 axis = _interior.position - _exterior.position; axis.y = 0;
            float length = axis.magnitude;
            if (length < .01f) return;
            axis /= length;
            for (int i = _approachOrder.Count - 1; i >= 0; i--)
            {
                var agent = _approachOrder[i];
                if (!WithinApproach(agent, axis, length))
                { Restore(agent); _approachOrder.RemoveAt(i); }
            }
            foreach (var agent in FindObjectsByType<NavMeshAgent>())
            {
                if (_priorities.ContainsKey(agent) || !WithinApproach(agent, axis, length) ||
                    !CrossesOpening(agent, axis, length)) continue;
                _priorities.Add(agent, agent.avoidancePriority);
                _approachOrder.Add(agent);
            }
            // Keep precedence until clear of the passage, including after the
            // midpoint. Otherwise a follower gains priority over the exiting lead.
            for (int i = 0; i < _approachOrder.Count; i++)
            {
                int priority = Mathf.Min(10 + i * 10, 99);
                if (_approachOrder[i].avoidancePriority != priority) _approachOrder[i].avoidancePriority = priority;
            }
        }

        private bool WithinApproach(NavMeshAgent agent, Vector3 axis, float length)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || !agent.hasPath) return false;
            Vector3 relative = agent.transform.position - _exterior.position; relative.y = 0;
            float progress = Vector3.Dot(relative, axis);
            float braking = agent.speed * agent.speed / (2 * Mathf.Max(agent.acceleration, .01f));
            float margin = 2 * agent.radius + braking;
            return progress >= -margin && progress <= length + margin &&
                (relative - axis * progress).magnitude <= _halfWidth + margin;
        }

        private bool CrossesOpening(NavMeshAgent agent, Vector3 axis, float length)
        {
            Vector3 previous = agent.transform.position;
            float plane = length * .5f;
            foreach (var corner in agent.path.corners)
            {
                float a = Vector3.Dot(previous - _exterior.position, axis) - plane;
                float b = Vector3.Dot(corner - _exterior.position, axis) - plane;
                if (a * b <= 0 && Mathf.Abs(a - b) > .001f)
                {
                    Vector3 crossing = Vector3.Lerp(previous, corner, a / (a - b)) - _exterior.position - axis * plane;
                    crossing.y = 0;
                    if (crossing.magnitude <= _halfWidth + agent.radius) return true;
                }
                previous = corner;
            }
            return false;
        }

        private void Restore(NavMeshAgent agent)
        {
            if (_priorities.TryGetValue(agent, out int priority))
            {
                if (agent != null) agent.avoidancePriority = priority;
                _priorities.Remove(agent);
            }
        }
        private void OnDisable()
        {
            foreach (var agent in _approachOrder) Restore(agent);
            _approachOrder.Clear();
        }
    }
}
