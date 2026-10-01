"""Original landmark-authored anatomy and facial surface construction.

No human assets are loaded. Face openings have authored concentric loops;
the body is a connected sculpt surface awaiting deformation retopology review.
"""
import math
import bpy
import bmesh
from mathutils import Vector
from author_accessories import mesh, grid, subdiv, thickness, cord, move_collection

TAU=math.tau


def interp(rows,z,index):
    if z<=rows[0][0]: return rows[0][index]
    if z>=rows[-1][0]: return rows[-1][index]
    for a,b in zip(rows,rows[1:]):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]); t=t*t*(3-2*t)
            return a[index]*(1-t)+b[index]*t


def gaussian(x,c,s): return math.exp(-((x-c)/s)**2)


def ellipsoid(name,center,radii,mat,col,segments=40,rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=center)
    o=bpy.context.object; o.name=name
    o.scale=radii
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat); move_collection(o,col)
    for p in o.data.polygons: p.use_smooth=True
    return o


def tube(name,rows,axis,mat,col,segments=40,detail=None):
    # rows: longitudinal coordinate, center1, center2, radius1, radius2.
    vertices=[]; faces=[]
    # Dense sculpt samples preserve explicitly placed anatomical sections.
    steps=max(20,len(rows)*3)
    for j in range(steps):
        q=rows[0][0]+(rows[-1][0]-rows[0][0])*j/(steps-1)
        c1,c2,r1,r2=[interp(rows,q,k) for k in range(1,5)]
        for i in range(segments):
            t=TAU*i/segments
            a=c1+r1*math.sin(t); b=c2+r2*math.cos(t)
            p=(q,a,b) if axis=='X' else (a,b,q)
            if detail: p=detail(p,t,q)
            vertices.append(p)
    for j in range(steps-1):
        for i in range(segments):
            k=(i+1)%segments; a=j*segments+i; b=j*segments+k
            faces.append((a,b,b+segments,a+segments))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple((steps-1)*segments+i for i in range(segments)))
    return mesh(name,vertices,faces,mat,col)


TORSO=[
    (.89,0,.008,.037,.055),(.935,0,.012,.119,.083),(.98,0,.010,.148,.092),
    (1.025,0,.008,.146,.087),(1.075,0,.006,.132,.076),(1.135,0,.008,.128,.075),
    (1.205,0,.006,.146,.085),(1.28,0,.004,.163,.092),(1.355,0,.001,.176,.095),
    (1.405,0,.002,.178,.086),(1.444,0,.006,.157,.074),(1.478,0,.004,.092,.056),
    (1.501,0,.003,.047,.045),(1.529,0,.001,.044,.045),
]
ARM=[(.140,0,1.446,.050,.051),(.190,0,1.459,.053,.054),(.245,0,1.461,.046,.051),
     (.305,0,1.459,.040,.046),(.38,0,1.46,.033,.039),(.440,0,1.46,.028,.031),
     (.475,0,1.46,.028,.029),(.509,0,1.46,.030,.033),(.55,0,1.46,.031,.032),
     (.605,0,1.46,.027,.026),(.674,0,1.46,.021,.020),(.727,0,1.46,.019,.014)]
LEG=[(.075,.102,.002,.027,.032),(.13,.102,.002,.031,.031),(.205,.103,.008,.034,.039),
     (.31,.103,.011,.047,.050),(.39,.103,.002,.044,.045),(.47,.103,-.010,.037,.039),
     (.505,.103,-.011,.038,.041),(.558,.102,-.001,.043,.044),(.67,.100,.006,.056,.059),
     (.78,.098,.009,.067,.071),(.875,.096,.013,.076,.083),(.965,.088,.014,.080,.087)]


def join_sculpt(objects,name,col,voxel=.0022):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name
    # Voxel union creates actual connected anatomy from the new sculpt volumes.
    rem=obj.modifiers.new('Connected sculpt surface','REMESH')
    rem.mode='VOXEL'; rem.voxel_size=voxel; rem.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=rem.name)
    sm=obj.modifiers.new('Sculpt relaxation','SMOOTH'); sm.factor=.35; sm.iterations=3
    bpy.ops.object.modifier_apply(modifier=sm.name)
    move_collection(obj,col)
    obj['topology_status']='Connected sculpt quads; joint retopology/deformation review pending'
    return obj


def make_body(m):
    col='Anatomy_Body'; parts=[]
    def trunk_detail(p,t,z):
        x,y,z=p; front=max(0,-math.cos(t))
        # Defined, understated pectoral/abdominal planes rather than inflated muscle balls.
        y-=front**3*(.005*gaussian(abs(x),.087,.057)*gaussian(z,1.348,.042))
        y-=front**4*.002*gaussian(abs(x),.040,.030)*(gaussian(z,1.20,.03)+gaussian(z,1.15,.025))
        y+=front**6*.0035*gaussian(x,0,.009)*gaussian(z,1.13,.009)
        return x,y,z
    parts.append(tube('Anatomy_TorsoSculpt',TORSO,'Z',m,col,64,trunk_detail))
    for sign in (-1,1):
        a=tube('Anatomy_ArmSculpt',ARM,'X',m,col,40)
        if sign<0:
            for v in a.data.vertices: v.co.x*=-1
        parts.append(a)
        legrows=[(q,sign*x,y,r1,r2) for q,x,y,r1,r2 in LEG]
        parts.append(tube('Anatomy_LegSculpt',legrows,'Z',m,col,48))
        parts.append(ellipsoid('Anatomy_GluteSculpt',(sign*.063,.067,.971),(.080,.049,.091),m,col))
        # Flat palms, individual metacarpal volume, three-knuckle fingers.
        palm=tube('Anatomy_PalmSculpt',[(.718,0,1.46,.019,.013),(.749,0,1.46,.030,.016),(.777,0,1.459,.034,.014),(.803,0,1.458,.028,.011)],'X',m,col,32)
        if sign<0:
            for v in palm.data.vertices:v.co.x*=-1
        parts.append(palm)
        for finger,y,length,r in [('Index',-.027,.075,.0077),('Middle',-.009,.084,.0082),('Ring',.009,.078,.0076),('Pinky',.026,.060,.0063)]:
            x=.796-(.008 if finger=='Pinky' else 0)
            rows=[]
            for t,rad in [(0,1.02),(.18,.99),(.42,1.06),(.53,.91),(.73,.94),(.9,.78),(1,.2)]:
                rows.append((x+length*t,y,1.458-.003*t,r*rad,r*.92*rad))
            f=tube('Anatomy_'+finger+'Sculpt',rows,'X',m,col,20)
            if sign<0:
                for v in f.data.vertices:v.co.x*=-1
            parts.append(f)
        thumb=tube('Anatomy_ThumbSculpt',[(.743,-.021,1.448,.013,.012),(.765,-.042,1.445,.011,.010),(.79,-.052,1.445,.0095,.009),(.812,-.063,1.445,.006,.006),(.818,-.065,1.445,.002,.002)],'X',m,col,24)
        if sign<0:
            for v in thumb.data.vertices:v.co.x*=-1
        parts.append(thumb)
        # Full heel, instep, forefoot and individually modeled toes.
        parts.append(ellipsoid('Anatomy_Heel',(sign*.102,.015,.059),(.032,.053,.059),m,col))
        foot=ellipsoid('Anatomy_Instep',(sign*.102,-.079,.046),(.038,.100,.043),m,col)
        parts.append(foot)
        for i,(dx,length,r) in enumerate([(-.026,.048,.0115),(-.007,.046,.0098),(.010,.037,.0087),(.024,.030,.0075),(.035,.023,.0065)]):
            # Positive foot is left; big toe points toward sagittal plane.
            parts.append(ellipsoid('Anatomy_Toe'+str(i),(sign*(.102+dx),-.153-length*.42,.019), (r,length*.70,.015 if i<2 else .012),m,col,24,16))
    # Complete nonsexual anatomical pelvis; no modeling shorts exist in this source.
    parts.append(ellipsoid('Anatomy_PubicVolume',(0,-.060,.924),(.039,.032,.038),m,col))
    parts.append(ellipsoid('Anatomy_ScrotalVolume',(0,-.071,.893),(.024,.026,.032),m,col,32,20))
    parts.append(ellipsoid('Anatomy_PenisVolume',(0,-.095,.898),(.012,.013,.032),m,col,32,20))
    body=join_sculpt(parts,'Body_CompleteAnatomy',col,.0019)
    # Ground the complete anatomy; retain the 91-bone scaffold exactly as authored.
    for v in body.data.vertices:
        if v.co.z<.004: v.co.z=0
    body['covered_regions']='Complete anatomy retained; there is no clothing in this mesh'
    body['shorts_present']=False
    return body


HEAD_PROFILE=[(1.525,.044,.045,.047),(1.541,.045,.052,.055),(1.554,.048,.075,.062),
    (1.569,.058,.083,.071),(1.585,.066,.076,.078),(1.606,.069,.072,.082),
    (1.625,.071,.067,.085),(1.648,.075,.060,.088),(1.668,.076,.060,.090),
    (1.688,.075,.066,.093),(1.710,.076,.063,.094),(1.731,.074,.059,.091),
    (1.75,.063,.049,.079),(1.765,.048,.036,.060),(1.776,.026,.019,.032),(1.783,.001,.001,.001)]


def face_y(x,z):
    w=interp(HEAD_PROFILE,z,1); dep=interp(HEAD_PROFILE,z,2)
    u=x/max(w,.001)
    y=.003-dep*math.sqrt(max(0,1-u*u))
    y-=.0085*gaussian(abs(x),.032,.024)*gaussian(z,1.688,.009)
    y-=.011*gaussian(abs(x),.045,.020)*gaussian(z,1.646,.020)
    y-=.020*gaussian(x,0,.010)*gaussian(z,1.663,.022)
    y-=.033*gaussian(x,0,.0095)*gaussian(z,1.640,.009)
    y-=.024*gaussian(abs(x),.0105,.006)*gaussian(z,1.632,.005)
    y-=.009*gaussian(x,0,.024)*gaussian(z,1.609,.014)
    y-=.004*gaussian(abs(x),.021,.008)*gaussian(z,1.623,.014)
    y+=.002*gaussian(abs(x),.029,.007)*gaussian(z,1.618,.015)
    return y


def make_head(m,lip,interior):
    col='Anatomy_Head'
    zrows=[1.525,1.534,1.542,1.549,1.555,1.561,1.568,1.576,1.584,1.590,1.596,1.600,
           1.604,1.608,1.612,1.617,1.622,1.627,1.632,1.638,1.644,1.650,1.655,1.660,1.665,
           1.670,1.675,1.680,1.686,1.693,1.701,1.710,1.720,1.731,1.742,1.751,1.759,1.766,1.772,1.777,1.781,1.783]
    N=48; rear=32; verts=[];faces=[]
    for z in zrows:
        w=interp(HEAD_PROFILE,z,1)
        for i in range(N+1):
            x=(i/N*2-1)*w
            verts.append((x,face_y(x,z),z))
    # Openings remove a rectangular patch; concentric loop flow fills around it.
    holes=[('EyeRight',7,20,21,28),('EyeLeft',28,41,21,28),('Mouth',12,36,9,16)]
    def inside(i,j):return any(a<=i<b and c<=j<d for _,a,b,c,d in holes)
    for j in range(len(zrows)-1):
        for i in range(N):
            if not inside(i,j):
                a=j*(N+1)+i;faces.append((a,a+1,a+N+2,a+N+1))
    # Back of skull shares the exact side edges with the facial surface.
    backloops=[]
    for j,z in enumerate(zrows):
        w=interp(HEAD_PROFILE,z,1); depth=interp(HEAD_PROFILE,z,3)
        ring=[j*(N+1)+N]
        for k in range(1,rear):
            t=math.pi*k/rear
            ring.append(len(verts));verts.append((w*math.cos(t),.003+depth*math.sin(t),z))
        ring.append(j*(N+1));backloops.append(ring)
    for j in range(len(zrows)-1):
        for k in range(rear):faces.append((backloops[j][k],backloops[j][k+1],backloops[j+1][k+1],backloops[j+1][k]))
    top=list(range((len(zrows)-1)*(N+1),len(zrows)*(N+1)))+backloops[-1][1:-1]
    faces.append(tuple(top))
    loops={}
    for name,i0,i1,j0,j1 in holes:
        boundary=[j0*(N+1)+i for i in range(i0,i1)]
        boundary += [j*(N+1)+i1 for j in range(j0,j1)]
        boundary += [j1*(N+1)+i for i in range(i1,i0,-1)]
        boundary += [j*(N+1)+i0 for j in range(j1,j0,-1)]
        previous=boundary
        iseye=name.startswith('Eye')
        cx=(.0315 if name=='EyeLeft' else -.0315) if iseye else 0
        cz=1.665 if iseye else 1.604
        count=7 if iseye else 9
        original=[Vector(verts[k]) for k in boundary]
        angles=[math.atan2((p.z-cz)/(.019 if iseye else .017),(p.x-cx)/(.022 if iseye else .035)) for p in original]
        for r in range(1,count+1):
            u=r/count; new=[]
            for p,t in zip(original,angles):
                if iseye:
                    dx=.0152*math.cos(t)
                    dz=(.0050 if math.sin(t)>0 else .0038)*math.sin(t)
                    dz+=.0008*math.cos(t)*(1 if cx>0 else -1)
                    target=Vector((cx+dx,-.069-math.sqrt(max(.000002,.0118**2-dx*dx-dz*dz))-.0003,cz+dz))
                    point=p.lerp(target,u)
                    # Upper crease and lower lid roll are carried by actual surface loops.
                    point.y=face_y(point.x,point.z)*(1-u)+target.y*u
                    point.y-=.0017*math.sin(math.pi*u)*max(0,math.sin(t))
                    if r==count-2: point.y+=.0008*max(0,math.sin(t))
                else:
                    dx=.0265*math.cos(t)
                    dz=.0007*math.sin(t)+.0004*(abs(dx)/.0265)
                    target=Vector((dx,-.086+.005*(abs(dx)/.0265)**1.4,cz+dz))
                    point=p.lerp(target,u)
                    # Sculpt separate upper/lower lip volumes with a cupid's bow.
                    lipweight=math.exp(-((u-.76)/.19)**2)
                    point.y-=.004*lipweight*(1-abs(math.cos(t))**2)
                    if math.sin(t)>0:
                        point.z+=.0015*lipweight*gaussian(abs(point.x),.008,.006)
                    else: point.y-=.001*lipweight
                new.append(len(verts));verts.append(tuple(point))
            for k in range(len(previous)):
                q=(k+1)%len(previous);faces.append((previous[k],previous[q],new[q],new[k]))
            previous=new
        loops[name]=previous
    obj=mesh('Head_FacialLoopSurface',verts,faces,m,col)
    obj['topology_status']='Authored eye and mouth loops; sculpt and identity review required'
    subdiv(obj,1)
    # Actual mouth cavity follows the lip opening inward, rather than a painted line.
    boundary=loops['Mouth'];v=[Vector(verts[i]) for i in boundary]
    vv=[tuple(p) for p in v]+[(p.x*.83,p.y+.024,p.z-.003) for p in v]
    n=len(v);ff=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(range(n,2*n))]
    mesh('Mouth_Interior',vv,ff,interior,col)
    # Lip border color is kept independent at this neutral geometry checkpoint.
    # Anatomical ears: helix, bowl, antihelix and tragus, no imported head parts.
    for side,sign in [('Left',1),('Right',-1)]:
        def ear(v,t):
            r=1-v*.92
            z=1.645+.029*r*math.cos(t)
            y=.003-.016*r*math.sin(t)*(1+.15*math.cos(t))
            x=sign*(.074+.012*r+.003*math.sin(t)*r-.004*math.sin(v*math.pi))
            return x,y,z
        e=grid(side+'_Ear_Bowl',10,48,ear,m,col);thickness(e,.002);subdiv(e)
        cord(side+'_Ear_Helix',[ear(.02,TAU*i/96) for i in range(96)],.0018,m,col,True)
        cord(side+'_Ear_Antihelix',[ear(.36,-.6+4.6*i/47) for i in range(48)],.0017,m,col)
        ellipsoid(side+'_Ear_Tragus',(sign*.082,-.010,1.642),(.0038,.0045,.006),m,col,24,16)
        ellipsoid(side+'_Ear_Canal',(sign*.078,-.004,1.640),(.002,.004,.005),interior,col,24,16)
        # Small recessed nostril volume below each ala.
        ellipsoid(side+'_NostrilInterior',(sign*.0105,-.0945,1.627),(.0034,.0018,.0018),interior,col,24,16)
    return obj


def make_eyes(sclera,iris,pupil):
    for side,sign in [('Left',1),('Right',-1)]:
        col='Eye_'+side
        eye=ellipsoid(side+'_Eye',(sign*.0315,-.069,1.665),(.0118,.0118,.0118),sclera,col,64,40)
        eye['semantic_bone']=side+'Eye'
        # Separate radial iris and pupil surface geometry; no baked eye color.
        ellipsoid(side+'_Iris',(sign*.0315,-.08045,1.665),(.0052,.0006,.0052),iris,col,64,24)
        ellipsoid(side+'_Pupil',(sign*.0315,-.081,1.665),(.00225,.00035,.00225),pupil,col,48,20)


def make_brows_hair(m):
    for sign in (-1,1):
        v=[];f=[]
        for j in range(25):
            u=j/24;x=sign*(.012+.046*u)
            z=1.687+.0035*math.sin(math.pi*u)-.004*u
            w=.0035*(1-u)+.0007*u
            for k in range(3):
                zz=z+w*(k-1); v.append((x,face_y(x,zz)-.0016-.0007*(1-(k-1)**2),zz))
        for j in range(24):
            for k in range(2):a=j*3+k;f.append((a,a+1,a+4,a+3))
        o=mesh(('Left' if sign>0 else 'Right')+'_Brow',v,f,m,'Brows');thickness(o,.0008);subdiv(o)
    def cap(v,t):
        front=max(0,math.cos(t))
        low=1.613+.121*front**2+.028*abs(math.sin(t))
        z=low+(1.792-low)*v
        srcz=min(z-.007,1.782)
        w=interp(HEAD_PROFILE,srcz,1)+.003*(1-v)
        d=(interp(HEAD_PROFILE,srcz,2) if math.cos(t)>=0 else interp(HEAD_PROFILE,srcz,3))+.004*(1-v)
        x=w*math.sin(t)
        y=.003-d*math.cos(t)
        # Restrained side-part volume, not a second skull.
        z+=.004*math.sin(math.pi*v)*max(0,math.cos(t-.4))
        return x,y,z
    hair=grid('Hair_SeparateScalpGroom',26,96,cap,m,'Hair');thickness(hair,.0015);subdiv(hair)
    # Broad swept locks with low elliptical cross-sections, individually editable.
    for j in range(30):
        theta=-1.5+TAU*j/30
        pts=[]
        for i in range(28):
            u=i/27
            t=theta+.32*math.sin(math.pi*u)*(1 if theta<1 else -1)
            v=.04+.88*u
            p=Vector(cap(v,t));p+=Vector((math.sin(t),-math.cos(t),.6))*.0012*math.sin(math.pi*u)
            pts.append(tuple(p))
        cord('Hair_SweptLock_'+str(j),pts,.0013 if j%3 else .0017,m,'Hair')
    return hair
