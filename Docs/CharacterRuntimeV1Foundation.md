# Character Runtime V1A-2 — shared foundation

This milestone implements the isolated four-family foundation described in [CharacterRuntimeV1ArchitectureAudit.md](CharacterRuntimeV1ArchitectureAudit.md). The audit remains unchanged as the pre-implementation rationale. Normal Studio recruitment still creates its existing capsules. No production family-assignment policy, Studio migration, filming adoption, or V1B work is included.

Repository baseline: `master`, HEAD and fetched `origin/master` both `ba63ced6889d4851fadca8f923c8b7239a3925a7`. The audit was the sole untracked file at the start. No staging, commit, push, branch switch, worktree creation, reset, stash, or history rewrite is part of this milestone.

## Ownership and identity

`PersonPresentationIdentity` is a Unity-free value type containing the stable `CharacterFamily` enum: Unassigned=0, AdultFemale=1, AdultMale=2, Boy=3, Girl=4. Unknown future enum values remain unresolved. The optional final `PersonProfile` constructor argument defaults to Unassigned; existing callers remain source compatible. The existing `AppearanceProfileId` boundary is unchanged. Family is never inferred from age, role, name, sex, height, prefab names or meshes.

`PersonProfile` remains the person. Candidate and Employee remain domain state. CandidateAgent and EmployeeAgent retain navigation, task and autonomy authority. `CharacterPresentation` attaches to their existing root and owns only its visual child, resolved definition/profile, Animator, facial controller and visual attachment anchors. It does not scale or replace the simulation root, create a second person, register autonomy or choose destinations.

The four mesh families remain independent. The historical CharacterAppearance/CharacterCatalog/CharacterMotion/adult-v1 pipeline is not reactivated.

The isolated API is explicit:

```csharp
var identity = new PersonPresentationIdentity(CharacterFamily.Girl);
// Supply identity when constructing the PersonProfile, before binding its agent.
var result = CharacterPresentationFactory.Attach(existingRoot, person, localCatalog);
if (result.IsCanonical && result.Presentation.Face.Supports(FacialSemantic.BlinkBoth))
    result.Presentation.Face.TrySet(FacialSemantic.BlinkBoth, 0.5f);
```

The root must already have one navigation agent and one capsule collider. The factory validates family, catalogue, version, physical data, wrapper references and component whitelist before changing a working presentation. Repeating an equivalent bind retains the visual and facial state. Held/settling ownership defers replacement; regeneration is never attempted per frame. A different person cannot be bound onto a working presentation. An explicit profile override resolves a value copy without mutating the shared definition; actual stature deformation is not implemented.

## Metadata and local catalogue

Four committed ScriptableObjects live in `Assets/SilverScreen/Characters/Definitions/`: AdultFemale.asset, AdultMale.asset, Boy.asset, Girl.asset. Their stable IDs are `silverscreen.canonical.adultfemale.v1`, `silverscreen.canonical.adultmale.v1`, `silverscreen.canonical.boy.v1`, and `silverscreen.canonical.girl.v1`. Each has version 1, measured physical defaults, explicit facial bindings, capability notes, a Humanoid requirement and a twenty-frame BlinkBoth requirement. The expected animation contract is `Humanoid.Speed.Idle.Walk.Run.v1`.

These assets contain no licensed mesh, material, Avatar, controller or ignored-prefab references. They remain loadable metadata on a clean checkout without local character imports.

`CharacterFamilyCatalog` builds its family dictionary once, validates duplicate families/definition IDs and returns a clear reason for missing/invalid data. Explicit configuration or asset reload invalidates the lookup. It performs no scene scans. The optional local catalogue holds references to the four committed definitions and the four ignored visual-only prefabs. There is no automatic Resources load from Studio recruitment.

`CharacterVisualReference` is a small local-wrapper component with explicit Animator, face-renderer and Head-bone references plus definition ID/version. It has no update loop or identity. Runtime validation rejects colliders, agents, arbitrary MonoBehaviours, missing scripts and multiple Animators anywhere beneath a wrapper. Required supported facial channels must actually exist; declared minimum BlinkBoth frame counts must be satisfied.

## Physical profile and measurements

All distances are metres. The simulation root is upright and has unit world scale. Its centre-root convention uses a base offset `b`:

```text
feet / ground point       = root.position - worldUp * b
visual local origin       = (0, -b, 0)
ground-seated capsule Y   = capsuleHeight / 2 - b
selection marker local Y  = groundClearance - b
information world point  = root.TransformPoint(visualOrigin + feetLocalInformationAnchor)
```

`CharacterPhysicalProfile` is a serialized value struct, copied per instance. It validates finite dimensions/anchors, positive dimensions, nonnegative offsets, height >= 2*radius, ground-seated capsule centre and the supported 2 m / .35 m envelope. The legacy profile stays 2 m / base offset 1 m / radius .35 m. No runtime Renderer.bounds fitting is used.

The defaults were measured read-only from the accepted local reference prefabs, using their native model scale/alignment and `SkinnedMeshRenderer.BakeMesh(mesh, true)`, then transforming baked positions into the reference `CharacterVisualRoot` frame. This is the same scale-aware CPU bake convention used by the committed family tests. A preliminary measurement without the `true` flag double-applied imported renderer scale; those values were rejected before creating metadata.

Body/rest height is maxY-minY of the neutral evaluated bald body. The carry pivot is that family's rest Neck in the same feet-local frame. The information anchor is measured rest height plus an explicit .08 m UI clearance; this is a fixed rest anchor, not a claim to track the animated head each frame. HeadAttachment separately exposes the actual Head-bone Transform for future hair/headwear. AppearanceRoot is available for future clothing/accessory attachment without implementing slots or wardrobe.

Navigation/capsule radius is an explicit core-body interaction policy: select vertices with at least .5 cumulative skin weight on Hips, Spine, Neck or Head; take the maximum XZ distance from the feet axis; add .025 m clearance. Arms/hands extended in the rest pose do not define a navigation cylinder. Each family was evaluated independently. This capsule is not a full animated limb envelope or a foot-contact solution.

| Family | Body/nav/capsule height | Nav/capsule radius | Base offset | Suspension pivot, feet-local | Information Y, feet-local |
| --- | ---: | ---: | ---: | --- | ---: |
| AdultFemale | 1.6753788 | .2161468 | .8376894 | (0, 1.375627, -.07523590) | 1.7553788 |
| AdultMale | 1.8437147 | .22361755 | .92185736 | (0, 1.524192, -.08326137) | 1.9237148 |
| Boy | 1.4662502 | .17680576 | .7331251 | (0, 1.209733, -.08244407) | 1.5462502 |
| Girl | 1.4607698 | .18160097 | .7303849 | (0, 1.192674, -.07856727) | 1.5407698 |

All four capsules have centre (0,0,0) with these defaults. Carry lift 1.2 m, marker clearance .02 m and placement floor clearance .06 m remain deliberate interaction policies, not copied anatomical measurements. The existing .15 m placement sampling tolerance remains unchanged. Raw measurement code/results are `TestResults/CharacterRuntimeV1/measure.cs` and `measurements.txt`.

`EmployeeNavigationProfile.Apply(root)` now detects an initialized CharacterPresentation and applies its profile. EmployeeAgent.Awake therefore retains the resolved dimensions; it still initializes legacy people exactly as before. Boy and Girl remain about 1.466/1.461 m after Awake and hiring, rather than being overwritten to 2 m. Factory attachment preserves the pre-existing ground point when changing centre offset. Selection markers are seated from the resolved profile.

The NavMesh bake is unchanged. Smaller runtime profiles do not create child-only paths, smaller-radius routes, or lower-doorway reachability. Current path topology is still baked for the existing Humanoid agent type at roughly 2 m / .35 m.

## Facial semantics and capability honesty

The thirty semantic values cover BlinkBoth and all twenty-nine shared lower-face concepts requested by the audit. Gameplay uses `Supports`, `Capability`, `Diagnostic`, `TrySet`, `Reset` and `ResetAll`; raw names and numeric mesh indices stay inside authoring/binding/controller code. A controller binds one explicit face renderer and resolves exact raw indices once. Finite normalized values are clamped to 0..1 and mapped to Unity weights through an optional normalized min/max range. Unknown, missing and rejected semantics return false without repeated warnings. Changing a renderer's shared mesh requires an explicit rebind; cached indices never target a replacement mesh.

The controller does not copy meshes, manipulate vertices, reconstruct blink interpolation, create facial bones or implement automatic blinking. Independent renderers keep independent weights even when they share one mesh. Multiple supported semantics can remain active simultaneously. Reset affects only supported semantics.

Every binding records the exact licensed raw spelling. In particular:

| Semantic | AdultFemale raw | AdultMale raw | Boy/Girl raw |
| --- | --- | --- | --- |
| MouthShrugLower | mouthShrugtLower (Rejected) | mouthShrugtLower | mothShrugtLower |
| MouthShrugUpper | mouthShrugtUpper (Rejected) | mothShrugtUpper | mothShrugtUpper |
| SmileLeft / Right | mouthSmileLeft / mouthSmileRight | same | same |
| DimpleLeft / Right | mouthDimpleLeft (Rejected) / mouthDimpleRight | mouthDimpleLeft / mouthDimpleRight | same |
| BlinkBoth | BlinkBoth | BlinkBoth | BlinkBoth |

Male's raw mouthLeft/mouthRight ordering also differs; no shared numeric index is assumed. No mesh channels are renamed or normalized.

Adult Female explicitly rejects exactly these six supplied capabilities, as established by FemaleBaldBaseAudit:

| Semantic | Exact rejected channel |
| --- | --- |
| FrownLeft | mouthFrownLeft |
| DimpleLeft | mouthDimpleLeft |
| MouthStretchLeft | mouthStretchLeft |
| MouthShrugLower | mouthShrugtLower |
| MouthShrugUpper | mouthShrugtUpper |
| MouthLowerDownLeft | mouthLowerDownLeft |

All six remain at zero during supported-expression tests and cannot be driven through the API. There is no mirrored replacement or supported claim for a symmetric expression requiring a rejected side. Female supports 23 safe supplied channels plus BlinkBoth; the other three families support all 29 supplied channels plus BlinkBoth. DimpleRight is supported for all four; DimpleLeft is supported only for Male, Boy and Girl.

All four accepted BlinkBoth assets currently contain twenty frames. Girl's position trajectory is specifically nonlinear; its semantic 50% result must follow the stored 50% frame. The integration test independently skins that stored frame through the current bone matrices and compares against Unity's baked 50% renderer result. Girl's maximum error was 0.0001354 mm, while its difference from a linear half-endpoint was 4.50058 mm. Female/Male/Boy matched their stored 50% frames within 0.0002674/0.0002517/0.0001867 mm respectively. No Girl-specific runtime code exists.

All families passed weights 0/25/50/75/100, exact return to neutral, simultaneous SmileLeft/SmileRight + JawOpen + BlinkBoth, and supported Dimple addressability. Default weights remain zero/open; there is no automatic eye closure.

## Hiring, waiting, profession and departure

CandidateAgent delegates canonical waiting to the shared presentation's existing idle/Speed path. It does not create WaitingBody or re-enable a hidden root capsule for canonical characters. Legacy WaitingBody behavior remains unchanged. The explicit `RegisterExistingCandidate` router seam supports isolated supplied roots; the normal Spawn method is unchanged.

CandidateWorldRouter still converts CandidateAgent to EmployeeAgent on the same GameObject. It retains the selected state/indicator and held flag, rebinds the same PersonProfile and presentation, and leaves the visual/Animator/facial controller/profile intact. Destroying the CandidateAgent is deferred by Unity, but it is disabled before the EmployeeAgent takes ownership; only one active agent owns the root. The old component is gone by the next frame.

Profession changes retain identity, family, appearance ID, physical profile, facial binding and visual. No profession-specific prefabs are introduced. Dismissal retains the presentation while the existing root follows its departure lifecycle; roster removal does not own the visual. CharacterPresentation destroys only its owned visual child when its root/owner is destroyed. Shared meshes, materials, catalogue metadata and unrelated simulation registrations are untouched.

## Carry and UI seams

CharacterPresentation implements HeldPersonPoseAdapter directly. HeldPersonPresentation remains the sole held/settling state owner; PersonDragSession still owns manipulation and navigation/collider restoration. The adapter freezes Animator playback speed without replacing its current state, applies the existing held response around the measured family Neck pivot, compensates pivot position for stretch, and restores exact visual local position/rotation/scale and prior Animator speed. Disable/destruction cleanup is idempotent. Canonical people never trigger the capsule-cloning fallback.

PersonDragSession takes carry lift and placement clearance from a resolved profile, and keeps legacy constants otherwise. After a same-root contextual hire it restores the current agent, rather than a cached destroyed CandidateAgent. Invalid contextual actions preserve the held session. Cancellation safely handles disabled navigation. The interaction controller cancels ownership if its held root becomes inactive. Carry controls are unchanged.

The information UI uses CharacterPresentation.InformationAnchorWorld for canonical people and retains root + 1.6 m for legacy people. The People UI itself is unchanged.

Both generic capsule gestures and explicit EmployeeAgent beat initiation are guarded for canonical presentations. Beat timing/completion callbacks remain operational without primitive arms. This is compatibility only: LiveFilmingPlayback still has its separate primitive proxy path and production cameras still have fixed framing assumptions. No claim of filming adoption is made.

## Fallback and local setup

Unassigned/unknown identity, absent catalogue, missing family visual and invalid entries return a bounded diagnostic on the explicit factory result. A new legacy representation uses the legacy mesh and physical dimensions together; assigned identity remains untouched and another family is never substituted. A failed rebuild of a working canonical presentation preserves its visual and physical profile. There is no automatic retry loop, repeated warning stream or background regeneration. Legacy WaitingBody, held capsule behavior and default Studio spawning remain available.

Local setup menus:

1. `SilverScreen > Characters > Runtime V1 > Validate accepted prerequisites`
2. `SilverScreen > Characters > Runtime V1 > Build local visual wrappers`

`CharacterRuntimeReferenceFingerprints.json` records SHA-256 values for accepted reference prefabs and their local mesh/Avatar/animation/material dependencies and import metas. This is metadata, not geometry. The tool checks all four recipes before preparing outputs, then validates all four extracted visuals before writing any generated wrapper. A missing or changed input reports its exact path and the relevant committed family report. It does not overwrite the input, reimport sources, rerun endpoint searches, choose a candidate, repair Female channels or rebake facial art.

Reference inputs:

- `Assets/SilverScreen/Art/Characters/FemaleBlinkPrototype/SS_FemaleBlinkPrototype.prefab` — [FemaleBlinkBothUnityValidation.md](FemaleBlinkBothUnityValidation.md)
- `Assets/SilverScreen/Art/Characters/MaleFoundationPrototype/SS_MaleBlinkPrototype.prefab` — [MaleCanonicalFoundationValidation.md](MaleCanonicalFoundationValidation.md)
- `Assets/SilverScreen/Art/Characters/BoyFoundationPrototype/SS_BoyBlinkPrototype.prefab` — [BoyCanonicalReferenceValidation.md](BoyCanonicalReferenceValidation.md)
- `Assets/SilverScreen/Art/Characters/GirlFoundationPrototype/SS_GirlBlinkPrototype.prefab` — [GirlCanonicalReferenceValidation.md](GirlCanonicalReferenceValidation.md)

The generated wrapper contains the existing CharacterVisualRoot children, native model transform/alignment, one Animator, skeleton transforms, existing SkinnedMeshRenderer/material references and CharacterVisualReference. It has no CandidateAgent, EmployeeAgent, NavMeshAgent, collider, person identity, autonomy, clock, recruitment state or carry session. Meshes, native buffers, BlendShapes, Avatar and controller are shared with the accepted reference assets without editing them. The old reference root's -1 m centre compensation is removed from the wrapper; the new root-local origin is resolved from the instance profile.

Ignored output files under `Assets/SilverScreen/Art/Characters/CharacterRuntimeGenerated/`:

- `AdultFemaleVisual.prefab` and `.meta`
- `AdultMaleVisual.prefab` and `.meta`
- `BoyVisual.prefab` and `.meta`
- `GirlVisual.prefab` and `.meta`
- `Resources.meta`
- `Resources/CharacterRuntimeV1.meta`
- `Resources/CharacterRuntimeV1/LocalCharacterCatalog.asset` and `.meta`

The output folder's sibling `CharacterRuntimeGenerated.meta` is also ignored. Existing output metas are retained on regeneration. No generated licensed geometry or dangling local-prefab GUID is added to committed metadata.

## Validation and evidence

Unity Editor version used: 6000.6.3f1, the installed project environment. Core shared types compiled before the first synthetic gate. The initial gates passed: identity 6, physical profile 11, facial controller 6, lifecycle 4, then local-wrapper generation, then four licensed integration tests (each exercising all four families). Final review added allocation and transactional-hire coverage: the final new-fixture counts are identity 6, physical 11, facial 7, lifecycle 5, integration 4 — 33 passing, no skips.

The expanded fallback test found and fixed a Unity missing-component/null-coalescing bug in factory creation on an empty visual root; explicit Unity null checks now create its MeshFilter and MeshRenderer. The fixture also proves transactional hire rollback/success and cancellation with disabled navigation. Test-authoring corrections used the actual sampled NavMesh height rather than a nominal flat floor, and profile-based Socialize clearance rather than an arbitrary one-metre separation threshold. The final four-family Socialize run had separations 1.0760/1.0138/1.0540/.9978 m against combined radii .4323/.4472/.3536/.3632 m. No legacy test was weakened or edited. Investigation receipts are `fallback-null-investigation.xml` and `social-threshold-investigation.xml`; they are not counted as final passing runs.

The isolated integration fixture builds temporary navigation at a nonzero ground height and uses the shared factory for every family. It restores the prior clean Studio scene after each test. Licensed prerequisites are asserted, not silently skipped. No persistent Studio scene or reference asset was edited.

Fresh local receipts are under `TestResults/CharacterRuntimeV1/`:

- `baseline.json`, `core-compile.json`, `measure.cs`, `measurements.txt`, `reference-fingerprints.txt`, `definitions-rpc.json`, `setup-rpc.json`
- `CharacterPresentationIdentityTests.xml`, `CharacterPhysicalProfileTests.xml`, `CharacterFacialControllerTests.xml`, `CharacterPresentationLifecycleTests.xml`, `CharacterRuntimeIntegrationTests.xml`, plus summaries
- `licensed-face-results.txt`, `licensed-locomotion-results.txt`, `licensed-interaction-results.txt`, `licensed-social-results.txt`
- `visual/<AdultFemale|AdultMale|Boy|Girl>/blink-<0|25|50|75|100>.png`
- `visual/<family>/oblique-<0|50|100>.png`, `simultaneous-smile-jaw-blink.png`, `neutral-full-body.png`, `held-full-body.png`

The front/oblique face and held images were actually inspected. Open, partial and closed states retain the accepted family shapes and documented contour/corner limitations. Plain-grey eyes/materials, closed-seam artifacts and locomotion foot-contact limitations are not repaired or reclassified. Girl ReviewG2 remains visually approved for the canonical-reference/foundation stage only, not final production eyelid art. These images are technical evidence, not final art approval.

Performance review: facial raw lookup occurs only in controller construction/explicit wrapper validation. Per-frame animation uses a cached Speed hash and cached navigation/time references; no scene scan, mesh clone or material instance is made by the new runtime path. Catalogue indexing is cached; no per-frame regeneration/instantiation occurs. Wrapper validation allocates at explicit attachment boundaries, not while setting facial weights. The synthetic hot-path test measured zero managed bytes for 10,000 repeated supported weight updates after warmup. No crowd benchmark is claimed.

## Deferred scope and limits

V1B must explicitly implement production family assignment and actual Studio applicant spawning/adoption, including real hire/reassignment/firing flows, construction-worker integration, full autonomy and mixed-family Socialize in the Studio, plus production fallback decisions. This milestone tests isolated shared compatibility only. Normal recruitment cadence, CandidateGenerator, StudioBootstrap and the default capsule path are unchanged.

Separate later gates remain for construction arrival offsets, filming/playback proxy adoption, production-camera framing, actual stature/proportion authoring and persistence, child-actor gameplay and age progression, Star Maker, wardrobe/hair/accessories, skin/eye materials, eyebrows/eyelashes, locomotion foot contact, automatic/independent blinking, gaze/LookAt/brows, expression AI, acting facial performance, speech/lip sync and Female broken-channel repairs. No environment/town/roads/nature work or save-system rewrite is included.

## Final verification record

Final synthetic/runtime suites: identity 6/6, physical 11/11, facial 7/7, lifecycle 5/5, licensed integration 4/4 (33/33; no skipped prerequisites).

Existing regression results are recorded separately:

| Existing fixture | Passed |
| --- | ---: |
| PersonManipulationRuntimeTests | 4 |
| HeldPersonPresentationTests | 2 |
| PersonInteractionTests | 8 |
| RecruitmentTests | 15 |
| RecruitmentStarterIntakeTests | 16 |
| RecruitmentStarterPhysicalTests | 1 |
| ConstructionV2BWorkforceTests | 6 |
| PersonAutonomySimulationTests | 33 |
| PersonActivitySessionTests | 2 |
| AutonomousActivityOpportunityViewTests | 5 |
| ContextualCarryTargetTests | 6 |
| ContextualCarryStudioTests | 1 |
| SimulationSpeedTests | 5 |
| Stage1LiveIntegrationTests | 8 |
| Stage1LiveRuntimeTests: entrance/navigation | 1 |
| Stage1LiveRuntimeTests: two-scene production | 0 (1 failed) |

The unchanged historical Stage1 runtime fixture requires its older populated developer scenario. The saved scene now starts New Studio and keeps the developer templates inactive, so an initial ordinary run failed before its intended assertions (missing Stage1 and zero actors). The local regression harness selects the existing `_startNewStudio=false` option and activates the preserved `Development Templates (inactive)` root in memory only. Entrance/navigation then passed with five employees, zero measured penetration and minimum spacing .7012311 m. The production case additionally exposed a stale building cache after template activation: CastingOffice was active but its existing router returned BuildingMissing. A temporary ignored Editor hook calls the existing `RefreshBuildings` API on Play Mode entry for this historical scenario. Neither the historical assertions nor the saved scene/bake are changed. The harness restores the saved New Studio scene after each case. Initial prerequisite-failure receipts remain available; they are not counted as passing runs.

**Original handoff's unresolved gate (subsequently dispositioned below):** after those setup prerequisites were established, `Stage1LiveRuntimeTests.TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts` reached filming but failed the unchanged actor local-Z assertion at line 308 (required 1..2 m; observed .990804672 m, then .963825226 m on one repeat). The original total was **146 passed / 147 tests, 1 failed, 0 skipped**: all 33 new tests and 113 existing regressions passed. The failing production test uses legacy capsule employees, not canonical presentation. Its actor-arrival assertion is unchanged, as are the saved scene and NavMesh. At that handoff a controlled checkpoint-baseline comparison had not yet been performed. No navigation tuning, actor repositioning, historical test relaxation or filming-system change was made to force success. V1A-2 was therefore reported as **NEEDS FURTHER WORK** pending resolution or explicit disposition. The following baseline investigation now establishes the failure's provenance without deleting these earlier receipts.

The temporary Editor hook was removed from Assets after testing and archived as `TestResults/CharacterRuntimeV1/Stage1RegressionSetup.cs`. The ignored `run_populated_stage1.py` harness documents the scoped scenario. To reproduce its production run, temporarily copy that hook into an Editor subfolder of the ignored generated runtime root, refresh Unity, run the harness, then remove the temporary hook and restore the saved Studio scene. The initial scene/prerequisite failures and the first arrival-boundary failure are retained separately; final XML receipts are `Final-Stage1RuntimeRoutes.xml` and `Final-Stage1RuntimeProduction.xml`.

RecruitmentStarterPhysicalTests wrote two new review files to `ArtReview/RecruitmentC11`; those generated outputs were moved intact to the ignored `TestResults/CharacterRuntimeV1/recruitment-starter-evidence/` directory. They are test evidence, not new committed art.

Final verification:

- Unity compilation succeeded after removal of the temporary historical-fixture hook. The final Console snapshot contains 0 errors, 39 warnings and 1 informational setup message. All 39 warnings originate in unchanged older test files (mostly obsolete Unity object-search APIs); none reference a new foundation file. This final post-compilation Console snapshot does not erase or supersede the failed historical test XML.
- `final-editor-state.json` confirms Unity 6000.6.3f1, Edit Mode, no compilation failure, exactly one clean `Assets/Scenes/Studio.unity`, New Studio enabled, development templates inactive, and zero canonical CharacterPresentation objects in Studio. Production recruitment remains on its capsule path.
- A second `BuildLocalWrappers` run succeeded against the recorded accepted fingerprints. All 13 generated files, including prefab/catalogue/folder metas, remained byte-for-byte identical (`regeneration-verification.json`); GUIDs were preserved. The temporary regression hook is absent from Assets.
- `final-verification.json` checks all 85 purchased source files by SHA-256 and modification timestamp: no differences. All 1,844 current protected reference/evidence files, 4,535 historical protected files and 627 Girl checkpoint evidence files match their recorded hashes (these sets overlap). Accepted geometry, topology, native buffers, BlendShapes, BlinkBoth, skeletons, Avatars and evidence were not modified.
- The architecture audit is byte-exact, SHA-256 `619f89ba49d362406aae23304fe5934c761374de54aa35a30040b42725cd6316`.
- Starting and ending branch: `master`. Starting HEAD, ending HEAD and fetched `origin/master`: `ba63ced6889d4851fadca8f923c8b7239a3925a7`. **HEAD did not move.** No staged files, commits, pushes, resets, stashes, branch/worktree changes or history rewrites.
- Final working tree: 7 modified tracked files, 44 untracked files (43 created here plus the preserved audit), and 13 ignored generated runtime files listed above. Other ignored test receipts remain under `TestResults/CharacterRuntimeV1/`.
- `git diff --check`: passed (exit 0). A separate whitespace scan of all new text/metadata files also passed. New Unity metadata whitespace was normalized without changing GUIDs; existing metadata was untouched.

Original handoff outcome: **CHARACTER RUNTIME V1A FOUNDATION NEEDS FURTHER WORK**, because the historical two-scene production regression was not yet dispositioned. The investigation below supersedes that readiness assessment. No commit or push has been authorized and V1B has not begun.

## Stage1 regression disposition — checkpoint baseline investigation, 2026-10-03

**CASE A — BASELINE ALSO FAILS. PRE-EXISTING / BASELINE-REPRODUCED.** The exact Girl checkpoint `ba63ced6889d4851fadca8f923c8b7239a3925a7` reproduced the same historical local-Z assertion in all three runs. The current uncommitted V1A-2 snapshot also reproduced it in all three runs. V1A-2 did not introduce this failure. No production code, historical test, scene, target, NavMesh, family definition, mesh, facial data or wrapper was changed to obtain this result.

### Comparison method and controls

The active checkout stayed on master at the Girl checkpoint, with no staged files. A preservation manifest records all seven modified and 44 untracked files, the thirteen generated wrapper/catalogue files and their hashes/metas, and all earlier runtime evidence. Original working files and earlier Stage1 XML receipts were also copied into the ignored investigation evidence directory.

Two independent Unity project snapshots were created beneath `Temp/CharacterRuntimeStage1Comparison/`. `baseline` was exported with `git archive` from the exact checkpoint's Assets, Packages and ProjectSettings; it contains no V1A-2 production code. `current` began as an identical independent copy and received the six modified Unity source files and the new foundation files, byte-matched to the working tree. The `.gitignore` and documentation changes have no runtime role. No branch/worktree switch, reset, stash, destructive checkout or temporary commit was used. The active main Unity project was never closed, reloaded or modified by the comparison.

Both snapshots used the same installed `6000.6.3f1 (45d8eee7de74)` executable, Windows graphics-enabled batch mode, package versions, saved Studio, NavMesh and unchanged historical test. The launch requested 1600×1000; a later camera diagnostic established that this Editor's batch mode actually uses 640×480. Both comparison snapshots used the same batch setup. Runs were serial, three per snapshot, using one identical temporary Editor harness (`Stage1BaselineStudy.cs`, SHA-256 `22be4103fb3325d053a37336ca9ec723086b9822cfa695856c8c98e1c4882864`). Baseline imported assets normally; after its process exited, independent copies of its import cache were reused for current, whose changed/new source was then recompiled. No live main-project Library files were written or hardlinked.

For every run the harness opened the saved Studio, selected `_startNewStudio=false` in memory, activated the preserved `Development Templates (inactive)` root and called the existing `StudioWorldRouter.RefreshBuildings` API on Play Mode entry. This is the same historical scenario setup used at the original handoff. The test body and assertions were untouched. The probe only read public actor/navigation data and the existing coordinator field; it did not invoke the coordinator's lazy initializer or change random seeds, time, frame rate, transforms, dimensions, routing or target poses. Domain state-transition samples captured the exact assertion-time Z values; route/phase and teardown samples provide surrounding context. Each run restored the saved New Studio scene.

Hash verification found no source, Studio scene, NavMesh, test or serialized-setting value changes in either comparison. Unity normalized line endings in several isolated ProjectSettings files; their values are unchanged. All 330 checkpoint C# files and all 345 current-snapshot C# files matched their expected contents. The main project's settings and assets remained untouched.

### Results and navigation evidence

The failed assertion belongs to **Jack Sterling**, the first actor checked. Clara Vance's position was measured, but the test aborts on Jack before reaching her assertion. Values below are at the first filming transition, matching the failed XML value exactly for Jack; they are not presented as fully settled poses.

| Snapshot / run | Jack assertion Z (m) | Clara observed Z (m) | Jack feet-to-authored-mark distance (m) | Result |
| --- | ---: | ---: | ---: | --- |
| Baseline 1 | .968353271 | .980192184 | .532804966 | Same Z assertion failed |
| Baseline 2 | .954460144 | 1.300901413 | .547286212 | Same Z assertion failed |
| Baseline 3 | .951768875 | 1.265939713 | .549772322 | Same Z assertion failed |
| Current 1 | .951520920 | 1.261610031 | .549995303 | Same Z assertion failed |
| Current 2 | .951799393 | 1.267646790 | .549720168 | Same Z assertion failed |
| Current 3 | .975564957 | .985008240 | .525593638 | Same Z assertion failed |

Jack's observed assertion ranges overlap: baseline .951768875–.968353271 m, current .951520920–.975564957 m. Means are .958194097 m and .959628423 m. Three runs per tree establish repeated baseline reproduction, not statistical equivalence or a crowd/performance benchmark. Numeric positions vary, but no pass/fail nondeterminism was observed in these six runs. This is Case A, not evidence that V1A introduced a flaky failure.

Identical relevant state across the six runs:

- Jack's initial root world position: (-3, 1.025, 4.5). All actors were legacy capsules; no CharacterPresentation component was present.
- Radius .35 m; height 2 m; base offset 1 m; stoppingDistance .3 m; autoBraking true; acceleration 14 m/s²; speed 3.5 m/s; angularSpeed 400°/s; HighQualityObstacleAvoidance; areaMask -1.
- Jack's authored mark: Stage1-local (-1.8, .08, 1.5), world (20.0111351, .08, -16.3299999). The last actual NavMesh path destination had the same X/Z and sampled floor Y=.1. Clara's authored local mark was (0, .08, 1.5).
- At the assertion: on NavMesh, pathPending=false, pathStatus=PathComplete, hasPath=false, remainingDistance=0 after ResetPath. Employee state and production phase were Filming, take status Recording. The last commanded destination remains recorded separately from NavMeshAgent.destination, which has already become the current feet point after path reset.
- Reported Jack velocity at filming entry was approximately 3.10–3.13 m/s, rather than zero. His later pre-teardown Z values were 1.192955017 / 1.233873367 / 1.120538712 for baseline and 1.120182037 / 1.145090103 / 1.143630981 for current. The historical assertion observes a transition before final settling, not a stable wrong-height/root-offset endpoint.

Every run's exact world/local root, target, current and previous destination, velocity, remaining distance, navigation configuration, person ID, frame/time and activity state is retained in `run-N-navigation.json` and the consolidated `comparison-results.json`.

### Mechanism and disposition

The checkpoint's existing `EmployeeAgent.CheckExplicitArrival` accepts arrival when feet-to-task distance is at most `stoppingDistance + .25`, which is **.55 m** here. `ForceCompleteArrival` resets the path and calls the existing arrival callback. Once participants satisfy readiness, MovieProductionCoordinator enters Filming immediately. The authored mark is Z=1.5 m, so that accepted arrival envelope can include positions near Z=.95 m. The historical test instead requires Z>=1 m at that same phase transition. All six measured distances satisfy the existing arrival predicate while violating that tighter rectangular assertion.

This is an existing mismatch between arrival/phase-transition timing and the test's positional contract. Small frame/navigation variation changes the exact number, but the failure mechanism and configuration are present in the checkpoint. No root/base-offset or canonical-profile change is implicated. The threshold code, readiness path, targets and test are unchanged between snapshots. Conditional V1A regression isolation/fixing was therefore not entered.

Separate future Stage1 maintenance should decide the intended arrival/settling contract and then validate it with repeated runs. It should also cover the pre-existing teardown-order exception reproduced in baseline runs 2/3 and current runs 1/2: MovieProductionDriver.OnDestroy calls MovieProductionCoordinator.Dispose, which releases an employee after PersonAutonomySimulation has already been disposed. This exception occurs after the Z assertion and does not replace its evidence. Neither issue was repaired or suppressed here.

The historical production test is **still failing**, now explicitly dispositioned as **PRE-EXISTING / BASELINE-REPRODUCED**. Earlier failed receipts remain intact. There is no production fix and therefore no post-fix claim. The only reviewable main-tree file changed during this investigation is this foundation report; temporary comparison/probe tooling and receipts are ignored local evidence.

### Foundation and legacy revalidation

All **33 foundation tests passed again**, with no skipped licensed prerequisites: identity 6, physical profile 11, facial controller 7, presentation lifecycle 5 and licensed integration 4. Revalidation consumed independent byte-matched copies of the already generated accepted references/wrappers; no wrappers or facial art were regenerated. All four family definitions and canonical profiles remain valid. Female's six rejected semantics remain undriven. Girl's existing twenty-frame BlinkBoth remains intact: its 50% state matches the reference within 1.35352778e-7 m and differs from linear endpoint interpolation by .00450057862 m. All four families passed BlinkBoth 0/25/50/75/100, shared-asset preservation and Smile/JawOpen coexistence through the common API.

The complete 147-test selection was rerun across the isolated current snapshot. Final per-test results are **146 passed, 1 failed, 0 skipped**: 33 foundation tests plus 113 passing legacy regressions, with the one historical production test remaining baseline-reproduced. The three current production repetitions count as one unique test in that total. Every legacy fixture in the earlier validation table was rerun. The Stage1 entrance/navigation test passed again: five employees traversed the personnel door, maximum measured penetration 0, minimum employee spacing .7005606 m. No historical assertion was edited.

Two isolated-environment prerequisites were resolved without changing test or production source:

- `RecruitmentStarterPhysicalTests` first failed because its assumed `ArtReview/RecruitmentC11` output directory did not exist in the exported project. Creating that evidence directory in the snapshot allowed the unchanged test to pass. The failed XML and log are retained.
- Two `ContextualCarryTargetTests` first failed in batch mode. A temporary camera diagnostic confirmed that batch mode clamps their requested 1000×1000 camera to 640×480, giving approximately 48 pixels per world metre instead of 100. These are pixel-distance assertions. A hidden non-batch Editor preserved the requested camera dimensions and the unchanged fixture passed 6/6. ContextualCarryStudio, SimulationSpeed and Stage1LiveIntegration also passed there. The failed batch receipts and both projection diagnostics remain available. No seeds, time scale, assertion thresholds or production targeting values were changed.

The temporary suite runner also retained an initial startup receipt for its own fixture-list CRLF parsing error, which occurred before tests ran and was corrected only in ignored snapshot tooling. These prerequisite/runner attempts are disclosed separately from the final unique-test total; none is presented as a pass.

### Final preservation and checkpoint decision

- Compilation succeeded in the isolated Editors and remains successful in the main Unity 6000.6.3f1 Editor. The main Console was inspected without clearing it: **0 errors, 39 pre-existing warnings, 1 informational message**, with message/type contents identical to the investigation's initial snapshot. This does not conceal the separately retained production-test failures or baseline-reproduced teardown exception.
- The main Editor still has exactly one clean saved `Assets/Scenes/Studio.unity`, in Edit Mode, New Studio enabled, development templates inactive and zero canonical CharacterPresentation objects. All investigation-owned Unity processes exited; the user's existing Editors were left running. The investigation never needed to reload the main scene.
- All **85 purchased source files** match their hashes and modification timestamps. All **1,844 current protected reference/evidence files**, **4,535 historical protected files**, **627 Girl checkpoint evidence files** and **259 prior Character Runtime evidence files** match their recorded hashes; these sets overlap. Original failed Stage1 receipts are preserved. The shared temporary Stage1 evidence folder was archived before the entrance test rewrote its own output.
- All **13 generated runtime wrapper/catalogue files and metas** remain byte-exact, preserving their GUIDs and accepted source references. All existing V1A-2 implementation files and metadata remain byte-exact relative to the investigation start. Only this report changed among the reviewable main-tree files.
- The architecture audit is unchanged, SHA-256 `619f89ba49d362406aae23304fe5934c761374de54aa35a30040b42725cd6316`.
- Starting and ending branch: **master**. Starting HEAD, ending HEAD and fetched origin/master: **`ba63ced6889d4851fadca8f923c8b7239a3925a7`**. HEAD did not move. No staged files. The exact working-tree status remains the seven modified tracked files and 44 untracked files listed below. No commits, pushes, resets, stashes or history changes were performed.
- `git diff --check` passed, exit 0; new text/metadata whitespace inspection also passed. Git emitted its existing fsmonitor empty-token/line-ending advisories, not whitespace errors.

**V1A is ready for a separately authorized checkpoint.** The Stage1 failure is proven pre-existing and is now dispositioned, not repaired or reported green. No V1B or Appearance Foundation work has begun. No commit or push is authorized by this investigation.

### Investigation evidence

All new evidence and temporary tooling are ignored under `TestResults/CharacterRuntimeV1/Stage1BaselineInvestigation/`:

- `initial-state.json`, `PreservedWorkingFiles/`, `PriorStage1Receipts/`: starting repository, source-file, wrapper/meta and prior-receipt preservation.
- `snapshot-manifest.json`, `snapshot-verification.json`, `Stage1BaselineStudy.cs`, `hypotheses.json`: exact exports, current overlay, shared harness, unchanged sources/settings and hypothesis outcomes.
- `baseline/run-1.xml` through `run-3.xml`, and `current/run-1.xml` through `run-3.xml`: all six failed production receipts. Each run has a matching `run-N-navigation.json`, `run-N-historical-runtime.txt`, started/result receipt; each snapshot retains `Editor.log`, launch/process metadata and completion receipt.
- `comparison-results.json`: consolidated assertion values and full per-actor navigation/state samples.
- `foundation-revalidation/`, `foundation-revalidation-remaining/`, `foundation-revalidation-editor/`: unchanged fixture results and logs; `validation-summary.json` lists each final XML and all retained setup attempts.
- `camera-projection-diagnostic/` and `foundation-revalidation-editor/camera-projection.txt`: batch/non-batch projection evidence and unchanged-test outcomes.
- `current-entrance/run-1.xml`, `current-entrance/run-1-historical-runtime.txt`: passing entrance/navigation result and movement/contact evidence.
- `SharedStage1EvidenceBeforeEntranceRerun/`: preserved earlier shared temporary Stage1 output.
- `main-initial-editor-state.json`, `main-initial-console.json`, `main-final-editor-state.json`, `main-final-console.json`, `final-preservation.json`: main Editor, Console, repository and source/reference/wrapper preservation checks.

The independent snapshots remain under ignored `Temp/CharacterRuntimeStage1Comparison/baseline/` and `current/` for reproduction. Revalidation input-copy hashes are in `revalidation-inputs.json`; newly generated licensed integration receipts/images live only in the current snapshot's ignored `TestResults/CharacterRuntimeV1/`. Earlier accepted family evidence was not regenerated or overwritten. These technical receipts do not grant additional facial-art approval.

## Exact review inventory

The following inventory includes the preserved, pre-existing untracked audit. Every other untracked file was created for this milestone. Existing Unity assets and their metas were not edited. No historical test file was changed.

```text
 M .gitignore
 M Assets/Scripts/Domain/PersonProfile.cs
 M Assets/Scripts/Presentation/Employees/EmployeeAgent.cs
 M Assets/Scripts/Presentation/Employees/EmployeeNavigationProfile.cs
 M Assets/Scripts/Presentation/Interaction/PersonDragSession.cs
 M Assets/Scripts/Presentation/Interaction/PersonInteractionController.cs
 M Assets/Scripts/Presentation/Recruitment/RecruitmentPresentation.cs
?? Assets/Editor/Characters/CharacterRuntimeReferenceFingerprints.json
?? Assets/Editor/Characters/CharacterRuntimeReferenceFingerprints.json.meta
?? Assets/Editor/Characters/CharacterRuntimeSetupTools.cs
?? Assets/Editor/Characters/CharacterRuntimeSetupTools.cs.meta
?? Assets/Scripts/Domain/Characters/PersonPresentationIdentity.cs
?? Assets/Scripts/Domain/Characters/PersonPresentationIdentity.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterFacialBinding.cs
?? Assets/Scripts/Presentation/Characters/CharacterFacialBinding.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterFacialController.cs
?? Assets/Scripts/Presentation/Characters/CharacterFacialController.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterFamilyCatalog.cs
?? Assets/Scripts/Presentation/Characters/CharacterFamilyCatalog.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterFamilyDefinition.cs
?? Assets/Scripts/Presentation/Characters/CharacterFamilyDefinition.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterPhysicalProfile.cs
?? Assets/Scripts/Presentation/Characters/CharacterPhysicalProfile.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterPresentation.cs
?? Assets/Scripts/Presentation/Characters/CharacterPresentation.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterPresentationFactory.cs
?? Assets/Scripts/Presentation/Characters/CharacterPresentationFactory.cs.meta
?? Assets/Scripts/Presentation/Characters/CharacterVisualReference.cs
?? Assets/Scripts/Presentation/Characters/CharacterVisualReference.cs.meta
?? Assets/SilverScreen/Characters.meta
?? Assets/SilverScreen/Characters/Definitions.meta
?? Assets/SilverScreen/Characters/Definitions/AdultFemale.asset
?? Assets/SilverScreen/Characters/Definitions/AdultFemale.asset.meta
?? Assets/SilverScreen/Characters/Definitions/AdultMale.asset
?? Assets/SilverScreen/Characters/Definitions/AdultMale.asset.meta
?? Assets/SilverScreen/Characters/Definitions/Boy.asset
?? Assets/SilverScreen/Characters/Definitions/Boy.asset.meta
?? Assets/SilverScreen/Characters/Definitions/Girl.asset
?? Assets/SilverScreen/Characters/Definitions/Girl.asset.meta
?? Assets/Tests/Editor/CharacterFacialControllerTests.cs
?? Assets/Tests/Editor/CharacterFacialControllerTests.cs.meta
?? Assets/Tests/Editor/CharacterPhysicalProfileTests.cs
?? Assets/Tests/Editor/CharacterPhysicalProfileTests.cs.meta
?? Assets/Tests/Editor/CharacterPresentationIdentityTests.cs
?? Assets/Tests/Editor/CharacterPresentationIdentityTests.cs.meta
?? Assets/Tests/Editor/CharacterPresentationLifecycleTests.cs
?? Assets/Tests/Editor/CharacterPresentationLifecycleTests.cs.meta
?? Assets/Tests/Editor/CharacterRuntimeIntegrationTests.cs
?? Assets/Tests/Editor/CharacterRuntimeIntegrationTests.cs.meta
?? Docs/CharacterRuntimeV1ArchitectureAudit.md
?? Docs/CharacterRuntimeV1Foundation.md
```
