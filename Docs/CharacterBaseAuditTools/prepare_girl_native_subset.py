"""Derive Girl native seam/material correspondence from his audited FBX corners.

Run with repository cwd after export_girl_bald.py and ExportNative().
The measured maximum position discrepancy was 0.000322 mm, with exact UVs.
Girl's 0.0007 mm position guard allows float-transform noise, not surface fitting.
"""
import hashlib,json
from pathlib import Path
import numpy as np
from scipy.spatial import cKDTree


def main():
    root=Path('TestResults/GirlFoundation');out=root/'Unity'
    s=json.loads((root/'Bald/source-correspondence.json').read_text())
    native_path=out/'native.json';n=json.loads(native_path.read_text())
    p=np.array(s['points'])[:,[0,2,1]]*[-1,1,-1]
    w=np.array([[v[k] for k in 'xyz'] for v in n['points']]);uv=np.array([[v[k] for k in 'xy'] for v in n['uv']])
    corners=s['corners'];ids=np.array([r['vertex'] for r in corners]);cu=np.array([r['uv'] for r in corners])
    material=np.array([r['material'] for r in corners]);tree=cKDTree(np.c_[p[ids],cu])
    native_materials=[set() for _ in w]
    for mat,row in enumerate(n['submeshes']):
        for i in row['indices']:native_materials[i].add(mat)
    mapping=[];position_errors=[];uv_errors=[]
    for i,point in enumerate(w):
        candidates=tree.query_ball_point(np.r_[point,uv[i]],7.001e-7)
        candidates=[c for c in candidates if np.linalg.norm(p[ids[c]]-point)<=7e-7 and np.linalg.norm(cu[c]-uv[i])==0 and material[c] in native_materials[i]]
        distinct=set(ids[c] for c in candidates)
        assert len(distinct)==1, ('Ambiguous or missing Girl correspondence',i,distinct)
        j=distinct.pop();mapping.append(int(j));position_errors.append(float(np.linalg.norm(p[j]-point)));uv_errors.append(float(min(np.linalg.norm(cu[c]-uv[i]) for c in candidates)))
        assert native_materials[i]==set(int(material[c]) for c in candidates)
    assert len(set(mapping))==17872 and len(w)==20207
    missing=sorted(set(range(len(p)))-set(mapping))
    assert missing==s['loose'], ('Only the four previously non-rendered loose vertices may be absent',missing)
    hair=set(s['hair']);keep=[i for i,j in enumerate(mapping) if j not in hair];keepset=set(keep)
    triangles=0;submeshes=[]
    for row in n['submeshes']:
        t=np.array(row['indices']).reshape(-1,3);retained=0
        for f in t:
            count=sum(int(i) in keepset for i in f);assert count in (0,3)
            retained+=count==3
        triangles+=retained;submeshes.append(retained)
    assert triangles==32330 and len(set(mapping[i] for i in keep))==16406
    report=dict(native_vertices=len(w),source_vertices=len(p),retained_native_vertices=len(keep),removed_native_vertices=len(w)-len(keep),
                retained_source_vertices=16406,seam_material_splits=len(w)-17872,retained_triangles=triangles,retained_submesh_triangles=submeshes,
                position_guard_mm=.0007,measured_max_position_error_mm=max(position_errors)*1000,uv_guard=0,measured_max_uv_error=max(uv_errors),
                all_mappings_unique=True,all_surface_source_vertices_covered=True,non_surface_source_vertices_absent_from_original_native=missing,blender_loose_vertices_retained=True,material_correspondence_exact=True,partial_triangles=0)
    (out/'partition.json').write_text(json.dumps(dict(keep=keep,triangles=triangles,nativeSha256=hashlib.sha256(native_path.read_bytes()).hexdigest())),encoding='utf-8')
    (out/'correspondence.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    np.savez_compressed(out/'native-mapping.npz',source_index=mapping,keep=keep,position_error=position_errors)
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
