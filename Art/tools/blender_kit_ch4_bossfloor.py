# Chapter 4 Boss Arena: silver grand clock dial floor → FBX + preview PNG.
# Run: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" --factory-startup -b -P C:/Project/Cat_Tang/Art/tools/blender_kit_ch4_bossfloor.py
"""Silver clock dial floor for the Ch4 twin-hands boss arena.

Flat 20 m radius dial with numbers 1-12, sector lines, minute ticks, centre hub.
Role shaders stay grey (.5) in the FBX.  Preview applies the noir palette.
"""
import math
from array import array
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4/Kit_Ch4_BossFloor.fbx'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_bossfloor_preview.png'
ROLES = set('dial_face dial_silver dial_number dial_line dial_black red_dark red_lacquer'.split())   # 빨강 = 2026-11-26 시계 디테일 (바닥 숫자 칸과 같은 빨강)
MATERIALS, PARTS = {}, []
TAU = math.tau
PALETTE = {
    'dial_face':   (0.92, 0.91, 0.89),
    'dial_silver': (0.78, 0.76, 0.73),
    'dial_number': (0.08, 0.08, 0.08),
    'dial_line':   (0.15, 0.14, 0.13),
    'dial_black':  (0.04, 0.04, 0.04),
    'red_dark':    (0.42, 0.03, 0.05),
    'red_lacquer': (0.78, 0.06, 0.08),
}


# ---------------------------------------------------------------------------
#  Helpers (same conventions as blender_kit_ch2_arena.py)
# ---------------------------------------------------------------------------

def setup_materials():
    for name in sorted(ROLES):
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .7
        shader.inputs['Metallic'].default_value = 0
        MATERIALS[name] = mat


def _mesh(label, verts, faces, role):
    assert role in ROLES, f'{role} not in {ROLES}'
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATERIALS[role])
    PARTS.append(obj)
    return obj


def polar(radius, angle):
    return radius * math.cos(angle), radius * math.sin(angle)


def slab(label, outline, bottom, top, role):
    n = len(outline)
    verts = [(x, y, z) for z in (bottom, top) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return _mesh(label, verts, faces, role)


def transform(obj, x=0, y=0, angle=0):
    obj.data.transform(Matrix.Translation((x, y, 0)) @ Matrix.Rotation(angle, 4, 'Z'))
    return obj


def box(label, x, y, sx, sy, bottom, top, role, angle=0):
    return transform(slab(label, [(-sx / 2, -sy / 2), (sx / 2, -sy / 2),
                                   (sx / 2,  sy / 2), (-sx / 2,  sy / 2)],
                          bottom, top, role), x, y, angle)


def disc(label, radius, bottom, top, role, n=36):
    return slab(label, [polar(radius, j * TAU / n) for j in range(n)],
                bottom, top, role)


def ring(label, inner, outer, bottom, top, role, n=48):
    verts = [(r * math.cos(j * TAU / n), r * math.sin(j * TAU / n), z)
             for r, z in ((inner, bottom), (outer, bottom), (outer, top), (inner, top))
             for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    return _mesh(label, verts, faces, role)


def clock_angle(hour):
    """Angle (from +X axis) for a clock hour.  12 → +Y, 3 → +X, 6 → −Y."""
    return math.pi / 2 - hour * TAU / 12


# ---------------------------------------------------------------------------
#  Geometry builders
# ---------------------------------------------------------------------------

def build_face():
    """Main dial-face disc (flat, z = 0)."""
    disc('Dial face', 19.5, -0.005, 0.0, 'dial_face', n=36)


def build_rim():
    """Outer rim ring (silver body + thin black outer edge) with minute ticks."""
    ring('Rim silver', 19.5, 19.92, 0, 0.25, 'dial_silver', n=48)
    ring('Rim black edge', 19.92, 20.0, 0, 0.25, 'dial_black', n=48)

    for i in range(60):
        a = clock_angle(i / 5)          # 60 ticks map to 12 hours
        is_quarter = (i % 15 == 0)      # 12, 3, 6, 9
        is_five    = (i % 5 == 0)
        if is_quarter:
            h, w, ln = 0.08, 0.10, 0.35
        elif is_five:
            h, w, ln = 0.05, 0.07, 0.28
        else:
            h, w, ln = 0.03, 0.04, 0.18
        cx, cy = polar(19.71, a)
        box(f'Tick_{i}', cx, cy, ln, w, 0.25, 0.25 + h, 'dial_line', a)


def digit_geometry(number, height, radius, role):
    """Create one clock number as an extruded text mesh (Arial Black)."""
    a = clock_angle(number)
    cx, cy = polar(radius, a)

    bpy.ops.object.text_add(location=(0, 0, 0))
    tobj = bpy.context.object
    tobj.data.body = str(number)
    tobj.data.size = height
    tobj.data.align_x = 'CENTER'
    tobj.data.align_y = 'CENTER'
    tobj.data.extrude = 0.03
    tobj.data.resolution_u = 2          # keep polys low

    font_path = Path('C:/Windows/Fonts/ariblk.ttf')
    if font_path.exists():
        tobj.data.font = bpy.data.fonts.load(str(font_path))
    else:
        print(f'WARNING: Arial Black not found, using default font', flush=True)

    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.object
    obj.name, obj.data.name = f'Digit_{number}', f'Digit_{number}_Mesh'

    obj.data.materials.clear()
    obj.data.materials.append(MATERIALS[role])

    # Centre the actual mesh bbox, then place at clock position
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    mx = (min(xs) + max(xs)) / 2
    my = (min(ys) + max(ys)) / 2
    # Extrude goes −Z; shift so back face sits at z ≈ 0.005
    min_z = min(v.co.z for v in obj.data.vertices)
    obj.data.transform(Matrix.Translation((cx - mx, cy - my, 0.005 - min_z)))

    PARTS.append(obj)
    return obj


def build_numbers():
    """Numbers 1-12 around the dial at radius 16 m."""
    for h in range(1, 13):
        digit_geometry(h, 1.4, 16.0, 'dial_number')


def build_lines():
    """12 sector-boundary lines + 12 inter-number radial markers."""
    # Sector boundaries: hour positions, radius 1–19 m, width 0.06 m
    for h in range(12):
        a = clock_angle(h)
        mid_r = (1.0 + 19.0) / 2
        length = 19.0 - 1.0
        cx, cy = polar(mid_r, a)
        box(f'Sector_{h}', cx, cy, length, 0.06, 0.005, 0.015, 'dial_line', a)

    # Short markers between numbers: mid-hour angles, radius 13–15.5 m
    for h in range(12):
        a = clock_angle(h + 0.5)
        mid_r = (13.0 + 15.5) / 2
        length = 15.5 - 13.0
        cx, cy = polar(mid_r, a)
        box(f'MidLine_{h}', cx, cy, length, 0.04, 0.005, 0.015, 'dial_line', a)


def build_centre_hub():
    """Dome + cone axis decoration, radius 1 m, height 0.5 m."""
    n = 12
    profile = [
        (1.00, 0.00), (0.96, 0.07), (0.87, 0.14), (0.74, 0.20),
        (0.56, 0.26), (0.35, 0.33), (0.0,  0.50),
    ]
    verts, ring_starts = [], []
    for r, z in profile:
        ring_starts.append(len(verts))
        if r > 0:
            for j in range(n):
                verts.append((r * math.cos(j * TAU / n),
                              r * math.sin(j * TAU / n), z))
        else:
            verts.append((0, 0, z))

    faces = [tuple(reversed(range(n)))]         # bottom cap
    for i in range(len(profile) - 2):
        b = ring_starts[i]
        nb = ring_starts[i + 1]
        for j in range(n):
            j1 = (j + 1) % n
            faces.append((b + j, b + j1, nb + j1, nb + j))
    tip = ring_starts[-1]
    lr = ring_starts[-2]
    for j in range(n):
        faces.append((lr + j, lr + (j + 1) % n, tip))

    _mesh('Centre hub', verts, faces, 'dial_silver')


def build_decorative_rings():
    """Two thin rings at radius 10 m and 14 m."""
    ring('Deco ring 10m', 9.96, 10.04, 0.005, 0.02, 'dial_silver', n=48)
    ring('Deco ring 14m', 13.96, 14.04, 0.005, 0.02, 'dial_silver', n=48)


def diamond(label, r, a, length, width, bottom, top, role):
    """반지름 방향으로 긴 마름모 (a = 각도)"""
    c, s_ = math.cos(a), math.sin(a)
    cx, cy = r * c, r * s_
    pts = [(cx + c * length / 2, cy + s_ * length / 2), (cx - s_ * width / 2, cy + c * width / 2),
           (cx - c * length / 2, cy - s_ * length / 2), (cx + s_ * width / 2, cy - c * width / 2)]
    return slab(label, pts, bottom, top, role)


def build_clock_detail():
    """2026-11-26 사용자: 경기장 바닥도 시계 디테일 더 (바닥 숫자 칸과 같은 결: 빨간 테 · 분 눈금 · 시 눈금 · 마름모).
    게임 판정 자리(칸 숫자 반지름 11m · 칸 경계선)는 가리지 않게 얇고 낮게."""
    # ① 바깥 테 안쪽 빨간 띠 + 검은 선
    ring('Rim red band', 19.05, 19.5, 0.0, 0.02, 'red_dark', n=96)
    ring('Rim red inner line', 18.9, 18.97, 0.0, 0.018, 'dial_line', n=96)
    # ② 숫자 바깥 분 눈금 트랙 (17.4 ~ 18.4m): 두 줄 + 분 눈금 60 + 시 눈금 12(빨강) + 12/3/6/9 큰 마름모
    ring('Minute track outer', 18.35, 18.42, 0.0, 0.018, 'dial_line', n=96)
    ring('Minute track inner', 17.38, 17.45, 0.0, 0.018, 'dial_line', n=96)
    for i in range(60):
        a = clock_angle(i / 5)
        if i % 15 == 0:
            diamond(f'Cardinal_{i}', 17.9, a, 1.3, 0.55, 0.0, 0.03, 'red_lacquer')
        elif i % 5 == 0:
            cx, cy = polar(17.9, a)
            box(f'HourTick_{i}', cx, cy, 0.9, 0.22, 0.0, 0.025, 'red_dark', a)
        else:
            cx, cy = polar(18.05, a)
            box(f'MinTick_{i}', cx, cy, 0.5, 0.07, 0.0, 0.02, 'dial_line', a)
    # ③ 숫자 안쪽 테 (14.6m) — 숫자 띠를 감쌈
    ring('Numeral band inner line', 14.55, 14.62, 0.0, 0.018, 'dial_line', n=96)
    # ④ 10m 장식 테: 은빛 선 위에 작은 마름모 48개 (기요셰 느낌)
    for j in range(48):
        diamond(f'Guilloche_{j}', 10.0, j * TAU / 48 + TAU / 96, 0.42, 0.18, 0.0, 0.022, 'dial_silver')
    # ⑤ 가운데 햇살: 2.2 ~ 4.2m 가는 빛살 36개 + 안팎 테, 축 둘레 빨간 테
    ring('Sunburst outer', 4.2, 4.27, 0.0, 0.018, 'dial_line', n=64)
    ring('Sunburst inner', 2.13, 2.2, 0.0, 0.018, 'dial_line', n=64)
    for j in range(36):
        a = j * TAU / 36
        length = 1.9 if j % 3 == 0 else 1.2
        cx, cy = polar(2.25 + length / 2, a)
        box(f'Ray_{j}', cx, cy, length, 0.05 if j % 3 else 0.09, 0.0, 0.016, 'dial_line', a)
    ring('Hub red ring', 1.02, 1.35, 0.0, 0.03, 'red_dark', n=48)
    ring('Hub black line', 1.35, 1.42, 0.0, 0.028, 'dial_black', n=48)


def build():
    build_face()
    build_rim()
    build_numbers()
    build_lines()
    build_centre_hub()
    build_decorative_rings()
    build_clock_detail()


# ---------------------------------------------------------------------------
#  Finish / export / preview  (same pattern as ch2)
# ---------------------------------------------------------------------------

def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        tiny = [f for f in bm.faces if f.calc_area() <= 1e-10]
        if tiny:
            print(f'CLEANUP {obj.name} zero-area faces {len(tiny)}', flush=True)
            bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=list(bm.edges))
            bm.to_mesh(obj.data)
        bm.free()
        obj.select_set(False)

    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = 'BossFloor', 'BossFloor_Mesh'

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bmesh.ops.triangulate(bm, faces=list(bm.faces), ngon_method='BEAUTY')
    bm.to_mesh(obj.data)
    bm.free()

    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.location = obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    bpy.context.view_layer.update()

    tc = triangles(obj)
    print(f'Triangle count: {tc}', flush=True)
    assert tc <= 16000, f'Too many triangles: {tc}'
    assert abs(min(v.co.z for v in obj.data.vertices)) < 0.01
    assert all(all(math.isfinite(c) for c in v.co) for v in obj.data.vertices)
    assert set(m.name for m in obj.data.materials) <= ROLES

    obj['dimensions_m'] = [40, 40, round(obj.dimensions[2], 3)]
    obj['front'], obj['origin_rule'] = '+Y', 'bottom center'
    obj['chapter'] = 'Ch4 Silver Clock Tower'
    obj['triangle_count'] = tc
    obj['hour_count'], obj['minute_tick_count'] = 12, 60
    obj['design_note'] = 'Silver grand clock dial boss arena floor'
    return obj


def export(obj):
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    assert len(bpy.context.scene.objects) == 1
    for mat in obj.data.materials:
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert OUTPUT.is_file() and OUTPUT.stat().st_size > 1024
    sz = OUTPUT.stat().st_size
    print(f'EXPORTED BossFloor {triangles(obj)} tris  {sz} bytes  → {OUTPUT}', flush=True)


def preview(obj):
    helpers = []
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples, scene.cycles.use_denoising = 24, True
    scene.render.resolution_x = scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'

    scene.world = bpy.data.worlds.new('Preview world')
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs['Color'].default_value = (0.12, 0.12, 0.15, 1)
    bg.inputs['Strength'].default_value = 0.5

    for name, colour in PALETTE.items():
        if name in MATERIALS:
            mat = MATERIALS[name]
            mat.diffuse_color = (*colour, 1)
            bsdf = mat.node_tree.nodes['Principled BSDF']
            bsdf.inputs['Base Color'].default_value = (*colour, 1)
            if 'silver' in name:
                bsdf.inputs['Metallic'].default_value = .55
                bsdf.inputs['Roughness'].default_value = .35
            elif 'number' in name or 'black' in name:
                bsdf.inputs['Metallic'].default_value = .3
                bsdf.inputs['Roughness'].default_value = .6

    try:
        bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.02))
        plane = bpy.context.object
        plane.name = 'Preview floor'
        helpers.append(plane)
        fm = bpy.data.materials.new('Preview floor mat')
        fm.use_nodes = True
        fm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.10, .10, .12, 1)
        fm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .9
        plane.data.materials.append(fm)

        light = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN'))
        scene.collection.objects.link(light)
        helpers.append(light)
        light.data.energy, light.data.angle = 2.0, math.radians(15)
        light.rotation_euler = (math.radians(30), math.radians(-15), math.radians(-20))

        cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
        scene.collection.objects.link(cam)
        helpers.append(cam)
        cam.data.type, cam.data.ortho_scale = 'ORTHO', 44
        cam.location = (0, 0, 50)
        cam.rotation_euler = (0, 0, 0)
        scene.camera = cam

        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        scene.render.filepath = str(PREVIEW)
        bpy.ops.render.render(write_still=True)
        print(f'PREVIEW → {PREVIEW}', flush=True)
    finally:
        for mat in MATERIALS.values():
            mat.diffuse_color = (.5, .5, .5, 1)
            bsdf = mat.node_tree.nodes['Principled BSDF']
            bsdf.inputs['Base Color'].default_value = (.5, .5, .5, 1)
            bsdf.inputs['Metallic'].default_value = 0
        scene.camera = None
        for h in helpers:
            bpy.data.objects.remove(h, do_unlink=True)
    assert len(scene.objects) == 1 and scene.objects[0] == obj


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    setup_materials()
    build()
    obj = finish()
    export(obj)
    preview(obj)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
