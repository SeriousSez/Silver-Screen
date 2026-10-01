"""Author independent surfaces from the canonical design's construction mesh.

Preserves the source's visible tailoring/UVs and weights. Reconstructs the
covered shirt, clean scalp and missing anatomy; no rejected garment generator.
"""
import bpy,bmesh,math,json
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent

def solid(name,color,roughness):
    mat=bpy.data.materials.new(name);mat.diffuse_color=(*color,1);mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness
    return mat

def mesh_object(name,verts,faces,mat,rig=None,bone=None):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
    if mat:data.materials.append(mat)
    for p in data.polygons:p.use_smooth=True
    if rig:
        ob.parent=rig;mod=ob.modifiers.new('Canonical rig','ARMATURE');mod.object=rig
        if bone:ob.vertex_groups.new(name=bone).add(list(range(len(verts))),1,'REPLACE')
    return ob

def extract(source,name,faces,rig,material=None,offset=0):
    polys=[source.data.polygons[i] for i in sorted(faces)]
    ids=sorted({v for p in polys for v in p.vertices});mapping={old:new for new,old in enumerate(ids)}
    verts=[source.data.vertices[i].co+source.data.vertices[i].normal*offset for i in ids]
    ob=mesh_object(name,verts,[[mapping[i] for i in p.vertices] for p in polys],material,rig)
    if material is None:
        for mat in source.data.materials:ob.data.materials.append(mat)
    uv=ob.data.uv_layers.new(name='UVMap')
    for src,dst in zip(polys,ob.data.polygons):
        dst.material_index=0 if material else src.material_index
        for a,b in zip(src.loop_indices,dst.loop_indices):uv.data[b].uv=source.data.uv_layers.active.data[a].uv
    for group in source.vertex_groups:ob.vertex_groups.new(name=group.name)
    for old,new in mapping.items():
        for group in source.data.vertices[old].groups:ob.vertex_groups[group.group].add([new],group.weight,'REPLACE')
    return ob

def add_thickness(ob,thickness=.0012):
    mod=ob.modifiers.new('Tailored edge thickness','SOLIDIFY');mod.thickness=thickness;mod.offset=-1
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.modifier_apply(modifier=mod.name)

def smooth_vertices(ob,iterations=4,factor=.3,only=None):
    bm=bmesh.new();bm.from_mesh(ob.data)
    verts=list(bm.verts) if only is None else [v for v in bm.verts if only(v.co)]
    for _ in range(iterations):bmesh.ops.smooth_vert(bm,verts=verts,factor=factor,use_axis_x=True,use_axis_y=True,use_axis_z=True)
    bm.to_mesh(ob.data);bm.free();ob.data.update()

def fit_anatomy(source,rig,skin):
    """Fit only the missing anatomical regions of the existing CC0 authored base.

    Face and hands remain the NEW sculpt on Renderpeople technical topology.
    The CC0 source does not supply facial identity, hair or garments.
    """
    path=ROOT/'ArtSource/Characters/ThirdParty/BlenderHumanBaseMeshes/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend'
    with bpy.data.libraries.load(str(path),link=False) as (src,dst):dst.objects=['GEO-body_male_realistic']
    ob=dst.objects[0];bpy.context.collection.objects.link(ob)
    for mod in ob.modifiers:
        if mod.type=='MULTIRES':mod.levels=1;mod.render_levels=1
    dg=bpy.context.evaluated_depsgraph_get();dg.update()
    data=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),depsgraph=dg)
    world=ob.matrix_world.copy();origin=ob.location.copy();ob.modifiers.clear();ob.data=data
    points=[world@v.co-origin for v in data.vertices];low=min(p.z for p in points);factor=1.755/(max(p.z for p in points)-low)
    for v,p in zip(data.vertices,points):
        p.z-=low;p*=factor;p.x=-p.x;p.y=-p.y
        # Shoulder/arm registration follows the clothed source's relaxed pose.
        sign=1 if p.x>=0 else -1;ax=abs(p.x)
        if ax>.18 and p.z<1.49:
            t=max(0,min(1,(ax-.18)/.10));t=t*t*(3-2*t)
            pivot=Vector((sign*.183,0,1.44))
            p=pivot+Matrix.Rotation(math.radians(sign*18)*t,3,'Y')@(p-pivot)
            p.x+=sign*.025;p.y+=.07*max(0,min(1,(1.2-p.z)/.35))
        else:
            p.x*=1.08 if p.z>1.14 else 1.02
        p.z+=.020
        v.co=p
    ob.matrix_world=Matrix.Identity(4);ob.name='Anatomy_ReconstructionMaster'
    # Match calf/thigh axis and torso envelope conservatively inside tailoring.
    for v in ob.data.vertices:
        p=v.co
        if p.z<1.05:
            sign=1 if p.x>=0 else -1
            p.x=sign*.09+(p.x-sign*.08)*.89;p.y*=.88
        if 1.05<p.z<1.48:p.y*=.85
    # Transfer authored deformation weights from the clothed anatomical landmarks.
    tree=KDTree(len(source.data.vertices))
    for v in source.data.vertices:tree.insert(v.co,v.index)
    tree.balance()
    for group in source.vertex_groups:ob.vertex_groups.new(name=group.name)
    for v in ob.data.vertices:
        nearest=tree.find_n(v.co,4);weights={};den=0
        for co,index,d in nearest:
            w=1/max(d,.002)**2;den+=w
            for vg in source.data.vertices[index].groups:weights[vg.group]=weights.get(vg.group,0)+vg.weight*w
        for index,w in weights.items():
            if w/den>.0001:ob.vertex_groups[index].add([v.index],w/den,'REPLACE')
    ob.data.materials.clear();ob.data.materials.append(skin)
    if not ob.data.uv_layers:ob.data.uv_layers.new(name='UVMap')
    # Region boundaries allow per-outfit masking without deleting body anatomy.
    regions={k:[] for k in ('Body_Torso','Body_Arms','Body_Legs','Body_Feet')}
    for p in ob.data.polygons:
        c=p.center
        # Discard the CC0 head and hands, keeping overlapping neck/wrist interfaces.
        if c.z>1.535 or (abs(c.x)>.235 and c.z<.939):continue
        if c.z<.115:region='Body_Feet'
        elif c.z<1.04 and abs(c.x)<.22:region='Body_Legs'
        elif abs(c.x)>.175 and c.z<1.465:region='Body_Arms'
        else:region='Body_Torso'
        regions[region].append(p.index)
    result=[extract(ob,name,ids,rig,skin) for name,ids in regions.items()]
    bpy.data.objects.remove(ob,do_unlink=True)
    return result

def fedora(rig):
    felt=solid('SS_Concept_Felt',(.145,.100,.065),.88)
    band=solid('SS_Concept_HatBand',(.040,.027,.018),.70)
    trim=solid('SS_Concept_HatEdge',(.105,.066,.040),.79)
    n=128;verts=[];faces=[]
    # Elliptical brim with an intentional dipped front and rolled side profile.
    for j in range(9):
        t=j/8;rx=.088+.061*t;ry=.109+.069*t
        for i in range(n):
            a=i*math.tau/n;x=rx*math.sin(a);y=ry*math.cos(a)
            z=1.744+.016*t*t*abs(math.sin(a))**3-.011*t*max(0,math.cos(a))
            verts.append((x,y+.012,z))
    for j in range(8):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    brim=mesh_object('Headwear_FedoraBrim',verts,faces,felt,rig,'head');add_thickness(brim,.0028)
    verts=[];faces=[]
    # Tapered crown with pinched front and longitudinal centre crease.
    for j in range(17):
        t=j/16;rx=.090*(1-.18*t**1.5);ry=.110*(1-.10*t)
        for i in range(n):
            a=i*math.tau/n;x=rx*math.sin(a);y=ry*math.cos(a)
            pinch=.015*math.exp(-((abs(a if a<math.pi else a-math.tau)-.6)/.26)**2)*math.sin(t*math.pi/2)**3
            x*=1-pinch/.09
            z=1.744+.119*t-.014*t**4*math.cos(a)**2
            verts.append((x,y+.012,z))
    for j in range(16):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    # Quad strips span the crown top, descending into its crease.
    top=verts[16*n:]
    centre=len(verts);verts.append((0,.012,1.837))
    for i in range(n):faces.append((16*n+i,16*n+(i+1)%n,centre))
    crown=mesh_object('Headwear_FedoraCrown',verts,faces,felt,rig,'head');add_thickness(crown,.002)
    verts=[];faces=[]
    for z,t in [(1.748,0),(1.780,.27)]:
        for i in range(n):
            a=i*math.tau/n;verts.append((.091*(1-.18*t**1.5)*math.sin(a),.111*(1-.10*t)*math.cos(a)+.012,z))
    for i in range(n):faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
    ribbon=mesh_object('Headwear_FedoraRibbon',verts,faces,band,rig,'head');add_thickness(ribbon,.001)
    # The ribbon bow has physical layered folds on the left of the crown.
    verts=[(-.091,.008,1.750),(-.089,.065,1.750),(-.091,.065,1.779),(-.094,.008,1.779),
           (-.094,.032,1.749),(-.094,.040,1.749),(-.097,.040,1.781),(-.097,.032,1.781)]
    bow=mesh_object('Headwear_FedoraBow',verts,[(0,1,2,3),(4,5,6,7)],band,rig,'head');add_thickness(bow,.0018)
    return [brim,crown,ribbon,bow]

def brows(source,rig,mat):
    tree=BVHTree.FromPolygons([v.co for v in source.data.vertices],[p.vertices[:] for p in source.data.polygons])
    verts=[];faces=[]
    for sign in (-1,1):
        for i in range(50):
            t=i/49;x=sign*(.013+.052*t);z=1.684+.003*math.sin(t*math.pi)-.009*t
            width=.0054*(1-.60*t)
            for dz in (-width/2,width/2):
                hit,normal,_,_=tree.ray_cast(Vector((x,.30,z+dz)),Vector((0,-1,0)))
                if hit is None:hit=Vector((x,.088,z+dz))
                verts.append(hit+Vector((0,.0007,0)))
        start=(len(verts)-100)
        for i in range(49):a=start+i*2;faces.append((a,a+2,a+3,a+1))
    return mesh_object('Groom_Brows',verts,faces,mat,rig,'head')

def build_parts(rig,source):
    assert len(source.data.vertices)==41816,'Authored atlas regions need explicit remapping if topology changes.'
    # A preserved editable sculpt master is excluded from the FBX selection.
    master=source.copy();master.data=source.data.copy();bpy.context.collection.objects.link(master)
    master.name='SOURCE_ConceptSculpt_EDITABLE';master.hide_render=True;master.hide_set(True)
    # Apply the chosen identity, retaining interpolated weights and original UVs.
    positions=[v.co.copy() for v in source.data.shape_keys.key_blocks['SilverScreen_CanonicalDesign'].data]
    source.shape_key_clear()
    for v,p in zip(source.data.vertices,positions):v.co=p
    source.data.update()
    uv=source.data.uv_layers.active.data
    image=bpy.data.images.get('rp_eric_rigged_001_dif.jpg');w,h=image.size
    pixels=np.empty(w*h*4,np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(h,w,4)
    def sample(poly):
        u,v=sum((uv[i].uv for i in poly.loop_indices),Vector((0,0)))/len(poly.loop_indices)
        return pixels[min(h-1,max(0,int(v*h))),min(w-1,max(0,int(u*w))),:3]
    skin=source.data.materials[0].copy();skin.name='SS_Concept_Skin'
    hair=source.data.materials[0].copy();hair.name='SS_Concept_Hair'
    cloth=source.data.materials[0].copy();cloth.name='SS_Concept_Cloth'
    leather=source.data.materials[0].copy();leather.name='SS_Concept_Leather'
    eye=source.data.materials[1]
    scalp=solid('SS_Concept_Anatomy',(.34,.193,.121),.64)
    shirt=solid('SS_Concept_Shirt',(.64,.585,.49),.78)
    brow=solid('SS_Concept_Brows',(.024,.012,.006),.78)
    source.data.materials.clear()
    for mat in (skin,hair,cloth,leather,eye,scalp,shirt):source.data.materials.append(mat)
    slots={name:[] for name in ('Identity_Head','Identity_Hands','Identity_Eyes','Groom_Hair','Outfit_Shirt','Outfit_Waistcoat','Outfit_Trousers','Outfit_Shoes','Outfit_Tie','Outfit_Belt')}
    scalp_faces=[];shirt_under=[]
    regions=json.loads((HERE/'source_regions.json').read_text())
    for region in regions['islands']:
        name=region['region']
        for index in region['faces']:
            p=source.data.polygons[index];x,y,z=p.center;color=sample(p);value=float(max(color))
            slot=None;material=2
            if name in ('face','head','mouth'):
                slot='Identity_Head';material=0
                # Separate actual dark scalp from the face, ears and nape.
                cap=z>1.714 or (z>1.663 and abs(x)>.061 and y<.068) or (z>1.612 and y<.022)
                if name!='mouth' and cap and value<.49:
                    slots['Groom_Hair'].append(index);scalp_faces.append(index);material=5
            elif name=='eyes':slot='Identity_Eyes';material=4
            elif name.startswith('hand') or name.startswith('cuff'):
                is_skin=color[0]>color[2]*1.27 and color[0]>color[1]*1.08
                slot='Identity_Hands' if is_skin else 'Outfit_Shirt';material=0 if is_skin else 2
            elif name=='neck':
                is_skin=color[0]>color[2]*1.28 and color[0]>color[1]*1.08
                slot='Identity_Head' if is_skin else 'Outfit_Shirt';material=0 if is_skin else 2
            elif name.startswith('sleeve'):slot='Outfit_Shirt'
            elif name.startswith('shoe'):slot='Outfit_Shoes';material=3
            elif name=='trousers':
                slot='Outfit_Belt' if 1.013<z<1.051 and abs(x)<.169 else 'Outfit_Trousers'
            elif name.startswith('vest') or name.startswith('front'):
                shirt_under.append(index)
                if name.startswith('front') and value>.52:slot='Outfit_Shirt'
                elif name.startswith('front') and y>.085 and abs(x)<.027 and 1.275<z<1.498:slot='Outfit_Tie'
                else:slot='Outfit_Waistcoat'
            if slot:slots[slot].append(index);p.material_index=material
    result=[]
    for name,ids in slots.items():
        ob=extract(source,name,ids,rig,hair if name=='Groom_Hair' else None,.0012 if name=='Groom_Hair' else 0)
        if name=='Groom_Hair':
            smooth_vertices(ob,3,.2)
            add_thickness(ob,.0014)
        if name in ('Outfit_Waistcoat','Outfit_Shirt','Outfit_Trousers','Outfit_Tie'):add_thickness(ob,.0010)
        result.append(ob)
    # Under-vest shirt uses the authored continuous tailored torso as a fitted
    # foundation, not disconnected generated panels. Suppress vest-shaped folds.
    under=extract(source,'Outfit_ShirtUnderlay',shirt_under,rig,shirt,-.0035)
    smooth_vertices(under,12,.48)
    add_thickness(under,.001)
    result.append(under)
    head=next(o for o in result if o.name=='Identity_Head')
    smooth_vertices(head,5,.28,lambda p:p.z>1.715 or (p.y<.018 and p.z>1.620))
    result+=fit_anatomy(source,rig,scalp)
    result+=fedora(rig)
    result.append(brows(source,rig,brow))
    for o in result:
        o['rig_version']='canonical-concept-v1';o['canonical_fit']='average-adult-01'
    source.hide_render=True;source.hide_set(True)
    return result
