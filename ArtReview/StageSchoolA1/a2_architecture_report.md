# Stage School A2 architectural candidate

30 September 2026. **Architectural review pending; not final LOD0 approval.** Generated, imported and visually inspected in Unity. Known defects are recorded below. Work stops at the requested review milestone.

1. **Previous limitations.** Replaced the 22 x 18 m rectangle with purposeful west support and east audition wings, a common bay and rear interview setback. Specialist rooms have more space; all elevations receive modeled articulation and varied heights.

2. **Dimensions.** Nominal masonry envelope **28 x 24 m**, nonrectangular union area **583 m²**, versus 396 m² (+47.2%). One occupied floor at 0.36 m. Imported bounds including steps/overhangs: **29.077 x 26.247 m**, maximum height **7.606 m**, minimum Y 0. These are architectural bounds, not a runtime placement footprint.

3. **Massing.** Retained ceremonial entrance, lower public wings, raised central hall, transverse evaluation/staff wing, subordinate service wing, common bay, recessed interview room and taller/deeper audition wing. Upper volumes are clerestories/roof voids, not another gameplay floor.

4. **Front.** Original arch, fanlight, bronze/glass entrance doors, lettering, banners, lanterns and steps retained. Ten exported front/entrance groups passed coordinate-preservation checks. Separate trim integrates them with the expanded wings.

5. **Left.** Common projection and paired evaluation/staff windows break the old long wall. Lower records/service volume adds another depth/eave step. Corners, plinth, surrounds and cornices provide relief.

6. **Right.** Audition projects beyond the public wing and farther rearward. Tall windows plus high glazing express its larger ceiling volume. Articulated base courses and corners continue around it. Downpipe/window conflicts remain; see item 24.

7. **Rear.** Unequal wings flank the recessed interview room. Working entrance has a supported canopy and steps, with louvers and connected utility details. Three blind masonry/stucco bays articulate the audition backdrop wall.

8. **Roof.** Nine named hip-roof masses use different spans and eave heights. Reused A1 tile-course/variation approach, ridge/hip caps, formed abutment flashing, open gutters, outlets, supported downpipes and shoes. Dense roof composition and junctions remain manual-review concerns; import success is not construction certification.

9. **Masonry/stucco.** Scored lower courses, expressed foundation/plinth, restrained corner blocks, pilasters, inset wall fields, cornice/string courses, lintels and projecting sills. Existing warm stucco, stone, terracotta, metal, wood and glass materials reused, with no additional material palette.

10. **Rooms.** Reception; applicant waiting; common/portfolio area; contextual hall; talent preparation/lounge; audition; Director Interview; flexible evaluation; staff office; records/archive; equipment store; two restroom cubicles/washstand; service passage and covered rear access. Extents: [source design](../../ArtSource/StageSchoolA2/design.md).

11. **Reception.** Curved walnut counter, telephone, banker lamp, trays and accessories retained. Routes pass both sides, with clear visual connection to interview/audition doors.

12. **Waiting/common.** Eight waiting seats and reading table retained. Separate six-chair portfolio table serves the common area/west projection. Headshots and noticeboard moved to new walls. Useful paperwork retained without duplicating it throughout the expansion.

13. **Audition.** About **10.5 x 13.5 m clear**, approximately 142 m² versus 56 m² previously. Depth sequence in source coordinates: evaluators Y4.0, operator allowance around 6.3, camera 7.8, lamps 11.0, performers around 13.4. Three evaluator chairs. Existing **5.6 x 2.6 m rostrum**, **3.1 m backdrop** and velvet assembly translated together without scaling/remodeling. Two performers plausibly fit. West-side access connects entrance to stage; actual actor navigation is untested.

14. **Director Interview.** About **5.7 x 6.5 m clear**. Existing desk, two visitor chairs, telephone, papers, banker lamp, filing, script library and clock retained. Smaller than audition. Shelf/window and picture-rail/clock placement need correction.

15. **Flexible room.** About **6.55 x 4.5 m clear**. Discussion table, three chairs, filing and paperwork; no permanent gameplay labels. Doors connect common space and staff circulation.

16. **Staff/support.** Two desk positions in roughly **3.8 x 4.5 m**. Separate records/archive and equipment rooms off a nominal 1.9 m service passage. Existing racks, cupboard, boxes and filing redistributed. Covered rear access separated from reception. One archive rack extends through the passage wall and needs repositioning; support layout is not fully resolved.

17. **Contextual readiness.** About 48 m² nominal central hall behind reception, with additional readable common/preparation floor areas. No overlay regions, floor labels, activity markers or collision/door-conflict validation implemented.

18. **Cutaway authoring.** Separate directional shells, windows, raised sections, nine roofs, ceilings, partitions, individual door pivots/hardware and room furnishings. Attachments follow supporting groups; drainage belongs to roof assemblies. Capture visibility changes are review-only. Runtime recursive cutaway is unimplemented/untested.

19. **Construction authoring.** Foundation, shell, upper volumes, roofs and finishes remain separate for later authoring. No construction derivatives; framing/staged construction completeness not validated.

20. **A1 preservation.** Approved camera/lamp source/export/Unity assets unchanged. Two lamps share six meshes. Curtain/rostrum/backdrop passed translation-only geometry checks with 0.02 mm tolerance. Front groups, eight waiting seats, materials, tile variation and useful dressing retained. **42 protected hashes match**, including Studio.unity, equipment and A1 generator/blend. A1 remains available. Existing unrelated working-tree changes left alone.

21. **LOD0 counts**, including furnishings and three equipment instances:

| Measure | Refined A1 | A2 |
|---|---:|---:|
| Triangles | 935,108 | **1,227,313** |
| Vertices | 1,079,901 | **1,535,494** |
| Mesh renderers | 133 | **278** |
| Unique materials | 36 | **36** |

Triangle growth 31.2%, versus area growth 47.2%. Source export alone: 222 groups, 1,025,992 triangles, 1,306,340 vertices; shared fixtures/equipment explain the difference. Blender has 3,266 authored mesh objects. Repeated boards/caps batched within semantic groups; no LOD simplification. Renderer count remains heavy and unprofiled.

22. **Files changed for A2.** New `ArtSource/StageSchoolA2/`: design, generator, blend, generation report, preservation baseline, source sanity script and README. New export `ArtExports/StageSchoolA2/stage_school_meshes.json.gz`. New meshes/prefab/review scene/metas under `Assets/SilverScreen/Environment/StageSchoolA2/`. Updated existing importer `Assets/SilverScreen/Environment/StageSchoolA1/Editor/StageSchoolA1Review.cs` and gallery builder `ArtSource/StageSchoolA1/build_review.py`. Existing review directory receives gallery/report, 14 PNGs and generation/import/capture/sanity evidence. Previous report saved as `a1_refinement_report.md`. No commit or push.

23. **Captures and checks.** [Fourteen-view gallery](review.html). Actual 1920 x 1200 Unity URP images, isolated review scene using Studio environment setup and `PC_RPAsset`, Unity 6000.6.3f1. Individually inspected all final captures: `a2_01_front`, `a2_02_left`, `a2_03_right`, `a2_04_rear`, `a2_05_front_three_quarter`, `a2_06_rear_three_quarter`, `a2_07_roof`, `a2_08_floor_plan`, `a2_09_reception`, `a2_10_waiting_common`, `a2_11_audition`, `a2_12_director`, `a2_13_flexible`, `a2_14_staff_support` (all PNGs in `ArtReview/StageSchoolA1/`). Source mesh/index sanity, import/command compilation, references, scale and basic grounding passed; zero missing meshes/materials. Evidence: `a2_source_sanity.json`, `a2_unity_sanity.json`, `a2_unity_result.json`, `a2_capture_result.json`. No full tests, Play Mode, integration or broad profiling.

24. **Remaining concerns.** Visible defects requiring follow-up: downpipes cross several window bays; archive rack intrudes into rear access; picture rails/paneling cross openings and the Director clock; bookcase obstructs a Director window; some relocated trim has exposed ends. Flexible-room close view is partly obscured by its open door; floor plan/support view supplies context. Architectural judgment remains on the dense nine-hip composition, audition intimacy, rear detailing and fidelity to the richer concept. Roof junctions/drainage need closer construction review after massing approval. Material microdetail is not fully legible at overview distances. Runtime lot fit, navigation, contextual regions, construction, cutaway and LOD optimization remain deferred. **Stopped for user review; Stage School B has not begun.**
