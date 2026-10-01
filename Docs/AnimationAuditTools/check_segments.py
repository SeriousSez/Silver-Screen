"""Check named trimmed-file candidates against raw curve samples; never merge partial clips."""
from fbx_scan import *

def curves(path):
    _,doc=parse(path.read_bytes());o={n.p[0]:n for n in doc.get('Objects').c if n.p and isinstance(n.p[0],int)};c=[n.p for n in doc.get('Connections').c]
    out={}
    for conn in c:
        if conn[1] not in o or o[conn[1]].name!='AnimationCurveNode' or conn[2] not in o or o[conn[2]].name!='Model':continue
        for child in c:
            if child[2]!=conn[1] or child[1] not in o or o[child[1]].name!='AnimationCurve':continue
            n=o[child[1]];ts=n.get('KeyTime').p;vs=n.get('KeyValueFloat').p
            if ts and vs:out[(norm(o[conn[2]].p[1]),conn[3],child[3])]=(ts[0],vs[0])
    return out

def main():
    cat=json.loads((ROOT/'Docs/AnimationLibraryCatalog.json').read_text());fs={f['relative_path']:f for f in cat['files']};out=[]
    for path,f in fs.items():
        if '_segment.fbx' not in path.lower():continue
        original=re.sub('_segment.fbx$','.fbx',path,flags=re.I)
        if original not in fs:continue
        a,b=curves(SOURCE/original),curves(SOURCE/path);common=sorted(a.keys()&b.keys());match=None
        pivots=[k for k in common if len(b[k][1])>3 and np.ptp(b[k][1])>.1]
        if pivots:
            k=max(pivots,key=lambda k:float(np.ptp(b[k][1]))); ta,va=a[k];tb,vb=b[k]
            for offset in np.where(np.isclose(va,vb[0],atol=1e-5,rtol=0))[0]:
                if offset+len(vb)>len(va):continue
                if not np.allclose(va[offset:offset+len(vb)],vb,atol=1e-5,rtol=0):continue
                time_offset=int(ta[offset]-tb[0]); valid=True
                for key in common:
                    at,av=a[key];bt,bv=b[key]; ix=np.searchsorted(at,bt+time_offset)
                    if np.any(ix>=len(at)) or not np.all(np.abs(at[ix]-(bt+time_offset))<=2) or not np.allclose(av[ix],bv,atol=1e-5,rtol=0):valid=False;break
                if valid:match={'full_take_start_seconds':float((tb[0]+time_offset-ta[0])/TICKS),'duration_seconds':float((tb[-1]-tb[0])/TICKS),'channels_compared':len(common)};break
        out.append({'category':'PARTIAL_OVERLAP_NOT_MERGED','full_file_id':fs[original]['id'],'segment_file_id':f['id'],'confidence':'high' if match else 'unresolved','evidence':match if match else 'Filename identifies a segment candidate; exact contiguous curve samples not established. Retained separately.'})
    dump=ROOT/'GeneratedAssets/AnimationAuditCache/segment_checks.json';dump.write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))

if __name__=='__main__':main()
