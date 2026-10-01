"""Manufacturing refinements in the canonical library build, never instance patches.

Source coordinates: metres, Z up, exterior/front -Y. Existing IDs and pivots stay.
"""
import bpy, math, pathlib, hashlib
from mathutils import Vector
import geometry as G
from geometry import box, beam, cylinder, tube, ball, mesh, asset
SOURCE_SHA256=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()

def reset(name):
 G.group(name)
 for o in list(G.GROUPS[name].objects):bpy.data.objects.remove(o,do_unlink=True)

def bolt(p,r=.009,axis=(0,-1,0),mat='Iron'):
 cylinder('Washer',p,r*1.6,.003,mat,axis,segments=16)
 q=Vector(p)+Vector(axis)*.004;cylinder('Hex bolt',q,r,.009,mat,axis,segments=6)

def ring(n,p,r,wire,mat='Iron',axis='Z',segments=40):
 pts=[]
 for i in range(segments+1):
  a=i*math.tau/segments;v=(r*math.cos(a),r*math.sin(a),0)
  if axis=='X':v=(0,v[0],v[1])
  if axis=='Y':v=(v[0],0,v[1])
  pts.append(tuple(p[k]+v[k] for k in range(3)))
 return tube(n,pts,wire,mat,sides=8)

def profile(n,points,depth,mat='Iron',y=0):
 v=[(x,y+dy,z) for dy in [-depth/2,depth/2] for x,z in points];N=len(points)
 return mesh(n,v,[tuple(range(N-1,-1,-1)),tuple(range(N,N*2))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)],mat)

def lathe(n,p,levels,mat,sides=40):
 v=[(p[0]+r*math.cos(i*math.tau/sides),p[1]+r*math.sin(i*math.tau/sides),p[2]+z) for z,r in levels for i in range(sides)]
 f=[]
 for j in range(len(levels)-1):
  for i in range(sides):f.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
 f += [tuple(range(sides-1,-1,-1)),tuple((len(levels)-1)*sides+i for i in range(sides))]
 o=mesh(n,v,f,mat)
 for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4
 return o

def window(w,h):
 # Fixed angle frame, bedding/rebate and a top-hung central ventilator sash.
 for x in [-w/2,w/2]:
  box('Outer frame web',(x,0,h/2),(.061,.14,h+.06),'Steel',.003)
  box('Interior mounting flange',(x,.078,h/2),(.095,.016,h+.09),'Iron',.002)
  for z in [.16,h-.16]:bolt((x,.091,z),.006,(0,1,0))
 for z in [0,h]:box('Frame head sill',(0,0,z),(w,.14,.061),'Steel',.003)
 cols=max(3,round(w/.45));rows=max(2,round(h/.49));cw=w/cols;rh=h/rows
 for i in range(1,cols):box('T mullion web',(-w/2+i*cw,.005,h/2),(.025,.095,h),'Steel',.002)
 for j in range(1,rows):box('T transom web',(0,.005,j*rh),(w,.095,.025),'Steel',.002)
 for i in range(cols):
  for j in range(rows):
   x=-w/2+(i+.5)*cw;z=(j+.5)*rh
   box('Seated glass pane',(x,.021,z),(cw-.031,.007,rh-.031),'Glass',0)
   for dx in [-cw/2+.014,cw/2-.014]:box('Glazing putty rebate',(x+dx,-.005,z),(.016,.025,rh-.018),'Iron',.002)
   for dz in [-rh/2+.014,rh/2-.014]:box('Glazing putty rebate',(x,-.005,z+dz),(cw-.018,.025,.016),'Iron',.002)
 # Operable sash spans upper middle pane(s); all user-operated hardware is inside (+Y).
 left=-w/2+(cols//2)*cw+.016;right=left+cw-.032;centre=(left+right)/2;bottom=h-rh+.016;top=h-.045
 for x in [left,right]:box('Ventilator sash',(x,.078,(bottom+top)/2),(.025,.028,top-bottom),'Steel',.002)
 for z in [bottom,top]:box('Ventilator sash',(centre,.078,z),(right-left,.028,.025),'Steel',.002)
 for x in [left+.06,right-.06]:
  cylinder('Ventilator hinge pin',(x,.107,top),.013,.075,'Brass',(1,0,0))
  box('Hinge leaf',(x,.097,top-.02),(.065,.014,.046),'Brass',.002)
 box('Interior latch plate',(centre,.105,bottom+.015),(.055,.018,.07),'Brass',.003)
 tube('Interior cockspur handle',[(centre,.116,bottom+.03),(centre,.157,bottom+.03),(centre+.085,.157,bottom+.075)],.009,'Brass')
 box('Latch keeper',(centre+.04,.106,bottom-.023),(.07,.025,.021),'Brass',.002)
 box('Interior sill',(0,.105,-.055),(w+.14,.28,.075),'LimestoneTrim',.006)
 o=box('Sloped exterior sill',(0,-.125,-.058),(w+.19,.26,.065),'LimestoneTrim',.006);o.rotation_euler.x=.09

def wiremesh(w,z0,z1,y=0):
 # Actual woven wire, with alternate offsets at crossings and bound perimeter.
 spacing=.12
 for slope in [-1,1]:
  for k in range(-40,41):
   intercept=k*spacing;ends=[]
   for x in [-w/2,w/2]:
    z=slope*x+intercept
    if z0<=z<=z1:ends.append((x,z))
   for z in [z0,z1]:
    x=(z-intercept)/slope
    if -w/2<x<w/2:ends.append((x,z))
   if len(ends)!=2:continue
   a,b=sorted(ends);steps=max(2,round(math.dist(a,b)/.085));pts=[]
   for j in range(steps+1):
    t=j/steps;x=a[0]+(b[0]-a[0])*t;z=a[1]+(b[1]-a[1])*t
    pts.append((x,y+slope*.004*math.cos(x*math.pi/spacing),z))
   tube('Woven galvanized diamond',pts,.0033,'Galvanized',sides=6)
 for x in [-w/2,w/2]:box('Vertical tension bar',(x,y,(z0+z1)/2),(.012,.012,z1-z0),'Galvanized',.001)
 for z in [z0,z1]:tube('Boundary tension wire',[(-w/2,y,z),(w/2,y,z)],.0045,'Galvanized')

def fence(length):
 for x in [-length/2,length/2]:
  cylinder('Tubular post',(x,0,1),.038,2,'Steel',segments=24)
  ball('Rounded post cap',(x,0,2),(.045,.045,.027),'Steel')
  for z in [.20,.86,1.52,1.86]:
   ring('Tension band',(x,0,z),.04,.007,'Galvanized');box('Band tab',(x+(.027 if x<0 else -.027),-.018,z),(.08,.017,.026),'Galvanized',.002)
 for z in [.17,1.86]:tube('Continuous tubular rail',[(-length/2,0,z),(length/2,0,z)],.024,'Steel')
 wiremesh(length-.10,.21,1.82,-.016)
 for x in [-length/2+.055+i*.35 for i in range(max(1,int(length/.35)))]:
  for z in [.21,1.82]:tube('Wire rail tie',[(x,-.022,z),(x,0,z+.035),(x,.023,z+.015),(x,-.012,z-.015)],.0035,'Galvanized',sides=6)

def gate():
 # Left hinge axis; one actual hinge post travels with the gate assembly.
 for x in [-.84,.84]:tube('Gate tube stile',[(x,0,.12),(x,0,1.84)],.026,'Steel')
 for z in [.12,1.84]:tube('Gate tube rail',[(-.84,0,z),(.84,0,z)],.026,'Steel')
 wiremesh(1.60,.17,1.79,-.008)
 cylinder('Gate hinge post',(-.96,0,1.0),.046,2,'Steel',segments=24);ball('Hinge post cap',(-.96,0,2),(.051,.051,.028),'Steel')
 for z in [.38,1.57]:
  ring('Hinge post clamp',(-.96,0,z),.048,.009,'Iron')
  box('Hinge bracket',(-.895,0,z),(.15,.044,.07),'Iron',.004)
  cylinder('Gate hinge barrel',(-.84,0,z),.035,.12,'Iron');cylinder('Vertical hinge pin',(-.84,0,z),.012,.15,'Brass')
  bolt((-.94,-.033,z),.011)
 tube('Gate diagonal tension rod',[(-.81,.025,.18),(.81,.025,1.77)],.009,'Steel')
 box('Latch backplate',(.80,-.034,1.02),(.10,.018,.17),'Iron',.004)
 tube('Latch pull',[(.75,-.044,.98),(.75,-.10,.98),(.75,-.10,1.10),(.75,-.044,1.10)],.01,'Iron')
 box('Sliding latch bolt',(.87,-.066,1.04),(.22,.025,.025),'Galvanized',.004)
 for z in [.96,1.10]:bolt((.82,-.047,z),.007)
 G.ASSETS[G.CURRENT]['anchors']=[dict(name='HingeAxis',position=[-.84,0,0]),dict(name='Latch',position=[.88,0,1.04])]

def sack(slumped=False):
 # Closed, weight-bearing fabric volume; gathered fabric fan, never a cylindrical neck.
 levels=[(0,.77),(.035,.94),(.12,1),(.25,1.02),(.39,.98),(.49,.89),(.56,.70),(.60,.39),(.625,.15),(.65,.20),(.70,.34)]
 N=40;v=[]
 for j,(z,r) in enumerate(levels):
  for i in range(N):
   a=i*math.tau/N;fold=.023*math.sin(7*a+z*16)+.018*math.cos(11*a-z*12)
   x=.23*(r+fold)*math.copysign(abs(math.cos(a))**.78,math.cos(a))+.025*math.sin(z*6)
   y=.18*(r+fold)*math.copysign(abs(math.sin(a))**.78,math.sin(a))+.017*math.sin(z*9)
   zz=z+(0 if j==0 else .007*math.sin(3*a+j))
   if slumped:x+=.065*z;zz*=.90;y*=1.10
   v.append((x,y,zz))
 f=[tuple(range(N-1,-1,-1)),tuple((len(levels)-1)*N+i for i in range(N))]
 for j in range(len(levels)-1):
  for i in range(N):f.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
 o=mesh('Weighted folded sack',v,f,'Canvas')
 for p in o.data.polygons:p.use_smooth=True
 for i in [0,N//2]:tube('Stitched side seam',[v[j*N+i] for j in range(1,8)],.0025,'TimberLight',sides=6)
 pts=[v[8*N+i] for i in range(N)]+[v[8*N]];tube('Gathered twine tie',pts,.0035,'TimberDark',sides=6)
 a=Vector(v[8*N+10]);tube('Tie tails',[a,a+Vector((.06,-.035,-.035)),a+Vector((.04,-.08,-.10))],.003,'TimberDark',sides=6)

def tool(kind,x,z,y=-.10,scale=1):
 before=set(G.GROUPS[G.CURRENT].objects)
 # All tools hang handle-down; each has a distinct, plausible working silhouette.
 if kind in ['claw hammer','wooden mallet']:
  o=box(kind+' ash handle',(0,0,-.18),(.027,.035,.34),'Timber',.01)
  if kind=='wooden mallet':box('Mallet head',(0,0,0),(.19,.07,.075),'TimberLight',.016)
  else:
   cylinder('Hammer striking face',(-.074,0,0),.025,.045,'Iron',(1,0,0))
   box('Forged hammer head',(0,0,0),(.105,.042,.043),'Iron',.009)
   for dy in [-.016,.016]:tube('Split claw',[(.025,dy,.008),(.065,dy,.035),(.104,dy,.026)],.009,'Iron')
 elif kind=='screwdriver':
  lathe('Turned screwdriver grip',(0,0,-.28),[(0,.013),(.02,.021),(.14,.021),(.16,.012)],'ToolRed',24)
  cylinder('Screwdriver shaft',(0,0,-.025),.005,.20,'Steel');box('Slotted tip',(0,0,.083),(.015,.003,.025),'Steel',.001)
 elif kind=='spanner':
  box('Spanner stem',(0,0,-.14),(.025,.018,.26),'Iron',.008)
  for zc in [0,-.28]:
   profile('Open spanner jaw',[(-.015,zc-.025),(-.043,zc-.005),(-.037,zc+.042),(-.014,zc+.049),(-.016,zc+.014),(.016,zc+.014),(.014,zc+.049),(.037,zc+.042),(.043,zc-.005),(.015,zc-.025)],.018,'Iron')
 elif kind=='pliers':
  for s in [-1,1]:
   tube('Pliers handles',[(s*.012,0,-.045),(s*.040,0,-.16),(s*.052,0,-.28)],.012,'ToolRed')
   profile('Pliers forged jaw',[(s*.005,-.06),(s*.036,0),(s*.014,.064),(s*.002,.06),(s*.004,.012),(s*-.012,-.04)],.021,'Iron')
  bolt((0,-.017,-.045),.013)
 elif kind=='hand saw':
  pts=[(-.065,.09),(.51,.035),(.51,-.085)]
  for j in range(28):pts.extend([(.51-j*.02,-.085-j*.002),(.50-j*.02,-.099-j*.002)])
  pts.append((-.065,-.145));profile('Toothed hand saw blade',pts,.003,'Galvanized')
  tube('Open saw grip',[(-.055,0,.095),(-.16,0,.13),(-.23,0,.055),(-.21,0,-.12),(-.09,0,-.145),(-.055,0,.095)],.022,'Timber')
  for zc in [-.1,.065]:bolt((-.065,-.017,zc),.009,(0,-1,0),'Brass')
 elif kind=='try square':
  box('Try square stock',(0,0,-.09),(.045,.038,.29),'TimberDark');box('Try square blade',(.16,0,.045),(.31,.004,.044),'Galvanized',.001)
 for o in set(G.GROUPS[G.CURRENT].objects)-before:o.location=Vector((x,y,z))+o.location*scale;o.scale*=scale

def toolboard():
 from build_kit import boards
 boards((0,.018,.625),(1.5,.047,1.25),7,0,'Timber',.006)
 for z in [.09,1.17]:
  box('Back mounting batten',(0,.051,z),(1.48,.035,.075),'TimberDark')
  for x in [-.61,.61]:bolt((x,-.015,z),.010)
 specs=[('claw hammer',-.57,.97),('wooden mallet',-.29,.98),('spanner',.04,.98),('pliers',.32,.99),('screwdriver',.59,.98),('hand saw',-.42,.42),('try square',.40,.38)]
 for kind,x,z in specs:
  tool(kind,x,z)
  for dx in [-.045,.045]:tube('Bent tool support hook',[(x+dx,-.015,z-.055),(x+dx,-.08,z-.055),(x+dx,-.135,z-.035)],.006,'Iron')
 G.ASSETS[G.CURRENT]['displayedTools']=[s[0] for s in specs]

def vise():
 # Bench-mounted cast body, dovetail slide, fixed/moving jaws and exposed screw hub.
 x=.78
 box('Vise mounting plate',(x,-.30,.975),(.33,.24,.03),'Iron',.012)
 for dx in [-.12,.12]:
  for y in [-.22,-.39]:bolt((x+dx,y,.998),.012,(0,0,1))
 profile('Cast vise fixed body',[(x-.11,.98),(x-.13,1.05),(x-.09,1.13),(x+.09,1.13),(x+.13,1.05),(x+.11,.98)],.24,'Iron',-.42)
 box('Fixed jaw face',(x,-.551,1.145),(.29,.015,.075),'Steel',.004)
 box('Moving jaw casting',(x,-.64,1.10),(.28,.075,.15),'Iron',.014)
 box('Moving jaw face',(x,-.596,1.145),(.29,.015,.075),'Steel',.004)
 box('Sliding vise guide',(x,-.58,1.015),(.085,.42,.075),'Steel',.007)
 cylinder('Lead screw',(x,-.67,1.04),.018,.29,'Steel',(0,1,0))
 for i in range(7):ring('Exposed screw thread',(x,-.75+i*.009,1.04),.020,.0025,'Iron','Y',16)
 cylinder('Screw boss',(x,-.81,1.04),.04,.065,'Iron',(0,1,0));cylinder('Sliding tommy bar',(x,-.85,.985),.01,.32,'Steel')
 for z in [.825,1.145]:ball('Tommy bar stop',(x,-.85,z),(.019,.019,.019),'Steel')

def workbench():
 from build_kit import boards
 for x in [-.94,.94]:
  for y in [-.31,.31]:box('Square mortised leg',(x,y,.425),(.12,.12,.85),'TimberDark',.009)
  for z in [.20,.72]:box('End stretcher',(x,0,z),(.10,.68,.115),'Timber')
 for y in [-.315,.315]:
  box('Apron',(0,y,.735),(2.02,.075,.235),'Timber')
  box('Lower stretcher',(0,y,.235),(1.98,.075,.10),'TimberDark')
  for x in [-.94,.94]:
   for z in [.22,.74]:bolt((x,y+(-.044 if y<0 else .044),z),.013,(0,-1 if y<0 else 1,0))
 boards((0,0,.90),(2.25,.80,.10),5,1,'TimberLight',.0015)
 boards((0,0,.22),(1.87,.60,.055),4,1,'Timber',.006)
 beam('Back diagonal brace',(-.84,.345,.29),(.84,.345,.70),.075,.035,'TimberDark')
 for x in [-.75,-.45,-.15,.15,.45]:cylinder('Bench dog socket',(x,-.25,.9505),.012,.002,'TimberDark')
 vise()

def wheelbarrow():
 # Rounded pressed/riveted tray, closed wall gauge and folded rim; open inside.
 N=40;v=[]
 for z,w,d,cy in [(.50,.235,.285,0),(.57,.29,.345,-.01),(.80,.35,.435,-.02)]:
  for i in range(N):
   a=i*math.tau/N;x=w*math.copysign(abs(math.cos(a))**.32,math.cos(a));y=cy+d*math.copysign(abs(math.sin(a))**.32,math.sin(a));v.append((x,y,z))
 f=[tuple(range(N-1,-1,-1))]
 for j in range(2):
  for i in range(N):f.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
 o=mesh('Pressed steel barrow tray',v,f,'PaintWear');m=o.modifiers.new('Sheet gauge','SOLIDIFY');m.thickness=.004;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 tube('Folded tray rim',v[2*N:]+[v[2*N]],.012,'WorkshopPaint')
 for x in [-.22,.22]:
  beam('Hardwood load runner',(x,-.61,.30),(x,.46,.46),.052,.064,'Timber')
  beam('Raised hardwood handle',(x,.46,.46),(x,1.0,.77),.052,.064,'Timber')
  cylinder('Rounded hand grip',(x,.93,.75),.031,.22,'TimberDark',(0,1,.30),segments=24)
  tube('Bent support leg',[(x,.03,.48),(x,.16,.08),(x,.22,.035),(x,.33,.035),(x,.39,.49)],.019,'Iron')
  for y in [-.24,.25]:box('Tray saddle bracket',(x,y,.465),(.095,.05,.060),'Iron',.004);bolt((x,y,.502),.008,(0,0,1))
  box('Axle bearing block',(x,-.57,.23),(.085,.10,.09),'Iron',.006)
 tube('Leg cross stay',[(-.22,.27,.16),(.22,.27,.16)],.014,'Steel')
 # Solid rubber tyre around a stamped steel spoked wheel.
 ring('Solid rubber tyre',(0,-.57,.23),.20,.031,'Black','X',48)
 ring('Steel wheel rim',(0,-.57,.23),.165,.015,'Iron','X',40)
 cylinder('Wheel hub',(0,-.57,.23),.055,.115,'Iron',(1,0,0));cylinder('Axle',(0,-.57,.23),.018,.54,'Steel',(1,0,0))
 for i in range(8):
  a=i*math.tau/8;beam('Pressed wheel spoke',(0,-.57+.042*math.cos(a),.23+.042*math.sin(a)),(0,-.57+.16*math.cos(a),.23+.16*math.sin(a)),.018,.012,'Iron')
 for x in [-.28,.28]:bolt((x,-.57,.23),.019,(1 if x>0 else -1,0,0))

def bench():
 from build_kit import boards
 for x in [-.83,.83]:
  # Cast ends bear below the seat; they must not emerge through its upper face.
  profile('Cast bench end',[(x-.037,.05),(x+.037,.05),(x+.038,.44),(x+.025,.455),(x-.026,.455)],.035,'Steel',-.18)
  for y in [-.18,.18]:
   beam('Splayed bench leg',(x,y,.03),(x,y*.63,.43),.035,.042,'Steel');box('Anchored foot',(x,y,.021),(.13,.115,.042),'Steel',.008)
   bolt((x,y,.045),.010,(0,0,1))
  beam('Seat support angle',(x,-.24,.439),(x,.31,.439),.045,.045,'Steel')
  # Return around the rear seat edge, then support the backs of the raked slats.
  beam('Back support return',(x,.31,.439),(x,.254725,.58),.035,.032,'Steel')
  beam('Raked back upright',(x,.254725,.58),(x,.375675,.99),.035,.032,'Steel')
 for j in range(5):
  y=-.22+j*.108;box('Seat slat',(0,y,.482),(2.1,.091,.041),'Timber',.008)
  for x in [-.83,.83]:bolt((x,y,.505),.006,(0,0,1),'Iron')
 for j in range(4):
  z=.625+j*.108;y=.233+(z-.625)*.295;o=box('Back slat',(0,y,z),(2.1,.033,.088),'Timber',.006);o.rotation_euler.x=-.285
  for x in [-.83,.83]:bolt((x,y-.022,z),.006)
 beam('Under-seat tie',(-.83,.13,.30),(.83,.13,.30),.035,.035,'Steel')
 # Keep the collision proxy around the corrected rear support envelope.
 G.COLLIDERS[G.CURRENT]=[dict(name='Bench',position=[0,.065,.5],size=[2.1,.67,1])]

def barrel(wood):
 levels=[(0,.255),(.10,.282),(.27,.308),(.45,.32),(.63,.308),(.80,.282),(.9,.255)]
 if wood:
  for i in range(24):
   a=i*math.tau/24+.002;b=(i+1)*math.tau/24-.002;v=[]
   for z,r in levels:v.extend([(r*math.cos(a),r*math.sin(a),z),(r*math.cos(b),r*math.sin(b),z),((r-.019)*math.cos(b),(r-.019)*math.sin(b),z),((r-.019)*math.cos(a),(r-.019)*math.sin(a),z)])
   f=[(3,2,1,0),tuple((len(levels)-1)*4+j for j in range(4))]
   for k in range(len(levels)-1):
    for j in range(4):f.append((k*4+j,k*4+(j+1)%4,(k+1)*4+(j+1)%4,(k+1)*4+j))
   mesh('Coopered solid stave',v,f,'TimberLight' if i%7==0 else 'Timber')
  for z in [.08,.26,.64,.82]:
   radius=max(r for zz,r in levels if abs(zz-z)<.22)+.006
   # Match the actual stave profile at both band edges, rather than leaving gaps.
   def at(h):
    for (za,ra),(zb,rb) in zip(levels,levels[1:]):
     if za<=h<=zb:return ra+(rb-ra)*(h-za)/(zb-za)
   tube('Continuous cooper hoop',[(0,0,z-.021),(0,0,z+.021)],max(at(z-.021),at(z+.021))+.004,'Steel',wall=.004,sides=48)
 else:
  prof=[(0,.292),(.025,.305),(.055,.305),(.075,.295),(.275,.295),(.288,.308),(.315,.308),(.328,.295),(.595,.295),(.608,.308),(.635,.308),(.648,.295),(.835,.295),(.855,.305),(.882,.305),(.90,.292)]
  lathe('Formed steel drum shell',(0,0,0),prof,'WorkshopPaint',48)
 cylinder('Seated barrel head',(0,0,.884),.250 if wood else .288,.028,'TimberLight' if wood else 'RoofMetal',segments=48)
 cylinder('Threaded or timber bung',(.11,0,.908),.026,.019,'TimberDark' if wood else 'Iron',segments=16)

def refine():
 from build_kit import boards,handle
 for name in list(G.ASSETS):
  if name.startswith('WindowSteel_'):
   reset(name);w,h=map(lambda x:int(x)/100,name.split('_')[1].split('x'));window(w,h)
  elif name.startswith('FenceWire_'):
   reset(name);fence(int(name.split('_')[1])/100)
  elif name.startswith('Gutter'):
   for o in G.GROUPS[name].objects:
    if o.name.startswith(('Hollow open gutter','Open swept gutter corner')):
     for poly in o.data.polygons:poly.use_smooth=True
 reset('FenceGateWire_180_1930');gate()
 reset('SackCanvas_1930');sack()
 asset('SackCanvasSlumped_1930','Materials');sack(True);G.COLLIDERS[G.CURRENT].append(dict(name='Sack',position=[0,0,.32],size=[.49,.41,.65]))
 reset('ToolRackCarpenter_1930');toolboard()
 reset('WorkbenchJoiner_225_1930');workbench()
 G.group('ToolTrayCarpenter_1930')
 for o in list(G.GROUPS[G.CURRENT].objects):
  if o.name.startswith(('Hammer shaft','Hammer head')):bpy.data.objects.remove(o,do_unlink=True)
 before=set(G.GROUPS[G.CURRENT].objects);tool('claw hammer',0,0,0,.70)
 from mathutils import Matrix
 pose=Matrix.Translation(Vector((.11,-.05,.075)))@Matrix.Rotation(math.pi/2,4,'X')
 for o in set(G.GROUPS[G.CURRENT].objects)-before:o.matrix_world=pose@o.matrix_world
 box('Plane iron',(.03,.07,.108),(.045,.075,.012),'Steel',.002)
 cylinder('Plane adjustment screw',(-.04,.07,.13),.014,.017,'Brass')
 G.ASSETS[G.CURRENT]['displayedTools']=['claw hammer','bench hand plane']
 reset('WheelbarrowSteel_1930');wheelbarrow()
 reset('BenchSlatted_210_1930');bench()
 reset('BarrelTimber_1930');barrel(True)
 reset('BarrelPaintedSteel_1930');barrel(False)
 reset('NoticeBoardTimber_173_1930')
 box('Complete shiplap backing',(0,.014,.575),(1.73,.055,1.15),'TimberDark',.004)
 box('Full recessed notice insert',(0,-.019,.575),(1.56,.018,.99),'Canvas',.002)
 for x in [-.815,.815]:box('Rebated timber stile',(x,-.037,.575),(.10,.09,1.15),'Timber',.008)
 for z in [.05,1.10]:box('Rebated frame rail',(0,-.037,z),(1.73,.09,.10),'Timber',.008)
 for x in [-.71,.71]:
  box('Wall mounting cleat',(x,.0725,.58),(.09,.055,.98),'TimberDark')
  for z in [.12,1.03]:bolt((x,-.012,z),.006)
 # Minor refinements retain existing functional volumes, proportions and colliders.
 G.group('ChairBentwood_1930')
 for y in [-.2,.2]:beam('Chair stretcher',(-.215,y,.22),(.215,y,.22),.025,.03,'TimberDark')
 for x in [-.215,.215]:beam('Chair side stretcher',(x,-.2,.24),(x,.2,.24),.025,.03,'TimberDark')
 G.group('DeskPedestal_1930')
 for x in [-.77,.77]:
  for z in [.16,.35,.54]:
   box('Drawer recessed field',(x,-.386,z),(.34,.008,.12),'TimberDark',.004)
   for dx in [-.15,.15]:bolt((x+dx,-.394,z),.004,mat='Brass')
 for x in [-1.00,1.00]:box('Desk side inset',(x,.015,.40),(.013,.58,.48),'Timber',.006)
 G.group('FilingCabinet_4Drawer_1930')
 box('Cabinet recessed plinth',(0,.01,.045),(.59,.54,.09),'Steel',.012)
 for z in [.20,.52,.84,1.16]:
  for x in [-.076,.076]:bolt((x,-.371,z+.065),.0035,mat='Brass')
 G.group('CupboardUtility_1930')
 for x in [-.57,.57]:
  for z in [.28,1.62]:
   box('Cupboard hinge leaf',(x,-.355,z),(.045,.012,.085),'Iron',.002);cylinder('Cupboard hinge knuckle',(x,-.363,z),.012,.10,'Iron')
 G.group('ShelfSteelTimber_170_1930')
 for x in [-.85,.85]:
  for y in [-.26,.26]:
   box('Shelf foot plate',(x,y,.012),(.10,.10,.024),'Steel',.003)
   for z in [.12,.58,1.04,1.50,1.96]:bolt((x,y-.034,z),.006)
 G.group('TelephoneDesk_1930')
 for x in [-.13,.13]:
  tube('Receiver cradle fork',[(x,.05,.15),(x,.05,.20),(x,.085,.225)],.008,'Black')
 box('Dial number card',(0,-.034,.176),(.031,.024,.002),'PaperLight',.002)
 tube('Receiver fabric cord',[(.14+.015*math.cos(i*.7),.10+i*.002,.22-i*.003) for i in range(55)],.0045,'Black',sides=8)
 G.group('DeskLampBanker_1930')
 for o in G.GROUPS[G.CURRENT].objects:
  if o.name.startswith('Green glass shade'):
   for poly in o.data.polygons:poly.use_smooth=True
 ring('Lamp foot bead',(0,0,.025),.076,.004,'Brass')
 for x in [-.12,.12]:cylinder('Shade tilt pin',(x,-.01,.37),.019,.025,'Brass',(1,0,0))
 tube('Desk lamp cloth supply',[(0,.065,.035),(.04,.12,.009),(.13,.16,.008)],.0035,'Black')
 G.group('LadderTimber_260_1930')
 for z in [.16,.72,1.56,2.40]:
  for x in [-.24,.24]:bolt((x,-.048,z),.008)
 G.group('GroundsToolRack_1930')
 for x in [-.48,0,.48]:
  for dx in [-.035,.035]:tube('Grounds tool support',[(x+dx,-.02,1.37),(x+dx,-.13,1.37),(x+dx,-.15,1.40)],.008,'Iron')
  cylinder('Tool head ferrule',(x,-.10,.39),.021,.12,'Iron')
 tube('Spade D grip',[(-.48,-.10,1.68),(-.56,-.10,1.76),(-.56,-.10,1.88),(-.40,-.10,1.88),(-.40,-.10,1.76),(-.48,-.10,1.68)],.012,'Timber')
 for i in range(13):
  x=.305+i*.029;tube('Broom bristle bundle',[(x,-.13,.20),(x+.010*math.sin(i),-.135,.095)],.011,'Canvas',sides=6)
 G.ASSETS[G.CURRENT]['displayedTools']=['spade with D grip','rake','broom']
 for name in ['CrateTimber_075_1930','CrateTimber_045_1930']:
  G.group(name);w,d,h=(.75,.55,.58) if '075' in name else(.45,.35,.34)
  for x in [-w/2,w/2]:
   for y in [-d*.32,d*.32]:
    box('End batten',(x,y,h/2),(.046,.065,h-.015),'TimberDark',.003)
    for z in [.08,h-.08]:bolt((x+(.028 if x>0 else -.028),y,z),.005,(1 if x>0 else -1,0,0))
  for x in [-w*.32,w*.32]:box('Crate base cleat',(x,0,.027),(.075,d,.044),'TimberDark',.003)
 for name in ['BarrelTimber_1930','BarrelPaintedSteel_1930']:
  G.group(name);wood='Timber' in name
  for z in [.025,.875]:ring('Rolled rim' if not wood else 'Chime hoop',(0,0,z),.30 if not wood else .26,.012,'Iron')
  if wood:
   for y in [-.16,-.08,0,.08,.16]:
    half=math.sqrt(.245**2-y*y);box('Head plank joint',(0,y,.901),(half*2,.002,.0015),'TimberDark',0)
   for z in [.08,.26,.64,.82]:bolt((0,-(.29 if z in [.08,.82] else .32),z),.006)
  else:
   ring('Large threaded bung seat',(.11,0,.912),.032,.006,'Iron');box('Bung wrench recess',(.11,0,.922),(.035,.012,.004),'Steel',.002)
   cylinder('Vent bung',(-.13,.10,.907),.016,.019,'Iron',segments=12)
 for name in ['MaterialRackTimber_400_1930','ScaffoldBayTimber_200_1930']:
  G.group(name);rack=name.startswith('Material');xs=[-1.8,0,1.8] if rack else [-.95,.95];ys=[-.4,.4] if rack else[-.37,.37]
  for x in xs:
   for y in ys:
    box('Post base shoe',(x,y,.025),(.16,.16,.05),'Iron',.003)
    for z in ([.2,.88,1.57] if rack else [.25,1.70]):bolt((x,y-.053,z),.011)
 # Correct the downward-facing return on the back of the personnel-door pulls.
 for name in ['DoorGlazedTimber_110x258_1930','DoorUtilityTimber_150x260_1930']:
  G.group(name)
  for o in G.GROUPS[name].objects:
   if o.name.startswith(('Pull','Pull socket')) and o.location.y>.015:pass
  w=1.1 if '110' in name else 1.5
  # Replace only the +Y pull pieces based on their actual mesh bounds.
  for o in list(G.GROUPS[name].objects):
   if o.name.startswith(('Pull','Pull socket')):
    yy=[(o.matrix_world@Vector(v)).y for v in o.bound_box]
    if max(yy)>.020:bpy.data.objects.remove(o,do_unlink=True)
  tube('Interior return pull',[(w/2-.22,.046,1.1),(w/2-.22,.095,1.1),(w/2-.12,.095,1.1),(w/2-.12,.046,1.1)],.008,'Brass')
  box('Mortice latch edge',(w/2-.004,0,1.055),(.008,.040,.17),'Brass',.002)
 G.group('ServiceDoorPair_485x346_1930')
 for side in [-1,1]:
  for z in [.45,1.65,2.94]:
   box('Jamb hinge plate',(side*2.47,-.065,z),(.12,.021,.22),'Iron',.003)
   for dz in [-.075,.075]:bolt((side*2.47,-.081,z+dz),.013)
  # Fix to the meeting stile, clear of the timber diagonal brace.
  x=side*.075;box('Drop bolt backplate',(x,-.079,.30),(.065,.013,.40),'Iron',.004)
  tube('Vertical drop bolt',[(x,-.109,.08),(x,-.109,.46),(x+side*.05,-.109,.46)],.012,'Steel')
  for z in [.14,.38]:
   box('Drop bolt guide saddle',(x,-.0925,z),(.047,.019,.035),'Iron',.003)
   tube('Drop bolt guide collar',[(x,-.109,z-.018),(x,-.109,z+.018)],.020,'Iron',wall=.006)
  for z in [.115,.485]:bolt((x,-.088,z),.006)
  box('Bolt floor keeper',(x,-.04,.025),(.085,.12,.035),'Iron',.004)
 G.group('AwningCurved_164_1930')
 box('Awning wall flashing',(0,.008,.475),(1.69,.075,.045),'Galvanized',.004)
 for x in [-.74,.74]:
  for z in [-.22,.25]:bolt((x,-.008,z),.010)
 tube('Hood front rolled drip',[(-.82,-.935,0),(.82,-.935,0)],.016,'WorkshopPaint')
 G.group('DownpipeHollow_300_1930')
 for z in [.30,1.50,2.70]:
  box('Pipe clamp wall plate',(0,.132,z),(.09,.018,.13),'Iron',.004)
  for dz in [-.04,.04]:bolt((0,.116,z+dz),.006)
 G.group('DownpipeShoe_1930')
 tube('Shoe socket collar',[(0,0,.32),(0,0,.37)],.056,'Galvanized',wall=.007)
 G.ASSETS[G.CURRENT]['anchors']=[dict(name='Inlet',position=[0,0,.36]),dict(name='Discharge',position=[0,-.29,.07])]
 G.group('EmploymentPorchTimber_232_1930')
 box('Canopy wall ledger',(0,-.06,2.99),(2.05,.13,.15),'TimberDark')
 for x in [-.89,.89]:
  box('Post base socket',(x,-1.79,.035),(.16,.16,.07),'Iron',.004)
  for z in [.16,2.70]:bolt((x,-1.857,z),.013)
  bolt((x,-.137,2.99),.013)
 # Office additions share the same source/catalog conventions.
 asset('WastebasketWire_1930','Office',clearance=.2)
 for z,r in [(.025,.12),(.37,.15)]:ring('Basket rolled rim',(0,0,z),r,.008,'Steel')
 cylinder('Basket base',(0,0,.012),.12,.024,'Iron')
 for i in range(24):
  a=i*math.tau/24;tube('Basket wire',[(.12*math.cos(a),.12*math.sin(a),.025),(.15*math.cos(a),.15*math.sin(a),.37)],.003,'Steel',sides=6)
 for z in [.12,.23]:ring('Basket binding',(0,0,z),.12+z*.081,.0035,'Steel')
 G.COLLIDERS[G.CURRENT].append(dict(name='Basket',position=[0,0,.19],size=[.31,.31,.38]))
 asset('CoatStandTimber_1930','Office',clearance=.35)
 lathe('Turned coat stand',(0,0,0),[(0,.04),(.09,.065),(.19,.03),(1.47,.022),(1.56,.048),(1.70,.025),(1.76,.015)],'TimberDark')
 for i in range(4):
  a=i*math.pi/2;vec=lambda r,z:(r*math.cos(a),r*math.sin(a),z)
  tube('Splayed stand foot',[vec(.02,.18),vec(.17,.10),vec(.28,.024)],.025,'TimberDark')
  tube('Bentwood coat hook',[vec(.02,1.50),vec(.13,1.63),vec(.23,1.68),vec(.24,1.60)],.014,'Timber')
 G.COLLIDERS[G.CURRENT].append(dict(name='Coat stand',position=[0,0,.88],size=[.58,.58,1.76]))
 asset('ClockSchoolhouse_1930','Office','Wall',.2)
 cylinder('Clock timber case',(0,.025,0),.24,.075,'TimberDark',(0,1,0),segments=48)
 cylinder('Clock dial',(0,-.021,0),.208,.008,'PaperLight',(0,1,0),segments=48)
 ring('Clock brass bezel',(0,-.035,0),.215,.012,'Brass','Y',48)
 for i in range(12):
  a=i*math.pi/6;beam('Hour mark',(.175*math.sin(a),-.03,.175*math.cos(a)),(.195*math.sin(a),-.03,.195*math.cos(a)),.005,.005,'Black')
 beam('Clock hour hand',(0,-.038,0),(.082,-.038,.055),.012,.006,'Black');beam('Clock minute hand',(0,-.045,0),(-.07,-.045,.135),.007,.004,'Black')
 cylinder('Clock spindle',(0,-.049,0),.012,.011,'Brass',(0,1,0))
