"""Straight-hanging concept-led velvet and removed service canopy, Z-up metres."""
clear_group('Furnishings/Audition/Curtains')
group('Furnishings/Audition/Curtains')
# Each wing is one sewn surface. Unequal fold centres/depths hang under gravity;
# the broad hem remains continuous, with no separate strips or pointed tips.
for side in [-1,1]:
 nx,nz=64,40;vs=[];fs=[]
 centres=[.06,.255,.49,.755,.955] if side<0 else [.045,.29,.545,.78,.98]
 amplitudes=[.12,.17,.135,.20,.105] if side<0 else [.13,.185,.145,.175,.11]
 def fabric(u,t):
  inner=2.045+.012*math.sin(t*math.pi*1.3+side)
  outer=3.64-.012*math.sin(t*math.pi)+.012*side*(1-t)
  x=8.5+side*(inner+(outer-inner)*u)
  warp=u+.018*math.sin(math.pi*t)*math.sin(u*5.4+side*.4)+.012*(1-t)*math.sin(u*9)
  depth=0
  for k,(c,a) in enumerate(zip(centres,amplitudes)):
   width=.058+.012*(k%3)+.013*(1-t)
   depth+=a*math.exp(-((warp-c)/width)**2)
  y=14.04-depth-.025*math.sin(math.pi*t)+.018*math.sin(u*4+t*2)
  hem=.775+.018*math.sin(u*7.3+side)+.011*math.sin(u*15+.4)
  z=hem+(3.79-hem)*t
  return (x,y,z)
 for j in range(nz+1):
  for i in range(nx+1):vs.append(fabric(i/nx,j/nz))
 for j in range(nz):
  for i in range(nx):
   a=j*(nx+1)+i;fs.append((a,a+1,a+nx+2,a+nx+1))
 R.cloth('Continuous heavy straight velvet wing',vs,fs,.012)
 tube('Weighted turned velvet hem',vs[:nx+1],.018,'SS_Curtain',sides=8)
 for i in [0,nx]:tube('Doubled sewn velvet edge',[vs[j*(nx+1)+i] for j in range(nz+1)],.012,'SS_Curtain',sides=8)
 # Straight wings remain open on their track; no tied waist or unnecessary hooks.
 # Slider eyes sit over the rail; short sewn tabs join the top heading.
 for i in range(9):
  p=fabric(i/8,1);x=p[0]
  tube('Curtain slider eye',[(x,14.07+.029*math.cos(k*math.pi/8),3.85+.029*math.sin(k*math.pi/8)) for k in range(17)],.006,'Steel',sides=6)
  beam('Sewn suspension heading tab',(x,p[1],3.78),(x,14.07,3.825),.034,.014,'SS_Curtain')
# One continuous straight-bottom pleated heading, as in the supplied room concept.
vs=[];fs=[];nx,nz=144,14
for j in range(nz+1):
 t=j/nz
 for i in range(nx+1):
  u=i/nx;bottom=3.49+.009*math.sin(u*19)+.004*math.sin(u*37)
  phase=2*math.pi*13*u+.34*math.sin(u*17)+.09*t
  depth=(.047+.010*math.sin(u*23))*math.cos(phase)+.008*math.sin(phase*2+.3)
  vs.append((4.65+7.70*u,13.74+.095*t**6-depth*(1-.75*t**8),bottom+(3.94-bottom)*t))
for j in range(nz):
 for i in range(nx):a=j*(nx+1)+i;fs.append((a,a+1,a+nx+2,a+nx+1))
R.cloth('Continuous sewn straight pleated velvet valance',vs,fs,.014)
tube('Valance weighted sewn hem',vs[:nx+1],.012,'SS_Curtain',sides=8)
box('Restrained walnut track fascia',(8.5,14.02,3.99),(7.9,.40,.17),'SS_Walnut',.008)
tube('Continuous curtain suspension track',[(4.75,14.07,3.85),(12.25,14.07,3.85)],.022,'Steel',sides=12)
for x in [4.795,6.55,8.5,10.45,12.205]:
 box('Track bracket masonry plate',(x,14.645,3.84),(.085,.025,.16),'Steel',.004)
 beam('Supported track outrigger',(x,14.64,3.88),(x,14.06,3.88),.025,.035,'Steel')

# The user removed the service canopy; retain the doorway, steps and wall lantern.
QA['a32_drapery_canopy']=dict(curtain_wings=2,continuous_fabric=True,service_canopy_removed=True,roof_massing_unchanged=True)
