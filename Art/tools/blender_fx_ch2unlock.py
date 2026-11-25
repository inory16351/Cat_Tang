# 11:59 2장 해금 탄 · 총 이펙트 메시 (2026-11-25, Blender 5.x 백그라운드)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_ch2unlock.py
# 결과: Game/Assets/Art/FX/Meshes/fx_c2_*.fbx → Unity 메뉴 CatTang/FX 메시 가져오기 → Resources/FX/Meshes/fx_c2_*.asset
# 도형 도구 · 좌표 규칙은 blender_fx.py 그대로 (Unity 좌표로 설계: X 오른쪽 · Y 위 · Z 앞 → 내보낼 때 블렌더 좌표로)
# 쓰는 곳: Resources/FX/Prefabs/FX_LingerWraith … FX_SteamBlast (Editor/Ch2UnlockFxBuilder.cs 가 프리팹 조립)
import bpy, bmesh, math, random, os, sys
from mathutils import Vector, Matrix, noise

HERE = os.path.dirname(os.path.abspath(__file__))
_argv = sys.argv
sys.argv = [_argv[0], "--", "__none__"]          # blender_fx.py의 도형 도구만 빌려 옴 (기존 메시는 다시 굽지 않음)
NS = {"__file__": os.path.join(HERE, "blender_fx.py")}
exec(open(NS["__file__"], encoding="utf-8").read(), NS)
sys.argv = _argv
V, tube, spike, puffy, revolve, polar, build = NS["V"], NS["tube"], NS["spike"], NS["puffy"], NS["revolve"], NS["polar"], NS["build"]
TAU = math.tau


def rot_to_z(bm, verts=None):
    """revolve(축 +Y)로 만든 것을 축 +Z로 눕힘"""
    bmesh.ops.rotate(bm, verts=verts or bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, 'X'))


# ---------- 미련 원혼: 두건 쓴 유령 (원점 = 땅, +Y 높이 1). 얼굴 없음 — 실루엣과 흔들리는 자락으로만 ----------
def m_wraith(bm):
    N, S = 22, 18
    rings = []
    for i in range(N):
        s = i / (N - 1)
        # 단면 반지름: 넓은 자락 → 허리 → 어깨 → 목 → 두건 → 뾰족 끝
        prof = [(0.0, 0.30), (0.25, 0.21), (0.48, 0.2), (0.62, 0.17), (0.72, 0.12), (0.82, 0.15), (0.93, 0.1), (1.0, 0.0)]
        for (a0, r0), (a1, r1) in zip(prof, prof[1:]):
            if a0 <= s <= a1:
                k = (s - a0) / (a1 - a0)
                k = k * k * (3 - 2 * k)
                r = r0 + (r1 - r0) * k
                break
        # 몸이 살짝 S자로 휘고 두건은 앞(+Z)으로 숙임
        cx = 0.07 * math.sin(s * 3.4 + 0.6) * s
        cz = 0.06 * math.sin(s * 2.2) + (0.08 * (s - 0.7) / 0.3 if s > 0.7 else 0.0)
        ring = []
        for j in range(S):
            a = TAU * j / S
            rr = r * (1 + 0.08 * math.sin(a * 3 + s * 7))
            y = s
            if s < 0.3:                                    # 자락: 5갈래로 찢긴 끝단이 아래로 처짐
                lobe = max(0.0, math.sin(a * 5 + 0.4)) ** 2
                y -= (0.3 - s) / 0.3 * 0.0 + (1 - s / 0.3) * 0.12 * lobe
                rr *= 1 + (1 - s / 0.3) * 0.25 * lobe
            ring.append(bm.verts.new(V(cx + math.cos(a) * rr, y, cz + math.sin(a) * rr)))
        rings.append(ring)
    for A, B in zip(rings, rings[1:]):
        for j in range(S):
            l = (j + 1) % S
            bm.faces.new((A[j], A[l], B[l], B[j]))
    top = bm.verts.new(rings[-1][0].co.copy()); top.co.x = sum(v.co.x for v in rings[-1]) / S; top.co.z = sum(v.co.z for v in rings[-1]) / S; top.co.y = 1.04
    for j in range(S):
        bm.faces.new((rings[-1][j], rings[-1][(j + 1) % S], top))
    bot = bm.verts.new(V(0, 0.1, 0))
    for j in range(S):
        bm.faces.new((rings[0][(j + 1) % S], rings[0][j], bot))
    # 어깨에서 뒤로 흩날리는 소매 자락 두 가닥
    for sgn in (-1, 1):
        pts = [V(sgn * (0.12 + 0.2 * s), 0.62 - 0.25 * s + 0.08 * math.sin(s * 5), -0.05 - 0.22 * s) for s in [i / 11 for i in range(12)]]
        tube(bm, pts, lambda s: 0.07 * (1 - s) ** 1.2 + 0.006, sides=8)


# ---------- 환승역: 사슬 고리 · 끊어진 고리 · 시곗바늘 ----------
def _link(bm, gap=0.0):
    """경기장 모양 고리 (+Z로 길쭉, XZ 평면), 굵기 0.045. gap = 끊어진 비율"""
    pts = []
    L, R = 0.16, 0.13
    N = 48
    per = 2 * (2 * L) + TAU * R
    for i in range(N):
        d = per * i / N
        if d < 2 * L: p = V(R, 0, -L + d)
        elif d < 2 * L + math.pi * R: a = (d - 2 * L) / R; p = V(R * math.cos(a), 0, L + R * math.sin(a))
        elif d < 4 * L + math.pi * R: p = V(-R, 0, L - (d - 2 * L - math.pi * R))
        else: a = math.pi + (d - 4 * L - math.pi * R) / R; p = V(R * math.cos(a), 0, -L + R * math.sin(a))
        pts.append(p)
    if gap > 0:
        k = int(N * gap)
        pts = pts[k // 2: N - k // 2]
        tube(bm, pts, lambda s: 0.045, sides=8, closed=False, cap=True)
    else:
        tube(bm, pts, lambda s: 0.045, sides=8, closed=True)


def m_chainlink(bm): _link(bm)
def m_chainbroken(bm): _link(bm, gap=0.22)


def m_clockhand(bm):
    """장식 시곗바늘 (원점 = 축, +Z로 길이 1, 바닥에 누운 판 · 두께는 Y). 뿌리에 둥근 추, 끝은 창끝 + 뚫린 마름모"""
    out = [V(-0.03, -0.12), V(0.03, -0.12), V(0.028, 0.55), V(0.09, 0.66), V(0.03, 0.78), V(0.022, 0.84), V(0.0, 1.0),
           V(-0.022, 0.84), V(-0.03, 0.78), V(-0.09, 0.66), V(-0.028, 0.55)]
    puffy(bm, out, 0.03, center=V(0, 0.5), rings=1)
    puffy(bm, polar(lambda a: 0.075, n=16, a0=0.0), 0.035, center=V(0, 0))
    puffy(bm, [V(c * 0.085 + 0.0, s * 0.085 - 0.2) for c, s in [(math.cos(TAU * i / 12), math.sin(TAU * i / 12)) for i in range(12)]], 0.03, center=V(0, -0.2))
    # 판(XY, 두께 Z) → 바닥에 눕힘 (길이 +Z, 두께 Y)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, 'X'))


# ---------- 마지막 기차: 증기 탄두 · 증기 원뿔 ----------
def m_trainhead(bm):
    """굵은 증기 탄두 (+Z 앞, 길이 1: z -0.5 ~ 0.5, 반지름 약 0.26). 보일러 띠 2줄 + 비틀린 증기 홈이 앞으로 감김"""
    prof = [(0.0, -0.5), (0.15, -0.5), (0.2, -0.44), (0.22, -0.3), (0.27, -0.24), (0.27, -0.18), (0.23, -0.12), (0.25, -0.02),
            (0.29, 0.04), (0.29, 0.1), (0.25, 0.16), (0.23, 0.26), (0.19, 0.36), (0.12, 0.45), (0.05, 0.5), (0.0, 0.52)]
    revolve(bm, prof, 28, fn=lambda a, t: (1 + 0.07 * math.sin(a * 6 + t * 14) * (1 - abs(t - 0.5) * 1.2), 0.0))
    rot_to_z(bm)


def m_steamcone(bm):
    """증기 활 물결: 앞(+Z 0)에서 뒤로 벌어지는 얇은 원뿔 껍질 (길이 1, 뒤 반지름 0.5), 가장자리 물결"""
    outer = [(0.04, 0.0), (0.18, -0.2), (0.33, -0.55), (0.5, -1.0)]
    inner = [(0.44, -0.98), (0.28, -0.56), (0.14, -0.22), (0.02, -0.04)]
    revolve(bm, outer + inner, 32, fn=lambda a, t: (1 + 0.12 * math.sin(a * 7) * t, 0.06 * math.sin(a * 5) * t), closed=True)
    rot_to_z(bm)


# ---------- 연기 고리: 뭉게뭉게 혹이 붙은 도넛 (XY 평면 · 축 +Z, 반지름 0.5 · 굵기 약 0.13) ----------
def m_smokering(bm):
    A, B = 72, 12
    R0 = 0.5
    rings = []
    for a in range(A):
        u = TAU * a / A
        c = V(math.cos(u), math.sin(u))
        puffs = max(0.0, math.cos(u * 9 + 0.5)) ** 2 * 0.5 + max(0.0, math.cos(u * 5 + 2.0)) ** 3 * 0.3
        ring = []
        for b in range(B):
            w = TAU * b / B
            n = noise.noise(Vector((math.cos(u) * 2.5, math.sin(u) * 2.5, w * 0.6)))
            rr = 0.11 * (1 + puffs + 0.25 * n)
            ring.append(bm.verts.new(c * (R0 + math.cos(w) * rr) + V(0, 0, math.sin(w) * rr * 1.15)))
        rings.append(ring)
    for a in range(A):
        x, y = rings[a], rings[(a + 1) % A]
        for b in range(B):
            l = (b + 1) % B
            bm.faces.new((x[b], x[l], y[l], y[b]))


# ---------- 레일 위의 불꽃: 바닥 긁힌 자국 (원점에서 +Z로 길이 1, 바닥에 누운 얇은 칼날) ----------
def m_scrape(bm):
    out = []
    N = 14
    for i in range(N + 1):
        s = i / N
        w = 0.09 * math.sin(math.pi * min(1, s * 1.25) ** 0.7) * (1 - s) ** 0.4 + 0.004
        out.append(V(w * (1 + 0.25 * math.sin(s * 19)), s))
    for i in range(N, -1, -1):
        s = i / N
        w = 0.09 * math.sin(math.pi * min(1, s * 1.25) ** 0.7) * (1 - s) ** 0.4 + 0.004
        out.append(V(-w * (1 + 0.25 * math.cos(s * 23)), s))
    puffy(bm, out, 0.025, center=V(0, 0.35), rings=1)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, 'X'))


# ---------- 가스등 랜턴 (원점 = 바닥, +Y 높이 1): 놋쇠 틀 · 빛나는 유리 ----------
def m_lantern_frame(bm):
    revolve(bm, [(0.0, 0.0), (0.2, 0.0), (0.22, 0.03), (0.2, 0.08), (0.15, 0.1), (0.0, 0.1)], 16)                  # 받침
    revolve(bm, [(0.0, 0.6), (0.17, 0.6), (0.24, 0.66), (0.2, 0.7), (0.1, 0.8), (0.07, 0.83), (0.07, 0.9), (0.09, 0.92), (0.0, 0.93)], 16)  # 갓 · 굴뚝
    for k in range(4):
        a = TAU * k / 4 + TAU / 8
        c = V(math.cos(a) * 0.16, 0, math.sin(a) * 0.16)
        tube(bm, [c + V(0, 0.08, 0), c * 1.08 + V(0, 0.35, 0), c + V(0, 0.62, 0)], 0.018, sides=6)
    # 손잡이 고리
    pts = [V(math.cos(math.pi * i / 16) * 0.14, 0.93 + math.sin(math.pi * i / 16) * 0.13, 0) for i in range(17)]
    tube(bm, pts, 0.016, sides=6)


def m_lantern_glass(bm):
    revolve(bm, [(0.0, 0.1), (0.1, 0.1), (0.15, 0.2), (0.165, 0.35), (0.15, 0.5), (0.11, 0.6), (0.0, 0.6)], 20)


# ---------- 울림 파동: 가는 물결 고리 (XY 평면 · 축 +Z, 반지름 0.5 · 굵기 0.016). 큰 반경에서도 띠가 두꺼워지지 않게 (fx_ring은 0.06)
#            둘레를 따라 위아래로 출렁이는 마루 12개 + 굵기가 숨 쉬듯 변함 → 소리 · 기적이 퍼지는 물결 ----------
def m_wave(bm):
    A, B = 96, 6
    rings = []
    for a in range(A):
        u = TAU * a / A
        c = V(math.cos(u), math.sin(u))
        rr = 0.016 * (1 + 0.5 * math.sin(u * 7 + 0.8) * math.sin(u * 3))
        lift = 0.02 * math.sin(u * 12)
        ring = []
        for b in range(B):
            w = TAU * b / B
            ring.append(bm.verts.new(c * (0.5 + math.cos(w) * rr) + V(0, 0, lift + math.sin(w) * rr * 1.6)))
        rings.append(ring)
    for a in range(A):
        x, y = rings[a], rings[(a + 1) % A]
        for b in range(B):
            l = (b + 1) % B
            bm.faces.new((x[b], x[l], y[l], y[b]))


SHAPES = [
    ("fx_c2_wave", m_wave),
    ("fx_c2_wraith", m_wraith), ("fx_c2_chainlink", m_chainlink), ("fx_c2_chainbroken", m_chainbroken), ("fx_c2_clockhand", m_clockhand),
    ("fx_c2_trainhead", m_trainhead), ("fx_c2_steamcone", m_steamcone), ("fx_c2_smokering", m_smokering), ("fx_c2_scrape", m_scrape),
    ("fx_c2_lantern_frame", m_lantern_frame), ("fx_c2_lantern_glass", m_lantern_glass),
]
ONLY = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for name, fn in SHAPES:
    if ONLY and name not in ONLY:
        continue
    build(name, fn)
print("DONE ch2unlock", len(SHAPES))
