# Cat_Tang VFX 메시 생성 (Blender 5.x, 백그라운드 실행)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx.py
# 결과: Game/Assets/Art/FX/Meshes/fx_*.fbx → Unity 메뉴 CatTang/FX 메시 가져오기 → Resources/FX/Meshes/fx_*.asset (FxMeshes.Lib · 이펙트 프리팹이 씀)
#
# 좌표는 Unity 기준으로 설계 (X 오른쪽 · Y 위 · Z 앞). 내보내기 직전에 블렌더 좌표로 바꿈:
#   Unity (ux, uy, uz) → Blender (-ux, -uz, uy)   (FBX 내보내기 axis_forward=-Z · axis_up=Y · bake_space_transform 기준, axis_test로 확인)
# 규칙 (FxMesh와 같음):
#   - 납작한 모양(별 · 꽃 · 고리 · 눈 · ∞ 등)은 XY 평면에 놓이고 +Z를 봄, 반지름 약 0.5
#   - 방향 있는 모양(왕관 · 파편 · 불씨)은 원점에서 +Z로 뻗음, 길이 약 1
#   - 툰 불꽃 셰이더는 "카메라를 정면으로 보는 면 = 흰 심지, 비스듬할수록 탄 색 → 짙은 테"라서
#     납작한 모양도 가운데가 볼록한 '부푼' 단면으로 만들어 안쪽 → 바깥으로 색이 층져 보이게 함
import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix

OUT = r"C:/Project/Cat_Tang/Game/Assets/Art/FX/Meshes"   # 원본 FBX. Unity 메뉴 CatTang/FX 메시 가져오기 → Resources/FX/Meshes/*.asset
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

TAU = math.tau


def V(x, y, z=0.0):
    return Vector((x, y, z))


# ---------- 기본 도형 ----------

def puffy(bm, outline, t, center=V(0, 0), rings=2):
    """별 모양(가운데에서 보이는) 외곽선 → 앞뒤가 볼록한 부푼 판. t = 가운데 두께(반)"""
    n = len(outline)
    layers = []
    for k in range(rings + 1):
        f = k / rings                          # 0 = 바깥, 1 = 가운데
        z = t * math.sin(f * math.pi / 2)
        layer_f, layer_b = [], []
        for p in outline:
            q = center.lerp(p, 1 - f)
            layer_f.append(bm.verts.new((q.x, q.y, z)))
            layer_b.append(bm.verts.new((q.x, q.y, -z)) if k > 0 else layer_f[-1])
        layers.append((layer_f, layer_b))
    cf = bm.verts.new((center.x, center.y, t))
    cb = bm.verts.new((center.x, center.y, -t))
    for k in range(rings):
        for side in (0, 1):
            a, b = layers[k][side], layers[k + 1][side]
            for i in range(n):
                j = (i + 1) % n
                quad = (a[i], a[j], b[j], b[i]) if side == 0 else (a[j], a[i], b[i], b[j])
                if len(set(quad)) == 4:
                    bm.faces.new(quad)
    inner_f, inner_b = layers[rings]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((inner_f[i], inner_f[j], cf))
        bm.faces.new((inner_b[j], inner_b[i], cb))


def tube(bm, pts, radius, sides=10, closed=False, cap=True):
    """점 목록을 따라가는 관. radius = 숫자 또는 (0~1 진행도) → 반지름 함수"""
    n = len(pts)
    rf = radius if callable(radius) else (lambda s: radius)
    rings = []
    for i, p in enumerate(pts):
        prev = pts[i - 1] if (i > 0 or closed) else p
        nxt = pts[(i + 1) % n] if (i < n - 1 or closed) else p
        tan = (nxt - prev)
        tan = tan.normalized() if tan.length > 1e-6 else V(0, 0, 1)
        up = V(0, 0, 1) if abs(tan.z) < 0.9 else V(0, 1, 0)
        side = tan.cross(up).normalized()
        up2 = side.cross(tan).normalized()
        r = rf(i / max(1, n - 1))
        ring = []
        for k in range(sides):
            a = TAU * k / sides
            ring.append(bm.verts.new(p + (side * math.cos(a) + up2 * math.sin(a)) * r))
        rings.append(ring)
    segs = n if closed else n - 1
    for i in range(segs):
        a, b = rings[i], rings[(i + 1) % n]
        for k in range(sides):
            l = (k + 1) % sides
            bm.faces.new((a[k], a[l], b[l], b[k]))
    if cap and not closed:
        for ring, rev in ((rings[0], True), (rings[-1], False)):
            c = sum((v.co for v in ring), Vector()) / sides
            cv = bm.verts.new(c)
            for k in range(sides):
                l = (k + 1) % sides
                bm.faces.new((ring[l], ring[k], cv) if rev else (ring[k], ring[l], cv))


def spike(bm, base, tip, w, sides=4, twist=0.0):
    """뿌리 단면이 별(4~5갈래)인 뾰족한 가시"""
    d = (tip - base)
    L = d.length
    d.normalize()
    up = V(0, 0, 1) if abs(d.z) < 0.9 else V(0, 1, 0)
    s1 = d.cross(up).normalized()
    s2 = s1.cross(d).normalized()
    ring = []
    m = sides * 2
    for k in range(m):
        a = TAU * k / m + twist
        r = w if k % 2 == 0 else w * 0.45
        ring.append(bm.verts.new(base + d * (L * 0.12) + (s1 * math.cos(a) + s2 * math.sin(a)) * r))
    tv = bm.verts.new(tip)
    bv = bm.verts.new(base)
    for k in range(m):
        l = (k + 1) % m
        bm.faces.new((ring[k], ring[l], tv))
        bm.faces.new((ring[l], ring[k], bv))


def polar(fn, n=64, a0=0.0):
    return [V(math.cos(a0 + TAU * i / n) * fn(a0 + TAU * i / n), math.sin(a0 + TAU * i / n) * fn(a0 + TAU * i / n)) for i in range(n)]


# ---------- 모양들 ----------

def m_star(bm):
    """만화식 충격 별: 10갈래, 길이 · 각도가 제각각"""
    rnd = random.Random(7)
    pts = []
    n = 10
    for i in range(n * 2):
        a = TAU * i / (n * 2) + rnd.uniform(-0.08, 0.08)
        r = 0.5 * rnd.uniform(0.72, 1.08) if i % 2 == 0 else rnd.uniform(0.19, 0.25)
        pts.append(V(math.cos(a) * r, math.sin(a) * r))
    puffy(bm, pts, 0.11, rings=2)


def m_sparkle(bm):
    """4갈래 반짝임 (세로 · 가로가 길고 오목하게 파인 별)"""
    pts = []
    for i in range(32):
        a = TAU * i / 32
        c = abs(math.cos(a)) ** 9 + abs(math.sin(a)) ** 9          # 축 방향일수록 1
        r = 0.06 + 0.44 * c * (1.0 if abs(math.sin(a)) > abs(math.cos(a)) else 0.72)
        pts.append(V(math.cos(a) * r, math.sin(a) * r))
    puffy(bm, pts, 0.07, rings=2)


def m_crown(bm):
    """튀는 왕관: 뿌리 고리에서 바깥 · 앞(+Z)으로 벌어지는 가시 9개"""
    rnd = random.Random(11)
    n = 9
    for i in range(n):
        a = TAU * i / n + rnd.uniform(-0.15, 0.15)
        L = rnd.uniform(0.7, 1.0)
        spread = math.radians(rnd.uniform(30, 48))
        base = V(math.cos(a) * 0.1, math.sin(a) * 0.1, 0)
        tip = base + V(math.cos(a) * math.sin(spread), math.sin(a) * math.sin(spread), math.cos(spread)) * L
        spike(bm, base, tip, rnd.uniform(0.07, 0.1), sides=3, twist=a)


def m_shard(seed):
    def f(bm):
        rnd = random.Random(seed)
        ring = []
        k = rnd.choice([4, 5, 5, 6])
        zr = rnd.uniform(0.25, 0.45)
        for i in range(k):
            a = TAU * i / k + rnd.uniform(-0.3, 0.3)
            r = rnd.uniform(0.11, 0.19)
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r * 0.75, zr + rnd.uniform(-0.06, 0.06))))
        tip = bm.verts.new((rnd.uniform(-0.05, 0.05), rnd.uniform(-0.05, 0.05), 1.0))
        back = bm.verts.new((0, 0, 0))
        for i in range(k):
            j = (i + 1) % k
            bm.faces.new((ring[i], ring[j], tip))
            bm.faces.new((ring[j], ring[i], back))
    return f


def m_catring(bm):
    """회상: 고양이 머리 윤곽 고리 (둥근 고리 위에 귀 두 개)"""
    R = 0.4
    pts = []

    def circ(deg):
        a = math.radians(deg)
        return V(math.cos(a) * R, math.sin(a) * R)
    # 오른쪽 귀 시작(55°)에서 아래로 돌아 왼쪽 귀 끝(125°)까지
    for d in range(55, -235 - 1, -10):
        pts.append(circ(d))
    pts.append(circ(125))
    pts.append(V(-0.36, 0.62))      # 왼쪽 귀 끝
    pts.append(circ(102))
    pts.append(circ(90))
    pts.append(circ(78))
    pts.append(V(0.36, 0.62))       # 오른쪽 귀 끝
    for p in pts:
        p.y -= 0.08
    tube(bm, pts, 0.045, sides=8, closed=True)


def m_flower(bm):
    """추억: 다섯 꽃잎"""
    puffy(bm, polar(lambda a: 0.17 + 0.33 * abs(math.cos(2.5 * a)) ** 0.7, n=90, a0=math.pi / 2), 0.09, rings=3)


def m_spiral(bm):
    """되감기: 거꾸로 감기는 나선 + 끝의 화살촉"""
    pts = []
    N = 70
    turns = 1.75
    for i in range(N):
        s = i / (N - 1)
        a = -TAU * turns * s + math.pi / 2
        r = 0.42 - 0.32 * s
        pts.append(V(math.cos(a) * r, math.sin(a) * r, 0.03 * math.sin(s * math.pi)))
    tube(bm, pts, lambda s: 0.055 - 0.03 * s, sides=8)
    # 화살촉: 바깥 끝(시작점)에서 진행 반대쪽(뒤로 감는 방향)
    p0, p1 = pts[0], pts[1]
    d = (p0 - p1).normalized()
    spike(bm, p0 - d * 0.02, p0 + d * 0.2, 0.11, sides=3)


def m_crescent(bm):
    """종말: 초승달 낫날 (두툼한 가운데, 뾰족한 양끝)"""
    outer, inner = [], []
    for i in range(25):
        a = math.radians(-78 + 156 * i / 24)
        outer.append(V(math.cos(a) * 0.5, math.sin(a) * 0.5))
    # 안쪽 호: 왼쪽으로 치우친 원 (끝에서 바깥 호와 만남)
    cx, R2 = -0.16, None
    e = outer[0]
    R2 = (e - V(cx, 0)).length
    for i in range(23):
        b = math.atan2(outer[-1].y, outer[-1].x - cx) - (math.atan2(outer[-1].y, outer[-1].x - cx) * 2) * i / 22
        inner.append(V(cx + math.cos(b) * R2, math.sin(b) * R2))
    poly = outer + inner[1:-1]
    slab(bm, poly, 0.07)


def slab(bm, poly, t):
    """오목한 외곽선 → 두께 있는 판 (가장자리는 내보낼 때 베벨로 둥글림)"""
    fv = [bm.verts.new((p.x, p.y, t)) for p in poly]
    bv = [bm.verts.new((p.x, p.y, -t)) for p in poly]
    bm.faces.new(fv)
    bm.faces.new(list(reversed(bv)))
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((fv[j], fv[i], bv[i], bv[j]))


def m_clock(bm):
    """지연: 시곗바늘 둘 + 가운데 축"""
    def needle(angle_deg, L, w):
        a = math.radians(angle_deg)
        d = V(math.cos(a), math.sin(a))
        s = V(-d.y, d.x)
        pts = [d * (-0.08), s * w + d * (L * 0.18), d * L, -s * w + d * (L * 0.18)]
        puffy(bm, pts, 0.05, center=d * (L * 0.2), rings=1)
    needle(90, 0.5, 0.06)
    needle(-30, 0.33, 0.075)
    puffy(bm, polar(lambda a: 0.075, n=16), 0.07)


def m_eye(bm):
    """예지: 아몬드 눈 윤곽 + 홍채"""
    pts = []
    N = 40
    for i in range(N):
        x = -0.5 + i / N
        pts.append(V(x, 0.27 * math.sin(math.pi * (x + 0.5)) ** 0.85))
    for i in range(N):
        x = 0.5 - i / N
        pts.append(V(x, -0.27 * math.sin(math.pi * (x + 0.5)) ** 0.85))
    tube(bm, pts, 0.035, sides=8, closed=True)
    puffy(bm, polar(lambda a: 0.15, n=24), 0.08)


def m_infinity(bm):
    """영원: ∞ (교차점에서 앞뒤로 살짝 비켜 감)"""
    pts = []
    N = 96
    for i in range(N):
        t = TAU * i / N
        d = 1 + math.sin(t) ** 2
        pts.append(V(0.5 * math.cos(t) / d, 0.62 * math.sin(t) * math.cos(t) / d, 0.05 * math.sin(t)))
    tube(bm, pts, 0.05, sides=8, closed=True)


def m_wisp(bm):
    """미련: 꼬리 긴 불씨 (뿌리 통통 → 끝으로 가늘게 휘어짐, +Z로)"""
    pts = []
    N = 30
    for i in range(N):
        s = i / (N - 1)
        pts.append(V(0.12 * math.sin(s * 5.0) * s, 0.08 * math.sin(s * 3.2 + 1) * s, s))
    tube(bm, pts, lambda s: 0.16 * (1 - s) ** 1.4 + 0.004, sides=10)


def m_hand(bm):
    """되감기: 큰 시곗바늘 하나 (원점 = 축, +Y로 뻗음, 끝은 화살촉). 스핀으로 거꾸로 돌림"""
    puffy(bm, [V(-0.035, -0.12), V(0.035, -0.12), V(0.045, 0.3), V(0.11, 0.3), V(0.0, 0.5), V(-0.11, 0.3), V(-0.045, 0.3)], 0.05, center=V(0, 0.2), rings=1)
    puffy(bm, polar(lambda a: 0.07, n=16), 0.06)


def m_charge(bm):
    """지연: 달라붙는 충전 폭탄 (가운데 볼록한 마름모 + 네 갈래 집게)"""
    puffy(bm, [V(0, 0.2), V(0.2, 0), V(0, -0.2), V(-0.2, 0)], 0.1, rings=3)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        d = V(math.cos(a), math.sin(a))
        s = V(-d.y, d.x)
        puffy(bm, [d * 0.16 + s * 0.06, d * 0.42 + s * 0.03, d * 0.5, d * 0.42 - s * 0.03, d * 0.16 - s * 0.06], 0.05, center=d * 0.3, rings=1)


# ---------- 공통 조각 (기존 코드 메시 FxMeshes.Spike · Flame · Ball · Torus 대체, 규격 같음) ----------
from mathutils import noise


def loft(bm, K, U, radius_fn, cap_base=True, base_z=-0.02):
    """+Z로 뻗는 회전체: radius_fn(theta, t) → (r, dx, dy) (dx, dy = 축 휨). t=1 끝은 한 점으로 모임"""
    rings = []
    for u in range(U + 1):
        t = u / U
        ring = []
        for k in range(K):
            th = TAU * k / K
            r, dx, dy = radius_fn(th, t)
            ring.append(bm.verts.new((math.cos(th) * r + dx, math.sin(th) * r + dy, t)))
        rings.append(ring)
    for u in range(U):
        a, b = rings[u], rings[u + 1]
        for k in range(K):
            l = (k + 1) % K
            bm.faces.new((a[k], a[l], b[l], b[k]))
    if cap_base:
        bv = bm.verts.new((0, 0, base_z))
        for k in range(K):
            bm.faces.new((rings[0][(k + 1) % K], rings[0][k], bv))


def m_spike(seed):
    """창끝 가시: 날 4~5개가 살짝 비틀리며 끝으로 뾰족, 축이 조금 휨"""
    def f(bm):
        rnd = random.Random(seed)
        ridges = rnd.choice([4, 5])
        phase = rnd.uniform(0, TAU)
        twist = rnd.uniform(0.6, 1.4) * rnd.choice([-1, 1])
        bend = V(rnd.uniform(-0.08, 0.08), rnd.uniform(-0.08, 0.08))

        def rf(th, t):
            pa = (th + phase + twist * t) / TAU * ridges
            peak = 1 - abs(pa - math.floor(pa) - 0.5) * 2
            star = 0.6 + 0.4 * peak ** 2.2                  # 날이 더 섬
            prof = (0.55 + 0.45 * math.sin(min(1, t / 0.16) * math.pi / 2)) if t < 0.16 else (1 - (t - 0.16) / 0.84) ** 1.2
            n = noise.noise(Vector((math.cos(th) * 2, math.sin(th) * 2, t * 4 + seed)))
            r = 0.5 * prof * star * (1 + 0.07 * n)
            return r, bend.x * t * t, bend.y * t * t
        loft(bm, 40, 18, rf)
    return f


def m_flame(seed):
    """불꽃 원뿔: 좁은 뿌리 → 넓게 퍼지다 끝이 꽃잎처럼 갈라짐, 표면은 노이즈로 일렁임"""
    def f(bm):
        rnd = random.Random(seed)
        petals = rnd.choice([5, 6, 7])
        phase = rnd.uniform(0, TAU)
        lens = [rnd.uniform(0.6, 1.0) for _ in range(petals)]
        K, U = 48, 20
        rings = []
        for u in range(U + 1):
            t = u / U
            ring = []
            for k in range(K):
                th = TAU * k / K
                pa = (th + phase) / TAU * petals
                pi = int(math.floor(pa)) % petals
                peak = 1 - abs(pa - math.floor(pa) - 0.5) * 2
                sp = peak ** 2.2
                z_end = 0.62 + 0.38 * sp * lens[pi]
                wid = 0.7 + 0.45 * sp
                prof = math.sin(math.pi * t ** 0.62)
                n = noise.noise(Vector((math.cos(th) * 1.5, math.sin(th) * 1.5, t * 3 + seed * 0.37)))
                r = 0.5 * prof * wid * (1 + 0.12 * n) + 0.12 * sp * t * t * prof
                ring.append(bm.verts.new((math.cos(th) * r, math.sin(th) * r, t * z_end)))
            rings.append(ring)
        for u in range(U):
            a, b = rings[u], rings[u + 1]
            for k in range(K):
                l = (k + 1) % K
                bm.faces.new((a[k], a[l], b[l], b[k]))
        bv = bm.verts.new((0, 0, -0.02))
        for k in range(K):
            bm.faces.new((rings[0][(k + 1) % K], rings[0][k], bv))
    return f


def m_ball(seed):
    """만화 불덩이 · 연기 덩어리: 둥근 몸에 혹 4~6개가 부풀어 오른 구름, 지름 약 1"""
    def f(bm):
        rnd = random.Random(seed)
        bmesh.ops.create_icosphere(bm, subdivisions=4, radius=0.5)
        lobes = [(Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1))).normalized(), rnd.uniform(0.18, 0.32), rnd.uniform(0.35, 0.6))
                 for _ in range(rnd.randint(4, 6))]
        for v in bm.verts:
            d = v.co.normalized()
            bulge = 0.0
            for (c, h, w) in lobes:
                dot = max(0.0, d.dot(c))
                bulge = max(bulge, h * (max(0.0, (dot - (1 - w)) / w)) ** 1.5)
            n = noise.noise(d * 2.2 + Vector((seed, 0, 0)))
            v.co = d * 0.5 * (0.82 + bulge + 0.05 * n)
    return f


def m_ring(bm):
    """충격 고리: XY 평면의 도넛 (축 = +Z), 반지름 0.5 · 굵기 약 0.06, 굵기가 출렁이고 바깥으로 작은 가시 8개"""
    A, B = 64, 10
    R0, r0 = 0.5, 0.06
    rings = []
    for a in range(A):
        u = TAU * a / A
        rr = r0 * (1 + 0.45 * math.sin(u * 5) * math.sin(u * 3 + 1.3))
        c = V(math.cos(u), math.sin(u))
        ring = []
        for b in range(B):
            w = TAU * b / B
            p = c * (R0 + math.cos(w) * rr) + V(0, 0, math.sin(w) * rr)
            ring.append(bm.verts.new(p))
        rings.append(ring)
    for a in range(A):
        x, y = rings[a], rings[(a + 1) % A]
        for b in range(B):
            l = (b + 1) % B
            bm.faces.new((x[b], x[l], y[l], y[b]))
    rnd = random.Random(5)
    for i in range(8):
        u = TAU * i / 8 + rnd.uniform(-0.2, 0.2)
        c = V(math.cos(u), math.sin(u))
        spike(bm, c * (R0 + 0.02), c * (R0 + rnd.uniform(0.1, 0.18)), 0.035, sides=3)


# ---------- 만들기 · 내보내기 ----------

U2B = Matrix(((-1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))     # Unity → Blender


def build(name, fn, bevel=0.0, smooth=False):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    fn(bm)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.transform(U2B)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)          # 거울 변환(행렬식 -1)으로 뒤집힌 감김을 되돌림
    bm.to_mesh(me)
    bm.free()
    if smooth:                                           # 물 메시: 매끈한 법선 (반사 · 가장자리 빛이 물처럼 흐르게)
        for p_ in me.polygons:
            p_.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    if bevel > 0:
        md = ob.modifiers.new("bevel", 'BEVEL')
        md.width = bevel
        md.segments = 2
        md.limit_method = 'ANGLE'
    md = ob.modifiers.new("tri", 'TRIANGULATE')
    for o in bpy.context.scene.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
    print("EXPORTED", name, len(me.polygons))


# ---------- 3장 거울 세계 (2026-11-18 사용자: 이펙트는 그림 말고 블렌더 3D로) ----------
def m_glass(seed):
    """얇은 유리 파편: 비뚤어진 삼각 · 사각 판 (XY 평면, +Z를 봄, 반지름 약 0.5), 가운데가 살짝 부푼 단면"""
    def f(bm):
        rnd = random.Random(seed)
        k = rnd.choice([3, 3, 4])
        pts = []
        for i in range(k):
            a = TAU * i / k + rnd.uniform(-0.35, 0.35) + 0.4
            r = 0.5 * rnd.uniform(0.55, 1.0)
            pts.append(V(math.cos(a) * r, math.sin(a) * r * rnd.uniform(0.7, 1.2)))
        puffy(bm, pts, 0.045, rings=2)
    return f


def m_diamond(bm):
    """신기루 마름모 조각 (세로로 긴 마름모 판)"""
    puffy(bm, [V(0, 0.5), V(0.24, 0), V(0, -0.5), V(-0.24, 0)], 0.05, rings=2)


def m_glint(bm):
    """반사 빛살: 가로가 아주 긴 4갈래 빛 (가로 반지름 0.5, 세로 0.22), 팔이 가늘게 파임"""
    pts = []
    for i in range(48):
        a = TAU * i / 48
        c, sn = math.cos(a), math.sin(a)
        r = 0.035 + 0.465 * abs(c) ** 9 + 0.19 * abs(sn) ** 9
        pts.append(V(c * r, sn * r))
    puffy(bm, pts, 0.03, rings=2)



# ---------- 3장 분수 샘 물 폭발 (2026-11-18 사용자: 그림 말고 블렌더 3D로, 확실히 물처럼) ----------
# 매끈한 회전체 (Unity 좌표: Y 위, XZ 둘레). 셰이더 CatTang/Water가 물체 좌표(높이 · 둘레 각)로 물결 줄무늬를 흘려서 UV 없음.
def revolve(bm, prof, sides, fn=None, closed=False):
    """prof: [(r, y)] 단면. r == 0 이면 축 위 한 점(극). closed = 단면 고리(두께 있는 껍질).
    fn(a, t) -> (r 배율, y 더함): a = 둘레 각, t = 단면 위치 0~1"""
    rings = []
    n = len(prof)
    for i, (r, y) in enumerate(prof):
        t = i / max(1, n - 1)
        if r <= 1e-6:
            rings.append([bm.verts.new(V(0, y, 0))])
            continue
        ring = []
        for j in range(sides):
            a = TAU * j / sides
            k, dy = fn(a, t) if fn else (1.0, 0.0)
            ring.append(bm.verts.new(V(math.cos(a) * r * k, y + dy, math.sin(a) * r * k)))
        rings.append(ring)
    pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
    for A, B in pairs:
        if len(A) == 1 and len(B) == 1:
            continue
        if len(A) == 1:
            for j in range(sides):
                bm.faces.new((A[0], B[(j + 1) % sides], B[j]))
        elif len(B) == 1:
            for j in range(sides):
                bm.faces.new((A[j], A[(j + 1) % sides], B[0]))
        else:
            for j in range(sides):
                bm.faces.new((A[j], A[(j + 1) % sides], B[(j + 1) % sides], B[j]))


def m_water_column(bm):
    """물기둥: 밑지름 1 · 높이 1 (y 0~1), 허리가 살짝 잘록하고 위가 부푼 둥근 머리, 비틀린 물결 혹"""
    prof = [(0, 0.0), (0.5, 0.0)]
    for i in range(1, 20):
        y = i / 20
        prof.append((0.42 + 0.06 * math.cos(y * math.pi * 1.6) + 0.06 * y * y, y))
    prof += [(0.46, 1.0), (0.4, 1.06), (0.26, 1.11), (0, 1.13)]

    def fn(a, t):
        return 1 + 0.07 * math.sin(3 * a + t * 10) + 0.04 * math.sin(5 * a - t * 16), 0.0
    revolve(bm, prof, 32, fn)


def m_water_crown(bm):
    """버섯 물 왕관: 위로 둥글게 부풀었다 바깥 · 아래로 말려 떨어지는 물 막 (지름 약 1, 두께 있는 껍질), 가장자리가 8갈래로 처짐"""
    prof = [(0.03, 0.14), (0.2, 0.13), (0.34, 0.08), (0.44, -0.02), (0.5, -0.14), (0.52, -0.26), (0.5, -0.32),
            (0.46, -0.27), (0.43, -0.13), (0.36, -0.03), (0.22, 0.05), (0.03, 0.07)]

    def fn(a, t):
        lip = max(0.0, 1 - abs(t - 0.55) / 0.3)               # 가장자리일수록
        sc = 0.5 + 0.5 * math.cos(8 * a)
        return 1 + 0.1 * lip * sc, -0.14 * lip * sc + 0.03 * math.sin(3 * a)
    revolve(bm, prof, 48, fn, closed=True)


def m_water_drop(bm):
    """물방울: 둥근 머리(반지름 0.3, 가운데 y 0.3)에서 +Y로 가늘어지는 꼬리 (길이 1). 날아가는 반대쪽으로 꼬리를 둠"""
    prof = [(0, 0.0)]
    for i in range(1, 9):
        a = math.pi * i / 16                                  # 아래 반구
        prof.append((0.3 * math.sin(a), 0.3 - 0.3 * math.cos(a)))
    for i in range(1, 9):
        u = i / 9
        prof.append((0.3 * (1 - u) ** 1.6, 0.3 + 0.7 * u))
    prof.append((0, 1.0))
    revolve(bm, prof, 16)


def m_water_splash(bm):
    """밑동 물보라 왕관: 바닥에서 솟은 얇은 물 벽 고리(반지름 약 0.5), 윗변이 10갈래 뾰족하게 튀어오름"""
    prof = [(0.36, 0.0), (0.52, 0.0), (0.58, 0.12), (0.62, 0.26), (0.6, 0.32), (0.55, 0.27), (0.5, 0.12), (0.4, 0.03)]

    def fn(a, t):
        top = max(0.0, 1 - abs(t - 0.57) / 0.25)
        spike = max(0.0, math.cos(10 * a)) ** 6
        return 1 + 0.06 * top * spike, 0.32 * top * spike
    revolve(bm, prof, 60, fn, closed=True)



# ---------- 종말 처형 · 보스 추가 피해: X자 상처 (2026-11-18 사용자: 다키스트 던전 '상처 찢기'처럼) ----------
def m_slash(bm):
    """베인 자국 한 획: X축 -0.5 → 0.5 (시작이 굵고 끝으로 갈수록 날카롭게 가늘어짐), 살짝 휜 칼 획, 위 가장자리가 찢긴 듯 들쭉날쭉.
    XY 평면, +Z를 봄 (부푼 단면이라 툰 불꽃 셰이더가 가운데 → 가장자리로 색이 층짐)"""
    top, bot = [], []
    n = 40
    for i in range(n + 1):
        u = i / n
        x = -0.5 + u
        w = 0.1 * (math.sin(math.pi * u) ** 0.75) * (1.25 - 0.55 * u)
        bend = 0.07 * math.sin(math.pi * u)
        tear = 0.012 * math.sin(u * 47) * math.sin(math.pi * u)
        top.append(V(x, bend + w + tear))
        bot.append(V(x, bend - w * 0.55))
    outline = top + list(reversed(bot[1:-1]))
    puffy(bm, outline, 0.035, center=V(-0.08, 0.08), rings=2)



# ---------- 은빛 메아리 관통 폭발 (2026-11-18 사용자: 관통 자리 폭발을 그림 + 블렌더 3D로) ----------
def m_echo_blade(bm):
    """은빛 칼날 파편: 원점에서 +Z로 뻗는 길이 1의 날카로운 마름모 단면 날. 뿌리 쪽이 좁고 1/3 지점이 가장 넓다가 끝이 바늘처럼.
    면이 각진 마름모(가로 넓고 세로 얇음)라 툰 셰이더가 날 등줄기는 흰 심지, 양 날은 탄 색 → 짙은 테로 층짐"""
    n = 14
    rows = []
    for i in range(n + 1):
        u = i / n
        z = u
        w = 0.11 * (math.sin(math.pi * min(1, u / 0.34) * 0.5) if u < 0.34 else (1 - (u - 0.34) / 0.66) ** 1.35)
        w = max(w, 0.004)
        t = w * 0.32
        kink = 0.018 * math.sin(u * 9.0) * (1 - u)          # 살짝 비뚤게 (깨진 금속 조각 느낌)
        rows.append([bm.verts.new((kink + w, 0, z)), bm.verts.new((kink, t, z)), bm.verts.new((kink - w, 0, z)), bm.verts.new((kink, -t, z))])
    for i in range(n):
        a, b = rows[i], rows[i + 1]
        for k in range(4):
            l = (k + 1) % 4
            bm.faces.new((a[k], a[l], b[l], b[k]))
    bm.faces.new(list(reversed(rows[0])))
    tip = bm.verts.new((0, 0, 1.06))
    last = rows[-1]
    for k in range(4):
        bm.faces.new((last[k], last[(k + 1) % 4], tip))


def m_echo_star(bm):
    """메아리 별: 날카로운 8갈래 별 (긴 4갈래 + 짧은 4갈래를 22° 비틂), 오목하게 파인 팔. XY 평면 +Z를 봄, 반지름 0.5"""
    pts = []
    n = 8
    for i in range(n * 2):
        a = TAU * i / (n * 2) + (0.0 if (i // 2) % 2 == 0 else 0.06)
        if i % 2 == 0:
            r = 0.5 if (i // 2) % 2 == 0 else 0.3
        else:
            r = 0.085
        pts.append(V(math.cos(a) * r, math.sin(a) * r))
    puffy(bm, pts, 0.07, rings=3)


# ---------- 망각: 얼음에 갇힘 · 얼음 깨짐 (2026-11-18 사용자: 불똥 빼고 얼음에 갇히는 느낌만, 터질 땐 얼음 깨지듯) ----------
def crystal(bm, base, r, h, tilt=0.0, yaw=0.0, sides=6, seed=0):
    """육각 수정 기둥: base(바닥 가운데)에서 기울기 tilt(라디안)로 서서 끝이 뾰족함"""
    rnd = random.Random(seed)
    up = Vector((math.sin(tilt) * math.cos(yaw), math.cos(tilt), math.sin(tilt) * math.sin(yaw)))
    side = up.cross(Vector((0, 0, 1)))
    if side.length < 1e-3:
        side = up.cross(Vector((1, 0, 0)))
    side.normalize()
    fwd = up.cross(side).normalized()
    def ring(h_, rr):
        return [bm.verts.new(base + up * h_ + (side * math.cos(TAU * k / sides) + fwd * math.sin(TAU * k / sides)) * rr * rnd.uniform(0.85, 1.1)) for k in range(sides)]
    b0 = ring(0.0, r)
    b1 = ring(h * 0.78, r * 0.95)
    tip = bm.verts.new(base + up * h + side * rnd.uniform(-0.05, 0.05) * r)
    bm.faces.new(list(reversed(b0)))
    for k in range(sides):
        l = (k + 1) % sides
        bm.faces.new((b0[k], b0[l], b1[l], b1[k]))
        bm.faces.new((b1[k], b1[l], tip))


def m_ice_block(bm):
    """얼음 감옥: 몸을 감싸는 굵은 여덟모 얼음 덩어리(반지름 0.5 · 높이 약 0.95, 꼭짓점이 비뚤비뚤) + 위 · 옆으로 삐죽 솟은 얼음 수정 6개. 바닥 y 0"""
    rnd = random.Random(91)
    n = 8
    def ring(y, r, j):
        return [bm.verts.new(V(math.cos(TAU * k / n + 0.2) * r * rnd.uniform(1 - j, 1 + j), y + rnd.uniform(-j, j) * 0.3, math.sin(TAU * k / n + 0.2) * r * rnd.uniform(1 - j, 1 + j))) for k in range(n)]
    r0 = ring(0.0, 0.5, 0.04)
    r1 = ring(0.42, 0.53, 0.08)
    r2 = ring(0.78, 0.46, 0.1)
    top = bm.verts.new(V(rnd.uniform(-0.05, 0.05), 0.95, rnd.uniform(-0.05, 0.05)))
    bm.faces.new(list(reversed(r0)))
    for A, B in ((r0, r1), (r1, r2)):
        for k in range(n):
            l = (k + 1) % n
            bm.faces.new((A[k], A[l], B[l], B[k]))
    for k in range(n):
        bm.faces.new((r2[k], r2[(k + 1) % n], top))
    spikes = [(V(0.12, 0.72, 0.05), 0.13, 0.55, 0.35, 0.3), (V(-0.18, 0.7, -0.1), 0.11, 0.45, 0.5, 3.3),
              (V(0.3, 0.45, -0.15), 0.1, 0.4, 1.0, 5.6), (V(-0.32, 0.35, 0.2), 0.12, 0.42, 1.05, 2.4),
              (V(0.05, 0.6, 0.32), 0.09, 0.35, 0.8, 1.6), (V(-0.05, 0.85, 0.0), 0.1, 0.4, 0.1, 0.0)]
    for i, (b, r, h, tilt, yaw) in enumerate(spikes):
        crystal(bm, b, r, h, tilt, yaw, 6, 200 + i)


def m_ice_chunk(seed):
    """얼음 조각: 각진 덩어리 (뾰족한 볼록 다면체, 지름 약 0.5~0.7)"""
    def f(bm):
        rnd = random.Random(seed)
        pts = []
        for _ in range(14):
            v = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1)))
            if v.length < 1e-3:
                continue
            v.normalize()
            pts.append(bm.verts.new(V(v.x * 0.32 * rnd.uniform(0.7, 1.15), v.y * 0.24 * rnd.uniform(0.7, 1.3), v.z * 0.22 * rnd.uniform(0.7, 1.1))))
        bmesh.ops.convex_hull(bm, input=pts)
        for v in [v for v in bm.verts if not v.link_faces]:
            bm.verts.remove(v)
    return f



def m_ice_petal(bm):
    """얼음 꽃잎: 밑(원점)에서 +Y로 길이 1, 납작한 육각 수정 칼날 (가운데쯤 가장 넓고 끝이 뾰족), 각진 면"""
    n = 6
    def ring(y, rx, rz, rot=0.0):
        return [bm.verts.new(V(math.cos(TAU * k / n + rot) * rx, y, math.sin(TAU * k / n + rot) * rz)) for k in range(n)]
    rings = [ring(0.0, 0.09, 0.05), ring(0.32, 0.2, 0.09, 0.15), ring(0.72, 0.13, 0.065, 0.05)]
    tip = bm.verts.new(V(0.0, 1.0, 0.0))
    bm.faces.new(list(reversed(rings[0])))
    for A, B in zip(rings, rings[1:]):
        for k in range(n):
            l = (k + 1) % n
            bm.faces.new((A[k], A[l], B[l], B[k]))
    for k in range(n):
        bm.faces.new((rings[-1][k], rings[-1][(k + 1) % n], tip))


SHAPES = [
    ("fx_star", m_star, 0), ("fx_sparkle", m_sparkle, 0), ("fx_crown", m_crown, 0),
    ("fx_shard_0", m_shard(3), 0), ("fx_shard_1", m_shard(17), 0), ("fx_shard_2", m_shard(29), 0),
    ("fx_catring", m_catring, 0), ("fx_flower", m_flower, 0), ("fx_spiral", m_spiral, 0),
    ("fx_crescent", m_crescent, 0.03), ("fx_clock", m_clock, 0), ("fx_eye", m_eye, 0),
    ("fx_infinity", m_infinity, 0), ("fx_wisp", m_wisp, 0), ("fx_hand", m_hand, 0), ("fx_charge", m_charge, 0),
    ("fx_spike_0", m_spike(53), 0), ("fx_spike_1", m_spike(64), 0), ("fx_spike_2", m_spike(75), 0),
    ("fx_flame_0", m_flame(101), 0), ("fx_flame_1", m_flame(118), 0), ("fx_flame_2", m_flame(135), 0),
    ("fx_ball_0", m_ball(7), 0), ("fx_ball_1", m_ball(20), 0), ("fx_ball_2", m_ball(33), 0),
    ("fx_ring", m_ring, 0),
    ("fx_glass_0", m_glass(5), 0), ("fx_glass_1", m_glass(12), 0), ("fx_glass_2", m_glass(27), 0),
    ("fx_diamond", m_diamond, 0), ("fx_glint", m_glint, 0),
    ("fx_water_column", m_water_column, 0), ("fx_water_crown", m_water_crown, 0),
    ("fx_water_drop", m_water_drop, 0), ("fx_water_splash", m_water_splash, 0),
    ("fx_slash", m_slash, 0),
    ("fx_ice_block", m_ice_block, 0), ("fx_ice_chunk_0", m_ice_chunk(301), 0), ("fx_ice_chunk_1", m_ice_chunk(317), 0), ("fx_ice_chunk_2", m_ice_chunk(333), 0),
    ("fx_ice_petal", m_ice_petal, 0),
    ("fx_echo_blade", m_echo_blade, 0), ("fx_echo_star", m_echo_star, 0),
]
WATER = {"fx_water_column", "fx_water_crown", "fx_water_drop", "fx_water_splash"}
import sys
ONLY = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SHAPES = [s for s in SHAPES if not ONLY or s[0] in ONLY]
for name, fn, bev in SHAPES:
    build(name, fn, bev, smooth=name in WATER)
print("DONE", len(SHAPES))
