# Studio Services — reported clipping corrections

2026-09-29 · Unity 6000.6.3f1 / URP · Blender 5.0.1

Status: the reported defects are corrected and the focused validation passes. User visual approval remains separate from these checks.

[Matched before/after review](review.html) · [Source measurements](source-clearances.json) · [Unity preflight](unity-preflight.json) · [Test results](tests.xml) · [Exact file manifest](implementation-manifest.json)

## Corrections

| Reported area | Source correction | Final evidence |
|---|---|---|
| Storage sacks embedded in crates | Moved both sacks to the clear rear storage strip; retained crate stacks, shelving and supply anchors. | At least 0.393 m axis separation from adjacent crates; sacks mutually separated by 0.162 m. Overhead Unity view inspected. |
| Yard barrel under vise | Moved the barrel 0.50 m forward. | 0.181 m separation from the complete workbench/vise mesh bounds. Front and overhead Unity views inspected. |
| Yard crates / workbench and projecting window sill | Moved the two stacked crates 0.30 m forward, then 0.23 m outward from the wall after the user's follow-up exposed the missed sill collision. | The first correction left 0.0784 m overlap with the sill. The final stack has 0.1516 m sill clearance and retains 0.449 m worktop clearance. Nearby barrels, wheelbarrows and ladder remain separated by at least 0.5446 m. Matched Unity sill views and the overhead yard view inspected. |
| Awning brackets / entrance trim / lamp | Centred the hood on the opening; moved both bracket assemblies outside the architraves; raised this lamp instance 0.11 m. Lowered brace tips and replaced round bearing pieces with formed straps beneath the curved sheet. | 0.0375 m minimum lateral trim clearance, 0.1075 m imported lamp/hood vertical clearance; source vertices do not pierce the hood's outer skin. Front, both sides and above inspected in Unity. |
| Bench supports piercing timber | Reworked the cast supports below the seat and routed the back supports around the rear seat edge, following the slat rake. Updated this bench's collision envelope. Moved the installed bench 0.20 m forward after the reverse view exposed a window-sill clash. | No unwanted support/slat triangle intersections; seat underside bearing retained. Complete bench/window bounds separated by 0.083 m. Seat and rear views inspected in Unity. |
| Workshop drop bolts / diagonal braces | Mounted the bolt assemblies on the meeting stiles, with backplates, saddles and guide collars. | No triangle intersections between bolt assemblies and either diagonal timber. Front and oblique Unity views inspected. |

## Reproducible source and scope

Edited authoritative sources:

- `ArtSource/PeriodEnvironment1930/fidelity.py`: bench and workshop door hardware.
- `ArtSource/PeriodEnvironment1930/final_qa.py`: awning supports.
- `ArtSource/StudioServices/generate_production.py`: prop spacing, bench position, awning alignment and office lamp placement.
- `Assets/SilverScreen/Environment/StudioServices/Editor/StudioServicesFinalQAReview.cs`: focused repeatable review captures.
- Added `ArtSource/StudioServices/validate_collision_clearances.py`: geometric regression checks for these reported defects and the adjoining sill/hood clearances.

Regenerated the canonical kit `.blend` and packets, linked Studio Services production `.blend` and packets, and their established Unity meshes/prefabs/definitions through `StudioServicesProductionBuilder.Build()`. No one-off edits to generated geometry. The manifest records every generated-file difference, including canonical assets rewritten by the existing importer.

The approved massing source, room layout, openings and all 19 semantic anchors remain unchanged. No employee dimensions, gameplay rules, Stage 1, Administration, project settings or material source changes were made. No scenes were saved by this correction.

## Validation

- **14/14 source checks passed** in `validate_collision_clearances.py`, including new checks for the yard crates against the projecting side-window sill and neighbouring movable props. The sill check reproduced the missed clash before the placement correction. Intentional fasteners and the seat's underside bearing contact are distinguished from unwanted intersections. Bounds checks are conservative separation checks, not claims of general geometric collision validation across the entire library.
- **Export validation passed**: 63 canonical kit meshes (325,262 triangles) and 13 building meshes (174,834 triangles); zero degenerate triangles, valid indices/attributes; approved source hash and anchors unchanged. Source dependency hashes match the generators used.
- **37/37 Unity preflight checks passed** via `StudioServicesProductionReview.Run(false)`: 19 anchor clearances with the existing 0.70 m / 2.00 m employee profile; nine complete navigation paths; live identity and canonical references; cutaway ownership and restoration; unchanged physical collision during reveal. 181 shared instances, 790,108 assembled rendered triangles. The open scene was preserved.
- **Imported lamp/hood check passed**: 0.1074998 m vertical separation, measured from the actual Unity prefab renderer bounds.
- **42/42 focused Unity tests passed**, zero failed/skipped. Fresh result time: 2026-09-29 12:17:20Z–12:17:40Z, rerun after the yard-sill correction. This is the current run, not the earlier milestone result.
- The suite includes `ActualStudioArrivalCarryCutawayHireAndInteriorRoute`, which entered actual Play Mode in Studio: empty studio, physical applicant arrival, hold/release carry, Escape restoration, separate-click immediate authored hiring into both professions, retained person identity, and physical office/workshop/storage routes with per-frame capsule obstruction checks.
- Inspected the imported URP result through matched detail captures, including the final bench rear, hood above, yard overhead, storage overhead, door hardware and front entrance. Fifteen final detail views are supplied, eleven with original pre-correction views and the new sill view matched against `YardSillBefore`. The sill and overhead views were inspected again after the outward crate adjustment. The preflight also refreshed management, front, yard and cutaway captures in the existing Production review directory.
- Unity command compilation/import and the focused tests succeeded. Existing obsolete-API warnings and editor licensing messages were not changed or suppressed. No standalone player build or unrelated full-project suite was run for this localized art correction.

## Preservation

The pre-interruption baseline contains 9,573 files. Comparison and SHA-256 evidence are in `implementation-manifest.json`; existing `.meta` files/GUIDs are preserved.

One unrelated difference from that earlier baseline was observed during the resumed session: `Assets/Scenes/Studio.unity` was saved at 11:35:48Z while the editor changed from an unsaved Studio scene to the clean ProductionReview scene. The correction commands did not save Studio. That current file was captured before runtime validation and remained byte-identical afterward. It was retained, not reset to the older baseline. The manifest distinguishes this external scene change from owned corrections.

Tests returned to Edit Mode, and Unity was returned to the original clean Studio Services production review scene without saving it. No commit or push was performed.

## Review limits

These checks close the six reported areas and the directly adjacent clashes discovered while inspecting them. They are not a certification that every prop in the entire environment kit has no remaining defect. No geometry budget, LOD, broader art-style or gameplay-system redesign is included.
