using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class ActorSceneMarkLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class Mark
        {
            public ActorSceneMarkType Type;
            public Transform Point;
        }

        [SerializeField] private List<Mark> _marks = new List<Mark>();

        public bool TryGetPosition(ActorSceneMarkType markType, out Vector3 position)
        {
            foreach (var mark in _marks)
            {
                if (mark.Type == markType && mark.Point != null)
                {
                    position = mark.Point.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public void EnsurePrototypeMarks(float filmingZ = -11f)
        {
            AddPrototypeIfMissing(ActorSceneMarkType.ActorMarkA, new Vector3(-1.5f, 0.05f, filmingZ));
            AddPrototypeIfMissing(ActorSceneMarkType.ActorMarkB, new Vector3(0f, 0.05f, filmingZ));
            AddPrototypeIfMissing(ActorSceneMarkType.ActorMarkC, new Vector3(1.5f, 0.05f, filmingZ));
        }

        private void AddPrototypeIfMissing(ActorSceneMarkType markType, Vector3 localPosition)
        {
            if (_marks.Exists(mark => mark.Type == markType && mark.Point != null)) return;

            var marker = new GameObject(markType.ToString());
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localPosition;
            _marks.Add(new Mark { Type = markType, Point = marker.transform });
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var mark in _marks)
            {
                if (mark.Point == null) continue;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(mark.Point.position, 0.3f);
            }
        }
    }
}
