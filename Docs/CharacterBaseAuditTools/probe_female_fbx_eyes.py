"""Check actual eye skin membership in the supplied female facial FBXs."""
import argparse,json,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils.kdtree import KDTree
parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True);parser.add_argument('--evidence',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
neutral=np.load(args.evidence/'Neutral.npz')['points'];tree=KDTree(708)
for i,p in enumerate(neutral[15789:16497]):tree.insert(p,i)
tree.balance();report=[]
for path in args.source.rglob('*.fbx'):
    if 'female' not in str(path).lower() or 'FacialRig(' not in path.name:continue
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
    bpy.context.view_layer.update()
    groups={};count=0;matches=set()
    for obj in bpy.data.objects:
        if obj.type!='MESH':continue
        for v in obj.data.vertices:
            _,i,d=tree.find(obj.matrix_world @ v.co)
            if d>1e-5:continue
            count+=1;matches.add(i)
            for g in v.groups:
                if g.weight>1e-5:
                    name=obj.vertex_groups[g.group].name;groups[name]=groups.get(name,0)+1
    report.append({'source':str(path),'matched_eye_vertices':count,'unique_eye_vertices':len(matches),'weighted_groups':groups})
(args.evidence/'fbx-eye-membership.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
