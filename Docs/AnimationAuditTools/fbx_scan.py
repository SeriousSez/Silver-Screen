"""Read-only FBX archive scanner. Outputs only under GeneratedAssets/AnimationAuditCache.
Binary FBX structural parser; no SDK evaluation, retargeting, or source writes.
"""
from pathlib import Path
import struct, zlib, hashlib, json, re, collections, sys, time
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ExternalSourceAssets/Animations'
CACHE = ROOT / 'GeneratedAssets/AnimationAuditCache'
TICKS = 46186158000

def digest(x):
    return hashlib.sha256(x).hexdigest()

def clean(x):
    return str(x).split('\x00')[0].split('::')[-1]

def norm(x):
    return re.sub(r'[^a-z0-9]', '', clean(x).split(':')[-1].lower())

class Node:
    def __init__(self, name, props, children):
        self.name, self.p, self.c = name, props, children
    def all(self, name): return [x for x in self.c if x.name == name]
    def get(self, name): return next((x for x in self.c if x.name == name), Node(name, [], []))

def parse(data, skip_animation=False):
    if not data.startswith(b'Kaydara FBX Binary'): raise ValueError('Nonbinary FBX: unsupported by this scanner')
    version = struct.unpack_from('<I', data, 23)[0]
    wide = version >= 7500
    fmt, hs = ('<QQQB', 25) if wide else ('<IIIB', 13)
    def node(pos):
        end, count, plen, nlen = struct.unpack_from(fmt, data, pos)
        if not end: return None, pos + hs
        if end > len(data) or end <= pos: raise ValueError('Invalid node end offset')
        pos += hs
        name = data[pos:pos+nlen].decode('utf8', errors='replace'); pos += nlen
        if name in ('Geometry', 'Video', 'Texture') or (skip_animation and name in ('AnimationCurve', 'AnimationCurveNode')): return Node(name, [], []), end
        props = []
        for _ in range(count):
            typ = chr(data[pos]); pos += 1
            if typ in 'Y C I F D L'.split():
                f = {'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}[typ]
                props.append(struct.unpack_from('<'+f, data, pos)[0]); pos += struct.calcsize(f)
            elif typ in 'SR':
                n = struct.unpack_from('<I', data, pos)[0]; pos += 4
                v = data[pos:pos+n]; pos += n
                props.append(v.decode('utf8', errors='replace') if typ == 'S' else v.hex())
            elif typ in 'fdlibc':
                n, enc, size = struct.unpack_from('<III', data, pos); pos += 12
                raw = data[pos:pos+size]; pos += size
                if enc == 1: raw = zlib.decompress(raw)
                elif enc != 0: raise ValueError('Unknown array encoding')
                a = np.frombuffer(raw, dtype={'f':'<f4','d':'<f8','l':'<i8','i':'<i4','b':'u1','c':'u1'}[typ])
                if len(a) != n: raise ValueError('Array length mismatch')
                props.append(a)
            else: raise ValueError('Unknown FBX property '+typ)
        children = []
        while pos < end:
            child, pos = node(pos)
            if child is None: break
            children.append(child)
        return Node(name, props, children), end
    children, pos = [], 27
    while pos + hs < len(data):
        n, pos = node(pos)
        if n is None: break
        children.append(n)
    return version, Node('Document', [], children)

def properties(node): return {x.p[0]:x.p[4:] for x in node.get('Properties70').c if x.name == 'P'}
def scalar(d,k,default=None): return d.get(k,[default])[0]

def analyze(data):
    version, doc = parse(data)
    objects = {n.p[0]:n for n in doc.get('Objects').c if n.p and isinstance(n.p[0],int)}
    con = [n.p for n in doc.get('Connections').c if n.name == 'C']
    parents = collections.defaultdict(list)
    children = collections.defaultdict(list)
    for c in con:
        parents[c[1]].append((c[2], c[3] if len(c)>3 else None))
        children[c[2]].append((c[1], c[3] if len(c)>3 else None))
    models = {i:n for i,n in objects.items() if n.name == 'Model'}
    bones = {i:n for i,n in models.items() if len(n.p)>2 and n.p[2] in ('LimbNode','Limb','Root')}
    hierarchy=[]
    for i,n in bones.items():
        par = next((p for p,_ in parents[i] if p in models), None)
        hierarchy.append({'name':clean(n.p[1]),'parent':clean(models[par].p[1]) if par else None,'type':n.p[2]})
    hierarchy.sort(key=lambda x:x['name'])
    settings=properties(doc.get('GlobalSettings'))
    mode=scalar(settings,'TimeMode')
    fps={0:30,1:120,2:100,3:60,4:50,5:48,6:30,7:30,8:30000/1001,9:30000/1001,10:25,11:24,12:1000,13:24000/1001,14:scalar(settings,'CustomFrameRate'),15:96,16:72,17:60000/1001}.get(mode)
    signature=digest(json.dumps([(norm(x['name']),norm(x['parent'])) for x in hierarchy],sort_keys=True).encode())
    stacks=[(i,n) for i,n in objects.items() if n.name=='AnimationStack']
    clips=[]; samples={}
    for si,stack in stacks:
        layers={i for i,_ in children[si] if objects.get(i,Node('',[],[])).name=='AnimationLayer'}
        cns={i for l in layers for i,_ in children[l] if objects.get(i,Node('',[],[])).name=='AnimationCurveNode'}
        curves=[]
        for cn in cns:
            targets=[(i,p) for i,p in parents[cn] if i in models]
            for ci,axis in children[cn]:
                curve=objects.get(ci)
                if not curve or curve.name!='AnimationCurve': continue
                ts=curve.get('KeyTime').p; vs=curve.get('KeyValueFloat').p
                if not ts or not vs or not len(ts[0]): continue
                for target,prop in targets:
                    curves.append((norm(models[target].p[1]),prop,axis,ts[0],vs[0],curve,target))
        pp=properties(stack); start=scalar(pp,'LocalStart'); stop=scalar(pp,'LocalStop')
        if start is None and curves: start=min(int(c[3][0]) for c in curves)
        if stop is None and curves: stop=max(int(c[3][-1]) for c in curves)
        duration=(stop-start)/TICKS if start is not None and stop is not None else None
        exact=hashlib.sha256(); sampled=hashlib.sha256(); curve_table=[]; varying=set(); allbones=set(); roots={}; fingers=[]
        grid=np.linspace(start,stop,121) if duration is not None else None
        matrix=[]; labels=[]
        for name,prop,axis,ts,vs,curve,target in sorted(curves,key=lambda c:(c[0],c[1] or '',c[2] or '')):
            label=[name,prop,axis]; allbones.add(name)
            span=float(np.ptp(vs)); active=span>1e-5
            if active: varying.add(name)
            if active and re.search(r'finger|thumb|index|middle|ring|pinky',name): fingers.append(name)
            exact.update(json.dumps(label).encode()); exact.update((ts-start).astype('<i8').tobytes()); exact.update(vs.astype('<f4').tobytes())
            for key in ('KeyAttrFlags','KeyAttrDataFloat','KeyAttrRefCount'):
                val=curve.get(key).p
                if val: exact.update(key.encode()); exact.update(val[0].tobytes())
            if grid is not None:
                vals=np.interp(grid,ts,vs)
                # Translation offsets are removed; rotations and scale retain absolute local values.
                if prop=='Lcl Translation': vals=vals-vals[0]
                matrix.append(vals); labels.append(label)
                sampled.update(json.dumps(label).encode()); sampled.update(np.round(vals,3).astype('<f4').tobytes())
            if prop=='Lcl Translation' and (name in ('hips','hip','pelvis','root') or not any(p in bones for p,_ in parents[target])):
                roots.setdefault(name,{})[axis]={'start':float(vs[0]),'end':float(vs[-1]),'range':span,'net':float(vs[-1]-vs[0])}
            curve_table.append({'bone':name,'property':prop,'axis':axis,'keys':len(ts),'varying':active})
        unique_times=np.unique(np.concatenate([c[3] for c in curves])) if curves else np.array([])
        up=scalar(settings,'UpAxis',1); horizontal=['d|X','d|Y','d|Z']; horizontal.pop(up if up in (0,1,2) else 1)
        unit=scalar(settings,'UnitScaleFactor'); threshold=5/unit if unit and unit>0 else None
        horiz=max((v['range'] for r in roots.values() for a,v in r.items() if a in horizontal),default=None)
        motion='unknown' if horiz is None or threshold is None else ('translating_candidate' if horiz>threshold else 'in_place_candidate')
        clip={'name':clean(stack.p[1]),'layer_count':len(layers),'start_ticks':start,'stop_ticks':stop,'duration_seconds':duration,'frame_rate':fps,'frame_count_inclusive':round(duration*fps)+1 if duration is not None and fps else None,'unique_key_time_count':len(unique_times),'animated_bone_count':len(varying),'keyed_bone_count':len(allbones),'keyed_channel_count':len(curves),'varying_channel_count':sum(c['varying'] for c in curve_table),'finger_animation_present':bool(fingers) if curves else None,'finger_bones_varying':sorted(set(fingers)),'root_translation':roots,'motion_classification':motion,'motion_classification_basis':'Local root/hip horizontal range >5 cm; no global transform or ground-path evaluation','curve_fingerprint':exact.hexdigest() if curves else None,'sample_fingerprint':sampled.hexdigest() if curves else None,'channels':curve_table}
        clips.append(clip)
        samples[str(len(clips)-1)]={'labels':labels,'values':np.asarray(matrix,dtype=np.float32)}
    names=' '.join(n['name'] for n in hierarchy).lower()
    rig='Mixamo' if 'mixamorig' in names else ('Unreal' if 'upperarm_l' in names or 'thigh_l' in names else ('Rokoko' if 'leftarm' in names and 'leftupleg' in names else 'unknown'))
    creator=doc.get('FBXHeaderExtension').get('Creator').p
    return {'fbx_version':version,'creator':creator[0] if creator else None,'bone_count':len(bones),'skeleton_hierarchy':hierarchy,'skeleton_signature':signature,'skeleton_name_inference':rig,'global_settings':settings,'take_count':len(clips),'takes':clips,'legacy_take_names':[n.p[0] for n in doc.get('Takes').all('Take') if n.p]},samples

def main():
    CACHE.mkdir(parents=True,exist_ok=True)
    old=json.loads((CACHE/'scan.json').read_text()) if (CACHE/'scan.json').exists() else {'files':[]}
    prior={x['relative_path']:x for x in old['files']}; out=[]
    files=sorted((p for p in SOURCE.rglob('*') if p.is_file()),key=lambda p:p.as_posix().lower())
    for index,p in enumerate(files):
        rel=p.relative_to(SOURCE).as_posix(); stat=p.stat()
        if rel in prior and prior[rel]['size_bytes']==stat.st_size and prior[rel]['mtime_ns']==stat.st_mtime_ns:
            out.append(prior[rel]); continue
        data=p.read_bytes(); sha=digest(data)
        entry={'relative_path':rel,'original_filename':p.name,'extension':p.suffix.lower(),'size_bytes':len(data),'mtime_ns':stat.st_mtime_ns,'sha256':sha}
        if p.suffix.lower()=='.fbx':
            try:
                meta_path=CACHE/(sha+'.json')
                if meta_path.exists(): entry['fbx']=json.loads(meta_path.read_text())
                else:
                    meta,samples=analyze(data); entry['fbx']=meta
                    meta_path.write_text(json.dumps(meta),encoding='utf8')
                    for i,s in samples.items(): np.savez_compressed(CACHE/(sha+'_'+i+'.npz'), labels=np.array(s['labels']),values=s['values'])
                entry['status']='parsed'
            except Exception as e: entry['status']='unreadable'; entry['error']=str(e)
        else: entry['status']='support_file'
        out.append(entry)
        if index%100==0: print(index,rel,entry['status'],flush=True)
    (CACHE/'scan.json').write_text(json.dumps({'files':out},indent=2),encoding='utf8')
    print('DONE',len(out),collections.Counter(x['status'] for x in out))

if __name__=='__main__': main()
