# Purchased character base audit and Unity prototype

2026-10-02. Evaluation milestone; canonical adoption is **conditional**, not approved. One adult female prototype is isolated from production recruitment and capsule people. No purchased file was uploaded, modified, committed, or pushed by this work. No Star Maker, wardrobe, lip sync, child gameplay, or production LOD system was implemented.

## Evidence and reproduction

- Initial 59-file inventory, SHA-256 checksums, exact mesh names, every source bone/parent/deform flag/constraint, export comparisons and exact shape orders: [source evidence](CharacterBaseSourceEvidence.md). The subsequently supplied Boy/Girl shape-key folders add 26 files: [child source update](CharacterBaseChildSourceUpdate.md). The current inventory contains **85 files**.
- Local detailed metadata, Unity results and screenshots: `TestResults/CharacterBaseAudit/` (Git-ignored). The initial `preservation.json` covers 59 files; `ChildSourceUpdate/preservation.json` verifies all 85 current source hashes and modification times are unchanged by inspection.
- Source inspection: Blender 5.0.1, factory startup, `--disable-autoexec`, `open_mainfile(use_scripts=False)`. Embedded `rig_ui.py` was deliberately not executed. Initially audited six .blend files and thirteen FBXs, then inspected metadata from the two added child .blends and four child shape-key FBXs. Control widgets were recorded but excluded from character complexity totals. Child Unity/export behavior remains unvalidated.
- Runtime environment: Unity **6000.6.3f1**, URP **17.6.0**, AI Navigation 2.0.14, Input System 1.20.0, Test Framework 1.8.0. The repository instruction's 6000.5 version is historical. The installed official local Unity MCP relay was used; no package was installed or replaced.
- Source tools: `Docs/CharacterBaseAuditTools/inspect_sources.py`, `inspect_topology.py`, `write_appendices.py`. Run the first two with Blender; run the last with ordinary Python. Explicit `--source` and `--output` arguments keep purchased inputs separate from output.
- Unity: **SilverScreen > Characters > Human Base Prototype > 1 Build local prototype**. The builder copies only four selected existing FBXs, preserves .meta GUIDs on rebuild, refuses to overwrite differing local copies, imports them, and creates the material/controller/prefab. Menu 2 opens an unsaved additive review; menu 3 closes it; menu 4 runs local prototype tests. The saved Studio scene is not rewritten.

## 1–4. Supplied files and variants

All **Adult Male, Adult Female, Boy and Girl** are present. Current inventory: 17 FBX, 8 Blender, 8 GLB, 16 DAE, 8 Alembic, 8 OBJ, 8 PLY, 8 STL, and four empty TXT notices. No archives, texture images, .mtl files, .gltf files, or substantive documentation/license text occur in the supplied directory. Commercial licensing is the user's supplied information, not an independently verified license document.

Adult directories each contain facial-rig and facial-shape-key export variants. The female also has a CGTrader-optimized FBX; this adds a different bone structure and is not a fifth character. Both child directories now also contain dedicated shape-key .blend, all-bone/deform-only FBX and GLB variants. The initial finding that child shape-key FBX/GLB files were absent is superseded by the added folders. The four new FBXs each expose Basis + 29 expression keys when inspected in Blender; GLB contents have not been validated.

There are 12 byte-identical duplicate groups (26 files), including the two female .blends, same-adult static output copies and the four empty TXT files. The male .blend files are different bytes but expose the same principal character variants in the audited metadata. Do not count internal duplicate meshes, rig widget meshes, or alternate formats as additional people. See the initial inventory and child addendum for exact filenames, including malformed parentheses in supplied female FBX names.

## 5–8. Body rigs, topology and compatibility

| Base | Facial-rig armature / total / source deform / non-deform | Shape-key armature / total / source deform / non-deform | Shape-key mesh vertices / triangles |
| --- | --- | --- | ---: |
| Adult Male | `Armature_Character1` / 441 / 137 / 304 | `Armature_Character_shape_keys` / 145 / 50 / 95 | 18,116 / 35,620 |
| Adult Female | `Armature_Character1.001` / 441 / 137 / 304 | `Armature_Character_shape_keys.001` / 145 / 50 / 95 | 17,571 / 34,554 |
| Boy | `Armature_Character` / 652 / 148 / 504 | `Armature_Character.001` / 144 / 50 / 94 | 17,468 / 34,108 |
| Girl | `Armature_Character` / 653 / 148 / 505 | `Armature_Character.001` / 145 / 50 / 95 | 17,876 / 35,162 |

Additional face-generation `rig` armatures exist: male 299/87 deform, female 298/87, children 509/98. These are authoring machinery, not additional runtime characters. Source `use_deform` means a flagged deformation bone; it is not automatically proof that a bone has weighted vertices. Bone-parented pieces also matter. FBX import into Blender marks exported bones as deform, so its resulting flag must not be used to claim all exported controls are useful joints.

The shared body chain is `ROOT > Hips > Spine > Spine1 > Spine2 > Neck > Head`, plus `Shoulder_L/R > Arm_L/R > ForeArm_L/R > Hand_L/R` and `UpLeg_L/R > Leg_L/R > Foot_L/R > Toe_L/R`. The boy's body has **Hips as a root**, unlike the other three. All have three index/middle/ring/pinky joints but only two thumb joints, spelled `HandTumb1_*` and `HandTumb2_*`. No separate weighted body twist/helper chain exists in the selected 50-bone body deformation set.

Authoring includes duplicated `IK_*`/`FK_*` limb/finger chains, IK foot/hand/elbow targets, `SWITCH`, `SWITCH_ARMS`, and constraint-driven face machinery (`ORG-*`, `MCH-*`, `DEF-*`). Keep IK/FK solvers and Rigify mechanisms in Blender. Export evaluated deformation and required ancestors, not the full authoring rig by default.

The supplied adult shape-key deform FBXs are especially simple: **51 exported bones (50 body deform bones plus ROOT)**. Adult facial-rig deform FBXs contain **241** bones; all-bone versions contain **367**. The female optimized version contains **500**. Boy facial exports contain **186/312** bones (deform/all), girl **199/325**. These are not hierarchy-identical. The child facial-rig deform FBXs expose detached face roots, especially the girl, so their facial runtime hierarchy needs repair/validation before adoption. The newly supplied shape-key FBXs are separate variants: Boy has 50/144 exported bones (deform/all), Girl 51/145. The deform-only Boy has Hips as its root, Girl ROOT. These metadata counts do not validate a Unity Avatar, animation, or export fidelity.

Body animation can plausibly be shared through Humanoid semantic mapping rather than identical transform paths. Only the female Avatar and animation retargeting are Unity-proved in this milestone. Male/child Avatars are **not** claimed validated. Generic was not needed for the chosen adult and was not substituted to bypass a mapping failure.

The four topology hashes and UV hashes differ. Adults have `UVMap` and `map1`; children have `UVMap`. This establishes that direct vertex-index morph transfer and one identical texture atlas are unsafe; it does not prove no atlas portions could be reconciled offline. Material layouts also differ: male shape mesh has no assigned material slot, female one, boy two, girl three.

Female facial source body `character2.003` is a closed connected 12,937-vertex surface; other source pieces include `character2.002` and gum/tongue objects. Male facial source separates `Character1`, eye pieces `Character1.002/.004`, lower/upper gums and tongue. Child facial variants are already joined meshes. Shape-key variants join body, eyes, mouth parts and hair geometry into one skinned object with disconnected islands: male 41, female 42, boy 51, girl 43. They are one renderer, not one watertight surface. Boundary edges largely occur on these accessory/oral islands; non-manifold joints must be reviewed before garment/morph authoring.

## 9–11. Facial systems and exact shapes

All four shape-key sources contain **Basis + 29 expression keys**. Female Unity import has 29 blendshapes, with exactly these names:

```text
jawForward, jawLeft, jawRight, jawOpen, mouthClose, mouthFunnel,
mouthPucker, mouthLeft, mouthRight, mouthSmileLeft, mouthSmileRight,
mouthFrownLeft, mouthFrownRight, mouthDimpleLeft, mouthDimpleRight,
mouthStretchLeft, mouthStretchRight, mouthRollLower, mouthRollUpper,
mouthShrugtLower, mouthShrugtUpper, mouthPressLeft, mouthPressRight,
mouthLowerDownLeft, mouthLowerDownRight, mouthUpperUpLeft,
mouthUpperUpRight, cheekPuff, tongueOut
```

Spelling is literal. Male uses `mouthShrugtLower` and **`mothShrugtUpper`**, and reverses the order of `mouthRight`/`mouthLeft`. Boy/Girl use **`mothShrugtLower`, `mothShrugtUpper`**. The evidence appendix lists every exact order. Recommend a small **semantic facial-shape mapping layer**: family-specific bindings from stable concepts such as MouthShrugUpper to the exact supplied renderer/shape name, with explicit unsupported capabilities. Resolve and cache indices from names on initialization; never share numeric indices or silently substitute unrelated shapes. Validate required bindings per asset and expose missing blink/brow/gaze support. This recommendation is documented only; no final facial system was implemented.

There are **no blink, eye-look, brow, identity/body morphs, named phonemes or visemes** in this inventory. Mouth funnel/pucker/close/jaw shapes are useful raw controls, not an authored speech system. This is not a complete ARKit set or turnkey lip sync.

The alternative facial rigs provide eyelid, brow, forehead, jaw, lip, cheek, nose, tongue and ear bones (87 facial deformation flags for adults; 98 for children). Eye movement uses additional non-deform mechanism/control bones such as `master_eye.L/R`, `MCH-eye.L/R`, `ORG-eye.L/R`, `eyes`, `eye.L/R`, with tracking/copy constraints. Those mechanisms are not present in the chosen shape-key FBX. Arbitrarily stripping all non-deform bones from the full face rig could lose eye-parent/constraint behavior. An eventual face-bone route needs explicit baked/runtime drive and evaluated eye parenting; a hybrid mouth-shape plus authored eye/blink/brow route is another option. Neither was implemented here.

## 12–15. Chosen prototype and import

Selected supplied file: `stylized-female-body-base/Stylized_Female_Body_Base_Shape_Keys/Shape Keys/StylizedFemaleBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` (3,139,916 bytes). It imports one `Character_shape_keys.002` SkinnedMeshRenderer with skinning, normals, two UV channels, one material slot and all expression shapes. No source re-export was necessary for this limited proof. The original source remains untouched.

Unity settings: Humanoid, Create From This Model, file units enabled, global scale 1, blendshapes/normals imported, animation disabled on the body, camera/light/visibility import disabled, Optimize GameObjects disabled for inspection. Custom skinning accepts up to 10 influences at a 0.00001 cutoff, preserving this export's observed limit. The renderer uses the existing project **FourBones** quality setting; retaining import weights above four does not mean all ten are evaluated at runtime. Compare joint quality before choosing a production weight budget. It is a validation configuration, not a production performance prescription.

Unity automatically produces a valid human Avatar with hips, three spine levels including UpperChest, neck/head, shoulders, upper/lower arms, hands, upper/lower legs, feet/toes, and 28 finger joints. Both thumb distal mappings are absent because no such bones exist. Eyes and jaw Humanoid mappings are absent in this body-only skeleton. No arbitrary bones were forced into absent slots. Exact Avatar mapping is in `unity-import.txt` and the initial import JSON.

Existing local animation sources, copied individually without modifying the archive:

- `ExternalSourceAssets/Animations/Basic Locomotion Pack/idle.fbx` — Humanoid, 8.333 s.
- `ExternalSourceAssets/Animations/Basic Locomotion Pack/walking.fbx` — Humanoid, 1.033 s.
- `ExternalSourceAssets/Animations/Female Basic Locomotion Pack/running.fbx` — Humanoid, 0.7 s.

All three imports build valid source Avatars. Clips loop with root rotation/Y/XZ baked into the pose and height from feet. Prototype Animator root motion is disabled; an isolated controller changes Idle/Walk/Run from measured NavMesh velocity. `HumanBasePrototypeVisual` contains no navigation implementation. Existing `CharacterMotion` procedural studies were not repurposed as evidence of supplied Humanoid animation.

**Functional animation passed; contact quality needs correction.** The runtime proof observed actual leg rotation changes exceeding 10 degrees while the navigation root moved, reached the destination, returned to Idle, and entered Run on a faster route. URP captures sampled four phases of each clip. Inspected poses show intelligible shoulder/elbow/knee bending, hand orientation and stable head/neck without a gross rig inversion. They do not establish polished motion, natural finger posing, or slip-free contacts.

A 61-phase-per-clip baked-geometry measurement used the runtime character at NavMesh surface Y=0.01853454 m. Idle lowest vertex Y=0.01777–0.01926 m: essentially on that surface; maximum hair height is 1.75155 m above it. Walk minimum reaches Y=-0.08442 m, **10.30 cm below** the surface. Run reaches Y=-0.06764 m, **8.62 cm below** it; run also has airborne phases, which are expected. This is a real retarget/contact limitation, not clean ground-contact approval. Foot sliding was not quantified against a continuously moving filmed sequence and remains unapproved. Do not mask the problem with a constant visual lift that would make the correct idle float. Clip/Avatar contact fitting is a required adult follow-up within the existing animation architecture. See `contact-samples.csv` and the pose images.

## 16–20. Existing person integration, scale and art

Prefab: `Assets/SilverScreen/Art/Characters/HumanBasePrototype/Prefabs/SS_CharacterPrototype.prefab`. It has the existing `EmployeeAgent`, NavMeshAgent, CapsuleCollider, selection indicator, `HeldPersonPresentation`, and the small `HumanBasePrototypeVisual` adapter. Animator and mesh live under a dedicated visual child. Domain identity remains `Employee.Person`; carry/drop uses the existing root and collider suspension.

Carry uses the existing `HeldPersonPoseAdapter` extension: freeze a neutral idle, apply the existing sway/settle to the visual child around a serialized 1.35 m pivot, then restore it. This is a whole-body suspension proof, not a finished dangling-limb pose. Root movement, valid/invalid placement, contextual actions and cancellation remain in the existing system. The adapter suppresses the capsule-era filming gesture on this prototype only.

Source female bounds span **1.88609456 m** including hair, with minimum -0.00134089 m. The model instance has uniform scale **0.92784318**, yielding **1.75 m** and a small visual-only sole offset. This is asset scale fitting, not a body customization implementation. World, buildings, navigation bake settings and production people are unchanged.

The prototype retains the production profile: NavMesh radius **0.35 m**, height **2.0 m**, root/baseOffset **1.0 m**, matching capsule collider. Its visual child is offset -1 m. The 1.75 m body sits inside this conservative 2 m interaction volume; selection can hit the empty space just above the hair. These dimensions are deliberately reported rather than globally changed. Recommended starting range for later review is radius 0.30–0.35 m and adult collision height 1.80–1.90 m, validated per family and against the existing bake. The normal idle silhouette is narrower than the 0.70 m collision diameter; extended arms/gestures still need clearance checks.

Studio Services authored fixtures include 1.10 × 2.58 m office doors, a 1.20 × 2.62 m frame and a 1.50 × 2.60 m utility door. These are source fixture dimensions, not a measured fully-open swept aperture. They plausibly accommodate the prototype; this alone is not a traversal proof. Furniture sitting/hand contacts and all construction/interior routes require later motion/contact checks.

Raw material is untextured light grey. The prototype uses one URP/Lit material carrying that neutral colour, not a skin redesign. Initial real-URP inspection confirms the body, joined hair, eyes, normals and shadows render; eyes have no iris/pupil texture or separate material treatment. The proportions read as stylized rather than photoreal, with a large expressive head and smooth anatomy. Hair strand tips and shiny untextured eyes are noticeable close up. It is useful as a technical base, but the raw unclothed grey asset is not finished SilverScreen character art or visual approval beside the production buildings.

The useful runtime context capture is `studio-services-technical.png`, alongside the actual Studio Services doors, bench, brickwork and yard fixtures. The 18-image pose/face gallery was recaptured with three engine frames between pose changes and capture: immediate same-frame SRP captures initially reused stale skin buffers, so those images were replaced. Representative Walk, Run, jaw-open and smile images were inspected. The ten-instance render was also inspected. The attempted Stage 1 camera was occluded by the lot boundary and is **not** Stage 1 visual validation; Stage School was not present in the loaded runtime hierarchy. Neither comparison is claimed complete. No production environment was repositioned to manufacture a result.

Recommended long-term organization follows existing project conventions: keep full purchased authoring data outside Unity, e.g. `ExternalSourceAssets/Characters/Purchased/HumanBase/{AdultMale,AdultFemale,Boy,Girl}` in private licensed storage, with derived work under `ArtSource/Characters/HumanBase`. After review, selected runtime imports can live under `Assets/SilverScreen/Art/Characters/HumanBase/{Models,Materials,Prefabs,Animation,Face}`. Keep family-specific model/morph assets distinct and share semantic mappings/approved animation clips where appropriate. This milestone only creates the separate **HumanBasePrototype** local folder; it does not move sources into that proposed permanent structure.

## 21–24. Star Maker feasibility — assessment only

**Body:** The largely quad source surfaces and weighted continuous main body are workable candidates for sculpted weight/shape morphs. No useful body morph library is supplied. Preserve vertex order; test extreme poses and coordinated skeleton fitting before promising a slider range. Adult/child identity cannot be implemented by uniformly shrinking this adult.

**Face:** Identity morphs can coexist with expression deltas on a stable mesh, but mouth corners, jaw, lips/teeth/tongue and eyelid contacts need corrective work. The expression inventory is incomplete and has naming defects. No neutral identity variation, expression combinations or extreme identity retargeting was proved.

**Wardrobe:** Offline garment fitting to each canonical family, skin-weight transfer, and matched runtime morphs are plausible. Body hiding masks and corrective garment morphs will likely be needed at shoulders, armpits, groin, elbows, hips and knees. One joined hair/mouth/body material layout is inconvenient for modular wardrobe, eye and skin work. No clothing was built or evaluated.

**Age/families:** Per the user's explicit direction, Male, Female, Boy and Girl are **four separate canonical mesh families** with a shared semantic body mapping and facial-name bindings. Do not unify their topology or morph between them. Do not assume shared vertex indices, UVs or interchangeable garments. The Boy shape-key object inside the original facial-rig .blend measures ~1.8815 m, but the newly supplied shape-key .blend and both shape-key FBXs measure ~1.4951 m, consistent with the facial variant; Girl remains ~1.4940 m. This corrects the earlier generalization about Boy shape-key height while retaining the source-variant discrepancy. Child hierarchy/export/Avatar validation and authoritative scale/variant selection are a **required later follow-up**, not part of this integration. No child runtime assets were imported and no aging interpolation was attempted.

## 25–27. Performance baseline

Female source: 17,571 vertices / 34,554 triangles. Unity splits vertices at import seams to **20,216** while preserving 34,554 triangles. One visible SkinnedMeshRenderer, 51 skin bone references, 29 blendshapes and one shared material per character. The optional selection marker is an additional MeshRenderer only when selected. Prototype `AlwaysAnimate` and `updateWhenOffscreen=true` favor inspection and deliberately overstate an eventual culled background-character cost.

| Visible real characters | Skinned renderers | Triangles, excluding selection | Skin bone references | Shared body materials |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 34,554 | 51 | 1 |
| 5 | 5 | 172,770 | 255 | 1 |
| 10 | 10 | 345,540 | 510 | 1 |

These are measured asset counts multiplied by instance count, not a GPU frame-rate guarantee. Meshes, clips and the neutral material are shared; Animator evaluation, transforms, skinning and instance blendshape weights scale with people. Compare against the capsule's rigid mesh/no skinning before budgeting a crowd. No production LOD was generated. Eventual strategy: full face/skin detail for inspected people; reduced geometry, bones and facial work at gameplay distance; aggressive animation update/culling and a low-cost far representation for crowds. Choose thresholds from camera distance and measured target hardware rather than premature fixed budgets.

Actual temporary **1/5/10** instance groups were created and rendered in Studio. The existing capsule mesh measured **832 triangles**, one rigid renderer, no bones or Animator. A manual `Animator.Update(1/60)` CPU microbenchmark, 30 warmups then 300 samples using elapsed UTC time, measured **0.0503 / 0.1833 / 0.4354 ms per group update** for 1/5/10 respectively. This approximate Editor measurement excludes rendering, GPU skinning, navigation, domain work and full-frame scheduling. It is not an FPS, GPU, player-build, target-hardware or crowd budget result. Detailed values are in `performance.txt`. No profiler/GPU capture or LOD generation was performed.

## 28–32. Repository and validation ledger

Before modification, `git fetch origin` succeeded. HEAD and fetched `origin/master` both identified **2975e3aac585b750ec1372ae9eab141bdb050be4**. Current branch was `fix/unrestricted-applicant-hiring`, with four existing modified recruitment/carry files. Because the tree was not clean, no new feature branch was created and none of those edits was discarded.

During this session an external operation advanced that branch to **9aa2b98** (`Allow unrestricted applicant job assignment`) and established its origin tracking branch. This task did not create that commit or push it. Work stays isolated in new prototype files; review should transplant only these files onto the desired feature branch later.

Reviewable changes: `.gitignore`, the prototype runtime adapter, its Editor builder, local-license-dependent prototype tests/runner, audit scripts, this report, source evidence appendix and a short project-context update. Unity-generated .meta files accompany new code. Four selected runtime FBXs plus generated material/controller/prefab and their metadata live under the ignored prototype root. Purchased .blend/GLB/other-family files were not copied into Assets.

The tests use an isolated NavMesh and scene, restore saved Studio, and do not modify production prefabs. Missing licensed local imports cause this optional test fixture to be skipped, rather than requiring purchased assets in ordinary CI. Tests must not be treated as passed when skipped.

Validation ledger:

| Criterion | Result / evidence |
| --- | --- |
| All four sources, variants, rig/mesh/face metadata | Initial source evidence plus child addendum; all 85 current source hashes and timestamps preserved. Child metadata inspection does not establish runtime/export validity |
| One adult Humanoid Avatar and 29 shapes | Passed, `unity-import.txt` and import JSON; no forced missing mappings |
| Idle → route → Walk → arrival → Idle; Run | Passed, `SuppliedHumanoidWalksArrivesRunsAndDeformsFace`, final three-scenario run |
| Facial controls deform actual vertices | Passed, same runtime test; jaw, smile, frown, funnel and tongue; `runtime-face-proof.txt` |
| Quick-click selection, hold pickup, carried presentation, separate drop click, invalid retention, contextual Practice Comedy, Escape | Passed, `RealVisualUsesExistingClickHoldDropCancelAndContextActions`; stable person identity |
| Hover raycast/inspection semantics | Physics hover path exercised by the real controller; no separate visual approval of information-card typography/layout |
| Two real characters: navigation, distinct positions, arrival, active Socialize | Passed, `TwoRealVisualsReachDistinctSocializePositions`; final actual root separation **1.13010 m**; no conversation animation |
| Existing carry/interaction/autonomy/Socialize/recruitment/construction workforce regressions | **66 passed, 0 failed, 0 skipped**, `existing-regressions.xml` |
| Clean compilation | Confirmed after fixes; final Console inspection recorded in local evidence |
| Ground-contact quality | **Failed as an adoption gate:** walk/run penetration above; idle passes |
| Rendering/art | Studio Services and sampled poses inspected in PC URP; **technical only, not art approval** |
| Stage 1 / Stage School comparison, full building traversal, sitting/furniture contacts | Not completed; source clearance comparison only |
| Performance | Structural counts, 1/5/10 renders and limited Animator CPU sample; GPU/frame budget not measured |
| Child runtime hierarchy/Avatar/export | Not run, explicitly deferred required follow-up |
| Player build | Not run; this was an Editor prototype evaluation |

There are **69 distinct passing tests** across the final evidence: **3 passed, 0 failed, 0 skipped** in `prototype-verified.xml`, plus **66 passed, 0 failed, 0 skipped** in `existing-regressions.xml`. The first prototype run passed carry and Socialize but failed the locomotion wall-clock deadline. NavMesh registration and background Editor execution were made explicit in the isolated fixture. Two diagnostic recompiles were needed (ambiguous TestMode and a malformed diagnostic string); stale-assembly attempts are retained as failed evidence rather than counted as new validated runs.

Final evidence review found that a retained test-run callback had overwritten the earlier targeted locomotion XML with subsequent regression results. The runner now unregisters its callback and checks the pending result filename before saving. All three prototype scenarios were rerun successfully to create `prototype-verified.xml`. A subsequent two-test carry-presentation run passed; the prototype XML still contains its three passing scenarios and earlier run timestamps (`runner-cleanup-verification.xml`, `runner-evidence-preservation.txt`). These repeated tests add no distinct test count. The overwritten `prototype-locomotion-final.xml` is diagnostic history only, not prototype pass evidence; earlier failed runs also remain locally available.

No existing production .cs file, recruitment prefab or saved scene was changed. `git diff --check` passes. Final Editor inspection confirms Edit Mode, no compilation failure, exactly one loaded scene (`Assets/Scenes/Studio.unity`) and that scene is not dirty. The final Console query returned zero errors. Evidence: `editor-final-state.txt` and `console-final.json`. Git status also flags `ProjectSettings/ProjectSettings.asset`, but its current bytes exactly match HEAD and its content diff is empty; no settings change is part of this work. `.gitignore` excludes both licensed prototype runtime imports and their root .meta; all review/test output stays in the already-ignored TestResults directory. Nothing was committed or pushed by this task.

## 33–35. Adoption decision and next milestone

**Promising foundations; not yet approved as four production canonical bases.** The small adult shape-key export is a viable route for the isolated person presentation prototype. Purchased format count and valid Humanoid import do not establish a complete facial, identity, wardrobe or child pipeline.

Before global replacement: resolve blink/brow/gaze and usable eye materials; choose a documented facial export contract; normalize source naming through semantic aliases; validate male and child Avatars and repair child hierarchy/scale; establish material/body-part ownership; finish retarget/contact/collision review at gameplay speed; confirm performance with representative dressed crowds. Keep capsules as production fallback.

Recommended next milestone: an **adult runtime-character acceptance pass** on this same representative female, covering locomotion contact tuning, skin/eye/hair material separation, and a decision between facial bones and completed blendshape coverage. Have the user approve its gameplay-scale appearance and motion before expanding to the other body families. This milestone does not authorize Star Maker or a global replacement.


## Adult Male foundation follow-up

The later [Adult Male canonical foundation validation](MaleCanonicalFoundationValidation.md) independently verifies the Male bald scalp, native-data preservation, valid 51-bone Humanoid, 29 finite supplied lower-face channels and an isolated source-plus-corner-corrective BlinkBoth. Eight focused Unity tests passed. The report discloses the small oblique corner opening, optional missing bones, existing ground-contact limitation and technical-only visual status. Earlier Male-unvalidated statements in this initial audit describe its original milestone; use the follow-up for current Male evidence. Male work is uncommitted at the Female checkpoint `16b90e90a8d13205f9267996d1e5284123355334`. Boy/Girl hierarchy/export/Avatar validation remains outstanding.

## Boy/Girl structural follow-up

The [child structural foundation validation](ChildCanonicalFoundationValidation.md) records the later Male checkpoint `5a4e25513583bffc7670cde9911a4b98bebda356` and independently validates both child lightweight Humanoids, native scale, 29 supplied channels, complete scalps and bounded full-source facial probes. Four child tests passed. Earlier child-unvalidated statements above and in the inventory addendum are historical; the follow-up distinguishes detached heavy facial exports from the working lightweight candidates and explains the inactive Boy object's 1.882 m state. Boy is recommended first because both pass this structural gate and Boy was the planned order. Child blink authoring, full person integration and final art remain unapproved/unimplemented. Child work remains uncommitted at the Male checkpoint.

## Boy canonical reference follow-up

The child structural work was subsequently checkpointed and normally pushed as `b28167e309a9c4b44c420de9c6d303f43407f5d4` (`Add child character foundation validation`). The [Boy canonical reference validation](BoyCanonicalReferenceValidation.md) documents the next isolated, uncommitted milestone: native bald preservation, user-accepted Boy ReviewR3 source blink, independently proven family-local delta transfer, 50-bone/30-shape Unity candidate and the full child person proof. The report discloses genuine small corner openings/internal contacts, grey-render shading, unchanged locomotion penetration and the existing EmployeeAgent policy that enforces a 2 m navigation/selection capsule. Boy's visual remains at 1.466252 m; production height-aware interaction data is a follow-up. Female, Male and Girl assets/evidence remain preserved. Girl's full implementation, other facial systems, final materials and production adoption were not started. Earlier status statements above remain a historical record of their individual milestones.
