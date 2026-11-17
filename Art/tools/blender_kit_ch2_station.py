# 멈춘 역의 벽면 모듈 8종을 만들고 개별 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_station.py
"""Deterministic, role-material station facades for Blender 5.x.

Metres, Z up, +Y front. Width includes all roof and trim overhangs.
Each export has a bottom-centred origin and a planar rear boundary.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2'
ROLES = {
    'wood', 'wood_dark', 'wood_light', 'iron', 'iron_dark', 'rust', 'brass',
    'rail_steel', 'brick', 'brick_dark', 'stone', 'stone_dark', 'roof_tin',
    'soot', 'coal', 'window', 'window_lit', 'glass_lamp', 'canvas', 'leather',
    'leather_dark', 'cloth_red', 'paint_cream', 'paint_green', 'clock_face',
    'steam', 'signal_red', 'signal_amber', 'signal_green',
}
BEVEL_ROLES = {
    'wood', 'wood_dark', 'wood_light', 'iron', 'iron_dark', 'brass',
    'rail_steel', 'roof_tin', 'paint_green',
}
MATERIALS = {}
PARTS = []
RNG = random.Random(0)
BACK = -0.75
FACES_BOX = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]


def material(role):
    assert role in ROLES, role
    if role not in MATERIALS:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (0.5, 0.5, 0.5, 1.0)
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (0.5, 0.5, 0.5, 1.0)
        bsdf.inputs['Roughness'].default_value = 0.85
        bsdf.inputs['Metallic'].default_value = 0.0
        MATERIALS[role] = mat
    return MATERIALS[role]


def mesh(label, vertices, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material(role))
    if bevel and role in BEVEL_ROLES:
        mod = obj.modifiers.new('Softened exposed edges', 'BEVEL')
        mod.width = 0.025
        mod.segments = 2
        if bevel in {'FRONT', 'RIMS'}:
            # Preserve a perfectly flat back and spend faces on exposed edges.
            mod.limit_method = 'WEIGHT'
            weights = data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
            front = max(v.co.y for v in data.vertices)
            adjacent = {}
            for face in data.polygons:
                for key in face.edge_keys:
                    adjacent.setdefault(tuple(sorted(key)), []).append(face.normal.copy())
            for edge in data.edges:
                a, b = (data.vertices[i].co for i in edge.vertices)
                normals = adjacent[tuple(sorted(edge.vertices))]
                sharp = len(normals) == 2 and normals[0].dot(normals[1]) < 0.8
                exposed = sharp and abs(a.y - front) < 1e-6 and abs(b.y - front) < 1e-6
                if bevel == 'RIMS':
                    exposed = sharp and abs(a.z - b.z) < 1e-6
                weights.data[edge.index].value = float(exposed)
        else:
            mod.limit_method = 'ANGLE'
            mod.angle_limit = math.radians(35)
    PARTS.append(obj)
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    vertices = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    obj = mesh(label, vertices, FACES_BOX, role, bevel)
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def prism(label, profile, rear, front, role, bevel=True):
    """Extrude an X/Z polygon along Y. Profiles may be concave."""
    count = len(profile)
    vertices = [(x, y, z) for y in (rear, front) for x, z in profile]
    faces = [tuple(range(count)), tuple(reversed(range(count, count * 2)))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count)
              for i in range(count)]
    return mesh(label, vertices, faces, role, bevel)


def rod(label, a, b, radius, role, top=None, sides=8):
    a, b = Vector(a), Vector(b)
    length = (b - a).length
    top = radius if top is None else top
    vertices = [(r * math.cos(i * math.tau / sides),
                 r * math.sin(i * math.tau / sides), z)
                for r, z in ((radius, -length / 2), (top, length / 2))
                for i in range(sides)]
    faces = [tuple(reversed(range(sides))), tuple(range(sides, 2 * sides))]
    faces += [(i, (i + 1) % sides, (i + 1) % sides + sides, i + sides)
              for i in range(sides)]
    obj = mesh(label, vertices, faces, role, bevel='RIMS')
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def beam(label, a, b, width, role, depth=None, bevel=True):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth or width, (b - a).length), role, bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def arc(label, cx, cz, inner, outer, rear, front, role,
        start=0, end=math.pi, steps=12):
    """Continuous square-section arch or round rim, facing +Y."""
    vertices = []
    closed = abs(end - start - math.tau) < 1e-6
    n = steps if closed else steps + 1
    for y, r in ((rear, inner), (rear, outer), (front, inner), (front, outer)):
        vertices += [(cx + r * math.cos(start + (end - start) * i / steps), y,
                      cz + r * math.sin(start + (end - start) * i / steps))
                     for i in range(n)]
    faces = []
    for i in range(steps):
        for a, b in ((0, 1), (1, 3), (3, 2), (2, 0)):
            j = (i + 1) % n
            faces.append((a * n + i, a * n + j, b * n + j, b * n + i))
    if not closed:
        faces += [(0, n, 3 * n, 2 * n),
                  (steps, 2 * n + steps, 3 * n + steps, n + steps)]
    return mesh(label, vertices, faces, role, bevel='FRONT')


def arch_profile(cx, radius, bottom, spring, steps=12):
    return [(cx - radius, bottom), (cx + radius, bottom)] + [
        (cx + radius * math.cos(i * math.pi / steps),
         spring + radius * math.sin(i * math.pi / steps))
        for i in range(steps + 1)]


def arch_wall(width, height, openings, role='brick'):
    """Solid piers, shaped spandrels and full-depth opening reveals."""
    cursor = -width / 2
    for cx, radius, bottom, spring in sorted(openings):
        left, right = cx - radius, cx + radius
        if left > cursor:
            box('Wall pier', ((cursor + left) / 2, -0.35, height / 2),
                (left - cursor, 0.7, height), role)
        if bottom > 0:
            box('Wall below opening', (cx, -0.35, bottom / 2),
                (radius * 2, 0.7, bottom), role)
        for i in range(12):
            t0, t1 = i * math.pi / 12, (i + 1) * math.pi / 12
            x0, z0 = cx + radius * math.cos(t0), spring + radius * math.sin(t0)
            x1, z1 = cx + radius * math.cos(t1), spring + radius * math.sin(t1)
            prism('Arched spandrel', [(x1, z1), (x0, z0), (x0, height),
                                     (x1, height)], -0.7, 0, role)
        cursor = right
    if cursor < width / 2:
        box('End pier', ((cursor + width / 2) / 2, -0.35, height / 2),
            (width / 2 - cursor, 0.7, height), role)
    box('Flat rear wall', (0, BACK + 0.025, height / 2),
        (width, 0.05, height), role)


def visible_intervals(width, z, openings):
    intervals = [(-width / 2 + 0.06, width / 2 - 0.06)]
    for cx, radius, bottom, spring in openings:
        if not bottom - 0.03 <= z <= spring + radius + 0.03:
            continue
        reach = radius if z <= spring else math.sqrt(
            max(0, radius * radius - (z - spring) ** 2))
        left, right = cx - reach - 0.04, cx + reach + 0.04
        clipped = []
        for a, b in intervals:
            if b <= left or a >= right:
                clipped.append((a, b))
            else:
                if a < left:
                    clipped.append((a, left))
                if b > right:
                    clipped.append((right, b))
        intervals = clipped
    return intervals


def face_rect(label, x0, x1, y, z0, z1, role):
    return mesh(label, [(x0, y, z0), (x0, y, z1), (x1, y, z1),
                        (x1, y, z0)], [(0, 1, 2, 3)], role, False)


def masonry(width, height, openings=(), role='brick_dark', course=0.42):
    # Sparse shallow mortar joints stay readable without hundreds of cubes.
    for row in range(1, math.ceil(height / course)):
        z = row * course
        for left, right in visible_intervals(width, z, openings):
            if right - left > 0.025:
                face_rect('Horizontal mortar joint', left, right, 0.009,
                          z - 0.012, z + 0.012, role)
        mid = z - course / 2
        for left, right in visible_intervals(width, mid, openings):
            x = -width / 2 + 0.55 + (0.5 if row % 2 else 0)
            while x < width / 2:
                if left + 0.03 < x < right - 0.03:
                    face_rect('Staggered mortar joint', x - 0.012, x + 0.012,
                              0.011, z - course + 0.035, z - 0.035, role)
                x += 1.0


def stone_arch(cx, radius, bottom, spring, thickness=0.22, steps=12):
    for i in range(steps):
        a, b = i * math.pi / steps + 0.012, (i + 1) * math.pi / steps - 0.012
        outer = radius + thickness + (0.09 if i in (steps // 2 - 1, steps // 2) else 0)
        profile = [(cx + r * math.cos(t), spring + r * math.sin(t))
                   for r, t in ((radius, a), (outer, a), (outer, b), (radius, b))]
        prism('Arch voussoir and crown keystone', profile, -0.045, 0.12, 'stone')
    for side in (-1, 1):
        box('Stone arch jamb', (cx + side * (radius + thickness / 2), 0.04,
                               (bottom + spring) / 2),
            (thickness, 0.19, spring - bottom), 'stone')


def sloped_roof(width, rear, front, back_z, front_z, ribs=7):
    vertices = [(x, y, z - offset) for offset in (0.12, 0)
                for x, y, z in ((-width / 2, rear, back_z),
                                (width / 2, rear, back_z),
                                (width / 2, front, front_z),
                                (-width / 2, front, front_z))]
    mesh('Sloped tin roof', vertices, FACES_BOX, 'roof_tin')
    for i in range(ribs):
        x = -width / 2 + 0.18 + (width - 0.36) * i / (ribs - 1)
        beam('Raised standing roof seam', (x, rear + 0.06, back_z + 0.014),
             (x, front - 0.06, front_z + 0.014), 0.045, 'roof_tin', bevel='FRONT')
    box('Roof front fascia', (0, front - 0.045, front_z - 0.12),
        (width, 0.09, 0.2), 'roof_tin', bevel='FRONT')


def stain(x, top, length, role, y=0.025, width=0.12):
    lean = RNG.uniform(-0.05, 0.05)
    mesh('Frozen weathering streak', [(x - width / 2, y, top),
         (x + width / 2, y, top), (x + width * 0.26 + lean, y, top - length * 0.7),
         (x + lean, y, top - length)], [(0, 1, 2, 3)], role, False)


def clock(cx, y, z, radius=0.68):
    rod('Stopped clock face', (cx, y - 0.08, z), (cx, y, z),
        radius - 0.045, 'clock_face', sides=24)
    arc('Brass clock bezel', cx, z, radius - 0.07, radius, y - 0.10,
        y + 0.055, 'brass', end=math.tau, steps=24)
    for i in range(12):
        a = i * math.tau / 12
        r0 = radius * (0.74 if i % 3 == 0 else 0.80)
        r1 = radius * 0.9
        beam('Clock hour marker', (cx + r0 * math.sin(a), y + 0.015, z + r0 * math.cos(a)),
             (cx + r1 * math.sin(a), y + 0.015, z + r1 * math.cos(a)),
             0.045, 'iron', depth=0.035, bevel='FRONT')
    # All station timepieces freeze at 10:10.
    beam('Stopped hour hand', (cx, y + 0.07, z),
         (cx - radius * 0.40, y + 0.07, z + radius * 0.23), 0.065, 'iron', bevel='FRONT')
    beam('Stopped minute hand', (cx, y + 0.09, z),
         (cx + radius * 0.61, y + 0.09, z + radius * 0.35), 0.05, 'iron', bevel='FRONT')
    rod('Clock hand pin', (cx, y + 0.08, z), (cx, y + 0.14, z),
        0.07, 'brass', sides=8)


def station_hall():
    openings = [(-1.95, 0.92, 1.15, 3.48), (1.95, 0.92, 1.15, 3.48)]
    arch_wall(8, 6.35, openings)
    masonry(8, 6.28, openings)
    box('Continuous stone sill band', (0, 0.10, 1.02), (8, 0.35, 0.22), 'stone')
    box('Stone foundation', (0, -0.22, 0.19), (8, 1.06, 0.38), 'stone')
    for cx, radius, bottom, spring in openings:
        prism('Recessed arched lit glazing', arch_profile(cx, radius, bottom, spring),
              -0.20, -0.16, 'window_lit')
        stone_arch(cx, radius, bottom, spring, 0.17)
        arc('Iron arched window frame', cx, spring, radius - 0.065, radius,
            -0.16, -0.075, 'iron')
        for x in (cx - radius + 0.04, cx, cx + radius - 0.04):
            top = spring + (radius - 0.07 if x == cx else 0)
            box('Tall iron mullion', (x, -0.07, (bottom + top) / 2),
                (0.065, 0.10, top - bottom), 'iron', bevel='FRONT')
        for z in (2.26, 3.47):
            box('Iron transom', (cx, -0.06, z), (1.82, 0.10, 0.065),
                'iron', bevel='FRONT')
    clock(0, 0.14, 5.37)
    for x in (-3.45, -2.95, -1.1, 0.83, 2.75, 3.40):
        stain(x, 6.20, RNG.uniform(0.25, 0.55), 'soot', width=0.16)
    sloped_roof(8, BACK, 0.34, 6.63, 6.40, ribs=5)


def waiting():
    # Real rectangular opening through individually modeled horizontal boards.
    box('Waiting room flat back', (0, BACK + 0.04, 2.45), (6, 0.08, 4.9), 'wood',
        bevel='FRONT')
    for row in range(10):
        z0, z1 = row * 0.49, (row + 1) * 0.49 - 0.018
        intervals = [(-3, 3)] if z1 <= 1.47 or z0 >= 3.43 else [(-3, -1.43), (1.43, 3)]
        for a, b in intervals:
            box('Waiting room horizontal plank', ((a + b) / 2, -0.31, (z0 + z1) / 2),
                (b - a, 0.62, z1 - z0), 'wood', bevel='FRONT')
    box('Wide recessed waiting room glass', (0, -0.13, 2.45), (2.86, 0.05, 1.94), 'window')
    for x in (-1.45, 0, 1.45):
        box('Waiting window upright', (x, 0.035, 2.45), (0.11, 0.17, 2.08),
            'wood_dark', bevel='FRONT')
    for z in (1.43, 3.47):
        box('Waiting window rail', (0, 0.05, z), (3.04, 0.20, 0.12),
            'wood_dark', bevel='FRONT')
    for side in (-1, 1):
        x = side * 2.05
        box('Open green shutter', (x, 0.11, 2.45), (0.94, 0.17, 2.02),
            'paint_green', rot=(0, 0, side * 0.07), bevel='FRONT')
        for z in (1.80, 2.45, 3.10):
            box('Raised shutter louver', (x, 0.23, z), (0.78, 0.11, 0.16),
                'paint_green', bevel='FRONT')
    box('Blank cream station sign', (0, 0.14, 4.47), (3.35, 0.16, 0.54), 'paint_cream')
    box('Sign top wooden edging', (0, 0.16, 4.77), (3.51, 0.18, 0.08),
        'wood_dark', bevel='FRONT')
    sloped_roof(4.1, -0.05, 0.58, 4.22, 4.08, ribs=4)
    for x in (-1.7, 1.7):
        beam('Iron awning wall bracket', (x, 0.08, 3.20), (x, 0.08, 4.12), 0.075, 'iron')
        beam('Iron awning diagonal bracket', (x, 0.08, 3.24), (x, 0.49, 4.00), 0.075, 'iron')
    sloped_roof(6, BACK, 0.30, 5.24, 5.02, ribs=5)


def warehouse():
    box('Warehouse dark flat back', (0, BACK + 0.025, 2.8), (7, 0.05, 5.6), 'brick_dark')
    # A 0.42 m exposed strip at the left makes the shifted sliding leaf legible.
    for a, b in ((-3.5, -1.70), (1.70, 3.5)):
        box('Warehouse lower brick pier', ((a + b) / 2, -0.35, 1.38),
            (b - a, 0.7, 2.76), 'brick_dark')
    box('Warehouse upper wall core', (0, -0.36, 4.18), (7, 0.68, 2.84), 'wood_dark',
        bevel='FRONT')
    for i in range(12):
        x = -3.5 + (i + 0.5) * 7 / 12
        box('Warehouse upper vertical board', (x, 0.025, 4.18),
            (7 / 12 - 0.025, 0.12, 2.84), 'wood_dark', bevel='FRONT')
    masonry(7, 2.76, [(0, 1.70, 0, 2.76)], role='stone_dark', course=0.46)
    box('Recessed open warehouse gap', (0, -0.55, 1.58), (3.4, 0.04, 3.16), 'soot')
    door_left, door_right = -1.28, 2.10
    for i in range(6):
        x = door_left + (i + 0.5) * (door_right - door_left) / 6
        box('Sliding door plank', (x, 0.18, 1.58),
            ((door_right - door_left) / 6 - 0.018, 0.20, 3.12), 'wood', bevel='FRONT')
    for z in (0.24, 2.98):
        box('Sliding door iron strap', (0.41, 0.31, z), (3.22, 0.08, 0.12),
            'iron', bevel='FRONT')
    beam('Sliding door X brace', (-1.16, 0.33, 0.33), (1.98, 0.33, 2.88), 0.11, 'iron', 0.075)
    beam('Sliding door X brace', (-1.16, 0.34, 2.88), (1.98, 0.34, 0.33), 0.11, 'iron', 0.075)
    box('Sliding door overhead track', (0.1, 0.20, 3.38), (5.1, 0.19, 0.16), 'iron', bevel='FRONT')
    for x in (-0.93, 1.75):
        rod('Door runner wheel', (x, 0.21, 3.27), (x, 0.37, 3.27), 0.13, 'iron', sides=8)
    box('Projecting pulley beam', (0, 0.47, 5.51), (0.25, 2.42, 0.28), 'wood', bevel='FRONT')
    beam('Pulley beam diagonal support', (0, 0.10, 4.68), (0, 1.32, 5.40), 0.14, 'iron')
    arc('Suspended pulley wheel', 0, 5.06, 0.15, 0.25, 1.36, 1.50,
        'iron', end=math.tau, steps=12)
    beam('Pulley hanger', (0, 1.43, 5.48), (0, 1.43, 5.20), 0.07, 'iron')
    rod('Frozen hanging pulley rope', (0.18, 1.43, 4.97), (0.18, 1.43, 4.12),
        0.035, 'canvas', sides=6)
    sloped_roof(7, BACK, 0.48, 5.94, 5.72, ribs=5)


def engine_shed():
    radius, spring = 2.15, 3.15
    opening = [(0, radius, 0, spring)]
    arch_wall(9, 6.65, opening)
    masonry(9, 6.60, opening, course=0.46)
    # Front wall is Y=0, so this dark doorway is precisely 0.6 m recessed.
    profile = arch_profile(0, radius, 0, spring)
    mesh('Dark engine shed interior at 0.6 m', [(x, -0.6, z) for x, z in profile],
         [tuple(reversed(range(len(profile))))], 'soot', False)
    stone_arch(0, radius, 0, spring, 0.30, steps=14)
    box('Shed threshold', (0, -0.18, 0.08), (4.3, 0.92, 0.16), 'stone')
    for x in (-3.40, 3.40):
        rod('Small round shed glazing', (x, 0.02, 4.82), (x, 0.07, 4.82),
            0.40, 'window', sides=16)
        arc('Round stone window surround', x, 4.82, 0.40, 0.54, -0.03, 0.13,
            'stone', end=math.tau, steps=16)
        for a, b in (((x - 0.36, 0.15, 4.82), (x + 0.36, 0.15, 4.82)),
                     ((x, 0.15, 4.46), (x, 0.15, 5.18))):
            beam('Round window iron crossbar', a, b, 0.065, 'iron')
    for x in (-4.05, -3.71, -2.9, 2.82, 3.60, 4.0):
        stain(x, RNG.uniform(5.88, 6.40), RNG.uniform(0.45, 1.0), 'rust', width=0.18)
    box('Shed stone cornice', (0, 0.06, 6.50), (9, 0.27, 0.22), 'stone')
    sloped_roof(9, BACK, 0.72, 6.91, 6.68, ribs=7)


def canopy():
    rear, front = BACK, 1.75
    box('Low platform slab', (0, (rear + front) / 2, 0.20), (8, 2.5, 0.40), 'stone')
    for x in (-3.0, 0, 3.0):
        box('Cast column square plinth', (x, 1.24, 0.53), (0.47, 0.47, 0.26), 'iron')
        rod('Tapered cast iron column', (x, 1.24, 0.65), (x, 1.24, 4.70),
            0.125, 'iron', top=0.09, sides=8)
        box('Cast column capital', (x, 1.24, 4.64), (0.42, 0.38, 0.19), 'iron')
        # Curved pierced spandrels remain open against the sky.
        for side in (-1, 1):
            profile = [(x + side * 0.10, 4.63), (x + side * 0.88, 4.63),
                       (x + side * 0.67, 4.20), (x + side * 0.46, 3.86),
                       (x + side * 0.27, 3.62), (x + side * 0.10, 3.55)]
            prism('Swept decorative cast bracket', profile, 1.17, 1.31, 'iron')
        beam('Canopy rearward knee brace', (x, 1.24, 3.96), (x, 0.12, 4.92), 0.10, 'iron')
    box('Long canopy front girder', (0, 1.24, 4.69), (7.90, 0.20, 0.23), 'iron', bevel='FRONT')
    sloped_roof(8, rear, front, 5.23, 4.82, ribs=7)
    # Lantern sits between columns, clear of the central shaft.
    x, y = 1.60, 1.05
    rod('Hanging lantern chain', (x, y, 4.70), (x, y, 3.49), 0.032, 'iron', sides=6)
    box('Hanging lantern glass', (x, y, 3.15), (0.34, 0.34, 0.55), 'glass_lamp')
    for z in (2.83, 3.47):
        box('Lantern frame cap', (x, y, z), (0.47, 0.47, 0.10), 'iron')
    for dx, dy in ((-0.19, -0.19), (-0.19, 0.19), (0.19, -0.19), (0.19, 0.19)):
        rod('Lantern cage upright', (x + dx, y + dy, 2.87),
            (x + dx, y + dy, 3.44), 0.025, 'iron', sides=4)


def ticket():
    # Cream wall panels leave a true inset ticket-window opening.
    for a, b in ((-2.5, -1.22), (1.22, 2.5)):
        box('Cream ticket office side wall', ((a + b) / 2, -0.35, 2.06),
            (b - a, 0.7, 4.12), 'paint_cream')
    for z, height in ((0.69, 1.38), (3.58, 1.08)):
        box('Cream ticket office centre wall', (0, -0.35, z), (2.44, 0.7, height), 'paint_cream')
    box('Ticket office flat back', (0, BACK + 0.025, 2.06), (5, 0.05, 4.12), 'paint_cream')
    box('Ticket office lit glass', (0, -0.20, 2.20), (2.44, 0.05, 1.64), 'window_lit')
    for x in (-1.25, 1.25):
        box('Dark ticket window jamb', (x, 0.03, 2.20), (0.15, 0.21, 1.88),
            'wood_dark', bevel='FRONT')
    box('Dark ticket window lintel', (0, 0.03, 3.08), (2.65, 0.21, 0.14),
        'wood_dark', bevel='FRONT')
    for x in (-0.90, -0.45, 0, 0.45, 0.90):
        # The central service opening is taller than the flanking bars.
        bottom = 1.86 if abs(x) < 0.5 else 1.44
        box('Iron ticket security bar', (x, 0.07, (bottom + 2.98) / 2),
            (0.075, 0.09, 2.98 - bottom), 'iron', bevel='FRONT')
    box('Iron ticket grille crossbar', (0, 0.08, 2.50), (2.35, 0.09, 0.07),
        'iron', bevel='FRONT')
    box('Projecting dark ticket counter', (0, 0.36, 1.43), (3.08, 1.10, 0.19), 'wood_dark')
    for x in (-0.98, 0.98):
        beam('Counter support', (x, 0.02, 0.94), (x, 0.71, 1.33), 0.12, 'wood_dark')
    rod('Brass counter bell foot', (0.90, 0.53, 1.53), (0.90, 0.53, 1.59),
        0.24, 'brass', sides=12)
    rod('Brass counter bell dome', (0.90, 0.53, 1.59), (0.90, 0.53, 1.82),
        0.22, 'brass', top=0.10, sides=12)
    rod('Bell push button', (0.90, 0.53, 1.82), (0.90, 0.53, 1.92),
        0.055, 'brass', sides=8)
    prism('Cream gable pediment', [(-2.5, 4.08), (2.5, 4.08), (0, 5.26)],
          BACK, 0.04, 'paint_cream')
    for side in (-1, 1):
        x0, x1 = sorted((0, side * 2.5))
        z0, z1 = 5.34 - abs(x0) * 0.47, 5.34 - abs(x1) * 0.47
        vertices = [(x, y, z - offset) for offset in (0.12, 0)
                    for x, y, z in ((x0, BACK, z0), (x1, BACK, z1),
                                    (x1, 0.61, z1), (x0, 0.61, z0))]
        mesh('Little gabled tin roof', vertices, FACES_BOX, 'roof_tin')
        fx0, fx1 = x0 * 0.98, x1 * 0.98
        beam('Gable front fascia', (fx0, 0.58, 5.27 - abs(fx0) * 0.47),
             (fx1, 0.58, 5.27 - abs(fx1) * 0.47), 0.12, 'roof_tin')
    box('Tin gable ridge', (0, -0.04, 5.34), (0.15, 1.42, 0.13), 'roof_tin')


def wall():
    box('Generic solid brick wall', (0, -0.35, 1.40), (4, 0.70, 2.80), 'brick')
    box('Generic flat rear', (0, BACK + 0.025, 1.4), (4, 0.05, 2.8), 'brick')
    masonry(4, 2.78, course=0.40)
    box('Continuous stone wall cap', (0, -0.25, 2.90), (4, 1.0, 0.20), 'stone')
    box('Stone wall footing', (0, -0.20, 0.12), (4, 1.1, 0.24), 'stone')


def embankment():
    # Five large-block courses recede toward one common, vertical rear plane.
    for row in range(5):
        front = 0.97 - row * 0.20
        width = 6 / (5 if row % 2 == 0 else 6)
        count = 5 if row % 2 == 0 else 6
        for i in range(count):
            a, b = -3 + i * width, -3 + (i + 1) * width
            box('Stepped massive retaining block', ((a + b) / 2,
                (BACK + front) / 2, row * 0.68 + 0.335),
                (b - a - 0.025, front - BACK, 0.65), 'stone_dark')
    box('Retaining wall flat rear', (0, BACK + 0.025, 1.69), (6, 0.05, 3.38), 'stone_dark')
    box('Retaining wall coping', (0, -0.30, 3.44), (6, 0.90, 0.16), 'stone_dark')
    for x in (-2.8, -1.87, -0.94, 0, 0.94, 1.87, 2.8):
        rod('Steel fence post', (x, -0.27, 3.52), (x, -0.27, 4.50),
            0.055, 'rail_steel', sides=6)
    for z in (3.75, 4.31):
        box('Steel fence horizontal rail', (0, -0.27, z), (5.76, 0.08, 0.10),
            'rail_steel', bevel='FRONT')
    for x, y, scale in ((-2.22, 1.12, 0.55), (-1.75, 1.20, 0.36),
                         (1.60, 1.06, 0.50), (2.05, 1.23, 0.38)):
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1)
        for vert in bm.verts:
            vert.co *= 1 + RNG.uniform(-0.17, 0.17)
            vert.co.x = x + vert.co.x * scale / 2
            vert.co.y = y + vert.co.y * scale * 0.40
            vert.co.z = max(0, (vert.co.z + 0.80) * scale * 0.38)
        bm.verts.ensure_lookup_table()
        mesh('Faceted loose coal at wall foot', [tuple(v.co) for v in bm.verts],
             [tuple(v.index for v in face.verts) for face in bm.faces], 'coal')
        bm.free()


SPECS = [
    ('Facade_StationHall', 8.0, station_hall),
    ('Facade_Waiting', 6.0, waiting),
    ('Facade_Warehouse', 7.0, warehouse),
    ('Facade_EngineShed', 9.0, engine_shed),
    ('Facade_Canopy', 8.0, canopy),
    ('Facade_Ticket', 5.0, ticket),
    ('Wall', 4.0, wall),
    ('Cliff_Embankment', 6.0, embankment),
]


def triangle_count(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, width):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + '_Mesh'
    count = triangle_count(obj)
    if count > 3000:
        raise RuntimeError(f'{name} exceeds the triangle budget: {count}')

    coords = [v.co for v in obj.data.vertices]
    low = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
    high = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    assert abs(high.x - low.x - width) < 0.002, (name, low, high, width)
    # Centre the complete footprint without rescaling the designed architecture.
    shift = Vector((-(low.x + high.x) / 2, -(low.y + high.y) / 2, -low.z))
    obj.data.transform(Matrix.Translation(shift))
    for vertex in obj.data.vertices:
        if abs(vertex.co.z) < 1e-5:
            vertex.co.z = 0.0
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    # Clamped bevels on narrow bars can collapse their original centre edges.
    bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    for face in bm.faces:
        if all(len(edge.link_faces) == 1 for edge in face.edges) and face.normal.y < 0:
            face.normal_flip()
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
        if face.area < 1e-10:
            raise RuntimeError(f'{name} contains a degenerate face')
    obj.data.update()
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    dims = tuple(obj.dimensions)
    height_range = (2.999, 3.001) if name == 'Wall' else (4.49, 7.0)
    assert height_range[0] <= dims[2] <= height_range[1], (name, dims)
    assert abs(dims[0] - width) < 0.002, (name, dims)
    if name == 'Facade_Canopy':
        assert abs(dims[1] - 2.5) < 0.002, dims
    assert min(v.co.z for v in obj.data.vertices) == 0
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(mat.name in ROLES for mat in obj.data.materials)
    assert len(bpy.context.scene.objects) == 1
    obj['front'] = '+Y'
    obj['width_m'] = width
    obj['chapter'] = 'The Stopped Station'
    obj['stopped_time'] = '10:10'
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangle_count(obj)} {tuple(round(d, 4) for d in dims)}', flush=True)


def main():
    global PARTS, RNG
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for index, (name, width, build) in enumerate(SPECS):
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)
        PARTS = []
        RNG = random.Random(220617 + index * 7919)
        build()
        finish(name, width)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
