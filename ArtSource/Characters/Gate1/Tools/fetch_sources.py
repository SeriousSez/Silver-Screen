"""Fetch pinned CC0 source data only; no external program or addon is installed."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import io
import json
from pathlib import Path
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[4]
DEST = ROOT / 'ArtSource/Characters/ThirdParty/MakeHuman'
REVISION = 'eb93ef2f0e7608e4a298192adc0cb4b351cfe689'

def download(url):
    request = urllib.request.Request(url, headers={'User-Agent':'SilverScreen-character-pipeline/1.0'})
    with urllib.request.urlopen(request, timeout=90) as response: return response.read()

def main():
    DEST.mkdir(parents=True, exist_ok=True)
    url = 'https://codeload.github.com/makehumancommunity/mpfb2/zip/' + REVISION
    raw = download(url)
    archive = zipfile.ZipFile(io.BytesIO(raw))
    records=[]
    for name in archive.namelist():
        relative = name.split('/',1)[-1]
        selected = relative in ('LICENSE.md','LICENSE.ASSETS.md') or relative == 'src/mpfb/data/3dobjs/base.obj'
        selected |= relative in ('src/mpfb/data/rigs/standard/rig.mixamo_unity.json','src/mpfb/data/rigs/standard/weights.mixamo_unity.json')
        if relative.startswith('src/mpfb/data/targets/') and relative.endswith('.gz'):
            group = relative.split('/')[4]
            selected |= group in ('head','nose','mouth','eyes','eyebrows','chin','cheek','ears','neck','armslegs','torso','expression')
            selected |= group == 'macrodetails' and 'young' in relative
        if not selected: continue
        local = relative.removeprefix('src/mpfb/data/')
        target = (DEST/local).resolve()
        if not target.is_relative_to(DEST.resolve()): raise ValueError(local)
        payload = archive.read(name)
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(payload)
        records.append({'path':local,'sha256':hashlib.sha256(payload).hexdigest(),'bytes':len(payload)})
    manifest = {'project':'MakeHuman Community MPFB2','revision':REVISION,'source':url,
                'archive_sha256':hashlib.sha256(raw).hexdigest(),'license':'CC0-1.0 (asset data; see LICENSE.md)', 'files':records}
    (DEST/'source-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps({'downloaded_bytes':len(raw),'selected_files':len(records),'selected_bytes':sum(x['bytes'] for x in records)}))

if __name__=='__main__':main()
