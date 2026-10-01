"""Download source packs into the OS cache; retain only Gate 1 inputs in ArtSource."""
import hashlib
import json
import os
from pathlib import Path
import urllib.request
import zipfile
from concurrent.futures import ThreadPoolExecutor

ROOT=Path(__file__).resolve().parents[4]
DEST=ROOT/'ArtSource/Characters/ThirdParty/MakeHuman/Packs'
PACKS={
 'system':'https://files.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip',
 'suits':'https://files.makehumancommunity.org/asset_packs/suits01/suits01_cc0.zip',
 'faceunits':'https://files.makehumancommunity.org/functional/faceunits01.zip'
}
ASSETS=('young_caucasian_male','young_asian_female','young_african_male','middleage_caucasian_male',
        'high-poly','brown','green','teeth_base','tongue01','eyebrow001','eyebrow003','eyelashes01',
        'short01','short02','short03','short04','bob01','bob02','afro01',
        'male_worksuit01','shoes01','shoes02','shoes03','shoes04','shoes05',
        'toigo_male_suit_tie_and_jacket','toigo_female_suit','toigo_female_double-breasted_suit')

def fetch(entry):
    key,url=entry
    cache=Path(os.environ['TEMP'])/('silverscreen-gate1-'+key+'.zip')
    if not cache.exists():
        request=urllib.request.Request(url,headers={'User-Agent':'SilverScreen-character-pipeline/1.0'})
        with urllib.request.urlopen(request,timeout=120) as response, cache.with_suffix('.part').open('wb') as output:
            while block:=response.read(1024*1024): output.write(block)
        cache.with_suffix('.part').rename(cache)
    z=zipfile.ZipFile(cache)
    records=[]
    for item in z.infolist():
        if item.is_dir():continue
        name=item.filename.replace('\\','/')
        selected=key=='faceunits' or any(a in name.split('/') for a in ASSETS) or 'license' in name.lower()
        if not selected:continue
        target=(DEST/key/name).resolve()
        if not target.is_relative_to(DEST.resolve()):raise ValueError(name)
        payload=z.read(item)
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(payload)
        records.append({'path':name,'sha256':hashlib.sha256(payload).hexdigest(),'bytes':len(payload)})
    (DEST/(key+'-inventory.json')).write_text(json.dumps(z.namelist(),indent=2),encoding='utf-8')
    digest=hashlib.sha256(cache.read_bytes()).hexdigest()
    (DEST/(key+'-manifest.json')).write_text(json.dumps({'source':url,'archive_sha256':digest,'license':'CC0-1.0','files':records},indent=2),encoding='utf-8')
    print(json.dumps({'pack':key,'selected':len(records),'bytes':sum(x['bytes'] for x in records)}),flush=True)

if __name__=='__main__':
    DEST.mkdir(parents=True,exist_ok=True)
    with ThreadPoolExecutor(max_workers=3) as executor:list(executor.map(fetch,PACKS.items()))
