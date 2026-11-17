# 서부 마을의 녹아내리는 파사드 8종과 골목 울타리를 만들고 FBX로 내보낸다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_facades.py
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1"
COLORS = {
    "wood_dark": (0.33, 0.19, 0.12),
    "wood": (0.55, 0.33, 0.19),
    "wood_light": (0.72, 0.48, 0.28),
    "paint_teal": (0.24, 0.48, 0.47),
    "paint_cream": (0.88, 0.8, 0.62),
    "paint_mustard": (0.82, 0.6, 0.2),
    "paint_red": (0.62, 0.2, 0.15),
    "paint_blue": (0.3, 0.38, 0.55),
    "adobe": (0.82, 0.6, 0.42),
    "brick": (0.58, 0.28, 0.2),
    "stone": (0.55, 0.5, 0.46),
    "roof_tin": (0.46, 0.48, 0.52),
    "roof_red": (0.5, 0.2, 0.14),
    "roof_shingle": (0.36, 0.25, 0.2),
    "window": (0.12, 0.13, 0.2),
    "window_lit": (0.98, 0.78, 0.4),
    "iron": (0.24, 0.22, 0.25),
    "brass": (0.85, 0.62, 0.25),
    "gold_drip": (1.0, 0.78, 0.22),
    "cloth_red": (0.7, 0.15, 0.13),
    "canvas": (0.85, 0.76, 0.6),
}
MATERIALS = {}
PARTS = []
RNG = random.Random()


def mesh(name, vertices, faces, material):
    data = bpy.data.meshes.new(name + "_Mesh")
    data.from_pydata(vertices, [], faces)
    data.update()
    normals = bmesh.new()
    normals.from_mesh(data)
    bmesh.ops.recalc_face_normals(normals, faces=list(normals.faces))
    normals.to_mesh(data)
    normals.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATERIALS[material])
    PARTS.append(obj)
    return obj


def bevel(obj, width=0.025, length_axis=None):
    modifier = obj.modifiers.new("손으로 다듬은 모서리", "BEVEL")
    modifier.width = width
    modifier.segments = 2
    if length_axis is None:
        modifier.limit_method = "ANGLE"
    else:
        # 긴 판재는 길이 방향 모서리를 둥글려 삼각형 예산을 아낀다.
        weights = obj.data.attributes.new("bevel_weight_edge", "FLOAT", "EDGE")
        for edge in obj.data.edges:
            a, b = (obj.data.vertices[index].co for index in edge.vertices)
            weights.data[edge.index].value = float(
                abs(a[length_axis] - b[length_axis]) > 0.00001
            )
        modifier.limit_method = "WEIGHT"
    return obj


def box(name, center, size, material, rounding=0.022, rotation=None):
    x, y, z = (dimension / 2 for dimension in size)
    vertices = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(3, 2, 1, 0), (0, 1, 5, 4), (1, 2, 6, 5),
             (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)]
    obj = mesh(name, vertices, faces, material)
    obj.location = center
    if rotation is not None:
        obj.rotation_euler = rotation
    if rounding:
        axis = max(range(3), key=lambda index: size[index])
        bevel(obj, rounding, axis)
    return obj


def prism(name, outline, back, front, material, rounding=0.025):
    count = len(outline)
    vertices = [(x, y, z) for y in (back, front) for x, z in outline]
    faces = [tuple(reversed(range(count))), tuple(range(count, 2 * count))]
    faces.extend((index, (index + 1) % count,
                  (index + 1) % count + count, index + count)
                 for index in range(count))
    obj = mesh(name, vertices, faces, material)
    if rounding:
        bevel(obj, rounding, 1)
    return obj


def beam(name, start, end, thickness, material, depth=None):
    start, end = Vector(start), Vector(end)
    obj = box(name, (start + end) / 2,
              (thickness, depth or thickness, (end - start).length), material)
    obj.rotation_euler = (end - start).to_track_quat("Z", "Y").to_euler()
    return obj


def cylinder(name, center, radius, length, material, axis="Z", sides=10):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=sides, radius=radius, depth=length, location=center
    )
    obj = bpy.context.object
    obj.name = name
    if axis == "Y":
        obj.rotation_euler.x = math.pi / 2
    elif axis == "X":
        obj.rotation_euler.y = math.pi / 2
    obj.data.materials.append(MATERIALS[material])
    PARTS.append(obj)
    return obj


def ring(name, x, z, width, height, border, material="wood_dark",
         back=0.985, front=1.10):
    outer = [(x - width / 2 - border, z - height / 2 - border),
             (x + width / 2 + border, z - height / 2 - border),
             (x + width / 2 + border, z + height / 2 + border),
             (x - width / 2 - border, z + height / 2 + border)]
    inner = [(x - width / 2, z - height / 2),
             (x + width / 2, z - height / 2),
             (x + width / 2, z + height / 2),
             (x - width / 2, z + height / 2)]
    vertices = [(px, y, pz) for y in (back, front)
                for loop in (outer, inner) for px, pz in loop]
    faces = []
    for index in range(4):
        following = (index + 1) % 4
        faces.extend([(index, following, following + 8, index + 8),
                      (index + 4, index + 12, following + 12, following + 4),
                      (index, index + 4, following + 4, following),
                      (index + 8, following + 8, following + 12, index + 12)])
    return bevel(mesh(name, vertices, faces, material), 0.02, 1)


def window(x, z, width, height, lit=False, bars=False, mullion=True):
    # 유리 앞면 1.05, 벽/프레임 앞면 1.10으로 실제 5cm 깊이를 준다.
    box("Inset glass", (x, 1.025, z), (width, 0.05, height),
        "window_lit" if lit else "window", rounding=0)
    ring("Window frame", x, z, width, height, 0.075)
    box("Window sill", (x, 1.115, z - height / 2 - 0.07),
        (width + 0.22, 0.19, 0.10), "wood_light")
    if mullion:
        box("Window mullion", (x, 1.075, z), (0.055, 0.05, height), "wood")
        box("Window transom", (x, 1.075, z + height * 0.08),
            (width, 0.05, 0.055), "wood")
    if bars:
        for offset in (-0.28, 0, 0.28):
            cylinder("Iron window bar", (x + offset * width, 1.16, z),
                     0.025, height + 0.07, "iron", sides=6)
        box("Iron crossbar", (x, 1.16, z - 0.12),
            (width + 0.08, 0.045, 0.055), "iron", rounding=0)


def door(x, width=0.85, height=1.75, material="wood", double=False):
    box("Closed door backing", (x, 1.115, height / 2 + 0.09),
        (width, 0.10, height), "wood_dark")
    for index in range(4):
        board_width = width / 4
        box("Door plank", (x - width / 2 + (index + 0.5) * board_width,
                           1.175, height / 2 + 0.09),
            (board_width - 0.012, 0.055, height - 0.07), material,
            rotation=(0, RNG.uniform(-0.008, 0.008), 0))
    ring("Door casing", x, height / 2 + 0.09, width, height,
         0.085, front=1.20, back=1.065)
    handle_x = x + (0.09 if double else width * 0.31)
    cylinder("Door handle", (handle_x, 1.22, height * 0.48),
             0.045, 0.055, "brass", axis="Y", sides=8)
    if double:
        box("Double door meeting stile", (x, 1.205, height / 2 + 0.09),
            (0.065, 0.05, height), "wood_dark")


def wall(width, height, material, openings=(), planks=True, bevel_width=0.025):
    if planks:
        box("Closed building core", (0, -0.26, height / 2),
            (width, 2.48, height), material, rounding=bevel_width)
        count = max(6, round(width / 0.43))
        step = (width - 0.06) / count
        for index in range(count):
            left = -width / 2 + 0.03 + index * step
            right = left + step
            intervals = [(0.035, height - 0.035)]
            for x, z, opening_width, opening_height in openings:
                if right <= x - opening_width / 2 - 0.035:
                    continue
                if left >= x + opening_width / 2 + 0.035:
                    continue
                lower = z - opening_height / 2 - 0.025
                upper = z + opening_height / 2 + 0.025
                clipped = []
                for bottom, top in intervals:
                    if upper <= bottom or lower >= top:
                        clipped.append((bottom, top))
                    else:
                        if lower > bottom:
                            clipped.append((bottom, lower))
                        if upper < top:
                            clipped.append((upper, top))
                intervals = clipped
            for bottom, top in intervals:
                if top - bottom < 0.04:
                    continue
                paint = material
                if material == "wood":
                    paint = RNG.choice(("wood", "wood", "wood_dark"))
                box("Hand laid siding", ((left + right) / 2, 1.033,
                                         (bottom + top) / 2),
                    (step - RNG.uniform(0.012, 0.025), 0.12,
                     top - bottom - 0.012), paint,
                    rotation=(0, RNG.uniform(-0.006, 0.006), 0))
    else:
        # 조적 벽은 앞면에 창 구멍을 파서 깊은 창틀을 만든다.
        body = box("Closed masonry shell", (0, -0.2, height / 2),
                   (width, 2.6, height), material, rounding=0)
        for x, z, opening_width, opening_height in openings:
            cutter = box("Temporary window pocket", (x, 1.16, z),
                         (opening_width + 0.02, 0.34, opening_height + 0.02),
                         material, rounding=0)
            bpy.context.view_layer.objects.active = body
            modifier = body.modifiers.new("Window recess", "BOOLEAN")
            modifier.operation = "DIFFERENCE"
            modifier.solver = "EXACT"
            modifier.object = cutter
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            PARTS.remove(cutter)
            bpy.data.objects.remove(cutter, do_unlink=True)
        bevel(body, bevel_width)
        # 깊은 창의 짧은 턱은 보존하고 건물 외곽에만 큰 라운딩을 적용한다.
        modifier = body.modifiers[-1]
        modifier.limit_method = "WEIGHT"
        weights = body.data.attributes.new("bevel_weight_edge", "FLOAT", "EDGE")
        limits = ((-width / 2, width / 2), (-1.5, 1.1), (0, height))
        for edge in body.data.edges:
            a, b = (body.data.vertices[index].co for index in edge.vertices)
            boundary_axes = sum(
                any(abs(a[axis] - bound) < 1e-5 and abs(b[axis] - bound) < 1e-5
                    for bound in limits[axis])
                for axis in range(3)
            )
            weights.data[edge.index].value = float(boundary_axes >= 2)


def gold_drip(x, y, top, length=0.36):
    # 덩어리와 길게 늘어난 목을 한 덩어리의 닫힌 금 방울로 연결한다.
    rings = [(0.00, 0.012, 0.010), (0.035, 0.067, 0.012),
             (0.10, 0.075, 0.008), (0.18, 0.035, 0.0),
             (length - 0.075, 0.024, -0.008),
             (length - 0.025, 0.065, -0.025),
             (length, 0.055, -0.045)]
    sides = 8
    vertices = []
    for z, radius, offset in rings:
        for index in range(sides):
            angle = math.tau * index / sides
            vertices.append((x + radius * math.cos(angle),
                             y + offset + radius * math.sin(angle),
                             top - length + z))
    faces = [tuple(reversed(range(sides)))]
    for index in range(len(rings) - 1):
        for side in range(sides):
            following = (side + 1) % sides
            faces.append((index * sides + side, index * sides + following,
                          (index + 1) * sides + following,
                          (index + 1) * sides + side))
    faces.append(tuple((len(rings) - 1) * sides + side for side in range(sides)))
    mesh("Single molten gold drip", vertices, faces, "gold_drip")


def flat_roof(width, z, material, pattern="seams"):
    box("Sealed roof deck", (0, -0.2, z - 0.055),
        (width, 2.6, 0.11), material)
    if pattern == "seams":
        count = max(4, round(width / 0.52))
        for index in range(1, count):
            x = -width / 2 + index * width / count
            box("Standing tin ridge", (x, -0.22, z + 0.025),
                (0.055, 2.45, 0.065), material, rounding=0)
    else:
        for index in range(7):
            y = -1.29 + index * 0.35
            box("Broad shingle course", (0, y, z + 0.018),
                (width - 0.12, 0.32, 0.045), material)


def slope_roof(name, x0, z0, x1, z1, material, courses=0):
    prism(name, [(x0, z0 - 0.10), (x1, z1 - 0.10),
                 (x1, z1), (x0, z0)], -1.5, 1.1, material, rounding=0)
    for index in range(courses):
        t = (index + 0.5) / courses
        x = x0 + (x1 - x0) * t
        z = z0 + (z1 - z0) * t
        box("Raised roof course", (x, -0.2, z - 0.005),
            (abs(x1 - x0) / courses * 0.88, 2.53, 0.05), material,
            rotation=(0, -math.atan2(z1 - z0, x1 - x0), 0))


def parapet(width, bottom, top, material, depth=0.18):
    box("Front parapet", (0, 1.1 - depth / 2, (bottom + top) / 2),
        (width, depth, top - bottom), material)
    box("Parapet cap", (0, 1.07, top - 0.045),
        (width, 0.25, 0.09), "wood_light")


def sign(x, z, width, height, face="wood_light", border="wood_dark"):
    box("Blank sign backing", (x, 1.215, z),
        (width, 0.09, height), face)
    ring("Blank sign border", x, z, width - 0.14, height - 0.14,
         0.07, border, back=1.22, front=1.29)


def barrel(x, y, z=0, radius=0.24, height=0.66):
    sides = 10
    rings = [(0, radius * 0.82), (0.07, radius),
             (height / 2, radius * 1.08), (height - 0.07, radius),
             (height, radius * 0.82)]
    vertices = [(x + r * math.cos(math.tau * index / sides),
                 y + r * math.sin(math.tau * index / sides), z + level)
                for level, r in rings for index in range(sides)]
    faces = [tuple(reversed(range(sides)))]
    for ring_index in range(len(rings) - 1):
        for index in range(sides):
            following = (index + 1) % sides
            faces.append((ring_index * sides + index,
                          ring_index * sides + following,
                          (ring_index + 1) * sides + following,
                          (ring_index + 1) * sides + index))
    faces.append(tuple((len(rings) - 1) * sides + index for index in range(sides)))
    mesh("Bulging stave barrel", vertices, faces, "wood")
    for level in (0.13, height - 0.13):
        cylinder("Barrel iron hoop", (x, y, z + level),
                 radius * 1.045, 0.065, "iron", sides=sides)
    box("Barrel lid seam", (x, y, z + height + 0.005),
        (radius * 1.3, 0.025, 0.016), "wood_dark", rounding=0)


def store():
    openings = [(-0.73, 1.18, 2.25, 1.05)]
    wall(5, 2.60, "paint_teal", openings)
    flat_roof(5, 2.57, "roof_tin")
    for width, bottom, top in [(5, 2.55, 2.91), (3.75, 2.84, 3.17),
                               (2.35, 3.10, 3.40)]:
        parapet(width, bottom, top, "paint_teal")
    sign(0, 2.88, 3.45, 0.48)
    window(*openings[0])
    door(1.62, width=0.82, height=1.86)
    for index in range(10):
        x0 = -2.35 + index * 0.47
        x1 = x0 + 0.47
        mesh("Sloping striped canvas", [
            (x0, 1.11, 2.45), (x1, 1.11, 2.45),
            (x1, 1.50, 2.20), (x0, 1.50, 2.20),
            (x0, 1.11, 2.41), (x1, 1.11, 2.41),
            (x1, 1.50, 2.16), (x0, 1.50, 2.16)],
            [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1),
             (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)],
            "canvas" if index % 2 == 0 else "cloth_red")
        box("Awning hanging valance", ((x0 + x1) / 2, 1.475, 2.125),
            (0.47, 0.045, 0.13 + 0.018 * math.sin(index)),
            "canvas" if index % 2 == 0 else "cloth_red", rounding=0)
    gold_drip(1.49, 1.31, 3.05)


def hotel():
    openings = [(x, 3.18, 0.78, 0.87) for x in (-2.16, -0.73, 0.73, 2.16)]
    openings.extend([(-1.94, 1.20, 1.02, 1.05), (1.94, 1.20, 1.02, 1.05)])
    wall(6, 3.91, "paint_cream", openings)
    flat_roof(6, 3.91, "roof_shingle", pattern="shingles")
    parapet(6, 3.87, 4.12, "paint_cream")
    prism("Central triangular pediment", [(-0.83, 4.03), (0.83, 4.03),
                                           (0, 4.40)], 0.92, 1.17, "wood_light")
    for index, opening in enumerate(openings):
        window(*opening, lit=index == 2)
    door(0, width=1.00, height=1.86)
    box("Balcony porch deck", (0, 1.285, 2.18),
        (5.72, 0.43, 0.15), "roof_shingle")
    for x in (-2.66, -0.90, 0.90, 2.66):
        box("Four porch posts", (x, 1.40, 1.06),
            (0.115, 0.12, 2.12), "wood_dark")
    box("Balcony top rail", (0, 1.40, 2.65),
        (5.52, 0.11, 0.10), "wood_dark")
    box("Balcony bottom rail", (0, 1.40, 2.32),
        (5.52, 0.08, 0.07), "wood_dark")
    for index in range(9):
        box("Balcony baluster", (-2.66 + index * 0.665, 1.40, 2.48),
            (0.065, 0.07, 0.38), "wood_dark")
    for x in (-2.72, 2.72):
        box("Balcony return rail", (x, 1.245, 2.65),
            (0.09, 0.40, 0.10), "wood_dark")
    box("Brick chimney", (-2.0, -0.85, 4.055), (0.48, 0.48, 0.55), "brick")
    box("Chimney crown", (-2.0, -0.85, 4.32), (0.59, 0.58, 0.11), "stone")
    box("Dark chimney throat", (-2.0, -0.85, 4.38),
        (0.32, 0.32, 0.018), "wood_dark", rounding=0)
    gold_drip(2.57, 1.23, 4.10, 0.40)


def narrow():
    openings = [(0, 2.64, 0.78, 0.91)]
    wall(3, 3.12, "paint_mustard", openings)
    prism("Closed front and rear gable", [(-1.5, 3.08), (1.5, 3.08),
                                          (0, 3.91)], -1.5, 1.10, "paint_mustard")
    slope_roof("Left red roof", -1.5, 3.15, 0, 3.97, "roof_red")
    slope_roof("Right red roof", 0, 3.97, 1.5, 3.15, "roof_red")
    for x in (-1.05, -0.52, 0.52, 1.05):
        z = 3.97 - abs(x) * 0.82 / 1.5
        box("Red roof raised seam", (x, -0.2, z + 0.015),
            (0.045, 2.55, 0.055), "roof_red", rounding=0)
    box("Red ridge cap", (0, -0.2, 3.97), (0.12, 2.6, 0.06), "roof_red")
    for side in (-1, 1):
        beam("Gable fascia", (side * 1.43, 1.145, 3.13),
             (0, 1.145, 3.92), 0.10, "wood_light")
    window(*openings[0], lit=True)
    door(-0.37, width=0.9, height=1.82)
    beam("Hanging sign iron arm", (0.97, 1.10, 2.42),
         (0.97, 1.44, 2.42), 0.065, "iron")
    beam("Bracket diagonal", (0.97, 1.10, 2.16),
         (0.97, 1.42, 2.42), 0.045, "iron")
    for x in (0.74, 1.20):
        cylinder("Sign suspension", (x, 1.34, 2.22), 0.022, 0.22, "iron", sides=6)
    box("Hanging blank sign", (0.97, 1.34, 1.98),
        (0.76, 0.12, 0.36), "wood_light", rotation=(0, -0.045, 0))
    gold_drip(1.21, 1.40, 2.12, 0.32)


def adobe():
    openings = [(-0.88, 1.28, 0.64, 0.67)]
    wall(4, 2.39, "adobe", openings, planks=False, bevel_width=0.08)
    flat_roof(3.78, 2.41, "stone", pattern="shingles")
    box("Soft front adobe parapet", (0, 0.985, 2.53),
        (4, 0.23, 0.34), "adobe", rounding=0)
    bevel(PARTS[-1], 0.08)
    for x in (-1.87, 1.87):
        box("Soft side adobe parapet", (x, -0.22, 2.51),
            (0.26, 2.56, 0.30), "adobe", rounding=0)
        bevel(PARTS[-1], 0.08)
    box("Rear adobe parapet", (0, -1.385, 2.51),
        (3.6, 0.23, 0.30), "adobe", rounding=0)
    bevel(PARTS[-1], 0.08)
    for x in (-1.43, -0.49, 0.49, 1.43):
        cylinder("Four protruding vigas", (x, 1.16, 2.21),
                 0.11, 0.47, "wood_dark", axis="Y", sides=10)
    window(*openings[0], mullion=False)
    door(0.65, width=0.90, height=1.76)
    box("Parapet rug fold", (1.35, 1.06, 2.67),
        (0.55, 0.29, 0.055), "cloth_red", rounding=0)
    prism("Hanging red rug", [(1.075, 2.68), (1.625, 2.68),
                              (1.60, 1.88), (1.10, 1.93)],
          1.155, 1.195, "cloth_red", rounding=0)
    for z in (2.04, 2.13):
        box("Woven rug stripe", (1.35, 1.205, z),
            (0.47, 0.018, 0.055), "canvas", rounding=0)
    gold_drip(-1.62, 1.16, 2.65, 0.39)


def shack():
    openings = [(-0.74, 1.22, 0.68, 0.66)]
    wall(3, 1.96, "wood", openings)
    prism("Closed lean-to upper wall", [(-1.5, 1.94), (1.5, 1.94),
                                         (1.5, 2.09), (-1.5, 2.09)],
          -1.5, 1.1, "wood_dark")
    # 뒤쪽이 높은 경사 지붕과 옆벽을 YZ 쐐기 형태로 닫는다.
    vertices = [(x, y, z) for x in (-1.5, 1.5)
                for y, z in [(-1.5, 1.94), (1.1, 1.94),
                             (1.1, 2.07), (-1.5, 2.41)]]
    mesh("Lean-to closed wedge", vertices,
         [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
          (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], "wood_dark")
    slope = -0.34 / 2.6
    angle = math.atan(slope)
    vertices = [(x, y, z) for x in (-1.5, 1.5)
                for y, z in [(-1.5, 2.36), (1.1, 2.02),
                             (1.1, 2.12), (-1.5, 2.46)]]
    mesh("Full footprint lean-to roof deck", vertices,
         [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
          (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], "roof_tin")
    for index in range(6):
        x = -1.25 + index * 0.50
        box("Lean-to corrugated tin sheet", (x, -0.2, 2.254),
            (0.50, 2.6, 0.095), "roof_tin",
            rotation=(angle, 0, 0))
        box("Lean-to chunky tin rib", (x + 0.17, -0.2, 2.309),
            (0.045, 2.52, 0.055), "roof_tin", rounding=0,
            rotation=(angle, 0, 0))
    for x, y in [(-0.80, -0.75), (0.52, 0.38)]:
        z = 2.254 + slope * (y + 0.2) + 0.067
        box("Two iron roof patches", (x, y, z),
            (0.65, 0.53, 0.035), "iron", rounding=0,
            rotation=(angle, 0, RNG.uniform(-0.06, 0.06)))
    window(*openings[0], mullion=False)
    for direction in (-1, 1):
        beam("Crossed window boarding", (-1.14, 1.19, 1.22 - direction * 0.31),
             (-0.34, 1.19, 1.22 + direction * 0.31), 0.14, "wood_light", depth=0.07)
    door(0.33, width=0.75, height=1.70, material="wood_dark")
    barrel(1.10, 1.30, radius=0.175, height=0.65)
    box("Crooked repair plank", (-1.20, 1.135, 0.62),
        (0.17, 0.06, 1.1), "wood_dark", rotation=(0, 0.10, 0))
    gold_drip(-1.26, 1.12, 2.13, 0.32)


def bank():
    openings = [(-1.22, 1.43, 0.67, 0.91), (1.22, 1.43, 0.67, 0.91)]
    wall(4, 2.94, "brick", openings, planks=False)
    flat_roof(4, 3.02, "roof_tin")
    for x in (-1.82, 1.82):
        box("Stone corner pier", (x, 1.055, 1.43),
            (0.35, 0.22, 2.86), "stone")
        for z in (0.18, 2.70):
            box("Stone pier capital", (x, 1.08, z),
                (0.36, 0.28, 0.19), "stone")
    box("Flat stone cornice", (0, 1.065, 2.96),
        (4, 0.28, 0.16), "stone")
    box("Front stone parapet", (0, 1.015, 3.17),
        (4, 0.17, 0.26), "stone")
    for x in (-1.90, 1.90):
        box("Stone roof side cap", (x, -0.2, 3.20),
            (0.20, 2.6, 0.20), "stone")
    box("Rear stone roof cap", (0, -1.405, 3.20),
        (3.62, 0.19, 0.20), "stone")
    for opening in openings:
        window(*opening, bars=True, mullion=False)
    arch = [(-0.52, 0.04), (0.52, 0.04), (0.52, 1.46)]
    arch.extend((0.52 * math.cos(math.pi * index / 12),
                 1.46 + 0.52 * math.sin(math.pi * index / 12))
                for index in range(1, 13))
    prism("Closed arched bank door", arch, 1.075, 1.16, "wood_dark")
    # 두께가 있는 반원 띠와 양쪽 기둥이 황동 아치를 이룬다.
    outer = [(0.62 * math.cos(math.pi * index / 12),
              1.46 + 0.62 * math.sin(math.pi * index / 12))
             for index in range(13)]
    inner = [(0.52 * math.cos(math.pi * index / 12),
              1.46 + 0.52 * math.sin(math.pi * index / 12))
             for index in range(12, -1, -1)]
    prism("Brass arch surround", outer + inner, 1.11, 1.23, "brass", rounding=0)
    for x in (-0.57, 0.57):
        box("Brass door jamb", (x, 1.17, 0.76),
            (0.10, 0.12, 1.44), "brass")
    for x in (-0.33, -0.11, 0.11, 0.33):
        box("Bank door inset panel", (x, 1.17, 0.76),
            (0.19, 0.055, 1.33), "wood")
    cylinder("Bank brass handle", (0.30, 1.23, 0.85),
             0.055, 0.06, "brass", axis="Y", sides=8)
    sign(0, 2.51, 1.50, 0.34, face="brass", border="stone")
    gold_drip(0.61, 1.32, 2.65, 0.33)


def barn():
    wall(5, 2.36, "paint_red")
    outline = [(-2.5, 2.30), (2.5, 2.30), (1.43, 3.25),
               (0, 3.73), (-1.43, 3.25)]
    prism("Closed gambrel upper walls", outline, -1.5, 1.10, "paint_red")
    stations = [(-2.5, 2.40), (-1.43, 3.30), (0, 3.78),
                (1.43, 3.30), (2.5, 2.40)]
    for index in range(4):
        x0, z0 = stations[index]
        x1, z1 = stations[index + 1]
        slope_roof("Gambrel shingle slope", x0, z0, x1, z1,
                   "roof_shingle", courses=3)
        beam("Cream gambrel fascia", (x0 * 0.97, 1.145, z0 - 0.045),
             (x1 * 0.97, 1.145, z1 - 0.045), 0.085, "paint_cream")
    box("Gambrel ridge cap", (0, -0.2, 3.77),
        (0.14, 2.6, 0.06), "roof_shingle")
    door(0, width=2.75, height=2.12, material="paint_red", double=True)
    for direction in (-1, 1):
        beam("Big cream door X brace", (-1.28, 1.245, 1.10 - direction * 0.92),
             (1.28, 1.245, 1.10 + direction * 0.92),
             0.12, "paint_cream", depth=0.075)
    for x in (-2.37, 2.37):
        box("Cream corner trim", (x, 1.12, 1.16),
            (0.14, 0.12, 2.32), "paint_cream")
    box("Closed hay loft door", (0, 1.14, 2.87),
        (0.77, 0.12, 0.67), "wood_dark")
    ring("Hay loft cream frame", 0, 2.87, 0.77, 0.67,
         0.075, "paint_cream", front=1.22)
    box("Loft door stile", (0, 1.225, 2.87),
        (0.065, 0.045, 0.62), "wood_light")
    beam("Pulley beam", (0, 0.98, 3.40), (0, 1.43, 3.40), 0.14, "wood_dark")
    cylinder("Loft pulley wheel", (0, 1.39, 3.21),
             0.105, 0.065, "iron", axis="X", sides=10)
    gold_drip(1.34, 1.21, 3.30, 0.38)


def blue():
    openings = [(-0.74, 1.18, 1.10, 0.96)]
    wall(4, 2.36, "paint_blue", openings)
    flat_roof(4, 2.40, "roof_tin", pattern="shingles")
    curve = [(-2, 2.29), (2, 2.29)]
    curve.extend((2 * math.cos(math.pi * index / 16),
                  2.53 + 0.42 * math.sin(math.pi * index / 16))
                 for index in range(17))
    prism("Arched blue false front", curve, 0.91, 1.10, "paint_blue")
    cap_outer = [(2 * math.cos(math.pi * index / 16),
                  2.58 + 0.42 * math.sin(math.pi * index / 16))
                 for index in range(17)]
    cap_inner = [(2 * math.cos(math.pi * index / 16),
                  2.49 + 0.42 * math.sin(math.pi * index / 16))
                 for index in range(16, -1, -1)]
    prism("Curved parapet cap", cap_outer + cap_inner,
          0.90, 1.17, "wood_light", rounding=0)
    window(*openings[0], lit=True)
    door(0.81, width=0.86, height=1.76)
    box("Small tin porch roof", (0, 1.28, 2.15),
        (3.28, 0.42, 0.12), "roof_tin", rotation=(-0.12, 0, 0))
    for x in (-1.48, 1.48):
        box("Two porch posts", (x, 1.405, 1.04),
            (0.12, 0.12, 2.08), "wood_dark")
    for x in (-0.93, 0, 0.93):
        box("Porch tin batten", (x, 1.28, 2.22),
            (0.05, 0.39, 0.04), "roof_tin", rounding=0,
            rotation=(-0.12, 0, 0))
    for index in range(8):
        cylinder("Barber pole band", (1.78, 1.32, 0.37 + index * 0.14),
                 0.105, 0.14, "cloth_red" if index % 2 == 0 else "paint_cream")
    for z in (0.24, 1.45):
        cylinder("Barber pole end cap", (1.78, 1.32, z), 0.145, 0.12, "brass")
    gold_drip(-1.25, 1.20, 2.88, 0.37)


def fence():
    for x in (-0.91, 0.91):
        box("Fence boundary post", (x, 0, 1), (0.18, 0.34, 2), "wood_dark")
    for z in (0.48, 1.43):
        box("Fence cross rail", (0, -0.12, z), (1.92, 0.16, 0.15), "wood_dark")
    for index in range(7):
        if index == 3:
            continue
        x = -0.72 + index * 0.24
        top = RNG.uniform(1.76, 1.94)
        box("Crooked fence plank", (x, 0.055, top / 2 + 0.015),
            (0.22, 0.16, top - 0.03), RNG.choice(("wood", "wood_dark")),
            rotation=(0, RNG.uniform(-0.033, 0.033), RNG.uniform(-0.018, 0.018)))
    gold_drip(0.91, 0.11, 1.94, 0.35)


def finish(name, width, height, depth, seed):
    assert sum(part.name.startswith("Single molten gold drip") for part in PARTS) == 1
    bpy.ops.object.select_all(action="DESELECT")
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
    obj.data.name = name + "_Mesh"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), f"{name} has an open component"
    assert all(face.calc_area() > 1e-10 for face in bm.faces), f"{name} has a zero-area face"
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    coords = [vertex.co for vertex in obj.data.vertices]
    low = Vector(tuple(min(co[axis] for co in coords) for axis in range(3)))
    high = Vector(tuple(max(co[axis] for co in coords) for axis in range(3)))
    dims = high - low
    assert abs(dims.x - width) <= 0.02, f"{name} width {dims.x}"
    assert low.x >= -width / 2 - 1e-5 and high.x <= width / 2 + 1e-5, (name, low, high)
    back, front = (-0.2, 0.2) if depth == 0.4 else (-1.5, 1.5)
    assert low.y >= back - 1e-5 and high.y <= front + 1e-5, (name, low, high)
    assert dims.y <= depth + 1e-5, f"{name} depth {dims.y}"
    assert low.z >= -1e-5 and height - 0.02 <= high.z <= height + 1e-5, (name, low, high)
    assert triangles < 5000, f"{name} exceeds 5000 triangles with {triangles}"
    assert all(math.isfinite(value) for co in coords for value in co)
    assert obj.location.length < 1e-6
    obj["front_axis"] = "+Y"
    obj["seed"] = seed
    obj["footprint_width"] = width
    obj["gold_drip_count"] = 1
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = OUTPUT / (name + ".fbx")
    result = bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False,
        use_custom_props=True,
    )
    assert "FINISHED" in result and destination.stat().st_size > 0, name
    print(f"EXPORTED {name} {triangles} {dims.x:.4f} {dims.y:.4f} {dims.z:.4f}", flush=True)
    PARTS.clear()


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    MATERIALS.clear()
    PARTS.clear()
    for name, color in COLORS.items():
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        material.diffuse_color = (*color, 1)
        material.node_tree.nodes.clear()
        shader = material.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        shader.inputs["Base Color"].default_value = (*color, 1)
        shader.inputs["Roughness"].default_value = 0.78
        output = material.node_tree.nodes.new("ShaderNodeOutputMaterial")
        material.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
        MATERIALS[name] = material
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    OUTPUT.mkdir(parents=True, exist_ok=True)
    builders = [
        ("Kit_Facade_Store", store, 5.0, 3.4, 3.0),
        ("Kit_Facade_Hotel", hotel, 6.0, 4.4, 3.0),
        ("Kit_Facade_Narrow", narrow, 3.0, 4.0, 3.0),
        ("Kit_Facade_Adobe", adobe, 4.0, 2.7, 3.0),
        ("Kit_Facade_Shack", shack, 3.0, 2.5, 3.0),
        ("Kit_Facade_Bank", bank, 4.0, 3.3, 3.0),
        ("Kit_Facade_Barn", barn, 5.0, 3.8, 3.0),
        ("Kit_Facade_Blue", blue, 4.0, 3.0, 3.0),
        ("Kit_AlleyFence", fence, 2.0, 2.0, 0.4),
    ]
    for index, (name, builder, width, height, depth) in enumerate(builders):
        seed = 170530 + index
        RNG.seed(seed)
        builder()
        finish(name, width, height, depth, seed)
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
