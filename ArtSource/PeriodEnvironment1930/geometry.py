"""Shared deterministic metre-scale authoring helpers. Blender Z up, authoring face -Y."""
import bpy,bmesh,math,json,gzip,pathlib
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
PALETTE={
 'WarmStucco':([.53,.445,.335],0,.87,'stucco'), 'InteriorPlaster':([.59,.535,.43],0,.88,'stucco'),
 'LimestoneTrim':([.64,.56,.435],0,.78,'stone'), 'Brick':([.27,.10,.048],0,.86,'stone'),
 'BrickLight':([.36,.16,.075],0,.86,'stone'), 'BrickDark':([.19,.063,.029],0,.87,'stone'),
 'Mortar':([.30,.28,.235],0,.90,'stone'), 'WorkshopPaint':([.070,.115,.095],0,.68,'paint'),
 'PaintWear':([.20,.245,.185],0,.77,'paint'), 'Timber':([.22,.125,.052],0,.73,'timber'),
 'TimberLight':([.36,.245,.122],0,.79,'timber'), 'TimberDark':([.095,.050,.022],0,.7,'timber'),
 'RoofMetal':([.115,.126,.12],.7,.59,'metal'), 'Galvanized':([.30,.32,.30],.78,.48,'metal'),
 'Steel':([.032,.038,.033],.65,.51,'paint'), 'Iron':([.06,.065,.055],.72,.55,'metal'),
 'Brass':([.36,.24,.08],.82,.33,'metal'), 'Glass':([.12,.17,.17],0,.12,''),
 'Concrete':([.29,.27,.235],0,.85,'concrete'), 'OfficeFloor':([.23,.195,.145],0,.7,'floor'),
 'Paper':([.71,.65,.48],0,.9,''), 'PaperLight':([.85,.79,.63],0,.88,''),
 'Black':([.012,.014,.012],.15,.38,''), 'Canvas':([.35,.29,.17],0,.93,'canvas'),
 'ToolRed':([.27,.052,.027],.15,.66,'paint'), 'Leather':([.13,.052,.024],0,.73,'canvas'),
 'ContactPatina':([1,1,1],0,.9,'weather:Weather_Contact'),
 'RunoffPatina':([1,1,1],0,.9,'weather:Weather_Runoff')}
MATS={}; GROUPS={}; CURRENT=''; ASSETS={}; COLLIDERS={}
def init_materials():
 for n,(c,m,r,t) in PALETTE.items():
  mat=bpy.data.materials.get(n) or bpy.data.materials.new(n);mat.diffuse_color=(*c,1);mat.use_nodes=True
  bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=m;bs.inputs['Roughness'].default_value=r;MATS[n]=mat
def group(n):
 global CURRENT
 CURRENT=n
 if n not in GROUPS:
  c=bpy.data.collections.get(n) or bpy.data.collections.new(n)
  if c.name not in bpy.context.scene.collection.children:bpy.context.scene.collection.children.link(c)
  GROUPS[n]=c
 return GROUPS[n]
def asset(n,family,surface='Ground',clearance=.6):
 group(n);ASSETS[n]=dict(name=n,family=family,surface=surface,clearance=clearance,anchors=[]);COLLIDERS[n]=[]
def obj(o,n,mat,g=None):
 o.name=n
 for c in list(o.users_collection):c.objects.unlink(o)
 groupname=g or CURRENT
 c=GROUPS.get(groupname)
 if c is None:c=group(groupname)
 c.objects.link(o);o.data.materials.clear();o.data.materials.append(MATS[mat]);return o
def box(n,p,s,mat='Timber',bevel=.008,g=None,solid=False):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);obj(o,n,mat,g)
 if bevel:
  m=o.modifiers.new('Edge','BEVEL');m.width=min(bevel,min(s)*.24);m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
 if solid:COLLIDERS.setdefault(g or CURRENT,[]).append(dict(name=n,position=list(p),size=list(s)))
 return o
def mesh(n,v,f,mat='Steel',g=None):
 m=bpy.data.meshes.new(n);m.from_pydata(v,[],f);m.update();bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(m);bm.free()
 o=bpy.data.objects.new(n,m);(GROUPS.get(g or CURRENT) or group(g or CURRENT)).objects.link(o);m.materials.append(MATS[mat]);return o
def beam(n,a,b,w=.06,d=.06,mat='Timber',g=None):
 a,b=Vector(a),Vector(b);o=box(n,(a+b)/2,(w,d,(b-a).length),mat,.004,g);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def cylinder(n,p,r,h,mat='Steel',axis=(0,0,1),g=None,segments=20):
 bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=r,depth=h,location=p);o=obj(bpy.context.object,n,mat,g);o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
 for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4
 return o
def ball(n,p,s,mat,g=None):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=1,location=p);o=obj(bpy.context.object,n,mat,g);o.scale=s
 for f in o.data.polygons:f.use_smooth=True
 return o
def tube(n,points,r,mat='Steel',wall=0,g=None,sides=16):
 points=[Vector(p) for p in points];v=[];previous_t=None;previous_a=None
 closed=len(points)>3 and (points[0]-points[-1]).length<1e-7
 for k,p in enumerate(points):
  t=(points[1]-points[-2]).normalized() if closed and k in [0,len(points)-1] else (points[min(k+1,len(points)-1)]-points[max(0,k-1)]).normalized()
  if previous_t is None:
   ref=Vector((0,0,1)) if abs(t.z)<.95 else Vector((0,1,0));a=t.cross(ref).normalized()
  else:
   # Parallel transport prevents a 180-degree frame flip through vertical bends.
   a=previous_t.rotation_difference(t)@previous_a;a=(a-t*a.dot(t)).normalized()
  b=t.cross(a).normalized();previous_t=t;previous_a=a
  for rad in ([r,r-wall] if wall else [r]):
   for i in range(sides):v.append(p+rad*(a*math.cos(i*2*math.pi/sides)+b*math.sin(i*2*math.pi/sides)))
 rings=2 if wall else 1;stride=sides*rings;f=[]
 for k in range(len(points)-1):
  for ring in range(rings):
   for i in range(sides):
    x=k*stride+ring*sides+i;y=k*stride+ring*sides+(i+1)%sides;f.append((x,y,y+stride,x+stride))
 if wall:
  for end in [0,len(points)-1]:
   start=end*stride
   for i in range(sides):f.append((start+i,start+(i+1)%sides,start+sides+(i+1)%sides,start+sides+i))
 else:
  f.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*stride+i for i in range(sides))])
 o=mesh(n,v,f,mat,g)
 for poly in o.data.polygons:poly.use_smooth=True
 return o
def text(n,body,p,size,maxwidth,mat='Paper',g=None):
 bpy.ops.object.select_all(action='DESELECT');curve=bpy.data.curves.new(n,'FONT');curve.body=body;curve.align_x='CENTER';curve.size=size;curve.extrude=.0007
 font=pathlib.Path('C:/Windows/Fonts/arial.ttf')
 if font.exists():curve.font=bpy.data.fonts.load(str(font))
 o=bpy.data.objects.new(n,curve);(GROUPS.get(g or CURRENT) or group(g or CURRENT)).objects.link(o);o.location=p;o.rotation_euler=(math.pi/2,0,0);curve.materials.append(MATS[mat]);bpy.context.view_layer.update()
 if o.dimensions.x>maxwidth:o.scale.x=maxwidth/o.dimensions.x
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False);return o
def uv_packet(c,canonical=False):
 bpy.context.view_layer.update();p=[];n=[];uv=[];mats=[];ix={};lookup={}
 def cv(v):return [-v.x,v.z,-v.y] if canonical else [v.x,v.z,v.y]
 for o in sorted(c.objects,key=lambda x:x.name):
  if o.type!='MESH':continue
  me=o.data;me.calc_loop_triangles();world=o.matrix_world;norm=world.to_3x3().inverted().transposed()
  for tri in me.loop_triangles:
   corners=[Vector(tuple(round(float(v),6) for v in (world@me.vertices[i].co))) for i in tri.vertices]
   # Font tessellation and roof clipping can retain collinear triangles. Remove
   # only zero-area faces from the packet; source geometry remains editable.
   if (corners[1]-corners[0]).cross(corners[2]-corners[0]).length_squared<1e-16:continue
   mat=me.materials[tri.material_index].name
   if mat not in mats:mats.append(mat);ix[mat]=[]
   ids=[]
   for li in tri.loops:
    pos=world@me.vertices[me.loops[li].vertex_index].co;normal=(norm@me.corner_normals[li].vector).normalized()
    axis=max(range(3),key=lambda i:abs(normal[i]));coords=list(me.uv_layers.active.data[li].uv) if o.get('explicit_uv') else [pos[i] for i in range(3) if i!=axis];key=tuple(round(x,6) for x in (*cv(pos),*cv(normal),*coords))
    if key not in lookup:lookup[key]=len(p)//3;p.extend(key[:3]);n.extend(key[3:6]);uv.extend(key[6:])
    ids.append(lookup[key])
   ix[mat].extend(reversed(ids))
 return dict(name=c.name,positions=p,normals=n,uv=uv,materials=mats,submeshes=[dict(indices=ix[m]) for m in mats])
def save_packet(path,data):
 path.parent.mkdir(parents=True,exist_ok=True)
 with path.open('wb') as f:
  with gzip.GzipFile(fileobj=f,mode='wb',mtime=0) as z:z.write(json.dumps(data,separators=(',',':')).encode())
def surfaces():return [dict(name=n,linearRGB=c,metallic=m,roughness=r,texture=t) for n,(c,m,r,t) in PALETTE.items()]
