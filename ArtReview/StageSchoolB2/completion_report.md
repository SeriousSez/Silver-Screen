> Historical B2 integration report, before the user-requested frontage revision. Current frontage, anchor and validation evidence: [frontage_revision.md](frontage_revision.md). Dimensions and measurements below describe the original B2 revision.

# Stage School B2 — A4 runtime integration

The compact A4 is now the Stage School production resource. Functional integration passed in Unity 6000.6.3f1 with PC_RPAsset. The approved art is unchanged: 1,574 protected source, export, asset, library and Studio scene files match the pre-B2 hashes. No gameplay was added, and nothing was committed or pushed.

Review images are in [review.html](review.html). Actual runtime evidence is in [live.txt](live.txt), [live-results.xml](live-results.xml), and [edit-results.xml](edit-results.xml). The final run passed; 24 focused Edit Mode tests passed. A separate sustained-input regression also passed. An initial integration attempt exposed test-harness issues, retained in `integration-first.txt` / `integration-results.xml`; it is not counted as a passing run.

1. **Runtime architecture.** New `Assets/SilverScreen/Environment/StageSchoolB2/Resources/StageSchool_A4_Runtime.prefab`, derived directly from A4, with three visual branches, one shared physical hierarchy, door state, anchors, cutaway, interior visibility and construction bindings. The 22 × 16 m slab, single floor, flat parapet roof, raised entrance façade, three rooms and forecourt remain. Stable IDs are `building.stage-school` and `content.stage-school.1930`. The historical B prefab was moved to `StageSchoolB/Archive` with GUID `6a9044b1d07c375488e32234f75b8d70` preserved; it is no longer a Resources entry.

2. **Measured geometry.** Counts include every rendered instance, not just unique mesh assets. Mesh-byte estimates include vertex/index buffers, excluding textures, engine overhead and transient fade materials.

   | Representation | Triangles | Vertices | Renderers | Materials | Material slots | Mesh buffers |
   |---|---:|---:|---:|---:|---:|---:|
   | Approved A4 | 586,310 | 698,133 | 110 | 35 | — | — |
   | Runtime LOD0 | 466,804 | 629,295 | 110 | 35 | 295 | 33,814,464 B |
   | Runtime LOD1 | 151,894 | 254,025 | 75 | 35 | 255 | 13,151,928 B |
   | Runtime LOD2 | 111,282 | 191,220 | 75 | 35 | 240 | 9,845,280 B |

   Relative-height transitions are .38 / .16 / .008, with crossfade. The project LOD bias is 2; runtime zoom tests account for it. No fourth LOD was justified. LOD2 is above the suggested 100k upper guide; this intentionally retains reusable camera/light detail rather than pursuing another optimization pass.

3. **Simplification.** Category costs were measured first (`master_categories.csv`): banner lettering 141,724 triangles, main signage 45,124, curtains 46,100, planters 25,699 including their existing paving packet. Per-object coplanar/bevel dissolution preserves structural silhouettes. Individual lettering and cloth receive local reduction; lower LOD furniture uses the established period-kit LODs. LOD0 planters remain exact. No whole-building decimation, roof redesign, material recolouring or master regeneration occurred.

4. **Cutaway.** Generic `BuildingCutawayController` with building-local camera direction, independent front/rear/left/right shells, three partitions, roof elements and room ceilings. There are 82 ownership/render groups including independent furniture/equipment components and empty ownership nodes. Explicit semantic batching respects door pivots and support boundaries. Existing opaque/dithered materials are reused.

5. **Attachment ownership.** Ceiling → mount → pendant; rear shell → curtain mount → curtains/backdrop; nearest supporting partition/shell → portraits, clock and wall lanterns; entrance leaves → front shell; specialist leaves → their hall partitions. Independent furniture stays independent. The ownership-chain/cycle test passes; both reveal directions were inspected. Full mapping: `visibility_groups.csv`.

6. **Hiring Hall regions.** Four 3.4 × 3.2 m physical regions, centered at X = −7.2, −2.4, 2.4, 7.2; Y = .38, Z = −5.45. First three contexts are ACTOR / DIRECTOR / EXTRA. Fourth has applicant context CREATE_IMPORT_TALENT and employee context DISMISS. No floor labels, actions or person anchors are merged into these bounds.

7. **Audition anchors.** Performer A/B at (−4.85,.73,4.7)/(−3.15,.73,4.7); camera (−4,.39,1.1); operator (−4,.39,−.05); three evaluator points at X −8.85/−8/−7.15, Y .39, Z −.55; two crew points; SCREEN TEST activity (−.7,.38,1.25). StudioCamera1930 and both StudioLamp1930 instances remain. Coordinates are building-local metres. Full list: `anchors.csv`.

8. **Interview anchors.** Applicant (5.85,.38,2.3), second documented applicant (7.35,.38,2.3), interviewer (6.6,.39,4.6), activity (4.6,.38,1.4). No Interview gameplay.

9. **Exterior applicants.** Six anchors at X −9.2/−7.2/−5.2/5.2/7.2/9.2, Y .05, Z −10.55. Central approach, public walk, outward-facing benches, their wall clearance and the final planters are preserved. No applicants were spawned by B2.

10. **Doors and inbound navigation.** Four initially closed authored leaves: two entrance leaves and one door to each specialist room. All LODs share each door's state. Generic automatic `BuildingDoorTraversal`, human radius .35 m and bidirectional links were reused without StageSchool-specific navigation code. Real agents completed exterior → Hiring Hall, exterior → Audition, and exterior → Interview, each beginning with closed doors.

11. **Outbound navigation.** Explicit real-agent Audition → Hall → exterior and Interview → Hall → exterior routes passed; starts were inside the specialist rooms, not outside-to-outside.

12. **Opposing agents.** Two real agents exchanged Hall/Audition positions through the initially closed audition doorway without deadlock. This is a two-agent sanity check, not a crowd-throughput guarantee. Existing generic doors remain open after traversal; automatic closing was not added.

13. **Placement.** Physical slab is 22 × 16 m centered at (0,−1). Reserved union: body/margin rectangle centered (0,−.45), 24 × 17.7 m; functional forecourt/public-walk rectangle centered (0,−11.65), 23 × 4.9 m. The body margin fits scaffolding and rear projections; no removed wings or decorative empty recesses remain reserved. Central approach clearance is 7.6 × 4.9 m; exterior approach is (0,−14.3). Arbitrary 37° placement and a legal neighboring footprint were exercised. Existing lot-relative snapping is unchanged and covered by the placement regression suite.

14. **Construction requirements.** 32 total work, 3 recommended workers, 5 maximum useful workers. These are initial values for the smaller, simpler building. Existing purchase cost remains $42,000; no balance loop or economy redesign.

15. **Phase weights/capacities.** Site preparation 12%/3; foundation 22%/4; structure 26%/5; exterior 20%/3; finishing 20%/4. Compound dressing now reuses authored scaffolds and material zones, clipped to the reserved union and clear of entrance/activity pads. A regression caught fence endpoints crossing an entrance exclusion; full bounds are now tested. All phases pass reservation checks. Floors/slab have explicit foundation bindings at every LOD, verified by the new test and image 11.

16. **Lifecycle.** Actual placement created a gated site. Zero workers produced no progress. Three workers used the real proficiency/diminishing-output service, person reservations and all five phases. Real completion activated the production view, removed dressing, released assignments/reservations, scheduled asynchronous navigation and settled it. The selected Fast speed and world time-scale adapter were preserved. Worker travel was accelerated through the existing arrival-admission API; physical construction-worker travel/animations were not validated. Pedestrian doorway tests used real moving NavMeshAgents.

17. **Collision.** 38 BoxColliders, zero MeshColliders. Permanent slab/floor, perimeter and partition walls, doorway headers, three stair treads/landing, major tables/desks/bookcases/platform/cabinet/bench boundaries and four pots use simplified proxies. Four separate door barriers carve while closed. Selection-only roof trigger is excluded from NavMesh input. Reveal/LOD changes do not change physical boundaries. Window openings are simplified as continuous physical wall barriers; their render geometry stays open/glazed.

18. **Closed-building visibility.** The existing distance-based interior visibility component disables deep camera/light/evaluation detail renderers beyond 32 m while closed and restores them on reveal or close inspection. Window-visible room architecture and furniture remain. All interiors stay instantiated; revealing does not create/destroy them. Review-only A4 lighting is excluded from the runtime prefab.

19. **Runtime/performance observations.** Main Thread ProfilerRecorder was valid and sampled the real completion window plus eight subsequent frames. First attempt: 19.31 / 113.68 / 31.46 / 6.11 / 4.17 / 4.20 / 4.54 / 4.60 / 12.28 ms. Passing integration run: 19.44 / 2038.27 / 10.54 / 10.82 / 25.29 / 5.57 / 11.11 / 8.19 / 7.87 ms. Clock-method timing alone was 64.73/48.98 ms and is not used as the frame-cost claim. These are whole Editor-main-thread samples and do not isolate B2, source collection, shader work or Editor overhead. The large spike is unresolved as a performance attribution issue; **smooth completion in a player build is not certified**. Functional async settlement passed in both runs. No player build or GPU/crowd stress profile was run.

20. **Files changed.** New source/reproduction files under `ArtSource/StageSchoolB2`; measured input/derived LOD packets under `ArtExports/StageSchoolB2`; runtime prefab, meshes, builder/reviewer and isolated scenes under `Assets/SilverScreen/Environment/StageSchoolB2`; review/tests/CSV/JSON here. Shared changes: `Assets/Scripts/Domain/Buildings/BuildingDefinition.cs` (compact metadata), `Assets/Scripts/Presentation/Buildings/StudioConstructionDriver.cs` (resource selection), `ConstructionDressingGenerator.cs` (compound scaffold/material bounds), `StageSchoolB/Runtime/StageSchoolInfrastructure.cs` (context metadata), historical B builder prefab path and prefab archive move. Updated `StageSchoolRuntimeTests.cs`; added B2 integration/input test files. A complete path inventory is in `files_changed.json`. Existing unrelated workspace changes were retained.

21. **Limitations.** No target-player performance approval; the completion spike requires profiling on the target build. LOD2 is 111k, not below 100k. No worker travel/crowd stress, save migration, nighttime lighting or gameplay tests were added. Full integration passed before the final construction-only LOD phase-binding correction; that correction was verified with all-LOD Edit Mode assertions and an inspected isolated URP image. The first harness expected an obsolete Console error that did not occur, and its one-shot pointer event was overwritten by Editor input. Both harness issues were corrected; the final integration XML passed without suppressing failing logs. The final source/asset audit reports no compilation failure.

22. **Exact manual checks.** Open `Assets/SilverScreen/Environment/StageSchoolB2/StageSchool_B2Review.unity` or the gallery. Compare master/LOD0 façade, lettering, arched entrance and planters; inspect LOD1 at management distance and LOD2 at distant zoom. In a fresh Studio with tutorial restrictions disabled, place one Stage School at roughly 37° beside another legal footprint; confirm only a site appears, no progress without workers, then all phases and final activation. At each zoom inspect slab visibility, fence/scaffold clearance and foreground cutaway ownership. Hover, click-pin, move the pointer away, orbit from both sides, zoom, press Escape. Route a person from outside to Hall and both rooms, then back outside, and try two opposing agents. Finally profile a cold and warm completion in a Development Player on target hardware, distinguishing Editor-only delays from actual game stalls before performance sign-off.

Stage School A4 runtime appears ready for manual approval

The compact art, metadata, placement, construction lifecycle, reveal input and door routes have passed the stated functional checks, with the approved master preserved. This is a functional/art handoff; target-player completion performance remains an explicit approval caveat.
