"""Matched technical renders of evaluated source and the one-shape candidate."""
import argparse
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--evidence', type=Path, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = args.evidence
source = np.load(root / 'source.npz')
candidate = np.load(root / 'candidate.npz')
faces = json.loads((root / 'topology.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.render.resolution_x = 1000
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Technical world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.19, .19, .19, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
material = bpy.data.materials.new('Unchanged neutral technical comparison')
material.diffuse_color = (.65, .65, .65, 1)
mesh = bpy.data.meshes.new('Evaluated comparison')
mesh.from_pydata(source['neutral'].tolist(), [], faces)
obj = bpy.data.objects.new('Evaluated surface', mesh)
scene.collection.objects.link(obj)
mesh.materials.append(material)
for poly in mesh.polygons:
    poly.use_smooth = True
camera_data = bpy.data.cameras.new('Matched camera')
camera = bpy.data.objects.new('Matched camera', camera_data)
scene.collection.objects.link(camera)
camera_data.type = 'ORTHO'
scene.camera = camera
light_data = bpy.data.lights.new('Technical key', 'AREA')
light_data.energy = 75
light_data.shape = 'DISK'
light_data.size = .6
light = bpy.data.objects.new('Technical key', light_data)
scene.collection.objects.link(light)
light.location = (-.3, -.5, 2.05)
target = Vector((0, -.06, 1.703))
light.rotation_euler = (target - light.location).to_track_quat('-Z', 'Y').to_euler()
views = [('front', (0, -.85, 1.718), .245), ('oblique', (.38, -.85, 1.72), .245)]
poses = [('source-neutral', source['neutral']), ('candidate-neutral', candidate['baseline']),
         ('source-BlinkBoth', source['blink']), ('candidate-BlinkBoth', candidate['blink'])]
settings = []
for view, position, size in views:
    camera.location = position
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data.ortho_scale = size
    for name, points in poses:
        mesh.vertices.foreach_set('co', points.astype(np.float32).ravel())
        mesh.update()
        scene.render.filepath = str((root / (view + '-' + name + '.png')).resolve())
        bpy.ops.render.render(write_still=True)
    settings.append({'view': view, 'position': position, 'target': list(target), 'ortho_scale': size})
(root / 'render-settings.json').write_text(json.dumps(settings, indent=2))
