# 거울 세계 전투 소품 10종을 만들고 FBX와 검토용 미리보기를 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_props.py
"""Stock Blender 5.x, deterministic metre-scale meshes; no external dependencies."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_props_preview.png'
ROLES = (
    'sandstone', 'sandstone_light', 'sandstone_dark', 'rock', 'rock_dark',
    'sand', 'marble', 'marble_dark', 'gold', 'gold_dark', 'mirror',
    'mirror_back', 'crystal', 'crystal_core', 'water', 'water_foam', 'cloud',
    'cloud_shade', 'cloth_rose', 'stardust', 'wood', 'roof_tile', 'window_lit',
    'glaze_teal', 'coral', 'ivory', 'bronze', 'rose_gold', 'gem_rose',
    'gem_amber', 'crystal_rose', 'tile_cream',
)
HARD = {'sandstone', 'sandstone_light', 'sandstone_dark', 'rock', 'rock_dark',
        'marble', 'marble_dark', 'gold', 'gold_dark', 'ivory', 'bronze',
        'rose_gold', 'glaze_teal', 'coral', 'tile_cream'}
PALETTE = {
    'sandstone': (.67, .32, .38), 'sandstone_light': (.94, .62, .59),
    'sandstone_dark': (.35, .14, .23), 'rock': (.45, .31, .40),
    'rock_dark': (.22, .17, .24), 'sand': (.83, .53, .39),
    'marble': (.91, .87, .78), 'marble_dark': (.43, .43, .51),
    'gold': (.94, .61, .18), 'gold_dark': (.43, .25, .07),
    'mirror': (.47, .71, .83), 'mirror_back': (.23, .23, .34),
    'crystal': (.71, .86, .96), 'crystal_core': (1.0, .97, .85),
    'water': (.15, .53, .58), 'water_foam': (.73, .97, .92),
    'cloth_rose': (.64, .18, .33), 'stardust': (1.0, .81, .44),
    'glaze_teal': (.05, .52, .51), 'coral': (.94, .34, .29),
    'ivory': (.99, .92, .70), 'bronze': (.49, .28, .14),
    'rose_gold': (.89, .48, .39), 'gem_rose': (.94, .13, .43),
    'gem_amber': (1.0, .44, .04), 'crystal_rose': (.86, .43, .73),
    'tile_cream': (.93, .76, .58),
}
MATS = {}
PARTS = []


def setup_materials():
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


def mesh(label, vertices, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel and role in HARD:
        mod = obj.modifiers.new('Soft worn edges', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method = 'ANGLE'
        # Preserve broad faceting; soften the silhouette and hard right angles.
        mod.angle_limit = math.radians(70)
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0)):
    x, y, z = (v / 2 for v in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return place(mesh(label, verts, faces, role), loc, rot)


def cylinder(label, loc, radius, depth, role, n=16, top=None,
             rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n),
              r * math.sin(j * math.tau / n), z)
             for r, z in ((radius, -depth / 2), (top, depth / 2))
             for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def lathe(label, profile, role, n=24, loc=(0, 0, 0), rot=(0, 0, 0)):
    """Revolve a closed radial/Z section: bowls and bands are genuinely hollow."""
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in profile for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % len(profile)) * n + (j + 1) % n,
              ((k + 1) % len(profile)) * n + j)
             for k in range(len(profile)) for j in range(n)]
    return place(mesh(label, verts, faces, role), loc, rot)


def ring(label, radius, width, z, height, role, n=24, loc=(0, 0, 0), rot=(0, 0, 0)):
    return lathe(label, [(radius - width, z), (radius, z),
                         (radius, z + height), (radius - width, z + height)],
                 role, n, loc, rot)


def tube(label, points, radius, role, n=6):
    verts = []
    previous = None
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(i - 1, 0)])).normalized()
        ref = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        side = ref.cross(tangent).normalized()
        if previous is not None:
            side = (previous - tangent * previous.dot(tangent)).normalized()
        other = tangent.cross(side)
        previous = side
        for j in range(n):
            a = math.tau * j / n
            verts.append(tuple(Vector(point) + radius *
                               (side * math.cos(a) + other * math.sin(a))))
    faces = [tuple(reversed(range(n)))]
    faces += [(i * n + j, i * n + (j + 1) % n,
               (i + 1) * n + (j + 1) % n, (i + 1) * n + j)
              for i in range(len(points) - 1) for j in range(n)]
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, verts, faces, role)


def lump(label, loc, size, role, facet_roles=False):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        v.co *= random.uniform(.89, 1.10)
    # Exact local bounds, with a flat floor instead of a buried origin.
    lows = [min(v.co[i] for v in bm.verts) for i in range(3)]
    highs = [max(v.co[i] for v in bm.verts) for i in range(3)]
    for v in bm.verts:
        v.co = Vector(tuple(loc[i] + size[i] *
                            ((v.co[i] - lows[i]) / (highs[i] - lows[i]) - .5)
                            for i in range(3)))
    obj = mesh(label, [tuple(v.co) for v in bm.verts],
               [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    if facet_roles:
        obj.data.materials.append(MATS['sandstone_dark'])
        obj.data.materials.append(MATS['sandstone_light'])
        for poly in obj.data.polygons:
            poly.material_index = 2 if poly.normal.z > .35 else (1 if poly.normal.z < -.15 else 0)
    return obj


def crystal(label, base, radius, length, role='crystal', lean=0, azimuth=0):
    # Six rectangular prism faces, six triangular pointed facets.
    verts = [(radius * math.cos(j * math.tau / 6),
              radius * math.sin(j * math.tau / 6), z)
             for z in (0, length * .76) for j in range(6)]
    verts.append((0, 0, length))
    faces = [tuple(reversed(range(6)))]
    faces += [(j, (j + 1) % 6, (j + 1) % 6 + 6, j + 6) for j in range(6)]
    faces += [(j + 6, (j + 1) % 6 + 6, 12) for j in range(6)]
    obj = mesh(label, verts, faces, role)
    obj.data.transform(Matrix.Translation(Vector(base)) @
                       Matrix.Rotation(azimuth, 4, 'Z') @
                       Matrix.Rotation(math.radians(lean), 4, 'Y'))
    if base[2] == 0:
        # Every scattered splinter touches the floor despite its individual lean.
        bottom = min(v.co.z for v in obj.data.vertices)
        for v in obj.data.vertices:
            v.co.z -= bottom
    return obj


def octahedron(loc, radius, role='stardust'):
    verts = [(radius, 0, 0), (0, radius, 0), (-radius, 0, 0),
             (0, -radius, 0), (0, 0, radius * 1.5), (0, 0, -radius * 1.5)]
    faces = [(i, (i + 1) % 4, 4) for i in range(4)]
    faces += [((i + 1) % 4, i, 5) for i in range(4)]
    return place(mesh('Faceted jewel', verts, faces, role), loc)


def extrude_y(label, profile, back, front, role, back_role=None, bevel=True):
    n = len(profile)
    verts = [(x, y, z) for y in (back, front) for x, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    obj = mesh(label, verts, faces, role, bevel)
    if back_role:
        obj.data.materials.append(MATS[back_role])
        obj.data.polygons[0].material_index = 1
        for poly in list(obj.data.polygons)[2:]:
            poly.material_index = 1
    return obj


def arch(width, bottom, spring, n=12):
    r = width / 2
    return [(-r, bottom), (r, bottom)] + [
        (r * math.cos(i * math.pi / n), spring + r * math.sin(i * math.pi / n))
        for i in range(n + 1)]


def arch_frame(label, inner, outer, back, front, role='gold'):
    n = len(inner)
    verts = [(x, y, z) for y, profile in
             ((back, outer), (front, outer), (front, inner), (back, inner))
             for x, z in profile]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    return mesh(label, verts, faces, role)


def medallion(label, x, y, z, radius):
    cylinder(label, (x, y, z), radius, .09, 'gold', n=12,
             rot=(math.pi / 2, 0, 0))
    cylinder(label + ' raised boss', (x, y + .05, z), radius * .53, .035,
             'gold_dark', n=8, rot=(math.pi / 2, 0, 0))


def scroll(x, y, z, side=1, scale=1, role='gold'):
    points = [(0, 0), (.13, .05), (.22, .17), (.20, .29),
              (.10, .32), (.035, .25), (.07, .18), (.13, .19)]
    return tube('Cast gold scroll curl',
                [(x + side * a * scale, y, z + b * scale) for a, b in points],
                .035 * scale, role)


def memory_crystal():
    cylinder('Octagonal sandstone foot', (0, 0, .045), .53, .09,
             'sandstone_dark', n=8)
    cylinder('Carved tapered octagonal pedestal', (0, 0, .15), .49, .18,
             'sandstone', n=8, top=.44)
    cylinder('Pedestal upper molding', (0, 0, .255), .50, .07,
             'sandstone_light', n=8)
    ring('Octagonal gold rim', .51, .055, .28, .05, 'gold', n=8)
    for i in range(8):
        a = math.tau * i / 8 + math.pi / 8
        x, y = .435 * math.cos(a), .435 * math.sin(a)
        box('Recessed dark pedestal panel', (x, y, .16), (.18, .045, .10),
            'sandstone_dark', rot=(0, 0, a - math.pi / 2))
        box('Teal octagonal mosaic inset', (x, y, .18), (.105, .055, .065),
            'glaze_teal', rot=(0, 0, a - math.pi / 2))
        box('Radial ivory crown tile', (.40 * math.cos(a), .40 * math.sin(a), .30),
            (.10, .09, .04), 'ivory', rot=(0, 0, a))
    cylinder('Bronze root socket', (0, 0, .35), .30, .13, 'bronze', n=12)
    ring('Gold root collar lip', .315, .05, .40, .05, 'gold', n=12)
    crystal('Tall central hexagonal prism', (0, 0, .39), .17, 1.29)
    # Exposed heart tips and broad side inlays read through the opaque cel shader.
    crystal('Exposed white heart crown', (0, 0, 1.35), .075, .35, 'crystal_core')
    crystal('Front white heart facet', (0, .145, .55), .063, .77, 'crystal_core')
    for i, (a, lean, length) in enumerate(((35, 25, .86), (125, 28, .69),
                                          (215, 22, 1.03), (305, 30, .75))):
        angle = math.radians(a)
        base = (.16 * math.cos(angle), .16 * math.sin(angle), .39)
        crystal('Outward leaning hexagonal prism',
                base, .12, length, role='crystal_rose' if i % 2 else 'crystal',
                lean=lean, azimuth=angle)
        crystal('Exposed satellite heart',
                (base[0], base[1] + .09, base[2] + .08), .047, length * .64,
                'crystal_core', lean=lean, azimuth=angle)
    ring('Floating gold orbital ring at 1.5 metres', .36, .045, 1.48, .04,
         'gold', n=16)
    for loc, r in (((-.48, .10, 1.32), .042), ((.43, -.25, 1.48), .047),
                   ((-.20, -.46, 1.63), .035), ((.31, .46, 1.21), .04)):
        octahedron(loc, r)


def fountain():
    cylinder('Basin stepped foundation', (0, 0, .045), 1.06, .09, 'marble_dark', n=24)
    cylinder('Basin floor', (0, 0, .12), 1.045, .10, 'ivory', n=24)
    lathe('Sculpted open marble basin', [(.84, .10), (1.02, .10), (1.08, .19),
                                       (1.07, .43), (1.10, .49), (1.10, .55),
                                       (.88, .55), (.84, .38)], 'marble', n=24)
    ring('Lower bronze basin molding', 1.08, .055, .12, .065, 'bronze', n=24)
    ring('Outer gold rim belt', 1.10, .06, .50, .05, 'gold', n=24)
    # Recessed scallop-shaped panels sit behind raised ivory arch moldings.
    for i in range(12):
        a = math.tau * i / 12
        start = len(PARTS)
        panel = arch(.24, .20, .30, n=6)
        extrude_y('Dark carved basin scallop recess', panel, 1.015, 1.055,
                  'marble_dark')
        tube('Raised scallop arch relief',
             [(x, 1.075, z) for x, z in panel[1:]], .025, 'ivory', n=5)
        box('Teal basin tile', (0, 1.066, .445), (.23, .055, .07), 'glaze_teal')
        for obj in PARTS[start:]:
            place(obj, rot=(0, 0, a))
    cylinder('Still lower water disc', (0, 0, .43), .85, .04, 'water', n=32)
    ring('Broad foam ripple on basin', .67, .04, .455, .025, 'water_foam', n=24)
    cylinder('Pedestal ivory foot', (0, 0, .51), .29, .12, 'ivory', n=12)
    cylinder('Fluted pedestal shadow core', (0, 0, .83), .17, .59,
             'marble_dark', n=16)
    for i in range(8):
        a = math.tau * i / 8
        cylinder('Separate broad marble flute', (.17 * math.cos(a), .17 * math.sin(a), .82),
                 .055, .53, 'marble', n=6)
        box('Coral and ivory mosaic diamond', (.23 * math.cos(a), .23 * math.sin(a), .65),
            (.09, .04, .09), 'coral' if i % 2 else 'ivory',
            rot=(0, math.pi / 4, a - math.pi / 2))
    ring('Pedestal gold neck molding', .25, .07, 1.05, .07, 'gold', n=16)
    lathe('Hollow upper ivory bowl', [(.17, 1.08), (.35, 1.16), (.43, 1.26),
                                    (.43, 1.32), (.35, 1.32), (.14, 1.15)],
          'ivory', n=16)
    ring('Upper bowl teal band', .424, .055, 1.245, .055, 'glaze_teal', n=16)
    ring('Upper bowl gold lip', .44, .05, 1.31, .045, 'gold', n=16)
    cylinder('Water in upper bowl', (0, 0, 1.28), .35, .04, 'water', n=24)
    medallion('Gold sun disc fountain finial', 0, .025, 1.72, .13)
    for i in range(8):
        a = math.tau * i / 8
        tube('Sun disc chunky ray', [(.15 * math.cos(a), .025, 1.72 + .15 * math.sin(a)),
                                     (.19 * math.cos(a), .025, 1.72 + .19 * math.sin(a))],
             .024, 'gold', n=5)
    tube('Fat frozen central water jet', [(0, -.07, 1.28), (0, -.07, 1.48),
                                         (0, -.07, 1.73)], .065, 'water', n=7)
    for i in range(4):
        a = math.tau * i / 4 + math.pi / 4
        arc = [(.07, 1.73), (.20, 1.84), (.39, 1.74), (.54, 1.48),
               (.64, 1.14), (.72, .77), (.76, .49)]
        tube('Drooping cartoon water arc',
             [(r * math.cos(a), r * math.sin(a), z) for r, z in arc],
             .06, 'water', n=6)
        x, y = .76 * math.cos(a), .76 * math.sin(a)
        crown = []
        for j in range(13):
            b = math.tau * j / 12
            crown.append((x + .125 * math.cos(b), y + .125 * math.sin(b),
                          .49 + (.14 if j % 2 else 0)))
        tube('Zigzag water foam splash crown', crown, .025, 'water_foam', n=5)


def stand_mirror():
    box('Carved marble plinth foot', (0, 0, .045), (1.5, .6, .09), 'marble')
    box('Recessed ivory plinth course', (0, 0, .14), (1.36, .48, .12), 'ivory')
    box('Gold plinth separator', (0, 0, .205), (1.41, .53, .04), 'gold')
    box('Marble plinth crown molding', (0, 0, .26), (1.47, .57, .08), 'marble')
    for x in (-.48, -.24, 0, .24, .48):
        box('Recessed plinth teal panel', (x, .25, .14), (.15, .045, .065), 'glaze_teal')
    inner = arch(1.08, .43, 1.74)
    arch_frame('Wide gold shadow frame profile', inner, arch(1.42, .31, 1.74),
               -.11, .035, 'gold_dark')
    arch_frame('Outer stepped gold frame profile', arch(1.28, .35, 1.74),
               arch(1.39, .31, 1.74), -.04, .09)
    arch_frame('Middle stepped gold frame profile', arch(1.16, .40, 1.74),
               arch(1.29, .35, 1.74), .015, .13)
    arch_frame('Rose gold inner bead profile', inner, arch(1.17, .40, 1.74),
               .055, .16, 'rose_gold')
    extrude_y('Recessed mirror with dull backing', inner, .04, .09,
              'mirror', 'mirror_back')
    for side in (-1, 1):
        tube('Gold dark claw foot', [(side * .47, -.19, .25),
                                    (side * .50, -.16, .39),
                                    (side * .49, .04, .42),
                                    (side * .54, .24, .28)], .07, 'gold_dark', n=6)
        scroll(side * .48, .15, 1.74, side, .8)
        scroll(side * .46, .15, .45, -side, .7)
        for z in (.77, 1.18, 1.59):
            octahedron((side * .65, .14, z), .044, 'ivory')
    medallion('Round crown medallion', 0, .11, 2.39, .15)
    octahedron((0, .185, 2.40), .085, 'gem_rose')
    for side in (-1, 1):
        octahedron((side * .22, .14, 2.33), .055, 'gem_amber')
        scroll(side * .10, .12, 2.20, side, .75)
    for i in range(5):
        a = math.pi * i / 4
        tube('Crown sun ray', [(.16 * math.cos(a), .10, 2.39 + .16 * math.sin(a)),
                               (.21 * math.cos(a), .10, 2.39 + .21 * math.sin(a))],
             .023, 'gold', n=5)


def cloth_strip(label, centres, widths, depth=.035):
    """Closed folded cloth ribbon; crosswise pleats stay broad from a high camera."""
    across = 5
    verts = []
    for offset in (-depth / 2, depth / 2):
        for (x, y, z), width in zip(centres, widths):
            for j in range(across):
                t = j / (across - 1)
                verts.append((x + (t - .5) * width,
                              y + .04 * math.cos(t * math.tau * 2) + offset, z))
    stride = len(centres) * across
    faces = []
    for layer in (0, stride):
        faces += [(layer + i * across + j, layer + i * across + j + 1,
                   layer + (i + 1) * across + j + 1, layer + (i + 1) * across + j)
                  for i in range(len(centres) - 1) for j in range(across - 1)]
    boundary = list(range(across)) + [i * across + across - 1 for i in range(1, len(centres))]
    boundary += list(range(stride - 2, stride - across - 1, -1))
    boundary += [i * across for i in range(len(centres) - 2, 0, -1)]
    faces += [(boundary[i], boundary[(i + 1) % len(boundary)],
               boundary[(i + 1) % len(boundary)] + stride, boundary[i] + stride)
              for i in range(len(boundary))]
    return mesh(label, verts, faces, 'cloth_rose')


def swag(x0, x1):
    # Vertical ribbon instead of cylinders: recognisable scalloped drapery.
    verts = []
    n = 9
    for y in (.105, .14):
        for edge in (0, 1):
            for i in range(n):
                t = i / (n - 1)
                z = 3.18 - .29 * math.sin(math.pi * t) - edge * .17
                verts.append((x0 + (x1 - x0) * t, y + .025 * math.sin(t * math.tau * 3), z))
    faces = []
    for k in range(4):
        faces += [(k * n + i, k * n + i + 1, ((k + 1) % 4) * n + i + 1,
                   ((k + 1) % 4) * n + i) for i in range(n - 1)]
    faces += [(0, n, 2 * n, 3 * n), (n - 1, 2 * n - 1, 3 * n - 1, 4 * n - 1)]
    mesh('Scalloped rose canopy swag', verts, faces, 'cloth_rose')


def merchant_mirror():
    box('Broad carved marble bottom step', (0, 0, .06), (2.8, 1.2, .12), 'marble')
    box('Recessed ivory step riser', (0, 0, .17), (2.66, 1.06, .14), 'ivory')
    box('Gold step molding', (0, 0, .255), (2.75, 1.15, .045), 'gold')
    box('Checker floor substrate', (0, 0, .305), (2.8, 1.2, .07), 'marble_dark')
    for ix in range(8):
        for iy in range(3):
            box('Cream and teal checker floor tile',
                ((ix - 3.5) * .34, (iy - 1) * .38, .355), (.32, .36, .04),
                'tile_cream' if (ix + iy) % 2 else 'glaze_teal')
    inner = arch(1.90, .35, 2.20)
    arch_frame('Grand gold shadow profile', inner, arch(2.26, .27, 2.20),
               -.24, -.025, 'gold_dark')
    arch_frame('Grand outer stepped gold profile', arch(2.12, .30, 2.20),
               arch(2.26, .27, 2.20), -.10, .04)
    arch_frame('Grand middle stepped gold profile', arch(1.99, .34, 2.20),
               arch(2.13, .30, 2.20), -.035, .09)
    arch_frame('Grand rose gold inner bead', inner, arch(2.0, .34, 2.20),
               .015, .12, 'rose_gold')
    extrude_y('Grand recessed mirror and back board', inner, .01, .06,
              'mirror', 'mirror_back')
    for side in (-1, 1):
        x = side * 1.20
        box('Column square foot', (x, -.12, .44), (.38, .46, .18), 'marble')
        cylinder('Recessed side column core', (x, -.12, 1.70), .13, 2.30,
                 'marble_dark', n=12)
        for i in range(6):
            a = math.tau * i / 6
            cylinder('Broad ivory column flute',
                     (x + .11 * math.cos(a), -.12 + .11 * math.sin(a), 1.73),
                     .045, 2.20, 'ivory', n=5)
        box('Ivory square capital abacus', (x, -.12, 3.00), (.39, .45, .12), 'ivory')
        cylinder('Gold column neck', (x, -.12, 2.87), .18, .12, 'gold', n=8)
        cylinder('Bronze column ankle', (x, -.12, .60), .17, .11, 'bronze', n=8)
        cloth_strip('Pleated side drape',
                    [(side * 1.03, .13, 3.15), (side * 1.15, .15, 2.80),
                     (side * 1.20, .16, 2.25), (side * 1.15, .14, 1.90),
                     (side * 1.19, .16, 1.21), (side * 1.20, .16, .48)],
                    [.26, .31, .26, .15, .24, .32])
        box('Gold drape tie', (side * 1.15, .23, 1.91), (.18, .07, .08), 'gold')
        tube('Looped gold drape cord', [(side * 1.12, .24, 1.94),
                                       (side * .99, .24, 1.79),
                                       (side * 1.02, .24, 1.64)], .025, 'gold', n=5)
        cylinder('Gold cord tassel', (side * 1.02, .24, 1.57), .055, .14,
                 'gold', n=6, top=.025)
        for offset in (-.09, 0, .09):
            cylinder('Drape hem gold tassel', (side * 1.20 + offset, .20, .48),
                     .035, .13, 'gold', n=5, top=.025)
        scroll(side * .77, .14, 2.30, side, 1.0)
        scroll(side * .77, .14, .44, -side, .95)
        octahedron((side * .48, .16, 3.12), .075, 'gem_amber')
    swag(-1.04, 0)
    swag(0, 1.04)
    medallion('Merchant round sun crest', 0, .08, 3.32, .20)
    octahedron((0, .17, 3.32), .11, 'gem_rose')
    for side in (-1, 1):
        scroll(side * .09, .12, 3.11, side, .75)
    for i in range(7):
        a = math.pi * i / 6
        tube('Short sun ray', [(.21 * math.cos(a), .04, 3.32 + .21 * math.sin(a)),
                               (.28 * math.cos(a), .04, 3.32 + .28 * math.sin(a))],
             .025, 'gold', n=5)


def sand_rock():
    lump('Tall rose sandstone boulder', (-.30, -.10, .65), (1.10, 1.12, 1.30),
         'sandstone')
    lump('Leaning broad rose boulder', (.43, .06, .43), (.94, 1.15, .86),
         'sandstone_light')
    lump('Small shoulder boulder', (-.61, .37, .30), (.58, .57, .60),
         'rock')
    mesh('Low triangular sand drift',
         [(-.90, -.70, 0), (.90, -.70, 0), (.72, .22, 0), (-.65, .35, 0),
          (-.51, -.12, .30), (.53, -.14, .22)],
         [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 5), (2, 3, 4, 5), (3, 0, 4)], 'sand')
    start = len(PARTS)
    fragment = [(-.44, 0), (.40, 0), (.45, .23), (.29, .32),
                (.37, .60), (-.13, .63), (-.25, .52), (-.44, .55)]
    extrude_y('Broken half buried marble frieze', fragment, -.08, .075, 'marble')
    box('Frieze recessed dark panel', (-.02, .09, .30), (.65, .045, .32), 'marble_dark')
    box('Carved ivory frieze lower molding', (-.01, .10, .10), (.78, .12, .09), 'ivory')
    box('Broken gold frieze upper fillet', (-.02, .10, .53), (.63, .07, .05), 'gold')
    medallion('Ancient frieze sun relief', -.02, .13, .31, .12)
    for i in range(8):
        a = math.tau * i / 8
        tube('Carved sun relief ray', [(-.02 + .15 * math.cos(a), .15, .31 + .15 * math.sin(a)),
                                      (-.02 + .20 * math.cos(a), .15, .31 + .20 * math.sin(a))],
             .024, 'ivory', n=5)
    for x in (-.29, .26):
        box('Frieze coral mosaic diamond', (x, .15, .32), (.09, .04, .09),
            'coral', rot=(0, math.pi / 4, 0))
    tube('Incised fracture through broken frieze', [(.27, .078, .60), (.19, .078, .48),
                                                  (.27, .078, .42)],
         .022, 'marble_dark', n=5)
    for obj in PARTS[start:]:
        place(obj, (.18, .39, .08), (math.radians(-18), 0, math.radians(-12)))
    tube('Dark inset boulder cleft', [(-.48, .37, 1.02), (-.35, .46, .88),
                                     (-.40, .49, .73)], .025, 'sandstone_dark', n=5)
    crystal('Small rose crystal ruin sprout', (-.62, .48, .46), .09, .41,
            'crystal_rose', lean=18, azimuth=2)
    crystal('Small pale crystal ruin sprout', (-.72, .49, .43), .065, .28,
            'crystal', lean=25, azimuth=3)
    ring('Bronze crystal sprout socket', .11, .045, .45, .06, 'bronze', n=8,
         loc=(-.62, .48, 0))


def fluted_drum(label, loc, length, angle):
    # Eight wide channels, each made of four flats, rather than applied microdetail.
    n = 32
    verts = []
    for end in (-1, 1):
        for j in range(n):
            a = math.tau * j / n
            radius = .34 if j % 4 != 2 else .285
            x = end * length / 2
            if end == 1:
                x += .025 * math.sin(a * 3)
            verts.append((x, radius * math.cos(a), radius * math.sin(a)))
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    obj = place(mesh(label, verts, faces, 'marble'), loc, (0, 0, angle))
    obj.data.materials.append(MATS['marble_dark'])
    obj.data.materials.append(MATS['ivory'])
    for j in range(n):
        obj.data.polygons[j + 2].material_index = 1 if j % 4 in (1, 2) else 0
    obj.data.polygons[1].material_index = 2
    # The axial collar is a closed annulus rotated with its own drum.
    collar = ring('Gold band around fallen drum', .355, .035, -.045, .09,
                  'gold', n=24, rot=(0, math.pi / 2, 0))
    place(collar, loc, (0, 0, angle))


def fallen_pillar():
    fluted_drum('First broken fluted drum', (-.59, -.035, .34), .96, -.075)
    fluted_drum('Second angled fluted drum', (.43, .015, .34), .75, .17)
    capital = box('Toppled carved ivory capital', (1.01, .015, .48), (.43, .82, .82),
                  'ivory', rot=(.085, .035, -.08))
    bottom = min(v.co.z for v in capital.data.vertices)
    for v in capital.data.vertices:
        v.co.z -= bottom
    box('Capital marble abacus face', (1.23, .015, .50), (.10, 1.0, 1.0),
        'marble', rot=(0, 0, -.08))
    box('Capital bronze recessed molding', (1.15, .015, .50), (.08, .88, .88),
        'bronze', rot=(0, 0, -.08))
    for side in (-1, 1):
        start = len(PARTS)
        scroll(side * .13, .09, .22, side, .9, 'ivory')
        for obj in PARTS[start:]:
            place(obj, (.91, .015, .20), (0, 0, -math.pi / 2))
        # The side-facing volutes remain legible from the above-front camera.
        scroll(.81, side * .49, .28, 1, .8, 'ivory')
    band = ring('Teal and ivory fallen pillar mosaic collar', .36, .045, -.07, .14,
                'glaze_teal', n=16, loc=(.67, .056, .34), rot=(0, math.pi / 2, .17))
    band.data.materials.append(MATS['ivory'])
    for poly in band.data.polygons:
        poly.material_index = poly.index % 2
    ring('Broken column end bronze molding', .36, .05, -.035, .07,
         'bronze', n=16, loc=(-1.02, -.002, .34), rot=(0, math.pi / 2, -.075))
    for i, (loc, size) in enumerate([((-.04, .38, .10), (.28, .20, .20)),
                      ((-.87, .36, .07), (.24, .19, .14)),
                      ((.20, -.38, .09), (.25, .24, .18)),
                      ((-.30, -.38, .065), (.18, .19, .13))]):
        lump('Mixed broken stone rubble', loc, size,
             ('marble_dark', 'sandstone', 'rock', 'sandstone_light')[i])
    box('Fallen coral inlay fragment', (-.32, .29, .04), (.16, .13, .06),
        'coral', rot=(0, 0, .35))


def mirror_shard():
    profile = [(-1.21, .65), (.92, .65), (1.25, 1.03), (1.22, 2.86),
               (.43, 3.00), (.10, 2.84), (-.41, 2.98), (-1.21, 2.73)]
    extrude_y('Thin broken glass reflective on BOTH faces', profile,
              -.025, .025, 'mirror', bevel=False)
    # Ornament stays inside a 15 cm slab. The broad glass remains planar on both sides.
    box('Broken bottom shadow frame', (-.145, 0, .84), (2.13, .10, .42), 'gold_dark')
    box('Broken left shadow frame', (-1.05, 0, 1.68), (.40, .10, 2.10), 'gold_dark')
    box('Bottom stepped gold outer molding', (-.145, 0, .66), (2.13, .13, .08), 'gold')
    box('Left stepped gold outer molding', (-1.22, 0, 1.68), (.08, .13, 2.10), 'gold')
    box('Bottom stepped gold inner molding', (-.12, 0, 1.04), (2.05, .12, .08), 'gold')
    box('Left stepped gold inner molding', (-.84, 0, 1.73), (.08, .12, 2.0), 'gold')
    top_fragment = [(-1.21, 2.47), (-.52, 2.60), (-.43, 2.98),
                    (-.80, 2.87), (-1.21, 2.73)]
    extrude_y('Jagged upper gold frame remnant', top_fragment, -.055, .055, 'gold')
    extrude_y('Separate broken upper rose gold profile',
              [(.12, 2.61), (.45, 2.72), (1.22, 2.58),
               (1.22, 2.86), (.43, 3.0), (.10, 2.84)],
              -.055, .055, 'rose_gold')
    extrude_y('Broken right gold frame remnant',
              [(1.02, 1.72), (1.22, 1.94), (1.22, 2.86), (.99, 2.90)],
              -.055, .055, 'rose_gold')
    for side in (-1, 1):
        y = side * .053
        tube('Bottom rose gold bead rail', [(-1.05, y, .79), (.81, y, .79)],
             .022, 'rose_gold', n=5)
        tube('Left rose gold bead rail', [(-1.10, y, .82), (-1.10, y, 2.62)],
             .022, 'rose_gold', n=5)
        for x in (-.72, -.34, .04, .42, .78):
            box('Chunky rose gold frame bead', (x, y, .79), (.07, .04, .07),
                'rose_gold', rot=(0, math.pi / 4, 0))
        for z in (1.20, 1.55, 1.90, 2.25):
            box('Vertical rose gold frame bead', (-1.10, y, z), (.07, .04, .07),
                'rose_gold', rot=(0, math.pi / 4, 0))
        tube('Remaining flat frame filigree curl',
             [(-1.15, y, 2.62), (-1.11, y, 2.80), (-.99, y, 2.91),
              (-.86, y, 2.90), (-.83, y, 2.80), (-.93, y, 2.77)],
             .022, 'gold', n=5)
    for x, z in ((-1.05, 1.20), (.64, .84)):
        for side in (-1, 1):
            jewel = octahedron((x, 0, z), .09, 'gem_rose')
            for vertex in jewel.data.vertices:
                vertex.co.y = side * .06 + vertex.co.y * .16
    for profile in ([(-.40, 2.98), (-.26, 2.75), (-.10, 2.87)],
                    [(.22, 2.87), (.43, 3.00), (.58, 2.79)],
                    [(1.08, 1.04), (1.25, 1.03), (1.20, 1.34)]):
        extrude_y('Thin faceted crystal edge chip', profile, -.045, .045, 'crystal',
                  bevel=False)


def pebbles3():
    for i, (x, y, w, d, h) in enumerate([
            (-.43, -.38, .34, .28, .11), (.40, -.33, .40, .30, .15),
            (-.17, -.03, .32, .25, .10), (.40, .28, .25, .30, .13),
            (-.42, .34, .36, .35, .14), (.02, .45, .22, .30, .09),
            (.07, -.44, .21, .19, .08), (.10, .14, .26, .18, .11)]):
        lump('Flat scattered pebble', (x, y, h / 2), (w, d, h),
             ('rock', 'sandstone_light', 'sandstone', 'marble',
              'sandstone_dark', 'ivory', 'rock_dark', 'sandstone')[i])
    extrude_y('Tiny broken teal tile shard', [(-.10, .025), (.08, .025),
                                             (.12, .065), (-.03, .075)],
              .25, .39, 'glaze_teal')
    box('Tile shard cream backing edge', (.015, .32, .025), (.21, .16, .04),
        'tile_cream', rot=(0, 0, -.12))


def crystal_bits():
    for i, (x, y, radius, length, lean, azimuth) in enumerate([
            (-.35, -.25, .085, .25, 30, .4), (.15, -.31, .09, .31, 24, 2.0),
            (-.05, .10, .11, .35, 13, 1.1), (.31, .23, .08, .24, 34, 0),
            (-.32, .34, .065, .20, 31, 2.7)]):
        crystal('Small angled crystal splinter', (x, y, 0), radius, length,
                role='crystal_rose' if i % 2 else 'crystal',
                lean=lean, azimuth=azimuth)
        cylinder('Scattered crystal metal root collar', (x, y, .035),
                 radius * 1.12, .06, 'bronze' if i % 2 else 'gold', n=6)
        crystal('Bright exposed crystal facet', (x, y + radius * .72, .045),
                .025, length * .60, 'crystal_core', lean=lean, azimuth=azimuth)
    crystal('Tiny exposed crystal core', (.28, -.02, 0), .04, .16,
            'crystal_core', lean=28, azimuth=-.7)


def ripple():
    for radius, z, height in ((.80, .005, .007), (.58, .009, .007), (.35, .013, .007)):
        ring('Flat concentric lake ripple', radius, .04, z, height,
             'water_foam', n=40)


SPECS = [
    ('MemoryCrystal', (1.1, 1.1, 1.7), memory_crystal),
    ('Fountain', (2.2, 2.2, 1.9), fountain),
    ('StandMirror', (1.5, .6, 2.6), stand_mirror),
    ('MerchantMirror', (2.8, 1.2, 3.6), merchant_mirror),
    ('SandRock', (1.8, 1.4, 1.3), sand_rock),
    ('FallenPillar', (2.6, 1.0, 1.0), fallen_pillar),
    ('MirrorShard', (2.5, .15, 3.05), mirror_shard),
    ('Pebbles3', (1.2, 1.2, .15), pebbles3),
    ('CrystalBits', (1.0, 1.0, .35), crystal_bits),
    ('Ripple', (1.6, 1.6, .02), ripple),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return (Vector(tuple(min(v.co[i] for v in obj.data.vertices) for i in range(3))),
            Vector(tuple(max(v.co[i] for v in obj.data.vertices) for i in range(3))))


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
    obj.name, obj.data.name = name, name + '_Mesh'
    # Weld nothing: disconnected floating dust and scattered ground bits survive.
    count = triangles(obj)
    budget = 8000 if name == 'MerchantMirror' else 6000
    if count > budget:
        mod = obj.modifiers.new('Preserve material regions within triangle budget', 'DECIMATE')
        mod.ratio = (budget - 100) / count
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name, triangles(obj))

    low, high = bounds(obj)
    centre = (low + high) / 2
    span = high - low
    # The shard and ripple have deliberate air under them, not a raised origin.
    floor = .6 if name == 'MirrorShard' else (.005 if name == 'Ripple' else 0)
    top = dimensions[2]
    for v in obj.data.vertices:
        v.co.x = (v.co.x - centre.x) * dimensions[0] / span.x
        v.co.y = (v.co.y - centre.y) * dimensions[1] / span.y
        v.co.z = floor + (v.co.z - low.z) * (top - floor) / span.z
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
    assert abs(low.z - floor) < 1e-5 and abs(high.z - top) < 1e-5, name
    assert all(abs(obj.dimensions[i] - dimensions[i]) < 1e-4 for i in (0, 1)), name
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    assert all(mat.name in ROLES for mat in obj.data.materials), name
    used_roles = {obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert name == 'Ripple' or len(used_roles) >= 5, (name, used_roles)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    obj['chapter'], obj['front'] = 'Mirage Desert / Mirror World', '+Y'
    obj['footprint_and_top_m'], obj['triangle_count'] = list(dimensions), triangles(obj)
    obj['collision'] = name not in {'Pebbles3', 'CrystalBits', 'Ripple'}
    if name == 'MemoryCrystal':
        obj['interactive'] = 'shootable explosive'
    if name == 'MirrorShard':
        obj['floor_clearance_m'], obj['reflective_faces'] = .6, '+Y / -Y'
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


def preview(objects):
    """Export is already complete. Only the review image uses viewport colours."""
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
    shade.show_cavity = True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.3, 1.0
    shade.show_specular_highlight = False
    shade.background_type, shade.background_color = 'WORLD', (.045, .055, .08)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review background')
    scene.world.color = (.045, .055, .08)
    helpers = []
    old_locations = [obj.location.copy() for obj in objects]
    try:
        for role, mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role, (.5, .5, .5)), 1)
        total = sum(spec[1][0] for spec in SPECS) + .65 * (len(objects) - 1)
        x = -total / 2
        for obj, (_, dims, _) in zip(objects, SPECS):
            obj.location.x = x + dims[0] / 2
            x += dims[0] + .65
        camera = bpy.data.objects.new('Review camera only', bpy.data.cameras.new('Review camera only'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type, camera.data.ortho_scale = 'ORTHO', total + 1.0
        elevation = math.radians(55)
        target = Vector((0, 0, .8))
        camera.location = target + Vector((0, 30 * math.cos(elevation), 30 * math.sin(elevation)))
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
        # Camera-facing captions are review-only and never enter an FBX.
        for obj in objects:
            text_data = bpy.data.curves.new('Review caption', 'FONT')
            text_data.body, text_data.align_x, text_data.size = obj.name, 'CENTER', .19
            text_data.materials.append(MATS['crystal_core'])
            label = bpy.data.objects.new('Review label ' + obj.name, text_data)
            scene.collection.objects.link(label)
            label.location = (obj.location.x, 1.52, -.05)
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
        for obj, loc in zip(objects, old_locations):
            obj.location = loc
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    setup_materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    exported = []
    for index, (name, dimensions, build) in enumerate(SPECS):
        PARTS = []
        random.seed(33117 + index * 7919)
        build()
        exported.append(finish(name, dimensions))
    assert len(scene.objects) == 10 and all(obj.type == 'MESH' for obj in scene.objects)
    preview(exported)
    assert len(scene.objects) == 10 and all(obj.type == 'MESH' for obj in scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
