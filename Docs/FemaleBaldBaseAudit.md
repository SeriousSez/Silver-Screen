# Adult Female bald canonical-base check

Date: 2026-10-02. Inspected with Blender 5.0.1 and the project's running Unity 6000.6.3f1 / URP 17.6.0.

## Decision

**The supplied Adult Female has a complete scalp beneath independently removable hair. An isolated generated bald prototype is suitable for continued technical evaluation as this family's canonical body geometry.** No scalp reconstruction, welding, remeshing, UV repair or eyebrow deletion was necessary.

The final Unity prototype preserves the original retained vertex data, skin weights, 51-bone Humanoid setup and all 29 supplied BlendShape data sets. This does **not** certify 29 usable facial controls: six channels already produce invalid deformation in the original import, as detailed below. Earlier upper-face/blink acceptance failures remain unresolved.

All images are **plain-grey technical validation, not art approval**. This check does not approve likeness, proportions, skin, eyes, eyebrow appearance or final shading. The original prototype remains available for comparison; production character spawning is unchanged.

## Hair representation and scalp evidence

The purchased source root is `C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models`. The directly inspected file is `stylized-female-body-base/Stylized_Female_Body_Base_Facial_Rig/Facial Rig/StylizedFemaleBodyBaseMesh3DModelFacialRig.blend`. The prior inventory identifies the two supplied Female Blender files as byte-identical; this file contains both facial-rig and joined shape-key variants.

| Representation | Finding |
| --- | --- |
| Facial-rig Blender variant | Hair is the separate mesh object `character2.002`: 1,074 vertices, 1,079 polygons, 2,042 triangles. It has `UVMap`, material `Material`, and every hair vertex is weighted exclusively to `Head` at 1.0. |
| Facial-rig FBX variants | The earlier source inspection also records hair as a separate `character2.002` mesh in the all-bones, deform-bones and optimized facial FBXs. |
| Shape-key Blender / lightweight FBX variant | Hair is joined into `Character_shape_keys.002`, but is disconnected from the body. In Blender vertex numbering it occupies exactly 16,497–17,570 inclusive. All 1,079 hair polygons match the separate object's topology at that offset. No edge crosses between hair and retained geometry. |
| Hair islands | Four: swept hair cap (784 vertices), bun (258), and two hairpins/accessories (16 each). All four were removed. None is scalp or eyebrow geometry. |
| Underlying body | `character2.003`: one connected component, 12,937 vertices, 13,019 polygons, 25,870 triangles. All 25,954 edges have exactly two incident faces; there are no boundary edges. The joined variant retains this complete body component. |

The scalp conclusion is based on **actual geometry and rendered inspection**, not the absence of obvious gaps while hair is present. Front, three-quarter, profile, back and overhead bald views were inspected in Blender and Unity. Crown, temples, back of head and nape remain continuous. No open hairline, missing cap or newly exposed scalp hole was found. The hair object's own open edges belong to its shell and accessories, not the scalp.

The complete joined mesh contains disconnected eye and oral pieces as well as the body. It is therefore inappropriate to demand that the entire joined character be one watertight component. The closed, connected body/head surface is the relevant scalp evidence.

| Matched Unity view: original | Matched Unity view: bald |
| --- | --- |
| ![Original hair](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-three-quarter-hair.png>) | ![Bald head](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-three-quarter-bald.png>) |

| Rear scalp | Crown |
| --- | --- |
| ![Rear scalp](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-back-bald.png>) | ![Crown](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-top-bald.png>) |

Additional inspected views: [front](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-front-bald.png>), [profile](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-profile-bald.png>), [smile](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-mouthSmileLeft.png>), [open jaw](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-jawOpen.png>). Fine surface faceting and the supplied untextured eye appearance remain visible; these are not repaired or approved by this investigation.

## Eyebrows

No independent eyebrow hair object or eyebrow island was found among the source character meshes. The brow ridge/skin belongs to the continuous **body geometry**, and brow movement belongs to the **facial bone system**: 16 `DEF-brow.*` bones influence the body. The removable hair has no such facial weights.

No image textures are present in the inspected Blender file. Body and joined-mesh colour attributes are uniformly white, and the joined mesh's assigned material is a plain Principled material. There is no supplied painted or textured eyebrow treatment demonstrated by this source. This is not a claim about unrelated store preview images or future appearance assets. No eyebrow region, body vertex or brow bone was removed.

## Generated prototype and preservation

The final review asset is [SS_FemaleBaldPrototype.prefab](<C:/Repos/Unity/Silver Screen/Assets/SilverScreen/Art/Characters/FemaleBaldPrototype/SS_FemaleBaldPrototype.prefab>). It uses [AdultFemaleBald_RetainedData.asset](<C:/Repos/Unity/Silver Screen/Assets/SilverScreen/Art/Characters/FemaleBaldPrototype/AdultFemaleBald_RetainedData.asset>), a generated Unity mesh subset, while retaining the original validated prefab's Avatar, skeleton, material, animation controller, presentation bindings, scale and sole offset.

| Measurement | Original with hair | Generated bald |
| --- | ---: | ---: |
| Blender vertices | 17,571 | 16,497 |
| Blender polygons | 17,638 | 16,559 |
| Triangles | 34,554 | 32,512 |
| Disconnected components | 42 | 38 |
| Unity runtime vertices, including import splits | 20,216 | 19,005 |
| Runtime skinned renderers / bones | 1 / 51 | 1 / 51 |
| Supplied BlendShape data sets | 29 | 29 |

The generated runtime mesh reports 20,008,996 bytes through Unity's shared-mesh memory query. This is a local mesh measurement, not a character or game memory budget.

The body has **not** been stretched after hair removal. The original prototype's approximately 1.75 m total height includes the bun and hairpins. With its existing scale retained, the bald crown is approximately 1.675 m high. A future approved anatomical height specification should use the scalp/body, not hairstyle bounds.

### Why the final runtime mesh is a preserved Unity subset

The reproducible Blender step deletes only the four verified disconnected islands from a copy of the lightweight FBX. Retained vertex coordinates, polygon order, material indices, smooth flags, UVs, corner normals, weights and all shape-key coordinates compare exactly before/after deletion. The 51-bone skeleton fingerprint is unchanged. A generated `.blend` retains the hidden original comparison and exports only the bald candidate.

A direct FBX round trip was geometrically accurate (maximum shape-coordinate error about 0.275 micrometres), but Unity's resulting BlendShape normals differed substantially during deformation. The first parity test correctly failed; for example, `jawForward` reached roughly 141 degrees of normal disagreement. Both original and generated imports use calculated BlendShape normals. The precise reason for that recalculation difference was not established, so the raw re-export is **not the accepted runtime comparison**.

The final Editor build uses the Blender-exported surface to identify retained original Unity vertices by position and UVs, then subsets the original native mesh data. It preserves original positions, normals, tangents, UV channels, weights, bind poses, triangle winding and every BlendShape frame's position/normal/tangent deltas and frame weight. It rejects triangles crossing the removal partition and requires the expected retained triangle count. The retained index map is saved for review. This avoids changing facial shading while still deriving the removal from the documented Blender process.

### Effects on deformation

- **Scalp, UVs and skinning:** retained surface data and UV assignments are preserved; no new scalp surface or skin binding is authored. All hair weights were `Head = 1`, and no hair-exclusive runtime bone needed removal.
- **Facial bones:** in the original full facial rig, removing the separate hair in memory produces exactly zero evaluated positional change in all six retained mesh objects for Neutral, BlinkBoth and BrowRaiseBoth. The source rig is retained. These samples demonstrate removal parity, not exhaustive facial-rig acceptance.
- **Runtime facial bones:** the final prototype remains the existing 51-bone body/shape arrangement. It does not import the earlier rejected 95-bone hybrid or add facial bones.
- **BlendShapes:** all 29 original data sets and frame metadata are preserved on retained vertices. All 23 channels with positive original frame weights have finite, matching deformation at weight 100. The other six inherit the original defects below.
- **Humanoid:** both the generated FBX and final prefab have valid Humanoid Avatars. The final prefab keeps the original Avatar and bone references. Idle, Walk and Run were sampled at four phases each; retained deformed geometry matches the original.

Across the finite facial endpoints, neutral resets and 12 locomotion samples, maximum retained position disagreement is **0.000000626 m**; maximum measured normal-angle disagreement is **0.0198 degrees**. Native stored shape deltas compare exactly. These tiny evaluated differences are within the validation tolerances.

## Existing facial limitations exposed by this check

Six original Unity BlendShapes have a supplied/imported frame weight of **zero**. Setting them to weight 100 causes non-finite baked vertices in both the original and bald meshes. The bald asset deliberately preserves this data; it does not silently repair or certify it.

| Original channel name | Invalid retained vertices, original and bald |
| --- | ---: |
| `mouthFrownLeft` | 15,319 |
| `mouthDimpleLeft` | 15,322 |
| `mouthStretchLeft` | 14,948 |
| `mouthShrugtLower` | 15,267 |
| `mouthShrugtUpper` | 15,258 |
| `mouthLowerDownLeft` | 15,259 |

A full symmetric frown is consequently **not accepted**. The failing diagnostic capture is labelled as an invalid baseline channel and is excluded from the final accepted gallery. The export/import cause of these zero frame weights needs separate investigation before those channels are exposed to users.

The earlier [baked upper-face report](<C:/Repos/Unity/Silver Screen/Docs/FemaleBakedUpperFaceExperiment.md>) still governs blink acceptance: hair removal does not resolve source eyelid closure or gaze limitations. Recommend a semantic facial-shape mapping layer with per-family bindings, corrected semantic labels for inconsistent supplied names, and explicit valid/missing/rejected capabilities. A channel's presence or name alone must not imply runtime support. No mapping system was implemented here.

## Reproduction

Run from `C:\Repos\Unity\Silver Screen`. Purchased files are inputs only. Existing GUIDs are retained on subsequent Editor rebuilds.

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
$femaleSource = 'C:\Users\Sez\Documents\Assets\rigged-stylized-human-body-base-mesh-rigged-3d-models\stylized-female-body-base\Stylized_Female_Body_Base_Facial_Rig\Facial Rig\StylizedFemaleBodyBaseMesh3DModelFacialRig.blend'
& $blenderExe --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/inspect_female_hair.py -- --source $femaleSource --output TestResults/FemaleBaldBase/Source
& $blenderExe --background --factory-startup --disable-autoexec --python Docs/CharacterBaseAuditTools/export_female_bald.py -- --baseline Assets/SilverScreen/Art/Characters/HumanBasePrototype/Models/AdultFemale.fbx --inspection TestResults/FemaleBaldBase/Source/hair-source.json --output TestResults/FemaleBaldBase/Generated --fbx Assets/SilverScreen/Art/Characters/FemaleBaldPrototype/Models/AdultFemaleBald.fbx
```

Then use Unity menu **SilverScreen > Characters > Female Bald Base > Build isolated generated comparison**. Run the Edit Mode fixture `SilverScreen.Tests.EditMode.FemaleBaldPrototypeTests`; it enters Play Mode in an isolated scene for deformation/capture and restores the original scene. Its two tests check retained geometry/shape/Humanoid parity and capture the technical gallery. The generated `.blend`, FBX, native mesh and prefab are local licensed derivatives and are Git-ignored.

Implementation: [Blender inspection](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/inspect_female_hair.py>), [Blender removal/export](<C:/Repos/Unity/Silver Screen/Docs/CharacterBaseAuditTools/export_female_bald.py>), [Unity build](<C:/Repos/Unity/Silver Screen/Assets/Editor/Characters/FemaleBaldPrototypeTools.cs>), [Unity tests](<C:/Repos/Unity/Silver Screen/Assets/Tests/Editor/FemaleBaldPrototypeTests.cs>).

## Validation and evidence

Final targeted run: **2 passed, 0 failed, 0 skipped**, [final-validation.xml](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/final-validation.xml>). This is targeted prototype validation, not a full game test suite or standalone player build. Final Editor state: compilation successful, Edit Mode, Console 0 errors / 0 warnings, only `Assets/Scenes/Studio.unity` loaded and clean.

Evidence is local under `TestResults/FemaleBaldBase`:

- [Source geometry, weights, eyebrows and facial removal parity](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/Source/hair-source.json>).
- [Blender subset preservation and retained indices](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/Generated/bald-export.json>); [generated Blender comparison](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/Generated/AdultFemaleBald.blend>).
- [Unity retained deformation parity](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-retained-parity.txt>), [original Unity vertex index map](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-original-indices.txt>), [preserved shape frames](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/preserved-shape-frames.txt>), [import metrics](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/unity-import.txt>).
- Earlier failed direct-export evidence remains in `FirstExport`, `all-shape-diagnostics.xml` and `imported-frame-diagnostic.txt`; the final passing result does not erase those findings.
- [Preservation check](<C:/Repos/Unity/Silver Screen/TestResults/FemaleBaldBase/preservation-check.json>): all **85 purchased source files** retain their recorded SHA-256, size and modification time. All **46 pre-existing prototype/evidence files** in this task's preservation baseline retain their hashes. Git HEAD is unchanged; ProjectSettings bytes equal HEAD. Nothing was committed or pushed.

## Recommended canonical-base workflow

1. Keep the purchased files immutable. Generate an Adult Female bald derivative from the verified body and separate hair partition. Keep removal indices, source fingerprints and export checks with the processing recipe; rerun inspection if the source changes rather than reusing numeric indices blindly.
2. Use the current preserved-data prefab for technical review. Retain the original with-hair, facial-rig and rejected upper-face comparisons. Approve scalp proportions, anatomical height, skin/eye treatment and eyebrow appearance separately before production adoption.
3. Apply the same retained-vertex map to any later accepted facial deltas for this Female family. Revalidate normals, all supported expressions, bone deformations and Humanoid locomotion after exporter or source changes. Resolve the six defective facial channels and previous upper-face gates separately.
4. Keep **Male, Female, Boy and Girl as four separate canonical mesh families**. Their topology and UVs differ; this removal map and facial data are not transferable by index to the other families. Child hierarchy/export/Avatar validation and integration remain required follow-up work and were not expanded here.
5. Future hairstyles can be cleanly attached as modular assets because this head has a complete independent scalp and no shared hair/body topology. The supplied rigid style is already entirely Head-weighted, so a Head attachment or equivalent skinned hair asset is a viable starting point. Preserve hair UVs/materials separately; fit and validate each style per family, including hairline, ears, neck, motion and hats. Flexible hairstyles may need their own deformation setup. No hairstyle, colour or hat system was built.
