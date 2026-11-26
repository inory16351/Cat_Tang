# 4장 쌍둥이 '뒤섞인 문자판' 바닥 숫자 1~12 (2026-11-26 사용자: 그림 배지가 바닥과 안 어울림 → 바닥에 새겨지는 3D 숫자)
# 보스 경기장 문자판(blender_kit_ch4_bossfloor.py)과 같은 결: 검은 Arial Black 숫자 + 얇은 진회색 테(바닥 문자판 선 색, 은빛은 상아 바닥에서 흐려 바꿈), 바닥에 거의 붙은 두께
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_floornum.py
# 결과: Game/Assets/Art/Characters/Ch4/Kit_FloorNum_{1..12}.fbx (지름 2.0m, 원점 = 바닥 가운데, 숫자 위쪽 = +Y(Blender) = 화면 위)
#  → Unity 메뉴 CatTang/4장 바닥 숫자만 만들기 → Resources/Kit/Ch4/FloorNum_{n}.prefab (FloorNumeral이 스스슥 새김)
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
TAU = math.tau
ROLES = {'gem_red': (1, .1, .12), 'red_lacquer': (.78, .06, .08), 'red_dark': (.42, .03, .05), 'iron_dark': (.08, .08, .09)}   # 2026-11-26 사용자: 빨강으로 (잘 보이게)
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_floornum_preview.png'
R_OUT, BAND, H = 1.0, .09, .035


def mats():
    out = {}
    for n in ROLES:
        m = bpy.data.materials.new(n)
        m.use_nodes = True
        m.diffuse_color = (.5, .5, .5, 1)
        m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.5, .5, .5, 1)
        out[n] = m
    return out


def ring(name, r0, r1, z0, z1, mat, n=64):
    bm = bmesh.new()
    def circ(r, z): return [bm.verts.new((r * math.cos(t * TAU / n), r * math.sin(t * TAU / n), z)) for t in range(n)]
    ot, ob, it, ib = circ(r1, z1), circ(r1, z0), circ(r0, z1), circ(r0, z0)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((ot[i], ot[j], it[j], it[i]))
        bm.faces.new((ob[j], ob[i], ib[i], ib[j]))
        bm.faces.new((ob[i], ob[j], ot[j], ot[i]))
        bm.faces.new((it[i], it[j], ib[j], ib[i]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob_ = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob_)
    me.materials.append(mat)
    return ob_


def bar(name, ang, r0, r1, w, z1, mat):
    """반지름 방향 눈금 막대 (r0 → r1, 폭 w, 높이 0 → z1)"""
    c, s_ = math.cos(ang), math.sin(ang)
    px, py = -s_ * w / 2, c * w / 2
    pts = [(r0 * c + px, r0 * s_ + py), (r1 * c + px, r1 * s_ + py), (r1 * c - px, r1 * s_ - py), (r0 * c - px, r0 * s_ - py)]
    return prism(name, pts, z1, mat)


def prism(name, pts, z1, mat):
    bm = bmesh.new()
    bot = [bm.verts.new((x, y, 0)) for x, y in pts]
    top = [bm.verts.new((x, y, z1)) for x, y in pts]
    bm.faces.new(top); bm.faces.new(list(reversed(bot)))
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    me.materials.append(mat)
    return o


def diamond(name, ang, r, size, z1, mat):
    c, s_ = math.cos(ang), math.sin(ang)
    cx, cy = r * c, r * s_
    # 반지름 방향으로 긴 마름모
    pts = [(cx + c * size, cy + s_ * size), (cx - s_ * size * .55, cy + c * size * .55),
           (cx - c * size, cy - s_ * size), (cx + s_ * size * .55, cy - c * size * .55)]
    return prism(name, pts, z1, mat)


def clock_detail(M):
    """2026-11-26 사용자: 시계 디테일 더 — 바깥 테 · 분 눈금 60 · 시 눈금 12 · 12/3/6/9 마름모 · 안쪽 선. 숫자 자리(반지름 .70 안)는 비움"""
    parts = [ring('Outer red rim', .90, 1.0, 0, H * .75, M['red_dark']),
             ring('Rim inner line', .865, .885, 0, H * .5, M['iron_dark']),
             ring('Inner track line', .715, .73, 0, H * .45, M['iron_dark'])]
    for k in range(60):
        a = math.pi / 2 - k * TAU / 60
        if k % 5 == 0:
            if k % 15 == 0:
                parts.append(diamond('Cardinal diamond', a, .795, .06, H * .8, M['red_lacquer']))
            else:
                parts.append(bar('Hour tick', a, .755, .855, .032, H * .6, M['red_dark']))
        else:
            parts.append(bar('Minute tick', a, .80, .85, .011, H * .45, M['iron_dark']))
    return parts


def digits(n, mat, font):
    bpy.ops.object.text_add(location=(0, 0, 0))
    t = bpy.context.object
    t.data.body = str(n)
    t.data.size = 1.25 if n < 10 else .9                                 # 2026-11-26 사용자: 숫자 더 크게 — 안쪽 선(반지름 .715) 안에 들어가게
    t.data.align_x, t.data.align_y = 'CENTER', 'CENTER'
    t.data.extrude = H / 2
    t.data.resolution_u = 2
    if font: t.data.font = font
    bpy.ops.object.convert(target='MESH')
    o = bpy.context.object
    o.data.materials.clear(); o.data.materials.append(mat)
    xs = [v.co.x for v in o.data.vertices]; ys = [v.co.y for v in o.data.vertices]; zs = [v.co.z for v in o.data.vertices]
    o.data.transform(Matrix.Translation((-(min(xs) + max(xs)) / 2, -(min(ys) + max(ys)) / 2, .004 - min(zs))))
    return o


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    M = mats()
    fp = Path('C:/Windows/Fonts/ariblk.ttf')
    font = bpy.data.fonts.load(str(fp)) if fp.exists() else None
    OUT.mkdir(parents=True, exist_ok=True)
    for n in range(1, 13):
        for o in list(bpy.data.objects): bpy.data.objects.remove(o)
        parts = clock_detail(M) + [digits(n, M['gem_red'], font)]
        bpy.ops.object.select_all(action='DESELECT')
        for o in parts: o.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.object.join()
        o = bpy.context.object
        o.name = o.data.name = f'FloorNum_{n}'
        for p in o.data.polygons: p.use_smooth = False
        dest = OUT / f'Kit_FloorNum_{n}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(dest), use_selection=True, axis_forward='-Z', axis_up='Y',
                                 apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'},
                                 mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
        print('EXPORTED', dest.name, len(o.data.polygons), tuple(round(d, 3) for d in o.dimensions), flush=True)
    preview(M)


def preview(M):
    """마지막(12)을 위에서 찍어 확인 그림 (상아 바닥 위)"""
    for name, m in M.items(): m.diffuse_color = (*ROLES[name], 1)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'FLAT'; sh.color_type = 'MATERIAL'; sh.show_shadows = False
    sh.background_type = 'WORLD'
    if sc.world is None: sc.world = bpy.data.worlds.new('w')
    sc.world.color = (.92, .91, .86)
    sc.view_settings.view_transform = 'Standard'
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    cam.data.type, cam.data.ortho_scale = 'ORTHO', 2.3
    cam.location = (0, 0, 5); sc.collection.objects.link(cam); sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = 700
    sc.render.filepath = str(PREVIEW)
    bpy.ops.render.render(write_still=True)


main()
