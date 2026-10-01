# Stage School A3 — production-art cleanup

2026-09-30. A3 preserves the approved A2 footprint, room program, one playable floor, expanded Audition room and nine roof masses. This is an improved isolated candidate, not a freeze approval. Studio Services and Stage 1 informed the construction/detail benchmark.

1. **Major defect classes:** incorrect shared bevel profile, incomplete reverse faces, outside-facing hardware, trim crossing openings, roof intrusion, reversed furniture, door conflicts and unfinished utilities. Corrections target reproducible generators.
2. **Doors/windows:** rebuilt 15 door leaves, including two cubicles, with thickness, both faces, casing and hardware; corrected selected swings/pivots. Rebuilt 39 window assemblies with reveals, glazing, sills and interior-only handles. Entrance fanlight retained. Cubicle hardware still needs finishing.
3. **Wainscot/trim:** rebuilt 62 opening-aware wall spans with baseboards, recessed panels, stiles, rails and caps. Removed conflicting picture-rail fragments. Hall upper trim remains imperfect.
4. **Roof/drainage:** preserved nine A2 roof definitions; clipped interior eave/tile/flashing intrusions and corrected local ceiling/partition closures. Moved pipes clear of windows and rear electrical equipment onto a solid pier. Roof junctions, supports/outlets and complete collection routing remain unfinished.
5. **Furniture/props:** corrected filing, racks, equipment cupboard, staff desks/accessories, archive box grounding and Director bookcase/window clearance. Approved shared furniture retained where suitable.
6. **Armchairs:** rebuilt all 14 green chairs with rounded upholstery, seams, completed rear shells, wood aprons, tapered legs and glides. Front/rear inspected.
7. **Reception:** retained curved walnut desk; added staff drawers, handles, modesty panel, supports and knee space. Corrected chair/phone orientation and lamp placement. Both sides inspected.
8. **Restrooms:** added hollow bowls/basins, seats, cisterns, faucets, visible traps, partition supports and two-sided doors. Mirror appearance, cubicle hardware and cistern/seat junction still fall below the required standard.
9. **Audition:** retained large A2 room and approved StudioCamera1930/StudioLamp1930 meshes. Added 5.65 m backdrop, foreground burgundy curtains with supports/valance/tiebacks and 7.5 m platform. Repositioned performers, lights, camera/operator and evaluators into the requested sequence.
10. **Entrance/signage:** retained arch, fanlight, bronze/glass doors and cast serif letters; refined steps/nosings, continuous bronze rails, arch returns, lettering centering/depth/mounting. Removed flat red runner. Foremost rail-post anchoring remains defective.
11. **Known A2 issues:** corrected pipe/window crossings, archive wall/circulation intrusion, opening-crossing paneling/picture rails, Director clock conflict and obstructing bookcase. Removed legacy orphaned trim; remaining Hall finish prevents claiming every trim end is resolved.
12. **Additional findings:** fixed shared bevel profile causing concave corners, backwards files/cupboard, unsupported archive boxes, service-door/workstation conflicts, low partitions and electrical equipment on a window. Final audit independently found the remaining defects below.
13. **QA:** source sanity, 502 protected-file hashes, preserved macro geometry, finite mesh/index/degenerate checks, opening/drainage projections, room bounds, sampled door/furniture arcs, roof triangle-centre tests, Unity import/compile/material/reference checks and full room/elevation visual audit. One corrective art pass followed initial inspection; final captures inspected. Subsequent changes only adjusted review cameras. No gameplay tests, Play Mode or broad profiling.
14. **Remaining defects:** roof junction/collection continuity; projecting gutter supports/incomplete outlets; low pendant mounting roses; Hall soffit/clerestory finish; entrance rail anchor; restroom mirrors, cubicle hardware and cistern/seat junction. See evidence below.
15. **Final Unity totals:** **1,771,295 triangles; 2,598,145 vertices; 317 MeshRenderers; 36 unique materials.** Includes shared furniture and approved camera/two lamps. Zero missing meshes/materials. Unity 6000.6.3f1, URP PC_RPAsset, Edit Mode.
16. **Gallery:** [review.html](review.html), 36 actual 1920×1200 Unity captures. [Audit matrix](a3_visual_audit.md). Prior report preserved as [a2_architecture_report.md](a2_architecture_report.md).
17. **Files:** new source/checks/Blender result under `ArtSource/StageSchoolA3/`; export under `ArtExports/StageSchoolA3/`; isolated meshes/prefab/scene/metas under `Assets/SilverScreen/Environment/StageSchoolA3/`. Updated `Assets/SilverScreen/Environment/StageSchoolA1/Editor/StageSchoolA1Review.cs`, `ArtSource/StageSchoolA1/build_review.py` and existing review gallery/reports/captures. [File inventory](a3_files_changed.txt). Unrelated existing workspace changes excluded.

## Remaining defects and evidence

| Defect | Capture numbers | Future correction |
|---|---|---|
| Entrance gutter brackets project beyond fascia; incomplete pipe anchors/outlet shoes | 02, 15, 34 | Correct support direction, wall anchors and collector/shoe construction |
| Overlapping roof caps/tile fields; incomplete valleys/collection route including Hall collector | 07, 35 | Finish local junctions and trace every roof to an outlet without changing massing |
| Pendant roses below ceilings, with rods above them | 11, 27, 31, 33 | Seat mounting roses against actual ceilings |
| Exposed Hall soffit/trim strip and partly obscured clerestory reveals | 33 | Finish local returns/reveals |
| Foremost entrance rail flange at incorrect tread level | 15 | Anchor fully to the correct tread surface |
| Dark opaque mirror appearance, incomplete cubicle hinge/strike/privacy hardware, poor cistern/seat junction | 23–25 | Finish material, hardware and porcelain/seat construction |

Source QA returned zero drainage/window projection conflicts, gallery/door conflicts, room-bounds candidates, sampled door/fixed-furniture swing candidates and roof triangle centres inside occupied-room prisms. These scoped checks do not prove complete collision freedom or water routing. The outward Staff door leaves roughly 0.87 m nominal passage width; navigation/accessibility was not certified.

All 502 protected baseline files remained unchanged. No prototype replacement, Studio.unity save, runtime cutaway, navigation, contextual regions, gameplay, recruitment, Star Maker, LOD1/2, commit or push. The one corrective art pass is complete. Final approval remains the user's.

**Known production-art defects remain; LOD0 should not yet be frozen.**
