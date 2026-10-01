# Stage 1 personnel door thickness and entrance spacing

2026-09-27. Focused validation complete: door reference passes; entrance spacing fails. No commits or pushes.
Unity 6000.6.3f1, URP 17.6.0, Windows / DX12.

Follow-up: the spacing failure described here is historical. The subsequent
[threshold stabilization](Stage1ThresholdStabilization.md) records the diagnosed
link-speed defect, the final local-yielding fix, 20/20 consecutive traversal passes
and passing affected runtime tests. The original failures below remain preserved.

## Scope and baseline

The user clarified that the concern is **door thickness, not leaf width**. Width,
height, wall opening, canopy, building footprint and employee dimensions remain
unchanged. The preceding full suite was 207/207, but its earlier 0.656042814 m
employee-separation failure against 0.69 m remains valid evidence.

Read the migration/stabilization documents, approved Stage 1 revision 04 records,
live integration generator/builder/prefab/navigation and runtime tests before
editing. `Logs/Stage1EntranceStabilization/files-before.json` preserves 3,146
working-tree hashes; `before/` preserves affected source/assets and context.
Comparisons use that working tree, not HEAD.

## Measurements and architectural finding

Measured editable source vertices, imported live mesh vertices, actual colliders,
NavMesh and serialized anchors. Coordinates below are Stage 1 local Unity metres;
source Blender Y corresponds to Unity Z. Measurements are not screenshot estimates.

| Item | Before | After |
| --- | --- | --- |
| Wall opening | 1.10 m wide x 2.30 m high, X 5.15–6.25, Y .08–2.38 | Unchanged |
| Leaf core width / height | 1.04 m / 2.278 m | Unchanged |
| Core thickness | .110 m | **.055 m** |
| Overall timber including raised joinery | .211 m | **.07015 m** |
| Handle-to-handle closed depth envelope | .568 m | .408 m |
| Steel jamb section / height | .08 m wide x .19 m deep / 2.40 m | Unchanged |
| Masonry reveal jamb section | .19 m wide x .34 m deep | Unchanged |
| Clear distance between steel jamb inner faces | 1.11 m | Unchanged |
| Clear distance between stop faces | 1.02 m | Unchanged; depth repositioned .0275 m toward the new leaf |
| Threshold apron | 1.10 x .48 m, top .08 m | Unchanged |
| Hinge pivot | (5.138, 0, -12.27) | Unchanged |
| Held-open angle | 95 degrees outward | Unchanged |
| Agent and capsule radius / diameter | .35 m / .70 m | Unchanged |
| Visible prototype body diameter / height | .60 m / 2 m | Unchanged |
| Open leaf plus hardware X footprint | .56584 m (4.96163–5.52747) | .40645 m (5.03635–5.44280) |
| Open leaf plus hardware Z footprint | 1.16433 m (-13.41043–-12.24609) | Unchanged |

The wide-looking edge in the supplied image was primarily excessive depth: the
110 mm core plus roughly 50 mm of joinery on each face produced a 211 mm timber
assembly. The 55 mm core and shallow raised joinery are a proportionate design
choice for this substantial industrial timber personnel door; this is an art and
construction judgment, not certification against a historical building standard.
The opening remains suitable for one .70 m capsule at a time, not two abreast.

The rear-facing pull projects the farthest into the open traversal corridor.
Its complete assembly moved .085 m toward the new face; the outside pull moved
.075 m. The pull's forged shape, tube diameter and projection were preserved.
Lock escutcheons moved .073 m, the edge plate became thinner, hinge leaves moved
.072 m and cranked returns extended to the unchanged hinge pins. Rebate stops,
connections and seals moved .0275 m to seat against the thinner core. Width,
height, threshold, canopy, wall/base trim and fixed steel jambs did not move.

## Physical and NavMesh clearance

A 1 cm X-grid at selected Z sections probes the same radius-.35, height-2 capsule
against enabled non-trigger architecture, accepting at most .005 m penetration.
It excludes the existing walkable 8 cm threshold apron from wall/door contact.
These are **capsule-centre intervals**, not raw architectural opening widths;
section values have 1 cm sampling resolution. Wider exterior sections are bounded
by the sampling window and do not represent a wall on their far side.

| Section Z | Physical centre interval before | Physical centre interval after | Interval gain |
| --- | --- | --- | --- |
| -13.2, handle region | X >= 5.88 within sample | X >= 5.79 | .09 m |
| -12.8 | 5.73–6.14 (.41 m) | 5.66–6.14 (.48 m) | .07 m |
| -12.6 | 5.74–6.14 (.40 m) | 5.68–6.14 (.46 m) | .06 m |
| -12.4 | 5.76–5.93 (.17 m) | 5.69–5.93 (.24 m) | .07 m |
| -12.2 | 5.73–5.90 (.17 m) | 5.66–5.89 (.23 m) | .06 m |
| -12.0, wall aperture | 5.54–5.86 (.32 m) | Same | 0 |

All three navigation configurations were baked and measured in temporary Editor
state without saving the Studio scene:

| Configuration | Result |
| --- | --- |
| A: thinner leaf, original volume | Physical gain exists, but old .56584 m volume unnecessarily retains the narrow contour: centre interval .04 m at Z=-12.4 and no sampled polygon at Z=-12.2. |
| B: thinner leaf, recalculated volume | Volume X shrinks to .40645 m. Centre intervals .18 m at Z=-12.4 and .11 m at Z=-12.2. Dense leaf/NavMesh probe: 1,963 samples, zero leaf overlap. Selected. |
| C: thinner leaf, no extra volume | Wider contour, but 34 of 2,427 sampled NavMesh positions exceed .005 m leaf penetration; worst .045053814 m. Normal erosion alone is insufficient. Rejected. |

The dense probe uses actual sampled NavMesh heights and 2 cm X/Z grid spacing;
these are geometric candidate-position checks, not employee traversal outcomes.
Configuration C's worst sample is (5.0, .025, -13.64), near the opened leaf tip.
Do not confuse this rejected-configuration result with final traffic penetration.
The volume remains derived from real mesh/hardware bounds with existing agent
radius erosion, with no new arbitrary padding. Its centre is
(5.23957, 1.225, -12.82826), size (.40645, 4.288, 1.16433).

Existing anchors remain: ExteriorApproach (5.7,0,-13.8), PersonnelThreshold
(5.82,.08,-12.09), InteriorArrival (5.7,.08,-10.7), FrontServiceAccess
(4.8,.08,-8). The bidirectional zero-width threshold link retains start
(5.85,.08,-12.90), end (5.82,.08,-11.05). All production station/actor/slate
anchors remain unchanged; full coordinates are in `unity-measurements.json`.

## Source and asset changes

The existing `ArtSource/Stage1LiveIntegration/build_live_parts.py` is the
authoritative live derivative generator. Its source correction preserves the
approved revision 04 visual-master blend byte-for-byte and asserts unchanged
geometry/UV/material fingerprints for other personnel doors. It generates the
existing FBX export and identical Unity copy, with importer mappings and GUIDs
preserved. The manifest now accurately records intentional closed-geometry
changes instead of claiming full visual-master equivalence.

All 70 affected-opening components are closed, outward and non-degenerate.
Timber depth is asserted during regeneration. A source BVH swing check at every
5 degrees from 0 through 95 reports no intersections with surrounding architecture
or fixed jambs/stops, using the established exclusions for intentional hinge,
latch and compressible-seal contact. This is a geometric sweep, not a new runtime
animated-door feature.

The existing prefab volume was recalculated, preserving object IDs. Only
`Stage1_Interior.asset` and the affected shared `Studio_Revision04.asset` were
rebaked, preserving GUIDs. The final triangulation has 6,859 vertices. No unrelated
world/navigation asset was rebaked and Studio.unity was not saved.

`Stage1LiveRuntimeTests` gains failing-sample diagnostics only: pair, exact local
positions, velocities, desired velocities, destinations, path corners, agent
state, avoidance priority, automatic/off-mesh traversal, capsule contact and door
pose. All original assertions, exterior ordinary-navigation setup, speed and
capsule dimensions remain enforced.

## Original interrupted validation

The stress script runs at most 20 separate graphics-enabled EditMode traversal
runs and stops on the first test failure. It does not retry failed tests. An
initial launch while the interactive Editor was still shutting down exited
before producing tests; its log is retained separately as launch tooling evidence.

The original sequence recorded one pass followed by a 0.573354244 m separation
failure on run 2. That evidence remains in `Logs/Stage1EntranceStabilization`.
The subsequent Movie Maker full suite exposed the obsolete closed-door reference
assertion; no Movie Maker code caused the geometry difference.

## Approved-door reference completion

The user reaffirmed approval of the thinner door and requested focused validation
only. The old test compared every closed-pose coordinate to the original review
model. Its first mismatch was a 27.5 mm depth displacement, exactly half the
110→55 mm core correction. This was an obsolete depth invariant, not a request
to restore the thicker door or change leaf width.

The already-completed independent source audit matched all **51,912 imported
triangles** against the approved generator transformation. Maximum FBX round-trip
coordinate/UV error was **0.000003**, with zero mismatched triangles. All source
width/height coordinates, UVs, polygon topology and material assignments remained
unchanged. The 70 affected-opening components remained manifold, nondegenerate
and positively oriented. Existing source/FBX assets were not regenerated.

`ClosedDoorDerivativeRetainsImportedGeometryUvsAndMaterials` now checks:

- The existing hinge pivot `(5.138, 0, -12.27)` and 95° held-open pose.
- Correct leaf parenting and collider/shared-render-mesh identity.
- All triangles' width/height coordinates, UVs and actual material GUIDs against
  the original import, excluding only the approved depth change from this check.
- Exact XYZ/normal/UV/material equality for **44,496 unaffected triangles**.
- All **51,912 approved closed-pose triangles**, including depth, normals, UVs,
  material asset assignments and hardware relationships, against a frozen SHA-256
  fingerprint at the existing 0.0001 quantization precision.

Approved fingerprint:

```text
fb42d59e75d0ed7f0234d4fed7ac57df286aac9f0a639a3828bfb559a2a133ca
```

It hashes sorted, triangle-based Stage-local records with invariant formatting;
it is not automatically regenerated from a failing asset. Evidence is
`Logs/Stage1ApprovedDoorValidation/approved-fingerprint.json` and
`source-audit.json`. Both the depth-independent comparison and the unaffected
triangle comparison were independently confirmed before filling the placeholder.

The corrected reference test and four directly related existing tests passed:
`DoorAndThresholdFailClosedWhenPoseIsInvalid`,
`HumanCapsuleFitsBodyWithMarginWithoutChangingHeight`,
`SelectionDoesNotObstructTheFilmingFloor`, and
`LiveIdentityIsUniqueAndReviewIdentityIsPreserved`. No unrelated test was added
or executed. Runtime and Editor assemblies compiled on Unity 6000.6.3f1.

## Twenty-repetition validation protocol

The resumed request explicitly requires **all 20 repetitions**, retaining every
failure. Unlike the original interrupted protocol, a failed repetition does not
stop the complete sequence and is never retried/replaced. Each repetition opens
the saved Studio scene, enters Play Mode, navigates every employee outside using
the existing ordinary-route precondition, and then measures the entrance route.
The existing test now records and asserts immediately on its first separation
sample below 0.69 m. Diagnostics include both agents' Stage-local positions,
velocities, desired velocities, destinations, paths, priorities, avoidance,
NavMesh/off-mesh-link states and pair/collider contact. Production navigation,
geometry, body/capsule dimensions and thresholds remain unchanged.

Execution uses the Unity Test Runner through the existing Pipeline bridge;
`stress20.ps1` only orchestrates consecutive runs and preserves their results.
Each numbered `stress-NN-result.json` and `stress-NN-runtime.txt` is retained in
`Logs/Stage1ApprovedDoorValidation`, with aggregate metrics in
`stress-summary.json`. Failed runs stop at the first observed violation, so their
metrics describe the observed prefix, not a completed traversal.

## Completed traversal results

**20/20 executed: 13 passed, 7 failed, 0 skipped. No retries or replacements.**
Worst observed wall/door penetration: **0 m** (limit 0.005 m).
Worst observed employee separation: **0.651486039 m** (minimum 0.69 m).
Failed repetitions ended at their first violating sample; later route portions
are unobserved. All 20 numbered results and runtime logs are retained, with every
run's metrics in `Logs/Stage1ApprovedDoorValidation/stress-summary.json`.

| Failed run | Pair | Minimum separation (m) | Situation |
| --- | --- | --- | --- |
| 6 | Stanley Hawk / Leo Morris | .651486039 | Exterior convergence, entering |
| 8 | Stanley Hawk / Leo Morris | .671723068 | Exterior convergence, entering |
| 9 | Jack Sterling / Clara Vance | .6864862 | Exterior convergence, entering |
| 10 | Stanley Hawk / Leo Morris | .6875625 | Exterior convergence, entering |
| 11 | Jack Sterling / Clara Vance | .6828671 | Following inside entrance, entering |
| 16 | Stanley Hawk / Leo Morris | .6829588 | Exterior convergence, entering |
| 20 | Stanley Hawk / Leo Morris | .655808866 | Exterior convergence, entering |

All failure details are in `Logs/Stage1ApprovedDoorValidation/failure-diagnostics.json`
and the corresponding `stress-NN-runtime.txt`: positions, actual/desired velocities,
destinations, next positions, paths, priorities, avoidance and NavMesh/link states,
physical pair overlap, time/frame and door pose. Coordinates are Stage-local metres.

Worst sample, run 6:

| Agent | Position | Actual velocity | Destination |
| --- | --- | --- | --- |
| Stanley | (5.737877,1.025000,-13.672660) | (.615074,0,2.686747) | (-.799999,.081002,-6.199999) |
| Leo | (6.316458,1.037453,-13.373200) | (-13.246790,1.925118,16.764160) | (.799999,.081002,-6.199999) |

Both were Walking, on NavMesh, PathComplete, not stopped/pending, priority 50,
HighQualityObstacleAvoidance, radius .35, configured speed 3.5 and acceleration 14.
Leo was auto-traversing the link; Stanley was not on it. Their centres were
1.37–1.67 m outside facade Z=-12, converging inward; capsule overlap was .04851395 m.
Link start (5.855619,.101679,-12.908300), end (5.82,.08,-11.05).
Door remained passable at 95 degrees. Frame 4754, deltaTime .00646870025.

In run 11, Jack at (5.824116,1.082506,-11.264790) followed Clara at
(5.538572,1.081002,-10.644490) just inside the opening; Jack was on the link,
Clara was off it. All seven failures coincide with one agent traversing the
existing automatic threshold link. Reported actual velocities on that link exceed
the configured speed. This identifies an investigation target, not a proven
engine/root-cause diagnosis. No crowd navigation changes were attempted.

## Completed visual inspection

Inspected six actual Studio URP renders: exterior front/oblique, close hardware,
interior looking outward, and closed from both sides. Evidence is in
`Logs/Stage1ApprovedDoorValidation/views`. A static capsule's renderer was hidden
only for unobstructed static renders and restored immediately; no geometry,
physics, dimensions or saved scene was changed.

The 55 mm core reads as a believable substantial 1930s industrial personnel door.
Leaf/frame/jamb seating, hinge/pivot connection, handle and lock placement,
threshold, wall/base trim and canopy relationships look consistent. No obvious
unintended gaps or door/architecture clipping were visible in the inspected views.
The thin edge and hardware remain visually coherent in both open and closed poses.

A separate manual runtime observation used Jack's ordinary task destinations to
walk outside first (verified Z<-14), then inside. It completed without teleporting,
with radius .35 and speed setting 3.5. The inspected approach/threshold frames show
clear physical passage around the open leaf. Evidence: `passage/result.json` and
`passage/frame-000.png`, `passage/frame-001.png` in the same evidence directory.
This was a visual smoke observation, not a replacement for a failed stress run.
Only two sampled frames cover passage; the link sample reports about 8.23 m/s
inward velocity. Smooth, natural traversal timing therefore is **not signed off**,
even though body/door clearance is visually satisfactory.

## Final recommendation and scope

The approved thinner-door geometry and its strong reference are validated. The
entrance as a complete runtime interaction is **not validated**: intermittent
employee-spacing failures remain (7/20), and natural link traversal timing needs
separate investigation. Keep the approved geometry. The next task should isolate
threshold-link/employee-spacing behavior using the retained evidence before
choosing a navigation fix; no speculative avoidance redesign was attempted here.

Focused geometry/import/asset tests: **5/5 passed**. Runtime stress: **13/20 passed**.
No complete project-wide suite was run, no unrelated test was added, and no feature
work was performed. Runtime/Editor compilation succeeded. The temporary static
capture helper emitted deprecated FindObjectsSortMode warnings; it is outside
Assets and does not change production assemblies. No commit or push was made.

## Editor state preservation

A modal save prompt initially blocked the background Test Runner. It was canceled
and the unsaved Studio state was preserved as
`Logs/Stage1ApprovedDoorValidation/Studio-unsaved-before-tests.unity` before loading
the saved project scene. The disposable Movie Maker proof window was closed after
it reported a pre-existing domain-reload error; it was not modified in this task.
Static door views use a temporary camera through the real Studio URP rendering
setup, restore the held-open hinge and do not save scene/asset changes.

The Editor was returned to Edit Mode on the saved Studio scene. The preserved
unsaved copy remains available at the path above; it was not written over the
authoritative scene. A final comparison with this task's starting file hashes
found only the two existing test files and this document changed. Unity's
incidental `SENTIS_ANALYTICS_ENABLED` project define was restored to the exact
starting ProjectSettings bytes. Approved meshes, FBX, materials, prefabs, NavMesh,
source geometry and the saved Studio scene remain unchanged in this resumed task.
