"""One reversible art-direction layer over the accepted authored topology.

Coordinates are metres after the baseline rest-pose bake, Blender +Y forward.
The original source, UVs, vertex order and garment construction remain intact.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[3]

def gaussian(x,centre,width): return math.exp(-((x-centre)/width)**2)
def ramp(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)

def apply_study(rig,body):
    base=[body.matrix_world@v.co for v in body.data.vertices]
    inverse=body.matrix_world.inverted()
    eye_ids={g.index:g.name for g in body.vertex_groups if g.name in ('eye_l','eye_r')}
    eye_centres={}
    for group,name in eye_ids.items():
        points=[base[v.index] for v in body.data.vertices if any(g.group==group and g.weight>.95 for g in v.groups)]
        eye_centres[name]=sum(points,Vector())/len(points)
    centre=sum(eye_centres.values(),Vector())/2
    hx,hy,ez=centre

    def sculpt(point):
        x,y,z=point;dx=x-hx;front=ramp(.015,.085,y)
        if z<1.43:return point.copy()
        # Broader cheek and jaw planes; preserve the chin, ears and neck transition.
        side=1 if dx>=0 else -1
        cheek=.0028*gaussian(abs(dx),.052,.026)*gaussian(z,ez-.040,.030)*front
        jaw=.0042*gaussian(abs(dx),.049,.032)*gaussian(z,ez-.108,.034)*ramp(-.015,.055,y)
        x+=side*(cheek+jaw)
        y+=.0018*gaussian(dx,0,.027)*gaussian(z,ez-.135,.020)*front
        # Deliberate bridge and tip, retaining the original nostril/alar topology.
        y+=.0014*gaussian(dx,0,.010)*gaussian(z,ez-.027,.026)*ramp(.1,.13,y)
        # Slightly wider apertures. The same smooth deformation affects globe,
        # lids and surrounding tissue, rather than scaling detached eyeballs.
        for eye in eye_centres.values():
            ex,ey,ze=eye
            influence=gaussian(x,ex,.026)*gaussian(z,ze,.018)*ramp(.045,.080,y)
            x+=(x-ex)*.065*influence
            z+=(z-ze)*.025*influence
            brow=gaussian(x,ex,.024)*gaussian(z,ze+.017,.009)*front
            y+=.0018*brow
        # A restrained 4.5% head-width increase, 3% height and 2.5% depth.
        # Smoothly fade through the upper neck, keeping the collar construction.
        blend=ramp(1.46,1.51,z)
        x=hx+(x-hx)*(1+.045*blend)
        y=.018+(y-.018)*(1+.025*blend)
        z=1.475+(z-1.475)*(1+.030*blend)
        # Preserve the source side part but give the swept crown a designed mass.
        z+=.0025*gaussian(x,hx-.03,.055)*gaussian(y,.035,.065)*ramp(1.68,1.73,z)
        return Vector((x,y,z))

    body.shape_key_add(name='Basis')
    study=body.shape_key_add(name='SilverScreen_ArtStudy01');study.value=1
    for point,vertex in zip(base,study.data):vertex.co=inverse@sculpt(point)
    # Joint landmarks receive the same spatial edit as the surface. This is a
    # neutral-pose art study; expression/animation approval remains out of scope.
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    inv=rig.matrix_world.inverted()
    for bone in rig.data.edit_bones:
        head=rig.matrix_world@bone.head;tail=rig.matrix_world@bone.tail
        if head.z>1.43 or tail.z>1.43:
            bone.head=inv@sculpt(head);bone.tail=inv@sculpt(tail)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Materials are classified on the unmodified authored surface. No texture
    # pixels are repainted or resampled here: filtering is an editable PBR input.
    original=body.data.materials[0];eye_material=body.data.materials[1]
    materials=[]
    for name in ('Skin','Hair','Cloth','Leather'):
        mat=original.copy();mat.name='SS_Study_'+name;materials.append(mat)
    materials.append(eye_material)
    image=bpy.data.images.get('rp_eric_rigged_001_dif.jpg')
    import numpy as np
    pixels=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32);image.pixels.foreach_get(pixels)
    pixels=pixels.reshape(image.size[1],image.size[0],4)
    uv=body.data.uv_layers.active.data
    def rgb_at(poly):
        u,v=sum((uv[i].uv for i in poly.loop_indices),Vector((0,0)))/len(poly.loop_indices)
        return pixels[min(image.size[1]-1,int(v*image.size[1])),min(image.size[0]-1,int(u*image.size[0])),:3]
    counts={m.name:0 for m in materials};indices=[]
    for polygon in body.data.polygons:
        point=sum((base[i] for i in polygon.vertices),Vector())/len(polygon.vertices);x,y,z=point
        if polygon.material_index==1:index=4
        elif z<.125:index=3
        elif z>1.475:
            color=rgb_at(polygon)
            cap=z>1.685 or (z>1.625 and abs(x-hx)>.065 and y<.06) or (y<.015 and z>1.585)
            index=1 if cap and max(color)<.48 else 0
        elif .59<z<.88 and abs(x)>.23:index=0
        else:index=2
        indices.append(index);counts[materials[index].name]+=1
    body.data.materials.clear()
    for material in materials:body.data.materials.append(material)
    for polygon,index in zip(body.data.polygons,indices):polygon.material_index=index

    # Locate facial features on the original UV atlas for selective preservation.
    def feature_uv(target):
        near=sorted(range(len(base)),key=lambda i:(base[i]-target).length_squared)[:12]
        loops=[i for p in body.data.polygons for i in p.loop_indices if body.data.loops[i].vertex_index in near]
        # Face island lives in the lower-right atlas area; exclude eye/mouth interiors.
        coords=[uv[i].uv for i in loops if uv[i].uv.x>.55 and uv[i].uv.y<.36]
        return list(sum(coords,Vector((0,0)))/len(coords)) if coords else [0,0]
    features={name:feature_uv(Vector((eye.x,eye.y+.022,eye.z+.008))) for name,eye in eye_centres.items()}
    features['mouth']=feature_uv(Vector((hx,.128,ez-.080)))
    features['nose']=feature_uv(Vector((hx,.15,ez-.037)))
    deltas=[(sculpt(point)-point).length for point in base]
    report={'study':'SilverScreen_ArtStudy01','source_vertex_count':len(base),'topology_changed':False,'uv_changed':False,
        'head_width_scale':1.045,'head_height_scale':1.03,'head_depth_scale':1.025,
        'max_displacement_m':max(deltas),'changed_vertices':sum(d>.000001 for d in deltas),
        'eye_centres_m':{k:list(v) for k,v in eye_centres.items()},'feature_uv':features,'material_faces':counts,
        'rendered_height_m':max(sculpt(p).z for p in base)-min(sculpt(p).z for p in base),
        'status':'one restrained study; visual approval pending; no animation approval'}
    (ROOT/'ArtSource/Characters/Canonical/stylization_report.json').write_text(json.dumps(report,indent=2))
    return report
