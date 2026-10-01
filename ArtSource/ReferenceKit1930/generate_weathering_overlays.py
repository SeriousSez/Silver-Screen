"""Reusable, UV-independent Unity decal masks. Run with Blender --background.

Separate from Administration base materials: no masonry grain or normal changes.
Image bottom is v=0. Contact/runoff starts at v=1; rotate for ground contacts.
"""
from pathlib import Path
import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/SilverScreen/Environment/Weathering'
OUT.mkdir(parents=True, exist_ok=True)
n = 512
y, x = np.mgrid[0:n, 0:n].astype(float) / (n-1)
d = 1-y
rng = np.random.default_rng(193019)
grain = rng.uniform(.55, 1, (n,n))
edge = np.clip(np.minimum(x,1-x)/.13,0,1)
edge = edge*edge*(3-2*edge)
for name in ('Runoff', 'Junction', 'Contact'):
    # Broken contact deposit, not an oval or a full-surface noise layer.
    knots=rng.uniform(.18,1,29)
    breakup=np.interp(x,np.linspace(0,1,len(knots)),knots)
    # Low-frequency deposit variation plus fine grit, never a repeating comb.
    coarse=rng.uniform(.35,1,(33,33))
    rows=np.array([np.interp(np.linspace(0,1,n),np.linspace(0,1,33),row) for row in coarse])
    mottling=np.array([np.interp(np.linspace(0,1,n),np.linspace(0,1,33),col) for col in rows.T]).T
    # Irregular contact pockets with fine granulation and short gravity tails.
    depth = .08 + .16*breakup
    alpha = .55*np.exp(-d/depth)*breakup*grain
    if name == 'Runoff':
        for center, length, width in ((.22,.82,.006),(.69,.53,.009)):
            axis = center + .003*np.sin(d*41) + .0015*np.sin(d*113)
            trace = np.exp(-((x-axis)/(width*(1-.65*d)))**2)
            alpha += .42*trace*np.maximum(0,1-d/length)**1.2*grain
        for center in (.08,.34,.43,.58,.84,.92):
            length=.18+.16*(.5+.5*np.sin(center*83))
            trace=np.exp(-((x-center-.004*np.sin(d*32))/.014)**2)
            alpha+=.18*trace*np.maximum(0,1-d/length)*grain
    elif name == 'Junction':
        alpha += .25*np.exp(-d*7)*breakup*grain
    else:
        alpha += .42*np.exp(-d*3)*(breakup*.35+.65)*grain
    rgba = np.ones((n,n,4), dtype=np.float32)
    rgba[:,:,:3] = (.105,.084,.060)
    rgba[:,:,3] = np.clip(alpha*edge*(.65+.65*mottling)*1.7,0,.62)
    image = bpy.data.images.new('Weather_'+name,width=n,height=n,alpha=True)
    image.colorspace_settings.name = 'sRGB'
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(OUT / ('Weather_'+name+'.png'))
    image.file_format = 'PNG'
    image.save()
