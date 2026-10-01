"""Compact A4 planter detailing; measured budget includes plant, pot and tray."""
import bpy, math, random, struct, zlib
import numpy as np
from pathlib import Path
from mathutils import Quaternion, Vector
from mathutils.bvhtree import BVHTree
import geometry as G

MAX_TRIANGLES = 6500
SOIL_Z = .630
EXPOSED_LEAF_Z = .775
REPORT = []


def soil_material(root):
    """Small shared albedo: quiet, isotropic soil variation, no dirt meshes."""
    size = 128
    rng = np.random.default_rng(193004)
    noise = np.zeros((size, size))
    for grid, weight in [(5, .60), (13, .28), (39, .12)]:
        lattice = rng.uniform(-1, 1, (grid + 1, grid + 1))
        axis = np.arange(size) * grid / size
        ix = axis.astype(int); f = axis - ix; f = f * f * (3 - 2 * f)
        a = lattice[ix[:, None], ix[None, :]]
        b = lattice[ix[:, None] + 1, ix[None, :]]
        c = lattice[ix[:, None], ix[None, :] + 1]
        d = lattice[ix[:, None] + 1, ix[None, :] + 1]
        noise += weight * ((a * (1-f[:, None]) + b*f[:, None])*(1-f[None, :])
                           + (c*(1-f[:, None]) + d*f[:, None])*f[None, :])
    rgb = np.array([.032, .019, .010])[None, None, :] * (1 + .16*noise[:, :, None])
    srgb = np.where(rgb <= .0031308, rgb*12.92, 1.055*rgb**(1/2.4)-.055)
    pixels = np.rint(srgb*255).astype(np.uint8)
    def chunk(kind, data):
        return struct.pack('>I', len(data))+kind+data+struct.pack('>I', zlib.crc32(kind+data)&0xffffffff)
    path = root/'ArtExports/StageSchoolA4/Textures/PottingSoil_albedo.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    raw = b''.join(b'\0'+row.tobytes() for row in pixels)
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('>IIBBBBB',size,size,8,2,0,0,0))
                     + chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
    mat = G.MATS['SS_A4_PottingSoil']
    tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(path), check_existing=False)
    tex.image.colorspace_settings.name = 'sRGB'; tex.image.pack()
    mat.node_tree.links.new(tex.outputs['Color'], mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])


def lathe(name, center, profile, material, segments=40):
    vertices = [(center[0]+r*math.cos(i*math.tau/segments),
                 center[1]+r*math.sin(i*math.tau/segments), z)
                for r,z in profile for i in range(segments)]
    # Profile is closed explicitly and its seam shares vertices.
    faces = [(k*segments+i,k*segments+(i+1)%segments,
              ((k+1)%len(profile))*segments+(i+1)%segments,
              ((k+1)%len(profile))*segments+i)
             for k in range(len(profile)) for i in range(segments)]
    obj = G.mesh(name, vertices, faces, material)
    for poly in obj.data.polygons: poly.use_smooth = True
    return obj


def leaf_geometry(xx, angle, tier, attempt, rng):
    direction = Vector((math.cos(angle), math.sin(angle), 0))
    across = Vector((-direction.y, direction.x, 0))
    if tier == 0:
        reach=rng.uniform(.43,.55); rise=rng.uniform(.50,.64)
        arch=rng.uniform(1.94,2.16); width=rng.uniform(.068,.091)
    elif tier == 1:
        reach=rng.uniform(.27,.38); rise=rng.uniform(.71,.87)
        arch=rng.uniform(1.43,1.67); width=rng.uniform(.059,.079)
    else:
        reach=rng.uniform(.14,.21); rise=rng.uniform(.78,.88)
        arch=rng.uniform(1.22,1.43); width=rng.uniform(.055,.070)
    # Modest fallback shaping avoids forcing intersecting blades into the crown.
    width *= max(.76, 1-attempt*.012)
    if direction.y > .25: width *= .86
    root = direction * (.033 if tier == 0 else .018)
    root.z = SOIL_Z+.017
    roll=rng.choice([-1,1])*rng.uniform(.22,.58); bend=rng.uniform(-.018,.018)
    phase=rng.uniform(0,math.tau)
    # Redistribute the existing leaf budget over a fuller eighteen-leaf crown.
    # Ten length segments preserve the bends without separate vein geometry.
    times=[0,.08,.18,.30,.43,.56,.68,.79,.88,.95,1]
    cross=[-1,-.35,0,.35,1]
    vertices=[];uv=[]
    for underside in [False,True]:
        for t in times:
            center=root+direction*(reach*math.sin(t*math.pi/2)**1.12)
            center+=across*(bend*math.sin(math.pi*t))
            center.z+=rise*math.sin(t*arch)
            blade_t=max(0,(t-.08)/.92)
            blade_width=.001+.004*(1-t)+width*math.sin(math.pi*blade_t)**1.12
            radial_slope=reach*1.12*max(.001,math.sin(t*math.pi/2))**.12*math.cos(t*math.pi/2)*math.pi/2
            tangent=direction*radial_slope+Vector((0,0,rise*arch*math.cos(t*arch)))
            cross_direction=Quaternion(tangent.normalized(),roll*math.sin(.65*math.pi*t))@across
            leaf_normal=tangent.cross(cross_direction).normalized()
            for s in cross:
                v=center+cross_direction*(s*blade_width*(1+.045*s*math.sin(phase)))
                # Integrated midrib/fold replaces a separate vein tube per leaf.
                v+=leaf_normal*(.0045*math.sin(math.pi*blade_t)*(1-abs(s)))
                v+=leaf_normal*(.0025*s*s*math.sin(4*math.pi*t+phase)*math.sin(math.pi*t))
                if underside:v-=leaf_normal*.0010
                # A compact rear canopy and clear entrance-side envelope.
                if v.y>0:v.y=.19*math.tanh(v.y/.19)
                if abs(xx)<5 and xx*v.x<0:v.x=math.copysign(.32*math.tanh(abs(v.x)/.32),v.x)
                v.x+=xx;v.y-=9.35
                vertices.append(v);uv.append(((s+1)/2,t))
    stride=len(cross); rows=len(times); sheet=rows*stride; faces=[]
    for layer in range(2):
        for k in range(rows-1):
            for q in range(stride-1):
                a=layer*sheet+k*stride+q
                faces.append((a,a+1,a+stride+1,a+stride))
    boundary=list(range(stride))+[k*stride+stride-1 for k in range(1,rows)]+list(range(sheet-2,sheet-stride-1,-1))+[k*stride for k in range(rows-2,0,-1)]
    for i,a in enumerate(boundary):
        b=boundary[(i+1)%len(boundary)];faces.append((a,b,b+sheet,a+sheet))
    exposed=[f for f in faces if min(vertices[i].z for i in f)>=EXPOSED_LEAF_Z]
    tree=BVHTree.FromPolygons(vertices,exposed,all_triangles=False,epsilon=.0003)
    return vertices,faces,uv,tree


def build_planter(xx, seed):
    collection=G.GROUPS[G.CURRENT]
    before=set(collection.objects)
    rng=random.Random(seed)
    center=(xx,-9.35)
    # Saucer floor supports the pot at 79 mm; its lip is only 32 mm higher.
    lathe('Shallow drainage saucer',center,
          [(.185,.052),(.315,.052),(.342,.060),(.350,.074),(.350,.099),
           (.341,.111),(.330,.107),(.328,.092),(.312,.079),(.185,.079)],'SS_Stone')
    # Defined rolled rim with a restrained shadow line below the bead.
    lathe('Tapered pot with rolled rim',center,
          [(.195,.079),(.220,.088),(.227,.145),(.289,.593),(.296,.616),
           (.315,.629),(.326,.644),(.328,.659),(.320,.675),(.305,.684),
           (.286,.679),(.273,.665),(.272,.644),(.263,.614),(.208,.128),(.195,.107)],'SS_Stone')
    # A simple closed disk; fine variation lives entirely in the shared texture.
    soil=G.cylinder('Soil with subtle material variation',(xx,-9.35,SOIL_Z-.009),.264,.018,'SS_A4_PottingSoil',segments=40)
    uv=soil.data.uv_layers.active
    for loop in soil.data.loops:
        p=soil.matrix_world@soil.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((p.x-xx)/.528+.5,(p.y+9.35)/.528+.5)
    soil['explicit_uv']=True
    # Three low basal sheaths read as a living clustered crown, not wire stems.
    for dx,dy,height in [(-.023,-.018,.146),(.024,-.013,.129),(0,.025,.160)]:
        lathe('Clustered basal leaf sheath',(xx+dx,-9.35+dy),
              [(.012,SOIL_Z-.012),(.031,SOIL_Z+.012),(.029,SOIL_Z+.055),
               (.018,SOIL_Z+height*.73),(.0015,SOIL_Z+height)],
              'SS_A4_LeafDeep',segments=8)
    phase=rng.uniform(-.15,.15)
    layouts=[(0,i*math.tau/10+phase) for i in range(10)]
    layouts += [(1,i*math.tau/6+phase+.34) for i in range(6)]
    layouts += [(2,phase+1.13),(2,phase+4.17)]
    trees=[];attempts=[];leaves=[]
    for j,(tier,nominal) in enumerate(layouts):
        for attempt in range(80):
            angle=nominal+rng.uniform(-.10,.10)*(1+min(attempt,20)/8)
            vertices,faces,uvs,tree=leaf_geometry(xx,angle,tier,attempt,rng)
            if any(tree.overlap(other) for other in trees):continue
            material=['SS_A4_LeafDeep','SS_A4_LeafGreen','SS_A4_LeafYoung'][(j%2) if tier==0 else (1+j%2)]
            leaf=G.mesh('Crown-grown curved leaf',vertices,faces,material)
            for poly in leaf.data.polygons:poly.use_smooth=True
            uv=leaf.data.uv_layers.new(name='LeafUV')
            for loop in leaf.data.loops:uv.data[loop.index].uv=uvs[loop.vertex_index]
            leaf['explicit_uv']=True
            trees.append(tree);attempts.append(attempt+1);leaves.append(leaf)
            break
        else:raise RuntimeError('Cannot place non-intersecting leaf %s in planter %s'%(j,xx))
    objects=[o for o in collection.objects if o not in before and o.type=='MESH']
    count=0;by_kind={}
    for o in objects:
        o.data.calc_loop_triangles();n=len(o.data.loop_triangles);count+=n
        kind='foliage' if o in leaves else 'crown' if o.name.startswith('Clustered') else 'pot_and_saucer' if 'pot' in o.name.lower() or 'saucer' in o.name else 'soil'
        by_kind[kind]=by_kind.get(kind,0)+n
        o['a4_planter_x']=xx
    assert count<=MAX_TRIANGLES,(xx,count,MAX_TRIANGLES)
    intersections=sum(bool(a.overlap(b)) for i,a in enumerate(trees) for b in trees[i+1:])
    assert intersections==0
    REPORT.append(dict(x=xx,leaves=len(leaves),triangles=count,triangle_budget=MAX_TRIANGLES,
                       breakdown=by_kind,exposed_leaf_intersections=intersections,
                       intersection_test_min_height=EXPOSED_LEAF_Z,placement_attempts=attempts,
                       intentional_overlap='Basal leaf sheaths overlap below the exposed canopy as a clustered crown.'))
