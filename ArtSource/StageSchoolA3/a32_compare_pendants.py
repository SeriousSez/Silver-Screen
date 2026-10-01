import json,gzip,numpy as np
from pathlib import Path
R=Path.cwd();a=json.loads(gzip.decompress((R/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz').read_bytes()));cur={p['name']:p for p in a['parts']}
for old in json.loads((R/'ArtSource/StageSchoolA3/a32_pendant_reference.json').read_text()):
 p=cur[old['name']]
 def rows(p,key,d):return sorted(map(tuple,np.array(p[key]).reshape(-1,d)))
 print(old['name'],'verts',len(old['positions']),len(p['positions']),'position sets',rows(old,'positions',3)==rows(p,'positions',3),'normal sets',rows(old,'normals',3)==rows(p,'normals',3),'materials',old['materials']==p['materials'])
