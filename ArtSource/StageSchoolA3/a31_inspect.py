import bpy,json
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath='ArtSource/StageSchoolA3/StageSchool_A3.blend')
for cn in ['Ceilings/Pendant00','Ceilings/Pendant01','Shell/Raised/HallLeft','Shell/Raised/HallRight','Roofs/Hall']:
 c=bpy.data.collections.get(cn)
 print(cn)
 for o in c.objects:
  if o.type!='MESH':continue
  if cn.startswith('Roofs') and not o.name.startswith(('Eave','Hip','Batched','Roof')):continue
  v=[o.matrix_world@p.co for p in o.data.vertices]
  print(o.name, [tuple(round(f(vv[i] for vv in v),3) for i in range(3)) for f in (min,max)])
