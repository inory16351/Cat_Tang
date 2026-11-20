# 붕괴하는 시간 감옥의 벽 모듈 7종과 검수용 미리보기를 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_walls.py
"""Chapter V wall kit: deterministic metre-scale meshes, +Y room-facing.

Run from any directory. Only the review pass changes viewport colours.
Each FBX contains a single joined, closed-component mesh at the origin.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_walls_preview.png'
ROLES = ('void_stone', 'void_stone_dark', 'marble', 'marble_dark', 'violet',
         'violet_dark', 'indigo', 'night_sky', 'starlight', 'crack_gold',
         'gold', 'gold_dark', 'mirror', 'mirror_back', 'crystal', 'magenta',
         'wood', 'wood_dark', 'iron', 'iron_dark', 'steel', 'brass',
         'dial_face', 'dial_mark', 'sand', 'red_lacquer', 'cloth',
         'lamp_warm', 'porcelain', 'velvet')
PALETTE = {
    'void_stone': (.30, .23, .45), 'void_stone_dark': (.085, .065, .16),
    'marble': (.73, .70, .85), 'marble_dark': (.31, .27, .43),
    'violet': (.43, .21, .68), 'violet_dark': (.17, .085, .29),
    'indigo': (.075, .10, .25), 'night_sky': (.016, .019, .065),
    'starlight': (.91, .96, 1), 'crack_gold': (1, .69, .16),
    'gold': (.94, .61, .18), 'gold_dark': (.40, .24, .075),
    'mirror': (.48, .75, .91), 'mirror_back': (.10, .20, .29),
    'crystal': (.59, .39, .94), 'wood': (.38, .25, .32),
    'wood_dark': (.14, .095, .16), 'iron': (.22, .23, .33),
    'iron_dark': (.075, .09, .15), 'steel': (.53, .64, .74),
    'brass': (.65, .43, .19), 'dial_face': (.83, .81, .67),
    'dial_mark': (.065, .055, .13), 'sand': (.56, .45, .52),
}
MATS, PARTS = {}, []
FACE = (math.pi / 2, 0, 0)


def materials():
    for role in ROLES:
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
        mod = obj.modifiers.new('Soft chipped hard edges', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(65)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, bevel=False, rot=(0, 0, 0)):
    x, y, z = [s / 2 for s in size]
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def extrude(label, profile, back, front, role, bevel=False):
    n = len(profile)
    verts = [(x, y, z) for y in (back, front) for x, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return mesh(label, verts, faces, role, bevel)


def beam(label, a, b, width, depth, role, bevel=False):
    vec = Vector(b) - Vector(a)
    obj = box(label, (0, 0, 0), (width, depth, vec.length), role, bevel)
    obj.data.transform(Matrix.Translation((Vector(a) + Vector(b)) / 2) @
                       vec.to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def line(label, points, width=.055, depth=.045, role='crack_gold'):
    # Flat broad inlays; the base lies in the wall, with only .01m exposed.
    for a, b in zip(points, points[1:]):
        beam(label, a, b, width, depth, role)


def seam(points, y=.985):
    line('Recessed dark fracture bed', [(x, y-.012, z) for x, z in points], .105, .035, 'void_stone_dark')
    line('Inset luminous fracture', [(x, y+.006, z) for x, z in points], .052, .04)


def cylinder(label, loc, radius, depth, role, n=16, rot=FACE, bevel=False):
    verts = [(radius * math.cos(j * math.tau / n), radius * math.sin(j * math.tau / n), z)
             for z in (-depth/2, depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(j, (j+1) % n, (j+1) % n+n, j+n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def sector(label, x, y, z, outer, inner, a, b, role, depth=.18, n=12, bevel=False):
    """Closed annular arc; no overlapping wedges or coplanar radial caps."""
    profile = [(x+outer*math.cos(a+(b-a)*j/n), z+outer*math.sin(a+(b-a)*j/n)) for j in range(n+1)]
    profile += [(x+inner*math.cos(a+(b-a)*j/n), z+inner*math.sin(a+(b-a)*j/n)) for j in reversed(range(n+1))]
    return extrude(label, profile, y-depth/2, y+depth/2, role, bevel)


def ring(label, loc, outer, inner, depth, role, n=32):
    x, y, z = loc
    verts = [(x+r*math.cos(j*math.tau/n), yy, z+r*math.sin(j*math.tau/n))
             for yy, r in ((y-depth/2, outer), (y+depth/2, outer),
                           (y+depth/2, inner), (y-depth/2, inner)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1)%n, ((k+1)%4)*n+(j+1)%n, ((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    return mesh(label, verts, faces, role)


def star(x, y, z, r=.11, role='starlight'):
    return extrude('Four point star inlay', [(x+math.sin(j*math.pi/4)*(r if j%2==0 else r*.3),
                   z+math.cos(j*math.pi/4)*(r if j%2==0 else r*.3)) for j in range(8)], y-.025, y+.025, role)


def rock(label, loc, size, role, seed=0):
    rng = random.Random(seed)
    sx, sy, sz = size
    # Angular closed solid, intentionally not an icosphere/boulder blob.
    verts = []
    for z, scale, offset in ((-.5, .55, .12), (.04, 1, 0), (.5, .73, -.08)):
        for j in range(5):
            a = math.tau*j/5
            rr = scale*rng.uniform(.85, 1.08)
            verts.append((sx*(math.cos(a)*rr*.5+offset), sy*math.sin(a)*rr*.5, sz*z))
    faces = [tuple(reversed(range(5))), tuple(range(10,15))]
    for k in range(2):
        for j in range(5):
            a, b, c, d = k*5+j, k*5+(j+1)%5, (k+1)*5+(j+1)%5, (k+1)*5+j
            faces.extend(((a,b,c),(a,c,d)))
    return place(mesh(label, verts, faces, role), loc)


def crystal(loc, size=.55, rot=(0,0,0)):
    r = size*.23
    verts = [(r*math.cos(j*math.tau/5), r*math.sin(j*math.tau/5), z)
             for z in (0, size*.65) for j in range(5)] + [(size*.10, 0, size)]
    faces = [tuple(reversed(range(5)))]
    faces += [(j,(j+1)%5,(j+1)%5+5,j+5) for j in range(5)]
    faces += [(j+5,(j+1)%5+5,10) for j in range(5)]
    return place(mesh('Five sided crystal spear', verts, faces, 'crystal'), loc, rot)


def arch_profile(x, bottom, width, height, n=20):
    r = width/2
    spring = bottom+height-r
    return [(x-r,bottom),(x+r,bottom),(x+r,spring)] + [
        (x+r*math.cos(j*math.pi/n), spring+r*math.sin(j*math.pi/n)) for j in range(1,n+1)]


def coping(width, z, role='marble', floats=True):
    cuts = [-width/2, -width*.31, -.34, width*.19, width/2]
    for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
        top = z + (0, -.05, .08, -.025)[j]
        mid=(a+b)/2
        ridge=[(a,top),(a+.11,top-.02),(mid-.08,top-.03),
               (mid+.12,top-.08),(b-.15,top+.02),(b,top-.02)]
        extrude('Jagged broken coping', [(a,z-.30),(b,z-.30)]+list(reversed(ridge)),
                -.55, 1.05, role, True)
        def surface(x):
            for (u,zu),(v,zv) in zip(ridge,ridge[1:]):
                if u<=x<=v: return zu+(zv-zu)*(x-u)/(v-u)+.014
            return top+.014
        # The same fracture wraps across the cap, so the overhead view carries
        # the collapse motif as strongly as the front face does.
        x=mid-.15
        path=[(x,-.49,surface(x)),(x+.11,-.02,surface(x+.11)),
              (x-.08,.35,surface(x-.08)),(x+.05,1.04,surface(x+.05))]
        line('Gold fracture across coping top',path,.055,.045)
        line('Branch in coping top',[(x-.08,.35,surface(x-.08)),
             (a+.13,.55,surface(a+.13))],.045,.04)
    if floats:
        for j,(x,zz,s) in enumerate(((-width*.25,z+.30,.52),(width*.24,z+.28,.37))):
            rock('Hovering crown fragment', (x,-.47,zz), (s,.30,.22), role, 201+j)


def foundation(w, role='marble_dark'):
    box('Flush continuous foot', (0,.20,.12), (w,1.5,.24), role, True)
    box('Foundation upper fillet', (0,.27,.29), (w,1.52,.10), 'gold_dark')


def mirror_wall():
    profile = [(-3,0),(3,0),(3,3.86),(2.4,3.84),(1.8,3.81),(.7,3.86),
               (.25,3.82),(-.4,3.86),(-1.6,3.81),(-2.5,3.85),(-3,3.86)]
    extrude('Pale threshold structural wall', profile, -.55,.94,'marble',True)
    foundation(6)
    for x in (-1.48,1.48):
        # Actual panel dimensions are 1.8 x 3.2; the stepped frame sits outside.
        extrude('Deep arched mirror recess',arch_profile(x,.45,2.10,3.45),.935,.98,'gold_dark')
        extrude('Silver blue arched mirror',arch_profile(x,.52,1.8,3.2),.979,1.025,'mirror')
        spring = .52+3.2-.9
        for side in (-1,1):
            box('Ornate gold jamb',(x+side*.985,1.045,(.52+spring)/2),(.16,.19,spring-.52),'gold',True)
            box('Dark inner frame bead',(x+side*.915,1.13,(.52+spring)/2),(.045,.055,spring-.52),'gold_dark')
            for zz in (.65,1.08,2.24,spring):
                box('Stepped frame collar',(x+side*.985,1.06,zz),(.23,.24,.105),'gold',True)
        sector('Arched gold archivolt',x,1.06,spring,1.07,.93,0,math.pi,'gold',.19,22,True)
        sector('Gold dark inner bead',x,1.17,spring,.935,.89,0,math.pi,'gold_dark',.05,22)
        box('Ornate mirror sill',(x,1.065,.48),(2.12,.28,.15),'gold',True)
        star(x,1.19,3.88,.12,'gold')
        for sign in (-1,1):
            sector('Curled frame shoulder',x+sign*1.02,1.08,3.28,.20,.12,
                   .1,math.pi*1.55,'gold',.09,10)
        impact = (x+.10,1.066,1.92)
        for j in range(8):
            a = j*math.tau/8+.12
            dx,dz = math.cos(a),math.sin(a)
            reach = min(.82/max(abs(dx),.01),1.28/max(abs(dz),.01))
            pts=[impact,(impact[0]+dx*reach*.38,1.066,impact[2]+dz*reach*.3),
                 (impact[0]+dx*reach*.58+.09*dz,1.066,impact[2]+dz*reach*.62),
                 (impact[0]+dx*reach,1.066,impact[2]+dz*reach)]
            line('Radial mirror impact gold ridge',pts,.045,.045)
        # Seven popped angular shards, each with a separate dark back surface.
        for j,(dx,dz) in enumerate(((-.54,.75),(.36,.98),(-.40,.05),(.48,.20),
                                   (-.34,-.82),(.38,-.72),(.02,.46))):
            w,h=.30+random.random()*.12,.40+random.random()*.21
            shard=[(-w/2,-h/2),(w*.55,-h*.20),(w*.37,h*.5),(-w*.48,h*.23)]
            loc=(x+dx,1.23+(j%3)*.09,1.92+dz)
            rot=(math.radians(-12+j*4),math.radians(-14+j*4),math.radians(8-j*3))
            place(extrude('Detached mirror shard back',shard,-.055,.0,'mirror_back'),loc,rot)
            place(extrude('Detached mirror shard face',shard,0,.035,'mirror'),loc,rot)
    box('Violet central frieze pilaster',(0,1.0,2.2),(.54,.15,3.43),'violet',True)
    for z in (.90,1.55,2.20,2.85,3.48):
        star(0,1.105,z,.12)
    for j in (-1,1):
        box('Violet edge frieze',(j*2.80,.99,2.2),(.29,.12,3.4),'violet')
    box('Frieze over the arches',(0,.98,4.04),(6,.16,.20),'violet')
    coping(6,4.09,'gold')
    seam([(-2.94,.38),(-2.75,.82),(-2.82,1.24)],1.065)
    seam([(.09,3.70),(.24,4.01),(.03,4.19)],1.105)


def memory_seam(h):
    extrude('Central stitched void fault',[(-.80,.31),(.73,.31),(.59,1.4),(.80,2.2),
            (.42,h-.13),(.05,h),(-.60,h-.3),(-.44,2.8),(-.77,1.75)],.30,1.025,'void_stone',True)
    seam([(-.16,.35),(.13,1.00),(-.24,1.58),(.23,2.18),(-.13,2.75),(.16,3.28),(-.12,h-.1)],1.04)
    for z,s in ((.85,-1),(1.67,1),(2.62,-1),(3.36,1)):
        seam([(0,z),(s*.43,z+.20),(s*.72,z+.11)],1.04)
    for x,z in ((-.56,1.23),(.56,2.48),(-.48,3.22)):
        beam('Gold fracture stitch',(x-.15,1.085,z-.10),(x+.15,1.085,z+.10),.085,.075,'gold_dark',True)


def tilt_parts(start, pivot, degrees):
    mat = Matrix.Translation(Vector(pivot)) @ Euler((0,math.radians(degrees),0)).to_matrix().to_4x4() @ Matrix.Translation(-Vector(pivot))
    for obj in PARTS[start:]:
        obj.data.transform(mat)


def station_clock(x,y,z,r=.48, tilt=-11):
    start=len(PARTS)
    cylinder('Station clock stepped casing',(x,y,z),r,.22,'iron_dark',24,bevel=True)
    cylinder('Ivory station dial',(x,y+.14,z),r*.83,.05,'dial_face',24)
    ring('Station clock metal bezel',(x,y+.18,z),r*.98,r*.83,.07,'steel',24)
    for j in range(12):
        a=j*math.tau/12
        line('Raised clock hour mark',[(x+r*.64*math.sin(a),y+.22,z+r*.64*math.cos(a)),
                                     (x+r*.76*math.sin(a),y+.22,z+r*.76*math.cos(a))],.045,.035,'dial_mark')
    line('Clock hands',[(x-.18,y+.245,z+.15),(x,y+.245,z),(x+.02,y+.245,z+.29)],.045,.04,'dial_mark')
    tilt_parts(start,(x,y,z),tilt)


def memory_west():
    # A stable flush backing allows rotated memory relief to tile cleanly.
    extrude('Jagged memory substrate',[(-3,0),(3,0),(3,3.80),(2.60,3.80),(2.43,3.99),
            (1.58,3.72),(.66,3.87),(.22,4.0),(-.63,3.80),(-1.4,3.94),(-2.6,3.75),(-3,3.80)],-.55,.54,'void_stone_dark',True)
    foundation(6,'void_stone')
    start=len(PARTS)
    for j in range(6):
        x=-2.53+j*.34
        top=3.53+(j%3)*.12
        extrude('Weathered saloon plank',[(x-.155,.45),(x+.155,.45),(x+.155,top),
                (x+.04,top-.09),(x-.07,top+.06),(x-.155,top-.02)],.53,.92,'wood',True)
        for z in (.90,2.85):
            cylinder('Old plank nail',(x,.955,z),.045,.04,'iron_dark',8)
    for z in (.72,2.77,3.36):
        box('Saloon dark cross batten',(-1.70,.98,z),(1.89,.14,.16),'wood_dark',True)
    # One crooked, forward-swung saloon door, with its scalloped broken top.
    door=[(-.53,-.64),(.53,-.64),(.53,.43),(.31,.57),(0,.63),(-.26,.49),(-.53,.40)]
    place(extrude('Crooked swinging door half',door,-.09,.09,'wood_dark',True),(-1.74,1.22,1.68),(0,.13,-.22))
    for j in range(4):
        box('Swing door raised plank',(-2.08+j*.22,1.35,1.68),(.15,.075,.86),'wood',True,rot=(0,.13,-.22))
    for z in (1.14,2.10):
        box('Door strap hinge',(-2.23,1.28,z),(.31,.14,.13),'iron')
    tilt_parts(start,(-1.7,.8,2),7)
    start=len(PARTS)
    box('Station iron panel',(1.76,.72,2.03),(1.77,.38,3.42),'iron',True)
    box('Recessed station enamel',(1.76,.94,2.02),(1.40,.10,2.97),'indigo',True)
    for x in (.91,2.60):
        box('Girder front flange',(x,1.015,2.04),(.18,.22,3.60),'iron_dark',True)
        for z in (.61,1.4,2.2,3.14,3.62):
            cylinder('Steel station rivet',(x,1.15,z),.068,.06,'steel',8)
    for z in (.46,3.67):
        box('Girder cross flange',(1.76,1.015,z),(1.87,.26,.17),'steel',True)
    beam('Broken diagonal station girder',(1.03,1.02,.74),(2.43,1.02,2.22),.16,.13,'iron',True)
    station_clock(1.79,1.08,2.77,.52)
    beam('Bent clock hanger',(1.59,1.0,3.68),(1.76,1.10,3.29),.08,.10,'steel')
    tilt_parts(start,(1.76,.8,2.0),-6)
    memory_seam(3.91)
    seam([(-2.56,.42),(-2.24,1.0),(-2.32,1.47)],1.0)
    rock('Hovering saloon splinter',(-1.30,-.46,4.03),(.58,.32,.30),'wood',5)
    rock('Hovering iron cornice',(1.49,-.46,4.07),(.65,.32,.26),'iron',6)
    line('Saloon top gold fracture',[(-2.82,-.49,3.807),(-2.52,-.07,3.799),
         (-2.66,.29,3.784),(-2.48,.53,3.777)],.06,.04)
    line('Station top gold fracture',[(2.70,-.50,3.808),(2.77,-.04,3.808),
         (2.65,.51,3.808)],.06,.04)


def gear(x,y,z,r,role='gold',teeth=24, start=.22, end=math.tau-.26, fragment=True):
    """Open-spoke broken gear, missing teeth and a detached crown segment."""
    sector('Broken gear annular crown',x,y,z,r*.91,r*.69,start,end,role,.24,teeth*2)
    step=math.tau/teeth
    for j in range(teeth):
        a=j*step
        if not start+.04<a<end-.04 or j in (teeth//3,teeth//3+1):
            continue
        sector('Individual broad gear tooth',x,y,z,r,r*.86,a-step*.27,a+step*.27,role,.26,1)
    for j in range(6):
        a=.55+j*math.tau/6
        if a>end: continue
        beam('Broad radial gear spoke',(x+r*.16*math.cos(a),y,z+r*.16*math.sin(a)),
             (x+r*.76*math.cos(a),y,z+r*.76*math.sin(a)),r*.14,.19,role)
    cylinder('Gear concentric boss',(x,y+.045,z),r*.25,.30,'gold_dark',16,bevel=True)
    cylinder('Bright axle cap',(x,y+.22,z),r*.13,.09,'steel',12)
    box('Dark axle key',(x,y+.271,z),(r*.14,.03,.06),'dial_mark')
    if fragment:
        sector('Detached cracked off gear crown',x+.30,y+.10,z+.14,r*.94,r*.68,
               -.17,.16,role,.25,4)
        for a in (-.11,.09):
            sector('Detached fragment teeth',x+.30,y+.10,z+.14,r,r*.86,a-.045,a+.045,role,.26,1)
    line('Gold gear rupture',[(x-r*.75,y+.145,z-.32*r),(x-r*.34,y+.145,z-.06*r),
                            (x-r*.19,y+.145,z+.18*r)],.055,.045)


def memory_clock():
    extrude('Clock desert jagged backing',[(-3,0),(3,0),(3,3.95),(2.4,3.95),(2.0,4.10),
            (1.35,3.91),(.65,4.03),(.10,3.84),(-.65,4.03),(-1.3,3.72),(-2.1,3.94),(-3,3.88)],-.55,.60,'void_stone_dark',True)
    foundation(6,'marble_dark')
    start=len(PARTS)
    for row in range(5):
        for j in range(2):
            x=-2.15+j*.96+(row%2)*.05
            box('Sandstone memory ashlar',(x,.77,.70+row*.63),(.92,.40,.59),'sand',True)
    # Fossilised desert mirror shard with a dark cut edge.
    shard=[(-2.37,1.58),(-1.50,1.91),(-1.71,2.68),(-2.30,2.47)]
    extrude('Desert mirror shard dark socket',shard,.98,1.045,'mirror_back',True)
    extrude('Desert embedded mirror face',[(x*.99,z) for x,z in shard],1.046,1.075,'mirror')
    for j in range(4):
        crystal((-2.25+j*.27,1.04,.55),.62+(j%2)*.32,(-.30,.20*(j-1),0))
    tilt_parts(start,(-1.75,.8,2),-6)
    start=len(PARTS)
    extrude('Navy clock tower memory',[(.74,.43),(2.64,.43),(2.64,3.80),(2.15,3.95),(.74,3.73)],.62,1.0,'indigo',True)
    for x in (.86,2.51):
        box('Clock tower gilt stile',(x,1.05,2.12),(.10,.13,3.28),'gold_dark',True)
    box('Violet tower recessed panel',(1.70,1.04,1.28),(1.42,.08,1.23),'violet_dark',True)
    gear(1.62,1.10,2.92,.92,'brass',18,start=-math.pi/2,end=math.pi/2,fragment=False)
    beam('Pendulum broken suspension',(1.66,1.15,2.35),(1.49,1.18,1.08),.085,.10,'brass',True)
    cylinder('Pendulum stub bob',(1.49,1.20,1.03),.26,.18,'gold',16,bevel=True)
    tilt_parts(start,(1.70,.8,2),6)
    memory_seam(4.02)
    seam([(-2.80,.38),(-2.49,.92),(-2.62,1.42)],1.02)
    sector('Floating gear memory above wall',.98,.30,3.61,.70,.46,.37,1.66,'brass',.20,8)
    for a in (.52,.85,1.18,1.5):
        sector('Floating memory teeth',.98,.30,3.61,.79,.66,a-.08,a+.08,'brass',.22,1)
    rock('Floating desert keystone',(-1.65,-.46,4.20),(.66,.32,.28),'sand',12)
    line('Desert top gold fracture',[(-2.72,-.49,3.908),(-2.55,-.02,3.922),
         (-2.67,.57,3.912)],.06,.04)
    line('Clock tower top gold fracture',[(2.72,-.49,3.958),(2.81,-.06,3.958),
         (2.66,.57,3.958)],.06,.04)


def debris_edge():
    # The origin is the island's upper floor. Suspended geology is intentional.
    extrude('Floating island broken lip',[(-3,-.18),(3,-.18),(3,.05),(2.1,.12),(1.1,.02),
            (.2,.15),(-.5,.02),(-1.7,.12),(-3,.05)],-.95,.65,'void_stone',True)
    # Tapered underside pieces cover the full edge, all remain behind +Y=0.
    for j,(x,w,bottom) in enumerate(((-2.38,1.20,-2.27),(-1.21,1.32,-3.50),
                                   (.09,1.45,-2.65),(1.43,1.33,-3.10),(2.52,.96,-1.85))):
        profile=[(x-w/2,0),(x+w/2,0),(x+w*.38,-.72),(x+w*.23,bottom*.68),
                 (x-w*.10,bottom),(x-w*.32,bottom*.62),(x-w*.46,-.79)]
        extrude('Stalactite island geology',profile,-.93,-.03,'void_stone_dark')
        # Large lit front facets break the dark mass into legible planes.
        extrude('Violet geological facet',[(x-w*.45,-.12),(x+w*.39,-.18),
                (x+w*.19,bottom*.54),(x-w*.07,bottom*.79)],-.025,.015,'void_stone')
        seam([(x-.10,-.20),(x+.16,-.71),(x-.08,bottom*.60),(x+.02,bottom*.84)],.04)
    for j,(a,b,h) in enumerate(((-3,-1.78,.98),(-1.58,-.42,.83),(-.12,1.30,1.0),(1.62,3,.91))):
        extrude('Broken parapet section',[(a,0),(b,0),(b,h-.14),(b-.19,h),
                ((a+b)/2,h-.07),(a,h)],-.40,.60,'void_stone',True)
        extrude('Pale irregular parapet cap',[(a,h-.09),(b,h-.12),(b,h+.08),
                ((a+b)/2,h+.14),(a,h+.10)],-.47,.70,'marble',True)
        box('Parapet cap shadow molding',((a+b)/2,.61,h-.12),(b-a,.14,.10),'marble_dark',True)
        seam([((a+b)/2,.09),((a+b)/2-.16,.36),((a+b)/2+.07,h-.05)],.615)
    rock('Hovering broken cap',(.49,-.46,1.30),(.65,.32,.20),'marble',72)
    for j,(x,z) in enumerate(((-2.36,-2.77),(.64,-3.20),(2.43,-2.65))):
        rock('Detached underside debris',(x,-.66,z),(.58,.57,.50),'void_stone',50+j)
    for j,(x,z) in enumerate(((-1.52,-1.12),(.15,-1.62),(1.89,-.52))):
        for k in range(3):
            crystal((x+k*.16,.02,z),.45+.16*(k%2),(-.45,(k-1)*.50,.10))


def clock_heart():
    # Shallow curved plate composed of fitted strips; straight flush outer ends.
    xs=[-4,-3.2,-2.4,-1.6,-.8,0,.8,1.6,2.4,3.2,4]
    tops=[4.40,4.58,4.42,4.67,4.35,4.60,4.43,4.69,4.38,4.58,4.40]
    for j,(a,b) in enumerate(zip(xs,xs[1:])):
        y=.68+.15*(1-((a+b)/8)**2)
        extrude('Curved indigo clock backplate',[(a,0),(b,0),(b,tops[j+1]),(a,tops[j])],-.55,y,'indigo',True)
        extrude('Violet inset mechanism panel',[(a+.055,.38),(b-.055,.38),(b-.055,tops[j+1]-.20),
                (a+.055,tops[j]-.20)],y,y+.045,'violet_dark',True)
        box('Top backplate gilded edge',((a+b)/2,y+.025,(tops[j]+tops[j+1])/2-.08),(b-a-.04,.11,.09),'gold_dark')
    foundation(8,'indigo')
    # A monumental, broken ivory dial crescent behind the open gears.
    sector('Giant ivory dial arc',-.55,.98,2.02,2.62,2.23,.12,math.pi-.17,'dial_face',.13,38)
    sector('Dial outer gilded rim',-.55,1.00,2.02,2.68,2.60,.12,math.pi-.17,'gold',.12,38)
    # Raised Roman numerals, built from broad I/V/X bars rather than tiny type.
    for j in range(11):
        a=.28+j*(math.pi-.58)/10
        cx,cz=-.55+2.43*math.cos(a),2.02+2.43*math.sin(a)
        radial=Vector((math.cos(a),0,math.sin(a)))
        tangent=Vector((-math.sin(a),0,math.cos(a)))
        center=Vector((cx,1.085,cz))
        def bar(u,v):
            aa=center+tangent*u[0]+radial*u[1]
            bb=center+tangent*v[0]+radial*v[1]
            beam('Raised Roman numeral bar',aa,bb,.055,.045,'dial_mark')
        mode=j%4
        if mode==0:
            bar((-.10,.12),(0,-.12));bar((0,-.12),(.10,.12))
        elif mode==1:
            bar((-.09,-.13),(.09,.13));bar((.09,-.13),(-.09,.13))
        else:
            for k in range(mode):
                t=(k-(mode-1)/2)*.09
                bar((t,-.13),(t,.13))
    gear(-1.82,1.10,2.16,1.70,'gold',28)
    gear(1.06,1.12,2.60,1.10,'brass',20,start=.32,end=math.tau-.36)
    gear(2.85,1.10,1.45,.75,'gold_dark',16)
    for pts in ([(-3.65,.40),(-3.34,1.2),(-3.53,1.9)],
                [(2.76,3.16),(2.38,3.60),(2.51,4.07),(2.26,4.59)],
                [(-.12,.35),(.23,.8),(-.07,1.36),(.31,1.82)],
                [(-1.04,3.91),(-.76,4.17),(-.86,4.56)]):
        seam(pts,1.00)
    rock('Detached indigo clock crown',(-.34,-.46,4.85),(.78,.32,.28),'indigo',85)
    rock('Detached ivory dial fragment',(2.45,-.46,4.64),(.61,.32,.30),'dial_face',86)
    for x,z in ((-2.50,4.43),(.16,4.57),(2.70,4.46)):
        line('Backplate gold fracture over crown',[(x-.10,-.49,z),
             (x+.06,.05,z+.01),(x-.03,.65,z-.01)],.06,.04)
    for x,z in ((-3.61,3.74),(3.54,3.46),(.02,3.97),(3.42,.59)):
        star(x,1.04,z,.12)


def filler():
    extrude('Filler dark mortar backing',[(-2,0),(2,0),(2,2.40),(1.25,2.43),
            (.6,2.39),(-.2,2.40),(-1.2,2.43),(-2,2.40)],-.55,.83,'void_stone_dark',True)
    foundation(4,'marble_dark')
    for row in range(3):
        cuts=[-2]+([-.92,.40,1.45] if row%2 else [-1.42,-.15,1.02])+[2]
        for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
            box('Offset violet stone ashlar',((a+b)/2,.82,.60+row*.52),
                (b-a-.035,.32,.48),'void_stone' if (j+row)%4 else 'marble_dark',True)
    box('Violet upper star frieze',(0,.77,2.20),(4,.48,.58),'violet',True)
    for z in (1.93,2.47):
        box('Frieze fine gold edge',(0,1.035,z),(4,.07,.07),'gold_dark')
    for x in (-1.65,-.82,0,.82,1.65):
        star(x,1.045,2.21,.13,'gold')
    coping(4,2.59,'marble')
    seam([(-1.62,.34),(-1.32,.71),(-1.49,1.12),(-1.17,1.55),(-1.29,1.91)],1.0)
    seam([(.51,.35),(.80,.79),(.61,1.14),(1.05,1.63)],1.0)
    seam([(.23,2.47),(.06,2.65),(.21,2.73)],1.07)


def star_arch():
    # The 2.8 x 3.4 opening is truly open; its distant sky is the only back card.
    for side in (-1,1):
        box('Arch flush pier core',(side*2.20,.15,1.0),(1.60,1.40,2.0),'marble_dark')
        for row in range(4):
            box('Arch dressed pier block',(side*2.20,.63,.25+row*.50),(1.60,.68,.47),
                'marble' if row%3 else 'marble_dark',True)
        box('Arch pier foot',(side*2.20,.23,.12),(1.60,1.56,.24),'marble_dark',True)
        box('Arch spring impost',(side*2.20,.29,2.0),(1.60,1.57,.16),'gold_dark',True)
    # Tessellated spandrel columns conform to the round-topped opening.
    for j in range(28):
        a=-3+j*6/28; b=-3+(j+1)*6/28
        def bottom(x): return 2+math.sqrt(max(0,1.4**2-x*x)) if abs(x)<1.4 else 2
        za,zb=bottom(a),bottom(b)
        topa=4.12+(.07 if j%6==0 else 0)
        topb=4.12+(.07 if (j+1)%6==0 else 0)
        extrude('Open arch fitted spandrel',[(a,za),(b,zb),(b,topb),(a,topa)],-.55,.95,'marble')
    for j in range(13):
        a=j*math.pi/13+.012; b=(j+1)*math.pi/13-.012
        # Two missing ring stones are moved into air, exposing real sockets.
        dy,dz=(.17,.29) if j==5 else ((.10,.38) if j==8 else (0,0))
        role='gold' if j==6 else ('violet' if j%2 else 'marble_dark')
        sector('Displaced arch voussoir' if dz else 'Alternating arch voussoir',0,1.035+dy,2+dz,
               1.76,1.40,a,b,role,.25,3,True)
    # Keystone and a split frieze keep the break legible from overhead.
    extrude('Gold arch keystone',[(-.14,3.34),(.14,3.34),(.23,3.88),(-.23,3.88)],.97,1.25,'gold',True)
    for a,b in ((-3,-.17),(.13,3)):
        box('Split violet celestial frieze',((a+b)/2,.79,4.03),(b-a,.47,.29),'violet',True)
        box('Split gold frieze lip',((a+b)/2,1.045,3.90),(b-a,.08,.07),'gold_dark')
    coping(6,4.37,'marble',False)
    rock('Detached arch crown stone',(-.47,-.46,4.68),(.63,.32,.24),'marble',92)
    rock('Detached violet arch stone',(1.07,-.46,4.58),(.44,.32,.24),'violet',93)
    # Recess is precisely .6 behind the principal wall face (+.95 -> +.35).
    extrude('Night sky behind open portal',arch_profile(0,.02,2.80,3.38),.27,.35,'night_sky')
    # A shallow sky sill return also shows the void under the overhead camera.
    box('Void threshold return',(0,.65,.025),(2.8,.60,.05),'night_sky')
    stars=[(-1.08,.18),(-.69,.50),(-.23,.17),(.34,.30),(.99,.20),
           (-1.05,.89),(-.34,1.03),(.48,.91),(.98,1.30),(.06,1.59)]
    for j,(x,z) in enumerate(stars):
        star(x,.39,z,.075+(j%3)*.013)
    line('Faint crystal constellation',[(x,.375,z) for x,z in
         [(-1.08,.18),(-.69,.50),(-.34,1.03),(.48,.91),(.98,1.30)]],.045,.025,'crystal')
    seam([(-.08,3.76),(.06,4.06),(-.12,4.28),(.08,4.45)],1.09)
    seam([(-2.72,.31),(-2.45,.85),(-2.57,1.27),(-2.25,1.83)],.99)
    seam([(2.71,2.10),(2.40,2.61),(2.59,3.13),(2.14,3.57)],.975)


SPECS=[('Wall_Mirror',6,4.50,mirror_wall),('Wall_MemoryWest',6,4.20,memory_west),
       ('Wall_MemoryClock',6,4.40,memory_clock),('Wall_DebrisEdge',6,1.45,debris_edge),
       ('Wall_ClockHeart',8,5.00,clock_heart),('Wall',4,3.00,filler),
       ('Wall_StarArch',6,4.80,star_arch)]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name,width,height):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name,obj.data.name=name,name+'_Mesh'
    # Do not silently decimate the requested forms: fail if richness exceeds budget.
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges), name+' has open geometry'
    bm.to_mesh(obj.data); bm.free()
    for poly in obj.data.polygons: poly.use_smooth=False
    obj.data.update()
    obj.location,obj.rotation_euler,obj.scale=(0,0,0),(0,0,0),(1,1,1)
    bpy.context.view_layer.update()
    low,high=bounds(obj)
    assert abs(low.x+width/2)<1e-4 and abs(high.x-width/2)<1e-4,(name,low,high)
    floor=-3.5 if name=='Wall_DebrisEdge' else 0
    assert abs(low.z-floor)<1e-4,(name,low)
    assert high.z<=height+.025,(name,high,height)
    assert 1.2<=high.y-low.y<=2.25,(name,low,high)
    assert triangles(obj)<=8000,(name,triangles(obj))
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co),name
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)>=6 and used.issubset(ROLES) and 'crack_gold' in used,(name,used)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    # Check forward-facing surface balance (more useful than counting hidden backs).
    areas={}
    for p in obj.data.polygons:
        if p.normal.y>.3 or p.normal.z>.3:
            role=obj.data.materials[p.material_index].name
            areas[role]=areas.get(role,0)+p.area
    share=max(areas.values())/sum(areas.values())
    assert share<=.56,(name,'dominant visible material',share)
    obj['front'],obj['module_width_m']='+Y',width
    obj['chapter'],obj['triangle_count']='The Collapsing End of Time',triangles(obj)
    obj['origin_rule']='Island floor Z=0; underside extends to -3.5m' if name=='Wall_DebrisEdge' else 'Bottom center'
    destination=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    assert destination.is_file() and destination.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    print(f'VALIDATED {name}: roles={len(used)} largest_visible_role={share:.1%}',flush=True)
    return obj


def preview(objects):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light,shade.color_type='FLAT','MATERIAL'
    shade.show_shadows,shade.show_cavity=False,True
    shade.cavity_type='BOTH'
    shade.curvature_ridge_factor,shade.curvature_valley_factor=.85,1.15
    shade.show_specular_highlight=False
    shade.background_type,shade.background_color='WORLD',(.022,.024,.052)
    if not scene.world: scene.world=bpy.data.worlds.new('Preview world')
    scene.world.color=(.022,.024,.052)
    helpers=[]
    original=[obj.location.copy() for obj in objects]
    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE.get(role,(.5,.5,.5)),1)
        positions=[(-11.1,-4.2),(-3.7,-4.2),(3.7,-4.2),(-9.7,4.9),
                   (-.7,4.9),(7.3,4.9),(11.1,-4.2)]
        for obj,(x,y) in zip(objects,positions): obj.location=(x,y,0)
        camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera);helpers.append(camera);scene.camera=camera
        camera.data.type,camera.data.ortho_scale='ORTHO',33.4
        target=Vector((.1,.0,.65)); elevation=math.radians(55)
        camera.location=target+Vector((0,45*math.cos(elevation),45*math.sin(elevation)))
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        def caption(body,loc,size,role='starlight'):
            data=bpy.data.curves.new('Review label','FONT')
            data.body,data.align_x,data.size=body,'CENTER',size
            data.materials.append(MATS[role])
            obj=bpy.data.objects.new('Review label',data);scene.collection.objects.link(obj)
            obj.location,obj.rotation_euler=loc,camera.rotation_euler
            helpers.append(obj)
        desc=['S1 / SHATTERED THRESHOLD','S2 / WESTERN + STATION','S2 / DESERT + CLOCK',
              'S3 / SUSPENDED ISLAND EDGE','S4 / COLLAPSING CLOCK HEART','FILLER / STAR FRIEZE',
              'LANDMARK / OPEN STAR PORTAL']
        for obj,subtitle in zip(objects,desc):
            y=obj.location.y+(3.25 if obj.name=='Wall_DebrisEdge' else 2.0)
            caption(obj.name,(obj.location.x,y,-.18),.30)
            caption(subtitle,(obj.location.x,y+.45,-.18),.16,'gold')
        caption('V  /  THE COLLAPSING END OF TIME',(.1,-7.4,5.1),.51)
        caption('WALL MODULES    /    +Y FRONT    /    METRE SCALE',(.1,-6.75,5.1),.22,'gold')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj,loc in zip(objects,original): obj.location=loc
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    materials();OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for index,(name,width,height,build) in enumerate(SPECS):
        random.seed(51031+index*7919)
        PARTS=[];build();objects.append(finish(name,width,height))
    assert len(bpy.context.scene.objects)==7
    preview(objects)
    assert len(bpy.context.scene.objects)==7 and all(o.type=='MESH' for o in bpy.context.scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
