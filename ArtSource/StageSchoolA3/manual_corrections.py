"""A3.2: six manual corrections only, before export. All coordinates metres, Z up."""
print('A3.2 fitted reception, cubicle leaves and local circulation',flush=True)
# Local Hall cap termination below the Audition west gutter. Keep the roof planes.
# A narrow metal abutment replaces only the terminal clay strip alongside the gutter.
c=G.GROUPS['Roofs/Hall'];cut=0
for o in list(c.objects):
 if o.type!='MESH' or not o.name.startswith(('Hip','Ridge','Batched','Clay','Terracotta','Concave','Convex','Tile','Fired')):continue
 vv=[v.co for v in o.data.vertices]
 if not vv or max(v.x for v in vv)<2.19 or max(v.y for v in vv)<4.84:continue
 bm=bmesh.new();bm.from_mesh(o.data)
 for point,normal in [((2.19,0,0),(1,0,0)),((0,4.84,0),(0,1,0))]:
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=point,plane_no=normal)
 bad=[f for f in bm.faces if f.calc_center_median().x>2.19 and f.calc_center_median().y>4.84]
 cut+=len(bad)
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 # Close the cut cap faces, but do not fill the broad openings of a tile field.
 if o.name.startswith(('Hip','Ridge','Batched')):
  edges=[e for e in bm.edges if e.is_boundary]
  if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=32)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
group('Roofs/Hall/Flashing');hall=next(r for r in roofs if r[0]=='Hall')
verts=[]
for y in [4.82,5.1,5.37]:
 for x in [2.10,2.22,2.57]:verts.append((x,y,roof_z(hall,x,y)+.024))
mesh('Hall collector corner continuous apron',verts,[(0,1,4,3),(1,2,5,4),(3,4,7,6),(4,5,8,7)],'Galvanized')
# Reception: keep exactly the approved curved visitor shell and flute work.
group('Furnishings/Reception/Counter')
c=G.GROUPS['Furnishings/Reception/Counter']
for o in list(c.objects):
 if not o.name.startswith(('Curved reception joinery','Counter brass flute')):bpy.data.objects.remove(o,do_unlink=True)
def prism(n,outline,z0,z1,mat):
 vs=[(x,y,z) for z in [z0,z1] for x,y in outline];k=len(outline)
 fs=[tuple(reversed(range(k))),tuple(range(k,k*2))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)]
 return mesh(n,vs,fs,mat)
# Fitted elliptical outer edge slots into the existing curved carcass.
outline=[(1.57*math.cos(math.pi+i*math.pi/48),-4+.58*1.57*math.sin(math.pi+i*math.pi/48)) for i in range(49)]
outline += [(1.57-3.14*i/24,-4+.19*math.sin(math.pi*i/24)) for i in range(1,24)]
prism('Fitted continuous curved staff worktop',outline,1.075,1.125,'SS_Walnut')
# Integral side cabinets follow the counter back rather than projecting into the aisle.
for side in [-1,1]:
 pts=[(side*x,-4+.58*math.sqrt(max(0,1.54**2-x*x))*-1) for x in [.79,.94,1.10,1.26,1.40,1.54]]
 pts += [(side*1.54,-3.99),(side*.79,-3.99)]
 if side<0:pts.reverse()
 prism('Counter fitted cabinet plinth',pts,.39,.47,'TimberDark')
 # Curved back is the approved shell; solid end and knee cheeks support the slab.
 for x,y,d in [(side*.79,-4.365,.76),(side*1.49,-4.105,.23)]:box('Integrated cabinet side',(x,y,.765),(.035,d,.60),'SS_Walnut',.003)
 for z in [.475,.67,.87,1.06]:
  prism('Fitted cabinet shelf',pts,z,z+.023,'SS_Walnut')
 if side<0:
  for z in [.57,.77,.973]:
   box('Applicant file drawer front',(-1.14,-3.982,z),(.66,.028,.174),'SS_Walnut',.004)
   tube('Recessed applicant drawer pull',[(-1.215,-3.963,z),(-1.215,-3.941,z-.013),(-1.065,-3.941,z-.013),(-1.065,-3.963,z)],.007,'Brass',sides=8)
   box('Drawer label frame',(-1.14,-3.961,z+.046),(.12,.010,.034),'Brass',.002)
   box('Drawer cream label',(-1.14,-3.955,z+.046),(.105,.004,.024),'PaperLight',0)
 else:
  for z in [.70,.90]:
   for j in range(3):box('Applicant folio in fitted cubby',(1.10+j*.022,-4.22,z+.043),(.018,.30,.07),['Canvas','Leather','SS_Sage'][j],.002)
# Worktop joins the curved skin all around; rear edge has no generic desk apron.
for x in [-.72,.72]:box('Knee opening worktop support',(x,-4.43,1.047),(.04,.57,.055),'SS_Walnut',.004)
shift('Furnishings/Reception/Accessories',dy=-.16,dz=-.025)
for f in fixtures:
 g=f['group'];p=f['position']
 if g=='Furnishings/Reception/Telephone':p[:]=[-.94,1.125,-4.30]
 if g=='Furnishings/Reception/BankerLamp':p[:]=[1.18,1.125,-4.18]
 if g=='Furnishings/Reception/StaffChair':p[:]=[0,.381,-3.43]
 # Two side-wall stations leave the central/east side of the room as a clear route.
 if g.startswith('Furnishings/Staff/'):
  if f['asset']=='DeskPedestal_1930':p[0]=-6.40;p[2]=4.65 if g.endswith('0') else 6.75
  elif f['asset']=='ChairBentwood_1930':p[0]=-5.53;p[2]=4.65 if g.endswith('0') else 6.75
  elif g.endswith('Telephone'):p[0]=-6.38;p[2]=4.65
  elif g.endswith('RecordsInUse'):p[0]=-6.38;p[2]=6.75
# Dedicated lightweight cubicle door, shared construction for both stalls.
wash=G.GROUPS['Furnishings/Washroom']
for o in list(wash.objects):
 if o.name.startswith(('Cubicle rebated fixed jamb','Cubicle fixed hinge leaf','Cubicle jamb hinge screw','Privacy bolt keep')):bpy.data.objects.remove(o,do_unlink=True)
for i,x in enumerate([7.79,9.49]):
 g='Doors/Cubicle'+str(i);clear_group(g);y=-6.20;bottom=.59;top=2.16;w=1.05;hinge=x-w/2
 pivots[g]=[hinge,bottom,y]
 box('Cubicle lightweight framed leaf',(x,y,(bottom+top)/2),(w,.032,top-bottom),'SS_Sage',.004)
 # Flush field and restrained framing, both sides; no room-door casing or heavy moulding.
 for side in [-1,1]:
  box('Cubicle flush infill',(x,y+side*.017,1.375),(.89,.006,1.34),'InteriorPlaster',.002)
  for dx in [-.482,.482]:box('Cubicle narrow stile',(x+dx,y+side*.019,1.375),(.075,.008,1.57),'SS_Sage',.002)
  for z in [.632,2.118]:box('Cubicle narrow rail',(x,y+side*.019,z),(1.05,.008,.085),'SS_Sage',.002)
 for z in [.82,1.94]:
  cylinder('Cubicle partition hinge barrel',(hinge-.009,y-.021,z),.013,.11,'Brass',segments=12)
  box('Cubicle moving hinge leaf',(hinge+.025,y-.023,z),(.055,.009,.10),'Brass',.001)
  for dz in [-.031,.031]:cylinder('Cubicle leaf hinge screw',(hinge+.03,y-.030,z+dz),.005,.006,'Steel',(0,1,0),segments=8)
 # Occupant side is negative Y. Bolt reaches a keep on the fixed partition jamb.
 hx=x+.416;z=1.45
 box('Occupant privacy bolt backplate',(hx,y-.027,z),(.17,.014,.062),'Brass',.003)
 cylinder('Occupant sliding privacy bolt',(x+.472,y-.048,z),.010,.183,'Brass',(1,0,0),segments=12)
 tube('Occupant bolt thumb piece',[(hx,y-.048,z),(hx,y-.075,z),(hx,y-.075,z+.032)],.008,'Brass',sides=8)
 tube('Occupant small finger pull',[(x+.37,y-.03,1.24),(x+.37,y-.065,1.24),(x+.37,y-.065,1.34),(x+.37,y-.03,1.34)],.009,'Brass',sides=10)
 cylinder('Outside occupancy indicator',(hx,y+.032,z),.029,.012,'Brass',(0,1,0),segments=16)
 box('Outside indicator enamel',(hx,y+.039,z),(.028,.006,.012),'SS_Sage',.001)
 tube('Outside cubicle pull',[(hx,y+.025,1.22),(hx,y+.06,1.22),(hx,y+.06,1.34),(hx,y+.025,1.34)],.009,'Brass',sides=10)
 group('Furnishings/Washroom')
 for side in [-1,1]:box('Light cubicle partition jamb',(x+side*.555,y,1.46),(.060,.065,1.86),'SS_Sage',.004)
 for zz in [.82,1.94]:
  box('Partition anchored hinge leaf',(hinge-.048,y-.024,zz),(.064,.009,.10),'Brass',.001)
  for dz in [-.031,.031]:cylinder('Partition hinge fixing',(hinge-.055,y-.031,zz+dz),.005,.008,'Steel',(0,1,0),segments=8)
 box('Occupant partition bolt keep',(x+.552,y-.045,z),(.052,.029,.055),'Brass',.003)
 for d in DOORS:
  if d['group']==g:d.update(bottom=bottom,height=top-bottom,thickness=.032,privacy_side='negative Y / occupant',swing=0)
QA['a32']=dict(scope='six manual corrections',hall_terminal_faces_removed=cut,reception_knee_width=1.54,cubicle_leaf_thickness=.032,cubicle_bottom_clearance=.21,cubicle_top_clearance=.18,privacy_side='occupant negative Y',scoring='upper stucco scoring row removed; lower stone-base joints retained')
print('A3.2 source corrections complete',flush=True)
