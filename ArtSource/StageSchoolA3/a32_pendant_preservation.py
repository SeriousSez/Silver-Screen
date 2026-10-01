"""Read-only reconstruction of A3.1 pendant geometry to distinguish export ordering from edits."""
from pathlib import Path
import json,hashlib,gzip
HERE=Path(__file__).resolve().parent
s=(HERE/'generate_stage_school_a3.py').read_text().split('# Batch repetitive')[0]
s=s.replace('for row in range(1):','for row in range(2):').replace("(.005,.005,.200),'Mortar',0)","(.005,.005,.255),'Mortar',0)")
s=s.replace('# Removed isolated A2 scoring strokes on the front stucco, above the actual stone plinth.',"for side in [-1,1]:\n for j in range(3):\n  for i in range(7):box('Restrained front stone course',(side*(4.0+i*.91+(j%2)*.20),-9.075,.78+j*.24),(.005,.006,.20),'Mortar',0)")
s=s.replace("exec(compile((HERE/'manual_corrections.py').read_text(),str(HERE/'manual_corrections.py'),'exec'))",'')
k=(HERE/'known_defects.py').read_text()
a=k.index('preferred=.94');b=k.index('\n chosen=None',a)
k=k[:a]+"choices=sorted([i/100 for i in range(3,98)],key=lambda f:abs(f-.94))"+k[b:]
k='\n'.join(line for line in k.splitlines() if "if name=='Entrance' and out.y<-.9:" not in line and "if name=='Hall' and out.x>.9:end=" not in line)
s=s.replace("exec(compile((HERE/'known_defects.py').read_text(),str(HERE/'known_defects.py'),'exec'))",'exec(compile('+repr(k)+',"A31 reference drainage","exec"))')
exec(compile(s,str(HERE/'generate_stage_school_a3.py'),'exec'))
parts=[]
for n,c in G.GROUPS.items():
 if n.startswith('Ceilings/SupportPendant'):
  p=G.uv_packet(c);p['pivot']=pivots.get(n,[0,0,0]);parts.append(p)
(HERE/'a32_pendant_reference.json').write_text(json.dumps(parts))
baseline=json.loads((HERE/'a32_baseline.json').read_text())
for p in parts:print(p['name'],hashlib.sha256(json.dumps(p,sort_keys=True).encode()).hexdigest()==baseline['parts'][p['name']])
