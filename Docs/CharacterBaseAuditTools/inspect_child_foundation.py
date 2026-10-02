"""Read-only, per-variant child geometry and hierarchy evidence (Blender).

Shares connectivity/metadata algorithms, never adult indices or rig bindings.
Run -- --source FILE --output DIRECTORY. Purchased files are never saved.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from inspect_male_foundation import components, describe, expose


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    out = args.output
    out.mkdir(parents=True, exist_ok=True)
    stamp = (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    if args.source.suffix.lower() == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(args.source), load_ui=False, use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(args.source), use_anim=False)
    expose()
    report = dict(source=str(args.source), sha256=stamp[1], units=bpy.context.scene.unit_settings.system,
                  unit_scale=bpy.context.scene.unit_settings.scale_length, meshes={}, rigs={})
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and 'BaseMesh' in obj.name:
            data = describe(obj, out)
            data['matrix_world'] = [list(row) for row in obj.matrix_world]
            data['modifiers'] = [dict(type=m.type, name=m.name, enabled=m.show_viewport,
                                     armature=m.object.name if m.type == 'ARMATURE' and m.object else None)
                                 for m in obj.modifiers]
            evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh = evaluated.to_mesh()
            points = np.array([evaluated.matrix_world @ v.co for v in mesh.vertices])
            np.savez_compressed(out/(obj.name+'-evaluated.npz'), points=points)
            data['evaluated_bounds'] = [points.min(0).tolist(), points.max(0).tolist()]
            data['evaluated_finite'] = bool(np.isfinite(points).all())
            evaluated.to_mesh_clear()
            for ids, row in zip(components(obj.data), data['components']):
                members = set(ids)
                row['materials'] = dict(Counter(p.material_index for p in obj.data.polygons if p.vertices[0] in members))
                sums = Counter()
                for i in ids:
                    for g in obj.data.vertices[i].groups:
                        sums[obj.vertex_groups[g.group].name] += g.weight
                row['weight_sums'] = dict(sums.most_common())
            report['meshes'][obj.name] = data
        elif obj.type == 'ARMATURE':
            rows = []
            for b in obj.data.bones:
                p = obj.pose.bones[b.name]
                rows.append(dict(name=b.name, parent=b.parent.name if b.parent else None,
                                 head=list(obj.matrix_world @ b.head_local), tail=list(obj.matrix_world @ b.tail_local),
                                 deform=b.use_deform, connected=b.use_connect,
                                 constraints=[dict(type=c.type, name=c.name) for c in p.constraints],
                                 lock_location=list(p.lock_location)))
            report['rigs'][obj.name] = dict(bones=len(rows), deform=sum(b['deform'] for b in rows),
                                           roots=[b['name'] for b in rows if not b['parent']], controls=rows,
                                           matrix_world=[list(row) for row in obj.matrix_world])
    assert stamp == (args.source.stat().st_mtime_ns, hashlib.sha256(args.source.read_bytes()).hexdigest())
    report['source_unchanged'] = True
    (out/'inspection.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('CHILD INSPECTION COMPLETE', out, flush=True)


if __name__ == '__main__':
    main()
