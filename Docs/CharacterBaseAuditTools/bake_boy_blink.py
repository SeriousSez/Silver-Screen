"""Transfer the reviewed Boy accepted source delta to its unchanged bald baseline.

Boy mapping is validated by complete retained polygon correspondence. The
baseline, 29 existing keys, UVs, normals, weights and body rig are fingerprinted.
Only one experimental BlinkBoth is added. No Unity import occurs here.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Vector

sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_male_blink import visibility


def fingerprint(obj):
    m=obj.data
    data=dict(vertices=[list(v.co) for v in m.vertices],faces=[list(p.vertices) for p in m.polygons],
        uvs={u.name:[list(v.uv) for v in u.data] for u in m.uv_layers},normals=[list(v.vector) for v in m.corner_normals],
        weights=[[(g.group,g.weight) for g in v.groups] for v in m.vertices],
        shapes=[dict(name=k.name,min=k.slider_min,max=k.slider_max,value=k.value,relative=k.relative_key.name,
                     points=[list(v.co) for v in k.data]) for k in list(m.shape_keys.key_blocks)[:30]],
        bones=[dict(name=b.name,parent=b.parent.name if b.parent else None,matrix=[list(r) for r in b.matrix_local],
                    pose=[list(r) for r in obj.find_armature().pose.bones[b.name].matrix_basis]) for b in obj.find_armature().data.bones])
    return {k:hashlib.sha256(json.dumps(v,separators=(',',':')).encode()).hexdigest() for k,v in data.items()}


def main():
    p=argparse.ArgumentParser();p.add_argument('--baseline',type=Path,required=True);p.add_argument('--source-evidence',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=args.output;out.mkdir(parents=True,exist_ok=True)
    baseline_stamp=hashlib.sha256(args.baseline.read_bytes()).hexdigest()
    source=args.source_evidence;n=np.load(source/'neutral.npz');neutral=n['points'];corrected=np.load(source/'ReviewR3.npz')['points']
    source_faces=json.loads((source/'topology.json').read_text());calibration=json.loads((source/'calibration.json').read_text())
    accepted=json.loads((source/'user-acceptance.json').read_text())
    assert accepted['candidate']=='ReviewR3' and accepted['answer']=='Accept ReviewR3 and continue'
    assert accepted['endpoint_sha256']==hashlib.sha256((source/'ReviewR3.npz').read_bytes()).hexdigest()
    # Retained source ordering is only a temporary graph index, never assumed to
    # correspond to the lightweight export. Geometry anchors and full graph
    # refinement independently resolve the map even across neutral/UV edits.
    hair=set(n['hair'].tolist());retained=np.array([i for i in range(len(neutral)) if i not in hair])
    source_inverse={int(v):i for i,v in enumerate(retained)}
    source_graph_faces=[[source_inverse[v] for v in f] for f in source_faces if all(v in source_inverse for v in f)]
    bpy.ops.wm.open_mainfile(filepath=calibration['source'],load_ui=False,use_scripts=False)
    from inspect_male_foundation import expose
    expose()
    full=bpy.data.objects['BoyBaseMesh_FaceRig']
    raw_source=np.array([full.matrix_world@v.co for v in full.data.vertices])
    source_corners=[(list(face.vertices),face.material_index,{full.data.loops[l].vertex_index:list(full.data.uv_layers.active.data[l].uv) for l in face.loop_indices}) for face in full.data.polygons if all(v in source_inverse for v in face.vertices)]
    bpy.ops.wm.open_mainfile(filepath=str(args.baseline.resolve()),load_ui=False,use_scripts=False)
    obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=obj.find_armature()
    assert len(obj.data.vertices)==len(retained)==15994 and len(arm.data.bones)==50
    faces=[list(p.vertices) for p in obj.data.polygons]
    world=np.array([obj.matrix_world@v.co for v in obj.data.vertices])
    from boy_topology_correspondence import correspondence
    local_map,mapping_proof=correspondence(raw_source[retained],source_graph_faces,world,faces)
    mapping=retained[local_map];inverse={int(v):i for i,v in enumerate(mapping)}
    mapped_faces=[[inverse[v] for v in f] for f in source_faces if all(v in inverse for v in f)]
    assert {tuple(sorted(f)) for f in faces}=={tuple(sorted(f)) for f in mapped_faces}
    source_surface={tuple(sorted(inverse[v] for v in f)):(mat,{inverse[v]:uv for v,uv in uvs.items()}) for f,mat,uvs in source_corners}
    vertex_uv_error=np.zeros(len(mapping))
    for face in obj.data.polygons:
        mat,uvs=source_surface[tuple(sorted(face.vertices))];assert face.material_index==mat
        for l in face.loop_indices:
            v=obj.data.loops[l].vertex_index
            vertex_uv_error[v]=max(vertex_uv_error[v],float(np.linalg.norm(np.array(obj.data.uv_layers.active.data[l].uv)-uvs[v])))
    uv_error=float(vertex_uv_error.max())
    mapping_proof.update(complete_polygon_sets_equal=True,material_assignments_equal=True,uv_max_difference=uv_error,
        source_uvs_transferred=False,neutral_position_max_difference_mm=float(np.linalg.norm(world-raw_source[mapping],axis=1).max()*1000))
    (out/'source-correspondence-proof.json').write_text(json.dumps(mapping_proof,indent=2))
    before=fingerprint(obj)
    def evaluated():
        bpy.context.view_layer.update();e=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh(preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
        pos=np.array([e.matrix_world@v.co for v in m.vertices]);normals=np.array([v.vector for v in m.corner_normals])
        nm=np.linalg.inv(np.array(e.matrix_world)[:3,:3]).T;normals=normals@nm.T;normals/=np.linalg.norm(normals,axis=1)[:,None]
        loop=np.array([v.vertex_index for v in m.loops]);uv=np.array([v.uv for v in m.uv_layers.active.data])
        e.to_mesh_clear();return pos,normals,loop,uv
    baseline=evaluated()[0]
    delta=corrected[mapping]-neutral[mapping]
    key=obj.shape_key_add(name='BlinkBoth',from_mix=False);basis=obj.data.shape_keys.key_blocks[0]
    local=delta@np.array(obj.matrix_world.to_3x3().inverted()).T
    for i,v in enumerate(key.data):v.co=basis.data[i].co+Vector(local[i])
    key.value=0;assert before==fingerprint(obj)
    frames={}
    for weight in range(0,101,5):
        key.value=weight/100;pos,normals,loop,uv=evaluated();frames[f'p{weight}']=pos;frames[f'n{weight}']=normals
        if weight==0:frames.update(loop_vertices=loop,uv=uv,world_matrix=np.array(obj.matrix_world),polygons=np.array([(p.loop_start,p.loop_total) for p in obj.data.polygons]))
    key.value=0;restored=evaluated()[0]
    assert np.array_equal(baseline,restored) and before==fingerprint(obj)
    parity=float(np.linalg.norm((frames['p100']-baseline)-delta,axis=1).max()*1000)
    assert parity<.002
    eyes={side:np.array([inverse[int(i)] for i in n['eye_'+side]]) for side in ['L','R']}
    settings=json.loads((source/'render-settings.json').read_text())
    coverage={name:visibility(points,baseline,faces,eyes,settings) for name,points in [('neutral',baseline),('blink',frames['p100'])]}
    obj.name='BoyBaldBlinkBoth_Experimental'
    bpy.ops.wm.save_as_mainfile(filepath=str((out/'BoyBaldBlinkBoth.blend').resolve()))
    np.savez_compressed(out/'blender-samples.npz',**frames)
    np.savez_compressed(out/'neutral.npz',points=baseline,hair=np.array([],dtype=int),body=np.array([inverse[int(i)] for i in n['body']]),eye_L=eyes['L'],eye_R=eyes['R'])
    np.savez_compressed(out/'blink.npz',points=frames['p100'])
    np.savez_compressed(out/'partial.npz',points=frames['p50'])
    (out/'topology.json').write_text(json.dumps(faces));(out/'render-settings.json').write_text(json.dumps(settings,indent=2))
    np.savez_compressed(out/'source-mapping.npz',source_vertices=mapping,delta=delta)
    report=dict(candidate=str((out/'BoyBaldBlinkBoth.blend').resolve()),sha256=hashlib.sha256((out/'BoyBaldBlinkBoth.blend').read_bytes()).hexdigest(),
        source_candidate='ReviewR3 (user accepted)',source_mapping='Independent Boy geometry anchors plus uniquely resolved graph refinement and complete polygon/material equality',source_uv_max_difference=uv_error,source_uvs_transferred=False,selected_endpoint_exact=True,neutral_exact=True,
        preserved=before,parity_max_mm=parity,source_neutral_offset_max_mm=float(np.linalg.norm(baseline-neutral[mapping],axis=1).max()*1000),
        bones=len(arm.data.bones),vertices=len(mapping),existing_keys_exact=29,total_shapes=30,coverage=coverage,
        baseline_file_unchanged=hashlib.sha256(args.baseline.read_bytes()).hexdigest()==baseline_stamp)
    assert report['baseline_file_unchanged']
    (out/'blender-export.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k!='preserved'},indent=2))


if __name__=='__main__':main()
