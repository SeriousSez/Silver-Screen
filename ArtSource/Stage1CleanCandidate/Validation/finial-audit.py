import bpy, json, hashlib, struct, sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

out=Path(sys.argv[sys.argv.index('--')+1])
items=[o for o in bpy.data.objects if o.type=='MESH']
def verts(o): return [o.matrix_world@v.co for v in o.data.vertices]
def bounds(o):
    vs=verts(o)
    return [Vector(tuple(f(v[i] for v in vs) for i in range(3))) for f in [min,max]]
def bvh(os):
    vs=[];fs=[];owners=[]
    for o in os:
        base=len(vs);vs.extend(verts(o))
        fs.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons)
        owners.extend([o.name]*len(o.data.polygons))
    return BVHTree.FromPolygons(vs,fs),owners
roof=[o for o in items if o.name.startswith(('ArchFascia','ArchOuterCoping','CircularSegmentMasonry')) or o.parent.name in ['Roof','RoofSeams','RoofGlazing','RoofVents','Rainwater']]
rb,rn=bvh(roof)
rows=[]
for face in [-1,1]:
    for side in [-1,1]:
        ornament=[o for o in items if o.name.startswith(('Finial','HippedMasonryFinial')) and (sum(v.x for v in verts(o))/len(o.data.vertices))*side>0 and (sum(v.y for v in verts(o))/len(o.data.vertices))*face>0]
        fb,fn=bvh(ornament)
        hits=fb.overlap(rb)
        rows.append({'corner':('front' if face<0 else 'rear')+('_left' if side<0 else '_right'), 'parts':[o.name for o in ornament], 'roof_intersections':sorted(set((fn[a],rn[b]) for a,b in hits)), 'ornament_inward_plane':min(face*v.y for o in ornament for v in verts(o)), 'roof_outward_plane':max(face*v.y for o in roof for v in verts(o))})
hashes={}
for o in items:
    h=hashlib.sha256()
    for v in verts(o):h.update(struct.pack('<3f',*v))
    for p in o.data.polygons:h.update(struct.pack('<'+str(len(p.vertices)+1)+'I',p.material_index,*p.vertices))
    h.update('|'.join(m.name for m in o.data.materials).encode())
    hashes[o.name]=h.hexdigest()
out.write_text(json.dumps({'corners':rows,'source_mesh_hashes':hashes},indent=2),encoding='utf-8')
print(json.dumps(rows,indent=2))
