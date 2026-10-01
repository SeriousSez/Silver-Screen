# Movie Maker domain and authoring foundation

## Ownership decision recorded before implementation

Movie Maker edits creative intent in the existing `ScreenplayProject`; it is not
another movie/footage system. `ScreenplayCharacter` remains the fictional person,
`ScreenplayScene` the ordered creative scene, and `ScreenplayBeat` the narrative
projection consumed by existing production. A small authoring service edits these
owned collections, rather than maintaining another mutable screenplay copy.

An optional immutable per-scene authoring plan selects a shared immutable
`SceneTemplate`, duration, character-role bindings and scene-specific choices.
Templates own reusable actions, variants, requirements and semantic sound cues;
they never own a movie's dialogue, people, physical asset choices or sound edits.
Variant changes resolve authored sequences, not playback speed. References to
beats include occurrence numbers so repeated template beats remain distinguishable.
Edits for beats inactive in a variant remain stored but are not previewed.

Project-level visualization actor assignments and director intent use stable IDs,
not Employee/GameObject references. Unresolved/unavailable references are allowed
as intent; known incompatibility and duplicate assignments are rejected. Background
groups remain population instructions, not invented screenplay characters.

`ScreenplayProductionAdapter` remains the sole greenlight adapter. It creates fresh
roles/scenes/beats and an immutable resolved authoring snapshot with source IDs and
production-role mappings. It does not retain a mutable screenplay or authoring
service. Intended actor/director IDs do not perform production casting. Preview
resolves a plan with no production, budget, work, take or release dependency.

Player-authored drafts use an explicit completion/submission operation; they do
not simulate writer minutes or fabricate a writing evaluation. Existing commissioned
screenplay writing/evaluation gates remain intact. Editing a submitted player
screenplay invalidates submission until explicitly submitted again.

## Baseline and scope

Unity 6000.6.3f1 / URP 17.6.0. No commits/pushes. The last complete suite was 207/207,
but the subsequent entrance milestone captured an unresolved 0.573354244 m spacing
failure on automatic off-mesh traversal. That work is preserved, outside this
milestone. No clean-baseline assumption is made. Validation results are below.

No final Movie Maker UI, scene/prefab changes, actor rendering, animation, audio
playback/generation, content libraries, crowd AI or physical execution of the new
options is part of this foundation. The developer window is an isolated in-memory
proof and explicitly does not save drafts across domain reload.

## Contracts and editing

`ScreenplayProject.CreatePlayerAuthored` creates a draft without a commissioned
writer. Its `ScreenplayAuthoring` service owns editing operations for title,
characters, ordered scenes, intended actors and director. Scene IDs survive
replacement/reordering; scene numbers are recalculated. Submission requires
characters and valid scenes. Any successful edit invalidates submission. The
commissioned writing, content-generation and evaluation path remains separate.

`ScreenplaySceneAuthoring` is an immutable selection of a shared template plus
movie-specific values. Replacing a plan regenerates the existing scene's narrative
beat projection. Public scene mutation cannot bypass an authored plan. Collections
are defensively copied and exposed through read-only wrappers. Beat references
combine the reusable beat ID and its one-based occurrence in a variant. Inactive
dialogue and sound edits survive duration changes; preview and greenlight include
only the selected sequence.

Template role IDs map to fictional character IDs. Optional `IntendedPerformer`
values map those character IDs to person IDs. Assignment can use an application
resolver supplying actor status, availability and capabilities; the domain retains
none of that resolver's objects. Known incompatible/duplicate actors are rejected.
Unresolved or subsequently unavailable/incompatible actors produce preview
warnings without erasing creative intent. This is not a staffing reservation.
The director is likewise an optional intended person ID, not `AssignedDirector`.

Supporting characters remain meaningful screenplay roles; the appended `Bit`
character category adapts to existing production `Minor` prominence. Templates
can have arbitrary role counts. Background groups are separate population
instructions with stable group/semantic IDs, minimum/maximum count, automatic
population permission, optional selected people, category, wardrobe and behavior
intent. Duplicate people within a scene and overlap with intended principal
performers are rejected. No people are spawned or hired.

Sets preserve a semantic requirement independently of automatic, owned-set,
saved-set or future location selection. Content references are opaque IDs: losing
a physical asset does not mutate the screenplay. Compatibility/availability of
real sets, props and wardrobe requires a future content resolver. Template
requirements and anchors are preserved in the production contract. Prop defaults
derive from template requirements; scene overrides must preserve required
semantics and cannot disable required props. Optional props may be disabled,
replaced or removed; removing an override restores its automatic default. Custom
props are supported. Costumes associate a fictional character with automatic
wardrobe, an optional semantic requirement and an optional wardrobe content ID.

## Dialogue and semantic sound

Dialogue has a stable line ID, beat occurrence, fictional speaker, text, optional
emotion and delivery direction. The speaker must match the performer of that
beat. The existing narrative projection receives active dialogue and emotion;
the richer immutable intent also survives adaptation.

`PerformanceBeat.SoundCues` contains reusable semantic cues, never audio clips or
paths. `SceneSoundPlanner` combines explicit cues with inferred Footsteps for
Approach/Move/Exit actions (without duplicating an explicit Footsteps cue).
Heated Argument declares Door.Open at entry and Door.Close at exit. Other
templates can declare Vehicle.Engine, Glass.Contact or other semantic events
without adding file dependencies or extending a central audio switch.

Each planned event has a stable ID, source beat occurrence, timing event,
automatic/custom and optional flags, and immutable `SoundSettings`. Automatic
edits replace by event ID; custom events use their own IDs. Optional cues can be
disabled; required cues cannot. Removing an automatic edit restores defaults;
removing a custom edit removes that event. Individual settings include enabled,
optional content ID, volume **0–1**, timing offset **−600–600 seconds**, fades
**0–60 seconds**, and source-character/scene/nonspatial intent. Nonfinite values
are rejected. Defaults are enabled, volume 1, zero offset/fades, source-character
positioning and no chosen content ID. These are planning values, not audio playback.

## Preview and production boundary

`AuthoredScenePreview` resolves choreography using fictional character IDs and
separately exposes visualization assignments, dialogue, sounds and reference
warnings. `SceneSequenceCursor` can inspect the sequence without any production
dependency. There is no API here for Keep Take, Shoot Again, quality, mistakes,
release, budget spending or strategic work.

`ScreenplayProductionAdapter` accepts explicitly submitted player drafts while
preserving the evaluated-content gate for commissioned work. It creates fresh
production roles/scenes/beats/shots and immutable `ProductionAuthoringIntent` /
`ProductionSceneAuthoring` snapshots. Source screenplay, scene and character IDs
remain available. `ProductionRoleBinding` maps source characters/template roles
to the new production role IDs; beat performer/target references use those new
IDs. Dialogue, costumes and actor intent retain source character IDs, resolved
through those bindings. Collections are newly owned; shared leaf objects are
immutable. No live screenplay, authoring service or mutable template reference is
retained. Actual casting/director assignment, kept takes and earned production
completion remain governed by the existing production systems.

## Developer proof

Open **SilverScreen → Movie Maker → Authoring Foundation Proof**. The disposable
draft is *Midnight Over Manhattan*, Heated Argument / Medium, Hale and Hart mapped
to Aggressor and Defender, placeholder actor/director IDs, two dialogue lines,
Office intent, costume intent and an Office Worker background group. Select
Door.Close to edit enabled, content ID, volume, offset and fades independently.
Change duration, edit dialogue, clear/replace people, step preview beats, then
submit/adapt an isolated production copy. Its displayed state must remain Draft,
0% and zero takes. This tool does not add a movie to the strategic slate.

The window edits disposable domain objects, not scene/assets; it has no asset Undo
or draft persistence. Reset or domain reload discards the demonstration draft.

## Deliberately deferred

Final Movie Maker UI; save/load/versioned draft serialization; actor view resolution;
final characters/3D-Disco/Star Maker; animation, IK, faces and lip sync; audio or
voice generation/playback; My Sounds/My Sets/My Wardrobe; set/content availability
resolvers; crowd spawning/AI; camera or custom-scene editors; physical execution
of the new intent, staffing reservation, post-production and all release systems.
Before persistent authoring ships, define template-version migration and stable
content catalog resolution. Existing `.v1` template IDs are the current contract.

## File map

Added:

- `Assets/Scripts/Domain/Writing/ScreenplayAuthoring.cs`: editing service and preview.
- `Assets/Scripts/Domain/Writing/ScreenplaySceneAuthoring.cs`: immutable scene choices and narrative projection.
- `Assets/Scripts/Domain/Writing/AuthoringIntent.cs`: role, dialogue, population and content-reference values.
- `Assets/Scripts/Domain/Writing/SceneSoundPlanner.cs`: defaults, overrides and validated settings.
- `Assets/Scripts/Domain/Performance/SemanticSoundCue.cs`: reusable semantic cue definition.
- `Assets/Scripts/Domain/Movie/ProductionAuthoringIntent.cs`: immutable production snapshots and role mappings.
- `Assets/Scripts/Domain/Movie/MovieMakerAuthoringExample.cs`: isolated Heated Argument draft factory.
- `Assets/Editor/MovieMakerAuthoringWindow.cs`: disposable developer proof.
- `Assets/Tests/Editor/MovieMakerAuthoringTests.cs`: authoring and boundary regressions.

Extended existing `ScreenplayProject`, `ScreenplayScene`, `ScreenplayCharacter`,
`SceneTemplate`/`PerformanceBeat`, `HeatedArgumentTemplate`, `MovieProject`,
`MovieScene` and `ScreenplayProductionAdapter`. New C# assets have Unity-generated
metadata; existing GUIDs are preserved. Existing writing UI and strategic slate
creation flow are not exposed as a player-authoring UI in this milestone.

## Automated validation — 2026-09-27

Editor: `C:\Repos\Unity\6000.6.3f1\Editor\Unity.exe` (6000.6.3f1), URP 17.6.0.
Runtime and Editor assemblies compile. Unity initially omitted the newly imported
`SceneSoundPlanner.cs` from its compilation inputs; refresh/reimport/clean-cache
compilation did not resolve it. A clean Editor restart restored the input and
both assemblies compiled normally. No source workaround, package change or
compiler-warning suppression was used.

The first focused run passed 47/47. Four further boundary tests were added before
the final full run. The complete suite used the recorded graphics-enabled batch
Editor methodology: `-batchmode -runTests -testPlatform EditMode`, with no filter,
no `-nographics`, and no `-quit`. Runtime fixtures enter/exit Play Mode themselves.

| Scope | Passed / total | Result |
|---|---:|---|
| Complete suite | 257 / 258 | One existing Stage 1 geometry failure |
| Existing tests matched by full name | 206 / 207 | None missing or skipped |
| New authoring tests | 51 / 51 | Passed |
| Production lifecycle | 16 / 16 | Passed |
| SceneTemplate | 31 / 31 | Passed |
| Production panel | 2 / 2 | Passed |
| Stage 1 runtime | 2 / 2 | Passed |

New coverage includes identity/order, variant resolution and repeated occurrences,
two/three-role mappings and invalid references, actor replacement/unavailability/
capabilities, director removal, supporting/bit/background distinctions, set and
wardrobe intent, required/optional props, cue defaults/replacement/disable/custom
events, independent volume and finite numeric bounds, dialogue ownership,
submission gates, defensive collections, provenance and preview/copy isolation.

Evidence: `Logs/MovieMakerAuthoring/full-tests.xml`, `full-tests.log`,
`comparison.json`, `focused-tests.xml`, and `geometry-baseline-evidence.json`.
These are local validation artifacts. No existing tests were rewritten, weakened,
removed or skipped for this milestone.

### Existing geometry failure and limits

`Stage1LiveIntegrationTests.ClosedDoorDerivativeRetainsImportedGeometryUvsAndMaterials`
expects the closed live derivative to have identical geometry to the imported
review model. Both fingerprints contain 51,912 entries, but the first mismatch
has z = −24.0330 versus −24.0605 (27.5 mm). That matches half the previous
110→55 mm door-core reduction in `ArtSource/Stage1LiveIntegration/build_live_parts.py`.
The fixture and all 370 checked Stage 1 assets exactly match this milestone's
starting SHA-256 snapshot. No Movie Maker code participates in its mesh/material
comparison. This is an inherited mismatch from the interrupted door-thickness
work, newly exposed by the full run, not an authoring regression. It remains
unfixed; the all-existing-tests-green success criterion is **not met**.

Door traversal passed this run with zero penetration and minimum spacing
0.6999402 m, and the production/runtime integration fixture passed. This does not
invalidate the earlier recorded 0.573354244 m spacing failure. Both entrance
issues require a separate stabilization pass using the approved door design.

The Editor log contains Unity licensing entitlement/access-token messages; they
did not prevent compilation or tests. No C# warnings/errors were reported in the
final full run. A standalone player build and persistent-draft round trip are not
claimed by this domain milestone.

## Editor proof validation and recommendation

The window opened in the live 6000.6.3f1 Editor. Through the Unity Editor API,
reset the example, selected Door.Close, set enabled/content `proof-door-close`,
volume 0.35, offset −0.2 s and fades 0.1/0.4 s, resolved 3/5/8-beat variants,
advanced the preview cursor and adapted a production copy. The window-owned
screenplay retained both requested dialogue lines, two intended actor IDs and
`placeholder-director`. Production remained Draft, progress 0, budget 0 and zero
takes; the active scene remained clean and Play Mode was off. Evidence is
`Logs/MovieMakerAuthoring/editor-proof-state.json`.

This is **Editor API/state validation**, not a completed manual visual/input
smoke test. Computer Use returned the occluding application instead of Unity's
rendered window; no interaction with that other application was attempted. The
proof window remains open with Door.Close selected for direct inspection. No
claim is made about inspected layout, mouse interaction or final actor rendering.

The domain foundation is implemented and its 51 tests pass. The overall milestone
cannot be called fully green: one inherited geometry test fails, the earlier
intermittent doorway spacing defect remains open, and the manual window check is
incomplete. Next milestone: finish Stage 1 entrance stabilization, reconcile the
closed-door preservation invariant with the approved thinner-door source, perform
the manual proof-window check, and restore a reliably green full suite. Then
define persistent authoring/template-version contracts before a player-facing
Movie Maker interface or character integration. No commit or push was performed.
