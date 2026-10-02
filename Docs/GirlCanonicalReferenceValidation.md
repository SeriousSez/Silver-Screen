# Girl canonical reference validation

Continuation of [ChildCanonicalFoundationValidation.md](ChildCanonicalFoundationValidation.md) and [BoyCanonicalReferenceValidation.md](BoyCanonicalReferenceValidation.md), 2 October 2026.

**ReviewG2 accepted for the canonical-reference/foundation stage on 3 October 2026.** Girl-specific correspondence and source-delta transfer passed. The accepted endpoint is unchanged; a newly observed linear-interpolation breakthrough at half closure was resolved by sampling the original source-rig trajectory. Unity blink and full person validation passed: eight new tests, plus the two preserved bald-foundation tests, make ten distinct passing Girl tests. This is not final production-quality eyelid art. Plain-grey renders remain technical evidence. Adult Female, Adult Male, Boy and Girl remain independent topology families.

## Phase A: completed Boy checkpoint

Before Girl work, branch `master` and fetched `origin/master` both pointed to `b28167e309a9c4b44c420de9c6d303f43407f5d4`. All 24 pending Boy files were reviewed and classified. Reviewable tools, tests, metadata, configuration, `.gitignore` and documentation were staged; licensed generated assets, purchased files and TestResults were excluded. Indexed file content was compared to the reviewed working files, accounting for Git line-ending normalization.

Unity compilation passed. Final focused reruns passed **10 distinct Boy tests: two bald, four blink and four person tests**, with zero failures/skips. A first broad filter selected zero tests; that diagnostic receipt remains available and is not counted. Existing historical failure receipts were preserved. The Console had zero errors and one unrelated Unity AI account API timeout warning. All 85 purchased files retained hash, size and mtime. Previous Boy evidence was backed up, fresh rerun results archived, and the original evidence restored byte-for-byte. Neutral/zero/restored render parity was checked; the earlier eyes baseline differs by only nine one-channel, one-level pixels, while face/oblique match exactly.

The authorized commit **`e384eed6ca851a65e5932f0e5ed2d11619073d6c`**, **`Add Boy canonical character reference`**, was pushed normally to `origin/master`. Local/remote equality and a clean non-ignored working tree were confirmed before Phase B. Receipts: `TestResults/BoyReferenceCheckpoint/checkpoint.json`, `precommit-validation.json`, `review.json`, `staged-review.patch`, `bald-tests.xml`, `blink-tests.xml`, `person-tests.xml`, `PriorBoyFoundation/` and `RerunOutputs/`.

## Phase B: immutable Girl inputs

Purchased root: `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/`.

| Input | Relative path | SHA-256 |
| --- | --- | --- |
| Lightweight runtime source | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f` |
| Complete authoring rig | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.blend` | `d088d51f5b6617ea29fb0145ade2af69934156d535872c43b55fe3011d4d45c8` |

Girl's source geometry, component partition, correspondence and control offsets were independently measured. Earlier family tools supply algorithmic architecture only. No other family's geometry, vertex IDs, UVs, skinning, corrective, endpoints, body scale or presentation offsets were reused. Purchased files were read without saving over them.

## Bald base and native preservation

Girl's verified component IDs `[1,6,7]` contain the disconnected 1,004-vertex hair cap and two 231-vertex buns: **1,466 hair vertices**. Only these three islands are removed. The connected **12,590-vertex body/scalp** has a complete, closed scalp; its 25,257 edges each have two incident faces. There are no shared hair/body edges. Eyes, ears, oral islands and brow anatomy remain. No scalp repair was authored. Visible brow anatomy belongs to the body/facial surface; final eyebrow material treatment has not been established by this technical-grey inspection.

Four loose non-surface FBX vertices, IDs **11835–11838**, are explicitly retained in the generated Blender derivative. The original native Unity importer already omits those four unreferenced points. The native subset cannot retain data absent from its input; it does not classify them as hair or delete a surface. The full Blender source has one three-vertex loose-edge component and one isolated point; the FBX import drops those loose edges and represents four single-point components.

| Representation | Vertices | Triangles | Skin bones | Supplied shapes |
| --- | ---: | ---: | ---: | ---: |
| Original lightweight Blender import | 17,876 | 35,162 | 51 | 29 |
| Generated bald Blender | 16,410 | 32,330 | 51 | 29 |
| Original native Unity | 20,207 | 35,162 | 51 | 29 |
| Generated bald native Unity | 18,429 | 32,330 | 51 | 29 |

The native mapping independently matches every imported vertex to Girl's raw FBX by position and exact UV, verifies material membership, and proves unique correspondence at seams. Maximum measured position difference is **0.000321844385 mm**; the Girl guard is **0.0007 mm**, with **zero UV error required**. All 17,872 source surface vertices are covered. The original native import has 2,335 seam/material splits. Hair removal drops 1,778 native vertices, leaving 16,406 distinct source surface vertices; zero triangles cross the retained/removal partition. Retained submesh triangle counts are **23,786 / 3,310 / 5,234**.

Original native positions, normals, tangents, colours, all UV channels, complete weights, bind poses, material assignments and all 29 BlendShape frames are preserved/subset exactly. No Blender-to-FBX normal round trip is used as runtime authority. The original Avatar/controller, body transforms, `ROOT → Hips` hierarchy and native scale remain. The prefab contains 51 skin references, 54 transforms and one renderer. Recorded mesh memory is 23,138,352 bytes, an Editor observation rather than a runtime performance budget.

Native heights remain approximately **1.493982 m** with supplied hair and **1.460771 m** for the bald body/scalp. The original approximately **−0.001078 m** foot minimum uses Girl's existing structural foot alignment; there is no rescaling.

### Unity bald gate

On Unity **6000.6.3f1**, `GirlCanonicalFoundationTests` passed **2/2**, zero failures/skips:

- `NativeBaldPreservesEveryRetainedBufferAndHumanoid`.
- `RuntimeBaldScalpLocomotionAnd29ShapeParity`.

The native Humanoid remains valid. Optional distal thumbs/eye/jaw mappings remain absent as supplied; no invented mappings or Generic fallback were introduced. Every supplied shape was tested at 25/50/75/100, with finite nonzero movement and exact neutral restoration. Raw names remain unchanged, including `mothShrugtLower` and `mothShrugtUpper`. Future semantic family bindings should resolve aliases by name and capability, not shared numeric indices.

Idle/Walk/Run were sampled at phases 0.15/0.45/0.75. Maximum original/bald world-position difference was **0.000719244 mm**. Walk minimum remained **−0.07500487 m**, Run **−0.05333803 m**; these existing ground-contact issues were not repaired. Actual URP front, side, crown and full-body evidence confirms a continuous bald scalp. This gate establishes retained-data and technical rendering compatibility, not final art approval.

## Girl source blink search

Blender **5.0.1** evaluates the original complete `Armature_Character` rig: 653 bones, 148 marked deform. Meaningful eyelid controls run through the dependency graph and normal mechanisms. No direct DEF rotations or runtime facial skeleton were substituted. Transient driver warnings during file loading are retained in logs; the completed evaluations report **zero invalid drivers**, exact neutral reset and unchanged source hash/mtime.

Girl's own central control gap is **32.017111778 mm** on each side. The search covers **26 non-neutral endpoint trials**: 12 central pairs, 10 adjacent refinements and four corner refinements. Central upper fractions are 85/90/95/100% and lower fractions 15/20/25%, giving upper **−27.214545 to −32.017112 mm**, lower **+4.802567 to +8.004278 mm**. Adjacent increments are 2.5% of Girl's gap, **0.800427794 mm**. Tested adjacent ranges reach upper outer −2.401283 mm, lower outer +3.201711 mm and upper inner −1.600856 mm. Exact configurations are versioned in `CharacterBaseAuditTools/girl_blink_candidates.json`.

### Initial ReviewG1 proposal, subsequently declined

ReviewG1 is the `cornerA` result. Both anatomical sides use the same offsets; Girl's evidence does not justify Boy's asymmetric inner-control treatment.

| Meaningful control | Left | Right |
| --- | ---: | ---: |
| Central upper `lid.T.*.002` | −30.416256189 mm | −30.416256189 mm |
| Central lower `lid.B.*.002` | +6.403422356 mm | +6.403422356 mm |
| Outer upper `lid.T.*.001` | −1.600855589 mm | −1.600855589 mm |
| Outer lower `lid.B.*.003` | +1.600855589 mm | +1.600855589 mm |
| Inner upper `lid.T.*.003` | −0.800427794 mm | −0.800427794 mm |

All other controls stay neutral. Values are world/source-armature Z offsets converted through each actual control's local matrix. No eye displacement, corrective sculpt or Basis change was used. `partial50` evaluates half of these control offsets through the rig; it is not a runtime BlendShape sample.

ReviewG1 was chosen for user review because the inspected views read closed with modest corner contributions. More aggressive alternatives reduced exposure but increased nonadjacent contact counts (80 for ReviewG1, 92 for cornerC, 96 for cornerD) without a clear contour benefit. The seam has small polygonal/scalloped segments and a raised outer-corner contour. These details need the user's visual judgment; numerical closure does not grant aesthetic acceptance.

### Geometric exposure

Measurements use first-hit BVH rays against the whole bald surface, never colour/shadow segmentation. Search resolution is 0.25 mm; final matched source review is **0.1 mm**. L/R mean anatomical sides. Oblique asymmetry includes the camera's viewing direction and is not proof that unequal control offsets are needed.

| State/view | Left area mm² | Right area mm² | Left remaining | Right remaining | Max exposed span L/R mm |
| --- | ---: | ---: | ---: | ---: | --- |
| Neutral front | 849.72 | 849.58 | 100% | 100% | 28.0 / 28.0 |
| Old structural strong, front | 3.37 | 3.41 | 0.397% | 0.401% | 1.3 / 1.4 |
| ReviewG1 front | 0.55 | 0.54 | 0.0647% | 0.0636% | 0.7 / 0.7 |
| Neutral oblique | 820.06 | 634.82 | 100% | 100% | 27.7 / 27.6 |
| Old structural strong, oblique | 22.39 | 8.92 | 2.730% | 1.405% | 3.4 / 2.1 |
| ReviewG1 oblique | 7.86 | 4.17 | 0.958% | 0.657% | 2.7 / 1.5 |

The old structural strong pose uses Girl's measured −28.815400600/+6.403422356 mm central offsets. Residual ReviewG1 exposure represents genuine small inner/outer corner openings. It is not concealed or assumed to disappear under lashes or final materials. The sampled span is supporting geometry evidence, not a sub-pixel closure guarantee.

### Contacts, surrounding movement and coupling

ReviewG1 moves 613 vertices by more than 0.001 mm, maximum **29.448605 mm**. Eyeball coordinates remain exact. Maximum motion outside the broad orbital window is **0.563556 mm**; outside all lid-weighted body vertices it is **0.000606491 mm** (no vertex over 0.001 mm). Weight-name region maxima overlap: a vertex tagged brow/nose/cheek may also be an eyelid vertex. Their 12–29 mm maxima must not be described as unrelated cheek/nose displacement. The full face must still be inspected for visible distortion.

At neutral and closed, each side has 28 lid-weighted samples with a signed nearest-eye tangent-plane proxy below −1 mm. Minimum is approximately −10.439 mm neutral and −10.449 mm closed, reflecting supplied internal geometry as well as pose contact. Nonadjacent orbital polygon overlap pairs rise from **0 neutral / 0 partial** to **80 closed**. Those internal contacts remain disclosed; they are not a clean collision certificate. First-hit-visible lid-vertex samples have **zero proxies below −0.2 mm** in the measured front and oblique views. Closed minimum visible proxies are approximately +0.453 mm front and +0.154 mm oblique on the left, +0.453/+0.804 mm on the right. Sparse vertex rays cannot prove that every triangle is penetration-free.

Source-only unilateral diagnostics distinguish real opposite-eye motion from broad body selection. Left controls move three opposite-side body vertices by up to **1.083269 mm**; right controls move four by up to **1.517896 mm**. These lie near the midline, source Z approximately 1.3063–1.3069 m, below the eye region. The opposite **224-vertex eye window stays bit-exact**, and its measured exposure remains neutral. Neither eyeball moves. These diagnostic poses do not create independent runtime blinks. Exact neutral restoration passes after every candidate/reset.

## Initial stop point and remaining work

Section 8 of the user's milestone brief explicitly reserves the Girl visual gate for the user and requires approval before source-delta transfer. ReviewG1 was **subsequently declined for contour**, while its degree of closure was judged sufficient. See the contour follow-up below. No corrective has been authored; if the user accepts source controls alone, no corrective should be added. If the user rejects a local defect, investigate the smallest Girl-only source adjustment/corrective on generated geometry.

After explicit acceptance, independently prove Girl complete-source/lightweight correspondence, transfer evaluated closed-minus-neutral deltas onto the unchanged lightweight Basis, validate Blender parity and then build/test the native Unity BlinkBoth candidate. The current raw-FBX/native mapping is only the bald-subset mapping; it does not establish the later facial-source correspondence.

Unity BlinkBoth weights, animated blink, body-animation coexistence, Smile/JawOpen combinations and full Girl person integration are outstanding. Girl has not yet independently reproduced the EmployeeAgent profile mismatch. The known Boy finding remains a required Girl follow-up: measure the enforced 2 m height/1 m base offset/0.35 m radius against Girl's actual body, use only the established isolated compensation strategy if needed, and document the mismatch. Do not copy Boy's numeric pivot/offset or change production EmployeeAgent policy. A future explicit presentation/navigation profile and semantic facial-name binding remain recommendations, not implementations.

No shared Character Runtime V1, independent runtime blink, gaze, brows, automatic blink, Star Maker, hairstyle/material system, production capsule replacement or locomotion-contact fix was started. A complete scalp makes modular hairstyles plausible; each future hairstyle still requires Girl-specific fit, scalp clearance, appropriate weights and hat compatibility. No hairstyle system was built.

## Reproducible tools and evidence

Reviewable tools are in `Docs/CharacterBaseAuditTools/`:

- `export_girl_bald.py`: Girl connectivity removal with retained Blender data assertions.
- `prepare_girl_native_subset.py`: measured Girl raw-FBX/native correspondence and partition.
- `calibrate_girl_blink.py` plus `girl_blink_candidates.json`: complete-rig evaluation, exact reset, first-hit exposure and movement regions.
- `inspect_girl_blink_contact.py`: signed contact proxies and nonadjacent orbital overlap diagnostics.
- `render_girl_blink.py`: actual evaluated source geometry in matched technical cameras.
- `package_girl_review.py`: labelled layouts of actual renders; no synthesized image content.

All generated/licensed data and evidence remain ignored:

- `Assets/SilverScreen/Art/Characters/GirlFoundationPrototype/GirlBald_RetainedData.asset` and `SS_GirlBaldReference.prefab`, with their `.meta` files.
- `TestResults/GirlFoundation/baseline.json`, `state-verification.json`, `bald-tests.xml` and its summary.
- `Bald/GirlBaldBaseline.blend`, `bald-preservation.json`, `source-correspondence.json` under the Girl evidence root.
- `Unity/native.json`, `correspondence-diagnostic.json`, `correspondence.json`, `partition.json`, `native-mapping.npz`, `representation.txt`, `bald-runtime.txt`, `bald-contact.png` and `Unity/Captures/`.
- `Calibration/`, `Refine/`, `Corner/`, `Coupling/`, `Review/`: exact candidate JSON, NPZ evaluated meshes, `calibration.json`, topology and render settings. Contact outputs are `Corner/contact.json` and `Review/contact.json`.
- `Review/{front,oblique,face,face-oblique,eye-L,eye-R,eye-oblique-L,eye-oblique-R}-{neutral,partial50,ReviewG1}-bald.png`.
- `Review/girl-source-eyes-review.png`, `girl-source-face-review.png`, `girl-source-corners-review.png` and `review-manifest.json` bind the displayed review to its exact source candidate.
- Logs and RPC receipts remain beside those directories, including refresh calls that timed out during domain reload; subsequent compilation and completed tests establish the bald result.

## Repository and preservation ledger

Phase B starts and remains on `master` at **`e384eed6ca851a65e5932f0e5ed2d11619073d6c`**, equal to the fetched/pushed origin/master checkpoint. HEAD did not move during Girl work. Girl changes remain uncommitted and unstaged. No branch/worktree switch, reset, stash, history rewrite or force-push occurred.

All 85 purchased files retain SHA-256, size and mtime. All 4,535 baseline prior asset/evidence files retain exact hashes. Generated Girl assets/evidence are ignored. `git diff --check` passes; new text files were also checked for trailing whitespace and Python parsing. Exact final status is recorded in `TestResults/GirlFoundation/state-verification.json`.

Final Editor readback confirms compilation successful, Edit Mode, exactly one loaded clean `Assets/Scenes/Studio.unity`, and zero Console errors/warnings. Receipts: `editor-final-rpc.json` and `console-final-rpc.json` under the Girl evidence root. All 24 matched review renders and the three layouts were generated; the front/oblique partial and closed views, both individual-eye corner views and full-face layouts were inspected. No obvious gross folding or surrounding facial crush was observed; user acceptance of the visible contour remains pending.

Exact non-ignored working-tree scope (all Girl work unstaged):

```text
 M .gitignore
 M Docs/CharacterBaseAudit.md
?? Assets/Editor/Characters/GirlFoundationPrototypeTools.cs
?? Assets/Editor/Characters/GirlFoundationPrototypeTools.cs.meta
?? Assets/Tests/Editor/GirlCanonicalFoundationTests.cs
?? Assets/Tests/Editor/GirlCanonicalFoundationTests.cs.meta
?? Docs/CharacterBaseAuditTools/calibrate_girl_blink.py
?? Docs/CharacterBaseAuditTools/export_girl_bald.py
?? Docs/CharacterBaseAuditTools/girl_blink_candidates.json
?? Docs/CharacterBaseAuditTools/inspect_girl_blink_contact.py
?? Docs/CharacterBaseAuditTools/package_girl_review.py
?? Docs/CharacterBaseAuditTools/prepare_girl_native_subset.py
?? Docs/CharacterBaseAuditTools/render_girl_blink.py
?? Docs/GirlCanonicalReferenceValidation.md
```

No Girl checkpoint is authorized by the current visual-gate stage.

## ReviewG1 contour rejection and source-only ReviewG2

The user explicitly declined ReviewG1: its coverage was sufficient, but the closed seam was too scalloped/wavy and the corner terminations looked unnatural, particularly front and oblique. This follow-up prioritizes a smoother continuous curve and comparable coverage. It does not seek zero eye exposure. ReviewG1's meshes, captures, measurements and initial gate receipt remain untouched as comparison history; `Contour/user-feedback.json` records the subsequent rejection.

### Source-control refinement

The full original Girl rig was inspected again. Both lid chains use the source rig's normal constrained controls and ten-segment B-bone mechanisms. At ReviewG1, the upper/lower control curves have uneven overlap and the outer canthus remains high relative to the closed central seam. These observations guided small control changes; no DEF bone was posed directly and no purchased file was saved.

Seventeen additional symmetric endpoint trials were evaluated: eight `ContourControls`, six `ContourPolish` and three `ContourArc` configurations. Four control trials, all six polish trials and all three arc trials received matched front/oblique renders. The configuration JSON preserves every exact value. Changes from ReviewG1 tested outer-corner Z offsets of −2/−4/−6 mm, inner-corner offsets down to −1.6 mm, central upper easing up to +3.4 mm, central lower redistribution from −1.6 to +1.6 mm, and smaller adjacent-control changes. Bounded ±8° source-Y rotations of the meaningful canthus controls were also evaluated through the rig's existing tangent-handle mechanisms; they were not selected.

The most useful improvement came from easing upper-lid overlap. Moving the lower lid up by the same amount retained the wavy intersection and was not preferred. Larger combined outer-corner adjustments introduced a sharper/doubled seam, whereas the smaller inner-corner refinement retained the smoother contour. The chosen `arcInner` result is named **ReviewG2**. All changes are Girl-specific, symmetric source-control offsets; **no localized corrective was necessary or authored**.

| Control | ReviewG1, each side | ReviewG2, each side | Change |
| --- | ---: | ---: | ---: |
| Central upper `lid.T.*.002` | −30.416256189 mm | −28.016256189 mm | +2.4 mm, eased |
| Central lower `lid.B.*.002` | +6.403422356 mm | +6.403422356 mm | Unchanged |
| Outer adjacent upper `lid.T.*.001` | −1.600855589 mm | −1.600855589 mm | Unchanged |
| Outer adjacent lower `lid.B.*.003` | +1.600855589 mm | +1.600855589 mm | Unchanged |
| Inner adjacent upper `lid.T.*.003` | −0.800427794 mm | −0.400427794 mm | +0.4 mm, eased |
| Outer canthus `lid.T.*` | 0 mm | −4.0 mm | Lowered |
| Inner canthus `lid.B.*` | 0 mm | −1.6 mm | Lowered |

All other controls and all control rotations remain neutral in ReviewG2. These are offsets converted from source-armature Z through each control's rest-local matrix; parent mechanisms and constraints mean they are not independent absolute world-space endpoint coordinates. Actual evaluated control positions are now also recorded in `calibration.json`.

### Coverage retained as a supporting check

The matched 0.1 mm first-hit BVH measurement uses the same bald surface and cameras as G1:

| View/eye | Neutral area mm² | G1 remaining area mm² / percent | G2 remaining area mm² / percent | G1 → G2 maximum span |
| --- | ---: | --- | --- | --- |
| Front L | 849.72 | 0.55 / 0.0647% | 2.00 / 0.2354% | 0.7 → 1.3 mm |
| Front R | 849.58 | 0.54 / 0.0636% | 1.99 / 0.2342% | 0.7 → 1.3 mm |
| Oblique L | 820.06 | 7.86 / 0.9585% | 6.62 / 0.8073% | 2.7 → 2.1 mm |
| Oblique R | 634.82 | 4.17 / 0.6569% | 8.27 / 1.3027% | 1.5 → 2.1 mm |

G2 deliberately allows slightly more exposure overall. It retains approximately **99.77% front coverage** and **98.70–99.19% oblique coverage** relative to neutral. The residual eye openings are genuine and remain disclosed. This is comparable closure, not a claim of identical exposure or mathematical sealing. At half source-control offsets, front exposure is approximately 46.9% of neutral; at 75% offsets, approximately 17.3%. Those states are full source-rig evaluations, not interpolated baked shapes.

### Contact and locality

Nonadjacent orbital polygon overlap pairs fall from **80 in G1 to 20 in G2**; neutral/50%/75% have zero in this diagnostic. This supports the overlap interpretation but is not the reason for aesthetic acceptance. Supplied internal contact proxies remain nonzero: each side still has 28 weighted samples below −1 mm, minimum approximately −10.449 mm versus −10.439 mm neutral. First-hit-visible lid-vertex samples have zero values below −0.2 mm; minimum at closed is +0.0924 mm on the oblique left. These are sparse supporting proxies, not complete intersection proof.

G2 changes 611 vertices by more than 0.001 mm from neutral, maximum 29.294628 mm. Relative to G1, 590 vertices exceed 0.001 mm change, maximum **3.646170 mm**, mean affected change **0.950353 mm**. Maximum movement outside the broad orbital window is **0.336778 mm**, smaller than G1's 0.563556 mm. Outside lid-weighted body vertices, maximum is 0.000606250 mm; no such vertex exceeds 0.001 mm. Eyeballs and hair remain bit-exact. The complete source reset is exact and final invalid-driver count is zero. Raw brow/nose/cheek weight masks still overlap the eyelids and should not be misread as isolated-region measurements.

### Evidence and gate

New evidence lives exclusively under **`TestResults/GirlFoundation/Contour/`**. `Controls/`, `Polish/` and `Arc/` retain all attempted endpoints, measurements and comparison renders. `ReviewG2/` contains matched neutral, `partial50`, `near75`, ReviewG2 and repeated ReviewG1 source evaluations; the repeated G1 and neutral are checked against the preserved originals. Review renders use the same 1000 × 700 cameras, material, lighting and 32-sample Cycles settings as G1. No subdivision, image synthesis, paint-over or geometry smoothing is used to hide the seam.

Required review outputs in `ReviewG2/` are:

- `{front,oblique,face,face-oblique,eye-L,eye-R,eye-oblique-L,eye-oblique-R}-{neutral,partial50,ReviewG2}-bald.png`.
- `girl-source-face-review.png`, `girl-source-eyes-review.png` and `girl-source-corners-review.png`.
- `girl-source-eye-L-review.png` and `girl-source-eye-R-review.png`: neutral/partial/closed front and oblique close-ups for each eye.
- `girl-contour-before-after.png` and `girl-corners-before-after.png`, directly comparing declined G1 to proposed G2.
- `calibration.json`, `contact.json`, `review-manifest.json`, `visual-gate.json` and exact NPZ states.

The central seam is visibly more continuous in the inspected trial front/oblique views, with reduced scalloping and gentler corner transitions. The surface remains a stylized, finite-resolution source mesh, and minor faceting/corner creasing remains a visual-review consideration. **ReviewG2 requires fresh user approval.** No Girl source-to-lightweight blink correspondence, delta transfer or Unity BlinkBoth work has begun. No commit or push is part of this contour follow-up.

The final 24 matched renders and seven review/comparison layouts were inspected, including both eyes separately at neutral/50%/closed and whole-face front/oblique. The observed contour improvement survives these close-ups; the remaining crease/faceting is not hidden by a material change or smoothing modifier. The supplied grey material still provides technical evidence only.

Source-only unilateral G2 diagnostics again leave the opposite 224-vertex eye window exact. Three/four cross-midline body-weighted vertices move up to 0.983030/1.396621 mm respectively, smaller than the G1 measurements; this is not opposite-eyelid motion. Both eyeballs remain exact. These diagnostic poses do not implement independent runtime blinks.

The contour follow-up began on `master`; `git fetch origin` succeeded and HEAD matched origin/master. Starting and ending HEAD are both **`e384eed6ca851a65e5932f0e5ed2d11619073d6c`**. `Contour/state-verification.json` records the exact working-tree status and final checks. All 85 purchased files, 4,535 earlier family/asset/evidence files and 180 pre-refinement Girl asset/evidence files are preserved. The bald derivative, 29 supplied keys and prior G1 comparison evidence were not overwritten. Girl work remains unstaged/uncommitted, with the same 14-file non-ignored scope listed above; this refinement changes only the calibration tool/configuration, review-layout tool and two documentation files. No Unity code or generated Unity asset was edited. `git diff --check` and parsing/whitespace checks for new Python/text files pass. No branch/worktree switch, reset, stash, commit or push occurred.


## ReviewG2 acceptance and transfer continuation — 3 October 2026

The user's approval is recorded verbatim in `Accepted/user-acceptance.json`, bound to ReviewG2 NPZ SHA-256 `d42994bffd9d648493a2a13c571f6213317283d967152e76a7ed08f168dabd29`. The approval covers the current canonical-reference/foundation stage. It does not approve final production eyelid art. Remaining contour, corner termination, faceting and crease limitations remain future facial-art polish. Historical pending/rejected gate receipts and all prior renders are retained unchanged.

### Independent Girl correspondence and endpoint transfer

Girl's own raw source/retained lightweight meshes supply 16,000 unique geometry anchors within a measured 0.0007 mm guard (maximum anchored error 0.000602397 mm). Eight graph-refinement iterations uniquely resolve all **16,410 retained vertices**, including the four loose non-surface points. Complete polygon sets and material assignments agree; independently measured UV differences happen to be zero. No UV equality or original vertex order was assumed. Maximum neutral variant difference is 0.956207 mm; it is not imported into the lightweight Basis.

The evaluated accepted source closed-minus-neutral delta is added to the unchanged lightweight Basis. Maximum evaluated displacement error is **0.000007451 mm**. Fingerprints prove exact baseline vertices, topology, UVs, corner normals, weights, all 29 supplied shapes and all 51 bone rest/pose matrices. Neutral restoration and the original bald `.blend` file are exact. Source UVs, materials and the heavy facial skeleton are not transferred.

Evidence: `Accepted/anchor-diagnostic.json`, `BlinkBlender/source-correspondence-proof.json`, `source-mapping.npz`, `blender-export.json`, and `source-bake-comparison.png`. The first linear candidate remains available as diagnostic history.

### New partial-closure transfer defect and bounded resolution

The first linear endpoint transfer reproduced the accepted closed eye but **failed the 50% visual check**: the upper eyelid took a straight chord through the eyeball, producing a visibly jagged opening. The matched source half-control state had a smooth edge. Sparse first-hit-visible lid samples corroborated a new approximately −0.0183 mm signed eye-surface penetration at linear 50%, compared with positive clearance in the source trajectory. These tiny measurements alone would have missed the visual significance; inspection determined the failure. No Unity candidate was built from that linear experiment.

The resolution leaves the approved ReviewG2 endpoint and all source controls unchanged. `sample_girl_blink_trajectory.py` evaluates the original complete Girl rig at 2.5% intervals. The 0%, 50% and 100% source arrays repeat the previously reviewed states exactly, final drivers are valid and resetting all controls is exact. The existing delta-transfer operation is applied independently to source samples at **5/10/…/100%**. Unity still receives exactly one new `BlinkBoth` channel, with twenty position/normal/tangent frames. This preserves the source lid arc; no sculpted corrective, additional expression channel, facial bones, eye movement or further endpoint refinement is introduced.

All sampled displacement transfers agree within 0.000007451 mm. Interpolation at the twenty intervening 2.5% samples differs from the fully evaluated source by at most **0.015041 mm**. Matched sampled 50% front/oblique renders no longer show the new jagged eye breakthrough; minimum visible-vertex clearance is approximately +0.09294 mm. The sampled 0% and 100% arrays are bit-exact with the original endpoint-transfer arrays. Neutral/50% have zero nonadjacent orbital overlaps in this diagnostic; closed retains the accepted 20 pairs and disclosed internal contacts. No claim of complete intersection freedom is made.

`BlinkBlenderSampled/blender-samples.npz` is the authoritative sampled playback data. Its saved `GirlBaldBlinkBoth.blend` preserves one editable **endpoint** key plus the 29 original keys; Blender's ordinary 50% slider on that endpoint-only file is still linear. Use the evaluated NPZ playback states or the generated Unity multi-frame shape to review the corrected trajectory. This limitation is stored on the Blender object and in the export receipt, rather than presenting that file as an interactive multi-frame player.

Evidence: `Trajectory/source-trajectory.npz`, `Trajectory/sampling.json`, `BlinkBlenderSampled/` (12 matched neutral/partial/closed front/oblique/full-face renders and sampled data), `Accepted/BakedContact/contact.json`, and `Accepted/blender-gate.json`.

| Baked state/view | Left visible area | Right visible area | Maximum span L/R |
| --- | ---: | ---: | --- |
| Neutral front | 849.72 mm² | 849.57 mm² | 28.0 / 28.0 mm |
| Closed front | 2.00 mm² | 1.99 mm² | 1.3 / 1.3 mm |
| Neutral oblique | 820.06 mm² | 634.82 mm² | 27.7 / 27.6 mm |
| Closed oblique | 6.62 mm² | 8.26 mm² | 2.1 / 2.1 mm |

Closed front exposure remains approximately 0.2354%/0.2342%; oblique approximately 0.8073%/1.3012%. Differences from accepted source coverage are at most one 0.1 mm grid sample. Residual openings and internal contacts are retained, not hidden or polished further.

### Native Unity binding

Girl's independently proven raw-FBX/native map covers **18,429 native vertices**, representing **16,406 surface vertices**; the four unreferenced Blender points were already absent from the original Unity import. Every native UV corner matches exactly. No other family's eye/body IDs are used: `native-regions.json` derives them through Girl's own mappings.

Girl's measured evaluated-rest mismatch is 0.660766 mm overall, only 0.000271972 mm in moving eyelid vertices. Native/source normal differences are at most 0.435236° overall and 0.0243814° in the blink region. Independent guards are 0.7 mm overall, 0.0007 mm in the blink region and 0.06° moving-normal agreement. The native baseline remains authoritative; the small inherited rest differences are not imported.

The generated native clone appends one twenty-frame `BlinkBoth`, retaining original buffers and shape frames. Normal/tangent additions transport the original native frame by the shortest arc between evaluated neutral and sampled Blender normals. Untouched regions retain exact values. This does not certify future normal-map materials or replace degenerate-UV tangent conventions. Packet version `SSBLINK2` includes independent reference positions for every frame so validation does not mistakenly assume a linear endpoint chord.

Generated assets: `Assets/SilverScreen/Art/Characters/GirlFoundationPrototype/GirlBald_BlinkBoth.asset` and `SS_GirlBlinkPrototype.prefab`. Licensed geometry and all evidence remain ignored. Native mesh: 18,429 vertices, 32,330 triangles, three submeshes, 51 skin bones, 30 shapes, one renderer and the existing valid Girl Humanoid Avatar/controller.


### Completed Unity and person proof

Actual Editor: **Unity 6000.6.3f1**, using the project's URP configuration and existing prototype materials. Four `GirlBlinkPrototypeTests` passed (12.178 seconds) and four `GirlBlinkPersonPrototypeTests` passed (6.147 seconds), zero failures/skips. Together with the preserved two bald tests (13.634 seconds), Girl now has **ten distinct passing focused tests**. The bald tests were not rerun or their evidence overwritten during this continuation.

Native preservation checks compare every vertex buffer, all eight UV channels, topology/submeshes, weights/bindposes, all 29 original BlendShape frame names/weights/position-normal-tangent data, materials, Avatar/controller references and all 51 bone paths/local transforms exactly. The added BlinkBoth alone contains the twenty sampled frames. Maximum moving-normal difference in Unity is 0.027977°. Maximum independently skinned world-space endpoint displacement error is **0.000197480 mm** across Idle/Walk/Run phases. Oral/eyeball islands receive zero BlinkBoth displacement.

The runtime tests exercise 0/25/50/75/100/restored 0, finite geometry, normal/tangent data, all sampled frames, intermediate and closed weights during Idle/Walk/Run, Animator retention and exact restoration within fixed body poses. Supplied SmileLeft/Right and JawOpen coexist additively (maximum mesh-local residuals 9.6043e−10 and 1.8629e−9 respectively). The raw supplied shape names, including `mothShrugtLower` and `mothShrugtUpper`, remain unchanged. Future semantic family bindings should map intent to these raw channels rather than renaming or assuming cross-family numeric indices.

Actual URP neutral, partial and closed front/oblique captures were inspected, together with all 49 slow-sweep frames and all 61 normal-speed frames. The newly introduced linear-chord artifact is absent from the sampled trajectory. No new gross fold, lid collapse, seam jump, reopening artifact or shading discontinuity was identified in these sequences. The accepted corner/contour limitations remain visible. Baseline, zero-weight candidate and restored neutral captures are **pixel-identical** in face, eye and oblique views. Body animation and lower-face coexistence views were also inspected. These are technical/reference findings, not a new final-art verdict.

The existing technical key/fill lights both already use `LightShadows.None`; the recorded default/no-shadows diagnostic is therefore equivalent. This does not certify a shadowed production lighting or final skin/eye/material setup. No Player build, crowd performance benchmark or final facial-art certification is claimed.

The full person fixture passed actual Girl EmployeeAgent/NavMesh Walk → arrival → Idle, Run, animated leg deformation, identity retention, physics hover/click selection, hold-to-carry and held presentation, invalid drop retention, valid drop, contextual Practice, Escape restoration, Socialize and distinct participant positions (measured separation **1.129928 m**). BlinkBoth remains active through the requested interactions. Camera keyboard pan/rotation and mouse-wheel zoom pass while carried; the actual management-camera capture contains the Girl visual and was inspected. The existing held Idle/sway is a compatibility demonstration, not authored child carry animation.

### Girl stature and future profile

Girl independently confirms `EmployeeAgent.Awake` applies height **2 m**, base offset **1 m** and radius **0.35 m** to the existing navigation/selection representation. Her native bald body remains **1.460771 m**; selection-capsule height excess is **0.5392288 m**. The isolated prefab uses the established visual-root compensation below the navigation origin and derives the suspension pivot from **Girl's own rest-pose Neck**, approximately `(0, 1.19, −0.08)` m. The test compares this against her own baseline Neck, not Boy's numeric range or an animated pose. The fixture navigation surface lies at +0.03333333 m; visual origin, feet convention and selection marker are checked against that sampled surface, rather than incorrectly asserting world zero.

No production navigation, capsule, selection or EmployeeAgent policy changed. Girl strengthens the recommendation for an explicit per-person/per-presentation profile containing physical height, navigation height/base offset/radius, capsule height/center, selection target and held suspension pivot. Native stature and authored anatomy should remain independent of this interaction policy. The known Walk/Run ground penetration remains a separate retarget/contact follow-up; no locomotion repair was attempted.

### Reproduction and evidence

All paths below are relative to `TestResults/GirlFoundation/` unless qualified. Reproduce on the existing licensed inputs using these reviewable tools in order:

1. `export_girl_bald.py` and `prepare_girl_native_subset.py`, then `GirlFoundationPrototypeTools`, produce the independently proved retained baseline (already preserved here).
2. `calibrate_girl_blink.py` with the Girl candidate configuration reproduces approved ReviewG2 through the original complete rig. `Accepted/user-acceptance.json` binds approval to its exact endpoint hash.
3. `sample_girl_blink_trajectory.py --evidence TestResults/GirlFoundation/Contour/ReviewG2 --acceptance TestResults/GirlFoundation/Accepted/user-acceptance.json --output TestResults/GirlFoundation/Trajectory` runs under Blender and records the unchanged source trajectory.
4. `bake_girl_blink.py --baseline TestResults/GirlFoundation/Bald/GirlBaldBaseline.blend --source-evidence TestResults/GirlFoundation/Contour/ReviewG2 --acceptance TestResults/GirlFoundation/Accepted/user-acceptance.json --source-samples TestResults/GirlFoundation/Trajectory/source-trajectory.npz --output TestResults/GirlFoundation/BlinkBlenderSampled` applies the proved delta transfer. Omitting `--source-samples` reproduces the rejected linear-trajectory experiment, not the accepted runtime candidate.
5. `GirlBlinkPrototypeTools.ExportNativeReference()` reads the existing retained mesh. `prepare_girl_blink_unity.py --diagnostic-only` measures Girl's own differences; `UnityBlink/calibrated-guards.json` records the justified guards above. Running without that flag generates the sampled packet. `GirlBlinkPrototypeTools.Build()` appends BlinkBoth to an isolated native clone and builds the reference prefab.
6. Run exact fixtures `SilverScreen.Tests.EditMode.GirlBlinkPrototypeTests` and `SilverScreen.Tests.EditMode.GirlBlinkPersonPrototypeTests` through the existing test runner, starting from a clean scene. Do not edit scripts during a run.

Key final evidence:

- `Accepted/user-acceptance.json`, `blender-gate.json`, `unity-validation-summary.json`, `state-verification.json`.
- `Accepted/blink-tests.xml` and `person-tests.xml` (plus their summaries); original `bald-tests.xml` remains unchanged.
- `UnityBlink/mapping.json`, `native-regions.json`, `native-mapping.npz`, `blink-addition.bin`, `construction.txt`, `native-data-validation.txt`, `runtime-motion-validation.txt`, `lower-face-validation.txt`.
- `UnityBlink/person-navigation.txt`, `person-selection-carry.txt`, `person-social-proof.txt`, `person-height-camera.txt`, `person-locomotion-trace.txt`, `person/held-camera.png`.
- `UnityBlink/girl-unity-review.png`, `girl-body-coexist-review.png`, `girl-blink-slow.mp4`, `girl-blink-normal.mp4`, `sweep-all-frames.png`, `normal-speed-all-frames.png`.
- `UnityBlink/stills/`, `baseline/`, `lighting/`, `body/`, `coexist/`, `sweep/` and `normal-speed/` contain the original actual URP frames; `animation-timeline.csv` records weights/times.
- `Accepted/editor-final-rpc.json` confirms successful compilation, Edit Mode and exactly one clean `Assets/Scenes/Studio.unity`. `console-final-rpc.json` records **zero errors and zero warnings**. Initial refresh/discovery timeout receipts are retained; refocusing the existing Editor and binding its actual process restored relay access. No Editor restart or scene discard was required.

### Final scope and repository safety

All four canonical families now have isolated reference-level evidence. They remain separate topology/UV families. ReviewG2 is foundation-approved only; final eyelid contour/corner polish, eye/skin materials, shadowed production review, contact quality, presentation profiles and production adoption remain future work. No gaze, brows, independent BlinkLeft/BlinkRight, automatic blinking, hairstyle system, Star Maker, production Character Runtime V1 or capsule replacement was started.

Current branch: **master**. Starting and ending HEAD: **`e384eed6ca851a65e5932f0e5ed2d11619073d6c`**. `git fetch origin` succeeded before this continuation; starting HEAD matched `origin/master`. **HEAD did not move.** No staging, commit, push, branch/worktree switch, reset, stash or history rewrite occurred during Girl work.

`Accepted/state-verification.json` confirms all **85 purchased files** preserve SHA-256, size and mtime; all **4,535 prior family/asset/evidence files** and **318 pre-continuation Girl files** preserve their hashes. Original G1/G2 evidence, the bald baseline and comparison experiments remain available. All generated Girl assets/evidence are ignored. `git diff --check`, new-text whitespace checks and Python parsing pass.

Exact final non-ignored working-tree status (24 unstaged/untracked files):

```text
 M .gitignore
 M Docs/CharacterBaseAudit.md
?? Assets/Editor/Characters/GirlBlinkPrototypeTools.cs
?? Assets/Editor/Characters/GirlBlinkPrototypeTools.cs.meta
?? Assets/Editor/Characters/GirlFoundationPrototypeTools.cs
?? Assets/Editor/Characters/GirlFoundationPrototypeTools.cs.meta
?? Assets/Tests/Editor/GirlBlinkPersonPrototypeTests.cs
?? Assets/Tests/Editor/GirlBlinkPersonPrototypeTests.cs.meta
?? Assets/Tests/Editor/GirlBlinkPrototypeTests.cs
?? Assets/Tests/Editor/GirlBlinkPrototypeTests.cs.meta
?? Assets/Tests/Editor/GirlCanonicalFoundationTests.cs
?? Assets/Tests/Editor/GirlCanonicalFoundationTests.cs.meta
?? Docs/CharacterBaseAuditTools/bake_girl_blink.py
?? Docs/CharacterBaseAuditTools/calibrate_girl_blink.py
?? Docs/CharacterBaseAuditTools/export_girl_bald.py
?? Docs/CharacterBaseAuditTools/girl_blink_candidates.json
?? Docs/CharacterBaseAuditTools/girl_topology_correspondence.py
?? Docs/CharacterBaseAuditTools/inspect_girl_blink_contact.py
?? Docs/CharacterBaseAuditTools/package_girl_review.py
?? Docs/CharacterBaseAuditTools/prepare_girl_blink_unity.py
?? Docs/CharacterBaseAuditTools/prepare_girl_native_subset.py
?? Docs/CharacterBaseAuditTools/render_girl_blink.py
?? Docs/CharacterBaseAuditTools/sample_girl_blink_trajectory.py
?? Docs/GirlCanonicalReferenceValidation.md
```
