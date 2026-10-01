import bpy, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/'ArtSource/Characters/ThirdParty/RenderpeopleEric/Original'
REVIEW=ROOT/'ArtReview/Characters/Canonical'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(SOURCE/'rp_eric_rigged_001_zup_a.fbx'))
items=[]
for ob in bpy.context.scene.objects:
    item={'name':ob.name,'type':ob.type,'location':list(ob.location),'rotation':list(ob.rotation_euler),'scale':list(ob.scale),'dimensions':list(ob.dimensions)}
    if ob.type=='MESH':
        item.update(vertices=len(ob.data.vertices),polygons=len(ob.data.polygons),materials=[m.name for m in ob.data.materials],modifiers=[(m.name,m.type) for m in ob.modifiers],groups=[g.name for g in ob.vertex_groups])
    if ob.type=='ARMATURE': item['bones']=[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local)} for b in ob.data.bones]
    items.append(item)
print(json.dumps(items,indent=2))
(REVIEW/'tailored-source-inspection.json').write_text(json.dumps(items,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Characters/Canonical/TailoredSource_Inspection.blend'))
