using System.Collections.Generic;
using UnityEngine;

namespace SilverScreen.Presentation.Writing
{
    public sealed class ScriptOfficeStationLayout : MonoBehaviour
    {
        [SerializeField] private Transform _entrance;
        [SerializeField] private List<Transform> _stations = new List<Transform>();
        private readonly Dictionary<string, int> _reservations = new Dictionary<string, int>();

        public Transform Entrance => _entrance;
        public int Capacity => _stations.Count;

        public bool TryGetStation(int index, out Vector3 position)
        {
            if (index >= 0 && index < _stations.Count && _stations[index] != null)
            {
                position = _stations[index].position;
                return true;
            }
            position = default;
            return false;
        }

        public bool TryReserve(string writerId, out int stationIndex)
        {
            if (_reservations.TryGetValue(writerId, out stationIndex)) return true;
            for (int i = 0; i < _stations.Count; i++)
            {
                if (_reservations.ContainsValue(i)) continue;
                _reservations[writerId] = i;
                stationIndex = i;
                return true;
            }
            stationIndex = -1;
            return false;
        }

        public void Release(string writerId)
        {
            if (!string.IsNullOrWhiteSpace(writerId)) _reservations.Remove(writerId);
        }

        public void Configure(Transform entrance, IEnumerable<Transform> stations)
        {
            _entrance = entrance;
            _stations.Clear();
            if (stations != null) _stations.AddRange(stations);
        }
    }
}
