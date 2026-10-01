"""Detail the approved massing in memory; preserve the approved blend and exports byte-for-byte."""
import bpy,bmesh,pathlib,sys,json,math,hashlib,random
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'ArtSource/PeriodEnvironment1930'))
import geometry as G
from geometry import box,beam,tube,cylinder,text,mesh
SOURCE=HERE/'StudioServices_Massing.blend';source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));G.GROUPS.clear();G.init_materials()
manifest=json.loads((ROOT/'ArtExports/StudioServices/Massing/massing_manifest.json').read_text())
placements=[]
def place(name,group,p,angle=0,existing=False):
 placements.append(dict(asset=name,group=group,position=[p[0],p[2],p[1]],yaw=180-angle,existing=existing))
def clear_group(name):
 c=bpy.data.collections.get(name)
 if c:
  for o in list(c.objects):bpy.data.objects.remove(o,do_unlink=True)
def delete_prefix(prefixes):
 for o in list(bpy.data.objects):
  if o.name.startswith(tuple(prefixes)):bpy.data.objects.remove(o,do_unlink=True)
for c in bpy.context.scene.collection.children:G.GROUPS[c.name]=c
for n in ['InteriorProps','ExteriorProps','YardProps','ScaleStudies','WorkshopDoors','OfficeDoor','YardDoor','EmploymentBoard','OfficeAwning','EmploymentCanopy']:clear_group(n)
delete_prefix(['Window ','Office door','Workshop jamb','Workshop header','Yard fence','Fence rail','Lumber rack','Skylight','Low hipped roof','Shelter metal roof','Roof edge','Parapet field','Parapet coping','Parapet step'])
# The unique masonry, room layout, roof outline and structural shelter remain from the approved source.
for o in bpy.data.objects:
 if o.type=='MESH':
  for i,m in enumerate(o.data.materials):
   if m.name in G.MATS:o.data.materials[i]=G.MATS[m.name]
 # Keep the approved roof envelope; set its framing fully below the sheet gauge.
 if o.name.startswith(('Shelter rafter','Shelter outer beam','Shelter knee brace')):o.location.z-=.10

G.group('FrontWall')
# One closed stepped masonry profile replaces abutting parapet boxes. Same outline.
outline=[(-7,4.22),(7,4.22),(7,4.61),(4.29,4.61),(4.29,5.19),(4.2,5.19),(4.2,5.20),(-4.2,5.20),(-4.2,5.19),(-4.29,5.19),(-4.29,4.61),(-7,4.61)]
vv=[(x,y,z) for y in [-4.51,-4.21] for x,z in outline];N=len(outline)
mesh('Continuous stepped sign parapet',vv,[tuple(range(N-1,-1,-1)),tuple(range(N,N*2))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)],'WarmStucco')
# Continuous folded coping follows the step with closed return risers, no open seams.
for x0,x1,z in [(-7.03,-4.20,4.61),(-4.23,4.23,5.20),(4.20,7.03,4.61)]:
 box('Dressed parapet coping',((x0+x1)/2,-4.36,z+.075),(x1-x0,.42,.15),'LimestoneTrim',.006)
for x in [-4.23,4.23]:
 box('Coping step return',(x,-4.36,4.975),(.12,.42,.60),'LimestoneTrim',.004)
# Exposed brick piers and restrained irregular patches, with mortar joints rather than flat red stripes.
rng=random.Random(1930)
def brick_face(cx,y,z0,w,h,patch=False):
 for row in range(int(h/.082)):
  for col in range(int(w/.245)+2):
   left=cx-w/2+col*.245-(.12 if row%2 else 0);right=min(cx+w/2,left+.23);left=max(left,cx-w/2)
   if right-left<.035 or (patch and (col==0 or col==int(w/.245)) and rng.random()<.43):continue
   mat=rng.choice(['Brick']*5+['BrickLight','BrickDark'])
   box('Brick course',((left+right)/2,y,z0+row*.082+.034),(right-left,.019,.067),mat,.003)
brick_face(-6.78,-4.617,.08,.44,4.1);brick_face(6.78,-4.617,.08,.44,4.1)
for cx,z,w,h in [(-3.22,.07,.54,1.82),(.29,.08,.63,2.87),(-5.1,2.73,.65,.42),(.85,3.58,.7,.24)]:brick_face(cx,-4.514,z,w,h,True)
for x in [1.24,6.31]:
 box('Workshop brick jamb',(x,-4.52,1.75),(.18,.28,3.5),'Mortar')
 brick_face(x,-4.67,.03,.18,3.49)
box('Workshop lintel',(3.775,-4.51,3.57),(5.24,.30,.21),'LimestoneTrim')

# Separate reusable condition overlays; clean base materials remain shared.
# Existing project patina textures have transparent edges, no baked lighting.
def patina(group,axis,const,a,b,z,h,reverse=False,runoff=False):
 G.group(group)
 points=[(a,z),(b,z),(b,z+h),(a,z+h)]
 verts=[(x,const,y) if axis=='X' else(const,x,y) for x,y in points]
 o=mesh('Localized runoff' if runoff else 'Entrance and base patina',verts,[(3,2,1,0) if reverse else (0,1,2,3)],'RunoffPatina' if runoff else 'ContactPatina')
 uv=o.data.uv_layers.new(name='OverlayUV');coords=[(0,0),(1,0),(1,1),(0,1)]
 for loop in o.data.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
 o['explicit_uv']=True
for a,b in [(-6.50,-4.79),(-3.51,1.18)]:patina('FrontWall','X',-4.533,a,b,.035,.42)
patina('RearWall','X',4.511,-6.53,6.53,.035,.44,True)
patina('LeftWall','Y',-7.012,-4.2,4.2,.035,.42,True)
for a,b in [(-4.2,-2.48),(-.82,4.2)]:patina('RightWall','Y',7.012,a,b,.035,.42)
for x in [-5.95,-3.04,.55,5.75]:patina('FrontWall','X',-4.534,x-.24,x+.24,2.85,.66,False,True)
for x in [-4.4,2.25]:patina('RearWall','X',4.512,x-.90,x+.9,.90,.28,True,True)

# Finished interior faces and caps. Faces belong to the same cutaway group as their wall.
def finish_wall(axis,const,length,height,holes,group,normal):
 G.group(group);xs=sorted(set([-length/2,length/2]+[v for h in holes for v in h[:2]]));zs=sorted(set([0,height]+[v for h in holes for v in h[2:]]))
 for a,b in zip(xs,xs[1:]):
  for c,d in zip(zs,zs[1:]):
   if any(h[0]<(a+b)/2<h[1] and h[2]<(c+d)/2<h[3] for h in holes):continue
   p=((a+b)/2,const,(c+d)/2) if axis=='X' else(const,(a+b)/2,(c+d)/2);s=(b-a,.012,d-c) if axis=='X' else(.012,b-a,d-c)
   box('Interior lime plaster',p,s,'InteriorPlaster',0)
  # Skirting must stop at every floor-level doorway.
  if not any(h[0]<(a+b)/2<h[1] and h[2]==0 for h in holes):
   p=((a+b)/2,const+normal*.022,.09) if axis=='X' else(const+normal*.022,(a+b)/2,.09)
   s=(b-a,.045,.18) if axis=='X' else(.045,b-a,.18);box('Interior skirting',p,s,'TimberDark',.004)
 for a,b,c,d in holes:
  for edge in [a,b]:
   p=(edge,const+normal*.025,(c+d)/2) if axis=='X' else(const+normal*.025,edge,(c+d)/2)
   s=(.073,.045,d-c+.12) if axis=='X' else(.045,.073,d-c+.12);box('Interior opening architrave',p,s,'Timber',.006)
  p=((a+b)/2,const+normal*.025,d+.037) if axis=='X' else(const+normal*.025,(a+b)/2,d+.037)
  s=(b-a+.12,.045,.075) if axis=='X' else(.045,b-a+.12,.075);box('Interior opening head',p,s,'Timber',.006)
 # Wall tops remain capped when the roof is hidden.
 p=(0,const,height-.035) if axis=='X' else(const,0,height-.035);s=(length,.30,.075) if axis=='X' else(.30,length,.075)
 box('Continuous wall cap',p,s,'LimestoneTrim')
finish_wall('X',-4.21,13.44,4.14,[(-4.75,-3.55,0,2.62),(-2.50,-.55,.83,2.95),(1.35,6.2,0,3.46)],'FrontWall',1)
finish_wall('X',4.21,13.44,4.10,[(-5.4,-3.4,1.3,3.1),(.7,3.8,1.15,3.1)],'RearWall',-1)
finish_wall('Y',6.71,8.44,4.10,[(-2.45,-.85,0,2.7),(.45,3.1,1.1,3.15)],'RightWall',-1)
finish_wall('Y',-6.71,8.44,4.10,[(-2.9,-.9,1,3.1),(1.5,3.4,1.3,3.1)],'LeftWall',1)
G.group('InteriorPartitions')
box('Office partition cap',(-.25,0,3.30),(.22,8.44,.09),'Timber')
box('Store partition cap',(-3.62,.30,3.30),(6.45,.22,.09),'Timber')
for x in [-.35,-.15]:
 for cy,ln in [(-2.71,3.02),(2.16,4.12)]:box('Partition skirting',(x,cy,.09),(.042,ln,.18),'TimberDark')
 for y in [-1.2,.1]:box('Passage jamb',(x,y,1.27),(.045,.10,2.54),'Timber')
 box('Passage header',(x,-.55,2.55),(.045,1.48,.12),'Timber')
for y in [.195,.405]:
 for x in [-4.88,-3.5]:box('Store passage jamb',(x,y,1.27),(.10,.042,2.54),'Timber')
 box('Store passage header',(-4.19,y,2.55),(1.54,.042,.12),'Timber')
 for x,w in [(-5.85,1.94),(-1.94,3.08)]:box('Store skirting',(x,y,.09),(w,.042,.18),'TimberDark')
G.group('Floor')
box('Office linoleum finish',(-3.525,-2.0,.006),(6.36,4.30,.012),'OfficeFloor',0)
# Modest expansion joints distinguish workshop floor bays, terminate at room boundary.
for y in [-2,1]:box('Floor control joint',(3.24,y,.001),(6.8,.009,.002),'Mortar',0)
for x in [2.2,4.5]:box('Floor control joint',(x,0,.001),(.009,8.42,.002),'Mortar',0)

# Native reusable window/door assemblies, correct actual opening dimensions.
for n,p,a,g in [('195x212',(-1.525,-4.43,.83),0,'FrontWall'),('200x180',(-4.4,4.43,1.3),180,'RearWall'),('310x195',(2.25,4.43,1.15),180,'RearWall'),('265x205',(6.93,1.775,1.1),90,'RightWall'),('200x210',(-6.93,-1.9,1),-90,'LeftWall'),('190x180',(-6.93,2.45,1.3),-90,'LeftWall')]:place('WindowSteel_'+n+'_1930',g,p,a)
place('DoorFrameTimber_120x262_1930','FrontWall',(-4.15,-4.36,0))
place('DoorGlazedTimber_110x258_1930','OfficeDoor',(-4.65,-3.91,0),90)
place('DoorUtilityTimber_150x260_1930','YardDoor',(6.60,-.10,0),90)
place('ServiceDoorPair_485x346_1930','WorkshopDoors',(3.775,-4.48,0))
# The canonical porch remains in the kit. This entrance uses the curved hood only.
place('AwningCurved_164_1930','OfficeAwning',(-4.15,-4.5265,2.73))
place('NoticeBoardTimber_173_1930','EmploymentBoard',(-5.88,-4.6,1.105))
G.group('EmploymentBoard');text('Employment heading','EMPLOYMENT',(-5.88,-4.635,2.01),.115,1.45,'TimberDark')
for i,(dx,dz) in enumerate([(-.55,0),(-.18,.04),(.19,-.03),(.55,.01)]):
 box('Replaceable hiring notice',(-5.88+dx,-4.633,1.53+dz),(.27+(i%2)*.015,.003,.36+(i%3)*.018),'PaperLight' if i%2 else 'Paper',.001)
 text('Notice heading',['WORK','GROUNDS','REPAIRS','HIRING'][i],(-5.88+dx,-4.636,1.65+dz),.032,.25,'Black')
 for j in range(4+i%3):box('Notice typesetting',(-5.88+dx,-4.636,1.56+dz-j*.024),(.21-((j+i)%3)*.025,.001,.003),'TimberDark',0)
 cylinder('Notice tack',(-5.88+dx,-4.64,1.69+dz),.007,.011,'Brass',(0,1,0))

# Roof panels around actual skylight holes. Clipping follows approved hip envelope exactly.
G.group('Roof')
def clip(poly,fn):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  fa,fb=fn(a),fn(b)
  if fa>=-1e-7:out.append(a)
  if (fa<0)!=(fb<0):t=fa/(fa-fb);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
 return out
def zroof(y):return 4.18+(y+4.1)*.97/4.3
def roof_normals_up(o):
 # Open sheets have no closed-volume inside/outside. Explicit upward normals
 # make Solidify add thickness below the approved roof plane on every panel.
 bm=bmesh.new();bm.from_mesh(o.data);bm.normal_update()
 bmesh.ops.reverse_faces(bm,faces=[f for f in bm.faces if f.normal.z<0])
 bm.to_mesh(o.data);bm.free();o.data.update()
rooflights=[(-3.70,-1.50),(1.40,3.60)]
xs=sorted(set([-6.95,6.95]+[x for pair in rooflights for x in pair]+[round(-6.95+i*.695,4) for i in range(21)]));ys=[-4.1,-2.2,-1,.2]
for xa,xb in zip(xs,xs[1:]):
 for ya,yb in zip(ys,ys[1:]):
  if ya>=-2.2 and yb<=-1 and any(xa>=a and xb<=b for a,b in rooflights):continue
  poly=[(xa,ya),(xb,ya),(xb,yb),(xa,yb)]
  poly=clip(poly,lambda p:p[0]+6.95-(p[1]+4.1)*4.15/4.3);poly=clip(poly,lambda p:6.95-(p[1]+4.1)*4.15/4.3-p[0])
  if len(poly)>=3:
   o=mesh('Front roofing panel',[(x,y,zroof(y)) for x,y in poly],[tuple(range(len(poly)))],'RoofMetal');roof_normals_up(o);m=o.modifiers.new('Roof panel thickness','SOLIDIFY');m.thickness=.075;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
verts=[(-6.95,-4.1,4.18),(6.95,-4.1,4.18),(6.95,4.55,4.18),(-6.95,4.55,4.18),(-2.8,.2,5.15),(2.8,.2,5.15)]
o=mesh('Rear hip roof',verts,[(1,2,5),(2,3,4,5),(3,0,4)],'RoofMetal');roof_normals_up(o);m=o.modifiers.new('Deck thickness','SOLIDIFY');m.thickness=.075;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
for x in [-6.25+i*.69 for i in range(19)]:
 yend=min(.2,-4.1+(6.95-abs(x))*4.3/4.15)
 for a,b in [(-4.1,min(-2.2,yend)),(-1,yend)]:
  if b>a:beam('Standing seam',(x,a,zroof(a)+.025),(x,b,zroof(b)+.025),.023,.025,'Galvanized')
 if not any(a<x<b for a,b in rooflights) and yend>-2.2:
  beam('Continuous roof seam',(x,-2.2,zroof(-2.2)+.025),(x,min(-1,yend),zroof(min(-1,yend))+.025),.023,.025,'Galvanized')
 # The rear slope has no apertures.
 yr=max(.2,4.55-(6.95-abs(x))*4.35/4.15)
 if yr<4.55:beam('Rear roof seam',(x,yr,4.18+(4.55-yr)*.97/4.35+.025),(x,4.55,4.205),.023,.025,'Galvanized')
for side in [-1,1]:
 for y in [-3.7+i*.48 for i in range(17)]:
  edge=6.95-min((y+4.1)/4.3,(4.55-y)/4.35)*4.15
  beam('Hip panel seam',(side*6.95,y,4.205),(side*edge,y,4.205+(6.95-edge)*.97/4.15),.024,.024,'Galvanized')
 for y in [-4.1,4.55]:beam('Hip ridge cap',(side*6.95,y,4.225),(side*2.8,.2,5.195),.10,.035,'RoofMetal')
beam('Ridge cap',(-2.8,.2,5.195),(2.8,.2,5.195),.14,.045,'RoofMetal')
for x0,x1 in rooflights:
 # Each curb is entirely inside the front plane, with upslope saddle and apron.
 assert x0>-6.95+3.1*4.15/4.3 and x1<6.95-3.1*4.15/4.3
 for y,depth in [(-2.25,.20),(-.94,.22)]:
  o=box('Rooflight apron flashing',((x0+x1)/2,y,zroof(y)+.027),(x1-x0+.20,depth,.015),'Galvanized',.002);o.rotation_euler.x=math.atan(.97/4.3)
 for x in [x0-.07,x1+.07]:beam('Rooflight side soaker',(x,-2.24,zroof(-2.24)+.035),(x,-.94,zroof(-.94)+.035),.16,.018,'Galvanized')
 for x in [x0,x1]:beam('Skylight curb',(x,-2.2,zroof(-2.2)+.08),(x,-1,zroof(-1)+.08),.085,.15,'Steel')
 for y in [-2.2,-1]:beam('Skylight curb',(x0,y,zroof(y)+.08),(x1,y,zroof(y)+.08),.085,.15,'Steel')
 mesh('Skylight glazing',[(x0+.04,-2.16,zroof(-2.16)+.16),(x1-.04,-2.16,zroof(-2.16)+.16),(x1-.04,-1.04,zroof(-1.04)+.16),(x0+.04,-1.04,zroof(-1.04)+.16)],[(0,1,2,3)],'Glass')
 for x in [x0+.55,x0+1.1,x0+1.65]:beam('Skylight mullion',(x,-2.2,zroof(-2.2)+.17),(x,-1,zroof(-1)+.17),.035,.04,'Steel')
# Underside deck ribs and rafters are roof-owned, disappear with it.
for y in [-3.5,-1.5,.5,2.5,4.0]:beam('Roof tie',(-6.6,y,3.98),(6.6,y,3.98),.12,.18,'TimberDark')
for x in [-5.8,-3.8,-1.8,.2,2.2,4.2,6.2]:
 beam('Front rafter',(x,-4.1,4.07),(x,0,4.07+min(.91,(6.95-abs(x))*.24)),.075,.14,'Timber')
# Side eave runoff is caught below the edge, with a drip flashing terminating over the open trough.
for side in [-1,1]:
 G.group('Roof')
 for i in range(3):place('GutterHalfRound_200_1930','Roof',(side*7.025,-2.95+i*2,4.08),90)
 place('GutterHalfRound_155_1930','Roof',(side*7.025,2.825,4.08),90)
 place('GutterHalfRound_040_1930','Roof',(side*7.025,3.8,4.08),90)
 place('GutterHalfRound_040Plain_1930','Roof',(side*7.025,4.20,4.08),90)
 beam('Eave drip flashing',(side*6.94,-4.08,4.18),(side*6.94,4.55,4.18),.12,.035,'Galvanized')
 # Entire downpipe/outlet assembly belongs to corresponding wall visibility group.
 g='RightWall' if side>0 else 'LeftWall';G.group(g)
 # Vertical socket entries and a smooth offset; no angled butt joint through the socket lip.
 pts=[(side*7.025,3.8,3.85),(side*7.025,3.8,3.80)]
 for i in range(1,17):
  t=i/16;pts.append((side*(7.025+.075*(3*t*t-2*t*t*t)),3.8,3.80-.23*t))
 pts.extend([(side*7.10,3.8,3.50),(side*7.10,3.8,3.42)])
 tube('Hollow swept gutter offset',pts,.042,'Galvanized',wall=.003,sides=24)
 tube('Downpipe offset lower socket',[(side*7.10,3.8,3.40),(side*7.10,3.8,3.47)],.052,'Galvanized',wall=.004,sides=24)
 place('DownpipeHollow_300_1930',g,(side*7.10,3.8,.43),90 if side>0 else -90)
 place('DownpipeShoe_1930',g,(side*7.10,3.8,.07),90 if side>0 else -90)
 place('SplashBlockConcrete_1930','ExteriorProps',(side*7.38,3.8,0),90 if side>0 else -90)
for y in [-4.1,4.55]:
 for i in range(6):place('GutterHalfRound_200_1930','Roof',(-5.875+i*2,y,4.08))
 place('GutterHalfRound_175_1930','Roof',(6,y,4.08))
for x,y,a in [(7.025,-4.1,0),(-7.025,-4.1,-90),(7.025,4.55,90),(-7.025,4.55,180)]:place('GutterCorner90_1930','Roof',(x,y,4.08),a)
# Shelter covering receives corrugation and perimeter flashing, keeping its approved profile.
G.group('YardShelterRoof')
v=[];f=[];roof_x=[6.88,7.00,7.20,10.82];roof_y=sorted(set([-3.925+i*8.45/340 for i in range(341)]+[3.67,3.93]))
def shelter_z(x,y):return 3.60-(x-6.88)*.716/3.94+.014*math.cos((y+3.925)*340/8.45*math.pi/2)
for x in roof_x:
 for y in roof_y:v.append((x,y,shelter_z(x,y)))
for j in range(len(roof_x)-1):
 for i in range(len(roof_y)-1):
  if j==1 and roof_y[i]>=3.67 and roof_y[i+1]<=3.93:continue
  k=j*len(roof_y)+i;f.append((k,k+1,k+1+len(roof_y),k+len(roof_y)))
o=mesh('Corrugated shelter sheet with flashed pipe opening',v,f,'RoofMetal')
roof_normals_up(o)
m=o.modifiers.new('Sheet gauge','SOLIDIFY');m.thickness=.008;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
v=[]
for outer in [True,False]:
 for i in range(32):
  a=i*math.tau/32;r=.195/max(abs(math.cos(a)),abs(math.sin(a))) if outer else .062
  x=7.10+r*math.cos(a);y=3.8+r*math.sin(a)
  v.append((x,y,shelter_z(x,y)+.021))
o=mesh('Lead apron around downpipe',v,[(i,(i+1)%32,(i+1)%32+32,i+32) for i in range(32)],'Galvanized')
tube('Flashed downpipe collar',[(7.10,3.8,3.535),(7.10,3.8,3.67)],.063,'Galvanized',wall=.008)
tube('Collar weather seal',[(7.10,3.8,3.65),(7.10,3.8,3.68)],.065,'RoofMetal',wall=.018)
for x,z in [(6.91,3.60),(10.78,2.89)]:beam('Shelter edge flashing',(x,-3.93,z),(x,4.51,z),.065,.06,'RoofMetal')
# The lean-to has its own runoff path, supported by the outer beam/front post.
# Shared gutter/shoe/splash modules; only the site-specific cut pipe is unique.
place('GutterHalfRound_040_1930','YardShelterRoof',(10.875,-3.725,2.80),90)
for i in range(4):place('GutterHalfRound_200_1930','YardShelterRoof',(10.875,-2.525+i*2,2.80),90)
for y in [-3.925,4.475]:box('Shelter gutter stop end',(10.875,y,2.757),(.178,.006,.086),'Galvanized',.002)
# A continuous folded eaves apron carries runoff from the sheet into the trough.
# Its upper flange follows the existing roof; its drip turns down inside the gutter.
v=[]
for y in roof_y:
 for x,z in [(10.74,3.60-(10.74-6.88)*.716/3.94-.022),(10.825,3.60-(10.825-6.88)*.716/3.94-.022),(10.872,2.812),(10.872,2.780)]:v.append((x,y,z))
o=mesh('Shelter continuous folded drip apron',v,[(j*4+i,j*4+i+1,(j+1)*4+i+1,(j+1)*4+i) for j in range(len(roof_y)-1) for i in range(3)],'Galvanized')
m=o.modifiers.new('Folded sheet gauge','SOLIDIFY');m.thickness=.0025;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
# External lap straps bridge each gutter joint without blocking its hollow waterway.
for y in [-3.525,-1.525,.475,2.475]:
 v=[]
 for yy in [y-.027,y+.027]:
  for i in range(25):
   a=math.pi+i*math.pi/24;v.append((10.875+.094*math.cos(a),yy,2.80+.094*math.sin(a)))
 o=mesh('Shelter gutter joint lap strap',v,[(i,i+1,i+26,i+25) for i in range(24)],'Galvanized')
 m=o.modifiers.new('Lap strap gauge','SOLIDIFY');m.thickness=.002;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
for y in [-3.5,-2.2,-.9,.4,1.7,3.0,4.2]:
 beam('Shelter gutter bracket',(10.47,y,2.69),(10.875,y,2.70),.027,.035,'Steel')
G.group('YardShelter')
pts=[(10.875,-3.725,2.57),(10.875,-3.725,2.54)]
for i in range(1,25):
 t=i/24;pts.append((10.875-.245*(3*t*t-2*t*t*t),-3.725,2.54-.23*t))
pts.append((10.63,-3.725,2.29))
tube('Shelter hollow swept outlet offset',pts,.042,'Galvanized',wall=.003,sides=24)
tube('Shelter cut-to-length hollow downpipe',[(10.63,-3.725,.43),(10.63,-3.725,2.31)],.047,'Galvanized',wall=.003)
tube('Shelter pipe socket',[(10.63,-3.725,2.28),(10.63,-3.725,2.33)],.052,'Galvanized',wall=.004)
for z in [.64,1.65,2.25]:
 tube('Shelter pipe clamp',[(10.63,-3.725,z-.019),(10.63,-3.725,z+.019)],.054,'Steel',wall=.007)
 beam('Shelter pipe stand-off',(10.535,-3.80,z),(10.63,-3.725,z),.035,.035,'Steel')
 box('Shelter clamp post plate',(10.538,-3.80,z),(.008,.11,.10),'Steel')
place('DownpipeShoe_1930','YardShelter',(10.63,-3.725,.07),90)
place('SplashBlockConcrete_1930','ExteriorProps',(10.91,-3.725,0),90)

# Furniture and organized supplies. All instances reference authoritative independent kit prefabs.
place('DeskPedestal_1930','InteriorProps',(-5.4,-1.45,0),180);place('ChairBentwood_1930','InteriorProps',(-5.4,-.65,0))
place('ChairBentwood_1930','InteriorProps',(-2.1,-.6,0),25);place('FilingCabinet_4Drawer_1930','InteriorProps',(-6.38,-.25,0))
place('ChairBentwood_1930','InteriorProps',(-1.12,-.60,0),-15)
place('WastebasketWire_1930','InteriorProps',(-6.36,-2.20,0))
place('CoatStandTimber_1930','InteriorProps',(-.87,-3.70,0))
place('ClockSchoolhouse_1930','InteriorPartitions',(-2.70,.1575,2.60))
place('TelephoneDesk_1930','InteriorProps',(-6.05,-1.48,.80),-15);place('DeskPaperwork_1930','InteriorProps',(-5.18,-1.47,.80))
# Keep the raked bench back in front of the projecting stone window sill.
place('BenchSlatted_210_1930','ExteriorProps',(-1.65,-5.16,0));place('NoticeBoardTimber_173_1930','InteriorProps',(-1.47,.12,1.3))
G.group('InteriorProps');text('Office notice title','WORK ORDERS',(-1.47,.081,2.23),.12,1.5,'TimberDark')
for i in range(5):
 box('Office admin paper',(-2.08+i*.3,.086,1.82),(.24,.003,.37),'Paper',0)
 cylinder('Office notice tack',(-2.08+i*.3,.078,1.98),.007,.012,'Brass',(0,1,0))
for x in [-6.25,-1.13]:
 for y in [1.32,3.22]:
  place('ShelfSteelTimber_170_1930','InteriorProps',(x,y,0),90 if x<-4 else -90)
  for level in [0,1,2,3]:
   for j in [-.55,0,.55]:
    name='ArchiveBox_1930' if x<-4 else ('PaintCan_1930' if level%2 else 'CrateTimber_045_1930')
    place(name,'InteriorProps',(x,y+j,.14+level*.46),90)
for x,y,z in [(-4.10,2.8,0),(-3.50,3.50,0),(-4.10,2.8,.58)]:place('CrateTimber_075_1930','InteriorProps',(x,y,z))
# Separate floor storage: sacks sit behind/left of the crates, never inside them.
for i,x in enumerate([-5.30,-4.65]):place('SackCanvasSlumped_1930' if i else 'SackCanvas_1930','InteriorProps',(x,3.72,0),i*32)
# Storage-facing solid office partition: clear of rear glazing, shelves and the circulation door.
place('GroundsToolRack_1930','InteriorPartitions',(-2.55,.43,.13),180)
place('WorkbenchJoiner_225_1930','InteriorProps',(5.75,2.75,0),-90)
place('ToolRackCarpenter_1930','InteriorPartitions',(-.0815,2.55,1.34),90)
place('CupboardUtility_1930','InteriorProps',(.6,3.7,0))
place('WorkbenchJoiner_225_1930','InteriorProps',(2.9,3.65,0))
place('ToolTrayCarpenter_1930','InteriorProps',(3.12,3.65,.952))
place('DeskLampBanker_1930','InteriorProps',(-4.70,-1.5,.80))
for p in [(5.6,2.10,.96),(5.9,2.2,.96),(5.8,3.4,.96)]:place('PaintCan_1930','InteriorProps',p)
place('CrateTimber_045_1930','InteriorProps',(5.75,2.75,.27),90)
place('WorkbenchJoiner_225_1930','YardProps',(8.6,3.52,0))
place('ToolTrayCarpenter_1930','YardProps',(8.9,3.5,.952))
G.group('YardShelter');beam('Rear tool rail',(7,4.15,1.34),(10.45,4.15,1.34),.085,.085,'TimberDark')
place('MaterialRackTimber_400_1930','YardProps',(11.55,1.2,0),90)
for z in [.25,.93,1.62]:place('LumberStack_240_1930','YardProps',(11.55,1.2,z),90)
place('WheelbarrowSteel_1930','YardProps',(8.35,.4,0),-20);place('WheelbarrowSteel_1930','YardProps',(11.5,-2.8,0),25)
for p in [(7.75,2.35,0),(7.75,2.35,.58)]:place('CrateTimber_075_1930','YardProps',p)
for p in [(7.5,.4,0),(7.5,1.15,0)]:place('BarrelPaintedSteel_1930','YardProps',p)
place('BarrelTimber_1930','YardProps',(9.55,2.15,0));place('LadderTimber_260_1930','YardProps',(7.20,3.73,0),90)
for i,p in enumerate([(10.7,4.03,0),(11.20,4.03,0),(11.70,4.03,0)]):place('SackCanvasSlumped_1930' if i%2 else 'SackCanvas_1930','YardProps',p,i*23)
# Fence sections with an open double gate. No props block the 3.6 m yard approach.
for y in [-3.3,-1.3,.7,2.7]:place('FenceWire_200_1930','ServiceYard',(12.35,y,0),90)
place('FenceWire_065_1930','ServiceYard',(12.35,4.025,0),90)
for x in [8,10]:place('FenceWire_200_1930','ServiceYard',(x,4.35,0))
place('FenceWire_135_1930','ServiceYard',(11.675,4.35,0));place('FenceWire_175_1930','ServiceYard',(11.475,-4.3,0))
place('FenceGateWire_180_1930','ServiceYard',(7.15,-3.58,0),90)
# Handed leaves share the same yard face when closed. Positive scale; the right leaf opens outward.
place('FenceGateWire_180_RH_1930','ServiceYard',(10.6,-5.26,0),90)
# Exactly one post per joint; the right gate assembly supplies the front fence's hinge post.
posts={(12.35,y) for y in [-4.3,-2.3,-.3,1.7,3.7,4.35]}
posts.update((x,4.35) for x in [9,11,12.35])
for x,y in sorted(posts):place('FencePostTubular_1930','ServiceYard',(x,y,0))
G.group('ServiceYard')
for z in [.17,1.86]:
 box('Rear fence wall terminal plate',(7.012,4.35,z),(.024,.105,.105),'Steel',.004)
 tube('Rear fence terminal rail socket',[(7.018,4.35,z),(7.10,4.35,z)],.030,'Steel',wall=.005)
# Short wall-fixed spigots support the terminal tension bands where no freestanding post is needed.
for z in [.20,.86,1.52,1.86]:
 box('Wall tension anchor plate',(7.012,4.35,z),(.024,.13,.085),'Steel',.004)
 cylinder('Wall tension band bearing',(7.005,4.35,z),.037,.030,'Steel',segments=24)
 for y in [4.30,4.40]:
  cylinder('Wall tension plate fixing',(7.029,y,z),.009,.012,'Galvanized',(1,0,0),segments=6)
# Existing canonical lamps/electrical panels and intentional conduit routes.
for f in manifest['fixtures']:
 f=dict(f,position=list(f['position']),existing=True)
 if f['asset']=='GooseneckLamp_1930':
  f['position'][2]=-4.511
  if f['position'][1]>5:
   if abs(f['position'][0])<.1:continue # Central shade obstructed the primary sign lettering.
   f['position'][1]=5.015;f['position'][0]=3.55 if f['position'][0]>0 else -3.55
  else:
   f['position'][2]=-4.501
   if abs(f['position'][0]+4.15)<.01:f['position'][1]=3.47 # Clearance over the curved office hood.
 if f['asset']=='PendantWorkLamp_1930' and f['position'][1]<3.5:f['position'][1]=3.40
 placements.append(f)
place('MeterCabinet_1930','RearWall',(5.6,4.51,1.22),180,True)
place('JunctionBox_Shallow_1930','RearWall',(5.6,4.515,3.47),180,True)
G.group('RearWall')
# Match the canonical cabinet's Service_Top socket (0, .57, .15) exactly.
tube('Cabinet service coupling',[(5.6,4.66,1.77),(5.6,4.66,1.88)],.023,'Galvanized',wall=.004)
tube('Service riser',[(5.6,4.66,1.86),(5.6,4.66,1.92),(5.6,4.64,1.97),(5.6,4.59,2.02),(5.6,4.57,2.07),(5.6,4.57,3.37)],.014,'Steel',wall=.002)
tube('Electrical feed',[(5.6,4.57,3.57),(5.6,4.57,3.70),(5.52,4.57,3.77),(4.8,4.57,3.77),(4.74,4.50,3.77)],.014,'Steel',wall=.002)
box('Wall penetration escutcheon',(4.74,4.515,3.77),(.07,.03,.07),'Iron',.005)
for z in [2.15,2.9,3.22]:box('Conduit wall strap',(5.6,4.58,z),(.07,.024,.03),'Galvanized')
G.group('Roof')
for x,y,z in [(-3.6,-1.7,3.40),(3,-1.7,3.70),(3,2.1,3.70),(-3.7,2.3,3.40)]:tube('Pendant suspension',[(x,y,z),(x,y,3.96)],.006,'Steel')

# Physical shell remains from the approved layout, furnishing collision belongs to kit instances.
keep_groups={'Floor','ExteriorGround','FrontWall','RightWall','LeftWall','RearWall','InteriorPartitions','YardShelter','ServiceYard'}
colliders=[c for c in manifest['colliders'] if c['group'] in keep_groups and not c['name'].startswith(('Yard slab',))]
colliders.extend([c for c in manifest['colliders'] if c['name']=='Yard slab'])
parts=[G.uv_packet(c) for c in G.GROUPS.values() if any(o.type=='MESH' for o in c.objects)]
out=ROOT/'ArtExports/StudioServices/Production';out.mkdir(parents=True,exist_ok=True)
data=dict(schema=1,parts=parts,materials=G.surfaces(),fixtures=placements,anchors=manifest['anchors'],colliders=colliders,approvedSourceSha256=source_hash,approvedDimensions=[14,9,4.22,5.35,5.15],reservedRect=manifest['reservedRect'])
G.save_packet(out/'production_meshes.json.gz',data)
summary={k:v for k,v in data.items() if k!='parts'};summary['parts']=[dict(name=p['name'],vertices=len(p['positions'])//3,triangles=sum(len(s['indices'])//3 for s in p['submeshes'])) for p in parts]
summary['generatorSha256']=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest();(out/'production_manifest.json').write_text(json.dumps(summary,indent=2))
# The editable assembly links the same authoritative kit collections; no embedded copies.
# Unique-mesh export above intentionally excludes these placements (Unity uses prefab instances).
names=sorted(set(f['asset'] for f in placements if not f['existing']))
with bpy.data.libraries.load(str(ROOT/'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'),link=True) as (available,linked):
 linked.collections=[n for n in names if n in available.collections]
collections={c.name:c for c in linked.collections if c is not None}
for f in placements:
 if f['existing']:continue
 o=bpy.data.objects.new('Shared '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=collections[f['asset']]
 G.GROUPS[f['group']].objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(180-f['yaw'])
for library in bpy.data.libraries:
 if pathlib.Path(library.filepath).name=='PeriodEnvironment1930.blend':library.filepath='//../PeriodEnvironment1930/PeriodEnvironment1930.blend'
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'StudioServices_Production.blend'))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
print('STUDIO_SERVICES_PRODUCTION',len(parts),'semantic meshes;',len(placements),'shared prefab placements')
