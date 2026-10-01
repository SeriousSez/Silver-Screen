"""Record and compare pre-existing working files without modifying Git."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
REPORT = ROOT / 'ArtReview/Characters/Gate1'
OWN_ROOTS = ('ArtSource/Characters/', 'ArtReview/Characters/', 'ArtExports/Characters/',
             'Assets/Scripts/Domain/Characters/', 'Assets/Scripts/Presentation/Characters/',
             'Assets/Editor/Characters/', 'Assets/SilverScreen/Art/Characters/Gate1/')

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024*1024), b''): h.update(block)
    return h.hexdigest()

def listed_files():
    raw = subprocess.check_output(['git','ls-files','-z','--cached','--others','--exclude-standard'], cwd=ROOT)
    return sorted(set(x.decode('utf-8') for x in raw.split(b'\0') if x))

def main():
    REPORT.mkdir(parents=True, exist_ok=True)
    target = REPORT / 'worktree-baseline.json'
    if sys.argv[1] == 'record':
        if target.exists(): raise RuntimeError('Refusing to replace baseline')
        files = {p:digest(ROOT/p) for p in listed_files() if not p.startswith(OWN_ROOTS) and (ROOT/p).is_file()}
        baseline = {'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
                    'status':subprocess.check_output(['git','status','--porcelain=v1'],cwd=ROOT,text=True), 'files':files}
        target.write_text(json.dumps(baseline,indent=2),encoding='utf-8')
        print(json.dumps({'baseline_files':len(files)}))
    else:
        baseline = json.loads(target.read_text(encoding='utf-8'))
        changed = [p for p,h in baseline['files'].items() if not (ROOT/p).is_file() or digest(ROOT/p) != h]
        added = [p for p in listed_files() if p not in baseline['files']]
        result = {'preexisting_changed':changed, 'added':added}
        (REPORT/'worktree-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
        print(json.dumps({'preexisting_changed':changed,'added_count':len(added)}))

if __name__ == '__main__': main()
