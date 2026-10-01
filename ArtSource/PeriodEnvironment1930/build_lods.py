"""Non-destructive LOD derivatives from approved Blender masters.

Blender --background --python-exit-code 1 --python ArtSource/PeriodEnvironment1930/build_lods.py
LOD0 packets, sources, materials and canonical prefabs are never written here.
"""
import bpy
import hashlib
import json
import math
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import geometry as G

# Semantic omissions, not a universal percentage applied to an assembled mesh.
SMALL_HARDWARE = re.compile(r'bolt|screw|rivet|fastener|nail|tack|washer|cotter|pin head|knuckle pin|nut\b', re.I)
FAR_DETAIL = re.compile(r'wear|scratch|grain|finger hole|number|label|notice title|stitch|thread|saddle|tension band|pipe clamp|strap|cradle buffer|contact patina|runoff|putty', re.I)
PROTECTED = re.compile(r'wall|jamb|lining|partition|floor|roof|sheet|glass|pane|sill|frame|parapet|coping|lintel|post|rafter|door leaf|door panel|shade|handset|wheel|title|descriptor', re.I)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def simplify(source, collection, level):
    name = source.name
    size = sorted(source.dimensions)
    important = bool(PROTECTED.search(name))
    if SMALL_HARDWARE.search(name) and size[-1] < (.20 if level == 1 else .55):
        return None, 'small fastener'
    if level == 2 and FAR_DETAIL.search(name) and not important:
        return None, 'distant surface/hardware detail'
    if size[-1] < (.018 if level == 1 else .065) and not important:
        return None, 'subpixel component'

    obj = source.copy()
    obj.data = source.data.copy()
    collection.objects.link(obj)
    obj.hide_set(False)
    bpy.context.view_layer.objects.active = obj
    # Preserve real cutaway surfaces, holes, walls, material borders and UVs.
    # Collapse operates separately on each construction component, never across
    # a doorway, material boundary or disconnected wall/furniture assembly.
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    if triangles > 24:
        planar = obj.modifiers.new('LOD coplanar dissolve', 'DECIMATE')
        planar.decimate_type = 'DISSOLVE'
        planar.angle_limit = math.radians(1.0 if level == 1 else 4.0)
        planar.delimit = {'NORMAL', 'MATERIAL', 'UV'}
        bpy.ops.object.modifier_apply(modifier=planar.name)
        obj.data.calc_loop_triangles()
        triangles = len(obj.data.loop_triangles)
        # Architectural panels get a high minimum; curved small objects lose
        # radial density. Letter silhouettes retain planar front faces.
        ratio = (.55 if important else .32) if level == 1 else (.30 if important else .14)
        if 'woven galvanized diamond' in name.lower():
            ratio = .30 if level == 1 else .15
        minimum = 32 if important else 16
        # A very thin barrel lid can collapse through its own thickness,
        # changing the top-face lighting even when the silhouette survives.
        # Retaining these few planar faces costs little and prevents a dark pop.
        flat_lid = 'seated barrel head' in name.lower()
        if triangles > minimum and not flat_lid:
            collapse = obj.modifiers.new('LOD component simplification', 'DECIMATE')
            collapse.ratio = max(ratio, minimum / triangles)
            collapse.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=collapse.name)
    return obj, 'component simplification'


def build(source, canonical, output):
    source_hash = sha(source)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    for layer in bpy.context.view_layer.layer_collection.children:
        layer.exclude = False
    bpy.context.view_layer.update()
    originals = [c for c in bpy.data.collections if not c.library and any(o.type == 'MESH' for o in c.objects)]
    # Snapshot dimensions once, then avoid evaluating thousands of unrelated
    # original/previous derivative objects for every modifier operation.
    for layer in bpy.context.view_layer.layer_collection.children:
        layer.exclude = True
    packets, records, derivatives = [], [], []
    for collection in originals:
        source_objects = [o for o in collection.objects if o.type == 'MESH']
        for level in (1, 2):
            target = bpy.data.collections.new(collection.name + '_LOD' + str(level))
            bpy.context.scene.collection.children.link(target)
            derivatives.append(target)
            removed = []
            for source_obj in source_objects:
                obj, reason = simplify(source_obj, target, level)
                if obj is None:
                    removed.append(dict(name=source_obj.name, reason=reason))
            if not len(target.objects):
                # Very small complete catalog assets retain a representative
                # shape; placement-level distance culling is the consumer's job.
                obj = max(source_objects, key=lambda o:max(o.dimensions)).copy()
                obj.data = obj.data.copy()
                target.objects.link(obj)
            p = G.uv_packet(target, canonical)
            packets.append(p)
            records.append(dict(name=collection.name, level=level,
                                vertices=len(p['positions'])//3,
                                triangles=sum(len(s['indices'])//3 for s in p['submeshes']),
                                removed=removed))
            bpy.context.view_layer.layer_collection.children[target.name].exclude = True
        print('LOD', collection.name, flush=True)
    provenance = dict(source=str(source.relative_to(ROOT)), sourceSha256=source_hash,
                      generatorSha256=sha(Path(__file__)), schema=1)
    G.save_packet(output/'lod_meshes.json.gz', dict(parts=packets))
    (output/'lod_manifest.json').write_text(json.dumps(dict(**provenance, assets=records), indent=2))
    # Save only derivative collections. The approved master remains untouched.
    for c in list(bpy.data.collections):
        if c not in derivatives:
            bpy.data.collections.remove(c)
    for obj in list(bpy.data.objects):
        if not obj.users_collection:
            bpy.data.objects.remove(obj)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'LOD_Derivatives.blend'))
    assert sha(source) == source_hash
    print('LOD_EXPORT', output, sum(r['triangles'] for r in records), flush=True)


build(HERE/'PeriodEnvironment1930.blend', True, ROOT/'ArtExports/PeriodEnvironment1930/LOD')
build(ROOT/'ArtSource/StudioServices/StudioServices_Production.blend', False, ROOT/'ArtExports/StudioServices/LOD')
