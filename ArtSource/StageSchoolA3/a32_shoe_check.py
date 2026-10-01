exec(compile(open('ArtSource/StageSchoolA3/a32_local_geometry.py').read().split('checks=[]')[0],'probe','exec'))
hits=[]
for roof,lower in [('Hall','Preparation'),('Audition','Interview')]:
 for o in bpy.data.collections['Roofs/'+roof+'/Rainwater'].objects:
  if not o.name.startswith('Supported upper-roof collector shoe'):continue
  vs=[v.co for v in o.data.vertices]
  if roof=='Hall' and max(v.x for v in vs)<0:continue
  t=tree(o)
  for target in bpy.data.collections['Roofs/'+lower].objects:
   if target.type!='MESH' or not len(target.data.polygons):continue
   overlaps=t.overlap(tree(target))
   if overlaps:
    hits.append([roof,o.name,target.name,len(overlaps)])
    print('SHOE INTERSECTION',*hits[-1])
assert not hits,hits
print('TARGETED SHOE CHECK COMPLETE')
