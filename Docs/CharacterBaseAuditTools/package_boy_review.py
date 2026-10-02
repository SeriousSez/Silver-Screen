"""Package actual Boy Blender/Unity renders; no synthetic imagery or retouching."""
import json,shutil
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw

root=Path('TestResults/BoyFoundation');review=root/'Review'
sources={'neutral':('Calibration','neutral'),'u90_l20':('Calibration','u90_l20'),'ReviewR3':('SideRefine','rInner3')}
for dest,(folder,name) in sources.items():
    assert np.array_equal(np.load(review/(dest+'.npz'))['points'],np.load(root/folder/(name+'.npz'))['points'])
    for view in ['front','oblique','face']:
        shutil.copy2(root/folder/(view+'-'+name+'-bald.png'),review/(view+'-'+dest+'-bald.png'))
    if dest=='ReviewR3':
        for side in ['L','R']:shutil.copy2(root/folder/f'eye-{side}-{name}-bald.png',review/f'eye-{side}-{dest}-bald.png')

def sheet(names,views,filename,width=440):
    height=int(width*.7);result=Image.new('RGB',(width*len(views),(height+28)*len(names)),'#222222');draw=ImageDraw.Draw(result)
    for row,name in enumerate(names):
        for col,view in enumerate(views):
            image=Image.open(review/f'{view}-{name}-bald.png').convert('RGB').resize((width,height))
            result.paste(image,(col*width,row*(height+28)+28))
            draw.text((col*width+8,row*(height+28)+7),name+' | '+view,fill='white')
    result.save(review/filename)
sheet(['neutral','partial','ReviewR3'],['front','oblique','face'],'review-contact.png')
sheet(['u90_l20','ReviewR3'],['front','oblique'],'previous-versus-review.png',550)
sheet(['ReviewR3'],['eye-L','eye-R'],'eyes-closeup.png',650)
(review/'image-provenance.json').write_text(json.dumps(dict(reused_exact_geometry=sources,partial='Direct evaluated source rig at half of all ReviewR3 control translations',processing='Labels/layout/resize only; actual rendered geometry'),indent=2))
