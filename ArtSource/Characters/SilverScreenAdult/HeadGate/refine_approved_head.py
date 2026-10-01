"""Focused identity pass on the actual user-approved technical foundation.

Called by build_head.py. Preserves every polygon, source index, UV, module and
neutral review setting. Adds no body, rig, performance controls or final materials.
"""
import bpy
import bmesh
import numpy as np
import hashlib
import json
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parent
if (ROOT.parent/'Foundations/SS_MaleHead_Foundation_v1.blend').exists():
    raise SystemExit('Head foundation is approved and frozen. This historical sculpt pass must not overwrite it. Use separate identity targets.')
REPO=ROOT.parents[3]
OUT=REPO/'ArtReview/Characters/SilverScreenAdult/HeadGate'
BASE=ROOT/'ApprovedTechnicalFoundation/SS_Head_Candidate.blend'
EXPECTED_BASE='9719fd6a564e5a7833fa0d85abbca0e6574b0ffe89afa6d7619749b669e88fb9'
assert hashlib.sha256(BASE.read_bytes()).hexdigest()==EXPECTED_BASE,'Approved foundation changed; reconcile it before rebuilding.'
bpy.ops.wm.open_mainfile(filepath=str(BASE))

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)

def refine(points):
    p=np.asarray(points,dtype=float);q=p.copy()
    x,y,z=p.T;a=np.abs(x);s=np.sign(x)
    def g(cx,cy,cz,sx,sy,sz):
        return np.exp(-.5*(((a-cx)/sx)**2+((y-cy)/sy)**2+((z-cz)/sz)**2))
    # Broader parietal vault, a slightly lower crown, and fuller upper temples.
    q[:,0]+=s*(.0031*g(.061,.032,1.737,.021,.090,.038)
                 +.0010*g(.073,-.003,1.707,.014,.047,.027))
    q[:,2]-=.0022*smooth(1.709,1.777,z)
    # A clearer orbital roof, with a restrained medial brow/glabellar mass.
    q[:,1]-=.0015*g(.036,-.061,1.687,.024,.020,.007)
    q[:,1]-=.0009*g(.011,-.073,1.687,.012,.021,.012)
    q[:,2]-=.00035*g(.011,-.069,1.685,.012,.020,.009)
    # Open the eye shape mainly through upper-lid contour, not globe scaling.
    q[:,2]+=.00125*g(.034,-.054,1.675,.018,.012,.0045)
    q[:,2]-=.00030*g(.036,-.051,1.658,.018,.011,.0040)
    q[:,0]+=s*.00055*g(.047,-.046,1.668,.008,.017,.009)
    q[:,2]+=.00030*g(.050,-.045,1.669,.008,.016,.006)
    # A stronger, coherent nasal bridge/tip/alar complex.
    q[:,1]-=.00125*g(.0,-.083,1.652,.011,.019,.019)
    q[:,0]+=s*.00050*g(.010,-.077,1.644,.009,.020,.021)
    q[:,1]-=.00080*g(.0,-.094,1.627,.013,.012,.009)
    q[:,2]-=.00040*g(.0,-.087,1.626,.017,.012,.011)
    q[:,0]+=s*.00065*g(.017,-.078,1.623,.008,.015,.009)
    # Zygomatic plane supported by a mild submalar transition.
    q[:,0]+=s*.0016*g(.056,-.031,1.639,.015,.027,.010)
    q[:,1]-=.0013*g(.049,-.045,1.638,.018,.021,.009)
    q[:,1]+=.00085*g(.051,-.029,1.612,.012,.025,.013)
    q[:,0]-=s*.00045*g(.055,-.010,1.611,.011,.025,.012)
    # Distinct mandibular angle; avoid simply widening the entire lower face.
    angle=g(.065,.001,1.588,.015,.029,.015)
    q[:,0]+=s*.0022*angle
    q[:,2]-=.0011*angle
    q[:,0]+=s*.0015*g(.024,-.059,1.559,.018,.026,.010)
    q[:,1]-=.0017*g(.012,-.075,1.560,.027,.024,.009)
    # Readable upper/lower lip volumes and a subtle confident corner rhythm.
    q[:,1]-=.00065*g(.009,-.075,1.601,.014,.011,.004)
    q[:,1]-=.00060*g(.007,-.074,1.584,.019,.012,.0045)
    q[:,1]+=.00035*g(.0,-.071,1.607,.023,.012,.003)
    q[:,1]+=.00040*g(.0,-.069,1.578,.021,.013,.003)
    q[:,0]+=s*.00045*g(.027,-.064,1.594,.011,.016,.009)
    q[:,2]+=.00035*g(.028,-.066,1.594,.010,.014,.006)
    # Support the slightly stronger head with restrained upper-neck volume.
    q[:,0]+=s*.0013*g(.037,.015,1.529,.017,.051,.024)
    q[:,1]-=.0006*g(.027,-.023,1.531,.014,.020,.028)
    return q

def mesh_signature(o):
    payload={'faces':[list(f.vertices) for f in o.data.polygons],
        'uv':[[list(d.uv) for d in l.data] for l in o.data.uv_layers],
        'sourceIds':[d.value for d in o.data.attributes['CC_source_vertex'].data]}
    return hashlib.sha256(json.dumps(payload,separators=(',',':')).encode()).hexdigest()

head=bpy.data.objects['SS_Head_Male_Candidate']
parts=[bpy.data.objects[n] for n in ['SS_Head_Male_Candidate','SS_Eye_L','SS_Eye_R','SS_Teeth','SS_Tongue']]
signatures={o.name:mesh_signature(o) for o in parts}
changes={};base_head=np.array([tuple(v.co) for v in head.data.vertices])
for o in parts:
    p=np.array([tuple(v.co) for v in o.data.vertices]);q=p.copy()
    # The existing independent globes remain rigid and in the same gaze pose.
    if o.name not in ('SS_Eye_L','SS_Eye_R'):
        q=refine(p)
        if o==head:
            bm=bmesh.new();bm.from_mesh(o.data)
            seam=[v.index for e in bm.edges if e.is_boundary for v in e.verts];bm.free()
            q[seam]=p[seam]  # retain the approved open module boundary exactly
        for v,co in zip(o.data.vertices,q):v.co=co
        o.data.update()
    delta=q-p;changes[o.name]=delta
    o['identity_revision']='02 - focused leading-man sculpt'
    o['foundation_approval']='User approved technical foundation; revised art direction pending review'
    o['gate_status']='IDENTITY_PASS_02_REVIEW'
    assert signatures[o.name]==mesh_signature(o),o.name+' topology/UV correspondence changed'

# Refit the same removable eyebrow strands to the moved brow surface.
bpy.context.view_layer.update()
bvh=BVHTree.FromObject(head,bpy.context.evaluated_depsgraph_get())
brows=bpy.data.objects['SS_Brow_Study_Removable']
for spline in brows.data.splines:
    for point in spline.points:
        p=np.array(point.co[:3]);q=refine(p[None,:])[0]
        hit,normal,_,_=bvh.ray_cast(Vector((q[0],-.25,q[2])),Vector((0,1,0)))
        if hit is not None:q=hit+normal*.00025
        point.co=(*q,1)

scene=bpy.context.scene
scene['identity_revision']='02'
scene['foundation_approval']='Technical foundation approved by user; identity pass 02 pending visual approval'
scene['acceptance']='STOP at head review; body and wardrobe remain gated'
scene['foundation_sha256']=EXPECTED_BASE
cam=scene.camera;target=Vector((0,.016,1.647))
def camera(delta,scale=.303,focus=target):
    cam.data.ortho_scale=scale;cam.location=focus+Vector(delta)
    cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler()
camera((.7,-2,.01))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_Head_Candidate.blend'))
np.savez_compressed(ROOT/'identity-pass-02-deltas.npz',**changes)

def inspect(o):
    bm=bmesh.new();bm.from_mesh(o.data)
    result={'name':o.name,'vertices':len(bm.verts),'faces':len(bm.faces),
        'quads':sum(len(f.verts)==4 for f in bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),
        'edgesWithMoreThanTwoFaces':sum(len(e.link_faces)>2 for e in bm.edges),
        'degenerateFaces':sum(f.calc_area()<1e-12 for f in bm.faces),
        'topologyUVAndSourceIdsUnchanged':signatures[o.name]==mesh_signature(o),
        'meanDisplacementMillimetres':float(np.linalg.norm(changes[o.name],axis=1).mean()*1000),
        'maxDisplacementMillimetres':float(np.linalg.norm(changes[o.name],axis=1).max()*1000)}
    bm.free();return result
report={'status':'IDENTITY_PASS_02_REVIEW','technicalFoundationApproved':True,'artDirectionApproved':False,
    'foundationSHA256':EXPECTED_BASE,'parts':[inspect(o) for o in parts],
    'lightingAndCameraContract':'Identical to the approved technical-foundation review',
    'rigContractUnchanged':hashlib.sha256((ROOT.parent/'rig-contract.json').read_bytes()).hexdigest()=='62e401ed06f554d13deff8c407c03ea5a8d173feb4b5067be124021a318912e4',
    'bodyAuthored':False,'rigged':False,'deformationValidated':False,
    'shapeKeyAudit':'Existing 148-shape audit preserved; no expression keys introduced',
    'renderViews':['front','threequarter','profile','closeup','clay-front','topology']}
assert all(p['degenerateFaces']==0 and p['edgesWithMoreThanTwoFaces']==0 for p in report['parts'])
(ROOT/'head-report.json').write_text(json.dumps(report,indent=2)+'\n')
shots=[('front',(0,-2,0),.303,target),('threequarter',(1.15,-2,.015),.303,target),
       ('profile',(2,0,0),.303,target),('closeup',(.52,-2,.005),.245,Vector((0,-.013,1.651)))]
if '--quick' in sys.argv:shots=shots[:3];scene.cycles.samples=24;scene.render.resolution_percentage=75
for name,delta,scale,focus in shots:
    camera(delta,scale,focus);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
if '--quick' not in sys.argv:
    brows.hide_render=True
    camera((0,-2,0));scene.render.filepath=str(OUT/'clay-front.png');bpy.ops.render.render(write_still=True)
    overlay=head.copy();overlay.data=head.data.copy();scene.collection.objects.link(overlay)
    for m in list(overlay.modifiers):overlay.modifiers.remove(m)
    wiremat=bpy.data.materials.get('Review only - topology')
    if wiremat is None:
        wiremat=bpy.data.materials.new('Review only - topology');wiremat.use_nodes=True
        wiremat.diffuse_color=(.014,.014,.014,1)
        wiremat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.014,.014,.014,1)
        wiremat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.6
    overlay.data.materials.clear();overlay.data.materials.append(wiremat)
    w=overlay.modifiers.new('Cage wire - review only','WIREFRAME');w.thickness=.00022;w.offset=1
    head.modifiers[0].show_render=False
    camera((.55,-2,.005));scene.render.filepath=str(OUT/'topology.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))
