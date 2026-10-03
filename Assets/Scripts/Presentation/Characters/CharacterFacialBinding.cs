using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    public enum FacialSemantic
    {
        BlinkBoth, JawForward, JawLeft, JawRight, JawOpen, MouthClose, MouthFunnel, MouthPucker,
        MouthLeft, MouthRight, SmileLeft, SmileRight, FrownLeft, FrownRight, DimpleLeft, DimpleRight,
        MouthStretchLeft, MouthStretchRight, MouthRollLower, MouthRollUpper, MouthShrugLower, MouthShrugUpper,
        MouthPressLeft, MouthPressRight, MouthLowerDownLeft, MouthLowerDownRight, MouthUpperUpLeft, MouthUpperUpRight,
        CheekPuff, TongueOut
    }
    public enum FacialCapability { Missing, Supported, Rejected }

    [Serializable]
    public struct FacialBindingEntry
    {
        [SerializeField] private FacialSemantic _semantic;
        [SerializeField] private FacialCapability _capability;
        [SerializeField] private string _rawChannel, _reason;
        [SerializeField] private float _normalizedMinimum, _normalizedMaximum;
        public FacialSemantic Semantic => _semantic;
        public FacialCapability Capability => _capability;
        public string RawChannel => _rawChannel;
        public string Reason => _reason;
        public float Weight(float normalized) => Mathf.Lerp(_normalizedMinimum, _normalizedMaximum, normalized) * 100;
        public FacialBindingEntry(FacialSemantic semantic, string rawChannel, FacialCapability capability = FacialCapability.Supported,
            string reason = null, float normalizedMinimum = 0, float normalizedMaximum = 1)
        { _semantic = semantic; _rawChannel = rawChannel; _capability = capability; _reason = reason;
            _normalizedMinimum = normalizedMinimum; _normalizedMaximum = normalizedMaximum; }
        internal bool Valid => Enum.IsDefined(typeof(FacialSemantic), _semantic) && Enum.IsDefined(typeof(FacialCapability), _capability) &&
            (_capability != FacialCapability.Supported || !string.IsNullOrWhiteSpace(_rawChannel)) &&
            float.IsFinite(_normalizedMinimum) && float.IsFinite(_normalizedMaximum) &&
            _normalizedMinimum >= 0 && _normalizedMaximum <= 1 && _normalizedMinimum <= _normalizedMaximum;
    }

    [Serializable]
    public sealed class CharacterFacialBinding
    {
        [SerializeField] private FacialBindingEntry[] _entries = Array.Empty<FacialBindingEntry>();
        public IReadOnlyList<FacialBindingEntry> Entries => Array.AsReadOnly(_entries);
        public CharacterFacialBinding(params FacialBindingEntry[] entries) { _entries = (FacialBindingEntry[])entries.Clone(); }
        public bool TryValidate(out string reason)
        {
            reason = null; var seen = new HashSet<FacialSemantic>(); var raw = new HashSet<string>(StringComparer.Ordinal);
            if (_entries == null) { reason = "Facial entries absent."; return false; }
            foreach (var entry in _entries)
                if (!entry.Valid || !seen.Add(entry.Semantic) || (entry.Capability == FacialCapability.Supported && !raw.Add(entry.RawChannel)))
                { reason = "Invalid or duplicate facial binding: " + entry.Semantic; return false; }
            return true;
        }
    }
}
