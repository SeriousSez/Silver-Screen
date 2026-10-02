"""Independent Male full-rig probes and bounded blink search through depsgraph.

Candidate distances derive from this rig's own central lid-control separation.
No Female indices, endpoint values or corrective data are consumed.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from inspect_male_foundation import expose, components


def points(obj):
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    result = np.array([evaluated.matrix_world @ v.co for v in mesh.vertices])
    evaluated.to_mesh_clear()
    return result


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--candidates',type=Path)
    parser.add_argument('--refine',action='store_true',help='The documented Male 3 by 3 adjacent-control refinement')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    out=args.output;out.mkdir(parents=True,exist_ok=True)
    stamp=(args.source.stat().st_mtime_ns,hashlib.sha256(args.source.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(args.source),load_ui=False,use_scripts=False)
    expose()
    arm=bpy.data.objects['Armature_Character1']
    objects=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==arm]
    rest={b.name:b.matrix_basis.copy() for b in arm.pose.bones}
    def reset():
        for name,m in rest.items():arm.pose.bones[name].matrix_basis=m
        bpy.context.view_layer.update()
    def sample():
        bpy.context.view_layer.update()
        return np.concatenate([points(o) for o in objects])
    faces=[];offsets={};offset=0
    for o in objects:
        offsets[o.name]=offset
        faces.extend([[i+offset for i in p.vertices] for p in o.data.polygons])
        offset+=len(o.data.vertices)
    neutral=sample()
    eyes={side:np.arange(offsets[name],offsets[name]+len(bpy.data.objects[name].data.vertices))
          for side,name in [('R','Character1.002'),('L','Character1.004')]}
    eye_polys={i:side for side,ids in eyes.items() for i,f in enumerate(faces) if min(f)>=ids[0] and max(f)<=ids[-1]}
    body=bpy.data.objects['Character1'];bodyoffset=offsets[body.name]
    bodygroups=components(body.data)
    # Largest component independently measured as the closed body/scalp; three
    # other body-object islands are reviewed as hair in the matched renders.
    scalp=np.array(bodygroups[0])+bodyoffset
    hair=np.array([i for g in bodygroups[1:] for i in g])+bodyoffset
    hair_set=set(hair.tolist())
    visible_faces=[f for f in faces if not any(i in hair_set for i in f)]
    eye_polys={i:side for side,ids in eyes.items() for i,f in enumerate(visible_faces) if min(f)>=ids[0] and max(f)<=ids[-1]}
    regions={'scalp_body':scalp,'hair':hair,'eyes':np.concatenate(list(eyes.values()))}
    for region in ['lid','brow','cheek','nose','jaw','lip','forehead']:
        regions[region]=np.array([v.index+bodyoffset for v in body.data.vertices if any(
            body.vertex_groups[g.group].name.startswith('DEF-'+region) and g.weight>1e-5 for g in v.groups)],dtype=int)
    regions['outside_lid_weights']=np.setdiff1d(scalp,regions['lid'])
    center=neutral[regions['eyes']].mean(0)
    settings=[dict(view='front',position=[0,-.85,float(center[2])],target=[0,float(center[1]),float(center[2])],ortho_scale=.18),
              dict(view='oblique',position=[.43,-.8,float(center[2])+.05],target=center.tolist(),ortho_scale=.19)]
    step=.00025;grids={}
    for s in settings:
        origin=Vector(s['position']);direction=(Vector(s['target'])-origin).normalized()
        q=direction.to_track_quat('-Z','Y');right=q@Vector((1,0,0));up=q@Vector((0,1,0))
        e=neutral[regions['eyes']]-np.array(origin)
        x=e@np.array(right);y=e@np.array(up)
        xs=np.arange(x.min()-.002,x.max()+.002,step);ys=np.arange(y.min()-.002,y.max()+.002,step)
        rays=[(xi,yi,origin+right*float(xx)+up*float(yy)) for yi,yy in enumerate(ys) for xi,xx in enumerate(xs)]
        grids[s['view']]=(direction,rays)
    def visibility(p):
        bvh=BVHTree.FromPolygons(p.tolist(),visible_faces)
        result={}
        for name,(direction,rays) in grids.items():
            samples={'L':[],'R':[]}
            for x,y,origin in rays:
                _,_,poly,_=bvh.ray_cast(origin,direction,2)
                if poly in eye_polys:samples[eye_polys[poly]].append((x,y))
            result[name]={}
            for side,hits in samples.items():
                cols={}
                for x,y in hits:cols.setdefault(x,[]).append(y)
                result[name][side]=dict(samples=len(hits),area_mm2=len(hits)*(.25**2),
                    max_span_mm=max(((max(v)-min(v)+1)*.25 for v in cols.values()),default=0))
        return result
    def move(control,delta):
        b=arm.pose.bones[control]
        b.location+=b.bone.matrix_local.to_3x3().inverted()@Vector(delta)
    def movement(p):
        d=np.linalg.norm(p-neutral,axis=1)*1000
        return {n:dict(vertices=len(ids),over_01mm=int(sum(d[ids]>.1)),max_mm=float(d[ids].max()),mean_mm=float(d[ids].mean())) for n,ids in regions.items() if len(ids)}
    np.savez_compressed(out/'neutral.npz',points=neutral,**regions,**{'eye_'+s:ids for s,ids in eyes.items()})
    (out/'topology.json').write_text(json.dumps(faces))
    (out/'render-settings.json').write_text(json.dumps(settings,indent=2))
    report=dict(source=str(args.source),sha256=stamp[1],objects=offsets,step_mm=.25,visibility_surface='bald full source',candidates={},probes={})
    probes={'upper_lid_L':('lid.T.L.002',(0,0,-.003)), 'upper_lid_R':('lid.T.R.002',(0,0,-.003)),
            'lower_lid_L':('lid.B.L.002',(0,0,.002)), 'brow_L':('brow.T.L.002',(0,0,.004)),
            'gaze_L':('eye.L',(.015,0,0)), 'forehead_L':('forehead.L',(0,0,.003)),
            'jaw':('jaw_master',(0,0,-.005)), 'lip':('lip.T',(0,-.003,0)),
            'cheek_L':('cheek.T.L.001',(0,-.003,0)), 'nose':('nose.002',(0,-.003,0))}
    for name,(control,delta) in probes.items():
        reset()
        if control not in arm.pose.bones:
            report['probes'][name]=dict(control=control,status='MISSING_CONTROL');continue
        move(control,delta);p=sample()
        report['probes'][name]=dict(control=control,armature_delta_m=delta,finite=bool(np.isfinite(p).all()),movement=movement(p))
        np.savez_compressed(out/('probe-'+name+'.npz'),points=p)
    gap={s:float(arm.pose.bones[f'lid.T.{s}.002'].bone.head_local.z-arm.pose.bones[f'lid.B.{s}.002'].bone.head_local.z) for s in eyes}
    reset()
    report['central_lid_separation_mm']={s:v*1000 for s,v in gap.items()}
    candidates=[dict(name='neutral',upper=0,lower=0)]
    candidates += [dict(name=f'u{u:03d}_l{l:02d}',upper=u/100,lower=l/100) for u in [65,85,100,115,130] for l in [0,15,30]]
    assert not (args.candidates and args.refine),'Choose --refine or an explicit candidate file'
    if args.refine:
        candidates=[dict(name='neutral',upper=0,lower=0)]
        candidates += [dict(name=f'u{u}_adj{x}',upper=u/100,lower=.25,extra_upper={'.001':x/100,'.003':x/200})
                       for u in [90,100,110] for x in [5,10,15]]
    if args.candidates:candidates=json.loads(args.candidates.read_text())
    for candidate in candidates:
        reset();assert np.array_equal(sample(),neutral),'Neutral reset drift'
        controls={}
        for side in eyes:
            for part,coefficient in [('T',-candidate['upper']),('B',candidate['lower'])]:
                n=f'lid.{part}.{side}.002';delta=(0,0,gap[side]*coefficient)
                move(n,delta);controls[n]=[v*1000 for v in delta]
            for suffix,coefficient in candidate.get('extra_upper',{}).items():
                n=f'lid.T.{side}'+suffix;delta=(0,0,-gap[side]*coefficient)
                move(n,delta);controls[n]=[v*1000 for v in delta]
        p=sample();assert np.isfinite(p).all()
        name=candidate['name']
        if name!='neutral':np.savez_compressed(out/(name+'.npz'),points=p)
        report['candidates'][name]=dict(controls_mm=controls,visibility=visibility(p),movement=movement(p))
        (out/'calibration.json').write_text(json.dumps(report,indent=2))
        print(name,json.dumps(report['candidates'][name]['visibility']),flush=True)
    reset();assert np.array_equal(sample(),neutral)
    report['neutral_reset_exact']=True
    report['invalid_drivers']=[d.data_path for d in arm.animation_data.drivers if not d.driver.is_valid]
    assert stamp==(args.source.stat().st_mtime_ns,hashlib.sha256(args.source.read_bytes()).hexdigest())
    report['source_unchanged']=True
    (out/'calibration.json').write_text(json.dumps(report,indent=2))


if __name__=='__main__':main()
