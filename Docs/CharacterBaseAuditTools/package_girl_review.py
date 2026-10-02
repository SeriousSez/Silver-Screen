"""Lay out actual Girl source renders for the explicit user visual gate.

Only labels, layout and display scaling are applied. No image synthesis, paint,
geometry edits, source-to-lightweight mapping or runtime bake occurs here.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

p = argparse.ArgumentParser()
p.add_argument('--evidence', type=Path, default=Path('TestResults/GirlFoundation/Review'))
p.add_argument('--candidate', default='ReviewG1')
p.add_argument('--previous', type=Path, help='Preserved ReviewG1 evidence for matched comparison only')
a = p.parse_args()
root = a.evidence
font_path = Path('C:/Windows/Fonts/segoeui.ttf')
font = ImageFont.truetype(str(font_path), 23) if font_path.exists() else ImageFont.load_default()
small = ImageFont.truetype(str(font_path), 19) if font_path.exists() else font


def sheet(filename, rows, columns, width=600):
    height = width * 7 // 10
    canvas = Image.new('RGB', (len(columns)*width, 105+len(rows)*(height+35)+45), '#202428')
    draw = ImageDraw.Draw(canvas)
    draw.text((18, 10), f'Girl {a.candidate} | original source rig | visual approval pending', font=font, fill='white')
    draw.text((18, 43), 'Technical grey evidence. Small corner openings and internal contacts remain disclosed.', font=small, fill='#d5dbe0')
    for col, column in enumerate(columns):
        name, title = column[:2]
        source_root = column[2] if len(column) > 2 else root
        draw.text((col*width+18, 77), title, font=small, fill='white')
        for row, (view, label) in enumerate(rows):
            y = 105+row*(height+35)
            with Image.open(source_root/f'{view}-{name}-bald.png') as source:
                canvas.paste(source.convert('RGB').resize((width, height), Image.Resampling.LANCZOS), (col*width, y))
            draw.text((col*width+12, y+height+4), label, font=small, fill='#d5dbe0')
    footer = ('50% means half source-control offsets; no runtime BlendShape has been generated.'
              if any(c[0]=='partial50' for c in columns)
              else 'Actual evaluated source geometry; matched cameras and lighting; no runtime BlendShape.')
    draw.text((18, canvas.height-34), footer, font=small, fill='#d5dbe0')
    canvas.save(root/filename)


columns = [('neutral', 'OPEN / neutral'), ('partial50', 'PARTIAL / 50% controls'), (a.candidate, 'CLOSED / proposed endpoint')]
sheet('girl-source-eyes-review.png', [('front','Front'), ('oblique','Oblique')], columns)
sheet('girl-source-face-review.png', [('face','Full-face front'), ('face-oblique','Full-face oblique')], columns)
sheet('girl-source-corners-review.png', [('eye-oblique-L','Anatomical left eye / oblique'), ('eye-oblique-R','Anatomical right eye / mirrored oblique')], [('neutral','OPEN / neutral'), (a.candidate,'CLOSED / proposed endpoint')], width=750)
for side in ['L','R']:
    sheet(f'girl-source-eye-{side}-review.png', [(f'eye-{side}',f'Anatomical {side} eye / front'), (f'eye-oblique-{side}',f'Anatomical {side} eye / oblique')], columns)
if a.previous:
    sheet('girl-contour-before-after.png', [('front','Matched front'), ('oblique','Matched oblique')],
          [('ReviewG1','BEFORE / ReviewG1 declined',a.previous), (a.candidate,f'AFTER / {a.candidate} proposed')], width=750)
    sheet('girl-corners-before-after.png', [('eye-oblique-L','Anatomical left eye / oblique'), ('eye-oblique-R','Anatomical right eye / mirrored oblique')],
          [('ReviewG1','BEFORE / ReviewG1 declined',a.previous), (a.candidate,f'AFTER / {a.candidate} proposed')], width=750)
files = sorted(root.glob('*.png'))
manifest = {f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in files}
manifest[a.candidate+'.npz'] = hashlib.sha256((root/(a.candidate+'.npz')).read_bytes()).hexdigest()
(root/'review-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
print(f'Packaged {len(files)} actual source render/evidence PNGs. User acceptance still required.')
