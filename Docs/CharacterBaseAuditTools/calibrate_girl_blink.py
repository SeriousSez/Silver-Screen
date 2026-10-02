"""Girl-only bounded source-control blink calibration. Full depsgraph, no corrective.

Candidate JSON uses millimetres in Girl armature axes, per side and control.
All geometry bindings originate in the independently audited Girl source.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Quaternion, Vector
from mathutils.bvhtree import BVHTree

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from inspect_male_foundation import expose, components


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--candidates', type=Path, required=True)
    parser.add_argument('--step-mm', type=float, default=.25)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.family = 'Girl'
    out = args.output
    out.mkdir(parents=True, exist_ok=True)
    stamp = (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    assert stamp[1] == 'd088d51f5b6617ea29fb0145ade2af69934156d535872c43b55fe3011d4d45c8', 'Use the audited original Girl facial rig'
    bpy.ops.wm.open_mainfile(filepath=str(args.source), load_ui=False, use_scripts=False)
    expose()
    obj = bpy.data.objects['GirlBaseMesh_Facial_Rig']
    arm = obj.find_armature()
    groups = components(obj.data)
    expected = 17876
    assert len(obj.data.vertices) == expected
    hair_components = [1,6,7]
    eye_components = {'L': 4, 'R': 5}
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
    gap={side:abs(arm.data.bones[f'lid.T.{side}.002'].head_local.z-arm.data.bones[f'lid.B.{side}.002'].head_local.z) for side in eyes}
    assert all(abs(g-.03201711177825928)<1e-8 for g in gap.values())
    assert len(hair)==1466 and len(body)==12590
    center = neutral[np.concatenate(list(eyes.values()))].mean(0)
    settings = [dict(view='front', position=(center+np.array([0, -.8, 0])).tolist(), target=center.tolist(), ortho_scale=.19),
                dict(view='oblique', position=(center+np.array([.42, -.7, .04])).tolist(), target=center.tolist(), ortho_scale=.22)]
    grids = {}
    step = args.step_mm/1000
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
            result[name] = {s: dict(area_mm2=sum(map(len, columns.values()))*args.step_mm**2,
                max_span_mm=max(((max(col)-min(col)+1)*args.step_mm for col in columns.values()), default=0)) for s, columns in hits.items()}
        return result
    regions = {'body': body, 'hair': hair, **{'eye_'+s: ids for s, ids in eyes.items()},
               'body_L': body[neutral[body, 0] > .001], 'body_R': body[neutral[body, 0] < -.001]}
    for side,ids in eyes.items():
        window=np.all((neutral[body]>=neutral[ids].min(0)-gap[side]*.1)&(neutral[body]<=neutral[ids].max(0)+gap[side]*.1),axis=1)
        regions['eye_window_'+side]=body[window]
    for prefix in ['lid', 'brow', 'jaw', 'lip', 'cheek', 'nose', 'tongue']:
        regions[prefix] = np.array([v.index for v in obj.data.vertices if any(
            obj.vertex_groups[g.group].name.startswith('DEF-'+prefix) and g.weight > 1e-5 for g in v.groups)], dtype=int)
    def movement(points):
        delta = np.linalg.norm(points-neutral, axis=1)*1000
        return {name: dict(vertices=len(ids), max_mm=float(delta[ids].max()), changed=int(sum(delta[ids]>.001)))
                for name, ids in regions.items() if len(ids)}
    regions['outside_lid_weights'] = np.setdiff1d(body, regions['lid'])
    np.savez_compressed(out/'neutral.npz', points=neutral, **regions)
    (out/'topology.json').write_text(json.dumps(faces))
    (out/'render-settings.json').write_text(json.dumps(settings, indent=2))
    report = dict(family=args.family, source=str(args.source), sha256=stamp[1], armature=arm.name,
                  hair_components=hair_components, hair_vertices=len(hair), eye_components=eye_components,
                  candidates={}, central_control_gap_mm={s:g*1000 for s,g in gap.items()}, eye_window_margin='Own eye bounds expanded by 10% of own central gap', step_mm=args.step_mm, visibility_surface='bald retained full rig')
    report['control_heads_m'] = {b.name: list(b.head_local) for b in arm.data.bones if b.name.startswith(('lid.T.', 'lid.B.'))}
    for candidate in json.loads(args.candidates.read_text()):
        reset()
        assert np.array_equal(sample(), neutral)
        controls = candidate['controls_z_mm']
        for control, millimetres in controls.items():
            assert control.startswith(('lid.T.', 'lid.B.'))
            assert abs(millimetres) <= max(gap.values())*1000*1.1, 'Bounded Girl search only'
            move(control, (0, 0, millimetres/1000))
        rotations = candidate.get('controls_rotation_y_deg', {})
        for control, degrees in rotations.items():
            assert control.startswith(('lid.T.', 'lid.B.')) and abs(degrees) <= 15
            b = arm.pose.bones[control]
            # Rotate the meaningful user handle about source-armature Y. The rig's
            # existing B-bone handle mechanisms evaluate this; DEF bones are untouched.
            axis = b.bone.matrix_local.to_3x3().inverted() @ Vector((0, 1, 0))
            b.rotation_mode = 'QUATERNION'
            b.rotation_quaternion = Quaternion(axis.normalized(), math.radians(degrees))
        points = sample()
        name = candidate['name']
        if name != 'neutral': np.savez_compressed(out/(name+'.npz'), points=points)
        report['candidates'][name] = dict(controls_z_mm=controls, controls_rotation_y_deg=rotations, visibility=visibility(points), movement=movement(points),
            evaluated_controls_m={b.name:list(arm.matrix_world @ b.head) for b in arm.pose.bones if b.name.startswith(('lid.T.', 'lid.B.'))})
        (out/'calibration.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        print(name, json.dumps(report['candidates'][name]['visibility']), flush=True)
    reset()
    assert np.array_equal(sample(), neutral)
    report['neutral_reset_exact'] = True
    report['invalid_drivers'] = [d.data_path for d in arm.animation_data.drivers if not d.driver.is_valid] if arm.animation_data else []
    assert stamp == (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    report['source_unchanged'] = True
    (out/'calibration.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('GIRL CALIBRATION COMPLETE', flush=True)


if __name__ == '__main__':
    main()
