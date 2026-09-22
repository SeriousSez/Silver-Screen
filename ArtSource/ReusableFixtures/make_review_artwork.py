"""Studio-agnostic template backgrounds. Identity/year/title are rendered by Unity fields."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/SilverScreen/Environment/ReusableFixtures/Textures'
OUT.mkdir(parents=True,exist_ok=True)
FONT=Path('C:/Windows/Fonts/arial.ttf')
def text(draw,xy,value,size,color): draw.text(xy,value,font=ImageFont.truetype(str(FONT),size),fill=color,anchor='mm')
im=Image.new('RGB',(600,900),'#13353d');d=ImageDraw.Draw(im)
d.rectangle((24,24,575,875),outline='#e7d5a8',width=3)
d.ellipse((125,225,475,575),fill='#d09b4c');d.polygon([(170,475),(300,290),(430,475)],fill='#214c54')
text(d,(300,672),'OPEN HOUSE',49,'#f0e6ca');text(d,(300,740),'A DAY AT THE MOVIES',23,'#e7d5a8')
im.save(OUT/'StudioPoster.png')
im=Image.new('RGB',(1000,600),'#e7dfc9');d=ImageDraw.Draw(im)
d.rectangle((20,20,980,580),outline='#274850',width=6)
text(d,(500,105),'STUDIO NOTICE',55,'#274850')
d.line((90,172,910,172),fill='#aa7544',width=5)
text(d,(500,282),'QUIET PLEASE',76,'#274850')
im.save(OUT/'StudioNotice.png')
print('Created two studio-agnostic template backgrounds')
