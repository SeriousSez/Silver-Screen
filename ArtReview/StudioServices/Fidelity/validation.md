# Studio Services — canonical fidelity refinement

Implemented and technically validated in Unity 6000.6.3f1. **Visual approval remains pending.** This report describes the refinement after the original production-detailing pass; it does not declare the art approved.

## Audit and source changes

All 57 original canonical assemblies were inspected. Initial findings: 16 A (retained), 29 B (targeted refinement), 10 C (substantial refinement), and 2 D (placement/connection). Individual decisions are in [canonical-audit.json](canonical-audit.json). Four supporting assemblies were added: `SackCanvasSlumped_1930`, `WastebasketWire_1930`, `CoatStandTimber_1930`, and `ClockSchoolhouse_1930`. The catalog now contains 61 assemblies; original stable IDs and asset GUIDs remain intact.

Canonical corrections originate in `ArtSource/PeriodEnvironment1930/fidelity.py`, called by the existing kit builder. `geometry.py` now transports tube cross-sections continuously through bends; this fixes pinched downpipe-shoe bores and discontinuous closed rims. The generated library blend, mesh packet and manifest were rebuilt together. Studio Services still links canonical source collections and nested Unity prefabs.

| Requested area | Implemented refinement / reviewed result |
|---|---|
| Fence and gates | Woven wire, tubular rails/posts, tension bars/bands, ties and caps. Gate frame, hinge axis/pins/brackets, tension rod, latch and handle. Both installed gate orientations and post connections reviewed. No mirrored scale. |
| Industrial windows | Six window sizes have seated panes, frame rebates, sash depth, upper operable sash and inward-facing operating hardware. Captures 18/19 show the same installed window outside/inside; capture 30 shows canonical interior hardware. |
| Employment board | Complete inset backing, frame, mounting cleats, clear header, varied papers and tacks. Header/papers are assembly content, not hardcoded player-studio identity in the canonical frame. |
| Employment entrance | Retained the curved green awning; removed the competing large porch placement. The independent porch remains reusable. Wall seat and head clearance reviewed. |
| Exterior lamps | All five placements checked. Three sign lamps moved below the coping onto the vertical facade; canonical lamp geometry retained. |
| Parapet/cornice | Closed stepped masonry and coherent coping/returns replace overlapping transition volumes. Approved outline retained. Both steps reviewed close up. |
| Roof/rooflights | Both rooflights fit the front roof plane with actual openings, curbs and flashing. Corrected source face orientation before solidifying roof sheets, removing raised sheet artifacts. Hip/ridge/seam transitions inspected. |
| Drainage | Main gutters, outlets, brackets, downpipes, open shoes and splash blocks checked on both sides. Separate supported shelter gutter/runoff path added. Hollow bends retain their bore; shoes discharge toward splash beds. No water simulation. |
| Electrical | Shared meter/junction fixtures retained. Installed penetration, conduit connections, bends and supports reviewed; no decorative dead-end wiring added. |
| Tools | Replaced ambiguous silhouettes with the identified tools below. Hooks/racks support them. |
| Workbench and vise | Laminated top, apron, stretchers, bracing, shelf, dog holes and fixings. Cast vise body, fixed/moving jaw plates, slide, screw, tommy bar/stops and bench mounting. |
| Wheelbarrow | Thin pressed bowl/rolled rim, below-tray runners, timber handles/grips, supported legs, axle/bearings and spoked solid-rubber wheel. |
| Sacks | Soft weighted volumes, flattened contact, gathered/folded closures and ties; slumped variant breaks repetition. |
| Bench | Slatted seat/raked back, metal supports, feet, brackets and bolts. |
| Barrels/crates | Staves/hoops and formed metal ribs; crate boards, battens and fixings. Grounding and stacked placements checked. |
| Lumber/racks | Rails, braces and supports; dunnage seated at actual support levels. Circulation retained. |
| Desk, cabinets, chairs | Drawer/pedestal and pull/label construction, chair cross-bracing and grounded feet. Room layout unchanged. |
| Telephone | Period handset/cradle, rotary-dial treatment and connected cord. |
| Banker/office lamp | Connected base/stem/shade construction and smoother shade curvature. |
| Workshop/personnel doors | Shallow leaf dimensions retained; interior pulls, mortice hardware, hinges, latch/drop bolts reviewed. Personnel/yard leaves remain authored open; workshop pair closed. |
| Canopies | Curved fabric and rigid corrugated structures retain distinct construction, supports and wall connections. |
| Interior pendants | Office/storage mount 3.40 m; lowest shade edge 2.7105 m. Workshop low edge 3.0105 m. Current 2.00 m employee height unchanged. |
| Office density | Added visitor chair, wastebasket, coat stand and wall clock; hiring spots and clear floor remain usable. |

### Displayed tools

- Carpenter board: claw hammer, wooden mallet, open-ended spanner, pliers, screwdriver, toothed hand saw, try square.
- Grounds rack: D-grip spade, rake, broom.
- Carrying tray: claw hammer, bench hand plane.

## Final validation

- **39/39 focused Unity tests passed**, zero failed/skipped. Final run: **2026-09-28 18:22:22–18:22:49 UTC**. See [tests.xml](../Production/tests.xml).
- **37/37 preflight checks passed**: full-size employee anchor clearances, complete isolated-bake paths, canonical references, cutaway ownership, renderer restoration and matching restored exterior. See [checks.json](../Production/checks.json).
- Actual Studio Play Mode passed: empty-studio initialization; physical applicant arrival; original release retains carry; compatible interior reveal; Escape restores pickup; separate-click immediate hiring into both Construction Worker and Groundskeeper with stable Person identity; exterior restoration; physical route through personnel access to workshop and storage. See [runtime.txt](../Production/runtime.txt) and fresh live captures 14/17.
- The unchanged per-frame capsule assertion detected millimetre-scale contact at a bench corner, then an open gate end. Scoped canonical prefab generation now supplies the same 60 mm horizontal **bake-only** guard already used for masonry to tall ground-standing workshop props and thin fence/gate colliders. This prevents native steering shaving rasterized corners. Physical colliders, visible placement, agent dimensions, anchors and movement rules are unchanged. Final traversal passed without relaxing assertions.
- An intermediate rerun timed out at applicant arrival during interrupted Editor/background execution. The active-Editor rerun reached the physical route and exposed the gate issue; the final active-Editor run passed. Earlier bench-contact XML remains diagnostic history, not the final result.
- Blender construction regression passed for all **52 front roof panels**, confirming sheet thickness lies below the approved plane: [construction-validation.json](construction-validation.json).
- Blender sweep regression passed: continuous bend cross-sections, 0.049 m outer / 0.046 m bore radius, coincident closed-rim end rings: [sweep-validation.json](sweep-validation.json).
- Export validation passed: finite coordinates, valid indices, zero degenerate triangles, unchanged approved source and all 19 anchors. Building packet: 13 groups / 161,022 triangles. Kit: 61 groups / 274,448 triangles. See [mesh-validation.json](../Production/mesh-validation.json).

## Visual evidence

[review.html](review.html) contains six before/after comparisons, installed close views, the original-57 audit and all 61 independent canonical renders. Actual Unity URP images were inspected at building/management distance, in all rooms and the yard, and at connection/detail distance. Paired window views establish orientation. Fences/gates, both parapet steps, rooflights/hip, main/shelter drains, sign lamps, electrical connections and vise have dedicated close views.

Local HTML resource links were checked separately. Interactive browser inspection was unavailable after the browser rejected local-file navigation; no alternate browser route was used. Visual inspection used rendered image files. An initial capture during URP initialization failed; it was discarded and captures were regenerated after initialization.

## Preservation and limits

The [manifest](implementation-manifest.json) records exact changed/added paths and hashes against the **9,135-file refinement baseline**. Changes belong to this refinement. No baseline file was removed; existing `.meta` files remain byte-identical. Approved massing source, exports, review and anchors remain intact. Stage 1, Administration, `Studio.unity`, gameplay scripts and unrelated dirty-worktree files were not changed by this refinement. The earlier production-integration manifest remains historical evidence of that preceding pass.

The instantiated building contains **175 shared prefab placements and 744,595 triangles before culling**. No final LODs, dense-lot performance qualification or standalone player build were completed. Explicit fence wire is a likely later LOD candidate. Dynamic gate/window/door operation, water simulation, inventory, new maintenance gameplay, character changes and new camera systems remain outside scope.

No commit or push. Technical completion and captures do not constitute user visual approval.
