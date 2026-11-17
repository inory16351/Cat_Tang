# 챕터 2 이동 위협용 유령 증기기관차와 객차를 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_train.py
"""Static, metre-scale train kit. Travel is +Y, with bottom-centred origins."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2'
ROLES = set('wood wood_dark wood_light iron iron_dark rust brass rail_steel '
            'brick brick_dark stone stone_dark roof_tin soot coal window '
            'window_lit glass_lamp canvas leather leather_dark cloth_red '
            'paint_cream paint_green clock_face steam signal_red signal_amber '
            'signal_green train_glow'.split())
MATERIALS = {}
PARTS = []
GLOW_PARTS = []
RNG = random.Random(261117)
SERIAL = 0
BUDGET = 3000


def setup_materials():
    for name in sorted(ROLES):
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .85
        shader.inputs['Metallic'].default_value = 0
        MATERIALS[name] = mat


def mesh(label, verts, faces, role, bevel=False):
    global SERIAL
    assert role in ROLES, role
    if role == 'train_glow':
        assert label.startswith(('Headlight lens', 'Cowcatcher')), label
    SERIAL += 1
    data = bpy.data.meshes.new(f'{label}_{SERIAL}')
    data.from_pydata(verts, [], faces)
    data.update()
    data.materials.append(MATERIALS[role])
    obj = bpy.data.objects.new(data.name, data)
    bpy.context.collection.objects.link(obj)
    if bevel:
        # Weight only the upper perimeter, as in the landmark primitives.
        weights = data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
        top = max(v.co.z for v in data.vertices)
        for edge in data.edges:
            if all(abs(data.vertices[i].co.z - top) < 1e-5 for i in edge.vertices):
                weights.data[edge.index].value = 1
        mod = obj.modifiers.new('Chunky softened edges 25mm', 'BEVEL')
        mod.width = .025
        mod.segments = 2
        mod.limit_method = 'WEIGHT'
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    if role == 'train_glow':
        GLOW_PARTS.append(obj)
    return obj


def move(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role='iron', bevel=True, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return move(mesh(label, verts, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role='iron', n=12, top=None,
             rot=(0, 0, 0), bevel=False):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in [(radius, -depth / 2), (top, depth / 2)]
             for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return move(mesh(label, verts, faces, role, bevel), loc, rot)


def beam(label, a, b, width, role='iron', depth=None, bevel=False):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth or width, (b - a).length),
              role, bevel=bevel)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def rod(label, a, b, radius, role='brass', n=6):
    a, b = Vector(a), Vector(b)
    obj = cylinder(label, (0, 0, 0), radius, (b - a).length, role, n=n)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def arch(label, width, length, eaves, rise, thickness, role, y=0):
    """Closed shallow barrel section, also reused for transverse roof ribs."""
    profile = [(width * (j / 8 - .5), eaves + rise * math.sin(j * math.pi / 8))
               for j in range(9)]
    cross = profile + [(x, z - thickness) for x, z in reversed(profile)]
    n = len(cross)
    verts = [(x, end, z) for end in [y - length / 2, y + length / 2]
             for x, z in cross]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return mesh(label, verts, faces, role)


def engine():
    box('Full length engine footplate', (0, -.12, .99), (2.44, 7.8, .22), 'iron_dark')
    box('Deep engine frame', (0, -.15, .72), (1.7, 7.3, .35), 'iron_dark')
    # Axes across X. Fourteen sides include a vertex at the bottom of each tire.
    for y in [-2.22, -.64, .94]:
        rod('Driving axle', (-1.07, y, .7), (1.07, y, .7), .09, 'iron_dark')
        for side in [-1, 1]:
            x = side * 1.02
            cylinder('Driving wheel', (x, y, .7), .7, .23, 'iron', n=14,
                     rot=(0, math.pi / 2, 0))
            cylinder('Wheel recessed face', (side * 1.142, y, .7), .52, .025,
                     'iron_dark', n=10, rot=(0, math.pi / 2, 0))
            for j in range(3):
                angle = j * math.pi / 3 + .15
                dy, dz = .46 * math.cos(angle), .46 * math.sin(angle)
                beam('Broad driving wheel spoke', (side * 1.16, y - dy, .7 - dz),
                     (side * 1.16, y + dy, .7 + dz), .075, 'iron', depth=.075)
            cylinder('Brass crank pin', (side * 1.235, y + .12, .8), .10, .09,
                     'brass', n=6, rot=(0, math.pi / 2, 0))
    for side in [-1, 1]:
        x = side * 1.19
        beam('Brass coupling rod', (x, -2.10, .8), (x, 1.06, .8), .12,
             'brass', depth=.11, bevel=True)
        beam('Piston connecting rod', (x, 1.06, .8), (x, 2.70, 1.06), .09, 'brass')
        cylinder('Small leading wheel', (side * 1.02, 2.65, .37), .37, .20,
                 'iron', n=12, rot=(0, math.pi / 2, 0))
        cylinder('Front side cylinder', (side * .99, 2.66, 1.14), .27, .85,
                 'iron', n=10, rot=(math.pi / 2, 0, 0))
    cylinder('Long dark boiler', (0, .76, 1.85), .85, 5.02,
             'iron_dark', n=16, rot=(math.pi / 2, 0, 0), bevel=True)
    for y in [-1.12, .55, 2.83]:
        cylinder('Boiler raised iron band', (0, y, 1.85), .875, .105,
                 'iron', n=16, rot=(math.pi / 2, 0, 0))
    cylinder('Round smokebox door', (0, 3.31, 1.85), .77, .14,
             'iron', n=16, rot=(math.pi / 2, 0, 0), bevel=True)
    cylinder('Smokebox central latch', (0, 3.40, 1.85), .13, .08,
             'brass', n=8, rot=(math.pi / 2, 0, 0))
    box('Headlight saddle', (0, 3.05, 2.66), (.44, .43, .25), 'iron_dark')
    cylinder('Big brass headlight housing', (0, 3.15, 2.81), .50, .39,
             'brass', n=16, rot=(math.pi / 2, 0, 0), bevel=True)
    cylinder('Headlight lens facing positive Y', (0, 3.36, 2.81), .45, .035,
             'train_glow', n=16, rot=(math.pi / 2, 0, 0))
    cylinder('Tall soot smokestack', (0, 2.12, 3.08), .23, .96,
             'soot', n=12, top=.35)
    cylinder('Flared stack crown', (0, 2.12, 3.57), .37, .12, 'soot', n=12, top=.41)
    cylinder('Dark stack opening', (0, 2.12, 3.635), .29, .015, 'iron_dark', n=12)
    cylinder('Steam dome base', (0, .40, 2.67), .38, .18, 'brass', n=12)
    cylinder('Brass steam dome', (0, .40, 2.90), .34, .38,
             'brass', n=12, top=.24)
    cylinder('Smaller sand dome', (0, -.65, 2.78), .24, .31,
             'brass', n=10, top=.17)
    cylinder('Brass whistle stem', (.40, -.97, 2.85), .065, .42, 'brass', n=8)
    cylinder('Brass whistle cap', (.40, -.97, 3.08), .11, .10, 'brass', n=8)
    for side in [-1, 1]:
        rod('Boiler brass handrail', (side * .96, -1.20, 2.23),
            (side * .96, 2.97, 2.23), .04)
        for y in [-1.08, 1.0, 2.85]:
            rod('Handrail stand', (side * .78, y, 2.17), (side * .96, y, 2.23), .035)
    box('Wood driver cab', (0, -2.64, 2.24), (2.44, 2.60, 2.30), 'wood_dark')
    box('Green cab upper shell', (0, -2.64, 2.77), (2.46, 2.60, 1.24), 'paint_green')
    for side in [-1, 1]:
        box('Cab side window frame', (side * 1.236, -2.48, 2.65), (.055, 1.50, .85),
            'wood_dark', bevel=False)
        box('Cab side lit window', (side * 1.266, -2.48, 2.65), (.018, 1.30, .65),
            'window_lit', bevel=False)
        box('Cab window mullion', (side * 1.28, -2.50, 2.65), (.025, .07, .68),
            'wood_dark', bevel=False)
        box('Cab boarding step', (side * 1.12, -3.49, .69), (.32, .62, .16), 'iron')
        rod('Cab brass grab', (side * 1.25, -3.42, 1.20), (side * 1.25, -3.42, 2.35), .035)
        box('Cab front window frame', (side * .73, -1.32, 2.78), (.71, .055, .69),
            'wood_dark', bevel=False)
        box('Cab front lit window', (side * .73, -1.284, 2.78), (.55, .025, .53),
            'window_lit', bevel=False)
    box('Overhanging flat cab roof', (0, -2.64, 3.48), (2.6, 2.83, .20), 'roof_tin')
    for y in [-3.60, -2.95, -2.30, -1.65]:
        box('Cab roof cross rib', (0, y + RNG.uniform(-.012, .012), 3.59),
            (2.49, .07, .045), 'iron', bevel=False)
    box('Red front buffer beam', (0, 3.44, 1.02), (2.48, .28, .27), 'cloth_red')
    # Closed triangular wedge plus forward-sloping slats; every pilot face glows.
    verts = [(-1.22, 3.53, .17), (-1.22, 4.43, .17), (-1.04, 3.53, .91),
             (1.22, 3.53, .17), (1.22, 4.43, .17), (1.04, 3.53, .91)]
    mesh('Cowcatcher solid wedge', verts,
         [(0, 2, 1), (3, 4, 5), (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5)],
         'train_glow')
    for j in range(9):
        x = -1.16 + j * .29
        beam('Cowcatcher angled slat', (x * .85, 3.54, .97),
             (x, 4.46, .23), .085, 'train_glow')
    box('Cowcatcher broad front lip', (0, 4.45, .20), (2.5, .10, .14),
        'train_glow', bevel=False)
    box('Rear coupler shank', (0, -4.11, .79), (.22, .43, .16), 'iron', bevel=False)
    box('Rear coupler hook block', (0, -4.38, .79), (.40, .24, .24), 'iron', bevel=False)
    box('Rear hook jaw', (.14, -4.48, .86), (.12, .04, .20), 'iron', bevel=False)


def car():
    for y in [-2.22, 2.22]:
        box('Two axle bogie', (0, y, .61), (1.92, 1.52, .32), 'iron_dark')
        for dy in [-.49, .49]:
            rod('Coach axle', (-1.05, y + dy, .45), (1.05, y + dy, .45), .075,
                'iron_dark')
            for side in [-1, 1]:
                cylinder('Coach wheel', (side * 1.075, y + dy, .45), .45, .19,
                         'iron', n=12, rot=(0, math.pi / 2, 0))
                cylinder('Coach wheel hub', (side * 1.18, y + dy, .45), .15, .06,
                         'iron_dark', n=8, rot=(0, math.pi / 2, 0))
    box('Coach floor at one metre', (0, 0, .91), (2.46, 6.42, .18), 'iron_dark')
    box('Green passenger body', (0, 0, 1.80), (2.38, 6.35, 1.65), 'paint_green')
    for side in [-1, 1]:
        box('Wood lower panel band', (side * 1.20, 0, 1.23), (.065, 6.29, .44), 'wood')
        box('Cream stripe under windows', (side * 1.213, 0, 1.52),
            (.045, 6.30, .085), 'paint_cream', bevel=False)
        # Frames stand proud of the wall, lenses sit below their outer edges.
        for j in range(7):
            y = (j - 3) * .85
            box('Coach dark window surround', (side * 1.208, y, 2.04),
                (.075, .73, .83), 'wood_dark', bevel=False)
            box('Coach warm window pane', (side * 1.25, y, 2.04),
                (.018, .58, .66), 'window_lit', bevel=False)
            box('Coach recessed frame outer sill', (side * 1.266, y, 1.682),
                (.035, .75, .065), 'wood_dark', bevel=False)
    arch('Gently curved tin roof', 2.60, 6.58, 2.64, .24, .09, 'roof_tin')
    for j in range(8):
        y = -2.91 + j * .83 + RNG.uniform(-.014, .014)
        arch('Thin iron roof cross rib', 2.58, .065, 2.67, .24, .035, 'iron', y=y)
    box('Raised longitudinal clerestory', (0, 0, 2.90), (.74, 6.12, .23), 'roof_tin')
    box('Clerestory cap', (0, 0, 3.035), (.85, 6.24, .07), 'roof_tin')
    for y in [-2.1, -.7, .7, 2.1]:
        cylinder('Small roof vent', (0, y, 3.105), .14, .11, 'iron_dark', n=8)
        cylinder('Roof vent mushroom cap', (0, y, 3.175), .21, .05,
                 'iron_dark', n=8, top=.17)
    for end in [-1, 1]:
        box('End platform floor', (0, end * 3.49, .93), (2.38, .65, .14), 'iron_dark')
        box('Wood end door', (0, end * 3.19, 1.77), (.76, .08, 1.53), 'wood_dark')
        box('End door window', (0, end * 3.238, 2.05), (.47, .025, .59),
            'window_lit', bevel=False)
        cylinder('End door brass handle', (.25, end * 3.255, 1.60), .045, .04,
                 'brass', n=6, rot=(math.pi / 2, 0, 0))
        for side in [-1, 1]:
            x = side * 1.12
            for y in [3.25, 3.74]:
                rod('End platform brass post', (x, end * y, 1.0),
                    (x, end * y, 1.83), .035)
            rod('Platform side top rail', (x, end * 3.25, 1.83),
                (x, end * 3.74, 1.83), .035)
            # Leave a central gangway opening aligned with the coupler.
            rod('Platform end top rail', (side * .48, end * 3.74, 1.83),
                (x, end * 3.74, 1.83), .035)
            rod('Platform end middle rail', (side * .48, end * 3.74, 1.39),
                (x, end * 3.74, 1.39), .028)
        box('Coach end coupler shank', (0, end * 3.75, .79), (.22, .38, .16), 'iron')
        box('Coach end coupler block', (0, end * 3.89, .79), (.40, .22, .24), 'iron')


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, target_length):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + '_Mesh'
    count = triangles(obj)
    assert count <= BUDGET, (name, 'post-modifier triangles', count, BUDGET)
    coords = [v.co for v in obj.data.vertices]
    low = Vector([min(v[i] for v in coords) for i in range(3)])
    high = Vector([max(v[i] for v in coords) for i in range(3)])
    # Translation only: preserve primitive proportions and wheel contact planes.
    offset = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
    for v in obj.data.vertices:
        v.co -= offset
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), (name, 'open component')
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    dims = tuple(round(d, 4) for d in obj.dimensions)
    assert obj.dimensions.x <= 2.6 + 1e-3, (name, dims)
    assert abs(obj.dimensions.y - target_length) < .06, (name, dims)
    assert abs(min(v.co.z for v in obj.data.vertices)) < 1e-5
    for axis in [0, 1]:
        assert abs(min(v.co[axis] for v in obj.data.vertices) +
                   max(v.co[axis] for v in obj.data.vertices)) < 1e-5
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(mat and mat.name in ROLES for mat in obj.data.materials)
    glow_faces = [p for p in obj.data.polygons
                  if obj.data.materials[p.material_index].name == 'train_glow']
    assert bool(glow_faces) == (name == 'TrainEngine')
    obj['front'] = '+Y'
    obj['origin_rule'] = 'bottom center including couplers and cowcatcher'
    obj['dimensions_m'] = dims
    obj['triangle_count'] = count
    obj['chapter'] = 'The Stopped Station'
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    assert destination.is_file() and destination.stat().st_size > 0, destination
    print(f'EXPORTED {name} {count} {dims}', flush=True)
    return obj


def main():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    setup_materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    exported = []
    for index, (name, length, build) in enumerate([
            ('TrainEngine', 9.0, engine), ('TrainCar', 8.0, car)]):
        PARTS.clear()
        GLOW_PARTS.clear()
        RNG.seed(261117 + index * 7919)
        build()
        assert bool(GLOW_PARTS) == (name == 'TrainEngine')
        exported.append(finish(name, length))
    assert [obj.name for obj in exported] == ['TrainEngine', 'TrainCar']
    assert len(bpy.context.scene.objects) == 2
    assert all(obj.type == 'MESH' for obj in bpy.context.scene.objects)
    for mat in MATERIALS.values():
        shader = mat.node_tree.nodes['Principled BSDF']
        assert tuple(shader.inputs['Base Color'].default_value) == (.5, .5, .5, 1)
        assert not shader.inputs['Base Color'].is_linked
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
