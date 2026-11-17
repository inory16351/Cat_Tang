# 굳은 승객 석상 10종: 분리 조각, 균열, 받침대와 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_passengers.py

import math
import random
import zlib
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Game/Assets/Art/Characters/Ch2"
PREVIEW = ROOT / "Art/tools/specs/kit_ch2_passengers_preview.png"
COLORS = {"statue": (0.72, 0.70, 0.66),
          "statue_dark": (0.52, 0.50, 0.47),
          "crack": (1.0, 1.0, 1.0),
          "statue_base": (0.45, 0.43, 0.40)}
BASE_HEIGHT = 0.10
STATUE_SEED = 0
MATERIALS = {}
PIECES = []
CRACKS = []
SHARDS = []


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


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
        shader.inputs["Roughness"].default_value = 0.87
        shader.inputs["Alpha"].default_value = 1.0
        shader.inputs["Transmission Weight"].default_value = 0.0
        MATERIALS[name] = material


class Geometry:
    """Build one fracture piece from overlapping, closed primitive volumes."""

    def __init__(self):
        self.vertices = []
        self.faces = []
        self.face_materials = []
        self.current_material = None

    def add(self, vertices, faces):
        offset = len(self.vertices)
        self.vertices.extend(tuple(vertex) for vertex in vertices)
        self.faces.extend(tuple(offset + index for index in face) for face in faces)
        self.face_materials.extend([self.current_material] * len(faces))
        return self

    def material(self, name):
        self.current_material = name
        return self

    def ellipsoid(self, center, radii, tilt=0.0, turn=0.0):
        # Six latitude bands keep rounded silhouettes within the triangle budget.
        center = Vector(center)
        vertices = [(0, 0, -radii[2])]
        for ring in range(1, 6):
            latitude = -math.pi / 2 + math.pi * ring / 6
            for side in range(12):
                angle = math.tau * side / 12
                vertices.append((radii[0] * math.cos(latitude) * math.cos(angle),
                                 radii[1] * math.cos(latitude) * math.sin(angle),
                                 radii[2] * math.sin(latitude)))
        vertices.append((0, 0, radii[2]))
        faces = []
        for side in range(12):
            nxt = (side + 1) % 12
            faces.append((0, 1 + nxt, 1 + side))
            for ring in range(4):
                a, b = 1 + ring * 12, 1 + (ring + 1) * 12
                faces.append((a + side, a + nxt, b + nxt, b + side))
            faces.append((61, 49 + side, 49 + nxt))
        transformed = []
        for x, y, z in vertices:
            # Positive tilt leans the local vertical toward the +Y front.
            y, z = y * math.cos(tilt) + z * math.sin(tilt), z * math.cos(tilt) - y * math.sin(tilt)
            x, y = x * math.cos(turn) - y * math.sin(turn), x * math.sin(turn) + y * math.cos(turn)
            transformed.append(center + Vector((x, y, z)))
        return self.add(transformed, faces)

    def tube(self, start, end, radius, end_radius=None, sides=10):
        start, end = Vector(start), Vector(end)
        direction = (end - start).normalized()
        u = direction.orthogonal().normalized()
        v = direction.cross(u).normalized()
        end_radius = radius if end_radius is None else end_radius
        vertices = []
        for center, size in ((start, radius), (end, end_radius)):
            vertices.extend(center + size * (u * math.cos(math.tau * i / sides)
                                             + v * math.sin(math.tau * i / sides))
                            for i in range(sides))
        faces = [tuple(reversed(range(sides))), tuple(range(sides, sides * 2))]
        faces.extend((i, (i + 1) % sides, (i + 1) % sides + sides, i + sides)
                     for i in range(sides))
        return self.add(vertices, faces)

    def limb(self, start, end, radius, tip_radius=None):
        tip_radius = radius * 0.87 if tip_radius is None else tip_radius
        self.tube(start, end, radius, tip_radius)
        self.ellipsoid(start, (radius, radius, radius))
        self.ellipsoid(end, (tip_radius, tip_radius, tip_radius))
        return self

    def loft(self, rings, start=0.0, end=math.tau, steps=12):
        # A partial ring makes a closed vertical coat/skirt fracture chunk.
        full = abs(end - start - math.tau) < 0.00001
        count = steps if full else steps + 1
        vertices = []
        for center, radii, hem_slope in rings:
            for index in range(count):
                angle = start + (end - start) * index / steps
                x, y = radii[0] * math.cos(angle), radii[1] * math.sin(angle)
                vertices.append((center[0] + x, center[1] + y, center[2] + hem_slope * y))
        faces = [tuple(reversed(range(count))), tuple(range((len(rings) - 1) * count, len(rings) * count))]
        for ring in range(len(rings) - 1):
            for index in range(steps):
                nxt = (index + 1) % count
                a, b = ring * count, (ring + 1) * count
                faces.append((a + index, a + nxt, b + nxt, b + index))
            if not full:
                faces.append((ring * count, (ring + 1) * count,
                              (ring + 2) * count - 1, (ring + 1) * count - 1))
        return self.add(vertices, faces)

    def box(self, center, dimensions, bevel=0.025):
        # Apply the bevel to temporary mesh data, then copy it into this piece.
        bpy.ops.mesh.primitive_cube_add(size=1, location=center)
        obj = bpy.context.object
        obj.dimensions = dimensions
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        modifier = obj.modifiers.new("Rounded corners", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        self.add([obj.matrix_world @ vertex.co for vertex in obj.data.vertices],
                 [tuple(face.vertices) for face in obj.data.polygons])
        mesh = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(mesh)
        return self

    def object(self, material="statue", group=None, label=""):
        group = PIECES if group is None else group
        prefix = "Piece_%02d" if group is PIECES else "Crack_%02d"
        name = "Shard_%d" % len(group) if group is SHARDS else prefix % len(group)
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        slots = [material] + sorted(set(m for m in self.face_materials if m and m != material))
        for slot in slots:
            obj.data.materials.append(MATERIALS[slot])
        for polygon, override in zip(mesh.polygons, self.face_materials):
            polygon.material_index = slots.index(override or material)
        obj["part"] = label
        group.append(obj)
        return obj


def blob(center, radii, label, tilt=0.0, turn=0.0):
    return Geometry().ellipsoid(center, radii, tilt, turn).object(label=label)


def arm(shoulder, elbow, hand):
    Geometry().limb(shoulder, elbow, 0.077).object(label="upper arm")
    Geometry().limb(elbow, hand, 0.066, 0.059).ellipsoid(
        hand, (0.079, 0.078, 0.086)).object(label="forearm and mitten hand")


def leg(hip, knee, ankle, foot, child=False, grounded=True):
    size = 0.72 if child else 1.0
    Geometry().limb(hip, knee, 0.10 * size, 0.078 * size).object(label="thigh")
    geo = Geometry().limb(knee, ankle, 0.078 * size, 0.065 * size)
    # The foot's long axis follows the ankle-to-toe direction in the sagittal plane.
    foot_axis = Vector(foot) - Vector(ankle)
    angle = math.atan2(-foot_axis.z, foot_axis.y)
    shoe_start = len(geo.vertices)
    geo.material("statue_dark").ellipsoid(foot, (0.087 * size, 0.155 * size, 0.065 * size), angle)
    lift = -min(vertex[2] for vertex in geo.vertices[shoe_start:]) if grounded else 0.0
    geo.vertices[shoe_start:] = [(x, y, z + lift) for x, y, z in geo.vertices[shoe_start:]]
    obj = geo.object(label="shin and shoe")
    obj["grounded"] = grounded
    obj["foot_center"] = (foot[0], foot[1], foot[2] + lift)
    return obj


def split_garment(rings, label, rear_only=False):
    spans = ((math.pi, math.pi * 1.5), (math.pi * 1.5, math.tau)) if rear_only else (
        (-math.pi / 2, math.pi / 2), (math.pi / 2, math.pi * 1.5))
    result = []
    for start, end in spans:
        result.append(Geometry().loft(rings, start, end, 6).object(label=label))
    return result


def luggage():
    torso = blob((0, 0.105, 1.185), (0.245, 0.16, 0.245), "forward leaning chest", math.radians(10))
    head = blob((0, 0.174, 1.445), (0.18, 0.165, 0.185), "head")
    hat = Geometry().loft([((0, 0.174, 1.586), (0.239, 0.207), 0),
                           ((0, 0.174, 1.613), (0.239, 0.207), 0)])
    hat.loft([((0, 0.174, 1.605), (0.17, 0.151), 0),
              ((0, 0.174, 1.685), (0.161, 0.144), 0),
              ((0, 0.174, 1.735), (0.106, 0.095), 0),
              ((0, 0.174, 1.75), (0.028, 0.025), 0)]).object("statue_dark", label="bowler hat")
    coats = split_garment([((0, -0.065, 0.51), (0.285, 0.23), 0.12),
                           ((0, -0.015, 0.77), (0.25, 0.18), 0),
                           ((0, 0.066, 1.045), (0.196, 0.145), 0)], "long coat half")
    # Facing +Y gives anatomical right at -X.
    arm((-0.22, 0.12, 1.31), (-0.37, 0.05, 1.00), (-0.49, 0.015, 0.69))
    arm((0.22, 0.12, 1.31), (0.37, 0.36, 1.10), (0.34, 0.59, 1.18))
    front_leg = leg((0.115, 0.02, 0.90), (0.13, 0.28, 0.47),
                    (0.13, 0.39, 0.105), (0.13, 0.49, 0.065))
    leg((-0.115, -0.045, 0.90), (-0.13, -0.27, 0.49),
        (-0.13, -0.43, 0.23), (-0.13, -0.32, 0.103))
    case = Geometry().box((-0.49, 0.015, 0.36), (0.55, 0.18, 0.4))
    handle = [(-0.59, 0.015, 0.55), (-0.59, 0.015, 0.65),
              (-0.39, 0.015, 0.65), (-0.39, 0.015, 0.55)]
    for a, b in zip(handle, handle[1:]):
        case.tube(a, b, 0.022, sides=8)
    case.object("statue_dark", label="suitcase with handle")
    return torso, head, coats[0], front_leg, (0, 0.105, 1.185)


def hat_woman():
    torso = blob((0, 0.005, 1.185), (0.212, 0.145, 0.235), "bodice")
    head = blob((-0.012, 0.025, 1.47), (0.17, 0.16, 0.185), "head turned right", turn=math.radians(20))
    hat = Geometry().loft([((-0.012, 0.025, 1.628), (0.335, 0.282), -0.06),
                           ((-0.012, 0.025, 1.653), (0.335, 0.282), -0.06)])
    hat.loft([((-0.012, 0.025, 1.645), (0.168, 0.156), 0),
              ((-0.012, 0.025, 1.735), (0.145, 0.135), 0),
              ((-0.012, 0.025, 1.75), (0.085, 0.079), 0)]).object("statue_dark", label="wide brim hat")
    skirts = split_garment([((0, -0.24, 0.275), (0.395, 0.405), -0.30),
                            ((0, -0.11, 0.57), (0.32, 0.30), -0.16),
                            ((0, 0.0, 1.04), (0.175, 0.14), 0)], "windswept bell skirt half")
    Geometry().loft([((0, -0.045, 1.09), (0.32, 0.215), 0.12),
                     ((0, 0.015, 1.35), (0.245, 0.16), 0),
                     ((0, 0.015, 1.39), (0.14, 0.115), 0)]).object(label="short cape")
    arm((0.22, 0.015, 1.32), (0.53, 0.06, 1.49), (0.17, 0.17, 1.655))
    arm((-0.22, -0.01, 1.31), (-0.40, -0.29, 1.12), (-0.51, -0.53, 1.23))
    front_leg = leg((0.12, 0, 0.9), (0.13, 0.08, 0.48),
                    (0.14, 0.12, 0.13), (0.14, 0.22, 0.065))
    leg((-0.12, -0.04, 0.9), (-0.13, -0.17, 0.48),
        (-0.14, -0.28, 0.13), (-0.14, -0.18, 0.065))
    return torso, head, skirts[0], front_leg, (0, 0.005, 1.185)


def child():
    torso = blob((0, 0.055, 0.755), (0.165, 0.125, 0.18), "running child chest", math.radians(15))
    head = blob((0, 0.12, 0.965), (0.155, 0.15, 0.155), "large child head")
    cap = Geometry().loft([((0, 0.13, 1.056), (0.175, 0.17), 0),
                           ((0, 0.105, 1.115), (0.164, 0.153), 0),
                           ((0, 0.085, 1.15), (0.07, 0.066), 0)])
    cap.ellipsoid((0, 0.244, 1.069), (0.158, 0.12, 0.021)).object("statue_dark", label="newsboy cap and visor")
    coats = split_garment([((0, -0.055, 0.505), (0.195, 0.16), 0.1),
                           ((0, 0.035, 0.735), (0.155, 0.12), 0)], "short coat half")
    # Smaller upper arms and mittens keep the child distinct from the adults.
    for shoulder, elbow, hand in (
        ((0.145, 0.075, 0.83), (0.29, 0.28, 0.80), (0.36, 0.43, 0.92)),
        ((-0.145, 0.035, 0.83), (-0.29, -0.18, 0.76), (-0.37, -0.36, 0.83)),
    ):
        Geometry().limb(shoulder, elbow, 0.058, 0.05).object(label="child upper arm")
        Geometry().limb(elbow, hand, 0.048, 0.043).ellipsoid(
            hand, (0.06, 0.06, 0.065)).object(label="child forearm and mitten")
    front_leg = leg((0.09, 0.01, 0.57), (0.11, 0.20, 0.32),
                    (0.11, 0.24, 0.19), (0.11, 0.305, 0.096), child=True)
    leg((-0.09, -0.025, 0.57), (-0.12, -0.31, 0.37),
        (-0.13, -0.49, 0.62), (-0.13, -0.51, 0.70), child=True, grounded=False)
    scarf = Geometry().loft([((0, 0.095, 0.85), (0.111, 0.105), 0),
                             ((0, 0.095, 0.90), (0.111, 0.105), 0)])
    scarf.add([(-0.06, 0.025, 0.88), (0.06, 0.025, 0.88),
               (0.13, -0.32, 0.96), (0.025, -0.40, 1.015),
               (-0.06, 0.025, 0.899), (0.06, 0.025, 0.899),
               (0.13, -0.32, 0.979), (0.025, -0.40, 1.034)],
              [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
               (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]).object("statue_dark", label="flying scarf")
    return torso, head, coats[0], front_leg, (0, 0.055, 0.755)


def watch_man():
    torso = blob((0, -0.035, 1.155), (0.225, 0.145, 0.285), "back leaning tailcoat chest", math.radians(-7))
    head = blob((0, -0.096, 1.43), (0.165, 0.15, 0.18), "head")
    Geometry().loft([((0, -0.096, 1.568), (0.245, 0.217), 0),
                     ((0, -0.096, 1.593), (0.245, 0.217), 0)]).loft([
                         ((0, -0.096, 1.584), (0.156, 0.14), 0),
                         ((0, -0.096, 1.78), (0.175, 0.157), 0),
                         ((0, -0.096, 1.80), (0.166, 0.149), 0)]).object("statue_dark", label="top hat")
    tails = split_garment([((0, -0.035, 0.46), (0.29, 0.28), 0),
                           ((0, -0.02, 0.76), (0.245, 0.21), 0),
                           ((0, -0.015, 1.04), (0.19, 0.15), 0)], "tailcoat tail", rear_only=True)
    arm((0.20, -0.045, 1.34), (0.36, 0.14, 1.11), (0.19, 0.40, 1.395))
    arm((-0.20, -0.045, 1.34), (-0.32, 0.005, 1.07), (-0.36, 0.18, 0.92))
    front_leg = leg((0.105, 0, 0.93), (0.12, 0.03, 0.49),
                    (0.13, 0.04, 0.13), (0.13, 0.14, 0.065))
    leg((-0.105, 0, 0.93), (-0.12, -0.04, 0.49),
        (-0.13, -0.055, 0.13), (-0.13, 0.045, 0.065))
    watch = Geometry().tube((0.13, 0.435, 1.451), (0.13, 0.459, 1.451), 0.07, sides=12)
    # The open lid swings outward to make two clearly separated discs.
    watch.tube((-0.005, 0.447, 1.458), (-0.026, 0.461, 1.458), 0.067, sides=12)
    chain = [(0.14, 0.437, 1.386), (0.14, 0.399, 1.33),
             (0.12, 0.32, 1.297), (0.105, 0.24, 1.32), (0.09, 0.11, 1.35)]
    for a, b in zip(chain, chain[1:]):
        watch.tube(a, b, 0.009, sides=6)
    watch.object("statue_dark", label="open pocket watch and chain")
    cane = Geometry().tube((-0.36, 0.18, 0.955), (-0.30, 0.39, 0.018), 0.018, sides=10).ellipsoid(
        (-0.30, 0.39, 0.018), (0.018, 0.018, 0.018)).ellipsoid(
        (-0.36, 0.18, 0.955), (0.045, 0.042, 0.042)).object("statue_dark", label="diagonally planted cane")
    cane["base_support"] = True
    return torso, head, tails[0], front_leg, (0, -0.035, 1.155)


def folded_sheet(columns, bottom, top, thickness=0.025):
    """Closed bent slab: columns are (x, y, vertical wave) in world space."""
    vertices = [(x, y + depth, z + wave)
                for depth in (-thickness / 2, thickness / 2)
                for z in (bottom, top) for x, y, wave in columns]
    n = len(columns)
    faces = []
    for i in range(n - 1):
        faces.extend([(i, i + 1, n + i + 1, n + i),
                      (2 * n + i, 3 * n + i, 3 * n + i + 1, 2 * n + i + 1),
                      (i, 2 * n + i, 2 * n + i + 1, i + 1),
                      (n + i, n + i + 1, 3 * n + i + 1, 3 * n + i)])
    faces.extend([(0, n, 3 * n, 2 * n),
                  (n - 1, 3 * n - 1, 4 * n - 1, 2 * n - 1)])
    return Geometry().add(vertices, faces)


def stone_hat(center, kind="bowler", scale=1.0, tilt=0.0):
    x, y, z = center
    rx, ry = ((0.20, 0.19) if kind == "cap" else (0.24, 0.21))
    rx, ry = rx * scale, ry * scale
    geo = Geometry().loft([((x, y, z), (rx, ry), tilt),
                           ((x, y, z + 0.025 * scale), (rx, ry), tilt)])
    geo.ellipsoid((x, y - 0.025 * scale, z + 0.055 * scale),
                  (rx * 0.78, ry * 0.78, 0.075 * scale), tilt=tilt)
    if kind == "cap":
        geo.ellipsoid((x, y + 0.14 * scale, z + 0.007),
                      (rx * 0.85, 0.115 * scale, 0.022 * scale))
    return geo


def newspaper_man():
    torso = Geometry().ellipsoid((0, 0, 1.17), (0.24, 0.16, 0.255))
    # Broad carved lapels and a projecting waistcoat make the opaque chest read.
    torso.box((-0.11, 0.144, 1.26), (0.07, 0.035, 0.22), 0.012)
    torso.box((0.11, 0.144, 1.26), (0.07, 0.035, 0.22), 0.012)
    torso = torso.object(label="waistcoat chest and carved lapels")
    head = blob((0, 0.005, 1.52), (0.185, 0.17, 0.195), "head over newspaper")
    stone_hat((0, 0, 1.665), "cap").object("statue_dark", label="flat cap")
    pelvis = blob((0, -0.02, 0.91), (0.23, 0.15, 0.16), "waistcoat lower hem")
    arm((-0.21, 0, 1.31), (-0.40, 0.19, 1.08), (-0.295, 0.43, 1.18))
    arm((0.21, 0, 1.31), (0.40, 0.19, 1.08), (0.295, 0.43, 1.18))
    shin = leg((-0.12, -0.01, 0.92), (-0.17, 0.18, 0.50),
               (-0.18, 0.10, 0.12), (-0.18, 0.21, 0.065))
    leg((0.12, -0.02, 0.92), (0.15, -0.06, 0.49),
        (0.16, -0.13, 0.12), (0.16, -0.02, 0.065))
    paper = folded_sheet([(-0.30, 0.45, 0.015), (0, 0.54, 0),
                          (0.30, 0.45, 0.015)], 0.99, 1.39)
    # Incised-looking page columns are shallow relief, part of the same prop.
    for side in (-1, 1):
        for z in (1.12, 1.19, 1.26, 1.33):
            paper.tube((side * 0.055, 0.543, z), (side * 0.26, 0.48, z + 0.012),
                       0.006, sides=4)
    paper.object("statue_dark", label="wide open newspaper with center fold")
    return torso, head, pelvis, shin, (0, 0, 1.17)


def waving_woman():
    torso = blob((-0.035, 0.015, 1.07), (0.205, 0.145, 0.22),
                 "twisted waving bodice", turn=math.radians(-28))
    head = blob((-0.10, 0.01, 1.365), (0.17, 0.16, 0.185), "turned head")
    bonnet = Geometry().ellipsoid((-0.10, -0.025, 1.43), (0.19, 0.18, 0.16))
    bonnet.loft([((-0.10, 0.02, 1.46), (0.225, 0.22), -0.15),
                  ((-0.10, 0.02, 1.485), (0.225, 0.22), -0.15)])
    bonnet.object("statue_dark", label="rounded bonnet and rim")
    skirts = split_garment([((0.07, -0.10, 0.16), (0.30, 0.27), -0.1),
                            ((0.015, -0.04, 0.55), (0.255, 0.22), 0),
                            ((-0.035, 0.015, 0.94), (0.155, 0.13), 0)], "long twisting dress")
    arm((-0.22, 0.015, 1.19), (-0.43, 0.05, 1.40), (-0.54, 0.03, 1.66))
    arm((0.15, 0.015, 1.19), (0.30, 0.15, 0.99), (0.015, 0.19, 1.17))
    shin = leg((-0.09, 0, 0.79), (-0.10, 0.06, 0.43),
               (-0.10, 0.09, 0.12), (-0.10, 0.20, 0.065))
    leg((0.12, -0.04, 0.79), (0.15, -0.08, 0.42),
        (0.16, -0.14, 0.12), (0.16, -0.03, 0.065))
    folded_sheet([(-0.55, 0.03, 0), (-0.69, -0.02, 0.06),
                  (-0.82, 0.035, 0.025), (-0.94, -0.04, 0.08)],
                 1.57, 1.75, 0.023).object("statue_dark", label="fluttering wavy handkerchief")
    return torso, head, skirts[0], shin, (-0.035, 0.015, 1.07)


def old_lady():
    torso = blob((0, 0.08, 0.98), (0.225, 0.18, 0.255), "hunched chest", tilt=0.32)
    head = Geometry().ellipsoid((0, 0.255, 1.245), (0.17, 0.155, 0.18), tilt=0.18)
    head.material("statue_dark").ellipsoid((0, 0.19, 1.33), (0.18, 0.16, 0.11)).ellipsoid(
        (0, 0.055, 1.30), (0.085, 0.085, 0.085))
    head = head.object(label="stooped head with hair and low bun")
    skirts = split_garment([((0, -0.045, 0.13), (0.26, 0.22), 0),
                            ((0, -0.035, 0.51), (0.24, 0.20), 0),
                            ((0, 0.015, 0.91), (0.17, 0.14), 0)], "old woman's long skirt")
    # Broad shoulder shawl folds over the pack, leaving its round silhouette exposed.
    shawl = Geometry().loft([((0, 0.045, 0.94), (0.29, 0.225), 0.22),
                             ((0, 0.095, 1.18), (0.255, 0.205), 0.14),
                             ((0, 0.14, 1.22), (0.13, 0.12), 0)])
    shawl.object(label="heavy folded shoulder shawl")
    bundle = Geometry().ellipsoid((0, -0.235, 1.10), (0.275, 0.255, 0.29))
    bundle.ellipsoid((0, -0.20, 1.38), (0.075, 0.07, 0.055))
    for x in (-0.10, 0.10):
        bundle.tube((x, -0.08, 1.30), (x, 0.19, 1.16), 0.023, sides=6)
    bundle.object("statue_dark", label="round tied cloth bundle and straps")
    arm((-0.21, 0.08, 1.11), (-0.31, 0.25, 0.88), (-0.31, 0.45, 0.86))
    arm((0.21, 0.08, 1.11), (0.32, 0.13, 0.92), (0.13, 0.26, 0.95))
    shin = leg((-0.10, -0.015, 0.74), (-0.13, 0.10, 0.39),
               (-0.13, 0.14, 0.11), (-0.13, 0.24, 0.06))
    leg((0.10, -0.03, 0.74), (0.12, -0.06, 0.39),
        (0.12, -0.13, 0.11), (0.12, -0.03, 0.06))
    cane = Geometry().tube((-0.31, 0.45, 0.88), (-0.27, 0.30, 0.023), 0.023, sides=8).tube(
        (-0.38, 0.45, 0.88), (-0.24, 0.45, 0.88), 0.032, sides=8).ellipsoid(
            (-0.27, 0.30, 0.023), (0.023,) * 3).object(
                "statue_dark", label="short planted walking stick")
    cane["base_support"] = True
    return torso, head, skirts[0], shin, (0, 0.08, 0.98)


def compact_leg(hip, knee, ankle, foot, size=1.0):
    """A whole articulated leg is one chunk in the two-person statue."""
    geo = Geometry().tube(hip, knee, 0.086 * size, 0.070 * size)
    geo.tube(knee, ankle, 0.070 * size, 0.057 * size)
    geo.ellipsoid(knee, (0.073 * size,) * 3)
    geo.material("statue_dark").ellipsoid(foot, (0.082 * size, 0.14 * size, 0.060 * size))
    obj = geo.object(label="whole leg and grounded shoe")
    obj["grounded"] = True
    obj["foot_center"] = foot
    return obj


def mother_child():
    # Mother strides to the left; child leans away to the right. The linked hands
    # bridge a deliberately open gap so the relationship reads from above.
    torso = blob((-0.30, 0.02, 1.14), (0.205, 0.145, 0.24), "mother pulling chest", turn=0.3)
    head = blob((-0.36, 0.045, 1.49), (0.18, 0.16, 0.19), "mother turned head")
    stone_hat((-0.36, 0.025, 1.635), scale=0.9).object("statue_dark", label="mother's hat")
    skirts = split_garment([((-0.23, -0.08, 0.26), (0.28, 0.255), -0.20),
                            ((-0.26, -0.035, 0.64), (0.235, 0.19), 0),
                            ((-0.29, 0.02, 1.0), (0.155, 0.13), 0)], "mother's striding skirt")
    shin = compact_leg((-0.40, 0.01, 0.87), (-0.44, 0.16, 0.46),
                       (-0.47, 0.23, 0.115), (-0.47, 0.30, 0.060))
    compact_leg((-0.20, -0.025, 0.87), (-0.14, -0.14, 0.46),
                (-0.12, -0.23, 0.115), (-0.12, -0.15, 0.060))
    Geometry().limb((-0.48, 0.02, 1.27), (-0.64, 0.22, 1.08), 0.07).limb(
        (-0.64, 0.22, 1.08), (-0.64, 0.37, 1.23), 0.06).ellipsoid(
            (-0.64, 0.37, 1.23), (0.073, 0.07, 0.08)).object(label="mother's forward bent arm")
    Geometry().limb((-0.12, 0.02, 1.25), (-0.02, 0.02, 1.01), 0.07).limb(
        (-0.02, 0.02, 1.01), (0.16, 0.015, 0.87), 0.058).ellipsoid(
            (0.16, 0.015, 0.87), (0.07, 0.07, 0.075)).object(label="mother's pulling arm and clasped hand")
    blob((0.47, -0.02, 0.65), (0.145, 0.115, 0.18), "child leaning back chest", turn=-0.25)
    blob((0.54, -0.04, 0.87), (0.135, 0.13, 0.14), "child looking back head")
    stone_hat((0.54, -0.045, 0.965), "cap", scale=0.70).object("statue_dark", label="small child's cap")
    # Lower coat overlaps the hips and is kept in the torso chunk's silhouette.
    compact_leg((0.40, 0, 0.55), (0.27, 0.12, 0.30),
                (0.22, 0.15, 0.075), (0.22, 0.19, 0.042), 0.70)
    compact_leg((0.53, -0.025, 0.55), (0.57, -0.12, 0.30),
                (0.63, -0.20, 0.075), (0.63, -0.14, 0.042), 0.70)
    # Both of the child's arms share one fracture chunk, keeping the total at 18.
    Geometry().limb((0.36, 0, 0.73), (0.27, 0.015, 0.79), 0.047).limb(
        (0.27, 0.015, 0.79), (0.16, 0.015, 0.87), 0.043).limb(
            (0.58, -0.02, 0.73), (0.77, -0.07, 0.77), 0.047).limb(
                (0.77, -0.07, 0.77), (0.92, -0.14, 0.89), 0.043).ellipsoid(
                    (0.92, -0.14, 0.89), (0.054, 0.054, 0.06)).object(label="child's linked and backward reaching arms")
    return torso, head, skirts[0], shin, (-0.30, 0.02, 1.14)


def sitting_man():
    torso = blob((0, -0.025, 0.80), (0.285, 0.21, 0.25), "stout seated yawning chest")
    head = blob((0, -0.065, 1.09), (0.185, 0.17, 0.185), "head tipped back", tilt=-0.22)
    stone_hat((0, -0.12, 1.22), tilt=-0.30).object("statue_dark", label="tilted back bowler hat")
    pelvis = blob((0, -0.045, 0.565), (0.27, 0.20, 0.16), "seated belly and hips")
    arm((-0.25, -0.02, 0.92), (-0.43, -0.035, 1.18), (-0.34, -0.015, 1.51))
    arm((0.25, -0.02, 0.92), (0.43, -0.035, 1.18), (0.34, -0.015, 1.51))
    shin = leg((-0.17, 0.02, 0.59), (-0.28, 0.21, 0.48),
               (-0.29, 0.20, 0.115), (-0.29, 0.29, 0.065))
    leg((0.17, 0.02, 0.59), (0.28, 0.21, 0.48),
        (0.29, 0.20, 0.115), (0.29, 0.29, 0.065))
    trunk = Geometry().box((0, -0.055, 0.25), (0.62, 0.40, 0.50), 0.035)
    trunk.box((0, -0.055, 0.48), (0.64, 0.42, 0.065), 0.018)
    for x in (-0.22, 0.22):
        trunk.box((x, 0.15, 0.27), (0.045, 0.025, 0.38), 0.008)
    trunk.box((0, 0.17, 0.37), (0.08, 0.035, 0.075), 0.009)
    trunk = trunk.object("statue_dark", label="big strapped travel trunk")
    trunk["base_support"] = True
    return torso, head, pelvis, shin, (0, -0.025, 0.80)


def runner():
    torso = blob((0, 0.16, 1.17), (0.205, 0.145, 0.27), "lanky lunging open coat chest", tilt=0.36)
    head = blob((0, 0.29, 1.50), (0.185, 0.165, 0.20), "runner's forward head", tilt=0.18)
    # This is its own floating fracture piece; its slender wisp overlaps the head.
    hat = stone_hat((0.025, -0.115, 1.66), tilt=-0.30)
    hat.tube((0.015, 0.18, 1.62), (0.025, -0.055, 1.69), 0.018, sides=6)
    hat.object("statue_dark", label="flying hat attached by a thin stone wisp")
    tails = []
    for side in (-1, 1):
        tails.append(Geometry().loft([
            ((side * 0.19, -0.75, 0.78), (0.11, 0.14), -0.30),
            ((side * 0.17, -0.33, 0.87), (0.12, 0.17), -0.30),
            ((side * 0.12, 0.07, 1.12), (0.10, 0.14), 0)], steps=8).object(label="flying open coat tail"))
    arm((-0.18, 0.22, 1.32), (-0.28, 0.53, 1.25), (-0.24, 0.84, 1.46))
    arm((0.18, 0.15, 1.31), (0.32, -0.14, 1.06), (0.30, -0.46, 1.17))
    shin = leg((-0.10, 0.06, 0.98), (-0.18, 0.43, 0.53),
               (-0.21, 0.59, 0.12), (-0.21, 0.70, 0.065))
    leg((0.10, 0, 0.98), (0.16, -0.40, 0.66),
        (0.20, -0.69, 0.37), (0.20, -0.78, 0.36), grounded=False)
    Geometry().box((-0.24, 0.88, 1.56), (0.16, 0.027, 0.24), 0.01).object(
        "statue_dark", label="raised train ticket")
    return torso, head, torso, shin, (0, 0.16, 1.17)


def bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (Vector(tuple(min(p[axis] for p in points) for axis in range(3))),
            Vector(tuple(max(p[axis] for p in points) for axis in range(3))))


def crack_strip(target, direction, points, width=0.015):
    """Project both edges onto the actual polygon surface, then extrude 6 mm."""
    direction = Vector(direction).normalized()
    across = Vector((direction.y, -direction.x, 0))
    up = Vector((0, 0, 1))
    low, high = bounds(target)
    center = (low + high) / 2
    half_up = (high.z - low.z) / 2
    inverse = target.matrix_world.inverted()
    vertices = []
    for index, point in enumerate(points):
        before = Vector(points[max(0, index - 1)])
        after = Vector(points[min(len(points) - 1, index + 1)])
        tangent = after - before
        perpendicular = Vector((-tangent.y, tangent.x)).normalized()
        for sign in (-1, 1):
            z = center.z + point[1] * half_up + perpendicular.y * sign * width / 2
            # Follow the changing cross-section of flared, tilted garments.
            # Their bounding-box center can lie outside the narrower waist.
            intersections = []
            for edge in target.data.edges:
                a, b = [target.matrix_world @ target.data.vertices[i].co for i in edge.vertices]
                if abs(b.z - a.z) > 0.000001 and min(a.z, b.z) <= z <= max(a.z, b.z):
                    intersections.append((a + (b - a) * ((z - a.z) / (b.z - a.z))).dot(across))
            assert intersections, target.name
            left, right = min(intersections), max(intersections)
            horizontal = (left + right) / 2 + point[0] * (right - left) / 2
            horizontal += perpendicular.x * sign * width / 2
            pos = center + across * (horizontal - center.dot(across)) + up * (z - center.z)
            origin = pos + direction * 3
            hit, location, normal, _ = target.ray_cast(inverse @ origin, inverse.to_3x3() @ -direction)
            assert hit, "Crack missed %s" % target["part"]
            location = target.matrix_world @ location
            normal = (target.matrix_world.to_3x3().inverted().transposed() @ normal).normalized()
            vertices.extend([location + normal * 0.004, location + normal * 0.010])
    faces = [(0, 1, 3, 2)]
    for index in range(len(points) - 1):
        a, b = index * 4, (index + 1) * 4
        faces.extend([(a, b, b + 2, a + 2), (a + 1, a + 3, b + 3, b + 1),
                      (a, a + 1, b + 1, b), (a + 2, b + 2, b + 3, a + 3)])
    end = (len(points) - 1) * 4
    faces.append((end, end + 2, end + 3, end + 1))
    obj = Geometry().add(vertices, faces).object("crack", CRACKS, "surface stone fracture")
    obj["starts_hidden"] = True


def add_cracks(torso, head, garment, shin):
    front = [(-0.32, 0.60), (-0.12, 0.34), (-0.24, 0.13),
             (0.03, -0.10), (-0.04, -0.35), (0.18, -0.60)]
    crack_strip(torso, (0, 1, 0), front, 0.017)
    crack_strip(torso, (0, 1, 0), [(-0.24, 0.13), (-0.04, 0.20),
                                 (0.17, 0.13), (0.28, 0.29), (0.43, 0.33)], 0.012)
    crack_strip(torso, (0, -1, 0), [(0.33, 0.6), (0.12, 0.35), (0.23, 0.10),
                                  (-0.07, -0.1), (0.04, -0.34), (-0.2, -0.56)])
    crack_strip(head, (0, 1, 0), [(-0.45, 0.38), (-0.22, 0.21), (-0.3, 0.0),
                                (0.0, -0.17), (-0.1, -0.35), (0.1, -0.48)], 0.013)
    crack_strip(head, (-1, 0, 0), [(-0.30, 0.46), (-0.08, 0.24), (-0.18, 0.05),
                                 (0.12, -0.10), (0.07, -0.36)], 0.012)
    crack_strip(garment, (0, -1, 0), [(-0.25, 0.55), (-0.07, 0.31), (-0.15, 0.05),
                                   (0.11, -0.18), (0.04, -0.36), (0.26, -0.58)], 0.018)
    crack_strip(garment, (0, -1, 0), [(-0.15, 0.05), (0.06, 0.14),
                                   (0.19, 0.07), (0.33, 0.23), (0.43, 0.29)], 0.012)
    crack_strip(shin, (0, 1, 0), [(-0.25, 0.48), (-0.04, 0.30), (-0.15, 0.10),
                                (0.12, -0.10), (0.04, -0.30)], 0.013)


def add_shards(center):
    for index, length in enumerate((0.08, 0.12, 0.16)):
        x, y, z = Vector(center) + Vector(((index - 1) * 0.025, 0, 0))
        thick = 0.01 + index * 0.005
        outline = [(-0.016, -length / 2), (0.028, -length * 0.12), (0.006, length / 2)]
        vertices = [(x + px, y + depth, z + pz) for depth in (-thick / 2, thick / 2)
                    for px, pz in outline]
        Geometry().add(vertices, [(0, 2, 1), (3, 4, 5), (0, 1, 4, 3),
                                  (1, 2, 5, 4), (2, 0, 3, 5)]).object("statue", SHARDS, "interior shard template")


def carve_stone(obj):
    """A few broad tool dents, with repeatable millimetric silhouette variation.

    Low spatial frequencies avoid noisy per-vertex displacement. Contact vertices
    stay on the ground; the slab has its own matched, hand-chipped construction.
    """
    seed = STATUE_SEED ^ zlib.crc32((obj.name + obj["part"]).encode("utf-8"))
    rng = random.Random(seed)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    low, high = bounds(obj)
    size = high - low
    center = (low + high) / 2
    amplitude = min(0.0025, max(size) * 0.006)
    phase = [rng.uniform(0, math.tau) for _ in range(3)]
    dents = [Vector(tuple(rng.uniform(low[i], high[i]) for i in range(3))) for _ in range(2)]
    radius = max(0.035, min(0.12, max(size) * 0.28))
    for vertex in obj.data.vertices:
        p = vertex.co.copy()
        if p.z < 0.012:  # Preserve exact contact for shoes, trunk and cane.
            continue
        ripple = sum(math.sin((p[i] - center[i]) * 11 + phase[i]) for i in range(3)) / 3
        dent = sum(math.exp(-(p - d).length_squared / radius ** 2) for d in dents)
        vertex.co += vertex.normal * amplitude * (0.65 * ripple - 1.25 * dent)
    obj["stone_seed"] = seed & 0x7FFFFFFF
    obj.data.update()


def prepare_figure(name, height, shard_center):
    """Normalize figure height, then lift it onto a base whose bottom stays Z=0."""
    for obj in PIECES:
        carve_stone(obj)
    bpy.context.view_layer.update()
    low_z = min(bounds(obj)[0].z for obj in PIECES)
    top = max(bounds(obj)[1].z for obj in PIECES)
    scale = height / (top - low_z)
    contacts = []
    for obj in PIECES:
        if obj.get("grounded", False) or obj.get("base_support", False):
            floor = min(v.co.z for v in obj.data.vertices)
            contacts.extend(v.co.copy() for v in obj.data.vertices if v.co.z < floor + 0.002)
    # Actual sole contacts, rather than ankle centers, also handle tilted shoes
    # and the sitter's trunk without placing their support behind the slab.
    offset = Vector(tuple((min(p[i] for p in contacts) + max(p[i] for p in contacts)) / 2
                          for i in range(2)) + (low_z,))
    xy = (0.82, 0.75) if name.endswith("MotherChild") else (1.0, 1.0)

    def transform(point):
        p = (Vector(point) - offset) * scale
        return Vector((p.x * xy[0], p.y * xy[1], p.z + BASE_HEIGHT))

    for obj in PIECES:
        for vertex in obj.data.vertices:
            vertex.co = transform(vertex.co)
        obj.data.update()
    bpy.context.view_layer.update()
    return transform(shard_center)


def add_base(height, shared=False):
    """Ten perimeter sides, two clipped corners, three precisely mating wedges."""
    rng = random.Random(STATUE_SEED ^ 0xBA5E)
    rx, ry = (0.56, 0.36) if shared else (0.42 * height / 1.75,) * 2
    contacts = []
    for obj in PIECES:
        if obj.get("grounded", False) or obj.get("base_support", False):
            floor = min(v.co.z for v in obj.data.vertices)
            contacts.extend(v.co for v in obj.data.vertices if v.co.z < floor + 0.002)
    # Allow a small pose-dependent increase so every sole and the trunk have
    # stone underneath, even at the indented/chamfered perimeter of the slab.
    fit = max(1.0, max(math.hypot(p.x / rx, p.y / ry) for p in contacts) / 0.86)
    rx, ry = rx * fit, ry * fit
    rim = []
    for i in range(8):
        angle = math.tau * i / 8 + 0.15
        radius = rng.uniform(0.96, 1.045)
        rim.append(Vector((rx * radius * math.cos(angle), ry * radius * math.sin(angle), 0)))
    # Replacing two original corners with pairs leaves an irregular decagon.
    outline = []
    for i, p in enumerate(rim):
        if i in (1, 5):
            outline.extend((p.lerp(rim[(i - 1) % 8], 0.20),
                            p.lerp(rim[(i + 1) % 8], 0.20)))
        else:
            outline.append(p)
    splits = (0, 3, 6, 10)
    seams = []
    for i in splits[:3]:
        p = outline[i]
        tangent = Vector((-p.y, p.x, 0)).normalized()
        seams.append([Vector(), p * 0.32 + tangent * 0.015,
                      p * 0.65 - tangent * 0.018, p])
    wedges = []
    for wedge in range(3):
        start, end = splits[wedge:wedge + 2]
        following = (wedge + 1) % 3
        polygon = seams[wedge] + [outline[i % 10] for i in range(start + 1, end + 1)]
        polygon += list(reversed(seams[following][1:-1]))
        outer = set(range(3, 4 + end - start))
        vertices = []
        for layer in range(4):
            for i, p in enumerate(polygon):
                z = (0, 0.012, 0.083, BASE_HEIGHT)[layer]
                q = p.copy()
                if i in outer:
                    if layer in (0, 3):
                        q *= 0.977  # A narrow hand-chipped rim bevel.
                    if layer == 3:
                        # Shared endpoints use the same analytic height on both wedges.
                        z -= 0.002 * (1 + math.sin(p.x * 31 + p.y * 19))
                vertices.append((q.x, q.y, z))
        n = len(polygon)
        faces = [tuple(reversed(range(n))), tuple(range(3 * n, 4 * n))]
        for layer in range(3):
            for i in range(n):
                j = (i + 1) % n
                faces.append((layer * n + i, layer * n + j,
                              (layer + 1) * n + j, (layer + 1) * n + i))
        obj = Geometry().add(vertices, faces).object("statue_base", label="jagged stone plinth wedge")
        obj["base_wedge"] = True
        wedges.append(obj)
    bpy.context.view_layer.update()
    for p in contacts:
        hits = [obj.ray_cast(Vector((p.x, p.y, BASE_HEIGHT + 0.02)), Vector((0, 0, -1)))
                for obj in wedges]
        heights = [hit[1].z for hit in hits if hit[0]]
        assert heights and abs(p.z - max(heights)) < 0.006, "Unsupported sole or prop on plinth"
    # Independent fine cracks lie on the actual upper faces, away from seams.
    for wedge, target in enumerate(wedges[:2]):
        a = math.atan2(outline[splits[wedge]].y / ry, outline[splits[wedge]].x / rx)
        b = math.atan2(outline[splits[wedge + 1] % 10].y / ry, outline[splits[wedge + 1] % 10].x / rx)
        if b <= a:
            b += math.tau
        angle = (a + b) / 2
        radial = Vector((rx * math.cos(angle), ry * math.sin(angle), 0))
        across = Vector((-radial.y, radial.x, 0)).normalized()
        points = [radial * t + across * jog for t, jog in
                  ((0.18, 0), (0.31, 0.012), (0.43, -0.008), (0.56, 0.009), (0.70, -0.005))]
        top_crack(target, points)


def top_crack(target, points):
    vertices = []
    for i, p in enumerate(points):
        tangent = points[min(i + 1, len(points) - 1)] - points[max(0, i - 1)]
        across = Vector((-tangent.y, tangent.x, 0)).normalized()
        for sign in (-1, 1):
            origin = p + across * sign * 0.0055 + Vector((0, 0, 3))
            hit, location, normal, _ = target.ray_cast(origin, Vector((0, 0, -1)))
            assert hit, "Base crack missed its wedge"
            vertices.extend((location + normal * 0.004, location + normal * 0.010))
    faces = [(0, 1, 3, 2)]
    for i in range(len(points) - 1):
        a, b = i * 4, (i + 1) * 4
        faces.extend(((a, b, b + 2, a + 2), (a + 1, a + 3, b + 3, b + 1),
                      (a, a + 1, b + 1, b), (a + 2, b + 2, b + 3, a + 3)))
    end = (len(points) - 1) * 4
    faces.append((end, end + 2, end + 3, end + 1))
    obj = Geometry().add(vertices, faces).object("crack", CRACKS, "plinth top fracture")
    obj["starts_hidden"] = True


def finish(name, height):
    objects = PIECES + CRACKS + SHARDS
    triangles = 0
    for obj in objects:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        assert all(edge.is_manifold for edge in bm.edges), obj.name
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
        activate(obj)
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
        low, high = bounds(obj)
        assert ((low + high) / 2 - obj.location).length < 0.00001
        assert obj.parent is None and obj.type == "MESH"
        obj.data.name = obj.name
        obj["passenger"] = name
    bpy.context.view_layer.update()
    low = Vector(tuple(min(bounds(obj)[0][i] for obj in objects) for i in range(3)))
    high = Vector(tuple(max(bounds(obj)[1][i] for obj in objects) for i in range(3)))
    dimensions = tuple(round(v, 4) for v in high - low)
    body_cracks = sum(obj["part"] == "surface stone fracture" for obj in CRACKS)
    assert 12 <= len(PIECES) <= 18 and 6 <= body_cracks <= 9 and len(SHARDS) == 3
    assert sum(bool(obj.get("base_wedge")) for obj in PIECES) == 3
    assert sum(obj["part"] == "plinth top fracture" for obj in CRACKS) == 2
    assert triangles <= 5500, (name, triangles)
    assert abs(low.z) < 0.00001 and abs(dimensions[2] - height - BASE_HEIGHT) < 0.0001, dimensions
    assert len(bpy.context.scene.objects) == len(objects)
    assert [obj.name for obj in PIECES] == ["Piece_%02d" % i for i in range(len(PIECES))]
    assert [obj.name for obj in CRACKS] == ["Crack_%02d" % i for i in range(len(CRACKS))]
    assert [obj.name for obj in SHARDS] == ["Shard_%d" % i for i in range(3)]
    assert set(mat.name for obj in objects for mat in obj.data.materials) == set(COLORS)
    bpy.ops.object.select_all(action="SELECT")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    path = OUTPUT / (name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
        object_types={"MESH"}, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, bake_anim=False, use_custom_props=True,
    )
    assert path.is_file() and path.stat().st_size > 0
    print("EXPORTED %s %d %d %d %s" % (name, len(PIECES), len(CRACKS), triangles, dimensions), flush=True)
    # Store detached preview copies before clearing the scene for the next FBX.
    copies = []
    for obj in objects:
        duplicate = obj.copy()
        duplicate.data = obj.data.copy()
        copies.append(duplicate)
    return copies


def render_preview(passengers):
    scene = bpy.context.scene
    target = Vector((0, 0, 0.8))
    view_offset = Vector((4, 7, 11.2))
    rotation = (-view_offset).to_track_quat("-Z", "Y")
    screen_right = rotation @ Vector((1, 0, 0))
    screen_up = rotation @ Vector((0, 1, 0))
    back = Vector((-view_offset.x, -view_offset.y, 0)).normalized()
    labels = []
    for index, (name, objects) in enumerate(passengers):
        placement = screen_right * (index % 5 - 2) * 2.12 + back * (0.5 - index // 5) * 3.6
        # Center each silhouette in its cell, including the mother's extended child.
        left = min((obj.location + Vector(corner)).dot(screen_right)
                   for obj in objects for corner in obj.bound_box)
        right = max((obj.location + Vector(corner)).dot(screen_right)
                    for obj in objects for corner in obj.bound_box)
        placement -= screen_right * (left + right) / 2
        for obj in objects:
            scene.collection.objects.link(obj)
            obj.location += placement
        bpy.context.view_layer.update()
        label = bpy.data.curves.new("Preview caption", "FONT")
        label.body = "%02d  %s" % (index + 1, name.removeprefix("Kit_Passenger_"))
        label.align_x = "CENTER"
        label.size = 0.105
        label.extrude = 0.0001
        obj = bpy.data.objects.new("Preview caption", label)
        scene.collection.objects.link(obj)
        bottom = min((piece.matrix_world @ Vector(corner)).dot(screen_up)
                     for piece in objects for corner in piece.bound_box)
        obj.location = placement + screen_right * (left + right) / 2
        obj.location += screen_up * (bottom - obj.location.dot(screen_up) - 0.18)
        obj.rotation_euler = rotation.to_euler()
        obj.data.materials.append(MATERIALS["statue"])
        labels.append(obj)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 2500
    scene.render.resolution_y = 1400
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.studiolight_rotate_z = math.radians(25)
    shading.color_type = "MATERIAL"
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = "BOTH"
    shading.show_specular_highlight = False
    shading.background_type = "WORLD"
    scene.world.color = (0.045, 0.055, 0.075)
    scene.view_settings.view_transform = "Standard"
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.location = target + view_offset
    camera.rotation_euler = rotation.to_euler()
    camera.data.type = "ORTHO"
    bpy.context.view_layer.update()
    inverse = camera.matrix_world.inverted()
    preview_objects = [obj for _, objects in passengers for obj in objects] + labels
    corners = [inverse @ (obj.matrix_world @ Vector(corner))
               for obj in preview_objects for corner in obj.bound_box]
    center_x = (min(p.x for p in corners) + max(p.x for p in corners)) / 2
    center_y = (min(p.y for p in corners) + max(p.y for p in corners)) / 2
    camera.location += camera.matrix_world.to_3x3() @ Vector((center_x, center_y, 0))
    width = max(p.x for p in corners) - min(p.x for p in corners)
    height = max(p.y for p in corners) - min(p.y for p in corners)
    aspect = scene.render.resolution_x / scene.render.resolution_y
    camera.data.ortho_scale = max(width, height * aspect) * 1.16
    scene.camera = camera
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(PREVIEW)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 0
    bpy.data.objects.remove(camera, do_unlink=True)


def clear_passenger():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    PIECES.clear()
    CRACKS.clear()
    SHARDS.clear()


def main():
    global STATUE_SEED
    setup()
    previews = []
    for name, builder, height in (
        ("Kit_Passenger_Luggage", luggage, 1.75),
        ("Kit_Passenger_Hat", hat_woman, 1.75),
        ("Kit_Passenger_Child", child, 1.15),
        ("Kit_Passenger_Watch", watch_man, 1.8),
        ("Kit_Passenger_Newspaper", newspaper_man, 1.75),
        ("Kit_Passenger_Wave", waving_woman, 1.7),
        ("Kit_Passenger_OldLady", old_lady, 1.45),
        ("Kit_Passenger_MotherChild", mother_child, 1.7),
        ("Kit_Passenger_Sitter", sitting_man, 1.25),
        ("Kit_Passenger_Runner", runner, 1.8),
    ):
        STATUE_SEED = zlib.crc32(name.encode("utf-8"))
        torso, head, garment, shin, center = builder()
        center = prepare_figure(name, height, center)
        bpy.context.view_layer.update()
        add_cracks(torso, head, garment, shin)
        add_shards(center)
        bpy.context.view_layer.update()
        for shard in SHARDS:
            for vertex in shard.data.vertices:
                for direction in ((1, 0, 0), (-1, 0, 0), (0, 1, 0),
                                  (0, -1, 0), (0, 0, 1), (0, 0, -1)):
                    assert torso.ray_cast(vertex.co, Vector(direction))[0], "Exposed interior shard"
        add_base(height, shared=name.endswith("MotherChild"))
        previews.append((name, finish(name, height)))
        clear_passenger()
    render_preview(previews)
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
