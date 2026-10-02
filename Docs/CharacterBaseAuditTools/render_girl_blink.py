"""Render evaluated Girl calibration geometry; technical evidence, user art gate."""
import argparse
import json
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Vector

p = argparse.ArgumentParser()
p.add_argument('--evidence', type=Path, required=True)
p.add_argument('--names', default='neutral,u90_l20,u95_l20,u95_l25')
p.add_argument('--views', default='front,oblique,face')
args = p.parse_args(sys.argv[sys.argv.index('--')+1:])
out = args.evidence
neutral = np.load(out/'neutral.npz')
faces = json.loads((out/'topology.json').read_text())
hair = set(neutral['hair'].tolist())
bald = [f for f in faces if not any(v in hair for v in f)]
points = neutral['points']
eye = points[np.concatenate([neutral['eye_L'], neutral['eye_R']])].mean(0)
body = points[neutral['body']]
head = np.array([0, eye[1]+.065, (body[:, 2].max()+eye[2]-.08)/2])
settings = json.loads((out/'render-settings.json').read_text())
settings.append(dict(view='face', position=(eye+np.array([0, -.8, -.035])).tolist(), target=(eye+np.array([0, 0, -.035])).tolist(), ortho_scale=.34))
settings.append(dict(view='face-oblique', position=(eye+np.array([.42, -.7, .005])).tolist(), target=(eye+np.array([0, 0, -.035])).tolist(), ortho_scale=.34))
for side in ['L','R']:
    center=points[neutral['eye_'+side]].mean(0)
    settings.append(dict(view='eye-'+side,position=(center+np.array([0,-.6,0])).tolist(),target=center.tolist(),ortho_scale=.073))
    settings.append(dict(view='eye-oblique-'+side,position=(center+np.array([.36 if side=='L' else -.36,-.6,.034])).tolist(),target=center.tolist(),ortho_scale=.09))
scalp = [dict(view='scalp-'+name, position=(head+np.array(offset)).tolist(), target=head.tolist(), ortho_scale=.36)
         for name, offset in [('front', [0, -.8, 0]), ('side', [.8, 0, 0]), ('crown', [.3, -.35, .65])]]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.render.resolution_x = 1000
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Technical grey')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.19, .19, .19, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
material = bpy.data.materials.new('Technical neutral')
material.diffuse_color = (.65, .65, .65, 1)
mesh = bpy.data.meshes.new('Evaluated child')
obj = bpy.data.objects.new('Evaluated child', mesh)
scene.collection.objects.link(obj)
mesh.materials.append(material)
camera = bpy.data.objects.new('Technical camera', bpy.data.cameras.new('Technical camera'))
scene.collection.objects.link(camera)
camera.data.type = 'ORTHO'
scene.camera = camera
light = bpy.data.objects.new('Technical key', bpy.data.lights.new('Technical key', 'AREA'))
scene.collection.objects.link(light)
light.data.energy = 25
light.data.size = .6
light.location = eye+np.array([-.3, -.5, .35])
light.rotation_euler = (Vector(eye)-light.location).to_track_quat('-Z', 'Y').to_euler()
for name in args.names.split(','):
    points = np.load(out/(name+'.npz'))['points']
    for variant in ['bald']:
        mesh.clear_geometry()
        mesh.from_pydata(points.tolist(), [], faces if variant == 'hair' else bald)
        for face in mesh.polygons:
            face.use_smooth = True
        mesh.update()
        for setting in settings+scalp:
            if setting['view'] not in args.views.split(','): continue
            camera.location = setting['position']
            camera.rotation_euler = (Vector(setting['target'])-camera.location).to_track_quat('-Z', 'Y').to_euler()
            camera.data.ortho_scale = setting['ortho_scale']
            scene.render.filepath = str((out/(setting['view']+'-'+name+'-'+variant+'.png')).resolve())
            bpy.ops.render.render(write_still=True)
(out/'capture-settings.json').write_text(json.dumps(settings+scalp, indent=2))
