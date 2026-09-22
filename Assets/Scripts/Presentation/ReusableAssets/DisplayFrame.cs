using System.Collections.Generic;
using UnityEngine;

namespace SilverScreen.Presentation.ReusableAssets
{
    /// <summary>Frame construction stays independent of its replaceable content sockets.</summary>
    public sealed class DisplayFrame : MonoBehaviour
    {
        [SerializeField] private Transform _backing, _outerFrame, _optionalDivider;
        [SerializeField] private DisplayInsertSlot[] _slots;
        public IReadOnlyList<DisplayInsertSlot> Slots => _slots;
        public Transform Backing => _backing;
        public Transform OuterFrame => _outerFrame;
        public Transform OptionalDivider => _optionalDivider;
    }
}
