"""Read-only inspection of user-supplied FBXs; writes reports/previews only."""
import bpy
import bmesh
from pathlib import Path
from mathutils import Vector
import json
import sys

args=sys.argv[sys.argv.index('--')+1:]
source=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source),use_anim=False)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
report={'source':str(source),'sourceModified':False,'meshCount':len(meshes),'meshes':[],'armatures':[]}
for rig in rigs:
    rig.data.pose_position='REST'
    report['armatures'].append({'name':rig.name,'bones':len(rig.data.bones),
        'headBones':[b.name for b in rig.data.bones if any(x in b.name.lower() for x in ('head','jaw','eye','neck'))]})
bpy.context.view_layer.update()
for o in meshes:
    coords=[o.matrix_world@Vector(c) for c in o.bound_box]
    ks=o.data.shape_keys.key_blocks if o.data.shape_keys else []
    report['meshes'].append({'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),
        'quadFaces':sum(len(p.vertices)==4 for p in o.data.polygons),
        'triangleFaces':sum(len(p.vertices)==3 for p in o.data.polygons),
        'uvLayers':len(o.data.uv_layers),'materialNames':[m.name if m else None for m in o.data.materials],
        'shapeKeyCount':len(ks),'shapeKeys':[k.name for k in ks],
        'vertexGroups':len(o.vertex_groups),'modifiers':[m.type for m in o.modifiers],
        'boundsMin':[min(p[k] for p in coords) for k in range(3)],'boundsMax':[max(p[k] for p in coords) for k in range(3)]})
body=next((o for o in meshes if 'CC_Base_Body' in o.name),None)
headbone=None
for rig in rigs:
    for b in rig.data.bones:
        if b.name=='CC_Base_Head':headbone=rig.matrix_world@b.head_local
if body is None:raise RuntimeError('Expected body mesh absent')
for o in meshes:
    o.hide_render=not any(token in o.name.lower() for token in ('cc_base_body','cc_base_eye','cc_base_tongue','cc_base_teeth','cc_base_tearline','cc_base_eyeocclusion'))
    if 'eyeocclusion' in o.name.lower() or 'tearline' in o.name.lower():o.hide_render=True
    mat=bpy.data.materials.new('InspectionClay_'+o.name);mat.use_nodes=True
    col=(.43,.43,.43,1)
    if o.name=='CC_Base_Eye':col=(.66,.66,.66,1)
    elif 'teeth' in o.name.lower():col=(.55,.55,.55,1)
    elif 'tongue' in o.name.lower():col=(.17,.17,.17,1)
    # Alpha eyelash cards become misleading solid strips in a clay override.
    # Exclude them only from this in-memory inspection, never from the source.
    lash_slots={i for i,m in enumerate(o.data.materials) if m and 'eyelash' in m.name.lower()}
    if lash_slots:
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in lash_slots],context='FACES')
        bm.to_mesh(o.data);bm.free()
    mat.diffuse_color=col
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=col;bs.inputs['Roughness'].default_value=.7
    o.data.materials.clear();o.data.materials.append(mat)
    for p in o.data.polygons:p.material_index=0;p.use_smooth=True
scene=bpy.context.scene
scene.world=bpy.data.worlds.new('Neutral inspection');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
deps=bpy.context.evaluated_depsgraph_get();ev=body.evaluated_get(deps)
points=[ev.matrix_world@v.co for v in ev.data.vertices]
zmax=max(p.z for p in points);zmin=min(p.z for p in points)
height=zmax-zmin
target=Vector((0,0,zmax-.095*height))
if headbone is not None:target.x=headbone.x;target.y=headbone.y
report['bodyHeight']=height;report['headBoneWorld']=list(headbone) if headbone else None
# CC exports face toward -Y after FBX import; record and verify via inspection.
for name,delta,power in [('Key',(2,-3,3),350),('Fill',(-3,-2,2),250),('Back',(0,3,3),250)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=3
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj)
    obj.location=target+Vector(delta);obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('SourceInspectionCamera');cam=bpy.data.objects.new('SourceInspectionCamera',data);scene.collection.objects.link(cam);scene.camera=cam
data.type='ORTHO';data.ortho_scale=height*.235
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=760;scene.render.resolution_y=840;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
report['inspectionOnly']=True
(out/'inspection.json').write_text(json.dumps(report,indent=2)+'\n')
for name,delta in [('head-front',(0,-3,0)),('head-threequarter',(1.6,-3,.05)),('head-profile',(3,0,0))]:
    cam.location=target+Vector(delta);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
wire=body.copy();wire.data=body.data.copy();scene.collection.objects.link(wire)
wire.name='InspectionTopologyOnly'
wm=bpy.data.materials.new('InspectionWire');wm.diffuse_color=(.008,.008,.008,1);wm.use_nodes=True
wm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.008,.008,.008,1)
wire.data.materials.clear();wire.data.materials.append(wm)
for f in wire.data.polygons:f.material_index=0
wiremod=wire.modifiers.new('InspectionWire','WIREFRAME')
wiremod.thickness=.00045/max(abs(v) for v in body.matrix_world.to_scale());wiremod.offset=1
cam.location=target+Vector((.8,-3,.04));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'head-topology.png');bpy.ops.render.render(write_still=True)
print(json.dumps({'source':str(source),'meshCount':len(meshes),'bodyHeight':height,'output':str(out)}))
