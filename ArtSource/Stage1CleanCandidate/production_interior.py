"""Detailed structural members, circulation, fixed services, lighting and UVs.
Executed inside production_geometry.py; no independent export path.
"""

def curved_ibeam(yy):
    profile=i_profile(.205,.23,.023,.014);n=len(profile)
    count=112;verts=[];faces=[]
    for k in range(count+1):
        angle=truss_angles[0]+(truss_angles[-1]-truss_angles[0])*k/count
        for u,v in profile:
            rr=RADIUS-.34+v
            verts.append((rr*math.sin(angle),yy+u,CIRCLE_Z+rr*math.cos(angle)))
    for k in range(count):
        for j in range(n):faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    faces.extend([tuple(reversed(range(n))),tuple(count*n+j for j in range(n))])
    o=mesh('RolledBowstringChord',verts,faces,'C1_StructuralSteel','PrimaryStructure')
    for face in o.data.polygons[:-2]:face.use_smooth=True
    # Custom split normals are authored later to retain hard flange edges.
    for edge in o.data.edges:
        if abs(edge.vertices[0]-edge.vertices[1])==n:edge.use_edge_sharp=True
    return o

for yy in FRAME_Y:
    curved_ibeam(yy)
    ibeam('BowstringBottomChord',(-column_x,yy,TIE),(column_x,yy,TIE),.21,.25)
    for side in [-1,1]:
        x=side*column_x
        ibeam('SteelWallStanchion',(x,yy,FLOOR+.06),(x,yy,truss_top[-1]-.01),.27,.29)
        block('StanchionBaseplate',(x,yy,FLOOR+.0425),(.49,.50,.085),'C1_StructuralSteel','StructuralConnections',.005)
        block('TrussBearingPlate',(x,yy,TIE-.125),(.42,.43,.045),'C1_StructuralSteel','StructuralConnections',.008)
        for dx in [-.18,.18]:
            for dy in [-.18,.18]:fastener('FoundationAnchor',(x+dx,yy+dy,FLOOR+.09),(0,0,1),.024,'C1_Galvanized','StructuralConnections')
        for dy in [-.17,.17]:
            plate_xz('TrussHeelGusset',[(x-side*.27,TIE-.19),(x+side*.17,TIE-.19),(x+side*.17,TIE+.20),(x-side*.27,TIE+.43)],yy+dy,.022)
    for i,x in enumerate(truss_x[1:-1],1):
        anglebeam('TrussVerticalAngle',(x,yy,TIE),(x,yy,truss_top[i]-.045),.088)
    for i in range(8):
        a=(truss_x[i],yy,TIE) if i<4 else (truss_x[i],yy,truss_top[i]-.045)
        b=(truss_x[i+1],yy,truss_top[i+1]-.045) if i<4 else (truss_x[i+1],yy,TIE)
        # Paired angles form an open built-up member instead of a solid bar.
        for dy in [-.035,.035]:anglebeam('PairedDiagonalAngle',(a[0],yy+dy,a[2]),(b[0],yy+dy,b[2]),.074)
    for i,x in enumerate(truss_x[1:-1],1):
        for z,sign in [(TIE,1),(truss_top[i]-.04,-1)]:
            poly=[(x-.22,z-sign*.04),(x+.22,z-sign*.04),(x+.14,z+sign*.27),(x-.14,z+sign*.27)]
            for dy in [-.113,.113]:
                plate_xz('RivetedNodeGusset',poly,yy+dy,.018)
                for dx,dz in [(-.14,.03),(.14,.03),(-.08,.18),(.08,.18)]:
                    fastener('TrussNodeRivet',(x+dx,yy+dy+math.copysign(.012,dy),z+sign*dz),(0,math.copysign(1,dy),0),.014,'C1_StructuralSteel','StructuralConnections')

for x in [-6.15,-3.60,0,3.60,6.15]:
    z=arch_z(x,-.34)-.005
    ibeam('RoofPurlin',(x,-11.60,z),(x,11.60,z),.115,.19)
    for yy in FRAME_Y:
        block('PurlinCleat',(x,yy,z-.045),(.19,.15,.16),'C1_StructuralSteel','StructuralConnections',.003)
# Two end-bay roof-plane diagonals stabilize the row of transverse trusses.
for y0,y1 in [(-11.2,-5.6),(5.6,11.2)]:
    for side in [-1,1]:
        for a,b in [((0,y0,arch_z(0,-.36)),(side*6.15,y1,arch_z(6.15,-.36))),((side*6.15,y0,arch_z(6.15,-.36)),(0,y1,arch_z(0,-.36)))]:
            rod('RoofPlaneTieRod',a,b,.014,'C1_StructuralSteel','PrimaryStructure',12)

# Rigging grid with real channels, hangers, tie clamps and crossing saddles.
rigging_rows=[-5.72,-3.8,-1.9,0,1.9,3.8,5.72]
rigging_crossings=[-11.2+i*1.4 for i in range(14)]
for x in rigging_rows:
    ibeam('RiggingLongitudinalChannel',(x,-11.2,GRID),(x,7.0,GRID),.09,.15,'RiggingPrimary')
    for yy in FRAME_Y[:-1]:
        rod('GridSuspensionRod',(x,yy,GRID+.075),(x,yy,TIE-.10),.013,'C1_Galvanized','RiggingPrimary',16)
        for zz in [GRID+.085,TIE-.105]:
            block('GridClampPlate',(x,yy,zz),(.19,.19,.024),'C1_Steel','RiggingPrimary',.004)
            for dx in [-.063,.063]:fastener('GridClampNut',(x+dx,yy,zz+.02),(0,0,1),.012,'C1_Galvanized','RiggingPrimary')
        rod('SuspensionTurnbuckle',(x,yy,6.90),(x,yy,7.05),.028,'C1_Galvanized','RiggingPrimary',16)
    # High carriers span the last roof bay, keeping the stair route above 2 m
    # headroom while supporting the low grid's rear edge over the filming floor.
    ibeam('RearHighRiggingCarrier',(x,5.6,7.22),(x,11.2,7.22),.105,.15,'RiggingPrimary')
    rod('RearGridHanger',(x,7.0,GRID+.075),(x,7.0,7.22),.013,'C1_Galvanized','RiggingPrimary',16)
    for yy in [5.6,11.2]:block('HighCarrierTrussCleat',(x,yy,7.25),(.17,.20,.17),'C1_Steel','RiggingPrimary',.005)
for yy in rigging_crossings:
    rod('LightingGridBatten',(-5.88,yy,GRID-.108),(5.88,yy,GRID-.108),.029,'C1_Steel','RiggingPrimary',20)
    for xx in rigging_rows:
        block('GridCrossingSaddle',(xx,yy,GRID-.07),(.14,.10,.10),'C1_Steel','RiggingPrimary',.008)
        for dx in [-.054,.054]:fastener('SaddleClampingBolt',(xx+dx,yy,GRID-.12),(0,0,-1),.011,'C1_Galvanized','RiggingPrimary')

def grating(name,x0,x1,y0,y1,z,cat='CirculationAllowances',spacing=.052):
    # Open bar grating: no hidden opaque plate under the grid.
    n=max(1,round((y1-y0)/spacing))
    for i in range(n+1):
        yy=y0+(y1-y0)*i/n
        block(name+'BearingBar',((x0+x1)/2,yy,z-.021),(x1-x0,.006,.042),'C1_Galvanized',cat,0)
    for xx in [x0,x1]:block(name+'EdgeBand',(xx,(y0+y1)/2,z-.021),(.018,y1-y0+.012,.048),'C1_Steel',cat,.003)
    n=max(1,round((x1-x0)/.26))
    for i in range(n+1):block(name+'CrossTie',(x0+i*(x1-x0)/n,(y0+y1)/2,z-.012),(.009,y1-y0,.009),'C1_Galvanized',cat,0)

def guard_post(x,y,z,cat='CirculationGuards'):
    rod('TubularGuardPost',(x,y,z+.045),(x,y,z+1.10),.021,'C1_Steel',cat,20)
    block('RailPostFoot',(x,y,z+.025),(.12,.12,.034),'C1_Steel',cat,.008)
    for dx in [-.038,.038]:fastener('RailFootBolt',(x+dx,y,z+.047),(0,0,1),.010,'C1_Galvanized',cat)

def straight_guard(a,b,z,toe=True):
    a,b=Vector((a[0],a[1],z)),Vector((b[0],b[1],z));length=(b-a).length
    for dz in [.55,1.10]:rod('ContinuousTubeRail',a+Vector((0,0,dz)),b+Vector((0,0,dz)),.021,'C1_Steel','CirculationGuards',20)
    n=max(1,math.ceil(length/1.55))
    for i in range(n+1):
        p=a.lerp(b,i/n);guard_post(p.x,p.y,z)
    if toe:extrusion('ToeBoard',[(-.004,0),(.004,0),(.004,.10),(-.004,.10)],a,b,'C1_Steel','CirculationGuards')

for side in [-1,1]:
    x0,x1=sorted([side*walk_inner,side*walk_outer])
    grating('SideCatwalk',x0,x1,-11.2,11.2,CATWALK)
    for xx in [side*(walk_inner+.055),side*(walk_outer-.055)]:ibeam('CatwalkEdgeGirder',(xx,-11.2,CATWALK-.16),(xx,11.2,CATWALK-.16),.11,.22,'CatwalkStructure')
    for yy in FRAME_Y:
        ibeam('CatwalkColumnCantilever',(side*column_x,yy,CATWALK-.15),(side*walk_inner,yy,CATWALK-.15),.12,.19,'CatwalkStructure')
        anglebeam('CatwalkKneeBrace',(side*column_x,yy,CATWALK-1.15),(side*(walk_inner+.08),yy,CATWALK-.23),.095,'CatwalkStructure')
        for x,z in [(side*column_x,CATWALK-1.15),(side*(walk_inner+.09),CATWALK-.23)]:
            for dy in [-.085,.085]:
                plate_xz('CatwalkBracketGusset',[(x-.11,z-.11),(x+.11,z-.11),(x+.11,z+.11),(x-.11,z+.11)],yy+dy,.014,cat='CatwalkStructure')
                fastener('BracketBolt',(x,yy+dy+math.copysign(.009,dy),z),(0,math.copysign(1,dy),0),.018,'C1_Galvanized','CatwalkStructure')
    straight_guard((side*walk_inner,-11.2),(side*walk_inner,10.02),CATWALK)
    straight_guard((side*walk_outer,-11.2),(side*walk_outer,11.2),CATWALK)
    straight_guard((side*walk_inner,-11.2),(side*walk_outer,-11.2),CATWALK)
grating('RearCrosswalk',-6.12,6.12,10.02,11.2,CATWALK)
for yy in [10.09,11.13]:ibeam('RearCrosswalkGirder',(-7.26,yy,CATWALK-.16),(7.26,yy,CATWALK-.16),.16,.24,'CatwalkStructure')
for x in [-3.65,3.65]:
    ibeam('RearCrosswalkBearing',(x,11.13,FLOOR),(x,11.13,CATWALK-.20),.135,.16,'CatwalkStructure')
    anglebeam('RearWalkKnee',(x,11.13,CATWALK-1.1),(x,10.09,CATWALK-.18),.085,'CatwalkStructure')
    block('RearSupportFoot',(x,11.13,FLOOR+.028),(.32,.32,.05),'C1_Steel','CatwalkStructure',.006)
straight_guard((-7.26,11.2),(7.26,11.2),CATWALK)
for x0,x1 in [(-6.12,top_x),(top_x+1.2,6.12)]:straight_guard((x0,10.02),(x1,10.02),CATWALK)

# Stair channels support grating treads with formed anti-slip nosings. Individual
# brackets tie each tread to the two side stringers; landings have actual beams.
block('StairApproachPad',(start_x-.52,8.95,FLOOR/2),(1.18,1.36,FLOOR),'C1_Base','Stairs',.006)
for flight,x0,z0 in [(0,start_x,FLOOR),(1,second_x,FLOOR+14*R)]:
    for i in range(14):
        x=x0+i*GOING;z=z0+(i+1)*R
        # Across the stair width the tread bearing bars run between stringers.
        for j in range(7):block('StairTreadBearing',(x-.138+j*.046,8.95,z-.019),(.005,1.20,.038),'C1_Galvanized','Stairs',0)
        for yy in [8.35,8.95,9.55]:block('StairTreadCrossTie',(x,yy,z-.02),(.283,.009,.034),'C1_Galvanized','Stairs',0)
        block('FormedTreadNosing',(x-.134,8.95,z-.02),(.027,1.20,.045),'C1_Steel','Stairs',.005)
        for yy in [8.40,9.50]:
            block('TreadAngleBracket',(x,yy,z-.049),(.19,.055,.045),'C1_Steel','Stairs',.004)
    for yy in [8.385,9.515]:
        extrusion('RolledStairStringer',[(-.037,-.13),(.037,-.13),(.037,-.118),(-.025,-.118),(-.025,.118),(.037,.118),(.037,.13),(-.037,.13)],(x0-.14,yy,z0-.01),(x0+FLIGHT_RUN+.14,yy,z0+14*R-.115),cat='Stairs')
        rail_points=[(x0-GOING,yy,z0+1.10),(x0,yy,z0+R+1.10),(x0+FLIGHT_RUN,yy,z0+14*R+1.10)]
        tube('StairHandrail',fillet(rail_points,.06,8),.021,'C1_Steel','CirculationGuards',24)
        tube('StairMidrail',[(x,y,z-.55) for x,y,z in rail_points],.018,'C1_Steel','CirculationGuards',20)
        if flight==0:
            # Turn both leading rails back into the first post; no exposed ends.
            tube('StairHandrailReturn',fillet([(x0-GOING,yy,z0+1.10),(x0-GOING-.05,yy,z0+1.05),(x0-GOING-.05,yy,z0+.78),(x0,yy,z0+.78)],.055,10),.021,'C1_Steel','CirculationGuards',24)
            tube('StairMidrailReturn',fillet([(x0-GOING,yy,z0+.55),(x0-GOING-.035,yy,z0+.51),(x0-GOING-.035,yy,z0+.40),(x0,yy,z0+.40)],.035,8),.018,'C1_Steel','CirculationGuards',20)
        for i in [0,6,13]:
            x=x0+i*GOING;z=z0+(i+1)*R
            guard_post(x,yy,z)
            fastener('StringerRailBracket',(x,yy+.055,z-.045),(0,1,0),.015,'C1_Galvanized','Stairs')
        block('StringerBottomShoe',(x0-.08,yy,z0+.045),(.29,.15,.08),'C1_Steel','Stairs',.006)
for name,x0,x1,y0,y1,z in [('Intermediate',mid_x,second_x,8.35,9.55,FLOOR+14*R),('Top',top_x,top_x+1.20,8.35,10.02,CATWALK)]:
    grating(name+'Landing',x0,x1,y0,y1,z,'Stairs')
    for yy in [y0+.04,y1-.04]:ibeam('LandingEdgeChannel',(x0,yy,z-.12),(x1,yy,z-.12),.09,.18,'Stairs')
    for xx in [x0+.05,x1-.05]:ibeam('LandingCrossChannel',(xx,y0,z-.12),(xx,y1,z-.12),.09,.18,'Stairs')
    for yy in [8.4,9.5]:
        xx=x1-.15
        ibeam('LandingSupport',(xx,yy,FLOOR+.05),(xx,yy,z-.15),.105,.14,'Stairs')
        block('LandingColumnFoot',(xx,yy,FLOOR+.03),(.28,.28,.055),'C1_Steel','Stairs',.006)
    for yy in [8.385,9.515]:
        if name=='Intermediate' or yy<9:
            # Meeting handrail segments have shared end coordinates.
            end=x1-GOING if name=='Intermediate' else x1
            rod('LandingHandrail',(x0,yy,z+1.10),(end,yy,z+1.10),.021,'C1_Steel','CirculationGuards',24)
            rod('LandingMidrail',(x0,yy,z+.55),(end,yy,z+.55),.018,'C1_Steel','CirculationGuards',20)
    if name=='Top':
        straight_guard((x1,8.385),(x1,10.02),z)
        straight_guard((x0,9.515),(x0,10.02),z)

# Fabric-faced wall absorbers in framed bays, below the catwalks. No modern foam
# wedges or production dressing. They preserve access to doors and switchgear.
for side in [-1,1]:
    for yy in [-8.4,-2.8,2.8,8.4]:
        width=2.25;z=3.27;h=1.78
        block('AcousticPanelBacking',(side*7.51,yy,z),(.09,width,h),'C1_RoofTimber','AcousticTreatment',.008)
        for dy in [-width/4,width/4]:block('FabricAbsorberPanel',(side*7.445,yy+dy,z),(.065,width/2-.028,h-.055),'C1_AcousticFabric','AcousticTreatment',.01)
        for y0 in [yy-width/2,yy,yy+width/2]:block('AcousticPanelStile',(side*7.41,y0,z),(.045,.044,h),'C1_Steel','AcousticTreatment',.003)
        for z0 in [z-h/2,z+h/2]:block('AcousticPanelRail',(side*7.41,yy,z0),(.045,width,.044),'C1_Steel','AcousticTreatment',.003)
for xx in [-4.9,4.9]:
    block('RearAcousticPanel',(xx,11.49,3.1),(2.65,.13,2.05),'C1_AcousticFabric','AcousticTreatment',.015)
    for x0 in [xx-1.33,xx+1.33]:block('RearAcousticFrame',(x0,11.39,3.1),(.045,.04,2.11),'C1_Steel','AcousticTreatment',.003)
    for z0 in [2.045,4.155]:block('RearAcousticFrame',(xx,11.39,z0),(2.71,.04,.045),'C1_Steel','AcousticTreatment',.003)

# Permanent fixtures use a spun reflector with a white inner surface, rolled
# rim, socket, bulb, mounting pipe and a real connection to a service circuit.
def pendant(x,y,z,top,large=True):
    rad=.245 if large else .135
    profile=[(.032,0),(.054,-.014),(.068,-.06),(rad*.49,-.13),(rad*.82,-.22),(rad,-.254),(rad+.009,-.265),(rad+.009,-.278),(rad-.008,-.282),(rad-.018,-.265),(rad*.76,-.235),(rad*.44,-.145),(.046,-.061),(.026,-.026)]
    shade=lathe('SpunEnamelReflector',profile,(x,y,z),'C1_Steel','PermanentLighting',48)
    shade.data.materials.append(materials['C1_LampReflector'])
    for p in shade.data.polygons:
        if 8*48<=p.index<13*48:p.material_index=1
    rod('PendantStem',(x,y,z+.015),(x,y,top),.014,'C1_Steel','PermanentLighting',20)
    block('PendantClamp',(x,y,top),(.10,.11,.065),'C1_Steel','PermanentLighting',.007)
    lathe('PorcelainSocket',[(.036,-.085),(.039,-.065),(.039,-.02),(.035,0)],(x,y,z),'C1_EnamelWhite','PermanentLighting',32)
    lathe('WarmLampBulb',[(.018,-.078),(.034,-.10),(.045,-.14),(.042,-.18),(.025,-.205),(.005,-.213)],(x,y,z),'C1_LampGlass','PermanentLighting',32)
    PERMANENT_LIGHTS.append({'name':'InteriorPendant','position':[x,y,z-.30],'direction':[0,0,-1],'intensity':32.0,'range':12.0,'spot_angle':115,'color':[1,.78,.53],'type':'spot'})

for xx in [-4.35,4.35]:
    rod('WorklightCarrier',(xx,-11.2,TIE+.075),(xx,11.2,TIE+.075),.025,'C1_Steel','PermanentLighting',24)
    for yy in FRAME_Y:
        block('WorklightCarrierCleat',(xx,yy,TIE+.072),(.15,.14,.15),'C1_Steel','PermanentLighting',.007)
    for yy in [-10.5,-7.7,-4.9,-2.1,.7,3.5,6.3,9.1]:
        pendant(xx,yy,7.02,TIE+.075)

def wall_lamp(centre,out=(0,-1,0),height=3.8):
    ox,oy=out[:2];u=(-oy,ox,0);normal=(ox,oy,0)
    x,y=centre[:2];start=len(objects)
    # Local -Y is away from the wall.
    block('LampWallBackplate',(0,0,height),(.13,.06,.27),'C1_Steel','PermanentLighting',.035)
    points=[(0,-.025,height-.06),(0,-.075,height+.24),(0,-.18,height+.40),(0,-.34,height+.41),(0,-.43,height+.30),(0,-.43,height+.16)]
    tube('GooseneckLampArm',fillet(points,.10,12),.017,'C1_Steel','PermanentLighting',24)
    profile=[(.032,0),(.047,-.025),(.070,-.075),(.12,-.14),(.188,-.18),(.195,-.195),(.19,-.209),(.178,-.209),(.12,-.16),(.055,-.083),(.026,-.03)]
    shade=lathe('WallLampSpunShade',profile,(0,-.43,height+.16),'C1_Steel','PermanentLighting',48)
    shade.data.materials.append(materials['C1_LampReflector'])
    for p in shade.data.polygons:
        if 7*48<=p.index<10*48:p.material_index=1
    lathe('WallLampGlass',[(.026,0),(.043,-.033),(.037,-.070),(.01,-.09)],(0,-.43,height+.035),'C1_LampGlass','PermanentLighting',32)
    for zz in [height-.09,height+.09]:fastener('LampMountScrew',(0,-.039,zz),(0,-1,0),.01,'C1_Steel','PermanentLighting')
    for o in objects[start:]:
        o['wall_lamp']=len(PERMANENT_LIGHTS)
        transform_object(o,(x,y,0),u,(-ox,-oy,0),(0,0,1))
    PERMANENT_LIGHTS.append({'name':'WallWorkLamp','position':[x+ox*.43,y+oy*.43,height-.065],'direction':[ox*.15,oy*.15,-1],'intensity':7.0,'range':6.0,'spot_angle':110,'color':[1,.77,.49],'type':'spot'})

wall_lamp((-5.97,-12.025),height=3.78)
wall_lamp((5.70,-12.025),height=4.13)
wall_lamp((1.87,12.025),out=(0,1,0),height=3.43)
INTERIOR_LAMP_Y=[-4.30,1.35,6.70]
for side in [-1,1]:
    for yy in [-7.0,0,7.0]:wall_lamp((side*7.925,yy),out=(side,0,0),height=4.0)
    for yy in INTERIOR_LAMP_Y:wall_lamp((side*7.56,yy),out=(-side,0,0),height=2.80)

def junction(pos,size=(.19,.11,.24),cat='PermanentServices',normal=-1):
    block('CastJunctionBox',pos,size,'C1_Steel',cat,.018)
    block('JunctionCover',(pos[0],pos[1]+normal*(size[1]/2+.01),pos[2]),(size[0]*.91,.024,size[2]*.91),'C1_Steel',cat,.009)
    for dx in [-size[0]*.31,size[0]*.31]:
        for dz in [-size[2]*.33,size[2]*.33]:fastener('JunctionLidScrew',(pos[0]+dx,pos[1]+normal*(size[1]/2+.025),pos[2]+dz),(0,normal,0),.007,'C1_Galvanized',cat)

# External risers are rooted in service cabinets and end at eave junctions.
for side in [-1,1]:
    for yy in [-11.02,3.98,11.02]:
        for k in range(2):
            x=side*(7.965+.055*k)
            tube('ExternalServiceRiser',fillet([(x,yy,.37),(x,yy,6.55),(x,yy+.20,6.70),(x,yy+.40,6.70)],.13),.018,'C1_Steel','PermanentServices',20)
            block('RiserUpperJunction',(x,yy+.41,6.70),(.14,.19,.19),'C1_Steel','PermanentServices',.018)
            for zz in [.82,2.2,3.6,5.0,6.35]:
                block('ConduitSaddle',(side*7.943,yy,zz),(.12,.084,.025),'C1_Steel','PermanentServices',.004)
        block('RiserBaseBox',(side*7.98,yy,.37),(.19,.31,.40),'C1_Steel','PermanentServices',.018)
    # Interior service spines are beneath the catwalks; mounted branches feed
    # each wall lamp and the wall-mounted period distribution equipment.
    for z in [4.34,4.43]:rod('InteriorConduitSpine',(side*7.43,-11.35,z),(side*7.43,11.35,z),.013,'C1_Steel','PermanentServices',16)
    for yy in INTERIOR_LAMP_Y:
        tube('WorklightBranch',fillet([(side*7.43,yy,4.34),(side*7.43,yy,2.87),(side*7.535,yy,2.87)],.10),.012,'C1_Steel','PermanentServices',16)
        for zz in [3.1,3.85]:block('InteriorConduitClip',(side*7.485,yy,zz),(.13,.065,.025),'C1_Steel','PermanentServices',.003)
    for yy in [-11.35,11.35]:block('ServiceSpineEndBox',(side*7.44,yy,4.39),(.15,.22,.23),'C1_Steel','PermanentServices',.012)
    tube('CeilingLightingFeed',fillet([(side*7.43,-11.35,4.39),(side*7.43,-11.35,TIE+.075),(side*4.35,-11.35,TIE+.075),(side*4.35,-11.20,TIE+.075)],.10),.019,'C1_Steel','PermanentServices',20)
    for zz in [5.3,6.5]:block('LightingRiserClip',(side*7.51,-11.35,zz),(.16,.08,.028),'C1_Steel','PermanentServices',.004)
    for yy in [-8.3,7.0]:
        block('DistributionCabinet',(side*7.39,yy,1.13),(.28,.69,1.02),'C1_Steel','PermanentServices',.018)
        block('DistributionDoor',(side*7.225,yy,1.13),(.035,.63,.94),'C1_Steel','PermanentServices',.011)
        for z in [.85,1.23,1.5]:
            rod('PanelSwitchBoss',(side*7.21,yy,z),(side*7.15,yy,z),.036,'C1_DarkCeramic','PermanentServices',24)
            rod('PanelSwitchLever',(side*7.145,yy,z),(side*7.115,yy+.057,z+.036),.012,'C1_Brass','PermanentServices',16)
        for delta in [-.20,.20]:
            tube('DistributionFeed',fillet([(side*7.40,yy+delta,1.66),(side*7.40,yy+delta,4.17),(side*7.43,yy+delta+.16,4.34)],.1),.018,'C1_Steel','PermanentServices',20)
        block('PanelMasonryMount',(side*7.52,yy,1.13),(.13,.75,1.1),'C1_Base','PermanentServices',.007)

# Enclosed exterior meter/switch cabinets adjacent to service entrances.
for side,yy in [(1,-7.6),(-1,9.0)]:
    block('ExteriorServiceCabinet',(side*8.055,yy,1.19),(.31,.73,1.05),'C1_Steel','PermanentServices',.018)
    block('CabinetRaisedLid',(side*8.23,yy,1.19),(.05,.66,.97),'C1_Steel','PermanentServices',.014)
    for zz in [.82,1.54]:rod('CabinetHinge',(side*8.25,yy-.32,zz-.065),(side*8.25,yy-.32,zz+.065),.018,'C1_Steel','PermanentServices',20)
    for y0 in [yy-.265,yy+.265]:
        for zz in [.80,1.57]:fastener('CabinetLidScrew',(side*8.26,y0,zz),(side,0,0),.011,'C1_Galvanized','PermanentServices')
    rod('AmmeterHousing',(side*8.265,yy,1.43),(side*8.305,yy,1.43),.091,'C1_DarkCeramic','PermanentServices',40)
    rod('AmmeterDial',(side*8.306,yy,1.43),(side*8.313,yy,1.43),.077,'C1_EnamelWhite','PermanentServices',40)
    ring=[(side*8.316,yy+.080*math.cos(a*math.pi/20),1.43+.080*math.sin(a*math.pi/20)) for a in range(41)]
    tube('AmmeterBezel',ring,.007,'C1_Brass','PermanentServices',12)
    rod('AmmeterPointer',(side*8.318,yy,1.40),(side*8.318,yy-.035,1.47),.0025,'C1_Steel','PermanentServices',8)
    tube('CabinetLatchPull',fillet([(side*8.26,yy+.22,1.00),(side*8.33,yy+.22,1.00),(side*8.33,yy+.22,1.19),(side*8.26,yy+.22,1.19)],.038),.012,'C1_Brass','PermanentServices',20)
    tube('CabinetSupply',fillet([(side*8.05,yy,1.73),(side*8.05,yy,2.53),(side*7.98,yy-.35,2.68),(side*7.98,-11.02,2.68)],.12),.023,'C1_Steel','PermanentServices',24)
    block('ServiceFeedTee',(side*7.98,-11.02,2.68),(.15,.14,.14),'C1_Steel','PermanentServices',.016)

# Rear electrical intake and disconnect, grouped beside the service door as in
# the approved rear view. Conduit connects panels, penetration and lamp circuit.
block('RearIntakeMount',(-2.10,12.035,1.0),(1.15,.12,1.64),'C1_Base','PermanentServices',.012)
block('RearIntakeCabinet',(-2.10,12.23,1.0),(1.02,.30,1.52),'C1_Steel','PermanentServices',.022)
block('RearIntakeLid',(-2.10,12.405,1.0),(.94,.045,1.44),'C1_Steel','PermanentServices',.013)
for zz in [.43,1.57]:rod('RearCabinetHinge',(-2.55,12.44,zz-.07),(-2.55,12.44,zz+.07),.019,'C1_Steel','PermanentServices',24)
pull_handle(-1.74,12.447,1.0,.25,'PermanentServices',normal=1)
for xx in [-2.48,-1.72]:
    for zz in [.40,1.61]:fastener('RearLidFastener',(xx,12.436,zz),(0,1,0),.013,'C1_Galvanized','PermanentServices')
block('RearDisconnect',(-3.02,12.145,1.46),(.40,.23,.58),'C1_Steel','PermanentServices',.018)
block('RearDisconnectCover',(-3.02,12.28,1.46),(.35,.035,.52),'C1_Steel','PermanentServices',.009)
rod('DisconnectBoss',(-2.95,12.305,1.46),(-2.95,12.355,1.46),.034,'C1_DarkCeramic','PermanentServices',24)
rod('DisconnectLever',(-2.95,12.36,1.46),(-2.90,12.36,1.54),.012,'C1_Brass','PermanentServices',20)
tube('RearCabinetFeed',fillet([(-3.02,12.17,1.77),(-3.02,12.17,2.12),(-2.33,12.17,2.12),(-2.33,12.17,1.78)],.12),.023,'C1_Steel','PermanentServices',24)
for xx,h in [(-2.04,3.10),(-1.85,3.42)]:
    tube('RearSupplyRiser',fillet([(xx,12.23,1.78),(xx,12.18,h),(xx,11.87,h)],.09),.023,'C1_Steel','PermanentServices',24)
    for zz in [2.05,2.70]:block('RearRiserSaddle',(xx,12.105,zz),(.09,.25,.026),'C1_Steel','PermanentServices',.003)
    junction((xx,12.05,h),(.14,.13,.18),normal=1)
tube('RearLampCircuit',fillet([(-1.85,12.11,3.42),(1.45,12.11,3.42),(1.45,12.11,3.47),(1.87,12.11,3.47)],.06),.013,'C1_Steel','PermanentServices',20)

# Front/rear lamp circuits enter through the fixture backplates, with conduit
# returning along the facade to sealed wall penetrations beside the door heads.
for x,h in [(-5.97,3.78),(5.70,4.13)]:
    tube('FrontLampCircuit',fillet([(x,-12.09,h+.04),(x+.24,-12.09,h+.04),(x+.24,-12.09,2.63),(x+.39,-12.09,2.63)],.09),.013,'C1_Steel','PermanentServices',20)
    junction((x+.39,-12.066,2.63),(.16,.085,.20))

# Fixed protective posts are architectural site hardware, not loose set dressing.
for x in [-6.80,-5.15,4.87,6.56]:
    y=-12.68
    lathe('BollardTube',[(.075,.057),(.075,1.21),(.071,1.25),(.052,1.285),(.018,1.31)],(x,y,GROUND),'C1_SafetyOchre','FixedProtection',32)
    block('BollardFoot',(x,y,GROUND+.03),(.26,.26,.06),'C1_Steel','FixedProtection',.006)
    for dx in [-.085,.085]:
        for dy in [-.085,.085]:fastener('BollardAnchor',(x+dx,y+dy,GROUND+.06),(0,0,1),.013,'C1_Galvanized','FixedProtection')

exec(compile((HERE/'production_corrections.py').read_text(encoding='utf-8'),str(HERE/'production_corrections.py'),'exec'),globals())

# Remove exact shared joint duplicates from intersecting rail runs.
seen=set()
for o in list(objects):
    key=(o.parent.name,o.data.materials[0].name,tuple(sorted(tuple(round(c,5) for c in v.co) for v in o.data.vertices)))
    if key in seen:objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
    else:seen.add(key)

# Meter-scale UVs. Each mesh owns its UV coordinates; no implicit Unity primitive
# mapping. Timber boards carry vertical grain; roof boards carry longitudinal grain.
for o in objects:
    data=o.data;data.update()
    uv=data.uv_layers.new(name='UVMap') if not data.uv_layers else data.uv_layers.active
    mat=o.data.materials[0].name
    tile=TEXTURE_MATERIALS.get(mat,{}).get('metres_per_tile',1.0)
    seed=sum((i+1)*ord(c) for i,c in enumerate(o.name))%10007
    rng=random.Random(seed);du,dv=rng.random()*9,rng.random()*9
    for poly in data.polygons:
        n=poly.normal;dominant=max(range(3),key=lambda i:abs(n[i]))
        for li in poly.loop_indices:
            p=data.vertices[data.loops[li].vertex_index].co
            if mat=='C1_RoofTimber' and o.parent.name=='InteriorRoofLining':
                a,b=p.x,p.y
            elif dominant==0:a,b=p.y,p.z
            elif dominant==1:a,b=p.x,p.z
            else:a,b=p.x,p.y
            # A single stable texture frame across all wall cells avoids patchwork.
            offset=(0,0) if o.parent.name in ['ExteriorWalls','MasonryFraming','InteriorFloor'] else (du,dv)
            uv.data[li].uv=(a/tile+offset[0],b/tile+offset[1])
