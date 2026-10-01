# Studio Services — source and integration

Massing is approved; final art still requires visual approval. Production detailing uses `generate_production.py`, which opens the approved massing source read-only and writes a separate production source, mesh packet and placement manifest. The historical massing review remains unchanged. Current refinement audit, before/after comparisons and the canonical catalog live in `ArtReview/StudioServices/Fidelity`; assembly captures and integration tests remain in `ArtReview/StudioServices/Production`.

## Production build

1. Blender 5.0: `--background --python ArtSource/PeriodEnvironment1930/build_kit.py`.
2. Blender 5.0: `--background --python ArtSource/StudioServices/generate_production.py`.
3. Unity 6000.6.3f1: **SilverScreen > Art > Studio Services > 3 Build production and independent kit**.
4. Run **4 Capture and check production** with the generated review scene closed. It never saves Studio.
5. Run `SilverScreen.Tests.EditMode.StudioServicesTestRunner.Run()` for focused asset, interaction, hiring and construction regression coverage. Unity's default `TestResults.xml` remains authoritative if entering Play Mode reloads the result callback.
6. Run `python ArtSource/StudioServices/validate_exports.py` to check finite geometry, indices, zero-area triangles and approved source/anchor preservation.
7. Run `python ArtSource/StudioServices/build_review.py` after capturing Unity evidence to assemble the local review page.
8. Run **5 Capture fidelity and all canonical assets** for installed connection close-ups and independent catalog renders. Then run `python ArtSource/StudioServices/build_fidelity_review.py`.
9. For construction regression checks, run Blender with `--background --python ArtSource/PeriodEnvironment1930/validate_sweeps.py` and `--background --python ArtSource/StudioServices/validate_production_source.py`. The latter reads the generated blend and verifies that every front roof panel is thickened below its approved surface.

The fidelity pass retains the curved office-door canopy and omits the competing porch placement; the porch remains an independent reusable asset. Both rooflights sit inside the front roof plane. Main-roof downpipes and the separately collected lean-to runoff discharge through open shoes toward splash blocks. The lean-to uses the shared gutter/shoe/splash modules and a building-specific cut pipe with supported connections. Office/storage pendant low points are 2.71 m; workshop low points are 3.01 m, above the current 2.00 m employee capsule. No semantic anchors, room/opening dimensions or gameplay rules changed in this refinement.

The production blend links canonical collections from the separate period kit. Unity uses nested prefab references, not embedded prop copies. Existing canonical lamps and electrical cabinets are reused unchanged. The building keeps its unique shell, facade, floor, room layout and yard shelter. Material families reference the existing neutral Stage 1 microtextures without editing Stage 1 materials or textures.

### Live entry point

`StudioConstructionDriver` loads `Resources/StudioServices_Live.prefab` in the existing **New Studio** startup path. The saved Studio scene's developer `_startNewStudio` toggle is not changed. The older populated development scenario remains available. Studio Services has its own appended `BuildingType` value, the existing `building.studio-service` facility ID and a 20.6 × 13.6 m construction reservation. It is the initial operational facility; this milestone does not add it to the player construction catalog or change building costs, startup balance or hiring permissions.

`StudioServicesFacility` binds all local semantic anchors, five applicant waiting slots, Construction Worker/Groundskeeper attraction and construction presentation metadata. Personnel and yard doors are held open; the workshop pair stays closed and physically blocks traversal. No generalized door simulation is introduced.

### Held-person integration

The generic cutaway controller hides renderers only. Explicit collider lists identify hidden occluders for placement-ray filtering, while physical collision and NavMesh remain active. Compatible interior hiring spots request `HeldPersonInteraction` reveal when the cursor ray enters the building bounds. The existing carry controller still requires a separate click after the original pickup press is released. Placement calls the existing immediate `TryDrop` path and then existing hiring rules; it does not route the held person inside. Drop, Escape, lost focus or loss of context releases only this reveal reason. Other reveal owners can remain active.

No Studio Services-specific input method, profession permission, final camera subsystem, maintenance simulation, tool inventory or construction balance change is added.

## Historical massing build and approved dimensions

## Reproduce

1. Run Blender 5.0 in background with `--python ArtSource/StudioServices/generate_massing.py`.
2. In Unity 6000.6.3f1, run **SilverScreen > Art > Studio Services > 1 Build massing review**.
3. Run **2 Capture and check massing**. Close the owned review scene first if it is open.

The generator owns this folder and `ArtExports/StudioServices/Massing`. The scoped Unity builder owns `Assets/SilverScreen/Environment/StudioServices/Massing` and `ArtReview/StudioServices/Massing`. It updates its own assets in place, preserving their GUIDs. It neither saves Studio nor rebuilds any pre-existing assets. Native mesh-packet conversion follows the existing reusable-fixture approach: Blender Z-up metres to Unity Y-up, reversed triangle winding, explicit normals/UVs, linear source colours converted to sRGB material properties.

The editable blend and mesh packet are generated together. No hand-edit to an exported mesh is authoritative. Nine lights are references to the existing canonical fixture prefabs, not embedded copies in the Blender source.

## Approved composition

- Building: 14 × 9 m; eave/wall 4.22 m; parapet 5.35 m; low hipped roof ridge 5.15 m.
- Front: employment shelter/notice board, distinct office door, office window/bench, large 4.85 × 3.46 m workshop opening.
- Personnel opening: 1.20 × 2.62 m, with a 55 mm leaf held inward in this study. Workshop leaves are 60 mm. Final hardware and door states follow the detailed kit stage.
- Office: front left, approximately 6.4 × 4.4 m inside the shell. Supply room: rear left, approximately 6.4 × 3.9 m. Workshop: right, approximately 6.9 × 8.4 m.
- Yard: east/right side, approximately 5.4 × 9 m; open-sided shelter against workshop, front access gap, rear material storage.
- Interior floor datum: local Y = 0; no hidden entrance step. Floor has collision independently of render visibility.
- Reserve including approach and yard: 20.6 × 13.6 m, local centre (2.7, -1.3). This fits the existing service origin (-28, -22) inside the current 76 × 64 m lot. The old 9 × 12 m construction reservation must be replaced at live integration.

All 19 local semantic anchors appear in the generated manifest. Furniture blocks are clearance studies, not canonical furniture or finished props. Glass, roof coverings, brick fields and paint are material/volume studies. They do not represent finished surface quality.

## Generic cutaway proof

`BuildingCutawayController` is renderer-only. It hides roof groups and exterior groups facing an observer, retaining far walls, partitions, floor and interior props. Multiple reveal reasons can coexist; releasing one reason does not release another. Ending reveal restores each renderer's original state. Colliders, navigation and anchors are never disabled by this controller.

The review builder checks hide/restore behaviour, physical capsule clearance at every anchor, canonical lamp references, lot fit and complete paths through a separately baked temporary navigation surface. The latter is moved 1 km away from Studio's existing NavMesh during the check so its paths cannot use the old lot surface. It is removed after validation and is not saved as live navigation.

The historical massing prefab is not registered as a live facility. Its clearances and source remain useful regression evidence. Production integration is described above.

## Current integration boundaries

The Studio scene has two explicit populated-scenario art references on `StudioBootstrap`: the art-only Administration instance (including its child weathering) and its reflection probe. They are deactivated only in New Studio, preventing them from intersecting the service facility. Neither Administration source nor its populated-scenario appearance is changed. The scene edit adds only these references; its existing data is preserved.

The live slab is seated 20 mm above the flat lot surface, avoiding coplanar floor/apron flicker without changing the approved geometry. Its foundation remains buried. New Studio uses the existing 25 mm navigation raster for this detailed interior, as already used for Stage 1; employee dimensions are unchanged. Bake-only modifier volumes add a 60 mm horizontal guard around masonry/jambs, ground-standing workshop props and thin fences/gates. This prevents native steering from shaving rasterized corners during turns; physical colliders and employee dimensions are unchanged. The refinement runtime check found and corrected millimetre-scale contact at a workbench corner and an open gate end without moving those assets.

Doors are fixed in the authored state in this milestone. Dynamic doors, maintenance/repair simulation, inventory, full camera automation, LODs and later-era replacements are deferred. Visual review remains separate from the automated acceptance checks.
