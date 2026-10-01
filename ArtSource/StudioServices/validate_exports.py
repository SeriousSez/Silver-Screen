"""Read-only validation of the source packets; writes only the owned review report."""
from pathlib import Path
import gzip,hashlib,json,math
ROOT=Path(__file__).resolve().parents[2]
report={}
for relative in ['ArtExports/StudioServices/Production/production_meshes.json.gz','ArtExports/PeriodEnvironment1930/kit_meshes.json.gz']:
    path=ROOT/relative;data=json.loads(gzip.decompress(path.read_bytes()));bad=0;triangles=0
    for part in data['parts']:
        positions=part['positions'];normals=part['normals'];uv=part['uv'];count=len(positions)//3
        assert len(normals)==len(positions) and len(uv)==count*2,part['name']
        assert all(math.isfinite(x) for x in positions+normals+uv),part['name']
        assert len(part['materials'])==len(part['submeshes']),part['name']
        for sub in part['submeshes']:
            indices=sub['indices'];assert len(indices)%3==0 and all(0<=v<count for v in indices)
            for i in range(0,len(indices),3):
                a,b,c=[positions[v*3:v*3+3] for v in indices[i:i+3]]
                u=[b[j]-a[j] for j in range(3)];v=[c[j]-a[j] for j in range(3)]
                cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
                triangles+=1;bad+=sum(x*x for x in cross)<1e-16
    report[relative]={'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'parts':len(data['parts']),'triangles':triangles,'degenerateTriangles':bad}
production=json.loads((ROOT/'ArtExports/StudioServices/Production/production_manifest.json').read_text(encoding='utf-8'))
massing=json.loads((ROOT/'ArtExports/StudioServices/Massing/massing_manifest.json').read_text(encoding='utf-8'))
report['approvedSourceUnchanged']=hashlib.sha256((ROOT/'ArtSource/StudioServices/StudioServices_Massing.blend').read_bytes()).hexdigest()==production['approvedSourceSha256']
report['approvedAnchorsUnchanged']=production['anchors']==massing['anchors']
assert report['approvedSourceUnchanged'] and report['approvedAnchorsUnchanged']
(ROOT/'ArtReview/StudioServices/Production/mesh-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
assert all(value['degenerateTriangles']==0 for value in report.values() if isinstance(value,dict))
