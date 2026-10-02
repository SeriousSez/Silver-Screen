"""Transfer the reviewed Girl accepted source delta to its unchanged bald baseline.

Girl mapping is validated by complete retained polygon correspondence. The
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
from mathutils.bvhtree import BVHTree

def visibility(points, neutral, faces, eyes, settings, step=.0001):
    eye_sets={s:set(ids.tolist()) for s,ids in eyes.items()}
    eye_polys={i:s for s,ids in eye_sets.items() for i,f in enumerate(faces) if all(v in ids for v in f)}
    bvh=BVHTree.FromPolygons(points.tolist(),faces);result={}
    for setting in settings:
        origin=Vector(setting['position']);direction=(Vector(setting['target'])-origin).normalized()
        q=direction.to_track_quat('-Z','Y');right=q@Vector((1,0,0));up=q@Vector((0,1,0))
        p=neutral[np.concatenate(list(eyes.values()))]-np.array(origin)
        x=p@np.array(right);y=p@np.array(up)
        xs=np.arange(x.min()-.002,x.max()+.002,step);ys=np.arange(y.min()-.002,y.max()+.002,step)
        columns={s:{} for s in eyes};counts={s:0 for s in eyes}
        for xi,xx in enumerate(xs):
            for yi,yy in enumerate(ys):
                _,_,poly,_=bvh.ray_cast(origin+right*float(xx)+up*float(yy),direction,2)
                if poly in eye_polys:
                    s=eye_polys[poly];counts[s]+=1;columns[s].setdefault(xi,[]).append(yi)
        result[setting['view']]={s:dict(samples=counts[s],area_mm2=counts[s]*(step*1000)**2,
            max_span_mm=max(((max(v)-min(v)+1)*step*1000 for v in columns[s].values()),default=0)) for s in eyes}
    return result



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
    p.add_argument('--output',type=Path,required=True);p.add_argument('--acceptance',type=Path,required=True);p.add_argument('--source-samples',type=Path)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=args.output;out.mkdir(parents=True,exist_ok=True)
    baseline_stamp=hashlib.sha256(args.baseline.read_bytes()).hexdigest()
    source=args.source_evidence;n=np.load(source/'neutral.npz');neutral=n['points'];corrected=np.load(source/'ReviewG2.npz')['points']
    source_faces=json.loads((source/'topology.json').read_text());calibration=json.loads((source/'calibration.json').read_text())
    accepted=json.loads(args.acceptance.read_text())
    assert accepted['candidate']=='ReviewG2' and accepted['accepted'] and accepted['stage']=='canonical-reference/foundation' and not accepted['final_production_art']
    assert accepted['endpoint_sha256']==hashlib.sha256((source/'ReviewG2.npz').read_bytes()).hexdigest()
    # Retained source ordering is only a temporary graph index, never assumed to
    # correspond to the lightweight export. Geometry anchors and full graph
    # refinement independently resolve the map even across neutral/UV edits.
    hair=set(n['hair'].tolist());retained=np.array([i for i in range(len(neutral)) if i not in hair])
    source_inverse={int(v):i for i,v in enumerate(retained)}
    source_graph_faces=[[source_inverse[v] for v in f] for f in source_faces if all(v in source_inverse for v in f)]
    bpy.ops.wm.open_mainfile(filepath=calibration['source'],load_ui=False,use_scripts=False)
    from inspect_male_foundation import expose
    expose()
    full=bpy.data.objects['GirlBaseMesh_Facial_Rig']
    raw_source=np.array([full.matrix_world@v.co for v in full.data.vertices])
    source_corners=[(list(face.vertices),face.material_index,{full.data.loops[l].vertex_index:list(full.data.uv_layers.active.data[l].uv) for l in face.loop_indices}) for face in full.data.polygons if all(v in source_inverse for v in face.vertices)]
    bpy.ops.wm.open_mainfile(filepath=str(args.baseline.resolve()),load_ui=False,use_scripts=False)
    obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=obj.find_armature()
    assert len(obj.data.vertices)==len(retained)==16410 and len(arm.data.bones)==51
    faces=[list(p.vertices) for p in obj.data.polygons]
    world=np.array([obj.matrix_world@v.co for v in obj.data.vertices])
    from girl_topology_correspondence import correspondence
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
    frames={};sampled=np.load(args.source_samples) if args.source_samples else None;sample_parity={}
    for weight in range(0,101,5):
        if sampled is not None:
            sampled_delta=sampled[f'p{weight}'][mapping]-neutral[mapping]
            local_sample=sampled_delta@np.array(obj.matrix_world.to_3x3().inverted()).T
            for i,v in enumerate(key.data):v.co=basis.data[i].co+Vector(local_sample[i])
            key.value=1 if weight else 0
        else:key.value=weight/100
        pos,normals,loop,uv=evaluated();frames[f'p{weight}']=pos;frames[f'n{weight}']=normals
        if sampled is not None:
            sample_parity[str(weight)]=float(np.linalg.norm((pos-baseline)-sampled_delta,axis=1).max()*1000)
            assert sample_parity[str(weight)]<.001
        if weight==0:frames.update(loop_vertices=loop,uv=uv,world_matrix=np.array(obj.matrix_world),polygons=np.array([(p.loop_start,p.loop_total) for p in obj.data.polygons]))
    for i,v in enumerate(key.data):v.co=basis.data[i].co+Vector(local[i])
    key.value=0;restored=evaluated()[0]
    assert np.array_equal(baseline,restored) and before==fingerprint(obj)
    parity=float(np.linalg.norm((frames['p100']-baseline)-delta,axis=1).max()*1000)
    assert parity<.001 # Sub-micrometre displacement parity; no anatomy tolerance.
    eyes={side:np.array([inverse[int(i)] for i in n['eye_'+side]]) for side in ['L','R']}
    settings=json.loads((source/'render-settings.json').read_text())
    coverage={name:visibility(points,baseline,faces,eyes,settings) for name,points in [('neutral',baseline),('partial',frames['p50']),('blink',frames['p100'])]}
    interpolation={}
    if sampled is not None:
        for weight in np.arange(2.5,100,5):
            lower=int(weight-2.5);upper=lower+5
            expected=baseline+sampled[f'p{weight:g}'][mapping]-neutral[mapping]
            interpolation[str(weight)]=float(np.linalg.norm((frames[f'p{lower}']+frames[f'p{upper}'])/2-expected,axis=1).max()*1000)
    obj.name='GirlBaldBlinkBoth_Experimental'
    obj['BlinkBothPlayback']='Endpoint-only Blender key; runtime in-betweens are the evaluated blender-samples.npz frames' if sampled is not None else 'Linear endpoint experiment'
    bpy.ops.wm.save_as_mainfile(filepath=str((out/'GirlBaldBlinkBoth.blend').resolve()))
    np.savez_compressed(out/'blender-samples.npz',**frames)
    np.savez_compressed(out/'neutral.npz',points=baseline,hair=np.array([],dtype=int),body=np.array([inverse[int(i)] for i in n['body']]),eye_L=eyes['L'],eye_R=eyes['R'])
    np.savez_compressed(out/'blink.npz',points=frames['p100'])
    np.savez_compressed(out/'partial.npz',points=frames['p50'])
    (out/'topology.json').write_text(json.dumps(faces));(out/'render-settings.json').write_text(json.dumps(settings,indent=2))
    np.savez_compressed(out/'source-mapping.npz',source_vertices=mapping,delta=delta)
    (out/'regions.json').write_text(json.dumps({k:[inverse[int(i)] for i in n[k] if int(i) in inverse] for k in ['body','eye_L','eye_R','lid','outside_lid_weights']}))
    report=dict(candidate=str((out/'GirlBaldBlinkBoth.blend').resolve()),sha256=hashlib.sha256((out/'GirlBaldBlinkBoth.blend').read_bytes()).hexdigest(),
        source_candidate='ReviewG2 (user accepted for canonical-reference/foundation, not final production art)',source_mapping='Independent Girl geometry anchors plus uniquely resolved graph refinement and complete polygon/material equality',source_uv_max_difference=uv_error,source_uvs_transferred=False,selected_endpoint_exact=True,neutral_exact=True,
        preserved=before,parity_max_mm=parity,source_neutral_offset_max_mm=float(np.linalg.norm(baseline-neutral[mapping],axis=1).max()*1000),
        sampled_source_trajectory=sampled is not None,sample_parity_mm=sample_parity,between_frame_source_error_mm=interpolation,blender_file_playback='Endpoint key only; sampled runtime trajectory is in blender-samples.npz',
        bones=len(arm.data.bones),vertices=len(mapping),existing_keys_exact=29,total_shapes=30,coverage=coverage,
        baseline_file_unchanged=hashlib.sha256(args.baseline.read_bytes()).hexdigest()==baseline_stamp)
    assert report['baseline_file_unchanged']
    (out/'blender-export.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k!='preserved'},indent=2))


if __name__=='__main__':main()
