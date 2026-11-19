# 추억 표식 터뜨리기 굽기 (blender_bake_fx.py의 bake_memshatter가 부름)
# 2026-11-18 사용자: 터뜨릴 때 시계 깨지는 느낌 → 유리 깨지는 폭발 느낌, 균일하게(부채꼴로) 부서지지 말 것
# 맞은 자리(가운데에서 비낀 한 점)에 거미줄 금 → 보로노이로 불규칙하게 쪼갠 조각이 그 점에서 터져 나감
#   (맞은 점 가까이는 잘고 빠르게, 멀리는 크고 느리게), 맑은 유리 파편이 가장 빠르게 흩어지고 반짝임 · 섬광 · 충격 물결
import bpy, bmesh, math, random
from mathutils import Vector
from memclock_v2 import lit_mat, soft_mat

TAU = math.tau


def clip_halfplane(poly, p, n):
    """다각형(2D 점 목록)을 (x - p) · n <= 0 쪽만 남김 (Sutherland-Hodgman)"""
    out = []
    for i in range(len(poly)):
        a, b = poly[i], poly[(i + 1) % len(poly)]
        da = (a[0] - p[0]) * n[0] + (a[1] - p[1]) * n[1]
        db = (b[0] - p[0]) * n[0] + (b[1] - p[1]) * n[1]
        if da <= 0: out.append(a)
        if (da < 0) != (db < 0):
            t = da / (da - db)
            out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return out


def voronoi_cells(seeds, R, ring=40):
    base = [(math.cos(TAU * k / ring) * R, math.sin(TAU * k / ring) * R) for k in range(ring)]
    cells = []
    for i, s in enumerate(seeds):
        poly = base[:]
        for j, o in enumerate(seeds):
            if i == j or len(poly) < 3: continue
            mid = ((s[0] + o[0]) / 2, (s[1] + o[1]) / 2)
            poly = clip_halfplane(poly, mid, (o[0] - s[0], o[1] - s[1]))
        if len(poly) >= 3: cells.append((s, poly))
    return cells


def cell_obj(name, poly, mats, front_idx, t):
    """2D 다각형(x, z) → 두께 t 조각. 앞면 재질 = mats[front_idx], 옆 · 뒤 = mats[-1]. 원점 = 무게중심"""
    cx = sum(p[0] for p in poly) / len(poly); cz = sum(p[1] for p in poly) / len(poly)
    me = bpy.data.meshes.new(name); bm = bmesh.new()
    for m in mats: me.materials.append(m)
    f = [bm.verts.new((x - cx, -t, z - cz)) for x, z in poly]
    b = [bm.verts.new((x - cx, t, z - cz)) for x, z in poly]
    ff = bm.faces.new(f); ff.material_index = front_idx
    fb = bm.faces.new(list(reversed(b))); fb.material_index = len(mats) - 1
    n = len(poly)
    for k in range(n):
        l = (k + 1) % n
        sd = bm.faces.new((f[l], f[k], b[k], b[l])); sd.material_index = len(mats) - 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    o.location = (cx, 0, cz)
    return o


def bake(sc, H):
    sc.render.resolution_x = sc.render.resolution_y = 384
    H.set_line_color(sc, (0.16, 0.08, 0.04)); sc.render.line_thickness = 1.6
    ink = bpy.data.collections.new("ink"); sc.collection.children.link(ink)
    for s_ in sc.view_layers[0].freestyle_settings.linesets:
        s_.select_by_collection = True; s_.collection = ink; s_.linestyle.thickness = 1.6; s_.linestyle.alpha = 0.85

    def link_ink(o):
        for c in list(o.users_collection): c.objects.unlink(o)
        ink.objects.link(o); return o

    setv, ease_out = H.setv, H.ease_out
    brass = [(0.0, (0.16, 0.07, 0.012)), (0.42, (0.42, 0.2, 0.035)), (0.62, (0.72, 0.41, 0.08)), (0.84, (1.0, 0.82, 0.42))]
    face_b = [(0.0, (0.62, 0.52, 0.36)), (0.45, (0.86, 0.78, 0.6)), (0.7, (0.98, 0.95, 0.86))]
    mb = lit_mat("brass", brass, 1.0, opaque=True)
    mc = lit_mat("face", face_b, 1.0, (0.4, 0.3, 0.2), opaque=True)
    md = lit_mat("dark", [(0.0, (0.08, 0.04, 0.04)), (0.6, (0.2, 0.12, 0.1))], 1.0, opaque=True)
    R, Ri = 0.62, 0.62 * 0.84
    rnd = random.Random(11)
    hit = (0.17, 0.12)                                         # 맞은 점 (가운데에서 비낌)

    # --- 문자판: 맞은 점 가까이 씨앗을 촘촘히 (잔 조각), 멀리 듬성듬성 (큰 조각) ---
    seeds = [hit]
    while len(seeds) < 17:
        a = rnd.uniform(0, TAU); r = Ri * (rnd.random() ** 0.75) * 1.05
        p = (hit[0] + math.cos(a) * r, hit[1] + math.sin(a) * r)
        if p[0] ** 2 + p[1] ** 2 < Ri * Ri and all((p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 > 0.012 for q in seeds): seeds.append(p)
    pieces = []
    for i, (sd, poly) in enumerate(voronoi_cells(seeds, Ri)):
        o = link_ink(cell_obj(f"f{i}", poly, [mc, mb], 0, 0.035))
        pieces.append(o)
    # --- 놋쇠 테: 불규칙한 길이의 호 조각 5개 ---
    cuts = sorted(rnd.uniform(0, TAU) for _ in range(5)); cuts.append(cuts[0] + TAU)
    for i in range(5):
        a0, a1 = cuts[i], cuts[i + 1]
        seg = max(3, int((a1 - a0) / 0.15))
        outer = [(math.cos(a0 + (a1 - a0) * k / seg) * R, math.sin(a0 + (a1 - a0) * k / seg) * R) for k in range(seg + 1)]
        inner = [(math.cos(a0 + (a1 - a0) * k / seg) * Ri, math.sin(a0 + (a1 - a0) * k / seg) * Ri) for k in range(seg, -1, -1)]
        o = link_ink(cell_obj(f"rim{i}", outer + inner, [mb], 0, 0.07))
        pieces.append(o)
    # 꼭지 (테 조각처럼 같이 튐)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.15, minor_radius=0.04, major_segments=32, minor_segments=10, rotation=(math.pi / 2, 0, 0), location=(0, 0, R + 0.22))
    bow = link_ink(bpy.context.active_object); bow.data.materials.append(mb); pieces.append(bow)
    for o in pieces:
        p = Vector((o.location.x - hit[0], 0, o.location.z - hit[1]))
        dist = max(0.06, p.length)
        d = p.normalized() if p.length > 1e-4 else Vector((rnd.uniform(-1, 1), 0, 1)).normalized()
        o["p0"] = tuple(o.location); o["r0"] = tuple(o.rotation_euler)
        o["dir"] = (d.x, d.z)
        o["v"] = rnd.uniform(0.8, 1.05) * (0.5 + 0.45 / (dist + 0.35))        # 맞은 점 가까울수록 빠르게
        o["rot"] = (rnd.uniform(-1, 1) * 9, rnd.uniform(-1, 1) * 7, rnd.uniform(-1, 1) * 9)
        o["toward"] = rnd.uniform(-0.6, -0.1)                                 # 카메라 쪽으로도 살짝 튐 (입체감)
    # 바늘 (깨지기 전 시계 모양)
    hm = link_ink(H.box_obj("minute", md, -0.024, 0.024, -0.07, R * 0.7, 0.012, -0.06)); hm.rotation_euler = (0, 0.5, 0)
    hh = link_ink(H.box_obj("hour", md, -0.034, 0.034, -0.05, R * 0.44, 0.012, -0.055)); hh.rotation_euler = (0, 2.2, 0)
    # --- 거미줄 금: 맞은 점에서 뻗는 들쭉날쭉 선 + 둘레 고리 금 ---
    cr_m = lit_mat("crack", [(0.0, (0.03, 0.015, 0.015)), (0.6, (0.08, 0.04, 0.03))], 1.0, opaque=True)
    cracks = []
    for i in range(9):
        a = TAU * i / 9 + rnd.uniform(-0.25, 0.25)
        x, z = hit; L = 0
        steps = rnd.randint(3, 5)
        for k in range(steps):
            a += rnd.uniform(-0.45, 0.45); ln = rnd.uniform(0.07, 0.16)
            nx, nz = x + math.cos(a) * ln, z + math.sin(a) * ln
            if nx * nx + nz * nz > Ri * Ri: break
            c = link_ink(H.box_obj(f"cr{i}_{k}", cr_m, -0.009, 0.009, 0.0, ln, 0.006, -0.05))
            c.location = (x, 0, z); c.rotation_euler = (0, math.atan2(nx - x, nz - z), 0)
            c["at"] = (L + ln) / 0.55                         # 맞은 점에서 멀수록 늦게 나타남
            cracks.append(c); L += ln; x, z = nx, nz
    for i in range(10):                                       # 둘레 고리 금 (짧은 조각)
        a = TAU * i / 10 + rnd.uniform(-0.2, 0.2); r = rnd.uniform(0.13, 0.22)
        x, z = hit[0] + math.cos(a) * r, hit[1] + math.sin(a) * r
        c = link_ink(H.box_obj(f"rc{i}", cr_m, -0.008, 0.008, 0.0, rnd.uniform(0.06, 0.1), 0.006, -0.05))
        c.location = (x, 0, z); c.rotation_euler = (0, a + math.pi / 2 + rnd.uniform(-0.3, 0.3), 0); c["at"] = 0.7
        cracks.append(c)
    # --- 맑은 유리 파편: 맞은 점에서 가장 빠르게 사방으로 (반투명 + 가장자리 빛) ---
    glass_m = soft_mat("glass", (0.92, 0.97, 1.0), 0.0, "fresnel")
    gseeds = [(hit[0] + rnd.gauss(0, 0.22), hit[1] + rnd.gauss(0, 0.22)) for _ in range(22)]
    gseeds = [p for p in gseeds if p[0] ** 2 + p[1] ** 2 < R * R]
    glass = []
    for i, (sd, poly) in enumerate(voronoi_cells(gseeds, R * 0.9)):
        o = cell_obj(f"g{i}", poly, [glass_m], 0, 0.012)
        o.location.y = -0.3
        p = Vector((o.location.x - hit[0], 0, o.location.z - hit[1]))
        d = p.normalized() if p.length > 1e-4 else Vector((0, 0, 1))
        o["p0"] = tuple(o.location); o["dir"] = (d.x, d.z); o["v"] = rnd.uniform(1.3, 2.1)
        o["rot"] = (rnd.uniform(-1, 1) * 14, rnd.uniform(-1, 1) * 10, rnd.uniform(-1, 1) * 14)
        glass.append(o)
    # 반짝임 (유리 조각에 빛이 번쩍)
    glint_m = soft_mat("glint", (1, 1, 1), 0.0, "fresnel")
    glints = []
    for i in range(8):
        o = H.star_obj(f"gl{i}", glint_m, 4, rnd.uniform(0.12, 0.2), 0.03, 0.02)
        a = rnd.uniform(0, TAU); o["dir"] = (math.cos(a), math.sin(a)); o["r"] = rnd.uniform(0.5, 1.3); o["lag"] = rnd.uniform(0.0, 0.3)
        glints.append(o)
    flash_m = soft_mat("flash", (1, 0.98, 0.9), 0.0, "radial")
    bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.0, fill_type='NGON', rotation=(math.pi / 2, 0, 0), location=(hit[0], -0.4, hit[1]))
    flash = bpy.context.active_object; flash.data.materials.append(flash_m)
    waves = []
    for i in range(2):
        wm = soft_mat(f"wave{i}", (1.0, 0.9, 0.65), 0.0, "ring", 0.82, 1.0)
        bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.0, fill_type='NGON', rotation=(math.pi / 2, 0, 0), location=(hit[0], 0.3, hit[1]))
        w = bpy.context.active_object; w.data.materials.append(wm); waves.append((w, wm, 0.0 + i * 0.1))

    N, T0 = 22, 0.2                                            # T0 = 깨지는 순간 (그 전엔 금이 번지며 떨림)
    for f in range(N):
        t = f / (N - 1)
        pre = t < T0
        k = t / T0 if pre else 1
        shake = (math.sin(t * 120) * 0.025 * k) if pre else 0
        q = 0.0 if pre else (t - T0) / (1 - T0)
        e = ease_out(q, 3)
        for m in (mb, mc): setv(m, "dissolve", max(0, (q - 0.55) / 0.45) * 0.9)
        for o in pieces:
            d = o["v"] * e
            o.location = (o["p0"][0] + o["dir"][0] * d + shake, o["toward"] * e, o["p0"][2] + o["dir"][1] * d - 0.45 * q * q)
            o.rotation_euler = (o["r0"][0] + o["rot"][0] * e * 0.25, o["r0"][1] + o["rot"][1] * e * 0.25, o["r0"][2] + o["rot"][2] * e * 0.25)
            o.hide_render = q > 0.85
        for o in (hm, hh):
            o.hide_render = not pre
        for c in cracks:
            c.hide_render = not pre or k < c["at"] * 0.85
        setv(glass_m, "alpha", 0.0 if pre else 0.95 * (1 - q) ** 0.7)
        for o in glass:
            d = o["v"] * e
            o.location = (o["p0"][0] + o["dir"][0] * d, -0.3 - 0.3 * e, o["p0"][2] + o["dir"][1] * d - 0.3 * q * q)
            o.rotation_euler = (o["rot"][0] * e * 0.3, o["rot"][1] * e * 0.3, o["rot"][2] * e * 0.3)
            o.hide_render = pre
        for o in glints:
            u = (q - o["lag"]) / 0.25
            v = math.sin(min(1, max(0, u)) * math.pi) if 0 <= u <= 1 else 0
            r = o["r"] * (0.5 + 0.5 * e)
            o.location = (hit[0] + o["dir"][0] * r, -0.5, hit[1] + o["dir"][1] * r)
            o.scale = (v,) * 3; o.hide_render = pre or v < 0.02
            o.rotation_euler = (0, u * 1.2, 0)
        setv(glint_m, "alpha", 1.0)
        fp = max(0.0, 1 - abs(t - T0) / 0.1)
        flash.scale = (0.6 + 0.8 * fp,) * 3; setv(flash_m, "alpha", 0.95 * fp)
        for w, wm, lag in waves:
            u = max(0.0, (q - lag) / 0.5) if not pre else 0
            s = 0.4 + 1.3 * ease_out(min(1, u), 3); w.scale = (s, s, s)
            setv(wm, "alpha", 0.8 * (1 - min(1, u)) if u > 0 else 0)
        H.render(sc, "memshatter", f)
