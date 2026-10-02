"""Copy the existing bald derivative and add source delta + authored correction.

The transfer arithmetic is the previously proven bake_female_blink method.
Correction input is separately recorded world-space authored displacement.
Purchased inputs and previous comparison artifacts are never saved over.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from bake_female_blink import evaluated, fingerprint, skeleton_fingerprint, canonical


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--bald-baseline', type=Path, required=True)
    parser.add_argument('--source-evidence', type=Path, required=True)
    parser.add_argument('--correction', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    assert json.loads((args.source_evidence/'reproduction-gate.json').read_text())['passed']
    args.output.mkdir(parents=True, exist_ok=True)
    before = (args.bald_baseline.stat().st_mtime_ns, hashlib.sha256(args.bald_baseline.read_bytes()).hexdigest())
    bpy.ops.wm.open_mainfile(filepath=str(args.bald_baseline.resolve()), load_ui=False, use_scripts=False)
    baseline = bpy.data.objects['AdultFemaleBald']
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    assert len(arm.data.bones) == 51
    assert len(baseline.data.vertices) == 16497
    assert len(baseline.data.shape_keys.key_blocks) == 30
    source_neutral = np.load(args.source_evidence/'neutral.npz')
    source_closed = np.load(args.source_evidence/'clean.npz')['points']
    neutral = source_neutral['points'][:16497]
    source_delta = source_closed[:16497]-neutral
    source_faces = json.loads((args.source_evidence/'topology.json').read_text())
    retained_faces = [f for f in source_faces if max(f)<16497]
    faces = [list(p.vertices) for p in baseline.data.polygons]
    assert [canonical(p) for p in faces] == [canonical(p) for p in retained_faces]
    original = fingerprint(baseline,30)
    skeleton = skeleton_fingerprint(arm)
    basis_world = evaluated(baseline)
    candidate = baseline.copy()
    candidate.data = baseline.data.copy()
    candidate.name = 'FemaleBaldBlinkCorrective_Experimental'
    bpy.context.scene.collection.objects.link(candidate)
    candidate.hide_set(False)
    key = candidate.shape_key_add(name='BlinkBoth',from_mix=False)
    basis = candidate.data.shape_keys.key_blocks[0]
    inverse = np.array(candidate.matrix_world.to_3x3().inverted())
    local_delta = source_delta @ inverse.T
    for i,v in enumerate(key.data):
        v.co = basis.data[i].co + Vector(local_delta[i])
    key.value = 1
    uncorrected = evaluated(candidate)
    transfer_error = np.linalg.norm((uncorrected-basis_world)-source_delta,axis=1)
    assert transfer_error.max() < .0000001
    correction = np.zeros_like(source_delta)
    if args.correction:
        data = np.load(args.correction)
        assert np.array_equal(data['baseline'],uncorrected), 'Correction belongs to different baked geometry.'
        correction = data['delta']
        assert correction.shape == source_delta.shape and np.isfinite(correction).all()
        assert not np.any(correction[12937:]), 'Corrective must not alter eyes, oral pieces or other objects.'
        authored_local = correction @ inverse.T
        for i,v in enumerate(key.data):
            v.co = basis.data[i].co + Vector(local_delta[i]+authored_local[i])
    corrected = evaluated(candidate)
    key.value = 0
    restored = evaluated(candidate)
    assert np.array_equal(restored,basis_world)
    assert fingerprint(candidate,30) == original
    assert skeleton_fingerprint(arm) == skeleton
    moved = np.flatnonzero(np.linalg.norm(correction,axis=1)>1e-12)
    expected = basis_world+source_delta+correction
    report = {'source_delta_method':'Unchanged evaluated source closed minus source neutral, transported to unchanged baseline Basis.',
              'bald_baseline':str(args.bald_baseline),'bald_baseline_sha256':before[1],
              'source_delta_max_error_mm':float(transfer_error.max()*1000),
              'corrected_evaluation_max_error_mm':float(np.linalg.norm(corrected-expected,axis=1).max()*1000),
              'neutral_reset_max_mm':float(np.linalg.norm(restored-basis_world,axis=1).max()*1000),
              'baseline_hashes':original,'candidate_existing_data_hashes':fingerprint(candidate,30),
              'skeleton_hash':skeleton,'candidate_skeleton_hash':skeleton_fingerprint(arm),'bones':len(arm.data.bones),
              'vertices':len(basis_world),'polygons':len(faces),'shape_names':[k.name for k in candidate.data.shape_keys.key_blocks][1:],
              'authored_correction_input':str(args.correction) if args.correction else None,
              'corrected_vertex_indices':moved.tolist(),'corrected_vertices':len(moved),
              'corrective_max_mm':float(np.linalg.norm(correction,axis=1).max()*1000),
              'input_unchanged':False,'acceptance':'Pending independent geometry and visual gates.'}
    np.savez_compressed(args.output/'neutral.npz',points=basis_world,
                        lids_L=source_neutral['lids_L'],lids_R=source_neutral['lids_R'])
    np.savez_compressed(args.output/'uncorrected.npz',points=uncorrected)
    np.savez_compressed(args.output/'corrected.npz',points=corrected)
    np.savez_compressed(args.output/'deformation.npz',basis=basis_world,source_delta=source_delta,
                        correction_delta=correction,restored=restored)
    (args.output/'topology.json').write_text(json.dumps(faces))
    (args.output/'render-settings.json').write_text((args.source_evidence/'render-settings.json').read_text())
    for obj in bpy.data.objects:
        if obj.type=='MESH':
            obj.hide_render = obj != candidate
            obj.hide_set(obj != candidate)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str((args.output/'FemaleBaldBlinkCorrective.blend').resolve()))
    assert (args.bald_baseline.stat().st_mtime_ns,hashlib.sha256(args.bald_baseline.read_bytes()).hexdigest()) == before
    report['input_unchanged'] = True
    (args.output/'build.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if not k.endswith('hashes') and k!='corrected_vertex_indices'},indent=2))


if __name__=='__main__':
    main()
