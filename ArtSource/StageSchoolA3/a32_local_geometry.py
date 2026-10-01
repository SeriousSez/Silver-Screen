import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
R=Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
def tree(o):
 o.data.calc_loop_triangles();return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[t.vertices for t in o.data.loop_triangles],all_triangles=True)
gutters=[]
for o in bpy.data.collections['Roofs/Audition/Rainwater'].objects:
 if o.name.startswith('Pitched'):
  vv=[v.co for v in o.data.vertices]
  if max(v.x for v in vv)<2.7:gutters.append(o)
checks=[]
for name in ['Hall','Interview']:
 for o in bpy.data.collections['Roofs/'+name].objects:
  if not o.name.startswith('Batched fired caps'):continue
  t=tree(o)
  for g in gutters:
   overlaps=t.overlap(tree(g));o.data.calc_loop_triangles()
   checks.append(dict(roof=name,gutter=g.name,overlaps=len(overlaps),cap_faces=[list(sum((o.data.vertices[v].co for v in o.data.loop_triangles[i].vertices),__import__('mathutils').Vector())/3) for i,j in overlaps[:12]]))
# All deliberate vertical scoring strokes must remain within actual stone base height.
scoring=[]
for c in bpy.data.collections:
 if c.library or not c.name.startswith('Shell/'):continue
 for o in c.objects:
  if o.name.startswith(('Restrained front stone course','Plinth staggered vertical joint')):
   vv=[o.matrix_world@v.co for v in o.data.vertices]
   scoring.append(dict(group=c.name,zmax=max(v.z for v in vv)))
out=dict(cap_gutter=checks,scoring_count=len(scoring),scoring_above_stone=[v for v in scoring if v['zmax']>.701])
(R/'ArtReview/StageSchoolA1/a32_local_geometry.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
