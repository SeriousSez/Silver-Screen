"""A3.2 preservation and targeted furniture-route checks; no general art audit."""
import json,gzip,hashlib,math
from pathlib import Path
import numpy as np
R=Path(__file__).resolve().parents[2];H=Path(__file__).parent
read=lambda p:json.loads(gzip.decompress((R/p).read_bytes()))
h=lambda x:hashlib.sha256(json.dumps(x,sort_keys=True).encode()).hexdigest()
a=read('ArtExports/StageSchoolA3/stage_school_meshes.json.gz');b=json.loads((H/'a32_baseline.json').read_text());r=json.loads((H/'generation_report.json').read_text())
changed=[p['name'] for p in a['parts'] if h(p)!=b['parts'].get(p['name'])]
allowed=lambda n:n.startswith(('Shell/','Ceilings/SupportPendant')) or n in ['Roofs/Entrance/Rainwater','Roofs/Hall/Rainwater','Roofs/Hall','Roofs/Hall/Flashing','Roofs/Audition/Rainwater','Furnishings/Reception/Counter','Furnishings/Reception/Accessories','Furnishings/Washroom','Doors/Cubicle0','Doors/Cubicle1']
additional=lambda n:n=='Furnishings/Audition/Curtains' or (n.startswith('Roofs/') and n.endswith('/Rainwater'))
assert not [n for n in changed if not allowed(n) and not additional(n)],[n for n in changed if not allowed(n) and not additional(n)]
basefixtures={f['group']:f for f in b['fixtures']};changedfixtures=[f['group'] for f in a['fixtures'] if f!=basefixtures[f['group']]]
assert all(n.startswith(('Furnishings/Reception/','Furnishings/Staff/')) for n in changedfixtures)
assert len(a['fixtures'])==len(b['fixtures']) and h(a['equipment'])==b['equipment']
# Blender UV spheres can reorder tessellation/round normals at the last decimal.
# Reconstructed pre-A3.2 source is a reference, never a replacement for the immutable baseline.
pendant_reference=json.loads((H/'a32_pendant_reference.json').read_text())
current={p['name']:p for p in a['parts']}
pendant_evidence=[]
for old in pendant_reference:
 p=current[old['name']]
 positions=lambda q:sorted(map(tuple,np.array(q['positions']).reshape(-1,3)))
 assert positions(p)==positions(old)
 assert p['materials']==old['materials']
 # Match normals by position, allowing only Blender's six-decimal export rounding.
 def normals(q):
  out={}
  for v,n in zip(np.array(q['positions']).reshape(-1,3),np.array(q['normals']).reshape(-1,3)):
   out.setdefault(tuple(v),[]).append(n)
  return out
 nn=normals(p);oo=normals(old);delta=0
 for v,values in nn.items():
  for n in values:delta=max(delta,min(float(np.max(np.abs(n-m))) for m in oo[v]))
 assert delta<.000003,delta
 pendant_evidence.append(dict(group=p['name'],positions_identical=True,max_normal_rounding_difference=delta,materials_identical=True))

protected=json.loads((H/'preservation_baseline.json').read_text());assert all(hashlib.sha256((R/p).read_bytes()).hexdigest()==v for p,v in protected.items())
assert r['roofs']==json.loads((R/'ArtSource/StageSchoolA2/generation_report.json').read_text())['roofs']
kit={p['name']:p for p in read('ArtExports/PeriodEnvironment1930/kit_meshes.json.gz')['parts']};bounds={}
for f in a['fixtures']:
 p=kit[f['asset']];v=np.array(p['positions']).reshape(-1,3);q=math.radians(f['yaw']);c,s=math.cos(q),math.sin(q)
 v=v@np.array([[c,0,-s],[0,1,0],[s,0,c]])+f['position'];bounds[f['group']]=[v.min(0).tolist(),v.max(0).tolist()]
for p in a['parts']:
 v=np.array(p['positions']).reshape(-1,3);assert np.isfinite(v).all()
 bounds[p['name']]=[v.min(0).tolist(),v.max(0).tolist()]
 for sm in p['submeshes']:assert min(sm['indices'])>=0 and max(sm['indices'])<len(v)
# Doorway approach rectangles reach .85 m into each adjoining room. Flag, do not auto-fix.
fixed={n:v for n,v in bounds.items() if n.startswith('Furnishings/') and any(x in n for x in ['Desk0','Desk1','/Desk','Table','/Counter']) and not any(x in n for x in ['Lamp','Telephone','Notes','Materials'])}
hits=[]
for d in r['doors']:
 if 'Cubicle' in d['group']:continue
 x,y=d['center'];q=d['angle'];t=np.array([math.cos(q),math.sin(q)]);normal=np.array([-t[1],t[0]])
 for n,(lo,hi) in fixed.items():
  # Project every bounding corner into portal coordinates.
  corners=np.array([[xx-x,yy-y] for xx in [lo[0],hi[0]] for yy in [lo[2],hi[2]]]);u=corners@t;v=corners@normal
  if u.min()<d['width']/2 and u.max()>-d['width']/2 and v.min()<.85 and v.max()>-.85:hits.append([d['group'],n])
staff={n:v for n,v in bounds.items() if n.startswith('Furnishings/Staff/')}
# A continuous 1 m north/south route x[-4.7,-3.7], with generous clearance behind seats.
route_hits=[n for n,(lo,hi) in staff.items() if lo[0]<-3.7 and hi[0]>-4.7 and lo[2]<8 and hi[2]>3.4]
assert not route_hits,route_hits
assert all(v[0][0]>-6.91 for n,v in staff.items() if n in basefixtures)
out=dict(changed_groups=changed,unchanged_groups=len(a['parts'])-len(changed),changed_fixtures=changedfixtures,pendant_preservation=pendant_evidence,protected_files_unchanged=len(protected),roof_massing_unchanged=True,equipment_unchanged=True,doorway_approach_candidates=hits,staff_bounds=staff,staff_one_metre_route_clear=True,source_checks=r['qa']['a32'])
(R/'ArtReview/StageSchoolA1/a32_source_qa.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))

