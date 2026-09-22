# Art Source and Blender Instructions

These rules apply to Blender files, procedural generators, source textures, reports, and export preparation under `ArtSource`.

## Authority and Reproducibility

- Treat source generators and source `.blend` files as authoritative where the asset uses the repository's reproducible generation workflow.
- Fix problems at the authoritative source rather than manually patching generated Blender files, FBXs, copied Unity assets, or generated material data.
- Preserve deterministic regeneration and validate all affected exports after a source change.
- Follow the existing asset's generator and output targets; do not invent a parallel export process.
- Keep generated reports and generator assertions accurate. Existing procedural building exports use metric units, applied scale, semantic export groups, outward normals, FBX `-Z` forward and `Y` up, and no baked animation; preserve those conventions unless the existing asset explicitly establishes another requirement.
- Preserve required Unity material GUIDs, importer mappings, and output paths when regenerating assets.

## Scale, Geometry, and Asset Boundaries

- Preserve gameplay-compatible metre scale and intentional pivots or origins. Apply transforms where the established exporter requires it.
- Use enough geometry for smooth visible curves without indiscriminate subdivision.
- Use geometry where silhouette or construction requires it; use materials, textures, and normals where geometry provides little visible benefit.
- Ensure exported architectural components remain physically connected and intentionally terminated.
- Keep loose gameplay or world props separate from permanent building architecture.
- Do not bake dynamic instance-specific signage or numbers into reusable architectural FBXs when the value belongs to the Unity building instance.

## Materials and Color

- Keep Blender-to-Unity material and color handling correct. Blender generator colors documented as linear must be converted through the project's established linear-to-sRGB transfer before writing Unity sRGB color properties.
- Do not copy Blender linear RGB values directly into Unity sRGB properties, arbitrarily brighten materials to compensate for lighting, or bake directional/showcase lighting into reusable materials.
- Use physically plausible metallic and roughness values with subtle, non-directional material microtexture.
- Avoid obvious procedural wave or fingerprint patterns.
- Keep reusable source materials primarily clean; localized condition and environmental weathering should normally remain in the reusable Unity decal/overlay workflow.

## Export Validation

- Regenerate the editable Blender source, configured exports, and generation report together when the generator owns them.
- Check unit scale, transforms, pivots, normals, degenerate geometry, semantic separation, material assignments, and expected output copies.
- Validate the imported result in Unity after export; a successful Blender render or FBX export is not visual approval.
- Report any export, import, Unity, or visual checks that were not completed.
