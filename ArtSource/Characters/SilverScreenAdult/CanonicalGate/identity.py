"""One persistent concept identity over the immutable male foundation."""
import bpy,bmesh,json,math
import numpy as np
from mathutils import Vector
from geometry import *

def target(points,family):
    p=np.asarray(points);q=p.copy();x,y,z=p.T;a=abs(x);s=np.sign(x)
    def g(cx,cy,cz,sx,sy,sz):return np.exp(-.5*(((a-cx)/sx)**2+((y-cy)/sy)**2+((z-cz)/sz)**2))
    if family=='cranial_jaw':
        q[:,0]+=s*.0022*g(.065,.010,1.725,.023,.080,.040)
        q[:,0]+=s*.0017*g(.062,.001,1.587,.015,.035,.016)
        q[:,1]-=.0023*g(.014,-.069,1.562,.025,.026,.014)
        q[:,2]+=.0010*g(.015,-.056,1.558,.035,.035,.018)
    elif family=='brow_cheek':
        q[:,1]-=.0022*g(.033,-.052,1.687,.025,.028,.008)
        q[:,2]-=.0005*g(.015,-.060,1.686,.012,.030,.009)
        q[:,1]-=.0014*g(.050,-.032,1.638,.020,.030,.013)
        q[:,0]+=s*.0012*g(.053,-.025,1.638,.021,.037,.014)
        q[:,1]+=.0010*g(.050,-.024,1.612,.013,.030,.014)
    elif family=='nose_lips':
        q[:,1]-=.0013*g(.003,-.077,1.653,.009,.026,.023)
        q[:,0]+=s*.0006*g(.016,-.070,1.623,.011,.025,.011)
        q[:,1]-=.0008*g(.011,-.073,1.600,.017,.013,.0045)
        q[:,1]-=.0007*g(.009,-.073,1.587,.019,.013,.0050)
        q[:,0]+=s*.0008*g(.023,-.059,1.595,.013,.024,.012)
    return q

def author_identity(head,root,mats):
    families=['cranial_jaw','brow_cheek','nose_lips'];deltas={};affected=[]
    for name in [head.name,'SS_Teeth','SS_Tongue']:
        o=bpy.data.objects[name];p=np.array([v.co[:] for v in o.data.vertices]);o.shape_key_add(name='Basis')
        boundary=[]
        if o==head:
            bm=bmesh.new();bm.from_mesh(o.data);boundary=list({v.index for e in bm.edges if e.is_boundary for v in e.verts});bm.free()
        for family in families:
            q=target(p,family)
            if boundary:q[boundary]=p[boundary]
            key=o.shape_key_add(name='Identity.'+family)
            key.data.foreach_set('co',q.reshape(-1));key.value=1;key.slider_min=0;key.slider_max=1
            deltas[name+'.'+family]=q-p
        affected.append(name);o['identity_preset']='silverscreen.canonical.leading_man.v1'
    # The existing removable brows follow the resolved neutral identity.
    brows=bpy.data.objects.get('SS_Brow_Study_Removable')
    if brows:
        for sp in brows.data.splines:
            for pt in sp.points:
                p=np.array(pt.co[:3])[None,:];q=p.copy()
                for family in families:q+=target(p,family)-p
                pt.co=(*q[0],1)
        assign(brows,mats['hair']);place(brows,'Module.Brows')
    for name in ('SS_Eye_L','SS_Eye_R'):
        o=bpy.data.objects[name]
        for mat in o.data.materials:
            for node in mat.node_tree.nodes:
                if node.bl_idname=='ShaderNodeMixRGB':
                    c=node.inputs[2].default_value
                    if c[0]>.05:node.inputs[2].default_value=(.095,.044,.018,1)
                    if node.inputs[1].default_value[0]>.3:node.inputs[1].default_value=(.62,.58,.50,1)
        o['iris_pigment']='Independent provisional brown; not baked into head texture'
    np.savez_compressed(root/'concept-identity-deltas.npz',**deltas)
    preset={'id':'silverscreen.canonical.leading_man.v1','foundation':'silverscreen.adult.male.head.v1',
        'status':'FIRST_AUTHORED_IDENTITY_VISUAL_REVIEW','identityTargets':{'Identity.'+x:1.0 for x in families},
        'expressionTargets':{},'affectedModules':affected,'eyesRemainRigid':True,'neckSeamDeltasZero':True,
        'appearance':{'hair':'swept_side_part.v1','iris':'brown.review','skin':'warm_neutral.review'},
        'validatedRange':'This authored combination only; no slider range or expression compatibility claim'}
    (root/'concept-identity-preset.json').write_text(json.dumps(preset,indent=2)+'\n')
    return preset

def author_hair(head,mats):
    col='Module.Hair';bvh=tree(head);centre=Vector((0,.022,1.690))
    def hem(theta):
        a=abs((theta+math.pi)%(2*math.pi)-math.pi)
        return float(np.interp(a,[0,.45,.86,1.15,1.45,1.80,2.12,math.pi],[1.17,1.25,1.40,1.43,1.52,2.00,2.55,2.62]))
    def point(v,theta,lift=0):
        phi=.035+v*(hem(theta)-.035)
        d=Vector((math.sin(phi)*math.sin(theta),-math.sin(phi)*math.cos(theta),math.cos(phi)))
        hit,n,_,_=bvh.ray_cast(centre+d*.22,-d,.28)
        if hit is None:hit=centre+Vector((d.x*.080,d.y*.086,d.z*.088));n=d
        swell=.0022+.0055*max(0,1-v)**.8+.0025*max(0,math.cos(theta))*max(0,math.sin(math.pi*v))
        return hit+n*(swell+lift)
    cap=grid('Hair.SweptScalp',27,96,lambda v,u:point(v,TAU*u),mats['hair'],col,True,1,.0012)
    cap['fit']='Follows resolved identity scalp; detachable head module'
    for side in (-1,1):
        grid('Hair.Sideburn.'+str(side),12,7,lambda v,u:point(1.0+.26*v,side*(1.26+(u-.5)*.19*(1-.35*v)),.0005),mats['hair'],col,False,1,.0008)
    # Swept, tapered ribbon clumps; the part sits left of centre. Fine groove
    # strands are geometry, not scalp texture or embedded source hair.
    for k in range(38):
        theta0=TAU*k/38
        def centre_theta(v):return theta0+.23*math.sin(math.pi*v)+.11*(1-v)
        width=.053 if k%3 else .063
        def lock(v,u):
            angle=centre_theta(v)+(u-.5)*width*(.6+.4*math.sin(math.pi*v))
            return point(.075+.920*v,angle,.0017*math.sin(math.pi*u)*math.sin(math.pi*v))
        grid('Hair.SweptLock.%02d'%k,22,5,lock,mats['hair_alt'] if k%5==0 else mats['hair'],col,False,1,0)
        for j in (-.24,0,.24):
            pts=[point(.12+.85*v,centre_theta(v)+j*width,.0015) for v in np.linspace(0,1,34)]
            cord('Hair.CombLine.%02d.%s'%(k,j),pts,.00012,mats['hair_line'],col)
    return cap
