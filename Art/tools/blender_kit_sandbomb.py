# 모래주머니 폭탄과 모래시계 뚜껑을 만들고 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_sandbomb.py

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1/Kit_SandBomb.fbx"
RNG = random.Random(48317)
PARTS = []
MATERIALS = {}
COLORS = {
    "canvas": (0.85, 0.76, 0.6),
    "sand": (0.9, 0.72, 0.45),
    "wood_dark": (0.33, 0.19, 0.12),
    "brass": (0.85, 0.62, 0.25),
    "iron": (0.24, 0.22, 0.25),
    "gold_drip": (1.0, 0.78, 0.22),
    "spark": (1.0, 0.55, 0.15),
}
BULGE_PHASE = RNG.uniform(-math.pi, math.pi)


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
        shader.inputs["Roughness"].default_value = 0.78
        if name == "spark":
            shader.inputs["Emission Color"].default_value = (*color, 1.0)
            shader.inputs["Emission Strength"].default_value = 3.0
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


def bevel(obj, angle_limit=math.radians(30)):
    modifier = obj.modifiers.new("Soft handmade edges", "BEVEL")
    modifier.width = 0.01
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = angle_limit
    # Clamp overlap keeps thin brass discs and tapered panels intact.
    modifier.use_clamp_overlap = True


def sphere(name, center, scale, material, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, radius=1.0, location=center
    )
    obj = bpy.context.object
    for vertex in obj.data.vertices:
        vertex.co.x *= scale[0]
        vertex.co.y *= scale[1]
        vertex.co.z *= scale[2]
    return register(obj, name, material, smooth=True)


def cone(name, center, bottom_radius, top_radius, height, material,
         segments=12, hard=False, direction=None):
    bpy.ops.mesh.primitive_cone_add(
        vertices=segments, radius1=bottom_radius, radius2=top_radius,
        depth=height, location=center
    )
    obj = register(bpy.context.object, name, material)
    if direction is not None:
        obj.rotation_euler = Vector(direction).to_track_quat("Z", "Y").to_euler()
    if hard:
        bevel(obj)
    return obj


def tube(name, points, radii, material, sides=6, smooth=True, closed=False):
    points = [Vector(point) for point in points]
    vertices = []
    for index, point in enumerate(points):
        if closed:
            tangent = points[(index + 1) % len(points)] - points[(index - 1) % len(points)]
        else:
            tangent = points[min(index + 1, len(points) - 1)] - points[max(index - 1, 0)]
        tangent.normalize()
        reference = Vector((0.0, 1.0, 0.0))
        if abs(tangent.dot(reference)) > 0.95:
            reference = Vector((1.0, 0.0, 0.0))
        normal = tangent.cross(reference).normalized()
        binormal = tangent.cross(normal).normalized()
        for side in range(sides):
            angle = math.tau * side / sides
            offset = normal * math.cos(angle) + binormal * math.sin(angle)
            vertices.append(point + radii[index] * offset)
    faces = []
    for ring in range(len(points) if closed else len(points) - 1):
        next_ring = (ring + 1) % len(points)
        for side in range(sides):
            a = ring * sides + side
            b = ring * sides + (side + 1) % sides
            c = next_ring * sides + (side + 1) % sides
            d = next_ring * sides + side
            faces.append((a, b, c, d))
    if not closed:
        faces.append(tuple(reversed(range(sides))))
        faces.append(tuple((len(points) - 1) * sides + side for side in range(sides)))
    return mesh_part(name, vertices, faces, material, smooth)


def sack_radius(angle, z):
    latitude = max(0.0, 1.0 - (z / 0.163) ** 2) ** 0.5
    lobes = 1.0 + 0.065 * math.cos(5.0 * angle + BULGE_PHASE)
    lobes += 0.025 * math.sin(3.0 * angle + z * 9.0)
    return 0.2 * latitude * lobes


def build_sack():
    sphere("Round iron bomb core", (0, 0, 0), (0.18, 0.176, 0.143), "iron", 12, 6)
    sack = sphere("Five soft canvas bulges", (0, 0, 0), (1, 1, 1), "canvas", 20, 10)
    for vertex in sack.data.vertices:
        unit = vertex.co.copy()
        angle = math.atan2(unit.y, unit.x)
        z = unit.z * 0.163
        radius = sack_radius(angle, z)
        vertex.co = (radius * math.cos(angle), radius * math.sin(angle), z)

    # A wide shoulder tapers into a folded, flared gathered mouth.
    vertices = []
    neck_rings = [(0.123, 0.095), (0.15, 0.061), (0.17, 0.044), (0.19, 0.055)]
    sides = 16
    for z, radius in neck_rings:
        for side in range(sides):
            angle = math.tau * side / sides
            fold = 1.0 + 0.12 * math.cos(8 * angle + 0.3)
            vertices.append((radius * fold * math.cos(angle),
                             radius * fold * math.sin(angle), z))
    faces = []
    for ring in range(len(neck_rings) - 1):
        for side in range(sides):
            a = ring * sides + side
            b = ring * sides + (side + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.extend([tuple(reversed(range(sides))), tuple(range(48, 64))])
    mesh_part("Gathered canvas neck", vertices, faces, "canvas", smooth=True)

    bpy.ops.mesh.primitive_torus_add(
        major_segments=16, minor_segments=6, location=(0, 0, 0.164),
        major_radius=0.052, minor_radius=0.012
    )
    register(bpy.context.object, "Neck rope tie", "wood_dark", smooth=True)

    # A single tilted meridian crosses the belly and meets the neck tie.
    points = []
    rope_angle = math.radians(28)
    for index in range(28):
        angle = math.tau * index / 28
        horizontal = 0.205 * math.sin(angle)
        z = 0.166 * math.cos(angle)
        lobe_angle = rope_angle if horizontal >= 0 else rope_angle + math.pi
        lobe_scale = 1.0 + 0.065 * math.cos(5 * lobe_angle + BULGE_PHASE)
        points.append((horizontal * lobe_scale * math.cos(rope_angle),
                       horizontal * lobe_scale * math.sin(rope_angle), z))
    tube("Crossing belly rope", points, [0.009] * len(points), "wood_dark", closed=True)


def hourglass_bulb(name, z_bottom, z_top, radius_bottom, radius_top):
    # Three closed brass cone panels leave broad windows to the inner sand.
    vertices = []
    faces = []
    for panel in range(3):
        center_angle = math.tau * panel / 3 + 0.2
        angles = (center_angle - 0.23, center_angle + 0.23)
        start = len(vertices)
        for inset in (0.0, 0.004):
            for z, radius in ((z_bottom, radius_bottom), (z_top, radius_top)):
                for angle in angles:
                    vertices.append(((radius - inset) * math.cos(angle),
                                     (radius - inset) * math.sin(angle), z))
        for face in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
                     (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
            faces.append(tuple(start + index for index in face))
    obj = mesh_part(name, vertices, faces, "brass")
    bevel(obj)


def build_hourglass():
    cone("Bottom brass disc", (0, 0, 0.192), 0.057, 0.057, 0.012, "brass", hard=True)
    hourglass_bulb("Lower brass cone", 0.198, 0.248, 0.049, 0.009)
    hourglass_bulb("Upper brass cone", 0.248, 0.298, 0.009, 0.049)
    cone("Top brass disc", (0, 0, 0.304), 0.057, 0.057, 0.012, "brass", hard=True)
    cone("Lower sand pile", (0, 0, 0.216), 0.039, 0.004, 0.036, "sand")
    cone("Upper sand reservoir", (0, 0, 0.278), 0.004, 0.033, 0.036, "sand")
    cone("Sand through waist", (0, 0, 0.248), 0.003, 0.003, 0.03, "sand", segments=6)


def build_fuse():
    points = []
    for index in range(13):
        t = 4.6 * index / 12
        points.append((0.021 * (1 - math.cos(t)), 0.021 * math.sin(t),
                       0.307 + 0.06 * index / 12))
    fuse = tube("Curly iron fuse", points, [0.008] * len(points), "iron")
    bevel(fuse, angle_limit=math.radians(75))
    center = Vector(points[-1])
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.014, location=center)
    register(bpy.context.object, "Spark heart", "spark")
    # Six spatial rays keep the spark recognizable throughout the airborne spin.
    for index, direction in enumerate(((1, 0, 0), (-1, 0, 0), (0, 1, 0),
                                       (0, -1, 0), (0, 0, 1), (0, 0, -1))):
        direction = Vector(direction)
        length = RNG.uniform(0.025, 0.033)
        cone("Spark ray %d" % index, center + direction * (0.009 + length / 2),
             0.009, 0, length, "spark", segments=5, direction=direction)


def build_leaks():
    angle = math.radians(-62)
    points = [(0.052 * math.cos(angle), 0.052 * math.sin(angle), 0.164)]
    heights = (0.148, 0.124, 0.082, 0.028, -0.027, -0.07)
    for z in heights:
        radius = sack_radius(angle, z) + 0.004
        points.append((radius * math.cos(angle), radius * math.sin(angle), z))
    tube("Single flowing gold teardrop", points,
         [0.009, 0.011, 0.012, 0.013, 0.012, 0.011, 0.015], "gold_drip", sides=7)
    end = Vector(points[-1])
    sphere("Rounded gold drop", end + Vector((0, 0, -0.009)),
           (0.02, 0.02, 0.028), "gold_drip", 10, 6)

    sphere("Dark bottom tear", (0.025, -0.025, -0.156),
           (0.029, 0.019, 0.009), "wood_dark", 10, 6)
    for index, (x, y, length) in enumerate(((0.012, -0.026, 0.047), (0.037, -0.02, 0.034))):
        tube("Leaking sand trickle %d" % index,
             [(x, y, -0.154), (x + 0.003, y - 0.002, -0.171),
              (x + 0.006, y - 0.004, -0.154 - length)],
             [0.009, 0.007, 0.003], "sand", sides=6)


def finish_and_export():
    # Apply the hard-part bevels before joining so every component retains its edges.
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
    obj.name = "Kit_SandBomb"
    obj.data.name = "Kit_SandBomb"

    # Preserve the body's center as the pivot while setting a compact overall height.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.context.view_layer.update()
    scale = 0.54 / obj.dimensions.z
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
    assert triangles < 3000, "Triangle budget exceeded: %d" % triangles
    assert abs(obj.dimensions.z - 0.54) < 0.0001
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
    print("EXPORTED Kit_SandBomb %d %s" % (triangles, dimensions))
    print("DONE")


def main():
    setup()
    build_sack()
    build_hourglass()
    build_fuse()
    build_leaks()
    finish_and_export()


if __name__ == "__main__":
    main()
