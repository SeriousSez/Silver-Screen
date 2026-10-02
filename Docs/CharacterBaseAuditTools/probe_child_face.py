"""Bounded structural probes on the complete purchased child facial rig.

Three bilateral poses only: neutral, partial, strong. No search or corrective.
Family partitions are explicit reviewed component bindings, never adult data.
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
from inspect_male_foundation import expose, components


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--family', choices=['Boy', 'Girl'], required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    out = args.output
    out.mkdir(parents=True, exist_ok=True)
    stamp = (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(args.source), load_ui=False, use_scripts=False)
    expose()
    obj = bpy.data.objects['BoyBaseMesh_FaceRig' if args.family == 'Boy' else 'GirlBaseMesh_Facial_Rig']
    arm = obj.find_armature()
    groups = components(obj.data)
    expected = 17477 if args.family == 'Boy' else 17876
    assert len(obj.data.vertices) == expected
    hair_components = [5, *range(7, 19), 51] if args.family == 'Boy' else [1, 6, 7]
    eye_components = {'L': 3, 'R': 4} if args.family == 'Boy' else {'L': 4, 'R': 5}
    hair = np.array([i for g in hair_components for i in groups[g]])
    eyes = {s: np.array(groups[g]) for s, g in eye_components.items()}
    assert all(len(ids) == 482 for ids in eyes.values())
    body = np.array(groups[0])
    faces = [list(p.vertices) for p in obj.data.polygons]
    hair_set = set(hair.tolist())
    bald_faces = [f for f in faces if not any(v in hair_set for v in f)]
    eye_sets = {s: set(ids.tolist()) for s, ids in eyes.items()}
    eye_polys = {i: s for i, f in enumerate(bald_faces) for s, ids in eye_sets.items() if all(v in ids for v in f)}
    rest = {b.name: b.matrix_basis.copy() for b in arm.pose.bones}
    def reset():
        for name, matrix in rest.items():
            arm.pose.bones[name].matrix_basis = matrix
        bpy.context.view_layer.update()
    def sample():
        bpy.context.view_layer.update()
        e = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        m = e.to_mesh()
        points = np.array([e.matrix_world @ v.co for v in m.vertices])
        e.to_mesh_clear()
        assert len(points) == expected and np.isfinite(points).all()
        return points
    def move(name, delta):
        b = arm.pose.bones[name]
        b.location += b.bone.matrix_local.to_3x3().inverted() @ Vector(delta)
    reset()
    neutral = sample()
    center = neutral[np.concatenate(list(eyes.values()))].mean(0)
    settings = [dict(view='front', position=(center+np.array([0, -.8, 0])).tolist(), target=center.tolist(), ortho_scale=.19),
                dict(view='oblique', position=(center+np.array([.42, -.7, .04])).tolist(), target=center.tolist(), ortho_scale=.22)]
    grids = {}
    step = .00025
    for setting in settings:
        origin = Vector(setting['position'])
        direction = (Vector(setting['target'])-origin).normalized()
        q = direction.to_track_quat('-Z', 'Y')
        right, up = q @ Vector((1, 0, 0)), q @ Vector((0, 1, 0))
        p = neutral[np.concatenate(list(eyes.values()))]-np.array(origin)
        xs, ys = p @ np.array(right), p @ np.array(up)
        grids[setting['view']] = (direction, [(xi, yi, origin+right*float(x)+up*float(y))
            for xi, x in enumerate(np.arange(xs.min()-.002, xs.max()+.002, step))
            for yi, y in enumerate(np.arange(ys.min()-.002, ys.max()+.002, step))])
    def visibility(points):
        bvh = BVHTree.FromPolygons(points.tolist(), bald_faces)
        result = {}
        for name, (direction, rays) in grids.items():
            hits = {s: {} for s in eyes}
            for x, y, origin in rays:
                _, _, poly, _ = bvh.ray_cast(origin, direction, 2)
                if poly in eye_polys:
                    hits[eye_polys[poly]].setdefault(x, []).append(y)
            result[name] = {s: dict(area_mm2=sum(map(len, columns.values()))*.25**2,
                max_span_mm=max(((max(col)-min(col)+1)*.25 for col in columns.values()), default=0)) for s, columns in hits.items()}
        return result
    regions = {'body': body, 'hair': hair, **{'eye_'+s: ids for s, ids in eyes.items()},
               'body_L': body[neutral[body, 0] > .001], 'body_R': body[neutral[body, 0] < -.001]}
    for prefix in ['lid', 'brow', 'jaw', 'lip', 'cheek', 'nose', 'tongue']:
        regions[prefix] = np.array([v.index for v in obj.data.vertices if any(
            obj.vertex_groups[g.group].name.startswith('DEF-'+prefix) and g.weight > 1e-5 for g in v.groups)], dtype=int)
    def movement(points):
        delta = np.linalg.norm(points-neutral, axis=1)*1000
        return {name: dict(vertices=len(ids), max_mm=float(delta[ids].max()), changed=int(sum(delta[ids]>.001)))
                for name, ids in regions.items() if len(ids)}
    np.savez_compressed(out/'neutral.npz', points=neutral, **regions)
    (out/'topology.json').write_text(json.dumps(faces))
    (out/'render-settings.json').write_text(json.dumps(settings, indent=2))
    report = dict(family=args.family, source=str(args.source), sha256=stamp[1], armature=arm.name,
                  hair_components=hair_components, hair_vertices=len(hair), eye_components=eye_components,
                  probes={}, candidates={}, step_mm=.25, visibility_surface='bald retained full rig')
    probes = {**{f'{part}_lid_{s}': (f'lid.{code}.{s}.002', (0, 0, distance))
                   for s in ['L', 'R'] for part, code, distance in [('upper', 'T', -.003), ('lower', 'B', .002)]},
              **{f'gaze_{s}': (f'eye.{s}', (.01, 0, 0)) for s in ['L', 'R']},
              'brow': ('brow.T.L.002', (0, 0, .003)), 'jaw': ('jaw_master', (0, 0, -.004)),
              'lip': ('lip.T', (0, -.003, 0)), 'cheek': ('cheek.T.L.001', (0, -.003, 0)),
              'nose': ('nose.002', (0, -.003, 0)), 'tongue': ('tongue', (0, -.003, 0))}
    for name, (control, delta) in probes.items():
        reset()
        assert np.array_equal(sample(), neutral)
        if control not in arm.pose.bones:
            report['probes'][name] = dict(control=control, status='MISSING_CONTROL')
            continue
        move(control, delta)
        points = sample()
        report['probes'][name] = dict(control=control, delta_m=delta, finite=True, movement=movement(points))
        np.savez_compressed(out/('probe-'+name+'.npz'), points=points)
    gap = {s: float(arm.data.bones[f'lid.T.{s}.002'].head_local.z-arm.data.bones[f'lid.B.{s}.002'].head_local.z) for s in eyes}
    report['central_control_gap_mm'] = {s: v*1000 for s, v in gap.items()}
    for name, upper, lower in [('neutral', 0, 0), ('partial', .45, .1), ('strong', .9, .2)]:
        reset()
        assert np.array_equal(sample(), neutral)
        controls = {}
        for side in eyes:
            for part, coefficient in [('T', -upper), ('B', lower)]:
                control = f'lid.{part}.{side}.002'
                move(control, (0, 0, gap[side]*coefficient))
                controls[control] = gap[side]*coefficient*1000
        points = sample()
        if name != 'neutral':
            np.savez_compressed(out/(name+'.npz'), points=points)
        report['candidates'][name] = dict(controls_z_mm=controls, visibility=visibility(points), movement=movement(points))
    reset()
    assert np.array_equal(sample(), neutral)
    report['neutral_reset_exact'] = True
    report['invalid_drivers'] = [d.data_path for d in arm.animation_data.drivers if not d.driver.is_valid] if arm.animation_data else []
    assert stamp == (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    report['source_unchanged'] = True
    (out/'probe.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('CHILD STRUCTURAL PROBES COMPLETE', args.family, flush=True)


if __name__ == '__main__':
    main()
