# Stage School B2 — approved A4 runtime derivative

Production art is the approved, preserved StageSchoolA4 master. The older B prefab remains historical; the existing `building.stage-school` / `content.stage-school.1930` catalogue identity now loads `Resources/StageSchool_A4_Runtime`.

Reproduction:

1. Run `preserve.py --baseline ArtSource/StageSchoolB2/frontage_finish_freeze.json` to verify the user-requested frontage revision. The original `master_freeze.json` remains immutable historical evidence; never replace either baseline during regeneration.
2. Execute `ArtReview/StageSchoolB2/export_master.cs` through the existing Unity relay to export actual approved mesh data and category measurements.
3. Run Blender 5 with `--background --factory-startup --python ArtSource/StageSchoolB2/derive_lods.py`.
4. In Unity, run `SilverScreen.Editor.EnvironmentArt.StageSchoolB2Builder.Build()`.
5. Run `StageSchoolB2Review.Capture()` with the generated B2 review scene closed. This creates a separate review scene with Studio rendering settings.
6. Run `StageSchoolB2Validation.RunEditMode()`, then its focused `Run()` integration test. The latter enters Play Mode in an isolated copy of Studio, exercises real construction and door traversal, and exits without saving runtime changes.
7. Run the same preservation command again and inspect the review images and test results.

The established Blender semantic packet exporter is reused. Coplanar and bevel topology reduction is performed per authored object. Lettering and cloth use local tessellation reduction. LOD0 preserves the final planters exactly. Lower LODs reuse the existing period furniture LOD packet; camera/light assets retain their approved component identity and materials. No source blend or library is saved by derivation.

Runtime collision uses box proxies, including separate permanent wall, floor, stair and furniture boundaries; no render-mesh collision. Four leaf state components and generic door links/traversal are shared with B1. Cutaway ownership remains in the generic controller. Physical anchors and large contextual areas are metadata only.

Initial construction settings: 32 work units, 3 recommended workers, 5 maximum useful workers. Phase shares/capacities: preparation 12%/3, foundation 22%/4, structure 26%/5, exterior 20%/3, finishing 20%/4. These reflect the compact 352 m² single-storey building and simple roof; they are starting values, not an economic balance study. Existing purchase cost remains unchanged.

The placement union contains the body and its narrow construction margin plus the entrance clearance and two compact applicant waiting pockets. Generic compound dressing clips scaffold and material modules to that union and excludes access/activity pads.

No Stage School recruitment, hiring, dismissal, interview, screen-test, talent-creation or queue gameplay is implemented.

## Building frontage revision � 2026-09-30

The standalone public pedestrian strip has been removed from the authoritative A4 generator and all B2 LODs. Keep the entrance steps, small central landing and two separate applicant pockets. Their outer edges terminate into the lot; there is no studio path mesh or hidden broad forecourt collider.

`Anchors/path.connection.main` is passive `StageSchoolInfrastructure` metadata at building-local Unity `(0, 0.05, -11.55)` metres, centered on the landing front edge, with local forward toward `-Z`. Transform it through the placed building when a future spline-path system is implemented. It represents the building attachment point, not a spline, route, path width requirement or navigation link. `entrance.approach` and `exit` are at `(0, 0, -12.8)`, on the lot beyond this edge. A4 authoring_readiness.json records the same connection convention.

Collision follows the three compact pads; placement reservations retain small maintenance/access margins and no longer reserve the removed strip. The six applicant positions, benches, detailed plants, shared materials and building geometry remain unchanged.
