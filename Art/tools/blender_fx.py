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


def build(name, fn, bevel=0.0):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    fn(bm)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.transform(U2B)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)          # 거울 변환(행렬식 -1)으로 뒤집힌 감김을 되돌림
    bm.to_mesh(me)
    bm.free()
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


SHAPES = [
    ("fx_star", m_star, 0), ("fx_sparkle", m_sparkle, 0), ("fx_crown", m_crown, 0),
    ("fx_shard_0", m_shard(3), 0), ("fx_shard_1", m_shard(17), 0), ("fx_shard_2", m_shard(29), 0),
    ("fx_catring", m_catring, 0), ("fx_flower", m_flower, 0), ("fx_spiral", m_spiral, 0),
    ("fx_crescent", m_crescent, 0.03), ("fx_clock", m_clock, 0), ("fx_eye", m_eye, 0),
    ("fx_infinity", m_infinity, 0), ("fx_wisp", m_wisp, 0),
    ("fx_spike_0", m_spike(53), 0), ("fx_spike_1", m_spike(64), 0), ("fx_spike_2", m_spike(75), 0),
    ("fx_flame_0", m_flame(101), 0), ("fx_flame_1", m_flame(118), 0), ("fx_flame_2", m_flame(135), 0),
    ("fx_ball_0", m_ball(7), 0), ("fx_ball_1", m_ball(20), 0), ("fx_ball_2", m_ball(33), 0),
    ("fx_ring", m_ring, 0),
]
import sys
ONLY = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SHAPES = [s for s in SHAPES if not ONLY or s[0] in ONLY]
for name, fn, bev in SHAPES:
    build(name, fn, bev)
print("DONE", len(SHAPES))
