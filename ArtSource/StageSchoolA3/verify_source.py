"""Bounded production-art QA. Candidate overlaps are leads, not automatic fixes."""
from pathlib import Path
import json,gzip,hashlib,math
import numpy as np
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
def read(p):return json.loads(gzip.decompress((ROOT/p).read_bytes()))
a=read('ArtExports/StageSchoolA3/stage_school_meshes.json.gz');old=read('ArtExports/StageSchoolA2/stage_school_meshes.json.gz')
r=json.loads((HERE/'generation_report.json').read_text());base=json.loads((HERE/'preservation_baseline.json').read_text())
changed=[p for p,h in base.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h]
assert not changed,changed
parts={p['name']:p for p in a['parts']};oldparts={p['name']:p for p in old['parts']}
assert r['footprint']==[28,24] and r['occupied_floors']==1 and len(r['roofs'])==9
assert r['roofs']==json.loads((ROOT/'ArtSource/StageSchoolA2/generation_report.json').read_text())['roofs']
for n in parts:
 if n.startswith(('Structure/Foundation/','Interior/Floors/')):assert parts[n]['positions']==oldparts[n]['positions'],n
bounds={};degenerate=0
for n,p in parts.items():
 v=np.array(p['positions']).reshape(-1,3);assert np.isfinite(v).all()
 assert len(p['normals'])==len(p['positions']) and len(p['uv'])==len(v)*2
 assert np.isfinite(p['normals']).all() and np.isfinite(p['uv']).all()
 bounds[n]=[v.min(0).tolist(),v.max(0).tolist()]
 for s in p['submeshes']:
  ids=np.array(s['indices']);assert ids.size%3==0 and ids.min()>=0 and ids.max()<len(v)
  t=v[ids.reshape(-1,3)];areas=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)
  degenerate+=int(np.sum(areas<1e-11))
kit={p['name']:p for p in read('ArtExports/PeriodEnvironment1930/kit_meshes.json.gz')['parts']}
for f in a['fixtures']:
 p=kit[f['asset']];v=np.array(p['positions']).reshape(-1,3);ang=math.radians(f['yaw']);cs,sn=math.cos(ang),math.sin(ang)
 v=v@np.array([[cs,0,-sn],[0,1,0],[sn,0,cs]])+f['position'];bounds[f['group']]=[v.min(0).tolist(),v.max(0).tolist()]
rooms={'Waiting':[-10.64,-3.06,-8.64,3.15],'Commons':[-10.64,-3.06,-.92,3.15],'Lounge':[3.12,6.59,-8.6,1],'Interview':[-2.85,2.85,5.10,11.65],'Flexible':[-13.66,-7.1,3.40,8.0],'Staff':[-6.91,-3.1,3.40,8.0],'Records':[-13.66,-8.6,10.1,13.66],'Storage':[-6.6,-3.1,10.1,13.66],'Audition':[3.1,13.66,1.1,14.68],'Reception':[-2.98,2.98,-8.70,-2.5]}
outside=[];fixed=[]
for n,(lo,hi) in bounds.items():
 if not n.startswith('Furnishings/'):continue
 room=n.split('/')[1]
 if room in rooms and not any(k in n for k in ['Curtains','Notices','Noticeboard','Portrait','ArchiveBoxes']):
  x0,x1,y0,y1=rooms[room]
  if lo[0]<x0-.03 or hi[0]>x1+.03 or lo[2]<y0-.03 or hi[2]>y1+.03:outside.append(dict(group=n,bounds=[lo,hi],room=room))
 if any(k in n.lower() for k in ['chair','desk','table','shelf','rack','cupboard','bookcase','files','counter']):fixed.append((n,lo,hi))
swings=[]
for door in r['doors']:
 if not door['swing']:continue
 cx,cy=door['center'];w=door['width'];angle=door['angle'];sign=-1 if door['hinge']=='right' else 1
 hx=cx-sign*w*.5*math.cos(angle);hy=cy-sign*w*.5*math.sin(angle)
 hits=set()
 for deg in np.linspace(0,door['swing'],40):
  theta=angle+math.radians(float(deg))
  for dist in np.linspace(.12,w,20):
   x=hx+sign*dist*math.cos(theta);y=hy+sign*dist*math.sin(theta)
   for n,lo,hi in fixed:
    if lo[0]-.025<x<hi[0]+.025 and lo[2]-.025<y<hi[2]+.025 and lo[1]<2.7 and hi[1]>.4:hits.add(n)
 if hits:swings.append(dict(door=door['group'],suspects=sorted(hits)))
drain_conflicts=[]
for d in r['drains']:
 x,y,z=d['top']
 for w in r['openings']:
  cx,cy=w['center'];ang=w['angle'];along=(x-cx)*math.cos(ang)+(y-cy)*math.sin(ang);normal=-(x-cx)*math.sin(ang)+(y-cy)*math.cos(ang)
  if abs(normal)<.60 and abs(along)<w['w']/2+.24 and z>w['z']:drain_conflicts.append([d['roof'],w['group'],w['center']])
assert not drain_conflicts,drain_conflicts
for t in r['qa']['trim_spans']:
 for lo,hi in t['door_exclusions']:assert min(hi,t['interval'][1])-max(lo,t['interval'][0])<=1e-6
gallery= bounds['Furnishings/Waiting/PortraitGallery']
gallery_conflicts=[]
for d in r['doors']:
 if abs(d['center'][1]-3.3)>.1:continue
 x=d['center'][0];half=d['width']/2+.13
 if min(gallery[1][0],x+half)>max(gallery[0][0],x-half):gallery_conflicts.append(d['group'])
assert not gallery_conflicts,gallery_conflicts
# Triangle-centre scan is a lead generator, supplemented by ceiling close captures.
ceiling_volumes=[(-3.4,3.4,-8.65,-2.6,5.69),(-10.55,-3.6,-8.55,3.2,4.04),(3.6,10.55,-8.55,.9,4.04),(-2.9,2.9,-2.4,4.9,5.29),(-2.75,2.75,5.1,11.55,4.36),(-13.55,-3.25,3.4,8,4.56),(-13.55,-3.25,8.2,13.55,3.96),(3.25,13.55,1.25,14.55,5.37)]
roof_candidates=[]
for name,p in parts.items():
 if not name.startswith('Roofs/'):continue
 v=np.array(p['positions']).reshape(-1,3)
 for sm in p['submeshes']:
  centres=v[np.array(sm['indices']).reshape(-1,3)].mean(axis=1)
  for x0,x1,z0,z1,y in ceiling_volumes:
   hits=(centres[:,0]>x0)&(centres[:,0]<x1)&(centres[:,2]>z0)&(centres[:,2]<z1)&(centres[:,1]<y)
   if hits.any():roof_candidates.append(dict(group=name,triangles=int(hits.sum())))
output=dict(protected_files=len(base),changed_protected_files=changed,macro_dimensions_preserved=True,nine_roofs_preserved=True,foundation_floor_coordinates_preserved=True,finite_meshes_valid_indices=True,near_zero_area_triangles=degenerate,windows=len(r['openings']),doors=len(r['doors']),drainage_window_conflicts=drain_conflicts,opening_aware_trim_spans=len(r['qa']['trim_spans']),gallery_door_conflicts=gallery_conflicts,roof_occupied_volume_candidates=roof_candidates,room_bounds_candidates=outside,door_swing_candidates=swings,group_bounds=bounds)
(ROOT/'ArtReview/StageSchoolA1/a3_source_qa.json').write_text(json.dumps(output,indent=2))
print(json.dumps({k:v for k,v in output.items() if k!='group_bounds'},indent=2))
