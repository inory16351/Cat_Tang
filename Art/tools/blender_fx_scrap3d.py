# 4장 톱니 바닥 맞물림 불꽃용 3D 조각 (2026-11-19 사용자: 그림(플립북) 말고 3D로)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_scrap3d.py
# 결과 (Unity 파티클 메시 모드가 Resources에서 바로 읽음):
#   Game/Assets/Resources/Kit/Ch4/FX_Spark.fbx  — 불티 1개: 앞뒤가 뾰족한 가늘고 긴 6각 결정 (길이 1, 굵기 0.14, 긴 축 = 블렌더 +Y → 유니티 -Z)
#   Game/Assets/Resources/Kit/Ch4/FX_Scrap.fbx  — 고철 조각 4개: Scrap_Gear · Scrap_Nut · Scrap_Bolt · Scrap_Plate (지름 약 1)
# 크기는 파티클 크기(startSize)로 정함. 색은 Unity 재질 · 파티클 색이 정함 (모델은 무늬 없음)
import bpy, bmesh, math, os

OUT = r"C:/Project/Cat_Tang/Game/Assets/Resources/Kit/Ch4"
TAU = math.tau


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def obj_from_bm(name, bm):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = False
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def spark():
    """앞(+Y)이 길고 뒤가 짧은 뾰족한 6각 결정 = 날아가는 불티 (앞 0.62, 뒤 0.38)"""
    bm = bmesh.new()
    ring = [bm.verts.new((math.cos(TAU * k / 6) * 0.07, 0.0, math.sin(TAU * k / 6) * 0.07)) for k in range(6)]
    tip = bm.verts.new((0, 0.62, 0)); tail = bm.verts.new((0, -0.38, 0))
    for k in range(6):
        l = (k + 1) % 6
        bm.faces.new((ring[k], ring[l], tip)); bm.faces.new((ring[l], ring[k], tail))
    return obj_from_bm("FX_Spark", bm)


def prism(bm, pts, z0, z1):
    """XY 다각형을 Z로 세운 기둥 (위 · 아래 면 + 옆면)"""
    lo = [bm.verts.new((x, y, z0)) for x, y in pts]
    hi = [bm.verts.new((x, y, z1)) for x, y in pts]
    n = len(pts)
    bm.faces.new(lo[::-1]); bm.faces.new(hi)
    for k in range(n):
        l = (k + 1) % n
        bm.faces.new((lo[k], lo[l], hi[l], hi[k]))


def ring_prism(bm, outer, inner, z0, z1):
    """구멍 뚫린 기둥: 바깥 · 안 다각형 점 수가 같아야 함"""
    n = len(outer)
    ol = [bm.verts.new((x, y, z0)) for x, y in outer]; oh = [bm.verts.new((x, y, z1)) for x, y in outer]
    il = [bm.verts.new((x, y, z0)) for x, y in inner]; ih = [bm.verts.new((x, y, z1)) for x, y in inner]
    for k in range(n):
        l = (k + 1) % n
        bm.faces.new((ol[k], ol[l], oh[l], oh[k]))
        bm.faces.new((il[l], il[k], ih[k], ih[l]))
        bm.faces.new((oh[k], oh[l], ih[l], ih[k]))
        bm.faces.new((ol[l], ol[k], il[k], il[l]))


def gear():
    bm = bmesh.new()
    teeth = 7; n = teeth * 4
    outer, inner = [], []
    for k in range(n):
        a = TAU * k / n
        r = 0.5 if (k % 4) in (1, 2) else 0.38
        outer.append((math.cos(a) * r, math.sin(a) * r)); inner.append((math.cos(a) * 0.14, math.sin(a) * 0.14))
    ring_prism(bm, outer, inner, -0.07, 0.07)
    return obj_from_bm("Scrap_Gear", bm)


def nut():
    bm = bmesh.new()
    outer = [(math.cos(TAU * k / 6) * 0.42, math.sin(TAU * k / 6) * 0.42) for k in range(6)]
    inner = [(math.cos(TAU * k / 6) * 0.18, math.sin(TAU * k / 6) * 0.18) for k in range(6)]
    ring_prism(bm, outer, inner, -0.14, 0.14)
    return obj_from_bm("Scrap_Nut", bm)


def bolt():
    bm = bmesh.new()
    head = [(math.cos(TAU * k / 6) * 0.26, math.sin(TAU * k / 6) * 0.26) for k in range(6)]
    prism(bm, head, 0.28, 0.5)
    shaft = [(math.cos(TAU * k / 8) * 0.11, math.sin(TAU * k / 8) * 0.11) for k in range(8)]
    prism(bm, shaft, -0.5, 0.28)
    return obj_from_bm("Scrap_Bolt", bm)


def plate():
    """구부러진 쇳조각: 들쭉날쭉한 판을 가운데서 꺾음"""
    bm = bmesh.new()
    pts = [(-0.5, -0.22), (-0.1, -0.32), (0.3, -0.2), (0.5, 0.05), (0.2, 0.3), (-0.25, 0.26), (-0.48, 0.08)]
    lo = [bm.verts.new((x, y, abs(x) * 0.35 - 0.04)) for x, y in pts]
    hi = [bm.verts.new((x, y, abs(x) * 0.35 + 0.04)) for x, y in pts]
    n = len(pts)
    bm.faces.new(lo[::-1]); bm.faces.new(hi)
    for k in range(n):
        l = (k + 1) % n
        bm.faces.new((lo[k], lo[l], hi[l], hi[k]))
    return obj_from_bm("Scrap_Plate", bm)


def export(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'},
                             use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    print("EXPORTED", name, [o.name for o in objs])


os.makedirs(OUT, exist_ok=True)
clear()
export([spark()], "FX_Spark")
clear()
export([gear(), nut(), bolt(), plate()], "FX_Scrap")
print("DONE")
