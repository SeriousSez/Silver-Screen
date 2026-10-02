"""Expanded diagnostics supplement, never replace, the established 600-triangle gate.

Checks every triangle incident to an authored vertex, including partially hidden
socket/canthus faces. Tests pairs sharing one vertex too. Classifies exterior via
seven surface probes from the matched cameras. This is not art approval.
"""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri

p = argparse.ArgumentParser()
p.add_argument('--evidence',type=Path,required=True)
p.add_argument('--names',nargs='+',required=True)
p.add_argument('--authoring',type=Path,required=True)
args = p.parse_args(sys.argv[sys.argv.index('--')+1:])
root = args.evidence
neutral = np.load(root/'neutral.npz')['points'].astype(float)
regions = np.load(root/'surface-regions.npz')
tris = regions['triangles']
authored = json.loads(args.authoring.read_text())
modified = set(map(int,authored['vertex_displacements_mm']))
local = np.array([j for j,t in enumerate(tris) if set(t)&modified])
prior = set(map(int,regions['visible_lid_triangles']))
settings = json.loads((root/'render-settings.json').read_text())
dirs = [(Vector(s['target'])-Vector(s['position'])).normalized() for s in settings]
weights = np.array([[1/3]*3,[.8,.1,.1],[.1,.8,.1],[.1,.1,.8],
                    [.45,.45,.1],[.1,.45,.45],[.45,.1,.45]])
area = lambda pts: np.cross(pts[tris[local,1]]-pts[tris[local,0]],pts[tris[local,2]]-pts[tris[local,0]])
a0 = area(neutral); size0 = np.linalg.norm(a0,axis=1)
report = {'local_triangles':len(local),'modified_vertices':len(modified),
          'probe_count_per_triangle':len(weights),'names':{},
          'note':'Signed plane penetration is diagnostic. Hidden socket intersections and source motion are reported separately. No automatic visual acceptance.'}
for name in args.names:
    pts = np.load(root/(name+'.npz'))['points'].astype(float)
    all_bvh = BVHTree.FromPolygons(pts.tolist(),tris.tolist(),all_triangles=True)
    exterior = set(prior)
    for j in local:
        for point in weights@pts[tris[j]]:
            for d in dirs:
                hit,_,face,_ = all_bvh.ray_cast(Vector(point)-d,d,2)
                if face==j or (hit is not None and (hit-Vector(point)).length<.000002):
                    exterior.add(int(j)); break
            if int(j) in exterior:
                break
    skin = BVHTree.FromPolygons(pts.tolist(),tris[local].tolist(),all_triangles=True)
    crossings = []
    for a,b in skin.overlap(skin):
        if a>=b: continue
        ia,ib = int(local[a]),int(local[b])
        shared = set(tris[ia])&set(tris[ib])
        if len(shared)>=2: continue
        ta,tb = [tuple(Vector(v) for v in pts[tris[i]]) for i in [ia,ib]]
        hits = []
        for first,second in [(ta,tb),(tb,ta)]:
            for i,j in [(0,1),(1,2),(2,0)]:
                edge=second[j]-second[i]
                if edge.length<1e-12: continue
                d=edge.normalized()
                hit=intersect_ray_tri(*first,d,second[i],True)
                if hit is None or not .000001 < (hit-second[i]).dot(d) < edge.length-.000001: continue
                if any((hit-Vector(pts[v])).length<.000002 for v in shared): continue
                if all((hit-old).length>.000001 for old in hits): hits.append(hit)
        if len(hits)>=2:
            visible_hits = 0
            for hit in hits:
                for d in dirs:
                    first,_,_,_=all_bvh.ray_cast(hit-d,d,2)
                    if first is not None and (first-hit).length<.000002:
                        visible_hits += 1; break
            crossings.append({'triangles':[ia,ib],'vertices':[tris[ia].tolist(),tris[ib].tolist()],
                              'shared_vertices':list(map(int,shared)),
                              'touches_exterior_triangle':ia in exterior or ib in exterior,
                              'visible_intersection_samples':visible_hits,'points_m':[list(v) for v in hits]})
    cross=area(pts); sizes=np.linalg.norm(cross,axis=1)
    ratios=sizes/size0
    angles=np.degrees(np.arccos(np.clip(np.sum(cross*a0,axis=1)/np.maximum(sizes*size0,1e-30),-1,1)))
    # Area/normal checks on all locally touched faces, with the exterior subset explicit.
    ext=np.array([int(j) in exterior for j in local])
    stats={}
    for label,mask in [('all_local',np.ones(len(local),bool)),('exterior',ext)]:
        stats[label]={'triangles':int(sum(mask)),'min_area_ratio':float(ratios[mask].min()),
                      'area_below_0_1':int(sum(ratios[mask]<.1)),
                      'normal_rotation_max_degrees':float(angles[mask].max()),
                      'normal_rotation_over_90':int(sum(angles[mask]>90)),
                      'normal_rotation_over_120':int(sum(angles[mask]>120))}
    signed=[]
    for side,start,end in [('L',16143,16497),('R',15789,16143)]:
        et=tris[(tris.min(1)>=start)&(tris.max(1)<end)]-start
        eye=BVHTree.FromPolygons(pts[start:end].tolist(),et.tolist(),all_triangles=True)
        samples=[]; visible_samples=[]
        for j in local:
            if j not in exterior or (pts[tris[j],0].mean()>0)!=(side=='L'): continue
            for point in weights@pts[tris[j]]:
                hit,normal,_,_=eye.find_nearest(Vector(point))
                distance = float((Vector(point)-hit).dot(normal))*1000
                samples.append(distance)
                for d in dirs:
                    first,_,_,_=all_bvh.ray_cast(Vector(point)-d,d,2)
                    if first is not None and (first-Vector(point)).length<.000002:
                        visible_samples.append(distance); break
        signed.append({'eye':side,'samples':len(samples),'min_signed_nearest_plane_mm':min(samples),
                       'inside_over_0_1mm':sum(s<-.1 for s in samples),
                       'inside_over_1mm':sum(s<-1 for s in samples),
                       'visible_samples_only':{'samples':len(visible_samples),
                                               'minimum_mm':min(visible_samples),
                                               'inside_over_0_1mm':sum(s<-.1 for s in visible_samples),
                                               'inside_over_1mm':sum(s<-1 for s in visible_samples)}})
    entry={'finite':bool(np.isfinite(pts).all()),'surfaces':stats,'crossings':crossings,
           'pairs_touching_exterior_triangles':sum(h['touches_exterior_triangle'] for h in crossings),
           'pairs_with_visible_intersection_samples':sum(h['visible_intersection_samples']>0 for h in crossings),
           'pairs_between_hidden_triangles':sum(not h['touches_exterior_triangle'] for h in crossings),'sampled_eye_contact':signed}
    report['names'][name]=entry
    print(name,json.dumps({k:v for k,v in entry.items() if k!='crossings'}),flush=True)
(root/'expanded-geometry.json').write_text(json.dumps(report,indent=2))
