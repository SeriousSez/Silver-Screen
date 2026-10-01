"""A3.1: only the six recorded A3 defects. Executed before packet export."""
print('A3.1 local roof junctions and supported drainage',flush=True)

def roof_planes(r):
 _,x,y,w,d,e,h=r;k=h/(min(w,d)*.5*.87)
 return [(0,0,e+h),(k,0,e+k*(w/2-x)),(-k,0,e+k*(w/2+x)),(0,k,e+k*(d/2-y)),(0,-k,e+k*(d/2+y))]
def roof_z(r,x,y):return min(a*x+b*y+c for a,b,c in roof_planes(r))
def in_roof(r,x,y,pad=0):return abs(x-r[1])<r[3]/2+pad and abs(y-r[2])<r[4]/2+pad
def line_interval(p,t,constraints):
 lo,hi=-100.,100.
 for a,b,c in constraints: # ax+by+c >= 0
  f=a*p.x+b*p.y+c;s=a*t.x+b*t.y
  if abs(s)<1e-9:
   if f<-.00001:return None
  elif s>0:lo=max(lo,-f/s)
  else:hi=min(hi,-f/s)
 return (p+t*lo,p+t*hi) if hi-lo>.025 else None
def domain(r,plane):
 _,x,y,w,d,_,_=r;a,b,c=plane
 return [(1,0,-x+w/2),(-1,0,x+w/2),(0,1,-y+d/2),(0,-1,y+d/2)]+[(u-a,v-b,s-c) for u,v,s in roof_planes(r)]

# Exact piecewise-planar intersections, clipped to active roof faces. Roof shapes do not change.
VALLEYS=[];PAIRLINES={}
for i,r in enumerate(roofs):
 for j,s in enumerate(roofs[:i]):
  if abs(r[1]-s[1])>=(r[3]+s[3])/2 or abs(r[2]-s[2])>=(r[4]+s[4])/2:continue
  lines=[]
  for a in roof_planes(r):
   for b in roof_planes(s):
    nx,ny,c=[a[k]-b[k] for k in range(3)];norm=nx*nx+ny*ny
    if norm<1e-10:continue
    p=Vector((-c*nx/norm,-c*ny/norm));t=Vector((-ny,nx)).normalized()
    seg=line_interval(p,t,domain(r,a)+domain(s,b))
    if not seg:continue
    if any((seg[0]-v[0]).length<.01 and (seg[1]-v[1]).length<.01 for v in lines):continue
    lines.append(seg)
    # Third roof masking can split a valley; retain only visible runs.
    aa,bb=seg;n=max(2,math.ceil((bb-aa).length/.12));run=[]
    for k in range(n+1):
     q=aa.lerp(bb,k/n);z=roof_z(r,q.x,q.y)
     visible=not any(o not in (r,s) and in_roof(o,q.x,q.y) and roof_z(o,q.x,q.y)>z+.02 for o in roofs)
     if visible:run.append(q)
     if (not visible or k==n) and run:
      if len(run)>1:VALLEYS.append((r,s,run[0],run[-1]))
      run=[]
  PAIRLINES[(i,j)]=lines

cut_faces=0
for i,r in enumerate(roofs):
 col=G.GROUPS['Roofs/'+r[0]]
 for o in list(col.objects):
  if o.type!='MESH' or not o.data.vertices:continue
  m=o.matrix_world.copy()
  for v in o.data.vertices:v.co=m@v.co
  o.matrix_world.identity()
  for j,s in enumerate(roofs):
   if i==j:continue
   pts=[v.co for v in o.data.vertices]
   if not pts:break
   xmin,xmax=min(v.x for v in pts),max(v.x for v in pts);ymin,ymax=min(v.y for v in pts),max(v.y for v in pts)
   sx0,sx1=s[1]-s[3]/2,s[1]+s[3]/2;sy0,sy1=s[2]-s[4]/2,s[2]+s[4]/2
   if xmax<=sx0 or xmin>=sx1 or ymax<=sy0 or ymin>=sy1:continue
   # If no height competition is possible, avoid touching the approved field.
   samples=[(x,y) for x in [max(xmin,sx0),min(xmax,sx1),(max(xmin,sx0)+min(xmax,sx1))/2] for y in [max(ymin,sy0),min(ymax,sy1),(max(ymin,sy0)+min(ymax,sy1))/2]]
   if all(roof_z(r,x,y)>roof_z(s,x,y)+.3 for x,y in samples):continue
   bm=bmesh.new();bm.from_mesh(o.data)
   cuts=[((1,0,0),(sx0,0,0)),((1,0,0),(sx1,0,0)),((0,1,0),(0,sy0,0)),((0,1,0),(0,sy1,0))]
   for aa,bb in PAIRLINES.get((max(i,j),min(i,j)),[]):
    d=(bb-aa).normalized();n=Vector((-d.y,d.x,0))
    # A 100 mm total open valley throat; metal lining extends beneath both tile ends.
    for offset in [-.05,.05]:cuts.append((n,Vector((aa.x,aa.y,0))+n*offset))
   for normal,point in cuts:
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=point,plane_no=normal)
   bad=[]
   for f in bm.faces:
    q=f.calc_center_median()
    if in_roof(s,q.x,q.y,-.00001):
     near=any(abs((q.x-aa.x)*(bb.y-aa.y)-(q.y-aa.y)*(bb.x-aa.x))/max((bb-aa).length,.001)<.049 for aa,bb in PAIRLINES.get((max(i,j),min(i,j)),[]))
     if roof_z(s,q.x,q.y)>roof_z(r,q.x,q.y)+.00001 or near:bad.append(f)
   cut_faces+=len(bad)
   if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
   bm.to_mesh(o.data);bm.free()

# Replace overlapping inherited aprons with a connected set based on the unchanged surfaces.
for n in list(G.GROUPS):
 if n.startswith('Roofs/') and n.endswith('/Flashing'):clear_group(n)
for r,s,a,b in VALLEYS:
 group('Roofs/'+r[0]+'/Flashing');d=(b-a).normalized();normal=Vector((-d.y,d.x));vs=[];fs=[];steps=max(2,math.ceil((b-a).length/.25))
 for k in range(steps+1):
  q=a.lerp(b,k/steps)
  for off in [-.21,-.15,0,.15,.21]:
   v=q+normal*off;z=max(roof_z(r,v.x,v.y),roof_z(s,v.x,v.y))+.012
   if abs(off)>.2:z+=.025
   vs.append((v.x,v.y,z))
 for k in range(steps):
  for h in range(4):v=k*5+h;fs.append((v,v+1,v+6,v+5))
 mesh('Continuous open valley lining with welted edges',vs,fs,'Galvanized')

# Finish higher-eave abutments where the adjacent lower roof reaches their vertical face.
for r in roofs:
 _,cx,cy,w,d,e,_=r
 edges=[((cx-w/2,cy-d/2),(cx+w/2,cy-d/2)),((cx+w/2,cy-d/2),(cx+w/2,cy+d/2)),((cx+w/2,cy+d/2),(cx-w/2,cy+d/2)),((cx-w/2,cy+d/2),(cx-w/2,cy-d/2))]
 for aa,bb in edges:
  a,b=Vector(aa),Vector(bb);t=(b-a).normalized();out=Vector((t.y,-t.x));N=math.ceil((b-a).length/.20)
  for k in range(N):
   p=a.lerp(b,(k+.5)/N)+out*.08
   lower=[s for s in roofs if s!=r and in_roof(s,p.x,p.y) and roof_z(s,p.x,p.y)<e-.10]
   if not lower:continue
   s=max(lower,key=lambda v:roof_z(v,p.x,p.y));lo=a.lerp(b,k/N);hi=a.lerp(b,(k+1)/N)
   group('Roofs/'+r[0]+'/Flashing');vs=[]
   for q in [lo,hi]:
    for off,z in [(-.018,e-.03),(.025,roof_z(s,q.x,q.y)+.035),(.25,roof_z(s,q.x+out.x*.25,q.y+out.y*.25)+.035)]:
     v=q+out*off;vs.append((v.x,v.y,z))
   mesh('Stepped abutment counterflashing',vs,[(0,1,4,3),(1,2,5,4)],'Galvanized')

# Complete small light wells at the existing Hall clerestories: no window changes.
REVEALS=[(-1,.25,1.75),(-1,-1.85,-.35),(1,-1.975,-.425)]
for side,ya,yb in REVEALS:
 roofname='Waiting' if side<0 else 'Preparation';r=next(r for r in roofs if r[0]==roofname)
 xinner=side*3.12;xouter=side*3.80;xa,xb=sorted([xinner,xouter]);ya-=.10;yb+=.10
 for o in list(G.GROUPS['Roofs/'+roofname].objects):
  if not o.data.vertices:continue
  pts=[v.co for v in o.data.vertices]
  if max(v.x for v in pts)<xa or min(v.x for v in pts)>xb or max(v.y for v in pts)<ya or min(v.y for v in pts)>yb:continue
  bm=bmesh.new();bm.from_mesh(o.data)
  for normal,point in [((1,0,0),(xa,0,0)),((1,0,0),(xb,0,0)),((0,1,0),(0,ya,0)),((0,1,0),(0,yb,0))]:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=point,plane_no=normal)
  bad=[f for f in bm.faces if xa<f.calc_center_median().x<xb and ya<f.calc_center_median().y<yb]
  if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
  bm.to_mesh(o.data);bm.free()
 group('Shell/Raised/HallRevealFinish')
 for y in [ya,yb]:
  z=roof_z(r,xouter,y)+.04
  mesh('Clerestory lead-lined end reveal',[(xinner,y,4.79),(xouter,y,4.79),(xouter,y,z),(xinner,y,5.35)],[(0,1,2,3)],'Galvanized')
 mesh('Clerestory flashed outer curb',[(xouter,ya,4.79),(xouter,yb,4.79),(xouter,yb,roof_z(r,xouter,yb)+.05),(xouter,ya,roof_z(r,xouter,ya)+.05)],[(0,1,2,3)],'Galvanized')
 box('Clerestory complete sill reveal',((xinner+xouter)/2,(ya+yb)/2,4.785),(abs(xouter-xinner),yb-ya,.04),'SS_Stone',.002)
 # Small open weep routes water from the lightwell onto the adjacent sloping roof.
 tube('Clerestory sill weep',[(xouter,(ya+yb)/2,4.79),(xouter+side*.12,(ya+yb)/2,4.77)],.014,'Galvanized',wall=.002,sides=8)

# Flush interior soffit return conceals the exposed end of the lower roof construction.
group('Shell/Raised/HallRevealFinish')
for side in [-1,1]:
 box('Entry soffit plaster return',(side*3.43,-5.625,4.10),(.16,6.25,.18),'InteriorPlaster',.003)
 box('Hall lower-band plaster return',(side*2.866,1.25,4.095),(.035,7.45,.13),'InteriorPlaster',.002)
 # Bridge the measured gap from the raised Hall band to the adjoining lower ceiling.
 end=3.3 if side<0 else 1.0
 box('Hall to wing continuous soffit',(side*3.31,(-2.5+end)/2,4.10),(.44,end+2.5,.08),'InteriorPlaster',0)
 if side<0:box('Hall low-band vertical soffit return',(-3.14,.40,3.955),(.04,5.8,.29),'InteriorPlaster',0)

# Gutters are supported from fascia independently of the downpipe's wall offset.
for n in list(G.GROUPS):
 if n.startswith('Roofs/') and n.endswith('/Rainwater'):clear_group(n)
DRAINS.clear();ROUTES=[]
def recessed_collection(r,a,b,out):
 # Narrow liner under the tile edge; box support and perimeter connection are concealed.
 a,b=Vector(a),Vector(b);out=Vector(out)
 if (b-a).length<.20:return
 group('Roofs/'+r[0]+'/Rainwater');vs=[]
 for k,c in enumerate([a,b]):
  z=r[5]-.025-.006*k
  for off,dz in [(-.035,.045),(-.025,0),(.070,0),(.080,.045)]:
   p=c+out*off;vs.append((p.x,p.y,z+dz))
 mesh('Recessed internal collection liner',vs,[(0,1,5,4),(1,2,6,5),(2,3,7,6)],'Galvanized')
 ROUTES.append(dict(roof=r[0],destination='perimeter rainwater via concealed box collection',type='recessed internal liner; no exposed cascade fittings'))

def gutter_run(r,a,b,out):
 name=r[0];a,b=Vector(a),Vector(b);out=Vector(out);t=(b-a).normalized();length=(b-a).length;e=r[5]
 if length<.45:return
 # A pier-safe outlet nearest the run's end; check both exposed and lower-roof destinations.
 preferred=.94
 if name=='Audition' and out.x<-.9:preferred=(a.y-12.05)/(a.y-b.y)
 if name=='Hall' and out.x>.9:preferred=(-.30-a.y)/(b.y-a.y)
 choices=sorted([i/100 for i in range(3,98)],key=lambda f:abs(f-preferred))
 chosen=None
 for frac in choices:
  p=a.lerp(b,frac);q=p-out*.13
  if p.y<-9:
   if name=='Entrance' and abs(p.x)<3.20:continue
   if name!='Entrance' and abs(p.x)<6.30:continue
  if name=='WestService' and p.y>14 and -8.90<p.x<-6.30:continue
  if any(abs((q-Vector(v['center'])).x)<v['width']/2+.3 and abs(q.y-v['center'][1])<.7 for v in DOORS if v.get('world')):continue
  lower=[s for s in roofs if s!=r and in_roof(s,q.x,q.y) and roof_z(s,q.x,q.y)<e-.15]
  if lower:chosen=(frac,p,q,max(lower,key=lambda v:roof_z(v,q.x,q.y)));break
  # Use the actual nearby exterior wall, not an assumed eave offset.
  walls=[]
  for w0 in WALLS:
   if w0['interior']:continue
   u,v=Vector(w0['a']),Vector(w0['b']);dd=v-u;tt=dd.normalized();nn=Vector((tt.y,-tt.x))
   if nn.dot(out)<.95:continue
   f=max(0,min(1,(q-u).dot(dd)/dd.length_squared));wall=u+dd*f
   if (wall-p).length<.95:walls.append(wall)
  wall=min(walls,key=lambda v:(v-p).length) if walls else p-out*.45
  q=wall+out*.36
  if name=='Entrance' and out.y<-.9:q.y=-9.69 # 151 mm clear of projecting portal pier, including pipe radius
  if any(xa+.02<q.x<xb-.02 and ya+.02<q.y<yb-.02 for _,xa,xb,ya,yb in volumes):continue
  if any(abs((q-Vector(win['center'])).dot(Vector((math.cos(win['angle']),math.sin(win['angle'])))))<win['w']/2+.28 and abs((q-Vector(win['center'])).dot(Vector((-math.sin(win['angle']),math.cos(win['angle'])))))<.75 for win in WINDOWS):continue
  chosen=(frac,p,q,None);break
 if chosen is None:
  # Short residual eave beside an adjoining mass need not acquire another pipe
  # across a window. Continue its liner into the concealed perimeter collection.
  recessed_collection(r,a-out*.14,b-out*.14,out);return
 frac,p,q,lower=chosen
 if lower:
  recessed_collection(r,a-out*.14,b-out*.14,out);return
 # Consolidate perimeter outlets instead of assigning a ground drop to every edge.
 # Front gutters feed boxed corner returns; the raised entrance uses concealed
 # collection behind its cornice into the adjoining wing system.
 shared=((name in ('Entrance','Waiting','Preparation','CommonBay') and out.y<-.9)
         or (name=='WestService' and abs(out.x)>.9)
         or (name=='Audition' and (out.y<-.9 or out.x<-.9)))
 if shared:
  frac=0.0 if name=='Waiting' else 1.0
  p=a.lerp(b,frac)
 group('Roofs/'+name+'/Rainwater');base=e-.045
 N=max(4,math.ceil(length/.30));vs=[];fs=[]
 for k in range(N+1):
  f=k/N;c=a.lerp(b,f);z=base+min(.026,abs(f-frac)*length*.006)
  for rad in [.090,.083]:
   for j in range(13):ang=math.pi+j*math.pi/12;v=c+out*(rad*math.cos(ang));vs.append((v.x,v.y,z+rad*math.sin(ang)))
 for k in range(N):
  for ring in range(2):
   for j in range(12):v=k*26+ring*13+j;fs.append((v,v+1,v+27,v+26))
  for j in [0,12]:v=k*26+j;fs.append((v,v+26,v+39,v+13))
 for end in [0,N]:
  for j in range(12):v=end*26+j;fs.append((v,v+13,v+14,v+1))
 mesh('Pitched half-round rainwater gutter',vs,fs,'Galvanized')
 for c in [a,b]:
  z=base+min(.026,(c-p).length*.006)
  verts=[(c.x,c.y,z)]+[(c.x+out.x*.083*math.cos(math.pi+j*math.pi/12),c.y+out.y*.083*math.cos(math.pi+j*math.pi/12),z+.083*math.sin(math.pi+j*math.pi/12)) for j in range(13)]
  mesh('Closed gutter end cap',verts,[tuple(range(14))],'Galvanized')
 for k in range(math.ceil(length/1.15)+1):
  c=a.lerp(b,k/math.ceil(length/1.15));anchor=c-out*.14;z=base+min(.026,(c-p).length*.006)
  tube('Fascia-supported gutter cradle',[(anchor.x,anchor.y,z-.015),(anchor.x,anchor.y,z-.10),(c.x,c.y,z-.105),(c.x+out.x*.082,c.y+out.y*.082,z-.012)],.010,'Steel',sides=8)
  cylinder('Fascia bracket anchor plate',(anchor.x,anchor.y,z-.052),.045,.016,'Steel',(out.x,out.y,0),segments=12)
  cylinder('Fascia fixing screw',(anchor.x+out.x*.01,anchor.y+out.y*.01,z-.052),.008,.014,'Brass',(out.x,out.y,0),segments=8)
 if shared:
  # Covered terminal throat under the fascia; no visible pipe, socket or gully.
  c=p-out*.06
  box('Concealed corner collection throat',(c.x,c.y,base-.045),(.10,.10,.075),'Galvanized',.003)
  destinations={'Entrance':'concealed entrance cornice to wing collection','Waiting':'west side outlet','Preparation':'east side outlet','CommonBay':'west side outlet','WestService':'rear outlet','Audition':'east and rear outlets'}
  ROUTES.append(dict(roof=name,destination=destinations[name],type='boxed fascia return; shared perimeter outlet'))
  return
 # Open outlet bell and socket; no floating collector boxes.
 tube('Gutter outlet socket',[(p.x,p.y,base-.06),(p.x,p.y,base-.22)],.057,'Galvanized',wall=.004,sides=16)
 if lower:
  target=roof_z(lower,q.x,q.y);end=q+out*.28
  if name=='Hall' and out.x>.9:end=Vector((p.x,p.y+.55))
  local_collector=(name=='Hall' and out.x>.9) or (name=='Audition' and out.x<-.9)
  endz=roof_z(lower,end.x,end.y)+(.25 if local_collector else .10)
  pipepts=[(p.x,p.y,base-.18),(q.x,q.y,max(target+.16,base-.35,endz+.05 if local_collector else -100)),(end.x,end.y,endz)]
  tube('Supported upper-roof collector shoe',pipepts,.049,'Galvanized',wall=.004,sides=12)
  # Spreader apron laps the lower clay surface in its downhill direction.
  grad=Vector((roof_z(lower,end.x+.05,end.y)-roof_z(lower,end.x-.05,end.y),roof_z(lower,end.x,end.y+.05)-roof_z(lower,end.x,end.y-.05)))
  down=-grad.normalized() if grad.length>.001 else out;cross=Vector((-down.y,down.x));vv=[]
  for d0 in [-.1,.50]:
   for s0 in [-.15,.15]:
    v=end+down*d0+cross*s0;vv.append((v.x,v.y,roof_z(lower,v.x,v.y)+(.122 if local_collector else .035)))
  mesh('Collector discharge spreader apron',vv,[(0,1,3,2)],'Galvanized')
  beam('Upper shoe fascia stay',(p.x,p.y,base-.24),(p.x-out.x*.15,p.y-out.y*.15,base-.09),.025,.025,'Steel')
  ROUTES.append(dict(roof=name,destination=lower[0],outlet=list(p),type='supported spreader onto lower roof'))
 else:
  end=q+out*.20;pipepts=[(p.x,p.y,base-.18),(p.x,p.y,base-.30),(q.x,q.y,base-.52),(q.x,q.y,.32),(end.x,end.y,.20)]
  tube('Socketed downpipe and open shoe',pipepts,.049,'Galvanized',wall=.004,sides=12)
  for h in [.65,2.15,3.65,5.1]:
   if h>base-.55:continue
   wall=q-out*.20
   if name=='Entrance' and out.y<-.9:wall.y=-9.49 # actual front of the projecting pilaster
   tube('Pipe retaining collar',[(q.x+.053*math.cos(k*math.pi/8),q.y+.053*math.sin(k*math.pi/8),h) for k in range(17)],.009,'Steel',sides=8)
   beam('Downpipe anchored wall standoff',(q.x,q.y,h),(wall.x,wall.y,h),.026,.026,'Steel')
   cylinder('Masonry fixing rosette',(wall.x,wall.y,h),.052,.022,'Steel',(out.x,out.y,0),segments=12)
   cylinder('Wall anchor bolt',(wall.x+out.x*.016,wall.y+out.y*.016,h),.010,.026,'Brass',(out.x,out.y,0),segments=8)
  box('Recessed drain gully',(end.x,end.y,.035),(.36,.36,.07),'Concrete',.005)
  box('Gully dark throat',(end.x,end.y,.073),(.25,.25,.009),'Black',.001)
  for k in [-2,-1,0,1,2]:box('Gully grate bar',(end.x+k*.046,end.y,.082),(.014,.27,.018),'Steel',.002)
  DRAINS.append(dict(roof=name,top=[q.x,q.y,base-.52],bottom=[q.x,q.y,.32]))
  ROUTES.append(dict(roof=name,destination='ground gully',outlet=list(p),type='wall-supported downpipe with open shoe'))

for r in roofs:
 _,x,y,w,d,e,_=r
 edges=[((x-w/2,y-d/2),(x+w/2,y-d/2),(0,-1)),((x+w/2,y-d/2),(x+w/2,y+d/2),(1,0)),((x+w/2,y+d/2),(x-w/2,y+d/2),(0,1)),((x-w/2,y+d/2),(x-w/2,y-d/2),(-1,0))]
 for aa,bb,nn in edges:
  a,b=Vector(aa),Vector(bb);out=Vector(nn);N=math.ceil((b-a).length/.15);start=0;previous=None
  for k in range(N+1):
   state=None
   if k<N:
    p=a.lerp(b,(k+.5)/N)+out*.10
    covered=any(s!=r and in_roof(s,p.x,p.y) and roof_z(s,p.x,p.y)>e-.28 for s in roofs)
    # The entrance front is an exposed perimeter eave. Nearby wing tolerances
    # must not split it and relocate its already-corrected front-right pipe.
    pad=0 if r[0]=='Entrance' and out.y<-.9 else .22
    adjoining=any(s!=r and in_roof(s,p.x,p.y,pad) for s in roofs)
    state='covered' if covered else ('internal' if adjoining else 'perimeter')
   if state!=previous:
    if previous=='internal':recessed_collection(r,a.lerp(b,start/N),a.lerp(b,k/N),out)
    elif previous=='perimeter':gutter_run(r,a.lerp(b,start/N)+out*.14,a.lerp(b,k/N)+out*.14,out)
    start=k;previous=state

print('A3.1 pendant mounts and restroom hardware',flush=True)
PENDANT_QA=[]
for name,c in G.GROUPS.items():
 if not name.startswith(('Ceilings/Pendant','Ceilings/SupportPendant')):continue
 objects=list(c.objects);rose=next(o for o in objects if o.name.startswith(('Ceiling rose','Pendant mounting rose')))
 points=[rose.matrix_world@v.co for v in rose.data.vertices];x=sum(p.x for p in points)/len(points);y=sum(p.y for p in points)/len(points)
 room=next(v for v in ceiling_rooms if v[1]<x<v[2] and v[3]<y<v[4]);underside=room[-1]-.04
 top=max(p.z for p in points);shiftz=underside-top
 for v in rose.data.vertices:v.co=rose.matrix_world@v.co+Vector((0,0,shiftz))
 rose.matrix_world.identity();bottom=min(v.co.z for v in rose.data.vertices)
 stems=[o for o in objects if o.name.startswith(('Pendant suspension','Extended pendant ceiling support','Pendant rod'))]
 low=min((o.matrix_world@v.co).z for o in stems for v in o.data.vertices)
 for o in stems:bpy.data.objects.remove(o,do_unlink=True)
 group(name);cylinder('Ceiling-seated continuous pendant stem',(x,y,(low+bottom+.008)/2),.012,bottom+.008-low,'Steel',segments=12)
 PENDANT_QA.append(dict(group=name,ceiling=underside,rose_top=max(v.co.z for v in rose.data.vertices),stem_top=bottom+.008))

for i,x in enumerate([7.79,9.49]):
 group('Furnishings/Washroom')
 # Fill the 55 mm gap with a rebated jamb, keeping approved door size and position.
 for side in [-1,1]:box('Cubicle rebated fixed jamb',(x+side*.555,-6.20,1.46),(.065,.08,1.86),'SS_Sage',.005)
 for z in [.62,1.52,2.00]:
  box('Cubicle fixed hinge leaf',(x-.561,-6.145,z),(.083,.014,.13),'Brass',.002)
  for dz in [-.042,.042]:cylinder('Cubicle jamb hinge screw',(x-.568,-6.133,z+dz),.007,.012,'Steel',(0,1,0),segments=8)
 box('Privacy bolt keep',(x+.548,-6.263,1.63),(.065,.027,.075),'Brass',.004)
 group('Doors/Cubicle'+str(i))
 box('Privacy slide-bolt backplate',(x+.405,-6.269,1.63),(.17,.016,.073),'Brass',.004)
 cylinder('Sliding privacy bolt',(x+.467,-6.294,1.63),.012,.17,'Brass',(1,0,0),segments=12)
 tube('Privacy thumb turn',[(x+.414,-6.294,1.63),(x+.414,-6.325,1.63),(x+.414,-6.325,1.675)],.009,'Brass',sides=8)
 cylinder('Exterior occupancy escutcheon',(x+.405,-6.126,1.63),.033,.012,'Brass',(0,1,0),segments=16)
 box('Occupied indicator enamel',(x+.405,-6.118,1.63),(.031,.008,.015),'SS_Sage',.002)

# New abutment sheets terminate in the enclosing masonry, never below occupied ceilings.
for name,col in list(G.GROUPS.items()):
 if not name.startswith('Roofs/') or not name.endswith('/Flashing'):continue
 for o in list(col.objects):
  for _,xa,xb,ya,yb,zz in ceiling_rooms:
   if not o.data.vertices:break
   pts=[v.co for v in o.data.vertices];xa-=.10;xb+=.10;ya-=.10;yb+=.10
   if min(v.z for v in pts)>=zz+.045 or max(v.x for v in pts)<=xa or min(v.x for v in pts)>=xb or max(v.y for v in pts)<=ya or min(v.y for v in pts)>=yb:continue
   bm=bmesh.new();bm.from_mesh(o.data)
   for axis,value in [(0,xa),(0,xb),(1,ya),(1,yb),(2,zz+.045)]:
    normal=[0,0,0];normal[axis]=1;point=[0,0,0];point[axis]=value
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=point,plane_no=normal)
   bad=[f for f in bm.faces if xa<f.calc_center_median().x<xb and ya<f.calc_center_median().y<yb and f.calc_center_median().z<zz+.045]
   if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
   bm.to_mesh(o.data);bm.free()
assert len(PENDANT_QA)==12
assert all(abs(p['ceiling']-p['rose_top'])<.00001 and p['stem_top']<p['rose_top'] for p in PENDANT_QA)
assert set(r[0] for r in roofs)==set(r['roof'] for r in ROUTES)
QA['a31']=dict(pendants=PENDANT_QA,roof_routes=ROUTES,valley_segments=len(VALLEYS),junction_faces_removed=cut_faces,scope='six known defects only')
print('A3.1 finished: twelve ceiling mounts, nine roofs routed, local junctions',flush=True)
