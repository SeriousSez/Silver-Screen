using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilverScreen.Presentation.Buildings
{
    [Flags]
    public enum BuildingRevealReason
    {
        None = 0, HeldPersonInteraction = 1, BuildingFocus = 2,
        FollowPerson = 4, ProductionView = 8, DeveloperPreview = 16,
        PointerHover = 32, ContextualTargetActive = 64, PostInteractionHold = 128,
        DismissalConfirmation = 256
    }

    public enum BuildingCutawayMode { Shell, StructuralOccluder }

    /// <summary>One reveal owner. Rendering changes never alter physical collision or the LOD hierarchy.</summary>
    public sealed class BuildingCutawayController : MonoBehaviour
    {
        [Serializable]
        public sealed class VisibilityGroup
        {
            public string Id;
            public bool Roof;
            [Tooltip("Use the full local camera direction for curved roof sections. Roof=true remains an always-revealed cap.")]
            public bool UseVerticalDirection;
            public Vector3 OutwardNormal;
            public Renderer[] Renderers = Array.Empty<Renderer>();
            public Light[] Lights = Array.Empty<Light>();
            public Collider[] Occluders = Array.Empty<Collider>();
            public bool SightlineOnly;
            public Bounds LocalSection;
            public string SupportGroupId;
            public BuildingCutawayMode Mode;
            [Range(0, 1), Tooltip("Remaining opacity of an obstructing structural group. Dependents inherit this value.")]
            public float CutawayOpacity = .22f;
            [Tooltip("Local gameplay area sampled for structural obstruction, independently of shell visibility.")]
            public Bounds StructuralFocus;
            [Range(0, 1)] public float StructuralEnterCoverage = .04f;
            [Range(0, 1)] public float StructuralExitCoverage = .015f;
        }
        [SerializeField] private VisibilityGroup[] _groups = Array.Empty<VisibilityGroup>();
        [SerializeField] private Bounds _localInterior = new Bounds(new Vector3(0, 2, 0), new Vector3(14, 4, 9));
        [SerializeField, Min(.01f)] private float _transitionSeconds = .35f;
        [SerializeField, Min(0)] private float _hoverIntent = .22f;
        [SerializeField, Min(0)] private float _hoverGrace = .45f;
        [SerializeField, Min(0)] private float _carryMargin = 4f;
        [SerializeField, Min(0)] private float _carryHysteresis = 1.5f;
        private BuildingRevealReason _reasons;
        private float _hoverSince = -1, _hoverUntil = -1, _postUntil = -1;
        private Vector3 _observer;
        private UnityEngine.Camera _camera;
        private readonly HashSet<Collider> _hiddenOccluders = new HashSet<Collider>();
        private readonly List<GroupFade> _fades = new List<GroupFade>();
        [SerializeField] private Vector3[] _interiorFocus = Array.Empty<Vector3>();
        private int[] _supports = Array.Empty<int>();
        private bool[] _cutawayTargets = Array.Empty<bool>();
        private BuildingCutawaySections _sections;
        private static readonly List<BuildingCutawayController> _active = new List<BuildingCutawayController>();
        public static IReadOnlyList<BuildingCutawayController> Active => _active;
        public bool IsRevealed => _reasons != BuildingRevealReason.None;
        public BuildingRevealReason Reasons => _reasons;
        public Bounds LocalInterior => _localInterior;
        public IReadOnlyList<VisibilityGroup> Groups => _groups;

        public void Configure(VisibilityGroup[] groups, Bounds interior)
        { Restore(); _groups = groups ?? Array.Empty<VisibilityGroup>(); _localInterior = interior; _supports = Array.Empty<int>(); }

        public void SetInteriorFocusPoints(Vector3[] localPoints) => _interiorFocus = localPoints ?? Array.Empty<Vector3>();

        public void AddInteriorSections(Bounds[] sections, MeshRenderer[] sources, Collider[] colliders)
        {
            if (_sections != null) return;
            foreach (var fade in _fades) fade.Dispose(); _fades.Clear();
            _sections = new BuildingCutawaySections(transform, sections, sources, colliders);
            var groups = new List<VisibilityGroup>(_groups); groups.AddRange(_sections.Groups);
            _groups = groups.ToArray();
            _supports = Array.Empty<int>();
        }

        public void SetReveal(BuildingRevealReason reason, bool enabled, Vector3 observerPosition)
        {
            _reasons = enabled ? _reasons | reason : _reasons & ~reason;
            _observer = observerPosition;
            // Editor review tooling still gets an immediate deterministic pose.
            if (!Application.isPlaying) Apply(1f, true);
        }
        public void UpdatePointer(bool hovering, Vector3 observerPosition)
        {
            float now = Time.unscaledTime;
            if (hovering)
            {
                if (_hoverSince < 0) _hoverSince = now;
                if (now - _hoverSince >= _hoverIntent) _hoverUntil = now + _hoverGrace;
            }
            else _hoverSince = -1;
            SetReveal(BuildingRevealReason.PointerHover, now < _hoverUntil, observerPosition);
        }
        public void HoldAfterInteraction(float seconds = 2f)
        {
            _postUntil = Mathf.Max(_postUntil, Time.unscaledTime + seconds);
            _reasons |= BuildingRevealReason.PostInteractionHold;
        }
        public bool WithinCarryEnvelope(Vector3 point, bool retaining)
        {
            var local = transform.InverseTransformPoint(point); local.y = _localInterior.center.y;
            float margin = _carryMargin + (retaining ? _carryHysteresis : 0);
            return _localInterior.SqrDistance(local) <= margin * margin;
        }
        private void LateUpdate()
        {
            if (_camera == null) _camera = UnityEngine.Camera.main;
            if (_camera != null) _observer = _camera.transform.position;
            if (Time.unscaledTime >= _postUntil) _reasons &= ~BuildingRevealReason.PostInteractionHold;
            if (Time.unscaledTime >= _hoverUntil) _reasons &= ~BuildingRevealReason.PointerHover;
            if (IsRevealed || _fades.Count != 0) Apply(Time.unscaledDeltaTime / Mathf.Max(.01f, _transitionSeconds), false);
        }
        private void Apply(float step, bool immediate)
        {
            if (_fades.Count == 0) foreach (var group in _groups) _fades.Add(new GroupFade(group));
            if (_supports.Length != _groups.Length) ResolveSupports();
            var localObserver = transform.InverseTransformPoint(_observer);
            var toward = localObserver - _localInterior.center;
            Vector3? parallelDirection = _camera != null && _camera.orthographic
                ? transform.InverseTransformDirection(_camera.transform.forward) : (Vector3?)null;
            // Evaluate each support once, before dependents, so group ordering does not affect hysteresis.
            for (int i = 0; i < _groups.Length; i++)
            {
                if (_supports[i] != i) continue;
                var support = _groups[i];
                if (support.Mode == BuildingCutawayMode.StructuralOccluder)
                    _cutawayTargets[i] = IsRevealed && StructuralObstructs(support, localObserver, _cutawayTargets[i], parallelDirection);
                else
                {
                    var direction = toward;
                    if (!support.UseVerticalDirection) direction.y = 0;
                    _cutawayTargets[i] = IsRevealed && (support.Roof ||
                        !support.SightlineOnly && Vector3.Dot(support.OutwardNormal, direction) > .01f || ObstructsInterior(support));
                }
            }
            _hiddenOccluders.Clear();
            for (int i = 0; i < _groups.Length; i++)
            {
                var group = _groups[i];
                // Resolve the entire semantic support chain, independent of Transform ancestry/order.
                var support = _groups[_supports[i]];
                bool hide = _cutawayTargets[_supports[i]];
                bool structural = support.Mode == BuildingCutawayMode.StructuralOccluder;
                var fade = _fades[i]; fade.Apply(hide ? 1 : 0, step, immediate, structural ? Mathf.Clamp01(support.CutawayOpacity) : 0);
                // Only interaction rays ignore cutaway surfaces. Physics and NavMesh remain intact.
                if (structural ? fade.Opacity <= .35f : hide || fade.Progress > 0)
                    foreach (var collider in group.Occluders) if (collider != null) _hiddenOccluders.Add(collider);
            }
        }
        private void ResolveSupports()
        {
            _supports = new int[_groups.Length];
            _cutawayTargets = new bool[_groups.Length];
            var ids = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _groups.Length; i++)
                if (!string.IsNullOrEmpty(_groups[i].Id)) ids[_groups[i].Id] = i;
            for (int i = 0; i < _groups.Length; i++)
            {
                int current = i, remaining = _groups.Length;
                while (!string.IsNullOrEmpty(_groups[current].SupportGroupId) && remaining-- > 0)
                {
                    if (!ids.TryGetValue(_groups[current].SupportGroupId, out int next)) break;
                    current = next;
                }
                // Malformed/cyclic authoring fails closed, without recursion or a frame-time allocation.
                _supports[i] = remaining < 0 ? i : current;
            }
        }
        public static bool StructuralObstructs(VisibilityGroup group, Vector3 localObserver, bool retaining, Vector3? parallelDirection = null)
        {
            // Fifty segments against one authored band, never renderer/triangle analysis.
            // Sampling a fixed gameplay area avoids churn at viewport edges during a slow orbit.
            var area = group.StructuralFocus;
            var low = area.min; var high = area.max;
            int obstructed = 0;
            for (int z = 0; z < 10; z++) for (int x = 0; x < 5; x++)
            {
                // Include a row below the band: thin beams viewed along their length must not fall
                // between grid samples. The same finite segment test still rejects far/non-occluding bands.
                float depth = z < 9 ? Mathf.Lerp(low.z, high.z, z / 8f) : Mathf.Clamp(group.LocalSection.center.z, low.z, high.z);
                var point = new Vector3(Mathf.Lerp(low.x, high.x, x / 4f), area.center.y, depth);
                var origin = localObserver;
                if (parallelDirection.HasValue)
                {
                    var direction = parallelDirection.Value.normalized;
                    float distance = Vector3.Dot(point - localObserver, direction);
                    if (distance <= 0) continue;
                    origin = point - direction * distance;
                }
                if (SegmentObstructed(group.LocalSection, origin, point)) obstructed++;
            }
            float threshold = retaining ? Mathf.Min(group.StructuralExitCoverage, group.StructuralEnterCoverage) : group.StructuralEnterCoverage;
            return obstructed > 0 && obstructed / 50f >= threshold;
        }
        private bool ObstructsInterior(VisibilityGroup group)
        {
            foreach (var localPoint in _interiorFocus)
            {
                var point = transform.TransformPoint(localPoint);
                Ray ray;
                if (Application.isPlaying && _camera != null)
                {
                    var screen = _camera.WorldToViewportPoint(point);
                    if (screen.z <= 0 || screen.x < -.05f || screen.x > 1.05f || screen.y < -.05f || screen.y > 1.05f) continue;
                    // Correct for both orthographic and perspective cameras, including an offset orbit pivot.
                    ray = _camera.ViewportPointToRay(screen);
                }
                else ray = new Ray(_observer, point - _observer);
                float distance = Vector3.Dot(point - ray.origin, ray.direction) - .03f;
                if (distance <= 0) continue;
                if (group.Occluders.Length > 0)
                {
                    foreach (var collider in group.Occluders)
                        if (collider != null && collider.Raycast(ray, out _, distance)) return true;
                }
                else if (group.SightlineOnly && SegmentObstructed(group.LocalSection,
                    transform.InverseTransformPoint(ray.origin), localPoint)) return true;
            }
            return false;
        }
        public static bool SegmentObstructed(Bounds section, Vector3 origin, Vector3 destination)
        {
            var delta = destination - origin;
            return section.IntersectRay(new Ray(origin, delta), out float along) && along < delta.magnitude - .03f;
        }
        public void Restore()
        {
            foreach (var fade in _fades) fade.Dispose(); _fades.Clear();
            _hiddenOccluders.Clear(); _reasons = BuildingRevealReason.None;
            Array.Clear(_cutawayTargets, 0, _cutawayTargets.Length);
            _hoverSince = _hoverUntil = _postUntil = -1;
        }
        public bool IsHiddenOccluder(Collider collider) => _hiddenOccluders.Contains(collider);
        public bool Intersects(Ray ray, out float distance)
        {
            var local = new Ray(transform.InverseTransformPoint(ray.origin), transform.InverseTransformDirection(ray.direction));
            return _localInterior.IntersectRay(local, out distance);
        }
        private void OnEnable() { if (!_active.Contains(this)) _active.Add(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => _active.Clear();
        private void OnDisable() { Restore(); _active.Remove(this); }
        private void OnDestroy() { Restore(); _sections?.Dispose(); }

        // Shared per-source/per-group variants, reused across LODs. Original materials return at rest.
        // No mesh copies, property blocks or per-frame material creation.
        private sealed class GroupFade : IDisposable
        {
            private sealed class Binding
            { public Renderer Renderer; public Material[] Original, Fade; public bool ForcedOff, UsingFade; }
            private readonly List<Binding> _bindings = new List<Binding>();
            private readonly Dictionary<Material, Material> _variants = new Dictionary<Material, Material>();
            private readonly List<(Light Light, float Intensity)> _lights = new List<(Light, float)>();
            private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            private static readonly int CutawayOpacity = Shader.PropertyToID("_CutawayOpacity");
            private const string UrpLitShader = "Universal Render Pipeline/Lit";
            private const string CutawayLitResource = "Shaders/CutawayLit";
            private static readonly string[] PreservedPasses = {
                "UniversalForward", "UniversalGBuffer", "ShadowCaster", "DepthOnly", "DepthNormals",
                "Meta", "Universal2D", "MotionVectors", "XRMotionVectors"
            };
            private static Shader _cutawayLitShader;
            public float Progress { get; private set; }
            public float Opacity { get; private set; } = 1;
            public GroupFade(VisibilityGroup group)
            {
                foreach (var renderer in group.Renderers)
                    if (renderer != null) _bindings.Add(new Binding { Renderer = renderer, Original = renderer.sharedMaterials, ForcedOff = renderer.forceRenderingOff });
                foreach (var light in group.Lights)
                    if (light != null) _lights.Add((light, light.intensity));
            }
            private Material Variant(Material source)
            {
                if (source == null) return null;
                if (_variants.TryGetValue(source, out var variant)) return variant;
                variant = new Material(source) { name = source.name + " (cutaway fade)", hideFlags = HideFlags.HideAndDontSave };
                if (source.shader != null && source.shader.name == UrpLitShader &&
                    (_cutawayLitShader != null || (_cutawayLitShader = Resources.Load<Shader>(CutawayLitResource)) != null))
                {
                    var queue = source.renderQueue;
                    var keywords = source.shaderKeywords;
                    var passStates = new bool[PreservedPasses.Length];
                    for (int i = 0; i < PreservedPasses.Length; i++)
                        passStates[i] = source.GetShaderPassEnabled(PreservedPasses[i]);
                    variant.shader = _cutawayLitShader;
                    variant.CopyPropertiesFromMaterial(source);
                    variant.shaderKeywords = keywords;
                    variant.renderQueue = queue;
                    for (int i = 0; i < PreservedPasses.Length; i++)
                        variant.SetShaderPassEnabled(PreservedPasses[i], passStates[i]);
                    variant.SetFloat(CutawayOpacity, 1);
                }
                else
                {
                    // Non-Lit shaders retain the previous compatibility path until they provide
                    // an opaque cutaway adapter. Existing Studio Services/Stage materials are URP Lit.
                    if (variant.HasProperty("_Surface")) variant.SetFloat("_Surface", 1);
                    if (variant.HasProperty("_Blend")) variant.SetFloat("_Blend", 0);
                    variant.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); variant.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    variant.SetInt("_ZWrite", 0);
                    variant.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); variant.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    variant.DisableKeyword("_ALPHAMODULATE_ON");
                    variant.SetOverrideTag("RenderType", "Transparent"); variant.renderQueue = (int)RenderQueue.Transparent;
                    variant.SetShaderPassEnabled("ShadowCaster", false);
                }
                _variants.Add(source, variant); return variant;
            }
            public void Apply(float target, float step, bool immediate, float cutawayOpacity)
            {
                float next = immediate ? target : Mathf.MoveTowards(Progress, target, step);
                float alpha = Mathf.Lerp(1, cutawayOpacity, Mathf.SmoothStep(0, 1, next));
                if (next == Progress && alpha == Opacity) return;
                Progress = next;
                Opacity = alpha;
                bool fading = Opacity > 0 && Opacity < 1;
                foreach (var binding in _bindings)
                {
                    var renderer = binding.Renderer; if (renderer == null) continue;
                    renderer.forceRenderingOff = binding.ForcedOff || Opacity <= 0;
                    if (fading && binding.Fade == null)
                    {
                        binding.Fade = new Material[binding.Original.Length];
                        for (int i = 0; i < binding.Fade.Length; i++) binding.Fade[i] = Variant(binding.Original[i]);
                    }
                    if (binding.UsingFade != fading)
                    { renderer.sharedMaterials = fading ? binding.Fade : binding.Original; binding.UsingFade = fading; }
                }
                foreach (var light in _lights) if (light.Light != null) light.Light.intensity = light.Intensity * alpha;
                foreach (var pair in _variants)
                {
                    if (pair.Value.HasProperty(CutawayOpacity)) pair.Value.SetFloat(CutawayOpacity, alpha);
                    else if (pair.Key != null && pair.Key.HasProperty(BaseColor))
                    { var color = pair.Key.GetColor(BaseColor); color.a *= alpha; pair.Value.SetColor(BaseColor, color); }
                }
            }
            public void Dispose()
            {
                foreach (var light in _lights) if (light.Light != null) light.Light.intensity = light.Intensity;
                foreach (var binding in _bindings) if (binding.Renderer != null)
                { binding.Renderer.forceRenderingOff = binding.ForcedOff; if (binding.UsingFade) binding.Renderer.sharedMaterials = binding.Original; }
                foreach (var material in _variants.Values)
                    if (Application.isPlaying) UnityEngine.Object.Destroy(material); else UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
