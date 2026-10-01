"""Original fitted garments over the approved, complete male foundation."""
import bpy,math
import numpy as np
from mathutils import Vector
from geometry import *

def shell_patch(body,name,mask,positions,mat,col):
    selected=[p for p in body.data.polygons if mask[np.array(p.vertices)].all()]
    ids=sorted({i for p in selected for i in p.vertices});remap={v:i for i,v in enumerate(ids)}
    o=mesh(name,[positions[i] for i in ids],[[remap[i] for i in p.vertices] for p in selected],mat,col,1,.0012)
    uv=o.data.uv_layers.new(name='FoundationFitUV_pending_relaxation')
    for dst,src in zip(o.data.polygons,selected):
        for di,si in zip(dst.loop_indices,src.loop_indices):uv.data[di].uv=body.data.uv_layers[0].data[si].uv
    o['fit_topology']='New garment fit over approved CC-derived male; separate copy with ease and cloth shape'
    return o

def shirt(body,land,mats):
    col='Module.Shirt';p=np.array([v.co[:] for v in body.data.vertices]);n=np.array([v.normal[:] for v in body.data.vertices]);q=p+n*.010
    x,y,z=p.T;mask=(z>1.054)|((abs(x)>.225)&(z>1.008))
    # Loose period shirt sleeves. Folds follow the local arm frame and concentrate
    # at elbow/cuff; they are not painted anatomical lines.
    for side,label in [(1,'L'),(-1,'R')]:
        sh=np.array(land[label]['shoulder']);el=np.array(land[label]['elbow']);wr=np.array(land[label]['wrist'])
        axis,u,v=frame(sh,wr);delta=p-sh;t=delta@axis/np.linalg.norm(wr-sh)
        arm=(p[:,0]*side>.190)&(p[:,2]>1.005)&(p[:,2]<1.492)
        ang=np.arctan2(delta@v,delta@u)
        fold=(.0033*np.sin(t*math.pi*11+ang*1.6)*np.exp(-((t-.59)/.22)**2)
            +.0036*np.sin(t*math.pi*21-ang*1.7)*np.exp(-((t-.91)/.15)**2))
        ease=.007+.007*np.sin(np.clip(t,0,1)*math.pi)+fold
        q[arm]+=n[arm]*ease[arm,None]
    o=shell_patch(body,'Shirt.BodyAndSleeves',mask,q,mats['shirt'],col)
    head=bpy.data.objects['SS_MaleHead_Foundation_v1']
    hp=np.array([v.co[:] for v in head.data.vertices]);hn=np.array([v.normal[:] for v in head.data.vertices])
    shell_patch(head,'Shirt.NeckYoke',hp[:,2]<1.501,hp+hn*.010,mats['shirt'],col)
    bvh=tree(o)
    # Cuffs follow the forearm axes rather than horizontal bracelets.
    for label in ('L','R'):
        wr=np.array(land[label]['wrist']);el=np.array(land[label]['elbow']);axis,u,v=frame(el,wr)
        def cuff(t,s):
            ang=TAU*s;c=wr-axis*(.003+.052*t)
            return c+u*(.038*math.cos(ang))+v*(.033*math.sin(ang))
        grid('Shirt.Cuff.'+label,5,48,cuff,mats['shirt'],col,True,1,.0014)
        for edge in (0,1):cord('Cuff.Stitch.'+label+str(edge),[cuff(edge,i/80) for i in range(80)],.00035,mats['shirt_seam'],col,True)
        button('Cuff.Button.'+label,wr-axis*.026+v*.034,.0037,mats['button_light'],col,tuple(v))
    # A separate standing collar and two shaped, pointed leaves.
    def band(v,t):
        a=TAU*t;return (.059*math.sin(a),.020-.061*math.cos(a),1.478+.034*v-.007*math.cos(a))
    grid('Shirt.CollarStand',5,72,band,mats['shirt'],col,True,1,.0014)
    for side in (-1,1):
        top_inner=np.array((side*.008,-.051,1.506));top_outer=np.array((side*.054,-.014,1.508))
        tip=np.array((side*.043,-.100,1.426));outer=np.array((side*.084,-.067,1.471))
        def leaf(v,u):
            a=(1-u)*top_inner+u*top_outer;b=(1-u)*tip+u*outer
            q=(1-v)*a+v*b;q[1]-=.004*math.sin(math.pi*u)*math.sin(math.pi*v)
            hit=bvh.ray_cast(Vector((q[0],-.7,q[2])),Vector((0,1,0)))[0]
            if hit is not None:q[1]=min(q[1],hit.y-.0025)
            return q
        grid('Shirt.PointCollar.'+str(side),9,8,leaf,mats['shirt'],col,False,1,.0015)
        for edge in (0,1):cord('Collar.Edge.'+str(side)+str(edge),[leaf(v,edge) for v in np.linspace(0,1,22)],.00045,mats['shirt_seam'],col)
    grid('Shirt.FrontPlacket',22,5,lambda v,u:((u-.5)*.019,project_y(bvh,(u-.5)*.019,1.065+.389*v,padding=.0017),1.065+.389*v),mats['shirt'],col,False,1,.0008)
    for z in np.arange(1.092,1.450,.065):button('Shirt.Button.'+str(round(z,3)),(0,project_y(bvh,0,z,padding=.003),z),.0032,mats['button_light'],col)
    return o

def waistcoat(shirt_obj,mats):
    col='Module.Waistcoat';bvh=tree(shirt_obj);panels=[];fn={}
    widths={}
    def side_width(z):
        key=round(float(z),5)
        if key not in widths:
            pos=Vector((.7,.014,z));hits=[]
            for _ in range(16):
                h=bvh.ray_cast(pos,Vector((-1,0,0)))[0]
                if h is None or h.x<.04:break
                if .06<h.x<.22:hits.append(h.x)
                pos=h-Vector((.0004,0,0))
            widths[key]=(min(hits)+.010) if hits else float(np.interp(z,[1.084,1.18,1.348],[.155,.159,.188]))
        return widths[key]
    topx=[0,.063,.101,.132,.162,.179];topz=[1.312,1.427,1.480,1.471,1.388,1.348]
    us=[0,.30,.50,.65,.81,1]
    for side in (-1,1):
        def front(v,u,side=side):
            xt=np.interp(u,us,topx);zt=np.interp(u,us,topz)
            xb=.150*u+(-.007 if side==1 else -.002)*(1-u)
            zb=1.068-.027*math.sin(math.pi*u)+.016*u
            x=side*((1-v)*xb+v*xt);z=(1-v)*zb+v*zt
            edge=smooth(.78,1,u);x=x*(1-edge)+side*side_width(z)*edge
            y=project_y(bvh,x,z,padding=.0055+.008*math.exp(-.5*(x/.037)**2),fallback=.085)
            y=y*(1-edge)+.014*edge
            return np.array((x,y,z))
        o=grid('Waistcoat.Front.'+str(side),31,25,front,mats['vest'],col,False,1,.0020);panels.append(o);fn[side]=front
        # Front edge and lower hem. Small rolled edge, no false lapel.
        cord('Waistcoat.Opening.'+str(side),[front(v,0)+np.array((0,-.0008,0)) for v in np.linspace(0,1,90)],.0007,mats['vest_edge'],col)
        cord('Waistcoat.NeckArmEdge.'+str(side),[front(1,u)+np.array((0,-.0008,0)) for u in np.linspace(0,1,95)],.00065,mats['vest_edge'],col)
        cord('Waistcoat.Hem.'+str(side),[front(0,u)+np.array((0,-.0008,0)) for u in np.linspace(0,1,85)],.00065,mats['vest_edge'],col)
        # Functional-looking welt pockets shaped on the fitted front surface.
        for z0,x0,width in [(1.139,.094,.075),(1.343,.108,.062)]:
            if z0>1.3 and side<0:continue
            pts=[]
            for xabs in np.linspace(x0-width/2,x0+width/2,28):
                x=side*xabs;z=z0+.055*(xabs-x0);y=project_y(bvh,x,z,padding=.009)
                pts.append((x,y,z))
            cord('Waistcoat.PocketWelt.'+str(side)+str(z0),pts,.0023,mats['vest_edge'],col)
            cord('Waistcoat.PocketMouth.'+str(side)+str(z0),[(x,y+.0004,z+.0025) for x,y,z in pts],.00065,mats['seam_dark'],col)
    def back(v,u):
        s=2*u-1;a=abs(s);xt=np.interp(a,[0,.50,.72,1],[0,.103,.151,.179]);zt=np.interp(a,[0,.35,.66,.84,1],[1.479,1.481,1.471,1.380,1.348])
        x=np.sign(s)*((1-v)*.150*a+v*xt);z=(1-v)*1.084+v*zt
        edge=smooth(.78,1,a);x=x*(1-edge)+np.sign(s)*side_width(z)*edge
        y=project_y(bvh,x,z,False,.0055,.09)*(1-edge)+.014*edge
        return np.array((x,y,z))
    panels.append(grid('Waistcoat.Back',31,37,back,mats['vest_back'],col,False,1,.0015))
    # Front and back terminate at the same curved side seam.
    # Fitted side underpanels bridge the actual cotton-shirt cross section. They
    # preserve underarm ease without leaving the shirt exposed below the armhole.
    for side in (-1,1):
        def underpanel(v,u):
            theta=.80+1.52*u;z_top=1.348+.079*abs(math.cos(theta))
            z=1.080+(z_top-1.080)*v;direction=Vector((side*math.sin(theta),-math.cos(theta),0))
            centre=Vector((0,.014,z));pos=centre+direction*.66;hits=[]
            for _ in range(18):
                h=bvh.ray_cast(pos,-direction)[0]
                if h is None:break
                radius=(h-centre).dot(direction)
                if radius<.045:break
                if abs(h.x)<.229:hits.append(radius)
                pos=h-direction*.00035
            radius=(min(hits) if hits else .160)+.007
            return centre+direction*radius
        panels.append(grid('Waistcoat.SideUnderpanel.'+str(side),30,20,underpanel,mats['vest'],col,False,1,.0017))
    for z in (1.104,1.151,1.198,1.245,1.292):
        y=project_y(bvh,0,z,padding=.017)
        button('Waistcoat.Button.'+str(z),(0,y,z),.0041,mats['button_dark'],col)
        cord('Waistcoat.Buttonhole.'+str(z),[(.005,y+.001,z),(.011,y+.001,z)],.0006,mats['seam_dark'],col)
    cord('Waistcoat.BackSeam',[back(v,.5)+np.array((0,.0008,0)) for v in np.linspace(0,1,80)],.00055,mats['vest_edge'],col)
    # Back cinch strap is a detachable detail of this waistcoat module.
    grid('Waistcoat.BackCinch',5,24,lambda v,u:((u-.5)*.176,project_y(bvh,(u-.5)*.176,1.139+.017*v,False,.009),1.139+.017*v),mats['vest'],col,False,1,.0015)
    y=project_y(bvh,0,1.148,False,.013)
    cord('Waistcoat.CinchBuckle',[(-.011,y,1.140),(.011,y,1.140),(.011,y,1.156),(-.011,y,1.156)],.0011,mats['metal'],col,True)
    return panels

def trousers(body,mats):
    col='Module.Trousers';p=np.array([v.co[:] for v in body.data.vertices]);n=np.array([v.normal[:] for v in body.data.vertices]);q=p+n*.012
    x,y,z=p.T;mask=(z<1.084)&(z>.118)&(abs(x)<.243)
    # Tailored leg sections retain a complete crotch and add garment ease.
    for side in (-1,1):
        ix=(x*side>0)&(z<.920);zz=z[ix]
        cx=side*np.interp(zz,[.12,.515,.988],[.111,.100,.088]);cy=np.interp(zz,[.12,.515,.988],[.005,-.012,.011])
        dx=x[ix]-cx;dy=y[ix]-cy;theta=np.arctan2(dy,dx)
        rx=np.interp(zz,[.12,.25,.515,.70,.92],[.055,.056,.065,.079,.086])
        ry=np.interp(zz,[.12,.25,.515,.70,.92],[.072,.063,.072,.084,.096])
        fold=.0038*np.sin((zz-.12)*98+1.2*np.sin(theta))*np.exp(-((zz-.190)/.075)**2)
        fold+=.0020*np.sin(zz*56+theta*2)*np.exp(-((zz-.52)/.11)**2)
        nx=cx+(rx+fold)*np.cos(theta);ny=cy+(ry+fold)*np.sin(theta)
        crease=np.exp(-.5*(np.cos(theta)/.13)**2)*np.sign(np.sin(theta))*.0030
        ny+=crease
        # The relaxed trouser section must enclose the real underlying anatomy.
        # Preserve every body face; use measured radial clearance, not masking.
        source_radius=np.sqrt(dx*dx+dy*dy)
        target_radius=np.sqrt((nx-cx)**2+(ny-cy)**2)
        clearance=np.maximum(1,(source_radius+.014)/np.maximum(target_radius,1e-6))
        nx=cx+(nx-cx)*clearance;ny=cy+(ny-cy)*clearance
        t=1-smooth(.820,.920,zz);q[ix,0]=nx*t+q[ix,0]*(1-t);q[ix,1]=ny*t+q[ix,1]*(1-t)
    o=shell_patch(body,'Trousers.TailoredShell',mask,q,mats['trouser'],col);bvh=tree(o)
    def waistband(v,u):
        a=TAU*u;return (.153*math.sin(a),.012-.104*math.cos(a),1.048+.029*v)
    grid('Trousers.Waistband',5,96,waistband,mats['trouser'],col,True,1,.0018)
    for side in (-1,1):
        points=[]
        for t in np.linspace(0,1,34):
            x=side*(.102+.037*t);z=1.058-.105*t;points.append((x,project_y(bvh,x,z,padding=.0018),z))
        cord('Trousers.SlantPocket.'+str(side),points,.0011,mats['trouser_seam'],col)
        for z0 in (.998,):
            pts=[]
            for x in np.linspace(.050,.123,27):pts.append((side*x,project_y(bvh,side*x,z0,False,.002),z0+.015*(x-.05)))
            cord('Trousers.BackWelt.'+str(side),pts,.0015,mats['trouser_seam'],col)
        # Pressed front/back leg lines reinforce the tailored silhouette.
        for isfront in (True,False):
            pts=[]
            for zz in np.linspace(.157,.885,85):
                xx=side*np.interp(zz,[.12,.515,.988],[.111,.100,.088]);pts.append((xx,project_y(bvh,xx,zz,isfront,.0008),zz))
            cord('Trousers.PressCrease.'+str(side)+str(isfront),pts,.0004,mats['trouser_seam'],col)
    pts=[(.009,project_y(bvh,.009,z,padding=.002),z) for z in np.linspace(.930,1.065,28)]
    cord('Trousers.FlySeam',pts,.0007,mats['trouser_seam'],col)
    for theta in [-2.4,-1.2,-.35,.35,1.2,2.4,math.pi]:
        grid('Trousers.BeltLoop.'+str(theta),5,4,lambda v,u:(.155*math.sin(theta+(u-.5)*.048),.012-.106*math.cos(theta+(u-.5)*.048),1.037+.042*v),mats['trouser'],col,False,1,.0015)
    return o

def accessories(root,shirt_obj,mats):
    src=root.parent/'SS_Adult_Accessories.blend';cols=['Hat_Fedora','Shoe_Left','Shoe_Right','Accessory_Belt']
    with bpy.data.libraries.load(str(src),link=False) as (a,b):b.collections=cols
    for c in b.collections:
        bpy.context.scene.collection.children.link(c);c.hide_render=False;c.hide_viewport=False
        c.name={'Hat_Fedora':'Module.Fedora','Shoe_Left':'Module.Shoe.L','Shoe_Right':'Module.Shoe.R','Accessory_Tie':'Module.Tie','Accessory_Belt':'Module.Belt'}[c.name]
    bvh=tree(shirt_obj)
    for c in b.collections:
        for o in c.all_objects:
            o['module']=c.name;o['rig_status']='UNRIGGED_VISUAL_GATE';o['source']='Fitted copy of preserved original SilverScreen accessory'
            if 'Shoe.' in c.name:
                side=1 if c.name.endswith('.L') else -1
                # A small outward and vertical fit around actual foot geometry;
                # the anatomical feet themselves remain complete and unchanged.
                def fit(p):
                    q=np.array(p);q[0]=side*.111+(q[0]-side*.102)*1.10;q[1]=-.025+(q[1]+.025)*1.10;q[2]*=1.08;return q
            elif c.name=='Module.Fedora':
                def fit(p):
                    q=np.array(p);v=np.clip((q[2]-1.730)/.127,0,1)
                    if 'Crown' in o.name or 'Top' in o.name:
                        front=max(0,-q[1]/.11)
                        pinch=np.exp(-((abs(q[0])-.038)/.020)**2)*front
                        q[0]*=(1-.065*v);q[1]+=.012*pinch*math.sin(math.pi*v)**2
                        q[2]=1.730+(q[2]-1.730)*.89
                        q[2]-=.010*front*front*v**3
                        q[1]+=.007*front*v*v
                        if 'Top' in o.name:q[2]-=.005*np.exp(-(q[0]/.034)**2)
                    if 'Brim' in o.name:
                        radial=np.clip((np.sqrt((q[0]/.091)**2+(q[1]/.110)**2)-1)/.67,0,1)
                        q[2]+=radial*radial*(.010*(abs(q[0])/.156)**2+.007*q[1]/.17)
                    q[0]*=1.07;q[1]=q[1]*1.08+.017;q[2]-=.015;return q
            elif c.name=='Module.Belt':
                def fit(p):
                    q=np.array(p);q[0]*=1.06;q[1]=q[1]*1.20+.012;q[2]+=.034;return q
            else:
                def fit(p):
                    q=np.array(p);q[2]+=.042
                    q[1]=project_y(bvh,q[0],min(q[2],1.475),padding=.007,fallback=.06)+(q[1]+.09)*.18
                    return q
            # Bake the object's local coordinates into the fitted module geometry.
            if o.type=='MESH':
                for v in o.data.vertices:v.co=fit(o.matrix_world@v.co)
            elif o.type=='CURVE':
                for sp in o.data.splines:
                    for p in sp.points:p.co=(*fit(o.matrix_world@Vector(p.co[:3])),1)
            o.matrix_world.identity()
            if c.name=='Module.Fedora':
                assign(o,mats['felt'] if any(x in o.name for x in ('Felt','Top','Brim')) and 'Bound' not in o.name else mats['ribbon'])
            elif c.name=='Module.Tie':assign(o,mats['tie'])
            elif c.name=='Module.Belt':assign(o,mats['metal'] if 'Buckle' in o.name else mats['leather'])
            else:
                mat=mats['leather']
                if any(s in o.name for s in ('Sole','Heel')):mat=mats['sole']
                elif 'Lace' in o.name:mat=mats['ribbon']
                elif 'Eyelet' in o.name:mat=mats['metal']
                elif 'Seam' in o.name:mat=mats['leather_seam']
                assign(o,mat)
    # The canonical tie is authored to the new collar and V opening. Preserved
    # earlier accessory studies remain unchanged in their original source file.
    col='Module.Tie'
    def blade(v,u):
        z=1.207+.274*v;w=float(np.interp(v,[0,.08,.30,.78,1],[.001,.023,.020,.010,.008]));x=(u-.5)*2*w
        return (x,project_y(bvh,x,min(z,1.475),padding=.0072,fallback=.056)-.001*math.sin(math.pi*u),z)
    grid('Tie.CanonicalBlade',24,7,blade,mats['tie'],col,False,1,.0012)
    grid('Tie.FourInHand',9,32,lambda v,u:((.008+.005*v+.0015*math.sin(v*math.pi))*math.sin(TAU*u),-.067-(.006+.001*math.sin(v*math.pi))*math.cos(TAU*u),1.468+.038*v+.001*math.sin(TAU*u)),mats['tie'],col,True,1,.0012)
    return b.collections
