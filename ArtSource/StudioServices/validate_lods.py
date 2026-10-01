"""Validate LOD export provenance and geometry without touching approved source."""
from pathlib import Path
import gzip
import hashlib
import json
import math

ROOT = Path(__file__).resolve().parents[2]
report = {}
for directory, base in [('PeriodEnvironment1930', 'ArtExports/PeriodEnvironment1930/kit_meshes.json.gz'),
                        ('StudioServices', 'ArtExports/StudioServices/Production/production_meshes.json.gz')]:
    folder = ROOT / 'ArtExports' / directory / 'LOD'
    manifest = json.loads((folder/'lod_manifest.json').read_text())
    assert hashlib.sha256((ROOT/manifest['source']).read_bytes()).hexdigest() == manifest['sourceSha256']
    assert hashlib.sha256((ROOT/'ArtSource/PeriodEnvironment1930/build_lods.py').read_bytes()).hexdigest() == manifest['generatorSha256']
    original = json.loads(gzip.decompress((ROOT/base).read_bytes()))
    data = json.loads(gzip.decompress((folder/'lod_meshes.json.gz').read_bytes()))
    expected = {p['name']+'_LOD'+str(level) for p in original['parts'] for level in (1,2)}
    assert {p['name'] for p in data['parts']} == expected
    counts = {0:sum(len(s['indices'])//3 for p in original['parts'] for s in p['submeshes']),1:0,2:0}
    for p in data['parts']:
        positions = p['positions']; n = len(positions)//3
        assert n > 0 and len(p['normals']) == n*3 and len(p['uv']) == n*2
        assert all(math.isfinite(x) for x in positions+p['normals']+p['uv'])
        assert len(p['materials']) == len(p['submeshes'])
        for sub in p['submeshes']:
            ix = sub['indices']; assert len(ix)%3 == 0 and all(0 <= x < n for x in ix)
            for i in range(0,len(ix),3):
                a,b,c = [positions[k*3:k*3+3] for k in ix[i:i+3]]
                u = [b[j]-a[j] for j in range(3)];v = [c[j]-a[j] for j in range(3)]
                cross = [u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
                assert sum(x*x for x in cross) >= 1e-16, p['name']
            counts[int(p['name'][-1])] += len(ix)//3
    report[directory] = dict(sourceUnchanged=True, sourceSha256=manifest['sourceSha256'], assemblies=len(original['parts']), triangles=counts)
out = ROOT/'ArtReview/StudioServices/LOD';out.mkdir(parents=True,exist_ok=True)
(out/'export-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
