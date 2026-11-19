# 챕터 2 보스 경기장: 바닥 타일을 드러내는 거대 시계 부품을 만들고 FBX와 미리보기를 저장합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b -P Art/tools/blender_kit_ch2_arena.py
"""Revision 3: open clock, metres, +Y = twelve, bottom-centred origin.

Closed components are joined as in the landmark kit; there is no face/deck.
Role shaders stay grey in the FBX. Preview colours/floor are render-only.
"""
import math
from array import array
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch2/Kit_Arena.fbx'
PREVIEW = ROOT / 'Art/tools/specs/kit_ch2_arena_preview.png'
ROLES = set('stone stone_dark iron iron_dark rail_steel brass wood wood_dark rust'.split())
MATERIALS, PARTS = {}, []
TAU = math.tau


def setup_materials():
    for name in sorted(ROLES):
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .85
        shader.inputs['Metallic'].default_value = 0
        MATERIALS[name] = mat


def mesh(label, verts, faces, role, bevel=False):
    assert role in ROLES
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATERIALS[role])
    if bevel:
        # Landmark-kit 25 mm / two segment bevel, exposed top perimeter only.
        mod = obj.modifiers.new('Chunky softened edges 25mm', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.affect = 'EDGES'
        mod.use_clamp_overlap = True
        mod.limit_method = 'WEIGHT'
        weights = data.attributes.new('bevel_weight_edge', 'FLOAT', 'EDGE')
        top = max(v.co.z for v in data.vertices)
        for edge in data.edges:
            if all(abs(data.vertices[i].co.z - top) < 1e-6 for i in edge.vertices):
                weights.data[edge.index].value = 1
    PARTS.append(obj)
    return obj


def polar(radius, angle):
    return radius * math.cos(angle), radius * math.sin(angle)


def slab(label, outline, bottom, top, role, bevel=False):
    n = len(outline)
    verts = [(x, y, z) for z in (bottom, top) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return mesh(label, verts, faces, role, bevel)


def transform(obj, x=0, y=0, angle=0):
    obj.data.transform(Matrix.Translation((x, y, 0)) @ Matrix.Rotation(angle, 4, 'Z'))
    return obj


def box(label, x, y, sx, sy, bottom, top, role, angle=0, bevel=False):
    return transform(slab(label, [(-sx / 2, -sy / 2), (sx / 2, -sy / 2),
                                 (sx / 2, sy / 2), (-sx / 2, sy / 2)],
                          bottom, top, role, bevel), x, y, angle)


def disc(label, radius, bottom, top, role, n=24, bevel=False):
    return slab(label, [polar(radius, j * TAU / n) for j in range(n)],
                bottom, top, role, bevel)


def ring(label, inner, outer, bottom, top, role, n=96):
    verts = [(r * math.cos(j * TAU / n), r * math.sin(j * TAU / n), z)
             for r, z in ((inner, bottom), (outer, bottom), (outer, top), (inner, top))
             for j in range(n)]
    faces = [(k * n + j, k * n + (j + 1) % n,
              ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j)
             for k in range(4) for j in range(n)]
    return mesh(label, verts, faces, role)


def boolean(obj, cutter, operation='DIFFERENCE'):
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Real recessed clock hardware', 'BOOLEAN')
    mod.operation, mod.solver, mod.object = operation, 'EXACT', cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)


def discard(obj):
    PARTS.remove(obj)
    bpy.data.objects.remove(obj, do_unlink=True)


def coping():
    # 2026-11-18 사용자: 테두리 슬롯 부분은 갈색 계열로, 시각 자리 동그라미는 뺌 → 막대 눈금
    blocks = []
    for j in range(24):
        a, b = j * TAU / 24 - TAU / 48, j * TAU / 24 + TAU / 48
        arcs = []
        for radius in (16, 15):
            gap = math.asin(.025 / (2 * radius))
            arcs.append([polar(radius, a + gap + (b - a - 2 * gap) * k / 4)
                         for k in range(5)])
        # Cardinal points are block centres, guaranteeing exactly 32 m.
        blocks.append(slab('Brown coping block', arcs[0] + arcs[1][::-1],
                           0, .25, 'wood' if j % 2 == 0 else 'rust'))
    ring('Thin dark inner vertical lip', 14.97, 15.005, .015, .18, 'wood_dark')
    # 시각 눈금: 테두리 윗면에 반지름 방향으로 누운 막대 (12 · 3 · 6 · 9 = 길고 굵은 놋쇠, 나머지 = 짙은 갈색)
    for hour in range(12):
        a = math.pi / 2 - hour * TAU / 12
        big = hour % 3 == 0
        x, y = polar(15.5, a)
        box('Hour bar', x, y, .9 if big else .7, .34 if big else .2,
            .25, .275, 'brass' if big else 'wood_dark', a)
    # 분 눈금: 작은 짙은 갈색 막대
    for hour in range(12):
        for minute in range(1, 5):
            a = math.pi / 2 - (hour + minute / 5) * TAU / 12
            x, y = polar(15.5, a)
            box('Minute tick', x, y, .3, .08, .25, .265, 'wood_dark', a)


def rails():
    # Four radial arms; the hub interrupts both orthogonal track pairs.
    end = math.sqrt(14.4 ** 2 - .81 ** 2)
    for angle in (0, math.pi / 2, math.pi, 3 * math.pi / 2):
        for offset in (-.75, .75):
            rail = box('Charge path rail', (1.6 + end) / 2, offset,
                       end - 1.6, .12, 0, .05, 'rail_steel')
            transform(rail, angle=angle)
        for j in range(11):
            plate = box('Track tie plate', 1.9 + j * 1.2, 0,
                        .22, 2.2, 0, .02, 'iron_dark')
            transform(plate, angle=angle)


def check_hand_clearance(obj):
    """Clip the footprint against all eight rail rectangles; no area overlaps."""
    poly = [(v.co.x, v.co.y) for v in obj.data.vertices[:len(obj.data.vertices) // 2]]
    end = math.sqrt(14.4 ** 2 - .81 ** 2)
    for axis in (0, 1):
        for sign in (-1, 1):
            for offset in (-.75, .75):
                bounds = [(min(sign * 1.6, sign * end), max(sign * 1.6, sign * end)),
                          (offset - .06, offset + .06)]
                if axis:
                    bounds.reverse()
                clipped = poly[:]
                for dimension in (0, 1):
                    for cut, keep_greater in ((bounds[dimension][0], True),
                                               (bounds[dimension][1], False)):
                        result = []
                        for a, b in zip(clipped, clipped[1:] + clipped[:1]):
                            ia = a[dimension] >= cut if keep_greater else a[dimension] <= cut
                            ib = b[dimension] >= cut if keep_greater else b[dimension] <= cut
                            if ia:
                                result.append(a)
                            if ia != ib:
                                t = (cut - a[dimension]) / (b[dimension] - a[dimension])
                                result.append(tuple(a[k] + t * (b[k] - a[k]) for k in (0, 1)))
                        clipped = result
                assert len(clipped) < 3, (obj.name, 'hand overlaps charge rail')


def hand(label, angle, length, root_width, moon_at, moon_r):
    # 2026-11-18 사용자: 바늘을 더 고급스럽게 → 브레게식 바늘.
    # 금 테두리(놋쇠 바탕 판) 위에 검은 날, 끝 가까이 속이 빈 놋쇠 달(구멍으로 타일이 보임) → 가는 놋쇠 창끝,
    # 날 위 작은 놋쇠 마름모 상감 2개. 허브 옆 목은 좁게 둬서 선로와 겹치지 않음.
    blade_end = moon_at - moon_r * .78
    w0, w1 = root_width / 2, .17
    blade = [(1.15, -.08), (2.1, -.08), (2.75, -w0), (blade_end, -w1),
             (blade_end, w1), (2.75, w0), (2.1, .08), (1.15, .08)]
    rim = .11
    gilt = [(1.15, -.08), (2.1, -.08), (2.75, -w0 - rim), (blade_end + .05, -w1 - rim),
            (blade_end + .05, w1 + rim), (2.75, w0 + rim), (2.1, .08), (1.15, .08)]
    base = transform(slab(label + ' gilt border', gilt, .02, .05, 'brass', bevel=True), angle=angle)
    body = transform(slab(label + ' black blade', blade, .05, .07, 'iron_dark', bevel=True), angle=angle)
    for obj in (base, body):
        check_hand_clearance(obj)
    cx, cy = polar(moon_at, angle)
    transform(ring(label + ' hollow moon', moon_r * .62, moon_r, .02, .07, 'brass', n=32), cx, cy)
    lance = [(moon_at + moon_r * .9, -.22), (moon_at + moon_r * 1.3, -.3), (length, 0),
             (moon_at + moon_r * 1.3, .3), (moon_at + moon_r * .9, .22)]
    transform(slab(label + ' lance tip', lance, .02, .07, 'brass', bevel=True), angle=angle)
    for k in (.42, .68):
        r = 2.75 + (blade_end - 2.75) * k
        half = w0 + (w1 - w0) * k
        d = half * .55
        transform(slab(label + ' brass inlay', [(r - d * 1.6, 0), (r, -d), (r + d * 1.6, 0), (r, d)],
                       .07, .08, 'brass'), angle=angle)


def build():
    coping()
    ring('Brass chapter ring', 14.4, 14.6, 0, .03, 'brass')
    rails()
    hand('Ten oclock hour hand', math.radians(150), 9.0, 1.04, 7.3, .85)
    hand('Two oclock minute hand', math.radians(30), 13.4, .8, 11.4, .66)
    teeth = []
    for j in range(12):
        for fraction, radius in ((0, 1.5), (.20, 1.65), (.60, 1.65), (.80, 1.5)):
            teeth.append(polar(radius, (j + fraction) * TAU / 12))
    slab('Brass hub with twelve low gear teeth', teeth, 0, .08, 'brass', bevel=True)
    ring('Dark hub groove', 1.0, 1.12, .08, .095, 'iron_dark', n=48)
    disc('Raised brass boss', .9, .08, .14, 'brass', n=32, bevel=True)
    disc('Dark central pin', .42, .14, .24, 'iron_dark', n=24, bevel=True)
    disc('Brass pin cap', .18, .24, .28, 'brass', n=16, bevel=True)


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
        check = bmesh.new()
        check.from_mesh(obj.data)
        tiny = [f for f in check.faces if f.calc_area() <= 1e-10]
        if tiny:
            print('CLEANUP', obj.name, 'zero-area boolean faces', len(tiny), flush=True)
            bmesh.ops.dissolve_degenerate(check, dist=1e-6, edges=list(check.edges))
            check.to_mesh(obj.data)
        check.free()
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = 'Arena', 'Arena_Mesh'
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), 'Open mesh component'
    assert all(f.calc_area() > 1e-10 for f in bm.faces), 'Degenerate face'
    bmesh.ops.triangulate(bm, faces=list(bm.faces), ngon_method='BEAUTY')
    assert all(e.is_manifold for e in bm.edges), 'Non-manifold triangulation'
    assert all(f.calc_area() > 1e-10 for f in bm.faces), 'Degenerate triangle'
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.location, obj.rotation_euler, obj.scale = (0, 0, 0), (0, 0, 0), (1, 1, 1)
    bpy.context.view_layer.update()
    assert triangles(obj) <= 9000, triangles(obj)
    assert all(abs(obj.dimensions[i] - 32) < .001 for i in range(2)), tuple(obj.dimensions)
    assert abs(min(v.co.z for v in obj.data.vertices)) < 1e-6
    assert all(v.co.z >= -1e-6 and all(math.isfinite(c) for c in v.co) for v in obj.data.vertices)
    assert set(m.name for m in obj.data.materials) <= ROLES
    hit, *_ = obj.ray_cast(Vector((7, -7, 2)), Vector((0, 0, -1)))
    assert not hit, 'The room tiles must remain visible'
    obj['dimensions_m'] = [32, 32, round(obj.dimensions[2], 3)]
    obj['front'], obj['origin_rule'] = '+Y', 'bottom center'
    obj['chapter'], obj['solid_obstacle'] = 'The Stopped Station', False
    obj['triangle_count'], obj['rail_gauge_m'] = triangles(obj), 1.5
    obj['design_revision'] = '3 / 2026-11-18 / open floor clock'
    obj['hour_bar_count'], obj['minute_tick_count'] = 12, 48
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
    print(f'EXPORTED Arena {triangles(obj)} {tuple(round(d, 4) for d in obj.dimensions)}', flush=True)


def preview(obj):
    palette = {
        'stone': (.43, .405, .36), 'stone_dark': (.20, .195, .18),
        'iron': (.16, .18, .185), 'iron_dark': (.045, .055, .060),
        'rail_steel': (.37, .45, .49), 'brass': (.64, .405, .12),
        'wood': (.42, .26, .14), 'wood_dark': (.2, .12, .07), 'rust': (.5, .23, .1),
    }
    helpers = []
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples, scene.cycles.use_denoising = 24, True
    scene.render.resolution_x = scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    scene.world = bpy.data.worlds.new('Preview world')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.32, .32, .32, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
    for name, colour in palette.items():
        MATERIALS[name].diffuse_color = (*colour, 1)
        MATERIALS[name].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*colour, 1)
    try:
        bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.003))
        plane = bpy.context.object
        plane.name = 'Render only warm grey beige room tiles'
        helpers.append(plane)
        floor = bpy.data.materials.new('Preview floor only')
        floor.use_nodes = True
        floor.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.62, .56, .48, 1)
        floor.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .9
        plane.data.materials.append(floor)
        light = bpy.data.objects.new('Preview sun', bpy.data.lights.new('Preview sun', 'SUN'))
        scene.collection.objects.link(light)
        helpers.append(light)
        light.data.energy, light.data.angle = 1.6, math.radians(15)
        light.rotation_euler = (math.radians(25), math.radians(-20), math.radians(-25))
        camera = bpy.data.objects.new('Preview camera', bpy.data.cameras.new('Preview camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        camera.data.type, camera.data.ortho_scale = 'ORTHO', 36
        scene.camera = camera
        pixels = array('f', [0]) * (1800 * 900 * 4)
        for index, elevation in enumerate((90, 55)):
            a = math.radians(elevation)
            camera.location = (0, -60 * math.cos(a), 60 * math.sin(a))
            camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
            temp = PREVIEW.with_name(f'.arena_preview_{index}.png')
            scene.render.filepath = str(temp)
            bpy.ops.render.render(write_still=True)
            im = bpy.data.images.load(str(temp), check_existing=False)
            src = array('f', [0]) * (900 * 900 * 4)
            im.pixels.foreach_get(src)
            for row in range(900):
                start = (row * 1800 + index * 900) * 4
                pixels[start:start + 3600] = src[row * 3600:(row + 1) * 3600]
            bpy.data.images.remove(im)
            temp.unlink()
        sheet = bpy.data.images.new('Arena top and 55 degree preview', width=1800, height=900)
        sheet.pixels.foreach_set(pixels)
        sheet.filepath_raw, sheet.file_format = str(PREVIEW), 'PNG'
        sheet.save()
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        for mat in MATERIALS.values():
            mat.diffuse_color = (.5, .5, .5, 1)
            mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.5, .5, .5, 1)
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper, do_unlink=True)
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
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview(obj)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
