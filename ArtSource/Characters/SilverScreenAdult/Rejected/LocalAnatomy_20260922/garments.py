"""Separate locally authored garment shells for the new character."""
import math
import bpy
import bmesh
from mathutils import Vector
from author_accessories import mesh, grid, thickness, subdiv, cord, move_collection
from anatomy import TORSO, ARM, LEG, tube, ellipsoid, join_sculpt, interp, gaussian

TAU=math.tau


def cut(obj,point,normal):
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,
                          plane_co=Vector(point),plane_no=Vector(normal),clear_outer=True,clear_inner=False)
    bm.to_mesh(obj.data); bm.free();obj.data.update()


def button(name,x,y,z,r,mat,col):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=(x,y,z))
    o=bpy.context.object;o.name=name;o.scale=(r,.0016,r)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat);move_collection(o,col)
    for p in o.data.polygons:p.use_smooth=True
    return o


def shirt_depth(z):return interp(TORSO,z,4)+.008


def make_shirt(mat,buttonmat):
    col='Garment_Shirt'
    rows=[]
    for z,x,y,rx,ry in TORSO:
        if z<.98 or z>1.501:continue
        # Looser shirt hem and sleeves retain the concept's tailored silhouette.
        rows.append((z,x,y,rx+.008+(max(0,1.17-z)*.035),ry+.008))
    def folds(p,t,q):
        x,y,z=p
        fold=.002*math.sin(43*z+5*t)*gaussian(z,1.08,.13)
        return x+fold*math.sin(t),y+fold*math.cos(t),z
    parts=[tube('Shirt_TorsoPattern',rows,'Z',mat,col,64,folds)]
    for sign in (-1,1):
        armrows=[(q,y,z,rx+.008,ry+.008) for q,y,z,rx,ry in ARM]
        armrows.append((.746,0,1.46,.024,.021))
        def sleevefold(p,t,q):
            x,y,z=p
            r=.0028*math.sin(q*91+2*math.sin(t))*gaussian(q,.49,.12)
            r+=.0016*math.sin(q*120+3*t)*gaussian(q,.69,.06)
            return sign*x,y+r*math.sin(t),z+r*math.cos(t)
        parts.append(tube('Shirt_SleevePattern',armrows,'X',mat,col,40,sleevefold))
    shirt=join_sculpt(parts,'Shirt_FittedShell',col,.0027)
    cut(shirt,(0,0,1.49),(0,0,1));cut(shirt,(0,0,1.005),(0,0,-1))
    cut(shirt,(.729,0,0),(1,0,0));cut(shirt,(-.729,0,0),(-1,0,0))
    thickness(shirt,.0012)
    shirt['module_status']='Independent shell; fit review required'
    # Standing collar and two pointed collar leaves are real separate cloth pieces.
    band=grid('Shirt_CollarStand',6,64,lambda v,t:((.049+.002*v)*math.sin(t),.001-(.050+.002*v)*math.cos(t),1.471+.027*v),mat,col)
    thickness(band,.001);subdiv(band)
    for sign in (-1,1):
        vertices=[(sign*.007,-.054,1.49),(sign*.047,-.037,1.495),(sign*.069,-.069,1.429),(sign*.03,-.082,1.40)]
        o=mesh('Shirt_CollarLeaf_'+str(sign),vertices,[(0,1,2,3)],mat,col)
        thickness(o,.0015)
        b=o.modifiers.new('Soft tailored edge','BEVEL');b.width=.0008;b.segments=2
        cuff=grid('Shirt_Cuff_'+str(sign),4,48,lambda v,t:(sign*(.688+.044*v),.025*math.sin(t),1.46+.022*math.cos(t)),mat,col)
        thickness(cuff,.0018);subdiv(cuff)
        button('Shirt_CuffButton_'+str(sign),sign*.715,-.026,1.46,.003,buttonmat,col)
    vs=[];fs=[]
    for j in range(22):
        z=1.012+.438*j/21
        yy=interp(TORSO,z,2)-shirt_depth(z)-.001
        for x in (-.009,.009):vs.append((x,yy,z))
    for j in range(21):a=j*2;fs.append((a,a+1,a+3,a+2))
    o=mesh('Shirt_ButtonPlacket',vs,fs,mat,col);thickness(o,.0007)
    for j in range(8):
        z=1.039+.054*j
        button('Shirt_FrontButton_'+str(j),0,interp(TORSO,z,2)-shirt_depth(z)-.003,z,.0034,buttonmat,col)
    return shirt


def vest_surface(v,t):
    angle=abs((t+math.pi)%TAU-math.pi)
    # V neckline front, scooped armholes, higher neck at rear.
    top=1.324+.157*min(angle/.63,1)
    top-=.135*math.exp(-((angle-math.pi/2)/.32)**2)
    if angle>2.45:top=1.482
    lower=1.048-.044*math.exp(-((angle-.37)/.30)**2)
    z=lower+(top-lower)*v
    rx=interp(TORSO,z,3)+.013
    ry=interp(TORSO,z,4)+.014
    # Vest lies outside shirt without simply sharing its renderer or geometry.
    return rx*math.sin(t),interp(TORSO,z,2)-ry*math.cos(t),z


def make_waistcoat(mat,buttons,trim):
    col='Garment_Waistcoat';segments=128;rings=22
    vs=[vest_surface(j/(rings-1),TAU*i/segments) for j in range(rings) for i in range(segments)]
    fs=[]
    for j in range(rings-1):
        for i in range(segments):
            k=(i+1)%segments;a=j*segments+i;b=j*segments+k
            fs.append((a,b,b+segments,a+segments))
    # Shoulder strips connect the front and back above each actual armhole.
    for sign in (-1,1):
        indexes=list(range(12,22))
        rows=[]
        for j in range(6):
            row=[];u=j/5
            for i in indexes:
                front=i if sign>0 else (segments-i)%segments
                back=(segments//2-i) if sign>0 else (segments//2+i)
                ia=(rings-1)*segments+front;ib=(rings-1)*segments+back
                if j==0:row.append(ia)
                elif j==5:row.append(ib)
                else:
                    p=Vector(vs[ia]).lerp(Vector(vs[ib]),u)
                    p.z+=.004*math.sin(math.pi*u)
                    row.append(len(vs));vs.append(tuple(p))
            rows.append(row)
        for j in range(5):
            for k in range(len(indexes)-1):fs.append((rows[j][k],rows[j][k+1],rows[j+1][k+1],rows[j+1][k]))
    vest=mesh('Waistcoat_FittedPanels',vs,fs,mat,col);thickness(vest,.0018);subdiv(vest)
    # Sewn edges remain geometry, not baked shadows.
    for v in (0,1):
        cord('Waistcoat_Edge_'+str(v),[vest_surface(v,TAU*i/256) for i in range(256)],.00055,trim,col,True)
    for j in range(5):
        z=1.069+.054*j;y=interp(TORSO,z,2)-interp(TORSO,z,4)-.017
        button('Waistcoat_Button_'+str(j),.006,y,z,.005,buttons,col)
    for sign in (-1,1):
        vertices=[]
        for z in (1.116,1.123):
            for u in (0,1):
                x=sign*(.053+.060*u)
                rx=interp(TORSO,z,3)+.014;ry=interp(TORSO,z,4)+.017
                y=interp(TORSO,z,2)-ry*math.sqrt(max(.01,1-(x/rx)**2))
                vertices.append((x,y,z+.004*u))
        o=mesh('Waistcoat_WeltPocket_'+str(sign),vertices,[(0,1,3,2)],mat,col);thickness(o,.001)
    cord('Waistcoat_BackAdjustment',[(x,.096,1.146) for x in (-.09,-.06,0,.06,.09)],.0045,trim,col)
    return vest


def make_trousers(mat,trim):
    col='Garment_Trousers';parts=[]
    # Full pair of independent trouser legs plus pelvis cloth, never anatomical skin.
    rows=[(.87,0,.014,.042,.065),(.919,0,.016,.125,.113),(.969,0,.014,.162,.122),(1.012,0,.011,.157,.107),(1.069,0,.008,.148,.096)]
    parts.append(tube('Trousers_PelvisPattern',rows,'Z',mat,col,64))
    for sign in (-1,1):
        legrows=[]
        for z,x,y,rx,ry in LEG:
            legrows.append((z,sign*x,y,max(rx+.01,.051),max(ry+.012,.051)))
        legrows.insert(0,(.036,sign*.102,-.012,.052,.058))
        def fold(p,t,z):
            x,y,z=p
            f=.0022*math.sin(94*z+2*t)*gaussian(z,.13,.08)
            f+=.0015*math.sin(81*z+3*t)*gaussian(z,.51,.09)
            return x+f*math.sin(t),y+f*math.cos(t),z
        parts.append(tube('Trousers_LegPattern',legrows,'Z',mat,col,48,fold))
    pants=join_sculpt(parts,'Trousers_FittedShell',col,.0028)
    cut(pants,(0,0,1.055),(0,0,1));cut(pants,(0,0,.064),(0,0,-1))
    thickness(pants,.0015)
    waist=grid('Trousers_Waistband',4,96,lambda v,t:(.151*math.sin(t),.009-.100*math.cos(t),1.028+.027*v),mat,col)
    thickness(waist,.0015)
    for sign in (-1,1):
        # Front crease is sculpted as a narrow low ridge, not a dark painted stripe.
        pts=[]
        for j in range(70):
            z=.088+.795*j/69;x=sign*interp(LEG,z,1)
            y=interp(LEG,z,2)-max(interp(LEG,z,4)+.013,.052)
            pts.append((x,y-.0007,z))
        cord('Trousers_PressedCrease_'+str(sign),pts,.00055,mat,col)
        cord('Trousers_PocketSeam_'+str(sign),[(sign*(.113+.029*u),-.064+.025*u,1.025-.106*u) for u in (0,.2,.4,.6,.8,1)],.0006,trim,col)
    for t in (-.55,.55,1.6,-1.6,2.65,-2.65):
        x=.154*math.sin(t);y=.009-.102*math.cos(t)
        o=mesh('Trousers_BeltLoop',[(x+d,y,z) for z in (1.012,1.056) for d in (-.003,.003)],[(0,1,3,2)],mat,col)
        thickness(o,.001)
    return pants
