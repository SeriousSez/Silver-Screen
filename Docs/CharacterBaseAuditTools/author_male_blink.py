"""Reproduce the bounded Male corner experiment and measure first-hit coverage.

This produces evaluated comparison arrays only, not a runtime BlendShape.
Controls and corner windows were independently investigated on Male.
"""
import argparse
import json
from pathlib import Path
import sys
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def visibility(points, neutral, faces, eyes, settings, step=.0001):
    eye_polys={i:s for s,ids in eyes.items() for i,f in enumerate(faces) if min(f)>=ids[0] and max(f)<=ids[-1]}
    bvh=BVHTree.FromPolygons(points.tolist(),faces);result={}
    for setting in settings:
        origin=Vector(setting['position']);direction=(Vector(setting['target'])-origin).normalized()
        q=direction.to_track_quat('-Z','Y');right=q@Vector((1,0,0));up=q@Vector((0,1,0))
        p=neutral[np.concatenate(list(eyes.values()))]-np.array(origin)
        x=p@np.array(right);y=p@np.array(up)
        xs=np.arange(x.min()-.002,x.max()+.002,step);ys=np.arange(y.min()-.002,y.max()+.002,step)
        columns={s:{} for s in eyes};counts={s:0 for s in eyes}
        for xi,xx in enumerate(xs):
            for yi,yy in enumerate(ys):
                _,_,poly,_=bvh.ray_cast(origin+right*float(xx)+up*float(yy),direction,2)
                if poly in eye_polys:
                    s=eye_polys[poly];counts[s]+=1;columns[s].setdefault(xi,[]).append(yi)
        result[setting['view']]={s:dict(samples=counts[s],area_mm2=counts[s]*(step*1000)**2,
            max_span_mm=max(((max(v)-min(v)+1)*step*1000 for v in columns[s].values()),default=0)) for s in eyes}
    return result


def main():
    p=argparse.ArgumentParser();p.add_argument('--evidence',type=Path,required=True);p.add_argument('--inspection',type=Path,required=True)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.evidence
    n=np.load(root/'neutral.npz');neutral=n['points'];endpoint=np.load(root/'u100_adj10.npz')['points']
    faces=json.loads((root/'topology.json').read_text())
    hair_set=set(n['hair'].tolist())
    visible_faces=[f for f in faces if not any(i in hair_set for i in f)]
    controls=json.loads((args.inspection/'inspection.json').read_text())['rigs']['Armature_Character1']['controls']
    control_names=['lid.T.L','lid.T.R','lid.B.L','lid.B.R']
    centers=[v['head'] for v in controls if v['name'] in control_names]
    adjacency=[set() for _ in neutral]
    for face in faces:
        for a,b in zip(face,face[1:]+face[:1]):adjacency[a].add(b);adjacency[b].add(a)
    dist=np.min(np.linalg.norm(neutral[:,None,:]-np.array(centers)[None,:,:],axis=2),axis=1)
    weight=np.clip(1-dist/.009,0,1)**2
    weight[np.setdiff1d(np.arange(len(neutral)),n['scalp_body'])]=0
    active=np.where(weight>0)[0]
    settings=json.loads((root/'render-settings.json').read_text());eyes={s:n['eye_'+s] for s in ['L','R']}
    report=dict(source_candidate='u100_adj10',corner_control_centers=control_names,radius_mm=9,
                strength=.55,cap_mm=1.5,active_vertices=active.tolist(),step_mm=.1,candidates={})
    variants={'neutral':neutral,'source':endpoint}
    for iterations in [2,5,10]:
        v=endpoint.copy()
        for _ in range(iterations):
            delta=np.array([v[list(adjacency[i])].mean(0)-v[i] for i in active])
            v[active]+=delta*(weight[active]*.55)[:,None]
            d=v-endpoint;length=np.linalg.norm(d,axis=1);over=length>.0015
            v[over]=endpoint[over]+d[over]/length[over,None]*.0015
        name='cornerRelax'+str(iterations);variants[name]=v
        np.savez_compressed(root/(name+'.npz'),points=v)
    for name,points in variants.items():
        delta=np.linalg.norm(points-endpoint,axis=1)*1000
        contact={}
        for side,ids in eyes.items():
            eye=BVHTree.FromPolygons(points[ids].tolist(),[[i-int(ids[0]) for i in f] for f in faces if min(f)>=ids[0] and max(f)<=ids[-1]])
            lid=n['lid'];box=neutral[ids];lid=lid[np.all((neutral[lid]>=box.min(0)-.004)&(neutral[lid]<=box.max(0)+.004),axis=1)]
            signed=[]
            for i in lid:
                hit,normal,_,_=eye.find_nearest(Vector(points[i]));signed.append((Vector(points[i])-hit).dot(normal)*1000)
            contact[side]=dict(lid_vertices=len(lid),minimum_signed_plane_mm=min(signed),inside_over_1mm=sum(x< -1 for x in signed),
                               near_within_02mm=sum(abs(x)<=.2 for x in signed))
        report['candidates'][name]=dict(visibility=visibility(points,neutral,visible_faces,eyes,settings),contact=contact,
            corrective_max_mm=float(delta.max()) if name!='neutral' else None,
            corrected_vertices=int(sum(delta>1e-5)) if name!='neutral' else None)
    chosen=variants['cornerRelax10'];delta=chosen-endpoint
    assert np.isfinite(chosen).all()
    assert np.linalg.norm(delta,axis=1).max()<.001501
    assert np.array_equal(chosen[np.setdiff1d(np.arange(len(chosen)),active)],endpoint[np.setdiff1d(np.arange(len(chosen)),active)])
    report['outside_corner_windows_exact']=True
    # Weight memberships overlap. Measure unrelated movement spatially as well.
    eye_window=np.zeros(len(neutral),dtype=bool)
    for ids in eyes.values():
        bounds=neutral[ids];eye_window|=np.all((neutral>=bounds.min(0)-.012)&(neutral<=bounds.max(0)+.012),axis=1)
    body=n['scalp_body'];outside=body[~eye_window[body]]
    report['selected_outside_eye_windows_max_mm']=float(np.linalg.norm(chosen[outside]-neutral[outside],axis=1).max()*1000)
    report['neutral_is_unchanged_input']=True
    (root/'corner-validation.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k not in ['active_vertices','candidates']},indent=2))
    for name,data in report['candidates'].items():print(name,json.dumps(data))


if __name__=='__main__':main()
