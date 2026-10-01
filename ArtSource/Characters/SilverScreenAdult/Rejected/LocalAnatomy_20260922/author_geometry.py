"""REJECTED HISTORICAL SOURCE. Do not execute or reuse."""
raise RuntimeError('This body/head was rejected by the user. No regeneration or further fitting is permitted.')
from pathlib import Path
import sys
import json
import hashlib
import bpy
import bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from author_accessories import material
from anatomy import make_body,make_head,make_eyes,make_brows_hair
from garments import make_shirt,make_waistcoat,make_trousers

REVIEW=ROOT.parents[2]/'ArtReview/Characters/SilverScreenAdult'


def visibility(outfit=True,hat=True,hair=True):
    wear=('Garment_','Shoe_','Accessory_')
    for c in bpy.context.scene.collection.children:
        if c.name.startswith(wear):c.hide_render=not outfit
        elif c.name=='Hat_Fedora':c.hide_render=not (outfit and hat)
        elif c.name=='Hair':c.hide_render=not hair
        elif c.name.startswith(('Anatomy_','Eye_','Brows')):c.hide_render=False


def capture(name,location,target,scale,width=1050,height=1300):
    scene=bpy.context.scene;cam=scene.camera
    cam.location=location
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=scale
    scene.render.resolution_x=width;scene.render.resolution_y=height
    scene.render.filepath=str(REVIEW/name)
    bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SS_Adult_Accessories.blend'))
    m=material('Geometry_NeutralSkinClay',(.43,.36,.31),.72)
    lip=material('Geometry_LipClay',(.35,.26,.235),.72)
    interior=material('Geometry_InteriorClay',(.035,.026,.026),.8)
    sclera=material('Geometry_EyeClay',(.60,.58,.54),.32)
    iris=material('Geometry_IrisClay',(.09,.068,.041),.6)
    pupil=material('Geometry_PupilClay',(.011,.009,.008),.6)
    hair=material('Geometry_HairClay',(.036,.030,.025),.72)
    shirt=material('Geometry_ShirtClay',(.61,.59,.54),.8)
    wool=material('Geometry_WaistcoatClay',(.095,.083,.073),.8)
    trouser=material('Geometry_TrouserClay',(.075,.072,.068),.8)
    trim=material('Geometry_SeamClay',(.066,.058,.05),.8)
    buttons=material('Geometry_ButtonClay',(.042,.035,.028),.55)
    body=make_body(m);print('BODY',len(body.data.vertices),flush=True)
    head=make_head(m,lip,interior);print('HEAD',len(head.data.vertices),flush=True)
    make_eyes(sclera,iris,pupil);make_brows_hair(hair)
    make_shirt(shirt,buttons);print('SHIRT',flush=True)
    make_waistcoat(wool,buttons,trim);make_trousers(trouser,trim);print('GARMENTS',flush=True)
    scene=bpy.context.scene
    scene['status']='FIRST LOCAL GEOMETRY STUDY - NOT APPROVED - NOT SKINNED'
    scene['originality']='Only new local source construction; no downloaded or rejected character art'
    scene.cycles.samples=40
    # Match source rest landmarks exactly; no pose or animation work at this gate.
    contract=json.loads((ROOT/'rig-contract.json').read_text())
    rig=bpy.data.objects['SS_Adult_Root']
    assert len(rig.data.bones)==91
    for b in contract['bones']:
        assert (rig.data.bones[b['name']].head_local-Vector(b['head'])).length<1e-6
        assert (rig.data.bones[b['name']].tail_local-Vector(b['tail'])).length<1e-6
    visibility()
    report={'status':'unapproved geometry checkpoint','rigBoneCount':91,'restLandmarksPreserved':True,
            'sourceHumanModels':[],'modelingShortsInBody':False,'bodyAnatomy':'Complete connected torso, pelvis, limbs, fingers, feet and toes; separate head',
            'materials':'Flat neutral review colors only; no final textures',
            'skinned':False,'animationsCreated':False,'unityRuntimeValidated':False,'visualGatePassed':False,
            'knownReviewItems':['Body sculpt requires joint retopology review before skinning','Head/body neck seam requires final matching','Garment clearances require sculpt/fit review','Expression deformation not implemented'],
            'meshObjects':[]}
    for o in bpy.data.objects:
        if o.type!='MESH':continue
        bm=bmesh.new();bm.from_mesh(o.data)
        report['meshObjects'].append({'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),
              'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges),
              'collections':[c.name for c in o.users_collection]})
        bm.free()
    (ROOT/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_Adult_GeometryCheckpoint.blend'))
    visibility(False,False,False)
    capture('body-front.png',(0,-5,1.0),(0,0,.91),2.07,1450,1450)
    capture('head-bald.png',(.20,-1,1.68),(0,-.01,1.65),.34,1150,1150)
    visibility(False,False,True)
    capture('head-hair.png',(.21,-1,1.70),(0,-.01,1.68),.33,1150,1150)
    visibility(True,True,True)
    capture('outfit-front.png',(0,-5,1.0),(0,0,.96),2.13,1450,1450)
    capture('outfit-threequarter.png',(3,-5,1.5),(0,0,.96),2.13,1450,1450)
    capture('outfit-side.png',(5,0,1.0),(0,0,.96),2.13,1450,1450)
    visibility(False,False,False)
    capture('body-threequarter.png',(3,-5,1.4),(0,0,.91),2.07,1450,1450)
    capture('body-back.png',(0,5,1.0),(0,0,.91),2.07,1450,1450)
    visibility(False,False,False)
    wire=material('Geometry_Wire',(.008,.008,.008),.8)
    wireobj=head.copy();wireobj.data=head.data.copy();bpy.data.collections['Anatomy_Head'].objects.link(wireobj)
    wireobj.name='REVIEW_ONLY_HeadWire'
    wireobj.modifiers.clear();wireobj.data.materials.clear();wireobj.data.materials.append(wire)
    mod=wireobj.modifiers.new('Topology overlay','WIREFRAME');mod.thickness=.00020;mod.offset=1
    capture('head-topology.png',(.20,-1,1.68),(0,-.01,1.65),.34,1150,1150)
    bpy.data.objects.remove(wireobj,do_unlink=True)
    visibility()
    print('GEOMETRY CHECKPOINT BUILT; VISUAL REVIEW REQUIRED',flush=True)


if __name__=='__main__':main()
