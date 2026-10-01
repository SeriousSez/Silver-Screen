"""Build a local art-review index from inspected Unity captures and canonical audit."""
from pathlib import Path
import json,html
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/StudioServices/Fidelity'
audit=json.loads((OUT/'canonical-audit.json').read_text(encoding='utf-8'))
kit=json.loads((ROOT/'ArtExports/PeriodEnvironment1930/kit_manifest.json').read_text())
def tile(path,label):return f'<a href="{path}" target="_blank"><img loading="lazy" src="{path}" alt="{html.escape(label)}"><span>{html.escape(label)}</span></a>'
comparisons=''
for name,label in [('01_front_three_quarter','Whole building'),('05_employment','Employment entrance'),('07_working_yard','Working service yard'),('10_office','Hiring office'),('11_workshop','Workshop'),('12_storage','Supply room')]:
 comparisons+=f'<h2>{label}</h2><div class="pair">'+tile('Before/'+name+'.png','Before')+tile('../Production/'+name+'.png','Refined Unity assembly')+'</div>'
detail=''.join(tile(p.name,p.stem.replace('_',' ')) for p in sorted(OUT.glob('[123][0-9]_*.png')))
rows=''.join('<tr><td>'+html.escape(r['asset'])+'</td><td>'+r['initialClass']+'</td><td>'+html.escape(r['finding'])+'</td><td>'+html.escape(r['action'])+'</td></tr>' for r in audit['assets'])
catalog=''.join(tile('Assets/'+a['name']+'.png',a['name'].replace('_',' ')) for a in sorted(kit['assets'],key=lambda a:a['name']))
page=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Studio Services — canonical fidelity review</title>
<style>*{{box-sizing:border-box}}body{{background:#ede7dc;color:#263331;margin:0;font:16px/1.5 system-ui,sans-serif}}main{{max-width:1600px;padding:32px;margin:auto}}h1,h2{{font-family:Georgia,serif}}h1{{font-size:44px}}p{{max-width:1050px}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:16px}}.grid{{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}}a{{color:#22594e}}.pair a,.grid a{{background:#fff;text-decoration:none}}img{{width:100%;display:block}}span{{padding:10px;display:block}}table{{border-collapse:collapse;width:100%;font-size:14px}}td,th{{padding:10px;border-bottom:1px solid #bbb;text-align:left;vertical-align:top}}details{{background:#fff;margin:22px 0;padding:18px}}summary{{cursor:pointer;font-weight:bold}}.note{{border-left:4px solid #9d793d;padding:10px 20px;background:#e4dccb}}@media(max-width:850px){{.pair,.grid{{grid-template-columns:1fr}}main{{padding:16px}}}}</style>
<main><p>SilverScreen · Unity 6000.6.3f1 · Art refinement</p><h1>Canonical environment kit fidelity</h1>
<p>All 57 original canonical assets audited. Their identities are retained. Four supporting additions: a slumped sack, wire wastebasket, coat stand and wall clock. Approved building massing, room layout, anchors and gameplay architecture remain the basis.</p>
<p class="note">These are actual Unity renders, supplied for visual review. Technical validation is recorded separately and does not imply visual approval.</p>
<p><a href="../Massing/approved-concept.png">Approved concept</a> · <a href="validation.md">Validation and changes</a> · <a href="canonical-audit.json">Full audit data</a> · <a href="implementation-manifest.json">Change/preservation manifest</a></p>
{comparisons}<h2>Installed construction details</h2><div class="grid">{detail}</div>
<details><summary>Systematic original-kit audit (57 assets)</summary><p>A: retained; B: targeted refinement; C: substantial refinement; D: primarily placement/connection. These labels describe the pre-refinement findings, not visual approval.</p><table><tr><th>Asset</th><th>Initial class</th><th>Finding</th><th>Action</th></tr>{rows}</table></details>
<h2>Canonical library — {len(kit['assets'])} independent assemblies</h2><p>Each image links to its full-resolution close view. Studio Services references these assets; defects were not concealed with instance-only geometry.</p><div class="grid">{catalog}</div>
<h2>Recognizable tools</h2><p>Carpenter board: claw hammer, wooden mallet, open-ended spanner, pliers, screwdriver, toothed hand saw and try square. Grounds rack: D-grip spade, rake and broom. Carrying tray: claw hammer and bench hand plane.</p>
<p>No commit or push. Dynamic doors, rain simulation and new gameplay systems remain outside this art pass.</p></main></html>'''
(OUT/'review.html').write_text(page,encoding='utf-8')
print('Written',OUT/'review.html')
