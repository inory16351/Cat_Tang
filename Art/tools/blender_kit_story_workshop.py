# 스토리 맵 4 '시계를 멈춘 공방' 키트 — 비 오는 밤 시계공의 공방, 2026-11-25
# 2026-11-26 배색 변경(사용자 확정): 4장 전투 맵(흑백 철 느와르)과 다른 '기억 속 집' — 따뜻한 호두 · 참나무 · 메이플 상감, 크림 회벽, 놋쇠 · 금, 초록 펠트, 포도주 커튼. 숄만 진한 빨강
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_workshop.py
# 회색 역할 재질 FBX(Kit_W_*.fbx) + 미리보기 PNG. Unity(StoryWorkshopBuilder)가 재질 이름으로 색을 입힌다. 명세 Art/tools/specs/kit_story_workshop_spec.md
# Blender (X,Y,Z) = Unity (X,Z,Y); 카메라는 남쪽(-Y)에서 북쪽을 본다. 조각 정면 = -Y.
# 움직이는 조각(바늘 · 진자 · 톱니 · 실린더 · 문자판 칸)은 원점 = 회전축(KEEP). 바늘은 +Z(12시)를 가리키고, 진자는 원점에서 -Z로 매달림.
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryWorkshop'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_workshop_preview.png'
TAU = math.tau
ROLES = (
    'plank', 'plank_dark', 'plank_light', 'plank_seam',
    'wallpaper', 'wallpaper_pat', 'wainscot', 'molding', 'plaster_dark', 'void',
    'wood', 'wood_dark', 'wood_light',
    'iron', 'iron_dark', 'steel', 'steel_dark', 'silver', 'brass', 'brass_dark', 'gold',
    'dial_face', 'dial_mark', 'dial_inlay',
    'red_lacquer', 'red_dark', 'gem_red', 'shawl_red', 'shawl_red_dark', 'velvet_red',
    'curtain', 'curtain_dark', 'sheet', 'sheet_shade', 'rug', 'rug_dark', 'leather', 'paper',
    'glass', 'rain', 'porcelain', 'stone_dark', 'walnut', 'maple', 'felt_green',
    'lantern_glow', 'red_glow', 'moon_glow',
)
HARD = {'wood', 'wood_dark', 'wood_light', 'molding', 'iron', 'iron_dark', 'steel',
        'steel_dark', 'brass', 'brass_dark', 'gold', 'silver', 'stone_dark'}
PALETTE = {  # Preview only (same values as Unity StoryWorkshopPalette); exported materials stay flat grey.
    'plank': (.7,.48,.26), 'plank_dark': (.48,.3,.17), 'plank_light': (.82,.62,.38), 'plank_seam': (.27,.16,.09),
    'wallpaper': (.9,.84,.7), 'wallpaper_pat': (.8,.68,.48), 'wainscot': (.52,.32,.17), 'molding': (.74,.52,.29),
    'plaster_dark': (.22,.13,.07), 'void': (.05,.03,.02),
    'wood': (.66,.44,.24), 'wood_dark': (.36,.21,.11), 'wood_light': (.88,.72,.48),
    'iron': (.32,.23,.16), 'iron_dark': (.17,.11,.07), 'steel': (.8,.76,.68), 'steel_dark': (.44,.37,.3),
    'silver': (.96,.92,.82), 'brass': (.92,.72,.36), 'brass_dark': (.62,.43,.2), 'gold': (1,.82,.36),
    'dial_face': (1,.94,.8), 'dial_mark': (.2,.11,.06), 'dial_inlay': (.86,.74,.54),
    'red_lacquer': (.7,.12,.1), 'red_dark': (.42,.08,.07), 'gem_red': (.9,.16,.14),
    'shawl_red': (.82,.06,.08), 'shawl_red_dark': (.52,.03,.06), 'velvet_red': (.46,.08,.12),
    'curtain': (.42,.08,.14), 'curtain_dark': (.26,.04,.09), 'sheet': (.95,.9,.8), 'sheet_shade': (.78,.7,.58),
    'rug': (.56,.24,.16), 'rug_dark': (.32,.13,.09), 'leather': (.55,.24,.14), 'paper': (.96,.9,.76),
    'glass': (.2,.26,.4), 'rain': (.66,.76,.9), 'porcelain': (.98,.95,.88), 'stone_dark': (.42,.33,.26),
    'walnut': (.3,.17,.09), 'maple': (.93,.8,.58), 'felt_green': (.2,.42,.3),
    'lantern_glow': (1,.82,.5), 'red_glow': (1,.35,.22), 'moon_glow': (.82,.9,1),
}
FONT_FILES = ('C:/Windows/Fonts/BOOKOSB.TTF', 'C:/Windows/Fonts/timesbd.ttf', 'C:/Windows/Fonts/georgiab.ttf')
ROMAN = ('XII', 'I', 'II', 'III', 'IIII', 'V', 'VI', 'VII', 'VIII', 'IX', 'X', 'XI')
MATS = {}
PARTS = []
FONT = None

# 기준점 (블렌더 좌표, 유니티 로컬 = (x, z, y)) — StoryWorkshopBuilder.cs와 같은 값
ANCHORS = {
    'GrandClock': {'hands': (0, -.65, 2.1), 'pendulum': (0, -.05, 1.2)},
    'WallClock1': {'hands': (0, -.37, 2.75), 'pendulum': (0, -.26, 1.92), 'hand_scale': 1.44, 'pend_scale': .6},
    'WallClock2': {'hands': (0, -.5, 3.15), 'pendulum': (0, -.27, 2.55), 'hand_scale': 1.0, 'pend_scale': 1.25},
    'WallClock3': {'hands': (0, -.55, 2.62), 'pendulum': (0, -.33, 2.0), 'hand_scale': 1.0, 'pend_scale': .85},
    'WallClock4': {'hands': (0, -.35, 2.85), 'pendulum': (0, -.25, 2.1), 'hand_scale': 1.2, 'pend_scale': .5},
    'MusicBox': {'cylinder': (-.17, .02, .24)},
    'GearStand': {'axle': (0, 0, 1.25)},
}


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
    """Batch solid components before the shared 0.02m/two-segment bevel."""

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
    """Solid polygon in XZ; extruded along Y around y."""
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


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), top=None, bevel=True):
    return lathe(label, [(-depth/2, radius),
                        (depth/2, radius if top is None else top)], role, n, bevel).transform(loc, rot)


def ycyl(label, loc, radius, depth, role, n=16, bevel=False):
    """Disc whose axis is Y (faces the camera side -Y)."""
    return cylinder(label, loc, radius, depth, role, n, rot=(math.pi/2,0,0), bevel=bevel)


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


def rod(label, a, b, radius, role, n=6, bevel=False):
    return tube(label, [a,b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role, bevel=True):
    a, b = Vector(a), Vector(b)
    part = box(label, (0,0,0), (width,depth,(b-a).length), role, bevel=bevel)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def hoop(label, loc, radius, band, depth, role, n=24, rot=(0,0,0), bevel=False):
    """Open ring in XZ, axis Y."""
    verts = [(r*math.cos(j*TAU/n), y, r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role, bevel).transform(loc, rot)


def flat_hoop(label, loc, radius, band, thick, role, n=32):
    """Horizontal ring lying on the floor (axis Z)."""
    return hoop(label, loc, radius, band, thick, role, n, rot=(math.pi/2,0,0))


def sector(label, inner, outer, start, end, depth, role, y=0, steps=4, bevel=True):
    angles = [start+(end-start)*j/steps for j in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    return extrude(label, polygon, depth, role, y, bevel)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.93,1.07)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    result = Part(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role, False)
    bm.free()
    return result


def ball(label, loc, r, role, detail=2):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=r)
    verts = [tuple(Vector(loc)+v.co) for v in bm.verts]
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return Part(label, verts, faces, role, False)


def scroll(label, loc, radius=.3, flip=1, role='gold', y=0):
    x,_,z = loc
    points = []
    for j in range(11):
        t = j/10
        a, r = -.8+t*math.pi*1.9, radius*(1-.73*t)
        points.append((x+flip*r*math.cos(a),loc[1],z+r*math.sin(a)))
    return tube(label, points, max(.022, radius*.12), role, n=5)


def numeral(text, loc, height=.28, role='gold', angle=0, depth=.05):
    """Bold mesh bars, no font dependency. I, V and X silhouettes in XZ."""
    start = len(PARTS)
    pitch = height*.45
    for i, char in enumerate(text):
        x = (i-(len(text)-1)/2)*pitch
        w = max(.03,height*.15)
        if char == 'I':
            box('Numeral I', (x,0,0), (w,depth,height), role, bevel=False)
        elif char == 'X':
            for s in (-1,1):
                beam('Numeral X', (x-s*height*.18,0,-height/2),
                     (x+s*height*.18,0,height/2), w,depth,role,bevel=False)
        elif char == 'V':
            for s in (-1,1):
                beam('Numeral V', (x,0,-height/2),
                     (x+s*height*.21,0,height/2), w,depth,role,bevel=False)
    move_group(start, loc, (0,angle,0))


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


def circle_poly(cx, cy, r, n=32, ccw=True):
    s = 1 if ccw else -1
    return [(cx+r*math.cos(s*j*TAU/n), cy+r*math.sin(s*j*TAU/n)) for j in range(n)]


def disc(label, xy, r, top, thick, role, n=32):
    return slab(label, circle_poly(xy[0], xy[1], r, n), top, thick, role)


def annulus(label, xy, r0, r1, top, thick, role, n=64):
    for j in range(n):
        a, b = j*TAU/n, (j+1)*TAU/n
        slab(label, [(xy[0]+r0*math.cos(a), xy[1]+r0*math.sin(a)), (xy[0]+r1*math.cos(a), xy[1]+r1*math.sin(a)),
                     (xy[0]+r1*math.cos(b), xy[1]+r1*math.sin(b)), (xy[0]+r0*math.cos(b), xy[1]+r0*math.sin(b))],
             top, thick, role)


def lozenge(label, xy, length, width, angle, top, thick, role):
    c, s = math.cos(angle), math.sin(angle)
    pts = [(length/2,0),(0,width/2),(-length/2,0),(0,-width/2)]
    return slab(label, [(xy[0]+x*c-y*s, xy[1]+x*s+y*c) for x,y in pts], top, thick, role)


def halfplane(poly, a, b, right=True):
    if not poly:
        return []
    def distance(p):
        return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])
    out, p = [], poly[-1]
    dp = distance(p)
    for q in poly:
        dq = distance(q)
        ip = dp <= 1e-9 if right else dp >= -1e-9
        iq = dq <= 1e-9 if right else dq >= -1e-9
        if ip != iq:
            t = dp/(dp-dq)
            out.append((p[0]+t*(q[0]-p[0]), p[1]+t*(q[1]-p[1])))
        if iq:
            out.append(q)
        p, dp = q, dq
    return out


def clip_convex(poly, clipper):
    """Clip poly by a convex CCW clipper polygon."""
    for i, a in enumerate(clipper):
        poly = halfplane(poly, a, clipper[(i+1) % len(clipper)], right=False)
        if len(poly) < 3:
            return []
    return poly


def outside_circle(poly, c, r, n=64):
    """Pieces of convex poly outside the circle (clockwise circle edges, like the lake clearing)."""
    circle = [(c[0]+r*math.cos(-j*TAU/n), c[1]+r*math.sin(-j*TAU/n)) for j in range(n)]
    pieces, remain = [], poly
    for k,a in enumerate(circle):
        b = circle[(k+1) % n]
        out = halfplane(remain,a,b,False)
        if len(out) >= 3:
            pieces.append(out)
        remain = halfplane(remain,a,b)
        if len(remain) < 3:
            break
    return pieces


def ring_skin(label, rings, role, cap_top=True, cap_bottom=True):
    """Closed skin through rings of equal point counts (bottom to top)."""
    n = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []
    if cap_bottom:
        faces.append(tuple(reversed(range(n))))
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    if cap_top:
        faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, False)


def frame(label, cx, cz, w, h, y, bar, depth, role, bevel=True):
    """Rectangular molding frame in XZ (centre cx,cz)."""
    box(label, (cx, y, cz+h/2-bar/2), (w, depth, bar), role, bevel=bevel)
    box(label, (cx, y, cz-h/2+bar/2), (w, depth, bar), role, bevel=bevel)
    box(label, (cx-w/2+bar/2, y, cz), (bar, depth, h-2*bar), role, bevel=bevel)
    box(label, (cx+w/2-bar/2, y, cz), (bar, depth, h-2*bar), role, bevel=bevel)


# ---------------------------------------------------------------- text numerals
def load_font():
    global FONT
    for path in FONT_FILES:
        if Path(path).is_file():
            try:
                FONT = bpy.data.fonts.load(path)
                print('FONT ' + path, flush=True)
                return
            except Exception as exc:
                print('font failed', path, exc)
    FONT = None


def text_mesh(text, offset=0.0, extrude=.5):
    cu = bpy.data.curves.new('num_text', 'FONT')
    cu.body = text
    if FONT:
        cu.font = FONT
    cu.align_x, cu.align_y = 'CENTER', 'CENTER'
    cu.size, cu.extrude, cu.offset = 1.0, extrude, offset
    cu.resolution_u = 3
    ob = bpy.data.objects.new('num_text', cu)
    bpy.context.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [v.co.copy() for v in me.vertices]
    faces = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    return verts, faces


def text_numeral(text, height, top_role, rim_role='walnut'):
    """Flat Arabic numeral lying on the floor, readable from the south. Origin centre, bottom z 0."""
    base_v, base_f = text_mesh(text, 0.0)
    xs = [v.x for v in base_v]; ys = [v.y for v in base_v]
    s = height/(max(ys)-min(ys))
    cx, cy = (max(xs)+min(xs))/2, (max(ys)+min(ys))/2
    rim_v, rim_f = text_mesh(text, .035)
    def place(vs, z0, thick):
        return [((v.x-cx)*s, (v.y-cy)*s, z0+(v.z+.5)*thick) for v in vs]
    Part('Gold numeral outline', place(rim_v, 0, .03), rim_f, rim_role, False)
    Part('Raised numeral', place(base_v, .02, .045), base_f, top_role, False)


# ---------------------------------------------------------------- clocks
def dial(cx, cz, yf, r, face='dial_face', bezel='brass', back='wood_dark', mark='dial_mark',
         ring2='gold', ticks=60, roman=True):
    """Clock face whose enamel front is the plane y = yf, facing -Y."""
    start = len(PARTS)
    ycyl('Dial back plate', (0, .085, 0), r*1.1, .14, back, n=32)
    ycyl('Enamel dial face', (0, 0, 0), r*.95, .04, face, n=32)
    hoop('Dial bezel', (0, -.035, 0), r*1.04, r*.13, .1, bezel, n=32)
    hoop('Dial inner bead', (0, -.035, 0), r*.9, max(.025, r*.03), .03, ring2, n=32)
    hoop('Chapter ring line', (0, -.028, 0), r*.79, max(.02, r*.018), .02, mark, n=32)
    for j in range(ticks):
        a = j*TAU/ticks
        major = j % max(1, ticks//12) == 0
        length = r*(.1 if major else .055)
        box('Minute tick', ((r*.85-length/2)*math.sin(a), -.03, (r*.85-length/2)*math.cos(a)),
            (max(.025, r*(.045 if major else .02)), .02, length), mark, rot=(0,a,0), bevel=False)
    if roman:
        for h in range(12):
            a = h*TAU/12
            numeral(ROMAN[h], (r*.64*math.sin(a), -.035, r*.64*math.cos(a)), height=r*.17,
                    role=mark, angle=a, depth=.03)
    ycyl('Dial centre boss', (0, -.05, 0), max(.03, r*.06), .05, ring2, n=10)
    move_group(start, (cx, yf, cz))


def baked_hands(cx, cz, y, r, hour, minute, role='iron_dark'):
    for frac, length, width, yy in ((((hour % 12)+minute/60)/12, .5, .05, 0), (minute/60, .75, .035, -.02)):
        a = frac*TAU
        beam('Stopped clock hand', (cx, y+yy, cz), (cx+r*length*math.sin(a), y+yy, cz+r*length*math.cos(a)),
             max(.03, r*width), .02, role, bevel=False)


def hand_hour(role, s=1.0, ornate=False):
    poly = [(-.022,-.07),(.022,-.07),(.022,.15),(.065,.19),(0,.3),(-.065,.19),(-.022,.15)]
    extrude('Hour hand spade', [(x*s,z*s) for x,z in poly], .024*s, role, y=-.014*s, bevel=False)
    hoop('Hour hand moon ring', (0,-.014*s,.205*s), .058*s, .022*s, .026*s, role, n=14)
    ycyl('Hour hand hub', (0,-.014*s,0), .042*s, .03*s, 'gold', n=12)
    if ornate:
        for side in (-1,1):
            scroll('Hour hand filigree', (side*.035*s, -.03*s, .1*s), .035*s, side, 'gold')


def hand_minute(role, s=1.0, tail='gold', ornate=False):
    poly = [(-.017,-.11),(.017,-.11),(.015,.37),(0,.44),(-.015,.37)]
    extrude('Minute hand blade', [(x*s,z*s) for x,z in poly], .02*s, role, y=-.04*s, bevel=False)
    ycyl('Minute hand counterweight', (0,-.04*s,-.11*s), .042*s, .026*s, tail, n=12)
    ycyl('Minute hand cap', (0,-.055*s,0), .032*s, .035*s, 'gold', n=12)
    if ornate:
        hoop('Minute hand ring', (0,-.04*s,.25*s), .035*s, .016*s, .022*s, 'gold', n=12)


def pendulum_s():
    beam('Pendulum rod', (0,0,-.02), (0,0,-.8), .035, .025, 'steel_dark', bevel=False)
    box('Pendulum suspension block', (0,0,-.03), (.075,.045,.08), 'brass', bevel=False)
    ycyl('Pendulum bob', (0,0,-.88), .15, .05, 'brass', n=20)
    hoop('Pendulum bob ring', (0,-.005,-.88), .15, .028, .065, 'silver', n=20)
    ycyl('Pendulum bob boss', (0,-.035,-.88), .045, .03, 'gold', n=10)


def grand_pendulum():
    box('Grand suspension', (0,0,-.03), (.1,.05,.08), 'brass', bevel=False)
    for side in (-1,1):
        tube('Lyre rod', [(side*.02,0,-.05),(side*.11,0,-.2),(side*.12,0,-.38),(side*.03,0,-.58)],
             .018, 'steel_dark', n=5)
        scroll('Lyre scroll', (side*.1,-.01,-.3), .06, side, 'gold')
    for z in (-.12,-.45):
        box('Lyre crossbar', (0,0,z), (.2,.03,.03), 'gold', bevel=False)
    beam('Grand pendulum rod', (0,0,-.05), (0,0,-.62), .03, .02, 'brass_dark', bevel=False)
    ycyl('Grand pendulum bob', (0,0,-.8), .2, .06, 'brass', n=24)
    hoop('Grand bob ring', (0,-.01,-.8), .2, .035, .075, 'gold', n=24)
    for j in range(12):
        a = j*TAU/12
        beam('Bob sunburst ray', (.05*math.sin(a),-.035,-.8+.05*math.cos(a)),
             (.15*math.sin(a),-.035,-.8+.15*math.cos(a)), .028, .015, 'silver', bevel=False)
    ball('Bob red gem', (0,-.045,-.8), .045, 'gem_red', detail=1)


def display_board(width=1.9, height=3.95):
    """Free-standing clock display panel (wall clock backboard). Front surface y = -0.12."""
    box('Board plinth foot', (0,-.12,.13), (width+.1,.5,.26), 'wood_dark')
    box('Plinth top molding', (0,-.14,.28), (width+.14,.54,.05), 'molding')
    box('Display board', (0,-.06,(height+.26)/2), (width,.12,height-.26), 'wood_dark')
    box('Patterned panel', (0,-.125,(height+.3)/2), (width-.36,.02,height-.75), 'wallpaper_pat', bevel=False)
    frame('Board molding frame', 0, (height+.3)/2, width-.16, height-.6, -.14, .09, .04, 'molding')
    frame('Board gold bead', 0, (height+.3)/2, width-.4, height-.82, -.15, .035, .02, 'gold')
    for side in (-1,1):
        for z in (.6, height-.35):
            scroll('Board corner scroll', (side*(width/2-.32),-.16,z), .1, side, 'gold')
    extrude('Board arched crest', [(-width/2+.1,height),(width/2-.1,height),(width/2-.35,height+.22),
            (0,height+.38),(-width/2+.35,height+.22)], .12, 'wood_dark', y=-.06)
    ball('Crest finial', (0,-.1,height+.46), .08, 'gold')
    box('Brass nameplate', (0,-.15,.55), (.42,.03,.13), 'brass', bevel=False)
    box('Nameplate engraved line', (0,-.168,.55), (.28,.01,.03), 'dial_mark', bevel=False)


def wall_clock_1():
    display_board()
    a = ANCHORS['WallClock1']
    cz = a['hands'][2]
    hoop('Round case carved ring', (0,-.22,cz), .84, .14, .2, 'wood', n=32)
    hoop('Round case dark lip', (0,-.3,cz), .76, .05, .06, 'wood_dark', n=32)
    dial(0, cz, -.32, .72, ring2='gold')
    for side in (-1,1):
        scroll('Round clock crest scroll', (side*.25,-.22,cz+.92), .17, side, 'gold')
    ball('Round clock crest pearl', (0,-.24,cz+.98), .07, 'silver')
    # Drop box under the round case: open window where the pendulum swings
    pz = a['pendulum'][2]
    box('Drop box back', (0,-.13,pz-.35), (.72,.04,.75), 'curtain_dark', bevel=False)
    for side in (-1,1):
        box('Drop box side', (side*.34,-.2,pz-.35), (.06,.18,.75), 'wood_dark')
    box('Drop box bottom', (0,-.2,pz-.74), (.72,.18,.06), 'wood_dark')
    extrude('Drop box point', [(-.36,0),(.36,0),(0,-.22)], .18, 'wood', y=-.2).transform((0,0,pz-.76))
    frame('Drop window gold frame', 0, pz-.36, .64, .7, -.3, .04, .03, 'gold')
    ball('Drop pendant', (0,-.2,pz-1.02), .05, 'gem_red')


def wall_clock_2():
    display_board()
    a = ANCHORS['WallClock2']
    cz, pz = a['hands'][2], a['pendulum'][2]
    box('Regulator back', (0,-.14,2.33), (1.0,.04,2.85), 'curtain_dark', bevel=False)
    for side in (-1,1):
        box('Regulator side', (side*.45,-.27,2.33), (.1,.3,2.85), 'wood_dark')
        cylinder('Regulator turned column', (side*.42,-.44,1.8), .045, 1.5, 'brass', n=8, bevel=False)
        for z in (1.03,2.57):
            box('Column capital', (side*.42,-.44,z), (.12,.08,.06), 'gold', bevel=False)
    box('Regulator base', (0,-.27,.98), (1.0,.32,.16), 'wood_dark')
    extrude('Regulator base drop', [(-.5,0),(.5,0),(.2,-.18),(0,-.24),(-.2,-.18)], .3, 'wood', y=-.27).transform((0,0,.9))
    box('Regulator mid rail', (0,-.27,2.62), (1.0,.32,.1), 'molding')
    box('Regulator hood', (0,-.27,3.2), (1.0,.32,1.05), 'wood_dark')
    frame('Pendulum window gold frame', 0, 1.82, .78, 1.5, -.43, .04, .03, 'gold')
    dial(0, cz, -.45, .5, ring2='gold')
    extrude('Regulator pediment', [(-.56,3.72),(.56,3.72),(0,4.05)], .34, 'wood_dark', y=-.27)
    hoop('Pediment red roundel', (0,-.45,3.84), .07, .03, .03, 'red_lacquer', n=12)
    ball('Regulator finial', (0,-.3,4.1), .07, 'gold')
    for side in (-1,1):
        ball('Regulator corner finial', (side*.5,-.3,3.78), .05, 'gold')


def wall_clock_3():
    display_board()
    a = ANCHORS['WallClock3']
    cz = a['hands'][2]
    box('Cuckoo house body', (0,-.3,2.65), (1.4,.36,1.2), 'wood')
    for z in (2.1,3.2):
        box('House trim board', (0,-.49,z), (1.42,.04,.08), 'wood_dark', bevel=False)
    for side in (-1,1):
        beam('Pitched roof slab', (side*.82,-.3,3.15), (0,-.3,3.85), .14, .5, 'wood_dark')
        for j in range(5):
            t = (j+.5)/5
            box('Carved roof edge tooth', (side*.82*(1-t),-.56,3.15+.7*t-.08), (.08,.03,.12), 'wood_light',
                rot=(0,side*.7,0), bevel=False)
        lump('Carved leaf garland', (side*.62,-.5,2.62), (.22,.08,.5), 'wood_dark')
        for j in range(3):
            lump('Carved oak leaf', (side*.55,-.52,2.25+j*.32), (.16,.06,.18), 'wood_light')
    extrude('Gable front', [(-.6,3.25),(.6,3.25),(0,3.72)], .04, 'wood_light', y=-.49)
    box('Cuckoo door', (0,-.52,3.43), (.18,.03,.2), 'wood_dark', bevel=False)
    lump('Cuckoo bird', (0,-.6,3.42), (.12,.16,.1), 'paper')
    lump('Cuckoo red beak', (0,-.68,3.43), (.04,.06,.03), 'red_lacquer')
    lump('Carved bird on roof', (0,-.32,3.95), (.32,.12,.18), 'wood_light')
    dial(0, cz, -.5, .5, bezel='gold', ring2='brass')
    pz = a['pendulum'][2]
    for side in (-1,1):
        x = side*.36
        points = [(x,-.32,2.05-j*.06) for j in range(13)]
        tube('Weight chain', points, .02, 'iron_dark', n=4)
        lathe('Pine cone weight', [(0,.02),(.08,.09),(.3,.1),(.42,.05),(.46,.02)], 'iron_dark', n=8,
              bevel=False).transform((x,-.32,1.25))
        cylinder('Weight brass cap', (x,-.32,1.73), .06, .05, 'brass', n=8, bevel=False)
    box('Pendulum guide', (0,-.32,pz+.02), (.1,.1,.08), 'brass', bevel=False)


def wall_clock_4():
    display_board()
    a = ANCHORS['WallClock4']
    cz, pz = a['hands'][2], a['pendulum'][2]
    octagon = [(.82*math.cos(j*TAU/8+TAU/16), .82*math.sin(j*TAU/8+TAU/16)) for j in range(8)]
    extrude('Octagon case', [(x,cz+z) for x,z in octagon], .2, 'wood_dark', y=-.2)
    for j in range(8):
        p, q = octagon[j], octagon[(j+1) % 8]
        beam('Octagon gold bead', (p[0]*.92,-.31,cz+p[1]*.92), (q[0]*.92,-.31,cz+q[1]*.92), .04, .03, 'gold', bevel=False)
        box('Octagon brass rivet', (p[0]*.84,-.31,cz+p[1]*.84), (.05,.03,.05), 'silver', bevel=False)
    dial(0, cz, -.3, .6, ring2='gold', back='wood')
    box('Drop box back', (0,-.13,pz-.3), (.8,.04,.66), 'curtain_dark', bevel=False)
    for side in (-1,1):
        box('Drop box side', (side*.38,-.22,pz-.3), (.06,.2,.66), 'wood_dark')
    extrude('Drop box bottom point', [(-.41,0),(.41,0),(.15,-.16),(0,-.2),(-.15,-.16)], .22, 'wood', y=-.22).transform((0,0,pz-.62))
    frame('Drop window frame', 0, pz-.3, .7, .58, -.33, .045, .03, 'gold')
    box('School clock red band', (0,-.33,pz-.63), (.6,.02,.04), 'red_lacquer', bevel=False)


def grand_clock():
    h = ANCHORS['GrandClock']
    cz, pz = h['hands'][2], h['pendulum'][2]
    # Plinth
    box('Plinth', (0,0,.14), (1.9,.95,.28), 'wood_dark')
    box('Plinth base molding', (0,0,.04), (2.02,1.07,.08), 'molding')
    box('Plinth top molding', (0,-.01,.27), (1.96,1.0,.05), 'molding')
    for side in (-1,1):
        box('Plinth brass corner', (side*.94,-.47,.14), (.07,.07,.24), 'brass', bevel=False)
    lozenge('Plinth red diamond', (0,0), .3, .14, 0, 0, .01, 'red_lacquer').transform((0,0,0), (math.pi/2,0,0))
    move_group(len(PARTS)-1, (0,-.48,.14))
    # Trunk (open pendulum recess)
    box('Trunk back panel', (0,.33,.78), (1.45,.12,1.0), 'wood_dark')
    box('Trunk velvet back', (0,.26,.78), (1.05,.03,.92), 'curtain_dark', bevel=False)
    for side in (-1,1):
        box('Trunk side', (side*.62,0,.78), (.21,.8,1.0), 'wood_dark')
        box('Trunk side raised panel', (side*.73,0,.78), (.03,.55,.75), 'wood', bevel=False)
        cylinder('Trunk turned column', (side*.6,-.43,.78), .055, .96, 'brass', n=8, bevel=False)
        for z in (.32,1.24):
            box('Trunk column capital', (side*.6,-.43,z), (.14,.1,.06), 'gold', bevel=False)
    frame('Trunk window gold bead', 0, .76, 1.04, .92, -.41, .045, .03, 'gold')
    # Waist and hood
    box('Waist molding', (0,0,1.3), (1.72,.98,.1), 'molding')
    box('Waist gold bead', (0,-.5,1.26), (1.7,.03,.04), 'gold', bevel=False)
    box('Hood', (0,0,2.18), (2.12,.95,1.7), 'wood_dark')
    box('Hood red lacquer band', (0,-.485,1.4), (2.0,.02,.05), 'red_lacquer', bevel=False)
    dial(0, cz, -.6, .82, ring2='gold')
    for side in (-1,1):
        cylinder('Hood column', (side*1.0,-.47,2.18), .075, 1.62, 'brass', n=10, bevel=False)
        for z in (1.4,2.96):
            box('Hood column capital', (side*1.0,-.47,z), (.2,.14,.08), 'gold', bevel=False)
        for z in (cz-.68, cz+.68):
            scroll('Dial spandrel scroll', (side*.72,-.5,z), .14, side, 'gold')
    # Cornice, swan-neck pediment, finials
    box('Cornice', (0,-.02,3.12), (2.34,1.06,.18), 'molding')
    for j in range(17):
        box('Cornice dentil', (-1.04+j*.13,-.56,3.0), (.07,.05,.08), 'gold', bevel=False)
    for side in (-1,1):
        tube('Swan neck pediment', [(side*1.0,-.3,3.2),(side*.85,-.3,3.5),(side*.55,-.3,3.66),(side*.32,-.3,3.6)],
             .08, 'wood_dark', n=6)
        scroll('Swan neck rosette', (side*.3,-.4,3.55), .12, side, 'gold')
        ball('Corner finial', (side*1.08,-.3,3.3), .09, 'gold')
    lathe('Centre urn finial', [(0,.08),(.1,.16),(.25,.12),(.34,.05),(.5,.03)], 'gold', n=10,
          bevel=False).transform((0,-.3,3.22))
    ball('Urn red gem', (0,-.42,3.42), .055, 'gem_red', detail=1)
    box('Stopped-time plate', (0,-.5,3.12), (.5,.02,.1), 'dial_face', bevel=False)


def window():
    # Origin = opening bottom centre at floor, inner wall face y=0, room toward -Y.
    for side in (-1,1):
        box('Jamb liner', (side*1.07,.3,2.55), (.06,.6,2.9), 'wood')
        box('Side casing', (side*1.22,-.04,2.62), (.2,.08,3.25), 'wood_dark')
    box('Head liner', (0,.3,3.98), (2.2,.6,.06), 'wood')
    box('Head casing', (0,-.05,4.18), (2.8,.1,.24), 'molding')
    box('Keystone', (0,-.11,4.18), (.24,.06,.32), 'gold', bevel=False)
    box('Window sill', (0,-.06,1.08), (2.6,.44,.07), 'wood')
    box('Sill apron', (0,-.03,.95), (2.3,.05,.18), 'molding')
    box('Night glass', (0,.33,2.55), (2.12,.03,2.86), 'glass', bevel=False)
    ycyl('Moon through the glass', (-.45,.305,3.35), .3, .02, 'moon_glow', n=24)
    hoop('Moon halo', (-.45,.3,3.35), .45, .04, .02, 'moon_glow', n=24)
    box('Centre mullion', (0,.29,2.55), (.06,.06,2.86), 'iron_dark', bevel=False)
    for z in (1.85,2.6,3.35):
        box('Glazing bar', (0,.29,z), (2.12,.06,.06), 'iron_dark', bevel=False)
    for j in range(16):
        x = -1.0+j*.133+random.uniform(-.03,.03)
        z0 = random.uniform(3.2,3.95); z1 = random.uniform(1.15,2.6)
        pts = [(x+.03*math.sin(k*1.7+j), .27, z0-(z0-z1)*k/5) for k in range(6)]
        tube('Rain streak on glass', pts, .022, 'rain', n=4)
        lump('Rain drop', pts[-1], (.06,.03,.08), 'rain')
    # Curtains gathered at tiebacks
    for side in (-1,1):
        top = (side*.85, side*1.85); tie = (side*1.33, side*1.62); bottom = (side*1.0, side*1.9)
        cols = 9
        for i in range(cols):
            t0, t1 = i/cols, (i+1)/cols
            def at(t, z):
                if z >= 1.75:
                    k = (z-1.75)/(4.3-1.75); a, b = tie[0]+(top[0]-tie[0])*k, tie[1]+(top[1]-tie[1])*k
                else:
                    k = (1.75-z)/1.65; a, b = tie[0]+(bottom[0]-tie[0])*k, tie[1]+(bottom[1]-tie[1])*k
                x = a+(b-a)*t
                gather = 1.0 if z >= 1.75 else .5
                y = -.16-.07*math.sin(t*cols*math.pi)*gather - (.04 if abs(z-1.75) < .3 else 0)
                return (x, y, z)
            zs = [4.3, 3.6, 2.9, 2.2, 1.75, 1.2, .6, .08]
            L = [at(t0, z) for z in zs]; R = [at(t1, z) for z in zs]
            verts = L+R+[(x,y+.05,z) for x,y,z in L]+[(x,y+.05,z) for x,y,z in R]
            n = len(zs)
            faces = []
            for k in range(n-1):
                faces += [(k, k+1, n+k+1, n+k), (2*n+k, 3*n+k, 3*n+k+1, 2*n+k+1),
                          (k, 2*n+k, 2*n+k+1, k+1), (n+k, n+k+1, 3*n+k+1, 3*n+k)]
            faces += [(0, n, 3*n, 2*n), (n-1, 2*n+n-1, 3*n+n-1, 2*n-1)]
            Part('Velvet curtain fold', verts, faces, 'curtain' if i % 2 == 0 else 'curtain_dark', False)
        box('Curtain hem band', (side*1.45,-.17,.12), (.95,.09,.1), 'curtain_dark', bevel=False)
        hoop('Gold tieback cord', (side*1.48,-.17,1.75), .2, .04, .05, 'gold', n=12, rot=(math.pi/2,0,0))
        lump('Tieback tassel', (side*1.68,-.26,1.5), (.1,.1,.3), 'gold')
        ball('Rod finial', (side*2.05,-.2,4.32), .08, 'gold')
    rod('Curtain rod', (-2.0,-.2,4.32), (2.0,-.2,4.32), .04, 'brass', n=8)
    box('Pelmet', (0,-.22,4.55), (4.3,.14,.36), 'wood_dark')
    box('Pelmet gold bead', (0,-.3,4.42), (4.25,.03,.04), 'gold', bevel=False)


# ---------------------------------------------------------------- floor & room
ROOM_X, ROOM_Y = 13.0, 11.0
DIAL_C = (0.0, 0.5)
DIAL_R = 5.6
TILE_R, TILE_RING = 1.0, 4.1


def ground():
    box('Outer void', (0,0,-.65), (100,100,.1), 'void', bevel=False)
    slab('Floor underlay', [(-13.7,-11.7),(13.7,-11.7),(13.7,11.7),(-13.7,11.7)], -.03, .5, 'plank_dark')
    rect = [(-ROOM_X,-ROOM_Y),(ROOM_X,-ROOM_Y),(ROOM_X,ROOM_Y),(-ROOM_X,ROOM_Y)]
    seeds = []
    for i in range(9):
        for j in range(8):
            seeds.append((-13+i*3.25+random.uniform(-1.1,1.1), -11+j*3.15+random.uniform(-1.1,1.1)))
    roles = ('plank', 'plank_dark', 'plank_light')
    for si, s in enumerate(seeds):
        cell = list(rect)
        for t in seeds:
            if t is s:
                continue
            if math.hypot(t[0]-s[0], t[1]-s[1]) > 9:
                continue
            m = ((s[0]+t[0])/2, (s[1]+t[1])/2)
            d = (-(t[1]-s[1]), t[0]-s[0])
            cell = halfplane(cell, m, (m[0]+d[0], m[1]+d[1]), right=False)
            if len(cell) < 3:
                break
        if len(cell) < 3:
            continue
        role = roles[(si*7 + si//8) % 3]
        angle = (0, math.pi/2)[(si*5+si//8) % 2]
        pieces = outside_circle(cell, DIAL_C, DIAL_R-.04)
        for piece in pieces:
            slab('Plank chunk', piece, 0, .1, role)
        # Plank seams of this chunk (one quiet pattern per chunk)
        c, sn = math.cos(angle), math.sin(angle)
        xs = [p[0]*c+p[1]*sn for p in cell]; ys = [-p[0]*sn+p[1]*c for p in cell]
        v = min(ys)+.34
        row = 0
        while v < max(ys):
            u0, u1 = min(xs)-1, max(xs)+1
            strip = [(u0, v-.018), (u1, v-.018), (u1, v+.018), (u0, v+.018)]
            strip = [(a*c-b*sn, a*sn+b*c) for a,b in strip]
            for piece in pieces:
                cut = clip_convex(strip, piece)
                if len(cut) >= 3:
                    slab('Plank seam', cut, .006, .02, 'plank_seam')
            # staggered butt joints
            for k in range(-6, 7):
                u = (min(xs)+max(xs))/2 + k*1.9 + (row % 2)*.95
                if u < min(xs) or u > max(xs):
                    continue
                joint = [(u-.018, v), (u+.018, v), (u+.018, v+.34), (u-.018, v+.34)]
                joint = [(a*c-b*sn, a*sn+b*c) for a,b in joint]
                for piece in pieces:
                    cut = clip_convex(joint, piece)
                    if len(cut) >= 3:
                        slab('Plank butt joint', cut, .011, .02, 'plank_seam')   # 이음 선(.006)과 같은 높이면 교차점이 깜빡임 → 5mm 위
            v += .34
            row += 1


def wall_local(u0, u1, H, openings=(), pilasters=(), low=False, halo=False):
    """Wall along X, inner face y=0, room toward -Y, body y 0..0.6."""
    T = .6
    L = u1-u0
    mid = (u0+u1)/2
    # core with openings (c, w, z0, z1)
    edges = [u0]
    for c, w, z0, z1 in sorted(openings):
        edges += [c-w/2, c+w/2]
    edges.append(u1)
    for k in range(0, len(edges), 2):
        a, b = edges[k], edges[k+1]
        if b-a > .01:
            box('Wall core', ((a+b)/2, T/2, H/2), (b-a, T, H), 'wallpaper', bevel=False)
    for c, w, z0, z1 in openings:
        box('Wall under opening', (c, T/2, z0/2), (w, T, z0), 'wallpaper', bevel=False)
        box('Wall over opening', (c, T/2, (z1+H)/2), (w, T, H-z1), 'wallpaper', bevel=False)
    # wainscot
    box('Skirting board', (mid,-.035,.1), (L,.07,.2), 'wood_dark', bevel=False)
    box('Wainscot band', (mid,-.02,.6), (L,.04,1.0), 'wainscot', bevel=False)
    n = max(1, int(L/1.25))
    pw = L/n
    for j in range(n):
        cx = u0+pw*(j+.5)
        frame('Wainscot panel molding', cx, .62, pw-.22, .66, -.05, .05, .03, 'molding', False)
        box('Wainscot raised panel', (cx,-.045,.62), (pw-.42,.02,.46), 'wood_dark', bevel=False)
    box('Chair rail', (mid,-.05,1.12), (L,.1,.1), 'molding', bevel=False)
    box('Chair rail gold bead', (mid,-.105,1.06), (L,.03,.035), 'gold', bevel=False)
    if low:
        box('Low wall cap', (mid,T/2-.05,1.2), (L,T+.14,.08), 'molding', bevel=False)
        return
    # damask lozenges
    def blocked(u, z):
        for c, w, z0, z1 in openings:
            if abs(u-c) < w/2+.45 and z > z0-.3:
                return True
        for p in pilasters:
            if abs(u-p) < .5:
                return True
        if halo and abs(u) < 1.9:
            return True
        return False
    row = 0
    z = 1.7
    while z < H-.95:
        u = u0+.5+(row % 2)*.39
        while u < u1-.4:
            if not blocked(u, z):
                box('Damask lozenge', (u,-.01,z), (.2,.02,.2), 'wallpaper_pat', rot=(0,math.pi/4,0), bevel=False)
            u += .78
        z += .78*.66
        row += 1
    # crown
    box('Crown molding', (mid,-.09,H-.48), (L,.18,.16), 'molding', bevel=False)
    box('Crown shadow band', (mid,-.05,H-.33), (L,.1,.12), 'wainscot', bevel=False)
    u = u0+.15
    while u < u1-.1:
        box('Gold dentil', (u,-.04,H-.62), (.08,.06,.1), 'gold', bevel=False)
        u += .26
    box('Wall top cap', (mid,T/2-.05,H+.02), (L,T+.1,.08), 'plaster_dark', bevel=False)   # 벽 몸체 윗면(H)과 겹치지 않게 위로 (z-fighting 처마, 2026-11-26)
    for p in pilasters:
        box('Pilaster shaft', (p,-.07,(H-.66+.2)/2), (.44,.14,H-.86), 'wood_dark', bevel=False)
        for dx in (-.11,0,.11):
            box('Pilaster flute', (p+dx,-.145,(H-.6)/2+.2), (.045,.02,H-1.9), 'molding', bevel=False)
        box('Pilaster base', (p,-.09,.18), (.56,.2,.3), 'molding', bevel=False)
        box('Pilaster gold capital', (p,-.1,H-.74), (.58,.22,.18), 'gold', bevel=False)
        scroll('Capital volute', (p-.2,-.21,H-.78), .07, -1, 'gold')
        scroll('Capital volute', (p+.2,-.21,H-.78), .07, 1, 'gold')
    if halo:
        box('Halo backing panel', (0,-.02,2.4), (3.4,.03,4.0), 'wainscot', bevel=False)
        frame('Halo panel frame', 0, 2.4, 3.4, 4.0, -.05, .1, .04, 'molding', False)
        hoop('Golden time halo', (0,-.05,2.6), 1.62, .07, .05, 'gold', n=48)
        hoop('Halo inner ring', (0,-.045,2.6), 1.45, .03, .04, 'wallpaper_pat', n=48)
        for j in range(24):
            a = j*TAU/24
            beam('Halo ray', (1.7*math.sin(a),-.04,2.6+1.7*math.cos(a)),
                 ((1.9 if j % 2 else 2.15)*math.sin(a),-.04,2.6+(1.9 if j % 2 else 2.15)*math.cos(a)),
                 .05, .03, 'gold' if j % 2 == 0 else 'wallpaper_pat', bevel=False)


def room():
    start = len(PARTS)
    wall_local(-13.6, 13.6, 5.2, openings=[(-6,2.2,1.1,4.0),(6,2.2,1.1,4.0)],
               pilasters=[-10.4,-3.3,3.3,10.4], halo=True)
    move_group(start, (0, ROOM_Y, 0))
    start = len(PARTS)
    wall_local(-11.6, 11.6, 5.2, pilasters=[9.4,4.8,.2,-4.4,-9.0])
    move_group(start, (-ROOM_X, 0, 0), (0, 0, math.pi/2))
    start = len(PARTS)
    wall_local(-11.6, 11.6, 5.2, pilasters=[-9.4,-4.6,0,4.6,9.4])
    move_group(start, (ROOM_X, 0, 0), (0, 0, -math.pi/2))
    start = len(PARTS)
    wall_local(-13.6, -1.8, 1.15, low=True)
    wall_local(1.8, 13.6, 1.15, low=True)
    for x in (-1.95, 1.95):
        box('Entrance newel post', (x,.25,.65), (.34,.5,1.3), 'wood_dark')
        box('Newel gold collar', (x,.25,1.25), (.4,.56,.07), 'gold', bevel=False)
        ball('Newel brass ball', (x,.25,1.45), .16, 'brass')
    box('Threshold plate', (0,.3,.012), (3.6,.6,.025), 'brass_dark', bevel=False)
    move_group(start, (0, -ROOM_Y, 0), (0, 0, math.pi))
    for x in (-ROOM_X-.3, ROOM_X+.3):
        for y in (-ROOM_Y-.3, ROOM_Y+.3):
            box('Corner post', (x,y,2.6 if y > 0 else .6), (.7,.7,5.2 if y > 0 else 1.2), 'wainscot', bevel=False)


def dial_rim():
    cx, cy = 0, 0
    disc('Dial base', (cx,cy), DIAL_R, .03, .12, 'walnut', n=72)
    for j in range(24):   # wood marquetry: maple / pale oak wedges
        a0, a1 = j*TAU/24, (j+1)*TAU/24
        poly = [(5.3*math.cos(a0+(a1-a0)*k/4), 5.3*math.sin(a0+(a1-a0)*k/4)) for k in range(5)]
        poly += [(1.62*math.cos(a1-(a1-a0)*k/4), 1.62*math.sin(a1-(a1-a0)*k/4)) for k in range(5)]
        slab('Marquetry wedge', poly, .035, .02, 'maple' if j % 2 else 'dial_inlay')
    annulus('Outer rim', (cx,cy), 5.3, DIAL_R, .045, .03, 'walnut', n=72)
    annulus('Rim gold ring', (cx,cy), 5.5, 5.58, .055, .02, 'gold', n=72)
    annulus('Rim inner bead', (cx,cy), 5.3, 5.36, .05, .02, 'gold', n=72)
    annulus('Chapter ring', (cx,cy), 2.74, 2.8, .042, .02, 'gold', n=64)
    annulus('Chapter walnut line', (cx,cy), 2.86, 2.92, .04, .01, 'walnut', n=64)
    for j in range(60):
        a = math.pi/2 - j*TAU/60
        major = j % 5 == 0
        r0, r1 = 5.38, 5.48 if not major else 5.5
        c, s = math.cos(a), math.sin(a)
        w = .035 if not major else .07
        slab('Rim minute tick', [(r0*c-w*s, r0*s+w*c), (r1*c-w*s, r1*s+w*c), (r1*c+w*s, r1*s-w*c), (r0*c+w*s, r0*s-w*c)],
             .056, .02, 'brass' if not major else 'gold')
    for h in range(12):
        a = math.pi/2 - h*TAU/12
        disc('Medallion socket', (TILE_RING*math.cos(a), TILE_RING*math.sin(a)), TILE_R+.07, .04, .02, 'wood_dark', n=28)
        hoop('Socket gold lip', (TILE_RING*math.cos(a), TILE_RING*math.sin(a), .045), TILE_R+.1, .03, .02, 'gold', n=28,
             rot=(math.pi/2,0,0))
        b = a - TAU/24
        lozenge('Half-hour gold diamond', (TILE_RING*math.cos(b), TILE_RING*math.sin(b)), .42, .16, b, .045, .02, 'gold')
        lozenge('Radial walnut line', (2.25*math.cos(a), 2.25*math.sin(a)), 1.0, .08, a, .042, .01, 'walnut')
    disc('Hub disc', (cx,cy), 1.62, .05, .03, 'walnut', n=48)
    annulus('Hub gold ring', (cx,cy), 1.5, 1.62, .058, .02, 'gold', n=48)
    for j in range(12):
        a = j*TAU/12
        lozenge('Hub rosette petal', (.85*math.cos(a), .85*math.sin(a)), .85, .3, a, .058, .02, 'maple' if j % 2 else 'wood')
        disc('Hub gold dot', (1.36*math.cos(a+TAU/24), 1.36*math.sin(a+TAU/24)), .06, .06, .02, 'gold', n=8)
    cylinder('Empty spindle boss', (0,0,.11), .3, .12, 'brass_dark', n=20)
    cylinder('Empty spindle', (0,0,.26), .12, .3, 'brass', n=14)
    ball('Spindle gold cap', (0,0,.42), .1, 'gold')


def dial_tile():
    # Round numeral medallion, origin = its centre on the floor (slides between sockets).
    disc('Medallion base', (0,0), TILE_R, .045, .08, 'walnut', n=36)
    disc('Medallion maple inlay', (0,0), TILE_R*.9, .055, .02, 'maple', n=36)
    for j in range(12):
        a = j*TAU/12
        lozenge('Medallion oak ray', (.62*math.cos(a), .62*math.sin(a)), .3, .08, a, .059, .008, 'wood')
    flat_hoop('Medallion gold ring', (0,0,.058), TILE_R*.97, .05, .02, 'gold', n=36)
    flat_hoop('Medallion walnut line', (0,0,.06), TILE_R*.82, .03, .012, 'walnut', n=36)
    for j in range(4):
        a = TAU/8+j*TAU/4
        cylinder('Medallion brass pin', (TILE_R*.93*math.cos(a), TILE_R*.93*math.sin(a), .06), .045, .03, 'brass_dark', n=8, bevel=False)


def make_num(n):
    def build():
        text_numeral(str(n), 1.12 if n < 10 else 1.0, 'brass', 'red_dark' if n == 12 else 'walnut')   # brass numeral, walnut rim (12: dark red rim)
    return build


# ---------------------------------------------------------------- furniture & props
def workbench():
    box('Bench top', (0,0,.96), (3.3,1.0,.08), 'wood')
    box('Green felt inset', (0,.02,1.003), (3.0,.78,.012), 'felt_green', bevel=False)
    for y in (-.39,.43):
        box('Brass stringing', (0,y,1.002), (3.04,.03,.012), 'brass', bevel=False)
    box('Bench top dark edge', (0,-.5,.93), (3.32,.05,.12), 'wood_dark')
    for x in (-1.5,1.5):
        for y in (-.4,.4):
            box('Bench leg', (x,y,.46), (.12,.12,.92), 'wood_dark')
        box('Leg stretcher', (x,0,.18), (.08,.8,.08), 'wood_dark')
    box('Lower shelf', (0,0,.25), (3.0,.82,.05), 'wood')
    for j in range(4):
        x = -1.17+j*.78
        box('Drawer front', (x,-.5,.78), (.7,.04,.2), 'wood' if j % 2 else 'wood_light')
        cylinder('Drawer brass pull', (x,-.53,.78), .035, .04, 'brass', n=8, rot=(math.pi/2,0,0), bevel=False)
        box('Drawer paper label', (x+.2,-.525,.8), (.14,.01,.07), 'paper', bevel=False)
    box('Back tool board', (0,.47,1.65), (3.3,.06,1.32), 'wood_dark')
    frame('Tool board frame', 0, 1.65, 3.3, 1.32, .43, .07, .04, 'molding')
    box('Tool board gold strip', (0,.425,2.24), (3.1,.02,.035), 'gold', bevel=False)
    for j in range(5):
        x = -1.35+j*.22
        rod('Screwdriver blade', (x,.42,1.8), (x,.42,1.45), .02, 'steel', n=5)
        cylinder('Screwdriver handle', (x,.42,1.88), .035, .17, 'red_dark' if j == 2 else 'wood', n=8, bevel=False)
    for j in range(3):
        x = -.2+j*.32
        beam('Wrench shank', (x,.42,2.0), (x,.42,1.45), .05, .02, 'steel', bevel=False)
        hoop('Wrench jaw', (x,.42,2.03), .06, .025, .03, 'steel', n=10)
    for j in range(2):
        x = .85+j*.38
        rod('Hammer handle', (x,.42,1.35), (x,.42,1.85), .025, 'wood', n=6)
        box('Hammer head', (x,.42,1.9), (.2,.06,.07), 'iron_dark', bevel=False)
    beam('Calipers arm', (1.45,.42,2.05), (1.45,.42,1.45), .04, .02, 'steel', bevel=False)
    beam('Calipers jaw', (1.45,.42,2.0), (1.6,.42,2.0), .04, .02, 'steel', bevel=False)
    # her framed photograph
    box('Photo frame', (-.95,.42,2.05), (.32,.03,.4), 'gold')
    box('Photo paper', (-.95,.405,2.05), (.24,.01,.32), 'paper', bevel=False)
    lump('Photo silhouette', (-.95,.395,2.02), (.1,.01,.18), 'iron_dark')
    # vise
    box('Vise body', (-1.45,-.3,1.08), (.22,.28,.16), 'iron_dark')
    box('Vise jaw', (-1.45,-.48,1.08), (.22,.06,.16), 'iron')
    rod('Vise screw', (-1.45,-.48,1.05), (-1.45,-.78,1.05), .025, 'steel', n=6)
    rod('Vise handle', (-1.6,-.78,1.05), (-1.3,-.78,1.05), .02, 'steel', n=5)
    # small parts scattered on the top (keeps x -0.75..0.4 clear for the music box, lamp at x 1.15)
    hoop('Loupe ring', (-1.0,.05,1.025), .07, .03, .04, 'brass', n=12, rot=(math.pi/2,0,0))
    cylinder('Loupe lens', (-1.0,.05,1.02), .045, .02, 'glass', n=10, bevel=False)
    for j in range(5):
        cylinder('Loose small gear', (-1.25+j*.12,-.2+random.uniform(-.05,.05),1.01), .045+.01*j, .02, 'brass', n=10, bevel=False)
    beam('Tweezers', (.6,-.25,1.012), (.85,-.12,1.012), .03, .015, 'steel', bevel=False)
    for j in range(2):
        box('Paper sketch', (.75+j*.12,.1-j*.08,1.005+j*.004), (.4,.3,.01), 'paper', rot=(0,0,.3*j-.2), bevel=False)
    hoop('Open pocket watch case', (.55,-.35,1.02), .09, .025, .03, 'gold', n=14, rot=(math.pi/2,0,0))
    cylinder('Pocket watch face', (.55,-.35,1.015), .07, .02, 'dial_face', n=12, bevel=False)
    tube('Coiled mainspring', [(1.2+.07*math.cos(k*.7)*(1-k/30), -.3+.07*math.sin(k*.7)*(1-k/30), 1.01) for k in range(30)],
         .012, 'steel', n=4)
    lathe('Oil can', [(0,.06),(.1,.07),(.12,.04),(.25,.012)], 'brass', n=8, bevel=False).transform((-1.2,.3,1.0))


def music_box():
    W, D, H = .95, .62, .42
    box('Box bottom', (0,0,.05), (W,D,.06), 'wood_dark')
    for side in (-1,1):
        box('Box side wall', (side*(W/2-.025),0,H/2), (.05,D,H-.04), 'wood_dark')
        box('Box end wall', (0,side*(D/2-.025),H/2), (W,.05,H-.04), 'wood_dark')
        box('Velvet side lining', (side*(W/2-.06),0,H/2), (.02,D-.1,H-.1), 'velvet_red', bevel=False)
        box('Velvet end lining', (0,side*(D/2-.06),H/2), (W-.1,.02,H-.1), 'velvet_red', bevel=False)
    for x in (-1,1):
        for y in (-1,1):
            box('Brass corner cap', (x*(W/2-.02),y*(D/2-.02),H/2), (.06,.06,H), 'brass', bevel=False)
            ball('Gold bun foot', (x*(W/2-.06),y*(D/2-.06),.02), .045, 'gold', detail=1)
    box('Front gold stringing', (0,-D/2-.005,H-.06), (W-.1,.01,.025), 'gold', bevel=False)
    box('Front gold stringing low', (0,-D/2-.005,.1), (W-.1,.01,.025), 'gold', bevel=False)
    for j in range(3):
        lozenge('Front marquetry diamond', (0,0), .2, .1, 0, 0, .01, 'wood_light')
        move_group(len(PARTS)-1, (-.27+j*.27,-D/2-.006,.24), (math.pi/2,0,0))
    box('Inner floor velvet', (0,0,.085), (W-.12,D-.12,.01), 'velvet_red', bevel=False)
    # comb in front of the cylinder
    cyl = ANCHORS['MusicBox']['cylinder']
    box('Comb base', (cyl[0],cyl[1]-.17,.2), (.46,.08,.06), 'steel_dark')
    for j in range(18):
        box('Comb tooth', (cyl[0]-.21+j*.025,cyl[1]-.115,.22), (.016,.09,.016), 'steel', bevel=False)
    for side in (-1,1):
        box('Cylinder bearing', (cyl[0]+side*.25,cyl[1],cyl[2]-.06), (.04,.08,.2), 'brass', bevel=False)
    # governor train and dancer on the right
    for j,(x,y,r) in enumerate(((.18,.15,.07),(.27,.12,.05),(.33,.16,.035))):
        cylinder('Governor gear', (x,y,.2+j*.03), r, .02, 'brass' if j % 2 == 0 else 'brass_dark', n=12, bevel=False)
    cylinder('Dancer turntable', (.27,-.12,.12), .1, .03, 'velvet_red', n=16, bevel=False)
    flat_hoop('Turntable gold rim', (.27,-.12,.13), .1, .02, .03, 'gold', n=16)
    lathe('Dancer skirt', [(.13,.07),(.17,.04),(.2,.015)], 'porcelain', n=10, bevel=False).transform((.27,-.12,0))
    rod('Dancer body', (.27,-.12,.19), (.27,-.12,.27), .02, 'porcelain', n=5)
    ball('Dancer head', (.27,-.12,.29), .025, 'porcelain', detail=1)
    rod('Dancer raised arm', (.27,-.12,.26), (.3,-.12,.33), .012, 'porcelain', n=4)
    # winding key on the right outside wall
    rod('Key shaft', (W/2,0,.22), (W/2+.08,0,.22), .015, 'steel', n=5)
    hoop('Key bow', (W/2+.13,0,.22), .05, .02, .02, 'gold', n=10, rot=(0,0,math.pi/2))
    # open lid (hinge at back top edge), rotated back 105 degrees
    start = len(PARTS)
    box('Lid panel', (0,-D/2,.03), (W,D,.06), 'wood_dark')
    box('Lid gold border', (0,-D/2,.065), (W-.06,D-.06,.012), 'gold', bevel=False)
    box('Lid top inlay', (0,-D/2,.072), (W-.18,D-.18,.01), 'wood_light', bevel=False)
    box('Lid inner mirror', (0,-D/2,-.006), (W-.2,D-.2,.01), 'silver', bevel=False)
    box('Lid inner velvet frame', (0,-D/2,-.003), (W-.1,D-.1,.006), 'velvet_red', bevel=False)
    for k in range(5):
        a = k*TAU/5
        ball('Painted rose petal', (.03*math.cos(a)+.25, -D/2+.03*math.sin(a)-.08, -.015), .035, 'red_lacquer', detail=1)
    ball('Rose heart', (.25,-D/2-.08,-.02), .025, 'red_dark', detail=1)
    move_group(start, (0, D/2, H), (-math.radians(105),0,0))
    for side in (-1,1):
        cylinder('Lid hinge', (side*.3,D/2,H), .025, .1, 'brass', n=8, rot=(0,math.pi/2,0), bevel=False)


def music_cylinder():
    start = len(PARTS)
    lathe('Pinned drum', [(-.21,.095),(.21,.095)], 'brass', n=20, bevel=False)
    for side in (-1,1):
        cylinder('Drum gold end cap', (0,0,side*.215), .1, .015, 'gold', n=20, bevel=False)
    rod('Drum axle', (0,0,-.26), (0,0,.26), .02, 'steel', n=6)
    for j in range(14):
        a = j*TAU/14
        box('Drive gear tooth', (.12*math.cos(a), .12*math.sin(a), .23), (.035,.035,.02), 'brass_dark',
            rot=(0,0,a), bevel=False)
    cylinder('Drive gear', (0,0,.23), .11, .02, 'brass_dark', n=14, bevel=False)
    random.seed(4141)
    for j in range(84):
        z = -.19+(j % 14)*.028
        a = j*TAU/84*5.3 + random.uniform(-.1,.1)
        box('Drum pin', (.1*math.cos(a), .1*math.sin(a), z), (.016,.016,.016), 'silver', rot=(0,0,a), bevel=False)
    move_group(start, rot=(0, math.pi/2, 0))


def lamp(red=False):
    glow = 'red_glow' if red else 'lantern_glow'
    s = .72 if red else 1.0
    start = len(PARTS)
    lathe('Lamp weighted base', [(0,.16),(.04,.17),(.07,.12),(.1,.07)], 'iron_dark', n=14, bevel=False)
    flat_hoop('Base gold ring', (0,0,.07), .12, .025, .03, 'gold', n=14)
    lathe('Lamp brass font', [(.1,.06),(.16,.13),(.24,.12),(.29,.06)], 'brass', n=14, bevel=False)
    cylinder('Wick collar', (0,0,.31), .06, .04, 'brass_dark', n=10, bevel=False)
    lathe('Glowing chimney', [(.33,.05),(.4,.075),(.5,.06),(.62,.035)], glow, n=12, bevel=False)
    lathe('Lamp shade', [(.5,.2),(.57,.15),(.62,.1)], 'red_dark' if red else 'paper', n=16, bevel=False)
    flat_hoop('Shade gold rim', (0,0,.5), .2, .025, .03, 'gold', n=16)
    if red:
        for j in range(12):
            a = j*TAU/12
            rod('Shade fringe', (.2*math.cos(a),.2*math.sin(a),.5), (.2*math.cos(a),.2*math.sin(a),.44), .012, 'red_lacquer', n=4)
    hoop('Lamp handle', (0,.17,.18), .06, .02, .02, 'brass', n=10, rot=(0,0,math.pi/2))
    move_group(start, scale=(s,s,s))


def side_table():
    cylinder('Table top', (0,0,.68), .36, .05, 'wood_dark', n=24)
    flat_hoop('Top gold rim', (0,0,.7), .36, .03, .03, 'gold', n=24)
    cylinder('Lace doily', (0,0,.708), .22, .006, 'paper', n=18, bevel=False)
    lathe('Turned pedestal', [(.12,.05),(.25,.07),(.4,.045),(.55,.06),(.66,.04)], 'wood', n=10, bevel=False)
    for j in range(3):
        a = j*TAU/3
        tube('Splayed leg', [(0,0,.15),(.18*math.cos(a),.18*math.sin(a),.07),(.3*math.cos(a),.3*math.sin(a),.02)], .03, 'wood_dark', n=5)
        ball('Brass foot', (.3*math.cos(a),.3*math.sin(a),.02), .035, 'brass', detail=1)


def gear():
    T = .22
    teeth = 18
    for j in range(teeth):
        a = j*TAU/teeth + TAU/36          # one tooth sits exactly at 12 o'clock (90 deg) = red marker
        sector('Gear rim segment', .78, .93, a-TAU/teeth/2, a+TAU/teeth/2, T, 'brass', steps=2, bevel=False)
        hw_root, hw_tip = TAU/teeth*.26, TAU/teeth*.16
        poly = [(.92*math.cos(a-hw_root), .92*math.sin(a-hw_root)), (1.05*math.cos(a-hw_tip), 1.05*math.sin(a-hw_tip)),
                (1.05*math.cos(a+hw_tip), 1.05*math.sin(a+hw_tip)), (.92*math.cos(a+hw_root), .92*math.sin(a+hw_root))]
        marker = abs(a-math.pi/2) < 1e-6 or abs(a-math.pi/2) < TAU/teeth/2-1e-6
        extrude('Gear tooth', poly, T*.9, 'red_lacquer' if marker else 'brass', bevel=False)
        ycyl('Rim silver rivet', (.855*math.cos(a+TAU/teeth/2), -T/2-.01, .855*math.sin(a+TAU/teeth/2)), .028, .02, 'silver', n=6)
    hoop('Rim gold bead', (0,0,0), .8, .035, T+.04, 'gold', n=48)
    hoop('Outer gold bead', (0,0,0), .935, .025, T+.02, 'gold', n=48)
    for j in range(6):
        a = j*TAU/6+TAU/12
        beam('Gear spoke', (.34*math.cos(a),0,.34*math.sin(a)), (.8*math.cos(a),0,.8*math.sin(a)), .14, .14, 'brass_dark', bevel=False)
        ball('Spoke boss', (.57*math.cos(a),-.08,.57*math.sin(a)), .05, 'gold', detail=1)
    ycyl('Gear hub', (0,0,0), .36, T+.06, 'steel', n=24)
    hoop('Hub gold ring', (0,-T/2-.03,0), .36, .06, .03, 'gold', n=24)
    ycyl('Number plate', (0,-T/2-.045,0), .3, .03, 'dial_face', n=24)
    hoop('Number plate ring', (0,-T/2-.06,0), .3, .03, .02, 'dial_mark', n=24)


def gear_stand():
    ax = ANCHORS['GearStand']['axle'][2]
    box('Stand base plate', (0,.25,.06), (1.5,1.0,.12), 'iron')
    box('Base dark molding', (0,.25,.015), (1.58,1.08,.03), 'brass_dark', bevel=False)
    for x in (-.65,.65):
        for y in (-.17,.67):
            cylinder('Base bolt', (x,y,.13), .04, .04, 'silver', n=6, bevel=False)
    box('Rear column', (0,.45,(ax+.12)/2+.06), (.26,.26,ax), 'iron_dark')
    box('Column gold capital', (0,.45,ax+.2), (.34,.34,.08), 'gold', bevel=False)
    ycyl('Bearing housing', (0,.3,ax), .18, .22, 'brass', n=16)
    rod('Axle', (0,.3,ax), (0,-.1,ax), .07, 'steel', n=10)
    for side in (-1,1):
        beam('Stand brace', (side*.6,.5,.12), (side*.1,.45,ax-.25), .08, .08, 'iron')
        scroll('Brace scroll', (side*.35,.3,.45), .12, side, 'gold')


def chair_body():
    box('Seat frame', (0,0,.48), (.56,.52,.07), 'wood_dark')
    lump('Leather cushion', (0,-.01,.53), (.52,.48,.1), 'leather')
    for x in (-.24,.24):
        lathe('Turned front leg', [(0,.03),(.15,.04),(.3,.028),(.45,.035)], 'wood_dark', n=8, bevel=False).transform((x,-.22,0))
        beam('Raked back leg', (x,.24,0), (x,.3,1.15), .05, .05, 'wood_dark')
        box('Side stretcher', (x,0,.18), (.035,.44,.035), 'wood', bevel=False)
    extrude('Carved crest rail', [(-.27,0),(.27,0),(.27,.1),(.12,.16),(0,.2),(-.12,.16),(-.27,.1)], .06, 'wood_dark', y=.3).transform((0,0,1.0))
    extrude('Vase splat', [(-.08,.55),(.08,.55),(.12,.7),(.06,.82),(.1,.95),(-.1,.95),(-.06,.82),(-.12,.7)], .03, 'wood', y=.28)
    box('Splat gold line', (0,.26,.78), (.03,.01,.35), 'gold', bevel=False)
    for j in range(9):
        x = -.24+j*.06
        cylinder('Brass nail head', (x,-.26,.5), .018, .012, 'brass', n=6, rot=(math.pi/2,0,0), bevel=False)


def chair():
    chair_body()


def empty_chair():
    chair_body()
    # Her red shawl draped over the crest rail and onto the seat
    for i in range(7):
        x = -.27+i*.09
        jit = random.uniform(-.03,.03)
        pts = [(x,-.2+jit,.6), (x,.05,.62), (x,.24,.9), (x,.31,1.22), (x,.38,1.0+jit), (x,.4,.62+jit*3)]
        ribbon_solid('Shawl drape', pts, .1, 'shawl_red' if i % 2 == 0 else 'shawl_red_dark')
        for end in (pts[0], pts[-1]):
            for k in range(2):
                rod('Shawl fringe', (end[0]-.02+k*.04, end[1], end[2]), (end[0]-.02+k*.04, end[1]-.01, end[2]-.08), .012, 'shawl_red_dark', n=4)
    ribbon_solid('Shawl corner hanging off the side', [(.3,.2,1.1),(.38,.12,.85),(.4,.05,.55)], .14, 'shawl_red')


def ribbon_solid(label, points, width, role, thick=.04):
    n = len(points)
    verts = [(x+dx,y,z+dz) for dz in (-thick/2,thick/2)
             for x,y,z in points for dx in (-width/2,width/2)]
    faces = []
    for i in range(n-1):
        a, b, k = i*2, (i+1)*2, n*2
        faces += [(a,b,b+1,a+1),(a+k+1,b+k+1,b+k,a+k),
                  (a,a+k,b+k,b),(a+1,b+1,b+k+1,a+k+1)]
    faces += [(0,1,2*n+1,2*n),(2*n-2,4*n-2,4*n-1,2*n-1)]
    return Part(label,verts,faces,role,False)


def stool():
    for x in (-.28,.28):
        extrude('Stool side', [(-.3,0),(.3,0),(.3,.3),(-.02,.3),(-.02,.62),(-.3,.62)], .05, 'wood', y=0).transform((x,0,0), (0,0,math.pi/2))
    box('Upper step', (0,-.16,.6), (.62,.32,.05), 'wood_dark')
    box('Lower step', (0,.16,.29), (.62,.3,.05), 'wood_dark')
    box('Step leather pad', (0,-.16,.63), (.5,.26,.02), 'leather', bevel=False)
    for y in (-.16,.16):
        box('Step brass nosing', (0,y-.15,.6 if y < 0 else .29), (.62,.03,.06), 'brass', bevel=False)
    box('Iron bracket', (0,.0,.15), (.6,.04,.04), 'iron_dark', bevel=False)


def tool_cabinet():
    box('Cabinet carcass', (0,0,1.08), (1.4,.55,1.96), 'wood_dark')
    box('Cabinet plinth', (0,0,.05), (1.48,.6,.1), 'molding')
    box('Cabinet crown', (0,0,2.1), (1.52,.62,.1), 'molding')
    box('Crown gold bead', (0,-.315,2.04), (1.5,.02,.03), 'gold', bevel=False)
    for r in range(8):
        for c in range(6):
            x = -.55+c*.22; z = .3+r*.22
            box('Small drawer', (x,-.29,z), (.2,.03,.19), 'wood' if (r+c) % 2 else 'wood_light', bevel=False)
            cylinder('Drawer knob', (x,-.31,z-.04), .018, .02, 'brass', n=6, rot=(math.pi/2,0,0), bevel=False)
            box('Drawer label', (x,-.307,z+.04), (.1,.005,.04), 'paper', bevel=False)
    lathe('Glass dome', [(0,.16),(.12,.16),(.3,.1),(.36,.02)], 'glass', n=12, bevel=False).transform((.35,0,2.15))
    cylinder('Dome base', (.35,0,2.16), .18, .03, 'brass', n=12, bevel=False)
    cylinder('Gear under dome', (.35,0,2.25), .08, .02, 'gold', n=10, rot=(math.pi/2,0,0), bevel=False)


def parts_crates():
    for k,(x,z,yaw) in enumerate(((0,.3,0),(0.05,.88,.15))):
        start = len(PARTS)
        box('Crate body', (0,0,0), (1.1,.75,.56), 'wood')
        for dz in (-.18,0,.18):
            box('Crate slat', (0,-.38,dz), (1.1,.02,.12), 'wood_light' if k else 'wood_dark', bevel=False)
        for dx in (-.53,.53):
            box('Crate iron band', (dx,0,0), (.04,.78,.58), 'iron_dark', bevel=False)
        box('Crate stencil', (0,-.395,.0), (.3,.01,.08), 'paper', bevel=False)
        move_group(start, (x,0,z), (0,0,yaw))
    start = len(PARTS)
    box('Open box', (0,0,.18), (.8,.6,.36), 'wood_dark')
    for j in range(5):
        ycyl('Spilled gear', (-.25+j*.13,-.05+random.uniform(-.1,.1),.4+random.uniform(0,.08)), .1-.01*j, .03,
             'brass' if j % 2 else 'brass_dark', n=12)
    ycyl('Spare dial', (.2,.1,.48), .16, .02, 'dial_face', n=16)
    tube('Spare spring', [(-.1+.08*math.cos(k*.6), .15+.08*math.sin(k*.6), .36+k*.012) for k in range(20)], .015, 'steel', n=4)
    move_group(start, (1.05,-.2,0), (0,0,-.3))


def covered_mirror():
    rings = []
    for k in range(13):
        z = .05+k*.19
        t = z/2.33
        W = .66+.16*(1-t)**3
        D = .2+.26*(1-t)**3
        if z > 2.05:
            q = math.sqrt(max(0, 1-((z-2.05)/.3)**2))
            W *= max(.1, q); D *= max(.1, q)
        ring = []
        for j in range(28):
            th = j*TAU/28
            f = (.03+.1*(1-t)**2)*math.sin(7*th+.5)
            ring.append((W*math.cos(th)*(1+f), D*math.sin(th)*(1+f), z))
        rings.append(ring)
    ring_skin('Dust sheet', rings, 'sheet')
    for k in range(7):
        th = (-.5-math.pi/2+k*TAU)/7
        pts = []
        for z in (.08,.5,.9,1.3):
            t = z/2.33
            W, D = .66+.16*(1-t)**3, .2+.26*(1-t)**3
            f = (.03+.1*(1-t)**2)*-1
            pts.append((W*math.cos(th)*(1+f)*1.02, D*math.sin(th)*(1+f)*1.02-.01, z))
        tube('Sheet fold shadow', pts, .035, 'sheet_shade', n=4)
    for side in (-1,1):
        beam('Cheval upright', (side*.9,0,.15), (side*.9,0,1.95), .07, .07, 'wood_dark')
        ball('Upright gold finial', (side*.9,0,2.0), .06, 'gold')
        ycyl('Pivot knob', (side*.9,-.05,1.3), .06, .06, 'brass', n=10)
        beam('Splayed foot', (side*.9,-.35,0.04), (side*.9,.35,.04), .08, .08, 'wood_dark')
        for y in (-.35,.35):
            ball('Gold claw foot', (side*.9,y,.04), .06, 'gold', detail=1)
    box('Mirror sliver', (.0,-.22,.1), (.7,.02,.08), 'silver', bevel=False)


def umbrella_stand():
    lathe('Stand body', [(0,.16),(.05,.17),(.55,.14),(.6,.16)], 'brass_dark', n=14, bevel=False)
    for z in (.1,.55):
        flat_hoop('Stand gold band', (0,0,z), .17, .03, .04, 'gold', n=14)
    rows = [(.3,.04),(.6,.09),(.95,.11),(1.15,.03)]
    lathe('Closed black umbrella', rows, 'curtain_dark', n=8, bevel=False).transform((.02,0,0), (0,.12,0))
    rod('Umbrella shaft', (.12,0,1.1), (.15,0,1.35), .015, 'iron_dark', n=5)
    tube('Umbrella hook handle', [(.15,0,1.35),(.17,0,1.45),(.25,0,1.45),(.27,0,1.38)], .025, 'wood', n=6)
    slab('Rain puddle', [(.4*math.cos(a)*random.uniform(.8,1.2)+.15, .3*math.sin(a)*random.uniform(.8,1.2)-.1) for a in [j*TAU/10 for j in range(10)]],
         .004, .004, 'rain')


def rug():
    def ellipse(a, b, n=40):
        return [(a*math.cos(j*TAU/n), b*math.sin(j*TAU/n)) for j in range(n)]
    slab('Rug dark border', ellipse(1.7,1.15), .012, .012, 'rug_dark')
    slab('Rug field', ellipse(1.52,.98), .016, .004, 'rug')
    for j in range(40):
        a, b = j*TAU/40, (j+1)*TAU/40
        slab('Rug red line', [(1.42*math.cos(a), .9*math.sin(a)), (1.47*math.cos(a), .94*math.sin(a)),
                              (1.47*math.cos(b), .94*math.sin(b)), (1.42*math.cos(b), .9*math.sin(b))], .019, .003, 'red_dark')
    lozenge('Rug medallion', (0,0), 1.2, .7, 0, .02, .004, 'rug_dark')
    lozenge('Medallion core', (0,0), .5, .3, 0, .023, .003, 'paper')
    for side in (-1,1):
        for j in range(8):
            y = -.5+j*.14
            rod('Rug fringe', (side*1.68*math.sqrt(max(0,1-(y/1.15)**2)), y, .008),
                (side*(1.68*math.sqrt(max(0,1-(y/1.15)**2))+.1), y, .008), .012, 'paper', n=4)


def deco_round():
    dial(0, 0, -.1, .3, back='wood_dark', ring2='gold', ticks=12)
    hoop('Deco carved rim', (0,-.03,0), .36, .07, .1, 'wood', n=24)
    baked_hands(0, 0, -.14, .3, 10, 10, 'iron_dark')


def deco_sun():
    for j in range(16):
        a = j*TAU/16
        r1 = .5 if j % 2 == 0 else .4
        beam('Sunburst ray', (.26*math.sin(a),-.03,.26*math.cos(a)), (r1*math.sin(a),-.03,r1*math.cos(a)), .05, .03,
             'gold' if j % 2 == 0 else 'brass', bevel=False)
        ball('Ray tip', (r1*math.sin(a),-.03,r1*math.cos(a)), .03, 'gold', detail=1)
    dial(0, 0, -.08, .22, back='wood_dark', ring2='gold', ticks=12, roman=False)
    baked_hands(0, 0, -.12, .22, 3, 40, 'red_lacquer')


def clock_shelf():
    box('Shelf board', (0,-.2,.38), (2.4,.4,.06), 'wood')
    box('Shelf front edge', (0,-.4,.37), (2.42,.03,.08), 'gold', bevel=False)
    for x in (-1.0,0,1.0):
        extrude('Shelf bracket', [(0,.35),(-.3,.35),(0,0)], .05, 'wood_dark', y=0).transform((0,0,0),(0,0,math.pi/2))
        move_group(len(PARTS)-1, (x,0,0))
    # napoleon hat clock
    start = len(PARTS)
    extrude('Hat clock case', [(-.3,0),(.3,0),(.28,.12),(.15,.25),(0,.3),(-.15,.25),(-.28,.12)], .14, 'wood_dark')
    dial(0, .15, -.08, .1, ring2='gold', ticks=12, roman=False)
    baked_hands(0, .15, -.1, .1, 8, 20, 'iron_dark')
    move_group(start, (-.75,-.22,.41))
    # carriage clock
    start = len(PARTS)
    box('Carriage clock frame', (0,0,.15), (.22,.16,.3), 'brass')
    box('Carriage glass', (0,0,.15), (.18,.17,.24), 'glass', bevel=False)
    tube('Carriage handle', [(-.08,0,.3),(-.06,0,.36),(.06,0,.36),(.08,0,.3)], .015, 'gold', n=4)
    dial(0, .17, -.09, .07, ring2='gold', ticks=12, roman=False)
    move_group(start, (0,-.22,.41))
    # dome clock
    start = len(PARTS)
    cylinder('Dome clock base', (0,0,.03), .15, .06, 'wood_dark', n=14)
    lathe('Glass dome', [(.06,.13),(.25,.12),(.38,.06),(.42,.02)], 'glass', n=12, bevel=False)
    dial(0, .2, -.02, .07, back='brass', ring2='gold', ticks=12, roman=False)
    baked_hands(0, .2, -.04, .07, 11, 59, 'red_lacquer')
    move_group(start, (.75,-.22,.41))


def sconce():
    """Wall candle sconce: back plane y=0 (wall), origin = back centre at candle-cup height."""
    extrude('Sconce backplate', [(-.12,-.35),(.12,-.35),(.16,.05),(0,.3),(-.16,.05)], .04, 'brass', y=-.02)
    hoop('Backplate gold ring', (0,-.045,0), .1, .025, .02, 'gold', n=14)
    ball('Backplate walnut boss', (0,-.05,0), .055, 'wood_dark', detail=1)
    scroll('Backplate scroll', (-.1,-.05,-.22), .08, -1, 'gold')
    scroll('Backplate scroll', (.1,-.05,-.22), .08, 1, 'gold')
    for side in (-1,1):
        tube('Sconce arm', [(0,-.05,-.1),(side*.1,-.18,-.16),(side*.22,-.25,-.05),(side*.24,-.25,.02)], .022, 'brass_dark', n=6)
        cylinder('Drip pan', (side*.24,-.25,.03), .07, .02, 'brass', n=12, bevel=False)
        cylinder('Ivory candle', (side*.24,-.25,.14), .03, .2, 'porcelain', n=10, bevel=False)
        lathe('Candle flame', [(.24,.0),(.25,.022),(.29,.018),(.33,.0)], 'lantern_glow', n=8, bevel=False).transform((side*.24,-.25,0))
        lump('Wax drip', (side*.24+.02,-.27,.08), (.03,.025,.06), 'paper')
    ball('Crest red bead', (0,-.04,.24), .025, 'red_dark', detail=1)


# ---------------------------------------------------------------- export
KEEP = {'Ground', 'Room', 'DialRim', 'DialTile', 'GrandHour', 'GrandMinute', 'GrandPendulum',
        'HandHour', 'HandMinute', 'HandHourRed', 'HandMinuteRed', 'PendulumS', 'Gear', 'MusicCylinder',
        'GrandClock', 'WallClock1', 'WallClock2', 'WallClock3', 'WallClock4', 'MusicBox', 'GearStand',
        'Window', 'DecoRound', 'DecoSun', 'ClockShelf', 'Sconce'}
MIN_ROLES = {'HandHour': 2, 'HandMinute': 2, 'HandHourRed': 2, 'HandMinuteRed': 2, 'GrandHour': 2,
             'GrandMinute': 3, 'PendulumS': 4, 'GrandPendulum': 4, 'MusicCylinder': 4, 'Rug': 4, 'Stool': 4}

SPECS = [
    ('Ground', ground, 60000),
    ('Room', room, 40000),
    ('Window', window, 8000),
    ('DialRim', dial_rim, 12000),
    ('DialTile', dial_tile, 2000),
]
SPECS += [('Num_%d' % n, make_num(n), 3000) for n in range(1, 13)]
SPECS += [
    ('GrandClock', grand_clock, 16000),
    ('GrandHour', lambda: hand_hour('iron_dark', 1.65, True), 1500),
    ('GrandMinute', lambda: hand_minute('iron_dark', 1.65, 'red_lacquer', True), 1500),
    ('GrandPendulum', grand_pendulum, 2500),
    ('WallClock1', wall_clock_1, 10000),
    ('WallClock2', wall_clock_2, 10000),
    ('WallClock3', wall_clock_3, 10000),
    ('WallClock4', wall_clock_4, 10000),
    ('HandHour', lambda: hand_hour('iron_dark'), 600),
    ('HandMinute', lambda: hand_minute('iron_dark'), 600),
    ('HandHourRed', lambda: hand_hour('red_lacquer'), 600),
    ('HandMinuteRed', lambda: hand_minute('red_lacquer'), 600),
    ('PendulumS', pendulum_s, 900),
    ('DecoRound', deco_round, 3000),
    ('DecoSun', deco_sun, 3000),
    ('ClockShelf', clock_shelf, 6000),
    ('Workbench', workbench, 9000),
    ('MusicBox', music_box, 7000),
    ('MusicCylinder', music_cylinder, 2500),
    ('Lamp', lambda: lamp(False), 2000),
    ('RedLamp', lambda: lamp(True), 2000),
    ('SideTable', side_table, 2000),
    ('Gear', gear, 6000),
    ('GearStand', gear_stand, 3000),
    ('Chair', chair, 4000),
    ('EmptyChair', empty_chair, 5000),
    ('Stool', stool, 1500),
    ('ToolCabinet', tool_cabinet, 7000),
    ('PartsCrates', parts_crates, 4000),
    ('CoveredMirror', covered_mirror, 4000),
    ('UmbrellaStand', umbrella_stand, 2000),
    ('Rug', rug, 3000),
    ('Sconce', sconce, 2000),
]


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
            mod.width, mod.segments = .02, 2
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


def finish(name, budget):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    raw = triangles(obj)
    for attempt in range(4):
        if triangles(obj) <= budget:
            break
        mod = obj.modifiers.new('Budget', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .95) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name, triangles(obj), budget)
    if name not in KEEP:
        lo, hi = bounds(obj)
        mid = (hi + lo) / 2
        for v in obj.data.vertices:
            v.co.x -= mid.x
            v.co.y -= mid.y
            v.co.z -= lo.z
    elif name.startswith('Num_'):
        pass
    obj.data.update()
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    need = MIN_ROLES.get(name, 2 if name.startswith('Num_') else 5)
    assert len(used) >= need, (name, 'needs role materials', need, used)
    destination = OUTPUT / ('Kit_W_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} (raw {raw}) {tuple(round(d, 3) for d in obj.dimensions)} roles {len(used)}', flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.render.resolution_x, scene.render.resolution_y = 2000, 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
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
    scene.world.color = (.36, .3, .26)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    hidden = ('Ground', 'Room', 'DialRim') + tuple('Num_%d' % n for n in range(2, 12))
    for obj in objects:
        obj.hide_render = obj.name in hidden
    objects = [o for o in objects if o.name not in hidden]
    by = {o.name: o for o in objects}
    row1 = [by[n] for n in ('GrandClock','WallClock1','WallClock2','WallClock3','WallClock4','Window','ToolCabinet','CoveredMirror')]
    row2 = [by[n] for n in ('Workbench','Gear','GearStand','PartsCrates','Rug','Chair','EmptyChair','UmbrellaStand')]
    row3 = [o for o in objects if o not in row1+row2]
    for obj in row3:
        obj.scale = (2.2,)*3 if obj.name not in ('DialTile','Num_1','Num_12','ClockShelf','DecoSun','MusicBox') else (1.2,)*3
    for obj in row1 + row2:
        obj.scale = (1,1,1)
    bpy.context.view_layer.update()
    def lay(row, y, gap):
        extents = []
        for obj in row:
            corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
            xs,ys,zs = ([c[k] for c in corners] for k in range(3))
            extents.append((min(xs)-obj.location.x,max(xs)-obj.location.x,(min(ys)+max(ys))/2-obj.location.y,min(zs)-obj.location.z))
        total = sum(hi-lo for lo,hi,_,_ in extents) + gap*(len(row)-1)
        x = -total/2
        for obj,(lo,hi,cy,zlo) in zip(row,extents):
            obj.location = (x-lo, y-cy, -zlo)
            x += hi-lo+gap
    lay(row1, 7, .8)
    lay(row2, 1, .8)
    half = (len(row3)+1)//2
    lay(row3[:half], -4, .6)
    lay(row3[half:], -8, .6)
    bpy.context.view_layer.update()
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    a = math.radians(40)
    cam.location = Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler = (-cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    corners = [inv @ (obj.matrix_world @ Vector(c)) for obj in objects for c in obj.bound_box]
    left,right = min(v.x for v in corners),max(v.x for v in corners)
    low,high = min(v.y for v in corners),max(v.y for v in corners)
    cam.location += cam.rotation_euler.to_matrix() @ Vector(((left+right)/2,(low+high)/2,0))
    aspect = scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale = max(right-left,(high-low)*aspect)*1.06
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
    load_font()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    out = []
    for i, (name, build, budget) in enumerate(SPECS):
        PARTS = []
        random.seed(41159 + i * 977)
        build()
        out.append(finish(name, budget))
    for name, a in ANCHORS.items():
        print('ANCHOR', name, a, flush=True)
    preview(out)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
