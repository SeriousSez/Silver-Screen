"""Boy-only generated bald baseline and native-correspondence input, in Blender.

Requires the independently audited Boy lightweight FBX. Never saves the source.
Native Unity buffers remain authoritative; this does not round-trip their normals.
"""
import argparse, hashlib, json, sys
from pathlib import Path
import bpy, bmesh
import numpy as np
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from inspect_male_foundation import components


def main():
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True)
    stamp=(a.source.stat().st_size,a.source.stat().st_mtime_ns,hashlib.sha256(a.source.read_bytes()).hexdigest())
    assert stamp[2]=='eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df'
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(a.source),use_anim=False)
    obj=bpy.data.objects['BoyBaseMesh_ShapeKeys'];arm=obj.find_armature();m=obj.data
    groups=components(m);assert len(groups)==51 and len(groups[0])==12178 and len(m.vertices)==17468
    hair_components=[5,*range(7,19)]
    remove=set(i for g in hair_components for i in groups[g]);assert len(remove)==1474
    assert not any((e.vertices[0] in remove)!=(e.vertices[1] in remove) for e in m.edges)
    keep=[i for i in range(len(m.vertices)) if i not in remove]
    points=np.array([obj.matrix_world@v.co for v in m.vertices])
    # Full original corner table preserves UV seams and per-polygon material identity.
    corners=[dict(vertex=m.loops[l].vertex_index,uv=list(m.uv_layers.active.data[l].uv),material=f.material_index)
             for f in m.polygons for l in f.loop_indices]
    shapes=[(k.name,np.array([v.co for v in k.data])[keep]) for k in m.shape_keys.key_blocks]
    weights=[[(g.group,g.weight) for g in m.vertices[i].groups] for i in keep]
    uvs={u.name:[list(u.data[l].uv) for f in m.polygons if f.vertices[0] not in remove for l in f.loop_indices] for u in m.uv_layers}
    skeleton=[(b.name,b.parent.name if b.parent else None,[list(r) for r in b.matrix_local]) for b in arm.data.bones]
    (a.output/'source-correspondence.json').write_text(json.dumps(dict(points=points.tolist(),corners=corners,hair=sorted(remove),keep=keep)),encoding='utf-8')
    bm=bmesh.new();bm.from_mesh(m);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.verts[i] for i in sorted(remove)],context='VERTS');bm.to_mesh(m);bm.free();m.update()
    assert all(name==k.name and np.array_equal(v,np.array([x.co for x in k.data])) for (name,v),k in zip(shapes,m.shape_keys.key_blocks))
    assert weights==[[(g.group,g.weight) for g in v.groups] for v in m.vertices]
    assert uvs=={u.name:[list(v.uv) for v in u.data] for u in m.uv_layers}
    assert skeleton==[(b.name,b.parent.name if b.parent else None,[list(r) for r in b.matrix_local]) for b in arm.data.bones]
    m.calc_loop_triangles()
    bpy.ops.wm.save_as_mainfile(filepath=str((a.output/'BoyBaldBaseline.blend').resolve()))
    report=dict(source=str(a.source),sha256=stamp[2],hair_components=hair_components,removed_vertices=len(remove),retained_vertices=len(keep),retained_triangles=len(m.loop_triangles),
                retained_components=len(components(m)),bones=len(arm.data.bones),shape_names=[n for n,_ in shapes],retained_shapes_uvs_weights_skeleton_exact=True)
    assert stamp==(a.source.stat().st_size,a.source.stat().st_mtime_ns,hashlib.sha256(a.source.read_bytes()).hexdigest())
    report['source_unchanged']=True
    (a.output/'bald-preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
