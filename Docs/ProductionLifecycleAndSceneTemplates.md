# Production lifecycle and scene templates

## Audit before changes (2026-09-27)

The current tree already contains a four-scene coordinator regression. The original
reported play session/save is unavailable; do not assume its exact state can be
reconstructed. The following mechanisms are directly reproducible in this tree:

- `ScreenplayProject` completes writing, then finalizes independent characters,
  scenes and beats and receives an evaluation. `ScreenplayProductionAdapter`
  creates fresh production roles/scenes/beats/shots and preserves source IDs.
  Coordinator greenlight checks the slate for an existing source screenplay ID.
- `MovieProject.CurrentState` is independently writable through `SetState`, even
  to Completed or Released without any scenes. `ProductionProgress` has its own
  unrestricted setter. `ReadyForFilming` checks only director/casting, so remains
  true even after every scene is finished or the movie is released.
- `MovieScene` has Planned -> Ready -> Filming -> Completed transitions, but
  `CompleteFilming` accepts a scene with no completed/selected take. A selected
  take can subsequently be discarded, leaving a Completed scene without footage.
- The coordinator starts a 480-unit strategic work item per take, independently
  of local visual playback. Automatic mode selects the completed take; manual
  mode waits for Keep Take. Only `CompleteActiveScene` finalizes quality and sets
  project Completed. A failed/interrupted take can leave scene Filming orphaned.
- The coordinator does already advance to each next scene. It resolves that
  scene's required owned facility, moves performers through stations/marks and
  starts the next take. An unavailable environment blocks here.
- The UI returns 100% for ReadyToFilm. Thus a newly cast, completely unfilmed
  four-scene movie appears fully progressed while release is correctly denied.
  This is a confirmed misleading display; it is not evidence of filmed scenes.
- Release service checks project Completed and a quality result, while direct
  `TryRelease` checks only Completed. Neither verifies actual scene completion.
- Production has no save/load DTO, serializer or restoration API. Scheduler/work
  snapshots exist, but do not reconstruct a movie/coordinator session.

Six failing regressions confirmed the empty-readiness, completion-without-footage,
discarded-kept-take, stale-project-completion, stale-readiness, and premature-quality
mechanisms before implementation. The original four-scene coordinator regression
passes on the baseline. Its advancement logic was retained.

## Lifecycle after the fix

`ScreenplayProject` -> completed writing + finalized content + evaluation ->
greenlight -> adapted `MovieProject` in Draft -> Casting -> ReadyToFilm -> Filming
-> repeat for remaining scenes -> Completed + finalized quality -> Released.

`MovieProject.CurrentState` is a derived summary, in this priority order:

| Summary | Authoritative facts |
| --- | --- |
| Released | A release date/run was successfully recorded |
| Completed | At least one scene exists and every scene is Completed |
| Filming | A scene is Filming (including review of a completed, unkept take) |
| ReadyToFilm | Director and casting are ready, unfinished scenes exist, no scene is Filming |
| Casting | A role has an assigned actor but filming readiness is not met |
| Draft | No production cast assignments yet |

`PreProduction`, `Rehearsing`, and `PostProduction` enum values remain reserved;
the current workflow does not enter them. The unrestricted `SetState` and
`SetProgress` APIs were removed; all repository consumers were updated. Cached
last-notified values exist only to suppress duplicate events, never to determine
state. Read-only collection wrappers prevent callers from replacing scene/take
lists behind these transitions.

The coordinator's `ProductionPhase` describes orchestration: facility resolution,
travel, blocking, take work, manual review, failure, and retry. It does not own a
second definition of movie completion. ReadyToFilm means content/casting readiness;
an unavailable facility or failed route is explained by the phase/status, and the
UI offers **Retry Production**. No final assets are necessary.

### Invariants

- Every `MovieScene` in this milestone requires filming until Completed. There
  is no separate `filmed` flag or optional/excluded scene system. Zero scenes
  means neither ready nor complete nor releasable. Empty screenplay content
  cannot finalize/greenlight. Legacy direct movie creation authors its one
  prototype scene immediately, so casting readiness never invents content.
- A scene follows Planned -> Ready -> Filming -> Completed. Completion needs a
  selected Completed take and no Preparing/Recording takes. Duplicate completion
  attempts fail without changing footage. Completed scenes cannot create new
  takes or discard/replace their kept take.
- Manual take completion alone does not complete a scene. Keep Take completes
  it; Shoot Again returns it to Ready and starts another take. Existing multiple
  takes and performance snapshots are preserved.
- Cancellation discards incomplete footage, releases strategic work/reservations,
  and returns a Filming scene to Ready. Retry resolves its environment again.
  A routing revision prevents canceled callbacks from affecting a newer attempt.
- `ProductionProgress` is completed scenes / total scenes, or zero for no scenes.
  `ActiveTakeProgress` reads earned/required work from `WorkService`. The UI labels
  casting progress, completed scenes, and active take work distinctly. A 100%
  take awaiting a manual decision is still an unfinished scene.
- Quality can finalize once, only after all scenes complete. `CanRelease` requires
  that completion, quality, and no previous release. Both release entry points
  enforce these facts; direct release also verifies the run's movie ID/date.
  Release is idempotent. Completed/released movies cannot gain new scenes.
- The creative source remains independent. Adapter behavior, SourceScreenplayId,
  scene/character provenance, beat IDs, role remapping, and generated shot links
  are preserved. Greenlight still rejects an existing source ID even after its
  first production completes.

There is no production save/load implementation to test. This change does not
invent one. Future restoration must restore and validate scene/take/cast/result
facts and reconstruct coordinator work; it must not deserialize an independent
project-complete flag. Current tests construct state through domain transitions.

## Scene template boundary

| Model | Purpose |
| --- | --- |
| `ScreenplayScene` | Creative source: fictional characters, semantic location, narrative beats |
| `MovieScene` | Adapted production plan with remapped cast roles, shots and take history |
| `SceneTemplate` | Immutable reusable choreography vocabulary and authored variant sequences |
| `MovieTake` / future filmed result | Existing earned take, performance scores and kept selection; future recorded motion, dialogue/audio and camera capture belong here or in attached result data |

`Assets/Scripts/Domain/Performance` contains pure C# with no Unity, actor,
finance, release, or UI dependency. It is not a new package or an alternative
production service. `ScreenplayBeat` remains narrative intent; the more granular
`PerformanceBeat` describes reusable actions and role/anchor references without
fictional character ownership or mutable ordering.

- `SceneTemplate`: explicit stable ID, name/description/tags, role slots, immutable
  beat library, exactly one Short/Medium/Long sequence, props/environment
  requirements and semantic anchors. Constructor validation rejects malformed
  identities, null entries, duplicate definition IDs and dangling references.
- `PerformerRoleSlot`: named role and capability requirements. `PerformerBinding`
  maps that role to an opaque performer ID plus declared capabilities.
  `ScenePerformance` requires exactly one distinct performer per role and checks
  capabilities. The consumer resolves IDs to capsules, character views, or other
  representations; definitions never retain `Employee`, `PersonProfile`,
  GameObject or Animator references. This initial distinct-performer rule may
  later be extended deliberately for doubles or nonoverlapping roles.
- `SceneTemplateVariant` references shared immutable beats in authored order.
  Different variants can reuse the same beat objects without copying mutable
  content. A sequence may repeat a beat ID when authored repetition is intended.
- Required/optional `SceneRequirement` values and `InteractionAnchor` references
  retain semantic IDs. They describe needs; this milestone does not implement a
  physical prop resolver or IK. `CameraSuggestion` stores framing and optional
  subject/other role IDs, independent of a Unity camera.
- `SceneSequenceCursor` advances deterministically through a resolved performance.
  It has no clock or animation engine. The consumer decides when to advance.
  Running it never changes a movie or finishes strategic work.

## Heated Argument proof

The SilverScreen-owned `HeatedArgumentTemplate` authors Aggressor and Defender:

| Duration | Ordered beats |
| --- | --- |
| Short | Confront, Respond, Exit |
| Medium | Approach, Confront, Respond, Escalate, Exit |
| Long | Approach, Confront, Respond, React, SecondExchange, Escalate, FinalReaction, Exit |

Open **SilverScreen > Scene Templates > Heated Argument Preview** in Unity.
Choose a duration and assign Placeholder A/B to the two roles. Play, Pause,
Next beat and Reset drive the same tested domain cursor. Duplicate performer
assignment reports a validation message. Colored 2D silhouettes move for
Approach/Exit and display the current action; the list shows beat order,
performer, camera suggestion and anchor. This is intentionally a developer
visualization. It creates no scene objects/assets and requires no animation.
The Editor gives every beat 1.4 preview seconds; length changes the sequence,
never playback speed. Preview timing is not filmmaking duration or work cost.

## Changed implementation

- `Domain/Movie/MovieProject.cs`: derived state/progress, shared release eligibility,
  guarded quality/release, read-only collections and consistent notifications.
- `MovieScene.cs`: footage-backed completion, take guards and cancellation.
- `MovieProductionCoordinator.cs`, `IMovieProductionService.cs`: fact-driven
  transitions, separate take work progress, default-scene authoring, retry and
  stale-route protection. Existing work/performance/presentation services retained.
- `MovieReleaseService.cs`, `Presentation/UI/MovieProjectUI.cs`: consume the
  authoritative facts; distinguish progress and expose retry/release actions.
- `Domain/Performance/SceneTemplate.cs`, `ScenePerformance.cs`: generic definitions,
  validation, role resolution and sequence cursor.
- `Domain/Movie/HeatedArgumentTemplate.cs`, `Editor/SceneTemplatePreviewWindow.cs`:
  authored example and isolated placeholder preview.
- `ProductionLifecycleRegressionTests.cs`, `SceneTemplateTests.cs`: new regression
  and template coverage. `MovieProductionCoordinatorTests.cs` now covers one/four
  scenes in both modes, interruption/retry, duplicate greenlight and stale routes.
  Existing release/runtime fixture helpers in `Phase1StudioRuntimeTests`,
  `Stage1LiveRuntimeTests`, and `StrategicTheatricalCompatibilityTests` now create
  real completed scenes instead of forging a Completed enum; assertions retained.

## Deferred and concerns

No final characters, character-pack integration, body/wardrobe work, retargeting,
clips/controllers, IK, paired-animation solver, dialogue/voices, facial/lip sync,
music/audio generation, advanced cameras, Movie Maker UI, scene editor, sharing,
modding, post-production, multiplayer, streamer mode, or game-specific Detective/
Mafia code was added. No additional take/retake system was built; the existing
one remains in use. Definitions are authored in code for this single example;
ScriptableObject authoring and serialization can be added at the boundary later.

The existing coordinator is large and mixes routing orchestration with lifecycle
application logic. This change reduces state authority without broadly redesigning
it. The current stage still supports at most three performers and the work adapter
still uses 480 strategic units per take. None of these limits belongs in templates.

After final models arrive, validate an actor-view adapter on this same template:
resolve the two opaque performer IDs to real character views, map a small set of
semantic beats to tested retargeted clips across different heights, and exercise
the existing take/presentation boundary. Add anchor/interaction handling only as
required by that vertical slice, then connect authored template choices to the
production scene plan. Keep earned work independent of local animation playback.

## Validation evidence

Unity 6000.5.9f1 / Windows Editor / URP 17.5.0. The Editor was initially closed;
tests used the installed Unity executable with `-batchmode -runTests
-testPlatform EditMode`. Graphics remained enabled for the existing runtime tests.
Local licensing required an elevated tool process. No MCP tool was exposed in
this session; the existing official package was preserved.

Baseline: 147 tests, 142 passed, 5 failed. Six newly added regression cases then
failed against the unchanged implementation. Results/logs are under
`Logs/ProductionMilestone` (generated, ignored by Git). Full final results and
visual inspection are recorded in the completion report below.

### Completion report

- Final full run: **201 tests; 196 passed, 5 failed, 0 skipped**, 41.84 seconds
  of test execution. XML: `Logs/ProductionMilestone/final.xml`; compiler/runtime
  log: `Logs/ProductionMilestone/final.log`. Unity runtime and Editor assemblies
  compiled successfully. This is not a green project-wide suite.
- **16/16** lifecycle regressions and **31/31** template tests passed. Expanded
  coordinator coverage passed for automatic/manual one/four-scene production,
  retry, stale callbacks, work-progress notifications and duplicate greenlight.
  The test count increased by 54; no tests were removed, ignored or weakened.
- `Phase1StudioRuntimeTests.StudioProductionLiveViewRetakeAndRelease` passed in
  the real Studio: local slate/beat playback does not finish work, paused strategic
  time does not scale local animation, retake transfers claims, Keep Take frees
  cast/facility, release pays four weeks, repeated advance does not duplicate money.
- All five final failures already occurred on the 147-test baseline:
  `ScreenplayContentGenerator_RefusesCleanlyWhenStudioHasNoCapabilities`,
  `ScreenplayContentGenerator_UsesSingleAvailableFacilityCapability`,
  `Generator_UsesNewlyConstructedFacilityWithoutMutatingEarlierContent`,
  `EmployeesTraverseDoorAndReachInteriorWithoutPenetration`, and
  `TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts`.
  The first three expose disagreement between generation and capability-test
  expectations; they were not changed. Stage 1 tests respectively detect a door
  penetration (0.00934 m final, threshold 0.005 m) and a double-click that fails to
  enter Live Camera View. These remain separate follow-up work.
- The initial baseline XML was read and its counts/failures recorded before
  Unity cleared its temporary output directory on restart. Subsequent evidence
  uses `Logs/ProductionMilestone` to survive restarts. The six pre-fix regressions
  are preserved in `regressions-before.xml` (0 passed, 6 failed).
- No standalone player build or production save/load round-trip was performed.
  No final-character or cinematic-quality claim is made.
- Visually inspected the actual Editor preview with the Computer Use tool:
  two placeholders, role assignments, duration options, ordered beats, semantic
  metadata, movement during Approach and the completed Medium sequence after Exit.
  This is 2D Editor visualization; it does not assert final 3D/animation quality.
