"""A4 read-only, authored-object runtime derivatives. Uses established mesh packets.

Run with Blender 5 --background --factory-startup --python this_file.
Structural topology is dissolved, never assembly-decimated. Only individual
letters, cloth and rounded props use local reduction. Final planters stay exact
at LOD0. Shared asset libraries and the approved blend are never saved.
"""
import bpy,bmesh,json,gzip,hashlib,math,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];OUT=R/'ArtExports/StageSchoolB2'
source=OUT/'master_meshes.json';digest=hashlib.sha256(source.read_bytes()).hexdigest();data=json.loads(source.read_text())
sys.path.insert(0,str(R/'ArtSource/PeriodEnvironment1930'));import geometry as G
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA4/StageSchool_A4.blend'))
collections={c.name:c for c in bpy.data.collections if c.library is None}
with gzip.open(R/'ArtExports/PeriodEnvironment1930/LOD/lod_meshes.json.gz','rt') as f:kit={p['name']:p for p in json.load(f)['parts']}
kitnames={}
for meta in (R/'Assets/SilverScreen/Environment/PeriodEnvironment1930/Meshes').glob('*.meta'):
    m=re.search(r'guid: (\w+)',meta.read_text())
    if m:kitnames[m[1]]=meta.stem.removesuffix('.asset')
dimensions={o:tuple(o.dimensions) for c in collections.values() for o in c.objects if o.type=='MESH'}
for layer in bpy.context.view_layer.layer_collection.children:layer.exclude=True
stats=[]
for level in range(3):
    parts=[]
    for i,p in enumerate(data['parts']):
        col=collections.get(p['sourceGroup'])
        if col is None or not any(o.type=='MESH' for o in col.objects) or level==0 and p['sourceGroup']=='Exterior/Planters':
            k=kit.get(kitnames.get(p['name'],'')+'_LOD'+str(level)) if level else None
            q=dict(k or p)
            if k:
                by={n:s for n,s in zip(q['materials'],q['submeshes'])}
                q['submeshes']=[by.get(Path(m).stem,dict(indices=[])) for m in p['materials']]
                q['materials']=p['materials'];q['name']=p['name']
            parts.append(q);continue
        work=bpy.data.collections.new('B2_derivative');bpy.context.scene.collection.children.link(work)
        for original in col.objects:
            if original.type!='MESH':continue
            name=original.name.lower()
            letters=any(x in name for x in ['letters','letter ','text','title'])
            if level==2 and col.name.endswith(('Banners','Notices')) and letters:continue
            structural=col.name.startswith(('Roofs','Interior','Shell','Doors','Partitions','Structure','Ceilings'))
            if level==2 and max(dimensions[original])<.12 and not structural:continue
            o=original.copy();o.data=original.data.copy();work.objects.link(o)
            bm=bmesh.new();bm.from_mesh(o.data)
            angle=math.radians((1,6,12)[level] if letters else (.01,22,42)[level])
            bmesh.ops.dissolve_limit(bm,angle_limit=angle,verts=list(bm.verts),edges=list(bm.edges),use_dissolve_boundaries=False,delimit={'MATERIAL','SEAM'})
            bm.to_mesh(o.data);bm.free()
            if (letters or level and not structural) and len(o.data.polygons)>120:
                bpy.context.view_layer.objects.active=o
                mod=o.modifiers.new('Authored local detail reduction','DECIMATE')
                mod.ratio=(.38,.20,.10)[level] if letters else (1,.48,.20)[level]
                bpy.ops.object.modifier_apply(modifier=mod.name)
        q=G.uv_packet(work);bymat={n:s for n,s in zip(q['materials'],q['submeshes'])}
        q['positions']=[v-p['pivot'][j%3] for j,v in enumerate(q['positions'])]
        q['submeshes']=[bymat.get(Path(m).stem,dict(indices=[])) for m in p['materials']]
        q['materials']=p['materials'];q['name']=p['name'];q['pivot']=[0,0,0];parts.append(q)
        for o in list(work.objects):
            mesh=o.data;bpy.data.objects.remove(o,do_unlink=True);bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(work)
        if i%20==0:print('B2 LOD',level,i,flush=True)
    for p in parts:
        used=[i for i,s in enumerate(p['submeshes']) if s['indices']]
        p['materials']=[p['materials'][i] for i in used];p['submeshes']=[p['submeshes'][i] for i in used]
    with gzip.open(OUT/f'lod{level}.json.gz','wt') as f:json.dump(dict(parts=parts),f,separators=(',',':'))
    stats.append(dict(level=level,uniqueMeshTriangles=sum(len(s['indices'])//3 for p in parts for s in p['submeshes']),vertices=sum(len(p['positions'])//3 for p in parts)))
    print(stats[-1],flush=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
(OUT/'provenance.json').write_text(json.dumps(dict(strategy='A4 authored-object topology; exact close planters; reused period prop LODs',masterPacketSha256=digest,levels=stats),indent=2))
