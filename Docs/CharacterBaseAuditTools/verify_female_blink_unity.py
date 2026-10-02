"""Verify milestone preservation and record its exact local evidence manifest."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path


def digest(path):
    hasher = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024*1024), b''):
            hasher.update(block)
    return hasher.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence',type=Path,default=Path('TestResults/FemaleBlinkUnity'))
    parser.add_argument('--source-root',type=Path,required=True)
    args = parser.parse_args()
    baseline = json.loads((args.evidence/'baseline.json').read_text())
    inventory = json.loads(Path('TestResults/CharacterBaseAudit/ChildSourceUpdate/inventory.json').read_text())
    findings = []
    for label, root, rows in (('purchased',args.source_root,inventory),('prior_work',Path('.'),baseline['preserved_files'])):
        changed=[]
        for row in rows:
            path=root/row['path']
            if not path.is_file() or path.stat().st_size!=row['bytes'] or digest(path)!=row['sha256'] or path.stat().st_mtime_ns!=row['mtime_ns']:
                changed.append(str(path))
        findings.append({'set':label,'files':len(rows),'changed':changed})
    def git(*args):
        return subprocess.check_output(['git','-c','core.fsmonitor=false',*args],text=True,encoding='utf-8').strip()
    check=subprocess.run(['git','-c','core.fsmonitor=false','diff','--check'],text=True,capture_output=True)
    new_paths=git('ls-files','--others','--exclude-standard').splitlines()
    whitespace=[]
    for name in new_paths:
        path=Path(name)
        if path.suffix in ('.py','.cs','.md','.json','.meta'):
            for i,line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(),1):
                if line.rstrip()!=line:whitespace.append(f'{name}:{i}')
    report={'branch':git('branch','--show-current'),'starting_head':baseline['starting_head'],'ending_head':git('rev-parse','HEAD'),
            'fetched_origin_master_at_start':baseline['origin_master'],'preservation':findings,
            'working_tree_status':git('status','--short','--untracked-files=all'),
            'git_diff_check_exit':check.returncode,'git_diff_check_output':check.stdout+check.stderr,
            'untracked_trailing_whitespace':whitespace}
    report['passed']=report['branch']=='master' and report['starting_head']==report['ending_head'] and all(not r['changed'] for r in findings) and check.returncode==0 and not whitespace
    (args.evidence/'preservation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n')
    manifest=[]
    for root in (args.evidence,Path('Assets/SilverScreen/Art/Characters/FemaleBlinkPrototype')):
        for path in sorted(root.rglob('*')):
            if path.is_file() and path.name!='evidence-manifest.json' and 'RelayLogs' not in path.parts:
                manifest.append({'path':path.as_posix(),'bytes':path.stat().st_size,'sha256':digest(path)})
    root_meta=Path('Assets/SilverScreen/Art/Characters/FemaleBlinkPrototype.meta')
    if root_meta.is_file():
        manifest.append({'path':root_meta.as_posix(),'bytes':root_meta.stat().st_size,'sha256':digest(root_meta)})
    (args.evidence/'evidence-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps(report,indent=2))
    assert report['passed'], 'Preservation or repository check failed; inspect the report.'


if __name__=='__main__':
    main()
