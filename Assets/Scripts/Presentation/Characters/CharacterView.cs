using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Characters;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    /// <summary>Feet-origin presentation. Does not own routing, employee state or simulation time.</summary>
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private CharacterAppearance _appearance;
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<Transform, Quaternion> _restRotation = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<Transform, Vector3> _restPosition = new Dictionary<Transform, Vector3>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private SkinnedMeshRenderer[] _renderers;
        private readonly Dictionary<string, List<(SkinnedMeshRenderer renderer, int index)>> _morphs = new Dictionary<string, List<(SkinnedMeshRenderer, int)>>();
        public CharacterAppearance Appearance => _appearance.Copy();
        public float StandingHeight { get; private set; }
        public float EyeHeight { get; private set; }
        public float ShoulderBreadth { get; private set; }
        public Vector3 Feet => transform.position;
        public Vector3 Eyes => (Bone("LeftEye").position + Bone("RightEye").position) * .5f;
        public Vector3 Head => Bone("Head").position;
        public Vector3 LeftHand => Bone("LeftHand").position;
        public Vector3 RightHand => Bone("RightHand").position;
        public Vector3 LeftFoot => Bone("LeftFoot").position;
        public Vector3 RightFoot => Bone("RightFoot").position;
        public Transform Bone(string name) => _bones.TryGetValue(name, out var bone) ? bone : throw new InvalidOperationException("Missing adult-v1 bone: " + name);
        public IReadOnlyList<SkinnedMeshRenderer> Renderers => _renderers;

        internal void Initialize(CharacterAppearance profile, float referenceHeight)
        {
            _appearance = profile.Copy();
            foreach (var t in GetComponentsInChildren<Transform>()) if (!_bones.ContainsKey(t.name)) _bones.Add(t.name, t);
            _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            float factor = profile.heightMetres / referenceHeight;
            if (Mathf.Abs(factor - 1) > .00001f) CalibrateScale(factor);
            foreach (var t in GetComponentsInChildren<Transform>()) if (t != transform)
            { _restRotation[t] = t.localRotation; _restPosition[t] = t.localPosition; }
            foreach (var renderer in _renderers)
            {
                renderer.updateWhenOffscreen = true;
                renderer.quality = SkinQuality.Bone4;
                var mesh = renderer.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string name = mesh.GetBlendShapeName(i); int dot = name.LastIndexOf('.'); if (dot >= 0) name = name.Substring(dot + 1);
                    if (!_morphs.TryGetValue(name, out var bindings)) _morphs[name] = bindings = new List<(SkinnedMeshRenderer, int)>();
                    bindings.Add((renderer, i));
                }
            }
            SetMorph("IdentityJawWidth", profile.jawWidth); SetMorph("IdentityNoseWidth", profile.noseWidth);
            SetMorph("IdentityNoseBridge", profile.noseBridge); SetMorph("IdentityMouthWidth", profile.mouthWidth);
            SetMorph("IdentityCheekWidth", profile.cheekWidth); SetMorph("IdentityChinHeight", profile.chinHeight);
            StandingHeight = referenceHeight * factor;
            EyeHeight = transform.InverseTransformPoint(Eyes).y;
            ShoulderBreadth = Vector3.Distance(Bone("LeftArm").position, Bone("RightArm").position);
            var lod = gameObject.AddComponent<LODGroup>();
            var common = _renderers.Where(r => !r.name.StartsWith("Body_LOD", StringComparison.Ordinal)).Cast<Renderer>();
            lod.SetLODs(new[] {
                new LOD(.22f, common.Concat(_renderers.Where(r => r.name.StartsWith("Body_LOD0", StringComparison.Ordinal))).ToArray()),
                new LOD(.015f, common.Concat(_renderers.Where(r => r.name.StartsWith("Body_LOD1", StringComparison.Ordinal))).ToArray()) });
            lod.RecalculateBounds();
            CharacterMotion.Sample(this, CharacterPose.Idle, 0);
        }

        private void CalibrateScale(float factor)
        {
            // Bake units into vertex/rest coordinates; root remains unit scale.
            foreach (var t in GetComponentsInChildren<Transform>()) if (t != transform) t.localPosition *= factor;
            foreach (var renderer in _renderers)
            {
                var source = renderer.sharedMesh; var mesh = Instantiate(source); mesh.name = source.name + "_Calibrated";
                _ownedMeshes.Add(mesh);
                var vertices = mesh.vertices; for (int i = 0; i < vertices.Length; i++) vertices[i] *= factor; mesh.vertices = vertices;
                mesh.ClearBlendShapes();
                var dv = new Vector3[source.vertexCount]; var dn = new Vector3[dv.Length]; var dt = new Vector3[dv.Length];
                for (int i = 0; i < source.blendShapeCount; i++) for (int f = 0; f < source.GetBlendShapeFrameCount(i); f++)
                {
                    source.GetBlendShapeFrameVertices(i, f, dv, dn, dt);
                    for (int v = 0; v < dv.Length; v++) dv[v] *= factor;
                    mesh.AddBlendShapeFrame(source.GetBlendShapeName(i), source.GetBlendShapeFrameWeight(i, f), dv, dn, dt);
                }
                mesh.bindposes = renderer.bones.Select(b => b.worldToLocalMatrix * renderer.transform.localToWorldMatrix).ToArray();
                mesh.RecalculateBounds(); renderer.sharedMesh = mesh;
                var bounds = renderer.localBounds; bounds.center *= factor; bounds.extents *= factor; renderer.localBounds = bounds;
            }
        }

        public void SetMorph(string name, float value)
        { if (_morphs.TryGetValue(name, out var bindings)) foreach (var b in bindings) b.renderer.SetBlendShapeWeight(b.index, Mathf.Clamp(value, -1, 1) * 100); }

        public void SetExpression(float blink, float jaw, float smile, float frown, Vector2 gaze)
        {
            SetMorph("eyeBlinkLeft", blink); SetMorph("eyeBlinkRight", blink); SetMorph("jawOpen", jaw);
            SetMorph("mouthSmileLeft", smile); SetMorph("mouthSmileRight", smile);
            SetMorph("mouthFrownLeft", frown); SetMorph("mouthFrownRight", frown);
            SetMorph("browDownLeft", frown * .7f); SetMorph("browDownRight", frown * .7f);
            SetMorph("cheekSquintLeft", smile * .4f); SetMorph("cheekSquintRight", smile * .4f);
            foreach (var name in new[] { "LeftEye", "RightEye" })
            {
                var eye = Bone(name); eye.localRotation = _restRotation[eye];
                eye.rotation = Quaternion.AngleAxis(Mathf.Clamp(gaze.x, -1, 1) * 20, transform.up) *
                    Quaternion.AngleAxis(Mathf.Clamp(gaze.y, -1, 1) * -12, transform.right) * eye.rotation;
            }
        }

        public void ResetPose()
        { foreach (var pair in _restRotation) { pair.Key.localRotation = pair.Value; pair.Key.localPosition = _restPosition[pair.Key]; } }

        public void SetHairVisible(bool visible)
        { foreach (var r in _renderers) if (r.name == "Hair") r.enabled = visible; }

        private void OnDestroy()
        { foreach (var mesh in _ownedMeshes) if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); } }
    }
}
