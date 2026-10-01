"""Compact A4 LOD0 art candidate. Read-only reuse of the A3.2 master.

Blender 5 --background --factory-startup --python this_file
Uses the established semantic mesh-packet pipeline; metres, Z up, front -Y.
Latest user direction: flat roof behind a parapet, NOT a hipped terracotta roof.
"""
import ast, bpy, bmesh, gzip, json, math, random, sys
from pathlib import Path
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
A1 = ROOT/'ArtSource/StageSchoolA1'
A3 = ROOT/'ArtSource/StageSchoolA3'
sys.path[:0] = [str(HERE), str(A1), str(ROOT/'ArtSource/PeriodEnvironment1930')]
import geometry as G
from geometry import mesh, tube
import refinements as R
import planters as P

def functions_from(path, names=None):
    tree = ast.parse(path.read_text())
    nodes = [n for n in tree.body if isinstance(n, ast.FunctionDef) and (names is None or n.name in names)]
    exec(compile(ast.Module(body=nodes, type_ignores=[]), str(path), 'exec'), globals())
    return tree

tree = functions_from(A1/'generate_stage_school.py')
CUSTOM = ast.literal_eval(next(n.value for n in tree.body if isinstance(n, ast.Assign) and any(isinstance(t,ast.Name) and t.id=='CUSTOM' for t in n.targets)))
G.PALETTE.update(CUSTOM)
bpy.ops.wm.open_mainfile(filepath=str(A3/'StageSchool_A3.blend'))
bpy.context.preferences.filepaths.save_version = 0
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1
G.MATS.clear(); G.GROUPS.clear()
G.MATS.update({m.name:m for m in bpy.data.materials if m.library is None and m.name in G.PALETTE})
# Foliage is local to A4: never recolour the shared sage upholstery material.
PLANT_SURFACES={
    'SS_A4_LeafDeep':((.025,.070,.020),0,.55,None),
    'SS_A4_LeafGreen':((.043,.108,.029),0,.51,None),
    'SS_A4_LeafYoung':((.075,.145,.037),0,.48,None),
    'SS_A4_PottingSoil':((.032,.019,.010),0,.98,None),
}
G.PALETTE.update(PLANT_SURFACES)
for n,(color,metallic,roughness,_) in PLANT_SURFACES.items():
    mat=bpy.data.materials.new(n);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=roughness
    G.MATS[n]=mat
P.soil_material(ROOT)
rng = random.Random(193004)
fixtures=[]; equipment=[]; pivots={}; reuse={}
base=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz').read_bytes()))
baseparts={p['name']:p for p in base['parts']}

# Retained geometry remains available in the historical master and asset library.
# Only instances in the NEW in-memory candidate are removed.
def retain(n):
    return (n.startswith('Shell/Front') or n=='Structure/EntranceSteps'
            or n in ('Doors/EntranceLeft','Doors/EntranceRight')
            or n.startswith('Furnishings/Interview/')
            or n.startswith('Furnishings/Audition/')
            or n in ('Furnishings/Waiting/PortraitGallery','Shell/Rear/OfficeClock')
            or n=='Ceilings/Pendant02')

for c in list(bpy.data.collections):
    if c.library is not None: continue
    for o in list(c.objects):
        if o.type!='MESH': bpy.data.objects.remove(o,do_unlink=True)
    if retain(c.name):
        G.GROUPS[c.name]=c
        if c.name in baseparts:
            pivots[c.name]=baseparts[c.name]['pivot'][:]
            reuse[c.name]=dict(source=c.name,offset=[0,0,0])
    else:
        for o in list(c.objects): bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(c)

functions_from(A3/'generate_stage_school_a3.py', {'transform_groups','facade','band','partition'})
exec(compile((A3/'components.py').read_text(),str(A3/'components.py'),'exec'))

def translate(n,dx=0,dy=0,dz=0):
    for o in G.GROUPS[n].objects:
        m=o.matrix_world.copy()
        o.data=o.data.copy()
        for v in o.data.vertices:v.co=m@v.co+Vector((dx,dy,dz))
        o.matrix_world.identity()
    off=[dx,dz,dy]
    if n in pivots:pivots[n]=[a+b for a,b in zip(pivots[n],off)]
    reuse[n]['offset']=[a+b for a,b in zip(reuse[n]['offset'],off)]

for n in list(G.GROUPS):
    if n not in reuse: continue
    if n.startswith('Furnishings/Interview/'):
        translate(n,6.6,-5)
        if n.endswith('/Bookcase'):translate(n,-1.14)
    elif n in ('Furnishings/Audition/Rostrum','Furnishings/Audition/Curtains','Furnishings/Audition/Backdrop'):
        translate(n,-12.5,-8)
    elif n in ('Furnishings/Audition/EvaluationTable','Furnishings/Audition/EvaluationMaterials'):
        translate(n,-16.5,-3.5)

def reuse_as(source,name,dx=0,dy=0,dz=0):
    seed=G.GROUPS[source];c=G.group(name)
    for original in seed.objects:
        o=original.copy();o.data=original.data.copy();c.objects.link(o)
    reuse[name]=dict(source=source,offset=[0,0,0]);pivots[name]=baseparts[source]['pivot'][:]
    translate(name,dx,dy,dz)

for i,x in enumerate([-7.8,-4.8,8.4]):
    reuse_as('Furnishings/Waiting/PortraitGallery','Furnishings/HiringHall/PortraitGallery'+str(i),x+12.125,-4.79)
reuse_as('Shell/Rear/OfficeClock','Furnishings/HiringHall/Clock',-1.7,-13.28)
for seed in ['Furnishings/Waiting/PortraitGallery','Shell/Rear/OfficeClock']:
    for o in list(G.GROUPS[seed].objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(G.GROUPS[seed]);del G.GROUPS[seed];del reuse[seed]

for f0 in base['fixtures']:
    f=dict(f0);f['position']=f0['position'][:];n=f['group']
    if n.startswith('Furnishings/Interview/'):
        f['position'][0]+=6.6;f['position'][2]-=5
        if n.endswith('/Files'):f['position']=[10.1,.38,4.6];f['yaw']=270
        fixtures.append(f)
    elif n.startswith('Furnishings/Audition/Evaluator'):
        f['position'][0]-=16.5;f['position'][2]-=3.5
        fixtures.append(f)
    elif n=='Furnishings/Audition/Notes':
        f['position'][0]-=16.5;f['position'][2]-=3.5
        fixtures.append(f)

equipment=[dict(asset='StudioCamera1930',group='Furnishings/Audition/Camera',position=[-4,.39,1.1],yaw=0),
           dict(asset='StudioLamp1930',group='Furnishings/Audition/StudioLamp0',position=[-7.3,.39,3.35],yaw=50),
           dict(asset='StudioLamp1930',group='Furnishings/Audition/StudioLamp1',position=[-.7,.39,3.35],yaw=-50)]

print('A4 three-room envelope',flush=True)
group('Structure/Foundation');box('Grounded foundation',(0,-1,.16),(22,16,.32),'Concrete',.01)
group('Interior/Floors/Base');box('Finished floor substrate',(0,-1,.335),(21.98,15.98,.05),'SS_Terrazzo',0)
rooms=[('HiringHall',-10.68,10.68,-8.64,-1.59),('Audition',-10.68,2.51,-1.41,6.68),('Interview',2.69,10.68,-1.41,6.68)]
for n,a,b,c,d in rooms:
    group('Interior/Floors/'+n)
    if n=='HiringHall':
        # Architectural inlay is a quiet perimeter; it never depicts hiring boxes.
        for x in [a+.27,b-.27]:box('Terrazzo perimeter border',(x,(c+d)/2,.367),(.09,d-c-.5,.012),'SS_Stone',0)
        for y in [c+.27,d-.27]:box('Terrazzo perimeter border',((a+b)/2,y,.367),(b-a-.5,.09,.012),'SS_Stone',0)
        for x in [-8,-4,0,4,8]:box('Fine brass terrazzo joint',(x,(c+d)/2,.363),(.005,d-c-.65,.006),'Brass',0)
        for y in [-7.2,-4.8,-2.4]:box('Fine transverse terrazzo joint',(0,y,.363),(20.65,.005,.006),'Brass',0)
    else:
        nx=math.ceil((b-a)/.23);w=(b-a)/nx
        for i in range(nx):
            start=c
            while start<d-.01:
                end=min(d,start+rng.uniform(2,3.4))
                box('Oak floor board',(a+(i+.5)*w,(start+end)/2,.368),(w-.002,end-start-.002,.026),'SS_FloorOak' if rng.random()<.78 else 'SS_FloorOakLight',0)
                start=end

# Approved front facade stays full scale. Main roof parapet is lower than its
# sign frieze; the entrance accent is a raised facade, not another roof volume.
for side,(a,b) in enumerate([(-11,-3.6),(3.6,11)]):
    band('Shell/Front/UpperClosure'+str(side),(a,-8.8),(b,-8.8),4.65,4.85)
facade('Shell/Left',(-10.84,6.84),(-10.84,-8.8),4.85,
       [(2,1.56,1.35,2.25),(5.9,1.56,1.35,2.25),(11.2,1.56,1.3,2.25),(14,1.56,1.3,2.25)])
facade('Shell/Right',(10.84,-8.8),(10.84,6.84),4.85,
       [(2,1.56,1.3,2.25),(5,1.56,1.3,2.25),(10.1,1.56,1.35,2.25),(13.8,1.56,1.35,2.25)])
facade('Shell/Rear',(10.84,6.84),(-10.84,6.84),4.85,
       [(1.8,1.56,1.35,2.25),(5.2,1.56,1.35,2.25),(10.2,1.5,4.02,.52),(19.7,1.5,4.02,.52)])
partition('HallAudition',(-10.84,-1.5),(2.6,-1.5),[('AuditionA4',11.35,1.6)],4.65)
partition('HallInterview',(2.6,-1.5),(10.84,-1.5),[('InterviewA4',3.0,1.35)],4.65)
partition('SpecialistDivider',(2.6,6.84),(2.6,-1.5),[],4.65)

# Reuse A3's opening-aware continuous walnut dado routine on the new walls only.
finish=(A3/'production_finish.py').read_text()
joinery=finish[finish.index('TRIM=[]'):finish.index('# The old one-wall finish')]
exec(compile(joinery,str(A3/'production_finish.py'),'exec'))
for i,x in enumerate([-9.6,-3.1,6.8,10.05]):
    lantern('Furnishings/HiringHall/WallLantern'+str(i),x,-1.63,2.90)

# A single low-slope mineral roof, with an uninterrupted flat silhouette.
# The surface falls 180 mm from front to rear. Two rear scuppers discharge
# directly into supported external downpipes. There are no internal valleys.
group('Roofs/Main/Deck')
mesh('Continuous shallow-fall roof deck',
     [(-10.75,-8.75,5.14),(10.75,-8.75,5.14),(10.75,6.75,4.96),(-10.75,6.75,4.96),
      (-10.75,-8.75,4.80),(10.75,-8.75,4.80),(10.75,6.75,4.80),(-10.75,6.75,4.80)],
     [(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'Concrete')
group('Roofs/Main/Parapet')
for x in [-10.84,10.84]:
    box('Side stucco parapet',(x,-1,5.14),(.32,16,.58),'SS_Stucco',.005)
    box('Side stone coping',(x,-1,5.46),(.48,16.12,.10),'SS_Stone',.012)
for a,b in [(-11,-3.62),(3.62,11)]:
    box('Front parapet',((a+b)/2,-8.80,5.14),(b-a,.32,.58),'SS_Stucco',.005)
    box('Front coping',((a+b)/2,-8.80,5.46),(b-a,.49,.10),'SS_Stone',.012)
group('Roofs/Main/RearParapet')
# Physical scupper holes rather than a downpipe ending against solid masonry.
xwall('Roofs/Main/RearParapet',6.84,-11,11,5.43,
      [(-10.38,-10.12,4.93,5.10),(10.12,10.38,4.93,5.10)],th=.32,z0=4.85)
for o in list(G.GROUPS['Roofs/Main/RearParapet'].objects):
    if o.name.startswith('Stone base course'):bpy.data.objects.remove(o,do_unlink=True)
box('Rear stone coping',(0,6.84,5.46),(22.12,.48,.10),'SS_Stone',.012)
group('Roofs/Main/EntranceReturn')
box('Entrance parapet rear face',(0,-8.80,5.43),(7.24,.30,.86),'SS_Stucco',.008)
box('Entrance cornice roof-side cap',(0,-9.02,5.79),(7.20,.66,.08),'SS_Stone',.008)
group('Roofs/Main/Flashing')
for x in [-10.655,10.655]:
    # Flashing follows the true roof fall and remains below the coping.
    mesh('Side parapet upstand',[(x,-8.63,5.14),(x,6.63,4.964),(x,6.63,5.28),(x,-8.63,5.28)],[(0,1,2,3)],'Galvanized')
box('Front parapet base flashing',(0,-8.62,5.196),(21.30,.025,.12),'Galvanized',0)
for a,b in [(-10.65,-10.38),(-10.12,10.12),(10.38,10.65)]:
    box('Rear flashing clear of scuppers',((a+b)/2,6.66,5.024),(b-a,.025,.12),'Galvanized',0)
group('Roofs/Main/Rainwater')
for x in [-10.25,10.25]:
    box('Scupper floor',(x,6.88,4.943),(.25,.64,.014),'Galvanized',0)
    for dx in [-.127,.127]:box('Scupper cheek',(x+dx,6.88,5.01),(.014,.64,.15),'Galvanized',0)
    # Open-top receiving hopper, connected to the through-wall scupper.
    box('Hopper front',(x,7.23,4.84),(.34,.018,.28),'Galvanized',.002)
    for dx in [-.165,.165]:box('Hopper side',(x+dx,7.10,4.84),(.018,.28,.28),'Galvanized',.002)
    box('Hopper bottom',(x,7.10,4.70),(.34,.28,.018),'Galvanized',.002)
    tube('Rear ground downpipe',[(x,7.10,4.71),(x,7.10,.28),(x,7.27,.16)],.05,'Galvanized',wall=.004,sides=12)
    for z in [.70,2.3,4.1]:
        box('Downpipe wall saddle',(x,7.045,z),(.14,.12,.035),'Steel',.004)
    box('Rear splash block',(x,7.32,.065),(.42,.50,.13),'Concrete',.012)

print('A4 ceilings, fixtures and exterior applicant forecourt',flush=True)
for n,a,b,c,d in rooms:
    group('Ceilings/'+n)
    box('Finished plaster ceiling',((a+b)/2,(c+d)/2,4.64),(b-a+.02,d-c+.02,.08),'InteriorPlaster',0)
    for x in [a+.045,b-.045]:box('Ceiling timber perimeter',(x,(c+d)/2,4.55),(.09,d-c,.10),'SS_Walnut')
    for y in [c+.045,d-.045]:box('Ceiling timber perimeter',((a+b)/2,y,4.55),(b-a,.09,.10),'SS_Walnut')

# Linked mesh reuse: approved pendant extended to its new ceiling, no float.
pendant=G.GROUPS['Ceilings/Pendant02']
for i,(x,y) in enumerate([(-7,-5),(-2.35,-5),(2.35,-5),(7,-5),(-4,1.6),(6.6,2.8)]):
    name='Ceilings/PendantA4_%02d'%i
    c=G.group(name)
    for original in pendant.objects:
        o=original.copy();o.data=original.data.copy();c.objects.link(o)
    reuse[name]=dict(source='Ceilings/Pendant02',offset=[0,0,0]);pivots[name]=[0,0,0]
    translate(name,x+6.7,y+5,.54)
# Remove only this candidate's unused seed instance.
for o in list(pendant.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(pendant);del G.GROUPS['Ceilings/Pendant02'];del reuse['Ceilings/Pendant02']

# Two unobstructed pavement pockets flank the central stair. Benches are shared
# period assets; the standing positions below exist only in authoring metadata.
group('Exterior/Forecourt')
# Building-owned landing and two discrete waiting pockets end at the lot.
# The future player path connects at the centered landing edge; no public walk.
box('Clear central approach',(0,-11.12,.025),(7.55,.86,.05),'SS_Stone',.012)
for y in [-11.48,-10.76]:box('Entrance landing border',(0,y,.051),(7.49,.055,.002),'SS_Terrazzo',0)
for x in [-3.70,3.70]:box('Entrance landing border',(x,-11.12,.051),(.055,.72,.002),'SS_Terrazzo',0)
for x in [-1.55,0,1.55]:box('Entrance paving expansion joint',(x,-11.12,.052),(.004,.61,.002),'Mortar',0)
for side in [-1,1]:
    x=side*7.2
    box('Applicant forecourt paving',(x,-10.2,.025),(6.45,2.7,.05),'SS_Stone',.012)
    for y in [-11.48,-8.96]:box('Forecourt border',(x,y,.051),(6.42,.055,.002),'SS_Terrazzo',0)
    for dx in [-3.16,3.16]:box('Forecourt border',(x+dx,-10.2,.051),(.055,2.55,.002),'SS_Terrazzo',0)
    for dx in [-1.55,0,1.55]:box('Paving expansion joint',(x+dx,-10.2,.052),(.004,2.45,.002),'Mortar',0)
    # Canonical period bench seats face +Z. Rotate outward toward the lot (-Z).
    # At Z=-9.58 the complete back clears the projecting facade strips by
    # 117.6 mm, measured from the imported mesh envelope at bench height.
    fixtures.append(dict(asset='BenchSlatted_210_1930',group='Exterior/ApplicantBench'+str(side),position=[x,.05,-9.58],yaw=180))
    group('Exterior/Planters')
    for xx in [side*4.15,side*10.18]:
        P.build_planter(xx,193004+int((xx+11)*100))

(HERE/'planter_report.json').write_text(json.dumps(dict(planters=P.REPORT,total_triangles=sum(p['triangles'] for p in P.REPORT),total_budget=4*P.MAX_TRIANGLES,soil_texture='128 x 128 shared albedo; no dirt-detail geometry'),indent=2))

# Join new floor-board objects only; UVs and semantic assembly boundaries remain.
for name,c in list(G.GROUPS.items()):
    candidates=[o for o in c.objects if o.type=='MESH' and o.name.startswith('Oak floor board')]
    if len(candidates)>1:
        bpy.ops.object.select_all(action='DESELECT')
        for o in candidates:o.select_set(True)
        bpy.context.view_layer.objects.active=candidates[0];bpy.ops.object.join()

print('A4 established semantic packet export',flush=True)
parts=[]
for name,c in G.GROUPS.items():
    if not any(o.type=='MESH' for o in c.objects):continue
    p=G.uv_packet(c);p['pivot']=pivots.get(name,[0,0,0])
    if name in reuse:
        p['sourceGroup']=reuse[name]['source'];p['offset']=reuse[name]['offset']
    parts.append(p)
used=sorted({m for p in parts for m in p['materials']})
data=dict(schema=1,candidate='StageSchool_A4',parts=parts,
          materials=[dict(name=n,linearRGB=G.PALETTE[n][0],metallic=G.PALETTE[n][1],roughness=G.PALETTE[n][2],texture=G.PALETTE[n][3],shared=n not in CUSTOM and n not in PLANT_SURFACES) for n in used],fixtures=fixtures,equipment=equipment)
G.save_packet(ROOT/'ArtExports/StageSchoolA4/stage_school_meshes.json.gz',data)

readiness=dict(coordinate_system='Unity X/Y/Z metres; documentation only, not runtime anchors',
    hall_regions=[dict(center=[x,.38,-5.45],size=[3.4,3.2]) for x in [-7.2,-2.4,2.4,7.2]],
    fourth_region_contexts=['Applicant: CREATE / IMPORT TALENT','Employee: DISMISS'],
    performers=[[-4.85,.73,4.7],[-3.15,.73,4.7]],camera=[-4,.39,1.1],operator=[-4,.39,-.05],
    screen_test=[-.7,.38,1.25],interview_applicants=[[5.85,.38,2.3],[7.35,.38,2.3]],
    interviewer=[6.6,.39,4.6],interview_marker=[4.6,.38,1.4],
    applicants_outside=[[x,.05,-10.55] for x in [-9.2,-7.2,-5.2,5.2,7.2,9.2]],
    central_approach_clear_width=7.6,
    path_connection=dict(id='path.connection.main',position=[0,.05,-11.55],forward=[0,0,-1],
        purpose='Future player-built spline path connection at entrance landing edge; metadata only'))
(HERE/'authoring_readiness.json').write_text(json.dumps(readiness,indent=2))
report=dict(candidate='StageSchool_A4',footprint=[22,16],gross_floor_area=352,
    net_room_area=sum((b-a)*(d-c) for n,a,b,c,d in rooms),roof_masses=1,
    roof='Single shallow-fall mineral roof concealed by stone-capped stucco parapet; raised entrance facade only',
    roof_direction='Latest user correction supersedes the original terracotta roof brief',
    roof_fall=.18,room_floor_extents=rooms,clear_ceiling_height=4.60-.38,
    source_groups=len(parts),triangles=sum(len(s['indices'])//3 for p in parts for s in p['submeshes']),
    vertices=sum(len(p['positions'])//3 for p in parts),materials=used,
    reused_assemblies=reuse,fixtures=fixtures,equipment=equipment,openings=WINDOWS,doors=DOORS,walls=WALLS,
    parts=[dict(name=p['name'],triangles=sum(len(s['indices'])//3 for s in p['submeshes']),vertices=len(p['positions'])//3) for p in parts])
(HERE/'generation_report.json').write_text(json.dumps(report,indent=2))

for folder,filename,items,equip in [
    ('PeriodEnvironment1930','PeriodEnvironment1930.blend',fixtures,False),
    ('ProductionEquipment1930Candidate','ProductionEquipment1930Candidate.blend',equipment,True)]:
    names={f['asset'] for f in items}
    with bpy.data.libraries.load(str(ROOT/'ArtSource'/folder/filename),link=True) as (src,dst):dst.collections=[n for n in names if n in src.collections]
    kit={c.name:c for c in dst.collections if c}
    for f in items:
        o=bpy.data.objects.new('Shared '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=kit[f['asset']]
        G.group(f['group']).objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(-f['yaw'] if equip else 180-f['yaw'])
for lib in bpy.data.libraries:
    for folder in ['ProductionEquipment1930Candidate','PeriodEnvironment1930']:
        if folder in lib.filepath:lib.filepath='//../'+folder+'/'+folder+'.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'StageSchool_A4.blend'))
print('A4 authored:',json.dumps({k:report[k] for k in ['footprint','gross_floor_area','net_room_area','roof_masses','source_groups','triangles','vertices']}),flush=True)
