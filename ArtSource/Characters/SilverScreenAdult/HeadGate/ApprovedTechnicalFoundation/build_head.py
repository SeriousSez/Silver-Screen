"""Head-only identity sculpt on the specifically authorized CC topology.

Landmark deformation fields preserve the supplied edge layout and UV correspondence.
All outputs are review candidates. No source skeleton, skin weights, hairstyles,
body or garments are copied into this head workspace.
"""
import bpy
import bmesh
import numpy as np
import json
import math
import random
import hashlib
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[3]
OUT=REPO/'ArtReview/Characters/SilverScreenAdult/HeadGate'
OUT.mkdir(parents=True,exist_ok=True)
SOURCE=np.load(ROOT/'source-head-coordinates.npz')
bpy.ops.wm.read_factory_settings(use_empty=True)

def smoothstep(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)

def sculpt(points):
    """All fields are specified in source metres, before adult cranial placement."""
    p=np.asarray(points,dtype=float);q=p.copy()
    x,y,z=p.T;a=np.abs(x);side=np.sign(x)
    front=1-smoothstep(.005,.070,y)
    def g(cx,cy,cz,sx,sy,sz):
        return np.exp(-.5*(((a-cx)/sx)**2+((y-cy)/sy)**2+((z-cz)/sz)**2))
    # Cranial proportions: adult male vault and flatter temporal silhouette.
    q[:,0]*=1.045
    q[:,1]+=.0015*smoothstep(1.64,1.70,z)*front
    q[:,0]-=side*.0023*g(.074,.010,1.649,.015,.047,.025)
    q[:,1]-=.0030*g(.0,.111,1.658,.075,.033,.046)
    # Substantial mandibular remodeling, including the ramus and mental eminence.
    q[:,0]+=side*(.0090*g(.049,-.005,1.533,.026,.054,.022)
                       +.0050*g(.022,-.053,1.511,.019,.028,.014))
    q[:,1]-=.0055*g(.015,-.064,1.510,.030,.034,.016)
    q[:,2]-=.0040*g(.018,-.046,1.505,.039,.044,.015)
    # Defined zygomatic plane with lean buccal volume below it.
    q[:,0]+=side*.0032*g(.051,-.031,1.583,.017,.029,.013)
    q[:,1]-=.0018*g(.046,-.048,1.585,.018,.020,.012)
    q[:,0]-=side*.0018*g(.050,-.017,1.554,.014,.030,.017)
    q[:,1]+=.0022*g(.047,-.043,1.559,.016,.023,.016)
    # Less domed forehead and a readable supraorbital/glabellar structure.
    q[:,1]-=.0045*g(.032,-.059,1.625,.024,.025,.010)
    q[:,1]-=.0025*g(.009,-.065,1.621,.012,.020,.016)
    q[:,1]+=.00055*g(.0045,-.067,1.634,.0022,.018,.008)
    q[:,1]-=.00060*g(.010,-.064,1.633,.004,.018,.010)
    # Sharpen the orbital roof/temple transition and the supraorbital ledge.
    q[:,1]-=.0030*g(.034,-.055,1.623,.023,.019,.0055)
    q[:,2]-=.0013*g(.035,-.056,1.623,.025,.018,.008)
    # Smaller/deeper orbits and narrowed lid aperture, with shared eye placement.
    for s in (-1,1):
        cx=s*.035;cy=-.035;cz=1.6082
        d=p-np.array([cx,cy,cz])
        norm=(d[:,0]/.032)**4+(d[:,1]/.049)**4+(d[:,2]/.031)**4
        w=np.exp(-1.1*norm)
        q[:,0]+=w*(-.17*d[:,0]-s*.002)
        q[:,1]+=w*(-.21*d[:,1]+.0005)
        q[:,2]+=w*(-.23*d[:,2])
    # Hooded upper lids and a quieter lower-lid arc, without flattening the rim.
    q[:,2]-=.0016*g(.035,-.053,1.619,.018,.011,.0045)
    q[:,2]+=.0005*g(.035,-.052,1.596,.019,.012,.004)
    q[:,1]+=.0009*g(.033,-.045,1.625,.021,.017,.0035)
    q[:,1]-=.0017*g(.036,-.047,1.592,.020,.016,.004)
    troughz=1.589-.20*(a-.022)
    trough=np.exp(-.5*((z-troughz)/.0025)**2)*np.exp(-.5*((a-.029)/.017)**2)*np.exp(-.5*((y+.043)/.020)**2)
    q[:,1]+=.0006*trough
    # Broader nasal bridge, less upturned tip, defined alar volume.
    q[:,0]+=side*.0020*g(.009,-.076,1.589,.011,.023,.025)
    q[:,0]+=side*.0012*g(.015,-.074,1.563,.010,.013,.010)
    q[:,1]-=.0048*g(.0,-.079,1.590,.012,.020,.022)
    q[:,1]-=.0023*g(.0,-.087,1.572,.014,.010,.009)
    q[:,2]-=.0030*g(.0,-.080,1.568,.019,.016,.016)
    q[:,1]-=.0016*g(.0,-.073,1.602,.008,.020,.014)
    q[:,1]+=.0008*g(.010,-.084,1.571,.004,.012,.007)
    # Wider oral aperture with restrained vermilion, preserving the mouth bag.
    mouth=g(.0,-.065,1.539,.039,.030,.017)
    q[:,0]+=x*.085*mouth
    q[:,1]+=.0015*g(.0,-.076,1.535,.024,.012,.010)
    q[:,2]+=(1.539-z)*.12*g(.0,-.073,1.539,.028,.016,.010)
    # Separate the philtrum, perioral pad, labiomental fold and chin planes.
    q[:,1]+=.00065*g(.0,-.075,1.551,.003,.012,.006)
    q[:,1]-=.00065*g(.006,-.074,1.551,.003,.012,.006)
    q[:,1]+=.0013*g(.0,-.067,1.524,.023,.015,.004)
    q[:,1]-=.0014*g(.013,-.065,1.512,.020,.019,.006)
    q[:,1]+=.00055*g(.0,-.071,1.511,.0045,.013,.006)
    # Soft anatomical nasolabial transition rather than a painted crease.
    foldx=.017+(1.560-z)*.46
    fold=np.exp(-.5*((a-foldx)/.0035)**2)*np.exp(-.5*((z-1.548)/.012)**2)*np.exp(-.5*((y+.065)/.018)**2)
    q[:,1]+=.00085*fold
    # Lean cheek planes: retain zygomatic volume while defining the hollow below.
    q[:,0]-=side*.0014*g(.048,-.026,1.560,.012,.027,.014)
    q[:,1]+=.0017*g(.047,-.033,1.560,.012,.020,.014)
    # Natural neck volume under the jaw, with distinct sternomastoid planes.
    neck=1-smoothstep(1.491,1.526,z)
    q[:,0]+=side*.0085*(1-np.exp(-(a/.018)**2))*neck
    q[:,1]-=.0038*g(.022,-.023,1.475,.021,.019,.045)
    q[:,1]-=.0020*g(.0,-.031,1.481,.013,.014,.012)
    # Original ear cartilage is retained, mildly reshaped at the helix/lobe.
    ear=smoothstep(.070,.080,a)*(1-smoothstep(1.621,1.641,z))
    q[:,0]+=side*.0012*ear
    q[:,2]+=(z-1.584)*.05*ear
    # Restrained resting asymmetry is part of this identity, not an expression.
    q[:,0]+=.00045*g(.0,-.079,1.589,.018,.022,.023)
    q[:,2]+=.00045*(x>0)*g(.023,-.068,1.540,.014,.017,.008)
    # Metric placement near the existing adult rig's planned head landmarks.
    q[:,2]=1.668+(q[:,2]-1.6082)*1.08
    return q

def mat(name,col,rough=.6):
    m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*col,1)
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Specular IOR Level'].default_value=.28
    return m

clay=mat('Review only - neutral clay',(.24,.24,.24),.68)
oral=mat('Review only - oral cavity',(.095,.095,.095))
tooth=mat('Review only - teeth',(.55,.55,.55))
white=mat('Review only - neutral eyes',(.55,.55,.55),.35)
wiremat=mat('Review only - topology',(.014,.014,.014))

def mesh_part(name,part,selection=None,material=clay):
    data=json.loads((ROOT/(part.lower()+'-topology.json')).read_text())
    coords=SOURCE[part];faces=data['faces'];uv=data['uv']
    if selection is not None:
        include=[i for i,f in enumerate(faces) if all(selection[j] for j in f)]
    else:include=list(range(len(faces)))
    ids=sorted({j for i in include for j in faces[i]});ix={v:i for i,v in enumerate(ids)}
    target=sculpt(coords[ids])
    if part=='Eyes':
        # Rigid spherical rest geometry: fit the eyes independently of skin fields.
        s=1 if coords[ids,0].mean()>0 else -1
        center=np.array([s*.035,-.035,1.6082])
        newcenter=sculpt(center[None,:])[0]
        target=(coords[ids]-center)*.79+newcenter
    mesh=bpy.data.meshes.new(name+'_Topology')
    mesh.from_pydata(target.tolist(),[],[[ix[j] for j in faces[i]] for i in include]);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(material)
    layer=mesh.uv_layers.new(name='SourceUV_Correspondence')
    attr=mesh.attributes.new('CC_source_vertex','INT','POINT')
    for new,old in enumerate(ids):attr.data[new].value=data['sourceVertexIds'][old]
    for face,old in zip(mesh.polygons,include):
        face.use_smooth=True
        for li,uvco in zip(face.loop_indices,uv[old]):layer.data[li].uv=uvco
    obj['provenance']='User-authorized CC topology; SilverScreen head identity sculpt'
    obj['gate_status']='UNAPPROVED_HEAD_REVIEW'
    obj['rig_contract']='SilverScreen adult 91-bone scaffold; not bound at this gate'
    obj['source_part']=part
    sub=obj.modifiers.new('Sculpt surface - editable cage remains intact','SUBSURF');sub.levels=1;sub.render_levels=2
    return obj,ids,target

head,headids,headtarget=mesh_part('SS_Head_Male_Candidate','Head')
# The supplied skin leaves a small opening at each lacrimal caruncle (its
# original tear/occlusion overlay hid this). Close those actual geometry holes
# with a shallow quad patch. The open 40-edge neck seam remains intentional.
def close_caruncles(obj):
    bm=bmesh.new();bm.from_mesh(obj.data)
    unseen={v for e in bm.edges if e.is_boundary for v in e.verts}
    loops=[]
    while unseen:
        first=unseen.pop();loop=[first];prev=None;cur=first
        while True:
            choices=[e.other_vert(cur) for e in cur.link_edges if e.is_boundary and e.other_vert(cur)!=prev]
            nxt=next((v for v in choices if v in unseen),None)
            if nxt is None:break
            unseen.remove(nxt);loop.append(nxt);prev,cur=cur,nxt
        loops.append(loop)
    for loop in loops:
        if len(loop)!=10:continue
        center=sum((v.co for v in loop),Vector())/len(loop)
        uvlayer=bm.loops.layers.uv.active
        original_uv={v:next(l[uvlayer].uv.copy() for l in v.link_loops) for v in loop}
        uvcenter=sum(original_uv.values(),Vector((0,0)))/len(loop)
        inside=[bm.verts.new(center+(v.co-center)*.43+Vector((0,-.00028,0))) for v in loop]
        provenance=bm.verts.layers.int.get('CC_source_vertex')
        patch_uv=dict(original_uv)
        for v,old in zip(inside,loop):
            if provenance is not None:v[provenance]=-1
            patch_uv[v]=uvcenter+(original_uv[old]-uvcenter)*.43
        added=[]
        for i in range(10):added.append(bm.faces.new((loop[i],loop[(i+1)%10],inside[(i+1)%10],inside[i])))
        for i in (1,3,5,7):added.append(bm.faces.new((inside[0],inside[i],inside[i+1],inside[(i+2)%10])))
        for face in added:
            for l in face.loops:l[uvlayer].uv=patch_uv[l.vert]
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    for f in obj.data.polygons:f.use_smooth=True
close_caruncles(head)
eyeL,_,_=mesh_part('SS_Eye_L','Eyes',SOURCE['Eyes'][:,0]>0,white)
eyeR,_,_=mesh_part('SS_Eye_R','Eyes',SOURCE['Eyes'][:,0]<0,white)
teeth,_,_=mesh_part('SS_Teeth','Teeth',material=tooth)
tongue,_,_=mesh_part('SS_Tongue','Tongue',material=oral)

# Independent, neutral grey iris/pupil preview. No source identity texture and
# no pigmentation system is implemented by this temporary inspection shader.
def eye_shader(obj):
    p=np.array([tuple(v.co) for v in obj.data.vertices]);center=(p.min(0)+p.max(0))/2
    m=white.copy();m.name='Review eye landmarks - '+obj.name
    nodes=m.node_tree.nodes;links=m.node_tree.links
    geo=nodes.new('ShaderNodeNewGeometry');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(geo.outputs['Position'],sep.inputs[0])
    def mathnode(op,a,b):
        n=nodes.new('ShaderNodeMath');n.operation=op
        for i,v in enumerate((a,b)):
            if isinstance(v,(float,int)):n.inputs[i].default_value=v
            else:links.new(v,n.inputs[i])
        return n.outputs[0]
    dx=mathnode('SUBTRACT',sep.outputs['X'],float(center[0]));dz=mathnode('SUBTRACT',sep.outputs['Z'],float(center[2]))
    dist=mathnode('SQRT',mathnode('ADD',mathnode('MULTIPLY',dx,dx),mathnode('MULTIPLY',dz,dz)),0)
    iris=mathnode('LESS_THAN',dist,.0057);pupil=mathnode('LESS_THAN',dist,.0023)
    mix=nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.58,.58,.58,1);mix.inputs[2].default_value=(.13,.13,.13,1);links.new(iris,mix.inputs[0])
    mix2=nodes.new('ShaderNodeMixRGB');links.new(mix.outputs[0],mix2.inputs[1]);mix2.inputs[2].default_value=(.015,.015,.015,1);links.new(pupil,mix2.inputs[0])
    links.new(mix2.outputs[0],nodes.get('Principled BSDF').inputs['Base Color'])
    obj.data.materials.clear();obj.data.materials.append(m)
for eye in (eyeL,eyeR):eye_shader(eye)

# A separate, removable neutral brow study clarifies the concept's horizontal
# brow rhythm. It is newly laid out on this sculpt, not source hairstyle data.
browmat=mat('Review only - graphite brow study',(.055,.055,.055),.8)
def brow_study():
    deps=bpy.context.evaluated_depsgraph_get();bvh=BVHTree.FromObject(head,deps)
    rng=random.Random(1930)
    curve=bpy.data.curves.new('Brow study strands','CURVE');curve.dimensions='3D'
    curve.resolution_u=2;curve.bevel_depth=.00012;curve.bevel_resolution=1
    for side in (-1,1):
        for i in range(720):
            t=rng.random();x=.013+.047*t
            arch=1.6825+.0023*math.sin(t*math.pi)-.0025*t+(.00035 if side>0 else 0)
            thickness=.0057*(1-t)**.5+.0005
            z=arch+(rng.random()-.5)*thickness
            length=.0021+rng.random()*.0018
            pts=[]
            for k in range(3):
                u=k/2;xx=side*(x+length*.74*u);zz=z+length*(.7-.5*t)*u
                hit,normal,_,_=bvh.ray_cast(Vector((xx,-.25,zz)),Vector((0,1,0)))
                if hit is not None:pts.append(hit+normal*(.00018+.00010*math.sin(u*math.pi)))
            if len(pts)==3:
                spline=curve.splines.new('POLY');spline.points.add(2)
                for k,p in enumerate(pts):spline.points[k].co=(*p,1);spline.points[k].radius=(.65,1,.12)[k]
    obj=bpy.data.objects.new('SS_Brow_Study_Removable',curve);bpy.context.scene.collection.objects.link(obj);curve.materials.append(browmat)
    obj['status']='Removable brow layout study for head review; not a production hairstyle'
    return obj
brows=brow_study()

scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene['scope']='HEAD ONLY - no body authoring, skinning, animation or Unity integration'
scene['acceptance']='Pending user visual review against ApprovedConcept.png'
scene.world=bpy.data.worlds.new('Neutral review world');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.29,.29,.29,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
target=Vector((0,.016,1.647))
for name,delta,power,size in [('Neutral key',(-.8,-1.2,1),95,.85),('Neutral fill',(.9,-.5,.35),18,1.2),('Neutral rear',(0,1,.65),45,1.0)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=target+Vector(delta);o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('HeadReview');cam=bpy.data.objects.new('HeadReview',camdata);scene.collection.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=.303
scene.render.engine='CYCLES';scene.cycles.samples=40
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.35
scene.render.film_transparent=False
scene.render.image_settings.color_mode='RGBA'
scene.render.use_file_extension=True

def camera(delta,scale=.303,focus=target):
    camdata.ortho_scale=scale;cam.location=focus+Vector(delta);cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler()

# Save the actual editable 3D result before rendering.
camera((.7,-2,.01))
bpy.context.view_layer.objects.active=head;head.select_set(True)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.type='MATERIAL'
scene['source_sha256']=json.loads((ROOT/'shape-key-audit.json').read_text())['sourceSHA256']
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_Head_Candidate.blend'))

def inspect(obj):
    bm=bmesh.new();bm.from_mesh(obj.data)
    result={'name':obj.name,'vertices':len(bm.verts),'faces':len(bm.faces),'quads':sum(len(f.verts)==4 for f in bm.faces),
        'boundaryEdges':sum(e.is_boundary for e in bm.edges),'edgesWithMoreThanTwoFaces':sum(len(e.link_faces)>2 for e in bm.edges),
        'degenerateFaces':sum(f.calc_area()<1e-12 for f in bm.faces),'uvLayers':len(obj.data.uv_layers),'shapeKeys':0,
        'armatureModifiers':sum(m.type=='ARMATURE' for m in obj.modifiers)}
    bm.free();return result
report={'status':'UNAPPROVED_HEAD_REVIEW','source':'CC topology explicitly authorized by user',
    'rigContractUnchanged':hashlib.sha256((ROOT.parent/'rig-contract.json').read_bytes()).hexdigest()=='62e401ed06f554d13deff8c407c03ea5a8d173feb4b5067be124021a318912e4',
    'preservationEvidence':'preservation-check.json records the gate baseline comparison for rig scaffold, rig contract, accessories, Studio scene file and supplied FBX',
    'lacrimalPatchNewVertices':20,
    'renderViews':['front','threequarter','profile','closeup','clay-front','topology'],
    'visualReview':'Pending user acceptance; structural mesh checks do not establish likeness, deformation or runtime quality.',
    'shapeKeysCarriedIntoCandidate':0,'identityAndExpressionSeparate':True,
    'bodyAuthored':False,'rigged':False,'deformationValidated':False,'headDisplacementFromSourceMetres':{
        'mean':float(np.linalg.norm(headtarget-SOURCE['Head'],axis=1).mean()),
        'note':'Includes metric placement near the planned adult head landmarks; not a measure of artistic difference.'},
    'parts':[inspect(o) for o in (head,eyeL,eyeR,teeth,tongue)]}
(ROOT/'head-report.json').write_text(json.dumps(report,indent=2)+'\n')

shots=[('front',(0,-2,0),.303,target),('threequarter',(1.15,-2,.015),.303,target),
       ('profile',(2,0,0),.303,target),('closeup',(.52,-2,.005),.245,Vector((0,-.013,1.651)))]
if '--quick' in sys.argv:shots=shots[:2];scene.cycles.samples=24;scene.render.resolution_percentage=75
for name,delta,scale,focus in shots:
    camera(delta,scale,focus);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
if '--quick' not in sys.argv:
    brows.hide_render=True
    camera((0,-2,0),.303,target);scene.render.filepath=str(OUT/'clay-front.png');bpy.ops.render.render(write_still=True)
    overlay=head.copy();overlay.data=head.data.copy();scene.collection.objects.link(overlay)
    for mod in list(overlay.modifiers):overlay.modifiers.remove(mod)
    overlay.data.materials.clear();overlay.data.materials.append(wiremat)
    w=overlay.modifiers.new('Cage wire - review only','WIREFRAME');w.thickness=.00022;w.offset=1
    # Use the cage surface too so all actual cage edges remain visible.
    head.modifiers[0].show_render=False
    camera((.55,-2,.005),.303,target)
    scene.render.filepath=str(OUT/'topology.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))
