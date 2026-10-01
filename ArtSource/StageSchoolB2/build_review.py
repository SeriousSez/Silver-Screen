"""Build the local review index from inspected Unity captures and measured data."""
from pathlib import Path
from html import escape
import json
R=Path(__file__).resolve().parents[2];out=R/'ArtReview/StageSchoolB2'
views=[
('01_master','Approved A4 master','586,206 triangles. User-requested frontage revision.'),
('02_runtime_lod0','Runtime LOD0','466,700 triangles. Same camera as the master.'),
('03_lod1_management','LOD1 · management','151,934 triangles · 75 renderers.'),
('04_lod2_distant','LOD2 · distant','111,322 triangles · 75 renderers.'),
('05_revealed','Front-side reveal','Camera-relative walls and roof; attached fixtures follow their supports.'),
('06_opposite_reveal','Opposite-side reveal','Independent furniture remains; rear-supported curtains disappear with their wall.'),
('07_forecourt','Applicant forecourt','Separate waiting pockets and entrance landing; no frontage strip.'),
('08_compound_site','Actual placement · 37°','Play Mode construction site and legal neighboring footprint.'),
('09_real_completed','Actual completed building','Real construction completion; captured before the final paving-finish correction.'),
]
cards=''.join(f'<figure><a href="{n}.png"><img src="{n}.png" alt="{escape(title)}" loading="lazy"></a><figcaption><b>{escape(title)}</b><span>{escape(note)}</span></figcaption></figure>' for n,title,note in views)
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SilverScreen · Stage School B2</title><style>
body{margin:0;background:#14191d;color:#eee9df;font:16px/1.6 system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:36px}header{margin-bottom:26px}h1{font-weight:500;font-size:36px;margin:6px 0}small{color:#cfb784;letter-spacing:.15em}p{max-width:1050px;color:#c7cbc9}a{color:#e3c989}nav{display:flex;gap:24px;flex-wrap:wrap}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}figure{margin:0;background:#20282e;border:1px solid #364047;border-radius:8px;overflow:hidden}img{width:100%;display:block}figcaption{padding:14px 18px}b,span{display:block}span{color:#bfc7c7;font-size:14px}.caveat{border-left:3px solid #d8af65;padding:12px 20px;background:#262724;margin:24px 0}footer{margin-top:30px;color:#bfc7c7}@media(max-width:800px){main{padding:18px}.grid{grid-template-columns:1fr}h1{font-size:28px}}</style><main><header><small>SILVERSCREEN · B2 RUNTIME INTEGRATION</small><h1>Stage School · compact A4</h1><p>Frontage strip removed; entrance landing and compact waiting pockets retained. Production runtime with three LODs, shared construction, local cutaway and automatic door traversal. Unity captures from the current frontage revision; frontage and completed-building views inspected.</p><nav><a href="frontage_revision.md">Frontage revision report</a><a href="completion_report.md">Historical integration report</a><a href="runtime_metrics.json">Measured LOD costs</a><a href="live.txt">Passing integration log</a><a href="anchors.csv">25 authored anchors</a></nav></header><div class="caveat"><b>Frontage and navigation checks passed; pointer-input validation remains incomplete.</b>25 Edit Mode tests passed. Construction and all navigation routes passed with background execution enabled; the broader run failed pointer-input checks. Editor completion has shown spikes (the latest background-enabled run reached 2,468 ms); smooth target-player completion is not certified.</div><section class="grid">'''+cards+'''</section><footer>Studio PC_RPAsset · Unity 6000.6.3f1 · Shared art and unrelated A4 meshes unchanged; original baseline retained · No Stage School gameplay · No commit or push.</footer></main></html>'''
(out/'review.html').write_text(html,encoding='utf-8')
changed=[
'Assets/Scripts/Domain/Buildings/BuildingDefinition.cs',
'Assets/Scripts/Presentation/Buildings/StudioConstructionDriver.cs',
'Assets/Scripts/Presentation/Buildings/ConstructionDressingGenerator.cs',
'Assets/SilverScreen/Environment/StageSchoolB/Runtime/StageSchoolInfrastructure.cs',
'Assets/SilverScreen/Environment/StageSchoolB/Editor/StageSchoolRuntimeBuilder.cs',
'Assets/Tests/Editor/StageSchoolRuntimeTests.cs',
'Assets/Tests/Editor/StageSchoolB2Validation.cs','Assets/Tests/Editor/StageSchoolB2Validation.cs.meta',
'Assets/Tests/Editor/StageSchoolB2InputValidation.cs','Assets/Tests/Editor/StageSchoolB2InputValidation.cs.meta']
new=[]
for folder in ['ArtSource/StageSchoolB2','ArtExports/StageSchoolB2','Assets/SilverScreen/Environment/StageSchoolB2','ArtReview/StageSchoolB2']:
    new.extend(p.relative_to(R).as_posix() for p in (R/folder).rglob('*') if p.is_file() and '__pycache__' not in p.parts)
(out/'files_changed.json').write_text(json.dumps(dict(modified_or_added_shared=changed,new_b2_files=sorted(new),moved={'from':'Assets/SilverScreen/Environment/StageSchoolB/Resources/StageSchool_Runtime.prefab','to':'Assets/SilverScreen/Environment/StageSchoolB/Archive/StageSchool_Runtime.prefab','guid':'6a9044b1d07c375488e32234f75b8d70','meta_preserved':True}),indent=2))
assert all((out/(n+'.png')).exists() for n,_,_ in views)
print('Review gallery:',len(views),'inspected captures')
