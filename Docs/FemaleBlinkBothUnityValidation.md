# Adult Female BlinkBoth — Unity validation

**ReviewCandidate90 passes the isolated Unity development gate.** The retained bald native mesh now has one additional `BlinkBoth` capability, and its rendered motion, body animation and requested person regressions passed. This report records technical validation under the user's explicit visual acceptance. It is not final art approval, all-angle certification or finished Star Maker compatibility.

The user's later approval supersedes the earlier recommendation to continue corrective authoring for the purpose of entering this milestone. The [corrective-authoring report](<C:/Repos/Unity/Silver Screen/Docs/FemaleBlinkCorrectiveAuthoring.md>) and all rejected candidates remain unchanged. Its observations have not been withdrawn. No further sculpting, source calibration or transfer redesign was performed.

## Repository and environment

| Item | Result |
|---|---|
| Branch | `master`, throughout |
| Starting HEAD | `2d12468ec2768c6b9ab014afee6912e53494d69c` |
| Fetched `origin/master` before edits | `2d12468ec2768c6b9ab014afee6912e53494d69c` |
| Ending HEAD | `2d12468ec2768c6b9ab014afee6912e53494d69c` |
| History | HEAD unchanged; no commit, push, branch switch, new branch, new worktree, stash or reset |
| Unity | Actual installed/editor version **6000.6.3f1**, URP **17.6.0**, `PC_RPAsset`, Linear colour, Windows Editor / StandaloneWindows64 target |
| Editor state | Began and ended out of Play Mode, one clean `Assets/Scenes/Studio.unity` scene |

The installed official Unity AI relay was used for Editor operations. The initial sandbox connection could not access its named pipe; the permitted local connection succeeded. No MCP package or project setting was added or changed.

## Candidate construction and preservation

The sole approved input is [ReviewCandidate90/FemaleBaldBlinkCorrective.blend](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/FemaleBaldBlinkCorrective.blend>), SHA-256 `559f941d99b21848e680085f941b621727fa292bd248dfe9c98f58468ba8734b`. The exporter pins this hash and reads the exact `FemaleBaldBlinkCorrective_Experimental` object. Evaluated neutral and closed positions match the previously approved arrays exactly. Blender does not save the input.

The [new Editor builder](<C:/Repos/Unity/Silver Screen/Assets/Editor/Characters/FemaleBlinkPrototypeTools.cs>) clones the existing retained-native bald mesh and bald prefab. It adds only `BlinkBoth` and changes the cloned renderer's mesh reference. The Avatar, controller, bones, hierarchy, materials and person components come from the existing bald prototype. There is no FBX reimport or geometry recalculation of the baseline.

Generated licensed assets:

- [SS_FemaleBlinkPrototype.prefab](<C:/Repos/Unity/Silver Screen/Assets/SilverScreen/Art/Characters/FemaleBlinkPrototype/SS_FemaleBlinkPrototype.prefab>)
- [AdultFemaleBald_BlinkBoth.asset](<C:/Repos/Unity/Silver Screen/Assets/SilverScreen/Art/Characters/FemaleBlinkPrototype/AdultFemaleBald_BlinkBoth.asset>)

Both assets, their `.meta` files and the generated directory `.meta` remain ignored. The original female, bald comparison, rejected hybrid and production presentation assets were not replaced.

| Canonical data | Result |
|---|---|
| Humanoid | Valid, human; exact same Avatar and locomotion controller references |
| Bones | **51**; all bone paths, local positions, rotations and scales exact |
| Mesh | **19,005 Unity vertices**, **32,512 triangles**, one submesh; exact native positions and triangle indices |
| Skinning | Every influence count, weight, bone index and bind pose exact |
| UVs / normals / tangents / colours | All native arrays and vertex attribute layouts exact at neutral |
| Scalp / eyebrows / material layout | Existing complete bald mesh, body geometry and material reference preserved |
| Existing shapes | All **29** names, frame counts, frame weights and position/normal/tangent deltas exact |
| Final shapes | **30**: the original 29 plus `BlinkBoth` |

The six known-invalid zero-frame-weight lower-face channels remain intact and were never driven to a nonzero value. They were not repaired. `ResetShapes` only returns channels to zero.

All 16,497 Blender vertices are accounted for by the native mesh's UV/corner splits. UV correspondence is exact. The original native mesh differs from the Blender neutral by at most **0.01055708 mm**; that inherited offset is retained rather than changing Basis.

## Endpoint and shading data

One linear displacement from the approved neutral to closed endpoint supplies every position sample. Blender evaluates the approved shape at 5% intervals to supply surface-normal samples. The new Unity shape has **20 frames**, weights 5 through 100, solely to represent that changing normal field; position motion remains the same linear BlinkBoth delta. No additional shape names or facial bones were introduced.

For each sample, the shortest rotation from the Blender neutral corner normal to its evaluated deformed normal is applied to the native normal and tangent. This retains the native shading offset and leaves unchanged regions exactly native. It avoids recalculating the 29 original shapes. Unity interpolates between these frames.

| Check | Measured result | Acceptance bound |
|---|---:|---:|
| Absolute model-space position agreement with Blender, including inherited offset | **0.01055708 mm maximum**, including 100% | 0.02 mm |
| Evaluated source/native neutral normal difference, Python double precision | **0.024341° maximum** | 0.1° |
| Unity sampled normal/reference angular difference | **0.019783° maximum** | 0.1° |
| Change in native tangent-to-normal relationship | **less than 0.00000018** dot-product error | 0.00001 |
| Independently skinned endpoint delta versus runtime `BakeMesh`, across Idle/Walk/Run | **0.000536483 mm maximum** | 0.002 mm |
| Blink displacement of oral and eyeball geometry islands | **0** | Exact zero |
| Neutral restoration in a fixed body pose | Positions and normals **exact** | Exact equality |

The positional bound is justified by the measured original native/Blender offset; it does not conceal a new transfer error. The separate runtime comparison computes the expected delta from the current bone matrices, bind poses and every skin influence.

Native tangents on some existing UV-degenerate oral islands are not perpendicular to their normals (maximum absolute dot about 0.999652). An initial test assumed global orthogonality and failed on this pre-existing condition. The final test verifies that the native relationship is retained, including those untouched islands. No original tangent was repaired. Transported tangents are finite; this milestone does not certify UV-parametric normal-map shading or future skin/eye materials.

## Runtime weights and visual motion

| BlinkBoth weight | Runtime result |
|---:|---|
| 0 | Finite; exact neutral mesh data; appearance matches bald baseline |
| 25 | Finite, progressively closing; no new mouth/jaw/body deformation |
| 50 | Finite, readable half blink; smooth lid surface |
| 75 | Finite, narrow opening consistent with an intermediate blink |
| 100 | Both eyes read closed in front and oblique; approved endpoint and shading correspondence pass |
| Restored 0 | Exact fixed-pose vertex/normal restoration; identical rendered appearance |

Baseline, candidate-neutral and restored-neutral captures have **zero differing pixels** in each of the face, eye-close-up and oblique comparisons. This also validates native scalp/head shading at neutral, beyond vertex parity alone.

The actual URP motion was inspected through sequential captured frames, including the closing/contact/reopening transition and individual close-ups. No new obvious corner pop, sudden seam change, lid collapse, severe shading discontinuity, closed-endpoint eyeball opening or unrelated body distortion was observed in these views. Expected visible eye exposure decreases through closing and returns through opening. The accepted corner contour and inherited medial/cheek movement remain visible under the stronger Unity technical lighting.

- **Slow sweep:** 49 frames at 30 fps, 1.6 seconds of motion. A smooth curve explicitly passes `0 → 25 → 50 → 75 → 100 → 75 → 50 → 25 → 0` at 0.2-second key intervals.
- **Normal-speed example:** 61 frames at 60 fps, with neutral lead/trail; 100 ms closing, 40 ms closed hold and 160 ms reopening. This is one deterministic demonstration, with no automatic blink scheduler.

Captures advance an actual engine frame after each weight change so the skinned GPU buffers update. The body pose is held for static comparisons. The initial synchronous capture incorrectly reused an open GPU image despite changing CPU geometry; it was rejected and archived in `InitialCaptureWithoutFrameAdvance`. Two intermediate harness failures concerned comparison-mesh rebinding/initialization and are retained separately. The final captures were produced after renderer initialization, passed the runtime test and were visually inspected. These are capture-harness corrections, not changes to the approved deformation.

The videos reconstruct the recorded intended timestamps from deterministic Unity frames. Capture wall-clock speed is not a real-time performance benchmark. GIFs include extra neutral pauses for looping; the MP4s, PNGs and timeline CSV preserve the primary timing evidence.

## Humanoid, lower face and person regressions

| Check | Result |
|---|---|
| Idle | Passed four sampled phases; body deforms, endpoint skinning agrees, BlinkBoth is retained |
| Walk | Passed four sampled phases and real NavMesh walking/arrival; BlinkBoth is retained |
| Run | Passed four sampled phases and real navigation-driven Run state; BlinkBoth is retained |
| Blink during animation | Weights 25/50/75/100 remain finite on sampled animated poses; an advancing Animator preserves weight 100; live navigation also retains it |
| Smile + BlinkBoth | Valid `mouthSmileLeft` + `mouthSmileRight` at 100, with BlinkBoth 100: finite, visually closed eyes, additive agreement and exact neutral restoration |
| JawOpen + BlinkBoth | Valid `jawOpen` at 100, with BlinkBoth 100: same checks passed |
| EmployeeAgent / NavMesh | Existing task assignment, Walk, arrival callback, Idle return and Run passed |
| Selection / carry / context | Real collider hover/click selects the same EmployeeAgent identity; hold pose, invalid/valid drops, Practice context action and Escape cancel passed with BlinkBoth 100 |
| Socialize | Two actual candidate prefabs reached an active session at separate locations; root separation **1.128485 m**; BlinkBoth retained |

The Smile/JawOpen checks ran only after the BlinkBoth data and visual-motion gate. Their maximum additive residual was **1.879 × 10⁻⁹ in baked mesh-local units**. Captures show both deformations coexisting, with no new visible eye opening or double deformation. The supplied lower-face appearance is otherwise unchanged. The inspection panel UI itself was not separately exercised; selection identity/routing was.

The known Walk/Run ground-contact problem remains separate and was not fixed. No production person code, scene, recruitment presentation or spawning integration was changed.

## Technical evidence

All images are grey **technical validation**, not final presentation or art approval.

| Evidence | Location |
|---|---|
| Open / half / closed comparison | [open-half-closed.png](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/Review/open-half-closed.png>) |
| Slow animated preview | [sweep.mp4](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/Review/sweep.mp4>) · [sweep.gif](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/Review/sweep.gif>) |
| Normal-speed preview | [normal-speed.mp4](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/Review/normal-speed.mp4>) · [normal-speed.gif](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/Review/normal-speed.gif>) |
| Face / eye / oblique, all five weights and restored | [stills directory](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/stills>) |
| Native comparison captures | [baseline directory](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/baseline>) |
| Deterministic motion frames | [sweep](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/sweep>) · [normal-speed](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/normal-speed>) |
| Body-animation and lower-face captures | [body](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/body>) · [coexist](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/coexist>) |
| Timing / pixel comparisons | [animation-timeline.csv](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/animation-timeline.csv>) · [pixel-comparison.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/pixel-comparison.json>) |
| Native/source parity and runtime results | [native-data-validation.txt](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/native-data-validation.txt>) · [runtime-motion-validation.txt](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/runtime-motion-validation.txt>) |
| Visual decision and input mapping | [visual-motion-review.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/visual-motion-review.json>) · [mapping.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/mapping.json>) |
| Exact generated/evidence file list, sizes and SHA-256 | [evidence-manifest.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/evidence-manifest.json>) |

## Limitations retained

The earlier Blender report remains the authority for its geometric diagnostics. ReviewCandidate90 retains the angular/raised extreme-close-up outer-corner crease and pinched inner termination. The expanded 714-triangle diagnostic recorded **34 crossing pairs touching partly exterior triangles and 46 between hidden triangles**, with **zero tested visible intersection locations** at the two established cameras. Hidden samples reached approximately **−2.642 / −3.180 mm** eye-plane distance; two hidden triangles rotated over 120°, maximum 144.874°. The original narrower exterior test's zero crossings must not be generalized to the whole eye region.

The inherited complete source blink also moves the medial/upper-nose region up to **4.463457 mm** and checked visible lower cheek up to **0.991719 mm**. The corrective did not add movement to those measured sets. None of this has been erased by the user's development approval or by passing Unity tests.

No full contact re-audit, all-angle or every-subframe geometric certification, normal-mapped skin material approval, standalone player build, crowd benchmark or final anatomical art approval is claimed. No new independent blinks, gaze, brows, corrective sculpt, broken lower-face repair, hairstyle system or next milestone was started. Male, Female, Boy and Girl remain separate families; child hierarchy/export validation remains a prior follow-up.

## Tests, compilation and memory

Six focused tests passed, none skipped:

| Run | Pass / fail | XML |
|---|---:|---|
| Native construction, preservation, endpoint and shading data | 1 / 0 | [data-tests.xml](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/data-tests.xml>) |
| Runtime weights, neutral return, deterministic capture, Idle/Walk/Run and skinning parity | 1 / 0 | [motion-tests.xml](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/motion-tests.xml>) |
| Valid lower-face combinations | 1 / 0 | [coexist-tests.xml](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/coexist-tests.xml>) |
| Navigation, real selection/carry/context and Socialize | 3 / 0 | [person-tests.xml](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/person-tests.xml>) |

Unity compiled the added Editor code successfully; final `isCompiling=false`, `scriptCompilationFailed=false`. The initial console had **36 existing warnings and 0 errors**, recorded before changes. The final available console has **0 errors and 0 warnings** after the test runner lifecycle; this does not claim those unrelated source warnings were fixed. Both console snapshots and the initial failed harness runs remain in the evidence folder. Python tools passed syntax checks and their export/mapping/packaging/preservation operations were executed.

Final memory probe: **22,522,908 bytes** for the shared candidate mesh versus **21,719,164 bytes** for the loaded native bald baseline, an increase of **803,744 bytes**. This is Unity's in-Editor runtime memory estimate, not a per-character GPU/crowd measurement. There are **51 bones**, **30 BlendShapes**, **one active SkinnedMeshRenderer**, plus the existing inactive selection-indicator MeshRenderer (**two renderer components total**).

## Reproduction

From the repository root, use Blender 5.0.1 and the existing locally licensed baseline/evidence:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/export_female_blink_unity.py -- --candidate TestResults/FemaleBlinkCorrective/ReviewCandidate90/FemaleBaldBlinkCorrective.blend --output TestResults/FemaleBlinkUnity
# Unity menu: SilverScreen > Characters > Female BlinkBoth > 1 Export native reference
python -B Docs/CharacterBaseAuditTools/prepare_female_blink_unity.py
# Unity menu: SilverScreen > Characters > Female BlinkBoth > 2 Build isolated approved candidate
```

Run `FemaleBlinkPrototypeTests.NativeBaselineAndApprovedEndpointArePreserved`, then `RuntimeWeightsMotionAndHumanoid` through the Unity Test Runner. Inspect the actual rendered motion before running `RuntimeValidLowerFaceCoexists`. `FemaleBlinkPersonPrototypeTests` provides the three isolated person regressions. Tests require a clean single scene and restore it; they refuse to discard dirty/additive scenes. The saved XML runs used `FemaleBlinkValidation.Run` through the installed relay.

```powershell
python -B Docs/CharacterBaseAuditTools/package_female_blink_preview.py
ffmpeg -framerate 30 -i 'TestResults/FemaleBlinkUnity/sweep/frame-%03d.png' -c:v libx264 -crf 16 -pix_fmt yuv420p -movflags +faststart 'TestResults/FemaleBlinkUnity/Review/sweep.mp4'
ffmpeg -framerate 60 -i 'TestResults/FemaleBlinkUnity/normal-speed/frame-%03d.png' -c:v libx264 -crf 16 -pix_fmt yuv420p -movflags +faststart 'TestResults/FemaleBlinkUnity/Review/normal-speed.mp4'
python -B Docs/CharacterBaseAuditTools/verify_female_blink_unity.py --source-root 'C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models'
```

These commands reproduce this approved milestone; preserve existing run evidence before deliberately rerunning captures. They do not authorize advancement to another milestone.

## Exact repository changes and preservation

**85/85 purchased files and 1,116/1,116 pre-existing audit/prototype/authoring/evidence files retain their size, SHA-256 and modification timestamp.** The seven previously untracked authoring files remain unmodified. See [preservation.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkUnity/preservation.json>) for the complete working-tree status and preservation result.

The exact non-ignored working-tree status at completion is:

```text
 M .gitignore
?? Assets/Editor/Characters/FemaleBlinkPrototypeTools.cs
?? Assets/Editor/Characters/FemaleBlinkPrototypeTools.cs.meta
?? Assets/Tests/Editor/FemaleBlinkPersonPrototypeTests.cs
?? Assets/Tests/Editor/FemaleBlinkPersonPrototypeTests.cs.meta
?? Assets/Tests/Editor/FemaleBlinkPrototypeTests.cs
?? Assets/Tests/Editor/FemaleBlinkPrototypeTests.cs.meta
?? Docs/CharacterBaseAuditTools/author_female_blink_corrective.py
?? Docs/CharacterBaseAuditTools/build_female_blink_corrective.py
?? Docs/CharacterBaseAuditTools/export_female_blink_unity.py
?? Docs/CharacterBaseAuditTools/female_blink_corrective_recipe.json
?? Docs/CharacterBaseAuditTools/measure_female_corrective.py
?? Docs/CharacterBaseAuditTools/package_female_blink_preview.py
?? Docs/CharacterBaseAuditTools/prepare_female_blink_unity.py
?? Docs/CharacterBaseAuditTools/validate_female_corrective_geometry.py
?? Docs/CharacterBaseAuditTools/verify_female_blink_corrective.py
?? Docs/CharacterBaseAuditTools/verify_female_blink_unity.py
?? Docs/FemaleBlinkBothUnityValidation.md
?? Docs/FemaleBlinkCorrectiveAuthoring.md
```

The pre-existing seven are the five corrective Python tools (`author`, `build`, `measure`, `validate`, `verify`), `female_blink_corrective_recipe.json` and `FemaleBlinkCorrectiveAuthoring.md`. All remaining untracked entries above are new for this milestone. The only modified tracked file is `.gitignore`, adding two generated-candidate ignore rules. Licensed geometry and captures are ignored; their exact file inventory is the evidence manifest linked above.

`git diff --check` passed with exit 0. A repeat with the read-only `core.fsmonitor=false` command override also passed without the environment's empty-token warning; no Git configuration was changed. A separate scan of all untracked text files found no trailing whitespace. HEAD did not move. All milestone work remains uncommitted and unpushed.

BLINKBOTH UNITY VALIDATED
