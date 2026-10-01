"""Bake approved roof tile colour variation and normals onto the existing deck UVs.
No lighting is baked. Source objects and the frozen blend are never saved.
"""
import bpy,numpy as np,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];D=R/'Assets/SilverScreen/Environment/StageSchoolB/RoofSurfaces';D.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/StageSchoolA3/StageSchool_A3.blend'))
S=1024
atlas=bpy.data.images.load(str(R/'Assets/SilverScreen/Environment/StageSchoolA1/Textures/RoofClay_albedo.png'),check_existing=True)
a=np.array(atlas.pixels[:],dtype=np.float32).reshape(atlas.size[1],atlas.size[0],4)
records=[]
def save(name,pixels,srgb):
 im=bpy.data.images.new(name,width=S,height=S,alpha=True);im.colorspace_settings.name='sRGB' if srgb else 'Non-Color';im.pixels.foreach_set(pixels.astype(np.float32).ravel());im.file_format='PNG';im.filepath_raw=str(D/(name+'.png'));im.save();bpy.data.images.remove(im)
for c in bpy.data.collections:
 if c.library or not c.name.startswith('Roofs/') or c.name.count('/')!=1:continue
 deck=next((o for o in c.objects if o.name.startswith('Thick boarded hip roof')),None);tiles=next((o for o in c.objects if o.name.startswith('Batched overlapping clay courses')),None)
 if deck is None or tiles is None:continue
 vv=np.array([tuple(tiles.matrix_world@v.co) for v in tiles.data.vertices]);lo=vv[:,:2].min(axis=0)-.02;hi=vv[:,:2].max(axis=0)+.02
 base=np.zeros((S,S,3),np.float32);base[:,:,2]=1;high=base.copy();depth=np.full((S,S),-1e8);colour=np.ones((S,S,4),np.float32)*.73;colour[:,:,3]=1
 def raster(o,normal_only):
  me=o.data;me.calc_loop_triangles();w=o.matrix_world;nm=w.to_3x3().inverted().transposed();uv=me.uv_layers.active
  for tri in me.loop_triangles:
   pts=np.array([tuple(w@me.vertices[i].co) for i in tri.vertices]);n=np.array([tuple((nm@me.corner_normals[i].vector).normalized()) for i in tri.loops])
   if n[:,2].mean()<.08:continue
   px=(pts[:,:2]-lo)/(hi-lo)*(S-1);mn=np.maximum(np.floor(px.min(axis=0)).astype(int),0);mx=np.minimum(np.ceil(px.max(axis=0)).astype(int),S-1)
   if np.any(mx<mn):continue
   xx,yy=np.meshgrid(np.arange(mn[0],mx[0]+1),np.arange(mn[1],mx[1]+1));v0=px[1]-px[0];v1=px[2]-px[0];den=v0[0]*v1[1]-v1[0]*v0[1]
   if abs(den)<1e-8:continue
   qx=xx-px[0,0];qy=yy-px[0,1];b=(qx*v1[1]-v1[0]*qy)/den;cc=(v0[0]*qy-qx*v0[1])/den;aa=1-b-cc;mask=(aa>=-1e-5)&(b>=-1e-5)&(cc>=-1e-5)
   weights=np.stack([aa,b,cc],axis=-1);z=weights@pts[:,2]
   if not normal_only:mask&=z>depth[yy,xx]
   iy=yy[mask];ix=xx[mask];weights=weights[mask];norm=weights@n;norm/=np.maximum(np.linalg.norm(norm,axis=1,keepdims=True),1e-8)
   if normal_only:base[iy,ix]=norm
   else:
    depth[iy,ix]=z[mask];high[iy,ix]=norm
    if uv:
     coords=weights@np.array([tuple(uv.data[i].uv) for i in tri.loops]);tx=np.clip((coords[:,0]*a.shape[1]).astype(int),0,a.shape[1]-1);ty=np.clip((coords[:,1]*a.shape[0]).astype(int),0,a.shape[0]-1);colour[iy,ix,:3]=a[ty,tx,:3]
 raster(deck,True);high=base.copy();raster(tiles,False)
 tangent=np.zeros_like(base);tangent[:,:,0]=1;tangent-=base*base[:,:,:1];tangent/=np.maximum(np.linalg.norm(tangent,axis=2,keepdims=True),1e-8);bitangent=np.cross(base,tangent)
 normal=np.ones((S,S,4),np.float32);normal[:,:,:3]=np.stack([(high*tangent).sum(2),(high*bitangent).sum(2),(high*base).sum(2)],axis=2)*.5+.5
 name=c.name.split('/')[1];save(name+'_Albedo',colour,True);save(name+'_Normal',normal,False);records.append(dict(name=name,min=lo.tolist(),max=hi.tolist()));print('Baked roof',name,flush=True)
(D/'roof_surfaces.json').write_text(json.dumps(records,indent=2))
