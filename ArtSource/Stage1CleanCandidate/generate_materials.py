"""Deterministic tileable material response, without baked lighting or weathering.
Run with the workspace Python (numpy + Pillow). Shared tiles use metre-scaled UVs.
RGB carries subtle albedo variation; alpha modulates smoothness in URP Lit.
"""
from pathlib import Path
import numpy as np
from PIL import Image
import json,shutil

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
OUT=HERE/'Textures';UNITY=ROOT/'Assets/SilverScreen/Environment/Stage1CleanCandidate/Textures'
OUT.mkdir(exist_ok=True);UNITY.mkdir(exist_ok=True)
N=1024;rng=np.random.default_rng(1930)
fy=np.fft.fftfreq(N)[:,None];fx=np.fft.fftfreq(N)[None,:]
def noise(scale,stretch=1):
    a=rng.normal(size=(N,N))
    filt=np.exp(-((fx*scale)**2+(fy*scale*stretch)**2)*2)
    result=np.fft.ifft2(np.fft.fft2(a)*filt).real
    return result/(result.std()+1e-8)
def tile(name,variation,height_mm,tile_m,anisotropy=1):
    fine=noise(2,anisotropy);medium=noise(13,anisotropy);broad=noise(85,1)
    signal=.52*fine+.31*medium+.17*broad
    if name=='timber':signal=.30*noise(1.2,55)+.35*noise(4,32)+.25*noise(18,15)+.1*broad
    if name=='cloth':
        y,x=np.mgrid[:N,:N]
        signal=.70*signal+.13*np.sin(x*2*np.pi/4)+.13*np.sin(y*2*np.pi/4)
    grey=np.clip(.94+variation*signal,.68,1)
    smooth=np.clip(.86+.06*medium+.025*broad,.65,1)
    rgba=np.empty((N,N,4),dtype=np.uint8)
    for i in range(3):rgba[:,:,i]=(grey*255).astype(np.uint8)
    rgba[:,:,3]=(smooth*255).astype(np.uint8)
    Image.fromarray(rgba).save(OUT/(name+'_albedo.png'))
    height=signal*(height_mm/1000)
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))/(2*tile_m/N)
    dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))/(2*tile_m/N)
    vectors=np.dstack((-dx,dy,np.ones_like(dx)))
    vectors/=np.linalg.norm(vectors,axis=2,keepdims=True)
    Image.fromarray(np.clip((vectors*.5+.5)*255,0,255).astype(np.uint8)).save(OUT/(name+'_normal.png'))
    for suffix in ['_albedo.png','_normal.png']:shutil.copy2(OUT/(name+suffix),UNITY/(name+suffix))
for args in [('stucco',.019,.13,3),('stone',.012,.06,2),('concrete',.025,.10,2.4),('metal',.009,.023,2.0),('timber',.055,.11,2.0,35),('paint',.009,.024,1.5),('cloth',.018,.08,1.4),('floor',.026,.033,4)]:tile(*args)
tile('roof_metal',.010,.019,2.5,2.5)
(HERE/'material_texture_report.json').write_text(json.dumps({'resolution':N,'seed':1930,'tiles':9,'maps':['albedo with smoothness multiplier in alpha','tangent-space normal'],'roof_metal':'Subtle elongated rolling grain in surface normals; no directional illumination or highlight painted in RGB','localized_weathering':False,'baked_lighting':False},indent=2))
print('Generated 9 shared PBR texture sets, 18 maps, with matching Unity copies.')
