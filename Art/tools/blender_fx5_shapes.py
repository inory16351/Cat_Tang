# 5장 셰이더용 VFX 메시 7종과 UV 확인용 한 줄 프리뷰를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx5_shapes.py
"""Geometry only; UV0 drives CatTang/StylizedVfx. Preview colours never export."""

import colorsys
import math
from pathlib import Path
import random

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Resources/FX/Meshes5'
PREVIEW = ROOT / 'Art/tools/specs/blender_fx5_shapes_preview.png'
TAU = math.tau


class Surface:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.uvs = []

    def vertex(self, xyz):
        self.vertices.append(tuple(xyz))
        return len(self.vertices) - 1

    def face(self, vertices, uvs, reverse=False):
        self.faces.append(tuple(reversed(vertices)) if reverse else tuple(vertices))
        self.uvs.append(tuple(reversed(uvs)) if reverse else tuple(uvs))

    def finish(self, name):
        mesh = bpy.data.meshes.new('fx5_' + name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        uv = mesh.uv_layers.new(name='UVMap')
        for polygon, coords in zip(mesh.polygons, self.uvs):
            polygon.use_smooth = True
            for loop, coord in zip(polygon.loop_indices, coords):
                uv.data[loop].uv = coord
        obj = bpy.data.objects.new(mesh.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        return obj


def dome():
    s = Surface()
    around, across = 32, 12
    rings = []
    for j in range(across):
        latitude = math.pi * .5 * j / across
        rings.append([s.vertex((math.cos(latitude) * math.cos(TAU * i / around),
                                math.cos(latitude) * math.sin(TAU * i / around),
                                math.sin(latitude))) for i in range(around)])
    top = s.vertex((0, 0, 1))
    for j in range(across - 1):
        for i in range(around):
            k = (i + 1) % around
            s.face((rings[j][i], rings[j][k], rings[j+1][k], rings[j+1][i]),
                   ((2*i/around, j/across), (2*(i+1)/around, j/across),
                    (2*(i+1)/around, (j+1)/across), (2*i/around, (j+1)/across)))
    for i in range(around):
        s.face((rings[-1][i], rings[-1][(i+1) % around], top),
               ((2*i/around, 11/12), (2*(i+1)/around, 11/12),
                (2*(i+.5)/around, 1)))
    return s.finish('dome')


def circular_band(name, tall=False):
    s = Surface()
    around, across = 64, 2
    rings = []
    for j in range(across + 1):
        v = j / across
        radius, z = (1 + .12*v, .35*v) if tall else (.55 + .45*v, 0)
        rings.append([s.vertex((radius*math.cos(TAU*i/around),
                                radius*math.sin(TAU*i/around), z))
                      for i in range(around)])
    for j in range(across):
        for i in range(around):
            k = (i + 1) % around
            s.face((rings[j][i], rings[j][k], rings[j+1][k], rings[j+1][i]),
                   ((3*i/around, j/across), (3*(i+1)/around, j/across),
                    (3*(i+1)/around, (j+1)/across), (3*i/around, (j+1)/across)),
                   reverse=not tall)
    return s.finish(name)


def swirl():
    s = Surface()
    segments, width = 120, .45
    # Constant-width upright ribbons lean forward into the CCW spiral.
    # Their lower/upper edges, rather than their centrelines, bound Z=0..2.2.
    lean = .18
    edge_z = width * .5 / math.sqrt(1 + lean*lean)
    for strand in range(3):
        rows = []
        for i in range(segments + 1):
            t = i / segments
            theta = TAU * (1.5*t + strand/3)
            radius = .35 + .65*t
            centre = Vector((radius*math.cos(theta), radius*math.sin(theta),
                             edge_z + (2.2 - 2*edge_z)*t))
            across = Vector((-lean*math.sin(theta), lean*math.cos(theta), 1)).normalized()
            rows.append([s.vertex(centre + across*width*(v-.5)) for v in (0, 1)])
        for i in range(segments):
            s.face((rows[i][0], rows[i+1][0], rows[i+1][1], rows[i][1]),
                   ((3*i/segments, 0), (3*(i+1)/segments, 0),
                    (3*(i+1)/segments, 1), (3*i/segments, 1)))
    return s.finish('swirl')


def slash_arc():
    s = Surface()
    along, across = 96, 4
    # Keep the outer envelope at radius 1; inner edge meets it at both tips.
    start_angle, end_angle = math.radians(-100), math.radians(100)
    start = s.vertex((math.cos(start_angle), math.sin(start_angle), 0))
    rows = {}
    for i in range(1, along):
        t = i / along
        theta = start_angle + (end_angle-start_angle)*t
        inner = 1 - .55*math.sin(math.pi*t)
        rows[i] = [s.vertex(((inner+(1-inner)*j/across)*math.cos(theta),
                              (inner+(1-inner)*j/across)*math.sin(theta), 0))
                   for j in range(across+1)]
    end = s.vertex((math.cos(end_angle), math.sin(end_angle), 0))
    for j in range(across):
        # A single shared vertex at each tip; per-corner UV retains V coverage.
        s.face((start, rows[1][j], rows[1][j+1]),
               ((0, (j+.5)/across), (1/along, j/across), (1/along, (j+1)/across)), True)
        s.face((rows[along-1][j], end, rows[along-1][j+1]),
               (((along-1)/along, j/across), (1, (j+.5)/across),
                ((along-1)/along, (j+1)/across)), True)
    for i in range(1, along-1):
        for j in range(across):
            s.face((rows[i][j], rows[i+1][j], rows[i+1][j+1], rows[i][j+1]),
                   ((i/along, j/across), ((i+1)/along, j/across),
                    ((i+1)/along, (j+1)/across), (i/along, (j+1)/across)), True)
    return s.finish('slash_arc')


def spiral_ball():
    s = Surface()
    around, latitudes, radius = 48, 24, .7
    bottom = s.vertex((0, 0, 0))
    rings = {}
    for j in range(1, latitudes):
        latitude = -math.pi*.5 + math.pi*j/latitudes
        rings[j] = [s.vertex((radius*math.cos(latitude)*math.cos(TAU*i/around),
                              radius*math.cos(latitude)*math.sin(TAU*i/around),
                              radius + radius*math.sin(latitude))) for i in range(around)]
    top = s.vertex((0, 0, 2*radius))

    def uv(longitude, latitude):
        return (2*longitude + 1.5*latitude, latitude)

    for i in range(around):
        k = (i+1) % around
        s.face((bottom, rings[1][k], rings[1][i]),
               (uv((i+.5)/around, 0), uv((i+1)/around, 1/latitudes), uv(i/around, 1/latitudes)))
        for j in range(1, latitudes-1):
            s.face((rings[j][i], rings[j][k], rings[j+1][k], rings[j+1][i]),
                   (uv(i/around, j/latitudes), uv((i+1)/around, j/latitudes),
                    uv((i+1)/around, (j+1)/latitudes), uv(i/around, (j+1)/latitudes)))
        s.face((rings[latitudes-1][i], rings[latitudes-1][k], top),
               (uv(i/around, (latitudes-1)/latitudes), uv((i+1)/around, (latitudes-1)/latitudes),
                uv((i+.5)/around, 1)))
    # Explicit 48x24 resolution is 2,208 tris, slightly above the general budget.
    return s.finish('spiral_ball')


def spike_burst():
    s = Surface()
    rng = random.Random(55005)
    sides = 12
    for spike in range(10):
        theta = TAU*spike/10
        tilt = math.radians(rng.uniform(-8, 8))
        direction = Vector((math.cos(theta)*math.cos(tilt), math.sin(theta)*math.cos(tilt), math.sin(tilt)))
        sideways = Vector((-math.sin(theta), math.cos(theta), 0))
        upright = direction.cross(sideways).normalized()
        base = [s.vertex(.06*(sideways*math.cos(TAU*j/sides) + upright*math.sin(TAU*j/sides)))
                for j in range(sides)]
        tip, centre = s.vertex(direction), s.vertex((0, 0, 0))
        for j in range(sides):
            k = (j+1) % sides
            s.face((base[j], base[k], tip), ((0, j/sides), (0, (j+1)/sides), (1, (j+.5)/sides)))
            # Cap also has U=0 throughout, so length dissolve remains consistent.
            s.face((base[k], base[j], centre), ((0, (j+1)/sides), (0, j/sides), (0, (j+.5)/sides)))
    obj = s.finish('spike_burst')
    obj.data.set_sharp_from_angle(angle=math.radians(60))
    return obj


EXPECTED = {'dome': 736, 'ring': 256, 'ring_tall': 256, 'swirl': 720,
            'slash_arc': 760, 'spiral_ball': 2208, 'spike_burst': 240}


def validate(obj):
    mesh = obj.data
    name = obj.name.removeprefix('fx5_')
    mesh.calc_loop_triangles()
    assert len(mesh.loop_triangles) == EXPECTED[name], name
    assert all(p.area > 1e-10 and p.use_smooth for p in mesh.polygons), name
    assert all(math.isfinite(c) for v in mesh.vertices for c in v.co), name
    assert obj.location.length == 0 and tuple(obj.scale) == (1, 1, 1), name
    assert len(mesh.uv_layers) == 1
    uv = [tuple(item.uv) for item in mesh.uv_layers.active.data]
    assert all(math.isfinite(c) for coord in uv for c in coord), name
    assert abs(min(c[1] for c in uv)) < 1e-6 and abs(max(c[1] for c in uv)-1) < 1e-6, name
    if name in ('ring', 'slash_arc'):
        assert all(p.normal.z > .999 for p in mesh.polygons), name
        assert all(abs(v.co.z) < 1e-7 for v in mesh.vertices), name
    if name in ('dome', 'ring_tall', 'swirl'):
        assert all(p.normal.dot(Vector((p.center.x, p.center.y, 0))) > 0 for p in mesh.polygons), name
    if name == 'spiral_ball':
        assert all(p.normal.dot(p.center-Vector((0, 0, .7))) > 0 for p in mesh.polygons)
    return len(mesh.loop_triangles)


def export(obj):
    tris = validate(obj)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT / (obj.name + '.fbx')), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    print(f'EXPORTED {obj.name} {tris}', flush=True)


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 2800, 760
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.view_settings.view_transform = 'Standard'
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new('Preview background')
    scene.world.color = (.026, .033, .05)
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.background_type = 'WORLD'
    shade.show_shadows = False
    shade.show_cavity = False
    shade.show_specular_highlight = False
    shade.show_backface_culling = False
    # Face colours sample UV0: hue repeats along U, brightness marks V.
    # A temporary subdivided copy reveals the length gradient on cone triangles.
    palette = []
    for u in range(12):
        for v in range(4):
            mat = bpy.data.materials.new(f'PREVIEW_U{u}_V{v}')
            mat.diffuse_color = (*colorsys.hsv_to_rgb(u/12, .72, .42+.18*v), 1)
            palette.append(mat)
    text_mat = bpy.data.materials.new('Preview lettering')
    text_mat.diffuse_color = (.79, .87, .95, 1)
    helpers = []
    spacing = 2.65
    camera = bpy.data.objects.new('Preview camera', bpy.data.cameras.new('Preview camera'))
    scene.collection.objects.link(camera)
    helpers.append(camera)
    target = Vector((0, 0, .85))
    camera.location = target + Vector((0, -18, 23))
    camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 18.9
    scene.camera = camera
    screen_up = camera.rotation_euler.to_quaternion() @ Vector((0, 1, 0))

    def caption(body, position, size):
        data = bpy.data.curves.new('Preview caption', 'FONT')
        data.body, data.align_x, data.size = body, 'CENTER', size
        data.materials.append(text_mat)
        obj = bpy.data.objects.new('Preview caption', data)
        scene.collection.objects.link(obj)
        helpers.append(obj)
        obj.location = position
        obj.rotation_euler = camera.rotation_euler

    try:
        for index, source in enumerate(objects):
            source.hide_render = True
            obj = bpy.data.objects.new('Preview ' + source.name, source.data.copy())
            scene.collection.objects.link(obj)
            helpers.append(obj)
            obj.location.x = (index-3)*spacing
            # Subdivision is preview-only; interpolated UVs colour big triangles.
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            modifier = obj.modifiers.new('Preview UV sampling', 'SUBSURF')
            modifier.subdivision_type, modifier.levels = 'SIMPLE', 2
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            for mat in palette:
                obj.data.materials.append(mat)
            layer = obj.data.uv_layers.active.data
            for face in obj.data.polygons:
                u = sum(layer[i].uv.x for i in face.loop_indices) / len(face.loop_indices)
                v = sum(layer[i].uv.y for i in face.loop_indices) / len(face.loop_indices)
                face.material_index = (math.floor(u*12) % 12)*4 + min(3, max(0, int(v*4)))
            at = Vector((obj.location.x, 0, 0))
            name = source.name.removeprefix('fx5_')
            caption(name, at-screen_up*1.02, .18)
            caption(f'{EXPECTED[name]:,} tris', at-screen_up*1.28, .12)
        caption('CHAPTER 5  /  STYLIZED VFX SHAPES  /  UV0', screen_up*2.82, .22)
        caption('U = repeating hue / flow     |     V = dark to light / dissolve', screen_up*2.51, .14)
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    finally:
        scene.camera = None
        for obj in helpers:
            data = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if isinstance(data, bpy.types.Mesh):
                bpy.data.meshes.remove(data)
        for source in objects:
            source.hide_render = False
        for mat in palette + [text_mat]:
            bpy.data.materials.remove(mat, do_unlink=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1
    OUTPUT.mkdir(parents=True, exist_ok=True)
    objects = [dome(), circular_band('ring'), circular_band('ring_tall', True),
               swirl(), slash_arc(), spiral_ball(), spike_burst()]
    for obj in objects:
        export(obj)
    preview(objects)
    assert len(scene.objects) == 7
    assert all(obj.location.length == 0 and len(obj.data.materials) == 0 for obj in objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
