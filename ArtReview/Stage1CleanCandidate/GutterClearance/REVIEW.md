# Stage 1 — gutter and eave clearance

**Corrected at source; stopped for visual review.** [Open the before/after Unity gallery](review.html).

The collection assembly was embedded in the eave: both gutter runs, four end caps, both drip profiles and all four upper downpipe connections intersected the masonry cornice. The curved roof ended at Z=7.084 m behind a cornice reaching 7.18 m, so its runoff path was obstructed. The former solid downpipes met a gutter floor without openings. These defects were missed by the preceding review and are now covered by source checks.

The curved sheet now stops 140 mm farther inboard, at X=±7.67 m and Z=7.250 m. A thin, falling metal apron laps the sheet by 90 mm, crosses above the unchanged cornice and drops into an external half-round gutter. Standing seams end upstream of this lap. The apron has supported cleats, folded end returns and small weather joints at the end coping. The roof crown, barrel curvature, end-cap profile, masonry cornice and previously corrected finials are preserved.

Both 200 mm gutters sit at X=±8.20 m, outside the eave masonry, with open tops and closed stop ends. They fall 1:400 from the midpoint toward outlets at Y=±11.40 m; the short end sections also fall back to those outlets. Each run has 19 wall-mounted cradle supports, approximately 1.28 m apart. Four shaped, open outlet sockets feed hollow doglegs and downpipes. Wraparound clips support the pipes without blocking their bores. The existing lower downpipe route and splash-block locations are retained.

| Check | Result |
|---|---|
| Gutter/end cap/downpipe versus building envelope | No detected intersections |
| Apron versus cornice/end cap and standing seams | No detected intersections |
| Gutter versus arch coping, lateral separation | 76 mm |
| Minimum apron tip above gutter rim | 26.5 mm |
| Midpoint-to-outlet fall | 28.5 mm |
| Runoff landing inside channel | 82/82 source rays |
| Outlet and hollow downpipe paths | All 4 open; 34 clear segments each |
| Downpipe shoes discharge onto retained splash blocks | 4/4 |
| Previous finial fix | All 24 finial/support components unchanged; four clearance checks pass |

The thin apron weather lap intentionally meets the roof sheet. Outlet sockets have a 1.5 mm fitted lip above the trough to prevent coincident surfaces; the water bore stays open. These are construction joints, not concealed gutter/roof intersections. The outlet lips and channel curvature were checked again in Unity after correcting an initial coincident-surface artifact.

Source changes: [generate_stage1_candidate.py](../../../ArtSource/Stage1CleanCandidate/generate_stage1_candidate.py) sets the revised local runoff edge; [production_geometry.py](../../../ArtSource/Stage1CleanCandidate/production_geometry.py) terminates the seams before the apron; [production_corrections.py](../../../ArtSource/Stage1CleanCandidate/production_corrections.py) delegates the rainwater work to new [production_rainwater.py](../../../ArtSource/Stage1CleanCandidate/production_rainwater.py), which owns the gutter, flashing, connections, supports and regression checks. The editable blend, source reports and both identical FBX exports were regenerated through the existing workflow. The candidate prefab/review scene and source README were refreshed. No production C# changes were needed.

Source mesh comparison: 6,537 components retain identical geometry/topology/material assignments. Changes are limited to the local curved-sheet/seam/end-flashing edges and rainwater assembly. The master contains 6,967 closed, outward components, 668,668 triangles and 29 export meshes; no degenerate faces. Grounding, door sweeps, lamps, blackout coverage and circulation checks also pass.

Actual Unity 6000.5.9f1/URP review: both entire runs from elevated front and rear angles, both midpoints, all four transitions from oblique and profile views, all four open outlets and full downpipes, plus front/three-quarter/side/rear overviews. All 32 native captures were inspected: 6 original and 26 corrected. The final set was recaptured after the seam and socket corrections. Console: **0 errors, 1 UI font warning**: LiberationSans SDF lacks the Roman numeral II (U+2161) used by a UI Label. The affected font/UI files were not changed by this correction. Import, mesh channels, material references, apertures, sign identity, reflection assignments and 31-light integrity passed.

All 483 protected files and 49 existing candidate `.meta` files are unchanged. Original Stage 1 and Administration remain intact. Studio was not saved or reloaded; no scene placement changed during this correction. The temporary preview is closed. Studio was in Play Mode by the final integrity check; this task did not enter or exit Play Mode. The validation applies to the isolated candidate, not gameplay behavior in the running scene.

The checks establish geometric drainage continuity, not rainfall capacity or hydraulic simulation. Existing material and close-range shadow artifacts remain. No gameplay validation, player build, LODs, optimization, prop extraction, commit or push.

[Full source and Unity evidence](validation.json)
