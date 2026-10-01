import json,gzip,re,pathlib
names={p.stem.removesuffix('.asset'):re.search(r'guid: (\w+)',p.read_text())[1] for p in pathlib.Path('Assets/SilverScreen/Environment/StageSchoolA3/Meshes').glob('*.meta')}
a={p['name']:p for p in json.load(open('ArtExports/StageSchoolB/master_meshes.json'))['parts']};b={p['name']:p for p in json.load(gzip.open('ArtExports/StageSchoolB/lod0.json.gz','rt'))['parts']}
for name in ['Shell_Front_Windows','Roofs_Entrance','Furnishings_Washroom']:
 print(name)
 for d in [a,b]:
  p=d[names[name]];print([(pathlib.Path(m).stem,len(s['indices'])//3) for m,s in zip(p['materials'],p['submeshes'])])
