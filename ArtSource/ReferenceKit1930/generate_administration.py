"""Isolated administration concept rebuild. Blender 5; no other kit output touched.

Run: blender --background --python ArtSource/ReferenceKit1930/generate_administration.py
Optional -- --no-render exports without rendering. All dimensions are metres.
"""
import bpy
import math
import json
import sys
import shutil
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0,str(HERE))
from administration_materials import prepare, remap
ROOT = HERE.parents[1]
OUT = ROOT / 'ArtExports/ReferenceKit1930'
NAME = 'StudioAdministration_01'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new(NAME)
scene.collection.children.link(asset)
root = bpy.data.objects.new(NAME + '_ROOT', None)
asset.objects.link(root)
materials = {}
groups = {}

def part_group(name, mat):
    if 'Cavity' in name or name.startswith('Lobby'): return 'TemporaryWindowBackings'
    if mat == 'ClearGlass': return 'Glass'
    if name.startswith('Sign'): return 'SignageArea'
    if name.startswith(('Escape', 'RearEscape', 'RearRail', 'RearTop', 'Maintenance')): return 'FireEscape'
    if name.startswith(('Roof', 'Vent')) or (any(s in name for s in ('Parapet', 'Coping', 'InnerFlashing')) and not name.startswith('Tower')): return 'Roof'
    if any(s in name for s in ('ServiceDoor', 'Door_', 'DoorTransom', 'Transom', 'Pull_', 'OuterHinge', 'FixedSidelight')): return 'Doors'
    if any(s in name for s in ('RevealJamb', 'Sash', 'Mullion', 'Muntin', 'ProjectingSill')) or name.endswith('_Head'): return 'Windows'
    if 'Masonry' in name or name in ('Footing','GroundFloor','FirstFloorSlab') or name.startswith('Tower'): return 'ExteriorShell'
    return 'ArchitecturalDetails'

def material(name, color, rough=.7, metal=0, grain=0):
    m = bpy.data.materials.new('ADM_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    if grain:
        n, links = m.node_tree.nodes, m.node_tree.links
        coord = n.new('ShaderNodeTexCoord')
        noise = n.new('ShaderNodeTexNoise')
        noise.inputs['Scale'].default_value = 3.5
        noise.inputs['Detail'].default_value = 3
        links.new(coord.outputs['Object'], noise.inputs['Vector'])
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].color = (*(v*.90 for v in color), 1)
        ramp.color_ramp.elements[1].color = (*(min(1,v*1.06) for v in color), 1)
        links.new(noise.outputs['Fac'], ramp.inputs[0])
        links.new(ramp.outputs[0], p.inputs['Base Color'])
        fine = n.new('ShaderNodeTexNoise')
        fine.inputs['Scale'].default_value = 140
        links.new(coord.outputs['Object'], fine.inputs['Vector'])
        bump = n.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .22
        bump.inputs['Distance'].default_value = grain
        links.new(fine.outputs['Fac'], bump.inputs['Height'])
        links.new(bump.outputs['Normal'], p.inputs['Normal'])
    materials[name] = m
    return m

material('Stucco', (.66,.535,.365), .82, grain=.002)
material('Limestone', (.74,.65,.49), .72, grain=.0012)
# Muted warm bronze/terracotta, judged against the approved concept in Unity.
material('Bronze', (.40,.245,.135), .24, 1.0)
material('WindowSteel', (.055,.069,.064), .34, .65)
material('CanopyEnamel', (.035,.042,.041), .34, .45)
material('DoorWood', (.12,.068,.032), .43, grain=.0005)
material('Interior', (.25,.23,.19), .95)
material('Roof', (.10,.095,.085), .88, grain=.002)
material('LampDiffuser', (.95,.70,.34), .36)
material('Clay', (.56,.55,.52), .7)
glass = material('ClearGlass', (.92,.975,.99), .095)
glass.node_tree.nodes['Principled BSDF'].inputs['Transmission Weight'].default_value = 1
glass.node_tree.nodes['Principled BSDF'].inputs['IOR'].default_value = 1.46

if '--materials-only' in sys.argv:
    # Load accepted geometry; only refresh the authoritative material definitions.
    import hashlib
    definitions={key:(tuple(m.diffuse_color),float(m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value),
                      float(m.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value))
                 for key,m in materials.items() if key!='Clay'}
    blend_path=HERE/(NAME+'.blend')
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))
    def geometry_signature():
        data=[(o.name,tuple(tuple(row) for row in o.matrix_world),
               tuple(tuple(v.co) for v in o.data.vertices),
               tuple(tuple(p.vertices) for p in o.data.polygons),
               tuple(m.name for m in o.data.materials))
              for o in sorted(bpy.data.objects,key=lambda o:o.name) if o.type=='MESH']
        return hashlib.sha256(repr(data).encode()).hexdigest()
    before=geometry_signature()
    materials={}
    for key,(color,rough,metal) in definitions.items():
        m=bpy.data.materials['ADM_'+key]
        m.diffuse_color=color
        p=m.node_tree.nodes['Principled BSDF']
        p.inputs['Base Color'].default_value=color
        p.inputs['Roughness'].default_value=rough
        p.inputs['Metallic'].default_value=metal
        materials[key]=m
    prepare(ROOT,materials,preserve_textures=True)
    assert geometry_signature()==before, 'Material-only refresh changed geometry'
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    print('MATERIAL_ONLY_GEOMETRY_SHA256 '+before)
    print('Updated Blender and Unity materials/textures; FBXs and importer mappings untouched.')
    raise SystemExit(0)

def assign(o, name, mat):
    o.name = name
    for c in list(o.users_collection): c.objects.unlink(o)
    asset.objects.link(o)
    category = part_group(name, mat)
    if category not in groups:
        group = bpy.data.objects.new(category, None)
        asset.objects.link(group)
        group.parent = root
        groups[category] = group
    o.parent = groups[category]
    o.data.materials.append(materials[mat])
    return o

def bevel(o, width):
    if width:
        b = o.modifiers.new('Built edge radius', 'BEVEL')
        b.width = width
        b.segments = 2
        b.harden_normals = True
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=b.name)
        for f in o.data.polygons: f.use_smooth = True
        n = o.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
        n.keep_sharp = True
        n.weight = 40
        bpy.ops.object.modifier_apply(modifier=n.name)

def box(name, pos, size, mat='Stucco', edge=.018):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = assign(bpy.context.object, name, mat)
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(o, min(edge, min(size)*.22))
    return o

def rod(name, a, b, radius, mat='Bronze', vertices=12):
    d = Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=d.length,
                                      location=(Vector(a)+Vector(b))/2)
    o = assign(bpy.context.object, name, mat)
    o.rotation_euler = d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel(o, min(.004, radius*.2))
    return o

# Facade coordinates: u horizontal, d inward from exterior face, z up.
def facade(name, origin, angle):
    ca, sa = math.cos(angle), math.sin(angle)
    def point(u,d,z): return (origin[0]+u*ca-d*sa, origin[1]+u*sa+d*ca,z)
    def block(label,u,d,z,w,t,h,mat='Stucco',edge=.016):
        o=box(name+'_'+label,point(u,d,z),(w,t,h),mat,edge)
        o.rotation_euler.z=angle
        return o
    return point, block

def perforated_wall(f, width, bottom, top, openings):
    # Tile the wall around openings. No solid shell behind windows or doors.
    xs=sorted(set([-width/2,width/2]+[v for x,z,w,h in openings for v in (x-w/2,x+w/2)]))
    zs=sorted(set([bottom,top]+[v for x,z,w,h in openings for v in (z-h/2,z+h/2)]))
    for a,b in zip(xs,xs[1:]):
        for c,d in zip(zs,zs[1:]):
            if not (-width/2<=a<b<=width/2 and bottom<=c<d<=top): continue
            x,z=(a+b)/2,(c+d)/2
            if any(abs(x-ox)<w/2 and abs(z-oz)<h/2 for ox,oz,w,h in openings): continue
            f('Masonry',x,.19,z,b-a,.38,d-c,edge=0)

def window(p,f,u,z,w=1.15,h=2.05):
    # Backing front at .785: 547 mm clear behind inner glass face (.238).
    # Every cavity component belongs to the removable backing group.
    f('CavityBack',u,.8075,z,w+.08,.045,h+.08,'Interior',.002)
    for side in (-1,1):
        f('CavitySide',u+side*(w/2+.015),.5125,z,.045,.545,h,'Interior',.002)
        f('RevealJamb',u+side*(w/2+.095),.06,z,.18,.45,h+.30,'Limestone',.012)
        f('SashStile',u+side*(w/2-.045),.19,z,.075,.085,h,'WindowSteel',.007)
    for side in (-1,1):
        f('CavityFloor',u,.5125,z+side*(h/2+.01),w,.545,.04,'Interior',.002)
        f('SashRail',u,.19,z+side*(h/2-.045),w-.15,.085,.075,'WindowSteel',.007)
    f('Glass',u,.235,z,w-.10,.006,h-.10,'ClearGlass',.001)
    f('CenterMullion',u,.18,z,.036,.068,h-.10,'WindowSteel',.006)
    for offset in (-.55,0,.55):
        f('HorizontalMuntin',u,.175,z+offset,w-.15,.068,.026,'WindowSteel',.004)
    f('ProjectingSill',u,-.06,z-h/2-.07,w+.34,.52,.13,'Limestone',.017)
    f('Head',u,.005,z+h/2+.085,w+.29,.36,.17,'Limestone',.012)

def ring_course(name,z,height,inner,outer,mat='Limestone',gaps=None):
    """One connected mitred ring, optionally interrupted at the two doorways.

    Shared corner vertices avoid overlapping boxes and internal coplanar faces.
    Boundary edges alone receive vertical faces, including intentional end caps.
    """
    ix,iy=inner; ox,oy=outer
    patches=[[(ox,-oy),(ox,oy),(ix,iy),(ix,-iy)],
             [(-ox,oy),(-ox,-oy),(-ix,-iy),(-ix,iy)]]
    for sign in (-1,1):
        gap=(gaps or {}).get(sign,0)
        if gap:
            patches.extend([[(-ox,sign*oy),(-gap,sign*oy),(-gap,sign*iy),(-ix,sign*iy)],
                            [(gap,sign*oy),(ox,sign*oy),(ix,sign*iy),(gap,sign*iy)]])
        else:
            patches.append([(-ox,sign*oy),(ox,sign*oy),(ix,sign*iy),(-ix,sign*iy)])
    points=[]; faces=[]; edges={}
    for polygon in patches:
        if sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(polygon,polygon[1:]+polygon[:1]))<0:
            polygon.reverse()
        ids=[]
        for p in polygon:
            if p not in points: points.append(p)
            ids.append(points.index(p))
        faces.append(ids)
        for a,b in zip(ids,ids[1:]+ids[:1]):
            key=tuple(sorted((a,b)))
            edges.setdefault(key,[]).append((a,b))
    n=len(points)
    vertices=[(x,y,z+dz) for dz in (-height/2,height/2) for x,y in points]
    mesh_faces=[list(reversed(f)) for f in faces]+[[i+n for i in f] for f in faces]
    for uses in edges.values():
        if len(uses)==1:
            a,b=uses[0]; mesh_faces.append([a,b,b+n,a+n])
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],mesh_faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); asset.objects.link(obj)
    assign(obj,name,mat)
    bevel(obj,min(.012,height*.1))
    return obj

# Main two-storey shell: generous but modest 18 x 11 m footprint.
box('Footing',(0,0,.27),(18.35,11.35,.54),'Limestone',.035)
box('GroundFloor',(0,0,.54),(17.7,10.7,.16),'Limestone',.02)
box('FirstFloorSlab',(0,0,3.84),(17.7,10.7,.18),'Interior',.008)
front_p,front=facade('Front',(0,-5.5),0)
back_p,back=facade('Rear',(0,5.5),math.pi)
left_p,left=facade('Left',(-9,0),-math.pi/2)
right_p,right=facade('Right',(9,0),math.pi/2)
front_open=[(x,z,1.15,2.05) for x in (-7.05,-4.80,4.80,7.05) for z in (1.92,5.30)]
front_open.append((0,1.88,2.65,2.72))
perforated_wall(front,18,.54,7.50,front_open)
for x,z,w,h in front_open[:-1]: window(front_p,front,x,z,w,h)
for p,f,width,xs in ((back_p,back,18,(-7.1,-4.7,-2.35,2.35,4.7,7.1)),
                     (left_p,left,10.24,(-3.2,0,3.2)),(right_p,right,10.24,(-3.2,0,3.2))):
    openings=[(x,z,1.10,2.05) for x in xs for z in (1.92,5.30)]
    if f is back:
        openings.extend([(0,1.59,1.05,2.10),(0,4.98,1.05,2.10)])
    perforated_wall(f,width,.54,7.50,openings)
    for x,z,w,h in openings:
        if h>2.1: continue
        if h==2.1:
            f('ServiceDoor',x,.12,z,w,.07,h,'DoorWood',.01)
            # Inward-opening service leaf: lever on latch stile, hinges opposite.
            for side in (-1,1):
                f('ServiceDoorFrame',x+side*(w/2+.055),.06,z,.10,.20,h,'Limestone',.01)
            f('ServiceDoorHeader',x,.06,z+h/2+.055,w+.21,.20,.10,'Limestone',.01)
            f('ServiceDoorThreshold',x,-.02,z-h/2-.025,w+.16,.36,.05,'Limestone',.007)
            handle_z=z-h/2+1.05
            f('ServiceDoorHandlePlate',x-.36,.075,handle_z,.07,.02,.19,'Bronze',.006)
            rod('ServiceDoorSpindle',p(x-.36,.075,handle_z),p(x-.36,-.025,handle_z),.016)
            rod('ServiceDoorLever',p(x-.36,-.025,handle_z),p(x-.20,-.025,handle_z),.016)
            for dz in (-.82,0,.82):
                axis=x+w/2+.0025
                f('ServiceDoorHingeLeaf',axis-.028,.159,z+dz,.056,.008,.11,'Bronze',.002)
                f('ServiceDoorHingeJamb',axis+.028,.164,z+dz,.056,.018,.11,'Bronze',.002)
                rod('ServiceDoorHinge',p(axis,.17,z+dz-.055),p(axis,.17,z+dz+.055),.018)
        else: window(p,f,x,z,w,h)
for z,t,h in ((.80,.15,.32),(7.0,.16,.16),(7.55,.33,.22)):
    ring_course('WrappingStoneCourse',z,h,(8.94,5.44),(9+t-.06,5.5+t-.06),
                gaps={-1:1.40,1:.575} if z==.80 else None)

# Stepped Deco entrance tower built as a shallow, hollow architectural projection.
# Adjacent fins rise progressively toward the central bronze crest.
for side in (-1,1):
    for idx,(x,width,height,depth) in enumerate(((2.65,.65,8.45,.65),(1.99,.62,9.38,.82),(1.28,.72,10.18,1.00))):
        if idx==2:
            # The inner fin must clear the portal below its head, including
            # the outward door swing. Preserve the accepted upper tower mass.
            split=3.39
            box('Tower_Pier',(side*x,-5.5-depth/2,(height+split)/2),(width,depth,height-split),'Stucco',.025)
            # Existing PortalStoneJamb supplies the lower support. A second
            # return here would duplicate its inner face and cause z-fighting.
        else:
            box('Tower_Pier', (side*x,-5.5-depth/2,height/2+.27),(width,depth,height-.54),'Stucco',.025)
        box('Tower_Coping',(side*x,-5.5-depth/2,height+.04),(width+.10,depth+.12,.14),'Limestone',.012)
        box('IncisedDecoLine',(side*(x+.12),-5.51-depth,height-.95),(.035,.018,1.4),'Limestone',.004)
box('Tower_Center',(0,-5.85,7.8),(1.85,.55,5.30),'Stucco',.025)
for x,h in ((-.36,10.72),(0,11.03),(.36,10.58)):
    box('BronzeVerticalCrest',(x,-6.25,(h+5.25)/2),(.20,.21,h-5.25),'Bronze',.012)
for x in (-.65,.65):
    box('TowerFlute',(x,-6.17,7.80),(.055,.12,4.45),'Limestone',.009)

# Entrance portal has clear space through to a shallow lobby preview cavity.
for x in (-1.58,1.58):
    box('PortalStoneJamb',(x,-6.15,1.94),(.38,1.00,2.80),'Limestone',.025)
box('PortalHead',(0,-6.15,3.25),(3.55,1.00,.28),'Limestone',.02)
box('LobbyBacking',(0,-3.65,1.94),(2.68,.10,2.8),'Interior',.005)
box('LobbyCeiling',(0,-4.75,3.25),(2.70,2.30,.1),'Interior',.005)

# Two 0.954 m leaves, outer hinges, outward swing toward the landing.
# Exterior pulls sit near the meeting stiles; opening would draw leaves toward user.
# Landing +0.60 m, 2.15 m door height; pulls centered 1.05 m above landing.
landing=.60
door_y=-5.82
for side in (-1,1):
    # 6 mm center seam; 3 mm clearance at each outer jamb.
    cx=side*.480
    outer=side*.9585
    for x in (cx-.4345,cx+.4345): box('Door_Stile',(x,door_y,landing+1.075),(.085,.09,2.15),'DoorWood',.004)
    for z,h in ((landing+.09,.18),(landing+.76,.12),(landing+2.105,.09)):
        box('Door_Rail',(cx,door_y,z),(.784,.09,h),'DoorWood',.004)
    box('Door_LowerPanel',(cx,door_y+.012,landing+.44),(.784,.055,.52),'DoorWood',.004)
    # Inset panel mouldings and a slim bronze kick plate, restrained period detail.
    for dx in (-.34,.34):
        box('Door_PanelBead',(cx+dx,door_y-.027,landing+.44),(.025,.022,.46),'DoorWood',.004)
    for z in (landing+.21,landing+.67):
        box('Door_PanelBead',(cx,door_y-.027,z),(.68,.022,.025),'DoorWood',.004)
    box('Door_KickPlate',(cx,door_y-.051,landing+.09),(.70,.016,.11),'Bronze',.004)
    box('Door_ClearGlass',(cx,door_y+.014,2.04),(.784,.006,1.24),'ClearGlass',0)
    box('Door_GlazingMullion',(cx,door_y-.005,2.04),(.032,.032,1.24),'Bronze',.002)
    handle_z=landing+1.05
    # Pulls sit on the solid meeting stiles, not in the glass aperture.
    handle_x=side*.0455
    box('Pull_Backplate',(handle_x,door_y-.053,handle_z),(.062,.016,.29),'Bronze',.003)
    for dz in (-.11,.11):
        rod('Pull_StandOff',(handle_x,door_y-.061,handle_z+dz),(handle_x,door_y-.123,handle_z+dz),.012)
    rod('Pull_Grip',(handle_x,door_y-.123,handle_z-.12),(handle_x,door_y-.123,handle_z+.12),.014)
    for z in (landing+.22,landing+1.02,landing+1.93):
        box('OuterHingeLeaf',(outer-side*.029,door_y-.049,z),(.058,.008,.11),'Bronze',.001)
        box('OuterHingeJamb',(outer+side*.029,door_y-.054,z),(.058,.008,.11),'Bronze',.001)
        rod('OuterHinge',(outer,door_y-.055,z-.055),(outer,door_y-.055,z+.055),.018)
for x in (-1.065,1.065):
    box('FixedSidelightFrame',(x,door_y,landing+1.075),(.21,.10,2.15),'DoorWood',.003)
# Deep jambs close the unintended gap between the leaf assembly and masonry.
for side in (-1,1):
    box('Door_RecessJamb',(side*1.255,-5.49,1.90),(.18,.76,2.72),'DoorWood',.009)
    box('Door_FrameStop',(side*1.164,-5.79,1.90),(.025,.055,2.66),'Bronze',.004)
box('Door_RecessHead',(0,-5.49,3.235),(2.68,.76,.06),'DoorWood',.009)
box('Door_Threshold',(0,-5.68,.575),(2.68,.98,.05),'Limestone',.008)
for z,h in ((2.79,.08),(3.175,.06)): box('DoorTransomRail',(0,door_y,z),(2.30,.10,h),'DoorWood',.008)
for x in (-1.13,-.375,.375,1.13): box('DoorTransomStile',(x,door_y,2.9875),(.05,.10,.315),'DoorWood',.002)
for a,b in ((-1.105,-.4),(-.35,.35),(.4,1.105)):
    box('TransomGlass',((a+b)/2,door_y+.014,2.9875),(b-a,.006,.315),'ClearGlass',0)

# Landing and broad shallow risers meet the building floor without burying doors.
box('EntranceLanding',(0,-6.4375,.30),(4.48,1.525,.60),'Limestone',.008)
for i in range(4):
    h=(4-i)*.15
    box('EntranceTread',(0,-7.35-i*.32,h/2),(max(4.48,4.25+i*.10),.30 if i==0 else .36,h),'Limestone',.008)

def entrance_return(name,side,inner,outer,front_y,bottom,top,mat):
    # Follow the actual two pilaster faces and narrow recess between them.
    # These are butt joints, not boxes extended through the facade.
    outline=[(inner,front_y),(outer,front_y),(outer,-6.15),
             (2.325,-6.15),(2.325,-5.5),(2.3,-5.5),
             (2.3,-6.32),(inner,-6.32)]
    outline=[(side*x,y) for x,y in outline]
    if side<0: outline.reverse()
    n=len(outline)
    vertices=[(x,y,z) for z in (bottom,top) for x,y in outline]
    faces=[list(reversed(range(n))),list(range(n,2*n))]
    faces += [[i,(i+1)%n,(i+1)%n+n,i+n] for i in range(n)]
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); asset.objects.link(obj)
    assign(obj,name,mat)
    bevel(obj,.004)

for side in (-1,1):
    # Continuous base meets the building footing below the raised pilasters.
    box('StairCheekBase',(side*2.42,-6.8625,.27),(.36,2.375,.54),'Stucco',.004)
    entrance_return('StairCheek',side,2.24,2.60,-8.05,.54,.92,'Stucco')
    entrance_return('StairCheekCap',side,2.205,2.635,-8.085,.92,.985,'Limestone')

# Cantilevered steel canopy with bronze rim and visible soffit framing.
box('Canopy',(0,-6.75,3.46),(4.55,1.75,.22),'CanopyEnamel',.028)
for z in (3.37,3.56):
    box('CanopyBronzeLip',(0,-7.635,z),(4.59,.05,.035),'Bronze',.007)
    for x in (-2.27,2.27): box('CanopySideLip',(x,-6.75,z),(.05,1.75,.035),'Bronze',.007)
for x in (-1.7,0,1.7): box('CanopyUnderBeam',(x,-6.7,3.32),(.075,1.45,.10),'WindowSteel',.012)
# Panel sits ahead of all fins. Its central 3.8 x .86 m face is uninterrupted.
# Future dynamic lettering belongs to Unity; no company name is baked here.
box('SignStonePanel',(0,-6.55,4.28),(4.08,.34,1.14),'Limestone',.022)
for z in (3.74,4.82): box('SignPanelBorder',(0,-6.74,z),(4.05,.08,.05),'Stucco',.008)

for side in (-1,1):
    x=side*1.93
    box('LanternBackplate',(x,-6.14,2.36),(.22,.10,.95),'Bronze',.014)
    box('LanternDiffuser',(x,-6.32,2.36),(.18,.19,.73),'LampDiffuser',.01)
    for dx in (-.12,.12): box('LanternCage',(x+dx,-6.43,2.36),(.026,.025,.85),'Bronze',.004)
    for z in (1.94,2.36,2.78): box('LanternRail',(x,-6.43,z),(.27,.035,.035),'Bronze',.005)
    for z in (1.90,2.82): box('LanternCap',(x,-6.31,z),(.32,.32,.09),'Bronze',.009)

# Roof with four parapet walls, sheltered felt deck and practical rear utilities.
box('RoofDeck',(0,0,7.38),(17.5,10.5,.18),'Roof',.006)
ring_course('RoofParapet',7.89,.78,(8.6,5.1),(9,5.5),'Stucco')
ring_course('RoofCoping',8.31,.13,(8.53,5.03),(9.09,5.59))
ring_course('RoofInnerFlashing',7.59,.29,(8.555,5.055),(8.6,5.1),'WindowSteel')
for x in (-6,6):
    box('RoofVentCurb',(x,2.2,7.685),(1.4,1.1,.43),'Limestone',.01)
    box('VentHood',(x,2.2,8.04),(1.54,1.23,.23),'WindowSteel',.035)
    for y in (1.8,2,2.2,2.4,2.6): box('VentLouvre',(x,y,7.98),(1.28,.05,.16),'WindowSteel',.008)
box('RoofAccess',(0,3.6,8.11),(2.35,2.20,1.42),'Stucco',.025)
box('RoofAccessCoping',(0,3.6,8.87),(2.48,2.34,.16),'Limestone',.02)
for x in (-8.5,8.5):
    rod('RainwaterPipe',(x,5.68,.4),(x,5.68,7.65),.045,'Bronze')
    for z in (1.4,3.5,5.6): box('PipeBracket',(x,5.60,z),(.16,.19,.06),'WindowSteel',.009)

# Upper threshold 3.93 -> 19 equal risers -> .54 landing -> 3 steps -> ground.
# Rear doors open inward, keeping the 1.65 m landing depth clear.
def guard(a,b,flight=False):
    rod('EscapeGuardTop',a,b,.025,'WindowSteel')
    # Flight balusters seat in a continuous lower rail on the stringer.
    bottom_offset=1.18 if flight else 1.10
    if flight:
        rod('EscapeGuardBottom',Vector(a)-Vector((0,0,bottom_offset)),
            Vector(b)-Vector((0,0,bottom_offset)),.024,'WindowSteel')
    length=(Vector(b)-Vector(a)).length
    for i in range(math.ceil(length/.13)+1):
        t=i/max(1,math.ceil(length/.13))
        top=Vector(a).lerp(Vector(b),t)
        rod('EscapeGuardUpright',top-Vector((0,0,bottom_offset)),top,.012,'WindowSteel',8)

box('RearEscapeUpperLanding',(0,6.325,3.85),(2.7,1.65,.16),'WindowSteel',.012)
guard((-1.30,7.10,5.03),(1.30,7.10,5.03))
guard((-1.30,5.60,5.03),(-1.30,7.10,5.03))
rise=(3.93-.54)/19
run=.26
for i in range(19):
    x=1.35+(i+.5)*run
    top=3.93-(i+1)*rise
    box('EscapeTread',(x,6.525,top-.035),(.26,1.05,.07),'WindowSteel',.008)
end=1.35+19*run
for y in (5.98,7.07):
    rod('EscapeStringer',(1.35,y,3.85),(end,y,.46),.05,'WindowSteel')
    guard((1.35,y,5.03),(end,y,1.64),flight=True)
rod('EscapeUpperGuardReturn',(1.30,7.10,5.03),(1.35,7.07,5.03),.025,'WindowSteel')
box('RearEscapeLowerLanding',(end+.5,6.525,.46),(1.0,1.25,.16),'WindowSteel',.012)
guard((end+.95,5.98,1.64),(end+.95,7.10,1.64))
guard((end,5.98,1.64),(end+.95,5.98,1.64))
rod('EscapeLowerGuardReturn',(end,7.07,1.64),(end+.05,7.10,1.64),.025,'WindowSteel')
for x in (-1.15,1.15,end+.85):
    box('EscapeSupportFoot',(x,6.95,.02),(.16,.16,.04),'WindowSteel',.003)
    rod('EscapeSupport',(x,6.95,.04),(x,6.95,3.77 if x<2 else .38),.045,'WindowSteel')

def ground_steps(prefix,x):
    for i in range(3):
        top=.54-i*.18
        box(prefix+'Step',(x,7.30+i*.28,top/2),(1.0,.30,top),'Limestone',.012)
    for side in (-1,1):
        # Posts stand within landing/tread footprints, never beside them.
        rail_x=x+side*.45
        rod('EscapeGroundHandrail',(rail_x,7.10,1.64),(rail_x,7.86,1.28),.024,'WindowSteel')
        for y,z in ((7.10,.54),(7.86,.18)):
            box('EscapeGroundPostFoot',(rail_x,y,z+.008),(.065,.065,.016),'WindowSteel',.002)
            rod('EscapeGroundPost',(rail_x,y,z+.016),(rail_x,y,z+1.1),.018,'WindowSteel')
ground_steps('EscapeFinal',end+.5)
# Lower service door has its own threshold-height landing and short front flight.
box('RearEscapeServiceLanding',(0,6.325,.27),(1.65,1.65,.54),'Limestone',.018)
for side in (-1,1):
    guard((side*.80,5.6,1.64),(side*.80,7.1,1.64))
    guard((side*.80,7.1,1.64),(side*.45,7.1,1.64))
ground_steps('EscapeService',0)

# Ground-accessible maintenance ladder beside the window bays; 280 mm rungs.
# Raised handholds return over the parapet, with inside rungs down to roof level.
for x in (-8.08,-7.62):
    box('MaintenanceLadderFoot',(x,5.94,.015),(.13,.13,.03),'WindowSteel',.003)
    rod('MaintenanceLadderRail',(x,5.94,.03),(x,5.94,9.40),.025,'WindowSteel')
    rod('MaintenanceLadderReturn',(x,5.94,9.40),(x,4.82,9.40),.025,'WindowSteel')
    box('MaintenanceRoofFoot',(x,4.82,7.485),(.13,.13,.03),'WindowSteel',.003)
    rod('MaintenanceLadderInside',(x,4.82,7.50),(x,4.82,9.40),.025,'WindowSteel')
    for z in (1,3.5,5,7.4): rod('MaintenanceBracket',(x,5.50,z),(x,5.94,z),.024,'WindowSteel')
for i in range(30):
    z=.28+i*.28
    rod('MaintenanceRung',(-8.08,5.94,z),(-7.62,5.94,z),.018,'WindowSteel')
for z in (7.72,8.0,8.28,8.56):
    rod('MaintenanceInsideRung',(-8.08,4.82,z),(-7.62,4.82,z),.018,'WindowSteel')

# Export only this building. Review lights, ground and scale marker added afterward.
bpy.context.view_layer.update()
objects=[o for o in asset.objects if o.type=='MESH']
def world_bounds(o):
    points=[o.matrix_world@Vector(c) for c in o.bound_box]
    return ([min(p[k] for p in points) for k in range(3)],
            [max(p[k] for p in points) for k in range(3)])

# Authoring QA: include ladder brackets as well as rails, treads and platforms.
rear_windows=[o for o in objects if o.name.startswith('Rear_') and o.parent.name=='Windows']
for access in (o for o in objects if o.parent.name=='FireEscape'):
    lo,hi=world_bounds(access)
    for window_part in rear_windows:
        other_lo,other_hi=world_bounds(window_part)
        if all(min(hi[k],other_hi[k])-max(lo[k],other_lo[k])>.002 for k in range(3)):
            raise RuntimeError('Access intersects window: '+access.name+' / '+window_part.name)
for o in objects:
    # Deterministic metre-based planar UVs for tiled Unity materials.
    uv=o.data.uv_layers.new(name='UVMap') if not o.data.uv_layers else o.data.uv_layers.active
    for face in o.data.polygons:
        world_normal=o.matrix_world.to_3x3() @ face.normal
        axis=max(range(3),key=lambda k:abs(world_normal[k]))
        axes=[k for k in range(3) if k!=axis]
        for li in face.loop_indices:
            co=o.matrix_world @ o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(co[axes[0]],co[axes[1]])
material_records=prepare(ROOT,materials,preserve_textures=True)
# Editable components stay grouped in Blender. Export one multi-material mesh
# per semantic part, with a shared ground pivot and independently removable backs.
export_objects=[]
for category,parent in groups.items():
    group=[o for o in objects if o.parent==parent]
    if not group: continue
    bpy.ops.object.select_all(action='DESELECT')
    copies=[]
    for original in group:
        copy=original.copy()
        copy.data=original.data.copy()
        asset.objects.link(copy)
        copies.append(copy)
        copy.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    if len(copies)>1:
        bpy.ops.object.join()
    combined=bpy.context.object
    combined.parent=root
    scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    combined.name='Export_'+category
    export_objects.append(combined)
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for o in export_objects: o.select_set(True)
bpy.context.view_layer.objects.active=root
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/(NAME+'.fbx')),use_selection=True,
    object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
    add_leaf_bones=False,bake_anim=False,use_custom_props=True)
export_mesh_count=len(export_objects)
for o in export_objects: bpy.data.objects.remove(o,do_unlink=True)
shutil.copy2(OUT/(NAME+'.fbx'),ROOT/'Assets/SilverScreen/Environment/ReferenceKit/Models'/ (NAME+'.fbx'))
remap(ROOT,material_records)
points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
report={'asset':NAME,'blender':bpy.app.version_string,'unit':'metre',
        'dimensions_m':[round(max(p[k] for p in points)-min(p[k] for p in points),3) for k in range(3)],
        'triangles':sum(len(p.vertices)-2 for o in objects for p in o.data.polygons),
        'source_mesh_count':len(objects),'export_mesh_count':export_mesh_count,'root_location':list(root.location),
        'materials':[m.name for m in materials.values()],
        'door':{'landing_height_m':.60,'clear_leaf_height_m':2.15,'pull_center_above_landing_m':1.05,
                'hinges':'connected leaf and jamb plates, exterior axis','opening':'outward toward landing',
                'center_seam_m':.006,'outer_jamb_gap_m':.003,'grip_clearance_m':.064,
                'glass_aperture_m':[.784,1.24]},
        'glass':{'thickness_m':.006,'ior':1.46,'transmission':1.0,'roughness':.095,
                 'window_recess_m':.235,'backing_front_depth_m':.785,'clear_cavity_m':.547},
        'export_groups':list(groups),
        'signage':{'baked_text':False,'usable_width_m':3.8,'usable_height_m':.86},
        'rear_access':{'upper_threshold_m':3.93,'lower_threshold_m':.54,
                       'stair_riser_m':rise,'stair_going_m':run,'clear_width_m':1.05,
                       'guard_height_m':1.10,'final_riser_m':.18,'ladder_rung_spacing_m':.28}}
(HERE/'administration_report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')

# Cycles provides true glazing transmission and realistic indirect light.
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.cycles.max_bounces=8
scene.cycles.transmission_bounces=6
scene.render.resolution_x=1400
scene.render.resolution_y=1100
scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('CaliforniaDay')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.62,.75,1,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
bpy.ops.object.light_add(type='SUN',location=(0,0,20))
sun=bpy.context.object
sun.rotation_euler=(math.radians(27),math.radians(-26),math.radians(-32))
sun.data.energy=2.6
sun.data.angle=math.radians(8)
sun.data.color=(1,.86,.68)
bpy.ops.object.light_add(type='AREA',location=(3,-14,10))
fill=bpy.context.object
fill.data.energy=650
fill.data.shape='DISK'
fill.data.size=9
fill.rotation_euler=(Vector((0,0,4))-fill.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200)
ground=bpy.context.object
ground.name='REVIEW_Ground'
ground.location.z=-.03
ground.data.materials.append(materials['Limestone'])
bpy.ops.object.camera_add()
camera=bpy.context.object
scene.camera=camera
def render(suffix,pos,target,lens=52):
    if '--entrance-qa' in sys.argv and not suffix.startswith('qa_front_steps'): return
    if '--connectivity-qa' in sys.argv and not suffix.startswith('qa_'): return
    camera.location=pos
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.lens=lens
    scene.render.filepath=str(HERE/(NAME+'_'+suffix+'.png'))
    if '--no-render' not in sys.argv: bpy.ops.render.render(write_still=True)

render('hero',(24,-32,17),(0,-.7,4.35),52)
for o in objects:
    if o.name.startswith('StudioLettering'): o.hide_render=True
scene.view_layers[0].material_override=materials['Clay']
render('clay',(24,-32,17),(0,-.7,4.35),52)
scene.view_layers[0].material_override=None
for o in objects: o.hide_render=False
render('detail',(7.8,-15.5,5.8),(0,-5.8,2.8),55)
render('signage',(5,-13,5.6),(0,-6.6,4.3),65)
render('window',(7.4,-9,2.6),(4.8,-5.2,1.95),55)

# A dimension ruler/silhouette only, excluded from FBX and building collection.
before=set(bpy.data.objects)
rod('ScaleReference_LegL',(-3.8,-8.5,.08),(-3.8,-8.5,.90),.07,'Clay')
rod('ScaleReference_LegR',(-3.6,-8.5,.08),(-3.6,-8.5,.90),.07,'Clay')
box('ScaleReference_Torso',(-3.7,-8.5,1.15),(.38,.20,.60),'Clay',.06)
rod('ScaleReference_ArmL',(-3.95,-8.5,.90),(-3.95,-8.5,1.42),.06,'Clay')
rod('ScaleReference_ArmR',(-3.45,-8.5,.90),(-3.45,-8.5,1.42),.06,'Clay')
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.14,location=(-3.7,-8.5,1.66))
bpy.context.object.data.materials.append(materials['Clay'])
scale_objects=set(bpy.data.objects)-before
render('human_scale',(7.8,-17,5.2),(-.7,-6.2,2.1),52)
for o in scale_objects: o.location+=Vector((3.7,17,0))
render('rear_access',(-10,19,8),(1.8,6.3,2.5),52)
for o in scale_objects: bpy.data.objects.remove(o,do_unlink=True)
render('rear',(-24,30,16),(0,0,4.2),52)
render('fire_escape',(13,18,9),(2.7,6.4,2.4),52)
render('left_side',(-24,-14,9),(-3,0,4),52)
render('right_side',(25,10,9),(3,0,4),52)
render('street',(12,-25,2.1),(0,-4,3.4),48)
render('qa_entrance',(2.7,-11,3.2),(0,-5.8,1.95),55)
render('qa_lower_escape',(10.6,11.5,3.6),(6.65,6.7,.9),52)
render('qa_upper_escape',(4.5,12.5,7.1),(.8,6.3,3.5),48)
for side,x in (('left',-9),('right',9)):
    for end,y in (('front',-5.5),('rear',5.5)):
        render('qa_corner_'+side+'_'+end,(x*1.6,y*2.1,8.3),(x,y,4.25),32)
render('qa_roof',(14,21,20),(0,1,7.6),48)
render('qa_front_steps',(5,-11,3.2),(1.4,-6.8,.7),52)
render('qa_front_steps_rear',(5.4,-6.5,1.65),(2.4,-6.1,.6),55)
render('qa_front_steps_left',(-5.4,-6.5,1.65),(-2.4,-6.1,.6),55)
camera.location=(24,-32,17)
camera.rotation_euler=(Vector((0,-.7,4.35))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(NAME+'.blend')))
print('ADMINISTRATION_REPORT '+json.dumps(report))
