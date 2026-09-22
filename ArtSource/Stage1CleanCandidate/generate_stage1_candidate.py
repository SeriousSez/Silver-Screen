"""Stage 1 clean candidate A, defect correction/material revision 04.

Run Blender --background --factory-startup --python <this file>.
No old stage source, meshes, or material data are loaded. Metres, ground-centred
pivot, Blender front -Y, FBX -Z forward / Y up, semantic export meshes.
Unity import/review companion: Stage1CandidateReview.cs. Visual approval pending.
"""
import bpy
import bmesh
import math
import json
import shutil
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
NAME = 'Stage1_CleanCandidate_A'
OUT = ROOT / 'ArtExports/Stage1CleanCandidate'
UNITY = ROOT / 'Assets/SilverScreen/Environment/Stage1CleanCandidate/Models'
W, D, EAVE, RISE = 15.8, 24.0, 7.10, 3.75
HALF, WALL, FLOOR = W / 2, .32, .08
GROUND = 0.0  # Placement datum and lowest exported surface; no hidden subgrade slab.
STEPS = 192
RADIUS = (HALF * HALF + RISE * RISE) / (2 * RISE)
CIRCLE_Z = EAVE + RISE - RADIUS
ARC_END = math.asin(HALF / RADIUS)
DOOR_W, DOOR_H = 7.2, 6.9
SIGN_BASE = 8.00
CATWALK, TIE, GRID = 4.95, 7.32, 6.48
FRAME_Y = [-11.20, -5.60, 0, 5.60, 11.20]
# Scene-linear palette. Unity companion converts RGB to sRGB exactly once.
PALETTE = {
    'C1_Stucco': ([.57, .50, .39], 0, .86),
    'C1_Trim': ([.63, .56, .45], 0, .80),
    'C1_Base': ([.24, .235, .22], 0, .91),
    'C1_Roof': ([.13, .14, .145], .65, .55),
    'C1_Timber': ([.045, .037, .029], 0, .80),
    'C1_Steel': ([.055, .061, .061], .65, .52),
    'C1_GlassStudy': ([.095, .145, .17], .30, .24),
    'C1_Blackout': ([.023, .022, .021], 0, .98),
    'C1_Floor': ([.15, .14, .12], 0, .92),
}
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.engine = 'BLENDER_EEVEE'
bpy.context.preferences.filepaths.save_version = 0
asset = bpy.data.collections.new(NAME)
scene.collection.children.link(asset)
root = bpy.data.objects.new(NAME, None)
asset.objects.link(root)
materials = {}
groups = {}
objects = []
for name, (rgb, metal, rough) in PALETTE.items():
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = rough
    materials[name] = m

def group(name):
    if name not in groups:
        g = bpy.data.objects.new(name, None)
        asset.objects.link(g)
        g.parent = root
        groups[name] = g
    return groups[name]

def mesh(name, verts, faces, mat, category, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    o = bpy.data.objects.new(name, data)
    asset.objects.link(o)
    o.parent = group(category)
    data.materials.append(materials[mat])
    if smooth:
        for p in data.polygons: p.use_smooth = True
    objects.append(o)
    return o

def box(name, x, y, z, sx, sy, sz, mat='C1_Stucco', category='ExteriorWalls', bevel=.0):
    vs = [(x+a*sx/2,y+b*sy/2,z+c*sz/2) for a,b,c in
          [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    o = mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,category)
    if bevel:
        bpy.context.view_layer.objects.active = o
        mod = o.modifiers.new('Massing edge response','BEVEL')
        mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def arch_z(x, offset=0):
    return CIRCLE_Z + math.sqrt((RADIUS + offset)**2 - x*x)

def shell_segment(name,a,b,y0,y1,offset=0,thick=.14,mat='C1_Roof',category='Roof'):
    # Constant-radius segment, with FINITE slopes at both spring points.
    # a/b are signed angles from the vertical, not ellipse parameters.
    count=max(2,round(STEPS*(b-a)/(2*ARC_END)))
    angles=[a+(b-a)*i/count for i in range(count+1)]
    vs=[]
    for yy in [y0,y1]:
        for rr in [RADIUS+offset,RADIUS+offset-thick]:
            vs.extend([(rr*math.sin(t),yy,CIRCLE_Z+rr*math.cos(t)) for t in angles])
    n=count+1;fs=[]
    for i in range(count):
        fs += [(i,i+1,2*n+i+1,2*n+i),(n+i,3*n+i,3*n+i+1,n+i+1),
               (i,n+i,n+i+1,i+1),(2*n+i,2*n+i+1,3*n+i+1,3*n+i)]
    fs += [(0,2*n,3*n,n),(count,n+count,3*n+count,2*n+count)]
    o=mesh(name,vs,fs,mat,category)
    for i,p in enumerate(o.data.polygons):p.use_smooth=i<count*4 and i%4<2
    return o

def panel_with_voids(name,horizontal0,horizontal1,y_or_x,thickness,holes,side=False):
    # Wall cells share cut lines; actual apertures, including sills and soffits.
    xs=sorted(set([horizontal0,horizontal1]+[v for h in holes for v in h[:2]]))
    zs=sorted(set([0,1.30,EAVE]+[v for h in holes for v in h[2:]]))
    for i in range(len(xs)-1):
        for j in range(len(zs)-1):
            lo,hi=xs[i:i+2];bottom,top=zs[j:j+2]
            cx,cz=(lo+hi)/2,(bottom+top)/2
            if any(h[0]<cx<h[1] and h[2]<cz<h[3] for h in holes):continue
            mat='C1_Base' if top<=1.30 else 'C1_Stucco'
            dims=(thickness,hi-lo,top-bottom) if side else (hi-lo,thickness,top-bottom)
            pos=(y_or_x,cx,cz) if side else (cx,y_or_x,cz)
            box(name,*pos,*dims,mat)

def end_arch(name,y):
    # Solid front/rear masonry under the curve; no triangle fan at the crown.
    xs=[-HALF+W*i/STEPS for i in range(STEPS+1)]
    tops=[arch_z(x) for x in xs]
    vs=[]
    for yy in [y-WALL/2,y+WALL/2]:
        vs += [(x,yy,EAVE) for x in xs]+[(x,yy,z) for x,z in zip(xs,tops)]
    n=len(xs);fs=[]
    # End samples have zero height; omit degenerates and weld coincident vertices below.
    for i in range(STEPS):
        fs += [(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),
               (n+i,n+i+1,3*n+i+1,3*n+i),(i,2*n+i,2*n+i+1,i+1)]
    o=mesh(name,vs,fs,'C1_Stucco','ExteriorWalls')
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()

def beam(name,start,end,width,depth=None,mat='C1_Steel',category='PrimaryStructure'):
    start,end=Vector(start),Vector(end)
    direction=end-start
    o=box(name,0,0,0,width,depth or width,direction.length,mat,category)
    rotation=direction.to_track_quat('Z','Y').to_matrix()
    centre=(start+end)*.5
    for v in o.data.vertices:v.co=rotation@v.co+centre
    o.data.update()
    return o

def canopy(name,x,front_y,width,projection,z,face=-1):
    # A sloping rain-hood VOLUME, not the earlier flat horizontal shelf.
    back=front_y;edge=front_y+face*projection
    vs=[(x+xx,yy,zz) for xx,yy,zz in [(-width/2,back,z),(width/2,back,z),(width/2,edge,z-.24),(-width/2,edge,z-.24),(-width/2,back,z+.09),(width/2,back,z+.09),(width/2,edge,z-.15),(-width/2,edge,z-.15)]]
    mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'C1_Roof','EntranceCanopies')

def end_pier(x,y,face):
    # Hierarchy is the full-depth end pier, recessed shaft face, then two caps.
    box('EndPierBase',x,y, .66,.83,.58,1.32,'C1_Trim','MasonryFraming')
    box('EndPierShaft',x,y,3.90,.68,.52,5.16,'C1_Trim','MasonryFraming')
    box('EndPierFace',x,y+face*.24,3.87,.49,.16,5.08,'C1_Stucco','MasonryFraming')
    box('EndPierNeck',x,y,6.79,.74,.57,.62,'C1_Trim','MasonryFraming')
    box('EndPierLowerCapital',x,y+face*.025,7.02,.90,.68,.16,'C1_Trim','MasonryFraming')
    box('EndPierUpperCapital',x,y+face*.035,7.18,.97,.76,.16,'C1_Trim','MasonryFraming')

# Revision 02: the supplied elevation establishes the circular arch, portal
# hierarchy, unequal side openings and lower wall-to-roof ratio.
front_holes=[(-DOOR_W/2,DOOR_W/2,FLOOR,FLOOR+DOOR_H),(-6.47,-5.47,FLOOR,2.33),(5.15,6.25,FLOOR,2.38)]
rear_holes=[(-.78,.78,FLOOR,2.78)]
panel_with_voids('FrontMasonry',-HALF,HALF,-12+WALL/2,WALL,front_holes)
panel_with_voids('RearMasonry',-HALF,HALF,12-WALL/2,WALL,rear_holes)
for y,face in [(-12+WALL/2,-1),(12-WALL/2,1)]:
    end_arch('CircularSegmentMasonry',y)
    shell_segment('ArchFascia',-ARC_END,ARC_END,y-.24,y+.24,offset=.075,thick=.22,mat='C1_Trim',category='MasonryFraming')
    shell_segment('ArchOuterCoping',-ARC_END,ARC_END,y-.27,y+.27,offset=.16,thick=.095,mat='C1_Trim',category='MasonryFraming')
    for x in [-7.54,7.54]:end_pier(x,y+face*.20,face)

# The concept's near side has three shorter clerestories. The opposite elevation
# has two longer openings; it is not a mirror of the hero-facing wall.
window_schedules={
    1:[(-10.0,-4.65,5.11,6.25),(-2.48,2.98,5.11,6.25),(5.25,10.0,5.11,6.25)],
    -1:[(-7.72,.30,5.18,6.25),(2.72,8.72,5.18,6.25)]
}
for side in [-1,1]:
    windows=window_schedules[side]
    doors=[(-10.55,-9.5,FLOOR,2.38),(9.5,10.55,FLOOR,2.38)]
    holes=windows+doors
    panel_with_voids('SideMasonry',-11.68,11.68,side*(HALF-WALL/2),WALL,holes,True)
    bays=[-3.61,4.13] if side==1 else [1.51]
    for y in bays:
        box('ShallowIntermediatePier',side*7.88,y,3.43,.20,.31,6.86,'C1_Stucco','MasonryFraming')
    for z,sx,sz in [(6.98,.30,.14),(7.12,.37,.12)]:
        box('ContinuousEaveCourse',side*7.87,0,z,sx,23.5,sz,'C1_Trim','MasonryFraming')
    for y0,y1,z0,z1 in windows:
        mid=(y0+y1)/2
        box('RecessedClerestory',side*7.68,mid,(z0+z1)/2,.035,y1-y0,z1-z0,'C1_GlassStudy','Clerestory')
        box('ClosedClerestoryBlackout',side*7.49,mid,(z0+z1)/2,.07,y1-y0+.10,z1-z0+.10,'C1_Blackout','BlackoutShutters')
        for z in [z0,z1]:box('WindowHeadSill',side*7.72,mid,z,.18,y1-y0+.08,.075,'C1_Steel','Clerestory')
        panes=round((y1-y0)/.68)
        for k in range(panes+1):box('WindowVertical',side*7.72,y0+(y1-y0)*k/panes,(z0+z1)/2,.10,.035,z1-z0,'C1_Steel','Clerestory')
        box('WindowTransom',side*7.72,mid,(z0+z1)/2,.10,y1-y0,.03,'C1_Steel','Clerestory')
    for y0,y1,z0,z1 in doors:
        mid=(y0+y1)/2
        box('SideServiceLeaf',side*7.62,mid,(z0+z1)/2,.12,y1-y0-.035,z1-z0-.015,'C1_Timber','PersonnelDoors')
        for yy in [y0-.06,y1+.06]:box('SideServiceReveal',side*7.77,yy,1.24,.23,.12,2.48,'C1_Trim','MasonryFraming')
        box('SideServiceHead',side*7.77,mid,2.43,.23,1.30,.12,'C1_Trim','MasonryFraming')

# The broad outer masonry and deeper inner reveal form one portal, with a
# restrained lintel ledge. No steel hardware, plank detail, rivets or weathering.
for side in [-1,1]:
    box('PortalOuterJamb',side*4.05,-12.115,3.70,.90,.55,7.40,'C1_Stucco','MasonryFraming',.015)
    box('PortalFaceJamb',side*3.94,-12.37,3.69,.63,.12,7.38,'C1_Trim','MasonryFraming',.015)
    box('PortalInnerRebate',side*3.665,-12.05,3.58,.13,.38,7.16,'C1_Base','MasonryFraming')
box('PortalOuterHead',0,-12.115,7.25,9.0,.55,.54,'C1_Stucco','MasonryFraming',.015)
box('PortalFaceHead',0,-12.37,7.22,8.51,.12,.48,'C1_Trim','MasonryFraming',.015)
box('PortalLintelLedge',0,-12.38,7.55,9.16,.29,.10,'C1_Trim','MasonryFraming')
for side in [-1,1]:
    cat='MainDoorLeft' if side<0 else 'MainDoorRight'
    box('MainDoorLeaf',side*1.803,-11.835,FLOOR+DOOR_H/2,3.586,.20,DOOR_H-.025,'C1_Timber',cat,.012)
    for x in [side*.08,side*3.49]:box('DoorLeafStile',x,-11.958,FLOOR+DOOR_H/2,.095,.045,DOOR_H-.025,'C1_Steel',cat)

# Personnel door opening and surround hierarchy follows the concept's smaller,
# offset industrial entrances. The right entry alone has the pitched hood.
for x,width,height in [(-5.97,1.0,2.25),(5.70,1.10,2.30)]:
    box('FrontPersonnelLeaf',x,-11.84,FLOOR+height/2,width-.035,.14,height-.015,'C1_Timber','PersonnelDoors')
    for xx in [x-width/2-.095,x+width/2+.095]:box('PersonnelRecessJamb',xx,-12.08,1.25,.19,.34,2.50,'C1_Trim','MasonryFraming')
    box('PersonnelHead',x,-12.08,height+.18,width+.38,.34,.20,'C1_Trim','MasonryFraming')
canopy('FrontPitchedRainHood',5.70,-12.13,1.83,.76,2.94)
box('RearServiceLeaf',0,11.83,1.43,1.515,.14,2.685,'C1_Timber','PersonnelDoors')
for x in [-.90,.90]:box('RearRecessJamb',x,12.08,1.49,.24,.40,2.98,'C1_Trim','MasonryFraming')
box('RearRecessHeader',0,12.08,2.9,2.04,.40,.22,'C1_Trim','MasonryFraming')
canopy('RearPitchedRainHood',0,12.13,2.30,.89,3.14,face=1)
for x,z,w,h in [(-2.60,8.04,.63,.78),(0,9.06,.62,.97),(2.60,8.04,.63,.78)]:
    box('RearVentOpeningAllowance',x,12.015,z,w,.08,h,'C1_Steel','RearVentilation')

# Roof set just behind the masonry arch, with a shallow eave seat. Geometry
# carries the revised barrel curvature; no panel seams or fine roof dressings.
# End the curved sheet above the cornice. A formed, falling apron carries
# runoff across the cornice to the external gutter (production_rainwater.py).
ROOF_RUNOFF_X=7.67
roof_end=math.asin(ROOF_RUNOFF_X/(RADIUS-.08))
bands=[(-math.asin(5.78/RADIUS),-math.asin(3.17/RADIUS)),(math.asin(3.17/RADIUS),math.asin(5.78/RADIUS))]
cuts=[-roof_end,bands[0][0],bands[0][1],bands[1][0],bands[1][1],roof_end]
for a,b in zip(cuts[:-1],cuts[1:]):
    if (a,b) in bands:
        for lo,hi in [(-11.72,-10.2),(10.2,11.72)]:shell_segment('RoofEndSheet',a,b,lo,hi,offset=-.08)
        shell_segment('RoofGlazingRibbon',a,b,-10.2,10.2,offset=-.075,thick=.05,mat='C1_GlassStudy',category='RoofGlazing')
        shell_segment('ClosedRoofBlackout',a-.005,b+.005,-10.24,10.24,offset=-.27,thick=.075,mat='C1_Blackout',category='BlackoutShutters')
        for angle in [a,b]:shell_segment('RibbonLongitudinalEdge',angle-.003,angle+.003,-10.26,10.26,offset=-.025,thick=.09,mat='C1_Steel',category='RoofGlazing')
        for i in range(15):
            yy=-10.2+i*20.4/14
            shell_segment('RibbonCrossFrame',a,b,yy-.023,yy+.023,offset=-.025,thick=.09,mat='C1_Steel',category='RoofGlazing')
    else:shell_segment('RoofSheet',a,b,-11.72,11.72,offset=-.08)
for y in [-7.85,0,7.85]:
    box('VentCurb',0,y,EAVE+RISE-.045,1.09,.95,.23,'C1_Roof','RoofVents')
    box('VentHousingMassing',0,y,EAVE+RISE+.27,.89,.78,.47,'C1_Steel','RoofVents')
    box('VentCap',0,y,EAVE+RISE+.54,1.15,1.03,.10,'C1_Roof','RoofVents')

# INTERIOR MAJOR SYSTEMS: actual load-path and circulation planning. These are
# deliberately plain sections, without connection plates, fasteners or grating.
box('FilmingFloor',0,0,(FLOOR+GROUND)/2,W-2*WALL,D-2*WALL,FLOOR-GROUND,'C1_Floor','InteriorFloor')
truss_half=7.42
truss_angles=[math.asin(x/RADIUS) for x in [-7.42,-5.565,-3.71,-1.855,0,1.855,3.71,5.565,7.42]]
truss_x=[(RADIUS-.34)*math.sin(t) for t in truss_angles]
truss_top=[CIRCLE_Z+(RADIUS-.34)*math.cos(t) for t in truss_angles]
column_x=truss_x[-1]
for yy in FRAME_Y:
    shell_segment('BowstringTopChord',truss_angles[0],truss_angles[-1],yy-.095,yy+.095,offset=-.34,thick=.17,mat='C1_Steel',category='PrimaryStructure')
    beam('BowstringTie',(-column_x,yy,TIE),(column_x,yy,TIE),.17,.20)
    for side in [-1,1]:
        box('PrimaryWallColumn',side*column_x,yy,(truss_top[-1]+FLOOR)/2,.25,.29,truss_top[-1]-FLOOR,'C1_Steel','PrimaryStructure')
    for i,x in enumerate(truss_x[1:-1],1):
        beam('TrussVertical',(x,yy,TIE),(x,yy,truss_top[i]-.06),.085,.095)
    for i in range(8):
        if i<4:
            start=(truss_x[i],yy,TIE);end=(truss_x[i+1],yy,truss_top[i+1]-.06)
        else:
            start=(truss_x[i],yy,truss_top[i]-.06);end=(truss_x[i+1],yy,TIE)
        beam('TrussDiagonal',start,end,.085,.095)
# Longitudinal purlins visibly meet every top chord and support the roof lining.
for x in [-6.15,-3.60,0,3.60,6.15]:
    z=arch_z(x,-.34)-.06
    beam('LongitudinalPurlin',(x,-11.2,z),(x,11.2,z),.13,.18)

# Film rigging is lower, orthogonal and separate from the curved primary roof.
# Its suspended supports connect to bowstring ties; no movable lights/sets.
for x in [-5.72,0,5.72]:
    beam('RiggingLongitudinal',(x,-11.20,GRID),(x,5.60,GRID),.11,.15,category='RiggingPrimary')
    for yy in FRAME_Y[:-1]:
        beam('RiggingHanger',(x,yy,GRID+.055),(x,yy,TIE-.06),.055,.055,category='RiggingPrimary')
for yy in [-11.2,-8.4,-5.6,-2.8,0,2.8,5.6]:
    beam('RiggingCrossMember',(-5.72,yy,GRID),(5.72,yy,GRID),.10,.13,category='RiggingPrimary')

# Supported side catwalks and a rear crosswalk. The stair is in a separate rear
# band, so an overhead deck cannot cut through the upper stair headroom.
walk_inner,walk_outer=6.12,7.26
for side in [-1,1]:
    box('SideCatwalkDeck',side*(walk_inner+walk_outer)/2,0,CATWALK-.055,walk_outer-walk_inner,22.4,.11,'C1_Base','CirculationAllowances')
    for xx in [walk_inner+.08,walk_outer-.07]:
        beam('CatwalkEdgeBeam',(side*xx,-11.2,CATWALK-.16),(side*xx,11.2,CATWALK-.16),.14,.22,category='CatwalkStructure')
    for yy in FRAME_Y:
        beam('ColumnCantilever',(side*column_x,yy,CATWALK-.16),(side*walk_inner,yy,CATWALK-.16),.15,.18,category='CatwalkStructure')
        beam('CatwalkKneeBrace',(side*column_x,yy,CATWALK-1.15),(side*(walk_inner+.06),yy,CATWALK-.19),.105,.105,category='CatwalkStructure')
    for yy in [-11.2,-8.4,-5.6,-2.8,0,2.8,5.6,8.4,11.2]:
        beam('CatwalkGuardPost',(side*walk_inner,yy,CATWALK),(side*walk_inner,yy,CATWALK+1.10),.045,.045,category='CirculationGuards')
    for zz in [CATWALK+.55,CATWALK+1.10]:
        beam('SideGuardRail',(side*walk_inner,-11.2,zz),(side*walk_inner,10.02,zz),.045,.045,category='CirculationGuards')
    # Front end guard joins the wall and returns to the inner side rail.
    for zz in [CATWALK+.55,CATWALK+1.10]:beam('FrontCatwalkReturn',(side*walk_inner,-11.2,zz),(side*walk_outer,-11.2,zz),.045,.045,category='CirculationGuards')
box('RearCrosswalkDeck',0,10.61,CATWALK-.055,12.24,1.18,.11,'C1_Base','CirculationAllowances')
for yy in [10.09,11.13]:beam('RearCrosswalkBeam',(-7.26,yy,CATWALK-.16),(7.26,yy,CATWALK-.16),.15,.22,category='CatwalkStructure')
# Guard the gap between walkway and wall, including the rear service edge.
for xx in [-walk_outer,walk_outer]:
    for zz in [CATWALK+.55,CATWALK+1.10]:beam('OuterWalkRail',(xx,-11.2,zz),(xx,11.2,zz),.045,.045,category='CirculationGuards')
    for yy in FRAME_Y:beam('OuterWalkPost',(xx,yy,CATWALK),(xx,yy,CATWALK+1.10),.045,.045,category='CirculationGuards')
for zz in [CATWALK+.55,CATWALK+1.10]:beam('RearOuterRail',(-walk_outer,11.2,zz),(walk_outer,11.2,zz),.045,.045,category='CirculationGuards')
for xx in [-6.12,-3.65,0,3.65,6.12]:beam('RearOuterPost',(xx,11.2,CATWALK),(xx,11.2,CATWALK+1.10),.045,.045,category='CirculationGuards')
# Rear intermediate support columns occupy the service edge, not the filming floor.
for x in [-3.65,3.65]:
    box('RearCatwalkBearing',x,11.13,(CATWALK-.16)/2,.18,.18,CATWALK-.16,'C1_Steel','CatwalkStructure')
    beam('RearCatwalkBracket',(x,11.13,CATWALK-1.1),(x,10.09,CATWALK-.16),.10,.10,category='CatwalkStructure')
# Stair: 28 risers of 174 mm, two 14-riser flights, 280 mm goings,
# 1.2 m clear width, 1.2 m intermediate and top landings.
RISERS=28;R=(CATWALK-FLOOR)/RISERS;GOING=.28;FLIGHT_RUN=13*GOING
stair_y0,stair_y1=8.35,9.55
start_x=-5.76;mid_x=start_x+FLIGHT_RUN;second_x=mid_x+1.20;top_x=second_x+FLIGHT_RUN
box('StairBottomLanding',start_x-.60,(stair_y0+stair_y1)/2,FLOOR-.05,1.20,1.20,.10,'C1_Base','Stairs')
for flight,x0,z0 in [(0,start_x,FLOOR),(1,second_x,FLOOR+14*R)]:
    for i in range(14):
        xx=x0+i*GOING
        box('StairTread',xx,(stair_y0+stair_y1)/2,z0+(i+1)*R-.045,.29,1.20,.09,'C1_Base','Stairs')
    for yy in [stair_y0+.035,stair_y1-.035]:
        beam('StairStringer',(x0-.12,yy,z0-.02),(x0+FLIGHT_RUN+.12,yy,z0+14*R-.11),.11,.22,category='Stairs')
        for xx,zz in [(x0,z0+R),(x0+FLIGHT_RUN/2,z0+7.5*R),(x0+FLIGHT_RUN,z0+14*R)]:
            beam('StairGuardPost',(xx,yy,zz),(xx,yy,zz+1.10),.045,.045,category='CirculationGuards')
        beam('StairHandrail',(x0-GOING,yy,z0+1.10),(x0+FLIGHT_RUN,yy,z0+14*R+1.10),.045,.045,category='CirculationGuards')
for name,xx,zz,depth in [('IntermediateLanding',mid_x+.60,FLOOR+14*R,1.20),('TopLanding',top_x+.60,CATWALK,2.85)]:
    centre_y=(stair_y0+stair_y1)/2 if depth==1.20 else (stair_y0+11.2)/2
    box(name,xx,centre_y,zz-.075,1.20,depth,.15,'C1_Base','Stairs')
    for yy in [stair_y0+.05,stair_y1-.05]:
        box(name+'Bearing',xx+.45,yy,(zz-.15+FLOOR)/2,.14,.14,zz-.15-FLOOR,'C1_Steel','Stairs')
        if name=='IntermediateLanding' or yy<stair_y0+.10:
            beam(name+'Rail',(xx-.60,yy,zz+1.10),(xx+.60,yy,zz+1.10),.045,.045,category='CirculationGuards')
# Top-landing guard returns leave the crosswalk entry open at rear.
for xx in [top_x,top_x+1.20]:
    y0=stair_y1-.035 if xx==top_x else stair_y0+.035
    for zz in [CATWALK+.55,CATWALK+1.10]:beam('LandingReturnRail',(xx,y0,zz),(xx,10.02,zz),.045,.045,category='CirculationGuards')
    beam('LandingReturnPost',(xx,y0,CATWALK),(xx,y0,CATWALK+1.10),.045,.045,category='CirculationGuards')
# Rear crosswalk inner guard is interrupted at the stair landing only.
for x0,x1 in [(-6.12,top_x),(top_x+1.20,6.12)]:
    for zz in [CATWALK+.55,CATWALK+1.10]:beam('RearGuardRail',(x0,10.02,zz),(x1,10.02,zz),.045,.045,category='CirculationGuards')
    for xx in [x0,(x0+x1)/2,x1]:beam('RearGuardPost',(xx,10.02,CATWALK),(xx,10.02,CATWALK+1.10),.045,.045,category='CirculationGuards')

# Replace provisional components with reproducible production construction.
exec(compile((HERE/'production_geometry.py').read_text(encoding='utf-8'),str(HERE/'production_geometry.py'),'exec'),globals())

# Assert clean geometry; authoring normals recalculated for every closed piece.
bad=[]
for o in objects:
    o.data.calc_loop_triangles()
    if any(p.area < 1e-10 for p in o.data.polygons):bad.append(o.name)
    assert all(abs(v-1)<1e-6 for v in o.scale),o.name
    bm=bmesh.new();bm.from_mesh(o.data)
    assert all(e.is_manifold for e in bm.edges), 'Open component: '+o.name
    assert bm.calc_volume(signed=True)>0, 'Non-outward component: '+o.name
    bm.free()
assert not bad, 'Degenerate faces: '+str(bad)
assert not any('stage 1' in o.name.lower() or o.type=='FONT' for o in objects)
assert abs((W-2*WALL)-15.16)<1e-6

# Check actual source geometry over circulation paths, not just nominal heights.
bpy.context.view_layer.update()
bvh_vertices=[];bvh_faces=[]
for o in objects:
    base=len(bvh_vertices)
    bvh_vertices.extend(o.matrix_world@v.co for v in o.data.vertices)
    bvh_faces.extend(tuple(base+i for i in p.vertices) for p in o.data.polygons)
bvh=BVHTree.FromPolygons(bvh_vertices,bvh_faces)
path_samples=[]
for flight,x0,z0 in [(0,start_x,FLOOR),(1,second_x,FLOOR+14*R)]:
    path_samples.extend((x0+i*GOING,8.95,z0+(i+1)*R) for i in range(14))
path_samples.extend((mid_x+.60,y,FLOOR+14*R) for y in [8.65,8.95,9.25])
path_samples.extend((top_x+.60,y,CATWALK) for y in [8.65,8.95,9.25,9.55,9.85,10.15,10.60])
path_samples.extend((side*6.60,-10.8+i*.54,CATWALK) for side in [-1,1] for i in range(41))
path_samples.extend((-6.60+i*.33,10.60,CATWALK) for i in range(41))
headrooms=[]
for x,y,z in path_samples:
    hit,normal,index,distance=bvh.ray_cast(Vector((x,y,z+.015)),Vector((0,0,1)),20)
    if hit is not None:headrooms.append(distance+.015)
assert min(headrooms)>2.0, 'Circulation headroom obstruction: '+str(min(headrooms))
floor_clearances=[]
for x in [-5.9,-3,0,3,5.9]:
    for y in [-10.7,-8,-5.6,-2.8,0,2.8,5.6,8.05]:
        hit,normal,index,distance=bvh.ray_cast(Vector((x,y,FLOOR+.015)),Vector((0,0,1)),20)
        if hit is not None:floor_clearances.append(distance+.015)
assert min(floor_clearances)>6.2, 'Clear filming floor obstruction'
planning_validation={'circulation_headroom_samples':len(path_samples),'minimum_sampled_headroom':round(min(headrooms),4),'clear_floor_samples':40,'minimum_sampled_filming_height':round(min(floor_clearances),4),'scope':'Geometric planning checks, not engineering or regulatory certification'}

# Export in repository convention: editable pieces remain in .blend, joined
# semantic copies exported to FBX, then copied unchanged into Unity Models.
exports=[]
for category,parent in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    copies=[]
    for o in objects:
        if o.parent != parent:continue
        c=o.copy();c.data=o.data.copy();asset.objects.link(c);c.select_set(True);copies.append(c)
    bpy.context.view_layer.objects.active=copies[0]
    if len(copies)>1:bpy.ops.object.join()
    c=bpy.context.object;c.parent=root;c.name='Export_'+category
    pivot=(-3.6,-11.835,FLOOR) if category=='MainDoorLeft' else ((3.6,-11.835,FLOOR) if category=='MainDoorRight' else (0,0,0))
    scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    exports.append(c)
for p in [OUT,UNITY]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
for o in exports:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/(NAME+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
shutil.copy2(OUT/(NAME+'.fbx'),UNITY/(NAME+'.fbx'))
vs=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
bounds=[[round(f(v[i] for v in vs),4) for i in range(3)] for f in [min,max]]
group_statistics={category:{'components':sum(o.parent==parent for o in objects),'triangles':sum(len(o.data.loop_triangles) for o in objects if o.parent==parent)} for category,parent in groups.items()}
report={'candidate':NAME,'revision':'04','gate':'FINAL_VISUAL_APPROVAL_PENDING','geometry_source':'Approved production candidate with targeted construction corrections','group_statistics':group_statistics,
        'dimensions':{'wall_width':W,'wall_depth':D,'eave':EAVE,'roof_crown':EAVE+RISE-.08,'coping_crown':EAVE+RISE+.16,'vent_top':bounds[1][2],'arch_circle_radius':RADIUS,'main_door_clear_width':DOOR_W,'main_door_clear_height':DOOR_H,'portal_outer_width':9.16,'wall_thickness':WALL,'interior_wall_clear_width':15.16,'interior_wall_clear_depth':23.36,'floor_level':FLOOR,'central_width_between_catwalks':12.24,'catwalk_deck_top':CATWALK,'truss_bottom_chord_centre':TIE,'rigging_grid_centre':GRID,'sign_baseline':SIGN_BASE,'sign_letter_height':1.02,'sign_maximum_width':4.95},
        'bounds_blender_xyz':bounds,'unity_front':'-Z with model root Euler (-90,180,0)',
        'source_meshes':len(objects),'export_meshes':len(exports),'triangles':sum(len(o.data.loop_triangles) for o in objects),'planning_validation':planning_validation,
        'degenerate_faces':0,'all_source_components_closed_and_outward':True,'export_groups':list(groups),'materials':[{'name':n,'linear_rgb':c,'metallic':m,'roughness':r,'texture_set':TEXTURE_MATERIALS.get(n,{}).get('set','')} for n,(c,m,r) in PALETTE.items()],
        'permanent_lights':PERMANENT_LIGHTS,'texture_materials':TEXTURE_MATERIALS,'ground_datum':GROUND,'defect_validation':DEFECT_VALIDATION,'roof_daylight_control':ROOF_BLACKOUT_DESCRIPTION,
        'structural_planning':{'frames_y':FRAME_Y,'system':'Five clear-span bowstring roof trusses with rolled chords, paired angle webs, riveted gussets and purlins','catwalks':'Open bar grating on girders and column knee brackets; rear crosswalk on service-edge supports; tubular guards and toe boards','rigging':'Seven longitudinal rows and fourteen cross battens, ending at Y=7.0 ahead of the stair; rear high carriers connect to roof ties above circulation headroom','stairs':{'risers':RISERS,'rise':R,'going':GOING,'deck_width':1.2,'intermediate_landing':1.2,'top_landing':1.2,'position':'Rear service band ahead of crosswalk; no deck over upper stair','headroom_below_ties':TIE-.125-CATWALK},'clear_central_floor_rectangle':[-6.0,6.0,-10.8,8.15]},
        'exclusions':['localized weathering and condition effects','loose props','baked stage lettering','LOD meshes','gameplay integration of door and blackout motion'],
        'anchors_unity_local':{'MainPortal':[0,FLOOR,-12.05],'FrontPersonnelRight':[5.7,FLOOR,-12.5],'FrontPersonnelLeft':[-5.97,FLOOR,-12.5],'FilmingCentre':[0,FLOOR,0],'SignBaseline':[0,SIGN_BASE,-12.035]}}
(HERE/'generation_report.json').write_text(json.dumps(report,indent=2))
for o in exports:bpy.data.objects.remove(o,do_unlink=True)
scene.cursor.location=(0,0,0)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(NAME+'.blend')))
print(json.dumps({k:report[k] for k in ['candidate','dimensions','bounds_blender_xyz','source_meshes','export_meshes','triangles','degenerate_faces']}))
