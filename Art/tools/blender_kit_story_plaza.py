# 스토리 맵 '어린 날 시계탑 광장' 키트 (2026-11-25 사용자: 첫 1장 보스문 앞 꿈 맵, 블렌더로)
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_plaza.py
# 만들어지는 것: Game/Assets/Art/Characters/StoryPlaza/Kit_S_*.fbx (회색 + 역할 재질 → Unity ModelPalette.StoryPlaza)
# 노을 진 옛 서부 광장 — 아직 부서지지 않은 시계탑 · 불 켜진 상점 · 바닥에 상감된 로마 숫자 문자판 · 그림자 아이 · 어른
# 헬퍼(Part · box · lathe · numeral …)는 blender_kit_village_dream.py와 같은 방식 (스크립트 하나로 돌게 복사)

import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryPlaza'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_plaza_preview.png'
TAU = math.tau
ROLES = (
    'wood', 'wood_dark', 'wood_light', 'plank', 'stone', 'stone_dark', 'cobble', 'cobble_dark',
    'brick', 'adobe', 'adobe_light', 'roof_shingle', 'brass', 'gold', 'iron', 'clock_face',
    'window_lit', 'lantern_glow', 'cloth_red', 'cloth_cream', 'paint_teal', 'shadow', 'shadow_rim',
    'hub', 'glass', 'rope', 'rock', 'earth', 'grass_dry',
)
HARD = set(ROLES) - {'earth', 'grass_dry', 'window_lit', 'lantern_glow', 'shadow', 'shadow_rim', 'cloth_red', 'cloth_cream', 'rope', 'glass'}
PALETTE = {   # 미리보기 전용 (게임 색은 Unity ModelPalette.StoryPlaza)
    'wood': (.55, .3, .14), 'wood_dark': (.25, .12, .07), 'wood_light': (.82, .58, .3), 'plank': (.68, .42, .2),
    'stone': (.86, .66, .5), 'stone_dark': (.5, .36, .32), 'cobble': (.8, .55, .4), 'cobble_dark': (.55, .34, .3),
    'brick': (.74, .32, .22), 'adobe': (.95, .68, .48), 'adobe_light': (1, .85, .66), 'roof_shingle': (.45, .2, .26),
    'brass': (.8, .55, .2), 'gold': (1, .75, .25), 'iron': (.2, .16, .2), 'clock_face': (1, .94, .78),
    'window_lit': (1, .78, .35), 'lantern_glow': (1, .7, .3), 'cloth_red': (.82, .25, .22), 'cloth_cream': (1, .92, .76),
    'paint_teal': (.24, .55, .55), 'shadow': (.1, .06, .14), 'shadow_rim': (1, .55, .35), 'hub': (1, .8, .3),
    'glass': (.7, .85, .9), 'rope': (.6, .45, .3),
    'rock': (.5, .36, .38), 'earth': (.66, .42, .32), 'grass_dry': (.8, .64, .38),
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


def gear(loc, radius, depth=.20, role='brass', teeth=16, broken=(), rot=(0,0,0)):
    """Spoked, genuinely hollow gear; separate trapezoidal teeth can break off."""
    start = len(PARTS)
    hoop('Gear annulus',(0,0,0),radius*.85,radius*.20,depth,role,n=teeth*2)
    hoop('Gold chased gear lip',(0,depth*.53,0),radius*.79,.045,.05,'gold',n=teeth*2)
    cylinder('Iron axle hub',(0,0,0),radius*.23,depth*1.65,'iron',n=12,rot=(math.pi/2,0,0))
    for j in range(6):
        a = j*TAU/6
        beam('Gold gear spoke',(radius*.19*math.cos(a),0,radius*.19*math.sin(a)),
             (radius*.70*math.cos(a),0,radius*.70*math.sin(a)),
             max(.07,radius*.11),depth*.72,'gold')
    for j in range(teeth):
        a = j*TAU/teeth
        width = TAU/teeth*.32
        if j in broken:
            # A visible stepped stub, rather than simply omitting every tooth.
            outer = radius*(.89 if j % 2 else .94)
        else:
            outer = radius
        poly = [(r*math.cos(a+da),r*math.sin(a+da)) for r,da in
                ((radius*.82,-width),(outer,-width*.72),
                 (outer,width*.72),(radius*.82,width))]
        extrude('Broken tooth' if j in broken else 'Broad gear tooth',poly,depth,role)
    for j in (1,6,10):
        a = j*TAU/teeth
        sector('Rust scar on gear',radius*.66,radius*.84,a-.13,a+.15,.05,'rust',y=depth*.57,steps=2)
    cylinder('Axle retaining button',(0,depth,0),radius*.12,.07,'brass',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc,rot)


def crystal(label, a, b, radius, core=True):
    """Faceted solid with an exposed inset core, readable with opaque shaders."""
    a,b = Vector(a),Vector(b)
    length = (b-a).length
    verts = [(r*math.cos(j*TAU/6),r*math.sin(j*TAU/6),z)
             for z,r in ((0,radius*.77),(length*.74,radius)) for j in range(6)]
    verts.append((radius*.14,-radius*.10,length))
    faces = [tuple(reversed(range(6)))]
    faces += [(j,(j+1) % 6,(j+1) % 6+6,j+6) for j in range(6)]
    faces += [(j+6,(j+1) % 6+6,12) for j in range(6)]
    # +Y facet is j=1. Inset is a real material region, not a hidden core.
    if core:
        outer = faces[2]
        mid = sum((Vector(verts[i]) for i in outer),Vector())/4
        inner = []
        for i in outer:
            v = mid+(Vector(verts[i])-mid)*.90
            v.y -= radius*.03
            inner.append(len(verts))
            verts.append(tuple(v))
        faces[2] = tuple(inner)
        faces += [(outer[j],outer[(j+1) % 4],inner[(j+1) % 4],inner[j]) for j in range(4)]
    part = Part(label,verts,faces,'crystal_blue',False)
    # A separate front sliver fills the inset, making the role unambiguous.
    if core:
        inset = [Vector(verts[i]) for i in faces[2]]
        core_verts = [tuple(v+Vector((0,dy,0))) for dy in (-.022,.022) for v in inset]
        core_faces = [(3,2,1,0),(4,5,6,7)]
        core_faces += [(j,(j+1) % 4,(j+1) % 4+4,j+4) for j in range(4)]
        Part(label+' exposed core',core_verts,core_faces,'crystal_core',False)
    # Minimal Z alignment keeps the exposed core on +Y, including vertical
    # prisms (track/up quaternions can introduce a 180-degree roll there).
    align = Vector((0,0,1)).rotation_difference((b-a).normalized())
    matrix = Matrix.Translation(a) @ align.to_matrix().to_4x4()
    for p in PARTS[-2 if core else -1:]:
        p.verts = [matrix @ v for v in p.verts]
    return part




# ======================= 스토리 광장 조각 =======================
ROMAN = {1: 'I', 2: 'II', 3: 'III', 4: 'IV', 5: 'V', 6: 'VI', 7: 'VII', 8: 'VIII', 9: 'IX', 10: 'X', 11: 'XI', 12: 'XII'}


def flat_numeral(h, r, height, role='gold', z=.14):
    """바닥에 눕힌 로마 숫자: 위쪽이 +Y(XII 쪽 = 화면 위), h시 자리 = 시계 방향"""
    start = len(PARTS)
    numeral(ROMAN[h], (0, 0, 0), height, role)
    a = h * TAU / 12
    move_group(start, (r * math.sin(a), r * math.cos(a), z), (-math.pi / 2, 0, 0))


def flat_hoop(label, z, radius, band, depth, role, n=48):
    return hoop(label, (0, 0, z), radius, band, depth, role, n, rot=(math.pi / 2, 0, 0))


def tower():
    box('Plinth', (0, 0, .15), (4.8, 4.8, .3), 'stone_dark')
    box('Base', (0, 0, .75), (4.4, 4.4, 1.2), 'stone')
    box('Shaft', (0, 0, 4.7), (3.6, 3.6, 7.0), 'brick')
    for sx in (-1, 1):
        for sy in (-1, 1):
            box('Pilaster', (sx * 1.75, sy * 1.75, 4.7), (.55, .55, 7.0), 'stone')
    box('Door', (0, 1.86, 1.45), (1.3, .2, 2.3), 'wood_dark')
    box('Door frame', (0, 1.83, 1.5), (1.7, .14, 2.6), 'stone_dark')
    for z in (4.0, 6.3):
        box('Slit window', (0, 1.82, z), (.5, .12, 1.0), 'window_lit')
        box('Slit frame', (0, 1.8, z), (.75, .1, 1.25), 'stone_dark')
    box('Cornice low', (0, 0, 8.35), (4.7, 4.7, .3), 'stone_dark')
    box('Clock housing', (0, 0, 9.9), (4.2, 4.2, 2.8), 'stone')
    box('Cornice high', (0, 0, 11.45), (4.7, 4.7, .3), 'stone_dark')
    for k in range(4):
        ang = k * math.pi / 2
        start = len(PARTS)
        clock((0, 0, 0), 1.3, hands=False)
        move_group(start, (-2.13 * math.sin(ang), 2.13 * math.cos(ang), 9.9), (0, 0, ang))
    for sx in (-1, 1):
        for sy in (-1, 1):
            box('Belfry post', (sx * 1.7, sy * 1.7, 12.75), (.45, .45, 2.3), 'wood')
    box('Belfry floor', (0, 0, 11.68), (3.9, 3.9, .16), 'plank')
    lathe('Bell', [(11.95, .8), (12.1, .74), (12.6, .52), (13.05, .38), (13.2, .1)], 'brass', n=16)
    box('Bell yoke', (0, 0, 13.35), (2.6, .3, .25), 'wood_dark')
    box('Roof slab', (0, 0, 14.0), (4.3, 4.3, .3), 'stone_dark')
    lathe('Roof', [(14.15, 3.3), (16.9, .05)], 'roof_shingle', n=4, bevel=False).transform(rot=(0, 0, math.pi / 4))
    rod('Finial', (0, 0, 16.8), (0, 0, 17.8), .06, 'gold')
    beam('Vane arrow', (-.7, 0, 17.5), (.7, 0, 17.5), .08, .06, 'gold')
    box('Vane tip', (.75, 0, 17.5), (.25, .05, .25), 'gold', rot=(0, math.pi / 4, 0))


def hand(length, width, tail):
    beam('Hand shaft', (0, 0, -tail), (0, 0, length * .82), width, .06, 'iron')
    box('Hand tip', (0, 0, length * .86), (width * 2.2, .05, width * 2.2), 'gold', rot=(0, math.pi / 4, 0))
    hoop('Hand eye', (0, 0, length * .45), width * 1.3, width * .45, .06, 'brass', n=12)
    cylinder('Tail weight', (0, 0, -tail), width * 1.2, .07, 'brass', n=10, rot=(math.pi / 2, 0, 0))
    cylinder('Pivot hub', (0, .05, 0), width * .9, .12, 'hub', n=10, rot=(math.pi / 2, 0, 0))


def tower_hand_hour():
    hand(.9, .13, .2)


def tower_hand_min():
    hand(1.2, .09, .25)


def plaza_dial():
    cylinder('Dial disk', (0, 0, .06), 10, .12, 'cobble', n=48)
    for k in range(24):
        a0, a1 = k * TAU / 24, (k + 1) * TAU / 24
        sector('Cobble band', 6.95, 9.5, a0, a1, .02, 'cobble_dark' if k % 2 else 'stone', steps=2).transform((0, 0, .125), (math.pi / 2, 0, 0))
    flat_hoop('Outer brass ring', .13, 9.75, .28, .04, 'brass')
    flat_hoop('Inner brass ring', .13, 6.95, .16, .04, 'brass')
    flat_hoop('Center ring', .13, 1.25, .12, .04, 'gold', n=24)
    cylinder('Center boss', (0, 0, .14), .9, .05, 'stone_dark', n=24)
    for h in range(1, 13):
        a = h * TAU / 12
        box('Major tick', (9.15 * math.sin(a), 9.15 * math.cos(a), .14), (.14, .55, .03), 'gold', rot=(0, 0, -a))
        flat_numeral(h, 8.05, 1.0)
    for k in range(60):
        if k % 5 == 0:
            continue
        a = k * TAU / 60
        box('Minor tick', (9.3 * math.sin(a), 9.3 * math.cos(a), .135), (.06, .26, .02), 'stone_dark', rot=(0, 0, -a), bevel=False)


def dial_hand():
    beam('Floor hand', (0, -1.3, .05), (0, 6.6, .05), .38, .1, 'iron')
    box('Floor hand tip', (0, 6.85, .05), (.75, .75, .1), 'gold', rot=(0, 0, math.pi / 4))
    hoop('Floor hand eye', (0, 3.6, .05), .42, .14, .1, 'brass', n=14, rot=(math.pi / 2, 0, 0))
    cylinder('Floor counterweight', (0, -1.3, .05), .55, .1, 'brass', n=16)
    cylinder('Floor pivot hub', (0, 0, .07), .5, .14, 'hub', n=16)


def windows(xs, z, y, w=1.3, h=1.3):
    for x in xs:
        box('Window frame', (x, y - .02, z), (w + .25, .1, h + .25), 'wood_dark')
        box('Window lit', (x, y, z), (w, .12, h), 'window_lit')
        box('Window bar', (x, y + .05, z), (.07, .06, h), 'wood_dark')


def porch(width, y, posts):
    box('Porch deck', (0, y, .12), (width, 2.0, .24), 'plank')
    for x in posts:
        box('Porch post', (x, y + .85, 1.7), (.2, .2, 3.0), 'wood')


def shop_a():
    box('Store body', (0, 0, 2.6), (8, 3.5, 5.2), 'adobe')
    box('False front', (0, 1.9, 6.0), (8.4, .3, 2.0), 'wood')
    box('False front cap', (0, 1.9, 7.1), (8.8, .45, .25), 'wood_dark')
    box('Sign', (0, 2.1, 6.1), (4.4, .12, .9), 'paint_teal')
    box('Sign border', (0, 2.06, 6.1), (4.7, .1, 1.15), 'gold')
    box('Roof', (0, 0, 5.3), (8.2, 3.7, .25), 'roof_shingle')
    porch(8.6, 2.75, (-4.0, -1.3, 1.3, 4.0))
    for i in range(8):
        box('Awning strip', (-3.5 + i, 2.6, 3.35), (1.0, 1.9, .08), 'cloth_red' if i % 2 else 'cloth_cream', rot=(-.32, 0, 0))
    windows((-2.5, 2.5), 1.9, 1.78)
    windows((-2.5, 2.5), 4.25, 1.78, 1.0, 1.0)
    box('Door', (0, 1.82, 1.2), (1.3, .12, 2.4), 'wood_dark')
    box('Door glass', (0, 1.88, 1.75), (.8, .06, .8), 'window_lit')
    box('Barrel stand', (3.3, 2.4, .7), (.9, .9, 1.2), 'wood_light')


def shop_b():
    box('Saloon body', (0, 0, 3.3), (8, 3.5, 6.6), 'brick')
    box('Parapet', (0, 0, 6.8), (8.3, 3.8, .4), 'stone')
    for x in (-3.0, -1.0, 1.0, 3.0):
        box('Parapet merlon', (x, 1.75, 7.2), (.8, .3, .5), 'stone')
    box('Balcony', (0, 2.45, 3.25), (8.4, 1.4, .2), 'plank')
    for x in [-4.0 + i * .8 for i in range(11)]:
        box('Rail post', (x, 3.1, 3.75), (.1, .1, .8), 'wood')
    box('Rail', (0, 3.1, 4.15), (8.4, .12, .12), 'wood_dark')
    for x in (-4.0, 0, 4.0):
        box('Balcony post', (x, 3.0, 1.6), (.22, .22, 3.2), 'wood')
    windows((-2.6, 2.6), 5.0, 1.78, 1.1, 1.6)
    windows((-2.8, 2.8), 1.8, 1.78, 1.4, 1.2)
    box('Doorway', (0, 1.76, 1.35), (1.8, .08, 2.7), 'wood_dark')
    for s in (-1, 1):
        box('Swing door', (s * .45, 1.85, 1.3), (.85, .08, 1.0), 'wood_light')
    box('Saloon sign', (0, 1.9, 4.55), (3.6, .12, .7), 'cloth_cream')
    box('Sign trim', (0, 1.86, 4.55), (3.9, .1, .95), 'brass')
    cylinder('Wall lantern L', (-1.4, 1.95, 2.9), .18, .4, 'lantern_glow', n=8)
    cylinder('Wall lantern R', (1.4, 1.95, 2.9), .18, .4, 'lantern_glow', n=8)


def shop_c():
    box('House body', (0, 0, 1.9), (7, 3.5, 3.8), 'adobe_light')
    box('Parapet', (0, 0, 4.0), (7.2, 3.7, .45), 'adobe')
    for x in [-3.0 + i * 1.0 for i in range(7)]:
        cylinder('Viga', (x, 1.95, 3.45), .1, .6, 'wood', n=6, rot=(math.pi / 2, 0, 0))
    box('Door', (-1.4, 1.8, 1.15), (1.1, .14, 2.3), 'paint_teal')
    box('Door frame', (-1.4, 1.77, 1.2), (1.4, .1, 2.55), 'wood_dark')
    windows((1.6,), 1.9, 1.78, 1.4, 1.1)
    box('Bench', (1.6, 2.3, .45), (2.2, .5, .12), 'plank')
    for x in (.7, 2.5):
        box('Bench leg', (x, 2.3, .2), (.12, .4, .4), 'wood_dark')
    rod('Lantern arm', (-2.4, 1.75, 2.9), (-2.4, 2.4, 2.9), .04, 'iron')
    lump('Lantern', (-2.4, 2.4, 2.6), (.35, .35, .5), 'lantern_glow')
    for i, x in enumerate((-3.0, 3.0)):
        lathe('Pot', [(0, .25), (.45, .35), (.5, .3)], 'brick', n=10).transform((x, 2.2, 0))
        lump('Pot shrub', (x, 2.2, .75), (.6, .6, .55), 'paint_teal')


def street_clock():
    lathe('Post base', [(0, .45), (.25, .45), (.35, .25), (.6, .18)], 'stone_dark', n=10)
    rod('Post', (0, 0, .5), (0, 0, 3.0), .1, 'iron', n=8)
    box('Clock case', (0, 0, 3.55), (1.25, .5, 1.25), 'iron')
    for s in (1, -1):
        start = len(PARTS)
        clock((0, 0, 0), .52, hands=True)
        move_group(start, (0, .3 * s, 3.55), (0, 0, 0 if s > 0 else math.pi))
    lathe('Crown', [(4.17, .5), (4.4, .3), (4.6, .05)], 'brass', n=8)
    box('Bracket', (0, 0, 3.0), (.9, .14, .14), 'brass')


def watch_pedestal():
    lathe('Pedestal', [(0, .6), (.16, .6), (.22, .44), (1.0, .36), (1.08, .55), (1.22, .55)], 'stone', n=12)
    box('Plate', (0, .37, .65), (.5, .03, .3), 'brass')
    start = len(PARTS)
    cylinder('Watch case', (0, -.05, 0), .72, .18, 'gold', n=24, rot=(math.pi / 2, 0, 0))
    cylinder('Watch dial', (0, .06, 0), .62, .06, 'clock_face', n=24, rot=(math.pi / 2, 0, 0))
    for j in range(12):
        a = j * TAU / 12
        box('Watch index', (.5 * math.sin(a), .1, .5 * math.cos(a)), (.04, .04, .1), 'iron', rot=(0, a, 0))
    beam('Hour hand (3)', (0, .12, 0), (.34, .12, 0), .06, .04, 'iron')
    beam('Minute hand (12)', (0, .13, 0), (0, .13, .5), .04, .04, 'iron')
    cylinder('Watch hub', (0, .15, 0), .05, .04, 'brass', n=8, rot=(math.pi / 2, 0, 0))
    cylinder('Crown stem', (0, -.05, .8), .08, .14, 'brass', n=8)
    hoop('Bow', (0, -.05, .95), .16, .05, .05, 'brass', n=12)
    hoop('Open lid rim', (0, -.3, -.55), .7, .08, .08, 'gold', n=24, rot=(1.0, 0, 0))
    cylinder('Open lid', (0, -.36, -.6), .64, .04, 'brass', n=24, rot=(math.pi / 2 + 1.0, 0, 0))
    move_group(start, (0, 0, 1.85), (.95, 0, 0))   # 문자판이 위 · 앞(카메라)을 보게
    tube('Chain', [(.6, .1, 1.5), (.75, .3, 1.25), (.62, .45, 1.15), (.4, .5, 1.2)], .03, 'brass')


def gate_arch():
    for s in (-1, 1):
        box('Gate footing', (s * 2.7, 0, .3), (.9, .9, .6), 'stone_dark')
        box('Gate post', (s * 2.7, 0, 2.5), (.5, .5, 4.4), 'wood')
        box('Post cap', (s * 2.7, 0, 4.8), (.7, .7, .2), 'wood_dark')
    box('Crossbeam', (0, 0, 4.4), (6.4, .45, .5), 'wood_dark')
    box('Gate sign', (0, .2, 3.75), (3.2, .14, .75), 'paint_teal')
    box('Sign border', (0, .16, 3.75), (3.45, .1, .95), 'gold')
    start = len(PARTS)
    clock((0, 0, 0), .45, hands=False)
    move_group(start, (0, .15, 5.1))
    box('Clock mount', (0, 0, 4.85), (.6, .35, .4), 'stone')


def gate_leaf():
    for i in range(5):
        box('Plank', (-1.0 + i * .5, 0, 1.4), (.48, .12, 2.8), 'plank' if i % 2 else 'wood_light')
    for z in (.5, 2.3):
        box('Brace', (0, .1, z), (2.5, .08, .22), 'wood_dark')
    beam('Diagonal', (-1.1, .1, .6), (1.1, .1, 2.2), .2, .08, 'wood_dark')
    for z in (.5, 2.3):
        box('Hinge', (-1.15, .14, z), (.35, .05, .12), 'iron')
    cylinder('Ring pull', (1.0, .16, 1.4), .12, .04, 'brass', n=10, rot=(math.pi / 2, 0, 0))


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


def figure(h, coat, hat):
    s = h / 1.8
    ghost_tail(.8 * s, (.34 if coat else .22) * s, depth=.9, s=s)   # 다리 대신 연기 꼬리 (2026-11-25 사용자: 떠다니는 유령)
    lathe('Torso', [(.8 * s, (.34 if coat else .22) * s), (1.2 * s, .24 * s), (1.45 * s, .2 * s)], 'shadow', n=10, bevel=False)
    lump('Head', (0, 0, 1.6 * s), (.32 * s, .32 * s, .36 * s), 'shadow')
    if hat:
        cylinder('Hat brim', (0, 0, 1.76 * s), .3 * s, .04 * s, 'shadow', n=12)
        cylinder('Hat crown', (0, 0, 1.86 * s), .16 * s, .2 * s, 'shadow', n=10, top=.13 * s)
    return s


def shadow_child():
    s = figure(1.15, False, False)
    lump('Cap', (0, .03, 1.73 * s), (.36 * s, .36 * s, .14 * s), 'shadow')
    rod('Arm up', (.2 * s, 0, 1.38 * s), (.42 * s, .12 * s, 1.05 * s), .05 * s, 'shadow', bevel=False)
    rod('Arm', (-.2 * s, 0, 1.38 * s), (-.3 * s, .05 * s, .95 * s), .05 * s, 'shadow', bevel=False)
    rod('Lantern bail', (.42 * s, .12 * s, 1.05 * s), (.42 * s, .12 * s, .92 * s), .015, 'iron', bevel=False)
    lump('Lantern', (.42 * s, .12 * s, .8 * s), (.18, .18, .24), 'lantern_glow')
    box('Lantern cap', (.42 * s, .12 * s, .94 * s), (.14, .14, .05), 'brass')


def shadow_adult():
    s = figure(1.85, True, True)
    rod('Arm', (.24 * s, 0, 1.4 * s), (.42 * s, .2 * s, 1.0 * s), .06 * s, 'shadow', bevel=False)
    rod('Arm other', (-.24 * s, 0, 1.4 * s), (-.3 * s, 0, .95 * s), .06 * s, 'shadow', bevel=False)
    beam('Wrench', (.42 * s, .2 * s, 1.0 * s), (.42 * s, .3 * s, .55 * s), .07, .04, 'iron')
    hoop('Wrench jaw', (.42 * s, .3 * s, .5 * s), .07, .03, .04, 'brass', n=8)
    box('Toolbelt', (0, 0, 1.0 * s), (.5 * s, .4 * s, .1 * s), 'wood_dark')


def ladder():
    for x in (-.32, .32):
        box('Rail', (x, 0, 2.6), (.1, .1, 5.2), 'wood')
    for i in range(13):
        box('Rung', (0, 0, .3 + i * .38), (.64, .07, .07), 'wood_light' if i % 2 else 'wood_dark')
    box('Foot', (0, 0, .03), (.9, .2, .06), 'iron')
    box('Top hook', (0, -.1, 5.15), (.75, .2, .08), 'iron')
    box('Bucket', (.45, 0, 2.4), (.2, .2, .25), 'brass')
    rod('Bucket bail', (.35, 0, 2.55), (.55, 0, 2.55), .015, 'iron')
    box('Rag', (-.42, 0, 3.1), (.05, .2, .35), 'cloth_cream', bevel=False)


def bunting():
    pts = []
    for i in range(17):
        t = i / 16
        pts.append((-4 + 8 * t, 0, 4.0 - .7 * (1 - (2 * t - 1) ** 2)))
    tube('Rope', pts, .025, 'rope', n=5)
    roles = ('cloth_red', 'cloth_cream', 'paint_teal', 'gold')
    for i in range(1, 16):
        x, _, z = pts[i]
        extrude('Pennant', [(-.22, 0), (.22, 0), (0, -.55)], .02, roles[i % 4], bevel=False).transform((x, 0, z - .02))
    for x in (-4.0, 4.0):
        box('Hook', (x, 0, 4.0), (.12, .12, .2), 'iron')


# ---- 바닥: 굽은 골목 + 울퉁불퉁한 광장 (Unity StoryPlazaBuilder.Outline과 같은 점, 블렌더 (x, y) = Unity (x, z)) ----
OUTLINE = [
    (-4.5, -37.0), (-6.2, -33.0), (-7.0, -29.0), (-5.6, -25.0), (-6.4, -21.0), (-5.2, -17.0), (-6.1, -14.2),
    (-10.5, -13.2), (-14.8, -10.5), (-16.6, -5.5), (-17.2, 0.5), (-15.4, 6.0), (-13.8, 10.2), (-9.6, 14.2),
    (-4.8, 17.6), (0.5, 18.4), (5.6, 17.2), (10.4, 14.6), (14.2, 10.8), (16.9, 5.2), (16.2, -0.8), (17.0, -6.0),
    (14.0, -10.8), (9.8, -13.8), (6.3, -14.2), (5.0, -16.8), (6.0, -20.5), (4.8, -24.5), (5.5, -28.5),
    (4.2, -32.5), (5.0, -37.0), (0.4, -38.2),
]


def inside(pt, poly=OUTLINE):
    x, y = pt
    c = False
    for i in range(len(poly)):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            c = not c
    return c


def slab(label, poly, z_top, thick, role, side_role=None):
    n = len(poly)
    verts = [(x, y, z_top) for x, y in poly] + [(x, y, z_top - thick) for x, y in poly]
    faces = [tuple(range(n)), tuple(reversed(range(n, 2 * n)))]
    faces += [(i, i + n, (i + 1) % n + n, (i + 1) % n) for i in range(n)]
    Part(label, verts, faces, role, bevel=False)
    if side_role:   # 옆면 띠 (같은 윤곽, 조금 아래)
        Part(label + ' side', [(x, y, z_top - .05) for x, y in poly] + [(x, y, z_top - thick + .05) for x, y in poly], faces, side_role, bevel=False)


def blob(cx, cy, r, k=8, jag=.35):
    pts = []
    a0 = random.uniform(0, TAU)
    for i in range(k):
        a = a0 + i * TAU / k + random.uniform(-.25, .25)
        rr = r * random.uniform(1 - jag, 1 + jag * .6)
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return pts


def grown(poly, lo, hi):
    """윤곽을 바깥으로 lo~hi m 불규칙하게 넓힘 (건물이 선 땅 — 걷는 곳은 OUTLINE 벽 안쪽만)"""
    out = []
    n = len(poly)
    for i in range(n):
        x, y = poly[i]
        px, py = poly[i - 1]
        qx, qy = poly[(i + 1) % n]
        nx, ny = (qy - py), -(qx - px)
        L = math.hypot(nx, ny) or 1
        nx, ny = nx / L, ny / L
        if inside((x + nx * .4, y + ny * .4)):
            nx, ny = -nx, -ny
        d = random.uniform(lo, hi)
        out.append((x + nx * d, y + ny * d))
    return out


def rough_hull(pts):
    """볼록 껍질 + 변마다 바깥으로 불규칙하게 튀어나온 점 (스스로 꼬이지 않는 울퉁불퉁한 외곽)"""
    pts = sorted(set(pts))
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    hull = lower[:-1] + upper[:-1]          # 반시계
    out = []
    for i in range(len(hull)):
        (x1, y1), (x2, y2) = hull[i], hull[(i + 1) % len(hull)]
        out.append((x1, y1))
        L = math.hypot(x2 - x1, y2 - y1)
        nx, ny = (y2 - y1) / L, -(x2 - x1) / L   # 반시계 → 오른쪽이 바깥
        k = max(1, int(L / 4))
        for j in range(1, k + 1):
            t = j / (k + 1)
            d = random.uniform(-.4, 1.8)
            out.append((x1 + (x2 - x1) * t + nx * d, y1 + (y2 - y1) * t + ny * d))
    return out


def ground():
    big = rough_hull(grown(OUTLINE, 6.5, 10.0))
    slab('Ground top', big, -0.02, .9, 'earth', 'rock')
    slab('Walk area', list(reversed(grown(OUTLINE, .25, .9))), 0.0, .05, 'cobble')
    # 판석 · 흙 · 마른 풀 조각 (불규칙 다각형, 윤곽 안만)
    roles = ('cobble_dark', 'stone', 'earth', 'grass_dry', 'stone_dark', 'cobble_dark', 'earth')
    tries = 0; made = 0
    while made < 46 and tries < 900:
        tries += 1
        cx, cy = random.uniform(-17, 17), random.uniform(-38, 18.5)
        r = random.uniform(.9, 3.2)
        b = blob(cx, cy, r, random.randint(6, 9))
        if not all(inside(p) for p in b):
            continue
        role = roles[made % len(roles)]
        slab('Patch', b, .02 + .004 * (made % 3), .03, role)
        made += 1
    # 가장자리: 부서져 떠 있는 조각 · 아래로 늘어진 바위
    for i in range(len(big)):
        x1, y1 = big[i]
        x2, y2 = big[(i + 1) % len(big)]
        for t in (.3, .75):
            x, y = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
            nx, ny = (y2 - y1), -(x2 - x1)
            L = math.hypot(nx, ny) or 1
            nx, ny = nx / L, ny / L
            if inside((x + nx * .5, y + ny * .5), big):
                nx, ny = -nx, -ny
            d = random.uniform(1.0, 3.2)
            if random.random() < .7:
                slab('Crumb', blob(x + nx * d, y + ny * d, random.uniform(.5, 1.3), 6), random.uniform(-1.4, -.2), random.uniform(.25, .5), random.choice(('cobble', 'stone', 'cobble_dark')), 'rock')
            lump('Hang rock', (x - nx * .6, y - ny * .6, random.uniform(-2.2, -1.3)), (random.uniform(1.6, 3.4), random.uniform(1.6, 3.4), random.uniform(1.6, 2.8)), random.choice(('rock', 'stone_dark')))
    for _ in range(14):
        cx, cy = random.uniform(-15, 15), random.uniform(-34, 15)
        if inside((cx, cy)):
            lump('Under rock', (cx, cy, random.uniform(-3.5, -1.8)), (random.uniform(3, 6), random.uniform(3, 6), random.uniform(2.5, 4.5)), random.choice(('rock', 'stone_dark')))


# ======================= 디테일 패스 (2026-11-25 사용자: 블렌더로 디테일 더) =======================
# 판자 결 · 모서리 돌 · 덧창 · 처마 받침 · 화분 · 계단 · 금 글자 · 상감 띠 · 루버 · 마룻대 · 연석 · 자갈 · 풀포기

def planks_v(x0, x1, z0, z1, y, step=.42, roles=('wood_light', 'wood')):
    n = max(1, int((x1 - x0) / step))
    w = (x1 - x0) / n
    for i in range(n):
        box('Plank board', (x0 + w * (i + .5), y, (z0 + z1) / 2), (w * .9, .05, (z1 - z0) * random.uniform(.96, 1.0)), roles[i % len(roles)])


def bands_h(x0, x1, z0, z1, y, step=.55, role='stone_dark', t=.06):
    z = z0
    while z < z1:
        box('Course band', ((x0 + x1) / 2, y, z), (x1 - x0, .05, t), role, bevel=False)
        z += step


def quoins(x, y, z0, z1, size=.42, roles=('stone', 'stone_dark'), out=1):
    z, i = z0, 0
    while z < z1 - .1:
        w = size * (1.25 if i % 2 else .9)
        box('Quoin', (x, y, z + size * .45), (w, w * .6 if out else w, size * .88), roles[i % 2])
        z += size; i += 1


def letters(cx, y, z, width, h=.32, role='gold', n=6):
    for i in range(n):
        x = cx - width / 2 + width * (i + .5) / n
        box('Letter stem', (x - .05, y, z), (.06, .04, h), role, bevel=False)
        box('Letter bar', (x + .02, y, z + random.choice((-1, 0, 1)) * h * .35), (.16, .04, .05), role, bevel=False)


def shutters(x, z, y, w=1.3, h=1.3, role='paint_teal'):
    for s in (-1, 1):
        box('Shutter', (x + s * (w / 2 + .3), y, z), (.52, .07, h + .1), role)
        for k in range(4):
            box('Shutter slat', (x + s * (w / 2 + .3), y + .045, z - h / 2 + .2 + k * (h - .3) / 3), (.44, .02, .05), 'wood_dark', bevel=False)


def flower_box(x, z, y, w=1.3):
    box('Flower box', (x, y + .12, z), (w, .3, .22), 'wood')
    for k in range(5):
        lump('Bloom', (x - w / 2 + .15 + k * (w - .3) / 4, y + .14, z + .2), (.26, .22, .22), 'cloth_red' if k % 2 else 'paint_teal')


def brackets(x0, x1, z, y, n=6, role='wood_dark'):
    for i in range(n):
        x = x0 + (x1 - x0) * i / (n - 1)
        beam('Eave bracket', (x, y, z - .4), (x, y + .35, z), .1, .1, role)


def lantern(x, y, z):
    rod('Lantern hook', (x, y, z + .45), (x, y, z + .2), .02, 'iron')
    box('Lantern cap', (x, y, z + .2), (.22, .22, .06), 'brass')
    box('Lantern glass', (x, y, z), (.18, .18, .32), 'lantern_glow', bevel=False)
    box('Lantern base', (x, y, z - .18), (.2, .2, .05), 'brass')


def steps(x, y, w, n=2, role='plank'):
    for k in range(n):
        h = .18 * (n - k)
        box('Step', (x, y + .32 * k, h / 2), (w, .32, h), role)


def shop_a_detail():
    planks_v(-4.1, 4.1, 5.05, 6.95, 2.07)
    letters(0, 2.19, 6.1, 3.4)
    for s in (-1, 1):
        box('Corner board', (s * 4.0, 1.8, 2.6), (.22, .12, 5.2), 'wood_dark')
        rod('Downpipe', (s * 3.85, 1.9, .2), (s * 3.85, 1.9, 5.1), .06, 'iron')
        shutters(s * 2.5, 1.9, 1.86)
        flower_box(s * 2.5, 3.6, 1.82, 1.1)
    brackets(-4.0, 4.0, 5.0, 1.78, 8)
    box('Plinth band', (0, 0, .2), (8.15, 3.65, .4), 'stone_dark')
    bands_h(-4.0, 4.0, .7, 4.9, 1.77, .6, 'adobe_light', .05)
    steps(0, 3.9, 2.2, 2)
    lantern(-1.3, 3.55, 2.7)
    lantern(1.3, 3.55, 2.7)
    box('Rail', (-2.7, 3.6, .75), (2.6, .08, .08), 'wood_dark')
    box('Rail', (2.7, 3.6, .75), (2.6, .08, .08), 'wood_dark')
    for x in (-3.4, -2.2, 2.2, 3.4):
        box('Baluster', (x, 3.6, .5), (.07, .07, .5), 'wood')
    lathe('Barrel', [(0, .32), (.35, .38), (.7, .32)], 'wood', n=10).transform((-3.3, 2.4, .24))
    for z in (.36, .7):
        hoop('Barrel hoop', (-3.3, 2.4, z), .375, .04, .05, 'iron', n=10, rot=(math.pi / 2, 0, 0))


def shop_b_detail():
    bands_h(-4.0, 4.0, .4, 6.4, 1.77, .32, 'stone_dark', .03)
    for s in (-1, 1):
        quoins(s * 3.85, 1.6, 0, 6.6)
        for x in (s * 2.6,):
            sector('Window arch', .55, .78, 0, math.pi, .08, 'stone', y=1.82).transform((x, 0, 5.8))
            box('Keystone', (x, 1.86, 6.58), (.22, .1, .3), 'gold')
        flower_box(s * 2.8, 2.6, 1.82, 1.4)
        beam('Balcony brace', (s * 4.0, 1.9, 2.3), (s * 4.0, 2.9, 3.15), .14, .14, 'wood_dark')
        beam('Balcony brace', (0, 1.9, 2.3), (0, 2.9, 3.15), .14, .14, 'wood_dark')
        lump('Hanging basket', (s * 2.0, 3.0, 2.75), (.45, .45, .35), 'paint_teal')
        lump('Basket bloom', (s * 2.0, 3.0, 2.95), (.3, .3, .2), 'cloth_red')
    box('Parapet inlay', (0, 1.92, 6.8), (7.8, .06, .12), 'gold', bevel=False)
    letters(0, 1.98, 4.55, 3.0, .34, 'cloth_red', 7)
    for x in (-.45, .45):
        hoop('Door ornament', (x, 1.9, 1.45), .2, .05, .04, 'brass', n=12)
    box('Chimney', (2.6, -.8, 7.4), (.7, .7, 1.2), 'brick')
    box('Chimney cap', (2.6, -.8, 8.05), (.9, .9, .12), 'stone_dark')
    steps(0, 2.0, 2.4, 2, 'stone')
    for s in (-1, 1):
        box('Hitch rail post', (s * 1.6, 3.9, .5), (.14, .14, 1.0), 'wood')
    box('Hitch rail', (0, 3.9, .9), (3.4, .1, .1), 'wood_dark')


def shop_c_detail():
    for s in (-1, 1):
        lump('Buttress', (s * 3.45, 1.4, .9), (.6, .7, 1.8), 'adobe')
    box('Plinth', (0, 0, .15), (7.1, 3.6, .3), 'adobe')
    for k in range(7):
        lump('Ristra pepper', (-2.0, 1.95, 3.0 - k * .17), (.12, .12, .16), 'cloth_red')
    rod('Ristra cord', (-2.0, 1.95, 3.15), (-2.0, 1.95, 1.85), .012, 'rope', bevel=False)
    for x in (.55, 2.65):
        box('Awning pole', (x, 3.0, 1.15), (.08, .08, 2.3), 'wood')
    box('Awning', (1.6, 2.45, 2.35), (2.6, 1.3, .06), 'cloth_cream', rot=(-.28, 0, 0))
    for i in range(4):
        box('Awning stripe', (.75 + i * .58, 2.47, 2.37), (.22, 1.3, .07), 'cloth_red', rot=(-.28, 0, 0), bevel=False)
    for k in range(4):
        box('Window grille', (1.6 - .45 + k * .3, 1.86, 1.9), (.05, .05, 1.1), 'iron', bevel=False)
    lump('Kiva chimney', (-2.4, -.4, 4.5), (.7, .7, .8), 'adobe_light')
    box('Chimney mouth', (-2.4, -.4, 4.95), (.35, .35, .1), 'wood_dark', bevel=False)
    steps(-1.4, 1.95, 1.4, 1, 'stone')
    box('Door knocker', (-1.05, 1.9, 1.3), (.1, .05, .1), 'brass')
    lathe('Water jar', [(0, .18), (.3, .3), (.55, .16), (.62, .18)], 'brick', n=10).transform((-3.0, 2.2, 0))


def tower_detail():
    for sx in (-1, 1):
        for sy in (-1, 1):
            quoins(sx * 1.98, sy * 1.98, 1.2, 8.2, .5)
    for z in (1.35, 8.0):
        for k in range(4):
            ang = k * math.pi / 2
            box('Gold inlay band', (-1.84 * math.sin(ang), 1.84 * math.cos(ang), z), (3.4, .05, .1), 'gold', rot=(0, 0, ang), bevel=False)
    for z in (4.0, 6.3):
        sector('Arch head', .28, .42, 0, math.pi, .1, 'stone', y=1.86).transform((0, 0, z + .5))
    for k in range(4):
        ang = k * math.pi / 2
        start = len(PARTS)
        for s in (-1, 1):
            scroll('Clock scroll', (s * 1.55, 0, -.9), .34, s)
            scroll('Clock scroll top', (s * 1.0, 0, 1.25), .26, -s)
        box('Clock crest', (0, 0, 1.45), (.5, .06, .3), 'gold')
        move_group(start, (-2.16 * math.sin(ang), 2.16 * math.cos(ang), 9.9), (0, 0, ang))
        start = len(PARTS)
        for i in range(5):
            box('Louver', (0, 0, 12.0 + i * .34), (2.8, .06, .12), 'wood_dark', rot=(.5, 0, 0), bevel=False)
        move_group(start, (-1.78 * math.sin(ang), 1.78 * math.cos(ang), 0), (0, 0, ang))
    for sx in (-1, 1):
        for sy in (-1, 1):
            beam('Roof ridge', (sx * 2.25, sy * 2.25, 14.2), (0, 0, 16.9), .12, .12, 'gold')
    start = len(PARTS)
    box('Dormer', (0, 0, 0), (.9, .7, .8), 'stone')
    box('Dormer glass', (0, .36, 0), (.5, .05, .5), 'window_lit', bevel=False)
    lathe('Dormer cap', [(.4, .55), (.9, .05)], 'roof_shingle', n=4, bevel=False).transform(rot=(0, 0, math.pi / 4))
    move_group(start, (0, 1.55, 14.9))
    box('Plaque', (0, 1.95, 3.0), (1.1, .06, .45), 'gold')
    letters(0, 1.99, 3.0, .9, .22, 'iron', 5)
    for s in (-1, 1):
        box('Door hinge', (-.5, 1.97, 1.45 + s * .7), (.5, .04, .08), 'iron', bevel=False)
    steps(0, 2.3, 2.0, 2, 'stone')


def gate_detail():
    for s in (-1, 1):
        for z in (.9, 2.0, 3.1):
            box('Iron strap', (s * 2.7, 0, z), (.56, .56, .08), 'iron', bevel=False)
        lathe('Post finial', [(4.9, .2), (5.1, .24), (5.3, .12), (5.45, .02)], 'brass', n=8)
        move_group(len(PARTS) - 1, (s * 2.7, 0, 0))
        scroll('Bracket scroll', (s * 2.25, .25, 4.0), .3, -s)
        lantern(s * 3.05, .45, 3.7)
    letters(0, .3, 3.75, 2.6, .3, 'gold', 7)


def ground_detail():
    # 걷는 곳 가장자리 연석 · 자갈 · 풀포기 · 금 간 판석
    n = len(OUTLINE)
    for i in range(n):
        (x1, y1), (x2, y2) = OUTLINE[i], OUTLINE[(i + 1) % n]
        L = math.hypot(x2 - x1, y2 - y1)
        k = int(L / .75)
        for j in range(k):
            t = (j + .5) / k
            x, y = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
            a = math.atan2(y2 - y1, x2 - x1)
            box('Curb', (x, y, .06), (.7 * random.uniform(.8, 1.05), .32, .14 * random.uniform(.8, 1.2)), 'stone_dark' if j % 3 else 'stone', rot=(0, 0, a + random.uniform(-.08, .08)), bevel=False)
    made = tries = 0
    while made < 90 and tries < 2000:
        tries += 1
        x, y = random.uniform(-18, 18), random.uniform(-38, 19)
        if not inside((x, y)):
            continue
        if math.hypot(x - .5, y - .5) < 10.3 or math.hypot(x + .4, y + 22.6) < 3.8:
            continue                       # 문자판 위는 비움
        r = random.random()
        if r < .45:
            lump('Pebble', (x, y, .03), (random.uniform(.12, .3), random.uniform(.12, .3), .08), random.choice(('stone', 'stone_dark', 'cobble_dark')))
        elif r < .75:
            for q in range(3):
                lump('Grass tuft', (x + random.uniform(-.2, .2), y + random.uniform(-.2, .2), .12), (.12, .12, random.uniform(.25, .45)), 'grass_dry')
        else:
            a = random.uniform(0, math.pi)
            box('Crack', (x, y, .035), (random.uniform(.6, 1.4), .04, .01), 'rock', rot=(0, 0, a), bevel=False)
            box('Crack fork', (x + .3, y, .035), (.5, .035, .01), 'rock', rot=(0, 0, a + .6), bevel=False)
    # 바깥 땅 (건물 둘레): 마른 풀 · 바위
    for _ in range(40):
        x, y = random.uniform(-26, 26), random.uniform(-44, 24)
        if inside((x, y)):
            continue
        lump('Outer tuft', (x, y, .1), (random.uniform(.3, .7), random.uniform(.3, .7), random.uniform(.3, .6)), random.choice(('grass_dry', 'grass_dry', 'rock')))


def _with(*fns):
    def run():
        for f in fns:
            f()
    return run


# ======================= 시계바늘 수리 퀘스트 — 마을 곳곳 숨은 물건 (현실과 이어진 것, 2026-11-25) =======================
def memento_glasses():
    """시계공의 돋보기 안경: 둥근 렌즈 둘 + 한쪽 확대경이 달린 철테"""
    for s in (-1, 1):
        hoop('Lens rim', (s * .2, 0, .2), .16, .035, .04, 'brass', n=20)
        cylinder('Lens', (s * .2, 0, .2), .13, .015, 'glass', n=20, rot=(math.pi / 2, 0, 0))
        beam('Temple', (s * .36, 0, .22), (s * .4, -.42, .2), .025, .025, 'brass')
        beam('Temple tip', (s * .4, -.42, .2), (s * .38, -.5, .12), .025, .025, 'brass')
    beam('Bridge', (-.06, 0, .25), (.06, 0, .25), .03, .03, 'gold')
    hoop('Loupe rim', (.2, .12, .2), .1, .03, .05, 'gold', n=16)
    cylinder('Loupe barrel', (.2, .17, .2), .085, .1, 'iron', n=16, rot=(math.pi / 2, 0, 0))
    cylinder('Loupe glass', (.2, .23, .2), .075, .01, 'glass', n=16, rot=(math.pi / 2, 0, 0))
    box('Cloth', (0, -.2, .02), (.9, .7, .03), 'cloth_cream', bevel=False)


def memento_band():
    """병원 손목 팔찌: 둥근 띠 + 이름이 지워진 명찰"""
    pts = []
    for i in range(17):
        a = i * TAU / 16
        pts.append((.24 * math.cos(a), .2 * math.sin(a), .06 + .02 * math.sin(a * 2)))
    tube('Band', pts, .035, 'cloth_cream', n=6)
    box('Tag', (.24, 0, .08), (.08, .3, .14), 'clock_face')
    box('Tag line', (.285, 0, .11), (.01, .22, .018), 'stone_dark', bevel=False)
    box('Tag line smudge', (.285, -.03, .07), (.01, .14, .018), 'rock', bevel=False)
    box('Clasp', (-.24, 0, .06), (.08, .1, .1), 'paint_teal')
    box('Barcode', (.285, .0, .045), (.01, .2, .02), 'iron', bevel=False)


def memento_note():
    """아이 손글씨 쪽지: 반쯤 접힌 종이 + 크레용 글씨 · 시계 낙서"""
    extrude('Paper', [(-.3, 0), (.3, 0), (.3, .4), (-.3, .4)], .012, 'clock_face', bevel=False).transform((0, 0, .02), (math.pi / 2, 0, 0))
    extrude('Fold', [(-.3, 0), (.3, 0), (.3, .4), (-.3, .4)], .012, 'cloth_cream', bevel=False).transform((0, -.4, .02), (math.pi / 2 - .5, 0, 0))
    for i in range(4):
        box('Crayon word', (-.18 + i * .12, .2, .035), (.08, .03, .01), 'cloth_red', bevel=False)
    hoop('Crayon clock', (.15, .32, .035), .06, .012, .01, 'paint_teal', n=12, rot=(math.pi / 2, 0, 0))
    box('Crayon hand', (.15, .345, .036), (.01, .05, .01), 'paint_teal', bevel=False)
    box('Crayon', (-.2, -.1, .04), (.24, .05, .05), 'cloth_red', rot=(0, 0, .4))
    lathe('Crayon tip', [(0, .025), (.05, .0)], 'cloth_red', n=6).transform((-.31, -.15, .04), (0, -math.pi / 2, .4))


SPECS = (
    # name, dims(None = 만든 치수), centered, builder, 삼각형 예산
    ('Tower', None, False, _with(tower, tower_detail), 18000),
    ('TowerHand_Hour', None, True, tower_hand_hour, 1500),
    ('TowerHand_Min', None, True, tower_hand_min, 1500),
    ('PlazaDial', None, False, plaza_dial, 14000),
    ('DialHand', None, True, dial_hand, 2000),
    ('ShopA', None, False, _with(shop_a, shop_a_detail), 14000),
    ('ShopB', None, False, _with(shop_b, shop_b_detail), 14000),
    ('ShopC', None, False, _with(shop_c, shop_c_detail), 12000),
    ('StreetClock', None, False, street_clock, 5000),
    ('WatchPedestal', None, False, watch_pedestal, 5000),
    ('GateArch', None, False, _with(gate_arch, gate_detail), 9000),
    ('GateLeaf', None, True, gate_leaf, 3000),
    ('ShadowChild', None, False, shadow_child, 2500),
    ('ShadowAdult', None, False, shadow_adult, 2500),
    ('Ladder', None, False, ladder, 3000),
    ('Bunting', None, True, bunting, 3000),
    ('Ground', None, False, _with(ground, ground_detail), 40000),
    ('Memento_Glasses', None, False, memento_glasses, 4000),
    ('Memento_Band', None, False, memento_band, 3000),
    ('Memento_Note', None, False, memento_note, 3000),
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
    lo, hi = bounds(obj)
    mid = (hi + lo) / 2
    # 손 · 바늘은 회전축(hub) 자리가 원점이 되게 그대로 둠 — Unity가 hub 재질 정점으로 축을 찾음
    keep_origin = name in ('TowerHand_Hour', 'TowerHand_Min', 'DialHand', 'Ground')
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
    destination = OUTPUT / ('Kit_S_' + name + '.fbx')
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
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    shading = scene.display.shading
    shading.light, shading.color_type = 'STUDIO', 'MATERIAL'
    shading.show_cavity, shading.show_object_outline = True, True
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.35, .18, .2)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    # 두 줄 배치: 큰 것 위 · 작은 것 아래
    objects = [o for o in objects if o.name != 'Ground']
    row1 = [o for o in objects if o.name in ('PlazaDial', 'Tower', 'ShopA', 'ShopB', 'ShopC')]
    row2 = [o for o in objects if o not in row1]
    def lay(row, y, gap):
        total = sum(o.dimensions.x for o in row) + gap * (len(row) - 1)
        x = -total / 2
        for o in row:
            o.location = (x + o.dimensions.x / 2, y, 0)
            x += o.dimensions.x + gap
        return total
    w1 = lay(row1, -8, 1.5)
    lay(row2, 10, .8)
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type, cam.data.ortho_scale = 'ORTHO', w1 + 4
    a = math.radians(40)
    target = Vector((0, 0, 3))
    cam.location = target + Vector((0, 60 * math.cos(a), 60 * math.sin(a)))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.render.render(write_still=True)
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
