"""Immutable before/after evidence for A4's read-only production dependencies."""
from pathlib import Path
import hashlib, json, sys

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
PATHS = [
    'ArtSource/StageSchoolA1', 'ArtSource/StageSchoolA2', 'ArtSource/StageSchoolA3',
    'ArtExports/StageSchoolA3', 'Assets/SilverScreen/Environment/StageSchoolA1',
    'Assets/SilverScreen/Environment/StageSchoolA2', 'Assets/SilverScreen/Environment/StageSchoolA3',
    'Assets/SilverScreen/Environment/StageSchoolB',
    'ArtSource/PeriodEnvironment1930', 'ArtSource/ProductionEquipment1930Candidate',
    'Assets/SilverScreen/Environment/PeriodEnvironment1930',
    'Assets/SilverScreen/Environment/ProductionEquipment1930Candidate',
    'Assets/Scenes/Studio.unity', 'Assets/Scenes/Studio.unity.meta',
]

def hashes():
    result = {}
    for rel in PATHS:
        root = ROOT / rel
        for p in ([root] if root.is_file() else root.rglob('*')):
            if p.is_file() and '__pycache__' not in p.parts:
                result[p.relative_to(ROOT).as_posix()] = hashlib.sha256(p.read_bytes()).hexdigest()
    return result

if __name__ == '__main__':
    target = HERE / 'preservation_baseline.json'
    current = hashes()
    if '--baseline' in sys.argv:
        if target.exists():
            raise RuntimeError('Never replace the immutable pre-A4 baseline')
        target.write_text(json.dumps(current, indent=2))
        print('Protected files:', len(current))
    else:
        original = json.loads(target.read_text())
        changed = [p for p, h in original.items() if current.get(p) != h]
        report = dict(protected_files=len(original), changed_or_missing=changed,
                      added_dependencies=sorted(set(current)-set(original)))
        (ROOT/'ArtReview/StageSchoolA4/preservation_result.json').write_text(json.dumps(report, indent=2))
        print(json.dumps(report))
        assert not changed
