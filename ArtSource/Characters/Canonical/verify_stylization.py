"""Verify the study preserves the accepted source's topology, UVs and weights."""
import bpy,hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'ArtSource/Characters/Canonical'
def digest(values):
    h=hashlib.sha256()
    for value in values:h.update(struct.pack('<d',float(value)))
    return h.hexdigest()
def inspect(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    mesh=body.data
    return {'vertices':len(mesh.vertices),'faces':len(mesh.polygons),
        'topology':digest(n for p in mesh.polygons for n in (len(p.vertices),*p.vertices)),
        'uv':digest(v for loop in mesh.uv_layers.active.data for v in loop.uv),
        'weights':digest(n for v in mesh.vertices for g in v.groups for n in (v.index,g.group,g.weight)),
        'bone_parents':{b.name:b.parent.name if b.parent else None for b in rig.data.bones},
        'basis':digest(n for v in mesh.vertices for n in v.co),
        'shape_keys':[] if not mesh.shape_keys else [k.name for k in mesh.shape_keys.key_blocks],
        'degenerate_faces':sum(p.area<1e-12 for p in mesh.polygons),
        'unweighted_vertices':sum(not v.groups for v in mesh.vertices)}
baseline=inspect(BASE/'Baseline_Authored/Canonical_AuthoredCandidate.blend')
study=inspect(BASE/'Canonical_AuthoredCandidate.blend')
checks={key:baseline[key]==study[key] for key in ('vertices','faces','topology','uv','weights','bone_parents','basis')}
checks['one_study_shape_key']=study['shape_keys']==['Basis','SilverScreen_ArtStudy01']
checks['no_new_degenerate_faces']=study['degenerate_faces']==baseline['degenerate_faces']
checks['no_unweighted_vertices']=study['unweighted_vertices']==0
result={'checks':checks,'passed':all(checks.values()),'baseline':baseline,'study':study,
    'scope':'source integrity only; not visual, animation, performance or gameplay approval'}
(ROOT/'ArtReview/Characters/Canonical/study-source-validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps({'passed':result['passed'],'checks':checks},indent=2))
if not result['passed']:raise RuntimeError('Source integrity validation failed')
