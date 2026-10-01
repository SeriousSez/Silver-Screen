"""Independent period kit. No Studio Services placement is baked into a canonical mesh."""
import bpy,sys,pathlib,math,json,hashlib
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import geometry as G
from geometry import box,beam,cylinder,tube,ball,mesh,text,asset

def fastener(p,r=.011,axis=(0,-1,0),mat='Iron'):cylinder('Fastener',p,r,.009,mat,axis,segments=10)
def handle(x,y,z,width=.14):
 tube('Pull',[(x-width/2,y,z),(x-width/2,y-.045,z),(x+width/2,y-.045,z),(x+width/2,y,z)],.008,'Brass')
 for dx in [-width/2,width/2]:box('Pull socket',(x+dx,y,z),(.026,.012,.034),'Brass',.005)
def boards(p,s,count,axis=0,mat='Timber',gap=.003):
 for i in range(count):
  q=list(p);size=list(s);size[axis]=s[axis]/count-gap;q[axis]+=(-.5+(i+.5)/count)*s[axis]
  box('Board',q,size,mat,.004)
def legs(w,d,h,mat='Timber',size=.065):
 for x in [-w/2,w/2]:
  for y in [-d/2,d/2]:box('Leg',(x,y,h/2),(size,size,h),mat,.009)
def shelf(w=1.7,d=.52,h=2.05):
 for x in [-w/2,w/2]:
  for y in [-d/2,d/2]:box('Shelf upright',(x,y,h/2),(.055,.055,h),'Steel',.005)
 for z in [.12,.58,1.04,1.5,1.96]:
  boards((0,0,z),(w,d,.04),4,axis=1,mat='TimberLight')
  for y in [-d/2,d/2]:box('Shelf angle',(0,y,z-.032),(w,.03,.07),'Steel',.003)
 beam('Shelf cross brace',(-w/2,d/2,.1),(w/2,d/2,1.96),.028,.025,'Steel')
def crate(w=.75,d=.55,h=.58):
 for y in [-d/2,d/2]:boards((0,y,h/2),(w,.035,h),5)
 for x in [-w/2,w/2]:boards((x,0,h/2),(.035,d,h),4,axis=1)
 for z in [.025,h-.015]:boards((0,0,z),(w,d,.03),4,axis=1)
 for x in [-w*.36,w*.36]:
  for y in [-d/2-.024,d/2+.024]:
   box('Crate batten',(x,y,h/2),(.07,.045,h),'TimberDark',.004)
   for z in [.09,h-.08]:fastener((x,y-.025,z),.007)
def sack(p=(0,0,0),size=(.23,.18,.34)):
 v=[]
 for k,(z,s) in enumerate([(0,.72),(.08,.98),(.30,1),(.49,.90),(.61,.62),(.65,.20)]):
  for i in range(24):
   a=i*math.tau/24;r=s*(1+.035*math.cos(5*a+k));v.append((p[0]+size[0]*r*math.cos(a),p[1]+size[1]*r*math.sin(a),p[2]+z))
 f=[tuple(range(23,-1,-1))]
 for k in range(5):
  for i in range(24):f.append((k*24+i,k*24+(i+1)%24,(k+1)*24+(i+1)%24,(k+1)*24+i))
 o=mesh('Filled canvas sack',v,f,'Canvas')
 for poly in o.data.polygons:poly.use_smooth=True
 cylinder('Sack gathered top',(p[0],p[1],p[2]+size[2]*1.96),.05,.08,'Canvas')
 tube('Sack seam',[(p[0]-.02,p[1]-size[1],p[2]+size[2]*.25),(p[0]-.03,p[1]-size[1]*1.005,p[2]+size[2]),(p[0]-.01,p[1]-size[1]*.65,p[2]+size[2]*1.8)],.004,'TimberLight',sides=8)
def window(w,h):
 for x in [-w/2,w/2]:
  box('Steel jamb',(x,0,h/2),(.075,.13,h+.075),'Steel',.007)
  box('Interior rebate',(x,.045,h/2),(.10,.045,h+.12),'Iron',.005)
 for z in [0,h]:box('Steel head sill',(0,0,z),(w,.13,.075),'Steel',.007)
 cols=max(3,round(w/.45));rows=max(2,round(h/.49))
 for i in range(1,cols):box('T mullion',(-w/2+i*w/cols,-.025,h/2),(.035,.075,h),'Steel',.004)
 for j in range(1,rows):box('Transom',(0,-.025,j*h/rows),(w,.075,.035),'Steel',.004)
 for i in range(cols):
  for j in range(rows):box('Individual glass pane',(-w/2+(i+.5)*w/cols,.018,(j+.5)*h/rows),(w/cols-.036,.009,h/rows-.036),'Glass',0)
 box('Interior sill',(0,.10,-.058),(w+.16,.30,.07),'LimestoneTrim')
 box('Exterior sloping sill',(0,-.11,-.047),(w+.20,.30,.085),'LimestoneTrim')
 handle(.25,-.08,h*.66,.12)
def door(w,h,glass=True):
 # 55 mm leaf; layered shallow rails form a plausible 65 mm maximum profile.
 for x in [-w/2+.065,w/2-.065]:box('Door stile',(x,0,h/2),(.13,.055,h),'WorkshopPaint',.01)
 for z in [.10,h*.42,h-.065]:box('Door rail',(0,0,z),(w-.15,.058,.14),'WorkshopPaint',.007)
 boards((0,.008,h*.235),(w-.23,.032,h*.32),4,0,'WorkshopPaint')
 if glass:
  gh=h*.48;gz=h*.70;box('Door glass',(0,.009,gz),(w-.24,.008,gh),'Glass',0)
  for x in [-w*.16,w*.16]:box('Door glazing bar',(x,-.008,gz),(.027,.04,gh),'WorkshopPaint',.003)
  box('Door glass crossbar',(0,-.008,gz),(w-.24,.04,.027),'WorkshopPaint',.003)
 else:boards((0,.006,h*.715),(w-.23,.032,h*.43),4,0,'WorkshopPaint')
 for z in [.25,h*.52,h-.22]:
  cylinder('Hinge knuckle',(-w/2-.012,.005,z),.018,.095,'Brass')
  box('Hinge leaf',(-w/2+.04,-.037,z),(.095,.012,.09),'Brass',.004)
  for dz in [-.025,.025]:fastener((-w/2+.055,-.046,z+dz),.006,mat='Brass')
 for y in [-.039,.039]:
  box('Lock escutcheon',(w/2-.11,y,1.05),(.075,.012,.22),'Brass',.012)
  handle(w/2-.17,y-.006,1.10,.10)
 cylinder('Keyhole',(w/2-.11,-.049,.99),.008,.006,'Black',(0,1,0),segments=12)
 box('Kick plate',(0,-.036,.12),(w-.12,.009,.20),'Brass',.005)
 G.COLLIDERS[G.CURRENT].append(dict(name='Leaf',position=[0,0,h/2],size=[w,.065,h]))
def barrel(wood=True):
 h=.90;prof=[(0,.255),(.1,.285),(.45,.32),(.80,.285),(.9,.255)] if wood else [(0,.295),(.06,.305),(.84,.305),(.90,.295)]
 segments=16 if wood else 48
 for i in range(segments):
  a=i*math.tau/segments+(.003 if wood else 0);b=(i+1)*math.tau/segments-(.003 if wood else 0);v=[]
  for z,rad in prof:v.extend([(rad*math.cos(a),rad*math.sin(a),z),(rad*math.cos(b),rad*math.sin(b),z)])
  o=mesh('Barrel stave',v,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(len(prof)-1)],'Timber' if wood else 'WorkshopPaint')
  if not wood:
   for poly in o.data.polygons:poly.use_smooth=True
 def radius(z):
  for (a,ra),(b,rb) in zip(prof,prof[1:]):
   if a<=z<=b:return ra+(rb-ra)*(z-a)/(b-a)
  return prof[-1][1]
 for z in ([.08,.26,.64,.82] if wood else [.045,.30,.61,.855]):
  rad=max(radius(z-.023),radius(z+.023))+.007
  for i in range(32):
   a=i*math.tau/32;b=(i+1)*math.tau/32
   mesh('Barrel hoop',[(rad*math.cos(a),rad*math.sin(a),z-.023),(rad*math.cos(b),rad*math.sin(b),z-.023),(rad*math.cos(b),rad*math.sin(b),z+.023),(rad*math.cos(a),rad*math.sin(a),z+.023)],[(0,1,2,3)],'Steel')
 cylinder('Barrel lid',(0,0,.885),.253 if wood else .296,.03,'TimberLight' if wood else 'RoofMetal',segments=48)
 cylinder('Barrel bung',(.11,0,.907),.029,.02,'TimberDark')
def build():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);G.GROUPS.clear();G.init_materials()
 for n,w,h in [('195x212',1.95,2.12),('200x210',2,2.1),('190x180',1.9,1.8),('200x180',2,1.8),('310x195',3.1,1.95),('265x205',2.65,2.05)]:
  asset('WindowSteel_'+n+'_1930','Windows','Wall',.3);window(w,h)
 asset('DoorGlazedTimber_110x258_1930','Doors','DoorLeaf',1.2);door(1.1,2.58)
 asset('DoorUtilityTimber_150x260_1930','Doors','DoorLeaf',1.6);door(1.5,2.6,False)
 asset('DoorFrameTimber_120x262_1930','Doors','Wall',1.2)
 for x in [-.63,.63]:
  box('Frame jamb',(x,0,1.31),(.09,.30,2.62),'TimberDark')
  for y in [-.16,.16]:box('Architrave',(x,y,1.31),(.15,.045,2.67),'WorkshopPaint')
 for y in [-.16,.16]:box('Architrave head',(0,y,2.65),(1.41,.045,.15),'WorkshopPaint')
 box('Frame head',(0,0,2.65),(1.35,.30,.09),'TimberDark')
 asset('ServiceDoorPair_485x346_1930','Doors','Wall',2.5)
 for side in [-1,1]:
  x=side*1.215
  boards((x,0,1.13),(2.38,.06,2.24),11,0,'WorkshopPaint')
  for dx in [-1.14,1.14]:box('Door stile',(x+dx,-.025,1.73),(.105,.09,3.4),'WorkshopPaint')
  for z in [.13,2.29,3.39]:box('Door rail',(x,-.025,z),(2.37,.09,.14),'WorkshopPaint')
  for i in range(4):
   for j in range(2):box('Service door pane',(x-.99+(i+.5)*.495,.01,2.35+(j+.5)*.49),(.46,.009,.455),'Glass',0)
  for dx in [-.99,-.495,0,.495,.99]:box('Glazing bar',(x+dx,-.018,2.84),(.035,.06,1.02),'WorkshopPaint',.003)
  box('Glazing transom',(x,-.018,2.84),(2.0,.06,.035),'WorkshopPaint',.003)
  beam('Door diagonal',(x-side*1.04,-.065,.25),(x+side*1.04,-.065,2.17),.085,.06,'Timber')
  for z in [.45,1.65,2.94]:
   cylinder('Pintle',(x+side*1.19,0,z),.025,.19,'Steel')
   box('Strap hinge',(x+side*.87,-.082,z),(.61,.024,.065),'Steel')
   for dx in [.61,.84,1.04]:fastener((x+side*dx,-.097,z),.016)
  handle(side*.14,-.087,1.14,.12)
  for dx in [-1.12,1.12]:
   for z in [.2,2.29,3.37]:fastener((x+dx,-.078,z))
  G.COLLIDERS[G.CURRENT].append(dict(name='Service leaf',position=[x,0,1.73],size=[2.40,.10,3.46]))
 asset('DeskPedestal_1930','Office')
 box('Desk top',(0,0,.765),(2.1,.85,.07),'Timber',.018)
 for x in [-.77,.77]:
  box('Pedestal',(x,.015,.37),(.43,.71,.70),'TimberDark',.018)
  for z in [.16,.35,.54]:box('Drawer front',(x,-.36,z),(.405,.045,.17),'Timber',.008);handle(x,-.39,z)
  box('Desk foot',(x,.015,.05),(.46,.74,.09),'Timber')
 box('Centre drawer',(0,-.31,.66),(1.02,.12,.13),'Timber');handle(0,-.381,.66)
 box('Modesty panel',(0,.31,.37),(1.22,.045,.45),'TimberDark')
 G.COLLIDERS[G.CURRENT].append(dict(name='Desk',position=[0,0,.4],size=[2.1,.85,.8]))
 asset('ChairBentwood_1930','Office')
 legs(.43,.40,.46,'TimberDark',.045);box('Seat',(0,0,.47),(.51,.48,.055),'Timber',.04)
 for x in [-.215,.215]:beam('Chair back upright',(x,.2,.4),(x,.25,.98),.045,.045,'TimberDark')
 for z in [.73,.94]:box('Curved back rail',(0,.25,z),(.47,.04,.075),'Timber')
 for x in [-.1,0,.1]:box('Back spindle',(x,.25,.83),(.018,.026,.25),'Timber')
 G.COLLIDERS[G.CURRENT].append(dict(name='Chair',position=[0,0,.5],size=[.55,.58,1]))
 asset('BenchSlatted_210_1930','Office')
 for x in [-.83,.83]:
  for y in [-.18,.18]:box('Cast bench leg',(x,y,.23),(.055,.07,.46),'Steel');box('Foot',(x,y,.035),(.13,.13,.04),'Steel')
  beam('Back support',(x,.18,.3),(x,.3,1),.06,.06,'Steel')
 boards((0,0,.48),(2.1,.50,.055),4,1,'Timber');boards((0,.27,.78),(2.1,.05,.47),4,2,'Timber')
 for x in [-.83,.83]:
  for z in [.61,.75,.88]:fastener((x,.235,z))
 G.COLLIDERS[G.CURRENT].append(dict(name='Bench',position=[0,.04,.5],size=[2.1,.62,1]))
 asset('FilingCabinet_4Drawer_1930','Office')
 box('Cabinet shell',(0,0,.70),(.65,.6,1.4),'WorkshopPaint',.018)
 for z in [.20,.52,.84,1.16]:
  box('Drawer',(0,-.315,z),(.59,.045,.29),'PaintWear',.012);handle(0,-.346,z-.028,.16)
  box('Label holder',(0,-.345,z+.065),(.19,.018,.05),'Brass',.003);box('Paper label',(0,-.357,z+.065),(.15,.006,.028),'Paper',0)
 for x in [-.26,.26]:box('Cabinet foot',(x,.01,.032),(.07,.5,.064),'Steel')
 G.COLLIDERS[G.CURRENT].append(dict(name='Cabinet',position=[0,0,.7],size=[.65,.64,1.4]))
 asset('ShelfSteelTimber_170_1930','Storage');shelf()
 G.COLLIDERS[G.CURRENT].append(dict(name='Rack',position=[0,0,1.025],size=[1.76,.58,2.05]))
 asset('CupboardUtility_1930','Storage')
 box('Cupboard carcass',(0,0,.94),(1.22,.58,1.88),'TimberDark')
 for x in [-.3,.3]:
  box('Panel door',(x,-.315,.96),(.58,.055,1.76),'WorkshopPaint');box('Recessed panel',(x,-.35,1.01),(.42,.018,1.35),'PaintWear');handle(x+(.2 if x<0 else -.2),-.38,.97,.055)
 box('Cupboard cornice',(0,0,1.91),(1.3,.65,.075),'Timber')
 G.COLLIDERS[G.CURRENT].append(dict(name='Cupboard',position=[0,0,.97],size=[1.3,.67,1.94]))
 asset('NoticeBoardTimber_173_1930','Displays','Wall',.8)
 box('Board backing',(0,.018,.575),(1.73,.055,1.15),'TimberDark')
 box('Cork insert',(0,-.02,.55),(1.53,.022,.87),'Canvas')
 for x in [-.815,.815]:box('Notice frame',(x,-.028,.575),(.10,.08,1.15),'Timber')
 for z in [.05,1.10]:box('Notice rail',(0,-.028,z),(1.73,.08,.10),'Timber')
 # Blank frame asset; notices/title are independent content instances on the building.
 asset('TelephoneDesk_1930','Office',clearance=.25)
 box('Bakelite base',(0,0,.05),(.23,.23,.09),'Black',.035);ball('Phone body',(0,.01,.12),(.10,.10,.075),'Black')
 cylinder('Rotary dial',(0,-.035,.165),.065,.014,'Brass')
 for i in range(10):
  a=math.radians(40+i*27);cylinder('Dial finger hole',(.047*math.cos(a),-.035+.047*math.sin(a),.177),.009,.005,'Black',segments=12)
 tube('Handset grip',[(-.14,.05,.22),(-.10,.05,.255),(.10,.05,.255),(.14,.05,.22)],.023,'Black')
 for x in [-.15,.15]:ball('Receiver cup',(x,.05,.205),(.049,.049,.033),'Black')
 tube('Phone cord',[(.14,.07,.21),(.19,.12,.09),(.19,.22,.016),(.09,.25,.016),(-.1,.22,.016)],.006,'Black',sides=10)
 asset('DeskPaperwork_1930','Office',clearance=.1)
 for z in range(8):box('Paper sheet',(.01*math.sin(z),0,.002+z*.0014),(.26,.34,.001),'PaperLight',0)
 box('Ledger cover',(.23,.03,.022),(.18,.27,.04),'Leather');box('Ledger pages',(.23,.025,.023),(.17,.25,.03),'Paper')
 cylinder('Ink bottle',(-.21,.09,.035),.022,.06,'Glass');cylinder('Ink cap',(-.21,.09,.07),.024,.015,'Black')
 beam('Pen',(-.21,.10,.08),(-.15,.11,.20),.006,.006,'TimberDark')
 asset('DeskLampBanker_1930','Office',clearance=.2)
 ball('Weighted lamp foot',(0,0,.025),(.12,.09,.035),'Brass');cylinder('Lamp upright',(0,0,.20),.013,.34,'Brass')
 for x in [-.12,.12]:tube('Shade yoke',[(0,0,.27),(x,0,.33),(x,-.01,.37)],.009,'Brass')
 v=[]
 for x in [-.16,.16]:
  for i in range(17):a=i*math.pi/16;v.append((x,-.04+.085*math.cos(a),.365+.085*math.sin(a)))
 o=mesh('Green glass shade',v,[(i,i+1,18+i,17+i) for i in range(16)],'WorkshopPaint');m=o.modifiers.new('Shade thickness','SOLIDIFY');m.thickness=.006;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 cylinder('Lamp bulb',(0,-.04,.38),.023,.16,'PaperLight',(1,0,0));tube('Pull chain',[(.13,-.04,.37),(.13,-.04,.26)],.002,'Brass',sides=8)
 asset('ToolTrayCarpenter_1930','Workshop',clearance=.2)
 box('Tray base',(0,0,.018),(.70,.30,.036),'TimberDark')
 for y in [-.145,.145]:box('Tray side',(0,y,.08),(.70,.025,.14),'Timber')
 for x in [-.337,.337]:box('Tray end',(x,0,.13),(.026,.29,.24),'Timber')
 cylinder('Tray carrying handle',(0,0,.23),.019,.70,'TimberLight',(1,0,0))
 beam('Hammer shaft',(-.25,-.07,.05),(.21,-.07,.05),.025,.025,'TimberLight');box('Hammer head',(.21,-.07,.075),(.075,.14,.09),'Iron')
 box('Plane body',(-.04,.07,.075),(.23,.10,.075),'Iron');box('Plane grip',(-.08,.07,.135),(.075,.06,.10),'TimberDark',.025)
 asset('WorkbenchJoiner_225_1930','Workshop')
 legs(1.9,.64,.9,'TimberDark',.11);boards((0,0,.90),(2.25,.8,.10),4,1,'TimberLight')
 for y in [-.31,.31]:box('Bench apron',(0,y,.73),(2.05,.055,.22),'Timber')
 boards((0,0,.22),(1.92,.65,.055),4,1,'Timber')
 box('Vise fixed jaw',(.78,-.48,.91),(.34,.12,.20),'Iron');box('Vise moving jaw',(.78,-.61,.91),(.34,.065,.20),'Iron')
 cylinder('Vise screw',(.78,-.60,.85),.024,.34,'Steel',(0,1,0));cylinder('Vise hub',(.78,-.79,.85),.042,.05,'Iron',(0,1,0));beam('Vise handle',(.78,-.81,.65),(.78,-.81,1.01),.018,.018,'Steel')
 for x in [-.75,-.45,-.15,.15,.45]:cylinder('Bench dog hole',(x,-.27,.952),.014,.004,'TimberDark')
 G.COLLIDERS[G.CURRENT].append(dict(name='Workbench',position=[0,-.035,.475],size=[2.25,.91,.95]))
 asset('ToolRackCarpenter_1930','Workshop','Wall',.6)
 boards((0,.015,.63),(1.5,.045,1.25),7,0,'TimberLight')
 for z in [.21,.97]:box('Tool hanging rail',(0,-.035,z),(1.5,.05,.07),'TimberDark')
 for i in range(6):
  x=-.61+i*.235;z=.70+(.06 if i%2 else 0)
  beam('Tool handle',(x,-.11,z-.22),(x,-.11,z+.18),.031,.036,'Timber')
  box('Tool head',(x,-.11,z+.17),(.15 if i%2 else .07,.06,.08 if i%2 else .18),'Iron')
  fastener((x,-.07,z+.22),.015)
 # Broad hand saw and square distinguish the rack from an arbitrary rod array.
 mesh('Hand saw blade',[(-.64,-.105,.44),(-.64,-.105,.26),(-.13,-.105,.16),(-.13,-.105,.39)],[(0,1,2,3)],'Galvanized')
 box('Saw handle',(-.69,-.105,.35),(.13,.06,.23),'Timber',.027)
 asset('LadderTimber_260_1930','Workshop',clearance=.7)
 for x in [-.24,.24]:box('Ladder stile',(x,0,1.30),(.066,.085,2.6),'Timber')
 for i in range(9):cylinder('Ladder rung',(0,0,.16+i*.28),.018,.47,'TimberLight',(1,0,0))
 G.COLLIDERS[G.CURRENT].append(dict(name='Ladder',position=[0,0,1.3],size=[.55,.10,2.6]))
 asset('WheelbarrowSteel_1930','Workshop',clearance=1)
 # Hollow open tub with a smaller bottom, folded rim and single iron-rimmed wheel.
 v=[(-.24,-.33,.51),(.24,-.33,.51),(.24,.32,.51),(-.24,.32,.51),(-.35,-.43,.80),(.35,-.43,.80),(.35,.41,.80),(-.35,.41,.80)]
 o=mesh('Open barrow tub',v,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'PaintWear');m=o.modifiers.new('Sheet thickness','SOLIDIFY');m.thickness=.012;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 tube('Tub rolled rim',[v[4],v[5],v[6],v[7],v[4]],.017,'Steel')
 for x in [-.22,.22]:
  beam('Barrow runner',(x,-.59,.22),(x,.98,.76),.052,.052,'Timber');beam('Barrow prop',(x,.17,.04),(x,.12,.54),.036,.036,'Steel')
  cylinder('Hand grip',(x,.92,.735),.035,.20,'TimberDark',(0,1,.35))
 cylinder('Wheel rim',(0,-.57,.23),.23,.075,'Iron',(1,0,0),segments=32);cylinder('Wheel face',(0,-.57,.23),.19,.079,'Timber',(1,0,0),segments=32)
 cylinder('Axle',(0,-.57,.23),.038,.64,'Steel',(1,0,0));G.COLLIDERS[G.CURRENT].append(dict(name='Barrow',position=[0,.13,.41],size=[.73,1.72,.82]))
 asset('CrateTimber_075_1930','Yard');crate()
 G.COLLIDERS[G.CURRENT].append(dict(name='Crate',position=[0,0,.29],size=[.80,.65,.58]))
 asset('CrateTimber_045_1930','Storage');crate(.45,.35,.34)
 G.COLLIDERS[G.CURRENT].append(dict(name='Crate',position=[0,0,.17],size=[.5,.44,.34]))
 asset('ArchiveBox_1930','Office')
 box('Archive box',(0,0,.125),(.32,.28,.25),'Canvas');box('Lid',(0,0,.257),(.334,.294,.026),'TimberLight')
 box('Archive label',(0,-.145,.15),(.15,.007,.063),'PaperLight',.002);handle(0,-.153,.08,.08)
 asset('PaintCan_1930','Workshop')
 cylinder('Paint tin',(0,0,.13),.105,.26,'WorkshopPaint',segments=24)
 for z in [.015,.25]:cylinder('Tin rim',(0,0,z),.111,.015,'Galvanized',segments=24)
 cylinder('Tin lid',(0,0,.264),.106,.018,'Galvanized',segments=24)
 tube('Wire bail',[(-.105,0,.18),(-.12,0,.33),(0,0,.40),(.12,0,.33),(.105,0,.18)],.004,'Iron',sides=8)
 asset('GroundsToolRack_1930','Workshop','Wall',.7)
 for z in [.20,1.37]:box('Grounds rail',(0,0,z),(1.35,.07,.09),'TimberDark')
 for x in [-.48,0,.48]:
  beam('Ash tool handle',(x,-.10,.22),(x,-.10,1.77),.027,.027,'TimberLight');fastener((x,-.08,1.37),.015)
 mesh('Spade blade',[(-.60,-.105,.38),(-.36,-.105,.38),(-.38,-.13,.13),(-.48,-.14,.045),(-.58,-.13,.13)],[(0,1,2,3,4)],'Iron')
 box('Broom head',(.48,-.12,.17),(.39,.11,.15),'Canvas',.015)
 box('Rake head',(0,-.13,.23),(.39,.05,.05),'Iron')
 for i in range(9):beam('Rake tine',(-.17+i*.043,-.13,.24),(-.17+i*.043,-.20,.11),.012,.012,'Iron')
 asset('BarrelTimber_1930','Yard');barrel();G.COLLIDERS[G.CURRENT].append(dict(name='Barrel',position=[0,0,.45],size=[.64,.64,.90]))
 asset('BarrelPaintedSteel_1930','Yard');barrel(False);G.COLLIDERS[G.CURRENT].append(dict(name='Drum',position=[0,0,.45],size=[.64,.64,.90]))
 asset('SackCanvas_1930','Materials');sack();G.COLLIDERS[G.CURRENT].append(dict(name='Sack',position=[0,0,.35],size=[.46,.36,.70]))
 asset('LumberStack_240_1930','Materials')
 for x in [-.78,.78]:box('Dunnage',(x,0,.06),(.10,.82,.12),'TimberDark')
 for layer in range(5):
  for j in range(4):box('Stacked plank',(.025*(layer%2),-.31+j*.205,.145+layer*.062),(2.4-.025*(j%2),.18,.05),'TimberLight' if j%2 else 'Timber')
 G.COLLIDERS[G.CURRENT].append(dict(name='Lumber',position=[0,0,.22],size=[2.45,.86,.44]))
 asset('MaterialRackTimber_400_1930','Materials')
 for x in [-1.8,0,1.8]:
  for y in [-.40,.40]:box('Rack upright',(x,y,1.0),(.12,.12,2),'TimberDark')
  for z in [.2,.88,1.57]:box('Rack bearer',(x,0,z),(.14,1.0,.12),'Timber')
 for z in [.2,.88,1.57]:
  for y in [-.40,.40]:box('Rack tie',(0,y,z),(4,.075,.10),'Timber')
 beam('Rack brace',(-1.8,.43,.1),(1.8,.43,1.9),.06,.06,'Timber')
 G.COLLIDERS[G.CURRENT].append(dict(name='Rack',position=[0,0,1],size=[4.1,1.05,2]))
 asset('ScaffoldBayTimber_200_1930','Construction')
 for x in [-.95,.95]:
  for y in [-.37,.37]:box('Scaffold post',(x,y,1.15),(.09,.09,2.3),'Timber')
 for z in [.25,1.7]:
  for y in [-.37,.37]:beam('Scaffold ledger',(-.97,y,z),(.97,y,z),.085,.07,'Timber')
 for y in [-.37,.37]:beam('Scaffold brace',(-.95,y,.1),(.95,y,1.7),.065,.06,'TimberLight')
 boards((0,0,1.73),(2.12,.84,.06),4,1,'TimberLight');G.COLLIDERS[G.CURRENT].append(dict(name='Scaffold',position=[0,0,1.15],size=[2.15,.88,2.3]))
 for length in [2,1.75,1.35,.65]:
  asset('FenceWire_%03d_1930'%round(length*100),'Fences','Ground',.3)
  for x in [-length/2,length/2]:cylinder('Fence post',(x,0,1),.037,2,'Steel')
  for z in [.16,1.85]:tube('Fence rail',[(-length/2,0,z),(length/2,0,z)],.025,'Steel')
  # Fine diagonal mesh inside the frame; ends clipped at top, bottom and posts.
  for slope in [-1,1]:
   for start in range(-30,31):
    intercept=start*.14;pts=[]
    for x in [-length/2,length/2]:
     z=slope*x+intercept
     if .16<=z<=1.85:pts.append((x,0,z))
    for z in [.16,1.85]:
     x=(z-intercept)/slope
     if -length/2<=x<=length/2:pts.append((x,0,z))
    if len(pts)==2:tube('Woven fence wire',pts,.0028,'Galvanized',sides=5)
  G.COLLIDERS[G.CURRENT].append(dict(name='Fence',position=[0,0,1],size=[length,.08,2]))
 asset('FenceGateWire_180_1930','Fences','Ground',1.9)
 for x in [-.88,.88]:box('Gate stile',(x,0,.92),(.048,.048,1.84),'Steel',.005)
 for z in [.04,1.81]:box('Gate rail',(0,0,z),(1.8,.048,.048),'Steel',.005)
 for i in range(17):tube('Gate vertical wire',[(-.8+i*.1,0,.06),(-.8+i*.1,0,1.78)],.003,'Galvanized',sides=5)
 for i in range(17):tube('Gate horizontal wire',[(-.85,0,.1+i*.1),(.85,0,.1+i*.1)],.003,'Galvanized',sides=5)
 beam('Gate diagonal',(-.85,0,.08),(.85,0,1.78),.02,.02,'Steel');handle(.69,-.04,.95)
 for z in [.23,1.56]:cylinder('Gate hinge',(-.93,0,z),.028,.12,'Iron')
 G.COLLIDERS[G.CURRENT].append(dict(name='Gate',position=[0,0,.92],size=[1.9,.1,1.84]))
 for length,plain in [(2,False),(1.75,False),(1.65,False),(1.55,False),(.60,False),(.40,False),(.40,True)]:
  asset('GutterHalfRound_%03d%s_1930'%(round(length*100),'Plain' if plain else ''),'Rainwater','Wall',.1);v=[]
  for x in [-length/2,length/2]:
   for r in [.092,.086]:
    for i in range(13):a=math.pi+i*math.pi/12;v.append((x,r*math.cos(a),r*math.sin(a)))
  f=[]
  for i in range(12):
   if length==.40 and not plain and i in [5,6]:continue
   f.extend([(i,i+1,i+27,i+26),(13+i,39+i,40+i,14+i)])
  for i in [0,12]:f.append((i,i+26,i+39,i+13))
  mesh('Hollow open gutter',v,f,'Galvanized')
  if length==.40 and not plain:
   # A real open lower throat, covered by a connected catch box rather than a capped pipe.
   for x in [-.20,.20]:box('Outlet catchbox end',(x,0,-.115),(.006,.065,.085),'Galvanized',0)
   for y in [-.033,.033]:box('Outlet catchbox side',(0,y,-.115),(.40,.006,.085),'Galvanized',0)
   for x in [-.122,.122]:box('Outlet catchbox floor',(x,0,-.157),(.15,.065,.006),'Galvanized',0)
   tube('Hollow outlet socket',[(0,0,-.153),(0,0,-.25)],.047,'Galvanized',wall=.003)
  for y in [-.092,.092]:tube('Rolled gutter edge',[(-length/2,y,0),(length/2,y,0)],.008,'Galvanized')
  for x in [-length*.32,length*.32]:tube('Gutter saddle',[(x,-.105,.012),(x,-.10,-.07),(x,0,-.112),(x,.10,-.07),(x,.105,.012)],.01,'Steel')
  G.ASSETS[G.CURRENT]['anchors']=[dict(name='Start',position=[-length/2,0,0]),dict(name='End',position=[length/2,0,0])]
 asset('GutterCorner90_1930','Rainwater','Wall',.1)
 v=[];f=[]
 for j in range(13):
  a=j*math.pi/24;cx=-.15+.15*math.sin(a);cy=.15-.15*math.cos(a)
  for r in [.092,.086]:
   for i in range(13):
    t=math.pi+i*math.pi/12;offset=r*math.cos(t)
    v.append((cx-math.sin(a)*offset,cy+math.cos(a)*offset,r*math.sin(t)))
 for j in range(12):
  k=j*26
  for i in range(12):f.extend([(k+i,k+i+1,k+i+27,k+i+26),(k+i+13,k+i+39,k+i+40,k+i+14)])
  for i in [0,12]:f.append((k+i,k+i+26,k+i+39,k+i+13))
 mesh('Open swept gutter corner',v,f,'Galvanized')
 for offset in [-.092,.092]:
  tube('Corner rolled lip',[(-.15+(.15-offset)*math.sin(j*math.pi/24),.15-(.15-offset)*math.cos(j*math.pi/24),0) for j in range(13)],.008,'Galvanized')
 G.ASSETS[G.CURRENT]['anchors']=[dict(name='Start',position=[-.15,0,0]),dict(name='End',position=[0,.15,0])]
 asset('DownpipeHollow_300_1930','Rainwater','Wall',.1);tube('Hollow pipe',[(0,0,0),(0,0,3)],.047,'Galvanized',wall=.003)
 for z in [.3,1.5,2.7]:
  tube('Pipe collar',[(0,0,z-.0185),(0,0,z+.0185)],.054,'Steel',wall=.007);box('Clamp stand-off',(0,.087,z),(.038,.12,.035),'Steel')
  for dx in [-.066,.066]:fastener((dx,-.014,z),.01)
 G.ASSETS[G.CURRENT]['anchors']=[dict(name='Bottom',position=[0,0,0]),dict(name='Top',position=[0,0,3])]
 asset('DownpipeElbow90_1930','Rainwater','Wall',.1)
 tube('Hollow swept elbow',[(0,-.17*(1-math.cos(i*math.pi/24)),.17*math.sin(i*math.pi/24)) for i in range(13)],.047,'Galvanized',wall=.003)
 asset('DownpipeShoe_1930','Rainwater','Wall',.2)
 tube('Open discharge shoe',[(0,0,.36),(0,0,.17),(0,-.04,.10),(0,-.18,.07),(0,-.29,.07)],.049,'Galvanized',wall=.003)
 asset('SplashBlockConcrete_1930','Rainwater')
 box('Splash bed',(0,0,.028),(.38,.72,.056),'Concrete')
 for x in [-.175,.175]:box('Splash kerb',(x,.025,.065),(.035,.65,.075),'Concrete')
 box('Splash backstop',(0,.34,.065),(.38,.035,.075),'Concrete')
 for length in [1]:
  asset('ConduitStraight_100_1930','Electrical','Wall',.1);tube('Conduit',[(0,0,0),(0,0,length)],.013,'Steel',wall=.002)
  for z in [.12,.88]:box('Conduit saddle',(0,.014,z),(.055,.035,.027),'Galvanized',.004)
 asset('ConduitElbow90_1930','Electrical','Wall',.1);tube('Conduit bend',[(0,-.10*(1-math.cos(i*math.pi/16)),.10*math.sin(i*math.pi/16)) for i in range(9)],.013,'Steel',wall=.002)
 asset('AwningCurved_164_1930','Canopies','Wall',.2)
 v=[]
 for x in [-.82,.82]:
  for i in range(17):t=i*math.pi/32;v.append((x,-.93*math.sin(t),.48*math.cos(t)))
 o=mesh('Painted curved hood',v,[(i,i+1,i+18,i+17) for i in range(16)],'WorkshopPaint');m=o.modifiers.new('Sheet','SOLIDIFY');m.thickness=.018;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 for x in [-.82,-.4,0,.4,.82]:tube('Hood seam',[(x,-.936*math.sin(i*math.pi/32),.486*math.cos(i*math.pi/32)) for i in range(17)],.01,'Steel')
 for x in [-.74,.74]:
  beam('Hood bracket',(x,.015,-.27),(x,-.7,.10),.037,.037,'Steel');box('Wall mounting plate',(x,.012,.03),(.085,.027,.60),'Steel')
 asset('EmploymentPorchTimber_232_1930','Canopies','Ground',.7)
 for x in [-.89,.89]:
  box('Porch post',(x,-1.79,1.38),(.12,.12,2.76),'Timber',.009,solid=True)
  beam('Porch knee brace',(x,-1.79,2.08),(x,-1.19,2.75),.09,.09,'Timber')
 beam('Front beam',(-1.01,-1.82,2.76),(1.02,-1.82,2.76),.15,.15,'TimberDark')
 for x in [-.84,-.14,.56,.96]:beam('Rafter',(x,-1.89,2.8),(x,-.14,3.01),.08,.10,'Timber')
 for i in range(38):
  x=-1.16+(i+.5)*2.32/38;z=.012*math.sin(i*math.pi/2)
  beam('Corrugated sheet',(x,-1.94,2.874+z),(x,0,3.108+z),.065,.025,'RoofMetal')
 for y,z in [(-1.94,2.874),(0,3.108)]:beam('Canopy flashing',(-1.2,y,z),(1.2,y,z),.045,.035,'Galvanized')
 # Fidelity corrections are part of this same canonical build, before export.
 import fidelity
 fidelity.refine()
 import final_qa
 final_qa.refine()
 # Common source and independent native export identity per collection.
 bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1;bpy.context.preferences.filepaths.save_version=0
 out=G.ROOT/'ArtExports/PeriodEnvironment1930';out.mkdir(parents=True,exist_ok=True);records=[];packets=[]
 for name,rec in G.ASSETS.items():
  p=G.uv_packet(G.GROUPS[name],True);pts=list(zip(*[iter(p['positions'])]*3));lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
  rec.update(boundsMin=lo,boundsMax=hi,id='silverscreen.period.'+name.lower(),availableFromYear=1930,colliders=[dict(name=c['name'],position=[-c['position'][0],c['position'][2],-c['position'][1]],size=[c['size'][0],c['size'][2],c['size'][1]]) for c in G.COLLIDERS[name]])
  rec['anchors']=[dict(name=a['name'],position=[-a['position'][0],a['position'][2],-a['position'][1]]) for a in rec['anchors']]
  records.append(rec);packets.append(p)
 G.save_packet(out/'kit_meshes.json.gz',dict(parts=packets,materials=G.surfaces(),assets=records))
 (out/'kit_manifest.json').write_text(json.dumps(dict(schema=1,units='metres',canonicalForward='+Z',source='ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend',generatorSha256=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest(),sourceDependencies={'geometry.py':hashlib.sha256((pathlib.Path(__file__).parent/'geometry.py').read_bytes()).hexdigest(),'fidelity.py':fidelity.SOURCE_SHA256,'final_qa.py':final_qa.SOURCE_SHA256},assets=records),indent=2))
 # Exclude other collections from this library's default view, not from linked instances.
 for i,c in enumerate(G.GROUPS.values()):
  c.hide_viewport=False;c.hide_render=False
  bpy.context.view_layer.layer_collection.children[c.name].exclude=i!=0
 bpy.ops.wm.save_as_mainfile(filepath=str(G.ROOT/'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'))
 print('PERIOD_KIT_BUILT',len(records),'canonical assets')
if __name__=='__main__':build()
