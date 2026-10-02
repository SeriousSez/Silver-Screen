"""Girl-only generated bald baseline and native-correspondence input, in Blender.

Requires the independently audited Girl lightweight FBX. Never saves the source.
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
    assert stamp[2]=='37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f'
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(a.source),use_anim=False)
    obj=bpy.data.objects['GirlBaseMesh_Shape_Keys'];arm=obj.find_armature();m=obj.data
    groups=components(m);assert len(groups)==45 and len(groups[0])==12590 and len(m.vertices)==17876
    hair_components=[1,6,7] # Girl: 1004-vertex cap and two 231-vertex buns.
    remove=set(i for g in hair_components for i in groups[g]);assert len(remove)==1466
    assert not any((e.vertices[0] in remove)!=(e.vertices[1] in remove) for e in m.edges)
    surface={v for face in m.polygons for v in face.vertices}
    loose=[v.index for v in m.vertices if v.index not in surface]
    assert len(loose)==4 and not set(loose).intersection(remove)
    assert len(arm.data.bones)==51
    # Keep all four audited non-surface vertices. They are not hairstyle geometry.
    keep=[i for i in range(len(m.vertices)) if i not in remove]
    points=np.array([obj.matrix_world@v.co for v in m.vertices])
    # Full original corner table preserves UV seams and per-polygon material identity.
    corners=[dict(vertex=m.loops[l].vertex_index,uv=list(m.uv_layers.active.data[l].uv),material=f.material_index)
             for f in m.polygons for l in f.loop_indices]
    shapes=[(k.name,np.array([v.co for v in k.data])[keep]) for k in m.shape_keys.key_blocks]
    weights=[[(g.group,g.weight) for g in m.vertices[i].groups] for i in keep]
    uvs={u.name:[list(u.data[l].uv) for f in m.polygons if f.vertices[0] not in remove for l in f.loop_indices] for u in m.uv_layers}
    skeleton=[(b.name,b.parent.name if b.parent else None,[list(r) for r in b.matrix_local]) for b in arm.data.bones]
    (a.output/'source-correspondence.json').write_text(json.dumps(dict(points=points.tolist(),corners=corners,hair=sorted(remove),keep=keep,loose=loose)),encoding='utf-8')
    bm=bmesh.new();bm.from_mesh(m);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.verts[i] for i in sorted(remove)],context='VERTS');bm.to_mesh(m);bm.free();m.update()
    assert all(name==k.name and np.array_equal(v,np.array([x.co for x in k.data])) for (name,v),k in zip(shapes,m.shape_keys.key_blocks))
    assert weights==[[(g.group,g.weight) for g in v.groups] for v in m.vertices]
    assert uvs=={u.name:[list(v.uv) for v in u.data] for u in m.uv_layers}
    assert skeleton==[(b.name,b.parent.name if b.parent else None,[list(r) for r in b.matrix_local]) for b in arm.data.bones]
    m.calc_loop_triangles()
    bpy.ops.wm.save_as_mainfile(filepath=str((a.output/'GirlBaldBaseline.blend').resolve()))
    report=dict(source=str(a.source),sha256=stamp[2],hair_components=hair_components,removed_vertices=len(remove),retained_vertices=len(keep),retained_triangles=len(m.loop_triangles),
                loose_source_vertices=loose,loose_policy='All four retained in Blender; independently check native representation',retained_components=len(components(m)),bones=len(arm.data.bones),shape_names=[n for n,_ in shapes],retained_shapes_uvs_weights_skeleton_exact=True)
    assert stamp==(a.source.stat().st_size,a.source.stat().st_mtime_ns,hashlib.sha256(a.source.read_bytes()).hexdigest())
    report['source_unchanged']=True
    (a.output/'bald-preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
