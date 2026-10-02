"""Source-only blink control calibration; evaluates the complete purchased rig.

Never saves the source or generates a runtime shape. Candidate controls are
armature-axis translations in millimetres, applied through each control's axes.
Visibility uses the established full-surface first-hit BVH cameras and grid.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from bake_female_blink import OFFSETS, source_points
from inspect_female_face import constraint_data


def visibility_grid(neutral, faces, settings, step):
    eyes = {'R': np.arange(15789, 16143), 'L': np.arange(16143, 16497)}
    eye_polys = {i: side for side, ids in eyes.items() for i, p in enumerate(faces)
                 if min(p) >= ids[0] and max(p) <= ids[-1]}
    grids = {}
    for setting in settings:
        origin = Vector(setting['position'])
        direction = (Vector(setting['target'])-origin).normalized()
        rotation = direction.to_track_quat('-Z', 'Y')
        right, up = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0))
        pts = neutral[np.concatenate(list(eyes.values()))]
        x = np.array([Vector(p-origin).dot(right) for p in pts])
        y = np.array([Vector(p-origin).dot(up) for p in pts])
        xs = np.arange(x.min()-.003, x.max()+.003, step)
        ys = np.arange(y.min()-.003, y.max()+.003, step)
        rays = [(xi, yi, origin+right*float(xx)+up*float(yy))
                for yi, yy in enumerate(ys) for xi, xx in enumerate(xs)]
        grids[setting['view']] = (direction, xs, ys, rays)
    return eyes, eye_polys, grids


def measure_visibility(points, faces, eye_polys, grids, step, save):
    bvh = BVHTree.FromPolygons(points.tolist(), faces)
    report = {}
    for view, (direction, xs, ys, rays) in grids.items():
        hits = {'R': [], 'L': []}
        for xi, yi, origin in rays:
            _, _, polygon, _ = bvh.ray_cast(origin, direction, 2)
            side = eye_polys.get(polygon)
            if side:
                hits[side].append((xi, yi))
        report[view] = {}
        for side, samples in hits.items():
            columns = {}
            for xi, yi in samples:
                columns.setdefault(xi, []).append(yi)
            report[view][side] = {
                'visible_samples': len(samples), 'area_mm2': len(samples)*(step*1000)**2,
                'max_summed_span_mm': max((len(v)*step*1000 for v in columns.values()), default=0),
                'max_extent_span_mm': max(((max(v)-min(v)+1)*step*1000 for v in columns.values()), default=0)}
        np.savez_compressed(str(save)+'-'+view+'-hits.npz', xs=xs, ys=ys,
                            **{side: np.array(v, dtype=int).reshape(-1, 2) for side, v in hits.items()})
    return report


def surface_metrics(points, neutral, faces, eyes, lids, regions):
    movement = np.linalg.norm(points-neutral, axis=1)*1000
    result = {'movement': {name: {'vertices': len(ids), 'max_mm': float(movement[ids].max()),
                                  'mean_mm': float(movement[ids].mean()),
                                  'over_0_1mm': int(sum(movement[ids] > .1))}
                           for name, ids in regions.items() if len(ids)}, 'eye_surface': {}}
    # Signed nearest-plane distances are a diagnostic, not a collision certificate.
    # Baseline inner eyelid vertices may already lie inside the eye surface.
    for side, ids in eyes.items():
        polys = [[v-int(ids[0]) for v in p] for p in faces if min(p) >= ids[0] and max(p) <= ids[-1]]
        eye = BVHTree.FromPolygons(points[ids].tolist(), polys)
        signed = []
        for i in lids[side]:
            hit, normal, _, distance = eye.find_nearest(Vector(points[i]))
            signed.append(float((Vector(points[i])-hit).dot(normal))*1000)
        result['eye_surface'][side] = {
            'lid_vertices': len(signed), 'minimum_signed_nearest_plane_mm': min(signed),
            'inside_over_0_1mm': sum(v < -.1 for v in signed),
            'inside_over_1mm': sum(v < -1 for v in signed),
            'near_surface_within_0_2mm': sum(abs(v) <= .2 for v in signed)}
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--previous', type=Path, required=True)
    parser.add_argument('--candidates', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--step-mm', type=float, default=.2)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.output.mkdir(parents=True, exist_ok=True)
    before = (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(args.source), load_ui=False, use_scripts=False)
    for c in bpy.data.collections:
        c.hide_viewport = False
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
    rest = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
    neutral = source_points(shape)
    old = np.load(args.previous/'source.npz')
    assert np.array_equal(neutral, old['neutral']), 'Neutral differs from previous source evidence.'
    faces = [list(p.vertices) for p in shape.data.polygons]
    assert faces == json.loads((args.previous/'topology.json').read_text())
    settings = json.loads((args.previous/'render-settings.json').read_text())
    eyes, eye_polys, grids = visibility_grid(neutral, faces, settings, args.step_mm/1000)
    lids = {}
    for side, ids in eyes.items():
        pts = neutral[ids]
        window = np.all((neutral >= pts.min(0)-.004) & (neutral <= pts.max(0)+.004), axis=1)
        lids[side] = old['lids_'+side][window[old['lids_'+side]]]
    broad = np.union1d(old['lids_L'], old['lids_R'])
    local = np.union1d(lids['L'], lids['R'])
    regions = {'all': np.arange(len(neutral)), 'local_lids': local, 'broad_lids': broad,
               'body_outside_lid_weights': np.setdiff1d(np.arange(12937), broad),
               'body_outside_eye_windows': np.setdiff1d(np.arange(12937), local),
               'oral': np.arange(12937, 15789), 'eyes': np.arange(15789, 16497),
               'hair': np.arange(16497, 17571)}
    for region in ('nose', 'cheek', 'brow'):
        regions[region] = np.array([v.index for v in body.data.vertices if any(
            body.vertex_groups[g.group].name.startswith('DEF-'+region) and g.weight > .00001 for g in v.groups)])
    report = {'source': str(args.source), 'source_sha256': before[1], 'step_mm': args.step_mm,
              'cameras': settings, 'controls': {}, 'candidates': {}}
    for pb in arm.pose.bones:
        if pb.name.startswith('lid.'):
            report['controls'][pb.name] = {'head': list(pb.head), 'lock_location': list(pb.lock_location),
                                         'constraints': [constraint_data(c) for c in pb.constraints]}
    np.savez_compressed(args.output/'neutral.npz', points=neutral, lids_L=lids['L'], lids_R=lids['R'],
                        **{'region_'+k: v for k,v in regions.items()})
    (args.output/'topology.json').write_text(json.dumps(faces))
    (args.output/'render-settings.json').write_text(json.dumps(settings, indent=2))
    candidates = json.loads(args.candidates.read_text())
    for candidate in candidates:
        name = candidate['name']
        for n, matrix in rest.items():
            arm.pose.bones[n].matrix_basis = matrix
        bpy.context.view_layer.update()
        restored_before = source_points(shape)
        assert np.array_equal(restored_before, neutral), 'Source neutral reset changed.'
        for n, delta_mm in candidate['controls_mm'].items():
            assert n.startswith('lid.') and not n.startswith('DEF-')
            pb = arm.pose.bones[n]
            pb.location += pb.bone.matrix_local.to_3x3().inverted() @ Vector(tuple(v/1000 for v in delta_mm))
        points = source_points(shape)
        assert np.isfinite(points).all()
        if name == 'previous':
            assert np.array_equal(points, old['blink']), 'Previous endpoint did not reproduce.'
        if name == 'neutral':
            assert np.array_equal(points, neutral)
        else:
            np.savez_compressed(args.output/(name+'.npz'), points=points)
        data = {'controls_mm': candidate['controls_mm'],
                'visibility': measure_visibility(points, faces, eye_polys, grids, args.step_mm/1000, args.output/name),
                **surface_metrics(points, neutral, faces, eyes, lids, regions),
                'neutral_reset_max_mm': float(np.linalg.norm(restored_before-neutral, axis=1).max()*1000),
                'driver_invalid': [f.data_path for f in arm.animation_data.drivers if not f.driver.is_valid]}
        report['candidates'][name] = data
        (args.output/'calibration.json').write_text(json.dumps(report, indent=2))
        print(name, json.dumps(data['visibility']), 'local motion mm', data['movement']['local_lids']['max_mm'], flush=True)
    for n, matrix in rest.items():
        arm.pose.bones[n].matrix_basis = matrix
    restored = source_points(shape)
    assert np.array_equal(restored, neutral)
    assert (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest()) == before
    report['final_neutral_reset_exact'] = True
    report['source_unchanged'] = True
    (args.output/'calibration.json').write_text(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
