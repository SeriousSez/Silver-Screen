import bpy,bmesh,json,hashlib
from pathlib import Path
root=Path.cwd()
p=root/'ArtSource/Stage1CleanCandidate/Stage1_CleanCandidate_A.blend'
bpy.ops.wm.open_mainfile(filepath=str(p))
issues=[];meshes=0;volumes=[]
for obj in bpy.data.objects:
 if obj.type!='MESH':continue
 meshes+=1;bm=bmesh.new();bm.from_mesh(obj.data)
 nonmanifold=sum(1 for e in bm.edges if not e.is_manifold)
 volume=bm.calc_volume(signed=True)
 if nonmanifold or volume<=0:issues.append({'name':obj.name,'nonmanifold_edges':nonmanifold,'signed_volume':volume})
 volumes.append(volume);bm.free()
a=root/'ArtExports/Stage1CleanCandidate/Stage1_CleanCandidate_A.fbx'
b=root/'Assets/SilverScreen/Environment/Stage1CleanCandidate/Models/Stage1_CleanCandidate_A.fbx'
report={'mesh_count':meshes,'issues':issues,'all_closed_and_outward':not issues,'min_signed_volume':min(volumes),'export_and_unity_copy_sha256_equal':hashlib.sha256(a.read_bytes()).hexdigest()==hashlib.sha256(b.read_bytes()).hexdigest(),'metres_per_unit':bpy.context.scene.unit_settings.scale_length}
(root/'ArtReview/Stage1CleanCandidate/source_validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
