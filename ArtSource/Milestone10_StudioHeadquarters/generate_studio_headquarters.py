"""Procedurally build and render the Silver Screen Studios headquarters proof of concept.

Run with Blender 5.x:
  blender --background --python generate_studio_headquarters.py

The model uses metres, Z-up coordinates, a base-centre origin, and faces local -Y.
Generated game exports intentionally live outside Unity's Assets folder until approved.
"""

import bpy
import json
import math
from pathlib import Path
from mathutils import Vector


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
EXPORT_DIR = ROOT / "ArtExports" / "Milestone10_StudioHeadquarters"
REVIEW_DIR = ROOT / "ArtReview" / "Milestone10_StudioHeadquarters"
BLEND_PATH = HERE / "SilverScreen_StudioHeadquarters.blend"
FBX_PATH = EXPORT_DIR / "SilverScreen_StudioHeadquarters.fbx"
GLB_PATH = EXPORT_DIR / "SilverScreen_StudioHeadquarters.glb"
REPORT_PATH = HERE / "generation_report.json"

MODEL_COLLECTION = "HQ_EXPORT"
FRONT_Y = -3.25


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials,
                       bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.resolution_percentage = 100


def material(name, color, metallic=0.0, roughness=0.6):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


def move_to_collection(obj, collection):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)


def box(name, size, location, mat, collection, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Soft architectural edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    move_to_collection(obj, collection)
    return obj


def cylinder(name, radius, depth, location, mat, collection, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Soft edges", "BEVEL")
    bevel.width = 0.04
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    move_to_collection(obj, collection)
    return obj


def join_named(objects, name):
    if not objects:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    objects[0].name = name
    return objects[0]


def text_mesh(name, body, size, location, mat, collection, extrude=0.025):
    bpy.ops.object.text_add(location=location, rotation=(math.radians(90), 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.resolution_u = 1
    obj.data.extrude = extrude
    obj.data.bevel_depth = 0.008
    obj.data.bevel_resolution = 0
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    return obj


def build_model():
    scene_collection = bpy.context.scene.collection
    export_collection = bpy.data.collections.new(MODEL_COLLECTION)
    scene_collection.children.link(export_collection)

    mats = {
        "Cream Stucco": material("HQ_CreamStucco", (0.76, 0.62, 0.43), roughness=0.78),
        "Terracotta Accent": material("HQ_Terracotta", (0.49, 0.16, 0.09), roughness=0.68),
        "Smoked Glass": material("HQ_SmokedGlass", (0.035, 0.09, 0.105), metallic=0.12, roughness=0.28),
        "Dark Bronze": material("HQ_DarkBronze", (0.055, 0.04, 0.026), metallic=0.65, roughness=0.32),
        "Warm Gold": material("HQ_WarmGold", (0.92, 0.53, 0.11), metallic=0.38, roughness=0.3),
        "Roof Charcoal": material("HQ_RoofCharcoal", (0.09, 0.075, 0.065), roughness=0.82),
    }

    body = []
    roof = []
    windows = []
    doors = []
    trim = []
    sign = []

    # Broad two-storey wings and a projecting central administration block.
    # The tower repeats the lower facade's stepped widths instead of reading as a roof box.
    body += [
        box("Body_LeftWing", (3.15, 6.45, 5.55), (-3.0, 0, 2.775), mats["Cream Stucco"], export_collection, 0.12),
        box("Body_RightWing", (3.15, 6.45, 5.55), (3.0, 0, 2.775), mats["Cream Stucco"], export_collection, 0.12),
        box("Body_Centre", (2.85, 6.65, 6.65), (0, -0.08, 3.325), mats["Terracotta Accent"], export_collection, 0.11),
        box("Body_TowerLower", (2.55, 3.55, 0.72), (0, 0.38, 7.04), mats["Terracotta Accent"], export_collection, 0.09),
        box("Body_TowerUpper", (1.75, 2.65, 0.78), (0, 0.38, 7.70), mats["Cream Stucco"], export_collection, 0.09),
    ]

    # Layered roof caps and shallow parapets exaggerate the silhouette at a 52-degree camera pitch.
    roof += [
        box("Roof_WingCap", (9.35, 6.85, 0.24), (0, 0, 5.72), mats["Roof Charcoal"], export_collection, 0.06),
        box("Roof_LeftFrontParapet", (3.05, 0.38, 0.46), (-3.0, -3.16, 5.93), mats["Cream Stucco"], export_collection, 0.06),
        box("Roof_RightFrontParapet", (3.05, 0.38, 0.46), (3.0, -3.16, 5.93), mats["Cream Stucco"], export_collection, 0.06),
        box("Roof_RearParapet", (9.05, 0.32, 0.34), (0, 3.15, 5.86), mats["Cream Stucco"], export_collection, 0.05),
        box("Roof_CentreStep1", (3.35, 4.0, 0.30), (0, 0.15, 6.76), mats["Roof Charcoal"], export_collection, 0.05),
        box("Roof_TowerLowerCap", (2.85, 3.85, 0.22), (0, 0.38, 7.43), mats["Roof Charcoal"], export_collection, 0.05),
        box("Roof_TowerUpperCap", (2.05, 2.95, 0.22), (0, 0.38, 8.14), mats["Roof Charcoal"], export_collection, 0.05),
        box("Roof_CrownWide", (1.35, 0.52, 0.32), (0, -0.72, 8.34), mats["Terracotta Accent"], export_collection, 0.04),
        box("Roof_CrownNarrow", (0.78, 0.52, 0.34), (0, -0.72, 8.64), mats["Warm Gold"], export_collection, 0.04),
    ]

    # Horizontal facade bands continue around the sides as a restrained all-around treatment.
    for z in (0.62, 3.12, 5.20):
        trim.append(box(f"Trim_FacadeBand_{z}", (9.25, 0.14, 0.16), (0, -3.28, z), mats["Terracotta Accent"], export_collection, 0.025))
    for side_x in (-4.58, 4.58):
        for z in (0.62, 3.12, 5.20):
            trim.append(box(f"Trim_SideBand_{side_x}_{z}", (0.14, 6.42, 0.16), (side_x, 0, z), mats["Terracotta Accent"], export_collection, 0.025))
    trim += [
        # Lower columns stop below the marquee; upper fins resume above it with a deliberate gap.
        box("Trim_LeftEntranceColumn", (0.26, 0.18, 4.20), (-1.36, -3.42, 2.30), mats["Warm Gold"], export_collection, 0.035),
        box("Trim_RightEntranceColumn", (0.26, 0.18, 4.20), (1.36, -3.42, 2.30), mats["Warm Gold"], export_collection, 0.035),
        box("Trim_LeftTowerFin", (0.22, 0.18, 1.30), (-1.12, -3.42, 6.12), mats["Warm Gold"], export_collection, 0.035),
        box("Trim_RightTowerFin", (0.22, 0.18, 1.30), (1.12, -3.42, 6.12), mats["Warm Gold"], export_collection, 0.035),
        box("Trim_EntranceFrameTop", (2.68, 0.22, 0.24), (0, -3.43, 3.15), mats["Warm Gold"], export_collection, 0.035),
        box("Trim_Canopy", (3.70, 1.24, 0.24), (0, -3.75, 3.43), mats["Dark Bronze"], export_collection, 0.07),
        box("Trim_CanopyGold", (3.80, 0.12, 0.16), (0, -4.37, 3.40), mats["Warm Gold"], export_collection, 0.025),
        box("Trim_RearBand", (9.15, 0.14, 0.15), (0, 3.28, 3.15), mats["Terracotta Accent"], export_collection, 0.025),
    ]

    # Oversized glass stays simple; thin bronze backing plates create a shallow framed recess and shadow.
    for floor_z in (1.75, 4.15):
        for x in (-3.55, -2.35, 2.35, 3.55):
            trim.append(box(f"WindowFrame_Front_{x}_{floor_z}", (1.01, 0.06, 1.36), (x, -3.24, floor_z), mats["Dark Bronze"], export_collection, 0.0))
            windows.append(box(f"Window_Front_{x}_{floor_z}", (0.83, 0.13, 1.18), (x, -3.30, floor_z), mats["Smoked Glass"], export_collection, 0.035))
            trim.append(box(f"Mullion_Front_{x}_{floor_z}", (0.07, 0.05, 1.08), (x, -3.39, floor_z), mats["Dark Bronze"], export_collection, 0.015))

    # Side windows use the same shallow frame language and remain readable at medium distance.
    for side_x in (-4.53, 4.53):
        frame_x = math.copysign(4.47, side_x)
        for y in (-1.65, 0.0, 1.65):
            for z in (1.75, 4.15):
                trim.append(box(f"WindowFrame_Side_{side_x}_{y}_{z}", (0.06, 1.10, 1.34), (frame_x, y, z), mats["Dark Bronze"], export_collection, 0.0))
                windows.append(box(f"Window_Side_{side_x}_{y}_{z}", (0.13, 0.92, 1.16), (side_x, y, z), mats["Smoked Glass"], export_collection, 0.03))

    # Rear facade and a small staff entrance for a finished all-around silhouette.
    for floor_z in (1.75, 4.15):
        for x in (-3.35, -2.05, 2.05, 3.35):
            trim.append(box(f"WindowFrame_Rear_{x}_{floor_z}", (1.06, 0.06, 1.30), (x, 3.24, floor_z), mats["Dark Bronze"], export_collection, 0.0))
            windows.append(box(f"Window_Rear_{x}_{floor_z}", (0.88, 0.13, 1.12), (x, 3.30, floor_z), mats["Smoked Glass"], export_collection, 0.03))
    doors += [
        box("Doors_MainGlass", (1.72, 0.16, 2.42), (0, -3.43, 1.51), mats["Smoked Glass"], export_collection, 0.035),
        box("Doors_RearStaff", (1.35, 0.15, 2.18), (0, 3.36, 1.20), mats["Dark Bronze"], export_collection, 0.035),
    ]
    trim += [
        box("Doors_CentreMullion", (0.08, 0.08, 2.30), (0, -3.54, 1.51), mats["Warm Gold"], export_collection, 0.015),
        box("Doors_MainLeftHandle", (0.05, 0.05, 0.45), (-0.18, -3.58, 1.48), mats["Warm Gold"], export_collection, 0.012),
        box("Doors_MainRightHandle", (0.05, 0.05, 0.45), (0.18, -3.58, 1.48), mats["Warm Gold"], export_collection, 0.012),
    ]

    # Tower glazing repeats the framed-window language on the upper stepped section.
    trim.append(box("WindowFrame_Tower", (1.40, 0.06, 0.78), (0, -0.91, 7.69), mats["Dark Bronze"], export_collection, 0.0))
    windows.append(box("Window_Tower", (1.22, 0.13, 0.60), (0, -0.97, 7.69), mats["Smoked Glass"], export_collection, 0.035))

    # Blank dynamic-name marquee. Its mounting blocks end exactly at the backplate's rear surface,
    # while the gold entrance columns stop below and resume above it, preventing intersections.
    sign += [
        box("Sign_MountLeft", (0.22, 0.10, 0.92), (-2.45, -3.44, 4.98), mats["Dark Bronze"], export_collection, 0.03),
        box("Sign_MountRight", (0.22, 0.10, 0.92), (2.45, -3.44, 4.98), mats["Dark Bronze"], export_collection, 0.03),
        box("Sign_BlankDisplaySurface", (5.70, 0.18, 0.86), (0, -3.58, 4.98), mats["Dark Bronze"], export_collection, 0.06),
        box("Sign_GoldTop", (5.82, 0.06, 0.07), (0, -3.70, 5.42), mats["Warm Gold"], export_collection, 0.018),
        box("Sign_GoldBottom", (5.82, 0.06, 0.07), (0, -3.70, 4.54), mats["Warm Gold"], export_collection, 0.018),
        box("Sign_GoldLeft", (0.07, 0.06, 0.82), (-2.89, -3.70, 4.98), mats["Warm Gold"], export_collection, 0.018),
        box("Sign_GoldRight", (0.07, 0.06, 0.82), (2.89, -3.70, 4.98), mats["Warm Gold"], export_collection, 0.018),
        cylinder("Sign_MedallionLeft", 0.29, 0.09, (-3.60, -3.39, 5.20), mats["Warm Gold"], export_collection, vertices=12, rotation=(math.radians(90), 0, 0)),
        cylinder("Sign_MedallionRight", 0.29, 0.09, (3.60, -3.39, 5.20), mats["Warm Gold"], export_collection, vertices=12, rotation=(math.radians(90), 0, 0)),
    ]

    # A shallow plinth keeps the bottom clean on flat Unity terrain.
    body.append(box("Body_PlacementPlinth", (9.38, 6.88, 0.18), (0, 0, 0.09), mats["Terracotta Accent"], export_collection, 0.055))

    components = {
        "HQ_Body": join_named(body, "HQ_Body"),
        "HQ_Roof": join_named(roof, "HQ_Roof"),
        "HQ_Windows": join_named(windows, "HQ_Windows"),
        "HQ_Doors": join_named(doors, "HQ_Doors"),
        "HQ_Trim": join_named(trim, "HQ_Trim"),
        "HQ_Sign": join_named(sign, "HQ_Sign"),
    }

    root = bpy.data.objects.new("SilverScreen_StudioHeadquarters", None)
    export_collection.objects.link(root)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.6
    for obj in components.values():
        obj.parent = root
    return export_collection, mats, components


def setup_preview_world():
    scene = bpy.context.scene
    world = scene.world or bpy.data.worlds.new("PreviewWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.065, 0.075, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.45

    preview = bpy.data.collections.new("PREVIEW_ONLY")
    scene.collection.children.link(preview)
    ground_mat = material("Preview_Ground", (0.17, 0.19, 0.20), roughness=0.9)
    box("PreviewGround", (34, 34, 0.16), (0, 0, -0.10), ground_mat, preview, 0.0)

    def light(name, kind, location, energy, color, size=5.0):
        data = bpy.data.lights.new(name, kind)
        data.energy = energy
        data.color = color
        if kind == "AREA":
            data.shape = "DISK"
            data.size = size
        obj = bpy.data.objects.new(name, data)
        preview.objects.link(obj)
        obj.location = location
        return obj

    sun = light("Preview_Sun", "SUN", (6, -9, 14), 2.0, (1.0, 0.78, 0.58))
    sun.rotation_euler = (math.radians(28), math.radians(-18), math.radians(-28))
    light("Preview_Key", "AREA", (-7, -9, 12), 1150, (1.0, 0.70, 0.48), 7.0)
    light("Preview_Fill", "AREA", (8, 2, 9), 850, (0.46, 0.66, 1.0), 6.0)
    light("Preview_Rim", "AREA", (0, 8, 11), 1050, (0.95, 0.55, 0.34), 5.0)
    return preview


def point_camera(camera, location, target, lens=52):
    camera.location = location
    camera.data.lens = lens
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_previews():
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new("PreviewCamera")
    cam = bpy.data.objects.new("PreviewCamera", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    scene.camera = cam
    views = [
        ("01_front", (0, -24.0, 5.5), (0, 0, 3.7), 52),
        ("02_rear_side", (17.0, 16.5, 10.5), (0, 0, 3.3), 52),
        ("03_elevated_three_quarter", (-13.5, -15.5, 12.0), (0, 0, 3.0), 56),
        # Approximate in-game 52-degree pitch and 23.4 m camera offset.
        ("04_gameplay_camera", (12.5, -14.2, 18.0), (0, 0, 1.7), 50),
    ]
    for filename, location, target, lens in views:
        point_camera(cam, location, target, lens)
        scene.render.filepath = str(REVIEW_DIR / f"{filename}.png")
        bpy.ops.render.render(write_still=True)


def export_model(export_collection):
    bpy.ops.object.select_all(action="DESELECT")
    exported = []
    for obj in export_collection.objects:
        obj.select_set(True)
        exported.append(obj)
    bpy.context.view_layer.objects.active = next(o for o in exported if o.type == "MESH")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        embed_textures=False,
        path_mode="AUTO",
    )
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
    )


def calculate_report(export_collection, mats):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    meshes = [o for o in export_collection.objects if o.type == "MESH"]
    vertices = polygons = triangles = 0
    minimum = Vector((1e9, 1e9, 1e9))
    maximum = Vector((-1e9, -1e9, -1e9))
    object_stats = {}
    for obj in meshes:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        v, p, t = len(mesh.vertices), len(mesh.polygons), len(mesh.loop_triangles)
        vertices += v
        polygons += p
        triangles += t
        object_stats[obj.name] = {"vertices": v, "polygons": p, "triangles": t}
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            minimum.x = min(minimum.x, world.x)
            minimum.y = min(minimum.y, world.y)
            minimum.z = min(minimum.z, world.z)
            maximum.x = max(maximum.x, world.x)
            maximum.y = max(maximum.y, world.y)
            maximum.z = max(maximum.z, world.z)
        evaluated.to_mesh_clear()
    dimensions = maximum - minimum
    report = {
        "asset": "Silver Screen Studios Headquarters",
        "generator": "Blender Python 5.x",
        "coordinate_convention": "Metres, Z-up in Blender, front faces -Y, origin at base centre",
        "scene_fit_reference": {
            "placeholder_body_metres": [9.0, 6.5, 4.5],
            "placeholder_with_trim_metres": [9.4, 6.9, 4.8],
            "entrance_local_unity": [0.0, 0.05, -4.45],
            "gameplay_camera_pitch_degrees": 52.0,
            "road_width_metres": 6.0,
        },
        "bounds_min_xyz_metres": [round(v, 3) for v in minimum],
        "bounds_max_xyz_metres": [round(v, 3) for v in maximum],
        "dimensions_xyz_metres": [round(v, 3) for v in dimensions],
        "mesh_totals": {"vertices": vertices, "polygons": polygons, "triangles": triangles},
        "object_stats": object_stats,
        "materials": list(mats.keys()),
        "dynamic_signage": {
            "modeled_lettering": False,
            "export_component": "HQ_Sign",
            "generated_surface_part": "Sign_BlankDisplaySurface",
            "display_surface_dimensions_metres": [5.7, 0.18, 0.86],
            "front_surface_center_blender_xyz_metres": [0.0, -3.67, 4.98],
            "front_facing": "-Y",
            "intended_use": "Dynamic Unity studio-name text after asset approval",
        },
        "exports": {
            "fbx": {"forward": "-Z", "up": "Y", "units": "metres", "animations": False, "embedded_textures": False},
            "glb": {"format": "GLB 2.0", "y_up": True, "materials": True, "animations": False},
        },
        "preview_files": ["01_front.png", "02_rear_side.png", "03_elevated_three_quarter.png", "04_gameplay_camera.png"],
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


def main():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    reset_scene()
    export_collection, mats, _ = build_model()
    calculate_report(export_collection, mats)
    export_model(export_collection)
    setup_preview_world()
    render_previews()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)
    print(f"Generated {BLEND_PATH}")
    print(f"Exported {FBX_PATH} and {GLB_PATH}")
    print(f"Rendered previews to {REVIEW_DIR}")


if __name__ == "__main__":
    main()
