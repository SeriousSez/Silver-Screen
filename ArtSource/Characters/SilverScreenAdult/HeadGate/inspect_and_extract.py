"""Audit the authorized CC source and extract only its head-region topology.

Downloads remain read-only. This file is source preparation, not the identity sculpt.
"""
import bpy
import json
import hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
SOURCE=Path('C:/Users/Sez/Downloads/Blender/Blender 1/blender.Fbx')
ROOT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE),use_anim=False)
body=bpy.data.objects['CC_Base_Body']

def coords(obj,values=None):
    values=values if values is not None else obj.data.vertices
    return np.array([tuple(obj.matrix_world@v.co) for v in values],dtype=np.float64)

def classify(name):
    if name.startswith('Eyelash_'):
        return 'source-specific','archive: eyelash-card adjustment; do not retain as a skin expression'
    if name.startswith('Eye_') and '_Look_' in name:
        return 'corrective','refit later against semantic eye gaze bones; never drive whole eye rotation with this skin shape'
    if name.startswith('Head_'):
        return 'source-specific','archive: source head-pose morph; use semantic head/neck bones instead'
    if name.startswith('Neck_'):
        return 'corrective','archive for inspection; recreate only if needed for the SilverScreen neck/head bones'
    if name.startswith(('V_','Tongue_')):
        return 'facial-expression','defer full viseme/tongue library beyond the head gate'
    return 'facial-expression','candidate only: refit and validate after identity approval'

base=coords(body,body.data.shape_keys.key_blocks[0].data)
headslot=next(i for i,m in enumerate(body.data.materials) if m.name=='Std_Skin_Head')
headfaces=[list(p.vertices) for p in body.data.polygons if p.material_index==headslot]
headids=sorted({i for f in headfaces for i in f})
headset=set(headids)
audit=[];archive={}
for key in list(body.data.shape_keys.key_blocks)[1:]:
    d=coords(body,key.data)-base
    mag=np.linalg.norm(d,axis=1)
    active=np.flatnonzero(mag>1e-5)
    cat,decision=classify(key.name)
    archive[key.name]=d[headids].astype(np.float32)
    audit.append({'name':key.name,'category':cat,'decision':decision,
        'affectedVerticesAbove10Microns':len(active),'headVerticesAffected':sum(int(i) in headset for i in active),
        'maximumDeltaMetres':float(mag.max()),
        'activeBoundsMin':base[active].min(axis=0).tolist() if len(active) else None,
        'activeBoundsMax':base[active].max(axis=0).tolist() if len(active) else None,
        'copiedIntoCandidate':False,'driverContractVerified':False})
report={'source':str(SOURCE),'sourceSHA256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    'shapeCount':len(audit),'classificationBasis':'Name, actual nonzero vertex displacement, and affected region. Corrective means a rig-coupled candidate, not verified corrective behavior; source driver metadata is unavailable. Not a validated deformation test.',
    'identityBodyCustomizationShapes':0,'identityNote':'No dedicated identity/body customization morphs identified among the 148 exported names. Local facial action controls are not identity presets.',
    'categoryCounts':{c:sum(a['category']==c for a in audit) for c in ['identity/body-customization','facial-expression','corrective','source-specific']},
    'shapes':audit,'materials':[],'objects':[]}
for i,m in enumerate(body.data.materials):
    ids=sorted({j for p in body.data.polygons if p.material_index==i for j in p.vertices})
    report['materials'].append({'name':m.name,'vertices':len(ids),'boundsMin':base[ids].min(axis=0).tolist(),'boundsMax':base[ids].max(axis=0).tolist()})

parts=[('Head','CC_Base_Body',headfaces),('Eyes','CC_Base_Eye',None),('Teeth','CC_Base_Teeth',None),('Tongue','CC_Base_Tongue',None)]
arrays={}
for name,objname,faces in parts:
    obj=bpy.data.objects[objname]
    if faces is None:faces=[list(p.vertices) for p in obj.data.polygons]
    ids=sorted({i for f in faces for i in f});remap={old:new for new,old in enumerate(ids)}
    p=coords(obj,obj.data.shape_keys.key_blocks[0].data if obj.data.shape_keys else None)
    # JSON holds polygon/UV correspondence; NPZ holds floating point coordinates.
    uv=[]
    selected=set(tuple(f) for f in faces)
    for face in obj.data.polygons:
        if tuple(face.vertices) not in selected:continue
        uv.append([list(obj.data.uv_layers.active.data[l].uv) for l in face.loop_indices])
    data={'name':name,'sourceObject':objname,'sourceVertexIds':ids,
        'faces':[[remap[i] for i in f] for f in faces],'uv':uv,
        'boundsMin':p[ids].min(axis=0).tolist(),'boundsMax':p[ids].max(axis=0).tolist()}
    (ROOT/(name.lower()+'-topology.json')).write_text(json.dumps(data)+'\n')
    arrays[name]=p[ids]
    report['objects'].append({k:v for k,v in data.items() if k not in ('faces','uv','sourceVertexIds')})

np.savez_compressed(ROOT/'source-head-coordinates.npz',**arrays)
np.savez_compressed(ROOT/'archival-only-source-deltas.npz',**archive)
(ROOT/'shape-key-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='shapes'},indent=2))
