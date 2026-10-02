"""Generate a Male-only bald comparison, after inspecting its complete scalp.

Recomputes hair components from Male source. No purchased input is saved.
The FBX is correspondence evidence; Unity retains/subsets native import data.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import bpy
import bmesh
import numpy as np
from mathutils.kdtree import KDTree

sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from inspect_male_foundation import components


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--baseline',type=Path,required=True)
    p.add_argument('--inspection',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    p.add_argument('--fbx',type=Path,required=True)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.output.mkdir(parents=True,exist_ok=True);args.fbx.parent.mkdir(parents=True,exist_ok=True)
    stamp=(args.baseline.stat().st_mtime_ns,hashlib.sha256(args.baseline.read_bytes()).hexdigest())
    source=np.load(args.inspection/'Character1.npz')
    hair=np.concatenate([source[k] for k in source.files if k.startswith('component_') and k!='component_0'])
    tree=KDTree(len(hair))
    for i,j in enumerate(hair):tree.insert(source['points'][j],i)
    tree.balance()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.baseline),use_anim=False)
    obj=next(o for o in bpy.data.objects if o.type=='MESH')
    arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    points=np.array([obj.matrix_world@v.co for v in obj.data.vertices])
    remove=set(i for i,v in enumerate(points) if tree.find(v)[2]<.000002)
    assert len(remove)==len(hair)==2558,'Male hair correspondence changed; reinspect'
    assert not any((a in remove)!=(b in remove) for a,b in [e.vertices for e in obj.data.edges]),'Hair bridges body'
    keep=[i for i in range(len(points)) if i not in remove]
    mesh=obj.data
    old_shapes=[np.array([v.co for v in k.data])[keep] for k in mesh.shape_keys.key_blocks]
    old_weights=[[(g.group,g.weight) for g in mesh.vertices[i].groups] for i in keep]
    old_uvs={u.name:[list(u.data[l].uv) for p in mesh.polygons if p.vertices[0] not in remove for l in p.loop_indices] for u in mesh.uv_layers}
    skeleton=[(b.name,b.parent.name if b.parent else None,[list(row) for row in b.matrix_local]) for b in arm.data.bones]
    bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.verts[i] for i in sorted(remove)],context='VERTS')
    bm.to_mesh(mesh);bm.free();mesh.update()
    assert all(np.array_equal(old,np.array([v.co for v in k.data])) for old,k in zip(old_shapes,mesh.shape_keys.key_blocks))
    assert old_weights==[[(g.group,g.weight) for g in v.groups] for v in mesh.vertices]
    assert old_uvs=={u.name:[list(v.uv) for v in u.data] for u in mesh.uv_layers}
    assert skeleton==[(b.name,b.parent.name if b.parent else None,[list(row) for row in b.matrix_local]) for b in arm.data.bones]
    mesh.calc_loop_triangles()
    report=dict(source=str(args.baseline),sha256=stamp[1],removed_source_indices=sorted(remove),
                retained_source_indices=keep,removed_vertices=len(remove),retained_vertices=len(mesh.vertices),
                retained_triangles=len(mesh.loop_triangles),retained_components=len(components(mesh)),
                bones=len(arm.data.bones),shapes=[k.name for k in mesh.shape_keys.key_blocks],
                retained_shapes_exact=True,retained_uvs_exact=True,retained_weights_exact=True,skeleton_exact=True)
    bpy.ops.wm.save_as_mainfile(filepath=str((args.output/'MaleBaldBaseline.blend').resolve()))
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=str(args.fbx.resolve()),use_selection=True,object_types={'ARMATURE','MESH'},
        use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
    assert stamp==(args.baseline.stat().st_mtime_ns,hashlib.sha256(args.baseline.read_bytes()).hexdigest())
    report['source_unchanged']=True
    (args.output/'bald-preservation.json').write_text(json.dumps(report,indent=2))
    print('MALE BALD PRESERVATION PASSED',report['retained_vertices'],report['retained_triangles'],flush=True)


if __name__=='__main__':main()
