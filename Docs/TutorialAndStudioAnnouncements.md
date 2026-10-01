> Construction update: the live New Studio opening now uses freeform sites, physical builders, and completion events. See [BuildingPlacementAndConstruction.md](BuildingPlacementAndConstruction.md). The instant-establish flow below describes the earlier prototype.

# Tutorial and Studio Announcements

## Existing-system audit (2026-09-28)

The working project is Unity **6000.6.3f1**, with first-party runtime and Editor
code in Unity's default assemblies. The old 6000.5 repository context predates
the project's migration. This milestone preserves the existing uncommitted work.

| Area | Existing authority reused |
| --- | --- |
| Studio start | `StudioBootstrap`, `StudioIdentity`, configured sandbox employees; no existing New Game screen or save system. |
| Starter buildings | Authored `StudioBuildingView` instances for HQ, Casting Office and Stage 1. These were already present and operational, with no starter construction service. |
| Construction | `SpecializedSetConstructionService` already constructs Street and Restaurant/Café sets. No general unlock/feature-flag system was found. |
| Facilities | `StudioFilmingCapabilities`, `SoundStageIdentity` and `StudioWorldRouter` connect semantic set requirements to real Stage 1 facilities and production stations. |
| Recruitment | `RecruitmentCoordinator` generates and hires candidates; `StudioEmployeeManager.OnEmployeeAdded` reports registered employees. Existing Stage School and Script Office support candidate routing. |
| Writing | `ScreenplayWritingCoordinator.ScreenplayAdded`, `ScreenplayProject.Changed`, completed writing and finalized content. Existing Screenplays panel remains the player route. |
| Production | `StudioProductionSlate.OnProjectAdded`, `MovieProject.OnProjectUpdated`, real cast/director/scene/take facts. `MovieProductionCoordinator` owns greenlight, casting, routing, filming and take decisions. |
| Release/finance | `MovieReleaseService`, `MovieProject.ReleaseDate`, theatrical runs and the finance ledger. Release begins a run; box-office receipts are not necessarily immediate. |
| HUD | Existing Studio, Staff, Screenplays, Productions and Finance panels. Semantic focus IDs are future presentation hints, not GameObject references. |
| Notifications | Production already emits transient English status strings. PA observes structured project facts instead of parsing those strings. No existing semantic PA queue was found. |
| Pause/time | `SimulationClock`, `ISimulationControl.AcquirePause` and `SimulationPauseState` own pause. Strategic time remains separate from local presentation time. |

## Start options and developer proof

Open **SilverScreen > Tutorial and Studio PA** with the Studio scene open.
Before Play, enable **New Studio** and choose **Tutorial ON** or OFF. These are
serialized `StudioBootstrap` settings, editable with Undo and persistable by
saving the scene. The milestone does not save the scene automatically.

New Studio starts without the configured sandbox employees in both cases. It
temporarily deactivates the three authored starter facilities in the Play Mode
instance. It does not edit their transforms, materials, GUIDs or prefab assets.
The existing support Script Office and Stage School remain prototype facilities;
this is an establishment foundation, not a completed empty-lot construction game.

The **New Studio** flag separates a fresh start from the existing populated
development sandbox; it is not a second simulation mode. Tutorial ON/OFF uses
the same establishment, recruitment, writing and production services. Existing
scenes default to retaining the sandbox, with tutorial disabled and PA enabled.

The window's **Establish** buttons call `StarterEstablishmentService.Establish`.
Successful establishment activates the actual authored facility and refreshes
the production router. Stage 1 also adds its actual supported set capabilities
back to the existing capability registry. There is no fake "objective complete"
button. Starter placement, costs and construction work are deliberately deferred:
those systems did not exist for these three buildings before this milestone.

Tutorial OFF offers all normally available starter establishment actions
immediately. It does not grant buildings, employees, cash or other progression.
Normal availability currently requires a matching authored facility and rejects
duplicate establishment. Future normal progression requirements belong in that
predicate, separately from the tutorial overlay.

## Tutorial model and flow

`InitialTutorialSequence` contains nine small immutable definitions:

1. Welcome: camera, lot, clock and studio identity; Continue advances.
2. Establish Headquarters: actual successful establishment advances.
3. Establish Casting Office: actual successful establishment advances.
4. Hire the first employee: real employee registration advances.
5. Establish Stage 1: actual establishment and facility registration advance.
6. Create a screenplay: completed writing plus finalized content advances.
7. Prepare the movie: greenlight the tracked screenplay, assign cast/director,
   complete casting, then reach Ready to Film (or subsequent production state).
8. Film the movie: finish actual scenes/takes and finalize quality.
9. Release the movie: release of the tracked movie completes the tutorial.

Each `TutorialStep` has a stable ID, `GuideMessage`, semantic completion condition,
entry/completion unlock IDs and optional next step. No giant switch or scene
object references are involved. `TutorialSession` owns progress and pause leases.
`TutorialGameEvents` subscribes to existing objects and re-evaluates their facts
only when events or guide transitions occur. Already-satisfied conditions are
reconciled, avoiding missed events at step boundaries. It never polls objectives
per frame or uses the transient notification strings as gameplay facts.

The first finalized observed screenplay is retained by ID; only its first
observed adapted movie can finish the guide. Releasing another production does
not complete the tutorial. Existing player-authored projects can be observed
with `ObserveScreenplay`; the live commissioned library is wired automatically.
The separate authoring-example Editor window remains an isolated authoring proof.

## Starter gates

The overlay combines `normallyAvailable && tutorial.Allows(featureId)`.
Before Welcome is continued, no starter establishment action is unlocked. HQ,
Casting, and Stage unlock as their respective steps are entered. Hiring is an
explicit objective between Casting and Stage. Specialized sets unlock upon
entering the screenplay step, after Stage 1 is established.

`SpecializedSetConstructionService` enforces the additional availability check
at its mutation boundary, so bypassing the button does not bypass the gate.
Its existing Studio panel buttons reflect the same rule and refresh on guide
changes. Recruitment in a fresh studio requires an established Casting Office
for both ON and OFF; this is a normal facility requirement, not a tutorial gate.
Recruitment's generation and hire boundaries enforce it. Existing candidate
generation, navigation and hiring behavior otherwise remains unchanged.

Skipping or completing makes every tutorial gate inert; normal facility and
progression requirements still apply. No other progression or Movie Maker unlock
rule is introduced. Navigation remains available, allowing the player to inspect
the existing systems without building a parallel UI.

## Guide messages, persistence and pause

`GuideMessage` includes title, body, optional guide/portrait identity, voice cue,
UI focus target, priority, pause policy and Continue behavior. Prototype Game
view text offers Continue and Skip; the developer window can reopen the guide.
Only Welcome's Continue completes an objective. Other Continue buttons dismiss
the message so strategic time can run while the player performs the real action.

The guide acquires a centralized pause lease while a pausing message is open.
Closing, skipping, completing, disabling its presentation, or destroying the
owner releases that lease. Player pause and other owners' leases are preserved.
No tutorial code sets `Time.timeScale` or chooses a replacement simulation speed.
Disabling presentation suspends its lease; re-enabling reacquires it if the
message remains open. PA never receives an `ISimulationControl` dependency.

`TutorialProgress` is a versioned serializable DTO: status (Disabled, Active,
Skipped, Completed), step ID, completed IDs, unlocked IDs, tracked screenplay and
movie IDs, and message-open state. `Capture` and restore copy mutable lists.
Definitions, Unity references, English prose and pause tokens are not saved as
progress. Full save/load integration is deferred; restoring a game must restore
its ordinary buildings/employees/projects and reconnect this observer as well.

Skipping only changes tutorial progress and releases its pause lease. It does
not reconstruct a studio, destroy employees, grant rewards or mutate production.
Completion stops forced guide sequencing and leaves the independent PA running.

Voice IDs include `Tutorial.Welcome`, `Tutorial.BuildHQ`, `Tutorial.BuildCasting`,
`Tutorial.BuildStage`, `Tutorial.CreateScreenplay`, `Tutorial.CastMovie`,
`Tutorial.StartFilming`, `Tutorial.ReleaseMovie` and `Tutorial.HireEmployee`.
They are semantic content references; text requires no audio assets or service.

## Studio PA

`StudioAnnouncementQueue` is separate from narration. An `AnnouncementRequest`
contains semantic type, Low/Normal/Important/Urgent priority, entity/project ID,
strategic timestamp, cooldown, optional text fallback and optional voice cue.
The fallback presentation catalog resolves types into text and `StudioPA.*`
voice IDs. Game state never depends on the wording, voice, year or medium.

Connected production conditions:

- Missing cast: `ProductionNeedsActor` (Important).
- Missing director: `ProductionNeedsDirector` (Important).
- Ready to Film: `ProductionReady` (Normal).
- Finalized completed production awaiting release: `ProductionCompleted` (Important).

Requests are edge-triggered per condition/project. Repeated project progress
events do not re-enqueue them. Resolution removes any pending request and clears
the cooldown record; a genuine recurrence can announce again. Queue deduplication
also rejects immediate direct requests by type/entity, with a default six-game-hour
cooldown. An already-pending request can be promoted to a higher priority.
Delivery selects highest priority first and preserves insertion order among ties.
There is no periodic repeat scheduler. A displayed subtitle finishes its short
presentation duration; resolved pending requests are discarded.

Prototype subtitles display for eight unscaled real seconds, independently of
strategic pause. Priority determines the next subtitle, without interrupting a
currently displayed one. This frame timer only expires presentation text; it
does not poll gameplay or generate events.

The window's **PA proof** button sends an actor request twice and a distinct
Urgent director request, records acceptance/duplicate results and verifies that
the strategic pause state is unchanged. These explicitly labelled developer
requests do not alter any production. Game view shows the queued fallback text.

## Changed code

- New domain files: `TutorialSequence.cs`, `StarterEstablishmentService.cs`,
  `TutorialGameEvents.cs`, `StudioAnnouncements.cs` under `Domain/Tutorial`.
- New presentation: `StudioGuidanceDriver.cs`; new Editor tool:
  `StudioGuidanceWindow.cs`.
- Integration: `StudioBootstrap.cs`, `SpecializedSetFacility.cs`,
  `RecruitmentCoordinator.cs`, `RecruitmentPresentation.cs`, `StudioOverviewUI.cs`.
- Focused tests: `TutorialAndAnnouncementTests.cs` (six tests).

## Validation

Unity 6000.6.3f1 compiled **Assembly-CSharp** and **Assembly-CSharp-Editor**
successfully at 08:53:12 UTC. The final compile status reports no errors and
`EditorUtility.scriptCompilationFailed` is false. One explicit final refresh was
requested after implementation. Earlier Unity relay requests caused automatic
imports/compilations despite the refresh hold, so this was **not literally one
compilation overall**. Subsequent ephemeral validation scripts are outside Assets
and do not rebuild the project assemblies.

| Selected EditMode fixture | Passed | Failed |
| --- | ---: | ---: |
| TutorialAndAnnouncementTests (new) | 6 | 0 |
| RecruitmentTests | 14 | 0 |
| SpecializedSetConstructionTests | 12 | 0 |
| ProductionLifecycleRegressionTests | 16 | 0 |
| Total | **48** | **0** |

Each executed test ran once. The initial recruitment filter used the wrong
namespace and selected zero tests; its corrected filter selected the 14 above.
No complete project-wide suite was run and no passing tests were rerun. The
Pipeline's RunStarted log counts its discovered test tree, while the filtered
result lists above identify the tests actually executed.

One disposable Play Mode smoke check confirmed:

- Tutorial ON starts at Welcome with zero employees, zero candidates, no Stage 1
  capability, all three establishment actions locked, and strategic pause held.
- Continue permits real HQ establishment; HQ and Casting establishment advance
  to hiring and enable recruitment.
- Skip preserves established facilities and permits Stage 1 establishment;
  the existing filming registry then contains its Stage 1 capability.
- Actor PA request accepted, immediate duplicate rejected, distinct Urgent
  director request accepted, strategic pause false throughout those requests.
- The Welcome panel, Continue/Skip buttons and paused HUD were visually inspected
  in a composited Unity Game view capture under the real rendering setup.
- On exit, original bootstrap options were restored, Play Mode was stopped,
  and the active scene remained clean. No scene was saved.

Current uncleared Console ground truth after the smoke: **0 errors, 0 warnings**.
The Pipeline history also contains older diagnostics from prior sessions; these
were not cleared or represented as newly introduced failures.

Local evidence is in `Temp/TutorialMilestone`: `tutorial-tests.json`, the three
existing-fixture result JSON files, `play-start-state.json`,
`play-actions-result.json`, `restored.json`, `console.json`, and `welcome.png`.
These are temporary validation artifacts, not game assets.

The complete guided release was validated through real domain objects/events in
the new lifecycle test. A full mouse-driven hire/write/film/release playthrough,
PA subtitle visual inspection, a player build and save/load integration were not
performed. No Computer Use, audio generation, package updates, commits or pushes
were used.

## Deferred work and next milestone

Final New Game/settings/construction UI, polished guide art and highlights,
recorded/generated voice, audio playback/TTS, localization, era presentation,
streamer settings, full save integration and Movie Maker progression remain
deferred. The semantic data has no 1930-only voice implementation.

Recommended next milestone: normal new-studio establishment/construction and
save/load integration, including authored site selection, facility costs/work,
support-building availability, and restoration of tutorial state alongside the
real studio. Keep the already-known Stage 1 crowd-separation defect tracked
separately; this milestone does not claim to resolve it.

