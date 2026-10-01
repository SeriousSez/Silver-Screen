"""Single canonical study from separately authored geometry and surfacing.

Retains the licensed Renderpeople Eric source, UVs, scan normals and rig.
No rejected MakeHuman assets or procedural body/clothing lofts are used.
This study is not an approved production foundation or modular wardrobe.
"""
import bpy, math, json, shutil, sys
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parents[3]
AUTHOR=ROOT/'ArtSource/Characters/Canonical'
SOURCE=ROOT/'ArtSource/Characters/ThirdParty/RenderpeopleEric/Original'
REVIEW=ROOT/'ArtReview/Characters/Canonical'
OUT=ROOT/'Assets/SilverScreen/Art/Characters/Canonical'
sys.path.insert(0,str(AUTHOR))
from build_canonical import studio, render

def build():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE/'rp_eric_rigged_001_zup_a.fbx'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    # A uniform calibration preserves the authored proportions and source rig.
    coords=[body.matrix_world@v.co for v in body.data.vertices]
    low=min(v.z for v in coords);high=max(v.z for v in coords);factor=1.75/(high-low)
    rig.scale*=factor;rig.location.z=-low*factor
    bpy.context.view_layer.update()
    for name,angle in [('upperarm_l',33),('upperarm_r',-33)]:
        bone=rig.pose.bones[name];pivot=bone.head.copy()
        bone.matrix=Matrix.Translation(pivot)@Matrix.Rotation(math.radians(angle),4,'Y')@Matrix.Translation(-pivot)@bone.matrix
        bpy.context.view_layer.update()
    # Restrained tailoring on authored garment topology: raise the waist 45 mm
    # and ease the lower trouser leg. No anatomy extremes or new body variants.
    inverse=body.matrix_world.inverted()
    for vertex in body.data.vertices:
        co=body.matrix_world@vertex.co;x,y,z=co
        if abs(x)<.245 and .70<z<1.22:
            co.z+=.045*math.exp(-((z-.965)/.12)**2)
        if .14<z<.82 and abs(x)<.23:
            blend=math.sin(math.pi*(z-.14)/(.82-.14))**.7
            centre=.095 if x>=0 else -.095
            co.x=centre+(x-centre)*(1+.16*blend)
            co.y=y*(1+.05*blend)
        vertex.co=inverse@co
    material=bpy.data.materials.new('SS_Canonical_Authored');material.use_nodes=True
    body.data.materials.clear();body.data.materials.append(material)
    nodes=material.node_tree.nodes;links=material.node_tree.links;p=nodes.get('Principled BSDF')
    p.inputs['Roughness'].default_value=.62
    p.inputs['Specular IOR Level'].default_value=.25
    for kind in ('dif','norm','gloss'):
        tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(SOURCE/f'tex/rp_eric_rigged_001_{kind}.jpg'))
        if kind=='dif': links.new(tex.outputs['Color'],p.inputs['Base Color'])
        elif kind=='norm':
            tex.image.colorspace_settings.name='Non-Color';normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65
            links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],p.inputs['Normal'])
        else:
            tex.image.colorspace_settings.name='Non-Color';mult=nodes.new('ShaderNodeMath');mult.operation='MULTIPLY';mult.inputs[1].default_value=.65
            invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
            links.new(tex.outputs['Color'],mult.inputs[0]);links.new(mult.outputs[0],invert.inputs[1]);links.new(invert.outputs[0],p.inputs['Roughness'])
    for polygon in body.data.polygons:polygon.use_smooth=True
    eye=material.copy();eye.name='SS_Canonical_Eyes'
    p=eye.node_tree.nodes.get('Principled BSDF')
    for socket in ('Roughness','Normal'):
        for link in list(p.inputs[socket].links):eye.node_tree.links.remove(link)
    p.inputs['Roughness'].default_value=.10;p.inputs['Specular IOR Level'].default_value=.5
    body.data.materials.append(eye)
    eye_groups={g.index for g in body.vertex_groups if g.name in ('eye_l','eye_r')}
    for polygon in body.data.polygons:
        if all(any(g.group in eye_groups and g.weight>.9 for g in body.data.vertices[i].groups) for i in polygon.vertices):polygon.material_index=1
    sub=body.modifiers.new('Authored surface interpolation','SUBSURF');sub.levels=sub.render_levels=1
    return rig,body

def export(rig,body):
    # Bake only this neutral pose into a new rest rig. Preserve authored UVs and
    # interpolated weights; no compatibility dependency on rejected adult-v1.
    bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
    for modifier in list(body.modifiers): bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    modifier=body.modifiers.new('Canonical authored rig','ARMATURE');modifier.object=rig
    rig.name='CanonicalAdult_Rig_v0';body.name='CanonicalAdult_AuthoredSurface'
    coords=[body.matrix_world@v.co for v in body.data.vertices]
    low=min(v.z for v in coords);height=max(v.z for v in coords)-low;correction=1.75/height
    rig.location=Vector((rig.location.x,rig.location.y,rig.location.z-low))*correction;rig.scale*=correction
    rig.rotation_euler.z=math.pi
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.context.view_layer.update()
    study=None
    if '--authored-baseline' not in sys.argv:
        from stylization_study import apply_study
        study=apply_study(rig,body)
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
    output=OUT/'Models/CanonicalAdult.fbx';output.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=study is None,
        add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE',path_mode='STRIP')
    texdir=OUT/'Textures';texdir.mkdir(parents=True,exist_ok=True)
    for kind in ('dif','norm','gloss'):shutil.copy2(SOURCE/f'tex/rp_eric_rigged_001_{kind}.jpg',texdir/f'Canonical_{kind}.jpg')
    coords=[body.matrix_world@v.co for v in body.data.vertices]
    report={'status':'single visual candidate; approval pending','source':'Renderpeople Eric Rigged 001','rig_version':'canonical-authored-v0',
        'height_m':max(v.z for v in coords)-min(v.z for v in coords),'feet_z':min(v.z for v in coords),
        'vertices':len(body.data.vertices),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(rig.data.bones),
        'model':str(output.relative_to(ROOT)),'unity_forward':'+Z','material':'SS_Canonical_Authored',
        'limitations':['Clothed source has no complete hidden nude body','Garments share one source mesh and atlas; not a modular wardrobe','No customization variants or animation approval','Period cut and stylization require explicit visual review']}
    if study:
        report['study']=study['study'];report['authored_base_height_m']=report['height_m'];report['height_m']=study['rendered_height_m']
        report['status']='technical foundation accepted; one stylization study awaiting visual review'
    (AUTHOR/'generation_report.json').write_text(json.dumps(report,indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(AUTHOR/'Canonical_AuthoredCandidate.blend'))

if __name__=='__main__':
    rig,body=build()
    if '--render-study' in sys.argv:
        camera=studio()
        render(camera,'authored_candidate_face',(0,-.94,1.64),(0,-.02,1.615),1000,1200)
        render(camera,'authored_candidate_body',(0,-4.1,1.15),(0,0,.9),1000,1400)
    export(rig,body)
