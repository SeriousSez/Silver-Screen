# Stage 1 — four finial clearances

**Corrected at source and stopped for visual review.** [Open the before/after Unity gallery](review.html).

The original placement embedded every finial assembly in the curved fascia, outer coping and end-wall masonry. Each plinth also intersected the end flashing. This issue was missed by the preceding revision 04 visual review; it is now covered by permanent source assertions.

The complete approved finial and plinth move 325 mm outward from each front/rear wall, preserving their profiles, widths, heights and materials. The two upper supporting cap courses extend 145 mm outward. This creates a real 40 mm separating gap from the entire roof/end-cap envelope and leaves 28 mm of bearing margin inside the cap bevel. The roof and its end-cap meshes are unchanged. No mesh is cut away to conceal the former overlap.

| Corner | Roof/end-cap gap | Plinth bearing margin | Roof intersections |
|---|---:|---:|---:|
| Front left | 40 mm | 28 mm | 0 |
| Front right | 40 mm | 28 mm | 0 |
| Rear left | 40 mm | 28 mm | 0 |
| Rear right | 40 mm | 28 mm | 0 |

The source fix is in [production_geometry.py](../../../ArtSource/Stage1CleanCandidate/production_geometry.py); [production_corrections.py](../../../ArtSource/Stage1CleanCandidate/production_corrections.py) now verifies all four assemblies against the roof, fascia, coping, end wall, glazing and rainwater geometry, plus full plinth bearing. A separating-plane assertion supplements actual mesh intersection checks so fully embedded parts cannot escape detection. The existing small masonry bedding contact between each plinth and cap is retained; the roof clearance is open space.

Regenerated the editable Blender source, generation/defect reports and both identical FBX exports through the existing generator. Refreshed the candidate prefab and isolated Unity review. Updated the source README and linked this addendum from the preceding review; no production C# changes were needed.

Validation: all four original defects reproduced; all four corrected finials inspected from oblique, side and roofward directions in actual Unity 6000.5.9f1/URP, plus front, three-quarter, side and rear overviews beside the approved concept excerpts. The gallery contains 4 original and 16 corrected native captures. Source hashes confirm only 24 components changed: four ornament/plinth assemblies and eight supporting cap pieces. The other 6,675 source meshes are identical; triangle count remains 630,004. Existing ground, door, lamp, blackout and circulation assertions also passed during regeneration.

Unity import, mesh channels, materials, openings, sign identity, reflection assignment and 31-light integrity passed. Console: **0 errors, 0 warnings**. 483 protected files and all 49 candidate `.meta` files are unchanged. Original Stage 1 and Administration are preserved. Studio was not saved or reloaded, and no scene placement was changed during this follow-up. The temporary preview is closed.

Existing close-range shadow stepping remains visible on small masonry details; material and lighting changes are outside this clearance correction. No structural load simulation, Play Mode or player build was performed. No optimization, LODs, prop extraction, commit or push.

[Machine-readable source and Unity evidence](validation.json)
