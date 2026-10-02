"""Generate an isolated 95-bone hybrid FBX from copied data blocks; never save sources.

Requires probe_female_poses.py evidence. Retains the 29 supplied shapes, uses only
the 42 empirically active weighted facial bones plus two rigid eye transforms.
Pose keys contain evaluated transforms, not Blender constraints or mechanisms.
"""
import argparse,json,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Matrix

parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True);parser.add_argument('--evidence',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);args.output.parent.mkdir(parents=True,exist_ok=True)
source=next(p for p in args.source.rglob('*FacialRig.blend') if 'female' in str(p).lower())
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False)
for c in bpy.data.collections:c.hide_viewport=False
def expose(l):
    l.exclude=False;l.hide_viewport=False
    for child in l.children:expose(child)
expose(bpy.context.view_layer.layer_collection)
for o in bpy.data.objects:o.hide_viewport=False;o.hide_set(False)
bpy.context.view_layer.update()
original_arm=bpy.data.objects['Armature_Character_shape_keys.001']
original_mesh=bpy.data.objects['Character_shape_keys.002']
face_arm=bpy.data.objects['Armature_Character1.001'];face_body=bpy.data.objects['character2.003']
probe=json.loads((args.evidence/'source-pose-probes.json').read_text())
assert probe['topology_correspondence']['matched']==17638
face_names=probe['candidate_changed_weighted_bones']
neutral=np.load(args.evidence/'Neutral.npz')
arm=original_arm.copy();arm.data=original_arm.data.copy();arm.name='FemaleHybridRig';bpy.context.scene.collection.objects.link(arm)
arm.animation_data_clear()
for pb in arm.pose.bones:
    for c in list(pb.constraints):pb.constraints.remove(c)
mesh=original_mesh.copy();mesh.data=original_mesh.data.copy();mesh.name='FemaleHybrid';bpy.context.scene.collection.objects.link(mesh)
mesh.parent=arm
for mod in mesh.modifiers:
    if mod.type=='ARMATURE':mod.object=arm
keep={b.name for b in arm.data.bones if b.use_deform}|{'ROOT'}
for obj in bpy.context.selected_objects:obj.select_set(False)
arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='EDIT')
for b in list(arm.data.edit_bones):
    if b.name not in keep:arm.data.edit_bones.remove(b)
world_inverse=arm.matrix_world.inverted()
for name in face_names+['SS_Eye_L','SS_Eye_R']:
    src='ORG-eye.'+name[-1] if name.startswith('SS_Eye_') else name
    b=arm.data.edit_bones.new(name)
    # Use a rigid neutral frame; all evaluated pose scale is relative to this bind.
    matrix=world_inverse @ Matrix(neutral[src].tolist())
    position,rotation,_=matrix.decompose()
    b.matrix=Matrix.LocRotScale(position,rotation,None);b.length=.02;b.use_deform=True
    b.parent=arm.data.edit_bones['Head']
bpy.ops.object.mode_set(mode='OBJECT')
for pb in arm.pose.bones:
    for c in list(pb.constraints):pb.constraints.remove(c)
    pb.matrix_basis=Matrix.Identity(4)
for group in list(mesh.vertex_groups):
    if group.name not in keep:mesh.vertex_groups.remove(group)
for name in face_names+['SS_Eye_L','SS_Eye_R']:mesh.vertex_groups.new(name=name)
for vertex in face_body.data.vertices:
    weights={face_body.vertex_groups[g.group].name:g.weight for g in vertex.groups
        if face_body.vertex_groups[g.group].name in face_arm.data.bones and face_arm.data.bones[face_body.vertex_groups[g.group].name].use_deform}
    total=sum(weights.values())
    active={n:w/total for n,w in weights.items() if n in face_names} if total>0 else {}
    if not active:continue
    current={mesh.vertex_groups[g.group].name:g.weight for g in mesh.data.vertices[vertex.index].groups}
    head=current.get('Head',0)
    transfer=min(head,sum(active.values()))
    if transfer<=0:continue
    mesh.vertex_groups['Head'].add([vertex.index],head-transfer,'REPLACE')
    for n,w in active.items():mesh.vertex_groups[n].add([vertex.index],w*transfer/sum(active.values()),'REPLACE')
for obj_name,first in [('character2.004',15789),('character2.005',16143)]:
    # Object numeric suffixes do not encode anatomical side; use the actual source parent.
    side=bpy.data.objects[obj_name].parent_bone[-1]
    ids=list(range(first,first+354))
    for g in mesh.vertex_groups:
        if g.name in keep:g.remove(ids)
    mesh.vertex_groups['SS_Eye_'+side].add(ids,1,'REPLACE')
poses=list(probe['poses'])
scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=len(poses)*3
for index,name in enumerate(poses):
    data=np.load(args.evidence/(name+'.npz'))
    for n in face_names+['SS_Eye_L','SS_Eye_R']:
        src='ORG-eye.'+n[-1] if n.startswith('SS_Eye_') else n
        pb=arm.pose.bones[n]
        # Relative evaluated motion around the source neutral frame preserves the shape-key neutral mesh.
        delta=Matrix(data[src].tolist()) @ Matrix(neutral[src].tolist()).inverted()
        pb.matrix=world_inverse @ delta @ arm.matrix_world @ pb.bone.matrix_local
        pb.rotation_mode='QUATERNION'
        for frame in [index*3+1,index*3+2,index*3+3]:
            pb.keyframe_insert('location',frame=frame);pb.keyframe_insert('rotation_quaternion',frame=frame);pb.keyframe_insert('scale',frame=frame)
arm.animation_data.action.name='FacialPoseSamples'
scene.frame_set(1);bpy.context.view_layer.update()
for obj in bpy.context.selected_objects:obj.select_set(False)
arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(args.output.resolve()),use_selection=True,object_types={'ARMATURE','MESH'},
    use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,axis_forward='-Z',axis_up='Y',
    bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
    bake_anim_force_startend_keying=True,path_mode='STRIP')
report={'bones':len(arm.data.bones),'face_bones':face_names+['SS_Eye_L','SS_Eye_R'],
    'body_bones':sorted(keep),'shapes':[k.name for k in mesh.data.shape_keys.key_blocks],
    'poses':[{'name':n,'firstFrame':i*3+1,'lastFrame':i*3+3} for i,n in enumerate(poses)],
    'vertices':len(mesh.data.vertices),'constraints':sum(len(p.constraints) for p in arm.pose.bones),
    'source_preserve_volume':[(o.name,m.use_deform_preserve_volume) for o in [face_body,original_mesh] for m in o.modifiers if m.type=='ARMATURE'],
    'hybrid_pose_comparison':{}}
reference=np.array([original_mesh.matrix_world @ v.co for v in original_mesh.data.vertices])
for name in ['Neutral','BlinkBoth','LookLeft','BrowRaiseBoth']:
    scene.frame_set(poses.index(name)*3+2);bpy.context.view_layer.update()
    obj=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());evaluated=obj.to_mesh()
    coords=np.array([obj.matrix_world @ v.co for v in evaluated.vertices]);obj.to_mesh_clear()
    target=reference+np.load(args.evidence/(name+'.npz'))['points']-neutral['points']
    error=np.linalg.norm(coords-target,axis=1)
    report['hybrid_pose_comparison'][name]={'max_m':float(error.max()),'over_1mm':int(sum(error>.001))}
    np.savez_compressed(args.evidence/('Hybrid'+name+'.npz'),points=coords)
(args.evidence/'hybrid-export.json').write_text(json.dumps(report,indent=2))
print('Exported',report['bones'],'bones,',len(report['shapes'])-1,'shape keys, constraints',report['constraints'])
