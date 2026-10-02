"""Geometric surface diagnostics on saved evaluated source arrays, no rig edits."""
import argparse
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from calibrate_female_blink import visibility_grid

parser = argparse.ArgumentParser()
parser.add_argument('--evidence', type=Path, required=True)
parser.add_argument('--names', nargs='+', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root = args.evidence
base = np.load(root/'neutral.npz')
neutral = base['points']
faces = json.loads((root/'topology.json').read_text())
settings = json.loads((root/'render-settings.json').read_text())
eyes, _, grids = visibility_grid(neutral, faces, settings, .0002)
bvh = BVHTree.FromPolygons(neutral.tolist(), faces)
visible_faces = set()
for direction, _, _, rays in grids.values():
    for _, _, origin in rays:
        _, _, face, _ = bvh.ray_cast(origin, direction, 2)
        if face is not None and max(faces[face]) < 12937:
            visible_faces.add(face)
visible_vertices = set()
for i in set(base['lids_L']) | set(base['lids_R']):
    point = Vector(neutral[i])
    for setting in settings:
        origin = Vector(setting['position'])
        direction = (Vector(setting['target'])-origin).normalized()
        ray_origin = point-direction*((point-origin).dot(direction))
        hit, _, _, _ = bvh.ray_cast(ray_origin, direction, 2)
        if hit is not None and (hit-point).length < .00005:
            visible_vertices.add(i)
            break
outer = {s: np.array(sorted(visible_vertices & set(base['lids_'+s])), dtype=int) for s in eyes}
mesh = bpy.data.meshes.new('Diagnostic triangulation')
mesh.from_pydata(neutral.tolist(), [], faces)
mesh.calc_loop_triangles()
tris = np.array([list(t.vertices) for t in mesh.loop_triangles])
owners = np.array([t.polygon_index for t in mesh.loop_triangles])
lid_set = set(base['lids_L']) | set(base['lids_R'])
lid_tris = np.array([i for i,t in enumerate(tris) if owners[i] in visible_faces and all(v in visible_vertices for v in t)])
def cross(points):
    t = points[tris[lid_tris]]
    return np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0])
n0 = cross(neutral)
area0 = np.linalg.norm(n0, axis=1)
n0 /= area0[:,None]
np.savez_compressed(root/'surface-regions.npz', triangles=tris, visible_neutral_polygons=np.array(sorted(visible_faces)),
                    visible_lid_triangles=lid_tris, **{'outer_'+s:ids for s,ids in outer.items()})
# Spatial areas avoid mislabelling eyelid vertices that also carry tiny nose/cheek weights.
x,y,z = neutral[:12937].T
regions = {
    'nose_midline': np.flatnonzero((abs(x)<.012)&(y<-.035)&(z>1.57)&(z<1.7)),
    'cheek_below_eye': np.flatnonzero((abs(x)>.020)&(abs(x)<.08)&(y<.005)&(z>1.60)&(z<1.64)),
    'brow_above_eye': np.flatnonzero((abs(x)>.02)&(abs(x)<.07)&(y<.01)&(z>1.70)&(z<1.73))}
# Keep raw spatial sets, but also separate visible facial skin from socket lining.
for region,ids in list(regions.items()):
    visible=[]
    for i in ids:
        point=Vector(neutral[i])
        for setting in settings:
            origin=Vector(setting['position'])
            direction=(Vector(setting['target'])-origin).normalized()
            hit,_,_,_=bvh.ray_cast(point-direction*((point-origin).dot(direction)),direction,2)
            if hit is not None and (hit-point).length < .00005:
                visible.append(i)
                break
    if visible:
        regions[region+'_visible_skin']=np.array(visible,dtype=int)
report = {'note': 'Nearest-plane signed distances and normal rotations are diagnostics, not proof of collision or inversion. Visible-neutral lid vertices exclude hidden inner socket points.',
          'regions': {k:v.tolist() for k,v in regions.items()}, 'candidates': {}}
left,right = base['lids_L'],base['lids_R']
mirrored = neutral[left]*np.array([-1,1,1])
pairing = right[np.argmin(np.linalg.norm(mirrored[:,None,:]-neutral[right][None,:,:],axis=2),axis=1)]
assert len(set(pairing)) == len(left)
for name in args.names:
    points = np.load(root/(name+'.npz'))['points']
    entry = {'outer_lids': {}, 'spatial_movement': {}}
    for side, ids in eyes.items():
        polys = [[v-int(ids[0]) for v in p] for p in faces if min(p)>=ids[0] and max(p)<=ids[-1]]
        eye = BVHTree.FromPolygons(points[ids].tolist(), polys)
        distances = []
        for i in outer[side]:
            p,n,_,_ = eye.find_nearest(Vector(points[i]))
            distances.append(float((Vector(points[i])-p).dot(n))*1000)
        entry['outer_lids'][side] = {'vertices':len(distances), 'minimum_signed_nearest_plane_mm': min(distances),
                                    'inside_over_0_1mm':sum(v<-.1 for v in distances),
                                    'inside_over_1mm':sum(v<-1 for v in distances),
                                    'vertex_signed_mm': dict(zip(map(str,outer[side]),distances))}
    for region,ids in regions.items():
        d = np.linalg.norm(points[ids]-neutral[ids],axis=1)*1000
        entry['spatial_movement'][region] = {'vertices':len(ids), 'max_mm':float(d.max()), 'mean_mm':float(d.mean())}
    normals = cross(points)
    areas = np.linalg.norm(normals, axis=1)
    angles = np.degrees(np.arccos(np.clip(np.sum(normals*n0,axis=1)/np.maximum(areas,1e-20),-1,1)))
    entry['visible_lid_surface'] = {'triangles':len(angles), 'normal_rotation_max_degrees':float(angles.max()),
                                   'normal_rotation_over_90':int(sum(angles>90)), 'normal_rotation_over_120':int(sum(angles>120)),
                                   'min_area_ratio_to_neutral':float((areas/area0).min()),
                                   'triangles_area_below_0_1_neutral':int(sum(areas/area0<.1))}
    # Nonadjacent exterior-lid triangles must not cross through one another.
    # BVH supplies pairs; finite segment/triangle intersections confirm them.
    skin = BVHTree.FromPolygons(points.tolist(), tris[lid_tris].tolist(), all_triangles=True)
    crossings = []
    for a,b in skin.overlap(skin):
        if a>=b or set(tris[lid_tris[a]]) & set(tris[lid_tris[b]]):
            continue
        ta,tb = [tuple(Vector(p) for p in points[tris[lid_tris[i]]]) for i in (a,b)]
        hits = []
        for first,second in [(ta,tb),(tb,ta)]:
            for i,j in [(0,1),(1,2),(2,0)]:
                edge=second[j]-second[i]
                hit=intersect_ray_tri(*first,edge.normalized(),second[i],True)
                if hit is not None and .000001 < (hit-second[i]).dot(edge.normalized()) < edge.length-.000001:
                    hits.append(list(hit))
        if len(hits)>=2:
            crossings.append({'triangles':[int(lid_tris[a]),int(lid_tris[b])], 'points_m':hits})
    entry['exterior_lid_nonadjacent_triangle_crossings'] = crossings
    entry['bilateral_local_lid_mirror_error_mm'] = float(np.linalg.norm(points[left]*np.array([-1,1,1])-points[pairing],axis=1).max()*1000)
    entry['residual_eye_hits'] = {}
    full = BVHTree.FromPolygons(points.tolist(), faces)
    for setting in settings:
        hit_file=root/(name+'-'+setting['view']+'-hits.npz')
        if not hit_file.exists():
            continue
        samples=np.load(hit_file)
        # Only retain sparse residuals here; dense neutral/previous masks remain in NPZ.
        if sum(len(samples[s]) for s in eyes)>100:
            continue
        origin=Vector(setting['position'])
        direction=(Vector(setting['target'])-origin).normalized()
        rotation=direction.to_track_quat('-Z','Y')
        r,u=rotation@Vector((1,0,0)),rotation@Vector((0,1,0))
        entry['residual_eye_hits'][setting['view']]={}
        for side in eyes:
            hit_points=[]
            for xi,yi in samples[side]:
                p,_,polygon,_=full.ray_cast(origin+r*float(samples['xs'][xi])+u*float(samples['ys'][yi]),direction,2)
                hit_points.append({'point_m':list(p),'eye_polygon':polygon})
            entry['residual_eye_hits'][setting['view']][side]=hit_points
    report['candidates'][name] = entry
(root/'surface-check.json').write_text(json.dumps(report,indent=2))
print(json.dumps({n:{k:({s:{a:b for a,b in v.items() if a!='vertex_signed_mm'} for s,v in value.items()} if k=='outer_lids' else value)
                       for k,value in entry.items()} for n,entry in report['candidates'].items()},indent=2))
