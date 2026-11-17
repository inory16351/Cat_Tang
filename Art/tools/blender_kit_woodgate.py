# 전투 종료 때 조각으로 터지는 서부풍 나무 바리케이드 문을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_woodgate.py

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch1/Kit_WoodGate.fbx"
RNG = random.Random(7731)
PARTS = []
SPLINTERS = []
MATERIALS = {}
COLORS = {
    "wood": (0.55, 0.34, 0.19),
    "wood_light": (0.68, 0.45, 0.26),
    "wood_dark": (0.33, 0.19, 0.12),
    "iron": (0.24, 0.22, 0.25),
    "gold_drip": (1.0, 0.78, 0.22),
}
BEVEL_WIDTH = 0.012


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
        MATERIALS[name] = material


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def recalc(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), "Unclosed mesh component"
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def restore_mating_edges(obj, paths, y_min, y_max):
    # Beveling both sides of a fracture would open a visible V-shaped gap.
    # Restore only the shared interfaces, keeping the exposed perimeter rounded.
    corners = [Vector(point) for path in paths for point in path]
    segments = [(Vector(a), Vector(b)) for path in paths
                for a, b in zip(path, path[1:])]
    reach = BEVEL_WIDTH * 2.1
    for vertex in obj.data.vertices:
        point = Vector((vertex.co.x, vertex.co.z))
        nearest_corner = min(corners, key=lambda corner: (corner - point).length_squared)
        if (nearest_corner - point).length <= reach:
            target = nearest_corner
        else:
            candidates = []
            for start, end in segments:
                direction = end - start
                t = max(0.0, min(1.0, (point - start).dot(direction)
                                 / direction.length_squared))
                candidates.append(start + t * direction)
            target = min(candidates, key=lambda candidate: (candidate - point).length_squared)
        if (target - point).length > reach:
            continue
        vertex.co.x, vertex.co.z = target
        if abs(vertex.co.y - y_min) <= BEVEL_WIDTH * 1.1:
            vertex.co.y = y_min
        elif abs(vertex.co.y - y_max) <= BEVEL_WIDTH * 1.1:
            vertex.co.y = y_max

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=0.000001)
    bmesh.ops.dissolve_limit(
        bm, angle_limit=0.00001, verts=list(bm.verts), edges=list(bm.edges),
        use_dissolve_boundaries=False,
    )
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def extrude_piece(outline, y_min, y_max, material, mating_paths=(), splinter=False):
    # A single concave XZ polygon is extruded with bmesh, without booleans.
    bm = bmesh.new()
    vertices = [bm.verts.new((x, y_min, z)) for x, z in outline]
    face = bm.faces.new(vertices)
    extrusion = bmesh.ops.extrude_face_region(bm, geom=[face])
    new_vertices = [element for element in extrusion["geom"]
                    if isinstance(element, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=new_vertices, vec=(0, y_max - y_min, 0))
    # The original face is retained by extrude_face_region as the back cap.
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    name = "Splinter_%d" % len(SPLINTERS) if splinter else "Piece_%02d" % len(PARTS)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material])
    (SPLINTERS if splinter else PARTS).append(obj)

    activate(obj)
    modifier = obj.modifiers.new("Soft handmade edges", "BEVEL")
    modifier.width = BEVEL_WIDTH
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(30)
    modifier.use_clamp_overlap = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    if mating_paths:
        restore_mating_edges(obj, mating_paths, y_min, y_max)
    recalc(obj.data)
    return obj


def attach(owner, decoration, material):
    decoration.data.materials.append(MATERIALS[material])
    activate(owner)
    decoration.select_set(True)
    bpy.ops.object.join()


def knot(owner, x, z):
    radius = RNG.uniform(0.04, 0.06)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8, radius=radius, depth=0.014,
        location=(x, 0.073, z), rotation=(math.pi / 2, 0, 0),
    )
    attach(owner, bpy.context.object, "wood_dark")


def rivet(owner, x, y, z):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(x, y, z))
    obj = bpy.context.object
    for vertex in obj.data.vertices:
        vertex.co.x *= 0.035
        vertex.co.y *= 0.018
        vertex.co.z *= 0.035
    attach(owner, obj, "iron")


def fracture(start, end, teeth, depth):
    # Alternating offsets form pointed splinters. Both neighbors reuse this list.
    start, end = Vector(start), Vector(end)
    tangent = (end - start).normalized()
    normal = Vector((-tangent.y, tangent.x))
    points = [tuple(start)]
    for index in range(1, teeth * 2):
        t = index / (teeth * 2)
        offset = depth * (0.5 if index % 2 else -0.5)
        points.append(tuple(start.lerp(end, t) + normal * offset))
    points.append(tuple(end))
    return points


def line_height(path, x):
    for (x0, z0), (x1, z1) in zip(path, path[1:]):
        if x0 <= x <= x1:
            return z0 + (z1 - z0) * (x - x0) / (x1 - x0)
    raise ValueError("Point outside plank")


def build_planks():
    # Sum to exactly 4.6 while retaining every width inside [0.52, 0.62].
    widths = [RNG.uniform(0.55, 0.60) for _ in range(7)]
    widths.append(4.6 - sum(widths))
    while not 0.52 <= widths[-1] <= 0.62:
        widths = [RNG.uniform(0.55, 0.60) for _ in range(7)]
        widths.append(4.6 - sum(widths))
    assert abs(sum(widths) - 4.6) < 1e-10
    x_left = -2.3
    previous_material = RNG.choice(("wood", "wood_light"))
    for index, width in enumerate(widths):
        x_right = x_left + width
        height = 2.6 if index == 3 else RNG.uniform(2.45, 2.585)
        style = index % 3
        if style == 0:
            top = [(x_left, height), (x_right, height)]
        elif style == 1:
            slope = RNG.uniform(0.035, 0.085)
            top = [(x_left, height - slope), (x_right, height)]
        else:
            top = [(x_left, height - 0.055),
                   (x_left + width * RNG.uniform(0.4, 0.6), height),
                   (x_right, height - 0.075)]
        if index in (2, 5):
            heights = [RNG.uniform(0.65, 0.95), RNG.uniform(1.55, 1.95)]
        else:
            heights = [RNG.uniform(0.85, 1.8)]
        breaks = [fracture((x_left, z), (x_right, z + RNG.uniform(-0.05, 0.05)),
                           3, RNG.uniform(0.06, 0.14)) for z in heights]
        boundaries = [[(x_left, 0), (x_right, 0)], *breaks, top]
        material = ("wood_light" if previous_material == "wood" else "wood")
        if RNG.random() < 0.2:
            material = previous_material
        previous_material = material
        for chunk_index, (lower, upper) in enumerate(zip(boundaries, boundaries[1:])):
            mating_paths = []
            if chunk_index:
                mating_paths.append(lower)
            if chunk_index < len(boundaries) - 2:
                mating_paths.append(upper)
            # Plank side joints are flush too, including their front/back edges.
            if index:
                mating_paths.append([lower[0], upper[0]])
            if index < 7:
                mating_paths.append([lower[-1], upper[-1]])
            obj = extrude_piece(lower + list(reversed(upper)), -0.07, 0.07,
                                material, mating_paths)
            for _ in range(RNG.randint(0, 2)):
                x = RNG.uniform(x_left + 0.09, x_right - 0.09)
                z_min = line_height(lower, x) + 0.10
                z_max = line_height(upper, x) - 0.10
                z = RNG.uniform(z_min, z_max)
                # Keep knots readable beside the front reinforcement boards.
                if abs(z - 0.55) < 0.19:
                    z = max(z_min, 0.34)
                elif abs(z - 2.0) < 0.19:
                    z = min(z_max, 1.79)
                knot(obj, x, z)
        x_left = x_right


def build_rails():
    rails = []
    for z in (0.55, 2.0):
        bottom, top = z - 0.11, z + 0.11
        cuts = [[(-2.3, bottom), (-2.3, top)]]
        for x in (-0.78, 0.77):
            cuts.append(fracture((x, bottom), (x, top), 2, RNG.uniform(0.065, 0.09)))
        cuts.append([(2.3, bottom), (2.3, top)])
        row = []
        for index, (left, right) in enumerate(zip(cuts, cuts[1:])):
            mating_paths = []
            if index:
                mating_paths.append(left)
            if index < 2:
                mating_paths.append(right)
            obj = extrude_piece(left + list(reversed(right)), 0.07, 0.15,
                                "wood_dark", mating_paths)
            # Rivets stay safely away from the rail fracture interfaces.
            for x in (-2.12, -1.57, -1.02, -0.47, 0.08, 0.63, 1.18, 1.73, 2.12):
                if left[0][0] + 0.07 < x < right[0][0] - 0.07:
                    rivet(obj, x, 0.151, z)
            row.append(obj)
        rails.append(row)
    return rails


def build_brace():
    # In local board coordinates, the break crosses its width with two teeth.
    start, end = Vector((-2.04, 0.64)), Vector((2.02, 1.91))
    tangent = (end - start).normalized()
    normal = Vector((-tangent.y, tangent.x))
    half_width = 0.135
    middle = start.lerp(end, 0.49)
    cut = fracture(middle - normal * half_width, middle + normal * half_width,
                   2, 0.11)
    left_end = [tuple(start - normal * half_width), tuple(start + normal * half_width)]
    right_end = [tuple(end - normal * half_width), tuple(end + normal * half_width)]
    # The diagonal lies above the planks and overlaps the rail edges slightly.
    # Its raised front face gives the complete gate approximately 0.32 m depth.
    extrude_piece(left_end + list(reversed(cut)), 0.073, 0.20, "wood_dark", [cut])
    extrude_piece(cut + list(reversed(right_end)), 0.073, 0.20, "wood_dark", [cut])


def build_straps():
    for z in (0.8, 1.8):
        cuts = (-2.3, -0.79, 0.79, 2.3)
        for index, (left, right) in enumerate(zip(cuts, cuts[1:])):
            bottom, top = z - 0.07, z + 0.07
            mating_paths = []
            if index:
                mating_paths.append([(left, bottom), (left, top)])
            if index < 2:
                mating_paths.append([(right, bottom), (right, top)])
            obj = extrude_piece([(left, bottom), (right, bottom),
                                 (right, top), (left, top)],
                                -0.10, -0.07, "iron", mating_paths)
            for x in (-2.12, -1.57, -1.02, -0.47, 0.08, 0.63, 1.18, 1.73, 2.12):
                if left + 0.07 < x < right - 0.07:
                    rivet(obj, x, -0.101, z)


def build_gold(upper_rail):
    # One continuous, broad teardrop flows from the upper rail onto a plank.
    # The lower bulb intersects its tail so it reads as a single molten drip.
    x = -0.34
    outline = [(x - 0.046, 2.08), (x + 0.041, 2.075),
               (x + 0.035, 1.98), (x + 0.025, 1.83),
               (x + 0.037, 1.66), (x + 0.012, 1.60),
               (x - 0.027, 1.625), (x - 0.039, 1.68),
               (x - 0.018, 1.83), (x - 0.027, 1.98)]
    bm = bmesh.new()
    verts = [bm.verts.new((px, 0.148 if z >= 1.89 else 0.067, z))
             for px, z in outline]
    face = bm.faces.new(verts)
    result = bmesh.ops.extrude_face_region(bm, geom=[face])
    new_vertices = [item for item in result["geom"] if isinstance(item, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=new_vertices, vec=(0, 0.018, 0))
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    mesh = bpy.data.meshes.new("Molten gold")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Molten gold", mesh)
    bpy.context.collection.objects.link(obj)
    attach(upper_rail, obj, "gold_drip")
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=8, ring_count=4, radius=1, location=(x, 0.084, 1.65),
    )
    obj = bpy.context.object
    for vertex in obj.data.vertices:
        vertex.co.x *= 0.048
        vertex.co.y *= 0.022
        vertex.co.z *= 0.071
    attach(upper_rail, obj, "gold_drip")


def build_splinters():
    for index in range(3):
        length = RNG.uniform(0.25, 0.45)
        width = RNG.uniform(0.04, 0.07)
        center = Vector(((index - 1) * 0.035, 1.3 + (index - 1) * 0.025))
        angle = RNG.uniform(-0.3, 0.3)
        tangent = Vector((math.sin(angle), math.cos(angle)))
        normal = Vector((tangent.y, -tangent.x))
        local_outline = [(0, -length / 2), (width / 2, -length * 0.23),
                         (width * 0.30, length * 0.08), (width / 2, length * 0.29),
                         (0, length / 2), (-width / 2, length * 0.16),
                         (-width * 0.33, -length * 0.17)]
        outline = [tuple(center + normal * across + tangent * along)
                   for across, along in local_outline]
        extrude_piece(outline, -0.015, 0.015,
                      "wood_light" if index != 1 else "wood", splinter=True)


def finish_and_export():
    objects = PARTS + SPLINTERS
    triangles = 0
    for obj in objects:
        recalc(obj.data)
        for face in obj.data.polygons:
            face.use_smooth = False
        activate(obj)
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        obj.data.name = obj.name
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
        assert obj.parent is None
        assert obj.type == "MESH"
        assert all(value == 1 for value in obj.scale)
        bounds = [Vector(corner) for corner in obj.bound_box]
        bounds_center = (Vector(tuple(min(corner[axis] for corner in bounds)
                                      for axis in range(3)))
                         + Vector(tuple(max(corner[axis] for corner in bounds)
                                        for axis in range(3)))) / 2
        assert bounds_center.length < 0.00001, "Off-center piece pivot"
    bpy.context.view_layer.update()
    corners = [obj.matrix_world @ Vector(corner)
               for obj in objects for corner in obj.bound_box]
    minimum = Vector(tuple(min(corner[axis] for corner in corners) for axis in range(3)))
    maximum = Vector(tuple(max(corner[axis] for corner in corners) for axis in range(3)))
    dimensions = tuple(round(value, 4) for value in maximum - minimum)
    assert len(PARTS) == 32 and len(SPLINTERS) == 3
    assert len(bpy.context.scene.objects) == len(objects)
    assert [obj.name for obj in PARTS] == ["Piece_%02d" % i for i in range(len(PARTS))]
    assert [obj.name for obj in SPLINTERS] == ["Splinter_%d" % i for i in range(3)]
    assert triangles <= 6000, "Triangle budget exceeded: %d" % triangles
    assert abs(dimensions[0] - 4.6) < 0.0001, dimensions
    assert abs(dimensions[2] - 2.6) < 0.0001, dimensions
    assert abs(minimum.z) < 0.0001
    assert 0.30 <= dimensions[1] <= 0.34, dimensions
    assert set(material.name for obj in objects for material in obj.data.materials) == set(COLORS)

    bpy.ops.object.select_all(action="SELECT")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False,
    )
    assert OUTPUT.is_file() and OUTPUT.stat().st_size > 0
    print("EXPORTED Kit_WoodGate %d %d %s" % (len(PARTS), triangles, dimensions))
    print("DONE")


def main():
    setup()
    build_planks()
    rails = build_rails()
    build_brace()
    build_straps()
    build_gold(rails[1][1])
    build_splinters()
    finish_and_export()


if __name__ == "__main__":
    main()
