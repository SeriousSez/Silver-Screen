# Unity 6.6 stabilization

Validation completed on 2026-09-27 with one unresolved intermittent spacing defect.
Unity 6000.6.3f1 / URP 17.6.0 / Windows Direct3D 12. No commits or pushes.

**Final suite: 207/207 passed, zero failed/skipped, including all 201 original tests.**
This is not yet an unqualified clean baseline: a focused traversal run violated
employee separation, although five subsequent focused runs and the final full
suite passed. See remaining defects below.

Editor executable: `C:\Repos\Unity\6000.6.3f1\Editor\Unity.exe`
(version `6000.6.3f1`, revision `45d8eee7de74`). Package manifest and lock file
are unchanged in this stabilization milestone. Cinemachine remains absent.

## Preserved baseline

The migration evidence contains 201 tests, 196 passed, five failed, zero skipped.
`Logs/Unity66Stabilization/files-before.json` preserves 3,143 working-tree hashes;
`before/` preserves 1,439 source/serialized/document files. Comparisons use this
working tree, not HEAD. Migration and lifecycle documents were read before edits.

## Investigation decisions

### Screenplay capabilities

The capability constructor discarded its argument and used the entire prototype
catalogue; the live writing driver also injected that unrestricted catalogue.
Generation now snapshots the authoritative studio capability service once per
screenplay. The three original failures pass, plus a new reused-generator test
covering provider replacement/removal and preservation of earlier content.

### Live Camera test precondition — recorded before test changes

Two instrumented reproductions showed both pointer presses intercepted before
world raycasting. At screen `(945.57, 678.08)`, GraphicRaycaster hit `Viewport`,
`ScrollView`, and `ProductionsPanel`. StudioHud intentionally opens the Production
Slate on startup. The test moved the world camera and confirmed a Physics ray hit
Stage 1 but never closed the opaque management panel covering that screen point.
Thus the test was clicking UI, not an exposed building. Timing and building
identity were not the cause. UI interception must remain intact.

The correction establishes the intended visible-building precondition through
the existing UI, retain the actual Input System/raycast path and original Live
Camera assertion, and checks that UI-covered and single clicks do not enter Live
Camera. No assertion, timeout, or expected camera behavior is relaxed.

With the slate closed, the original double-click assertion passed. The next,
previously unreachable screenshot helper then threw because it always requested
`Camera.main`: Live Camera intentionally disables the tagged management camera
and enables an untagged production camera. Capture must use the active rendering
camera instead. This is a diagnostic-helper correction, not a camera behavior
change or an omitted capture.

### Door clearance

An exact capsule/door query at the migration failure position reproduced
0.0109506855 m penetration, while NavMesh.SamplePosition accepted that same X/Z.
The baked contour around the thin, angled leaf admitted a physically invalid
capsule centre. The builder now adds a non-walkable volume projected from the
actual leaf/hardware bounds; the existing NavMesh agent-radius erosion supplies
body clearance. No tolerance, employee size, collider, door pose, entrance anchor,
or visual geometry was changed.

The Stage1 live prefab gains only that volume (all existing object IDs preserved)
and its two existing navigation assets were rebaked with GUIDs retained. Normal
6.6 integer-array suffix serialization also appears in the prefab. Studio needs
no saved scene changes. The corrected runtime test measured **zero** physical
door/wall penetration and **0.6998842 m** minimum employee spacing, with all five
employees arriving through the personnel entrance. Both Stage1 runtime tests
passed, including the complete two-scene retake/release/payout flow.

### Panel initialization and layout

The inactive serialized component owned bindings in Start while AttachToHost
detached only its card. Initialization is now explicit and idempotent, shared by
hosting, Start, and reference setup. Hosting keeps the panel owner with its view
and activates it, restoring normal lifecycle/cleanup. The displayed project is
observed through its own update event; the director picker targets that displayed
project. Layout uses separate mode, take-selection, and action rows, with a
horizontal scroll area for accumulated takes and reserved status/progress space.

Two new regressions failed before the fix: the initial director button did not
open and decision buttons overlapped. Both now pass, including repeated hosting,
first-use director selection, independent pinned-project updates, cast/mode refresh,
and longer control labels.

### Warnings

ProductionAppearance is an existing serializable DTO. Its nullable grey override
is now a nullable property backed by serializable presence/value fields, retaining
null-inherit and explicit-zero behavior without adding a Unity dependency to the
domain. Nine appearance tests pass, including three new Unity JSON round trips.
The pause label used Roman numeral U+2161; it now uses the existing font's ASCII
double bar (`||`) instead of expanding a font atlas.

## Full-suite investigation and final validation

The first full graphics-enabled run returned 207 total, 206 passed, one failed,
zero skipped. The added UI-interception assertion assumed the projected building
point lay beneath the slate at every Game view size. This was true in the focused
Editor run and false in batch. The test now targets the slate's actual rectangle
for the covered-click check, then moves to the original building point after
closing the slate. UI interception, single-click, and the original double-click
assertions remain enforced with unchanged timing. A second batch run exposed a
separate focus precondition: direct GraphicRaycaster hits were valid at the slate
center and the synthetic mouse position matched, but `Application.isFocused` was
false. A further probe showed the UI point action enabled but still reading `(0,0)`
while the device read the requested position: Editor focus routing prevented the
action from receiving pointer events. The fixture now saves/restores both
`InputSystem.settings.backgroundBehavior` and `editorInputBehaviorInPlayMode`,
using IgnoreFocus and AllDeviceInputAlwaysGoesToGameView only during synthetic
input, alongside its existing hardware isolation.
The production setting and selection controller are unchanged. All unsuccessful full runs and focused diagnostics are retained. The final complete
run passed after the fixture corrections described here; results follow below.

### Traversal starting-state precondition — recorded before test changes

The next full run passed the camera flow but counted only four entrance crossings.
The first navigation sample already placed Ruby at root-local `(5.847, 1.091,
-11.783)`, beyond the monitored `z=-12` entrance plane; she reached her station
without crossing that plane again. All five arrived and collision checks passed.
The test had allowed autonomous movement during startup without establishing
that all five began outside. The fixture now first routes every employee to an
exterior rendezvous using ordinary navigation, then start the existing measured
traversal. It retains all five crossings, all collision samples, spacing,
agent dimensions/speed, and the original penetration tolerance. No teleport,
employee-specific exception, or omitted crossing is used.

The exterior-start fixture exposed one employee-spacing result of 0.656042814 m
against the unchanged 0.69 m minimum. Two subsequent focused runs passed (minimum
spacing 0.6939961 m / 0.6998668 m; maximum wall/door penetration 0.002629775 m /
zero). Three further bounded repeats also passed, for five consecutive focused
passes after the spacing failure. This intermittent result is not dismissed by
passing reruns. Its pair/contact location was not reproduced with diagnostics,
so no speculative crowd-navigation change is justified. The fixture retains
pair/position logging whenever separation drops below the requirement. No spacing
assertion or navigation dimensions have been weakened. Until the cause is
resolved, an unqualified clean-baseline claim is not warranted.

## Final test results

The complete suite used the recorded migration methodology: graphics-enabled
Unity batch mode, EditMode runner (including its EnterPlayMode runtime tests),
without `-nographics`, a test filter, or `-quit`:

```powershell
& 'C:\Repos\Unity\6000.6.3f1\Editor\Unity.exe' -batchmode `
  -projectPath 'C:\Repos\Unity\Silver Screen' `
  -runTests -testPlatform EditMode `
  -testResults 'C:\Repos\Unity\Silver Screen\Logs\Unity66Stabilization\full-tests.xml' `
  -logFile 'C:\Repos\Unity\Silver Screen\Logs\Unity66Stabilization\full-tests.log'
```

Runtime and Editor assemblies compiled with zero C# errors. Runner exit code: 0.
The XML full-name comparison preserves all original 201 tests; none are skipped,
removed, or replaced. Original assertions, timeouts, physical dimensions and
0.005 m penetration / 0.69 m separation thresholds remain enforced.

| Group | Migration baseline | Final stabilization |
| --- | --- | --- |
| Original tests | 196 passed / 5 failed / 0 skipped | 201 passed / 0 failed / 0 skipped |
| Added regressions | — | 6 passed |
| Complete suite | 201 total | 207 passed / 0 failed / 0 skipped |
| ProductionLifecycleRegressionTests | 16 passed | 16 passed |
| SceneTemplateTests | 31 passed | 31 passed |

All five named baseline failures pass in the final run:

- `ScreenplayContentGenerator_RefusesCleanlyWhenStudioHasNoCapabilities`: the supplied capability provider is now honored; empty availability fails cleanly.
- `ScreenplayContentGenerator_UsesSingleAvailableFacilityCapability`: generation uses the single available facility rather than the whole catalogue.
- `Generator_UsesNewlyConstructedFacilityWithoutMutatingEarlierContent`: each generation snapshots current capabilities; existing content is unchanged.
- `EmployeesTraverseDoorAndReachInteriorWithoutPenetration`: final maximum wall/door penetration **0 m**, minimum employee separation **0.6955977 m**, all five crossed and arrived. The intermittent separation failure below prevents a reliability claim.
- `TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts`: passes the actual input/raycast/camera path and the full two-scene lifecycle. The opaque slate and batch input focus were invalid test preconditions; production camera behavior was not altered.

Focused validation included capabilities 2/2, SpecializedSetConstruction 12/12,
appearance 9/9, production-panel regressions 2/2, and Stage1 runtime tests 2/2.
The six added cases are one reused-capability-provider regression, two panel
lifecycle/layout regressions, and three optional-grey JSON round trips (null,
explicit zero, nonzero). Longer labels and repeated initialization are covered.

Evidence is retained under `Logs/Unity66Stabilization/`: `full-tests.xml`,
`full-tests.log`, `test-comparison.json`, `final-runtime.txt`, focused XML/JSON
results, earlier failed runs, and preserved before-files. Final full-suite
traversal completed at 13:53:55 UTC on 2026-09-27.

## Manual Studio smoke results

Performed after the green complete suite in the real Editor, Studio scene,
PC_RPAsset / StudioDaylightProfile / DX12, Full HD 1920x1080 Game view.
These are actual inspected UI/rendering results, with normal clock advancement
through the existing API to accelerate casting, filming, and payout waits.
No writable project-state/progress override was introduced or used.

| Check | Observed result |
| --- | --- |
| Studio and buildings | Studio loaded; HQ, Casting, Stage 1 exterior/interior and administration rendered with normal materials and daylight. |
| First detail / Assign Director | Created The Last Picture; the first director click opened the picker and assigning Stanley Hawk immediately updated the panel. |
| Cast / mode / state | Jack Sterling and Clara Vance assignments displayed immediately; Manual mode highlighted immediately; Casting, Ready to Film, moving crew, and Filming reflected current state. |
| Take controls | Shoot Again, Take 1/Take 2, Keep Take, mode and Release remained separate and accessible. The second take appeared without closing/reopening the detail panel. |
| Complete / release | Selected Take 2, kept it, completed at quality 79, and released to Now Playing. Release became unavailable after release. |
| Payouts | Four payout weeks completed; finance ledger and project gross agreed at $216,562, cash $416,562 after the $50,000 budget. |
| Stage 1 input | Single click did not enter Live Camera; two separate real pointer double-clicks entered it. The interior performance rendered. |
| Entrance / exit | Actors visibly entered and exited using ordinary navigation, with no visible leaf penetration. At a paused exit sample both capsule/leaf queries reported no contact; peer separation was 1.09333944 m. |
| Heated Argument | Short/Medium/Long visibly resolved to 3/5/8 beats. Next Beat changed performer/action, Reset returned to the first beat, and Long playback animated silhouettes and reached Sequence complete. |

Entrance/exit visual inspection used existing task-destination APIs to route actors
and paused at the threshold; it did not teleport them or change agent dimensions.
The paused exit measurement is a sample, not a continuous exit collision proof.
The synthetic Escape tap did not demonstrate camera exit, so manual keyboard-exit
validation is not claimed; the existing Exit API restored the management camera.
The automated full lifecycle camera flow passed.

The live asset scan found **0 missing scripts, 239 renderers, 0 missing materials,
0 unsupported shaders**, and 6,853 NavMesh vertices / 3,413 triangles. All five
production employees had complete paths on the NavMesh. The inactive prototype
slate operator was correctly reported as inactive, not an active path failure.
No player build or save/load validation was performed in this milestone.

## Console and warnings

The uncleared Play Mode Console after the production/entrance smoke contained
**0 errors and 0 warnings** (`play-console.json`). Neither the nullable-field
UAC1001 warning nor the missing U+2161 glyph warning recurred. Batch logs retain
Unity/package diagnostics (including internal Deprecated.cs/Windows module and
licensing-retry messages); package sources were not patched.

After Play Mode, a bridge SaveAssets/read request issued while the preview's
native duration dropdown was open timed out at 5 seconds. Closing the dropdown
and repeating the request succeeded. The final uncleared Editor Console retains
that **one com.unity.pipeline tooling error**, zero warnings, and no compilation
failure. It is not a project gameplay exception and is not hidden by clearing.

## Changed files and assets

Compared with the preserved working tree, the intentional existing-file changes
are:

- `Assets/Scripts/Domain/Writing/ScreenplayContentGenerator.cs`: authoritative capability snapshots.
- `Assets/Scripts/Domain/Writing/ScreenplayWritingCoordinator.cs`: starter capabilities for the existing convenience path.
- `Assets/Scripts/Presentation/Writing/ScreenplayWritingPresentation.cs`: inject live filming capabilities.
- `Assets/Scripts/Presentation/UI/MovieProjectUI.cs`: explicit initialization, owned subscriptions/listeners, displayed-project refresh and take layout.
- `Assets/Scripts/Presentation/UI/StudioHud.cs`: ASCII pause representation.
- `Assets/Scripts/Domain/Characters/ModularAppearance.cs`: serializable optional grey representation.
- `Assets/SilverScreen/Environment/Stage1Live/Editor/Stage1LiveIntegrationBuilder.cs`: derive held-open-door navigation clearance from real mesh bounds.
- `Assets/SilverScreen/Environment/Stage1Live/Stage1_Live.prefab`: non-walkable clearance volume and normal Unity serialization.
- `Assets/SilverScreen/Environment/Stage1Live/Navigation/Stage1_Interior.asset` and `Studio_Revision04.asset`: rebaked navigation, existing GUIDs preserved.
- `Assets/Tests/Editor/ModularAppearanceTests.cs`, `SpecializedSetConstructionTests.cs`, `Stage1LiveRuntimeTests.cs`: regressions, justified fixture preconditions and failure diagnostics.

Added `Assets/Tests/Editor/ProductionPanelRegressionTests.cs` and its `.meta`, plus
this document. Existing prefab object IDs and asset metadata are preserved.
Studio.unity, package manifest/lock, project settings and production selection
controller remain unchanged from the saved working-tree baseline. Dynamic TMP
fallback-cache noise was restored from that baseline. No scene art, employee
body dimensions, door geometry, or camera architecture was replaced.

## Remaining defect and recommendation

**Clean baseline: not yet.** `batch-door-after.xml` records minimum employee
separation **0.656042814 m**, below the unchanged **0.69 m** requirement, after
establishing the valid exterior starting state. Five subsequent focused runs and
the final full suite passed, but do not invalidate that result. The responsible
pair/contact position was not reproduced with added diagnostics. It is not yet
established whether this is caused by the navigation rebake or an existing crowd
avoidance defect exposed by the stronger starting precondition.

No speculative avoidance refactor, smaller capsule, relaxed threshold, retry-to-pass
wrapper or skipped assertion was added. Preserve the failing evidence and new
pair/position/link-state diagnostics. The next milestone should reproduce and
fix intermittent crowd separation at the Stage 1 entrance, then repeat the
unchanged traversal invariant and complete suite. Continue focused stabilization
on Unity 6.6; do not treat this run as approval for a clean-baseline milestone or
start the deferred Movie Maker/character/integration feature work.
