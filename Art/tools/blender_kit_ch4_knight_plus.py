# 4장 정예 태엽 기사: 뼈 부착 장갑 6종과 강화 진자 낫, FBX 및 착용 미리보기.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_knight_plus.py
"""Blender 5.x, metres, +Y front / +X right / +Z up.

Armor vertices are bone-local; weapon vertices are grip-local.  No bounds
recentering. All seven neutral-grey FBXs are exported before preview-only
instances, mannequins, captions and viewport colors are introduced.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_knight_plus_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
PARTS, MATS = [], {}
# Brass/copper remain desaturated in this noir review; shader colors are grey.
PALETTE = {
    'iron_dark': (.025, .032, .043), 'iron': (.13, .15, .18),
    'steel': (.32, .37, .43), 'steel_dark': (.18, .22, .27),
    'silver': (.86, .89, .93), 'brass': (.61, .60, .55),
    'brass_dark': (.21, .20, .17), 'copper': (.37, .32, .29),
    'dial_mark': (.009, .012, .018), 'gem_red': (.92, .025, .045),
    'red_lacquer': (.50, .015, .030), 'red_dark': (.19, .008, .015),
    'ribbon_red': (.72, .025, .04),
}
ATTACH = {
    'Pauldron_R': (.27, 0, 1.42), 'Pauldron_L': (-.27, 0, 1.42),
    'BackPendulum': (0, 0, 1.42), 'ChestPendulum': (0, 0, 1.42),
    'Tasset': (0, 0, 1.06), 'HelmCrest': (0, 0, 1.54),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Chunky edge chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(35)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), bevel=False):
    verts = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
             for z in (-depth/2, depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def solid(label, outline, depth, role, y=0, bevel=True):
    """Closed extrusion of a possibly concave XZ silhouette."""
    n = len(outline)
    verts = [(x, yy, z) for yy in (y-depth/2, y+depth/2) for x,z in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label, verts, faces, role, bevel)


def contour(label, outer, inner, depth, role, y=0, closed=True, bevel=False):
    """Annulus or open arc: actual open windows, closed thickness and end caps."""
    n = len(outer)
    assert n == len(inner)
    verts = [(x,yy,z) for yy,curve in ((y-depth/2,outer),(y+depth/2,outer),
              (y+depth/2,inner),(y-depth/2,inner)) for x,z in curve]
    faces = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
             for k in range(4) for j in range(n if closed else n-1)]
    if not closed:
        faces += [(0,n,2*n,3*n),(n-1,4*n-1,3*n-1,2*n-1)]
    return mesh(label, verts, faces, role, bevel)


def ring(label, loc, radius, width, depth, role, n=24):
    x,y,z = loc
    outer = [(x+radius*math.sin(TAU*j/n),z+radius*math.cos(TAU*j/n)) for j in range(n)]
    inner = [(x+(radius-width)*math.sin(TAU*j/n),z+(radius-width)*math.cos(TAU*j/n)) for j in range(n)]
    return contour(label, outer, inner, depth, role, y)


def gear(label, loc, radius, hole, depth, role='brass', teeth=10, arc=False):
    x,y,z = loc
    count = teeth*4
    angles = [(-.75*math.pi + 1.5*math.pi*j/count) if arc else TAU*j/count
              for j in range(count+1 if arc else count)]
    outer,inner = [],[]
    for j,a in enumerate(angles):
        r = radius if j%4 in (1,2) else radius-min(.04,radius*.30)
        outer.append((x+r*math.sin(a), z+r*math.cos(a)))
        inner.append((x+hole*math.sin(a), z+hole*math.cos(a)))
    return contour(label, outer, inner, depth, role, y, closed=not arc)


def tube(label, points, radius, role, n=6):
    verts = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized()
        w = t.cross(u)
        verts += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    return mesh(label, verts, faces, role)


def pin(loc, radius=.023, role='brass', rot=FRONT):
    return cylinder('Broad cold rivet', loc, radius, .032, role, 8, rot)


def diamond(loc, width, height, role, depth=.034):
    x,y,z = loc
    return solid('Lozenge inlay',[(x,z+height/2),(x+width/2,z),(x,z-height/2),(x-width/2,z)],
                 depth,role,y,bevel=False)


def lens(loc, diameter, role='copper'):
    """Low-poly lenticular bob, .04+ thick; silhouette stays round."""
    x,y,z = loc
    r = diameter/2
    n = 16
    profiles = [(r*.45,-.047),(r*.94,-.028),(r,0),(r*.94,.028),(r*.45,.047)]
    verts = [(x+rr*math.sin(TAU*j/n), y+yy, z+rr*math.cos(TAU*j/n)) for rr,yy in profiles for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range(4*n,5*n))]
    faces += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j) for k in range(4) for j in range(n)]
    mesh('Hammered pendulum lens',verts,faces,role)
    ring('Heavy bob bezel',(x,y,z),r+.015,.035,.060,'brass',n=16)


def spoked_gear(loc, radius, teeth=10):
    x,y,z = loc
    hole=max(.042,radius-.08)
    hub=.058 if radius>.1 else .026
    spoke=.024 if radius>.1 else .015
    gear('Open spoked shoulder gear',loc,radius,hole,.055,teeth=teeth)
    for a in (0,math.pi/2,math.pi,3*math.pi/2):
        tube('Cast gear spoke',[(x+hub*.65*math.sin(a),y,z+hub*.65*math.cos(a)),
                              (x+(hole+.015)*math.sin(a),y,z+(hole+.015)*math.cos(a))],spoke,'brass_dark')
    cylinder('Dark gear hub',loc,hub,.085,'brass_dark',12,FRONT,True)
    pin((x,y+.06,z),min(.026,hub*.8),'silver')
    pin((x,y-.06,z),min(.026,hub*.8),'iron')


def surface(label, rows, thickness, role):
    """Closed curved plate with a constant vertical thickness."""
    m,n = len(rows),len(rows[0])
    top = [p for row in rows for p in row]
    verts = top + [(x,y,z-thickness) for x,y,z in top]
    faces = []
    for k in range(m-1):
        for j in range(n-1):
            a=k*n+j; b=a+n
            faces += [(a,a+1,b+1,b),(a+m*n,b+m*n,b+1+m*n,a+1+m*n)]
    edge = list(range(n))+[k*n+n-1 for k in range(1,m)]+list(range((m-1)*n+n-2,(m-1)*n-1,-1))+[k*n for k in range(m-2,0,-1)]
    faces += [(a,edge[(j+1)%len(edge)],edge[(j+1)%len(edge)]+m*n,a+m*n) for j,a in enumerate(edge)]
    return mesh(label,verts,faces,role,True)


def pauldron():
    # A domed saddle rather than a floating sphere: the attachment is below it.
    rows = []
    for k in range(7):
        t=-.97+1.94*k/6; span=math.sqrt(1-t*t)
        x=.17+.24*t
        rows.append([(x,.26*span*math.sin(-math.pi/2+math.pi*j/8),
                      .085+.255*span*math.cos(-math.pi/2+math.pi*j/8)) for j in range(9)])
    surface('Dark saddle under dome',rows,.075,'iron_dark')
    # Crown plate leaves a heavy black border all around the rolled silver.
    rows = []
    for k in range(7):
        t=-.94+1.88*k/6; span=math.sqrt(1-t*t)
        x=.17+.23*t
        rows.append([(x,.247*span*math.sin(-math.pi/2+math.pi*j/8),
                      .108+.245*span*math.cos(-math.pi/2+math.pi*j/8)) for j in range(9)])
    surface('Hammered silver dome',rows,.043,'silver')
    # Three articulated lames roll over the outside of the upper arm.
    for k in range(3):
        x=.27+.064*k; z=.17-.102*k; half=.245-.018*k
        rows = [[(x+dx,half*math.sin(a),z+.060*math.cos(a)-dx*.7)
                 for a in [-math.pi/2+math.pi*j/6 for j in range(7)]] for dx in (-.075,.065)]
        surface('Black overlap between lames',rows,.070,'iron_dark')
        rows = [[(x+dx,(half-.012)*math.sin(a),z+.145*math.cos(a)-dx*.7+.012)
                 for a in [-math.pi/2+math.pi*j/6 for j in range(7)]] for dx in (-.054,.057)]
        surface('Silver overlapping shoulder lame',rows,.040,'silver')
        for j in range(5):
            a=-1.25+2.5*j/4
            pin((x+.04,(half-.012)*math.sin(a),z+.145*math.cos(a)-.012),.020,'brass',rot=(0,0,0))
    # Raised top spiral follows the dome's X curvature.
    pts=[]
    for j in range(24):
        a=TAU*1.35*j/23; r=.020+.105*j/23
        x=.16+r*math.cos(a); y=r*math.sin(a)
        z=.108+.245*math.sqrt(max(0,1-((x-.17)/.23)**2-(y/.247)**2))+.024
        pts.append((x,y,z))
    tube('Raised clock spring swirl',pts,.020,'steel')
    for x in (.00,.10,.20,.30):
        t=(x-.17)/.23
        y=.247*math.sqrt(1-t*t)*.92
        z=.108+.245*math.sqrt((1-t*t)*(1-.92**2))
        pin((x,y+.016,z),.022,'brass')
    # Three jagged, square-forged fangs point OUT and down, not into the head.
    for j,y in enumerate((-.16,0,.16)):
        start=Vector((.425,y,-.055)); end=start+Vector((.125+(.025 if j==1 else 0),0,-.16))
        direction=end-start
        verts=[tuple(start+Vector((0,dy,dz))) for dy,dz in ((-.05,-.025),(.05,-.025),(.045,.05),(-.045,.05))]+[tuple(end)]
        mesh('Jagged lower shoulder fang',verts,[(0,3,2,1),(0,1,4),(1,2,4),(2,3,4),(3,0,4)],'silver',True)
        box('Fang iron socket',start,(.065,.12,.075),'iron')
    spoked_gear((.14,-.22,.415),.21)
    tube('Rear gear mount',[(.12,-.18,.08),(.14,-.22,.36)],.037,'iron')
    solid('Torn front ribbon',[(.18,.095),(.28,.08),(.30,-.12),(.27,-.155),(.24,-.11),(.21,-.17),(.18,-.13)],
          .035,'ribbon_red',y=.25,bevel=False)
    box('Ribbon clamp',(.23,.273,.07),(.13,.04,.055),'brass_dark')
    pin((.23,.302,.07),.023,'brass')
    for x,z in ((.05,.19),(.34,.02)):
        solid('Inset rust scar',[(x-.035,z+.035),(x+.04,z+.02),(x+.02,z-.025),(x-.027,z-.02)],
              .031,'copper',y=.224,bevel=False)


def back_pendulum():
    box('Riveted rear mounting plate',(0,-.35,.025),(.35,.06,.35),'iron_dark')
    box('Bracket inset',(0,-.39,.025),(.23,.035,.23),'steel')
    for x in (-.13,.13):
        for z in (-.10,.15): pin((x,-.399,z),.026)
    # Both braces meet the lower arc; the central wind-up-key corridor is open.
    for a,b in (((-.10,-.40,.12),(-.075,-.40,.34)),((.13,-.40,.11),(.61,-.40,.35))):
        tube('Heavy rear arc brace',[a,b],.040,'iron')
    gear('Three quarter toothed halo',(.30,-.40,.70),.55,.47,.065,'brass',18,arc=True)
    angles=[-.75*math.pi+1.5*math.pi*j/36 for j in range(37)]
    contour('Dark inset halo molding',[(.3+.486*math.sin(a),.7+.486*math.cos(a)) for a in angles],
            [(.3+.455*math.sin(a),.7+.455*math.cos(a)) for a in angles],.036,'brass_dark',-.443,False)
    for a in (-2.1,-1.4,-.7,0,.7,1.4,2.1):
        x=.3+.485*math.sin(a); z=.7+.485*math.cos(a)
        pin((x,-.446,z),.021,'silver')
    tube('Top iron hanger hook',[(.30,-.40,1.20),(.30,-.40,1.25),(.39,-.40,1.24),
                                  (.40,-.40,1.13),(.33,-.40,1.10)],.023,'iron_dark')
    tube('Short free pendulum rod',[(.33,-.40,1.10),(.33,-.40,.85)],.025,'steel')
    lens((.33,-.40,.75),.26)
    for y in (-.465,-.335):
        diamond((.33,y,.75),.12,.17,'dial_mark')
        diamond((.33,y+(-.020 if y<-.4 else .020),.75),.06,.10,'gem_red')
    box('Old rust patch on bracket',(.035,-.413,.02),(.08,.032,.065),'copper',bevel=False)


def chest_pendulum():
    # X/Y bow gives the shield a real convex cross section over the torso.
    outline=[(-.25,.12),(-.17,.18),(.17,.18),(.25,.12),(.235,-.19),(.13,-.27),(-.13,-.27),(-.235,-.19)]
    plate=solid('Riveted dark breast shield',outline,.05,'iron_dark',y=.325)
    for v in plate.data.vertices: v.co.y-=.055*(abs(v.co.x)/.25)**2
    # Bright shoulder trim and bottom chevron break up the dark shield.
    tube('Lower silver shield welt',[(-.23,.30,-.18),(-.13,.34,-.27),(0,.36,-.29),(.13,.34,-.27),(.23,.30,-.18)],.020,'steel')
    for s in (-1,1):
        spoked_gear((s*.255,.315,-.03),.08,8)
        for z in (.10,-.13): pin((s*.207,.328,z),.021)
        # A thick curling crest with turned-in ends.
        pts=[(s*x,.390,z) for x,z in ((0,.10),(.07,.16),(.145,.16),(.177,.11),(.153,.075),(.115,.09))]
        tube('Silver scroll crest',pts,.025,'silver')
        solid('Chest rust inset',[(s*.17,-.05),(s*.215,-.08),(s*.19,-.15),(s*.15,-.12)],.032,'copper',y=.342,bevel=False)
    diamond((0,.375,.10),.11,.14,'silver',.045)
    pin((0,.426,.10),.044,'brass')
    tube('Chest pendulum rod',[(0,.40,.075),(0,.40,-.225)],.021,'steel')
    lens((0,.40,-.245),.18)
    diamond((0,.466,-.245),.07,.095,'dial_mark')
    pin((0,.489,-.245),.018,'silver')


def tasset():
    outer=[(-.15,-.04),(.15,-.04),(.145,-.28),(.085,-.48),(0,-.60),(-.085,-.48),(-.145,-.28)]
    inner=[(x*.70,-.09+(z+.04)*.83) for x,z in outer]
    solid('Dark kite field',outer,.055,'iron_dark',y=.335)
    contour('Rolled silver kite border',outer,inner,.040,'silver',y=.37,bevel=True)
    # Black diamond sits in a larger silver escutcheon, so it reads clearly.
    diamond((0,.377,-.405),.14,.20,'steel')
    diamond((0,.399,-.405),.085,.13,'dial_mark')
    diamond((0,.420,-.405),.035,.065,'brass')
    for s in (-1,1):
        for x,z in ((.112,-.11),(.108,-.25),(.055,-.46)):
            pin((s*x,.402,z),.020)
    for s in (-1,1):
        begin=len(PARTS)
        # Local XZ side apron then rotate outward twenty degrees about its belt pin.
        solid('Torn red side skirt',[(-.12,-.06),(.12,-.06),(.12,-.41),(.07,-.38),
              (.035,-.45),(-.015,-.405),(-.06,-.46),(-.12,-.40)],.035,'red_lacquer',y=.295,bevel=False)
        side=[(-.11,-.065),(.11,-.065),(.105,-.35),(.075,-.405),(-.075,-.405),(-.105,-.35)]
        solid('Side tasset under plate',side,.055,'iron_dark',y=.33)
        solid('Side tasset steel plate',[(x*.85,z+.018) for x,z in side],.04,'steel',y=.37)
        tube('Side apron silver shoulder',[(-.087,.403,-.105),(0,.403,-.08),(.087,.403,-.105)],.018,'silver')
        for x in (-.067,0,.067):
            box('Square gear hem tooth',(x,.365,-.404),(.041,.065,.050),'brass_dark',bevel=False)
        for x in (-.072,.072): pin((x,.41,-.15),.022)
        box('Rust inset on side apron',(.018,.404,-.275),(.070,.031,.070),'copper',bevel=False)
        for obj in PARTS[begin:]:
            place(obj,(s*.25,0,0),(0,-s*math.radians(20),0))
    box('Heavy belt buckle',(0,.36,.005),(.18,.08,.12),'iron')
    buckle=[(-.115,.08),(.115,.08),(.115,-.07),(-.115,-.07)]
    aperture=[(-.075,.043),(.075,.043),(.075,-.033),(-.075,-.033)]
    contour('Raised brass buckle frame',buckle,aperture,.045,'brass',y=.425,bevel=True)
    box('Buckle tongue',(0,.446,.005),(.04,.032,.11),'silver',bevel=False)


def helm_crest():
    # Front crown leaves the existing plume's central top/back corridor free.
    solid('Arrowhead visor crown',[(-.06,.55),(.06,.55),(.065,.67),(.045,.73),(0,.90),(-.045,.73),(-.065,.67)],
          .065,'silver',y=.25)
    solid('Dark crown foot',[(-.105,.43),(.105,.43),(.075,.58),(-.075,.58)],.07,'iron',y=.25)
    diamond((0,.297,.687),.065,.18,'dial_mark')
    diamond((0,.321,.58),.070,.09,'gem_red',.04)
    # Swept temple blades run back (-Y) and up, away from the face/plume.
    for s in (-1,1):
        blade=solid('Swept steel temple horn',[(-.05,0),(.05,0),(.075,.12),(.025,.265),(-.025,.18)],
                    .065,'steel',y=0)
        # Build blade in YZ with a small outward flare in X.
        for v in blade.data.vertices:
            u,yy,zz=v.co
            v.co=(s*(.28+zz*.20+yy),-.03-zz*.65+u,.45+zz*.75)
        tube('Bright temple blade spine',[(s*.30,-.02,.46),(s*.33,-.11,.58),(s*.33,-.20,.65)],.018,'silver')
        pin((s*.28,.053,.45),.032,'brass')
        diamond((s*.28,.074,.45),.04,.05,'copper')
    # Front half only, in XY, at the helmet brow's actual height.
    pts=[(.33*math.sin(-math.pi/2+math.pi*j/20),.33*math.cos(-math.pi/2+math.pi*j/20),.40) for j in range(21)]
    tube('Front half brass brow band',pts,.025,'brass',n=6)
    for j in range(7):
        a=-1.4+2.8*j/6
        pin((.34*math.sin(a),.34*math.cos(a),.405),.021,'iron_dark',rot=(math.pi/2,0,-a))


def weapon():
    # Disc outer edge 2.05 / butt -.55 = exactly 2.60 m.
    cylinder('Dark iron pole',(0,0,.52),.055,2.04,'iron_dark',12)
    cylinder('Lacquer grip wrap',(0,0,0),.068,.20,'red_lacquer',12,bevel=True)
    for z in (-.115,.115,.38,.74,1.05):
        cylinder('Staff brass collar',(0,0,z),.070,.045,'brass',12,bevel=True)
    for z in (-.065,0,.065):
        cylinder('Grip winding',(0,0,z),.073,.032,'red_dark',12)
    cylinder('Butt brass collar',(0,0,-.39),.078,.09,'brass',12,bevel=True)
    solid('Forged butt spike',[(-.066,-.40),(.066,-.40),(.052,-.47),(0,-.55),(-.052,-.47)],.10,'iron',bevel=False)
    # 0.95 m toothed disc with a real four-way openwork centre.
    # Sampled gear-tooth tips sit off the vertical axis; account for that here.
    c=2.05-.475*math.cos(TAU/64)
    gear('Sixteen tooth pendulum rim',(0,0,c),.475,.390,.08,'brass',16)
    ring('Silver dial annulus',(0,.025,c),.408,.105,.08,'silver',32)
    ring('Back dial dark molding',(0,-.047,c),.402,.040,.035,'iron',32)
    for j in range(12):
        a=TAU*j/12
        box('Twelve raised hour bars',(.354*math.sin(a),.084,c+.354*math.cos(a)),
            (.033,.035,.062 if j%3 else .085),'dial_mark',rot=(0,a,0),bevel=False)
    for j in range(4):
        a=TAU*j/4
        pts=[(r*math.sin(a+t),.005,c+r*math.cos(a+t)) for r,t in ((.05,0),(.14,.28),(.24,.30),(.32,.03))]
        tube('Four swept openwork spokes',pts,.031,'steel',6)
        pin((.25*math.sin(a+.24),.061,c+.25*math.cos(a+.24)),.023,'brass_dark')
    cylinder('Black dial jewel socket',(0,.03,c),.090,.13,'iron_dark',12,FRONT,True)
    diamond((0,.118,c),.135,.17,'gem_red',.055)
    # Opposed blades: long +X sweep, shorter -X counter-sickle.
    long=[(.27,1.88),(.48,1.96),(.71,1.94),(.94,1.80),(1.12,1.58),(1.21,1.31),
          (1.22,1.08),(1.06,1.36),(.88,1.53),(.66,1.66),(.44,1.69),(.28,1.67)]
    short=[(-.26,1.80),(-.44,1.85),(-.64,1.79),(-.82,1.62),(-.93,1.39),(-.96,1.14),
           (-.84,1.37),(-.66,1.51),(-.46,1.57),(-.27,1.56)]
    solid('Long scythe forged body',long,.095,'steel')
    solid('Short counter scythe forged body',short,.095,'steel')
    edge=[(.28,1.67),(.44,1.69),(.66,1.66),(.88,1.53),(1.06,1.36),(1.22,1.08),
          (1.15,1.40),(.94,1.61),(.69,1.74),(.46,1.77),(.28,1.75)]
    shortedge=[(-.27,1.56),(-.46,1.57),(-.66,1.51),(-.84,1.37),(-.96,1.14),
               (-.89,1.45),(-.69,1.59),(-.48,1.65),(-.27,1.64)]
    for y in (-.064,.064):
        solid('Long bright cutting band',edge,.033,'silver',y,False)
        solid('Short bright cutting band',shortedge,.033,'silver',y,False)
    for x,z in ((.59,1.83),(.88,1.70),(-.54,1.72),(-.76,1.58)):
        solid('Broad inset oxidation scar',[(x-.045,z+.025),(x+.05,z+.03),(x+.07,z-.027),(x-.035,z-.04)],
              .032,'copper',y=.052,bevel=False)
        pin((x-.045,.077,z-.015),.020,'brass_dark')
    # Chain is offset from the shaft so all three chunky interlocking links read.
    tube('Chain hanger fork',[(.16,0,1.22),(.19,0,1.13)],.025,'iron')
    for j in range(3):
        link=ring('Hanging steel chain link',(.19,0,1.095-j*.082),.064,.031,.035,'steel',12)
        # Alternate front and side link planes, retaining a connected silhouette.
        if j%2:
            pivot=Vector((.19,0,1.095-j*.082))
            link.data.transform(Matrix.Translation(pivot) @ Matrix.Rotation(math.pi/2,4,'Z') @ Matrix.Translation(-pivot))
    lens((.19,0,.795),.20)
    diamond((.19,.066,.795),.065,.10,'dial_mark')


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        bm=bmesh.new(); bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(obj.data); bm.free(); obj.data.update()
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    if len(PARTS)>1: bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name; obj.data.name=name+'_Mesh'
    limit=5000 if name=='PendulumScythePlus' else 4000
    # Material seams survive simplification; no final dimension/origin rescaling.
    if triangles(obj)>=limit:
        mod=obj.modifiers.new('Preserve silhouette within kit budget','DECIMATE')
        mod.ratio=(limit-120)/triangles(obj); mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name+' open surface'
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    for p in obj.data.polygons: p.use_smooth=False
    bpy.context.view_layer.update()
    assert obj.location.length<1e-7 and obj.rotation_euler.to_matrix()==Matrix.Identity(3)
    assert triangles(obj)<limit,(name,triangles(obj))
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    areas={}
    for p in obj.data.polygons:
        role=obj.data.materials[p.material_index].name
        areas[role]=areas.get(role,0)+p.area
    assert len(areas)>=4,(name,areas)
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    if name in ATTACH:
        obj['attachment_world_m']=ATTACH[name]
        obj['origin_convention']='bone attachment; local zero'
    else:
        lo,hi=bounds(obj)
        assert abs(lo.z+.55)<.0001 and abs(hi.z-2.05)<.0001,(lo,hi)
        obj['origin_convention']='exact grip centre'
        obj['grip_centre_m']=[0.,0.,0.]
    obj['front']='+Y'; obj['long_axis']='+Z'; obj['triangle_count']=triangles(obj)
    prefix='Kit_W_' if name=='PendulumScythePlus' else 'Kit_KP_'
    dest=OUTPUT/(prefix+name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    global PARTS
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='SCREEN'
    shade.curvature_ridge_factor=1.15; shade.curvature_valley_factor=1.05
    shade.show_specular_highlight=False
    shade.background_type='WORLD'
    scene.world=bpy.data.worlds.new('Review background')
    scene.world.color=(.055,.065,.083)
    for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
    for obj in objects: obj.hide_render=True
    PARTS=[]
    helpers=[]
    # Neutral grey dummy uses its own PREVIEW ONLY material, never exported.
    dummy=bpy.data.materials.new('PREVIEW ONLY mannequin grey')
    dummy.diffuse_color=(.235,.25,.27,1)

    def grey(obj):
        obj.data.materials.clear(); obj.data.materials.append(dummy)
        for p in obj.data.polygons: p.use_smooth=True
        return obj

    def ellipsoid(loc,scale):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=loc)
        obj=bpy.context.object; obj.name='PREVIEW ONLY capsule'
        obj.scale=scale; grey(obj); helpers.append(obj)
        return obj

    # Same global camera: yaw the second complete assembly to expose its back.
    for x,yaw in ((2.55,0),(.12,math.radians(145))):
        root=Matrix.Translation(Vector((x,0,0))) @ Matrix.Rotation(yaw,4,'Z')
        begin=len(PARTS); start=len(helpers)
        ellipsoid((0,0,1.10),(.375,.30,.45))
        grey(cylinder('PREVIEW ONLY torso core',(0,0,1.04),.32,.48,'iron',16))
        ellipsoid((0,0,1.85),(.32,.32,.32))
        for s in (-1,1):
            arm=grey(cylinder('PREVIEW ONLY upper arm',(s*.39,0,1.22),.125,.47,'iron',12,rot=(0,-s*.18,0)))
            grey(cylinder('PREVIEW ONLY forearm',(s*.46,.015,.94),.11,.26,'iron',12))
            ellipsoid((s*.175,0,.45),(.145,.17,.37))
            grey(box('PREVIEW ONLY foot',(s*.175,.09,.15),(.28,.40,.20),'iron'))
        # Plume and back key indicate the pre-existing body's occupied spaces.
        plume=solid('PREVIEW ONLY existing plume',[(-.05,2.12),(.05,2.12),(.09,2.31),(.025,2.38),(-.08,2.29)],
                    .10,'red_dark',y=-.08,bevel=False)
        tube('PREVIEW ONLY existing key stem',[(0,-.28,1.12),(0,-.52,1.12)],.04,'steel_dark')
        for s in (-1,1): ring('PREVIEW ONLY windup key eye',(s*.10,-.54,1.12),.09,.04,.05,'steel_dark',16)
        # Operator-created spheres have deferred scale/matrix updates. Resolve
        # them before composing the assembly transform (especially the last leg).
        bpy.context.view_layer.update()
        for obj in helpers[start:]: obj.matrix_world=root @ obj.matrix_world
        for obj in PARTS[begin:]: obj.matrix_world=root @ obj.matrix_world
        for obj in objects:
            if obj.name not in ATTACH: continue
            inst=bpy.data.objects.new('PREVIEW ONLY '+obj.name,obj.data)
            scene.collection.objects.link(inst); helpers.append(inst)
            inst.matrix_world=root @ Matrix.Translation(Vector(ATTACH[obj.name]))
    weapon_obj=next(o for o in objects if o.name=='PendulumScythePlus')
    inst=bpy.data.objects.new('PREVIEW ONLY weapon',weapon_obj.data)
    scene.collection.objects.link(inst); helpers.append(inst)
    inst.location=(-2.65,0,1.0)
    for x in (2.55,.12,-2.65):
        cylinder('PREVIEW ONLY review plinth',(x,0,-.015),.86,.06,'iron_dark',48)
        # Two strips form a quiet floor grid, visibly distinct from exported kit.
        box('PREVIEW ONLY base index',(x,.71,.022),(.21,.032,.032),'brass_dark',bevel=False)
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
    target=Vector((.05,0,1.44))
    camera.location=target+Vector((0,15,3.7))
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'; camera.data.ortho_scale=8.15

    def caption(body,x,z,size=.10,role='silver',y=.43):
        data=bpy.data.curves.new('PREVIEW ONLY caption','FONT')
        data.body=body; data.size=size; data.align_x='CENTER'
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('PREVIEW ONLY caption',data)
        scene.collection.objects.link(obj); helpers.append(obj)
        obj.location=(x,y,z); obj.rotation_euler=camera.rotation_euler

    caption('C H A P T E R   I V   /   P E N D U L U M   K N I G H T',.05,3.48,.135)
    caption('ELITE ARMOR KIT    /    BONE-LOCAL METRES    /    NOIR CLOCKWORK',.05,3.28,.063,'brass')
    for x,title,sub in ((2.55,'FRONT / EQUIPPED','6 ATTACHMENTS  /  2.2 m BODY'),
                        (.12,'BACK / THREE-QUARTER','REAR HALO  /  KEY CLEARANCE'),
                        (-2.65,'PENDULUM SCYTHE +','2.60 m  /  GRIP AT Z 1.0')):
        caption(title,x,-.29,.095)
        caption(sub,x,-.47,.052,'brass')
    caption('SILVER PLATE   /   DARK IRON   /   CAST GEARS   /   TORN RED CLOTH',.05,-.70,.064,'steel')
    PREVIEW.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
    print(f'PREVIEW {PREVIEW}',flush=True)
    # Leave only the seven export meshes, with neutral materials and local anchors.
    scene.camera=None
    for obj in list(scene.objects):
        if obj not in objects: bpy.data.objects.remove(obj,do_unlink=True)
    for obj in objects: obj.hide_render=False
    for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    specs=[('Pauldron_R',pauldron),('Pauldron_L',pauldron),
           ('BackPendulum',back_pendulum),('ChestPendulum',chest_pendulum),
           ('Tasset',tasset),('HelmCrest',helm_crest),('PendulumScythePlus',weapon)]
    for i,(name,build) in enumerate(specs):
        PARTS=[]; random.seed(440730 if name.startswith('Pauldron') else 440730+i*7907)
        if name=='Pauldron_L':
            # Mirror the FINISHED topology. Bevel/decimate on separate mirrored
            # inputs can choose different diagonals, breaking an exact mirror.
            obj=bpy.data.objects.new('Mirrored shoulder',objects[0].data.copy())
            bpy.context.collection.objects.link(obj)
            obj.data.transform(Matrix.Diagonal((-1,1,1,1)))
            PARTS=[obj]
        else:
            build()
        objects.append(finish(name))
    # Mirrored local vertices must agree exactly, including lames, spikes and cloth.
    right,left=objects[:2]
    assert len(right.data.vertices)==len(left.data.vertices)
    for r,l in zip(right.data.vertices,left.data.vertices):
        assert (Vector((-r.co.x,r.co.y,r.co.z))-l.co).length<1e-6
    assert len(bpy.context.scene.objects)==7
    preview(objects)
    assert len(bpy.context.scene.objects)==7
    assert all(obj.type=='MESH' and obj.location.length<1e-7 for obj in objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
