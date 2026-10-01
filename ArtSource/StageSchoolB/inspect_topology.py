import bpy,json
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath=str(Path('ArtSource/StageSchoolA3/StageSchool_A3.blend').resolve()))
r=[]
for c in bpy.data.collections:
 if c.name in ['Roofs/Entrance','Furnishings/Audition/Curtains','Ceilings/Pendant00','Shell/Front/Banners']:
  r.append([c.name,[(o.name,o.type,len(o.data.vertices) if o.type=='MESH' else 0,[(m.type) for m in o.modifiers] if o.type=='MESH' else []) for o in c.objects]])
Path('ArtReview/StageSchoolB1/topology.json').write_text(json.dumps(r,indent=2))
