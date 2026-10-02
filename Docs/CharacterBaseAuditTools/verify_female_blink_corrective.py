"""Preservation checks for the isolated failed/review-only corrective experiment.

These checks do not assert artistic acceptance and must never unlock Unity.
Run with host Python (numpy); evidence comes from actual evaluated Blender data.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

import numpy as np


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--evidence',type=Path,required=True)
    p.add_argument('--authoring',type=Path,required=True)
    p.add_argument('--snapshot',type=Path,required=True)
    p.add_argument('--inventory',type=Path,required=True)
    p.add_argument('--source-root',type=Path,required=True)
    args=p.parse_args()
    root=args.evidence
    build=json.loads((root/'build.json').read_text())
    authored=json.loads(args.authoring.read_text())
    snapshot=json.loads(args.snapshot.read_text())
    arrays=np.load(root/'deformation.npz')
    neutral=np.load(root/'neutral.npz')['points']
    uncorrected=np.load(root/'uncorrected.npz')['points']
    corrected=np.load(root/'corrected.npz')['points']
    ids=np.array(sorted(map(int,authored['vertex_displacements_mm'])))
    delta=arrays['correction_delta']
    outside=np.setdiff1d(np.arange(len(neutral)),ids)
    tests={
        'finite_geometry':all(np.isfinite(a).all() for a in [neutral,uncorrected,corrected,delta]),
        'neutral_exact':np.array_equal(neutral,arrays['restored']),
        'original_data_fingerprints_exact':build['baseline_hashes']==build['candidate_existing_data_hashes'],
        'skeleton_exact':build['skeleton_hash']==build['candidate_skeleton_hash'] and build['bones']==51,
        '29_original_shapes_plus_only_BlinkBoth':len(build['shape_names'])==30 and build['shape_names'][-1]=='BlinkBoth',
        'source_transfer_max_error_below_0_0001mm':build['source_delta_max_error_mm']<.0001,
        'authored_transfer_max_error_below_0_0001mm':build['corrected_evaluation_max_error_mm']<.0001,
        'correction_ledger_exact':np.allclose(delta[ids]*1000,[authored['vertex_displacements_mm'][str(i)] for i in ids],rtol=0,atol=1e-12),
        'zero_authored_motion_outside_ledger':not np.any(delta[outside]),
        'evaluated_outside_correction_exact':np.array_equal(corrected[outside],uncorrected[outside]),
        'eyes_and_oral_unchanged_by_corrective':np.array_equal(corrected[12937:],uncorrected[12937:]),
        'bald_input_not_modified':build['input_unchanged']}
    preserved_failures=[]
    for item in snapshot['preserved_files']:
        path=Path(item['path'])
        if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']:
            preserved_failures.append(item['path'])
    tests['previous_audit_and_prototype_files_preserved']=not preserved_failures
    inventory=json.loads(args.inventory.read_text())
    source_failures=[]
    for item in inventory:
        path=args.source_root/item['path']
        if not path.exists():
            source_failures.append({'path':str(path),'failure':'missing'}); continue
        st=path.stat()
        if st.st_size!=item['bytes'] or st.st_mtime_ns!=item['mtime_ns'] or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']:
            source_failures.append({'path':str(path),'failure':'hash, size or mtime mismatch'})
    tests['purchased_source_hashes_sizes_and_mtimes_preserved']=not source_failures
    def git(*args):
        r=subprocess.run(['git','-c','core.fsmonitor=false',*args],capture_output=True,text=True,check=True)
        return r.stdout.strip()
    branch=git('branch','--show-current'); head=git('rev-parse','HEAD')
    tests['master_and_head_unchanged']=branch=='master' and head==snapshot['starting_head']
    report={'preservation_tests':tests,'all_preservation_tests_passed':all(tests.values()),
            'preserved_previous_files_checked':len(snapshot['preserved_files']),
            'preserved_previous_file_failures':preserved_failures,
            'source_files_checked':len(inventory),'source_failures':source_failures,
            'branch':branch,'starting_head':snapshot['starting_head'],'ending_head':head,
            'status_porcelain':git('status','--porcelain=v1','--untracked-files=all').splitlines(),
            'tracked_diff_check':git('diff','--check'),
            'acceptance':'NOT ACCEPTED: preservation passing is not visual/contact approval.'}
    (root/'preservation-check.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))
    assert all(tests.values()),'Preservation check failed; inspect the report.'


if __name__=='__main__':
    main()
