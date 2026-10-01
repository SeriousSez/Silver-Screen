"""Canonical concept design on the source's authored topology, not its identity.

Metres in the baked rest space, +Y forward. This is a single designed identity;
the sculpt is an independently named shape key, not a population-wide modifier.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3]
def g(x,c,w): return math.exp(-((x-c)/w)**2)
def ramp(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def interpolate(x,keys):
    for (a,b),(c,d) in zip(keys,keys[1:]):
        if x<=c:return b+(d-b)*(x-a)/(c-a)
    return keys[-1][1]+x-keys[-1][0]

def apply_design(rig,body):
    points=[v.co.copy() for v in body.data.vertices]
    groups={v.index:v.name for v in body.vertex_groups}
    eye_centres={}
    for name in ('eye_l','eye_r'):
        coords=[v.co for v in body.data.vertices if any(groups[w.group]==name and w.weight>.95 for w in v.groups)]
        eye_centres[name]=sum(coords,Vector())/len(coords)
    centre=sum(eye_centres.values(),Vector())*.5;hx,hy,ez=centre
    def sculpt(p):
        x,y,z=p;dx=x-hx
        if z>1.475:
            front=ramp(.01,.070,y);side=1 if dx>=0 else -1
            # Deliberately rebuild the long narrow source lower face: squared
            # mandible, broad chin pad and more structured zygomatic plane.
            jaw=g(z,1.540,.040)*ramp(.022,.057,abs(dx))
            chin=g(z,1.513,.021)*g(dx,0,.045)
            cheek=g(z,1.604,.023)*g(abs(dx),.052,.025)*front
            x=hx+dx*1.10+side*(.0120*jaw+.0035*cheek)
            y+=.0065*chin*front+.0032*cheek
            y-=.0038*g(z,1.562,.026)*g(abs(dx),.048,.023)*front
            # A straighter, more intentional bridge and compact alar silhouette.
            nose=g(dx,0,.017)*g(z,1.603,.040)*ramp(.103,.128,y)
            y+=.0020*nose*g(z,1.610,.019)
            y-=.0027*nose*g(z,1.590,.010)
            x-=dx*.08*nose
            # Re-space the eyes and keep lids/globes in the same deformation.
            for eye in eye_centres.values():
                ex,ey,ze=eye;local=g(dx,ex-hx,.025)*g(z,ze,.021)*ramp(.05,.08,y)
                x+=((ex-hx)*.055)*local
                z-=(z-ze)*.14*local
                brow=g(dx,ex-hx,.027)*g(z,ze+.021,.012)*front
                y+=.0038*brow;z-=.0022*brow
            # Neutral, firmer lips replace the source's lifted smile corners.
            mouth=g(z,1.551,.020)*g(dx,0,.040)*ramp(.075,.105,y)
            x+=dx*.095*mouth
            z-=.0048*g(abs(dx),.027,.010)*mouth
            y+=.0018*g(z,1.542,.009)*mouth
            # Re-centre the new head on the torso, retaining natural asymmetry.
            x-=hx*ramp(1.475,1.525,z)
            z=interpolate(z,[(1.475,1.511),(1.525,1.550),(ez,1.657),(1.75,1.779)])
        else:
            # Broader shoulder girdle and chest, stronger torso taper. Hands
            # retain authored anatomy; move with their fitted forearm stations.
            ax=abs(x);sign=1 if x>=0 else -1
            arm=ramp(.135,.205,ax)
            shoulder=.038*g(z,1.402,.10)
            upper=.014*g(z,1.290,.13)
            waist=.004*g(z,1.065,.10)
            x+=sign*(shoulder+upper+waist)*ramp(.035,.13,ax)
            x+=sign*.014*arm*(1-ramp(1.18,1.36,z))
            if z>.72:
                y-=.028*arm*(1-ramp(.94,1.15,z))
                z-=.009*arm*(1-ramp(.95,1.20,z))
            # Authored trouser folds survive the period cut and leg ease.
            if .13<z<1.02 and ax<.21:
                c=.090*sign;ease=.10*g(z,.69,.28)+.10*g(z,.32,.18)
                x=c+(x-c)*(1+ease);y*=1+.06*g(z,.70,.28)
            z=interpolate(z,[(0,0),(.52,.52),(.94,.954),(1.06,1.075),(1.23,1.257),(1.41,1.454),(1.475,1.511)])
        return Vector((x,y,z))
    body.shape_key_add(name='Basis')
    key=body.shape_key_add(name='SilverScreen_CanonicalDesign');key.value=1
    for v,p in zip(key.data,points):v.co=sculpt(p)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for b in rig.data.edit_bones:b.head=sculpt(b.head.copy());b.tail=sculpt(b.tail.copy())
    bpy.ops.object.mode_set(mode='OBJECT')
    report={'study':'SilverScreen_CanonicalDesign','source_role':'technical construction; source identity is not the target',
        'design_target':'supplied SilverScreen character concept','vertices':len(points),
        'changed_vertices':sum((sculpt(p)-p).length>1e-6 for p in points),
        'max_displacement_m':max((sculpt(p)-p).length for p in points),
        'height_m':max(sculpt(p).z for p in points),'eye_centres_m':{n:list(sculpt(p)) for n,p in eye_centres.items()},
        'status':'new canonical identity; visual approval pending'}
    (ROOT/'ArtSource/Characters/Canonical/concept_sculpt_report.json').write_text(json.dumps(report,indent=2))
    return report
