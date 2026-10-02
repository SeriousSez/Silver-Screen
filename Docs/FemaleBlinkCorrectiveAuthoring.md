# Adult Female blink corrective authoring

**2026-10-02 — NOT ACCEPTED. Continue localized corrective authoring.**

The clean source pose reproduced exactly. A reproducible, deliberately authored correction on an isolated bald derivative removes the measured eye opening, but the corner contour still fails close-up inspection. No candidate is approved for the Unity gate. Combinations, independent blinks and Unity work were not started.

This is technical deformation evidence, not final art approval. A passing visibility or preservation check must not be presented as an accepted facial shape.

## Source and transfer

The input was the purchased `StylizedFemaleBodyBaseMesh3DModelFacialRig.blend` in `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/stylized-female-body-base/Stylized_Female_Body_Base_Facial_Rig/Facial Rig/`. Blender 5.0.1 evaluated the complete source rig through its dependency graph. No DEF-bone approximation was used, and the purchased file was never saved.

Both eyes used the same clean `Refine/t30_b8_ot3_ob3` controls:

| Control | Armature-axis Z translation |
|---|---:|
| `lid.T.L.002`, `lid.T.R.002` | -30 mm |
| `lid.B.L.002`, `lid.B.R.002` | +8 mm |
| `lid.T.L.001`, `lid.T.R.001` | -3 mm additional outer upper |
| `lid.B.L.003`, `lid.B.R.003` | +3 mm additional outer lower |
| Inner additional offsets | 0 |

The evaluated neutral and clean arrays equal the prior calibration evidence exactly: **0 mm maximum repeat error**, valid evaluated drivers, exact neutral restoration, and **0 crossings in the unchanged original 600-triangle exterior test**. The established 0.2 mm first-hit grid reproduced front exposure of 0 for both eyes and oblique exposure of Right **0.16 mm² / 0.2 mm span**, Left **4.00 mm² / 1.4 mm span**. The aggressive rejected endpoint was not used.

The existing generated `TestResults/FemaleBaldBase/Generated/AdultFemaleBald.blend` was copied in memory. Its SHA-256 remains `92d04cc9c58e7fa923041b64c7a6c990f040bc0321dbd21bdf7245f4baec79e6`. No hair-removal logic was changed. The with-hair prototype, bald comparison, rejected hybrid and previous blink evidence remain intact.

The established transfer arithmetic is unchanged: evaluated source closed minus source neutral, converted through the inverse baseline world linear transform, then added to unchanged Basis. The new builder imports the existing bake's evaluation and fingerprint helpers. Source displacement-transfer maximum error is **0.000008330 mm**. Authored deltas are a separately recorded addition; they are not claimed to originate from the purchased rig.

## Authored correction and failed candidates

The authoring tool targets four existing 27-vertex eyelid rings, with a two-ring harmonic displacement falloff into adjacent orbital skin. The seam curves, contact-depth slope, ring volumes, correspondence columns, explicit corner adjustments and relaxation operations are authored choices recorded in JSON. Cubic Hermite interpolation and fixed relaxation iterations make those choices reproducible. There are no unrecorded manual sculpt operations.

All recipes, displacement ledgers and geometry arrays remain under `C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/`. The portable review recipe is [female_blink_corrective_recipe.json](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/female_blink_corrective_recipe.json>), explicitly marked rejected.

| Iteration | Change tested | Max corrective mm | Original exterior crossing pairs | Minimum checked area / neutral | Left oblique exposure mm² |
|---|---|---:|---:|---:|---:|
| 01 | Shared seam and ring volumes | 6.333 | 0 | 0.0201 | 4.60 |
| 02 | Preserve canthus volume; overlap lips | 6.333 | 78 | 0.2092 | 0.60 |
| 03 | Sloping contact surface | 7.096 | 28 | 0.0896 | 0.24 |
| 04 | Align contact columns | 7.066 | 0 | 0.0881 | 0.24 |
| 05 | Corner contact and lid volume | 7.066 | 2 | 0.1304 | 0 |
| 06 | Outer curvature adjustment | 7.066 | 0 | 0.1461 | 0 |
| 07 | Align outer cross-section | 7.066 | 0 | 0.1453 | 0 |
| 08 | Local surface relaxation | 7.066 | 0 | 0.1874 | 0 |
| 09 | Bring outer contact forward | 7.071 | 0 | 0.1504 | 0 |
| 10 | Explicit canthus cap | 7.071 | 0 | 0.1785 | 0 |
| 11 | 50% of iteration 09 correction | 3.535 | 24 | 0.3154 | 1.16 |
| 12 | 75% of iteration 09 correction | 5.303 | 18 | 0.2524 | 0.04 |
| 13 | 90% of iteration 09 correction | 6.364 | 0 | 0.2144 | 0 |

The table uses the unchanged original test, with a 0.2 mm exposure grid except iteration 07's 0.05 mm grid. It is not an acceptance table. Iterations 01–04 failed coverage, collapse or crossing checks. Iteration 05 still crossed. Several later variants passed those narrow metrics but retained a visibly angular or raised corner; iteration 10 made this especially obvious. Iterations 11–12 were rejected geometrically without visual acceptance testing.

**Iteration 13 is the smallest tested amplitude that passes the original coverage/crossing checks, not a proven minimum or an accepted sculpt.** It is saved as `ReviewCandidate90`. The full-strength iteration 09 is retained separately as `ReviewCandidate`; it has fewer contacts in the expanded socket diagnostic described below. Neither is recommended as a runtime asset.

The review-90 corrective changes **298 vertices, 149 per side**. Maximum displacement is **6.363651 mm**, RMS over changed vertices **2.999266 mm**; maximum complete blink displacement from neutral is **23.584867 mm**. The affected neutral-space bounds are X ±67.073 mm, Y -44.690 to -14.145 mm, Z 1648.814 to 1689.104 mm. These are lids/corners and immediately adjacent orbital skin. Exact IDs, per-vertex XYZ deltas, mirrored pair IDs and ring membership are in `Iteration13/authoring.json`; `correction.npz` records the input geometry and world-space deltas.

The authored target is bilateral, using neutral-position correspondence and mirrored target positions, then the recorded 90% strength. There is no intentional left/right endpoint asymmetry. Sub-micron inherited numerical differences remain. No eyes or oral geometry were moved by the corrective, and no vertices outside its explicit ledger changed relative to the uncorrected bake.

## Eye coverage and contact

These measurements are from the **actual evaluated ReviewCandidate90 Blender shape**, using the established full-mesh first-hit BVH and original front/oblique cameras. The refined grid spacing is **0.05 mm**; area quantum is 0.0025 mm². No image colour, shadow segmentation or eye-material masking was used.

| View / eye | Neutral area mm² | Clean uncorrected area mm² | Corrected area mm² | Corrected remaining | Corrected maximum span |
|---|---:|---:|---:|---:|---:|
| Front / Right | 855.3100 | 0 | 0 | 0% sampled | 0 mm sampled |
| Front / Left | 855.3500 | 0 | 0 | 0% sampled | 0 mm sampled |
| Oblique / Right | 688.3225 | 0.1500 | 0 | 0% sampled | 0 mm sampled |
| Oblique / Left | 863.0025 | 4.1200 | 0 | 0% sampled | 0 mm sampled |

The refined uncorrected maximum spans are 0.30 mm Right and 1.35 mm Left oblique. Differences from the established 0.2 mm grid are sampling differences, not a changed pose. No residual sampled eyeball opening remains in the corrected endpoint at these cameras; this does not prove continuous zero exposure at every angle.

The unchanged original exterior gate on the evaluated review-90 endpoint reports:

- **0 nonadjacent crossing pairs** across its 600 triangles.
- Minimum area ratio **0.214437**; **0 triangles below 0.1 of neutral area**.
- Maximum normal rotation **97.631°**; 4 triangles over 90°, none over 120°. Rotation during closure is not itself proof of inversion.
- The 186 checked lid vertices per eye have minimum signed nearest-eye-plane distance **+0.269946 mm Right / +0.269972 mm Left**, with none inside the eye by more than 0.1 mm.

The additional diagnostic examines **all 714 triangles incident to authored vertices**, including socket/canthus faces omitted by the original mask. It also checks pairs sharing only one vertex. Seven triangle probes and direct intersection-point rays distinguish a partly visible triangle from an actually visible intersection location. This extension supplements the original check; it does not weaken or replace it.

| Expanded diagnostic | Neutral | Clean uncorrected | Review-90 corrected | Full-strength 09 corrected |
|---|---:|---:|---:|---:|
| Crossing pairs touching a partly exterior triangle | 0 | 82 | 34 | 6 |
| Crossing pairs between hidden triangles | 0 | 33 | 46 | 34 |
| Pairs with a visible tested intersection location | 0 | 2 | 0 | 0 |

Thus, the old description “crossing-free” applies specifically to the established exterior mask. Exact reproduction passed that test, but the expanded test exposes additional source-pose/socket contacts. Do not generalize the old zero to the whole lid. **The corrected endpoint is not certified intersection-free**: substantial hidden contacts remain, and ray-tested invisibility at two cameras is not a safety guarantee for future views or animation.

For review-90, expanded actually-visible contact samples have minimum signed eye-plane distances **+0.7825 mm Left / +1.2201 mm Right**, with no sampled visible penetration beyond 0.1 mm. Samples from hidden portions of partly visible triangles reach **-2.6424 / -3.1802 mm**. Neutral contains hidden socket samples as deep as about -5.90 mm. Those hidden values must not be described as external skin penetration, nor silently discarded. Cross-section plots show the internal lining/contact that still needs authoring review.

Across all 714 local triangles, the corrected minimum area ratio remains 0.214437. Two hidden triangles rotate more than 120° (maximum 144.874°); these diagnostics and the hidden crossings remain unresolved. No all-angle, subframe, or whole-animation certification is claimed.

## Visual decision

Matched neutral, uncorrected and corrected grey technical renders were generated and inspected. The front/oblique views read as closed eyes. The large stepped gap is reduced, and the eyeball opening is covered. At full-face and conversation scale the result can look plausible.

At the 0.069 m eye close-up, however, the anatomical Left outer corner retains a raised/angular crease, and the inner termination remains pinched. The matching Right eye has the corresponding contour limitation under different lighting. Local smoothing and cap variants did not produce a convincingly natural surface without other defects. **This fails the requested movie-close-up acceptance standard.** Normal rotation counts and a green zero-exposure result do not override that observation.

The gameplay-scale (2.4 m orthographic width), conversation-scale (0.65 m), face (0.35 m) and eye (0.069 m) captures are Blender technical scale comparisons. They are not claims about the current game's real camera/rendering setup; Unity validation was not entered.

Useful review evidence:

- [Neutral front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/front-neutral.png>) and [corrected front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/front-corrected.png>).
- [Neutral oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/oblique-neutral.png>), [clean uncorrected oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/oblique-uncorrected.png>) and [corrected oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/oblique-corrected.png>).
- [Clean Left close-up](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/eye-L-uncorrected.png>), [corrected Left close-up](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/eye-L-corrected.png>) and [corrected Right close-up](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/eye-R-corrected.png>).
- [Face scale](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/face-corrected.png>), [conversation scale](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/Distances/conversation-scale-corrected.png>) and [gameplay scale](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/Distances/gameplay-scale-corrected.png>).
- [Geometric sections, neutral / clean / corrected](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/ReviewCandidate90/eyelid-sections.png>).

## Preservation and surrounding deformation

The isolated generated file contains 16,497 bald mesh vertices, 16,559 polygons, the unchanged 51-bone armature and 29 supplied expressions plus one experimental `BlinkBoth`. The review file is saved with BlinkBoth at zero; select `FemaleBaldBlinkCorrective_Experimental` and set its key to 1 to inspect the rejected endpoint. Hidden baseline comparison objects in this Blender authoring file are not a proposed runtime representation.

**All 15 preservation assertions pass.** The final portable recipe was replayed and reproduced the recorded baseline and authored delta arrays exactly (`recipe-replay-check.json`). Neutral restoration is exactly 0 mm. Original vertex/Basis, topology, UV, corner-normal, skinning and all 30 pre-existing key-block fingerprints (Basis plus 29 expressions) match. Bone names, hierarchy, rest matrices and pose matrices match. The baseline object/armature transforms and copied modifier binding are retained. The authored correction is confined to its ledger; eyes and oral geometry remain identical to the uncorrected bake. Authored-delta evaluation maximum error is **0.000062316 mm**, below the explicit 0.0001 mm floating-point tolerance. These results verify transfer and preservation, not shape acceptance.

The complete source blink already moves some surrounding tissue. The visible nose/medial region reaches **4.463457 mm** at vertex 2764, neutral position approximately (-11.478, -38.018, 1672.234) mm, near the medial eye/upper nose. Visible lower-cheek movement reaches **0.991719 mm**. The broader cheek box includes hidden interior points reaching 9.040634 mm. The corrective adds **0 mm** to these measured nose/cheek sets; the checked brow-above-eye set remains unchanged. This inherited source motion is reported explicitly, rather than claiming all blink motion is confined to the authored mask. Full-face inspection showed no gross new nose/mouth distortion, but final anatomy approval was not reached.

The known invalid Unity channels remain unchanged and untested here: `mouthFrownLeft`, `mouthDimpleLeft`, `mouthStretchLeft`, `mouthShrugtLower`, `mouthShrugtUpper`, `mouthLowerDownLeft`. Their presence is not a claim of supported behavior. The data-preservation assertions include them without repairing or driving them.

Because BlinkBoth failed acceptance, **Smile/JawOpen combinations, BlinkLeft/BlinkRight derivation, additive comparison, Unity import/Humanoid/Idle/Walk/Run/shading checks and runtime memory measurements were not reached**. No renderer-count or new Unity compatibility claim is made. The known native-data preservation approach remains the appropriate later Unity path; no raw FBX round trip was introduced.

## Follow-up and family binding

The next work is targeted canthus/contact authoring on generated geometry: inspect the partly hidden corner/lining triangles and their adjacent faces, improve the inner and outer terminations, then repeat the original and expanded checks plus close-ups. The source transfer does not need redesign. More runtime bones would not resolve this authored surface problem. The 13 recipes and failed comparisons should remain available rather than replacing evidence with an apparent success.

This recipe is **strongly tied to the default Adult Female anatomy**: it uses this family's ring indices, correspondence and authored world-space curves. Moderate identity changes have not been tested. Eye size, opening, width, depth, tilt or asymmetry may require transported/regenerated deltas and additional identity-specific corrective authoring. Do not promise universal composition. No Star Maker eye controls were implemented.

Retain the semantic facial-shape mapping recommendation: bindings should explicitly declare supported channels, family-specific shape targets and calibrated values, rather than infer capability from inconsistent supplied names. Male, Female, Boy and Girl remain separate canonical topology/UV families. Child hierarchy/export validation remains a separate required follow-up; no child integration occurred. Bald geometry remains the canonical target, with future modular appearance attachments outside this milestone.

## Files and reproduction

All generated evidence is local and ignored by the existing `TestResults` policy. [evidence-manifest.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCorrective/evidence-manifest.json>) enumerates every generated evidence file with its exact repository-relative path, byte size and SHA-256 (excluding the manifest itself). Important entries under `TestResults/FemaleBlinkCorrective/` are:

| Path | Purpose |
|---|---|
| `baseline.json` | Starting master/HEAD/origin state and 672 prior-file hashes |
| `Source/reproduction-gate.json`, `calibration.json`, `surface-check.json` | Exact source reproduction and original gate |
| `Source/neutral.npz`, `clean.npz` | Complete evaluated source geometry |
| `source-reproduction.json`, `recipe-01.json` through `recipe-13.json` | Control inputs and authored choices |
| `Uncorrected/` | Unchanged source-delta bake on copied bald baseline |
| `Iteration01/` through `Iteration13/` | Per-candidate deltas, vertex ledger, topology, measurements and available renders |
| `ReviewCandidate/FemaleBaldBlinkCorrective.blend` | Rejected full-strength iteration 09 comparison |
| `ReviewCandidate90/FemaleBaldBlinkCorrective.blend` | Rejected 90% review derivative |
| `ReviewCandidate90/build.json`, `deformation.npz` | Transfer and preservation evidence |
| `ReviewCandidate90/visibility-0.05mm.json` | Fine-grid endpoint/neutral/clean measurements |
| `ReviewCandidate90/surface-check.json`, `expanded-geometry.json` | Original and expanded geometric diagnostics |
| `ReviewCandidate90/preservation-check.json`, `acceptance.json` | Passing preservation, explicitly failed acceptance |
| `ReviewCandidate90/capture-settings.json`, `Distances/capture-settings.json` | Exact technical camera/render settings |
| `repository-final.json` | Final branch/HEAD/status and whitespace checks |

Run the following from `C:/Repos/Unity/Silver Screen`, with the previously validated source evidence present. Use a new output folder to retain the recorded comparisons. The recipe includes the exact source controls; the recorded source reproduction used the unchanged `calibrate_female_blink.py` and `check_female_blink_surface.py`, verified array equality against the previous Refine endpoint, then wrote the reproduction gate.

```powershell
$blinkBlender = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
python -B Docs/CharacterBaseAuditTools/author_female_blink_corrective.py --evidence TestResults/FemaleBlinkCorrective/Uncorrected --recipe Docs/CharacterBaseAuditTools/female_blink_corrective_recipe.json --output TestResults/FemaleBlinkCorrective/ReproducedAuthoring
& $blinkBlender --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/build_female_blink_corrective.py -- --bald-baseline TestResults/FemaleBaldBase/Generated/AdultFemaleBald.blend --source-evidence TestResults/FemaleBlinkCorrective/Source --correction TestResults/FemaleBlinkCorrective/ReproducedAuthoring/correction.npz --output TestResults/FemaleBlinkCorrective/ReproducedReview
& $blinkBlender --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/check_female_blink_surface.py -- --evidence TestResults/FemaleBlinkCorrective/ReproducedReview --names neutral uncorrected corrected
& $blinkBlender --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/measure_female_corrective.py -- --evidence TestResults/FemaleBlinkCorrective/ReproducedReview --names neutral uncorrected corrected --step-mm 0.05
& $blinkBlender --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/validate_female_corrective_geometry.py -- --evidence TestResults/FemaleBlinkCorrective/ReproducedReview --names neutral uncorrected corrected --authoring TestResults/FemaleBlinkCorrective/ReproducedAuthoring/authoring.json
& $blinkBlender --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/render_female_blink_calibration.py -- --evidence TestResults/FemaleBlinkCorrective/ReproducedReview --names neutral uncorrected corrected --samples 32 --close
python -B Docs/CharacterBaseAuditTools/verify_female_blink_corrective.py --evidence TestResults/FemaleBlinkCorrective/ReproducedReview --authoring TestResults/FemaleBlinkCorrective/ReproducedAuthoring/authoring.json --snapshot TestResults/FemaleBlinkCorrective/baseline.json --inventory TestResults/CharacterBaseAudit/ChildSourceUpdate/inventory.json --source-root 'C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models'
```

The preservation command is scoped to this rejected experiment and reports NOT ACCEPTED independently of passing preservation. It is not an automatic acceptance or Unity-unlock tool. Blender process exit status alone was not used as evidence: JSON outputs and logs were inspected. Render logs contain a nonfatal pre-existing extension-cache warning; completed PNGs were verified visually.

## Repository and source safety

- Current branch: **master** throughout. No branch/worktree creation or switching.
- Starting HEAD: **2d12468ec2768c6b9ab014afee6912e53494d69c**.
- `git fetch origin` completed before editing; fetched `origin/master` equaled starting HEAD.
- Starting working tree: clean, including all untracked files.
- Ending HEAD: **2d12468ec2768c6b9ab014afee6912e53494d69c**. **HEAD did not move.**
- All **672** snapshotted previous character audit/prototype files retain their hashes.
- All **85** purchased inventory files retain their SHA-256, size and modification time.
- No tracked file is modified or staged. The exact non-ignored untracked files are below.
- `git diff --check`: passes with no output. Each new file also passes `git diff --no-index --check` against `NUL`, because ordinary `git diff` excludes untracked files.
- No commit, push, force-push, reset, stash, history rewrite or Unity asset change occurred.

```text
?? Docs/CharacterBaseAuditTools/author_female_blink_corrective.py
?? Docs/CharacterBaseAuditTools/build_female_blink_corrective.py
?? Docs/CharacterBaseAuditTools/female_blink_corrective_recipe.json
?? Docs/CharacterBaseAuditTools/measure_female_corrective.py
?? Docs/CharacterBaseAuditTools/validate_female_corrective_geometry.py
?? Docs/CharacterBaseAuditTools/verify_female_blink_corrective.py
?? Docs/FemaleBlinkCorrectiveAuthoring.md
```

**BLINK CORRECTIVE NOT ACCEPTED — CONTINUE LOCALIZED AUTHORING.**
