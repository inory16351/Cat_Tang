# 1장 보스 달리오스 '시간 지연 구역' 돔 (2026-11-23 사용자: 시간 지연인 걸 알 수 있게 구역에 돔을 씌우고 그 안에서 폭발 — 히오스 제라툴 궁처럼)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_timedome.py
# 결과: Game/Assets/Resources/FX/TimeDome/FX_TimeDome.fbx — 반지름 1 · 높이 0.75 (Unity에서 구역 반지름만큼 키움)
#   - Shell: 살짝 눌린 반구 껍질 (안팎 두 겹 대신 한 겹, 셰이더가 림 · 띠를 그림). UV: U = 둘레 0~1, V = 바닥 0 ~ 꼭대기 1
#   - Cage: 껍질 바로 바깥의 천구의 뼈대 — 자오선 반원 6개 + 위도 고리 2개 (느리게 거꾸로 돎 = 시간이 느려짐)
#   - Dial: 바닥 둘레의 시계판 띠 + 눈금 12개 (3 · 6 · 9 · 12시는 길고 굵게) + 가운데로 향하는 큰 바늘 두 개
#   좌표: 블렌더 Z 위 → FBX 내보내기(axis_forward=-Z, axis_up=Y)로 Unity Y 위
import bpy, bmesh, math, os
from mathutils import Matrix, Vector

OUT = r"C:/Project/Cat_Tang/Game/Assets/Resources/FX/TimeDome"
TAU = math.tau
CAP_V = 0.6                   # 꼭대기 시계판 테두리 높이 (v). 셰이더 _CapV와 맞춤 (2026-11-23 사용자: 이 원을 시계처럼 써서 지속 시간 표시)
H = 0.75                      # 돔 높이 (반지름 1 대비, 눌린 반구 — 위에서 보는 카메라에서 바닥 원이 잘 보이게)

bpy.ops.wm.read_factory_settings(use_empty=True)


def dome_pt(u, v, r=1.0):
    """u: 둘레 0~1, v: 0 바닥 ~ 1 꼭대기"""
    phi = v * math.pi / 2
    a = u * TAU
    return (r * math.cos(phi) * math.cos(a), r * math.cos(phi) * math.sin(a), H * r * math.sin(phi))


def finish(bm, name, smooth=True):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def tube(bm, pts, radius, sides=8, closed=False):
    n = len(pts)
    rings = []
    for i, p in enumerate(pts):
        a = pts[(i + 1) % n] if closed or i + 1 < n else p
        b = pts[i - 1] if closed or i > 0 else p
        t = [a[k] - b[k] for k in range(3)]
        tl = math.sqrt(sum(c * c for c in t)) or 1
        t = [c / tl for c in t]
        up = (0, 0, 1) if abs(t[2]) < 0.9 else (1, 0, 0)
        n1 = (t[1] * up[2] - t[2] * up[1], t[2] * up[0] - t[0] * up[2], t[0] * up[1] - t[1] * up[0])
        l1 = math.sqrt(sum(c * c for c in n1)) or 1
        n1 = [c / l1 for c in n1]
        n2 = (t[1] * n1[2] - t[2] * n1[1], t[2] * n1[0] - t[0] * n1[2], t[0] * n1[1] - t[1] * n1[0])
        ring = []
        for s in range(sides):
            a2 = s / sides * TAU
            ring.append(bm.verts.new(tuple(p[k] + radius * (math.cos(a2) * n1[k] + math.sin(a2) * n2[k]) for k in range(3))))
        rings.append(ring)
    segs = n if closed else n - 1
    for i in range(segs):
        A, B = rings[i], rings[(i + 1) % n]
        for s in range(sides):
            bm.faces.new((A[s], A[(s + 1) % sides], B[(s + 1) % sides], B[s]))
    if not closed:
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])


def box(bm, center, size, yaw):
    c, s = math.cos(yaw), math.sin(yaw)
    vs = []
    for dz in (-1, 1):
        for dy in (-1, 1):
            for dx in (-1, 1):
                x, y, z = dx * size[0] / 2, dy * size[1] / 2, dz * size[2] / 2
                vs.append(bm.verts.new((center[0] + x * c - y * s, center[1] + x * s + y * c, center[2] + z)))
    for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        bm.faces.new([vs[i] for i in f])


# ---------- 껍질 ----------
NU, NV = 48, 14
bm = bmesh.new()
uvl = bm.loops.layers.uv.new("UVMap")
grid = [[bm.verts.new(dome_pt(i / NU, j / NV)) for j in range(NV + 1)] for i in range(NU + 1)]
for i in range(NU):
    for j in range(NV):
        q = (grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1])
        f = bm.faces.new(q)
        for loop, (ii, jj) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))):
            loop[uvl].uv = (ii / NU, jj / NV)
bmesh.ops.remove_doubles(bm, verts=[grid[NU][j] for j in range(NV + 1)] + [grid[0][j] for j in range(NV + 1)], dist=1e-5)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
shell = finish(bm, "Shell")
for p in shell.data.polygons:          # 바깥을 보게
    c = p.center
    if p.normal.dot(c) < 0:
        p.flip()

# ---------- 천구의 뼈대 ----------
bm = bmesh.new()
for k in range(6):
    u = k / 12                         # 반원 6개 = 꼭대기에서 12갈래
    pts = [dome_pt(u, j / 20, 1.03) for j in range(21)] + [dome_pt(u + 0.5, (20 - j) / 20, 1.03) for j in range(1, 21)]
    tube(bm, pts[:41], 0.012, sides=6)
for v, r in ((0.3, 0.016), (CAP_V, 0.022)):            # 위 고리 = 꼭대기 시계판의 테두리 (셰이더의 _CapV와 같은 높이)
    tube(bm, [dome_pt(i / 64, v, 1.03) for i in range(64)], r, sides=6, closed=True)
finish(bm, "Cage")

# ---------- 바닥 시계판 ----------
bm = bmesh.new()
tube(bm, [dome_pt(i / 96, 0, 1.0) for i in range(96)], 0.03, sides=8, closed=True)
tube(bm, [dome_pt(i / 96, 0, 0.88) for i in range(96)], 0.012, sides=6, closed=True)
for h in range(12):
    a = h / 12 * TAU
    big = h % 3 == 0
    ln = 0.16 if big else 0.09
    r = 0.97 - ln / 2
    box(bm, (r * math.cos(a), r * math.sin(a), 0.02), (ln, 0.05 if big else 0.03, 0.03), a)
finish(bm, "Dial", smooth=False)

bm = bmesh.new()                       # 바늘: 꼭대기 시계판 위 (2026-11-23 사용자: 바닥 바늘은 잘 안 보임 → 돔 위에)
# 돔 표면을 따라 꼭대기에서 12시(Unity +Z = 블렌더 u 0.75) 쪽 테두리까지 휜 바늘. Unity에서 Y축으로 돌려 남은 시간을 가리킴
hand = [dome_pt(0.75, 1 - (1 - CAP_V + 0.04) * j / 16, 1.045) for j in range(17)]
tube(bm, hand, 0.034, sides=8)
bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.075, radius2=0.075, depth=0.04,
                      matrix=Matrix.Translation(Vector((0, 0, H * 1.045 + 0.01))))        # 가운데 축 (꼭대기)
finish(bm, "Hands", smooth=False)

# 던지는 시계 구슬 (반지름 1 구 + 허리의 시계 띠 + 눈금 혹 4개) — 보스가 포물선으로 던져 떨어진 자리에 돔이 펼쳐짐
bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=1.0)
finish(bm, "Orb")
bm = bmesh.new()
tube(bm, [(1.04 * math.cos(i / 40 * TAU), 1.04 * math.sin(i / 40 * TAU), 0) for i in range(40)], 0.09, sides=8, closed=True)
for h in range(4):
    a = h / 4 * TAU
    box(bm, (1.08 * math.cos(a), 1.08 * math.sin(a), 0), (0.22, 0.16, 0.3), a)
finish(bm, "OrbBand", smooth=False)

os.makedirs(OUT, exist_ok=True)
for ob in bpy.context.scene.objects:
    ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "FX_TimeDome.fbx"), use_selection=True, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'},
                         use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
print("EXPORTED", [o.name for o in bpy.context.scene.objects])
print("DONE")
