"""Retrieve the publisher's free FBX bundle; retain only the Eric source candidate."""
import hashlib, json, tempfile, urllib.request, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
DEST = ROOT / 'ArtSource/Characters/ThirdParty/RenderpeopleEric'
URL = 'https://renderpeople.com/sample/free/renderpeople_free_rigged_people_FBX.zip'
ARCHIVE = Path(tempfile.gettempdir()) / 'silverscreen-renderpeople-free-fbx.zip'

if __name__ == '__main__':
    if not ARCHIVE.exists():
        request = urllib.request.Request(URL, headers={'User-Agent':'SilverScreen-art-source-evaluation/1.0'})
        with urllib.request.urlopen(request, timeout=120) as response, ARCHIVE.with_suffix('.part').open('wb') as output:
            print('Download bytes:', response.headers.get('Content-Length', 'unknown'), flush=True)
            count = 0
            while data := response.read(4 * 1024 * 1024):
                output.write(data)
                count += len(data)
                if count % (32 * 1024 * 1024) == 0: print('Downloaded', count, flush=True)
        ARCHIVE.with_suffix('.part').rename(ARCHIVE)
    DEST.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(ARCHIVE) as archive:
        inventory = [{'path':i.filename, 'bytes':i.file_size} for i in archive.infolist() if not i.is_dir()]
        (DEST/'bundle-inventory.json').write_text(json.dumps(inventory, indent=2))
        print(json.dumps(inventory, indent=2), flush=True)
        records = []
        for info in archive.infolist():
            if info.is_dir() or not any(t in info.filename.lower() for t in ('eric','license','readme','terms')): continue
            path = (DEST / info.filename).resolve()
            if not path.is_relative_to(DEST.resolve()): raise ValueError(info.filename)
            data = archive.read(info)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
            records.append({'path':info.filename,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
    with zipfile.ZipFile(DEST/'rp_eric_rigged_001_FBX.zip') as inner:
        for info in inner.infolist():
            if info.is_dir(): continue
            path = (DEST/'Original'/info.filename).resolve()
            if not path.is_relative_to(DEST.resolve()): raise ValueError(info.filename)
            data = inner.read(info)
            path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(data)
            records.append({'path':str(path.relative_to(DEST)),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
    with ARCHIVE.open('rb') as source: digest = hashlib.file_digest(source, 'sha256').hexdigest()
    (DEST/'source-manifest.json').write_text(json.dumps({'source':URL,'publisher':'Renderpeople','license_url':'https://renderpeople.com/general-terms-and-conditions/','archive_sha256':digest,'files':records}, indent=2))
