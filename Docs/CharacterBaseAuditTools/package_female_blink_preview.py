"""Package actual Unity captures for technical review; no generated/repainted imagery."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path('TestResults/FemaleBlinkUnity')


def main():
    rows = []
    for view in ('face', 'eyes', 'oblique'):
        baseline = np.array(Image.open(ROOT/'baseline'/f'{view}.png'))
        neutral = np.array(Image.open(ROOT/'stills'/f'{view}-weight-000.png'))
        restored = np.array(Image.open(ROOT/'stills'/f'{view}-restored.png'))
        rows.append({'view': view, 'baseline_neutral_pixel_max': int(np.abs(baseline.astype(int)-neutral).max()),
                     'baseline_neutral_changed_pixels': int(np.any(baseline!=neutral, axis=2).sum()),
                     'neutral_restored_pixel_max': int(np.abs(neutral.astype(int)-restored).max()),
                     'neutral_restored_changed_pixels': int(np.any(neutral!=restored, axis=2).sum())})
    (ROOT/'pixel-comparison.json').write_text(json.dumps(rows, indent=2)+'\n', encoding='utf-8', newline='\n')
    review = ROOT/'Review'; review.mkdir(exist_ok=True)
    for sequence, fps in (('sweep', 30), ('normal-speed', 60)):
        files = sorted((ROOT/sequence).glob('frame-*.png'))
        assert len(files) == (49 if sequence=='sweep' else 61)
        frames = [Image.open(p).convert('RGB') for p in files]
        # Timing alternates integer hundredths in GIF; MP4/frame CSV are precise references.
        durations = [round((i+1)*1000/fps/10)*10-round(i*1000/fps/10)*10 for i in range(len(frames))]
        durations[0] += 500; durations[-1] += 700
        frames[0].save(review/f'{sequence}.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False)
        for start in range(0,len(files),16):
            selected = files[start:start+16]
            sheet = Image.new('RGB',(1600,1000),(20,24,31));draw=ImageDraw.Draw(sheet)
            for j,path in enumerate(selected):
                x=(j%4)*400;y=(j//4)*250
                image=Image.open(path);image.thumbnail((400,225));sheet.paste(image,(x,y+25))
                draw.text((x+8,y+5),f'{sequence} frame {start+j:03d} / {(start+j)/fps:.3f}s',fill='white')
            sheet.save(review/f'{sequence}-sheet-{start//16+1}.png')
    compare=Image.new('RGB',(1500,360),(20,24,31));draw=ImageDraw.Draw(compare)
    for i,(weight,label) in enumerate(((0,'Open / 0%'),(50,'Half blink / 50%'),(100,'Closed / 100%'))):
        image=Image.open(ROOT/'stills'/f'eyes-weight-{weight:03d}.png');image.thumbnail((500,325))
        compare.paste(image,(i*500,30));draw.text((i*500+12,8),label,fill='white')
    compare.save(review/'open-half-closed.png')
    print(json.dumps(rows,indent=2))


if __name__=='__main__':
    main()
