# 거울 세계 4개 스테이지의 파괴형 문, 통로, 포털과 보상 문을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_gates.py
"""Deterministic Blender 5.x kit. Exports stay gray; only the review is colored."""

import hashlib
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_gates_preview.png'
PALETTE = {
    'sandstone': (.71, .36, .43), 'sandstone_light': (.95, .64, .59),
    'sandstone_dark': (.45, .21, .31), 'rock': (.47, .37, .48),
    'rock_dark': (.25, .23, .32), 'sand': (.86, .64, .48),
    'marble': (.93, .90, .83), 'marble_dark': (.57, .53, .65),
    'gold': (1.0, .68, .24), 'gold_dark': (.53, .32, .12),
    'mirror': (.69, .75, .95), 'mirror_back': (.29, .28, .42),
    'crystal': (.73, .86, 1.0), 'crystal_core': (1.0, .98, .91),
    'water': (.16, .57, .60), 'water_foam': (.69, .96, .89),
    'cloud': (.98, .88, .91), 'cloud_shade': (.61, .57, .76),
    'cloth_rose': (.69, .28, .45), 'stardust': (1.0, .87, .56),
    'wood': (.44, .25, .29), 'roof_tile': (.65, .34, .40),
    'window_lit': (1.0, .88, .67),
}
HARD = {'sandstone', 'sandstone_light', 'sandstone_dark', 'rock',
        'rock_dark', 'marble', 'marble_dark', 'gold', 'gold_dark', 'wood'}
MATS = {}
PARTS = []
ASSETS = []


def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], out.inputs['Surface'])
        MATS[role] = mat
    OUTPUT.mkdir(parents=True, exist_ok=True)


def recalc(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), obj.name + ': open component'
    assert all(face.calc_area() > 1e-11 for face in bm.faces), obj.name + ': zero area face'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for face in obj.data.polygons:
        face.use_smooth = False


def mesh(label, verts, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    PARTS.append(obj)
    recalc(obj)
    if bevel and role in HARD:
        mod = obj.modifiers.new('Handmade edge bevel', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(35)
        mod.use_clamp_overlap = True
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, bevel=True, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
    return place(mesh(label, [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                              (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)],
                      [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
                       (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], role, bevel), loc, rot)


def prism(label, profile, back, front, role, bevel=True, back_role=None):
    n = len(profile)
    obj = mesh(label, [(x, y, z) for y in (back, front) for x, z in profile],
               [tuple(reversed(range(n))), tuple(range(n, n * 2))] +
               [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)],
               role, bevel)
    if back_role:
        obj.data.materials.append(MATS[back_role])
        obj.data.polygons[0].material_index = 1
        for face in list(obj.data.polygons)[2:]:
            face.material_index = 1
    return obj


def cylinder(label, loc, radius, height, role, n=8, top=None, rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in ((radius, -height / 2), (top, height / 2)) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def tube(label, points, radius, role, n=6):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(0, i - 1)])).normalized()
        ref = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        side = ref.cross(tangent).normalized()
        other = tangent.cross(side)
        verts += [tuple(Vector(point) + radius *
                        (side * math.cos(j * math.tau / n) + other * math.sin(j * math.tau / n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(len(verts) - n, len(verts)))]
    faces += [(i * n + j, i * n + (j + 1) % n, (i + 1) * n + (j + 1) % n,
               (i + 1) * n + j) for i in range(len(points) - 1) for j in range(n)]
    return mesh(label, verts, faces, role, bevel=False)


def lump(label, loc, size, role, cloud=False):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2 if cloud else 1, radius=1)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        v.co *= random.uniform(.97, 1.03) if cloud else random.uniform(.88, 1.12)
    low = [min(v.co[i] for v in bm.verts) for i in range(3)]
    high = [max(v.co[i] for v in bm.verts) for i in range(3)]
    verts = [tuple(loc[i] + size[i] * ((v.co[i] - low[i]) / (high[i] - low[i]) - .5)
                   for i in range(3)) for v in bm.verts]
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return mesh(label, verts, faces, role, bevel=False)


def crystal(label, base, tip, radius, role='crystal', depth=None):
    start, end = Vector(base), Vector(tip)
    length = (end - start).length
    verts = [(radius * math.cos(j * math.tau / 6),
              (depth if depth is not None else radius) * math.sin(j * math.tau / 6), z)
             for z in (0, length * .72) for j in range(6)] + [(0, 0, length)]
    faces = [tuple(reversed(range(6)))]
    faces += [(j, (j + 1) % 6, (j + 1) % 6 + 6, j + 6) for j in range(6)]
    faces += [(j + 6, (j + 1) % 6 + 6, 12) for j in range(6)]
    obj = mesh(label, verts, faces, role, bevel=False)
    obj.data.transform(Matrix.Translation(start) @
                       (end - start).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    obj.data.materials.append(MATS['crystal_core'])
    for p in obj.data.polygons:
        if p.index in (2, 8, 9):
            p.material_index = 1
    return obj


def gem(loc, size, role='stardust'):
    x, y, z = size
    return place(mesh('Facet', [(x, 0, 0), (0, y, 0), (-x, 0, 0), (0, -y, 0),
                               (0, 0, z), (0, 0, -z)],
                      [(i, (i + 1) % 4, 4) for i in range(4)] +
                      [((i + 1) % 4, i, 5) for i in range(4)], role, False), loc)


def apply_modifiers(obj):
    activate(obj)
    had_modifiers = bool(obj.modifiers)
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if had_modifiers:
        # A clamped bevel can meet itself on a shallow inlay. Remove its collapsed
        # edges before joining, without welding separate physics fragments.
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
        bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-7)
        bm.to_mesh(obj.data)
        bm.free()
        recalc(obj)


def join(objects, name, origin=(0, 0, 0)):
    assert objects
    for obj in objects:
        apply_modifiers(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    bpy.context.scene.cursor.location = origin
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    recalc(obj)
    return obj


def restore_interfaces(obj, paths, back, front):
    """Reference wood gate technique: bevel perimeter, restore shared cut faces."""
    apply_modifiers(obj)
    corners = [Vector(p) for path in paths for p in path]
    segments = [(Vector(a), Vector(b)) for path in paths for a, b in zip(path, path[1:])]
    for v in obj.data.vertices:
        p = Vector((v.co.x, v.co.z))
        corner = min(corners, key=lambda c: (c - p).length_squared)
        if (corner - p).length <= .043:
            target = corner
        else:
            candidates = []
            for a, b in segments:
                d = b - a
                t = min(1, max(0, (p - a).dot(d) / d.length_squared))
                candidates.append(a + t * d)
            target = min(candidates, key=lambda c: (c - p).length_squared)
        if (target - p).length <= .043:
            v.co.x, v.co.z = target
            if abs(v.co.y - back) <= .023:
                v.co.y = back
            elif abs(v.co.y - front) <= .023:
                v.co.y = front
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-6)
    bmesh.ops.dissolve_limit(bm, angle_limit=1e-5, verts=list(bm.verts),
                           edges=list(bm.edges), use_dissolve_boundaries=False)
    bm.to_mesh(obj.data)
    bm.free()
    recalc(obj)


def fracture(a, b, amplitude=.06, teeth=2):
    a, b = Vector(a), Vector(b)
    d = b - a
    normal = Vector((-d.y, d.x)).normalized()
    return [tuple(a)] + [tuple(a.lerp(b, j / (teeth * 2)) + normal *
                              amplitude * (1 if j % 2 else -1))
                         for j in range(1, teeth * 2)] + [tuple(b)]


def gate_piece(start, pieces):
    obj = join(PARTS[start:], f'Piece_{len(pieces):02d}')
    activate(obj)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    pieces.append(obj)


def gate_stone(stage):
    pieces = []
    for row in range(4):
        levels = (0, .55, 1.55, 2.075, 2.6) if stage == 3 else (0, .65, 1.3, 1.95, 2.6)
        z0, z1 = levels[row], levels[row + 1]
        if stage == 3 and row:
            z0 += .022  # Narrow air seams, no missing section of barrier.
        offsets = [0, -.12 if row % 2 else .13, .12 if row % 2 else -.10, -.08, 0]
        xs = [-2.3 + j * 1.15 + offsets[j] for j in range(5)]
        sides = [[(x, z0), (x, z1)] if j in (0, 4) else
                 fracture((x, z0), (x, z1), .055, 1) for j, x in enumerate(xs)]
        for col in range(4):
            start = len(PARTS)
            left, right = sides[col], sides[col + 1]
            profile = left + list(reversed(right))
            role = ('sandstone', 'sandstone_light', 'sandstone_dark')[(row + col * 2) % 3]
            if stage == 3:
                role = 'marble' if (row + col) % 3 else 'marble_dark'
            obj = prism('Interlocking stone cell', profile, -.14, .14, role)
            paths = []
            if col:
                paths.append(left)
            if col < 3:
                paths.append(right)
            if stage == 1:
                if row:
                    paths.append([left[0], right[0]])
                if row < 3:
                    paths.append([left[-1], right[-1]])
            if paths:
                restore_interfaces(obj, paths, -.14, .14)
            middle = (xs[col] + xs[col + 1]) / 2
            if stage == 3:
                box('Slab front gold inlay', (middle, .151, z0 + .12),
                    (xs[col + 1] - xs[col] - .18, .026, .045), 'gold')
                if (row + col) % 2 == 0:
                    gem((middle + .1, .15, z0 + (z1 - z0) * .60),
                        (.22, .025, min(.19, (z1 - z0) * .30)), 'crystal')
            elif (row + col) % 2 == 0:
                # Flat faceted growth crosses the face beside a mortar joint.
                gem((xs[col + 1] - .14, .14, z0 + .34), (.10, .035, .27), 'crystal')
                gem((xs[col + 1] - .25, .14, z0 + .23), (.07, .030, .16), 'crystal_core')
            gate_piece(start, pieces)
    return pieces


def gate_wood():
    pieces = []
    for col in range(8):
        x0, x1 = -2.3 + col * .575, -2.3 + (col + 1) * .575
        cut = fracture((x0, 1.15 + .12 * (col % 3)),
                       (x1, 1.22 + .12 * (col % 3)), .065, 2)
        edges = [[(x0, 0), (x1, 0)], cut, [(x0, 2.6), (x1, 2.6)]]
        for row in range(2):
            start = len(PARTS)
            lower, upper = edges[row], edges[row + 1]
            obj = prism('Splintered plank', lower + list(reversed(upper)), -.14, .10, 'wood')
            paths = [cut]
            if col:
                paths.append([lower[0], upper[0]])
            if col < 7:
                paths.append([lower[-1], upper[-1]])
            restore_interfaces(obj, paths, -.14, .10)
            # Each strip and nail band belongs to its own physics fragment.
            low = .12 if row == 0 else 1.62
            high = 1.04 if row == 0 else 2.48
            prism('Glassy mirror strip', [(x0 + .16, low), (x0 + .34, low + .05),
                                          (x0 + .39, high), (x0 + .14, high - .08)],
                  .103, .126, 'mirror', False, 'mirror_back')
            for z in ((.40, .90) if row == 0 else (1.85, 2.32)):
                box('Gold nail band', ((x0 + x1) / 2, .133, z), (.575, .05, .09), 'gold')
                for x in (x0 + .10, x1 - .10):
                    cylinder('Broad gold nail', (x, .166, z), .035, .018, 'gold_dark',
                             n=6, rot=(math.pi / 2, 0, 0), bevel=False)
            gate_piece(start, pieces)
    return pieces


def lantern(x, z, back=.0, scale=1):
    """Inverted lantern: broad roof below the glass, mounting stem above."""
    s = scale
    cylinder('Upside down roof', (x, back, z - .13 * s), .16 * s, .12 * s,
             'gold', n=6, top=.09 * s, bevel=False)
    cylinder('Inverted lamp body', (x, back, z + .01 * s), .085 * s, .20 * s,
             'crystal_core', n=6, bevel=False)
    for dx in (-.09, .09):
        box('Lantern gold cage', (x + dx * s, back, z + .01 * s),
            (.024 * s, .09 * s, .22 * s), 'gold', False)
    cylinder('Lantern hanging mount', (x, back, z + .17 * s), .035 * s,
             .12 * s, 'gold', n=6, bevel=False)


def gate_cloud():
    pieces = []
    # Full-depth touching cells prevent holes; overlapping shallow puffs hide the seams.
    for row in range(4):
        for col in range(5):
            start = len(PARTS)
            x0, x1 = -2.3 + col * .92, -2.3 + (col + 1) * .92
            z0, z1 = row * .65, (row + 1) * .65
            box('Cloud cell interior', ((x0 + x1) / 2, -.12, (z0 + z1) / 2),
                (.92, .08, .65), 'cloud', False)
            # Keep every fragment within the 35 cm gate slab.
            cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
            lump('Broad cloud puff', (cx, -.025, cz),
                 (min(1.08, 2 * (2.3 - abs(cx))), .30,
                  min(.80, 2 * min(cz, 2.6 - cz))), 'cloud', True)
            small_x = max(-1.98, min(1.98, cx + random.uniform(-.20, .20)))
            small_z = max(.255, min(2.345, cz + random.uniform(-.20, .20)))
            lump('Irregular overlapping cloud lobe', (small_x, -.04, small_z),
                 (.64, .26, .51), 'cloud_shade' if (col + row * 3) % 7 == 0 else 'cloud', True)
            if row in (1, 3) and col in (1, 3):
                lantern((x0 + x1) / 2, z0 + .24, .09, .52)
            gate_piece(start, pieces)
    return pieces


def splinters(stage):
    result = []
    for i in range(3):
        role = {1: 'sandstone', 2: 'wood', 3: 'marble', 4: 'cloud'}[stage]
        length = .26 + i * .04
        if stage == 4:
            obj = lump('Cloud debris', (0, 0, 1.3), (.12, .04, length), role, True)
        else:
            obj = prism('Hidden debris', [(-.025, 1.3 - length / 2), (.04, 1.23),
                                           (.027, 1.3 + length / 2), (-.04, 1.37)],
                        -.018, .018, role, False)
        obj = join([obj], f'Splinter_{i}')
        activate(obj)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        result.append(obj)
    return result


def floor(role):
    box('Continuous corridor foundation', (0, 0, -.09), (4.6, 10.4, .22), role)


def corridor_lake():
    floor('sand')
    for side in (-1, 1):
        for i, y in enumerate((-4.35, -2.15, 0, 2.15, 4.35)):
            lump('Rose sandstone edge', (side * 2.60, y, .21), (.60, 1.65, .46),
                 'sandstone_light' if i % 2 else 'sandstone')
            if i in (0, 2, 4):
                for k in range(2):
                    crystal('Shore crystal', (side * 2.61, y + k * .18, .35),
                            (side * (2.57 + k * .10), y + .1, .92 - k * .22),
                            .12 - k * .025)
        for y in (-3.2, 1.1, 3.55):
            # Foam silhouette stays underneath the shallow inlet surface.
            lump('Foam inlet rim', (side * 1.96, y, .035), (.68, 1.25, .025), 'water_foam')
            lump('Shallow lake inlet', (side * 2.02, y, .049), (.55, 1.10, .016), 'water')
    for i in range(9):
        y = -4.6 + i * 1.15
        x = math.sin(i * 1.8) * .48
        lump('Flat sandstone stepping stone', (x, y, .047),
             (1.18, .75, .054), 'sandstone_light' if i % 2 else 'marble')
    for side in (-1, 1):
        for y in (-1.05, 4.7):
            lump('Sandy bank pebble', (side * 1.7, y, .065), (.22, .28, .09), 'rock')


def mirror_panel(x, y, width=1.68, height=1.45, cracked=False):
    # Local XZ glass is rotated to face the corridor (+/- X).
    start = len(PARTS)
    base = .36
    profile = [(-width / 2, base), (width / 2, base),
               (width / 2, base + height), (-width / 2, base + height)]
    prism('Mirror panel glass', profile, -.045, .045, 'mirror', False, 'mirror_back')
    for xx in (-width / 2, width / 2):
        box('Gold mirror stile', (xx, 0, base + height / 2), (.09, .15, height + .14), 'gold', False)
    for z in (base, base + height):
        box('Gold mirror rail', (0, 0, z), (width + .12, .15, .09), 'gold', False)
    if cracked:
        for side in (-1, 1):
            tube('Mirror branching crack', [(-.62, side * .047, 1.65),
                                           (-.14, side * .047, 1.24),
                                           (.02, side * .047, .91),
                                           (.55, side * .047, .65)], .018, 'mirror_back', 4)
            tube('Mirror crack fork', [(-.14, side * .048, 1.24),
                                       (.30, side * .048, 1.35),
                                       (.63, side * .048, 1.68)], .016, 'mirror_back', 4)
    for obj in PARTS[start:]:
        place(obj, (x, y, 0), (0, 0, math.pi / 2))


def corridor_town():
    floor('wood')
    for i in range(20):
        y = -4.94 + i * .52
        box('Individual boardwalk plank', (0, y, .042), (4.56, .485, .044), 'wood')
    for x in (-2.18, 2.18):
        box('Boardwalk long gold edge', (x, 0, .055), (.07, 10.4, .06), 'gold')
    for side in (-1, 1):
        for y in (-5, -3, -1, 1, 3, 5):
            box('Wooden mirror post', (side * 2.60, y, 1.0), (.42, .30, 2.0), 'wood')
            box('Gold post cap', (side * 2.60, y, 2.025), (.60, .38, .11), 'gold')
            box('Gold post shoe', (side * 2.60, y, .15), (.60, .38, .23), 'gold_dark', False)
        for i, y in enumerate((-4, -2, 0, 2, 4)):
            mirror_panel(side * 2.6, y, cracked=i % 2 == 1)
        for i in range(9):
            gem((side * random.uniform(2.40, 2.78), -4.65 + i * 1.12,
                 random.uniform(2.15, 2.40)), (.055, .06, .095))


def corridor_bridge():
    floor('marble_dark')
    for row in range(8):
        y = -4.55 + row * 1.30
        for col in range(4):
            x = -1.725 + col * 1.15
            box('Marble bridge tile', (x, y, .043), (1.12, 1.27, .046), 'marble')
        box('Crosswise gold inlay', (0, y + .63, .057), (4.58, .025, .018), 'gold', False)
    for x in (-1.15, 0, 1.15):
        box('Long gold tile inlay', (x, 0, .057), (.025, 10.4, .018), 'gold', False)
    for side in (-1, 1):
        for y in (-4.9, -2.45, 0, 2.45, 4.9):
            box('Low marble bridge post', (side * 2.6, y, .47), (.42, .42, .90), 'marble')
            box('Bridge post gold capital', (side * 2.6, y, .94), (.60, .54, .10), 'gold')
        for z in (.36, .82):
            box('Open gold railing', (side * 2.6, 0, z), (.075, 10.4, .085), 'gold')
        for i, y in enumerate((-3.9, -1.25, 1.25, 3.9)):
            lump('Floating marble underside fragment', (side * 2.56, y, -.40 - .09 * (i % 2)),
                 (.54, .72, .28), 'marble_dark' if i % 2 else 'rock')
            gem((side * 2.65, y + .25, -.83), (.10, .12, .15), 'crystal')


def inverted_column(x, y):
    box('Inverted column broad top', (x, y, 1.94), (.42, .48, .16), 'marble', False)
    cylinder('Downward column shaft', (x, y, 1.60), .12, .55, 'marble', n=8, bevel=False)
    cylinder('Downward column gold end', (x, y, 1.31), .17, .10, 'gold', n=8, bevel=False)


def corridor_sky():
    floor('cloud')
    for row in range(10):
        y = -4.68 + row * 1.04
        for col in range(3):
            x = -1.53 + col * 1.53
            lump('Soft shallow cloud tile', (x, y, .045), (1.54, 1.04, .05), 'cloud', True)
    for side in (-1, 1):
        for i, y in enumerate((-4.5, -2.25, 0, 2.25, 4.5)):
            lump('Low cloud bank', (side * 2.6, y, .48), (.60, 1.40, 1.0), 'cloud_shade', True)
            lump('Cloud bank crest', (side * 2.6, y, 1.98), (.60, 1.40, .54), 'cloud', True)
            for yy in (-.46, .46):
                inverted_column(side * 2.6, y + yy)
            # A shallow inverted U, hanging between the columns.
            tube('Inverted gold arch', [(side * 2.6, y - .46, 1.82),
                                       (side * 2.6, y - .33, 1.47),
                                       (side * 2.6, y, 1.35),
                                       (side * 2.6, y + .33, 1.47),
                                       (side * 2.6, y + .46, 1.82)], .055, 'gold')


def frame_path(label, path, width, depth, role):
    """Square swept molding; no bottom sill across a portal's clear opening."""
    return tube(label, [(x, 0, z) for x, z in path], width, role, n=4)


def shoulder_arch():
    # Vertical below 2.9; all curvature stays above the clearance rectangle.
    return [(-2.59, .2), (-2.59, 2.96), (-2.37, 3.35),
            (-1.60, 3.63), (0, 3.77), (1.60, 3.63),
            (2.37, 3.35), (2.59, 2.96), (2.59, .2)]


def portal_crystal():
    for side in (-1, 1):
        box('Rose sandstone spire footing', (side * 2.85, 0, .18), (1.10, 1.15, .36), 'sandstone')
        # Custom bent six-sided spires stay outside the opening below 2.9 m.
        levels = [(side * 2.83, .30, .44, .43), (side * 2.83, 2.88, .40, .32),
                  (side * 1.52, 3.44, .30, .24)]
        verts = [(x + rx * math.cos(j * math.tau / 6), ry * math.sin(j * math.tau / 6), z)
                 for x, z, rx, ry in levels for j in range(6)]
        verts.append((side * .04, 0, 4.06))
        faces = [tuple(reversed(range(6)))]
        faces += [(k * 6 + j, k * 6 + (j + 1) % 6, (k + 1) * 6 + (j + 1) % 6,
                   (k + 1) * 6 + j) for k in range(2) for j in range(6)]
        faces += [(12 + j, 12 + (j + 1) % 6, 18) for j in range(6)]
        obj = mesh('Leaning faceted crystal spire', verts, faces, 'crystal', False)
        obj.data.materials.append(MATS['crystal_core'])
        for p in obj.data.polygons:
            if p.index % 6 == 2:
                p.material_index = 1
        crystal('Short outward crystal root', (side * 3.08, .10, .3),
                (side * 3.20, .10, 1.30), .15)
        cylinder('Gold spire socket', (side * 2.83, 0, .43), .48, .16, 'gold', n=6)


def scroll(x, z, side=1, scale=1, y=.40):
    points = [(x + side * scale * a, y, z + scale * b) for a, b in
              ((0, 0), (.10, .24), (.32, .32), (.50, .22), (.48, .04), (.33, -.02), (.25, .09))]
    tube('Broad gold scroll', points, .045 * scale, 'gold')


def portal_mirror():
    path = shoulder_arch()
    frame_path('Gold dark outer mirror arch', path, .29, .7, 'gold_dark')
    # Layered moldings form an ornate mirror FRAME, leaving the aperture empty.
    for y in (-.26, .26):
        tube('Outer gold arch molding', [(x, y, z) for x, z in path], .105, 'gold')
        tube('Inner gold arch molding', [(x * .96, y, z) for x, z in path], .060, 'gold')
    for side in (-1, 1):
        box('Mirror arch pedestal', (side * 2.87, 0, .17), (1.0, 1.1, .34), 'gold_dark')
        box('Mirror arch pedestal lip', (side * 2.87, 0, .36), (1.02, 1.13, .12), 'gold')
        for z in (.80, 1.65, 2.52):
            gem((side * 2.62, .32, z), (.12, .07, .17), 'gold')
        scroll(side * 2.62, 2.98, side, .85)
        scroll(side * .80, 3.78, side, .75)
    gem((0, 0, 3.98), (.23, .16, .21), 'gold')


def portal_marble():
    for side in (-1, 1):
        box('Marble arch plinth', (side * 2.84, 0, .18), (1.08, 1.15, .36), 'marble')
        box('Gold plinth molding', (side * 2.84, 0, .39), (1.02, 1.1, .09), 'gold')
        top = 2.96 if side < 0 else 2.34
        box('Broken marble arch pillar', (side * 2.79, 0, (.44 + top) / 2),
            (.80, .83, top - .44), 'marble')
        box('Pillar inset shadow', (side * 2.79, .425, 1.37), (.43, .025, 1.55), 'marble_dark')
        box('Pillar gold inset', (side * 2.79, .445, 1.37), (.055, .035, 1.53), 'gold')
    # Separate closed voussoirs, including a visibly dislocated right shoulder.
    spans = [(-2.80, -1.64, 3.00, 3.57), (-1.64, -.55, 3.44, 3.97),
             (-.55, .56, 3.57, 4.08), (.56, 1.65, 3.44, 3.98)]
    for a, b, low, high in spans:
        prism('Broken marble crown block', [(a, low), (b, low), (b - .06, high), (a + .07, high)],
              -.42, .42, 'marble')
        box('Crown gold front strip', ((a + b) / 2, .435, high - .09), (b - a - .14, .05, .055), 'gold')
    for loc, size, angle in [((2.23, 0, 3.48), (.78, .76, .52), -.18),
                              ((2.86, 0, 2.91), (.62, .69, .36), .19),
                              ((3.10, .05, 2.51), (.39, .48, .25), -.25)]:
        box('Suspended crumbling marble block', loc, size, 'marble', rot=(0, angle, 0))
        box('Floating gold trim fragment', (loc[0], .42, loc[2] + .11),
            (size[0] * .8, .05, .05), 'gold', rot=(0, angle, 0))
    for x, z in ((2.38, 2.72), (3.17, 3.34), (1.89, 3.05)):
        gem((x, .1, z), (.08, .12, .12), 'crystal')


def weather_vane():
    # Pivot and arrow are BELOW their top mount: recognizably upside down.
    tube('Weather vane inverted shaft', [(0, 0, 4.18), (0, 0, 3.69)], .055, 'gold')
    tube('Weather vane arrow stem', [(-.64, 0, 3.79), (.66, 0, 3.79)], .045, 'gold')
    prism('Gold arrowhead', [(.66, 3.63), (.98, 3.79), (.66, 3.96)], -.08, .08, 'gold')
    prism('Inverted vane swallow tail', [(-.64, 3.65), (-.96, 3.59), (-.84, 3.79),
                                          (-.96, 3.99), (-.64, 3.93)], -.07, .07, 'gold')
    gem((0, 0, 3.62), (.10, .09, .12), 'gold_dark')


def portal_cloud():
    path = [(-2.75, .25), (-2.75, 2.95), (-2.30, 3.35),
            (0, 3.53), (2.30, 3.35), (2.75, 2.95), (2.75, .25)]
    tube('Cloud wrapped gold arch spine', [(x, 0, z) for x, z in path], .22, 'gold')
    for side in (-1, 1):
        for i in range(5):
            z = .37 + i * .61
            lump('Puffy portal pillar wrap', (side * 2.85, .02, z),
                 (1.02, 1.04, .74), 'cloud' if i % 2 else 'cloud_shade', True)
    for i in range(7):
        x = -2.22 + i * .74
        z = 3.45 + .12 * (1 - abs(x) / 2.22)
        lump('Cloud arch crown puff', (x, 0, z), (.87, 1.04, .59), 'cloud', True)
    weather_vane()


def reward_door():
    frame_start = len(PARTS)
    for side in (-1, 1):
        x = side * 1.33
        box('Door gold foot', (x, 0, .10), (.34, .50, .20), 'gold')
        box('Ornate gold door jamb', (x, 0, 1.27), (.34, .42, 2.36), 'gold_dark')
        for dx in (-.12, .12):
            box('Door jamb molding', (x + dx, .225, 1.30), (.065, .05, 2.34), 'gold')
        for z in (.54, 1.32, 2.12):
            gem((x, .24, z), (.10, .035, .12), 'gold')
    box('Door ornate gold lintel', (0, 0, 2.48), (3.0, .46, .24), 'gold')
    box('Door lintel recessed stripe', (0, .24, 2.48), (2.67, .025, .065), 'gold_dark')
    for side in (-1, 1):
        scroll(side * .20, 2.56, side, .65, .18)
    crystal('Reward crown crystal', (0, 0, 2.57), (0, 0, 3.0), .19)
    box('Door shallow threshold', (0, 0, .055), (2.36, .48, .11), 'gold')
    result = [join(PARTS[frame_start:], 'Frame')]
    for side, name in ((-1, 'Leaf_L'), (1, 'Leaf_R')):
        start = len(PARTS)
        x = side * .565
        box('Mirror leaf solid backing', (x, -.025, 1.33), (1.10, .13, 2.12), 'mirror_back')
        box('Reward reflective face', (x, .05, 1.33), (1.04, .025, 2.04), 'mirror', False)
        for dx in (-.51, .51):
            box('Leaf gold edge', (x + dx, .08, 1.33), (.075, .08, 2.12), 'gold')
        for z in (.30, 2.36):
            box('Leaf gold rail', (x, .08, z), (1.10, .08, .075), 'gold')
        # Broad diamond ornament, thin enough to preserve the mirror face.
        tube('Leaf gold diamond', [(x, .102, .56), (x + .32, .102, 1.33),
                                  (x, .102, 2.10), (x - .32, .102, 1.33),
                                  (x, .102, .56)], .023, 'gold', 4)
        cylinder('Gold mirror door handle', (side * .16, .14, 1.28), .065, .08,
                 'gold', n=8, rot=(math.pi / 2, 0, 0), bevel=False)
        for z in (.63, 2.02):
            cylinder('Outer gold hinge', (side * 1.115, 0, z), .065, .22, 'gold', n=8)
        obj = join(PARTS[start:], name, (side * 1.115, 0, 0))
        obj['hinge_axis'] = 'local Z'
        result.append(obj)
    return result


def world_vertices(objects):
    bpy.context.view_layer.update()
    return [obj.matrix_world @ v.co for obj in objects for v in obj.data.vertices]


def bounds(objects):
    verts = world_vertices(objects)
    return (Vector(tuple(min(v[i] for v in verts) for i in range(3))),
            Vector(tuple(max(v[i] for v in verts) for i in range(3))))


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def validate(name, objects):
    low, high = bounds(objects)
    dims = high - low
    for obj in objects:
        recalc(obj)
        assert obj.type == 'MESH' and obj.parent is None
        assert all(abs(s - 1) < 1e-6 for s in obj.scale)
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        for mat in obj.data.materials:
            assert mat.name in PALETTE
            assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
            assert tuple(mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value) == (.5, .5, .5, 1)
            assert all(node.type != 'TEX_IMAGE' for node in mat.node_tree.nodes)
    if name.startswith('Gate'):
        count = len(objects) - 3
        assert 10 <= count <= 24
        assert [o.name for o in objects] == [f'Piece_{i:02d}' for i in range(count)] + [f'Splinter_{i}' for i in range(3)]
        assert abs(low.x + 2.3) < 1e-4 and abs(high.x - 2.3) < 1e-4, (name, low, high)
        assert abs(low.z) < 1e-4 and 2.4 <= high.z <= 2.7
        assert dims.y <= .3501 and abs((high.y + low.y) / 2) < 1e-5, (name, dims)
        for obj in objects:
            verts = [v.co for v in obj.data.vertices]
            centre = Vector(tuple((min(v[i] for v in verts) + max(v[i] for v in verts)) / 2 for i in range(3)))
            assert centre.length < 1e-5, (obj.name, centre)
        for obj in objects[-3:]:
            assert (obj.location - Vector((0, 0, 1.3))).length < 1e-5
            assert .2 <= max(obj.dimensions) <= .45
        budget = 6000
    elif name.startswith('Corridor'):
        assert len(objects) == 1 and objects[0].location.length < 1e-6
        assert abs(low.y + 5.2) < 1e-4 and abs(high.y - 5.2) < 1e-4, (name, low, high)
        assert low.x >= -2.9001 and high.x <= 2.9001 and high.z <= 2.5
        if name == 'Corridor_S3':
            assert high.z <= 1.1
        # The first component is the exact 4.6 x 10.4 foundation in every stage.
        assert any(abs(v.z - .02) < 1e-5 for v in world_vertices(objects))
        budget = 7000
    elif name.startswith('Portal'):
        assert len(objects) == 1 and objects[0].location.length < 1e-6
        assert low.x >= -3.4001 and high.x <= 3.4001
        assert low.y >= -.6001 and high.y <= .6001
        assert abs(low.z) < 1e-4 and 3.4 <= high.z <= 4.2
        for v in world_vertices(objects):
            assert not (-2.2999 < v.x < 2.2999 and v.z < 2.8999), (name, 'Obstructed opening', v)
        # Test triangulated faces, too: no beam may span across the clear aperture.
        for obj in objects:
            obj.data.calc_loop_triangles()
            for tri in obj.data.loop_triangles:
                coords = [obj.matrix_world @ obj.data.vertices[i].co for i in tri.vertices]
                if max(p.z for p in coords) < 2.9:
                    assert min(p.x for p in coords) >= 2.2999 or max(p.x for p in coords) <= -2.2999
        budget = 4000
    else:
        assert [obj.name for obj in objects] == ['Frame', 'Leaf_L', 'Leaf_R']
        assert abs(dims.x - 3) < 1e-4 and abs(dims.z - 3) < 1e-4 and abs(low.z) < 1e-4
        assert .45 <= dims.y <= .55
        for obj, side in zip(objects[1:], (-1, 1)):
            assert abs(obj.location.x - side * 1.115) < 1e-5
            assert abs(obj.location.y) < 1e-6 and abs(obj.location.z) < 1e-6
        budget = 5000
    count = sum(triangles(obj) for obj in objects)
    assert count <= budget, (name, count, budget)
    return count, tuple(round(v, 4) for v in dims)


def export(name, objects):
    count, dims = validate(name, objects)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        obj['chapter'], obj['front'] = 'Mirage Desert / Mirror World', '+Y'
    bpy.context.view_layer.objects.active = objects[0]
    path = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert path.is_file() and path.stat().st_size > 1024
    print(f'EXPORTED {name} {len(objects)} {count} {dims}', flush=True)
    ASSETS.append((name, objects, path, hashlib.sha256(path.read_bytes()).hexdigest()))
    # Exact flat physics names are reused in the next FBX; review names are unique.
    for obj in objects:
        obj.name = name + '__' + obj.name


def preview():
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
    shade.show_shadows, shade.show_specular_highlight = False, False
    shade.show_cavity, shade.cavity_type = True, 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.05, .85
    shade.background_type = 'WORLD'
    shade.background_color = (.055, .045, .085)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.055, .045, .085)
    camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 52
    target = Vector((-.3, 0, 1.2))
    elevation = math.radians(55)
    camera.location = target + Vector((0, 60 * math.cos(elevation), 60 * math.sin(elevation)))
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    helpers = [camera]
    matrices = {obj: obj.matrix_world.copy() for _, objects, _, _ in ASSETS for obj in objects}

    def caption(body, loc, size=.34, align='CENTER', role='crystal_core'):
        data = bpy.data.curves.new('Review typography', 'FONT')
        data.body, data.align_x, data.size = body, align, size
        data.materials.append(MATS[role])
        obj = bpy.data.objects.new('Review caption', data)
        scene.collection.objects.link(obj)
        obj.location, obj.rotation_euler = (-loc[0], loc[1], loc[2]), camera.rotation_euler
        helpers.append(obj)

    try:
        for role, mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role], 1)
        for name, objects, _, _ in ASSETS:
            if name == 'RewardDoor':
                transform = Matrix.Translation(Vector((-18.3, 0, 0)))
            else:
                stage = int(name[-1])
                y = (stage - 2.5) * 8
                x = -13 if name.startswith('Gate') else (-2 if name.startswith('Corridor') else 9)
                # Review-only quarter-turn gives the long corridors room across each cell.
                rot = Matrix.Rotation(math.pi / 2 if name.startswith('Corridor') else 0, 4, 'Z')
                transform = Matrix.Translation(Vector((-x, y, 0))) @ rot
                caption(name, (x, y + 3.65, -.08), .33)
            for obj in objects:
                obj.matrix_world = transform @ matrices[obj]
        for stage, title in enumerate(('MIRROR LAKE', 'MIRRORED TOWN', 'FLOATING ISLANDS', 'UPSIDE-DOWN SKY'), 1):
            caption(f'0{stage} / {title}', (-17.7, (stage - 2.5) * 8 - 2.5, 0), .32, 'LEFT', 'stardust')
        caption('CHAPTER 03 / MIRAGE DESERT', (-17.7, -17.0, 0), .63, 'LEFT')
        caption('MIRROR WORLD  /  STAGE CONNECTION KIT', (-17.7, -15.8, 0), .29, 'LEFT', 'stardust')
        caption('REWARD', (18.3, -3.2, 0), .35, role='stardust')
        caption('MIRROR DOOR', (18.3, 2.4, -.1), .31)
        caption('HINGED LEAVES', (18.3, 3.0, -.1), .24, role='stardust')
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        scene.camera = None
        for obj, matrix in matrices.items():
            obj.matrix_world = matrix
        for obj in helpers:
            bpy.data.objects.remove(obj, do_unlink=True)
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)
    for _, _, path, digest in ASSETS:
        assert hashlib.sha256(path.read_bytes()).hexdigest() == digest, 'Preview changed export'


def main():
    global PARTS
    setup()
    corridors = (corridor_lake, corridor_town, corridor_bridge, corridor_sky)
    portals = (portal_crystal, portal_mirror, portal_marble, portal_cloud)
    for stage in range(1, 5):
        random.seed(330071 + stage * 7919)
        PARTS = []
        pieces = gate_stone(stage) if stage in (1, 3) else (gate_wood() if stage == 2 else gate_cloud())
        low, high = bounds(pieces)
        for obj in pieces:
            obj.location.y -= (low.y + high.y) / 2
        export(f'Gate_S{stage}', pieces + splinters(stage))
        PARTS = []
        corridors[stage - 1]()
        export(f'Corridor_S{stage}', [join(PARTS, f'Corridor_S{stage}')])
        PARTS = []
        portals[stage - 1]()
        export(f'Portal_S{stage}', [join(PARTS, f'Portal_S{stage}')])
    PARTS = []
    random.seed(33971)
    export('RewardDoor', reward_door())
    assert len(ASSETS) == 13
    assert set(bpy.context.scene.objects) == {o for _, objects, _, _ in ASSETS for o in objects}
    preview()
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
