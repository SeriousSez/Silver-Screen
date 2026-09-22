"""Summarize existing Unity evidence; never changes a Unity asset or source master."""
from pathlib import Path
from PIL import Image, ImageChops, ImageStat
import html,json
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/ReusableFixtures/Milestone1'
manifest=json.loads((ROOT/'ArtExports/ReusableFixtures/milestone1_manifest.json').read_text())
comparison=json.loads((OUT/'source-comparison.json').read_text())['assets']
tests=ET.parse(OUT/'editmode-results.xml').getroot().attrib
pixels=[]
for path in sorted(OUT.glob('*_canonical.png')):
    name=path.name.split('_',1)[1].removesuffix('_canonical.png')
    other=next(OUT.glob('*_'+name+'_source.png'))
    diff=ImageChops.difference(Image.open(path).convert('RGB'),Image.open(other).convert('RGB'))
    stats=ImageStat.Stat(diff)
    pixels.append(dict(asset=name,meanAbsoluteChannelDifference_0_to_255=sum(stats.mean)/3,maximumChannelDifference=max(v[1] for v in diff.getextrema())))
(OUT/'pixel-comparison.json').write_text(json.dumps(pixels,indent=2)+'\n')

rows=[]
for r in manifest['assets']:
    dims=' × '.join(f'{v:.3f}' for v in r['dimensions'])
    rows.append(f"| {r['name']} | {'Both / player-facing' if r['player'] else 'Architectural authoring'} | {dims} | {r['surface']} |")
table='\n'.join(rows)
max_position=max(r['maximumPositionErrorMetres'] for r in comparison)
max_normal=max(r['maximumNormalDifference'] for r in comparison)
members={m['name'] for r in manifest['assets'] for p in r['parts'] for m in p['members']}
report=f'''# Reusable asset extraction — Milestone 1

Implemented 19 independent canonical prefabs: 12 complete player-facing fixtures and 7 architectural authoring subassemblies. Stage 1 revision 04 remains the approved visual/provenance master; no migration was performed. No commit or push was made.

## Architecture and assets

The milestone adds a small Unity-independent catalog/availability model and aspect-layout calculation, Unity asset definitions, typed anchor components, editor-only provenance, optional light behavior and independent display insert behavior. It does not change the existing building/Set Builder construction code.

| Asset | Intended reuse | Visible W × H × D (m) | Placement |
| --- | --- | --- | --- |
{table}

All 19 assemblies use exact approved source surfaces. There are {len(members)} distinct source objects, 188 component memberships including shared source controls, and 27,160 source triangles across the canonical variants. There is no geometry reduction, new LOD, re-UV or fixture redesign.

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
- All canonical vertices were found in the approved Unity source; UV difference is zero, triangle counts agree for every assembly, maximum position difference is {max_position:.3g} m, maximum normal-vector difference is {max_normal:.6f} (FBX precision).
- Eighteen image pairs differ by at most one 8-bit channel value. The pendant retains a source socket/shade coplanar cap, causing small localized depth-order differences at extreme close range; its surface positions, UVs and triangle counts still match. This inherited source detail was not redesigned during extraction.
- {tests['passed']} targeted EditMode tests passed; {tests['failed']} failed. Tests cover fit/crop, invalid input, catalog availability/granularity, every imported mesh's positions/normals/UVs/triangles/materials, provenance/bounds/mounts, artwork isolation/lifecycle, dynamic template fields and a physical bollard collider ray hit.
- Windows runtime script compilation passed (35 assemblies). This was not a packaged player build or broad gameplay/playthrough test.
- Separate save/reopen verification confirmed all six display slots restore their roles and per-instance texture property blocks.
- Source and existing shared materials were hash-protected. The final protection report checks every pre-existing working-tree file, including unrelated user work; Studio remains clean and active in Edit Mode.

The preview scene uses simple collision and mount examples, not a simulation of electrical service or an engineering certification. Detailed source membership and pixel statistics are in `source-comparison.json`, `pixel-comparison.json`, and the export manifest.

## Deferred scope

No rainwater, conduit, circulation, structural, window, rigging or blackout kits; no LODs, optimization, material consolidation, Stage 1 replacement, Administration changes, electrical/acoustic/water simulation, or full Set Builder UI. Lever states remain representational. Glazing is an optional interface, not a new asset library. Lock/hinge dimensions require deliberate matching to future doorway assemblies. Runtime upload/storage/persistence of external player art remains future integration work.

The approved Stage 1 master remains visually and structurally unchanged. M1 additions are independent, and unrelated working-tree changes are preserved. Stop at this milestone.
'''
(OUT/'REPORT.md').write_text(report,encoding='utf-8')

gallery=[]
for r in manifest['assets']:
    c=next(OUT.glob('*_'+r['name']+'_canonical.png'));s=next(OUT.glob('*_'+r['name']+'_source.png'))
    gallery.append(f'<details><summary>{html.escape(r["label"])}</summary><div class="pair"><figure><img loading="lazy" src="{s.name}"><figcaption>Approved Unity source</figcaption></figure><figure><img loading="lazy" src="{c.name}"><figcaption>Canonical extraction</figcaption></figure></div></details>')
views=[('50_template_before','Same frame instances: Aurora Pictures · 1937 · THE SILVER LINING'),('51_template_after','After studio rename, clock rollover and title edit: Crescent Film Company · 1938 · MIDNIGHT ON THE COAST'),('52_rival_unchanged','Independent rival context remains Majestic Pictures · 1937'),('01_overview','Independent validation setup'),('02_electrical','Complete cabinets and utility boxes'),('03_controls','Approved three-lever hardware'),('04_lamps','Wall and overhead mounting'),('05_frames','Double frame: original fabric and independent inserts'),('06_single_frames','Single frame: acoustic and fitted artwork'),('07_hardware','Paired personnel hardware on authoring coupon'),('08_instance_swap','Same single frame after per-instance artwork swap'),('09_optional_lights','Optional lighting enabled')]
cards=''.join(f'<figure><a href="{name}.png"><img loading="lazy" src="{name}.png"></a><figcaption>{label}</figcaption></figure>' for name,label in views)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SilverScreen — Reusable Fixtures M1</title>
<style>body{margin:0;background:#182025;color:#e5dfd0;font:16px/1.6 system-ui}main{max-width:1450px;margin:auto;padding:36px}h1{font-weight:500;line-height:1.15;font-size:38px}p{max-width:1000px;color:#c5c8c7}a{color:#dfbe7f}.eyebrow{letter-spacing:.17em;font-size:12px;color:#dfbe7f}figure{margin:0 0 28px}img{width:100%;display:block}figcaption{padding:10px 14px;background:#263137}details{border-top:1px solid #4b5657;padding:14px 0}summary{cursor:pointer}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px;margin-top:16px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}.grid figure:first-child{grid-column:1/-1}.links{display:flex;flex-wrap:wrap;gap:24px;margin:24px 0 40px}@media(max-width:750px){.grid,.pair{grid-template-columns:1fr}main{padding:20px}}</style><main>
<div class="eyebrow">SILVERSCREEN / CANONICAL FIXTURES</div><h1>Reusable asset extraction<br>Milestone 1 — artwork correction</h1>
<p>19 independent canonical assemblies derived from the approved Stage 1 revision 04 master. Complete fixtures, authoring hardware and replaceable acoustic/display inserts. Stage 1 and Administration remain unchanged.</p>
<p>The first two captures show the same frame instances before and after a studio rename, clock rollover and production-title change. The third uses independent rival-studio data. Canonical frames and shared materials are unchanged.</p>
<div class="links"><a href="REPORT.md">Implementation report</a><a href="../../../Docs/Art/ReusableAssetConventions.md">Durable conventions</a><a href="FILES.md">All added files</a><a href="template-protection-check.json">Correction protection check</a><a href="source-comparison.json">Geometry comparison</a><a href="editmode-results.xml">Test results</a></div>
<div class="grid">'''+cards+'''</div><h2>Matched source / canonical comparisons</h2><p>Each left image is reconstructed from the approved Unity FBX. Each right image uses the independent canonical asset. Camera and light are matched. All 19 agree in geometry, UVs and triangle count. The pendant retains a small source cap depth-order artifact; see the report.</p>'''+''.join(gallery)+'''<p>Milestone 1 only. No commit or push. No Stage 1 migration.</p></main></html>'''
(OUT/'review.html').write_text(page,encoding='utf-8')
print('Wrote report, visual gallery and pixel statistics')
