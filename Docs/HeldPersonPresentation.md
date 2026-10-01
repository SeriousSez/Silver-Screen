# Held Person Presentation

Small presentation follow-up to `PersonInteractionFoundation.md`, Unity 6000.6.3f1 (2026-09-28).

## Lifecycle and authority

`PersonDragSession.Begin` starts `HeldPersonPresentation` after the existing navigation/collider suspension. The session still owns every root position and rotation. Presentation modifies only a visual child. No physics bodies, domain state, destinations, proficiency or placement rules were added or changed.

States are `Normal`, `Held`, `GroundSettle`, and `InteractionEntry`. Repeated pickup first restores the previous presentation, then captures the new one. Disabling/destroying the component restores the source renderer and previously enabled animation drivers. Invalid placement attempts do not release or cancel: `Held` stays active while the player moves elsewhere and retries. Escape explicitly uses `Cancel`, restoring normal presentation at the original pickup location.

The current live people are capsule MeshRenderers. Their existing mesh/materials and property block are copied to a reusable child on pickup; the original renderer is temporarily hidden. The child tilts about an upper-body pivot, so its lower end hangs rather than maintaining a grounded stance. There is a short blend into an off-axis neutral suspension, plus restrained vertical stretch/response. This is deliberately a whole-body placeholder: the live capsules have no independently poseable knees, feet or arms. No bone names, rejected rig studies, IK or new character assets are used.

`EmployeeAgent` hides its existing prototype performance gesture while held, without canceling the beat or obligation. Its existing held guard already suspends its ordinary update. The placeholder adapter saves/restores Animator enabled flags if present. A future adapter owns arbitration with its own locomotion/animation driver.

## Motion response

Each LateUpdate samples the actual root displacement, not mouse deltas. Velocity is capped at 12 m/s and acceleration at 50 m/s² before exponential smoothing. Root-local horizontal motion drives opposite trailing tilt. Acceleration adds a small delayed reaction to direction changes. A damped second-order spring uses bounded integration substeps of at most 1/120 second. There are no allocations in the frame sampler.

Unscaled presentation seconds keep motion independent of strategic time speed. Long stalls contribute at most 0.1 seconds of spring integration, while velocity still uses the actual elapsed time to avoid a false impulse. This also bounds recovery work after losing focus. The test compares the same movement at 30 and 120 Hz, checks response direction/limits, and verifies settling after stopping.

## Release

Pickup requires the existing hold threshold. Releasing the original press leaves the person carried; only a later, separate left mouse-down attempts a drop. A quick click before the threshold never picks up. The presentation layer itself is unchanged by this input correction.

- **Ordinary ground:** the existing immediate Warp occurs first. A 0.28-second visual recovery eases out tilt with at most 4% compression / 4 cm visual lowering. Navigation and obligations resume through the existing path immediately; the recovery is not a gameplay stun and cannot change the chosen root position.
- **Contextual:** the controller explicitly requests `Contextual` for authored spot drops. The authored position/facing remains authoritative. A 0.12-second rotation blend goes directly into the activity, without ground compression or a separate landing. Hiring and Practice Comedy use the same path; the presentation component survives candidate-to-employee conversion on the existing GameObject.
- **Invalid attempt:** retain `Held`, sway and compatible contextual spots. No landing, Warp or return-to-pickup occurs. The player may retry with a later click.
- **Escape / explicit cancellation:** return to pickup position and restore normal rendering directly. The existing 0.15 m validation rule remains unchanged.

## Tuning

Values are serialized on `HeldPersonPresentation` (automatically attached on first pickup, or pre-attach it for authored settings):

| Parameter | Default | Purpose |
| --- | --- | --- |
| Maximum body tilt | 16° | Caps total suspension/sway tilt |
| Sway strength | 1.2 | Horizontal motion response gain |
| Damping | 0.85 | Restrains oscillation after direction changes |
| Settling speed | 8 s⁻¹ | Spring response/return rate |
| Vertical response | 0.018 | Up/down stretch, bounded to −2.5% / +4% |
| Motion smoothing | 12 s⁻¹ | Exponential velocity/acceleration smoothing |
| Ground settle | 0.28 s | Ordinary release recovery |
| Interaction entry | 0.12 s | Clean contextual transition |

The capsule has no limbs, so a limb-specific tuning control would have no effect and was not added.

## Future animation hook

Attach or assign a `HeldPersonPoseAdapter`. Its `BeginPose(reactionId)`, `ApplyPose(frame)`, and `EndPose()` replace the capsule fallback. A frame contains the presentation state, local rotation/offset, vertical response and weight; it has no employment, task or navigation API.

A future Humanoid implementation can play **Held.Neutral**, apply this drag sway additively to a dedicated visual pivot, perform body-aware adjustments using explicit Avatar references, and later supply a facial reaction. Begin/End should arbitrate ordinary locomotion and restore prior animation state. Root motion must remain disabled for the held pose. `reactionId` defaults to `Held.Neutral` and is the extension point for future mood/personality choices; no reaction variants are implemented here.

## Developer proof and validation

In the existing Studio Play session:

1. Pick up an idle capsule: after the short pickup blend, its upper body acts as the suspension point and its lower body hangs off-axis. Any current prototype filming gesture is hidden while held.
2. Drag slowly, then quickly horizontally; reverse direction, then stop. Observe restrained trailing tilt and damped settling rather than a direct noisy cursor rotation.
3. Drop on clear ground: immediate placement with a brief settle while autonomy resumes.
4. Drop a suitable employee onto Practice Comedy: direct interaction-entry blend, no ground compression. Pick up a waiting applicant and drop onto a profession spot: identity/employment behavior is unchanged.
5. Click outside allowed ground while carrying: remain carried, with held presentation and compatible nearby spots still active. Move elsewhere and retry. Escape returns to the original position and normal presentation.
6. Repeat with an employee who has a production obligation: the same obligation resumes from the chosen valid location.

Subjective visual/interaction approval remains with the user. No visual snapshot tests or Computer Use are used. The selected tests are only `HeldPersonPresentationTests`, `PersonInteractionTests`, and `PersonManipulationRuntimeTests`; the latter uses an isolated Play Mode scene and restores the saved Studio scene. No full suite, commit or push.

Validation result: **10 selected tests passed** across the focused runs (2 new held-presentation checks, 7 person-interaction domain checks, 1 extended isolated runtime check). The first run passed 9 and failed at native NavMeshAgent initialization before pickup. The fixture now allows a frame for native registration; only that failing test was rerun and passed. The implementation compiled without errors on its first compile; one corrective compile was needed for the fixture. Passing tests were not rerun.

The runtime proof verifies held-state activation, visual/root separation, immediate ground placement, contextual entry rather than generic landing, explicit cancellation return, authored facing, stable identity and retained obligation. Domain checks cover hiring and practice behavior. Final Unity state: compilation errors false, Console errors zero, Edit Mode, Studio restored cleanly. Subjective appearance and full mouse-driven visual acceptance were not claimed or automated.

## Carry input lifecycle correction — 2026-09-28

`Normal -> PendingPickup -> Carrying` uses the existing threshold. The original mouse-up only arms subsequent placement clicks. Carry, held sway and compatible nearby spot visibility persist without a pressed button. A fresh left mouse-down attempts ground/context placement; only success ends the session. Invalid attempts preserve it. Escape explicitly cancels to the original pickup transform. Drop presses are consumed by the person controller so they cannot also select buildings or begin another pickup.

Focused correction validation: **2/2 tests passed on the first run**, zero failures. Compiled once with no script errors; no unrelated tests or repeat runs. The new synthetic-input scenario drives the real controller through quick click, threshold pickup, initial release, button-free carry, invalid retry, ground click, contextual Practice Comedy click/activity, and Escape. The existing manipulation test covers placement/obligation and presentation invariants. No held-pose implementation was redesigned.

Exact focused tests: `PersonManipulationRuntimeTests.CarryRequiresSeparateClickAndInvalidAttemptsKeepPresentation` and `PersonManipulationRuntimeTests.GroundDropWarpsImmediatelyRejectsWallsAndRetainsWorkObligation`. The synthetic-input fixture restores input devices/settings and the saved Studio scene after its isolated Play Mode proof.
