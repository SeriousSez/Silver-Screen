# Adult Female facial runtime feasibility investigation

2026-10-02 · SilverScreen · investigation and isolated prototype only.

**Decision: do not adopt the reduced facial-bone hybrid.** The source can move its eyelids independently, direct gaze and move its brows. Unity can drive facial bones alongside a valid Humanoid body and all 29 supplied lower-face BlendShapes. However, the tested 95-bone reduction fails to reproduce the source blink: the upper lids fold inward and leave an unacceptable opening. The reduced deformation already diverges in Blender, before Unity import. This is a failed representation/transfer gate, not proof that Unity cannot animate a face or that every possible hybrid is impossible.

**Source-closure correction, 2026-10-02:** the [subsequent one-shape baking experiment](FemaleBakedUpperFaceExperiment.md) reproduces the saved source endpoint exactly but finds actual exposed eyeball geometry in closer matched views and ray tests. The earlier claim that this endpoint fully closes both eyes was overstated. Independent lid control and the rejected hybrid's measured transfer error remain established; source endpoint contact/coverage is not accepted.

The investigation stops at that gate as requested. No corrective sculpting, weight repainting, new facial system, locomotion fix or production integration follows from this report. The original lightweight prototype remains available for comparison. All renders are **technical evidence only, never art approval**.

## Evidence, reproduction and scope

The purchased root remains `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models`. Only Adult Female assets were investigated or imported. All 59 original source hashes and modification times still match the previous audit inventory. Embedded `rig_ui.py` scripts were not executed; Blender opened sources with `use_scripts=False` and no source file was saved.

- [Complete bone classification and exact reduced set](FemaleFaceBoneEvidence.md).
- Original cross-family audit: [CharacterBaseAudit.md](CharacterBaseAudit.md), [source inventory](CharacterBaseSourceEvidence.md).
- Detailed local evidence: `TestResults/FemaleFaceInvestigation/`, already Git-ignored. It contains source structure, constraints, pose matrices/evaluated points, export comparisons, Unity metrics, XML tests and screenshots.
- Reproducible tools: `Docs/CharacterBaseAuditTools/inspect_female_face.py`, `probe_female_poses.py`, `render_female_probe.py`, `export_female_hybrid.py`, and `probe_female_fbx_eyes.py`. The scripts require explicit external source and local evidence/output paths.
- Derived and copied licensed imports: `Assets/SilverScreen/Art/Characters/FemaleFacePrototype/`, including its root `.meta`, explicitly Git-ignored. The four supplied comparison FBXs are copies; `Models/FemaleHybrid.fbx` is generated output.
- Unity builder: **SilverScreen > Characters > Female Face Investigation > 1 Import and build isolated hybrid**. It requires the locally generated export and evidence JSON. It creates `SS_FemaleFacePrototype.prefab`; it does not replace `SS_CharacterPrototype.prefab` or production recruitment assets.

Environment: Blender 5.0.1; Unity 6000.6.3f1; URP 17.6.0. Unity import, execution, tests and rendering used the existing official local Unity relay. No new Unity package was installed. The previous walk/run ground penetration remains a separate, unfixed issue.

## 1–4. Exact architecture and supplied export contents

The source `.blend` is `stylized-female-body-base/Stylized_Female_Body_Base_Facial_Rig/Facial Rig/StylizedFemaleBodyBaseMesh3DModelFacialRig.blend`.

| Source armature | Total | Deform flags | Other bones | Purpose |
| --- | ---: | ---: | ---: | --- |
| `Armature_Character1.001` | 441 | 137 | 304 | Facial authoring character: 50 body + 87 facial deformation bones |
| `Armature_Character_shape_keys.001` | 145 | 50 | 95 | Body/control rig for the joined shape-key character |
| `rig` | 298 | 87 | 211 | Additional facial rig-generation/authoring infrastructure |

All 137 deformation bones in the main facial armature have actual nonzero mesh skin membership. Bone-parented eyes and teeth also matter: ordinary vertex-group membership alone misses their attachment. The audited main armature contains 99 Copy Transforms, 85 Stretch To, 52 Copy Location, 18 Damped Track, four Copy Rotation, four Copy Scale, four IK and two Limit Location constraints. All 137 source deform bones have one B-Bone segment; this is not a hidden multi-segment B-Bone export problem. Both relevant mesh armature modifiers use linear deformation, not Preserve Volume.

Some source collections are hidden. They must be exposed **in memory** for a valid evaluated dependency graph. Initial file-load warnings named five mouth/eye drivers; after graph evaluation the inspected drivers were valid. They are property-driven SUM drivers. Those transient load warnings are not evidence that the rig needs its embedded UI script or that the source drivers remain broken.

The body chain remains `ROOT > Hips > Spine > Spine1 > Spine2 > Neck > Head`, with the existing shoulder/arm/hand and hip/leg/foot/toe chains. There are two thumb joints per side. The facial system descends through `ORG-face`, eye mechanisms and animator controls rather than a simple set of independent Head children.

### Mesh correspondence

The facial source separates body `character2.003` (12,937 vertices), hair `character2.002` (1,074), eyes `.004/.005` (354 each), lower gums (1,224), upper gums (1,412) and tongue `GumsLower_lowres.003` (216). These sum to **17,571 vertices / 34,554 triangles**, the same total as joined `Character_shape_keys.002`.

An explicit index-offset mapping matches **all 17,638 polygons** between the source pieces and the joined shape-key mesh. Thus a topological correspondence exists; vertex count alone was not used as proof. Nevertheless, the evaluated neutral coordinates are not identical: maximum difference **5.807 mm**, with **450 vertices over 1 mm**; the body component reaches 5.262 mm and tongue reaches 5.807 mm. Compatible connectivity does not establish compatible bind deformation or neutral facial surface positions.

Facial source body has no assigned material slot, hair/eyes use `Material`, mouth parts use `teeth_tongue3`. Supplied FBX assigns one slot per exported mesh; the shape-key export has one joined material slot. Both source variants have `UVMap` and `map1`. No production skin/iris material was authored.

### Supplied FBXs and actual Unity imports

The filenames below preserve the supplier's unmatched parentheses.

| Supplied file | Blender-imported bones | Unity transforms | Unity renderers / skin bone references per renderer | Shapes | Humanoid result |
| --- | ---: | ---: | --- | ---: | --- |
| `StylizedFemaleBodyBaseMesh3DModelFacialRig(all_bones).fbx` | 367 | 374 | 5 / 234 | 0 | Valid body Avatar; automatic eye mappings are wrong |
| `StylizedFemaleBodyBaseMesh3DModelFacialRig(deform_bones_only.fbx` | 241 | 248 | 5 / 140 | 0 | Valid body Avatar; automatic eye mappings are wrong |
| `StylizedFemaleBodyBaseMesh3DModelFacialShapeKeys(all_bones.fbx` | 145 | 148 | 1 / 145 | 29 | Valid Humanoid; no facial bone rig |
| `StylizedFemaleBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | 51 | 54 | 1 / 51 | 29 | Valid Humanoid; existing lightweight route |

The facial-rig files reside in `Stylized_Female_Body_Base_Facial_Rig/Facial Rig/`; the shape files in `Stylized_Female_Body_Base_Shape_Keys/Shape Keys/`, under the female directory. The all-bone facial FBX is **not the complete 441-bone Blender control rig**. It contains extra body/control/export transforms and end bones, but the original MCH/ORG eye-control chain and Blender constraints do not survive as a functional runtime rig. Counting imported `use_deform` flags, or counting `_end` transforms as useful facial joints, would be misleading.

Both facial FBXs have the same geometry: body plus eyes are joined as `character2.003` (13,645 Blender vertices); hair and mouth parts remain separate. Unity seam splitting yields **19,903 vertices across five skins**, still 34,554 triangles. The joined shape-key route has **20,216 Unity vertices**, one skin and the same triangle count.

**Critical eye-export result:** all **708 eyeball vertices** in each supplied facial FBX are weighted to **Head**. The actual source eyes are bone-parented to `MCH-eye.R` (`character2.004`) and `MCH-eye.L` (`character2.005`). Independent gaze therefore does not survive either supplied facial FBX. A custom eye attachment/export is required for gaze, even when choosing the so-called all-bone file.

Unity automatically maps `DEF-cheek.T.L/R` to LeftEye/RightEye on both facial FBXs. Their green/valid Avatar status must not be interpreted as correct facial mapping. `DEF-jaw` is selected for Jaw. A controlled body-only mapping or verified explicit eye/jaw assignments is required.

Direct Unity deformation is proven: a 20-degree local rotation of `DEF-lid.T.L.002` moves actual vertices by about **3.762 mm** in either supplied facial FBX. Driving `jawOpen` in either shape-key FBX moves vertices by about **35.387 mm** at raw imported scale. This proves accessible deformation data, not a complete or natural expression system.

## 5–8. How blink, gaze and brows work

### Eyelids and independent control

There is no supplied semantic `Blink` control/shape. For each side, the tested control pair is `lid.T.L/R.002` and `lid.B.L/R.002`. The source probe moves the upper centre down 25.5 mm and lower centre up 5.5 mm in armature/world-aligned vertical direction, converted to each control's local axes. These are experimental endpoint poses, not an approved blink range or timing curve.

Neighbouring `.001/.003` controls copy 60% of the central control's translation. `MCH-lid.*` Damped Track constraints point from the eye pivot toward the lid controls. Eight `DEF-lid.*` bones per eye then Stretch To neighbouring mechanism targets, including scale changes. Both upper and lower lids participate. Merely rotating one DEF joint cannot stand in for this evaluated system.

**PROVEN:** source left/right inputs independently move the corresponding eight lid-bone matrices. Source neutral, BlinkBoth and a left-wink endpoint were rendered and inspected, but the initial full-closure interpretation is superseded by the later close-up/ray evidence linked above. Source mouth-interior vertices also move due to skin weights. Left wink has approximately 5.32 mm and right wink 8.84 mm maximum movement across the centreline, concentrated in internal oral geometry rather than the opposite eyelid. Do not silently infer perfectly isolated skin weights from independent controls.

**UNKNOWN:** natural temporal blink motion, prolonged closure at extreme gaze, and artist-approved eyelid/eyeball contact over a continuous range. No blink scheduler or corrective sculpting was implemented.

### Eye gaze

`eyes` provides the shared target parent; `eye.L/R` are separate targets. `master_eye.L/R` establish local eye/eyelid frames under `ORG-face`. `MCH-eye.L/R` Damped Track their corresponding target along local Y; `ORG-eye.*` inherits that orientation. Each actual eyeball is parented to its corresponding MCH eye bone. `MCH-eye.*.001` follows the rotating eye tip and contributes 50% Copy Location to the central upper/lower lid controls, providing lid-follow behaviour.

The `eyes_follow` property drives `MCH-eyes_parent` following `ORG-face`. A runtime system should express gaze in head-local coordinates or a clamped target direction, with two direct eye transforms and explicit eyelid follow. It does not need to evaluate Blender's target/constraint graph per NPC.

The source experiment translates the shared target ±100 mm horizontally or ±70 mm vertically, resulting in **21.27°** horizontal, **15.09°** up and **15.37°** down eye rotation. These directions are measured, not inferred from names. The supplied eye geometry is asymmetric about its bone pivot: its vertex centroid travels 3.36–4.66 mm during these rotations. A stationary-centroid test is inappropriate.

### Brows and upper-face expressions

The tested meaningful controls are `brow.T.L/R`, `.001` and `.003`, translated ±8 mm vertically. `.002` interpolates neighbours through Copy Location. Stretch To deformation affects upper/lower brow chains, forehead and nearby cheeks. A raise/lower/furrow-like pose is available; its aesthetic usefulness and full expression range remain unapproved.

Across the tested pose set the changing weighted facial bones are exactly: **16 eyelid + 16 brow + 6 forehead + 4 cheek = 42**. Source brow raising moves vertices about 7.65 mm. These eye-direction/closure/brow concepts are practical control inputs, but they depend on evaluated transforms or baked geometry; importing animator-control names alone is insufficient.

## 9–13. Reduced Unity prototype and hybrid result

The isolated candidate has **95 bones**: 51 original body/root bones, the 42 empirically active facial deformation bones, and two generated `SS_Eye_L/R` transforms. All facial/eye bones are reparented beneath Head, with source-evaluated pose samples baked to local translation, rotation and scale. There are **zero Blender constraints or mechanism/control bones** in the generated skeleton. The exact set is in the bone evidence appendix.

The derived mesh preserves topology, both UV channels and **all 29 original lower-face keys**. Selected facial skin contributions are transferred using the proven source vertex correspondence, with residual Head weighting retained; eyes are assigned rigidly using the actual source parent side. Neither source suffix `.004/.005` nor nearest-point matching is used to guess eye side. An initial export-side mistake in that assignment was identified in visual review and corrected; the failed initial gallery remains in `first-hybrid-gallery/`.

Unity initially made a second bad automatic mapping: `DEF-lid.B.L.001` became Jaw. The builder now reuses the existing validated 50-joint body mapping, leaving Humanoid eye/jaw slots unused for this independently controlled prototype. It also applies an explicit transform mask to import the facial animation curves. Without that mask most extra facial curves were omitted. This follows Unity's [animation import masking mechanism](https://docs.unity.com/en-us/engine/6000.6/manual/animation-section/animation-mecanim/animation-clips/animation-mask-on-imported-clips). Runtime body animation remains the existing Idle/Walk/Run controller; facial poses are applied afterward in LateUpdate.

### What is proven

- Valid Humanoid body and 29 simultaneous lower-face BlendShapes on the **same** runtime mesh.
- Runtime facial bone movement, independent opposite-eye region stability, neutral restoration and finite/bounded vertices in the numeric checks.
- Eye transform rotation and brow movement; source export requires explicit eye restoration.
- Smile + blink, smile + gaze, frown + brow lower, jaw open + blink can all be driven on the same representation. Technical renders exist for all four combinations.
- Existing EmployeeAgent navigation, Humanoid animation, carry/context interaction and Socialize were exercised on the candidate; the body/navigation/carry architecture was not replaced.

These are implementation/coexistence proofs. They do **not** establish correct blink surface shape, contact or finished expression quality.

### Why the candidate is rejected

The reduced candidate folds the upper lid inward and leaves a slit/opening with poor eye contact. The combined smile/blink and jaw/blink inherit that defect. Its mismatch against the source is still measured below, but the source endpoint itself also has residual eye exposure, as established in the subsequent baking experiment. Neutral is restored correctly, so reversibility does not imply that the intervening deformation is acceptable.

The exporter compares the reduced Blender result against the original evaluated source pose delta applied to the shape-key neutral. Maximum differences before Unity are:

| Pose | Maximum difference | Vertices differing by over 1 mm |
| --- | ---: | ---: |
| Neutral | 0.000604 mm | 0 |
| Blink both | **7.155 mm** | **168** |
| Look left | 1.408 mm | 19 |
| Brow raise both | 2.892 mm | 91 |

This isolates a mismatch in the simplified binding/weight/neutral-pose transfer before Unity. It is **not established** that a Unity renderer bug, missing Blender UI, B-Bone segmentation or dual-quaternion skinning causes it. Neutral surface differences and collapsing deformation into a Head-parented representation need reconciliation; the precise corrective transfer has not been proved. The experiment does not license a claim that every 95-bone implementation would fail.

The prototype keeps the project's existing FourBones skin quality while retaining up to ten import influences. This can introduce additional differences and deserves a controlled comparison later, but it cannot explain the already-measured Blender-side mismatch by itself.

**Stop condition reached:** this clean reduced representation does not reproduce required eyelid deformation. We stop rather than add corrective geometry, repaint weights, carry the complete control rig, or build another framework to hide that failure.

### Smallest credible direction

**95 bones is the tested upper-face bone candidate, not a proven minimum or production recommendation.** A likely smaller alternative is the existing 51-bone body/root plus two eye transforms, with carefully baked and validated upper-face shape poses added to the lower-face library. That would make a **53-bone candidate**, trading skin-transform work for additional shape data. It is **LIKELY**, not implemented or validated here. Exact shape count, interpolation, eyelid follow and combination corrections cannot be promised yet.

A family with all-blendshape gaze or separate rigid eye objects may have a different minimum. This report does not authorize unifying Male, Female, Boy and Girl or morphing between their topologies.

## 14. Runtime cost and scaling

| Metric | Existing lightweight prefab | Rejected reduced hybrid |
| --- | ---: | ---: |
| Whole-prefab transforms | 57 | 101 |
| Body skin bone references | 51 | 95 |
| Facial transforms explicitly written | 0 | 44 |
| SkinnedMeshRenderers | 1 | 1 |
| Shared body materials | 1 | 1 |
| Unity vertices / triangles | 20,216 / 34,554 | 20,216 / 34,554 |
| Lower-face BlendShapes | 29 | 29 |
| Editor-reported shared mesh native memory | 22,842,184 bytes | 22,864,756 bytes |

The optional selection marker is a separate rigid renderer. The mesh memory measurement is Unity's Editor runtime-memory estimate of the shared mesh object, including its morph data; it is not a player memory budget, GPU allocation or a per-character duplication cost. This experiment increases that mesh estimate by 22,572 bytes. The serialized prototype pose tables add roughly 22.9 KB of numeric pose entries per component instance before object/array overhead; a production binding should share immutable family data rather than copy it into every character.

Actual preview-scene groups of up to 100 instances were created and destroyed. A bounded Editor microbenchmark called `Animator.Update(1/60)` and, for the hybrid, explicit face Apply; three warmups, 30 measured group iterations, elapsed UTC time:

| Instances | Lightweight ms/group | Hybrid ms/group | Hybrid transforms | Hybrid skin references | Triangles |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.03309 | 0.05054 | 101 | 95 | 34,554 |
| 10 | 0.36677 | 0.35106 | 1,010 | 950 | 345,540 |
| 25 | 0.91825 | 0.94245 | 2,525 | 2,375 | 863,850 |
| 50 | 1.81573 | 2.11649 | 5,050 | 4,750 | 1,727,700 |
| 100 | 3.79375 | 4.34555 | 10,100 | 9,500 | 3,455,400 |

Small-sample noise is visible; the 10-instance result must not be called a speed improvement. This measures explicit animation/pose calls only, excluding rendering, GPU skinning, navigation, simulation, full-frame scheduling, player compilation and platform costs. No crowd frame-rate claim is made. Both prototypes favour inspection with AlwaysAnimate/offscreen skin updates.

Future near characters may use complete facial behaviour; medium distance should reduce update cadence and expression detail; far distance can stop facial updates while retaining body animation. Turning off the facial controller alone does **not** remove 44 transforms or skin influences from the mesh; a cheaper far representation may need an asset variant. No facial LOD was implemented.

## 15–16. Problems and required authoring/export work

| Finding | Evidence/status | Consequence |
| --- | --- | --- |
| Folded/open hybrid blink | **PROVEN visual failure** plus Blender delta mismatch | Blocks adoption |
| Supplied facial FBX eyes rigid to Head | **PROVEN**, all 708 eye vertices | Custom eye export/attachment needed |
| Automatic Humanoid face mappings select cheek/eyelid bones | **PROVEN** importer mappings | Body-only/explicit checked mapping required |
| Same topology, different evaluated neutral coordinates | **PROVEN** | Blind weight/pose transfer is unsafe |
| Eyelid weights move internal oral geometry | **PROVEN** source deltas | Review weight ownership before production |
| BlendShapes plus bone transforms execute together | **PROVEN numeric/render evidence** | Does not rule out contact defects or corrective needs |
| Source/Unity eyes have no iris/pupil treatment | **PROVEN visual limitation** | Direction is numerically measurable but poorly readable in grey renders |
| Stable natural gaze/blink combination over a continuous range | **UNKNOWN** | Only deterministic directions/endpoints sampled |
| Automatic blinking, LookAt solver, lipsync, final emotional control | **NOT SUPPORTED by this prototype** | Deliberately unimplemented |

For a facial-bone route, a derived authoring/export step must evaluate controls, establish compatible neutral/bind geometry, bake the required facial transforms, preserve eye attachment, remove unrelated controls/constraints/end bones, retain shape keys and normals, and validate the exported result against source deformation. Every generated export must remain separate from purchased inputs. The current script demonstrates that workflow structure but **fails its deformation parity gate**.

A future baked-shape route would instead sample evaluated upper-face deformation on the verified vertex correspondence and validate closure/interpolation plus lower-face combinations. This is a recommendation for an isolated next experiment, not a completed solution.

## 17–19. Semantic binding and production boundaries

Use **SilverScreen semantic command → family facial binding → bone and/or BlendShape implementation**. Gameplay should ask for BlinkLeft/BlinkRight, normalized gaze yaw/pitch or LookAt intent, BrowRaise/Lower per side, Smile/Frown/JawOpen/MouthPucker. It should not know `DEF-lid.*`, `SS_Eye_*`, misspelled purchased shapes or numeric vertex/shape indices.

The small `FemaleFacePrototype` demonstrates that boundary with semantic enums, serialized pose bindings and cached lower-face shape indices. Its shared LookLeftBlink sample is a deterministic combined pose, not a general gaze/blink mixer. Unsupported semantics return no binding; no silent substitution should claim full capability. Existing lower-face spelling differences still require family aliases.

For later production, bindings should declare supported channels, neutral values, ranges, head-space axes, pose/shape resources, corrective combinations, ownership and Animator application order. Resolve names once, validate them on load, and share immutable binding data. Keep eyelid/eye/brow ownership separate from lower-face expression and Humanoid muscle ownership to prevent double application. A child family can implement the same semantic channels using entirely different bones or morphs; this architecture does not imply common mappings or geometry.

Only after a deformation acceptance pass should production work consider idle blinking, gaze targets, acting/performance direction, Movie Maker controls, dialogue/lip movement or emotional mixing. None is implemented here.

Do **not** carry the 441-bone authoring rig, IK/FK controls, MCH/ORG constraint graph, unused face/ear/jaw branches, export end bones, test controllers, raw auto-mappings, unconditional AlwaysAnimate settings or the failed prototype skin transfer into NPC production.

## Validation and repository handoff

**Overall: investigation complete; facial representation rejected for adoption.** Numeric tests and visual/deformation acceptance are separate gates; an XML pass never overrides the rejected blink screenshots.

| Criterion | Result and evidence |
| --- | --- |
| Source control evaluation / topology correspondence | **PROVEN**; `source-pose-probes.json`, `female-structure.json` |
| Supplied facial FBX eye membership | **PROVEN** Head-only, `fbx-eye-membership.json` |
| All four female variants imported and direct skin/shape deformation exercised | **PROVEN**, `unity-variants.txt`, `runtime-cost-and-variants.txt` |
| Hybrid Humanoid, facial bone + 29-shape coexistence, neutral restoration | **PROVEN numerically**, final `final-facial-numeric.xml` |
| Independent opposite-eye region stability / rigid eye transforms / brow deformation | **PROVEN numerically**, final focused test and `runtime-deformation.txt` |
| Hybrid navigation/Idle/Walk/Run/arrival, carry/context/Escape, Socialize | **3 passed**, `final-runtime-tests.xml` |
| Original lightweight prototype navigation and Socialize | **2 passed**, same combined run |
| Original lightweight carry/context regression | Failed contextual Practice assertion in combined run; **passed unchanged in isolation**, `lightweight-carry-isolation.xml`. Intermittent cause unresolved; no production fix claimed |
| Runtime technical gallery | **Passed capture scenario**; 18 final images; source and representative runtime poses visually inspected |
| Natural/faithful blink and combined eyelid contact | **FAILED** visual/source-parity gate |
| Full continuous gaze/blink/expression range | **UNKNOWN**, not validated |
| Performance | Counts, shared-mesh estimate and bounded manual Editor timing only; no GPU/player budget |
| Production integration, child/male work, locomotion correction, player build | **NOT RUN**, outside scope |

The combined runtime run was **6 passed / 2 failed / 0 skipped**. The final two focused checks each passed, so there is passing evidence for all **eight distinct numerical/integration/capture scenarios**, but no single final all-green batch is claimed. The mixed batch failures remain on disk. One was the intermittent original carry/context failure above. The other was an incorrect test assumption that an eye's vertex centroid should move less than 3 mm: the source itself moves 3.36–4.66 mm. It was replaced by **socket ownership plus per-vertex rigid-transform parity** (20 micrometre tolerance), not by relaxing the centroid threshold. That stronger final facial check passed. No visual assertion was weakened or declared passed.

Earlier diagnostic failures are also retained: initial missing facial import mask/wrong automatic Jaw mapping, the exporter eye-side assignment mistake, and a relay-only timing snippet that could not reference Stopwatch. The final bounded timing used elapsed UTC time. They are not hidden inside a claim of first-attempt success.

### Technical screenshot evidence

The source and Unity blink comparison is the key acceptance evidence. Lighting differs between Blender and URP; the rejected finding is the lid surface/closure, not the lighting or material colour.

| Source evaluated blink | Reduced Unity blink — rejected |
| --- | --- |
| ![Source technical blink](<C:/Repos/Unity/Silver Screen/TestResults/FemaleFaceInvestigation/source-BlinkBoth.png>) | ![Rejected Unity technical blink](<C:/Repos/Unity/Silver Screen/TestResults/FemaleFaceInvestigation/unity-BlinkBoth.png>) |

| Required view | Local evidence |
| --- | --- |
| Neutral / restoration | [Neutral](../TestResults/FemaleFaceInvestigation/unity-Neutral.png), [restored](../TestResults/FemaleFaceInvestigation/unity-NeutralRestored.png) |
| Independent blink | [Source left wink](../TestResults/FemaleFaceInvestigation/source-BlinkLeft.png), [Unity left](../TestResults/FemaleFaceInvestigation/unity-BlinkLeft.png), [Unity right](../TestResults/FemaleFaceInvestigation/unity-BlinkRight.png) |
| Gaze | [Left](../TestResults/FemaleFaceInvestigation/unity-LookLeft.png), [right](../TestResults/FemaleFaceInvestigation/unity-LookRight.png), [up](../TestResults/FemaleFaceInvestigation/unity-LookUp.png), [down](../TestResults/FemaleFaceInvestigation/unity-LookDown.png) |
| Brows | [Raise both](../TestResults/FemaleFaceInvestigation/unity-BrowRaiseBoth.png), [raise left](../TestResults/FemaleFaceInvestigation/unity-BrowRaiseLeft.png), [raise right](../TestResults/FemaleFaceInvestigation/unity-BrowRaiseRight.png), [lower both](../TestResults/FemaleFaceInvestigation/unity-BrowLowerBoth.png) |
| Gaze + blink | [Combined technical pose](../TestResults/FemaleFaceInvestigation/unity-LookLeftBlink.png) |
| Lower-face + upper-face | [Smile + blink](../TestResults/FemaleFaceInvestigation/unity-Smile-BlinkBoth.png), [smile + gaze](../TestResults/FemaleFaceInvestigation/unity-Smile-LookLeft.png), [frown + brows](../TestResults/FemaleFaceInvestigation/unity-Frown-BrowLowerBoth.png), [jaw + blink](../TestResults/FemaleFaceInvestigation/unity-JawOpen-BlinkBoth.png) |

The gallery waits three engine frames after every pose change to avoid stale SRP skin buffers. Numeric Blender/Unity comparisons, not image brightness, establish deformation and restoration. Grey eyes cannot communicate gaze direction clearly without iris treatment; axis rotation and vertex-transform evidence supplement the images.

No production capsule, recruitment prefab, saved Studio scene, material library, input system, navigation implementation or normal spawn path was changed. The only existing code edits are the optional prefab path on the local prototype spawn helper and parameterization of its tests so the original and facial candidate run the same integration scenarios. New runtime code, builder and tests are confined to the isolated prototype.

Current branch: `fix/unrestricted-applicant-hiring`; starting HEAD `9aa2b98d491cfcce6706c7777874307c8da575ed`. The existing audit/prototype work was already uncommitted and was preserved. No commit, push or PR was made. No player build, GPU profile or production facial integration was attempted.

Final verification: HEAD is unchanged; all 59 source hashes/timestamps remain unchanged; `git diff --check` passes; licensed exports/copies and review evidence are Git-ignored. Unity is in Edit Mode, not compiling, with no compilation failure and exactly one loaded, clean `Assets/Scenes/Studio.unity`. The final Console query reports zero errors. The earlier failed tests remain in the report and XML evidence. Git status still flags `ProjectSettings/ProjectSettings.asset`, but its bytes exactly match HEAD and its content diff is empty; no settings change is part of this investigation.

## 20. Recommended next milestone

An **Adult Female neutral/bind and eyelid export acceptance experiment**, starting with a single blink and the current source comparison, is the next useful gate. Compare a correctly reconciled reduced-bone binding against a small baked-upper-face-shape route plus two eyes. Require source/Unity deformation agreement and acceptable eyelid/eyeball contact before choosing the permanent representation. Keep the 29 lower-face shapes and existing Humanoid body as the controlled baseline.

Do not start that milestone automatically. Do not fix locomotion, integrate children, expand wardrobe or replace production capsules as part of this handoff.
