# 녹아내리는 서부 마을용 저폴리 모듈 13종을 만들고 개별 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_simple.py

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
    "iron": (0.24, 0.22, 0.25),
    "brass": (0.85, 0.62, 0.25),
    "gold_drip": (1.0, 0.78, 0.22),
    "sand": (0.82, 0.62, 0.4),
    "rock": (0.62, 0.32, 0.22),
    "rock_dark": (0.45, 0.22, 0.16),
    "cloth_red": (0.7, 0.15, 0.13),
    "canvas": (0.85, 0.76, 0.6),
    "water": (0.3, 0.55, 0.65),
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


def bevel(obj, width=0.022):
    mod = obj.modifiers.new("모서리 둥글림", "BEVEL")
    mod.width = width
    mod.segments = 2
    return obj


def box(location, size, material, rotation=(0, 0, 0), worn=False,
        rounding=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler = rotation
    if worn:
        obj.rotation_euler.x += RNG.uniform(-0.012, 0.012)
        obj.rotation_euler.y += RNG.uniform(-0.015, 0.015)
        obj.rotation_euler.z += RNG.uniform(-0.012, 0.012)
    register(obj, material)
    if material.startswith("wood") or rounding is not None:
        bevel(obj, rounding if rounding is not None else 0.022)
    return obj


def mesh_part(vertices, faces, material):
    mesh = bpy.data.meshes.new("primitive_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("primitive", mesh)
    bpy.context.collection.objects.link(obj)
    return register(obj, material)


def cylinder(location, radius, depth, material, vertices=12,
             rotation=(0, 0, 0), radius_top=None):
    if radius_top is None:
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=vertices, radius=radius, depth=depth, location=location,
            rotation=rotation)
    else:
        bpy.ops.mesh.primitive_cone_add(
            vertices=vertices, radius1=radius, radius2=radius_top,
            depth=depth, location=location, rotation=rotation)
    obj = register(bpy.context.object, material)
    if material.startswith("wood"):
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bevel(obj)
    return obj


def ring(location, radius, tube, material, rotation=(math.pi / 2, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=12, minor_segments=6, location=location,
        major_radius=radius, minor_radius=tube, rotation=rotation)
    return register(bpy.context.object, material)


def blob(location, size, material):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1,
                                        location=location)
    obj = register(bpy.context.object, material)
    obj.scale = size
    return obj


def drip(x, y, top, length):
    # 굵은 끝방울과 살짝 휘어진 금빛 줄기를 겹쳐 연결합니다.
    profiles = [(0, .095), (.15, .065), (.48, .037), (.79, .045), (1, .065)]
    vertices = []
    for t, radius in profiles:
        for j in range(8):
            a = math.tau * j / 8
            vertices.append((x + .045 * math.sin(t * 2.2) + radius * math.cos(a),
                             y + radius * .58 * math.sin(a), top - t * length))
    faces = [tuple(reversed(range(8)))]
    for i in range(len(profiles) - 1):
        for j in range(8):
            a = i * 8 + j
            b = i * 8 + (j + 1) % 8
            faces.append((a, b, b + 8, a + 8))
    faces.append(tuple(range(32, 40)))
    mesh_part(vertices, faces, "gold_drip")
    blob((x + .045 * math.sin(2.2), y, top - length),
         (.085, .06, .105), "gold_drip")


def plank_wall():
    for i in range(12):
        height = 2.6 - RNG.uniform(0, .075)
        box((-1.827 + i * .332, 0, height / 2), (.319, .17, height),
            "wood" if i % 2 == 0 else "wood_light", worn=True)
    for z in (.52, 2.15):
        box((0, .105, z), (4, .13, .19), "wood_dark", worn=True)
    for x, z in ((-1.6, 2.15), (0, .52), (1.6, 2.15)):
        box((x, .185, z), (.19, .035, .16), "iron", rounding=.015)
    drip(.78, .185, 2.25, .57)


def boardwalk():
    for i in range(12):
        box((-1.83 + i * .333, 0, .18), (.316, 1.99, .14),
            "wood_light" if i % 3 == 0 else "wood", worn=True)
    for x in (-1.75, -.6, .6, 1.75):
        box((x, .81, .075), (.17, .22, .15), "wood_dark", worn=True)
    for y in (-.78, .65):
        box((0, y, .085), (3.95, .12, .10), "wood_dark")


def sandbags():
    # 각 층 일곱 포대: 어긋난 이층 곡선으로 낮은 엄폐물을 만듭니다.
    for layer in range(2):
        for i in range(7):
            x = (i - 3) * .303 + (.025 if layer else -.025)
            y = .18 * (1 - (x / 1.0) ** 2) + (.055 if layer else -.055)
            obj = box((x, y, .22 + layer * .405),
                      (.375 + RNG.uniform(-.012, .012), .56, .44),
                      "canvas" if (i + layer) % 2 else "sand",
                      rotation=(0, RNG.uniform(-.035, .035), -.22 * x),
                      rounding=.115)
            obj.scale.y = RNG.uniform(.96, 1.04)


def barrel(center, lying=False, twist=0):
    # 배가 불룩한 십이각 나무통과 속이 빈 철제 띠를 만듭니다.
    before = len(PARTS)
    n = 12
    profile = [(-.43, .30), (-.33, .335), (0, .37), (.33, .335), (.43, .30)]
    vertices = [(r * math.cos(math.tau * j / n),
                 r * math.sin(math.tau * j / n), z)
                for z, r in profile for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(k * n + j, k * n + (j + 1) % n,
               (k + 1) * n + (j + 1) % n, (k + 1) * n + j)
              for k in range(4) for j in range(n)]
    faces.append(tuple(range(4 * n, 5 * n)))
    body = mesh_part(vertices, faces, "wood")
    body.data.materials.append(MATERIALS["wood_light"])
    for polygon in body.data.polygons:
        if polygon.index not in (0, len(faces) - 1):
            polygon.material_index = int(polygon.index % 3 == 0)
    bevel(body, .018)
    for z in (-.27, .27):
        band_vertices = []
        for height, r in ((z - .045, .354), (z + .045, .354),
                          (z - .045, .332), (z + .045, .332)):
            band_vertices += [(r * math.cos(math.tau * j / n),
                               r * math.sin(math.tau * j / n), height)
                              for j in range(n)]
        band_faces = []
        for j in range(n):
            k = (j + 1) % n
            band_faces += [(j, k, n + k, n + j),
                           (2*n+j, 3*n+j, 3*n+k, 2*n+k),
                           (j, 2*n+j, 2*n+k, k),
                           (n+j, n+k, 3*n+k, 3*n+j)]
        mesh_part(band_vertices, band_faces, "iron")
    # 뚜껑 위의 굵은 판자 이음새만 표현합니다.
    for x in (-.12, .12):
        box((x, 0, .434), (.018, .49, .012), "wood_dark", rounding=.015)
    from mathutils import Euler
    rotation = Euler((math.pi / 2 if lying else 0, 0, twist)).to_matrix()
    for obj in PARTS[before:]:
        obj.location = rotation @ obj.location + Vector(center)
        obj.rotation_euler = (rotation @ obj.rotation_euler.to_matrix()).to_euler()


def barrel_stack():
    barrel((-.39, 0, .43), twist=-.06)
    barrel((.39, .025, .43), twist=.08)
    barrel((0, 0, 1.20), lying=True, twist=.10)


def hitch_post():
    for x in (-.84, .84):
        box((x, 0, .53), (.20, .22, 1.06), "wood_dark", worn=True)
    box((0, 0, .91), (2, .18, .20), "wood_dark", worn=True)
    ring((.18, .14, .82), .105, .025, "iron")


def trough():
    for x in (-.61, .61):
        for y in (-.19, .19):
            box((x, y, .13), (.13, .13, .26), "wood_dark", worn=True)
    box((0, 0, .23), (1.6, .60, .10), "wood_dark")
    for y in (-.257, .257):
        for i in range(5):
            box((-.634 + i * .317, y, .425), (.308, .085, .35),
                "wood" if i % 2 else "wood_light", worn=True)
    for x in (-.755, .755):
        box((x, 0, .425), (.09, .44, .35), "wood", worn=True)
    box((0, 0, .513), (1.40, .40, .014), "water")
    for x in (-.55, .55):
        for y in (-.305, .305):
            box((x, y, .421), (.055, .025, .32), "iron")


def arrow_board(x, y, z, direction):
    outline = [(-.58, -.14), (.28, -.14), (.28, -.24),
               (.64, 0), (.28, .24), (.28, .14), (-.58, .14)]
    def extrude(scale, depth, offset, material):
        vertices = [(x + direction * px * scale, y + side * depth / 2 + offset,
                     z + pz * scale) for side in (-1, 1) for px, pz in outline]
        n = len(outline)
        faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
        faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
        return bevel(mesh_part(vertices, faces, material), .018)
    extrude(1, .12, 0, "wood_dark")
    extrude(.88, .035, .073, "wood_light")


def signpost():
    box((0, 0, 1.20), (.17, .18, 2.40), "wood_dark", worn=True)
    arrow_board(.11, .125, 2.07, 1)
    arrow_board(-.15, .13, 1.53, -1)
    drip(.35, .24, 1.99, .36)


def lamp_post():
    cylinder((0, 0, .09), .24, .18, "iron")
    cylinder((0, 0, .27), .14, .20, "iron")
    cylinder((0, 0, 1.325), .075, 2.11, "iron")
    cylinder((0, 0, 2.36), .15, .12, "brass")
    box((0, 0, 2.415), (.44, .44, .07), "brass", rounding=.018)
    box((0, 0, 2.63), (.31, .31, .36), "canvas")
    for x in (-.19, .19):
        for y in (-.19, .19):
            box((x, y, 2.63), (.045, .045, .40), "brass", rounding=.015)
    box((0, 0, 2.835), (.44, .44, .07), "brass", rounding=.018)
    cylinder((0, 0, 2.925), .32, .15, "iron", vertices=4, radius_top=0,
             rotation=(0, 0, math.pi / 4))


def rock(index):
    sizes = [(1.08, .85, .69), (1.47, 1.04, .87), (1.79, 1.21, 1.07)]
    width, depth, height = sizes[index]
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1)
    obj = register(bpy.context.object, "rock")
    obj.data.materials.append(MATERIALS["rock_dark"])
    for vertex in obj.data.vertices:
        vertex.co *= RNG.uniform(.88, 1.12)
        vertex.co.x += .15 * vertex.co.z * (index - 1)
        vertex.co.z = max(vertex.co.z, -.51)
    # 잘린 바닥은 수평을 유지하고 모든 바위의 크기를 독립적으로 맞춥니다.
    for axis, dimension in enumerate((width, depth, height)):
        values = [v.co[axis] for v in obj.data.vertices]
        lo, hi = min(values), max(values)
        for vertex in obj.data.vertices:
            vertex.co[axis] = (vertex.co[axis] - lo) / (hi - lo) * dimension
            if axis < 2:
                vertex.co[axis] -= dimension / 2
    for polygon in obj.data.polygons:
        polygon.material_index = int(RNG.random() < .24)
    obj.rotation_euler.z = (.12, -.21, .28)[index]


def railing():
    for i in range(5):
        x = -1.89 + i * .945
        box((x, 0, .47), (.21, .22, .94), "wood_dark", worn=True)
        cylinder((x, 0, .97), .145, .06, "brass", vertices=4,
                 radius_top=.105, rotation=(0, 0, math.pi / 4))
    for z in (.34, .75):
        box((0, .035, z), (4, .14, .14), "wood", worn=True)


def saloon_door():
    for x in (-.70, .70):
        box((x, 0, 1.16), (.20, .23, 2.32), "wood_dark", worn=True)
    box((0, 0, 2.29), (1.60, .25, .22), "wood_dark")
    for side in (-1, 1):
        for i in range(4):
            x = side * (.086 + i * .143)
            top = 1.65 + .10 * (i / 3) ** 2
            box((x, .015, (.63 + top) / 2), (.131, .10, top - .63),
                "wood_light", worn=True)
        for z in (.78, 1.45):
            box((side * .30, .085, z), (.56, .065, .10), "wood", worn=True)
        for z in (.85, 1.46):
            box((side * .585, .105, z), (.10, .045, .12), "iron")


def finish(name):
    # 합치기 전에 각 부품의 베벨을 적용하여 나무별 모서리를 보존합니다.
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
    offset = Vector(((min(v.x for v in coords) + max(v.x for v in coords)) / 2,
                     (min(v.y for v in coords) + max(v.y for v in coords)) / 2,
                     min(v.z for v in coords)))
    for vertex in obj.data.vertices:
        vertex.co -= offset
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    # 바위의 평평한 바닥에서 생긴 면적 0인 삼각형을 정리합니다.
    bmesh.ops.dissolve_degenerate(bm, dist=0.000001, edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    assert triangles < 3000, f"{name}: {triangles} triangles exceeds budget"
    assert obj.type == "MESH" and obj.location.length < 1e-6
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(p.area > 1e-12 for p in obj.data.polygons), name + ": empty face"
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
        ("Kit_PlankWall", plank_wall),
        ("Kit_Boardwalk", boardwalk),
        ("Kit_Sandbags", sandbags),
        ("Kit_BarrelStack", barrel_stack),
        ("Kit_HitchPost", hitch_post),
        ("Kit_Trough", trough),
        ("Kit_Signpost", signpost),
        ("Kit_LampPost", lamp_post),
        ("Kit_Rock_0", lambda: rock(0)),
        ("Kit_Rock_1", lambda: rock(1)),
        ("Kit_Rock_2", lambda: rock(2)),
        ("Kit_Railing", railing),
        ("Kit_SaloonDoor", saloon_door),
    ]
    for index, (name, builder) in enumerate(builders):
        RNG.seed(SEED + index)
        builder()
        finish(name)
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
