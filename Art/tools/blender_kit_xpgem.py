# 청록색 결정과 황동 시곗바늘로 경험치 보석을 만들고 FBX로 내보냅니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_xpgem.py

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1/Kit_XpGem.fbx"
PARTS = []
MATERIALS = {}
COLORS = {
    "gem_teal": (0.25, 0.85, 0.95),
    "gem_light": (0.7, 1.0, 1.0),
    "gem_core": (0.9, 1.0, 1.0),
    "gold": (1.0, 0.78, 0.22),
}
HEIGHT = 0.35
RADIUS = 0.092
BELT_HEIGHT = 0.040
# Center the tip-to-tip bounds at the spin origin, retaining 60/40 pyramids.
BELT_CENTER = -0.1 * (HEIGHT - BELT_HEIGHT)
BELT_BOTTOM = BELT_CENTER - BELT_HEIGHT / 2
BELT_TOP = BELT_CENTER + BELT_HEIGHT / 2
APOTHEM = RADIUS * math.cos(math.pi / 6)


def setup():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    PARTS.clear()
    MATERIALS.clear()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    for name, color in COLORS.items():
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        material.diffuse_color = (*color, 1.0)
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*color, 1.0)
        shader.inputs["Roughness"].default_value = 0.3 if name == "gold" else 0.38
        if name in {"gem_teal", "gem_light"}:
            shader.inputs["Emission Color"].default_value = (*color, 1.0)
            shader.inputs["Emission Strength"].default_value = 0.5
        # Opaque facets: the inner core is deliberately enclosed, not transparent.
        shader.inputs["Alpha"].default_value = 1.0
        shader.inputs["Transmission Weight"].default_value = 0.0
        MATERIALS[name] = material


def mesh_part(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material])
    for face in mesh.polygons:
        face.use_smooth = False
    PARTS.append(obj)
    return obj


def hex_ring(radius, z):
    # Opposite broad faces lie in the XZ plane, facing +/-Y.
    return [(radius * math.cos(math.tau * index / 6),
             radius * math.sin(math.tau * index / 6), z) for index in range(6)]


def build_crystal(name, scale, material, bright_belt=False):
    vertices = hex_ring(RADIUS, BELT_BOTTOM) + hex_ring(RADIUS, BELT_TOP)
    vertices += [(0, 0, -HEIGHT / 2), (0, 0, HEIGHT / 2)]
    vertices = [tuple(scale * value for value in vertex) for vertex in vertices]
    faces = []
    for index in range(6):
        following = (index + 1) % 6
        faces.append((12, following, index))
        faces.append((index, following, following + 6, index + 6))
        faces.append((13, index + 6, following + 6))
    obj = mesh_part(name, vertices, faces, material)
    if bright_belt:
        # A separate ring of six opaque faces provides the visible glow stripe.
        obj.data.materials.append(MATERIALS["gem_light"])
        for index in range(6):
            obj.data.polygons[3 * index + 1].material_index = 1
    return obj


def bevel(obj):
    # Same applied bevel convention as the heart, scaled for the small brass mark.
    modifier = obj.modifiers.new("Soft handmade edges", "BEVEL")
    modifier.width = 0.0004
    modifier.segments = 1
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(30)
    modifier.use_clamp_overlap = True


def build_caps():
    # Closed, genuinely hollow hexagonal rings; no disks cover the bright belt.
    for name, z in (("Lower brass setting", BELT_BOTTOM),
                    ("Upper brass setting", BELT_TOP)):
        vertices = []
        for height, radius in ((z - 0.0015, RADIUS - 0.0015),
                               (z - 0.0015, RADIUS + 0.002),
                               (z + 0.0015, RADIUS - 0.0015),
                               (z + 0.0015, RADIUS + 0.002)):
            vertices.extend(hex_ring(radius, height))
        faces = []
        for index in range(6):
            following = (index + 1) % 6
            faces.extend([
                (index, following, following + 6, index + 6),
                (index + 12, index + 18, following + 18, following + 12),
                (index, index + 12, following + 12, following),
                (index + 6, following + 6, following + 18, index + 18),
            ])
        mesh_part(name, vertices, faces, "gold")


def clock_bar(name, side, start, end, width):
    direction = (Vector(end) - Vector(start)).normalized()
    across = Vector((-direction.y, direction.x)) * width / 2
    outline = [Vector(start) - across, Vector(end) - across,
               Vector(end) + across, Vector(start) + across]
    # Embed the underside slightly; the visible relief is exactly 4 mm deep.
    vertices = [(side * point.x, side * (APOTHEM + depth), point.y)
                for depth in (-0.0005, 0.004) for point in outline]
    faces = [(3, 2, 1, 0), (4, 5, 6, 7)]
    for index in range(4):
        following = (index + 1) % 4
        faces.append((index, following, following + 4, index + 4))
    obj = mesh_part(name, vertices, faces, "gold")
    bevel(obj)


def clock_dot(side, pivot):
    vertices = []
    sides = 12
    for depth in (-0.0005, 0.004):
        for index in range(sides):
            angle = math.tau * index / sides
            vertices.append((side * (pivot[0] + 0.0035 * math.cos(angle)),
                             side * (APOTHEM + depth),
                             pivot[1] + 0.0035 * math.sin(angle)))
    faces = [tuple(reversed(range(sides))), tuple(range(sides, 2 * sides))]
    for index in range(sides):
        following = (index + 1) % sides
        faces.append((index, following, following + sides, index + sides))
    mesh_part("Clock pivot %d" % side, vertices, faces, "gold")


def build_clock_marks():
    pivot = (0, BELT_CENTER - 0.006)
    for side in (-1, 1):
        for name, length, angle, width in (
                ("Minute hand", 0.018, 15, 0.003),
                ("Hour hand", 0.012, 65, 0.0035)):
            angle = math.radians(angle)
            end = (pivot[0] + length * math.sin(angle),
                   pivot[1] + length * math.cos(angle))
            clock_bar("%s %d" % (name, side), side, pivot, end, width)
        clock_dot(side, pivot)


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
    obj.name = "Kit_XpGem"
    obj.data.name = "Kit_XpGem"
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(len(edge.link_faces) == 2 for edge in bm.edges), "Unclosed mesh component"
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    bpy.context.view_layer.update()
    dimensions = tuple(round(value, 4) for value in obj.dimensions)
    assert triangles < 800, "Triangle budget exceeded: %d" % triangles
    assert abs(obj.dimensions.z - HEIGHT) < 0.000001
    assert obj.dimensions.z > max(obj.dimensions.x, obj.dimensions.y)
    assert obj.location.length < 0.000001
    bounds = [Vector(corner) for corner in obj.bound_box]
    assert (bounds[0] + bounds[6]).length < 0.000001, "Off-center spin origin"
    assert set(material.name for material in obj.data.materials) == set(COLORS)
    assert len(bpy.context.scene.objects) == 1 and obj.type == "MESH"
    assert not obj.modifiers

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False
    )
    assert OUTPUT.is_file() and OUTPUT.stat().st_size > 0
    print("EXPORTED Kit_XpGem %d %s" % (triangles, dimensions))
    print("DONE")


def main():
    setup()
    build_crystal("Faceted teal crystal", 1.0, "gem_teal", bright_belt=True)
    build_crystal("Enclosed glow core", 0.55, "gem_core")
    build_caps()
    build_clock_marks()
    finish_and_export()


if __name__ == "__main__":
    main()
