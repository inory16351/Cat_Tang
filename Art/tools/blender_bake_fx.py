# Cat_Tang 3D 이펙트 → 플립북 이미지 굽기 (Blender 5.x, 백그라운드)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_bake_fx.py [-- 세트이름...]
# 결과: Game/Assets/Resources/FX/anim/bk_<세트>_<프레임>.png (투명 배경, 정사각)
#   → FxPart.flipSet = "bk_<세트>" 로 이펙트 프리팹 조각에서 재생 (FxFlipbook과 같은 폴더 규칙)
# 화풍: 툰 불꽃 셰이더와 같은 규칙 — 카메라를 정면으로 보는 면일수록 뜨거운 색, 가장자리일수록 짙은 테 (Layer Weight → 색 띠, 계단식)
#       + 잉크 외곽선 (Freestyle) + 노이즈 구멍으로 녹아 사라짐. 조명 계산 없음 (Emission)
import bpy, bmesh, math, random, os, sys
from mathutils import Vector, noise

OUT = r"C:/Project/Cat_Tang/Game/Assets/Resources/FX/anim"
RES = 256
FRAMES = 16
os.makedirs(OUT, exist_ok=True)
TAU = math.tau


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            sc.render.engine = eng
            break
        except TypeError:
            continue
    sc.render.resolution_x = sc.render.resolution_y = RES
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    # 잉크 외곽선
    sc.render.use_freestyle = True
    sc.render.line_thickness_mode = 'ABSOLUTE'
    sc.render.line_thickness = 2.2
    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 3.2
    cam = bpy.data.objects.new("cam", cam_data)
    sc.collection.objects.link(cam)
    cam.location = (0, -10, 0)
    cam.rotation_euler = (math.pi / 2, 0, 0)
    sc.camera = cam
    return sc


def set_line_color(sc, rgb):
    ls = sc.view_layers[0].freestyle_settings.linesets
    if not ls:
        sc.view_layers[0].freestyle_settings.linesets.new("ink")
    for s in sc.view_layers[0].freestyle_settings.linesets:
        s.select_by_visibility = True
        s.select_silhouette = True
        s.select_border = True
        s.select_crease = False
        if s.linestyle is None:
            s.linestyle = bpy.data.linestyles.new("ink")
        s.linestyle.color = rgb
        s.linestyle.thickness = 2.2


def toon_mat(name, bands):
    """bands: [(위치 0~1, (r,g,b))] — Layer Weight(Facing) 기준 계단 색. 위치 0 = 정면(심지), 1 = 가장자리.
    재질 값 'heat'(0~1)로 전체를 식은 색으로, 'dissolve'(0~1)로 노이즈 구멍"""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    try:
        m.surface_render_method = 'DITHERED'
    except Exception:
        m.blend_method = 'CLIP'
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.35
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = 'CONSTANT'
    els = ramp.color_ramp.elements
    els[0].position = bands[0][0]; els[0].color = (*bands[0][1], 1)
    els[1].position = bands[1][0]; els[1].color = (*bands[1][1], 1)
    for pos, col in bands[2:]:
        e = els.new(pos); e.color = (*col, 1)
    # heat: facing 값에 더해 전체를 가장자리 쪽 색으로 밀어냄 (식음)
    heat = nt.nodes.new("ShaderNodeValue"); heat.name = "heat"; heat.outputs[0].default_value = 1.0
    inv = nt.nodes.new("ShaderNodeMath"); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    nt.links.new(heat.outputs[0], inv.inputs[1])
    add = nt.nodes.new("ShaderNodeMath"); add.operation = 'ADD'; add.use_clamp = True
    nt.links.new(lw.outputs["Facing"], add.inputs[0])
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'; mul.inputs[1].default_value = 0.8
    nt.links.new(inv.outputs[0], mul.inputs[0])
    nt.links.new(mul.outputs[0], add.inputs[1])
    nt.links.new(add.outputs[0], ramp.inputs["Fac"])
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.0
    nt.links.new(ramp.outputs["Color"], em.inputs["Color"])
    # dissolve: 3D 노이즈 > dissolve 인 곳만 남김
    tex = nt.nodes.new("ShaderNodeTexNoise"); tex.inputs["Scale"].default_value = 3.0; tex.inputs["Detail"].default_value = 3.0
    dis = nt.nodes.new("ShaderNodeValue"); dis.name = "dissolve"; dis.outputs[0].default_value = 0.0
    cmp = nt.nodes.new("ShaderNodeMath"); cmp.operation = 'GREATER_THAN'
    nt.links.new(tex.outputs["Fac"], cmp.inputs[0])
    nt.links.new(dis.outputs[0], cmp.inputs[1])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(cmp.outputs[0], mix.inputs["Fac"])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    return m


def lumpy(name, seed, mat, subdiv=3):
    """혹이 붙은 구름 덩어리 (지름 약 1)"""
    rnd = random.Random(seed)
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=0.5)
    lobes = [(Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1))).normalized(), rnd.uniform(0.18, 0.34), rnd.uniform(0.35, 0.6)) for _ in range(rnd.randint(4, 6))]
    for v in bm.verts:
        d = v.co.normalized()
        bulge = 0.0
        for (c, h, w) in lobes:
            bulge = max(bulge, h * max(0.0, (d.dot(c) - (1 - w)) / w) ** 1.5)
        v.co = d * 0.5 * (0.82 + bulge + 0.05 * noise.noise(d * 2.2 + Vector((seed, 0, 0))))
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def spike_obj(name, mat, w=0.06, L=1.0):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    ring = []
    for k in range(8):
        a = TAU * k / 8
        r = w if k % 2 == 0 else w * 0.45
        ring.append(bm.verts.new((math.cos(a) * r, 0.12 * L, math.sin(a) * r)))
    tip = bm.verts.new((0, L, 0)); base = bm.verts.new((0, 0, 0))
    for k in range(8):
        l = (k + 1) % 8
        bm.faces.new((ring[k], ring[l], tip)); bm.faces.new((ring[l], ring[k], base))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def setv(mat, node, v):
    mat.node_tree.nodes[node].outputs[0].default_value = v


def ease_out(t, p=3):
    return 1 - (1 - t) ** p


def render(sc, set_name, f):
    sc.render.filepath = os.path.join(OUT, f"bk_{set_name}_{f}.png")
    bpy.ops.render.render(write_still=True)


# ---------- 세트 ----------

def bake_fire():
    """폭발 불꽃: 흰 심지 덩어리가 펑 → 불덩이들이 바깥 · 위로 부풀며 노랑 → 주황 → 빨강 → 그을음으로 식고 녹음"""
    sc = reset(); set_line_color(sc, (0.16, 0.04, 0.05))
    rnd = random.Random(3)
    mats, obs = [], []
    for i in range(7):
        m = toon_mat(f"fire{i}", [(0.0, (1, 0.98, 0.86)), (0.14, (1, 0.82, 0.25)), (0.34, (1, 0.45, 0.1)), (0.58, (0.78, 0.13, 0.08)), (0.85, (0.3, 0.17, 0.15))])
        o = lumpy(f"f{i}", 10 + i, m)
        d = Vector((rnd.uniform(-1, 1), rnd.uniform(-0.6, 0.6), rnd.uniform(-0.3, 1))).normalized()
        o["dir"] = d; o["s"] = rnd.uniform(0.55, 0.85); o["lag"] = rnd.uniform(0, 0.15); o["spin"] = rnd.uniform(-1.2, 1.2)
        mats.append(m); obs.append(o)
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        for o, m in zip(obs, mats):
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            grow = ease_out(min(1, k / 0.35)) * (1 + 0.35 * k)
            o.location = Vector(o["dir"]) * (0.15 + 0.85 * ease_out(k, 2)) + Vector((0, 0, -0.35))
            fade = min(1.0, max(0.0, (k - 0.7) / 0.3))          # 끝 무렵엔 오그라들어 사라짐 (외곽선만 남지 않게)
            o.scale = Vector((1, 1, 1)) * o["s"] * max(0.001, grow * (1 - fade) ** 1.5)
            o.hide_render = fade >= 0.999
            o.rotation_euler = (o["spin"] * k * 2, o["spin"] * k, 0)
            setv(m, "heat", 1 - min(1, max(0, (k - 0.15) / 0.7)))
            setv(m, "dissolve", min(0.55, max(0, (k - 0.45) / 0.55)) ** 1.2)
        render(sc, "fire", f)


def bake_smoke():
    """연기 뭉게: 회색 덩어리가 천천히 피어올라 커지며 녹아 흩어짐"""
    sc = reset(); set_line_color(sc, (0.1, 0.08, 0.08))
    rnd = random.Random(7)
    mats, obs = [], []
    for i in range(6):
        m = toon_mat(f"smoke{i}", [(0.0, (0.9, 0.87, 0.84)), (0.2, (0.7, 0.66, 0.63)), (0.45, (0.5, 0.46, 0.44)), (0.75, (0.32, 0.29, 0.28))])
        o = lumpy(f"s{i}", 30 + i, m)
        d = Vector((rnd.uniform(-1, 1), rnd.uniform(-0.5, 0.5), rnd.uniform(0, 1))).normalized()
        o["dir"] = d; o["s"] = rnd.uniform(0.5, 0.75); o["lag"] = rnd.uniform(0, 0.2)
        mats.append(m); obs.append(o)
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        for o, m in zip(obs, mats):
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            o.location = Vector(o["dir"]) * 0.6 * ease_out(k, 2) + Vector((0, 0, 0.5 * k - 0.3))
            fade = min(1.0, max(0.0, (k - 0.7) / 0.3))
            o.scale = Vector((1, 1, 1)) * o["s"] * max(0.001, ease_out(min(1, k / 0.3)) * (1 + 0.6 * k) * (1 - fade) ** 1.5)
            o.hide_render = fade >= 0.999
            setv(m, "heat", 1 - 0.5 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.35) / 0.65)))
        render(sc, "smoke", f)


def bake_spark():
    """타격 불똥: 첫 프레임 섬광 덩어리 + 가는 가시들이 사방으로 쏜살같이 뻗었다 가늘어지며 사라짐"""
    sc = reset(); set_line_color(sc, (0.18, 0.06, 0.03)); sc.render.line_thickness = 1.3
    for s_ in sc.view_layers[0].freestyle_settings.linesets: s_.linestyle.thickness = 1.3
    rnd = random.Random(11)
    core_m = toon_mat("core", [(0.0, (1, 1, 0.95)), (0.18, (1, 0.9, 0.45)), (0.42, (1, 0.62, 0.18)), (0.7, (0.9, 0.3, 0.08))])
    core = lumpy("core", 5, core_m)
    sp = []
    for i in range(12):
        m = toon_mat(f"sp{i}", [(0.0, (1, 1, 0.92)), (0.15, (1, 0.85, 0.35)), (0.4, (1, 0.55, 0.14)), (0.7, (0.85, 0.25, 0.08))])
        o = spike_obj(f"sp{i}", m, w=rnd.uniform(0.11, 0.17), L=1.0)
        a = TAU * i / 12 + rnd.uniform(-0.2, 0.2)
        o.rotation_euler = (math.pi / 2, a, 0)   # 스파이크 축 +Y → +Z로 세운 뒤 Y축으로 돌려 화면(X-Z) 평면에서 사방으로
        o["L"] = rnd.uniform(0.6, 1.4); o["lag"] = rnd.uniform(0, 0.12)
        sp.append((o, m))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        c = max(0.0, 1 - t / 0.35)
        core.scale = Vector((1, 1, 1)) * (0.25 + 0.7 * math.sin(min(1, t / 0.15) * math.pi / 2)) * (c ** 0.5 if t > 0.15 else 1)
        core.hide_render = t > 0.4
        setv(core_m, "dissolve", max(0, (t - 0.15) / 0.25))
        for o, m in sp:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            dirv = o.matrix_world.to_3x3() @ Vector((0, 1, 0))
            o.location = dirv.normalized() * 1.1 * ease_out(k, 2)
            o.scale = (max(0.02, 1 - k), o["L"] * (0.4 + 0.6 * ease_out(min(1, k / 0.2))), max(0.02, 1 - k))
            setv(m, "heat", 1 - k)
            setv(m, "dissolve", max(0, (k - 0.5) / 0.5))
        render(sc, "spark", f)


def bake_mist():
    """사망 검은 안개: 몸 크기의 검보라 덩어리들이 소용돌이치며 위로 솟고 길쭉하게 늘어나다 흩어짐"""
    sc = reset(); set_line_color(sc, (0.16, 0.06, 0.22))
    rnd = random.Random(21)
    mats, obs = [], []
    for i in range(8):
        m = toon_mat(f"mist{i}", [(0.0, (0.15, 0.11, 0.19)), (0.22, (0.08, 0.06, 0.11)), (0.5, (0.04, 0.03, 0.06)), (0.8, (0.015, 0.012, 0.02))])
        o = lumpy(f"m{i}", 50 + i, m)
        a = TAU * i / 8 + rnd.uniform(-0.3, 0.3)
        o["a"] = a; o["r"] = rnd.uniform(0.15, 0.45); o["z0"] = rnd.uniform(-0.9, 0.3); o["s"] = rnd.uniform(0.45, 0.7); o["lag"] = rnd.uniform(0, 0.18)
        mats.append(m); obs.append(o)
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        for o, m in zip(obs, mats):
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            a = o["a"] + k * 2.2                                   # 소용돌이
            r = o["r"] * (1 + 0.8 * k)
            o.location = (math.cos(a) * r, 0, o["z0"] + 1.1 * ease_out(k, 2))
            fade = min(1.0, max(0.0, (k - 0.65) / 0.35))
            grow = ease_out(min(1, k / 0.25))
            o.scale = (o["s"] * grow * (1 - 0.3 * k) * (1 - fade) ** 1.3, o["s"] * grow * (1 - fade) ** 1.3, o["s"] * grow * (1 + 0.9 * k) * (1 - fade) ** 1.3)
            o.hide_render = fade >= 0.999 or grow <= 0.001
            setv(m, "heat", 1 - 0.6 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.4) / 0.6)))
        render(sc, "mist", f)


def star_obj(name, mat, points=4, r_out=0.5, r_in=0.08, t=0.06):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    n = points * 2
    ring = []
    for k in range(n):
        a = TAU * k / n + math.pi / 2
        r = r_out if k % 2 == 0 else r_in
        ring.append(bm.verts.new((math.cos(a) * r, 0, math.sin(a) * r)))
    front = bm.verts.new((0, -t, 0)); back = bm.verts.new((0, t, 0))
    for k in range(n):
        l = (k + 1) % n
        bm.faces.new((ring[k], ring[l], front)); bm.faces.new((ring[l], ring[k], back))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bake_dodge():
    """닷지 액션 (젠레스 존 제로 극한 회피 느낌): 파란 고리 두 겹이 퍼지고, 큰 4갈래 별이 번쩍였다 줄고, 작은 반짝임이 차례로 깜빡임"""
    sc = reset(); set_line_color(sc, (0.04, 0.08, 0.25)); sc.render.line_thickness = 1.6
    for s_ in sc.view_layers[0].freestyle_settings.linesets: s_.linestyle.thickness = 1.6
    blue = [(0.0, (1, 1, 1)), (0.2, (0.75, 0.9, 1)), (0.45, (0.4, 0.7, 1)), (0.75, (0.18, 0.4, 0.95))]
    rings = []
    for i in range(2):
        m = toon_mat(f"ring{i}", blue)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.03 if i == 0 else 0.022, major_segments=64, minor_segments=10)
        o = bpy.context.active_object
        o.rotation_euler = (math.pi / 2, 0, 0)            # 고리 축을 카메라 쪽(Y)으로
        o.data.materials.append(m)
        rings.append((o, m, 0.0 if i == 0 else 0.12))
    sm = toon_mat("star", blue)
    star = star_obj("star", sm, 4, 0.85, 0.16, 0.1)
    sparks = []
    rnd = random.Random(31)
    for i in range(7):
        m = toon_mat(f"tw{i}", blue)
        o = star_obj(f"tw{i}", m, 4, 0.26, 0.07, 0.05)
        a = rnd.uniform(0, TAU); r = rnd.uniform(0.45, 1.05)
        o.location = (math.cos(a) * r, 0, math.sin(a) * r * 0.9)
        o["lag"] = 0.08 + i * 0.09; o["s"] = rnd.uniform(0.6, 1.1)
        sparks.append((o, m))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        for o, m, lag in rings:
            k = max(0.0, (t - lag) / (1 - lag))
            s = 0.25 + 1.7 * ease_out(k, 3)
            o.scale = (s, s, s)
            o.hide_render = k <= 0 or k >= 0.8
            setv(m, "heat", 1 - k)
            setv(m, "dissolve", max(0, (k - 0.55) / 0.45) * 0.6)
        pop = math.sin(min(1, t / 0.12) * math.pi / 2) * max(0.0, 1 - max(0, t - 0.12) / 0.45)
        star.scale = (pop, pop, pop)
        star.rotation_euler = (0, t * 0.6, 0)
        star.hide_render = pop <= 0.01
        setv(sm, "heat", 1 - t)
        for o, m in sparks:
            k = (t - o["lag"]) / 0.3
            v = math.sin(min(1, max(0, k)) * math.pi) if 0 <= k <= 1 else 0
            o.scale = (v * o["s"],) * 3
            o.hide_render = v <= 0.01
            o.rotation_euler = (0, k * 1.5, 0)
        render(sc, "dodge", f)


def gear_obj(name, mat, teeth=8, r=0.2, t=0.05):
    """톱니바퀴 (카메라 쪽 = -Y 면), 가운데 구멍"""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    n = teeth * 4
    outer, inner = [], []
    for k in range(n):
        a = TAU * k / n
        rr = r * (1.0 if (k % 4) in (1, 2) else 0.78)
        outer.append((math.cos(a) * rr, math.sin(a) * rr))
        inner.append((math.cos(a) * r * 0.32, math.sin(a) * r * 0.32))
    def ring(pts, y): return [bm.verts.new((x, y, z)) for (x, z) in pts]
    of, ob_, inf, inb = ring(outer, -t), ring(outer, t), ring(inner, -t), ring(inner, t)
    for k in range(n):
        l = (k + 1) % n
        bm.faces.new((of[k], of[l], inf[l], inf[k])); bm.faces.new((ob_[l], ob_[k], inb[k], inb[l]))
        bm.faces.new((of[l], of[k], ob_[k], ob_[l])); bm.faces.new((inf[k], inf[l], inb[l], inb[k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    o.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(o)
    return o


def box_obj(name, mat, x0, x1, z0, z1, t=0.03, y=0.0):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    v = [bm.verts.new((x, y + dy, z)) for dy in (-t, t) for (x, z) in ((x0, z0), (x1, z0), (x1, z1), (x0, z1))]
    for f in ((0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)):
        bm.faces.new([v[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    o.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(o)
    return o


def bake_memclock():
    """추억 시계 v2 (반투명 · 한쪽 빛 입체 음영 · 유리 빛줄기 · 후광 · 빛 물결 · 기억 빛알 · 바늘 잔상): Art/tools/memclock_v2.py"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import memclock_v2
    memclock_v2.bake(reset(), sys.modules[__name__])


def bake_memshatter():
    """추억 표식 터뜨리기: 회중시계가 금 가며 깨져 조각 · 유리 파편 · 톱니바퀴가 튐 (Art/tools/memshatter.py)"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import memshatter
    memshatter.bake(reset(), sys.modules[__name__])


# ---------- 3장 (거울 세계) 일반 몬스터 탄 · 불똥 (2026-11-18 사용자: 2D 말고 블렌더로 구운 3D로) ----------
# 장미빛 유리 결정 계열: 정면 = 흰 심지 → 분홍 → 자홍 → 짙은 보라 테 (적 탄은 보라 ~ 붉은 계열 규칙 유지), 잉크 = 짙은 보라
C3_GEM = [(0.0, (1, 0.93, 0.99)), (0.05, (1, 0.5, 0.84)), (0.17, (0.9, 0.24, 0.74)), (0.36, (0.62, 0.12, 0.64)), (0.6, (0.34, 0.06, 0.42))]
C3_GLASS = [(0.0, (1, 1, 1)), (0.2, (0.85, 0.96, 1)), (0.45, (0.62, 0.8, 1)), (0.72, (0.5, 0.42, 0.9))]
C3_INK = (0.2, 0.05, 0.26)


def ink(sc, thick):
    set_line_color(sc, C3_INK)
    sc.render.line_thickness = thick
    for s_ in sc.view_layers[0].freestyle_settings.linesets:
        s_.linestyle.thickness = thick


def mesh_obj(name, mat, verts, faces, smooth=False):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    for p_ in me.polygons:
        p_.use_smooth = smooth
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def gem_obj(name, mat, radius):
    """깎은 보석 (정이십면체, 면마다 색이 계단으로 갈림). 꼭짓점이 Z축 위아래 → Z축 72° 대칭"""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=radius)
    bm.to_mesh(me); bm.free()
    for p_ in me.polygons:
        p_.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def shard_obj(name, mat, front=0.95, back=0.55, r=0.2, sides=6):
    """유리 조각: +X로 뾰족한 길쭉한 쌍뿔 (가운데 육각 고리) — X축 60° 대칭"""
    verts = [(front, 0, 0), (-back, 0, 0)]
    for k in range(sides):
        a = TAU * k / sides
        verts.append((0.12, math.cos(a) * r, math.sin(a) * r))
    faces = []
    for k in range(sides):
        a, b = 2 + k, 2 + (k + 1) % sides
        faces.append((0, a, b)); faces.append((1, b, a))
    ob = mesh_obj(name, mat, verts, faces)
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    return ob


def bake_c3orb():
    """3장 적 구슬 탄 (반복 16장): 깎은 장미빛 보석이 돌고, 기울어진 가는 빛 고리 + 작은 4갈래 반짝임 3개가 둘레를 돎"""
    sc = reset(); ink(sc, 1.8)
    gm = toon_mat("c3gem", C3_GEM)
    gem = gem_obj("gem", gm, 0.62)
    rm = toon_mat("c3ring", C3_GLASS)
    bpy.ops.mesh.primitive_torus_add(major_radius=1.0, minor_radius=0.035, major_segments=64, minor_segments=8)
    ring = bpy.context.active_object
    ring.data.materials.append(rm)
    ring.rotation_euler = (math.pi / 2 - 1.15, 0.35, 0)     # 비스듬히 누운 고리
    stars = []
    for i in range(3):
        m = toon_mat(f"c3tw{i}", C3_GLASS)
        stars.append(star_obj(f"tw{i}", m, 4, 0.2, 0.05, 0.04))
    tilt = ring.matrix_world.to_3x3()
    for f in range(FRAMES):
        t = f / FRAMES                                         # 반복: 마지막 다음 = 처음
        gem.rotation_euler = (0.35, 0, TAU / 5 * t)             # 72° 대칭이라 이어짐
        for i, st in enumerate(stars):
            a = TAU * (t + i / 3)
            st.location = tilt @ Vector((math.cos(a), math.sin(a), 0))
            tw = 0.75 + 0.25 * math.sin(TAU * (t * 2 + i / 3))
            st.scale = (tw, tw, tw)
            st.rotation_euler = (0, TAU / 4 * t, 0)            # 4갈래 별 → 90° 대칭
            st.hide_render = st.location.y > -0.25             # 보석 뒤 · 안으로 돌면 숨김 (앞쪽 반 바퀴만 보임)
        render(sc, "c3orb", f)


def bake_c3shard():
    """3장 적 산탄 (반복 16장): 오른쪽(+X)으로 뾰족한 장미빛 유리 조각이 진행 축으로 구르며 날아감"""
    sc = reset(); ink(sc, 1.6)
    m = toon_mat("c3shard", C3_GEM)
    sh = shard_obj("shard", m, 1.3, 0.7, 0.3)
    gm = toon_mat("c3shard_tw", C3_GLASS)
    tw = star_obj("tw", gm, 4, 0.26, 0.05, 0.04)
    tw.location = (1.05, -0.3, 0.12)
    for f in range(FRAMES):
        t = f / FRAMES
        sh.rotation_euler = (TAU / 6 * t, 0, 0)                 # 육각이라 60°에 이어짐
        k = 0.6 + 0.4 * math.sin(TAU * t)
        tw.scale = (k, k, k)
        render(sc, "c3shard", f)


def bake_c3spark():
    """3장 탄 맞음 · 튕김 불똥 (한 번 16장): 흰 4갈래 섬광이 번쩍 → 유리 조각(분홍 · 하늘빛 번갈아)이 돌며 사방으로 튀고 가는 고리가 퍼짐"""
    sc = reset(); ink(sc, 1.4)
    rnd = random.Random(41)
    cm = toon_mat("c3flash", C3_GLASS)
    core = star_obj("core", cm, 4, 0.9, 0.14, 0.08)
    rm = toon_mat("c3sring", C3_GLASS)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.025, major_segments=64, minor_segments=8)
    ring = bpy.context.active_object
    ring.data.materials.append(rm)
    ring.rotation_euler = (math.pi / 2, 0, 0)
    shards = []
    for i in range(10):
        m = toon_mat(f"c3sh{i}", C3_GEM if i % 2 == 0 else C3_GLASS)
        o = shard_obj(f"sh{i}", m, 0.32, 0.18, 0.08)
        a = TAU * i / 10 + rnd.uniform(-0.25, 0.25)
        o["a"] = a; o["d"] = rnd.uniform(0.9, 1.35); o["lag"] = rnd.uniform(0, 0.1); o["spin"] = rnd.uniform(4, 9) * (1 if i % 2 else -1)
        shards.append((o, m))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        pop = math.sin(min(1, t / 0.1) * math.pi / 2) * max(0.0, 1 - max(0, t - 0.1) / 0.3)
        core.scale = (pop, pop, pop)
        core.rotation_euler = (0, 0.4 + t * 0.8, 0)
        core.hide_render = pop <= 0.01
        setv(cm, "heat", 1 - t)
        k = ease_out(t, 3)
        s = 0.3 + 2.0 * k
        ring.scale = (s, s, s)
        ring.hide_render = t >= 0.6
        setv(rm, "dissolve", max(0, (t - 0.3) / 0.3) * 0.6)
        for o, m in shards:
            kk = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            r = o["d"] * ease_out(kk, 2)
            o.location = (math.cos(o["a"]) * r, 0, math.sin(o["a"]) * r - 0.25 * kk * kk)   # 살짝 떨어짐
            o.rotation_euler = (o["spin"] * kk, -o["a"] + 0.6, o["spin"] * kk * 0.5)
            sc_ = max(0.001, 1 - max(0, (kk - 0.55) / 0.45))
            o.scale = (sc_, sc_, sc_)
            o.hide_render = sc_ <= 0.01
            setv(m, "heat", 1 - 0.5 * kk)
        render(sc, "c3spark", f)


C3_WATER = [(0.0, (0.94, 1, 1)), (0.06, (0.62, 0.95, 1)), (0.2, (0.3, 0.82, 0.96)), (0.42, (0.12, 0.6, 0.86)), (0.7, (0.05, 0.36, 0.64))]


def drop_obj(name, mat, r=0.5):
    """물방울: 매끈한 공 (속도 방향으로 늘려 씀)"""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=r)
    bm.to_mesh(me); bm.free()
    for p_ in me.polygons:
        p_.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bake_c3water():
    """3장 분수 샘 물 폭발 (한 번 16장, 2026-11-18 사용자: 물기둥 지속 말고 물이 한 번 터지게):
    밑에서 물기둥이 펑 솟구쳐 꼭대기가 부풀고 → 물방울 왕관이 사방으로 튀어 포물선으로 떨어지고 → 밑동 물보라가 옆으로 퍼지며 녹아 사라짐"""
    sc = reset(); set_line_color(sc, (0.03, 0.2, 0.32))
    sc.render.line_thickness = 1.8
    for s_ in sc.view_layers[0].freestyle_settings.linesets: s_.linestyle.thickness = 1.8
    sc.camera.data.ortho_scale = 3.4
    rnd = random.Random(61)
    G = -1.0                                                     # 0~1 시간 동안의 중력 (캔버스 단위)
    BASE = -1.45
    # 물기둥: 위로 쌓인 덩어리들이 차례로 솟음
    col = []
    for i in range(8):
        m = toon_mat(f"wc{i}", C3_WATER)
        o = lumpy(f"wc{i}", 70 + i, m)
        o["h"] = 0.2 + i * 0.27; o["s"] = 0.95 - i * 0.05; o["x"] = rnd.uniform(-0.1, 0.1)
        col.append((o, m))
    # 물방울 왕관: 꼭대기에서 바깥 · 위로 튀어 떨어짐
    drops = []
    for i in range(24):
        m = toon_mat(f"wd{i}", C3_WATER)
        o = drop_obj(f"wd{i}", m)
        a = (i / 24) * math.pi + rnd.uniform(-0.1, 0.1)            # 위쪽 반원으로 퍼짐
        sp = rnd.uniform(1.4, 2.2)
        o["v"] = (math.cos(a) * sp, rnd.uniform(-0.4, 0.4), abs(math.sin(a)) * sp * 0.75 + 0.5)
        o["z0"] = rnd.uniform(0.6, 1.0); o["s"] = rnd.uniform(0.13, 0.24); o["lag"] = 0.14 + rnd.uniform(0, 0.12)
        drops.append((o, m))
    # 밑동 물보라: 옆으로 납작하게 퍼지는 덩어리
    base = []
    for i in range(8):
        m = toon_mat(f"wb{i}", C3_WATER)
        o = lumpy(f"wb{i}", 90 + i, m)
        side = -1 if i % 2 == 0 else 1
        o["d"] = side * rnd.uniform(0.45, 1.4); o["s"] = rnd.uniform(0.4, 0.62); o["lag"] = rnd.uniform(0.02, 0.12)
        base.append((o, m))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        # 물기둥: 0~0.25 솟음, 0.25~0.55 꼭대기 부풀며 버팀, 뒤로 무너져 녹음
        rise = ease_out(min(1, t / 0.25), 3)
        fall = max(0.0, (t - 0.45) / 0.55)
        for i, (o, m) in enumerate(col):
            top = i == len(col) - 1
            z = BASE + o["h"] * rise * (1 - 0.55 * fall * fall)
            o.location = (o["x"] + math.sin(t * 9 + i) * 0.04, 0, z)
            bulge = 1 + (0.7 if top else 0.15) * math.sin(min(1, t / 0.4) * math.pi)
            w = o["s"] * bulge * (1 - fall) ** 1.2
            o.scale = (w * (1.5 if top else 1), w, w * (0.75 if top else 1.25 - 0.4 * fall))   # 꼭대기는 넓적한 버섯 물보라
            o.hide_render = rise <= 0.02 or w <= 0.01
            setv(m, "heat", 0.8 - 0.4 * t)
            setv(m, "dissolve", min(0.55, max(0, (t - 0.5) / 0.5)))
        # 물방울: 포물선 + 속도 방향으로 늘어남
        for o, m in drops:
            k = (t - o["lag"]) / (1 - o["lag"])
            if k <= 0:
                o.hide_render = True
                continue
            vx, vy, vz = o["v"]
            x, y, z = vx * k * 0.8, vy * k * 0.5, BASE + 0.2 + 0.27 * 7 * o["z0"] + vz * k + G * 2.2 * k * k
            o.location = (x, y, z)
            vel = Vector((vx, vy, vz + 2 * G * 2.2 * k))
            o.rotation_euler = vel.to_track_quat('Z', 'Y').to_euler()
            sc_ = o["s"] * (1 - max(0, (k - 0.7) / 0.3))
            o.scale = (sc_, sc_, sc_ * (1.6 + 0.6 * min(1, vel.length / 2)))
            o.hide_render = sc_ <= 0.005 or z < BASE - 0.1
            setv(m, "heat", 0.85 - 0.4 * k)
        # 밑동 물보라
        for o, m in base:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            o.location = (o["d"] * ease_out(k, 2), 0, BASE + 0.1 + 0.15 * math.sin(min(1, k * 2) * math.pi))
            fade = max(0.0, (k - 0.5) / 0.5)
            w = o["s"] * ease_out(min(1, k / 0.2)) * (1 - fade) ** 1.3
            o.scale = (w * 1.6, w, w * 0.7)
            o.hide_render = w <= 0.01
            setv(m, "dissolve", min(0.55, fade))
        render(sc, "c3water", f)


SETS = {"fire": bake_fire, "smoke": bake_smoke, "spark": bake_spark, "mist": bake_mist, "dodge": bake_dodge, "memclock": bake_memclock, "memshatter": bake_memshatter,
        "c3orb": bake_c3orb, "c3shard": bake_c3shard, "c3spark": bake_c3spark, "c3water": bake_c3water}
only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SETS)
for n in only:
    SETS[n]()
    print("BAKED", n)
print("DONE")
