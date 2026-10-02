# Boy canonical reference validation

Continuation of [ChildCanonicalFoundationValidation.md](ChildCanonicalFoundationValidation.md), 2 October 2026. Four independent canonical families remain Adult Female, Adult Male, Boy and Girl. Shared algorithms and animation architecture do not imply shared topology, vertex IDs, UVs, endpoints, bind poses or body scale.

The user accepted **Boy ReviewR3** with the explicit reply **“Accept ReviewR3 and continue”**, after seeing the source review images and disclosure of corner openings/internal contacts. That source visual gate authorized delta transfer and Unity validation. No Boy corrective was needed. This is an isolated technical reference, not production child gameplay or final material/art approval.

## Checkpoint and scope

Phase A began on `master` at `5a4e25513583bffc7670cde9911a4b98bebda356`. Fetch succeeded and HEAD matched origin/master. The eleven pending child structural audit files were reviewed; generated licensed assets, TestResults and purchased sources were excluded. The four child structural tests passed, compilation and `git diff --check` passed, and all 85 purchased files retained their hashes, sizes and mtimes. The 104 prior child Unity PNGs were byte-identical after the rerun.

The authorized commit **`b28167e309a9c4b44c420de9c6d303f43407f5d4`**, **`Add child character foundation validation`**, was pushed normally to origin/master. Local/remote equality and a clean non-ignored working tree were confirmed before Phase B. Evidence is under `TestResults/ChildReferenceCheckpoint/`, including `review.json`, `checkpoint.json` and `child-tests.xml`.

All Boy implementation described below is Phase B and remains uncommitted. No branch switch, extra worktree, reset, stash, history rewrite or force-push occurred. Girl remains at the structural checkpoint. No independent blinks, gaze, brows, automatic blinking, facial framework, Star Maker, hairstyle/clothing/material system, production capsule replacement or locomotion-contact fix was implemented.

## Authoritative input and bald derivative

Purchased root: `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/`.

| Input | Relative path under purchased root | SHA-256 |
| --- | --- | --- |
| Lightweight Boy | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df` |
| Full facial authoring rig | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.blend` | `2afd93d4a875a3f6cdaf86b906ff3cf552eb18b2fc2ffb0976db803ec5bb8537` |

Boy's verified lightweight components `[5,7,8,9,10,11,12,13,14,15,16,17,18]` are the 13 disconnected hair islands: 1,474 vertices. Only these were removed. The full source has an additional nine-vertex hair fragment; its visibility/render mask independently covers 1,483 hair vertices. Eyes and oral islands are retained. The main 12,178-vertex body/scalp component is closed and includes complete scalp geometry beneath the hair. Brow anatomy is retained with the body/facial surface; this milestone neither removes brows nor authors final eyebrow appearance.

`export_boy_bald.py` creates an isolated Blender derivative using Boy connectivity and guards against mixed hair/body edges. Its retained Basis, all 29 supplied keys, UVs, weights and skeleton compare exactly. Native Unity subsetting then preserves original buffers rather than round-tripping normals through an FBX export.

| Representation | Vertices | Triangles | Skin bones / shapes |
| --- | ---: | ---: | --- |
| Original lightweight Blender import | 17,468 | 34,108 | 50 / 29 |
| Bald Blender | 15,994 | 31,506 | 50 / 29 |
| Original native Unity | 19,861 | 34,108 | 50 / 29 |
| Bald native Unity | 18,069 | 31,506 | 50 / 29 |
| Final native BlinkBoth | 18,069 | 31,506 | 50 / 30 |

Boy stays at native scale: supplied-hair height approximately **1.495085 m**, bald height **1.466251839 m**. The source foot minimum **−0.002548806 m** is handled by the existing local foot alignment, not a rescale. Native submesh triangles remain **26,272 / 5,234**.

Boy's independently derived raw-FBX/native mapping covers every source vertex uniquely, including seam/material splits; no partially retained triangle exists. Measured maximum position difference is **0.000363044 mm**, exact UV difference zero. Boy-specific guards are **0.001 mm** and **1e−8 UV**. Every retained native position, normal, tangent, colour, UV channel, weight, bind pose and original shape frame is copied exactly. Humanoid Avatar/controller references and body bone transforms remain exact.

Actual Unity front, side, crown and full-body images were inspected. The scalp is continuous and usable as a bald reference; no scalp patch was fabricated. The supplied hairstyle remains intact in external source storage. Future modular hairstyles need Boy/family and identity fitting, hat compatibility, period variants and appropriate skin weights. Boy's authored hair weights are not justification for assuming every hairstyle can be rigidly attached to Head.

## Source calibration and accepted endpoint

Blender **5.0.1** evaluates the original full `Armature_Character` rig through the dependency graph: 652 source bones, including 148 marked deform. Meaningful eyelid controls drive their normal constraints/mechanisms. No DEF rotation approximation, adult endpoint or adult corrective geometry was used.

Boy's measured central control gap is **29.061198 mm**. The bounded search includes 27 non-neutral endpoint trials: 12 central pairs, nine adjacent-control refinements and six side refinements. Central upper travel spans **−24.702019 to −30.514258 mm** (85–105% of Boy's gap); lower travel spans **+4.359180 to +7.265300 mm** (15–25%). Central pairs tested were 90/20, 85/25, 90/15, 90/25, 95/15, 95/20, 95/25, 100/15, 100/20, 100/25, 105/15 and 105/20 percent. Adjacent trials covered upper outer/inner and lower inner/outer contributions. Exact configurations are versioned in `CharacterBaseAuditTools/boy_blink_candidates.json`; every trial's measurements remain in Calibration, Refine and SideRefine evidence.

ReviewR3 uses these signed source-armature-Z offsets from source neutral; scripts convert them through each actual control's local matrix:

| Control contribution | Left | Right |
| --- | ---: | ---: |
| Central upper `lid.T.*.002` | −27.608138323 mm | −27.608138323 mm |
| Central lower `lid.B.*.002` | +5.812239647 mm | +5.812239647 mm |
| Outer adjacent upper `lid.T.*.001` | −2.000000 mm | −2.000000 mm |
| Inner adjacent upper `lid.T.*.003` | −1.000000 mm | −3.000000 mm |

All other controls remain neutral. Boy needs a small side-specific inner contribution: the right eye retains a larger inner-corner opening under equal controls. The tested −4 mm right-inner alternative was not selected simply to minimize exposure. ReviewR3 preserves the preferred contour with less travel. No eyeball was moved or shrunk; no Basis sculpt or corrective was added.

Visibility uses geometric BVH first hits against the bald surface, not colour/shadow segmentation. Search grids use 0.25 mm; the matched final source review uses **0.1 mm**. L/R identify anatomical sides.

| View / pose | Left visible eye area | Right visible eye area | Left/right remaining vs neutral |
| --- | ---: | ---: | --- |
| Front neutral | 745.35 mm² | 745.19 mm² | 100% / 100% |
| Front previous 90/20 probe | 8.52 mm² | 19.38 mm² | 1.143% / 2.601% |
| Front ReviewR3 | **0.69 mm²** | **2.54 mm²** | **0.0926% / 0.3409%** |
| Oblique neutral | 728.17 mm² | 545.09 mm² | 100% / 100% |
| Oblique previous 90/20 probe | 17.03 mm² | 15.06 mm² | 2.339% / 2.763% |
| Oblique ReviewR3 | **0 mm²** | **2.29 mm²** | **0% / 0.4201%** |

Maximum exposed vertical spans for ReviewR3 are **0.5 / 1.0 mm front** and **0 / 1.2 mm oblique**. These are genuine small corner openings, not assumed hidden by hypothetical lashes/materials. They remain disclosed in the acceptance receipt.

Front, oblique, whole-face and both eye close-ups were inspected against neutral and the previous probe. The accepted result reads closed at the displayed full-face distance; close-ups retain slight scalloping/pinching at the margins and inner corners. No obvious gross exterior fold, inversion or crushed surrounding face was seen in those views. This is not an assertion of perfect canthus anatomy at every angle.

Contact diagnostics are supporting evidence, not a collision certificate. Among 603 lid-weighted vertices, each eye already has 29 vertices more than 1 mm inside a nearest-eye tangent-plane proxy at neutral; the count remains 29 when closed. Deepest proxy distances change from about −9.055 mm to **−10.449 / −10.515 mm**. Nonadjacent orbital polygon overlap pairs increase from 0 at neutral/partial to **84** closed. Visible first-hit lid vertex samples have minimum signed eye distances **+0.528 / +0.105 mm front** and **+1.177 / +0.105 mm oblique**; none of those sampled visible vertices exceeds the 0.2 mm penetration threshold. This does not prove that every overlap pair is harmless or internal. The disclosed contacts did not prevent the user's visual acceptance.

Maximum source displacement is **25.647009 mm**, with 570 vertices moving more than 0.001 mm. Eyes remain exactly unchanged. Outside the lid-weighted set, maximum dependency-graph jitter is **0.000479 mm**. A conservative eye-box region expanded by half Boy's lid gap reports **0.559636 mm** maximum motion outside that box; this spatial diagnostic must not be confused with unrelated whole-face collapse. Reset to source neutral is exact, final invalid drivers are empty, and the original file is unchanged.

## Mapping and Blender delta-transfer gate

Boy's full source and lightweight variants have **different UVs and small neutral differences even within this family**. Raw vertex-order/UV equality was not assumed after the diagnostic mismatch. `boy_topology_correspondence.py` uses 15,640 independent near-exact geometry anchors (maximum **0.000547695 mm**, 0.001 mm guard), then eight adjacency-refinement iterations to uniquely resolve **all 15,994 retained vertices**. Complete polygon vertex sets and material assignments match after mapping. Any ambiguity stops the process.

Maximum source/lightweight UV difference is **0.244827446 UV units**; these source UVs are **not transferred**. Maximum raw neutral difference is **4.601367 mm**; evaluated source-versus-lightweight neutral difference reaches **4.727823 mm**, principally in oral geometry. Only the accepted evaluated displacement `closed − neutral` is transferred. Absolute source positions, source UVs and source rig complexity are not imported into the runtime baseline.

The unchanged lightweight Basis receives one `BlinkBoth`. Blender fingerprints confirm exact retained topology, UVs, normals, skin weights, all 29 keys and 50-bone skeleton/pose. Neutral restoration is exact and the baseline file remains byte-identical. Maximum displacement-transfer error is **0.000119733 mm**. No non-finite geometry occurred.

Baked neutral eye areas are 745.33 / 745.18 mm² front and 728.11 / 545.10 mm² oblique. Baked closed areas are **0.69 / 2.53 mm² front**, **0 / 2.29 mm² oblique**, with the same maximum exposed spans. The right-front difference from source is one 0.1 mm grid hit. Matched baked front/oblique/whole-face images at 0/50/100 were inspected. The accepted endpoint's contour and disclosed corner/contact limitations remain. Half a linear baked delta is not claimed to equal half the nonlinear source-control motion.

Blender candidate: `TestResults/BoyFoundation/BlinkBlender/BoyBaldBlinkBoth.blend`, SHA-256 **`5a24cf9f165b6c9ded4921f121aa0cfb88eaa654eb88957538cec09e6ba01934`**.

## Unity data and visual gate

Actual Editor: **Unity 6000.6.3f1**, existing PC URP configuration. The final candidate clones the native bald mesh and appends only BlinkBoth. Its 20 frames (weights 5–100 in steps of five) retain one linear positional delta while sampling evaluated normal changes. Shortest-arc transport preserves the native normal/tangent relationship, including its existing nonorthogonality on degenerate-UV islands. This is not final normal-map material certification.

Mapping uses the independently verified raw Boy FBX/native map plus exact corner UVs. The Blender-imported evaluated rest differs from native Unity at an unrelated forearm by **2.649879 mm** and **1.716201°** in the global normal comparison. These inherited offsets are retained rather than importing Blender's rest pose. In the blink-moving region, baseline position difference is only **0.000249490 mm** and normal difference **0.031647°**. Independently measured guards: 2.7 mm global inherited offset, 0.001 mm blink-region position, 1e−8 UV, 0.1° blink-region normals. Native UV difference is exactly zero.

Unity tests measure maximum frame displacement parity error **0.000053836 mm**, maximum blink-region absolute source error **0.000365103 mm**, and maximum blink-region normal angle **0.019783°**. CPU skinning endpoint parity across Idle/Walk/Run is within **0.000429476 mm**. Native baseline buffers, original 29 channels, 50 bone paths/local transforms, Avatar and controller are exact.

Actual URP captures cover 0/25/50/75/100/restored 0. All geometry is finite. Restored positions and normals are exact. Baseline, new-zero and restored-zero PNGs are byte-identical in front eye, oblique and whole-face views. The deterministic 49-frame 30 fps sweep and 61-frame 60 fps example exercise closing/reopening; the latter uses 100 ms close, 40 ms hold and 160 ms reopen. Captured motion frames were inspected for lid collapse, seam/corner jumps, exposure and shading discontinuities. No new abrupt pop was evident in those inspected sequences. There is no automatic blinking behavior.

The harsh nose/temple shading is visible in the pre-blink baseline and the adult technical references. The no-shadow diagnostic is unchanged; the lights already have shadows disabled. Its root cause is not established here. Native shading parity passes; final skin, eye, brow/lash materials and close-up lighting remain separate art work. Grey renders are **technical evidence only**.

All 29 supplied channels survive with their original names, including `mothShrugtLower` and `mothShrugtUpper`. They were exercised at 25/50/75/100 with exact resets. Smile and JawOpen each coexist with BlinkBoth; measured additive error is about **1.88e−9 mesh-local units**, with exact reset. These mesh-local values must not be misreported as world metres. Actual combination images were inspected.

## Body motion, person integration and stature

The bald subset matches the structural reference at nine Idle/Walk/Run phases within **0.000725161 mm** of retained world positions. Known Walk minimum **−0.07391284 m** and Run minimum **−0.06159093 m** remain unchanged. Body animation exercises BlinkBoth at intermediate weights and full closure through twelve additional phases; Animator playback does not overwrite the shape, and neutral restores exactly within each fixed body pose. This is not approval of the known locomotion ground contact.

The isolated full person prefab uses existing EmployeeAgent, HumanBasePrototypeVisual, HeldPersonPresentation, navigation, selection, contextual interaction and autonomy implementations. The existing serialized suspension pivot is fitted to Boy's own Neck (approximately `(0, 1.21, −0.08)` in the visual pivot). No production person is replaced.

The first full-person run passed navigation, actual pointer selection/carry/context, and Socialize but failed the child-height test. Diagnosis: `EmployeeAgent.Awake` unconditionally applies the existing **2 m height / 1 m base offset / 0.35 m radius** profile to both NavMeshAgent and CapsuleCollider. An initially generated child-height profile was therefore overwritten and left the visual 0.266874 m too high after navigation settled.

The isolated prefab now explicitly uses that existing runtime profile and places its visual root at −1 m. Boy's mesh stays 1.466252 m tall. The selection marker stays 0.02 m above the ground. This avoids an ineffective height override without changing production interaction policy. The collider consequently extends roughly **0.533748 m above the neutral child body**; it is a conservative oversized click/clearance target, not a correctly fitted production child collider. Recommend a small explicit per-presentation height/base-offset/radius data contract respected by EmployeeAgent initialization before production adoption. Do not infer that changing prefab values alone works today. The existing NavMesh bake remains shared, so this pass also does not establish child-specific doorway clearance.

The corrected prefab passes actual EmployeeAgent identity, NavMesh Walk/arrival/Idle/Run, real physics hover/click selection, hold pickup/held presentation, invalid-drop retention, valid drop, contextual Practice and Escape restore. Two actual Boy presentations reach active Socialize with distinct positions: **1.162752 m** root separation in the final full-person run. BlinkBoth remains at 100 through these relevant state changes. The visual marker's height is asserted; this does not establish final marker artwork or tooltip typography.

The carried-camera check exercises the real StudioCameraController with injected keyboard pan/rotation and wheel zoom, then restores the person. Carry lift stays **1.2 m** under the existing interaction semantics. A second test failure correctly exposed an inaccurate fixture assumption: its NavMesh surface is **+0.033333 m**, not exactly the rendered floor plane. Assertions now use the measured navigation surface, retaining the 2 mm alignment tolerance. This is not a ground-contact fix. The first camera capture after input was empty because the test panned beyond the subject during Editor stalls; its image is retained as `held-camera-before-refocus.png`. The final capture explicitly releases input, refocuses the existing camera and asserts that the subject is inside its viewport before rendering.

## Runtime cost and future binding

Final prefab: **50 skin bones, 30 BlendShapes, 56 transforms, one active SkinnedMeshRenderer, two submeshes**. The inactive selection indicator is an additional renderer when selected. Unity's shared-mesh memory estimate at construction is **24,777,952 bytes**, versus **24,206,848 bytes** for the inspected bald baseline instance; after final inspection the candidate estimate is **25,029,045 bytes**. These Editor estimates depend on loaded/cached mesh data and are not GPU allocations or a crowd budget. No player build, crowd benchmark, GPU profile, LOD or production culling work was performed.

Keep a semantic facial layer above family-local raw assets: e.g. a SilverScreen mouth-shrug semantic maps to Boy's existing misspelled name without renaming its raw key. Bindings should store supported capabilities, raw names, calibrated ranges and side-specific values by family; never share numeric shape indices or source/runtime correspondence across families. This milestone documents the recommendation only.

Future identity changes to eye shape/size/spacing/tilt, lid anatomy, head shape, jaw/chin, nose, lips, cheeks, ears, height/proportions and asymmetry need family-specific fitting and regression of blink contact. This default endpoint does not automatically survive large identity edits; identity-dependent correctives may be needed. Dimples are a future identity trait whose visibility depends on expression. Freckles and moles belong to future UV/material appearance work. None of these systems was implemented.

## Reproduction and evidence

Tracked tools are under `Docs/CharacterBaseAuditTools/`; all licensed arrays and rendered evidence are under ignored `TestResults/BoyFoundation/`. The workflow is: export Boy bald Blender derivative; export native Unity data; derive the Boy native subset; build/test bald asset; evaluate bounded source candidates; capture review and record user acceptance; prove source correspondence and bake delta; sample Blender data; export native baseline; prepare Unity additions; build/test the isolated candidate; package actual captured frames. Source-opening scripts use separate generated save paths and never save over purchased files.

Core evidence paths relative to `TestResults/BoyFoundation/`:

| Gate | Exact evidence files |
| --- | --- |
| Preservation baseline | `baseline.json`, `preservation.json` |
| Bald source | `Bald/BoyBaldBaseline.blend`, `Bald/bald-preservation.json`, `Bald/source-correspondence.json` |
| Native subset | `Unity/native.json`, `Unity/partition.json`, `Unity/native-mapping.npz`, `Unity/correspondence.json`, `Unity/bald-runtime.txt` |
| Bald views | `Unity/Captures/Bald-front.png`, `Bald-side.png`, `Bald-crown.png`, `Bald-full-body.png` under the same Captures directory; `Unity/bald-contact.png`, `Unity/locomotion-contact.png` |
| Source search | `Calibration/calibration.json`, `Refine/calibration.json`, `SideRefine/calibration.json`; their candidate NPZs and renders |
| Accepted source | `Review/calibration.json`, `Review/contact.json`, `Review/ReviewR3.npz`, `Review/user-acceptance.json`, `Review/review-contact.png`, `Review/previous-versus-review.png`, `Review/eyes-closeup.png`, `Review/image-provenance.json` |
| Blender transfer | `BlinkBlender/BoyBaldBlinkBoth.blend`, `blender-export.json`, `source-correspondence-proof.json`, `source-mapping.npz`, `blender-samples.npz`, `neutral.npz`, `partial.npz`, `blink.npz` under the same directory |
| Native blink data | `UnityBlink/mapping.json`, `UnityBlink/blink-addition.bin`, `UnityBlink/native-data-validation.txt`, `UnityBlink/construction.txt` |
| Unity visuals | `UnityBlink/stills/`, `UnityBlink/body/`, `UnityBlink/coexist/`, `UnityBlink/baseline/`, `UnityBlink/lighting/`; exact files in the generated manifest |
| Motion/review | `UnityBlink/animation-timeline.csv`, `UnityBlink/runtime-motion-validation.txt`, `UnityBlink/lower-face-validation.txt`, `UnityBlink/pixel-comparison.json`, `UnityBlink/Review/open-half-closed.png`, `UnityBlink/Review/coexist-body.png`, `UnityBlink/Review/normal-speed.mp4`, `UnityBlink/Review/normal-speed.gif`, `UnityBlink/Review/sweep.gif` |
| Focused tests | `bald-tests.xml`, `blink-tests.xml`, `person-tests.xml` (retained failed first run), later final XML as listed below |

Generated Unity assets are under ignored `Assets/SilverScreen/Art/Characters/BoyFoundationPrototype/`: `BoyBald_RetainedData.asset`, `SS_BoyBaldReference.prefab`, `BoyBald_BlinkBoth.asset`, `SS_BoyBlinkPrototype.prefab`, with their metadata. No purchased/generated licensed geometry is proposed for Git.

Diagnostic history is retained. Initial helper compilation failed on assigning through a NativeArray using-variable and was corrected with a proper lifetime block. Initial source UV/order diagnostics stopped rather than baking an unproved map; graph correspondence resolved the ambiguity. An evaluated-rest nearest-point attempt failed and was replaced by the independently proven raw native map. One Python mapping job was stopped because repeatedly decompressing NPZ UV data was unnecessarily slow; caching the array produced the same mapping efficiently. Refresh RPC timeouts during domain reload are retained separately from actual compilation results. Regenerating the Blink asset emitted an object-name/filename warning; it is a naming diagnostic, not a geometry failure. The first failed person XML remains intact and is not counted as a pass.

## Validation ledger and repository handoff

| Validation | Result / evidence under TestResults/BoyFoundation |
| --- | --- |
| Native bald / Humanoid / all 29 channels / body parity | 2 passed, zero failed/skipped; `bald-tests.xml` |
| Blink native data / weights / motion / lighting / lower-face coexistence | 4 passed, zero failed/skipped; `blink-tests.xml` |
| First person run | 3 passed, 1 failed, zero skipped; `person-tests.xml`; child profile overwritten by existing Awake policy |
| Person rerun after visual-root correction | 3 passed, 1 failed, zero skipped; `person-height-tests.xml`; navigation, selection/carry/context and Socialize passed; camera fixture expected wrong ground height |
| Focused corrected height/camera | 1 passed, zero failed/skipped; `person-camera-tests.xml`; numerical proof, off-subject screenshot retained |
| Final framed camera evidence | 1 passed, zero failed/skipped; `person-camera-framed-tests.xml`; actual `UnityBlink/person/held-camera.png` inspected, showing the held Boy at management distance |
| Final mesh/prefab recheck | `NativeBaselineAndSelectedEndpointArePreserved` repeated successfully; `final-unity-rpc.json`, `final-unity-state.txt` |
| Closing Editor / Console | Edit Mode, compilation complete with no failure, one clean `Assets/Scenes/Studio.unity`; final Console zero errors and zero warnings; `final-unity-closed-rpc.json`, `console-final-rpc.json` |
| Source and prior-work preservation | All 85 purchased hashes/sizes/mtimes exact; all 3,513 baseline files exact; `preservation.json` |
| Repository and tooling checks | `git diff --check` exit 0; all pending text checked for trailing whitespace; Boy Python syntax and candidate JSON parse; `repository-final.json` |

The suites cover **10 distinct passing Boy tests**; repeated scenarios do not increase this count. Phase A's four child structural tests are separate. Missing licensed assets are not silently counted as passing. The camera capture proves visible carried presentation in the existing camera, not final lighting or a bespoke child carry animation. Additional baked individual-eye views (`BlinkBlender/eye-L-blink-bald.png` and `eye-R-blink-bald.png`) were inspected and retain the accepted margin/corner appearance.

Phase B starts and ends at **`b28167e309a9c4b44c420de9c6d303f43407f5d4`** on **`master`**, matching `origin/master`. HEAD moved only for the explicitly authorized Phase A checkpoint; it did not move during Boy implementation. No Phase B commit or push was made. Existing committed Female, Male and child structural work is preserved.

Exact non-ignored Phase B file scope (all unstaged; no deletions):

```text
 M .gitignore
 M Docs/CharacterBaseAudit.md
?? Assets/Editor/Characters/BoyBlinkPrototypeTools.cs
?? Assets/Editor/Characters/BoyBlinkPrototypeTools.cs.meta
?? Assets/Editor/Characters/BoyFoundationPrototypeTools.cs
?? Assets/Editor/Characters/BoyFoundationPrototypeTools.cs.meta
?? Assets/Tests/Editor/BoyBlinkPersonPrototypeTests.cs
?? Assets/Tests/Editor/BoyBlinkPersonPrototypeTests.cs.meta
?? Assets/Tests/Editor/BoyBlinkPrototypeTests.cs
?? Assets/Tests/Editor/BoyBlinkPrototypeTests.cs.meta
?? Assets/Tests/Editor/BoyCanonicalFoundationTests.cs
?? Assets/Tests/Editor/BoyCanonicalFoundationTests.cs.meta
?? Docs/BoyCanonicalReferenceValidation.md
?? Docs/CharacterBaseAuditTools/bake_boy_blink.py
?? Docs/CharacterBaseAuditTools/boy_blink_candidates.json
?? Docs/CharacterBaseAuditTools/boy_topology_correspondence.py
?? Docs/CharacterBaseAuditTools/calibrate_boy_blink.py
?? Docs/CharacterBaseAuditTools/export_boy_bald.py
?? Docs/CharacterBaseAuditTools/inspect_boy_blink_contact.py
?? Docs/CharacterBaseAuditTools/package_boy_blink_preview.py
?? Docs/CharacterBaseAuditTools/package_boy_review.py
?? Docs/CharacterBaseAuditTools/prepare_boy_blink_unity.py
?? Docs/CharacterBaseAuditTools/prepare_boy_native_subset.py
?? Docs/CharacterBaseAuditTools/render_boy_blink.py
```

Generated licensed assets/evidence stay ignored and locally available. `generated-files.json` provides exact generated file paths, sizes and SHA-256 values; `repository-final.json` records the final working-tree status, HEAD/origin and whitespace checks. No production runtime script, saved scene or existing prototype asset is changed by Phase B.

Closing status: **2 modified tracked files, 22 untracked reviewable files, nothing staged**. All **482** generated asset/evidence files indexed by the manifest are ignored; the index itself and repository ledger are also ignored and excluded from their own hash index. A final read-only `git ls-remote origin refs/heads/master` confirmed the checkpoint hash. Git emitted its existing empty-update-token notice and an LF/CRLF normalization notice for the appended audit document; neither is a whitespace-check failure. No repository configuration was changed to suppress these notices.

**Outcome: Boy is the validated isolated child reference for the default bald family mesh and accepted BlinkBoth.** Production adoption remains subject to the documented collider fitting, locomotion contact, material/art and performance follow-ups. Girl remains structural only. No subsequent milestone was started.
