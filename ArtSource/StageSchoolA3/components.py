"""A3 component definitions, executed in the established generator namespace.
Local wall exterior is -Y. All hardware and finish exclusions use that contract.
"""
WALLS=[];WINDOWS=[];DOORS=[];DRAINS=[];QA={}
_legacy_box=box

def box(n,p,s,mat='SS_Stone',bevel=.008):
 if n=='Grounded foundation':return _legacy_box(n,p,s,mat,bevel)
 # bmesh's default profile is zero (a concave cut), not the UI's round .5.
 x,y,z=p;a,b,c=[v/2 for v in s]
 o=mesh(n,[(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)
 if bevel:
  bm=bmesh.new();bm.from_mesh(o.data);bm.normal_update()
  bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(s)*.2),segments=3,profile=.5,affect='EDGES',clamp_overlap=True)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 return o

def clear_group(n):
 if n in G.GROUPS:
  for o in list(G.GROUPS[n].objects):bpy.data.objects.remove(o,do_unlink=True)
 group(n)

def spans_without(a,b,cuts):
 spans=[(a,b)]
 for c,d in sorted(cuts):
  spans=[(lo,hi) for x,y in spans for lo,hi in [(x,min(y,c)),(max(x,d),y)] if hi-lo>.10]
 return spans

def transform_records(names,a,angle):
 r=Matrix.Rotation(angle,3,'Z')
 for rec in WINDOWS+DOORS:
  if rec['group'] not in names or rec.get('world'):continue
  v=r@Vector((*rec['center'],0))+Vector((*a,0));rec['center']=[v.x,v.y]
  rec['angle']+=angle;rec['world']=True

def window(g,x,y,z,w=1.5,h=2.1,rot=0):
 group(g);before=set(G.GROUPS[g].objects)
 # One glazing plane within continuous steel rebates; no outside latch duplicates.
 box('Rebated window glass',(x,y,z+h/2),(w-.12,.024,h-.12),'SS_ClearGlass',0)
 for dx in [-w/2,w/2]:
  box('Dressed stone jamb',(x+dx,y,z+h/2),(.14,.40,h+.14),'SS_Stone')
  box('Steel jamb rebate',(x+dx*.92,y,z+h/2),(.055,.105,h-.045),'Steel',.003)
  box('Interior painted casing',(x+dx,y+.205,z+h/2),(.105,.055,h+.12),'SS_Stone',.006)
 for zz in [z,z+h]:
  box('Stone opening head or sill',(x,y-.04,zz),(w+.29,.42,.12),'SS_Stone')
  box('Steel head or sill',(x,y,zz+(.046 if zz==z else -.046)),(w-.04,.105,.06),'Steel',.003)
  box('Interior head or apron',(x,y+.205,zz),(w+.18,.055,.085),'SS_Stone',.004)
 box('Weathered sill with drip',(x,y-.19,z-.055),(w+.36,.32,.07),'SS_Stone')
 box('Interior sill stool',(x,y+.24,z-.015),(w+.27,.16,.06),'SS_Stone')
 for dx in [-w/6,w/6]:box('Steel casement mullion',(x+dx,y,z+h/2),(.033,.09,h-.10),'Steel',.003)
 for t in ([.34,.67] if h>1 else [.5]):box('Steel glazing transom',(x,y,z+h*t),(w-.10,.085,.028),'Steel',.002)
 if h>1:
  for dx in [-w/6,w/6]:
   box('Interior latch plate',(x+dx,y+.059,z+h*.45),(.047,.012,.115),'Brass',.002)
   tube('Interior casement lever',[(x+dx,y+.064,z+h*.45+.025),(x+dx,y+.10,z+h*.45+.025),(x+dx+.055,y+.10,z+h*.45-.025)],.009,'Brass',sides=8)
 else:
  box('Interior fanlight catch',(x,y+.063,z+.07),(.10,.022,.035),'Brass',.002)
 WINDOWS.append(dict(group=g,center=[x,y],z=z,w=w,h=h,angle=math.radians(rot),world=False,latch_side='interior +normal'))
 if rot:
  for o in set(G.GROUPS[g].objects)-before:
   for v in o.data.vertices:v.co=Matrix.Rotation(math.radians(rot),3,'Z')@v.co

def frame(g,x,y,w=1.2,th=.18,h=2.4):
 group(g)
 for dx in [-w/2,w/2]:
  box('Timber lining jamb',(x+dx,y,.36+h/2),(.072,th+.04,h),'SS_Walnut')
  for side in [-1,1]:
   box('Door casing both faces',(x+dx,y+side*(th/2+.041),.36+h/2),(.12,.064,h+.08),'SS_Walnut')
   box('Architrave bead',(x+dx-side*.032,y+side*(th/2+.077),.36+h/2),(.016,.018,h+.04),'Timber',.003)
 for side in [-1,1]:box('Casing head both faces',(x,y+side*(th/2+.041),.36+h+.045),(w+.19,.064,.12),'SS_Walnut')
 box('Timber head lining',(x,y,.36+h+.015),(w+.07,th+.04,.06),'SS_Walnut')

def door(name,x,y,width=1.12,h=2.34,angle=0,glazed=False):
 g='Doors/'+name;group(g);right=name in ['EntranceRight','StaffService'];hinge=x+(width/2 if right else -width/2)
 angle={'StaffA2':-85,'StaffService':-85,'FlexibleService':-85,'RecordsA2':90,'StoreA2':90,'StaffInterviewLink':-85,'Washroom':-85}.get(name,angle)
 pivots[g]=[hinge,.38,y]
 mat='SS_Bronze' if glazed else 'SS_Walnut';bottom=.38;top=bottom+h
 if not glazed:box('Solid mortised door core',(x,y,(bottom+top)/2),(width,.056,h),mat,.009)
 for side in [-1,1]:
  for dx in [-width/2+.053,width/2-.053]:box('Door face stile',(x+dx,y+side*.037,(bottom+top)/2),(.102,.035,h),mat,.005)
  for zz in [bottom+.055,bottom+.80,top-.055]:box('Door cross rail',(x,y+side*.038,zz),(width-.10,.036,.11),mat,.004)
  fields=[(bottom+.42,.60)] if glazed else [(bottom+.42,.60),(bottom+(h+.83)/2,h-.99)]
  for zz,hh in fields:
   box('Recessed door panel both sides',(x,y+side*.031,zz),(width-.25,.018,hh),'TimberDark',.006)
   for dx in [-width/2+.115,width/2-.115]:box('Field bolection stile',(x+dx,y+side*.050,zz),(.025,.022,hh+.055),mat,.004)
   for z in [zz-hh/2-.02,zz+hh/2+.02]:box('Field bolection rail',(x,y+side*.050,z),(width-.22,.022,.026),mat,.004)
  hx=x+(-1 if right else 1)*(width/2-.15)
  box('Lock plate both faces',(hx,y+side*.062,1.40),(.055,.018,.19),'Brass',.003)
  tube('Returned pull both faces',[(hx,y+side*.065,1.34),(hx,y+side*.14,1.34),(hx,y+side*.14,1.48),(hx,y+side*.065,1.48)],.012,'Brass',sides=10)
 if glazed:box('Rebated door glazing',(x,y,(bottom+.86+top-.11)/2),(width-.19,.024,h-.97),'SS_ClearGlass',0)
 for z in [.62,1.52,top-.22]:
  cylinder('Hinge knuckle',(hinge,y+.028,z),.018,.14,'Brass',segments=12)
  box('Hinge leaf mortise',(hinge+(-.032 if right else .032),y+.048,z),(.058,.012,.13),'Brass',.002)
  for dz in [-.042,.042]:cylinder('Hinge screw',(hinge+(-.03 if right else .03),y+.057,z+dz),.007,.009,'Steel',(0,1,0),segments=8)
 box('Latch edge plate',(x+(-width/2 if right else width/2),y,1.40),(.007,.047,.15),'Brass',.001)
 if angle:
  p=Vector((hinge,y,.38));r=Matrix.Rotation(math.radians(angle),3,'Z')
  for o in G.GROUPS[g].objects:
   for v in o.data.vertices:v.co=p+r@(v.co-p)
 DOORS.append(dict(group=g,center=[x,y],width=width,angle=0,swing=angle,hinge='right' if right else 'left',world=False))

def xwall(g,y,a,b,height,holes=(),mat='SS_Stucco',th=.32,z0=.36):
 group(g);xs=sorted(set([a,b]+[v for h in holes for v in h[:2]]));zs=sorted(set([z0,height]+[v for h in holes for v in h[2:]]))
 for x0,x1 in zip(xs,xs[1:]):
  for q,r in zip(zs,zs[1:]):
   if any(h[0]<(x0+x1)/2<h[1] and h[2]<(q+r)/2<h[3] for h in holes):continue
   box('Masonry pier or spandrel',((x0+x1)/2,y,(q+r)/2),(x1-x0,th,r-q),mat,0)
 if mat=='SS_Stucco':
  for lo,hi in spans_without(a,b,[(h[0],h[1]) for h in holes if h[2]<.70]):box('Stone base course',((lo+hi)/2,y,.53),(hi-lo,th+.035,.34),'SS_Stone')

def drain_positions(name,a,b,normal,offset):
 length=(b-a).length;out=[]
 front=a.y<-9 and b.y<-9
 preferences=[.03,.97] if front and name=='Entrance' else ([.055,.945] if length>7 else [.10])
 for preferred in preferences:
  options=sorted([i/100 for i in range(3,98)],key=lambda f:abs(f-preferred))
  for f in options:
   q=a.lerp(b,f)-normal*offset;bad=False
   # Front pipes must also clear cast letters, portal piers, banners and lanterns.
   if front and ((name=='Entrance' and abs(q.x)<3.20) or (name!='Entrance' and abs(q.x)<6.30)):continue
   for win in WINDOWS:
    c=Vector(win['center']);t=Vector((math.cos(win['angle']),math.sin(win['angle'])));n=Vector((-t.y,t.x));v=q-c
    if abs(v.dot(n))<.75 and abs(v.dot(t))<win['w']/2+.34:bad=True;break
   if not bad and all(abs(f-v)*length>1 for v in out):out.append(f);break
  else:raise ValueError('No clear drainage pier: '+name)
 return out

def install_components():
 # Retained A1 objects must receive the same corrected component definition.
 clear_group('Shell/Front/Windows')
 for x in [-8.4,8.4]:window('Shell/Front/Windows',x,-8.83,1.26,1.68,2.25);WINDOWS[-1]['world']=True
 for side in [-1,1]:
  n='Entrance'+('Left' if side<0 else 'Right');clear_group('Doors/'+n);door(n,side*.79,-9.02,1.52,2.49,0,True);DOORS[-1]['world']=True
 clear_group('Doors/Washroom');door('Washroom',7.75,-4.15,.92,2.34,-85);DOORS[-1]['world']=True
 for n,c in G.GROUPS.items():
  for o in list(c.objects):
   if o.name.startswith('Interior timber skirting'):bpy.data.objects.remove(o,do_unlink=True)
 # Baseline front and washroom walls are retained, but enter the finish registry.
 for a,b,x in [(-11,-3.6,-8.4),(3.6,11,8.4)]:WALLS.append(dict(name='Shell/Front',a=(a,-8.8),b=(b,-8.8),height=4.65,windows=[(x-a,1.68,1.26,2.25)],doors=[],th=.32,interior=False))
 WALLS.extend([dict(name='Partitions/Washroom',a=(6.7,-4.15),b=(10.65,-4.15),height=3.2,windows=[],doors=[('Washroom',1.05,1.1)],th=.16,interior=True),dict(name='Partitions/WashroomSide',a=(6.7,-8.65),b=(6.7,-4.15),height=3.2,windows=[],doors=[],th=.16,interior=True)])
 group('Partitions/Washroom');box('Washroom ceiling closure',(8.675,-4.15,3.65),(3.95,.16,.90),'InteriorPlaster',0)
 group('Partitions/WashroomSide');box('Washroom side ceiling closure',(6.7,-6.40,3.65),(.16,4.50,.90),'InteriorPlaster',0)
