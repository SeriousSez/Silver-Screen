# Boy/Girl source inventory update

2026-10-02. The user supplied the previously omitted Boy and Girl shape-key folders after the initial audit. This addendum corrects source availability and records read-only metadata. Child runtime integration and hierarchy/export/Avatar validation remain deferred.

## Inventory correction

The source root now contains **85 files: 26 additions, no removals, and no content or modification-time changes to the original 59 files**. Each child adds 13 files in `Stylized_{Boy|Girl}_Body_Base/Stylized_{Boy|Girl}_Body_Base_Shape_Keys/Shape_Keys/`: one .blend, two FBX, two GLB, three DAE, one each ABC/OBJ/PLY/STL, and an empty TXT notice. The earlier statement that no child shape-key FBX/GLB was supplied is superseded.

Current totals: 17 FBX, 8 Blender, 8 GLB, 16 DAE, 8 ABC, 8 OBJ, 8 PLY, 8 STL, 4 TXT. Twelve duplicate-hash groups now contain 26 files; only the two added empty notices join an existing duplicate group. The new .blends are not byte-identical copies of the original child .blends.

## Metadata inspected

Blender 5.0.1 opened the two added .blends and imported the four added FBXs with the existing `inspect_sources.py`, factory startup, `--disable-autoexec`, and `open_mainfile(use_scripts=False)`. No source was saved or re-exported. All six metadata reads completed. Blender emitted invalid-driver warnings while opening the .blends with scripts disabled; this was a metadata inspection, not a working-rig evaluation. GLB and other added formats were inventoried and hashed only.

| Family | Shape mesh | Vertices / triangles | Shape-key FBX bones: deform / all | Deform FBX root | Height in new .blend / deform FBX |
| --- | --- | ---: | ---: | --- | ---: |
| Boy | `BoyBaseMesh_ShapeKeys` | 17,468 / 34,108 | 50 / 144 | `Hips` | 1.495085 / 1.495084 m |
| Girl | `GirlBaseMesh_Shape_Keys` | 17,876 / 35,162 | 51 / 145 | `ROOT` | 1.493982 / 1.493981 m |

Heights are world bounding-box Z spans in Blender with metric units and unit scale 1, including hair; they are not measured Unity character heights. Exported FBX bone counts are total imported bones, not proof of weighted deform membership.

Each inspected child shape mesh has **Basis + 29 expression keys**, including the supplied spellings `mothShrugtLower` and `mothShrugtUpper`. Names/order match that family's original shape mesh. The newly supplied .blend shape-key metadata also matches the original .blend's names, ranges and defaults. No blink, brow, eye-look, identity/body morph or named viseme keys were added. The recommended semantic facial mapping layer remains appropriate: resolve stable concepts through family-specific supplied names and explicitly report unsupported controls.

Within each family, polygon-index topology hashes and UV hashes match the original child shape mesh across the new .blend and both FBXs. Boy retains one `UVMap` with two material slots; Girl one `UVMap` with three slots. These metadata matches do not establish rest/bind/deformation parity. Boy and Girl remain distinct from each other and from Male/Female: **four separate canonical mesh families, with no topology unification or cross-family morphing**.

The new Boy shape-key files measure approximately **1.4951 m**, whereas the shape-key object contained in the original Boy facial-rig .blend measures **1.8815 m**. Thus the previous scale concern applies to that particular original authoring variant, not every Boy shape-key asset. The new height agrees with the previously recorded Boy facial variant. Selecting the authoritative source and checking evaluated scale/bind behavior remain follow-up work.

## Validation boundary and follow-up

All **85 current source hashes and modification times were rechecked and preserved**. Nothing from these added folders was copied into Unity, imported as a child runtime asset, rendered, or integrated into gameplay. No new art approval is implied; the existing plain-grey adult render remains technical validation only. The adult female facial investigation and its rejected hybrid conclusion are unchanged.

Required child follow-up remains: select the authoritative variant/scale per family; validate hierarchy, roots, export fidelity and evaluated deformation; then verify Unity Humanoid mapping, animation and skinning before considering integration. The earlier detached facial-root observations concern the child **facial-rig** exports and must not be generalized to these newly supplied body-plus-shape-key exports.

## Evidence and reproduction

The initial inventory remains intact in [CharacterBaseSourceEvidence.md](CharacterBaseSourceEvidence.md). Local, Git-ignored evidence is in `TestResults/CharacterBaseAudit/ChildSourceUpdate/`: `inventory.json`, `inventory-delta.json`, `preservation.json`, and per-family `inventory.json`, `source-audit.json`, `preservation.json`. The table below records the additions so the complete inventory can be read with the original appendix.

Reproduce each family's metadata with the existing audit script, substituting Boy or Girl and the licensed source root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --disable-autoexec --python 'Docs/CharacterBaseAuditTools/inspect_sources.py' -- --source '<source-root>\Stylized_Boy_Body_Base\Stylized_Boy_Body_Base_Shape_Keys\Shape_Keys' --output 'TestResults/CharacterBaseAudit/ChildSourceUpdate/Boy'
```

## Added files

| Relative source path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/only .fbx .blend and .glb support shape keys .txt` | 0 | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(all_bones).fbx` | 4,101,436 | `3f5dc9e605f6d88305b6e38ce622660685894bd2f054146eb1b4d44418b3a062` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(all_bones).glb` | 14,093,848 | `0b7268f6233d5c4ca82cc72066815140f3ff380bdb7f30e884a7482896942f04` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | 3,925,324 | `eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).glb` | 14,064,224 | `e837a91835f8b476218399d350b2dd4894e1782f8c88e658892ffb78366edbf6` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(with_rig_all_bones).dae` | 10,248,138 | `def8881d3cd75aa24719b3f8018ae70afb534c61e11f9d8d2c43939166f9a7e4` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(with_rig_deform_bones_only).dae` | 10,173,451 | `6fbcc95c148004237aee01bcd4c5726cc3f2392e7010829b265e37ec4b21ad6c` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys(without_rig).dae` | 9,573,978 | `6b15c069d575cab0817865655ff2e511d05ca8c21dffc29db166a46d38246915` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.abc` | 1,199,428 | `7f722e250f1a79e6792502f49e41dd169dd6d4f315316dd5d5b9e3ca84837e27` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.blend` | 37,280,053 | `81785f92ee503944c3a3bc05fd239056cf7d602513aab648f693e1f97d406761` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.obj` | 2,515,577 | `c3b3baeaf7d6b72570e0eb840413c43725c0f255a97cb83cc193d34220b8be3b` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.ply` | 12,586,678 | `b3d73c4ddbf332b21a221134aa9d99286c3c96199c33635a785e510c8a810a50` |
| `Stylized_Boy_Body_Base/Stylized_Boy_Body_Base_Shape_Keys/Shape_Keys/StylizedBoyBodyBaseMesh3DModelFacialShapeKeys.stl` | 1,705,484 | `36a961ac6fc312b2ef9fae4a9d9716bd0e3f405d061e9aaea29f70f6186f2d4c` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/only .fbx .blend and .glb support shape keys .txt` | 0 | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(all_bones).fbx` | 4,194,668 | `d5b01e2a960129f2309810c521e4ba7384a64832397e07793c43664370515e30` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(all_bones).glb` | 11,891,720 | `a8b2ad2178c3eb414d6fd01218921df3d508dfb6d20652e4d71d1c60675d20ab` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx` | 4,017,884 | `37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).glb` | 11,861,748 | `4b374f26527087e3106e3eccd436666a5777564b625bc02a499a6fd64590590f` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(with_rig_all_bones).dae` | 10,579,566 | `77939a16333ab18e3dd26e7b0d7e4bb1dcdd93e7a3ca2873bb35fa2044f42130` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(with_rig_deform_bones_only).dae` | 10,501,363 | `d21d7caf6b7600f9957ccd312cfdb4b0e9cb066b001c1d03887062aca7dfeba5` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys(without_rig).dae` | 9,861,203 | `9354d3cd2bb8f186fe36a4720e9cd5959f9b119a88c1e814dfe0e1de4e754811` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.abc` | 1,232,716 | `1f1a080ba74d0924f44986be05444eb18b3926479db6f5bc305b37b035a585c7` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.blend` | 39,264,048 | `e087e18c7b52de0dd2aed9c50885779877cd955e84a248dd0bfe76222ce5a53c` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.obj` | 2,587,910 | `144a340cf39e8e54c43cf8ce801dbb00c6a3b9676b5fa097d74f77db2dd77ddd` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.ply` | 12,850,906 | `f596a3c684be1becf1c1ca08a95523b015a1aec5aed1c61df5f63fe51a70a38a` |
| `Stylized_Girl_Body_Base/Stylized_Girl_Body_Base_Shape_Keys/Shape_Keys/StylizedGirlBodyBaseMesh3DModelFacialShapeKeys.stl` | 1,758,184 | `2730f417570d88745edfd64c369ff05813a80e66dd8f7e640b5e2a5c63bdb6f0` |
