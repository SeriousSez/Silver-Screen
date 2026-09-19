using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class SlatePositionLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class SlatePosition
        {
            public SlatePositionType Type;
            public Transform Point;
        }

        [SerializeField] private List<SlatePosition> _positions = new List<SlatePosition>();

        public bool TryGetPosition(SlatePositionType positionType, out Vector3 position)
        {
            foreach (var slatePosition in _positions)
            {
                if (slatePosition.Type == positionType && slatePosition.Point != null)
                {
                    position = slatePosition.Point.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public void EnsurePrototypePositions()
        {
            AddPrototypeIfMissing(SlatePositionType.SlateStaging, new Vector3(3.8f, 0.05f, -8.4f));
            AddPrototypeIfMissing(SlatePositionType.SlateMark, new Vector3(0f, 0.05f, -9.8f));
        }

        private void AddPrototypeIfMissing(SlatePositionType positionType, Vector3 localPosition)
        {
            if (_positions.Exists(position => position.Type == positionType && position.Point != null)) return;

            var marker = new GameObject(positionType.ToString());
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localPosition;
            _positions.Add(new SlatePosition { Type = positionType, Point = marker.transform });
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var slatePosition in _positions)
            {
                if (slatePosition.Point == null) continue;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(slatePosition.Point.position, Vector3.one * 0.45f);
            }
        }
    }
}
