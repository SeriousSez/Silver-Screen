"""A2 architectural expansion using approved A1 assemblies and mesh-packet workflow.
Blender 5 --background --factory-startup --python this_file. Metres, Z up, front -Y.
Approved equipment and A1 sources are read-only dependencies, never regenerated here.
"""
import bpy,bmesh,math,json,sys,random,ast,gzip,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];A1=ROOT/'ArtSource/StageSchoolA1'
sys.path[:0]=[str(A1),str(ROOT/'ArtSource/PeriodEnvironment1930')]
import geometry as G
from geometry import mesh,tube
import refinements as R
# Reuse the established authoring functions, without executing A1's build/output code.
tree=ast.parse((A1/'generate_stage_school.py').read_text())
CUSTOM=ast.literal_eval(next(n.value for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='CUSTOM' for t in n.targets)))
G.PALETTE.update(CUSTOM)
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),str(A1/'generate_stage_school.py'),'exec'))
bpy.ops.wm.open_mainfile(filepath=str(A1/'StageSchool_A1.blend'))
bpy.context.preferences.filepaths.save_version=0
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
G.GROUPS.clear();G.MATS.clear();R.TILES.clear();R.TR.seed(193001)
G.MATS.update({m.name:m for m in bpy.data.materials if m.library is None and m.name in G.PALETTE})
base=json.loads(gzip.decompress((ROOT/'ArtExports/StageSchoolA1/stage_school_meshes.json.gz').read_bytes()))
rng=random.Random(193002);fixtures=[];pivots={};reuse=[]

def retain(name):
 return name.startswith('Shell/Front') or name=='Structure/EntranceSteps' or name.startswith('Furnishings/') or name in ['Doors/EntranceLeft','Doors/EntranceRight','Doors/Washroom','Partitions/Washroom','Partitions/WashroomSide','Shell/Rear/OfficeClock'] or name.startswith('Ceilings/Pendant')

for c in list(bpy.data.collections):
 if c.library is not None:continue
 for o in list(c.objects):
  if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
 if retain(c.name):G.GROUPS[c.name]=c
 else:
  for o in list(c.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(c)
for p in base['parts']:
 if p['name'] in G.GROUPS:pivots[p['name']]=p['pivot']

def shift(g,dx=0,dy=0,dz=0):
 if g not in G.GROUPS:return
 for o in G.GROUPS[g].objects:
  m=o.matrix_world.copy()
  for v in o.data.vertices:v.co=m@v.co+Vector((dx,dy,dz))
  o.matrix_world.identity()
 if g in pivots:
  p=pivots[g];pivots[g]=[p[0]+dx,p[1]+dz,p[2]+dy]
 reuse.append(dict(group=g,translation=[dx,dz,dy]))

# Translation only for approved visible assemblies. No equipment mesh is edited.
for g in list(G.GROUPS):
 if g.startswith('Furnishings/Interview/'):shift(g,dy=3)
 elif g in ['Furnishings/Audition/Rostrum','Furnishings/Audition/Curtains','Furnishings/Audition/Backdrop']:shift(g,1.6,6.45)
 elif g=='Furnishings/Audition/EvaluationTable':shift(g,2.3,.35)
 elif g=='Furnishings/Audition/EvaluationMaterials':shift(g,2.3,.35)
 elif g=='Furnishings/Waiting/PortraitGallery':shift(g,dy=1.20,dz=.15)
 elif g=='Furnishings/Waiting/Notices':shift(g,-7.1,1.20)
 elif g=='Furnishings/Staff/ArchiveBoxes':shift(g,-2.8,5.1)
 elif g=='Shell/Rear/OfficeClock':shift(g,dy=3.10)
pendants=[(0,-6.2),(0,.2),(-6.7,-5),(-6.5,.3),(4.6,-4.5),(0,8.6),(8.5,8.3),(-10.5,5.8)]
oldpendants=[(0,-6.2),(0,-.2),(-6.7,-5),(-6.5,.3),(4.6,-4.5),(0,5.6),(6.8,4.3),(-6.7,6.5)]
for i,((x,y),(ox,oy)) in enumerate(zip(pendants,oldpendants)):shift('Ceilings/Pendant%02d'%i,x-ox,y-oy,.75 if i==6 else 0)
for f in base['fixtures']:
 f=dict(f);f['position']=f['position'][:];g=f['group']
 if g.startswith('Furnishings/Interview/'):f['position'][2]+=3
 elif g.startswith('Furnishings/Audition/Evaluator'):
  f['position'][0]=[7.65,8.5][int(g[-1])];f['position'][2]=2.95
 elif g=='Furnishings/Audition/Notes':f['position'][0]+=2.3;f['position'][2]+=.35
 elif g=='Furnishings/Waiting/Noticeboard':f['position'][0]-=7.1;f['position'][2]+=1.2
 elif g.startswith('Furnishings/Staff/Rack'):
  i=int(g[-1]);f['position']=[-12.7+i*1.85,.36,13.35];f['group']='Furnishings/Records/Rack'+str(i)
 elif g=='Furnishings/Staff/Cupboard':f['position']=[-5.1,.36,13.2];f['yaw']=180;f['group']='Furnishings/Storage/Cupboard'
 elif g=='Furnishings/Staff/Files':f['position']=[-12.7,.38,10.6];f['yaw']=90;f['group']='Furnishings/Records/Files'
 fixtures.append(f)
equipment=[dict(asset='StudioCamera1930',group='Furnishings/Audition/Camera',position=[8.5,.39,7.8],yaw=0),dict(asset='StudioLamp1930',group='Furnishings/Audition/StudioLamp0',position=[5.65,.39,11],yaw=50),dict(asset='StudioLamp1930',group='Furnishings/Audition/StudioLamp1',position=[11.35,.39,11],yaw=-50)]

def transform_groups(names,a,angle):
 rot=Matrix.Rotation(angle,4,'Z');origin=Vector((*a,0))
 for name in names:
  for o in G.GROUPS[name].objects:
   m=o.matrix_world.copy()
   for v in o.data.vertices:v.co=origin+rot@(m@v.co)
   o.matrix_world.identity()
  if name in pivots:
   p=pivots[name];v=origin+rot@Vector((p[0],p[2],p[1]));pivots[name]=[v.x,v.z,v.y]

def facade(name,a,b,height,windows=(),doors=(),ornament=True):
 """Counterclockwise perimeter edge; local -Y faces outdoors on every elevation."""
 length=(Vector(b)-Vector(a)).length;angle=math.atan2(b[1]-a[1],b[0]-a[0]);before=set(G.GROUPS)
 holes=[(u-w/2,u+w/2,z,z+h) for u,w,z,h in windows]+[(u-w/2,u+w/2,.36,2.76) for _,u,w in doors]
 xwall(name,0,0,length,height,holes)
 # A1's world-Y helper places skirting according to front/rear sign. In this
 # local outward-facing frame the interior is always +Y.
 for o in G.GROUPS[name].objects:
  if o.name.startswith('Interior timber skirting'):
   for v in o.data.vertices:v.co.y+=.36
 for u,w,z,h in windows:window(name+'/Windows',u,-.025,z,w,h)
 for dn,u,w in doors:
  frame(name+'/DoorSurrounds',u,0,w);door(dn,u,-.025,w-.10,2.34)
  group(name+'/DoorSurrounds');box('Stone threshold',(u,0,.38),(w+.14,.48,.055),'SS_Stone',.006)
 group(name+'/Articulation')
 # A continuous substantial plinth, horizontal joints and fine cornice returns.
 for z,depth,h in [(1.04,.355,.10),(height-.27,.40,.10),(height-.10,.50,.12),(height+.015,.58,.065)]:
  box('Stone string or cornice',(length/2,-.035,z),(length,depth,h),'SS_Stone',.008)
 for z in [.46,.73]:
  spans=[(0,length)]
  for _,u,w in doors:
   spans=[seg for x0,x1 in spans for seg in [(x0,min(x1,u-w/2)),(max(x0,u+w/2),x1)] if seg[1]-seg[0]>.02]
  for x0,x1 in spans:box('Foundation course joint',((x0+x1)/2,-.184,z),(x1-x0,.006,.006),'Mortar',0)
 if ornament:
  for u in [.23,length-.23]:
   box('Expressed corner pier',(u,-.073,(height+.36)/2),(.30,.39,height-.36),'SS_Stone',.006)
   for j in range(max(1,int((height-1.05)/.43))):
    z=1.15+j*.43;box('Corner block bed joint',(u,-.272,z),(.31,.006,.007),'Mortar',0)
  # Subtle scored ashlar at the base; large untextured stucco fields remain calm.
  for row in range(2):
   for i in range(int(length/.85)):
    u=.42+i*.85+(row%2)*.35
    if u>length-.12 or any(abs(u-du)<dw/2+.08 for _,du,dw in doors):continue
    box('Plinth staggered vertical joint',(u,-.185,.595+row*.27),(.005,.005,.255),'Mortar',0)
  # Panel heads and short pilasters articulate grouped windows without fake masonry.
  for u,w,z,h in windows:
   if h<1:continue
   box('Dressed lintel crown',(u,-.24,z+h+.105),(w+.52,.20,.085),'SS_Stone',.008)
   for dx in [-w/2-.21,w/2+.21]:
    box('Shallow opening pier',(u+dx,-.162,2.22),(.10,.08,2.25),'SS_Stone',.004)
  for i in range(1,max(1,math.ceil(length/3.9))):
   u=i*length/math.ceil(length/3.9)
   if any(abs(u-h[0])<h[1]/2+.44 for h in windows) or any(abs(u-d[1])<d[2]/2+.45 for d in doors):continue
   box('Stucco recessed field edge',(u,-.17,2.45),(.055,.045,2.63),'SS_Stucco',.003)
 transform_groups(set(G.GROUPS)-before,a,angle)

def band(name,a,b,z0,z1,windows=()):
 length=(Vector(b)-Vector(a)).length;before=set(G.GROUPS);group(name)
 xs=sorted(set([0,length]+[v for u,w,z,h in windows for v in [u-w/2,u+w/2]]));zs=sorted(set([z0,z1]+[v for u,w,z,h in windows for v in [z,z+h]]))
 for x0,x1 in zip(xs,xs[1:]):
  for lo,hi in zip(zs,zs[1:]):
   if any(u-w/2<(x0+x1)/2<u+w/2 and z<(lo+hi)/2<z+h for u,w,z,h in windows):continue
   box('Upper masonry band',((x0+x1)/2,0,(lo+hi)/2),(x1-x0,.25,hi-lo),'SS_Stucco',0)
 for u,w,z,h in windows:window(name+'/Windows',u,-.015,z,w,h)
 group(name+'/Cornice');box('Upper stone cornice',(length/2,0,z1-.04),(length,.38,.12),'SS_Stone')
 transform_groups(set(G.GROUPS)-before,a,math.atan2(b[1]-a[1],b[0]-a[0]))

def partition(name,a,b,doors=(),height=3.8):
 length=(Vector(b)-Vector(a)).length;before=set(G.GROUPS)
 xwall('Partitions/'+name,0,0,length,height,[(u-w/2,u+w/2,.36,2.76) for _,u,w in doors],'InteriorPlaster',.18)
 for dn,u,w in doors:frame('Partitions/'+name+'/Trim',u,0,w);door(dn,u,0,w-.12,2.34,76)
 transform_groups(set(G.GROUPS)-before,a,math.atan2(b[1]-a[1],b[0]-a[0]))

print('A2 foundations, floors and articulated shell',flush=True)
# Union of purposeful volumes; no full rectangular slab beyond the wall silhouette.
volumes=[('Front',-11,11,-9,1),('WestCommons',-14,-3,1,3.3),('WestWork',-14,-3,3.3,14),('Hall',-3,3,1,5),('Interview',-3,3,5,12),('Audition',3,14,1,15)]
for n,a,b,c,d in volumes:
 group('Structure/Foundation/'+n);box('Grounded foundation',((a+b)/2,(c+d)/2,.16),(b-a,d-c,.32),'Concrete',.01)
 group('Interior/Floors/'+n+'Base');box('Finished floor substrate',((a+b)/2,(c+d)/2,.335),(b-a-.02,d-c-.02,.05),'SS_Terrazzo',0)

floor_rooms=[('Waiting',-10.65,-3.1,-8.65,-1.0),('Commons',-10.65,-3.1,-.9,3.15),('CommonsBay',-13.65,-10.65,1.15,3.15),('Audition',3.15,13.65,1.15,14.65),('Interview',-2.85,2.85,5.15,11.65),('Flexible',-13.65,-7.1,3.45,7.95),('Staff',-6.9,-3.1,3.45,7.95)]
for n,a,b,c,d in floor_rooms:
 group('Interior/Floors/'+n);nx=math.ceil((b-a)/.23);w=(b-a)/nx
 # Long boards with restrained joints rather than needless bevel subdivisions.
 for i in range(nx):
  start=c
  while start<d-.01:
   end=min(d,start+rng.uniform(2.0,3.4));box('Oak floor board',(a+(i+.5)*w,(start+end)/2,.368),(w-.002,end-start-.002,.026),'SS_FloorOak' if rng.random()<.78 else 'SS_FloorOakLight',0);start=end
group('Interior/Floors/HallInlay')
for x in [-2.78,2.78]:box('Hall border',(x,-2,.366),(.07,13.5,.010),'SS_Stone',0)
for x in [-2,-1,0,1,2]:box('Terrazzo brass strip',(x,-2,.365),(.007,13.4,.008),'Brass',0)
for y in range(-8,5):box('Terrazzo brass cross strip',(0,y,.365),(5.5,.007,.008),'Brass',0)

# Clockwise-facing local fronts follow the counterclockwise footprint perimeter.
facade('Shell/Left/Waiting',(-10.84,1),(-10.84,-8.8),4.65,[(3.0,1.56,1.3,2.25),(7.1,1.56,1.3,2.25)])
facade('Shell/Left/CommonShoulder',(-13.84,1),(-10.84,1),4.30,[(1.5,1.35,1.4,1.9)])
facade('Shell/Left/CommonBay',(-13.84,3.3),(-13.84,1),4.30,[(1.15,1.25,1.4,1.9)])
facade('Shell/Left/Evaluation',(-13.84,8.1),(-13.84,3.3),5.10,[(1.4,1.5,1.35,2.35),(3.4,1.5,1.35,2.35)])
facade('Shell/Left/Records',(-13.84,13.84),(-13.84,8.1),4.40,[(1.45,1.4,1.65,1.9),(4.60,1.1,1.65,1.9)])
facade('Shell/Rear/Service',(-3,13.84),(-13.84,13.84),4.40,[(1.45,1.4,1.55,2.0),(7.0,1.4,1.55,2.0),(9.35,1.4,1.55,2.0)],[('RearServiceA2',4.60,1.45)])
facade('Shell/Rear/WestSetback',(-3,12),(-3,13.84),4.40,[],[],False)
facade('Shell/Rear/Interview',(3,11.84),(-3,11.84),4.97,[(1.35,1.5,1.4,2.25),(4.55,1.5,1.4,2.25)])
facade('Shell/Rear/AuditionReturn',(3,14.84),(3,12),5.70,[(1.45,1.2,4.5,.75)])
facade('Shell/Rear/Audition',(13.84,14.84),(3,14.84),5.70,[(1.4,1.25,4.55,.75),(9.35,1.25,4.55,.75)])
facade('Shell/Right/Audition',(13.84,1),(13.84,14.84),5.70,[(2,1.65,1.5,2.25),(6.6,1.65,1.5,2.25),(10.7,1.65,1.5,2.25),(2,1.65,4.5,.75),(6.6,1.65,4.5,.75),(10.7,1.65,4.5,.75)])
facade('Shell/Right/AuditionShoulder',(10.84,1),(13.84,1),5.70,[(1.5,1.35,1.5,2.25),(1.5,1.35,4.5,.75)])
facade('Shell/Right/Preparation',(10.84,-8.8),(10.84,1),4.65,[(2,1.40,1.65,1.9),(6.8,1.65,1.3,2.25)])

# Source organization distinguishes occupied partitions from high/clerestory shell.
partition('InterviewFront',(-3,5),(3,5),[('InterviewA2',2.65,1.35)],4.05)
partition('AuditionFront',(3,1),(10.84,1),[('AuditionA2',1.95,1.55)],4.1)
partition('AuditionWest',(3,12),(3,1),[],4.1)
partition('WestSpine',(-3,3.3),(-3,12),[('StaffInterviewLink',2.5,1.2)],4.05)
partition('WestRoomFront',(-14,3.3),(-3,3.3),[('FlexibleA2',4.7,1.35),('StaffA2',8.5,1.2)],3.8)
partition('StaffDivider',(-7,3.3),(-7,8.1),[],3.8)
partition('ServiceFront',(-14,8.1),(-3,8.1),[('FlexibleService',3.0,1.15),('StaffService',8.8,1.15)],3.8)
partition('RecordsFront',(-14,10),(-8.5,10),[('RecordsA2',4.35,1.15)],3.65)
partition('StoreFront',(-6.7,10),(-3,10),[('StoreA2',1.10,1.15)],3.65)
partition('RecordsPassage',(-8.5,10),(-8.5,13.84),[],3.65)
partition('StorePassage',(-6.7,10),(-6.7,13.84),[],3.65)
frame('Partitions/WashroomTrim',7.75,-4.15,1.0)
band('Shell/Raised/EntryLeft',(-3.50,-2.5),(-3.50,-8.8),4.30,5.90,[(1.35,1.4,5.05,.50),(4.35,1.4,5.05,.50)])
band('Shell/Raised/EntryRight',(3.50,-8.8),(3.50,-2.5),4.30,5.90,[(1.6,1.4,5.05,.50),(4.6,1.4,5.05,.50)])
band('Shell/Raised/EntryRear',(3.5,-2.5),(-3.5,-2.5),4.75,5.90)
band('Shell/Raised/HallLeft',(-3,5),(-3,-2.5),3.8,5.4,[(4.0,1.5,4.85,.42),(6.1,1.5,4.85,.42)])
band('Shell/Raised/HallRight',(3,-2.5),(3,5),4.1,5.4,[(1.3,1.55,4.85,.42)])
band('Shell/Raised/HallRear',(3,5),(-3,5),4.05,5.4)
band('Shell/Raised/AuditionWest',(3,12),(3,1),4.1,5.70,[(3.0,1.6,4.65,.6),(7.7,1.6,4.65,.6)])
band('Shell/Raised/AuditionFront',(3,1),(10.84,1),4.1,5.70,[(3.5,1.8,5.05,.44),(6.1,1.8,5.05,.44)])
band('Shell/Raised/WestFront',(-14,3.3),(-3,3.3),3.8,5.10)
band('Shell/Raised/WestRear',(-3,8.1),(-14,8.1),3.8,5.10)
band('Shell/Raised/WestSide',(-3,3.3),(-3,8.1),4.05,5.10)
band('Shell/Raised/WaitingBay',(-10.84,3.3),(-10.84,1),4.0,4.65)
band('Shell/Raised/InterviewLeft',(-3,12),(-3,5),4.05,4.97)

# Recessed architectural accents reinforce all elevations without changing palette.
group('Shell/Front/AddedArticulation')
for x in [-10.6,-6.65,6.65,10.6]:
 box('Front recessed-field pilaster',(x,-9.035,2.55),(.15,.075,3.6),'SS_Stone',.004)
for side in [-1,1]:
 for j in range(3):
  for i in range(7):box('Restrained front stone course',(side*(4.0+i*.91+(j%2)*.20),-9.075,.78+j*.24),(.005,.006,.20),'Mortar',0)

# The rear of the performance room is necessarily blind below its clerestory.
# Model three solid wall bays instead of leaving a featureless acoustic wall.
group('Shell/Rear/Audition/BlindBays')
for x in [4.65,8.42,12.19]:
 for dx in [-1.35,1.35]:
  box('Rear acoustic bay pilaster',(x+dx,15.045,2.60),(.18,.13,3.1),'SS_Stone',.008)
  box('Pilaster foot',(x+dx,15.075,1.15),(.25,.18,.18),'SS_Stone',.008)
  box('Pilaster capital',(x+dx,15.075,4.16),(.27,.18,.12),'SS_Stone',.008)
 for z in [1.35,4.00]:box('Recessed stucco panel border',(x,15.03,z),(2.55,.09,.07),'SS_Stone',.005)
 box('Calm recessed stucco field',(x,15.005,2.67),(2.46,.035,2.56),'SS_Stucco',.006)

# Room-specific interior trim; higher exterior volumes do not imply another floor.
for n,a,b,y in [('Interview',-2.8,2.8,11.66),('Flexible',-13.65,-7.15,7.96),('Audition',3.2,13.65,14.66)]:
 group('Partitions/'+n+'Finish')
 box('Picture rail',((a+b)/2,y,2.85),(b-a,.05,.07),'SS_Walnut')
 box('Dado rail',((a+b)/2,y,1.17),(b-a,.06,.075),'SS_Walnut')
 for i in range(int((b-a)/.85)):
  x=a+.42+i*.85;box('Raised timber dado',(x,y,.79),(.75,.04,.73),'SS_Walnut',.006)

print('A2 stepped roofs and connected drainage',flush=True)
roofs=[('Entrance',0,-5.8,7.6,7.2,5.97,1.15),('Waiting',-7.25,-3.0,8.1,12.5,4.79,1.18),('Preparation',7.25,-3.9,8.1,10.9,4.79,1.05),('Hall',0,1.25,6.8,8.2,5.50,.90),('WestEvaluation',-8.6,5.7,11.6,5.5,5.25,1.05),('WestService',-8.6,11.15,11.6,6.5,4.55,.95),('CommonBay',-12.4,2.2,4,3.2,4.44,.55),('Interview',0,8.7,6.8,7.4,5.12,1.02),('Audition',8.5,8.0,11.8,14.8,5.85,1.55)]
for args in roofs:hip_roof(*args)

def flashing(name,wall_a,wall_b,roof):
 _,cx,cy,w,d,eave,rise=roof;ha=w/2;hb=d/2;hip=min(ha,hb)*.87
 def surface(x,y):return eave+rise*max(0,min(1,(ha-abs(x-cx))/hip,(hb-abs(y-cy))/hip))+.118
 a,b=Vector(wall_a),Vector(wall_b);t=(b-a).normalized();out=Vector((t.y,-t.x));vv=[];ff=[];steps=max(2,math.ceil((b-a).length/.35))
 for i in range(steps+1):
  p=a.lerp(b,i/steps)
  for offset,up in [(0,.18),(0,0),(.27,0)]:
   q=p+out*offset;vv.append((q.x,q.y,surface(q.x,q.y)+up))
 for i in range(steps):
  for j in range(2):k=i*3+j;ff.append((k,k+1,k+4,k+3))
 group('Roofs/'+name+'/Flashing');o=mesh('Formed stepped roof apron',vv,ff,'Galvanized')
 m=o.modifiers.new('Sheet flashing gauge','SOLIDIFY');m.thickness=.003;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
flashings=[('Waiting',(-3.52,-8.9),(-3.52,-2.5),roofs[1]),('Preparation',(3.52,-2.5),(3.52,-8.9),roofs[2]),('Hall',(-3.35,-2.5),(3.35,-2.5),roofs[3]),('Waiting',(-11.2,3.27),(-3.2,3.27),roofs[1]),('CommonBay',(-13.9,3.27),(-11.0,3.27),roofs[6]),('CommonBay',(-10.99,3.15),(-10.99,1),roofs[6]),('Preparation',(3.2,1.02),(11.2,1.02),roofs[2]),('Interview',(3.02,11.85),(3.02,5.1),roofs[7]),('WestService',(-3.15,8.13),(-13.95,8.13),roofs[5])]
for args in flashings:flashing(*args)

def drainage(name,a,b,eave,wall_offset=.27):
 # Runs are outside roof drip edges. All hardware is a child of its roof mass.
 a,b=Vector(a),Vector(b);t=(b-a).normalized();normal=Vector((t.y,-t.x));vv=[];ff=[];z=eave-.035
 for p in [a,b]:
  for r in [.086,.079]:
   for i in range(17):ang=math.pi+i*math.pi/16;v=p+normal*(r*math.cos(ang));vv.append((v.x,v.y,z+r*math.sin(ang)))
 for i in range(16):ff.extend([(i,i+1,i+35,i+34),(i+17,i+51,i+52,i+18)])
 for i in [0,16]:ff.append((i,i+34,i+51,i+17))
 for off in [0,34]:
  for i in range(16):ff.append((off+i,off+i+17,off+i+18,off+i+1))
 group('Roofs/'+name+'/Rainwater');mesh('Open half-round gutter',vv,ff,'Galvanized')
 for p in [a,b]:
  vs=[(p.x,p.y,z)]+[(p.x+normal.x*.079*math.cos(math.pi+i*math.pi/16),p.y+normal.y*.079*math.cos(math.pi+i*math.pi/16),z+.079*math.sin(math.pi+i*math.pi/16)) for i in range(17)]
  mesh('Gutter stop-end',vs,[tuple(range(18))],'Galvanized')
 for side in [-1,1]:
  x=a+normal*side*.083;y=b+normal*side*.083;tube('Rolled gutter edge',[(x.x,x.y,z),(y.x,y.y,z)],.0055,'Galvanized',sides=8)
 for k in range(math.ceil((b-a).length/1.8)+1):
  p=a.lerp(b,k/math.ceil((b-a).length/1.8));q=p-normal*wall_offset
  tube('Fascia gutter bracket',[(q.x,q.y,z-.09),(p.x,p.y,z-.12),(p.x,p.y,z-.01)],.012,'Steel',sides=8)
 for frac in ([.12,.88] if (b-a).length>7 else [.84]):
  p=a.lerp(b,frac);q=p-normal*wall_offset;end=q+normal*.17
  cylinder('Gutter outlet sleeve',(p.x,p.y,z-.08),.055,.13,'Galvanized',segments=16)
  tube('Outlet and supported downpipe',[(p.x,p.y,z-.07),(p.x,p.y,z-.35),(q.x,q.y,z-.57),(q.x,q.y,.32),(end.x,end.y,.16)],.049,'Galvanized',wall=.004,sides=12)
  for h in [.7,2.3,3.7]:
   if h>z-.65:continue
   beam('Downpipe wall fixing',(q.x,q.y,h),(q.x-normal.x*.12,q.y-normal.y*.12,h),.035,.035,'Steel')
  box('Drain splash shoe',(end.x,end.y,.075),(.44,.44,.15),'Concrete',.005)

drainage('Waiting',(-11.35,1),(-11.35,-9.1),4.79,.29)
drainage('Preparation',(11.35,-9.1),(11.35,.8),4.79,.29)
drainage('CommonBay',(-14.45,3.1),(-14.45,1),4.44,.39)
drainage('WestEvaluation',(-14.45,8.1),(-14.45,3.35),5.25,.39)
drainage('WestService',(-14.45,13.9),(-14.45,8.4),4.55,.39)
drainage('WestService',(-3.2,14.45),(-14.2,14.45),4.55,.39)
drainage('Audition',(14.45,1.1),(14.45,14.9),5.85,.39)
drainage('Audition',(14.2,15.45),(3.1,15.45),5.85,.39)
drainage('Interview',(2.8,12.45),(-2.8,12.45),5.12,.39)

# Rear service canopy, shallow landing, utility and louver details.
group('Structure/RearServiceSteps')
for y,z,w,d in [(14.25,.18,2.35,1.0),(14.90,.09,2.6,.4)]:box('Service entrance tread',(-7.60,y,z),(w,d,z*2),'SS_Stone',.02)
group('Shell/Rear/ServiceCanopy')
for x in [-8.56,-6.64]:
 box('Service canopy post',(x,14.85,1.73),(.12,.12,3.10),'SS_Walnut')
 beam('Canopy brace',(x,14.85,2.67),(x,14.35,3.2),.06,.06,'SS_Walnut')
box('Canopy rear wall ledger',(-7.6,13.94,3.2),(2.30,.12,.16),'SS_Walnut')
box('Canopy front bearer',(-7.6,14.85,3.12),(2.30,.13,.16),'SS_Walnut')
for i in range(5):beam('Canopy rafter',(-8.60+i*.50,13.92,3.28),(-8.60+i*.50,15.10,3.13),.07,.10,'SS_Walnut')
mesh('Lead service canopy',[(-8.8,13.84,3.37),(-6.4,13.84,3.37),(-6.4,15.18,3.20),(-8.8,15.18,3.20)],[(0,1,2,3)],'Galvanized')
# Service light is truly wall-supported and faces outwards.
before=set(G.GROUPS);lantern('Shell/Rear/Service/Light',0,0,2.75);transform_groups(set(G.GROUPS)-before,(-6.35,13.88),math.pi)
group('Shell/Rear/Service/Utilities')
box('Service disconnect box',(-4.30,14.04,1.55),(.36,.18,.55),'Steel')
tube('Service conduit',[(-4.30,14.02,.30),(-4.30,14.02,1.25)],.022,'Galvanized',sides=10)
tube('Service conduit to roof entry',[(-4.30,14.02,1.85),(-4.30,14.02,3.70),(-4.30,13.84,3.80)],.022,'Galvanized',sides=10)
for z in [.65,2.15,3.35]:box('Conduit masonry saddle',(-4.30,13.96,z),(.08,.13,.025),'Steel')
for x in [-11.0,-5.1]:
 box('Vent louver surround',(x,14.045,3.93),(.66,.12,.35),'SS_Stone')
 box('Recessed vent throat',(x,14.113,3.93),(.54,.015,.25),'Black',0)
 for i in range(5):box('Vent louver blade',(x,14.13,3.825+i*.05),(.53,.05,.018),'Steel',.002)

# Purposeful room furnishing; keep the approved pieces and avoid scatter.
print('A2 room furnishings and ceilings',flush=True)
table('Furnishings/Flexible/ReviewTable',-10.4,5.75,1.8,.9,.75)
for i,(x,y,angle) in enumerate([(-10.4,6.65,0),(-11.0,4.90,180),(-9.8,4.90,180)]):place('ChairBentwood_1930','Furnishings/Flexible/Chair'+str(i),(x,y,.39),angle)
place('DeskPaperwork_1930','Furnishings/Flexible/ReviewPapers',(-10.5,5.7,1.15))
place('FilingCabinet_4Drawer_1930','Furnishings/Flexible/Files',(-12.8,7.45,.38))
for i,y in enumerate([4.65,6.85]):
 place('DeskPedestal_1930','Furnishings/Staff/Desk'+str(i),(-5.05,y,.39),90)
 place('ChairBentwood_1930','Furnishings/Staff/Chair'+str(i),(-4.0,y,.39),-90)
place('TelephoneDesk_1930','Furnishings/Staff/Telephone',(-5.1,4.4,1.19),90)
place('DeskPaperwork_1930','Furnishings/Staff/RecordsInUse',(-5.1,6.8,1.19),90)
place('ShelfSteelTimber_170_1930','Furnishings/Storage/EquipmentShelf',(-4.0,11.5,.36),90)
place('ChairBentwood_1930','Furnishings/Audition/Evaluator2',(9.35,2.95,.39),180)
# Existing curtain wall plates are supported by shallow battens at the new back wall.
group('Shell/Rear/Audition/CurtainSupports')
for x in [5.65,7.0,8.5,10.0,11.35]:box('Curtain bracket backing batten',(x,14.81,3.48),(.10,.10,.18),'SS_Walnut')

ceiling_rooms=[('Reception',-3.5,3.5,-8.75,-2.5,5.25),('Waiting',-10.65,-3.5,-8.65,3.3,4.10),('Preparation',3.5,10.65,-8.65,1,4.10),('CommonBay',-13.65,-10.65,1.1,3.3,3.95),('Hall',-3,3,-2.5,5,5.25),('Interview',-2.85,2.85,5,11.65,4.42),('WestWork',-13.65,-3.15,3.3,8.1,4.62),('Service',-13.65,-3.15,8.1,13.65,4.02),('Audition',3.15,13.65,1.15,14.65,5.43)]
for n,a,b,c,d,z in ceiling_rooms:
 group('Ceilings/'+n);box('Finished plaster ceiling',((a+b)/2,(c+d)/2,z),(b-a,d-c,.08),'InteriorPlaster',0)
 for x in [a+.06,b-.06]:box('Ceiling perimeter timber',(x,(c+d)/2,z-.075),(.10,d-c,.10),'SS_Walnut')
 for y in [c+.06,d-.06]:box('Ceiling perimeter timber',((a+b)/2,y,z-.075),(b-a,.10,.10),'SS_Walnut')
# Suspensions continue to the higher ceilings; fixtures themselves remain approved.
for i,((x,y),ceiling_z) in enumerate(zip(pendants,[5.25,5.25,4.10,4.10,4.10,4.42,5.43,4.62])):
 group('Ceilings/Pendant%02d'%i);oldtop=4.045+(.75 if i==6 else 0)
 if ceiling_z-.04>oldtop:tube('Extended pendant ceiling support',[(x,y,oldtop),(x,y,ceiling_z-.04)],.012,'Steel',sides=10)
for i,(x,y) in enumerate([(-5,5.8),(-11.2,11.8),(-4.9,11.8),(-7.6,9.05)]):
 group('Ceilings/SupportPendant'+str(i));z=4.02 if i else 4.62
 cylinder('Pendant mounting rose',(x,y,z-.04),.11,.05,'Brass')
 cylinder('Pendant rod',(x,y,z-.31),.012,.52,'Steel')
 ball('Opal pendant shade',(x,y,z-.68),(.21,.21,.19),'SS_Lamp')

# Batch repetitive authored pieces without simplifying their geometry or merging
# semantic assemblies. Tile atlases remain separate objects with explicit UVs.
for name,c in G.GROUPS.items():
 candidates=[]
 if name.startswith('Interior/Floors/'):
  candidates=[o for o in c.objects if o.type=='MESH' and o.name.startswith('Oak floor board')]
 elif name.startswith('Roofs/') and name.count('/')==1:
  candidates=[o for o in c.objects if o.type=='MESH' and o.name.startswith(('Hip roll cap','Ridge roll','Ridge return'))]
 if len(candidates)>1:
  bpy.ops.object.select_all(action='DESELECT')
  for o in candidates:o.select_set(True)
  bpy.context.view_layer.objects.active=candidates[0];bpy.ops.object.join()
  bpy.context.view_layer.objects.active.name='Batched '+('oak courses' if name.startswith('Interior') else 'fired caps and joints')
print('A2 semantic packet export',flush=True)
bpy.context.view_layer.update();parts=[]
for name,c in G.GROUPS.items():
 if not any(o.type=='MESH' for o in c.objects):continue
 for o in c.objects:
  if o.type!='MESH':continue
  assert all(math.isfinite(v) for vertex in o.data.vertices for v in vertex.co),name
 bpy.context.view_layer.update();p=G.uv_packet(c);p['pivot']=pivots.get(name,[0,0,0]);parts.append(p)
used=sorted({m for p in parts for m in p['materials']})
data=dict(schema=1,candidate='StageSchool_A2',parts=parts,materials=[dict(name=n,linearRGB=G.PALETTE[n][0],metallic=G.PALETTE[n][1],roughness=G.PALETTE[n][2],texture=G.PALETTE[n][3],shared=n not in CUSTOM) for n in used],fixtures=fixtures,equipment=equipment)
out=ROOT/'ArtExports/StageSchoolA2';G.save_packet(out/'stage_school_meshes.json.gz',data)
report=dict(candidate='StageSchool_A2',status='MANUAL_ARCHITECTURE_REVIEW_PENDING',footprint=[28,24],masonry_area=sum((b-a)*(d-c) for n,a,b,c,d in volumes),floor=.36,occupied_floors=1,source_groups=len(parts),source_objects=sum(len(c.objects) for c in G.GROUPS.values()),triangles=sum(len(s['indices'])//3 for p in parts for s in p['submeshes']),vertices=sum(len(p['positions'])//3 for p in parts),unique_materials=len(used),fixtures=len(fixtures),equipment_instances=equipment,roofs=roofs,reused_assemblies=reuse,room_floor_extents=floor_rooms,parts=[dict(name=p['name'],triangles=sum(len(s['indices'])//3 for s in p['submeshes']),vertices=len(p['positions'])//3) for p in parts])
(HERE/'generation_report.json').write_text(json.dumps(report,indent=2))
# Recreate approved linked instances at their new placements; no duplicate meshes.
names={f['asset'] for f in fixtures}
with bpy.data.libraries.load(str(ROOT/'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'),link=True) as (src,dst):dst.collections=[n for n in names if n in src.collections]
kit={c.name:c for c in dst.collections if c}
for f in fixtures:
 o=bpy.data.objects.new('Shared '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=kit[f['asset']];G.group(f['group']).objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(180-f['yaw'])
with bpy.data.libraries.load(str(ROOT/'ArtSource/ProductionEquipment1930Candidate/ProductionEquipment1930Candidate.blend'),link=True) as (src,dst):dst.collections=['StudioCamera1930','StudioLamp1930']
kit={c.name:c for c in dst.collections if c}
for f in equipment:
 o=bpy.data.objects.new('Approved '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=kit[f['asset']];G.group(f['group']).objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(-f['yaw'])
for lib in bpy.data.libraries:
 if 'ProductionEquipment1930Candidate' in lib.filepath:lib.filepath='//../ProductionEquipment1930Candidate/ProductionEquipment1930Candidate.blend'
 elif 'PeriodEnvironment1930' in lib.filepath:lib.filepath='//../PeriodEnvironment1930/PeriodEnvironment1930.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'StageSchool_A2.blend'))
print('STAGE_SCHOOL_A2',json.dumps({k:v for k,v in report.items() if k not in ['parts','roofs','reused_assemblies','room_floor_extents']}))
