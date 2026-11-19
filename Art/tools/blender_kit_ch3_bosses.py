# 3장 보스 두 종을 관절 원점이 있는 분리 메시 FBX와 검토용 PNG로 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_bosses.py
"""Unrigged, metre-scale chapter-three bosses, using only stock Blender 5.x."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_bosses_preview.png'
PALETTE = {
    'boss_lavender': (.39, .29, .62), 'boss_slate': (.075, .105, .20),
    'boss_crimson': (.38, .035, .13), 'boss_cream': (.88, .83, .68),
    'gold': (.88, .57, .16), 'gold_dark': (.43, .24, .065),
    'bronze': (.46, .28, .14), 'ivory': (.96, .94, .85),
    'mirror': (.49, .72, .84), 'mirror_back': (.23, .37, .56),
    'crystal': (.70, .88, .95), 'crystal_core': (.91, .99, 1.0),
    'crystal_rose': (.81, .48, .77), 'gem_rose': (.70, .16, .42),
    'rose_gold': (.78, .44, .36), 'stardust': (1.0, .89, .56),
}
MATS = {}
COMPONENTS = []
PIVOTS = {
    'Ch3_Magician': {
        'Body': (0, 0, 0), 'Head': (0, 0, 3.05),
        'ArmL': (-.55, 0, 2.85), 'ArmR': (.55, 0, 2.85),
    },
    'Ch3_Whale': {
        'Body': (0, 0, 0), 'Tail': (0, -4.5, 1.8),
        'FinL': (-2.3, 1.5, 1.4), 'FinR': (2.3, 1.5, 1.4),
        'Jaw': (0, 4.2, 1.3),
    },
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .75
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=0):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        modifier = obj.modifiers.new('Soft ornate edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = 'ANGLE'
        modifier.angle_limit = math.radians(40)
    COMPONENTS.append(obj)
    return obj


def transform(obj, location=(0, 0, 0), rotation=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(location)) @
                       Euler(rotation).to_matrix().to_4x4())
    return obj


def box(label, location, size, role, bevel=.025, rotation=(0, 0, 0)):
    x, y, z = (value / 2 for value in size)
    vertices = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return transform(mesh(label, vertices, faces, role, bevel), location, rotation)


def cylinder(label, location, radius, depth, role, n=12, top=None,
             rotation=(0, 0, 0), bevel=0):
    top = radius if top is None else top
    vertices = [(r * math.cos(i * math.tau / n),
                 r * math.sin(i * math.tau / n), z)
                for r, z in [(radius, -depth / 2), (top, depth / 2)]
                for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    return transform(mesh(label, vertices, faces, role, bevel), location, rotation)


def rod(label, start, end, radius, role, n=6, top=None):
    start, end = Vector(start), Vector(end)
    obj = cylinder(label, (0, 0, 0), radius, (end - start).length,
                   role, n=n, top=top)
    obj.data.transform(Matrix.Translation((start + end) / 2) @
                       (end - start).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def ellipsoid(label, location, radii, role, n=16, rings=8, smooth=True):
    vertices = [(0, 0, -1)]
    for j in range(1, rings):
        angle = -math.pi / 2 + math.pi * j / rings
        vertices += [(math.cos(angle) * math.cos(i * math.tau / n),
                      math.cos(angle) * math.sin(i * math.tau / n),
                      math.sin(angle)) for i in range(n)]
    vertices.append((0, 0, 1))
    faces = [(0, 1 + (i + 1) % n, 1 + i) for i in range(n)]
    faces += [(1 + j * n + i, 1 + j * n + (i + 1) % n,
               1 + (j + 1) * n + (i + 1) % n, 1 + (j + 1) * n + i)
              for j in range(rings - 2) for i in range(n)]
    faces += [(len(vertices) - 1, 1 + (rings - 2) * n + i,
               1 + (rings - 2) * n + (i + 1) % n) for i in range(n)]
    vertices = [tuple(Vector(location) + Vector(tuple(v[k] * radii[k]
                 for k in range(3)))) for v in vertices]
    obj = mesh(label, vertices, faces, role)
    for face in obj.data.polygons:
        face.use_smooth = smooth
    return obj


def tube(label, points, radius, role, n=6):
    vertices = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(i - 1, 0)])).normalized()
        reference = Vector((0, 1, 0))
        if abs(tangent.dot(reference)) > .92:
            reference = Vector((1, 0, 0))
        u = tangent.cross(reference).normalized()
        v = tangent.cross(u)
        vertices += [tuple(Vector(point) + radius *
                     (u * math.cos(j * math.tau / n) + v * math.sin(j * math.tau / n)))
                     for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i * n + j, i * n + (j + 1) % n,
               (i + 1) * n + (j + 1) % n, (i + 1) * n + j)
              for i in range(len(points) - 1) for j in range(n)]
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, vertices, faces, role)


def hoop(label, location, radius, width, depth, role, n=32, rotation=(0, 0, 0)):
    vertices = [(r * math.cos(i * math.tau / n), r * math.sin(i * math.tau / n), z)
                for r, z in [(radius - width, -depth / 2), (radius, -depth / 2),
                             (radius, depth / 2), (radius - width, depth / 2)]
                for i in range(n)]
    faces = [(j * n + i, j * n + (i + 1) % n,
              ((j + 1) % 4) * n + (i + 1) % n, ((j + 1) % 4) * n + i)
             for j in range(4) for i in range(n)]
    return transform(mesh(label, vertices, faces, role), location, rotation)


def slab(label, outline, thickness, role, bevel=0):
    """A closed relief following an arbitrary 3D polygon, thickened in Z."""
    count = len(outline)
    vertices = [(x, y, z + offset) for offset in (-thickness / 2, thickness / 2)
                for x, y, z in outline]
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count)
              for i in range(count)]
    return mesh(label, vertices, faces, role, bevel)


def jewel(label, location, radius, role='gem_rose', height=None):
    height = radius * 1.6 if height is None else height
    x, y, z = location
    vertices = [(x + radius * math.cos(i * math.tau / 6),
                 y + radius * math.sin(i * math.tau / 6), z) for i in range(6)]
    vertices += [(x, y, z + height * .65), (x, y, z - height * .35)]
    faces = [(i, (i + 1) % 6, 6) for i in range(6)]
    faces += [((i + 1) % 6, i, 7) for i in range(6)]
    return mesh(label, vertices, faces, role)


def gear(label, location, radius, role, rotation=(0, 0, 0), teeth=12):
    outline = []
    for i in range(teeth * 4):
        angle = i * math.tau / (teeth * 4)
        r = radius * (1 if i % 4 in (1, 2) else .81)
        outline.append((r * math.cos(angle), r * math.sin(angle), 0))
    return transform(slab(label, outline, .07, role), location, rotation)


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


def numeral(label, text, location, size, role='gold', rotation=(0, 0, 0)):
    """Arabic numerals with local X/Y text and local +Z front (size = digit height)."""
    vertices, faces = digit_geometry(text, size, max(.02, size * .17))
    obj = mesh(label, vertices, faces, role)
    obj.data.transform(Matrix.Translation(Vector(location)) @ Euler(rotation).to_matrix().to_4x4())
    return obj


def card(location, angle, text, size=1):
    """A double-sided card in X/Z, fanned about the bottom centre."""
    start = len(COMPONENTS)
    w, h = .29 * size, .49 * size
    box('Crimson card back', (0, 0, h / 2), (w, .045, h), 'boss_crimson', .02)
    box('Ivory card face', (0, .028, h / 2), (w - .035, .018, h - .035), 'ivory', .012)
    # The gilded border is broad enough to survive the game's outline shader.
    for x in (-w * .43, w * .43):
        rod('Card gold edge', (x, .043, .035), (x, .043, h - .035), .012 * size, 'gold', 4)
    for z in (.035, h - .035):
        rod('Card gold edge', (-w * .43, .043, z), (w * .43, .043, z), .012 * size, 'gold', 4)
    numeral('Card clock numeral', text, (0, .051, h * .57), .105 * size,
          rotation=(math.pi / 2, 0, 0))
    # A crimson diamond below the numeral remains visible on every card.
    box('Card suit', (0, .055, h * .24), (.065 * size, .015, .065 * size),
        'boss_crimson', 0, rotation=(0, math.pi / 4, 0))
    matrix = (Matrix.Translation(Vector(location)) @
              Euler((math.radians(42), 0, 0)).to_matrix().to_4x4() @
              Euler((0, angle, 0)).to_matrix().to_4x4())
    for obj in COMPONENTS[start:]:
        obj.data.transform(matrix)


def finish_part(name, pivot):
    """Join ornament only within this animation part, retaining its exact pivot."""
    assert COMPONENTS, name
    bpy.ops.object.select_all(action='DESELECT')
    for obj in COMPONENTS:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    for obj in COMPONENTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = COMPONENTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + '_Mesh'
    obj.data.transform(Matrix.Translation(-Vector(pivot)))
    obj.location = pivot
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), name + ' has an open edge'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    assert obj.parent is None and not obj.modifiers
    assert (obj.location - Vector(pivot)).length < 1e-6
    obj['pivot_m'] = list(pivot)
    obj['front'] = '+Y'
    COMPONENTS.clear()
    return obj


def magician_body():
    for side in (-1, 1):
        x = side * .24
        rod('Slate trouser leg', (x, 0, .30), (x, 0, 1.76), .155, 'boss_slate', 10)
        rod('Trouser gold side stripe', (x + side * .145, .035, .37),
            (x + side * .145, .035, 1.72), .026, 'gold', 4)
        ellipsoid('Pointed shoe heel', (x, .06, .16), (.20, .27, .16), 'boss_slate', 12, 6)
        # Tapered toes point forward and turn out at the tips.
        rod('Long pointed shoe', (x, .16, .15), (x + side * .13, .58, .09),
            .15, 'boss_slate', 8, top=.025)
        tube('Gold shoe welt', [(x - .15, .13, .10), (x, .46, .075),
                               (x + side * .13, .58, .08)], .026, 'gold')
        hoop('Clockwork knee collar', (x, 0, 1.02), .165, .045, .10, 'bronze', 12)
    ellipsoid('Long fitted tailcoat', (0, -.025, 2.20), (.53, .29, .70), 'boss_lavender')
    cylinder('Coat waist belt', (0, 0, 1.86), .42, .12, 'boss_slate', n=12, bevel=.025)
    box('Belt clasp', (0, .39, 1.86), (.24, .08, .15), 'gold')
    jewel('Belt clasp gem', (0, .445, 1.86), .085)
    cylinder('Puppet neck', (0, 0, 2.98), .13, .24, 'bronze', bevel=.025)
    hoop('Neck clock collar', (0, 0, 3.025), .16, .045, .08, 'gold', 16)
    # Two pointed coat tails, separate from the broad crimson cape behind them.
    for side in (-1, 1):
        outline = [(side * .07, -.26, 1.91), (side * .43, -.20, 1.90),
                   (side * .55, -.36, .88), (side * .20, -.41, .68)]
        obj = slab('Pointed coat tail', [(x, z, 0) for x, y, z in outline], .10,
                   'boss_lavender', .025)
        # The slab helper's extrusion axis is rotated into the coat's depth.
        obj.data.transform(Euler((math.pi / 2, 0, 0)).to_matrix().to_4x4())
        obj.data.transform(Matrix.Translation(Vector((0, -.29, 0))))
        tube('Coat tail gold trim', [(x, y - .04, z) for x, y, z in outline] +
             [(outline[0][0], outline[0][1] - .04, outline[0][2])], .025, 'gold')
    for side in (-1, 1):
        # Broad ivory lapel inserts and layered gilded piping.
        obj = slab('Ivory lapel', [(side * .08, 2.68, 0), (side * .44, 2.85, 0),
                                 (side * .34, 2.43, 0), (side * .06, 2.12, 0)],
                   .065, 'boss_cream', .02)
        obj.data.transform(Euler((math.pi / 2, 0, 0)).to_matrix().to_4x4())
        obj.data.transform(Matrix.Translation(Vector((0, .265, 0))))
        tube('Gold lapel piping', [(side * .43, .29, 2.83), (side * .33, .33, 2.43),
                                  (side * .08, .34, 2.13)], .027, 'gold')
        curl = [(side * (.27 + .085 * math.cos(t)), .35,
                 2.52 + .12 * math.sin(t)) for t in [i * math.pi * 1.6 / 10 for i in range(11)]]
        tube('Lapel filigree curl', curl, .022, 'gold', 5)
    for z in (1.99, 2.19, 2.39, 2.59):
        ellipsoid('Gold coat button', (0, .30, z), (.055, .045, .055), 'gold', 8, 4)
    ellipsoid('Collar brooch mount', (0, .32, 2.81), (.155, .06, .12), 'gold', 12, 6)
    ellipsoid('Rose collar brooch', (0, .378, 2.81), (.105, .045, .08), 'gem_rose', 8, 4, False)
    # Solid double-faced cape with stiff radial pleats and a contrasting lining.
    count = 16
    vertices = []
    for inner in (False, True):
        for row in range(4):
            t = row / 3
            for i in range(count + 1):
                u = i / count * 2 - 1
                x = u * (.57 + .75 * t)
                y = -.19 - .95 * t - .18 * (1 - u * u)
                y += (.075 if i % 2 else -.075) * t
                if inner:
                    y += .055
                z = 2.88 - 2.62 * t + .10 * t * math.cos(u * math.pi * 2)
                vertices.append((x, y, z))
    layer = 4 * (count + 1)
    faces, roles = [], []
    for inner in (False, True):
        base = layer if inner else 0
        for row in range(3):
            for i in range(count):
                a = base + row * (count + 1) + i
                faces.append((a, a + 1, a + count + 2, a + count + 1))
                # Turned-back edge pleats expose cream lining, while the
                # central folds present the crimson exterior to the camera.
                roles.append(1 if inner and (i < 2 or i >= count - 2) else 0)
    boundary = list(range(count + 1))
    boundary += [row * (count + 1) + count for row in range(1, 4)]
    boundary += [3 * (count + 1) + i for i in range(count - 1, -1, -1)]
    boundary += [row * (count + 1) for row in (2, 1)]
    for i, a in enumerate(boundary):
        b = boundary[(i + 1) % len(boundary)]
        faces.append((a, b, b + layer, a + layer))
        roles.append(1)
    cape = mesh('Pleated crimson cape with cream lining', vertices, faces, 'boss_crimson')
    cape.data.materials.append(MATS['boss_cream'])
    for face, material in zip(cape.data.polygons, roles):
        face.material_index = material
    for i in (0, count):
        tube('Cape cream rolled edge', [tuple(Vector(vertices[row * (count + 1) + i]) +
             Vector((0, .065, 0))) for row in range(4)], .075, 'boss_cream')
    tube('Cape ornate gold hem', [tuple(Vector(vertices[3 * (count + 1) + i]) +
         Vector((0, -.035, .045))) for i in range(count + 1)], .037, 'gold')
    for i in (2, 5, 8, 11, 14):
        x, y, z = vertices[3 * (count + 1) + i]
        jewel('Cape rose hem stud', (x, y - .045, z + .15), .075)
    rod('Wind-up key spindle', (0, -.24, 2.67), (0, -.87, 2.67), .095, 'bronze', 10)
    for side in (-1, 1):
        hoop('Wind-up key bow', (side * .22, -.88, 2.70), .235, .073, .09,
             'gold', 16, (math.pi / 2, 0, 0))
    box('Wind-up key bridge', (0, -.88, 2.66), (.24, .11, .13), 'bronze')


def magician_head():
    ellipsoid('Smooth porcelain mask', (0, .12, 3.22), (.285, .24, .21), 'boss_cream', 20, 10)
    for side in (-1, 1):
        box('Recessed eye slit', (side * .12, .337, 3.26), (.18, .04, .075),
            'boss_slate', .025, (0, side * -.14, 0))
        box('Glowing eye slit', (side * .12, .365, 3.265), (.14, .018, .042),
            'crystal_core', .015, (0, side * -.14, 0))
        gear('Clockwork gold ear', (side * .285, .075, 3.23), .13, 'gold',
             (0, math.pi / 2, 0), teeth=10)
        rod('Ear axle', (side * .28, .075, 3.23), (side * .35, .075, 3.23), .05, 'bronze', 8)
    # A filled crescent inlay following the porcelain surface, with no nose.
    outline = []
    for i in range(13):
        x = -.17 + .34 * i / 12
        z = 3.16 - .03 * (1 - (x / .17) ** 2)
        outline.append((x, z, 0))
    for i in range(11, 0, -1):
        x = -.17 + .34 * i / 12
        z = 3.16 - .075 * (1 - (x / .17) ** 2)
        outline.append((x, z, 0))
    smile = slab('Painted gold crescent smile', outline, .014, 'gold')
    smile.data.transform(Euler((math.pi / 2, 0, 0)).to_matrix().to_4x4())
    smile.data.transform(Matrix.Translation(Vector((0, .332, 0))))
    cylinder('Top hat broad brim', (0, 0, 3.37), .43, .085, 'boss_slate', 24, bevel=.025)
    hoop('Hat brim gold piping', (0, 0, 3.41), .43, .035, .035, 'gold', 24)
    cylinder('Tall top hat crown', (0, -.025, 3.695), .27, .61, 'boss_slate', 20,
             top=.31, bevel=.025)
    cylinder('Crimson hat ribbon', (0, -.025, 3.48), .285, .14, 'boss_crimson', 20, top=.295)
    hoop('Top hat upper gold line', (0, -.025, 3.982), .31, .028, .035, 'gold', 20)
    box('Hat ribbon gold buckle', (0, .268, 3.48), (.19, .055, .13), 'gold', .02)
    box('Hat ribbon buckle inset', (0, .300, 3.48), (.11, .016, .065), 'boss_crimson', .01)
    for i, text in enumerate(('1', '5', '10')):
        card((-.18 + i * .15, .12 - i * .018, 3.45),
             math.radians((i - 1) * 18), text, .58)


def magician_arm(side):
    shoulder = (side * .55, 0, 2.85)
    if side < 0:
        elbow, wrist = (-.78, .02, 2.27), (-.92, .10, 1.84)
    else:
        elbow, wrist = (.86, .08, 2.34), (1.04, .75, 2.61)
    ellipsoid('Shoulder clock socket', shoulder, (.22, .22, .22), 'gold', 12, 6)
    rod('Lavender upper sleeve', shoulder, elbow, .185, 'boss_lavender', 12, top=.14)
    ellipsoid('Bronze elbow joint', elbow, (.155, .155, .155), 'bronze', 12, 6)
    rod('Lavender forearm sleeve', elbow, wrist, .15, 'boss_lavender', 12, top=.125)
    direction = (Vector(wrist) - Vector(elbow)).normalized()
    cuff = Vector(wrist) - direction * .06
    rod('Cream turned cuff', cuff - direction * .09, cuff + direction * .06,
        .172, 'boss_cream', 12)
    rod('Gilded cuff edge', cuff + direction * .05, cuff + direction * .095,
        .177, 'gold', 12)
    palm = Vector(wrist) + direction * .13
    ellipsoid('Ivory glove palm', palm, (.15, .145, .17), 'ivory', 12, 6)
    for i in range(3):
        p = palm + Vector(((i - 1) * .065, .035, -.075))
        rod('Sculpted glove fingers', p, p + direction * .11, .041, 'ivory', 6)
    ellipsoid('Glove thumb', palm + Vector((-side * .13, .02, .01)),
              (.075, .075, .10), 'ivory', 8, 4)
    # A wide sleeve inlay reads through the top-down view.
    tube('Sleeve golden braid', [(shoulder[0], .17, 2.81),
                                (elbow[0], elbow[1] + .14, elbow[2]),
                                (wrist[0], wrist[1] + .12, wrist[2])], .026, 'gold')
    if side < 0:
        a, b = (-1.03, .12, 1.55), (-1.31, .20, 2.32)
        rod('Short slate magic wand', a, b, .045, 'boss_slate', 10)
        direction = (Vector(b) - Vector(a)).normalized()
        rod('Gold wand tip', Vector(b) - direction * .14, b, .056, 'gold', 10)
        rod('Ivory wand heel', a, Vector(a) + direction * .10, .049, 'ivory', 10)
    else:
        for i, text in enumerate(('1', '2', '3', '4', '5', '6', '7')):
            card((1.055 + (i - 3) * .021, .91 + i * .012, 2.65),
                 math.radians(-35 + i * 70 / 6), text, 1.12)


def build_magician():
    parts = []
    magician_body()
    parts.append(finish_part('Body', PIVOTS['Ch3_Magician']['Body']))
    magician_head()
    parts.append(finish_part('Head', PIVOTS['Ch3_Magician']['Head']))
    for name, side in [('ArmL', -1), ('ArmR', 1)]:
        magician_arm(side)
        parts.append(finish_part(name, PIVOTS['Ch3_Magician'][name]))
    return parts


# Longitudinal elliptical stations, from the tail articulation to the +Y nose.
WHALE_STATIONS = [
    (-4.5, .48, 1.80, .52), (-3.7, .86, 1.95, .91),
    (-2.5, 1.50, 2.10, 1.48), (-1.0, 2.12, 2.20, 1.88),
    (1.0, 2.45, 2.30, 2.00), (3.2, 2.45, 2.30, 2.00),
    (4.8, 2.16, 2.30, 1.80), (6.0, 1.56, 2.40, 1.45),
    (6.75, .77, 2.50, .90), (7.0, .06, 2.50, .08),
]


def whale_point(station, angle, offset=0):
    y, rx, z, rz = station
    return Vector(((rx + offset) * math.cos(angle), y,
                   z + (rz + offset) * math.sin(angle)))


def patch(label, corners, role, inset=.045, thickness=.035):
    """Closed inset plate. Its exposed foundation forms continuous gold seams."""
    center = sum((Vector(p) for p in corners), Vector()) / len(corners)
    normal = (Vector(corners[1]) - Vector(corners[0])).cross(
              Vector(corners[3]) - Vector(corners[0])).normalized()
    outer = [Vector(p).lerp(center, inset) for p in corners]
    vertices = [tuple(p + normal * offset) for offset in (-thickness / 2, thickness / 2)
                for p in outer]
    faces = [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return mesh(label, vertices, faces, role)


def whale_body():
    n = 24
    vertices = [tuple(whale_point(station, i * math.tau / n))
                for station in WHALE_STATIONS for i in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(j * n + i, (j + 1) * n + i,
               (j + 1) * n + (i + 1) % n, j * n + (i + 1) % n)
              for j in range(len(WHALE_STATIONS) - 1) for i in range(n)]
    faces.append(tuple(range((len(WHALE_STATIONS) - 1) * n, len(vertices))))
    mesh('Continuous gold stained-mirror foundation', vertices, faces, 'gold')
    plate_roles = ['mirror', 'mirror', 'crystal', 'mirror', 'mirror_back',
                   'mirror', 'boss_lavender', 'mirror', 'crystal_rose']
    for j in range(len(WHALE_STATIONS) - 1):
        for i in range(n):
            a, b = i * math.tau / n, (i + 1) * math.tau / n
            corners = [whale_point(WHALE_STATIONS[j], a, .025),
                       whale_point(WHALE_STATIONS[j + 1], a, .025),
                       whale_point(WHALE_STATIONS[j + 1], b, .025),
                       whale_point(WHALE_STATIONS[j], b, .025)]
            lower = math.sin((a + b) / 2) < -.48
            role = 'boss_cream' if lower else plate_roles[(i + j * 2) % len(plate_roles)]
            patch('Cream belly panel' if lower else 'Large mirror skin plate',
                  corners, role, .075 if lower else .055)
    # Sweeping longitudinal ventral grooves are proud, dark inlays between plates.
    for angle in [math.radians(d) for d in (220, 240, 260, 280, 300, 320)]:
        tube('Lavender belly flute', [whale_point(s, angle, .055)
             for s in WHALE_STATIONS[2:9]], .044, 'boss_lavender')
    # Side cheek rosettes, luminous inset eyes, and large readable brows.
    for side in (-1, 1):
        eye = Vector((side * 1.91, 4.96, 2.87))
        rotation = (math.pi / 2, side * -.58, 0)
        ellipsoid('Eye dark socket', eye, (.32, .25, .28), 'boss_slate', 16, 8)
        ellipsoid('Luminous crystal eye', eye + Vector((side * .11, .15, .025)),
                  (.20, .16, .19), 'crystal_core', 16, 8)
        tube('Gold raised eye brow', [(side * 1.61, 5.12, 3.13),
                                      (side * 1.92, 5.06, 3.24),
                                      (side * 2.13, 4.82, 3.08)], .065, 'gold', 8)
        gear('Gilded cheek rosette', (side * 2.30, 3.60, 2.16), .30, 'gold',
             (0, math.pi / 2, 0), 12)
        ellipsoid('Rose cheek jewel', (side * 2.34, 3.60, 2.16),
                  (.10, .16, .16), 'gem_rose', 10, 6, False)
    # A curved mouth seam on the fixed upper face, above the movable jaw.
    tube('Upper gold smile line', [(-1.50, 5.52, 1.25), (-1.16, 6.12, 1.15),
                                  (0, 6.72, 1.19), (1.16, 6.12, 1.15),
                                  (1.50, 5.52, 1.25)], .065, 'gold', 8)
    clock_dial()
    for i, (y, z, radius, height) in enumerate([
        (-3.45, 2.91, .20, .65), (-2.65, 3.43, .27, .95),
        (-1.70, 3.97, .29, 1.0), (3.45, 4.28, .22, .76),
        (4.30, 4.05, .20, .70),
    ]):
        cylinder('Spine crystal gold collar', (0, y, z), radius * 1.18, .13,
                 'bronze', 10, bevel=.025)
        cylinder('Spine faceted crystal prism', (0, y, z + height * .32), radius,
                 height * .64, 'crystal_rose' if i % 2 else 'crystal', 6, top=radius * .80)
        cylinder('Spine crystal crown', (0, y, z + height * .82), radius * .80,
                 height * .36, 'crystal_rose' if i % 2 else 'crystal', 6, top=0)
        rod('Spine crystal luminous face', (0, y + radius * .88, z + .09),
            (0, y + radius * .77, z + height * .65), .055, 'crystal_core', 4)
    for location, radius in [((-2.9, -.9, 3.55), .10), ((2.85, 2.7, 4.1), .11),
                             ((-1.6, -3.7, 3.7), .09), ((1.3, -.6, 5.1), .10)]:
        jewel('Floating stardust', location, radius, 'stardust')


def clock_dial():
    # The clock sits in the broad dorsal saddle, facing the review/game camera.
    center = Vector((0, .90, 4.23))
    radius = 1.65
    cylinder('Clock bronze plinth', center, radius + .16, .13, 'bronze', 48, bevel=.03)
    cylinder('Dorsal ivory clock dial', center + Vector((0, 0, .09)), radius, .12,
             'ivory', 48, bevel=.025)
    hoop('Clock outer gold molding', center + Vector((0, 0, .17)), radius + .07,
         .095, .09, 'gold', 48)
    hoop('Clock lavender inner annulus', center + Vector((0, 0, .16)), radius - .40,
         .07, .035, 'boss_lavender', 40)
    hoop('Clock gold inner hairline', center + Vector((0, 0, .18)), radius - .50,
         .035, .035, 'gold', 40)
    numerals = ('12', '1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11')
    for i, text in enumerate(numerals):
        angle = i * math.tau / 12
        loc = center + Vector(((radius - .25) * math.sin(angle),
                               (radius - .25) * math.cos(angle), .19))
        numeral('Gold clock hour', text, loc, .24, rotation=(0, 0, -angle))
        stud = center + Vector(((radius + .12) * math.sin(angle),
                                (radius + .12) * math.cos(angle), .18))
        jewel('Clock rose bezel stud', stud, .065)
    for angle, length, width in [(math.radians(-50), .85, .12),
                                 (math.radians(36), 1.14, .085)]:
        start = center + Vector((0, 0, .245))
        end = start + Vector((math.sin(angle) * length, math.cos(angle) * length, 0))
        rod('Gold clock hand', start, end, width, 'gold', 4, top=.025)
        back = start - (end - start).normalized() * .22
        rod('Gold clock counterweight', back, start, .055, 'gold', 4)
    jewel('Clock rose central pin', center + Vector((0, 0, .29)), .17)
    # Four large scrolls connect the clock's bezel to the mirrored back.
    for side in (-1, 1):
        for yside in (-1, 1):
            points = [(side * (1.55 + .27 * math.cos(t)),
                       .90 + yside * (.9 + .30 * math.sin(t)), 4.22 - .18 * abs(math.sin(t)))
                      for t in [i * math.tau * .8 / 15 for i in range(16)]]
            tube('Dorsal gold filigree spiral', points, .05, 'gold')


def plated_appendage(label, outline, center, seams):
    """Gold-rimmed broad mirror flipper with a coloured fan of inset plates."""
    slab(label + ' gold rim foundation', outline, .17, 'gold', .035)
    roles = ('mirror', 'mirror_back', 'mirror', 'crystal', 'boss_lavender', 'mirror')
    for i, point in enumerate(outline):
        nxt = outline[(i + 1) % len(outline)]
        triangle = [Vector(center), Vector(point), Vector(nxt)]
        centroid = sum(triangle, Vector()) / 3
        inset = [tuple(p.lerp(centroid, .10) + Vector((0, 0, .105))) for p in triangle]
        slab(label + ' inset mirror plate', inset, .055, roles[i % len(roles)])
    for points in seams:
        tube(label + ' gold fan rib', points, .04, 'gold')
    tube(label + ' rolled golden edge', list(outline) + [outline[0]], .065, 'gold', 8)


def whale_tail():
    rod('Tapered tail stock', (0, -4.5, 1.8), (0, -5.72, 1.97),
        .50, 'mirror', 16, top=.28)
    for y, z, radius in [(-4.58, 1.81, .51), (-5.15, 1.89, .41), (-5.63, 1.96, .31)]:
        hoop('Tail stock gold articulation band', (0, y, z), radius, .055, .08,
             'gold', 16, (math.pi / 2, 0, 0))
    outline = [(0, -5.48, 2.04), (-.85, -5.61, 2.06), (-2.1, -5.30, 2.30),
               (-3.0, -5.52, 2.49), (-2.65, -6.32, 2.27),
               (-1.35, -7.0, 1.98), (0, -6.32, 1.91),
               (1.35, -7.0, 1.98), (2.65, -6.32, 2.27),
               (3.0, -5.52, 2.49), (2.1, -5.30, 2.30), (.85, -5.61, 2.06)]
    plated_appendage('Broad split tail fluke', outline, (0, -5.97, 2.10), [])
    for side in (-1, 1):
        for x, y, z in [(.88, -6.58, 2.10), (1.65, -6.57, 2.17), (2.35, -5.82, 2.44)]:
            jewel('Tail rose stud', (side * x, y, z), .115)


def whale_fin(side):
    outline = [(side * 2.20, 1.82, 1.55), (side * 2.70, 1.45, 1.27),
               (side * 3.14, .37, .84), (side * 3.30, -1.15, .46),
               (side * 2.85, -.82, .54), (side * 2.34, .10, .93),
               (side * 2.16, 1.08, 1.36)]
    plated_appendage('Pectoral fin', outline, (side * 2.64, .47, 1.02), [])
    ellipsoid('Fin root bronze bearing', (side * 2.30, 1.50, 1.40),
              (.23, .30, .27), 'bronze', 12, 6)
    jewel('Fin root rose cabochon', (side * 2.48, 1.50, 1.57), .12)


def whale_jaw():
    # Its posterior attachment is the explicit jaw hinge, independent of Body.
    outline = [(-1.46, 4.20, 1.26), (-1.56, 5.36, 1.00),
               (-1.16, 6.10, .90), (0, 6.70, .92),
               (1.16, 6.10, .90), (1.56, 5.36, 1.00), (1.46, 4.20, 1.26)]
    slab('Cream lower jaw', outline, .35, 'boss_cream', .05)
    # Dark inner mouth and the lip are part of Jaw and rotate with it.
    inset = [(x * .87, 4.25 + (y - 4.25) * .94, z + .19) for x, y, z in outline]
    slab('Lavender inner mouth', inset, .07, 'boss_lavender', .025)
    tube('Golden lower lip', [(x, y, z + .20) for x, y, z in outline], .07, 'gold', 8)
    for x in (-.8, -.4, 0, .4, .8):
        tube('Jaw lavender ventral groove', [(x, 4.35, 1.05), (x * .85, 5.25, .78),
                                            (x * .45, 6.15, .71)], .033, 'boss_lavender')
    for side in (-1, 1):
        gear('Jaw hinge gold medallion', (side * 1.47, 4.2, 1.3), .23,
             'gold', (0, math.pi / 2, 0), 10)


def build_whale():
    parts = []
    whale_body()
    parts.append(finish_part('Body', PIVOTS['Ch3_Whale']['Body']))
    whale_tail()
    parts.append(finish_part('Tail', PIVOTS['Ch3_Whale']['Tail']))
    for name, side in [('FinL', -1), ('FinR', 1)]:
        whale_fin(side)
        parts.append(finish_part(name, PIVOTS['Ch3_Whale'][name]))
    whale_jaw()
    parts.append(finish_part('Jaw', PIVOTS['Ch3_Whale']['Jaw']))
    return parts


def bounds(objects):
    points = [obj.matrix_world @ vertex.co for obj in objects for vertex in obj.data.vertices]
    return (Vector(tuple(min(p[i] for p in points) for i in range(3))),
            Vector(tuple(max(p[i] for p in points) for i in range(3))))


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def visible_materials(objects):
    """Occlusion-aware projected area across all separate animation parts."""
    vertices, faces, roles = [], [], []
    for obj in objects:
        obj.data.calc_loop_triangles()
        offset = len(vertices)
        vertices += [obj.matrix_world @ v.co for v in obj.data.vertices]
        faces += [tuple(i + offset for i in t.vertices) for t in obj.data.loop_triangles]
        roles += [obj.data.materials[t.material_index].name for t in obj.data.loop_triangles]
    tree = BVHTree.FromPolygons(vertices, faces, all_triangles=True)
    view = Vector((.18, 1, 1)).normalized()
    areas = {}
    for i, face in enumerate(faces):
        a, b, c = (vertices[index] for index in face)
        area_vector = (b - a).cross(c - a) / 2
        projected = area_vector.dot(view)
        if projected <= 0:
            continue
        center = (a + b + c) / 3
        _, _, hit, _ = tree.ray_cast(center + view * 50, -view, 50.01)
        if hit == i:
            areas[roles[i]] = areas.get(roles[i], 0) + projected
    total = sum(areas.values())
    return {role: area / total for role, area in areas.items()}


def export_boss(name, objects, budget):
    bpy.context.view_layer.update()
    count = sum(triangles(obj) for obj in objects)
    assert count < budget, (name, count, budget)
    assert {obj.name for obj in objects} == set(PIVOTS[name]), name
    for obj in objects:
        assert obj.type == 'MESH' and obj.parent is None and not obj.constraints
        assert (obj.location - Vector(PIVOTS[name][obj.name])).length < 1e-6
        assert Vector(obj.rotation_euler).length < 1e-6 and (obj.scale - Vector((1, 1, 1))).length < 1e-6
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        for mat in obj.data.materials:
            assert mat.name in PALETTE and tuple(mat.diffuse_color) == (.5, .5, .5, 1)
            assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
            assert not any(node.type == 'TEX_IMAGE' for node in mat.node_tree.nodes)
    lo, hi = bounds(objects)
    coverage = visible_materials(objects)
    print('VISIBLE ' + name + ' ' + str({k: round(v, 3) for k, v in
          sorted(coverage.items(), key=lambda item: -item[1])}), flush=True)
    assert len(coverage) >= 5 and max(coverage.values()) <= .56, (name, coverage)
    if name == 'Ch3_Magician':
        assert abs(lo.z) < .04 and abs(hi.z - 4.0) < .08, (lo, hi)
    else:
        assert abs((hi - lo).y - 14) < .25 and 4.8 < hi.z < 5.5, (lo, hi)
        assert .20 < lo.z < .40, (lo, hi)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj['triangle_count'] = triangles(obj)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    path = OUTPUT / ('Kit_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert path.is_file() and path.stat().st_size > 1024
    dimensions = tuple(round(value, 3) for value in hi - lo)
    print(f'EXPORTED {name} {count} {dimensions} parts=' + ','.join(obj.name for obj in objects), flush=True)
    # Free the exact shared part names for the next boss, after its FBX is written.
    for obj in objects:
        obj.name = name + '__' + obj.name


def verify_fbx(name):
    """Read the deliverable back and check its animation interface and world size."""
    existing = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(OUTPUT / ('Kit_' + name + '.fbx')),
                             use_anim=False)
    imported = list(set(bpy.data.objects) - existing)
    try:
        assert {obj.name for obj in imported} == set(PIVOTS[name]), (name, [o.name for o in imported])
        for obj in imported:
            assert obj.type == 'MESH' and obj.parent is None, (name, obj.name)
            assert (obj.matrix_world.translation - Vector(PIVOTS[name][obj.name])).length < .001, (name, obj.name, obj.location)
        bpy.context.view_layer.update()
        original = [obj for obj in existing if obj.name.startswith(name + '__')]
        before, after = bounds(original), bounds(imported)
        assert all((a - b).length < .005 for a, b in zip(before, after)), (name, before, after)
        print('VERIFIED ' + name + ' FBX mesh names, top-level parts, pivots and bounds', flush=True)
    finally:
        for obj in imported:
            data = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if data.users == 0:
                bpy.data.meshes.remove(data)


def preview(groups):
    """Only the review scene receives colours and a side-by-side arrangement."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light, shading.color_type = 'FLAT', 'MATERIAL'
    shading.show_shadows, shading.show_cavity = False, True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor, shading.curvature_valley_factor = 1.35, 1.1
    shading.show_specular_highlight = False
    shading.show_object_outline = True
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.025, .034, .060)
    originals = {obj: obj.location.copy() for group in groups for obj in group}
    helpers = []
    try:
        for role, mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role], 1)
        # Keep true metre scale. Move the small magician forward beside the whale.
        for obj in groups[0]:
            obj.location += Vector((-5.9, 3.8, 0))
        for obj in groups[1]:
            obj.location.x += 1.65
        bpy.context.view_layer.update()
        camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type = 'ORTHO'
        direction = Vector((.18, 1, 1)).normalized()
        target = Vector((0, 0, 2))
        camera.location = target + direction * 40
        camera.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
        rotation = camera.rotation_euler.to_matrix()
        right, up = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0))
        points = [obj.matrix_world @ v.co for group in groups for obj in group for v in obj.data.vertices]
        xmin, xmax = min(p.dot(right) for p in points), max(p.dot(right) for p in points)
        ymin, ymax = min(p.dot(up) for p in points), max(p.dot(up) for p in points)
        target = right * ((xmin + xmax) / 2) + up * ((ymin + ymax) / 2)
        camera.location = target + direction * 40
        camera.data.ortho_scale = max(xmax - xmin, (ymax - ymin) * 1600 / 900) * 1.13
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW ' + str(PREVIEW), flush=True)
    finally:
        scene.camera = None
        for obj in helpers:
            bpy.data.objects.remove(obj, do_unlink=True)
        for obj, location in originals.items():
            obj.location = location
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(331903)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    magician = build_magician()
    export_boss('Ch3_Magician', magician, 12000)
    whale = build_whale()
    export_boss('Ch3_Whale', whale, 16000)
    for name in PIVOTS:
        verify_fbx(name)
    assert len(scene.objects) == 9 and all(obj.type == 'MESH' for obj in scene.objects)
    preview([magician, whale])
    assert len(scene.objects) == 9 and all(obj.parent is None for obj in scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
