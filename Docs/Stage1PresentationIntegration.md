# Stage 1 Presentation Integration — 2026-09-29

Follow-up: [structural readability refinement](Stage1StructuralReadability.md)
adds partial camera-occluding truss fades. Its report supersedes the original
always-visible-truss behavior described below; shell reveal remains unchanged.

Implemented against Unity 6000.6.3f1 and the existing production Stage 1 live variant.
Final visual and input approval belongs to the user. No architecture was regenerated.

## Completion report

1. **Existing hierarchy and limitations.** `Stage1_Live.prefab` is a variant of the approved clean candidate. Its imported geometry consists of semantic batches rather than individually selectable wall faces: exterior walls, masonry, roof sheet/seams/glazing, roof lining/blackouts, primary steel/connections, rigging, circulation, stairs, acoustics, utilities, lamps, ventilation and doors. It also has instance lettering, 31 real lights, an enclosed custom reflection probe, live personnel-door parts and hinge, selection envelopes, physical obstructions, navigation-clearance volumes, entrance anchors and production layouts. There was no BuildingCutawayController. The generic support resolver followed only one support link.

2. **Generic integration.** The live prefab now has the existing BuildingCutawayController, with serialized groups. StudioSelectionController's existing hover and building-selection paths discover it without Stage-specific input code. Stage1PresentationAuthoring is an Editor authoring recipe, not a second runtime controller. The existing live-variant builder invokes it so regeneration retains the integration.

3. **Authored groups.** Forty groups: four exterior shell sides; five barrel bands; five roof-support groups; five roof-equipment groups; trim, utilities, lamps and openings for each wall; separate left stage door, right stage door and stage-door frame groups; instance lettering; retained interior. Some support groups intentionally express semantic ownership even when they contain no separate renderer. Sixty-two derived mesh assets partition existing triangles at edit time. Original meshes remain on their original objects for collision and provenance; their replaced renderers are disabled.

4. **Camera-relative logic.** The shared controller transforms the observer into building-local coordinates. Wall normals use horizontal local direction. The new optional UseVerticalDirection field lets curved roof bands use the full local direction, including elevation. No world-north/front assumption was added. Runtime code contains no Stage mesh-name classification.

5. **Barrel roof.** Sections follow the existing five bands: outer sheet, glazed ribbon, crown sheet, glazed ribbon, outer sheet. Lining and seam positions are projected onto the same source circle for consistent boundaries at different shell depths. Camera-facing bands fade; the crown fades from overhead; far low barrel portions can remain. Glazing blackouts, lining, seams and vents follow support ownership so an opaque secondary layer does not remain across an opened band. All geometry returns intact when closed. Subjective seam quality remains for manual review.

6. **Wall behavior.** Front, rear, left and right shells respond independently. Camera-facing shells and their trim/openings disappear; opposite shells restore as the camera moves. Cornices, masonry details, side acoustic treatment, clerestories, canopies and rear ventilation are included intentionally. Low threshold pieces and ground-supported rainwater shoes remain.

7. **Interior retained.** Primary steel and its connections, interior columns, rigging, catwalk supports/decks/guards, stairs, the floor, independent pendant lamps/carriers, and fixed protective posts remain visible. No blanket interior hierarchy hiding is used. There are no new gameplay partitions or contextual floor markers.

8. **Attachment implementation.** VisibilityGroup.SupportGroupId now resolves the full chain once per configuration. Renderer visibility and interaction-ray occluder filtering follow the ultimate support. Optional Light bindings fade fixture illumination with geometry. Physics colliders are never disabled by cutaway. Materials/light intensities restore on release, disable and destruction.

9. **Configured examples.** Side shell → utilities → wall lamps; front shell → trim → lettering; shell → trim → personnel openings; barrel band → roof support/lining/seams → blackout or vent equipment. Each main stage-door leaf is a distinct group intentionally dependent on the front cutaway, with its integrated hardware retained on the leaf. The existing door transforms and future independent motion remain available.

10. **Nested behavior.** Support relationships do not depend on Transform parents or group ordering. Connected lamp/conduit/bracket pieces are classified as complete components during authoring, including across material/normal vertex seams. Runtime dependency traversal is cached and bounded for malformed graphs. The authored graph is checked for missing IDs, cycles and duplicate renderer ownership. Independent interior categories are explicitly retained rather than hidden merely because they are near a wall.

11. **Hover.** Existing 0.22-second intent and 0.45-second grace are retained. The Stage's local interaction envelope covers its actual shell and roof. Hover ownership releases independently of other reasons.

12. **Click/pin.** Existing BuildingFocus ownership pins the selected Stage. Moving the pointer away does not release that reason. Existing selection of another object, clicking elsewhere and Escape clear selection. No parallel Stage selection system or UI was introduced. Existing double-click live-filming behavior is untouched.

13. **Fade.** Existing 0.35-second unscaled transition, smooth alpha easing and cached URP material variants are reused. Attached light intensity uses the same easing. No per-frame material creation or Time.timeScale owner was introduced. Originals are restored at rest.

14. **Arbitrary yaw.** Focused tests cover 0°, 37°, 90° and 143°, both opposite camera directions, roof/wall behavior, retained interiors and unchanged collider enablement. The runtime clone retained 37°. Pointer/camera feel at arbitrary angles is reserved for manual review.

15. **Construction compatibility.** The existing ConstructionSiteView behavior gate disables the controller and lights until operational completion. New renderer sections carry explicit ConstructionPhaseVisuals bindings matching each source renderer's prior phase. Original physical geometry, doors, entrance anchors and navigation remain. Build Mode, placement, rotation, worker dispatch, asynchronous navigation and construction-state code were not modified. The runtime sanity verified that the current scene's inactive construction template inherits the new configuration. A full construction cycle was not rerun in this milestone.

16. **Material audit.** Reviewed imported material assignments and source palette for roof, glazing, masonry/trim, floor, acoustic fabric/timber, structural and painted steel, catwalks/stairs, doors, brass/galvanized hardware and electrical details. Nineteen shared production materials were present, with no default-material substitution or accidentally duplicated palette identified. A single existing exterior view was inspected under Studio lighting. This was not an exhaustive interior or multi-angle visual review.

17. **Roof issue and correction.** The enclosed custom probe extends to approximately 11.1 m, overlapping the barrel. Roof renderers still used BlendProbes despite an exterior probe anchor. An anchor is not an exclusion rule for the renderer/probe overlap. The live presentation authoring now explicitly selects sky reflection for roof sheets, seams, glazing and vents with ReflectionProbeUsage.Off. This retains normal maps, metallic/smoothness values and direct specular lighting rather than flattening roof color. This fixes an identified reflection-source mismatch; the limited audit does not establish it as the sole cause of every player-observed angle-dependent artifact. Any remaining roof striping or lighting issue needs the user's specific failing view.

18. **Interior coherence.** Existing material differences match the source's deliberate distinctions: matte acoustic fabric, warmer timber, rough painted utilities, metallic structural/galvanized steel and smoother floor/ceramic surfaces. Those values were preserved. Wall lamps now fade with their mounting surfaces instead of leaving unsupported light sources illuminating the opened interior. No unsupported recoloring or global lighting adjustment was made.

19. **Materials changed/reused.** No .mat assets, texture maps, FBX materials or source palette values changed. The same material references and vertex attributes are used by derived sections. Only roof renderer reflection-probe usage changed. Temporary fade materials remain managed by the existing controller.

20. **LOD.** No Stage LODGroup exists. No LODs were generated. Groups bind Renderer arrays and the shared fade implementation already supports multiple renderers per group; later LOD representations can join the corresponding groups. Splitting increases renderer count; meshes are authored once rather than split or allocated during placement.

21. **Files/assets.** Modified StudioServices/Runtime/BuildingCutawayController.cs (generic nested supports, curved-roof direction and lights), Stage1Live/Editor/Stage1LiveIntegrationBuilder.cs (reproducible integration), and Stage1Live/Stage1_Live.prefab. Added Stage1Live/Editor/Stage1PresentationAuthoring.cs, Stage1Live/PresentationMeshes (62 meshes and Unity-generated metadata), Assets/Tests/Editor/Stage1PresentationTests.cs and this report. No scene was saved, and existing GUIDs were preserved.

22. **Validation.** Runtime/editor assemblies compiled. Eight focused EditMode cases passed, zero failed/skipped, in about 2.9 seconds. They cover four yaws, complete triangle counts, unique group ownership, acyclic dependencies, fifteen mounted lights, construction bindings, nested multi-reason restoration, existing partition occlusion, and unchanged live personnel-door geometry/UVs/materials. One Play Mode session confirmed the scene template's forty groups; at 37° yaw, 38 renderers finished hiding, all 290 enabled colliders remained enabled, retained interior stayed visible, and Restore returned original rendering. Play Mode was exited. The Console still reports the pre-existing unrelated `NewStudioSceneAuthoring` / `ExtensionOfNativeClass` error; no new Stage exception was reported. Test output: Temp/Stage1Presentation/results.xml. No full suite or camera sweep.

23. **Exact manual checks.** Build Stage 1 at 37° and let normal construction complete. Confirm unfinished sites do not reveal. Hover the completed Stage, briefly leave, and return; then leave long enough to close. Click to pin, move away, then select another building and use Escape. Repeat reveal while paused and at 1x/2x/3x. Orbit from front-right to rear-left and overhead: inspect roof sheet, glazing and blackout together, far-wall context and complete restoration. Check wall lamp → conduit/support → wall, sign → trim → front, and vent → support → roof, including emitted light. Check trusses, catwalks, railings, stairs and floor remain readable. Check both large doors and the held-open personnel leaf, then ordinary employee entry/production routing. Repeat key angles at 0° and 143°. Inspect roof grazing highlights and interior surfaces for any remaining material issue; report a specific camera view if one remains.

24. **Deferred.** Final visual/input approval, any further roof/material correction identified by that review, full construction/navigation regression testing, Stage LOD/performance work, contextual activities/filming gameplay, construction art V2C, worker distribution, other building rebuilds, HUD and global lighting. No next milestone was started. No commit or push.

## Regeneration

`SilverScreen > Stage 1 Live > 3 Integrate presentation` updates the existing live
prefab and section meshes without promoting/baking or saving Studio. The existing
live-variant build also calls the same recipe. Configuration is serialized on the
prefab; no runtime mesh-name discovery or Inspector setup is required.

## Roof reflection correction — 2026-09-30

The renderer-only exclusion described in item 17 was insufficient in the active
URP 17.6 Forward+ path. Its clustered reflection lookup selects the interior probe
per pixel despite `ReflectionProbeUsage.Off` and the exterior probe anchor. The
interior cubemap produced broad dark/warm patches on the curved roof sheets,
leaving the ends cooler/brighter. These patches were not longitudinal end caps
or a change to the roof's base color. A fixed-camera Editor comparison under
Studio lighting reproduced the patches with the initialized probe enabled and
removed them by disabling only that probe.

`ConfigureReflections` now caps the interior influence 0.05 m below the lowest
authored exterior-roof vertex. The live prefab changes only the probe's box height
and vertical offset: the top moves from 11.1 m to 6.987 m and the bottom remains
at -0.5 m. The existing cubemap, probe position, horizontal footprint, intensity,
blend distance, and exterior renderer Off settings are retained. Occupied interior
surfaces keep the interior reflection; upper surfaces above the influence use the
scene environment. No materials, meshes, global lighting, or cutaway behavior change.

The regular integration recipe includes the correction. `SilverScreen > Stage 1
Live > 4 Repair roof reflection influence` applies only this configuration to an
existing live prefab without regenerating sections. The regression check verifies
that roof sheets, seams, glazing, and vents stay outside the influence while floor
and occupied interior remain covered at 0° and 37° building yaw. Final appearance
approval remains manual; no Play Mode session or camera sweep was used here.
