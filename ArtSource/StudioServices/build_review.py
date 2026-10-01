"""Assemble a local review page from actual Unity captures and validation evidence."""
from pathlib import Path
import json, html, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/StudioServices/Production'
shots=[
 ('01_front_three_quarter','Front three-quarter','Exterior'),('02_front','Front elevation','Exterior'),
 ('03_rear_yard','Rear and service yard','Exterior'),('04_management','Management distance','Exterior'),
 ('05_employment','Employment entrance','Detail'),('06_workshop_doors','Service doors and hardware','Detail'),
 ('07_working_yard','Working yard','Interior'),('08_cutaway','Cutaway overview','Interior'),
 ('09_plan','Floor plan and circulation','Interior'),('10_office','Office / hiring','Interior'),
 ('11_workshop','Workshop','Interior'),('12_storage','Supply room','Interior'),
 ('13_restored','Exterior restored after cutaway','Exterior'),('14_live_held_cutaway','Actual Studio: carrying a person','Live'),
 ('15_drainage_corner','Drainage, flashing and electrical connection','Detail'),('16_left_eave','Opposite eave and outlet','Detail'),
 ('17_live_exterior','Actual Studio: exterior restored after hiring','Live')]
checks=json.loads((OUT/'checks.json').read_text())
kit=json.loads((ROOT/'ArtExports/PeriodEnvironment1930/kit_manifest.json').read_text())
test=ET.parse(OUT/'tests.xml').getroot() if (OUT/'tests.xml').exists() else None
passed=sum(c['passed'] for c in checks['checks'])
test_summary=f"{test.get('passed')}/{test.get('total')} focused tests" if test is not None else 'Tests pending'
options=''.join(f'<option value="{n}">{label}</option>' for n,label,_ in shots if (OUT/(n+'.png')).exists())
gallery=''.join(f'<button class="thumb" data-shot="{n}"><img loading="lazy" src="{n}.png" alt="{label}"><span>{label}</span></button>' for n,label,_ in shots if (OUT/(n+'.png')).exists())
assets=kit['assets']
rows=''.join(f'<tr><td>{html.escape(a["name"])}</td><td>{html.escape(a["family"])}</td><td>{html.escape(a["surface"])}</td><td>{" Ã— ".join(f"{b-a:.2f}" for a,b in zip(a["boundsMin"],a["boundsMax"]))} m</td></tr>' for a in assets)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Studio Services â€” production detail review</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee9df;color:#252d2c;font:16px/1.55 system-ui,sans-serif}header,main{max-width:1600px;margin:auto;padding:32px}header{padding-bottom:15px}small{letter-spacing:.2em;text-transform:uppercase;color:#66634f}h1{font:48px/1.15 Georgia,serif;margin:10px 0}h2{font:27px Georgia,serif}p{max-width:1040px}.stats{display:flex;gap:12px;flex-wrap:wrap}.stats span{background:#263c39;color:#fff;padding:9px 15px;border-radius:3px}.compare{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0;background:#263c39;color:white}figure img{width:100%;display:block}figcaption{padding:12px 15px}.ref{overflow:hidden;aspect-ratio:2.43}.ref img{width:100%}.viewer{background:#52636c}.viewer img{width:100%;cursor:zoom-in}select{font:inherit;padding:9px;max-width:100%}.controls{display:flex;gap:18px;align-items:center;flex-wrap:wrap;margin:20px 0}.gallery{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.thumb{border:0;background:#fff;padding:0;text-align:left;color:inherit;cursor:pointer;font:inherit}.thumb img{width:100%;display:block}.thumb span{display:block;padding:10px}.thumb:hover{outline:2px solid #a27732}a{color:#27685d}details{background:#fff;padding:16px 22px;margin:20px 0}summary{cursor:pointer;font-weight:650}table{border-collapse:collapse;width:100%;font-size:14px}th,td{border-bottom:1px solid #ddd;padding:8px;text-align:left}.scroll{overflow:auto}.note{border-left:4px solid #b88a44;padding:10px 20px;background:#e6ddcc}footer{padding:25px 0;color:#6b6b5d}@media(max-width:850px){.compare{grid-template-columns:1fr}.gallery{grid-template-columns:repeat(2,1fr)}header,main{padding:18px}h1{font-size:36px}}
</style><header><small>SilverScreen Â· Environment art Â· Unity 6000.6.3f1</small><h1>Studio Services</h1><p>Production detailing and reusable kit. The approved 14 Ã— 9 m massing, openings, room arrangement and service-yard relationship are retained. This page presents actual Unity captures for visual review.</p><div class="stats">
<span>__KIT__ canonical kit assets</span><span>__FIXTURES__ linked instances</span><span>__CHECKS__ art / navigation checks</span><span>__TESTS__</span></div></header>
<main><div class="compare"><figure><div class="ref"><img src="../Massing/approved-concept.png" alt="Approved Studio Services concept"></div><figcaption>Approved concept â€” primary exterior reference Â· <a style="color:#dcc28d" href="../Massing/approved-concept.png" target="_blank">Full reference sheet</a></figcaption></figure>
<figure><img src="01_front_three_quarter.png" alt="Studio Services Unity front three-quarter"><figcaption>Unity production assembly â€” original massing camera</figcaption></figure></div>
<div class="controls"><label for="view">Inspect a view</label><select id="view">__OPTIONS__</select><a id="full" href="01_front_three_quarter.png" target="_blank">Open full-resolution capture</a></div>
<div class="viewer"><img id="selected" src="01_front_three_quarter.png" alt="Selected Unity capture"></div>
<p class="note">Technical validation does not constitute visual approval. Final LODs, dynamic doors, inventory/maintenance simulation and later-era replacements are outside this pass. Current complete assembly: __TRIS__ triangles before culling; repeated fixtures share meshes and materials.</p>
<p><a href="../Fidelity/review.html">Current canonical fidelity audit, close-ups and before/after review</a></p><h2>Interiors, exterior and live integration</h2><div class="gallery">__GALLERY__</div>
<details><summary>What is integrated</summary><p>The existing New Studio entry point loads StudioServices_Live with the authoritative service-facility identity, five waiting positions, two interior hiring spots and construction reservation metadata. The generic cutaway hides roof/camera-facing renderers while preserving physical collision. Carry release, separate-click placement and Escape cancellation use the existing person interaction rules.</p><p>The populated developer scene remains available. Two explicit scenario references hide its art-only Administration instance and reflection probe only for New Studio. The live slab sits 20 mm above the lot to prevent coplanar flicker. A 25 mm navigation bake and 60 mm wall, workshop-prop and fence/gate bake guards provide reliable interior turns without changing physical walls or employee size.</p></details>
<details><summary>Canonical kit catalog (__KIT__ assets)</summary><p>Independent source collections, meshes, prefabs and catalog definitions. Canonical front +Z; Unity metres; 1930 availability with no forced expiry. Existing SilverScreen lamps and electrical cabinets are referenced unchanged.</p><div class="scroll"><table><thead><tr><th>Canonical assembly</th><th>Family</th><th>Placement</th><th>Visible bounds (X / Y / Z)</th></tr></thead><tbody>__ROWS__</tbody></table></div></details>
<details><summary>Evidence and reproducibility</summary><p><a href="checks.json">37-check preflight</a> Â· <a href="tests.xml">Focused Unity tests</a> Â· <a href="runtime.txt">Actual Play Mode proof</a> Â· <a href="implementation-manifest.json">Implementation / preservation manifest</a> Â· <a href="../../../ArtSource/StudioServices/README.md">Build instructions</a> Â· <a href="../../../ArtSource/PeriodEnvironment1930/README.md">Kit conventions</a></p><p>Source generators own the Blender files and exports. Unity builders update only their output folders. The approved massing source, Stage 1 and Administration art assets remain unchanged.</p></details>
<footer>No commit or push. Visual review requested on the finished production pass.</footer></main><script>
const select=document.querySelector('#view'),picture=document.querySelector('#selected'),link=document.querySelector('#full');
function show(name){select.value=name;picture.src=name+'.png';picture.alt=select.selectedOptions[0].textContent;link.href=picture.src;}
select.addEventListener('change',()=>show(select.value));document.querySelectorAll('[data-shot]').forEach(b=>b.onclick=()=>{show(b.dataset.shot);picture.scrollIntoView({behavior:'smooth',block:'center'});});picture.onclick=()=>window.open(link.href,'_blank');
</script></html>'''
for key,value in {'KIT':len(assets),'FIXTURES':checks['fixtureInstances'],'CHECKS':f'{passed}/{len(checks["checks"])}','TESTS':test_summary,'OPTIONS':options,'GALLERY':gallery,'TRIS':f'{checks["triangles"]:,}','ROWS':rows}.items():page=page.replace('__'+key+'__',str(value))
(OUT/'review.html').write_text(page,encoding='utf-8')
print('Review written:',OUT/'review.html')
