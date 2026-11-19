# 시계탑 전투방 벽 모듈 7종과 검수용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_walls.py
"""Deterministic Blender 5.x wall kit. Metres, Z up, room/front = +Y.

Only the last, review-only pass changes viewport colours or adds a camera.
All exports are one grey, role-material mesh, with their origin at (0, 0, 0).
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_walls_preview.png'
ROLES = (
    'steel', 'steel_dark', 'iron', 'iron_dark', 'silver', 'brass',
    'brass_dark', 'gold', 'copper', 'navy_paint', 'navy_dark',
    'red_lacquer', 'red_dark', 'gem_red', 'wood', 'wood_dark',
    'stone', 'stone_dark', 'dial_face', 'dial_mark', 'glass',
    'starlight', 'night_sky', 'porcelain', 'ribbon_red', 'velvet', 'lamp_warm',
)
PALETTE = {
    'steel': (.46, .61, .69), 'steel_dark': (.19, .29, .36),
    'iron': (.25, .32, .39), 'iron_dark': (.075, .12, .19),
    'silver': (.75, .85, .89), 'brass': (.74, .47, .16),
    'brass_dark': (.34, .22, .10), 'gold': (1.0, .73, .27),
    'copper': (.73, .32, .19), 'navy_paint': (.10, .20, .34),
    'navy_dark': (.055, .10, .19), 'red_lacquer': (.58, .065, .09),
    'red_dark': (.25, .045, .06), 'wood': (.44, .27, .15),
    'wood_dark': (.21, .12, .085), 'stone': (.47, .52, .60),
    'stone_dark': (.23, .29, .38), 'dial_face': (.86, .85, .69),
    'dial_mark': (.06, .085, .14), 'glass': (.37, .66, .73),
    'starlight': (.86, .93, 1.0), 'night_sky': (.018, .032, .10),
    'lamp_warm': (1.0, .78, .34),
}
MATS = {}
PARTS = []
FACE = (math.pi / 2, 0, 0)


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .76
        if role == 'glass':
            shader.inputs['Transmission Weight'].default_value = .72
            shader.inputs['Roughness'].default_value = .18
            shader.inputs['Alpha'].default_value = .23
            mat.surface_render_method = 'DITHERED'
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Hand softened hard edges', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(70)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, bevel=False, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
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


def cylinder(label, loc, radius, depth, role, n=16, rot=FACE, bevel=False):
    verts = [(radius * math.cos(j * math.tau / n),
              radius * math.sin(j * math.tau / n), z)
             for z in (-depth / 2, depth / 2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def ring(label, loc, outer, inner, depth, role, n=32, bevel=False):
    verts = [(r * math.cos(j * math.tau / n), y,
              r * math.sin(j * math.tau / n))
             for y, r in ((-depth / 2, outer), (depth / 2, outer),
                          (depth / 2, inner), (-depth / 2, inner))
             for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc)


def beam(label, a, b, width, depth, role, bevel=False):
    vec = Vector(b) - Vector(a)
    obj = box(label, (0, 0, 0), (width, depth, vec.length), role, bevel)
    obj.data.transform(Matrix.Translation((Vector(a) + Vector(b)) / 2) @
                       vec.to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def tube(label, points, radius, role, n=6):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(i - 1, 0)])).normalized()
        reference = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        side = reference.cross(tangent).normalized()
        other = tangent.cross(side)
        for j in range(n):
            a = j * math.tau / n
            verts.append(tuple(Vector(point) + radius *
                               (side * math.cos(a) + other * math.sin(a))))
    faces = [tuple(reversed(range(n)))]
    faces += [(k * n + j, k * n + (j + 1) % n,
               (k + 1) * n + (j + 1) % n, (k + 1) * n + j)
              for k in range(len(points) - 1) for j in range(n)]
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, verts, faces, role)


def rivet(x, y, z, role='brass_dark', radius=.055):
    cylinder('Broad forged rivet', (x, y, z), radius, .05, role, n=8)


def star(x, y, z, radius=.09, role='starlight'):
    profile = []
    for j in range(8):
        a = j * math.tau / 8
        r = radius if j % 2 == 0 else radius * .30
        profile.append((x + math.sin(a) * r, z + math.cos(a) * r))
    return extrude('Four pointed tower star', profile, y - .025, y + .025, role)


def gear(x, y, z, radius, role, teeth_role, teeth=24, phase=0):
    """True open spokes, a continuous toothed crown, and a stepped axle boss."""
    n = teeth * 4
    verts = []
    for yy, inner in ((y - .14, False), (y + .14, False),
                      (y + .14, True), (y - .14, True)):
        for j in range(n):
            a = math.tau * j / n + phase
            r = radius * (.79 if inner else (1 if j % 4 in (1, 2) else .90))
            verts.append((x + r * math.cos(a), yy, z + r * math.sin(a)))
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    mesh('Cut gear teeth', verts, faces, teeth_role)
    ring('Machined gear rim', (x, y + .025, z), radius * .84,
         radius * .71, .32, role, n=teeth * 2)
    for j in range(5):
        a = math.tau * j / 5 + phase
        beam('Broad gear spoke',
             (x + math.cos(a) * radius * .18, y + .02, z + math.sin(a) * radius * .18),
             (x + math.cos(a) * radius * .75, y + .02, z + math.sin(a) * radius * .75),
             radius * .16, .23, role)
    cylinder('Stepped gear boss', (x, y + .12, z), radius * .24, .38,
             role, n=16, bevel=True)
    cylinder('Axle end', (x, y + .34, z), radius * .11, .09, 'iron', n=10)
    box('Axle key slot', (x, y + .393, z), (radius * .13, .025, .055), 'dial_mark')


def crate(x, y, bottom, w=.65, h=.55, boxed_gear=False):
    box('Crate carcass', (x, y, bottom + h / 2), (w, .47, h), 'wood', True)
    for dx in (-w * .39, w * .39):
        box('Crate iron hoop', (x + dx, y + .25, bottom + h / 2),
            (.065, .06, h), 'iron')
    for zz in (bottom + .10, bottom + h - .10):
        box('Crate cross batten', (x, y + .29, zz), (w, .07, .075), 'wood_dark')
    if boxed_gear:
        # Open presentation box with a visible miniature spare gear.
        gear(x, y + .33, bottom + h * .56, min(w, h) * .35,
             'brass', 'brass_dark', teeth=10)
    else:
        beam('Crate diagonal', (x - w * .32, y + .30, bottom + .14),
             (x + w * .32, y + .30, bottom + h - .14), .065, .065, 'wood_dark')


def masonry(width, height, front=.10, back=-.55, rows=5):
    box('Continuous dark mortar backing', (0, (front + back) / 2, height / 2),
        (width, front - back, height), 'stone_dark')
    step = height / rows
    for row in range(rows):
        left = -width / 2
        cuts = [left] + [x for x in
                        [left + .7 + (row % 2) * .52 + j * 1.14 for j in range(7)]
                        if left + .12 < x < width / 2 - .12] + [width / 2]
        for j, (a, b) in enumerate(zip(cuts, cuts[1:])):
            box('Offset ashlar', ((a + b) / 2, front + .035, step * (row + .5)),
                (b - a - .035, .15, step - .045),
                'stone_dark' if (row + j) % 5 == 0 else 'stone',
                bevel=(row == rows - 1 and j % 2 == 0))


def storehouse():
    masonry(6, 3.77)
    box('Dressed foundation', (0, -.17, .15), (6, 1.0, .30), 'stone_dark', True)
    box('Dark cornice', (0, -.08, 3.78), (6, .96, .18), 'stone_dark', True)
    for x, width, top in ((-2.18, 1.64, 4.0), (-.30, 2.1, 3.91), (2.17, 1.66, 3.97)):
        box('Uneven cornice coping', (x, -.08, (top + 3.86) / 2),
            (width, 1.03, top - 3.86), 'stone', True)
    # Two tall racks with visibly different shelf rhythms and stock.
    for x, width, top, shelves in ((-1.62, 2.12, 3.47, (.42, 1.46, 2.52, 3.40)),
                                      (1.10, 2.12, 3.22, (.42, 1.70, 3.15))):
        for dx in (-width / 2, width / 2):
            box('Rack upright', (x + dx, .59, top / 2), (.14, .92, top), 'wood_dark', True)
            box('Brass rack foot', (x + dx, .64, .16), (.22, .98, .22), 'brass_dark')
        for zz in shelves:
            box('Thick shelf', (x, .63, zz), (width, 1.00, .13), 'wood_dark', True)
            box('Shelf worn lip', (x, 1.145, zz), (width - .15, .055, .065), 'wood')
        beam('Rack back diagonal', (x - width / 2, .17, .47),
             (x + width / 2, .17, top - .10), .09, .09, 'iron')
    for x, z, r in ((-2.14, 1.98, .35), (-1.25, 2.04, .42), (.65, 2.35, .43), (1.60, 2.27, .34)):
        pts = []
        for j in range(60):
            a = j * math.tau * 2.65 / 59
            rr = .09 + (r - .09) * j / 59
            pts.append((x + rr * math.cos(a), .93, z + rr * math.sin(a)))
        tube('Stored coiled mainspring', pts, .045, 'steel')
        box('Spring cradle', (x, .84, z - r - .07), (.52, .50, .10), 'iron')
    crate(-2.08, .68, .49, .73, .69, True)
    crate(-1.15, .68, .49, .72, .53)
    crate(-1.77, .66, 2.59, .90, .55)
    crate(.62, .65, .49, .78, .65, True)
    crate(1.57, .66, .49, .73, .88)
    # Ladder leans toward the rack at the top, all feet remain inside the seam.
    for x in (2.38, 2.86):
        beam('Leaning ladder stile', (x, 1.40, .04), (x, .65, 3.38), .085, .085, 'iron', True)
    for j in range(9):
        t = .10 + j * .095
        beam('Ladder rung', (2.36, 1.40 - .75 * t, .04 + 3.34 * t),
             (2.88, 1.40 - .75 * t, .04 + 3.34 * t), .065, .065, 'steel')
    box('Lamp bracket mounting plate', (-.15, .24, 3.48), (.30, .11, .46), 'brass', True)
    beam('Lamp bracket arm', (-.15, .28, 3.63), (-.15, 1.02, 3.63), .10, .10, 'brass', True)
    beam('Lamp bracket brace', (-.15, .31, 3.32), (-.15, .91, 3.63), .065, .065, 'brass_dark')
    cylinder('Lamp shade', (-.15, 1.02, 3.54), .24, .10, 'brass', n=12, rot=(0, 0, 0), bevel=True)
    cylinder('Warm lamp bulb', (-.15, 1.02, 3.38), .12, .24, 'lamp_warm', n=12, rot=(0, 0, 0), bevel=True)


def plate_wall(width, height):
    box('Flat back iron wall', (0, -.10, height / 2), (width, .90, height), 'iron_dark')
    columns = 3 if width == 6 else 2
    panel_w = width / columns
    for j in range(columns):
        x = -width / 2 + panel_w * (j + .5)
        box('Recessed enamel wall panel', (x, .39, height / 2),
            (panel_w - .20, .16, height - .44), 'navy_paint', True)
        box('Raised panel lower inset', (x, .49, .48), (panel_w - .36, .07, .42), 'navy_dark')
        for xx in (x - panel_w / 2 + .10, x + panel_w / 2 - .10):
            box('Iron panel frame', (xx, .52, height / 2), (.13, .18, height - .16), 'iron')
            for zz in (.37, 1.15, 2.00, height - .37):
                rivet(xx, .64, zz)
    for zz in (.13, height - .14):
        box('Continuous wall rail', (0, .47, zz), (width, .35, .20), 'iron', True)


def gears():
    plate_wall(6, 4.35)
    for x, w, top in ((-2.2, 1.6, 4.50), (-.2, 2.15, 4.41), (2.23, 1.54, 4.46)):
        box('Stepped engine coping', (x, -.03, (top + 4.25) / 2),
            (w, 1.14, top - 4.25), 'steel', True)
    gear(-1.0, .68, 2.40, 1.60, 'brass', 'brass_dark', 28, .025)
    gear(1.48, .68, 3.00, 1.00, 'copper', 'red_dark', 18, .12)
    gear(2.08, .68, 1.46, .70, 'steel', 'steel_dark', 13, .11)
    cylinder('Horizontal engine drive shaft', (0, 1.03, .51), .10, 5.65, 'steel',
             n=12, rot=(0, math.pi / 2, 0), bevel=True)
    for x in (-2.35, -.18, 2.48):
        box('Bearing pedestal', (x, .79, .27), (.44, .60, .40), 'iron', True)
        cylinder('Brass shaft bearing', (x, 1.03, .51), .23, .26, 'brass',
                 n=12, rot=(0, math.pi / 2, 0), bevel=True)
    # A readable pale gauge isolated in the upper-left corner.
    x, z = -2.38, 3.63
    cylinder('Red pressure gauge housing', (x, .80, z), .40, .36, 'red_lacquer', n=24, bevel=True)
    cylinder('Pressure dial ivory face', (x, 1.0, z), .32, .055, 'dial_face', n=24)
    ring('Gauge brass bezel', (x, 1.035, z), .37, .31, .06, 'brass', n=24)
    for j in range(8):
        a = -.72 * math.pi + j * 1.44 * math.pi / 7
        beam('Pressure gauge index', (x + .23 * math.sin(a), 1.075, z + .23 * math.cos(a)),
             (x + .285 * math.sin(a), 1.075, z + .285 * math.cos(a)), .04, .025, 'dial_mark')
    beam('Gauge needle', (x, 1.10, z), (x + .17, 1.10, z + .16), .045, .035, 'dial_mark')
    tube('Pressure pipe', [(-2.38, .75, 3.25), (-2.76, .75, 3.10),
                           (-2.76, .75, .52)], .06, 'copper')


def bell_arch():
    # The central 2.8 x 3.4 opening contains no stone, including its back.
    for sign in (-1, 1):
        for row in range(5):
            z = (row + .5) * .40
            box('Arch pier ashlar', (sign * 2.20, .15, z), (1.60, 1.0, .385),
                'stone' if row % 3 else 'stone_dark', bevel=(row % 2 == 0))
        box('Pier base molding', (sign * 2.20, .23, .13), (1.60, 1.15, .26), 'stone_dark', True)
        box('Arch spring impost', (sign * 2.21, .29, 2.00), (1.58, 1.28, .18), 'brass_dark', True)
    profile = [(-3, 4.16), (3, 4.16), (3, 2), (1.4, 2)]
    profile += [(1.4 * math.cos(j * math.pi / 24),
                 2 + 1.4 * math.sin(j * math.pi / 24)) for j in range(1, 25)]
    profile += [(-3, 2)]
    extrude('Open arch spandrel', profile, -.35, .65, 'stone', True)
    for j in range(13):
        a, b = j * math.pi / 13 + .008, (j + 1) * math.pi / 13 - .008
        wedge = [(r * math.cos(t), 2 + r * math.sin(t))
                 for r, t in ((1.4, a), (1.69, a), (1.69, b), (1.4, b))]
        extrude('Alternating arch voussoir', wedge, .64, .98,
                'brass' if j == 6 else ('stone_dark' if j % 2 == 0 else 'stone'), True)
    for sign in (-1, 1):
        for j in range(3):
            box('Spandrel inset joint', (sign * (1.93 + .32 * j), .66, 3.63 - j * .16),
                (.05, .025, .57), 'stone_dark')
    box('Starry night beyond the open arch', (0, -.99, 1.78), (2.8, .08, 3.56), 'night_sky')
    # A shallow return at sill level keeps the opening sky-dark when viewed
    # downwards at 55 degrees. It is backdrop, not an opaque wall infill.
    box('Night backdrop sill return', (0, -.13, .025), (2.8, 1.72, .05), 'night_sky')
    for x, z, r in ((-.95, .12, .08), (.95, .14, .075), (-.45, .35, .075),
                    (.45, .40, .07), (.03, .62, .08)):
        star(x, -.93, z, r)
    for x, y in ((-.80, .30), (.64, .08)):
        obj = star(0, 0, 0, .085)
        place(obj, (x, y, .08), (math.pi / 2, 0, 0))
    box('Frieze deep enamel', (0, .29, 4.07), (6, 1.30, .30), 'navy_paint', True)
    box('Frieze lower gold line', (0, .96, 3.94), (6, .07, .07), 'brass')
    for j in range(11):
        star(-2.65 + j * .53, .975, 4.075, .09, 'gold')
    box('Arch coping lower molding', (0, .20, 4.27), (6, 1.30, .13), 'stone_dark', True)
    for x, w, top in ((-2.3, 1.4, 4.46), (0, 2.9, 4.50), (2.32, 1.36, 4.41)):
        box('Arch raised coping', (x, .20, (top + 4.31) / 2),
            (w, 1.36, top - 4.31), 'stone', True)
    # Hollow flared bell, a separate lip and visible clapper.
    box('Bell hanger bracket', (0, .92, 3.16), (.72, .17, .12), 'iron', True)
    beam('Bell bracket anchor left', (-.32, .47, 3.16), (-.32, .92, 3.16), .09, .09, 'iron')
    beam('Bell bracket anchor right', (.32, .47, 3.16), (.32, .92, 3.16), .09, .09, 'iron')
    beam('Bell suspension', (0, .92, 3.16), (0, .92, 2.95), .09, .09, 'iron')
    bell_profile = [(.10, 2.98), (.21, 2.91), (.25, 2.69), (.37, 2.48),
                    (.45, 2.43), (.45, 2.35), (.35, 2.35), (.29, 2.48),
                    (.17, 2.68), (.14, 2.88), (.065, 2.91)]
    n = 24
    verts = [(r * math.cos(j * math.tau / n), .92 + .55 * r * math.sin(j * math.tau / n), z)
             for r, z in bell_profile for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % len(bell_profile)) * n + (j + 1) % n,
              ((k + 1) % len(bell_profile)) * n + j)
             for k in range(len(bell_profile)) for j in range(n)]
    mesh('Hollow bronze bell', verts, faces, 'copper')
    cylinder('Bell crown', (0, .92, 2.94), .12, .12, 'brass', n=12, rot=(0, 0, 0), bevel=True)
    beam('Bell clapper stem', (0, .92, 2.72), (0, .92, 2.27), .07, .07, 'iron')
    cylinder('Bell clapper', (0, .92, 2.27), .095, .14, 'brass', n=12, rot=(0, 0, 0))


def dial_back():
    # Lower disc sector: exact radius 6.0 about (0, .50, 6.15).
    # Its lower edge rises towards the sides; its center lies ABOVE the module.
    center_z = 6.15
    lower = [(x, center_z - math.sqrt(36 - x * x))
             for x in [-4 + j * 8 / 64 for j in range(65)]]
    profile = lower + [(4, 4.76), (-4, 4.76)]
    glass = extrude('Giant translucent clock face sector R6', profile, .49, .56, 'glass')
    # Slight backward lean makes the unseen circle center also behind the face.
    for v in glass.data.vertices:
        v.co.y += (center_z - v.co.z) * .035
    box('Starry sky behind dial', (0, -.78, 2.36), (8, .10, 4.72), 'night_sky')
    for j in range(31):
        x, z = random.uniform(-3.84, 3.84), random.uniform(.20, 4.58)
        star(x, -.71, z, random.uniform(.055, .105))
    # A structural chord and flush end posts keep neighbours tileable.
    for x in (-3.88, 3.88):
        box('End stanchion web', (x, .19, 2.32), (.18, 1.14, 4.64), 'iron_dark')
        for yy in (-.39, .78):
            box('End stanchion flange', (x, yy, 2.32), (.24, .10, 4.64), 'steel', True)
        for z in (.28, 1.60, 3.0, 4.38):
            rivet(x, .85, z, 'brass', .065)
    box('Bottom structural sill', (0, -.01, .12), (8, 1.50, .24), 'iron_dark', True)
    box('Upper truss chord', (0, .30, 4.66), (8, .45, .20), 'iron_dark', True)
    for j in range(6):
        a = (-3.82 + j * 7.64 / 6, .28, 4.47)
        b = (-3.82 + (j + 1) * 7.64 / 6, .28, 3.96)
        beam('Upper zigzag truss', a, b, .12, .18, 'iron_dark')
        beam('Upper zigzag truss return', b, (b[0], .28, 4.47), .10, .18, 'steel')
    # Two concentric bands trace the actual curved outer circumference.
    for radius, role, thickness in ((5.94, 'steel', .13), (5.67, 'brass', .065)):
        xmax = 3.80
        pts = [(x, .81, center_z - math.sqrt(radius * radius - x * x))
               for x in [-xmax + j * 2 * xmax / 48 for j in range(49)]]
        tube('Circular dial retaining band', pts, thickness, role, n=6)
    for a in (-.62, -.31, 0, .31, .62):
        p = (5.81 * math.sin(a), .84, center_z - 5.81 * math.cos(a))
        q = (2.03 * math.sin(a), .84, center_z - 2.03 * math.cos(a))
        beam('Radial steel girder', p, q, .12, .20, 'steel', True)
        for t in (.12, .55, .88):
            v = Vector(p).lerp(Vector(q), t)
            rivet(v.x, .97, v.z, 'brass', .06)
    # Mirrored Roman VI, VII, VIII, V, IV bars. No font/text in the asset.
    # Local tangential X is flipped because we are looking at the BACK.
    glyphs = ('VIII', 'VII', 'VI', 'V', 'IV')
    for a, numeral in zip((-.59, -.30, 0, .30, .59), glyphs):
        def tick(u, v):
            return (5.13 * math.sin(a) - u * math.cos(a) - v * math.sin(a),
                    1.025, center_z - 5.13 * math.cos(a) - u * math.sin(a) + v * math.cos(a))
        cursor = -.13 * (len(numeral) - 1)
        for letter in numeral:
            if letter == 'I':
                beam('Mirrored Roman I', tick(cursor, -.24), tick(cursor, .24), .065, .07, 'dial_mark')
            else:
                beam('Mirrored Roman V left', tick(cursor - .11, .24), tick(cursor, -.24), .065, .07, 'dial_mark')
                beam('Mirrored Roman V right', tick(cursor + .11, .24), tick(cursor, -.24), .065, .07, 'dial_mark')
            cursor += .26
    # Back of the clock hands; the pivot and counterweights enter the top sector.
    pivot = (.58, 1.14, 4.01)
    for tip, width in (((-2.68, 1.14, 1.44), .23), ((2.58, 1.14, 2.13), .30)):
        beam('Giant clock hand back', pivot, tip, width, .10, 'dial_mark', True)
        vec = (Vector(pivot) - Vector(tip)).normalized()
        end = Vector(pivot) + vec * .61
        beam('Hand gold counterweight', Vector(pivot) + vec * .15, end,
             width * 1.7, .14, 'gold', True)
    ring('Clock axle rear ring', pivot, .31, .17, .19, 'brass', n=24)
    cylinder('Clock axle rear cap', (.58, 1.27, 4.01), .18, .14, 'iron', n=16, bevel=True)


def pendulum():
    plate_wall(6, 4.75)
    box('Full width lower stone plinth', (0, -.03, .17), (6, 1.30, .34), 'stone_dark', True)
    for x in (-2.57, 2.57):
        box('Side wall relief panel', (x, .52, 2.64), (.52, .10, 3.36), 'steel_dark', True)
        for zz in (1.25, 2.13, 3.01, 3.89):
            box('Side wall brass hash', (x, .59, zz), (.24, .055, .07), 'brass')
    box('Case red recessed back', (0, .59, 2.41), (3.53, .16, 4.24), 'red_lacquer', True)
    for x in (-1.86, 1.86):
        box('Carved dark case pillar', (x, .86, 2.45), (.23, .76, 4.44), 'wood_dark', True)
        box('Pillar inset', (x, 1.25, 2.45), (.075, .04, 3.80), 'wood')
        for z in (.39, 4.35):
            box('Case pillar ferrule', (x, .88, z), (.33, .83, .16), 'brass', True)
    for zz, w, h in ((.30, 4.15, .20), (.51, 3.94, .15), (4.58, 4.17, .20), (4.76, 3.83, .16)):
        box('Stacked case molding', (0, .83, zz), (w, .83, h), 'wood_dark', True)
    crown = [(-1.45, 4.81), (-.66, 4.89), (-.32, 5), (.32, 5), (.66, 4.89), (1.45, 4.81)]
    extrude('Pediment with irregular crown', crown, .50, 1.15, 'brass', True)
    ring('Pendulum pivot collar', (0, .94, 4.09), .30, .18, .15, 'brass_dark', n=24)
    cylinder('Pivot pin', (0, 1.04, 4.09), .13, .18, 'steel', n=12, bevel=True)
    beam('Long pendulum steel rod', (0, 1.03, 4.09), (.29, 1.03, 1.39), .12, .12, 'steel', True)
    cylinder('Heavy pendulum bob disc', (.29, 1.02, 1.39), .77, .24, 'brass', n=32, bevel=True)
    ring('Gold bob annulus', (.29, 1.17, 1.39), .74, .59, .065, 'gold', n=32)
    cylinder('Bob central medallion', (.29, 1.19, 1.39), .28, .08, 'brass_dark', n=16, bevel=True)
    star(.29, 1.25, 1.39, .16, 'gold')
    box('Tall glass case front', (0, 1.31, 2.55), (3.43, .055, 3.90), 'glass')
    # Fine brass glazing bars and corner plates frame the transparent sheet.
    for x in (-1.69, 1.69):
        box('Glass edge trim', (x, 1.36, 2.55), (.055, .045, 3.92), 'brass')
    for z in (.61, 4.48):
        box('Glass horizontal trim', (0, 1.36, z), (3.43, .045, .055), 'brass')
    for x in (-1.48, 1.48):
        for z in (.82, 4.24):
            box('Glass corner escutcheon', (x, 1.36, z), (.18, .05, .18), 'brass_dark')
    box('Clock case door keyhole', (1.57, 1.40, 2.39), (.10, .06, .18), 'gold', True)


def filler():
    masonry(4, 1.86, front=.58, back=-.55, rows=3)
    box('Filler foundation molding', (0, .08, .12), (4, 1.26, .24), 'stone_dark', True)
    box('Filler band backing', (0, .10, 2.26), (4, 1.30, .86), 'iron_dark')
    for j in range(3):
        x = -4 / 3 + j * 4 / 3
        box('Filler navy iron panel', (x, .79, 2.26), (1.27, .12, .70), 'navy_paint', True)
        for xx in (x - .52, x + .52):
            for z in (2.03, 2.49):
                rivet(xx, .89, z, 'brass', .05)
    for x in (-.67, .67):
        box('Dark panel seam', (x, .87, 2.26), (.045, .025, .71), 'dial_mark')
    box('Band lower rolled edge', (0, .22, 1.87), (4, 1.45, .13), 'steel', True)
    box('Continuous brass cap rail', (0, .18, 2.76), (4, 1.44, .18), 'brass', True)
    for j in range(9):
        x = -1.78 + j * .445
        top = 3.0 if j % 3 != 1 else 2.95
        tooth = [(x - .15, 2.83), (x + .15, 2.83),
                 (x + .105, top), (x - .105, top)]
        extrude('Chamfered gear tooth crenellation', tooth, .15, .78,
                'brass_dark' if j % 3 == 0 else 'brass', True)
    # An inset gear relief breaks the otherwise horizontal filler silhouette.
    gear(.12, .94, 2.26, .30, 'steel', 'steel_dark', teeth=10)


def girder():
    # No solid wall/sky card: this piece is genuinely open to the void.
    for z in (.08, .98):
        box('Railing I beam web', (0, .36, z), (6, .16, .16), 'steel')
        for zz in (z - .07, z + .07):
            box('Railing I beam flange', (0, .34, zz), (6, .52, .08), 'steel', True)
    for x in (-2.88, -1.0, 1.0, 2.88):
        box('Railing upright', (x, .33, .55), (.16, .44, 1.10), 'steel', True)
        box('Upright foot gusset', (x, .60, .17), (.24, .06, .31), 'iron')
        for z in (.17, .91):
            rivet(x, .65, z, 'brass', .06)
    for left, right in ((-2.83, -1.06), (-.94, .94), (1.06, 2.83)):
        for lo, hi in ((.20, .87), (.87, .20)):
            beam('Railing X brace', (left, .37, lo), (right, .37, hi), .09, .11, 'iron')
        rivet((left + right) / 2, .46, .535, 'brass', .065)
    # The load-bearing platform edge is behind the rail and exactly at Z=0.
    box('Platform lip top flange', (0, -.28, -.055), (6, 1.12, .11), 'steel', True)
    box('Platform lip bottom flange', (0, -.28, -.46), (6, 1.12, .10), 'iron', True)
    box('Deep edge girder web', (0, -.28, -.25), (6, .17, .43), 'iron_dark')
    for x in (-2.84, -.95, .95, 2.84):
        box('Hanging truss vertical', (x, -.41, -2.02), (.19, .42, 3.96), 'iron_dark', True)
        for zz in (-.60, -2.03, -3.82):
            box('Truss gusset plate', (x, -.12, zz), (.30, .10, .30), 'steel_dark')
            rivet(x, -.04, zz, 'brass', .065)
    for zz in (-2.03, -3.91):
        box('Hanging truss cross chord', (0, -.41, zz), (6, .35, .18), 'iron_dark', True)
    for left, right in ((-2.84, -.95), (-.95, .95), (.95, 2.84)):
        for bottom, top in ((-3.82, -2.12), (-1.94, -.46)):
            beam('Suspended X diagonal', (left, -.40, bottom), (right, -.40, top), .12, .20, 'iron_dark')
            beam('Suspended X return', (left, -.40, top), (right, -.40, bottom), .12, .20, 'iron_dark')
    gear(-1.88, -.09, -1.20, .52, 'brass_dark', 'iron', 14, .1)
    gear(1.86, -.09, -2.91, .65, 'brass_dark', 'iron', 16, .12)
    gear(.79, -.09, -2.85, .43, 'brass_dark', 'iron', 11, .21)


SPECS = [
    ('Wall_Storehouse', 6, 4.0, storehouse),
    ('Wall_Gears', 6, 4.5, gears),
    ('Wall_BellArch', 6, 4.5, bell_arch),
    ('Wall_DialBack', 8, 4.76, dial_back),
    ('Wall_Pendulum', 6, 5.0, pendulum),
    ('Wall', 4, 3.0, filler),
    ('Wall_Girder', 6, 1.1, girder),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return tuple(Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
                 for fn in (min, max))


def finish(name, width, height):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    count = triangles(obj)
    if count > 8000:
        # Material boundaries survive; the broad forms are always constructed
        # first, so this only removes excess bevel tessellation/detail.
        mod = obj.modifiers.new('Triangle budget preserving material borders', 'DECIMATE')
        mod.ratio = 7900 / count
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges), name
    bm.to_mesh(obj.data)
    bm.free()
    for poly in obj.data.polygons:
        poly.use_smooth = False
    obj.data.update()
    obj.location, obj.rotation_euler, obj.scale = (0, 0, 0), (0, 0, 0), (1, 1, 1)
    bpy.context.view_layer.update()
    low, high = bounds(obj)
    assert abs(low.x + width / 2) < 1e-4 and abs(high.x - width / 2) < 1e-4, (name, low, high)
    floor = -4 if name == 'Wall_Girder' else 0
    assert abs(low.z - floor) < 1e-4, (name, low)
    assert high.z <= height + .025, (name, high, height)
    assert 1.1 <= high.y - low.y <= 2.25, (name, low, high)
    assert triangles(obj) <= 8000, (name, triangles(obj))
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    used = {obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used) >= 6 and used.issubset(ROLES), (name, used)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    obj['front'], obj['module_width_m'] = '+Y', width
    obj['triangle_count'], obj['chapter'] = triangles(obj), 'Inside the Clockwork'
    obj['origin_rule'] = 'Bottom center; suspended truss intentionally extends to Z=-4' if name == 'Wall_Girder' else 'Bottom center'
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 4) for d in obj.dimensions)}', flush=True)
    return obj


def preview(objects):
    """A single above-front contact sheet; exported FBXs are never rewritten."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.show_shadows, shade.show_cavity = False, True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.25, 1.1
    shade.show_specular_highlight = False
    shade.background_type, shade.background_color = 'WORLD', (.032, .043, .068)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Preview world')
    scene.world.color = (.032, .043, .068)
    helpers = []
    locations = [obj.location.copy() for obj in objects]
    try:
        for role, mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role, (.5, .5, .5)), .18 if role == 'glass' else 1)
        # Four modules across the back, three across the front. Truss remains
        # fully visible below its platform, without a floor plane to hide it.
        positions = [(-12.4, -3.4), (-4.2, -3.4), (4.0, -3.4), (12.6, -3.4),
                     (-9.0, 3.7), (-1.2, 3.7), (7.2, 3.7)]
        for obj, (x, y) in zip(objects, positions):
            obj.location = (x, y, 0)
            obj.show_transparent = True
        camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type, camera.data.ortho_scale = 'ORTHO', 35.8
        target = Vector((.4, 0, .65))
        elevation = math.radians(55)
        camera.location = target + Vector((0, 45 * math.cos(elevation), 45 * math.sin(elevation)))
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()

        def caption(body, loc, size, role='starlight'):
            data = bpy.data.curves.new('Review type', 'FONT')
            data.body, data.align_x, data.size = body, 'CENTER', size
            data.materials.append(MATS[role])
            obj = bpy.data.objects.new('Review caption', data)
            scene.collection.objects.link(obj)
            obj.location, obj.rotation_euler = loc, camera.rotation_euler
            helpers.append(obj)

        descriptions = ['S1 / SPRING STORES', 'S2 / ENGINE ROOM', 'S3 / BELFRY',
                        'S4 / BEHIND THE DIAL', 'LANDMARK / PENDULUM',
                        'FILLER / BATTLEMENT', 'S4 / SUSPENDED EDGE']
        for obj, desc in zip(objects, descriptions):
            y = obj.location.y + (3.30 if obj.name == 'Wall_Girder' else 2.00)
            caption(obj.name, (obj.location.x, y, -.18), .30)
            caption(desc, (obj.location.x, y + .46, -.18), .17, 'brass')
        caption('IV  /  INSIDE THE CLOCKWORK', (.4, -5.8, 5.2), .52)
        caption('MODULAR WALLS     /     +Y FRONT     /     METRE SCALE',
                (.4, -5.1, 5.2), .23, 'brass')
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        scene.camera = None
        for obj in helpers:
            bpy.data.objects.remove(obj, do_unlink=True)
        for obj, loc in zip(objects, locations):
            obj.location, obj.show_transparent = loc, False
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    objects = []
    for index, (name, width, height, build) in enumerate(SPECS):
        random.seed(41031 + index * 7919)
        PARTS = []
        build()
        objects.append(finish(name, width, height))
    assert len(scene.objects) == 7 and all(o.type == 'MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects) == 7 and all(o.type == 'MESH' for o in scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
