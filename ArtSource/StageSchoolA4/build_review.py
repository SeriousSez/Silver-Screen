"""Build the A4 local review gallery from actual Unity captures and measured counts."""
from pathlib import Path
import html, json

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/StageSchoolA4'
source=json.loads((ROOT/'ArtSource/StageSchoolA4/generation_report.json').read_text())
unity=json.loads((OUT/'unity_sanity.json').read_text())
shots=[
 ('01_front_three_quarter','01 · Exterior front three-quarter','The flat parapet silhouette follows the corrected concept direction; A3.2 entrance, signage and material identity are retained.'),
 ('02_rear_three_quarter','02 · Exterior rear three-quarter','Two rear perimeter outlets; high audition windows leave a solid performance wall.'),
 ('03_front_elevation','03 · Straight front elevation','Raised entrance façade within a single low roof composition. Applicant pockets flank the clear central approach.'),
 ('04_roof','04 · Elevated roof view','One shallow-fall mineral roof, concealed by stone-capped parapets. No hipped tile masses, roof valleys or internal drainage network.'),
 ('05_full_plan','05 · Full top-down interior','Exactly three rooms. Audition at rear left, Interview at rear right, Hiring Hall across the front.'),
 ('06_cutaway','06 · Elevated cutaway overview','Roof, ceiling and front façade are hidden for this review capture only. Both specialist rooms connect directly to the Hiring Hall.'),
 ('07_hiring_hall','07 · Hiring Hall','21.36 × 7.05 m. Four 3.4 × 3.2 m contextual regions fit on the open floor. Create/Import and Dismiss reuse the fourth position; there are no permanent floor labels.'),
 ('08_audition','08 · Audition / Screen Test Room','13.19 × 8.09 m. The original wide screen, platform and burgundy curtains surround a two-performer area; camera, operator, lighting and evaluators remain separate.'),
 ('09_interview','09 · Interview Room','7.99 × 8.09 m. One flexible interview room with evaluator desk, two applicant seats, bookcase and a useful filing cabinet.'),
 ('10_applicant_forecourt','10 · Exterior applicant waiting area','Two 6.45 × 2.7 m paved pockets, shared period benches and six documented standing positions. A 2.1 m public walk and clear central approach remain outside those positions.'),
 ('11_forecourt_detail','11 · Bench and planter detail','Both shared benches face outward toward the public walk. Four planters have layered curved leaves, connected stems, fine midribs and three local foliage colours.'),
 ('12_planter_detail','12 · Foliage close-up','Eighteen individually shaped leaves per plant, with smooth curved surfaces, subtle curled edges and closed undersides. Soil sits below a rounded open planter rim.'),
 ('13_right_bench_clearance','13 · Right bench clearance','The bench sits 20 cm farther forward. Its complete rear mesh envelope clears the projecting facade strips by at least 11.76 cm.'),
 ('14_left_bench_clearance','14 · Left bench clearance','The mirrored placement has the same measured clearance. Both benches remain level on the forecourt paving.'),
]
for name,_,_ in shots:
    assert (OUT/(name+'.png')).is_file(),name
cards='\n'.join(f'<figure id="{name}"><a href="{name}.png" target="_blank"><img src="{name}.png" alt="{html.escape(title)}" loading="lazy"></a><figcaption><h2>{title}</h2><p>{caption}</p></figcaption></figure>' for name,title,caption in shots)
rows=''.join(f'<tr><td>{n.replace("HiringHall","Hiring Hall")}</td><td>{b-a:.2f} × {d-c:.2f} m</td><td>{(b-a)*(d-c):.2f} m²</td></tr>' for n,a,b,c,d in source['room_floor_extents'])
OUT.joinpath('review.html').write_text(f'''<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SilverScreen — Stage School A4 review</title>
<style>
*{{box-sizing:border-box}}body{{margin:0;background:#171c1e;color:#ebe5d9;font:16px/1.6 system-ui,sans-serif}}main{{max-width:1500px;margin:auto;padding:42px 28px 60px}}header{{max-width:1100px;margin-bottom:32px}}.eyebrow{{color:#c6a96f;letter-spacing:.17em;font-size:12px;text-transform:uppercase}}h1{{font:500 clamp(30px,4vw,52px)/1.1 Georgia,serif;margin:12px 0 18px}}h2{{font-size:19px;font-weight:550;margin:0 0 8px}}p{{margin:0 0 14px;color:#c5c9c8}}.metrics{{display:flex;gap:28px;flex-wrap:wrap;border-block:1px solid #3e4545;padding:20px 0;margin:24px 0}}.metrics b{{display:block;color:#f5ebd6;font:29px Georgia,serif}}.metrics span{{font-size:13px;color:#adb5b4}}.grid{{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}}figure{{margin:0;background:#22292b;border:1px solid #343c3d;overflow:hidden}}img{{width:100%;display:block}}figcaption{{padding:20px}}figcaption p{{font-size:14px;margin:0}}a{{color:#d6b87c}}.notes{{display:grid;grid-template-columns:1fr 1fr;gap:32px;margin:32px 0}}table{{border-collapse:collapse;width:100%;font-size:14px}}th,td{{text-align:left;padding:10px;border-bottom:1px solid #3a4445}}.status{{padding:18px 22px;background:#29332f;border-left:3px solid #b29b6c}}nav{{display:flex;gap:18px;flex-wrap:wrap;margin-top:22px}}@media(max-width:850px){{.grid,.notes{{grid-template-columns:1fr}}main{{padding:28px 15px}}}}
</style><main><header><div class="eyebrow">SilverScreen · Architecture / art review · A4</div><h1>Three rooms. One clear purpose.</h1><p>A compact sibling of the approved Stage School, recomposed around hiring and evaluation. The roof follows the user's corrected reference: a low flat surface behind a parapet, with a restrained raised entrance façade.</p>
<div class="metrics"><div><b>22 × 16 m</b><span>Main floor-slab footprint</span></div><div><b>352 m²</b><span>Gross floor area · 321.93 m² net rooms</span></div><div><b>1 roof</b><span>Flat parapet silhouette</span></div><div><b>{unity['triangles']:,}</b><span>Unity instance triangles</span></div></div>
<div class="status">Ready for manual architectural/art review. Runtime integration, LOD generation and Stage School gameplay remain outside this milestone.</div>
<nav><a href="#05_full_plan">Floor plan</a><a href="#04_roof">Roof</a><a href="completion_report.md">Completion report & manual checks</a><a href="source_qa.json">Source checks</a><a href="unity_sanity.json">Unity counts</a><a href="preservation_result.json">Preservation evidence</a></nav></header>
<div class="notes"><section><h2>Room dimensions</h2><table><tr><th>Room</th><th>Inside wall faces</th><th>Area</th></tr>{rows}</table><p style="margin-top:12px;font-size:13px">Room dimensions include furniture and circulation. Exterior steps, façade projections and forecourt extend beyond the main 22 × 16 m slab.</p></section><section><h2>Read the review</h2><p>These are actual Unity URP captures using the Studio daylight sky, sun and volume. Open any image at full resolution. Overview visibility changes are temporary authoring views, not a runtime reveal system.</p><p>The empty hall floor is intentional. Future interactions are documented in <a href="../../ArtSource/StageSchoolA4/authoring_readiness.json">authoring readiness</a>; no activity markers or gameplay anchors are included in the prefab.</p></section></div>
<div class="grid">{cards}</div><footer style="margin-top:30px"><p>{unity['vertices']:,} instance vertices · {unity['renderers']} renderers · {unity['materials']} materials · {unity['uniqueMeshes']} unique meshes. Shared source geometry and materials retain their original GUIDs.</p><p>A3.2, unused modular assets and existing runtime integration are preserved. No commit or push.</p></footer></main></html>''',encoding='utf-8')
print(OUT/'review.html')
