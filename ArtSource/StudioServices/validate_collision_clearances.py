"""Read-only checks for the six reported Studio Services dressing/hardware clashes.

Run with Blender --background --python. Hardware fasteners and intended bearing
contacts are excluded; this checks the specific unwanted intersections, not a
blanket no-intersection rule for assembled furniture.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtReview/StudioServices/CollisionQA'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'))
for layer in bpy.context.view_layer.layer_collection.children:
    layer.exclude = False
bpy.context.view_layer.update()
depsgraph = bpy.context.evaluated_depsgraph_get()
checks = []


def check(name, passed, evidence):
    checks.append(dict(name=name, passed=bool(passed), evidence=evidence))


def objects(asset, prefixes=None):
    return [o for o in bpy.data.collections[asset].objects
            if o.type == 'MESH' and (prefixes is None or o.name.startswith(prefixes))]


def geometry(items, transform=Matrix.Identity(4)):
    vertices, faces = [], []
    for obj in items:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        first = len(vertices)
        matrix = transform @ obj.matrix_world
        vertices.extend(matrix @ v.co for v in mesh.vertices)
        faces.extend(tuple(first + i for i in p.vertices) for p in mesh.polygons)
        evaluated.to_mesh_clear()
    assert vertices and faces, 'No geometry matched the requested source parts'
    return vertices, faces


def bounds(vertices):
    return [min(p[i] for p in vertices) for i in range(3)], [max(p[i] for p in vertices) for i in range(3)]


def tree(items, transform=Matrix.Identity(4)):
    return BVHTree.FromPolygons(*geometry(items, transform))


bench = 'BenchSlatted_210_1930'
seats = objects(bench, 'Seat slat')
backs = objects(bench, 'Back slat')
supports = objects(bench, ('Cast bench end', 'Back support return', 'Raked back upright'))
check('Bench supports do not cut through wooden slats',
      not tree(seats + backs).overlap(tree(supports)),
      'Evaluated triangle intersections, excluding fasteners and the seat underside bearing contact checked separately')
seat_lo, seat_hi = bounds(geometry(seats)[0])
_, bearing_hi = bounds(geometry(objects(bench, ('Cast bench end', 'Seat support angle')))[0])
check('Bench bearing metal remains below seat surface', bearing_hi[2] <= seat_lo[2] + .0001,
      dict(seatUnderside=seat_lo[2], bearingTop=bearing_hi[2]))

door = 'ServiceDoorPair_485x346_1930'
bolts = objects(door, ('Drop bolt', 'Vertical drop bolt', 'Bolt floor keeper'))
diagonals = objects(door, 'Door diagonal')
check('Workshop drop bolts clear both timber diagonals', not tree(bolts).overlap(tree(diagonals)),
      'Evaluated triangle intersections for backplates, shafts, guides, keepers and diagonal timbers')

manifest = json.loads((ROOT / 'ArtExports/StudioServices/Production/production_manifest.json').read_text())
fixtures = manifest['fixtures']


def placed(fixture, prefixes=None):
    p = fixture['position']
    transform = Matrix.Translation(Vector((p[0], p[2], p[1]))) @ Matrix.Rotation(math.radians(180-fixture['yaw']), 4, 'Z')
    return geometry(objects(fixture['asset'], prefixes), transform)[0]


def gap(a, b):
    lo_a, hi_a = bounds(a)
    lo_b, hi_b = bounds(b)
    return max(max(lo_a[i]-hi_b[i], lo_b[i]-hi_a[i]) for i in range(3))


storage_crates = [f for f in fixtures if f['asset'].startswith('CrateTimber') and f['group'] == 'InteriorProps' and f['position'][0] < 0 and f['position'][2] > 2]
storage_sacks = [f for f in fixtures if f['asset'].startswith('SackCanvas') and f['position'][0] < 0]
assert len(storage_crates) >= 1 and len(storage_sacks) == 2
clearances = [gap(placed(s), placed(c)) for s in storage_sacks for c in storage_crates]
check('Storage sacks clear all adjacent crates', min(clearances) > .08,
      dict(minimumAxisSeparationMetres=min(clearances), pairs=len(clearances)))
check('Storage sacks clear one another', gap(*[placed(f) for f in storage_sacks]) > .08,
      dict(axisSeparationMetres=gap(*[placed(f) for f in storage_sacks])))

yard_bench = next(f for f in fixtures if f['asset'].startswith('WorkbenchJoiner') and f['position'][0] > 7)
yard_barrel = next(f for f in fixtures if f['asset'].startswith('BarrelTimber') and f['position'][0] > 9)
clearance = gap(placed(yard_bench), placed(yard_barrel))
check('Yard barrel clears workbench including vise handle', clearance > .15,
      dict(completeMeshBoundsSeparationMetres=clearance))
yard_crates = [f for f in fixtures if f['asset'].startswith('CrateTimber') and 7 < f['position'][0] < 8]
bench_top_items = [o for o in objects(yard_bench['asset'], 'Board') if o.location.z > .8]
p = yard_bench['position']
bench_transform = Matrix.Translation(Vector((p[0], p[2], p[1]))) @ Matrix.Rotation(math.radians(180-yard_bench['yaw']), 4, 'Z')
bench_top = geometry(bench_top_items, bench_transform)[0]
clearance = min(gap(placed(c), bench_top) for c in yard_crates)
check('Yard stacked crates have clear space ahead of bench top', clearance > .40,
      dict(separationMetres=clearance))
right_window = next(f for f in fixtures if f['asset'].startswith('WindowSteel') and f['group'] == 'RightWall')
clearance = min(gap(placed(c), placed(right_window)) for c in yard_crates)
check('Yard stacked crates clear projecting side-window sill', clearance > .10,
      dict(completeMeshBoundsSeparationMetres=clearance,
           crateBounds=[bounds(placed(c)) for c in yard_crates],
           windowBounds=bounds(placed(right_window))))
yard_neighbours = [f for f in fixtures if f['group'] == 'YardProps'
                   and f['asset'].startswith(('Barrel', 'Wheelbarrow', 'Ladder'))]
clearances = [dict(asset=f['asset'], separationMetres=min(gap(placed(c), placed(f)) for c in yard_crates))
              for f in yard_neighbours]
check('Yard stacked crates clear nearby barrels, wheelbarrows and ladder',
      min(c['separationMetres'] for c in clearances) > .05, clearances)

awning = next(f for f in fixtures if f['asset'].startswith('AwningCurved'))
frame = next(f for f in fixtures if f['asset'].startswith('DoorFrame') and abs(f['position'][0]+4.15) < .1)
frame_lo, frame_hi = bounds(placed(frame))
mount_points = placed(awning, ('Wall mounting plate', 'Connected hood brace', 'Hood brace bearing saddle'))
centre = awning['position'][0]
left = [v for v in mount_points if v.x < centre]
right = [v for v in mount_points if v.x > centre]
clearance = min(frame_lo[0]-max(v.x for v in left), min(v.x for v in right)-frame_hi[0])
check('Awning mounts clear door architraves on both sides', clearance > .03,
      dict(minimumHorizontalSeparationMetres=clearance))
check('Awning is centred on personnel opening', abs(awning['position'][0]-frame['position'][0]) < .001,
      dict(awningCentre=awning['position'][0], frameCentre=frame['position'][0]))
hood_mounts = objects(awning['asset'], ('Connected hood brace', 'Hood brace bearing saddle'))
mount_vertices = geometry(hood_mounts)[0]
# Curved hood's outside surface follows this ellipse. Any support vertex above
# it is a visible puncture; true sheet bearing/contact underneath is intentional.
above_skin = [v.z - .48*math.sqrt(max(0, 1-(v.y/.93)**2)) for v in mount_vertices if -.93 < v.y < 0]
check('Awning supports remain below outer roof skin', max(above_skin) <= .0001,
      dict(maximumAboveSkinMetres=max(above_skin)))
installed_bench = next(f for f in fixtures if f['asset'] == bench)
front_window = next(f for f in fixtures if f['asset'].startswith('WindowSteel') and f['group'] == 'FrontWall')
clearance = gap(placed(installed_bench), placed(front_window))
check('Installed bench clears projecting window sill', clearance > .05,
      dict(completeMeshBoundsSeparationMetres=clearance))

massing = json.loads((ROOT / 'ArtExports/StudioServices/Massing/massing_manifest.json').read_text())
check('Approved semantic anchors unchanged', manifest['anchors'] == massing['anchors'], len(manifest['anchors']))
report = dict(checks=checks, passed=sum(c['passed'] for c in checks), total=len(checks))
(OUT / 'source-clearances.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('COLLISION_SOURCE_CHECKS', json.dumps(report))
assert all(c['passed'] for c in checks), 'See source-clearances.json for failed geometry checks'
