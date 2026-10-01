"""Build the first fully assembled, unrigged SilverScreen concept candidate.

Frozen professional anatomical foundations + persistent identity deltas +
replaceable authored wardrobe/hair. No source/rejected clothing or skeleton.
"""
import bpy,bmesh,json,hashlib,sys,math
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT))
from geometry import *
from identity import author_identity,author_hair
from wardrobe import shirt,waistcoat,trousers,accessories
REPO=ROOT.parents[3];OUT=REPO/'ArtReview/Characters/SilverScreenAdult/CanonicalGate';OUT.mkdir(parents=True,exist_ok=True)
BASE=ROOT.parent/'Foundations/SS_MaleBody_Foundation_v1.blend'
BASE_SHA='682780ad972dc167b4789ff871f3670fa538dd45c7e0683f7143770b454e12b0'
assert hashlib.sha256(BASE.read_bytes()).hexdigest()==BASE_SHA
bpy.ops.wm.open_mainfile(filepath=str(BASE))
scene=bpy.context.scene;body=bpy.data.objects['SS_MaleBody_Foundation_Candidate'];head=bpy.data.objects['SS_MaleHead_Foundation_v1']
body_base=np.array([v.co[:] for v in body.data.vertices]);head_base=np.array([v.co[:] for v in head.data.vertices])
for o in list(bpy.data.objects):
    if o.type in ('LIGHT','CAMERA'):bpy.data.objects.remove(o,do_unlink=True)
place(body,'Anatomy.Body');place(head,'Anatomy.Head')
for n,col in [('SS_Eye_L','Anatomy.Eye.L'),('SS_Eye_R','Anatomy.Eye.R'),('SS_Teeth','Anatomy.Mouth'),('SS_Tongue','Anatomy.Mouth')]:place(bpy.data.objects[n],col)
mats={
 'skin':material('Skin warm neutral',(.38,.226,.145),.64),
 'lips':material('Lip surface',(.285,.126,.088),.65),
 'hair':material('Hair deep chestnut',(.023,.016,.011),.56),
 'hair_alt':material('Hair clump variation',(.030,.020,.013),.59),
 'hair_line':material('Hair comb relief',(.014,.009,.006),.68),
 'shirt':material('Ivory cotton',(.48,.455,.405),.86),
 'shirt_seam':material('Cotton stitching',(.48,.445,.36),.85),
 'button_light':material('Shirt buttons',(.63,.60,.50),.45),
 'vest':material('Warm charcoal wool',(.026,.022,.018),.90),
 'vest_back':material('Waistcoat satin back',(.025,.021,.018),.65),
 'vest_edge':material('Wool bound edge',(.039,.031,.024),.88),
 'button_dark':material('Horn buttons',(.026,.020,.015),.48),
 'seam_dark':material('Dark seam',(.009,.008,.007),.9),
 'trouser':material('Charcoal trousers',(.029,.025,.021),.90),
 'trouser_seam':material('Trouser tailoring',(.058,.049,.041),.9),
 'felt':material('Fedora brown felt',(.080,.049,.028),.89),
 'ribbon':material('Grosgrain dark ribbon',(.025,.019,.014),.78),
 'tie':material('Dark silk tie',(.031,.029,.025),.56),
 'leather':material('Dark oxblood leather',(.031,.021,.014),.34),
 'leather_seam':material('Leather seam',(.055,.034,.019),.60),
 'sole':material('Leather sole',(.014,.011,.008),.65),
 'metal':material('Antique brass fittings',(.24,.16,.061),.36,.65),
}
assign(body,mats['skin']);assign(head,mats['skin'])
# Smooth, restrained lip-region tint in this review shader, not hard polygon
# material islands or a baked source face. The basis and UVs stay unchanged.
lip=head.data.color_attributes.new(name='ReviewLipMask',type='FLOAT_COLOR',domain='POINT')
for i,v in enumerate(head.data.vertices):
    x,y,z=v.co;mask=(math.exp(-.5*((z-1.600)/.0035)**2)+math.exp(-.5*((z-1.587)/.004)**2))*math.exp(-.5*(x/.019)**4)*(1-float(smooth(-.066,-.046,y)))
    lip.data[i].color=(min(.42,mask*.32),)*3+(1,)
skin_head=mats['skin'].copy();skin_head.name='Review · Skin with soft lip mask';assign(head,skin_head)
nodes=skin_head.node_tree.nodes;links=skin_head.node_tree.links;attr=nodes.new('ShaderNodeVertexColor');attr.layer_name='ReviewLipMask';mix=nodes.new('ShaderNodeMixRGB')
mix.inputs[1].default_value=(.38,.226,.145,1);mix.inputs[2].default_value=(.285,.126,.088,1);links.new(attr.outputs['Color'],mix.inputs[0]);links.new(mix.outputs[0],nodes['Principled BSDF'].inputs['Base Color'])
preset=author_identity(head,ROOT,mats)
author_hair(head,mats)
land=json.loads((ROOT.parent/'BodyGate/male-rest-landmarks-v2.json').read_text())['meshLandmarks']
shirt_obj=shirt(body,land,mats);vest=waistcoat(shirt_obj,mats);pants=trousers(body,mats);accessories(ROOT,shirt_obj,mats)
assert np.array_equal(body_base,np.array([v.co[:] for v in body.data.vertices]))
assert np.array_equal(head_base,np.array([v.co[:] for v in head.data.vertices]))
scene['scope']='FIRST FULLY ASSEMBLED UNRIGGED CANONICAL VISUAL GATE'
scene['foundation']='Male proportions approved; authorized shoulder relaxation and final restrained anatomical planes completed'
scene['identity']='Persistent identity shape keys above unchanged foundation; expressions separate and absent'
scene['body_coverage']='All anatomical faces retained and visible; clothing hides them by physical occlusion only'
scene['rig_contract']='91-bone SilverScreen semantic scaffold preserved separately; no skinning/animation in this gate'
scene['material_status']='Provisional concept palette only; pigmentation, final PBR maps and UV density are later work'

focus=Vector((0,.0,.94));world=scene.world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.24,.24,.24,1);world.node_tree.nodes['Background'].inputs[1].default_value=.30
for name,pos,power,size in [('Neutral key',(-2.4,-3.0,3.6),480,2.1),('Neutral fill',(2,-2,2.6),170,3),('Neutral back',(1,2.5,3.1),340,2.4)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(focus-o.location).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('CanonicalReview');cam=bpy.data.objects.new('CanonicalReview',camd);scene.collection.objects.link(cam);scene.camera=cam;camd.type='ORTHO'
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1500;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
def camera(delta,scale,target):
    camd.ortho_scale=scale;cam.location=Vector(target)+Vector(delta);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
camera((1.5,-4,.05),2.04,focus)
for o in bpy.context.selected_objects:o.select_set(False)
head.select_set(True);bpy.context.view_layer.objects.active=head
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_location=focus;area.spaces.active.region_3d.view_distance=2.65
            area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.clip_start=.01
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_Canonical_Unrigged_Candidate.blend'))
modules={c.name:[o.name for o in c.all_objects] for c in bpy.data.collections if c.name.startswith(('Module.','Anatomy.'))}
report={'status':'ASSEMBLED_UNRIGGED_VISUAL_REVIEW_PENDING','foundationSHA256':BASE_SHA,'bodyBasisUnchanged':True,'headBasisUnchanged':True,
        'identityPreset':preset['id'],'modules':modules,'completeAnatomicalBody':True,'hiddenAnatomicalFaces':0,'bodyVertices':len(body.data.vertices),
        'bodyFaces':len(body.data.polygons),'armatures':sum(o.type=='ARMATURE' for o in bpy.data.objects),'skinning':False,'animation':False,
        'materials':'Review palette only, no final PBR or pigmentation claim','UnityImported':False,'productionReady':False}
(ROOT/'assembly-report.json').write_text(json.dumps(report,indent=2)+'\n')
shots=[('front',(0,-4,0),2.04,focus),('threequarter',(1.7,-4,.02),2.04,focus),('profile',(4,0,0),2.04,focus),('back',(0,4,0),2.04,focus),
       ('portrait',(0,-3,.025),.43,Vector((0,0,1.639))),('portrait-threequarter',(1.35,-3,.03),.43,Vector((0,0,1.639))),
       ('tailoring',(.6,-3,.04),.76,Vector((0,0,1.22))),('shoes',(.7,-2,1.2),.50,Vector((0,-.06,.09)))]
if '--quick' in sys.argv:shots=[shots[1],shots[4]];scene.cycles.samples=20;scene.render.resolution_percentage=65
for name,delta,scale,target in shots:
    camera(delta,scale,target);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
if '--quick' not in sys.argv:
    bpy.data.collections['Module.Fedora'].hide_render=True
    camera((1.2,-3,.015),.33,Vector((0,.02,1.659)));scene.render.filepath=str(OUT/'hair-and-identity.png');bpy.ops.render.render(write_still=True)
    bpy.data.collections['Module.Fedora'].hide_render=False
    for c in bpy.data.collections:
        if c.name.startswith('Module.'):c.hide_render=True
    camera((1.2,-4,0),1.95,Vector((0,0,.90)));scene.render.filepath=str(OUT/'complete-underlying-body.png');bpy.ops.render.render(write_still=True)
    for c in bpy.data.collections:
        if c.name.startswith('Module.'):c.hide_render=False
print(json.dumps({k:v for k,v in report.items() if k!='modules'},indent=2))
