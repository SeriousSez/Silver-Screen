"""Scientific cross-section plots from evaluated meshes, not image segmentation.

Uses triangulation saved by check_female_blink_surface.py. Run with host Python
(numpy and Pillow). Blue = head surface, orange = eyeball, in raw-source mm.
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--evidence', type=Path, required=True)
parser.add_argument('--names', nargs='+', required=True)
args = parser.parse_args()
root = args.evidence
triangles = np.load(root/'surface-regions.npz')['triangles']
width, height = 560, 610
image = Image.new('RGB', (width*len(args.names),height*2),'white')
draw = ImageDraw.Draw(image)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
for col,name in enumerate(args.names):
    points = np.load(root/(name+'.npz'))['points']*1000
    for row,section in enumerate([43,58]):
        left,top = col*width,row*height
        draw.text((left+20,top+12),name,fill='black',font=font)
        draw.text((left+20,top+38),f'Anatomical Left; x={section} mm',fill='black',font=font)
        draw.text((left+20,top+62),'Blue: head/lids. Orange: eyeball. +Y is posterior.',fill='black',font=small)
        ymin,ymax,zmin,zmax = -60,10,1635,1705
        def pixel(p):
            return (left+45+(p[1]-ymin)*7,top+100+(zmax-p[2])*7)
        for y in range(ymin,ymax+1,10):
            x=left+45+(y-ymin)*7
            draw.line((x,top+100,x,top+590),fill='#e7e7e7')
            draw.text((x-10,top+592),str(y),fill='#444444',font=small)
        for z in range(1640,1701,10):
            yy=top+100+(zmax-z)*7
            draw.line((left+45,yy,left+535,yy),fill='#e7e7e7')
            draw.text((left+2,yy-8),str(z),fill='#444444',font=small)
        for triangle in triangles:
            if max(triangle)<12937:colour='#1468ba'
            elif min(triangle)>=16143 and max(triangle)<16497:colour='#db731c'
            else:continue
            t=points[triangle]
            if not (t[:,0].min()<section<t[:,0].max()):continue
            cut=[]
            for a,b in [(0,1),(1,2),(2,0)]:
                if (t[a,0]-section)*(t[b,0]-section)<0:
                    cut.append(t[a]+(t[b]-t[a])*(section-t[a,0])/(t[b,0]-t[a,0]))
            if len(cut)!=2:continue
            if not all(ymin<=p[1]<=ymax and zmin<=p[2]<=zmax for p in cut):continue
            draw.line((*pixel(cut[0]),*pixel(cut[1])),fill=colour,width=2)
image.save(root/'eyelid-sections.png')
