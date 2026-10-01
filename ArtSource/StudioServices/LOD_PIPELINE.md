# Studio Services render LOD pipeline

The approved production source, LOD0 mesh packets, canonical prefabs and shared
materials remain authoritative and unchanged. `build_lods.py` opens the approved
Blender masters read-only and writes separate derivative collections and mesh
packets under `ArtExports/PeriodEnvironment1930/LOD` and
`ArtExports/StudioServices/LOD`. Do not regenerate LOD0 to run this pass.

## Reproduction

1. Run Blender background with `--python-exit-code 1 --python
   ArtSource/PeriodEnvironment1930/build_lods.py`.
2. Run `python ArtSource/StudioServices/validate_lods.py`.
3. In Unity, use **SilverScreen > Art > Studio Services > 7 Build render-only
   LOD derivatives**. The regular production builder also reapplies the LODs
   when derivative packets exist. Stale source/generator hashes fail explicitly.
4. Use **8 Capture and validate LODs** and `StudioServicesLodReview.StartSweep()`
   for matched rendering and automatic camera-selection evidence.
5. Run `StudioServicesLodTests` and the existing focused Studio Services suites.

## Ownership and reuse

- All 63 current canonical assemblies have independent LOD prefab variants in
  `PeriodEnvironment1930/LOD/Prefabs`. They inherit their original meshes,
  materials, definitions, anchors and colliders. No new catalog identities.
- Reusable variants share the same lower meshes. Use those variants for standalone
  prop placements that need automatic distance selection. Existing catalog/base
  prefab references remain compatible; they are not silently redirected.
- Studio Services combines derivative geometry by existing visibility ownership
  (roof, four wall directions, retained interior/yard), then by shared material.
  Each combined renderer has one shared material. Multi-material zone meshes
  produced colour artifacts in Unity comparison renders; material-homogeneous
  batches retain correct appearance without adding material copies.
  Transparent surfaces have separate renderers. These combined meshes belong to
  the building, not to each prop instance. LOD0 retains the original instancing.
- Never combine across a cutaway boundary or move physical collision into a LOD.
  The generated child contains rendering only. It can be regenerated independently.

## Detail policy

LOD1 removes tiny fasteners and subpixel components, dissolves coplanar geometry,
and reduces each remaining construction component independently. Important
panels, frames, openings, glazing, roof, lettering and silhouettes receive higher
retention than mechanical detail. LOD2 additionally removes small surface and
hardware details and reduces radial/bevel density further. Tiny interior/yard
accessories may disappear. All enterable floors, partitions and wall linings
remain because the cutaway can expose them at any distance.

The process does not merge door openings, decimate an entire building blindly,
or remove surfaces merely because they are normally hidden by the roof. Omission
records in the export manifests make every removed component reviewable.

## Transitions and rendering

`StudioServicesLodBuilder.ConfigureGroup` owns the thresholds. The building uses
its architectural height as the LOD reference size, so the attached yard width
does not keep small details alive unnecessarily. Every MeshRenderer still has
its actual geometry bounds for rendering and shadows. Current PC quality has a
LOD bias of 2; the Studio camera uses 60 degrees vertical FOV. Thresholds are
screen-relative, not immutable metre distances.

URP Lit already supports cross-fade in this project. Distance-based dither fades
are used so zoom reversal is immediate and does not leave timed fades behind.
LOD0 and LOD1/2 retain their physical shadow silhouettes; purely transparent
lower-detail renderers do not cast opaque shadows. No material copies, render
pipeline changes, global quality changes or per-frame custom LOD scripts.

No fourth representation is added: LOD2 remains until its screen contribution is
very small. Native frustum/occlusion handling remains available. The derivative
meshes increase resident asset memory; they reduce geometry and submission cost,
not the number of gameplay objects or colliders. Editor screenshots and triangle
counts are not a shipping-hardware GPU benchmark.

## Rollback

Remove the generated render child and root LODGroup, and remove their renderer
references from the cutaway groups, or rebuild the regular production prefab
with derivative application disabled. Keep original canonical assets and all
gameplay state. Do not restore unrelated scene files or reset the working tree.
