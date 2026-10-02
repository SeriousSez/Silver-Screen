"""Read-only Adult Female hair/scalp inspection with matched technical views."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector


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
    return sorted(groups.values(), key=len, reverse=True)


def describe(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    points = np.array([obj.matrix_world @ v.co for v in mesh.vertices])
    edges = Counter()
    for p in mesh.polygons:
        for a, b in zip(p.vertices, list(p.vertices[1:]) + [p.vertices[0]]):
            edges[tuple(sorted((a, b)))] += 1
    return {'vertices': len(mesh.vertices), 'triangles': len(mesh.loop_triangles),
            'polygons': len(mesh.polygons), 'edge_face_counts': dict(Counter(edges.values())),
            'components': [{'vertices': len(ids), 'min_index': min(ids), 'max_index': max(ids),
                            'bounds_min': points[ids].min(0).tolist(), 'bounds_max': points[ids].max(0).tolist()}
                           for ids in components(mesh)],
            'materials': [m.name if m else None for m in mesh.materials],
            'uv_layers': [u.name for u in mesh.uv_layers]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--no-render', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.output.mkdir(parents=True, exist_ok=True)
    stamp = (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(args.source), load_ui=False, use_scripts=False)
    for collection in bpy.data.collections:
        collection.hide_viewport = False
    def expose(layer):
        layer.exclude = False
        layer.hide_viewport = False
        for child in layer.children:
            expose(child)
    expose(bpy.context.view_layer.layer_collection)
    for obj in bpy.data.objects:
        obj.hide_viewport = False
        obj.hide_set(False)
    bpy.context.view_layer.update()
    arm = bpy.data.objects['Armature_Character1.001']
    hair = bpy.data.objects['character2.002']
    body = bpy.data.objects['character2.003']
    joined = bpy.data.objects['Character_shape_keys.002']
    start = 16497
    assert len(hair.data.vertices) == 1074 and len(joined.data.vertices) == start+1074
    canonical = lambda p: tuple(sorted(p))
    mapped = {canonical([i+start for i in p.vertices]) for p in hair.data.polygons}
    joined_hair = {canonical(p.vertices) for p in joined.data.polygons if min(p.vertices) >= start}
    assert mapped == joined_hair
    bridges = [list(e.vertices) for e in joined.data.edges if (e.vertices[0] >= start) != (e.vertices[1] >= start)]
    assert not bridges, 'Hair shares edges with retained geometry; stop.'
    brows = [b.name for b in arm.data.bones if 'brow' in b.name and b.use_deform]
    brow_members = {b: [v.index for v in body.data.vertices if any(
        body.vertex_groups[g.group].name == b and g.weight > .00001 for g in v.groups)] for b in brows}
    hair_weights = [{hair.vertex_groups[g.group].name: g.weight for g in v.groups if g.weight > .00001}
                    for v in hair.data.vertices]
    report = {'source': str(args.source), 'body': describe(body), 'hair': describe(hair),
              'joined': describe(joined), 'hair_start': start, 'cross_partition_edges': bridges,
              'matched_hair_polygons': len(mapped), 'hair_weight_patterns': dict(Counter(json.dumps(w, sort_keys=True) for w in hair_weights)),
              'brow_deform_bones': brows, 'body_brow_membership': {k:len(v) for k,v in brow_members.items()},
              'source_character_meshes': [o.name for o in bpy.data.objects if o.type=='MESH' and o.parent==arm],
              'body_colour_attributes': [a.name for a in body.data.color_attributes],
              'image_textures': [{'material': m.name, 'image': n.image.name if n.image else None}
                  for m in bpy.data.materials if m.node_tree for n in m.node_tree.nodes if n.type=='TEX_IMAGE']}
    report['colour_attributes'] = {}
    for obj in (body, joined):
        report['colour_attributes'][obj.name] = {}
        for attribute in obj.data.color_attributes:
            colours = np.array([list(v.color) for v in attribute.data])
            report['colour_attributes'][obj.name][attribute.name] = {
                'domain': attribute.domain, 'min': colours.min(0).tolist(), 'max': colours.max(0).tolist(),
                'distinct_colours': len(np.unique(colours,axis=0))}
    report['material_nodes'] = {m.name: [{'type':n.type, 'name':n.name,
        'attribute': getattr(n,'attribute_name',None), 'layer':getattr(n,'layer_name',None)} for n in m.node_tree.nodes]
        for m in bpy.data.materials if m.node_tree}
    graph = bpy.context.evaluated_depsgraph_get()
    pieces = {}
    for obj in bpy.data.objects:
        if obj.type != 'MESH' or obj.parent != arm:
            continue
        instance = obj.evaluated_get(graph)
        mesh = instance.to_mesh()
        pieces[obj.name] = (np.array([instance.matrix_world @ v.co for v in mesh.vertices]),
                            [list(p.vertices) for p in mesh.polygons])
        instance.to_mesh_clear()
    # Verify removal does not influence the actual full facial dependency graph.
    rest = {p.name: p.matrix_basis.copy() for p in arm.pose.bones}
    retained_names = [n for n in pieces if n != hair.name]
    def sample(pose):
        for name, matrix in rest.items():
            arm.pose.bones[name].matrix_basis = matrix
        for side in ('L', 'R'):
            controls = ([(f'lid.T.{side}.002', -.0255), (f'lid.B.{side}.002', .0055)] if pose=='BlinkBoth'
                        else [(f'brow.T.{side}'+suffix, .008) for suffix in ('','.001','.003')] if pose=='BrowRaiseBoth' else [])
            for name, dz in controls:
                control = arm.pose.bones[name]
                control.location += control.bone.matrix_local.to_3x3().inverted() @ Vector((0,0,dz))
        bpy.context.view_layer.update()
        graph = bpy.context.evaluated_depsgraph_get()
        result = {}
        for name in retained_names:
            instance = bpy.data.objects[name].evaluated_get(graph)
            mesh = instance.to_mesh()
            result[name] = np.array([instance.matrix_world @ v.co for v in mesh.vertices])
            instance.to_mesh_clear()
        return result
    poses = ['Neutral','BlinkBoth','BrowRaiseBoth']
    original = {pose: sample(pose) for pose in poses}
    bpy.data.objects.remove(hair, do_unlink=True)
    report['facial_rig_removal_parity'] = {}
    for pose in poses:
        removed = sample(pose)
        errors = {name: float(np.linalg.norm(removed[name]-original[pose][name],axis=1).max()) for name in retained_names}
        assert max(errors.values()) < .000001
        report['facial_rig_removal_parity'][pose] = errors
    report['source_unchanged'] = stamp == (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    (args.output/'hair-source.json').write_text(json.dumps(report, indent=2))
    if args.no_render:
        print('Metadata and facial removal parity saved; existing renders retained.')
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.render.resolution_x = scene.render.resolution_y = 850
    scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('Technical world')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.16,.18,.2,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
    material = bpy.data.materials.new('Neutral technical inspection')
    material.diffuse_color = (.65,.65,.65,1)
    for name, (points, faces) in pieces.items():
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(points.tolist(), [], faces)
        obj = bpy.data.objects.new(name, mesh)
        scene.collection.objects.link(obj)
        mesh.materials.append(material)
        for face in mesh.polygons:
            face.use_smooth = True
    hair = bpy.data.objects['character2.002']
    camera_data = bpy.data.cameras.new('Head inspection camera')
    camera = bpy.data.objects.new(camera_data.name, camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = .35
    light_data = bpy.data.lights.new('Technical key', 'AREA')
    light_data.energy = 55
    light_data.size = .55
    light = bpy.data.objects.new(light_data.name, light_data)
    scene.collection.objects.link(light)
    target = Vector((0,.018,1.696))
    views = {'front': (0,-.85,1.71), 'three-quarter': (.6,-.65,1.78),
             'profile': (.85,.02,1.72), 'back': (0,.85,1.73), 'top': (0,.02,2.55)}
    for view, position in views.items():
        camera.location = position
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        light.location = camera.location + Vector((-.25,-.1,.25))
        light.rotation_euler = (target-light.location).to_track_quat('-Z','Y').to_euler()
        for keep_hair in ([True,False] if view=='three-quarter' else [False]):
            hair.hide_render = not keep_hair
            scene.render.filepath = str((args.output/(view+('-hair' if keep_hair else '-bald')+'.png')).resolve())
            bpy.ops.render.render(write_still=True)
    print(json.dumps({k:v for k,v in report.items() if k not in ('body','joined')}, indent=2))


if __name__ == '__main__':
    main()
