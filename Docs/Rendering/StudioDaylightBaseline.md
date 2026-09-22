# Studio daylight baseline

Configured and visually inspected through the installed Unity MCP bridge in Studio.unity.
This is a static late-morning baseline, not a time-of-day or weather system.

## Configuration

- Existing URP 17.5 PC renderer, Linear color space, HDR camera and HDR color grading.
- One directional light: existing (50, 330, 0) rotation, intensity 1.5,
  5500 K, soft shadows, normal bias 0.2. No additional fill lights.
- StudioDaylightSky: procedural sky, exposure 1.1, atmosphere thickness 0.9.
- Trilight ambient: sky (0.72, 0.76, 0.80), equator (0.62, 0.64, 0.67),
  ground (0.38, 0.37, 0.34). Environment reflection intensity 0.85.
- StudioDaylightProfile: existing Neutral tonemapping retained, bloom 0.03,
  vignette disabled, motion blur disabled. No exposure boost or color filter.
- Main Camera: high-quality SMAA. Camera pose and controls unchanged.
- Existing 2048 shadow atlas and four cascades retained; shadow distance 90 m.
- Existing SSAO: intensity 0.25, direct-light strength 0.1, radius 0.22.
- Administration Daylight Reflection: one 128-pixel HDR baked reflection probe,
  intensity 0.85, blending enabled, no recurring realtime capture.

## Asset correction

ADM_ClearGlass had One/OneMinusSrcAlpha blending without diffuse alpha
premultiplication. Set Alpha blend mode with Preserve Specular and the corresponding
_ALPHAPREMULTIPLY_ON keyword. Color, opacity (0.13), smoothness, meshes and backings
are unchanged. This prevents additive-looking milky panes; it is not recoloring.

## Validation and limitations

Compared native MCP renders from the actual Main Camera before/after, then checked
one paused Play Mode session: management, front three-quarter, entrance, rear access,
shaded side, roof, and Casting Office. Review images are in Library/LightingReview
(local evidence, not versioned). Camera restored to (0,18,-15), Euler (52,0,0).

The reflection bake represents editor-visible geometry, not runtime-generated town
objects or moving employees. Re-bake after changes to nearby persistent geometry.
No screen-space reflections, realtime GI, terrain/art pass or performance benchmark.
The broad world remains blockout quality. The separate lantern diffuser material can
support future emissive fixtures; no night lights were added.

Future environment control can drive the existing sun, RenderSettings, sky material
and Volume profile. No calendar or simulation-clock coupling was introduced.
