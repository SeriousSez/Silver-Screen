"""Generate Silver Screen's original 1930s Casting Office and review renders.

Run with Blender 5.x:
  blender --background --python generate_casting_office.py

The building uses metres, Blender Z-up, a base-centre origin, and faces local -Y.
It is intentionally exported outside Unity's Assets folder pending art approval.
"""

import json
import math
import sys
from pathlib import Path

import bpy


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
    move_to_collection,
    point_camera,
    reset_scene,
    setup_preview_world,
)


EXPORT_DIR = ROOT / "ArtExports" / "Milestone12_CastingOffice"
REVIEW_DIR = ROOT / "ArtReview" / "Milestone12_CastingOffice"
BLEND_PATH = HERE / "SilverScreen_CastingOffice.blend"
FBX_PATH = EXPORT_DIR / "SilverScreen_CastingOffice.fbx"
REPORT_PATH = HERE / "generation_report.json"
HQ_FBX_PATH = ROOT / "ArtExports" / "Milestone10_StudioHeadquarters" / "SilverScreen_StudioHeadquarters.fbx"
MODEL_COLLECTION = "CASTING_EXPORT"


def build_model():
    collection = bpy.data.collections.new(MODEL_COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    mats = create_palette("CASTING")

    body = []
    roof = []
    windows = []
    doors = []
    trim = []
    sign = []

    # Low, broad one-storey administration shell with an asymmetric audition bay.
    body += [
        box("Body_Main", (7.55, 5.70, 3.55), (0.20, 0.05, 1.775), mats["Cream Stucco"], collection, 0.11),
        (audition_bay := box("Body_LeftAuditionBay", (2.45, 0.72, 3.15), (-2.65, -3.02, 1.575), mats["Terracotta Accent"], collection, 0.09)),
        box("Body_EntryPier", (2.20, 0.48, 3.82), (0.25, -3.08, 1.91), mats["Cream Stucco"], collection, 0.09),
        box("Body_RightServiceReturn", (1.55, 0.42, 3.25), (3.45, -2.84, 1.625), mats["Cream Stucco"], collection, 0.08),
        box("Body_PlacementPlinth", (8.15, 6.20, 0.18), (0, 0, 0.09), mats["Terracotta Accent"], collection, 0.05),
    ]

    # Three shallow recessed grooves give the terracotta audition bay a restrained Deco rhythm.
    # They read through light and shadow without introducing a new material or dense surface detail.
    for index, x in enumerate((-3.02, -2.65, -2.28)):
        cutter = box(f"TemporaryGrooveCutter_{index}", (0.10, 0.10, 1.72), (x, -3.375, 1.72), None, collection, 0)
        modifier = audition_bay.modifiers.new(f"Recessed groove {index + 1}", "BOOLEAN")
        modifier.operation = "DIFFERENCE"
        modifier.solver = "EXACT"
        modifier.object = cutter
        bpy.context.view_layer.objects.active = audition_bay
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.data.objects.remove(cutter, do_unlink=True)

    # Modest stepped parapet and offset clerestory create a distinct, readable silhouette.
    roof += [
        box("Roof_MainCap", (8.05, 6.05, 0.22), (0.12, 0.04, 3.66), mats["Roof Charcoal"], collection, 0.055),
        box("Roof_LeftParapet", (3.15, 0.34, 0.48), (-2.38, -2.90, 3.91), mats["Terracotta Accent"], collection, 0.055),
        box("Roof_RightParapet", (2.35, 0.34, 0.34), (2.72, -2.90, 3.84), mats["Cream Stucco"], collection, 0.05),
        box("Roof_EntryStepWide", (2.75, 1.90, 0.52), (0.25, -1.98, 4.02), mats["Cream Stucco"], collection, 0.065),
        box("Roof_EntryStepNarrow", (1.92, 1.48, 0.48), (0.25, -2.10, 4.50), mats["Terracotta Accent"], collection, 0.06),
        box("Roof_EntryCap", (2.15, 1.70, 0.18), (0.25, -2.10, 4.82), mats["Roof Charcoal"], collection, 0.045),
        box("Roof_RearParapet", (7.75, 0.30, 0.30), (0.15, 2.90, 3.80), mats["Cream Stucco"], collection, 0.045),
    ]

    # Horizontal bands echo HQ; side continuation prevents blank slab-like elevations.
    for z in (0.55, 3.22):
        trim.append(box(f"Trim_FrontBand_{z}", (7.90, 0.13, 0.14), (0.10, -2.88, z), mats["Terracotta Accent"], collection, 0.022))
    for side_x in (-3.84, 4.04):
        for z in (0.55, 3.22):
            trim.append(box(f"Trim_SideBand_{side_x}_{z}", (0.13, 5.65, 0.14), (side_x, 0.05, z), mats["Terracotta Accent"], collection, 0.022))

    trim += [
        box("Trim_EntryLeft", (0.22, 0.18, 2.72), (-0.83, -3.37, 1.62), mats["Dark Bronze"], collection, 0.03),
        box("Trim_EntryRight", (0.22, 0.18, 2.72), (1.33, -3.37, 1.62), mats["Dark Bronze"], collection, 0.03),
        box("Trim_EntryTop", (2.34, 0.18, 0.22), (0.25, -3.37, 2.96), mats["Dark Bronze"], collection, 0.03),
        box("Trim_Canopy", (3.35, 1.18, 0.22), (0.25, -3.58, 3.08), mats["Dark Bronze"], collection, 0.06),
        box("Trim_CanopyLip", (3.45, 0.10, 0.12), (0.25, -4.18, 3.04), mats["Warm Gold"], collection, 0.02),
        box("Trim_RearBand", (7.75, 0.13, 0.14), (0.15, 2.93, 2.72), mats["Terracotta Accent"], collection, 0.022),
        box("Trim_LeftCornerPilaster", (0.28, 0.32, 3.15), (-3.76, -2.72, 1.72), mats["Terracotta Accent"], collection, 0.035),
    ]

    # Large recessed lobby/audition windows with chunky bronze frames and simple mullions.
    front_windows = [(-2.70, 1.70, 1.78), (2.62, 1.55, 1.60)]
    for index, (x, width, height) in enumerate(front_windows):
        trim.append(box(f"WindowFrame_Front_{index}", (width + 0.20, 0.07, height + 0.20), (x, -3.18, 1.78), mats["Dark Bronze"], collection, 0))
        windows.append(box(f"Window_Front_{index}", (width, 0.14, height), (x, -3.25, 1.78), mats["Smoked Glass"], collection, 0.035))
        trim.append(box(f"WindowMullion_Front_{index}", (0.07, 0.06, height - 0.12), (x, -3.35, 1.78), mats["Dark Bronze"], collection, 0.012))

    # Side and rear fenestration keep the building finished but practical.
    for side_x in (-3.88, 4.08):
        for y in (-1.25, 0.55, 2.00):
            trim.append(box(f"WindowFrame_Side_{side_x}_{y}", (0.07, 1.12, 1.28), (side_x - math.copysign(0.06, side_x), y, 1.75), mats["Dark Bronze"], collection, 0))
            windows.append(box(f"Window_Side_{side_x}_{y}", (0.14, 0.94, 1.10), (side_x, y, 1.75), mats["Smoked Glass"], collection, 0.03))
    for x in (-2.45, -0.85, 0.85, 2.45):
        trim.append(box(f"WindowFrame_Rear_{x}", (1.18, 0.07, 1.24), (x, 2.88, 1.76), mats["Dark Bronze"], collection, 0))
        windows.append(box(f"Window_Rear_{x}", (1.00, 0.14, 1.06), (x, 2.95, 1.76), mats["Smoked Glass"], collection, 0.03))

    doors += [
        box("Doors_MainGlass", (1.72, 0.16, 2.36), (0.25, -3.39, 1.42), mats["Smoked Glass"], collection, 0.035),
        box("Doors_RearService", (1.20, 0.15, 2.14), (3.05, 3.02, 1.17), mats["Dark Bronze"], collection, 0.035),
    ]
    trim += [
        box("Doors_CentreMullion", (0.07, 0.08, 2.22), (0.25, -3.50, 1.42), mats["Dark Bronze"], collection, 0.012),
        box("Doors_LeftHandle", (0.045, 0.045, 0.38), (0.08, -3.55, 1.42), mats["Warm Gold"], collection, 0.01),
        box("Doors_RightHandle", (0.045, 0.045, 0.38), (0.42, -3.55, 1.42), mats["Warm Gold"], collection, 0.01),
        # Practical poster/audition notice case, deliberately blank at gameplay scale.
        box("Trim_AuditionNoticeFrame", (0.92, 0.10, 1.18), (-1.28, -3.28, 1.72), mats["Dark Bronze"], collection, 0.025),
        box("Trim_AuditionNoticeInset", (0.72, 0.05, 0.98), (-1.28, -3.35, 1.72), mats["Cream Stucco"], collection, 0.018),
    ]

    # Blank CASTING marquee: physical backing only; later Unity world-space text sits on the front face.
    sign += [
        box("Sign_MountLeft", (0.18, 0.12, 0.70), (-1.35, -3.20, 3.70), mats["Dark Bronze"], collection, 0.025),
        box("Sign_MountRight", (0.18, 0.12, 0.70), (1.85, -3.20, 3.70), mats["Dark Bronze"], collection, 0.025),
        box("Sign_BlankDisplaySurface", (3.70, 0.18, 0.68), (0.25, -3.34, 3.70), mats["Dark Bronze"], collection, 0.05),
        box("Sign_GoldBottom", (3.78, 0.055, 0.06), (0.25, -3.45, 3.35), mats["Warm Gold"], collection, 0.015),
    ]

    # Two low bollards communicate a busier public-facing entrance without small clutter.
    trim += [
        cylinder("EntryBollard_Left", 0.11, 0.72, (-1.28, -3.88, 0.38), mats["Dark Bronze"], collection, vertices=10),
        cylinder("EntryBollard_Right", 0.11, 0.72, (1.78, -3.88, 0.38), mats["Dark Bronze"], collection, vertices=10),
    ]

    components = {
        "Casting_Body": join_named(body, "Casting_Body"),
        "Casting_Roof": join_named(roof, "Casting_Roof"),
        "Casting_Windows": join_named(windows, "Casting_Windows"),
        "Casting_Doors": join_named(doors, "Casting_Doors"),
        "Casting_Trim": join_named(trim, "Casting_Trim"),
        "Casting_Sign": join_named(sign, "Casting_Sign"),
    }

    root = bpy.data.objects.new("SilverScreen_CastingOffice", None)
    collection.objects.link(root)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.45
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
        ("01_front", (0, -20.0, 4.6), (0, 0, 2.25), 54),
        ("02_rear_side", (13.5, 13.0, 8.5), (0, 0, 2.0), 54),
        ("03_elevated_three_quarter", (-11.5, -13.2, 9.5), (0, 0, 1.8), 56),
        ("04_gameplay_camera", (11.5, -13.0, 16.5), (0, 0, 1.25), 50),
    ]
    for filename, location, target, lens in views:
        point_camera(camera, location, target, lens)
        scene.render.filepath = str(REVIEW_DIR / f"{filename}.png")
        bpy.ops.render.render(write_still=True)


def render_scale_comparison(camera, casting_root):
    if not HQ_FBX_PATH.exists():
        return False

    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(HQ_FBX_PATH))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    hq_root = next((obj for obj in imported if obj.parent is None), None)
    if hq_root is None:
        return False

    hq_root.location.x = -6.0
    casting_root.location.x = 5.3
    point_camera(camera, (19.0, -24.0, 19.0), (0, 0, 2.8), 53)
    bpy.context.scene.render.filepath = str(REVIEW_DIR / "05_scale_comparison.png")
    bpy.ops.render.render(write_still=True)
    casting_root.location.x = 0

    bpy.ops.object.select_all(action="DESELECT")
    for obj in imported:
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    return True


def write_report(collection, mats, comparison_rendered):
    report = {
        "asset": "Silver Screen Casting Office",
        "generator": "Blender Python 5.x",
        "coordinate_convention": "Metres, Z-up in Blender, front faces -Y, origin at base centre",
        "scene_fit_reference": {
            "placeholder_position_unity": [15.0, 0.0, 9.0],
            "placeholder_bounds_size_unity_xyz_metres": [7.90, 4.30, 8.15],
            "entrance_local_unity": [0.0, 0.05, -4.2],
            "gameplay_camera_pitch_degrees": 52.0,
            "gameplay_camera_position_unity": [0.0, 18.0, -15.0],
            "gameplay_camera_fov_degrees": 60.0,
            "road_width_metres": 6.0,
            "hq_dimensions_xyz_metres": [9.38, 7.87, 8.81],
        },
        **calculate_mesh_stats(collection),
        "materials": list(mats.keys()),
        "dynamic_signage": {
            "modeled_lettering": False,
            "intended_text": "CASTING",
            "export_component": "Casting_Sign",
            "generated_surface_part": "Sign_BlankDisplaySurface",
            "display_surface_dimensions_metres": [3.70, 0.18, 0.68],
            "front_surface_center_blender_xyz_metres": [0.25, -3.43, 3.70],
            "front_facing": "-Y",
        },
        "entrance": {
            "door_center_blender_xyz_metres": [0.25, -3.39, 1.42],
            "navigation_target_blender_xyz_metres": [0.0, -4.20, 0.05],
            "front_facing": "-Y",
        },
        "refinement": {
            "terracotta_feature_wall": "Three shallow boolean-cut vertical grooves; no added material or lettering",
            "canopy_projection_increase_metres": 0.12,
            "preserved_canopy_width_metres": 3.35,
        },
        "shared_generation": {
            "utility": "ArtSource/Shared/silver_screen_art_utils.py",
            "reused_concepts": ["palette", "beveled primitives", "camera aiming", "mesh statistics", "FBX export", "neutral preview lighting"],
        },
        "exports": {
            "fbx": {
                "forward": "-Z",
                "up": "Y",
                "units": "metres",
                "animations": False,
                "embedded_textures": False,
            }
        },
        "preview_files": [
            "01_front.png",
            "02_rear_side.png",
            "03_elevated_three_quarter.png",
            "04_gameplay_camera.png",
        ] + (["05_scale_comparison.png"] if comparison_rendered else []),
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


def main():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    reset_scene()
    collection, mats, _, root = build_model()
    export_fbx(collection, FBX_PATH)
    setup_preview_world(34)
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
    main()
