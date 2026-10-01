"""Read-only source regression checks for defects found during manual close inspection."""
import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/PeriodEnvironment1930/PeriodEnvironment1930.blend'))
for c in bpy.context.view_layer.layer_collection.children:c.exclude=False
bpy.context.view_layer.update()
report={}
for name in ['FenceGateWire_180_1930','FenceGateWire_180_RH_1930']:
    objects=bpy.data.collections[name].objects
    points=[o.matrix_world@v.co for o in objects if o.type=='MESH' for v in o.data.vertices]
    size=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
    assert 1.85<size[0]<2.05 and size[1]<.35 and 2.0<size[2]<2.06,(name,size)
    report[name]={'sourceBoundsMetres':size,'humanScaleHardware':True}
cap=next(o for o in bpy.data.collections['FenceGateWire_180_RH_1930'].objects if o.name.startswith('Thumb lever rounded end'))
assert max(cap.dimensions)<.03,tuple(cap.dimensions)
report['rightGateLatchCapMetres']=list(cap.dimensions)

gutter=bpy.data.collections['GutterHalfRound_040_1930']
tested=[]
for o in gutter.objects:
    if o.type!='MESH':continue
    tree=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
    origin=o.matrix_world.inverted()@Vector((0,0,.1))
    direction=o.matrix_world.inverted().to_3x3()@Vector((0,0,-1))
    hit=tree.ray_cast(origin,direction,.5)
    assert hit[0] is None,(o.name,'Drain aperture obstructed')
    tested.append(o.name)
report['outletCentreOpenThroughTroughAndThroat']=True
report['outletObjectsChecked']=tested
lamp=bpy.data.collections['DeskLampBanker_1930']
assert len([o for o in lamp.objects if o.name.startswith('Closed moulded glass shade end')])==2
assert len([o for o in lamp.objects if o.name.startswith('White inner end cheek')])==2
assert any(o.name.startswith('Frosted pear bulb') for o in lamp.objects)
outer_end=next(o for o in lamp.objects if o.name.startswith('Closed moulded glass shade end'))
inner_end=next(o for o in lamp.objects if o.name.startswith('White inner end cheek'))
assert max(v.co.z for v in inner_end.data.vertices)<max(v.co.z for v in outer_end.data.vertices),'White lining protrudes beyond green glass'
report['lampClosedEndsAndSeparateInnerGlass']=True
path=root/'ArtReview/StudioServices/FinalQA/source-regressions.json'
path.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FINAL_QA_SOURCE_REGRESSIONS',json.dumps(report))
