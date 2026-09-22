# Reusable asset extraction — Milestone 1

Implemented 19 independent canonical prefabs: 12 complete player-facing fixtures and 7 architectural authoring subassemblies. Stage 1 revision 04 remains the approved visual/provenance master; no migration was performed. No commit or push was made.

## Architecture and assets

The milestone adds a small Unity-independent catalog/availability model and aspect-layout calculation, Unity asset definitions, typed anchor components, editor-only provenance, optional light behavior and independent display insert behavior. It does not change the existing building/Set Builder construction code.

| Asset | Intended reuse | Visible W × H × D (m) | Placement |
| --- | --- | --- | --- |
| MeterCabinet_1930 | Both / player-facing | 0.730 × 1.095 × 0.442 | Wall |
| DistributionPanel_1930 | Both / player-facing | 0.750 × 1.100 × 0.589 | Wall |
| IntakeCabinet_1930 | Both / player-facing | 1.150 × 1.640 × 0.631 | Wall |
| Disconnect_1930 | Both / player-facing | 0.400 × 0.640 × 0.342 | Wall |
| JunctionBox_Shallow_1930 | Both / player-facing | 0.160 × 0.200 × 0.128 | Wall |
| JunctionBox_Deep_1930 | Both / player-facing | 0.140 × 0.180 × 0.173 | Wall |
| PullBox_1930 | Both / player-facing | 0.310 × 0.420 × 0.305 | Wall |
| LeverControl_Raised_1930 | Architectural authoring | 0.128 × 0.239 × 0.202 | Wall |
| LeverControl_Lowered_1930 | Architectural authoring | 0.128 × 0.221 × 0.221 | Wall |
| GooseneckLamp_1930 | Both / player-facing | 0.390 × 0.559 × 0.655 | Wall |
| PendantWorkLamp_1930 | Both / player-facing | 0.508 × 0.689 × 0.508 | Ceiling |
| Bollard_1930 | Both / player-facing | 0.260 × 1.310 × 0.260 | Ground |
| DoorPull_1930 | Architectural authoring | 0.100 × 0.460 × 0.169 | DoorLeaf |
| PersonnelLockset_1930 | Architectural authoring | 0.256 × 0.257 × 0.312 | DoorLeaf |
| PersonnelHinge_180_1930 | Architectural authoring | 0.303 × 0.150 × 0.097 | DoorLeaf |
| PersonnelHinge_190_1930 | Architectural authoring | 0.303 × 0.150 × 0.106 | DoorLeaf |
| PersonnelHinge_140_1930 | Architectural authoring | 0.303 × 0.150 × 0.094 | DoorLeaf |
| DisplayFrame_Double_1930 | Both / player-facing | 2.294 × 1.824 × 0.168 | Wall |
| DisplayFrame_Single_1930 | Both / player-facing | 2.710 × 2.155 × 0.185 | Wall |

All 19 assemblies use exact approved source surfaces. There are 162 distinct source objects, 188 component memberships including shared source controls, and 27,160 source triangles across the canonical variants. There is no geometry reduction, new LOD, re-UV or fixture redesign.

Canonical repackaging consists of local pivots/axes, meaningful part boundaries, anchors, shared material references, definitions, optional behavior and simple colliders. The pendant keeps the approved short stem/clamp with a separate head/socket/bulb group and an overhead datum. The paired lockset retains face hardware, edge latch, strike and thumbturn as separate children. Three crank-offset hinge variants and two complete lever states are authoring pieces, not tiny player catalog entries.

New interface geometry is confined to the display system: a shared artwork/matte quad and a removable single-frame graphic backing board. These do not change either original acoustic variant. The original source geometry and acoustic mode are preserved.

## Files and conventions

- Source/reproduction: `ArtSource/ReusableFixtures/` and `ArtExports/ReusableFixtures/`.
- Unity meshes, prefabs, definitions, display material/textures, editor provenance and isolated scene: `Assets/SilverScreen/Environment/ReusableFixtures/`.
- Model/runtime boundary: `Assets/Scripts/Domain/ReusableAssets/` and `Assets/Scripts/Presentation/ReusableAssets/`.
- Tests: `Assets/Tests/Editor/ReusableFixtureTests.cs`.
- Durable conventions: `Docs/Art/ReusableAssetConventions.md`.
- Evidence and complete file list: this review folder; see `FILES.md` and `protection-check.json`.

Metres, Y-up and unit root scale are consistent across the library. Wall assets use the host mounting plane and face +Z. Bollards use base centre at ground. Pendants use the upper clamp face and point down. Hardware uses leaf/frame planes or a hinge axis. Typed anchors distinguish mount, service, socket, pivot, latch, insert and light interfaces; visible bounds and service clearances are separate. No automatic snap/placement solver is introduced.

Definitions contain stable ID, display/category/subcategory, player-facing flag, availability, suitability, surface, dimensions, handedness, material roles and variant capability, architectural/decorative role, optional interaction and broad tags. Availability starts in 1930 and has no end date; this is game availability rather than historical invention. Genre is not restricted. Provenance records retain the approved commit, source path/hash, semantic groups, component membership, coordinate transform, dimensions and original material roles without runtime provenance dependencies.

## Frame and artwork behavior

Both source frame styles share the same slot interface. Mount/backing, outer frame, optional divider, clear aperture, recess depth, acoustic insert, artwork, matte and optional glazing are distinct. The two initial frame sizes demonstrate the contract without turning their large acoustic dimensions into a universal poster size.

Acoustic, poster, studio notice, photograph, advertising/signage and player movie poster roles are supported. Two studio-agnostic template backgrounds prove fit/matting and centred crop. Studio name, current year and production title are composed from per-display contexts, with arbitrary future field keys supported. Per-instance textures and colors use `MaterialPropertyBlock`, keeping frame meshes/materials shared. The same instance is captured before and after a texture swap. Scene reload regenerates artwork from its explicit preview snapshot. No player upload/storage UI or content library is implemented.

### Studio identity / year correction

The canonical backgrounds no longer contain baked Silver Screen Studios branding or 1930. A `StudioArtworkBinding` uses the existing `StudioIdentity` rename event and `ISimulationTimeService` year/restore notifications. Rendering is studio-agnostic: the template takes an `ArtworkContext`, not a player singleton. Production title and future template keys are supplied by the content owner. The open-house poster uses `currentYear`; the quiet notice intentionally has no year and displays only the studio name and production title. Fixed historical dates can use literal fields. Fixture availability remains a separate catalog value.

The same double-frame instances are shown as **Aurora Pictures / 1937 / THE SILVER LINING** in `50_template_before.png`, then **Crescent Film Company / 1938 / MIDNIGHT ON THE COAST** in `51_template_after.png`. The year change comes from an actual `SimulationClock` rollover. `52_rival_unchanged.png` retains **Majestic Pictures / 1937** on a separate context. Images 53–56 are the corresponding composed texture outputs. This uses optional presenter components on review instances; the canonical frame prefabs, meshes and shared base material remain byte-identical.

Tests compare actual rendered pixels after independent studio/year/title edits, verify rival isolation, lifecycle/rebinding, missing-field behavior, arbitrary template keys and fixed historical dates. The composer updates only on changes and uses a static font atlas with rich text disabled. Unsupported font glyphs currently render as `?`; localized templates require a suitable static font. The isolated review scene contains explicit sample snapshots; gameplay hosts supply their real state through the documented binding API. No live Stage 1 or Studio-scene integration is introduced.

## Validation and source comparison

- Actual Unity 6000.5.9f1 / project URP renders inspected at overview and close fixture views, including mounted lamps, cabinets, bollards, hardware and both frame styles.
- Nineteen matched canonical/source image pairs come from independent geometry reads: canonical meshes from the new packet; reference meshes from the approved imported Unity FBX semantic groups. The original prefab/assets are read only.
- All canonical vertices were found in the approved Unity source; UV difference is zero, triangle counts agree for every assembly, maximum position difference is 2.98e-08 m, maximum normal-vector difference is 0.000794 (FBX precision).
- Eighteen image pairs differ by at most one 8-bit channel value. The pendant retains a source socket/shade coplanar cap, causing small localized depth-order differences at extreme close range; its surface positions, UVs and triangle counts still match. This inherited source detail was not redesigned during extraction.
- 15 targeted EditMode tests passed; 0 failed. Tests cover fit/crop, invalid input, catalog availability/granularity, every imported mesh's positions/normals/UVs/triangles/materials, provenance/bounds/mounts, artwork isolation/lifecycle, dynamic template fields and a physical bollard collider ray hit.
- Windows runtime script compilation passed (35 assemblies). This was not a packaged player build or broad gameplay/playthrough test.
- Separate save/reopen verification confirmed all six display slots restore their roles and per-instance texture property blocks.
- Source and existing shared materials were hash-protected. The final protection report checks every pre-existing working-tree file, including unrelated user work; Studio remains clean and active in Edit Mode.

The preview scene uses simple collision and mount examples, not a simulation of electrical service or an engineering certification. Detailed source membership and pixel statistics are in `source-comparison.json`, `pixel-comparison.json`, and the export manifest.

## Deferred scope

No rainwater, conduit, circulation, structural, window, rigging or blackout kits; no LODs, optimization, material consolidation, Stage 1 replacement, Administration changes, electrical/acoustic/water simulation, or full Set Builder UI. Lever states remain representational. Glazing is an optional interface, not a new asset library. Lock/hinge dimensions require deliberate matching to future doorway assemblies. Runtime upload/storage/persistence of external player art remains future integration work.

The approved Stage 1 master remains visually and structurally unchanged. M1 additions are independent, and unrelated working-tree changes are preserved. Stop at this milestone.
