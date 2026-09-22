# Building and Environment Art Instructions

These rules apply to production building and environment art in this subtree. Source-authoring rules in `/ArtSource/AGENTS.md` also apply when changing Blender sources or generators.

## Major Asset Workflow

For a significant new building or environment asset:

1. Review the supplied initial art or reference.
2. Inspect the actual Unity project before designing.
3. Determine real world scale, footprint constraints, neighboring assets, gameplay requirements, camera distances, and technical constraints.
4. When no final production design is approved, generate a project-aware production concept that translates the supplied art direction into those constraints.
5. Stop for user approval before substantial production modeling or detailing.
6. Once approved, treat the production concept as the visual specification, not loose inspiration.
7. Create a correctly scaled blockout.
8. Validate massing and proportions in Unity through MCP when available.
9. If implementation materially diverges from the approved concept, stop for review instead of detailing the wrong design.
10. Perform production detailing only after design and massing approval.
11. Validate the finished asset at management, medium, and close distance.
12. For important assets, produce concept-versus-Unity comparisons before declaring visual completion.

Do not discover the final visual design while simultaneously production-modeling it. The supplied initial art establishes creative direction; the project-aware production concept translates it into project constraints; the approved production concept becomes the implementation target.

## Concept Fidelity and Quality

- Do not simplify an approved concept merely because a simpler implementation is easier to generate.
- Evaluate silhouette, proportions, architectural depth, construction, material hierarchy, doors and windows, hardware, roof treatment, entrances, utility logic, interior infrastructure, detail density, and overall visual hierarchy.
- A requested feature merely existing is not sufficient. If the production asset still reads as a simplified blockout of the approved concept, continue refinement.
- **INDUSTRIAL SIMPLICITY != LOW DETAIL.**

## Visual Quality Standard

- Approved production concept art is the primary visual-fidelity target for an asset.
- Real-world architecture, engineering and construction logic are the physical-plausibility target.
- Existing SilverScreen assets should be used for compatible world scale, spatial context and broad art-direction cohesion, but their simplifications or deficiencies must not be inherited automatically.
- Existing assets may be below the current production standard and may be remastered later.
- Each new production asset should meet or improve upon the current SilverScreen quality standard rather than being limited by an older asset.
- Target realistic, production-quality PC game art that reads clearly at management distance and remains convincing at medium distance and intentional close inspection.
- Architectural simplicity must not be confused with low geometric or material quality.
- When an approved concept exists, visual fidelity to that concept takes precedence over matching the detail level of an older in-game asset.

Administration is an earlier production asset and may still be referenced for:

- world scale
- relative building scale
- lot composition
- spatial relationship
- broad SilverScreen visual cohesion

Do not use Administration as the visual-quality benchmark or realism ceiling. Its current modeling and material realism must establish neither the minimum nor the maximum quality target for future assets.

## Construction Logic

Every visible architectural component must connect physically to something that explains its purpose, terminate intentionally with believable construction or hardware, or be removed. In particular:

- Railings connect to stairs or platforms; stairs begin and end on usable surfaces.
- Hinges connect doors and frames; handles attach physically; glazing fills framed openings.
- Trim wraps or terminates intentionally.
- Pipes and conduit have believable origins and destinations; utility runs connect to panels, junctions, penetrations, or equipment.
- Lamps have believable mounts; roof vents use believable curbs or penetrations.
- Structural members connect to structure; catwalks have supports; braces visibly reinforce something.

No floating decorative infrastructure.

## Architecture and Dressing

- Keep vehicles, crates, cameras, movable film lights, carts, chairs, equipment cases, temporary scenery, and other loose props separate from architectural assets.
- Permanent architectural and utility infrastructure may belong to the building.
- Do not bake instance-specific signs or numbers into reusable building geometry when identity belongs to the Unity building instance.
- Buildings should reflect their construction era. Do not automatically modernize historical architecture as game decades advance.
- Older buildings may remain recognizable while renovations update equipment, materials, and infrastructure.

## Materials and Weathering

- Use physically plausible PBR materials with restrained roughness and tonal variation, correct metallic response, sensible separation, and subtle non-directional microtexture.
- Avoid obvious procedural patterns, directional fingerprint or wave noise, perfectly flat materials, fake baked directional lighting, and brightness changes used to compensate for incorrect lighting or color-space handling.
- Use the established Blender-to-Unity color-space handling.
- Keep reusable source materials primarily clean. Prefer reusable Unity decals or overlays for localized runoff, grime, contact dirt, cracks, and deterioration rather than baking them into shared source materials.
- Do not over-focus on weathering during initial production. Condition polish may remain backlog work unless requested.

## Performance

- SilverScreen may display many buildings, characters, and props simultaneously. Management-camera readability is the primary distance, while intentional closer inspection must remain supported.
- Do not solve quality problems by indiscriminately increasing polygon counts.
- Add geometry where it improves silhouette, curvature, construction readability, or close-range presentation. Use materials, textures, and normals where geometry adds little visible benefit.
- Avoid expensive per-building techniques without meaningful visible benefit.

## Unity Validation Checklist

- Do not validate visual assets only in Blender. Inspect the imported asset in Unity under the game's real rendering setup using Unity MCP when available.
- Check the normal management camera, medium distance, close distance, direct light, shade, front, sides, rear, roof, important entrances, applicable interiors, and relationships to neighboring buildings.
- When an approved concept exists, compare the Unity implementation directly against it.
