"""Reusable individual cast-letter meshes; never exports STAGE 1 as a word/model."""
import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
font=bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
glyphs=[]
for character in 'AEGST0123456789':
    curve=bpy.data.curves.new('Glyph_'+character,'FONT');curve.body=character;curve.font=font
    curve.size=1;curve.extrude=.012;curve.bevel_depth=.0015;curve.bevel_resolution=1;curve.resolution_u=6
    obj=bpy.data.objects.new('Glyph_'+character,curve);bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.convert(target='MESH');obj=bpy.context.object
    mesh=obj.data;mesh.calc_loop_triangles()
    minx=min(v.co.x for v in mesh.vertices);miny=min(v.co.y for v in mesh.vertices);maxy=max(v.co.y for v in mesh.vertices)
    h=maxy-miny
    # Unity coordinates: X right, Y up, front towards -Z; reverse triangle winding.
    vertices=[[float((v.co.x-minx)/h*.76),float((v.co.y-miny)/h),float(-v.co.z/h)] for v in mesh.vertices]
    triangles=[i for t in mesh.loop_triangles for i in reversed(t.vertices)]
    glyphs.append({'character':character,'vertices':[x for v in vertices for x in v],'triangles':triangles})
    obj.select_set(False)
(root/'stage_glyphs.json').write_text(json.dumps({'glyphs':glyphs}))
print('Exported 15 individual industrial glyphs, no baked stage identity.')
