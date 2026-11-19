# 3장 보스 소품 10종과 재질별 색상 미리보기를 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_bossprops.py
"""Standalone Blender 5.x builder. Exported role materials are always grey.

The altar's 0.25 m dimension describes its submerged ring body. Its raised
numerals, compass relief and crystal clusters extend above the floor at Z=0.
Preview transforms and colours are temporary and are applied after exporting.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_bossprops_preview.png'
PALETTE = {
    'ivory': (.96, .87, .65), 'gold': (.96, .61, .12),
    'gold_dark': (.20, .095, .025), 'rose_gold': (.81, .40, .31),
    'gem_amber': (1.0, .35, .035), 'gem_rose': (.87, .06, .32),
    'cloth_rose': (.45, .055, .17), 'mirror': (.32, .64, .79),
    'mirror_back': (.14, .19, .27), 'wood': (.26, .105, .07),
    'coral': (.91, .24, .22), 'marble': (.82, .83, .79),
    'marble_dark': (.26, .31, .39), 'glaze_teal': (.025, .43, .40),
    'crystal': (.56, .79, .97), 'crystal_core': (.95, .99, 1.0),
    'crystal_rose': (.80, .32, .61), 'stardust': (1.0, .83, .41),
    'tile_cream': (.79, .64, .44), 'bronze': (.43, .23, .075),
    'window_lit': (1.0, .73, .27),
}
MATERIALS = {}
VERTICES, FACES, FACE_ROLES = [], [], []
TAU = math.tau


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        bsdf = nodes.new('ShaderNodeBsdfPrincipled')
        bsdf.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        bsdf.inputs['Roughness'].default_value = .8
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
        MATERIALS[role] = mat


def add(verts, faces, role, bevel=0, loc=(0, 0, 0), rot=(0, 0, 0), segments=2):
    """Append closed geometry, baking local bevels before the final mesh join."""
    if bevel:
        bm = bmesh.new()
        bv = [bm.verts.new(v) for v in verts]
        for face in faces:
            bm.faces.new([bv[i] for i in face])
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel,
                        segments=segments, affect='EDGES', clamp_overlap=True)
        bm.verts.ensure_lookup_table()
        bm.verts.index_update()
        verts = [tuple(v.co) for v in bm.verts]
        faces = [tuple(v.index for v in f.verts) for f in bm.faces]
        bm.free()
    transform = Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4()
    offset = len(VERTICES)
    VERTICES.extend(tuple(transform @ Vector(v)) for v in verts)
    FACES.extend(tuple(offset + i for i in f) for f in faces)
    FACE_ROLES.extend([role] * len(faces) if isinstance(role, str) else role)


def box(loc, size, role, rot=(0, 0, 0), bevel=.02, segments=2):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    add(verts, faces, role, min(bevel, min(size) * .22), loc, rot, segments)


def prism(profile, low, high, role, axis='Y', bevel=0,
          loc=(0, 0, 0), rot=(0, 0, 0)):
    """Extrude a 2D outline in X/Z (Y axis) or X/Y (Z axis)."""
    if axis == 'Y':
        verts = [(x, d, z) for d in (low, high) for x, z in profile]
    else:
        verts = [(x, y, d) for d in (low, high) for x, y in profile]
    n = len(profile)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    add(verts, faces, role, bevel, loc, rot)


def cylinder(loc, radius, depth, role, n=16, rot=(0, 0, 0), bevel=0):
    verts = [(radius * math.cos(TAU * i / n), radius * math.sin(TAU * i / n), z)
             for z in (-depth / 2, depth / 2) for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    add(verts, faces, role, bevel, loc, rot)


def ring(loc, outer, width, depth, role, n=32, rot=(0, 0, 0), bevel=0):
    section = [(outer - width, -depth / 2), (outer, -depth / 2),
               (outer, depth / 2), (outer - width, depth / 2)]
    verts = [(r * math.cos(TAU * i / n), r * math.sin(TAU * i / n), z)
             for r, z in section for i in range(n)]
    faces = [(k * n + i, k * n + (i + 1) % n,
              ((k + 1) % 4) * n + (i + 1) % n, ((k + 1) % 4) * n + i)
             for k in range(4) for i in range(n)]
    add(verts, faces, role, bevel, loc, rot)


def tube(points, radius, role, n=6):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(i - 1, 0)])).normalized()
        reference = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        side = reference.cross(tangent).normalized()
        other = tangent.cross(side)
        for j in range(n):
            a = TAU * j / n
            verts.append(tuple(Vector(point) + radius *
                               (math.cos(a) * side + math.sin(a) * other)))
    faces = [tuple(reversed(range(n)))]
    faces += [(k * n + j, k * n + (j + 1) % n,
               (k + 1) * n + (j + 1) % n, (k + 1) * n + j)
              for k in range(len(points) - 1) for j in range(n)]
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    add(verts, faces, role)


def beam(a, b, width, depth, role, bevel=.02):
    a, b = Vector(a), Vector(b)
    rotation = (b - a).to_track_quat('Z', 'Y').to_euler()
    box((a + b) / 2, (width, depth, (b - a).length), role, rotation, bevel)


def jewel(loc, size, role, n=6, secondary=None):
    x, y, z = (s / 2 for s in size)
    verts = [(0, 0, -z), (0, 0, z)]
    verts += [(x * math.cos(TAU * i / n), y * math.sin(TAU * i / n), 0)
              for i in range(n)]
    faces, roles = [], []
    for i in range(n):
        faces.extend([(0, 2 + (i + 1) % n, 2 + i),
                      (1, 2 + i, 2 + (i + 1) % n)])
        roles.extend([role, secondary if secondary and i % 3 == 0 else role])
    add(verts, faces, roles, loc=loc)


def ellipsoid(loc, size, role, n=12, rows=6):
    verts = [(0, 0, -size[2] / 2), (0, 0, size[2] / 2)]
    for j in range(1, rows):
        latitude = -math.pi / 2 + j * math.pi / rows
        for i in range(n):
            a = TAU * i / n
            verts.append((size[0] / 2 * math.cos(latitude) * math.cos(a),
                          size[1] / 2 * math.cos(latitude) * math.sin(a),
                          size[2] / 2 * math.sin(latitude)))
    faces = [(0, 2 + (i + 1) % n, 2 + i) for i in range(n)]
    faces += [(2 + k * n + i, 2 + k * n + (i + 1) % n,
               2 + (k + 1) * n + (i + 1) % n, 2 + (k + 1) * n + i)
              for k in range(rows - 2) for i in range(n)]
    last = 2 + (rows - 2) * n
    faces += [(1, last + i, last + (i + 1) % n) for i in range(n)]
    add(verts, faces, role, loc=loc)


def star(loc, radius, role='gold', axis='Y', points=5, depth=.04, rot=(0, 0, 0)):
    profile = []
    for i in range(points * 2):
        angle = math.pi / 2 + i * math.pi / points
        r = radius if i % 2 == 0 else radius * .44
        profile.append((r * math.cos(angle), r * math.sin(angle)))
    prism(profile, -depth / 2, depth / 2, role, axis, loc=loc, rot=rot)


def scroll(x, y, z, size, sign=1, role='gold'):
    points = []
    for i in range(20):
        t = i / 19
        angle = -.5 * math.pi + t * TAU * 1.18
        r = size * (1 - .83 * t)
        points.append((x + sign * r * math.cos(angle), y,
                       z + r * math.sin(angle)))
    tube(points, max(.006, size * .12), role)


def face_frame(cx, y, bottom, width, height, role='gold', thickness=.07):
    box((cx - width / 2, y, bottom + height / 2), (thickness, .055, height), role)
    box((cx + width / 2, y, bottom + height / 2), (thickness, .055, height), role)
    for z in (bottom, bottom + height):
        box((cx, y, z), (width + thickness, .055, thickness), role)


def merchant_mask():
    # Two thick annuli produce real almond-shaped eye openings on both sides.
    for sign in (-1, 1):
        n, verts = 24, []
        for y, outer in ((.045, True), (.045, False), (.09, False), (.09, True)):
            for i in range(n):
                a = TAU * i / n
                rx, rz = (.143, .107) if outer else (.082, .039)
                x = sign * .124 + rx * math.cos(a)
                z = rz * math.sin(a) * abs(math.sin(a)) ** .15
                curve = .07 * (1 - (abs(x) / .29) ** 1.5)
                verts.append((x, y + curve, z))
        faces = [(k * n + i, k * n + (i + 1) % n,
                  ((k + 1) % 4) * n + (i + 1) % n, ((k + 1) % 4) * n + i)
                 for k in range(4) for i in range(n)]
        add(verts, faces, 'ivory')
        for radius_x, radius_z, role, y in [(.147, .111, 'rose_gold', .115),
                                            (.090, .047, 'gold', .17)]:
            points = [(sign * .124 + radius_x * math.cos(TAU * i / 24),
                       y - .035 * abs(math.cos(TAU * i / 24)),
                       radius_z * math.sin(TAU * i / 24)) for i in range(25)]
            tube(points, .009, role)
        scroll(sign * .165, .165, .075, .042, sign)
        scroll(sign * .205, .125, -.067, .035, -sign)
        jewel((sign * .208, .15, -.082), (.051, .035, .051), 'mirror', n=4)
        prism([(sign * .252, .025), (sign * .31, -.07),
               (sign * .295, -.18), (sign * .256, -.137)],
              0, .027, 'cloth_rose')
        tube([(sign * .264, .032, -.03), (sign * .292, .032, -.10),
              (sign * .278, .032, -.15)], .007, 'rose_gold')
    prism([(-.055, .081), (.055, .081), (.038, -.012),
           (.025, -.105), (0, -.137), (-.025, -.105), (-.038, -.012)],
          .09, .20, 'ivory', bevel=.005)
    tube([(-.034, .205, -.075), (0, .225, -.11), (.034, .205, -.075)], .009, 'gold')
    star((0, .13, .142), .063, points=10, depth=.026)
    cylinder((0, .154, .142), .032, .016, 'rose_gold', 12, (math.pi / 2, 0, 0))
    jewel((0, .18, .142), (.041, .044, .041), 'gem_amber', n=8)


def sword_box():
    box((0, 0, .09), (1.6, 1.6, .18), 'wood')
    box((0, 0, 1.24), (1.6, 1.6, .16), 'wood')
    # Four independently constructed wall openings, each with a recessed bore.
    for side in range(4):
        angle = side * math.pi / 2
        matrix = Euler((0, 0, angle)).to_matrix()

        def wall_box(x, y, z, size, role, bevel=.02):
            box(matrix @ Vector((x, y, z)), size, role, (0, 0, angle), bevel)

        for x in (-.52, .52):
            wall_box(x, .72, .67, (.50, .16, .98), 'wood')
            wall_box(x, .813, .67, (.39, .04, .80), 'coral')
        for z, height in ((.365, .37), (.995, .33)):
            wall_box(0, .72, z, (.55, .16, height), 'wood')
            wall_box(0, .814, z, (.49, .04, height - .06), 'coral')
        wall_box(0, .625, .69, (.57, .035, .20), 'gold_dark', 0)
        for z in (.575, .805):
            wall_box(0, .831, z, (.64, .04, .04), 'rose_gold')
        for x in (-.32, .32):
            wall_box(x, .831, .69, (.04, .04, .27), 'gold')
        for z in (.19, 1.18):
            wall_box(0, .825, z, (1.50, .055, .065), 'gold')
        for x in (-.53, .53):
            center = matrix @ Vector((x, .85, .70))
            star(center, .145, 'ivory', rot=(0, 0, angle))
            center = matrix @ Vector((x, .883, .70))
            jewel(center, (.075, .055, .075), 'gem_rose', n=4)
        for x in (-.72, .72):
            wall_box(x, .83, .69, (.08, .065, .96), 'rose_gold')
            for z in (.28, 1.1):
                center = matrix @ Vector((x, .88, z))
                jewel(center, (.09, .06, .09), 'glaze_teal', n=4)
    for x in (-.72, .72):
        for y in (-.72, .72):
            for z in (.16, 1.23):
                box((x, y, z), (.18, .18, .19), 'gold')
    box((0, 0, 1.34), (1.30, 1.30, .06), 'coral')
    for angle in (0, math.pi / 2):
        box((0, 0, 1.38), (.09, 1.34, .035), 'rose_gold', (0, 0, angle))
    star((0, 0, 1.43), .28, axis='Z', depth=.09)
    jewel((0, 0, 1.50), (.14, .14, .11), 'gem_amber')


def magic_sword():
    # Cross-section has a central ridge and contrasting side facets.
    profile = [(-.044, .35), (.044, .35), (.039, 1.52), (0, 1.80), (-.039, 1.52)]
    prism(profile, -.024, .024, 'ivory', axis='Z', bevel=.003)
    for sign in (-1, 1):
        prism([(sign * .027, .40), (sign * .040, .40),
               (sign * .035, 1.49), (0, 1.75), (sign * .024, 1.49)],
              .025, .03, 'rose_gold', axis='Z')
    prism([(-.010, .39), (.010, .39), (.010, 1.48), (0, 1.70), (-.010, 1.48)],
          .027, .035, 'gold', axis='Z')
    for y in (.66, .96, 1.26):
        star((0, y, .038), .019, 'crystal_core', 'Z', points=4, depth=.006)
    cylinder((0, .17, 0), .031, .25, 'coral', 10, (math.pi / 2, 0, 0))
    for y in (.07, .125, .18, .235, .285):
        ring((0, y, 0), .034, .008, .024, 'rose_gold', 10, (math.pi / 2, 0, 0))
    prism([(-.09, .295), (-.09, .35), (-.04, .375),
           (0, .35), (.04, .375), (.09, .35), (.09, .295), (0, .32)],
          -.033, .033, 'gold', axis='Z', bevel=.005)
    jewel((0, .33, .032), (.052, .052, .033), 'gem_rose', n=6)
    jewel((0, .026, 0), (.065, .052, .072), 'gem_amber', n=8)


def mirror_panel():
    box((0, 0, 1.41), (5.84, .13, 2.16), 'mirror_back')
    for sign in (-1, 1):
        y = sign * .08
        box((0, y, 1.42), (5.25, .018, 1.67), 'mirror', bevel=.008)
        face_frame(0, sign * .105, .46, 5.70, 1.99, 'rose_gold', .13)
        face_frame(0, sign * .125, .55, 5.40, 1.78, 'gold', .095)
        for x in (-2.84, 2.84):
            box((x, sign * .07, 1.42), (.20, .11, 2.10), 'ivory')
        for z in (.36, 2.49):
            box((0, sign * .065, z), (6, .11, .18), 'gold')
            box((0, sign * .13, z), (5.92, .035, .055), 'glaze_teal')
            for i in range(19):
                jewel((-2.70 + i * .30, sign * .15, z),
                      (.095, .07, .095), 'rose_gold', n=6)
        for x in (-2.50, 2.50):
            for z, direction in ((.77, 1), (2.06, -1)):
                scroll(x, sign * .13, z, .18, direction * (1 if x > 0 else -1))
                jewel((x, sign * .17, z), (.13, .07, .13), 'gem_rose')
        star((0, sign * .15, 2.48), .16, 'ivory', points=8, depth=.025)
    for x in (-2.35, 2.35):
        box((x, 0, .115), (.68, .25, .23), 'marble', bevel=.035)
        box((x, 0, .27), (.43, .21, .12), 'rose_gold')
        box((x, .132, .13), (.38, .025, .08), 'glaze_teal')


def dove():
    ellipsoid((0, 0, 0), (.22, .39, .24), 'crystal')
    ellipsoid((0, .225, .065), (.15, .17, .15), 'crystal_core')
    # Broad, individually faceted pinions make a V in plan and elevation.
    for sign in (-1, 1):
        for i in range(5):
            root = (sign * .055, .04 - i * .033)
            tip = (sign * (.35 + .022 * i), .19 - i * .10)
            profile = [root, (tip[0] - sign * .055, tip[1] + .065),
                       tip, (tip[0] - sign * .018, tip[1] - .07),
                       (root[0], root[1] - .055)]
            prism(profile, .015 + i * .006, .07 + i * .006,
                  'crystal_core' if i % 2 == 0 else 'crystal', 'Z',
                  rot=(0, -sign * .22, 0))
            jewel((tip[0] * .86, tip[1], .12), (.075, .10, .055),
                  'crystal_rose' if i in (1, 3) else 'stardust', n=4)
        tube([(sign * .055, .06, .085), (sign * .20, .10, .13),
              (sign * .33, .14, .17)], .014, 'gold')
        jewel((sign * .061, .285, .088), (.021, .025, .025), 'gem_amber', n=4)
    for i in (-1, 0, 1):
        prism([(-.035 + i * .045, -.14), (.035 + i * .045, -.14),
               (.055 + i * .065, -.37), (-.055 + i * .065, -.37)],
              -.03, .015, 'crystal_rose' if i else 'crystal_core', 'Z')
    prism([(-.029, .27), (.029, .27), (0, .40)], -.005, .035, 'gold', 'Z')
    ellipsoid((0, .095, -.033), (.16, .12, .15), 'crystal_core')
    ring((0, .10, -.017), .087, .014, .018, 'rose_gold', 16, (math.pi / 2, 0, 0))


def number_card():
    box((0, 0, .65), (.9, .025, 1.3), 'ivory', bevel=.025)
    box((0, -.018, .65), (.84, .012, 1.24), 'coral', bevel=.02)
    for sign in (-1, 1):
        y = sign * .027
        face_frame(0, y, .075, .77, 1.15, 'gold', .034)
        face_frame(0, y, .12, .68, 1.06, 'rose_gold', .016)
        for x in (-.31, .31):
            for z in (.18, 1.12):
                scroll(x, y, z, .055, 1 if x > 0 else -1)
                jewel((x, y + sign * .006, z), (.041, .014, .041), 'gem_rose', 4)
    cylinder((0, .023, .65), .30, .012, 'tile_cream', 48, (math.pi / 2, 0, 0))
    ring((0, .031, .65), .326, .026, .012, 'gold', 48, (math.pi / 2, 0, 0))
    for z in (.19, 1.11):
        star((0, .031, z), .068, 'coral', points=4, depth=.009)
    # Back clock is raised entirely on the negative-Y face.
    for radius in (.34, .27):
        ring((0, -.030, .65), radius, .018, .010, 'gold', 40, (math.pi / 2, 0, 0))
    for i in range(12):
        a = TAU * i / 12
        beam((.28 * math.sin(a), -.038, .65 + .28 * math.cos(a)),
             (.315 * math.sin(a), -.038, .65 + .315 * math.cos(a)),
             .019, .010, 'ivory', bevel=0)
    beam((0, -.04, .65), (-.12, -.04, .80), .025, .012, 'gold', 0)
    beam((0, -.04, .65), (.19, -.04, .69), .018, .012, 'gold', 0)
    jewel((0, -.042, .65), (.06, .018, .06), 'gem_amber')
    for z in (.22, 1.08):
        star((0, -.03, z), .095, 'ivory', points=8, depth=.01)


def memory_shard():
    # The main bipyramid begins at Z=.4. Only the single dust diamond is lower.
    jewel((0, 0, 1.30), (.63, .57, 1.80), 'crystal', 8, 'crystal_rose')
    jewel((0, .18, 1.30), (.28, .20, .82), 'crystal_core', 6, 'crystal_rose')
    ring((0, 0, 1.17), .50, .050, .060, 'gold', 40, (.30, .10, 0))
    for sign in (-1, 1):
        tube([(sign * .22, 0, 1.17), (sign * .46, 0, 1.17)], .022, 'rose_gold')
        jewel((sign * .48, 0, 1.19), (.11, .11, .17), 'gem_rose')
    ring((0, 0, .66), .11, .035, .065, 'gold', 8)
    for sign in (-1, 1):
        jewel((sign * .27, -.20, .77), (.13, .13, .37), 'crystal_rose', 5, 'crystal_core')
    jewel((.12, .07, .035), (.055, .055, .070), 'stardust', 4)


def curtain():
    box((0, -.02, .25), (8, 1.56, .50), 'marble', bevel=.04)
    box((0, .765, .27), (8, .05, .36), 'marble_dark')
    for i in range(25):
        x = -3.84 + i * .32
        box((x, .796, .27), (.27, .025, .25), 'glaze_teal', bevel=.014)
        if i % 2 == 0:
            star((x, .815, .27), .071, 'ivory', points=4, depth=.018)
    for z in (.065, .475):
        box((0, .79, z), (8, .05, .045), 'gold')
    box((0, .04, .51), (8, 1.50, .04), 'rose_gold')
    # A closed, corrugated velvet volume. Coral ridges are broad raised folds.
    count = 120
    xs = [-4 + 8 * i / count for i in range(count + 1)]
    front = [-.12 + .28 * math.cos(TAU * 10 * i / count) for i in range(count + 1)]
    verts = []
    for back in (False, True):
        for top in (False, True):
            for i, x in enumerate(xs):
                z = 5.27 if top else .54 + .05 * (1 - math.cos(TAU * i / 12))
                verts.append((x, front[i] - (.075 if back else 0), z))
    row = count + 1
    faces, roles = [], []
    for side in range(2):
        for i in range(count):
            offset = side * row * 2
            faces.append((offset + i, offset + i + 1,
                          offset + row + i + 1, offset + row + i))
            roles.append('coral' if not side and i % 12 in (0, 1, 10, 11) else 'cloth_rose')
    for i in range(count):
        faces.extend([(i, 2 * row + i, 2 * row + i + 1, i + 1),
                      (row + i, row + i + 1, 3 * row + i + 1, 3 * row + i)])
        roles.extend(['cloth_rose', 'cloth_rose'])
    faces.extend([(0, row, 3 * row, 2 * row),
                  (count, 2 * row + count, 3 * row + count, row + count)])
    roles.extend(['cloth_rose', 'cloth_rose'])
    add(verts, faces, roles)
    for i in range(10):
        cx = -3.6 + i * .8
        profile = [(cx - .4, 5.34), (cx + .4, 5.34)]
        scallop = []
        for j in range(13):
            x = cx + .4 - .8 * j / 12
            z = 4.98 - .39 * math.sin(math.pi * j / 12)
            profile.append((x, z))
            scallop.append((x, .31, z))
        prism(profile, .17, .29, 'cloth_rose')
        tube(scallop, .032, 'gold')
        tube([(cx, .33, 4.59), (cx, .33, 4.39)], .034, 'rose_gold')
        jewel((cx, .33, 4.34), (.12, .12, .20), 'gold', 6)
        for sign in (-1, 1):
            tube([(cx + sign * .075, .33, 4.41),
                  (cx + sign * .065, .33, 4.24)], .024, 'gold')
    for z, height, role in ((5.32, .14, 'rose_gold'), (5.44, .12, 'gold')):
        box((0, .03, z), (8, .55, height), role)
    for i in range(20):
        star((-3.8 + i * .4, .325, 5.34), .075, 'ivory', points=4, depth=.035)
    for x in (-3.13, 3.13):
        box((x, .11, 3.78), (.20, .16, .45), 'gold')
        tube([(x, .15, 3.80), (x, .58, 3.80), (x, .69, 3.62)], .045, 'gold')
        scroll(x, .34, 3.73, .16, 1 if x > 0 else -1, 'rose_gold')
        box((x, .59, 3.28), (.30, .31, .45), 'window_lit')
        for z in (3.02, 3.53):
            box((x, .59, z), (.39, .39, .09), 'gold')
        for dx in (-.16, .16):
            box((x + dx, .76, 3.28), (.045, .045, .48), 'rose_gold')
        jewel((x, .59, 3.64), (.20, .20, .18), 'gem_amber')


def arch_outline(cx, bottom, spring, radius, n=20):
    return [(cx - radius, bottom), (cx + radius, bottom)] + [
        (cx + radius * math.cos(math.pi * i / n),
         spring + radius * math.sin(math.pi * i / n)) for i in range(n + 1)]


def arch_band(cx, y, bottom, spring, radius, width, role):
    outer = arch_outline(cx, bottom, spring, radius)
    inner = arch_outline(cx, bottom + width, spring, radius - width)
    n = len(outer)
    verts = [(x, yy, z) for yy, profile in ((y - .06, outer), (y - .06, inner),
                                           (y + .06, inner), (y + .06, outer))
             for x, z in profile]
    faces = [(k * n + i, k * n + (i + 1) % n,
              ((k + 1) % 4) * n + (i + 1) % n, ((k + 1) % 4) * n + i)
             for k in range(4) for i in range(n)]
    add(verts, faces, role)


def mirror_hall():
    box((0, -.43, 2.25), (6, .30, 4.5), 'marble_dark')
    for z, depth, height, role in ((.16, 1.2, .32, 'marble'),
                                   (.39, 1.05, .14, 'rose_gold'),
                                   (4.37, .94, .26, 'marble'),
                                   (4.22, .86, .065, 'gold')):
        box((0, -.02, z), (6, depth, height), role, bevel=.03)
    for i in range(24):
        x = -2.875 + .25 * i
        box((x, .596, .17), (.20, .025, .18), 'glaze_teal', bevel=.014)
    for cx in (-1.98, 0, 1.98):
        prism(arch_outline(cx, .47, 3.35, .80), -.29, -.20, 'mirror_back')
        prism(arch_outline(cx, .58, 3.35, .69), -.195, -.17, 'mirror')
        arch_band(cx, -.12, .49, 3.35, .84, .12, 'gold')
        arch_band(cx, -.04, .57, 3.35, .72, .045, 'rose_gold')
        for j in range(13):
            a = j * math.pi / 12
            jewel((cx + .785 * math.cos(a), -.025, 3.35 + .785 * math.sin(a)),
                  (.09, .075, .09), 'rose_gold', 6)
        for side in (-1, 1):
            for z in (.85, 1.45, 2.05, 2.65, 3.25):
                jewel((cx + side * .785, -.025, z), (.09, .075, .09), 'gem_rose', 4)
            scroll(cx + side * .50, -.09, .84, .14, side)
        star((cx, -.035, 4.10), .14, 'gold', points=8, depth=.055)
        jewel((cx, .015, 4.10), (.11, .08, .11), 'gem_amber')
    for x in (-2.90, -.99, .99, 2.90):
        box((x, -.04, .59), (.20, .60, .25), 'ivory')
        # Fluted prism with alternating radial recesses.
        n = 16
        verts = [(x + (.105 if i % 2 else .075) * math.cos(TAU * i / n),
                  -.06 + (.22 if i % 2 else .185) * math.sin(TAU * i / n), z)
                 for z in (.72, 3.91) for i in range(n)]
        faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
        faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
        add(verts, faces, 'ivory')
        for z in (.75, 3.91, 4.04):
            box((x, -.035, z), (.21, .59, .09), 'gold')
        jewel((x, .27, 4.04), (.13, .08, .13), 'gem_rose')


def sector(inner, outer, a0, a1, low, high, role, n=6):
    profile = [(outer * math.sin(a0 + (a1 - a0) * i / n),
                outer * math.cos(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]
    profile += [(inner * math.sin(a0 + (a1 - a0) * i / n),
                 inner * math.cos(a0 + (a1 - a0) * i / n)) for i in reversed(range(n + 1))]
    prism(profile, low, high, role, 'Z')


# 2026-11-18 사용자: 로마 숫자 말고 아라비아 숫자로 — 굵은 글꼴(Arial Black) 글자를 실제 메시로
_DIGIT_FONT = None


def digit_geometry(text, height, depth):
    """Chunky Arabic numerals as (vertices, faces): centred, text in local X/Y (Y up), extruded about Z=0."""
    global _DIGIT_FONT
    if _DIGIT_FONT is None:
        try:
            _DIGIT_FONT = bpy.data.fonts.load('C:/Windows/Fonts/ariblk.ttf')
        except Exception:
            _DIGIT_FONT = False
    curve = bpy.data.curves.new('digits', 'FONT')
    curve.body = text
    if _DIGIT_FONT:
        curve.font = _DIGIT_FONT
    curve.size = height / .716                      # 숫자 높이 ≈ 글꼴 크기의 0.716
    curve.align_x = 'CENTER'
    curve.extrude = depth / 2
    curve.resolution_u = 2 if height < .2 else 4      # 카드처럼 작은 숫자는 거칠게 (삼각형 예산)
    obj = bpy.data.objects.new('digits', curve)
    bpy.context.collection.objects.link(obj)
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    data = evaluated.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(data)
    evaluated.to_mesh_clear()
    # 글꼴 메시는 앞뒷면 · 옆면이 따로 놀아 열린 모서리가 됨 → 붙이고 법선 정리
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bad = [e for e in bm.edges if not e.is_manifold]
    assert not bad, (text, 'open digit edges', len(bad))
    bm.verts.ensure_lookup_table()
    vertices = [tuple(v.co) for v in bm.verts]
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    bpy.data.objects.remove(obj)
    bpy.data.curves.remove(curve)
    ys = [v[1] for v in vertices]
    mid = (min(ys) + max(ys)) / 2                    # 숫자 자체의 세로 가운데로 맞춤
    return [(x, y - mid, z) for x, y, z in vertices], faces


def number_plaque(number, angle):
    center = Vector((18.72 * math.sin(angle), 18.72 * math.cos(angle), 0))
    rotation = -angle
    box(center + Vector((0, 0, .020)), (1.80, .93, .04),
        'rose_gold', (0, 0, rotation), bevel=.018)
    box(center + Vector((0, 0, .045)), (1.62, .78, .035),
        'ivory', (0, 0, rotation), bevel=.014)
    vertices, faces = digit_geometry(str(number), .58, .07)
    add(vertices, faces, 'gold', loc=center + Vector((0, 0, .095)), rot=(0, 0, rotation))


def altar():
    # Continuous open annulus. No disc or hidden underlay spans the arena.
    ring((0, 0, -.125), 20, 2.5, .25, 'marble', 128)
    ring((0, 0, -.045), 20, .15, .09, 'rose_gold', 128)
    ring((0, 0, -.035), 17.68, .18, .07, 'gold', 128)
    ring((0, 0, -.018), 19.81, .075, .036, 'gold', 128)
    for i in range(12):
        a = i * TAU / 12
        number_plaque(12 if i == 0 else i, a)
        sector(17.92, 19.69, a + .075, a + TAU / 12 - .075,
               -.012, .001, 'glaze_teal', 5)
        mid = a + math.pi / 12
        for r in (18.13, 19.48):
            star((r * math.sin(mid), r * math.cos(mid), .017), .14,
                 'tile_cream', 'Z', points=4, depth=.025)
    for i in range(8):
        a = math.radians(22.5) + i * math.pi / 4
        x, y = 18.80 * math.sin(a), 18.80 * math.cos(a)
        cylinder((x, y, .035), .26, .07, 'gold', 8)
        for j in range(3):
            dx, dy = ((0, 0), (.19, .035), (-.13, -.14))[j]
            height = (.62, .40, .33)[j]
            jewel((x + dx, y + dy, .06 + height / 2),
                  (.23, .23, height), 'crystal' if j != 1 else 'crystal_rose',
                  5, 'crystal_core')
    for a in (0, math.pi / 2, math.pi, 3 * math.pi / 2):
        box((10.75 * math.sin(a), 10.75 * math.cos(a), .015),
            (.15, 13.5, .03), 'gold', (0, 0, -a), bevel=0)
    cylinder((0, 0, .018), 2, .036, 'ivory', 64)
    ring((0, 0, .020), 2, .10, .04, 'gold', 64)
    ring((0, 0, .025), 1.71, .045, .025, 'rose_gold', 64)
    for i in range(8):
        a = TAU * i / 8
        r = 1.60 if i % 2 == 0 else 1.18
        tip = (r * math.sin(a), r * math.cos(a))
        left = (.27 * math.sin(a - math.pi / 2), .27 * math.cos(a - math.pi / 2))
        right = (.27 * math.sin(a + math.pi / 2), .27 * math.cos(a + math.pi / 2))
        prism([(0, 0), left, tip], .036, .04, 'gold', 'Z')
        prism([(0, 0), tip, right], .036, .04, 'tile_cream', 'Z')
    cylinder((0, 0, .030), .21, .020, 'gold', 16)


SPECS = [
    ('MerchantMask', (.62, .25, .42), merchant_mask, 'BACK_CENTER', 6000),
    ('SwordBox', (1.6, 1.6, 1.4), sword_box, 'BOTTOM_CENTER', 6000),
    ('MagicSword', (.18, 1.8, .08), magic_sword, 'HILT', 6000),
    ('MirrorPanel', (6, .25, 2.6), mirror_panel, 'BOTTOM_CENTER', 8000),
    ('Dove', (.9, .8, .35), dove, 'BODY_CENTER', 6000),
    ('NumberCard', (.9, .05, 1.3), number_card, 'BOTTOM_CENTER', 6000),
    ('MemoryShard', (1, 1, 2.2), memory_shard, 'BOTTOM_CENTER', 6000),
    ('Facade_Curtain', (8, 1.6, 5.5), curtain, 'BOTTOM_CENTER', 8000),
    ('Facade_MirrorHall', (6, 1.2, 4.5), mirror_hall, 'BOTTOM_CENTER', 8000),
    ('Altar', (40, 40, .25), altar, 'CENTER_FLOOR', 12000),
]


def bounds(obj):
    coords = [v.co for v in obj.data.vertices]
    return (Vector(tuple(min(v[i] for v in coords) for i in range(3))),
            Vector(tuple(max(v[i] for v in coords) for i in range(3))))


def triangle_count(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, dimensions, origin, budget):
    data = bpy.data.meshes.new(name + '_Mesh')
    data.from_pydata(VERTICES, [], FACES)
    data.update()
    used = list(dict.fromkeys(FACE_ROLES))
    for role in used:
        data.materials.append(MATERIALS[role])
    indices = {role: i for i, role in enumerate(used)}
    for poly, role in zip(data.polygons, FACE_ROLES):
        poly.material_index = indices[role]
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    count = triangle_count(obj)
    if count > budget:
        modifier = obj.modifiers.new('Retain material boundaries within budget', 'DECIMATE')
        modifier.ratio = (budget - 150) / count
        modifier.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    low, high = bounds(obj)
    span = high - low
    center = (low + high) / 2
    if origin != 'CENTER_FLOOR':
        for vertex in obj.data.vertices:
            point = vertex.co.copy()
            point.x = (point.x - center.x) * dimensions[0] / span.x
            if origin == 'HILT':
                point.y = (point.y - low.y) * dimensions[1] / span.y
                point.z *= dimensions[2] / span.z
            elif origin == 'BACK_CENTER':
                point.y = (point.y - low.y) * dimensions[1] / span.y
                point.z = (point.z - center.z) * dimensions[2] / span.z
            elif origin == 'BODY_CENTER':
                point.y *= dimensions[1] / span.y
                point.z *= dimensions[2] / span.z
            else:
                point.y = (point.y - center.y) * dimensions[1] / span.y
                point.z = (point.z - low.z) * dimensions[2] / span.z
            vertex.co = point
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), (name, 'open mesh component')
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    low, high = bounds(obj)
    count = triangle_count(obj)
    assert count <= budget, (name, count, budget)
    assert len(used) >= 5, (name, used)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    if origin == 'CENTER_FLOOR':
        assert abs(low.z + .25) < 1e-5
        assert abs(low.x + 20) < 1e-5 and abs(high.x - 20) < 1e-5
        assert abs(low.y + 20) < 1e-5 and abs(high.y - 20) < 1e-5
        # Every below-floor vertex belongs to the ring, never an interior disc.
        assert all(math.hypot(v.co.x, v.co.y) >= 17.499
                   for v in obj.data.vertices if v.co.z < -.001)
    else:
        assert all(abs(obj.dimensions[i] - dimensions[i]) < 1e-4 for i in range(3)), name
    if name == 'MemoryShard':
        assert all(v.co.z >= .3999 for p in obj.data.polygons
                   if obj.data.materials[p.material_index].name != 'stardust'
                   for v in (obj.data.vertices[i] for i in p.vertices))
        obj['floor_clearance_m'] = .4
    if origin == 'BOTTOM_CENTER':
        assert abs(low.z) < 1e-5, name
    if origin == 'BACK_CENTER':
        assert abs(low.y) < 1e-5, name
    if origin == 'HILT':
        assert abs(low.y) < 1e-5 and high.y > 1.7999, name
    obj['origin_convention'] = origin
    obj['front'] = '+Y'
    obj['triangle_count'] = count
    obj['specified_dimensions_m'] = list(dimensions)
    if name == 'Altar':
        obj['ring_inner_radius_m'], obj['body_thickness_m'] = 17.5, .25
        obj['floor_z_m'], obj['compass_diameter_m'] = 0, 4
    destination = OUTPUT / ('Kit_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
        use_custom_props=True,
    )
    assert destination.is_file() and destination.stat().st_size > 1024
    print(f'EXPORTED {name} {count} {tuple(round(d, 4) for d in obj.dimensions)}', flush=True)
    return obj


def verify_exports(objects):
    """Round-trip each actual FBX and check one mesh, role names and dimensions."""
    for original in objects:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(OUTPUT / f'Kit_{original.name}.fbx'))
        imported = set(bpy.data.objects) - before
        assert len(imported) == 1, (original.name, 'FBX object count', len(imported))
        copy = imported.pop()
        assert copy.type == 'MESH' and len(copy.data.vertices) > 0
        bpy.context.view_layer.update()
        world_corners = [copy.matrix_world @ Vector(v) for v in copy.bound_box]
        exported_size = [max(v[i] for v in world_corners) - min(v[i] for v in world_corners)
                         for i in range(3)]
        assert all(abs(exported_size[i] - original.dimensions[i]) < .002 for i in range(3)), (
            original.name, exported_size, tuple(original.dimensions))
        assert len(copy.data.materials) == len(original.data.materials)
        for mat in copy.data.materials:
            assert mat.name.rsplit('.', 1)[0] in PALETTE
            bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
            assert all(abs(c - .5) < .001 for c in bsdf.inputs['Base Color'].default_value[:3])
        mesh_data = copy.data
        bpy.data.objects.remove(copy, do_unlink=True)
        bpy.data.meshes.remove(mesh_data)
    print('VERIFIED 10 single-mesh FBX round trips', flush=True)


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.show_shadows = False
    shade.show_cavity, shade.cavity_type = True, 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.4, 1.1
    shade.show_specular_highlight = False
    shade.show_object_outline = True
    shade.object_outline_color = (.045, .04, .06)
    shade.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Preview background')
    scene.world.color = (.035, .045, .065)
    # Review-only magnification keeps the small handheld props legible in a row.
    scales = [3.3, 1.25, 1.45, .80, 2.45, 1.9, 1.25, .72, .76, .15]
    widths = [obj.dimensions.x * scale for obj, scale in zip(objects, scales)]
    widths[2] = 1.8 * scales[2]
    gap = .56
    total = sum(widths) + gap * (len(objects) - 1)
    cursor = -total / 2
    saved = [(o.location.copy(), o.rotation_euler.copy(), o.scale.copy()) for o in objects]
    helpers = []
    try:
        for role, mat in MATERIALS.items():
            mat.diffuse_color = (*PALETTE[role], 1)
        for i, (obj, scale, width) in enumerate(zip(objects, scales, widths)):
            obj.scale = (scale,) * 3
            obj.location = (cursor + width / 2, 0, 0)
            if i == 2:
                obj.rotation_euler.z = math.pi / 2
                obj.location.x += .9 * scale
                obj.location.z = .25
            if i == 0:
                obj.location.z = .9
            if i == 4:
                obj.location.z = .65
            cursor += width + gap
        camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type = 'ORTHO'
        camera.data.ortho_scale = total + 1.5
        target = Vector((0, 0, 1.15))
        elevation = math.radians(55)
        camera.location = target + Vector((0, 40 * math.cos(elevation), 40 * math.sin(elevation)))
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
        for obj in objects:
            text = bpy.data.curves.new('Review caption', 'FONT')
            text.body = obj.name
            text.align_x, text.size = 'CENTER', .28
            text.materials.append(MATERIALS['crystal_core'])
            label = bpy.data.objects.new('Review caption ' + obj.name, text)
            scene.collection.objects.link(label)
            label.location = (obj.location.x, 1.7, -.30)
            if obj.name == 'Altar':
                label.location.y = 3.5
            label.rotation_euler = camera.rotation_euler
            helpers.append(label)
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper, do_unlink=True)
        for obj, (loc, rot, scale) in zip(objects, saved):
            obj.location, obj.rotation_euler, obj.scale = loc, rot, scale
        for mat in MATERIALS.values():
            mat.diffuse_color = (.5, .5, .5, 1)


def main():
    global VERTICES, FACES, FACE_ROLES
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    objects = []
    for index, (name, dimensions, build, origin, budget) in enumerate(SPECS):
        VERTICES, FACES, FACE_ROLES = [], [], []
        random.seed(33129 + index * 7919)
        build()
        objects.append(finish(name, dimensions, origin, budget))
    verify_exports(objects)
    assert len(scene.objects) == 10 and all(o.type == 'MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects) == 10 and all(o.type == 'MESH' for o in scene.objects)
    assert all(tuple(m.diffuse_color) == (.5, .5, .5, 1) for m in MATERIALS.values())
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
