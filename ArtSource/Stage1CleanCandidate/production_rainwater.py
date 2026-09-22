"""Connected eave runoff, supported open gutters and hollow downpipes.

Executed by production_corrections.py in the authoritative generator scene.
The curved roof ends above the cornice; only the weather lap touches roofing.
"""
remove_named(['EaveCollectionPipe','FoldedEaveApron','RainwaterDownpipe','EavePipeSaddle','DownpipeWallClip','PipeClipAnchor'])
GUTTER_X=8.20
GUTTER_RADIUS=.100
GUTTER_WALL=.003
GUTTER_END=11.64
GUTTER_OUTLET=11.40
GUTTER_FALL=.0025
def gutter_rim(y):return 6.985+min(abs(y-GUTTER_OUTLET),abs(y+GUTTER_OUTLET))*GUTTER_FALL

def rainwater_tree(items):
    vs=[];fs=[];owners=[]
    for o in items:
        base=len(vs);vs.extend(v.co.copy() for v in o.data.vertices)
        fs.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons)
        owners.extend([o.name]*len(o.data.polygons))
    return BVHTree.FromPolygons(vs,fs),owners

def hollow_pipe(name,points,outer=.045,inner=.041):
    # Reuse the established sweep frame, then join outer/inner walls with
    # annular ends. The material volume is closed but the water bore is open.
    a=tube(name,points,outer,'C1_Roof','Rainwater',32)
    b=tube('TemporaryPipeBore',points,inner,'C1_Roof','Rainwater',32)
    n=len(a.data.vertices);k=32
    vs=[v.co.copy() for v in a.data.vertices]+[v.co.copy() for v in b.data.vertices]
    fs=[tuple(p.vertices) for p in a.data.polygons[:-2]]
    fs += [tuple(n+i for i in reversed(p.vertices)) for p in b.data.polygons[:-2]]
    for base in [0,n-k]:
        fs += [(base+j,base+(j+1)%k,n+base+(j+1)%k,n+base+j) for j in range(k)]
    name=a.name;delete_object(a);delete_object(b)
    o=mesh(name,vs,fs,'C1_Roof','Rainwater')
    for p in o.data.polygons[:-2*k]:p.use_smooth=True
    return o

gutter_parts=[];aprons=[];outlet_checks=[];rainwater_paths=[];bracket_records=[]
angles=[math.pi+i*math.pi/32 for i in range(33)]
outer=[(GUTTER_RADIUS*math.cos(a),GUTTER_RADIUS*math.sin(a)) for a in angles]
inner=[((GUTTER_RADIUS-GUTTER_WALL)*math.cos(a),(GUTTER_RADIUS-GUTTER_WALL)*math.sin(a)) for a in reversed(angles)]
cross=outer+inner;n=len(cross)
# The lap follows the roof; the exposed apron then falls over the cornice.
lap=[(x,arch_z(x,-.08)+.003) for x in [7.58,7.61,7.64,ROOF_RUNOFF_X]]
apron_top=lap+[(8.09,7.198),(8.19,7.095),(8.19,7.040)]
apron_profile=apron_top+[(x-.0015,z-.003) for x,z in reversed(apron_top)]
support_ys=[-11.55+i*23.10/18 for i in range(19)]

for side in [-1,1]:
    stations=[-GUTTER_END,-GUTTER_OUTLET,0,GUTTER_OUTLET,GUTTER_END]
    vs=[(side*GUTTER_X+x,y,gutter_rim(y)+z) for y in stations for x,z in cross]
    fs=[tuple(reversed(range(n))),tuple((len(stations)-1)*n+i for i in range(n))]
    fs += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(stations)-1) for i in range(n)]
    gutter=mesh('GutterRun',vs,fs,'C1_Roof','Rainwater')
    for p in gutter.data.polygons[2:]:
        section=(p.index-2)%n
        p.use_smooth=section<32 or 33<=section<65  # curved skins, sharp rim edges
    # Open bottom outlets; no capped solid pipe touching a sealed trough.
    for yy in [-GUTTER_OUTLET,GUTTER_OUTLET]:
        cutter=rod('TemporaryOutletCut',(side*GUTTER_X,yy,6.76),(side*GUTTER_X,yy,7.10),.042,'C1_Roof','Rainwater',32)
        bpy.context.view_layer.objects.active=gutter
        mod=gutter.modifiers.new('Open rainwater outlet','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
        bpy.ops.object.modifier_apply(modifier=mod.name);delete_object(cutter)
    gutter_parts.append(gutter)
    for yy in [-GUTTER_END,GUTTER_END]:
        cap=extrusion('GutterStopEnd',outer,(side*GUTTER_X,yy-.002,gutter_rim(yy)),(side*GUTTER_X,yy+.002,gutter_rim(yy)),'C1_Roof','Rainwater')
        gutter_parts.append(cap)
    apron=extrusion('EaveRunoffApron',[(side*x,z) for x,z in apron_profile],(0,-11.568,0),(0,11.568,0),'C1_Roof','RoofSeams')
    aprons.append(apron)
    # A small standing return at each abutment keeps water on the apron;
    # it meets the masonry end face through a thin compressible weather joint.
    return_profile=apron_top+[(x,z+.018) for x,z in reversed(apron_top)]
    for end in [-1,1]:
        yy=end*11.566
        aprons.append(extrusion('EaveApronEndReturn',[(side*x,z) for x,z in return_profile],(0,yy-.0015,0),(0,yy+.0015,0),'C1_Roof','RoofSeams'))
        extrusion('EaveAbutmentWeatherJoint',[(side*x,z) for x,z in return_profile],(0,end*11.5675,0),(0,end*11.570,0),'C1_Blackout','RoofSeams')
    for yy in support_ys:
        rim=gutter_rim(yy)
        strap_outer=[(.106*math.cos(a),.106*math.sin(a)) for a in angles]
        strap_inner=[(.100*math.cos(a),.100*math.sin(a)) for a in reversed(angles)]
        extrusion('GutterCradleStrap',strap_outer+strap_inner,(side*GUTTER_X,yy-.018,rim),(side*GUTTER_X,yy+.018,rim),'C1_Steel','Rainwater')
        block('GutterBracketWallPlate',(side*8.019,yy,rim-.04),(.018,.070,.10),'C1_Steel','Rainwater',.003)
        beam('GutterCradleArm',(side*8.028,yy,rim-.025),(side*8.096,yy,rim),.025,.018,'C1_Steel','Rainwater')
        for z in [rim-.016,rim-.068]:fastener('GutterBracketAnchor',(side*8.029,yy,z),(side,0,0),.009,'C1_Steel','Rainwater')
        # Tapered cleat bears on the top of the existing cornice and supports
        # the apron underside; the cornice profile itself is unchanged.
        def apron_under(x):return lap[-1][1]+(7.198-lap[-1][1])*(x-ROOF_RUNOFF_X)/(8.09-ROOF_RUNOFF_X)-.003
        wedge=[(7.71,7.18),(8.04,7.18),(8.04,apron_under(8.04)),(7.71,apron_under(7.71))]
        extrusion('EaveApronBearingCleat',[(side*x,z) for x,z in wedge],(0,yy-.014,0),(0,yy+.014,0),'C1_Steel','Rainwater')
        bracket_records.append({'side':side,'y':round(yy,4),'rim_z':round(rim,5)})
    for yy in [-GUTTER_OUTLET,GUTTER_OUTLET]:
        # The outlet lip follows the trough floor, avoiding a raised inlet dam.
        rings=[]
        for rad,top in [(.049,True),(.049,False),(.042,False),(.042,True)]:
            for i in range(32):
                a=i*2*math.pi/32;dx=rad*math.cos(a);dy=rad*math.sin(a)
                z=gutter_rim(yy+dy)-math.sqrt((GUTTER_RADIUS-GUTTER_WALL)**2-dx*dx)+.0015 if top else 6.80
                rings.append((side*GUTTER_X+dx,yy+dy,z))
        fs=[(k*32+i,k*32+(i+1)%32,((k+1)%4)*32+(i+1)%32,((k+1)%4)*32+i) for k in range(4) for i in range(32)]
        socket=mesh('GutterOpenOutletSocket',rings,fs,'C1_Roof','Rainwater')
        for p in socket.data.polygons:p.use_smooth=True
        path=fillet([(side*GUTTER_X,yy,6.84),(side*GUTTER_X,yy,6.76),(side*8.035,yy,6.58),(side*8.035,yy,.34),(side*8.20,yy,.21)],.10)
        pipe=hollow_pipe('RainwaterHollowDownpipe',path)
        gutter_parts.extend([socket,pipe]);rainwater_paths.append((side,yy,path))
        for zz in [.75,2.55,4.5,6.5]:
            wrap=hollow_pipe('DownpipeWrapClip',[(side*8.035,yy,zz-.0225),(side*8.035,yy,zz+.0225)],.052,.045)
            wrap.data.materials.clear();wrap.data.materials.append(materials['C1_Steel'])
            for direction in [-1,1]:
                block('DownpipeClipWallLug',(side*7.976,yy+direction*.057,zz),(.152,.028,.045),'C1_Steel','Rainwater',.004)
                fastener('DownpipeClipWallAnchor',(side*8.053,yy+direction*.057,zz),(side,0,0),.009,'C1_Steel','Rainwater')

# Durable clearance and flow-path checks on the generated geometry.
obstacles=[o for o in objects if o.parent.name in ['Roof','MasonryFraming','ExteriorWalls'] or o.name.startswith(('RoofStandingSeam','EndAbutmentFlashing'))]
ob,owners=rainwater_tree(obstacles)
intersections=[]
for o in gutter_parts:
    gb,_=rainwater_tree([o]);hits=gb.overlap(ob)
    intersections.extend((o.name,owners[b]) for a,b in hits)
assert not intersections,'Rainwater intersects envelope: '+str(sorted(set(intersections)))
masonry=[o for o in objects if o.parent.name in ['MasonryFraming','ExteriorWalls']]
mb,mn=rainwater_tree(masonry)
for apron in aprons:
    ab,_=rainwater_tree([apron]);hits=ab.overlap(mb)
    assert not hits,'Apron intersects cornice/end cap: '+str(sorted({mn[b] for a,b in hits}))
seams,sn=rainwater_tree([o for o in objects if o.name.startswith('RoofStandingSeam')])
for apron in aprons:
    ab,_=rainwater_tree([apron]);hits=ab.overlap(seams)
    assert not hits,'Standing seam crosses apron: '+str(sorted({sn[b] for a,b in hits}))
water_bvh,water_names=rainwater_tree([o for o in objects if o.parent.name=='Rainwater'])
runoff_samples=0
for side in [-1,1]:
    for i in range(41):
        yy=-11.55+i*23.1/40
        hit,normal,index,distance=water_bvh.ray_cast(Vector((side*8.19,yy,7.035)),Vector((0,0,-1)),.22)
        assert hit is not None and water_names[index].startswith('GutterRun'),'Runoff misses open gutter: '+str((side,yy,hit))
        runoff_samples+=1
for side,yy,path in rainwater_paths:
    start=Vector((side*GUTTER_X,yy,7.04))
    hit=water_bvh.ray_cast(start,Vector((0,0,-1)),.27)[0]
    assert hit is None,'Blocked gutter outlet: '+str((side,yy,hit))
    clear_segments=0
    for a,b in zip(path[:-1],path[1:]):
        direction=(b-a).normalized();length=(b-a).length
        hit=water_bvh.ray_cast(a+direction*.0001,direction,max(.0001,length-.0002))[0]
        assert hit is None,'Blocked downpipe bore: '+str((side,yy,hit))
        clear_segments+=1
    # The shoe discharges into open air, then onto the existing splash block.
    direction=(path[-1]-path[-2]).normalized()
    hit,normal,index,distance=water_bvh.ray_cast(path[-1]+direction*.001,direction,.30)
    assert hit is not None and abs(hit.z-.07)<.003,'Shoe misses splash block'
    outlet_checks.append({'side':side,'y':yy,'open_outlet':True,'clear_downpipe_segments':clear_segments,'shoe_hits_splash_block':True})
assert all(apron_top[i][1]>apron_top[i+1][1] for i in range(len(apron_top)-1))
assert max(gutter_rim(y) for y in [-GUTTER_END,0,GUTTER_END])<apron_top[-1][1]-.02
assert GUTTER_X-(GUTTER_RADIUS-GUTTER_WALL)<apron_top[-1][0]<GUTTER_X+(GUTTER_RADIUS-GUTTER_WALL)
roof_clearance=GUTTER_X-GUTTER_RADIUS-max(abs(v.co.x) for o in obstacles if o.name.startswith(('ArchFascia','ArchOuterCoping')) for v in o.data.vertices)
DEFECT_VALIDATION['rainwater']={'gutter_side_offset':GUTTER_X,'outside_radius':GUTTER_RADIUS,'wall_thickness':GUTTER_WALL,'run_end_y':GUTTER_END,'outlet_y':GUTTER_OUTLET,'fall_toward_outlets':GUTTER_FALL,'ridge_to_outlet_drop_m':round(gutter_rim(0)-gutter_rim(GUTTER_OUTLET),6),'minimum_arch_coping_separation_m':round(roof_clearance,6),'gutter_envelope_intersections':[],'apron_masonry_intersections':[],'apron_standing_seam_intersections':[],'roof_sheet_runoff_x':ROOF_RUNOFF_X,'apron_weather_lap_x':[7.58,ROOF_RUNOFF_X],'apron_tip_xz':list(apron_top[-1]),'minimum_tip_above_gutter_rim_m':round(apron_top[-1][1]-gutter_rim(0),6),'runoff_capture_samples':runoff_samples,'outlet_lip_above_trough_m':.0015,'supports':bracket_records,'outlets':outlet_checks,'scope':'Actual source mesh clearance and connected open flow paths; no hydraulic capacity simulation'}
