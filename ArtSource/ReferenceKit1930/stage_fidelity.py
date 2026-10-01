"""Permanent construction refinement, executed within generate_stage1.py.
Metres; no movable props, stage text, weathering or gameplay changes.
"""

# Replace weak assemblies rather than layering more primitives over them.
for group_name in ('Structure','Rigging','Catwalk','Stair'):
    for o in list(groups[group_name]):
        objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
    groups[group_name]=[]
remove_parts(('Lamp','EnamelBellShade','RolledLampRim','ServicePortal',
              'FoldedServiceCanopy','CanopyBracket','CanopySeam','CanopyDrip',
              'RearServiceHeader','RearLampSupply','FeedConduit','ConduitClamp',
              'WallJoint','HingeFramePlate'))

def smooth(o):
    for f in o.data.polygons:f.use_smooth=True
    return o

def tube(name,points,r=.022,mat='ADM_WindowSteel',group='Utilities',sides=12):
    """Continuous tube with tangent-oriented rings; capped intentional endpoints."""
    pts=[Vector(p) for p in points];vv=[]
    for i,p in enumerate(pts):
        tangent=pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]
        q=tangent.to_track_quat('Z','Y')
        vv.extend(p+q@Vector((r*math.cos(j*2*math.pi/sides),r*math.sin(j*2*math.pi/sides),0)) for j in range(sides))
    ff=[(i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j) for i in range(len(pts)-1) for j in range(sides)]
    ff += [tuple(reversed(range(sides))),tuple(range((len(pts)-1)*sides,len(pts)*sides))]
    return smooth(mesh(name,vv,ff,mat,group))

def beam(name,a,b,w=.16,h=.22,group='Structure'):
    """Rolled I-section; webs and flanges meet along their full length."""
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');t=.018
    profile=[(-w/2,-h/2),(w/2,-h/2),(w/2,-h/2+t),(t/2,-h/2+t),(t/2,h/2-t),(w/2,h/2-t),(w/2,h/2),(-w/2,h/2),(-w/2,h/2-t),(-t/2,h/2-t),(-t/2,-h/2+t),(-w/2,-h/2+t)]
    vv=[p+q@Vector((x,y,0)) for p in (a,b) for x,y in profile];n=len(profile)
    ff=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(reversed(range(n))),tuple(range(n,n*2))]
    return mesh(name,vv,ff,'ADM_WindowSteel',group)

def plate(name,pos,size,group='Structure',mat='ADM_WindowSteel'):
    return detail(name,pos,size,mat,group,.003)

def grate(name,x,y,z,w,d,group):
    """Open steel bar grating in one mesh, not a flat painted strip."""
    vv=[];ff=[]
    def bar(cx,cy,sx,sy):
        n=len(vv);hh=.035
        vv.extend([(cx+dx,cy+dy,z+dz) for dz in (-hh,0) for dx,dy in ((-sx/2,-sy/2),(sx/2,-sy/2),(sx/2,sy/2),(-sx/2,sy/2))])
        ff.extend(tuple(n+k for k in f) for f in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)))
    for i in range(max(2,int(w/.075))+1):
        bar(x-w/2+i*w/max(2,int(w/.075)),y,.008,d)
    for i in range(max(2,int(d/.25))+1):
        bar(x,y-d/2+i*d/max(2,int(d/.25)),w,.009)
    for s in (-1,1):bar(x+s*w/2,y,.025,d);bar(x,y+s*d/2,w,.025)
    return mesh(name,vv,ff,'ADM_WindowSteel',group)

# Layered masonry framing; restrained industrial caps, returns and joints.
for x in (-7.64,7.64):
    detail('PierInnerLayer',(x,-12.19,3.65),(.83,.38,7.30),'ADM_Stucco')
    detail('PierNeckCourse',(x,-12.29,7.20),(.87,.68,.10))
    detail('PierCopingReturn',(x,-12.16,7.70),(.92,.95,.10))
    for z in (.94,3.7,5.9):
        detail('PierConstructionJoint',(x,-12.48,z),(.51,.008,.012),'STG_Foundation',bevel=0)
for x in (-3.02,3.02):
    detail('PortalPlinth',(x,-12.21,.24),(.52,.74,.48),'STG_Foundation')
    detail('PortalImpost',(x,-12.24,6.02),(.58,.72,.12))
    for z in (2.05,4.03):detail('PortalBedJoint',(x,-12.515,z),(.43,.006,.011),'STG_Foundation',bevel=0)
detail('PortalHeadCourse',(0,-12.19,6.59),(6.56,.53,.18),'ADM_Stucco')
detail('MainThreshold',(0,-12.08,.035),(5.6,.65,.07),'STG_Foundation','DoorFrames')
for side in (-1,1):
    for y in (-4,4):
        detail('BayPierLayer',(side*7.98,y,3.72),(.43,.66,7.4),'ADM_Stucco')
        detail('BayPierFoot',(side*8.0,y,.45),(.48,.74,.9),'STG_Foundation')
    # Independent precast plinth blocks, shallow open joints and bevelled top.
    for j in range(12):
        detail('ConcretePlinthBlock',(side*8.005,-11+j*2,.33),(.32,1.986,.64),'STG_Foundation','Foundation',.006)
    for y in (-8,0,8):
        for k in range(1,6):
            yy=y-2.6+k*5.2/6
            detail('SteelWindowMullion',(side*7.995,yy,6.2),(.10,.045,1.08),'ADM_WindowSteel','Windows',.003)
        detail('SteelWindowTransom',(side*7.995,y,6.2),(.10,5.1,.04),'ADM_WindowSteel','Windows',.003)
        detail('WindowHeadFlashing',(side*8.06,y,6.93),(.39,5.52,.035),'STG_ZincRoof')

# Door hardware: reveal-mounted fixed leaves, interior framing and latching.
for side in (-1,1):
    grp='StageDoorLeft' if side<0 else 'StageDoorRight';cx=side*1.4
    for z in (.30,2.12,4.12,5.65):
        detail('FixedHingeReturn',(side*2.808,-12.06,z),(.028,.29,.19),'ADM_WindowSteel','DoorFrames',.003)
        for dy in (-12.14,-11.99):
            tube('HingeAnchorBolt',[(side*2.79,dy,z),(side*2.84,dy,z)],.012,group='DoorFrames')
    for x in (cx-1.29,cx+1.29):plate('InsideDoorStile',(x,-11.905,3),(.12,.065,5.9),grp)
    for z in (.18,2.05,4.03,5.84):
        plate('InsideDoorRail',(cx,-11.88,z),(2.66,.10,.13),grp)
        for x in (cx-1.15,cx+1.15):tube('InsideRailBolt',[(x,-11.82,z),(x,-11.80,z)],.014,group=grp)
    for lo,hi in ((.28,1.97),(2.14,3.94),(4.12,5.76)):
        strip('InsideDoorBrace',(cx-side*1.22,-11.86,lo),(cx+side*1.22,-11.86,hi),.09,.045,group=grp)
    hx=side*.29
    tube('DropBolt',[(hx,-11.77,.08),(hx,-11.77,.78)],.019,group=grp)
    for z in (.18,.66):plate('DropBoltGuide',(hx,-11.83,z),(.10,.13,.09),grp)
    tube('DropBoltHandle',[(hx,-11.77,.72),(hx+side*.12,-11.77,.72)],.018,'ADM_Bronze',grp)
    plate('ThresholdBoltSocket',(hx,-11.78,.018),(.13,.15,.035),'DoorFrames')
plate('DoorAstragal',(.025,-12.18,3),(.048,.035,5.96),'StageDoorRight')
plate('LatchCase',(.13,-12.23,1.44),(.20,.09,.11),'StageDoorRight')
tube('DoorLatchBar',[(-.20,-12.29,1.44),(.20,-12.29,1.44)],.016,'ADM_Bronze','StageDoorRight')
plate('LatchKeeper',(-.22,-12.23,1.44),(.065,.11,.13),'StageDoorLeft')

def shade(name,center,group='WorkLighting',radius=.27):
    # Smooth spun-metal bell with rolled rim, hollow underside and socket.
    x,y,z=center;n=40
    profile=[(.048,0),(.052,-.035),(.07,-.07),(.105,-.12),(.16,-.18),(radius*.88,-.235),(radius,-.25),(radius,-.263)]
    vv=[(x+r*math.cos(i*2*math.pi/n),y+r*math.sin(i*2*math.pi/n),z+h) for r,h in profile for i in range(n)]
    ff=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]
    o=smooth(mesh(name,vv,ff,'ADM_WindowSteel',group));m=o.modifiers.new('Spun enamel thickness','SOLIDIFY');m.thickness=.006
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    tube(name+'Socket',[(x,y,z+.04),(x,y,z-.13)],.038,group=group,sides=20)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=.067,location=(x,y,z-.20))
    smooth(finish(bpy.context.object,name+'Bulb','ADM_LampDiffuser','LampSources'))

def wall_lamp(x,y,z,sign=-1):
    tube('LampMount',[(x,y,z),(x,y+sign*.12,z)],.085,sides=24,group='WorkLighting')
    pp=[(x,y+sign*(.12+.25*(1-math.cos(math.pi*i/24))),z+.25*math.sin(math.pi*i/24)) for i in range(25)]
    pp.append((x,y+sign*.62,z-.055))
    tube('CastGooseneck',pp,.022,group='WorkLighting',sides=12)
    shade('WallEnamelShade',(x,y+sign*.62,z-.075))

# One lamp per personnel entrance; no duplicated main-door lamp cluster.
for x,y,angle in ((-5.8,-12,0),(5.8,-12,0),(0,12,math.pi)):
    s=-1 if y<0 else 1;grp='PersonnelFront' if x<0 else ('PersonnelRear' if y>0 else 'PersonnelRight')
    for dx in (-.60,.60):detail('ServiceReveal',(x+dx,y+s*.06,1.10),(.18,.46,2.20),'STG_Foundation','DoorFrames')
    detail('ServiceHead',(x,y+s*.08,2.25),(1.38,.47,.20),'STG_Foundation','DoorFrames')
    for dx in (-.36,.36):plate('PersonnelStile',(x+dx,y+s*.09,1.1),(.07,.035,2.03),grp)
    for z in (.20,.95,2.02):plate('PersonnelRail',(x,y+s*.09,z),(.79,.035,.075),grp)
    # Interior jamb stops, panel relief, operable lever and spindle.
    for dx in (-.47,.47):plate('InsidePersonnelStop',(x+dx,y-s*.14,1.08),(.04,.07,2.15),'DoorFrames')
    for z in (.20,.95,2.02):plate('InsidePersonnelRail',(x,y-s*.08,z),(.81,.035,.07),grp)
    hx=x+(-.34 if y>0 else .34)
    plate('InsideLeverRose',(hx,y-s*.095,1.02),(.07,.035,.15),grp,'ADM_Bronze')
    tube('InsideLever',[(hx,y-s*.09,1.02),(hx,y-s*.16,1.02),(hx-.12,y-s*.16,1.02)],.013,'ADM_Bronze',grp)
    canopy=detail('ServiceRainHood',(x,y+s*.49,2.65),(1.63,.98,.045),'ADM_WindowSteel',bevel=.012);canopy.rotation_euler.x=-s*math.radians(12)
    detail('HoodWallFlashing',(x,y+s*.18,2.76),(1.67,.05,.16),'STG_ZincRoof')
    detail('HoodFoldedFascia',(x,y+s*.97,2.53),(1.66,.045,.12),'ADM_WindowSteel')
    for dx in (-.66,.66):
        strip('HoodBracket',(x+dx,y+s*.17,2.27),(x+dx,y+s*.89,2.57),.045,.05,group='ArchitecturalDetails')
        plate('HoodAnchor',(x+dx,y+s*.17,2.34),(.09,.035,.28),'ArchitecturalDetails')
    wall_lamp(x-.88,y+s*.16,2.48,s)
    # Junction to fixture, through-wall supply terminates visibly in box.
    plate('EntranceJunction',(x-.88,y+s*.20,1.82),(.18,.14,.23),'Utilities')
    tube('EntranceLampFeed',[(x-.88,y+s*.20,1.94),(x-.88,y+s*.20,2.48)],.014)
    tube('EntranceWallFeed',[(x-.88,y+s*.20,1.82),(x-.88,y,1.82)],.021)

# Service topology: rear wall penetration -> panel -> junction -> interior trunk.
tube('ServiceEntry',[(-6,12.0,1.0),(-6,12.48,1.0)],.037)
plate('ServiceEntryFlange',(-6,12.16,1.0),(.22,.05,.22),'Utilities')
for x in (-6,-5.1):
    tube('PanelRiser',[(x,12.23,1.90),(x,12.23,2.7)],.024)
    plate('PanelJunction',(x,12.23,2.7),(.18,.15,.18),'Utilities')
tube('PanelLink',[(-6,12.23,2.7),(-5.1,12.23,2.7)],.024)
tube('InteriorFeedPenetration',[(-5.1,12.23,2.7),(-5.1,11.64,2.7)],.03)
plate('InteriorMainBoard',(-5.1,11.67,1.50),(1.1,.32,1.7),'InteriorServices')
for dx in (-.26,.26):
    plate('BoardDoor',(-5.1+dx,11.49,1.5),(.50,.04,1.55),'InteriorServices','STG_ZincRoof')
    plate('BoardHandle',(-5.1+dx+.15,11.43,1.5),(.035,.055,.18),'InteriorServices','ADM_Bronze')
tube('BoardFeed',[(-5.1,11.64,2.7),(-5.1,11.64,2.35)],.03,group='InteriorServices')
tube('PowerTrunk',[(-5.1,11.64,2.7),(7.55,11.64,2.7),(7.55,-10.0,2.7)],.034,group='InteriorServices')
for y in (-10,-5,0,5,10):
    plate('PowerJunction',(7.55,y,2.7),(.20,.18,.20),'InteriorServices')
    tube('PowerDrop',[(7.55,y,2.7),(7.55,y,1.4)],.026,group='InteriorServices')
    plate('StagePowerOutlet',(7.54,y,1.12),(.22,.44,.52),'InteriorServices')
    for z in (1.0,1.22):tube('BakeliteSocket',[(7.41,y,z),(7.38,y,z)],.06,'STG_DarkDoors','InteriorServices')
    for z in (1.65,2.35):plate('PowerSaddle',(7.62,y,z),(.17,.075,.04),'InteriorServices')
    # Upper lighting junction, branch and wall penetration to permanent fixture.
    tube('WorkLightFeed',[(7.55,y,2.7),(7.55,y,6.72),(4.8,y,6.72)],.018,group='InteriorServices')
    plate('LightJunction',(4.8,y,6.65),(.17,.17,.16),'InteriorServices')
    tube('PendantStem',[(4.8,y,6.65),(4.8,y,6.18)],.022,group='WorkLighting')
    shade('PendantEnamel',(4.8,y,6.18),radius=.32)

# Engineered bowstring roof trusses and wall shoes. No supports on the floor.
ys=(-10.7,-5.35,0,5.35,10.7)
for y in ys:
    for side in (-1,1):
        beam('RolledWallColumn',(side*7.53,y,.10),(side*7.53,y,7.45),.24,.28)
        plate('ColumnBase',(side*7.53,y,.09),(.42,.42,.12))
        plate('TrussBearing',(side*7.53,y,7.42),(.54,.46,.12))
    beam('TrussLowerChord',(-7.53,y,7.48),(7.53,y,7.48),.17,.22)
    nodes=[]
    for i in range(13):
        x=-7.53+i*15.06/12;z=7.6+2.73*math.sqrt(max(0,1-(x/7.70)**2));nodes.append((x,y,z))
    for i in range(12):
        beam('TrussUpperChord',nodes[i],nodes[i+1],.16,.20)
        low=(nodes[i][0],y,7.48);high=nodes[i+1]
        beam('TrussDiagonal',low,high,.085,.09)
    for x,_,z in nodes:
        beam('TrussVertical',(x,y,7.48),(x,y,z),.075,.09)
        for zz in (7.48,z):
            plate('TrussGusset',(x,y-.115,zz),(.25,.018,.28))
            for dx in (-.075,.075):bolt('GussetRivet',x+dx,y-.13,zz,'Structure')
for x in (-6,-3,0,3,6):
    z=7.6+2.73*math.sqrt(1-(x/7.7)**2)
    beam('RoofPurlin',(x,-11.8,z),(x,11.8,z),.12,.15)

# Separate supported rigging pipe grid below structural ties, hanging collars.
for x in (-4.8,-2.4,0,2.4,4.8):
    tube('RiggingPipe',[(x,-10.7,6.65),(x,10.7,6.65)],.040,group='Rigging')
    for y in ys:
        tube('GridHanger',[(x,y,7.48),(x,y,6.65)],.016,group='Rigging')
        plate('GridSuspensionClamp',(x,y,6.65),(.11,.11,.10),'Rigging')
for y in ys:tube('RiggingCrossbar',[(-6.4,y,6.72),(5.0,y,6.72)],.040,group='Rigging')

# Real access decks. Rear crossover remains aligned with top stair landing.
grate('SideGrating',-7.05,0,4.5,1.15,22,'Catwalk')
grate('RearGrating',.03,10.7,4.5,12.96,1.0,'Catwalk')
for x in (-7.64,-6.46):beam('CatwalkEdgeBeam',(x,-11,4.38),(x,11.2,4.38),.10,.18,'Catwalk')
for y in (10.2,11.2):beam('RearEdgeBeam',(-6.46,y,4.38),(6.51,y,4.38),.10,.18,'Catwalk')
for y in ys:
    beam('CatwalkCantilever',(-7.54,y,4.32),(-6.45,y,4.32),.10,.15,'Catwalk')
    beam('CatwalkKnee',(-7.54,y,3.45),(-6.45,y,4.30),.075,.10,'Catwalk')
    plate('CatwalkWallPlate',(-7.63,y,3.9),(.09,.23,1.05),'Catwalk')
for x in (-5.8,0,6.4):
    beam('RearDeckBearer',(x,11.82,4.32),(x,10.18,4.32),.10,.15,'Catwalk')
    beam('RearDeckKnee',(x,11.82,3.4),(x,10.18,4.30),.09,.12,'Catwalk')
def railing(a,b):
    a,b=Vector(a),Vector(b);n=math.ceil((b-a).length/1.35)
    for h in (.55,1.1):tube('ContinuousGuard',[a+Vector((0,0,h)),b+Vector((0,0,h))],.022,group='Catwalk')
    delta=b-a
    box('KickPlate',(a+b)/2+Vector((0,0,.065)),(max(abs(delta.x),.012),max(abs(delta.y),.012),.13),'ADM_WindowSteel','Catwalk')
    for i in range(n+1):
        p=a+(b-a)*i/n;tube('GuardStanchion',[p,p+Vector((0,0,1.1))],.025,group='Catwalk')
        plate('GuardFoot',p+Vector((0,0,.012)),(.12,.12,.025),'Catwalk')
railing((-6.46,-11,4.5),(-6.46,10.2,4.5))
railing((-7.64,-11,4.5),(-6.46,-11,4.5))
railing((-5.30,10.2,4.5),(6.51,10.2,4.5))
railing((6.51,10.2,4.5),(6.51,11.2,4.5))
railing((-6.46,11.2,4.5),(6.51,11.2,4.5))
for i in range(24):
    y=3.15+i*.30;z=(i+1)*.1875
    grate('StairGrating',-5.88,y,z,1.08,.285,'Stair')
    plate('TreadNosing',(-5.88,y-.14,z-.018),(1.08,.038,.035),'Stair')
    for x in (-6.40,-5.36):plate('TreadEndPlate',(x,y,z-.055),(.045,.29,.11),'Stair')
for x in (-6.46,-5.30):
    beam('StairChannel',(x,3.0,.02),(x,10.2,4.52),.09,.24,'Stair')
    tube('StairHandrail',[(x,2.70,1.10),(x,3.0,1.10),(x,10.2,5.60),(x,10.6,5.60)],.025,group='Stair')
    for i in range(0,25,4):
        y=3+i*.3;z=i*.1875
        tube('StairStanchion',[(x,y,z),(x,y,z+1.1)],.023,group='Stair')
    plate('StairGroundShoe',(x,3.0,.04),(.23,.30,.08),'Stair')
    plate('StairLandingConnection',(x,10.20,4.35),(.20,.18,.30),'Stair')
detail('StairBottomLanding',(-5.88,2.50,.025),(1.42,1.05,.05),'STG_ConcreteFloor','Floor')

# Durable floor and lower wall, calm joints; upper masonry remains clear.
for side in (-1,1):
    detail('InteriorDado',(side*7.71,0,.56),(.07,23.6,1.12),'STG_Foundation','InteriorServices')
    for y in (-8,-3,2,7):
        detail('AcousticBatten',(side*7.68,y,4.78),(.055,3.50,.055),'STG_DarkDoors','InteriorServices')
        # Timber-framed fibreboard lining above the service zone, not modern foam.
        detail('FibreboardLining',(side*7.72,y,4.2),(.055,3.45,1.10),'STG_Foundation','InteriorServices',.003)
for x in (-3.95,0,3.95):box('FloorJoint',(x,0,.002),(.008,23.7,.002),'STG_DarkDoors','Floor')
for y in (-8,-4,0,4,8):box('FloorJoint',(0,y,.002),(15.5,.008,.002),'STG_DarkDoors','Floor')

# Smooth original roof surface; keep the standing seams and opaque roof lights.
for o in objects:
    if o.name.startswith(('BarrelRoof','Rainwater','DoorPull','VerticalDoorPull')):smooth(o)
for y in (-8,0,8):
    detail('VentApronFlashing',(0,y,10.65),(1.40,1.30,.035),'STG_ZincRoof','Roof')
    for x in (-.46,.46):detail('VentCornerPost',(x,y,11.03),(.06,.78,.54),'ADM_WindowSteel','Roof')

# Deterministic material micro-detail, exported independently of shared ADM maps.
# Scalar maps multiply authored colors; no lighting or localized grime is baked.
import numpy as np
texdir=ROOT/'Assets/SilverScreen/Environment/ReferenceKit/StageTextures';texdir.mkdir(exist_ok=True)
N=512;rng=np.random.default_rng(1930);v,u=np.mgrid[0:N,0:N]/N
def save_map(name,rgb,alpha=None):
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    if name.endswith(('Normal','Surface')):im.colorspace_settings.name='Non-Color'
    rgba=np.ones((N,N,4),dtype=np.float32);rgba[:,:,:3]=rgb
    if alpha is not None:rgba[:,:,3]=alpha
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(texdir/(name+'.png'));im.file_format='PNG';im.save()
for family in ('Timber','Concrete','Floor'):
    grain=rng.normal(0,1,(N,N))
    if family=='Timber':
        height=.5+.14*np.sin(2*math.pi*(u*83+.30*np.sin(v*2*math.pi)))+.035*grain
        tone=np.clip(.86+.22*(height-.5)+.035*np.sin(u*2*math.pi*7),.73,.98)
    else:
        height=.5+.08*grain+.025*np.sin(u*2*math.pi*5)*np.sin(v*2*math.pi*7)
        tone=np.clip(.95+.06*(height-.5),.88,1)
    save_map('STG_'+family+'Color',np.repeat(tone[:,:,None],3,axis=2))
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*.35
    dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*.35
    normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);normal/=np.linalg.norm(normal,axis=2)[:,:,None]
    save_map('STG_'+family+'Normal',normal*.5+.5)
    metallic=.12 if family=='Timber' else 0
    rough=.58 if family=='Timber' else (.84 if family=='Concrete' else .83)
    save_map('STG_'+family+'Surface',np.full((N,N,3),metallic),np.clip(1-rough+(height-.5)*.12,0,1))
    p=materials['STG_DarkDoors' if family=='Timber' else ('STG_Foundation' if family=='Concrete' else 'STG_ConcreteFloor')].node_tree
    image_node=p.nodes.new('ShaderNodeTexImage');image_node.image=bpy.data.images.get('STG_'+family+'Color')
    # Keep authored base color authoritative; Unity uses the same multiplier map.
    base=p.nodes.get('Principled BSDF');mix=p.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
    mix.inputs[1].default_value=base.inputs['Base Color'].default_value
    p.links.new(image_node.outputs['Color'],mix.inputs[2]);p.links.new(mix.outputs[0],base.inputs['Base Color'])
    normal_tex=p.nodes.new('ShaderNodeTexImage');normal_tex.image=bpy.data.images.get('STG_'+family+'Normal')
    normal_node=p.nodes.new('ShaderNodeNormalMap');normal_node.inputs['Strength'].default_value=.28 if family=='Timber' else .16
    p.links.new(normal_tex.outputs['Color'],normal_node.inputs['Color']);p.links.new(normal_node.outputs['Normal'],base.inputs['Normal'])
    surface=p.nodes.new('ShaderNodeTexImage');surface.image=bpy.data.images.get('STG_'+family+'Surface')
    invert=p.nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
    p.links.new(surface.outputs['Alpha'],invert.inputs[1]);p.links.new(invert.outputs[0],base.inputs['Roughness'])
