"""B1 object-aware derivatives. Frozen blend is read-only; only B packets written."""
import bpy,bmesh,json,gzip,hashlib,math,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];OUT=R/'ArtExports/StageSchoolB'
source=OUT/'master_meshes.json';digest=hashlib.sha256(source.read_bytes()).hexdigest();data=json.loads(source.read_text())
sys.path.insert(0,str(R/'ArtSource/PeriodEnvironment1930'));import geometry as G
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
names={}
for meta in (R/'Assets/SilverScreen/Environment/StageSchoolA3/Meshes').glob('*.meta'):
 m=re.search(r'guid: (\w+)',meta.read_text())
 if m:names[m[1]]=meta.stem.removesuffix('.asset')
collections={c.name.replace('/','_'):c for c in bpy.data.collections if c.library is None}
with gzip.open(R/'ArtExports/StageSchoolA3/stage_school_meshes.json.gz','rt') as f: pivots={p['name'].replace('/','_'):p.get('pivot',[0,0,0]) for p in json.load(f)['parts']}
with gzip.open(R/'ArtExports/PeriodEnvironment1930/LOD/lod_meshes.json.gz','rt') as f: kit={p['name']:p for p in json.load(f)['parts']}
kitnames={}
for meta in (R/'Assets/SilverScreen/Environment/PeriodEnvironment1930/Meshes').glob('*.meta'):
 m=re.search(r'guid: (\w+)',meta.read_text())
 if m:kitnames[m[1]]=meta.stem.removesuffix('.asset')
dimensions={o:tuple(o.dimensions) for c in collections.values() for o in c.objects if o.type=='MESH'}
# Avoid re-evaluating the entire 20,000-object source scene for each component.
for layer in bpy.context.view_layer.layer_collection.children:layer.exclude=True
stats=[]
bakes={r['name']:r for r in json.loads((R/'Assets/SilverScreen/Environment/StageSchoolB/RoofSurfaces/roof_surfaces.json').read_text())}
for level in range(3):
 parts=[]
 for i,p in enumerate(data['parts']):
  col=collections.get(names.get(p['name'],''))
  # Independent reusable assets retain their exact master corner attributes.
  if col is None:
   k=kit.get(kitnames.get(p['name'],'')+'_LOD'+str(level)) if level else None
   if k:
    q=dict(k);by={n:s for n,s in zip(q['materials'],q['submeshes'])};q['submeshes']=[by.get(Path(m).stem,dict(indices=[])) for m in p['materials']];q['materials']=p['materials'];q['name']=p['name'];parts.append(q)
   else:parts.append(p)
   continue
  work=bpy.data.collections.new('B1_derivative');bpy.context.scene.collection.children.link(work)
  for original in col.objects:
   if original.type!='MESH':continue
   o=original.copy();o.data=original.data.copy();work.objects.link(o)
   name=original.name.lower();roof=col.name.startswith('Roofs/')
   if level and 'overlapping clay courses' in name:
    bpy.data.objects.remove(o,do_unlink=True);continue
   if level==2 and col.name.endswith(('Banners','Notices')) and any(x in name for x in ['letters','letter ','text','title']):
    bpy.data.objects.remove(o,do_unlink=True);continue
   if level and 'thick boarded hip roof' in name:
    spec=bakes[col.name.split('/')[1]];uv=o.data.uv_layers.active or o.data.uv_layers.new()
    for loop in o.data.loops:
     v=o.matrix_world@o.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=((v.x-spec['min'][0])/(spec['max'][0]-spec['min'][0]),(v.y-spec['min'][1])/(spec['max'][1]-spec['min'][1]))
    o['explicit_uv']=True;matname='B1Roof_'+col.name.split('/')[1];o.data.materials.clear();o.data.materials.append(bpy.data.materials.get(matname) or bpy.data.materials.new(matname))
   if level==2 and max(dimensions[original])<.13 and not roof and not col.name.startswith(('Doors','Shell','Partitions')):
    bpy.data.objects.remove(o,do_unlink=True);continue
   bm=bmesh.new();bm.from_mesh(o.data);tiles='overlapping clay courses' in name
   if tiles:
    # Hidden undersides only; preserve course lips and clipped abutment borders.
    bm.normal_update();bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.normal.z<-.15],context='FACES')
   letters=any(x in name for x in ['letters','letter ','text','notice title'])
   angle=math.radians((.01,23,46)[level] if tiles else (1,6,12)[level] if letters else (.01,23,46)[level])
   bmesh.ops.dissolve_limit(bm,angle_limit=angle,verts=list(bm.verts),edges=list(bm.edges),use_dissolve_boundaries=False,delimit={'MATERIAL','SEAM'})
   bm.to_mesh(o.data);bm.free()
   protected=roof or col.name.startswith(('Interior','Shell','Doors','Partitions','Structure'))
   # Local rounded props/cloth only, never a whole mixed assembly.
   if (level and not protected or letters) and len(o.data.polygons)>120:
    bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Local cloth and prop tessellation','DECIMATE');mod.ratio=(.5,.22,.12)[level] if letters else (1,.45,.18)[level];bpy.ops.object.modifier_apply(modifier=mod.name)
  q=G.uv_packet(work);bymat={n:s for n,s in zip(q['materials'],q['submeshes'])}
  pivot=pivots.get(names[p['name']],[0,0,0]);q['positions']=[v-pivot[j%3] for j,v in enumerate(q['positions'])]
  materialpaths=list(p['materials'])
  materialpaths.extend('Assets/SilverScreen/Environment/StageSchoolB/RoofSurfaces/'+n+'.mat' for n in q['materials'] if n.startswith('B1Roof_'))
  q['submeshes']=[bymat.get(Path(m).stem,dict(indices=[])) for m in materialpaths]
  q['materials']=materialpaths;q['name']=p['name'];q['pivot']=[0,0,0];parts.append(q)
  for o in list(work.objects):
   mesh=o.data;bpy.data.objects.remove(o,do_unlink=True);bpy.data.meshes.remove(mesh)
  bpy.data.collections.remove(work)
  if i%40==0:print('B1 LOD',level,i,flush=True)
 for p in parts:
  used=[i for i,s in enumerate(p['submeshes']) if s['indices']]
  p['materials']=[p['materials'][i] for i in used];p['submeshes']=[p['submeshes'][i] for i in used]
 with gzip.open(OUT/f'lod{level}.json.gz','wt') as f:json.dump(dict(parts=parts),f,separators=(',',':'))
 stats.append(dict(level=level,triangles=sum(len(s['indices'])//3 for p in parts for s in p['submeshes']),vertices=sum(len(p['positions'])//3 for p in parts)))
 print(stats[-1],flush=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
(OUT/'provenance.json').write_text(json.dumps(dict(strategy='authored-object-topology',masterPacketSha256=digest,levels=stats),indent=2))
