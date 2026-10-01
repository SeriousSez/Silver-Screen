"""Small authored-surface helpers for the unrigged canonical visual gate."""
import bpy,bmesh,math
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

TAU=math.tau
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def collection(name):
    c=bpy.data.collections.get(name)
    if c is None:c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c)
    return c
def place(obj,col):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection(col).objects.link(obj)
def material(name,color,rough=.7,metal=0):
    m=bpy.data.materials.new('Review · '+name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    m['status']='Provisional neutral-light concept palette; not production pigmentation/texturing'
    return m
def assign(o,mat):o.data.materials.clear();o.data.materials.append(mat)
def mesh(name,vs,fs,mat,col,sub=0,thickness=0):
    m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
    o=bpy.data.objects.new(name,m);collection(col).objects.link(o);assign(o,mat)
    for p in m.polygons:p.use_smooth=True
    if sub:
        mod=o.modifiers.new('Editable surface refinement','SUBSURF');mod.levels=sub;mod.render_levels=sub
    if thickness:
        mod=o.modifiers.new('Physical garment thickness','SOLIDIFY');mod.thickness=thickness;mod.offset=-1
    o['module']=col;o['rig_status']='UNRIGGED_VISUAL_GATE';o['source']='Authored for approved male foundation; no rejected mesh input'
    return o
def grid(name,rows,cols,fn,mat,col,closed=False,sub=1,thickness=.001):
    vs=[tuple(fn(j/(rows-1),i/(cols if closed else cols-1))) for j in range(rows) for i in range(cols)]
    fs=[(j*cols+i,j*cols+(i+1)%cols,(j+1)*cols+(i+1)%cols,(j+1)*cols+i) for j in range(rows-1) for i in range(cols if closed else cols-1)]
    o=mesh(name,vs,fs,mat,col,sub,thickness);uv=o.data.uv_layers.new(name='AuthoredPanelUV')
    for p in o.data.polygons:
        us=[(o.data.loops[li].vertex_index%cols)/(cols if closed else cols-1) for li in p.loop_indices]
        if closed and max(us)-min(us)>.5:us=[u+1 if u<.5 else u for u in us]
        for li,u in zip(p.loop_indices,us):uv.data[li].uv=(u,(o.data.loops[li].vertex_index//cols)/(rows-1))
    return o
def cord(name,pts,r,mat,col,closed=False):
    d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.resolution_u=2;d.bevel_depth=r;d.bevel_resolution=2
    sp=d.splines.new('POLY');sp.points.add(len(pts)-1)
    for p,q in zip(sp.points,pts):p.co=(*q,1)
    sp.use_cyclic_u=closed;o=bpy.data.objects.new(name,d);collection(col).objects.link(o);assign(o,mat)
    o['module']=col;o['rig_status']='UNRIGGED_VISUAL_GATE';return o
def button(name,p,r,mat,col,axis=(0,-1,0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=p)
    o=bpy.context.object;o.name=name;o.scale=(r,r,.0017);o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);place(o,col);assign(o,mat)
    for f in o.data.polygons:f.use_smooth=True
    o['module']=col;return o
def tree(o):bpy.context.view_layer.update();return BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
def project_y(bvh,x,z,front=True,padding=0,fallback=.08):
    sign=-1 if front else 1
    hit=bvh.ray_cast(Vector((x,sign*.7,z)),Vector((0,-sign,0)))[0]
    return (hit.y if hit is not None else sign*fallback)+sign*padding
def frame(a,b):
    a=np.asarray(a);b=np.asarray(b);axis=(b-a)/np.linalg.norm(b-a)
    u=np.array([1.,0,0]);u-=np.dot(u,axis)*axis;u/=np.linalg.norm(u);v=np.cross(axis,u)
    return axis,u,v
