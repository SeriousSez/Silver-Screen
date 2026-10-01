# Stage School A3 production candidate

An isolated production-art cleanup of the approved Stage School A2 architecture. A1/A2 and approved StudioCamera1930/StudioLamp1930 assets are read-only dependencies. The existing prototype and Studio.unity are not replaced or saved.

## Authoritative source

- `generate_stage_school_a3.py`: retained A2 footprint, room program and nine roof definitions; assembly, export and linked equipment.
- `components.py`: shared opening construction, bevel profile, pivots, hardware, wall registry and drainage exclusions.
- `production_finish.py`: reusable upholstered chair, opening-aware wainscoting, furniture placement, workstation joinery, washroom fixtures, audition installation and local roof/interior cuts.
- `StageSchool_A3.blend`: reproducible authored result, with linked equipment and shared period furniture.

Run Blender 5 in background with `--factory-startup --python ArtSource/StageSchoolA3/generate_stage_school_a3.py` from the repository root. Run `verify_source.py` with Python and NumPy. Preserve `preservation_baseline.json`; it is a pre-edit hash baseline, not a generated current-state snapshot.

## Unity review

The existing `StageSchoolA1Review` editor class retains its name and GUID but targets `Assets/SilverScreen/Environment/StageSchoolA3`. Build and create the review scene in Edit Mode from a clean scene. The bridge import script reads the Studio environment, creates the isolated review, checks shared equipment references, and opens A3 without saving Studio.

Run captures after import in a separate editor call so URP initializes. `Exteriors`, `Interiors`, and `AuditViews` write 36 purposeful review views into the existing `ArtReview/StageSchoolA1` gallery directory. Only plan/overview captures hide assemblies; interior close views include roofs and ceilings. These visibility operations are not a runtime cutaway implementation.

Rebuild the existing gallery using `ArtSource/StageSchoolA1/build_review.py`. See `ArtReview/StageSchoolA1/completion_report.md` for the final audit, counts, limitations and freeze recommendation.

## Bounded QA

Source checks cover finite mesh data, index validity, degenerate triangles, protected hashes, preserved macro geometry, window/drainage projections, opening-aware joinery, gallery/door overlap, room bounds, sampled door/furniture swing arcs, and roof triangle centres inside occupied volumes. Bounds and triangle-centre scans identify candidates; they do not prove complete collision freedom. Visual inspection supplements them.

No gameplay tests, Play Mode, broad profiling, navigation, contextual regions, runtime cutaway, LOD1/2, recruitment or Star Maker are included. No commit or push is part of this task.

## A3.1 known-defect correction

`known_defects.py` is the bounded follow-on for the six documented A3 defects. It runs after the A3 finish pass and before the established mesh-packet export. It resolves local roof intersections using the original roof planes, supplies collection/support construction, finishes the Hall soffit/reveals, seats every pendant rose, and completes cubicle hardware. The small cistern/seat and stair-anchor edits remain in `production_finish.py`.

`a31_baseline.json` is the immutable pre-A3.1 packet hash baseline. `verify_a31.py` asserts that all changed mesh groups belong to those six areas and that shared furniture placements and approved audition equipment remain identical. It also checks all twelve mounts and the directed nine-roof outlet graph. This is scoped source evidence, not a hydraulic simulation or substitute for the targeted Unity views.

The existing Unity importer creates `SS_Mirror` and bakes one 128-pixel HDR box-projected cubemap shared by both restroom mirrors, using the established Stage 1 baked-reflection pattern. It adds no realtime capture camera. The new reflection is local to the isolated candidate.

For A3.1, regenerate with the same Blender command, run `verify_a31.py`, import through the existing editor class, then call `KnownDefectViews()` in a separate editor invocation. Do not call the broad Exteriors/Interiors/AuditViews methods for this follow-on. Rebuild the same gallery with `build_review.py`. Retained A3 captures are explicitly labelled as context. See `ArtReview/StageSchoolA1/a31_completion_report.md` for the final inspected outcome.

The final basin verification must run in the regular isolated review scene after `KnownDefectViews()`: execute `ArtReview/StageSchoolA1/a31_mirror_check.cs.txt` through the existing bridge. Preview-scene captures do not select the local reflection probe correctly. The regular-scene capture overwrites the same basin image, without another art pass. Final A3.1 status is partial: the two remaining drainage/junction defects are documented in the report.

## A3.2 final manual corrections

Final follow-up: the user removed the rear service canopy. Its complete source assembly (roof, framing, posts, anchors and flashing) is omitted. Door, steps, wall lantern and utilities are retained. `a32_canopy_removal_capture.cs.txt` refreshes only the six affected rear views; the earlier canopy-remodel description below is historical.

`manual_corrections.py` supplies the fitted curved reception workstation, lightweight occupant-latched cubicle doors, side-wall staff arrangement and local Hall cap termination. Entrance offsets and separated collector shoes are corrected in `known_defects.py`; random stucco marks are removed at their generator in `generate_stage_school_a3.py`.

The user's follow-on curtain and canopy request is implemented in `drapery_service_finish.py`: continuous straight-hanging velvet wings, unequal broad folds, weighted sewn hems and a straight pleated valance matching the final concept direction; a sloping boarded and metal-covered timber service canopy with anchored shoes, housed braces and wall flashing. The later request supersedes the tied/gathered curtain shape. The shared backdrop, platform, equipment, room and nine main roofs remain unchanged.

The final drainage direction is perimeter-led. `known_defects.py` classifies roof-edge spans against adjoining masses before generating rainwater. External eaves retain supported gutters and sensible downpipes; internal spans use recessed liners with the existing valley/abutment flashing and concealed collection routes. Exposed cascading shoes/collectors are omitted. `verify_simplified_drainage.py` verifies this bounded simplification and measures its geometry reduction. Water routes are conceptual construction intent, not a hydraulic simulation.

Run the established generator/import workflow, then `verify_a32.py` with Python/NumPy. `a32_baseline.json` remains the immutable pre-A3.2 baseline. The local Blender checks are `a32_local_geometry.py`, `a32_shoe_check.py` and `verify_drapery_canopy.py`. They flag local surface intersections and construction clearances, not general asset quality. The source check also verifies protected files and approved equipment, and checks fixed desk/table doorway approaches without blindly modifying bounds candidates.

The regular-scene scripts `ArtReview/StageSchoolA1/a32_capture.cs.txt` and `a32_drapery_canopy_capture.cs.txt` capture only the corrected areas and restore camera/visibility. Rebuild the same gallery using `ArtSource/StageSchoolA1/build_review.py`. The final A3.2 outcome, inspected views and Unity counts are recorded in `ArtReview/StageSchoolA1/a32_completion_report.md`.

Final downpipe consolidation: seven side/rear ground drops replace fifteen. Front Entrance/Waiting/Preparation drops, duplicate CommonBay front drop, WestService side duplicates and Audition south/west duplicates are omitted at source. Front gutter terminals use boxed fascia returns; raised entrance collection is concealed into adjoining wing drainage. Roof massing and external gutters retained.
