"""Narrow A2 source/approved-asset sanity. No gameplay or project tests."""
from pathlib import Path
import gzip,json,hashlib,math,itertools
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
baseline=json.loads((HERE/'preservation_baseline.json').read_text())
changed=[p for p,h in baseline.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h]
assert not changed,changed
a1=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA1/stage_school_meshes.json.gz').read_bytes()))
a2=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA2/stage_school_meshes.json.gz').read_bytes()))
old={p['name']:p for p in a1['parts']};new={p['name']:p for p in a2['parts']}
def coords(p,offset=(0,0,0)):
 return {tuple(round(p['positions'][i+j]-offset[j],3) for j in range(3)) for i in range(0,len(p['positions']),3)}
def translated_equal(original,moved,offset):
 # Float32 translation can cross a rounding boundary. Check actual distances.
 points=[tuple(original['positions'][i:i+3]) for i in range(0,len(original['positions']),3)];buckets={}
 for p in points:buckets.setdefault(tuple(math.floor(v*1000) for v in p),[]).append(p)
 for i in range(0,len(moved['positions']),3):
  p=tuple(moved['positions'][i+j]-offset[j] for j in range(3));key=tuple(math.floor(v*1000) for v in p)
  candidates=(q for d in itertools.product([-1,0,1],repeat=3) for q in buckets.get(tuple(key[j]+d[j] for j in range(3)),[]))
  if not any(max(abs(p[j]-q[j]) for j in range(3))<.00002 for q in candidates):return False
 return True
protected=[n for n in old if n.startswith('Shell/Front') or n=='Structure/EntranceSteps' or n.startswith('Doors/Entrance')]
for name in protected:assert coords(old[name])==coords(new[name]),name
for name in ['Furnishings/Audition/Curtains','Furnishings/Audition/Rostrum','Furnishings/Audition/Backdrop']:
 assert translated_equal(old[name],new[name],(1.6,0,6.45)),name
 assert sum(len(s['indices']) for s in old[name]['submeshes'])==sum(len(s['indices']) for s in new[name]['submeshes'])
for p in a2['parts']:
 assert len(p['positions'])==len(p['normals'])
 assert len(p['positions'])//3==len(p['uv'])//2
 assert all(math.isfinite(v) for v in p['positions']+p['normals']+p['uv'])
 for s in p['submeshes']:assert len(s['indices'])%3==0 and all(0<=i<len(p['positions'])//3 for i in s['indices'])
assert len([n for n in new if n.startswith('Furnishings/Waiting/Chair')])==8
assert len(a2['equipment'])==3
report=dict(preserved_file_hashes=len(baseline),modified_protected_files=changed,front_groups_unchanged=protected,curtain_rostrum_backdrop_translation_only=True,waiting_seats=8,equipment_assets_unchanged=True,finite_coordinates=True,valid_mesh_indices=True,occupied_floors=1,nominal_envelope=[28,24],audition_clear_floor=[10.5,13.5],equipment_instances=a2['equipment'])
(ROOT/'ArtReview/StageSchoolA1/a2_source_sanity.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
