# Stage School A3.2 — final manual corrections

The isolated A3 candidate retains the approved footprint, rooms, nine roof masses, normal doors/windows, audition backdrop/platform and approved camera/lights. This report includes the subsequent curtain, service-canopy and drainage-simplification instructions. Final art approval remains with the user.

| Requested item | Outcome |
| --- | --- |
| Front-right entrance downpipe | Resolved. Offset forward of the projecting pier, with wall anchors, collars and a visible shoe/gully. Final source and Unity inspection confirm the corrected front-right route is preserved. |
| Hall/Audition junction and roof drainage | Resolved. Local cap termination and flashing preserved; exposed internal cascade shoes, outlet fittings and supports removed. Internal routes use narrow recessed liners and valley/abutment flashing. Visible gutters/downpipes remain at external eaves. |
| Exterior stucco scoring | Resolved. Removed the upper scoring row and isolated front-field strokes across all elevations. Deliberate stone-base joints, corner blocks, surrounds and pilasters remain. |
| Reception workstation | Resolved. Preserved the curved walnut visitor shell; fitted a curved lower worktop, integral drawers/file cubbies and a 1.54 m knee opening. Phone, lamp, application trays and chair serve the staff side. No generic office desk inside the shell. |
| Dedicated cubicle doors | Resolved. Reusable 32 mm leaves with 210 mm bottom and 180 mm top clearance, partition-mounted hinges, inside privacy bolts and outside pulls/indicators. Both stalls inspected inside/outside. A3.1 fixtures and mirrors preserved. |
| Staff/workroom circulation | Resolved. Full-size desks now form a side-wall arrangement; both doorway approaches and the east link remain accessible, with a continuous 1 m route behind seated staff. Other fixed desk/table routes checked. |
| Audition curtains | Resolved to the final reference direction. Two continuous straight-hanging velvet wings with unequal broad folds, thick sewn edges, weighted hems and a straight pleated valance. Removed the tied waist and tieback hardware. Wide backdrop, opening, foreground relationship and intimate room identity retained. |
| Rear/service canopy | Removed entirely by the final user instruction, including roof, flashing, frame, posts and anchors. Back door, steps and wall lantern retained. |

## Verification

- Regenerated from authoritative Blender sources, imported into the existing isolated Unity candidate, and checked mesh/material references. The approved lamp instances still share their six meshes.
- Unity project compilation completed with `Compiling=False` and `CompileFailed=False`. Import and capture commands compiled and executed successfully. No Play Mode, gameplay tests or broad profiling.
- Source checks: finite vertices and valid indices; unchanged approved roof definitions/equipment; 502 protected files unchanged, including the protected Studio/prototype dependencies. Four pendant packets differ only through Blender normal rounding; positions/materials remain identical.
- Targeted geometry: curtain/platform and curtain/backdrop triangle intersection checks pass; cloth clears the platform and rear screen. The final follow-up removes the canopy assembly entirely. Internal cascade components are absent from the simplified drainage.
- Desk/table bounds identified one marginal staff desk-corner approach overlap. Plan and eye-level inspection confirmed a clear doorway approach and continuous circulation; no bounds-only auto-fix.
- Inspected the actual Unity captures: front and both oblique curtain/canopy views, elevated management views, two complete-roof angles, local roof junctions, entrance pipe, all stucco elevations, fitted reception front/rear, both cubicles inside/outside and staff circulation. Review-only roof visibility changes are restored after capture.

Drainage uses conceptual concealed construction routes; this is not a hydraulic simulation. This was a targeted correction pass, not a new general asset audit. Earlier A3/A3.1 gallery images are labelled historical context and do not supersede the new detail views.

## Final counts and review

Final Unity LOD0: **1,805,401 triangles / 2,631,815 vertices / 319 renderers / 37 materials**. Missing meshes: 0. Missing materials: 0. Scale valid; no runtime building components. Unity 6000.6.3f1, URP `PC_RPAsset`.

The drainage simplification reduced rainwater geometry from 111,966 to 86,104 triangles: **25,862 fewer triangles (23.1%)**. No broad LOD0 optimization was performed.

Review gallery: [review.html](review.html). It contains 27 targeted A3.2 views, with the final roof and curtain views first, plus labelled historical context.

## Final downpipe consolidation

At the user's request, visible ground drops were reduced from 15 to 7. All three front-facing drops and five redundant side/return drops were removed at source, together with their sockets, supports and gullies. Retained gutters feed shared side/rear outlets through discreet boxed-fascia/concealed collection logic. Nine roof masses are unchanged. This supersedes the earlier front-right pipe correction.

Regeneration, Unity import/command compilation and seven-outlet/nine-roof assertions passed. All eight refreshed exterior/entrance captures were visually inspected. No missing meshes or materials. This follow-up removes 26,224 triangles and 36,383 vertices.

## Changed files

- Authoritative source: `ArtSource/StageSchoolA3/generate_stage_school_a3.py`, `known_defects.py`, new `manual_corrections.py`, new `drapery_service_finish.py`, and `README.md`.
- Validation: `verify_a32.py`, `a32_local_geometry.py`, `a32_shoe_check.py` (historical shoe correction), `verify_drapery_canopy.py`, `verify_simplified_drainage.py`, plus immutable A3.2 baseline and pendant-reference evidence.
- Generated candidate: `ArtSource/StageSchoolA3/StageSchool_A3.blend`, `generation_report.json`, `ArtExports/StageSchoolA3/stage_school_meshes.json.gz`, and existing A3 Unity meshes/prefab/review scene/reflection data under `Assets/SilverScreen/Environment/StageSchoolA3` with existing GUIDs retained.
- Review: `ArtSource/StageSchoolA1/build_review.py`, the two A3.2 capture scripts, targeted PNGs, gallery, this report and scoped validation JSON/logs under `ArtReview/StageSchoolA1`.

No commit or push. No Stage School B, LOD1/LOD2, runtime cutaway, navigation, construction integration or gameplay work.

No known requested production-art defect remains after the targeted final inspections. Final freeze approval remains with the user.

**LOD0 appears ready for manual freeze review.**
