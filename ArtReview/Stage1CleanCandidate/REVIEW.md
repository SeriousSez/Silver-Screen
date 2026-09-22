# Stage 1 — defect correction and material polish, revision 04

**Visual master approved by the user on 2026-09-22**, including both subsequent clearance corrections. [Approval, commit scope and retained limitations](MILESTONE.md). The following review preserves the evidence and status recorded before approval.

**Gutter/eave follow-up:** [Current rainwater correction, Unity views and validation](GutterClearance/REVIEW.md). The review below records preceding snapshots.

**Finial clearance follow-up:** The four ornaments were subsequently corrected after user review. [Latest before/after views and clearance report](FinialClearance/REVIEW.md). The 47 captures and validation below describe the preceding snapshot.

**Stopped for final visual approval.** [Open all 47 Unity views beside the approved concepts](review.html).

The approved production architecture is preserved. The work corrects construction, grounding, openings, hardware, services, blackout operation, awnings, corner crowns and roof material response. The revision 03 review is preserved in [Revision03](Revision03/review.html).

## Requested issues

| Issue | Resolution / intentional behavior | Evidence |
|---|---|---|
| 1. Ground datum | Source GROUND=0 is the lowest exported surface; the hidden 20 cm subgrade floor is removed. Wall/foundation contacts, bollard plates, column feet, stair feet and all threshold aprons reference the source datum. The existing live candidate is seated as one whole instance against the actual Ground collider, with Undo; no individual mesh offsets. | 23–26, 39, 45–47; source contacts; live placement log |
| 2. All personnel/service openings | All seven assemblies use usable sloped thresholds and hinge placement that clears the masonry. Connected rebated stops and compression/bottom seals close the required leaf swing gaps. The previous base-course and above-door conduit correction is preserved. Every leaf and its moving surface hardware was swept 0–95° in 5° increments against surrounding construction; zero detected intersections. Fixed steel jambs/stops are included; intentional hinge/latch engagement and compressible seals are excluded. | 27–28, 40–44; seven source sweep records |
| 3. MasonryFraming remnant | Opening exclusion now operates on full 3D volumes through the facade depth. It removes the four base-control-joint remnants at side doors, including elements visible only obliquely. | 28 and all side-door captures; source exclusion records |
| 4. Lamps versus columns | Six interior lamps and their feeds move together to clear wall bays. All 15 wall-lamp assemblies are checked against primary steel, catwalks, rigging, canopies and acoustic treatment; no detected intersection. | 29, 09, 12; fifteen source lamp checks |
| 5. Electrical/service routing | Retained the infrastructure and added missing sleeves, glands, conduit supports and guide saddles. Cabinet entry glands bridge the former 15–20 mm gaps; exterior lamp/awning mounts now contact their backing surfaces. Distribution feeds clear absorbers, the front-right lamp feed goes outside the canvas, rear junction covers face outward, and rear lamp feed reaches its backplate. Open half-round gutters and formed drip edges feed the existing downpipes, with dark shoe mouths and seated splash blocks. Wall penetrations intentionally lead to concealed chases. | 21–22, 28–29, 32, 41–44 |
| 6. Electrical panel controls | Three independent controls per interior panel now have bolted escutcheons, stepped bronze bearings, transverse pins/retainers, forged levers, shaped insulating grips and mechanical stops. Lever states vary; grip dimensions were corrected after close Unity inspection to keep controls separated. | 30 |
| 7. Personnel pull/lock | The old brass rectangle represented a lock without readable hardware. It is replaced across all seven doors by a keyhole escutcheon, mortise edge plate and bolt engaging a frame strike, plus an interior thumbturn. Curved pulls are retained. | 31, 27–28, 40–44 |
| 8. Canvas canopies | Both existing entrance canopies now use thick, tensioned canvas with restrained drape, sewn seams, front valance and edge lashings. Steel ribs, wall clamp bars and triangular brackets provide a visible support path; no canopy is added at other doors. | 32, 14, 44 |
| 9. Pilaster crowns | Restored four end-pier stepped coping lips, small plinths/necks, collars and hipped finials from the concept. Intermediate piers remain unornamented. | 33, 01–04 |
| 10. Roof PBR | Roof metallic/roughness adjusted to 0.78/0.36 with subtle rolled-metal microtexture and varying smoothness. Exterior roof renderers sample an exterior reflection anchor rather than the enclosed interior probe. Direct, shaded, grazing and medium views show broad highlights and seam response; no highlights or lighting are baked into the texture. | 01, 05, 19, 34, 37–38 |
| 11. Roof blackout | The old continuous panels plus fixed lining could not operate and intersected purlins. Compact opaque roller sections now run in curved guides within the cavity above primary steel, with overlap seals, gear housings and pole-drive eyes. Timber lining stops outside glazing; glazing/roof/frame geometry is retained. Zero checked structure intersections and all 290 coverage rays blocked. | 35, 19; source coverage/intersection checks |
| 12. Clerestory blackout | Existing rollers are retained. Fabric guides now align with the roller tangent and gain wall saddles; a stowed fabric hem, lower bar, gearcase and operating chain show how the curtain covers the opening. Full width/drop are recorded; zero checked structure intersections. Rollers intentionally remain stowed in review. | 36, 18; five coverage schedules |
| 13. Material polish | Retained the restrained stucco, base, timber, painted/structural steel and brass responses; added heavy taupe canvas and a roof-specific metal map. Nine shared PBR sets use subtle variation; all existing material GUIDs remain. No heavy weathering, cracks, stains or baked directional lighting. | 01–22, 30–38; material/texture reports |
| 14. Concept audit | Inspected the standard 22 Unity views against both concept sheets, then 25 focused views covering all seven personnel openings, eight low perimeter angles, lamps, hardware, cloth, crowns and sun response. Approved massing, portal, trusses, rigging, circulation and clear floor remain. | 47 native Unity captures in gallery |
| 15. No optimization | No LODs, broad performance work, material consolidation or reusable prop extraction. Source remains the detailed visual master; original Stage and Administration are unchanged. No commit or push. | Protected-file and GUID hash reports |

## Additional defects corrected

The audit also found threshold strips intersecting closed leaves, overlong stringer profiles below the ground datum, millimetre gaps below landing feet, conduit passing through a canopy and acoustic faces, reversed/floating junction covers, closed eave pipes with no rain-collection opening, control grips crowding each other after enlargement, unsealed leaf-clearance gaps, and small gaps at cabinet cable entries/exterior lamp mounts. These were corrected at source. Final validation also exposed duplicated generated lighting roots caused by saving an edited prefab instance back to its own prefab. The review wrapper is now unpacked before editing, all old generated light roots are replaced, and two consecutive refresh/save operations plus reflection bake retain exactly one root with 31 lights. The final captures were rebuilt after this correction. The fixed roof lining and purlin interference made the previous blackout claim incomplete; the final source explicitly models a roller mechanism that fits the cavity.

## Files and reproducibility

Authoritative source changes: `ArtSource/Stage1CleanCandidate/generate_stage1_candidate.py`, `production_geometry.py`, `production_interior.py`, new `production_corrections.py`, and `generate_materials.py`. The editable blend, source reports, roof texture set, both identical FBX exports, candidate prefab/materials/reflection and isolated review scene were regenerated. `Assets/SilverScreen/Environment/Stage1CleanCandidate/Editor/Stage1CandidateReview.cs` now assigns the exterior reflection anchor and reproduces the complete correction-view set through menu **6 Capture final correction review**.

## Actual validation

- Blender: 6,699 closed, outward components; 630,004 triangles; zero degenerate faces; unit scales; 29 semantic meshes. Minimum source Z is exactly 0.
- Seven personnel leaf sweeps, 15 wall-lamp assembly checks, roof and clerestory structure checks, 290 roof-coverage rays. These check actual source meshes, not feature-existence flags. Door sweeps sample 5° intervals and include fixed steel jambs/stops, excluding intended hinge/strike and compression-seal engagement; they are not gameplay animation tests.
- Existing planning checks retained: 161 circulation samples, 2.195 m minimum; 40 filming-floor samples, 6.263 m minimum.
- Unity MCP: imported model, material mappings, mesh channels, five existing broad colliders, floor and openings, instance sign 1 → 2 → 1, reflection, editor compilation and native URP captures. Console result: 0 errors, 0 warnings. No Console clear.
- 483 protected files unchanged; 46 existing candidate GUIDs retained. Export and texture copies match by SHA-256.
- The temporary preview was closed. Original Studio was neither saved nor reloaded. Its previously placed candidate was lowered from Y=0.200003 to the actual ground at Y=0 as one Undo-recorded source-datum correction. Original Stage, Administration and unrelated unsaved work are preserved.
- Every native Unity capture was inspected. Gallery links were checked on disk. Browser layout was not re-tested because browser automation previously rejected the local file URL.

## Intentional choices and remaining limits

Clerestory blinds remain rolled up to show the glazing and interior in review; their guides/cloth span the full opening when lowered. Roof blinds are closed and light-tight at the sampled locations. Wall-mounted cabinets intentionally sit above grade and connect through sleeves into concealed wall chases. Finials appear only on the four end piers; the smaller front-left entrance intentionally has no awning.

The master retains the approved exterior envelope (15.8 × 24 m, 7.10 m eave, 10.77 m roof crown) and the established interior planning, including the lower height than the interior concept's dimension labels. Views approximate the concept cameras; the existing lot composition, clean materials and game lighting remain less cinematic than the illustrations. Small exposed details/grating can still alias at management resolution. The probe is a 128px interior reflection; this is not a baked-GI or photographic lighting pass.

Door/blackout motion, walkable stair/catwalk collision, navigation, Play Mode, a player build, structural/acoustic simulation and many-building performance were not validated or integrated. They remain outside this visual-master pass. No optimization, LOD generation or prop extraction was performed.

Original Stage remains the fallback. Administration is unchanged. **No commit or push.**
