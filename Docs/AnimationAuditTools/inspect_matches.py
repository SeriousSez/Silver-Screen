from fbx_scan import CACHE
import json,numpy as np
from pathlib import Path
c=json.loads(Path('Docs/AnimationLibraryCatalog.json').read_text()); fs={f['id']:f for f in c['files']}; ts=c['takes']
def load(t):
 f=fs[t['file_id']]; z=np.load(CACHE/(f['sha256']+'_'+str(t['stack_index'])+'.npz'))
 return {tuple(l):v for l,v in zip(z['labels'],z['values']) if l[1]=='Lcl Rotation'}
packs=[t for t in ts if 'Mocap' not in fs[t['file_id']]['relative_path'] and '/' in fs[t['file_id']]['relative_path']]
data={t['id']:load(t) for t in packs}
for t in ts:
 f=fs[t['file_id']]
 if '/' in f['relative_path']:continue
 a=load(t); results=[]
 for b in packs:
  if abs(t['duration_seconds']-b['duration_seconds'])>.08:continue
  d=data[b['id']]; common=sorted(a.keys()&d.keys())
  if len(common)<100:continue
  delta=np.array([a[k]-d[k] for k in common]);delta=(delta+180)%360-180
  results.append((float(np.sqrt(np.mean(delta**2))),fs[b['file_id']]['relative_path'],b['id']))
 if results and min(results)[0]<2:print(f['relative_path'],sorted(results)[:3])
