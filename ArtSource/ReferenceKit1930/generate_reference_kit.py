import bpy
import json
import math
import mathutils
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource" / "ReferenceKit1930"
EXPORT = ROOT / "ArtExports" / "ReferenceKit1930"
SOURCE.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1.0

PALETTE = {
    "MAT_StuccoWarm": (0.63, 0.50, 0.34, 1),
    "MAT_StuccoCream": (0.78, 0.70, 0.56, 1),
    "MAT_BrickWarm": (0.40, 0.18, 0.11, 1),
    "MAT_WoodDark": (0.20, 0.10, 0.05, 1),
    "MAT_WoodPainted": (0.35, 0.43, 0.38, 1),
    "MAT_MetalDark": (0.10, 0.11, 0.11, 1),
    "MAT_MetalPainted": (0.26, 0.31, 0.29, 1),
    "MAT_GlassSmoked": (0.06, 0.13, 0.15, 1),
    "MAT_RoofCharcoal": (0.15, 0.14, 0.13, 1),
    "MAT_RoofTerracotta": (0.48, 0.18, 0.09, 1),
    "MAT_FabricBurgundy": (0.34, 0.08, 0.06, 1),
    "MAT_Concrete": (0.48, 0.46, 0.41, 1),
    "MAT_Brass": (0.55, 0.36, 0.10, 1),
}
MATS = {}
for name, color in PALETTE.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.roughness = 0.68
    if "Glass" in name:
        mat.metallic = 0.05
        mat.roughness = 0.28
    elif "Metal" in name or "Brass" in name:
        mat.metallic = 0.65
        mat.roughness = 0.38
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = mat.roughness
    shader.inputs["Metallic"].default_value = mat.metallic
    MATS[name] = mat

def surface_material(name, dark, light, roughness=.68, noise_scale=5.0,
                     bump_strength=.12, metallic=0.0):
    mat=bpy.data.materials.new(name)
    mat.use_nodes=True
    nodes=mat.node_tree.nodes
    links=mat.node_tree.links
    shader=nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value=roughness
    shader.inputs["Metallic"].default_value=metallic
    noise=nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value=noise_scale
    noise.inputs["Detail"].default_value=3.0
    noise.inputs["Roughness"].default_value=.62
    ramp=nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].color=(*light,1)
    bump=nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value=bump_strength
    bump.inputs["Distance"].default_value=.08
    links.new(noise.outputs["Fac"],ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"],shader.inputs["Base Color"])
    links.new(noise.outputs["Fac"],bump.inputs["Height"])
    links.new(bump.outputs["Normal"],shader.inputs["Normal"])
    mat.diffuse_color=(*light,1)
    mat.roughness=roughness
    mat.metallic=metallic
    MATS[name]=mat
    return mat

surface_material("MAT_AdminStucco",(.47,.38,.28),(.72,.63,.48),.76,7.0,.11)
surface_material("MAT_AdminStone",(.34,.31,.26),(.58,.54,.45),.7,11.0,.08)
surface_material("MAT_AdminRoof",(.20,.10,.055),(.43,.20,.10),.62,16.0,.08)
surface_material("MAT_AdminWood",(.12,.055,.025),(.28,.12,.05),.54,9.0,.07)
surface_material("MAT_AdminMetal",(.09,.10,.095),(.23,.25,.23),.34,22.0,.04,.72)
surface_material("MAT_AdminGlass",(.025,.055,.065),(.08,.17,.18),.22,3.0,.025,.08)

ASSETS = {}

def root(name):
    r = bpy.data.objects.new(name + "_ROOT", None)
    bpy.context.collection.objects.link(r)
    ASSETS[name] = r
    return r

def cube(parent, name, loc, scale, mat, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(MATS[mat])
    o.parent = parent
    if bevel:
        mod = o.modifiers.new("Bevel", 'BEVEL')
        mod.width = min(bevel, min(scale) * 0.18)
        mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def cyl(parent, name, loc, radius, depth, mat, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    o = bpy.context.object; o.name = name; o.data.materials.append(MATS[mat]); o.parent = parent
    return o

def window(parent, name, x, y, z, w=1.2, h=1.6):
    cube(parent, name + "_Recess", (x, y, z), (w + .18, .16, h + .18), "MAT_MetalDark", .03)
    cube(parent, name + "_Glass", (x, y - .1, z), (w, .12, h), "MAT_GlassSmoked", .02)

def door(parent, x, y, z=1.15, w=1.4, h=2.3, name="Door"):
    cube(parent, name + "_Frame", (x, y, z), (w + .25, .18, h + .25), "MAT_MetalDark", .03)
    cube(parent, name, (x, y - .11, z), (w, .14, h), "MAT_WoodDark", .03)

def building_shell(name, dims, mat="MAT_StuccoCream"):
    r = root(name)
    cube(r, name + "_Shell", (0, 0, dims[2] / 2), dims, mat, .14)
    return r

def mesh_object(parent, name, vertices, faces, mat, bevel=0.04):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    o.data.materials.append(MATS[mat])
    o.parent = parent
    if bevel:
        mod = o.modifiers.new("Bevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def gable_roof(parent, name, center, width, depth, rise, mat, overhang=.45):
    x, y, z = center
    hw, hd = width / 2 + overhang, depth / 2 + overhang
    verts = [(x-hw,y-hd,z),(x+hw,y-hd,z),(x-hw,y+hd,z),(x+hw,y+hd,z),
             (x-hw,y,z+rise),(x+hw,y,z+rise)]
    faces = [(0,1,5,4),(2,4,5,3),(0,4,2),(1,3,5),(0,2,3,1)]
    return mesh_object(parent, name, verts, faces, mat, .06)

def barrel_roof(parent, name, center, width, depth, rise, mat, segments=12, overhang=.35):
    x, y, z = center
    hw, hd = width / 2 + overhang, depth / 2 + overhang
    verts=[]
    for i in range(segments+1):
        t=-1.0+2.0*i/segments
        py=y+t*hd
        pz=z+rise*math.sqrt(max(0.0,1.0-t*t))
        verts.extend([(x-hw,py,pz),(x+hw,py,pz)])
    faces=[]
    for i in range(segments):
        a=i*2
        faces.append((a,a+1,a+3,a+2))
    faces.append(tuple(range(0,(segments+1)*2,2)))
    faces.append(tuple(range(1,(segments+1)*2,2)))
    return mesh_object(parent,name,verts,faces,mat,.04)

def steps(parent, name, center, width, count=3, tread=.38, rise=.16):
    x, y, z = center
    for i in range(count):
        cube(parent, f"{name}_{i+1}", (x, y-i*tread/2, z+(i*rise)/2),
             (width-i*.18, tread*(i+1), rise*(i+1)), "MAT_Concrete", .025)

def detailed_window(parent, name, x, y, z, w=1.2, h=1.7, mullions=True):
    window(parent, name, x, y, z, w, h)
    cube(parent, name+"_Sill", (x,y-.18,z-h/2-.08), (w+.35,.32,.14), "MAT_Concrete", .025)
    cube(parent, name+"_Lintel", (x,y-.15,z+h/2+.09), (w+.28,.26,.16), "MAT_Concrete", .025)
    if mullions:
        cube(parent, name+"_Mullion", (x,y-.19,z), (.07,.08,h), "MAT_MetalDark", .01)

def admin_cube(parent,name,loc,scale,mat,bevel=.07):
    o=cube(parent,name,loc,scale,mat,bevel)
    return o

def admin_window(parent,name,x,y,z,w=1.25,h=1.75,facing=-1):
    # Deep cavity, stone reveal, wooden sash and glass all occupy distinct planes.
    admin_cube(parent,name+"_Cavity",(x,y,z),(w+.42,.22,h+.42),"MAT_AdminMetal",.035)
    admin_cube(parent,name+"_Glass",(x,y+facing*.14,z),(w,.08,h),"MAT_AdminGlass",.025)
    frame=.10
    for sx in (-1,1): admin_cube(parent,name+f"_Jamb_{sx}",(x+sx*(w/2+.09),y+facing*.23,z),(frame,.18,h+.34),"MAT_AdminWood",.025)
    for sz in (-1,1): admin_cube(parent,name+f"_Rail_{sz}",(x,y+facing*.23,z+sz*(h/2+.09)),(w+.28,.18,frame),"MAT_AdminWood",.025)
    admin_cube(parent,name+"_Mullion",(x,y+facing*.25,z),(.075,.16,h),"MAT_AdminWood",.018)
    admin_cube(parent,name+"_Transom",(x,y+facing*.25,z+.28),(w,.16,.075),"MAT_AdminWood",.018)
    admin_cube(parent,name+"_StoneSill",(x,y+facing*.30,z-h/2-.22),(w+.58,.42,.18),"MAT_AdminStone",.035)
    admin_cube(parent,name+"_StoneHead",(x,y+facing*.17,z+h/2+.23),(w+.48,.28,.16),"MAT_AdminStone",.03)

def admin_side_window(parent,name,x,y,z,outward=1,w=1.2,h=1.65):
    admin_cube(parent,name+"_Cavity",(x,y,z),(.22,w+.42,h+.42),"MAT_AdminMetal",.035)
    admin_cube(parent,name+"_Glass",(x+outward*.14,y,z),(.08,w,h),"MAT_AdminGlass",.025)
    for sy in (-1,1): admin_cube(parent,name+f"_Jamb_{sy}",(x+outward*.23,y+sy*(w/2+.09),z),(.18,.10,h+.34),"MAT_AdminWood",.025)
    for sz in (-1,1): admin_cube(parent,name+f"_Rail_{sz}",(x+outward*.23,y,z+sz*(h/2+.09)),(.18,w+.28,.10),"MAT_AdminWood",.025)
    admin_cube(parent,name+"_Mullion",(x+outward*.25,y,z),(.16,.075,h),"MAT_AdminWood",.018)
    admin_cube(parent,name+"_StoneSill",(x+outward*.30,y,z-h/2-.22),(.42,w+.58,.18),"MAT_AdminStone",.035)

def admin_text(parent,body,loc,size=.42):
    bpy.ops.object.text_add(location=loc,rotation=(math.radians(90),0,0))
    o=bpy.context.object
    o.name="Admin_StudioName"
    o.data.body=body
    o.data.align_x='CENTER'
    o.data.align_y='CENTER'
    o.data.size=size
    o.data.extrude=.025
    o.data.bevel_depth=.008
    o.data.materials.append(MATS["MAT_Brass"])
    o.parent=parent
    bpy.context.view_layer.objects.active=o
    o.select_set(True)
    bpy.ops.object.convert(target='MESH')
    return o

def make_soundstage():
    r = root("StudioSoundStage_01")
    cube(r,"Stage_MainVolume",(0,0,3.8),(18,14,7.6),"MAT_StuccoWarm",.12)
    barrel_roof(r,"Stage_BarrelRoof",(0,0,7.45),18,14,2.5,"MAT_RoofCharcoal",14,.35)
    cube(r,"Stage_FrontHeadwall",(0,-7.18,5.0),(14.5,.55,5.2),"MAT_StuccoWarm",.08)
    cube(r,"Stage_SignBand",(0,-7.52,7.05),(8.2,.28,1.0),"MAT_Concrete",.05)
    door(r,0,-7.52,2.65,6.2,5.3,"Stage_LoadingDoor")
    cube(r,"Stage_LoadingHeader",(0,-7.68,5.65),(7.1,.38,.55),"MAT_Concrete",.04)
    cube(r,"Stage_ServiceWing",(-10.0,1.6,2.1),(4.0,8.2,4.2),"MAT_BrickWarm",.09)
    cube(r,"Stage_ServiceRoof",(-10.0,1.6,4.35),(4.5,8.7,.35),"MAT_RoofCharcoal",.05)
    door(r,-10,-2.58,1.15,1.2,2.3,"Stage_PersonnelDoor")
    cube(r,"Stage_ServiceCanopy",(-10,-3.15,2.75),(2.8,1.25,.22),"MAT_MetalPainted",.03)
    for x in (-7.7,-5.1,5.1,7.7):
        cube(r,f"Stage_FrontPilaster_{x}",(x,-7.38,3.7),(.48,.62,7.0),"MAT_Concrete",.035)
    for x in (-6,-2,2,6): detailed_window(r,f"Stage_Clerestory_{x}",x,-7.5,6.25,1.25,.8,False)
    for y in (-4.5,0,4.5): cube(r,f"Stage_SideButtress_{y}",(8.95,y,3.2),(.65,.75,6.4),"MAT_Concrete",.04)
    for x in (-4.2,0,4.2): cyl(r,f"Stage_RoofVent_{x}",(x,1.0,9.75),.38,1.2,"MAT_MetalPainted",16)

def make_admin():
    r=root("StudioAdministration_01")
    # Foundation and primary construction volumes.
    admin_cube(r,"Admin_Foundation",(0,.25,.34),(18.4,12.5,.68),"MAT_AdminStone",.10)
    admin_cube(r,"Admin_MainBody",(0,.4,3.95),(15.8,11.0,7.25),"MAT_AdminStucco",.14)
    admin_cube(r,"Admin_LeftWing",(-7.65,.6,3.7),(3.1,9.8,6.75),"MAT_AdminStucco",.12)
    admin_cube(r,"Admin_RightWing",(7.65,.6,3.7),(3.1,9.8,6.75),"MAT_AdminStucco",.12)
    admin_cube(r,"Admin_CentralEntranceMass",(0,-5.55,4.9),(5.4,2.25,9.1),"MAT_AdminStucco",.11)
    admin_cube(r,"Admin_UpperTower",(0,-.5,8.4),(5.7,5.0,2.45),"MAT_AdminStucco",.11)

    # Ground plinth, floor bands and restrained vertical Art Deco rhythm.
    admin_cube(r,"Admin_PlinthBand",(0,-5.15,.82),(17.9,.48,.82),"MAT_AdminStone",.055)
    admin_cube(r,"Admin_FirstFloorBand",(0,-5.16,3.58),(15.9,.40,.30),"MAT_AdminStone",.045)
    admin_cube(r,"Admin_Cornice",(0,-5.18,7.45),(16.4,.48,.48),"MAT_AdminStone",.055)
    for x in (-7.15,-3.05,3.05,7.15):
        admin_cube(r,f"Admin_FacadePilaster_{x}",(x,-5.27,4.0),(.42,.48,6.85),"MAT_AdminStone",.045)
        admin_cube(r,f"Admin_PilasterCap_{x}",(x,-5.32,7.22),(.68,.56,.32),"MAT_AdminStone",.04)

    # Constructed windows: cavity, frame, mullions, glass, sill and head.
    for z in (2.18,5.35):
        for x in (-6.0,-4.25,4.25,6.0):
            admin_window(r,f"Admin_FrontWindow_{z}_{x}",x,-5.18,z,1.12,1.62)
        for x in (-5.5,-2.7,0,2.7,5.5):
            admin_window(r,f"Admin_RearWindow_{z}_{x}",x,5.92,z,1.12,1.58,1)
    for z in (2.18,5.35):
        for y in (-2.2,.5,3.2):
            admin_side_window(r,f"Admin_LeftSideWindow_{z}_{y}",-9.22,y,z,-1,1.1,1.55)
            admin_side_window(r,f"Admin_RightSideWindow_{z}_{y}",9.22,y,z,1,1.1,1.55)

    # Deep lobby entrance and period double doors.
    admin_cube(r,"Admin_EntranceRecess",(0,-6.73,2.08),(3.18,.32,4.15),"MAT_AdminMetal",.06)
    admin_cube(r,"Admin_EntranceLeftPier",(-1.8,-6.80,2.45),(.68,.68,4.9),"MAT_AdminStone",.075)
    admin_cube(r,"Admin_EntranceRightPier",(1.8,-6.80,2.45),(.68,.68,4.9),"MAT_AdminStone",.075)
    admin_cube(r,"Admin_EntranceLintel",(0,-6.80,4.55),(4.3,.68,.70),"MAT_AdminStone",.07)
    for x in (-.69,.69):
        admin_cube(r,f"Admin_DoorLeaf_{x}",(x,-6.94,1.55),(1.30,.16,3.10),"MAT_AdminWood",.055)
        admin_cube(r,f"Admin_DoorGlass_{x}",(x,-7.04,2.05),(.87,.06,1.35),"MAT_AdminGlass",.025)
        admin_cube(r,f"Admin_DoorHandle_{x}",(x+(.20 if x<0 else -.20),-7.13,1.40),(.045,.06,.48),"MAT_Brass",.015)
    for i in range(5):
        step_height=(i+1)*.15
        admin_cube(r,f"Admin_EntryStep_{i+1}",(0,-8.20+i*.34,step_height/2),
                   (4.85-i*.10,.42,step_height),"MAT_AdminStone",.035)
    admin_cube(r,"Admin_Canopy",(0,-7.30,4.72),(5.0,1.72,.28),"MAT_AdminMetal",.055)
    admin_cube(r,"Admin_CanopyFascia",(0,-8.12,4.57),(5.1,.16,.42),"MAT_AdminMetal",.035)
    for x in (-2.2,2.2): admin_cube(r,f"Admin_CanopyTie_{x}",(x,-7.35,5.35),(.07,.07,1.55),"MAT_AdminMetal",.015)
    admin_text(r,"SILVERSCREEN STUDIOS",(0,-6.97,5.62),.38)

    # Tower windows, relief and period wall lights focus the entrance composition.
    admin_window(r,"Admin_TowerWindow",0,-6.73,7.15,1.55,2.25)
    for x in (-1.55,1.55):
        admin_cube(r,f"Admin_DecoFlute_{x}",(x,-6.80,7.0),(.20,.30,2.25),"MAT_AdminStone",.025)
        admin_cube(r,f"Admin_WallLightBase_{x}",(x*1.45,-6.98,3.45),(.16,.12,.42),"MAT_AdminMetal",.025)
        cyl(r,f"Admin_WallLight_{x}",(x*1.45,-7.10,3.63),.13,.42,"MAT_StuccoCream",12)

    # Real parapet perimeter with coping, inset roof surfaces and restrained utilities.
    for name,loc,scale in (
        ("Front",(0,-4.92,7.95),(16.3,.44,1.08)),("Rear",(0,5.78,7.80),(16.3,.44,.78)),
        ("Left",(-7.92,.42,7.80),(.44,11.1,.78)),("Right",(7.92,.42,7.80),(.44,11.1,.78))):
        admin_cube(r,"Admin_Parapet"+name,loc,scale,"MAT_AdminStucco",.055)
        coping=(scale[0]+.20,scale[1]+.20,.16)
        admin_cube(r,"Admin_Coping"+name,(loc[0],loc[1],loc[2]+scale[2]/2+.08),coping,"MAT_AdminStone",.035)
    admin_cube(r,"Admin_MainRoofSurface",(0,.42,7.50),(15.4,10.25,.18),"MAT_AdminRoof",.025)
    admin_cube(r,"Admin_TowerRoofSurface",(0,-.5,9.72),(5.35,4.65,.20),"MAT_AdminRoof",.025)
    admin_cube(r,"Admin_TowerCoping",(0,-2.92,9.98),(6.1,.42,.42),"MAT_AdminStone",.045)
    for x in (-1.8,1.8): cyl(r,f"Admin_RoofVent_{x}",(x,1.0,8.2),.24,.72,"MAT_AdminMetal",16)
    admin_cube(r,"Admin_RoofAccess",(4.7,2.2,8.25),(1.8,2.2,1.55),"MAT_AdminStucco",.08)
    admin_cube(r,"Admin_RoofAccessCap",(4.7,2.2,9.10),(2.05,2.45,.18),"MAT_AdminStone",.035)

def make_storefront():
    r=root("DowntownStorefront_01")
    cube(r,"Store_Main",(0,0,3.7),(11.0,11.5,7.4),"MAT_BrickWarm",.08)
    cube(r,"Store_RearService",(0,6.4,2.5),(8.4,2.0,5.0),"MAT_BrickWarm",.07)
    cube(r,"Store_Parapet",(0,0,7.75),(11.4,11.9,.75),"MAT_Concrete",.035)
    cube(r,"Store_CentralParapet",(0,-5.62,8.35),(4.2,.55,1.5),"MAT_BrickWarm",.04)
    cube(r,"Store_Cornice",(0,-5.88,7.25),(11.7,.48,.42),"MAT_Concrete",.03)
    cube(r,"Storefront_Recess",(0,-5.82,1.65),(2.1,.5,3.3),"MAT_MetalDark",.02)
    door(r,0,-6.12,1.25,1.25,2.5,"Store_Door")
    for x in (-3.6,3.6):
        cube(r,f"DisplayFrame_{x}",(x,-5.86,1.65),(3.0,.38,3.15),"MAT_WoodDark",.025)
        cube(r,f"DisplayGlass_{x}",(x,-6.08,1.7),(2.65,.12,2.75),"MAT_GlassSmoked",.015)
        cube(r,f"Awning_{x}",(x,-6.45,3.35),(3.2,1.45,.24),"MAT_FabricBurgundy",.03)
    for x in (-4.9,-2.45,0,2.45,4.9):
        cube(r,f"Store_Pilaster_{x}",(x,-5.86,3.65),(.35,.42,7.0),"MAT_Concrete",.025)
    for x in (-3.7,-1.25,1.25,3.7): detailed_window(r,f"Store_Upper_{x}",x,-5.88,5.45,1.25,1.6)
    cube(r,"Store_ServiceCanopy",(0,7.5,2.8),(4.5,1.5,.24),"MAT_MetalPainted",.03)

def make_bungalow():
    r=root("Bungalow_01")
    cube(r,"Bungalow_Main",(0,.7,1.65),(8.8,7.2,3.3),"MAT_WoodPainted",.08)
    cube(r,"Bungalow_RearWing",(2.5,4.6,1.5),(3.8,3.0,3.0),"MAT_WoodPainted",.07)
    gable_roof(r,"Bungalow_MainRoof",(0,.7,3.25),8.8,7.2,2.25,"MAT_RoofCharcoal",.55)
    gable_roof(r,"Bungalow_RearRoof",(2.5,4.6,2.95),3.8,3.0,1.25,"MAT_RoofCharcoal",.4)
    cube(r,"Bungalow_FrontGable",(-2.25,-3.2,2.15),(3.5,1.25,4.3),"MAT_WoodPainted",.06)
    gable_roof(r,"Bungalow_FrontRoof",(-2.25,-3.25,4.25),3.5,2.4,1.25,"MAT_RoofCharcoal",.35)
    cube(r,"Bungalow_PorchDeck",(1.45,-3.8,.28),(5.4,2.2,.56),"MAT_WoodDark",.04)
    steps(r,"Bungalow_PorchSteps",(1.45,-5.05,.07),2.0,3,.34,.14)
    gable_roof(r,"Bungalow_PorchRoof",(1.45,-4.0,3.05),5.5,2.25,1.05,"MAT_RoofCharcoal",.3)
    for x in (-.95,3.85): cube(r,f"Bungalow_PorchPost_{x}",(x,-4.65,1.6),(.22,.22,3.2),"MAT_WoodPainted",.025)
    door(r,1.3,-3.95,1.2,1.15,2.4,"Bungalow_Door")
    detailed_window(r,"Bungalow_BayWindow",-2.25,-3.87,1.75,1.55,1.55)
    detailed_window(r,"Bungalow_FrontWindow",3.15,-3.72,1.75,1.4,1.45)
    cube(r,"Bungalow_Chimney",(-3.0,2.0,4.4),(.9,1.0,3.6),"MAT_BrickWarm",.04)

def make_apartment():
    r=root("ApartmentSmall_01")
    cube(r,"Apartment_Core",(0,.7,5.0),(12.5,13.0,10.0),"MAT_StuccoWarm",.09)
    cube(r,"Apartment_LeftBay",(-5.7,-1.0,5.15),(3.0,10.2,10.3),"MAT_StuccoCream",.07)
    cube(r,"Apartment_RightBay",(5.7,-1.0,5.15),(3.0,10.2,10.3),"MAT_StuccoCream",.07)
    cube(r,"Apartment_StairTower",(0,-6.25,5.55),(3.7,2.5,11.1),"MAT_StuccoCream",.07)
    cube(r,"Apartment_Parapet",(0,.7,10.35),(13.0,13.5,.7),"MAT_Concrete",.035)
    cube(r,"Apartment_TowerCap",(0,-6.25,11.3),(4.25,3.0,.55),"MAT_RoofTerracotta",.04)
    steps(r,"Apartment_EntrySteps",(0,-7.85,.08),3.0,5,.38,.14)
    door(r,0,-7.55,1.4,1.55,2.8,"Apartment_MainDoor")
    cube(r,"Apartment_EntryCanopy",(0,-8.0,3.25),(3.4,1.3,.22),"MAT_MetalPainted",.025)
    for z in (2.0,5.1,8.2):
        for x in (-5.7,-3.3,3.3,5.7): detailed_window(r,f"Apartment_Window_{z}_{x}",x,-6.15,z,1.1,1.55)
    for z in (5.0,8.15):
        cube(r,f"Apartment_Balcony_{z}",(0,-7.55,z-.55),(3.0,1.25,.18),"MAT_Concrete",.025)
        for x in (-1.35,0,1.35): cube(r,f"Apartment_Rail_{z}_{x}",(x,-8.05,z),(.06,.06,1.1),"MAT_MetalDark",.01)
        cube(r,f"Apartment_RailTop_{z}",(0,-8.05,z+.5),(2.8,.07,.07),"MAT_MetalDark",.01)
    cube(r,"Apartment_RearStair",(5.9,5.7,5.1),(1.0,1.4,9.0),"MAT_MetalDark",.03)

def make_warehouse():
    r=root("Warehouse_01")
    cube(r,"Warehouse_Main",(0,0,3.8),(20.0,15.0,7.6),"MAT_BrickWarm",.09)
    gable_roof(r,"Warehouse_Roof",(0,0,7.5),20.0,15.0,1.5,"MAT_RoofCharcoal",.4)
    cube(r,"Warehouse_Monitor",(0,.6,8.9),(11.5,4.0,1.5),"MAT_MetalPainted",.05)
    gable_roof(r,"Warehouse_MonitorRoof",(0,.6,9.6),11.5,4.0,.8,"MAT_RoofCharcoal",.25)
    for x in (-4.2,-1.4,1.4,4.2): detailed_window(r,f"Warehouse_MonitorWindow_{x}",x,-1.43,9.0,1.5,.7,False)
    cube(r,"Warehouse_Office",(-8.4,-5.5,2.25),(5.2,5.2,4.5),"MAT_StuccoWarm",.07)
    cube(r,"Warehouse_OfficeRoof",(-8.4,-5.5,4.65),(5.7,5.7,.35),"MAT_RoofCharcoal",.04)
    door(r,-8.4,-8.18,1.2,1.15,2.4,"Warehouse_OfficeDoor")
    detailed_window(r,"Warehouse_OfficeWindow",-6.8,-8.18,2.05,1.15,1.35)
    for x in (-4.5,0,4.5): door(r,x,-7.58,2.25,3.6,4.5,f"Warehouse_LoadingDoor_{x}")
    cube(r,"Warehouse_LoadingDock",(0,-8.65,.6),(15.7,2.2,1.2),"MAT_Concrete",.06)
    cube(r,"Warehouse_LoadingCanopy",(0,-9.0,5.05),(16.5,2.4,.32),"MAT_MetalPainted",.035)
    for x in (-7.2,-2.4,2.4,7.2): cube(r,f"Warehouse_CanopyPost_{x}",(x,-9.65,2.55),(.18,.18,5.1),"MAT_MetalDark",.02)
    for x in (-7.5,-2.5,2.5,7.5): cube(r,f"Warehouse_Buttress_{x}",(x,-7.53,4.0),(.45,.5,7.4),"MAT_Concrete",.03)

def prop(name, maker):
    r = root(name); maker(r)

def make_props():
    prop("OfficeDesk_01", lambda r: (cube(r,"Top",(0,0,0.75),(1.5,.75,.12),"MAT_WoodDark",.05), cube(r,"PedestalL",(-.55,0,.37),(.35,.65,.7),"MAT_WoodDark",.04), cube(r,"PedestalR",(.55,0,.37),(.35,.65,.7),"MAT_WoodDark",.04)))
    for n in ("OfficeChair_01","DiningChair_01","DirectorChair_01"):
        def chair(r, director=(n=="DirectorChair_01")):
            mat="MAT_WoodDark"; cube(r,"Seat",(0,0,.48),(.48,.48,.1),mat,.04); cube(r,"Back",(0,.2,.88),(.48,.1,.75),"MAT_FabricBurgundy" if director else mat,.03)
            for x in (-.19,.19):
                for y in (-.19,.19): cube(r,"Leg",(x,y,.23),(.07,.07,.46),mat,.02)
        prop(n, chair)
    prop("SmallTable_01", lambda r: (cube(r,"Top",(0,0,.7),(.9,.9,.12),"MAT_WoodDark",.04), cyl(r,"Pedestal",(0,0,.35),.09,.7,"MAT_WoodDark")))
    prop("FilingCabinet_01", lambda r: (cube(r,"Body",(0,0,.65),(.55,.65,1.3),"MAT_MetalPainted",.05), cube(r,"Handles",(0,-.34,.7),(.28,.05,.6),"MAT_Brass",.02)))
    prop("Bookshelf_01", lambda r: (cube(r,"Body",(0,0,1.0),(1.0,.35,2.0),"MAT_WoodDark",.04), *[cube(r,f"Shelf{i}",(0,-.2,.25+i*.45),(.9,.28,.06),"MAT_WoodDark",.02) for i in range(4)]))
    prop("TableLamp_01", lambda r: (cyl(r,"Base",(0,0,.05),.22,.1,"MAT_Brass"), cyl(r,"Stem",(0,0,.35),.035,.6,"MAT_Brass"), cube(r,"Shade",(0,0,.7),(.5,.28,.22),"MAT_WoodPainted",.05)))
    prop("Typewriter_01", lambda r: (cube(r,"Body",(0,0,.14),(.48,.36,.24),"MAT_MetalDark",.05), cube(r,"Paper",(0,.1,.38),(.4,.04,.35),"MAT_StuccoCream",.01)))
    prop("Telephone_01", lambda r: (cube(r,"Base",(0,0,.12),(.35,.28,.22),"MAT_MetalDark",.05), cyl(r,"Receiver",(0,0,.28),.07,.42,"MAT_MetalDark",12)))
    prop("WoodenCrate_01", lambda r: cube(r,"Crate",(0,0,.45),(.9,.9,.9),"MAT_WoodDark",.05))
    prop("Barrel_01", lambda r: (cyl(r,"Barrel",(0,0,.55),.38,1.1,"MAT_WoodDark",16), *[cyl(r,f"Band{i}",(0,0,.2+i*.35),.395,.05,"MAT_MetalDark",16) for i in range(3)]))
    prop("EquipmentCase_01", lambda r: (cube(r,"Case",(0,0,.35),(.9,.55,.7),"MAT_MetalDark",.06), cube(r,"Trim",(0,-.29,.35),(.76,.04,.55),"MAT_Brass",.02)))
    prop("StudioLight_01", lambda r: (cyl(r,"Stand",(0,0,.75),.05,1.5,"MAT_MetalDark"), cube(r,"Lamp",(0,0,1.55),(.55,.45,.48),"MAT_MetalDark",.08), *[cube(r,f"Foot{i}",((i-1)*.25,0,.04),(.32,.08,.08),"MAT_MetalDark",.02) for i in range(3)]))
    prop("StreetLamp_01", lambda r: (cyl(r,"Pole",(0,0,1.8),.08,3.6,"MAT_MetalDark",16), cyl(r,"Base",(0,0,.12),.22,.24,"MAT_MetalDark",16), cube(r,"Lantern",(0,0,3.8),(.45,.45,.7),"MAT_StuccoCream",.06)))

make_soundstage(); make_admin(); make_storefront(); make_bungalow(); make_apartment(); make_warehouse(); make_props()

def hierarchy(root_obj):
    found=[]
    def walk(o):
        found.append(o)
        for c in o.children: walk(c)
    walk(root_obj); return found

stats = {}
for name, r in ASSETS.items():
    bpy.ops.object.select_all(action='DESELECT')
    objs=hierarchy(r)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=r
    bpy.ops.export_scene.fbx(filepath=str(EXPORT / f"{name}.fbx"), use_selection=True,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
        add_leaf_bones=False, bake_anim=False, path_mode='AUTO')
    verts=sum(len(o.data.vertices) for o in objs if o.type=='MESH')
    tris=sum(len(p.vertices)-2 for o in objs if o.type=='MESH' for p in o.data.polygons)
    points=[o.matrix_world @ mathutils.Vector(corner) for o in objs if o.type=='MESH' for corner in o.bound_box]
    mins=[min(p[i] for p in points) for i in range(3)]
    maxs=[max(p[i] for p in points) for i in range(3)]
    stats[name]={"vertices":verts,"triangles":tris,"objects":len(objs)-1,
                 "dimensions_m":[round(maxs[i]-mins[i],2) for i in range(3)]}

layout = {
    "StudioSoundStage_01":(-30,16,0), "StudioAdministration_01":(-3,16,0),
    "DowntownStorefront_01":(25,16,0), "Bungalow_01":(-27,-14,0),
    "ApartmentSmall_01":(-3,-14,0), "Warehouse_01":(25,-14,0)
}
prop_names=[n for n in ASSETS if n not in layout]
for name, pos in layout.items(): ASSETS[name].location=pos
for i,name in enumerate(prop_names):
    ASSETS[name].location=(-28+i*4,-28,0)
    for o in hierarchy(ASSETS[name]): o.hide_render=True

bpy.context.scene.render.engine='BLENDER_EEVEE'
bpy.ops.mesh.primitive_plane_add(size=150, location=(0, 0, -.04))
ground=bpy.context.object
ground.name="Reference_Ground"
ground.data.materials.append(MATS["MAT_Concrete"])
bpy.ops.object.light_add(type='SUN', location=(8, -12, 30))
bpy.context.object.rotation_euler=(math.radians(28), math.radians(-18), math.radians(-28))
bpy.context.object.data.energy=2.0
bpy.ops.object.light_add(type='AREA', location=(-20, -25, 30))
bpy.context.object.data.energy=1800
bpy.context.object.data.shape='DISK'
bpy.context.object.data.size=18
bpy.ops.object.camera_add(location=(69, -105, 79))
camera=bpy.context.object
direction=mathutils.Vector((0, 0, 3))-camera.location
camera.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
camera.data.lens=56
bpy.context.scene.camera=camera
bpy.context.scene.render.resolution_x=1600
bpy.context.scene.render.resolution_y=900
bpy.context.scene.render.resolution_percentage=100
bpy.context.scene.render.image_settings.file_format='PNG'
bpy.context.scene.render.filepath=str(SOURCE / "reference_kit_preview.png")
if bpy.context.scene.world is None:
 bpy.context.scene.world=bpy.data.worlds.new("ReferenceWorld")
bpy.context.scene.world.color=(0.055,0.045,0.04)
bpy.ops.render.render(write_still=True)

clay=bpy.data.materials.new("MAT_ClayReview")
clay.diffuse_color=(0.52,0.48,0.42,1)
clay.roughness=.82
clay.use_nodes=True
clay.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value=clay.diffuse_color
clay.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value=.82
bpy.context.scene.view_layers[0].material_override=clay
bpy.context.scene.render.filepath=str(SOURCE / "reference_kit_clay_preview.png")
bpy.ops.render.render(write_still=True)
bpy.context.scene.view_layers[0].material_override=None
for name in prop_names:
    for o in hierarchy(ASSETS[name]): o.hide_render=False

# Hero-building review renders. Other kit assets remain unchanged and hidden.
admin=ASSETS["StudioAdministration_01"]
admin_saved_location=admin.location.copy()
admin.location=(0,0,0)
for asset_name,asset_root in ASSETS.items():
    if asset_name != "StudioAdministration_01":
        for o in hierarchy(asset_root): o.hide_render=True
bpy.ops.object.light_add(type='AREA',location=(1,-18,11))
hero_fill=bpy.context.object
hero_fill.name="AdminPreview_Fill"
hero_fill.data.energy=1100
hero_fill.data.shape='DISK'
hero_fill.data.size=10
hero_fill.rotation_euler=(math.radians(35),0,0)

def point_camera(location,target,lens=55):
    camera.location=location
    camera.rotation_euler=(mathutils.Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.lens=lens

bpy.context.scene.render.resolution_x=1400
bpy.context.scene.render.resolution_y=1000
point_camera((20,-31,20),(0,0,4.2),58)
bpy.context.scene.render.filepath=str(SOURCE / "StudioAdministration_01_material_preview.png")
bpy.ops.render.render(write_still=True)

bpy.context.scene.view_layers[0].material_override=clay
bpy.context.scene.render.filepath=str(SOURCE / "StudioAdministration_01_clay_preview.png")
bpy.ops.render.render(write_still=True)
bpy.context.scene.view_layers[0].material_override=None

point_camera((9,-17,7.5),(0,-5.1,3.5),62)
bpy.context.scene.render.filepath=str(SOURCE / "StudioAdministration_01_close_preview.png")
bpy.ops.render.render(write_still=True)

# A neutral 1.8 m mannequin gives an unambiguous entrance/door scale check.
human=[]
def human_cylinder(name,loc,radius,depth):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=depth,location=loc)
    o=bpy.context.object; o.name=name; o.data.materials.append(clay); human.append(o)
    return o
human_cylinder("ScaleHuman_Torso",(-3.35,-8.65,1.18),.18,.66)
human_cylinder("ScaleHuman_LegL",(-3.44,-8.65,.48),.07,.96)
human_cylinder("ScaleHuman_LegR",(-3.26,-8.65,.48),.07,.96)
human_cylinder("ScaleHuman_ArmL",(-3.58,-8.65,1.12),.055,.72)
human_cylinder("ScaleHuman_ArmR",(-3.12,-8.65,1.12),.055,.72)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.14,location=(-3.35,-8.65,1.70))
head=bpy.context.object; head.name="ScaleHuman_Head"; head.data.materials.append(clay); human.append(head)
point_camera((10,-19,6.0),(0,-5.8,2.5),58)
bpy.context.scene.render.filepath=str(SOURCE / "StudioAdministration_01_human_scale_preview.png")
bpy.ops.render.render(write_still=True)
for o in human: bpy.data.objects.remove(o,do_unlink=True)
bpy.data.objects.remove(hero_fill,do_unlink=True)

admin.location=admin_saved_location
for asset_root in ASSETS.values():
    for o in hierarchy(asset_root): o.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "SilverScreen_ReferenceKit1930.blend"))
report={"blender_version":bpy.app.version_string,"units":"1 Blender unit = 1 metre","forward":"-Z","up":"Y","assets":stats,"materials":list(MATS.keys())}
(SOURCE / "generation_report.json").write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
