# 멈춘 역의 대형 장애물 6종, 보스 회전대와 승강장 경계를 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_landmarks.py
"""Standalone Blender 5.x, deterministic metre-scale chapter-two FBX kit.

The room-facing side is +Y. Locomotive travel is +X, exposing its running
gear to the room. All role materials deliberately have identical grey shaders.
Only meshes are exported; disconnected details remain in one joined object.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2'
ROLES = set('wood wood_dark wood_light iron iron_dark rust brass rail_steel '
            'brick brick_dark stone stone_dark roof_tin soot coal window '
            'window_lit glass_lamp canvas leather leather_dark cloth_red '
            'paint_cream paint_green clock_face steam signal_red signal_amber '
            'signal_green'.split())
MATERIALS = {}
PARTS = []
RNG = random.Random(20261117)
SERIAL = 0
METAL_WOOD = set('wood wood_dark wood_light iron iron_dark rust brass '
                 'rail_steel roof_tin paint_green paint_cream cloth_red'.split())


def setup_materials():
    for name in sorted(ROLES):
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .85
        shader.inputs['Metallic'].default_value = 0
        MATERIALS[name] = mat


def mesh(label, verts, faces, role, bevel=False, top_only=False):
    global SERIAL
    assert role in ROLES, role
    SERIAL += 1
    data = bpy.data.meshes.new(f'{label}_{SERIAL}')
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(data.name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATERIALS[role])
    if bevel and role in METAL_WOOD:
        mod = obj.modifiers.new('Chunky softened edges 25mm', 'BEVEL')
        mod.width = .025
        mod.segments = 2
        mod.affect = 'EDGES'
        mod.use_clamp_overlap = True
        if top_only:
            # Soften the exposed upper perimeter; the buried bottom stays cheap.
            weights = data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
            top = max(v.co.z for v in data.vertices)
            for edge in data.edges:
                if all(abs(data.vertices[i].co.z - top) < 1e-5 for i in edge.vertices):
                    weights.data[edge.index].value = 1
            mod.limit_method = 'WEIGHT'
        else:
            mod.limit_method = 'ANGLE'
            mod.angle_limit = math.radians(40)
    PARTS.append(obj)
    return obj


def move(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role='wood', rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return move(mesh(label, verts, faces, role, bevel, True), loc, rot)


def cylinder(label, loc, radius, depth, role='iron', n=12, top=None,
             rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in [(radius, -depth / 2), (top, depth / 2)] for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return move(mesh(label, verts, faces, role, bevel, True), loc, rot)


def rod(label, a, b, radius, role='iron', n=8, bevel=False):
    a, b = Vector(a), Vector(b)
    obj = cylinder(label, (0, 0, 0), radius, (b - a).length, role,
                   n=n, bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def beam(label, a, b, width, role='iron', depth=None, bevel=True):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth or width, (b - a).length), role,
              bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def ring(label, loc, radius, thickness, height, role='iron', n=24,
         rot=(0, 0, 0), bevel=False):
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in [(radius - thickness, -height / 2), (radius, -height / 2),
                          (radius, height / 2), (radius - thickness, height / 2)]
             for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    return move(mesh(label, verts, faces, role, bevel, True), loc, rot)


def tube(label, points, radii, role='iron', n=8, bevel=False):
    verts = []
    previous = None
    for k, p in enumerate(points):
        tangent = (Vector(points[min(k + 1, len(points) - 1)]) -
                   Vector(points[max(0, k - 1)])).normalized()
        if previous is None:
            reference = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
            u = reference.cross(tangent).normalized()
        else:
            u = (previous - tangent * previous.dot(tangent)).normalized()
        v = tangent.cross(u).normalized()
        previous = u
        for j in range(n):
            a = j * math.tau / n
            verts.append(tuple(Vector(p) + radii[k] * (u * math.cos(a) + v * math.sin(a))))
    faces = [tuple(reversed(range(n)))]
    for k in range(len(points) - 1):
        for j in range(n):
            a, b = k * n + j, k * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, verts, faces, role, bevel)


def lump(label, loc, size, role, detail=1, irregular=.08, ground=False):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= 1 + RNG.uniform(-irregular, irregular)
        for axis in range(3):
            v.co[axis] = v.co[axis] * size[axis] / 2 + loc[axis]
        if ground:
            v.co.z = max(0, v.co.z)
    bm.verts.ensure_lookup_table()
    result = mesh(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return result


def clock(label, loc, radius, normal=(0, 1, 0)):
    start = len(PARTS)
    cylinder(label + ' bezel', (0, 0, 0), radius, .13, 'brass', n=16)
    cylinder(label + ' lens', (0, 0, .076), radius * .79, .025,
             'clock_face', n=16, bevel=False)
    for j in range(4):
        a = j * math.pi / 2
        box('Clock hour marker', (radius * .61 * math.cos(a),
                                 radius * .61 * math.sin(a), .097),
            (.055, .10, .022), 'iron_dark', (0, 0, a), bevel=False)
    beam('Frozen clock minute hand', (0, 0, .115), (0, radius * .51, .115),
         .045, 'iron_dark', bevel=False)
    beam('Frozen clock hour hand', (0, 0, .12), (radius * .32, -.08, .12),
         .05, 'iron_dark', bevel=False)
    rotation = Vector(normal).to_track_quat('Z', 'Y').to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Translation(Vector(loc)) @ rotation)


def coach():
    # Axle bearings and low bogies leave the specified floor exactly 0.5m high.
    for x in [-3.35, 3.35]:
        box('Coach bogie', (x, 0, .24), (1.75, 2.45, .25), 'iron_dark')
        for dx in [-.55, .55]:
            rod('Coach axle', (x + dx, -1.25, .24), (x + dx, 1.25, .24), .075, 'iron_dark')
            for y in [-1.26, 1.26]:
                cylinder('Coach wheel', (x + dx, y, .24), .24, .16,
                         'iron_dark', n=10, rot=(math.pi / 2, 0, 0), bevel=False)
    box('Wood carriage floor top 0.5m', (0, -.13, .425), (9.6, 2.82, .15), 'wood')
    for x in [-4.9, 4.9]:
        box('Coupler block', (x, -.1, .32), (.2, .36, .2), 'iron_dark')
    box('Closed green back wall', (0, -1.49, 1.67), (9.6, .16, 2.34), 'paint_green')
    box('Back wood lower panel', (0, -1.385, .96), (9.55, .07, .85), 'wood')
    box('Back brass waist rail', (0, -1.33, 1.43), (9.5, .06, .065), 'brass', bevel=False)
    for x in [-3.72, -1.86, 0, 1.86, 3.72]:
        box('Back window frame', (x, -1.475, 2.12), (1.48, .20, 1.03), 'wood_dark')
        box('Back lit window', (x, -1.475, 2.12), (1.25, .23, .8), 'window_lit', bevel=False)
        box('Window center mullion', (x, -1.345, 2.12), (.06, .05, .83), 'wood', bevel=False)
    for x in [-4.72, 4.72]:
        box('Closed wooden carriage end', (x, -.2, 1.61), (.16, 2.58, 2.22), 'paint_green')
        box('End wooden panel', (x, -.2, .98), (.21, 2.5, .83), 'wood')
    # Three transverse bench rows: paired leather cushions, broad tall backs.
    for x in [-2.75, 0, 2.75]:
        box('Bench wood pedestal', (x, -.12, .72), (.95, 1.96, .44), 'wood_dark')
        for y in [-.65, .41]:
            box('Leather bench cushion', (x, y, 1.01), (1.12, .93, .20), 'leather')
        box('Leather transverse row back', (x - .48, -.12, 1.34), (.19, 2.02, .66),
            'leather', (0, -.08, 0))
        for y in [-1.14, .93]:
            rod('Brass seat handrail upright', (x - .39, y, .98), (x - .39, y, 1.51), .045, 'brass')
            rod('Brass seat handrail', (x - .39, y, 1.51), (x + .44, y, 1.51), .045, 'brass')
    # A volumetric half-barrel shell on the BACK only. No front canopy hides seats.
    sections = []
    for j in range(9):
        a = j * math.pi / 16
        sections.append((-1.6 * math.cos(a), 2.78 + .82 * math.sin(a)))
    cross = sections + [(y, z - .085) for y, z in reversed(sections)]
    n = len(cross)
    verts = [(x, y, z) for x in [-4.88, 4.88] for y, z in cross]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    mesh('Cutaway curved tin roof', verts, faces, 'roof_tin', True)
    box('Cut roof front lip', (0, -.02, 3.54), (9.8, .07, .10), 'roof_tin')
    for x in [-3.5, 3.5]:
        box('Iron boarding step', (x, 1.24, .12), (1.12, .72, .24), 'iron')
        rod('Boarding brass grab', (x + .66, .95, .56), (x + .66, .95, 1.67), .055, 'brass')


def locomotive():
    box('Locomotive chassis', (-.15, 0, .91), (8.35, 2.7, .3), 'iron_dark')
    # Side-facing running gear is the primary high-camera read.
    for y in [-1.36, 1.36]:
        for x in [-2.72, -.7, 1.32]:
            ring('Driving wheel tire', (x, y, .74), .74, .13, .18,
                 'iron', n=14, rot=(math.pi / 2, 0, 0))
            cylinder('Driving wheel hub', (x, y, .74), .18, .24, 'iron',
                     n=10, rot=(math.pi / 2, 0, 0), bevel=False)
            for j in range(6):
                a = j * math.tau / 6
                rod('Broad wheel spoke', (x, y, .74),
                    (x + .61 * math.cos(a), y, .74 + .61 * math.sin(a)), .065, 'iron', n=6)
        beam('Connecting rod', (-2.72, y * 1.12, .89), (1.32, y * 1.12, .89),
             .15, 'iron', depth=.1)
        for x in [-2.72, -.7, 1.32]:
            cylinder('Rod crank pin', (x, y * 1.15, .89), .11, .10,
                     'brass', n=8, rot=(math.pi / 2, 0, 0), bevel=False)
        cylinder('Front pilot wheel', (3.18, y, .4), .4, .17,
                 'iron', n=12, rot=(math.pi / 2, 0, 0), bevel=False)
    cylinder('Black boiler', (-.05, 0, 2.12), 1.05, 5.8, 'iron_dark',
             n=16, rot=(0, math.pi / 2, 0))
    for x in [-2.5, -.4, 2.52]:
        ring('Boiler iron band', (x, 0, 2.12), 1.085, .055, .12,
             'iron', n=16, rot=(0, math.pi / 2, 0))
    cylinder('Smokebox front', (2.93, 0, 2.12), .94, .16, 'iron_dark',
             n=16, rot=(0, math.pi / 2, 0))
    box('Rear firebox', (-3.63, 0, 1.88), (1.42, 2.38, 1.78), 'iron_dark')
    box('Firebox shoulder cap', (-3.63, 0, 2.87), (1.68, 2.65, .22), 'iron_dark')
    cylinder('Steam dome', (-1.05, 0, 3.14), .39, .53, 'iron_dark', n=12, top=.3)
    cylinder('Flared soot chimney', (1.52, 0, 3.23), .25, .77,
             'soot', top=.42, n=12, bevel=False)
    ring('Chimney lip', (1.52, 0, 3.62), .45, .10, .15, 'soot', n=12)
    for loc, size in [((1.52, 0, 3.88), (.65, .6, .55)),
                      ((1.11, -.05, 4.03), (.72, .65, .5)),
                      ((.73, -.12, 4.08), (.55, .52, .4))]:
        lump('Frozen detached steam puff', loc, size, 'steam', detail=2, irregular=.04)
    # The large lens turns slightly toward +Y so its circle reads from the room.
    clock('Round locomotive headlight', (3.05, .39, 2.77), .48, (1, .65, .12))
    for y in [-1.08, 1.08]:
        rod('Boiler side handrail', (-2.62, y, 2.62), (2.4, y, 2.62), .045, 'brass')
    # Solid wedge, with chunky slats proud of the sloping red pilot face.
    verts = [(3.25, -1.58, .13), (4.5, -1.58, .13), (3.25, -1.58, .94),
             (3.25, 1.58, .13), (4.5, 1.58, .13), (3.25, 1.58, .94)]
    mesh('Solid red cowcatcher wedge', verts,
         [(0, 2, 1), (3, 4, 5), (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5)],
         'cloth_red', True)
    for y in [-1.38, -.92, -.46, 0, .46, .92, 1.38]:
        beam('Red cowcatcher slat', (3.28, y, .98), (4.45, y, .22),
             .13, 'cloth_red', bevel=False)


def water_crane():
    cx, cy = -.30, -.27
    for sx in [-1, 1]:
        for sy in [-1, 1]:
            bottom = (cx + sx * 1.03, cy + sy * 1.04, .09)
            top = (cx + sx * .75, cy + sy * .75, 4.28)
            box('Tower iron foot', (bottom[0], bottom[1], .09), (.34, .34, .18), 'iron')
            beam('Tower splayed iron leg', bottom, top, .19, 'iron')
    for sy in [-1, 1]:
        for sign in [-1, 1]:
            beam('Tower X bracing', (cx - sign * .99, cy + sy * .99, .42),
                 (cx + sign * .76, cy + sy * .76, 4.03), .10, 'iron', bevel=False)
    for sx in [-1, 1]:
        beam('Tower side diagonal', (cx + sx * .99, cy - .99, .42),
             (cx + sx * .76, cy + .76, 4.03), .10, 'iron', bevel=False)
    box('Tank support deck', (cx, cy, 4.20), (2.38, 2.38, .19), 'iron')
    cylinder('Solid wooden water tank', (cx, cy, 5.01), 1.08, 1.48, 'wood', n=16)
    for j in range(16):
        a = j * math.tau / 16
        rod('Tank stave joint', (cx + 1.084 * math.cos(a), cy + 1.084 * math.sin(a), 4.32),
            (cx + 1.084 * math.cos(a), cy + 1.084 * math.sin(a), 5.68),
            .018, 'wood_dark', n=4)
    for z in [4.39, 5.0, 5.62]:
        ring('Tank iron hoop', (cx, cy, z), 1.14, .065, .12, 'iron', n=16)
    cylinder('Tank shallow wooden lid', (cx, cy, 5.87), 1.18, .26,
             'wood_dark', top=.12, n=16)
    for a, length in [(.48, .52), (1.12, .79), (1.92, .39), (2.5, .60)]:
        x, y = cx + 1.09 * math.cos(a), cy + 1.09 * math.sin(a)
        box('Broad tank rust streak', (x, y, 5.53 - length / 2),
            (.04, .14, length), 'rust', (0, 0, a), bevel=False)
    cylinder('Spout swivel', (.62, .48, 4.65), .24, .40, 'iron', n=10)
    tube('Swinging water spout', [(.59, .48, 4.72), (1.13, 1.12, 4.72),
                                (1.28, 1.29, 4.52), (1.28, 1.29, 3.88)],
         [.16, .16, .16, .19], 'iron', n=8, bevel=True)
    rod('Spout brace', (.59, .48, 5.05), (1.13, 1.12, 4.69), .055, 'iron')
    # Isolated droplet, not a continuous stream: time stopped during the pour.
    lump('Frozen water droplet', (1.28, 1.29, 3.46), (.30, .30, .55),
         'steam', detail=2, irregular=.025)
    clock('Tower valve clock', (.38, .88, 4.28), .20)


def coal_pile():
    lump('Solid coal heap core', (0, -.05, .50), (4.35, 3.25, 1.30),
         'coal', detail=2, irregular=.12, ground=True)
    for j in range(22):
        a = j * 2.399963
        r = math.sqrt((j + .5) / 22)
        x, y = 1.83 * r * math.cos(a), 1.32 * r * math.sin(a)
        z = .37 + .75 * (1 - r * r)
        s = RNG.uniform(.58, .9)
        lump('Angular exposed coal chunk', (x, y, z), (s, s * .85, s * .8),
             'coal', irregular=.17, ground=True)
    lump('Coal crown lump', (-.2, -.08, 1.35), (.93, .87, .8), 'coal', irregular=.09)
    box('Coal retaining board', (0, 1.44, .33), (4.5, .17, .66), 'wood_dark')
    for x in [-1.95, 0, 1.95]:
        box('Retaining board iron stake', (x, 1.56, .38), (.14, .17, .76), 'iron')
    # An oversized spade blade and D grip read clearly against the broad heap.
    box('Shovel blade', (.83, .65, .66), (.44, .10, .56),
        'iron', (0, -.35, -.08))
    rod('Iron shovel shaft', (.88, .65, .8), (1.25, .65, 1.58), .052, 'iron')
    ring('Shovel D grip', (1.29, .65, 1.68), .17, .045, .08,
         'iron', n=8, rot=(math.pi / 2, 0, 0))


def suitcase(loc, size, role, rot=(0, 0, 0)):
    start = len(PARTS)
    sx, sy, sz = size
    box('Baggage case', (0, 0, 0), size, role)
    strap = 'canvas' if role == 'leather' else 'leather'
    for x in [-sx * .30, sx * .30]:
        box('Case top strap', (x, 0, sz / 2 + .012), (.08, sy + .025, .026), strap, bevel=False)
        box('Case front strap', (x, sy / 2 + .012, 0), (.08, .026, sz), strap, bevel=False)
        box('Case brass clasp', (x, sy / 2 + .033, .025), (.12, .035, .11), 'brass', bevel=False)
    tube('Case carrying handle', [(-.13, sy / 2, 0), (-.13, sy / 2 + .13, 0),
                                  (.13, sy / 2 + .13, 0), (.13, sy / 2, 0)],
         [.032] * 4, 'iron', n=6)
    matrix = Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(matrix)


def luggage_cart():
    for x in [-1.27, 1.17]:
        rod('Cart axle', (x, -.77, .28), (x, .77, .28), .075)
        for y in [-.78, .78]:
            cylinder('Baggage cart wheel', (x, y, .28), .28, .18,
                     'iron', n=12, rot=(math.pi / 2, 0, 0))
    box('Baggage cart frame', (-.12, 0, .43), (3.55, 1.56, .18), 'iron')
    for j in range(5):
        box('Baggage bed plank', (-.12, -.64 + j * .32, .57), (3.65, .30, .13), 'wood')
    tube('Iron cart push handle', [(1.57, -.64, .53), (1.92, -.64, 1.14),
                                 (1.92, .64, 1.14), (1.57, .64, .53)],
         [.065] * 4, 'iron', n=8, bevel=True)
    suitcase((-.92, -.05, .82), (1.36, 1.10, .39), 'leather', (0, 0, -.05))
    suitcase((.53, -.10, .86), (1.25, 1.10, .46), 'canvas', (0, -.08, .03))
    suitcase((-.60, -.10, 1.14), (1.15, .86, .36), 'canvas', (0, .15, -.12))
    suitcase((.56, .0, 1.18), (.93, .87, .30), 'leather', (.08, -.13, .13))
    # Visible air gaps separate both falling cases from the supported stack.
    suitcase((-1.11, .04, 1.83), (.86, .63, .30), 'leather', (.17, -.30, -.23))
    suitcase((.40, .03, 1.92), (.78, .57, .28), 'canvas', (-.14, .35, .23))


def signal_gantry():
    for x in [-2.2, 2.2]:
        box('Solid gantry foot', (x, 0, .12), (.60, 1.2, .24), 'iron')
        box('Solid gantry column', (x, 0, 2.05), (.32, .44, 3.86), 'iron')
        for z in [.7, 1.5, 2.3, 3.1]:
            box('Column flange', (x, 0, z), (.41, .51, .09), 'iron', bevel=False)
    for z in [3.77, 4.39]:
        box('Gantry truss chord', (0, -.09, z), (5, .28, .18), 'iron')
    for j in range(8):
        x = -2.4 + j * .6
        beam('Lattice diagonal', (x, -.09, 3.86 if j % 2 == 0 else 4.30),
             (x + .6, -.09, 4.30 if j % 2 == 0 else 3.86), .085,
             'iron', bevel=False)
    for x in [-1.16, 1.16]:
        box('Tall signal backing', (x, .19, 3.62), (.62, .28, 1.76), 'iron_dark')
        for z, role in [(4.17, 'signal_red'), (3.62, 'signal_amber'), (3.07, 'signal_green')]:
            cylinder('Signal lens socket', (x, .365, z), .235, .12,
                     'iron', n=12, rot=(math.pi / 2, 0, 0), bevel=False)
            cylinder('Signal colored lens', (x, .44, z), .185, .07,
                     role, n=12, rot=(math.pi / 2, 0, 0), bevel=False)
            box('Signal rain hood', (x, .40, z + .22), (.53, .38, .075), 'iron_dark')


def clip_x(poly, cut, keep_greater):
    """Clip a convex polygon to a plank boundary without stair-stepped ends."""
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        inside_a = a[0] >= cut if keep_greater else a[0] <= cut
        inside_b = b[0] >= cut if keep_greater else b[0] <= cut
        if inside_a:
            result.append(a)
        if inside_a != inside_b:
            t = (cut - a[0]) / (b[0] - a[0])
            result.append((cut, a[1] + t * (b[1] - a[1])))
    return result


def turntable():
    # Only this asset has a top-centred origin: everything lies in [-0.3, 0].
    ring('Stone pit edge 0.6m wide', (0, 0, -.15), 12, .6, .30, 'stone', n=96)
    cylinder('Solid recessed deck substrate', (0, 0, -.195), 11.4, .21,
             'wood_dark', n=64, bevel=False)
    circle = [(11.35 * math.cos(j * math.tau / 96), 11.35 * math.sin(j * math.tau / 96))
              for j in range(96)]
    for j in range(54):
        low = -11.35 + j * 22.7 / 54 + .009
        high = -11.35 + (j + 1) * 22.7 / 54 - .009
        poly = clip_x(clip_x(circle, low, True), high, False)
        if len(poly) < 3:
            continue
        n = len(poly)
        verts = [(x, y, z) for z in [-.18, -.06] for x, y in poly]
        faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
        faces += [(k, (k + 1) % n, (k + 1) % n + n, k + n) for k in range(n)]
        # Diagonal planks distinguish BOTH orthogonal rail pairs at high angles.
        plank = mesh('Circle-clipped deck plank', verts, faces, 'wood_dark', True, True)
        plank.data.transform(Matrix.Rotation(math.pi / 4, 4, 'Z'))
    ring('Iron deck edge rail', (0, 0, -.045), 11.4, .075, .09, 'iron', n=96)
    # Two standard-gauge track pairs cross at grade. Split Y rails exactly at
    # the X rail heads, avoiding overlapping coplanar crossing faces.
    gauge = .75
    end = math.sqrt(11.28 ** 2 - (gauge + .08) ** 2)
    for offset in [-gauge, gauge]:
        box('X track rail foot', (0, offset, -.082), (end * 2, .18, .025), 'rail_steel', bevel=False)
        box('X track rail web', (0, offset, -.048), (end * 2, .055, .065), 'rail_steel', bevel=False)
        box('X track rail head', (0, offset, -.021), (end * 2, .12, .032), 'rail_steel')
        for start, stop in [(-end, -gauge - .09), (-gauge + .09, gauge - .09), (gauge + .09, end)]:
            center, length = (start + stop) / 2, stop - start
            box('Y track rail foot', (offset, center, -.082), (.18, length, .025), 'rail_steel', bevel=False)
        for start, stop in [(-end, -gauge - .06), (-gauge + .06, gauge - .06), (gauge + .06, end)]:
            center, length = (start + stop) / 2, stop - start
            box('Y track rail web', (offset, center, -.048), (.055, length, .065), 'rail_steel', bevel=False)
            box('Y track rail head', (offset, center, -.021), (.12, length, .032), 'rail_steel')
    cylinder('Brass central hub', (0, 0, -.04), .56, .08, 'brass', n=32)
    for j in range(16):
        a = j * math.tau / 16
        box('Brass hub gear tooth', (.60 * math.cos(a), .60 * math.sin(a), -.045),
            (.20, .14, .09), 'brass', (0, 0, a), bevel=False)
    cylinder('Center axle cap', (0, 0, -.015), .21, .03, 'brass', n=16)
    for j in range(8):
        a = j * math.tau / 8
        cylinder('Hub bolt', (.39 * math.cos(a), .39 * math.sin(a), -.012),
                 .045, .024, 'iron', n=6, bevel=False)


def platform_edge():
    box('Platform stone slab', (0, -.01, .218), (8, 1.18, .436), 'stone', bevel=False)
    # Adjacent volumes avoid coplanar grey/recolourable faces and z-fighting.
    box('Dark room-facing platform face', (0, .59, .218), (8, .02, .436), 'stone_dark', bevel=False)
    for j in range(8):
        x = -3.5 + j
        box('Top stone coping', (x, 0, .462), (.982, 1.2, .052), 'stone', bevel=False)
        box('Cream safety line', (x, .40, .494), (.982, .19, .012), 'paint_cream')
    for x in [-3, -2, -1, 0, 1, 2, 3]:
        box('Face stone mortar joint', (x, .5995, .22), (.022, .003, .42), 'stone', bevel=False)


SPECS = [
    ('LM_Coach', (10, 3.2, 3.6), coach),
    ('LM_Locomotive', (9, 3.4, 4.2), locomotive),
    ('LM_WaterCrane', (3, 3, 6), water_crane),
    ('LM_CoalPile', (4.5, 3.5, 1.8), coal_pile),
    ('LM_LuggageCart', (4, 1.8, 2), luggage_cart),
    ('LM_SignalGantry', (5, 1.2, 4.5), signal_gantry),
    ('Turntable', (24, 24, .3), turntable),
    ('PlatformEdge', (8, 1.2, .5), platform_edge),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, dimensions):
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
    obj.name = name
    obj.data.name = name + '_Mesh'
    budget = 6000 if name == 'Turntable' else 4000
    count = triangles(obj)
    # Material-delimited reduction is a fallback for Blender tessellation
    # differences. Main forms already use economical hand-authored topology.
    for attempt in range(3):
        if count <= budget:
            break
        mod = obj.modifiers.new('Preserve silhouette within triangle budget', 'DECIMATE')
        mod.ratio = (budget - 100) / count
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
        count = triangles(obj)
    assert count <= budget, (name, count, budget)
    coords = [v.co for v in obj.data.vertices]
    low = Vector([min(v[i] for v in coords) for i in range(3)])
    high = Vector([max(v[i] for v in coords) for i in range(3)])
    span = high - low
    center = (low + high) / 2
    scale = Vector([dimensions[i] / span[i] for i in range(3)])
    for v in coords:
        v.x = (v.x - center.x) * scale.x
        v.y = (v.y - center.y) * scale.y
        v.z = (v.z - (high.z if name == 'Turntable' else low.z)) * scale.z
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    assert all(abs(obj.dimensions[i] - dimensions[i]) < .001 for i in range(3)), name
    # BMesh replaced the datablock's vertex storage; do not reuse old RNA refs.
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    assert all(m and m.name in ROLES for m in obj.data.materials), name
    if name == 'Turntable':
        assert abs(max(v.co.z for v in obj.data.vertices)) < 1e-5
        assert abs(min(v.co.z for v in obj.data.vertices) + .3) < 1e-5
    else:
        assert abs(min(v.co.z for v in obj.data.vertices)) < 1e-5
    # Joining must not accidentally introduce open construction surfaces.
    check = bmesh.new()
    check.from_mesh(obj.data)
    assert all(e.is_manifold for e in check.edges), (name, 'open mesh component')
    check.free()
    obj['dimensions_m'] = list(dimensions)
    obj['front'] = '+Y'
    obj['chapter'] = 'The Stopped Station'
    obj['origin_rule'] = 'top center' if name == 'Turntable' else 'bottom center'
    obj['solid_obstacle'] = name.startswith('LM_')
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} '
          f'{tuple(round(d, 4) for d in obj.dimensions)}', flush=True)
    return obj


def main():
    global PARTS, RNG
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    setup_materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    exported = []
    for index, (name, dimensions, build) in enumerate(SPECS):
        PARTS = []
        RNG = random.Random(261117 + index * 7919)
        build()
        exported.append(finish(name, dimensions))
    assert [obj.name for obj in exported] == [spec[0] for spec in SPECS]
    assert len(bpy.context.scene.objects) == len(SPECS)
    assert all(obj.type == 'MESH' for obj in bpy.context.scene.objects)
    for mat in MATERIALS.values():
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
