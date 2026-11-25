# 망각 6단계 얼음 꽃 3D (2026-11-25 사용자: "얼음 효과 보스 땅 내려찍을 때 나오는 오브젝트처럼 블렌더로 저 모양으로 3D 구워서")
#   전엔 Codex 별 모양 꽃 그림(cx_ice_bloom)을 바닥에 눕혀 깔았음 → 같은 별 모양을 땅에서 솟는 얼음 수정 무더기로 (땅 솟음 Upheaval과 같은 방식)
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_icebloom.py
# 결과: Game/Assets/Resources/FX/IceBloom/fx_icebloom_{0,1,2}.fbx — 반지름 1m 기준, 가운데 지름 0.5는 비워 둠(갇힌 적이 서는 자리)
#   재질 칸 3개 (유니티에서 코드로 칠함): Ice = 수정 옆면, IceTip = 끝 · 윗면(밝게), Frost = 바닥 별 모양 서리판(빛남)
#   2026-11-25 사용자: "갑자기 생성 말고 얼음꽃 피어나듯이 얼어버리는 느낌으로 아래부터" → 수정마다 따로 된 오브젝트(원점 = 밑동)로 내보냄.
#     이름 = c_<지연ms>_<번호> (안쪽 → 중간 → 바깥 차례, 빙글 도는 순서) · frost = 바닥 서리판. Unity가 이름의 지연대로 밑동에서부터 키움
#   바깥 10~12갈래 긴 수정 칼날이 거의 눕듯 벌어짐(별 꼭짓점) + 사이사이 중간 수정 + 안쪽에 곧게 선 수정이 몸을 감쌈 + 발치 작은 조각
import bpy, bmesh, math, random
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Game/Assets/Resources/FX/IceBloom"
PREVIEW = ROOT / "Art/tools/specs/blender_fx_icebloom_preview.png"
TAU = math.tau


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb):
    m = bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1); return m


def blade(bm, base, out, tilt, length, r, flat, rng, side_mat, tip_mat):
    """육각 수정 칼날: base에서 바깥(out) 쪽으로 tilt(수직에서 라디안)만큼 기울어 length만큼 뻗음. 가운데쯤 가장 굵고 끝이 뾰족, flat < 1 이면 납작"""
    up = Vector((out.x * math.sin(tilt), out.y * math.sin(tilt), math.cos(tilt))).normalized()
    side = Vector((-out.y, out.x, 0)).normalized()
    fwd = up.cross(side).normalized()
    sides = 6
    twist = rng.uniform(0, TAU)

    def ring(h, rr):
        pts = []
        for k in range(sides):
            a = TAU * k / sides + twist
            pts.append(bm.verts.new(base + up * h + side * math.cos(a) * rr * rng.uniform(0.88, 1.08) + fwd * math.sin(a) * rr * flat * rng.uniform(0.88, 1.08)))
        return pts
    rings = [ring(-0.08, r * 0.7), ring(length * 0.35, r), ring(length * 0.72, r * 0.72)]
    tip = bm.verts.new(base + up * length + side * rng.uniform(-0.04, 0.04))
    f = bm.faces.new(list(reversed(rings[0]))); f.material_index = side_mat
    for A, B in zip(rings, rings[1:]):
        for k in range(sides):
            l = (k + 1) % sides
            f = bm.faces.new((A[k], A[l], B[l], B[k])); f.material_index = side_mat
    for k in range(sides):
        f = bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tip))
        # 끝 면 중 위를 보는 면은 밝게 (빛 받는 결정면)
        f.material_index = tip_mat


def chunk(bm, center, rng, s, side_mat, tip_mat):
    verts = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=s)["verts"]
    for v in verts:
        v.co = Vector((v.co.x * rng.uniform(0.6, 1.3), v.co.y * rng.uniform(0.6, 1.3), v.co.z * rng.uniform(0.8, 1.6))) + center
    for f in {f for v in verts for f in v.link_faces}:
        f.material_index = tip_mat if f.normal.z > 0.6 else side_mat


def frost(bm, rng, n, a0, mat_i):
    """바닥 별 모양 서리판: 꼭짓점 n개 (긴 수정 칼날 방향과 맞춤), 가운데는 둥근 구멍 없이 채움"""
    c = bm.verts.new((0, 0, 0.015))
    ring = []
    for i in range(n * 2):
        a = a0 + i / (n * 2) * TAU
        r = rng.uniform(1.05, 1.18) if i % 2 == 0 else rng.uniform(0.42, 0.52)
        ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0.015)))
    for i in range(len(ring)):
        f = bm.faces.new((c, ring[i], ring[(i + 1) % len(ring)])); f.material_index = mat_i


def piece(root, name, mats, build, at):
    """build(bm)로 만든 조각을 at(밑동)을 원점으로 하는 오브젝트로"""
    me = bpy.data.meshes.new(name)
    for m in mats: me.materials.append(m)
    bm = bmesh.new()
    build(bm)
    bmesh.ops.translate(bm, verts=bm.verts, vec=-at)
    bm.normal_update(); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = at
    ob.parent = root
    return ob


def make(k):
    rng = random.Random(4100 + k * 53)
    root = bpy.data.objects.new(f"fx_icebloom_{k}", None)
    bpy.context.scene.collection.objects.link(root)
    mats = [mat(name, rgb) for name, rgb in (("Ice", (0.55, 0.78, 0.98)), ("IceTip", (0.9, 0.97, 1.0)), ("Frost", (0.6, 0.9, 1.0)))]
    n = rng.choice((10, 11, 12))
    a0 = rng.uniform(0, TAU)
    piece(root, "frost", mats, lambda bm: frost(bm, rng, n, a0, 2), Vector((0, 0, 0)))
    idx = [0]

    def add(delay, build, at):
        piece(root, f"c_{int(delay * 1000):04d}_{idx[0]:02d}", mats, build, at); idx[0] += 1

    def frac(a):
        return ((a - a0) % TAU) / TAU          # 빙글 도는 차례 (0~1)
    # 가운데 심 (2026-11-25 사용자: "가운데까지 꽉 차게, 비워두지 말고"): 굵은 기둥 하나 + 둘레 곧은 수정 6~7개가 몸을 통째로 덮음 — 맨 먼저 (0 ~ 0.08초)
    up0 = Vector((math.cos(a0), math.sin(a0), 0))
    add(0.0, lambda bm: blade(bm, Vector((0, 0, 0)), up0, math.radians(rng.uniform(0, 5)), rng.uniform(1.0, 1.12), 0.15, 0.9, rng, 0, 1), Vector((0, 0, 0)))
    mc = rng.randint(6, 7)
    for i in range(mc):
        a = a0 + i / mc * TAU + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0)); base = out * rng.uniform(0.07, 0.15)
        args = (out, math.radians(rng.uniform(6, 16)), rng.uniform(0.78, 0.98), rng.uniform(0.1, 0.125), 0.85)
        add(0.02 + frac(a) * 0.06, lambda bm, base=base, args=args: blade(bm, base, *args, rng, 0, 1), base)
    # 안쪽: 곧게 선 수정 6~7개가 몸을 감쌈 (12~26°) — 다음 (0.05 ~ 0.17초)
    m = rng.randint(6, 7)
    for i in range(m):
        a = a0 + (i + 0.25) / m * TAU + rng.uniform(-0.15, 0.15)
        out = Vector((math.cos(a), math.sin(a), 0)); base = out * 0.26
        args = (out, math.radians(rng.uniform(12, 26)), rng.uniform(0.62, 0.85), rng.uniform(0.07, 0.095), 0.8)
        add(0.05 + frac(a) * 0.12, lambda bm, base=base, args=args: blade(bm, base, *args, rng, 0, 1), base)
    # 사이사이 중간 수정 (45~55°) — 다음 (0.1 ~ 0.24초)
    for i in range(n):
        a = a0 + (i + 0.5) / n * TAU + rng.uniform(-0.08, 0.08)
        out = Vector((math.cos(a), math.sin(a), 0)); base = out * 0.3
        args = (out, math.radians(rng.uniform(42, 55)), rng.uniform(0.48, 0.62), rng.uniform(0.08, 0.1), 0.7)
        add(0.1 + frac(a) * 0.14, lambda bm, base=base, args=args: blade(bm, base, *args, rng, 0, 1), base)
    # 바깥 별 꼭짓점: 긴 칼날, 거의 눕듯 (62~74°) — 마지막으로 활짝 (0.2 ~ 0.38초)
    for i in range(n):
        a = a0 + i / n * TAU + rng.uniform(-0.05, 0.05)
        out = Vector((math.cos(a), math.sin(a), 0)); base = out * 0.28
        args = (out, math.radians(rng.uniform(62, 74)), rng.uniform(0.82, 1.0), rng.uniform(0.085, 0.11), 0.55)
        add(0.2 + frac(a) * 0.18, lambda bm, base=base, args=args: blade(bm, base, *args, rng, 0, 1), base)
    # 발치 작은 얼음 조각 — 바깥 칼날과 함께
    for i in range(rng.randint(6, 8)):
        a = rng.uniform(0, TAU); r = rng.uniform(0.55, 1.0)
        c = Vector((math.cos(a) * r, math.sin(a) * r, 0.0))
        s_ = rng.uniform(0.05, 0.09)
        add(0.26 + frac(a) * 0.14, lambda bm, c=c, s_=s_: chunk(bm, c + Vector((0, 0, 0.03)), rng, s_, 0, 1), c)
    return root


def export(ob):
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    for c in ob.children: c.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT / (ob.name + ".fbx")), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
        object_types={"MESH", "EMPTY"}, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    print("EXPORTED", ob.name, len(ob.children), "pieces", flush=True)


def preview(objs):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 1500, 560
    sc.render.filepath = str(PREVIEW)
    for i, o in enumerate(objs): o.location.x = (i - 1) * 2.7
    cam = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = (0, -6.2, 5.0); co.rotation_euler = (math.radians(52), 0, 0)
    bpy.ops.render.render(write_still=True)
    for o in objs: o.location.x = 0


reset()
objs = [make(k) for k in range(3)]
for o in objs: export(o)
preview(objs)
