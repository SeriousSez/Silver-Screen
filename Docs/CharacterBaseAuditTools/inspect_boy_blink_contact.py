"""Boy evaluated-surface contact diagnostics; no authoring or runtime bake.

Signed nearest-eye tangent-plane distance is a local penetration proxy.
BVH overlap counts include internal contacts and are not an exterior art verdict.
"""
import argparse,json,sys
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser();p.add_argument('--evidence',type=Path,required=True);p.add_argument('--names',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);root=a.evidence
archive=np.load(root/'neutral.npz');neutral=archive['points'];faces=json.loads((root/'topology.json').read_text())
body=set(archive['body'].tolist());lid=set(archive['lid'].tolist())
eyeids={s:archive['eye_'+s] for s in ['L','R']};report={}
lidfaces=[f for f in faces if all(v in body for v in f) and any(v in lid for v in f)]
hair=set(archive['hair'].tolist());visible_faces=[f for f in faces if not any(v in hair for v in f)]
settings=json.loads((root/'render-settings.json').read_text())
for name in a.names.split(','):
    points=np.load(root/(name+'.npz'))['points'];delta=np.linalg.norm(points-neutral,axis=1)*1000
    contact={};surface=BVHTree.FromPolygons(points.tolist(),visible_faces)
    for side,ids in eyeids.items():
        members=set(ids.tolist());eye_faces=[f for f in faces if all(v in members for v in f)]
        eye=BVHTree.FromPolygons(points.tolist(),eye_faces)
        same_side=[i for i in lid if (neutral[i,0]>0)==(side=='L')]
        signed=[];visible_signed={s['view']:[] for s in settings}
        for i in same_side:
            hit,normal,_,_=eye.find_nearest(Vector(points[i]));distance=(Vector(points[i])-hit).dot(normal)*1000;signed.append(distance)
            for setting in settings:
                origin=Vector(setting['position']);direction=(Vector(setting['target'])-origin).normalized()
                start=Vector(points[i])-direction*.6
                hit,_,_,_=surface.ray_cast(start,direction,1)
                # Vertex rays are only a supporting point sample, not full surface collision proof.
                if hit is not None and (hit-Vector(points[i])).length<.00005:visible_signed[setting['view']].append(distance)
        contact[side]=dict(vertices=len(same_side),minimum_signed_plane_mm=float(min(signed)),inside_over_1mm=int(sum(v< -1 for v in signed)),within_02mm=int(sum(abs(v)<=.2 for v in signed)),
            first_hit_visible_lid_vertex_samples={view:dict(count=len(values),minimum_signed_plane_mm=float(min(values)) if values else None,inside_over_02mm=int(sum(v< -.2 for v in values))) for view,values in visible_signed.items()})
    bvh=BVHTree.FromPolygons(points.tolist(),lidfaces)
    overlaps=[(i,j) for i,j in bvh.overlap(bvh) if i<j and not set(lidfaces[i]).intersection(lidfaces[j])]
    # Boy's own measured control gap defines a spatial context, not copied adult windows.
    gap=.029061198234558;window=np.zeros(len(points),dtype=bool)
    for ids in eyeids.values():window |= np.all((neutral>=neutral[ids].min(0)-gap/2)&(neutral<=neutral[ids].max(0)+gap/2),axis=1)
    bodyids=archive['body'];outside=bodyids[~window[bodyids]]
    report[name]=dict(contact=contact,nonadjacent_orbital_polygon_overlap_pairs=len(overlaps),overlap_pairs=overlaps,
        max_displacement_mm=float(delta.max()),changed_over_001mm=int(sum(delta>.001)),
        outside_eye_window_max_mm=float(delta[outside].max()),outside_lid_weights_max_mm=float(delta[archive['outside_lid_weights']].max()),
        eyes_exact=all(np.array_equal(points[ids],neutral[ids]) for ids in eyeids.values()),finite=bool(np.isfinite(points).all()))
(root/'contact.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:{a:b for a,b in v.items() if a!='overlap_pairs'} for k,v in report.items()},indent=2))
