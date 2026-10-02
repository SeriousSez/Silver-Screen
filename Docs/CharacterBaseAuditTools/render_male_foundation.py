"""Matched technical (not art approval) Male source views from evaluated meshes."""
import argparse
import json
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Vector

p=argparse.ArgumentParser()
p.add_argument('--evidence',type=Path,required=True)
p.add_argument('--names',nargs='+',default=['neutral'])
p.add_argument('--scalp',action='store_true')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
out=args.evidence
faces=json.loads((out/'topology.json').read_text())
neutral=np.load(out/'neutral.npz')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1000;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Technical grey');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.19,.19,.19,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
material=bpy.data.materials.new('Technical neutral');material.diffuse_color=(.65,.65,.65,1)
mesh=bpy.data.meshes.new('Evaluated Male');mesh.from_pydata(neutral['points'].tolist(),[],faces)
obj=bpy.data.objects.new('Male evaluated surface',mesh);scene.collection.objects.link(obj);mesh.materials.append(material)
for f in mesh.polygons:f.use_smooth=True
camera=bpy.data.objects.new('Technical camera',bpy.data.cameras.new('Technical camera'));scene.collection.objects.link(camera)
camera.data.type='ORTHO';scene.camera=camera
light=bpy.data.objects.new('Area key',bpy.data.lights.new('Area key','AREA'));scene.collection.objects.link(light)
light.data.energy=25;light.data.size=.6;light.location=(-.3,-.5,2.1)
light.rotation_euler=(Vector((0,-.03,1.73))-light.location).to_track_quat('-Z','Y').to_euler()
settings=json.loads((out/'render-settings.json').read_text())
settings.append(dict(view='face',position=[0,-.85,1.72],target=[0,-.03,1.72],ortho_scale=.35))
if args.scalp:
    settings=[dict(view=n,position=pos,target=[0,.04,1.74],ortho_scale=.4) for n,pos in [
        ('head-front',[0,-.85,1.74]),('head-side',[.85,.04,1.74]),('head-crown',[.35,-.35,2.4])]]
for name in args.names:
    points=np.load(out/(name+'.npz'))['points']
    variants=['hair','bald'] if args.scalp else ['bald']
    for variant in variants:
        hair=set(neutral['hair'].tolist())
        polys=faces if variant=='hair' else [f for f in faces if not any(i in hair for i in f)]
        mesh.clear_geometry();mesh.from_pydata(points.tolist(),[],polys)
        for f in mesh.polygons:f.use_smooth=True
        mesh.update()
        for s in settings:
            camera.location=s['position'];camera.rotation_euler=(Vector(s['target'])-camera.location).to_track_quat('-Z','Y').to_euler()
            camera.data.ortho_scale=s['ortho_scale']
            scene.render.filepath=str((out/(s['view']+'-'+name+'-'+variant+'.png')).resolve())
            bpy.ops.render.render(write_still=True)
(out/'capture-settings.json').write_text(json.dumps(settings,indent=2))
