"""Reuse the established first-hit BVH visibility metric on generated geometry."""
import argparse
import json
from pathlib import Path
import sys
import numpy as np
from mathutils.bvhtree import BVHTree

sys.dont_write_bytecode = True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from calibrate_female_blink import visibility_grid, measure_visibility

p = argparse.ArgumentParser()
p.add_argument('--evidence',type=Path,required=True)
p.add_argument('--names',nargs='+',required=True)
p.add_argument('--step-mm',type=float,default=.2)
args = p.parse_args(sys.argv[sys.argv.index('--')+1:])
root = args.evidence
neutral = np.load(root/'neutral.npz')['points']
faces = json.loads((root/'topology.json').read_text())
settings = json.loads((root/'render-settings.json').read_text())
_,eye_polys,grids = visibility_grid(neutral,faces,settings,args.step_mm/1000)
report = {'step_mm':args.step_mm,'candidates':{}}
for name in args.names:
    points = np.load(root/(name+'.npz'))['points']
    assert np.isfinite(points).all()
    report['candidates'][name] = measure_visibility(points,faces,eye_polys,grids,args.step_mm/1000,root/name)
    bvh = BVHTree.FromPolygons(points.tolist(),faces)
    for view,(direction,xs,ys,rays) in grids.items():
        hits = np.load(str(root/name)+'-'+view+'-hits.npz')
        for side in ['L','R']:
            if 0<len(hits[side])<500:
                xyz = []
                for xi,yi in hits[side]:
                    origin = rays[int(yi)*len(xs)+int(xi)][2]
                    xyz.append(list(bvh.ray_cast(origin,direction,2)[0]))
                report['candidates'][name][view][side]['residual_bounds_mm'] = (np.array([np.min(xyz,axis=0),np.max(xyz,axis=0)])*1000).tolist()
    print(name,json.dumps(report['candidates'][name]),flush=True)
(root/('visibility-'+str(args.step_mm)+'mm.json')).write_text(json.dumps(report,indent=2))
