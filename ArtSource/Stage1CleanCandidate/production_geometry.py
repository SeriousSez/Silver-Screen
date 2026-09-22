"""Production construction authored into the candidate generator's scene.

Executed by generate_stage1_candidate.py before its geometry checks/export.
The parent generator owns all output paths, semantic groups and identities.
"""
from mathutils import Quaternion
import random

PERMANENT_LIGHTS=[]
TEXTURE_MATERIALS={}

def material(name,rgb,metal,rough,texture=None,scale=1):
    PALETTE[name]=(rgb,metal,rough)
    m=materials.get(name) or bpy.data.materials.new(name)
    materials[name]=m;m.diffuse_color=(*rgb,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    if texture:
        TEXTURE_MATERIALS[name]={'set':texture,'metres_per_tile':scale}
        path=HERE/'Textures'/(texture+'_albedo.png')
        if path.exists():
            tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True)
            tint=m.node_tree.nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*rgb,1)
            m.node_tree.links.new(tex.outputs['Color'],tint.inputs[1]);m.node_tree.links.new(tint.outputs[0],bs.inputs['Base Color'])
            ntex=m.node_tree.nodes.new('ShaderNodeTexImage');ntex.image=bpy.data.images.load(str(HERE/'Textures'/(texture+'_normal.png')),check_existing=True);ntex.image.colorspace_settings.name='Non-Color'
            norm=m.node_tree.nodes.new('ShaderNodeNormalMap');m.node_tree.links.new(ntex.outputs['Color'],norm.inputs['Color']);m.node_tree.links.new(norm.outputs['Normal'],bs.inputs['Normal'])
    return m

material('C1_Stucco',[.55,.48,.375],0,.84,'stucco',3.0)
material('C1_Trim',[.61,.545,.445],0,.76,'stone',2.0)
material('C1_Base',[.26,.25,.226],0,.88,'concrete',2.4)
material('C1_Roof',[.19,.20,.205],.78,.36,'roof_metal',2.5)
material('C1_Timber',[.026,.023,.020],0,.62,'timber',2.0)
material('C1_Steel',[.031,.035,.036],.04,.49,'paint',1.5)
material('C1_GlassStudy',[.14,.205,.235],.05,.15)
material('C1_Blackout',[.032,.030,.026],0,.96,'cloth',1.4)
material('C1_Floor',[.145,.150,.147],0,.28,'floor',4.0)
material('C1_StructuralSteel',[.06,.065,.063],.52,.52,'metal',2.0)
material('C1_RoofTimber',[.275,.184,.105],0,.74,'timber',2.0)
material('C1_Brass',[.42,.28,.09],.83,.32,'metal',1.2)
material('C1_Galvanized',[.37,.38,.37],.76,.47,'metal',1.7)
material('C1_EnamelWhite',[.77,.735,.64],.06,.31)
material('C1_LampReflector',[.77,.735,.64],.06,.31)
material('C1_LampGlass',[.93,.75,.46],0,.22)
material('C1_SafetyOchre',[.57,.365,.055],.03,.57,'paint',1.2)
material('C1_AcousticFabric',[.09,.085,.068],0,.96,'cloth',1.4)
material('C1_DarkCeramic',[.022,.019,.015],0,.28)
material('C1_Canvas',[.105,.094,.076],0,.90,'cloth',1.4)

def remove_categories(categories):
    for o in list(objects):
        if o.parent.name in categories:
            objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)

def remove_named(prefixes):
    for o in list(objects):
        if any(o.name.startswith(p) for p in prefixes):
            objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)

remove_categories({'MainDoorLeft','MainDoorRight','PersonnelDoors','Clerestory','BlackoutShutters','EntranceCanopies','RoofVents','RearVentilation','PrimaryStructure','RiggingPrimary','CirculationAllowances','CatwalkStructure','CirculationGuards','Stairs'})
remove_named(['SideServiceReveal','SideServiceHead'])

def bevel_object(o,width=.008,segments=2):
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.bevel(bm,geom=list(bm.edges),offset=width,segments=segments,affect='EDGES',clamp_overlap=True)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    return o

def block(name,pos,size,mat='C1_Steel',cat='PermanentServices',edge=.005):
    o=box(name,*pos,*size,mat,cat)
    return bevel_object(o,min(edge,min(size)*.21),2) if edge else o

def transform_object(o,origin,u,v,w):
    origin,u,v,w=map(Vector,[origin,u,v,w])
    for vertex in o.data.vertices:
        a,b,c=vertex.co;vertex.co=origin+u*a+v*b+w*c
    if u.cross(v).dot(w)<0:
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    o.data.update();return o

def tube(name,points,radius=.021,mat='C1_Steel',cat='PermanentServices',sides=16):
    points=list(map(Vector,points));verts=[];faces=[]
    previous=None
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        if previous is None:
            ref=Vector((0,0,1)) if abs(tangent.z)<.9 else Vector((0,1,0))
            n=tangent.cross(ref).normalized()
        else:n=(previous-tangent*previous.dot(tangent)).normalized()
        b=tangent.cross(n).normalized();previous=n
        verts.extend(p+radius*(n*math.cos(j*2*math.pi/sides)+b*math.sin(j*2*math.pi/sides)) for j in range(sides))
    for i in range(len(points)-1):
        for j in range(sides):
            k=i*sides+j;q=i*sides+(j+1)%sides
            faces.append((k,q,q+sides,k+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
    o=mesh(name,verts,faces,mat,cat)
    for p in o.data.polygons[:-2]:p.use_smooth=True
    return o

def fillet(points,r=.09,steps=10):
    pts=list(map(Vector,points));out=[pts[0]]
    for i in range(1,len(pts)-1):
        p=pts[i];a=(pts[i-1]-p).normalized();b=(pts[i+1]-p).normalized()
        distance=min(r,(pts[i-1]-p).length*.35,(pts[i+1]-p).length*.35)
        p0=p+a*distance;p2=p+b*distance
        for k in range(steps+1):
            t=k/steps;out.append((1-t)**2*p0+2*(1-t)*t*p+t*t*p2)
    out.append(pts[-1]);return out

def rod(name,start,end,r=.025,mat='C1_Steel',cat='PermanentServices',sides=20):
    return tube(name,[start,end],r,mat,cat,sides)

def lathe(name,profile,centre=(0,0,0),mat='C1_Steel',cat='PermanentServices',sides=40):
    # Profiles have positive radius; cap disks close the two ends.
    vs=[];fs=[]
    for rad,z in profile:
        vs.extend((centre[0]+rad*math.cos(i*2*math.pi/sides),centre[1]+rad*math.sin(i*2*math.pi/sides),centre[2]+z) for i in range(sides))
    for k in range(len(profile)-1):
        for i in range(sides):fs.append((k*sides+i,k*sides+(i+1)%sides,(k+1)*sides+(i+1)%sides,(k+1)*sides+i))
    fs.extend([tuple(reversed(range(sides))),tuple((len(profile)-1)*sides+i for i in range(sides))])
    o=mesh(name,vs,fs,mat,cat)
    for p in o.data.polygons[:-2]:p.use_smooth=True
    return o

def fastener(name,point,axis=(0,-1,0),r=.016,mat='C1_Steel',cat='Hardware'):
    o=lathe(name,[(r*.95,0),(r,.005),(r*.85,.012),(r*.45,.018)],mat=mat,cat=cat,sides=12)
    q=Vector(axis).normalized().to_track_quat('Z','Y')
    for v in o.data.vertices:v.co=q@v.co+Vector(point)
    return o

def extrusion(name,poly,start,end,mat='C1_StructuralSteel',cat='PrimaryStructure',up=(0,0,1)):
    a,b=Vector(start),Vector(end);axis=(b-a).normalized();ref=Vector(up)
    if abs(axis.dot(ref))>.99:ref=Vector((0,1,0))
    u=axis.cross(ref).normalized();v=u.cross(axis).normalized()
    verts=[p+u*x+v*y for p in [a,b] for x,y in poly];n=len(poly)
    faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
    return mesh(name,verts,faces,mat,cat)

def i_profile(width,depth,flange=.017,web=.011):
    w=width/2;h=depth/2;t=web/2
    return [(-w,-h),(w,-h),(w,-h+flange),(t,-h+flange),(t,h-flange),(w,h-flange),(w,h),(-w,h),(-w,h-flange),(-t,h-flange),(-t,-h+flange),(-w,-h+flange)]

def ibeam(name,a,b,width=.18,depth=.24,cat='PrimaryStructure'):
    return extrusion(name,i_profile(width,depth),a,b,cat=cat)

def anglebeam(name,a,b,size=.085,cat='PrimaryStructure'):
    h=size/2;t=.009
    return extrusion(name,[(-h,-h),(h,-h),(h,-h+t),(-h+t,-h+t),(-h+t,h),(-h,h)],a,b,cat=cat)

def plate_xz(name,poly,y,depth=.016,mat='C1_StructuralSteel',cat='StructuralConnections'):
    n=len(poly);vs=[(x,yy,z) for yy in [y-depth/2,y+depth/2] for x,z in poly]
    return mesh(name,vs,[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)],mat,cat)

# Masonry arrises, sill projection, pilaster transitions and expressed base joints.
for o in list(objects):
    if o.parent.name=='MasonryFraming' and not o.name.startswith(('Arch','Portal')):
        bevel_object(o,.008,2)
for side in [-1,1]:
    # The base course stops outside the two personnel door frames.
    for y0,y1 in [(-11.70,-10.685),(-9.365,9.365),(10.685,11.70)]:
        block('StoneBaseTopCourse',(side*7.918,(y0+y1)/2,1.315),(.065,y1-y0,.075),'C1_Base','MasonryFraming',.006)
    for yy in [-10.4,-7.8,-5.2,-2.6,0,2.6,5.2,7.8,10.4]:
        block('BaseControlJoint',(side*7.903,yy,.67),(.007,.012,1.22),'C1_Blackout','MasonryFraming',0)
for xx in [-6.9,-4.9,4.9,6.9]:
    block('FrontBaseJoint',(xx,-12.003,.67),(.012,.007,1.22),'C1_Blackout','MasonryFraming',0)
block('MainThreshold',(0,-11.94,FLOOR/2),(7.20,.55,FLOOR),'C1_Base','MasonryFraming',.005)
block('ThresholdWearStrip',(0,-12.14,FLOOR-.004),(7.12,.15,.008),'C1_Galvanized','MasonryFraming',.002)

def threshold_apron(name,width,start,run,centre,u=(1,0,0),out=(0,-1,0)):
    # Top slopes continuously from the interior floor to a 2 mm pavement lip.
    vs=[(x,d,z) for x in [-width/2,width/2] for d,z in [(start,GROUND),(start+run,GROUND),(start+run,GROUND+.002),(start,FLOOR)]]
    o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'C1_Base','MasonryFraming')
    return transform_object(o,centre,u,out,(0,0,1))

threshold_apron('MainVehicleThresholdApron',7.20,.20,.72,(0,-12.0,0))

# Main scenery doors: separate timber planks, substantial frame, steel reinforcing
# straps, built hinges, meeting strip, interior bracing and formed brass pulls.
def pull_handle(x,y,z,height=.79,cat='Hardware',normal=-1):
    for zz in [z-height/2,z+height/2]:
        block('PullEscutcheon',(x,y,zz),(.10,.02,.16),'C1_Brass',cat,.025)
        for dz in [-.049,.049]:fastener('EscutcheonScrew',(x,y+normal*.012,zz+dz),(0,normal,0),.007,'C1_Brass',cat)
    path=[(x,y,z-height/2),(x,y+normal*.14,z-height/2),(x,y+normal*.14,z+height/2),(x,y,z+height/2)]
    tube('ForgedPull',fillet(path,.105,12),.019,'C1_Brass',cat,24)

for side in [-1,1]:
    cat='MainDoorLeft' if side<0 else 'MainDoorRight'
    xmid=side*1.803;width=3.586;zmid=FLOOR+DOOR_H/2
    block('DoorAcousticCore',(xmid,-11.805,zmid),(width,.21,DOOR_H-.03),'C1_Blackout',cat,.008)
    block('ExteriorTimberTongue',(xmid,-11.947,zmid),(width-.012,.030,DOOR_H-.04),'C1_Timber',cat,.002)
    block('InteriorTimberTongue',(xmid,-11.651,zmid),(width-.012,.018,DOOR_H-.04),'C1_Timber',cat,.002)
    count=22;pitch=width/count
    for j in range(count):
        xx=xmid-width/2+(j+.5)*pitch
        block('VerticalTimberBoard',(xx,-11.942,zmid),(pitch-.007,.075,DOOR_H-.035),'C1_Timber',cat,.004)
        block('InteriorTimberBoard',(xx,-11.661,zmid),(pitch-.007,.070,DOOR_H-.035),'C1_Timber',cat,.004)
    for xx in [xmid-width/2+.078,xmid+width/2-.078]:
        block('DoorOuterTimberStile',(xx,-11.989,zmid),(.145,.060,DOOR_H-.06),'C1_Timber',cat,.006)
        block('DoorInnerTimberStile',(xx,-11.59,zmid),(.18,.095,DOOR_H-.08),'C1_Timber',cat,.006)
    for zz in [.35,2.91,5.31,6.81]:
        block('ForgedStrap',(xmid,-12.027,zz),(width-.09,.028,.13),'C1_Steel',cat,.008)
        block('InteriorDoorRail',(xmid,-11.562,zz),(width-.12,.14,.18),'C1_Timber',cat,.008)
        for j in range(9):
            xx=xmid-width/2+.14+j*(width-.28)/8
            fastener('StrapRivet',(xx,-12.047,zz),(0,-1,0),.017,'C1_Steel',cat)
    for yy,mat in [(-12.025,'C1_Steel'),(-11.46,'C1_Steel')]:
        a=(side*3.43,yy,.47);b=(side*.19,yy,2.79)
        extrusion('LowerDiagonalStrap',[(-.055,-.013),(.055,-.013),(.055,.013),(-.055,.013)],a,b,mat,cat,up=(0,1,0))
        for t in [.06,.3,.55,.8,.95]:
            p=Vector(a).lerp(Vector(b),t);fastener('BraceRivet',p+(Vector((0,-.017,0)) if yy< -12 else Vector((0,.017,0))),(0,-1 if yy< -12 else 1,0),.016,mat,cat)
    # The concept's lower leaf panels use restrained crossed straps.
    a=(side*3.43,-12.052,2.79);b=(side*.19,-12.052,.47)
    extrusion('LowerCrossStrap',[(-.048,-.013),(.048,-.013),(.048,.013),(-.048,.013)],a,b,'C1_Steel',cat,up=(0,1,0))
    for t in [.06,.30,.55,.80,.95]:
        p=Vector(a).lerp(Vector(b),t);fastener('CrossStrapRivet',p+Vector((0,-.017,0)),(0,-1,0),.016,'C1_Steel',cat)
    # Interior upper X spreads leaf loads into the substantial stile/rail frame.
    for a,b in [((side*.19,-11.458,3.02),(side*3.43,-11.458,6.70)),((side*3.43,-11.456,3.02),(side*.19,-11.456,6.70))]:
        extrusion('InteriorCrossBrace',[(-.045,-.012),(.045,-.012),(.045,.012),(-.045,.012)],a,b,'C1_Steel',cat,up=(0,1,0))
    for zz in [.43,1.72,3.15,4.72,6.43]:
        hinge_x=side*3.57
        block('HingeFramePlate',(side*3.655,-12.072,zz),(.18,.045,.29),'C1_Steel','DoorFrameHardware',.009)
        block('HingeLeafStrap',(side*3.31,-12.065,zz),(.52,.042,.15),'C1_Steel',cat,.009)
        for dz in [-.10,0,.10]:rod('HingeKnuckle',(hinge_x,-12.111,zz+dz-.038),(hinge_x,-12.111,zz+dz+.038),.045,'C1_Steel',cat,24)
        rod('HingePin',(hinge_x,-12.111,zz-.17),(hinge_x,-12.111,zz+.17),.016,'C1_Galvanized',cat,20)
        for xx in [side*3.12,side*3.42]:fastener('HingeStrapBolt',(xx,-12.09,zz),(0,-1,0),.019,'C1_Steel',cat)
        for dz in [-.095,.095]:fastener('HingeAnchor',(side*3.69,-12.10,zz+dz),(0,-1,0),.021,'C1_Steel','DoorFrameHardware')
    pull_handle(side*.24,-12.04,1.77,.79,cat)
    pull_handle(side*.25,-11.425,1.77,.72,cat,normal=1)
    for zz in [.49,6.51]:
        rod('DropBolt',(side*.09,-11.425,zz-.21),(side*.09,-11.425,zz+.21),.016,'C1_Galvanized',cat)
        for dz in [-.12,.12]:block('DropBoltGuide',(side*.09,-11.438,zz+dz),(.07,.055,.045),'C1_Steel',cat,.005)
block('DoorMeetingAstragal',(.017,-12.035,zmid),(.047,.035,DOOR_H-.04),'C1_Steel','MainDoorRight',.006)
for x in [-3.69,3.69]:
    block('InteriorPortalLiner',(x,-11.605,3.56),(.18,.17,7.12),'C1_Steel','DoorFrameHardware',.01)
block('InteriorPortalHeader',(0,-11.605,7.04),(7.56,.17,.24),'C1_Steel','DoorFrameHardware',.01)

# Detailed personnel doors are assembled locally, then oriented onto any facade.
PERSONNEL_OPENINGS=[]
def personnel(name,centre,width,height,u=(1,0,0),out=(0,-1,0),hinge_out=.42):
    start=len(objects);cat='PersonnelDoors';z0=FLOOR
    block(name+'Core',(0,0,z0+height/2),(width-.06,.11,height-.022),'C1_Timber',cat,.004)
    for xx in [-width/2+.10,width/2-.10]:block('RaisedDoorStile',(xx,-.078,z0+height/2),(.14,.057,height-.06),'C1_Timber',cat,.004)
    for zz in [z0+.09,z0+.85,z0+height-.11]:block('DoorCrossRail',(0,-.079,zz),(width-.21,.058,.16),'C1_Timber',cat,.004)
    for bottom,top in [(z0+.18,z0+.75),(z0+.95,z0+height-.22)]:
        block('RecessedDoorPanel',(0,-.067,(bottom+top)/2),(width-.30,.04,top-bottom),'C1_Timber',cat,.006)
    for xx in [-width/2-.045,width/2+.045]:block('PersonnelSteelJamb',(xx,-.06,z0+height/2),(.08,.19,height+.10),'C1_Steel',cat,.006)
    block('PersonnelSteelHeader',(0,-.06,z0+height+.035),(width+.17,.19,.09),'C1_Steel',cat,.006)
    # Rebated stops seal the swing clearance behind the closed leaf. They
    # attach to the jamb and release as the leaf swings outward.
    for xx in [-width/2+.01,width/2-.01]:
        block('PersonnelStopJamb',(xx,.083,z0+height/2),(.06,.050,height+.01),'C1_Steel',cat,.003)
        block('PersonnelDoorSeal',(xx,.055,z0+height/2),(.06,.006,height-.01),'C1_Blackout',cat,.001)
    for xx in [-width/2-.024,width/2+.024]:block('PersonnelStopConnection',(xx,.052,z0+height/2),(.060,.100,height+.01),'C1_Steel',cat,.003)
    block('PersonnelStopHeadConnection',(0,.052,z0+height+.031),(width+.06,.10,.045),'C1_Steel',cat,.003)
    block('PersonnelStopHead',(0,.083,z0+height+.0075),(width+.04,.050,.045),'C1_Steel',cat,.003)
    block('PersonnelDoorSealHead',(0,.055,z0+height+.0075),(width-.04,.006,.040),'C1_Blackout',cat,.001)
    block('PersonnelLeafBottomSeal',(0,.046,FLOOR+.010),(width-.06,.016,.018),'C1_Blackout',cat,.003)
    block('PersonnelThreshold',(0,-.09,FLOOR-.004),(width-.008,.44,.008),'C1_Galvanized',cat,.002)
    pull_handle(width*.30,-.12,1.15,.30,cat)
    # A mortise lock: keyhole escutcheon below the pull, latch in the leaf edge,
    # and a strike recessed into the latch jamb. No unexplained brass block.
    lock_x=width*.30;lock_z=.82
    block('MortiseKeyEscutcheon',(lock_x,-.115,lock_z),(.073,.018,.105),'C1_Brass',cat,.023)
    rod('KeyholeRound',(lock_x,-.126,lock_z+.014),(lock_x,-.13,lock_z+.014),.012,'C1_Blackout',cat,24)
    block('KeyholeSlot',(lock_x,-.13,lock_z-.006),(.012,.004,.031),'C1_Blackout',cat,.003)
    for zz in [lock_z-.035,lock_z+.039]:fastener('LockEscutcheonScrew',(lock_x,-.128,zz),(0,-1,0),.005,'C1_Brass',cat)
    block('MortiseLockEdgePlate',(width/2-.034,0,.91),(.014,.085,.23),'C1_Brass',cat,.003)
    block('LatchTongue',(width/2-.018,-.01,.95),(.070,.035,.034),'C1_Brass',cat,.008)
    block('LatchStrike',(width/2+.013,-.015,.95),(.012,.085,.14),'C1_Steel',cat,.004)
    # Cranked hinge leaves place the pin beyond the deepest masonry surround.
    # The moving leaf stays recessed; the actual sweep is checked below.
    for zz in [.35,1.18,height-.27]:
        hx=-width/2-.012
        rod('PersonnelHinge',(hx,-hinge_out,zz-.073),(hx,-hinge_out,zz+.073),.024,'C1_Steel',cat,24)
        block('PersonnelHingeLeaf',(-width/2+.083,-.12,zz),(.19,.025,.09),'C1_Steel',cat,.004)
        block('CrankedHingeReturn',(hx,-(hinge_out+.12)/2,zz),(.025,hinge_out-.12,.09),'C1_Steel',cat,.004)
        block('HingeJambAttachment',(hx-.053,-hinge_out+.025,zz),(.12,.09,.15),'C1_Steel',cat,.005)
        for dx in [.035,.125]:fastener('HingeLeafScrew',(-width/2+dx,-.138,zz),(0,-1,0),.008,'C1_Steel',cat)
    for xx in [-width/2+.115,width/2-.115]:block('InteriorEntryStile',(xx,.077,z0+height/2),(.14,.052,height-.06),'C1_Timber',cat,.005)
    for zz in [z0+.09,z0+.85,z0+height-.11]:block('InteriorEntryRail',(0,.077,zz),(width-.21,.052,.16),'C1_Timber',cat,.005)
    pull_handle(width*.30,.13,1.15,.30,cat,normal=1)
    block('InteriorMortiseThumbturnPlate',(lock_x,.115,lock_z),(.074,.018,.103),'C1_Brass',cat,.023)
    rod('InteriorLockSpindle',(lock_x,.12,lock_z),(lock_x,.15,lock_z),.016,'C1_Brass',cat,20)
    block('InteriorLockThumbturn',(lock_x,.157,lock_z),(.068,.018,.025),'C1_Brass',cat,.01)
    fixed=('PersonnelSteel','PersonnelThreshold','PersonnelStop','PersonnelDoorSeal','LatchStrike','HingeJambAttachment')
    moving=[]
    for o in objects[start:]:
        if not o.name.startswith(fixed):moving.append(o)
        o['personnel_opening']=name
        transform_object(o,centre,u,tuple(-v for v in out),(0,0,1))
    threshold_apron(name+'ThresholdApron',width,.30,.48,centre,u,out)
    PERSONNEL_OPENINGS.append({'name':name,'centre':centre,'width':width,'height':height,'u':u,'out':out,'hinge_out':hinge_out,'moving':moving})

personnel('FrontLeftEntry',(-5.97,-12.09,0),1.0,2.25,hinge_out=.18)
personnel('FrontRightEntry',(5.70,-12.09,0),1.10,2.30,hinge_out=.18)
personnel('RearEntry',(0,12.11,0),1.56,2.70,u=(-1,0,0),out=(0,1,0),hinge_out=.19)
for side in [-1,1]:
    for yy in [-10.025,10.025]:personnel('SideEntry_%s_%s'%(side,yy),(side*7.80,yy,0),1.05,2.30,u=(0,side,0),out=(side,0,0),hinge_out=.14)

# Heavy canvas is tensioned over three steel ribs; the restrained edge drop and
# shallow cloth deflection distinguish it from the previous folded metal hood.
def production_canopy(x,y,width,projection,z,face=-1):
    edge=y+face*projection
    def surface(s,t):
        return (x+(s-.5)*width,y+face*t*projection,z-.24*t-.024*math.sin(2*math.pi*s)**2*math.sin(math.pi*t))
    nx,ny=40,18;vs=[];fs=[]
    for dz in [0,-.004]:
        for i in range(nx+1):
            for j in range(ny+1):
                p=surface(i/nx,j/ny);vs.append((p[0],p[1],p[2]+dz))
    n=(nx+1)*(ny+1)
    for i in range(nx):
        for j in range(ny):
            q=i*(ny+1)+j;r=q+ny+1
            fs.extend([(q,r,r+1,q+1),(q+n,q+1+n,r+1+n,r+n)])
    boundary=list(range(ny+1))+[i*(ny+1)+ny for i in range(1,nx+1)]+[nx*(ny+1)+j for j in range(ny-1,-1,-1)]+[i*(ny+1) for i in range(nx-1,0,-1)]
    fs.extend((q,r,r+n,q+n) for q,r in zip(boundary,boundary[1:]+boundary[:1]))
    mesh('TensionedCanvasAwning',vs,fs,'C1_Canvas','EntranceCanopies',True)
    block('CanvasWallClamp',(x,face*12.03,z+.015),(width+.03,.07,.066),'C1_Steel','EntranceCanopies',.006)
    for s in [0,.5,1]:
        a=Vector(surface(s,0));b=Vector(surface(s,1));a.z-=.023;b.z-=.023
        rod('AwningSteelRib',a,b,.018,'C1_Steel','EntranceCanopies',20)
    for t in [0,1]:
        a=Vector(surface(0,t));b=Vector(surface(1,t));a.z-=.023;b.z-=.023
        rod('AwningCrossRail',a,b,.018,'C1_Steel','EntranceCanopies',20)
    # Sewn front valance, an 80 mm restrained drop with doubled hem.
    for i in range(nx):
        p=surface(i/nx,1);q=surface((i+1)/nx,1)
        verts=[(pt[0],pt[1]+d,pt[2]-h) for d in [-.002,.002] for pt,h in [(p,0),(q,0),(q,.085+.003*math.sin(i)),(p,.085+.003*math.sin(i-1))]]
        mesh('CanvasValance',verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'C1_Canvas','EntranceCanopies')
    for s in [0,.33,.67,1]:
        pts=[Vector(surface(s,j/ny))+Vector((0,0,.0025)) for j in range(ny+1)]
        tube('CanvasFelledSeam',pts,.0035,'C1_Canvas','EntranceCanopies',10)
    for i in range(9):
        s=i/8;p=Vector(surface(s,1));p.z-=.017
        # Short lashings bind reinforced edge to the front rail.
        ring=[p+Vector((0,.026*math.sin(k*2*math.pi/16),.026*math.cos(k*2*math.pi/16))) for k in range(17)]
        tube('AwningEdgeLacing',ring,.0028,'C1_Canvas','EntranceCanopies',8)
        fastener('CanvasWallClampScrew',(p.x,face*12.067,z+.015),(0,face,0),.009,'C1_Galvanized','EntranceCanopies')
    for xx in [x-width/2+.04,x+width/2-.04]:
        block('CanopyBracketPlate',(xx,face*12.025,z-.33),(.12,.08,.62),'C1_Steel','EntranceCanopies',.005)
        anglebeam('CanopyArm',(xx,y,z-.025),(xx,edge,z-.263),.045,'EntranceCanopies')
        anglebeam('CanopyDiagonal',(xx,y,z-.62),(xx,edge+.08*(-face),z-.25),.045,'EntranceCanopies')
        for zz in [z-.12,z-.55]:fastener('CanopyWallAnchor',(xx,face*12.067,zz),(0,face,0),.017,'C1_Steel','EntranceCanopies')
production_canopy(5.70,-12.035,1.83,.86,2.94)
production_canopy(0,12.035,2.30,.99,3.14,1)

# Four end-pier crowns: stepped coping, squat fluted neck and small hipped peak
# taken from the concept. Intermediate side piers receive no added ornament.
# Keep the complete approved ornament in front of the end coping, with a real
# 40 mm air gap behind its widest part. Extend only the two supporting cap
# courses outward so the square plinth retains a full bearing and edge margin.
FINIAL_ROOF_GAP = .040
FINIAL_PLACEMENTS = []
for yy,face in [(-12.04,-1),(12.04,1)]:
    end_plane=max(face*v.co.y for o in objects if o.name.startswith(('ArchFascia','ArchOuterCoping','CircularSegmentMasonry')) for v in o.data.vertices)
    finial_y=face*(end_plane+FINIAL_ROOF_GAP+.43/2)
    cap_extension=abs(finial_y)+.43/2+.040-(abs(yy)+.035+.80/2)
    for xx in [-7.54,7.54]:
        upper=next(o for o in objects if o.name.startswith('EndPierUpperCapital') and all(v.co.x*xx>0 and v.co.y*face>0 for v in o.data.vertices))
        for v in upper.data.vertices:
            if face*v.co.y>abs(yy)+.035:v.co.y+=face*cap_extension
        upper.data.update()
        lip=block('EndPierCopingLip',(xx,yy+face*(.035+cap_extension/2),7.285),(1.035,.80+cap_extension,.07),'C1_Trim','MasonryFraming',.012)
        first=len(objects)
        plinth=block('FinialSquarePlinth',(xx,finial_y,7.355),(.43,.43,.075),'C1_Trim','MasonryFraming',.012)
        block('FinialNeck',(xx,finial_y,7.475),(.255,.255,.18),'C1_Trim','MasonryFraming',.016)
        block('FinialCollar',(xx,finial_y,7.565),(.335,.335,.055),'C1_Trim','MasonryFraming',.01)
        vs=[(xx+sx*r,finial_y+sy*r,z) for r,z in [(.137,7.586),(.125,7.745),(.035,7.805)] for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]]
        fs=[(0,3,2,1),(8,9,10,11)]+[(k*4+j,k*4+(j+1)%4,(k+1)*4+(j+1)%4,(k+1)*4+j) for k in range(2) for j in range(4)]
        peak=mesh('HippedMasonryFinial',vs,fs,'C1_Trim','MasonryFraming');bevel_object(peak,.009,2)
        FINIAL_PLACEMENTS.append({'corner':('front' if face<0 else 'rear')+('_left' if xx<0 else '_right'),'face':face,'parts':objects[first:],'plinth':plinth,'lip':lip,'outward_shift':abs(finial_y)-abs(yy),'cap_extension':cap_extension})

# Recessed rolled-steel window frames. Every glass pane sits behind a flange.
for side in [-1,1]:
    for y0,y1,z0,z1 in window_schedules[side]:
        mid=(y0+y1)/2;wide=y1-y0;high=z1-z0;panes=round(wide/.68)
        block('ClerestoryMasonrySill',(side*7.81,mid,z0-.06),(.34,wide+.17,.13),'C1_Trim','MasonryFraming',.012)
        block('ClerestoryLintel',(side*7.80,mid,z1+.045),(.25,wide+.17,.11),'C1_Trim','MasonryFraming',.007)
        for zz in [z0,z1]:
            block('WindowSteelHeadSill',(side*7.77,mid,zz),(.15,wide+.065,.055),'C1_Steel','Clerestory',.004)
            block('WindowGlazingStop',(side*7.826,mid,zz),(.025,wide,.035),'C1_Steel','Clerestory',.003)
        for j in range(panes+1):
            yy=y0+j*wide/panes
            block('MullionWeb',(side*7.75,yy,(z0+z1)/2),(.11,.022,high),'C1_Steel','Clerestory',.003)
            block('MullionFace',(side*7.819,yy,(z0+z1)/2),(.029,.045,high),'C1_Steel','Clerestory',.003)
        block('WindowTransomWeb',(side*7.75,mid,(z0+z1)/2),(.11,wide,.022),'C1_Steel','Clerestory',.003)
        block('WindowTransomFace',(side*7.819,mid,(z0+z1)/2),(.029,wide,.042),'C1_Steel','Clerestory',.003)
        for j in range(panes):
            for row in range(2):
                block('SeatedGlassPane',(side*7.776,y0+(j+.5)*wide/panes,z0+(row+.5)*high/2),(.009,wide/panes-.034,high/2-.031),'C1_GlassStudy','WindowGlass',0)
        rod('BlackoutRoller',(side*7.46,y0-.035,z1+.20),(side*7.46,y1+.035,z1+.20),.075,'C1_Blackout','BlackoutShutters',24)
        for yy in [y0-.025,y1+.025]:
            block('BlackoutRollerBracket',(side*7.50,yy,z1+.18),(.20,.055,.19),'C1_Steel','BlackoutShutters',.004)
            block('BlackoutGuide',(side*7.52,yy,(z0+z1)/2),(.047,.036,high+.07),'C1_Steel','BlackoutShutters',.004)

# Roof sheet standing seams follow the same circular surface; terminate at the
# glazed ribbons. Flashing folds cover the masonry/metal junction at both ends.
seam_eave_end=math.asin(7.575/(RADIUS-.052))  # sealed seam ends upstream of the apron lap
for i in range(47):
    yy=-11.5+i*.50
    for a,b in zip(cuts[:-1],cuts[1:]):
        if (a,b) not in bands:
            shell_segment('RoofStandingSeam',max(a,-seam_eave_end),min(b,seam_eave_end),yy-.011,yy+.011,offset=-.052,thick=.027,mat='C1_Roof',category='RoofSeams')
for yy in [-11.71,11.71]:
    shell_segment('EndAbutmentFlashing',-roof_end,roof_end,yy-.12,yy+.12,offset=-.031,thick=.025,mat='C1_Roof',category='RoofSeams')
for side in [-1,1]:
    block('FoldedEaveApron',(side*7.855,0,7.185),(.08,23.45,.14),'C1_Roof','RoofSeams',.008)
    # Downpipes terminate in shoes just above grade, served by eave collection.
    rod('EaveCollectionPipe',(side*7.996,-11.40,6.97),(side*7.996,11.40,6.97),.058,'C1_Roof','Rainwater',28)
    for yy in [-11.40,11.40]:
        tube('RainwaterDownpipe',fillet([(side*7.996,yy,6.97),(side*8.035,yy,6.75),(side*8.035,yy,.34),(side*8.20,yy,.21)],.10),.045,'C1_Roof','Rainwater',24)
        for zz in [.75,2.55,4.5,6.5]:
            block('DownpipeWallClip',(side*7.995,yy,zz),(.20,.13,.045),'C1_Steel','Rainwater',.005)
            fastener('PipeClipAnchor',(side*8.07,yy+.055,zz),(side,0,0),.009,'C1_Steel','Rainwater')

for a,b in bands:
    shell_segment('OpaqueRoofBlackout',a-.005,b+.005,-10.24,10.24,offset=-.285,thick=.048,mat='C1_Blackout',category='BlackoutShutters')
    for ang in [a,b]:
        shell_segment('RoofRibbonCurb',ang-.007,ang+.007,-10.28,10.28,offset=.007,thick=.18,mat='C1_Steel',category='RoofGlazing')
        shell_segment('BlackoutRunner',ang-.004,ang+.004,-10.28,10.28,offset=-.285,thick=.085,mat='C1_Steel',category='BlackoutShutters')

# Interior timber roof lining, with narrow board joints and intentional end seams.
def ceiling_board(a,b,y0,y1,offset):
    # Shallow V-joint with a continuous tongue backing. No light-leaking slits.
    outer=RADIUS+offset;inner=outer-.027;chamfer=.0025/RADIUS
    profile=[(a,outer),(b,outer),(b,inner+.003),(b-chamfer,inner),(a+chamfer,inner),(a,inner+.003)]
    vs=[(r*math.sin(t),y,CIRCLE_Z+r*math.cos(t)) for y in [y0,y1] for t,r in profile]
    n=len(profile)
    fs=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
    return mesh('TongueAndGrooveRoofLining',vs,fs,'C1_RoofTimber','InteriorRoofLining')

lining_end=math.asin(7.43/(RADIUS-.252))
boards=round(2*lining_end*(RADIUS-.252)/.15)
for j in range(boards):
    a=-lining_end+2*lining_end*j/boards;b=-lining_end+2*lining_end*(j+1)/boards
    for y0,y1 in [(-11.68,-5.6),(-5.59,0),(.01,5.6),(5.61,11.68)]:
        in_ribbon=any(lo<(a+b)/2<hi for lo,hi in bands)
        if in_ribbon:
            # Glazing must open onto the blackout top, not intersect roof boards.
            if y0<-10.2:ceiling_board(a,b,y0,-10.2,-.252)
            if y1>10.2:ceiling_board(a,b,10.2,y1,-.252)
            lo=max(y0,-10.2);hi=min(y1,10.2)
            if hi>lo:ceiling_board(a,b,lo,hi,-.331)
        else:ceiling_board(a,b,y0,y1,-.252)

for yy in [-7.85,0,7.85]:
    block('VentFlashedCurb',(0,yy,10.80),(1.15,1.00,.25),'C1_Roof','RoofVents',.022)
    block('VentThroat',(0,yy,11.02),(.76,.66,.34),'C1_Blackout','RoofVents',.01)
    for xx in [-.43,.43]:
        for y0 in [yy-.37,yy+.37]:block('VentCornerPost',(xx,y0,11.10),(.055,.055,.48),'C1_Steel','RoofVents',.006)
    for zz in [10.92,11.025,11.13,11.235]:
        for face in [-1,1]:
            block('VentLouverBlade',(0,yy+face*.37,zz),(.86,.075,.048),'C1_Roof','RoofVents',.009)
            block('VentSideLouver',(face*.43,yy,zz),(.075,.73,.048),'C1_Roof','RoofVents',.009)
    block('VentCapFold',(0,yy,11.38),(1.12,.99,.095),'C1_Roof','RoofVents',.025)
for xx,zz,w,h in [(-2.60,8.04,.63,.78),(0,9.06,.62,.97),(2.60,8.04,.63,.78)]:
    block('RearVentRecess',(xx,12.007,zz),(w,.035,h),'C1_Blackout','RearVentilation',.006)
    for sx in [-1,1]:block('RearVentJamb',(xx+sx*w/2,12.04,zz),(.045,.10,h+.045),'C1_Steel','RearVentilation',.004)
    for z0 in [zz-h/2,zz+h/2]:block('RearVentFrame',(xx,12.04,z0),(w+.045,.10,.045),'C1_Steel','RearVentilation',.004)
    for j in range(round(h/.11)):block('RearVentLouver',(xx,12.06,zz-h/2+.07+j*.11),(w-.04,.075,.052),'C1_Steel','RearVentilation',.008)

# Structural construction, circulation, services and UV assignment continue in a
# separate source module for readable ownership; they share this export scene.
exec(compile((HERE/'production_interior.py').read_text(encoding='utf-8'),str(HERE/'production_interior.py'),'exec'),globals())
