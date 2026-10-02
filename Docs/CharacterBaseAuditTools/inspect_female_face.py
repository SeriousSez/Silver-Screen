"""Adult-female-only read-only facial structure and mesh correspondence investigation."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
from mathutils.kdtree import KDTree


def constraint_data(c):
    values = {'type': c.type, 'name': c.name}
    for p in c.bl_rna.properties:
        if p.identifier in ('rna_type', 'name', 'type') or p.type == 'COLLECTION':
            continue
        try:
            v = getattr(c, p.identifier)
            if p.type == 'POINTER': v = v.name if v else None
            elif getattr(p, 'is_array', False): v = list(v)
            values[p.identifier] = v
        except (AttributeError, TypeError): pass
    return values


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    args.output.mkdir(parents=True, exist_ok=True)
    path = next(p for p in args.source.rglob('*FacialRig.blend') if 'female' in str(p).lower())
    before = (path.stat().st_mtime_ns, hashlib.sha256(path.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    # Both authoring variants are stored in hidden collections; force dependency evaluation in memory.
    for collection in bpy.data.collections: collection.hide_viewport = False
    def expose(layer):
        layer.exclude = False; layer.hide_viewport = False
        for child in layer.children: expose(child)
    expose(bpy.context.view_layer.layer_collection)
    for obj in bpy.data.objects:
        obj.hide_viewport = False
        obj.hide_set(False)
    bpy.context.view_layer.update()
    arm = bpy.data.objects['Armature_Character1.001']
    shape = bpy.data.objects['Character_shape_keys.002']
    report = {'source': str(path), 'bones': [], 'drivers': [], 'meshes': []}
    for pb in arm.pose.bones:
        report['bones'].append({'name': pb.name, 'parent': pb.parent.name if pb.parent else None,
            'deform': pb.bone.use_deform, 'head': list(pb.bone.head_local), 'tail': list(pb.bone.tail_local),
            'matrix': [list(row) for row in pb.matrix], 'rest_matrix': [list(row) for row in pb.bone.matrix_local],
            'custom': {k: v for k,v in pb.items() if isinstance(v, (str,int,float,bool))},
            'constraints': [constraint_data(c) for c in pb.constraints]})
    for owner in [arm, arm.data]:
        if not owner.animation_data: continue
        for fc in owner.animation_data.drivers:
            report['drivers'].append({'owner':owner.name, 'path':fc.data_path, 'index':fc.array_index,
                'expression':fc.driver.expression, 'variables':[{'name':v.name,'type':v.type,
                    'targets':[{'id':t.id.name if t.id else None,'path':t.data_path,'bone':t.bone_target} for t in v.targets]}
                    for v in fc.driver.variables]})
    tree = KDTree(len(shape.data.vertices))
    for v in shape.data.vertices: tree.insert(shape.matrix_world @ v.co, v.index)
    tree.balance()
    mappings = []
    all_used = []
    graph = bpy.context.evaluated_depsgraph_get()
    for obj in sorted(bpy.data.objects, key=lambda x:x.name):
        if obj.type != 'MESH' or obj.parent != arm: continue
        evaluated = obj.evaluated_get(graph)
        mesh = evaluated.to_mesh()
        pairs = [tree.find(evaluated.matrix_world @ v.co) for v in mesh.vertices]
        raw_pairs = [tree.find(obj.matrix_world @ v.co) for v in obj.data.vertices]
        mapping = [index for _, index, _ in pairs]
        all_used.extend(mapping)
        report['meshes'].append({'name':obj.name,'vertices':len(mesh.vertices),'parent_type':obj.parent_type,
            'parent_bone':obj.parent_bone,'modifiers':[{'type':m.type,'name':m.name} for m in obj.modifiers],
            'max_nearest_distance':max(d for _,_,d in pairs),'unique_target_vertices':len(set(mapping)),
            'raw_max_nearest_distance':max(d for _,_,d in raw_pairs),
            'raw_unique_target_vertices':len(set(i for _,i,_ in raw_pairs)),
            'over_1e5':sum(d>1e-5 for _,_,d in pairs),
            'weighted_groups':{g.name:sum(any(x.group==g.index and x.weight>1e-5 for x in v.groups) for v in obj.data.vertices) for g in obj.vertex_groups}})
        mappings.append({'name':obj.name,'indices':mapping,'raw_indices':[i for _,i,_ in raw_pairs],
            'polygons':[[mapping[i] for i in poly.vertices] for poly in mesh.polygons]})
        evaluated.to_mesh_clear()
    def canonical(poly):
        smallest=min(poly); i=poly.index(smallest)
        forward=tuple(poly[i:]+poly[:i]); rev=list(reversed(poly)); i=rev.index(smallest)
        return min(forward,tuple(rev[i:]+rev[:i]))
    source_polys={canonical(p) for m in mappings for p in m['polygons']}
    target_polys={canonical(list(p.vertices)) for p in shape.data.polygons}
    report['correspondence']={'source_vertices':len(all_used),'target_vertices':len(shape.data.vertices),
        'unique_mapped':len(set(all_used)), 'source_polygons':len(source_polys),'target_polygons':len(target_polys),
        'shared_polygons':len(source_polys & target_polys)}
    (args.output/'female-structure.json').write_text(json.dumps(report,indent=2))
    (args.output/'female-correspondence.json').write_text(json.dumps(mappings))
    assert before == (path.stat().st_mtime_ns, hashlib.sha256(path.read_bytes()).hexdigest())
    print(json.dumps(report['correspondence']))
    for m in report['meshes']: print(m['name'],m['vertices'],m['max_nearest_distance'],m['unique_target_vertices'])


if __name__=='__main__': main()
