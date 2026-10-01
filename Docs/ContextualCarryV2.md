# Contextual Carry Interaction V2

## Ownership and input

`PersonInteractionController` retains quick selection, threshold pickup, persistent
carry after release, a subsequent left-click drop, invalid-drop retention and
Escape cancellation. It also remains the sole owner of the held-person cutaway
reason. Targets never acquire reveal reasons themselves.

Carry claims contextual **left** clicks, not camera input. Information-card right
clicks claim their own right gesture only when not carrying. Right rotation,
middle pan, keyboard pan/rotation and zoom keep their existing bindings and
unscaled timing. A left click while a right/middle camera gesture is held does
not drop the person. Targeting continues during camera movement. The cursor is
never warped.

## Authoring a target

Add `ContextualDropTarget` beside an action MonoBehaviour implementing
`IPersonDropAction`. Assign:

- **Action:** stable target ID, current domain validity, synchronous execution.
- **Placement:** the actual feet position and facing, independent of zone size.
- **Interior:** optional existing `BuildingCutawayController`; this gates use on
  reveal and lets pointer tests ignore only the hidden cutaway occluders.
- **Shape / Size / Region Offset:** rectangular zone or elliptical round marker,
  in local XZ. Keep the plane on the actual floor and clear of furniture.
- **Symbol / Tint:** initial reusable floor visual identity; no floating label.
- **Magnet / Release Pixels:** default 22/32 pixel acquisition/release margins.
- **Maximum World Reach:** 0.65 m outside the region prevents distant snapping.
- **Reveal Distance:** local relevance limit, currently 12 m.

Selected-object gizmos show the region and final placement/facing. They are not
gameplay visuals. IDs must be unique within the active facility/session.

`PersonInteractionSpot` adapts the existing hiring and comedy-practice services to
the action interface. Old/runtime-created spots receive a compatible floor target;
explicitly authored target components take precedence. New activity kinds can
implement the interface without editing the carry controller or extending the
legacy action enum.

Actions must return false without side effects when unavailable. The placement
adapter validates lot bounds, NavMesh and capsule clearance, tentatively places
the person, then executes synchronously. A false result restores the held pose
and navigation position. Success releases at the authored pose; it never issues
a route-to-interaction command. Hiring callbacks therefore reconstruct the same
person at the actual placement.

## Targeting and visuals

Only currently valid, relevant and revealed targets are presented. Domain validity
is queried every carry input update and again at activation. Hiring uses existing
`CanHire` rules, never `JobSought`. Practice checks eligibility and the existing
person/interaction resource claims through `CanPracticeAt`.

Selection measures the cursor against the projected floor outline. Direct hits
outrank magnetic margins; stable IDs break equal scores. The previous candidate
gets a bounded release margin, not permanent stickiness. Solid occluders block
pointing; hidden cutaway walls do not. The held-person preview snaps to the chosen
placement, while the mouse remains untouched.

Visuals are disposable runtime meshes: a subtle transparent floor wash, border
and optional symbol, depth-tested against the world. Available and active states
have different opacity/contrast. There are no colliders, permanent world labels,
shadow casters or simulation-time dependencies. Render resources are reused
during carry and released with the target. Material/glyph refinement can replace
this presentation without changing action or input semantics.

## Studio Services migration

The existing Construction Worker and Groundskeeper anchors now have independent
2.0 x 1.65 m floor regions with hammer and leaf symbols. Their original placement
transforms, facility IDs and cutaway owner remain unchanged. Both the live prefab
and its authoritative production builder contain this configuration. No building
geometry, collision, navigation, LOD or saved scene changes are required.

The existing Practice Comedy interaction is the representative round activity
marker; this milestone adds no new profession or activity gameplay.

## Targeted verification

Use `ContextualCarryTargetTests`, `ContextualCarryStudioTests`,
`PersonManipulationRuntimeTests`, `PersonInteractionTests`,
`HeldPersonPresentationTests` and `SimulationSpeedTests`. The Studio proof starts
New Studio in memory, physically receives applicants, exercises both hiring
zones including cross-profession hiring, checks identity/placement and restores
the saved scene. Floor/cutaway captures are written to the OS temporary
`SilverScreenCarryV2` folder, not to building-art/LOD review outputs.

Visual approval still requires reviewing floor contrast, symbol readability and
magnetic feel at the player's preferred camera distances. No Casting Office,
character creator, Phase 2 travel or broader camera milestone is included.

## Validation result — Unity 6000.6.3f1, 2026-09-29

31 distinct focused tests passed across the initial run and affected-test rerun:

| Fixture | Passed |
| --- | ---: |
| ContextualCarryTargetTests | 6 |
| ContextualCarryStudioTests | 1 |
| PersonManipulationRuntimeTests | 2 |
| PersonInteractionTests | 8 |
| HeldPersonPresentationTests | 2 |
| SimulationSpeedTests | 5 |
| StrategicWorkTests | 7 |

The initial run had two test-harness failures, corrected before the affected
23-test rerun passed. An EditMode check incorrectly expected runtime disable
callbacks; disable/hide is now tested in Play Mode. Cursor immutability is checked
immediately across input handling, independently of Editor input-device resets.

The real Studio proof passed physical applicant arrival, both authored hiring
zones, cross-profession hiring, identity/placement preservation, Escape and
cutaway restoration. The isolated runtime proof passed quick selection, hold,
release-to-keep-carrying, separate click, invalid drop, failed-action retention,
round-marker practice and camera right-rotation/middle-pan/keyboard-pan/zoom while
carrying at all four simulation speeds. The existing speed-policy suite passed.

The actual Unity floor-zone capture was inspected: both zones lie on clear office
floor and the active zone is visibly stronger. No building art/LOD review set or
full project suite was regenerated. Final editor state: Play Mode off, Studio
scene clean, timeScale 1. Glyph polish and magnetic feel remain player visual/UX
review items, not claims of final art approval.

## Exact change manifest

Modified:

- `Assets/Scripts/Domain/Interaction/PersonPracticeService.cs`
- `Assets/Scripts/Domain/Resources/ResourceReservationBook.cs`
- `Assets/Scripts/Presentation/Camera/StudioCameraController.cs`
- `Assets/Scripts/Presentation/Interaction/PersonDragSession.cs`
- `Assets/Scripts/Presentation/Interaction/PersonInteractionController.cs`
- `Assets/Scripts/Presentation/Interaction/PersonInteractionSpot.cs`
- `Assets/SilverScreen/Environment/StudioServices/Editor/StudioServicesProductionBuilder.cs`
- `Assets/SilverScreen/Environment/StudioServices/Resources/StudioServices_Live.prefab`
- `Assets/Tests/Editor/PersonInteractionTests.cs`
- `Assets/Tests/Editor/PersonManipulationRuntimeTests.cs`

Added (each C# file also has its Unity-generated `.meta`):

- `Assets/Scripts/Presentation/Interaction/PersonDropContext.cs`
- `Assets/Scripts/Presentation/Interaction/ContextualDropTarget.cs`
- `Assets/Scripts/Presentation/Interaction/FloorTargetVisual.cs`
- `Assets/Tests/Editor/ContextualCarryTargetTests.cs`
- `Assets/Tests/Editor/ContextualCarryStudioTests.cs`
- `Docs/ContextualCarryV2.md`

The prefab delta is two target components and their component references only.
Existing anchor transforms, art, materials, GUIDs, collision and navigation remain
unchanged. Narrow pre-edit copies of touched existing files and test evidence are
under the OS temporary `SilverScreenCarryV2` directory. No repository-wide
preservation audit, commit or push was performed.
