"""Reproducible, deliberately authored lid-ring targets; NOT source-rig output.

World-space mm control curves define this experimental sculpt. No Basis edits.
This tool only writes new arrays, a recipe and an exact vertex-displacement ledger.
Acceptance must be assessed separately; a generated file is not an accepted blink.
"""
import argparse
import json
from pathlib import Path

import numpy as np


RINGS = {
    'rim': [83,85,87,90,88,70,68,56,67,65,63,61,59,55,53,51,49,47,45,1143,73,1134,75,1125,77,79,81],
    'outer': [82,84,86,91,89,71,69,57,66,64,62,60,58,54,52,50,48,46,44,1142,72,1133,74,1124,76,78,80],
    'middle': [1050,1051,1052,1054,1053,1044,1043,1037,1042,1041,1040,1039,1038,1036,1035,1034,1033,1032,1031,1149,1045,1140,1046,1131,1047,1048,1049],
    'inner': [2247,2248,2249,2251,2250,2241,2240,2234,2239,2238,2237,2236,2235,2233,2232,2231,2230,2229,2228,2254,2242,2253,2243,2252,2244,2245,2246],
}


def curve(x, anchors):
    """C1 cubic Hermite interpolation with monotonicity-limited slopes."""
    a = np.array(anchors, dtype=float)
    h = np.diff(a[:,0]); d = np.diff(a[:,1])/h
    m = np.zeros(len(a)); m[0],m[-1] = d[0],d[-1]
    for i in range(1,len(a)-1):
        if d[i-1]*d[i]>0:
            w1,w2 = 2*h[i]+h[i-1],h[i]+2*h[i-1]
            m[i] = (w1+w2)/(w1/d[i-1]+w2/d[i])
    j = np.clip(np.searchsorted(a[:,0],x)-1,0,len(a)-2)
    t = np.clip((x-a[j,0])/h[j],0,1)
    return ((2*t**3-3*t**2+1)*a[j,1]+(t**3-2*t**2+t)*h[j]*m[j]
            +(-2*t**3+3*t**2)*a[j+1,1]+(t**3-t**2)*h[j]*m[j+1])


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--evidence',type=Path,required=True)
    p.add_argument('--recipe',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    args = p.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    recipe = json.loads(args.recipe.read_text())
    neutral = np.load(args.evidence/'neutral.npz')['points'].astype(float)*1000
    raw = np.load(args.evidence/'uncorrected.npz')['points']
    baseline = raw.astype(float)*1000
    faces = json.loads((args.evidence/'topology.json').read_text())
    neighbors = [set() for _ in baseline]
    for f in faces:
        for a,b in zip(f,f[1:]+f[:1]):
            neighbors[a].add(b); neighbors[b].add(a)
    delta = np.zeros_like(baseline)
    fixed = set()
    rim = neutral[RINGS['rim']]
    for ring, ids in RINGS.items():
        for j,i in enumerate(ids):
            upper = j<=6 or j>=19
            x = rim[j,0]
            column_x = recipe.get('column_x_mm',[])
            # Corner vertices remain rounded; the same target curve is used on both sides.
            taper = min(1, max(0,(x-17.38)/3), max(0,(63.0-x)/3))
            z_offset,y_offset = recipe['ring_offsets_mm'][ring]['upper' if upper else 'lower']
            cx = neutral[i,0] if recipe.get('contact_slope') else x
            if column_x:
                cx = column_x[j]
            target = np.array([neutral[i,0],curve(cx,recipe['seam_y_mm']),curve(cx,recipe['seam_z_mm'])])
            if column_x:
                target[0] = cx + (neutral[i,0]-x)*(1-taper)
            target[1] += y_offset*taper
            target[2] += z_offset*taper
            target[2] += recipe.get('contact_slope',0)*y_offset*taper
            # At either canthus keep the ring's original radial ordering with a reduced volume.
            target += (neutral[i]-rim[j])*np.array([0,.35,.25])*(1-taper)
            weight = 1
            if recipe.get('preserve_canthus'):
                weight = min(1,max(0,(x-17.4)/4),max(0,(63.1-x)/2))
                weight = weight*weight*(3-2*weight)
            delta[i] = (target-baseline[i])*weight
            fixed.add(i)
    for i,offset in recipe.get('early_offsets_mm',{}).items():
        assert int(i) in fixed
        delta[int(i)] += np.array(offset)
    for i,target in recipe.get('target_vertices_mm',{}).items():
        assert int(i) in fixed
        delta[int(i)] = np.array(target)-baseline[int(i)]
    # Harmonic displacement falloff across two adjacent exterior rings. Exact zero outside.
    frontier = set(RINGS['outer']); allowed = set(fixed)
    for _ in range(recipe['falloff_rings']):
        frontier = {j for i in frontier for j in neighbors[i] if j not in allowed
                    and 14<neutral[j,0]<71 and 1643<neutral[j,2]<1696 and neutral[j,1]<0}
        allowed |= frontier
    free = sorted(allowed-fixed)
    for _ in range(80):
        previous = delta.copy()
        for i in free:
            delta[i] = np.mean(previous[list(neighbors[i])],axis=0)
    for i,offset in recipe.get('post_offsets_mm',{}).items():
        i = int(i)
        assert i in allowed
        delta[i] += np.array(offset)
    for step in recipe.get('ring_relaxation',[]):
        posed = baseline+delta
        for ring in step['rings']:
            ids = RINGS[ring]
            for j in step['columns']:
                i = ids[j]
                target = (posed[ids[(j-1)%27]]+posed[ids[(j+1)%27]])*.5
                delta[i] += step['strength']*(target-posed[i])
    for step in recipe.get('surface_relaxation',[]):
        movable = sorted(set(step['vertices']))
        assert set(movable)<=allowed
        for _ in range(step['iterations']):
            posed = baseline+delta
            for i in movable:
                target = np.mean(posed[list(neighbors[i])],axis=0)
                delta[i] += step['strength']*(target-posed[i])
    # Same authored absolute targets for the mirrored ring/neighborhood. No eyeball motion.
    right = np.flatnonzero((neutral[:,0]<0)&(np.arange(len(neutral))<12937))
    pairs = []
    for i in sorted(allowed):
        mirror = neutral[i]*[-1,1,1]
        k = right[np.argmin(np.linalg.norm(neutral[right]-mirror,axis=1))]
        assert np.linalg.norm(neutral[k]-mirror)<.001
        delta[k] = (baseline[i]+delta[i])*[-1,1,1]-baseline[k]
        pairs.append([i,int(k)])
    delta *= recipe.get('strength',1.0)
    assert not np.any(delta[12937:])
    moved = np.flatnonzero(np.linalg.norm(delta,axis=1)>1e-9)
    report = dict(recipe=recipe,method='Authored seam and ring volumes, harmonic displacement falloff; mirrored absolute targets.',
                  manual_unrecorded_steps=False,rings_left=RINGS,mirrored_pairs=pairs,
                  modified_vertices=len(moved),maximum_displacement_mm=float(np.linalg.norm(delta,axis=1).max()),
                  rms_modified_mm=float(np.sqrt(np.mean(np.sum(delta[moved]**2,axis=1)))),
                  vertex_displacements_mm={str(i):delta[i].tolist() for i in moved})
    np.savez_compressed(args.output/'correction.npz',baseline=raw,delta=delta/1000)
    np.savez_compressed(args.output/'corrected.npz',points=raw+delta/1000)
    (args.output/'authoring.json').write_text(json.dumps(report,indent=2))
    for name in ['neutral.npz','uncorrected.npz','topology.json','render-settings.json']:
        (args.output/name).write_bytes((args.evidence/name).read_bytes())
    print(json.dumps({k:report[k] for k in ['modified_vertices','maximum_displacement_mm','rms_modified_mm']}))


if __name__ == '__main__':
    main()
