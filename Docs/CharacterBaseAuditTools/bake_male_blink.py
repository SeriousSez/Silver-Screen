"""Transfer the reviewed Male source/corner delta to its unchanged bald baseline.

Male mapping is validated by complete retained polygon correspondence. The
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
    source=args.source_evidence;n=np.load(source/'neutral.npz');neutral=n['points'];corrected=np.load(source/'cornerRelax10.npz')['points']
    source_faces=json.loads((source/'topology.json').read_text());calibration=json.loads((source/'calibration.json').read_text())
    corners=json.loads((source/'corner-validation.json').read_text())
    # The reviewed bald endpoint reads closed, with a disclosed sub-millimetre
    # outer-corner slit in the oblique view. These bounds detect evidence drift;
    # they are not a substitute for the recorded close-up visual review.
    coverage_check=corners['candidates']['cornerRelax10']['visibility']
    assert all(v['samples']==0 for v in coverage_check['front'].values())
    assert max(v['area_mm2'] for v in coverage_check['oblique'].values())<.8
    assert max(v['max_span_mm'] for v in coverage_check['oblique'].values())<.7
    # These Male block extents were discovered from Male component geometry and
    # separate source objects. Complete polygon equality below guards the map.
    blocks=[('Character1',12128),('GumsLower_lowres',1224),('GumsLower_lowres.002',216),
            ('GumsUpper_lowres',1412),('Character1.002',289),('Character1.004',289)]
    mapping=np.concatenate([np.arange(calibration['objects'][name],calibration['objects'][name]+count) for name,count in blocks])
    inverse={int(v):i for i,v in enumerate(mapping)}
    mapped_faces=[[inverse[v] for v in f] for f in source_faces if all(v in inverse for v in f)]
    bpy.ops.wm.open_mainfile(filepath=str(args.baseline.resolve()),load_ui=False,use_scripts=False)
    obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=obj.find_armature()
    assert len(obj.data.vertices)==len(mapping)==15558
    faces=[list(p.vertices) for p in obj.data.polygons]
    assert {tuple(sorted(f)) for f in faces}=={tuple(sorted(f)) for f in mapped_faces},'Male topology correspondence failed'
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
    obj.name='MaleBaldBlinkBoth_Experimental'
    bpy.ops.wm.save_as_mainfile(filepath=str((out/'MaleBaldBlinkBoth.blend').resolve()))
    np.savez_compressed(out/'blender-samples.npz',**frames)
    np.savez_compressed(out/'neutral.npz',points=baseline,hair=np.array([],dtype=int))
    np.savez_compressed(out/'blink.npz',points=frames['p100'])
    (out/'topology.json').write_text(json.dumps(faces));(out/'render-settings.json').write_text(json.dumps(settings,indent=2))
    np.savez_compressed(out/'source-mapping.npz',source_vertices=mapping,delta=delta)
    report=dict(candidate=str((out/'MaleBaldBlinkBoth.blend').resolve()),sha256=hashlib.sha256((out/'MaleBaldBlinkBoth.blend').read_bytes()).hexdigest(),
        source_candidate='u100_adj10 + cornerRelax10',source_mapping_blocks=blocks,selected_endpoint_exact=True,neutral_exact=True,
        preserved=before,parity_max_mm=parity,source_neutral_offset_max_mm=float(np.linalg.norm(baseline-neutral[mapping],axis=1).max()*1000),
        bones=len(arm.data.bones),vertices=len(mapping),existing_keys_exact=29,total_shapes=30,coverage=coverage,
        baseline_file_unchanged=hashlib.sha256(args.baseline.read_bytes()).hexdigest()==baseline_stamp)
    assert report['baseline_file_unchanged']
    (out/'blender-export.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k!='preserved'},indent=2))


if __name__=='__main__':main()
