"""Chapter 1 gate kit. Run with Blender 5.x --background --factory-startup -P.

Exports stay at assembly coordinates with grey role materials. The final contact
sheet is a separate, temporary presentation of the already exported meshes.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Game/Assets/Art/Characters/Ch1"
PREVIEW = ROOT / "Art/tools/specs/blender_kit_ch1_gates_preview.png"
MATS = {}
ASSETS = []
RNG = random.Random(1107)
COLORS = {
    "wood": (.58, .34, .17), "wood_light": (.78, .55, .30),
    "wood_dark": (.34, .19, .10), "iron": (.18, .23, .26),
    "brass": (.70, .44, .12), "gold": (.95, .65, .12),
    "gold_drip": (1.0, .74, .12), "canvas": (.67, .52, .31),
    "paint_teal": (.12, .48, .43), "paint_red": (.65, .16, .10),
    "brick": (.57, .27, .16), "stone": (.56, .52, .43),
    "rock": (.64, .45, .31), "rock_dark": (.46, .35, .27),
    "rock_light": (.76, .56, .35), "sand": (.79, .62, .38),
    "bone": (.92, .84, .63), "cream": (.96, .87, .63),
    "window_lit": (1.0, .68, .25),
}


def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def recalc(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), obj.name + ': open component'
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()


def bevel(obj, width=.018, angle=30):
    activate(obj)
    mod = obj.modifiers.new('Hand softened edges', 'BEVEL')
    mod.width = width
    mod.segments = 2
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(angle)
    mod.use_clamp_overlap = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def mesh(name, verts, faces, mat, soft=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[mat])
    recalc(obj)
    if soft:
        bevel(obj)
    return obj


def box(loc, size, mat, soft=True):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = mat
    obj.dimensions = size
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(MATS[mat])
    if soft:
        bevel(obj)
    return obj


def prism(points, y0, y1, mat, soft=True, seams=()):
    n = len(points)
    verts = [(x, y, z) for y in (y0, y1) for x, z in points]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    obj = mesh(mat, verts, faces, mat, soft)
    if seams:
        # Restore the shared jagged fracture interface after bevel, as in the
        # woodgate reference. Adjacent chunks use the EXACT same boundary.
        corners = [Vector(p) for path in seams for p in path]
        segments = [(Vector(a), Vector(b)) for path in seams
                    for a, b in zip(path, path[1:])]
        for v in obj.data.vertices:
            p = Vector((v.co.x, v.co.z))
            q = min(corners, key=lambda c: (c - p).length_squared)
            if (q - p).length > .039:
                candidates = []
                for a, b in segments:
                    d = b - a
                    t = max(0, min(1, (p - a).dot(d) / d.length_squared))
                    candidates.append(a + d * t)
                q = min(candidates, key=lambda c: (c - p).length_squared)
            if (q - p).length <= .039:
                v.co.x, v.co.z = q
                if abs(v.co.y - y0) < .021:
                    v.co.y = y0
                if abs(v.co.y - y1) < .021:
                    v.co.y = y1
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
        bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=.000001)
        bm.to_mesh(obj.data)
        bm.free()
        recalc(obj)
    return obj


def join(objects, name='component'):
    objects = list(objects)
    assert objects
    activate(objects[0])
    for obj in objects:
        obj.select_set(True)
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = objects[0]
    obj.name = name
    return obj


def tube(points, radius, mat, sides=8, soft=False):
    """Closed low-poly tube; shared tangent sections also permit exact bar cuts."""
    points = [Vector(p) for p in points]
    verts = []
    for i, point in enumerate(points):
        tangent = points[min(i + 1, len(points) - 1)] - points[max(0, i - 1)]
        tangent.normalize()
        reference = Vector((0, 1, 0))
        if abs(tangent.dot(reference)) > .95:
            reference = Vector((1, 0, 0))
        u = tangent.cross(reference).normalized()
        v = tangent.cross(u).normalized()
        for j in range(sides):
            a = j * math.tau / sides
            verts.append(tuple(point + radius * (u * math.cos(a) + v * math.sin(a))))
    faces = [tuple(range(sides - 1, -1, -1))]
    for i in range(len(points) - 1):
        for j in range(sides):
            a = i * sides + j
            b = i * sides + (j + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(range((len(points) - 1) * sides, len(points) * sides)))
    obj = mesh(mat, verts, faces, mat)
    # Round rods already have a faceted round cross-section. Bevel their hard
    # end caps and sharp bends, rather than beveling every longitudinal seam.
    return bevel(obj, angle=60) if soft else obj


def ball(loc, size, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=loc)
    obj = bpy.context.object
    for v in obj.data.vertices:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    obj.data.materials.append(MATS[mat])
    return obj


def ring(x, y, z, radius, thickness, mat, start=0, end=math.tau, steps=16):
    return tube([(x + radius * math.cos(a), y, z + radius * math.sin(a))
                 for a in [start + (end - start) * i / steps for i in range(steps + 1)]],
                thickness, mat, sides=6)


def board(a, b, width, y0, y1, mat, count=2, jagged=True):
    a, b = Vector(a), Vector(b)
    t = (b - a).normalized()
    normal = Vector((-t.y, t.x))
    cuts = []
    for i in range(count + 1):
        center = a.lerp(b, i / count)
        if i in (0, count) or not jagged:
            cuts.append([tuple(center - normal * width / 2),
                         tuple(center + normal * width / 2)])
        else:
            cuts.append([tuple(center + normal * width * (j / 4 - .5)
                               + t * (0 if j in (0, 4) else (.046 if j % 2 else -.046)))
                         for j in range(5)])
    result = []
    for i in range(count):
        seams = ([cuts[i]] if i else []) + ([cuts[i + 1]] if i < count - 1 else [])
        result.append(prism(cuts[i] + list(reversed(cuts[i + 1])), y0, y1, mat,
                            seams=seams))
    return result


def drip(x, y, top, length, width=.11):
    return prism([(x - width / 2, top), (x + width / 2, top),
                  (x + width * .29, top - length * .62),
                  (x + width * .55, top - length * .84),
                  (x + width * .40, top - length * .97),
                  (x, top - length), (x - width * .45, top - length * .91),
                  (x - width * .48, top - length * .81),
                  (x - width * .18, top - length * .58)],
                 y - .035, y + .035, 'gold_drip')


def skull(x, y, z, scale=1):
    def p(a, b):
        return (x + a * scale, z + b * scale)
    parts = [prism([p(-.19, .14), p(.19, .14), p(.16, -.07),
                    p(.08, -.12), p(.055, -.32), p(-.055, -.32),
                    p(-.08, -.12), p(-.16, -.07)],
                   y - .045 * scale, y + .045 * scale, 'bone')]
    for sign in (-1, 1):
        parts.append(tube([(x + sign * a * scale, y, z + b * scale)
                           for a, b in ((.13, .08), (.31, .13), (.42, .32), (.40, .39))],
                          .055 * scale, 'bone', 6))
        parts.append(ball((x + sign * .105 * scale, y + .052 * scale, z),
                          (.065 * scale, .018 * scale, .060 * scale), 'wood_dark'))
    return join(parts)


def clock(x, y, z, radius, melt=.25):
    """Drooping dial, swept gold bezel, large hour marks and readable hands."""
    outline = []
    steps = 12 if radius < .25 else 24
    ticks = 4 if radius < .25 else 12
    for i in range(steps):
        a = i * math.tau / steps
        px = math.cos(a) * radius
        pz = math.sin(a) * radius
        sag = melt * max(0, math.cos(a)) ** 3
        outline.append((x + px, z + pz - sag))
    parts = [prism(outline, y - .055, y + .055, 'cream', soft=False)]
    parts.append(tube([(px, y + .055, pz) for px, pz in outline + outline[:1]],
                      min(.045, radius * .10), 'gold', 6))
    for i in range(ticks):
        a = i * math.tau / ticks
        r = radius * .79
        sag = melt * max(0, math.cos(a)) ** 3
        parts.append(tube([(x + r * math.cos(a), y + .08, z + r * math.sin(a) - sag),
                           (x + (r - radius * .18) * math.cos(a), y + .08,
                            z + (r - radius * .18) * math.sin(a) - sag)],
                          min(.018, radius * .05), 'iron', 4))
    parts += [tube([(x, y + .09, z), (x - radius * .34, y + .09, z + radius * .30)], .027, 'iron', 6),
              tube([(x, y + .09, z), (x + radius * .36, y + .09, z + radius * .47)], .022, 'iron', 6),
              ball((x, y + .10, z), (.055, .025, .055), 'brass')]
    return join(parts)


def rivets(obj, coords, mat='iron'):
    return join([obj] + [ball(p, (.043, .022, .043), mat) for p in coords])


def build_gate(stage):
    parts = []
    if stage == 1:
        for x in (-2.13, 2.13):
            post = board((x, 0), (x, 2.38), .34, -.15, .15, 'wood_dark')
            for z in (.56, 1.76):
                # Canvas is the allowed fibrous rope role; wraps encircle posts.
                wraps = []
                for dz in (-.052, .025, .10):
                    wraps.append(tube([(x - .146, -.13, z + dz), (x + .146, -.13, z + dz),
                                       (x + .146, .13, z + dz), (x - .146, .13, z + dz),
                                       (x - .146, -.13, z + dz)], .021, 'canvas', 6))
                post[0 if z < 1 else 1] = join([post[0 if z < 1 else 1]] + wraps)
            parts += post
        for row, z in enumerate((.36, .91, 1.46, 2.02)):
            chunks = board((-1.99, z), (1.99, z + (.04 if row % 2 else -.035)),
                           .26, -.12, .12, 'wood' if row % 2 else 'wood_light', 3)
            for i, part in enumerate(chunks):
                knot = ball((-1.35 + i * 1.32, .126, z), (.09, .015, .037), 'wood_dark')
                chunks[i] = join([part, knot])
            parts += chunks
        parts[-2] = join([parts[-2], skull(0, .112, 2.36, .82)])
    elif stage == 2:
        for i in range(8):
            x = -2.3 + (i + .5) * .575
            h = 2.52 + (.06 if i in (2, 5) else -.025 * (i % 3))
            parts += board((x, 0), (x, h), .575, -.105, .075,
                           'wood_light' if i % 3 == 0 else 'wood', 2)
        for a, b, depth in [((-2.17, .32), (2.17, 2.27), .135),
                             ((-2.17, 2.27), (2.17, .32), .17)]:
            parts += board(a, b, .19, .077, depth, 'wood_dark', 2)
        for z in (.35, 2.17):
            # Split each strap into four chunks, attached to existing owners.
            for i in range(4):
                s = box((-1.725 + 1.15 * i, -.127, z), (1.15, .045, .11), 'iron')
                s = rivets(s, [(-1.725 + 1.15 * i, -.151, z)])
                owner = i * 4 + (0 if z < 1 else 1)
                parts[owner] = join([parts[owner], s])
        sign = box((0, .12, 1.85), (1.12, .05, .43), 'paint_red')
        # A bold hand-painted exclamation, no fragile text or external fonts.
        mark = box((0, .151, 1.91), (.075, .018, .19), 'cream')
        dot = ball((0, .157, 1.74), (.043, .014, .035), 'cream')
        parts[9] = join([parts[9], sign, mark, dot])
    else:
        # Eight independently severed frame chunks and twelve iron bar chunks.
        for x in (-2.2, 2.2):
            parts += board((x, 0), (x, 2.60), .20, -.14, .14, 'iron', 2, False)
        for z in (.12, 2.49):
            parts += board((-2.3, z), (2.3, z), .22, -.14, .14, 'iron', 2, False)
        for i, x in enumerate((-1.75, -1.05, -.35, .35, 1.05, 1.75)):
            bend = .09 if stage == 3 and i in (1, 4) else 0
            melt = .14 * max(0, x) if stage == 4 else 0
            for k in range(2):
                z0, z1 = (.23, 1.30) if k == 0 else (1.30, 2.40)
                # Vertical tangents at the shared cut keep mating caps flush.
                points = [(x, 0, z0), (x, 0, z0 + .12),
                          (x + bend + melt, 0, (z0 + z1) / 2),
                          (x, 0, z1 - .12), (x, 0, z1)]
                parts.append(tube(points, .057 if stage == 3 else .048, 'iron', 8, True))
        if stage == 3:
            for z in (.77, 1.86):
                parts += board((-2.1, z), (2.1, z), .09, -.045, .045, 'iron', 2, False)
            lock = box((0, .065, 1.30), (.64, .18, .47), 'brass')
            hole = ball((0, .164, 1.34), (.053, .017, .062), 'iron')
            slot = box((0, .164, 1.27), (.045, .02, .09), 'iron')
            parts[14] = join([parts[14], lock, hole, slot])
        else:
            # Two clearly separate leaves: central stiles, inward-facing scrolls.
            for sign in (-1, 1):
                stile = box((sign * .055, 0, 1.32), (.07, .12, 2.18), 'iron')
                decorations = [stile]
                for z in (.65, 1.83):
                    for x in (.57, 1.38):
                        decorations.append(ring(sign * x, .035, z, .23, .034,
                                                'iron', -.4, math.tau * .88, 12))
                parts.append(join(decorations))
            medal = clock(0, .065, 1.67, .36, .13)
            rays = []
            for i in range(10):
                a = i * math.tau / 10
                rays.append(tube([(.40 * math.cos(a), .07, 1.67 + .40 * math.sin(a)),
                                  (.51 * math.cos(a), .07, 1.67 + .51 * math.sin(a))], .025, 'brass', 5))
            parts[11] = join([parts[11], medal] + rays)
            for i, x in enumerate((.45, .85, 1.20, 1.57, 1.94)):
                parts[7] = join([parts[7], drip(x, .145, 2.60, .32 + .11 * (i % 3), .12)])
        for i in range(8):
            # Rivets live on their own frame fragment, not on a separate object.
            x = (-2.20 if i < 4 else 2.20)
            z = .25 + (i % 4) * .66
            owner = (0 if i < 4 else 2) + (1 if z > 1.3 else 0)
            parts[owner] = rivets(parts[owner], [(x, .15, z)])
    assert 10 <= len(parts) <= 24, (stage, len(parts))
    for i, obj in enumerate(parts):
        obj.name = 'Piece_%02d' % i
        activate(obj)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    # Templates are physically enclosed by a central solid owner at Z=1.3.
    # For the open ranch gate, this tiny core is tucked behind its central log.
    if stage == 1:
        core = box((0, 0, 1.30), (.31, .11, .43), 'wood')
        parts[11] = join([parts[11], core], parts[11].name)
    elif stage == 4:
        core = box((0, 0, 1.30), (.13, .10, .38), 'iron')
        parts[20] = join([parts[20], core], parts[20].name)
    debris = []
    for i in range(3):
        length = .24 + i * .065
        width = .040 + i * .006
        outline = [(0, -.5), (.5, -.28), (.34, .15), (0, .5),
                   (-.5, .22), (-.34, -.21)]
        chip = prism([(x * width, 1.3 + z * length) for x, z in outline], -.012, .012,
                     'wood' if stage < 3 else 'iron', soft=False)
        chip.name = 'Splinter_%d' % i
        debris.append(chip)
    if stage == 4:
        # The whole right leaf sags continuously, including its frame, curls and
        # molten accents. Identical cut coordinates receive identical offsets.
        for obj in parts:
            inverse = obj.matrix_world.inverted()
            for vertex in obj.data.vertices:
                p = obj.matrix_world @ vertex.co
                if p.x > 0:
                    sag = .18 * math.sin(math.pi * p.x / 2.3) + .10 * p.x / 2.3
                    p.z -= sag * (max(0, p.z) / 2.6) ** 3
                    vertex.co = inverse @ p
    # Joining decorations above can shift geometric centers: set all final pivots.
    for obj in parts + debris:
        activate(obj)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    return parts + debris


def rock(loc, size, mat):
    obj = ball(loc, size, mat)
    # Seeded per-rock silhouette rather than a repeated perfect ico-sphere.
    for v in obj.data.vertices:
        v.co *= RNG.uniform(.91, 1.07)
    return obj


def barrel(x, y, z=.05):
    profile = [(0, .22), (.09, .26), (.43, .29), (.78, .26), (.86, .22)]
    verts = [(x + r * math.cos(i * math.tau / 10),
              y + r * math.sin(i * math.tau / 10), z + h)
             for h, r in profile for i in range(10)]
    faces = [tuple(range(9, -1, -1)), tuple(range(40, 50))]
    faces += [(k * 10 + i, k * 10 + (i + 1) % 10,
               (k + 1) * 10 + (i + 1) % 10, (k + 1) * 10 + i)
              for k in range(4) for i in range(10)]
    parts = [mesh('barrel', verts, faces, 'wood_light', True)]
    for h in (.16, .67):
        points = [(x + .266 * math.cos(i * math.tau / 10),
                   y + .266 * math.sin(i * math.tau / 10), z + h) for i in range(11)]
        parts.append(tube(points, .023, 'iron', 4))
    parts.append(box((x, y, z + .866), (.36, .035, .012), 'wood_dark', False))
    return join(parts)


def crate(x, y, z, size=.49):
    parts = [box((x, y, z + size / 2), (size, size, size), 'wood')]
    for dx in (-size * .39, size * .39):
        parts.append(box((x + dx, y + size / 2, z + size / 2),
                         (.06, .045, size), 'wood_light'))
    parts.append(tube([(x - size * .35, y + size / 2 + .025, z + .08),
                       (x + size * .35, y + size / 2 + .025, z + size - .08)],
                      .045, 'wood_light', 4))
    return join(parts)


def build_corridor(stage):
    parts = [box((0, 0, -.09), (4.6, 10.4, .22), 'sand' if stage == 1 else
                 ('wood_dark' if stage == 2 else 'stone'))]
    if stage == 1:
        for i in range(17):
            parts.append(rock((RNG.uniform(-1.95, 1.95), RNG.uniform(-4.85, 4.85), .025),
                              (RNG.uniform(.17, .38), RNG.uniform(.17, .35), .038),
                              'rock_light' if i % 3 else 'stone'))
        for sign in (-1, 1):
            for i in range(9):
                y = -4.60 + i * 1.15
                for level in range(2 if i % 3 else 3):
                    # Keep canyon dressing entirely inside the 0.6 m edge band.
                    parts.append(rock((sign * 2.60, y + .06 * (level % 2), .32 + level * .43),
                                      (.28, .58, .38), 'rock' if (i + level) % 3 else 'rock_dark'))
            for y in (-3.0, 2.1):
                parts.append(tube([(sign * 2.6, y, .05), (sign * 2.70, y + .13, 1.72)],
                                  .085, 'wood_dark', 6, True))
    elif stage == 2:
        for i in range(24):
            plank = box((0, -5.2 + (i + .5) * 10.4 / 24, .04),
                        (4.6, 10.4 / 24 - .025, .04), 'wood_light' if i % 4 else 'wood')
            parts.append(plank)
        for sign in (-1, 1):
            for i in range(5):
                y = -4.8 + i * 2.4
                parts.append(box((sign * 2.60, y, .54), (.20, .20, 1.08), 'wood_dark'))
                if i < 4:
                    parts.append(box((sign * 2.60, y + 1.2, .87), (.14, 2.4, .13), 'wood'))
            parts.append(barrel(sign * 2.59, -1.4 if sign == 1 else 1.5))
            parts.append(box((sign * 2.6, 3.6 if sign == 1 else -3.6, .69),
                             (.56, .15, .14), 'wood_dark'))
    else:
        # Twelve broad slabs leave an actual recessed drain channel down center.
        for sign in (-1, 1):
            for row in range(6):
                parts.append(box((sign * 1.205, -5.2 + (row + .5) * 10.4 / 6, .04),
                                 (2.17, 10.4 / 6 - .035, .04), 'stone'))
        parts.append(box((0, 0, .025), (.21, 10.4, .01), 'iron', False))
        for row in range(12):
            parts.append(box((0, -4.75 + row * .86, .044), (.27, .055, .025), 'iron', False))
        for sign in (-1, 1):
            if stage == 3:
                for row in range(2):
                    for i in range(6):
                        parts.append(box((sign * 2.6, -4.33 + i * 1.73, .25 + row * .44),
                                         (.58, 1.69, .43), 'brick'))
                for i in range(4):
                    parts.append(box((sign * 2.6, -3.9 + i * 2.6, .94),
                                     (.60, 2.56, .14), 'stone'))
                parts.append(crate(sign * 2.6, -2.2, 1.02))
                parts.append(crate(sign * 2.6, -2.2, 1.51, .43))
            else:
                for i in range(4):
                    y = -3.9 + i * 2.6
                    # Sagging top profile, still low and confined to edge band.
                    wall = prism([(sign * 2.3, .02), (sign * 2.9, .02),
                                  (sign * 2.9, .68), (sign * 2.7, .55),
                                  (sign * 2.3, .80)], y - 1.27, y + 1.27, 'stone')
                    parts.append(wall)
                for i, y in enumerate((-3.8, 0, 3.8)):
                    x = sign * 2.61
                    parts.append(box((x, y, .12), (.50, .47, .20), 'stone'))
                    parts.append(tube([(x, y, .20), (x, y, 1.20),
                                       (x + sign * .08, y, 1.68)], .060, 'iron', 8, True))
                    if i != 1:
                        parts.append(clock(x, y + .03, 1.67, .205, .17))
                    else:
                        parts.append(box((x, y, 1.63), (.26, .27, .35), 'brass'))
                        parts.append(box((x, y + .15, 1.64), (.17, .025, .23), 'window_lit'))
                        parts.append(ball((x, y, 1.88), (.23, .22, .09), 'iron'))
                    parts.append(drip(x + .08 * sign, y + .12, 1.62, .61, .09))
                    parts.append(ball((x, y + .27, .075), (.27, .31, .045), 'gold_drip'))
    return [join(parts, 'Corridor_S%d' % stage)]


def build_portal(stage):
    parts = []
    if stage == 1:
        for sign in (-1, 1):
            for i in range(4):
                # Solid irregular prisms keep the opening strictly unobstructed.
                x0, x1 = (2.3, 3.34) if sign > 0 else (-3.34, -2.3)
                z0 = i * .77
                parts.append(prism([(x0 + .11, z0), (x1 - .08, z0),
                                    (x1, z0 + .19), (x1 - .035, z0 + .78),
                                    (x0 + .035, z0 + .80), (x0, z0 + .33)], -.49, .49,
                                   'rock' if i % 2 else 'rock_dark'))
        parts += board((-3.23, 3.24), (3.20, 3.43), .36, -.27, .27, 'wood_dark', 1)
        parts.append(tube([(-.52, .49, 3.45), (-.52, .49, 3.14)], .026, 'canvas', 6))
        parts.append(skull(-.52, .51, 3.12, .50))
    elif stage == 2:
        for sign in (-1, 1):
            parts.append(box((sign * 2.67, 0, 1.65), (.74, .69, 3.30), 'wood_dark'))
            parts.append(box((sign * 2.755, 0, .13), (.91, .88, .26), 'wood'))
            for z in (.45, 2.73):
                parts.append(box((sign * 2.67, .37, z), (.74, .09, .16), 'iron'))
        parts.append(box((0, 0, 3.39), (6.58, .73, .37), 'wood'))
        for x in (-.93, .93):
            parts.append(tube([(x, .43, 3.40), (x, .43, 3.17)], .025, 'iron', 6))
        parts.append(box((0, .43, 3.13), (2.58, .16, .45), 'wood_light'))
        parts.append(drip(2.66, .40, 3.50, 1.15, .16))
    else:
        for sign in (-1, 1):
            if stage == 3:
                for row in range(5):
                    parts.append(box((sign * 2.76, 0, .295 + row * .59),
                                     (.92, .94, .58), 'brick'))
                parts.append(box((sign * 2.85, 0, .12), (1.10, 1.10, .24), 'stone'))
            else:
                parts.append(prism([(sign * 2.3, 0), (sign * 3.22, 0),
                                    (sign * 3.30, 3.29), (sign * 2.42, 3.49),
                                    (sign * 2.3, 2.54)], -.43, .43, 'stone'))
                for z in (.16, 2.86):
                    parts.append(box((sign * 2.84, 0, z), (1.08, 1.05, .26), 'brass'))
        if stage == 3:
            # A shallow segmented arch: its lowest intrados is Z=2.92.
            for i in range(11):
                x0 = -2.3 + i * 4.6 / 11
                x1 = x0 + 4.6 / 11
                def underside(x):
                    return 2.92 + .54 * max(0, 1 - (x / 2.3) ** 2)
                parts.append(prism([(x0, underside(x0)), (x1, underside(x1)),
                                    (x1, underside(x1) + .39),
                                    (x0, underside(x0) + .39)], -.45, .45, 'brick'))
            parts.append(clock(0, .46, 3.66, .34, .21))
            parts.append(drip(.20, .51, 3.51, .51, .10))
        else:
            parts.append(prism([(-3.3, 3.16), (-1.7, 3.13), (0, 3.07),
                                (1.6, 2.95), (3.3, 2.94), (3.3, 3.54),
                                (1.6, 3.66), (0, 3.74), (-3.3, 3.66)],
                               -.44, .44, 'stone'))
            dial = clock(.12, .47, 3.61, .54, .40)
            for vertex in dial.data.vertices:
                vertex.co.x = .12 + (vertex.co.x - .12) * 1.35
            parts.append(dial)
            # A long side of the dial visibly slides across the lintel.
            parts.append(drip(.74, .52, 3.55, .58, .15))
            for x, length in ((-1.43, .44), (1.62, .36), (2.78, 1.47), (-2.74, .89)):
                top = 3.53 if abs(x) > 2.3 else 3.40
                parts.append(drip(x, .49, top, length, .18))
            for sign in (-1, 1):
                parts.append(ball((sign * 2.8, .17, .085), (.48, .38, .075), 'gold_drip'))
    return [join(parts, 'Portal_S%d' % stage)]


def build_reward():
    frame = []
    for sign in (-1, 1):
        x = sign * 1.32
        frame += [box((x, 0, 1.48), (.30, .42, 2.96), 'wood_dark'),
                  box((x, 0, .10), (.36, .50, .20), 'wood'),
                  box((x, .23, 2.64), (.30, .06, .33), 'brass'),
                  box((x, .23, .34), (.30, .06, .24), 'brass'),
                  box((x, .225, 1.5), (.10, .025, 1.60), 'wood_light')]
    frame += [box((0, 0, 2.86), (3.0, .45, .28), 'wood'),
              box((0, .247, 2.88), (1.8, .04, .12), 'wood_light'),
              drip(.98, .26, 2.95, .46, .12)]
    frame_obj = join(frame, 'Frame')
    leaves = []
    for sign, name in ((-1, 'Leaf_L'), (1, 'Leaf_R')):
        a, b = sorted((sign * .035, sign * 1.16))
        points = [(a, .48), (b, .48), (b, 2.18),
                  ((a + b) / 2, 2.40), (a, 2.18)]
        pieces = [prism(points, -.09, .09, 'wood_light')]
        center = (a + b) / 2
        pieces += [box((center, .108, 1.35), (.86, .035, 1.07), 'paint_teal'),
                   box((center, .13, .80), (1.02, .055, .11), 'wood'),
                   box((center, .13, 1.91), (1.02, .055, .11), 'wood')]
        for x in (center - .32, center, center + .32):
            pieces.append(box((x, .135, 1.35), (.035, .025, .94), 'wood_light', False))
        for z in (.72, 2.07):
            pieces.append(box((sign * 1.12, .01, z), (.17, .25, .12), 'iron'))
        leaves.append(join(pieces, name))
    return [frame_obj] + leaves


def bounds(objects):
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ Vector(c) for obj in objects for c in obj.bound_box]
    lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    return lo, hi


def export(name, objects, kind, stage=0):
    for obj in objects:
        recalc(obj)
        assert obj.type == 'MESH' and obj.parent is None
        assert all(abs(s - 1) < .000001 for s in obj.scale)
        for mat in obj.data.materials:
            assert mat.name in COLORS
            assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    if kind != 'gate':
        for obj in objects:
            activate(obj)
            bpy.context.scene.cursor.location = (0, 0, 0)
            if obj.name.startswith('Leaf_'):
                bpy.context.scene.cursor.location = (-1.16 if obj.name == 'Leaf_L' else 1.16, 0, .48)
            bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    lo, hi = bounds(objects)
    dims = hi - lo
    triangles = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
    limit = {'gate': 6000, 'corridor': 7000, 'portal': 4000, 'reward': 5000}[kind]
    assert triangles <= limit, (name, triangles, limit)
    eps = .0001
    if kind == 'gate':
        assert abs(lo.x + 2.3) < eps and abs(hi.x - 2.3) < eps, (name, lo, hi)
        assert abs(lo.z) < eps and 2.4 <= hi.z <= 2.7
        assert dims.y <= .3501 and abs((lo.y + hi.y) / 2) <= .035, (name, dims)
        assert [o.name for o in objects[:-3]] == ['Piece_%02d' % i for i in range(len(objects) - 3)]
        assert [o.name for o in objects[-3:]] == ['Splinter_%d' % i for i in range(3)]
        for obj in objects:
            local = [Vector(c) for c in obj.bound_box]
            center = Vector(tuple((min(c[i] for c in local) + max(c[i] for c in local)) / 2 for i in range(3)))
            assert center.length < eps, (obj.name, center)
    elif kind == 'corridor':
        assert len(objects) == 1 and abs(lo.y + 5.2) < eps and abs(hi.y - 5.2) < eps
        assert lo.x >= -2.9001 and hi.x <= 2.9001 and hi.z <= 2.5, (name, lo, hi)
    elif kind == 'portal':
        assert len(objects) == 1 and 3.4 <= hi.z <= 4.2, (name, hi)
        assert lo.x >= -3.4001 and hi.x <= 3.4001 and lo.y >= -.6001 and hi.y <= .6001, (name, lo, hi)
        for obj in objects:
            for vertex in obj.data.vertices:
                p = obj.matrix_world @ vertex.co
                assert not (-2.2999 < p.x < 2.2999 and p.z < 2.8999), (name, tuple(p))
    else:
        assert [o.name for o in objects] == ['Frame', 'Leaf_L', 'Leaf_R']
        assert abs(dims.x - 3) < eps and abs(hi.z - 3) < eps
    activate(objects[0])
    for obj in objects:
        obj.select_set(True)
    path = OUT / (name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    assert path.is_file() and path.stat().st_size > 0
    print('EXPORTED %s %d %d %s' % (name, len(objects), triangles,
          tuple(round(v, 4) for v in dims)), flush=True)
    ASSETS.append((name, objects, kind, stage))
    # Free the exact object names for the next FBX without changing its file.
    for obj in objects:
        obj.name = name + '__' + obj.name
    return triangles


def preview():
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.render.filepath = str(PREVIEW)
    scene.world.color = (.78, .73, .63)
    shade = scene.display.shading
    shade.light = 'STUDIO'
    shade.studiolight_rotate_z = .4
    shade.color_type = 'MATERIAL'
    shade.show_shadows = True
    shade.show_cavity = True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor = 1.4
    shade.curvature_valley_factor = 1.1
    shade.show_object_outline = True
    shade.background_type = 'WORLD'
    scene.view_settings.view_transform = 'Standard'
    scene.display.render_aa = '16'
    for name, mat in MATS.items():
        mat.diffuse_color = (*COLORS[name], 1)
    elevation = math.radians(55)
    up = Vector((0, -math.sin(elevation), math.cos(elevation)))
    normal = Vector((0, math.cos(elevation), math.sin(elevation)))
    right = Vector((-1, 0, 0))
    bpy.ops.object.camera_add(location=normal * 60)
    camera = bpy.context.object
    camera.rotation_euler = (-normal).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 38.8
    scene.camera = camera

    def text(body, x, v, size, mat='wood_dark'):
        curve = bpy.data.curves.new('Preview label', 'FONT')
        curve.body = body
        curve.size = size
        curve.align_x = 'CENTER'
        curve.extrude = 0
        obj = bpy.data.objects.new('Preview label', curve)
        scene.collection.objects.link(obj)
        obj.location = right * x + up * v + normal * 4
        obj.rotation_euler = camera.rotation_euler
        curve.materials.append(MATS[mat])

    text('CHAPTER 01  /  THE MELTING FRONTIER', -1, 10.0, .62)
    text('GATES', -12.3, 8.65, .36)
    text('CORRIDORS  /  10.4 m', -2.0, 8.65, .36)
    text('PORTALS', 8.3, 8.65, .36)
    titles = ['01  BADLANDS', '02  OUTSKIRTS', '03  SHERIFF ALLEY', '04  MELTING PLAZA']
    for row, title in enumerate(titles):
        text(title, -16.85, 6.5 - row * 4.5, .29)
    for name, objects, kind, stage in ASSETS:
        if kind == 'reward':
            x, vertical, scale = 15.8, -7.4, .83
        else:
            x = {'gate': -11.9, 'corridor': -2.0, 'portal': 8.0}[kind]
            vertical = 5.25 - (stage - 1) * 4.5
            scale = {'gate': 1.10, 'corridor': .58, 'portal': .88}[kind]
        # Place the projected bounding-box center exactly in its grid cell.
        rotation = Matrix.Rotation(math.radians(75), 4, 'Z') if kind == 'corridor' else Matrix.Identity(4)
        points = [rotation @ (obj.matrix_world @ vertex.co)
                  for obj in objects for vertex in obj.data.vertices]
        center_x = (min(p.x for p in points) + max(p.x for p in points)) / 2
        center_up = (min(p.dot(up) for p in points) + max(p.dot(up) for p in points)) / 2
        projected_center = Vector((center_x, 0, 0)) + up * center_up
        offset = right * x + up * vertical - projected_center * scale
        for obj in objects:
            if '__Splinter_' in obj.name:
                obj.hide_render = True
            obj.matrix_world = Matrix.Translation(offset) @ Matrix.Scale(scale, 4) @ rotation @ obj.matrix_world
        if kind == 'reward':
            text('REWARD', 15.8, -4.65, .35)
            text('SALOON DOOR', 15.8, -5.15, .25)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file()
    # Even the in-memory role materials return to their neutral delivery state.
    for mat in MATS.values():
        mat.diffuse_color = (.5, .5, .5, 1)


def main():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    for name in COLORS:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        MATS[name] = mat
    OUT.mkdir(parents=True, exist_ok=True)
    for stage in range(1, 5):
        RNG.seed(1107 + stage * 100)
        export('Kit_Gate_S%d' % stage, build_gate(stage), 'gate', stage)
        export('Kit_Corridor_S%d' % stage, build_corridor(stage), 'corridor', stage)
        export('Kit_Portal_S%d' % stage, build_portal(stage), 'portal', stage)
    export('Kit_RewardDoor', build_reward(), 'reward')
    preview()
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
