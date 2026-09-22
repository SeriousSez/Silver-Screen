"""Revision 04 targeted construction corrections and geometric regression checks.

Runs before UV assignment/export. All placements use the source GROUND/FLOOR.
No Unity mesh offsets, replacement architecture, prop extraction or LOD changes.
"""

DEFECT_VALIDATION={'ground_datum':GROUND,'floor_level':FLOOR}

def bounds(o):
    return tuple(Vector(tuple(fn(v.co[i] for v in o.data.vertices) for i in range(3))) for fn in [min,max])

def delete_object(o):
    objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)

# An opening is a volume, not just a facade line. Apply the exclusion to every
# masonry/trim component, including narrow joints and elements offset in depth.
excluded=[]
for opening in PERSONNEL_OPENINGS:
    c=Vector(opening['centre']);u=Vector(opening['u']);d=Vector(opening['out'])
    limits=[(-opening['width']/2+.001,opening['width']/2-.001),(-.55,.70),(GROUND,FLOOR+opening['height']-.002)]
    for o in list(objects):
        if o.parent.name!='MasonryFraming' or 'Threshold' in o.name:continue
        local=[Vector(((v.co-c).dot(u),(v.co-c).dot(d),v.co.z)) for v in o.data.vertices]
        lo=[min(p[i] for p in local) for i in range(3)];hi=[max(p[i] for p in local) for i in range(3)]
        if not all(hi[i]>limits[i][0] and lo[i]<limits[i][1] for i in range(3)):continue
        excluded.append({'opening':opening['name'],'component':o.name})
        if all(lo[i]>=limits[i][0] and hi[i]<=limits[i][1] for i in range(3)):
            delete_object(o);continue
        centre=[(a+b)/2 for a,b in limits];size=[b-a for a,b in limits]
        cutter=box('OpeningExclusionVolume',*centre,*size,'C1_Base','MasonryFraming')
        transform_object(cutter,c,u,d,(0,0,1))
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Personnel opening volume','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
        bpy.ops.object.modifier_apply(modifier=mod.name)
        delete_object(cutter)
        if not o.data.vertices:delete_object(o)
DEFECT_VALIDATION['masonry_opening_exclusions']=excluded

# Panel controls: bolted escutcheon, stepped bronze bearing, transverse pivot,
# forged lever and shaped insulating grip. Three independent positive stops.
remove_named(['PanelSwitchBoss','PanelSwitchLever'])
def disconnect_control(centre,normal,u,angle):
    start=len(objects);cat='PermanentServices'
    block('SwitchEscutcheon',(0,0,0),(.128,.026,.176),'C1_Steel',cat,.018)
    for xx in [-.044,.044]:
        for zz in [-.065,.065]:fastener('SwitchMountScrew',(xx,-.016,zz),(0,-1,0),.0065,'C1_Galvanized',cat)
    boss=lathe('SwitchBearingBoss',[(.040,0),(.047,.008),(.047,.016),(.034,.024),(.031,.055)],mat='C1_Brass',cat=cat,sides=32)
    transform_object(boss,(0,-.013,0),(1,0,0),(0,0,1),(0,-1,0))
    rod('SwitchPivotPin',(-.048,-.071,0),(.048,-.071,0),.016,'C1_Galvanized',cat,24)
    for xx in [-.044,.044]:fastener('SwitchPivotRetainer',(xx,-.071,0),(math.copysign(1,xx),0,0),.02,'C1_Brass',cat)
    t=math.radians(angle);a=Vector((0,-.075,0));b=a+Vector((0,-math.sin(t)*.105,math.cos(t)*.105))
    rod('ForgedSwitchLever',a,b,.015,'C1_Brass',cat,24)
    grip=lathe('ShapedSwitchGrip',[(.017,0),(.026,.014),(.029,.045),(.025,.058),(.014,.069)],mat='C1_DarkCeramic',cat=cat,sides=32)
    axis=(b-a).normalized();q=axis.to_track_quat('Z','Y')
    for v in grip.data.vertices:v.co=q@v.co+b
    for z in [-.064,.064]:block('SwitchTravelStop',(0,-.038,z),(.051,.027,.020),'C1_Brass',cat,.005)
    for o in objects[start:]:transform_object(o,centre,u,tuple(-v for v in normal),(0,0,1))

for side in [-1,1]:
    for yy in [-8.3,7.0]:
        for z,a in zip([.85,1.17,1.46],[35,135,40]):
            disconnect_control((side*7.204,yy,z),(-side,0,0),(0,-side,0),a)

# Keep the services, correct paths which intersected fabric/structure and add
# missing functional connections/supports. Penetrations lead into wall chases.
remove_named(['DistributionFeed','FrontLampCircuit','RearLampCircuit'])
for side in [-1,1]:
    for yy in [-8.3,7.0]:
        for delta in [-.20,.20]:
            points=[(side*7.36,yy+delta,1.64),(side*7.36,yy+delta,4.17),(side*7.43,yy+delta+.16,4.34)]
            tube('DistributionFeedClearOfAbsorbers',fillet(points,.10),.018,'C1_Steel','PermanentServices',20)
            for z in [1.90,3.20]:
                block('DistributionConduitSaddle',(side*7.39,yy+delta,z),(.075,.075,.025),'C1_Steel','PermanentServices',.004)
    for yy in [-11.02,3.98,11.02]:
        # Base pullbox has a real rear-facing wall penetration and paired glands.
        rod('RiserWallPenetration',(side*7.77,yy,.37),(side*7.97,yy,.37),.038,'C1_Steel','PermanentServices',24)
        for k in range(2):
            x=side*(7.965+.055*k)
            rod('RiserCompressionGland',(x,yy,.54),(x,yy,.59),.027,'C1_Galvanized','PermanentServices',20)
            rod('UpperJunctionWallSleeve',(side*7.77,yy+.41,6.70),(x,yy+.41,6.70),.028,'C1_Steel','PermanentServices',20)
    for yy in [-7.,0.,7.]:
        # Each exterior lamp has a short drop to an accessible, sealed wall box.
        tube('ExteriorLampFeed',fillet([(side*7.94,yy,4.07),(side*8.01,yy,4.07),(side*8.01,yy,4.52),(side*7.94,yy+.13,4.62)],.08),.013,'C1_Steel','PermanentServices',20)
        block('ExteriorLampJunction',(side*7.945,yy+.13,4.62),(.12,.16,.20),'C1_Steel','PermanentServices',.012)
        rod('ExteriorLampWallSleeve',(side*7.77,yy+.13,4.62),(side*7.98,yy+.13,4.62),.027,'C1_Steel','PermanentServices',20)
        block('LampFeedSaddle',(side*7.96,yy,4.30),(.11,.06,.025),'C1_Steel','PermanentServices',.003)
    for yy in [-10.,-7.2,-4.4,-1.6,1.2,4.,6.8,9.6]:
        block('InteriorSpineSaddle',(side*7.50,yy,4.385),(.16,.05,.15),'C1_Steel','PermanentServices',.004)
    # Rear service-spine box connects to a wall sleeve, fed by concealed chase.
    rod('InteriorSpineWallSleeve',(side*7.44,11.35,4.39),(side*7.73,11.35,4.39),.034,'C1_Steel','PermanentServices',20)
    for yy in [-11.35,11.35]:
        for z in [4.34,4.43]:rod('SpineEndGland',(side*7.43,yy-.02,z),(side*7.43,yy+.02,z),.022,'C1_Galvanized','PermanentServices',20)

for x,h,jx in [(-5.97,3.78,-5.58),(5.70,4.13,6.95)]:
    route_x=jx-.15
    tube('FrontLampCircuit',fillet([(x,-12.04,h+.04),(x,-12.07,h+.04),(route_x,-12.07,h+.04),(route_x,-12.07,2.63),(jx,-12.07,2.63)],.09),.013,'C1_Steel','PermanentServices',20)
    for z in [3.0,3.6]:block('FrontLampFeedSaddle',(route_x,-12.035,z),(.065,.09,.025),'C1_Steel','PermanentServices',.003)
    # Re-use left junction, replace the right junction formerly under the cloth.
    if x>0:junction((jx,-12.066,2.63),(.16,.085,.20))
    rod('FrontLampWallSleeve',(jx,-11.87,2.63),(jx,-12.075,2.63),.026,'C1_Steel','PermanentServices',20)
for o in list(objects):
    if o.name.startswith(('CastJunctionBox','JunctionCover','JunctionLidScrew')):
        lo,hi=bounds(o)
        if 5.98<lo.x and hi.x<6.18 and lo.y< -12:delete_object(o)
tube('RearLampCircuit',fillet([(-1.85,12.11,3.42),(1.45,12.11,3.42),(1.87,12.11,3.47),(1.87,12.055,3.47)],.06),.013,'C1_Steel','PermanentServices',20)
for xx in [-1.1,0,.9]:block('RearLampCircuitSaddle',(xx,12.06,3.42),(.065,.13,.025),'C1_Steel','PermanentServices',.003)

# Long cabinet supply runs need intermediate wall saddles; remain above doors.
for side,yy in [(1,-7.6),(-1,9.0)]:
    rod('ExteriorCabinetEntryGland',(side*8.05,yy,1.705),(side*8.05,yy,1.76),.038,'C1_Galvanized','PermanentServices',24)
    n=math.ceil((yy-.35+11.02)/1.5)
    for i in range(1,n):
        y=-11.02+(yy-.35+11.02)*i/n
        block('CabinetSupplySaddle',(side*7.945,y,2.68),(.13,.065,.060),'C1_Steel','PermanentServices',.005)

rod('RearDisconnectEntryGland',(-3.02,12.17,1.74),(-3.02,12.17,1.81),.034,'C1_Galvanized','PermanentServices',24)
for xx,yy in [(-2.33,12.17),(-2.04,12.23),(-1.85,12.23)]:
    rod('RearIntakeEntryGland',(xx,yy,1.75),(xx,yy,1.82),.036,'C1_Galvanized','PermanentServices',24)

# Pipe shoes discharge onto small fixed splash shoes at grade, clear of doors.
for side in [-1,1]:
    for yy in [-11.4,11.4]:
        block('RainwaterSplashShoe',(side*8.22,yy,.035),(.44,.25,.07),'C1_Base','Rainwater',.014)

# Connected gutter/apron/outlet construction replaces the intersecting collection.
exec(compile((HERE/'production_rainwater.py').read_text(encoding='utf-8'),str(HERE/'production_rainwater.py'),'exec'),globals())

# Minor foot gaps on the existing circulation steel are corrected at source.
for o in objects:
    if o.name.startswith(('LandingColumnFoot','StringerBottomShoe','RearSupportFoot')):
        lo,hi=bounds(o)
        if FLOOR<lo.z<FLOOR+.01:
            for v in o.data.vertices:v.co.z-=lo.z-FLOOR

# Roof daylight control occupies the cavity ABOVE primary structure. The old
# continuous backing and timber ceiling were not operable and intersected the
# purlins. Opaque cloth rolls across the curved glazing into small cassettes;
# the surrounding timber ceiling, glazing frames and primary steel stay intact.
remove_named(['OpaqueRoofBlackout','BlackoutRunner'])
for o in list(objects):
    if o.name.startswith('TongueAndGrooveRoofLining'):
        lo,hi=bounds(o);mid=(lo+hi)*.5;ang=math.atan2(mid.x,mid.z-CIRCLE_Z)
        if any(a<ang<b for a,b in bands) and lo.y<10.2 and hi.y> -10.2:delete_object(o)
roof_panel_schedules=[]
for a,b in bands:
    side=-1 if b<0 else 1
    inner=b if side<0 else a
    outer=a if side<0 else b
    roller_angle=inner+side*.016
    for j in range(14):
        y0=-10.22+j*20.44/14;y1=-10.22+(j+1)*20.44/14
        shell_segment('ClosedRoofBlackoutFabric',a-.005,b+.005,y0-.012,y1+.012,offset=-.211,thick=.0012,mat='C1_Blackout',category='BlackoutShutters')
        r=RADIUS-.171;x=r*math.sin(roller_angle);z=CIRCLE_Z+r*math.cos(roller_angle)
        rod('RoofBlackoutRoller',(x,y0,z),(x,y1,z),.040,'C1_Blackout','BlackoutShutters',32)
        for yy in [y0,y1]:
            shell_segment('RoofBlackoutCurvedGuide',a-.007,b+.007,yy-.014,yy+.014,offset=-.15,thick=.068,mat='C1_Steel',category='BlackoutShutters')
            rod('RoofRollerAxle',(x,yy-.025,z),(x,yy+.025,z),.015,'C1_Galvanized','BlackoutShutters',20)
        shell_segment('RoofBlackoutLeadingBar',outer-.003,outer+.003,y0,y1,offset=-.19,thick=.024,mat='C1_Steel',category='BlackoutShutters')
        # Compact geared drive on the roller end, operated via a pole socket.
        gy=(y0+y1)/2
        rod('RoofBlackoutGearHousing',(x,gy-.04,z),(x,gy+.04,z),.042,'C1_Steel','BlackoutShutters',32)
        direction=Vector((-math.sin(roller_angle),0,-math.cos(roller_angle)))
        socket=Vector((x,gy,z))+direction*.14
        rod('RoofBlackoutDriveStem',(x,gy,z),socket,.012,'C1_Steel','BlackoutShutters',20)
        ring=[socket+Vector((.022*math.cos(t*math.pi/12),.022*math.sin(t*math.pi/12),0)) for t in range(25)]
        tube('RoofBlackoutPoleEye',ring,.005,'C1_Steel','BlackoutShutters',10)
        roof_panel_schedules.append({'angle_limits':[a,b],'y_limits':[y0,y1],'fabric_outer_radius_offset':-.211,'minimum_primary_chord_radial_clearance':.0128,'mechanism':'Curved guides and spring roller; gearbox accepts removable operating pole from service access'})
    for angle in [a,b]:
        shell_segment('RoofBlackoutPerimeterSeal',angle-.006,angle+.006,-10.25,10.25,offset=-.115,thick=.103,mat='C1_Blackout',category='BlackoutShutters')

# Guides meet the fabric tangent; a stowed hem and operating chain make the
# roller's coverage and practical operation legible from the catwalk.
remove_named(['BlackoutGuide'])
for side in [-1,1]:
    for y0,y1,z0,z1 in window_schedules[side]:
        x=side*7.535
        for yy in [y0-.025,y1+.025]:
            block('BlackoutFabricGuide',(x,yy,(z0+z1)/2),(.040,.035,z1-z0+.08),'C1_Steel','BlackoutShutters',.004)
            for z in [z0+.15,z1-.15]:block('ShutterGuideWallSaddle',(side*7.565,yy,z),(.075,.055,.033),'C1_Steel','BlackoutShutters',.004)
        block('StowedBlackoutLeadingFabric',(x,(y0+y1)/2,z1+.075),(.002,y1-y0+.08,.25),'C1_Blackout','BlackoutShutters',0)
        rod('BlackoutBottomBar',(x,y0-.025,z1-.05),(x,y1+.025,z1-.05),.016,'C1_Steel','BlackoutShutters',20)
        rod('BlackoutRollerGearcase',(side*7.46,y1+.02,z1+.20),(side*7.46,y1+.085,z1+.20),.080,'C1_Steel','BlackoutShutters',32)
        chain=[(side*7.43,y1+.075,z1+.20),(side*7.43,y1+.075,CATWALK+.65),(side*7.49,y1+.075,CATWALK+.65),(side*7.49,y1+.075,z1+.20)]
        tube('BlackoutOperatingChain',fillet(chain,.028),.0035,'C1_Galvanized','BlackoutShutters',10)
ROOF_BLACKOUT_DESCRIPTION='Closed opaque roof blackout fabric on curved guides above primary steel; fourteen independent roller sections per glazed ribbon with sealed overlaps, end gears and pole-operated drives. Clerestory rollers stowed with aligned guides, hem and catwalk-accessible chains. Roof lining stops outside glazing. Motion is structurally planned, not gameplay animated.'
DEFECT_VALIDATION['roof_blackout_panels']=roof_panel_schedules

# Terminate the first-flight stringers against the floor rather than allowing
# their full inclined profile to project below the asset placement datum.
for o in objects:
    if o.name.startswith('RolledStairStringer') and bounds(o)[0].z<FLOOR:
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=(0,0,FLOOR),plane_no=(0,0,1),clear_inner=True)
        boundary=[e for e in bm.edges if e.is_boundary]
        bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()

# Geometric checks: ground contacts, complete door volumes and actual leaf
# sweeps. Report the geometry measured, not a claim of game collision behavior.
bpy.context.view_layer.update()
DEFECT_VALIDATION['ground_contacts']={}
for prefix,level in [('FilmingFloor',GROUND),('EndPierBase',GROUND),('BollardFoot',GROUND),('StanchionBaseplate',FLOOR)]:
    zs=[round(bounds(o)[0].z,6) for o in objects if o.name.startswith(prefix)]
    assert zs and all(abs(z-level)<1e-5 for z in zs),(prefix,zs,level)
    DEFECT_VALIDATION['ground_contacts'][prefix]=zs
below_ground=[(o.name,tuple(bounds(o)[0])) for o in objects if bounds(o)[0].z<GROUND-.00001]
assert not below_ground,'Subgrade geometry breaks surface placement datum: '+str(below_ground)

def object_bvh(items):
    vs=[];fs=[];owners=[]
    for o in items:
        base=len(vs);vs.extend(v.co.copy() for v in o.data.vertices)
        fs.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons)
        owners.extend([o.name]*len(o.data.polygons))
    return (BVHTree.FromPolygons(vs,fs),owners) if vs else (None,[])

# Check the actual four ornaments, including their widest plinths, against the
# full roof/end-cap assembly. A separating plane proves positive clearance even
# where surface-only BVH overlap could miss a fully embedded component.
finial_roof=[o for o in objects if o.name.startswith(('ArchFascia','ArchOuterCoping','CircularSegmentMasonry')) or o.parent.name in ['Roof','RoofSeams','RoofGlazing','RoofVents','Rainwater']]
finial_roof_bvh,finial_roof_names=object_bvh(finial_roof)
finial_checks=[]
for placement in FINIAL_PLACEMENTS:
    face=placement['face'];parts=placement['parts']
    ornament_bvh,ornament_names=object_bvh(parts)
    hits=ornament_bvh.overlap(finial_roof_bvh)
    gap=min(face*v.co.y for o in parts for v in o.data.vertices)-max(face*v.co.y for o in finial_roof for v in o.data.vertices)
    p0,p1=bounds(placement['plinth']);c0,c1=bounds(placement['lip'])
    bearing_margin=min(p0.x-c0.x,c1.x-p1.x,p0.y-c0.y,c1.y-p1.y)-.012  # lip bevel
    bedding=p0.z-c1.z
    row={'corner':placement['corner'],'roof_intersections':sorted({(ornament_names[a],finial_roof_names[b]) for a,b in hits}),'minimum_separating_gap_m':round(gap,6),'plinth_bearing_margin_m':round(bearing_margin,6),'plinth_bedding_m':round(bedding,6),'outward_shift_m':round(placement['outward_shift'],6),'cap_extension_m':round(placement['cap_extension'],6)}
    finial_checks.append(row)
    assert not hits and gap>=FINIAL_ROOF_GAP-.00001,'Finial/roof clearance: '+str(row)
    assert bearing_margin>=.025 and -.003<=bedding<=.00001,'Finial lacks seated plinth bearing: '+str(row)
DEFECT_VALIDATION['four_finial_clearances']=finial_checks

door_checks=[]
for opening in PERSONNEL_OPENINGS:
    c=Vector(opening['centre']);u=Vector(opening['u']);d=Vector(opening['out']);width=opening['width'];height=opening['height']
    near=[]
    for o in objects:
        if o.parent.name=='PersonnelDoors' and o.get('personnel_opening')==opening['name']:
            # Include the actual fixed steel jamb and stop; exclude only the
            # moving leaf and deliberate latch/hinge/compressible-seal contacts.
            if o in opening['moving'] or o.name.startswith(('PersonnelDoorSeal','HingeJambAttachment','LatchStrike')):continue
        lo,hi=bounds(o)
        if hi.z<FLOOR+.0005 or lo.z>height+.10:continue
        mid=(lo+hi)*.5
        if (mid-c).length<width+max((hi-lo).length/2,1.5)+1.0:near.append(o)
    static,owners=object_bvh(near)
    moving=[o for o in opening['moving'] if not o.name.startswith(('PersonnelHinge','CrankedHinge','LatchTongue','MortiseLockEdgePlate'))]
    hinge=c+u*(-width/2-.012)+d*opening['hinge_out']
    collisions=[];collision_objects=set()
    for degrees in range(0,96,5):
        t=math.radians(degrees);vs=[];fs=[]
        for o in moving:
            base=len(vs)
            for v in o.data.vertices:
                p=v.co-hinge;a=p.dot(u);b=p.dot(d)
                vs.append(hinge+u*(a*math.cos(t)-b*math.sin(t))+d*(a*math.sin(t)+b*math.cos(t))+Vector((0,0,p.z)))
            fs.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons)
        moving_bvh=BVHTree.FromPolygons(vs,fs)
        hits=moving_bvh.overlap(static) if static else []
        if hits:
            collisions.append(degrees);collision_objects.update(owners[b] for a,b in hits)
    door_checks.append({'door':opening['name'],'tested_outward_degrees':[0,95],'increment_degrees':5,'collisions':collisions,'collision_objects':sorted(collision_objects)})
DEFECT_VALIDATION['door_sweeps']=door_checks
# Assertions are enabled after reporting first-pass intersections for diagnosis.
(HERE/'defect_checks.json').write_text(json.dumps(DEFECT_VALIDATION,indent=2))
assert not any(r['collisions'] for r in door_checks),'Personnel sweep collision: '+str(door_checks)

structure,structure_names=object_bvh([o for o in objects if o.parent.name in ['PrimaryStructure','CatwalkStructure','RiggingPrimary','EntranceCanopies','AcousticTreatment']])
lamp_checks=[]
for lamp_id in sorted({o['wall_lamp'] for o in objects if 'wall_lamp' in o}):
    lamp,_=object_bvh([o for o in objects if o.get('wall_lamp',-1)==lamp_id])
    hits=lamp.overlap(structure)
    lamp_checks.append({'lamp':lamp_id,'structural_or_acoustic_intersections':sorted({structure_names[b] for a,b in hits})})
DEFECT_VALIDATION['all_wall_lamp_clearances']=lamp_checks
(HERE/'defect_checks.json').write_text(json.dumps(DEFECT_VALIDATION,indent=2))
assert not any(row['structural_or_acoustic_intersections'] for row in lamp_checks),'Wall lamp obstruction: '+str(lamp_checks)

roof_blackouts=[o for o in objects if o.name.startswith(('ClosedRoofBlackoutFabric','RoofBlackoutRoller','RoofBlackoutCurvedGuide','RoofBlackoutLeadingBar','RoofBlackoutGearHousing','RoofBlackoutDriveStem','RoofBlackoutPoleEye'))]
blind,_=object_bvh(roof_blackouts)
hits=blind.overlap(structure)
DEFECT_VALIDATION['roof_blackout_structure_intersections']=sorted({structure_names[b] for a,b in hits})
coverage=[]
for a,b in bands:
    for j in range(29):
        yy=-10.20+j*20.4/28
        for t in [.02,.25,.5,.75,.98]:
            angle=a+(b-a)*t;r=RADIUS-.126
            start=Vector((r*math.sin(angle),yy,CIRCLE_Z+r*math.cos(angle)))
            direction=Vector((-math.sin(angle),0,-math.cos(angle)))
            hit,normal,index,distance=blind.ray_cast(start,direction,.16)
            coverage.append(hit is not None)
DEFECT_VALIDATION['roof_blackout_coverage']={'rays':len(coverage),'blocked':sum(coverage)}
(HERE/'defect_checks.json').write_text(json.dumps(DEFECT_VALIDATION,indent=2))
assert not hits,'Blackout intersects roof structure: '+str(DEFECT_VALIDATION['roof_blackout_structure_intersections'])
assert all(coverage),'Uncovered roof glazing sample'

clerestory,_=object_bvh([o for o in objects if o.name.startswith(('BlackoutRoller','BlackoutFabricGuide','BlackoutBottomBar','BlackoutOperatingChain','StowedBlackoutLeadingFabric'))])
hits=clerestory.overlap(structure)
DEFECT_VALIDATION['clerestory_blackout_structure_intersections']=sorted({structure_names[b] for a,b in hits})
DEFECT_VALIDATION['clerestory_coverage']=[{'side':side,'opening':[y0,y1,z0,z1],'fabric_width':y1-y0+.08,'required_drop':z1-z0+.25,'fabric_plane_x':side*7.535,'wall_inner_x':side*7.58} for side in [-1,1] for y0,y1,z0,z1 in window_schedules[side]]
(HERE/'defect_checks.json').write_text(json.dumps(DEFECT_VALIDATION,indent=2))
assert not hits,'Clerestory blind intersects structure'
