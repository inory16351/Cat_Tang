# 멈춘 역의 4단계 문·복도·포털과 보상문을 생성하고 FBX 및 미리보기를 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_gates.py
"""Stock Blender 5.x; deterministic metre-scale meshes, no external dependencies."""

import math
import random
import tempfile
from array import array
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch2_gates_preview.png'
COLORS = {
    'wood': (.46, .27, .13), 'wood_dark': (.20, .105, .065),
    'wood_light': (.65, .43, .23), 'iron': (.24, .29, .32),
    'iron_dark': (.085, .12, .14), 'rust': (.52, .19, .075),
    'brass': (.86, .59, .20), 'rail_steel': (.50, .63, .68),
    'brick': (.51, .235, .16), 'brick_dark': (.30, .13, .10),
    'stone': (.48, .49, .46), 'stone_dark': (.27, .30, .29),
    'roof_tin': (.35, .45, .44), 'soot': (.065, .06, .055),
    'coal': (.025, .028, .033), 'window': (.17, .39, .48),
    'window_lit': (.99, .71, .32), 'glass_lamp': (1.0, .86, .51),
    'canvas': (.65, .59, .40), 'leather': (.45, .22, .10),
    'leather_dark': (.22, .12, .08), 'cloth_red': (.57, .075, .065),
    'paint_cream': (.91, .85, .65), 'paint_green': (.14, .34, .25),
    'clock_face': (.97, .93, .79), 'steam': (.78, .88, .93),
    'signal_red': (.85, .045, .025), 'signal_amber': (.96, .44, .025),
    'signal_green': (.18, .69, .29),
}
MATS = {}
PARTS = []
RNG = random.Random(23741)
ASSETS = []


def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def material_setup():
    for role in COLORS:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .85
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], out.inputs['Surface'])
        MATS[role] = mat


def mesh(label, verts, faces, mat, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[mat])
    if bevel:
        mod = obj.modifiers.new('Worn edges', 'BEVEL')
        mod.width = .02
        mod.segments = 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(30)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(loc, size, mat, bevel=True, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
    return transform(mesh(mat, [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                               (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)],
                          [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
                           (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], mat, bevel), loc, rot)


def cylinder(loc, radius, depth, mat, n=12, rot=(0, 0, 0), bevel=True):
    verts = [(radius * math.cos(i * math.tau / n), radius * math.sin(i * math.tau / n), z)
             for z in (-depth / 2, depth / 2) for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    return transform(mesh(mat, verts, faces, mat, bevel), loc, rot)


def beam(a, b, width, depth, mat, bevel=True):
    a, b = Vector(a), Vector(b)
    obj = box((0, 0, 0), (width, depth, (b - a).length), mat, bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def rod(a, b, radius, mat, n=8, bevel=False):
    a, b = Vector(a), Vector(b)
    obj = cylinder((0, 0, 0), radius, (b - a).length, mat, n=n, bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def puff(loc, size, mat='steam'):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1)
    for v in bm.verts:
        factor = RNG.uniform(.96, 1.04)
        v.co = Vector(loc) + Vector(tuple(v.co[i] * size[i] * .5 * factor for i in range(3)))
    bm.verts.ensure_lookup_table()
    obj = mesh('Frozen ' + mat, [tuple(v.co) for v in bm.verts],
               [tuple(v.index for v in f.verts) for f in bm.faces], mat, False)
    bm.free()
    return obj


def extrude(profile, y0, y1, mat, seams=(), bevel=True):
    n = len(profile)
    verts = [(x, y, z) for y in (y0, y1) for x, z in profile]
    obj = mesh(mat, verts, [tuple(reversed(range(n))), tuple(range(n, n * 2))] +
               [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)], mat, bevel)
    if seams and bevel:
        # Shared fracture interfaces stay exact. Bevel only exposed edges, as in woodgate.
        mod = obj.modifiers[0]
        mod.limit_method = 'WEIGHT'
        weights = obj.data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
        seam_points = [Vector(p) for path in seams for p in path]
        for edge in obj.data.edges:
            touches_seam = any((Vector((obj.data.vertices[i].co.x,
                                       obj.data.vertices[i].co.z)) - p).length < 1e-5
                               for i in edge.vertices for p in seam_points)
            weights.data[edge.index].value = 0 if touches_seam else 1
    return obj


def tube(points, radius, mat):
    for a, b in zip(points, points[1:]):
        rod(a, b, radius, mat)


def arch(inner_half, spring, rise, thickness, depth, mat, segments=12):
    # Elliptical arch with a flat soffit at least 2.9 m over the entire clear opening.
    for i in range(segments):
        t0, t1 = i * math.pi / segments, (i + 1) * math.pi / segments
        profile = [(inner_half * math.cos(t0), spring + rise * math.sin(t0)),
                   (inner_half * math.cos(t1), spring + rise * math.sin(t1)),
                   ((inner_half + thickness) * math.cos(t1),
                    spring + (rise + thickness) * math.sin(t1)),
                   ((inner_half + thickness) * math.cos(t0),
                    spring + (rise + thickness) * math.sin(t0))]
        extrude(profile, -depth / 2, depth / 2, mat, bevel=False)


def clock(x, y, z, radius):
    cylinder((x, y - .045, z), radius, .13, 'brass', n=24,
             rot=(math.pi / 2, 0, 0))
    cylinder((x, y + .03, z), radius * .84, .024, 'clock_face', n=24,
             rot=(math.pi / 2, 0, 0), bevel=False)
    for i in range(12):
        a = i * math.tau / 12
        beam((x + radius * .65 * math.sin(a), y + .048, z + radius * .65 * math.cos(a)),
             (x + radius * .76 * math.sin(a), y + .048, z + radius * .76 * math.cos(a)),
             radius * .055, .012, 'iron_dark', False)
    # Frozen at 10:59, one second before departure: distinct hands read at thumbnail size.
    for a, length in [(-6, .66), (-30, .45)]:
        a = math.radians(a)
        beam((x, y + .06, z), (x + radius * length * math.sin(a), y + .06,
                              z + radius * length * math.cos(a)),
             radius * .065, .018, 'iron_dark', False)


def lamp(x, y, top):
    rod((x, y, top), (x, y, top - .25), .025, 'iron_dark')
    box((x, y, top - .44), (.24, .24, .34), 'glass_lamp')
    for dx in (-.13, .13):
        for dy in (-.13, .13):
            rod((x + dx, y + dy, top - .61), (x + dx, y + dy, top - .27), .02, 'iron_dark')
    for z in (top - .63, top - .25):
        box((x, y, z), (.34, .34, .065), 'iron_dark')


def join_since(start, name=None):
    objects = PARTS[start:]
    for obj in objects:
        activate(obj)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    activate(objects[0])
    for obj in objects:
        obj.select_set(True)
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    if name:
        obj.name = name
    PARTS[start:] = [obj]
    return obj


def piece(start, index):
    return join_since(start, f'Piece_{index:02d}')


def jagged(x0, x1, z, seed):
    rng = random.Random(seed)
    return [(x0, z), (x0 + (x1 - x0) * .26, z + rng.uniform(.035, .065)),
            (x0 + (x1 - x0) * .43, z - .055),
            (x0 + (x1 - x0) * .65, z + .045), (x1, z)]


def gate_crates():
    index = 0
    for row in range(2):
        for col in range(3):
            x0, x1 = -2.3 + col * 4.6 / 3, -2.3 + (col + 1) * 4.6 / 3
            z0 = row * 1.3
            cuts = [[(x0, z0), (x1, z0)],
                    jagged(x0, x1, z0 + .43, 81 + col + row * 9),
                    jagged(x0, x1, z0 + .87, 91 + col + row * 9),
                    [(x0, z0 + 1.3), (x1, z0 + 1.3)]]
            for j in range(3):
                start = len(PARTS)
                lower, upper = cuts[j:j + 2]
                seams = [cuts[k] for k in (j, j + 1) if k in (1, 2)]
                extrude(lower + list(reversed(upper)), -.15, .105,
                        'wood' if (col + row) % 2 else 'wood_light', seams)
                mid = z0 + (j + .5) * 1.3 / 3
                for x in (x0 + .065, x1 - .065):
                    box((x, .12, mid), (.13, .055, .43334), 'iron', False)
                if j in (0, 2):
                    z = z0 + (.075 if j == 0 else 1.225)
                    box(((x0 + x1) / 2, .12, z), (x1 - x0, .055, .15), 'wood_dark')
                # Diagonal batten fragments belong to their own wood chunk.
                beam((x0 + .19 + j * .38, .13, z0 + .12 + j * .40),
                     (x0 + .51 + j * .38, .13, z0 + .47 + j * .40), .105, .055, 'wood_dark')
                if row == 1 and col == 1 and j == 1:
                    box(((x0 + x1) / 2, .145, mid), (1.00, .035, .25), 'paint_cream')
                    # Bold stencilled cargo arrows, modelled without text textures.
                    for x in (-.26, .26):
                        beam((x, .168, mid - .075), (x, .168, mid + .06), .035, .012, 'wood_dark', False)
                        beam((x - .07, .168, mid), (x, .168, mid + .075), .035, .012, 'wood_dark', False)
                        beam((x + .07, .168, mid), (x, .168, mid + .075), .035, .012, 'wood_dark', False)
                piece(start, index)
                index += 1


def gate_barrier():
    for i in range(12):
        start = len(PARTS)
        w = 4.6 / 12
        x = -2.3 + (i + .5) * w
        box((x, 0, 1.22), (.09, .17, 2.44), 'iron')
        for z in (.16, 2.20):
            # Flush module ends, with bevels only on longitudinal exposed edges.
            rail = box((x, 0, z), (w, .22, .15), 'iron')
            rail.modifiers[0].limit_method = 'WEIGHT'
            weights = rail.data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
            for edge in rail.data.edges:
                a, b = (rail.data.vertices[v].co for v in edge.vertices)
                weights.data[edge.index].value = 1 if abs(a.x - b.x) > w * .9 else 0
        cylinder((x, 0, 2.445), .055, .11, 'brass', n=8)
        if i in (5, 6):
            box((x, 0, 1.30), (w, .25, .46), 'iron')
        if i == 8:
            cylinder((x, .12, 1.66), .23, .045, 'signal_red', n=8,
                     rot=(math.pi / 2, 0, 0))
            box((x, .15, 1.66), (.29, .025, .065), 'paint_cream', False)
        piece(start, i)
    for i, x in enumerate((-.97, .97)):
        start = len(PARTS)
        box((x, .10, 1.02), (1.75, .12, .10), 'iron')
        cylinder((x, .075, 1.02), .145, .15, 'brass', n=12,
                 rot=(math.pi / 2, 0, 0))
        piece(start, 12 + i)


def gate_coach():
    for row in range(4):
        for col in range(3):
            start = len(PARTS)
            x = -2 + (col + .5) * 4 / 3
            z = (row + .5) * 2.35 / 4
            box((x, 0, z), (4 / 3, .26, 2.35 / 4), 'paint_green', False)
            if row == 3 and col == 1:
                box((x, .135, z), (1.06, .04, .40), 'window')
                for dx in (-.55, .55):
                    box((x + dx, .15, z), (.06, .045, .46), 'brass', False)
                for dz in (-.23, .23):
                    box((x, .15, z + dz), (1.16, .045, .06), 'brass', False)
            else:
                box((x, .14, z), (1.10, .04, .36), 'paint_green')
            if row == 1 and col == 1:
                box((.44, .145, z + .04), (.10, .045, .26), 'brass')
                box((.33, .17, z + .08), (.30, .015, .065), 'brass')
            piece(start, row * 3 + col)
    for side in (-1, 1):
        for row in range(2):
            start = len(PARTS)
            box((side * 2.15, 0, (row + .5) * 1.175), (.30, .34, 1.175), 'wood_dark', False)
            box((side * 2.15, .13, (row + .5) * 1.175), (.20, .06, 1.02), 'wood_dark')
            piece(start, 12 + (0 if side == -1 else 2) + row)
    for col in range(2):
        start = len(PARTS)
        box(((-1 if col == 0 else 1) * 1.15, 0, 2.475), (2.3, .34, .25), 'wood_dark', False)
        piece(start, 16 + col)


def gate_shutter():
    for i in range(12):
        start = len(PARTS)
        h = 2.6 / 12
        z = (i + .5) * h
        box((0, 0, z), (4.6, .26, h), 'iron', False)
        box((0, .13, z + h * .28), (4.48, .045, .065), 'iron')
        for x in (-2.20, 2.20):
            box((x, 0, z), (.20, .34, h), 'iron_dark', False)
            cylinder((x, .172, z), .035, .006, 'brass', n=8,
                     rot=(math.pi / 2, 0, 0), bevel=False)
        if i % 3 != 0:
            x = RNG.uniform(-1.8, 1.7)
            extrude([(x - .22, z - .065), (x + .27, z - .04),
                     (x + .35, z + .05), (x - .12, z + .075)],
                    .131, .139, 'rust', bevel=False)
        piece(start, i)


def debris(mat):
    for i in range(3):
        length = .24 + i * .065
        # Narrow chips are completely inside even the opaque central barrier plate.
        obj = extrude([(-.025, 1.3 - length / 2), (.027, 1.3 - length * .16),
                       (.014, 1.3 + length / 2), (-.022, 1.3 + length * .22)],
                      -.022 + i * .008, -.014 + i * .008, mat, bevel=False)
        obj.name = f'Splinter_{i}'


def floor(mat):
    box((0, 0, -.09), (4.6, 10.4, .22), mat, False)


def crate(x, y, z, height=.86):
    box((x, y, z + height / 2), (.52, 1.60, height), 'wood_light')
    for yy in (y - .65, y + .65):
        box((x, yy, z + height / 2), (.56, .10, height), 'iron', False)
        box((x, yy, z + height + .012), (.52, .10, .024), 'iron', False)
    for xx in (x - .25, x + .25):
        for zz in (z + .12, z + height - .12):
            box((xx, y, zz), (.035, 1.60, .11), 'wood_dark', False)
        beam((xx, y - .58, z + .15), (xx, y + .58, z + height - .15),
             .08, .04, 'wood_dark', False)


def barrel(x, y):
    # Narrow freight barrels fit wholly in the 0.6 m dressing strip.
    n = 10
    rings = [(.04, .22), (.20, .27), (.75, .28), (1.06, .23)]
    verts = [(x + r * math.cos(i * math.tau / n), y + r * math.sin(i * math.tau / n), z)
             for z, r in rings for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(3 * n, 4 * n))]
    faces += [(k * n + i, k * n + (i + 1) % n,
               (k + 1) * n + (i + 1) % n, (k + 1) * n + i)
              for k in range(3) for i in range(n)]
    mesh('Barrel', verts, faces, 'wood', False)
    for z in (.23, .82):
        cylinder((x, y, z), .285, .09, 'iron', n=10, bevel=False)


def corridor_freight():
    floor('stone_dark')
    for side in (-1, 1):
        box((side * 2.60, 0, -.04), (.60, 10.4, .12), 'stone_dark', False)
    for i in range(82):
        puff((RNG.uniform(-2.16, 2.16), RNG.uniform(-5.05, 5.05), .035),
             (RNG.uniform(.13, .27), RNG.uniform(.17, .31), .065), 'stone')
    for i in range(14):
        box((0, -4.91 + i * .755, .045), (2.30, .22, .05), 'wood_dark')
    # Exactly ONE track: two running rails, shallow and continuous for tile joins.
    for x in (-.78, .78):
        box((x, 0, .065), (.10, 10.4, .03), 'rail_steel', False)
    for side in (-1, 1):
        for i, y in enumerate((-4.2, -2.1, 0, 2.1, 4.2)):
            if i % 2:
                barrel(side * 2.60, y)
            else:
                crate(side * 2.60, y, .02)
                crate(side * 2.60, y + .035, .88, .72)
    # A suitcase suspended above the freight line, attached only by frozen time.
    box((2.59, 1.8, 1.68), (.45, .70, .41), 'leather', rot=(.15, 0, .08))
    for y in (1.59, 2.01):
        box((2.59, y, 1.69), (.47, .06, .42), 'leather_dark', False, rot=(.15, 0, .08))
    tube([(2.50, 1.8, 1.91), (2.50, 1.8, 2.02),
          (2.68, 1.8, 2.02), (2.68, 1.8, 1.91)], .018, 'brass')


def corridor_platform():
    floor('stone')
    for x in (-1.9, 0, 1.9):
        for y in (-4, -2, 0, 2, 4):
            box((x, y, .025), (.012, 1.85, .009), 'stone_dark', False)
    for side in (-1, 1):
        box((side * 2.15, 0, .033), (.16, 10.4, .025), 'paint_cream', False)
        box((side * 2.60, 0, .13), (.60, 10.4, .26), 'stone_dark')
        # Low parapet remains open; the canopy is confined to X=2.3..2.9.
        for y in (-5.0, -2.5, 0, 2.5, 5.0):
            cylinder((side * 2.60, y, 1.29), .075, 2.38, 'iron', n=8)
            for z in (.30, 2.15):
                cylinder((side * 2.60, y, z), .12, .12, 'iron', n=8, bevel=False)
            beam((side * 2.60, y, 1.93), (side * 2.82, y, 2.30), .065, .065, 'iron')
        box((side * 2.76, 0, 2.36), (.28, 10.4, .16), 'roof_tin')
        box((side * 2.64, 0, 2.24), (.04, 10.4, .18), 'paint_cream', False)
        for y in (-3.75, -1.25, 1.25, 3.75):
            box((side * 2.60, y, .64), (.055, 2.4, .09), 'iron')
    lamp(-2.60, .70, 2.28)
    # The puff drifts out from under the canopy, suspended above the platform aisle.
    for loc, size in [((2.12, -1.25, 1.33), (.58, .66, .40)),
                      ((1.90, -.94, 1.48), (.72, .70, .48)),
                      ((1.63, -.65, 1.60), (.70, .60, .44))]:
        puff(loc, size)


def corridor_coach():
    floor('wood_dark')
    for i in range(26):
        box((0, -5 + i * .4, .04), (4.59, .38, .04),
            'wood' if i % 3 else 'wood_light', False)
    box((0, 0, .066), (1.42, 10.4, .024), 'cloth_red', False)
    for x in (-.66, .66):
        box((x, 0, .079), (.035, 10.4, .002), 'brass', False)
    for side in (-1, 1):
        x = side * 2.60
        box((x, 0, .53), (.60, 10.4, 1.02), 'paint_green')
        box((x, 0, 2.13), (.60, 10.4, .38), 'wood_dark')
        for y in (-4.5, -3, -1.5, 0, 1.5, 3, 4.5):
            box((x, y, 1.50), (.60, .20, .98), 'wood_dark')
        for i, y in enumerate((-3.75, -2.25, -.75, .75, 2.25, 3.75)):
            box((x, y, 1.50), (.49, 1.25, .84), 'window_lit' if i % 3 == 1 else 'window', False)
            for xx in (side * 2.33, side * 2.87):
                box((xx, y, 1.50), (.035, .045, .84), 'brass', False)
                box((xx, y, 1.06), (.055, 1.28, .085), 'wood_light', False)
        for end in (-1, 1):
            for i in range(4):
                y = end * (4.62 + i * .15)
                box((x, y, 1.22), (.57 if i % 2 else .44, .075, 2.40), 'iron_dark')


def valve(side, y, z):
    x = side * 2.40
    cylinder((x, y, z), .16, .055, 'brass', n=10, rot=(0, math.pi / 2, 0))
    cylinder((x - side * .035, y, z), .095, .059, 'iron_dark', n=10,
             rot=(0, math.pi / 2, 0), bevel=False)
    for a in (0, math.pi / 2):
        rod((x - side * .07, y - .14 * math.cos(a), z - .14 * math.sin(a)),
            (x - side * .07, y + .14 * math.cos(a), z + .14 * math.sin(a)), .025, 'brass')


def corridor_shed():
    floor('stone_dark')
    for row in range(20):
        for col in range(7):
            x = -1.93 + col * .64
            box((x, -4.94 + row * .52, .043), (.60, .48, .042),
                'stone' if (row + col) % 4 else 'stone_dark', False)
    box((1.83, 0, .068), (.30, 10.4, .022), 'soot', False)
    for side in (-1, 1):
        # Recess brick behind the pipe run; pipes stay in the dressing strip and are visible.
        box((side * 2.68, 0, 1.0), (.44, 10.4, 2.0), 'brick_dark', False)
        for row in range(5):
            for col in range(10):
                y = -4.68 + col * 1.04 + (.20 if row % 2 else 0)
                if y + .49 <= 5.2:
                    box((side * 2.68, y, .21 + row * .39), (.44, .98, .35),
                        'brick' if (row + col) % 4 else 'brick_dark', False)
        for y in (-4.8, -2.4, 0, 2.4, 4.8):
            box((side * 2.60, y, 1.12), (.60, .22, 2.24), 'brick_dark')
        for z in (.71, 1.65):
            rod((side * 2.43, -5.2, z), (side * 2.43, 5.2, z), .085, 'iron_dark', n=10)
            for y in (-3.8, 0, 3.8):
                cylinder((side * 2.43, y, z), .12, .10, 'iron', n=10,
                         rot=(math.pi / 2, 0, 0), bevel=False)
        valve(side, .8, 1.65)
        for y in (-2.8, 2.9):
            for j in range(3):
                box((side * 2.455, y + j * .17, 1.48 + j * .14),
                    (.012, .14, .80 - j * .12), 'soot', False)
    rod((-2.41, -1.5, 1.65), (-2.37, -1.5, 2.10), .07, 'iron_dark')
    for j in range(3):
        puff((-2.55, -1.5 + j * .16, 2.14 + j * .045), (.41, .34 + j * .12, .27))


def portal_gantry():
    for side in (-1, 1):
        x = side * 2.75
        box((x, 0, .12), (.90, 1.10, .24), 'iron_dark')
        for xx in (side * 2.38, side * 3.12):
            for y in (-.36, .36):
                box((xx, y, 1.77), (.13, .13, 3.30), 'iron')
        for y in (-.36, .36):
            for i in range(3):
                a, b = (side * 2.42, side * 3.08)
                if i % 2:
                    a, b = b, a
                beam((a, y, .25 + i * 1.03), (b, y, 1.28 + i * 1.03),
                     .10, .10, 'iron')
        box((x, 0, 3.38), (.98, 1.0, .20), 'iron_dark')
    box((0, 0, 3.57), (6.48, .80, .30), 'iron')
    for z in (3.41, 3.73):
        box((0, 0, z), (6.55, .92, .085), 'iron_dark')
    cylinder((0, .43, 3.65), .27, .20, 'brass', n=16, rot=(math.pi / 2, 0, 0))
    rod((0, .50, 3.47), (0, .50, 3.23), .045, 'iron_dark')
    tube([(0, .50, 3.25), (-.13, .50, 3.14), (-.10, .50, 3.02),
          (.06, .50, 2.98), (.18, .50, 3.10)], .06, 'iron')


def brick_pillars():
    for side in (-1, 1):
        x = side * 2.82
        box((x, 0, 1.47), (1.0, .95, 2.94), 'brick_dark', False)
        for i in range(7):
            box((x, .015 if i % 2 else -.015, .22 + i * .40),
                (.98, .95, .36), 'brick' if i % 3 else 'brick_dark', False)
        for z in (.14, 2.88):
            box((x, 0, z), (1.04, 1.12, .24), 'stone')


def portal_station():
    brick_pillars()
    arch(2.30, 2.94, .38, .33, .92, 'brick')
    box((0, 0, 3.48), (1.18, .75, .42), 'iron_dark')
    clock(0, .43, 3.61, .49)
    for side in (-1, 1):
        beam((side * .65, .47, 3.46), (side * 1.55, .47, 3.28), .07, .06, 'brass')


def portal_coach():
    for side in (-1, 1):
        x = side * 2.65
        box((x, 0, 1.49), (.70, .92, 2.98), 'paint_green')
        box((side * 2.34, .06, 1.44), (.08, .97, 2.88), 'wood_dark')
        cylinder((x, .43, .48), .23, .30, 'iron_dark', n=12,
                 rot=(math.pi / 2, 0, 0))
        for y in (-.40, -.20, 0):
            box((side * 2.70, y, 1.60), (.57, .075, 2.50), 'iron_dark')
    arch(2.30, 2.98, .65, .33, .92, 'paint_green')
    arch(2.31, 2.98, .65, .07, 1.02, 'wood_dark')
    box((0, .36, 3.66), (.54, .25, .26), 'iron_dark')
    cylinder((0, .51, 3.66), .14, .035, 'glass_lamp', n=12,
             rot=(math.pi / 2, 0, 0), bevel=False)


def portal_shed():
    brick_pillars()
    arch(2.30, 2.95, .51, .42, 1.06, 'brick')
    rod((-3.20, 0, 3.76), (3.20, 0, 3.76), .20, 'iron_dark', n=12)
    for x in (-2.65, -1.10, 1.10, 2.65):
        cylinder((x, 0, 3.76), .26, .14, 'iron', n=12,
                 rot=(0, math.pi / 2, 0), bevel=False)
    rod((.40, 0, 3.76), (.40, .22, 3.95), .09, 'brass')
    for loc, size in [((.50, .24, 4.00), (.45, .42, .29)),
                      ((.82, .20, 4.00), (.65, .51, .38)),
                      ((1.22, .18, 4.00), (.65, .55, .37))]:
        puff(loc, size)


def reward():
    start = len(PARTS)
    for side in (-1, 1):
        x = side * 1.31
        box((x, 0, .10), (.38, .50, .20), 'iron_dark')
        box((x, 0, 1.39), (.24, .32, 2.58), 'iron')
        for z in (.29, 2.52):
            box((x, 0, z), (.33, .40, .10), 'brass')
        cylinder((x, 0, 2.79), .11, .22, 'brass', n=8)
        extrude([(x - .09, 2.87), (x, 3.0), (x + .09, 2.87)], -.07, .07, 'brass', bevel=False)
    box((0, 0, 2.56), (2.62, .34, .30), 'iron')
    box((0, 0, .035), (2.62, .40, .07), 'iron_dark')
    clock(0, .19, 2.57, .24)
    frame = join_since(start, 'Frame')
    leaves = []
    for side, name in [(-1, 'Leaf_L'), (1, 'Leaf_R')]:
        start = len(PARTS)
        x = side * .565
        box((x, 0, .92), (1.12, .18, 1.56), 'iron_dark')
        for xx in (x - .52, x + .52):
            box((xx, 0, 1.20), (.075, .22, 2.12), 'iron')
        for z in (.19, .62, 1.73, 2.24):
            box((x, 0, z), (1.12, .22, .08), 'iron')
        # Brass surround and grille form a ticket window, rather than an opaque plaque.
        for xx in (x - .42, x + .42):
            box((xx, .13, 1.18), (.085, .05, .88), 'brass')
        for z in (.76, 1.60):
            box((x, .13, z), (.92, .05, .085), 'brass')
        box((x, .105, 1.18), (.75, .035, .73), 'window')
        for dx in (-.22, 0, .22):
            box((x + dx, .15, 1.18), (.035, .03, .76), 'brass', False)
        for dx in (-.30, 0, .30):
            rod((x + dx, 0, 1.78), (x + dx, 0, 2.22), .026, 'iron')
        box((side * .12, .15, .80), (.09, .07, .20), 'brass')
        leaf = join_since(start, name)
        # Place origin directly on the OUTER stile's hinge axis, retaining world geometry.
        hinge = Vector((side * 1.125, 0, 0))
        leaf.data.transform(Matrix.Translation(-hinge))
        leaf.location = hinge
        leaf['hinge_axis'] = 'Z'
        leaves.append(leaf)
    return [frame] + leaves


def tris(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(objects):
    bpy.context.view_layer.update()
    points = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    return (Vector(tuple(min(p[i] for p in points) for i in range(3))),
            Vector(tuple(max(p[i] for p in points) for i in range(3))))


def finish(name, objects, budget):
    count = sum(tris(o) for o in objects)
    # Preserve role boundaries when bevels push a large joined assembly over its budget.
    if count > budget:
        for obj in objects:
            activate(obj)
            mod = obj.modifiers.new('Gameplay triangle budget', 'DECIMATE')
            mod.ratio = budget * .94 / count
            mod.delimit = {'MATERIAL'}
            bpy.ops.object.modifier_apply(modifier=mod.name)
    for obj in objects:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        assert all(e.is_manifold for e in bm.edges), (name, obj.name, 'open mesh')
        bm.to_mesh(obj.data)
        bm.free()
        for p in obj.data.polygons:
            p.use_smooth = False
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        assert all(mat.name in COLORS for mat in obj.data.materials)
        assert obj.parent is None and obj.type == 'MESH'
        if name.startswith('Gate_'):
            activate(obj)
            bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
            local_min, local_max = (Vector(tuple(fn(v.co[i] for v in obj.data.vertices)
                                                for i in range(3))) for fn in (min, max))
            assert ((local_min + local_max) / 2).length < 1e-5
        obj.data.name = name + '_' + obj.name + '_Mesh'
        obj['chapter'] = 'The Stopped Station'
        obj['front'] = '+Y'
        obj['asset'] = name
    low, high = bounds(objects)
    dims = high - low
    count = sum(tris(o) for o in objects)
    assert count <= budget, (name, count, budget)
    if name.startswith('Gate_'):
        pieces = [o for o in objects if o.name.startswith('Piece_')]
        assert 10 <= len(pieces) <= 24
        assert [o.name for o in pieces] == [f'Piece_{i:02d}' for i in range(len(pieces))]
        assert [o.name for o in objects[-3:]] == [f'Splinter_{i}' for i in range(3)]
        assert abs(dims.x - 4.6) < 1e-4 and 2.4 <= dims.z <= 2.7
        assert dims.y <= .3501 and abs(low.y + high.y) < .04 and abs(low.z) < 1e-5
    elif name.startswith('Corridor_'):
        assert len(objects) == 1
        assert abs(low.y + 5.2) < 1e-4 and abs(high.y - 5.2) < 1e-4
        assert abs(low.x + 2.9) < 1e-4 and abs(high.x - 2.9) < 1e-4, (name, tuple(low), tuple(high))
        assert abs(low.z + .2) < 1e-5 and high.z <= 2.5
        assert objects[0].location.length < 1e-5
    elif name.startswith('Portal_'):
        assert len(objects) == 1 and 3.4 <= high.z <= 4.2
        assert low.y >= -.6001 and high.y <= .6001
        assert low.x >= -3.4001 and high.x <= 3.4001 and abs(low.z) < 1e-5
        # Conservative triangle/opening intersection check, including long straddling faces.
        obj = objects[0]
        obj.data.calc_loop_triangles()
        for tri in obj.data.loop_triangles:
            vs = [obj.data.vertices[i].co for i in tri.vertices]
            crosses = min(v.x for v in vs) < 2.3 - 1e-5 and max(v.x for v in vs) > -2.3 + 1e-5
            assert not (crosses and min(v.z for v in vs) < 2.9 - 1e-5), (name, 'blocked opening')
    else:
        assert [o.name for o in objects] == ['Frame', 'Leaf_L', 'Leaf_R']
        assert abs(dims.x - 3.0) < 1e-4 and abs(dims.z - 3.0) < 1e-4
        assert abs(objects[1].location.x + 1.125) < 1e-5
        assert abs(objects[2].location.x - 1.125) < 1e-5
    for mat in MATS.values():
        assert tuple(mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value) == (.5, .5, .5, 1)
        assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    path = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True,
                           axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
                           bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
                           mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    assert path.is_file() and path.stat().st_size > 1024
    print(f'EXPORTED {name} {len(objects)} {count} {tuple(round(d, 4) for d in dims)}', flush=True)
    ASSETS.append((name, objects[:]))
    # Move assets into a dedicated collection so Piece_00 can be reused verbatim in each FBX.
    # Blender names are globally unique: free names only AFTER exporting the current asset.
    for obj in objects:
        obj.name = name + '__' + obj.name
        obj.hide_render = True


def preview():
    """One contact sheet: common 55-degree view, stage rows, type columns, reward at end.

    Workbench renders grid cells into automatically removed temporary images. Separate
    orthographic framing keeps a 10.4 m corridor and a thin gate equally legible. Exported
    objects and shader values are never changed; only viewport colours change after export.
    """
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    shading = scene.display.shading
    shading.light = 'FLAT'
    shading.studiolight_rotate_z = .35
    shading.color_type = 'MATERIAL'
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor = 1.3
    shading.curvature_valley_factor = 1.1
    shading.show_object_outline = True
    shading.background_type = 'WORLD'
    scene.world.color = (.035, .047, .061)
    scene.view_settings.view_transform = 'Standard'
    for role, mat in MATS.items():
        mat.diffuse_color = (*COLORS[role], 1)
    cam_data = bpy.data.cameras.new('Preview camera')
    cam = bpy.data.objects.new('Preview camera', cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam_data.type = 'ORTHO'
    cam_data.lens = 50
    # Front +Y; a small X angle reveals thickness while preserving the primary silhouette.
    direction = Vector((.46, .53, 1.0)).normalized()
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    scene.render.resolution_x = 450
    scene.render.resolution_y = 250
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    sheet = array('f', [0.035, .047, .061, 1]) * (1800 * 1000)
    labels = {'Gate': 'GATE', 'Corridor': 'CORRIDOR', 'Portal': 'PORTAL'}
    subtitles = ['FREIGHT YARD', 'PLATFORM', 'STOPPED COACHES', 'ENGINE SHED']
    temporary = tempfile.TemporaryDirectory(prefix='ch2-gates-preview-')
    for index, (name, objects) in enumerate(ASSETS):
        for obj in objects:
            obj.hide_render = False
        low, high = bounds(objects)
        target = (low + high) / 2
        cam.location = target + direction * 35
        # Fit all eight world corners in camera coordinates, leaving space for the label.
        inverse = cam.rotation_euler.to_matrix().transposed()
        projected = [inverse @ (Vector((x, y, z)) - target)
                     for x in (low.x, high.x) for y in (low.y, high.y) for z in (low.z, high.z)]
        width = max(p.x for p in projected) - min(p.x for p in projected)
        height = max(p.y for p in projected) - min(p.y for p in projected)
        cam_data.ortho_scale = max(width * 1.14, height * 1.8 * 1.32)
        row, col = (index // 3, index % 3) if index < 12 else (3, 3)
        title = (f'S{row + 1}  /  {labels[name.split("_")[0]]}  /  {subtitles[row]}'
                 if index < 12 else 'CH2  /  REWARD DOOR')
        curve = bpy.data.curves.new('Preview label', 'FONT')
        curve.body = title
        curve.align_x = 'CENTER'
        curve.size = cam_data.ortho_scale * .034
        text_obj = bpy.data.objects.new('Preview label', curve)
        scene.collection.objects.link(text_obj)
        text_obj.rotation_euler = cam.rotation_euler
        text_obj.location = target + cam.rotation_euler.to_matrix() @ Vector((0, -cam_data.ortho_scale / 1.8 * .45, 1))
        curve.materials.append(MATS['paint_cream'])
        # Blender 5.x keeps Render Result pixels on the render side; load a temporary PNG
        # for the contact sheet, using Raw so its display-encoded pixels are not transformed twice.
        tile = str(Path(temporary.name) / 'cell.png')
        scene.render.filepath = tile
        bpy.ops.render.render(write_still=True)
        result = bpy.data.images.load(tile, check_existing=False)
        result.colorspace_settings.name = 'Non-Color'
        pixels = array('f', [0]) * (450 * 250 * 4)
        result.pixels.foreach_get(pixels)
        if index == 0:
            sheet = array('f', pixels[:4]) * (1800 * 1000)
        for line in range(250):
            dst = (((3 - row) * 250 + line) * 1800 + col * 450) * 4
            src = line * 450 * 4
            sheet[dst:dst + 1800] = pixels[src:src + 1800]
        bpy.data.images.remove(result)
        bpy.data.objects.remove(text_obj, do_unlink=True)
        bpy.data.curves.remove(curve)
        for obj in objects:
            obj.hide_render = True
    # Use the otherwise unused top-right cells as a quiet title/legend column.
    shading.show_cavity = False
    shading.show_shadows = False
    shading.show_object_outline = False
    cards = [
        ('THE STOPPED\nSTATION\n\nCHAPTER 02', .48),
        ('FROZEN AT DEPARTURE\n\nFreight / Platform\nCoaches / Engine shed\n\nSteam. Brass. Stopped clocks.', .28),
        ('13 MODULAR ASSETS\n\n4 burst-apart gates\n4 open corridors\n4 doorway frames\n1 hinged reward gate', .30),
    ]
    cam_data.ortho_scale = 10
    cam.location = direction * 35
    for row, (body, size) in enumerate(cards):
        curve = bpy.data.curves.new('Contact sheet title', 'FONT')
        curve.body = body
        curve.size = size
        curve.space_line = 1.25
        obj = bpy.data.objects.new('Contact sheet title', curve)
        scene.collection.objects.link(obj)
        obj.rotation_euler = cam.rotation_euler
        obj.location = cam.rotation_euler.to_matrix() @ Vector((-4.1, 1.7, 0))
        curve.materials.append(MATS['paint_cream'])
        bpy.ops.render.render(write_still=True)
        result = bpy.data.images.load(tile, check_existing=False)
        result.colorspace_settings.name = 'Non-Color'
        pixels = array('f', [0]) * (450 * 250 * 4)
        result.pixels.foreach_get(pixels)
        for line in range(250):
            dst = (((3 - row) * 250 + line) * 1800 + 1350) * 4
            sheet[dst:dst + 1800] = pixels[line * 1800:(line + 1) * 1800]
        bpy.data.images.remove(result)
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.curves.remove(curve)
    img = bpy.data.images.new('Chapter 2 gate contact sheet', width=1800, height=1000, alpha=True)
    img.colorspace_settings.name = 'Non-Color'
    img.pixels.foreach_set(sheet)
    img.filepath_raw = str(PREVIEW)
    img.file_format = 'PNG'
    img.save()
    temporary.cleanup()
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    print(f'PREVIEW {PREVIEW}', flush=True)


def main():
    global PARTS, RNG
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    material_setup()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    gates = [gate_crates, gate_barrier, gate_coach, gate_shutter]
    corridors = [corridor_freight, corridor_platform, corridor_coach, corridor_shed]
    portals = [portal_gantry, portal_station, portal_coach, portal_shed]
    for stage in range(4):
        RNG = random.Random(23741 + stage * 7919)
        PARTS = []
        gates[stage]()
        debris(['wood', 'iron', 'paint_green', 'iron'][stage])
        finish(f'Gate_S{stage + 1}', PARTS[:], 6000)
        for kind, build, budget in [('Corridor', corridors[stage], 7000),
                                    ('Portal', portals[stage], 4000)]:
            PARTS = []
            build()
            obj = join_since(0, f'{kind}_S{stage + 1}')
            finish(f'{kind}_S{stage + 1}', [obj], budget)
    PARTS = []
    finish('RewardDoor', reward(), 5000)
    assert len(ASSETS) == 13
    preview()
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
