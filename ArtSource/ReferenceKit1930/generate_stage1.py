"""1930 compact barrel-roof Stage 1. Blender 5 CLI; metres, no other exports.
Production footprint: 15.8 x 24 m. Architectural interior only.
"""
import bpy, bmesh, math, json, shutil, re
from pathlib import Path
from mathutils import Vector, Matrix

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
NAME='StudioSoundStage_1930'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
with bpy.data.libraries.load(str(HERE/'StudioAdministration_01.blend'),link=False) as (src,dst):
    dst.materials=[n for n in src.materials if n.startswith('ADM_')]
materials={m.name:m for m in bpy.data.materials}
definitions={}
def material(name,color,rough,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    materials[name]=m;definitions[name]={'linear':color,'roughness':rough,'metallic':metal}
material('STG_Foundation',(.25,.235,.205),.84)
material('STG_ZincRoof',(.27,.265,.245),.52,.72)
material('STG_DarkDoors',(.045,.039,.03),.58,.12)
material('STG_ConcreteFloor',(.20,.19,.17),.83)
material('STG_BlackoutRoof',(.085,.105,.115),.64,.35)
groups={};objects=[]
def finish(o,name,mat,group):
    o.name=name;o.data.materials.append(materials[mat]);objects.append(o);groups.setdefault(group,[]).append(o)
    bpy.context.view_layer.objects.active=o
    o.data.transform(Matrix.Diagonal((*o.scale,1)))
    o.scale=(1,1,1)
    return o
def box(name,pos,size,mat='ADM_Stucco',group='ExteriorShell'):
    x,y,z=[v/2 for v in size]
    data=bpy.data.meshes.new(name)
    data.from_pydata([(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)],[],
                    [(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
    data.update();o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=pos
    return finish(o,name,mat,group)
def rod(name,a,b,r=.025,mat='ADM_WindowSteel',group='Structure'):
    delta=Vector(b)-Vector(a)
    if delta.length<.000001:return None
    n=4 if name=='RoofStandingSeam' else 8
    vv=[(r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z) for z in (-delta.length/2,delta.length/2) for i in range(n)]
    ff=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(reversed(range(n))),tuple(range(n,2*n))]
    data=bpy.data.meshes.new(name);data.from_pydata(vv,[],ff);data.update()
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=(Vector(a)+Vector(b))/2
    o.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,group)
def mesh(name,verts,faces,mat,group):
    m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update()
    o=bpy.data.objects.new(name,m);scene.collection.objects.link(o)
    return finish(o,name,mat,group)
def wall(name,width,origin,angle,openings):
    # Perforated masonry, not glazing pasted onto a closed wall.
    xs=sorted(set([-width/2,width/2]+[v for u,z,w,h in openings for v in (u-w/2,u+w/2)]))
    zs=sorted(set([0,7.6]+[v for u,z,w,h in openings for v in (z-h/2,z+h/2)]))
    for a,b in zip(xs,xs[1:]):
        for c,d in zip(zs,zs[1:]):
            u,z=(a+b)/2,(c+d)/2
            if any(abs(u-p)<w/2 and abs(z-q)<h/2 for p,q,w,h in openings):continue
            o=box(name,(origin[0]+u*math.cos(angle),origin[1]+u*math.sin(angle),z),(b-a,.30,d-c))
            o.rotation_euler.z=angle
def frame(name,u,z,w,h,origin,angle):
    def f(label,x,depth,zz,ww,th,hh,mat='ADM_WindowSteel',grp='Windows'):
        o=box(name+label,(origin[0]+x*math.cos(angle)-depth*math.sin(angle),origin[1]+x*math.sin(angle)+depth*math.cos(angle),zz),(ww,th,hh),mat,grp);o.rotation_euler.z=angle;return o
    for side in (-1,1):
        f('Jamb',u+side*(w/2-.04),0,z,.08,.20,h)
        f('Rail',u,0,z+side*(h/2-.04),w,.20,.08)
    f('Glazing',u,.025,z,w-.10,.015,h-.10,'ADM_ClearGlass','Glass')
    for x in range(1,6):f('Mullion',u-w/2+x*w/6,-.04,z,.035,.08,h-.08)
    f('Midrail',u,-.04,z,w-.08,.08,.035)
    f('Sill',u,-.03,z-h/2-.05,w+.22,.40,.10,'ADM_Limestone','ArchitecturalDetails')
def door(name,x,y,w,h,angle=0,group=None):
    group=group or name
    def p(u,d,z):return (x+u*math.cos(angle)-d*math.sin(angle),y+u*math.sin(angle)+d*math.cos(angle),z)
    def f(label,u,d,z,ww,t,hh,mat='STG_DarkDoors',grp=group):
        o=box(name+label,p(u,d,z),(ww,t,hh),mat,grp);o.rotation_euler.z=angle
    f('Leaf',0,0,h/2+.025,w-.04,.13,h-.05)
    for u in (-w/2,w/2):f('Jamb',u,0,h/2,.09,.27,h+.08,'ADM_WindowSteel','DoorFrames')
    f('Header',0,0,h+.055,w+.18,.27,.11,'ADM_WindowSteel','DoorFrames')
    f('Threshold',0,0,.025,w,.35,.05,'STG_Foundation','DoorFrames')
    for z in (.35,h-.35):
        rod(name+'Hinge',p(-w/2,-.10,z-.07),p(-w/2,-.10,z+.07),.024,group=group)
        f('HingePlate',-w/2+.045,-.075,z,.16,.02,.09,'ADM_WindowSteel')
    f('HandlePlate',w/2-.16,-.078,1.02,.055,.025,.20,'ADM_Bronze')
    rod(name+'Handle',p(w/2-.16,-.105,1.02),p(w/2-.29,-.105,1.02),.012,'ADM_Bronze',group)
    rod(name+'HandleSpindle',p(w/2-.16,-.075,1.02),p(w/2-.16,-.105,1.02),.018,'ADM_Bronze',group)
    return p
def lamp(name,x,y,z,angle=0):
    def p(u,d,zz):return(x+u*math.cos(angle)-d*math.sin(angle),y+u*math.sin(angle)+d*math.cos(angle),zz)
    o=box(name+'Mount',p(0,-.025,z),(.15,.08,.30),'ADM_WindowSteel','Utilities');o.rotation_euler.z=angle
    points=[p(0,-.06,z+.10),p(0,-.15,z+.35),p(0,-.40,z+.38),p(0,-.62,z+.23),p(0,-.65,z+.02)]
    for a,b in zip(points,points[1:]):rod(name+'Gooseneck',a,b,.022,group='Utilities')
    bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.23,radius2=.075,depth=.17,location=p(0,-.65,z-.05));finish(bpy.context.object,name+'Shade','ADM_WindowSteel','Utilities')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.07,location=p(0,-.65,z-.14));finish(bpy.context.object,name+'Bulb','ADM_LampDiffuser','Utilities')

# Production proportions; Unity access points adapt to this architecture.
wall('Front',15.8,(0,-5.5),0,[(0,3,5.6,6),(-5.8,1.075,1,2.15),(5.8,1.075,1,2.15)])
wall('Rear',15.8,(0,5.5),0,[(0,1.075,1,2.15)])
for side in (-1,1):
    wall('Side',24,(side*7.9,0),math.pi/2,[(u,6.2,5.2,1.15) for u in (-8,0,8)])
    for u in (-8,0,8):frame('Clerestory',u,6.2,5.2,1.15,(side*7.9,0),math.pi/2)
    for y in (-11.8,-4,4,11.8):box('Buttress',(side*7.94,y,3.7),(.38,.46,7.4),'ADM_Stucco','ArchitecturalDetails')
box('ClearFilmingFloor',(0,0,-.07),(15.8,11,.14),'STG_ConcreteFloor','Floor')
for x in (-7.91,7.91):box('BaseSide',(x,0,.35),(.34,11.05,.70),'STG_Foundation','Foundation')
for a,b in ((-7.9,-6.3),(-5.3,-2.8),(2.8,5.3),(6.3,7.9)):
    box('BaseFront',((a+b)/2,-5.51,.35),(b-a,.34,.70),'STG_Foundation','Foundation')
for a,b in ((-7.9,-.5),(.5,7.9)):box('BaseRear',((a+b)/2,5.51,.35),(b-a,.34,.70),'STG_Foundation','Foundation')
door('PersonnelFront',-5.8,-5.5,1,2.15)
door('PersonnelRear',0,5.5,1,2.15,math.pi)
for x,y,ang in ((-5.8,-5.5,0),(4.8,5.5,math.pi)):
    o=box('PersonnelCanopy',(x,y+(-.38 if y<0 else .38),2.40),(1.4,.9,.10),'ADM_WindowSteel','ArchitecturalDetails')
    lamp('ServiceLight',x-1.0,y,2.1,ang)
# Large hinged double doors, distinct pivots retained in exported groups.
for side in (-1,1):
    grp='StageDoorLeft' if side<0 else 'StageDoorRight'
    cx=side*1.4
    box('MainDoorLeaf',(cx,-5.53,3),(2.78,.22,5.96),'STG_DarkDoors',grp)
    for x in range(11):box('DoorBoard',(cx-1.27+x*.254,-5.65,3),(.018,.018,5.75),'ADM_WindowSteel',grp)
    for z in (.20,2.15,4.10,5.80):box('DoorCrossRail',(cx,-5.685,z),(2.70,.065,.13),'ADM_WindowSteel',grp)
    rod('DoorBrace',(cx-1.25,-5.73,.30),(cx+1.25,-5.73,2.10),.045,group=grp)
    for z in (.45,2.25,4.05,5.55):
        rod('DoorHingePin',(side*2.80,-5.68,z-.12),(side*2.80,-5.68,z+.12),.055,group=grp)
        box('HingeStrap',(side*2.53,-5.70,z),(.65,.05,.11),'ADM_WindowSteel',grp)
    hx=side*.18
    for z in (.95,1.25):rod('PullMount',(hx,-5.65,z),(hx,-5.83,z),.022,'ADM_Bronze',grp)
    rod('DoorPull',(hx,-5.83,.95),(hx,-5.83,1.25),.018,'ADM_Bronze',grp)
    box('MainJamb',(side*2.89,-5.52,3.05),(.18,.40,6.1),'ADM_Limestone','DoorFrames')
box('StageDoorLintel',(0,-5.52,6.17),(6.00,.45,.24),'ADM_Limestone','DoorFrames')
for x in (-4.1,4.1):lamp('StageLight',x,-5.66,2.95)
# Elliptical barrel roof with thickness, arched gable end walls and seams.
segments=32
def arch(t,inset=0):return ((7.96-inset)*math.cos(t),7.6+(3.05-inset)*math.sin(t))
verts=[]
for y in (-5.66,5.66):
    for inset in (0,.16):
        for i in range(segments+1):
            x,z=arch(math.pi*i/segments,inset);verts.append((x,y,z))
k=segments+1;faces=[]
for i in range(segments):
    faces.extend([(i,i+1,2*k+i+1,2*k+i),(k+i,3*k+i,3*k+i+1,k+i+1),(i,k+i,k+i+1,i+1),(2*k+i,2*k+i+1,3*k+i+1,3*k+i)])
faces.extend([(0,2*k,3*k,k),(segments,k+segments,3*k+segments,2*k+segments)])
mesh('BarrelRoof',verts,faces,'STG_ZincRoof','Roof')
for y in (-5.51,5.51):
    verts=[(0,y,7.6)]+[(7.85*math.cos(math.pi*i/segments),y,7.6+2.95*math.sin(math.pi*i/segments)) for i in range(segments+1)]
    o=mesh('ArchedGable',verts,[(0,i+1,i+2) for i in range(segments)],'ADM_Stucco','ExteriorShell')
    mod=o.modifiers.new('Masonry thickness','SOLIDIFY');mod.thickness=.28
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
for y in [-12.16+i*.76 for i in range(33)]:
    for i in range(segments):
        x,z=arch(math.pi*i/segments);xx,zz=arch(math.pi*(i+1)/segments)
        rod('RoofStandingSeam',(x,y,z+.015),(xx,y,zz+.015),.018,'STG_ZincRoof','Roof')
for y in (-5.72,5.72):
    for i in range(segments):
        x,z=arch(math.pi*i/segments);xx,zz=arch(math.pi*(i+1)/segments)
        rod('GableRim',(x,y,z),(xx,y,zz),.065,'ADM_Limestone','ArchitecturalDetails')
for y in (-2.8,2.8):
    box('VentCurb',(0,y,10.63),(1.1,.9,.40),'STG_Foundation','Roof')
    for z in (10.85,11.02,11.19):box('VentLouvre',(0,y,z),(1.3,1.1,.06),'ADM_WindowSteel','Roof')
    box('VentCore',(0,y,10.99),(.75,.65,.35),'ADM_WindowSteel','Roof')
    box('VentCap',(0,y,11.30),(1.4,1.2,.10),'STG_ZincRoof','Roof')
# Clear-span trusses at 3.5m intervals; no posts on the filming floor.
for y in (-10.7,-5.35,0,5.35,10.7):
    rod('TrussTie',(-7.7,y,7.35),(7.7,y,7.35),.065,group='Structure')
    for i in range(16):
        t=math.pi*i/16;tt=math.pi*(i+1)/16
        a=(7.7*math.cos(t),y,7.6+2.75*math.sin(t));b=(7.7*math.cos(tt),y,7.6+2.75*math.sin(tt))
        rod('TrussTopChord',a,b,.065,group='Structure')
        rod('TrussWeb',(a[0],y,7.35),b,.027,group='Structure')
    for side in (-1,1):box('WallColumn',(side*7.6,y,3.7),(.18,.18,7.4),'ADM_WindowSteel','Structure')
for x in (-4,-2,0,2,4):rod('RiggingLongitudinal',(x,-10.7,7.35),(x,10.7,7.35),.033,group='Rigging')
# Side catwalk, rear crossover, accessible stair, all at 4.5m.
box('SideCatwalk',(-7.05,0,4.43),(1.15,9,.14),'ADM_WindowSteel','Catwalk')
box('RearCatwalk',(0,4.2,4.43),(15.1,1.0,.14),'ADM_WindowSteel','Catwalk')
def guard(a,b):
    a,b=Vector(a),Vector(b);length=(b-a).length
    for h in (.55,1.1):rod('GuardRail',a+Vector((0,0,h)),b+Vector((0,0,h)),.023,group='Catwalk')
    for i in range(max(1,math.ceil(length/1.5))+1):
        p=a+(b-a)*i/max(1,math.ceil(length/1.5));rod('GuardPost',p,p+Vector((0,0,1.1)),.026,group='Catwalk')
guard((-6.47,-4.5,4.5),(-6.47,3.7,4.5))
guard((-7.6,-4.5,4.5),(-6.47,-4.5,4.5))
guard((-5.3,3.7,4.5),(7.55,3.7,4.5))
guard((7.55,3.7,4.5),(7.55,4.7,4.5))
# Stair runs parallel to side catwalk; its top opens into rear crossover.
for i in range(24):box('StairTread',(-5.88,-3.35+i*.30,(i+1)*.1875-.035),(1.12,.30,.07),'ADM_WindowSteel','Stair')
for x in (-6.43,-5.33):
    rod('StairStringer',(x,-3.50,0),(x,3.7,4.5),.06,group='Stair')
    rod('StairHandrail',(x,-3.50,1.1),(x,3.7,5.6),.024,group='Stair')
    for i in range(0,25,4):
        y=-3.5+i*.3;z=i*.1875;rod('StairPost',(x,y,z),(x,y,z+1.1),.026,group='Stair')
# Minimal perimeter service infrastructure, no permanent sets or furniture.
for x in (5.8,6.6):box('ElectricalCabinet',(x,5.25,1.1),(.55,.30,1.5),'ADM_WindowSteel','Utilities')
for x in (5.8,6.6):rod('ServiceConduit',(x,5.06,1.85),(x,5.06,6.9),.018,group='Utilities')
for x in (-7.5,7.5):
    rod('Downpipe',(x,-5.78,.15),(x,-5.78,7.55),.045,group='Utilities')
    for z in (1,3.5,6):box('PipeBracket',(x,-5.66,z),(.16,.25,.065),'ADM_WindowSteel','Utilities')
# Continuous facade. Invisible signage anchor and dynamic lettering belong in Unity.

# Move end assemblies without stretching doors or human-scale hardware.
end_names=('Front','Rear','BaseFront','BaseRear','Personnel','ServiceLight',
           'MainDoor','DoorBoard','DoorCross','DoorBrace','DoorHinge','HingeStrap',
           'PullMount','DoorPull','MainJamb','StageDoorLintel','StageLight',
           'ArchedGable','GableRim','Downpipe','PipeBracket','StageSignMount',
           'ElectricalCabinet','ServiceConduit')
for o in objects:
    if o.name.startswith('ArchedGable'):
        for v in o.data.vertices:v.co.y += 6.5 if v.co.y>0 else -6.5
    elif o.name.startswith(end_names):o.location.y += 6.5 if o.location.y>0 else -6.5
    if o.name.startswith(('ClearFilmingFloor','BaseSide','SideCatwalk')):
        o.scale.y=(24/11 if not o.name.startswith('SideCatwalk') else 22/9)
    if o.name.startswith('BarrelRoof'):
        for v in o.data.vertices:v.co.y += 6.5 if v.co.y>0 else -6.5
    if o.name.startswith('Stair'):o.location.y+=6.5
for o in list(groups['Catwalk']):
    if o.name.startswith('Guard'):
        groups['Catwalk'].remove(o);objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
guard((-6.47,-11,4.5),(-6.47,10.2,4.5))
guard((-7.6,-11,4.5),(-6.47,-11,4.5))
guard((-5.3,10.2,4.5),(7.55,10.2,4.5))
guard((7.55,10.2,4.5),(7.55,11.2,4.5))
# Brackets tie access decks back into the structure rather than floating.
for y in (-10.7,-5.35,0,5.35,10.7):
    rod('CatwalkBracket',(-7.6,y,3.55),(-6.47,y,4.36),.055,group='Catwalk')
    box('CatwalkBearer',(-7.03,y,4.33),(1.3,.10,.12),'ADM_WindowSteel','Catwalk')
for x in (-7,0,7):
    rod('RearDeckBracket',(x,11.85,3.55),(x,10.2,4.36),.055,group='Catwalk')
    box('RearDeckBearer',(x,11.05,4.33),(.12,1.7,.12),'ADM_WindowSteel','Catwalk')
# Metre-based UVs and semantic export groups; clean source remains editable.
def remove_parts(prefixes):
    for o in list(objects):
        if o.name.startswith(prefixes):
            objects.remove(o)
            for parts in groups.values():
                if o in parts:parts.remove(o)
            bpy.data.objects.remove(o,do_unlink=True)

# Rebuild the rejected prototype detailing, not an overlay on top of it.
remove_parts(('MainDoor','DoorBoard','DoorCrossRail','DoorBrace','DoorHinge',
              'HingeStrap','PullMount','DoorPull','MainJamb','StageDoorLintel',
              'StageLight','ServiceLight','PersonnelCanopy','GableRim',
              'ElectricalCabinet','ServiceConduit','Downpipe','PipeBracket',
              'VentCurb','VentLouvre','VentCore','VentCap'))
for o in objects:
    if o.name.startswith('ArchedGable'):
        for v in o.data.vertices:v.co.y-=.14

def detail(name,pos,size,mat='ADM_Limestone',group='ArchitecturalDetails',bevel=.008):
    o=box(name,pos,size,mat,group)
    if bevel:
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.bevel(bm,geom=list(bm.edges),offset=bevel,segments=1,affect='EDGES',clamp_overlap=True)
        bm.to_mesh(o.data);bm.free();o.data.update()
    return o

def strip(name,a,b,width,depth,mat='ADM_WindowSteel',group='ArchitecturalDetails'):
    a,b=Vector(a),Vector(b);delta=b-a
    o=box(name,(a+b)/2,(width,depth,delta.length),mat,group)
    o.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return o

def bolt(name,x,y,z,group='Utilities'):
    rod(name,(x,y,z),(x,y-.009,z),.012,'ADM_WindowSteel',group)

# Arched masonry coping: a continuous shaped band, not a round pipe.
for y in (-12.22,12.22):
    vv=[]
    for inset in (0,.22):
        for i in range(49):
            t=math.pi*i/48
            vv.append(((8.02-inset)*math.cos(t),y,7.6+(3.11-inset)*math.sin(t)))
    o=mesh('ArchedCoping',vv,[(i,i+1,50+i,49+i) for i in range(48)],'ADM_Limestone','ArchitecturalDetails')
    mod=o.modifiers.new('Coping depth','SOLIDIFY');mod.thickness=.34
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    for x in (-7.64,7.64):
        detail('CornerPier',(x,y,3.8),(.52,.56,7.6),'ADM_Stucco')
        detail('PierBase',(x,y,.47),(.60,.62,.94),'STG_Foundation')
        detail('PierCapital',(x,y,7.46),(.70,.70,.18))
        detail('PierCap',(x,y,7.64),(.76,.76,.14))
for side in (-1,1):
    detail('EaveBelt',(side*7.99,0,7.35),(.28,23.7,.20))
    detail('EaveDrip',(side*8.04,0,7.48),(.35,23.9,.07))
    for y in (-4,4):
        detail('BayPierCap',(side*7.97,y,7.2),(.46,.66,.14))
    # Recess trim and explicit removable blackout panels behind all glazing.
    for y in (-8,0,8):
        detail('ClerestoryLintel',(side*8.01,y,6.84),(.32,5.48,.16))
        detail('ClerestorySill',(side*8.04,y,5.56),(.44,5.52,.16))
        for end in (-1,1):
            detail('ClerestoryReveal',(side*7.96,y+end*2.67,6.2),(.32,.14,1.18))
        box('RemovableBlackout',(side*7.66,y,6.2),(.035,5.12,1.12),'ADM_WindowSteel','BlackoutPanels')
    # Foundation cap and narrow vertical masonry construction joints.
    detail('BaseWeatheringCap',(side*7.99,0,.73),(.42,23.8,.07),'STG_Foundation')
    for y in (-4,4):
        box('WallJoint',(side*8.145,y+.25,3.65),(.008,.009,5.8),'STG_Foundation','ArchitecturalDetails')

# Deep load-bearing portal and fine double-leaf construction.
for side in (-1,1):
    detail('PortalJamb',(side*3.02,-12.18,3.10),(.44,.66,6.20))
    detail('PortalOuterFillet',(side*3.29,-12.19,3.13),(.11,.38,6.26),'ADM_Stucco')
detail('PortalLintel',(0,-12.18,6.22),(6.5,.66,.36))
detail('PortalDrip',(0,-12.23,6.44),(6.78,.72,.09))
for side in (-1,1):
    grp='StageDoorLeft' if side<0 else 'StageDoorRight'
    cx=side*1.4
    # Backing plus individual flush planks, bounded by narrow steel stiles.
    box('HeavyDoorCore',(cx,-12.025,3),(2.77,.20,5.96),'STG_DarkDoors',grp)
    for j in range(13):
        detail('DoorPlank',(cx-1.26+j*.21,-12.135,3),(.201,.024,5.75),'STG_DarkDoors',grp,.002)
    for x in (cx-1.32,cx+1.32):
        detail('LeafStile',(x,-12.16,3),(.075,.045,5.9),'ADM_WindowSteel',grp,.004)
    for z in (.15,2.05,4.03,5.86):
        detail('LeafRail',(cx,-12.18,z),(2.66,.035,.075),'ADM_WindowSteel',grp,.003)
        for j in range(10):bolt('RailRivet',cx-1.20+j*.267,-12.205,z,grp)
    # Flat narrow tension straps, not oversized cylindrical diagonal bars.
    strip('LowerTensionStrap',(cx-side*1.26,-12.18,.25),(cx+side*1.26,-12.18,2.0),.065,.025,group=grp)
    for z in (.30,2.12,4.12,5.65):
        rod('HingeBarrel',(side*2.79,-12.16,z-.08),(side*2.79,-12.16,z+.08),.035,group=grp)
        detail('HingeDoorStrap',(side*2.61,-12.17,z),(.35,.028,.085),'ADM_WindowSteel',grp,.003)
        detail('HingeFramePlate',(side*2.86,-12.17,z),(.12,.045,.15),'ADM_WindowSteel','DoorFrames',.003)
    hx=side*.17
    detail('PullEscutcheon',(hx,-12.18,1.1),(.065,.025,.46),'ADM_Bronze',grp,.008)
    for z in (.91,1.29):rod('PullStandOff',(hx,-12.19,z),(hx,-12.27,z),.018,'ADM_Bronze',grp)
    rod('VerticalDoorPull',(hx,-12.27,.91),(hx,-12.27,1.29),.018,'ADM_Bronze',grp)
    # Interior back face has usable hand pulls rather than a featureless slab.
    for z in (.94,1.24):rod('InsidePullStandOff',(hx,-11.92,z),(hx,-11.84,z),.017,'ADM_WindowSteel',grp)
    rod('InsideDoorPull',(hx,-11.84,.94),(hx,-11.84,1.24),.017,'ADM_WindowSteel',grp)

door('PersonnelRight',5.8,-12,1,2.15)

def industrial_lamp(x,y,z,angle):
    def p(u,d,zz):return(x+u*math.cos(angle)-d*math.sin(angle),y+u*math.sin(angle)+d*math.cos(angle),zz)
    rod('LampWallFlange',p(0,-.14,z),p(0,-.20,z),.085,group='Utilities')
    # Smooth bent manufactured tube on a physical flange.
    pp=[p(0,-.20,z)]
    for i in range(13):
        t=math.pi*i/12
        pp.append(p(0,-.44+.24*math.cos(t),z+.26*math.sin(t)))
    pp.append(p(0,-.68,z-.10))
    for a,b in zip(pp,pp[1:]):rod('LampNeck',a,b,.016,group='Utilities')
    vv=[]
    for radius,dz in ((.045,0),(.065,-.055),(.13,-.13),(.235,-.21)):
        for i in range(20):
            t=2*math.pi*i/20
            vv.append(p(radius*math.cos(t),-.68+radius*math.sin(t),z+dz-.10))
    o=mesh('EnamelBellShade',vv,[(j*20+i,j*20+(i+1)%20,(j+1)*20+(i+1)%20,(j+1)*20+i) for j in range(3) for i in range(20)],'ADM_WindowSteel','Utilities')
    mod=o.modifiers.new('Sheet metal','SOLIDIFY');mod.thickness=.008
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    for i in range(20):
        t=2*math.pi*i/20;tt=2*math.pi*(i+1)/20
        rod('RolledLampRim',p(.235*math.cos(t),-.68+.235*math.sin(t),z-.31),p(.235*math.cos(tt),-.68+.235*math.sin(tt),z-.31),.009,group='Utilities')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.062,location=p(0,-.68,z-.285))
    finish(bpy.context.object,'LampGlobe','ADM_LampDiffuser','Utilities')

# Integrated personnel portals with pitched folded-metal canopies and brackets.
for x,y,angle in ((-5.8,-12,0),(5.8,-12,0),(4.8,12,math.pi)):
    s=-1 if y<0 else 1
    for dx in (-.62,.62):detail('ServicePortalJamb',(x+dx,y+s*.08,1.11),(.18,.36,2.22))
    detail('ServicePortalLintel',(x,y+s*.08,2.25),(1.44,.38,.20))
    canopy=detail('FoldedServiceCanopy',(x,y+s*.48,2.55),(1.62,.95,.055),'ADM_WindowSteel')
    canopy.rotation_euler.x=s*math.radians(10)
    for dx in (-.65,.65):
        strip('CanopyBracket',(x+dx,y+s*.18,2.16),(x+dx,y+s*.90,2.48),.028,.04)
        rod('CanopySeam',(x+dx,y+s*.06,2.66),(x+dx,y+s*.91,2.51),.012,group='ArchitecturalDetails')
    detail('CanopyDrip',(x,y+s*.94,2.48),(1.66,.045,.08),'ADM_WindowSteel')
    industrial_lamp(x-.9,y,2.45,angle)
for x in (-4.2,4.2):industrial_lamp(x,-12,3.55,0)

# Connected gutters, downpipes and service electrical run; no loose dressing.
for side in (-1,1):
    box('EaveGutter',(side*8.15,0,7.52),(.18,24,.14),'STG_ZincRoof','Utilities')
    rod('RainwaterElbow',(side*8.15,-11.75,7.5),(side*7.42,-12.26,7.5),.04,group='Utilities')
    rod('RainwaterPipe',(side*7.42,-12.26,.25),(side*7.42,-12.26,7.5),.04,group='Utilities')
    rod('RainwaterShoe',(side*7.42,-12.26,.25),(side*7.42,-12.45,.12),.04,group='Utilities')
    for z in (1,3.5,6):box('DownpipeClamp',(side*7.42,-12.20,z),(.14,.17,.05),'ADM_WindowSteel','Utilities')
for x in (-6,-5.1):
    detail('ServiceEnclosure',(x,12.23,1.25),(.55,.32,1.35),'ADM_WindowSteel','Utilities')
    detail('EnclosureCover',(x,12.405,1.25),(.49,.035,1.28),'STG_ZincRoof','Utilities')
    detail('EnclosureLatch',(x+.18,12.44,1.2),(.035,.025,.12),'ADM_Bronze','Utilities')
    rod('FeedConduit',(x,12.22,1.92),(x,12.22,6.9),.022,group='Utilities')
    for z in (2.1,4.2,6.4):box('ConduitClamp',(x,12.19,z),(.09,.13,.04),'ADM_WindowSteel','Utilities')
rod('RearServiceHeader',(-6,12.22,6.9),(4.8,12.22,6.9),.022,group='Utilities')
rod('RearLampSupply',(3.9,12.22,6.9),(3.9,12.22,2.45),.018,group='Utilities')
for x,z in ((-3,7.9),(0,8.7),(3,7.9)):
    detail('RearVentFrame',(x,12.21,z),(.55,.18,.65),'ADM_WindowSteel','Utilities')
    for dz in (-.22,-.11,0,.11,.22):
        o=box('RearVentSlat',(x,12.32,z+dz),(.47,.08,.055),'STG_ZincRoof','Utilities');o.rotation_euler.x=.35

# Closed, opaque blackout roof-light strip. No uncontrolled daylight or glass.
# Original sealed barrel remains intact below it; shutters are independently grouped.
for side in (-1,1):
    for j in range(16):
        a,b=-9.8+j*1.225,-9.8+(j+1)*1.225
        vv=[]
        for y in (a+.035,b-.035):
            for i in range(5):
                t=.62+i*.105
                vv.append((side*(7.96*math.cos(t)+.045),y,7.6+3.05*math.sin(t)+.10))
        o=mesh('ClosedRoofLightPanel',vv,[(i,i+1,6+i,5+i) for i in range(4)],'STG_BlackoutRoof','RoofBlackout')
        mod=o.modifiers.new('Opaque panel thickness','SOLIDIFY');mod.thickness=.045
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    for t in (.61,1.05):
        x=side*(7.96*math.cos(t)+.05);z=7.6+3.05*math.sin(t)+.12
        box('RoofLightEdge',(x,0,z),(.075,19.75,.09),'ADM_WindowSteel','RoofBlackout')
    for j in range(17):
        y=-9.8+j*1.225
        for i in range(5):
            t=.61+i*.088;tt=t+.088
            rod('RoofLightMullion',(side*(7.96*math.cos(t)+.05),y,7.6+3.05*math.sin(t)+.13),(side*(7.96*math.cos(tt)+.05),y,7.6+3.05*math.sin(tt)+.13),.025,group='RoofBlackout')
for y in (-8,0,8):
    detail('RoofVentCurb',(0,y,10.68),(1.12,1.02,.30),'STG_Foundation','Roof')
    box('VentOpaqueCore',(0,y,11.0),(.77,.67,.55),'ADM_WindowSteel','Roof')
    for z in (10.83,10.97,11.11,11.25):
        detail('VentLouver',(0,y,z),(1.22,1.12,.055),'ADM_WindowSteel','Roof',.01)
    detail('VentWeatherCap',(0,y,11.36),(1.35,1.25,.10),'STG_ZincRoof','Roof')

exec(compile((HERE/'stage_fidelity.py').read_text(),str(HERE/'stage_fidelity.py'),'exec'))
bpy.context.view_layer.update()
for o in objects:
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for face in o.data.polygons:
        normal=o.matrix_world.to_3x3()@face.normal;axis=max(range(3),key=lambda k:abs(normal[k]));axes=[k for k in range(3) if k!=axis]
        for li in face.loop_indices:
            co=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]],co[axes[1]])
    assert all(math.isfinite(v) for v in o.dimensions)
root=bpy.data.objects.new(NAME,None);scene.collection.objects.link(root)
exports=[]
for name,parts in groups.items():
    if not parts:continue
    bpy.ops.object.select_all(action='DESELECT');copies=[]
    for original in parts:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.select_set(True);copies.append(o)
    bpy.context.view_layer.objects.active=copies[0]
    if len(copies)>1:bpy.ops.object.join()
    o=bpy.context.object;o.name=name;o.parent=root
    scene.cursor.location=(-2.8,-12.03,0) if name=='StageDoorLeft' else ((2.8,-12.03,0) if name=='StageDoorRight' else (0,0,0))
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    # Recalculate outward normals on the export mesh.
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    exports.append(o)
out=ROOT/'ArtExports/ReferenceKit1930';out.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
for o in exports:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(out/(NAME+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,bake_anim=False)
target=ROOT/'Assets/SilverScreen/Environment/ReferenceKit/Models';target.mkdir(exist_ok=True)
shutil.copy2(out/(NAME+'.fbx'),target/(NAME+'.fbx'))
report={'footprint':[15.8,24],'eaves':7.6,'roof_crown':10.65,'highest_vent':11.41,'door':[5.6,6],'catwalk_height':4.5,'stair_rise':.1875,'stair_going':.30,'groups':list(groups),'triangles':sum(len(p.vertices)-2 for o in exports for p in o.data.polygons),'materials':definitions}
report['blackout']='Opaque closed roof-light panels over sealed barrel; separate clerestory blackout panels.'
report['degenerate_faces']=sum(p.area<1e-10 for o in exports for p in o.data.polygons)
report['unit_scale']=all((o.scale-Vector((1,1,1))).length<1e-6 for o in exports)
assert report['degenerate_faces']==0, 'Export contains zero-area faces'
assert report['unit_scale'], 'Export scale must be applied'
assert not any(o.type=='FONT' for o in objects), 'Stage lettering must remain in Unity'
(HERE/'stage1_report.json').write_text(json.dumps(report,indent=2)+'\n')
# Reapply authored linear colors through the accepted Unity sRGB transfer.
# Existing material assets retain their shared microtexture maps and GUIDs.
def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
for name,data in definitions.items():
    path=ROOT/'Assets/SilverScreen/Environment/ReferenceKit/StageMaterials'/(name+'.mat')
    if not path.exists():continue
    text=path.read_text()
    rgb=[srgb(v) for v in data['linear']]
    color='{r: %.8f, g: %.8f, b: %.8f, a: 1}'%tuple(rgb)
    for key in ('_BaseColor','_Color'):
        text=re.sub(r'(- '+key+r': )[^\n]+',lambda m:m[1]+color,text)
    for key,value in (('_Metallic',data['metallic']),('_Smoothness',(1-data['roughness'])/.9)):
        text=re.sub(r'(- '+key+r': )[^\n]+',lambda m:m[1]+str(value),text)
    path.write_text('\n'.join(line.rstrip() for line in text.splitlines())+'\n')
for o in exports:bpy.data.objects.remove(o,do_unlink=True)
bpy.data.objects.remove(root,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(NAME+'.blend')))
print(json.dumps(report))
