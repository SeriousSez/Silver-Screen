"""Read the approved master; derive independent, metre-scale canonical fixtures.

Run with Blender --background --python this_file.py. Never saves the master.
The mesh packet stores Unity coordinates, split normals, unchanged UVs and
material indices explicitly; the Unity builder creates native Mesh assets.
This avoids inheriting the building FBX's root correction/combined groups.
"""
import bpy, gzip, hashlib, json, math, pathlib, struct
from mathutils import Matrix, Vector

ROOT = pathlib.Path(__file__).resolve().parents[2]
HERE = pathlib.Path(__file__).resolve().parent
MASTER = ROOT/'ArtSource/Stage1CleanCandidate/Stage1_CleanCandidate_A.blend'
COMMIT = '927ad371a96c4044689286473b542febac6a7265'
OUT = ROOT/'ArtExports/ReusableFixtures'
OUT.mkdir(parents=True, exist_ok=True)
master_hash = hashlib.sha256(MASTER.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(MASTER))
source = [o for o in bpy.data.objects if o.type == 'MESH']

def base(o): return o.name.split('.')[0]
def center(o):
    return Vector(tuple((min(v.co[i] for v in o.data.vertices)+max(v.co[i] for v in o.data.vertices))/2 for i in range(3)))
def select(group, names, point, radius):
    result = [o for o in source if o.parent and o.parent.name == group and base(o) in names.split() and (center(o)-Vector(point)).length < radius]
    assert result, (group,names,point)
    return result
def tagged(tag,value): return [o for o in source if o.get(tag) == value]

# Local source bases, followed by one handedness conversion at the export
# boundary. Normals transform with positions; triangle winding reverses once.
FRONT = Matrix(((1,0,0),(0,0,1),(0,-1,0)))
REAR = Matrix(((-1,0,0),(0,0,1),(0,1,0)))
RIGHT = Matrix(((0,1,0),(0,0,1),(1,0,0)))
INSIDE = Matrix(((0,-1,0),(0,0,1),(-1,0,0)))
HANDEDNESS = Matrix(((-1,0,0),(0,1,0),(0,0,1)))
assets=[]
def add(name,label,family,parts,origin,basis=FRONT,player=True,surface='Wall',suitability='Both',hand='None',anchors=None,clearance=.0):
    if isinstance(parts,list): parts={'Assembly':parts}
    all_objects=[o for group in parts.values() for o in group]
    assert len(set(all_objects)) == len(all_objects), name
    for anchor in anchors or []:
        anchor['position'][0]*=-1;anchor['forward'][0]*=-1
    assets.append(dict(name=name,label=label,family=family,parts=parts,origin=Vector(origin),basis=HANDEDNESS@basis,player=player,surface=surface,suitability=suitability,hand=hand,anchors=anchors or [],clearance=clearance))
def a(name,kind,pos,forward=(0,0,1),diameter=0):
    return dict(name=name,kind=kind,position=list(pos),forward=list(forward),diameter=diameter)

S='PermanentServices'
meter=select(S,'ExteriorServiceCabinet CabinetRaisedLid CabinetHinge CabinetLidScrew AmmeterHousing AmmeterDial AmmeterBezel AmmeterPointer CabinetLatchPull ExteriorCabinetEntryGland',(8.1,-7.6,1.2),.8)
add('MeterCabinet_1930','Exterior meter cabinet','Electrical',meter,(7.9,-7.6,1.19),RIGHT,anchors=[a('Service_Top','Service',(0,.57,.15),(0,1,0),.046)],clearance=.8)
controls='SwitchEscutcheon SwitchMountScrew SwitchBearingBoss SwitchPivotPin SwitchPivotRetainer ForgedSwitchLever ShapedSwitchGrip SwitchTravelStop'
panel=select(S,'DistributionCabinet DistributionDoor PanelMasonryMount '+controls,(7.35,-8.3,1.13),.7)
assert len([o for o in panel if base(o)=='ForgedSwitchLever'])==3
add('DistributionPanel_1930','Three-lever distribution panel','Electrical',panel,(7.585,-8.3,1.13),INSIDE,suitability='Indoor',anchors=[a('Service_TopA','Service',(.20,.53,.225),(0,1,0),.036),a('Service_TopB','Service',(-.20,.53,.225),(0,1,0),.036)],clearance=.8)
intake=select(S,'RearIntakeMount RearIntakeCabinet RearIntakeLid RearCabinetHinge PullEscutcheon EscutcheonScrew ForgedPull RearLidFastener RearIntakeEntryGland',(-2.1,12.2,1),1.05)
add('IntakeCabinet_1930','Electrical intake cabinet','Electrical',intake,(-2.1,11.975,1),REAR,anchors=[a('Service_TopA','Service',(.23,.82,.195),(0,1,0),.046),a('Service_TopB','Service',(-.06,.82,.255),(0,1,0),.046),a('Service_TopC','Service',(-.25,.82,.255),(0,1,0),.046)],clearance=.9)
disc=select(S,'RearDisconnect RearDisconnectCover DisconnectBoss DisconnectLever RearDisconnectEntryGland',(-3.02,12.2,1.46),.4)
add('Disconnect_1930','Disconnect box','Electrical',disc,(-3.02,12.03,1.46),REAR,anchors=[a('Service_Top','Service',(0,.35,.14),(0,1,0),.046)],clearance=.45)
for variant,pos,size,basis,back in [('Shallow',(-5.58,-12.066,2.63),(.16,.085,.20),FRONT,-12.0235),('Deep',(-2.04,12.05,3.10),(.14,.13,.18),REAR,11.985)]:
    parts=select(S,'CastJunctionBox JunctionCover JunctionLidScrew',pos,.16)
    assert len(parts)==6,(variant,[o.name for o in parts])
    add('JunctionBox_'+variant+'_1930',variant+' junction box','Electrical',parts,(pos[0],back,pos[2]),basis,anchors=[a('Service_Rear','Service',(0,0,0),(0,0,-1),.026)],clearance=.25)
pullbox=select(S,'RiserBaseBox RiserWallPenetration RiserCompressionGland',(7.98,-11.02,.37),.3)
add('PullBox_1930','Twin-entry service pull box','Electrical',pullbox,(7.885,-11.02,.37),RIGHT,anchors=[a('Service_TopA','Service',(0,.22,.08),(0,1,0),.036),a('Service_TopB','Service',(0,.22,.135),(0,1,0),.036),a('WallSleeve','Service',(0,0,-.115),(0,0,-1),.076)],clearance=.3)
for state,z in [('Raised',.85),('Lowered',1.17)]:
    parts=select(S,controls,(7.204,-8.3,z),.235)
    # Neighboring controls are more than .30 m away at their mount centres;
    # each unit's grip may extend toward its neighbor: assign by nearest plate.
    plates=[o for o in source if o.parent and o.parent.name==S and base(o)=='SwitchEscutcheon']
    wanted=min(plates,key=lambda o:(center(o)-Vector((7.204,-8.3,z))).length)
    parts=[o for o in select(S,controls,(7.204,-8.3,z),.27) if min(plates,key=lambda p:(center(o)-center(p)).length)==wanted]
    assert len(parts)==13,(state,len(parts))
    add('LeverControl_'+state+'_1930',state+' lever control','ControlHardware',parts,(7.217,-8.3,z),INSIDE,player=False,suitability='Indoor',anchors=[a('LeverAxis','Pivot',(0,0,.084),(1,0,0))],clearance=.2)

lamp=tagged('wall_lamp',16)
assert len(lamp)==6
add('GooseneckLamp_1930','Gooseneck wall lamp','Lighting',lamp,(-5.97,-11.995,3.78),anchors=[a('LightOrigin','Light',(0,-.065,.46),(0,-1,.15)),a('Service_Rear','Service',(0,0,0),(0,0,-1),.026)])
pendant=select('PermanentLighting','SpunEnamelReflector PendantStem PendantClamp PorcelainSocket WarmLampBulb',(-4.35,-10.5,7.15),.6)
assert len(pendant)==5
top=max(v.co.z for o in pendant for v in o.data.vertices)
add('PendantWorkLamp_1930','Pendant work lamp','Lighting',{'Head':[o for o in pendant if base(o) not in ['PendantStem','PendantClamp']],'Mount':[o for o in pendant if base(o) in ['PendantStem','PendantClamp']]},(-4.35,-10.5,top),surface='Ceiling',suitability='Indoor',anchors=[a('HeadSocket','Socket',(0,7.02-top,0),(0,1,0)),a('LightOrigin','Light',(0,6.72-top,0),(0,-1,0))])
bollard=select('FixedProtection','BollardTube BollardFoot BollardAnchor',(-6.8,-12.68,.65),.8)
assert len(bollard)==6
add('Bollard_1930','Bolted safety bollard','SiteProtection',bollard,(-6.8,-12.68,0),surface='Ground')

door=tagged('personnel_opening','FrontLeftEntry')
pull=[o for o in door if base(o) in ['PullEscutcheon','EscutcheonScrew','ForgedPull'] and center(o).y < -12.09]
assert len(pull)==7,[o.name for o in pull]
add('DoorPull_1930','Curved personnel-door pull','DoorHardware',pull,(-5.67,-12.20,1.15),player=False,surface='DoorLeaf')
locknames=['MortiseKeyEscutcheon','KeyholeRound','KeyholeSlot','LockEscutcheonScrew','MortiseLockEdgePlate','LatchTongue','LatchStrike','InteriorMortiseThumbturnPlate','InteriorLockSpindle','InteriorLockThumbturn']
lock=[o for o in door if base(o) in locknames]
parts={'FaceHardware':[o for o in lock if base(o) in locknames[:4]],'EdgeLatch':[o for o in lock if base(o) in locknames[4:6]],'Strike':[o for o in lock if base(o)=='LatchStrike'],'Thumbturn':[o for o in lock if base(o).startswith('Interior')]}
assert all(parts.values())
add('PersonnelLockset_1930','Paired personnel mortise lockset','DoorHardware',parts,(-5.67,-12.20,.82),player=False,surface='DoorLeaf',hand='LeftHinge',anchors=[a('LeafMount','Mount',(0,0,0),(0,0,-1)),a('Spindle','Pivot',(0,0,-.23),(0,0,1)),a('LatchAxis','Latch',(.182,.13,-.10),(1,0,0)),a('StrikeMount','Socket',(.213,.13,-.095),(1,0,0))])
for label,tag,out in [('180','FrontLeftEntry',.18),('190','RearEntry',.19),('140','SideEntry_-1_-10.025',.14)]:
    # Select the actual tagged opening instead of relying on duplicated suffixes.
    if label=='140':
        choices=sorted({o.get('personnel_opening') for o in source if str(o.get('personnel_opening','')).startswith('Side')})
        tag=choices[0]
    d=tagged('personnel_opening',tag)
    hs=[o for o in d if base(o)=='PersonnelHinge']
    assert len(hs)==3,(tag,len(hs))
    h=min(hs,key=lambda o:center(o).z);p=center(h)
    group=[o for o in d if base(o) in ['PersonnelHinge','PersonnelHingeLeaf','CrankedHingeReturn','HingeJambAttachment','HingeLeafScrew'] and abs(center(o).z-p.z)<.10]
    assert len(group)==6,(tag,len(group))
    # Hinge leaf points identify local +X; the leaf/jamb share a fixed axle.
    leaf=next(o for o in group if base(o)=='PersonnelHingeLeaf')
    right=center(leaf)-p;right.z=0
    # Crank contributes a depth offset; use axis-aligned principal horizontal.
    if abs(right.x)>abs(right.y): basis=FRONT if right.x>0 else REAR
    else: basis=RIGHT if right.y>0 else INSIDE
    add('PersonnelHinge_'+label+'_1930','Cranked personnel hinge '+label,'DoorHardware',group,p,basis,player=False,surface='DoorLeaf',hand='LeftHinge',anchors=[a('HingeAxis','Pivot',(0,0,0),(0,1,0)),a('LeafMount','Mount',(.095,0,-(out-.1325)),(0,0,-1)),a('JambMount','Mount',(-.053,0,-.07),(0,0,-1))])

side=select('AcousticTreatment','AcousticPanelBacking FabricAbsorberPanel AcousticPanelStile AcousticPanelRail',(7.45,-8.4,3.27),1.6)
assert len(side)==8,len(side)
stiles=[o for o in side if base(o)=='AcousticPanelStile'];divider=min(stiles,key=lambda o:abs(center(o).y+8.4))
parts={'Backing':[o for o in side if base(o)=='AcousticPanelBacking'],'OuterFrame':[o for o in side if base(o) in ['AcousticPanelRail','AcousticPanelStile'] and o!=divider],'Divider':[divider],'AcousticLeft':[],'AcousticRight':[]}
inserts=sorted([o for o in side if base(o)=='FabricAbsorberPanel'],key=lambda o:-center(o).y)
parts['AcousticLeft']=[inserts[0]];parts['AcousticRight']=[inserts[1]]
add('DisplayFrame_Double_1930','Double industrial acoustic / display frame','Displays',parts,(7.555,-8.4,3.27),INSIDE,suitability='Indoor',anchors=[a('Insert_Left','Insert',(-.5625,0,.1435)),a('Insert_Right','Insert',(.5625,0,.1435))])
rear=select('AcousticTreatment','RearAcousticPanel RearAcousticFrame',(-4.9,11.43,3.1),1.8)
assert len(rear)==5,len(rear)
add('DisplayFrame_Single_1930','Single industrial acoustic / display frame','Displays',{'OuterFrame':[o for o in rear if base(o)=='RearAcousticFrame'],'Acoustic':[o for o in rear if base(o)=='RearAcousticPanel']},(-4.9,11.555,3.1),FRONT,suitability='Indoor',anchors=[a('Insert','Insert',(0,0,.131))])

def vec(v): return [float(c) for c in v]
def packet(objects,basis,origin):
    vertices=[];normals=[];uvs=[];indices={};materials=[];members=[]
    for o in sorted(objects,key=lambda o:o.name):
        assert not o.modifiers,(o.name,'unexpected unapplied modifier')
        m=o.data;m.calc_loop_triangles();lookup={};uv=m.uv_layers.active
        assert uv is not None,o.name
        uv_hash=hashlib.sha256(b''.join(struct.pack('<ff',*d.uv) for d in uv.data)).hexdigest()
        geometry_hash=hashlib.sha256(b''.join(struct.pack('<fff',*v.co) for v in m.vertices)).hexdigest()
        members.append(dict(name=o.name,semanticGroup=o.parent.name,vertices=len(m.vertices),triangles=len(m.loop_triangles),uvSha256=uv_hash,positionSha256=geometry_hash))
        for mat in m.materials:
            if mat.name not in materials:materials.append(mat.name);indices[mat.name]=[]
        for tri in m.loop_triangles:
            ids=[]
            for li in tri.loops:
                vi=m.loops[li].vertex_index;n=m.corner_normals[li].vector
                key=(vi,tuple(n),tuple(uv.data[li].uv))
                if key not in lookup:
                    pos=basis@(m.vertices[vi].co-origin);normal=basis@n
                    assert (basis.transposed()@pos+origin-m.vertices[vi].co).length<.000005
                    lookup[key]=len(vertices)//3;vertices.extend(vec(pos));normals.extend(vec(normal));uvs.extend(vec(uv.data[li].uv))
                ids.append(lookup[key])
            indices[m.materials[tri.material_index].name].extend(reversed(ids))
    return dict(positions=vertices,normals=normals,uv=uvs,materials=materials,submeshes=[dict(indices=indices[m]) for m in materials],members=members)

records=[];packets=[];copies=[]
for spec in assets:
    basis=spec['basis'];origin=spec['origin'];parts=[]
    for part,objects in spec['parts'].items():
        p=packet(objects,basis,origin);p['name']=part;parts.append(p)
    ps=[v for p in parts for v in zip(*[iter(p['positions'])]*3)]
    lo=[min(p[i] for p in ps) for i in range(3)];hi=[max(p[i] for p in ps) for i in range(3)]
    record={k:spec[k] for k in ['name','label','family','player','surface','suitability','hand','anchors','clearance']}
    record.update(id='silverscreen.fixture.'+spec['name'].lower(),origin=vec(origin),basisRows=[float(c) for row in basis for c in row],boundsMin=lo,boundsMax=hi,dimensions=[b-a for a,b in zip(lo,hi)],status='Exact source geometry; canonical pivot, hierarchy and winding repackaging',parts=[dict(name=p['name'],materials=p['materials'],members=p['members'],vertices=len(p['positions'])//3,triangles=sum(len(s['indices'])//3 for s in p['submeshes'])) for p in parts])
    if spec['family']=='Displays':record['interfaceAddition']='Independent flat artwork/matte planes; single frame receives a removable backing board for graphic mode only. No source frame geometry changed.'
    records.append(record);packets.append(dict(name=spec['name'],parts=parts))
    # Editable source stays Blender Z-up, grouped by canonical asset/part.
    coll=bpy.data.collections.new(spec['name']);bpy.context.scene.collection.children.link(coll)
    coll.hide_viewport=spec['name']!='MeterCabinet_1930';coll.hide_render=coll.hide_viewport
    unity_to_blender=FRONT.transposed()@HANDEDNESS
    for part,objects in spec['parts'].items():
        for old in objects:
            new=old.copy();new.data=old.data.copy();new.parent=None;new.matrix_world=Matrix.Identity(4)
            new.name=spec['name']+'__'+part+'__'+old.name
            new.data.transform((unity_to_blender@basis).to_4x4() @ Matrix.Translation(-origin))
            new['source_component']=old.name;new['canonical_part']=part
            coll.objects.link(new);copies.append(new)

manifest=dict(schema=1,masterCommit=COMMIT,source=str(MASTER.relative_to(ROOT)).replace('\\','/'),sourceSha256=master_hash,units='metres',unityAxes='X right, Y up, Z out from mounting face; clockwise triangles',assets=records)
(OUT/'milestone1_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
with (OUT/'milestone1_meshes.json.gz').open('wb') as f:
    with gzip.GzipFile(fileobj=f,mode='wb',mtime=0) as z:z.write(json.dumps(dict(assets=packets),separators=(',',':')).encode())
# Remove only in-memory source objects; save ONLY the new canonical source file.
keep=set(copies)
for o in list(bpy.data.objects):
    if o not in keep:bpy.data.objects.remove(o,do_unlink=True)
for coll in list(bpy.data.collections):
    if not coll.objects and not coll.children:bpy.data.collections.remove(coll)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'ReusableFixtures_Milestone1.blend'))
assert hashlib.sha256(MASTER.read_bytes()).hexdigest()==master_hash,'Protected source changed'
print(json.dumps(dict(assets=len(records),sourceComponents=sum(len(p['members']) for r in records for p in r['parts']),triangles=sum(p['triangles'] for r in records for p in r['parts']),masterUnchanged=True)))
