"""Separate visible eye-window parity from broad source lid-weight membership.

Ray hits identify actual eye geometry through the lids; dark shading alone is
not accepted as evidence of a hole. Uses matched front/oblique render cameras.
"""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser = argparse.ArgumentParser()
parser.add_argument('--evidence', type=Path, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = args.evidence
source = np.load(root / 'source.npz')
candidate = np.load(root / 'candidate.npz')
faces = json.loads((root / 'topology.json').read_text())
eyes = {side: np.arange(start, start + 354) for side, start in [('R', 15789), ('L', 16143)]}
report = {'region_definition': 'Source lid-weight vertices within each neutral eyeball world bounding box expanded 4 mm on every axis.',
          'surface': {}, 'eye_visibility': {}}
def metrics(values):
    return {'vertices': len(values), 'max_mm': float(values.max()*1000), 'mean_mm': float(values.mean()*1000),
            **{f'over_{mm:g}mm': int(sum(values > mm/1000)) for mm in (.1, .5, 1, 2)}}
for side, ids in eyes.items():
    points = source['neutral'][ids]
    low, high = points.min(0)-.004, points.max(0)+.004
    lids = source['lids_'+side]
    lids = lids[np.all((source['neutral'][lids] >= low) & (source['neutral'][lids] <= high), axis=1)]
    errors = np.linalg.norm(source['blink'][lids]-candidate['blink'][lids], axis=1)
    report['surface'][side] = metrics(errors)
    report['surface'][side]['indices'] = lids.tolist()
    report['surface'][side]['bounds_min'] = low.tolist()
    report['surface'][side]['bounds_max'] = high.tolist()

eye_polys = {side: {i for i, p in enumerate(faces) if min(p) >= ids[0] and max(p) <= ids[-1]}
             for side, ids in eyes.items()}
poses = {'source-neutral': source['neutral'], 'source-BlinkBoth': source['blink'],
         'candidate-neutral': candidate['baseline'], 'candidate-BlinkBoth': candidate['blink']}
settings = json.loads((root / 'render-settings.json').read_text())
for setting in settings:
    view = setting['view']
    origin = Vector(setting['position'])
    direction = (Vector(setting['target'])-origin).normalized()
    rotation = direction.to_track_quat('-Z', 'Y')
    right, up = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0))
    # Sample a fixed 0.2 mm grid in camera space covering both neutral eyeballs.
    pts = source['neutral'][np.concatenate(list(eyes.values()))]
    x = np.array([Vector(p-origin).dot(right) for p in pts])
    y = np.array([Vector(p-origin).dot(up) for p in pts])
    xs = np.arange(x.min()-.003, x.max()+.003, .0002)
    ys = np.arange(y.min()-.003, y.max()+.003, .0002)
    for name, points in poses.items():
        bvh = BVHTree.FromPolygons(points.tolist(), faces)
        hits = {side: [] for side in eyes}
        for yi, yy in enumerate(ys):
            for xi, xx in enumerate(xs):
                point, normal, polygon, distance = bvh.ray_cast(origin+right*float(xx)+up*float(yy), direction, 2)
                for side, polys in eye_polys.items():
                    if polygon in polys:
                        hits[side].append((xi, yi))
        report['eye_visibility'][view+'-'+name] = {side: {'visible_samples': len(values),
            'projected_area_mm2': len(values)*.04,
            'max_vertical_span_mm': max((sum(xi==column for xi, yi in values)*.2 for column in {xi for xi, yi in values}), default=0)}
            for side, values in hits.items()}
        np.savez_compressed(root/(view+'-'+name+'-eye-hits.npz'), xs=xs, ys=ys, **{s:np.array(v) for s,v in hits.items()})
(root / 'blink-contact.json').write_text(json.dumps(report, indent=2))
print(json.dumps({**report, 'surface': {k:{a:b for a,b in v.items() if a!='indices'} for k,v in report['surface'].items()}}, indent=2))
