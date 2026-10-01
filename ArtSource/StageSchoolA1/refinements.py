"""Approved A1 refinement: tile atlas/batching, sewn drapery and purposeful dressing."""
import bpy,math,random,sys
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'ArtSource/ProductionEquipment1930Candidate'))
import geometry as G
from primitives import box,cylinder,beam
from geometry import mesh,tube
TILES={};TR=random.Random(193001)

def atlas():
 """64 non-directional clay samples: one map pair, no lighting or damage baked in."""
 dest=ROOT/'Assets/SilverScreen/Environment/StageSchoolA1/Textures';dest.mkdir(exist_ok=True)
 rng=np.random.default_rng(1930);size=1024;cell=128
 albedo=np.ones((size,size,4),dtype=np.float32);packed=np.zeros_like(albedo)
 for y in range(8):
  for x in range(8):
   # Restrained firing variation, with low-amplitude broad mineral mottling.
   coarse=rng.uniform(-.012,.012,(8,8));broad=np.repeat(np.repeat(coarse,16,0),16,1)
   for _ in range(12):broad=(broad+np.roll(broad,1,0)+np.roll(broad,-1,0)+np.roll(broad,1,1)+np.roll(broad,-1,1))/5
   fine=rng.normal(0,.002,(cell,cell));v=rng.uniform(.935,.997)+broad+fine
   hue=np.array([1,rng.uniform(.987,1.006),rng.uniform(.98,1.014)])
   albedo[y*cell:(y+1)*cell,x*cell:(x+1)*cell,:3]=np.clip(v[:,:,None]*hue,0,1)
   packed[y*cell:(y+1)*cell,x*cell:(x+1)*cell,3]=np.clip(rng.uniform(.19,.28)+broad*.6+fine,0,1)
 for name,data,colorspace in [('RoofClay_albedo',albedo,'sRGB'),('RoofClay_metallicSmoothness',packed,'Non-Color')]:
  img=bpy.data.images.new(name,width=size,height=size,alpha=True);img.colorspace_settings.name=colorspace
  # Pixels are supplied in scene-linear space; the albedo file is encoded as sRGB.
  img.pixels.foreach_set(data.ravel());img.filepath_raw=str(dest/(name+'.png'));img.file_format='PNG';img.save()
  img.filepath='//../../Assets/SilverScreen/Environment/StageSchoolA1/Textures/'+name+'.png'
 for name in ['SS_Tile','SS_TileLight','SS_TileDark']:
  mat=G.MATS[name];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
  tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['RoofClay_albedo']
  mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*G.PALETTE[name][0],1)
  links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],bs.inputs['Base Color'])
  rough=nodes.new('ShaderNodeTexImage');rough.image=bpy.data.images['RoofClay_metallicSmoothness'];inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
  links.new(rough.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])

def tile(name,verts,n,rng):
 # Preserve the architectural generator's RNG sequence for unrelated furniture.
 rng.choice(['SS_Tile']*6+['SS_TileLight','SS_TileDark'])
 vs,fs,uv=TILES.setdefault(G.CURRENT,([],[],[]));offset=len(vs);count=len(verts)
 vs.extend(verts+[(x,y,z-.018) for x,y,z in verts]);ff=[]
 for k in range(n):
  q=(k,k+1,n+2+k,n+1+k);ff.extend([q,tuple(count+i for i in reversed(q))])
 for a,b in [(k,k+1) for k in range(n)]+[(n+1+k,n+2+k) for k in range(n)]+[(0,n+1),(n,2*n+1)]:ff.append((a,b,b+count,a+count))
 fs.extend(tuple(offset+i for i in f) for f in ff)
 cell=TR.randrange(64);flip=TR.choice([False,True]);u=(cell%8)/8;v=(cell//8)/8
 for i in range(count*2):
  a=i%count;xx=(a%(n+1))/n;yy=a//(n+1)
  uv.append((u+.004+(1-xx if flip else xx)*.117,v+.004+yy*.117))

def flush_tiles():
 vs,fs,uv=TILES.pop(G.CURRENT);o=mesh('Batched overlapping clay courses',vs,fs,'SS_Tile')
 layer=o.data.uv_layers.new(name='ClayFiringAtlas')
 for loop in o.data.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
 o['explicit_uv']=True

def cap(name,points,radius,mat,sides=12):
 # Same cap envelope and ridge/hip arrangement, with small visible lap joints.
 a,b=map(Vector,points);length=(b-a).length;count=max(1,math.ceil(length/.43))
 for i in range(count):
  start=a.lerp(b,i/count);end=a.lerp(b,(i+1)/count)
  tube(name+' fired section',[start,end],radius,mat,sides=sides)
  if i:
   # A narrow recessed seam, rather than enlarged bands that alter the silhouette.
   q=(b-a).normalized();tube(name+' mortar joint',[start-q*.004,start+q*.004],radius+.0008,'SS_TileDark',sides=sides)

def gutter(x):
 vv=[];ff=[]
 for y in [-9.28,9.28]:
  for r in [.085,.079]:
   for i in range(17):a=math.pi+i*math.pi/16;vv.append((x+r*math.cos(a),y,4.76+r*math.sin(a)))
 for j in range(16):ff.extend([(j,j+1,j+35,j+34),(j+17,j+51,j+52,j+18)])
 for i in [0,16]:ff.append((i,i+34,i+51,i+17))
 for offset in [0,34]:
  for j in range(16):ff.append((offset+j,offset+j+17,offset+j+18,offset+j+1))
 mesh('Open half-round eave gutter',vv,ff,'Galvanized')
 for y in [-9.278,9.278]:
  v=[(x,y,4.76)]+[(x+.079*math.cos(math.pi+i*math.pi/16),y,4.76+.079*math.sin(math.pi+i*math.pi/16)) for i in range(17)]
  mesh('Gutter stop end',v,[tuple(range(len(v)))],'Galvanized')
 for dx in [-.083,.083]:tube('Rolled gutter lip',[(x+dx,-9.28,4.76),(x+dx,9.28,4.76)],.006,'Galvanized',sides=8)
 for y in [-8.2,8.2]:cylinder('Gutter outlet sleeve',(x,y,4.69),.058,.09,'Galvanized',segments=20)

def flashing():
 for side in [-1,1]:
  G.group('Roofs/'+('LeftWing' if side<0 else 'RightWing'))
  # The wing abuts the raised central wall; upstand is tucked into that wall.
  vv=[];ff=[]
  for i in range(45):
   y=-8.8+i*.4
   for x,up in [(side*3.63,.17),(side*3.63,0),(side*3.83,0)]:
    z=4.79+1.18*max(0,min(1,(4.05-abs(abs(x)-7.35))/3.5235,(9.3-abs(y))/3.5235))+.116
    vv.append((x,y,z+up))
  for i in range(44):
   for j in range(2):a=i*3+j;ff.append((a,a+1,a+4,a+3))
  o=mesh('Formed lead apron and headwall upstand',vv,ff,'Galvanized')
  mod=o.modifiers.new('Sheet flashing gauge','SOLIDIFY');mod.thickness=.003
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)

def cloth(name,vs,fs,th=.009):
 o=mesh(name,vs,fs,'SS_Curtain')
 for p in o.data.polygons:p.use_smooth=True
 mod=o.modifiers.new('Sewn velvet thickness','SOLIDIFY');mod.thickness=th
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o

def curtains():
 G.group('Furnishings/Audition/Curtains')
 # Dense enough across the pleats, economical down the smoothly hanging length.
 for side in [-1,1]:
  vv=[];ff=[];nx=80;nz=24
  for j in range(nz+1):
   t=j/nz;z=.722+t*2.73;gather=math.exp(-((t-.29)/.17)**2)
   inner=1.57+.45*gather;width=1.21-.43*gather
   for i in range(nx+1):
    u=i/nx;phase=u*math.pi*18+.22*math.sin(u*13)+.25*(1-t)
    fold=(math.sin(phase)+.22*math.sin(phase*2+.6))*(.062-.022*gather)
    x=6.9+side*(inner+width*u);y=8.12+fold+.035*math.sin(t*math.pi)*math.sin(u*5)
    zz=z+(.009*math.cos(phase) if j==0 else 0);vv.append((x,y,zz))
  for j in range(nz):
   for i in range(nx):a=j*(nx+1)+i;ff.append((a,a+1,a+nx+2,a+nx+1))
  cloth('Gathered velvet side drape',vv,ff)
  tube('Weighted stitched bottom hem',vv[:nx+1],.012,'SS_Curtain',sides=6)
  tube('Turned leading edge',[vv[j*(nx+1)] for j in range(nz+1)],.009,'SS_Curtain',sides=6)
  # A cord follows the gathered fabric and returns to a wall-supported hook.
  x0=6.9+side*2.01;x1=6.9+side*2.80
  tube('Braided curtain tieback',[(x0,8.10,1.54),(x0+side*.18,8.035,1.46),(x1-side*.12,8.04,1.46),(x1,8.20,1.55)],.014,'Brass',sides=8)
  box('Curtain tieback wall plate',(x1,8.34,1.55),(.065,.024,.08),'Brass')
  tube('Tieback return hook',[(x1,8.34,1.55),(x1,8.20,1.55)],.014,'Brass',sides=8)
 # Continuous tailored valance with shallow scallops; no string of spherical lobes.
 vv=[];ff=[];nx=160;ny=8
 for j in range(ny+1):
  t=j/ny
  for i in range(nx+1):
   u=i/nx;x=3.93+5.94*u;bottom=3.09+.09*math.cos(u*math.pi*6)
   y=8.005+.027*math.sin(u*math.pi*64+.1*math.sin(u*20));z=bottom+(3.48-bottom)*t
   vv.append((x,y,z))
 for j in range(ny):
  for i in range(nx):a=j*(nx+1)+i;ff.append((a,a+1,a+nx+2,a+nx+1))
 cloth('Tailored scalloped velvet valance',vv,ff)
 tube('Valance sewn hem',vv[:nx+1],.011,'SS_Curtain',sides=6)
 box('Curtain walnut pelmet',(6.9,8.21,3.54),(6.05,.29,.28),'SS_Walnut')
 tube('Curtain suspension rail',[(3.99,8.22,3.43),(9.81,8.22,3.43)],.025,'Steel',sides=12)
 for x in [4.05,5.4,6.9,8.4,9.75]:
  box('Track bracket rear plate',(x,8.35,3.48),(.065,.03,.10),'Steel')
  beam('Rail support',(x,8.35,3.48),(x,8.22,3.43),.022,.023,'Steel')

def papers(x,y,z,w=.22,d=.29,title=False):
 box('Bound typescript leaves',(x,y,z+.010),(w,d,.020),'PaperLight',.001)
 for j in range(7):box('Typescript lines',(x,y-d*.32+j*d*.083,z+.0206),(w*(.65 if j%3 else .5),.002,.001),'TimberDark',0)
 if title:box('Typescript heading',(x,y+d*.39,z+.021),(w*.64,.010,.001),'TimberDark',0)

def portfolio(x,y,z):
 box('Leather headshot portfolio',(x,y,z+.018),(.31,.39,.036),'Leather',.006)
 box('Portfolio paper edge',(x,y-.005,z+.022),(.29,.365,.018),'Paper',.001)
 box('Portfolio cover',(x,y,z+.035),(.31,.39,.010),'Leather',.003)
 box('Portfolio brass corner',(x+.14,y-.18,z+.041),(.027,.027,.002),'Brass',0)
 papers(x+.025,y-.005,z+.044,.22,.29,True)

def dressing():
 G.group('Furnishings/Reception/Accessories')
 x,y,z=.40,-4.12,1.15
 # A two-level application tray stays within the existing paperwork patch.
 for zz in [z+.027,z+.104]:
  box('Application tray base',(x,y,zz),(.30,.37,.014),'SS_Walnut')
  for dx in [-.15,.15]:box('Tray raised side',(x+dx,y,zz+.027),(.012,.37,.047),'SS_Walnut')
  box('Tray rear edge',(x,y+.18,zz+.027),(.30,.012,.047),'SS_Walnut')
  papers(x,y,zz+.008,.23,.29,True)
 for dx in [-.14,.14]:box('Tray brass riser',(x+dx,y+.14,z+.08),(.012,.015,.15),'Brass')
 box('Desk blotter',(-.12,-4.14,1.16),(.28,.33,.008),'Leather',.002)
 cylinder('Ink well',(-.14,-4.05,1.186),.025,.04,'Glass',segments=16)
 beam('Desk pen',(-.19,-4.23,1.17),(-.09,-4.07,1.17),.004,.004,'Black')
 G.group('Furnishings/Waiting/ReadingMaterial')
 for i in range(2):
  x=-6.90+i*.34;y=-5.70+i*.04;z=.923
  box('Film trade publication',(x,y,z),(.255,.33,.016),['SS_Sage','SS_Curtain'][i],.002)
  box('Cream publication masthead',(x,y+.107,z+.009),(.225,.042,.001),'PaperLight',0)
  for j in range(5):box('Publication print column',(x-.06,y-.04+j*.022,z+.009),(.10,.004,.001),'PaperLight',0)
  box('Monochrome cover photo',(x+.065,y-.01,z+.009),(.079,.12,.001),'TimberDark',0)
 portfolio(-6.22,-5.64,.921)
 G.group('Furnishings/Commons/ReviewMaterials');portfolio(-6.85,.30,1.144);papers(-5.78,.35,1.144,.24,.30,True)
 box('Portfolio review pencil',(-5.94,.33,1.172),(.006,.20,.006),'Timber',0)
 G.group('Furnishings/Audition/EvaluationMaterials');papers(6.67,3.68,1.145,.23,.30,True)
 box('Evaluation pencil',(6.83,3.68,1.16),(.005,.20,.005),'Timber',0)
 G.group('Furnishings/Interview/PortfolioReview');portfolio(.32,5.46,1.19)
 box('Script ribbon',(.03,5.34,1.24),(.22,.014,.005),'SS_Curtain',0)
 G.group('Furnishings/Reception/Wastebasket')
 # Tucked behind the desk, outside applicant circulation.
 x,y=-1.20,-3.56
 tube('Rolled wastebasket rim',[(x+.12*math.cos(i*math.pi/16),y+.12*math.sin(i*math.pi/16),.71) for i in range(33)],.009,'Steel',sides=6)
 cylinder('Wastebasket floor',(x,y,.38),.095,.015,'Steel',segments=20)
 for i in range(16):
  a=i*math.pi/8;beam('Wastebasket wire',(x+.09*math.cos(a),y+.09*math.sin(a),.385),(x+.12*math.cos(a),y+.12*math.sin(a),.71),.004,.004,'Steel')
