"""Export seven low-poly enemy projectile meshes in their game-space dimensions."""

import math
from pathlib import Path

import bmesh
import bpy


OUTPUT = Path(__file__).resolve().parents[2] / "Game/Assets/Resources/FX/EnemyProjectiles"
DARK = (0.12, 0.02, 0.05, 1)
RIM = (0.65, 0.17, 0.14, 1)
CORE = (1.0, 0.75, 0.08, 1)


class Surface:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.colors = []

    def vertex(self, x, y, z, color):
        index = len(self.vertices)
        self.vertices.append((x, -z, y))  # Blender Z is game up.
        self.colors.append(color)
        return index

    def join_rings(self, rings):
        for a, b in zip(rings, rings[1:]):
            for i in range(len(a)):
                j = (i + 1) % len(a)
                self.faces.append((a[i], a[j], b[j], b[i]))

    def lathe(self, profile, sides, turn=0):
        rings = []
        for height, radius, color in profile:
            rings.append([
                self.vertex(radius * math.cos(angle), radius * math.sin(angle), height, color)
                for angle in (turn + i * math.tau / sides for i in range(sides))
            ])
        self.join_rings(rings)
        self.faces += [tuple(reversed(rings[0])), tuple(rings[-1])]

    def torus(self, radius, tube, angle, color):
        rings = []
        for i in range(32):
            azimuth = math.tau * i / 32
            ring = []
            for j in range(6):
                around = math.tau * j / 6
                span = radius + tube * math.cos(around)
                x, y, z = span * math.cos(azimuth), span * math.sin(azimuth), tube * math.sin(around)
                ring.append(self.vertex(x, y * math.cos(angle) - z * math.sin(angle),
                                        y * math.sin(angle) + z * math.cos(angle), color))
            rings.append(ring)
        self.join_rings(rings + [rings[0]])

    def panel(self, outline, height, color, angle=0):
        face = []
        for x, z in outline:
            face.append(self.vertex(x * math.cos(angle) + height * math.sin(angle),
                                    height * math.cos(angle) - x * math.sin(angle), z, color))
        self.faces.append(tuple(face))

    def export(self, name):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices, [], self.faces)
        masks = mesh.color_attributes.new(name="Emission", type="FLOAT_COLOR", domain="POINT")
        for item, color in zip(masks.data, self.colors):
            item.color = color
        mesh.color_attributes.active_color = masks
        work = bmesh.new()
        work.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(work, faces=list(work.faces))
        work.to_mesh(mesh)
        work.free()
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        destination = OUTPUT / f"{name}.fbx"
        bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True,
                                 axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
                                 bake_space_transform=True, object_types={"MESH"}, colors_type="LINEAR",
                                 prioritize_active_color=True, mesh_smooth_type="FACE", bake_anim=False)
        print(f"{name}: {len(mesh.vertices)} vertices, {destination.stat().st_size} bytes")


def orb():
    mesh = Surface()
    mesh.lathe([(-0.32, 0.035, RIM), (-0.23, 0.20, CORE), (-0.08, 0.29, CORE),
                (0.08, 0.26, RIM), (0.23, 0.18, CORE), (0.34, 0.028, CORE)], 7, 0.15)
    mesh.torus(0.338, 0.019, 0.31, RIM)
    mesh.torus(0.338, 0.016, -0.48, (0.52, 0.1, 0.14, 1))
    return mesh


def slug():
    mesh = Surface()
    mesh.lathe([(-0.39, 0.085, DARK), (-0.33, 0.135, RIM), (-0.28, 0.137, CORE),
                (-0.20, 0.133, RIM), (-0.03, 0.147, CORE), (0.19, 0.129, CORE),
                (0.34, 0.065, CORE), (0.40, 0.019, CORE)], 9)
    return mesh


def pellet():
    mesh = Surface()
    mesh.lathe([(-0.26, 0.038, RIM), (-0.12, 0.146, RIM), (0.055, 0.151, CORE),
                (0.18, 0.098, CORE), (0.25, 0.019, CORE)], 6, 0.22)
    return mesh


def lance(long):
    mesh = Surface()
    extent = 0.81 if long else 0.59
    mesh.lathe([(-extent, 0.022, RIM), (-extent * 0.65, 0.087, RIM),
                (-0.21, 0.113, CORE), (-0.11, 0.121, RIM), (0.18, 0.112, CORE),
                (extent * 0.74, 0.046, CORE), (extent, 0.009, CORE)], 6)
    for i in range(3):
        mesh.panel([(0.03, -0.54), (0.22 if long else 0.16, -0.37), (0.11, 0.02), (0.04, 0.24)],
                   0, RIM, i * math.tau / 3)
    return mesh


def dial():
    mesh = Surface()
    mesh.lathe([(-0.075, 0.29, DARK), (-0.057, 0.346, RIM), (-0.027, 0.372, CORE),
                (0.027, 0.372, CORE), (0.057, 0.346, RIM), (0.075, 0.29, DARK)], 24)
    return mesh


def ticket():
    mesh = Surface()
    outline = [(-0.18, -0.36), (0.12, -0.37), (0.20, -0.33), (0.155, -0.16),
               (0.18, 0.30), (0.13, 0.37), (-0.13, 0.37), (-0.19, 0.31), (-0.155, -0.16)]
    rings = []
    for height, size, color in ((-0.046, 0.93, RIM), (0, 1, CORE), (0.046, 0.91, CORE)):
        rings.append([mesh.vertex(x * size, height + z * z * 0.42, z, color) for x, z in outline])
    mesh.join_rings(rings)
    mesh.faces += [tuple(reversed(rings[0])), tuple(rings[-1])]
    for z in (-0.13, 0.02, 0.16):
        mesh.panel([(-0.10, z), (0.07, z), (0.07, z + 0.025), (-0.10, z + 0.025)], 0.104, DARK)
    return mesh


if __name__ == "__main__":
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, shape in (("Orb", orb()), ("Revolver", slug()), ("Pellet", pellet()),
                        ("Sniper", lance(True)), ("Needle", lance(False)),
                        ("Numeral", dial()), ("Ticket", ticket())):
        shape.export(name)
