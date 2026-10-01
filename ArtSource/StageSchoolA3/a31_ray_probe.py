import bpy,math
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='ArtSource/StageSchoolA3/StageSchool_A3.blend')
p=Vector((0,3.8,2));f=(Vector((0,-2.8,4.7))-p).normalized();right=f.cross(Vector((0,0,1))).normalized();up=right.cross(f)
for x,y in [(460,510),(520,550),(620,590),(475,490),(1370,295)]:
 d=(f+right*((x/1920*2-1)*1.6)+up*(1-y/1200*2)).normalized();hit,loc,n,idx,obj,m=bpy.context.scene.ray_cast(bpy.context.evaluated_depsgraph_get(),p,d)
 print('PIXEL',x,y,hit,tuple(loc),obj.name if obj else None, [c.name for c in obj.users_collection] if obj else [])
