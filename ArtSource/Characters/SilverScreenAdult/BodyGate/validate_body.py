"""Inspect the saved body candidate; structural evidence is not deformation approval."""
import bpy
import bmesh
import numpy as np
import json
import hashlib
from pathlib import Path
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[3]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def signature(o,geometry=True):
    data={'faces':[list(p.vertices) for p in o.data.polygons],
          'uv':[[list(x.uv) for x in layer.data] for layer in o.data.uv_layers],
          'sourceIds':[x.value for x in o.data.attributes['CC_source_vertex'].data]}
    if geometry:data['coordinates']=[list(v.co) for v in o.data.vertices]
    return hashlib.sha256(json.dumps(data,separators=(',',':')).encode()).hexdigest()

bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'Foundations/SS_MaleHead_Foundation_v1.blend'))
parts={o.name:signature(o) for o in bpy.data.objects if o.type=='MESH'}
head=bpy.data.objects['SS_Head_Male_Candidate']
foundation={'id':'silverscreen.adult.male.head.v1','approval':'Male anatomical foundation; user approved technical/artistic direction',
    'file':'SS_MaleHead_Foundation_v1.blend','sha256':sha(ROOT.parent/'Foundations/SS_MaleHead_Foundation_v1.blend'),
    'headTopologyUVSourceIdFingerprint':signature(head,False),'meshGeometrySignatures':parts,
    'headVertices':len(head.data.vertices),'headQuads':len(head.data.polygons),
    'identityTargets':[],'expressionTargets':[],
    'historicalInternalStatus':'The preserved binary predates approval; this manifest records the subsequent user approval.',
    'doNotOverwriteForIdentityChanges':True}
(ROOT.parent/'Foundations/male-head-v1.json').write_text(json.dumps(foundation,indent=2)+'\n')

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SS_MaleBody_Foundation_Candidate.blend'))
renames={'SS_Head_Male_Candidate':'SS_MaleHead_Foundation_v1'}
head_checks={n:signature(bpy.data.objects[renames.get(n,n)])==s for n,s in parts.items()}
body=bpy.data.objects['SS_MaleBody_Foundation_Candidate'];m=body.data
head=bpy.data.objects['SS_MaleHead_Foundation_v1']
bm=bmesh.new();bm.from_mesh(m);bm.verts.ensure_lookup_table()
boundary={v.index for e in bm.edges if e.is_boundary for v in e.verts}
body_ids=[x.value for x in m.attributes['CC_source_vertex'].data]
head_ids={x.value:i for i,x in enumerate(head.data.attributes['CC_source_vertex'].data) if x.value>=0}
seam_error=max((m.vertices[i].co-head.data.vertices[head_ids[body_ids[i]]].co).length for i in boundary)
source=json.loads((ROOT/'source-body-topology.json').read_text())
topology_equal=[list(p.vertices) for p in m.polygons]==source['faces']
uv_equal=all(max(abs(float(m.uv_layers[0].data[li].uv[k])-uv[k]) for k in (0,1))<1e-7
    for p,coords in zip(m.polygons,source['uv']) for li,uv in zip(p.loop_indices,coords))
m.calc_loop_triangles()
tri=[tuple(t.vertices) for t in m.loop_triangles]
bvh=BVHTree.FromPolygons([tuple(v.co) for v in m.vertices],tri,all_triangles=True)
overlap=sorted({tuple(sorted((a,b))) for a,b in bvh.overlap(bvh) if a!=b and not set(tri[a]).intersection(tri[b])})
intersect_faces=sorted({m.loop_triangles[i].polygon_index for pair in overlap for i in pair})
def overlap_categories(vertices):
    tree=BVHTree.FromPolygons(vertices,tri,all_triangles=True)
    pairs={tuple(sorted((a,b))) for a,b in tree.overlap(tree) if a!=b and not set(tri[a]).intersection(tri[b])}
    result={'skinSkin':0,'nailSkin':0,'nailNail':0}
    for a,b in pairs:
        nail_count=sum(source['faceRegions'][m.loop_triangles[i].polygon_index]=='Std_Nails' for i in (a,b))
        result[['skinSkin','nailSkin','nailNail'][nail_count]]+=1
    return result,pairs
source_points=np.load(ROOT/'source-body-data.npz')['coordinates']
source_overlap,source_pairs=overlap_categories(source_points.tolist())
candidate_overlap,candidate_pairs=overlap_categories([tuple(v.co) for v in m.vertices])

# Dry-run weld of the anatomical shell. The saved editable head/body stay separate.
coords=[tuple(v.co) for v in head.data.vertices];faces=[list(p.vertices) for p in head.data.polygons]
remap={}
for i,v in enumerate(m.vertices):
    old=body_ids[i]
    if old in head_ids:remap[i]=head_ids[old]
    else:remap[i]=len(coords);coords.append(tuple(v.co))
faces.extend([[remap[i] for i in p.vertices] for p in m.polygons])
temp=bpy.data.meshes.new('Validation only');temp.from_pydata(coords,[],faces);temp.update()
whole=bmesh.new();whole.from_mesh(temp)
whole_boundary=sum(e.is_boundary for e in whole.edges)
whole_nonmanifold=sum(len(e.link_faces)!=2 for e in whole.edges)
whole_volume=whole.calc_volume(signed=True)
whole.free();bpy.data.meshes.remove(temp)

preserved={
    'SS_Adult_Authoring.blend':'23aab3cd859f9afccdb8c7731e33e83046542232c3ad6c1612cd51ae04e5cbd1',
    'rig-contract.json':'62e401ed06f554d13deff8c407c03ea5a8d173feb4b5067be124021a318912e4',
    'SS_Adult_Accessories.blend':'4e50aa2cfd4c22e1764e6d3af950973fe339d4fb390bf38303c1125afa629bdd'}
preservation={name:sha(ROOT.parent/name)==expected for name,expected in preserved.items()}
preservation['Studio.unity']=sha(REPO/'Assets/Scenes/Studio.unity')=='1ec4bb0925316abd4c02d82f374badb8c993482d9c9d37eff5463d169343e1d6'
report={'candidateSHA256':sha(ROOT/'SS_MaleBody_Foundation_Candidate.blend'),
    'approvedHeadModuleGeometryTopologyUVUnchanged':head_checks,
    'bodyTopologyAndVertexOrderPreserved':topology_equal,'bodySourceUVCoordinatesPreserved':uv_equal,
    'bodySourceIdCorrespondencePreserved':body_ids==source['sourceVertexIds'],
    'bodyVertexCount':len(m.vertices),'bodyFaceCount':len(m.polygons),'bodyQuads':sum(len(p.vertices)==4 for p in m.polygons),
    'neckBoundaryVertices':len(boundary),'neckSeamErrorMetres':seam_error,
    'anatomicalShellBoundaryEdgesAfterDryRunSeamWeld':whole_boundary,
    'anatomicalShellNonManifoldEdgesAfterDryRunSeamWeld':whole_nonmanifold,
    'anatomicalShellSignedVolumeCubicMetres':whole_volume,
    'nonAdjacentTriangleOverlapPairs':len(overlap),'overlappingCageFaces':intersect_faces,
    'sourceCageOverlapCategories':source_overlap,'candidateCageOverlapCategories':candidate_overlap,
    'overlapTrianglePairsIdenticalToSource':source_pairs==candidate_pairs,
    'degenerateBodyFaces':sum(f.calc_area()<1e-12 for f in bm.faces),
    'bodyVertexGroups':len(body.vertex_groups),'armatureObjects':sum(o.type=='ARMATURE' for o in bpy.data.objects),
    'bodyShapeKeys':0 if not m.shape_keys else len(m.shape_keys.key_blocks),
    'preservedFilesUnchanged':preservation,
    'deformationValidated':False,'subdivisionSurfaceIntersectionTested':False,
    'UVRelaxationOrFinalTextureDensityValidated':False,'UnityImported':False,
    'visualApproval':'Pending user review; structural checks do not establish visual approval'}
bm.free()
(ROOT/'validation-report.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
for material in body.data.materials:
    if material.use_nodes:
        n=material.node_tree.nodes.get('Principled BSDF')
        if n:print('REVIEW MATERIAL',material.name,tuple(n.inputs['Base Color'].default_value))
assert all(head_checks.values())
assert topology_equal and uv_equal and body_ids==source['sourceVertexIds']
assert seam_error<1e-7 and whole_boundary==0 and whole_nonmanifold==0
assert candidate_overlap['skinSkin']==0
assert all(preservation.values())
