"""Small Blender geometry diagnostic for the edge-profile defect found in A3."""
import bpy,bmesh,json
for profile in [0,.5]:
 m=bpy.data.meshes.new('probe');m.from_pydata([(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)],[],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]);m.update()
 bm=bmesh.new();bm.from_mesh(m)
 bm.normal_update()
 bmesh.ops.bevel(bm,geom=list(bm.edges),offset=.2,segments=5,profile=profile,affect='EDGES',clamp_overlap=True)
 print('BEVEL_PROBE',profile,len(bm.verts),[(min(v.co[i] for v in bm.verts),max(v.co[i] for v in bm.verts)) for i in range(3)], [tuple(round(x,3) for x in v.co) for v in list(bm.verts)[:10]])
 bm.free()
