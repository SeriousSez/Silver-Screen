# SilverScreen Environment Art Specification

## Direction

SilverScreen uses stylized semi-realistic Golden Age Hollywood environments: believable human proportions, warm period materials, strong silhouettes, moderate bevels, and detail that remains readable from an elevated management camera. Avoid photorealism, toy-like exaggeration, and unrelated marketplace styles. The project reference is `ArtSource/References/SilverScreen_1930s_ArtDirection.png`.

The 1930 starting era combines utilitarian studio construction, Art Deco accents, Spanish Revival, Craftsman and bungalow housing, period commercial façades, and brick industry. Not every building should share one architectural style.

## Scale and coordinates

- Metric units: 1 Blender unit = 1 Unity metre.
- Blender uses Z-up; export uses `-Z Forward`, `Y Up` for Unity.
- Human reference: approximately 1.8 m. Exterior doors normally 2.1–2.6 m tall.
- Building roots sit at ground level near the footprint centre.
- Props use a ground-contact origin; hinged assets should use the functional hinge when later animated.
- Apply transforms before export. Avoid hidden scale corrections in Unity prefabs.

## Naming and separation

- Assets: `PurposeVariant_01`, e.g. `Warehouse_01`.
- Root: `<Asset>_ROOT`.
- Meshes use descriptive architectural roles: `Shell`, `Roof`, `Door`, `Window`, `Trim`.
- Materials use reusable `MAT_<Family>` names.
- Keep shells, roofs, openings and major trim logically separate where this supports future interiors or variants; do not fragment tiny decorative pieces unnecessarily.

## Architecture and modularity

- Completed buildings must read through silhouette and composed massing before materials, windows, or signage are considered. A decorated rectangular shell is not an acceptable finished reference asset.
- Compose primary and secondary volumes deliberately. Use building-appropriate roof profiles, setbacks, entrance projections, porches, canopies, parapets, loading structures, stair/service volumes, and structural rhythm.
- Openings require visible wall-to-frame-to-glass/door depth. Cornices, roof overhangs, awnings, docks, and stairs must project enough to create readable shadows from the management camera.
- Prefer reusable openings, façade bays, trim bands, roof families, porches and service elements.
- Use practical snapping increments of 0.5 m, with major façade modules commonly 1, 2 or 4 m.
- Maintain believable floor heights, window rows, entrance positions, rear/service access and structural depth.
- Exterior shells must leave plausible internal volume. Openings should be capable of matching future rooms, corridors, stairs and filmable spaces.
- Important locations may be bespoke, but should still share the same scale, materials and opening conventions.

## Materials and UVs

Use a compact reusable family: warm stucco/plaster, brick, concrete, wood, painted wood, dark/painted metal, smoked glass, roofing, fabric, leather, asphalt and ground. This proof kit uses flat Blender materials; production assets should use consistent texel density and non-overlapping UVs where baked or unique textures are required. Prefer tiling surfaces and small trim sheets over unique large texture sets.

## Geometry, colliders and LOD readiness

- Bevel silhouette-critical edges modestly; avoid dense subdivisions and invisible detail.
- Treat polygon counts as an outcome of visible form, not a target to minimize. Reference or hero architecture may use additional geometry when it materially improves roofline, massing, façade depth, or silhouette.
- Keep repeated props instancing-friendly and material counts low.
- Export render meshes without authored gameplay colliders unless a separate `<Asset>_COL` mesh is deliberately provided.
- Preserve clean component boundaries so later LODs can simplify trim/openings before the primary mass.

## Furniture and prop reuse

Furniture is built as reusable base families. Variation should come from material, condition and dressing rather than unique geometry for every placement. Keep chairs, desks, lamps, telephones and equipment at believable human scale.

## Workflow

1. Extend or add a generator under `ArtSource/`.
2. Generate and retain the `.blend` source plus `generation_report.json`.
3. Export individual FBXs to `ArtExports/` using metric units, `-Z Forward`, `Y Up`, no animation, no embedded textures.
4. Copy only approved runtime FBXs/material assets into `Assets/SilverScreen/Environment/`.
5. Create Unity materials/prefabs separately; keep Blender and reports outside `Assets`.
6. Validate scale beside an EmployeeAgent and the approved HQ/Casting/Stage assets before replacing blockouts.

Blender is an authoring dependency only. The shipped game must never invoke Blender or procedural authoring scripts.
