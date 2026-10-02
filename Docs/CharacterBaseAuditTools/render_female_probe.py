"""Render evaluated source facial poses as local technical evidence."""
import argparse,json,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
path=next(p for p in args.source.rglob('*FacialRig.blend') if 'female' in str(p).lower())
bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
shape=bpy.data.objects['Character_shape_keys.002']
faces=[list(p.vertices) for p in shape.data.polygons]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Technical world');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.19,.19,.19,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
mat=bpy.data.materials.new('Supplied neutral technical');mat.diffuse_color=(.65,.65,.65,1)
mesh=bpy.data.meshes.new('Evaluated female');mesh.from_pydata(np.load(args.output/'Neutral.npz')['points'].tolist(),[],faces)
obj=bpy.data.objects.new('Evaluated source face',mesh);scene.collection.objects.link(obj);mesh.materials.append(mat)
for p in mesh.polygons:p.use_smooth=True
camera_data=bpy.data.cameras.new('Technical camera');camera=bpy.data.objects.new('Technical camera',camera_data);scene.collection.objects.link(camera)
camera.location=(0,-.85,1.70);target=Vector((0,-.01,1.68));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO';camera_data.ortho_scale=.38;scene.camera=camera
light_data=bpy.data.lights.new('Technical key','AREA');light_data.energy=75;light_data.shape='DISK';light_data.size=.6
light=bpy.data.objects.new('Technical key',light_data);scene.collection.objects.link(light);light.location=(-.3,-.5,2.05)
light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
for name in ['Neutral','BlinkBoth','BlinkLeft','BrowRaiseBoth']:
    points=np.load(args.output/(name+'.npz'))['points']
    mesh.vertices.foreach_set('co',points.astype(np.float32).ravel());mesh.update()
    scene.render.filepath=str((args.output/('source-'+name+'.png')).resolve())
    bpy.ops.render.render(write_still=True)
