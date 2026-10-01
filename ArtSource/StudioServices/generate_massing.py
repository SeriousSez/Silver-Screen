"""Studio Services review gate 01. Metres, Blender Z-up, front -Y.

Owns only ArtSource/StudioServices and ArtExports/StudioServices/Massing.
Review geometry is not the final reusable kit. Existing Stage 1 is never loaded/saved.
Run Blender --background --python this_file.py
"""
import bpy, bmesh, json, gzip, math, pathlib, hashlib
from mathutils import Vector

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtExports/StudioServices/Massing'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
bpy.context.preferences.filepaths.save_version = 0
groups = {}
colliders = []
fixtures = []
anchors = []
studies = []
palette = {
    'WarmStucco': ([.53,.445,.335],0,.86),
    'LimestoneTrim': ([.64,.56,.435],0,.8),
    'Brick': ([.28,.115,.063],0,.87),
    'WorkshopPaint': ([.075,.13,.115],0,.72),
    'Timber': ([.235,.145,.070],0,.8),
    'RoofMetal': ([.13,.14,.135],.55,.65),
    'Steel': ([.037,.044,.043],.65,.57),
    'Glass': ([.12,.18,.18],.25,.23),
    'Concrete': ([.31,.29,.25],0,.88),
    'Paper': ([.78,.70,.51],0,.9),
    'FurnishingStudy': ([.26,.235,.185],0,.84),
}
materials = {}
for name,(color,metal,rough) in palette.items():
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    materials[name]=m

def unity(v): return [round(v[0],6),round(v[2],6),round(v[1],6)]
def collection(name):
    if name not in groups:
        c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c
    return groups[name]
def assign(o,name,group,mat):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    collection(group).objects.link(o)
    o.data.materials.append(materials[mat]);return o
def box(name,p,size,group='FrontWall',mat='WarmStucco',bevel=.015,collision=False):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    assign(o,name,group,mat)
    if bevel:
        mod=o.modifiers.new('Construction edge','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if collision: colliders.append(dict(name=name,group=group,position=unity(p),size=unity(size)))
    return o
def beam(name,a,b,width,depth,group,mat):
    a,b=Vector(a),Vector(b);d=b-a
    o=box(name,(a+b)/2,(width,depth,d.length),group,mat,.006)
    o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return o
def mesh(name,verts,faces,group,mat):
    m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(m);bm.free()
    o=bpy.data.objects.new(name,m);collection(group).objects.link(o);m.materials.append(materials[mat]);return o
def cyl(name,p,r,h,group,mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=h,location=p)
    return assign(bpy.context.object,name,group,mat)
def text(name,body,p,height,width,group,mat='Steel'):
    curve=bpy.data.curves.new(name,'FONT');curve.body=body;curve.align_x='CENTER';curve.size=height;curve.extrude=.003
    font=pathlib.Path('C:/Windows/Fonts/timesbd.ttf')
    if font.exists():curve.font=bpy.data.fonts.load(str(font))
    o=bpy.data.objects.new(name,curve);collection(group).objects.link(o)
    o.location=p;o.rotation_euler=(math.pi/2,0,0);o.data.materials.append(materials[mat])
    bpy.context.view_layer.update()
    if o.dimensions.x>width:o.scale.x=width/o.dimensions.x
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.convert(target='MESH');o.select_set(False)

# Flat floor datum: interior surface y=0 in Unity, no hidden step or solid box shell.
box('Foundation floor',(0,0,-.09),(14,9,.18),'Floor','Concrete',.012,True)
box('Employment apron',(-2.6,-5.3,-.08),(9.3,1.6,.16),'ExteriorGround','Concrete',.015,True)
box('Service apron',(3.5,-5.4,-.08),(5.9,1.8,.16),'ExteriorGround','Concrete',.015,True)
box('Yard slab',(9.7,0,-.08),(5.4,9,.16),'ServiceYard','Concrete',.015,True)

# Wall panels around genuine openings. Front signage and pilasters stay in front visibility group.
openings=[(-4.75,-3.55,0,2.62),(-2.50,-.55,.83,2.95),(1.35,6.2,0,3.46)]
def perforated_wall(name,length,height,thick,holes,group,axis,constant):
    xs=sorted(set([-length/2,length/2]+[v for h in holes for v in h[:2]]))
    zs=sorted(set([0,height]+[v for h in holes for v in h[2:]]))
    for i,(x0,x1) in enumerate(zip(xs,xs[1:])):
        for j,(z0,z1) in enumerate(zip(zs,zs[1:])):
            if any(h[0] < (x0+x1)/2 < h[1] and h[2] < (z0+z1)/2 < h[3] for h in holes):continue
            p=((x0+x1)/2,constant,(z0+z1)/2) if axis=='X' else (constant,(x0+x1)/2,(z0+z1)/2)
            s=(x1-x0,thick,z1-z0) if axis=='X' else(thick,x1-x0,z1-z0)
            box(name+'_%02d_%02d'%(i,j),p,s,group,'WarmStucco',0,True)
perforated_wall('Front masonry',14,4.22,.28,openings,'FrontWall','X',-4.36)
perforated_wall('Right masonry',9,4.12,.28,[(-2.45,-.85,0,2.7),(.45,3.1,1.1,3.15)],'RightWall','Y',6.86)
perforated_wall('Left masonry',9,4.12,.28,[(-2.9,-.9,1.0,3.1),(1.5,3.4,1.3,3.1)],'LeftWall','Y',-6.86)
perforated_wall('Rear masonry',14,4.12,.28,[(-5.4,-3.4,1.3,3.1),(.7,3.8,1.15,3.1)],'RearWall','X',4.36)
for x in [-6.78,6.78]:
    box('Brick corner',(x,-4.53,2.15),(.44,.15,4.3),'FrontWall','Brick')
    box('Corner cap',(x,-4.53,4.32),(.56,.27,.14),'FrontWall','LimestoneTrim')
# Restrained stepped parapet, much lower than Stage 1.
for x,w,h in [(-5.6,2.8,.39),(0,8.4,.98),(5.6,2.8,.39)]:
    box('Parapet field',(x,-4.36,4.22+h/2),(w,.30,h),'FrontWall')
    box('Parapet coping',(x,-4.36,4.22+h+.075),(w+.06,.42,.15),'FrontWall','LimestoneTrim')
for x in [-4.2,4.2]:
    box('Parapet step',(x,-4.36,4.82),(.18,.40,.74),'FrontWall','LimestoneTrim')
for z,h,depth in [(3.91,.13,.39),(4.14,.095,.37)]:
    box('Facade stringcourse',(0,-4.50,z),(14.15,depth,h),'FrontWall','LimestoneTrim')
text('Services title','STUDIO SERVICES',(0,-4.534,4.63),.62,8.0,'FrontWall')
text('Service descriptor','MAINTENANCE  •  CONSTRUCTION  •  GROUNDS',(0,-4.54,4.30),.22,7.8,'FrontWall')
# Main roof: low hipped utility roof behind facade, not a barrel vault.
verts=[(-6.95,-4.1,4.18),(6.95,-4.1,4.18),(6.95,4.55,4.18),(-6.95,4.55,4.18),(-2.8,.2,5.15),(2.8,.2,5.15)]
roof=mesh('Low hipped roof',verts,[(0,1,5,4),(1,2,5),(2,3,4,5),(3,0,4)],'Roof','RoofMetal')
sol=roof.modifiers.new('Roof thickness','SOLIDIFY');sol.thickness=.10
bpy.context.view_layer.objects.active=roof;bpy.ops.object.modifier_apply(modifier=sol.name)
# Roof outline, ridge and simple skylight volumes communicate silhouette only.
for a,b in [(0,1),(1,2),(2,3),(3,0),(0,4),(1,5),(2,5),(3,4),(4,5)]:
    beam('Roof edge',verts[a],verts[b],.08,.06,'Roof','Steel')
for x in [-3.4,2.5]:
    o=box('Skylight study',(x,-1.6,4.79),(2.15,1.15,.07),'Roof','Glass',.01)
    o.rotation_euler.x=math.atan(.97/4.3)
    for dx in [-1.1,0,1.1]:
        beam('Skylight framing',(x+dx,-2.175,4.685),(x+dx,-1.025,4.945),.055,.055,'Roof','Steel')

# Clear office/workshop/storage partitions with 1.25 m through openings.
perforated_wall('Office workshop partition',8.44,3.30,.16,[(-1.2,.1,0,2.5)],'InteriorPartitions','Y',-.25)
# Left rear storage room, doorway at x=-4.9 ... -3.6.
for x,w in [(-5.87,1.98),(-1.94,3.12)]:
    box('Office store partition',(x,.30,1.65),(w,.16,3.3),'InteriorPartitions','WarmStucco',.01,True)
box('Store doorway header',(-4.25,.30,2.91),(1.3,.16,.78),'InteriorPartitions','WarmStucco',.01,True)

# Openable items are their own components. Review pose: office leaf held inward.
def front_window(cx,y,z,w,h,group):
    for dx in [-w/2,w/2]:box('Window jamb',(cx+dx,y,z),( .095,.12,h+.1),group,'Steel',.006)
    for dz in [-h/2,h/2]:box('Window rail',(cx,y,z+dz),(w,.12,.095),group,'Steel',.006)
    for i in [-1,0,1]:box('Window mullion',(cx+i*w/4,y,z),(.034,.065,h),group,'Steel',.004)
    for dz in [-h/6,h/6]:box('Window transom',(cx,y,z+dz),(w,.065,.034),group,'Steel',.004)
    # Real aperture; glazing is a separate shallow pane.
    box('Window glazing',(cx,y+.025,z),(w-.08,.012,h-.08),group,'Glass',0)
    box('Window sill',(cx,y-.07,z-h/2-.065),(w+.24,.35,.13),group,'LimestoneTrim')
front_window(-1.525,-4.47,1.89,1.95,2.12,'FrontWall')
front_window(-4.4,4.48,2.2,2,1.8,'RearWall')
front_window(2.25,4.48,2.125,3.1,1.95,'RearWall')
# Side windows authored by rotating front-window subassemblies about their own centres.
for x,cy,w,h,z,g in [(6.98,1.775,2.65,2.05,2.125,'RightWall'),(-6.98,-1.9,2,2.1,2.05,'LeftWall'),(-6.98,2.45,1.9,1.8,2.2,'LeftWall')]:
    before=set(bpy.data.objects);front_window(0,0,z,w,h,g)
    for o in set(bpy.data.objects)-before:
        px,py=o.location.x,o.location.y;o.location.x=x-py;o.location.y=cy+px;o.rotation_euler.z=math.pi/2
for x in [-4.81,-3.49]:box('Office door jamb',(x,-4.48,1.32),(.12,.20,2.64),'FrontWall','Timber')
box('Office door head',(-4.15,-4.48,2.66),(1.45,.22,.16),'FrontWall','LimestoneTrim')
o=box('Office leaf 55mm open',(-4.65,-3.91,1.29),(.055,1.1,2.58),'OfficeDoor','WorkshopPaint',.012,True)
for z in [.65,1.9]:box('Office leaf inset',(-4.612,-3.91,z),(.025,.88,.9),'OfficeDoor','Timber',.008)
# Workshop pair; leaves 60mm, upper glazing. Design study omits fine hardware.
for cx in [2.565,4.985]:
    box('Workshop lower leaf',(cx,-4.48,1.13),(2.385,.06,2.24),'WorkshopDoors','WorkshopPaint',.014,True)
    for dx in [-1.14,1.14]:box('Workshop stile',(cx+dx,-4.51,1.73),(.10,.12,3.4),'WorkshopDoors','WorkshopPaint')
    for z in [2.29,3.38]:box('Workshop transom',(cx,-4.51,z),(2.38,.12,.11),'WorkshopDoors','WorkshopPaint')
    front_window(cx,-4.5,2.84,2.14,.92,'WorkshopDoors')
    a,b=(cx-1.08,cx+1.08) if cx<3.7 else (cx+1.08,cx-1.08)
    beam('Workshop diagonal',(a,-4.535,.17),(b,-4.535,2.2),.09,.08,'WorkshopDoors','Timber')
for x in [1.24,6.31]:box('Workshop jamb',(x,-4.52,1.75),(.18,.28,3.5),'FrontWall','Brick')
box('Workshop header',(3.775,-4.51,3.57),(5.24,.30,.21),'FrontWall','LimestoneTrim')
# Side service doorway is held open along inner wall, no invisible closed door.
box('Yard service leaf 60mm',(6.6,-.1,1.3),(.06,1.5,2.6),'YardDoor','WorkshopPaint',.01,True)

# Employment porch and green arched office awning are intentionally modest.
for x in [-6.75,-4.97]:
    box('Employment porch post',(x,-6.1,1.38),(.12,.12,2.76),'EmploymentCanopy','Timber',.015,True)
    beam('Porch knee brace',(x,-6.1,2.08),(x,-5.5,2.75),.09,.09,'EmploymentCanopy','Timber')
beam('Porch front beam',(-6.87,-6.13,2.76),(-4.84,-6.13,2.76),.15,.15,'EmploymentCanopy','Timber')
o=box('Porch roof',(-5.86,-5.28,2.99),(2.32,1.94,.10),'EmploymentCanopy','RoofMetal');o.rotation_euler.x=.12
for x in [-6.7,-6.0,-5.3,-4.9]:beam('Porch rafter',(x,-6.2,2.8),(x,-4.45,3.01),.08,.10,'EmploymentCanopy','Timber')
awning_verts=[]
for x in [-4.92,-3.28]:
    for i in range(9):
        t=i*math.pi/16
        awning_verts.append((x,-4.45-.93*math.sin(t),2.73+.48*math.cos(t)))
awning=mesh('Curved office awning',awning_verts,[(i,i+1,i+10,i+9) for i in range(8)],'OfficeAwning','WorkshopPaint')
mod=awning.modifiers.new('Canopy sheet','SOLIDIFY');mod.thickness=.045
bpy.context.view_layer.objects.active=awning;bpy.ops.object.modifier_apply(modifier=mod.name)
box('Employment board',(-5.88,-4.58,1.68),(1.73,.12,1.15),'EmploymentBoard','Timber')
box('Notice surface',(-5.88,-4.652,1.57),(1.5,.025,.75),'EmploymentBoard','Paper',0)
text('Employment heading','EMPLOYMENT',(-5.88,-4.67,2.045),.16,1.50,'EmploymentBoard')

# Attached open-sided workshop shelter; six usable bays and a wide material aisle.
for y in [-3.6,.3,4.15]:
    box('Yard shelter post',(10.45,y,1.45),(.17,.17,2.90),'YardShelter','Timber',.014,True)
    beam('Shelter rafter',(6.98,y,3.55),(10.6,y,2.89),.15,.18,'YardShelter','Timber')
    beam('Shelter knee brace',(10.45,y,2.05),(9.6,y,3.06),.12,.12,'YardShelter','Timber')
beam('Shelter outer beam',(10.45,-3.8,2.88),(10.45,4.35,2.88),.19,.19,'YardShelter','Timber')
roof=box('Shelter metal roof',(8.83,.3,3.245),(3.98,8.45,.08),'YardShelterRoof','RoofMetal');roof.rotation_euler.y=.18
for y in [-4.3,0,4.35]:cyl('Yard fence post',(12.35,y,1),.048,2,'ServiceYard','Steel')
for a,b in [((12.35,-4.3,1.8),(12.35,4.35,1.8)),((12.35,-4.3,.2),(12.35,4.35,.2)),((7,4.35,1.8),(12.35,4.35,1.8)),((10.6,-4.3,1.8),(12.35,-4.3,1.8))]:
    beam('Fence rail',a,b,.048,.048,'ServiceYard','Steel')

# Furniture envelopes are separate study components, deliberately not final canonical props.
def furniture(name,p,size):
    box(name,p,size,'InteriorProps','FurnishingStudy',.025,True)
    studies.append(dict(name=name,position=unity(p),dimensions=unity(size),status='Furnishing envelope only; canonical asset after visual gate'))
furniture('Office desk',(-5.4,-1.45,.39),(2.1,.85,.78))
furniture('Office chair',(-5.4,-.65,.48),(.55,.58,.96))
furniture('Filing cabinet',(-6.38,-.25,.70),(.65,.6,1.4))
furniture('Office visitor seat',(-2.1,-.6,.48),(.60,.58,.96))
furniture('Workshop bench',(5.75,2.75,.46),(1.4,2.75,.92))
furniture('Workshop tool cabinet',(.6,3.7,.90),(1.3,.75,1.8))
for x in [-6.25,-1.13]:furniture('Supply shelving',(x,2.42,1.05),(.68,3.30,2.1))
furniture('Supply centre rack',(-3.8,2.9,.65),(1.3,1.3,1.3))
box('Yard workbench',(8.6,3.52,.47),(2.25,.8,.94),'YardProps','FurnishingStudy',.02,True)
box('Lumber rack',(11.55,1.2,.85),(1.1,4.1,1.7),'YardProps','Timber',.01,True)
box('Bench seat',(-1.65,-4.94,.48),(2.1,.53,.08),'ExteriorProps','Timber')
for x in [-2.5,-.8]:box('Bench support',(x,-4.93,.24),(.10,.5,.48),'ExteriorProps','Steel')
box('Bench back',(-1.65,-4.70,.79),(2.1,.07,.57),'ExteriorProps','Timber')

# Existing approved canonical fixtures are instantiated by Unity, never copied into this source.
for x,y,z,yaw in [(-4.15,-4.54,3.36,180),(-.05,-4.54,3.37,180),(-3.0,-4.54,5.40,180),(0,-4.54,5.40,180),(3.0,-4.54,5.40,180)]:
    fixtures.append(dict(asset='GooseneckLamp_1930',group='FrontWall',position=[x,z,y],yaw=yaw))
for x,y,z in [(-3.6,-1.7,3.15),(3,-1.7,3.70),(3,2.1,3.70),(-3.7,2.3,3.15)]:
    fixtures.append(dict(asset='PendantWorkLamp_1930',group='InteriorLighting',position=[x,z,y],yaw=0))
def anchor(name,x,y,yaw=0):anchors.append(dict(name=name,position=[x,0,y],yaw=yaw))
anchor('OfficeEntrance',-4.15,-4.32);anchor('OfficeApproach',-4.15,-5.35)
anchor('WorkshopEntrance',3.775,-3.80);anchor('WorkshopApproach',3.775,-5.50)
anchor('YardAccess',6.86,-1.65,270)
anchor('ApplicantArrival',-7.5,-7.4);anchor('ApplicantExit',-3.0,-7.4)
for i,(x,y) in enumerate([(-6.45,-5.20),(-5.40,-5.25),(-6.45,-6.90),(-5.10,-6.90),(-3.40,-6.45)]):anchor('ApplicantWaiting_%02d'%i,x,y)
anchor('HireConstructionWorker',-5.40,-2.75);anchor('HireGroundskeeper',-2.5,-2.60)
anchor('ConstructionMaterialPickup',-2.60,2.85,270);anchor('ToolPickup',.7,2.85)
anchor('WorkshopWork',4.35,2.75,90);anchor('GroundskeeperSupply',-5.0,1.65,270)
anchor('ApplicantInspection',-5.8,-7.7)

# Human scale studies are separate review-only objects; never building architecture.
for p in [(-3.1,-5.65),(3.7,-5.7),(8.5,-2.3)]:
    for dx in [-.13,.13]:
        box('Human scale leg',(p[0]+dx,p[1],.43),(.16,.22,.86),'ScaleStudies','Steel',.065)
        box('Human scale foot',(p[0]+dx,p[1]-.04,.05),(.18,.32,.10),'ScaleStudies','Steel',.03)
    box('Human scale torso',(p[0],p[1],1.17),(.46,.28,.62),'ScaleStudies','Steel',.09)
    for side in [-1,1]:beam('Human scale arm',(p[0]+side*.25,p[1],1.40),(p[0]+side*.33,p[1],.87),.13,.14,'ScaleStudies','Steel')
    cyl('Human scale neck',(p[0],p[1],1.49),.07,.10,'ScaleStudies','Steel')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.135,location=(p[0],p[1],1.615))
    assign(bpy.context.object,'Human scale head','ScaleStudies','Steel')

def packet(coll):
    positions=[];normals=[];uv=[];mats=[];triangles={}
    for o in sorted(coll.objects,key=lambda o:o.name):
        if o.type!='MESH':continue
        m=o.data;m.calc_loop_triangles();world=o.matrix_world;normal=world.to_3x3().inverted().transposed()
        for tri in m.loop_triangles:
            mat=m.materials[tri.material_index].name
            if mat not in mats:mats.append(mat);triangles[mat]=[]
            ids=[]
            for loop in tri.loops:
                p=world@m.vertices[m.loops[loop].vertex_index].co;n=(normal@m.corner_normals[loop].vector).normalized()
                ids.append(len(positions)//3);positions.extend(unity(p));normals.extend(unity(n))
                axis=max(range(3),key=lambda i:abs(n[i]));axes=[i for i in range(3) if i!=axis]
                uv.extend([round(p[i]/2,6) for i in axes])
            triangles[mat].extend(reversed(ids))
    return dict(name=coll.name,positions=positions,normals=normals,uv=uv,materials=mats,submeshes=[dict(indices=triangles[m]) for m in mats])
bpy.context.view_layer.update()
packets=[packet(c) for c in groups.values()]
data=dict(schema=1,phase='MASSING_REVIEW_NOT_PRODUCTION',parts=packets,fixtures=fixtures,anchors=anchors,colliders=colliders,
          materials=[dict(name=n,linearRGB=c,metallic=m,roughness=r) for n,(c,m,r) in palette.items()],
          studies=studies,buildingWidth=14,buildingDepth=9,eavesHeight=4.22,parapetHeight=5.35,roofRidgeHeight=5.15,
          yardBounds=[7,-4.5,12.4,4.5],reservedRect=[2.7,-1.3,20.6,13.6],
          cutaway=dict(roof=['Roof','EmploymentCanopy','OfficeAwning','YardShelterRoof'],walls=['FrontWall','RightWall','RearWall','LeftWall'],retain=['Floor','InteriorPartitions','InteriorProps','ServiceYard']))
with open(OUT/'massing_meshes.json.gz','wb') as f:
    with gzip.GzipFile(fileobj=f,mode='wb',mtime=0) as z:z.write(json.dumps(data,separators=(',',':')).encode())
report={k:v for k,v in data.items() if k!='parts'}
report['parts']=[dict(name=p['name'],vertices=len(p['positions'])//3,triangles=sum(len(s['indices'])//3 for s in p['submeshes'])) for p in packets]
report['generatorSha256']=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()
(OUT/'massing_manifest.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/StudioServices/StudioServices_Massing.blend'))
print('STUDIO_SERVICES_MASSING',len(groups),'visibility groups;',len(anchors),'anchors;',sum(p['triangles'] for p in report['parts']),'triangles')
