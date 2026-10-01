exec(open('ArtSource/StageSchoolB/inspect_slots.py').read().split('for name in')[0])
import numpy as np
for name in ['Shell_Front_Windows','Roofs_Entrance','Furnishings_Washroom']:
 p=a[names[name]];q=b[names[name]]
 lookup={}
 for v,n in zip(np.array(p['positions']).reshape(-1,3),np.array(p['normals']).reshape(-1,3)):lookup.setdefault(tuple(np.round(v,4)),[]).append(n)
 dots=[]
 for v,n in zip(np.array(q['positions']).reshape(-1,3),np.array(q['normals']).reshape(-1,3)):
  ns=lookup.get(tuple(np.round(v,4)))
  if ns is not None:dots.append(max(float(np.dot(n,k)) for k in ns))
 print(name,'matched',len(dots),'normal median/min',np.median(dots),min(dots),'wrong',sum(x<.99 for x in dots))
 for part in [p,q]:
  vs=np.array(part['positions']).reshape(-1,3);ns=np.array(part['normals']).reshape(-1,3);ids=np.array([i for s in part['submeshes'] for i in s['indices']]).reshape(-1,3)
  face=np.cross(vs[ids[:,1]]-vs[ids[:,0]],vs[ids[:,2]]-vs[ids[:,0]]);dot=(face*ns[ids[:,0]]).sum(1);print('winding pos/neg',sum(dot>1e-7),sum(dot< -1e-7))
