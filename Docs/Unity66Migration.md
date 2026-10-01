# Unity 6.6 migration

Status on 2026-09-27: **Migration and validation completed. Safe to continue development on Unity 6.6 with the existing defects recorded below.** No confirmed migration-caused regression was found. This does not mean the project has a completely passing test suite or defect-free production UI.

## Version, scope, and preserved baseline

- From: **6000.5.9f1 (b57deb96f08d)**, URP **17.5.0**.
- To: **6000.6.3f1 (45d8eee7de74)**, URP **17.6.0**, Windows Editor / Direct3D 12.
- Exact executable: `C:/Repos/Unity/6000.6.3f1/Editor/Unity.exe`. Confirmed in Unity Hub's configured `C:/Repos/Unity` install root before opening or modifying the project. Executable product version: `6000.6.3f1_45d8eee7de74`; file version: `6000.6.3.4577518`. Windows Standalone IL2CPP support is installed.
- The earlier installation blocker is resolved. The recorded audit, working-tree backup, and 6.5 test results were reused; the 6.5 suite was not rerun.
- The repository already contained extensive uncommitted work. All migration comparisons use the saved working tree, not HEAD: `Logs/Unity66Migration/files-before.json` records 3,142 files; `before/` contains 1,496 backed-up Assets/Packages/ProjectSettings/Docs files. `status-before.txt`, `head-before.txt`, and `packages-before.json` preserve the remaining baseline evidence.
- No commits or pushes. No new gameplay, Movie Maker, character/3D-Disco, animation/IK, Cinemachine, or audio-generation work. No existing test was changed, weakened, removed, or skipped.

## Upgrade procedure and compilation

Closed the 6.5 Editor with Studio clean. Opened the project with the verified 6.6 executable using normal Unity migration and API updating:

```text
-batchmode -quit -accept-apiupdate -projectPath "C:/Repos/Unity/Silver Screen"
-logFile "C:/Repos/Unity/Silver Screen/Logs/Unity66Migration/upgrade.log"
```

Unity resolved packages, reimported assets, migrated settings, and exited successfully. No hand-edited Unity YAML, blanket package update, or forced whole-project reserialization was used. The interactive Editor subsequently completed its normal initialization; File > Save Project persisted the generated settings.

**Compilation: passed, zero C# errors.** Both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll` compiled and loaded under 6000.6.3f1. No project source/API compatibility edits were required. Package-driven analytics define changes caused the expected recompilation between batch and interactive contexts; final Standalone defines remain the baseline `APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED`.

The final Editor is out of Play Mode, idle, and displaying `Assets/Scenes/Studio.unity` with `isDirty=false`. The Heated Argument preview remains available. Runtime smoke-test names, movies, and camera positioning were not saved into the scene.

## Package changes

Resolved versions from `packages-lock.json`. Unity selected these changes automatically during migration; none was manually upgraded to an arbitrary latest release.

| Package (`com.unity.` prefix) | Before | After | Reason / source |
| --- | --- | --- | --- |
| `burst` | 1.8.30, registry | 2.0.0, built-in | Editor-provided version |
| `collections` | 6.5.0 | 6.6.0 | Editor-provided version |
| `dt.app-ui` | 2.1.7 | 2.1.11 | Resolver selected an update to the existing inference/UI dependency |
| `multiplayer.center` | 1.0.1 | 2.0.1 | Editor-provided version; no multiplayer integration added |
| `render-pipelines.core` | 17.5.0 | 17.6.0 | 6.6 rendering package set |
| `render-pipelines.universal` | 17.5.0 | 17.6.0 | 6.6 rendering package set |
| `render-pipelines.universal-config` | 17.5.0 | 17.6.0 | URP dependency |
| `shadergraph` | 17.5.0 | 17.6.0 | URP dependency |
| `test-framework` | 1.7.0 | 1.8.0 | Editor-provided version |
| `test-framework.performance` | 3.5.0, registry | 6.6.0, built-in | Now supplied with the Editor |
| `timeline` | 1.8.12, registry | 6.6.0, built-in | Editor-provided version |
| `ugui` | 2.5.0 | 2.6.0 | Editor-provided UGUI/TMP version |
| `graph-authoring` | Absent | 1.0.0, built-in | New Shader Graph dependency |
| `profiling.core` | Absent | 1.0.3, registry | New Render Pipeline Core dependency |
| `modules.tetgen` | Absent | 1.0.0, built-in | Engine module added by Unity |

Unchanged resolved packages:

| Package (`com.unity.` prefix) | Before = after |
| --- | --- |
| `2d.sprite` | 1.0.0 |
| `ai.assistant` | 2.19.0-pre.2 |
| `ai.inference` | 2.6.1 |
| `ai.navigation` | 2.0.14 |
| `collab-proxy` | 2.13.6 |
| `ext.nunit` | 2.1.0 |
| `ide.rider` | 3.0.38 |
| `ide.visualstudio` | 2.0.26 |
| `inputsystem` | 1.20.0 |
| `mathematics` | 1.4.0 |
| `nuget.mono-cecil` | 1.11.6 |
| `nuget.newtonsoft-json` | 3.2.2 |
| `pipeline` | 0.7.0-exp.1 |
| `searcher` | 4.9.5 |
| `visualscripting` | 1.9.12 |

Existing `com.unity.modules.*` dependencies remain 1.0.0. Terrain remains explicitly declared even though Render Pipeline Core dropped its dependency on it. UI Toolkit is supplied through the Editor UIElements module; the runtime management UI uses UGUI/TMP.

**Cinemachine: absent before and after.** Existing SilverScreen camera classes are preserved. No Cinemachine migration or addition was performed.

## Unity-generated serialization and asset review

Complete migration delta relative to the saved working tree, excluding ignored evidence:

1. `ProjectSettings/ProjectVersion.txt`: exact Editor version/revision above.
2. `Packages/manifest.json`: Editor-bound direct dependency updates and the TetGen module.
3. `Packages/packages-lock.json`: resolved versions, source/dependency changes, and three additions listed above.
4. `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`: asset version **10 → 11**; post-processing stripping field renamed; 2D stripping field added; SSAO resource types migrated to BlueNoise/Core resources while retaining the existing shader GUID; normal SSR, mip-generation, film-grain, sky, UI backdrop, and rendering-layer resources added.
5. `ProjectSettings/ProjectSettings.asset`: serialized version **29 → 30**; Unity's integer-array text gains `i` suffixes; defaults added for `callOnDisableOnAssetBundleUnload`, `webProgressiveAssetLoading`, `managedCodeVariant`, and `webGPUDeviceFilterListAsset`. No custom Managed Code Variant or gameplay configuration was selected.
6. `ProjectSettings/ProjectAuditorSettings.asset`: parameter ordering changed; keys and values are unchanged.
7. `ProjectSettings/QualitySettings.asset`: both quality entries migrate serialized version **4 → 5**, adding `meshLodThreshold: 1`; Unity adds the Nintendo Switch 2 default quality mapping. PC/Mobile pipeline GUIDs and existing quality values are preserved.
8. `Docs/Unity66Migration.md`: this report, originally created during the blocked audit and now completed.

**No other baseline file changed or disappeared.** Source, tests, Studio scene, prefabs, materials, navigation assets, and all existing `.meta` files retain their baseline hashes. Thus asset GUIDs and authored material assignments were preserved. No asset re-authoring was needed. The final detailed generated diff is in `Logs/Unity66Migration/migration-diff.txt`.

Save Project also persisted a temporary dynamic TMP fallback atlas containing the back-arrow glyph used during smoke testing. Its entire diff was glyph/cache data, with no font schema or authored-setting change. Restored that font asset from the saved working-tree baseline and refreshed it in Unity; the normal empty dynamic font asset loads successfully. The incidental cache is preserved only in ignored evidence.

## Complete automated test comparison

Used the same graphics-enabled full EditMode methodology as the recorded baseline, including tests that enter Play Mode. No `-nographics`, filtering, exclusions, or assertion changes:

```text
"C:/Repos/Unity/6000.6.3f1/Editor/Unity.exe"
-batchmode -projectPath "C:/Repos/Unity/Silver Screen"
-runTests -testPlatform EditMode
-testResults "C:/Repos/Unity/Silver Screen/Logs/Unity66Migration/full-tests.xml"
-logFile "C:/Repos/Unity/Silver Screen/Logs/Unity66Migration/full-tests.log"
```

| Run | Total | Passed | Failed | Skipped | Duration |
| --- | ---: | ---: | ---: | ---: | ---: |
| 6000.5.9f1 recorded final | 201 | 196 | 5 | 0 | 41.8401797 s |
| 6000.6.3f1 | 201 | 196 | 5 | 0 | 55.5329522 s |

The XML test-name/result maps match exactly: **zero added/removed tests and zero changed outcomes**. Overall runner status remains Failed because the five baseline defects remain. The single-run duration difference is not a performance benchmark.

- **ProductionLifecycleRegressionTests: 16/16 passed.**
- **SceneTemplateTests: 31/31 passed.**
- One/four-scene automatic and manual production, retakes/Keep Take, interruption/cancellation, retry, stale callbacks, duplicate greenlight protection, release idempotence, and progress reporting remain green.
- `Phase1StudioRuntimeTests.StudioProductionLiveViewRetakeAndRelease` passed in the real Studio scene, including casting/routing, strategic-work versus local presentation timing, Live Camera pause behavior, retake/keep, release, four weekly payouts, and no duplicate revenue afterward. Its diagnostic output is preserved; missing test-capture files are not presented as visually inspected screenshots.

### The five known failures

All are **A: existing baseline failures**. None was fixed or hidden.

| Test | 6.6 behavior versus 6.5 |
| --- | --- |
| `MovieProductionCoordinatorTests.ScreenplayContentGenerator_RefusesCleanlyWhenStudioHasNoCapabilities` | Identical: expected `ScreenplayContentGenerationException`, none thrown. |
| `MovieProductionCoordinatorTests.ScreenplayContentGenerator_UsesSingleAvailableFacilityCapability` | Identical: expected true, actual false. |
| `SpecializedSetConstructionTests.Generator_UsesNewlyConstructedFacilityWithoutMutatingEarlierContent` | Identical: expected only office capability; the same mixed capability sequence was produced. |
| `Stage1LiveRuntimeTests.EmployeesTraverseDoorAndReachInteriorWithoutPenetration` | Same employee/door and assertion. Penetration **0.009343893 → 0.011192441 m**, about **1.85 mm greater**, against the unchanged 0.005 m threshold. Position changed from `(5.757, 1.041, -13.453)` to `(5.759, 1.042, -13.443)`. |
| `Stage1LiveRuntimeTests.TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts` | Identical failure: actual building double-click did not enter Live Camera View. |

The door measurement is numerically worse and is not described as identical. It remains the same small contact defect; successful Studio routing/entrance and production checks did not reveal a broader navigation regression. A single timing-sensitive sample does not establish a new engine collision regression or prove that the old defect is resolved. Leave collision investigation to the separate stabilization milestone.

No automated failures were classified B (migration regression), C (newly exposed pre-existing defect), or D (test/environment problem). Additional manual findings are classified separately below.

## Editor/runtime smoke and asset checks

Performed in the actual 6000.6.3f1 Editor with Direct3D 12, PC quality, `PC_RPAsset`, and `StudioDaylightProfile`. Visual claims below refer to inspected Editor/Game views, not only serialized data.

| Check | Result and method |
| --- | --- |
| Open Studio, enter/exit Play Mode | Passed. Final Studio scene clean, Editor idle, both assemblies loaded. |
| HQ, Casting Office, Stage 1, signage | Visually inspected. Prototype HQ and Casting signs readable; the separate `StudioAdministration_01` facade, roof/windows, and Stage 1 render normally. No persistent cyan/pink or missing-material appearance after initial shader warmup. |
| Studio identity | UI rename to `Migration Smoke Studios` updated overview, top HUD, and HQ sign. Runtime only; discarded on Play Mode exit. |
| Camera | Mouse-wheel zoom passed. Bounded W/Q Input System events exercised existing pan/rotation handlers and visibly moved the camera; desktop automation's instantaneous key taps were too short for frame-based sampling. Focus API used to frame representative assets for inspection. |
| Management UI | Studio, Productions, Staff, Screenplays, Finances, create/cast dialogs, and close/back controls inspected. Production detail limitations below prevent an unqualified all-UI-pass claim. |
| Time and finance | Pause stopped the clock; 2x/3x resumed at Fast/VeryFast; initial 1x ran normally. HUD and finance ledger updated. Existing missing pause glyph remains. |
| Navigation and entrances | Jack Sterling and Clara Vance routed into casting, then both actors and Stanley Hawk reached Stage 1 and filmed. Post-production staff showed no assignment. All five employee NavMesh agents reported on-mesh/complete paths; triangulation contained 6,860 vertices / 3,418 triangles. Known door test remains failed as above. |
| Create / cast / Ready to Film | Created `The Last Picture` through UI; cash $250,000 → $200,000 for $50,000 budget. Cast both actors through UI. Assigned director through existing coordinator API because of the inactive legacy panel defect. Observed ReadyToFilm/MovingToStations, then Filming with all participants at stage. |
| Film / retake / keep / complete | Normal clock advanced take work; no forced state/progress setters. UI Shoot Again created take 2. Reopened detail panel to refresh, selected take 2, and clicked Keep. One scene completed, quality 79, release enabled, take 2 selected for final cut. |
| Release / payouts | UI Release succeeded. Advanced normal clock/scheduler by 28 days and allowed pending work to drain. Four weekly payouts: $97,453, $64,968, $36,815, $17,325; total revenue $216,562 and cash $416,562. Finance panel displayed the matching transactions. |
| Greenlight integration | Existing four-scene screenplay test fixture passed into the real Studio coordinator: Draft movie with four independent adapted scenes/two cast roles created; duplicate rejected as AlreadyGreenlit. This was service-assisted, not a manual screenplay-writing/recruitment walkthrough. |
| Heated Argument preview | Short/Medium/Long selectable (3/5/8 beats); A/B and swapped assignments work; duplicate binding rejected and valid binding recovers. Play, Pause, Next Beat, Reset, Approach/Exit movement, beat order, roles, camera suggestions, anchors, environment/prop metadata inspected. Remains a developer visualization, not a cinematic-quality claim. |
| Prefabs/materials | Representative HQ, CastingOffice, Stage1_Live prefab scans: no missing scripts/materials, relevant shaders supported. Studio runtime scan: 245 renderers, zero missing scripts/materials, no unsupported assigned shader. |
| UI/lighting/definitions | StudioCanvas ScreenSpaceOverlay intact; directional/interior lighting and daylight volume load. Six genre ScriptableObjects retain valid IDs/display names. URP PC/Mobile assignments and GUID-bearing assets remain unchanged. |
| Performance sanity | Editor/UI/routes remained responsive after initial import/shader warmup. Sampled Editor frame times approximately 5.18–5.53 ms during the warmed runtime; not a controlled before/after benchmark. No severe rendering failure or repeated exception stream. No allocation profiling or optimization was performed. |

### Newly surfaced pre-existing UI defects (C)

The production detail panel can retain stale state/cast/mode labels until closed and reopened. Its initial Assign Director button did not open the picker. Take-decision controls overlap the mode row, though Shoot Again, take selection, Keep, and Release were usable.

Evidence: the saved 6.5 `Studio.unity` already has `MovieProjectPanel` **`m_IsActive: 0`**, byte-identical to the migrated scene. `StudioHud` calls `MovieProjectUI.AttachToHost` to reparent its visible card but does not activate the component's original object; `MovieProjectUI.Start` owns director-button/event initialization. Runtime inspection confirmed that component remained inactive. The take-control anchor/position code is also unchanged from the snapshot and places the decision buttons into the mode row. These findings explain the observed behavior without a Unity compatibility change. A second 6.5 interactive reproduction was not performed; classification is based on the preserved scene/source and runtime evidence.

No persistent workaround or UI fix was applied. Reopening the panel and service-assisted director assignment were validation techniques, not shipped fixes. These findings should join the next stabilization milestone; they are distinct from the five recorded automated failures.

## Warnings, limits, and recommendation

- Existing **UAC1001** at `ModularAppearance.cs:117`: nullable `float? greyFractionOverride` is skipped by serialization. Present in the 6.5 baseline; unchanged.
- Existing TMP **U+2161** missing-glyph warning: pause label renders a replacement square. Present in the 6.5 log; unchanged.
- 6.6 import/reload logs contain a Render Pipeline Core `Editor/Deprecated.cs` partial-class diagnostic and unsupported unused Terrain/HDRP shader-variant messages. Both assemblies compile; inspected assigned shaders are supported. No package-source patch was made.
- The native WindowsStandalone-extension message also appears in the 6.5 baseline. Installed Windows IL2CPP support was confirmed independently. Licensing initially retried, then resolved normally.
- Final Play Mode console: **0 errors**, four warning events comprising the two existing project warning types above. Console was read without clearing it to hide results.
- The earlier compatibility audit found no relevant legacy UXML Factory/Traits, removed Rendering Debugger, `UNITY_64`, `DEVELOPMENT_BUILD`, `ForceEnableAssertions`, or inline-YAML setting usage requiring a rewrite. Dynamic batching was already disabled. Keep Managed Code Variant and the serialization warning in mind for future build/version work. See the [official Unity 6.6 upgrade guide](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66) and [6000.6.3f1 release notes](https://unity.com/releases/editor/whats-new/6000.6.3f1).
- No standalone/IL2CPP Player build was requested or performed. Editor/runtime validation does not certify a shipping Player build, every art asset, or save/load behavior.

**Recommendation: safe to continue development on 6000.6.3f1, with the existing baseline defects and documented UI limitations.** No migration-specific source fix or rollback is indicated by this validation. The full suite retains its exact baseline outcome and both required regression groups remain green.

**Next milestone: focused stabilization of the five known baseline failures and the production-panel initialization/layout defects**, before Movie Maker or final character integration. That work is not begun here.

## Evidence index

Generated evidence remains under ignored `Logs/Unity66Migration/`:

- `upgrade.log`, `full-tests.xml`, `full-tests.log`, `smoke.log`.
- `test-comparison.json`: exact before/after test names, outcomes, failure messages, and critical-suite totals.
- `migration-diff.txt`, `final-file-comparison.json`: reviewed changes against the saved working tree.
- `asset-inspection.json`, `studio-inspection.json`, `final-editor-state.json`, `console-final-play.json`.
- `runtime-evidence/`: routing, retake, kept/released take, payout, greenlight, and camera-state records; inspected take-control screenshot; extracted automated runtime diagnostics.

Temporary diagnostic snippets are confined to that ignored evidence directory. A capture tool briefly created an Assets/Logs screenshot import; it was moved to the evidence directory and its generated folder/metas removed. No temporary test or screenshot asset remains in Assets.
