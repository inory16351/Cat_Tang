# 스토리 맵 5 '11:59의 병실' 키트 — 지금 이 순간 시계공이 누운 밤의 병실, 2026-11-26
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_ward.py
# 회색 Principled 역할 재질 FBX(Kit_H_*.fbx): Unity는 재질 NAME으로 재채색. 미리보기만 PALETTE 적용.
# Blender (X,Y,Z) = Unity (X,Z,Y); +Y 북쪽, 정면 -Y; 남쪽 위 57도 카메라.
# KEEP: Ground/Room 맵 원점, WallClock 뒷면 (0,0,0) = 문자판 중심 높이, 바늘 축 +Z=12시.
# 링거 점적통 가운데 높이 = 1.52m. 중심 보정 후 Blender (0.012216,0,1.52)m.
# 사신 목/머리 피벗 = Blender (-0.077326,-0.002695,2.0)m = Unity (-0.077326,2.0,-0.002695)m.
# 원형 목 (0,0,2.0)을 몸 중심 보정과 함께 변환한 실측값; ANCHOR 출력으로 재검증.
# ReaperHead 원점은 목; 중심 보정된 ANCHORS['ReaperBody']['neck']에 부착. IVDrop 원점은 가운데.
# Bed 매트리스 평평한 윗면 = Z 0.75m; 북쪽 등받이는 30도 상승.
# OldManLying 명세의 특수 피벗 = 매트리스 가운데 윗면: KEEP, Bed 위치 + (0,0,.75)에 부착.
# CurtainRail도 바닥 기준 원점을 유지(KEEP): 매달린 천 하단 .30, 레일 3.40m 보존.
# 19개 명세 항목, 바늘 3개를 각각 내보내므로 FBX 총 21개.
# 그림자(2재질)와 단일 iv_fluid 물방울은 조각별 명시 규칙이 일반 5~8재질 규칙보다 우선.
# 공방 프레임워크의 Part/geometry/chunk clipping/silhouette/mesh/budget/FBX 옵션을 그대로 계승.
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryWard'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_ward_preview.png'
TAU = math.tau
PALETTE = {
    'tile': (.9,.87,.8), 'tile_light': (.8,.82,.84), 'tile_dark': (.55,.62,.66),
    'tile_seam': (.3,.33,.38), 'wall': (.72,.86,.82), 'wall_pat': (.6,.75,.74),
    'wainscot': (.32,.5,.52), 'molding': (.92,.9,.82), 'plaster_dark': (.1,.14,.22),
    'void': (.02,.02,.04), 'wood': (.66,.44,.24), 'wood_dark': (.36,.21,.11),
    'wood_light': (.88,.72,.48), 'walnut': (.3,.17,.09), 'steel': (.78,.8,.84),
    'steel_dark': (.36,.38,.44), 'chrome': (.92,.94,.97), 'brass': (.92,.72,.36),
    'gold': (1,.82,.36), 'sheet': (.97,.97,.95), 'sheet_shade': (.78,.82,.86),
    'pillow': (.95,.95,.92), 'blanket': (.62,.75,.85), 'blanket_pat': (.45,.58,.72),
    'curtain': (.86,.82,.72), 'curtain_dark': (.56,.6,.66), 'dial_face': (1,.94,.8),
    'dial_mark': (.2,.11,.06), 'iv_bag': (.92,.94,.96), 'iv_fluid': (1,.95,.7),
    'tube': (.88,.92,.96), 'screen': (.04,.06,.06), 'screen_glow': (.3,1,.55),
    'plastic': (.86,.87,.88), 'plastic_dark': (.45,.47,.5),
    'book_cover': (.55,.22,.15), 'book_page': (.96,.9,.76), 'book_gold': (1,.75,.35),
    'shawl_red': (.8,.08,.1), 'shawl_red_dark': (.45,.04,.06), 'glass': (.4,.55,.75),
    'night': (.08,.1,.25), 'moon_glow': (1,.96,.8), 'lamp_glow': (1,.75,.4),
    'flower': (.98,.97,.94), 'leaf': (.35,.6,.35), 'vase': (.6,.78,.9),
    'shadow': (.08,.05,.12), 'shadow_rim': (1,.75,.45),
}
ROLES = tuple(PALETTE)
HARD = {'wood','wood_dark','wood_light','walnut','steel','steel_dark','chrome',
        'brass','gold','molding','plastic','plastic_dark','dial_face'}
FONT_FILES = ('C:/Windows/Fonts/BOOKOSB.TTF', 'C:/Windows/Fonts/timesbd.ttf', 'C:/Windows/Fonts/georgiab.ttf')
ROMAN = ('XII', 'I', 'II', 'III', 'IIII', 'V', 'VI', 'VII', 'VIII', 'IX', 'X', 'XI')
MATS, PARTS, FONT = {}, [], None
ROOM_X, ROOM_Y = 12.0, 10.0
OUTLINE = [(-12,-10),(12,-10),(12,10),(-12,10)]
BATTLE_C, BATTLE_R = (0,-3.5), 6.0
MATTRESS_TOP = .75
REAPER_NECK = (0,0,2.0)
IV_CHAMBER = (.24,0,1.52)
ANCHORS = {'ReaperBody': {'neck': REAPER_NECK},
           'IVStand': {'drip_chamber': IV_CHAMBER},
           'Bed': {'mattress_top': MATTRESS_TOP},
           'OldManLying': {'mount': (0,0,MATTRESS_TOP)},
           'WallClock': {'hands': (0,-.30,0)}}
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
        w = max(.04,height*.15)
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


# ---------------------------------------------------------------- chunk floor / room
def chunk_cells():
    """Workshop half-plane Voronoi framework; a quiet single tile pattern per cell."""
    seeds = [(-12+i*3.2+random.uniform(-.65,.65),
              -10+j*3.2+random.uniform(-.65,.65)) for i in range(9) for j in range(8)]
    for i,s in enumerate(seeds):
        cell = list(OUTLINE)
        for t in seeds:
            if t is s:
                continue
            m = ((s[0]+t[0])/2,(s[1]+t[1])/2)
            d = (-(t[1]-s[1]),t[0]-s[0])
            cell = halfplane(cell,m,(m[0]+d[0],m[1]+d[1]),False)
            if len(cell) < 3:
                break
        if len(cell) >= 3:
            yield i,cell


def ground():
    box('Outer void', (0,0,-.55), (30,26,.16), 'void', bevel=False)
    slab('Floor underlay', OUTLINE, -.06,.4,'tile_dark')
    for i,cell in chunk_cells():
        slab('Voronoi grout bed',cell,-.01,.12,'tile_seam')
        # A .04m inset on each edge makes the irregular chunk outlines readable.
        inset = list(cell)
        for k,p in enumerate(cell):
            q = cell[(k+1)%len(cell)]
            dx,dy = q[0]-p[0],q[1]-p[1]
            length = math.hypot(dx,dy)
            if length < 1e-6:
                continue
            a = (p[0]-dy/length*.025,p[1]+dx/length*.025)
            inset = halfplane(inset,a,(a[0]+dx,a[1]+dy),False)
        if len(inset)<3:
            continue
        role = ('tile','tile_light','tile_dark')[(i+i//8)%3]
        pattern = (i*7+i//8)%3
        angle = math.pi/4 if pattern == 1 else 0
        c,s = math.cos(angle),math.sin(angle)
        local = [(x*c+y*s,-x*s+y*c) for x,y in inset]
        xmin,xmax = min(p[0] for p in local),max(p[0] for p in local)
        ymin,ymax = min(p[1] for p in local),max(p[1] for p in local)
        def tile(poly):
            cut = clip_convex([(x*c-y*s,x*s+y*c) for x,y in poly],inset)
            if len(cut)>=3:
                slab('Chunk tile',cut,0,.08,role)
        if pattern < 2:
            for u in range(math.floor(xmin/.6), math.ceil(xmax/.6)):
                for v in range(math.floor(ymin/.6), math.ceil(ymax/.6)):
                    x,y = u*.6,v*.6
                    tile([(x+.025,y+.025),(x+.575,y+.025),(x+.575,y+.575),(x+.025,y+.575)])
        else:
            # True interlocked herringbone: two perpendicular rectangles per .9m cell.
            for u in range(math.floor(xmin/.3)-4,math.ceil(xmax/.3)+4):
                for v in range(math.floor(ymin/.3)-4,math.ceil(ymax/.3)+4):
                    x,y = u*.3,v*.3
                    if (u+v)%4 == 0:
                        tile([(x+.025,y+.025),(x+.575,y+.025),(x+.575,y+.275),(x+.025,y+.275)])
                    elif (u+v)%4 == 2:
                        tile([(x+.025,y+.025),(x+.275,y+.025),(x+.275,y+.575),(x+.025,y+.575)])
    annulus('Battle circle seam',BATTLE_C,5.96,6.04,.045,.04,'tile_seam',n=96)
    for j in range(12):
        a = j*TAU/12
        box('Battle brass hour tick',(6*math.sin(a),-3.5+6*math.cos(a),.065),
            (.10,.30,.04),'brass',rot=(0,0,-a),bevel=False)


def wall_local(u0,u1,H,openings=(),pilasters=(),low=False):
    """Workshop wall layout, inner face y=0, body y=0..0.5; cut every finish at openings."""
    L,mid,T = u1-u0,(u0+u1)/2,.5
    edges = sorted({u0,u1,*[c-w/2 for c,w,z0,z1 in openings],
                     *[c+w/2 for c,w,z0,z1 in openings]})
    for a,b in zip(edges,edges[1:]):
        spans=[(0,H)]
        for c,w,z0,z1 in openings:
            if c-w/2 <= (a+b)/2 <= c+w/2:
                spans=[(0,z0),(z1,H)]
        for z0,z1 in spans:
            if z1>z0:
                box('Mint wall core',((a+b)/2,T/2,(z0+z1)/2),(b-a,T,z1-z0),'wall',bevel=False)
    def finish_strip(label,z,h,depth,role):
        spans=[(u0,u1)]
        for c,w,z0,z1 in openings:
            if z+h/2>z0 and z-h/2<z1:
                new=[]
                for a,b in spans:
                    if a<c-w/2: new.append((a,min(b,c-w/2)))
                    if b>c+w/2: new.append((max(a,c+w/2),b))
                spans=new
        for a,b in spans:
            if b>a:
                box(label,((a+b)/2,-depth/2,z),(b-a,depth,h),role,bevel=False)
    finish_strip('Wainscot',.55,1.1,.08,'wainscot')
    finish_strip('Skirting',.10,.20,.12,'molding')
    finish_strip('Chair rail',1.06,.08,.14,'molding')
    for j in range(math.ceil(L/.8)):
        x=u0+.4+j*.8
        if x>u1-.1: continue
        if not any(abs(x-c)<w/2+.08 for c,w,z0,z1 in openings):
            frame('Wainscot inset',x,.58,.62,.65,-.09,.04,.04,'molding',False)
    if low:
        finish_strip('Low wall cap',1.08,.04,.6,'molding')
        return
    for j in range(math.ceil(L/.4)):
        x=u0+.18+j*.4
        if x>u1: continue
        for z,h in ((2.4,2.5),(4.1,.6)):
            if not any(abs(x-c)<w/2+.05 and z+h/2>z0 and z-h/2<z1 for c,w,z0,z1 in openings):
                box('Quiet vertical wall stripe',(x,-.025,z),(.10,.05,h),'wall_pat',bevel=False)
    finish_strip('Crown shadow band',4.50,.25,.1,'wainscot')
    finish_strip('Cream crown',4.63,.16,.20,'molding')
    # Dark, open upper edge: no ceiling.
    box('Dark open wall cap',(mid,.25,4.76),(L,.58,.08),'wainscot',bevel=False)
    for x in pilasters:
        box('Pilaster',(x,-.10,2.36),(.32,.20,4.72),'wainscot',bevel=False)
        for z in (.14,1.1,4.48):
            box('Pilaster collar',(x,-.14,z),(.48,.28,.12),'molding',bevel=False)


def room():
    start=len(PARTS)
    wall_local(-12,12,4.8,[(6.5,3.4,1,3.8)],[-10,-3,3,10.8])
    move_group(start,(0,10,0))
    start=len(PARTS)
    wall_local(-10,10,4.8)
    move_group(start,(-12,0,0),(0,0,math.pi/2))
    start=len(PARTS)
    wall_local(-10,10,4.8)
    move_group(start,(12,0,0),(0,0,-math.pi/2))
    start=len(PARTS)
    wall_local(-12,-1.8,1.1,low=True)
    wall_local(1.8,12,1.1,low=True)
    for x in (-1.9,1.9):
        box('Entrance jamb',(x,.25,1.55),(.20,.5,3.1),'wood_dark')
        box('Handle plate',(x,-.04,1.0),(.13,.08,.30),'brass')
    box('Entrance lintel',(0,.25,3.08),(4,.5,.16),'wood_dark')
    move_group(start,(0,-10,0),(0,0,math.pi))
    box('Oxygen rail',(0,9.90,1.80),(4.5,.14,.16),'chrome')
    for x in (-1.5,-.5,.5,1.5):
        box('Outlet socket',(x,9.79,1.8),(.24,.10,.30),'plastic')
        for dx in (-.05,.05):
            box('Outlet recess',(x+dx,9.72,1.8),(.04,.04,.07),'wainscot',bevel=False)


# ---------------------------------------------------------------- cloth / silhouettes
def pleats(label,x0,x1,y,z0,z1,folds,roles=('curtain','curtain_dark')):
    for j in range(folds):
        a=x0+(x1-x0)*j/folds
        b=x0+(x1-x0)*(j+1)/folds
        ya=y+(.075 if j%2 else -.075)
        yb=y+(.075 if (j+1)%2 else -.075)
        verts=[(a,ya,z0),(b,yb,z0),(b,y,z1),(a,y,z1),
               (a,ya+.06,z0),(b,yb+.06,z0),(b,y+.06,z1),(a,y+.06,z1)]
        Part(label,verts,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),
                          (2,6,7,3),(3,7,4,0)],roles[j%2],False)


def window():
    # Opening x +/-1.7, z 1..3.8; origin on the floor beneath the window.
    start=len(PARTS)
    for x in (-1.72,1.72):
        box('Window casing',(x,0,1.53),(.18,.20,2.94),'molding')
    for z in (.12,2.94):
        box('Window casing',(0,0,z),(3.62,.22,.18),'molding')
    box('Night sky',(0,.13,1.53),(3.4,.08,2.70),'night',bevel=False)
    ycyl('Full moon',(.85,.06,2.30),.34,.04,'moon_glow',n=24)
    for x,z in ((-1.3,2.5),(-.7,1.9),(.1,2.6),(.3,1.3),(1.3,1.7),(-1.1,1.0)):
        ball('Star',(x,.04,z),.035,'moon_glow',detail=1)
    box('Window mullion',(0,-.03,1.53),(.08,.16,2.72),'molding')
    box('Window crossbar',(0,-.04,1.6),(3.4,.16,.08),'molding')
    box('Broad sill',(0,-.16,.08),(4,.55,.16),'molding')
    pleats('Left curtain',-2.4,-1.5,-.20,-.88,3.20,8)
    pleats('Right curtain',1.5,2.4,-.20,-.88,3.20,8)
    rod('Curtain rod',(-2.5,-.16,3.23),(2.5,-.16,3.23),.045,'molding')
    lathe('Flower pot',[(0,.13),(.22,.17),(.26,.18)],'vase',n=10,bevel=False).transform((-.95,-.25,.16))
    for j in range(4):
        a=j*TAU/4
        rod('Plant stem',(-.95,-.25,.38),(-.95+.13*math.cos(a),-.25+.10*math.sin(a),.7),.022,'leaf')
        lump('Plant leaf',(-.95+.13*math.cos(a),-.25+.1*math.sin(a),.65),(.20,.09,.16),'leaf')
    lump('White flower',(-.95,-.25,.76),(.18,.16,.16),'flower')
    move_group(start,(0,0,.88))


def bed():
    # Overall footprint X 1.25, Y 2.4; head = +Y.
    for x in (-.48,.48):
        for y in (-.98,.98):
            ycyl('Caster wheel',(x,y,.10),.10,.08,'steel',n=12)
            rod('Caster fork',(x,y,.12),(x,y,.34),.035,'chrome')
            box('Bed leg',(x,y,.40),(.08,.08,.28),'steel')
    box('Underbed chassis',(0,0,.46),(1.16,2.22,.12),'steel')
    box('Mattress shadow seam',(0,-.30,.61),(1.12,1.72,.18),'sheet_shade',bevel=False)
    box('Flat mattress top',(0,-.30,.715),(1.10,1.72,.07),'sheet',bevel=False)
    # The flat mattress joins a 30-degree raised northern section at y=.35, z=.75.
    start=len(PARTS)
    box('Raised mattress underside',(0,.43,-.09),(1.10,.86,.18),'sheet_shade',bevel=False)
    box('Raised mattress sheet',(0,.43,-.025),(1.10,.86,.05),'sheet',bevel=False)
    lump('Pillow',(0,.66,.065),(.86,.38,.16),'pillow',detail=2)
    for x in (-.45,.45):
        rod('Pillow piping',(x,.5,.10),(x,.80,.10),.02,'sheet_shade')
    move_group(start,(0,.35,.75),(math.radians(30),0,0))
    for y,h in ((-1.14,.80),(1.14,1.35)):
        for x in (-.585,.585):
            rod('Head foot post',(x,y,.32),(x,y,h),.04,'chrome')
        box('Wood end panel',(0,y,h-.20),(1.09,.08,.32),'wood_light')
        box('End panel silver inlay',(0,y-.05,h-.20),(.80,.04,.07),'steel',bevel=False)
        rod('End crown',(-.58,y,h),(.58,y,h),.045,'chrome')
    for x in (-.605,.605):
        for y in (-.55,.40):
            rod('Folding rail upright',(x,y,.56),(x,y,.98),.025,'chrome')
        for z in (.78,.99):
            rod('Folding side rail',(x,-.74,z),(x,.65,z),.025,'steel')
    for x in (-.42,-.20,.20,.42):
        ribbon_solid('Sheet fold',[(x,-.72,.76),(x+.04,-.2,.77),(x,.30,.75)],.055,'sheet_shade')
    box('Folded blanket',(0,-.89,.81),(1.05,.40,.12),'blanket',bevel=False)
    for x in (-.4,-.2,0,.2,.4):
        box('Blanket check stripe',(x,-.89,.88),(.06,.4,.04),'blanket_pat',bevel=False)
    for y in (-1.02,-.83):
        box('Blanket check cross',(0,y,.905),(1.05,.06,.04),'blanket_pat',bevel=False)


def old_man_lying():
    # Mattress-centred origin (specific silhouette pivot exception, preserved in KEEP).
    # Overall toes to crown 1.72m, with chest under the raised sheet and head toward +Y.
    rings=[]
    for y,w,z in ((-.89,.17,.08),(-.75,.23,.13),(-.38,.24,.17),
                  (-.05,.24,.19),(.20,.25,.27),(.30,.27,.30)):
        rings.append([(-w,y,.02),(-w,y,z*.6),(-w*.65,y,z),(w*.65,y,z),
                      (w,y,z*.6),(w,y,.02)])
    ring_skin('Continuous chest high shadow sheet',rings,'shadow')
    lump('Stooped shoulders',(0,.47,.36),(.57,.28,.25),'shadow',detail=2)
    rod('Thin neck',(0,.50,.35),(0,.63,.43),.065,'shadow',n=8)
    # Faceless bald oval: swept back crown, smaller jaw; no eyes/nose/mouth.
    lump('Old bald head',(0,.70,.49),(.29,.34,.31),'shadow',detail=2)
    lump('Receding jaw',(0,.57,.47),(.23,.20,.22),'shadow',detail=1)
    tube('Right arm on sheet',[(.24,.42,.35),(.36,.19,.24),(.47,.08,.18),(.51,-.09,.16)],.055,'shadow',n=8)
    lump('Resting hand',(.51,-.17,.16),(.12,.22,.09),'shadow',detail=2)
    tube('Left shoulder fold',[(-.24,.48,.38),(-.30,.29,.24),(-.26,.06,.18)],.055,'shadow',n=6)
    # Narrow warm silhouette rim; dominant surface remains shadow as explicitly specified.
    tube('Head rim',[(-.13,.64,.53),(-.12,.78,.62),(0,.84,.64),(.12,.78,.62),(.14,.66,.54)],.021,'shadow_rim')
    tube('Sheet chest boundary',[(-.27,.29,.30),(-.10,.25,.33),(.08,.26,.33),(.27,.22,.29)],.022,'shadow_rim')
    tube('Hand rim',[(.43,-.25,.19),(.53,-.25,.20),(.56,-.11,.20),(.48,.05,.22)],.02,'shadow_rim')
    tube('Sheet side boundary',[(-.24,-.87,.11),(-.28,-.4,.22),(-.24,.12,.27)],.02,'shadow_rim')
    ys=[v.y for p in PARTS for v in p.verts]
    move_group(0,scale=(1,1.72/(max(ys)-min(ys)),1))
# ---------------------------------------------------------------- bedside furniture / clock
def nightstand():
    for x in (-.28,.28):
        for y in (-.20,.20):
            box('Cabinet foot',(x,y,.08),(.08,.08,.16),'walnut')
    box('Bedside carcass',(0,0,.40),(.67,.51,.58),'walnut')
    box('Oak tabletop',(0,0,.68),(.70,.55,.08),'wood')
    for z in (.30,.53):
        box('Drawer panel',(0,-.273,z),(.57,.045,.20),'wood')
        frame('Drawer walnut inset',0,z,.52,.16,-.301,.04,.04,'walnut')
        rod('Drawer handle',(-.09,-.34,z),(.09,-.34,z),.022,'brass')
        for x in (-.09,.09):
            rod('Pull mount',(x,-.30,z),(x,-.34,z),.023,'brass')
    # .55m tall amber lamp standing on the .72m tabletop.
    start=len(PARTS)
    cylinder('Lamp base',(0,0,.035),.12,.07,'brass',n=12,bevel=False)
    rod('Lamp stem',(0,0,.07),(0,0,.37),.025,'brass')
    lathe('Amber lamp shade',[(.32,.15),(.50,.095),(.53,.075)],'lamp_glow',n=12,bevel=False)
    flat_hoop('Shade trim',(0,0,.32),.15,.04,.04,'brass',n=12)
    ball('Lamp finial',(0,0,.53),.02,'brass',detail=1)
    move_group(start,(-.19,.12,.72))
    # Solid stylised glass, with a visible water surface, avoids fragile transparent shells.
    lathe('Water cup',[(0,.055),(.14,.065),(.16,.065)],'glass',n=10,bevel=False).transform((.24,-.16,.72))
    cylinder('Water surface',(.24,-.16,.85),.05,.04,'vase',n=10,bevel=False)
    for x in (-.06,.07):
        flat_hoop('Reading glasses lens rim',(x,-.17,.755),.055,.04,.04,'brass',n=10)
    rod('Glasses bridge',(-.005,-.17,.76),(.015,-.17,.76),.02,'brass')
    for x in (-.11,.12):
        rod('Glasses folded temple',(x,-.17,.75),(x,-.04,.75),.02,'brass')
    lathe('Flower vase',[(0,.075),(.05,.09),(.17,.075),(.23,.04),(.27,.05)],'vase',n=10,bevel=False).transform((.20,.13,.72))
    for j,(dx,dy,h) in enumerate(((-.07,0,.44),(.04,.04,.48),(.08,-.04,.39))):
        end=(.20+dx,.13+dy,.72+h)
        rod('Flower stem',(.20,.13,.94),end,.02,'leaf')
        lump('Flower leaf',(.20+dx*.6,.13+dy*.6,1.03),(.12,.06,.09),'leaf')
        for k in range(5):
            a=k*TAU/5
            lump('White petal',(end[0]+.045*math.cos(a),end[1]+.045*math.sin(a),end[2]),(.085,.075,.06),'flower')
        ball('Flower heart',end,.026,'brass',detail=1)


def picture_book():
    box('Old book covers',(0,0,.025),(.50,.34,.05),'book_cover',bevel=False)
    for side in (-1,1):
        box('Open page block',(side*.123,0,.065),(.235,.31,.055),'book_page',
            rot=(0,side*math.radians(5),0),bevel=False)
        box('Gold sunset band',(side*.123,.04,.104),(.21,.065,.04),'book_gold',bevel=False)
        for y in (-.105,-.05):
            box('Illustration story line',(side*.12,y,.105),(.16,.04,.04),'wood_light',bevel=False)
    rod('Book spine',(0,-.17,.045),(0,.17,.045),.025,'book_cover')
    # A small, clear cat hero on the right page: ears, head, body, curled tail.
    disc('Cat head',(.11,.055),.052,.13,.04,'shadow',n=10)
    slab('Cat left ear',[(.061,.08),(.064,.15),(.10,.10)],.14,.04,'shadow')
    slab('Cat right ear',[(.12,.10),(.16,.15),(.16,.07)],.14,.04,'shadow')
    lump('Cat body',(.11,-.015,.115),(.09,.12,.05),'shadow')
    tube('Cat tail',[(.15,-.055,.13),(.20,-.045,.13),(.20,.01,.13)],.02,'shadow')
    for x,y in ((-.23,-.14),(.23,.14)):
        slab('Worn cover corner',[(x,y),(x-.05*(1 if x>0 else -1),y),(x,y-.05*(1 if y>0 else -1))],
             .065,.04,'wood_light')


def wall_clock():
    # Backmost plane y=0, face centre z=0. KEEP prevents any bounding-box recenter.
    ycyl('Walnut clock case',(0,-.10,0),1.10,.20,'walnut',n=40)
    hoop('Carved broad rim',(0,-.16,0),1.11,.16,.20,'walnut',n=40)
    ycyl('Ivory dial',(0,-.225,0),.95,.05,'dial_face',n=40)
    hoop('Brass bezel',(0,-.25,0),1.01,.08,.08,'brass',n=40)
    hoop('Gold inner bead',(0,-.28,0),.94,.04,.04,'gold',n=40)
    for j in range(60):
        a=j*TAU/60
        major=j%5==0
        r=.86
        box('Clock minute tick',(r*math.sin(a),-.29,r*math.cos(a)),
            (.04,.04,.105 if major else .055),'shawl_red' if j==0 else 'dial_mark',
            rot=(0,a,0),bevel=False)
    for j in range(12):
        a=j*TAU/12
        numeral(ROMAN[j],(.68*math.sin(a),-.29,.68*math.cos(a)),.15,'dial_mark',depth=.04)
    for x in (-.42,.42):
        scroll('Clock crest scroll',(x,-.14,1.08),.18,1 if x>0 else -1,'gold')
    ball('Golden finial',(0,-.12,1.29),.09,'gold',detail=2)
    box('Pendulum box back',(0,-.05,-1.28),(.62,.10,.54),'walnut')
    box('Pendulum dark window',(0,-.12,-1.28),(.43,.05,.42),'shadow',bevel=False)
    frame('Pendulum brass case',0,-1.28,.60,.54,-.18,.07,.10,'brass')
    rod('Short pendulum rod',(0,-.18,-1.04),(0,-.18,-1.39),.022,'gold')
    ycyl('Short pendulum bob',(0,-.22,-1.40),.10,.05,'brass',n=16)


def clock_hand(length,kind):
    # Chunky .04m minimum thickness; tips reach exactly the specified +Z length.
    role='shawl_red' if kind=='second' else 'steel_dark'
    width=.04 if kind=='second' else (.075 if kind=='hour' else .05)
    poly=[(-width/2,-.10),(width/2,-.10),(width/2,length-.10),
          (0,length),(-width/2,length-.10)]
    extrude('Clock hand',poly,.04,role,y=-.04,bevel=False)
    ycyl('Axis hub',(0,-.04,0),.055,.05,'steel_dark' if kind=='second' else 'brass',n=10)
    ycyl('Red counterweight',(0,-.04,-.13),.055,.04,'shawl_red',n=10)


# ---------------------------------------------------------------- hospital equipment
def iv_stand():
    for j in range(5):
        a=j*TAU/5
        p=(.36*math.cos(a),.36*math.sin(a),.10)
        rod('Five star base',(0,0,.16),p,.035,'chrome')
        ycyl('Caster wheel',(p[0],p[1],.07),.07,.065,'steel_dark',n=8)
    rod('IV chrome mast',(0,0,.13),(0,0,2.05),.03,'chrome',n=10)
    cylinder('Height collar',(0,0,1.03),.065,.10,'plastic',n=10)
    tube('IV hook',[(0,0,2.04),(.16,0,2.08),(.24,0,2.03),(.24,0,1.98)],.025,'chrome')
    tube('Spare hook',[(0,0,2.04),(-.16,0,2.08),(-.22,0,2.02),(-.20,0,1.98)],.025,'chrome')
    box('Hanging IV bag',(.24,0,1.84),(.26,.10,.30),'iv_bag',bevel=False)
    box('Visible fluid volume',(.24,-.061,1.80),(.20,.04,.16),'iv_fluid',bevel=False)
    box('Bag identification strip',(.24,-.065,1.92),(.18,.04,.06),'plastic',bevel=False)
    rod('Bag connector',(.24,0,1.69),(.24,0,1.62),.025,'tube')
    # Open central chamber plus visible shell bands, centre exactly 1.52.
    # Walls are opaque grey role surfaces in FBX, so leave the front aperture
    # open to keep the separately animated droplet readable.
    for x in (.19,.29):
        box('Chamber side wall',(x,0,1.52),(.04,.08,.14),'tube',bevel=False)
    box('Chamber back',(.24,.04,1.52),(.10,.04,.14),'tube',bevel=False)
    for z in (1.45,1.59):
        cylinder('Chamber collar',(.24,0,z),.07,.04,'plastic',n=10,bevel=False)
    tube('Sagging bedside IV line',[(.24,0,1.43),(.15,0,1.18),(-.15,-.04,.55),
                                  (.15,-.12,.37),(.53,-.15,.50),(.80,-.16,.82)],.022,'tube')
    cylinder('Line regulator',(.18,0,1.27),.04,.10,'iv_fluid',n=8,bevel=False)


def iv_drop():
    # .06m, centre at the origin, single fluid material as explicitly specified.
    lathe('Animated drop',[(-.03,0),(-.02,.023),(0,.025),(.018,.014),(.03,0)],
          'iv_fluid',n=8,bevel=False)


def monitor():
    for x,y in ((-.24,-.20),(.24,-.20),(-.24,.20),(.24,.20)):
        rod('Monitor base spoke',(0,0,.12),(x,y,.08),.035,'steel')
        ycyl('Monitor wheel',(x,y,.07),.07,.06,'plastic_dark',n=8)
    rod('Monitor pole',(0,0,.12),(0,0,1.17),.04,'chrome',n=10)
    box('Monitor tray',(0,0,.83),(.44,.32,.06),'plastic')
    start=len(PARTS)
    box('Monitor housing',(0,0,1.19),(.69,.22,.50),'plastic')
    box('Dark screen bezel',(0,-.135,1.21),(.61,.07,.44),'plastic_dark')
    box('Black screen',(0,-.18,1.21),(.55,.04,.40),'screen',bevel=False)
    # ECG waveform and number blocks are raised .04m bars.
    pts=[(-.245,1.20),(-.18,1.20),(-.15,1.23),(-.12,1.18),(-.09,1.20),
         (-.055,1.20),(-.02,1.33),(.025,1.08),(.07,1.20),(.14,1.20),(.17,1.24),(.21,1.20),(.25,1.20)]
    tube('Green ECG trace',[(x,-.215,z) for x,z in pts],.02,'screen_glow',n=4)
    # Legible seven-segment 59, not a text texture.
    segments={'5':(0,1,3,5,6),'9':(0,1,2,3,5,6)}
    for x,char in ((.10,'5'),(.19,'9')):
        for k in segments[char]:
            if k in (0,3,6):
                zz={0:1.365,3:1.315,6:1.265}[k]
                box('ECG numeral',(x,-.217,zz),(.06,.04,.04),'screen_glow',bevel=False)
            else:
                xx=x+(-.025 if k in (1,4) else .025)
                zz=1.34 if k in (1,2) else 1.29
                box('ECG numeral',(xx,-.217,zz),(.04,.04,.05),'screen_glow',bevel=False)
    for x in (-.21,-.08,.05):
        ycyl('Control button',(x,-.16,.99),.025,.04,'chrome',n=8)
    move_group(start,rot=(0,0,math.radians(12)))
    tube('ECG cable',[(.28,0,1.04),(.36,-.03,.71),(.33,-.12,.35),(.55,-.28,.12),(.74,-.18,.12)],.022,'tube')
    box('Cable plug',(.74,-.18,.12),(.10,.06,.07),'plastic_dark')


def visitor_chair():
    for x in (-.29,.29):
        for y in (-.27,.27):
            beam('Chair leg',(x,y,0),(x*.92,y*.92,.54),.07,.07,'walnut')
    box('Seat frame',(0,0,.53),(.68,.64,.09),'walnut')
    box('Padded seat',(0,-.01,.61),(.59,.55,.10),'blanket',bevel=False)
    for x in (-.30,.30):
        beam('Back upright',(x,.27,.48),(x,.34,1.25),.07,.07,'wood')
        rod('Arm support',(x,-.20,.57),(x,-.20,.86),.035,'wood')
        box('Wood armrest',(x,.02,.89),(.12,.63,.07),'wood')
        box('Arm brass inlay',(x,-.04,.93),(.04,.36,.04),'brass',bevel=False)
    box('Chair back',(0,.32,1.04),(.60,.08,.38),'wood')
    box('Back cushion',(0,.255,1.02),(.48,.06,.27),'blanket',bevel=False)
    for j in range(6):
        x=-.22+j*.088
        pts=[(x,-.18,.69),(x,.10,.70),(x,.23,.92),(x,.30,1.28),
             (x,.41,1.12),(x,.44,.61+random.uniform(-.04,.04))]
        ribbon_solid('Red shawl fold',pts,.095,'shawl_red' if j%2==0 else 'shawl_red_dark',.05)
        for end in (pts[0],pts[-1]):
            rod('Shawl fringe',end,(end[0],end[1]-.02,end[2]-.11),.02,'shawl_red_dark',n=4)


def cabinet():
    for x in (-.78,.78):
        box('Cabinet foot',(x,0,.08),(.12,.46,.16),'wood_dark')
    box('Ward cabinet',(0,0,.99),(1.8,.60,1.78),'wood_light')
    for x in (-.43,.43):
        box('Cabinet door',(x,-.325,1.01),(.80,.05,1.54),'plastic')
        frame('Door oak trim',x,1.01,.78,1.52,-.36,.06,.04,'wood_light')
        rod('Cabinet pull',(x*.35,-.41,.90),(x*.35,-.41,1.13),.025,'brass')
    box('Top slab',(0,0,1.93),(1.8,.6,.14),'wood_light')
    for j in range(3):
        x=-.65+j*.20
        cylinder('Medicine bottle',(x,.06,2.10),.07,.20,'iv_bag',n=10,bevel=False)
        cylinder('Bottle cap',(x,.06,2.22),.075,.04,'brass',n=10,bevel=False)
        box('Bottle label',(x,-.016,2.10),(.10,.04,.07),'iv_fluid',bevel=False)
    for j in range(3):
        box('Folded towel',(.47,.01,2.04+j*.08),(.52,.40,.08),'sheet' if j%2==0 else 'sheet_shade',bevel=False)
        box('Towel folded seam',(.47,-.20,2.05+j*.08),(.42,.04,.04),'sheet_shade',bevel=False)


def curtain_rail():
    # Lowest geometry .30m: preserve through post-recenter mounting offset in finish.
    rod('Ceiling rail',(-2.5,0,3.4),(2.5,0,3.4),.04,'chrome',n=10)
    for x in (-2.4,2.4):
        rod('Rail hanger',(x,0,3.4),(x,0,3.68),.035,'steel_dark')
        cylinder('Hanger plate',(x,0,3.70),.09,.04,'brass',n=10,bevel=False)
    pleats('Half drawn privacy curtain',.05,2.45,0,.30,3.28,18)
    for j in range(19):
        x=.05+j*2.4/18
        hoop('Curtain eyelet',(x,0,3.35),.07,.04,.04,'chrome',n=10)
    for z in (.36,3.22):
        box('Curtain hem',(1.25,-.04,z),(2.4,.08,.07),'curtain_dark',bevel=False)
# ---------------------------------------------------------------- reaper / door / pickup
def reaper_body():
    # Faceted, uneven hanging cloak. Neck pivot is intentionally centred above the hem.
    n=18
    rings=[]
    for level,(z,rx,ry) in enumerate(((.05,.68,.46),(.30,.62,.43),(1.10,.40,.30),
                                     (1.90,.48,.30),(2.16,.34,.23),(2.30,.14,.13))):
        ring=[]
        for j in range(n):
            a=j*TAU/n
            ripple=1+.06*math.cos(j*math.pi)+random.uniform(-.025,.025)
            zz=z+(.11 if j%3==0 else -.03 if j%3==1 else .01) if level==0 else z
            ring.append((rx*math.cos(a)*ripple,ry*math.sin(a)*ripple,zz))
        rings.append(ring)
    # Make the body floor exactly zero so its neck remains 2.30 after finish().
    rings[0][13]=(rings[0][13][0],rings[0][13][1],0)
    ring_skin('Tattered reaper cloak',rings,'shadow')
    tube('Warm ragged hem',[(x,y,z+.025) for x,y,z in rings[0]]+[tuple(Vector(rings[0][0])+Vector((0,0,.025)))],
         .022,'shadow_rim',n=5)
    for side in (-1,1):
        tube('Cloak front opening',[(side*.13,-.13,2.29),(side*.29,-.26,1.93),
                                  (side*.21,-.31,1.12),(side*.36,-.40,.28)],.022,'shadow_rim',n=5)
    # Both long sleeves and hands grasp the vertical scythe shaft.
    tube('Left draped sleeve',[(-.37,-.03,1.99),(-.51,-.20,1.60),(-.16,-.40,1.42),(.76,.08,1.48)],
         .105,'shadow',n=7)
    tube('Right draped sleeve',[(.35,-.04,1.98),(.48,-.24,1.81),(.75,.08,1.91)],
         .105,'shadow',n=7)
    for z in (1.48,1.91):
        lump('Faceless shadow hand',(.77,.08,z),(.17,.14,.12),'shadow',detail=2)
        rod('Hand edge',(.70,0,z+.04),(.84,0,z+.04),.02,'shadow_rim')
    # A 2.0m neck + .705m hood gives the requested approximately 2.7m figure.
    move_group(0,scale=(1,1,2.0/2.3))
    # Exactly 3.0m staff, .07m thick; blade span 1.4m, turned inward toward the patient.
    rod('Scythe staff',(.80,.10,.04),(.80,.10,3.04),.035,'shadow',n=8)
    for z in (.18,1.45,1.94,2.80):
        cylinder('Scythe binding',(.80,.10,z),.055,.07,'shadow_rim',n=8,bevel=False)
    blade=[(.80,3.00),(.51,3.28),(.16,3.40),(-.20,3.37),(-.48,3.20),(-.60,2.98),
           (-.36,3.11),(-.10,3.20),(.20,3.20),(.51,3.08)]
    extrude('Sweeping scythe blade',blade,.07,'shadow',y=.10,bevel=False)
    tube('Scythe blade luminous edge',[(-.60,.055,2.98),(-.36,.055,3.11),(-.10,.055,3.20),
                                     (.20,.055,3.20),(.51,.055,3.08),(.80,.055,3.00)],.022,'shadow_rim')
    # Bound-box centre is asymmetrical because of staff: compensate in finish() so
    # the cloak axis, not the weapon extent, is the actual assembly datum.


def reaper_head():
    hood_start=len(PARTS)
    # Hood is an OPEN, thick ring around a clock. No face, eyes or skull.
    outer=[(-.32,.02),(-.43,.34),(-.35,.65),(-.14,.87),(0,.94),
           (.17,.85),(.36,.63),(.43,.31),(.30,.02)]
    inner=[(-.20,.10),(-.28,.34),(-.22,.57),(-.09,.69),(0,.72),
           (.10,.67),(.23,.55),(.28,.31),(.19,.10)]
    # The top-down camera sees the peak and the clock tilted slightly upward.
    for j in range(len(outer)-1):
        p,q=outer[j],outer[j+1]
        r,s=inner[j+1],inner[j]
        extrude('Hood thick fold',[p,q,r,s],.32,'shadow',y=.02,bevel=False)
    extrude('Hood back',outer,.10,'shadow',y=.22,bevel=False)
    tube('Hood warm inner edge',[(x,-.16,z) for x,z in inner],.028,'shadow_rim')
    # The front is pitched up 16 degrees so the ivory dial reads at 57 degrees.
    start=len(PARTS)
    ycyl('Clock face in hood',(0,-.20,.37),.255,.055,'dial_face',n=32)
    hoop('Clock face rim',(0,-.24,.37),.28,.04,.05,'shadow_rim',n=32)
    for j in range(12):
        a=j*TAU/12
        box('Hood dial tick',(.215*math.sin(a),-.26,.37+.215*math.cos(a)),
            (.055,.055,.075),'shadow',rot=(0,a,0),bevel=False)
    for frac,length in (((11+59/60)/12,.135),(59/60,.20)):
        a=frac*TAU
        beam('Fixed 1159 hand',(0,-.29,.37),(length*math.sin(a),-.29,.37+length*math.cos(a)),
             .055,.055,'shadow',bevel=False)
    ycyl('Clock face hub',(0,-.29,.37),.028,.04,'shadow_rim',n=8)
    # Rotate around face centre, keeping the neck at zero.
    for p in PARTS[start:]:
        p.transform((0,0,-.37))
        p.transform((0,0,.37),(math.radians(-16),0,0))
    tube('Hood shoulder lip',[(-.30,-.08,.04),(0,-.19,0),(.30,-.08,.04)],.028,'shadow_rim')
    move_group(hood_start,scale=(.83,.83,.75))


def door():
    # Door-frame overall width exactly 3.6, semi-open leaf rotates around west hinge.
    for x in (-1.7,1.7):
        box('Door jamb',(x,0,1.50),(.20,.28,3),'wood_dark')
        box('Jamb cream lining',(x,-.17,1.50),(.08,.06,2.92),'molding')
    box('Door lintel',(0,0,3.05),(3.6,.28,.18),'wood_dark')
    start=len(PARTS)
    box('Part open ward door',(1.54,0,1.47),(3.08,.12,2.94),'wood_light')
    # Inset glazed window (solid glass role panel in its trim).
    box('Small door glazing',(1.54,-.085,2.12),(.76,.05,.69),'glass',bevel=False)
    frame('Window molding',1.54,2.12,.90,.83,-.11,.07,.06,'molding')
    frame('Door raised lower panel',1.54,.72,2.64,1.05,-.085,.08,.05,'wood_dark')
    rod('Chrome door handle',(2.72,-.20,1.25),(2.95,-.20,1.25),.035,'chrome')
    rod('Door handle spindle',(2.82,-.08,1.25),(2.82,-.20,1.25),.035,'chrome')
    move_group(start,(-1.60,0,0),(0,0,math.radians(38)))
    for z in (.30,1.50,2.70):
        cylinder('Door hinge',(-1.60,0,z),.055,.18,'brass',n=10,bevel=False)


def hourglass():
    for z in (.04,.66):
        flat_hoop('Open hourglass end',(0,0,z),.23,.055,.08,'brass',n=16)
        flat_hoop('End gold inlay',(0,0,z+.045),.22,.04,.04,'gold',n=16)
    for j in range(4):
        a=j*TAU/4+TAU/8
        rod('Hourglass pillar',(.18*math.cos(a),.18*math.sin(a),.08),
            (.18*math.cos(a),.18*math.sin(a),.62),.025,'wood_dark')
    # Silhouette glass back halves, no full opaque bulb hiding the sand.
    for upper in (False,True):
        rings=[(.10,.13),(.15,.15),(.24,.12),(.33,.035)]
        if upper: rings=[(.70-z,r) for z,r in reversed(rings)]
        shell=[]
        for z,r in rings:
            shell.append([(r*math.cos(j*math.pi/8),r*math.sin(j*math.pi/8),z) for j in range(9)])
        # Back half is a chunky solid curved wall .04m thick.
        for j in range(8):
            for k in range(len(shell)-1):
                p,q=shell[k][j],shell[k][j+1]
                r,s=shell[k+1][j+1],shell[k+1][j]
                verts=[p,q,r,s]+[(x,y+.04,z) for x,y,z in (p,q,r,s)]
                Part('Glass half shell',verts,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),
                                               (2,6,7,3),(3,7,4,0)],'glass',False)
    lathe('Lower sand cone',[(.10,.12),(.13,.12),(.24,.025)],'iv_fluid',n=12,bevel=False)
    lathe('Upper remaining sand',[(.36,.025),(.49,.11),(.54,.11)],'iv_fluid',n=12,bevel=False)
    rod('Falling sand stream',(0,0,.24),(0,0,.40),.02,'iv_fluid',n=6)
    flat_hoop('Glass neck collar',(0,0,.35),.055,.02,.04,'brass',n=12)


# ---------------------------------------------------------------- export (workshop framework)
KEEP = {'Ground','Room','WallClock','HandHour','HandMinute','HandSecond',
        'ReaperHead','IVDrop','OldManLying','CurtainRail'}
# Explicit per-piece role exceptions: silhouettes stay shadow + rim, droplet stays fluid.
MIN_ROLES = {'HandHour':2,'HandMinute':2,'HandSecond':2,'OldManLying':2,
             'ReaperBody':2,'ReaperHead':3,'IVDrop':1}

SPECS = [
    ('Ground',ground,45000), ('Room',room,28000), ('Window',window,6000),
    ('Bed',bed,10000), ('OldManLying',old_man_lying,4000),
    ('Nightstand',nightstand,6000), ('PictureBook',picture_book,1500),
    ('WallClock',wall_clock,6000),
    ('HandHour',lambda: clock_hand(.50,'hour'),500),
    ('HandMinute',lambda: clock_hand(.78,'minute'),500),
    ('HandSecond',lambda: clock_hand(.85,'second'),300),
    ('IVStand',iv_stand,3000), ('IVDrop',iv_drop,120),
    ('Monitor',monitor,4000), ('VisitorChair',visitor_chair,4000),
    ('Cabinet',cabinet,4000), ('CurtainRail',curtain_rail,5000),
    ('ReaperBody',reaper_body,5000), ('ReaperHead',reaper_head,2500),
    ('Door',door,3000), ('Hourglass',hourglass,1500),
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
        # Assembly attachments must follow the exact same workshop recenter.
        if name == 'ReaperBody':
            ANCHORS[name]['neck']=tuple(Vector(REAPER_NECK)-Vector((mid.x,mid.y,lo.z)))
        if name == 'IVStand':
            ANCHORS[name]['drip_chamber']=tuple(Vector(IV_CHAMBER)-Vector((mid.x,mid.y,lo.z)))
    elif name.startswith('Num_'):
        pass
    obj.data.update()
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    need = MIN_ROLES.get(name, 2 if name.startswith('Num_') else 5)
    assert len(used) >= need, (name, 'needs role materials', need, used)
    assert len(used) <= 8, (name, 'too many role materials', used)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    for mat in obj.data.materials:
        shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        assert tuple(shader.inputs['Base Color'].default_value)==(.5,.5,.5,1), mat.name
    if name == 'Bed':
        assert abs(lo.z)<1e-6, ('Mattress datum moved',lo.z)
    if name == 'IVStand':
        assert abs(ANCHORS[name]['drip_chamber'][2]-1.52)<1e-6
    destination = OUTPUT / ('Kit_H_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 3) for d in obj.dimensions)} roles {len(used)} raw {raw}', flush=True)
    return obj


def preview(objects):
    """Three labelled rows, plus assembly copies to verify the two silhouette mounts."""
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=32
    scene.cycles.use_denoising=True
    scene.view_settings.view_transform='Standard'
    scene.view_settings.look='None'
    scene.render.resolution_x,scene.render.resolution_y=3000,1900
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    if scene.world is None:
        scene.world=bpy.data.worlds.new('Deep night blue')
    scene.world.color=(.06,.08,.16)
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.06,.08,.16,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
    # Only after every FBX is written: change both display and node colours for PNG.
    for role,mat in MATS.items():
        mat.diffuse_color=(*PALETTE[role],1)
        shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        shader.inputs['Base Color'].default_value=(*PALETTE[role],1)
        if role in ('screen_glow','moon_glow','lamp_glow','shadow_rim'):
            shader.inputs['Emission Color'].default_value=(*PALETTE[role],1)
            shader.inputs['Emission Strength'].default_value=.18 if role=='shadow_rim' else .4
    by={o.name:o for o in objects}
    by['Ground'].hide_render=True
    rows=[
        [('Room',.23),('Window',.85),('Door',.85),('CurtainRail',.85)],
        [('Bed',2.0),('IVStand',1.45),('Monitor',1.8),('Nightstand',2.0),
         ('VisitorChair',2.0),('Cabinet',1.25),('ReaperBody',1.2)],
        [('WallClock',1.15),('PictureBook',4),('Hourglass',2.7),('HandHour',3),
         ('HandMinute',3),('HandSecond',3),('IVDrop',12),('OldManLying',2.2),('ReaperHead',2.1)],
    ]
    review=[]
    labels=[]
    def label(text,x,y):
        cu=bpy.data.curves.new('Review caption','FONT')
        cu.body=text
        cu.align_x='CENTER'
        cu.size=.21
        cu.extrude=.002
        ob=bpy.data.objects.new('Caption '+text,cu)
        scene.collection.objects.link(ob)
        ob.location=(x,y,.02)
        cu.materials.append(MATS['molding'])
        review.append(ob)
        labels.append(ob)
    def lay(row,y):
        extents=[]
        for name,scale in row:
            obj=by[name]
            obj.scale=(scale,)*3
        bpy.context.view_layer.update()
        for name,scale in row:
            obj=by[name]
            cs=[obj.matrix_world@Vector(c) for c in obj.bound_box]
            extents.append((min(v.x for v in cs),max(v.x for v in cs),
                            min(v.y for v in cs),max(v.y for v in cs),min(v.z for v in cs)))
        gap=1.05
        total=sum(b-a for a,b,_,_,_ in extents)+gap*(len(row)-1)
        x=-total/2
        for (name,scale),(a,b,c,d,zlo) in zip(row,extents):
            obj=by[name]
            obj.location=(x-a,y-(c+d)/2,-zlo)
            x+=b-a+gap
            review.append(obj)
            label(name,obj.location.x+(a+b)/2,y-(d-c)/2-.48)
    for row,y in zip(rows,(8,0,-6)):
        lay(row,y)
    # Copies are preview-only and were never selected for FBX export.
    def mount_copy(name,parent,offset,rotation=0):
        src=by[name]
        ob=bpy.data.objects.new('Assembly '+name,src.data.copy())
        scene.collection.objects.link(ob)
        ob.scale=parent.scale
        ob.location=parent.location+Vector(tuple(offset[i]*parent.scale[i] for i in range(3)))
        ob.rotation_euler.z=rotation
        review.append(ob)
        return ob
    mount_copy('OldManLying',by['Bed'],(0,0,MATTRESS_TOP))
    mount_copy('ReaperHead',by['ReaperBody'],ANCHORS['ReaperBody']['neck'])
    for name,angle in (('HandHour',-(11+59/60)*TAU/12),
                       ('HandMinute',-59*TAU/60),('HandSecond',-59*TAU/60)):
        ob=mount_copy(name,by['WallClock'],(0,-.30,0))
        ob.rotation_euler.y=-angle
    mount_copy('IVDrop',by['IVStand'],ANCHORS['IVStand']['drip_chamber'])
    bpy.context.view_layer.update()
    for name,loc,energy,size in (
        ('Soft key',(-6,-10,25),3800,15),
        ('Cool fill',(9,8,18),2200,12),
        ('Front dial fill',(0,-18,12),1000,14)):
        light=bpy.data.objects.new(name,bpy.data.lights.new(name,'AREA'))
        scene.collection.objects.link(light)
        light.location=loc
        light.data.energy,light.data.shape,light.data.size=energy,'DISK',size
        light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
    cam=bpy.data.objects.new('Cam',bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera=cam
    cam.data.type='ORTHO'
    a=math.radians(57)
    cam.location=Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.view_layer.update()
    inv=cam.matrix_world.inverted()
    corners=[inv@(obj.matrix_world@Vector(c)) for obj in review for c in obj.bound_box]
    left,right=min(v.x for v in corners),max(v.x for v in corners)
    low,high=min(v.y for v in corners),max(v.y for v in corners)
    cam.location+=cam.rotation_euler.to_matrix()@Vector(((left+right)/2,(low+high)/2,0))
    aspect=scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale=max(right-left,(high-low)*aspect)*1.08
    PREVIEW.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
    print('PREVIEW '+str(PREVIEW),flush=True)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length='METRIC',1.0
    materials()
    load_font()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    out=[]
    for i,(name,build,budget) in enumerate(SPECS):
        PARTS=[]
        random.seed(51159+i*977)
        build()
        out.append(finish(name,budget))
    assert len(out)==21
    for name,a in ANCHORS.items():
        print('ANCHOR',name,a,flush=True)
    preview(out)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
