"""Offline, reusable URP Lit surface maps. No light, AO or shadow information.

RGB is a near-neutral albedo multiplier; alpha is normalized smoothness.
Only intrinsic material variation lives here. Localized weathering uses Unity decals.
"""
import bpy
import numpy as np

SPAN = 24.0
PROFILES = {'Stucco':'Stucco', 'Limestone':'Stone', 'WindowSteel':'Paint',
            'CanopyEnamel':'Paint', 'Bronze':'Bronze', 'Roof':'Roof'}

def noise(u,v,seed,scale):
    """Band-limited aperiodic variation, not pixel noise or a repeating sine grid."""
    rng=np.random.default_rng(seed)
    total=np.zeros_like(u)
    for _ in range(12):
        angle=rng.uniform(0,2*np.pi)
        frequency=rng.uniform(.65,1.6)/scale
        total+=np.sin((u*np.cos(angle)+v*np.sin(angle))*frequency*2*np.pi+rng.uniform(0,2*np.pi))
    return np.tanh(total/3)

def mineral_height(size,seed,scales):
    """Periodic isotropic grain: radially filtered white noise, not plane waves.

    Each octave has an independent seed. Gaussian radii are in texels; the
    0.5-metre detail tile makes these millimetre-scale surface irregularities.
    Filtering in frequency space keeps derivatives continuous across tile edges.
    """
    rng=np.random.default_rng(seed)
    frequency=np.fft.fftfreq(size)
    radius_squared=frequency[:,None]**2+frequency[None,:]**2
    height=np.zeros((size,size))
    for sigma,weight in scales:
        spectrum=np.fft.fft2(rng.normal(size=(size,size)))
        filtered=np.fft.ifft2(spectrum*np.exp(-2*np.pi**2*sigma**2*radius_squared)).real
        height+=weight*filtered/max(filtered.std(),1e-8)
    return height

def generate(folder,guid):
    size=512
    v,u=np.mgrid[0:size,0:size]
    u=(u+.5)/size*SPAN-SPAN/2
    v=(v+.5)/size*SPAN-SPAN/2
    broad=noise(u,v,1930,3.5)
    medium=noise(u,v,1931,.65)
    fine=noise(u,v,1932,.13)
    for index,profile in enumerate(('Stucco','Stone','Paint','Bronze','Roof')):
        surface=medium if profile in ('Paint','Bronze') else broad
        amplitude={'Stucco':.012,'Stone':.008,'Paint':.006,'Bronze':.008,'Roof':.012}[profile]
        # Retain the previous .96 multiplier for masonry/roof; metals were untextured.
        center=.96 if profile in ('Stucco','Stone','Roof') else .995
        tone=center+amplitude*surface+.003*medium
        smooth=.90+.055*medium+.018*fine
        rgba=np.ones((size,size,4),dtype=np.float32)
        rgba[:,:,:3]=np.clip(tone,.92,1)[:,:,None]
        rgba[:,:,3]=np.clip(smooth,.78,1)
        save(folder,'Surface_'+profile,rgba,False,guid)
    # Shared tileable fine normals, independent of the architecture masks.
    size=256
    for profile,slope_rms,seed,scales in (
        ('Stucco',.06,1934,((.7,.55),(1.5,.35),(3.0,.10))),
        ('Stone',.025,1971,((.55,.75),(1.1,.20),(2.0,.05)))):
        height=mineral_height(size,seed,scales)
        dx=np.roll(height,-1,1)-np.roll(height,1,1)
        dy=np.roll(height,-1,0)-np.roll(height,1,0)
        # Normalize slope energy, not peak height: rare grains cannot amplify
        # the entire surface. Shader strengths remain .22 stucco / .14 stone.
        strength=slope_rms/max(np.sqrt(np.mean(dx*dx+dy*dy)),1e-8)
        length=np.sqrt((strength*dx)**2+(strength*dy)**2+1)
        rgba=np.ones((size,size,4),dtype=np.float32)
        rgba[:,:,0]=.5-.5*strength*dx/length
        rgba[:,:,1]=.5-.5*strength*dy/length
        rgba[:,:,2]=.5+.5/length
        save(folder,'Surface_'+profile+'Normal',rgba,True,guid)

def save(folder,name,rgba,normal,guid):
    image=bpy.data.images.get(name) or bpy.data.images.new(name,width=rgba.shape[1],height=rgba.shape[0],alpha=True)
    image.colorspace_settings.name='Non-Color' if normal else 'sRGB'
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw=str(folder/(name+'.png'))
    image.file_format='PNG'
    image.save()
    meta=folder/(name+'.png.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+guid(folder/(name+'.png'))+f'''
TextureImporter:
  serializedVersion: 13
  internalIDToNameTable: []
  externalObjects: {{}}
  mipmaps:
    enableMipMap: 1
    sRGBTexture: {0 if normal else 1}
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: {1 if normal else 0}
    flipGreenChannel: 0
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: 0
    wrapU: {0 if normal else 1}
    wrapV: {0 if normal else 1}
    wrapW: {0 if normal else 1}
  nPOTScale: 0
  textureType: {1 if normal else 0}
  textureShape: 1
  alphaSource: 1
  alphaIsTransparency: 0
  userData:
''',encoding='utf8')
