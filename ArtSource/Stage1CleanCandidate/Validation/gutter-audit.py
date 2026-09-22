import bpy,json,hashlib,struct,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
out=Path(sys.argv[sys.argv.index('--')+1])
items=[o for o in bpy.data.objects if o.type=='MESH']
def verts(o):return [o.matrix_world@v.co for v in o.data.vertices]
def tree(os):
    vs=[];fs=[];owners=[]
    for o in os:
        base=len(vs);vs.extend(verts(o));fs.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons);owners.extend([o.name]*len(o.data.polygons))
    return BVHTree.FromPolygons(vs,fs),owners
structure=[o for o in items if o.parent.name in ['Roof','MasonryFraming','ExteriorWalls'] or o.name.startswith(('RoofStandingSeam','EndAbutmentFlashing'))]
rb,rn=tree(structure);rows=[]
for o in items:
    if o.name.startswith(('OpenHalfRoundEaveGutter','FormedRoofDripIntoGutter','GutterClosedEnd','RainwaterDownpipe','GutterRun','GutterStopEnd','EaveRunoffApron','RainwaterHollowDownpipe')):
        gb,_=tree([o]);hits=gb.overlap(rb);vs=verts(o)
        rows.append({'part':o.name,'bounds':[[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]],'structure_intersections':sorted({rn[b] for a,b in hits})})
hashes={}
for o in items:
    h=hashlib.sha256()
    for v in verts(o):h.update(struct.pack('<3f',*v))
    for p in o.data.polygons:h.update(struct.pack('<'+str(len(p.vertices)+1)+'I',p.material_index,*p.vertices))
    h.update('|'.join(m.name for m in o.data.materials).encode());hashes[o.name]=h.hexdigest()
out.write_text(json.dumps({'rainwater':rows,'source_mesh_hashes':hashes},indent=2),encoding='utf-8')
print(json.dumps(rows,indent=2))
