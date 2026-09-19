using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class ProductionStationLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class Station
        {
            public ProductionStationType Type;
            public Transform Point;
        }

        [SerializeField] private List<Station> _stations = new List<Station>();

        public bool TryGetPosition(ProductionStationType stationType, out Vector3 position)
        {
            foreach (var station in _stations)
            {
                if (station.Type == stationType && station.Point != null)
                {
                    position = station.Point.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public void EnsurePrototypeStations()
        {
            AddPrototypeIfMissing(ProductionStationType.Director, new Vector3(-3f, 0.05f, -8.2f));
            AddPrototypeIfMissing(ProductionStationType.ActorWaitingA, new Vector3(-1.5f, 0.05f, -8.2f));
            AddPrototypeIfMissing(ProductionStationType.ActorWaitingB, new Vector3(0f, 0.05f, -8.2f));
            AddPrototypeIfMissing(ProductionStationType.ActorWaitingC, new Vector3(1.5f, 0.05f, -8.2f));
            AddPrototypeIfMissing(ProductionStationType.Camera, new Vector3(-2f, 0.05f, -9.7f));
            AddPrototypeIfMissing(ProductionStationType.Sound, new Vector3(0f, 0.05f, -9.7f));
            AddPrototypeIfMissing(ProductionStationType.CrewWaiting, new Vector3(2f, 0.05f, -9.7f));
        }

        private void AddPrototypeIfMissing(ProductionStationType stationType, Vector3 localPosition)
        {
            if (_stations.Exists(station => station.Type == stationType && station.Point != null)) return;

            var marker = new GameObject(stationType.ToString());
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localPosition;
            _stations.Add(new Station { Type = stationType, Point = marker.transform });
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var station in _stations)
            {
                if (station.Point == null) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(station.Point.position, 0.3f);
            }
        }
    }
}