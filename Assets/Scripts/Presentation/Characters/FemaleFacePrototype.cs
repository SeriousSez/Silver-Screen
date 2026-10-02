using System;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    public enum PrototypeFacePose
    {
        Neutral, BlinkLeft, BlinkRight, BlinkBoth, LookLeft, LookRight, LookUp, LookDown,
        BrowRaiseLeft, BrowRaiseRight, BrowRaiseBoth, BrowLowerBoth, LookLeftBlink
    }
    public enum PrototypeMouthPose { SmileLeft, SmileRight, Smile, Frown, JawOpen, MouthPucker }

    /// <summary>Deterministic feasibility binding. No scheduler, gaze AI or production integration.</summary>
    public sealed class FemaleFacePrototype : MonoBehaviour
    {
        [Serializable] public struct BonePose
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }
        [Serializable] public sealed class PoseBinding
        {
            public PrototypeFacePose semantic;
            public BonePose[] bones;
        }
        [Serializable] public sealed class MouthBinding
        {
            public PrototypeMouthPose semantic;
            public string[] shapes;
        }
        [SerializeField] private Transform[] _bones;
        [SerializeField] private PoseBinding[] _poses;
        [SerializeField] private MouthBinding[] _mouthBindings;
        [SerializeField] private SkinnedMeshRenderer _face;
        [SerializeField] private PrototypeFacePose _pose;
        [SerializeField, Range(0, 1)] private float _amount = 1;
        private int[][] _mouthIndices;
        public Transform[] Bones => _bones;
        public SkinnedMeshRenderer Face => _face;

        public void Configure(Transform[] bones, PoseBinding[] poses, MouthBinding[] mouths, SkinnedMeshRenderer face)
        { _bones = bones; _poses = poses; _mouthBindings = mouths; _face = face; CacheShapes(); }
        private void Awake() => CacheShapes();
        private void CacheShapes()
        {
            if (_face == null || _mouthBindings == null) return;
            _mouthIndices = new int[_mouthBindings.Length][];
            for (int i = 0; i < _mouthBindings.Length; i++)
            {
                _mouthIndices[i] = new int[_mouthBindings[i].shapes.Length];
                for (int j = 0; j < _mouthIndices[i].Length; j++)
                {
                    _mouthIndices[i][j] = _face.sharedMesh.GetBlendShapeIndex(_mouthBindings[i].shapes[j]);
                    if (_mouthIndices[i][j] < 0) throw new InvalidOperationException("Missing required facial prototype binding.");
                }
            }
        }
        public bool SetPose(PrototypeFacePose semantic, float amount = 1)
        {
            if (Find(semantic) == null) return false;
            _pose = semantic; _amount = Mathf.Clamp01(amount); Apply(); return true;
        }
        public bool SetMouth(PrototypeMouthPose semantic, float amount)
        {
            if (_mouthIndices == null) CacheShapes();
            for (int i = 0; i < _mouthBindings.Length; i++)
            {
                if (_mouthBindings[i].semantic != semantic) continue;
                foreach (int index in _mouthIndices[i]) _face.SetBlendShapeWeight(index, Mathf.Clamp01(amount) * 100);
                return true;
            }
            return false;
        }
        public void ResetNeutral()
        {
            for (int i = 0; i < _face.sharedMesh.blendShapeCount; i++) _face.SetBlendShapeWeight(i, 0);
            SetPose(PrototypeFacePose.Neutral);
        }
        private PoseBinding Find(PrototypeFacePose semantic)
        {
            if (_poses != null) foreach (var p in _poses) if (p.semantic == semantic) return p;
            return null;
        }
        public void Apply()
        {
            var neutral = Find(PrototypeFacePose.Neutral); var target = Find(_pose);
            if (neutral == null || target == null || _bones == null) return;
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(neutral.bones[i].position, target.bones[i].position, _amount);
                _bones[i].localRotation = Quaternion.Slerp(neutral.bones[i].rotation, target.bones[i].rotation, _amount);
                _bones[i].localScale = Vector3.Lerp(neutral.bones[i].scale, target.bones[i].scale, _amount);
            }
        }
        private void LateUpdate() => Apply();
    }
}
