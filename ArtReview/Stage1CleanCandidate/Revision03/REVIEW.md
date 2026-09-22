# Stage 1 — production geometry revision 03

**Stopped for visual approval.** [Open the concept-versus-Unity gallery](review.html).

The candidate now has built architectural assemblies: board-and-core scenery doors with straps, hinges and locks; panelled entrances and folded canopies; recessed T-section glazing; roof seams, curbs, flashing and louvers; rolled bowstring trusses and riveted connections; a separate suspended rigging system; supported grating catwalks; tread-by-tread stairs and continuous rails; permanent lamps and connected service equipment. No major component is deliberately left as a primitive replacement placeholder.

Twenty-two final Unity views were inspected, covering front, both visible side compositions, rear, roof, enclosed interior, reverse interior, structure, stairs, doors, services and management/context. Iterations corrected roof-lining/glazing interference, dark door material response, sparse rigging, low worklights, landing rail connections, lower handrail returns, base trim crossing side doorways and conduit crossing a door head. Native higher-resolution captures resolve thin-joint/bevel aliasing more clearly; normal-resolution management images are also included.

## Architecture and interpretation

| Item | Candidate |
|---|---|
| Wall footprint | 15.8 × 24 m |
| Eave / roof crown / coping crown | 7.10 / 10.77 / 11.01 m |
| Clear main portal | 7.2 × 6.9 m |
| Roof structure | Five clear-span bowstring frames with I-section chords, paired angle webs, gussets, purlins and bearing connections |
| Rigging | Seven longitudinal rows, fourteen cross battens, hanger/clamp/turnbuckle connections; rear carriers above circulation headroom |
| Catwalk deck | 4.95 m high; supported side walks and rear crosswalk |
| Stair | Two 14-riser flights, 0.28 m going, 1.2 m deck and landings |
| Central floor reservation | 12 × 18.95 m (227.4 m²), with perimeter circulation/services |
| Sampled circulation headroom | 2.195 m minimum across 161 source samples |
| Sampled clear filming height | 6.263 m minimum across 40 source samples |

The interior illustration labels 8 m eaves, 12 m crown and a 7.6 m door. Those conflict with the already-established exterior envelope. This pass retains the approved architectural direction and translates the interior into it, so its vertical space is lower than the interior illustration's labels. Catwalks and the rear stair keep usable headroom; the rigging stops before the upper circulation zone.

The illustrations are not mutually consistent orthographic drawings. Viewpoints are approximate matches, not calibrated reconstructions. Stair location, structural spacing and service runs resolve the illustrated character into one connected building. Administration and lot positions are unchanged, so the supplied Administration composition is not reproduced exactly.

The clean materials intentionally omit the concepts' accumulated age and localized staining. The existing game lighting is flatter than the cinematic concept lighting, particularly on the enclosed floor and lower walls. Materials have UVs, normal detail, surface variation and distinct PBR responses, but final material/light-response tuning is still warranted. I would not describe the current Unity images as photorealistically equivalent to the illustrations.

## Validation and scope

- Editable source: 5,980 constructed components, 519,824 source triangles, 29 semantic exported meshes. Zero degenerate faces; source components closed/outward, unit transforms.
- Unity import, meshes, normals, UVs, tangents, material references, five broad colliders, main/front personnel wall openings, unobstructed central collider ray and floor level validated. Stage identity changed 1 → 2 → 1 successfully using the existing sign architecture.
- Nineteen materials, eight shared 1024px texture sets, 31 permanent spotlights and one 128px custom interior reflection cubemap. The source and Unity FBXs/textures match by SHA-256.
- 483 protected files match this pass's baseline, including original Stage, Administration, Studio, scripts, packages and rendering settings. 16 existing candidate GUIDs retained. The working tree was already extensively dirty; unrelated changes were preserved.
- Original Studio remains open with its unsaved user-placed candidate. A separate `ReviewSnapshots/Studio_before_production_review.unity` snapshot preserves that state. Studio itself was never saved or reloaded by this pass.

No Play Mode gameplay, player build, navigation rebake, walkable stair collision or multi-building performance benchmark was run. Source clearance sampling is geometric evidence, not a structural-load or acoustic assessment. Console details and resolved diagnostic/setup issues are retained in `unity_validation.json`.

The first native reflection bake omitted preview-scene geometry. The final bake uses a disposable regular-scene copy with reflection-static flags and an isolated culling layer; its six cubemap faces were inspected and contain the actual interior. Temporary copies are removed after baking. The final Unity Console read returned zero errors and zero warnings.

The Browser tool blocked the local file URL, so browser layout and category-button behavior were not verified. All 93 local image/document links were checked on disk, and the 22 Unity captures themselves were visually inspected. The temporary Unity preview was closed afterward; the original unsaved Studio remains open.

## Remaining after visual approval

Final material response and subtle condition/weathering; loose world dressing as separate assets; LOD/culling and light-budget optimization; minor polish; then a separately authorized migration into the live Stage gameplay layout. At roughly half a million triangles and 31 shadow-capable worklights, this is an inspection candidate, not a benchmarked many-building performance result. Low-resolution thin-edge/grating shimmer should be addressed with the later LOD/rendering work.

Original Stage remains the fallback. Administration is unchanged. No commit or push.
