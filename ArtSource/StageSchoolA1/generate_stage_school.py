"""Stage School A1. New architecture; shared period palette/loose fixtures only.
Blender 5 --background --factory-startup --python this_file. Metric, front -Y.
Authoritative source -> existing semantic mesh-packet pipeline -> isolated Unity review.
"""
import bpy, bmesh, math, json, sys, random
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent; ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'ArtSource/PeriodEnvironment1930'))
import geometry as G
from geometry import mesh, tube, cylinder, ball, beam
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'; bpy.context.scene.unit_settings.scale_length=1
bpy.context.preferences.filepaths.save_version=0
G.GROUPS.clear();G.MATS.clear()
CUSTOM={
 'SS_Stucco':([.64,.56,.44],0,.86,'stucco'),
 'SS_Stone':([.70,.625,.51],0,.77,'stone'),
 'SS_Tile':([.34,.105,.045],0,.78,'stone'),
 'SS_TileLight':([.38,.123,.055],0,.78,'stone'),
 'SS_TileDark':([.30,.087,.037],0,.81,'stone'),
 'SS_Walnut':([.105,.050,.024],0,.48,'timber'),
 'SS_Sage':([.125,.17,.125],0,.91,'cloth'),
 'SS_Curtain':([.18,.029,.024],0,.89,'cloth'),
 'SS_Ivory':([.73,.67,.53],0,.71,'stone'),
 'SS_Lamp':([.92,.71,.39],0,.22,''),
 'SS_Leather':([.125,.055,.025],0,.61,'cloth'),
 'SS_Terrazzo':([.45,.40,.315],0,.49,'concrete'),
 'SS_Headshots':([1,1,1],0,.88,'headshots'),
 'SS_FloorOak':([.25,.15,.073],0,.64,'timber'),
 'SS_FloorOakLight':([.27,.165,.084],0,.64,'timber'),
 'SS_Bronze':([.065,.048,.029],.70,.37,'metal'),
 'SS_ClearGlass':([.14,.18,.17],0,.15,'')}
G.PALETTE.update(CUSTOM); G.init_materials()
portrait_path=ROOT/'Assets/SilverScreen/Environment/StageSchoolA1/Textures/PerformerHeadshots.png'
if portrait_path.exists():
 mat=G.MATS['SS_Headshots'];tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(portrait_path));mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
fixtures=[]; pivots={}; rng=random.Random(1930)
sys.path.insert(0,str(HERE))
import refinements as R
R.atlas()
equipment=[dict(asset="StudioCamera1930",group="Furnishings/Audition/Camera",position=[8.6,.39,4.45],yaw=-33),dict(asset="StudioLamp1930",group="Furnishings/Audition/StudioLamp0",position=[4.15,.39,5.1],yaw=54),dict(asset="StudioLamp1930",group="Furnishings/Audition/StudioLamp1",position=[9.65,.39,5.1],yaw=-54)]
def group(n):G.group(n)
def box(n,p,s,mat='SS_Stone',bevel=.008):
 x,y,z=p;a,b,c=[v/2 for v in s]
 o=mesh(n,[(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)
 if bevel:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(s)*.2),segments=2,affect='EDGES',clamp_overlap=True);bm.to_mesh(o.data);bm.free()
 return o
def cylinder(n,p,r,h,mat='Steel',axis=(0,0,1),g=None,segments=20):
 q=Vector(axis).to_track_quat('Z','Y');origin=Vector(p)
 vs=[origin+q@Vector((r*math.cos(i*2*math.pi/segments),r*math.sin(i*2*math.pi/segments),z)) for z in [-h/2,h/2] for i in range(segments)]
 fs=[tuple(reversed(range(segments))),tuple(range(segments,2*segments))]+[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
 o=mesh(n,vs,fs,mat,g)
 for f in list(o.data.polygons)[2:]:f.use_smooth=True
 return o
def ball(n,p,s,mat,g=None):
 bm=bmesh.new();bmesh.ops.create_uvsphere(bm,u_segments=20,v_segments=12,radius=1)
 for v in bm.verts:v.co=Vector((v.co.x*s[0]+p[0],v.co.y*s[1]+p[1],v.co.z*s[2]+p[2]))
 me=bpy.data.meshes.new(n);bm.to_mesh(me);bm.free();o=bpy.data.objects.new(n,me);G.GROUPS[g or G.CURRENT].objects.link(o);me.materials.append(G.MATS[mat])
 for f in me.polygons:f.use_smooth=True
 return o
def beam(n,a,b,w=.06,d=.06,mat='Timber',g=None):
 a,b=Vector(a),Vector(b);o=box(n,(0,0,0),(w,d,(b-a).length),mat,.004);q=(b-a).to_track_quat('Z','Y')
 for v in o.data.vertices:v.co=(a+b)/2+q@v.co
 return o
def text(body,p,size,width,mat='Brass',name='Applied lettering'):
 o=G.text(name,body,p,size,width,mat)
 return o
def serif(body,p,size,width,mat='Steel'):
 curve=bpy.data.curves.new('Cast serif letters','FONT');curve.body=body;curve.align_x='CENTER';curve.size=size;curve.extrude=.012;curve.bevel_depth=.0015;curve.resolution_u=8
 curve.font=bpy.data.fonts.load('C:/Windows/Fonts/times.ttf');o=bpy.data.objects.new('Individual cast letters '+body,curve);G.GROUPS[G.CURRENT].objects.link(o);o.location=p;o.rotation_euler=(math.pi/2,0,0);curve.materials.append(G.MATS[mat]);bpy.context.view_layer.update()
 if o.dimensions.x>width:o.scale.x=width/o.dimensions.x
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o.select_set(False)
 return o
def place(name,g,p,angle=0):fixtures.append(dict(asset=name,group=g,position=[p[0],p[2],p[1]],yaw=180-angle))
def xwall(g,y,a,b,height,holes=(),mat='SS_Stucco',th=.32,z0=.36):
 group(g);xs=sorted(set([a,b]+[v for h in holes for v in h[:2]]));zs=sorted(set([z0,height]+[v for h in holes for v in h[2:]]))
 for x0,x1 in zip(xs,xs[1:]):
  for q,r in zip(zs,zs[1:]):
   if any(h[0]<(x0+x1)/2<h[1] and h[2]<(q+r)/2<h[3] for h in holes):continue
   box('Masonry pier or spandrel',((x0+x1)/2,y,(q+r)/2),(x1-x0,th,r-q),mat,0)
 for x0,x1 in zip(xs,xs[1:]):
  if not any(h[0]<(x0+x1)/2<h[1] and h[2]<=z0 for h in holes):
   box('Stone base course',((x0+x1)/2,y,.53),(x1-x0,th+.06,.34),'SS_Stone')
   box('Interior timber skirting',((x0+x1)/2,y+(.18 if y<0 else -.18),.45),(x1-x0,.045,.18),'SS_Walnut')
def rotate_group(g):
 # Build side walls in local x/y then rotate 90 degrees, mapping x -> y.
 for o in G.GROUPS[g].objects:
  for v in o.data.vertices:v.co.x,v.co.y=-v.co.y,v.co.x
def window(g,x,y,z,w=1.5,h=2.1,rot=0):
 group(g);before=set(G.GROUPS[g].objects)
 box('Recessed glass',(x,y,z+h/2),(w-.14,.025,h-.16),'SS_ClearGlass',0)
 for dx in [-w/2,w/2]:
  box('Deep stone reveal',(x+dx,y,z+h/2),(.15,.42,h+.19),'SS_Stone')
  box('Bronze casement frame',(x+dx*.89,y-.055,z+h/2),(.065,.09,h-.04),'Steel')
 for zz in [z,z+h]:
  box('Stone lintel or sill',(x,y-.07,zz),(w+.34,.5,.13),'SS_Stone')
  box('Bronze horizontal frame',(x,y-.065,zz+(.06 if zz==z else -.06)),(w,.075,.065),'Steel')
 box('Projecting sill drip',(x,y-.20,z-.045),(w+.43,.33,.06),'SS_Stone')
 for dx in [-w/6,w/6]:box('Casement mullion',(x+dx,y-.08,z+h/2),(.035,.065,h),'Steel',.004)
 for t in [.34,.67]:box('Casement transom',(x,y-.08,z+h*t),(w,.065,.033),'Steel',.003)
 for dx in [-.13,.13]:cylinder('Window latch',(x+dx,y-.13,z+h*.45),.019,.09,'Brass')
 if rot:
  for o in set(G.GROUPS[g].objects)-before:
   # All coordinates are mesh coordinates except the cylinder helper.
   bpy.context.view_layer.update()
   m=o.matrix_world.copy()
   for v in o.data.vertices:
    p=m@v.co;v.co=(-p.y,p.x,p.z)
   o.matrix_world.identity()
def arch_band(n,cx,y,spring,ri,ro,depth,mat,steps=48):
 vs=[]
 for yy in [y-depth/2,y+depth/2]:
  for rr in [ri,ro]:
   vs.extend((cx+rr*math.cos(i*math.pi/steps),yy,spring+rr*math.sin(i*math.pi/steps)) for i in range(steps+1))
 k=steps+1;fs=[]
 for i in range(steps):fs.extend([(i,i+1,k+i+1,k+i),(2*k+i,3*k+i,3*k+i+1,2*k+i+1),(i,2*k+i,2*k+i+1,i+1),(k+i,k+i+1,3*k+i+1,3*k+i)])
 fs.extend([(0,k,3*k,2*k),(steps,2*k+steps,3*k+steps,k+steps)])
 return mesh(n,vs,fs,mat)
def door(name,x,y,width=1.12,h=2.34,angle=0,glazed=False):
 g='Doors/'+name;group(g);pivots[g]=[x-width/2,.36,y]
 if not glazed:box('Hinged timber leaf',(x,y,.36+h/2),(width,.08,h),'SS_Walnut',.012)
 for zz,hh in ([(.78,.53)] if glazed else [(.78,.53),(1.60,.88),(2.35,.30)]):
  box('Raised field panel',(x,y-.051,zz),(width-.22,.028,hh),'TimberDark',.008)
 if glazed:box('Door glazing',(x,y-.07,2.01),(width-.16,.025,1.58),'SS_ClearGlass',0)
 for dx in [-width/2+.045,width/2-.045]:box('Door stile',(x+dx,y-.058,.36+h/2),(.085,.09,h),'SS_Bronze' if glazed else 'SS_Walnut')
 for zz in [.42,1.19,.36+h-.04]:box('Door rail',(x,y-.062,zz),(width,.09,.085),'SS_Bronze' if glazed else 'SS_Walnut')
 box('Lock escutcheon',(x+width*.34,y-.065,1.39),(.055,.025,.20),'Brass')
 tube('Supported pull handle',[(x+width*.34,y-.07,1.33),(x+width*.34,y-.15,1.33),(x+width*.34,y-.15,1.53),(x+width*.34,y-.07,1.53)],.014,'Brass')
 for z in [.67,1.55,2.45]:cylinder('Hinge barrel',(x-width/2,y-.03,z),.018,.14,'Brass')
 if angle:
  pivot=Vector((x-width/2,y,.36));from mathutils import Matrix
  rotation=Matrix.Rotation(math.radians(angle),4,'Z')
  for o in G.GROUPS[g].objects:
   bpy.context.view_layer.update();m=o.matrix_world.copy()
   for v in o.data.vertices:v.co=pivot+rotation@(m@v.co-pivot)
   o.matrix_world.identity()
def frame(g,x,y,w=1.2):
 group(g)
 for dx in [-w/2,w/2]:box('Door architrave',(x+dx,y,1.57),(.11,.25,2.42),'SS_Walnut')
 box('Door header',(x,y,2.78),(w+.16,.25,.13),'SS_Walnut')
def lantern(g,x,y,z):
 group(g)
 box('Lantern wall backplate',(x,y,z+.12),(.13,.06,.33),'Steel')
 tube('Forged lantern bracket',[(x,y,z+.23),(x,y-.32,z+.23),(x,y-.36,z+.14)],.025,'Steel')
 cy=y-.36
 box('Amber lantern diffuser',(x,cy,z-.15),(.20,.20,.33),'SS_Lamp',.012)
 for dx in [-.12,.12]:
  for dy in [-.12,.12]:beam('Lantern corner rib',(x+dx,cy+dy,z-.36),(x+dx,cy+dy,z+.05),.018,.018,'Steel')
 box('Lantern foot',(x,cy,z-.37),(.29,.29,.045),'Steel')
 vs=[(x+dx*.18,cy+dy*.18,z+.05) for dx,dy in [(-1,-1),(1,-1),(1,1),(-1,1)]]+[(x,cy,z+.23)]
 mesh('Pitched lantern cap',vs,[(0,3,2,1),(0,1,4),(1,2,4),(2,3,4),(3,0,4)],'Steel')
 cylinder('Lantern finial',(x,cy,z+.24),.029,.07,'Brass')

# Finished floor is .36 above the authoritative ground datum. Foundation goes to 0.
group('Structure/Foundation');box('Continuous founded plinth',(0,0,.16),(22,18,.32),'Concrete',.02)
group('Interior/Floors');box('Stone entrance hall',(0,0,.335),(21.65,17.65,.05),'SS_Terrazzo',.005)
# Timber rooms use visible board construction, with small grain-bearing bevels.
for name,a,b,c,d in [('Waiting',-10.65,-3.1,-8.65,-1.0),('Audition',3.1,10.65,1.2,8.65),('Interview',-2.7,2.7,2.2,8.65),('Commons',-10.65,-3.1,-.8,3.75)]:
 group('Interior/Floors/'+name)
 nx=math.ceil((b-a)/.18);w=(b-a)/nx
 for i in range(nx):
  start=c
  while start<d-.01:
   end=min(d,start+rng.uniform(1.5,2.7));box('Tongue and groove oak board',(a+(i+.5)*w,(start+end)/2,.368),(w-.002,end-start-.002,.026),'SS_FloorOak' if rng.random()<.75 else 'SS_FloorOakLight',.001);start=end
group('Interior/Floors/HallInlay')
for x in [-2.78,2.78]:box('Hall stone border',(x,-3.1,.366),(.075,11.1,.011),'SS_Stone',.002)
for x in [-2,-1,0,1,2]:box('Terrazzo brass divider',(x,-3.1,.366),(.008,11.1,.007),'Brass',0)
for y in [-8,-7,-6,-5,-4,-3,-2,-1,0,1,2]:box('Terrazzo transverse divider',(0,y,.366),(5.5,.008,.007),'Brass',0)
# Entrance steps and red runner are fixed entrance finish, not landscaping.
group('Structure/EntranceSteps')
for y,z,w,dep in [(-9.85,.06,6.4,1.7),(-9.55,.12,6.0,1.7),(-9.25,.18,5.6,1.7)]:
 box('Bullnose entrance tread',(0,y,z),(w,dep,2*z),'SS_Stone',.025)
 box('Red entrance runner',(0,y,2*z+.007),(1.6,dep+.008,.014),'SS_Curtain',.002)
 box('Runner riser return',(0,y-dep/2-.004,2*z-.06),(1.6,.009,.12),'SS_Curtain',.001)
for x in [-2.70,2.70]:
 box('Entrance cheek plinth',(x,-9.2,.44),(.42,1.9,.88),'SS_Stone')
 box('Cheek coping',(x,-9.2,.91),(.51,2,.10),'SS_Stone')
 tube('Bronze approach rail',[(x,-10.0,.96),(x,-9.2,1.30),(x,-8.45,1.30)],.033,'Steel')
 for y,z in [(-9.9,.99),(-8.55,1.30)]:tube('Rail socket upright',[(x,y,.95),(x,y,z)],.026,'Steel')

# Side wings, real recessed openings, elevated centre. No wall behind any window.
for side in [-1,1]:
 a,b=(-11,-3.6) if side<0 else (3.6,11);wx=side*8.4
 xwall('Shell/Front',-8.8,a,b,4.65,[(wx-.84,wx+.84,1.26,3.51)])
 window('Shell/Front/Windows',wx,-8.83,1.26,1.68,2.25)
xwall('Shell/Rear',8.84,-11,11,4.65,[(-9.6,-8.1,1.55,3.6),(-5.8,-4.3,1.55,3.6),(-1.65,-.35,1.4,3.65),(1.1,2.3,.36,2.75),(5.4,6.9,1.55,3.6),(8,9.5,1.55,3.6)])
for x in [-8.85,-5.05,-1,6.15,8.75]:window('Shell/Rear/Windows',x,8.86,1.55,1.5,2.05)
frame('Shell/Rear',1.7,8.86,1.2);door('RearService',1.7,8.87,1.12,2.34)
group('Structure/RearStep');box('Rear landing',(1.7,9.4,.18),(2.2,1.0,.36),'SS_Stone');box('Rear step',(1.7,10,.08),(2.5,.45,.16),'SS_Stone')
for side,g in [(-1,'Shell/Left'),(1,'Shell/Right')]:
 # xwall rotated yields x=-y, y=x
 holes=[(y-.78,y+.78,1.3,3.55) for y in [-6,-1.8,2.4,6.5]]
 xwall(g,-side*10.84,-8.8,8.8,4.65,holes);rotate_group(g)
 for y in [-6,-1.8,2.4,6.5]:window(g+'/Windows',y,-side*10.84,1.3,1.56,2.25,90)

group('Shell/Front/Portal')
# Curved opening spring at 2.9, radius 1.65, cut out of actual front masonry.
for x in [-2.64,2.64]:box('Portal flanking pier',(x,-9.03,2.94),(1.98,.64,5.16),'SS_Stucco')
box('Central sign frieze',(0,-9.03,5.25),(7.25,.64,1.1),'SS_Stucco')
# Curved spandrel fills between semicircle and frieze without opaque rectangle.
for i in range(48):
 a=i*math.pi/48;b=(i+1)*math.pi/48;x0=1.65*math.cos(b);x1=1.65*math.cos(a);z0=2.9+1.65*math.sin(b);z1=2.9+1.65*math.sin(a)
 vv=[(xx,yy,zz) for yy in [-9.35,-8.71] for xx,zz in [(x0,z0),(x1,z1),(x1,4.71),(x0,4.71)]]
 mesh('Curved portal spandrel',vv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'SS_Stucco')
for ri,ro,yy,dep in [(1.65,1.80,-9.35,.15),(1.82,2.06,-9.37,.22),(2.08,2.16,-9.37,.12)]:arch_band('Concentric dressed archivolt',0,yy,2.9,ri,ro,dep,'SS_Stone')
for x in [-1.93,1.93]:
 box('Portal jamb',(x,-9.38,1.62),(.28,.23,2.54),'SS_Stone')
 box('Jamb pedestal',(x,-9.39,.63),(.43,.29,.54),'SS_Stone')
 box('Impost capital',(x,-9.39,2.91),(.46,.30,.14),'SS_Stone')
for i in range(13):
 a=(i+.5)*math.pi/13
 tube('Voussoir radial mortar',[(1.83*math.cos(a),-9.488,2.9+1.83*math.sin(a)),(2.055*math.cos(a),-9.488,2.9+2.055*math.sin(a))],.006,'Mortar',sides=6)
box('Central keystone',(0,-9.52,4.94),(.31,.23,.45),'SS_Stone',.025)
group('Shell/Front/Signage');serif('STAGE SCHOOL',(0,-9.373,5.10),.62,6.28,'Steel')
for x in [-3.43,3.43]:
 group('Shell/Front');box('Portal pilaster',(x,-9.12,3.02),(.23,.74,5.3),'SS_Stone')
 for z,w,d,h in [(5.74,7.5,.88,.14),(5.9,7.7,.94,.10),(4.89,7.30,.72,.08)]:
  if x<0:box('Central cornice',(0,-9.03,z),(w,d,h),'SS_Stone')
# Fixed fanlight and bronze tracery occupy the real opening above entrance leaves.
group('Shell/Front/Fanlight');arch_band('Bronze fanlight surround',0,-9.03,2.90,1.57,1.65,.11,'Steel')
vs=[(0,-9.01,2.90)]+[(1.57*math.cos(i*math.pi/48),-9.01,2.9+1.57*math.sin(i*math.pi/48)) for i in range(49)]
mesh('Semicircular fanlight glass',vs,[(0,i+1,i+2) for i in range(48)],'SS_ClearGlass')
for r in [.55,1.05]:arch_band('Fanlight concentric tracery',0,-9.055,2.9,r-.017,r+.017,.028,'Steel',48)
for i in range(1,10):
 a=i*math.pi/10;tube('Fanlight radial tracery',[(0,-9.05,2.90),(1.6*math.cos(a),-9.05,2.9+1.6*math.sin(a))],.018,'Steel')
box('Fanlight transom',(0,-9.04,2.90),(3.3,.15,.1),'Steel')
for side in [-1,1]:
 door('Entrance'+('Left' if side<0 else 'Right'),side*.79,-9.02,1.52,2.49,0,True)
 lantern('Shell/Front/EntranceLanterns',side*2.83,-9.43,2.97)
 group('Shell/Front/Banners');x=side*5.30
 box('Bronze framed talent banner',(x,-9.01,2.57),(1.36,.12,2.96),'Steel',.014)
 for dx in [-.61,.61]:box('Banner gold edging',(x+dx,-9.08,2.57),(.019,.012,2.80),'Brass',.002)
 for z in [1.18,3.96]:box('Banner gold edging',(x,-9.08,z),(1.23,.012,.019),'Brass',.002)
 for i,word in enumerate(['TALENT','DISCIPLINE','OPPORTUNITY'] if side<0 else ['ACT','DIRECT','BELONG']):serif(word,(x,-9.092,2.1-i*.27),.20,1.10,'Brass')
 # Abstract theatrical sunburst replaces concept's human silhouette; actual raised metalwork.
 arch_band('Banner Art Deco sun',x,-9.10,2.72,.29,.31,.012,'Brass',24)
 for i in range(13):
  a=i*math.pi/12;tube('Deco banner ray',[(x+.36*math.cos(a),-9.10,2.72+.36*math.sin(a)),(x+.53*math.cos(a),-9.10,2.72+.53*math.sin(a))],.006,'Brass',sides=6)
# Cornices, chamfered quoins, honest stone joints.
for g,y in [('Shell/Front',-8.8),('Shell/Rear',8.84)]:
 group(g)
 for a,b in ([(-11,-3.6),(3.6,11)] if y<0 else [(-11,11)]):
  for z,dep,hh in [(4.37,.42,.13),(4.58,.57,.17),(4.72,.68,.09)]:box('Layered wing cornice',((a+b)/2,y,z),(b-a,dep,hh),'SS_Stone')
  for x in [a+.23,b-.23]:
   box('Corner pilaster',(x,y,2.55),(.34,.41,4.1),'SS_Stone')
  for z in [1.0,1.62,2.24,2.86,3.48,4.1]:
   for x in [a+.25,b-.25]:box('Quoin bedding joint',(x,y+(-.215 if y<0 else .215),z),(.35,.007,.012),'Mortar',0)
for side,g in [(-1,'Shell/Left'),(1,'Shell/Right')]:
 group(g)
 for z,w,h in [(4.37,.42,.13),(4.58,.57,.17),(4.72,.68,.09)]:box('Side cornice return',(side*10.84,0,z),(w,17.6,h),'SS_Stone')

# Hipped roofs with barrel caps, concave pans, visible clay gauge and ridge tiles.
def hip_roof(name,cx,cy,w,d,eave,rise):
 group('Roofs/'+name);a=w/2;b=d/2;hip=min(a,b)*.87
 def z(x,y):return eave+rise*max(0,min(1,(a-abs(x))/hip,(b-abs(y))/hip))
 # Closed deck defined by hip planes, then individual clay strips sampled onto it.
 ridge_x=max(0,a-hip);ridge_y=max(0,b-hip)
 vs=[(cx-a,cy-b,eave),(cx+a,cy-b,eave),(cx+a,cy+b,eave),(cx-a,cy+b,eave),
     (cx-ridge_x,cy-ridge_y,eave+rise),(cx+ridge_x,cy-ridge_y,eave+rise),(cx+ridge_x,cy+ridge_y,eave+rise),(cx-ridge_x,cy+ridge_y,eave+rise)]
 roof=mesh('Thick boarded hip roof',vs,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'SS_TileDark')
 mod=roof.modifiers.new('Roof deck gauge','SOLIDIFY');mod.thickness=.13;bpy.context.view_layer.objects.active=roof;bpy.ops.object.modifier_apply(modifier=mod.name)
 # Tile courses follow each slope: front/back and clipped left/right slopes.
 for side in [-1,1]:
  for i in range(math.ceil(w/.285)):
   x=-a+(i+.5)*w/math.ceil(w/.285)
   maxrun=min(hip,a-abs(x))
   for j in range(math.ceil(maxrun/.36)):
    s0=j*.36;s1=min(maxrun,s0+.42)
    if s1-s0<.04:continue
    verts=[];n=8
    for s in [s0,s1]:
     for k in range(n+1):
      ang=k*math.pi/n;xx=x+.132*math.cos(ang);yy=side*(b-s)
      verts.append((cx+xx,cy+yy,z(xx,yy)+.035+.080*math.sin(ang)))
    tile('Overlapping convex clay cover tile',verts,n)
  for i in range(math.ceil(d/.285)):
   y=-b+(i+.5)*d/math.ceil(d/.285);maxrun=min(hip,b-abs(y))
   for j in range(math.ceil(maxrun/.36)):
    s0=j*.36;s1=min(maxrun,s0+.42)
    if s1-s0<.04:continue
    verts=[];n=8
    for s in [s0,s1]:
     for k in range(n+1):
      ang=k*math.pi/n;yy=y+.132*math.cos(ang);xx=side*(a-s)
      verts.append((cx+xx,cy+yy,z(xx,yy)+.035+.080*math.sin(ang)))
    tile('Side slope clay cover tile',verts,n)
 R.flush_tiles()
 # Low ridge rectangle and hip corner caps have explicit support from the roof deck.
 for sx in [-1,1]:
  for sy in [-1,1]:R.cap('Hip roll cap',[(cx+sx*a,cy+sy*b,eave+.10),(cx+sx*ridge_x,cy+sy*ridge_y,eave+rise+.10)],.105,'SS_TileLight',sides=12)
 for xx in [-ridge_x,ridge_x]:R.cap('Ridge roll',[(cx+xx,cy-ridge_y,eave+rise+.10),(cx+xx,cy+ridge_y,eave+rise+.10)],.105,'SS_Tile',sides=12)
 for yy in [-ridge_y,ridge_y]:R.cap('Ridge return',[(cx-ridge_x,cy+yy,eave+rise+.10),(cx+ridge_x,cy+yy,eave+rise+.10)],.105,'SS_Tile',sides=12)
 for yy in [-b,b]:box('Eave fascia',(cx,cy+yy,eave-.08),(w,.12,.20),'SS_Walnut')
 for xx in [-a,a]:box('Eave fascia',(cx+xx,cy,eave-.08),(.12,d,.20),'SS_Walnut')
def tile(name,verts,n):return R.tile(name,verts,n,rng)
print('Building roof geometry',flush=True)
hip_roof('LeftWing',-7.35,0,8.1,18.6,4.79,1.18)
hip_roof('RightWing',7.35,0,8.1,18.6,4.79,1.18)
# Centre raised roof runs back to rear roof, with no intersecting coplanar roof planes.
group('Shell/CentralUpper');box('Upper left clerestory wall',(-3.50,0,5.17),(.25,17.8,1.10),'SS_Stucco');box('Upper right clerestory wall',(3.50,0,5.17),(.25,17.8,1.10),'SS_Stucco');box('Raised rear wall',(0,8.8,5.17),(7.2,.32,1.10),'SS_Stucco')
hip_roof('Central',0,-.15,7.5,18.5,5.97,1.28)
R.flashing()
# Rainwater is parented to its supporting shell, not loose scene decoration.
for side,g in [(-1,'Shell/Left/Rainwater'),(1,'Shell/Right/Rainwater')]:
 group(g);x=side*11.415
 R.gutter(x)
 for y in [-7.8,-3.9,0,3.9,7.8]:tube('Gutter fascia bracket',[(side*11.1,y,4.66),(x,y,4.62),(x,y,4.78)],.014,'Steel',sides=8)
 for y in [-8.2,8.2]:
  tube('Downpipe swan neck',[(x,y,4.72),(x,y,4.45),(side*11.05,y,4.23),(side*11.05,y,.35),(side*11.25,y,.18)],.053,'Galvanized',wall=.004)
  for z in [.70,2.25,3.9]:box('Pipe masonry strap',(side*11.02,y,z),(.17,.17,.035),'Steel')
  box('Drain splash shoe',(side*11.28,y,.075),(.46,.50,.15),'Concrete')

# Open central hall; partitions stop at generous doors and shared circulation.
xwall('Partitions/RearRooms',2.05,-10.65,10.65,3.65,[(-9.1,-7.8,.36,2.76),(-1.0,.3,.36,2.76),(4.2,5.7,.36,2.76)],'InteriorPlaster',.18)
for x in [-2.90,2.90]:
 g='Partitions/'+('WestSpine' if x<0 else 'EastSpine');xwall(g,-x,2.05,8.65,3.65,[],'InteriorPlaster',.18);rotate_group(g)
xwall('Partitions/Staff',5.15,-10.65,-2.9,3.25,[(-5.0,-3.8,.36,2.76)],'InteriorPlaster',.16)
# Right-front compact washroom. Open lounge retains 45 sqm beside it.
xwall('Partitions/Washroom',-4.15,6.7,10.65,3.2,[(7.2,8.3,.36,2.76)],'InteriorPlaster',.16)
xwall('Partitions/WashroomSide',-6.7,-8.65,-4.15,3.2,[],'InteriorPlaster',.16);rotate_group('Partitions/WashroomSide')
for name,x,y,w in [('Interview',-.35,2.05,1.2),('Audition',4.95,2.05,1.4),('Commons',-8.45,2.05,1.2),('Staff',-4.4,5.15,1.1),('Washroom',7.75,-4.15,1.0)]:
 frame('Partitions/DoorTrim',x,y,w);door(name,x,y,w-.08,2.34,82)
# Continuous picture rail and wainscot panels on room back walls.
for g,a,b,y in [('Shell/Rear/InteriorTrim',-10.6,1.04,8.65),('Shell/Rear/InteriorTrim',2.36,10.6,8.65),('Partitions/InterviewTrim',-2.75,-1.06,2.15),('Partitions/InterviewTrim',.36,2.75,2.15),('Partitions/CommonsTrim',-10.6,-5.06,5.04),('Partitions/CommonsTrim',-3.74,-3.05,5.04)]:
 group(g)
 if not g.startswith('Shell/Rear'):box('Picture rail',((a+b)/2,y,2.85),(b-a,.055,.065),'SS_Walnut')
 box('Dado cap',((a+b)/2,y,1.18),(b-a,.055,.08),'SS_Walnut')
 for i in range(int((b-a)/.85)):
  x=a+.42+i*.85;box('Wainscot panel',(x,y,.81),(.78,.035,.76),'SS_Walnut',.008)
  box('Inset wainscot field',(x,y-.025,.81),(.64,.024,.59),'TimberDark',.006)

# Bespoke rounded reception counter. Open staff side, free bypass on both sides.
group('Furnishings/Reception/Counter')
def curved_counter(z,h,ri,ro,mat):
 verts=[];steps=32
 for zz in [z,z+h]:
  for rr in [ri,ro]:
   verts.extend((rr*math.cos(math.pi+i*math.pi/steps),-4.0+.58*rr*math.sin(math.pi+i*math.pi/steps),zz) for i in range(steps+1))
 k=steps+1;faces=[]
 for i in range(steps):faces.extend([(i,i+1,k+i+1,k+i),(2*k+i,3*k+i,3*k+i+1,2*k+i+1),(i,2*k+i,2*k+i+1,i+1),(k+i,k+i+1,3*k+i+1,3*k+i)])
 faces.extend([(0,k,3*k,2*k),(steps,2*k+steps,3*k+steps,k+steps)])
 return mesh('Curved reception joinery',verts,faces,mat)
curved_counter(.39,.82,1.54,1.71,'SS_Walnut');curved_counter(1.21,.08,1.44,1.82,'TimberDark');curved_counter(.40,.09,1.5,1.79,'SS_Walnut')
for i in range(13):
 a=math.pi+i*math.pi/12;tube('Counter brass flute',[(1.721*math.cos(a),-4+.58*1.721*math.sin(a),.54),(1.721*math.cos(a),-4+.58*1.721*math.sin(a),1.12)],.007,'Brass',sides=8)
box('Staff writing surface',(0,-4.08,1.10),(2.7,.58,.08),'SS_Walnut')
place('TelephoneDesk_1930','Furnishings/Reception/Telephone',(-.72,-4.2,1.15))
place('DeskLampBanker_1930','Furnishings/Reception/BankerLamp',(1.25,-4.23,1.29))
place('ChairBentwood_1930','Furnishings/Reception/StaffChair',(0,-3.4,.39),180)
group('Furnishings/Reception/Details');serif('WELCOME',(0,-5.005,.91),.18,1.1,'Brass')

def armchair(g,x,y,rot=0,width=.78):
 group(g);before=set(G.GROUPS[g].objects)
 for dx in [-width*.35,width*.35]:
  for dy in [-.26,.26]:box('Tapered walnut chair foot',(x+dx,y+dy,.52),(.065,.065,.27),'SS_Walnut')
 box('Upholstered seat',(x,y,.83),(width,.70,.21),'SS_Sage',.06)
 box('Upholstered back',(x,y+.28,1.10),(width,.18,.62),'SS_Sage',.06)
 for dx in [-width/2,width/2]:
  box('Walnut arm support',(x+dx,y,.91),(.08,.65,.19),'SS_Walnut',.025)
  box('Padded arm',(x+dx,y,1.04),(.12,.69,.12),'SS_Sage',.04)
 for dx in [-width*.23,0,width*.23]:ball('Upholstery button',(x+dx,y+.173,1.16),(.012,.009,.012),'Brass')
 if rot:
  from mathutils import Matrix
  r=Matrix.Rotation(math.radians(rot),4,'Z');origin=Vector((x,y,0))
  for o in set(G.GROUPS[g].objects)-before:
   bpy.context.view_layer.update();m=o.matrix_world.copy()
   for v in o.data.vertices:v.co=origin+r@(m@v.co-origin)
   o.matrix_world.identity()
def table(g,x,y,w=1.2,d=.65,h=.52):
 group(g);box('Rounded timber table top',(x,y,.36+h),(w,d,.065),'SS_Walnut',.025)
 for dx in [-w*.4,w*.4]:
  for dy in [-d*.35,d*.35]:box('Turned table leg',(x+dx,y+dy,.36+h/2),(.045,.045,h),'SS_Walnut')
for i,(x,y,r) in enumerate([(-9.1,-6.7,-90),(-9.1,-5.5,-90),(-9.1,-4.3,-90),(-4.1,-6.7,90),(-4.1,-5.5,90),(-4.1,-4.3,90),(-7.45,-2.2,0),(-6.0,-2.2,0)]):armchair('Furnishings/Waiting/Chair%02d'%i,x,y,r)
table('Furnishings/Waiting/CoffeeTable',-6.6,-5.7,1.55,.78)

place('CoatStandTimber_1930','Furnishings/Waiting/CoatStand',(-10,-2,.36))
place('NoticeBoardTimber_173_1930','Furnishings/Waiting/Noticeboard',(-5.2,1.94,1.25))
group('Furnishings/Waiting/Notices');serif('TALENT BUILDS TOMORROW',(-5.2,1.84,2.18),.11,1.45,'TimberDark')
for i in range(4):
 x=-5.77+i*.38;box('Typed casting notice',(x,1.865,1.83),(.28,.009,.37),'PaperLight',.001)
 text(['SCREEN TESTS','STUDIO NEWS','VOICE COACH','ETIQUETTE'][i],(x,1.856,1.92),.032,.24,'TimberDark')
 for j in range(6):box('Printed notice line',(x,1.855,1.86-j*.029),(.20-(j%3)*.025,.002,.003),'TimberDark',0)
# Fictional period headshots are art dressing, independent from future talent data.
for i in range(6):
 group('Furnishings/Waiting/PortraitGallery');x=-10.05+i*.93;y=1.94
 box('Portrait walnut frame',(x,y,2.94),(.64,.08,.78),'SS_Walnut')
 box('Ivory portrait mount',(x,y-.05,2.94),(.55,.025,.69),'PaperLight')
 o=mesh('Sepia performer photograph',[(x-.215,y-.073,2.69),(x+.215,y-.073,2.69),(x+.215,y-.073,3.24),(x-.215,y-.073,3.24)],[(0,1,2,3)],'SS_Headshots')
 uv=o.data.uv_layers.new(name='PortraitAtlas');u=(i%3)/3;v=.5 if i<3 else 0;coords=[(u+.012,v+.015),(u+1/3-.012,v+.015),(u+1/3-.012,v+.485),(u+.012,v+.485)]
 for loop in o.data.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
 o['explicit_uv']=True
 box('Portrait caption',(x,y-.07,2.64),(.24,.016,.025),'Brass',.002)
for o in G.GROUPS['Furnishings/Waiting/PortraitGallery'].objects:
 for v in o.data.vertices:v.co.z+=.24

# Open common/assignment areas intentionally have no gameplay floor labels.
table('Furnishings/Commons/PortfolioTable',-6.4,.3,2.5,1.05,.75)
for i,x in enumerate([-7.25,-6.4,-5.55]):
 place('ChairBentwood_1930','Furnishings/Commons/ChairN'+str(i),(x,1.15,.38))
 place('ChairBentwood_1930','Furnishings/Commons/ChairS'+str(i),(x,-.65,.38),180)

for i,(x,y,r) in enumerate([(4,-6,0),(5.3,-6,0),(4,-3.1,180),(5.3,-3.1,180)]):armchair('Furnishings/Lounge/Chair'+str(i),x,y,r)
table('Furnishings/Lounge/Table',4.65,-4.5,1.5,.7)

# Director interview: desk, two guest chairs, script library, filing, wall clock.
place('DeskPedestal_1930','Furnishings/Interview/Desk',(0,5.6,.39),180)
place('ChairBentwood_1930','Furnishings/Interview/DirectorChair',(0,6.6,.39))
armchair('Furnishings/Interview/GuestA',-.75,4.3,180)
armchair('Furnishings/Interview/GuestB',.75,4.3,180)
place('TelephoneDesk_1930','Furnishings/Interview/Phone',(-.7,5.55,1.19))
place('DeskLampBanker_1930','Furnishings/Interview/Lamp',(.7,5.65,1.19))
place('DeskPaperwork_1930','Furnishings/Interview/Scripts',(0,5.4,1.19))
place('FilingCabinet_4Drawer_1930','Furnishings/Interview/Files',(2.25,7.9,.38))
group('Furnishings/Interview/Bookcase')
for x in [-2.65,-1.35]:box('Bookcase stile',(x,7.95,1.52),(.09,.42,2.30),'SS_Walnut')
for z in [.44,.95,1.47,1.98,2.66]:box('Bookcase shelf',(-2,7.95,z),(1.4,.44,.065),'SS_Walnut')
box('Bookcase back',(-2,8.16,1.52),(1.4,.04,2.3),'TimberDark')
for level in range(4):
 for i in range(11):
  h=rng.uniform(.25,.40);box('Bound script volume',(-2.55+i*.105,7.93,.49+level*.51+h/2),(.075,.29,h),['SS_Curtain','SS_Sage','Leather'][i%3],.003)
  box('Script spine band',(-2.55+i*.105,7.775,.55+level*.51),(.06,.006,.008),'Brass',.001)
group('Shell/Rear/OfficeClock');cylinder('Clock walnut case',(.6,8.61,3.0),.26,.10,'SS_Walnut',(0,1,0),segments=48);cylinder('Clock enamel face',(.6,8.55,3.0),.225,.01,'PaperLight',(0,1,0),segments=48)
for i in range(12):
 a=i*math.pi/6;tube('Clock hour index',[(.6+.185*math.sin(a),8.54,3+.185*math.cos(a)),(.6+.207*math.sin(a),8.54,3+.207*math.cos(a))],.008,'Steel',sides=6)
tube('Clock hands',[(.49,8.535,3.10),(.6,8.535,3),(.72,8.535,3.13)],.009,'Steel',sides=6)

# Intimate audition room: 4.8 m wide rostrum, curtains, period camera and two lamps.
group('Furnishings/Audition/Rostrum');box('Modest audition platform',(6.9,7.05,.52),(5.6,2.6,.30),'SS_Walnut',.015)
for i in range(22):box('Platform timber board',(4.23+i*.255,7.05,.681),(.25,2.57,.025),'Timber',.002)
box('Rostrum step',(6.9,5.56,.44),(2.2,.38,.16),'SS_Walnut',.01)
R.curtains()

group('Furnishings/Audition/Backdrop');box('Neutral audition backdrop',(6.9,8.07,2.03),(3.1,.018,2.58),'Canvas',0)
table('Furnishings/Audition/EvaluationTable',6.2,3.65,2.3,.75,.75)
for i,x in enumerate([5.55,6.45]):place('ChairBentwood_1930','Furnishings/Audition/Evaluator'+str(i),(x,2.8,.39),180)
place('DeskPaperwork_1930','Furnishings/Audition/Notes',(5.6,3.6,1.17))
R.dressing()

# Support rooms, visually compact and legible.
for i,x in enumerate([-9.8,-8.0,-6.2]):place('ShelfSteelTimber_170_1930','Furnishings/Staff/Rack'+str(i),(x,8.25,.36))
place('CupboardUtility_1930','Furnishings/Staff/Cupboard',(-10,6,.36),-90)
place('FilingCabinet_4Drawer_1930','Furnishings/Staff/Files',(-3.55,8.0,.36))
for i in range(8):
 group('Furnishings/Staff/ArchiveBoxes');box('Labelled script archive',(-9.8+(i%4)*1.3,8.2,.5+(i//4)*.6),(.42,.36,.32),'Canvas')
 box('Archive paper label',(-9.8+(i%4)*1.3,8.01,.56+(i//4)*.6),(.18,.01,.075),'PaperLight')
group('Furnishings/Washroom')
for x in [8.05,9.55]:
 box('Porcelain cistern',(x,-8.3,1.1),(.49,.25,.58),'SS_Ivory',.035)
 ball('Porcelain bowl',(x,-7.92,.66),(.27,.37,.23),'SS_Ivory')
 cylinder('Pedestal',(x,-7.97,.51),.15,.29,'SS_Ivory',segments=24)
 tube('Seat ring',[(x+.22*math.cos(a*2*math.pi/32),-7.94+.29*math.sin(a*2*math.pi/32),.85) for a in range(33)],.024,'Black')
box('Cubicle partition',(8.8,-7.28,1.30),(.07,2.4,1.88),'InteriorPlaster')
for x in [8.05,9.55]:
 box('Cubicle door',(x,-6.1,1.3),(1.32,.055,1.74),'SS_Sage')
 box('Cubicle privacy latch',(x+.43,-6.14,1.3),(.07,.035,.09),'Brass')
box('Washstand counter',(9.7,-4.7,1.18),(1.4,.55,.09),'SS_Stone',.025)
for x in [9.3,10.1]:
 ball('Wash basin',(x,-4.7,1.2),(.29,.22,.09),'SS_Ivory');tube('Basin faucet',[(x,-4.52,1.2),(x,-4.52,1.40),(x,-4.66,1.40)],.016,'Brass')
box('Washroom framed mirror',(9.7,-4.26,1.94),(1.45,.05,.85),'SS_Walnut');box('Mirror',(9.7,-4.295,1.94),(1.33,.009,.73),'Glass',0)

# Pendants and ceiling are independent removable roof-supported fixtures.
for i,(x,y) in enumerate([(0,-6.2),(0,-.2),(-6.7,-5),(-6.5,.3),(4.6,-4.5),(0,5.6),(6.8,4.3),(-6.7,6.5)]):
 group('Ceilings/Pendant%02d'%i)
 cylinder('Ceiling rose',(x,y,4.015),.13,.06,'Brass');cylinder('Pendant suspension',(x,y,3.72),.012,.59,'Steel')
 ball('Schoolhouse opal glass',(x,y,3.23),(.24,.24,.23),'SS_Lamp')
 cylinder('Pendant brass collar',(x,y,3.42),.085,.07,'Brass')
 tube('Glass shade equatorial band',[(x+.239*math.cos(j*math.pi/24),y+.239*math.sin(j*math.pi/24),3.23) for j in range(49)],.008,'Brass',sides=8)
group('Ceilings/Plaster');box('Finished suspended plaster ceiling',(0,0,4.10),(21.65,17.65,.10),'InteriorPlaster')
for x in [-10.6,-3,3,10.6]:box('Ceiling perimeter timber',(x,0,4.015),(.12,17.3,.12),'SS_Walnut')

# Export packet uses established production pipeline (Unity X/Z ground, Y up).
print('Exporting semantic packets',flush=True)
bpy.context.view_layer.update()
parts=[];bad=[]
for name,c in G.GROUPS.items():
 if not any(o.type=='MESH' for o in c.objects):continue
 for o in c.objects:
  if o.type!='MESH':continue
  if any(abs(v-1)>1e-7 for v in o.scale):
   for vertex in o.data.vertices:vertex.co.x*=o.scale.x;vertex.co.y*=o.scale.y;vertex.co.z*=o.scale.z
   o.scale=(1,1,1)
  o.data.calc_loop_triangles()
  if any(not math.isfinite(v) for vertex in o.data.vertices for v in vertex.co):bad.append(o.name)
 bpy.context.view_layer.update();part=G.uv_packet(c);part['pivot']=pivots.get(name,[0,0,0]);parts.append(part)
assert not bad,bad
used=sorted(set(n for p in parts for n in p['materials']))
out=ROOT/'ArtExports/StageSchoolA1';out.mkdir(parents=True,exist_ok=True)
data=dict(schema=1,candidate='StageSchool_A1',parts=parts,materials=[dict(name=n,linearRGB=G.PALETTE[n][0],metallic=G.PALETTE[n][1],roughness=G.PALETTE[n][2],texture=G.PALETTE[n][3],shared=n not in CUSTOM) for n in used],fixtures=fixtures,equipment=equipment)
G.save_packet(out/'stage_school_meshes.json.gz',data)
report=dict(candidate='StageSchool_A1',status='MANUAL_ART_REVIEW_PENDING',footprint=[22,18],floor=.36,wall_eave=4.65,central_eave=5.97,roof_crown=7.455,source_groups=len(parts),source_objects=sum(len(c.objects) for c in G.GROUPS.values()),unique_materials=len(used),triangles=sum(sum(len(s['indices'])//3 for s in p['submeshes']) for p in parts),vertices=sum(len(p['positions'])//3 for p in parts),fixtures=len(fixtures),invalid_coordinates=bad,parts=[dict(name=p['name'],vertices=len(p['positions'])//3,triangles=sum(len(s['indices'])//3 for s in p['submeshes'])) for p in parts],materials=data['materials'],ground_datum=0,notes=['Mesh packet excludes linked furnishings; Unity counts include them.','No runtime building components, LODs, construction states or contextual overlays.'])
(HERE/'generation_report.json').write_text(json.dumps(report,indent=2))
# Shared furnishings remain linked collection instances in the editable source.
names=sorted(set(f['asset'] for f in fixtures))
with bpy.data.libraries.load(str(ROOT/'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'),link=True) as (src,dst):dst.collections=[n for n in names if n in src.collections]
collections={c.name:c for c in dst.collections if c}
for f in fixtures:
 o=bpy.data.objects.new('Shared '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=collections[f['asset']];G.group(f['group']).objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(180-f['yaw'])
with bpy.data.libraries.load(str(ROOT/'ArtSource/ProductionEquipment1930Candidate/ProductionEquipment1930Candidate.blend'),link=True) as (src,dst):dst.collections=['StudioCamera1930','StudioLamp1930']
collections={c.name:c for c in dst.collections if c}
for f in equipment:
 o=bpy.data.objects.new('Reusable '+f['asset'],None);o.instance_type='COLLECTION';o.instance_collection=collections[f['asset']];G.group(f['group']).objects.link(o);p=f['position'];o.location=(p[0],p[2],p[1]);o.rotation_euler.z=math.radians(-f['yaw'])
for lib in bpy.data.libraries:
 lib.filepath='//../'+('ProductionEquipment1930Candidate/ProductionEquipment1930Candidate.blend' if 'ProductionEquipment1930Candidate' in lib.filepath else 'PeriodEnvironment1930/PeriodEnvironment1930.blend')
for img in bpy.data.images:
 if img.filepath and 'PerformerHeadshots' in img.filepath:img.filepath='//../../Assets/SilverScreen/Environment/StageSchoolA1/Textures/PerformerHeadshots.png'
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'StageSchool_A1.blend'))
print('STAGE_SCHOOL_A1',json.dumps({k:v for k,v in report.items() if k not in ['parts','materials']}))
