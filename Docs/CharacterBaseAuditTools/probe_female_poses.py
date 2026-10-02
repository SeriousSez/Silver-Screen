"""Deterministic in-memory adult female rig probes. No purchased file is saved."""
import argparse
import json
import math
from pathlib import Path
import sys
import bpy
import numpy as np
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
args.output.mkdir(parents=True,exist_ok=True)
path=next(p for p in args.source.rglob('*FacialRig.blend') if 'female' in str(p).lower())
bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
for collection in bpy.data.collections: collection.hide_viewport=False
def expose(layer):
    layer.exclude=False; layer.hide_viewport=False
    for child in layer.children: expose(child)
expose(bpy.context.view_layer.layer_collection)
for obj in bpy.data.objects: obj.hide_viewport=False; obj.hide_set(False)
bpy.context.view_layer.update()
arm=bpy.data.objects['Armature_Character1.001']
shape=bpy.data.objects['Character_shape_keys.002']
body=bpy.data.objects['character2.003']
offsets={'character2.003':0,'GumsLower_lowres.003':12937,'GumsLower_lowres.001':13153,
    'GumsUpper_lowres.001':14377,'character2.004':15789,'character2.005':16143,'character2.002':16497}
def canon(poly):
    p=list(poly); i=p.index(min(p)); a=tuple(p[i:]+p[:i]);p.reverse();i=p.index(min(p))
    return min(a,tuple(p[i:]+p[:i]))
src={canon([i+offsets[n] for i in p.vertices]) for n in offsets for p in bpy.data.objects[n].data.polygons}
dst={canon(p.vertices) for p in shape.data.polygons}
report={'topology_correspondence':{'source_polygons':len(src),'target_polygons':len(dst),'matched':len(src&dst)},'poses':{},
    'bbones':{b.name:b.bbone_segments for b in arm.data.bones if b.use_deform},
    'driver_status':[{'path':fc.data_path,'valid':fc.driver.is_valid,'type':fc.driver.type,'expression':fc.driver.expression}
        for fc in arm.animation_data.drivers if any(s in fc.data_path for s in ['eye','jaw_master'])]}
assert src==dst,'Source and shape mesh connectivity differs; stop hybrid experiment.'
rest={p.name:p.matrix_basis.copy() for p in arm.pose.bones}
def reset():
    for name,matrix in rest.items(): arm.pose.bones[name].matrix_basis=matrix
    bpy.context.view_layer.update()
def move(name,delta):
    pb=arm.pose.bones[name]
    pb.location+=pb.bone.matrix_local.to_3x3().inverted() @ Vector(delta)
def blink(side,amount=1):
    move('lid.T.'+side+'.002',(0,0,-.0255*amount))
    move('lid.B.'+side+'.002',(0,0,.0055*amount))
def brow(side,amount):
    for suffix in ['', '.001','.003']:
        move('brow.T.'+side+suffix,(0,0,.008*amount))
def evaluate():
    bpy.context.view_layer.update()
    graph=bpy.context.evaluated_depsgraph_get()
    points=np.zeros((len(shape.data.vertices),3),dtype=np.float64)
    for name,offset in offsets.items():
        obj=bpy.data.objects[name].evaluated_get(graph); mesh=obj.to_mesh()
        points[offset:offset+len(mesh.vertices)]=[obj.matrix_world @ v.co for v in mesh.vertices]
        obj.to_mesh_clear()
    matrices={p.name:np.array(arm.matrix_world @ p.matrix) for p in arm.pose.bones}
    return points,matrices
neutral,neutral_bones=evaluate()
target=np.array([shape.matrix_world @ v.co for v in shape.data.vertices])
delta=np.linalg.norm(neutral-target,axis=1)
report['neutral_difference']={'max':float(delta.max()),'over_1mm':int(sum(delta>.001)),
    'body_max':float(delta[:12937].max()),'tongue_max':float(delta[12937:13153].max())}
poses=['Neutral','BlinkLeft','BlinkRight','BlinkBoth','LookLeft','LookRight','LookUp','LookDown',
    'BrowRaiseLeft','BrowRaiseRight','BrowRaiseBoth','BrowLowerBoth','LookLeftBlink','NeutralRestored']
weighted={body.vertex_groups[g.group].name for v in body.data.vertices for g in v.groups if g.weight>1e-5}
all_changed=set()
for pose in poses:
    reset()
    if 'Blink' in pose:
        if pose in ['BlinkLeft','BlinkBoth','LookLeftBlink']:blink('L')
        if pose in ['BlinkRight','BlinkBoth','LookLeftBlink']:blink('R')
    if pose.startswith('Look'):
        d={'LookLeft':(.10,0,0),'LookRight':(-.10,0,0),'LookUp':(0,0,.07),'LookDown':(0,0,-.07),'LookLeftBlink':(.10,0,0)}[pose]
        move('eyes',d)
    if pose.startswith('Brow'):
        for side in ['L','R']:
            if pose.endswith('Both') or pose.endswith('Left' if side=='L' else 'Right'):
                brow(side,-1 if 'Lower' in pose else 1)
    points,matrices=evaluate()
    distances=np.linalg.norm(points-neutral,axis=1)
    changed=[n for n in weighted if np.max(np.abs(matrices[n]-neutral_bones[n]))>1e-5]
    all_changed.update(changed)
    report['poses'][pose]={'max_vertex_delta':float(distances.max()),'changed_vertices':int(sum(distances>1e-5)),
        'opposite_side_max':float(distances[(neutral[:,0]<-.005) if 'Left' in pose else (neutral[:,0]>.005)].max()),
        'weighted_changed_bones':changed,
        'eye_angles_degrees':{s:math.degrees((Vector(neutral_bones['ORG-eye.'+s][:3,1])).angle(Vector(matrices['ORG-eye.'+s][:3,1]))) for s in ['L','R']}}
    # Exact evaluated positions/matrices are local licensed evidence, never Git assets.
    np.savez_compressed(args.output/(pose+'.npz'),points=points,**matrices)
report['candidate_changed_weighted_bones']=sorted(all_changed)
(args.output/'source-pose-probes.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
