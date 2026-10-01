"""SilverScreen adult-v1 gate sources. Run with Blender 5 in background mode.

Uses only CC0 geometry/targets; no MakeHuman application code is required.
Coordinates in hm08 are decimetres, Y up. The exported rig is metres, feet origin.
"""
import bpy, json, gzip, re, shutil, math, sys
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'ArtSource/Characters/ThirdParty/MakeHuman'
OUT = ROOT / 'Assets/SilverScreen/Art/Characters/Gate1'
AUTHOR = ROOT / 'ArtSource/Characters/Gate1'
PACK = SOURCE / 'Packs'
VERSION = 'adult-v1.0.0'

RECIPES = [
 dict(id='A', name='Adrian', foundation='adult-m', height=1.75, ancestry='caucasian', sex='male',
      muscle='averagemuscle', weight='averageweight', skin='young_caucasian_male', hair='short02', shoes='shoes01',
      outfit='suits/clothes/toigo_male_suit_tie_and_jacket',
      identity={'head/head-square':.25, 'chin/chin-prominent-incr':.2, 'nose/nose-hump-incr':.25, 'mouth/mouth-scale-horiz-decr':.15}),
 dict(id='B', name='Beatrice', foundation='adult-f', height=1.575, ancestry='asian', sex='female',
      muscle='averagemuscle', weight='minweight', skin='young_asian_female', hair='bob02', shoes='shoes04',
      outfit='suits/clothes/toigo_female_suit',
      identity={'head/head-oval':.4, 'chin/chin-width-decr':.18, 'nose/nose-scale-depth-incr':.18, 'mouth/mouth-cupidsbow-incr':.3}),
 dict(id='C', name='Calvin', foundation='adult-m', height=1.98, ancestry='african', sex='male',
      muscle='maxmuscle', weight='maxweight', skin='young_african_male', hair='short01', shoes='shoes03',
      outfit='system/clothes/male_worksuit01',
      identity={'head/head-rectangular':.25, 'chin/chin-width-incr':.22, 'nose/nose-scale-horiz-decr':.12, 'mouth/mouth-scale-horiz-incr':.15}),
]

def obj_read(path):
    v,uv,faces,groups = [],[],[],{}; group='default'
    for line in path.read_text().splitlines():
        p=line.split()
        if not p: continue
        if p[0]=='v': v.append([float(x) for x in p[1:4]])
        elif p[0]=='vt': uv.append([float(x) for x in p[1:3]])
        elif p[0]=='g': group=p[1]
        elif p[0]=='f':
            f=[tuple(int(x)-1 if x else -1 for x in s.split('/')[:2]) for s in p[1:]]
            faces.append((group,f)); groups.setdefault(group,set()).update(x[0] for x in f)
    return np.array(v),uv,faces,groups

BASE, UV, FACES, GROUPS = obj_read(SOURCE/'3dobjs/base.obj')
RIG = json.loads((SOURCE/'rigs/standard/rig.mixamo_unity.json').read_text())
BNAMES = list(RIG)
WEIGHTS = np.zeros((len(BASE),len(BNAMES)),dtype=float)
for name,values in json.loads((SOURCE/'rigs/standard/weights.mixamo_unity.json').read_text())['weights'].items():
    if name in BNAMES:
        for i,w in values: WEIGHTS[i,BNAMES.index(name)] = w

def target(path):
    a=np.zeros_like(BASE)
    content=gzip.open(path,'rt').read() if path.suffix=='.gz' else path.read_text()
    for line in content.splitlines():
        p=line.split()
        if p and p[0].isdigit() and len(p)>=4: a[int(p[0])]=[float(x) for x in p[1:4]]
    return a

def morph(name): return target(SOURCE/'targets'/(name+'.target.gz'))

SHAPES = {name:target(PACK/'faceunits/targets/faceunits'/(name+'.target')) for name in [
    'eyeBlinkLeft','eyeBlinkRight','jawOpen','mouthSmileLeft','mouthSmileRight',
    'mouthFrownLeft','mouthFrownRight','browDownLeft','browDownRight','browInnerUp','cheekSquintLeft','cheekSquintRight']}
SHAPES.update({name:morph(path)*.45 for name,path in {
    'IdentityJawWidth':'chin/chin-width-incr','IdentityNoseWidth':'nose/nose-scale-horiz-incr',
    'IdentityNoseBridge':'nose/nose-hump-incr','IdentityMouthWidth':'mouth/mouth-scale-horiz-incr',
    'IdentityCheekWidth':'head/head-scale-horiz-incr','IdentityChinHeight':'chin/chin-height-incr'}.items()})

def parse_proxy(folder):
    path=next(folder.glob('*.mhclo')); attrs={}; refs=[]; deletes=set(); mode='head'
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        p=line.split()
        if not p or p[0].startswith('#'): continue
        if p[0]=='verts': mode='verts';continue
        if p[0]=='delete_verts': mode='delete';continue
        if mode=='head': attrs[p[0]]=p[1:]
        elif mode=='verts' and p[0].isdigit():
            if len(p)==1: refs.append(([int(p[0])]*3,[1,0,0],[0,0,0]))
            elif len(p)==9: refs.append(([int(x) for x in p[:3]],[float(x) for x in p[3:6]],[float(x) for x in p[6:9]]))
        elif mode=='delete':
            for m in re.finditer(r'(\d+)(?:\s*-\s*(\d+))?',line):
                a=int(m[1]);b=int(m[2] or a);deletes.update(range(a,b+1))
    return dict(folder=folder, path=path, attrs=attrs, indices=np.array([x[0] for x in refs]),
                bary=np.array([x[1] for x in refs]), offsets=np.array([x[2] for x in refs]),deletes=deletes)

def fit(proxy,coords):
    scale=[]
    for axis,letter in enumerate('xyz'):
        p=proxy['attrs'].get(letter+'_scale')
        scale.append(abs(coords[int(p[0]),axis]-coords[int(p[1]),axis])/float(p[2]) if p else 1)
    return (coords[proxy['indices']]*proxy['bary'][:,:,None]).sum(axis=1)+proxy['offsets']*scale

MATERIALS={}
def material(name,path,kind):
    attrs={}
    if path and path.is_dir(): path=next(path.glob('*.mhmat'),None)
    if path and path.exists():
        for line in path.read_text().splitlines():
            p=line.split()
            if p: attrs[p[0]]=p[1:]
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    color=[1,1,1,1]
    if kind=='teeth': color=[.88,.83,.72,1]
    if kind=='tongue': color=[.35,.07,.065,1]
    if kind=='lashes': color=[.025,.018,.014,1]
    rough={'skin':.68,'hair':.6,'eyes':.2,'teeth':.3,'tongue':.4,'outfit':.74,'shoes':.48,'brows':.75,'lashes':.75}[kind]
    bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=rough
    record=dict(name=name,color=color,roughness=rough,kind=kind,albedo='',normal='',bump='',alpha=kind in ('hair','brows','lashes','eyes'))
    for field,key in [('albedo','diffuseTexture'),('normal','normalmapTexture'),('bump','bumpTexture')]:
        if key not in attrs: continue
        file=(path.parent/' '.join(attrs[key])).resolve()
        if not file.exists(): continue
        dst=OUT/'Textures'/(name+'_'+field+file.suffix);shutil.copyfile(file,dst)
        record[field]=str(dst.relative_to(ROOT)).replace('\\','/')
        if field=='albedo':
            tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(file),check_existing=True)
            m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
            if record['alpha']:m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
    MATERIALS[name]=record
    return m

def make_mesh(name,coords,uv,faces,weights,mat,shape_coords=None,subdiv=0):
    # Keep only referenced vertices, retaining UV seams as loop UVs.
    used=sorted({i for _,f in faces for i,*_ in f});index={v:i for i,v in enumerate(used)}
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(coords[used],[],[[index[x[0]] for x in f] for _,f in faces]);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(mat)
    layer=mesh.uv_layers.new(name='UVMap')
    for poly,(_,f) in zip(mesh.polygons,faces):
        poly.use_smooth=True
        for li,point in zip(poly.loop_indices,f):
            if len(point)>1 and point[1]>=0:layer.data[li].uv=uv[point[1]]
    for j,b in enumerate(BNAMES):
        vg=ob.vertex_groups.new(name=b.replace('mixamorig:',''))
        for i,source in enumerate(used):
            w=weights[source,j]
            if w>1e-5:vg.add([i],float(w),'REPLACE')
    if shape_coords:
        ob.shape_key_add(name='Basis')
        for key,values in shape_coords.items():
            kb=ob.shape_key_add(name=key);kb.data.foreach_set('co',values[used].flatten())
    if subdiv:
        # Evaluate the same Catmull-Clark topology for basis and every target.
        mod=ob.modifiers.new('Closeup subdivision','SUBSURF');mod.levels=mod.render_levels=subdiv
        dg=bpy.context.evaluated_depsgraph_get();dg.update()
        baked=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),depsgraph=dg)
        samples={}
        if shape_coords:
            for key in shape_coords:
                ob.data.shape_keys.key_blocks[key].value=1;dg.update()
                em=ob.evaluated_get(dg).to_mesh();a=np.empty(len(em.vertices)*3);em.vertices.foreach_get('co',a);samples[key]=a
                ob.evaluated_get(dg).to_mesh_clear();ob.data.shape_keys.key_blocks[key].value=0
            dg.update()
        ob.modifiers.clear();ob.data=baked
        if samples:
            ob.shape_key_add(name='Basis')
            for key,a in samples.items(): ob.shape_key_add(name=key).data.foreach_set('co',a)
    # Prune to four normalized influences, the actual runtime skinning contract.
    for v in ob.data.vertices:
        keep=sorted([(g.group,g.weight) for g in v.groups if g.weight>1e-5],key=lambda x:-x[1])[:4]
        total=sum(w for _,w in keep)
        for group in list(v.groups):ob.vertex_groups[group.group].remove([v.index])
        if total:
            for j,w in keep:ob.vertex_groups[j].add([v.index],w/total,'REPLACE')
        else:ob.vertex_groups['Hips'].add([v.index],1,'REPLACE')
    return ob

def endpoint(record,coords):
    s=record['strategy']
    if s=='CUBE':return coords[list(GROUPS[record['cube_name']])].mean(axis=0)
    if s=='MEAN':return coords[record['vertex_indices']].mean(axis=0)
    if s=='VERTEX':return coords[record.get('vertex_index',record.get('vertex_indices',[0])[0])]
    raise ValueError(record)

def build(recipe):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    raw=BASE.copy()+morph('macrodetails/'+recipe['ancestry']+'-'+recipe['sex']+'-young')
    raw+=morph('macrodetails/universal-'+recipe['sex']+'-young-'+recipe['muscle']+'-'+recipe['weight'])
    for path,value in recipe['identity'].items():raw+=morph(path)*value
    bodyids=list(GROUPS['body']);ground=raw[bodyids,1].min();height=raw[bodyids,1].max()-ground
    factor=recipe['height']/height
    def convert(v):
        a=v.copy();a[:,1]-=ground
        # Unity sees FBX -Z forward as +Z after import; rotate the authored
        # MakeHuman forward to +Y in Blender so the final visual faces Unity +Z.
        return np.column_stack((-a[:,0],a[:,2],a[:,1]))*factor
    coords=convert(raw)
    armdata=bpy.data.armatures.new('SS_Adult_v1');arm=bpy.data.objects.new('Rig',armdata);bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for name,r in RIG.items():
        b=armdata.edit_bones.new(name.replace('mixamorig:',''))
        b.tail=convert(endpoint(r['tail'],raw)[None,:])[0]
        # Head and tail records are fitted from helper landmarks, never fixed source positions.
        b.head=convert(endpoint(r['head'],raw)[None,:])[0]
        if (b.tail-b.head).length<.001:b.tail=b.head+Vector((0,0,.02))
        b.roll=r['roll']
    for name,r in RIG.items():
        if r['parent']:armdata.edit_bones[name.replace('mixamorig:','')].parent=armdata.edit_bones[r['parent'].replace('mixamorig:','')]
    bpy.ops.object.mode_set(mode='OBJECT')
    pieces=[('Outfit',PACK/recipe['outfit'],'outfit'),('Shoes',PACK/'system/clothes'/recipe['shoes'],'shoes'),
            ('Hair',PACK/'system/hair'/recipe['hair'],'hair'),('Eyes',PACK/'system/eyes/high-poly','eyes'),
            ('Brows',PACK/'system/eyebrows/eyebrow001','brows'),('Lashes',PACK/'system/eyelashes/eyelashes01','lashes'),
            ('Teeth',PACK/'system/teeth/teeth_base','teeth'),('Tongue',PACK/'system/tongue/tongue01','tongue')]
    proxies=[(n,parse_proxy(p),kind) for n,p,kind in pieces]
    hidden=set().union(*(p['deletes'] for _,p,k in proxies if k in ('outfit','shoes')))
    bodyfaces=[f for f in FACES if f[0]=='body' and not any(v[0] in hidden for v in f[1])]
    skinpath=next((PACK/'system/skins'/recipe['skin']).glob('*.mhmat'))
    skin=material('SS_'+recipe['id']+'_Skin',skinpath,'skin')
    objs=[]
    shapes={key:convert(raw+delta) for key,delta in SHAPES.items()}
    for lod in (0,1):
        ob=make_mesh('Body_LOD'+str(lod),coords,UV,bodyfaces,WEIGHTS,skin,shapes,subdiv=1 if lod==0 else 0);objs.append(ob)
    for name,p,kind in proxies:
        _,uv,faces,_=obj_read(p['folder']/' '.join(p['attrs']['obj_file']))
        fitted=fit(p,raw)
        # Adapt the supplied knee skirt to a 1930s mid-calf hem in the source recipe.
        if recipe['id']=='B' and kind=='outfit':
            y=fitted[:,1];lo=y.min();zone=(y<lo+2.0)
            fitted[zone,1]-=.95*np.clip(1-(y[zone]-lo)/2.0,0,1)
        w=(WEIGHTS[p['indices']]*p['bary'][:,:,None]).sum(axis=1);w=np.maximum(w,0)
        if kind in ('eyes','hair'):
            w[:]=0
            if kind=='hair':w[:,BNAMES.index('mixamorig:Head')]=1
            else:
                w[fitted[:,0]>0,BNAMES.index('mixamorig:LeftEye')]=1;w[fitted[:,0]<=0,BNAMES.index('mixamorig:RightEye')]=1
        matpath=p['folder']/' '.join(p['attrs'].get('material',[]))
        if kind=='eyes':matpath=PACK/'system/eyes/materials'/('green.mhmat' if recipe['id']=='A' else 'brown.mhmat')
        mat=material('SS_'+recipe['id']+'_'+name,matpath,kind)
        facial=kind in ('brows','lashes','teeth','tongue')
        targets={key:convert(fit(p,raw+delta)) for key,delta in SHAPES.items()} if facial else None
        ob=make_mesh(name,convert(fitted),uv,faces,w,mat,targets,subdiv=1 if kind=='outfit' else 0);objs.append(ob)
    for ob in objs:
        ob.parent=arm;mod=ob.modifiers.new('Adult skin','ARMATURE');mod.object=arm
    # Place the whole rest skeleton and all skinned vertices above the shoe sole.
    floor=min(v.co.z for ob in objs if ob.name.startswith('Shoes') for v in ob.data.vertices)
    if floor<0:
        bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
        for b in armdata.edit_bones:b.head.z-=floor;b.tail.z-=floor
        bpy.ops.object.mode_set(mode='OBJECT')
        for ob in objs:
            if ob.data.shape_keys:
                for k in ob.data.shape_keys.key_blocks:
                    for v in k.data:v.co.z-=floor
            else:
                for v in ob.data.vertices:v.co.z-=floor
    bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
    bpy.ops.wm.save_as_mainfile(filepath=str(AUTHOR/(recipe['id']+'_Adult_v1.blend')))
    bpy.ops.object.select_all(action='SELECT')
    fbx=OUT/'Models'/(recipe['id']+'_Adult_v1.fbx')
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE'},
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,mesh_smooth_type='FACE')
    return dict(**recipe,rig=VERSION,anatomicalHeight=recipe['height'],soleLift=-min(0,float(floor)),
        model=str(fbx.relative_to(ROOT)).replace('\\','/'),
        meshes=[dict(name=o.name,vertices=len(o.data.vertices),triangles=sum(len(p.vertices)-2 for p in o.data.polygons),
                     morphs=len(o.data.shape_keys.key_blocks)-1 if o.data.shape_keys else 0) for o in objs],
        bones=[dict(name=b.name,parent=b.parent.name if b.parent else '',head=list(b.head_local),tail=list(b.tail_local)) for b in armdata.bones])

if __name__=='__main__':
    raise SystemExit('REJECTED VISUAL PROTOTYPE. Retained for provenance only; do not regenerate or extend this set. See ArtReview/Characters/Gate1/REJECTED.md.')
    for d in ('Models','Textures'): (OUT/d).mkdir(parents=True,exist_ok=True)
    reports=[]
    for recipe in RECIPES:reports.append(build(recipe))
    (AUTHOR/'generation_report.json').write_text(json.dumps(dict(version=VERSION,candidates=reports,materials=list(MATERIALS.values())),indent=2))
    print('SILVERSCREEN_GATE1_EXPORT_COMPLETE',[(x['id'],sum(m['triangles'] for m in x['meshes'])) for x in reports])
