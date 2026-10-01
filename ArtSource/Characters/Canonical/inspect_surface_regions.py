import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Characters/Canonical/TailoredSource_Inspection.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=o.data
adj=[[] for v in mesh.vertices]
for e in mesh.edges:a,b=e.vertices;adj[a].append(b);adj[b].append(a)
visited=set();components=[]
for i in range(len(adj)):
    if i in visited:continue
    stack=[i];visited.add(i);indices=[]
    while stack:
        n=stack.pop();indices.append(n)
        for k in adj[n]:
            if k not in visited:visited.add(k);stack.append(k)
    co=[o.matrix_world@mesh.vertices[i].co for i in indices]
    weights={}
    for i in indices:
        for g in mesh.vertices[i].groups:
            name=o.vertex_groups[g.group].name;weights[name]=weights.get(name,0)+g.weight
    components.append({'vertices':len(indices),'min':[min(c[a] for c in co) for a in range(3)],'max':[max(c[a] for c in co) for a in range(3)],'groups':sorted(weights.items(),key=lambda p:-p[1])[:4],'sample':indices[0]})
(ROOT/'ArtReview/Characters/Canonical/surface-regions.json').write_text(json.dumps(components,indent=2))
print(json.dumps(components,indent=2))
