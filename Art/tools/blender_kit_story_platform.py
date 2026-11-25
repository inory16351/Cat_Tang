# 스토리 맵 2 '배웅하던 승강장' 키트 — 비 오는 밤 기차역, 2026-11-25
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_platform.py
# 온전한 19세기 말 서부 증기철도역: 회색 역할 재질 FBX 아홉 조각과 비 오는 밤 미리보기.
# Blender (X,Y,Z) = Unity (X,Z,Y); 카메라는 남쪽(-Y)에서 북쪽을 본다.
import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryPlatform'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_platform_preview.png'
TAU = math.tau
ROLES = (
    'stone', 'stone_dark', 'tile_light', 'tile_dark', 'cobble', 'cobble_dark',
    'brick', 'brick_dark', 'wood', 'wood_dark', 'iron', 'iron_dark', 'brass', 'gold',
    'rail_steel', 'ballast', 'clock_face', 'window_lit', 'lantern_glow', 'glass',
    'puddle', 'paint_cream', 'paint_green', 'poster', 'scarf', 'leather', 'shadow', 'shadow_rim',
)
HARD = set(ROLES) - {'ballast', 'puddle', 'window_lit', 'lantern_glow', 'glass', 'scarf', 'poster', 'shadow', 'shadow_rim'}
PALETTE = {  # Preview only; FBX materials stay flat grey for Unity recolouring by NAME.
    'stone': (.55,.6,.72), 'stone_dark': (.32,.35,.48),
    'tile_light': (.72,.74,.8), 'tile_dark': (.4,.42,.55),
    'cobble': (.5,.52,.62), 'cobble_dark': (.33,.34,.44),
    'brick': (.62,.36,.36), 'brick_dark': (.4,.24,.28),
    'wood': (.5,.34,.26), 'wood_dark': (.28,.18,.16),
    'iron': (.2,.22,.3), 'iron_dark': (.12,.13,.2),
    'brass': (.85,.62,.3), 'gold': (1,.78,.32), 'rail_steel': (.6,.64,.72),
    'ballast': (.3,.3,.36), 'clock_face': (1,.95,.82),
    'window_lit': (1,.8,.45), 'lantern_glow': (1,.72,.35),
    'glass': (.55,.7,.85), 'puddle': (.45,.6,.85),
    'paint_cream': (.95,.9,.78), 'paint_green': (.25,.45,.42),
    'poster': (.85,.7,.5), 'scarf': (.85,.25,.25), 'leather': (.45,.28,.2),
    'shadow': (.08,.05,.12), 'shadow_rim': (1,.7,.4),
}
MATS = {}
PARTS = []

def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Batch solid components before the shared 0.025m/two-segment bevel."""

    def __init__(self, label, verts, faces, role, bevel=True):
        assert role in ROLES, role
        self.label, self.role = label, role
        self.verts = [Vector(v) for v in verts]
        self.faces = faces
        self.bevel = bevel and role in HARD
        PARTS.append(self)

    def transform(self, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
        matrix = (Matrix.Translation(Vector(loc)) @
                  Euler(rot).to_matrix().to_4x4() @
                  Matrix.Diagonal(Vector((*scale, 1))))
        self.verts = [matrix @ v for v in self.verts]
        return self


def move_group(start, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    for part in PARTS[start:]:
        part.transform(loc, rot, scale)


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s/2 for s in size)
    return Part(label, [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    """Solid polygon in XZ; +Y is its decorated/front surface."""
    n = len(polygon)
    verts = [(x, y+d, z) for d in (-depth/2, depth/2) for x, z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    return Part(label, verts, faces, role, bevel)


def lathe(label, rings, role, n=12, bevel=True):
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, bevel)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), top=None):
    return lathe(label, [(-depth/2, radius),
                        (depth/2, radius if top is None else top)], role, n).transform(loc, rot)


def tube(label, points, radius, role, n=5, bevel=False):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i+1, len(points)-1)]) -
                   Vector(points[max(0, i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = ref.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [tuple(Vector(point) + radius*(u*math.cos(j*TAU/n) +
                                                v*math.sin(j*TAU/n))) for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n, len(points)*n)))
    return Part(label, verts, faces, role, bevel)


def rod(label, a, b, radius, role, n=6, bevel=True):
    return tube(label, [a,b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role):
    a, b = Vector(a), Vector(b)
    part = box(label, (0,0,0), (width,depth,(b-a).length), role)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def hoop(label, loc, radius, band, depth, role, n=24, rot=(0,0,0)):
    """Open ring in XZ, axis Y; no disk hiding the negative space."""
    verts = [(r*math.cos(j*TAU/n), y, r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role).transform(loc, rot)


def sector(label, inner, outer, start, end, depth, role, y=0, steps=4):
    angles = [start+(end-start)*j/steps for j in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    return extrude(label, polygon, depth, role, y)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.91,1.09)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    result = Part(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return result


def scroll(label, loc, radius=.3, flip=1):
    x,y,z = loc
    points = []
    for j in range(11):
        t = j/10
        a, r = -.8+t*math.pi*1.9, radius*(1-.73*t)
        points.append((x+flip*r*math.cos(a),y,z+r*math.sin(a)))
    return tube(label, points, .025 if radius < .3 else .035, 'gold')


def numeral(text, loc, height=.28, role='gold', angle=0):
    """Bold mesh bars, no font dependency. I, V and X are actual silhouettes."""
    start = len(PARTS)
    pitch = height*.45
    for i, char in enumerate(text):
        x = (i-(len(text)-1)/2)*pitch
        w = max(.04,height*.15)
        if char == 'I':
            box('Numeral I', (x,0,0), (w,.065,height), role)
        elif char == 'X':
            for s in (-1,1):
                beam('Numeral X', (x-s*height*.18,0,-height/2),
                     (x+s*height*.18,0,height/2), w,.065,role)
        elif char == 'V':
            for s in (-1,1):
                beam('Numeral V', (x,0,-height/2),
                     (x+s*height*.21,0,height/2), w,.065,role)
    move_group(start, loc, (0,angle,0))


def clock(loc, radius, hands=True):
    start = len(PARTS)
    cylinder('Clock iron back', (0,-.045,0), radius,.13,'iron',n=24,rot=(math.pi/2,0,0))
    cylinder('Cream enamel dial', (0,.045,0),radius*.86,.07,'clock_face',n=24,rot=(math.pi/2,0,0))
    hoop('Brass clock bezel',(0,.075,0),radius,.13*radius,.15,'brass')
    hoop('Gold inner bezel',(0,.12,0),radius*.84,.045,.06,'gold')
    for j in range(12):
        a = j*TAU/12
        box('Gold hour index',(radius*.69*math.sin(a),.13,radius*.69*math.cos(a)),
            (.045,.05,radius*.12),'gold',rot=(0,a,0))
    if hands:
        beam('Minute hand',(0,.19,0),(-radius*.32,.19,radius*.55),.065,.055,'iron')
        beam('Hour hand',(0,.20,0),(radius*.44,.20,radius*.18),.09,.06,'iron')
        cylinder('Clock pivot',(0,.24,0),.07,.06,'gold',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc)


# ======================= 배웅하던 승강장 =======================
# This is the exact Unity invisible-edge-wall polygon. Never grow or bevel it.
OUTLINE = [
    (-7.0,-41.0), (-7.2,-20.0), (-7.0,0.0), (-7.4,12.0),
    (-11.0,14.0), (-15.5,17.5), (-17.0,23.0), (-16.0,29.0),
    (-12.5,34.0), (-6.0,36.5), (0.0,36.0), (3.2,34.0), (3.2,-41.0),
]


def inside(pt, poly=OUTLINE):
    x, y = pt
    result = False
    for i, (x1,y1) in enumerate(poly):
        x2,y2 = poly[(i+1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < (x2-x1)*(y-y1)/(y2-y1)+x1:
            result = not result
    return result


def edge_distance(pt):
    p = Vector(pt)
    distances = []
    for i, a in enumerate(OUTLINE):
        a, b = Vector(a), Vector(OUTLINE[(i+1) % len(OUTLINE)])
        d = b-a
        t = max(0, min(1, (p-a).dot(d)/d.length_squared))
        distances.append((p-(a+t*d)).length)
    return min(distances)


def clip(poly, axis, value, above):
    """Half-plane clipping keeps surface tiles strictly inside OUTLINE."""
    out = []
    if not poly:
        return out
    a = poly[-1]
    for b in poly:
        ia = (a[axis] >= value) if above else (a[axis] <= value)
        ib = (b[axis] >= value) if above else (b[axis] <= value)
        if ia != ib:
            t = (value-a[axis])/(b[axis]-a[axis])
            out.append(tuple(a[k]+t*(b[k]-a[k]) for k in range(2)))
        if ib:
            out.append(b)
        a = b
    return out


def clipped_rect(x0, x1, y0, y1, poly=OUTLINE):
    for axis, value, above in ((0,x0,True),(0,x1,False),(1,y0,True),(1,y1,False)):
        poly = clip(poly,axis,value,above)
    return poly


def slab(label, poly, top, thick, role):
    if len(poly) < 3:
        return None
    area = abs(sum(poly[i][0]*poly[(i+1) % len(poly)][1] -
                   poly[(i+1) % len(poly)][0]*poly[i][1] for i in range(len(poly))))
    if area < 1e-6:
        return None
    n = len(poly)
    verts = [(x,y,z) for z in (top-thick,top) for x,y in poly]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1) % n,(i+1) % n+n,i+n) for i in range(n)]
    return Part(label,verts,faces,role,False)


def blob(x, y, radius, n=8):
    phase = random.uniform(0,TAU)
    return [(x+radius*r*math.cos(a),y+radius*r*math.sin(a))
            for a,r in [(phase+j*TAU/n+random.uniform(-.07,.07),
                         random.uniform(.78,1.0)) for j in range(n)]]


def flat_ring(label, xy, radius, band, top, role, n=64):
    # Preserve the ground's inlaid rings without bevel/decimation drift.
    p = hoop(label,(xy[0],xy[1],top-.02),radius,band,.04,role,n,
             rot=(math.pi/2,0,0))
    p.bevel = False
    return p


def ground():
    slab('Exact outline solid skirt',OUTLINE,-.6,.9,'stone_dark')
    platform = clipped_rect(-18,3.2,-41,13)
    slab('Platform grout and 0.6m slab',platform,-.025,.575,'stone_dark')
    # Square 1.2m checker tiles, with real recessed dark grout between them.
    for i in range(9):
        x0 = -7.4+i*1.2
        for j in range(45):
            y0 = -41+j*1.2
            poly = clipped_rect(x0+.025,min(x0+1.175,2.55),y0+.025,y0+1.175,platform)
            slab('Checker floor',poly,0,.56,'tile_light' if (i+j) % 2 else 'tile_dark')
    slab('Cream platform safety line',clipped_rect(2.55,2.9,-41,13),0,.6,'paint_cream')
    for j in range(45):
        slab('East coping',clipped_rect(2.9,3.2,-41+j*1.2,-41+(j+1)*1.2),0,.14,'stone')
    # Inlaid cross-platform mosaic carpets, kept almost flush with the tiles.
    for y in (-28,-12,4):
        slab('Mosaic dark border',clipped_rect(-7.25,2.55,y-.25,y+.25),.003,.04,'stone_dark')
        for j in range(19):
            x = -6.9+j*.5
            box('Mosaic diamond',(x,y,.007),(.28,.28,.04),
                'gold' if j % 3 == 0 else 'tile_dark',rot=(0,0,math.pi/4),bevel=False)
        for offset in (-.28,.28):
            slab('Mosaic brass edge',clipped_rect(-7.25,2.55,y+offset-.0225,y+offset+.0225),
                 .002,.04,'brass')
    yard = clip(OUTLINE,1,13,True)
    slab('Yard cobble grout',yard,-.035,.565,'cobble_dark')
    # Octagonal, jittered cobbles are solid blob chunks, not a flat colour field.
    for i in range(21):
        for j in range(25):
            x = -17+i*.98
            y = 13+j*.98
            poly = clipped_rect(x+.035,x+.945,y+.035,y+.945,yard)
            if not poly:
                continue
            cx,cy = x+.49,y+.49
            # Clip an irregular, chunky octagon to the original boundary cell.
            r = .48
            octagon = [(cx+dx*r*random.uniform(.91,1.0),cy+dy*r*random.uniform(.91,1.0))
                       for dx,dy in ((-1,-.65),(-.65,-1),(.65,-1),(1,-.65),
                                     (1,.65),(.65,1),(-.65,1),(-1,.65))]
            # The outline is clockwise; use cell clipping for boundary pieces.
            if all(inside(p) for p in octagon) and min(p[1] for p in octagon) >= 13:
                poly = octagon
            slab('Irregular yard cobble',poly,0,.54,
                 'cobble' if random.random() < .58 else 'cobble_dark')
    center = (-6.5,25)
    flat_ring('Battle circle stone outer',center,7.5,.55,.004,'stone')
    flat_ring('Battle circle brass band',center,6.95,.16,.006,'brass')
    flat_ring('Battle circle dark annulus',center,6.79,.85,.004,'tile_dark')
    flat_ring('Battle circle inner gold thread',center,5.94,.06,.006,'gold')
    # Eight long compass points alternate cardinal and intercardinal lengths.
    star = []
    for j in range(16):
        a = j*TAU/16
        r = (.8 if j % 2 else (4.7 if j % 4 == 0 else 3.35))
        star.append((center[0]+r*math.sin(a),center[1]+r*math.cos(a)))
    slab('Gold eight point compass rose',star,.008,.04,'gold')
    flat_ring('Compass center brass ring',center,.55,.12,.01,'brass',24)
    for j in range(12):
        a = j*TAU/12
        box('Twelve gold hour studs',(center[0]+6.4*math.sin(a),center[1]+6.4*math.cos(a),.012),
            (.18,.32,.06),'gold',rot=(0,0,-a),bevel=False)
    puddles = []
    for attempt in range(5000):
        if len(puddles) == 14:
            break
        x,y = random.uniform(-15,1),random.uniform(-38,33)
        r = random.uniform(.6,1.6)
        poly = blob(x,y,r,random.randint(8,10))
        if not all(inside(p) and edge_distance(p) >= 2 for p in poly):
            continue
        if any(math.hypot(x-px,y-py) < r+pr+.5 for px,py,pr in puddles):
            continue
        # Keep the battle compass clean and readable.
        if math.hypot(x+6.5,y-25) < 5.2+r:
            continue
        slab('Rain puddle %02d' % (len(puddles)+1),poly,.012,.04,'puddle')
        puddles.append((x,y,r))
    assert len(puddles) == 14, 'Need exactly fourteen interior puddles'
    # Railway: continuous ballast, staggered lumpy stones, full-length rails.
    box('Track ballast bed',(6,7,-.95),(5.6,110,.7),'ballast',bevel=False)
    for j in range(110):
        for x in (3.65,7.65,8.35):
            lump('Ballast chunk',(max(3.65,min(8.35,x+random.uniform(-.2,.2))),-47.55+j*.99,-.68),
                 (random.uniform(.45,.75),.75,.14),'ballast')
    for j in range(157):
        y = -47.8+j*.7
        box('Timber sleeper',(5.5,y,-.54),(2.6,.25,.12),'wood_dark',bevel=False)
        if j % 3 == 0:
            for x in (4.75,6.25):
                box('Rail chair',(x,y,-.46),(.35,.34,.08),'iron',bevel=False)
                for dx in (-.12,.12):
                    cylinder('Rail chair bolt',(x+dx,y,-.405),.04,.05,'brass',n=6).bevel = False
    for x in (4.75,6.25):
        box('Continuous steel rail',(x,7,-.4),(.12,110,.16),'rail_steel',bevel=False)
    box('Far platform solid',(10.65,7,-.79),(3.7,110,1.42),'stone_dark',bevel=False)
    for j in range(92):
        ya,yb = -48+j*1.2,min(62,-48+(j+1)*1.2)
        if yb <= ya:
            continue
        for i in range(3):
            xa,xb = 9.35+i*1.05,min(12.5,9.35+(i+1)*1.05)
            box('Far platform paving',((xa+xb)/2,(ya+yb)/2,-.04),
                (xb-xa-.025,yb-ya-.025,.08),'stone' if (i+j) % 3 else 'tile_light',bevel=False)
    box('Far dark coping',(8.95,7,-.07),(.3,110,.14),'stone_dark',bevel=False)
    box('Far cream safety line',(9.225,7,-.02),(.25,110,.04),'paint_cream',bevel=False)


def moulded_base(radius=.4, square=False):
    if square:
        for z,w,h,role in ((.09,radius*2.3,.18,'stone_dark'),(.25,radius*2,.14,'stone'),
                           (.37,radius*1.8,.1,'stone_dark'),(.45,radius*1.4,.08,'brass')):
            box('Stepped square plinth',(0,0,z),(w,w,h),role)
    else:
        lathe('Dark base moulding',[(0,radius),(.1,radius),(.16,radius*.88)],'stone_dark',n=12)
        lathe('Stone pedestal',[(.16,radius*.88),(.3,radius*.8),(.4,radius*.55)],'stone',n=12)
        cylinder('Brass base collar',(0,0,.41),radius*.57,.08,'brass',n=12)


def fluted_post(bottom, top, radius=.14):
    lathe('Fluted iron core',[(bottom,radius),(top,radius*.88)],'iron_dark',n=12)
    for j in range(10):
        a = j*TAU/10
        r = radius*.92
        rod('Raised cast iron flute',(r*math.cos(a),r*math.sin(a),bottom+.08),
            (r*.9*math.cos(a),r*.9*math.sin(a),top-.08),.028,'iron',n=5,bevel=False)
    for z in (bottom+.05,top-.05):
        cylinder('Gold post collar',(0,0,z),radius*1.3,.07,'gold',n=12).bevel = False


def lantern_head(loc, scale=1):
    start = len(PARTS)
    box('Lantern brass foot',(0,0,-.31),(.44,.44,.08),'brass')
    box('Warm gas glass',(0,0,0),(.34,.34,.54),'lantern_glow',bevel=False)
    for x in (-.19,.19):
        for y in (-.19,.19):
            beam('Lantern cage',(x*.87,y*.87,-.27),(x,y,.3),.045,.045,'brass')
    box('Lantern eave',(0,0,.31),(.48,.48,.08),'iron_dark')
    lathe('Lantern crown',[(.35,.33),(.51,.13),(.55,.08)],'brass',n=4).transform(rot=(0,0,math.pi/4))
    lathe('Gold crown finial',[(.54,.075),(.63,.09),(.72,.02)],'gold',n=8)
    move_group(start,loc,scale=(scale,scale,scale))


def canopy_bay():
    moulded_base(.36,True)
    fluted_post(.49,4.28,.19)
    cylinder('Gold mid column band',(0,0,1.2),.24,.12,'gold',n=12)
    for z,r,h,role in ((4.25,.24,.13,'brass'),(4.37,.31,.12,'gold'),
                        (4.49,.4,.12,'gold'),(4.61,.44,.12,'iron')):
        cylinder('Acanthus capital ring',(0,0,z),r,h,role,n=12)
    for j in range(6):
        a = j*TAU/6
        start = len(PARTS)
        scroll('Capital curled leaf',(.27,0,4.4),.16)
        move_group(start,rot=(0,0,a))
    # Three oversized pierced scroll brackets, in XY-aligned vertical planes.
    for angle in (0,math.pi/2,-math.pi/2):
        start = len(PARTS)
        tube('Sweeping canopy bracket',[(.05,0,3.4),(.32,0,3.55),(.75,0,3.9),
                                       (1.1,0,4.45),(1.55,0,4.7)],.065,'gold',n=6)
        scroll('Large open bracket curl',(.76,0,4.18),.44)
        scroll('Small bracket curl',(.3,0,3.7),.23,-1)
        move_group(start,rot=(0,0,angle))
    def roof_z(x):
        return 5.22 - abs(x+.1)*.14
    def roof_panel(label, xa, xb, ya, yb, role, lift=0):
        verts = [(x,y,roof_z(x)+dz+lift) for dz in (-.065,0)
                 for x,y in ((xa,ya),(xb,ya),(xb,yb),(xa,yb))]
        return Part(label,verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),
                                 (1,2,6,5),(2,3,7,6),(3,0,4,7)],role,False)
    for j in range(9):
        y = -4+j
        for xa,xb in ((-1.6,-.1),(-.1,2.6)):
            beam('Cast iron roof rafter',(xa,y,roof_z(xa)),(xb,y,roof_z(xb)),.18,.18,'iron')
        for x in (-1.6,2.6):
            cylinder('Rafter brass rivet',(x,y,roof_z(x)+.09),.05,.055,'brass',n=6).bevel = False
    # Subdivide each roof slope into thick framed glass panes and opaque fascia.
    for xa,xb in ((-1.6,-.1),(-.1,1.25),(1.25,2.6)):
        for j in range(8):
            ya,yb = -3.89+j,-3.11+j
            roof_panel('Roof glass pane',xa+.08,xb-.08,ya,yb,'glass')
            # Broad enamel glazing beads break up the roof's dominant glass area.
            for a,b in ((xa+.08,xa+.16),(xb-.16,xb-.08)):
                roof_panel('Cream glazing side bead',a,b,ya,yb,'paint_cream',.025)
            for a,b in ((ya,ya+.08),(yb-.08,yb)):
                roof_panel('Cream glazing end bead',xa+.16,xb-.16,a,b,'paint_cream',.025)
    for x in (-1.6,-.1,1.25,2.6):
        box('Long roof purlin',(x,0,roof_z(x)),(.16,8,.16),'iron_dark')
        box('Purlin gold crest',(x,0,roof_z(x)+.09),(.045,8,.04),'gold',bevel=False)
    box('Wood roof ridge',(-.1,0,5.25),(.45,8,.15),'paint_green')
    box('Cream ridge trim',(-.1,0,5.34),(.16,8,.05),'paint_cream',bevel=False)
    for x in (-1.63,2.63):
        box('Dark gutter',(x,0,roof_z(x)-.08),(.22,8,.19),'iron_dark')
    # Long valance is in the YZ plane. Bottom edge is an actual dog-tooth outline.
    poly = [(-4,4.85),(4,4.85),(4,4.48)]
    for j in range(32,-1,-1):
        poly.append((-4+j*.25,4.35 if j % 2 else 4.5))
    extrude('Dog tooth wooden valance',poly,.12,'paint_green').transform((2.6,0,0),rot=(0,0,math.pi/2))
    box('Valance wood backing',(2.53,0,4.77),(.16,8,.22),'wood')
    box('Valance cream trim',(2.68,0,4.7),(.065,8,.07),'paint_cream',bevel=False)
    for j in range(17):
        cylinder('Valance gold fixing',(2.73,-4+j*.5,4.78),.045,.05,'gold',n=6,
                 rot=(0,math.pi/2,0)).bevel = False
    for y in (-4,4):
        lathe('Canopy ridge finial',[(5.3,.08),(5.4,.02)],'gold',n=8).transform((-.1,y,0))
    tube('Brass hanging lamp hook',[(1.45,0,4.95),(1.8,0,4.9),(1.83,0,4.65),(1.77,0,4.59)],.04,'brass',n=6)
    lantern_head((1.8,0,4.08),.65)


def gas_lamp():
    moulded_base(.38)
    fluted_post(.47,2.7,.13)
    for z in (1.12,2.42,2.72):
        cylinder('Gold lamp collar',(0,0,z),.19,.09,'gold',n=12)
    beam('Ladder cross arm',(-.47,0,2.56),(.47,0,2.56),.08,.09,'iron_dark')
    for s in (-1,1):
        cylinder('Cross arm brass stop',(s*.45,0,2.56),.075,.12,'brass',n=8,rot=(0,math.pi/2,0))
        scroll('Lamp shoulder curl',(s*.19,0,2.81),.18,s)
    lantern_head((0,0,3.125),.94)


def clock_1159(loc, radius=.7, double=True):
    """Clock helper with twelve dark ticks and the same 11:59 on BOTH dials."""
    for side in ((-1,1) if double else (-1,)):
        start = len(PARTS)
        clock((0,0,0),radius,hands=False)
        # Replace the inherited gold tick role with iron_dark, keeping geometry.
        for p in PARTS[start:]:
            if p.label == 'Gold hour index':
                p.role = 'iron_dark'
        for label,angle,length,width,depth in (
                # Local dial faces +Y: positive X appears LEFT to its viewer.
                ('Minute hand 59',TAU/60,radius*.73,.052,.20),
                ('Hour hand 11:59',TAU/720,radius*.49,.075,.25)):
            beam(label,(0,depth,0),(length*math.sin(angle),depth,length*math.cos(angle)),
                 width,.045,'iron_dark')
        cylinder('Clock hand pin',(0,.29,0),.06,.05,'gold',n=8,rot=(math.pi/2,0,0))
        move_group(start,(loc[0],loc[1]+side*.08,loc[2]),(0,0,0 if side > 0 else math.pi))


def hang_clock():
    moulded_base(.38)
    fluted_post(.48,4.35,.14)
    cylinder('Clock post brass crown',(0,0,4.35),.23,.15,'brass',n=12)
    lathe('Clock post finial',[(4.42,.17),(4.62,.1),(4.8,.02)],'gold',n=8)
    tube('Gold clock arm',[(0,0,4.2),(.24,0,4.52),(.7,0,4.61),
                           (1.2,0,4.51),(1.2,0,4.33)],.07,'gold',n=6)
    scroll('Clock arm pierced scroll',(.56,0,4.25),.35)
    rod('Clock hanging link',(1.2,0,4.35),(1.2,0,4.14),.045,'brass',n=6)
    clock_1159((1.2,0,3.44),.7)
    for s in (-1,1):
        scroll('Clock gold crest curl',(1.2+s*.21,0,4.13),.2,s)
    box('Gold clock crest',(1.2,0,4.24),(.2,.14,.18),'gold',rot=(0,math.pi/4,0))


def ticket_gate():
    for s in (-1,1):
        x = s*4.6
        box('Pillar dark footing',(x,0,.12),(1,1.2,.24),'stone_dark')
        box('Gate brick pillar',(x,0,2.3),(.8,.92,4.4),'brick')
        for j in range(8):
            z = .4+j*.5
            box('Pillar brick band',(x,0,z),(.85,.98,.08),'brick_dark',bevel=False)
            for dx in (-.34,.34):
                for y in (-.47,.47):
                    box('Gate stone quoin',(x+dx,y,z+.18),(.22,.18,.32),
                        'stone' if j % 3 else 'paint_cream')
        box('Pillar cornice',(x,0,4.54),(1,1.15,.2),'stone')
        box('Gold pillar cap',(x,0,4.71),(1,1.16,.14),'gold')
        lathe('Gate finial',[(4.78,.24),(4.92,.24),(5.15,.06)],'brass',n=8).transform((x,0,0))
        # Wickets occupy ONLY the flanks; central X -2.2..2.2 stays empty.
        a,b = (2.2,4.2) if s > 0 else (-4.2,-2.2)
        cx = (a+b)/2
        box('Ticket booth plinth',(cx,0,.08),(2,1.12,.16),'stone_dark')
        box('Solid wood wicket',(cx,0,.6),(2,1,.95),'wood')
        box('Ticket counter',(cx,-.02,1.1),(2,1.2,.15),'wood_dark')
        for side in (-1,1):
            box('Recessed counter panel',(cx,side*.515,.61),(1.6,.05,.56),'paint_green')
            for dx in (-.84,.84):
                box('Panel gold vertical trim',(cx+dx,side*.55,.6),(.055,.055,.68),'gold',bevel=False)
            for z in (.27,.95):
                box('Panel brass horizontal trim',(cx,side*.55,z),(1.74,.055,.055),'brass',bevel=False)
        for dx in (-.88,.88):
            box('Booth upright',(cx+dx,.2,1.95),(.13,.15,1.7),'wood_dark')
        box('Warm booth interior',(cx,.36,1.95),(1.64,.06,1.5),'window_lit',bevel=False)
        box('Booth window glass',(cx,.25,1.95),(1.64,.06,1.5),'glass',bevel=False)
        # Exposed warm inset panes remain readable even with Unity's opaque cel shader.
        for dx,w in ((-.62,.32),(0,.68),(.62,.32)):
            box('Booth warm pane inset',(cx+dx,.205,1.95),(w,.05,1.32),'window_lit',bevel=False)
        for dx in (-.4,.4):
            box('Window brass mullion',(cx+dx,.18,1.95),(.06,.07,1.52),'brass',bevel=False)
        box('Booth top moulding',(cx,.2,2.8),(2,.72,.18),'paint_cream')
        cylinder('Turnstile spindle',(cx,-.34,1.37),.075,.4,'brass',n=8)
        for j in range(3):
            a = j*TAU/3
            rod('Brass turnstile arm',(cx,-.34,1.4),
                (cx+.66*math.cos(a),-.34+.22*math.sin(a),1.4),.045,'brass',n=6,bevel=False)
    # Arched lattice has real negative space. Its lowest member is above 3.2m.
    for zoff,role,r in ((0,'iron',.09),(.43,'gold',.055)):
        pts = [(x,0,3.4+1.13*(1-(x/4.2)**2)+zoff) for x in [-4.2+j*.35 for j in range(25)]]
        tube('Open arched lattice rail',pts,r,role,n=6)
    for j in range(21):
        x = -4+j*.4
        z = 3.4+1.13*(1-(x/4.2)**2)
        beam('Arch lattice diagonal',(x,0,z),(x+.25,0,z+.4),.055,.07,'iron')
    box('Gold sign outer frame',(0,-.12,4.31),(3.7,.22,.8),'gold')
    box('Green enamel sign board',(0,-.26,4.31),(3.44,.1,.57),'paint_green')
    for s in (-1,1):
        scroll('Raised gold sign relief',(s*.88,-.34,4.31),.19,s)
        box('Sign relief diamond',(s*1.5,-.34,4.31),(.15,.06,.15),'gold',rot=(0,math.pi/4,0))
    clock_1159((0,0,5.04),.42,False)
    for s in (-1,1):
        scroll('Clock pediment scroll',(s*.64,-.12,4.91),.29,s)


def station_wall():
    box('Station wall brick core',(0,0,3.25),(.8,12,6.5),'brick')
    box('Dark foundation',(0,0,.16),(1,12,.32),'stone_dark')
    box('Stone base course',(0,0,.52),(.94,12,.4),'stone')
    for z in (1.0,3.0,5.45):
        box('Brick dark belt',(0,0,z),(.87,12,.18),'brick_dark',bevel=False)
    for z,w,h,role in ((5.9,.98,.18,'stone_dark'),(6.12,1.08,.22,'stone'),
                        (6.33,1.2,.18,'paint_cream'),(6.46,1.08,.08,'iron_dark')):   # 위에서 보이는 지붕 윗면은 어둡게 (금 띠가 한 줄로 이어져 보임 · 2026-11-25)
        box('Continuous cornice',(0,0,z),(w,12,h),role)
    for j in range(29):
        box('Cornice dentil',(.5,-5.8+j*.415,5.91),(.27,.22,.27),'stone')
    # Build the facade in local XZ, then turn +Y toward world +X.
    start = len(PARTS)
    for x in (-4,0,4):
        spring = 4.05
        arch = [(x-1.08,1.3),(x+1.08,1.3),(x+1.08,spring)]
        arch += [(x+1.08*math.cos(j*math.pi/12),spring+1.08*math.sin(j*math.pi/12))
                 for j in range(1,13)]
        extrude('Recessed arched window shadow',[(px,zz) for px,zz in arch],.08,'iron_dark',y=.425)
        inner = [(x-.91,1.48),(x+.91,1.48),(x+.91,spring)]
        inner += [(x+.91*math.cos(j*math.pi/12),spring+.91*math.sin(j*math.pi/12))
                  for j in range(1,13)]
        extrude('Tall glowing arched pane',inner,.07,'window_lit',y=.49,bevel=False)
        for dx in (-1.12,1.12):
            box('Stone window jamb',(x+dx,.52,2.68),(.21,.23,2.9),'stone')
        for j in range(9):
            sector('Stone arch voussoir',1.03,1.3,j*math.pi/9+.016,(j+1)*math.pi/9-.016,
                   .23,'stone' if j % 3 else 'paint_cream',y=.53,steps=2).transform((x,0,spring))
        box('Window keystone',(x,.56,5.29),(.28,.28,.35),'gold')
        box('Stone window sill',(x,.55,1.34),(2.55,.38,.2),'stone_dark')
        for dx in (-.46,0,.46):
            box('Iron window mullion',(x+dx,.56,2.77),(.065,.08,2.65),'iron_dark',bevel=False)
        for z in (2.35,3.3,4.04):
            box('Iron window transom',(x,.56,z),(1.87,.08,.065),'iron_dark',bevel=False)
        for j in range(1,6):
            a = j*math.pi/6
            rod('Fanlight radial bar',(x,.57,spring),
                (x+.9*math.cos(a),.57,spring+.9*math.sin(a)),.027,'iron_dark',n=5,bevel=False)
    for x in (-5.75,-2,2,5.75):
        box('Pilaster dark foot',(x,.5,.89),(.6,.25,.24),'stone_dark')
        box('Stone pilaster',(x,.5,3.21),(.38,.24,4.4),'stone')
        for dx in (-.11,.11):
            box('Pilaster flute',(x+dx,.64,3.2),(.045,.04,4.1),'stone_dark',bevel=False)
        box('Pilaster gold capital',(x,.53,5.5),(.62,.34,.21),'gold')
        for s in (-1,1):
            scroll('Pilaster capital curl',(x+s*.17,.65,5.48),.14,s)
    for x in (-2,2):
        box('Poster outer dark frame',(x,.73,2.55),(1.13,.18,1.74),'wood_dark')
        box('Poster brass inner frame',(x,.84,2.55),(.96,.055,1.57),'brass')
        box('Station poster',(x,.88,2.55),(.81,.045,1.42),'poster',bevel=False)
        # Pictorial travel advertisement; no lettering or modern graphics.
        cylinder('Poster moon',(x,.915,2.85),.2,.04,'paint_cream',n=12,rot=(math.pi/2,0,0)).bevel=False
        for z,w in ((2.05,.55),(2.18,.65),(2.35,.48)):
            box('Poster ornamental print',(x,.916,z),(w,.04,.055),'paint_green',bevel=False)
    move_group(start,rot=(0,0,-math.pi/2))
    # Rear elevation also has recessed bands, avoiding a single-colour slab.
    for y in (-4,0,4):
        box('Rear recessed brick panel',(-.425,y,3.1),(.06,3.2,3.9),'brick_dark')
        box('Rear cream inset',(-.465,y,3.1),(.04,2.85,3.5),'paint_cream',bevel=False)


def fence():
    for j in range(5):
        y = -3+j*1.5
        box('Fence stone shoe',(0,y,.06),(.3,.3,.12),'stone_dark')
        box('Fence stone socket',(0,y,.15),(.22,.22,.1),'stone')
        rod('Fence dark post',(0,y,.2),(0,y,1.22),.065,'iron_dark',n=8,bevel=False)
        cylinder('Fence brass collar',(0,y,1.15),.105,.09,'brass',n=8).bevel=False
        lathe('Gold spear finial',[(1.22,.07),(1.31,.12),(1.4,.0)],'gold',n=4).transform((0,y,0))
    for z in (.32,1.03):
        box('Iron fence rail',(0,0,z),(.075,6,.075),'iron')
    for j in range(24):
        y = -2.875+j*.25
        rod('Fence upright bar',(0,y,.26),(0,y,1.12),.024,'iron',n=5,bevel=False)
    for j in range(12):
        start = len(PARTS)
        scroll('Fence repeating gold scroll',(-2.75+j*.5,0,1.1),.19,1 if j % 2 else -1)
        move_group(start,rot=(0,0,math.pi/2))


GHOST = {}   # 유령 꼬리 아랫단 높이 → finish()가 정점 색 알파(아래로 갈수록 투명)를 구움


def ghost_tail(z_top, r_top, depth=.67, s=1.0, extra=None):
    """유령 꼬리 (2026-11-25 사용자: 불꽃 X · 꼬리를 아래로 길게 늘리고 끝으로 갈수록 디졸브되어 배경에 섞이게)
    코트 아랫단에서 한 덩어리로 내려가며 가늘어지고 살짝 뒤(+Y)로 흐름. 투명도는 정점 색 알파 (Unity CatTang/GhostFade)"""
    extra = .5 * s if extra is None else extra                                  # 꼬리를 아랫단에서 더 길게 (2026-11-25 사용자)
    rings, seg, n = [], 18, 14
    for i in range(seg + 1):
        t = i / seg
        x = .07 * s * math.sin(t * math.pi * 1.4)                              # 살짝 S자로 흔들림
        y = .5 * s * t ** 1.6                                                 # 아래로 갈수록 뒤로 끌림
        z = z_top - (z_top + extra) * t                                        # 땅 아래로 extra만큼 더 → 몸 전체가 그만큼 떠오름 (finish가 맨 아래를 0으로)
        r = r_top * (1.06 - .06 * t) * (1 - t) ** .55 + .02 * s                # 천천히 가늘어짐 (끝까지 꽤 굵게 → 투명해지며 사라짐)
        r *= 1 + .07 * math.sin(t * math.pi * 4)                                # 물결치는 굵기
        rings.append((Vector((x, y, z)), r))
    verts, faces = [], []
    for c, r in rings:
        for k in range(n):
            a = TAU * k / n
            verts.append((c.x + math.cos(a) * r, c.y + math.sin(a) * r * depth, c.z))
    for i in range(seg):
        for k in range(n):
            a, b = i * n + k, i * n + (k + 1) % n
            faces.append((a, b, b + n, a + n))
    top = len(verts); verts.append((rings[0][0].x, rings[0][0].y, rings[0][0].z + .02))
    faces += [(top, (k + 1) % n, k) for k in range(n)]
    tip = len(verts); c, r = rings[-1]; verts.append((c.x, c.y + .03 * s, c.z))
    base = seg * n
    faces += [(base + k, base + (k + 1) % n, tip) for k in range(n)]
    Part('Ghost tail', verts, faces, 'shadow', bevel=False)
    GHOST['top'] = (z_top + extra) * 1.08                                      # 바닥에서 재므로 꼬리 전체 길이


def ghost_fade(obj):
    """유령 꼬리가 있으면: 바닥(0) 투명 → 아랫단 위로는 불투명, 정점 색 알파로 (부드러운 곡선)"""
    top = GHOST.pop('top', None)
    if not top:
        return
    me = obj.data
    attr = me.color_attributes.new('Col', 'BYTE_COLOR', 'POINT')
    for v in me.vertices:
        t = max(0., min(1., v.co.z / top))
        attr.data[v.index].color = (1., 1., 1., t * t * (3 - 2 * t))


def silhouette_body(height, woman=False):
    s = height/1.85
    start = len(PARTS)
    ghost_tail(.4,.34 if woman else .29)                  # 다리 대신 연기 꼬리 (떠다니는 유령)
    # Elliptical long coat, fitted at waist and flared at its intact hem.
    rings = [(.4,.34 if woman else .29),(.47,.33 if woman else .285),
             (.98,.19),(1.25,.225),(1.44,.24)]
    lathe('Long fitted coat',rings,'shadow',n=12,bevel=False).transform(scale=(1,.67,1))
    # 아랫단 금빛 테는 뺌 — 유령 꼬리 위에 고리처럼 떠 보임 (2026-11-25)
    cylinder('Silhouette neck',(0,0,1.48),.075,.14,'shadow',n=8)
    lump('Faceless head',(0,-.005,1.635),(.285,.26,.32),'shadow',detail=2)
    if woman:
        lathe('Cloche bell hat',[(1.73,.2),(1.8,.17),(1.85,.1)],'shadow',n=12,bevel=False)
        cylinder('Cloche thin rim',(0,0,1.734),.205,.045,'shadow_rim',n=12)
    else:
        lump('Flat cap crown',(0,.005,1.775),(.36,.3,.145),'shadow',detail=2)
        box('Flat cap peak',(0,-.16,1.743),(.32,.21,.055),'shadow',rot=(.08,0,0),bevel=False)
        box('Warm thin cap peak rim',(0,-.265,1.743),(.3,.04,.045),'shadow_rim',bevel=False)
    move_group(start,scale=(s,s,s))
    return s


def shadow_young():
    silhouette_body(1.85)
    tube('Bent arm holding watch',[(.22,0,1.4),(.36,-.09,1.13),(.22,-.31,1.27)],.07,'shadow',n=7)
    rod('Resting left arm',(-.22,0,1.38),(-.29,-.02,.93),.066,'shadow',n=7,bevel=False)
    lump('Watch holding hand',(.2,-.34,1.28),(.14,.12,.14),'shadow')
    # Small open pocket watch tilted toward the viewer at chest height.
    cylinder('Pocket watch brass case',(.18,-.41,1.34),.105,.06,'brass',n=12,rot=(math.pi/2,0,0))
    cylinder('Pocket watch dial',(.18,-.45,1.34),.079,.04,'clock_face',n=12,rot=(math.pi/2,0,0))
    beam('Pocket minute hand',(.18,-.475,1.34),(.18,-.475,1.395),.04,.04,'iron_dark')
    hoop('Open brass watch lid',(.37,-.41,1.34),.1,.04,.045,'brass',n=12,rot=(0,0,-.5))
    tube('Pocket watch chain',[(.18,-.4,1.24),(.13,-.29,1.08),(.05,-.19,1.0),(-.03,-.16,1.12)],.02,'brass',n=5)


def shadow_woman():
    s = silhouette_body(1.7,True)
    start = len(PARTS)
    rod('Woman left arm',(.22,0,1.4),(.35,-.04,.95),.064,'shadow',n=7,bevel=False)
    tube('Woman other arm',[(-.22,0,1.4),(-.33,-.04,1.17),(-.27,-.23,1.3)],.062,'shadow',n=7)
    # Her anatomical left is +X when facing south (-Y).
    box('Leather suitcase',(.39,-.03,.67),(.48,.24,.42),'leather')
    for x in (.23,.55):
        box('Suitcase leather strap',(x,-.16,.67),(.055,.045,.42),'wood_dark',bevel=False)
        box('Suitcase brass clasp',(x,-.19,.75),(.065,.04,.065),'brass',bevel=False)
    tube('Suitcase handle',[(.28,-.03,.9),(.28,-.03,.99),(.49,-.03,.99),(.49,-.03,.9)],.025,'leather',n=6)
    lathe('Knitted neck scarf',[(1.43,.14),(1.52,.145),(1.57,.12)],'scarf',n=12,bevel=False)
    move_group(start,scale=(s,s,s))
    # Thick wavy ribbon: 1.4m centerline, trailing north behind the woman.
    origin = Vector((.07*s,.08*s,1.46*s))
    offsets = [Vector((.1*math.sin(j*.65),j*.112,-.04*j+.08*math.sin(j*.8))) for j in range(13)]
    length = sum((b-a).length for a,b in zip(offsets,offsets[1:]))
    points = [tuple(origin+p*(1.4/length)) for p in offsets]
    verts = [(x+dx,y,z+dz) for dz in (-.025,.025) for x,y,z in points for dx in (-.105,.105)]
    n = len(points)*2
    faces = []
    for j in range(len(points)-1):
        a = 2*j
        faces += [(a,a+2,a+3,a+1),(a+n+1,a+n+3,a+n+2,a+n),
                  (a,a+n,a+n+2,a+2),(a+1,a+3,a+n+3,a+n+1)]
    faces += [(0,1,n+1,n),(n-2,2*n-2,2*n-1,n-1)]
    Part('Wind blown knitted scarf ribbon',verts,faces,'scarf',False)
    for j,(x,y,z) in enumerate(points):
        if j % 2 == 0:
            rod('Raised knitted scarf rib',(x-.1,y,z+.035),(x+.1,y,z+.035),.021,'scarf',n=5,bevel=False)
    x,y,z = points[-1]
    curls = [Vector((.13*math.sin(j*.6),j*.072,.045*math.sin(j*.85))) for j in range(12)]
    length = sum((b-a).length for a,b in zip(curls,curls[1:]))
    thread = [tuple(Vector((x,y,z))+p*(.8/length)) for p in curls]
    tube('Loose curling scarf thread',thread,.02,'scarf',n=6)


SPECS = (
    # name, optional dimensions, centered, builder, triangle budget
    ('Ground', None, False, ground, 40000),
    ('CanopyBay', None, False, canopy_bay, 7000),
    ('GasLamp', None, False, gas_lamp, 3500),
    ('HangClock', None, False, hang_clock, 4500),
    ('TicketGate', None, False, ticket_gate, 9000),
    ('StationWall', None, False, station_wall, 8000),
    ('Fence', None, False, fence, 3000),
    ('ShadowYoung', None, False, shadow_young, 2500),
    ('ShadowWoman', None, False, shadow_woman, 2800),
)




def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min, max)]


def assembled_mesh(name):
    objects = []
    for bevel in (True, False):
        verts, faces, indices = [], [], []
        for p in PARTS:
            if p.bevel != bevel:
                continue
            offset = len(verts)
            verts.extend(p.verts)
            faces.extend(tuple(offset + i for i in f) for f in p.faces)
            indices.extend([ROLES.index(p.role)] * len(p.faces))
        if not verts:
            continue
        data = bpy.data.meshes.new(name + '_parts')
        data.from_pydata(verts, [], faces)
        for role in ROLES:
            data.materials.append(MATS[role])
        for f, index in zip(data.polygons, indices):
            f.material_index = index
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-7, edges=list(bm.edges))
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(name + '_parts', data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Worn edges', 'BEVEL')
            mod.width, mod.segments = .025, 2
            mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(45)
            mod.use_clamp_overlap = True
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
        objects.append(obj)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    return obj


def finish(name, centered, budget):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    if triangles(obj) > budget:
        mod = obj.modifiers.new('Budget', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .96) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Material boundaries can constrain collapse. Recheck the actual result.
    for attempt in range(3):
        if triangles(obj) <= budget:
            break
        mod = obj.modifiers.new('Budget remainder', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .94) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name, triangles(obj), budget)
    lo, hi = bounds(obj)
    mid = (hi + lo) / 2
    # Ground alone retains world coordinates; props are XY-centred and grounded.
    keep_origin = name in ('Ground',)
    if not keep_origin:
        for v in obj.data.vertices:
            v.co.x -= mid.x
            v.co.y -= mid.y
            v.co.z -= mid.z if centered else lo.z
    obj.data.update()
    ghost_fade(obj)
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    destination = OUTPUT / ('Kit_P_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 3) for d in obj.dimensions)}', flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    shading = scene.display.shading
    shading.light, shading.color_type = 'STUDIO', 'MATERIAL'
    shading.show_cavity, shading.show_object_outline = True, True
    shading.object_outline_color = (0,0,0)
    shading.background_type = 'WORLD'
    shading.show_shadows = True
    shading.show_specular_highlight = False
    shading.cavity_type = 'BOTH'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.08, .1, .2)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    # Ground retains its export coordinates but is excluded from the contact sheet.
    for obj in objects:
        obj.hide_render = obj.name == 'Ground'
    objects = [o for o in objects if o.name != 'Ground']
    row1 = [o for o in objects if o.name in ('CanopyBay', 'TicketGate', 'StationWall')]
    row2 = [o for o in objects if o not in row1]
    # Reveal the station facade and keep the canopy's column visible below its roof.
    for obj in objects:
        if obj.name in ('StationWall','Fence'):
            obj.rotation_euler.z = -math.pi/2
        elif obj.name == 'CanopyBay':
            obj.rotation_euler.z = math.pi/2
    bpy.context.view_layer.update()
    def lay(row, y, gap):
        # Object.dimensions are local-axis extents; rotated props need world bounds.
        extents = []
        for obj in row:
            xs = [(obj.matrix_world @ Vector(c)).x for c in obj.bound_box]
            extents.append((min(xs),max(xs)))
        total = sum(hi-lo for lo,hi in extents) + gap * (len(row) - 1)
        x = -total / 2
        for obj,(lo,hi) in zip(row,extents):
            obj.location = (x-lo,y,0)
            x += hi-lo+gap
        return total
    lay(row1, 5, 2)
    lay(row2, -6, 2)
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    a = math.radians(55)
    target = Vector((0,0,0))
    cam.location = target + Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    # Fit transformed bounds in camera space, allowing for the tall post tops.
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    corners = [inv @ (obj.matrix_world @ Vector(c)) for obj in objects for c in obj.bound_box]
    left,right = min(v.x for v in corners),max(v.x for v in corners)
    low,high = min(v.y for v in corners),max(v.y for v in corners)
    center = Vector(((left+right)/2,(low+high)/2,0))
    cam.location += cam.rotation_euler.to_matrix() @ center
    aspect = scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale = max(right-left,(high-low)*aspect)*1.12
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    print('PREVIEW ' + str(PREVIEW), flush=True)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    out = []
    for i, (name, dims, centered, build, budget) in enumerate(SPECS):
        PARTS = []
        random.seed(51150 + i * 977)
        build()
        out.append(finish(name, centered, budget))
    preview(out)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
