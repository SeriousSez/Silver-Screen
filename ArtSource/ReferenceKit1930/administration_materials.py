"""Small authoring-only material bridge for the administration reference asset.

Generated PNGs and URP materials carry the Blender surface response into Unity.
No editor scripts, runtime shader code or project render settings are changed.
"""
import bpy
import numpy as np
import uuid
import re
from pathlib import Path
from surface_detail import PROFILES, SPAN, generate

def guid(path):
    meta=Path(str(path)+'.meta')
    if meta.exists():
        match=re.search(r'^guid: ([0-9a-f]+)',meta.read_text(),re.M)
        if match: return match.group(1)
    return uuid.uuid5(uuid.NAMESPACE_URL,'silverscreen-administration-v2/'+path.name).hex

def linear_to_srgb(value):
    """Unity Color properties are serialized as sRGB; Blender node RGB is linear."""
    return 12.92*value if value <= 0.0031308 else 1.055*value**(1/2.4)-0.055

def prepare(root,materials,preserve_existing=False,preserve_textures=False):
    folder=root/'Assets/SilverScreen/Environment/ReferenceKit/AdministrationMaterials'
    folder.mkdir(exist_ok=True)
    folder_meta=Path(str(folder)+'.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: '+guid(folder)+'\nfolderAsset: yes\nDefaultImporter:\n  userData:\n')
    # One reusable neutral surface map and one normal map, at 512px per metre.
    rng=np.random.default_rng(1930)
    n=512
    y,x=np.mgrid[0:n,0:n]/n
    broad=np.sin(2*np.pi*x)*np.sin(4*np.pi*y)+.35*np.cos(6*np.pi*x+2*np.pi*y)
    fine=rng.normal(0,1,(n,n))
    value=np.clip(.96+.004*broad+.005*fine,.92,1)
    images={}
    for name,normal in [('ADM_Surface',False),('ADM_SurfaceNormal',True)]:
        rgba=np.ones((n,n,4),dtype=np.float32)
        if normal:
            rgba[:,:,0]=.5+.012*(np.roll(fine,1,1)-np.roll(fine,-1,1))
            rgba[:,:,1]=.5+.012*(np.roll(fine,1,0)-np.roll(fine,-1,0))
            rgba[:,:,2]=1
        else: rgba[:,:,:3]=value[:,:,None]
        path=folder/(name+'.png')
        if (preserve_existing or preserve_textures) and path.exists():
            images[name]=bpy.data.images.load(str(path),check_existing=True)
            images[name].colorspace_settings.name='Non-Color' if normal else 'sRGB'
            continue
        image=bpy.data.images.new(name,width=n,height=n,alpha=True)
        image.colorspace_settings.name='Non-Color' if normal else 'sRGB'
        image.pixels.foreach_set(rgba.ravel())
        image.filepath_raw=str(path)
        image.file_format='PNG'
        image.save()
        images[name]=image
        meta=Path(str(path)+'.meta')
        if not meta.exists():
            meta.write_text('fileFormatVersion: 2\nguid: '+guid(path)+f'''\nTextureImporter:
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
    filterMode: 1
    aniso: 2
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  textureType: {1 if normal else 0}
  textureShape: 1
  alphaSource: 1
  alphaIsTransparency: 0
  userData:
''')
    records=[]
    generate(folder,guid)
    # Use the same packed surface profiles for Blender and Unity.
    for key,m in materials.items():
        if key=='Clay': continue
        p=m.node_tree.nodes.get('Principled BSDF')
        color=list(m.diffuse_color[:3])
        rough=float(p.inputs['Roughness'].default_value)
        metal=float(p.inputs['Metallic'].default_value)
        profile=PROFILES.get(key)
        path=folder/(m.name+'.mat')
        if not profile and path.exists() and preserve_textures:
            # Glass, wood, backing and lamp materials are outside this detail pass.
            records.append((m.name,guid(path)))
            continue
        textured=key in ('Stucco','Limestone','Roof','DoorWood')
        surface_scale=1/SPAN if profile else 1
        surface_offset=.5 if profile else 0
        peak_smoothness=(1-rough)/.9 if profile else 1-rough
        detail_normal=profile in ('Stucco','Stone')
        if profile:
            textured=True
        if textured:
            nodes,links=m.node_tree.nodes,m.node_tree.links
            base=nodes.get('UnitySurfaceColor') or nodes.new('ShaderNodeTexImage')
            base.name='UnitySurfaceColor'
            base.image=bpy.data.images['Surface_'+profile] if profile else images['ADM_Surface']
            multiply=nodes.get('UnitySurfaceTint') or nodes.new('ShaderNodeMixRGB')
            multiply.name='UnitySurfaceTint'
            multiply.blend_type='MULTIPLY'
            multiply.inputs[0].default_value=1
            multiply.inputs[2].default_value=(*color,1)
            links.new(base.outputs['Color'],multiply.inputs[1])
            links.new(multiply.outputs[0],p.inputs['Base Color'])
            coordinates=nodes.get('SurfaceUV') or nodes.new('ShaderNodeTexCoord')
            coordinates.name='SurfaceUV'
            mapping=nodes.get('SurfaceMapping') or nodes.new('ShaderNodeMapping')
            mapping.name='SurfaceMapping'
            mapping.inputs['Scale'].default_value=(surface_scale,surface_scale,1)
            mapping.inputs['Location'].default_value=(surface_offset,surface_offset,0)
            links.new(coordinates.outputs['UV'],mapping.inputs['Vector'])
            links.new(mapping.outputs['Vector'],base.inputs['Vector'])
            if profile:
                smooth=nodes.get('SurfaceSmoothness') or nodes.new('ShaderNodeMath')
                smooth.name='SurfaceSmoothness'; smooth.operation='MULTIPLY'
                smooth.inputs[1].default_value=peak_smoothness
                links.new(base.outputs['Alpha'],smooth.inputs[0])
                roughness=nodes.get('SurfaceRoughness') or nodes.new('ShaderNodeMath')
                roughness.name='SurfaceRoughness'; roughness.operation='SUBTRACT'
                roughness.inputs[0].default_value=1
                links.new(smooth.outputs[0],roughness.inputs[1])
                links.new(roughness.outputs[0],p.inputs['Roughness'])
            normal=nodes.get('UnitySurfaceNormal') or nodes.new('ShaderNodeTexImage')
            normal.name='UnitySurfaceNormal'
            normal.image=bpy.data.images['Surface_'+profile+'Normal'] if detail_normal else images['ADM_SurfaceNormal']
            normal_node=nodes.get('UnityNormalStrength') or nodes.new('ShaderNodeNormalMap')
            normal_node.name='UnityNormalStrength'
            normal_node.inputs['Strength'].default_value=(.22 if profile=='Stucco' else .14) if profile else .4
            detail_mapping=nodes.get('SurfaceDetailMapping') or nodes.new('ShaderNodeMapping')
            detail_mapping.name='SurfaceDetailMapping'
            detail_mapping.inputs['Scale'].default_value=(2,2,1) if profile else (1,1,1)
            links.new(coordinates.outputs['UV'],detail_mapping.inputs['Vector'])
            links.new(detail_mapping.outputs['Vector'],normal.inputs['Vector'])
            links.new(normal.outputs['Color'],normal_node.inputs['Color'])
            links.new(normal_node.outputs[0],p.inputs['Normal'])
            if profile and not detail_normal:
                for link in list(p.inputs['Normal'].links): links.remove(link)
        path=folder/(m.name+'.mat')
        if preserve_existing and path.exists():
            records.append((m.name,guid(path)))
            continue
        glass=key=='ClearGlass'
        keywords=[]
        if profile: keywords.append('_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A')
        if detail_normal: keywords.append('_DETAIL_SCALED')
        elif textured and not profile: keywords.append('_NORMALMAP')
        if glass: keywords.extend(['_SURFACE_TYPE_TRANSPARENT','_ALPHAPREMULTIPLY_ON'])
        # URP Lit uses alpha transmission with preserved specular; geometry provides depth.
        # Native real-time approximation of the Cycles dielectric, no black base color.
        alpha=.13 if glass else 1
        # Encode RGB once, never alpha/roughness/metallic or normal-map data.
        unity_color=[linear_to_srgb(v) for v in color]
        base_tex=('{fileID: 2800000, guid: '+guid(folder/'ADM_Surface.png')+', type: 3}') if textured else '{fileID: 0}'
        norm_tex=('{fileID: 2800000, guid: '+guid(folder/'ADM_SurfaceNormal.png')+', type: 3}') if textured else '{fileID: 0}'
        if profile:
            base_tex='{fileID: 2800000, guid: '+guid(folder/('Surface_'+profile+'.png'))+', type: 3}'
            norm_tex='{fileID: 0}'
        detail_tex=('{fileID: 2800000, guid: '+guid(folder/('Surface_'+profile+'Normal.png'))+', type: 3}') if detail_normal else '{fileID: 0}'
        keyword_yaml=('\n'+''.join('  - '+v+'\n' for v in keywords)).rstrip() if keywords else ' []'
        text=f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {m.name}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:{keyword_yaml}
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: {3000 if glass else -1}
  stringTagMap:
    RenderType: {'Transparent' if glass else 'Opaque'}
  disabledShaderPasses: {'[ShadowCaster, DepthOnly]' if glass else '[]'}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {base_tex}
        m_Scale: {{x: {surface_scale}, y: {surface_scale}}}
        m_Offset: {{x: {surface_offset}, y: {surface_offset}}}
    - _BumpMap:
        m_Texture: {norm_tex}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailNormalMap:
        m_Texture: {detail_tex}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailAlbedoMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: {2*SPAN}, y: {2*SPAN}}}
        m_Offset: {{x: {-SPAN}, y: {-SPAN}}}
    m_Ints: []
    m_Floats:
    - _Surface: {1 if glass else 0}
    - _Blend: 0
    - _AlphaClip: 0
    - _SrcBlend: 1
    - _DstBlend: {10 if glass else 0}
    - _SrcBlendAlpha: 1
    - _DstBlendAlpha: {10 if glass else 0}
    - _ZWrite: {0 if glass else 1}
    - _Cull: 2
    - _Smoothness: {peak_smoothness:.4f}
    - _SmoothnessTextureChannel: {1 if profile else 0}
    - _DetailAlbedoMapScale: 0
    - _DetailNormalMapScale: {.22 if profile=='Stucco' else .14}
    - _Metallic: {metal:.4f}
    - _WorkflowMode: 1
    - _BumpScale: 0.4
    - _SpecularHighlights: 1
    - _EnvironmentReflections: 1
    - _BlendModePreserveSpecular: 1
    - _ReceiveShadows: 1
    - _QueueOffset: 0
    - _OcclusionStrength: 1
    m_Colors:
    - _BaseColor: {{r: {unity_color[0]}, g: {unity_color[1]}, b: {unity_color[2]}, a: {alpha}}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.04, g: 0.04, b: 0.04, a: 1}}
  m_BuildTextureStacks: []
'''
        path.write_text(text,encoding='utf8')
        meta=Path(str(path)+'.meta')
        if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData:\n')
        records.append((m.name,guid(path)))
    return records

def remap(root,records):
    path=root/'Assets/SilverScreen/Environment/ReferenceKit/Models/StudioAdministration_01.fbx.meta'
    text=path.read_text(encoding='utf8')
    mappings='  externalObjects:\n'
    for name,g in records:
        mappings+=f'''  - first:
      type: UnityEngine:Material
      assembly: UnityEngine.CoreModule
      name: {name}
    second: {{fileID: 2100000, guid: {g}, type: 2}}
'''
    text,count=re.subn(r'  externalObjects:.*?(?=  materials:)',mappings,text,count=1,flags=re.S)
    if count!=1: raise RuntimeError('Unexpected FBX importer format; preserving meta')
    path.write_text('\n'.join(line.rstrip() for line in text.splitlines())+'\n',encoding='utf8')
