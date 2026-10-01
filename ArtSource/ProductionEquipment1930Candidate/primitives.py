"""Metric mesh primitives for independently reusable production equipment."""
import bpy,bmesh,math
from mathutils import Vector
import geometry as G
from geometry import mesh,tube
def box(n,p,s,mat='Steel',bevel=.008):
 x,y,z=p;a,b,c=[v/2 for v in s]
 o=mesh(n,[(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)
 if bevel:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(s)*.2),segments=2,affect='EDGES',clamp_overlap=True);bm.to_mesh(o.data);bm.free()
 return o
def cylinder(n,p,r,h,mat='Steel',axis=(0,0,1),g=None,segments=20):
 q=Vector(axis).to_track_quat('Z','Y');origin=Vector(p)
 vs=[origin+q@Vector((r*math.cos(i*2*math.pi/segments),r*math.sin(i*2*math.pi/segments),z)) for z in [-h/2,h/2] for i in range(segments)]
 fs=[tuple(reversed(range(segments))),tuple(range(segments,2*segments))]+[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
 o=mesh(n,vs,fs,mat,g)
 for f in list(o.data.polygons)[2:]:f.use_smooth=True
 return o
def ball(n,p,s,mat,g=None):
 bm=bmesh.new();bmesh.ops.create_uvsphere(bm,u_segments=20,v_segments=12,radius=1)
 for v in bm.verts:v.co=Vector((v.co.x*s[0]+p[0],v.co.y*s[1]+p[1],v.co.z*s[2]+p[2]))
 me=bpy.data.meshes.new(n);bm.to_mesh(me);bm.free();o=bpy.data.objects.new(n,me);G.GROUPS[g or G.CURRENT].objects.link(o);me.materials.append(G.MATS[mat])
 for f in me.polygons:f.use_smooth=True
 return o
def beam(n,a,b,w=.06,d=.06,mat='Timber',g=None):
 a,b=Vector(a),Vector(b);o=box(n,(0,0,0),(w,d,(b-a).length),mat,.004);q=(b-a).to_track_quat('Z','Y')
 for v in o.data.vertices:v.co=(a+b)/2+q@v.co
 return o
