"""Four small stucco hairlines in one reusable Unity decal atlas (Blender CLI).

No base materials, normals or building geometry are touched. Each 256px tile
starts at (0.20,0.98); the thin path tapers away from that stress origin.
"""
from pathlib import Path
import bpy
import numpy as np

out = Path(__file__).resolve().parents[2] / 'Assets/SilverScreen/Environment/Weathering/HairlineCracks.png'
n = 256
y,x = np.mgrid[0:n,0:n].astype(float)/(n-1)
atlas = np.zeros((512,512,4),dtype=np.float32)
atlas[:,:,:3] = (.22,.195,.16)
for i in range(4):
    rng = np.random.default_rng(731+i)
    steps = 17+i*2
    py = np.linspace(.98,.12,steps)
    px = .20 + np.linspace(0,.36+i*.025,steps) + rng.uniform(-.025,.025,steps)
    px[0] = .20
    alpha = np.zeros((n,n))
    for j in range(steps-1):
        dx,dy=px[j+1]-px[j],py[j+1]-py[j]
        t=np.clip(((x-px[j])*dx+(y-py[j])*dy)/(dx*dx+dy*dy),0,1)
        dist=np.sqrt((x-px[j]-t*dx)**2+(y-py[j]-t*dy)**2)
        width=.0048*(1-.40*j/steps)*(.85+.25*np.sin(j*2.3+i))
        segment=np.exp(-(dist/width)**2)*.82*(1-.55*j/steps)
        alpha=np.maximum(alpha,segment)
    # Only two variants have a tiny subsidiary branch, not a damage network.
    if i in (1,3):
        j=6
        dx,dy=-.105,-.13
        t=np.clip(((x-px[j])*dx+(y-py[j])*dy)/(dx*dx+dy*dy),0,1)
        dist=np.sqrt((x-px[j]-t*dx)**2+(y-py[j]-t*dy)**2)
        alpha=np.maximum(alpha,np.exp(-(dist/.0030)**2)*.48*(1-t))
    alpha*=rng.uniform(.8,1,(n,n))
    atlas[(i//2)*n:(i//2+1)*n,(i%2)*n:(i%2+1)*n,3]=alpha
image=bpy.data.images.new('HairlineCracks',width=512,height=512,alpha=True)
image.colorspace_settings.name='sRGB'
image.pixels.foreach_set(atlas.ravel())
image.filepath_raw=str(out)
image.file_format='PNG'
image.save()
