"""Small reusable Blender helpers for Silver Screen's procedural building pipeline."""

import math

import bpy
from mathutils import Vector


PALETTE = {
    "Cream Stucco": ((0.76, 0.62, 0.43), 0.0, 0.78),
    "Terracotta Accent": ((0.49, 0.16, 0.09), 0.0, 0.68),
    "Smoked Glass": ((0.035, 0.09, 0.105), 0.12, 0.28),
    "Dark Bronze": ((0.055, 0.04, 0.026), 0.65, 0.32),
    "Warm Gold": ((0.92, 0.53, 0.11), 0.38, 0.30),
    "Roof Charcoal": ((0.09, 0.075, 0.065), 0.0, 0.82),
}


def reset_scene(resolution=(1280, 720)):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"


def create_palette(prefix):
    materials = {}
    for label, (color, metallic, roughness) in PALETTE.items():
        mat = bpy.data.materials.new(f"{prefix}_{label.replace(' ', '')}")
        mat.diffuse_color = (*color, 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        materials[label] = mat
    return materials


def move_to_collection(obj, collection):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)


def box(name, size, location, material, collection, bevel=0.06):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft architectural edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    move_to_collection(obj, collection)
    return obj


def cylinder(name, radius, depth, location, material, collection, vertices=12, rotation=(0, 0, 0), bevel=0.035):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
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


def point_camera(camera, location, target, lens=52):
    camera.location = location
    camera.data.lens = lens
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def calculate_mesh_stats(collection):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    minimum = Vector((1e9, 1e9, 1e9))
    maximum = Vector((-1e9, -1e9, -1e9))
    totals = {"vertices": 0, "polygons": 0, "triangles": 0}
    objects = {}

    for obj in (item for item in collection.objects if item.type == "MESH"):
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        stats = {
            "vertices": len(mesh.vertices),
            "polygons": len(mesh.polygons),
            "triangles": len(mesh.loop_triangles),
        }
        objects[obj.name] = stats
        for key in totals:
            totals[key] += stats[key]
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
    return {
        "bounds_min_xyz_metres": [round(value, 3) for value in minimum],
        "bounds_max_xyz_metres": [round(value, 3) for value in maximum],
        "dimensions_xyz_metres": [round(value, 3) for value in dimensions],
        "mesh_totals": totals,
        "object_stats": objects,
    }


def export_fbx(collection, path):
    bpy.ops.object.select_all(action="DESELECT")
    exported = list(collection.objects)
    for obj in exported:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = next(obj for obj in exported if obj.type == "MESH")
    bpy.ops.export_scene.fbx(
        filepath=str(path),
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


def setup_preview_world(ground_size=30):
    scene = bpy.context.scene
    world = scene.world or bpy.data.worlds.new("PreviewWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.065, 0.075, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.45

    preview = bpy.data.collections.new("PREVIEW_ONLY")
    scene.collection.children.link(preview)
    ground = bpy.data.materials.new("Preview_Ground")
    ground.diffuse_color = (0.17, 0.19, 0.20, 1)
    ground.use_nodes = True
    ground.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.17, 0.19, 0.20, 1)
    ground.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
    box("PreviewGround", (ground_size, ground_size, 0.16), (0, 0, -0.10), ground, preview, 0)

    def add_light(name, kind, location, energy, color, size=5.0):
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

    sun = add_light("Preview_Sun", "SUN", (6, -9, 14), 2.0, (1.0, 0.78, 0.58))
    sun.rotation_euler = (math.radians(28), math.radians(-18), math.radians(-28))
    add_light("Preview_Key", "AREA", (-7, -9, 12), 1150, (1.0, 0.70, 0.48), 7.0)
    add_light("Preview_Fill", "AREA", (8, 2, 9), 850, (0.46, 0.66, 1.0), 6.0)
    add_light("Preview_Rim", "AREA", (0, 8, 11), 1050, (0.95, 0.55, 0.34), 5.0)
    return preview
