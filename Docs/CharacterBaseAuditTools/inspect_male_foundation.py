"""Read-only Male source inspection. Writes evidence, never the purchased file.

All component partitions are recomputed from Male edges, without Female data.
Run in Blender with -- --source FILE --output DIRECTORY.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np

sys.dont_write_bytecode = True


def components(mesh):
    parent = list(range(len(mesh.vertices)))
    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for e in mesh.edges:
        a, b = e.vertices
        parent[root(a)] = root(b)
    groups = {}
    for v in mesh.vertices:
        groups.setdefault(root(v.index), []).append(v.index)
    return sorted(groups.values(), key=lambda ids: (-len(ids), min(ids)))


def expose():
    for c in bpy.data.collections:
        c.hide_viewport = False
    def visit(layer):
        layer.exclude = layer.hide_viewport = False
        for child in layer.children:
            visit(child)
    visit(bpy.context.view_layer.layer_collection)
    for o in bpy.data.objects:
        o.hide_viewport = False
        o.hide_set(False)
    bpy.context.view_layer.update()


def describe(obj, output):
    mesh = obj.data
    mesh.calc_loop_triangles()
    points = np.array([obj.matrix_world @ v.co for v in mesh.vertices])
    groups = components(mesh)
    edges = Counter()
    for p in mesh.polygons:
        ids = list(p.vertices)
        for a, b in zip(ids, ids[1:]+ids[:1]):
            edges[tuple(sorted((a, b)))] += 1
    rows = []
    for ids in groups:
        members = set(ids)
        ce = {k:v for k,v in edges.items() if k[0] in members}
        boundary = [k for k,v in ce.items() if v == 1]
        boundary_ids = sorted(set(i for e in boundary for i in e))
        weights = Counter(obj.vertex_groups[g.group].name for i in ids for g in mesh.vertices[i].groups if g.weight > .00001)
        rows.append(dict(vertices=len(ids), first=min(ids), last=max(ids),
                         bounds_min=points[ids].min(0).tolist(), bounds_max=points[ids].max(0).tolist(),
                         edge_face_counts=dict(Counter(ce.values())), boundary_vertices=boundary_ids,
                         boundary_z_range=points[boundary_ids,2].min().item() if boundary_ids else None,
                         weights=weights))
    shapedata = []
    if mesh.shape_keys:
        basis = np.array([v.co for v in mesh.shape_keys.key_blocks[0].data])
        for k in mesh.shape_keys.key_blocks:
            p = np.array([v.co for v in k.data]); delta = np.linalg.norm(p-basis, axis=1)
            shapedata.append(dict(name=k.name, min=k.slider_min, max=k.slider_max, value=k.value,
                                  finite=bool(np.isfinite(p).all()), changed=int(sum(delta>1e-7)),
                                  max_local_delta=float(delta.max())))
    np.savez_compressed(output/(obj.name+'.npz'), points=points,
                        **{'component_'+str(i):np.array(ids) for i,ids in enumerate(groups)})
    (output/(obj.name+'-faces.json')).write_text(json.dumps([list(p.vertices) for p in mesh.polygons]))
    uvhashes = {}
    for layer in mesh.uv_layers:
        data = np.array([v.uv for v in layer.data], dtype=np.float32)
        uvhashes[layer.name] = hashlib.sha256(data.tobytes()).hexdigest()
    colours=[]
    for a in mesh.color_attributes:
        values=np.array([list(v.color) for v in a.data])
        colours.append(dict(name=a.name,domain=a.domain,min=values.min(0).tolist(),max=values.max(0).tolist(),distinct=len(np.unique(values,axis=0))))
    return dict(vertices=len(mesh.vertices),triangles=len(mesh.loop_triangles),polygons=len(mesh.polygons),
                height=float(np.ptp(points[:,2])),bounds_min=points.min(0).tolist(),bounds_max=points.max(0).tolist(),
                components=rows,uv_hashes=uvhashes,materials=[m.name if m else None for m in mesh.materials],
                colour_attributes=colours,
                shapes=shapedata, parent=obj.parent.name if obj.parent else None,
                parent_type=obj.parent_type,parent_bone=obj.parent_bone,
                unweighted=sum(not any(g.weight>1e-7 for g in v.groups) for v in mesh.vertices))


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--source',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.output.mkdir(parents=True,exist_ok=True)
    stamp=(args.source.stat().st_mtime_ns,hashlib.sha256(args.source.read_bytes()).hexdigest())
    if args.source.suffix.lower()=='.blend':
        bpy.ops.wm.open_mainfile(filepath=str(args.source),load_ui=False,use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(args.source))
    expose()
    report=dict(source=str(args.source),sha256=stamp[1],units=bpy.context.scene.unit_settings.system,
                unit_scale=bpy.context.scene.unit_settings.scale_length,meshes={},rigs={})
    for obj in bpy.data.objects:
        if obj.type=='MESH' and not obj.name.startswith(('WGT-','root_Shape','Switch shape')):
            report['meshes'][obj.name]=describe(obj,args.output)
        elif obj.type=='ARMATURE':
            report['rigs'][obj.name]=dict(bones=len(obj.data.bones),deform=sum(b.use_deform for b in obj.data.bones),
                controls=[dict(name=b.name,head=list(b.head),tail=list(b.tail),deform=b.bone.use_deform,
                               parent=b.parent.name if b.parent else None,location=list(b.location),
                               lock_location=list(b.lock_location),constraints=[dict(name=c.name,type=c.type) for c in b.constraints]) for b in obj.pose.bones])
    assert stamp==(args.source.stat().st_mtime_ns,hashlib.sha256(args.source.read_bytes()).hexdigest())
    report['source_unchanged']=True
    (args.output/'inspection.json').write_text(json.dumps(report,indent=2))
    print('MALE INSPECTION COMPLETE',str(args.output),flush=True)


if __name__=='__main__':
    main()
