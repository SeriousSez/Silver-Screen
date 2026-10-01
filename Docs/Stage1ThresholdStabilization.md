# Stage 1 threshold/link investigation

2026-09-27, Unity 6000.6.3f1. Focused work only; no Computer Use, full suite,
commit or push. The final candidate passes the requested focused technical
validation. Subjective visual approval remains with the user.

## Baseline and diagnosis before implementation

The approved 55 mm door and its fingerprint remain fixed. The seven recorded
failures were read first. Six are exterior convergence at automatic link entry;
run 11 is Jack following Clara just inside, while Jack is still on the link.
Every pair contains one on-link and one ordinary walking agent. All are inbound,
with complete paths, priority 50, HighQuality avoidance, radius .35, configured
speed 3.5 and acceleration 14. No opposing-direction collision is in this sample.
Different destinations and pairs rule out a single employee/station exception.

On-link reported speeds range 8.23534–22.35614 m/s; peers remain 1.36–2.87 m/s.
Desired on-link speed is 3.5 m/s in each failure. All seven physical capsule
queries confirm overlap; these are not only a distance-reporting discrepancy.

The authored threshold was a zero-width bidirectional link, start
(5.85,.08,-12.90), end (5.82,.08,-11.05), with automatic traversal and no authored
traversal duration. An entire building-footprint exclusion separates the lot
surface from the independently baked interior. With the link disabled, paths
are partial. Normal agent settings allow climbing .75 m; the apron is only .08 m.

A focused baseline run with independent frame-to-frame displacement measurement
passed separation but recorded 22.93998 m/s horizontal and 23.0206146 m/s spatial
movement at automatic link entry. Reported NavMesh velocity agreed. Therefore
the spike is real motion, not a normalization/clock/reporting issue. Evidence:
`Logs/Stage1ThresholdStabilization/baseline-speed-measured-*`.

Automatic off-mesh movement is unsuitable for this ordinary walking threshold:
it moves an agent toward/across the link independently of its usual walking
avoidance and acceleration behavior. The measured 8.235 m/s across the link is
consistent with compressed interpolation. Upstream DetourCrowd's implementation
uses half the ordinary travel duration, reserves the first 15% for pulling the
agent to the link start, and skips normal walking steering/collision integration
for off-mesh agents. Its remaining-phase factor predicts 3.5/(.5*.85)=8.235294 m/s.
This is a matching mechanism, **not proof of Unity's private native source**.
Sources: [DetourCrowd](https://github.com/recastnavigation/recastnavigation/blob/main/DetourCrowd/Source/DetourCrowd.cpp),
[Unity navigation internals](https://docs.unity3d.com/cn/2018.3/Manual/nav-InnerWorkings.html).
The diagnosis relies on measured Unity displacement and the controlled experiment.

## Discriminating experiment and candidates

A temporary bake removed only the separate interior surface, footprint exclusion
and link. The existing lot surface collected the same physical architecture.
Both inward and outward paths became complete without a link. No physical
geometry, agent dimensions, navigation build radius or .05 m voxel size changed.
The continuous result has 6,863 vertices. The temporary scene was discarded.

Candidate A saved this continuous navigation and replaced the old link's
fail-closed switch with a threshold carving obstacle. It eliminated the speed
spike (3.50035334 m/s horizontal), but failed at .689963937 m separation outside
the opening. Both agents were now on normal NavMesh; wall contact was zero.
This falsifies the hypothesis that removing the link alone fixes every spacing
case. `continuous-candidate-*` retains this failure.

Candidate B adds local right of way at the one-person passage, preserving agents'
original destinations and normal avoidance. It passed its first focused check
(.697458446 m, zero wall/door penetration, 3.50038433 m/s horizontal).
The complete 20-run set returned 15 passed / 5 failed (runs 2, 6, 9, 17, 20), with
zero wall/door penetration. All results and the rejected controller are in
`candidate-B-stress`. Run 2 exposed .687686861 m:
Jack rounded the opened-leaf contour toward Stanley, whom the local rule had
stopped. Choosing the next occupant only by forward position can select a
lateral approach that needs the space occupied by another waiter. This candidate
is rejected; every failure remains part of the evidence.

## Preserved scope

Before-state hashes and copies are in `Logs/Stage1ThresholdStabilization/before`.
The saved Studio scene is unchanged. The former `Stage1_Interior.asset` is retained
with its GUID but no longer loaded by the prefab; the existing lot navigation
asset keeps its GUID. Door meshes, materials, hardware, anchors, collider shapes,
approved fingerprint, body/capsule sizes and test thresholds remain unchanged.

## Candidate C: preserve steering, assign local precedence

The stopped-waiter candidate is rejected (15/20 pass). Its failures include two
stopped agents coasting/pushing together, so merely changing admission order is
insufficient. Candidate C removes explicit stopping entirely. Earlier arrivals
receive higher local avoidance precedence and retain it until clear of the
passage. Every agent's original priority is restored afterward or on component
disable. No destination, velocity, speed, acceleration, capsule, pose or movement
owner is replaced. Approaches are scoped to this authored doorway and account for
normal braking distance; there is no global entrance lock or staged queue position.

Its focused candidate check passed: minimum .7009428 m, no wall/door contact,
maximum horizontal 3.500372 m/s, maximum spatial 3.68648982 m/s while stepping up
the apron. The closest pair was near the interior stations, off-link.
The numerical opposing-traffic probe passed: two exited while three entered,
all reached destinations, minimum separation .697487 m, zero wall/door contact,
maximum horizontal 3.54238582 m/s and spatial 3.68546867 m/s. Closing the held-open
door made the path incomplete; reopening restored it. All five original avoidance
priorities were restored to 50 on arrival. Evidence: `two-way-probe.json`.
The final candidate completed **20/20 consecutive passes**, zero failed/skipped,
with no retries or replacement runs. The same candidate and thresholds were held
fixed throughout. Exact per-run metrics and closest pairs are in
`Logs/Stage1ThresholdStabilization/stress-summary.json`; the aggregate is
`stress-totals.json`. Follow-up runtime/pose results are recorded below.

| Final-candidate measurement | Result |
| --- | --- |
| Twenty-run pass/fail/skip | 20 / 0 / 0 |
| Worst wall/door penetration | 0 m |
| Minimum employee separation | .699997067 m (rounded aggregate .6999971) |
| Maximum horizontal displacement speed | 3.50038958 m/s |
| Maximum spatial displacement speed, including apron climb | 3.68877077 m/s |
| Maximum reported NavMesh velocity magnitude | 3.68876743 m/s |
| Closest pair | Stanley Hawk / Leo Morris, run 11 |
| Pair positions | (5.703730,1.025000,-13.788740) / (6.241482,1.025000,-14.236870) |
| Pair location/link state | Exterior approach; neither on a link |

Across the separate opposing-traffic probe, minimum spacing was .697487 m and
maximum horizontal speed 3.54238582 m/s; these values remain visible separately
from the twenty-run set. The short spatial-speed increase at the apron includes
vertical step movement. Baseline horizontal/spatial peaks were
22.93998 / 23.0206146 m/s. No comparable interpolation spike recurred.

## Implementation and regression protection

- `HeldOpenStageAccess.cs`: the existing pose guard controls a carving blocker,
  preserving closed/disabled-door path blocking on continuous navigation.
- `PersonnelEntranceYield.cs` and its `.meta`: local arrival-based avoidance
  priorities, restored on departure/cancellation/disable. No employee identities
  or global entrance serialization are involved. Unity's
  [priority API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent-avoidancePriority.html)
  assigns precedence using smaller values; the rule is scoped to intended paths
  through this doorway.
- `Stage1LiveIntegrationBuilder.cs`: authoritative generation now uses the lot's
  continuous bake, threshold blocker and configured approach anchors. It removes
  the obsolete separate surface, whole-footprint exclusion and link.
- `Stage1_Live.prefab`: only the corresponding navigation components changed.
  All 294 serialized mesh/renderer/physical-collider components compare unchanged.
- `Studio_Revision04.asset`: rebaked continuous navigation, same GUID and name.
- `Stage1LiveRuntimeTests.cs`: the existing traversal retains all original checks
  and gains independent displacement extrema, closest-pair evidence and a
  3.55 m/s horizontal-speed assertion (3.5 m/s setting plus .05 sampling allowance).
  Spatial velocity is also retained, including the apron climb; it is not hidden
  or normalized away. The existing scene diagnostic accepts the absence of a link.
- `Stage1LiveIntegrationTests.cs`: the existing fail-closed test now verifies
  carving activation on invalid pose/disable and deactivation on reopening. The
  approved geometry fingerprint and geometry test body remain unchanged.

No new automated test was added. Temporary numerical probes are outside Assets.
The original seven failures, the continuous-only failure, and the rejected
stopped-waiter batch remain retained separately from the final candidate.

## Final affected-test and Editor results

After the final twenty-run set, both directly affected runtime tests were run
once, with the same production implementation:

| Existing test | Result | Evidence in `Logs/Stage1ThresholdStabilization` |
| --- | --- | --- |
| `EmployeesTraverseDoorAndReachInteriorWithoutPenetration` | Passed; .7025664 m minimum separation, 0 m penetration | `final-entrance-result.json`, `final-entrance-runtime.txt` |
| `TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts` | Passed | `final-production-result.json`, `final-production-runtime.txt` |
| `DoorAndThresholdFailClosedWhenPoseIsInvalid` | Passed after correcting its preview-scene invocation | `final-pose-guard-corrected-result.json` |

The production test exercised both scenes, autonomous station arrival, screenplay
beats, Live Camera interaction, retake/keep, production completion, release and
four weekly payouts without duplicates. The separate numerical probe also
verified autonomous exit, opposing traffic, and actual path blocking/reopening.

The first pose-guard check called `SendMessage("LateUpdate")` on an ExecuteAlways
component in a preview scene, triggering Unity's `ShouldRunBehaviour()` assertion.
The fixture now uses the existing `Configure` API after changing the hinge pose;
that API invokes the same refresh logic. Its corrected focused rerun passed.
The initial failure is retained in `final-pose-guard-result.json`. No production
behavior changed after the final twenty-run batch, and no traversal run was retried
or replaced to hide a failure.

Final Editor ground truth: Unity **6000.6.3f1**, compilation complete and
`compilationFailed=false`; current Console **0 errors / 0 warnings**. The bridge's
cumulative historical counters are distinct from this current Console snapshot.
The Editor is out of Play Mode with clean `Assets/Scenes/Studio.unity`, one
NavMesh surface and zero NavMesh links. Evidence: `console-final.json` and
`editor-final.json`. Executable: `C:\Repos\Unity\6000.6.3f1\Editor\Unity.exe`.

## Conclusion and limits

The entrance is technically stable within the requested scope: all twenty
consecutive final-candidate traversals satisfy the unchanged .69 m separation
and .005 m penetration limits, normal exterior navigation reaches all production
stations, opposing traffic completes, and production still works. No intermittent
spacing failure was observed with the final implementation. This bounded run set
does not prove that every possible future crowd configuration is collision-free.

The unnecessary automatic threshold link caused real excessive movement; ordinary
equal-priority convergence also needed local precedence. Continuous walking plus
arrival-order avoidance priorities addresses both without changing physical
geometry, destinations, speed/acceleration settings or employee dimensions. No
navigation link remains because the existing climb settings handle the 8 cm apron
on the connected NavMesh. The pose guard retains a closed-door path barrier.

The approved 55 mm door, all preservation checks and fingerprint
`fb42d59e75d0ed7f0234d4fed7ac57df286aac9f0a639a3828bfb559a2a133ca`
remain unchanged. There is no evidence requiring a broader crowd-navigation
redesign in this milestone. Next is the user's subjective review of movement;
no new visual inspection or visual approval is claimed here. The complete
project-wide suite was not run, and no unrelated feature work was performed.
