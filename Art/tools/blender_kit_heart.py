# 양면 십자와 녹아내리는 금 테두리가 있는 통통한 체력 하트를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_heart.py

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1/Kit_Heart.fbx"
PARTS = []
MATERIALS = {}
COLORS = {
    "heart_red": (0.92, 0.18, 0.22),
    "heart_light": (1.0, 0.62, 0.62),
    "gold": (1.0, 0.78, 0.22),
    "cream": (1.0, 0.95, 0.85),
}
HALF_DEPTH = 0.14


def setup():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    for name, color in COLORS.items():
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        material.diffuse_color = (*color, 1.0)
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*color, 1.0)
        shader.inputs["Roughness"].default_value = {
            "heart_red": 0.38, "heart_light": 0.2, "gold": 0.3, "cream": 0.6,
        }[name]
        if name == "heart_red":
            shader.inputs["Emission Color"].default_value = (*color, 1.0)
            shader.inputs["Emission Strength"].default_value = 0.4
        MATERIALS[name] = material


def register(obj, name, material, smooth=False):
    obj.name = name
    obj.data.materials.append(MATERIALS[material])
    for face in obj.data.polygons:
        face.use_smooth = smooth
    PARTS.append(obj)
    return obj


def mesh_part(name, vertices, faces, material, smooth=False):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return register(obj, name, material, smooth)


def bevel(obj):
    modifier = obj.modifiers.new("Soft handmade edges", "BEVEL")
    modifier.width = 0.004
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(30)
    modifier.use_clamp_overlap = True


def sphere(name, center, scale, material, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, radius=1.0, location=center
    )
    obj = bpy.context.object
    for vertex in obj.data.vertices:
        vertex.co.x *= scale[0]
        vertex.co.y *= scale[1]
        vertex.co.z *= scale[2]
    return register(obj, name, material, smooth=True)


def heart_outline():
    # Eight tangent-matched cubic arcs give a rounded cleft and a soft pointed tip.
    arcs = [
        ((0, 0.155), (0.045, 0.155), (0.07, 0.275), (0.15, 0.275)),
        ((0.15, 0.275), (0.245, 0.275), (0.3, 0.21), (0.3, 0.1)),
        ((0.3, 0.1), (0.3, -0.005), (0.205, -0.105), (0.14, -0.16)),
        ((0.14, -0.16), (0.075, -0.215), (0.023, -0.275), (0, -0.275)),
        ((0, -0.275), (-0.023, -0.275), (-0.075, -0.215), (-0.14, -0.16)),
        ((-0.14, -0.16), (-0.205, -0.105), (-0.3, -0.005), (-0.3, 0.1)),
        ((-0.3, 0.1), (-0.3, 0.21), (-0.245, 0.275), (-0.15, 0.275)),
        ((-0.15, 0.275), (-0.07, 0.275), (-0.045, 0.155), (0, 0.155)),
    ]
    outline = []
    for arc in arcs:
        points = [Vector(point) for point in arc]
        for index in range(6):
            t = index / 6
            u = 1 - t
            outline.append(u ** 3 * points[0] + 3 * u * u * t * points[1]
                           + 3 * u * t * t * points[2] + t ** 3 * points[3])
    return outline


def build_body(outline):
    # Elliptical sections close at both face centers, with a shared equatorial ring.
    vertices = []
    faces = []
    count = len(outline)
    for latitude in range(-4, 5):
        angle = latitude * math.pi / 10
        radius = math.cos(angle)
        y = HALF_DEPTH * math.sin(angle)
        vertices.extend((point.x * radius, y, point.y * radius) for point in outline)
    for ring in range(8):
        for index in range(count):
            a = ring * count + index
            b = ring * count + (index + 1) % count
            faces.append((a, b, b + count, a + count))
    for ring, side in ((0, -1), (8, 1)):
        pole = len(vertices)
        vertices.append((0, side * HALF_DEPTH, 0))
        for index in range(count):
            faces.append((pole, ring * count + index, ring * count + (index + 1) % count))
    return mesh_part("Inflated red heart", vertices, faces, "heart_red", smooth=True)


def face_depth(body, x, z):
    # Raycast the actual low-poly surface so each highlight hugs the cushion.
    hit, location, normal, index = body.ray_cast(
        Vector((x, -0.5, z)), Vector((0, 1, 0))
    )
    assert hit, "Face detail lies outside the heart"
    return abs(location.y)


def build_highlights(body):
    for side in (-1, 1):
        # Mirror the lobe so the highlight reads upper-left when viewing either face.
        obj = sphere("Glossy lobe highlight %d" % side, (0, 0, 0),
                     (1, 1, 1), "heart_light", segments=10)
        for vertex in obj.data.vertices:
            unit = vertex.co.copy()
            tilt = math.radians(-side * 22)
            dx = 0.027 * unit.x
            dz = 0.052 * unit.z
            x = side * 0.155 + dx * math.cos(tilt) + dz * math.sin(tilt)
            z = 0.16 - dx * math.sin(tilt) + dz * math.cos(tilt)
            y = side * (face_depth(body, x, z) + 0.002 + 0.008 * unit.y)
            vertex.co = (x, y, z)
        obj.data.update()


def build_crosses():
    # Union outline of two 0.100 x 0.030 crossing bars avoids overlapping top faces.
    long = 0.05
    short = 0.015
    outline = [
        (-short, long), (short, long), (short, short), (long, short),
        (long, -short), (short, -short), (short, -long), (-short, -long),
        (-short, -short), (-long, -short), (-long, short), (-short, short),
    ]
    for side in (-1, 1):
        vertices = [(x, side * y, z) for y in (0.133, 0.155) for x, z in outline]
        count = len(outline)
        faces = [tuple(reversed(range(count))), tuple(range(count, 2 * count))]
        for index in range(count):
            following = (index + 1) % count
            faces.append((index, following, following + count, index + count))
        obj = mesh_part("Embossed cream cross %d" % side, vertices, faces, "cream")
        bevel(obj)


def tube(name, points, radii, material, sides=6, closed=False, depth_scale=1.0):
    points = [Vector(point) for point in points]
    vertices = []
    faces = []
    for index, point in enumerate(points):
        if closed:
            tangent = points[(index + 1) % len(points)] - points[(index - 1) % len(points)]
        else:
            tangent = points[min(index + 1, len(points) - 1)] - points[max(index - 1, 0)]
        tangent.normalize()
        normal = tangent.cross(Vector((0, 1, 0))).normalized()
        binormal = tangent.cross(normal).normalized()
        for side in range(sides):
            angle = math.tau * side / sides
            offset = normal * math.cos(angle) + binormal * depth_scale * math.sin(angle)
            vertices.append(point + radii[index] * offset)
    for ring in range(len(points) if closed else len(points) - 1):
        following = (ring + 1) % len(points)
        for side in range(sides):
            a = ring * sides + side
            b = ring * sides + (side + 1) % sides
            c = following * sides + (side + 1) % sides
            d = following * sides + side
            faces.append((a, b, c, d))
    if not closed:
        faces.append(tuple(reversed(range(sides))))
        faces.append(tuple((len(points) - 1) * sides + side for side in range(sides)))
    return mesh_part(name, vertices, faces, material, smooth=True)


def build_gold(outline):
    points = [(point.x * 1.006, 0, point.y * 1.006) for point in outline]
    tube("Thin brass silhouette setting", points, [0.007] * len(points), "gold",
         closed=True, depth_scale=1.6)
    # A single pendant of melted gold hangs from the outer right lobe.
    tube("Elongated melting gold teardrop",
         [(0.299, 0, 0.125), (0.307, 0, 0.1), (0.31, 0, 0.065),
          (0.313, 0, 0.025), (0.313, 0, -0.012)],
         [0.01, 0.011, 0.009, 0.008, 0.013], "gold", sides=8)
    sphere("Rounded gold drip end", (0.313, 0, -0.024),
           (0.018, 0.017, 0.027), "gold", segments=10, rings=6)


def finish_and_export():
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
    obj.name = "Kit_Heart"
    obj.data.name = "Kit_Heart"

    # The red heart's center remains the spin pivot even with the asymmetric drip.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.context.view_layer.update()
    scale = 0.6 / obj.dimensions.x
    obj.scale = (scale, scale, scale)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(len(edge.link_faces) == 2 for edge in bm.edges), "Unclosed mesh component"
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    bpy.context.view_layer.update()
    dimensions = tuple(round(value, 4) for value in obj.dimensions)
    assert triangles < 2500, "Triangle budget exceeded: %d" % triangles
    assert abs(obj.dimensions.x - 0.6) < 0.0001
    assert obj.location.length < 0.000001
    assert set(material.name for material in obj.data.materials) == set(COLORS)
    assert len(bpy.context.scene.objects) == 1 and obj.type == "MESH"

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False
    )
    assert OUTPUT.is_file() and OUTPUT.stat().st_size > 0
    print("EXPORTED Kit_Heart %d %s" % (triangles, dimensions))
    print("DONE")


def main():
    setup()
    outline = heart_outline()
    body = build_body(outline)
    # Face raycasts need the newly linked body's evaluated transform.
    bpy.context.view_layer.update()
    build_highlights(body)
    build_crosses()
    build_gold(outline)
    finish_and_export()


if __name__ == "__main__":
    main()
