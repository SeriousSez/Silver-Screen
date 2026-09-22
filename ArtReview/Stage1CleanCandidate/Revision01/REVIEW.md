# Stage 1 — clean candidate A: massing approval gate

**Ready for massing review, with limitations. Production detailing is stopped.**
Actual Unity visuals were inspected on 2026-09-22. This is not finished production
art or a claim that the full concept has been matched.

Open [the visual comparison gallery](review.html) for equivalent concept/Unity views
and all nine full-size captures. Original image pixels and Unity captures are
unaltered; the gallery frames concept excerpts for easier comparison.

## New files

| Artifact | Repository path |
|---|---|
| Independent generator | `ArtSource/Stage1CleanCandidate/generate_stage1_candidate.py` |
| Editable source | `ArtSource/Stage1CleanCandidate/Stage1_CleanCandidate_A.blend` |
| Source report | `ArtSource/Stage1CleanCandidate/generation_report.json` |
| Approved reference copy | `ArtSource/Stage1CleanCandidate/ApprovedConcept.png` |
| FBX export | `ArtExports/Stage1CleanCandidate/Stage1_CleanCandidate_A.fbx` |
| Imported FBX | `Assets/SilverScreen/Environment/Stage1CleanCandidate/Models/Stage1_CleanCandidate_A.fbx` |
| Candidate prefab | `Assets/SilverScreen/Environment/Stage1CleanCandidate/Stage1_CleanCandidate_A.prefab` |
| Isolated review scene | `Assets/SilverScreen/Environment/Stage1CleanCandidate/Stage1_Candidate_A_Review.unity` |
| Import/capture companion | `Assets/SilverScreen/Environment/Stage1CleanCandidate/Editor/Stage1CandidateReview.cs` |

The previous Stage source/export/prefab and the original Studio scene are retained.
The review scene copies Studio's actual lighting, camera and neighbours. Only in
this separate copy is the old Stage hierarchy inactive, with the candidate at the
same `(0,0,-12)` location. Administration has not been edited or regenerated.
The candidate has no production-router registration and is for Edit Mode review.

## Dimensions

| Measurement | Metres |
|---|---:|
| Nominal exterior wall footprint | **15.8 × 24.0** |
| Eave / roof spring line | **8.00** |
| Barrel roof crown | **12.00** |
| Front/rear coping crown | 12.10 |
| Vent cap maximum | 12.88 |
| Extents including trim and canopies | 16.22 wide × 25.755 deep |
| Foundation underside | -0.20 |
| Filming floor top | +0.08 |
| Main portal masonry opening | **7.20 wide × 7.60 high** |
| Front personnel masonry openings | 1.10 wide × 2.30 high |
| Rear service masonry opening | 1.40 wide × 2.60 high |
| Typical wall thickness | 0.32 |
| Interior between wall faces | **15.16 × 23.36** (~354 m² gross) |
| Central width between catwalk allowances | 12.66 |
| Catwalk deck top / allowance width | 5.40 / 1.02 |
| Main arch structural-depth allowance | 0.30 |

The usable floor is smaller than the gross interior rectangle because perimeter
columns and the stair allowance occupy its edges. There are no central columns.
The dimensions above are authored metric values, verified against imported Unity
bounds and a collider floor hit at +0.08 m. Clearance will change slightly when
door frames, structure and acoustic treatments are resolved after approval.

## Architectural decisions and concept comparison

- **Silhouette:** an elliptical barrel, 64 segments across the full arch, covers
  a long rectangular industrial volume. The uninterrupted arched facade and
  front portal hierarchy follow the approved concept. It is clean geometry.
- **Front:** a substantial projecting portal surrounds two independent door
  leaves. Personnel entrances flank it; the right entrance has the rain-hood
  volume. Corner pilasters, capitals and arched coping establish depth. The flat
  leaves are explicitly massing placeholders; timber/steel detail is not approved
  or represented as complete.
- **Side:** three structural bays and three long clerestory openings establish
  the concept's high window rhythm. Two service doors per side reserve access.
  Their final reveals and permanent service infrastructure are deferred.
- **Rear:** a simple arched end, central service doorway and three high ventilation
  allowances. No duplicate main stage portal or loose dressing is introduced.
- **Roof:** two symmetrical glazed ribbons preserve the concept's visible roof
  character. The unseen opposite side is a conservative symmetry assumption.
  Closed opaque shutter volumes below the roof glazing and clerestories reserve
  full daylight control. Historically styled sliding hardware, acoustic seals,
  sheet seams and vent construction remain to be designed during approved detail
  work. The panes currently use opaque study material: glass transparency and
  final light leakage/acoustic performance have not been validated.
- **Interior:** connected arch/column depth allowances, a calm floor slab and
  perimeter circulation reservations preserve a clear filming volume. The stair
  is a sloped space reservation, not a finished ramp or stair asset. Catwalk plates
  reserve deck space, not a completed load-bearing/guarded assembly. Rigging,
  truss webs, longitudinal members, work lighting and sound treatment are deferred.
- **Sign:** existing instance-driven glyph architecture, directly on masonry,
  with no panel. The lettering is readable in the inspected front and management
  captures. Stage number 1 is not in the reusable FBX or material content.
- **Materials:** nine simple source-authored URP study materials establish warm
  masonry, a darker base, charcoal roof/steel and dark timber. They are deliberately
  below the Administration finish benchmark at this gate. Final PBR maps, UVs,
  physical glass, surface variation and bevel refinement are not completed.

The broad identity reads correctly in the actual Unity captures. The reference's
material richness, roof seams, timber construction, lamps and utilities are absent
by design at this approval gate; their absence is not claimed as concept fidelity.

## Matters to resolve before production detailing

1. **Height relationship to Administration.** Its inspected highest point is
   approximately 11.02 m. Candidate A's roof crown is 12.00 m, with vents to 12.88 m.
   It therefore reads taller in the same-plane comparison. The concept's comparison
   tends to give Administration's tower more vertical prominence, though perspective
   differs. Review reducing the proposed roof height by roughly 0.8–1.0 m, or
   explicitly accept the present envelope. Administration should remain unchanged.
2. **Default management framing.** The saved camera `(0,18,-15)`, pitch 52°, FOV 60
   sits above the Stage footprint and mostly sees its roof. The panned 52°/60° view
   establishes that the whole candidate and door composition read at a normal
   controller distance (34 m, within its 8–45 m range). A later camera-framing
   adjustment should accompany integration; no camera-controller code was changed.
3. **Interior circulation and structure.** Approve the clear floor and 5.4 m
   catwalk level before replacing the allowances with engineered-looking arches/
   trusses, supporting beams, stairs, rails and distinct film rigging. Reserve
   shutter travel and acoustic build-up without losing the filming volume.

## Technical integration inspected

| Existing system | Finding / implication for later integration |
|---|---|
| Stage identity | Active fallback is `SoundStage1`, facility `stage-1`, number 1. Candidate uses review-only identity and no building view. Never register both as one facility. |
| World routing | `StudioWorldRouter` discovers active `StudioBuildingView` instances and rejects duplicate Stage numbers/IDs. Preserve the live identity/root when eventually replacing only approved architecture. |
| Interaction point | Existing local entrance `(-5.8,0.05,-12.8)` is near a front personnel entrance; final approach must be aligned with the approved threshold/navigation. |
| Production stations | Existing stations local Z=-14.3, camera/sound/crew Z=-15.8, outside the new -12..12 envelope. Explicit authored anchors must replace the obsolete defaults. |
| Actor/slate/blocking marks | Local Z=-17.1, also outside the stage. Existing serialized layouts are not automatically relocated by `EnsurePrototype*` methods. |
| Live Camera View | `PrototypeProductionCamera` hardcodes wide focus `(0,1.2,-11)` and position `(0,7,-19)`. The wide camera is outside the building and must adapt to interior anchors. `LiveFilmingPlayback` and production binding stay on the live facility. |
| Colliders | Existing root box is 16.1 × 7.6 × 24.3 m: too low for this roof and solid through the interior. Candidate uses separate non-convex shell/floor/door colliders for review. Final selection/navigation collision roles need separation. |
| Navigation | Existing Stage navigation asset retained; no rebake or employee routing test. Rebuild access from the approved entrance/floor geometry only after integration approval. |
| Dynamic sign | Existing components and glyph library reused independently of old architecture; candidate source contains no letters or backing plaque. Switch 1→2→1 was checked in Unity. |
| Scene/pivot | Ground-centred Unity wrapper at `(0,0,-12)`, scale 1; separate model root uses the repository's Blender correction. All old scene references remain unchanged in Studio. |

No gameplay hooks were redirected during this visual gate. Full production
routing, animation, live-camera shots and navigation through the new building are
**not tested** and must not be inferred from these editor checks.

## Validation evidence

| Criterion | Result |
|---|---|
| Clean source and separate candidate | Passed: independent generator, no old Stage source/geometry loaded |
| Metric scale, semantic groups and copies | Passed: 1 m/unit, applied scale, 15 FBX groups; export and Unity-copy SHA256 match |
| Source geometry | Passed: 350 components, all closed/outward; zero degenerate faces; 11,436 architectural triangles |
| Unity import / Editor compilation | Passed through installed official Unity MCP; native URP materials correctly remapped |
| Candidate references | Passed: zero missing scripts/meshes/materials; 16 renderers including the dynamic sign, 24 material slots |
| Unity triangle count | 15,472 including the existing dynamic glyph meshes; no frame-time claim |
| Candidate collider checks | Passed: five mesh colliders, centre 20 m horizontal ray clear, floor hit at +0.08 m, no masonry across three front apertures |
| Stage lettering | Passed: glyph geometry changes for Stage 2, restored to Stage 1; no visible backing panel |
| Actual rendering review | Passed: nine Main Camera URP captures produced through MCP and visually inspected, including shaded rear and interior |
| Console | Zero warnings/errors before and after; no Console clearing |
| Original files preserved | Baseline hashes checked for 483 existing files covering original scenes, scripts, Stage/Admin sources/exports/assets, settings and packages |
| Play Mode / navigation / player build | Not run; outside this isolated architectural approval gate |
| Finished production quality | Deferred by explicit user instruction |

Captures use the review copy's actual PC URP renderer, lighting, sky, global volume
and Main Camera. No added fill lights, material brightness compensation, AI renders
or composited geometry were used. Original scene capsules/buildings in the
background are existing context; they are not in the candidate FBX or prefab.
Camera poses are restored after captures. `source_validation.json` and
`unity_validation.json` retain numerical evidence.

## Unity captures

| Capture | Purpose |
|---|---|
| [01 — front three-quarter](01_front_three_quarter.png) | Primary architectural comparison |
| [02 — straight front](02_front.png) | Arch, portal, personnel doors and sign proportions |
| [03 — side](03_side.png) | 24 m depth, bay rhythm and clerestories |
| [04 — rear](04_rear.png) | Arched rear, service opening, shaded face |
| [05 — roof/silhouette](05_roof.png) | Curvature, glazing ribbons and vent locations |
| [06 — original management pose](06_management_original_pose.png) | Unchanged saved Main Camera framing; roof-dominated |
| [07 — management camera panned](07_management_candidate.png) | Same 52° pitch / 60° FOV at 34 m controller distance |
| [08 — beside Administration](08_beside_administration.png) | Actual adjacent asset size and vertical hierarchy |
| [09 — interior wide](09_interior_wide.png) | Clear-span floor, arch depth and circulation allowances |

**Awaiting the user's explicit massing approval. No production detailing, live
Stage replacement, commit or push has been performed.**
