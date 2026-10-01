"""Freeze approved art before deriving runtime assets; never rewrite the baseline."""
from pathlib import Path
import hashlib,json,sys
R=Path(__file__).resolve().parents[2]
paths=['ArtSource/StageSchoolA4','ArtExports/StageSchoolA4','Assets/SilverScreen/Environment/StageSchoolA4','ArtSource/StageSchoolA3','Assets/SilverScreen/Environment/StageSchoolA3','ArtSource/PeriodEnvironment1930','ArtSource/ProductionEquipment1930Candidate','Assets/SilverScreen/Environment/PeriodEnvironment1930','Assets/SilverScreen/Environment/ProductionEquipment1930Candidate','Assets/Scenes/Studio.unity']
def snapshot():
    return {p.relative_to(R).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for rel in paths for p in ([R/rel] if (R/rel).is_file() else (R/rel).rglob('*')) if p.is_file() and '__pycache__' not in p.parts}
base=R/(sys.argv[sys.argv.index('--baseline')+1] if '--baseline' in sys.argv else 'ArtSource/StageSchoolB2/master_freeze.json')
if '--freeze' in sys.argv:
    assert not base.exists(), 'Do not replace approval baseline'
    base.write_text(json.dumps(snapshot(),indent=2))
else:
    old=json.loads(base.read_text());now=snapshot();changed=[p for p,h in old.items() if now.get(p)!=h]
    report=dict(protectedFiles=len(old),changed=changed)
    (R/'ArtReview/StageSchoolB2/preservation.json').write_text(json.dumps(report,indent=2))
    print(report);assert not changed
