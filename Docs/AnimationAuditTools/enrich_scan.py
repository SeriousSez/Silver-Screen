"""One cached supplemental pass for static FBX transforms and non-skeletal channels."""
from fbx_scan import *

def main():
    scan=json.loads((CACHE/'scan.json').read_text()); done={}
    for idx,f in enumerate(scan['files']):
        if 'fbx' not in f:continue
        sha=f['sha256']; meta=f['fbx']; ep=CACHE/(sha+'_extra.json')
        if sha in done: extra=done[sha]
        elif ep.exists():extra=json.loads(ep.read_text())
        else:
            data=(SOURCE/f['relative_path']).read_bytes()
            _,doc=parse(data,skip_animation=True)
            models=[n for n in doc.get('Objects').c if n.name=='Model']
            transforms={norm(n.p[1]):properties(n) for n in models}
            # Retain transform-affecting properties, excluding visibility/display/export metadata.
            keys=('Lcl Translation','Lcl Rotation','Lcl Scaling','PreRotation','PostRotation','RotationOrder','RotationActive','RotationPivot','RotationOffset','ScalingPivot','ScalingOffset','InheritType','GeometricTranslation','GeometricRotation','GeometricScaling')
            rest={name:{k:v for k,v in props.items() if k in keys} for name,props in transforms.items()}
            extra={'static_model_transforms':rest,'static_transform_signature':digest(json.dumps(rest,sort_keys=True).encode()),'non_skeletal_curves':None}
            if any(t['keyed_channel_count']==0 for t in meta['takes']):
                _,doc=parse(data); curves=[n for n in doc.get('Objects').c if n.name=='AnimationCurve'];defs=[n for n in doc.get('Objects').c if n.name=='Deformer']
                ts=[n.get('KeyTime').p[0] for n in curves if n.get('KeyTime').p]
                extra['non_skeletal_curves']={'curve_count':len(curves),'deformer_types':dict(collections.Counter(n.p[2] for n in defs if len(n.p)>2)),'varying_channel_count':sum(float(np.ptp(n.get('KeyValueFloat').p[0]))>1e-5 for n in curves if n.get('KeyValueFloat').p),'unique_key_time_count':len(np.unique(np.concatenate(ts))) if ts else 0,'key_start_ticks':min(int(x[0]) for x in ts) if ts else None,'key_stop_ticks':max(int(x[-1]) for x in ts) if ts else None}
            ep.write_text(json.dumps(extra),encoding='utf8')
        done[sha]=extra;meta.update(extra)
        for i,t in enumerate(meta['takes']):
            if extra['non_skeletal_curves']:
                t['non_skeletal_animation']=extra['non_skeletal_curves']
                t['content_type']='facial_blendshapes' if 'BlendShapeChannel' in extra['non_skeletal_curves']['deformer_types'] else 'unknown_non_skeletal'
                face=extra['non_skeletal_curves']
                if t['duration_seconds'] is None and face['key_start_ticks'] is not None:
                    t['start_ticks']=face['key_start_ticks'];t['stop_ticks']=face['key_stop_ticks']
                    t['duration_seconds']=(t['stop_ticks']-t['start_ticks'])/TICKS
                    t['frame_count_inclusive']=round(t['duration_seconds']*t['frame_rate'])+1 if t['frame_rate'] else None
                    t['duration_basis']='Non-skeletal curve key bounds; stack LocalStart/LocalStop absent'
            else:t['content_type']='skeletal'
            z=np.load(CACHE/(sha+'_'+str(i)+'.npz')); roots={}
            for label,values in zip(z['labels'],z['values']):
                if label[1]=='Lcl Translation' and re.search(r'(hips|pelvis|reference|root)$',label[0]):
                    roots.setdefault(label[0],{})[label[2]]={'range':float(np.ptp(values)),'net':float(values[-1]-values[0])}
            t['sampled_root_candidates']=roots
        if idx%200==0:print('enriched',idx,flush=True)
    (CACHE/'scan.json').write_text(json.dumps(scan,indent=2),encoding='utf8')
    print('enrichment complete')

if __name__=='__main__':main()
