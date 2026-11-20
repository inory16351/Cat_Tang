# 붕괴하는 시간 감옥의 대형 엄폐물 6종과 검수용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_landmarks.py
"""Deterministic Blender 5.x kit; metres, Z up, +Y front, bottom-centre origins.

All components are closed solids, including detached debris. Each export is one
mesh with seven or eight grey role materials. Review swatches are applied only
after all exports. Broad, continuous foundations make these combat blockers.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_landmarks_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
PALETTE = {
    'mirror': (.56, .79, .92), 'mirror_back': (.16, .28, .43),
    'marble': (.79, .74, .89), 'marble_dark': (.25, .20, .36),
    'gold': (.94, .61, .16), 'gold_dark': (.43, .27, .10),
    'crack_gold': (1.0, .85, .32), 'violet': (.52, .22, .79),
    'violet_dark': (.17, .075, .29), 'starlight': (.92, .96, 1.0),
    'wood': (.44, .30, .38), 'wood_dark': (.16, .09, .18),
    'cloth': (.59, .43, .66), 'void_stone': (.28, .23, .43),
    'lamp_warm': (1.0, .66, .27), 'iron_dark': (.065, .075, .15),
    'red_lacquer': (.54, .10, .25), 'steel': (.39, .48, .63),
    'brass': (.66, .44, .20), 'dial_face': (.85, .81, .90),
    'dial_mark': (.12, .075, .22), 'crystal': (.54, .58, .88),
    'sand': (.62, .43, .57),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        bsdf = mat.node_tree.nodes['Principled BSDF']
        bsdf.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        bsdf.inputs['Metallic'].default_value = 0
        bsdf.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Soft 3cm carved edges', 'BEVEL')
        mod.width, mod.segments = .03, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(60)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def group(start, loc=(0, 0, 0), rot=(0, 0, 0)):
    for obj in PARTS[start:]:
        place(obj, loc, rot)


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=20, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, n=20, loc=(0,0,0), rot=(0,0,0)):
    # Closed cross-section revolved without duplicate vertices at the poles.
    # Use a small positive bore radius in profiles, or cylinder for solid caps.
    v = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z)
         for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,
          ((k+1)%len(profile))*n+j) for k in range(len(profile)) for j in range(n)]
    return place(mesh(label,v,f,role),loc,rot)


def ring(label, r, w, z, h, role, n=32, loc=(0,0,0), rot=(0,0,0)):
    return lathe(label,[(r-w,z),(r,z),(r,z+h),(r-w,z+h)],role,n,loc,rot)


def slab(label, outline, back, front, role, bevel=False):
    n = len(outline)
    v = [(x,y,z) for y in (back,front) for x,z in outline]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label,v,f,role,bevel)


def beam(label, a, b, width, depth, role, bevel=False):
    d = Vector(b)-Vector(a)
    obj = box(label,(0,0,0),(width,depth,d.length),role,bevel=bevel)
    obj.data.transform(Matrix.Translation((Vector(a)+Vector(b))/2) @
                       d.to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def tube(label, points, r, role, n=6):
    v = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-
             Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized(); w = t.cross(u)
        v += [tuple(Vector(p)+r*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w))
              for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    f += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
          for i in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def star(loc, r, role, points=4, rot=(0,0,0)):
    shape = []
    for j in range(points*2):
        a = TAU*j/(points*2)
        radius = r if j%2==0 else r*.29
        shape.append((radius*math.sin(a),radius*math.cos(a)))
    return place(slab('Raised cosmic star',shape,-.025,.025,role),loc,rot)


def shard(loc, width, height, role='mirror', rot=(0,0,0), thickness=.12):
    start = len(PARTS)
    outline = [(-width*.44,-height*.35),(width*.18,-height*.5),
               (width*.52,height*.12),(-width*.08,height*.5)]
    slab('Angular shard dark thickness',outline,-thickness,0,'mirror_back' if role=='mirror' else role)
    slab('Broad shard face',[(x*.91,z*.94) for x,z in outline],0,.025,role)
    beam('Shard light-catching edge',(-width*.40,.035,-height*.30),
         (-width*.08,.035,height*.45),.045,.045,'gold' if role=='mirror' else 'marble')
    group(start,loc,rot)


def mound(label, rx, ry, h, role, loc=(0,0,0), n=14):
    v = [(rx*math.cos(TAU*j/n),ry*math.sin(TAU*j/n),0) for j in range(n)]
    v += [(rx*.83*math.cos(TAU*j/n),ry*.83*math.sin(TAU*j/n),h*.48)
          for j in range(n)]
    v += [(rx*.40*math.cos(TAU*j/n+.13),ry*.40*math.sin(TAU*j/n+.13),
           h*(.87+random.random()*.13)) for j in range(n)]
    v.append((0,0,h))
    f = [tuple(reversed(range(n)))]
    f += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
          for k in range(2) for j in range(n)]
    f += [(2*n+j,2*n+(j+1)%n,3*n) for j in range(n)]
    return place(mesh(label,v,f,role),loc)


def roman(text, loc, height=.25, angle=0):
    start = len(PARTS)
    for k,symbol in enumerate(text):
        x = (k-(len(text)-1)/2)*height*.43
        if symbol=='I':
            box('Raised Roman I',(x,0,0),(.05,.06,height),'dial_mark',bevel=False)
        else:
            for s in (-1,1):
                box('Raised Roman '+symbol,(x+s*height*(.09 if symbol=='V' else 0),0,0),
                    (.05,.06,height),'dial_mark',rot=(0,s*.40,0),bevel=False)
    group(start,loc,(0,angle,0))


def mirror_monolith():
    box('Full marble footing',(0,0,.12),(3,2.4,.24),'marble_dark')
    box('Pale plinth course',(0,0,.32),(2.85,2.25,.20),'marble')
    box('Gold plinth fillet',(0,0,.435),(2.73,2.13,.07),'gold')
    box('Upper carved marble step',(0,0,.535),(2.62,2.02,.13),'marble_dark')
    for y in (-1.14,1.14):
        for x in (-.99,0,.99):
            box('Recessed violet plinth panel',(x,y,.32),(.73,.04,.13),'violet',bevel=False)
            star((x,y+(.03 if y>0 else -.03),.32),.12,'violet')
    for x in (-1.19,1.19):
        star((x,.43,.614),.19,'violet',rot=(math.pi/2,0,0))
    # Tapered four-sided prism with an irregular broken top contour.
    outline = [(-.91,.60),(.91,.60),(.55,3.29),(.21,3.40),(-.08,3.24),(-.54,3.39)]
    body = slab('Four sided broken mirror monolith',outline,-.51,.51,'mirror',True)
    for v in body.data.vertices:
        v.co.y *= 1-.35*(v.co.z-.6)/2.8
    for s in (-1,1):
        for t in (-1,1):
            beam('Heavy tapered gold corner frame',(s*.91,t*.51,.64),
                 (s*.55,t*.335,3.32),.105,.105,'gold',True)
    box('Lower gold mirror socket',(0,0,.72),(1.93,1.12,.20),'gold')
    slab('Oblique mirror fracture facet',[(-.79,.9),(-.07,1.96),(-.42,2.95),(-.59,2.91)],
         .435,.464,'mirror_back')
    # Front cracks follow the taper in depth; no ridges hidden behind the face.
    def front_crack(points,r=.034):
        tube('Impact gold fracture',[(x,.543-.06375*(z-.6),z) for x,z in points],r,'crack_gold')
    centre=(-.12,1.98)
    for chain in [
        [centre,(-.26,2.33),(-.16,2.62),(-.32,2.90),(-.25,3.32)],
        [centre,(.18,2.21),(.24,2.49),(.57,2.67)],
        [centre,(.25,1.82),(.49,1.91),(.74,1.65)],
        [centre,(-.38,1.69),(-.29,1.30),(-.52,1.04),(-.48,.81)],
        [centre,(-.51,2.05),(-.70,2.30)],
        [centre,(.10,1.60),(.03,1.31),(.31,1.10)],
    ]:
        front_crack(chain)
    star((-.12,.49,1.98),.20,'crack_gold',points=5)
    # Broken tip has a real half-metre air gap and an asymmetric rotated crown.
    start=len(PARTS)
    slab('Hovering mirror tip',[(-.51,0),(.51,.06),(.03,.67)],-.31,.31,'mirror',True)
    for a,b in [((-.51,.34,0),(.03,.03,.67)),((.51,.34,.06),(.03,.03,.67))]:
        beam('Broken tip golden edge',a,b,.075,.075,'gold',True)
    beam('Tip exposed golden break',(-.50,.34,0),(.5,.34,.06),.07,.07,'crack_gold')
    group(start,(.12,-.08,3.91),(.07,-.19,.20))
    for i,(x,y,z,w,h) in enumerate([(-1.12,.18,1.30,.34,.65),(1.15,.14,1.90,.43,.82),
          (-.93,-.55,2.65,.35,.69),(.83,-.38,3.40,.31,.57),
          (-.95,.42,3.64,.26,.51),(1.10,.39,.94,.38,.49)]):
        shard((x,y,z),w,h,rot=(.11*(i-2),.2*(-1)**i,.25*(i-2)))
    for x,z in ((-.62,2.6),(.61,1.48),(.35,3.2)):
        star((x,.58-.064*(z-.6),z),.065,'starlight')


def saloon_fragment():
    mound('Continuous solid void rock intrusion',2.3,1.5,.86,'void_stone',n=16)
    for x,y,z,s in [(-1.9,.5,.42,.7),(1.72,.62,.38,.8),(.4,1.10,.31,.7),(-.9,-1,.4,.65)]:
        box('Large violet memory strata',(x,y,z),(s,.55,.35),'violet',rot=(.08,.16,.3))
    start=len(PARTS)
    box('Buried saloon solid wall backing',(-.10,-.22,1.11),(3.54,1.40,1.24),'wood_dark')
    # Two genuinely intersecting wall planes form a crooked front corner.
    for j in range(9):
        x=-1.69+j*.39
        top=2.53+random.uniform(-.06,.10)
        box('Vertical western facade plank',(x,.45,(top+.56)/2),(.36,.16,top-.56),'wood')
        if j%2==0:
            beam('Broad carved plank grain',(x-.08,.546,1.1),(x-.06,.546,2.36),.043,.035,'wood_dark')
    for j in range(6):
        y=-1.0+j*.29
        box('Return wall plank',(-1.91,y,1.50),(.16,.26,1.92),'wood')
    for x in (-1.97,1.77):
        box('Heavy crooked corner post',(x,.47,1.58),(.25,.28,2.24),'wood_dark')
        for z in (.71,2.46):
            box('Post golden collar',(x,.48,z),(.30,.34,.10),'gold')
    for z in (.8,2.5):
        box('Thick facade beam',(-.1,.57,z),(3.88,.22,.15),'wood_dark')
    # Dark recess and swung batwing halves read above the solid intrusion.
    box('Deep door recess',(.13,.56,1.53),(1.46,.09,1.51),'wood_dark',bevel=False)
    for s in (-1,1):
        ds=len(PARTS)
        shape=[(-.33,-.51),(.33,-.51),(.33,.43),(.13,.56),(-.11,.51),(-.33,.36)]
        slab('Saloon swinging batwing door',shape,-.07,.07,'wood',True)
        for x in (-.23,0,.23):
            box('Door carved vertical ribs',(x,.09,-.05),(.045,.05,.73),'wood_dark',bevel=False)
        for z in (-.35,.23):
            box('Door inset cross rail',(0,.12,z),(.61,.07,.08),'gold')
        group(ds,(.13+s*.36,.76,1.48),(0,0,s*.34))
    # Broken roof deck: broad planks make the top legible at the game camera.
    for j in range(9):
        x=-1.8+j*.43
        length=1.63-random.random()*.32
        box('Shattered roof plank',(x,-.27,2.62),(.39,length,.14),'wood',rot=(.04,.025*(j-4),0))
    box('Broken front cornice',(-.19,.65,2.68),(3.97,.25,.19),'wood_dark')
    shape=[(-1.28,2.67),(-1.36,3.00),(-1.16,3.18),(-.54,3.18),(-.36,3.29),
           (.36,3.29),(.54,3.18),(1.16,3.18),(1.36,3.00),(1.28,2.67)]
    slab('Faded western cloth sign',shape,.66,.80,'cloth',True)
    for z in (2.74,3.14):
        box('Sign carved border',(0,.84,z),(2.30,.08,.065),'gold')
    star((0,.89,2.96),.245,'gold',points=5)
    for x in (-.77,.77):
        box('Sign broad relief dash',(x,.865,2.96),(.47,.055,.06),'wood_dark',bevel=False)
    for x,z in [(-1.5,2.30),(.96,1.27)]:
        tube('Gold splitting wooden facade',[(x,.58,z+.23),(x+.13,.58,z),
             (x+.02,.58,z-.22),(x+.19,.58,z-.43)],.035,'crack_gold')
    tube('Gold broken roof seam',[(-1.12,-.75,2.73),(-.8,-.49,2.74),
         (-.96,-.17,2.74),(-.54,.19,2.74)],.034,'crack_gold')
    for j in range(3):
        box('Floating roof splinter',(-1.16+j*.88,-.28,3.20+j*.10),(.34,.94,.12),
            'wood',rot=(.22,-.17+j*.12,.16-j*.20))
        box('Floating plank golden fracture',(-1.16+j*.88,.15,3.24+j*.10),(.32,.07,.07),
            'crack_gold',rot=(0,0,.16-j*.2))
    # Lantern includes an angular glowing body, cage and crooked suspension.
    ls=len(PARTS)
    cylinder('Lantern bottom',(0,0,-.32),.22,.11,'gold',n=6)
    cylinder('Lantern light body',(0,0,0),.17,.52,'lamp_warm',n=6)
    for j in range(4):
        a=TAU*j/4
        beam('Lantern cage',( .19*math.cos(a),.19*math.sin(a),-.28),
             (.19*math.cos(a),.19*math.sin(a),.28),.055,.055,'wood_dark')
    lathe('Lantern pitched cap',[(.06,.44),(.26,.28),(.26,.22),(.06,.26)],'gold',n=8)
    ring('Lantern hanging loop',.13,.045,-.035,.07,'gold',n=12,loc=(0,0,.48),rot=FRONT)
    group(ls,(1.98,.88,1.98),(.13,.22,-.12))
    tube('Crooked lantern hanger',[(1.74,.58,2.72),(2.02,.83,2.69),(1.99,.9,2.5)],.045,'wood_dark')
    group(start,(0,0,.02),(.035,-.095,.085))


def train_fragment():
    mound('Continuous burial undercarriage',2.70,1.30,.56,'void_stone',n=16)
    start=len(PARTS)
    box('Solid locomotive chassis',(0,0,.66),(4.90,1.92,.47),'iron_dark')
    for y in (-1.0,1.0):
        box('Red chassis stripe',(0,y,.79),(4.85,.12,.20),'red_lacquer')
        box('Steel running board',(0,y,.58),(4.99,.22,.10),'steel')
    for x in (-1.73,-.43,1.12):
        for y in (-.99,.99):
            cylinder('Partly buried locomotive wheel',(x,y,.55),.52,.17,'steel',n=20,rot=FRONT,bevel=True)
            cylinder('Recessed dark wheel web',(x,y*1.10,.55),.39,.07,'iron_dark',n=20,rot=FRONT)
            ring('Broad steel wheel tyre',.48,.085,-.03,.07,'steel',n=20,loc=(x,y*1.13,.55),rot=FRONT)
            cylinder('Brass wheel hub',(x,y*1.18,.55),.13,.08,'brass',n=12,rot=FRONT)
            for j in range(6):
                a=TAU*j/6
                beam('Broad wheel spoke',(x+.12*math.sin(a),y*1.15,.55+.12*math.cos(a)),
                     (x+.38*math.sin(a),y*1.15,.55+.38*math.cos(a)),.065,.065,'steel')
    cylinder('Old locomotive boiler',(-.94,0,1.38),.72,2.69,'iron_dark',n=20,rot=(0,math.pi/2,0))
    for x in (-2.22,-1.35,-.48,.35):
        ring('Boiler brass hoop',.744,.06,-.05,.10,'brass',n=20,loc=(x,0,1.38),rot=(0,math.pi/2,0))
    cylinder('Smokebox front steel plate',(-2.31,0,1.38),.65,.13,'steel',n=20,rot=(0,math.pi/2,0),bevel=True)
    cylinder('Front lamp brass casing',(-2.41,.04,1.63),.26,.21,'brass',n=16,rot=(0,math.pi/2,0))
    cylinder('Front lamp mirror lens',(-2.53,.04,1.63),.19,.055,'mirror',n=12,rot=(0,math.pi/2,0))
    lathe('Flared locomotive smokestack',[(.13,1.8),(.22,1.8),(.22,2.36),(.34,2.61),
          (.34,2.72),(.14,2.72)],'iron_dark',n=12,loc=(-1.52,0,0))
    ring('Stack brass lip',.35,.075,2.61,.11,'brass',n=16,loc=(-1.52,0,0))
    cylinder('Steam dome',(-.45,0,2.08),.26,.43,'brass',n=12,bevel=True)
    for y in (-.70,.70):
        box('Boiler red flank stripe',(-.94,y,1.32),(2.48,.08,.23),'red_lacquer')
    # The cab is an actual clipped prism. All rear-edge details follow x=2.2+.28*y.
    def cut_prism(label, x0, y0, y1, z0, z1, role):
        poly=[(x0,y0),(2.20+.28*y0,y0),(2.20+.28*y1,y1),(x0,y1)]
        verts=[(x,y,z) for z in (z0,z1) for x,y in poly]
        faces=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
        return mesh(label,verts,faces,role,True)
    cut_prism('Diagonal sliced solid cab',.31,-.92,.92,.77,2.27,'iron_dark')
    cut_prism('Red passenger cab belt',.29,-.96,.96,1.00,1.28,'red_lacquer')
    cut_prism('Steel broken cab roof',.15,-1.02,1.02,2.28,2.44,'steel')
    for y in (-.95,.95):
        for x in (.79,1.61):
            cylinder('Round brass window bezel',(x,y,1.81),.305,.10,'brass',n=20,rot=FRONT)
            cylinder('Round mirror passenger window',(x,y*1.06,1.81),.237,.035,'mirror',n=20,rot=FRONT)
            beam('Window pale glint',(x-.12,y*1.09,1.72),(x+.06,y*1.09,1.94),.045,.04,'steel')
        for z in (.90,2.16):
            box('Cab rolled edge molding',(1.13,y*1.07,z),(1.65,.065,.075),'brass')
    # Exposed cross-section is inset into its gold fracture perimeter.
    def cp(y,z,off=.015): return (2.20+.28*y+off,y,z)
    for ya,yb in [(-.89,.89)]:
        for z in (.81,2.25):
            beam('Glowing clean diagonal cut edge',cp(ya,z),cp(yb,z),.09,.09,'crack_gold')
    for y in (-.9,.9):
        beam('Vertical cut face gold edge',cp(y,.8),cp(y,2.26),.085,.085,'crack_gold')
    for y in (-.53,0,.53):
        beam('Exposed cross-section structure',cp(y,.9,.04),cp(y,2.16,.04),.085,.085,'steel')
    beam('Sectioned inner deck',cp(-.83,1.30,.04),cp(.83,1.30,.04),.08,.08,'brass')
    tube('Gold crack along locomotive flank',[(-2.0,.77,1.38),(-1.65,.79,1.45),
         (-1.43,.76,1.28),(-1.1,.78,1.39)],.035,'crack_gold')
    # Shallow pilot grille, deliberately too broad to read as thin wires.
    for j in range(7):
        y=-.84+j*.28
        beam('Locomotive pilot slat',(-2.54,y,.47),(-2.28,y,.87),.09,.09,'steel')
    group(start,(0,0,.035),(.035,-.055,0))
    for i in range(2):
        y=-.52+i*1.08
        pts=[(-1.2,y,2.85),(-.48,y+.08,2.87),(.12,y-.04,2.72),(.85,y+.14,2.94)]
        tube('Floating bent rail head',pts,.085,'steel',n=4)
        tube('Floating bent rail foot',[(x,yy,z-.17) for x,yy,z in pts],.11,'steel',n=4)
        for a,b in zip(pts[:-1],pts[1:]):
            beam('Rail vertical web',(a[0],a[1],a[2]-.09),(b[0],b[1],b[2]-.09),.065,.17,'iron_dark')


def gear_sector(label,r,depth,role,a0,a1,loc=(0,0,0),rot=(0,0,0)):
    # Half an annular gear, closed across both radial fracture faces.
    outer=[]; steps=9
    for j in range(steps):
        for t,rr in ((0,r*.86),(.18,r*.86),(.18,r),(.82,r),(.82,r*.86)):
            a=a0+(a1-a0)*(j+t)/steps
            outer.append((rr*math.cos(a),rr*math.sin(a)))
    outer.append((r*.86*math.cos(a1),r*.86*math.sin(a1)))
    inner=[(r*.61*math.cos(a1-(a1-a0)*j/36),r*.61*math.sin(a1-(a1-a0)*j/36)) for j in range(37)]
    # slab uses X/Z; gear sectors therefore start directly in the front plane.
    obj=slab(label,outer+inner,-depth/2,depth/2,role)
    return place(obj,loc,rot)


def clock_heart():
    cylinder('Solid violet circular dais',(0,0,.23),2.2,.46,'violet_dark',n=48,bevel=True)
    ring('Dais lower golden rim',2.20,.12,.05,.10,'gold',n=48)
    ring('Dais upper golden rim',2.18,.10,.42,.09,'gold',n=48)
    ring('Dais inset celestial circle',1.93,.055,.468,.045,'brass',n=48)
    for j in range(32):
        a=TAU*j/32
        box('Dais raised clock tick',(2.04*math.cos(a),2.04*math.sin(a),.51),
            (.15 if j%4==0 else .09,.06,.045),'gold',rot=(0,0,a),bevel=False)
    mound('Collapsed movement core',1.45,1.17,1.18,'gold_dark',loc=(-.15,-.12,.45))
    # A genuine half dial, jagged along its severed diameter, tilted backward.
    start=len(PARTS)
    r=1.72
    arc=[(r*math.cos(math.pi-j*math.pi/32),r*math.sin(math.pi-j*math.pi/32)) for j in range(33)]
    lower=[(1.45,-.12),(.98,.09),(.61,-.14),(.19,.11),(-.22,-.12),(-.60,.08),(-1.19,-.16)]
    slab('Broken half dial dark core',arc+lower,-.25,.09,'gold_dark')
    slab('Pale half dial face',[(x*.968,z*.965) for x,z in arc+lower],.09,.16,'dial_face')
    outer=[(r*math.cos(a),r*math.sin(a)) for a in [math.pi-j*math.pi/32 for j in range(33)]]
    inner=[((r-.13)*math.cos(a),(r-.13)*math.sin(a)) for a in [j*math.pi/32 for j in range(33)]]
    slab('Half dial gold bezel',outer+inner,.16,.27,'gold')
    for j,text in enumerate(('IX','X','XI','XII','I','II','III')):
        a=-math.pi/2+j*math.pi/6
        roman(text,(1.30*math.sin(a),.21,1.30*math.cos(a)),.25,a)
    for j in range(25):
        a=-math.pi/2+j*math.pi/24
        box('Half dial minute engraving',(1.49*math.sin(a),.20,1.49*math.cos(a)),
            (.045,.055,.14 if j%4==0 else .075),'dial_mark',rot=(0,a,0),bevel=False)
    tube('Cracked porcelain gold seam',[(-.41,.205,1.65),(-.25,.205,1.31),(-.39,.205,1.03),
         (-.10,.205,.68),(-.26,.205,.28)],.035,'crack_gold')
    for x,z in ((-.27,1.31),(-.11,.68)):
        star((x,.25,z),.095,'starlight')
    beam('Broken clock hand',(0,.30,.03),(.68,.30,1.00),.11,.09,'gold')
    cylinder('Dial pivot',(0,.34,.03),.18,.12,'brass',n=16,rot=FRONT)
    group(start,(-.48,-.55,2.08),(.25,-.10,-.045))
    # Lying gear with broad spokes, contrasting dark hub, and a bright axle.
    gs=len(PARTS)
    for a0,a1 in ((-math.pi/2,math.pi/2),(math.pi/2,3*math.pi/2)):
        gear_sector('Lying cast gear',1.02,.23,'gold',a0,a1)
    for j in range(6):
        a=TAU*j/6
        beam('Lying gear broad spoke',(.12*math.cos(a),0,.12*math.sin(a)),
             (.75*math.cos(a),0,.75*math.sin(a)),.13,.16,'brass')
    cylinder('Lying gear hub',(0,0,0),.25,.30,'gold_dark',n=16,rot=FRONT)
    group(gs,(-.92,.85,.74),(math.pi/2,.08,.17))
    # Standing gear halves have open air between the torn radial edges.
    for s in (-1,1):
        hs=len(PARTS)
        a0,a1=(-math.pi/2,math.pi/2) if s>0 else (math.pi/2,3*math.pi/2)
        gear_sector('Drifting broken standing gear',1.08,.23,'brass',a0,a1)
        for a in [a0+(a1-a0)*t for t in (.16,.5,.84)]:
            beam('Standing gear cast spoke',(.12*math.cos(a),0,.12*math.sin(a)),
                 (.78*math.cos(a),0,.78*math.sin(a)),.13,.18,'gold')
        halfhub=[(.25*math.cos(a0+(a1-a0)*j/12),.25*math.sin(a0+(a1-a0)*j/12)) for j in range(13)]
        slab('Split axle hub',halfhub,-.18,.18,'gold_dark')
        for sign in (-1,1):
            tube('Gold torn gear end',[(.02,.15,sign*.66),(.07,.15,sign*.78),(.02,.15,sign*.94)],.035,'crack_gold')
        group(hs,(.70+s*.14,.42,1.89+(s+1)*.055),(.02,s*.07,0))
    for x,z in ((.70,.97),(.72,2.75),(.71,1.95)):
        star((x,.64,z),.11,'starlight')
    # A detached bob floats beside its broken rod, proud of the base.
    cylinder('Hovering pendulum bob',(1.43,1.08,1.06),.45,.18,'gold_dark',n=24,rot=FRONT,bevel=True)
    ring('Bob concentric gold inlay',.34,.065,-.04,.08,'gold',n=24,loc=(1.43,1.20,1.06),rot=FRONT)
    star((1.43,1.27,1.06),.19,'gold')
    beam('Broken pendulum upper stem',(1.43,1.08,1.76),(1.43,1.08,2.34),.075,.075,'gold_dark')
    star((1.43,1.08,1.58),.085,'starlight')
    tube('Dais gold fault',[(-1.6,.4,.53),(-1.17,.2,.54),(-.81,.4,.54),(-.33,.2,.54)],.035,'crack_gold')


def glass_section(label,profile,a0,a1,n=12):
    # Thick closed low-poly shell section. Missing front panes expose the sand
    # under the required opaque grey export shader; their remaining triangular
    # glass corners preserve the outline rather than hiding the hourglass core.
    cross=profile+[(max(.045,r-.055),z) for r,z in reversed(profile)]
    m=len(cross)
    v=[(r*math.cos(a0+(a1-a0)*j/n),r*math.sin(a0+(a1-a0)*j/n),z)
       for j in range(n+1) for r,z in cross]
    f=[tuple(reversed(range(m))),tuple(range(n*m,(n+1)*m))]
    f += [(j*m+k,(j+1)*m+k,(j+1)*m+(k+1)%m,j*m+(k+1)%m)
          for j in range(n) for k in range(m)]
    return mesh(label,v,f,'crystal')


def hourglass():
    box('Solid square marble foundation',(0,0,.13),(2.8,2.8,.26),'marble')
    box('Violet plinth inset',(0,0,.28),(2.59,2.59,.10),'violet')
    for z,h in ((.43,.22),(4.10,.22)):
        if z<1:
            cylinder('Massive gold hourglass bottom plate',(0,0,z),1.35,h,'gold',n=32,bevel=True)
        else:
            # An oculus in the carved top plate exposes the upper glass bulb
            # at 55 degrees; a solid disk would hide its whole upper silhouette.
            ring('Gold hourglass top plate with oculus',1.35,.29,z-h/2,h,'gold',n=32)
        ring('Plate violet inlay',1.29,.13,z+h/2,.045,'violet',n=32)
        ring('Plate golden outer molding',1.36,.08,z-h/2,.07,'gold_dark',n=32)
    cylinder('Top inset medallion',(0,0,4.245),.33,.08,'violet',n=16)
    star((0,0,4.30),.29,'gold',points=8,rot=(math.pi/2,0,0))
    for a in (0,math.pi/2,math.pi,3*math.pi/2):
        beam('Top plate carved radial bridge',(.30*math.cos(a),.30*math.sin(a),4.15),
             (1.12*math.cos(a),1.12*math.sin(a),4.15),.10,.08,'gold')
    # Four turned columns, with collars, capitals and broad fluted shafts.
    for x in (-.88,.88):
        for y in (-.88,.88):
            profile=[(.10,.53),(.19,.53),(.19,.69),(.12,.80),(.115,1.04),
                     (.16,1.15),(.12,1.30),(.105,3.26),(.16,3.43),(.12,3.55),
                     (.19,3.76),(.19,3.97),(.10,3.97)]
            lathe('Turned gold dark hourglass post',profile,'gold_dark',n=12,loc=(x,y,0))
            for z in (.68,1.16,3.43,3.86):
                ring('Column golden collar',.19,.065,z,.085,'gold',n=12,loc=(x,y,0))
            beam('Column violet flute',(x,y+.112,1.45),(x,y+.112,3.10),.045,.045,'violet')
    lower=[(.70,.64),(.88,.89),(.84,1.19),(.62,1.58),(.26,1.94),(.11,2.17)]
    upper=[(.11,2.23),(.26,2.46),(.61,2.85),(.84,3.23),(.88,3.56),(.70,3.87)]
    # Front viewing fracture spans 100 degrees around +Y. Shells remain closed.
    for label,profile in [('Lower faceted glass bulb',lower),('Upper cracked glass bulb',upper)]:
        glass_section(label,profile,math.radians(140),math.radians(400),n=13)
        for a in (math.radians(40),math.radians(140)):
            tube('Broad crystal window boundary',[(r*math.cos(a),r*math.sin(a),z) for r,z in profile],.045,'crystal')
    # Sand is violet by chapter palette; the sand role remains distinct in game.
    mound('Violet sand pile inside lower bulb',.69,.68,.78,'sand',loc=(0,0,.68),n=16)
    cylinder('Sand at base of bulb',(0,0,.72),.64,.11,'sand',n=16)
    tube('Frozen violet sand stream',[(0,.08,2.72),(.025,.08,2.38),(-.02,.08,2.12),
         (.02,.08,1.86),(0,.08,1.47)],.059,'sand',n=8)
    # Retained triangular front panes frame the open, shattered viewing facet.
    slab('Lower bulb retained glass facet',[(-.56,.85),(-.18,.67),(-.72,1.14)],.48,.54,'crystal')
    slab('Upper bulb fractured front pane',[(-.70,3.48),(-.10,3.80),(-.15,2.40)],.43,.49,'crystal')
    slab('Upper bulb opposite glass facet',[(.58,3.61),(.77,3.22),(.12,2.39)],.39,.45,'crystal')
    tube('Upper bulb golden fracture',[(-.72,.48,3.46),(-.36,.58,3.33),(-.20,.53,3.08),
         (.07,.51,3.18),(.31,.48,3.02),(.69,.44,3.14)],.034,'crack_gold')
    for i,(x,y,z) in enumerate([(-.31,.96,3.48),(.33,1.10,3.38),(.64,.90,2.92)]):
        shard((x,y,z),.27,.40,'crystal',rot=(.2,-.25+i*.25,.15-i*.21),thickness=.065)
    for a in (0,math.pi/2,math.pi,3*math.pi/2):
        star((1.12*math.cos(a),1.12*math.sin(a),.57),.14,'violet',rot=(math.pi/2,0,a))
    tube('Cracked marble base',[(-1.25,.9,.275),(-.96,.65,.275),(-1.08,.36,.275),(-.74,.06,.275)],.032,'crack_gold')


def desert_shard():
    mound('Full solid sand and void foundation',2.0,1.5,.67,'sand',n=16)
    mound('Exposed dark intrusion',1.55,.95,.74,'void_stone',loc=(.08,-.19,.02),n=12)
    # A thick, tilted arch with an intact left pier and a broken right springer.
    start=len(PARTS)
    slab('Left sandstone arch pier',[(-1.72,.35),(-.81,.35),(-.84,1.44),(-1.62,1.48)],-.46,.38,'sand',True)
    for j in range(7):
        a0=math.radians(12+j*24); a1=a0+math.radians(23.1)
        out=[(-.23+1.48*math.cos(a0+(a1-a0)*k/3),1.27+1.48*math.sin(a0+(a1-a0)*k/3)) for k in range(4)]
        inn=[(-.23+.82*math.cos(a1-(a1-a0)*k/3),1.27+.82*math.sin(a1-(a1-a0)*k/3)) for k in range(4)]
        slab('Carved sandstone arch voussoir',out+inn,-.46,.38,'sand',True)
        # Dark inset relief follows the arch, breaking the broad sandstone face
        # into a carved outer molding, recessed field and gold radial accents.
        inset_out=[(-.23+1.32*math.cos(a0+.05+(a1-a0-.10)*k/3),
                    1.27+1.32*math.sin(a0+.05+(a1-a0-.10)*k/3)) for k in range(4)]
        inset_in=[(-.23+.97*math.cos(a1-.05-(a1-a0-.10)*k/3),
                   1.27+.97*math.sin(a1-.05-(a1-a0-.10)*k/3)) for k in range(4)]
        slab('Arch dark recessed voussoir field',inset_out+inset_in,.383,.412,'violet_dark')
        if j in (0,3):
            beam('Arch inlaid gold radial seam',(-.23+.85*math.cos(a0),.435,1.27+.85*math.sin(a0)),
                 (-.23+1.44*math.cos(a0),.405,1.27+1.44*math.sin(a0)),.05,.05,'crack_gold')
    slab('Right broken arch stump',[(.67,.44),(1.44,.46),(1.39,1.11),(1.19,1.20),(.98,1.07),(.72,1.24)],
         -.46,.38,'sand',True)
    slab('Exposed void stone broken springer',[(.68,1.33),(1.18,1.51),(1.19,1.20),(.98,1.07),(.72,1.24)],
         -.48,.40,'void_stone')
    for z in (.72,1.12):
        box('Pier recessed carved band',(-1.28,.405,z),(.73,.055,.065),'violet_dark',bevel=False)
    box('Pier inset void stone relief',(-1.28,.414,.94),(.51,.055,.24),'void_stone')
    tube('Sandstone golden split',[(-1.53,.43,1.47),(-1.27,.43,1.75),(-1.38,.43,2.02),
         (-1.10,.43,2.27),(-.98,.43,2.49)],.036,'crack_gold')
    group(start,(-.03,-.25,.05),(.14,-.11,-.035))
    # Large mirror blade has a substantial dark core and a broad gold border.
    blade=[(-.37,.44),(.31,.57),(1.40,3.42),(.72,3.09)]
    slab('Impaled mirror blade gold edge',blade,-.08,.13,'gold',True)
    cx=sum(x for x,z in blade)/4; cz=sum(z for x,z in blade)/4
    inner=[(cx+(x-cx)*.87,cz+(z-cz)*.94) for x,z in blade]
    slab('Huge embedded mirror face',inner,.13,.18,'mirror')
    slab('Mirror blade dark back',inner,-.14,-.08,'void_stone')
    beam('Broad mirror glint',(.33,.205,1.53),(1.06,.205,3.04),.06,.045,'crystal')
    tube('Blade gold fault',[(.47,.225,2.0),(.72,.225,2.12),(.65,.225,2.33),(.88,.225,2.51)],.032,'crack_gold')
    mound('Small foreground sand dune',1.02,.62,.45,'sand',loc=(.03,.76,.16),n=14)
    for x,y,s in [(-1.50,-.49,.61),(1.44,-.28,.63),(-.86,.38,.58),(.67,-.80,.74)]:
        box('Exposed void strata in sand',(x,y,.39),(s,.63,.43),'void_stone',rot=(.10,.18,x*.22))
    for x,y,z,s in [(-1.40,.69,.42,.64),(-1.03,1.02,.35,.48),(-1.65,.18,.39,.40),
                       (1.36,.77,.34,.51),(1.63,.27,.36,.37)]:
        # Closed, pointed hexagonal crystal prism, deliberately chunky.
        profile=[(.02,0),(s*.27,0),(s*.31,s*.59),(.035,s*1.5),(.02,s*1.45)]
        lathe('Foot crystal cluster',profile,'crystal',n=6,loc=(x,y,z),rot=(.1,-.25,x*.12))
    for i,(x,y,z) in enumerate([(-1.35,-.36,2.92),(-.64,-.54,3.12),(.0,-.46,2.96)]):
        box('Floating sandstone arch chunk',(x,y,z),(.42,.50,.30),'sand',rot=(.16,.3,i*.36))
        box('Floating core exposure',(x,y+.26,z),(.32,.045,.16),'void_stone',rot=(0,.3,i*.36))
    for x,y in [(-.66,.96),(.63,.89)]:
        star((x,y,.51),.16,'gold',rot=(math.pi/2,0,0))


SPECS = [
    ('LM_MirrorMonolith',(3.0,2.4,4.6),mirror_monolith,8),
    ('LM_SaloonFragment',(4.6,3.0,3.6),saloon_fragment,8),
    ('LM_TrainFragment',(5.4,2.6,3.0),train_fragment,7),
    ('LM_ClockHeart',(4.4,4.4,4.0),clock_heart,8),
    ('LM_Hourglass',(2.8,2.8,4.4),hourglass,7),
    ('LM_DesertShard',(4.0,3.0,3.4),desert_shard,7),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def finish(name,dims,count):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True); bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name; obj.data.name=name+'_Mesh'
    if triangles(obj)>8000:
        mod=obj.modifiers.new('Retain role boundaries within 8000 triangles','DECIMATE')
        mod.ratio=7850/triangles(obj); mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj)<=8000,(name,triangles(obj))
    low,high=bounds(obj); span=high-low; centre=(low+high)/2
    # Fit exact specified outer extents, baking the modest corrections into data.
    for v in obj.data.vertices:
        v.co.x=(v.co.x-centre.x)*dims[0]/span.x
        v.co.y=(v.co.y-centre.y)*dims[1]/span.y
        v.co.z=(v.co.z-low.z)*dims[2]/span.z
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name
    bm.to_mesh(obj.data); bm.free()
    for face in obj.data.polygons: face.use_smooth=False
    obj.data.update(); bpy.context.view_layer.update()
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)==count and 6<=count<=8,(name,used)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    low,high=bounds(obj)
    assert abs(low.z)<1e-5 and abs(high.z-dims[2])<1e-4,(name,low,high)
    assert abs(low.x+high.x)<1e-4 and abs(low.y+high.y)<1e-4
    assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in range(3))
    assert obj.location.length<1e-8 and tuple(obj.scale)==(1,1,1)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['chapter']='The Collapsing End of Time'; obj['front']='+Y'
    obj['triangle_count']=triangles(obj); obj['solid_combat_blocker']=True
    obj['origin']='Bottom centre'; obj['dimensions_m']=list(dims)
    path=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert path.is_file() and path.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
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
    shade.curvature_ridge_factor=1.45; shade.curvature_valley_factor=1.25
    shade.cavity_ridge_factor=1.3; shade.cavity_valley_factor=1.15
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review background')
    scene.world.color=(.023,.016,.045)
    helpers=[]
    az=math.radians(16); el=math.radians(55)
    near=Vector((math.sin(az),math.cos(az),0))
    right=Vector((-math.cos(az),math.sin(az),0))
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
    camera.location=near*40*math.cos(el)+Vector((0,0,40*math.sin(el)))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'; inv=camera.rotation_euler.to_matrix().transposed()

    def points(obj):
        return [inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]

    def label(body,x,y,size,role='marble'):
        data=bpy.data.curves.new('Review caption','FONT')
        data.body=body; data.size=size; data.align_x='CENTER'
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('Review caption',data)
        scene.collection.objects.link(obj); helpers.append(obj)
        obj.rotation_euler=camera.rotation_euler
        obj.location=camera.rotation_euler.to_matrix() @ Vector((x,y,8))

    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        # Pack the projected widths, retaining real scale and a generous air gap.
        cursor=0
        for obj in objects:
            bpy.context.view_layer.update()
            pp=points(obj); lo=min(p.x for p in pp); hi=max(p.x for p in pp)
            obj.location=right*(cursor-lo)
            cursor+=hi-lo+.78
        bpy.context.view_layer.update()
        cloud=[p for obj in objects for p in points(obj)]
        x0=min(p.x for p in cloud); x1=max(p.x for p in cloud)
        y0=min(p.y for p in cloud); y1=max(p.y for p in cloud)
        baseline=y0-.57
        for obj,(_,dims,_,_) in zip(objects,SPECS):
            pp=points(obj); x=(min(p.x for p in pp)+max(p.x for p in pp))/2
            label(obj.name,x,baseline,.235)
            label(f'{dims[0]:.1f} x {dims[1]:.1f} x {dims[2]:.1f} m',x,baseline-.36,.19,'gold')
            label(f'{triangles(obj):,} tris',x,baseline-.66,.17,'mirror')
        mid=(x0+x1)/2
        label('V  /  THE COLLAPSING END OF TIME',mid,y1+1.65,.49)
        label('COMBAT LANDMARKS     /     FRACTURED MEMORIES     /     SOLID COVER',mid,y1+1.02,.21,'gold')
        label('1 : 1 METRES     /     +Y FRONT     /     55 DEG ELEVATION',mid,baseline-1.36,.18,'mirror')
        bpy.context.view_layer.update()
        cloud += [inv @ (o.matrix_world @ Vector(c)) for o in helpers if o.type=='FONT' for c in o.bound_box]
        x0=min(p.x for p in cloud); x1=max(p.x for p in cloud)
        y0=min(p.y for p in cloud); y1=max(p.y for p in cloud)
        camera.location+=camera.rotation_euler.to_matrix() @ Vector(((x0+x1)/2,(y0+y1)/2,0))
        camera.data.ortho_scale=max(x1-x0,(y1-y0)*1.8)*1.065
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
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
    for i,(name,dims,build,count) in enumerate(SPECS):
        PARTS=[]; random.seed(55071+i*7919)
        build(); objects.append(finish(name,dims,count))
    assert len(scene.objects)==6 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==6 and all(o.type=='MESH' for o in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
