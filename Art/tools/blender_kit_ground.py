# 챕터 1의 먼지 낀 서부 바닥에 흩뿌릴 낮은 장식 5종을 개별 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ground.py

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1"
SEED = 27183
RNG = random.Random(SEED)
PARTS = []
PALETTE = {
    "wood_dark": (0.33, 0.19, 0.12),
    "wood": (0.55, 0.33, 0.19),
    "wood_light": (0.72, 0.48, 0.28),
    "rock": (0.62, 0.32, 0.22),
    "rock_dark": (0.45, 0.22, 0.16),
    "sand_light": (0.93, 0.7, 0.45),
    "grass_dry": (0.85, 0.68, 0.32),
    "grass_dark": (0.62, 0.46, 0.2),
    "iron": (0.24, 0.22, 0.25),
    "bone": (0.92, 0.86, 0.74),
}
MATERIALS = {}


def setup():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    for name, color in PALETTE.items():
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (*color, 1.0)
        shader = mat.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*color, 1.0)
        shader.inputs["Roughness"].default_value = 0.78
        MATERIALS[name] = mat
    OUTPUT.mkdir(parents=True, exist_ok=True)


def register(obj, material):
    obj.data.materials.append(MATERIALS[material])
    PARTS.append(obj)
    return obj


def bevel(obj, width=0.012):
    mod = obj.modifiers.new("모서리 닳음", "BEVEL")
    mod.width = width
    mod.segments = 2
    return obj


def mesh_part(vertices, faces, material):
    mesh = bpy.data.meshes.new("primitive_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("primitive", mesh)
    bpy.context.collection.objects.link(obj)
    return register(obj, material)


def extrude(outline, bottom, top, material):
    vertices = [(x, y, z) for z in (bottom, top) for x, y in outline]
    n = len(outline)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n)
              for j in range(n)]
    return mesh_part(vertices, faces, material)


def cylinder(location, radius, depth, material, vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location)
    return register(bpy.context.object, material)


def ground_planks():
    boards = [
        ((-.04, -.28, 0), 2.10, .31, .12, "wood", False),
        ((.05, .04, .082), 1.94, .30, -.38, "wood_light", False),
        ((-.22, .22, .164), 1.22, .34, .57, "wood", True),
    ]
    for center, length, width, angle, material, broken in boards:
        x, y, z = center
        half_length, half_width = length / 2, width / 2
        outline = [(-half_length, -half_width * .88),
                   (-half_length + .09, -half_width),
                   (half_length - .05, -half_width * .94)]
        if broken:
            outline += [(half_length - .13, -half_width * .35),
                        (half_length, -half_width * .10),
                        (half_length - .15, half_width * .40),
                        (half_length - .04, half_width * .91)]
        else:
            outline += [(half_length, -half_width * .70),
                        (half_length - .035, half_width * .90)]
        outline += [(-half_length + .06, half_width),
                    (-half_length - .025, half_width * .55)]
        outline = [(px + RNG.uniform(-.012, .012),
                    py + RNG.uniform(-.008, .008)) for px, py in outline]
        before = len(PARTS)
        bevel(extrude(outline, 0, .08, material))
        # 넓은 갈라짐 하나로 위에서 보아도 낡은 판자임을 알아볼 수 있게 합니다.
        split = [(-half_length + .04, -.045),
                 (-half_length + length * .48, -.010),
                 (-half_length + .07, -.014)]
        extrude(split, .079, .081, "wood_dark")
        if broken:
            for nail_x in (-.32, .22):
                cylinder((nail_x, .035, .085), .028, .010, "iron")
        for obj in PARTS[before:]:
            obj.location = Vector((x, y, z)) + Vector((
                math.cos(angle) * obj.location.x - math.sin(angle) * obj.location.y,
                math.sin(angle) * obj.location.x + math.cos(angle) * obj.location.y,
                obj.location.z))
            obj.rotation_euler.z = angle


def pebbles():
    centers = [(-.48, -.30), (-.06, -.43), (.36, -.33), (.50, .04),
               (.27, .40), (-.14, .44), (-.48, .18), (-.19, .00),
               (.13, .06), (.04, .29)]
    for index, (x, y) in enumerate(centers):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1)
        obj = register(bpy.context.object,
                       "rock_dark" if index in (2, 6, 8) else "rock")
        width = RNG.uniform(.20, .36)
        depth = width * RNG.uniform(.72, 1.05)
        height = .25 if index == 0 else RNG.uniform(.09, .21)
        for vertex in obj.data.vertices:
            vertex.co *= RNG.uniform(.85, 1.13)
            vertex.co.z = max(vertex.co.z, -.48)
        for axis, dimension in enumerate((width, depth, height)):
            values = [v.co[axis] for v in obj.data.vertices]
            lo, hi = min(values), max(values)
            for vertex in obj.data.vertices:
                vertex.co[axis] = (vertex.co[axis] - lo) / (hi - lo) * dimension
                if axis < 2:
                    vertex.co[axis] -= dimension / 2
        obj.location = (x + RNG.uniform(-.025, .025),
                        y + RNG.uniform(-.025, .025), 0)
        obj.rotation_euler.z = RNG.uniform(-math.pi, math.pi)


def grass_blade(center, angle, lean, height, width, material):
    # 납작한 마름모 단면을 가진 휘어진 원뿔로 양면에서 보이는 잎을 만듭니다.
    direction = Vector((math.cos(angle), math.sin(angle), 0))
    across = Vector((-direction.y, direction.x, 0))
    vertices = []
    for t, taper in ((0, .72), (.40, 1.0), (.76, .52)):
        position = Vector(center) + direction * (lean * t ** 1.4)
        position.z += height * t
        for side, thickness in ((-1, 0), (0, -1), (1, 0), (0, 1)):
            point = position + across * (side * width * taper / 2)
            point += direction * (thickness * .009 * taper)
            vertices.append(tuple(point))
    vertices.append(tuple(Vector(center) + direction * lean + Vector((0, 0, height))))
    faces = [tuple(reversed(range(4)))]
    for layer in range(2):
        for j in range(4):
            a, b = layer * 4 + j, layer * 4 + (j + 1) % 4
            faces.append((a, b, b + 4, a + 4))
    faces += [(8 + j, 8 + (j + 1) % 4, 12) for j in range(4)]
    mesh_part(vertices, faces, material)


def dry_grass():
    centers = [(-.23, -.16, 0), (.22, -.13, 0),
               (-.14, .22, 0), (.23, .21, 0)]
    for tuft, center in enumerate(centers):
        for blade in range(6):
            angle = math.tau * blade / 6 + RNG.uniform(-.24, .24)
            height = .45 if (tuft, blade) == (0, 0) else RNG.uniform(.24, .42)
            grass_blade(center, angle, RNG.uniform(.14, .29), height,
                        RNG.uniform(.055, .085),
                        "grass_dark" if blade % 3 == 0 else "grass_dry")


def sand_drift():
    n = 32
    outline = []
    for j in range(n):
        angle = math.tau * j / n
        radius = (1 + .075 * math.sin(3 * angle + .5)
                  + .045 * math.sin(7 * angle) + RNG.uniform(-.018, .018))
        outline.append((1.2 * radius * math.cos(angle),
                        .8 * radius * math.sin(angle)))
    vertices = []
    for fraction in (1, .74, .46, .20):
        for x, y in outline:
            ripple = .014 * math.sin(y * 14 * fraction + x * fraction * 2)
            height = .006 + .106 * (1 - fraction ** 1.7)
            height += ripple * (1 - fraction)
            vertices.append((x * fraction, y * fraction, height))
    vertices.append((0, 0, .12))
    center = len(vertices) - 1
    bottom = len(vertices)
    vertices += [(x, y, 0) for x, y in outline]
    faces = []
    for layer in range(3):
        for j in range(n):
            a, b = layer * n + j, layer * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    faces += [(3 * n + j, 3 * n + (j + 1) % n, center) for j in range(n)]
    faces += [(j, bottom + j, bottom + (j + 1) % n, (j + 1) % n)
              for j in range(n)]
    faces.append(tuple(reversed(range(bottom, bottom + n))))
    mesh_part(vertices, faces, "sand_light")


def horn(side):
    profile = [(.135, .16, .16, .062), (.22, .18, .15, .056),
               (.30, .23, .14, .043), (.35, .31, .15, .028),
               (.35, .39, .18, .014)]
    vertices = []
    n = 6
    points = [Vector((side * x, y, z)) for x, y, z, radius in profile]
    tip = Vector((side * .30, .45, .22))
    for index, point in enumerate(points):
        previous = points[max(0, index - 1)]
        following = points[index + 1] if index + 1 < len(points) else tip
        tangent = (following - previous).normalized()
        across = tangent.cross(Vector((0, 0, 1))).normalized()
        up = tangent.cross(across).normalized()
        radius = profile[index][3]
        for j in range(n):
            angle = math.tau * j / n
            vertex = point + radius * (across * math.cos(angle) + up * math.sin(angle))
            vertices.append(tuple(vertex))
    vertices.append(tuple(tip))
    faces = [tuple(reversed(range(n)))]
    for layer in range(len(profile) - 1):
        for j in range(n):
            a, b = layer * n + j, layer * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    last = (len(profile) - 1) * n
    faces += [(last + j, last + (j + 1) % n, len(vertices) - 1)
              for j in range(n)]
    mesh_part(vertices, faces, "bone")


def recess(obj, outline, floor):
    cutter = extrude(outline, floor, .5, "wood_dark")
    PARTS.remove(cutter)
    # Boolean operands require consistent winding before the final mesh cleanup.
    for target in (obj, cutter):
        bm = bmesh.new()
        bm.from_mesh(target.data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(target.data)
        bm.free()
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new("뼈의 오목한 구멍", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.solver = "EXACT"
    mod.object = cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    cutter_mesh = cutter.data
    bpy.data.objects.remove(cutter, do_unlink=True)
    bpy.data.meshes.remove(cutter_mesh)
    center = Vector((sum(x for x, y in outline) / len(outline),
                     sum(y for x, y in outline) / len(outline)))
    inset = [(center.x + (x - center.x) * .97,
              center.y + (y - center.y) * .97) for x, y in outline]
    extrude(inset, floor - .006, floor + .001, "wood_dark")


def skull():
    # 넓은 이마, 좁은 주둥이, 땅과 평행한 두 뿔로 소의 두개골 실루엣을 만듭니다.
    outline = [(-.075, -.33), (.075, -.33), (.10, -.25), (.095, -.13),
               (.16, .005), (.18, .13), (.145, .225), (.065, .25),
               (-.065, .25), (-.145, .225), (-.18, .13),
               (-.16, .005), (-.095, -.13), (-.10, -.25)]
    n = len(outline)
    vertices = [(x, y, 0) for x, y in outline]
    vertices += [(x, y, .16 if y < -.13 else .25) for x, y in outline]
    vertices.append((0, .10, .30))
    faces = [tuple(reversed(range(n)))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    faces += [(n + j, n + (j + 1) % n, 2 * n) for j in range(n)]
    head = mesh_part(vertices, faces, "bone")
    for side in (-1, 1):
        eye = []
        for j in range(8):
            angle = math.tau * j / 8
            eye.append((side * (.098 + .061 * math.cos(angle)),
                        .065 + .077 * math.sin(angle) + .012 * math.cos(angle)))
        recess(head, eye, .105)
        horn(side)
    recess(head, [(-.036, -.225), (.036, -.225), (0, -.145)], .075)


def finish(name, footprint, max_height):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in PARTS:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    if len(PARTS) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + "_Mesh"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    coords = [v.co for v in obj.data.vertices]
    low = Vector(tuple(min(v[axis] for v in coords) for axis in range(3)))
    high = Vector(tuple(max(v[axis] for v in coords) for axis in range(3)))
    offset = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
    for vertex in obj.data.vertices:
        vertex.co -= offset
        vertex.co.x *= footprint[0] / (high.x - low.x)
        vertex.co.y *= footprint[1] / (high.y - low.y)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=0.000001, edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), name + ": open mesh"
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    assert triangles < 1500, f"{name}: {triangles} triangles exceeds budget"
    assert obj.type == "MESH" and obj.location.length < 1e-6
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(p.area > 1e-12 for p in obj.data.polygons), name + ": empty face"
    assert min(v.co.z for v in obj.data.vertices) >= -1e-6
    assert max(v.co.z for v in obj.data.vertices) <= max_height + 1e-6, name
    obj["front_axis"] = "+Y"
    obj["seed"] = SEED
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = OUTPUT / (name + ".fbx")
    result = bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
        object_types={"MESH"}, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, bake_anim=False, use_custom_props=True,
        path_mode="AUTO")
    assert "FINISHED" in result and destination.stat().st_size > 0
    print(f"EXPORTED {name} {triangles}", flush=True)
    PARTS.clear()


def main():
    setup()
    builders = [
        ("Kit_GroundPlanks", ground_planks, (2.2, 1.2), .35),
        ("Kit_Pebbles", pebbles, (1.4, 1.3), .25),
        ("Kit_DryGrass", dry_grass, (1.0, 1.0), .45),
        ("Kit_SandDrift", sand_drift, (2.4, 1.6), .12),
        ("Kit_Skull", skull, (.8, .78), .30),
    ]
    for index, (name, builder, footprint, max_height) in enumerate(builders):
        RNG.seed(SEED + index)
        builder()
        finish(name, footprint, max_height)
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
