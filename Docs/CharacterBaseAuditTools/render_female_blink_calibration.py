"""Matched technical views of evaluated source-only calibration candidates."""
import argparse
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector


parser = argparse.ArgumentParser()
parser.add_argument('--evidence', type=Path, required=True)
parser.add_argument('--names', nargs='+', required=True)
parser.add_argument('--samples', type=int, default=32)
parser.add_argument('--close', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root = args.evidence
faces = json.loads((root/'topology.json').read_text())
settings = json.loads((root/'render-settings.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = args.samples
scene.render.resolution_x, scene.render.resolution_y = 1000, 700
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Technical world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.19, .19, .19, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
material = bpy.data.materials.new('Neutral technical comparison')
material.diffuse_color = (.65, .65, .65, 1)
mesh = bpy.data.meshes.new('Evaluated complete source surface')
mesh.from_pydata(np.load(root/'neutral.npz')['points'].tolist(), [], faces)
obj = bpy.data.objects.new('Evaluated surface', mesh)
scene.collection.objects.link(obj)
mesh.materials.append(material)
for poly in mesh.polygons:
    poly.use_smooth = True
data = bpy.data.cameras.new('Matched source camera')
camera = bpy.data.objects.new('Matched source camera', data)
scene.collection.objects.link(camera)
data.type = 'ORTHO'
scene.camera = camera
light_data = bpy.data.lights.new('Technical key', 'AREA')
light_data.energy, light_data.size = 75, .6
light_data.shape = 'DISK'
light = bpy.data.objects.new('Technical key', light_data)
scene.collection.objects.link(light)
light.location = (-.3, -.5, 2.05)
light.rotation_euler = (Vector((0, -.06, 1.703))-light.location).to_track_quat('-Z', 'Y').to_euler()
if args.close:
    settings += [dict(view='eye-'+side, position=(x, -.85, 1.675), target=(x, -.03, 1.666), ortho_scale=.069)
                 for side,x in [('R',-.04),('L',.04)]]
    settings += [dict(view='face', position=(0, -.85, 1.66), target=(0, -.03, 1.66), ortho_scale=.35)]
for setting in settings:
    camera.location = setting['position']
    camera.rotation_euler = (Vector(setting['target'])-camera.location).to_track_quat('-Z', 'Y').to_euler()
    data.ortho_scale = setting['ortho_scale']
    for name in args.names:
        points = np.load(root/(name+'.npz'))['points']
        mesh.vertices.foreach_set('co', points.astype(np.float32).ravel())
        mesh.update()
        scene.render.filepath = str((root/(setting['view']+'-'+name+'.png')).resolve())
        bpy.ops.render.render(write_still=True)
(root/'capture-settings.json').write_text(json.dumps({'views': settings, 'samples': args.samples,
                                                      'names': args.names}, indent=2))
