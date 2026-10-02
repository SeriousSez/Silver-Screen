# Adult Female baked upper-face acceptance experiment

2026-10-02. Narrow follow-up to [the facial runtime investigation](FemaleFaceRuntimeInvestigation.md).

**The Blender acceptance gate failed on eyelid/eyeball coverage.** The single baked `BlinkBoth` preserves the lightweight baseline and transfers the source deformation accurately. However, closer matched renders and geometric visibility tests show that the previously accepted **source endpoint itself leaves both eyeballs partly exposed**. The bake faithfully retains that opening. The earlier report's complete-closure claim was overstated and has been corrected.

The experiment stops here under the requested contact/coverage stop condition. No generated candidate was imported into Unity. Lower-face combinations, independent baked blinks, eyes/gaze and brows were not attempted. The rejected 95-bone hybrid was not modified. All grey views are technical evidence only, never art approval.

## 1–2. Controlled baseline and transfer method

The baseline is the actual validated local runtime import:

`Assets/SilverScreen/Art/Characters/HumanBasePrototype/Models/AdultFemale.fbx`

Its SHA-256 is `5891b551d6ad4fb5c4d451c66301c14a27da77ac42e48a2eab82b772fc98e9e0`. The file, importer .meta, existing prototype prefab and locomotion controller were hashed before and after the experiment. They remain unchanged. The current Humanoid importer settings/mapping therefore remain unchanged; no new candidate Avatar is claimed validated.

The authority is the purchased original `stylized-female-body-base/Stylized_Female_Body_Base_Facial_Rig/Facial Rig/StylizedFemaleBodyBaseMesh3DModelFacialRig.blend`, under the previously inventoried external source root. Blender 5.0.1 opens it with factory startup and embedded scripts disabled. Collections/objects are exposed in memory, and the complete rig is evaluated through its dependency graph, including its constraints and drivers. No source bone reduction is used.

The exact prior endpoint is reused: for both sides, move `lid.T.L/R.002` down **25.5 mm** and `lid.B.L/R.002` up **5.5 mm**, converting those armature-aligned directions through the controls' local axes. Fresh evaluated Neutral and BlinkBoth arrays match the previous investigation's saved arrays with **zero maximum difference**. This is the same source endpoint, not a newly adjusted blink.

The seven source pieces map to the joined mesh through verified vertex-index offsets: body 0; tongue/lower/upper oral pieces 12,937, 13,153 and 14,377; right eye 15,789; left eye 16,143; hair 16,497. All **17,638 polygons** correspond, including their order in the imported lightweight baseline (cyclic direction normalized); both meshes have **17,571 vertices**.

For each corresponding vertex in a common world coordinate system:

```text
source_delta = evaluated_source_BlinkBoth - evaluated_source_Neutral
candidate_key_local = unchanged_baseline_Basis_local
                    + inverse(baseline_object_world_linear) * source_delta
```

Only one new key is created. The baseline Basis is never replaced with absolute source coordinates. This preserves the pre-existing neutral differences instead of silently changing the lightweight face. The resulting evaluated candidate displacement is checked against the source displacement, while a separate absolute-surface comparison retains and reports the residual neutral differences. Displacement agreement alone is not treated as contact acceptance.

## 3. Blender parity metrics

All values below are millimetres at the raw source scale, before the existing runtime's adult scale fitting. Means are per-vertex Euclidean distances, not area-weighted surface distances. No closest-point matching hides vertex correspondence errors.

| Comparison region | Vertices | Maximum absolute closed-surface difference | Mean difference | >0.1 mm | >0.5 mm | >1 mm | >2 mm |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Whole joined mesh | 17,571 | 5.807090 | 0.077593 | 871 | 612 | 450 | 249 |
| Body vertices with any source facial DEF weight | 3,935 | 2.176029 | 0.086899 | 485 | 240 | 133 | 10 |
| All source lid-weight vertices, either side | 751 | 1.637242 | 0.027427 | 43 | 8 | 4 | 0 |
| Local eye-window lid vertices, both sides | 428 | 0.182120 | 0.005417 | 6 | 0 | 0 | 0 |
| Right eye-window lid vertices | 214 | 0.182092 | 0.005459 | 3 | 0 | 0 | 0 |
| Left eye-window lid vertices | 214 | 0.182120 | 0.005375 | 3 | 0 | 0 | 0 |

The broad lid-weight region is defined by source `DEF-lid.*` weights above 0.00001 before examining the errors. It extends into cheek/internal regions well below the eye. To distinguish the actual eye neighbourhood, the local eye-window region intersects that membership with each source neutral eyeball's world bounding box expanded **4 mm on every axis**. Both broad and local results are retained; no high-error vertices are discarded from the broad result.

The whole-mesh 5.807 mm maximum already exists between the two neutral representations and occurs in oral geometry. The broad lid maximum also exists at neutral, below the eye window. The bake's **displacement-transfer maximum error is 0.000007451 mm**, with mean **0.0000000415 mm** over the whole mesh; in the broad lid region its mean is **0.000000959 mm**. Thus the large absolute numbers are not a recurrence of the hybrid's 7.155 mm deformation-transfer failure.

## 4. Visual comparison and actual eye coverage

Eight matched technical renders cover source/candidate, neutral/BlinkBoth, and front/oblique views. All use the same topology, smoothing, grey comparison surface, camera and lighting. They visualize evaluated geometry; they do not validate a Unity shader or new imported shape normals. The four closed views and neutral references were inspected.

| Original evaluated source | Lightweight plus baked BlinkBoth |
| --- | --- |
| ![Source front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBakedUpperFace/Blender/front-source-BlinkBoth.png>) | ![Baked front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBakedUpperFace/Blender/front-candidate-BlinkBoth.png>) |
| ![Source oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBakedUpperFace/Blender/oblique-source-BlinkBoth.png>) | ![Baked oblique](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBakedUpperFace/Blender/oblique-candidate-BlinkBoth.png>) |

The shapes look closely matched, but an opening remains, especially apparent on the anatomical left eye at image right. To distinguish an opening from a shadow or crease, a BVH ray test uses these exact cameras and the full evaluated mesh. Orthographic rays on a **0.2 mm grid** record which surface is hit first. A sample counts as exposed eye only when the first polygon belongs to that eye's verified 354-vertex geometry. The rays do not use colour, shading or normals to guess whether an eye is exposed.

| View / anatomical eye | Source neutral exposed area | Source BlinkBoth exposed area | Baked BlinkBoth exposed area | Closed-source exposure relative to neutral |
| --- | ---: | ---: | ---: | ---: |
| Front / Right | 855.56 mm² | 94.56 mm² | 94.56 mm² | 11.05% |
| Front / Left | 855.88 mm² | 94.40 mm² | 94.44 mm² | 11.03% |
| Oblique / Right | 688.00 mm² | 50.04 mm² | 50.04 mm² | 7.27% |
| Oblique / Left | 863.32 mm² | 134.48 mm² | 134.48 mm² | 15.58% |

Areas are approximate projected visible areas (sample count × 0.04 mm²), not physical contact areas. The front-view maximum summed exposed vertical span per sampled column is **5.8 mm Right / 6.0 mm Left**; oblique Left reaches **6.4 mm**. These measurements establish substantial residual eyeball exposure. They do not certify collision-free lid contact or measure penetration.

![Geometry visibility comparison](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBakedUpperFace/Blender/eye-coverage-comparison.png>)

**Visual acceptance: failed.** Eye coverage matches the source extremely closely, but that source endpoint is not an acceptable fully closed blink. No source controls were adjusted to make the experiment pass, no correctives were sculpted, and no framework was added. Good transfer cannot turn an unaccepted endpoint into an accepted blink.

## 5–7. Unity gate, neutral restoration and preservation

| Requirement | Result |
| --- | --- |
| Neutral geometry / vertex ordering | Exact hash equality in the imported Blender baseline and candidate; Basis untouched |
| Topology and both UV channels | Exact hash equality |
| Baseline corner normals | Exact hash equality at neutral, including after reset; closed-shape Unity normal behaviour not tested |
| Skin weights / group membership | Exact hash equality |
| Body skeleton / hierarchy / rest matrices / pose bases | Exact hash equality, 51 bones retained |
| Existing 29 keys | Exact hash equality of names, ranges, relative key, defaults and every key coordinate |
| New shape count | Exactly one: `BlinkBoth`; 30 expression keys total, plus Basis |
| BlinkBoth zero / neutral restoration | **0 mm maximum difference**, all 17,571 evaluated vertices |
| Existing Humanoid mapping | Original FBX and importer .meta unchanged; no new candidate Avatar imported |
| Unity BlinkBoth 0/100, body clips, normals, mouth/jaw, matched URP render | **Not run**, Blender contact gate failed |

The generated `.blend` holds an inspection copy of the imported baseline and a separate copied candidate mesh sharing the unchanged imported body armature. Preservation refers to this actual FBX baseline and its imported data, not an assumption that the other purchased .blend's neutral is identical. Input hashes and all preservation hashes are recorded in the evidence JSON.

## 8–14. Conditional stages not reached

| Requested stage | Result |
| --- | --- |
| Smile + BlinkBoth | Not run; primary blink acceptance failed |
| Frown + BlinkBoth | Not run; no claim about cheek distortion or correctives |
| JawOpen + BlinkBoth | Not run; no claim about mouth contamination or double deformation |
| Baked BlinkLeft | Not created |
| Baked BlinkRight | Not created |
| Left + Right versus BlinkBoth additivity | Not measured in this experiment |
| Separate SS_Eye_L / SS_Eye_R transforms | Not added; 53-bone representation remains untested |
| Neutral/left/right/up/down gaze and blink + gaze | Not run; eyelid-follow viability remains unknown |
| BrowRaiseBoth / BrowLowerBoth and lower-face combinations | Not created or tested |

The source authority already had saved independent/gaze/brow probes from the prior investigation; their existence is not counted as passing this candidate's conditional stages. No new facial capability is marked supported. Keep the semantic command → Adult Female binding → implementation boundary, and keep `BlinkBoth` experimental/unaccepted.

## 15–17. Representation, cost and Humanoid status

| Representation | Skin bones | Expression shapes | Body renderers | Shared mesh memory estimate |
| --- | ---: | ---: | --- | ---: |
| Existing lightweight Unity baseline | 51 | 29 | 1 SkinnedMeshRenderer | 22,842,184 bytes, prior investigation |
| Rejected Unity hybrid | 95 | 29 | 1 SkinnedMeshRenderer | 22,864,756 bytes, prior investigation |
| This generated Blender candidate | 51 | 30 | One candidate mesh; Unity renderer count unmeasured | Not measured in Unity |

The two Unity rows are historical measurements, not new profiling. This candidate did not reach Unity, so there is no final runtime bone/renderer/memory measurement and no new Humanoid validation. The original validated Humanoid remains unchanged. A 53-bone route may still be simpler than the 95-bone hybrid, but it is not established by this failed acceptance gate. No crowd profiling was performed.

## 18–20. Source preservation, exact files and tests

The 85-file inventory from the child-folder update is used only for preservation hashing; no Male/Boy/Girl metadata, geometry or runtime work is performed here. All source hashes and timestamps remain unchanged. The baseline FBX, importer metadata, prefab and controller are also unchanged.

Reproducible source tools added:

- `Docs/CharacterBaseAuditTools/bake_female_blink.py` — exact source endpoint, copied baseline key, preservation checks and parity arrays.
- `Docs/CharacterBaseAuditTools/render_female_blink_comparison.py` — eight matched technical views and camera settings.
- `Docs/CharacterBaseAuditTools/measure_female_blink_contact.py` — broad/local eye-region metrics and first-hit eyeball visibility.

Generated licensed geometry and detailed local evidence remain under the already Git-ignored `TestResults/FemaleBakedUpperFace/`:

- `baseline.json` — branch, HEAD, initial working tree and baseline file hashes.
- `Blender/FemaleBlinkBoth.blend` — derived inspection baseline plus one-shape candidate; **not a runtime adoption asset**.
- `Blender/source.npz`, `candidate.npz`, `topology.json` — exact evaluated arrays and correspondence data.
- `Blender/blink-bake.json` — preservation fingerprints and full parity/threshold metrics.
- `Blender/blink-contact.json`, eight `*-eye-hits.npz` files — spatial regions and geometric eye visibility.
- Eight `Blender/{front|oblique}-{source|candidate}-{neutral|BlinkBoth}.png` captures, `render-settings.json`, and `eye-coverage-comparison.png`.
- `blender-bake.log`, `blender-render.log`, `blender-contact.log`, Unity baseline/final-state records and `preservation.json`.

Validation executed: source endpoint reproduction; exact polygon/index correspondence; baseline attribute/key/skeleton hash equality; numerical displacement and absolute-surface comparisons; exact neutral reset; eight matched renders; geometric eye-exposure checks; source/baseline file preservation. The data checks passed; **the visual/contact acceptance failed**. No Unity XML suite or player build ran because the experiment stopped before candidate import.

Blender's file-load driver warnings are retained. The stored evaluated-driver statuses are valid after graph evaluation. An initial relay diagnostic returned no usable result; a later snippet had an `Editor` namespace/type compilation error. The corrected fully qualified reflection query succeeded. These snippets did not add C# project files or modify a scene. The current Unity project is still cleanly compiling with zero Console errors/warnings; project compilation is not candidate runtime validation.

Reproduction from the project root (replace the original female source path):

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --disable-autoexec --python 'Docs/CharacterBaseAuditTools/bake_female_blink.py' -- --source '<female facial-rig .blend>' --baseline 'Assets/SilverScreen/Art/Characters/HumanBasePrototype/Models/AdultFemale.fbx' --previous-evidence 'TestResults/FemaleFaceInvestigation' --output 'TestResults/FemaleBakedUpperFace/Blender'
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --disable-autoexec --python 'Docs/CharacterBaseAuditTools/render_female_blink_comparison.py' -- --evidence 'TestResults/FemaleBakedUpperFace/Blender'
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --disable-autoexec --python 'Docs/CharacterBaseAuditTools/measure_female_blink_contact.py' -- --evidence 'TestResults/FemaleBakedUpperFace/Blender'
```

## 21–23. Acceptance and repository status

The complete experiment is **failed at the Blender closure/contact gate**, with the later conditional work correctly not run. This preserves a useful distinction: the baked deformation transfer is accurate; the authoritative endpoint used for acceptance is inadequate.

The current checkout remains `C:/Repos/Unity/Silver Screen`, branch `fix/unrestricted-applicant-hiring`, HEAD `9aa2b98d491cfcce6706c7777874307c8da575ed`. No new branch/worktree was created. Prior uncommitted character audit/prototype work was preserved. This follow-up adds the three audit scripts and this report, corrects closure claims in the earlier female report, and appends project context. It adds no Unity scripts, imported assets, production integration, material work or locomotion changes.

`git diff --check` passes. No commit, push or PR was made. `ProjectSettings/ProjectSettings.asset` remains a pre-existing status entry with bytes equal to HEAD and no content diff. Unity remains in Edit Mode with one clean `Assets/Scenes/Studio.unity`; no candidate import occurred. No next milestone was started.

## Recommendation

**CONTINUE INVESTIGATION**

The baked transfer mechanism preserves the baseline and reproduces the source motion accurately, so the evidence does not justify rejecting the whole route. Adoption is blocked by the source endpoint's residual eyeball exposure. A separately authorized follow-up should first establish an actually accepted source closure/contact endpoint, then repeat this same single-shape gate before any Unity, independent-blink or gaze expansion.
