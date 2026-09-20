using System;
using System.Collections.Generic;
using SilverScreen.Domain.Movie;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class SetBlockingPointLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class BlockingPoint
        {
            public string Id;
            public Transform Point;
        }

        [SerializeField] private List<BlockingPoint> _points = new List<BlockingPoint>();

        public bool TryGetPosition(string blockingPointId, out Vector3 position)
        {
            string normalizedId = string.IsNullOrWhiteSpace(blockingPointId)
                ? null
                : blockingPointId.Trim();
            foreach (var point in _points)
            {
                if (point.Point != null &&
                    string.Equals(point.Id, normalizedId, StringComparison.Ordinal))
                {
                    position = point.Point.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public void EnsurePrototypePoints(float filmingZ = -11f)
        {
            AddPrototypeIfMissing(SceneBlockingPointIds.Center, new Vector3(0f, 0.05f, filmingZ));
            AddPrototypeIfMissing(SceneBlockingPointIds.StageLeft, new Vector3(-3f, 0.05f, filmingZ));
            AddPrototypeIfMissing(SceneBlockingPointIds.StageRight, new Vector3(3f, 0.05f, filmingZ));
        }

        private void AddPrototypeIfMissing(string id, Vector3 localPosition)
        {
            if (_points.Exists(point => point.Id == id && point.Point != null)) return;

            var marker = new GameObject($"BlockingPoint_{id}");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localPosition;
            _points.Add(new BlockingPoint { Id = id, Point = marker.transform });
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var point in _points)
            {
                if (point.Point == null) continue;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(point.Point.position, new Vector3(0.45f, 0.05f, 0.45f));
            }
        }
    }
}
