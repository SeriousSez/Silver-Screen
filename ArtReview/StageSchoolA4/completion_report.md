# Stage School A4 — compact architectural/art review

A4 is a separate candidate ready for **manual architecture/art review**. The
latest roof correction is incorporated: one shallow-fall flat roof concealed
behind stone-capped stucco parapets, with the entrance façade slightly raised.
It supersedes the original terracotta-roof instruction. The building retains
A3.2's warm stucco, restrained masonry, arched entrance, dark window frames,
black/gold signage and walnut interior.

[Open the Unity gallery, including bench and foliage close-ups](review.html).

The forecourt follow-up rotates both bench instances 180 degrees toward the
public walk. Four planters now have eighteen curved leaves each, connected
petioles, fine midribs, subtle colour variation, rounded open rims and recessed
potting soil. Geometry and materials are authored in the A4 source; the shared
bench and upholstery assets retain their original meshes and materials.

The subsequent bench-clearance correction moves both instances 20 cm toward
the public walk, from Z = -9.38 to -9.58 m. Imported Unity geometry now has at
least **117.6 mm separation** from the projecting facade trim at bench height;
the previous placement overlapped that envelope by 82.4 mm. Close-ups 13–14
show both corrected placements. No mesh or material data changed in this pass.

## 1–7. Footprint, area, roof and room program

| Item | Final candidate |
| --- | --- |
| Main floor-slab footprint | **22 × 16 m** |
| Gross floor area | **352 m²**, about 40% below the brief's approximately 583 m² A3.2 |
| Net room area | **321.93 m²**, measured between inner wall faces, including furnishings/circulation |
| Roof masses | **1**; the raised entrance is a façade/parapet accent, not another roof mass |
| Hiring Hall | **21.36 × 7.05 m**, 150.59 m² |
| Audition / Screen Test Room | **13.19 × 8.09 m**, 106.71 m² |
| Interview Room | **7.99 × 8.09 m**, 64.64 m² |

This is the complete room list. There are no dedicated reception, indoor waiting,
common/portfolio, lounge, flexible evaluation, records, equipment store, service,
restroom, storage or staff/admin rooms.

The 22 × 16 m measurement is the main slab, not the full dressed site envelope.
Façade/coping projections, steps, rear downpipes and the applicant forecourt
extend beyond it. The complete prefab bounds are **23 × 21.22 m**, including the
public walk and drainage shoes; maximum height is **5.95 m**. Floor is around
0.36–0.38 m above ground and the ceiling underside is 4.60 m.

## 8–9. Circulation and exterior applicant space

The main entrance leads directly into the open Hiring Hall. Two obvious doors
lead directly to the specialist rooms, with no corridor. The audition doorway is
1.60 m wide and the interview doorway 1.35 m at the authored wall openings;
frames and leaves reduce the effective passage width.

Four **3.4 × 3.2 m** future region envelopes fit across the hall at Unity X
positions −7.2, −2.4, 2.4 and 7.2 m, centered at Z = −5.45 m. The fourth position
changes with carried-person context: Create/Import for applicants, Dismiss for
employees. The current asset has no region labels or interaction boxes. Floor
inlay is ordinary architectural finish.

Two **6.45 × 2.7 m** paved forecourt pockets flank the entrance, with shared
outward-facing period benches and detailed planters. Six standing positions are documented at
X = −9.2, −7.2, −5.2, 5.2, 7.2 and 9.2 m, Z = −10.55 m. The central approach is
7.55 m wide before the stairs; a 2.1 m public walk runs in front of both pockets.
No applicant or queue gameplay was implemented.

Two performers, camera/operator, evaluators, lights and a precise future Screen
Test position are documented in the Audition Room. Applicant/interviewer and
Interview positions are documented in the Interview Room. These coordinates are
in `ArtSource/StageSchoolA4/authoring_readiness.json`, **not runtime anchors**.

## 10–12. Reuse, new work and preserved unused assets

**Reused:** A3.2 front façade, arch/portal, entrance doors and fanlight, signage,
black/gold banners, lanterns, steps/rails, dark window construction, normal
two-sided doors and frames, opening-aware walnut dado, pendant, portrait gallery,
clock, upholstered applicant chairs, bookcase, audition platform, wide neutral
backdrop, approved straight-hanging burgundy curtains and evaluator table.
Shared `StudioCamera1930` and two `StudioLamp1930` instances retain their approved
meshes/materials. Period desk, chairs, filing cabinet, telephone, banker lamp,
paperwork and benches reuse existing prefab/library assets.

**New/reworked:** compact foundation and three-room layout; new side/rear walls
using approved opening components; new partitions, floor extents and ceilings;
single shallow-fall roof, perimeter parapets/coping, two scuppers and direct rear
downpipes; forecourt paving, modest planters, and furniture/portrait placements.
The roof has a 180 mm front-to-rear fall concealed by its parapet. It is simple
architectural drainage intent, not a hydraulic design certification.

**Deliberately preserved but unused by A4:** reception workstation and reception
dressing; waiting/lounge chairs and extra tables; restroom fixtures, mirrors and
dedicated cubicle doors; archive shelving and boxes; equipment-store furniture;
staff/workroom assemblies; service doors/architecture; old roof assemblies and
all reusable source components. These remain in the historical `.blend`, source
files, Unity meshes/materials/prefabs and libraries. Nothing was destructively
removed from A3.2 or the modular library.

## 13–16. Final imported geometry

| Unity count | Value |
| --- | ---: |
| Triangles, all rendered instances | **617,211** |
| Vertices, all rendered instances | **720,837** |
| Mesh renderers | **110** |
| Distinct materials | **35** |
| Unique meshes | 92 |
| Unique mesh triangles | 590,111 |
| Unique mesh vertices | 686,084 |
| Renderers directly sharing A3 meshes | 31 |
| Missing mesh/material references | **0 / 0** |

Counts include furniture, equipment and forecourt dressing; exclude the separate
review-ground cube and cameras. Unity uses the original mesh/material GUIDs for
reused assemblies. This reduction comes from the smaller composition and removed
room/roof instances; no runtime LOD derivation or old-B optimization was performed.

## 17. Files created/changed

All task-authored files are isolated under A4 paths; existing A3.2 and runtime
files are unchanged.

- `ArtSource/StageSchoolA4/`: reproducible `generate_stage_school_a4.py`, editable
  `StageSchool_A4.blend`, `generation_report.json`, `authoring_readiness.json`,
  preserved `CompactGameplayReference.png`, `README.md`, `verify_source.py`,
  `preserve.py`, immutable `preservation_baseline.json`, and `build_review.py`.
- `ArtExports/StageSchoolA4/stage_school_meshes.json.gz`: established semantic
  mesh packet, including original-assembly reuse metadata.
- `Assets/SilverScreen/Environment/StageSchoolA4/`: `StageSchool_A4.prefab`,
  `StageSchool_A4_Review.unity`, new architectural meshes, editor-only
  `Editor/StageSchoolA4Review.cs`, and Unity-generated `.meta` files.
- `ArtReview/StageSchoolA4/`: fourteen PNG captures, `review.html`, this report,
  source/Unity/preservation results, Studio-lighting provenance, import/capture
  logs and diagnostic evidence. See `files_created.json` for the exact inventory.

## 18–19. Preservation and validation

**A3.2 remains intact. Reusable modular assets were not destructively deleted.**
The immutable pre-A4 hash baseline covers **4,036 existing files**. The follow-up
comparison finds **4,035 unchanged**, including A3.2 sources/export, A1/A2/A3 Unity
assets, Stage School B and the reusable libraries. `Assets/Scenes/Studio.unity`
differs from that historical baseline: its modification time is 15:55:18 UTC,
before this follow-up's first Unity probe at 16:02:21 UTC. This follow-up did not
open or write Studio.unity, and the historical baseline has not been replaced.
The strict historical preservation check therefore reports that one difference.

The initial foliage-pass comparison confirms that **only `Exterior/Planters` mesh
data and the two bench rotations changed**. Existing A4 `.meta` files, other mesh
groups, equipment placements and retained material definitions match the
follow-up baseline. See `forecourt_validation.json` for that pass's evidence.
The bench-clearance pass changes only the two fixture positions; all 80 mesh
groups, material definitions, equipment placements, existing A4 asset identifiers
and 1,405 protected files match its fresh baseline. The clearance check uses
transformed imported mesh vertices and overlapping facade triangle envelopes,
not physics colliders. See `bench_clearance_validation.json` and the before/after
Unity measurement logs for the evidence.

Validation performed:

- Blender regenerated the new editable source and established packet together.
- Source QA passed for exactly three rooms/one roof, valid finite mesh data,
  valid indices, no degenerate exported triangles, translated source geometry,
  four-region envelope fit, six documented applicant positions, approved
  equipment identities, absence of permanent floor labels and roof clearance.
- Unity **6000.6.3f1**, **URP PC_RPAsset** imported and compiled successfully, with
  unit transforms, no missing references, no gameplay components and no LODGroup.
- The scene uses the actual Studio daylight sky, directional light and volume.
  Six review room lights supplement it. URP attaches its normal
  `UniversalAdditionalLightData` during rendering; that render metadata is
  explicitly distinguished from gameplay behavior in the validator.
- The initial ten actual Unity captures were visually inspected: front/rear, elevation,
  roof, full plan, cutaway, hall, audition, interview and applicant forecourt.
  Source corrections resolved the pavement overlap and roof-deck edge overlap;
  filing furniture is against the Interview Room side wall with access inwards.
- The forecourt follow-up refreshed all captures and added two detail views.
  Bench orientation, foliage, leaf shading, planter rim and soil were inspected
  directly in the Unity URP images; the front elevation checks all four plants.
- The bench-clearance pass measured both sides above the 100 mm minimum and
  visually inspected their new close-ups under the same Unity URP rendering.
- The existing working scene was kept open; its unsaved state was not discarded
  or saved. Review capture uses an isolated preview scene and restores visibility.

No Play Mode, navigation, actor blocking simulation, runtime cutaway, player
build, time-of-day test or general mesh-intersection proof was performed. The
candidate is an architecture/art submission, not runtime approval. Small trim and
floor joints can show subpixel aliasing in the captures. Artistic acceptance and
the final room proportions remain for the user's review.

## 20. Exact manual checks

1. Open `review.html`, then inspect **01, 03 and 04** at full size. Confirm the
   flat parapet roof and raised entrance match the corrected reference direction,
   and that the façade still reads as SilverScreen's Stage School.
2. Inspect **02 and 04**. Confirm the single roof, two rear drains, restrained
   rear treatment and lack of unnecessary roof masses are acceptable.
3. Inspect **05 and 06**. Confirm the three-room program and proportions; trace
   entrance → Hiring Hall → each specialist room without a corridor.
4. Inspect **07**. Assess the hall's open floor and portrait/walnut treatment.
   The four documented 3.4 × 3.2 m envelopes occupy the open central band; check
   that this is the right amount of space for the future carry interaction.
5. Inspect **08**. Check platform depth for one/two performers, curtain/backdrop
   relationship, the camera/operator lane, lamp placement, evaluator seating,
   and access from the doorway along the clear right side.
6. Inspect **09**. Check applicant/interviewer distance, desk access, chair
   grounding, inward access to filing furniture and the remaining circulation.
7. Inspect **10–14**. Check the six future exterior standing positions against the
   benches, stairs and public walk; verify applicants will be visible outside.
   Confirm both benches face outward and assess the foliage and planter details.
   Views 13–14 show their clearance from the projecting wall strips.
8. For free inspection, open
   `Assets/SilverScreen/Environment/StageSchoolA4/StageSchool_A4_Review.unity`
   after preserving any unsaved working scene. Do not enter Play Mode. Inspect
   the A4 prefab's roof, wall openings and furniture from both sides. Toggle
   `Roofs` and `Ceilings` only for review; revert those visibility edits afterward.
9. Compare against the preserved A3 review/master as needed, then approve or
   request architectural/art changes. **Stop here before runtime integration,
   LOD generation or gameplay.**

No commit or push was performed.
