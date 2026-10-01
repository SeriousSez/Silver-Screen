"""Targeted verification of perimeter-led rainwater after the user's simplification."""
import bpy,json
from pathlib import Path
R=Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
r=json.loads((R/'ArtSource/StageSchoolA3/generation_report.json').read_text())
parts=[p for p in r['parts'] if p['name'].startswith('Roofs/') and p['name'].endswith('/Rainwater')]
objects=[o for c in bpy.data.collections if c.name.startswith('Roofs/') and c.name.endswith('/Rainwater') for o in c.objects]
unwanted=[o.name for o in objects if o.name.startswith(('Supported upper-roof collector shoe','Collector discharge spreader apron','Upper shoe fascia stay'))]
assert not unwanted,unwanted
assert len(r['roofs'])==9
assert len(r['drains'])==7, 'Seven consolidated side/rear outlets'
assert not any(v['roof']=='Entrance' or v['top'][1]<-9 for v in r['drains']), 'No front-facing ground drops'
assert set(v['roof'] for v in r['qa']['a31']['roof_routes'])==set(v[0] for v in r['roofs'])
before=json.loads((R/'ArtReview/StageSchoolA1/a32_before_drainage_simplification.json').read_text(encoding='utf-8-sig'))
tri=sum(v['triangles'] for v in parts)
assert tri<before['rainwaterTriangles']
out=dict(roof_masses=9,exposed_internal_cascade_components=unwanted,recessed_liners=sum(o.name.startswith('Recessed internal') for o in objects),visible_perimeter_downpipes=len(r['drains']),rainwater_triangles_before=before['rainwaterTriangles'],rainwater_triangles_after=tri,rainwater_triangles_removed=before['rainwaterTriangles']-tri,conceptual_routes=r['qa']['a31']['roof_routes'])
(R/'ArtReview/StageSchoolA1/a32_simplified_drainage_qa.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
