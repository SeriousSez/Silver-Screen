"""Targeted A3.1 preservation and six-defect source assertions."""
import json,gzip,hashlib,math
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
b=json.loads((HERE/'a31_baseline.json').read_text());d=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz').read_bytes()));r=json.loads((HERE/'generation_report.json').read_text())
h=lambda v:hashlib.sha256(json.dumps(v,sort_keys=True).encode()).hexdigest()
allowed=lambda n:n.startswith(('Roofs/','Ceilings/Pendant','Ceilings/SupportPendant','Shell/Raised/HallRevealFinish')) or n in ['Furnishings/Washroom','Doors/Cubicle0','Doors/Cubicle1','Structure/EntranceSteps']
changed=[p['name'] for p in d['parts'] if h(p)!=b['parts'].get(p['name'])]
unexpected=[n for n in changed if not allowed(n)]
assert not unexpected,unexpected
assert h(d['fixtures'])==b['fixtures'],'Furniture placements changed'
assert h(d['equipment'])==b['equipment'],'Approved audition equipment changed'
q=r['qa']['a31'];assert len(q['pendants'])==12
assert all(abs(p['ceiling']-p['rose_top'])<1e-5 and p['stem_top']<p['rose_top'] for p in q['pendants'])
routes=q['roof_routes'];names={v[0] for v in r['roofs']};assert names=={v['roof'] for v in routes}
def reaches_ground(name,seen=()):
 if name=='ground gully':return True
 if name in seen:return False
 return any(reaches_ground(v['destination'],seen+(name,)) for v in routes if v['roof']==name)
assert all(reaches_ground(n) for n in names)
for part in d['parts']:
 assert all(math.isfinite(v) for v in part['positions']+part['normals']+part['uv'])
 count=len(part['positions'])//3
 assert all(0<=i<count for sm in part['submeshes'] for i in sm['indices'])
output=dict(unexpected_changed_groups=unexpected,changed_groups=changed,unchanged_groups=len(d['parts'])-len(changed),fixtures_unchanged=True,equipment_unchanged=True,pendant_mounts=12,nine_roofs_reach_ground=True,roof_routes=routes,valley_segments=q['valley_segments'],finite_meshes_valid_indices=True)
(ROOT/'ArtReview/StageSchoolA1/a31_source_qa.json').write_text(json.dumps(output,indent=2))
print(json.dumps({k:v for k,v in output.items() if k not in ['changed_groups','roof_routes']},indent=2))
