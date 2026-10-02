# Boy/Girl structural foundation validation

2026-10-02. **Both child families are structurally viable for the shared high-level character architecture. Recommend Boy as the first full child reference implementation.** Neither child has a production blink, accepted final facial art, runtime gaze/brows, identity morphs or full person-interaction proof. All plain-grey captures are technical evidence only.

Adult Female, Adult Male, Boy and Girl remain four independent canonical topology families. No adult geometry, UVs, vertex indices, hair partitions, eyelid calibration or corrective deltas were reused for a child. This milestone stops at structural validation. Child work is uncommitted on `master`.

## Phase A: completed Male checkpoint (report items 36–37)

- Starting branch `master`; fetched origin; starting HEAD and `origin/master` both `16b90e90a8d13205f9267996d1e5284123355334`.
- Individually reviewed all 21 pending Male files: reviewable tooling/tests/docs plus the two scoped documentation/ignore modifications. No unrelated non-ignored changes existed.
- Commit **`5a4e25513583bffc7670cde9911a4b98bebda356`**, **Add Adult Male character foundation pipeline**. Normal push to `origin/master` succeeded. Local HEAD, remote-tracking master and `git ls-remote` agreed. The non-ignored working tree was clean before any child work.
- Generated Male geometry/prefabs stayed ignored, evidence stayed under ignored `TestResults`, and all 85 purchased files remained external and unchanged. No reset, stash, force push, branch/worktree change or history rewrite.
- Unity compiled. Foundation tests: 2 passed; BlinkBoth/lighting tests: 4 passed; person tests: 2 passed on unchanged repeat. **The initial person run had one failure:** context drop expected `Practicing`, observed `Idle`; Socialize passed. The failed XML and unchanged passing repeat are both retained. This is intermittent input/integration-test behavior of unresolved root cause, not a clean first-pass claim. No test was weakened and no gameplay code changed.
- All 215 recaptured PNGs were byte-identical to the backed-up Male images. All 1,079 pre-Male preserved files remained unchanged. Source SHA-256, sizes and mtimes matched for all 85 purchased files. Ordinary and staged `git diff --check` passed.
- The disclosed Male oblique opening, approximately 0.74 mm² and 0.6 mm span, was left unchanged as requested.

Checkpoint evidence: `TestResults/MaleReferenceCheckpoint/{baseline,review,stage-review,checkpoint,recapture-comparison}.json`; `foundation-tests.xml`, `blink-tests.xml`, `person-tests.xml` (initial failure), `person-recheck.xml` (passing repeat); `editor-after-rpc.json`. `PriorUnity/` and `PriorUnityBlink/` preserve the earlier evidence before test regeneration. The exact committed-file list appears below.

## Inputs and authoritative candidates (items 1–2 and 15–16)

External pack root: `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/`.

The 85-file inventory contains **21 Boy and 21 Girl files**. Each family has an older eight-file facial package and a later thirteen-file shape package. The appendix records all 42 exact paths, format groups and hashes. Six Blender/FBX variants per family were freshly inspected: facial Blender, facial all/deform FBX, shape Blender, shape all/deform FBX. GLB, DAE, ABC, OBJ, PLY, STL and TXT are inventoried/hash-verified; their contents are not newly certified runtime candidates.

Selected lightweight files:

| Family | Exact path relative to pack | SHA-256 |
|---|---|---|
| Boy | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df` |
| Girl | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f` |

These are selected because measured geometry agrees with the evaluated child-scale sources, body-only hierarchy is suitable, Unity creates a correct Humanoid, all 29 channels work numerically, and runtime retargeting was inspected. Selection is not based on filenames alone. No Generic fallback, arbitrary stature correction or imported-face-rig repair was used.

Full facial probes use the original `Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.blend` and corresponding Girl `.blend`, with their complete evaluated dependency graphs and meaningful controls. Both source .blends also contain another shape authoring object and a generation rig; these are not additional people.

### Boy's earlier 1.882 m finding

The original facial `.blend` initially exposes the inactive `BoyBaseMesh_ShapeKeys` object at **1.881535 m**, while its active facial object measures 1.495085 m. The newly supplied shape `.blend` reverses which instance initially carries the large measurement: its active shape object is 1.495085 m and inactive facial object is 1.881535 m. After making the relevant collections visible and evaluating Blender's dependency graph, **both objects in both files resolve to about 1.495085 m**, with world scale 0.794609487. No scale value was assigned by the audit.

`source-details.json` preserves before/after measurements; the individual `inspection.json` files record the resolved matrices. The earlier measurement was real for that stored inactive object state, but is not authoritative evaluated child stature and must not be generalized to the new lightweight package. Girl's before/after variants remain about 1.493982 m. This resolves candidate selection while retaining the original evidence.

## Geometry, proportions and scalp (items 3–5 and 17–19)

Measurements below use the selected shape export in Blender metres (XYZ, Z up). Unity imports use Y up. Values are rounded for display; raw arrays and JSON retain precision.

| Measurement | Boy | Girl |
|---|---:|---:|
| Total bounds minimum | (−0.566678, −0.088700, −0.002549) | (−0.545392, −0.080668, −0.001078) |
| Total bounds maximum | (0.566678, 0.185177, 1.492537) | (0.545392, 0.149957, 1.492904) |
| Total height including hair | 1.495085 m | 1.493982 m |
| Connected body/scalp height | 1.466252 m | 1.460771 m |
| Native foot minimum | −0.002549 m | −0.001078 m |
| Source Head bone length / scalp height | 0.184174 m / 12.56% | 0.173724 m / 11.89% |
| Scalp above Head joint | 0.194142 m | 0.201024 m |
| Upper arm + forearm / scalp height | 0.444354 m / 30.31% | 0.420827 m / 28.81% |
| Thigh + shin / scalp height | 0.709627 m / 48.40% | 0.706761 m / 48.38% |
| Source scene units | Metric, unit scale 1 | Metric, unit scale 1 |
| Unity global scale / file unit conversion | 1 / 0.01 | 1 / 0.01 |
| Unity model root scale | (1,1,1) | (1,1,1) |
| Unity native height including hair | 1.495085 m | 1.493982 m |

Head/joint measurements are explicit rig proportion proxies, not anatomical chin-to-crown measurements or age estimates. Source bone endpoints are used; FBX-generated leaf tails are not authoritative head lengths. Individual segment measurements are in `measurements.json`. The prototypes translate their measured foot minimum to ground and retain their own imported scale. They are not normalized to adult height or to each other. Height alone does not establish a gameplay age.

| Topology | Boy | Girl |
|---|---:|---:|
| Facial source vertices / triangles / polygons | 17,477 / 34,116 / 17,465 | 17,876 / 35,162 / 18,001 |
| Selected shape vertices / triangles / polygons | 17,468 / 34,108 / 17,457 | 17,876 / 35,162 / 18,001 |
| Facial / shape Blender connected components | 52 / 51 | 43 / 43 |
| Selected FBX components in Blender | 51 | 45 |
| Main connected body/scalp vertices | 12,178 | 12,590 |
| Native Unity vertices / triangles | 19,861 / 34,108 | 20,207 / 35,162 |
| UV layers | one `UVMap` | one `UVMap` |
| Material slots / Unity submeshes | 2 | 3 |

Unity vertices include seam/material splits. Each family's topology, UV hash and correspondence remain independent. Boy's facial source includes one extra nine-vertex/eight-polygon hair fragment absent from the shape source. Girl has four non-surface loose vertices: a three-vertex loose-edge component plus an isolated point in Blender; the FBX drops the loose edges, producing four isolated components and a count of 45. Polygon/triangle counts remain unchanged; no widespread invalid geometry or broken numerical shapes resulted. Retained-vertex mapping must explicitly account for these details in later facial work.

Boy material slots are `Material.001` and `teeth_tongue3`; Girl uses `Material`, `Material.013`, `teeth_tongue3`. No image texture nodes were found on these assigned materials. Inspected colour attributes are uniformly white. The Unity prototypes use the existing neutral URP material, not authored skin/eye appearance. Per-variant UV hashes, material assignments, component extents and weights are in the inspection JSONs.

### Hair, scalp, eyebrows and oral pieces

**Both have complete scalp geometry beneath removable disconnected hair.** Actual front, side and crown comparisons were inspected. Every edge on the Boy main body/scalp component has two incident faces (24,391 edges); Girl likewise (25,257 edges). Neither needs a scalp patch. The retained bald contours look sound in the inspected technical views; final art approval is not implied.

- **Boy:** hair is joined into both the facial and shape mesh. Facial source has 14 hair islands / 1,483 vertices; lightweight shape has 13 / 1,474. The haircut's cap and locks are disconnected from the body. Source hair has Head plus some facial/ear/forehead/temple weight influence; the lightweight export reduces it to Head. Removing every non-body island would wrongly remove eyes and mouth parts, so only the explicitly inspected hair components are omitted in the render comparison.
- **Girl:** joined hair has three islands / 1,466 vertices: cap 1,004 and two buns of 231 each. It is Head weighted and shares no vertices/edges with the retained scalp. Four loose non-surface vertices are reported separately, not silently called hair.
- Each has separate **geometry islands** for the two eyeballs (482 vertices each), upper gums (804), lower gums (696), tongue (216) and multiple tooth islands. They are all within the joined skinned character object, not separate eye GameObjects in the selected export.
- No independent eyebrow mesh was found. Brow ridges remain on the body and participate in the source facial system. No eyebrow texture image or separate eyebrow material treatment was established. Hair exclusion preserves the body/brows, ears, eyes and oral pieces. No additional hat/accessory asset was identified.

Only reversible evaluated-surface bald renders were generated, by excluding explicit child hair components. **No bald child FBX/native subset was authored in this milestone.** Thus the source geometry demonstrates removable hair and complete scalp, while retained-key/normal/UV/skinning parity for a future bald runtime derivative still requires its own family-local check. The current Unity meshes are the untouched native imports. Future hairstyles can be separately fitted and Head attached/weighted per child and identity; source facial weights in Boy hair should not automatically become a modular-hair contract. No hairstyle system was built.

## Hierarchy, Humanoid and locomotion (items 6–8 and 20–22)

| Skeleton/export | Boy | Girl |
|---|---|---|
| Complete source `Armature_Character` | 652 bones, 148 deform, 13 roots | 653 bones, 148 deform, 9 roots |
| Source shape `Armature_Character.001` | 144 bones, 50 deform, 7 roots | 145 bones, 50 deform, 3 roots |
| Source generation `rig` | 509 bones, 98 deform | 509 bones, 98 deform |
| Facial FBX all / deform bones | 312 / 186 | 325 / 199 |
| Facial FBX all / deform root count | 8 / 2 | 23 / 21 |
| Shape FBX all / deform bones | 144 / 50 | 145 / 51 |
| Selected shape export root | `Hips` | `ROOT` → `Hips` |
| Unity skin references / model transforms | 50 / 53 | 51 / 54 |
| Unity weights per vertex | 1–6 | 1–6 |
| Unity Humanoid | valid and human | valid and human |

The source combines 50 body deform bones with 98 facial deform bones. FBX reimport marks imported joints deform; that flag alone is not a runtime-necessity classification. The complete source includes constrained mechanism roots that evaluate correctly in Blender. Boy's facial deform FBX has detached `DEF-nose` alongside Hips. Girl's has 20 facial roots alongside ROOT, including jaw/chin/cheek/brow/nose/lid chains. These are real export differences and make those heavy facial exports unsuitable as an assumed drop-in runtime hierarchy. They do **not** invalidate the independent body-plus-shape candidates or source-depsgraph evaluation.

The selected lightweight body chain was inspected independently in both Unity Avatars:

| Humanoid semantics | Actual child bones / parent structure |
|---|---|
| Hips, Spine, Chest, UpperChest, Neck, Head | `Hips` → `Spine` → `Spine1` → `Spine2` → `Neck` → `Head` |
| Shoulders, upper/lower arms, hands | `Spine2` → `Shoulder_L/R` → `Arm_L/R` → `ForeArm_L/R` → `Hand_L/R` |
| Upper/lower legs, feet, toes | `Hips` → `UpLeg_L/R` → `Leg_L/R` → `Foot_L/R` → `Toe_L/R` |
| Index/middle/ring/little fingers | Three mapped segments per digit/side, under the corresponding hand |
| Thumbs | Supplied `HandTumb1/2_L/R`; distal thumb segments absent |
| Eye and jaw Humanoid bones | Missing from both lightweight exports |

Full paths and rest transforms are in each `Unity/inspection.json`. No suspicious cross-side/incorrect-body mapping was found. Both map UpperChest. Optional distal thumbs, eyes and jaw are disclosed rather than fabricated. The authored rest stance has lowered arms; Unity's Humanoid pose conversion retargets it without an observed limb-axis error. No manual T-pose repair or Generic substitution was required.

The minimum ignored prefabs are `Assets/SilverScreen/Art/Characters/ChildFoundationPrototype/Boy/SS_BoyStructural.prefab` and the corresponding Girl path. They use their native mesh and Avatar, the existing locomotion controller, root motion disabled and the existing URP neutral material. No body mesh buffers or supplied facial frames were rewritten. They keep authored hair for the runtime structural test; bald scalp evidence is separate.

Actual Idle/Walk/Run playback was sampled at normalized phases 0.15, 0.45 and 0.75, with front and oblique captures and a one-metre world marker. Knees, hips, feet, shoulders, elbows, hands, neck, head and proportions were inspected. No gross family-specific detachment, limb inversion or collapsed skinning was observed. The root GameObject stays fixed; clip-authored pelvis/body motion remains. Adult clips are usable for prototype structural testing, not final child movement style.

Known ground contact remains visible/measurable: Boy sampled Walk minimum −0.073913 m, Run −0.061591 m; Girl Walk −0.075005 m, Run −0.053338 m. Some phases lift above ground. No locomotion/contact correction was attempted. Phase samples are not an exhaustive animation certification.

These are minimal renderer/Animator prototypes. Existing person-root/presentation code was inspected and can accept family-local Animator/renderer bindings, but EmployeeAgent identity/navigation/carry/context/Socialize behavior was **not exercised with children**. Full child person proof remains part of the later selected reference implementation. No production capsule was replaced.

## Lower-face channels and dimples (items 9–10 and 23–24)

Both children have **29 technically SUPPORTED supplied channels; zero numerically BROKEN channels**. Every stored position/normal/tangent frame is finite, has one frame at 100 and default 0. Blender ranges are 0–1 (Basis is not an expression). Unity exercised all at 0/25/50/75/100, and repeated 25/50/75/100 during frozen animated Idle. All produced finite nonzero displacement and restored neutral exactly. The appendix lists every exact name and maximum displacement at 100; each child's 30-image neutral/expression contact sheet was inspected.

Smile/frown/stretch/press/lower-down/upper-up/dimple left/right pairs have distinct deformation arrays. Similar maximum magnitudes are not duplicated channels. They remain broad authored expressions, not certified isolated semantic action units. Finite behavior and recognizable expression changes do not establish final acting quality, coarticulation or identity compatibility. Some extremes, particularly mouth closure/roll and tongue, need later expression-art review.

Boy/Girl names/order match each other, but their data and bindings must remain separate. Both use the literal misspellings **`mothShrugtLower` and `mothShrugtUpper`**. Male uses `mouthShrugtLower` and `mothShrugtUpper`, and reverses mouthLeft/mouthRight order. The prior Female prototype has six numerical channel failures; neither child reproduces the zero-frame-weight failure class. Never bind by numeric index.

`mouthDimpleLeft` and `mouthDimpleRight` both provide usable, distinct expression data: Boy maximum displacement about **4.058 mm** each; Girl **5.493 mm** each. They pull the cheek/mouth-corner expression and are useful authoring references. They do not establish a permanent neutral dimple identity feature or its behavior through every expression. **Dimples remain an explicit future Star Maker identity requirement**, with separate identity geometry and expression-dependent depth/crease validation. No identity system was implemented.

Supplied blink, brow and gaze shapes are **MISSING** from both lightweight files. Child source-to-runtime delta transfer, independent blink runtime behavior, identity morphs and expression combinations remain **UNKNOWN / unvalidated** in this milestone. No final semantic framework was implemented; retain the recommendation of stable semantic commands resolved through exact family names, ranges, aliases and capability status.

## Source facial capability and bounded blink (items 11–14 and 25–28)

Both original complete source rigs were evaluated through Blender's dependency graph. Meaningful controls were translated through their own local axes; no DEF-bone rotation approximation or purchased-file save was used. All tested points remained finite; neutral restored exactly after each probe and at the end. No invalid driver remained on the evaluated authoritative armature. Load-time warnings remain in the logs and are not erased by this result.

| Deterministic source probe | Boy relevant maximum | Girl relevant maximum |
|---|---:|---:|
| Upper lid L/R −3 mm | 2.845 / 2.802 mm | 2.663 / 2.663 mm |
| Lower lid L/R +2 mm | 1.599 / 1.545 mm | 1.457 / 1.457 mm |
| Eye target L/R +10 mm sideways | 2.594 / 2.594 mm eye movement | 2.564 / 2.564 mm eye movement |
| Brow L +3 mm | 2.510 mm body-side maximum | 2.898 mm |
| Jaw master −4 mm | 4.449 mm body-side maximum | 4.325 mm |
| Upper lip −3 mm forward | 3.363 mm | 3.106 mm |
| Cheek L −3 mm forward | 2.799 mm | 2.504 mm |
| Nose −3 mm forward | 2.954 mm | 3.000 mm |
| Tongue −3 mm forward | 3.069 mm tongue region | 3.058 mm tongue region |

Left/right upper lids and eye targets are independently controllable; the opposite eyeball remains unchanged. Lower-lid probes leave the opposite **eye window** unchanged. Girl does show small cross-midline movement elsewhere in the body-weighted face selection (up to 0.421 mm for these lower-lid probes); Boy's corresponding opposite-body-side maximum is zero. Source region memberships overlap, so this is not evidence that the opposite eyelid moved. Tongue controls also have small body influence, about 0.041 mm Boy and 0.136 mm Girl. Preserve these findings for later family-local authoring rather than assuming perfect isolation.

Each eye is a disconnected island in the joined full-source mesh, weighted to `DEF-eye.L/R`; source gaze mechanisms and eye targets work independently. In the lightweight exports both eyes are Head weighted, with no eye bones or look shapes. Independent runtime gaze is therefore lost and requires future family-specific implementation. No gaze was implemented.

### Three-pose structural blink probe

Exactly three bilateral source poses were tested per child: neutral, partial, strong. Upper/lower central travel was derived from that child's own neutral control gap; no adult endpoint or corrective was reused. The partial uses −45% / +10% of the gap, strong −90% / +20%. Adjacent controls were not calibrated. All values below are per eye, with shared magnitudes L/R:

| Family | Own central gap | Partial upper / lower | Strong upper / lower |
|---|---:|---:|---:|
| Boy | 29.061198 mm | −13.077539 / +2.906120 mm | −26.155078 / +5.812240 mm |
| Girl | 32.017112 mm | −14.407700 / +3.201711 mm | −28.815401 / +6.403422 mm |

Matched front, oblique and whole-face neutral/partial/strong captures were inspected, using the bald retained surface. Geometry-only first-hit BVH measurements use a 0.25 mm grid; hair is excluded from occlusion. These are coarse structural measurements, not a final closure or collision certificate.

| Family/view | Neutral eye area L / R (mm²) | Strong area L / R (mm²) | Remaining L / R | Strong exposed span L / R |
|---|---:|---:|---:|---:|
| Boy front | 745.6875 / 746.4375 | 8.3125 / 19.4375 | 1.115% / 2.604% | 1.25 / 2.00 mm |
| Boy oblique | 728.0625 / 545.5625 | 17.0000 / 15.0000 | 2.335% / 2.749% | 1.75 / 2.50 mm |
| Girl front | 850.0000 / 849.8125 | 3.4375 / 3.5000 | 0.404% / 0.412% | 1.25 / 1.25 mm |
| Girl oblique | 819.3750 / 634.6875 | 22.3750 / 9.0000 | 2.731% / 1.418% | 3.50 / 2.25 mm |

Both lids move towards plausible closure; partial poses narrow the eyes as expected. The strongest sampled contours approach contact but retain genuine narrow openings and corner pinching, especially obliquely. The Boy endpoint is less symmetric in residual coverage. No gross inversion, widespread folding, eye displacement or broad cheek/nose collapse was observed in the inspected views. Eyeball points stay unchanged during blink probes. Local contact/intersection and final crease quality remain unaccepted; residual exposure is not dismissed as future lashes/materials.

**Boy: PROMISING. Girl: PROMISING.** This means viable source controls for a later focused calibration/authoring milestone, not approved BlinkBoth endpoints. Three bounded poses cannot establish whether more careful source calibration is sufficient or whether a small corrective will be necessary. No extra endpoint search, corrective sculpt, transfer bake, runtime BlinkBoth, independent blinks or automatic blinking was started.

Neither family triggered a structural stop condition. Complete scalp, identifiable child-scale candidate, valid Humanoid, finite lower-face export and usable evaluated facial controls were established. The detached heavy facial exports, optional missing bones, loose non-surface vertices, residual blink gaps and contact/style work remain explicit risks/follow-ups.

## Cross-family architecture and first-child recommendation (items 29–35)

Boy and Girl are comparably viable at this scope: both have a complete scalp, working lightweight Humanoid, 29 supported channels, source eyelid/gaze/brow/jaw/lip/cheek/nose/tongue capability and usable prototype locomotion. Girl's heavy facial export has more detached roots; Boy has more hair pieces and a source-state scale trap. Neither prevents the chosen high-level pipeline. This is a technical comparison, not an aesthetic ranking.

**Recommend Boy first, following the planned validation order because both clear the structural gate.** Boy's simpler heavy facial export and zero measured cross-side lower-lid influence are helpful observations, not proof that final facial authoring will be easier. Girl's smaller front-view residual at one uncalibrated endpoint is not grounds to rank its final blink better. Later work must validate Boy's own bald native subset, source correspondence, accepted blink/corrective, Unity parity and full person binding before calling it the child reference implementation.

| Capability | Shared algorithm / semantic | Family-specific data / authoring | Female | Male | Boy | Girl |
|---|---|---|---|---|---|---|
| Humanoid body animation | Humanoid semantics and existing clips | Bone mapping, rest pose, scale/proportions | Validated | Validated | Structurally validated, 50 skin bones | Structurally validated, 51 skin bones |
| Bald-base generation | Connectivity discovery and retained-data checks | Hair component selection, scalp proof | Generated/validated | Generated/validated | Complete scalp; rendered comparison only | Complete scalp; rendered comparison only |
| Native Unity mesh preservation | Keep original buffers; subset only verified islands | Seam/UV/material map, shape frame semantics | Validated subset | Validated subset | Untouched native import; subset pending | Untouched native import; subset pending |
| Facial shape validation | Enumerate, sample, finite/reset checks | Names, ranges, meaning and capability | 23 supported / 6 broken supplied | 29 supported | 29 supported | 29 supported |
| Source-delta transfer | Evaluated posed-minus-neutral transport | Source/runtime correspondence and neutral offset | Proven | Proven | Not run | Not run |
| BlinkBoth | Stable semantic command and acceptance gate | Eyelid calibration, contact, correctives | Experimental validated reference | Experimental validated; disclosed residual | Source probe promising only | Source probe promising only |
| Independent blinks | Left/right semantic commands | Coupling, per-eye endpoint/corrective | Future | Future | Source independence probed; runtime future | Source independence probed; coupling noted; runtime future |
| Gaze | Target/look semantics | Eye pivots, axes, hierarchy and lid follow | Future | Future | Source independent; lightweight loses gaze | Source independent; lightweight loses gaze |
| Brows | Expression semantics | Controls, shape data and identity response | Future | Future | Source probe only | Source probe only |
| Dimples | Expression vs identity semantics | Neutral anatomy, side/depth, crease/contact | Supplied family data; identity future | Supplied family data; identity future | Distinct expression data; identity future | Distinct expression data; identity future |
| Person presentation | Existing identity, navigation and interaction owners | Family Animator/renderer/collider/held bindings | Runtime proven | Runtime proven | Minimal model only; full proof pending | Minimal model only; full proof pending |
| Hair attachment | Modular asset concept | Family/identity fitting and appropriate weights | Complete scalp | Complete scalp | Complete scalp; no system | Complete scalp; no system |
| Star Maker identity morphs | High-level appearance semantics | Separate topology, UVs, deltas, skeleton fitting and corrections | Future | Future | Future | Future |

Semantic binding should map stable names such as `SmileLeft`, `ShrugUpper`, `DimpleRight` or `BlinkBoth` to each family's exact supported implementation. Do not share numeric channel indices or silently drive missing/broken capabilities. Sharing algorithms does not establish common geometry; no family-to-family morph was attempted.

Star Maker implications, without implementation:

- Child face variation needs family-local identity deltas and expression compatibility checks. Eye size/shape/spacing/tilt changes can invalidate lid contact, requiring identity-dependent corrections and gaze fitting.
- Child hairstyles/hats require family- and identity-specific scalp fitting. Skin tone belongs to a later material pipeline; current white vertex colours and grey renders do not validate it.
- Dimples need explicit neutral-identity and expression-dependent definitions. Freckles and moles need family UV/material placement and identity persistence, not borrowed texture coordinates.
- Facial asymmetry needs separate per-side contact and expression acceptance. Native near-symmetry is not a mandate to force all identities symmetric.
- Height variation needs coordinated family geometry/skeleton/interaction-volume fitting. No uniform adult-to-child scaling rule is established. Age progression cannot be a direct vertex morph between these four different topologies; its transition and appearance policy remain design work.
- Appearance presets and simulation identity are separate. Ethnicity or background must not be inferred from appearance presets.

## Reproduction, evidence and validation limits

Reviewable tools: `inspect_child_foundation.py`, `explain_child_sources.py`, `probe_child_face.py`, `render_child_foundation.py`, `ChildFoundationPrototypeTools.cs`, `ChildFoundationPrototypeTests.cs`. They reuse generic connectivity/description routines from the committed audit tooling; no adult geometric data is imported by the child tools.

1. Run Blender 5.0.1 with `--background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/inspect_child_foundation.py -- --source <exact child variant> --output TestResults/ChildFoundation/<Boy|Girl>/<FaceBlend|ShapeBlend|FaceAll|FaceDeform|ShapeAll|ShapeDeform>`. Source `.blend` opens use `load_ui=False, use_scripts=False`; embedded rig UI scripts are not executed.
2. `explain_child_sources.py --pack <external root> --jobs TestResults/ChildFoundation/jobs.json --output TestResults/ChildFoundation` records the inactive-object scale behavior and material structure.
3. `probe_child_face.py --source <original facial blend> --family <Boy|Girl> --output TestResults/ChildFoundation/<family>/Face`; then `render_child_foundation.py --evidence <same Face directory>`. No runtime shape or saved child .blend is generated.
4. Unity `ChildFoundationPrototypeTools.ImportAndInspect("Boy")` and `("Girl")` copy hash-guarded FBXs only into ignored local folders, import independently, record every mapping/channel and build a minimal prefab only after the structural gate passes. Imports run in a preview scene; source data stays untouched.
5. Run `ChildFoundationPrototypeTests` through the existing reload-safe test runner. Two parameterized native checks plus Boy/Girl runtime tests total **4 passed, 0 failed, 0 skipped**. Runtime tests use an isolated temporary scene, actual Play Mode and actual URP renders, then restore the clean Studio scene. There is no complete carry/Socialize child test suite.

All evidence paths below are relative to `TestResults/ChildFoundation/` and ignored:

| Evidence | Exact location/pattern |
|---|---|
| Phase B baseline and source inventory | `baseline.json`, `source-inventory.json`, `jobs.json` |
| Original/added source-state explanation | `source-details.json`, `source-details.log` |
| Each variant's mesh/UV/weight/component/hierarchy metadata | `<family>/<variant>/inspection.json`, per-object NPZ and faces JSON, `blender.log` |
| Proportions and opposite-eye window check | `measurements.json` |
| Source facial probes / three bilateral endpoints | `<family>/Face/probe.json`, `neutral.npz`, `partial.npz`, `strong.npz`, `probe-*.npz`, `topology.json` |
| Matched source/scalp technical views | `<family>/Face/{front,oblique,face}-{neutral,partial,strong}-bald.png`; `scalp-{front,side,crown}-neutral-{hair,bald}.png` |
| Source image indexes | `<family>/Face/scalp-contact.png`, `blink-contact.png`, `capture-settings.json` |
| Unity native mesh, hierarchy and all 29 channel measurements | `<family>/Unity/inspection.json`, `semantic-pairs.txt` |
| Unity playback, joints, bounds and reset evidence | `<family>/Unity/runtime.txt`, `Captures/Rest-front.png`, `Captures/{Idle,Walk,Run}-{15,45,75}-{front,oblique}.png`, `Captures/Neutral-face.png`, `Captures/Shape-*.png` |
| Reviewed Unity contact sheets | `<family>/Unity/locomotion-{front,oblique}-contact.png`, `shapes-contact.png` |
| Test receipts | `child-tests.xml`, `child-tests.xml.summary.txt`, `run-tests-rpc.json` |
| Import diagnostics and final Editor state | `console-import-rpc.json`, `editor-check-rpc.json`, `console-after-tests-rpc.json`, `editor-final-rpc.json` |
| Preservation/repository and exact generated-file index | `preservation.json`, `repository-final.json`, `generated-files.json` |

Unity is actually **6000.6.3f1**, using URP `PC_RPAsset`. Final compilation succeeded; no pending compilation; one clean `Assets/Scenes/Studio.unity` scene restored. A transient **“Internal error - unexpected guid mismatch”** occurred during initial new-script/import work. It is preserved in `console-import-rpc.json`; its precise cause was not established. Subsequent AssetDatabase GUIDs matched both script `.meta` files, compilation and all four tests passed, and the post-test Console snapshot had zero errors/warnings. This does not claim the historical import log was error-free. No production GUID or serialized reference was changed.

No player build, performance/crowd test, final child animation-style approval, full collision/contact proof, final materials, child gameplay, production capsule replacement or Star Maker system is claimed. The temporary script/import diagnostic and intermittent Male test receipt remain disclosed. Full child blink calibration/authoring, delta transfer and Unity blink gates belong to the next explicitly requested milestone.

## Source preservation and repository boundary

All **85 purchased file hashes, sizes and modification times** remain unchanged. All **1,544 pre-child adult asset/evidence files** in the Phase B preservation snapshot remain byte-identical. Ten generated child asset/meta files remain locally available and ignored; all evidence remains ignored. No purchased file was saved over or staged.

- Current branch: **master**.
- Overall starting HEAD: `16b90e90a8d13205f9267996d1e5284123355334`.
- Male checkpoint / Phase B starting HEAD: `5a4e25513583bffc7670cde9911a4b98bebda356`.
- Phase B ending HEAD: **`5a4e25513583bffc7670cde9911a4b98bebda356`**. HEAD did not move during child work and still matches `origin/master`.
- Only the requested Male checkpoint was committed/pushed. Child changes are unstaged and uncommitted. No branch/worktree was created or switched.
- `git diff --check`: passed. New untracked reviewable text files were separately checked for trailing whitespace; Python syntax checks passed.

Exact changed/untracked files, the 21-file Male checkpoint, the 29-channel table and complete child inventory follow.

## Exact Male checkpoint files

- `.gitignore`
- `Assets/Editor/Characters/MaleBlinkPrototypeTools.cs`
- `Assets/Editor/Characters/MaleBlinkPrototypeTools.cs.meta`
- `Assets/Editor/Characters/MaleFoundationPrototypeTools.cs`
- `Assets/Editor/Characters/MaleFoundationPrototypeTools.cs.meta`
- `Assets/Tests/Editor/MaleBlinkPersonPrototypeTests.cs`
- `Assets/Tests/Editor/MaleBlinkPersonPrototypeTests.cs.meta`
- `Assets/Tests/Editor/MaleBlinkPrototypeTests.cs`
- `Assets/Tests/Editor/MaleBlinkPrototypeTests.cs.meta`
- `Assets/Tests/Editor/MaleFoundationPrototypeTests.cs`
- `Assets/Tests/Editor/MaleFoundationPrototypeTests.cs.meta`
- `Docs/CharacterBaseAudit.md`
- `Docs/CharacterBaseAuditTools/author_male_blink.py`
- `Docs/CharacterBaseAuditTools/bake_male_blink.py`
- `Docs/CharacterBaseAuditTools/export_male_bald.py`
- `Docs/CharacterBaseAuditTools/inspect_male_foundation.py`
- `Docs/CharacterBaseAuditTools/package_male_blink_preview.py`
- `Docs/CharacterBaseAuditTools/prepare_male_blink_unity.py`
- `Docs/CharacterBaseAuditTools/probe_male_face.py`
- `Docs/CharacterBaseAuditTools/render_male_foundation.py`
- `Docs/MaleCanonicalFoundationValidation.md`

## Exact child channel inventory

All rows: SUPPORTED in both children; one Unity frame at 100, default 0, Blender slider range 0–1. Position/normal/tangent data finite, every sampled deformation nonzero, every reset exact. Maximum world displacement at weight 100 is shown in millimetres. Index is evidence only, never a binding contract.

| Index | Exact supplied name | Boy max mm | Girl max mm | Status Boy / Girl |
|---:|---|---:|---:|---|
| 0 | `jawForward` | 8.2633 | 6.4117 | SUPPORTED / SUPPORTED |
| 1 | `jawLeft` | 10.1665 | 8.5135 | SUPPORTED / SUPPORTED |
| 2 | `jawRight` | 10.1665 | 8.5135 | SUPPORTED / SUPPORTED |
| 3 | `jawOpen` | 26.4115 | 33.0114 | SUPPORTED / SUPPORTED |
| 4 | `mouthClose` | 21.3817 | 25.6603 | SUPPORTED / SUPPORTED |
| 5 | `mouthFunnel` | 3.9093 | 3.4272 | SUPPORTED / SUPPORTED |
| 6 | `mouthPucker` | 8.8682 | 9.0535 | SUPPORTED / SUPPORTED |
| 7 | `mouthLeft` | 11.8392 | 8.0867 | SUPPORTED / SUPPORTED |
| 8 | `mouthRight` | 11.8390 | 8.0867 | SUPPORTED / SUPPORTED |
| 9 | `mouthSmileLeft` | 4.4734 | 6.5055 | SUPPORTED / SUPPORTED |
| 10 | `mouthSmileRight` | 4.4734 | 6.5055 | SUPPORTED / SUPPORTED |
| 11 | `mouthFrownLeft` | 6.1511 | 6.7420 | SUPPORTED / SUPPORTED |
| 12 | `mouthFrownRight` | 6.1163 | 6.7420 | SUPPORTED / SUPPORTED |
| 13 | `mouthDimpleLeft` | 4.0581 | 5.4929 | SUPPORTED / SUPPORTED |
| 14 | `mouthDimpleRight` | 4.0581 | 5.4929 | SUPPORTED / SUPPORTED |
| 15 | `mouthStretchLeft` | 3.7755 | 4.0696 | SUPPORTED / SUPPORTED |
| 16 | `mouthStretchRight` | 3.7755 | 4.0696 | SUPPORTED / SUPPORTED |
| 17 | `mouthRollLower` | 10.3294 | 9.6795 | SUPPORTED / SUPPORTED |
| 18 | `mouthRollUpper` | 8.6329 | 10.3929 | SUPPORTED / SUPPORTED |
| 19 | `mothShrugtLower` | 6.9798 | 4.8226 | SUPPORTED / SUPPORTED |
| 20 | `mothShrugtUpper` | 4.6598 | 4.0332 | SUPPORTED / SUPPORTED |
| 21 | `mouthPressLeft` | 6.8453 | 6.4669 | SUPPORTED / SUPPORTED |
| 22 | `mouthPressRight` | 6.8453 | 6.4669 | SUPPORTED / SUPPORTED |
| 23 | `mouthLowerDownLeft` | 3.5824 | 4.2355 | SUPPORTED / SUPPORTED |
| 24 | `mouthLowerDownRight` | 3.5824 | 4.2355 | SUPPORTED / SUPPORTED |
| 25 | `mouthUpperUpLeft` | 3.5092 | 5.1029 | SUPPORTED / SUPPORTED |
| 26 | `mouthUpperUpRight` | 3.5092 | 5.1029 | SUPPORTED / SUPPORTED |
| 27 | `cheekPuff` | 18.2763 | 8.8992 | SUPPORTED / SUPPORTED |
| 28 | `tongueOut` | 32.0477 | 29.1836 | SUPPORTED / SUPPORTED |

## Exact child source inventory

Paths are relative to the external pack root given above. Older = original facial package; Added = subsequently supplied shape package. All files remain external and immutable.

| Package / format group | Exact relative path | SHA-256 |
|---|---|---|
| Older facial / FBX all bones | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig(all_bones).fbx` | `c0cbdb91ea352ac7ecc346c7de3bded23fed8d8f40767b758d3d3052ab9adf9d` |
| Older facial / FBX deform only | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig(deform_bones_only).fbx` | `78168a8f333490fac05f47b0280286ed924354a077e9034b633cab44f62b7051` |
| Older facial / DAE without rig | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig(without_rig).dae` | `4fd6ec9e882d445d511a040a0774dcb7a11b7f51eb9efcbf363f06719c9cdd7f` |
| Older facial / ABC | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.abc` | `76a67a42c784462c97ffc26aa128ab74f9e50ddffea3475c9cc6f8d508dbd7de` |
| Older facial / BLEND | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.blend` | `2afd93d4a875a3f6cdaf86b906ff3cf552eb18b2fc2ffb0976db803ec5bb8537` |
| Older facial / OBJ | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.obj` | `ab928ea18e3ae1cc3ca487d38a35177925b1670026ddb30b203da776e36181cd` |
| Older facial / PLY | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.ply` | `06ba67370ca5587a909e6976fe2587cf676b4f67767e53fa18c91094d479568b` |
| Older facial / STL | `Stylized_Boy_Body_Base/Facial_Rig/StylizedBoyBodyBaseMesh3DModelFacialRig.stl` | `9da8cb6dc44e83d109fec29f22bd48c5056a39384b0d0d1f2a8d8cc308a255a1` |
| Added shape / TXT | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/only .fbx .blend and .glb support shape keys .txt` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Added shape / FBX all bones | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(all_bones).fbx` | `3f5dc9e605f6d88305b6e38ce622660685894bd2f054146eb1b4d44418b3a062` |
| Added shape / GLB all bones | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(all_bones).glb` | `0b7268f6233d5c4ca82cc72066815140f3ff380bdb7f30e884a7482896942f04` |
| Added shape / FBX deform only | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df` |
| Added shape / GLB deform only | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).glb` | `e837a91835f8b476218399d350b2dd4894e1782f8c88e658892ffb78366edbf6` |
| Added shape / DAE all bones | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(with_rig_all_bones).dae` | `def8881d3cd75aa24719b3f8018ae70afb534c61e11f9d8d2c43939166f9a7e4` |
| Added shape / DAE deform only | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(with_rig_deform_bones_only).dae` | `6fbcc95c148004237aee01bcd4c5726cc3f2392e7010829b265e37ec4b21ad6c` |
| Added shape / DAE without rig | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(without_rig).dae` | `6b15c069d575cab0817865655ff2e511d05ca8c21dffc29db166a46d38246915` |
| Added shape / ABC | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.abc` | `7f722e250f1a79e6792502f49e41dd169dd6d4f315316dd5d5b9e3ca84837e27` |
| Added shape / BLEND | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.blend` | `81785f92ee503944c3a3bc05fd239056cf7d602513aab648f693e1f97d406761` |
| Added shape / OBJ | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.obj` | `c3b3baeaf7d6b72570e0eb840413c43725c0f255a97cb83cc193d34220b8be3b` |
| Added shape / PLY | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.ply` | `b3d73c4ddbf332b21a221134aa9d99286c3c96199c33635a785e510c8a810a50` |
| Added shape / STL | `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.stl` | `36a961ac6fc312b2ef9fae4a9d9716bd0e3f405d061e9aaea29f70f6186f2d4c` |
| Older facial / FBX all bones | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig(all_bones).fbx` | `5d37c4e9307840a4f0862e33c3af772ef5b1f7eba07691ccd086ebf37dd83632` |
| Older facial / FBX deform only | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig(deform_bones_only).fbx` | `00355758854c85616c135c2871b620fe2d74fcbb25a96f9e47cb4a9fdaccd8d0` |
| Older facial / DAE without rig | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig(without_rig).dae` | `4c488007392fa158878c2c25b8f1ad8bf8971b7bc7f7f78a42c0e6f874781a6f` |
| Older facial / ABC | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.abc` | `c9f14e339d1c89365d07a77b732fd9753fa164c3ac784b29b11d4cec5cb031d2` |
| Older facial / BLEND | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.blend` | `d088d51f5b6617ea29fb0145ade2af69934156d535872c43b55fe3011d4d45c8` |
| Older facial / OBJ | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.obj` | `26135654aed62232db407b3650928f733ff2c8bb88920f2b4bf83f6648925116` |
| Older facial / PLY | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.ply` | `2a695b16f35124a20b9c835db4c3df580164b6a90ce5ed42251d0c78c71feaf6` |
| Older facial / STL | `Stylized_Girl_Body_Base/Facial_Rig/StylizedGirlBodyBaseMesh3DModelFacialRig.stl` | `edb05ad686b135de67c80f169f6a78881364b9bf129c57c63ef705033774bffc` |
| Added shape / TXT | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/only .fbx .blend and .glb support shape keys .txt` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Added shape / FBX all bones | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(all_bones).fbx` | `d5b01e2a960129f2309810c521e4ba7384a64832397e07793c43664370515e30` |
| Added shape / GLB all bones | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(all_bones).glb` | `a8b2ad2178c3eb414d6fd01218921df3d508dfb6d20652e4d71d1c60675d20ab` |
| Added shape / FBX deform only | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | `37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f` |
| Added shape / GLB deform only | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).glb` | `4b374f26527087e3106e3eccd436666a5777564b625bc02a499a6fd64590590f` |
| Added shape / DAE all bones | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(with_rig_all_bones).dae` | `77939a16333ab18e3dd26e7b0d7e4bb1dcdd93e7a3ca2873bb35fa2044f42130` |
| Added shape / DAE deform only | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(with_rig_deform_bones_only).dae` | `d21d7caf6b7600f9957ccd312cfdb4b0e9cb066b001c1d03887062aca7dfeba5` |
| Added shape / DAE without rig | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(without_rig).dae` | `9354d3cd2bb8f186fe36a4720e9cd5959f9b119a88c1e814dfe0e1de4e754811` |
| Added shape / ABC | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.abc` | `1f1a080ba74d0924f44986be05444eb18b3926479db6f5bc305b37b035a585c7` |
| Added shape / BLEND | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.blend` | `e087e18c7b52de0dd2aed9c50885779877cd955e84a248dd0bfe76222ce5a53c` |
| Added shape / OBJ | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.obj` | `144a340cf39e8e54c43cf8ce801dbb00c6a3b9676b5fa097d74f77db2dd77ddd` |
| Added shape / PLY | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.ply` | `f596a3c684be1becf1c1ca08a95523b015a1aec5aed1c61df5f63fe51a70a38a` |
| Added shape / STL | `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.stl` | `2730f417570d88745edfd64c369ff05813a80e66dd8f7e640b5e2a5c63bdb6f0` |

## Exact Phase B working-tree status

Nothing staged. No Phase B commit or push.

| Status | File |
|---|---|
| ` M` | `.gitignore` |
| ` M` | `Docs/CharacterBaseAudit.md` |
| `??` | `Assets/Editor/Characters/ChildFoundationPrototypeTools.cs` |
| `??` | `Assets/Editor/Characters/ChildFoundationPrototypeTools.cs.meta` |
| `??` | `Assets/Tests/Editor/ChildFoundationPrototypeTests.cs` |
| `??` | `Assets/Tests/Editor/ChildFoundationPrototypeTests.cs.meta` |
| `??` | `Docs/CharacterBaseAuditTools/explain_child_sources.py` |
| `??` | `Docs/CharacterBaseAuditTools/inspect_child_foundation.py` |
| `??` | `Docs/CharacterBaseAuditTools/probe_child_face.py` |
| `??` | `Docs/CharacterBaseAuditTools/render_child_foundation.py` |
| `??` | `Docs/ChildCanonicalFoundationValidation.md` |

BOY AND GIRL STRUCTURAL FOUNDATIONS VALIDATED — BOY RECOMMENDED FIRST
