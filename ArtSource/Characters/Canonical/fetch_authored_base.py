"""Download the official, versioned Blender community authored asset bundle."""
import hashlib, json, urllib.request, zipfile, io
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
DEST=ROOT/'ArtSource/Characters/ThirdParty/BlenderHumanBaseMeshes'
URL='https://download.blender.org/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.4.1.zip'

if __name__=='__main__':
    req=urllib.request.Request(URL,headers={'User-Agent':'SilverScreen-source-art-review/1.0'})
    with urllib.request.urlopen(req,timeout=120) as response: data=response.read()
    z=zipfile.ZipFile(io.BytesIO(data)); DEST.mkdir(parents=True,exist_ok=True)
    records=[]
    for info in z.infolist():
        if info.is_dir():continue
        path=(DEST/info.filename).resolve()
        if not path.is_relative_to(DEST.resolve()):raise ValueError(info.filename)
        path.parent.mkdir(parents=True,exist_ok=True);content=z.read(info)
        path.write_bytes(content)
        records.append({'path':info.filename,'sha256':hashlib.sha256(content).hexdigest(),'bytes':len(content)})
    (DEST/'source-manifest.json').write_text(json.dumps({'source':URL,'version':'1.4.1','archive_sha256':hashlib.sha256(data).hexdigest(),'files':records},indent=2))
    print(json.dumps({'bytes':len(data),'files':records},indent=2))
