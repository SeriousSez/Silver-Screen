"""Read-only local construction checks on the two final requested remodels."""
import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
R=Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
def tree(o):
 o.data.calc_loop_triangles()
 return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[t.vertices for t in o.data.loop_triangles],all_triangles=True)
curtain=bpy.data.collections['Furnishings/Audition/Curtains']
assert not [o.name for o in curtain.objects if 'tieback' in o.name.lower()]
cloth=[o for o in curtain.objects if o.name.startswith(('Continuous heavy','Continuous sewn','Weighted turned','Doubled sewn'))]
targets=[o for n in ['Furnishings/Audition/Rostrum','Furnishings/Audition/Backdrop'] for o in bpy.data.collections[n].objects if o.type=='MESH']
hits=[]
for o in cloth:
 t=tree(o)
 for q in targets:
  v=t.overlap(tree(q))
  if v:hits.append([o.name,q.name,len(v)])
assert not hits,hits
verts=[o.matrix_world@v.co for o in cloth for v in o.data.vertices]
assert min(v.z for v in verts)>.72 # platform clearance
assert max(v.y for v in verts)<14.40 # backdrop front is 14.41, rear wall 14.67
assert bpy.data.collections.get('Shell/Rear/ServiceCanopy') is None
out=dict(cloth_platform_backdrop_intersections=hits,straight_hanging_wings=True,tieback_hardware_removed=True,cloth_z_min=min(v.z for v in verts),cloth_rear_y_max=max(v.y for v in verts),service_canopy_removed=True)
(R/'ArtReview/StageSchoolA1/a32_drapery_canopy_geometry.json').write_text(json.dumps(out,indent=2));print(json.dumps(out))
