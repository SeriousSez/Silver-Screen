"""Single SilverScreen canonical adult art study.

Independent authored anatomy: Dan Ulrich / Blender Human Base Meshes 1.4.1, CC0.
No MakeHuman assets, targets, proxy fitting or rejected Gate1 meshes are used.
One restrained identity, no body generation or variants. Metres, feet origin.
"""
import bpy, math, json, random, sys
import bmesh
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/'ArtSource/Characters/ThirdParty/BlenderHumanBaseMeshes/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend'
AUTHOR=ROOT/'ArtSource/Characters/Canonical'
REVIEW=ROOT/'ArtReview/Characters/Canonical'
OUT=ROOT/'Assets/SilverScreen/Art/Characters/Canonical'
for p in (AUTHOR,REVIEW,OUT/'Models',OUT/'Textures'):p.mkdir(parents=True,exist_ok=True)
random.seed(327)

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)

def material(name,color,roughness=.6):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness
    return m

def evaluate(ob,level=2):
    for m in ob.modifiers:
        if m.type=='MULTIRES':m.levels=min(level,m.total_levels);m.sculpt_levels=m.levels;m.render_levels=m.levels
    dg=bpy.context.evaluated_depsgraph_get();dg.update()
    mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),depsgraph=dg)
    ob.modifiers.clear();ob.data=mesh
    for p in mesh.polygons:p.use_smooth=True

def mesh(name,verts,faces,mat,subdiv=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);data.materials.append(mat)
    for p in data.polygons:p.use_smooth=True
    if subdiv:
        mod=ob.modifiers.new('Tailored surface','SUBSURF');mod.levels=mod.render_levels=subdiv
    return ob

def curve(name,points,radius,mat,resolution=12):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=resolution;data.bevel_depth=radius;data.bevel_resolution=3
    sp=data.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
    for p,co in zip(sp.bezier_points,points):p.co=co;p.handle_left_type=p.handle_right_type='AUTO'
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.data.materials.append(mat);return ob

def uv_sphere(name,position,scale,mat,segments=48,rings=32):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=position)
    ob=bpy.context.object;ob.name=name;ob.scale=scale;ob.data.materials.append(mat)
    for p in ob.data.polygons:p.use_smooth=True
    return ob

def anatomy():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    with bpy.data.libraries.load(str(SOURCE),link=False) as (src,dst):
        dst.objects=[n for n in src.objects if n.startswith('GEO-body_male_realistic')]
    objs=dst.objects
    for o in objs:bpy.context.collection.objects.link(o)
    body=next(o for o in objs if o.name=='GEO-body_male_realistic')
    origin=body.location.copy()
    # Retain authored multiresolution detail; do not replace it with subdivision of a generated body.
    for o in objs:evaluate(o,2 if o==body else 1)
    coords=np.array([body.matrix_world @ v.co-origin for v in body.data.vertices]);low=coords[:,2].min();high=coords[:,2].max();factor=1.75/(high-low)
    for o in objs:
        matrix=o.matrix_world.copy()
        for v in o.data.vertices:
            co=matrix@v.co-origin;co.z-=low;co*=factor;v.co=co
        o.matrix_world=Matrix.Identity(4)
    # Authored relaxed arm stance. No changes to head identity, body mass or limb lengths.
    for v in body.data.vertices:
        x,y,z=v.co;sign=1 if x>=0 else -1
        influence=float(smooth(.185,.275,abs(x)))*(1-float(smooth(1.41,1.49,z)))
        if influence>0:
            pivot=Vector((sign*.195,.0,1.425));theta=math.radians(sign*18)*influence
            v.co=pivot+Matrix.Rotation(theta,3,'Y')@(v.co-pivot)
    body.name='Canonical_AuthoredAnatomy'
    skin=material('SS_Canonical_Skin',(1,1,1),.62)
    body.data.materials.clear();body.data.materials.append(skin)
    # Low-amplitude procedural microstructure is non-directional and belongs to material response.
    nodes=skin.node_tree.nodes;links=skin.node_tree.links;p=nodes.get('Principled BSDF')
    p.inputs['Subsurface Weight'].default_value=.07;p.inputs['Subsurface Radius'].default_value=(1,.42,.22)
    colors=body.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
    for i,v in enumerate(body.data.vertices):
        x,y,z=v.co;front=float(smooth(-.055,-.115,y));c=np.array((.43,.245,.158))
        cheeks=math.exp(-((abs(x)-.055)/.026)**2-((z-1.595)/.028)**2)*front
        c+=np.array((.026,-.011,-.006))*cheeks
        lip=math.exp(-((x/.032)**6)-((z-1.548)/.009)**4)*front
        c=c*(1-.45*lip)+np.array((.34,.105,.085))*.45*lip
        beard=float(smooth(1.59,1.55,z))*float(smooth(1.49,1.52,z))*front*(1-.9*lip)
        c*=1-.11*beard
        variation=.008*math.sin(x*331+math.sin(z*119))*math.sin(y*227+z*421)
        colors.data[i].color=(*np.clip(c+variation,0,1),1)
    attr=nodes.new('ShaderNodeVertexColor');attr.layer_name='Color';links.new(attr.outputs['Color'],p.inputs['Base Color'])
    tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=1200;tex.inputs['Detail'].default_value=2
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.00015
    links.new(tex.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])
    for o in objs:
        if o!=body:o.name='Canonical_Eye_'+('L' if o.name.endswith('.L') else 'R');o.hide_render=True
    coords=np.array([v.co for v in body.data.vertices]);report={}
    for lo,hi in [(0,.1),(.7,.85),(1.,1.1),(1.35,1.45),(1.5,1.6),(1.6,1.65),(1.65,1.75)]:
        a=coords[(coords[:,2]>=lo)&(coords[:,2]<=hi)];report[str((lo,hi))]=dict(min=a.min(axis=0).tolist(),max=a.max(axis=0).tolist())
    (REVIEW/'anatomy-measures.json').write_text(json.dumps({'sections':report,'eyes':[dict(name=o.name,min=np.array([v.co for v in o.data.vertices]).min(axis=0).tolist(),max=np.array([v.co for v in o.data.vertices]).max(axis=0).tolist()) for o in objs if o!=body]},indent=2))
    return body,objs,skin

def eyes(parts):
    white=material('SS_Canonical_Sclera',(.66,.625,.54),.22)
    iris=material('SS_Canonical_Iris',(1,1,1),.2)
    n=iris.node_tree.nodes;l=iris.node_tree.links;a=n.new('ShaderNodeVertexColor');a.layer_name='Color';l.new(a.outputs['Color'],n.get('Principled BSDF').inputs['Base Color'])
    black=material('SS_Canonical_Pupil',(.002,.0014,.001),.16)
    for ob in parts:
        if not ob.name.startswith('Canonical_Eye_'):continue
        coords=np.array([v.co for v in ob.data.vertices]);center=(coords.min(axis=0)+coords.max(axis=0))*.5
        radius=(coords[:,2].max()-coords[:,2].min())*.5
        ob.hide_render=False;ob.data.materials.clear();ob.data.materials.append(white)
        verts=[];faces=[];colors=[];segments=128;rings=20
        for j in range(rings+1):
            r=.00215+(.0062-.00215)*j/rings
            for i in range(segments):
                angle=i*math.tau/segments;x=r*math.cos(angle);z=r*math.sin(angle)
                verts.append((center[0]+x,center[1]-math.sqrt(radius*radius-r*r)-.00008,center[2]+z))
                fiber=(math.sin(angle*47+math.sin(j*.8))*math.sin(angle*83+j*.2)+1)*.5
                dark=1-.65*float(smooth(.0055,.0062,r));inner=math.exp(-((r-.0025)/.0008)**2)
                color=np.array((.085+.06*inner,.047+.017*inner,.02))*(.6+fiber*.65)*dark
                colors.append((*color,1))
        for j in range(rings):
            for i in range(segments):a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
        ring=mesh(ob.name+'_Iris',verts,faces,iris)
        ca=ring.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
        for data,c in zip(ca.data,colors):data.color=c
        uv_sphere(ob.name+'_Pupil',(center[0],center[1]-radius-.00013,center[2]),(.0022,.00015,.0022),black)

def brows(body):
    brow=material('SS_Canonical_Brows',(.033,.018,.011),.66)
    coords=[v.co.copy() for v in body.data.vertices];faces=[p.vertices[:] for p in body.data.polygons];bvh=BVHTree.FromPolygons(coords,faces)
    for sign in (-1,1):
        for i in range(300):
            t=random.random();x=sign*(.014+.048*t);z=1.653+.004*math.sin(t*math.pi)-.007*t+random.uniform(-.002,.002)
            hit,normal,_,_=bvh.ray_cast(Vector((x,-.4,z)),Vector((0,1,0)))
            if hit is None:continue
            p=hit+Vector((0,-.0004,0));q=p+Vector((sign*.005,-.00015,.0025*(1-t)))
            curve('Brow_fiber', [p,(p+q)*.5+Vector((0,-.0003,.0002)),q],random.uniform(.00009,.00017),brow,3)

def hair(body):
    """Authored side-part groom on the new sculpt; no reused hair cards."""
    hairmat=material('SS_Canonical_Hair',(.026,.013,.007),.55)
    coords=[v.co.copy() for v in body.data.vertices];faces=[p.vertices[:] for p in body.data.polygons]
    bvh=BVHTree.FromPolygons(coords,faces)
    def line(v):
        x,y,z=v
        # Forehead recesses at the temples; back tapers at the nape.
        front=float(smooth(.01,-.115,y));temple=float(smooth(.04,.075,abs(x)))
        return 1.619+.079*front-.035*front*temple
    scalpfaces=[f for f in faces if all(coords[i].z>line(coords[i]) for i in f)]
    ids=sorted({i for f in scalpfaces for i in f});mapping={i:j for j,i in enumerate(ids)}
    scalpverts=[]
    for i in ids:
        p=coords[i].copy();normal=body.data.vertices[i].normal
        p+=normal*(.0015+.003*float(smooth(1.66,1.75,p.z)));scalpverts.append(p)
    mesh('Hair_undercoat',scalpverts,[[mapping[i] for i in f] for f in scalpfaces],hairmat)
    data=bpy.data.curves.new('Side_part_groom','CURVE');data.dimensions='3D';data.bevel_depth=.00020;data.bevel_resolution=1;data.resolution_u=1
    ob=bpy.data.objects.new('Hair_side_part_strands',data);bpy.context.collection.objects.link(ob);data.materials.append(hairmat)
    # Each guide lies on the actual sculpt. Small length/direction changes avoid
    # repeated waves and let the side part remain readable in neutral light.
    for k in range(4200):
        p=scalpverts[random.randrange(len(scalpverts))].copy();length=random.uniform(.024,.082)
        sign=-1 if p.x<.028 else 1
        sp=data.splines.new('POLY');sp.points.add(13)
        for j in range(14):
            nearest,normal,_,_=bvh.find_nearest(p)
            if nearest is None:break
            t=j/13
            lift=.002+.006*math.sin(t*math.pi)*float(smooth(1.65,1.71,nearest.z))
            lift+=.002*math.exp(-((nearest.y+.12)/.06)**2)*float(smooth(1.69,1.73,nearest.z))
            q=nearest+normal*lift
            sp.points[j].co=(*q,1);sp.points[j].radius=max(.12,(1-t*.88))*random.uniform(.75,1.15)
            direction=Vector((sign*.75,.55,-.15 if nearest.z<1.7 else .02))
            direction-=normal*direction.dot(normal)
            if direction.length<.01:direction=Vector((sign,0,0))
            p=nearest+direction.normalized()*(length/13)+normal*.002
    return ob

def clothing():
    # Each garment section is an authored pattern surface. Its silhouette has
    # ease, structured shoulder, rolled lapel, independent collar and cuffs.
    wool=material('SS_Canonical_Wool',(.065,.059,.053),.78)
    lining=material('SS_Canonical_Lining',(.038,.031,.026),.63)
    ivory=material('SS_Canonical_Shirt',(.70,.65,.53),.69)
    tie=material('SS_Canonical_Tie',(.14,.081,.037),.52)
    button=material('SS_Canonical_Horn',(.027,.018,.012),.4)
    stitch=material('SS_Canonical_Seam',(.04,.035,.03),.8)
    leather=material('SS_Canonical_Leather',(.035,.016,.009),.35)
    sole=material('SS_Canonical_Sole',(.012,.009,.007),.63)
    def loft(name,sections,mat,n=64,sub=2,opening=0):
        # section = centre x, centre y, z, half-width, half-depth.
        verts=[];faces=[]
        for j,(cx,cy,z,rx,ry) in enumerate(sections):
            for i in range(n+1):
                angle=opening+(math.tau-2*opening)*i/n
                # Front is -Y; side seams retain a soft woven rather than tube silhouette.
                x=cx+rx*math.sin(angle);y=cy-ry*math.cos(angle)
                if name in ('Jacket_tailored_panels','Shirt_body'): y-=.034*max(0,math.cos(angle))
                verts.append((x,y,z))
        for j in range(len(sections)-1):
            for i in range(n):a=j*(n+1)+i;faces.append((a,a+1,a+n+2,a+n+1))
        ob=mesh(name,verts,faces,mat,sub)
        solid=ob.modifiers.new('Fabric thickness','SOLIDIFY');solid.thickness=.0018;solid.offset=0
        return ob
    # Pleated high-rise trousers. Generous straight legs, with a modest shoe break.
    for sign in (-1,1):
        sections=[(sign*.128,-.002,.11,.079,.087),(sign*.129,-.003,.12,.080,.089),
          (sign*.13,0,.15,.075,.083),(sign*.128,.004,.23,.074,.079),(sign*.122,.008,.38,.077,.083),
          (sign*.115,.006,.52,.083,.09),(sign*.108,.004,.65,.092,.102),(sign*.096,.001,.80,.108,.113),
          (sign*.084,0,.90,.111,.108),(sign*.078,0,1.005,.108,.102),(sign*.077,0,1.02,.107,.102)]
        trouser=loft('Trousers_'+str(sign),sections,wool)
        # Sharp, subtle front/back pressed crease and relaxed knee/shoe wrinkles.
        for v in trouser.data.vertices:
            x,y,z=v.co;front=max(0,-y/.1);rear=max(0,y/.1)
            center=sign*(.077+(.13-.077)*np.clip((1.02-z)/.9,0,1));dx=abs(x-center)
            crease=math.exp(-(dx/.012)**2)*.005
            v.co.y+=(-front+rear)*crease
            if z<.25:v.co.y+=.004*math.sin((z-.1)*67+x*21)*float(smooth(.1,.16,z))*(1-float(smooth(.18,.25,z)))
        # Narrow turn-up with independent edge, not a heavy ring.
        curve('Trouser_hem',[(sign*.128+.079*math.sin(a),-.002-.087*math.cos(a),.125) for a in np.linspace(0,math.tau,65)],.001,stitch,2)
    # Waistcoat and shirt are distinct layers, visible in the jacket opening.
    loft('Shirt_body',[(0,.0,.98,.16,.107),(0,0,1.2,.175,.112),(0,.005,1.35,.20,.12),(0,.014,1.43,.18,.104),(0,.02,1.48,.064,.071)],ivory,sub=2)
    # Jacket back/sides and deliberately open fronts; upper pattern tapers to shoulder.
    sections=[(0,.007,.835,.191,.132),(0,.007,.85,.196,.137),(0,.004,.93,.193,.13),
      (0,.003,1.025,.177,.12),(0,.006,1.12,.176,.122),(0,.011,1.24,.198,.135),
      (0,.017,1.34,.217,.128),(0,.027,1.415,.211,.108),(0,.027,1.445,.168,.092),
      (0,.022,1.475,.067,.075)]
    jacket=loft('Jacket_tailored_panels',sections,wool,opening=.08)
    # Shape the open V and curved lower fronts directly in the authored pattern.
    n=65
    for j,section in enumerate(sections):
        z=section[2]
        opening=.017 if z<1.16 else np.interp(z,[1.16,1.24,1.34,1.415,1.475],[.017,.045,.073,.073,.052])
        if z<.95:opening+=.046*(1-float(smooth(.84,.98,z)))
        for edge in (0,n-1):
            idx=j*n+edge;v=jacket.data.vertices[idx];v.co.x=opening*(1 if edge==0 else -1);v.co.y=-section[4]+section[1]-.038
    # Sleeve cap is slightly pitched forward; shape is relaxed, not cylindrical armour.
    for sign in (-1,1):
        sleeve=[(sign*.185,.02,1.423,.061,.086),(sign*.217,.018,1.395,.073,.086),
          (sign*.235,.01,1.34,.068,.077),(sign*.248,-.004,1.23,.061,.066),
          (sign*.254,-.018,1.14,.056,.061),(sign*.248,-.033,1.08,.057,.061),
          (sign*.244,-.05,.99,.047,.052),(sign*.241,-.064,.905,.044,.047),
          (sign*.24,-.065,.888,.043,.046)]
        ob=loft('Jacket_sleeve_'+str(sign),sleeve,wool)
        for v in ob.data.vertices:
            x,y,z=v.co
            v.co.y+=.0032*math.sin((z-1.08)*90+x*13)*math.exp(-((z-1.1)/.06)**2)
        loft('Shirt_cuff_'+str(sign),[(sign*.24,-.065,.891,.041,.044),(sign*.24,-.066,.879,.041,.044)],ivory,sub=1)
        for z in (.912,.93,.948):uv_sphere('Sleeve_horn_button',(sign*.279,-.063,z),(.002,.006,.006),button,20,12)
    def panel(name,points,mat,thickness=.002):
        # Beveled edges carry tailoring thickness without the inflated shell effect.
        ob=mesh(name,points,[tuple(range(len(points)))],mat)
        mod=ob.modifiers.new('Cloth facing','SOLIDIFY');mod.thickness=thickness
        bev=ob.modifiers.new('Soft cut edge','BEVEL');bev.width=.0012;bev.segments=3
        return ob
    for sign in (-1,1):
        def pts(p):return [(sign*x,y,z) for x,y,z in p]
        panel('Waistcoat_front',pts([(.006,-.148,1.31),(.062,-.135,1.40),(.155,-.144,1.31),(.151,-.15,1.03),(.09,-.159,.988),(.009,-.159,1.02)]),lining)
        # A moderately broad 1930s notch lapel, with an actual roll and collar notch.
        lap=pts([(.019,-.173,1.125),(.112,-.177,1.342),(.092,-.17,1.359),(.111,-.158,1.379),
           (.057,-.11,1.465),(.046,-.137,1.424),(.061,-.167,1.378),(.032,-.179,1.265)])
        panel('Notched_lapel',lap,wool,.003)
        curve('Lapel_roll',[lap[0],lap[7],lap[6],lap[5],lap[4]],.0007,stitch,10)
        # Spread shirt collar shows clear stand, fold and pointed leaf.
        panel('Shirt_collar_leaf',pts([(.008,-.087,1.477),(.047,-.067,1.477),(.057,-.11,1.439),(.033,-.137,1.413),(.013,-.117,1.457)]),ivory,.0015)
        # Welt hip pockets and welt breast pocket, with fabric thickness.
        for z,x,length in [(1.017,.12,.075),(1.313,.127,.052)] if sign==1 else [(1.017,.12,.075)]:
            pts0=[(sign*(x-length/2),-.157,z),(sign*(x+length/2),-.14,z+.006)]
            curve('Welt_pocket',pts0,.0025,wool,4)
    # Collar stand wraps around the neck independently of the lapels.
    loft('Shirt_collar_stand',[(0,.02,1.448,.058,.064),(0,.02,1.467,.057,.064)],ivory,sub=1)
    for z in (1.05,1.103,1.155,1.207):uv_sphere('Waistcoat_button',(0,-.163,z),(.005,.0018,.005),button,24,16)
    uv_sphere('Jacket_button',(-.019,-.171,1.122),(.009,.003,.009),button,32,20)
    panel('Tie_blade',[(-.01,-.137,1.44),(.009,-.137,1.44),(.024,-.162,1.23),(0,-.164,1.205),(-.024,-.162,1.23)],tie,.002)
    panel('Tie_four_in_hand',[(-.012,-.121,1.463),(.012,-.123,1.463),(.009,-.14,1.436),(-.007,-.14,1.436)],tie,.004)
    # Leather lace-up oxfords, independent sole, heel, vamp, facing and laces.
    for sign in (-1,1):
        cx=sign*.128
        uv_sphere('Oxford_upper',(cx,-.059,.062),(.071,.165,.053),leather,64,32)
        uv_sphere('Oxford_sole',(cx,-.059,.019),(.073,.169,.014),sole,64,20)
        uv_sphere('Oxford_heel',(cx,.043,.021),(.061,.067,.023),sole,40,20)
        uv_sphere('Oxford_quarter',(cx,.004,.095),(.058,.070,.050),leather,48,24)
        for i in range(5):
            y=-.035-i*.014;z=.12-i*.005
            curve('Oxford_lace',[(cx-.023,y,z),(cx,y-.005,z+.004),(cx+.023,y-.007,z)],.0011,sole,4)
        curve('Oxford_toecap_seam',[(cx+.064*math.sin(a),-.133+.017*math.cos(a),.077+.015*math.cos(a)) for a in np.linspace(-math.pi/2,math.pi/2,20)],.0006,stitch,3)
    return wool

def mask_covered_body(body):
    # A reversible authoring duplicate retains the complete sculpt in the blend.
    complete=body.copy();complete.data=body.data.copy();complete.name='SOURCE_CompleteAnatomy';bpy.context.collection.objects.link(complete)
    complete.hide_render=True;complete.hide_viewport=True
    bm=bmesh.new();bm.from_mesh(body.data)
    def visible(v):
        x,y,z=v.co
        return z>1.493 or (z>1.443 and abs(x)<.068) or (.60<z<.89 and abs(x)>.207)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if not any(visible(v) for v in f.verts)],context='FACES')
    bm.to_mesh(body.data);bm.free();body.data.update()

def studio():
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.32,.32,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=0
    for name,pos,power,size in [('Key',(-2,-3,3),280,3),('Fill',(2,-2,2),150,3),('Back',(1,2,3),160,2)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
        ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((0,0,1.1))-ob.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.name='REVIEW_Floor';plane.location.z=-.006;plane.data.materials.append(material('REVIEW_floor',(.18,.19,.2)))
    data=bpy.data.cameras.new('REVIEW_Camera');camera=bpy.data.objects.new('REVIEW_Camera',data);bpy.context.collection.objects.link(camera);scene.camera=camera;data.lens=70
    return camera

def render(camera,name,position,focus,width=900,height=1100):
    camera.location=position;camera.rotation_euler=(Vector(focus)-camera.location).to_track_quat('-Z','Y').to_euler()
    scene=bpy.context.scene;scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.resolution_percentage=100
    scene.render.filepath=str(REVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    body,parts,skin=anatomy();eyes(parts);brows(body);hair(body);clothing();mask_covered_body(body);camera=studio()
    render(camera,'canonical_working_face',(0,-.94,1.64),(0,-.04,1.625))
    render(camera,'canonical_working_body',(0,-4.1,1.15),(0,0,.91),1000,1400)
    bpy.ops.wm.save_as_mainfile(filepath=str(AUTHOR/'Canonical_Working.blend'))
