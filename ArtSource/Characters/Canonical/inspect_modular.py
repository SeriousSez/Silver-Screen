"""Read-only measurements for the authored modular conversion."""
import bpy, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Characters/Canonical/Canonical_AuthoredCandidate.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=body.data;uv=mesh.uv_layers.active.data
# Faces sharing an edge and both UV endpoints belong to one atlas island.
edges={};adj=[[] for p in mesh.polygons]
for p in mesh.polygons:
    for j,loop in enumerate(p.loop_indices):
        n=p.loop_indices[(j+1)%len(p.loop_indices)]
        a=(mesh.loops[loop].vertex_index,tuple(round(v,5) for v in uv[loop].uv))
        b=(mesh.loops[n].vertex_index,tuple(round(v,5) for v in uv[n].uv))
        key=tuple(sorted((a,b)))
        if key in edges:adj[p.index].append(edges[key]);adj[edges[key]].append(p.index)
        else:edges[key]=p.index
seen=set();islands=[]
for i in range(len(adj)):
    if i in seen:continue
    ids=[];stack=[i];seen.add(i)
    while stack:
        p=stack.pop();ids.append(p)
        for n in adj[p]:
            if n not in seen:seen.add(n);stack.append(n)
    points=[body.matrix_world@mesh.vertices[v].co for i in ids for v in mesh.polygons[i].vertices]
    uvs=[uv[l].uv for i in ids for l in mesh.polygons[i].loop_indices]
    islands.append(dict(faces=ids,count=len(ids),min=[min(c[a] for c in points) for a in range(3)],max=[max(c[a] for c in points) for a in range(3)],uvmin=[min(c[a] for c in uvs) for a in range(2)],uvmax=[max(c[a] for c in uvs) for a in range(2)]))
islands.sort(key=lambda x:-x['count'])
report={'islands':islands,'bones':{b.name:{'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)} for b in rig.data.bones},'matrix':list(map(list,body.matrix_world))}
(ROOT/'ArtReview/Characters/Canonical/modular-inspection.json').write_text(json.dumps(report,indent=2))
print(json.dumps([{k:v for k,v in x.items() if k!='faces'} for x in islands[:35]],indent=2))
