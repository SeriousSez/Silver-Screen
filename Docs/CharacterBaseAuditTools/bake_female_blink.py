"""One-shape acceptance experiment on the validated lightweight Female FBX.

Run with Blender --background --factory-startup --disable-autoexec. Inputs are
read only. Generated geometry/evidence stays in the explicit local output folder.
No Unity import or downstream blink/gaze/brow experiment is implicit.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector


OFFSETS = {'character2.003': 0, 'GumsLower_lowres.003': 12937,
           'GumsLower_lowres.001': 13153, 'GumsUpper_lowres.001': 14377,
           'character2.004': 15789, 'character2.005': 16143, 'character2.002': 16497}


def canonical(poly):
    p = list(poly)
    i = p.index(min(p))
    a = tuple(p[i:] + p[:i])
    p.reverse()
    i = p.index(min(p))
    return min(a, tuple(p[i:] + p[:i]))


def evaluated(obj):
    bpy.context.view_layer.update()
    instance = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = instance.to_mesh()
    points = np.array([instance.matrix_world @ v.co for v in mesh.vertices])
    instance.to_mesh_clear()
    return points


def source_points(shape):
    points = np.zeros((len(shape.data.vertices), 3))
    for name, offset in OFFSETS.items():
        data = evaluated(bpy.data.objects[name])
        points[offset:offset + len(data)] = data
    return points


def fingerprint(obj, key_count):
    mesh = obj.data
    report = {'vertices': [list(v.co) for v in mesh.vertices],
              'topology': [list(p.vertices) for p in mesh.polygons],
              'uvs': {u.name: [list(v.uv) for v in u.data] for u in mesh.uv_layers},
              'normals': [list(n.vector) for n in mesh.corner_normals],
              'weights': [[(obj.vertex_groups[g.group].name, g.weight) for g in v.groups]
                          for v in mesh.vertices],
              'shapes': [{ 'name': k.name, 'min': k.slider_min, 'max': k.slider_max,
                          'relative': k.relative_key.name, 'value': k.value,
                          'coords': [list(v.co) for v in k.data]}
                         for k in list(mesh.shape_keys.key_blocks)[:key_count]]}
    return {name: hashlib.sha256(json.dumps(value, separators=(',', ':')).encode()).hexdigest()
            for name, value in report.items()}


def skeleton_fingerprint(arm):
    data = [{'name': bone.name, 'parent': bone.parent.name if bone.parent else None,
             'matrix': [list(row) for row in bone.matrix_local],
             'pose': [list(row) for row in arm.pose.bones[bone.name].matrix_basis]}
            for bone in arm.data.bones]
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def stats(errors, ids):
    values = errors[ids]
    return {'vertices': len(values), 'max_mm': float(values.max() * 1000),
            'mean_mm': float(values.mean() * 1000),
            'over_0_1mm': int(sum(values > .0001)), 'over_0_5mm': int(sum(values > .0005)),
            'over_1mm': int(sum(values > .001)), 'over_2mm': int(sum(values > .002))}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True, help='Original Adult Female facial .blend')
    parser.add_argument('--baseline', type=Path, required=True, help='Validated lightweight AdultFemale.fbx')
    parser.add_argument('--previous-evidence', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    args.output.mkdir(parents=True, exist_ok=True)
    before_files = {str(p): (p.stat().st_mtime_ns, hashlib.sha256(p.read_bytes()).hexdigest())
                    for p in (args.source, args.baseline)}
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
    shape = bpy.data.objects['Character_shape_keys.002']
    body = bpy.data.objects['character2.003']
    polygons = [list(p.vertices) for p in shape.data.polygons]
    source_polygons = {canonical([i + offset for i in p.vertices])
                       for name, offset in OFFSETS.items() for p in bpy.data.objects[name].data.polygons}
    assert source_polygons == {canonical(p) for p in polygons}
    # Define eyelid membership from source ownership before examining errors.
    lids = {side: np.array([v.index for v in body.data.vertices if any(
        body.vertex_groups[g.group].name.startswith('DEF-lid.') and
        ('.' + side) in body.vertex_groups[g.group].name and g.weight > .00001
        for g in v.groups)], dtype=int) for side in ('L', 'R')}
    face = np.array([v.index for v in body.data.vertices if any(
        body.vertex_groups[g.group].name.startswith('DEF-') and g.weight > .00001
        for g in v.groups)], dtype=int)
    neutral = source_points(shape)
    for side in ('L', 'R'):
        for part, dz in [('T', -.0255), ('B', .0055)]:
            control = arm.pose.bones[f'lid.{part}.{side}.002']
            control.location += control.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, dz))
    blink = source_points(shape)
    previous = args.previous_evidence
    repeated = {name: float(np.linalg.norm(points - np.load(previous / (name + '.npz'))['points'], axis=1).max())
                for name, points in [('Neutral', neutral), ('BlinkBoth', blink)]}
    assert max(repeated.values()) < .000002, 'Source endpoint changed relative to previous authority.'
    driver_state = [{'path': f.data_path, 'valid': f.driver.is_valid} for f in arm.animation_data.drivers]
    np.savez_compressed(args.output / 'source.npz', neutral=neutral, blink=blink,
                        lids_L=lids['L'], lids_R=lids['R'], face=face)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.baseline), use_anim=False)
    baseline = next(o for o in bpy.data.objects if o.type == 'MESH')
    body_arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    assert len(body_arm.data.bones) == 51
    assert len(baseline.data.shape_keys.key_blocks) == 30
    assert len(baseline.data.vertices) == len(neutral)
    assert [canonical(p.vertices) for p in baseline.data.polygons] == [canonical(p) for p in polygons]
    baseline.name = 'ValidatedLightweightBaseline'
    original_fingerprint = fingerprint(baseline, 30)
    original_skeleton = skeleton_fingerprint(body_arm)
    baseline_points = evaluated(baseline)
    candidate = baseline.copy()
    candidate.data = baseline.data.copy()
    candidate.name = 'FemaleBakedBlinkBoth_Candidate'
    bpy.context.scene.collection.objects.link(candidate)
    key = candidate.shape_key_add(name='BlinkBoth', from_mix=False)
    basis = candidate.data.shape_keys.key_blocks[0]
    # Deformation is measured in a common world space, relative to the evaluated
    # source neutral. Only this delta is transported to the untouched FBX Basis.
    # Copying source absolute coordinates would silently replace the baseline face.
    delta = blink - neutral
    inverse = np.array(candidate.matrix_world.to_3x3().inverted())
    local_delta = delta @ inverse.T
    for i, vertex in enumerate(key.data):
        vertex.co = basis.data[i].co + Vector(local_delta[i])
    key.value = 0
    bpy.context.view_layer.update()
    after_fingerprint = fingerprint(candidate, 30)
    assert after_fingerprint == original_fingerprint, 'Baseline data changed while adding the new shape.'
    neutral_candidate = evaluated(candidate)
    key.value = 1
    candidate_points = evaluated(candidate)
    key.value = 0
    restored = evaluated(candidate)
    assert fingerprint(candidate, 30) == original_fingerprint
    assert skeleton_fingerprint(body_arm) == original_skeleton
    regions = {'all': np.arange(len(neutral)), 'face': face,
               'eyelids': np.union1d(lids['L'], lids['R']), 'left_eyelid': lids['L'], 'right_eyelid': lids['R'],
               'oral_pieces': np.arange(12937, 15789), 'eyes': np.arange(15789, 16497)}
    errors = {'neutral_source_vs_baseline': np.linalg.norm(baseline_points - neutral, axis=1),
              'absolute_closed_surface': np.linalg.norm(candidate_points - blink, axis=1),
              'transferred_delta': np.linalg.norm((candidate_points - baseline_points) - delta, axis=1),
              'neutral_restoration': np.linalg.norm(restored - baseline_points, axis=1),
              'blink_motion': np.linalg.norm(candidate_points - baseline_points, axis=1)}
    report = {'method': 'Evaluated source BlinkBoth minus source Neutral, world-space displacement transported to unchanged baseline FBX Basis.',
              'source': str(args.source), 'baseline': str(args.baseline), 'source_endpoint_repeat_max_m': repeated,
              'matched_polygons_in_order': len(polygons), 'vertex_count': len(neutral),
              'source_driver_state': driver_state, 'preserved_baseline_hashes': original_fingerprint,
              'candidate_existing_data_hashes': after_fingerprint,
              'preserved_skeleton_hash': original_skeleton,
              'candidate_skeleton_hash': skeleton_fingerprint(body_arm),
              'bones': len(body_arm.data.bones), 'shape_names': [k.name for k in candidate.data.shape_keys.key_blocks][1:],
              'metrics': {kind: {name: stats(values, indices) for name, indices in regions.items()}
                          for kind, values in errors.items()},
              'visual_acceptance': 'Pending matched-view inspection; numeric delta parity alone is insufficient.'}
    np.savez_compressed(args.output / 'candidate.npz', baseline=baseline_points, blink=candidate_points,
                        neutral=neutral_candidate, restored=restored, local_delta=local_delta)
    (args.output / 'topology.json').write_text(json.dumps(polygons))
    candidate.hide_render = True
    # Save only derived data to a separate local evidence file, never to either input.
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str((args.output / 'FemaleBlinkBoth.blend').resolve()))
    for path, old in before_files.items():
        p = Path(path)
        assert (p.stat().st_mtime_ns, hashlib.sha256(p.read_bytes()).hexdigest()) == old
    report['input_files_unchanged'] = True
    (args.output / 'blink-bake.json').write_text(json.dumps(report, indent=2))
    print(json.dumps({k: v for k, v in report.items() if k in ('bones', 'shape_names', 'metrics', 'source_endpoint_repeat_max_m')}, indent=2))


if __name__ == '__main__':
    main()
