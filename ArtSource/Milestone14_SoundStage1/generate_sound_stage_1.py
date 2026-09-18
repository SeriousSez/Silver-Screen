"""Generate Silver Screen's original 1930s Sound Stage 1 and review renders.

Run with Blender 5.x:
  blender --background --python generate_sound_stage_1.py

Validate the saved blend and exported FBX with:
  blender --background SilverScreen_SoundStage1.blend --python generate_sound_stage_1.py -- --validate-only

The building uses metres, Blender Z-up, a base-centre origin, and faces local -Y.
It is intentionally exported outside Unity's Assets folder pending art approval.
"""

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SHARED = ROOT / "ArtSource" / "Shared"
sys.path.insert(0, str(SHARED))

from silver_screen_art_utils import (  # noqa: E402
    box,
    calculate_mesh_stats,
    create_palette,
    cylinder,
    export_fbx,
    join_named,
    point_camera,
    reset_scene,
    setup_preview_world,
)


EXPORT_DIR = ROOT / "ArtExports" / "Milestone14_SoundStage1"
REVIEW_DIR = ROOT / "ArtReview" / "Milestone14_SoundStage1"
BLEND_PATH = HERE / "SilverScreen_SoundStage1.blend"
FBX_PATH = EXPORT_DIR / "SilverScreen_SoundStage1.fbx"
REPORT_PATH = HERE / "generation_report.json"
HQ_FBX_PATH = ROOT / "ArtExports" / "Milestone10_StudioHeadquarters" / "SilverScreen_StudioHeadquarters.fbx"
CASTING_FBX_PATH = ROOT / "ArtExports" / "Milestone12_CastingOffice" / "SilverScreen_CastingOffice.fbx"
MODEL_COLLECTION = "STAGE1_EXPORT"

PERSONNEL_TARGET = (0.0, -6.70, 0.05)
PERSONNEL_DOOR = (0.0, -6.05, 1.45)
LOADING_DOOR = (8.14, -0.65, 2.85)
SIGN_SURFACE = (0.0, -6.08, 4.55)


def build_model():
    collection = bpy.data.collections.new(MODEL_COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    mats = create_palette("STAGE1")

    body = []
    roof = []
    windows = []
    doors = []
    trim = []
    sign = []

    # The stage volume dominates: a tall charcoal industrial shell on a low cream base.
    body += [
        box("Body_PlacementPlinth", (16.10, 11.55, 0.22), (0, 0, 0.11), mats["Dark Bronze"], collection, 0.055),
        box("Body_LowerBase", (15.70, 11.15, 1.18), (0, 0, 0.70), mats["Cream Stucco"], collection, 0.10),
        box("Body_MainStage", (15.45, 10.90, 6.35), (0, 0.05, 3.70), mats["Roof Charcoal"], collection, 0.14),
        # Shallow central front service bay makes the personnel entrance legible without office-like symmetry.
        box("Body_FrontServiceBay", (5.05, 0.72, 4.45), (0, -5.57, 2.28), mats["Cream Stucco"], collection, 0.10),
        box("Body_ServiceSidePier", (0.58, 5.80, 6.00), (7.83, -0.55, 3.35), mats["Terracotta Accent"], collection, 0.08),
    ]

    # Layered parapets and a broad roof monitor remain readable at the 52-degree management camera.
    roof += [
        box("Roof_MainCap", (15.92, 11.34, 0.28), (0, 0.05, 6.98), mats["Dark Bronze"], collection, 0.07),
        box("Roof_FrontParapet", (15.78, 0.42, 0.70), (0, -5.35, 7.24), mats["Terracotta Accent"], collection, 0.065),
        box("Roof_RearParapet", (15.78, 0.36, 0.52), (0, 5.43, 7.15), mats["Roof Charcoal"], collection, 0.06),
        box("Roof_LeftParapet", (0.38, 10.45, 0.48), (-7.66, 0.05, 7.13), mats["Roof Charcoal"], collection, 0.055),
        box("Roof_RightParapet", (0.38, 10.45, 0.48), (7.66, 0.05, 7.13), mats["Terracotta Accent"], collection, 0.055),
        box("Roof_MonitorBase", (7.35, 5.45, 0.72), (-1.10, 0.42, 7.38), mats["Roof Charcoal"], collection, 0.09),
        box("Roof_MonitorStep", (5.45, 4.10, 0.62), (-1.10, 0.42, 8.02), mats["Terracotta Accent"], collection, 0.085),
        box("Roof_MonitorCap", (5.78, 4.40, 0.24), (-1.10, 0.42, 8.45), mats["Dark Bronze"], collection, 0.055),
        # Two chunky ventilation housings add production character without prop clutter.
        box("Roof_VentHousingFront", (1.45, 1.25, 0.58), (3.15, -0.85, 7.42), mats["Cream Stucco"], collection, 0.07),
        box("Roof_VentHousingRear", (1.45, 1.25, 0.58), (3.15, 1.75, 7.42), mats["Cream Stucco"], collection, 0.07),
    ]

    # Broad structural bays and continuous bands carry the shared lot language at industrial scale.
    for x in (-7.45, -3.75, 3.75, 7.45):
        trim.append(box(f"Trim_FrontPilaster_{x}", (0.38, 0.32, 6.25), (x, -5.50, 3.45), mats["Terracotta Accent"], collection, 0.05))
    for x in (-7.45, -3.75, 0.0, 3.75, 7.45):
        trim.append(box(f"Trim_RearPilaster_{x}", (0.34, 0.28, 5.95), (x, 5.54, 3.38), mats["Dark Bronze"], collection, 0.045))
    trim += [
        box("Trim_FrontLowerBand", (15.70, 0.22, 0.22), (0, -5.54, 1.28), mats["Terracotta Accent"], collection, 0.035),
        box("Trim_FrontUpperBand", (15.72, 0.22, 0.26), (0, -5.54, 5.83), mats["Cream Stucco"], collection, 0.04),
        box("Trim_RearLowerBand", (15.70, 0.20, 0.22), (0, 5.55, 1.28), mats["Terracotta Accent"], collection, 0.035),
        box("Trim_LeftLowerBand", (0.20, 10.75, 0.22), (-7.80, 0.05, 1.28), mats["Terracotta Accent"], collection, 0.035),
        box("Trim_RightLowerBand", (0.20, 10.75, 0.22), (7.80, 0.05, 1.28), mats["Terracotta Accent"], collection, 0.035),
        # Personnel entrance is deliberately smaller and separate from the loading side.
        box("Trim_PersonnelLeft", (0.30, 0.22, 2.95), (-1.18, -6.00, 1.72), mats["Dark Bronze"], collection, 0.04),
        box("Trim_PersonnelRight", (0.30, 0.22, 2.95), (1.18, -6.00, 1.72), mats["Dark Bronze"], collection, 0.04),
        box("Trim_PersonnelTop", (2.66, 0.22, 0.28), (0, -6.00, 3.18), mats["Dark Bronze"], collection, 0.04),
        box("Trim_PersonnelCanopy", (3.45, 1.18, 0.25), (0, -6.18, 3.42), mats["Dark Bronze"], collection, 0.065),
        box("Trim_CanopyAccent", (3.55, 0.10, 0.10), (0, -6.78, 3.39), mats["Warm Gold"], collection, 0.018),
    ]

    # Sparse high clerestory glazing; the main stage walls remain predominantly solid.
    for x in (-5.55, -1.85, 1.85, 5.55):
        trim.append(box(f"WindowFrame_Front_{x}", (2.20, 0.08, 1.02), (x, -5.43, 4.78), mats["Dark Bronze"], collection, 0.0))
        windows.append(box(f"Window_Front_{x}", (1.96, 0.14, 0.78), (x, -5.52, 4.78), mats["Smoked Glass"], collection, 0.035))
        trim.append(box(f"WindowMullion_Front_{x}", (0.08, 0.05, 0.68), (x, -5.62, 4.78), mats["Dark Bronze"], collection, 0.012))
    for x in (-5.55, -1.85, 1.85, 5.55):
        trim.append(box(f"WindowFrame_Rear_{x}", (2.20, 0.08, 0.98), (x, 5.43, 4.72), mats["Dark Bronze"], collection, 0.0))
        windows.append(box(f"Window_Rear_{x}", (1.96, 0.14, 0.74), (x, 5.52, 4.72), mats["Smoked Glass"], collection, 0.035))

    doors += [
        box("Doors_Personnel", (1.82, 0.18, 2.48), PERSONNEL_DOOR, mats["Smoked Glass"], collection, 0.04),
        # Loading doors occupy the +X service wall, clearly distinct from the gameplay entrance.
        box("Doors_LoadingMain", (0.20, 5.10, 5.15), LOADING_DOOR, mats["Dark Bronze"], collection, 0.045),
        box("Doors_RearService", (1.40, 0.17, 2.28), (-5.70, 5.58, 1.32), mats["Dark Bronze"], collection, 0.035),
    ]
    trim += [
        box("Doors_PersonnelMullion", (0.08, 0.08, 2.34), (0, -5.86, 1.45), mats["Warm Gold"], collection, 0.012),
        box("LoadingFrameFront", (0.28, 0.34, 5.62), (8.18, -3.36, 3.02), mats["Terracotta Accent"], collection, 0.045),
        box("LoadingFrameRear", (0.28, 0.34, 5.62), (8.18, 2.06, 3.02), mats["Terracotta Accent"], collection, 0.045),
        box("LoadingFrameTop", (0.28, 5.72, 0.34), (8.18, -0.65, 5.78), mats["Terracotta Accent"], collection, 0.045),
        box("LoadingDoorSeam", (0.12, 0.09, 4.82), (8.31, -0.65, 2.85), mats["Cream Stucco"], collection, 0.015),
    ]

    # Restrained louvres and vents communicate a stage's technical function from medium distance.
    for z in (2.30, 2.78, 3.26, 3.74):
        trim.append(box(f"Louvre_Left_{z}", (0.16, 2.05, 0.18), (-7.86, 2.45, z), mats["Cream Stucco"], collection, 0.022))
    for y in (-2.65, 2.65):
        trim.append(cylinder(f"VentCap_{y}", 0.34, 0.42, (-7.96, y, 5.12), mats["Dark Bronze"], collection, vertices=12, rotation=(0, math.radians(90), 0), bevel=0.035))

    # Blank stage-number marquee. Unity will add responsive STAGE 1 lettering later.
    sign += [
        box("Sign_MountLeft", (0.24, 0.14, 1.05), (-2.35, -5.91, 4.55), mats["Dark Bronze"], collection, 0.03),
        box("Sign_MountRight", (0.24, 0.14, 1.05), (2.35, -5.91, 4.55), mats["Dark Bronze"], collection, 0.03),
        box("Sign_BlankDisplaySurface", (5.15, 0.20, 1.00), SIGN_SURFACE, mats["Dark Bronze"], collection, 0.06),
        box("Sign_TerracottaTop", (5.28, 0.07, 0.09), (0, -6.20, 5.08), mats["Terracotta Accent"], collection, 0.02),
        box("Sign_GoldBottom", (5.28, 0.06, 0.07), (0, -6.20, 4.02), mats["Warm Gold"], collection, 0.016),
    ]

    components = {
        "Stage1_Body": join_named(body, "Stage1_Body"),
        "Stage1_Roof": join_named(roof, "Stage1_Roof"),
        "Stage1_Windows": join_named(windows, "Stage1_Windows"),
        "Stage1_Doors": join_named(doors, "Stage1_Doors"),
        "Stage1_Trim": join_named(trim, "Stage1_Trim"),
        "Stage1_Sign": join_named(sign, "Stage1_Sign"),
    }

    root = bpy.data.objects.new("SilverScreen_SoundStage1", None)
    collection.objects.link(root)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.65
    root["personnel_entrance_xyz_metres"] = PERSONNEL_TARGET
    root["personnel_entrance_facing"] = "-Y"
    root["loading_door_center_xyz_metres"] = LOADING_DOOR
    root["loading_door_facing"] = "+X"
    root["sign_surface_center_xyz_metres"] = SIGN_SURFACE
    root["sign_surface_dimensions_xyz_metres"] = (5.15, 0.20, 1.00)
    root["sign_surface_facing"] = "-Y"
    for obj in components.values():
        obj.parent = root
    return collection, mats, components, root


def create_camera():
    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return camera


def render_standard_previews(camera):
    scene = bpy.context.scene
    views = [
        ("01_front_personnel_entrance", (0, -29.0, 7.0), (0, 0, 3.6), 55),
        ("02_stage_door_service_side", (27.0, -4.0, 10.5), (0, -0.2, 3.6), 55),
        ("03_rear_side", (-23.0, 20.0, 12.5), (0, 0, 3.4), 56),
        ("04_elevated_three_quarter", (-22.0, -24.0, 18.5), (0, 0, 2.8), 55),
        ("05_gameplay_camera", (20.0, -25.0, 29.5), (0, 0, 1.8), 52),
    ]
    for filename, location, target, lens in views:
        point_camera(camera, location, target, lens)
        scene.render.filepath = str(REVIEW_DIR / f"{filename}.png")
        bpy.ops.render.render(write_still=True)


def import_fbx_root(path):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    root = next((obj for obj in imported if obj.parent is None), None)
    return root, imported


def render_scale_comparison(camera, stage_root):
    if not HQ_FBX_PATH.exists() or not CASTING_FBX_PATH.exists():
        return False
    hq_root, hq_objects = import_fbx_root(HQ_FBX_PATH)
    casting_root, casting_objects = import_fbx_root(CASTING_FBX_PATH)
    if hq_root is None or casting_root is None:
        return False

    hq_root.location.x = -14.0
    casting_root.location.x = -4.2
    stage_root.location.x = 9.0
    point_camera(camera, (29.0, -37.0, 28.5), (0, 0, 3.5), 55)
    bpy.context.scene.render.filepath = str(REVIEW_DIR / "06_scale_comparison.png")
    bpy.ops.render.render(write_still=True)
    stage_root.location.x = 0

    bpy.ops.object.select_all(action="DESELECT")
    for obj in hq_objects + casting_objects:
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    return True


def write_report(collection, mats, comparison_rendered):
    report = {
        "asset": "Silver Screen Sound Stage 1",
        "generator": "Blender Python 5.x",
        "coordinate_convention": "Metres, Z-up in Blender, front/personnel facade faces -Y, origin at base centre",
        "scene_fit_reference": {
            "placeholder_position_unity": [0.0, 0.0, -12.0],
            "placeholder_renderer_bounds_xyz_metres": [16.40, 7.30, 13.15],
            "placeholder_collider_bounds_xyz_metres": [16.40, 7.30, 11.40],
            "entrance_local_unity": list(PERSONNEL_TARGET),
            "entrance_world_unity": [0.0, 0.05, -18.70],
            "gameplay_camera_pitch_degrees": 52.0,
            "gameplay_camera_position_unity": [0.0, 18.0, -15.0],
            "gameplay_camera_fov_degrees": 60.0,
            "road_width_metres": 6.0,
            "hq_dimensions_xyz_metres": [9.38, 7.87, 8.81],
            "casting_dimensions_xyz_metres": [8.30, 7.33, 4.91],
        },
        **calculate_mesh_stats(collection),
        "components": list(obj.name for obj in collection.objects if obj.type == "MESH"),
        "materials": list(mats.keys()),
        "material_direction": {
            "primary_mass": ["Roof Charcoal", "Dark Bronze", "Cream Stucco"],
            "structural_accents": "Terracotta Accent",
            "sparse_glazing": "Smoked Glass",
            "gold_usage": "Personnel door mullion, canopy edge, and thin sign underline only",
        },
        "personnel_entrance": {
            "door_center_blender_xyz_metres": list(PERSONNEL_DOOR),
            "navigation_target_blender_xyz_metres": list(PERSONNEL_TARGET),
            "front_facing": "-Y",
            "purpose": "Gameplay employee entrance; intentionally separate from loading doors",
        },
        "loading_stage_door": {
            "center_blender_xyz_metres": list(LOADING_DOOR),
            "dimensions_xyz_metres": [0.20, 5.10, 5.15],
            "front_facing": "+X",
        },
        "dynamic_signage": {
            "modeled_lettering": False,
            "intended_text": "STAGE 1",
            "export_component": "Stage1_Sign",
            "generated_surface_part": "Sign_BlankDisplaySurface",
            "display_surface_dimensions_xyz_metres": [5.15, 0.20, 1.00],
            "front_surface_center_blender_xyz_metres": list(SIGN_SURFACE),
            "front_facing": "-Y",
        },
        "shared_generation": {
            "utility": "ArtSource/Shared/silver_screen_art_utils.py",
            "utility_modified": False,
            "reused_concepts": ["six-material palette", "beveled primitives", "camera aiming", "mesh statistics", "FBX export", "neutral preview lighting"],
        },
        "exports": {
            "fbx": {
                "forward": "-Z",
                "up": "Y",
                "units": "metres",
                "animations": False,
                "embedded_textures": False,
                "mesh_modifiers": True,
                "smoothing": "Face",
            }
        },
        "preview_files": [
            "01_front_personnel_entrance.png",
            "02_stage_door_service_side.png",
            "03_rear_side.png",
            "04_elevated_three_quarter.png",
            "05_gameplay_camera.png",
        ] + (["06_scale_comparison.png"] if comparison_rendered else []),
        "validation": {"generation_completed": True, "reopen_and_fbx_import": "pending"},
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


def bounds_for_objects(objects):
    minimum = Vector((1e9, 1e9, 1e9))
    maximum = Vector((-1e9, -1e9, -1e9))
    for obj in (item for item in objects if item.type == "MESH"):
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            minimum.x = min(minimum.x, world.x)
            minimum.y = min(minimum.y, world.y)
            minimum.z = min(minimum.z, world.z)
            maximum.x = max(maximum.x, world.x)
            maximum.y = max(maximum.y, world.y)
            maximum.z = max(maximum.z, world.z)
    return [round(v, 3) for v in maximum - minimum]


def validate_saved_outputs():
    collection = bpy.data.collections.get(MODEL_COLLECTION)
    root = bpy.data.objects.get("SilverScreen_SoundStage1")
    expected = {"Stage1_Body", "Stage1_Roof", "Stage1_Windows", "Stage1_Doors", "Stage1_Trim", "Stage1_Sign"}
    actual = {obj.name for obj in collection.objects if obj.type == "MESH"}
    blend_ok = collection is not None and root is not None and expected == actual
    no_text = not any(obj.type in {"FONT", "CURVE"} for obj in collection.objects)
    metadata_ok = (
        tuple(round(v, 2) for v in root["personnel_entrance_xyz_metres"]) == tuple(round(v, 2) for v in PERSONNEL_TARGET)
        and root["personnel_entrance_facing"] == "-Y"
        and root["sign_surface_facing"] == "-Y"
    )

    fbx_root, imported = import_fbx_root(FBX_PATH)
    imported_meshes = [obj for obj in imported if obj.type == "MESH"]
    imported_names = {obj.name.split(".")[0] for obj in imported_meshes}
    imported_materials = sorted({slot.material.name for obj in imported_meshes for slot in obj.material_slots if slot.material})
    fbx_ok = fbx_root is not None and expected.issubset(imported_names)
    dimensions = bounds_for_objects(imported)
    orientation_ok = dimensions[0] > dimensions[1] and dimensions[2] > 8.0

    report = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
    report["validation"] = {
        "blend_reopened": True,
        "expected_components": sorted(expected),
        "actual_components": sorted(actual),
        "component_check_passed": blend_ok,
        "font_or_text_geometry_present": not no_text,
        "metadata_check_passed": metadata_ok,
        "fbx_reimported": True,
        "fbx_component_check_passed": fbx_ok,
        "fbx_bounds_xyz_metres": dimensions,
        "fbx_orientation_check_passed": orientation_ok,
        "fbx_materials": imported_materials,
        "material_assignment_check_passed": len(imported_materials) == 6,
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    if not all((blend_ok, no_text, metadata_ok, fbx_ok, orientation_ok, len(imported_materials) == 6)):
        raise RuntimeError("Sound Stage validation failed; inspect generation_report.json")
    print(json.dumps(report["validation"], indent=2))


def generate():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    reset_scene()
    collection, mats, _, root = build_model()
    export_fbx(collection, FBX_PATH)
    setup_preview_world(46)
    camera = create_camera()
    render_standard_previews(camera)
    comparison_rendered = render_scale_comparison(camera, root)
    report = write_report(collection, mats, comparison_rendered)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)
    print(json.dumps(report["mesh_totals"], indent=2))
    print(f"Generated {BLEND_PATH}")
    print(f"Exported {FBX_PATH}")
    print(f"Rendered previews to {REVIEW_DIR}")


if __name__ == "__main__":
    if "--validate-only" in sys.argv:
        validate_saved_outputs()
    else:
        generate()
