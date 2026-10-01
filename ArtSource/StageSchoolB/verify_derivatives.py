import json,gzip,hashlib,math
from pathlib import Path
R=Path.cwd();frozen=json.loads((R/'ArtSource/StageSchoolB/master_freeze.json').read_text(encoding='utf-8-sig'))
changed=[x['path'] for x in frozen if hashlib.sha256(Path(x['path']).read_bytes()).hexdigest().upper()!=x['sha256']]
assert not changed,changed
levels=[]
for i in range(3):
 with gzip.open(R/f'ArtExports/StageSchoolB/lod{i}.json.gz','rt') as f:d=json.load(f)
 for p in d['parts']:
  assert all(math.isfinite(v) for v in p['positions']+p['normals']+p['uv'])
  n=len(p['positions'])//3
  assert all(0<=v<n for s in p['submeshes'] for v in s['indices'])
 levels.append({'level':i,'sharedMeshes':len(d['parts']),'triangles':sum(len(s['indices'])//3 for p in d['parts'] for s in p['submeshes'])})
out={'frozenFilesVerified':len(frozen),'masterChanged':changed,'levels':levels}
(R/'ArtReview/StageSchoolB/source_checks.json').write_text(json.dumps(out,indent=2));print(out)
