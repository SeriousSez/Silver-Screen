"""Saved-file structural and neutral-fit audit; not a deformation approval."""
import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parent;REPO=ROOT.parents[3]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def sig(o):
    return {'coordinates':[list(v.co) for v in o.data.vertices],'faces':[list(p.vertices) for p in o.data.polygons],
            'uv':[[list(u.uv) for u in l.data] for l in o.data.uv_layers]}
bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'Foundations/SS_MaleBody_Foundation_v1.blend'))
names=['SS_MaleBody_Foundation_Candidate','SS_MaleHead_Foundation_v1','SS_Eye_L','SS_Eye_R','SS_Teeth','SS_Tongue']
baseline={n:sig(bpy.data.objects[n]) for n in names}
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SS_Canonical_Unrigged_Candidate.blend'))
checks={n:sig(bpy.data.objects[n])==s for n,s in baseline.items()}
body=bpy.data.objects[names[0]];head=bpy.data.objects[names[1]]
bm=bmesh.new();bm.from_mesh(head.data);seam=list({v.index for e in bm.edges if e.is_boundary for v in e.verts});bm.free()
keys=head.data.shape_keys.key_blocks
seam_delta=max((k.data[i].co-keys['Basis'].data[i].co).length for k in keys for i in seam)
identity_names=[k.name for k in keys if k.name!='Basis']
expected=['Anatomy.Body','Anatomy.Head','Anatomy.Eye.L','Anatomy.Eye.R','Anatomy.Mouth','Module.Hair','Module.Brows','Module.Shirt','Module.Waistcoat','Module.Trousers','Module.Shoe.L','Module.Shoe.R','Module.Fedora','Module.Tie','Module.Belt']
module_checks={n:bool(bpy.data.collections.get(n) and len(bpy.data.collections[n].objects)>0) for n in expected}
counts={};degenerate={}
for n in expected:
    c=bpy.data.collections[n];counts[n]={'objects':len(c.objects),'meshVertices':sum(len(o.data.vertices) for o in c.objects if o.type=='MESH')}
    for o in c.objects:
        if o.type=='MESH':
            bm=bmesh.new();bm.from_mesh(o.data);bad=sum(f.calc_area()<1e-12 for f in bm.faces);bm.free()
            if bad:degenerate[o.name]=bad
preserved={'SS_Adult_Authoring.blend':'23aab3cd859f9afccdb8c7731e33e83046542232c3ad6c1612cd51ae04e5cbd1',
           'rig-contract.json':'62e401ed06f554d13deff8c407c03ea5a8d173feb4b5067be124021a318912e4',
           'SS_Adult_Accessories.blend':'4e50aa2cfd4c22e1764e6d3af950973fe339d4fb390bf38303c1125afa629bdd'}
unchanged={n:sha(ROOT.parent/n)==v for n,v in preserved.items()}
unchanged['Studio.unity']=sha(REPO/'Assets/Scenes/Studio.unity')=='1ec4bb0925316abd4c02d82f374badb8c993482d9c9d37eff5463d169343e1d6'
# Explicit literal avoids conflating basis preservation with identity evaluation.
unchanged['approvedHeadFoundation']=sha(ROOT.parent/'Foundations/SS_MaleHead_Foundation_v1.blend')=='ad692545e7b10451a5856c78b33695bf33a4f522c0e14cc6b29c292615a48eaf'
report={'candidateSHA256':sha(ROOT/'SS_Canonical_Unrigged_Candidate.blend'),'foundationBasisGeometryTopologyUVUnchanged':checks,
 'identityTargets':identity_names,'identityNeckSeamMaxDeltaMetres':seam_delta,'modulesPresent':module_checks,'counts':counts,'degenerateFacesByObject':degenerate,
 'bodyFacesRetained':len(body.data.polygons),'bodyVerticesRetained':len(body.data.vertices),'hiddenBodyFaces':sum(p.hide for p in body.data.polygons),
 'bodyObjectVisible':not body.hide_render,'bodyCollectionVisible':not bpy.data.collections['Anatomy.Body'].hide_render,
 'armatureObjects':sum(o.type=='ARMATURE' for o in bpy.data.objects),
 'armatureModifiers':sum(m.type=='ARMATURE' for o in bpy.data.objects for m in o.modifiers),
 'animationActions':len(bpy.data.actions),'preservedFilesUnchanged':unchanged,
 'deformationValidated':False,'UnityImported':False,'finalMaterialsValidated':False,'visualGate':'Awaiting first assembled canonical-character review'}
(ROOT/'validation-report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
assert all(checks.values()) and all(module_checks.values()) and all(unchanged.values())
assert seam_delta<1e-8 and all(n.startswith('Identity.') for n in identity_names)
assert report['bodyFacesRetained']==9288 and report['hiddenBodyFaces']==0
assert report['armatureObjects']==0 and report['armatureModifiers']==0
assert not degenerate,degenerate
