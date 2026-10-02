"""Match selected Blender samples to retained native vertices and write additions.

Run in the repository after bake_male_blink.py and the Unity menu's
Export native reference. Requires numpy/scipy. Licensed arrays remain ignored.
The existing native mesh supplies every baseline value; only BlinkBoth is new.
"""
import argparse
import hashlib
import json
import struct
from pathlib import Path

import numpy as np
from scipy.spatial import cKDTree


def vectors(rows, axes='xyz'):
    return np.array([[v[k] for k in axes] for v in rows])


def unity(v):
    return v[:, [0, 2, 1]] * np.array([-1, 1, -1])


def rotate(vectors_to_rotate, source, target):
    """Shortest arc, preserving the native normal/tangent frame's small offset."""
    cross = np.cross(source, target)
    cosine = np.sum(source * target, axis=1)
    assert cosine.min() > -0.99999, 'Ambiguous opposite normal: inspect before proceeding.'
    out = vectors_to_rotate + np.cross(cross, vectors_to_rotate)
    out += np.cross(cross, np.cross(cross, vectors_to_rotate)) / (1 + cosine[:, None])
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, default=Path('TestResults/MaleFoundation/UnityBlink'))
    args = parser.parse_args()
    root = args.evidence
    sample = np.load(root.parent/'BlinkBlender/blender-samples.npz')
    native = json.loads((root/'native-baseline.json').read_text())
    manifest = json.loads((root.parent/'BlinkBlender/blender-export.json').read_text())
    assert manifest['selected_endpoint_exact'] and manifest['neutral_exact']
    world, uv = vectors(native['world']), vectors(native['uv'], 'xy')
    n0 = vectors(native['worldNormals'])
    matrix = np.array(native['matrix']).reshape(4, 4).T
    inv_linear = np.linalg.inv(matrix[:3, :3])
    normal_to_local = matrix[:3, :3].T
    source_ids = sample['loop_vertices']
    corners = unity(sample['p0'])[source_ids]
    tree = cKDTree(np.c_[corners, sample['uv']])
    _, loop_map = tree.query(np.c_[world, uv])
    vertex_map = source_ids[loop_map]
    position_error = np.linalg.norm(corners[loop_map] - world, axis=1)
    uv_error = np.linalg.norm(sample['uv'][loop_map] - uv, axis=1)
    # Male's native import differs from Blender's evaluated bind by 0.04027 mm.
    # Exact UV/corner and complete vertex coverage disambiguate the mapping;
    # preserve that native offset instead of moving the baseline to Blender.
    assert position_error.max() < 0.00005 and uv_error.max() < 0.000002
    assert len(np.unique(vertex_map)) == len(sample['p0']) == 15558
    source_normal0 = unity(sample['n0'])[loop_map]
    angles = np.rad2deg(np.arccos(np.clip(np.sum(n0*source_normal0, axis=1), -1, 1)))
    assert angles.max() < 0.1, 'Corner-normal mapping is not equivalent.'
    endpoint = unity(sample['p100'])[vertex_map]
    delta_world = unity(sample['p100'] - sample['p0'])[vertex_map]
    delta = delta_world @ inv_linear.T
    native_normals = vectors(native['normals'])
    native_tangents = vectors(native['tangents'])
    t0 = native_tangents @ matrix[:3, :3].T
    t0 /= np.linalg.norm(t0, axis=1)[:, None]
    frames = []
    normal_errors = []
    for percent in range(5, 101, 5):
        source_normal = unity(sample[f'n{percent}'])[loop_map]
        changed = np.linalg.norm(source_normal-source_normal0, axis=1) > 1e-10
        target_n = rotate(n0, source_normal0, source_normal)
        target_t = rotate(t0, source_normal0, source_normal)
        ln = target_n @ normal_to_local.T
        lt = target_t @ inv_linear.T
        ln /= np.linalg.norm(ln, axis=1)[:, None]
        lt /= np.linalg.norm(lt, axis=1)[:, None]
        # Avoid recasting untouched native normals/tangents, including the scalp.
        dn, dt = np.zeros_like(ln), np.zeros_like(lt)
        dn[changed] = ln[changed] - native_normals[changed]
        dt[changed] = lt[changed] - native_tangents[changed]
        frames.append((percent, delta*(percent/100), dn, dt))
        error = np.rad2deg(np.arccos(np.clip(np.sum(target_n*source_normal, axis=1), -1, 1)))
        normal_errors.append({'weight': percent, 'max_reference_angle_deg': float(error.max()),
                              'changed_normals': int(changed.sum()),
                              'tangent_normal_dot_max': float(np.abs(np.sum(target_n*target_t, axis=1)).max())})
    output = root/'blink-addition.bin'
    with output.open('wb') as stream:
        stream.write(b'SSBLINK1')
        stream.write(struct.pack('<ii', len(world), len(frames)))
        stream.write(vertex_map.astype('<i4').tobytes())
        # Independent absolute source samples, in Unity model-world metres.
        stream.write(unity(sample['p0'])[vertex_map].astype('<f4').tobytes())
        stream.write(endpoint.astype('<f4').tobytes())
        for percent, dv, dn, dt in frames:
            stream.write(struct.pack('<f', percent))
            for data in (dv, dn, dt, unity(sample[f'n{percent}'])[loop_map]):
                assert np.isfinite(data).all()
                stream.write(data.astype('<f4').tobytes())
    np.savez_compressed(root/'native-mapping.npz', vertex_map=vertex_map, loop_map=loop_map,
                        baseline_position_error=position_error, blink_delta_world=delta_world)
    report = {'candidate': manifest['candidate'], 'candidate_sha256': manifest['sha256'],
              'packet_sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
              'native_vertices': len(world), 'source_vertices': len(sample['p0']),
              'matched_source_vertices': len(np.unique(vertex_map)), 'uv_max_error': float(uv_error.max()),
              'neutral_position_max_error_mm': float(position_error.max()*1000),
              'neutral_normal_max_angle_deg': float(angles.max()),
              'blink_frames': [f[0] for f in frames], 'position_method': 'One linear selected endpoint delta at all frames.',
              'normal_method': 'Shortest-arc rotation from evaluated neutral to evaluated sampled Blender corner normal, applied to native normal and tangent. Untouched regions retain exact native values.',
              'tangent_limit': 'Transported native tangent, retaining its original normal relationship (including existing degenerate-UV islands); no new UV-parametric tangent solve or normal-map material certification.',
              'baseline_tangent_normal_dot_max': float(np.abs(np.sum(n0*t0, axis=1)).max()),
              'normal_samples': normal_errors,
              'max_delta_mm': float(np.linalg.norm(delta_world,axis=1).max()*1000),
              'outside_body_delta_mm': float(np.linalg.norm(delta_world[vertex_map>=12128],axis=1).max()*1000)}
    (root/'mapping.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8', newline='\n')
    print(json.dumps({k:v for k,v in report.items() if k!='normal_samples'}, indent=2))


if __name__ == '__main__':
    main()
