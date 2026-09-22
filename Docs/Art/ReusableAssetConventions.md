# SilverScreen reusable fixtures

Milestone 1 establishes a small authoring and catalog boundary for fixed fixtures. It is not a universal construction, placement or simulation framework. Stage 1 revision 04, commit `927ad371a96c4044689286473b542febac6a7265`, supplies the approved source geometry. Canonical assets are independent; Stage 1 does not reference them.

## Ownership and folders

| Location | Responsibility |
| --- | --- |
| `ArtSource/ReusableFixtures/` | Extraction script, representative-art script and independent editable Blender source |
| `ArtExports/ReusableFixtures/` | Deterministic geometry packet and source/component manifest |
| `Assets/SilverScreen/Environment/ReusableFixtures/Meshes/` | Unity native meshes, split by useful authoring part |
| `…/Prefabs/` | Complete fixture or intentionally paired authoring subassembly |
| `…/Definitions/` | Runtime-safe catalog and placement facts |
| `…/Materials/`, `…/Textures/` | New display surface and two original representative graphics |
| `…/Editor/Provenance/` | Development provenance linked to each prefab; not a runtime dependency |
| `…/Validation/` | Independent inspection scene |
| `Assets/Scripts/Domain/ReusableAssets/` | Unity-independent catalog availability and aspect-preserving layout mathematics |
| `Assets/Scripts/Presentation/ReusableAssets/` | Unity definitions, anchors and optional display/light behavior |
| `ArtReview/ReusableFixtures/Milestone1/` | Native URP captures, matched source comparisons, validation evidence and report |

Names use `Family_Variant_1930` where a variant is meaningful. IDs use `silverscreen.fixture.<lowercase prefab name>` and must remain stable after catalog/save integration. Do not infer identity from localized display names. Preserve new GUIDs when rebuilding: update existing meshes/definitions/prefabs at their established paths.

## Coordinates, pivots and anchors

- Metres, Unity Y-up, root position zero, root rotation identity, root scale one.
- Wall assets face outward along local +Z, with a pivot on the host mounting plane. Looking toward the asset from +Z, screen-right is local -X. This preserves the approved handedness through Blender-to-Unity conversion.
- Ground assets use base centre at Y=0. Pendants use the upper face of the clamp as their overhead datum and extend down -Y.
- Pulls use their leaf mounting plane. The paired lockset uses its front leaf mounting datum; strike, edge latch and thumbturn retain their approved relationship as separate geometry children. This initial lockset is for the source's approximately 0.22 m overall leaf assembly, not an automatically resizable universal lock.
- Hinges use the barrel-axis centre as their pivot. `Anchors/Mount` is typed `Pivot` for hinges; explicit `LeafMount`, `JambMount` and `HingeAxis` anchors supply the mechanical interfaces. Variants retain the source's 140/180/190 mm crank offsets. They are not interchangeable without checking the host leaf/jamb.
- Anchors are children under `Anchors`, with `FixtureAnchorKind`. Anchor-local +Z is its mating/axis direction. Mount anchors point into the host; service anchors point toward a future connecting segment. A pivot's +Z is the rotation axis. Diameter is a nominal interface dimension, not a modeled bore guarantee.
- Current kinds: Mount, Service, Pivot, Socket, Latch, Insert, Light. No snap solver is implemented. Future systems should consume explicit compatible anchors, not guess from mesh origins or names.
- `VisibleBounds` describes rendered source geometry. `FunctionalClearance` separately describes cabinet/control access space; an empty clearance means unspecified. Clearance is guidance, never a solid collider or engineering certification.

## Source fidelity and reproduction

`extract_milestone1.py` opens the approved `.blend` read-only, selects named component memberships within their semantic groups, and writes independent output. Never run the Stage 1 generator to adopt these prefabs.

The independent `.blend` retains editable component meshes, one collection per canonical asset. Collections share canonical origin; only the first collection is initially visible so artists can isolate one asset at a time. Blender uses Z-up. The packet explicitly contains Unity Y-up coordinates, split normals, original UVs, material slots and triangle indices. One handedness conversion reverses triangle winding. No UV projection, geometry simplification or LOD generation occurs.

This new fixture pipeline writes native Unity meshes from the packet. It deliberately does not inherit the building FBX's origin/axis correction or use an entire `Export_*` group as an asset. The approved Stage 1 FBX remains the independent Unity appearance reference.

Reproduce from the repository root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --python 'ArtSource/ReusableFixtures/extract_milestone1.py'
python 'ArtSource/ReusableFixtures/make_review_artwork.py'
```

The artwork script requires Pillow and Windows Arial. It creates only the two representative graphics. In Unity Edit Mode use `SilverScreen > Art > Reusable Fixtures`:

1. Build Milestone 1 assets.
2. Build and capture isolated validation scene.
3. Run fixture EditMode tests.
4. Compare against approved Unity source.

These are explicit operations; importing the project does not automatically regenerate sources. The builder checks the source hash recorded in the manifest and saves only owned output assets. It never calls a project-wide `SaveAssets` or saves Studio. The review scene must be closed before rebuilding it. Regenerating milestone-owned files overwrites their contents; change their generator first when intentional authoring changes are required.

## Catalog and material policy

`ReusableAssetDefinition` holds the prefab reference, stable ID, label, category/subcategory, player-catalog flag, availability, suitability, surface, dimensions/bounds, handedness, role, material roles, material-variant capability, broad environment tags, snap description and optional interaction description. `CatalogEntry` projects identity and availability into a Unity-independent model. No changes to the existing set/building construction systems are required.

`AvailableFromYear = 1930` means availability from the game's start, not an invention date. `AvailableUntilYear` is unset. Old fixtures remain available for later-period sets. Tags describe studio/industrial/utility use; they are not genre restrictions. Hardware/control units are authoring assets; no screw, rivet, bearing or reflector is a player catalog entry.

Exact meshes continue to reference the approved `C1_*` material assets and their original textures. Do not mutate these shared materials, consolidate them, regenerate their GUIDs, or rebake their source UVs. Material variants are supported as an authoring capability; this milestone does not create a variant library or registry. Assign alternate material assets on the intended canonical instance/variant, never edit a shared Stage 1 material to change one fixture.

Development provenance is editor-only. Each record includes master commit, source path and SHA-256, semantic group, component names, position/UV hashes, family/variant ID, coordinate transform, dimensions, original roles and extraction status. The manifest is the reproducibility boundary; runtime code does not read source paths or provenance.

## Display frame contract

`DisplayFrame` identifies backing, outer frame, optional divider and insert slots. Each `DisplayInsertSlot` has an aperture in metres, insertion depth, acoustic insert, optional graphic backing, matte plane, artwork plane and optional glazing reference. No new glazing is manufactured in M1.

- Double frame: source backing/frame/divider and two original fabric inserts, with two 1.081 × 1.725 m clear graphic apertures. Socket face is 0.1435 m from the mount; frame-front recess is approximately 24 mm.
- Single frame: source outer frame and original fabric insert, with a 2.615 × 2.050 m graphic aperture. Socket face is 0.131 m from the mount; recess is approximately 54 mm. Graphic mode uses a removable backing board because the source single panel has no separate rigid backing. Acoustic mode hides this addition and shows the original insert.
- These are initial industrial dimensions, not limits of the interface. Later smaller frames can supply other apertures without duplicating the content system. Removing the double divider does not automatically create one large aperture; author a deliberate compatible socket layout before doing so.
- Supported roles: acoustic fabric, poster, studio notice, photograph, advertising/signage, player movie poster. Roles share the same insert interface; only two original graphics ship as examples.
- `slot.SetInsert(role, texture, ArtworkFit.FitWithMat)` preserves the whole image with a surrounding matte. `ArtworkFit.Crop` fills the aperture with a centred, aspect-preserving crop. `SetMatColor` is per instance. A null graphic texture leaves a blank matte; choosing AcousticFabric restores the original fabric.
- Texture assignment uses `MaterialPropertyBlock`; frame meshes and shared materials stay shared. No `Renderer.material` instantiation or shared-material mutation is used. Assign imported artwork with `NPOT Scale = None` so Unity does not change its aspect ratio. The supplied graphics preserve their original dimensions.
- The Inspector refreshes the selected slot after authoring changes. Runtime texture ownership remains with the caller. Upload/storage/moderation, persistence of external player textures and a catalog UI are later systems.

## Studio-agnostic artwork templates

Canonical artwork backgrounds contain no player studio name or current game year. `DisplayArtworkTemplate` specifies a background, static font atlas, text shader and named text rectangles. `ArtworkContext` supplies `studioName`, `currentYear`, `productionTitle` and arbitrary template-specific keys. Missing fields are blank; they never fall back to `StudioIdentity.DefaultName` or 1930. Only content intentionally representing a historical artifact should use a literal date with an empty field key. Fixture catalog availability is a separate concept and remains 1930.

`DisplayArtworkPresenter` is optional instance behavior attached to a display socket, separate from the canonical frame prefab. It composes a private render texture when content changes and supplies it through the slot's existing property block. The shared frame, geometry, base material, template and font atlas are not edited. Disable releases the texture and unsubscribes; re-enable reads current context. Rebinding detaches the old context. Serialized preview fields are explicit authoring snapshots for the independent review scene, not live player state or implicit defaults.

The gameplay composition owner supplies its existing identity/time services and owns the binding lifetime:

```csharp
var fields = new ArtworkContext();
var binding = new StudioArtworkBinding(fields, studioIdentity, timeDriver.TimeService);
fields.Set(ArtworkContext.ProductionTitle, production.Title);
presenter.Initialize(fields);
// On a title edit, supply the revised production.Title again.
// On host teardown, dispose binding; the presenter unsubscribes on disable.
```

`StudioArtworkBinding` listens to `StudioIdentity.OnNameChanged`, `ISimulationTimeService.OnYearPassed`, and the existing clock restore notification (`OnSpeedChanged`). It also exposes `Refresh` for host restore/rebind workflows. There is no global player lookup in a template, frame or content context. Rival content uses the same presenter/template with a separate context populated from its owning company/movie data. A studio name change must not rewrite rival content. `MovieProject.Title` currently has no dedicated change event; callers explicitly update `productionTitle` when editing that value. This is a display-content seam, not a Movie Poster service, catalog, upload system or persistence layer.

The composer uses TMP layout with a static atlas and literal text (rich text disabled), then draws to an instance texture. It performs no per-frame work and creates no rendering camera. Text shrinks to fit its rectangle. Unsupported glyphs use `?` rather than populating shared dynamic fallback atlases; localized templates should supply an appropriate static font. Templates retain portrait/landscape aspect through the existing fit/crop path. Runtime textures are transient and recreated from template/context, not serialized as content assets.

M1 examples bind an actual `StudioIdentity` and `SimulationClock`: Aurora Pictures / 1937 changes to Crescent Film Company / 1938 across a year rollover, with a changed production title. The open-house poster displays the year; the quiet notice intentionally displays only the studio name and production title. A Majestic Pictures example retains its own context. `50_template_before.png` / `51_template_after.png` show the same frame instances, and `52_rival_unchanged.png` shows isolation. Use the isolated validation-scene builder to reproduce; no Stage 1 regeneration or canonical-frame rebuild is needed for template changes.

## Collision and optional behavior

Use simple enclosure/frame boxes, a capsule plus foot box for the bollard, and separate mount/shade boxes for lamps. Small hardware/control subassemblies have no colliders; a complete future doorway/control assembly owns collision. Do not turn service clearances into obstacles. No mesh colliders or LOD groups are introduced.

`FixtureLightSwitch` is an optional child with a normal Unity Light. Geometry does not depend on it; it can be removed when a host supplies lighting. Canonical prefabs default to lights off; the validation scene enables them through the API for a separate view. Controls remain representational. No electrical or acoustic simulation is provided.

## Validation expectations

Check independent placement, scale, mounts, original materials/UVs and simple collision in Unity using the real project renderer. Compare source and canonical under matched camera/light conditions. Verify artwork fit/crop, per-instance isolation and scene reload. Run the targeted tests after relevant changes. Record limits rather than treating a Blender export or successful C# compile as visual approval.

`ReusableSourceComparison` reconstructs temporary comparison meshes directly from the approved imported Unity semantic meshes, using position/UV membership. It compares all canonical vertices, UVs, normals and triangle counts, then captures each source/canonical pair without modifying the master. Normal tolerance accounts for FBX normal precision; numeric evidence is written to `source-comparison.json`.

Before handoff, compare the hashes of all pre-existing working-tree files, not just Stage 1. Preserve unrelated dirty/untracked work. Any future migration of Stage 1, rainwater/conduit/circulation extraction, broad material work, optimization or LOD generation needs its own authorized milestone.
