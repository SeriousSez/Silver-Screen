# Studio Services — final canonical fidelity and reported-defect correction

The source corrections are implemented and inspected in Unity 6000.6.3f1. **Studio Services is not declared visually approved.** The user remains the visual approval authority.

Open [review.html](review.html) for the five submitted defect images beside new Unity captures, matched earlier/final views, the complete canonical audit, and all assembled close-ups. No render has been retouched. The submitted screenshots and new captures differ in camera/lighting; the separate Before/After sets use the review camera definitions.

## Latest five corrections

| Issue | Source correction | Verification |
|---|---|---|
| Giant sphere on right gate | Mirroring was baking a stale Blender object matrix before the last sphere's scale had evaluated. The generator now updates the dependency graph before and after mirror baking. | Cap is 20 × 20 × 26 mm; gate depth is 255 mm including hardware. Both handed gates have source and imported-Unity bounds checks. Both faces of each installed gate were inspected. |
| Apparently disconnected shelter drain | The modules already met, but the roof had no continuous drip apron into the collection channel. Added a folded apron tucked below the roof edge, four external gutter joint laps, and retained the connected open outlet/socket/offset. | Elevated, along-eave, outlet and discharge views inspected; source raycast verifies the outlet remains open through the trough. Module socket continuity passes. |
| Primitive try square | Rounded rosewood stock, brass wear face, mortised thin steel blade, flush rivets and etched divisions. | Front and oblique installed tool-board inspection; original tool placement retained. |
| Crude telephone | Moulded base, flared cradle pedestal, supporting hook fingers/switches, shaped handset cups, pierced ten-hole numbered dial, finger stop, cord and strain relief. | Front and side close-ups show the receiver support, unobstructed dial and connected cord. |
| Crude banker lamp | Turned brass base/stem, curved yoke and tilt pivots, closed green glass shade, separate white lining, recessed bulb/socket, pull chain and supply cord. | Front, side and underside inspection. An intermediate liner protrusion was corrected and source-tested; curved glass shading was smoothed. |

All changes originate in the reproducible source and regenerate through the existing Blender → mesh packet → native Unity mesh/prefab workflow. Two shared glass materials were added; existing material GUIDs remain stable.

## Completed broader final QA

The original 61 assemblies were audited individually; two supporting canonical assemblies add intentional right-hand gate construction and independent shared fence posts. The catalog now has **63** assets and the building uses **181** shared prefab instances.

- Fence panels share one post at each joint; wall terminations have mounted sockets. The left/passive gate has keeper and drop bolt, while the right/active leaf has the sliding latch. Primary hardware faces the yard; secondary return pulls face outside. Their authored open swing remains unchanged. The left keeper close-up uses a documented diagnostic wall cutaway because the open leaf is close to the facade.
- All six installed industrial windows were checked from both sides: operating hardware remains inside, seated glass and reveals remain intact.
- The unsupported exterior tool-board placement was removed; the reusable asset remains. Interior carpenter board mounting was corrected. Groundskeeping tools were rebuilt and moved onto the storage/office partition to clear the window and shelves.
- Hand saw, hand plane, grounds tools and wheelbarrow grips were refined. The center facade lamp no longer obscures the sign; two outer sign lamps remain. Curved canopy braces now connect to the hood.
- Main drainage offsets preserve hollow bores; gutter-to-pipe transitions, shoes and splash beds were checked. Roof, rooflight and cornice corrections from the preceding pass were rechecked.
- Desk/furniture, bench, vise, crates, barrels, sacks and material racks were retained where already satisfactory and inspected for grounding/support.

## Technical validation

- **42/42 focused Unity tests passed**, 0 failed/skipped/inconclusive, 2026-09-28 19:52:19–19:52:40 UTC. Includes three new regression cases: left/right gate dimensions and gutter sockets. Evidence: [editmode-tests.xml](editmode-tests.xml).
- The runtime test opens the real Studio scene in Play Mode and checks applicant arrival, original-release retention while carrying, cutaway reveal, Escape restoration, separate-click immediate hiring into both professions, identity continuity, real NavMesh routing through the entrance to workshop/storage and per-frame capsule checks. [Runtime evidence](../Production/runtime.txt).
- Final isolated prefab preflight: **37/37 checks**, covering finished visibility groups, cutaway restore, all semantic anchors/capsules, lot reservation and complete routes. [Checks](../Production/checks.json). This run does not save or replace the open review scene.
- Source regressions: both gate bounds, 26 mm maximum cap dimension, open outlet bore, closed lamp ends and liner containment. [Results](source-regressions.json).
- Source tube test: 49 mm outer / 46 mm inner shoe radius, continuous frames and coincident closed-rim ends. Roof source checks: all 52 front panels have thickness below the approved plane.
- Export checks: 13 building meshes / 174,834 triangles; 63 canonical meshes / 323,854 triangles; zero degenerate triangles; finite attributes and valid indices. Approved massing source and all 19 semantic anchors match. [Mesh validation](../Production/mesh-validation.json).
- The assembled prefab has **788,700 triangles**, up from the final-QA baseline of 744,595 (+44,105, 5.92%). These are unoptimized authoring assets. LOD/performance optimization remains deferred as instructed; no frame-rate target is claimed.
- Final captures comprise 144 assembled views plus all 63 canonical asset views and the production/cutaway review set. Actual imported Unity renders were inspected, including close views in shade and daylight. Automated tests are not visual approval.

The last post-test source revision only refines the lamp liner/normals and the shelter apron fold; it changes no colliders, routes, anchors, gameplay code or test expectations. Final source/export checks and the 37-check Unity preflight were repeated after that revision.

## Scope and preservation

Changed source: `ArtSource/PeriodEnvironment1930/build_kit.py`, `final_qa.py`, `validate_final_qa.py`, `README.md`, and `ArtSource/StudioServices/generate_production.py`; associated generated source/packets/native meshes, definitions and nested prefab placements are captured by the exact manifest. Review tooling adds systematic assembled captures and a scene-preserving preflight option. Three targeted tests were added to `StudioServicesProductionTests.cs`.

The exact added/changed paths and before/after hashes are in [implementation-manifest.json](implementation-manifest.json), compared with both the start-of-final-QA baseline and the five-reported-defects baseline. Existing `.meta` files were preserved. No approved massing, Stage 1, Administration, Studio scene, employee dimensions, person interaction, hiring/navigation/construction architecture or unrelated worktree changes were rewritten in this pass.

No commit or push. No LODs, water simulation, dynamic gates/doors or unrelated gameplay systems were implemented. Gates/doors remain in their authored states. Small tools and hardware are designed to read during intentional close inspection; distant management views cannot expose every construction detail. The next gate is user visual review.
