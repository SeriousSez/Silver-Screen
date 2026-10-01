"""Small source/reference sanity check for the single art pass; no gameplay tests."""
import gzip,hashlib,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/StageSchoolA1'
baseline=json.loads((OUT/'refinement_baseline.json').read_text())
packet=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA1/stage_school_meshes.json.gz').read_bytes()))
def digest(p):return hashlib.sha256(json.dumps(p,sort_keys=True).encode()).hexdigest()
current={p['name']:digest(p) for p in packet['parts']}
allowed=['Roofs/','Shell/Left/Rainwater','Shell/Right/Rainwater','Furnishings/Audition/Curtains','Furnishings/Audition/Camera','Furnishings/Audition/StudioLamp']
protected=[n for n in baseline['parts'] if not any(n.startswith(a) for a in allowed)]
reordered=[n for n in protected if baseline['parts'][n]!=current.get(n)]
changed=[]
for name in reordered:
 p=next(p for p in packet['parts'] if p['name']==name)
 positions=sorted(set(tuple(round(v,3) or 0.0 for v in p['positions'][i:i+3]) for i in range(0,len(p['positions']),3)))
 old=baseline['geometry'][name.replace('/','_')]
 if hashlib.sha256(json.dumps(positions).encode()).hexdigest()!=old['vertices_sha256'] or sum(len(s['indices'])//3 for s in p['submeshes'])!=old['triangles']:changed.append(name)
assert not changed,changed
assert sum(n.startswith('Furnishings/Waiting/Chair') for n in current)==8
assert hashlib.sha256((ROOT/'Assets/Scenes/Studio.unity').read_bytes()).hexdigest()==baseline['studio_sha256']
equipment=json.loads(gzip.decompress((ROOT/'ArtExports/ProductionEquipment1930Candidate/equipment_meshes.json.gz').read_bytes()))
for part in packet['parts']+[p for a in equipment['assets'] for p in a['parts']]:
 assert len(part['normals'])==len(part['positions'])
 assert len(part['uv'])//2==len(part['positions'])//3
 assert all(math.isfinite(v) for v in part['positions']+part['normals']+part['uv'])
 for s in part['submeshes']:assert len(s['indices'])%3==0 and all(0<=i<len(part['positions'])//3 for i in s['indices'])
report=dict(protected_groups_checked=len(protected),protected_groups_changed=changed,reordered_curved_groups_verified_to_one_mm=reordered,waiting_seats=8,studio_file_unchanged=True,finite_coordinates=True,mesh_indices_valid=True,equipment_instances=packet['equipment'],new_groups=sorted(set(current)-set(baseline['parts'])),retired_groups=sorted(set(baseline['parts'])-set(current)))
(OUT/'refinement_source_sanity.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
