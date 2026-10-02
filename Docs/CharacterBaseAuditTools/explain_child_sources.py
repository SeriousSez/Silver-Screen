"""Explain cached source bounds, material data, proportions and export partitions.

Run in Blender with -- --pack ROOT --jobs jobs.json --output DIRECTORY.
Reads all four child Blender variants; saves JSON only, never source files.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from inspect_male_foundation import expose


def bounds(points):
    p = np.array(points)
    return dict(min=p.min(0).tolist(), max=p.max(0).tolist(), height=float(np.ptp(p[:, 2])))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--pack', type=Path, required=True)
    parser.add_argument('--jobs', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    report = []
    for job in json.loads(args.jobs.read_text()):
        if not job['variant'].endswith('Blend'):
            continue
        source = args.pack/job['source']
        stamp = (source.stat().st_mtime_ns, hashlib.sha256(source.read_bytes()).hexdigest())
        bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
        row = dict(**job, sha256=stamp[1], before_expose={}, after_expose={}, materials={})
        objects = [o for o in bpy.data.objects if o.type == 'MESH' and 'BaseMesh' in o.name]
        for o in objects:
            row['before_expose'][o.name] = dict(cached_bbox=bounds([o.matrix_world @ Vector(v) for v in o.bound_box]),
                actual_vertices=bounds([o.matrix_world @ v.co for v in o.data.vertices]),
                matrix=[list(r) for r in o.matrix_world], hidden=o.hide_get())
        expose()
        for o in objects:
            e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
            m = e.to_mesh()
            row['after_expose'][o.name] = dict(cached_bbox=bounds([o.matrix_world @ Vector(v) for v in o.bound_box]),
                evaluated_vertices=bounds([e.matrix_world @ v.co for v in m.vertices]))
            e.to_mesh_clear()
            for material in o.data.materials:
                if not material:
                    continue
                nodes = material.node_tree.nodes if material.node_tree else []
                row['materials'][material.name] = dict(diffuse=list(material.diffuse_color),
                    images=[n.image.filepath if n.image else None for n in nodes if n.type == 'TEX_IMAGE'],
                    node_types=[n.type for n in nodes])
        assert stamp == (source.stat().st_mtime_ns, hashlib.sha256(source.read_bytes()).hexdigest())
        report.append(row)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output/'source-details.json').write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
