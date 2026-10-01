"""Independent, unbranded c.1930 equipment candidates. Metres, +Y optical axis.
Blender --background --factory-startup --python this_file
Semantic mesh packet and linked collection source; no Stage School dependencies.
"""
import bpy,math,sys,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path[:0]=[str(HERE),str(ROOT/'ArtSource/PeriodEnvironment1930')]
import geometry as G
from primitives import box,cylinder,beam,ball
from geometry import mesh,tube
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
bpy.context.preferences.filepaths.save_version=0
G.GROUPS.clear();G.MATS.clear();G.init_materials()
def group(n):G.group(n)
def ring(n,p,r,mat='Steel',axis='Y',wire=.006):
 x,y,z=p
 points=[(x+r*math.cos(i*math.pi/24),y,z+r*math.sin(i*math.pi/24)) if axis=='Y' else (x+r*math.cos(i*math.pi/24),y+r*math.sin(i*math.pi/24),z) for i in range(49)]
 return tube(n,points,wire,mat,sides=8)
def bolt(p,axis=(0,1,0),r=.011):cylinder('Slotted fastener',p,r,.007,'Galvanized',axis,segments=6)

# Camera: a robust hand-cranked mechanism, paired magazines and timber legs.
group('StudioCamera1930/Tripod')
cylinder('Tripod spreader centre hub',(0,0,.42),.054,.034,'Steel',segments=20)
for i in range(3):
 a=i*2*math.pi/3+.3;rad=Vector((math.cos(a),math.sin(a),0));tan=Vector((-math.sin(a),math.cos(a),0))
 foot=rad*.52+Vector((0,0,.045));top=rad*.105+Vector((0,0,1.20))
 for side in [-1,1]:beam('Paired ash upper leg',top+tan*side*.040,foot*.52+top*.48+tan*side*.034,.036,.048,'TimberDark')
 beam('Sliding lower tripod leg',foot,foot*.35+top*.65,.048,.049,'TimberDark')
 for t in [.42,.59]:
  p=foot.lerp(top,t);beam('Leg clamp band',p-tan*.067,p+tan*.067,.023,.022,'Steel');bolt(p+rad*.027,tuple(rad))
 beam('Ground shoe',foot,foot+Vector((0,0,-.038)),.065,.070,'Steel')
 beam('Tripod spreader',rad*.035+Vector((0,0,.42)),foot.lerp(top,.23),.020,.017,'Steel')
 cylinder('Leg hinge pin',tuple(top),.028,.14,'Steel',tuple(tan),segments=16)
group('StudioCamera1930/Head')
cylinder('Cast tripod crown',(0,0,1.22),.15,.085,'Steel',segments=32)
cylinder('Pan bearing',(0,0,1.285),.108,.055,'Galvanized',segments=32)
box('Geared pan head',(0,0,1.36),(.25,.25,.11),'Steel',.012)
for x in [-.13,.13]:
 box('Tilt cheek',(x,0,1.435),(.045,.23,.16),'Steel')
 cylinder('Tilt axle cap',(x*1.23,0,1.46),.044,.025,'Galvanized',(1,0,0),segments=24)
box('Camera saddle',(0,0,1.50),(.29,.41,.06),'Steel')
tube('Pan handle',[(.12,-.07,1.40),(.23,-.35,1.28),(.25,-.56,1.24)],.014,'Steel',sides=10)
beam('Pan handle grip',(.24,-.44,1.265),(.25,-.57,1.24),.035,.035,'TimberDark')
group('StudioCamera1930/Body')
box('Cast camera case',(0,0,1.745),(.37,.47,.43),'Steel',.024)
box('Removable mechanism cover',(.193,-.012,1.745),(.022,.385,.335),'Black',.009)
box('Rear loading cover',(0,-.243,1.75),(.305,.022,.345),'Steel',.014)
for x in [-.135,.135]:
 for z in [1.62,1.89]:bolt((x,-.258,z),(0,-1,0))
for y in [-.15,.15]:
 cylinder('Cover latch',(.212,y,1.745),.027,.014,'Galvanized',(1,0,0),segments=16)
box('Unbranded brass serial plate',(.208,-.045,1.67),(.008,.11,.032),'Brass',.002)
tube('Folding carry handle',[(-.12,-.10,1.96),(-.12,-.10,2.025),(.12,-.10,2.025),(.12,-.10,1.96)],.013,'Steel',sides=10)
# Magazine centres are separated enough to read the characteristic double silhouette.
group('StudioCamera1930/Magazines')
box('Light-tight film throat',(0,.055,1.965),(.25,.17,.14),'Steel')
for x in [-.185,.185]:
 cylinder('Film magazine shell',(x,.05,2.16),.202,.15,'Steel',(0,1,0),segments=48)
 cylinder('Raised magazine lid',(x,.131,2.16),.187,.018,'Black',(0,1,0),segments=48)
 ring('Magazine rolled seam',(x,.144,2.16),.185,'Galvanized',wire=.003)
 cylinder('Magazine locking hub',(x,.15,2.16),.037,.027,'Steel',(0,1,0),segments=24)
 box('Magazine latch wing',(x,.168,2.16),(.080,.012,.019),'Galvanized',.003)
group('StudioCamera1930/Lens')
cylinder('Lens turret',(0,.244,1.755),.16,.039,'Steel',(0,1,0),segments=48)
for x,z,r in [(0,1.79,.081),(-.099,1.68,.038),(.098,1.68,.044)]:
 cylinder('Machined lens barrel',(x,.323,z),r,.14,'Steel',(0,1,0),segments=32)
 for yy in [.277,.354,.39]:ring('Lens knurl band',(x,yy,z),r+.003,'Galvanized',wire=.004)
 cylinder('Recessed optical glass',(x,.395,z),r*.82,.006,'Glass',(0,1,0),segments=32)
 if x==0:
  for j in range(18):
   a=j*math.pi/9;bolt((x+(r+.001)*math.cos(a),.355,z+(r+.001)*math.sin(a)),r=.0025)
group('StudioCamera1930/Controls')
cylinder('Crank bearing',(.22,.02,1.79),.056,.052,'Steel',(1,0,0),segments=24)
tube('Hand crank',[(.25,.02,1.79),(.28,.02,1.79),(.28,-.045,1.65),(.36,-.045,1.65)],.012,'Galvanized',sides=10)
cylinder('Crank timber grip',(.378,-.045,1.65),.024,.095,'TimberDark',(1,0,0),segments=20)
cylinder('Footage indicator',(.214,-.11,1.855),.041,.014,'Brass',(1,0,0),segments=24)
cylinder('Footage enamel dial',(.224,-.11,1.855),.033,.005,'Paper',(1,0,0),segments=24)
beam('Footage pointer',(.229,-.11,1.855),(.229,-.092,1.874),.003,.003,'Black')
box('Sighting tube bracket',(-.235,-.08,1.82),(.10,.12,.046),'Steel')
cylinder('Side optical finder',(-.275,-.02,1.86),.035,.36,'Steel',(0,1,0),segments=24)
cylinder('Rubber eyecup',(-.275,-.219,1.86),.049,.042,'Black',(0,1,0),segments=24)
cylinder('Finder front glass',(-.275,.166,1.86),.029,.008,'Glass',(0,1,0),segments=24)

# Open-face incandescent lamp. No Fresnel or LED features; ordinary mechanical yoke.
group('StudioLamp1930/Stand')
for i in range(3):
 a=i*math.pi*2/3;rad=Vector((math.cos(a),math.sin(a),0));foot=rad*.43+Vector((0,0,.036))
 beam('Folding steel foot',tuple(rad*.03+Vector((0,0,.44))),tuple(foot),.035,.03,'Steel')
 beam('Foot brace',tuple(rad*.02+Vector((0,0,.22))),tuple((rad*.03+Vector((0,0,.44))).lerp(foot,.64)),.014,.016,'Galvanized')
 box('Rubber foot pad',tuple(foot),(.075,.07,.045),'Black')
for z,r,h in [(.68,.031,1.28),(1.53,.024,.82),(1.82,.040,.055),(.41,.052,.08),(1.10,.044,.065)]:cylinder('Stand tube or clamp',(0,0,z),r,h,'Steel',segments=24)
for z in [1.10,1.82]:
 cylinder('Tension screw',(.061,0,z),.012,.065,'Galvanized',(1,0,0),segments=12)
 beam('T-handle',(.092,-.032,z),(.092,.032,z),.012,.012,'Steel')
group('StudioLamp1930/Yoke')
box('Yoke crossbar',(0,0,1.83),(.56,.047,.042),'Steel')
for x in [-.267,.267]:
 box('Yoke upright',(x,0,1.967),(.035,.055,.30),'Steel')
 cylinder('Tilt pivot',(x,0,2.09),.042,.066,'Galvanized',(1,0,0),segments=24)
 cylinder('Tilt locking handwheel',(x*1.16,0,2.09),.052,.023,'Steel',(1,0,0),segments=20)
group('StudioLamp1930/Housing')
# Hollow rolled drum, with a real rim rather than a luminous solid end cap.
tube('Open drum',[(0,-.17,2.09),(0,.13,2.09)],.216,'Steel',wall=.012,sides=48)
cylinder('Vented rear shell',(0,-.181,2.09),.199,.018,'Steel',(0,1,0),segments=48)
for z in [-.105,-.055,0,.055,.105]:box('Rear cooling slot',(0,-.195,2.09+z),(.235,.005,.013),'Black',.003)
for yy in [-.145,.09,.145]:ring('Rolled drum stiffener',(0,yy,2.09),.218,'Steel',wire=.012)
for x in [-.135,.135]:
 bolt((x,.157,2.24));bolt((x,.157,1.94))
tube('Insulated carrying loop',[(-.075,-.08,2.30),(-.075,-.08,2.36),(.075,-.08,2.36),(.075,-.08,2.30)],.013,'Black',sides=10)
group('StudioLamp1930/Reflector')
vv=[];ff=[]
for j in range(9):
 r=.025+j*.0215;yy=-.12+.245*(r/.197)**2
 for i in range(48):a=i*math.pi/24;vv.append((r*math.cos(a),yy,2.09+r*math.sin(a)))
for j in range(8):
 for i in range(48):a=j*48+i;b=j*48+(i+1)%48;ff.append((a,b,b+48,a+48))
o=mesh('Spun metal reflector',vv,ff,'Galvanized')
for p in o.data.polygons:p.use_smooth=True
cylinder('Porcelain bulb socket',(0,-.085,2.09),.036,.07,'Paper',(0,1,0),segments=20)
ball('Incandescent envelope',(0,.005,2.09),(.046,.072,.046),'Glass')
tube('Visible tungsten support',[(-.014,.01,2.06),(-.014,.02,2.12),(.014,.02,2.12),(.014,.01,2.06)],.0025,'Brass',sides=6)
for z in [-.15,0,.15]:tube('Protective wire guard',[(-math.sqrt(.193**2-z*z),.164,2.09+z),(math.sqrt(.193**2-z*z),.164,2.09+z)],.003,'Steel',sides=6)
group('StudioLamp1930/Shutters')
for side in [-1,1]:
 # Four thin folded sheet shutters open out from their hinges.
 for horizontal in [False,True]:
  coords=[(side*.175,.165,-.175),(side*.175,.165,.175),(side*.35,.38,.21),(side*.35,.38,-.21)]
  vs=[(z if horizontal else x,y,2.09+(x if horizontal else z)) for x,y,z in coords]
  o=mesh('Hinged sheet shutter',vs,[(0,1,2,3)],'Black')
  mod=o.modifiers.new('Folded sheet gauge','SOLIDIFY');mod.thickness=.002
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  a,b=vs[:2];tube('Shutter hinge',[a,b],.008,'Steel',sides=8)
group('StudioLamp1930/Cable')
tube('Rubber insulated supply lead',[(.08,-.2,2.0),(.095,-.24,1.84),(.075,-.16,1.63),(.045,-.04,1.30),(.045,-.04,.58),(.10,-.14,.08),(.25,-.24,.025),(.34,-.32,.025),(.24,-.39,.025),(.12,-.36,.025)],.009,'Black',sides=8)
box('Bakelite supply connector',(.12,-.36,.025),(.065,.035,.037),'Black',.005)

# Match an operator-height optical axis while retaining the tripod footprint.
for name,c in G.GROUPS.items():
 if name.startswith('StudioCamera1930/'):
  for o in c.objects:
   for v in o.data.vertices:v.co.z-=.25*min(1,max(0,v.co.z/1.2))
bpy.context.view_layer.update();assets=[]
for asset in ['StudioCamera1930','StudioLamp1930']:
 parts=[];root=bpy.data.collections.new(asset);bpy.context.scene.collection.children.link(root)
 for name,c in list(G.GROUPS.items()):
  if not name.startswith(asset+'/'):continue
  bpy.context.scene.collection.children.unlink(c);root.children.link(c)
  p=G.uv_packet(c);p['name']=name.split('/',1)[1];p['pivot']=[0,0,0];parts.append(p)
 assert all(math.isfinite(v) for p in parts for v in p['positions'])
 assets.append(dict(name=asset,parts=parts))
out=ROOT/'ArtExports/ProductionEquipment1930Candidate';out.mkdir(parents=True,exist_ok=True)
G.save_packet(out/'equipment_meshes.json.gz',dict(schema=1,assets=assets))
report={a['name']:dict(triangles=sum(len(s['indices'])//3 for p in a['parts'] for s in p['submeshes']),vertices=sum(len(p['positions'])//3 for p in a['parts']),renderers=len(a['parts']),materials=sorted({m for p in a['parts'] for m in p['materials']})) for a in assets}
(HERE/'generation_report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'ProductionEquipment1930Candidate.blend'))
print(json.dumps(report))
