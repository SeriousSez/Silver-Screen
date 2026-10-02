"""Read-only connectivity, weights and packed-image supplement to inspect_sources.py."""
import argparse
from collections import Counter
import json
from pathlib import Path
import sys
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
reports = []
for path in args.source.rglob('*FacialRig.blend'):
    bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    record = {'path': path.relative_to(args.source).as_posix(), 'meshes': []}
    for obj in bpy.data.objects:
        if obj.type != 'MESH' or not any(m.type == 'ARMATURE' and m.object for m in obj.modifiers):
            continue
        mesh = obj.data
        parent = list(range(len(mesh.vertices)))
        def root(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        for edge in mesh.edges:
            a, b = edge.vertices
            parent[root(a)] = root(b)
        components = sorted(Counter(root(v.index) for v in mesh.vertices).values(), reverse=True)
        edge_faces = Counter()
        for p in mesh.polygons:
            for i, a in enumerate(p.vertices):
                b = p.vertices[(i + 1) % len(p.vertices)]
                edge_faces[tuple(sorted((a, b)))] += 1
        weights = Counter(sum(g.weight > .00001 for g in v.groups) for v in mesh.vertices)
        record['meshes'].append({'name': obj.name, 'components_vertices': components,
            'edge_face_counts': dict(Counter(edge_faces.values())), 'weights_per_vertex': dict(weights),
            'shape_vertex_counts': {k.name: sum((a.co-b.co).length > .000001 for a,b in zip(k.data, mesh.shape_keys.key_blocks[0].data))
                for k in mesh.shape_keys.key_blocks} if mesh.shape_keys else {}})
    reports.append(record)
args.output.write_text(json.dumps(reports, indent=2))
