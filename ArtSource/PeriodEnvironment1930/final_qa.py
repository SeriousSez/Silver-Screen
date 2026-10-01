"""Final canonical QA corrections. Source metres, Z up, front -Y.

Invoked by the same library build after fidelity.py. No instance-only mesh repairs.
"""
import bpy, bmesh, math, hashlib
from pathlib import Path
from mathutils import Vector, Matrix
import geometry as G
from geometry import box, beam, tube, cylinder, mesh, ball, asset
from fidelity import reset, bolt, ring, profile, lathe
SOURCE_SHA256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()

def smooth_path(points,steps=5):
 pts=[Vector(p) for p in points];out=[]
 for j in range(len(pts)-1):
  a,b,c,d=pts[max(0,j-1)],pts[j],pts[j+1],pts[min(len(pts)-1,j+2)]
  for i in range(steps):
   t=i/steps;out.append(tuple(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)))
 return out+[tuple(pts[-1])]

def bevel(o,width=.003):
 bpy.context.view_layer.objects.active=o
 m=o.modifiers.new('Manufactured edge','BEVEL');m.width=width;m.segments=3
 bpy.ops.object.modifier_apply(modifier=m.name)
 return o

def oval_handle(name,points,widths,depths,mat='TimberLight',sides=12):
 # Ring sections perpendicular to the longitudinal centre line; no cylinder sleeves.
 pts=[Vector(p) for p in points];verts=[]
 for j,p in enumerate(pts):
  t=(pts[min(j+1,len(pts)-1)]-pts[max(0,j-1)]).normalized()
  a=Vector((1,0,0));a=(a-t*a.dot(t)).normalized();b=t.cross(a)
  for i in range(sides):
   q=i*math.tau/sides;verts.append(p+a*widths[j]*math.cos(q)+b*depths[j]*math.sin(q))
 faces=[tuple(range(sides-1,-1,-1)),tuple((len(pts)-1)*sides+i for i in range(sides))]
 for j in range(len(pts)-1):
  for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
 o=mesh(name,verts,faces,mat)
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o

def closed_grip(name,cx,cy,cz,wx,hz,ix,iz,depth,mat='Timber',slant=0):
 # Solid rounded ring with a genuine hand aperture and a broad blade attachment cheek.
 N=32;v=[]
 for y in [-depth/2,depth/2]:
  for w,h in [(wx,hz),(ix,iz)]:
   for i in range(N):
    a=i*math.tau/N;z=h*math.sin(a);x=w*math.copysign(abs(math.cos(a))**.7,math.cos(a))+slant*z
    v.append((cx+x,cy+y,cz+z))
 f=[]
 for i in range(N):
  j=(i+1)%N
  f.extend([(i,j,N+j,N+i),(2*N+i,3*N+i,3*N+j,2*N+j),(i,2*N+i,2*N+j,j),(N+i,N+j,3*N+j,3*N+i)])
 return bevel(mesh(name,v,f,mat),.003)

def handsaw(x=-.42,z=.42,y=-.10):
 # 530 mm tapering plate with real tooth profile, 2.4 mm gauge, closed ergonomic grip.
 pts=[(-.035,.11),(.49,.032),(.505,.010),(.505,-.075)]
 for i in range(35):
  xx=.505-i*.0155;zz=-.075-(.505-xx)*.095
  pts.extend([(xx-.005,zz-.009),(xx-.014,zz)])
 pts.extend([(-.050,-.134),(-.050,.085)])
 before=set(G.GROUPS[G.CURRENT].objects)
 profile('Saw tapered toothed steel plate',pts,.0024,'Galvanized')
 closed_grip('Saw shaped closed walnut grip',-.114,0,-.014,.094,.122,.046,.077,.032,'TimberDark',.16)
 bevel(profile('Saw blade mounting cheek',[(-.055,.105),(-.012,.09),(-.027,-.121),(-.073,-.137)],.031,'TimberDark'),.004)
 for zz in [.066,-.079]:
  for side in [-1,1]:bolt((-.038,side*.018,zz),.009,(0,side,0),'Brass')
 # Dedicated pins support the handle aperture and the toe: no generic hooks through steel.
 for xx,zz in [(-.105,.045),(.463,-.109)]:
  tube('Saw hanging hook',[(xx,.092,zz),(xx,.012,zz),(xx,-.020,zz+.015)],.0045,'Iron',sides=8)
 for o in set(G.GROUPS[G.CURRENT].objects)-before:o.location+=Vector((x,y,z))

def carpenter():
 reset('ToolRackCarpenter_1930')
 from fidelity import tool
 from build_kit import boards
 boards((0,.018,.625),(1.5,.047,1.25),7,0,'Timber',.006)
 for zz in [.09,1.17]:
  box('Wall mounting batten',(0,.051,zz),(1.48,.035,.075),'TimberDark')
  for xx in [-.61,.61]:bolt((xx,-.013,zz),.008)
 specs=[('claw hammer',-.56,.99),('wooden mallet',-.28,.99),('spanner',.025,.98),('pliers',.31,1.0),('screwdriver',.60,1.08),('try square',.42,.35)]
 for kind,x,z in specs:
  if kind=='try square':try_square(x,z)
  else:tool(kind,x,z)
  # Each support is set at a feature that can actually carry the tool's weight.
  if kind=='screwdriver':
   box('Screwdriver shelf clip',(x,-.092,z-.286),(.060,.085,.014),'Iron',.004)
   tube('Grip retention hoop',[(x-.025,-.055,z-.25),(x-.025,-.13,z-.25),(x+.025,-.13,z-.25),(x+.025,-.055,z-.25)],.004,'Iron',sides=8)
  elif kind=='pliers':
   tube('Pliers pivot cradle',[(x,-.012,z-.078),(x,-.121,z-.078),(x,-.132,z-.038)],.005,'Iron')
  else:
   height=z-.03 if kind in ('claw hammer','wooden mallet') else z-.005
   if kind=='try square':height=z+.024
   for dx in [-.035,.035]:tube('Shaped hanging peg',[(x+dx,-.012,height),(x+dx,-.125,height),(x+dx,-.139,height+.018)],.0045,'Iron',sides=8)
 # Replace straight narrow square hammer shafts with comfortable continuously shaped ash.
 for o in list(G.GROUPS[G.CURRENT].objects):
  if 'ash handle' in o.name:bpy.data.objects.remove(o,do_unlink=True)
 for x in [-.56,-.28]:
  oval_handle('Shaped ash hand-tool haft',[(x,-.10,.645),(x-.008,-.10,.67),(x-.004,-.10,.75),(x+.005,-.10,.88),(x,-.10,1.00)],[.008,.016,.014,.012,.011],[.010,.019,.017,.016,.017])
 handsaw()
 G.ASSETS[G.CURRENT]['displayedTools']=['claw hammer','wooden mallet','open-ended spanner','pliers','screwdriver','hand saw','try square']

def try_square(x,z,y=-.10):
 # A riveted rosewood-and-brass stock and hardened thin blade, not two crossing blocks.
 before=set(G.GROUPS[G.CURRENT].objects)
 bevel(profile('Square rounded rosewood stock',[(-.024,.067),(.022,.067),(.026,.033),(.023,-.20),(.015,-.231),(-.020,-.229),(-.029,-.21),(-.029,.041)],.029,'TimberDark'),.003)
 box('Square brass reference wear face',(.026,0,-.080),(.003,.032,.295),'Brass',.0007)
 bevel(profile('Square hardened blade and mortised tang',[(-.023,.061),(.31,.061),(.318,.054),(.318,.013),(.024,.013),(.019,-.026),(-.023,-.026)],.0026,'Galvanized'),.0005)
 for face in [-1,1]:
  for zz in [.043,-.007,-.177]:
   cylinder('Square flush brass stock rivet',(-.006,face*.0155,zz),.0052,.0018,'Brass',(0,face,0),segments=16)
  # Fine engraved divisions improve reading at close inspection without thick raised strips.
  for i in range(1,29):
   length=.014 if i%5==0 else .007
   box('Blade etched division',(.025+i*.010,face*.00145,.060-length/2),(.00045,.00025,length),'Iron',0)
 for o in set(G.GROUPS[G.CURRENT].objects)-before:o.location+=Vector((x,y,z))

def grounds():
 reset('GroundsToolRack_1930')
 for z in [.32,1.36]:
  box('Grounds wall rail',(0,.004,z),(1.35,.045,.09),'TimberDark',.007)
  for x in [-.57,.57]:bolt((x,-.024,z),.009)
 for x in [-.47,0,.47]:
  cylinder('Round ash shaft',(x,-.15,1.02),.016,1.23,'TimberLight',segments=16)
  # Shaped clamp seats the shaft in front of the rail; retention lip is an actual support.
  tube('Spring shaft clip',[(x-.022,-.025,1.35),(x-.022,-.16,1.35),(x,-.174,1.35),(x+.022,-.16,1.35),(x+.022,-.025,1.35)],.006,'Iron',sides=8)
  cylinder('Shaft stop collar',(x,-.15,1.365),.023,.016,'Iron',segments=16)
 # Spade: curved, closed blade with shoulders and thin sharpened lower edge.
 x=-.47;rows=[(.10,.028),(.13,.085),(.21,.123),(.37,.119),(.415,.10)];v=[];N=9
 for z,w in rows:
  for i in range(N):
   u=i/(N-1)*2-1;v.append((x+w*u,-.195-.027*(1-u*u)+.05*(z-.10),z))
 f=[(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i) for j in range(len(rows)-1) for i in range(N-1)]
 o=mesh('Curved spade blade',v,f,'Iron');m=o.modifiers.new('Forged blade gauge','SOLIDIFY');m.thickness=.0035;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 for p in o.data.polygons:p.use_smooth=True
 tube('Spade socket neck',[(x,-.15,.50),(x,-.15,.40),(x,-.18,.34)],.021,'Iron',sides=16)
 for s in [-1,1]:tube('Folded spade shoulder tread',[(x+s*.025,-.15,.408),(x+s*.10,-.17,.408)],.010,'Iron')
 cylinder('Spade D-grip ferrule',(x,-.15,1.62),.020,.085,'Iron')
 for s in [-1,1]:tube('Forged D-grip fork',[(x,-.15,1.63),(x+s*.067,-.15,1.72),(x+s*.068,-.15,1.84)],.008,'Iron')
 cylinder('Rounded D-grip crosspiece',(x,-.15,1.837),.017,.145,'Timber',(1,0,0),segments=16)
 for s in [-1,1]:bolt((x+s*.070,-.15,1.837),.009,(s,0,0))
 # Rake head: swept neck socket and forged curved square-section teeth.
 tube('Rake socket and arched tang',[(0,-.15,.46),(0,-.15,.33),(0,-.19,.28),(0,-.205,.245)],.020,'Iron')
 box('Forged rake crossbar',(0,-.205,.247),(.355,.035,.037),'Iron',.006)
 for i in range(11):
  x=-.16+i*.032
  tube('Curved individual rake tine',[(x,-.205,.25),(x,-.213,.20),(x,-.248,.13),(x,-.266,.109)],.007,'Iron',sides=8)
 # Push broom: distinct wood back, splayed multi-row fibre bundles with irregular ends.
 box('Broom beech head',(.47,-.16,.273),(.36,.10,.045),'Timber',.009)
 for row in range(4):
  for i in range(17):
   x=.305+i*.0205;y=-.194+row*.023;z=.112+.008*math.sin(i*2.4+row*1.1)
   tube('Splayed stiff broom bristles',[(x,y,.252),(x+(x-.47)*.075,y+(y+.16)*.3,z)],.0083,'Canvas',sides=5)
 tube('Broom handle socket',[(.47,-.15,.43),(.47,-.15,.32),(.47,-.16,.29)],.022,'Iron')
 for s in [-1,1]:
  beam('Broom socket brace',(.47,-.15,.36),(.47+s*.095,-.16,.298),.013,.014,'Iron')
  bolt((.47+s*.10,-.16,.298),.007,(0,0,1))
 G.ASSETS[G.CURRENT]['displayedTools']=['D-grip spade','rake','push broom']

def wheel_handles():
 G.group('WheelbarrowSteel_1930')
 for o in list(G.GROUPS[G.CURRENT].objects):
  if o.name.startswith(('Hardwood load runner','Raised hardwood handle','Rounded hand grip','Tray saddle bracket')):bpy.data.objects.remove(o,do_unlink=True)
 for side in [-1,1]:
  pts=[(side*.22,-.63,.29),(side*.22,-.40,.327),(side*.22,.15,.414),(side*.23,.43,.46),(side*.25,.63,.56),(side*.28,.79,.67),(side*.29,.92,.749),(side*.29,1.02,.785),(side*.29,1.037,.787)]
  oval_handle('Continuous shaped hardwood runner and handgrip',pts,[.022,.025,.025,.025,.024,.019,.0155,.0155,.009],[.027,.030,.030,.030,.027,.021,.017,.017,.009],'Timber',16)
  for y in [-.24,.25]:
   z=.29+(y+.63)*(.17/1.06)
   box('Tray saddle bracket',(side*.22,y,(z+.495)/2),(.074,.048,.495-z),'Iron',.003)
   # Existing tray bolts at z=.502 remain; do not duplicate their washers.

def gates(right=False):
 # Properly authored handed versions: same +Y yard face in the CLOSED configuration.
 name='FenceGateWire_180_RH_1930' if right else 'FenceGateWire_180_1930'
 if right:
  asset(name,'Fences','Ground',1.9);G.COLLIDERS[name].append(dict(name='Gate',position=[0,0,.92],size=[1.9,.1,1.84]))
 else:reset(name)
 from fidelity import gate
 gate()
 for o in list(G.GROUPS[name].objects):
  if o.name.startswith(('Latch backplate','Latch pull','Sliding latch bolt')) or (o.name.startswith(('Washer','Hex bolt')) and o.location.x>.6):bpy.data.objects.remove(o,do_unlink=True)
 # Two graspable round return pulls; primary mechanism on yard (+Y), simple pull outside.
 for face in [-1,1]:
  box('Gate pull mounting strap',(.78,face*.035,1.025),(.07,.013,.235),'Iron',.008)
  tube('Rounded return gate pull',[(.78,face*.046,.935),(.78,face*.078,.945),(.78,face*.095,.97),(.78,face*.095,1.085),(.78,face*.078,1.11),(.78,face*.046,1.115)],.009,'Iron')
  for z in [.925,1.125]:bolt((.78,face*.047,z),.007,(0,face,0))
 if right:
  box('Latch case mounting plate',(.83,.042,1.16),(.13,.016,.085),'Iron',.005)
  tube('Sliding round latch bolt',[(.70,.077,1.17),(.98,.077,1.17)],.009,'Galvanized')
  for x in [.75,.87]:tube('Round bolt guide sleeve',[(x-.018,.077,1.17),(x+.018,.077,1.17)],.014,'Iron',wall=.004)
  tube('Lift and slide latch thumb lever',[(.76,.077,1.17),(.76,.128,1.17),(.76,.141,1.195),(.76,.141,1.23)],.0075,'Iron')
  ball('Thumb lever rounded end',(.76,.141,1.23),(.010,.010,.013),'Iron')
 else:
  # Passive leaf receives the opposing bolt and can be secured by its vertical drop bolt.
  box('Keeper backing plate',(.84,.045,1.16),(.10,.018,.10),'Iron',.005)
  tube('Bolt keeper eye',[(.81,.055,1.14),(.81,.094,1.14),(.90,.094,1.14),(.90,.094,1.195),(.81,.094,1.195),(.81,.055,1.195)],.009,'Iron')
  tube('Raised gate drop bolt',[(.79,.065,.14),(.79,.065,.73),(.79,.11,.76),(.79,.13,.76)],.011,'Galvanized')
  for z in [.23,.60]:
   box('Drop bolt bracket',(.79,.037,z),(.074,.016,.065),'Iron',.004)
   tube('Drop bolt guide',[(.79,.065,z-.02),(.79,.065,z+.02)],.017,'Iron',wall=.005)
 if right:
  # Bake handedness into geometry; instances remain positive scale. Do not reverse the yard face.
  # Blender defers matrix evaluation after scale assignment (notably the last latch cap).
  bpy.context.view_layer.update()
  for o in G.GROUPS[name].objects:
   o.data.transform(o.matrix_world);o.matrix_world=Matrix.Identity(4)
   for v in o.data.vertices:v.co.x=-v.co.x
   bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
  bpy.context.view_layer.update()
  cap=next(o for o in G.GROUPS[name].objects if o.name.startswith('Thumb lever rounded end'))
  assert max(cap.dimensions)<.03, ('Gate latch cap lost its authoring scale',tuple(cap.dimensions))
 G.ASSETS[name]['anchors']=[dict(name='HingeAxis',position=[.84 if right else -.84,0,0]),dict(name='Latch',position=[-.88 if right else .88,0,1.17]),dict(name='YardFace',position=[0,.2,1])]
 G.ASSETS[name]['construction']={'hand':'right' if right else 'left','yardFaceSource':'+Y','closedLeafFront':'-Y','role':'active sliding bolt' if right else 'passive keeper and drop bolt'}

def fence_posts():
 # One authoritative post at each installed junction; panels no longer double their shared posts.
 for name in list(G.ASSETS):
  if not name.startswith('FenceWire_'):continue
  G.group(name)
  for o in list(G.GROUPS[name].objects):
   if o.name.startswith(('Tubular post','Rounded post cap')):bpy.data.objects.remove(o,do_unlink=True)
  length=int(name.split('_')[1])/100
  G.ASSETS[name]['anchors']=[dict(name='StartPost',position=[-length/2,0,0]),dict(name='EndPost',position=[length/2,0,0])]
  G.ASSETS[name]['construction']={'assembly':'rail and tensioned mesh section','requires':'one shared FencePostTubular_1930 at each unique junction; gate hinge post may supply it'}
 asset('FencePostTubular_1930','Fences','Ground',.10)
 cylinder('Tubular service fence post',(0,0,1),.038,2,'Steel',segments=24);ball('Rounded post cap',(0,0,2),(.045,.045,.027),'Steel')
 G.COLLIDERS[G.CURRENT].append(dict(name='Fence post',position=[0,0,1],size=[.08,.08,2]))

def outlet():
 # Pressed open trough with an actual circular drain aperture and a drawn hollow throat.
 G.group('GutterHalfRound_040_1930')
 for o in list(G.GROUPS[G.CURRENT].objects):
  if o.name.startswith(('Outlet catchbox','Hollow outlet socket','Hollow open gutter')):bpy.data.objects.remove(o,do_unlink=True)
 v=[];M=25
 for x in [-.20,.20]:
  for r in [.092,.086]:
   for i in range(M):a=math.pi+i*math.pi/(M-1);v.append((x,r*math.cos(a),r*math.sin(a)))
 f=[]
 for i in range(M-1):
  f.extend([(i,i+1,i+1+2*M,i+2*M),(M+i,3*M+i,3*M+i+1,M+i+1),
            (i,M+i,M+i+1,i+1),(2*M+i,2*M+i+1,3*M+i+1,3*M+i)])
 for i in [0,M-1]:f.append((i,i+2*M,i+3*M,i+M))
 trough=mesh('Hollow trough with circular outlet aperture',v,f,'Galvanized')
 cut=cylinder('temporary drain bore',(0,0,-.075),.050,.16,'Black',segments=48)
 bpy.context.view_layer.objects.active=trough;m=trough.modifiers.new('Real open drain aperture','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cut;bpy.ops.object.modifier_apply(modifier=m.name);bpy.data.objects.remove(cut,do_unlink=True)
 N=48;v=[];levels=[(None,.054,.004),(-.12,.054,.004),(-.16,.047,.004),(-.25,.047,.004)]
 for z,r,t in levels:
  for ri,rad in enumerate([r,r-t]):
   for i in range(N):
    a=i*math.tau/N;y=rad*math.sin(a)
    zz=-math.sqrt((.092 if ri==0 else .086)**2-y*y) if z is None else z
    v.append((rad*math.cos(a),y,zz))
 f=[]
 for j in range(len(levels)-1):
  for ringid in [0,1]:
   for i in range(N):a=j*N*2+ringid*N+i;b=j*N*2+ringid*N+(i+1)%N;f.append((a,b,b+N*2,a+N*2))
 for j in [0,len(levels)-1]:
  for i in range(N):a=j*N*2;f.append((a+i,a+(i+1)%N,a+N+(i+1)%N,a+N+i))
 o=mesh('Drawn round open gutter outlet',v,f,'Galvanized')
 for p in o.data.polygons:p.use_smooth=True


def telephone():
 reset('TelephoneDesk_1930')
 # Oval desk set base, raised rear cradle, front rotary face clear of its shell.
 N=48;v=[];levels=[(.007,.11,.13),(.014,.126,.143),(.050,.126,.143),(.072,.105,.13)]
 for z,rx,ry in levels:
  for i in range(N):a=i*math.tau/N;v.append((rx*math.cos(a),ry*math.sin(a),z))
 f=[tuple(range(N-1,-1,-1)),tuple((len(levels)-1)*N+i for i in range(N))]
 for j in range(len(levels)-1):
  for i in range(N):f.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
 o=mesh('Moulded oval desk telephone base',v,f,'Black')
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 ring('Base moulding seam',(0,0,.012),.12,.0012,'Iron')
 lathe('Flared cradle pedestal',(0,.050,.056),[(0,.052),(.008,.052),(.024,.037),(.060,.033),(.086,.036),(.096,.049),(.103,.049)],'Black',48)
 # Broad cradle saddle and four short hook fingers support the necks, not long dangling arms.
 box('Cradle saddle',(0,.05,.169),(.18,.045,.019),'Black',.008)
 for x in [-.084,.084]:
  for y in [.029,.071]:
   tube('Cradle hook finger',smooth_path([(x,y,.165),(x,y,.195),(x,y,.224),(x*.97,y,.229)]),.006,'Black',sides=16)
  cylinder('Spring hook switch',(x,.05,.208),.008,.026,'Iron')
 # Tube frames follow the curve; cross section remains a comfortable 35 mm, not a bar.
 tube('Moulded handset bow',smooth_path([(-.145,.05,.214),(-.12,.05,.230),(-.08,.05,.238),(0,.05,.247),(.08,.05,.238),(.12,.05,.230),(.145,.05,.214)],8),.018,'Black',sides=24)
 for x in [-.145,.145]:
  r=.045 if x<0 else .048
  lathe('Moulded earpiece' if x<0 else 'Moulded transmitter cup',(x,.05,.169),[(0,r*.75),(.003,r),(.010,r),(.012,r*.95),(.029,r*.92),(.043,r*.61),(.048,r*.36)],'Black',48)
  ring('Threaded cup cap seam',(x,.05,.180),r*.96,.0012,'Iron')
  cylinder('Recessed acoustic face',(x,.05,.169),r*.70,.002,'Iron',segments=32)
  for i in range(12):
   a=i*math.tau/12;cylinder('Receiver acoustic perforation',(x+.022*math.cos(a),.05+.022*math.sin(a),.1675),.0022,.0015,'Black',segments=8)
 # The sloping dial housing connects to the base and stays clear of the rear pedestal.
 v=[]
 for upper in [False,True]:
  for i in range(48):
   a=i*math.tau/48;yy=.068*math.sin(a)
   v.append((.068*math.cos(a),-.071+yy,.096+yy*math.tan(math.radians(18)) if upper else .064))
 f=[tuple(range(47,-1,-1)),tuple(range(48,96))]+[(i,(i+1)%48,(i+1)%48+48,i+48) for i in range(48)]
 mesh('Sloped dial housing',v,f,'Black')
 # An actual pierced dial, seated above the sloped face. Small holes are not raised buttons.
 before=set(G.GROUPS[G.CURRENT].objects)
 cylinder('Dial backing',(0,0,0),.067,.006,'Black',segments=48)
 dial=cylinder('Pierced rotary finger plate',(0,0,.006),.062,.006,'Black',segments=64)
 for i in range(10):
  a=math.radians(40+i*27);cut=cylinder('temporary finger opening',(.045*math.cos(a),.045*math.sin(a),.006),.0082,.04,'Black',segments=16)
  bpy.context.view_layer.objects.active=dial;m=dial.modifiers.new('Finger aperture','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cut;bpy.ops.object.modifier_apply(modifier=m.name);bpy.data.objects.remove(cut,do_unlink=True)
 cylinder('Central dial label',(0,0,.011),.025,.002,'PaperLight',segments=32)
 ring('Dial card retaining bezel',(0,0,.013),.027,.0018,'Brass')
 # Porcelain number ring with true pierced finger plate above it.
 for i in range(10):
  a=math.radians(40+i*27);xx=.045*math.cos(a);yy=.045*math.sin(a)
  cylinder('Dial number porcelain inset',(xx,yy,.0032),.008,.001,'PaperLight',segments=16)
  curve=bpy.data.curves.new('Rotary numeral','FONT');curve.body=str((i+1)%10);curve.align_x='CENTER';curve.align_y='CENTER';curve.size=.008;curve.extrude=0
  o=bpy.data.objects.new('Dial numeral',curve);G.GROUPS[G.CURRENT].objects.link(o);o.location=(xx,yy,.004);curve.materials.append(G.MATS['Black'])
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o.select_set(False)
 ring('Number ring bezel',(0,0,.008),.065,.0025,'Iron')
 tube('Dial finger stop',[(.052,-.032,0),(.057,-.037,.014),(.044,-.040,.022)],.0035,'Iron',sides=8)
 pose=Matrix.Translation(Vector((0,-.071,.100)))@Matrix.Rotation(math.radians(18),4,'X')
 bpy.context.view_layer.update()
 for o in set(G.GROUPS[G.CURRENT].objects)-before:o.matrix_world=pose@o.matrix_world
 tube('Fabric handset cord',smooth_path([(.17,.07,.201),(.199,.095,.165),(.202,.145,.075),(.18,.205,.010),(.09,.22,.006),(.026,.173,.010),(.042,.125,.047)],10),.0035,'Black',sides=12)
 tube('Handset cord strain relief',[(.17,.07,.201),(.183,.077,.187)],.007,'Black',sides=16)
 for x in [-.074,.074]:
  for y in [-.075,.08]:cylinder('Rubber telephone foot',(x,y,.006),.013,.009,'Black',segments=20)

def awning_supports():
 G.group('AwningCurved_164_1930')
 for o in list(G.GROUPS[G.CURRENT].objects):
  if o.name.startswith('Hood bracket'):bpy.data.objects.remove(o,do_unlink=True)
  # Mounts sit outside the personnel-frame architraves, under the existing hood.
  elif o.name.startswith('Wall mounting plate') or o.name.startswith(('Washer','Hex bolt')):
   if abs(abs(o.location.x)-.74)<.001:o.location.x=math.copysign(.785,o.location.x)
 for x in [-.785,.785]:
  beam('Connected hood brace',(x,.0,-.27),(x,-.808,.189),.037,.037,'Steel')
  # Formed bearing strap follows the INNER sheet face. A round tube centred
  # near the outer skin (and the old high brace end) pierced the curved hood.
  vertices=[]
  for depth in [.018,.030]:
   for dx in [-.026,.026]:
    for y in [-.865,-.82,-.775]:
     z=.48*math.sqrt(1-(y/.93)**2)
     normal=Vector((0,y/(.93*.93),z/(.48*.48))).normalized()
     vertices.append(tuple(Vector((x+dx,y,z))-normal*depth))
  faces=[]
  for i in range(2):
   faces.extend([(i,i+1,i+4,i+3),(i+6,i+9,i+10,i+7),
                 (i,i+6,i+7,i+1),(i+3,i+4,i+10,i+9)])
  faces.extend([(0,3,9,6),(2,8,11,5)])
  mesh('Hood brace bearing saddle',vertices,faces,'Steel')

def banker_lamp():
 reset('DeskLampBanker_1930')
 # Distinct glass finishes, shared by future lamps; no baked lighting in the material.
 for name,c,rough in [('LampGreenGlass',[.018,.10,.043],.19),('LampOpalGlass',[.73,.70,.61],.26)]:
  G.PALETTE[name]=(c,0,rough,'')
  mat=bpy.data.materials.get(name) or bpy.data.materials.new(name);mat.use_nodes=True;mat.diffuse_color=(*c,1)
  bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=rough;G.MATS[name]=mat
 lathe('Spun stepped brass lamp foot',(0,0,0),[(.004,.085),(.009,.103),(.018,.106),(.025,.101),(.031,.087),(.038,.065),(.043,.032),(.047,.025)],'Brass',64)
 ring('Foot rolled rim',(0,0,.020),.103,.002,'Brass')
 lathe('Turned stem ferrule',(0,0,.041),[(0,.024),(.008,.024),(.012,.019),(.027,.017),(.029,.019),(.034,.019),(.039,.012)],'Brass',32)
 cylinder('Brass lamp stem',(0,0,.18),.009,.216,'Brass',segments=32)
 lathe('Yoke stem collar',(0,0,.283),[(0,.014),(.006,.018),(.016,.018),(.020,.012)],'Brass',32)
 # Supports travel around the closed shade ends and meet its actual pivot bosses.
 for side in [-1,1]:
  pts=smooth_path([(0,0,.302),(side*.072,0,.309),(side*.14,0,.333),(side*.171,-.01,.365),(side*.171,-.035,.402)],6)
  tube('Curved brass shade yoke',pts,.006,'Brass',sides=16)
  cylinder('Shade pivot spindle',(side*.162,-.035,.402),.008,.030,'Brass',(1,0,0),segments=24)
  cylinder('Shade tilt friction boss',(side*.174,-.035,.402),.013,.010,'Brass',(1,0,0),segments=32)
  ball('Domed tilt screw',(side*.181,-.035,.402),(.004,.012,.012),'Brass')
  box('Tilt screw slot',(side*.1848,-.035,.402),(.0006,.014,.0015),'Iron',0)
 # Green outer glass and opal inner skin, rolled lip, genuinely closed curved end cheeks.
 NX=17;NA=33;v=[]
 for inner in [False,True]:
  for j in range(NX):
   x=-.157+j*.314/(NX-1);rounding=math.sqrt(max(0,1-(abs(x)/.167)**8))
   for i in range(NA):
    a=i*math.pi/(NA-1);ry=.085-(.004 if inner else 0);rz=.080-(.004 if inner else 0)
    v.append((x,-.035+ry*math.cos(a),.384+rz*math.sin(a)*rounding))
 outer=[];inside=[];offset=NX*NA
 for j in range(NX-1):
  for i in range(NA-1):
   k=j*NA+i;outer.append((k,k+1,k+NA+1,k+NA));inside.append((offset+k,offset+k+NA,offset+k+NA+1,offset+k+1))
 outer_shell=mesh('Emerald glass outer shade',v[:offset],outer,'LampGreenGlass')
 inner_shell=mesh('Opal glass shade lining',v[offset:],[[k-offset for k in f] for f in inside],'LampOpalGlass')
 for o in [outer_shell,inner_shell]:
  for p in o.data.polygons:p.use_smooth=True
 for side in [-1,1]:
  j=0 if side<0 else NX-1
  # Filled end, with a small flat lower lip; yoke attaches through this wall.
  points=[v[j*NA+i] for i in range(NA)]+[(side*.157,-.12,.384)]
  mesh('Closed moulded glass shade end',points,[tuple(range(len(points)-1))],'LampGreenGlass')
  end_curve=.076*math.sqrt(1-(.157/.167)**8)
  pts=[(side*.157,-.035+.081*math.cos(i*math.pi/32),.384+end_curve*math.sin(i*math.pi/32)) for i in range(33)]
  mesh('White inner end cheek',[(p[0]-side*.003,p[1],p[2]) for p in pts],[tuple(range(33))],'LampOpalGlass')
 for edge in [-1,1]:tube('Rounded glass shade lip',[(-.157,-.035+edge*.083,.384),(.157,-.035+edge*.083,.384)],.003,'LampGreenGlass',sides=12)
 # Socket and a pear-shaped bulb stay within the shade, unlike the exposed old cylinder.
 cylinder('Porcelain lamp socket',(-.092,-.035,.405),.020,.049,'LampOpalGlass',(1,0,0),segments=32)
 cylinder('Bulb threaded base',(-.060,-.035,.405),.012,.024,'Brass',(1,0,0),segments=24)
 bulb=lathe('Frosted pear bulb',(0,0,0),[(0,.010),(.010,.011),(.025,.019),(.038,.025),(.051,.026),(.063,.021),(.071,.010),(.073,.001)],'LampOpalGlass',32)
 bulb.rotation_euler=(0,math.pi/2,0);bulb.location=(-.054,-.035,.405)
 tube('Socket retaining stem',[(-.147,-.035,.405),(-.119,-.035,.405)],.005,'Brass',sides=12)
 for i in range(25):ball('Pull chain bead',(-.10,-.031,.389-i*.0035),(.0018,.0018,.0018),'Brass')
 lathe('Pull chain acorn',(-.10,-.031,.291),[(0,.001),(.003,.005),(.009,.005),(.012,.002)],'Brass',16)
 tube('Braided desk lamp supply',smooth_path([(0,.064,.023),(.020,.10,.008),(.085,.125,.005),(.16,.11,.005)],8),.0032,'Black',sides=10)
 tube('Cord foot grommet',[(0,.064,.023),(0,.082,.019)],.005,'Black')

def hand_plane():
 G.group('ToolTrayCarpenter_1930')
 for o in list(G.GROUPS[G.CURRENT].objects):
  if o.name.startswith('Plane '):bpy.data.objects.remove(o,do_unlink=True)
 y=.067
 box('Plane ground steel sole',(-.015,y,.043),(.285,.082,.012),'Galvanized',.005)
 for side in [-1,1]:
  bevel(profile('Plane cast side cheek',[(-.157,.048),(.128,.048),(.128,.064),(.09,.07),(.033,.09),(-.047,.093),(-.125,.073),(-.157,.065)],.006,'Iron',y+side*.038),.002)
 closed_grip('Plane curved rear tote',-.105,y,.111,.033,.056,.016,.033,.023,'TimberDark',.20)
 box('Plane tote mounting foot',(-.113,y,.058),(.064,.043,.017),'TimberDark',.004)
 lathe('Plane turned front knob',(.082,y,.05),[(0,.018),(.01,.015),(.025,.011),(.042,.02),(.055,.022),(.062,.015)],'TimberDark',24)
 bevel(profile('Plane angled cutting iron',[(.021,.05),(.025,.05),(-.025,.123),(-.032,.123)],.059,'Galvanized',y),.001)
 bevel(profile('Plane lever cap',[(.012,.067),(.021,.071),(-.014,.119),(-.027,.12)],.046,'Iron',y),.002)
 cylinder('Plane lever cap screw',(-.011,y,.112),.006,.015,'Brass',(.8,0,.6),segments=12)
 cylinder('Plane brass depth wheel',(-.059,y,.094),.014,.009,'Brass',(1,0,0),segments=24)
 tube('Plane blade depth adjuster',[(-.061,y,.094),(-.03,y,.081)],.004,'Brass',sides=8)

def refine():
 carpenter();grounds();wheel_handles();gates();gates(True);fence_posts();outlet();telephone();awning_supports();hand_plane();banker_lamp()
