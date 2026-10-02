"""Export evaluated ReviewCandidate90 samples, without saving or reauthoring it.

Run with Blender --background --factory-startup --disable-autoexec --python
this_file -- --candidate <ReviewCandidate90/FemaleBaldBlinkCorrective.blend>
--output <ignored evidence directory>. No FBX round trip is used.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np

APPROVED_SHA256 = '559f941d99b21848e680085f941b621727fa292bd248dfe9c98f58468ba8734b'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    before = (args.candidate.stat().st_mtime_ns, hashlib.sha256(args.candidate.read_bytes()).hexdigest())
    assert before[1] == APPROVED_SHA256, 'This milestone accepts only the user-reviewed ReviewCandidate90 file.'
    bpy.ops.wm.open_mainfile(filepath=str(args.candidate.resolve()))
    obj = bpy.data.objects['FemaleBaldBlinkCorrective_Experimental']
    keys = obj.data.shape_keys.key_blocks
    assert len(obj.data.vertices) == 16497 and len(keys) == 31
    assert len(obj.find_armature().data.bones) == 51
    assert all(k.value == 0 for k in list(keys)[1:])
    args.output.mkdir(parents=True, exist_ok=True)
    frames = {}
    world = np.array(obj.matrix_world)
    normal_matrix = np.linalg.inv(world[:3, :3]).T
    for percent in range(0, 101, 5):
        keys['BlinkBoth'].value = percent / 100
        bpy.context.view_layer.update()
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=bpy.context.evaluated_depsgraph_get())
        try:
            # Same float matrix evaluation as the approved evidence, for exact identity.
            pos = np.array([evaluated.matrix_world @ v.co for v in mesh.vertices])
            normals = np.array([n.vector[:] for n in mesh.corner_normals]) @ normal_matrix.T
            normals /= np.linalg.norm(normals, axis=1)[:, None]
            frames[f'p{percent}'] = pos
            frames[f'n{percent}'] = normals
            if percent == 0:
                frames['loop_vertices'] = np.array([v.vertex_index for v in mesh.loops], dtype=np.int32)
                frames['uv'] = np.array([uv.uv[:] for uv in mesh.uv_layers.active.data])
                frames['polygons'] = np.array([(p.loop_start, p.loop_total) for p in mesh.polygons], dtype=np.int32)
                frames['world_matrix'] = world
        finally:
            evaluated.to_mesh_clear()
    keys['BlinkBoth'].value = 0
    bpy.context.view_layer.update()
    # Verify this is the exact approved saved candidate, not a similarly named alternative.
    prior = args.candidate.parent
    assert np.array_equal(frames['p0'], np.load(prior/'neutral.npz')['points'])
    assert np.array_equal(frames['p100'], np.load(prior/'corrected.npz')['points'])
    assert (args.candidate.stat().st_mtime_ns, hashlib.sha256(args.candidate.read_bytes()).hexdigest()) == before
    np.savez_compressed(args.output/'blender-samples.npz', **frames)
    report = {'candidate': str(args.candidate.resolve()), 'sha256': before[1], 'source_unchanged': True,
              'approved_endpoint_exact': True, 'neutral_exact': True,
              'object': obj.name, 'vertices': len(frames['p0']), 'loops': len(frames['loop_vertices']),
              'weights': list(range(0, 101, 5)), 'bone_count': 51,
              'normal_method': 'Evaluated Blender corner normals at each weight; no shape coordinates edited. Native Unity tangents are transported with the changing surface normal.'}
    (args.output/'blender-export.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8', newline='\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
