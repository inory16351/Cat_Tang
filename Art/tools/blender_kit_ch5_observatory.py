# 무너지는 그리니치 천문대 벽 7종과 랜드마크 6종을 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_observatory.py
"""Chapter V observatory replacement kit, Blender 5.x, deterministic metres.

Only this script, thirteen FBXs and one review PNG are written. Exported role
materials are neutral grey; viewport palette is assigned AFTER all exports.
Disconnected hovering components are closed solids joined to the same mesh.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_observatory_preview.png'
TAU = math.tau
FACE = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
PALETTE = {
    'void_stone': (.055, .115, .22), 'void_stone_dark': (.023, .048, .10),
    'marble': (.77, .85, .91), 'marble_dark': (.28, .40, .52),
    'violet': (.22, .32, .44), 'violet_dark': (.09, .15, .24),
    'indigo': (.035, .08, .19), 'night_sky': (.012, .026, .075),
    'starlight': (.88, .97, 1), 'crack_gold': (.04, .89, 1),
    'gold': (.85, .61, .27), 'gold_dark': (.37, .24, .09),
    'brass': (.66, .44, .18), 'dial_face': (.89, .87, .72),
    'dial_mark': (.025, .047, .09), 'mirror': (.36, .66, .81),
    'mirror_back': (.10, .20, .29), 'crystal': (.44, .83, .94),
    'iron': (.11, .17, .24), 'iron_dark': (.035, .055, .085),
    'steel': (.37, .48, .59), 'red_lacquer': (.79, .035, .045),
    'wood': (.26, .17, .12), 'wood_dark': (.105, .066, .045),
    'porcelain': (.88, .89, .85), 'cloth': (.12, .26, .43),
    'velvet': (.10, .15, .25), 'lamp_warm': (1, .70, .30),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .76
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Carved 25mm chamfers', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(65)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def group(start, loc=(0, 0, 0), rot=(0, 0, 0)):
    for obj in PARTS[start:]:
        place(obj, loc, rot)


def box(label, loc, size, role, bevel=False, rot=(0, 0, 0)):
    x, y, z = [s / 2 for s in size]
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def slab(label, outline, back, front, role, bevel=False):
    n = len(outline)
    verts = [(x,y,z) for y in (back,front) for x,z in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label, verts, faces, role, bevel)


def cylinder(label, loc, radius, depth, role, n=24, rot=(0,0,0), bevel=False):
    verts = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
             for z in (-depth/2,depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def ring(label, loc, radius, width, depth, role, rot=(0,0,0), n=40,
         a0=0, a1=TAU):
    # Rectangular-section annulus, with true open ends for broken arcs.
    closed = abs(a1-a0-TAU)<1e-6
    count = n if closed else n+1
    verts = [(r*math.cos(a0+(a1-a0)*j/n),r*math.sin(a0+(a1-a0)*j/n),z)
             for r,z in ((radius,-depth/2),(radius,depth/2),
                         (radius-width,depth/2),(radius-width,-depth/2))
             for j in range(count)]
    faces = [(k*count+j,k*count+(j+1)%count,((k+1)%4)*count+(j+1)%count,
              ((k+1)%4)*count+j) for k in range(4) for j in range(n)]
    if not closed:
        faces += [(0,count,2*count,3*count),
                  (n,3*count+n,2*count+n,count+n)]
    return place(mesh(label,verts,faces,role),loc,rot)


def sector(label, x, y, z, radius, a0, a1, role, depth=.10, n=32):
    outline = [(x,z)] + [(x+radius*math.sin(a0+(a1-a0)*j/n),
                         z+radius*math.cos(a0+(a1-a0)*j/n)) for j in range(n+1)]
    return slab(label,outline,y-depth/2,y+depth/2,role)


def beam(label, a, b, width, depth, role, bevel=False):
    delta = Vector(b)-Vector(a)
    obj = box(label,(0,0,0),(width,depth,delta.length),role,bevel)
    obj.data.transform(Matrix.Translation((Vector(a)+Vector(b))/2) @
                       delta.to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def tube(label, points, radius, role, n=6):
    verts = []
    for i,p in enumerate(points):
        direction = (Vector(points[min(i+1,len(points)-1)])-
                     Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(direction.y)<.9 else Vector((1,0,0))
        u = direction.cross(ref).normalized()
        v = direction.cross(u)
        verts += [tuple(Vector(p)+radius*(u*math.cos(TAU*j/n)+v*math.sin(TAU*j/n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    return mesh(label,verts,faces,role)


def crack(points, radius=.033):
    tube('Dark inset fracture bed', points, radius*1.40, 'void_stone_dark')
    # Lift the narrow core toward BOTH review-facing directions: a concentric
    # smaller tube would be completely swallowed by the dark recess solid.
    tube('Cyan time-light fracture',[(x,y+.035,z+.035) for x,y,z in points],radius,'crack_gold')


def seam(outline, y=1.025):
    crack([(x,y,z) for x,z in outline])


def star(loc, radius=.10, role='starlight', rot=(0,0,0)):
    outline = [(radius*(1 if j%2==0 else .30)*math.sin(j*math.pi/4),
                radius*(1 if j%2==0 else .30)*math.cos(j*math.pi/4)) for j in range(8)]
    return place(slab('Raised four point observatory star',outline,-.024,.024,role),loc,rot)


def sphere(label, loc, radius, role, n=20, m=10):
    verts = [(0,0,-radius)]
    verts += [(radius*math.sin(math.pi*k/m)*math.cos(TAU*j/n),
               radius*math.sin(math.pi*k/m)*math.sin(TAU*j/n),
               -radius*math.cos(math.pi*k/m)) for k in range(1,m) for j in range(n)]
    verts += [(0,0,radius)]
    faces = [(0,1+(j+1)%n,1+j) for j in range(n)]
    faces += [(1+k*n+j,1+k*n+(j+1)%n,1+(k+1)*n+(j+1)%n,1+(k+1)*n+j)
              for k in range(m-2) for j in range(n)]
    faces += [(1+(m-2)*n+j,1+(m-2)*n+(j+1)%n,len(verts)-1) for j in range(n)]
    return place(mesh(label,verts,faces,role),loc)


def rock(label, loc, size, role='marble', seed=0):
    rng = random.Random(seed)
    verts = []
    for z,scale in ((-.5,.55),(.04,1),(.5,.68)):
        for j in range(5):
            a = TAU*j/5
            r = rng.uniform(.85,1.05)*scale*.5
            verts.append((math.cos(a)*r*size[0],math.sin(a)*r*size[1],z*size[2]))
    faces = [tuple(reversed(range(5))),tuple(range(10,15))]
    faces += [(k*5+j,k*5+(j+1)%5,(k+1)*5+(j+1)%5,(k+1)*5+j)
              for k in range(2) for j in range(5)]
    return place(mesh(label,verts,faces,role),loc)


def crystal(loc, height=.5, rot=(0,0,0)):
    r = height*.24
    verts = [(r*math.cos(TAU*j/5),r*math.sin(TAU*j/5),z)
             for z in (0,height*.66) for j in range(5)] + [(0,0,height)]
    faces = [tuple(reversed(range(5)))]
    faces += [(j,(j+1)%5,(j+1)%5+5,j+5) for j in range(5)]
    faces += [(5+j,5+(j+1)%5,10) for j in range(5)]
    return place(mesh('Foundation crystal cluster',verts,faces,'crystal'),loc,rot)


def arch_profile(x, bottom, width, height, n=20):
    r=width/2; spring=bottom+height-r
    return [(x-r,bottom),(x+r,bottom),(x+r,spring)] + [
        (x+r*math.cos(math.pi*j/n),spring+r*math.sin(math.pi*j/n)) for j in range(1,n+1)]


def arch_frame(x,bottom,width,height,y=1.06):
    r=width/2; spring=bottom+height-r
    for s in (-1,1):
        box('Pale arch jamb',(x+s*(r+.12),y,(bottom+spring)/2),(.24,.22,spring-bottom),'marble',True)
        box('Arch jamb stepped foot',(x+s*(r+.12),y,bottom+.10),(.36,.30,.20),'marble_dark',True)
        box('Arch impost',(x+s*(r+.12),y,spring),(.37,.29,.14),'gold',True)
    for j in range(11):
        ring('Individual arch voussoir',(x,y,spring),r+.25,.24,.22,'marble',FACE,3,
             j*math.pi/11+.009,(j+1)*math.pi/11-.009)
    box('Molded window sill',(x,y,bottom),(width+.65,.35,.16),'marble',True)


def pilaster(x,h):
    box('Pale pilaster shaft',(x,1.025,h/2),(.34,.24,h-.42),'marble',True)
    for z,w,d in ((.28,.54,.22),(h-.36,.55,.18),(h-.18,.66,.15)):
        box('Stacked pilaster molding',(x,1.01,z),(w,.37,d),'marble',True)
    box('Pilaster dark flute',(x,1.158,h/2),(.09,.045,h-1),'marble_dark')


def foundation(w):
    box('Continuous flush plinth',(0,.20,.13),(w,1.60,.26),'marble_dark',True)
    box('Pale plinth molding',(0,.22,.32),(w,1.64,.12),'marble',True)


def masonry(w,h,bottom=.40,front=.97):
    box('Flat navy structural backing',(0,.14,(h+bottom)/2),(w,1.42,h-bottom),'void_stone_dark')
    rows=max(1,round((h-bottom)/.56))
    dh=(h-bottom)/rows
    for row in range(rows):
        boundaries=[-w/2] + [v for v in [(-w/2)+.88*j+(.44 if row%2 else 0)
                                       for j in range(1,int(w/.88)+1)] if v<w/2-.05] + [w/2]
        for j,(a,b) in enumerate(zip(boundaries,boundaries[1:])):
            box('Navy dressed observatory brick',((a+b)/2,front-.10,bottom+(row+.5)*dh),
                (b-a-.025,.20,dh-.025),'void_stone' if (row+j)%7 else 'marble_dark',
                (row+j)%2==0)


def coping(w,z,role='marble',floats=True):
    cuts=[-w/2,-w*.23,w*.07,w*.31,w/2]
    for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
        dz=(0,-.09,.035,-.045)[j]
        slab('Broken irregular coping',[(a,z-.22),(b,z-.22),(b,z+dz-.03),
             (b-.17,z+dz+.025),((a+b)/2,z+dz-.045),(a+.15,z+dz),(a,z+dz)],
             -.60,1.06,role,True)
        # Broad top surfaces are important at the game's high camera angle.
        # Inlaid navy measuring plates break the otherwise blank pale caps.
        cx=(a+b)/2
        box('Coping recessed navy measuring tablet',(cx,.12,z+dz-.025),
            ((b-a)*.57,.92,.055),'indigo')
        box('Coping brass datum inlay',(cx,.12,z+dz+.010),
            ((b-a)*.49,.06,.045),'brass')
        for k in (-1,0,1):
            box('Coping measuring cross mark',(cx+k*(b-a)*.15,.12,z+dz+.018),
                (.045,.28 if k==0 else .18,.045),'gold')
    # Visible top seam runs from back to front, with no hairline details.
    crack([(-w*.34,-.53,z+.01),(-w*.32,-.08,z-.02),
           (-w*.35,.37,z-.01),(-w*.33,1.05,z-.015)])
    if floats:
        for j,(x,s) in enumerate(((-w*.27,.48),(w*.24,.37))):
            rock('Hovering broken coping chunk',(x,-.24,z+.37),(s,.40,.24),role,j+101)


def ticks(x,y,z,r,count=24,role='dial_mark',shift=None):
    for j in range(count):
        a=TAU*j/count
        dx,dy,dz=shift(j) if shift else (0,0,0)
        length=r*(.24 if j%6==0 else .13)
        box('Raised dial tick %02d'%j,(x+r*.83*math.sin(a)+dx,y+dy,z+r*.83*math.cos(a)+dz),
            (max(.045,r*.055),.055,length),role,rot=(0,a,0))


def dial(x,y,z,r,one_hand=True,broken=False):
    ring('Dark bezel bead',(x,y-.035,z),r+ .15,.08,.17,'gold_dark',FACE,48)
    ring('Thick brass gold clock bezel',(x,y+.025,z),r+.10,.14,.16,'gold',FACE,48)
    if broken:
        # Missing 45-degree wedge at upper-right; displaced face and its three
        # own tick blocks sit 0.30m forward, leaving an actual dark socket.
        a0,a1=math.pi/4,math.pi/2
        sector('Remaining cracked gate dial',x,y+.04,z,r,a1,a0+TAU,'dial_face',.12,45)
        sector('Hovering one eighth dial wedge',x+.07,y+.34,z+.04,r,a0+.015,a1-.015,'dial_face',.12,7)
        ticks(x,y+.13,z,r,shift=lambda j:(.07,.30,.04) if 3<=j<=6 else (0,0,0))
        seam([(x-r*.90,z-.23),(x-.30,z-.07),(x+.03,z+.12),(x+.32,z+.07),
              (x+.75,z+.30)],y+.12)
    else:
        cylinder('Cream instrument dial',(x,y+.045,z),r,.09,'dial_face',48,FACE)
        ticks(x,y+.115,z,r)
    beam('Long brass time hand',(x,y+.21,z),(x-r*.37,y+.21,z+r*.67),
         max(.055,r*.07),.065,'brass')
    if not one_hand:
        beam('Short brass hour hand',(x,y+.235,z),(x+r*.39,y+.235,z+r*.10),.06,.06,'brass')
    cylinder('Central brass hand pin',(x,y+.27,z),max(.07,r*.10),.09,'brass',12,FACE)


def plaque(loc,w=.70,h=.30):
    x,y,z=loc
    box('Brass engraved measure plaque',loc,(w,.075,h),'brass',True)
    for i in range(4):
        box('Raised plaque inscription bars',(x-.05+i*.04,y+.055,z-.09+i*.06),
            (w*.67-i*.07,.04,.04),'gold_dark')


def wall_mirror():
    foundation(6)
    box('Portland stone gate wall',(0,.18,2.18),(6,1.44,3.76),'marble',True)
    masonry(6,1.03)
    for x in (-2.66,2.66): pilaster(x,4.06)
    for x in (-2.05,2.05):
        box('Inset indigo gate side panel',(x,.932,2.53),(.77,.08,2.65),'indigo',True)
        for z in (1.60,2.43,3.38): star((x,1.002,z),.12)
        for dx in (-.40,.40):
            box('Panel gold bead',(x+dx,.99,2.53),(.045,.05,2.75),'gold')
    box('Clock marble surround',(0,.96,2.70),(3.03,.20,2.96),'marble_dark',True)
    box('Pale clock frame',(0,1.02,2.70),(2.88,.19,2.82),'marble',True)
    slab('Small clock pediment',[(-1.60,4.03),(1.60,4.03),(0,4.40)],.90,1.18,'marble',True)
    beam('Pediment left gilt molding',(-1.59,1.20,4.04),(0,1.20,4.40),.07,.07,'gold')
    beam('Pediment right gilt molding',(0,1.20,4.40),(1.59,1.20,4.04),.07,.07,'gold')
    dial(0,1.10,2.70,1.20,broken=True)
    box('Standard measures brass plaque band',(0,1.06,1.07),(4.80,.16,.31),'brass',True)
    for j in range(29):
        box('Raised standard ruler division',(-2.24+j*.16,1.16,1.10),
            (.045,.045,.23 if j%4==0 else .12),'gold_dark')
    box('Upper gold cornice',(0,.20,3.99),(6,1.66,.15),'gold',True)
    coping(6,4.23)
    seam([(-2.47,.36),(-2.35,.65),(-2.46,1.02),(-2.18,1.43),(-2.30,1.90)],1.04)


def wall_west():
    foundation(6); masonry(6,3.94)
    for x in (-2.66,2.66): pilaster(x,3.94)
    x=-.65
    slab('Arched starry tall window',arch_profile(x,.62,1.6,3.0),.97,1.015,'night_sky')
    arch_frame(x,.62,1.6,3.0)
    for dx in (-.40,0,.40):
        top=2.82+math.sqrt(max(0,.8**2-dx**2))
        box('Thin brass window mullion',(x+dx,1.06,(.72+top)/2),(.045,.05,top-.72),'brass')
    for z in (1.38,2.12,2.82):
        box('Brass window transom',(x,1.06,z),(1.53,.05,.045),'brass')
    for dx,dz in ((-.52,1.05),(.20,1.13),(-.21,1.73),(.51,2.40),(-.42,2.54),(.20,3.12)):
        star((x+dx,1.044,dz),.09)
    # A single vertical meridian, physically interrupted and offset below crack.
    box('Prime meridian upper strip',(1.45,1.09,3.05),(.12,.12,1.94),'brass')
    box('Prime meridian shifted lower strip',(1.60,1.09,1.02),(.12,.12,1.92),'brass')
    for z in (.48,.90,1.32,1.74,2.35,2.77,3.19,3.61):
        box('Meridian standard measure tick',(1.60 if z<2 else 1.45,1.17,z),(.31,.045,.045),'gold')
    plaque((2.05,1.04,2.90),.50,.42)
    seam([(.63,1.87),(1.12,2.04),(1.47,1.98),(1.80,2.13),(2.44,2.05)],1.17)
    box('Meridian entablature',(0,.20,3.86),(6,1.64,.14),'gold',True)
    coping(6,4.05)


def wall_clock():
    foundation(6)
    box('Indigo paneled clock wall',(0,.18,2.12),(6,1.44,3.80),'indigo',True)
    for x in (-2.66,0,2.66): pilaster(x,4.05)
    for z in (.54,3.97):
        box('Marble wall frame rail',(0,1.00,z),(5.78,.20,.18),'marble',True)
        box('Fine gold wall frame rail',(0,1.12,z+.10),(5.78,.055,.055),'gold')
    # Integrated regulator: molded wood bonnet, cream dial, open pendulum glass.
    x=-1.40
    box('Regulator wood long case',(x,1.05,2.16),(1.48,.31,3.18),'wood_dark',True)
    box('Regulator plinth',(x,1.14,.66),(1.70,.43,.22),'wood_dark',True)
    box('Regulator bonnet cornice',(x,1.14,3.72),(1.73,.44,.18),'gold',True)
    dial(x,1.28,3.04,.53,one_hand=False)
    box('Pendulum glass window',(x,1.23,1.65),(.85,.055,1.40),'mirror',True)
    for dx in (-.49,.49):
        box('Pendulum window dark stile',(x+dx,1.31,1.66),(.10,.12,1.62),'wood_dark',True)
    # The flat grey export remains opaque; relief places the moving mechanism
    # in front of its blue glass backing so it stays visible in the game.
    beam('Brass pendulum suspension',(x,1.34,2.32),(x+.13,1.34,1.17),.075,.07,'brass')
    cylinder('Regulator pendulum bob',(x+.13,1.38,1.13),.24,.10,'brass',24,FACE)
    for z in (.86,2.43): box('Pendulum window rail',(x,1.32,z),(1.1,.13,.12),'gold')
    # Constellation chart on right.
    box('Star chart marble outer frame',(1.40,1.03,2.23),(2.07,.19,2.89),'marble',True)
    box('Framed navy star map',(1.40,1.15,2.23),(1.80,.07,2.61),'night_sky')
    ring('Chart brass coordinate circle',(1.40,1.22,2.32),.75,.055,.05,'brass',FACE,40)
    coords=[(.77,1.42),(1.22,1.76),(1.00,2.35),(1.52,2.63),(1.99,2.32),(1.80,3.18),(1.12,3.32)]
    tube('Raised steel constellation',[(xx,1.23,zz) for xx,zz in coords],.025,'steel')
    for j,(xx,zz) in enumerate(coords): star((xx,1.27,zz),.09+(j%2)*.025)
    seam([(-2.28,1.23),(-1.85,1.43),(-1.60,1.81),(-1.22,1.93)],1.40)
    seam([(.55,2.76),(.96,2.53),(1.32,2.71),(1.61,2.44),(2.20,2.59)],1.29)
    # Actual absent chart corner, hovering independently above the frame.
    box('Hovering broken chart frame corner',(2.18,1.15,3.95),(.45,.23,.22),'marble',True,rot=(.07,.19,.08))
    coping(6,4.13)
    # Front is +Y, so positive world X is the viewer's left.
    for obj in PARTS:
        obj.data.transform(Matrix.Diagonal(Vector((-1,1,1,1))))


def wall_debris():
    box('Floor level navy island rim',(0,-.10,-.14),(6,1.60,.28),'void_stone',True)
    box('Marble balustrade continuous shoe',(0,.36,.10),(6,.68,.20),'marble_dark',True)
    for x in (-2.70,-.12,2.70):
        box('Balustrade solid square pier',(x,.35,.55),(.43,.54,.93),'marble',True)
        box('Balustrade pier cap',(x,.35,1.04),(.56,.65,.15),'marble',True)
    for j,x in enumerate((-2.20,-1.63,-1.06,-.55,.44,1.02,1.60,2.17)):
        if j in (2,5): continue
        start=len(PARTS)
        cylinder('Turned baluster shaft',(x,.34,.54),.105,.62,'marble',10)
        for z,r in ((.28,.18),(.52,.15),(.84,.17)):
            cylinder('Baluster turned collar',(x,.34,z),r,.12,'marble',10)
        if j==3:
            # Broken fallen baluster lies along the edge, below the intact cap.
            transform=Matrix.Translation(Vector((x,.34,.43))) @ Euler((.2,math.pi/2,0)).to_matrix().to_4x4() @ Matrix.Translation(Vector((-x,-.34,-.54)))
            for obj in PARTS[start:]: obj.data.transform(transform)
    for a,b in ((-3,-1.20),(-.65,.78),(1.49,3)):
        slab('Broken balustrade handrail',[(a,.90),(b,.90),(b,1.05),(b-.20,1.14),(a,1.10)],.02,.67,'marble',True)
    beam('Snapped brass railing',(.85,.38,.88),(1.72,.44,1.10),.07,.07,'brass')
    beam('Bent brass rail end',(1.72,.44,1.10),(2.03,.46,.88),.07,.07,'brass')
    # Foundation hangs behind the room-facing rim. Tapered masonry and angular
    # rocks form one substantial floating-island underside, not surface rubble.
    for row,(z,w,d) in enumerate(((-.52,5.9,1.45),(-1.09,4.90,1.22),(-1.64,3.64,1.05))):
        for j in range(5-row):
            count=5-row
            x=-w/2+(j+.5)*w/count
            box('Exposed hanging foundation ashlar',(x,-.16,z),
                (w/count-.04,d,.50),'void_stone_dark' if (row+j)%3 else 'void_stone',True,
                rot=(0,.035*(-1)**j,0))
    rock('Deep fractured island keel',(-.15,-.20,-2.07),(2.65,1.15,1.24),'void_stone_dark',9)
    for j,(x,z,s) in enumerate(((-2.19,-2.20,.54),(1.83,-2.45,.65),(.30,-3.20,.60))):
        rock('Separately hovering underside chunk',(x,-.24,z),(s,.65,.60),'void_stone',21+j)
    seam([(-2.70,-.18),(-2.15,-.44),(-2.29,-.78),(-1.64,-1.11),(-1.78,-1.43)],.61)
    seam([(.32,-.20),(.12,-.60),(.39,-.98),(.10,-1.34),(.23,-1.92),(-.10,-2.54)],.41)
    seam([(-2.86,.12),(-2.52,.30),(-2.61,.67),(-2.41,.97)],.64)
    for j,(x,y,z) in enumerate(((-1.20,.35,-1.39),(.65,.30,-1.92),(1.35,.35,-.85))):
        crystal((x,y,z),.48,(-math.pi*.70,0,j*.60))
        crystal((x+.18,y,z-.05),.30,(-math.pi*.65,.40,j*.60))
    rock('Hovering balustrade cap',(-1.00,.12,1.40),(.44,.42,.24),'marble',41)
    rock('Hovering fractured rim cap',(1.18,.05,1.37),(.34,.35,.20),'marble',42)


def telescope(loc, length=3.6, radius=.29, rot=(0,.65,0)):
    # Instrument local +Z is the objective direction; negative end is eyepiece.
    start=len(PARTS)
    cylinder('Dark wood refractor tube',(0,0,0),radius,length,'wood_dark',24)
    for z in (-length*.45,-length*.23,length*.06,length*.34,length*.49):
        cylinder('Brass refractor tube collar',(0,0,z),radius*1.13,.12,'brass',24,bevel=True)
    cylinder('Objective flared brass hood',(0,0,length*.47),radius*1.27,.28,'brass',24)
    cylinder('Silver blue objective lens',(0,0,length*.5+.148),radius*1.12,.055,'mirror',32)
    ring('Objective dark retaining bead',(0,0,length*.5+.18),radius*1.24,.07,.07,'gold_dark',n=32)
    cylinder('Eyepiece focusing tube',(0,0,-length*.5-.17),radius*.36,.37,'brass',12)
    cylinder('Eyepiece glass',(0,0,-length*.5-.36),radius*.30,.06,'mirror',12)
    beam('Finder telescope mounting rail',(.0,radius*.99,-.5),(0,radius*.99,.65),.065,.07,'brass')
    cylinder('Small parallel finder scope',(0,radius*1.53,.28),.078,.83,'brass',12)
    group(start,loc,rot)


def dome_point(x,a,offset=0):
    # A quarter-cylinder recedes in Y; its crown also falls toward either end
    # of this wide dome segment. That broad arch keeps the observatory readable
    # in flat cel colours rather than resembling two upright rectangular doors.
    crown=math.sqrt(max(.10,1-(x/4.25)**2))
    return (x,-.84+1.75*math.cos(a)+offset,
            2.14+2.80*math.sin(a)*crown+offset*.5)


def dome_plate(x0,x1,a0,a1,loc=(0,0,0)):
    verts=[dome_point(x,a,off) for off in (-.12,0)
           for x in (x0,x1) for a in (a0,a1)]
    faces=[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]
    return place(mesh('Curved observatory shell plate',verts,faces,'void_stone'),loc)


def wall_dome():
    foundation(8)
    box('Lower pale observatory drum',(0,.16,1.19),(8,1.44,1.72),'marble',True)
    box('Drum navy dado',(0,.93,.67),(8,.13,.55),'void_stone')
    for z in (.39,1.98): box('Drum brass encircling band',(0,.22,z),(8,1.65,.12),'gold',True)
    for x in (-3.32,-2.06,2.06,3.32):
        cylinder('Round blue dome window',(x,.96,1.40),.31,.11,'mirror',24,FACE)
        ring('Porthole brass rim',(x,1.055,1.40),.38,.09,.10,'gold',FACE,24)
        for dx in (-.12,.12): box('Window vertical division',(x+dx,1.12,1.40),(.045,.04,.49),'brass')
    for side in (-1,1):
        lo,hi=(-4,-.51) if side<0 else (.51,4)
        for j in range(7):
            a0=j*math.pi/14; a1=(j+1)*math.pi/14
            for k in range(3):
                x0=lo+(hi-lo)*k/3; x1=lo+(hi-lo)*(k+1)/3
                # Two missing plates leave genuine sockets in the curved roof.
                if (side,j,k) in ((-1,4,1),(1,5,1)):
                    dome_plate(x0+.08,x1-.08,a0+.01,a1-.01,(0,.26,.24))
                else: dome_plate(x0+.012,x1-.012,a0+.005,a1-.005)
        for xx in (lo+.06,(lo+hi)/2,hi-.06):
            tube('Arcing brass dome rib',[dome_point(xx,j*math.pi/28,.035) for j in range(15)],.045,'brass',6)
    # Roof slit is open all the way to its crown; instrument projects through.
    for x in (-.50,.50):
        tube('Slit reinforced gold rim',[dome_point(x,j*math.pi/28,.055) for j in range(15)],.065,'gold',6)
    telescope((0,.22,3.40),2.33,.29,rot=(-.40,-.55,0))
    crack([dome_point(-3.74,.72,.035),dome_point(-3.10,.78,.035),
           dome_point(-2.78,.66,.035),dome_point(-2.09,.79,.035),dome_point(-1.49,.74,.035)])
    crack([dome_point(1.02,1.19,.035),dome_point(1.49,1.10,.035),
           dome_point(2.13,1.23,.035),dome_point(2.61,1.17,.035),dome_point(3.31,1.30,.035)])
    seam([(-1.37,.38),(-1.53,.82),(-1.24,1.10),(-1.46,1.51),(-1.11,1.97)],1.04)
    # Broken crown strip and hovering fragments keep the room-edge silhouette.
    for a,b in ((-3.94,-2.2),(-2.17,-.51),(.51,2.6),(2.63,3.94)):
        tube('Broken arched dome crown lip',
             [dome_point(a+(b-a)*j/10,math.pi/2,.025) for j in range(11)],.055,'gold_dark')
    crown_z=dome_point(-2.80,math.pi/2)[2]
    rock('Hovering dome crown stone',(-2.80,-.70,crown_z+.38),(.40,.30,.20),'marble',99)


def wall_arch():
    foundation(6)
    # Real architectural opening: piers and fitted upper spandrels only.
    for s in (-1,1):
        box('Navy arch side pier',(s*2.24,.18,2.05),(1.52,1.44,3.55),'void_stone',True)
        pilaster(s*2.66,4.08)
    for j in range(16):
        a=-3+j*6/16; b=a+6/16
        def edge(x): return 2.45+math.sqrt(max(0,1.43**2-x*x)) if abs(x)<1.43 else 2.45
        slab('Fitted navy arch spandrel',[(a,edge(a)),(b,edge(b)),(b,4.12),(a,4.12)],-.54,.97,'void_stone')
    slab('Deep starry arch opening',arch_profile(0,.40,2.86,3.48),-.05,.04,'night_sky')
    arch_frame(0,.40,2.86,3.48)
    for j in range(12):
        x=random.uniform(-1.25,1.25); z=random.uniform(.65,3.50)
        if z<2.45+math.sqrt(max(0,1.35**2-x*x)): star((x,.09,z),.08+(j%3)*.015)
    # Small armillary motif hangs forward of the sky, inside the arch.
    c=(0,.72,2.30)
    ring('Hanging armillary front meridian',c,.78,.075,.08,'brass',FACE,40)
    ring('Hanging armillary oblique circle',c,.77,.065,.075,'brass',(1.15,.65,.3),40)
    ring('Hanging armillary horizontal circle',c,.76,.065,.075,'brass',(.18,.10,0),40)
    beam('Armillary suspension',(0,.72,3.61),(0,.72,3.04),.065,.065,'brass')
    sphere('Armillary crystal centre',c,.16,'crystal',12,6)
    slab('Pale meridian keystone',[(-.13,3.78),(.13,3.78),(.25,4.22),(-.25,4.22)],.98,1.24,'marble',True)
    box('Keystone prime meridian',(0,1.28,4.02),(.12,.07,.35),'brass')
    for z in (.53,4.09): box('Spandrel gold horizontal trim',(0,1.02,z),(6,.09,.075),'gold')
    coping(6,4.23)
    seam([(-2.35,.39),(-2.14,.91),(-2.31,1.39),(-2.05,1.79),(-2.17,2.25)],1.035)
    seam([(.26,3.98),(.44,4.14),(.37,4.28)],1.28)


def wall_filler():
    foundation(4); masonry(4,3.37)
    box('Recessed indigo filler panel',(0,1.005,2.24),(2.62,.09,1.44),'indigo',True)
    for x in (-1.36,1.36): box('Marble recessed panel stile',(x,1.04,2.24),(.15,.15,1.65),'marble',True)
    for z in (1.46,3.02): box('Pale panel header and sill',(0,1.04,z),(2.88,.16,.14),'marble',True)
    # Small quiet 24-hour roundel, without hands.
    cylinder('Cream twenty four tick roundel',(0,1.08,2.24),.48,.10,'dial_face',32,FACE)
    ring('Brass filler roundel rim',(0,1.15,2.24),.55,.08,.08,'brass',FACE,32)
    ticks(0,1.17,2.24,.48)
    box('Horizontal brass observatory datum',(0,1.015,1.10),(4,.12,.12),'brass')
    coping(4,3.43)
    seam([(-1.71,.36),(-1.48,.75),(-1.60,1.14),(-1.28,1.48),(-1.43,1.87),(-1.26,2.32)],1.11)


def platform(w,d,h=.36):
    box('Blue grey stone combat footing',(0,0,.12),(w,d,.24),'marble_dark',True)
    box('Gold edge fillet',(0,0,.265),(w-.12,d-.12,.07),'gold',True)
    box('Upper stone platform',(0,0,(.30+h)/2),(w-.26,d-.26,h-.30),'marble',True)


def gear(loc,r=.45,rot=FACE,role='steel',teeth=16):
    start=len(PARTS)
    ring('Mechanism gear web',(0,0,0),r*.84,r*.28,.12,role,n=24)
    cylinder('Gear hub',(0,0,0),r*.22,.22,'brass',12)
    for j in range(teeth):
        a=TAU*j/teeth
        box('Gear tooth',(r*.88*math.sin(a),r*.88*math.cos(a),0),
            (r*.20,r*.24,.13),role,rot=(0,0,-a))
    for j in range(5):
        a=TAU*j/5
        beam('Gear spoke',(.1*math.sin(a),.1*math.cos(a),0),
             (r*.65*math.sin(a),r*.65*math.cos(a),0),.065,.08,role)
    group(start,loc,rot)


def landmark_ball():
    platform(3,2.4,.48)
    box('Time ball stepped pedestal',(0,0,.83),(2.32,1.72,.70),'marble',True)
    box('Time ball pedestal cap',(0,0,1.23),(2.48,1.88,.16),'marble',True)
    for y in (-.874,.874):
        box('Navy pedestal side panel',(0,y,.86),(1.76,.055,.42),'void_stone',True)
    for x in (-1.174,1.174):
        box('Navy pedestal end panel',(x,0,.86),(.055,1.17,.42),'void_stone',True)
    plaque((0,.931,.86),.96,.28)
    cylinder('Tall iron time ball mast',(0,0,2.91),.105,3.38,'iron_dark',12)
    cylinder('Steel mast highlight',(0,0,3.33),.062,2.52,'steel',12)
    for z in (1.43,2.06,2.72,4.24,4.53):
        cylinder('Gold mast collar',(0,0,z),.16,.11,'gold',16)
    sphere('Red Greenwich time ball mid drop',(0,0,3.49),.55,'red_lacquer',24,12)
    ring('Gold equatorial time ball band',(0,0,3.49),.558,.07,.11,'gold',n=40)
    # Broad guide cage feet and an exposed drop-height rule.
    for x in (-.21,.21):
        beam('Mast guide foot',(x,0,1.34),(x*.50,0,2.15),.075,.075,'iron_dark')
    for j in range(5): box('Mast height scale division',(.13,.09,1.72+j*.23),(.13,.05,.045),'brass')
    seam([(-1.12,.48),(-.96,.71),(-1.02,.92),(-.79,1.22)],.94)
    crack([(-1.36,.75,.247),(-1.12,.51,.247),(-1.22,.24,.247),(-.93,-.08,.247)])
    rock('Hovering broken time ball base block',(1.04,-.42,1.61),(.44,.39,.31),'marble',312)


def armillary_rings(c,r=1.23):
    ring('Armillary equator',c,r,.085,.085,'brass',n=48)
    ring('Armillary prime meridian',c,r,.085,.085,'brass',FACE,48)
    ring('Armillary second meridian',c,r-.06,.075,.075,'brass',(0,math.pi/2,0),48)
    ring('Ecliptic tilted twenty three degrees',c,r+.08,.10,.085,'brass',(math.radians(23),0,0),48,
         a0=.52,a1=TAU-.25)
    # The absent ecliptic arc is translated beyond the corresponding socket.
    ring('Hovering snapped ecliptic arc',(c[0]+.25,c[1]+.20,c[2]+.18),r+.08,.10,.085,
         'brass',(math.radians(23),0,0),12,a0=-.25,a1=.52)
    for dz in (-.53,.53):
        ring('Armillary tropic circle',(c[0],c[1],c[2]+dz),r*.88,.065,.07,'brass',n=40)
    beam('Dark golden celestial polar axis',(c[0],c[1]-.45,c[2]-r-.12),
         (c[0],c[1]+.45,c[2]+r+.12),.085,.085,'gold_dark')


def landmark_armillary():
    platform(4.6,3.2,.43)
    box('Armillary pale pedestal',(0,0,.77),(2.48,1.93,.67),'marble',True)
    for y in (-.982,.982):
        box('Armillary navy pedestal panel',(0,y,.77),(1.94,.055,.40),'void_stone',True)
    for x in (-1.256,1.256):
        box('Pedestal navy return panel',(x,0,.77),(.05,1.40,.40),'void_stone')
    cylinder('Armillary pedestal circular capital',(0,0,1.12),1.13,.18,'marble',32,bevel=True)
    ring('Twenty four hour pedestal datum',(0,0,1.225),1.10,.20,.04,'dial_face',n=48)
    for j in range(24):
        a=TAU*j/24
        box('Armillary pedestal hour tick',(.99*math.sin(a),.99*math.cos(a),1.254),
            (.055,.17 if j%6==0 else .10,.045),'dial_mark',rot=(0,0,-a))
    c=(0,0,2.30)
    armillary_rings(c,1.15)
    sphere('Small crystalline earth',c,.28,'crystal',20,10)
    ring('Earth equatorial band',c,.286,.04,.05,'gold_dark',n=24)
    crack([(-1.90,.85,.247),(-1.60,.50,.247),(-1.73,.22,.247),(-1.42,-.10,.247)])


def landmark_telescope():
    platform(5.4,2.6,.42)
    box('Heavy equatorial mount foot',(0,0,.58),(1.92,1.44,.35),'iron',True)
    cylinder('Equatorial pedestal',(0,0,1.12),.38,1.05,'iron',16,bevel=True)
    for z in (.75,1.51): cylinder('Mount steel collar',(0,0,z),.48,.16,'steel',20)
    beam('Slanted polar bearing',(-.20,0,1.32),(.24,0,2.05),.39,.38,'iron',True)
    gear((0,.32,1.70),.46,FACE,'steel',18)
    beam('Counterweight declination shaft',(-.58,0,1.62),(.62,0,2.15),.12,.12,'steel')
    cylinder('Heavy iron counterweight',(-.66,0,1.57),.28,.38,'iron_dark',16,rot=(0,1.12,0),bevel=True)
    beam('Tube saddle',(-.37,0,1.88),(.39,0,2.12),.36,.40,'brass',True)
    telescope((.12,0,2.10),3.85,.28,rot=(0,math.radians(68),0))
    gear((1.52,-.48,1.42),.37,(1.30,.10,.20),'steel',16)
    crack([(-2.57,.74,.247),(-2.13,.47,.247),(-1.94,.59,.247),(-1.57,.25,.247),(-1.26,.30,.247)])
    seam([(-2.58,.02),(-2.39,.12),(-2.12,.21)],1.25)


def landmark_orrery():
    cylinder('Solid round marble orrery plinth',(0,0,.17),2.2,.34,'marble',64,bevel=True)
    ring('Orrery lower gold molding',(0,0,.10),2.18,.12,.12,'gold',n=56)
    cylinder('Indigo star chart tabletop',(0,0,.375),2.04,.13,'indigo',56)
    for r in (.84,1.32,1.84): ring('Inlaid brass orbital chart',(0,0,.448),r,.045,.04,'brass',n=48)
    for j in range(22):
        a=j*2.39996; r=.69+(j%6)*.22
        star((r*math.sin(a),r*math.cos(a),.475),.07+(j%3)*.012,rot=FACE)
    for j in range(6):
        a=TAU*j/6
        gear((.63*math.cos(a),.63*math.sin(a),.59),.27,(0,0,a),'steel',12)
    cylinder('Brass orrery central column',(0,0,1.82),.15,2.52,'brass',16)
    sphere('Central gold sun',(0,0,3.22),.38,'gold',20,10)
    ring('Sun horizon aureole',(0,0,3.22),.48,.055,.055,'brass',n=24)
    roles=['crystal','porcelain','red_lacquer','mirror','crystal']
    for j,(r,z,a) in enumerate(((.62,2.88,.20),(1.05,2.49,1.83),(1.44,2.04,3.25),
                               (1.70,1.59,4.58),(1.66,1.09,5.78))):
        tip=Vector((r*math.cos(a),r*math.sin(a),z))
        if j==3:
            direction=Vector((math.cos(a),math.sin(a),0))
            beam('Snapped inner planet arm',(0,0,z),tuple(direction*.68+Vector((0,0,z))),.085,.085,'brass')
            tip+=Vector((.15,.10,.26))
            beam('Floating broken planet arm',tuple(direction*.94+Vector((.15,.10,z+.26))),tip,.085,.085,'brass')
            sphere('Time light at snapped planet arm',tuple(direction*.68+Vector((0,0,z))),.065,'crack_gold',8,4)
        else: beam('Planet orbital arm',(0,0,z),tip,.085,.085,'brass')
        cylinder('Stacked arm pivot collar',(0,0,z),.24,.11,'gold',16)
        beam('Planet upright',tip,tip+Vector((0,0,.20)),.065,.065,'brass')
        sphere('Orrery planet '+str(j),tip+Vector((0,0,.28)),.15+j*.019,roles[j],12,6)
        if j==4: ring('Outer planet ring',tip+Vector((0,0,.28)),.36,.055,.05,'gold',(.20,.25,0),32)
    crack([(-1.92,.64,.452),(-1.62,.31,.452),(-1.70,-.05,.452),(-1.36,-.24,.452)])


def globe_surface(lon,lat,r=.87):
    return Vector((r*math.cos(lat)*math.cos(lon),r*math.cos(lat)*math.sin(lon),r*math.sin(lat)))


def globe_patch(c,lon0,lon1,lat0,lat1,r=.87,detached=False):
    # Eight-vertex closed curved patch; lifted shell replaces a missing patch.
    verts=[tuple(Vector(c)+globe_surface(lon,lat,rr)) for rr in (r-.065,r)
           for lon,lat in ((lon0,lat0),(lon1,lat0),(lon1,lat1),(lon0,lat1))]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return mesh('Lifted celestial globe shell' if detached else 'Celestial globe shell tile',verts,faces,'night_sky')


def landmark_globe():
    platform(2.8,2.8,.36)
    for x in (-.73,.73):
        for y in (-.73,.73):
            beam('Four dark wood globe stand legs',(x,y,.37),(x*.75,y*.75,1.20),.16,.16,'wood_dark',True)
            cylinder('Brass stand foot',(x,y,.40),.14,.10,'brass',12)
    for y in (-.55,.55): box('Wood stand cross stretcher',(0,y,.75),(1.30,.12,.12),'wood_dark',True)
    c=Vector((0,0,1.77)); r=.87
    # Sphere body with one actual missing shell tile on the visible front-right.
    # Polar caps use a small inner solid while middle bands stay closed tiles.
    sphere('Globe dark inner core',c,.78,'void_stone_dark',24,12)
    for k in range(6):
        lat0=-math.pi/2+k*math.pi/6+.005
        lat1=-math.pi/2+(k+1)*math.pi/6-.005
        # Avoid degenerate vertices at either pole.
        lat0=max(lat0,-math.pi/2+.005); lat1=min(lat1,math.pi/2-.005)
        for j in range(16):
            lon0=TAU*j/16+.003; lon1=TAU*(j+1)/16-.003
            detached=(j==3 and k==4)
            offset=Vector((.10,.22,.16)) if detached else Vector((0,0,0))
            globe_patch(c+offset,lon0,lon1,lat0,lat1,r,detached)
    ring('Celestial globe brass horizon',(0,0,1.65),1.10,.09,.08,'brass',n=48)
    ring('Celestial globe tilted meridian',c,1.00,.085,.08,'brass',(math.pi/2,.20,.20),48)
    # Stars and raised constellations follow the sphere, including its crown.
    coords=[(.35,.40),(.78,.72),(1.14,.35),(1.68,.62),(2.00,.20),(2.55,.38),
            (2.91,.79),(3.52,.22),(4.32,.65),(5.12,.37),(5.70,.89)]
    for lon,lat in coords:
        p=c+globe_surface(lon,lat,.90)
        obj=star(p,.075)
        # Star local Y is normal to its face.
        normal=(p-c).normalized()
        obj.data.transform(Matrix.Translation(-p))
        obj.data.transform(normal.to_track_quat('Y','Z').to_matrix().to_4x4())
        obj.data.transform(Matrix.Translation(p))
    for a,b in zip(coords[:6],coords[1:6]):
        pts=[tuple(c+globe_surface(a[0]+(b[0]-a[0])*t/5,a[1]+(b[1]-a[1])*t/5,.90)) for t in range(6)]
        tube('Globe raised brass constellation',pts,.023,'brass')
    crack([tuple(c+globe_surface(lon,lat,.895)) for lon,lat in
           ((.40,-.35),(.71,-.12),(.85,.12),(1.13,.25),(1.30,.49),(1.57,.64),(1.70,.92))])


def landmark_sundial():
    platform(4,2.6,.42)
    box('Long low meridian monument slab',(0,0,.69),(3.70,2.28,.54),'marble',True)
    for y in (-1.158,1.158):
        box('Monument navy side inset',(0,y,.70),(3.18,.055,.30),'void_stone',True)
        for z in (.52,.87): box('Monument side gold trim',(0,y+.01,z),(3.4,.05,.055),'gold')
    for x in (-1.872,1.872):
        box('Monument navy end inset',(x,0,.70),(.055,1.70,.30),'void_stone')
    # Long straight prime meridian along X, lower half laterally offset 0.15m.
    box('Prime meridian west strip',(-1.09,.075,.995),(1.42,.12,.07),'brass')
    box('Prime meridian east shifted strip',(.83,-.075,.995),(2.25,.12,.07),'brass')
    for j in range(11):
        x=-1.70+j*.31
        box('Slab meridian measuring notch',(x,.075 if x<-.35 else -.075,1.042),(.045,.28,.04),'gold_dark')
    x=.71
    cylinder('Sundial marble drum',(x,0,1.07),.91,.20,'marble',40,bevel=True)
    cylinder('Cream sundial face',(x,0,1.195),.84,.06,'dial_face',48)
    ring('Gold sundial outer rim',(x,0,1.23),.90,.075,.08,'gold',n=48)
    for j in range(24):
        a=TAU*j/24
        box('Twenty four sundial hour tick',(x+.70*math.sin(a),.70*math.cos(a),1.244),
            (.05,.19 if j%6==0 else .12,.045),'dial_mark',rot=(0,0,-a))
    slab('Tall triangular brass sundial gnomon',[(x-.46,1.26),(x+.47,1.26),(x+.34,2.40)],-.065,.065,'brass',True)
    beam('Gnomon dark measuring edge',(x-.44,.073,1.28),(x+.34,.073,2.37),.045,.045,'gold_dark')
    crack([(-.34,-1.12,.977),(-.48,-.70,.977),(-.29,-.40,.977),(-.42,-.05,.977),
           (-.19,.25,.977),(-.35,.65,.977),(-.14,1.11,.977)])
    seam([(-.35,.46),(-.21,.63),(-.37,.82),(-.14,.97)],1.187)


# Width/depth/height values for landmarks are the complete prescribed bounds.
# Walls retain the specified wall line, front depth and floor origin directly.
SPECS = [
    ('Wall_Mirror',6,None,wall_mirror,'24-HOUR GATE CLOCK'),
    ('Wall_MemoryWest',6,None,wall_west,'PRIME MERIDIAN WINDOW'),
    ('Wall_MemoryClock',6,None,wall_clock,'REGULATOR + STAR CHART'),
    ('Wall_DebrisEdge',6,None,wall_debris,'SUSPENDED ISLAND FOUNDATION'),
    ('Wall_ClockHeart',8,None,wall_dome,'OPEN-SLIT OBSERVATORY DOME'),
    ('Wall_StarArch',6,None,wall_arch,'CELESTIAL ARCH COLONNADE'),
    ('Wall',4,None,wall_filler,'24-TICK FILLER'),
    ('LM_MirrorMonolith',None,(3.0,2.4,4.6),landmark_ball,'RED TIME BALL MAST'),
    ('LM_SaloonFragment',None,(4.6,3.2,3.6),landmark_armillary,'ARMILLARY SPHERE'),
    ('LM_TrainFragment',None,(5.4,2.6,3.2),landmark_telescope,'GREAT REFRACTOR'),
    ('LM_ClockHeart',None,(4.4,4.4,3.6),landmark_orrery,'PLANETARY ORRERY'),
    ('LM_Hourglass',None,(2.8,2.8,2.8),landmark_globe,'CELESTIAL GLOBE'),
    ('LM_DesertShard',None,(4.0,2.6,2.4),landmark_sundial,'MERIDIAN + SUNDIAL'),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def finish(name,width,dims):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True); bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name,obj.data.name=name,name+'_Mesh'
    low,high=bounds(obj)
    if dims:
        span=high-low; centre=(low+high)/2
        # Platforms already have exact footprints. Fit final heights and account
        # for the armillary's small floating arc without moving the floor origin.
        for v in obj.data.vertices:
            v.co.x=(v.co.x-centre.x)*dims[0]/span.x
            v.co.y=(v.co.y-centre.y)*dims[1]/span.y
            v.co.z=(v.co.z-low.z)*dims[2]/span.z
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name+' contains open components'
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth=False
    obj.data.update(); bpy.context.view_layer.update()
    low,high=bounds(obj)
    assert triangles(obj)<=8000,(name,triangles(obj),'triangle budget exceeded')
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co),name
    if width:
        assert abs(low.x+width/2)<1e-4 and abs(high.x-width/2)<1e-4,(name,low,high)
        assert 1.2<=high.y-low.y<=2.2+.001,(name,'wall depth',low,high)
        floor=-3.5 if name=='Wall_DebrisEdge' else 0
        assert abs(low.z-floor)<1e-4,(name,'floor',low)
        assert high.z<=5.05,(name,high)
    else:
        assert abs(low.z)<1e-5 and abs(low.x+high.x)<1e-4 and abs(low.y+high.y)<1e-4
        assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in range(3)),(name,obj.dimensions)
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)>=6 and used.issubset(PALETTE) and 'crack_gold' in used,(name,used)
    # Measure only faces visible from the front or above, not hidden backs.
    areas={}
    for p in obj.data.polygons:
        if p.normal.y>.3 or p.normal.z>.3:
            role=obj.data.materials[p.material_index].name
            areas[role]=areas.get(role,0)+p.area
    share=max(areas.values())/sum(areas.values())
    assert share<=.56,(name,'dominant visible role',share,areas)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    assert obj.location.length<1e-8 and tuple(obj.scale)==(1,1,1)
    obj['chapter']='Greenwich Observatory of Time'
    obj['front']='+Y'; obj['triangle_count']=triangles(obj)
    obj['origin_rule']='Floor Z=0; underside to -3.5m' if name=='Wall_DebrisEdge' else 'Bottom centre'
    path=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    assert path.is_file() and path.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    print(f'VALIDATED {name}: {len(used)} roles; largest visible role {share:.1%}',flush=True)
    return obj


def preview(objects):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=.85; shade.curvature_valley_factor=1.05
    shade.cavity_ridge_factor=1.05; shade.cavity_valley_factor=1.05
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review navy background')
    scene.world.color=(.018,.027,.047)
    helpers=[]
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
    # Small side angle reveals ring crossings and clock relief; +Y stays front.
    az=math.radians(20); el=math.radians(55)
    camera.location=Vector((math.sin(az)*40*math.cos(el),math.cos(az)*40*math.cos(el),40*math.sin(el)))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'
    rotation=camera.rotation_euler.to_matrix(); inverse=rotation.transposed()

    def cloud(obj):
        return [inverse @ (obj.matrix_world @ v.co) for v in obj.data.vertices]

    def label(text,x,y,size,role='starlight'):
        data=bpy.data.curves.new('Review caption','FONT')
        data.body=text; data.size=size; data.align_x='CENTER'
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('Review caption',data)
        scene.collection.objects.link(obj); helpers.append(obj)
        obj.rotation_euler=camera.rotation_euler
        obj.location=rotation @ Vector((x,y,10))

    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        # Screen-space grid prevents tall underside debris from obscuring the
        # next row. Real object scale is shared across all thirteen assets.
        rows=[[objects[i] for i in (0,1,2,5)],
              [objects[i] for i in (3,4,6,7)],
              [objects[i] for i in (8,9,10,11,12)]]
        total_width=29.0; row_height=6.10
        top=7.0
        for row_index,row in enumerate(rows):
            widths=[]
            for obj in row:
                pp=cloud(obj); widths.append(max(p.x for p in pp)-min(p.x for p in pp))
            gap=(total_width-sum(widths))/len(row)
            cursor=-total_width/2
            for obj,w in zip(row,widths):
                pp=cloud(obj)
                lo=min(p.x for p in pp); hi=max(p.x for p in pp)
                bot=min(p.y for p in pp); cap=max(p.y for p in pp)
                cx=cursor+(w+gap)/2
                cy=top-row_index*row_height
                offset=Vector((cx-(lo+hi)/2,cy-(bot+cap)/2,0))
                obj.location=rotation @ offset
                idx=objects.index(obj)
                baseline=cy-max(2.38,(cap-bot)/2+.27)
                label(obj.name,cx,baseline,.255)
                label(SPECS[idx][4],cx,baseline-.36,.17,'gold')
                dims=obj.dimensions
                label(f'{dims.x:.1f} x {dims.y:.1f} x {dims.z:.1f} m  /  {triangles(obj):,} tris',
                      cx,baseline-.68,.155,'mirror')
                cursor+=w+gap
        label('V  /  GREENWICH OBSERVATORY OF TIME',0,10.80,.55)
        label('13 MODULAR REPLACEMENTS     /     PALE STONE + NAVY + BRASS     /     CYAN TIME FRACTURES',0,10.12,.22,'gold')
        label('1 : 1 METRES     /     +Y FRONT     /     55 DEG ABOVE-FRONT     /     GREY ROLE MATERIALS IN FBX',0,-9.12,.19,'mirror')
        bpy.context.view_layer.update()
        pp=[p for obj in objects for p in cloud(obj)]
        pp += [inverse @ (obj.matrix_world @ Vector(c)) for obj in helpers if obj.type=='FONT' for c in obj.bound_box]
        x0=min(p.x for p in pp); x1=max(p.x for p in pp)
        y0=min(p.y for p in pp); y1=max(p.y for p in pp)
        camera.location+=rotation @ Vector(((x0+x1)/2,(y0+y1)/2,0))
        camera.data.ortho_scale=max(x1-x0,(y1-y0)*1.8)*1.045
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for helper in helpers: bpy.data.objects.remove(helper,do_unlink=True)
        for obj in objects: obj.location=(0,0,0)
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for index,(name,width,dims,build,_) in enumerate(SPECS):
        PARTS=[]; random.seed(20261120+index*7919)
        build(); objects.append(finish(name,width,dims))
    assert len(scene.objects)==13 and all(obj.type=='MESH' for obj in scene.objects)
    preview(objects)
    assert len(scene.objects)==13 and all(obj.type=='MESH' for obj in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
