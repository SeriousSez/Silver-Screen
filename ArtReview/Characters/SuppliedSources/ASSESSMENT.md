# User-supplied Blender folder — source inspection

Inspected 22 September 2026. This is a source assessment, not a new SilverScreen head or a passed visual gate.

## Files inspected

- `C:/Users/Sez/Downloads/Blender/Blender 1/blender.Fbx`
- `C:/Users/Sez/Downloads/Blender/Blender 2/Blender.Fbx`
- `C:/Users/Sez/Downloads/Blender/Blender 3/blender.Fbx`

Each folder contains an FBX, Character Creator material metadata and textures. Metadata identifies the generation as `RL_CC3_Plus`. These are pre-existing character bases; using their geometry would be an adaptation of supplied topology, not a character constructed entirely from blank geometry.

## Observed contents

All three imports have the same body mesh counts: 14,164 vertices and 14,046 quad faces, including the head. Each body has one UV layer and 148 non-basis shape keys. Named shapes include independent blinks, eye directions, brow changes, nose and cheek changes and mouth/viseme shapes. Their correctness after reshaping has not been validated.

Eyes are separate from skin, although the two eyes are in one mesh object. Teeth and tongue are separate objects. Hair, clothing and shoes are separate meshes. The primary imported armature has 101 bones and there is an additional three-bone armature. Neither replaces the existing SilverScreen 91-bone contract.

Neutral head previews were inspected for all three exports. They appear to share the same underlying feminine head/body foundation; the differences are primarily outfit, hair and textures. The source head has formed eyelids, nostrils, lips and ears, and is a substantially more useful anatomical starting mesh than the rejected local prototype. It does not match the male SilverScreen concept as supplied.

## Suitability

The files are usable as supplied base topology for a head-only sculpting pass. The concept still requires substantial changes to skull silhouette, orbital/brow structure, nose, cheek planes, jaw/chin and neck. Mouth interior, eyelid closure and facial deformation must be checked after reshaping. Quad counts and existing shape names alone do not establish production readiness.

The source head currently shares the body mesh. A working head copy would need a deliberate neck seam and independent left/right eyes. Existing shape keys would need adaptation or rebuilding following the identity sculpt. Later rigging would follow the preserved SilverScreen contract.

The current user scope remains head-only: no body reconstruction, garment fitting, skinning or animation before visual review.

## Inspection limits and preservation

The original Downloads files were only read. No imported source has been incorporated into canonical authoring files. The 91-bone scaffold, accessory work, Studio scene and Stage 1 were not changed by this inspection.

Blender imports completed and rendered, but logged missing FBX layer-data warnings. Their scope remains unclassified; UV/material/deformation fidelity is not certified by this inspection.

Previews use neutral clay overrides with hair and clothes hidden. Eyelash cards were excluded in memory because an opaque clay override makes them misleading solid strips. Source files remain unchanged. These renders show the supplied head, not newly authored SilverScreen geometry.

Machine-readable inventory and neutral previews are in the `Blender1`, `Blender2` and `Blender3` subfolders. `inspect_fbx.py` reproduces the assessment previews.
