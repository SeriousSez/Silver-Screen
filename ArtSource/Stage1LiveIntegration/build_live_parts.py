"""Read revision 04; derive live door parts with a thinner front-right personnel leaf.
The approved visual-master source remains unchanged; this live derivative owns the correction.
Run with Blender --background --python ArtSource/Stage1LiveIntegration/build_live_parts.py.
"""
import bpy
import bmesh
import hashlib
import json
import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
SOURCE = REPO / 'ArtSource/Stage1CleanCandidate/Stage1_CleanCandidate_A.blend'
EXPORT = REPO / 'ArtExports/Stage1LiveIntegration/Stage1_PersonnelLiveParts.fbx'
UNITY = REPO / 'Assets/SilverScreen/Environment/Stage1Live/Models'
FIXED = ('PersonnelSteel', 'PersonnelThreshold', 'PersonnelStop', 'PersonnelDoorSeal', 'LatchStrike', 'HingeJambAttachment')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

source_hash = digest(SOURCE)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
parts = [o for o in bpy.data.objects if o.type == 'MESH' and o.parent and o.parent.name == 'PersonnelDoors']
moving = [o for o in parts if o.get('personnel_opening') == 'FrontRightEntry' and not o.name.startswith(FIXED)]
assert len(parts) == 490 and len(moving) == 53

def triangles(objects):
    """Winding-independent closed-pose geometry/UV/material fingerprint."""
    rows = []
    for o in objects:
        o.data.calc_loop_triangles()
        uv = o.data.uv_layers.active
        for t in o.data.loop_triangles:
            corners = []
            for vi, li in zip(t.vertices, t.loops):
                p = o.matrix_world @ o.data.vertices[vi].co
                tex = uv.data[li].uv if uv else (0, 0)
                corners.append(tuple(round(v, 6) for v in (*p, *tex)))
            rows.append((o.data.materials[t.material_index].name, tuple(sorted(corners))))
    return sorted(rows)

original = triangles(parts)
# The live personnel leaf used a 110 mm core and 211 mm overall timber depth.
# Preserve its width/height/style while fitting a 55 mm core with shallow raised
# joinery. Keep hinge pins fixed, extend their cranked returns, and seat hardware
# and the rebated stops against the corrected faces. All units are metres.
front_parts = [o for o in parts if o.get('personnel_opening') == 'FrontRightEntry']
unaffected = triangles([o for o in parts if o not in front_parts])
centre_y = -12.09
thickness_changes = []
for o in front_parts:
    name = o.name
    old = [v.co.copy() for v in o.data.vertices]
    def depth(d):
        if abs(d) <= .055:
            return d * .5
        return (1 if d > 0 else -1) * (.0275 + (abs(d) - .055) * .15)
    for v in o.data.vertices:
        d = v.co.y - centre_y
        if name.startswith(('FrontRightEntryCore', 'RaisedDoorStile', 'DoorCrossRail',
                            'RecessedDoorPanel', 'InteriorEntryStile', 'InteriorEntryRail',
                            'PersonnelLeafBottomSeal', 'MortiseLockEdgePlate')):
            v.co.y = centre_y + depth(d)
        elif name.startswith(('PersonnelStop', 'PersonnelDoorSeal')):
            v.co.y -= .0275
        elif name.startswith('CrankedHingeReturn'):
            v.co.y = centre_y - .18 + (d + .18) * (.132 / .06)
        elif name.startswith(('PersonnelHingeLeaf', 'HingeLeafScrew')):
            v.co.y += .072
        elif name.startswith(('MortiseKeyEscutcheon', 'Keyhole', 'LockEscutcheonScrew')):
            v.co.y += .073
        elif name.startswith(('InteriorMortiseThumbturnPlate', 'InteriorLock')):
            v.co.y -= .073
        elif name.startswith(('ForgedPull', 'PullEscutcheon', 'EscutcheonScrew')):
            # Keep the forged pull's section, silhouette and projection; move the
            # complete assembly onto the new timber face rather than crushing it.
            v.co.y += .075 if d < 0 else -.085
    if any((v.co - before).length > 1e-7 for v, before in zip(o.data.vertices, old)):
        thickness_changes.append(name)
        o.data.update()
assert triangles([o for o in parts if o not in front_parts]) == unaffected
corrected = triangles(parts)
# Positive depth mappings preserve topology. Verify the exported source pieces
# remain closed, outward and non-degenerate rather than trusting the remap.
geometry_validation = {'components': len(front_parts), 'open_edges': 0,
                       'degenerate_faces': 0, 'non_positive_volumes': 0}
for o in front_parts:
    bm = bmesh.new(); bm.from_mesh(o.data)
    geometry_validation['open_edges'] += sum(not e.is_manifold for e in bm.edges)
    geometry_validation['degenerate_faces'] += sum(f.calc_area() <= 1e-12 for f in bm.faces)
    geometry_validation['non_positive_volumes'] += int(bm.calc_volume(signed=True) <= 0)
    bm.free()
assert not any(geometry_validation[k] for k in ('open_edges','degenerate_faces','non_positive_volumes')), geometry_validation
wood = [o for o in front_parts if o.data.materials and o.data.materials[0].name == 'C1_Timber']
wood_points = [o.matrix_world @ v.co for o in wood for v in o.data.vertices]
geometry_validation['timber_depth_m'] = max(v.y for v in wood_points) - min(v.y for v in wood_points)
assert abs(geometry_validation['timber_depth_m'] - .07015) < .00001


colliders = []
for o in bpy.data.objects:
    if o.type != 'MESH' or not o.parent:
        continue
    group = o.parent.name
    vertices = [o.matrix_world @ v.co for v in o.data.vertices]
    low = [min(v[i] for v in vertices) for i in range(3)]
    high = [max(v[i] for v in vertices) for i in range(3)]
    # Individual wall segments retain authored holes. Fine services are represented
    # only by their enclosing cabinets/bollards, never by one building-sized hull.
    physical = group in ('ExteriorWalls', 'MasonryFraming', 'InteriorFloor') or (
        group == 'PrimaryStructure' and low[2] < 2.1) or o.name.startswith(('BollardTube', 'ElectricalCabinet', 'InteriorPortalLiner'))
    if physical and high[2] > .02:
        colliders.append({'name': o.name, 'group': group,
                          'center': [(low[i]+high[i])/2 for i in (0, 2, 1)],
                          'size': [high[i]-low[i] for i in (0, 2, 1)]})

root = bpy.data.objects.new('Stage1_PersonnelLiveParts', None)
bpy.context.scene.collection.objects.link(root)
exports = []
for name, subset in [('LivePersonnelRemainder', [o for o in parts if o not in moving]), ('LiveFrontRightLeaf', moving)]:
    bpy.ops.object.select_all(action='DESELECT')
    copies = []
    for o in subset:
        c = o.copy(); c.data = o.data.copy()
        bpy.context.scene.collection.objects.link(c)
        c.select_set(True); copies.append(c)
    bpy.context.view_layer.objects.active = copies[0]
    bpy.ops.object.join()
    c = bpy.context.object; c.name = name; c.parent = root
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    exports.append(c)
assert triangles(exports) == corrected, 'Joining changed corrected geometry, UVs or materials'
EXPORT.parent.mkdir(parents=True, exist_ok=True); UNITY.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
for o in exports: o.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(filepath=str(EXPORT), use_selection=True, object_types={'EMPTY','MESH'},
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    add_leaf_bones=False, bake_anim=False, use_custom_props=True)
shutil.copy2(EXPORT, UNITY / EXPORT.name)
assert digest(SOURCE) == source_hash
manifest = {'source_sha256': source_hash, 'revision': '04', 'source_components': len(parts),
            'moving_components': len(moving), 'closed_triangles': len(original),
            'closed_geometry_uv_material_equivalent': False,
            'unaffected_personnel_geometry_uv_material_equivalent': True,
            'front_right_core_thickness_before_m': .110, 'front_right_core_thickness_after_m': .055,
            'front_right_leaf_width_m': 1.04, 'front_right_leaf_height_m': 2.278,
            'thickness_adjusted_components': thickness_changes, 'geometry_validation': geometry_validation, 'fbx_sha256': digest(EXPORT),
            'hinge_unity': [5.138, 0, -12.27], 'held_open_degrees': 95,
            'colliders': colliders}
(HERE/'integration_manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
print(json.dumps({k:v for k,v in manifest.items() if k != 'colliders'}))
