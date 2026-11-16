# 챕터 1 소품: 굵직한 형태와 녹아내리는 금빛 포인트를 생성하고 내보낸다.
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


RNG = random.Random(17031)
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Game/Assets/Art/Characters/Ch1"
COLORS = {
    "wood_dark": (0.33, 0.19, 0.12),
    "wood": (0.55, 0.33, 0.19),
    "wood_light": (0.72, 0.48, 0.28),
    "iron": (0.24, 0.22, 0.25),
    "brass": (0.85, 0.62, 0.25),
    "gold_drip": (1.0, 0.78, 0.22),
    "barrel_red": (0.72, 0.14, 0.12),
    "fuse": (0.85, 0.75, 0.55),
    "spark": (1.0, 0.9, 0.35),
}
MATS = {}


def mesh_object(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(MATS[material])
    return obj


def box(name, center, size, material, rotation=(0, 0, 0), bevel=False):
    x, y, z = (v / 2 for v in size)
    vertices = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(3, 2, 1, 0), (0, 1, 5, 4), (1, 2, 6, 5),
             (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)]
    obj = mesh_object(name, vertices, faces, material)
    obj.location = center
    obj.rotation_euler = rotation
    if bevel:
        # 긴 모서리 네 개만 둥글게 하여 판재 실루엣과 삼각형 예산을 지킨다.
        axis = max(range(3), key=lambda i: size[i])
        weights = obj.data.attributes.new("bevel_weight_edge", "FLOAT", "EDGE")
        for edge in obj.data.edges:
            a, b = (obj.data.vertices[i].co for i in edge.vertices)
            weights.data[edge.index].value = float(abs(a[axis] - b[axis]) > 0.001)
        mod = obj.modifiers.new("Wood edge bevel", "BEVEL")
        mod.width = RNG.uniform(0.015, 0.023)
        mod.segments = 2
        mod.limit_method = "WEIGHT"
    return obj


def beam(name, start, end, width, depth, material, bevel=False):
    a, b = Vector(start), Vector(end)
    obj = box(name, (a + b) / 2, (width, depth, (b - a).length), material,
              bevel=bevel)
    obj.rotation_euler = (b - a).to_track_quat("Z", "Y").to_euler()
    return obj


def lathe(name, rings, material, sides=16, wobble=0.0):
    offsets = [RNG.uniform(-wobble, wobble) for _ in range(sides)]
    vertices = []
    for z, radius in rings:
        for i in range(sides):
            angle = 2 * math.pi * i / sides
            r = radius + offsets[i]
            vertices.append((r * math.cos(angle), r * math.sin(angle), z))
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings) - 1):
        for i in range(sides):
            n = (i + 1) % sides
            faces.append((j * sides + i, j * sides + n,
                          (j + 1) * sides + n, (j + 1) * sides + i))
    faces.append(tuple((len(rings) - 1) * sides + i for i in range(sides)))
    return mesh_object(name, vertices, faces, material)


def tube(name, points, radii, material, sides=7):
    points = [Vector(p) for p in points]
    vertices = []
    for i, point in enumerate(points):
        tangent = points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]
        tangent.normalize()
        reference = Vector((0, 1, 0))
        if abs(tangent.dot(reference)) > 0.95:
            reference = Vector((1, 0, 0))
        u = tangent.cross(reference).normalized()
        v = tangent.cross(u).normalized()
        for j in range(sides):
            angle = 2 * math.pi * j / sides
            vertices.append(point + radii[i] * (u * math.cos(angle) + v * math.sin(angle)))
    faces = [tuple(reversed(range(sides)))]
    for i in range(len(points) - 1):
        for j in range(sides):
            n = (j + 1) % sides
            faces.append((i * sides + j, i * sides + n,
                          (i + 1) * sides + n, (i + 1) * sides + j))
    faces.append(tuple((len(points) - 1) * sides + j for j in range(sides)))
    return mesh_object(name, vertices, faces, material)


def blob(name, center, scale, material):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=center)
    obj = bpy.context.object
    obj.name = name
    for vertex in obj.data.vertices:
        vertex.co.x *= scale[0]
        vertex.co.y *= scale[1]
        vertex.co.z *= scale[2]
    obj.data.materials.append(MATS[material])
    return obj


def extrude_xz(name, polygon, y, thickness, material):
    count = len(polygon)
    vertices = [(x, y + dy, z) for dy in (-thickness / 2, thickness / 2)
                for x, z in polygon]
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    for i in range(count):
        j = (i + 1) % count
        faces.append((i, j, j + count, i + count))
    return mesh_object(name, vertices, faces, material)


def keg():
    pieces = [lathe("Bulging powder barrel",
                    [(0, 0.295), (0.06, 0.325), (0.26, 0.36),
                     (0.50, 0.375), (0.75, 0.36), (0.94, 0.325), (0.98, 0.30)],
                    "barrel_red", wobble=0.003)]
    for z, radius in [(0.15, 0.348), (0.50, 0.387), (0.84, 0.353)]:
        pieces.append(lathe("Iron hoop", [(z - 0.035, radius - 0.006),
                                         (z - 0.025, radius), (z + 0.025, radius),
                                         (z + 0.035, radius - 0.006)], "iron"))
    lid = lathe("Inset lid", [(0.972, 0.298), (1.0, 0.298)], "wood_dark")
    lid_bevel = lid.modifiers.new("Wood lid bevel", "BEVEL")
    lid_bevel.width = 0.015
    lid_bevel.segments = 2
    pieces.append(lid)
    # 굵은 번개는 앞쪽에서 즉시 읽히도록 통짜로 돌출시킨다.
    pieces.append(extrude_xz("Powder lightning",
                            [(0.015, 0.80), (-0.145, 0.53), (-0.035, 0.53),
                             (-0.095, 0.27), (0.155, 0.59), (0.035, 0.59),
                             (0.115, 0.80)], 0.382, 0.035, "brass"))
    pieces.append(lathe("Fuse socket", [(0.99, 0.065), (1.035, 0.055)], "iron", 10))
    fuse_points = [(0, 0, 1.025), (0.015, 0, 1.12), (0.07, 0.01, 1.20),
                   (0.16, 0.018, 1.24), (0.245, 0.02, 1.225), (0.29, 0.02, 1.19)]
    pieces.append(tube("Curved fuse", fuse_points, [0.025] * len(fuse_points), "fuse"))
    star = []
    for i in range(10):
        angle = math.pi / 2 + i * math.pi / 5
        radius = 0.105 if i % 2 == 0 else 0.043
        star.append((0.29 + radius * math.cos(angle), 1.19 + radius * math.sin(angle)))
    pieces.append(extrude_xz("Lit fuse star", star, 0.02, 0.035, "spark"))
    pieces.append(tube("Gold from keg lid", [(-0.18, 0.21, 1.005), (-0.22, 0.24, 0.98),
                                            (-0.24, 0.255, 0.88), (-0.25, 0.27, 0.72),
                                            (-0.245, 0.28, 0.65)],
                       [0.065, 0.052, 0.036, 0.028, 0.035], "gold_drip"))
    pieces.append(blob("Heavy gold drop", (-0.245, 0.28, 0.65), (0.047, 0.043, 0.065), "gold_drip"))
    return pieces


def crate(size, center, angle):
    w, d, h = size
    parts = []
    t = 0.11 if w > 1 else 0.09
    # 세 판으로 나뉜 앞면, 옆면, 윗면과 단순한 뒷면을 조립한다.
    for i in range(3):
        x = (i - 1) * (w - 0.05) / 3
        y = (i - 1) * (d - 0.05) / 3
        color = "wood_light" if i == 1 else "wood"
        parts.append(box("Front plank", (x, d / 2 - 0.035, 0),
                         ((w - 0.065) / 3, 0.065, h - 0.07), color,
                         rotation=(0, RNG.uniform(-0.006, 0.006), 0), bevel=True))
        parts.append(box("Side plank", (w / 2 - 0.035, y, 0),
                         (0.065, (d - 0.065) / 3, h - 0.07), color,
                         rotation=(RNG.uniform(-0.006, 0.006), 0, 0), bevel=True))
        parts.append(box("Lid plank", (x, 0, h / 2 - 0.035),
                         ((w - 0.065) / 3, d - 0.06, 0.065), color,
                         rotation=(0, 0, RNG.uniform(-0.006, 0.006)), bevel=True))
    parts.append(box("Back panel", (0, -d / 2 + 0.035, 0),
                     (w - 0.07, 0.065, h - 0.07), "wood", bevel=True))
    parts.append(box("Left panel", (-w / 2 + 0.035, 0, 0),
                     (0.065, d - 0.07, h - 0.07), "wood", bevel=True))
    parts.append(box("Base panel", (0, 0, -h / 2 + 0.035),
                     (w - 0.06, d - 0.06, 0.065), "wood_dark", bevel=True))
    for x in (-w / 2 + t / 2, w / 2 - t / 2):
        for y in (-d / 2 + t / 2, d / 2 - t / 2):
            parts.append(box("Corner timber", (x, y, 0), (t, t, h), "wood_dark", bevel=True))
    for z in (-h / 2 + t / 2, h / 2 - t / 2):
        for y in (-d / 2 + 0.025, d / 2 - 0.025):
            parts.append(box("Front back rim", (0, y, z), (w, 0.10, t), "wood_dark", bevel=True))
        for x in (-w / 2 + 0.025, w / 2 - 0.025):
            parts.append(box("Side rim", (x, 0, z), (0.10, d - 0.10, t), "wood_dark", bevel=True))
    for direction in (-1, 1):
        parts.append(beam("Front X brace", (-w * 0.38, d / 2 + 0.025, -direction * h * 0.35),
                          (w * 0.38, d / 2 + 0.025, direction * h * 0.35),
                          t * 0.85, 0.06, "wood_light", True))
        parts.append(beam("Side X brace", (w / 2 + 0.025, -d * 0.36, -direction * h * 0.35),
                          (w / 2 + 0.025, d * 0.36, direction * h * 0.35),
                          0.06, t * 0.85, "wood_light", True))
    transform = Matrix.Translation(Vector(center)) @ Matrix.Rotation(angle, 4, "Z")
    bpy.context.view_layer.update()
    for part in parts:
        part.matrix_world = transform @ part.matrix_world
    return parts, transform


def crates():
    bottom, _ = crate((1.1, 1.0, 0.9), (0, 0, 0.45), 0)
    top, transform = crate((0.8, 0.8, 0.8), (0.06, 0.015, 1.30), math.radians(12))
    points = [(0.13, 0.32, 0.405), (0.14, 0.415, 0.38), (0.15, 0.44, 0.25),
              (0.17, 0.445, 0.07), (0.18, 0.45, -0.02)]
    drip = tube("Crate lid gold spill", points, [0.075, 0.065, 0.04, 0.026, 0.036], "gold_drip")
    drop = blob("Crate gold droplet", points[-1], (0.045, 0.043, 0.065), "gold_drip")
    bpy.context.view_layer.update()
    for obj in (drip, drop):
        obj.matrix_world = transform @ obj.matrix_world
    return bottom + top + [drip, drop]


def fence():
    parts = []
    for i, x in enumerate((-1.08, 0, 1.08)):
        height = (0.96, 1.0, 0.91)[i]
        post = box("Uneven fence post", (x, 0, height / 2), (0.24, 0.24, height),
                   "wood_dark", bevel=True)
        # 윗면 꼭짓점 높이를 달리하여 부러진 나무 끝을 표현한다.
        for vertex in post.data.vertices:
            if vertex.co.z > 0:
                vertex.co.z -= RNG.uniform(0.0, 0.065)
        parts.append(post)
        parts.append(box("Broken top splinter", (x - 0.055, 0.01, height - 0.035),
                         (0.065, 0.105, 0.07), "wood", rotation=(0, 0.12, 0), bevel=True))
    parts.append(beam("Lower fence rail", (-1.18, 0.045, 0.32), (1.18, 0.045, 0.32),
                      0.20, 0.15, "wood", True))
    parts.append(beam("Leaning upper rail", (-1.18, 0.045, 0.76), (1.18, 0.045, 0.83),
                      0.20, 0.15, "wood_light", True))
    for x in (-1.08, 0, 1.08):
        for z in (0.32, 0.795 + x * 0.07 / 2.36):
            parts.append(box("Rail iron strap", (x, 0.132, z), (0.105, 0.035, 0.25), "iron"))
    return parts


def finish_export(name, pieces):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in pieces:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    for obj in pieces:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    # 원점은 바닥 외곽의 중심이며 접지면은 항상 Z=0이다.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    low = Vector(tuple(min(v.co[i] for v in obj.data.vertices) for i in range(3)))
    high = Vector(tuple(max(v.co[i] for v in obj.data.vertices) for i in range(3)))
    offset = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
    for vertex in obj.data.vertices:
        vertex.co -= offset
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    assert triangles < 3000, f"{name}: triangle budget exceeded ({triangles})"
    assert all(math.isfinite(v) for vertex in obj.data.vertices for v in vertex.co)
    assert min(vertex.co.z for vertex in obj.data.vertices) >= -0.0001
    assert len([o for o in bpy.context.selected_objects if o.type == "MESH"]) == 1
    target = OUT / (name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(target), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False,
    )
    assert target.is_file() and target.stat().st_size > 0
    print(f"EXPORTED {name} {triangles}", flush=True)


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    for name, rgb in COLORS.items():
        material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        material.use_nodes = True
        material.diffuse_color = (*rgb, 1)
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*rgb, 1)
        shader.inputs["Roughness"].default_value = 0.78
        shader.inputs["Metallic"].default_value = 0.0
        MATS[name] = material
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build in (("Kit_Keg", keg), ("Kit_Crates", crates), ("Kit_Fence", fence)):
        finish_export(name, build())
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
