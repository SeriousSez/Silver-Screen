import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
source=ROOT/'ArtSource/Characters/ThirdParty/BlenderHumanBaseMeshes/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend'
bpy.ops.wm.open_mainfile(filepath=str(source),use_scripts=False)
out=ROOT/'ArtReview/Characters/Canonical';out.mkdir(parents=True,exist_ok=True)
def asset(x):
    a=x.asset_data
    return dict(name=x.name,author=a.author,description=a.description,license=a.license,copyright=a.copyright) if a else {'name':x.name}
report={'texts':{t.name:t.as_string() for t in bpy.data.texts},'collections':[dict(**asset(c),objects=[x.name for x in c.all_objects]) for c in bpy.data.collections],
        'objects':[dict(**asset(o),type=o.type,dimensions=list(o.dimensions),location=list(o.location),rotation=list(o.rotation_euler),scale=list(o.scale),
          vertices=len(o.data.vertices) if o.type=='MESH' else 0,polygons=len(o.data.polygons) if o.type=='MESH' else 0,
          groups=[g.name for g in o.vertex_groups] if o.type=='MESH' else [],modifiers=[dict(name=m.name,type=m.type) for m in o.modifiers],
          materials=[s.material.name if s.material else None for s in o.material_slots]) for o in bpy.data.objects]}
(out/'authored-base-inspection.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'collections':report['collections'],'texts':report['texts']},indent=2))
