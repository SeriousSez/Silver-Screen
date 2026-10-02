"""Read-only purchased Blender/FBX audit. Run with Blender --background --disable-autoexec.

Never saves a .blend, exports geometry, or executes embedded source scripts.
Outputs derived metadata only. Source directory and output are explicit CLI args.
"""
import argparse
import hashlib
import json
import struct
import sys
from collections import Counter
from pathlib import Path

import bpy
from mathutils import Vector


def digest(values):
    return hashlib.sha256(json.dumps(values, separators=(',', ':')).encode()).hexdigest()


def inspect(path):
    if path.suffix.lower() == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
    result = {'unit_system': bpy.context.scene.unit_settings.system,
              'unit_scale': bpy.context.scene.unit_settings.scale_length,
              'meshes': [], 'armatures': [], 'images': [],
              'actions': [a.name for a in bpy.data.actions]}
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            mesh = obj.data
            mesh.calc_loop_triangles()
            coords = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
            deformers = [m.object.name if m.object else None for m in obj.modifiers if m.type == 'ARMATURE']
            keys = []
            if mesh.shape_keys:
                for key in mesh.shape_keys.key_blocks:
                    keys.append({'name': key.name, 'min': key.slider_min, 'max': key.slider_max,
                                 'relative': key.relative_key.name, 'value': key.value})
            weighted = Counter()
            unweighted = 0
            max_weights = 0
            for vert in mesh.vertices:
                groups = [g for g in vert.groups if g.weight > .00001]
                if not groups:
                    unweighted += 1
                max_weights = max(max_weights, len(groups))
                for group in groups:
                    weighted[obj.vertex_groups[group.group].name] += 1
            result['meshes'].append({
                'name': obj.name, 'data': mesh.name, 'vertices': len(mesh.vertices),
                'triangles': len(mesh.loop_triangles), 'polygons': len(mesh.polygons),
                'polygon_sizes': dict(Counter(len(p.vertices) for p in mesh.polygons)),
                'materials': [m.name if m else None for m in mesh.materials],
                'material_faces': dict(Counter(p.material_index for p in mesh.polygons)),
                'uv_maps': [uv.name for uv in mesh.uv_layers],
                'topology_hash': digest([list(p.vertices) for p in mesh.polygons]),
                'uv_hashes': [digest([[round(v, 6) for v in d.uv] for d in uv.data]) for uv in mesh.uv_layers],
                'shape_keys': keys, 'armature_modifiers': deformers,
                'parent': obj.parent.name if obj.parent else None, 'parent_type': obj.parent_type,
                'hide_render': obj.hide_render, 'hide_viewport': obj.hide_viewport,
                'bounds_min': [min(c[i] for c in coords) for i in range(3)],
                'bounds_max': [max(c[i] for c in coords) for i in range(3)],
                'scale': list(obj.scale), 'modifiers': [{'type': m.type, 'name': m.name,
                    'viewport': m.show_viewport, 'render': m.show_render,
                    'levels': getattr(m, 'levels', None)} for m in obj.modifiers],
                'weighted_groups': dict(weighted), 'unweighted_vertices': unweighted,
                'max_vertex_groups': max_weights})
        if obj.type == 'ARMATURE':
            bones = [{'name': b.name, 'parent': b.parent.name if b.parent else None,
                      'deform': b.use_deform, 'head': list(b.head_local), 'tail': list(b.tail_local),
                      'constraints': [c.type for c in obj.pose.bones[b.name].constraints]}
                     for b in obj.data.bones]
            result['armatures'].append({'name': obj.name, 'bones': bones,
                'bone_count': len(bones), 'deform_count': sum(b['deform'] for b in bones),
                'hierarchy_hash': digest(sorted((b['name'], b['parent']) for b in bones)),
                'scale': list(obj.scale)})
    for im in bpy.data.images:
        result['images'].append({'name': im.name, 'path': im.filepath,
                                 'packed': bool(im.packed_file), 'size': list(im.size)})
    result['materials'] = []
    for mat in bpy.data.materials:
        result['materials'].append({'name': mat.name, 'diffuse_color': list(mat.diffuse_color),
            'nodes': [{'type': n.type, 'name': n.name,
                       'image': n.image.name if n.type == 'TEX_IMAGE' and n.image else None}
                      for n in mat.node_tree.nodes] if mat.node_tree else []})
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    args.output.mkdir(parents=True, exist_ok=True)
    inventory = []
    for path in sorted(args.source.rglob('*')):
        if path.is_file():
            stat = path.stat()
            inventory.append({'path': path.relative_to(args.source).as_posix(), 'bytes': stat.st_size,
                              'mtime_ns': stat.st_mtime_ns, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    (args.output / 'inventory.json').write_text(json.dumps(inventory, indent=2))
    reports = []
    for entry in inventory:
        path = args.source / entry['path']
        if path.suffix.lower() not in ('.blend', '.fbx'):
            continue
        print('AUDIT ' + entry['path'], flush=True)
        try:
            report = inspect(path)
        except Exception as ex:
            report = {'error': str(ex)}
        report['path'] = entry['path']
        reports.append(report)
        (args.output / 'source-audit.json').write_text(json.dumps(reports, indent=2))
    unchanged = all((args.source / e['path']).stat().st_mtime_ns == e['mtime_ns'] and
                    hashlib.sha256((args.source / e['path']).read_bytes()).hexdigest() == e['sha256']
                    for e in inventory)
    (args.output / 'preservation.json').write_text(json.dumps({'source_unchanged': unchanged, 'files': len(inventory)}))
    assert unchanged


if __name__ == '__main__':
    main()
