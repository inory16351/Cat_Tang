# 멈춘 역의 전투 소품과 엄폐물 10종을 회색 역할 재질로 만들어 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_props.py
"""Deterministic, metre-scale chapter-two props. Requires only stock Blender."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2'
ROLES = (
    'wood', 'wood_dark', 'wood_light', 'iron', 'iron_dark', 'rust', 'brass',
    'rail_steel', 'brick', 'brick_dark', 'stone', 'stone_dark', 'roof_tin',
    'soot', 'coal', 'window', 'window_lit', 'glass_lamp', 'canvas', 'leather',
    'leather_dark', 'cloth_red', 'paint_cream', 'paint_green', 'clock_face',
    'steam', 'signal_red', 'signal_amber', 'signal_green',
)
HARD = {'wood', 'wood_dark', 'wood_light', 'iron', 'iron_dark', 'rust',
        'brass', 'rail_steel', 'roof_tin', 'paint_cream'}
MATS = {}
PARTS = []
RNG = random.Random(0)


def setup_materials():
    for name in ROLES:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .85
        shader.inputs['Metallic'].default_value = 0
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[name] = mat


def mesh(label, verts, faces, mat, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[mat])
    if bevel:
        mod = obj.modifiers.new('Soft cast and worn edges', 'BEVEL')
        mod.width = .025
        mod.segments = 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(60)
    PARTS.append(obj)
    return obj


def move(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, mat, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return move(mesh(label, verts, faces, mat, bevel), loc, rot)


def cylinder(label, loc, radius, depth, mat, n=12, top=None,
             rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in [(radius, -depth / 2), (top, depth / 2)]
             for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return move(mesh(label, verts, faces, mat, bevel and mat in HARD), loc, rot)


def rod(label, a, b, radius, mat, n=6, bevel=False):
    a, b = Vector(a), Vector(b)
    obj = cylinder(label, (0, 0, 0), radius, (b - a).length, mat,
                   n=n, bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def beam(label, a, b, width, depth, mat, bevel=True):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth, (b - a).length), mat,
              bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def hoop(label, loc, radius, thickness, depth, mat, axis='Z', n=16,
         bevel=True):
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for z, r in [(-depth / 2, radius - thickness), (-depth / 2, radius),
                          (depth / 2, radius), (depth / 2, radius - thickness)]
             for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    rotation = {'X': (0, math.pi / 2, 0), 'Y': (math.pi / 2, 0, 0),
                'Z': (0, 0, 0)}[axis]
    return move(mesh(label, verts, faces, mat, bevel and mat in HARD), loc, rotation)


def tube(label, points, radius, mat, n=6, bevel=True):
    verts = []
    previous = None
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(i - 1, 0)])).normalized()
        reference = Vector((1, 0, 0)) if abs(tangent.y) > .9 else Vector((0, 1, 0))
        basis = reference.cross(tangent)
        if previous is not None:
            basis = previous - tangent * previous.dot(tangent)
        basis.normalize()
        other = tangent.cross(basis)
        previous = basis
        for j in range(n):
            angle = j * math.tau / n
            verts.append(tuple(Vector(point) + radius *
                               (basis * math.cos(angle) + other * math.sin(angle))))
    faces = [tuple(reversed(range(n)))]
    faces += [(i * n + j, i * n + (j + 1) % n,
               (i + 1) * n + (j + 1) % n, (i + 1) * n + j)
              for i in range(len(points) - 1) for j in range(n)]
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, verts, faces, mat, bevel and mat in HARD)


def lump(label, loc, size, mat, irregular=.08, subdivisions=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1)
    for vert in bm.verts:
        vert.co *= 1 + RNG.uniform(-irregular, irregular)
        vert.co = Vector(loc) + Vector(tuple(vert.co[i] * size[i] / 2
                                             for i in range(3)))
    bm.verts.ensure_lookup_table()
    obj = mesh(label, [tuple(v.co) for v in bm.verts],
               [tuple(v.index for v in f.verts) for f in bm.faces], mat)
    bm.free()
    return obj


def extrude_y(label, profile, y0, y1, mat, bevel=True):
    """Closed X/Z polygon extruded along Y, with intact coplanar end faces."""
    n = len(profile)
    verts = [(x, y, z) for y in (y0, y1) for x, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return mesh(label, verts, faces, mat, bevel)


def clock_face(x, y, z, radius, side=1, detailed=True):
    cylinder('Clock dial', (x, y, z), radius * .91, .018, 'clock_face',
             n=24, rot=(math.pi / 2, 0, 0), bevel=False)
    hoop('Brass clock rim', (x, y, z), radius, radius * .09, .04,
         'brass', 'Y', n=24, bevel=False)
    surface = y + side * .017
    if detailed:
        for i in range(12):
            angle = i * math.tau / 12
            inner = radius * (.68 if i % 3 == 0 else .77)
            beam('Clock hour marker',
                 (x - side * inner * math.sin(angle), surface, z + inner * math.cos(angle)),
                 (x - side * radius * .85 * math.sin(angle), surface,
                  z + radius * .85 * math.cos(angle)),
                 radius * .045, .012, 'iron_dark', bevel=False)
    # 11:59. The hour hand has advanced to half a degree before twelve.
    for angle, length, width in [(-.5, .51, .065), (-6, .74, .045)]:
        angle = math.radians(angle)
        beam('Frozen clock hand', (x, surface + side * .012, z),
             (x - side * radius * length * math.sin(angle), surface + side * .012,
              z + radius * length * math.cos(angle)),
             radius * width, .012, 'iron_dark', bevel=False)
    cylinder('Clock pin', (x, surface + side * .019, z), radius * .065, .018,
             'brass', n=8, rot=(math.pi / 2, 0, 0), bevel=False)


def steam_valve():
    box('Floor casting', (0, -.28, .055), (1, .24, .11), 'iron')
    cylinder('Pipe floor flange', (0, -.28, .14), .22, .12, 'rust', n=12)
    cylinder('Cast steam riser', (0, -.28, .63), .125, .94, 'iron', n=12)
    for z in (.28, .89):
        cylinder('Rusted pipe collar', (0, -.28, z), .155, .09, 'rust', n=12)
    rod('Valve spindle', (0, -.28, .99), (0, -.28, 1.22), .065, 'brass', bevel=True)
    hoop('Large valve wheel', (0, -.28, 1.11), .49, .075, .075, 'brass', 'Y', n=20)
    cylinder('Wheel hub', (0, -.28, 1.11), .105, .105, 'brass', n=10,
             rot=(math.pi / 2, 0, 0))
    for i in range(4):
        angle = i * math.tau / 4 + math.pi / 4
        rod('Valve wheel spoke', (0, -.28, 1.11),
            (.43 * math.cos(angle), -.28, 1.11 + .43 * math.sin(angle)),
            .036, 'brass', n=6)
    rod('Steam outlet neck', (0, -.28, .69), (0, -.035, .69), .09, 'iron', bevel=True)
    hoop('Open nozzle rim', (0, -.015, .69), .115, .032, .07, 'iron', 'Y', n=12)
    cylinder('Nozzle dark bore', (0, -.04, .69), .081, .01, 'iron_dark',
             n=12, rot=(math.pi / 2, 0, 0), bevel=False)
    rod('Gauge mounting elbow', (.08, -.28, .58), (.28, -.24, .61), .035, 'brass')
    cylinder('Pressure gauge housing', (.28, -.20, .67), .16, .10, 'brass',
             n=16, rot=(math.pi / 2, 0, 0))
    cylinder('Pressure gauge dial', (.28, -.14, .67), .132, .012, 'clock_face',
             n=16, rot=(math.pi / 2, 0, 0), bevel=False)
    beam('Gauge needle', (.28, -.126, .67), (.22, -.126, .75), .016, .012,
         'iron_dark', bevel=False)
    for dx, dz in [(-.09, .04), (0, .10), (.09, .04)]:
        box('Gauge scale mark', (.28 + dx, -.126, .67 + dz), (.016, .012, .025),
            'iron_dark', bevel=False)
    # The puff centre is exactly 0.30 m in front of the outlet face (Y=.02).
    for loc, size in [((0, .32, .69), (.21, .16, .21)),
                      ((-.075, .30, .75), (.16, .15, .17)),
                      ((.065, .33, .76), (.18, .14, .18))]:
        lump('Frozen steam sphere', loc, size, 'steam', .025, subdivisions=2)


def suitcase(loc, size, mat='leather', rot=(0, 0, 0), tag=False):
    start = len(PARTS)
    w, d, h = size
    box('Fat trunk body', (0, 0, 0), (w, d, h), mat,
        bevel=True)
    box('Rounded trunk lid', (0, 0, h * .35), (w * 1.015, d * 1.015, h * .32), mat)
    for x in (-w * .29, w * .29):
        # Leather straps wrap all four faces and remain readable from above.
        for y in (-d / 2 - .007, d / 2 + .007):
            box('Leather strap face', (x, y, 0), (.065, .018, h * 1.02),
                'leather_dark', bevel=False)
        for z in (-h / 2, h / 2 + .012):
            box('Leather strap wrap', (x, 0, z), (.065, d + .026, .018),
                'leather_dark', bevel=False)
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            for z in (-h / 2, h / 2):
                # Thin pressed corner protectors follow the softened case edges.
                box('Brass corner protector', (x * .97, y * .97, z * .94),
                    (.075, .065, .065), 'brass', bevel=False)
    box('Trunk lock', (0, d / 2 + .022, .025), (.11, .03, .10), 'brass', bevel=False)
    tube('Trunk carry handle', [(-.09, d / 2 + .025, -.025),
                               (-.09, d / 2 + .075, -.07),
                               (.09, d / 2 + .075, -.07),
                               (.09, d / 2 + .025, -.025)],
         .018, 'leather_dark', bevel=False)
    if tag:
        rod('Frozen tag string', (w / 2, .04, .10), (w / 2 + .11, .04, .15),
            .009, 'canvas', n=5)
        box('Stiff luggage tag', (w / 2 + .18, .04, .15), (.16, .025, .095),
            'paint_cream', rot=(0, -.16, 0))
        cylinder('Tag eyelet', (w / 2 + .12, .059, .15), .015, .012, 'brass',
                 n=6, rot=(math.pi / 2, 0, 0), bevel=False)
    matrix = Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(matrix)


def float_luggage():
    suitcase((-.07, -.025, .35), (.66, .40, .46),
             rot=(0, math.radians(15), 0), tag=True)


def mine_cart():
    for y in (-.29, .29):
        rod('Cart axle', (-.65, y, .20), (.65, y, .20), .045, 'iron_dark')
        for x in (-.61, .61):
            cylinder('Cart flanged wheel', (x, y, .20), .20, .12, 'iron_dark',
                     n=12, rot=(0, math.pi / 2, 0))
            cylinder('Wheel raised hub', (x * 1.085, y, .20), .075, .035, 'iron',
                     n=8, rot=(0, math.pi / 2, 0), bevel=False)
    box('Cart underframe', (0, 0, .32), (1.12, .78, .12), 'iron_dark')
    levels = [(.37, .49, .32), (.85, .68, .46),
              (.85, .62, .40), (.43, .44, .27)]
    verts = [(x, y, z) for z, w, d in levels
             for x, y in [(-w, -d), (w, -d), (w, d), (-w, d)]]
    faces = [(0, 3, 2, 1), (12, 13, 14, 15)]
    for lower, upper in [(0, 4), (4, 8), (8, 12)]:
        faces += [(lower + j, lower + (j + 1) % 4,
                   upper + (j + 1) % 4, upper + j) for j in range(4)]
    mesh('Open tapered ore tub', verts, faces, 'iron', True)
    for x in (-.65, .65):
        box('Rusted side rim', (x, 0, .85), (.08, .98, .07), 'rust')
        for y in (-.28, .28):
            beam('Rusted tub binding', (x * .75, y, .39), (x, y, .84),
                 .07, .035, 'rust', bevel=False)
            for z in (.48, .72):
                xx = .49 + (z - .37) / .48 * .19
                cylinder('Tub band rivet', (math.copysign(xx + .013, x), y, z),
                         .027, .02, 'iron_dark', n=6, rot=(0, math.pi / 2, 0),
                         bevel=False)
    for y in (-.45, .45):
        box('Tub end rim', (0, y, .85), (1.34, .07, .07), 'rust')
    for i in range(9):
        lump('Chunky coal load', ((i % 3 - 1) * .36, (i // 3 - 1) * .24,
                                 .82 + (.04 if i == 4 else 0)),
             (.44, .35, .31), 'coal', .15)


def rail_track():
    extrude_y('Tapered ballast bed', [(-1.30, 0), (1.30, 0),
                                     (1.20, .045), (-1.20, .045)],
              -2, 2, 'stone', bevel=False)
    for y in (-1.6, -.8, 0, .8, 1.6):
        box('Dark timber sleeper', (0, y, .08), (2.6, .25, .07), 'wood_dark')
    # Rail gauge is measured between the inside faces of the heads, at +/- .75.
    # Only longitudinal edges are bevelled, so both tile ends stay at Y=+/-2.
    profile = [(-.10, .11), (.10, .11), (.10, .13), (.025, .13),
               (.025, .17), (.05, .17), (.05, .20), (-.05, .20),
               (-.05, .17), (-.025, .17), (-.025, .13), (-.10, .13)]
    for x in (-.8, .8):
        obj = extrude_y('Continuous steel rail', [(x + px, z) for px, z in profile],
                        -2, 2, 'rail_steel', bevel=True)
        mod = obj.modifiers[0]
        mod.limit_method = 'WEIGHT'
        weights = obj.data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
        n = len(profile)
        for edge in obj.data.edges:
            if abs(edge.vertices[0] - edge.vertices[1]) == n:
                weights.data[edge.index].value = 1


def bench():
    box('Bench seat frame', (0, 0, .40), (2.08, .59, .09), 'iron')
    for y in (-.26, -.13, 0, .13, .26):
        box('Wooden seat slat', (0, y, .48), (2.2, .112, .075), 'wood')
    for z in (.67, .81, .95):
        box('Wooden back slat', (0, -.27 - (z - .6) * .13, z),
            (2.2, .075, .10), 'wood', rot=(.13, 0, 0))
    for x in (-.93, .93):
        tube('Curled cast bench leg', [(x, .30, .035), (x, .20, .08),
                                      (x, .13, .24), (x, .03, .39),
                                      (x, -.15, .38), (x, -.24, .22),
                                      (x, -.25, .07), (x, -.33, .035)], .045, 'iron')
        tube('Curled iron armrest', [(x, -.28, .42), (x, -.34, .66),
                                    (x, -.32, .91), (x, -.28, .99),
                                    (x, -.22, .96), (x, -.24, .76),
                                    (x, .10, .70), (x, .29, .72),
                                    (x, .32, .65), (x, .25, .62)], .037, 'iron')
        rod('Armrest front support', (x, .22, .43), (x, .22, .68), .03, 'iron')


def luggage_pile():
    suitcase((-.48, -.10, .255), (.94, 1.02, .47), 'leather')
    suitcase((.47, -.10, .255), (.88, .93, .47), 'canvas', rot=(0, 0, -.055))
    suitcase((-.20, -.04, .72), (1.26, .70, .40), 'leather_dark', rot=(0, 0, .07))
    suitcase((.47, .29, 1.115), (.86, .65, .36), 'leather',
             rot=(0, math.radians(-16), math.radians(-12)))
    cylinder('Canvas hatbox', (.55, -.38, .72), .28, .39, 'canvas', n=12, bevel=False)
    cylinder('Hatbox leather lid', (.55, -.38, .925), .30, .055,
             'leather_dark', n=12, bevel=False)
    hoop('Hatbox brass edge', (.55, -.38, .95), .30, .025, .025,
         'brass', n=12, bevel=False)


def freight_crate(loc, size, rot=0):
    start = len(PARTS)
    w, d, h = size
    box('Freight crate body', (0, 0, 0), size, 'wood_light')
    for z in (-h * .35, h * .35):
        box('Freight crate face batten', (0, d / 2 + .025, z),
            (w, .06, .075), 'wood_light')
    beam('Freight diagonal plank', (-w * .40, d / 2 + .06, -h * .34),
         (w * .40, d / 2 + .06, h * .34), .08, .045, 'wood_light')
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            # Two intersecting faces form a substantial angle-iron corner band.
            box('Iron corner band face', (x * .94, y, 0), (.10, .035, h),
                'iron', bevel=False)
            box('Iron corner band return', (x, y * .94, 0), (.035, .10, h),
                'iron', bevel=False)
    matrix = Matrix.Translation(Vector(loc)) @ Matrix.Rotation(rot, 4, 'Z')
    for obj in PARTS[start:]:
        obj.data.transform(matrix)


def crates():
    freight_crate((-.34, -.03, .43), (1.07, 1.02, .86))
    freight_crate((.28, -.19, .98), (.91, .72, .44), -.08)
    lump('Bulging canvas freight sack', (.58, .22, .43), (.65, .78, .82),
         'canvas', .08, subdivisions=2)
    cylinder('Gathered sack neck', (.60, .22, .84), .095, .13,
             'canvas', n=8, top=.13, bevel=False)
    hoop('Sack rope tie', (.60, .22, .82), .105, .025, .045,
         'leather_dark', n=10, bevel=False)


def ticket_booth():
    box('Rising booth plinth', (0, 0, .075), (2.24, 1.20, .15), 'wood_dark')
    box('Booth lower cabinet', (0, -.06, .58), (2.12, 1.02, 1.0), 'wood_dark')
    box('Cream front panel', (0, .465, .57), (1.80, .035, .72), 'paint_cream')
    for x in (-1.07, 1.07):
        box('Cream side panel', (x, -.06, .59), (.035, .79, .72), 'paint_cream')
        for y in (-.52, .45):
            box('Booth upright post', (x, y, 1.13), (.13, .13, 2.14), 'wood_dark')
    box('Booth enclosed back', (0, -.53, 1.54), (2.10, .09, 1.12), 'wood_dark')
    for x in (-.91, .91):
        box('Window side panelling', (x, .45, 1.60), (.29, .10, 1.08), 'paint_cream')
    spring, radius = 1.40, .70
    arch = [(-radius, 1.08), (radius, 1.08), (radius, spring)]
    arch += [(radius * math.cos(i * math.pi / 12),
              spring + radius * math.sin(i * math.pi / 12)) for i in range(1, 13)]
    extrude_y('Arched warm ticket window', arch, .425, .475, 'window_lit', bevel=False)
    points = [(-radius, .50, 1.08), (-radius, .50, spring)]
    points += [(-radius * math.cos(i * math.pi / 12), .50,
                spring + radius * math.sin(i * math.pi / 12)) for i in range(1, 13)]
    points.append((radius, .50, 1.08))
    tube('Arched wooden window frame', points, .055, 'wood_dark', n=6)
    for x in (-.50, -.25, 0, .25, .50):
        top = spring + math.sqrt(radius * radius - x * x) - .035
        rod('Vertical ticket window bar', (x, .535, 1.10), (x, .535, top),
            .021, 'iron', n=6)
    rod('Ticket grille crossbar', (-.67, .54, 1.53), (.67, .54, 1.53), .018, 'iron')
    box('Booth header', (0, .45, 2.14), (2.20, .15, .12), 'wood_dark')
    box('Projecting ticket counter', (0, .46, 1.065), (2.30, .48, .13), 'wood_dark')
    cylinder('Bell base', (.68, .52, 1.16), .115, .04, 'brass', n=12)
    cylinder('Brass service bell', (.68, .52, 1.225), .10, .11,
             'brass', n=12, top=.045)
    cylinder('Bell button', (.68, .52, 1.30), .024, .035, 'brass', n=8, bevel=False)
    extrude_y('Small pitched tin roof', [(-1.20, 2.16), (1.20, 2.16),
                                        (1.20, 2.22), (0, 2.40), (-1.20, 2.22)],
              -.70, .70, 'roof_tin')
    cylinder('Roof clock housing', (0, .27, 2.41), .19, .15, 'wood_dark',
             n=20, rot=(math.pi / 2, 0, 0))
    clock_face(0, .355, 2.41, .19, detailed=False)


def lamp_hood(z):
    outer, inner, n = .165, .14, 8
    verts = [(radius * math.cos(i * math.pi / n), y,
              z + radius * math.sin(i * math.pi / n))
             for y, radius in [(.055, outer), (.245, outer),
                               (.245, inner), (.055, inner)] for i in range(n + 1)]
    stride = n + 1
    faces = [(k * stride + i, k * stride + i + 1,
              ((k + 1) % 4) * stride + i + 1, ((k + 1) % 4) * stride + i)
             for k in range(4) for i in range(n)]
    faces += [(0, stride, stride * 2, stride * 3),
              (n, stride + n, stride * 2 + n, stride * 3 + n)]
    mesh('Overhanging signal lens hood', verts, faces, 'iron_dark', True)


def signal_lamp():
    box('Signal ground pedestal', (0, 0, .065), (.50, .50, .13), 'iron')
    cylinder('Signal post socket', (0, -.04, .18), .14, .18, 'iron', n=10)
    cylinder('Signal iron pole', (0, -.04, .83), .065, 1.38, 'iron', n=10)
    box('Three lens signal housing', (0, -.045, 1.855), (.43, .23, 1.09), 'iron')
    for z, mat in [(2.19, 'signal_red'), (1.85, 'signal_amber'), (1.51, 'signal_green')]:
        cylinder('Round signal lens', (0, .091, z), .125, .04, mat, n=16,
                 rot=(math.pi / 2, 0, 0), bevel=False)
        hoop('Lens bezel', (0, .088, z), .147, .024, .035,
             'iron_dark', 'Y', n=16, bevel=False)
        lamp_hood(z)


def station_clock():
    box('Clock cast base', (0, 0, .06), (.58, .36, .12), 'iron')
    cylinder('Clock pedestal foot', (0, 0, .18), .16, .24, 'iron', n=12, top=.11)
    cylinder('Clock fluted post', (0, 0, 1.20), .075, 1.98, 'iron', n=10)
    for z in (.38, 1.91, 2.14):
        cylinder('Cast post collar', (0, 0, z), .11, .07, 'iron', n=10)
    for side in (-1, 1):
        tube('Clock scroll bracket', [(0, 0, 1.99), (side * .18, 0, 2.08),
                                      (side * .23, 0, 2.22), (side * .14, 0, 2.27)],
             .025, 'iron')
    cylinder('Double sided clock case', (0, 0, 2.60), .40, .32, 'iron', n=24,
             rot=(math.pi / 2, 0, 0))
    clock_face(0, .18, 2.60, .40, side=1)
    clock_face(0, -.18, 2.60, .40, side=-1)


SPECS = [
    ('SteamValve', (1.0, .8, 1.6), steam_valve),
    ('FloatLuggage', (.9, .5, .7), float_luggage),
    ('MineCart', (1.4, 1.0, 1.0), mine_cart),
    ('RailTrack', (2.6, 4.0, .2), rail_track),
    ('Bench', (2.2, .7, 1.0), bench),
    ('LuggagePile', (2.0, 1.4, 1.4), luggage_pile),
    ('Crates', (1.8, 1.2, 1.2), crates),
    ('TicketBooth', (2.4, 1.4, 2.6), ticket_booth),
    ('SignalLamp', (.5, .5, 2.4), signal_lamp),
    ('StationClock', (.8, .4, 3.0), station_clock),
]


def triangle_count(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, dimensions):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
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

    # Bevelled assemblies are reduced only when they exceed the gameplay budget.
    # Protect role boundaries so the engine's material recolouring stays intact.
    count = triangle_count(obj)
    if count > 2450:
        modifier = obj.modifiers.new('Prop triangle budget', 'DECIMATE')
        modifier.ratio = 2400 / count
        modifier.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    assert triangle_count(obj) <= 2500, (name, triangle_count(obj))

    verts = obj.data.vertices
    low = Vector(tuple(min(v.co[i] for v in verts) for i in range(3)))
    high = Vector(tuple(max(v.co[i] for v in verts) for i in range(3)))
    span = high - low
    centre = (low + high) / 2
    for vert in verts:
        vert.co.x = (vert.co.x - centre.x) * dimensions[0] / span.x
        vert.co.y = (vert.co.y - centre.y) * dimensions[1] / span.y
        vert.co.z = (vert.co.z - low.z) * dimensions[2] / span.z
        if abs(vert.co.z) < 1e-6:
            vert.co.z = 0
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    # Every component is a closed solid, even the hollow tub and signal hoods.
    assert not any(edge.is_boundary or not edge.is_manifold for edge in bm.edges), name
    bm.to_mesh(obj.data)
    bm.free()
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    obj.data.update()
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    assert all(abs(obj.dimensions[i] - dimensions[i]) < .0001 for i in range(3)), name
    assert abs(min(v.co.z for v in verts)) < 1e-6, name
    assert all(math.isfinite(c) for v in verts for c in v.co), name
    assert all(mat.name in ROLES for mat in obj.data.materials), name
    obj['front'] = '+Y'
    obj['dimensions_m'] = list(dimensions)
    obj['chapter'] = 'The Stopped Station'
    if name == 'RailTrack':
        obj['gauge_m'] = 1.5
        obj['tile_length_m'] = 4.0
    if name == 'FloatLuggage':
        obj['frozen_tilt_degrees'] = 15.0
    if name == 'StationClock':
        obj['frozen_time'] = '11:59'
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangle_count(obj)} '
          f'{tuple(round(d, 4) for d in obj.dimensions)}', flush=True)
    return obj


def main():
    global PARTS, RNG
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    setup_materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    exported = []
    for index, (name, dimensions, build) in enumerate(SPECS):
        PARTS = []
        RNG = random.Random(23117 + index * 7919)
        build()
        exported.append(finish(name, dimensions))
    assert len(exported) == 10
    assert len(bpy.context.scene.objects) == 10
    assert all(obj.type == 'MESH' for obj in bpy.context.scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
