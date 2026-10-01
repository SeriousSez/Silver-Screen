"""Authored accessory meshes for the fresh SilverScreen source lineage.

All vertices and construction curves originate here. No rejected art is loaded.
This is an accessory-only intermediate, not the character geometry gate.
"""
from pathlib import Path
import json
import math
import bpy
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent
REVIEW = ROOT.parents[2] / 'ArtReview/Characters/SilverScreenAdult'
TAU = math.tau


def material(name, color, roughness=.7):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = roughness
    return m


def move_collection(obj, collection):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    bpy.data.collections[collection].objects.link(obj)


def mesh(name, vertices, faces, mat, collection, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.data.collections[collection].objects.link(obj)
    obj.data.materials.append(mat)
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    for p in data.polygons:
        p.use_smooth = smooth
    obj['source'] = 'Fresh authored construction; no imported character asset'
    obj['status'] = 'Geometry study; fit/deformation review pending'
    return obj


def grid(name, ring_count, segments, fn, mat, collection, close=True):
    vertices = [fn(j / (ring_count - 1), TAU * i / segments) for j in range(ring_count) for i in range(segments)]
    faces = []
    for j in range(ring_count - 1):
        for i in range(segments if close else segments - 1):
            k = (i + 1) % segments
            faces.append((j * segments + i, j * segments + k, (j+1) * segments + k, (j+1) * segments + i))
    return mesh(name, vertices, faces, mat, collection)


def thickness(obj, value):
    m = obj.modifiers.new('Real cloth/leather thickness', 'SOLIDIFY')
    m.thickness = value
    m.offset = -1
    m.use_even_offset = True


def subdiv(obj, levels=1):
    m = obj.modifiers.new('Editable surface refinement', 'SUBSURF')
    m.levels = levels
    m.render_levels = levels


def cord(name, points, radius, mat, collection, cyclic=False):
    d = bpy.data.curves.new(name, 'CURVE')
    d.dimensions = '3D'
    d.resolution_u = 3
    d.bevel_depth = radius
    d.bevel_resolution = 2
    s = d.splines.new('POLY')
    s.points.add(len(points) - 1)
    for p, v in zip(s.points, points):
        p.co = (*v, 1)
    s.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, d)
    bpy.data.collections[collection].objects.link(obj)
    d.materials.append(mat)
    obj['source'] = 'Fresh authored curve'
    return obj


def add_hat(mats):
    col = 'Hat_Fedora'
    def crown(v, t):
        # Distinct front pinches, tapered crown and restrained felt irregularity.
        front = max(0, math.cos(t))
        pinch = math.exp(-((abs(math.sin(t))-.46)/.22)**2) * front**3
        rx = .092 - .007*v - .008*pinch*math.sin(math.pi*v)**2
        ry = .111 - .011*v - .010*pinch*math.sin(math.pi*v)**2
        z = 1.730 + .127*v + .008*math.sin(t)**2*v
        z += .002*math.sin(3*t+.4)*v*(1-v)
        return (rx*math.sin(t), -ry*math.cos(t), z)
    o = grid('Fedora_FeltCrown', 15, 96, crown, mats['felt'], col)
    thickness(o,.0018)
    subdiv(o)
    def roof(v,t):
        r = 1-v*.98
        x=.085*r*math.sin(t)
        y=-.100*r*math.cos(t)
        z=1.857+.008*(math.sin(t)**2)*r
        z-=.022*(1-r*r)*math.exp(-(x/.035)**2)
        return x,y,z
    top=grid('Fedora_CreasedTop',13,96,roof,mats['felt'],col)
    thickness(top,.0018)
    subdiv(top)
    # Merge roof to crown at final fitted authoring pass; editable seam is explicit.
    def brim(v,t):
        x=(.091+.065*v)*math.sin(t)
        y=-(.110+.061*v)*math.cos(t)
        z=1.730+v*v*(.006*math.sin(t)**2-.009*math.cos(t))
        return x,y,z
    o=grid('Fedora_Brim',9,128,brim,mats['felt'],col)
    thickness(o,.0020)
    subdiv(o)
    cord('Fedora_BoundBrim', [brim(1,TAU*i/192) for i in range(192)],.00085,mats['ribbon'],col,True)
    def ribbon(v,t):
        p=crown(.075+.27*v,t)
        return p[0]*1.012,p[1]*1.012,p[2]
    o=grid('Fedora_GrosgrainRibbon',5,96,ribbon,mats['ribbon'],col)
    thickness(o,.0008)
    for edge in (0,1):
        cord('Ribbon_BoundEdge_'+str(edge),[ribbon(edge,TAU*i/128) for i in range(128)],.00045,mats['ribbon'],col,True)
    # Two softly folded bow leaves on the wearer's left temple.
    for leaf in (-1,1):
        vs=[]
        fs=[]
        for j in range(7):
            u=j/6
            for i in range(5):
                v=i/4-.5
                y=.005+leaf*(.004+.026*u)
                x=.095+.003*math.sin(math.pi*u)+.0015*math.cos(v*TAU)*math.sin(math.pi*u)
                z=1.758+v*(.020+.008*u)
                vs.append((x,y,z))
        for j in range(6):
            for i in range(4):
                a=j*5+i
                fs.append((a,a+1,a+6,a+5))
        o=mesh('Ribbon_BowLeaf_'+str(leaf),vs,fs,mats['ribbon'],col)
        thickness(o,.0007)
        subdiv(o)
    o=mesh('Ribbon_BowKnot',[(.098,y,z) for z in (1.746,1.770) for y in (-.004,.008)],[(0,1,3,2)],mats['ribbon'],col)
    thickness(o,.0012)
    o=grid('Fedora_InnerSweatband',3,96,lambda v,t:(.088*math.sin(t),-.106*math.cos(t),1.716+.024*v),mats['leather'],col)
    thickness(o,.0012)


def add_shoe(side, center, mats):
    col='Shoe_'+side
    def outline(t):
        c,s=math.cos(t),math.sin(t)
        x=.050*math.copysign(abs(s)**.78,s)*(1-.18*max(0,-c))
        y=-.083-.154*c
        return x,y
    def surface(v,t):
        # v=0 is ankle opening; v=1 terminates at the welt.
        ox,oy=outline(t)
        ix=.034*math.sin(t)
        iy=.014-.040*math.cos(t)
        x=ix*(1-v)+ox*v
        y=iy*(1-v)+oy*v
        z=.025+.104*(1-v)**.62
        z+=.009*max(0,math.cos(t))*math.sin(math.pi*v)
        return center+x,y,z
    upper=grid(side+'_Shoe_LeatherUpper',18,96,surface,mats['leather'],col)
    thickness(upper,.002)
    subdiv(upper)
    cord(side+'_Shoe_CollarBinding',[surface(0,TAU*i/128) for i in range(128)],.00125,mats['leather'],col,True)
    sole=grid(side+'_Shoe_WeltSole',5,96,lambda v,t:(center+outline(t)[0]*1.035,outline(t)[1],.007+.017*v),mats['sole'],col)
    thickness(sole,.004)
    subdiv(sole)
    vs=[(center+outline(TAU*i/96)[0]*1.035,outline(TAU*i/96)[1],.007) for i in range(96)]
    mesh(side+'_Shoe_SoleBottom',vs,[tuple(reversed(range(96)))],mats['sole'],col)
    # Rounded heel block adds physical ground contact without changing scaffold bones.
    bpy.ops.mesh.primitive_cube_add(size=1,location=(center,.029,.0055))
    heel=bpy.context.object
    heel.name=side+'_Shoe_StackedHeel'
    heel.dimensions=(.079,.082,.011)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    heel.data.materials.append(mats['sole'])
    move_collection(heel,col)
    b=heel.modifiers.new('Rounded heel construction','BEVEL'); b.width=.008; b.segments=4
    for p in heel.data.polygons: p.use_smooth=True
    # Surface intersection locates seams and laces without floating detail.
    bvh=BVHTree.FromPolygons([v.co for v in upper.data.vertices],[list(f.vertices) for f in upper.data.polygons])
    def z_at(x,y,offset=.001):
        hit=bvh.ray_cast(Vector((center+x,y,.4)),Vector((0,0,-1)))[0]
        return (hit.z if hit is not None else .10)+offset
    for delta in (0,.0018):
        points=[]
        for i in range(65):
            x=-.043+.086*i/64
            y=-.163+delta-.009*(1-(x/.043)**2)
            points.append((center+x,y,z_at(x,y)))
        cord(side+'_Shoe_CapToeSeam_'+str(delta),points,.00055,mats['stitch'],col)
    # Lacing edges, metal eyelets and individual cords.
    for sign in (-1,1):
        pts=[]
        for i in range(32):
            y=-.109+.095*i/31
            x=sign*(.017+.004*i/31)
            pts.append((center+x,y,z_at(x,y,.0015)))
        cord(side+'_Shoe_Eyestay_'+str(sign),pts,.0012,mats['leather'],col)
        for i in range(5):
            y=-.097+.016*i
            x=sign*(.015+.002*i/4)
            bpy.ops.mesh.primitive_torus_add(major_radius=.0023,minor_radius=.0005,major_segments=16,minor_segments=6,location=(center+x,y,z_at(x,y,.002)))
            eye=bpy.context.object; eye.name=side+'_Shoe_Eyelet_'+str(sign)+'_'+str(i)
            eye.data.materials.append(mats['metal']); move_collection(eye,col)
    for i in range(4):
        for sign in (-1,1):
            a=(sign*(.015+.002*i/4),-.097+.016*i)
            b=(-sign*(.015+.002*(i+1)/4),-.097+.016*(i+1))
            pts=[]
            for j in range(15):
                u=j/14; x=a[0]*(1-u)+b[0]*u; y=a[1]*(1-u)+b[1]*u
                pts.append((center+x,y,z_at(x,y,.0035)+.001*math.sin(math.pi*u)))
            cord(side+'_Shoe_Lace_'+str(i)+'_'+str(sign),pts,.0008,mats['lace'],col)
    for sign in (-1,1):
        pts=[]
        for i in range(49):
            t=TAU*i/48
            x=sign*(.009+.010*math.sin(t))
            y=-.018+.010*math.cos(t)
            pts.append((center+x,y,z_at(x,y,.005)))
        cord(side+'_Shoe_LaceBow_'+str(sign),pts,.0008,mats['lace'],col)


def add_tie_belt(mats):
    col='Accessory_Tie'
    # Blade follows the front chest plane; final fit awaits the actual shirt mesh.
    rows=[(1.444,.008,-.081),(1.420,.010,-.092),(1.375,.012,-.103),(1.315,.015,-.109),(1.25,.024,-.105),(1.205,.027,-.098),(1.172,.001,-.095)]
    vs=[]
    for z,w,y in rows:
        for u in (-1,-.5,0,.5,1):
            vs.append((w*u,y-.0018*(1-u*u),z))
    fs=[]
    for j in range(len(rows)-1):
        for i in range(4):
            a=j*5+i; fs.append((a,a+1,a+6,a+5))
    o=mesh('Tie_FrontBlade',vs,fs,mats['ribbon'],col); thickness(o,.0013); subdiv(o)
    knot=grid('Tie_FourInHandKnot',7,24,lambda v,t:((.010+.004*math.sin(v*math.pi)) * math.sin(t),-.079-(.006+.003*math.sin(v*math.pi))*math.cos(t),1.437+.034*v),mats['ribbon'],col)
    thickness(knot,.001); subdiv(knot)
    grid('Tie_NeckBand',3,64,lambda v,t:(.055*math.sin(t),-.010-.049*math.cos(t),1.457+.009*v),mats['ribbon'],col)
    col='Accessory_Belt'
    belt=grid('Belt_LeatherStrap',4,128,lambda v,t:(.148*math.sin(t),-.087*math.cos(t),1.004+.029*v),mats['leather'],col)
    thickness(belt,.0022)
    def buckle_loop():
        points=[]
        w,h=.018,.012
        for cx,cz,start in ((w-.003,h-.003,0),(-w+.003,h-.003,90),(-w+.003,-h+.003,180),(w-.003,-h+.003,270)):
            for i in range(9):
                a=math.radians(start+i*90/8)
                points.append((cx+.003*math.cos(a),-.091,1.018+cz+.003*math.sin(a)))
        return points
    cord('Belt_BuckleFrame',buckle_loop(),.0014,mats['metal'],col,True)
    cord('Belt_BuckleProng',[(0,-.092,1.008),(0,-.094,1.02),(0,-.092,1.03)],.001,mats['metal'],col)
    for sign in (-1,1):
        cord('Belt_Edge_'+str(sign),[(.1483*math.sin(TAU*i/192),-.0873*math.cos(TAU*i/192),1.0185+sign*.012) for i in range(192)],.00035,mats['stitch'],col,True)


def studio():
    world=bpy.context.scene.world
    if world is None:
        world=bpy.data.worlds.new('Neutral Review'); bpy.context.scene.world=world
    world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.65
    for name,loc,power,size in [('Neutral_Key',(3,-4,5),400,4),('Neutral_Fill',(-3,-1,3),280,4),('Neutral_Back',(0,4,4),350,3)]:
        d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size
        o=bpy.data.objects.new(name,d); bpy.context.scene.collection.objects.link(o); o.location=loc
        o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
    camera=bpy.data.cameras.new('Geometry Review Camera')
    obj=bpy.data.objects.new('Geometry Review Camera',camera)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.scene.camera=obj
    camera.type='ORTHO'
    scene=bpy.context.scene
    scene.render.engine='CYCLES'; scene.cycles.samples=32
    scene.render.resolution_x=1200; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
    scene.view_settings.view_transform='AgX'
    scene.render.image_settings.file_format='PNG'
    scene.render.film_transparent=False
    return obj


def render(camera,name,collection,location,target,scale):
    for c in bpy.context.scene.collection.children:
        if c.name in ('Hat_Fedora','Shoe_Left','Shoe_Right','Accessory_Tie','Accessory_Belt'):
            c.hide_render=c.name!=collection
    camera.location=location
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=scale
    bpy.context.scene.render.filepath=str(REVIEW/name)
    bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SS_Adult_Authoring.blend'))
    REVIEW.mkdir(parents=True,exist_ok=True)
    mats={
        'felt':material('Review_FeltClay',(.23,.20,.17)),
        'ribbon':material('Review_DarkClothClay',(.052,.047,.042)),
        'leather':material('Review_LeatherClay',(.075,.064,.051),.42),
        'sole':material('Review_SoleClay',(.026,.024,.022)),
        'stitch':material('Review_ConstructionEdges',(.14,.12,.09)),
        'lace':material('Review_LaceClay',(.035,.030,.025)),
        'metal':material('Review_MetalClay',(.27,.25,.19),.38),
    }
    add_hat(mats)
    add_shoe('Left',.102,mats)
    add_shoe('Right',-.102,mats)
    add_tie_belt(mats)
    bpy.context.scene['status']='ACCESSORY GEOMETRY ONLY - BODY AND CLOTHING PENDING'
    camera=studio()
    rig=bpy.data.objects['SS_Adult_Root']
    assert len(rig.data.bones)==91
    contract=json.loads((ROOT/'rig-contract.json').read_text())
    for b in contract['bones']:
        assert (rig.data.bones[b['name']].head_local-Vector(b['head'])).length<1e-6
        assert (rig.data.bones[b['name']].tail_local-Vector(b['tail'])).length<1e-6
    report={'status':'accessory-only intermediate; not the requested character gate',
            'rigBoneCount':91,'rigRestLandmarksPreserved':True,
            'authoredModules':['fedora','left shoe','right shoe','tie','belt'],
            'bodyPresent':False,'headPresent':False,'clothingPresent':False,
            'skinningPerformed':False,'visualGatePassed':False,
            'objects':[{'name':o.name,'type':o.type,'vertices':len(o.data.vertices) if o.type=='MESH' else None} for o in bpy.data.objects if o.type in ('MESH','CURVE')]}
    (ROOT/'accessory-geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SS_Adult_Accessories.blend'))
    render(camera,'accessory-fedora.png','Hat_Fedora',(.36,-.60,2.06),(0,0,1.79),.42)
    render(camera,'accessory-shoe.png','Shoe_Left',(.47,-.59,.39),(.102,-.07,.068),.38)
    render(camera,'accessory-tie.png','Accessory_Tie',(.25,-1,1.55),(0,-.07,1.32),.39)
    render(camera,'accessory-belt.png','Accessory_Belt',(.45,-.85,1.25),(0,0,1.02),.39)
    print(json.dumps({k:v for k,v in report.items() if k!='objects'}))


if __name__=='__main__':
    main()
