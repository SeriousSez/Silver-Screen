"""Sample Girl's approved control trajectory without changing its endpoint.

The first linear transfer exposed the eyeball at partial closure. These evaluated
in-betweens preserve the source arc in the same runtime BlinkBoth channel.
"""
import argparse,hashlib,json,sys
from pathlib import Path
import bpy,numpy as np
from mathutils import Vector
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from inspect_male_foundation import expose
p=argparse.ArgumentParser();p.add_argument('--evidence',type=Path,required=True);p.add_argument('--acceptance',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True)
c=json.loads((a.evidence/'calibration.json').read_text());accept=json.loads(a.acceptance.read_text())
assert accept['accepted'] and accept['candidate']=='ReviewG2' and accept['endpoint_sha256']==hashlib.sha256((a.evidence/'ReviewG2.npz').read_bytes()).hexdigest()
path=Path(c['source']);stamp=(path.stat().st_mtime_ns,hashlib.sha256(path.read_bytes()).hexdigest());assert stamp[1]==c['sha256']
bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False);expose();obj=bpy.data.objects['GirlBaseMesh_Facial_Rig'];arm=obj.find_armature()
rest={b.name:b.matrix_basis.copy() for b in arm.pose.bones};controls=c['candidates']['ReviewG2']['controls_z_mm'];samples={}
def reset():
 for name,matrix in rest.items():arm.pose.bones[name].matrix_basis=matrix
 bpy.context.view_layer.update()
def sample():
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();e=obj.evaluated_get(dg);m=e.to_mesh();points=np.array([e.matrix_world@v.co for v in m.vertices]);e.to_mesh_clear();assert np.isfinite(points).all();return points
for weight in np.arange(0,100.01,2.5):
 reset()
 for name,mm in controls.items():
  b=arm.pose.bones[name];b.location+=b.bone.matrix_local.to_3x3().inverted()@Vector((0,0,mm*weight/100000))
 samples[f'p{weight:g}']=sample()
assert np.array_equal(samples['p0'],np.load(a.evidence/'neutral.npz')['points'])
assert np.array_equal(samples['p100'],np.load(a.evidence/'ReviewG2.npz')['points'])
assert np.array_equal(samples['p50'],np.load(a.evidence/'partial50.npz')['points'])
reset();assert np.array_equal(sample(),samples['p0'])
invalid=[d.data_path for d in arm.animation_data.drivers if not d.driver.is_valid];assert not invalid
np.savez_compressed(a.output/'source-trajectory.npz',**samples)
report=dict(endpoint_exact=True,source_half_exact=True,neutral_reset_exact=True,invalid_drivers=invalid,controls_unchanged=controls,weights=list(np.arange(0,100.01,2.5)),source_unchanged=stamp==(path.stat().st_mtime_ns,hashlib.sha256(path.read_bytes()).hexdigest()),method='Original approved source controls scaled uniformly, full dependency graph. No sculpt or endpoint refinement.')
assert report['source_unchanged'];(a.output/'sampling.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
