"""A3 finish/joinery and room composition. A2 floor/roof program is invariant."""
def move_rotate(g,old,new,angle=0):
 r=Matrix.Rotation(math.radians(angle),3,'Z');a=Vector(old);b=Vector(new)
 for o in G.GROUPS[g].objects:
  m=o.matrix_world.copy()
  for v in o.data.vertices:v.co=b+r@(m@v.co-a)
  o.matrix_world.identity()

def soft(n,p,s,r,mat):
 o=box(n,p,s,mat,0);bm=bmesh.new();bm.from_mesh(o.data)
 bm.normal_update()
 bmesh.ops.bevel(bm,geom=list(bm.edges),offset=r,segments=5,profile=.5,affect='EDGES',clamp_overlap=True)
 bm.to_mesh(o.data);bm.free()
 for poly in o.data.polygons:poly.use_smooth=True
 m=o.modifiers.new('Upholstery weighted normals','WEIGHTED_NORMAL');m.keep_sharp=True
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 return o

def chair(g,x,y,angle):
 clear_group(g)
 for dx in [-.285,.285]:
  for dy in [-.255,.255]:
   verts=[(dx+sx*w,dy+sy*w,z) for z,w in [(.381,.026),(.69,.040)] for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]]
   mesh('Tapered walnut leg',verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'SS_Walnut')
   cylinder('Felt floor glide',(dx,dy,.378),.026,.007,'TimberDark',segments=12)
 soft('Upholstered sprung seat deck',(0,0,.686),(.76,.73,.18),.05,'SS_Sage')
 box('Walnut apron',(0,-.351,.635),(.67,.043,.09),'SS_Walnut',.012)
 soft('Boxed seat cushion',(0,-.037,.822),(.58,.59,.14),.060,'SS_Sage')
 soft('Upholstered back shell',(0,.292,1.025),(.76,.22,.61),.072,'SS_Sage')
 soft('Shaped inner back cushion',(0,.145,1.075),(.58,.15,.40),.065,'SS_Sage')
 for dx in [-.339,.339]:
  soft('Continuous upholstered arm',(dx,-.005,.901),(.15,.72,.30),.066,'SS_Sage')
  box('Walnut arm bottom reveal',(dx,-.01,.716),(.13,.64,.047),'SS_Walnut',.012)
 # Tailored piping follows the cushion perimeter and the complete rear shell.
 for z,wx,wy,cy in [(.856,.278,.277,-.037),(.763,.276,.275,-.037)]:
  pts=[]
  for i in range(65):
   a=i*math.pi/32;pts.append((wx*math.copysign(abs(math.cos(a))**.35,math.cos(a)),cy+wy*math.copysign(abs(math.sin(a))**.35,math.sin(a)),z))
  tube('Tailored cushion welt',pts,.0045,'SS_Sage',sides=6)
 for dx in [-.29,.29]:tube('Rear upholstery seam',[(dx,.405,.78),(dx,.405,1.22),(dx*.85,.398,1.28)],.004,'SS_Sage',sides=6)
 move_rotate(g,(0,0,0),(x,y,0),angle)

print('A3 continuous opening-aware wall joinery',flush=True)
TRIM=[]
for w in WALLS:
 a,b=Vector(w['a']),Vector(w['b']);length=(b-a).length;t=(b-a).normalized();normal=Vector((-t.y,t.x))
 for side in ([1,-1] if w['interior'] else [1]):
  g=w['name']+'/InteriorFinish'+('A' if side==1 else 'B');group(g);before=set(G.GROUPS[g].objects)
  cuts=[(u-width/2-.13,u+width/2+.13) for _,u,width in w['doors']]
  spans=spans_without(.08,length-.08,cuts)
  wash='Washroom' in w['name'];mat='SS_Ivory' if wash else 'SS_Walnut'
  for lo,hi in spans:
   yy=side*(w['th']/2+.023);wide=hi-lo
   box('Continuous dado backing',((lo+hi)/2,yy,.775),(wide,.034,.79),mat,.002)
   for zz,hh,depth in [(.46,.16,.07),(.59,.067,.051),(1.065,.075,.051),(1.135,.060,.081)]:
    box('Joined base rail or returned cap',((lo+hi)/2,yy+side*.015,zz),(wide,depth,hh),mat,.006)
   count=max(1,round(wide/.85));step=wide/count
   for i in range(count):
    x=lo+(i+.5)*step
    box('Recessed framed wainscot panel',(x,yy+side*.020,.825),(step-.105,.021,.40),mat,.008)
   for i in range(count+1):box('Continuous joinery stile',(lo+i*step,yy+side*.027,.817),(.070,.030,.48),mat,.006)
   TRIM.append(dict(wall=w['name'],side=side,interval=[lo,hi],door_exclusions=cuts))
  for o in set(G.GROUPS[g].objects)-before:
   for v in o.data.vertices:
    q=a+t*v.co.x+normal*v.co.y;v.co=(q.x,q.y,v.co.z)

# The old one-wall finish system is not retained as disconnected decorative strips.
for n in ['Partitions/InterviewFinish','Partitions/FlexibleFinish','Partitions/AuditionFinish']:clear_group(n)

print('A3 reusable armchair and functional furnishing audit',flush=True)
for i,(x,y,r) in enumerate([(-9.1,-6.7,90),(-9.1,-5.5,90),(-9.1,-4.3,90),(-4.1,-6.7,-90),(-4.1,-5.5,-90),(-4.1,-4.3,-90),(-7.45,-2.2,0),(-6,-2.2,0)]):chair('Furnishings/Waiting/Chair%02d'%i,x,y,r)
for i,(x,y,r) in enumerate([(4,-6,180),(5.3,-6,180),(4,-3.1,0),(5.3,-3.1,0)]):chair('Furnishings/Lounge/Chair'+str(i),x,y,r)
for n,x in [('GuestA',-.75),('GuestB',.75)]:chair('Furnishings/Interview/'+n,x,7.3,180)

for f in fixtures:
 g=f['group'];p=f['position']
 if g=='Furnishings/Reception/StaffChair':f['yaw']=180;p[1]=.381;p[2]=-3.40
 if g=='Furnishings/Reception/Telephone':f['yaw']=0
 if g=='Furnishings/Reception/BankerLamp':p[:]=[1.08,1.14,-4.12];f['yaw']=0
 if g.startswith('Furnishings/Interview/') and f['asset'] in ['TelephoneDesk_1930','DeskPaperwork_1930','DeskLampBanker_1930']:f['yaw']=0
 if g=='Furnishings/Interview/Files':p[:]=[2.28,.38,10.15];f['yaw']=270
 if g.startswith('Furnishings/Records/Rack'):
  i=int(g[-1]);p[:]=[-11.90+i*.80,.36,12.0];f['yaw']=270 if i==0 else 90
 if g=='Furnishings/Records/Files':p[:]=[-9.13,.38,12.85];f['yaw']=270
 if g=='Furnishings/Storage/Cupboard':p[:]=[-6.08,.36,12.32];f['yaw']=90
 if g=='Furnishings/Storage/EquipmentShelf':p[:]=[-3.70,.36,12.18];f['yaw']=270
 if g=='Furnishings/Flexible/Files':p[:]=[-7.60,.38,6.65];f['yaw']=270
 # Pull two staff workstations away from both north/south doorway swing zones.
 if g.startswith('Furnishings/Staff/') and f['asset'] in ['DeskPedestal_1930','ChairBentwood_1930']:
  p[2]=4.65 if g.endswith('0') else 6.85
  if f['asset']=='DeskPedestal_1930':p[0]=-5.72;f['yaw']=90
  else:
   p[0]=-4.65;f['yaw']=270
   if g.endswith('1'):p[2]=6.60
 if g=='Furnishings/Staff/Telephone':p[:]=[-5.70,1.19,4.65];f['yaw']=90
 if g=='Furnishings/Staff/RecordsInUse':p[:]=[-5.70,1.19,6.85];f['yaw']=90

fixtures[:]=[f for f in fixtures if f['group']!='Furnishings/Records/Rack2']

# Keep library on an uninterrupted side wall, clear of the interview connecting door.
move_rotate('Furnishings/Interview/Bookcase',(-2,10.95,0),(-2.54,9.42,0),90)
shift('Shell/Rear/OfficeClock',dx=-.60,dy=-.10,dz=.13)
clear_group('Furnishings/Staff/ArchiveBoxes')
for i in range(6):
 x=-11.9 if i<3 else -11.1;y=11.45+(i%3)*.51;z=.64
 box('Supported labelled record box',(x,y,z),(.36,.48,.28),'Canvas',.008)
 box('Readable aisle-side label',(x+(-.184 if i<3 else .184),y,z),(.007,.18,.075),'PaperLight',.001)

# Reception rear: finished pedestals/drawers, modesty return and legroom under worktop.
group('Furnishings/Reception/Counter')
for x in [-1.08,1.08]:
 box('Counter staff-side pedestal',(x,-4.03,.744),(.39,.54,.71),'SS_Walnut',.015)
 box('Pedestal recessed plinth',(x,-4.03,.422),(.35,.49,.078),'TimberDark',.009)
 for z in [.57,.80,1.01]:
  box('Staff drawer framed front',(x,-3.746,z),(.35,.035,.18),'SS_Walnut',.009)
  tube('Staff drawer brass bail',[(x-.075,-3.719,z),(x-.075,-3.692,z-.018),(x+.075,-3.692,z-.018),(x+.075,-3.719,z)],.008,'Brass',sides=8)
box('Counter interior modesty panel',(0,-4.43,.77),(1.82,.045,.65),'SS_Walnut',.01)
for x in [-.83,.83]:box('Writing surface bracket',(x,-4.15,1.02),(.055,.40,.12),'SS_Walnut',.01)
# Legs and aprons are completed on every bespoke table, not merely the hero view.
for g in ['Furnishings/Waiting/CoffeeTable','Furnishings/Lounge/Table','Furnishings/Commons/PortfolioTable','Furnishings/Flexible/ReviewTable','Furnishings/Audition/EvaluationTable']:
 c=G.GROUPS[g];top=next(o for o in c.objects if o.name.startswith('Rounded timber table top'))
 vs=[top.matrix_world@v.co for v in top.data.vertices];lo=Vector(tuple(min(v[i] for v in vs) for i in range(3)));hi=Vector(tuple(max(v[i] for v in vs) for i in range(3)));center=(lo+hi)/2;sz=hi-lo
 group(g)
 for side in [-1,1]:
  box('Table mortised apron',(center.x,center.y+side*sz.y*.34,center.z-.10),(sz.x*.80,.032,.12),'SS_Walnut',.005)
  box('Table end apron',(center.x+side*sz.x*.40,center.y,center.z-.10),(.032,sz.y*.69,.12),'SS_Walnut',.005)

print('A3 audition performance installation',flush=True)
clear_group('Furnishings/Audition/Backdrop')
box('Wide taut neutral screen',(8.5,14.40,2.23),(5.65,.028,3.00),'Canvas',.003)
for x in [5.65,11.35]:box('Backdrop stretched timber frame',(x,14.445,2.23),(.075,.065,3.06),'SS_Walnut',.004)
for z in [.72,3.74]:box('Backdrop header and sole',(8.5,14.445,z),(5.77,.065,.075),'SS_Walnut',.004)
for x in [5.65,8.5,11.35]:
 beam('Backdrop wall standoff',(x,14.445,3.70),(x,14.67,3.70),.038,.038,'Steel')
 box('Backdrop anchor plate',(x,14.674,3.70),(.09,.02,.09),'Steel',.003)
clear_group('Furnishings/Audition/Curtains')
# Retain approved sewn velvet vocabulary; widen assembly and put cloth ahead of screen.
R.curtains()
for o in G.GROUPS['Furnishings/Audition/Curtains'].objects:
 for v in o.data.vertices:
  p=v.co;v.co=(8.5+(p.x-6.9)*1.30,13.96+(p.y-8.12),.735+(p.z-.722)*1.12)
 # Supports must reach the actual rear wall rather than ending at the old A1 plane.
 if o.name.startswith(('Track bracket rear plate','Curtain tieback wall plate')):
  for v in o.data.vertices:v.co.y+=.49
 if o.name.startswith(('Rail support','Tieback return hook')):
  for v in o.data.vertices:
   if v.co.y>14.13:v.co.y+=.49
group('Shell/Rear/Audition/CurtainSupports')
for x in [4.795,6.55,8.5,10.45,12.205]:box('Curtain mounting backing',(x,14.665,3.82),(.11,.09,.20),'SS_Walnut',.005)
clear_group('Furnishings/Audition/Rostrum')
box('Joinery rostrum carcass',(8.5,13.03,.535),(7.50,3.00,.31),'SS_Walnut',.014)
for i in range(30):box('Tongued rostrum floor',(4.88+i*.2495,13.03,.703),(.247,2.98,.028),'Timber',.003)
for x in [4.75,12.25]:box('Platform nosed side',(x,13.03,.690),(.08,3.08,.08),'SS_Walnut',.015)
box('Platform front nosing',(8.5,11.52,.690),(7.57,.10,.08),'SS_Walnut',.015)
box('Platform half-height step',(8.5,11.28,.464),(2.8,.42,.168),'SS_Walnut',.014)
box('Step nosing',(8.5,11.055,.544),(2.86,.065,.047),'SS_Walnut',.012)
equipment[1]['position']=[5.30,.39,10.50];equipment[2]['position']=[11.70,.39,10.50]
equipment[1]['yaw']=48;equipment[2]['yaw']=-48

print('A3 complete period washroom fixtures',flush=True)
clear_group('Furnishings/Washroom')
def bowl(n,x,y,z,rx,ry,depth):
 # Revolved oval shell with a real recess, finished rim and visible drain, no flat blob.
 vs=[];fs=[];rings=[(1,0),(.88,-.015),(.69,-depth*.7),(.29,-depth),( .22,-depth)]
 for r,dz in rings:
  for i in range(48):a=i*math.pi/24;vs.append((x+rx*r*math.cos(a),y+ry*r*math.sin(a),z+dz))
 for j in range(4):
  for i in range(48):k=j*48+i;fs.append((k,j*48+(i+1)%48,(j+1)*48+(i+1)%48,k+48))
 o=mesh(n,vs,fs,'SS_Ivory')
 for p in o.data.polygons:p.use_smooth=True
 m=o.modifiers.new('Ceramic shell thickness','SOLIDIFY');m.thickness=.022;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 cylinder('Recessed basin drain',(x,y,z-depth-.002),rx*.21,.015,'Steel',segments=20)

for i,x in enumerate([7.74,9.53]):
 soft('Close-coupled porcelain cistern',(x,-8.35,1.04),(.50,.20,.33),.04,'SS_Ivory')
 box('Cistern lid',(x,-8.35,1.227),(.54,.24,.045),'SS_Ivory',.012)
 soft('Cistern supported ceramic rear shelf',(x,-8.27,.855),(.43,.34,.06),.02,'SS_Ivory')
 bowl('Open vitreous toilet bowl',x,-7.93,.819,.275,.36,.20)
 soft('Toilet pedestal',(x,-8.00,.48),(.29,.36,.20),.075,'SS_Ivory')
 tube('Black molded toilet seat',[(x+.249*math.cos(i*math.pi/24),-7.93+.33*math.sin(i*math.pi/24),.842) for i in range(49)],.028,'Black',sides=10)
 for dx in [-.12,.12]:cylinder('Seat hinge',(x+dx,-8.225,.843),.026,.08,'Brass',(1,0,0),segments=12)
 tube('Flush lever',[(x+.245,-8.35,1.08),(x+.29,-8.35,1.08),(x+.29,-8.40,1.03)],.012,'Brass',sides=10)
 tube('Cistern feed',[(x+.20,-8.38,.39),(x+.20,-8.38,.91),(x+.20,-8.31,.99)],.018,'Galvanized',sides=10)
 cylinder('Isolating valve',(x+.20,-8.40,.64),.037,.025,'Brass',(0,1,0),segments=12)
# Stalls: supported panels, leg clearances, full front framing and two-sided hardware.
for x in [6.94,8.64,10.34]:
 box('Cubicle partition',(x,-7.35,1.44),(.055,2.28,1.82),'InteriorPlaster',.01)
 for y in [-8.40,-6.28]:
  cylinder('Partition floor leg',(x,y,.47),.028,.22,'Steel',segments=12)
  cylinder('Partition foot flange',(x,y,.386),.065,.023,'Steel',segments=12)
 box('Cubicle top stabilizer',(x,-7.35,2.37),(.08,2.33,.065),'SS_Walnut',.008)
for i,x in enumerate([7.79,9.49]):
 # Narrow enough to open inward without the bowl, partition or adjacent leaf.
 for dx in [-.70,.70]:box('Stall front fixed pilaster',(x+dx,-6.20,1.46),(.24,.065,1.86),'SS_Sage',.009)
 dn='Cubicle'+str(i);door(dn,x,-6.20,1.05,1.84,0);DOORS[-1]['world']=True
 group('Furnishings/Washroom');box('Cubicle header',(x,-6.20,2.38),(1.72,.07,.08),'SS_Walnut',.008)
# Wall-hung basins have visible traps, supported brackets and reachable taps.
for x in [9.10,10.00]:
 bowl('Recessed wash basin',x,-4.72,1.22,.36,.27,.14)
 box('Ceramic tap deck',(x,-4.50,1.205),(.66,.16,.055),'SS_Ivory',.013)
 for dx in [-.21,.21]:tube('Basin wall bracket',[(x+dx,-4.26,.97),(x+dx,-4.72,.97),(x+dx,-4.72,1.085)],.018,'Steel',sides=10)
 tube('Exposed basin waste trap',[(x,-4.72,1.07),(x,-4.72,.83),(x,-4.77,.72),(x,-4.84,.72),(x,-4.89,.82),(x,-4.89,.94),(x,-4.25,.94)],.025,'Galvanized',sides=12)
 tube('Swan-neck mixer tap',[(x,-4.50,1.20),(x,-4.50,1.43),(x,-4.65,1.46),(x,-4.71,1.39)],.015,'Brass',sides=12)
 for dx in [-.15,.15]:
  cylinder('Tap base',(x+dx,-4.51,1.24),.035,.055,'Brass',segments=12)
  tube('Cross tap handle',[(x+dx-.034,-4.51,1.283),(x+dx+.034,-4.51,1.283)],.008,'Brass',sides=8)
 box('Mirror framed backing',(x,-4.258,1.94),(.67,.047,.91),'SS_Walnut',.01)
 box('Mirror polished glass',(x,-4.286,1.94),(.60,.009,.84),'SS_Mirror',.001)
 box('Wall soap tray',(x,-4.42,1.48),(.18,.20,.027),'SS_Ivory',.008)

print('A3 entrance stonework, handrails and cast letters',flush=True)
clear_group('Structure/EntranceSteps')
for i in range(3):
 z=(i+1)*.12;front=-10.70+i*.32;back=-8.68
 box('Continuous dressed stone stair',(0,(front+back)/2,z/2),(6.4-i*.4,back-front,z),'SS_Stone',.012)
 box('Rounded stair nosing',(0,front-.012,z-.016),(6.4-i*.4+.016,.065,.038),'SS_Stone',.010)
 for x in [-2.0,-1.0,0,1.0,2.0]:box('Fine tread stone joint',(x,(front+back)/2,z+.0006),(.004,back-front-.06,.002),'Mortar',0)
# The runner is omitted: the stone stair reads as permanent public architecture.
for side in [-1,1]:
 x=side*2.46
 tube('Continuous returned bronze handrail',[(x,-10.63,.93),(x,-10.63,1.02),(x,-10.32,1.02),(x,-9.65,1.29),(x,-9.06,1.29),(x,-8.78,1.29),(x,-8.78,1.17)],.029,'SS_Bronze',sides=16)
 for y,z,basez in [(-10.50,1.02,.12),(-9.64,1.29,.36),(-8.88,1.29,.36)]:
  cylinder('Handrail anchored post',(x,y,(z+basez)/2),.024,z-basez,'SS_Bronze',segments=12)
  cylinder('Stone-fixed post flange',(x,y,basez+.018),.073,.032,'SS_Bronze',segments=16)
  for dx in [-.041,.041]:cylinder('Recessed anchor bolt',(x+dx,y,basez+.037),.009,.008,'Brass',segments=8)
box('Entrance dressed threshold',(0,-9.015,.386),(3.26,.54,.052),'SS_Stone',.009)
group('Shell/Front/Portal')
for x in [-1.62,1.62]:
 box('Interior portal lining',(x,-8.705,1.64),(.12,.12,2.53),'SS_Stone',.008)
arch_band('Interior arch return',0,-8.71,2.9,1.65,1.79,.09,'SS_Stone')
clear_group('Shell/Front/Signage')
serif('STAGE SCHOOL',(0,-9.348,5.185),.64,5.72,'Steel')
# Correct text placement using actual mesh bounds (font origin is a baseline).
o=next(iter(G.GROUPS['Shell/Front/Signage'].objects));bpy.context.view_layer.update()
pts=[o.matrix_world@v.co for v in o.data.vertices];xmin=min(v.x for v in pts);xmax=max(v.x for v in pts);zmin=min(v.z for v in pts);zmax=max(v.z for v in pts)
for v in o.data.vertices:
 q=o.matrix_world@v.co;q.x-=(xmin+xmax)/2;q.z+=5.38-(zmin+zmax)/2;v.co=q
o.matrix_world.identity()
# Flush-backed individual cast letters: mountings are concealed within each glyph.

print('A3 waiting/common wall composition',flush=True)
# A central approach to the board, away from door casings, at normal reading height.
for f in fixtures:
 if f['group']=='Furnishings/Waiting/Noticeboard':f['position']=[-7.45,1.36,3.145];f['yaw']=180
move_rotate('Furnishings/Waiting/Notices',(-12.3,3.14,0),(-7.45,3.145,.11))
clear_group('Furnishings/Waiting/PortraitGallery')
for i,(x,z) in enumerate([(-12.55,2.08),(-11.70,2.08),(-12.55,2.96),(-11.70,2.96)]):
 y=3.145
 box('Gallery walnut frame',(x,y,z),(.58,.075,.73),'SS_Walnut',.008)
 box('Ivory gallery mount',(x,y-.046,z),(.50,.021,.65),'PaperLight',.004)
 o=mesh('Fictional performer photograph',[(x-.20,y-.059,z-.245),(x+.20,y-.059,z-.245),(x+.20,y-.059,z+.245),(x-.20,y-.059,z+.245)],[(0,1,2,3)],'SS_Headshots')
 uv=o.data.uv_layers.new(name='PortraitAtlas');u=(i%3)/3;v=.5 if i<3 else 0;coords=[(u+.012,v+.015),(u+1/3-.012,v+.015),(u+1/3-.012,v+.485),(u+.012,v+.485)]
 for loop in o.data.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
 o['explicit_uv']=True

print('A3 roof/occupied-volume intersection correction',flush=True)
removed=0;affected=[]
# Cut only geometry actually inside a neighboring occupied prism below its ceiling.
# This preserves nine roof spans/heights; cuts terminate within the supporting walls.
for n,c in list(G.GROUPS.items()):
 if not n.startswith('Roofs/'):continue
 for o in list(c.objects):
  if o.type!='MESH':continue
  for rn,a,b,cc,d,zz in ceiling_rooms:
   a-=.10;b+=.10;cc-=.10;d+=.10
   if not o.data.vertices:break
   pts=[v.co for v in o.data.vertices];lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
   # Include the ceiling thickness; require meaningful plan overlap.
   if hi[0]<=a+.015 or lo[0]>=b-.015 or hi[1]<=cc+.015 or lo[1]>=d-.015 or lo[2]>=zz+.041:continue
   bm=bmesh.new();bm.from_mesh(o.data)
   for axis,value in [(0,a),(0,b),(1,cc),(1,d),(2,zz+.045)]:
    normal=[0,0,0];normal[axis]=1;point=[0,0,0];point[axis]=value
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
   bad=[f for f in bm.faces if a-.0001<f.calc_center_median().x<b+.0001 and cc-.0001<f.calc_center_median().y<d+.0001 and f.calc_center_median().z<zz+.045]
   if bad:
    removed+=len(bad);affected.append([n,rn]);bmesh.ops.delete(bm,geom=bad,context='FACES')
   bm.to_mesh(o.data);bm.free()
QA.update(roof_faces_clipped=removed,roof_room_pairs=sorted(set(tuple(v) for v in affected)),trim_spans=TRIM,armchairs_rebuilt=14,runner='removed',backdrop_width=5.65,platform=[7.5,3.0],macro_architecture='A2 retained')
print('A3 source finish complete; clipped roof faces:',removed,flush=True)
