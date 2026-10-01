# Administration concept rebuild

This isolated authoring entry point supersedes the administration recipe in
`generate_reference_kit.py`. Use the dedicated generator for this asset; running
the older whole-kit script would overwrite the new administration FBX.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --python 'ArtSource\ReferenceKit1930\generate_administration.py'
```

`-- --no-render` regenerates source/export/materials without rendering. The script
updates only administration outputs and its own Unity material mappings. Other
building recipes, FBXs, town scenes and gameplay code are outside this workflow.

## Architecture and scale

An 18 x 11 m two-storey shell with a shallow stepped Deco entrance composition.
Floor level is 0.60 m above ground; the upper floor is approximately 3.93 m.
Narrow steel windows correspond to office floors. Actual openings are assembled
from masonry panels around voids. The shell is hollow, with shallow removable
window cavities providing an interior impression. These are not finished rooms.
The rear includes service doors, a simple escape stair and restrained utilities.

The root is at world ground (0,0,0). Exact exterior extents, including projecting
trim, entrance stairs and rear fire escape, are in `administration_report.json`.

## Door operation

Two 0.94 m leaves have outer hinge axes and open outward onto the landing.
Exterior vertical pulls sit near the meeting stiles, at 1.05 m above the landing,
with approximately 54 mm clear space behind the grip. Pulls have separate fixing
plates, stand-offs and round grips. The door clear leaf height is 2.15 m; the
transom is a separate framed glazing assembly. No animation is implemented.

## Glass and materials

Blender glass is a light, neutral dielectric: transmission 1, IOR 1.46,
roughness 0.095, 6 mm thickness. Window glass sits 235 mm behind the exterior
wall plane; cavity backing front sits 785 mm behind that plane. Glass darkness is
therefore produced by recessed geometry and lighting, not a black base color.

`administration_materials.py` writes two small reusable PNG maps and native URP
Lit materials, and remaps only the administration FBX while retaining its GUID.
Unity glass uses Alpha blending with Preserve Specular (shader-premultiplied diffuse),
smoothness 0.905, pale base color, no depth write and no shadow-caster pass.
This is a real-time approximation of the Blender dielectric, not ray-traced
refraction. Its final reflections depend on the inspection scene's environment.
Stucco, stone, roof and wood share restrained surface variation and normal maps.
No custom runtime shaders or project rendering settings are required.

### Material-only refresh and color convention

Run the dedicated generator with `-- --materials-only` to update the saved
Administration blend and its ten Unity materials without rebuilding geometry,
exporting FBXs, rewriting textures, or changing importer mappings. The command
checks a geometry/transform/material-slot signature before saving.

Authoritative material RGB values in `generate_administration.py` are scene-linear
Blender colors. The bridge encodes these once to sRGB for Unity Color properties;
Unity's Linear rendering then recovers the authored linear RGB. Alpha, metallic,
roughness and normal data are not gamma-converted. Existing texture color-space
settings remain unchanged. Material GUIDs are retained.

The transfer-only correction was inspected under Studio's unchanged daylight
baseline before artistic edits. Stucco/limestone already matched the requested
ivory palette and were retained. Only bronze was muted toward terracotta, the
canopy darkened toward charcoal, and the roof reduced to warm dark gray.
These edits live in the source definitions, not Unity-only overrides.
Glass retains the Alpha + Preserve Specular configuration on regeneration.

Full regeneration now refreshes source-defined materials too. Earlier QA notes
below describe historical passes that preserved materials; they do not override
this current reproducible workflow. Existing preview PNGs are historical geometry
reviews, not refreshed palette approval images.

The editable Blender components retain applied bevels and weighted corner
normals. The FBX uses nine semantic meshes with material slots: ExteriorShell,
Roof, Doors, Windows, Glass, ArchitecturalDetails, SignageArea, FireEscape and
TemporaryWindowBackings. Each uses the common ground pivot. Authoring pieces in
the blend sit under equivalent named groups. A future interior can be a separate
root child; no reveal behavior is implemented. The scale reference and review ground/lights are
excluded from the building FBX. Texture images are packed into the saved blend.

## Manual Unity review

1. Let `StudioAdministration_01.fbx` and `AdministrationMaterials` import.
2. Inspect the FBX Materials tab: all ADM_* source materials should be remapped.
3. Place an instance at scale (1,1,1) in a temporary inspection scene.
4. Compare with a 1.8 m reference and the existing employee visual. Check the
   0.60 m landing, 2.15 m leaves, and handle height measured from that landing.
5. Orbit front, sides, rear and roof. Check corners for flicker, openings for
   depth, and normals for broad-face shading artifacts.
6. Zoom to the doors. The pulls must lie on the meeting stiles; hinge barrels
   must lie at the outer edges. Leaves would swing toward the landing.
7. Inspect windows obliquely and frontally under a sky/environment reflection.
   Glass should reveal cavity backing with a restrained reflective highlight.
   Verify it does not appear opaque, pink or cast solid black pane shadows.
8. Compare the hero, clay, detail, scale and rear renders before accepting art.
   Do not replace existing gameplay buildings as part of this inspection.

## Final access and reveal preparation

The sign is blank. The 3.8 x 0.86 m usable face at front Y=-6.72 clears the
surrounding pilasters. There is no baked company name or dynamic text component.
Window backing front faces now sit 0.547 m behind the inner glazing face.
Cavity sides, tops, bottoms and lobby preview backing are all independently
removable through TemporaryWindowBackings; the central interior is unoccupied.

Rear service doors are 2.10 m tall with frames, thresholds, lever hardware at
1.05 m above threshold and hinges for inward opening. The upper threshold and
interior floor top are 3.93 m. Its 1.65 m deep landing connects to a 1.05 m wide
stair: 19 risers of about 0.178 m and 0.26 m goings. A lower landing at 0.54 m
connects to ground with three 0.18 m risers / 0.28 m goings. The lower rear door
has a separate landing and three ground steps. Guards are 1.10 m high.
The maintenance ladder has 0.28 m rung spacing, its first rung at 0.28 m, and
handholds returning over the parapet with inside rungs down to roof level.
Doorway plinth courses are interrupted so they do not obstruct either entrance.
This is visual architectural preparation, not certified building-code design.

The escape flight is offset 0.20 m outward from its first revised position.
The inner rail axis is Y=5.98 and its radius is 0.025 m; the projecting rear
sill ends at Y=5.82, leaving at least 0.135 m clearance. Tread inner edges are
at Y=6.00. Ladder brackets use the clear wall band at Z=3.5 instead of crossing
the lower-window header at Z=3.0. The generator rejects bounding-box overlaps
between escape components and rear window assemblies before export.
Door landings retain their wall connection. The main door now has
deep timber jambs, an uninterrupted threshold, seated lower panels, subtle
panel beads and bronze kick plates. Frame/header joints meet without coplanar
overlap. The narrow deep recesses around the central tower remain intentional.
The innermost tower fins are notched below the portal head to clear the outer
door edges and outward swing; their upper mass and the building silhouette remain.

Additional previews: signage, window, fire_escape and rear_access PNGs.
The left_side, right_side and street PNGs provide broader geometry QA views.
In Unity, expand the imported model and independently hide Roof, ExteriorShell
and TemporaryWindowBackings. Check that Glass/Doors/Windows remain separately
controllable and that no gameplay interior or interaction geometry was added.
Inspect both rear-door routes all the way to ground and the ladder-to-roof route.

Unity was not launched by this authoring workflow. Editor import and visual
acceptance remain manual; successful Blender export alone is not art approval.

## Surgical connectivity QA

Run the Administration generator with `-- --connectivity-qa` to export only this
asset and render the eight `qa_*` close-ups (entrance, upper/lower escape, four
corners and roof). Earlier general-purpose previews are not refreshed in this
mode. Existing Unity materials/textures are retained, not rewritten.

Horizontal stone courses, roof parapet, coping and flashing use shared-vertex
mitred rings rather than intersecting or disconnected facade boxes. The lowest
course retains deliberately capped doorway interruptions. Flashing seats against
the parapet and roof; vent curbs and ladder feet seat on their supporting surfaces.

Entrance leaves retain a 6 mm center seam and 3 mm outer jamb clearances. Hinge
leaves join the outer door stiles to the jambs; pulls attach to the meeting stiles.
Door glazing fills the 0.784 x 1.24 m aperture, and transom panes derive their
widths from the actual stile edges. Rear-door hinges and handle mounts also have
physical attachment faces rather than isolated decorative cylinders.

Lower stair handrails now sit 50 mm inside the stair edges. Their posts have
baseplates seated on the landing and last tread; landing guard returns join the
handrails. Main-flight balusters connect to lower rails on the stringers. The
last flight tread meets, rather than overlaps, the lower landing. Service-door
landing guards return to the short stair, and support columns have ground feet.

This pass adds no interior, signage text, building interaction or gameplay code.
Use the eight close-ups to inspect joints, then orbit the imported model in Unity
at scale 1. Check entrance hinges/panes, both rear-door routes, all four trim
corners and ladder-to-roof access. This remains a visual asset, not animated doors
or a certified building-code design.

### Entrance stair-to-cheek-wall closure

The landing and upper steps now meet the half walls' actual inner faces at
X=+/-2.24 m (4.48 m clear width), instead of leaving 115 mm side slots. The
lowest flared step retains its existing 4.55 m width. Landing paving extends to
the building footing. Each half wall has a continuous base to the footing and
a shaped upper return/cap matching the two existing pilaster faces and the
narrow recess between them. Existing half-wall side positions and front ends
are unchanged. The first tread now meets the landing without a coplanar overlap.

Use `-- --entrance-qa` for the three `qa_front_steps*` close-ups of this repair.
Other previews are not refreshed in that mode. No facade, material or gameplay
changes are part of this local correction.

### Well-maintained surface detail (URP Lit)

`surface_detail.py` generates deterministic, reusable material profiles. Regenerate
only materials with `generate_administration.py -- --materials-only`; this updates
the source blend and Unity materials/textures without exporting or changing meshes.
Approved base colors and glass are preserved. There is no custom shader or runtime
component, and no baked lighting, shadow or AO information.

Five 512-square maps pack a near-neutral albedo multiplier in RGB and normalized
smoothness in alpha: Stucco, Stone, Paint, Bronze and Roof. Window steel/fire escape
and canopy share Paint. Two 256-square tileable detail normals distinguish stucco
from finer stone. All maps have mipmaps; trilinear filtering and anisotropy 4 keep
oblique detail restrained. URP Lit's albedo-alpha smoothness and scaled-detail
features are used. Detail coordinates compensate for the base-map transform so
the fine normal tile covers 0.5 metres in both Blender and Unity.

Low-frequency tone and roughness variation are intentionally faint. Existing sill
geometry supplies downward deposit masks; ground contact fades over 0.24 metres.
Roof edges/curbs supply accumulation masks and the entrance landing supplies a
subtle central traffic-polish mask. These describe surface deposits/wear, not light.
The existing projected UVs share coordinates across faces: this is a restrained
projection-based approximation, not unique facade-specific weathering or a new UV
atlas. Other assets can reuse the profiles/normals and supply their own anchors and
meter-scaled mapping. The 24-metre map span is authoring configuration, not gameplay.

The pass adds seven small textures, no geometry, draw calls or material slots.
Native Lit remains compatible with the existing SRP batching path. Fine normals
add detail texture sampling on masonry; actual frame-time impact is not benchmarked.
Condition gameplay is not implemented: future condition variants can change mask
strength/profile outputs without baking deterioration into geometry. Current mask
strengths represent excellent maintenance, not damage.

Unity MCP Game-camera captures are in `Library/LightingReview/surface-final-*`:
management, front, entrance, close, rear, shade, roof and camera-offset checks.
Still captures show clean ivory at distance and fine texture nearby; continuous
camera motion should still be checked manually for temporal shimmer.

#### Isotropic masonry-grain refinement

The original normal generator summed 24 plane waves with positive X/Y frequencies,
producing diagonal fingerprint ridges. It is replaced by independent white-noise
octaves filtered with radial Gaussian kernels, then differentiated periodically.
Stucco uses 0.7/1.5/3.0 texel radii; stone uses its own seed and finer
0.55/1.1/2.0 texel radii. At 256 texels per 0.5-metre tile these are millimetre-scale
irregularities. No displacement or geometry changes are involved.

Normal slope RMS is explicitly limited to 0.06 for stucco and 0.025 for stone,
before the existing 0.22/0.14 shader strengths. This removes directional structure
and makes normals secondary to the existing roughness variation. Base palette,
roughness maps, construction masks, UV scale, glass and lighting remain unchanged.
No new textures, shaders or runtime texture samples are required by this refinement.

Matched Unity MCP captures: `Library/GrainReview/before-*` and `after-*`, with
management, medium and extreme views under identical lighting. The extreme view
shows no former fingerprint ridges; medium/distant views remain clean. Continuous
motion and target-device performance were not benchmarked. Projected weather masks
remain approximate and the finite grain tile is still periodic, though no obvious
repeat was visible in the inspected views.

#### Unity-local environmental weathering (supersedes previous mask passes)

Localized source-UV weathering has been removed from surface_detail.py. Base
variation, approved mineral grain/normal maps, normal strengths and scales remain
unchanged. No source sill, ledge, corner, ground, roof or parapet deposits remain.

Run generate_weathering_overlays.py with Blender to regenerate three independent
512px RGBA masks in Assets/SilverScreen/Environment/Weathering. These are reusable
Runoff, Junction and Contact overlays; they do not depend on Administration UVs.

Studio.unity contains Administration_Weathering under the Administration model,
with seven native DecalProjectors. The PC renderer adds an albedo-only DBuffer
DecalRendererFeature. No custom runtime component or shader is needed. Albedo-only
preserves the approved normal and roughness response. Three shared instanced
materials use the package Decal shader. Each projector supports transform, size,
Fade Factor and enable/disable. Draw distance is 45m with a 0.75 fade start.

Sites: two selected front sills, one broken front cornice patch, two small base
contacts, one roof/parapet contact and one vent-base contact. Most windows and
walls remain clean. Projection depth is 0.1m to avoid distant/opposite receivers.
Disable Administration_Weathering to inspect the clean baseline. Future revealed
interiors should disable the weathering together with their exterior shell.

The bronze source now uses linear RGB (0.40, 0.245, 0.135), metallic 1.0 and
roughness 0.24, retaining the existing subtle surface variation and corrected
linear-to-sRGB transfer. Wood, charcoal metals, glass and masonry colors remain
unchanged. Materials-only generation does not re-export geometry.

Validation captures are in Library/UnityWeatheringReview (clean-* versus decal-*,
bronze-light, bronze-shade, entrance and roof). These are MainCamera renders from
Unity MCP, not Blender previews. Library is disposable and not an asset dependency.
DBuffer adds a color buffer/compositing pass and requires a depth/normal prepass;
this desktop renderer already uses depth-normal SSAO. Seven projectors are a small
prototype, not a measured frame-time guarantee. No mobile performance claim.
No condition simulation, automatic placement or prefab-wide deployment is added.
