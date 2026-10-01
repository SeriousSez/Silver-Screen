"""Scoped source QA. These bounds checks supplement, never replace, Unity review."""
import gzip, json, math
from pathlib import Path
import numpy as np
from scipy.spatial import cKDTree

ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
packet=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA4/stage_school_meshes.json.gz').read_bytes()))
old=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz').read_bytes()))
oldparts={p['name']:p for p in old['parts']}
report=json.loads((HERE/'generation_report.json').read_text())
checks=[]
def check(name,value):
    checks.append(dict(check=name,passed=bool(value)))

check('Exactly three occupied rooms', [r[0] for r in report['room_floor_extents']]==['HiringHall','Audition','Interview'])
check('Single main roof mass', report['roof_masses']==1)
check('No legacy roof masses', all(not p['name'].startswith('Roofs/') or p['name'].startswith('Roofs/Main/') for p in packet['parts']))
check('No unwanted room program',not any(any(s in p['name'] for s in ['Reception','Waiting','Lounge','Records','Storage','Washroom','Flexible','ServicePassage']) for p in packet['parts']))
bad_mesh=[];degenerate=[];reused=[]
for p in packet['parts']:
    v=np.asarray(p['positions']).reshape(-1,3); n=np.asarray(p['normals']).reshape(-1,3)
    if not np.isfinite(v).all() or not np.isfinite(n).all():bad_mesh.append(p['name'])
    for sub in p['submeshes']:
        ix=np.asarray(sub['indices'],dtype=int).reshape(-1,3)
        if ix.size and (ix.min()<0 or ix.max()>=len(v)):bad_mesh.append(p['name']);continue
        points=v[ix]
        area=np.linalg.norm(np.cross(points[:,1]-points[:,0],points[:,2]-points[:,0]),axis=1)
        if np.any(area<1e-9):degenerate.append(p['name'])
    if p.get('sourceGroup'):
        orig=oldparts[p['sourceGroup']]
        original=np.asarray(orig['positions']).reshape(-1,3)+np.asarray(p['offset'])
        # Packet vertex deduplication may change normal rounding. Compare occupied
        # vertex position sets to certify rigid translation, not array ordering.
        deviation=max(cKDTree(original).query(v)[0].max(),cKDTree(v).query(original)[0].max())
        reused.append(dict(group=p['name'],source=p['sourceGroup'],max_deviation_metres=float(deviation),translation_only=bool(deviation<.000003)))
check('Finite vertex/normal data and valid indices',not bad_mesh)
check('No degenerate exported triangles',not degenerate)
check('Reused assemblies retain source geometry',all(r['translation_only'] for r in reused))
check('Approved equipment identity', [f['asset'] for f in packet['equipment']]==['StudioCamera1930','StudioLamp1930','StudioLamp1930'])
check('No exported contextual floor text', not any(s in p['name'] for p in packet['parts'] for s in ['ACTOR','DIRECTOR','EXTRA','DISMISS','SCREEN TEST','INTERVIEW']))
readiness=json.loads((HERE/'authoring_readiness.json').read_text())
check('Four context regions with shared fourth state',len(readiness['hall_regions'])==4 and len(readiness['fourth_region_contexts'])==2)
check('Six documented exterior applicant positions',len(readiness['applicants_outside'])==6)
# Floor boxes are checked against the room envelope, not emitted into the asset.
_,a,b,c,d=report['room_floor_extents'][0]
check('Four region envelopes fit the hall',all(a<r['center'][0]-r['size'][0]/2 and r['center'][0]+r['size'][0]/2<b and c<r['center'][2]-r['size'][1]/2 and r['center'][2]+r['size'][1]/2<d for r in readiness['hall_regions']))
roof=[p for p in packet['parts'] if p['name'].startswith('Roofs/')]
check('Roof stays above occupied ceiling',all(min(p['positions'][1::3])>=4.69 for p in roof if not p['name'].endswith('Rainwater')))
result=dict(checks=checks,reused_assemblies=reused,invalid_meshes=bad_mesh,degenerate_meshes=degenerate,
            status='PASS' if all(c['passed'] for c in checks) else 'FAIL',
            limitations='Geometry and bounds evidence only; no general collision proof, navigation simulation or hydraulic simulation.')
(ROOT/'ArtReview/StageSchoolA4/source_qa.json').write_text(json.dumps(result,indent=2))
print(json.dumps({k:v for k,v in result.items() if k!='reused_assemblies'},indent=2))
assert result['status']=='PASS'
