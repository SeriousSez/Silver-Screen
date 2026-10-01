# Period environment kit — 1930

Independent canonical source for Studio Services and later office, workshop, yard and film-set dressing. `build_kit.py` uses deterministic metre-scale geometry helpers in `geometry.py`. Regeneration writes the editable collection library, native mesh packet and catalog manifest together. Do not hand-edit the generated Unity meshes.

63 assets retain the original 57 catalog identities and add a slumped sack, wire wastebasket, coat stand, wall clock, right-hand gate leaf and shared fence post. They cover six steel-window sizes; shallow personnel leaves, frame and service-door pair; canopies; office furniture and accessories; workshop equipment and recognizable tools; yard storage, fencing, gates; hollow rainwater and conduit components.

Unity outputs: `Assets/SilverScreen/Environment/PeriodEnvironment1930/{Meshes,Materials,Prefabs,Definitions}`. Every asset has a stable catalog ID, canonical prefab, shared material roles, metre dimensions, placement surface, functional clearance and 1930 availability with no forced end year. Metadata does not activate a new Set Builder or inventory system.

## Coordinates and composition

- Blender: metres, Z up, source face −Y. Collection origin is its wall mount or ground contact centre.
- Unity: metres, Y up, canonical front +Z. The exporter maps (x,y,z) to (−x,z,−y), reverses triangle winding, and retains explicit UVs and normals.
- Do not scale instances to fit openings. Window/door dimensions have explicit names; supported sizes are independently catalogued.
- Door leaf thickness is 55 mm; the service leaves are 60 mm with shallow framing/hardware. Leaf pivots are assembly centres for this held-open proof; hinge hardware remains visible. A future animated door should add a hinge-parent transform at its measured hinge axis, without moving the canonical mesh origin.
- Gutters have Start/End socket anchors and a swept open 90-degree corner; the 0.40 m outlet has a circular drain aperture and a drawn hollow throat. Downpipe ends and discharge shoes retain hollow interiors. Roof-to-gutter drip flashing and cut-to-length offsets belong to the building assembly. No water simulation.
- Fence sections contain rails, tensioned wire and terminal bands. Place one `FencePostTubular_1930` per unique junction; a gate hinge post may supply the terminal support. Wall terminals require explicit wall-mounted sockets/spigots. Never overlap two embedded posts at a section join.
- The two gate leaves have explicit handedness baked into positive-scale geometry. Source +Y is the yard face when closed. The LH leaf carries the keeper/drop bolt; the RH leaf carries the sliding latch. Both have graspable return pulls. Do not mirror an instance to create the opposite hand. The Studio Services LH leaf opens into the yard; RH opens outward; their primary hardware belongs to the same closed-gate face.
- Noticeboard frame is blank canonical content. Studio Services notice inserts are separate building content; they contain job/function text, no assumed player studio name or year.
- Box collision is owned by major physical assemblies. Service clearance is metadata, not a blocking collider. Small hardware and desk accessories inherit collision from their supporting furniture.

## Reproduction / provenance

Run `build_kit.py` before `ArtSource/StudioServices/generate_production.py`. Unity's scoped production builder imports both. Export manifests retain canonical IDs, source paths, generator hashes, bounds, material roles and placements; the build evidence records source/export hashes. These are development records, not runtime gameplay dependencies.

`fidelity.py` is part of that same build and refines canonical collections before export. Its hash and the geometry helper hash are recorded in the kit manifest. `validate_sweeps.py` runs in Blender without saving source assets; it checks that downward pipe bends retain their bore and that rolled closed rims join without a frame discontinuity. Window operating hardware is on source +Y (interior); the source exterior is -Y. Gate HingeAxis/Latch anchors describe the fixed assembly; no dynamic behavior is added.

`final_qa.py` runs after `fidelity.py` in that same build. It owns the subsequent manufacturing corrections and is also hashed in the export manifest. Evaluate Blender's dependency graph before baking or mirroring object matrices: deferred scale on the last latch cap previously exported a two-metre sphere. `validate_final_qa.py` checks gate bounds, cap dimensions, the open outlet bore and the lamp's closed shade construction against the generated source. These checks supplement, and cannot replace, close visual review.

Furniture changes belong in this library. Building placement/layout changes belong in Studio Services' placement manifest generator. Existing `ReusableFixtures` assets remain authoritative for the reused lamps and electrical cabinets. Do not copy their geometry or edit Stage 1 to change this kit.

No final LOD chain, automatic snap placement, movable-door behavior, prop inventory, rain simulation or period-upgrade simulation is implied by these authoring assets. Repeated placements share meshes/materials. See the current Unity `ArtReview/StudioServices/Production/checks.json` for the measured triangle count. Population-scale performance and LOD tuning remain a measured later pass.
