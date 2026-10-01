import bpy,json,gzip,hashlib
from pathlib import Path
R=Path.cwd(); p=R/'ArtSource/StageSchoolA3/a32_baseline.json'
d=json.loads(gzip.decompress((R/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz').read_bytes()))
h=lambda v:hashlib.sha256(json.dumps(v,sort_keys=True).encode()).hexdigest()
if not p.exists():p.write_text(json.dumps(dict(parts={v['name']:h(v) for v in d['parts']},fixtures=d['fixtures'],equipment=h(d['equipment'])),indent=2))
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
out=[]
for c in bpy.data.collections:
 if c.library:continue
 for o in c.objects:
  if o.type!='MESH':continue
  if c.name.startswith('Shell/') or c.name=='Furnishings/Reception/Counter' or c.name in ['Roofs/Hall/Rainwater','Roofs/Audition/Rainwater']:
   vs=[o.matrix_world@v.co for v in o.data.vertices]
   if vs:out.append(dict(group=c.name,name=o.name,lo=[min(v[i] for v in vs) for i in range(3)],hi=[max(v[i] for v in vs) for i in range(3)]))
(R/'ArtReview/StageSchoolA1/a32_objects.json').write_text(json.dumps(out,indent=2))
print('A32 fixtures',json.dumps([f for f in d['fixtures'] if '/Reception/' in f['group'] or '/Staff/' in f['group']]))
