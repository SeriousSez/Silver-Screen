"""Remove only the verified four hair islands from a copied lightweight FBX.

No scalp repair, remeshing, material changes or source writes. Existing per-vertex
and per-corner data are checked against the retained subset before FBX export.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bmesh
import bpy
import numpy as np

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from bake_female_blink import skeleton_fingerprint
from inspect_female_hair import describe


def subset(obj, keep_count):
    mesh = obj.data
    faces = [p for p in mesh.polygons if all(i < keep_count for i in p.vertices)]
    loops = [i for p in faces for i in p.loop_indices]
    return {'vertices': [list(v.co) for v in list(mesh.vertices)[:keep_count]],
            'polygons': [list(p.vertices) for p in faces],
            'materials': [p.material_index for p in faces],
            'smooth': [p.use_smooth for p in faces],
            'uvs': {u.name: [list(u.data[i].uv) for i in loops] for u in mesh.uv_layers},
            'normals': [list(mesh.corner_normals[i].vector) for i in loops],
            'weights': [[(obj.vertex_groups[g.group].name, g.weight) for g in v.groups]
                        for v in list(mesh.vertices)[:keep_count]],
            'shapes': [{'name': k.name, 'min': k.slider_min, 'max': k.slider_max,
                        'relative': k.relative_key.name, 'value': k.value,
                        'coords': [list(v.co) for v in list(k.data)[:keep_count]]}
                       for k in mesh.shape_keys.key_blocks]}


def hashes(data):
    return {k: hashlib.sha256(json.dumps(v, separators=(',', ':')).encode()).hexdigest() for k,v in data.items()}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--inspection', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--fbx', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    assert args.baseline.resolve() != args.fbx.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    args.fbx.parent.mkdir(parents=True, exist_ok=True)
    info = json.loads(args.inspection.read_text())
    keep_count = info['hair_start']
    assert keep_count == 16497 and not info['cross_partition_edges']
    before = (args.baseline.stat().st_mtime_ns, hashlib.sha256(args.baseline.read_bytes()).hexdigest())
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.baseline), use_anim=False)
    baseline = next(o for o in bpy.data.objects if o.type == 'MESH')
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    assert len(baseline.data.vertices) == 17571 and len(arm.data.bones) == 51
    assert len(baseline.data.shape_keys.key_blocks) == 30
    hair_ids = set(range(keep_count, len(baseline.data.vertices)))
    assert not any((e.vertices[0] in hair_ids) != (e.vertices[1] in hair_ids) for e in baseline.data.edges)
    before_description = describe(baseline)
    hair_components = [c for c in before_description['components'] if c['min_index'] >= keep_count]
    assert sorted(c['vertices'] for c in hair_components) == [16,16,258,784]
    reference = subset(baseline, keep_count)
    bone_hash = skeleton_fingerprint(arm)
    candidate = baseline.copy()
    candidate.data = baseline.data.copy()
    candidate.name = 'AdultFemaleBald'
    bpy.context.scene.collection.objects.link(candidate)
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    candidate.select_set(True)
    bpy.context.view_layer.objects.active = candidate
    candidate.active_shape_key_index = 0
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(candidate.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index in hair_ids], context='VERTS')
    bmesh.update_edit_mesh(candidate.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    assert len(candidate.data.vertices) == keep_count
    # Disconnected deletion must retain corner normals, including authored splits.
    candidate.data.normals_split_custom_set(reference['normals'])
    preserved = subset(candidate, keep_count)
    preservation = {k: reference[k] == preserved[k] for k in reference}
    # Custom normal storage is quantized by Blender; measure rather than claim bit equality.
    normal_error = np.linalg.norm(np.array(reference['normals'])-np.array(preserved['normals']), axis=1)
    assert max(normal_error) < .0002
    assert all(v for k,v in preservation.items() if k != 'normals'), preservation
    assert skeleton_fingerprint(arm) == bone_hash
    candidate.data.calc_loop_triangles()
    assert len(candidate.data.loop_triangles) == 32512
    report = {'input': str(args.baseline), 'output': str(args.fbx), 'removed_vertex_indices': sorted(hair_ids),
              'retained_old_indices': list(range(keep_count)), 'removed_components': hair_components,
              'before': before_description, 'after': describe(candidate),
              'retained_data_equal': preservation, 'retained_hashes_before': hashes(reference), 'retained_hashes_after': hashes(preserved),
              'neutral_normal_max_vector_error': float(max(normal_error)), 'skeleton_before': bone_hash,
              'skeleton_after': skeleton_fingerprint(arm), 'bones': len(arm.data.bones),
              'shapes': [k.name for k in candidate.data.shape_keys.key_blocks][1:]}
    # Keep the original comparison inside the local .blend but exclude it from export.
    baseline.hide_render = True
    baseline.hide_set(True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str((args.output/'AdultFemaleBald.blend').resolve()))
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    candidate.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=str(args.fbx.resolve()), use_selection=True,
        object_types={'ARMATURE','MESH'}, use_mesh_modifiers=False, add_leaf_bones=False,
        use_armature_deform_only=False, axis_forward='-Z', axis_up='Y', bake_anim=False,
        path_mode='STRIP', mesh_smooth_type='OFF')
    assert before == (args.baseline.stat().st_mtime_ns, hashlib.sha256(args.baseline.read_bytes()).hexdigest())
    report['baseline_unchanged'] = True
    (args.output/'bald-export.json').write_text(json.dumps(report, indent=2))
    print(json.dumps({k: v for k,v in report.items() if k in ('retained_data_equal','neutral_normal_max_vector_error','bones','shapes')}, indent=2))


if __name__ == '__main__':
    main()
