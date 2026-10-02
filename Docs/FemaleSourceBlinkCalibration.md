# Adult Female source BlinkBoth endpoint calibration

2026-10-02. Source-rig-only follow-up to [FemaleBakedUpperFaceExperiment.md](<C:/Repos/Unity/Silver Screen/Docs/FemaleBakedUpperFaceExperiment.md>). Blender 5.0.1; no Unity investigation or import.

## Decision

**No tested endpoint passes both coverage and natural lid-contact acceptance.** The rig can achieve almost complete geometric coverage, but the strongest useful candidates create intersecting eyelid surfaces and a pinched, angular seam in close-up. The best tested candidate without exterior lid crossings retains a visible outer-corner opening. Recommend a small authored eyelid corrective, followed by source acceptance, rather than more runtime rig complexity.

This is a bounded calibration result, not a mathematical proof that no possible arrangement of every source control could work. The selected pose below is an **unaccepted diagnostic endpoint**, preserved to make the remaining authoring problem concrete. It is not a production binding.

The proven delta-transfer method was **not changed or rerun**. No new runtime `BlinkBoth`, independent blink, gaze or brow capability was created. The rejected hybrid was untouched. Grey renders are technical evidence only, not final art approval.

## 1. Source authority and control ranges

The original complete rig was loaded from:

`C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models\stylized-female-body-base\Stylized_Female_Body_Base_Facial_Rig\Facial Rig\StylizedFemaleBodyBaseMesh3DModelFacialRig.blend`

Its SHA-256 remains `8a4dfe5cc3be6d201f2d2808ae6bee7031df8e721ecda1d1d2260e7bf0f4f0db`. Blender opened it with factory startup and embedded scripts disabled. Hidden source collections were exposed in memory, and all seven source mesh pieces were evaluated through the full dependency graph. No DEF-bone rotation approximation, rig reduction or geometry editing was used. Evaluated drivers were valid throughout the sweeps.

The meaningful eyelid controls have no authored translation limits. Central controls retain their normal eye-follow constraints; the lower central control also retains its cheek-follow constraint. The adjoining controls inherit 60% of central movement through offset Copy Location constraints. Their explicit offsets below are **additional control inputs**, not final evaluated world positions. Unchanged Damped Track and other mechanisms produce the evaluated skin deformation.

All translations use armature-axis directions converted through each control's local axes. The bounded search covered:

| Input, applied bilaterally | Values/range explored |
| --- | --- |
| Upper central `lid.T.[L/R].002`, Z | −22 to −36 mm; finer 24, 25, 26, 27 and 28 mm magnitudes around promising poses |
| Lower central `lid.B.[L/R].002`, Z | 0 to +16 mm, including 3, 5.5, 6, 7, 8, 9, 10 and 12 mm |
| Outer upper `lid.T.[L/R].001`, additional Z | 0, −3, −5, −6, −8 mm |
| Outer lower `lid.B.[L/R].003`, additional Z | 0, +3, +4, +6, +8 mm |
| Inner upper `lid.T.[L/R].003`, additional Z | 0, −1, −2 mm |
| Inner lower `lid.B.[L/R].001`, additional Z | 0, +1, +2 mm |
| Central upper/lower Y, depth checks | 0, −1, −2, −4 mm; negative Y is anterior in this source |

These were staged combinations, not a full Cartesian sweep. There were **130 distinct new endpoints**, plus neutral and the previous endpoint: 133 sweep evaluations including one repeated combination. Four poses were then remeasured on a finer grid. The [candidate matrix](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/candidate-matrix.md>) and [complete data](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/candidate-matrix.json>) record every tested combination, both-eye exposure in both cameras, spans, movement, surface diagnostics, crossings and reset checks. Representative candidates and all final comparisons were visually inspected; not every sweep row has a rendered acceptance assessment.

Increasing central travel alone reduced exposure but produced a low, angular central contour while leaving an outer-corner wedge. Sharing more motion with lower/outer controls reduced that wedge, at the cost of lid overlap and corner pinching. The depth checks did not resolve the contact problem. There is no justified single numeric “safe maximum”: safe use depends on the combination and evaluated surface.

## 2. Previous endpoint reproduction

The original −25.5 mm upper / +5.5 mm lower central-only endpoint and neutral arrays reproduce the prior experiment **exactly**. The established 0.2 mm BVH grid also reproduces its measurements exactly:

| Camera / eye | Neutral area, mm² | Previous closed area, mm² | Remaining | Maximum exposed span |
| --- | ---: | ---: | ---: | ---: |
| Front / Right | 855.56 | 94.56 | 11.05% | 5.8 mm |
| Front / Left | 855.88 | 94.40 | 11.03% | 6.0 mm |
| Oblique / Right | 688.00 | 50.04 | 7.27% | 2.8 mm |
| Oblique / Left | 863.32 | 134.48 | 15.58% | 6.4 mm |

The finer 0.05 mm recheck gives approximately 11.062% front exposure on each eye, with 5.85 mm spans, and 7.224% Right / 15.588% Left oblique exposure, with 2.80 / 6.45 mm spans. These small numerical differences are sampling resolution effects, not a changed pose. The old endpoint was a calibration starting point, not an authoritative target.

## 3–4. Selected diagnostic endpoint and bilateral calibration

Selected sweep identifier: `Balance/t26_b7_ot5_ob6_it1_ib2`, saved as `Final/selected.npz`. It balances low remaining exposure against the more severe overlap in the stronger alternatives. **It still fails acceptance.**

| Control | Left input Z | Right input Z |
| --- | ---: | ---: |
| Upper central `.002` | −26 mm | −26 mm |
| Lower central `.002` | +7 mm | +7 mm |
| Upper outer `.001`, additional offset | −5 mm | −5 mm |
| Lower outer `.003`, additional offset | +6 mm | +6 mm |
| Upper inner `.003`, additional offset | −1 mm | −1 mm |
| Lower inner `.001`, additional offset | +2 mm | +2 mm |

All selected X/Y inputs are zero. Other controls remain at the source neutral. Exact names and vectors are in [final-candidates.json](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/final-candidates.json>).

**No different left/right magnitude is justified by this evidence.** The selected local lid surfaces match across the sagittal mirror to within 0.000367 mm; the neutral comparison is 0.000375 mm. Each side has ten exterior triangle-crossing pairs. The measured oblique exposure difference follows camera position and sampling, not a meaningful source anatomical asymmetry. No independent blink was generated or evaluated.

## 5–7. Selected source exposure and span

Measurement uses the same front and oblique orthographic cameras as the previous experiment, the full evaluated character BVH and first-hit polygon ownership for each verified 354-vertex eyeball. Neither image colour nor shadow classifies exposure. The final grid spacing is **0.05 mm**, so each sample represents 0.0025 mm² of projected area.

| Camera / anatomical eye | Neutral visible area, mm² | Selected visible area, mm² | Remaining | Maximum exposed vertical span |
| --- | ---: | ---: | ---: | ---: |
| Front / Right | 855.3050 | 0 | 0% sampled | 0 mm sampled |
| Front / Left | 855.3475 | 0 | 0% sampled | 0 mm sampled |
| Oblique / Right | 688.3150 | 0.0225 | 0.003269% | 0.05 mm |
| Oblique / Left | 862.9950 | 0.0350 | 0.004056% | 0.10 mm |

Zero samples do not prove continuous mathematical closure. Both summed occupied-column span and full first-to-last occupied-column extent are recorded; they agree for these final poses.

The residual hits are **genuine visible eyeball geometry**: nine samples at the anatomical Right inner corner and fourteen at the Left outer corner in the oblique camera. They are not shadows. They are small enough that lashes or material treatment might conceal them, but no such treatment was supplied, tested or credited as closure. Residual hit positions and eye polygon IDs are retained in `Final/surface-check.json`. These tiny slivers alone are not the main reason for rejection; the contact defects are.

## 8. Visual and contact acceptance

| Matched source neutral | Selected, unaccepted BlinkBoth |
| --- | --- |
| ![Neutral front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/front-neutral.png>) | ![Selected front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/front-selected.png>) |
| ![Neutral oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/oblique-neutral.png>) | ![Selected oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/oblique-selected.png>) |

The selected pose reads as closed at the wider technical face view. In close-up, however, the contact line is angular and slightly wavy, with a ledge-like overlap and a pinched outer corner. The evaluated surfaces cross instead of forming a clean meeting seam. This is insufficient for the requested close-up movie-shot criterion. It is not rejected merely because a tiny nonzero exposure metric remains.

| Previous rejected endpoint, Left close-up | Selected endpoint, Left close-up |
| --- | --- |
| ![Previous eye](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/eye-L-previous.png>) | ![Selected eye](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/eye-L-selected.png>) |

Both [Right close-up](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/eye-R-selected.png>) and [full-face view](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/face-selected.png>) were inspected. The lighting makes the two sides look different, but numerical mirrored geometry is effectively symmetric.

Contact diagnostics support the visual finding:

- **20 nonadjacent exterior-lid triangle pairs cross**, ten per eye. Neutral and the previous endpoint have zero such pairs. BVH candidate pairs are confirmed by finite segment/triangle intersection; adjacent triangles sharing vertices are excluded. Pair IDs and intersection coordinates are saved.
- Of 186 lid vertices per eye individually visible at neutral, ten per eye move more than 0.1 mm inside the eyeball's nearest surface plane; five exceed 1 mm. The worst signed penetration estimate is **1.801 mm**. Neutral has no negative distances in this visible subset. These are signed nearest-plane diagnostics, not a complete volumetric penetration certificate.
- The strongest nominal-zero-exposure comparison has **44 crossing pairs** and roughly **2.214 mm** worst penetration. It reads closed but is a worse contact solution. Its name `zero-exposure-alternative` refers to the coarse-grid result; the fine grid still detects 0.0300 mm² on the oblique Right eye.
- Two of the 600 initially visible lid triangles rotate their normals more than 90 degrees in the selected pose; maximum rotation is 93.755 degrees. The smallest triangle retains 27.81% of its neutral area. Rotation alone is not proof of inversion, and no large turned-inside-out patch is claimed. The actual crossings and visible pinch are the stronger evidence.

![Geometric eyelid sections](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/eyelid-sections.png>)

These raw-source sections at anatomical Left x = 43 and 58 mm show the upper/lower relationship and eyeball surface without relying on shadows. The source socket lining already extends inside the eye at neutral; those hidden interior vertices are retained in the broad diagnostics but are not all counted as newly penetrating exterior skin.

For comparison, the best tested pose with **zero detected exterior crossing pairs** is `Refine/t30_b8_ot3_ob3`: central upper −30 / lower +8 mm; outer upper −3 / lower +3 mm; no extra inner offsets. Its front grid is covered, but the oblique Left eye still exposes **4.00 mm² with a 1.4 mm span** on the established 0.2 mm grid. The [outer-corner opening](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBlinkCalibration/Final/eye-L-no-crossing-alternative.png>) was inspected. It is a useful softer comparison for corrective authoring, not an accepted closed endpoint.

## 9. Surrounding deformation and restoration

For the selected endpoint:

| Region / check | Result |
| --- | --- |
| All 17,571 vertices | Maximum motion 22.943 mm; 623 vertices move more than 0.1 mm |
| 428 local lid-window vertices | Maximum 22.024 mm; mean 6.345 mm |
| 751 vertices carrying any lid weight | Maximum 22.943 mm; mean 4.628 mm |
| 12,186 body vertices without lid weights | Maximum 0.000404 mm; none moves more than 0.1 mm |
| Separate eyeballs, oral pieces and hair | Exactly zero movement |
| Visible lower-cheek spatial sample | Maximum 1.083 mm; mean 0.060 mm |
| Visible medial nose/bridge spatial sample | Maximum 4.069 mm; mean 0.158 mm |
| Sample above the eye/brow | Exactly zero movement; no brow controls were used |
| Neutral reset before each candidate and after each source sweep | Exact array equality; zero maximum error |

The largest nose/bridge sample is a medial orbital vertex near x = −11.48, y = −38.02, z = 1672.23 mm, which moves about 3.71 mm downward and 1.67 mm posterior-to-anterior. It is not the nose tip. This pull is recorded rather than dismissed. The full-face comparison does not show a broad cheek/nose collapse or mouth contamination; local eye-corner/bridge deformation would still need review with a corrective. The main rejection remains lid contact and contour.

Raw spatial and source-weight groups include overlapping eyelid and hidden socket vertices, so their maxima cannot be casually labelled “nose movement” or “cheek crushing.” Both raw sets and directly visible skin subsets are retained in the evidence.

## 10–13. Acceptance and bake gate

| Gate | Result |
| --- | --- |
| Source BlinkBoth accepted | **No** — close-up contact and contour fail |
| New experimental BlinkBoth re-bake | **Not reached** |
| New baked displacement parity / coverage | Not measured; no new bake exists |
| Lightweight baseline, existing 29 keys and 51-bone skeleton | Existing files preserved; no modification or re-import attempted |
| Existing proven bake/transfer implementation | Unchanged, hash verified |
| Source neutral restoration | Exact in every source sweep |
| Unity, independent blinks, gaze/brows, hybrid work | Not begun |

The evidence supports an authored correction of the closed lid seam and corner shape, with explicit eyeball/contact checks. It does not support changing the accurate transfer arithmetic or increasing runtime bone complexity. No corrective was authored in this task and no further milestone was started.

## 14. Exact evidence and reproduction

All generated licensed arrays, images and detailed evidence are local and Git-ignored under `TestResults/FemaleBlinkCalibration/`. No new `.blend`, FBX or Unity asset was generated.

- `baseline.json`, `preservation.json`: initial repository state and final preservation results.
- `coarse-candidates.json`, `refine-candidates.json`, `fine-candidates.json`, `contact-candidates.json`, `balance-candidates.json`, `final-candidates.json`: exact control recipes. Every recipe poses both eyes together.
- `candidate-matrix.json`, `candidate-matrix.md`: all 133 sweep evaluations and geometric diagnostics.
- `Coarse/`, `Refine/`, `Fine/`, `Contact/`, `Balance/`: `calibration.json`, `surface-check.json`, `surface-regions.npz`, `neutral.npz`, `topology.json`, `render-settings.json`, named evaluated pose arrays and per-camera hit masks; representative rendered comparisons where captured.
- `Final/calibration.json`: neutral/previous/selected/stronger-alternative metrics on the 0.05 mm grid.
- `Final/surface-check.json`, `Final/surface-regions.npz`: contact pairs, penetration diagnostics, spatial regions, mirror parity and exact sparse residual-eye hit coordinates.
- `Final/neutral.npz`, `previous.npz`, `selected.npz`, `zero-exposure-alternative.npz`, `no-crossing-alternative.npz`: evaluated source geometry only; these are not baked runtime shapes.
- `Final/{front,oblique,eye-L,eye-R,face}-{neutral,previous,selected,zero-exposure-alternative}.png`: 20 matched technical views; `capture-provenance.json` records original captures, hashes and exact evaluated-array equality. Five further `*-no-crossing-alternative.png` views record the softer comparison.
- `Final/{neutral,previous,selected,zero-exposure-alternative}-{front,oblique}-hits.npz`: geometric visibility masks. `Final/eyelid-sections.png`: geometric cross-sections.
- `*-render.log`, `*-surface.log` and sweep logs retain processing diagnostics. Blender's source-file embedded-script warnings do not indicate invalid evaluated drivers.

Source-only tools added: [calibration](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/calibrate_female_blink.py>), [matched rendering](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/render_female_blink_calibration.py>), [surface/contact checks](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/check_female_blink_surface.py>), [section plotting](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/plot_female_blink_sections.py>).

From the existing project root, reproduce into a new ignored review directory to retain this comparison evidence:

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
$femaleSource = 'C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models\stylized-female-body-base\Stylized_Female_Body_Base_Facial_Rig\Facial Rig\StylizedFemaleBodyBaseMesh3DModelFacialRig.blend'
$reviewDir = 'TestResults/FemaleBlinkCalibration/Recheck'
& $blenderExe --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/calibrate_female_blink.py -- --source $femaleSource --previous TestResults/FemaleBakedUpperFace/Blender --candidates TestResults/FemaleBlinkCalibration/final-candidates.json --output $reviewDir --step-mm .05
& $blenderExe --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/check_female_blink_surface.py -- --evidence $reviewDir --names neutral previous selected zero-exposure-alternative
& $blenderExe --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/render_female_blink_calibration.py -- --evidence $reviewDir --names neutral previous selected --samples 32 --close
python Docs/CharacterBaseAuditTools/plot_female_blink_sections.py --evidence $reviewDir --names neutral previous selected
```

The final plotting command uses host Python with numpy/Pillow. Earlier stages use the corresponding recipe JSON and a 0.2 mm grid. The original bake script is not part of these commands.

## 15–17. Preservation and repository status

All **85 purchased source files** match the inventory SHA-256, byte size and modification time. All **96 pre-existing prototype, bake, audit-tool and evidence files** in this task's preservation baseline retain their hashes. The purchased `.blend` was never saved. Earlier rejected endpoints, baked evidence, the 95-bone hybrid and the bald comparison remain intact.

Worktree: `C:\Repos\Unity\Silver Screen`. Branch: `fix/unrestricted-applicant-hiring`. HEAD remains `9aa2b98d491cfcce6706c7777874307c8da575ed`. No branch/worktree was created or switched; other existing worktrees were untouched. Previous uncommitted character work remains. This task adds four source-audit Python tools, this report and project-context notes; generated evidence stays ignored. The pre-existing `ProjectSettings/ProjectSettings.asset` status entry still has bytes equal to HEAD.

`git diff --check` passes. No commit, push or PR was made. No Unity tooling was called in this follow-up, so no new Unity validation result is claimed.

CONTINUE INVESTIGATION — SOURCE BLINK NEEDS CORRECTIVE AUTHORING
