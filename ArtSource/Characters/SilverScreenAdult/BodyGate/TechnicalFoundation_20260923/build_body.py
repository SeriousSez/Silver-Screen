"""Author a neutral-fit male anatomical foundation from authorized CC topology.

The source groups are static regional selectors only. No skeleton, binding,
animation, clothing or texture identity is copied into this body gate.
"""
import bpy
import bmesh
import numpy as np
import json
import hashlib
import sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent;REPO=ROOT.parents[3]
OUT=REPO/'ArtReview/Characters/SilverScreenAdult/BodyGate';OUT.mkdir(parents=True,exist_ok=True)
HEAD=ROOT.parent/'Foundations/SS_MaleHead_Foundation_v1.blend'
HEAD_SHA='ad692545e7b10451a5856c78b33695bf33a4f522c0e14cc6b29c292615a48eaf'
assert hashlib.sha256(HEAD.read_bytes()).hexdigest()==HEAD_SHA
D=json.loads((ROOT/'source-body-topology.json').read_text());N=np.load(ROOT/'source-body-data.npz')
P=N['coordinates'];W=N['regionSelectors'];GROUPS=D['sourceRegionGroupNames']
BONES={b['name']:np.array(b['head']) for b in D['sourceBones']}
bpy.ops.wm.open_mainfile(filepath=str(HEAD))
head=bpy.data.objects['SS_Head_Male_Candidate'];head.name='SS_MaleHead_Foundation_v1'
head_before=np.array([tuple(v.co) for v in head.data.vertices])
head['foundation_approval']='User approved male anatomical foundation; neutral geometry frozen'
head['gate_status']='APPROVED_HEAD_FOUNDATION'
head['identity_revision']='Foundation v1; future character identity targets are separate'
for o in list(bpy.data.objects):
    if o.type in ('LIGHT','CAMERA'):bpy.data.objects.remove(o,do_unlink=True)

def smooth(a,b,x):
    u=np.clip((x-a)/(b-a),0,1);return u*u*(3-2*u)
def interp(x,knots,values):return np.interp(x,knots,values)
def height_map(z):
    return interp(z,[.0229666,.0753,.5714,.84,.9876,1.0721,1.1095,1.2304,1.4209,1.4738],
                    [0,.082,.515,.835,.982,1.057,1.112,1.254,1.447,1.501])

def trunk(points):
    x,y,z=points.T;a=np.abs(x);s=np.sign(x);q=points.copy()
    # Full ribcage/pelvis cross-sections replace the inherited breast/waist/hip
    # distribution. Preserve angular topology ordering, then sculpt anatomy.
    levels=[.83,.90,.98,1.055,1.115,1.20,1.28,1.36,1.425,1.475]
    source_w=interp(z,levels,[.135,.162,.176,.140,.095,.105,.125,.144,.160,.060])
    source_d=interp(z,levels,[.085,.108,.112,.086,.065,.073,.096,.092,.079,.050])
    source_c=interp(z,levels,[.014,.019,.010,.005,-.004,.004,.007,.020,.037,.045])
    angle=np.arctan2((y-source_c)/source_d,x/source_w)
    width=interp(z,levels,[.124,.146,.151,.137,.123,.140,.156,.169,.180,.063])
    depth=interp(z,levels,[.079,.095,.097,.087,.088,.103,.111,.105,.084,.057])
    center=interp(z,levels,[.012,.014,.014,.013,.011,.016,.022,.029,.037,.045])
    power=interp(z,levels,[2.05,2.1,2.1,2.05,2.0,2.15,2.40,2.30,2.05,2.0])
    co=np.cos(angle);si=np.sin(angle)
    q[:,0]=width*np.sign(co)*abs(co)**(2/power)
    q[:,1]=center+depth*np.sign(si)*abs(si)**(2/power)
    q[:,2]=height_map(z)
    # A trunk cross-section is unsuitable at the crotch, where the surface
    # passes between the thighs. Preserve that continuous topology in a local
    # pelvis frame and blend into the newly authored iliac/ribcage sections.
    trunk_mix=smooth(.875,.975,z)
    q[:,0]=trunk_mix*q[:,0]+(1-trunk_mix)*x*.855
    q[:,1]=trunk_mix*q[:,1]+(1-trunk_mix)*(.014+(y-.014)*.94)
    X,Y,Z=q.T;A=np.abs(X);front=1-smooth(-.01,.035,Y);back=smooth(.035,.08,Y)
    def g(cx,cz,sx,sz):return np.exp(-.5*(((A-cx)/sx)**2+((Z-cz)/sz)**2))
    # Broad pectoral planes, sternum, lower costal arch and a soft abdominal wall.
    q[:,1]-=.0120*g(.085,1.351,.053,.039)*front
    q[:,1]+=.0040*g(.0,1.347,.012,.074)*front
    q[:,1]+=.0040*g(.088,1.297,.050,.012)*front
    q[:,1]-=.0012*g(.085,1.341,.0035,.0035)*front
    clavicle_z=1.434+.065*A
    q[:,1]-=.0045*np.exp(-.5*(((A-.087)/.063)**2+((Z-clavicle_z)/.008)**2))*front
    q[:,1]+=.0020*g(.0,1.441,.014,.012)*front
    q[:,1]-=.0030*g(.036,1.177,.028,.085)*front
    q[:,1]+=.0010*g(.0,1.181,.008,.076)*front
    q[:,1]+=.0035*g(.0,1.081,.004,.006)*front
    # Scapular planes, lats and restrained lumbar/gluteal volumes.
    q[:,1]+=.0065*g(.083,1.350,.042,.053)*back
    q[:,1]-=.0030*g(.0,1.331,.012,.110)*back
    q[:,0]+=np.sign(X)*.0030*g(.139,1.288,.028,.073)*back
    q[:,1]+=.0140*g(.069,.955,.045,.062)*back
    q[:,1]-=.0060*g(.0,.956,.012,.063)*back
    q[:,1]-=.0040*g(.043,1.098,.060,.050)*back
    # Neutral continuous pubic surface: no explicit genital feature is authored.
    pubic=np.exp(-.5*((A/.046)**2+((Z-.962)/.041)**2))*(1-smooth(-.043,-.017,y))
    q[:,1]=(1-pubic)*q[:,1]+pubic*(-.073+.15*(Z-.923))
    return q

def frame(a,b):
    axis=(b-a)/np.linalg.norm(b-a)
    u=np.array([1.,0,0]);u-=axis*np.dot(u,axis)
    if np.linalg.norm(u)<.05:u=np.array([0.,1,0]);u-=axis*np.dot(u,axis)
    u/=np.linalg.norm(u);v=np.cross(axis,u)
    return axis,u,v

def segment(points,a,b,c,d,scale_u,scale_v):
    axis,u,v=frame(a,b);na,nu,nv=frame(c,d);r=points-a
    along=r@axis;frac=along/np.linalg.norm(b-a)
    su=scale_u(frac) if callable(scale_u) else scale_u
    sv=scale_v(frac) if callable(scale_v) else scale_v
    return c+np.outer(frac,np.array(d)-c)+(r@u*su)[:,None]*nu+(r@v*sv)[:,None]*nv

tr=trunk(P);maps={};landmarks={}
for s,label in [(1,'L'),(-1,'R')]:
    sb=lambda n:BONES['CC_Base_'+label+'_'+n]
    hip=np.array([s*.091,.010,.982]);knee=np.array([s*.099,-.012,.515]);ankle=np.array([s*.098,.005,.082])
    shoulder=np.array([s*.177,.028,1.447]);elbow=shoulder+np.array([s*.251,-.004,-.145])
    wrist=elbow+np.array([s*.214,-.009,-.124])
    landmarks[label]={'hip':hip.tolist(),'knee':knee.tolist(),'ankle':ankle.tolist(),'shoulder':shoulder.tolist(),'elbow':elbow.tolist(),'wrist':wrist.tolist()}
    maps[label+'thigh']=segment(P,sb('Thigh'),sb('Calf'),hip,knee,
        lambda t:interp(t,[0,.35,.75,1],[.79,.91,1.04,1.12]),
        lambda t:interp(t,[0,.4,1],[.91,1.00,1.10]))
    maps[label+'calf']=segment(P,sb('Calf'),sb('Foot'),knee,ankle,
        lambda t:interp(t,[0,.35,.75,1],[1.10,1.12,1.15,1.25]),
        lambda t:interp(t,[0,.35,1],[1.08,1.09,1.24]))
    # Preserve the existing detailed toes/arch/heel under a larger male foot.
    r=P-sb('Foot');heading=sb('ToeBase')-sb('Foot')
    theta=np.arctan2(heading[0],-heading[1]);newtheta=s*.035
    ang=newtheta-theta;rot=np.array([[np.cos(ang),-np.sin(ang),0],[np.sin(ang),np.cos(ang),0],[0,0,1]])
    foot=r*np.array([1.16,1.20,1.45]);foot=foot@rot.T+ankle
    maps[label+'foot']=foot
    maps[label+'upper']=segment(P,sb('Upperarm'),sb('Forearm'),shoulder,elbow,
        lambda t:interp(t,[0,.30,.72,1],[1.29,1.24,1.18,1.19]),
        lambda t:interp(t,[0,.30,.72,1],[1.27,1.21,1.14,1.17]))
    maps[label+'fore']=segment(P,sb('Forearm'),sb('Hand'),elbow,wrist,
        lambda t:interp(t,[0,.30,.8,1],[1.20,1.25,1.15,1.13]),
        lambda t:interp(t,[0,.30,1],[1.17,1.23,1.15]))
    # Hands: local palm breadth, thickness and finger reach; not whole-body scale.
    src_end=sb('Hand')+(sb('Hand')-sb('Forearm'))
    dst_end=wrist+(wrist-elbow)/np.linalg.norm(wrist-elbow)*np.linalg.norm(src_end-sb('Hand'))*1.12
    maps[label+'hand']=segment(P,sb('Hand'),src_end,wrist,dst_end,1.15,1.22)
    maps[label+'knee']=(maps[label+'thigh']+maps[label+'calf'])*.5
    maps[label+'elbow']=(maps[label+'upper']+maps[label+'fore'])*.5

Q=np.zeros_like(P);total=W.sum(1)
assert np.all(total>.99),'Unmapped anatomical source vertices'
for i,name in enumerate(GROUPS):
    label='L' if '_L_' in name else 'R' if '_R_' in name else None
    kind=None
    if label:
        if 'Thigh' in name:kind='thigh'
        elif 'Calf' in name:kind='calf'
        elif 'Knee' in name:kind='knee'
        elif 'Foot' in name or 'Toe' in name:kind='foot'
        elif 'Upperarm' in name:kind='upper'
        elif 'Forearm' in name:kind='fore'
        elif 'Elbow' in name:kind='elbow'
        elif any(t in name for t in ('Hand','Pinky','Ring','Mid','Index','Thumb')):kind='hand'
    target=maps[label+kind] if kind else tr
    Q+=target*(W[:,i]/total)[:,None]

# Keep the full mesh standing at ground without scaling height. Only the lower
# foot region is adjusted to seat the soles; the limb landmarks stay explicit.
lowest=Q[:,2].min();Q[:,2]-=lowest*(1-smooth(.035,.16,Q[:,2]))

# Match the preserved head seam through its stable original vertex IDs. Diffuse
# the correction into the upper torso, without editing the accepted head.
head_ids={a.value:tuple(head.data.vertices[i].co) for i,a in enumerate(head.data.attributes['CC_source_vertex'].data) if a.value>=0}
body_ids=D['sourceVertexIds'];seam=[i for i,v in enumerate(body_ids) if v in head_ids]
assert len(seam)==40
adj=[set() for _ in range(len(P))]
for f in D['faces']:
    for a,b in zip(f,f[1:]+f[:1]):adj[a].add(b);adj[b].add(a)
offset=np.zeros_like(Q);fixed=np.array([i in set(seam) for i in range(len(P))])
active=(P[:,2]>1.335)&(abs(P[:,0])<.15)
for i in seam:offset[i]=np.array(head_ids[body_ids[i]])-Q[i]
for _ in range(220):
    nex=offset.copy()
    for i in np.flatnonzero(active&~fixed):nex[i]=sum((offset[j] for j in adj[i]),np.zeros(3))/max(len(adj[i]),1)
    offset=nex
Q+=offset
for i in seam:Q[i]=head_ids[body_ids[i]]
# Fair the existing collar topology into the fixed neck boundary. This resolves
# local folds caused by bringing the source shoulder girdle to the new landmarks;
# neither the head nor its seam positions are resculpted.
fair=smooth(1.350,1.425,Q[:,2])*(1-smooth(.115,.230,abs(Q[:,0])))
fair[seam]=0
for _ in range(32):
    nex=Q.copy()
    for i in np.flatnonzero(fair>.001):
        average=np.mean(Q[list(adj[i])],axis=0)
        nex[i]=Q[i]+.48*fair[i]*(average-Q[i])
    Q=nex
# Match the tangent using distances on the *fitted* collar, not distances on the
# displaced source collar. Using the latter overextends the first body ring.
head_adj=[set() for _ in head.data.vertices]
for edge in head.data.edges:
    a,b=edge.vertices;head_adj[a].add(b);head_adj[b].add(a)
head_by_id={a.value:i for i,a in enumerate(head.data.attributes['CC_source_vertex'].data) if a.value>=0}
head_seam={head_by_id[body_ids[i]] for i in seam};targets={}
for i in seam:
    h=head_by_id[body_ids[i]]
    upper=[j for j in head_adj[h] if j not in head_seam]
    if not upper:continue
    tangent=np.mean(head_before[upper],axis=0)-Q[i]
    for j in adj[i]:
        if j in seam:continue
        reach=np.linalg.norm(Q[j]-Q[i])
        target=Q[i]-tangent/max(np.linalg.norm(tangent),1e-9)*reach
        targets.setdefault(j,[]).append(target)
correction=np.zeros_like(Q);locked=set(seam)|set(targets)
for j,ts in targets.items():correction[j]=.80*(np.mean(ts,axis=0)-Q[j])
for _ in range(24):
    nex=correction.copy()
    for i in np.flatnonzero(fair>.001):
        if i not in locked:nex[i]=np.mean(correction[list(adj[i])],axis=0)*fair[i]
    correction=nex
Q+=correction

m=bpy.data.meshes.new('SS_MaleBody_v1_Topology');m.from_pydata(Q.tolist(),[],D['faces']);m.update()
body=bpy.data.objects.new('SS_MaleBody_Foundation_Candidate',m);scene=bpy.context.scene;scene.collection.objects.link(body)
m.materials.append(bpy.data.materials['Review only - neutral clay'])
uv=m.uv_layers.new(name='SourceUV_Correspondence');attr=m.attributes.new('CC_source_vertex','INT','POINT')
for i,old in enumerate(body_ids):attr.data[i].value=old
for face,coords in zip(m.polygons,D['uv']):
    face.use_smooth=True
    for li,p in zip(face.loop_indices,coords):uv.data[li].uv=p
sub=body.modifiers.new('Editable anatomy cage','SUBSURF');sub.levels=1;sub.render_levels=2
body['status']='BODY_FOUNDATION_VISUAL_REVIEW_PENDING'
body['source']='User supplied CC professional topology; neutral-fit male anatomical derivation'
body['region_selectors']='Used only while authoring rest geometry; no imported skin binding'
body['rig_target']='SilverScreen adult semantic contract, 91 bones; skinning deferred'
body['groin']='Neutral continuous surface, no explicit genital geometry'
scene['scope']='MALE BODY FOUNDATION GATE - before garment fitting, skinning or animation'
scene['head_foundation']='Approved frozen male anatomical foundation; identity presets remain separate'

# Neutral full-body review, no clothing or body-region hiding.
focus=Vector((0,.012,.89))
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.29,.29,.29,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.30
for name,pos,power,size in [('Neutral key',(-2,-3,3.8),430,2.6),('Neutral fill',(2,-2,2.1),140,3),('Neutral back',(1,2,3),240,2.5)]:
    ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.size=size
    light=bpy.data.objects.new(name,ld);scene.collection.objects.link(light);light.location=pos
    light.rotation_euler=(focus-light.location).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('BodyReview');cam=bpy.data.objects.new('BodyReview',camd);scene.collection.objects.link(cam);scene.camera=cam
camd.type='ORTHO';camd.ortho_scale=1.95
scene.render.engine='CYCLES';scene.cycles.samples=40
scene.render.resolution_x=1250;scene.render.resolution_y=1450;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=0
def camera(delta,scale=1.95,target=focus):
    camd.ortho_scale=scale;cam.location=target+Vector(delta);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
camera((1.7,-4,.05))
assert np.array_equal(head_before,np.array([tuple(v.co) for v in head.data.vertices]))
for o in bpy.context.selected_objects:o.select_set(False)
body.select_set(True);bpy.context.view_layer.objects.active=body
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_location=focus
            area.spaces.active.region_3d.view_distance=2.65
            area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            area.spaces.active.region_3d.view_perspective='PERSP'
            area.spaces.active.clip_start=.01
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_MaleBody_Foundation_Candidate.blend'))
bm=bmesh.new();bm.from_mesh(m)
report={'status':'BODY_FOUNDATION_VISUAL_REVIEW_PENDING','headFoundationSHA256':HEAD_SHA,'headCoordinatesUnchanged':True,
    'sourceSHA256':D['sourceSHA256'],'headSeamVertices':len(seam),'headSeamMaxErrorMetres':float(max(np.linalg.norm(Q[i]-head_ids[body_ids[i]]) for i in seam)),
    'bodyVertices':len(m.vertices),'bodyFaces':len(m.polygons),'bodyQuads':sum(len(f.vertices)==4 for f in m.polygons),
    'boundaryEdges':sum(e.is_boundary for e in bm.edges),'degenerateFaces':sum(f.calc_area()<1e-12 for f in bm.faces),
    'nonManifoldEdgesMoreThanTwoFaces':sum(len(e.link_faces)>2 for e in bm.edges),'skinBindingPresent':False,
    'bodyBoundsMin':Q.min(0).tolist(),'bodyBoundsMax':Q.max(0).tolist(),'anatomicalHeightMetres':float(head_before[:,2].max()),
    'limbLandmarks':landmarks,'bodyCompleteUnderClothing':True,'garmentsPresent':False,'explicitGenitalGeometry':False,
    'rigContractModified':False,'deformationValidated':False,'identityTargetsImplemented':False}
bm.free();(ROOT/'body-report.json').write_text(json.dumps(report,indent=2)+'\n')
np.savez_compressed(ROOT/'male-body-foundation-coordinates.npz',coordinates=Q,sourceVertexIds=body_ids)
shots=[('front',(0,-4,0),1.95,focus),('threequarter',(1.7,-4,.03),1.95,focus),('side',(4,0,0),1.95,focus),('back',(0,4,0),1.95,focus),
       ('torso',(.7,-3,.0),.87,Vector((0,0,1.195))),('hands',(.6,-.1,1.4),.30,Vector(landmarks['L']['wrist'])+Vector((.070,0,-.045))),
       ('feet',(.7,-3,1.7),.40,Vector((0,-.070,.045))),
       ('neck',(.6,-3,.2),.40,Vector((0,0,1.485))),
       ('pelvis',(.6,-3,.1),.50,Vector((0,0,.975)))]
if '--quick' in sys.argv:shots=shots[:5];scene.cycles.samples=24;scene.render.resolution_percentage=65
if '--neck-review' in sys.argv:shots=[('neck',(.6,-3,.2),.40,Vector((0,0,1.485)))];scene.cycles.samples=24
for name,delta,scale,target in shots:
    camera(delta,scale,target);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
if '--quick' not in sys.argv and '--neck-review' not in sys.argv:
    overlay=body.copy();overlay.data=body.data.copy();scene.collection.objects.link(overlay)
    for mod in list(overlay.modifiers):overlay.modifiers.remove(mod)
    wm=bpy.data.materials.new('Topology lines');wm.use_nodes=True
    wm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.008,.008,.008,1)
    overlay.data.materials.clear();overlay.data.materials.append(wm)
    wire=overlay.modifiers.new('Review cage','WIREFRAME');wire.thickness=.0005;wire.offset=1
    body.modifiers[0].show_render=False
    # Both sides of the neck must be displayed at the same subdivision level.
    # Otherwise a cage/rendered-surface mismatch looks like a physical seam gap.
    for mod in head.modifiers:
        if mod.type=='SUBSURF':mod.show_render=False
    camera((.7,-4,0));scene.render.filepath=str(OUT/'topology.png');bpy.ops.render.render(write_still=True)
    camera((.7,-4,0),.83,Vector((0,0,1.16)));scene.render.filepath=str(OUT/'topology-torso.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))
