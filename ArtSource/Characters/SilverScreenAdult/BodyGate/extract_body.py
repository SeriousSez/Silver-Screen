"""Extract authorized CC anatomy for a male foundation; no source styling/rig output."""
import bpy
import json
import hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
SRC=Path('C:/Users/Sez/Downloads/Blender/Blender 1/blender.Fbx')
ROOT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SRC),use_anim=False)
o=bpy.data.objects['CC_Base_Body']
for rig in (x for x in bpy.context.scene.objects if x.type=='ARMATURE'):rig.data.pose_position='REST'
bpy.context.view_layer.update()
p=np.array([tuple(o.matrix_world@v.co) for v in o.data.shape_keys.key_blocks[0].data])
material_names=[m.name for m in o.data.materials]
chosen=[f for f in o.data.polygons if material_names[f.material_index] not in ('Std_Skin_Head','Std_Eyelash')]
ids=sorted({i for f in chosen for i in f.vertices});remap={i:k for k,i in enumerate(ids)}
data={'source':str(SRC),'sourceSHA256':hashlib.sha256(SRC.read_bytes()).hexdigest(),
    'sourceObject':o.name,'sourceVertexIds':ids,'faces':[[remap[i] for i in f.vertices] for f in chosen],
    'uv':[[list(o.data.uv_layers.active.data[i].uv) for i in f.loop_indices] for f in chosen],
    'faceRegions':[material_names[f.material_index] for f in chosen],
    'materialNames':material_names,'sourceBones':[]}
for rig in (x for x in bpy.context.scene.objects if x.type=='ARMATURE'):
    for b in rig.data.bones:
        if b.name.startswith('CC_Base_'):
            data['sourceBones'].append({'name':b.name,'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)})
# Source weighting is retained only as an anatomical region selector, not a skin
# binding. The eventual production weights must target the SilverScreen rig.
group_names=[g.name for g in o.vertex_groups]
weights=np.zeros((len(ids),len(group_names)),dtype=np.float32)
for k,i in enumerate(ids):
    for g in o.data.vertices[i].groups:weights[k,g.group]=g.weight
data['sourceRegionGroupNames']=group_names
np.savez_compressed(ROOT/'source-body-data.npz',coordinates=p[ids],regionSelectors=weights)
(ROOT/'source-body-topology.json').write_text(json.dumps(data)+'\n')

# A clay source inspection, recorded separately from the male candidate.
for x in list(bpy.data.objects):bpy.data.objects.remove(x,do_unlink=True)
m=bpy.data.meshes.new('AuthorizedSourceAnatomy');m.from_pydata(p[ids].tolist(),[],data['faces']);m.update()
obj=bpy.data.objects.new('Source anatomy - inspection only',m);bpy.context.scene.collection.objects.link(obj)
mat=bpy.data.materials.new('Source inspection clay');mat.diffuse_color=(.25,.25,.25,1);mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.25,.25,.25,1)
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.68
m.materials.append(mat)
for f in m.polygons:f.use_smooth=True
sub=obj.modifiers.new('Surface','SUBSURF');sub.levels=1
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.27,.27,.27,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
focus=Vector((0,0,.79))
for name,loc,power,size in [('Key',(-2,-3,4),500,3),('Fill',(2,-1,2),150,3)]:
    ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.size=size
    light=bpy.data.objects.new(name,ld);scene.collection.objects.link(light);light.location=loc
    light.rotation_euler=(focus-light.location).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('Inspect');cam=bpy.data.objects.new('Inspect',camd);scene.collection.objects.link(cam);scene.camera=cam
camd.type='ORTHO';camd.ortho_scale=1.70
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=900;scene.render.resolution_y=1000
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
out=ROOT.parents[3]/'ArtReview/Characters/SilverScreenAdult/BodyGate/SourceInspection';out.mkdir(parents=True,exist_ok=True)
for name,delta in [('front',(0,-4,0)),('side',(4,0,0)),('back',(0,4,0))]:
    cam.location=focus+Vector(delta);cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps({'vertices':len(ids),'faces':len(chosen),'boundsMin':p[ids].min(0).tolist(),'boundsMax':p[ids].max(0).tolist(),
    'landmarks':[b for b in data['sourceBones'] if any(k in b['name'] for k in ('Pelvis','Spine','Clavicle','Upperarm','Forearm','Hand','Thigh','Calf','Foot','ToeBase','Neck'))]},indent=2))
