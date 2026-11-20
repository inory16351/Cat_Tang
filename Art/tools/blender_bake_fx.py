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


def toon_mat(name, bands, light=0.0):
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
    if light > 0:
        # 위쪽 빛: (1 - N.z) / 2 × light 를 더함 → 위 면은 심지 쪽 색, 아래 면은 테 쪽 색 (돌진 꼬리 입체감)
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        sep = nt.nodes.new("ShaderNodeSeparateXYZ")
        nt.links.new(geo.outputs["Normal"], sep.inputs[0])
        down = nt.nodes.new("ShaderNodeMath"); down.operation = 'MULTIPLY_ADD'
        down.inputs[1].default_value = -0.5 * light; down.inputs[2].default_value = 0.5 * light
        nt.links.new(sep.outputs["Z"], down.inputs[0])
        add2 = nt.nodes.new("ShaderNodeMath"); add2.operation = 'ADD'; add2.use_clamp = True
        nt.links.new(add.outputs[0], add2.inputs[0]); nt.links.new(down.outputs[0], add2.inputs[1])
        nt.links.new(add2.outputs[0], ramp.inputs["Fac"])
    else:
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


# ---------- 4장 (시계 장치 속, 느와르) 일반 몬스터 탄 · 불똥 (2026-11-19 사용자: 흑백 대비, 적 공격 궤적은 빨간 계열, 이미지 + 블렌더로) ----------
# 빨강 계열: 정면 = 흰 심지 → 연분홍 → 새빨강 → 짙은 핏빛 테, 금속 = 흰 → 은 → 강철, 잉크 = 거의 검정
C4_RED = [(0.0, (1, 0.96, 0.95)), (0.05, (1, 0.62, 0.58)), (0.16, (1, 0.12, 0.12)), (0.36, (0.7, 0.03, 0.05)), (0.62, (0.32, 0.0, 0.02))]
C4_STEEL = [(0.0, (1, 1, 1)), (0.18, (0.86, 0.86, 0.9)), (0.45, (0.55, 0.55, 0.6)), (0.72, (0.22, 0.22, 0.26))]
C4_INK = (0.02, 0.02, 0.03)


def ink4(sc, thick):
    set_line_color(sc, C4_INK)
    sc.render.line_thickness = thick
    for s_ in sc.view_layers[0].freestyle_settings.linesets:
        s_.linestyle.thickness = thick


def laser_rod(name, mat, length, r, tip=0.25, sides=8):
    """가는 레이저 막대: +X로 뾰족, 뒤도 뾰족 (원기둥 + 양끝 뿔). 정면 = 흰 심지, 가장자리 = 빨강 (toon facing)"""
    verts, faces = [(length / 2 + tip, 0, 0), (-length / 2 - tip, 0, 0)], []
    for x in (length / 2, -length / 2):
        for k in range(sides):
            a = TAU * k / sides
            verts.append((x, math.cos(a) * r, math.sin(a) * r))
    for k in range(sides):
        a0, a1 = 2 + k, 2 + (k + 1) % sides
        b0, b1 = 2 + sides + k, 2 + sides + (k + 1) % sides
        faces += [(0, a1, a0), (a0, a1, b1, b0), (1, b0, b1)]
    ob = mesh_obj(name, mat, verts, faces, smooth=True)
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    return ob


def bake_c4orb():
    """4장 적 구슬 탄 = 레이저 구슬 (반복 16장): 흰 심지 빨간 에너지 구슬 + 엇갈린 가는 레이저 고리 2개가 돌고, 4갈래 은빛 빛살"""
    sc = reset(); ink4(sc, 1.8)
    om = toon_mat("c4orb", C4_RED)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, segments=24, ring_count=12)
    orb = bpy.context.active_object; orb.data.materials.append(om)
    bpy.ops.object.shade_smooth()
    rings = []
    for i in range(2):
        rm = toon_mat(f"c4ring{i}", C4_RED)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.82, minor_radius=0.035, major_segments=64, minor_segments=8)
        rg = bpy.context.active_object; rg.data.materials.append(rm)
        rings.append((rg, rm, i))
    gm = toon_mat("c4glint", C4_STEEL)
    glint = star_obj("glint", gm, 4, 1.35, 0.05, 0.03)
    for f in range(FRAMES):
        t = f / FRAMES
        setv(om, "heat", 0.78)
        for rg, rm, i in rings:
            rg.rotation_euler = (math.pi / 2 - 1.1 + 0.5 * i, TAU * t * (1 if i else -1) * 0.5 + 1.2 * i, 0.4 * i)
            setv(rm, "heat", 0.72)
        k = 0.8 + 0.2 * math.sin(TAU * 2 * t)
        glint.scale = (k, k, k)
        glint.rotation_euler = (0, TAU / 4 * t, 0)
        render(sc, "c4orb", f)


def bake_c4needle():
    """4장 적 바늘 · 직선 탄 = 레이저 볼트 (반복 16장): +X로 뾰족한 가는 레이저 막대 + 뒤로 끌리는 속도선 3가닥"""
    sc = reset(); ink4(sc, 1.6)
    m = toon_mat("c4bolt", C4_RED)
    bolt = laser_rod("bolt", m, 1.5, 0.11, 0.3)
    bolt.location = (0.35, 0, 0)
    streaks = []
    for i, (y, L) in enumerate(((0.0, 1.1), (0.16, 0.7), (-0.16, 0.55))):
        sm = toon_mat(f"c4st{i}", C4_RED)
        s = laser_rod(f"st{i}", sm, L, 0.035, 0.1, 6)
        s.location = (-0.75 - L / 2 + (0.35 if i == 0 else 0), 0, y)
        streaks.append((s, sm, i))
    for f in range(FRAMES):
        t = f / FRAMES
        setv(m, "heat", 0.8)
        bolt.rotation_euler = (TAU / 8 * t, 0, 0)
        for s, sm, i in streaks:
            ph = (t * 2 + i / 3) % 1
            s.scale = (0.6 + 0.4 * (1 - ph), 1, 1)
            setv(sm, "heat", 0.55)
            setv(sm, "dissolve", 0.25 * ph)
        render(sc, "c4needle", f)


def bake_c4spark():
    """4장 탄 맞음 · 튕김 = 레이저 섬광 (한 번 16장): 흰 4갈래 섬광 → 가는 레이저 가시 8개가 쭉 뻗었다 가늘어지고, 빨간 고리 · 작은 불똥"""
    sc = reset(); ink4(sc, 1.4)
    rnd = random.Random(44)
    cm = toon_mat("c4flash", C4_STEEL)
    core = star_obj("core", cm, 4, 0.95, 0.12, 0.07)
    rm = toon_mat("c4sring", C4_RED)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.03, major_segments=64, minor_segments=8)
    ring = bpy.context.active_object; ring.data.materials.append(rm)
    ring.rotation_euler = (math.pi / 2, 0, 0)
    spikes = []
    for i in range(8):
        m = toon_mat(f"c4sp{i}", C4_RED)
        o = laser_rod(f"sp{i}", m, 1.0, 0.045, 0.2, 6)
        o["a"] = TAU * i / 8 + rnd.uniform(-0.15, 0.15); o["L"] = rnd.uniform(0.8, 1.25)
        spikes.append((o, m))
    bits = []
    for i in range(8):
        m = toon_mat(f"c4b{i}", C4_RED)
        o = gem_obj(f"b{i}", m, 0.06)
        o["a"] = rnd.uniform(0, TAU); o["d"] = rnd.uniform(0.7, 1.3)
        bits.append((o, m))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        pop = math.sin(min(1, t / 0.1) * math.pi / 2) * max(0.0, 1 - max(0, t - 0.1) / 0.3)
        core.scale = (pop, pop, pop); core.hide_render = pop <= 0.01
        core.rotation_euler = (0, 0.4 + t * 0.8, 0)
        s = 0.3 + 2.1 * ease_out(t, 3)
        ring.scale = (s, s, s); ring.hide_render = t >= 0.55
        setv(rm, "heat", 0.6); setv(rm, "dissolve", max(0, (t - 0.25) / 0.3) * 0.6)
        for o, m in spikes:
            grow = ease_out(min(1, t / 0.25), 3)
            thin = max(0.001, 1 - max(0, (t - 0.2) / 0.5))
            L = o["L"] * grow
            o.location = (math.cos(o["a"]) * (0.25 + L / 2), 0, math.sin(o["a"]) * (0.25 + L / 2))
            o.rotation_euler = (0, -o["a"], 0)
            o.scale = (max(0.001, L), thin, thin)
            o.hide_render = thin <= 0.01 or L <= 0.01
            setv(m, "heat", 0.75 - 0.25 * t)
        for o, m in bits:
            r = o["d"] * ease_out(t, 2)
            o.location = (math.cos(o["a"]) * r, 0, math.sin(o["a"]) * r - 0.2 * t * t)
            sc_ = max(0.001, 1 - max(0, (t - 0.5) / 0.5)); o.scale = (sc_, sc_, sc_); o.hide_render = sc_ <= 0.01
            setv(m, "heat", 0.65)
        render(sc, "c4spark", f)


def ribbon_obj(name, mat, pts, widths, notch=0.6, y=0.0):
    """카메라를 향한 평평한 리본 띠 (XZ 평면). pts = 머리 → 꼬리 순 점들, widths = 점마다 폭.
    꼬리 끝은 리본 꼬리(제비꼬리, V자로 파임): 마지막 점 양 끝이 뒤로 뻗고 가운데가 notch × 폭만큼 앞으로 파임"""
    n = len(pts)
    verts, faces = [], []
    def side(i):
        a = pts[max(0, i - 1)]; b = pts[min(n - 1, i + 1)]
        dx, dz = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dz) or 1.0
        return (-dz / L, dx / L), (dx / L, dz / L)
    for i, (p, w) in enumerate(zip(pts, widths)):
        (sx, sz), _ = side(i)
        verts.append((p[0] + sx * w / 2, y, p[1] + sz * w / 2))
        verts.append((p[0] - sx * w / 2, y, p[1] - sz * w / 2))
    for i in range(n - 1):
        faces.append((2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2))
    # 꼬리 V 파임: 마지막 두 꼭짓점 사이 가운데 점을 앞(머리 쪽)으로 당김
    (sx, sz), (fx, fz) = side(n - 1)
    p, w = pts[-1], widths[-1]
    verts.append((p[0] - fx * w * notch, y, p[1] - fz * w * notch))
    c = len(verts) - 1
    faces[-1] = (2 * (n - 2), 2 * (n - 2) + 1, 2 * (n - 1) + 1, c, 2 * (n - 1))
    return mesh_obj(name, mat, verts, faces)


def flat_mat(name, rgb):
    """한 색 평면 재질 (리본 띠: 정면 = 카메라라 facing 띠가 안 갈리므로 층을 겹쳐 색 띠를 만듦)"""
    return toon_mat(name, [(0.0, rgb), (1.0, rgb)])


def ribbon_layers(name, pts, widths, notch=0.6):
    """레이저 리본 3겹: 바깥 핏빛 테 → 새빨강 → 흰 심지 (앞으로 갈수록 좁음, 잉크는 바깥 테에만 걸림)"""
    obs = []
    for k, (rgb, wk, dy) in enumerate((((0.42, 0.0, 0.03), 1.0, 0.0), ((1.0, 0.1, 0.12), 0.72, -0.01), ((1.0, 0.97, 0.95), 0.3, -0.02))):
        m = flat_mat(f"{name}_m{k}", rgb)
        obs.append((ribbon_obj(f"{name}{k}", m, pts, [w * wk for w in widths], notch, dy), m))
    return obs


def bake_c4slash():
    """4장 근접 베기 = 칼끝이 그리는 레이저 리본 (한 번 16장, 카메라를 향한 XZ 면 = 게임에선 바닥 쪽으로 눕힘):
    칼끝(은빛 별)이 120° 호를 쭉 그으며 레이저 리본을 남기고, 리본 꼬리(제비꼬리)가 칼끝을 따라오며 줄어 사라짐. 가운데 = 휘두르는 몸, +X = 정면"""
    sc = reset(); ink4(sc, 1.4)
    R = 1.25
    hm = toon_mat("c4head", C4_STEEL)
    head = star_obj("head", hm, 4, 0.35, 0.06, 0.04)
    head.location.y = -0.05
    live = []
    for f in range(FRAMES):
        for o, m in live:
            bpy.data.objects.remove(o, do_unlink=True)
        live = []
        t = f / (FRAMES - 1)
        sweep = ease_out(min(1, t / 0.3), 2)                  # 칼끝 0~1
        tail = ease_out(max(0, (t - 0.22) / 0.62), 2)         # 꼬리가 따라옴
        a0, a1 = -60 + 120 * tail, -60 + 120 * sweep
        if a1 - a0 > 2:
            N = 24
            pts = [(math.cos(math.radians(a1 - (a1 - a0) * i / (N - 1))) * R, math.sin(math.radians(a1 - (a1 - a0) * i / (N - 1))) * R) for i in range(N)]
            thick = 0.36 * max(0.35, 1 - max(0, t - 0.3) / 0.7)
            widths = [thick * (1.0 - 0.35 * i / (N - 1)) for i in range(N)]   # 칼끝 쪽이 두껍고 꼬리로 갈수록 살짝 좁음
            live = ribbon_layers(f"arc{f}_", pts, widths, 0.75)
        a = math.radians(a1)
        head.location.x, head.location.z = math.cos(a) * R, math.sin(a) * R
        k = 1.0 if t < 0.32 else max(0.001, 1 - (t - 0.32) / 0.15)
        head.scale = (k, k, k); head.hide_render = k <= 0.01
        render(sc, "c4slash", f)


def bake_c4strip():
    """이동 궤적 텍스처 (1장, 눈빛 꼬리 · 돌진 자국): 가로 리본 — 왼쪽(U=0) = 머리 둥근 끝, 오른쪽(U=1) = 리본 꼬리(제비꼬리).
    흰 심지 · 새빨강 · 핏빛 테 · 잉크선. TrailRenderer는 머리 → 꼬리로 U 0 → 1이라 꼬리 끝이 늘 리본 꼬리로 끝남"""
    sc = reset(); ink4(sc, 1.2)
    N = 20
    pts = [(-1.5 + 3.0 * i / (N - 1), 0.0) for i in range(N)]
    widths = [0.62 * (1.0 - 0.25 * i / (N - 1)) for i in range(N)]
    ribbon_layers("strip", pts, widths, 0.9)
    render(sc, "c4strip", 0)


# ---------- 4장 톱니 바닥 맞물림: 고철 불꽃 (2026-11-19 사용자: 기존 불똥이 이상함 → 쇳물 · 고철이 튀는 느낌 확실하게, 제철소 불꽃 사진 참고) ----------
# 흰 심지 → 노랑 → 주황 → 검붉은 꼬리. 불티는 속도 방향으로 길게 늘어진 가는 막대(움직임 줄), 포물선으로 떨어지며 식음. 고철 조각은 식은 쇠 + 달군 테
HOT = [(0.0, (1.0, 0.9, 0.55)), (0.18, (1.0, 0.66, 0.16)), (0.45, (1.0, 0.4, 0.05)), (0.75, (0.66, 0.12, 0.02))]   # 사용자: 주황색으로 (흰 심지 줄임)
SCRAP = [(0.0, (0.42, 0.4, 0.38)), (0.5, (0.2, 0.19, 0.19)), (0.78, (1.0, 0.45, 0.08))]


# 분수 불티 (2026-11-19 사용자: 아래는 흰색으로 시작해 올라가면서 주황으로): 식어도 검게 되지 않고 주황에서 멈춤
GRIND = [(0.0, (1.0, 1.0, 0.96)), (0.22, (1.0, 0.88, 0.5)), (0.42, (1.0, 0.6, 0.12)), (0.66, (1.0, 0.42, 0.04))]


def _spark_rod(name, rnd, bands=None):
    m = toon_mat(name, bands or HOT)
    o = laser_rod(name, m, 1.0, 0.026, 0.16, 6)   # 가는 불티 (사용자: 너무 크지 않게)
    return o, m


def _place_rod(o, pos, vel, length, thin):
    """막대(+X 축)를 속도 방향으로 눕혀 머리가 pos, 꼬리가 뒤로"""
    v = Vector(vel)
    if v.length < 1e-4:
        v = Vector((0, 0, 1))
    d = v.normalized()
    o.location = Vector(pos) - d * (length / 2)
    o.rotation_euler = (0, -math.atan2(d.z, d.x), 0)
    o.scale = (max(0.001, length), thin, thin)


def bake_c4grind():
    """맞물린 이빨 자리에서 계속 뿜는 고철 불꽃 분수 (반복 16장): 바닥 한 점(아래 가운데)에서 불티 60개가 위 · 양옆으로 뿜어져 포물선으로 떨어짐 + 굴러 떨어지는 쇳조각 + 달군 바닥 빛"""
    sc = reset(); ink4(sc, 0.55)   # 불티가 잉크선에 묻혀 검게 보이지 않게 얇게
    sc.camera.data.ortho_scale = 4.2
    rnd = random.Random(71)
    src = Vector((0, 0, -1.55))
    g = -6.5
    sparks = []
    for i in range(60):
        o, m = _spark_rod(f"gs{i}", rnd, GRIND)
        a = rnd.uniform(-1.15, 1.15) + (0.35 if rnd.random() < 0.5 else -0.35) * rnd.random()   # 위를 중심으로 양옆으로 넓게
        sp = rnd.uniform(3.0, 5.6)
        o["vx"] = math.sin(a) * sp; o["vz"] = math.cos(a) * sp
        o["ph"] = rnd.random(); o["life"] = rnd.uniform(0.55, 1.0)
        sparks.append((o, m))
    chunks = []
    for i in range(6):
        m = toon_mat(f"gc{i}", SCRAP)
        o = gear_obj(f"gc{i}", m, teeth=rnd.choice((5, 6, 7)), r=rnd.uniform(0.07, 0.11), t=0.04) if i % 2 == 0 else gem_obj(f"gc{i}", m, rnd.uniform(0.06, 0.09))
        a = rnd.uniform(-0.9, 0.9)
        o["vx"] = math.sin(a) * rnd.uniform(1.6, 2.6); o["vz"] = math.cos(a) * rnd.uniform(2.6, 3.6); o["ph"] = rnd.random(); o["spin"] = rnd.uniform(-9, 9)
        chunks.append((o, m))
    gm = toon_mat("gglow", GRIND)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.42, segments=20, ring_count=10)
    glow = bpy.context.active_object; glow.data.materials.append(gm); glow.scale = (1.0, 0.4, 0.42); glow.location = src
    flash = star_obj("gflash", toon_mat("gfl", GRIND), 4, 0.75, 0.1, 0.05); flash.location = src + Vector((0, -0.05, 0.08))
    for f in range(FRAMES):
        t = f / FRAMES
        for o, m in sparks:
            age = ((t + o["ph"]) % 1.0) / o["life"]
            if age >= 1.0:
                o.hide_render = True; continue
            o.hide_render = False
            tt = age * 0.55                                   # 초
            pos = src + Vector((o["vx"] * tt, 0, o["vz"] * tt + 0.5 * g * tt * tt))
            vel = Vector((o["vx"], 0, o["vz"] + g * tt))
            _place_rod(o, pos, vel, 0.12 + 0.07 * vel.length, max(0.05, 1.0 - age))   # 식기보다 가늘어지며 사라짐
            rise = max(0.0, pos.z - src.z)                    # 분출점에서 올라간 높이
            setv(m, "heat", max(0.12, 1.0 - 0.7 * rise - 0.2 * age))   # 아래(분출점) = 흰색 → 올라갈수록 주황
        for o, m in chunks:
            age = (t + o["ph"]) % 1.0
            tt = age * 0.6
            o.location = src + Vector((o["vx"] * tt, -0.1, o["vz"] * tt + 0.5 * g * tt * tt))
            o.rotation_euler = (0, o["spin"] * tt, 0)   # 톱니 · 볼트 면이 카메라(-Y)를 본 채 화면 안에서 돎
            setv(m, "heat", 0.6 - 0.4 * age)
        k = 0.85 + 0.15 * math.sin(TAU * 3 * t)
        glow.scale = (1.0 * k, 0.4, 0.42 * k); setv(gm, "heat", 0.95)
        fk = 0.6 + 0.4 * abs(math.sin(TAU * 2 * t + 0.7)); flash.scale = (fk, fk, fk); flash.rotation_euler = (0, 0.5 * t, 0)
        render(sc, "c4grind", f)


def bake_c4scrap():
    """누가 이빨에 끼었을 때 고철 폭발 (한 번 16장): 흰 섬광 → 불티 70개가 사방(위쪽 많이)으로 터져 포물선으로 떨어지며 식음 + 톱니 · 볼트 고철 조각이 돌며 날아감 + 달군 연기 덩어리"""
    sc = reset(); ink4(sc, 0.6)
    sc.camera.data.ortho_scale = 4.6
    rnd = random.Random(83)
    src = Vector((0, 0, -0.4))
    g = -5.5
    sparks = []
    for i in range(70):
        o, m = _spark_rod(f"ss{i}", rnd)
        a = rnd.uniform(-math.pi, math.pi) * (0.55 if rnd.random() < 0.7 else 1.0)   # 70%는 위쪽 반원
        sp = rnd.uniform(3.5, 7.5)
        o["vx"] = math.sin(a) * sp; o["vz"] = math.cos(a) * sp; o["life"] = rnd.uniform(0.6, 1.0)
        sparks.append((o, m))
    chunks = []
    for i in range(10):
        m = toon_mat(f"sc{i}", SCRAP)
        o = gear_obj(f"sc{i}", m, teeth=rnd.choice((6, 8)), r=rnd.uniform(0.1, 0.17), t=0.05) if i % 3 != 2 else box_obj(f"sc{i}", m, -0.05, 0.05, -0.12, 0.12, 0.05)
        a = rnd.uniform(-1.4, 1.4)
        sp = rnd.uniform(2.2, 4.2)
        o["vx"] = math.sin(a) * sp; o["vz"] = math.cos(a) * sp; o["spin"] = rnd.uniform(-14, 14)
        chunks.append((o, m))
    fm = toon_mat("sflash", HOT)
    flash = star_obj("sflash", fm, 6, 1.5, 0.22, 0.08); flash.location = src
    pm = toon_mat("spuff", HOT)
    puff = lumpy("spuff", 5, pm, 3); puff.location = src
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        pop = math.sin(min(1, t / 0.08) * math.pi / 2) * max(0.0, 1 - max(0, t - 0.08) / 0.22)
        flash.scale = (pop, pop, pop); flash.hide_render = pop <= 0.01; flash.rotation_euler = (0, 0.3 + t, 0)
        setv(fm, "heat", 1.0)
        ps = 0.5 + 1.1 * ease_out(t, 2); puff.scale = (ps, ps * 0.6, ps * 0.8); puff.location = src + Vector((0, 0.1, 0.5 * t))
        setv(pm, "heat", max(0.0, 0.9 - 1.2 * t)); setv(pm, "dissolve", min(1.0, max(0.0, (t - 0.15) / 0.5)))
        puff.hide_render = t > 0.42                         # 녹아 사라진 뒤 외곽선 고리만 남지 않게
        for o, m in sparks:
            age = t / o["life"]
            if age >= 1.0:
                o.hide_render = True; continue
            o.hide_render = False
            tt = age * 0.7
            pos = src + Vector((o["vx"] * tt, 0, o["vz"] * tt + 0.5 * g * tt * tt))
            vel = Vector((o["vx"], 0, o["vz"] + g * tt))
            _place_rod(o, pos, vel, 0.15 + 0.08 * vel.length, max(0.05, 1.0 - age))
            setv(m, "heat", 1.0 - 0.5 * age)
        for o, m in chunks:
            tt = t * 0.75
            o.location = src + Vector((o["vx"] * tt, -0.1, o["vz"] * tt + 0.5 * g * tt * tt))
            o.rotation_euler = (0, o["spin"] * tt, 0)   # 톱니 · 볼트 면이 카메라(-Y)를 본 채 화면 안에서 돎
            setv(m, "heat", 0.65 - 0.45 * t)
            o.hide_render = t > 0.92
        render(sc, "c4scrap", f)


# ---------- Chapter 5: cosmic void / broken mirror / gold cracks ----------

def _c5_rgb(hex_rgb):
    """sRGB art colours -> linear emission, for the existing Standard view."""
    rgb = [int(hex_rgb[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in rgb)


def _c5_bands(kind="glass"):
    palettes = {
        "glass": [(0.0, "FFFFFF"), (0.12, "EAD9FF"), (0.32, "FF6FD0"), (0.58, "FF2FB4"), (0.86, "3A1A6A")],
        "void": [(0.0, "24103F"), (0.22, "3A1A6A"), (0.50, "7E237F"), (0.72, "FF2FB4"), (0.93, "FF6FD0")],
        "white": [(0.0, "FFFFFF"), (0.35, "FFF3FF"), (0.65, "EAD9FF"), (0.90, "FF6FD0")],
        "gold": [(0.0, "FFF7DB"), (0.18, "FFE8A0"), (0.42, "FFC64A"), (0.82, "B77722")],
        "pink": [(0.0, "FFFFFF"), (0.08, "FFEDF9"), (0.22, "FF6FD0"), (0.45, "FF2FB4"), (0.90, "3A1A6A")],
    }
    return [(p, _c5_rgb(c)) for p, c in palettes[kind]]


def _c5_ring(name, mat, radius, width):
    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=width, major_segments=64, minor_segments=8)
    ob = bpy.context.active_object
    ob.name = name; ob.data.materials.append(mat)
    ob.rotation_euler = (math.pi / 2, 0, 0)
    return ob


def _c5_segment(ob, start, end):
    """Place a unit laser rod between two points (including its pointed caps)."""
    a, b = Vector(start), Vector(end)
    ob.location = (a + b) / 2
    ob.rotation_euler = (b - a).to_track_quat('X', 'Y').to_euler()
    ob.scale.x = max(0.001, (b - a).length / 1.04)


def bake_c5orb():
    """16-frame loop: dark faceted core, pulsing magenta rim, gold orbit and three glass chips."""
    from mathutils import Quaternion
    sc = reset(); ink4(sc, 1.3)
    vm = toon_mat("c5void", _c5_bands("void"))
    core = gem_obj("void_core", vm, 0.65)
    core.rotation_mode = 'QUATERNION'
    tilt = Quaternion((1, 0, 0), 0.28)
    gm = toon_mat("c5orbit_gold", _c5_bands("gold"))
    ring = _c5_ring("gold_orbit", gm, 0.98, 0.023)
    chips = []
    for i in range(3):
        m = toon_mat(f"c5orb_chip{i}", _c5_bands("white"))
        chips.append(shard_obj(f"orbit_glass{i}", m, 0.11, 0.07, 0.045, 3))
    for f in range(FRAMES):
        t = f / FRAMES  # All motions are periodic at t=1, without a duplicated endpoint.
        core.rotation_quaternion = tilt @ Quaternion((0, 0, 1), TAU / 5 * t)
        setv(vm, "heat", 0.97 + 0.03 * math.cos(TAU * t))
        ring.rotation_euler = (math.pi / 3, 0.18 * math.sin(TAU * t), 0.12 * math.sin(TAU * t))
        orbit = ring.rotation_euler.to_matrix()
        for i, chip in enumerate(chips):
            a = TAU * (t + i / 3)
            chip.location = orbit @ Vector((1.02 * math.cos(a), 1.02 * math.sin(a), 0))
            chip.rotation_euler = (TAU * t, -a, 0.2)
        render(sc, "c5orb", f)


def bake_c5shard():
    """16-frame loop: +X crystal splinter, white cutting edge and two chips behind it."""
    sc = reset(); ink4(sc, 1.2)
    m = toon_mat("c5splinter", _c5_bands())
    setv(m, "heat", 0.80)
    sh = shard_obj("void_splinter", m, 1.10, 0.58, 0.27)
    sh.location.x = 0.13
    wm = toon_mat("c5splinter_edge", _c5_bands("white"))
    edge = laser_rod("white_hot_edge", wm, 1.0, 0.014, 0.02, 6)
    edge.parent = sh
    _c5_segment(edge, (0.12, -0.275, 0), (1.10, 0, 0))
    chips = []
    for i in range(2):
        cm = toon_mat(f"c5trail_glass{i}", _c5_bands("white"))
        o = shard_obj(f"trailing_chip{i}", cm, 0.13, 0.09, 0.05, 3)
        chips.append(o)
    for f in range(FRAMES):
        t = f / FRAMES
        sh.rotation_euler = (TAU * t, 0, 0)
        for i, chip in enumerate(chips):
            a = TAU * t + i * math.pi
            chip.location = (-0.83 - i * 0.30, -0.02, (0.15 if i == 0 else -0.16) + 0.045 * math.sin(a))
            chip.rotation_euler = (TAU * t, 0.15 * math.sin(a), 0)
        render(sc, "c5shard", f)


def bake_c5spark():
    """One shot: frames 0-2 flash, eight triangular mirror chips, magenta ring and six gold sparks."""
    sc = reset(); ink4(sc, 1.0)
    rnd = random.Random(505)
    fm = toon_mat("c5hit_flash", _c5_bands("pink"))
    flash = star_obj("white_magenta_flash", fm, 4, 0.91, 0.14, 0.07)
    rim = star_obj("magenta_flash_rim", flat_mat("c5flash_rim", _c5_rgb("FF2FB4")), 4, 1.03, 0.19, 0.06)
    rim.location.y = 0.10
    rm = toon_mat("c5hit_ring", _c5_bands("pink"))
    ring = _c5_ring("magenta_shock", rm, 0.5, 0.022)
    shards, sparks = [], []
    for i in range(8):
        m = toon_mat(f"c5hit_glass{i}", _c5_bands("white" if i % 3 == 0 else "glass"))
        setv(m, "heat", 1.0 if i % 3 == 0 else 0.82)
        ob = shard_obj(f"triangular_glass{i}", m, rnd.uniform(0.19, 0.28), 0.10, rnd.uniform(0.065, 0.10), 3)
        shards.append((ob, m, TAU * i / 8 + rnd.uniform(-0.15, 0.15), rnd.uniform(0.90, 1.20), rnd.uniform(4, 8)))
    for i in range(6):
        m = toon_mat(f"c5hit_gold{i}", _c5_bands("gold"))
        setv(m, "heat", 0.73)
        ob = laser_rod(f"gold_spark{i}", m, 0.23, 0.018, 0.05, 6)
        sparks.append((ob, m, TAU * (i + 0.35) / 6))
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        pop = (0.75, 1.0, 0.48)[min(f, 2)]
        flash.scale = (pop,) * 3; flash.hide_render = f > 2
        rim.scale = (pop,) * 3; rim.hide_render = f > 2
        setv(fm, "heat", 0.97)
        k = max(0.0, (t - 0.10) / 0.90)
        ring.scale = (0.25 + 2.30 * ease_out(k, 2),) * 3
        ring.hide_render = f < 2 or f >= 12
        setv(rm, "heat", 0.78); setv(rm, "dissolve", min(0.50, max(0, (k - 0.45) / 0.45)))
        for ob, m, a, distance, spin in shards:
            r = 0.14 + distance * ease_out(k, 2)
            ob.location = (math.cos(a) * r, 0, math.sin(a) * r - 0.10 * k * k)
            ob.rotation_euler = (spin * k, -a + spin * k * 0.6, 0.25)
            s = max(0.001, 1 - max(0, (k - 0.48) / 0.52))
            ob.scale = (s,) * 3; ob.hide_render = f < 2 or f == FRAMES - 1
            setv(m, "dissolve", max(0, (k - 0.55) / 0.45))
        for ob, m, a in sparks:
            r = 0.3 + 0.95 * ease_out(k, 2)
            ob.location = (math.cos(a) * r, -0.02, math.sin(a) * r)
            ob.rotation_euler = (0, -a, 0)
            s = max(0.001, 1 - k)
            ob.scale = (0.4 + 0.6 * s, s, s); ob.hide_render = f < 2 or f >= 14
            setv(m, "dissolve", max(0, (k - 0.55) / 0.45))
        render(sc, "c5spark", f)


def bake_c5rift():
    """Vertical air crack: grow 0-7, violet star lens 8-9, shatter 10-15."""
    sc = reset(); ink4(sc, 0.9)
    rnd = random.Random(515)
    pm = flat_mat("c5crack_magenta", _c5_rgb("FF2FB4"))
    wm = flat_mat("c5crack_white", _c5_rgb("FFF5FF"))
    gm = flat_mat("c5crack_gold", _c5_rgb("FFC64A"))
    paths = [
        [(0, 0), (-0.12, 0.25), (0.10, 0.49), (-0.10, 0.72), (0.07, 0.97), (-0.04, 1.24)],
        [(0, 0), (0.15, -0.24), (-0.08, -0.47), (0.11, -0.73), (-0.06, -0.98), (0.04, -1.24)],
        [(0.10, 0.49), (0.39, 0.60), (0.52, 0.85)],
        [(-0.10, 0.72), (-0.37, 0.80), (-0.46, 0.99)],
        [(-0.08, -0.47), (-0.37, -0.61), (-0.49, -0.83)],
        [(0.11, -0.73), (0.38, -0.80), (0.45, -1.01)],
    ]
    cracks = []
    for p, path in enumerate(paths):
        for j, (a, b) in enumerate(zip(path, path[1:])):
            outer = laser_rod(f"crack_band{p}_{j}", pm, 1, 0.053 if p < 2 else 0.036, 0.02, 6)
            inner = laser_rod(f"crack_core{p}_{j}", gm if p >= 2 and j == 1 else wm, 1, 0.018 if p < 2 else 0.012, 0.02, 6)
            cracks.append((outer, inner, a, b, p))
    lm = toon_mat("c5rift_lens", _c5_bands("void"))
    lens = gem_obj("violet_void_lens", lm, 1.0)
    lens.location.y = 0.12
    stars = []
    sm = toon_mat("c5rift_starlight", _c5_bands("white"))
    for i in range(5):
        ob = star_obj(f"trapped_star{i}", sm, 4, 0.055 if i % 2 else 0.075, 0.012, 0.015)
        ob.location = (rnd.uniform(-0.21, 0.21), -0.34, -0.63 + i * 0.30)
        stars.append(ob)
    shards = []
    for i in range(12):
        m = toon_mat(f"c5rift_glass{i}", _c5_bands("white" if i % 4 == 0 else "glass"))
        setv(m, "heat", 1.0 if i % 4 == 0 else 0.82)
        ob = shard_obj(f"rift_mirror_piece{i}", m, rnd.uniform(0.15, 0.27), 0.10, rnd.uniform(0.065, 0.12), 3)
        shards.append((ob, m, TAU * i / 12 + rnd.uniform(-0.10, 0.10), rnd.uniform(0.85, 1.17), rnd.uniform(3, 7)))
    rm = toon_mat("c5rift_shock", _c5_bands("pink"))
    ring = _c5_ring("rift_shatter_ring", rm, 0.5, 0.022)
    for f in range(FRAMES):
        reach = 0.10 + 1.18 * min(1.0, f / 7)
        shatter = max(0.0, (f - 9) / 6)
        for outer, inner, a, b, p in cracks:
            amount = min(1.0, max(0.0, (reach - abs(a[1])) / max(0.01, abs(b[1]) - abs(a[1]))))
            end = (a[0] + (b[0] - a[0]) * amount, a[1] + (b[1] - a[1]) * amount)
            for ob, depth in ((outer, -0.12), (inner, -0.19)):
                _c5_segment(ob, (a[0], depth, a[1]), (end[0], depth, end[1]))
                ob.scale.y = ob.scale.z = max(0.001, 1 - shatter)
                ob.hide_render = amount <= 0.01 or f >= 14
        for m in (pm, wm, gm):
            setv(m, "dissolve", min(1, shatter * 1.25))
        bulge = 0.82 if f == 8 else 1.0
        lens.scale = (0.44 * bulge, 0.28, 1.10 * bulge)
        lens.rotation_euler = (0, 0, 0.12)
        lens.hide_render = f not in (8, 9)
        for i, star in enumerate(stars):
            star.hide_render = f not in (8, 9)
            star.scale = (0.8 + 0.2 * math.cos(f + i),) * 3
        for ob, m, a, distance, spin in shards:
            r = 0.12 + distance * ease_out(shatter, 2)
            ob.location = (math.cos(a) * r, -0.05, math.sin(a) * r)
            ob.rotation_euler = (spin * shatter, -a + spin * shatter * 0.55, 0.25)
            s = max(0.001, 1 - max(0, (shatter - 0.42) / 0.58))
            ob.scale = (s,) * 3; ob.hide_render = f < 10 or f == 15
            setv(m, "dissolve", max(0, (shatter - 0.48) / 0.52))
        ring.scale = (0.5 + 2.05 * ease_out(shatter, 2),) * 3
        ring.hide_render = f < 10 or f >= 14
        setv(rm, "heat", 0.80); setv(rm, "dissolve", min(0.50, max(0, (shatter - 0.4) / 0.6)))
        render(sc, "c5rift", f)


def bake_c5strip():
    """One frame, matching c4strip: white-hot LEFT head, pink body, violet notched RIGHT tail."""
    sc = reset(); ink4(sc, 1.0)
    n = 32
    # Leave a final segment deeper than the V cut, so earlier faces cannot fill the notch.
    pts = [(-1.40 + 2.34 * i / (n - 2), 0.0) for i in range(n - 1)] + [(1.40, 0.0)]
    widths = [0.62 * (1 - 0.25 * i / (n - 1)) for i in range(n)]
    stops = [(0.0, "FFFFFF"), (0.12, "FFF0FC"), (0.30, "FF6FD0"), (0.60, "FF2FB4"), (1.0, "3A1A6A")]
    for layer, (wk, depth) in enumerate(((1.0, 0.0), (0.73, -0.012), (0.29, -0.024))):
        ob = ribbon_obj(f"c5strip_layer{layer}", flat_mat(f"c5strip_base{layer}", _c5_rgb("3A1A6A")), pts, [w * wk for w in widths], 0.9 / wk, depth)
        for i, face in enumerate(ob.data.polygons):
            u = i / (n - 2)
            for (a, ca), (b, cb) in zip(stops, stops[1:]):
                if a <= u <= b:
                    k = (u - a) / (b - a)
                    rgb = tuple(x * (1 - k) + y * k for x, y in zip(_c5_rgb(ca), _c5_rgb(cb)))
                    break
            if layer == 0:
                rgb = tuple(v * 0.38 for v in rgb)
            elif layer == 1:
                rgb = tuple(v * 0.85 for v in rgb)
            m = flat_mat(f"c5strip_{layer}_{i}", rgb)
            # Gradually translucent tail, with the existing noise dissolve convention.
            nt = m.node_tree
            mix = next(node for node in nt.nodes if node.type == 'MIX_SHADER')
            source = mix.inputs[0].links[0].from_socket
            fade = nt.nodes.new("ShaderNodeMath"); fade.operation = 'MULTIPLY'
            fade.inputs[1].default_value = 1 - 0.72 * u ** 3
            nt.links.new(source, fade.inputs[0]); nt.links.new(fade.outputs[0], mix.inputs[0])
            ob.data.materials.append(m); face.material_index = len(ob.data.materials) - 1
    render(sc, "c5strip", 0)


DUST_BANDS = [(0.0, (0.92, 0.86, 0.77)), (0.1, (0.78, 0.7, 0.6)), (0.3, (0.6, 0.52, 0.43)), (0.58, (0.43, 0.36, 0.3))]
ROCK_BANDS = [(0.0, (0.46, 0.39, 0.32)), (0.3, (0.3, 0.25, 0.21)), (0.65, (0.17, 0.14, 0.12))]


FOOT_RX, FOOT_RY = 0.24, 0.42      # 발바닥 타원 반지름 (게임: HeavyStep이 발 길이에 맞춰 크기를 정함 — 발 길이 0.84 = 크기 1)


def bake_dustring():
    """발 디딤 흙먼지 — 바닥 고리 (한 번 16장, 2026-11-20 사용자: 땅에 닿는 순간 그 자리에서 퍼지게).
    위에서 똑바로 내려다보고 구움 → 게임에선 바닥에 눕힌 판(FxPart.flat)이라 발이 먼지를 제대로 가림. 그림 가운데 = 발바닥 가운데, 위(+Y) = 발끝 방향.
    "한 점이 아니라 발이 닿는 면적에서부터 퍼져야" → 첫 장 = 발바닥 윤곽(가로 0.48 × 세로 0.84 타원) 둘레에 먼지가 눌려 삐져나온 모양,
    그 윤곽에서 바깥으로 빠르게 밀려 퍼지며 부풀었다 녹고, 돌 부스러기가 발 가장자리에서 바닥을 미끄러져 나감"""
    sc = reset(); set_line_color(sc, (0.16, 0.11, 0.08))
    sc.render.line_thickness = 1.6
    cam = sc.camera
    cam.data.ortho_scale = 3.2
    cam.location = (0, 0, 10); cam.rotation_euler = (0, 0, 0)
    rnd = random.Random(21)
    ring, rocks = [], []
    for i in range(13):
        m = toon_mat(f"dust{i}", DUST_BANDS)
        o = lumpy(f"d{i}", 60 + i, m)
        o["a"] = TAU * i / 13 + rnd.uniform(-0.08, 0.08); o["r"] = rnd.uniform(0.62, 0.8); o["s"] = rnd.uniform(0.3, 0.46)   # r = 발 윤곽에서 바깥으로 더 나가는 거리
        o["lag"] = rnd.uniform(0, 0.06); o["spin"] = rnd.uniform(-1.5, 1.5)
        ring.append((o, m))
    for i in range(8):
        m = toon_mat(f"rock{i}", ROCK_BANDS)
        o = lumpy(f"r{i}", 90 + i, m, subdiv=1)
        for p in o.data.polygons:
            p.use_smooth = False
        a = rnd.uniform(0, TAU)
        o["d"] = Vector((math.cos(a), math.sin(a), 0)); o["far"] = rnd.uniform(0.7, 1.3); o["s"] = rnd.uniform(0.09, 0.15); o["spin"] = rnd.uniform(4, 9)
        rocks.append((o, m))
    for f in range(FRAMES):
        t = (f + 1) / FRAMES
        for o, m in ring:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            e = o["r"] * ease_out(k, 3)
            o.location = (math.cos(o["a"]) * (FOOT_RX + e), math.sin(o["a"]) * (FOOT_RY + e), 0)   # 발바닥 타원 윤곽 → 바깥
            fade = min(1.0, max(0.0, (k - 0.6) / 0.4))
            g = o["s"] * (0.6 + 0.4 * ease_out(min(1, k / 0.18))) * (1 + 0.5 * k) * (1 - fade) ** 1.4
            o.scale = (g * 1.05, g * 1.45, g * 0.6)            # 고리 둘레 방향(Y)으로 늘여 이어지게
            o.hide_render = fade > 0.8
            o.rotation_euler = (0, 0, o["a"] + o["spin"] * k)
            setv(m, "heat", 1 - 0.6 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.4) / 0.6)) ** 1.1)
        for o, m in rocks:
            k = min(1.0, t / 0.7)
            dv = Vector(o["d"])
            o.location = Vector((dv.x * FOOT_RX, dv.y * FOOT_RY, 0)) + dv * o["far"] * ease_out(k, 2.5)
            o.scale = (o["s"] * (1 - max(0, (k - 0.85) / 0.15)),) * 3
            o.rotation_euler = (o["spin"] * k, o["spin"] * 0.7 * k, 0)
            o.hide_render = t > 0.72
        render(sc, "dustring", f)


def bake_dustpuff():
    """발 디딤 흙먼지 — 솟는 뭉게 (한 번 16장): 게임 카메라(57° 내려다봄) 각도로 구움, 발 디딘 점 = 그림 가운데 아래(높이 30%).
    디딘 자리에서 뭉게 4개가 낮게 부풀어 오르며 천천히 솟고 녹음 + 돌 부스러기 5개가 포물선으로 튐"""
    sc = reset(); set_line_color(sc, (0.16, 0.11, 0.08))
    sc.render.line_thickness = 1.6
    cam = sc.camera
    cam.data.ortho_scale = 3.0
    pitch = math.radians(57)
    cam.rotation_euler = (math.pi / 2 - pitch, 0, 0)
    fwd = Vector((0, math.cos(pitch), -math.sin(pitch)))
    upv = Vector((0, math.sin(pitch), math.cos(pitch)))           # 카메라 위쪽 (월드)
    cam.location = -fwd * 10 + upv * (0.2 * 3.0)                  # 발 디딘 점이 그림 높이 30%에 오게
    rnd = random.Random(33)
    puffs, rocks = [], []
    for i in range(6):
        m = toon_mat(f"puff{i}", DUST_BANDS)
        o = lumpy(f"p{i}", 120 + i, m)
        a = TAU * i / 6 + rnd.uniform(-0.3, 0.3)
        o["off"] = Vector((math.cos(a) * 0.3, math.sin(a) * 0.3, 0)); o["s"] = rnd.uniform(0.5, 0.66); o["lag"] = rnd.uniform(0, 0.12); o["rise"] = rnd.uniform(0.5, 0.95)
        puffs.append((o, m))
    for i in range(5):
        m = toon_mat(f"prock{i}", ROCK_BANDS)
        o = lumpy(f"pr{i}", 140 + i, m, subdiv=1)
        for p in o.data.polygons:
            p.use_smooth = False
        a = rnd.uniform(0, TAU)
        o["d"] = Vector((math.cos(a), math.sin(a), 0)); o["far"] = rnd.uniform(0.4, 0.8); o["up"] = rnd.uniform(0.5, 0.9)
        o["s"] = rnd.uniform(0.1, 0.15); o["end"] = rnd.uniform(0.45, 0.6); o["spin"] = rnd.uniform(4, 9)
        rocks.append((o, m))
    for f in range(FRAMES):
        t = (f + 1) / FRAMES
        for o, m in puffs:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            o.location = Vector(o["off"]) * (1 + 1.4 * ease_out(k, 2)) + Vector((0, 0, 0.12 + o["rise"] * ease_out(k, 1.6)))
            fade = min(1.0, max(0.0, (k - 0.55) / 0.45))
            g = o["s"] * (0.35 + 0.65 * ease_out(min(1, k / 0.25))) * (1 + 0.45 * k) * (1 - fade) ** 1.5
            o.scale = (g, g, g * 0.85)
            o.hide_render = fade > 0.8
            o.rotation_euler = (0.6 * k, 0.4 * k, 0)
            setv(m, "heat", 1 - 0.55 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.35) / 0.65)))
        for o, m in rocks:
            k = min(1.0, t / o["end"])
            p = Vector(o["d"]) * o["far"] * ease_out(k, 1.6)
            p.z = 0.05 + 4 * o["up"] * k * (1 - k)
            o.location = p
            o.scale = (o["s"],) * 3
            o.rotation_euler = (o["spin"] * k, o["spin"] * 0.7 * k, 0)
            o.hide_render = t >= o["end"]
        render(sc, "dustpuff", f)


def bake_dustclump():
    """발 윤곽 한 점에서 굴러 나가는 흙먼지 덩이 (한 번 16장, 2026-11-20 사용자: 발 콜라이더가 닿는 면적을 입체로 추적해 그 끝점에서부터 퍼지게).
    HeavyStep이 발바닥 윤곽(볼록 껍질) 둘레의 점마다 하나씩 놓고 바깥 방향으로 밀어 보냄(FxPart.drift) — 그림 자체는 제자리에서 낮게 부풀었다 녹음.
    게임 카메라(57°) 각도, 땅에 닿는 점 = 그림 아래 35%. 납작한 덩어리 3개가 눌려 삐져나오듯 옆으로 퍼지고 조금 솟으며, 작은 돌 하나가 튐"""
    sc = reset(); set_line_color(sc, (0.16, 0.11, 0.08))
    sc.render.line_thickness = 1.5
    cam = sc.camera
    cam.data.ortho_scale = 1.6
    pitch = math.radians(57)
    cam.rotation_euler = (math.pi / 2 - pitch, 0, 0)
    fwd = Vector((0, math.cos(pitch), -math.sin(pitch)))
    upv = Vector((0, math.sin(pitch), math.cos(pitch)))
    cam.location = -fwd * 10 + upv * (0.15 * 1.6)                 # 땅 점이 그림 높이 35%에
    rnd = random.Random(44)
    blobs = []
    for i in range(3):
        m = toon_mat(f"clump{i}", DUST_BANDS)
        o = lumpy(f"c{i}", 150 + i, m)
        a = rnd.uniform(0, TAU)
        o["off"] = Vector((math.cos(a) * 0.12, math.sin(a) * 0.1, 0)); o["s"] = rnd.uniform(0.3, 0.4); o["lag"] = 0.05 * i; o["rise"] = rnd.uniform(0.12, 0.3)
        blobs.append((o, m))
    rm = toon_mat("clumprock", ROCK_BANDS)
    rock = lumpy("crock", 160, rm, subdiv=1)
    for p in rock.data.polygons:
        p.use_smooth = False
    for f in range(FRAMES):
        t = (f + 1) / FRAMES
        for o, m in blobs:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            o.location = Vector(o["off"]) * (1 + 1.5 * ease_out(k, 2)) + Vector((0, 0, 0.05 + o["rise"] * ease_out(k, 2)))
            fade = min(1.0, max(0.0, (k - 0.55) / 0.45))
            g = o["s"] * (0.5 + 0.5 * ease_out(min(1, k / 0.2))) * (1 + 0.5 * k) * (1 - fade) ** 1.5
            o.scale = (g * 1.2, g * 1.2, g * 0.7)
            o.hide_render = fade > 0.8
            o.rotation_euler = (0.5 * k, 0.3 * k, 0.8 * k)
            setv(m, "heat", 1 - 0.55 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.35) / 0.65)))
        k = min(1.0, t / 0.5)
        rock.location = (0.35 * ease_out(k, 1.6), -0.1 * k, 0.04 + 1.2 * k * (1 - k))
        rock.scale = (0.07,) * 3
        rock.rotation_euler = (7 * k, 5 * k, 0)
        rock.hide_render = t > 0.5
        render(sc, "dustclump", f)


# ---------- 돌진 꼬리 (2026-11-20 사용자: 돌진 경로 뒤에 몬스터 컨셉에 맞는 이펙트, 블렌더 + 이미지 섞어서) ----------
# 그림 규칙 (Codex 시트 dash_*.png와 같음): 몬스터는 오른쪽(+X)으로 달려 나감 → 꼬리는 오른쪽에서 생겨 왼쪽으로 흘러가며 흩어짐.
# 바닥 = 그림 아래 1/3 (z = -0.55). 한 번 16장. 게임: DashTrail이 돌진 길 뒤에 카메라 보는 판으로, 그림 +X를 화면의 진행 방향에 맞춰 놓음
def _hex_bands(stops):
    return [(p, _c5_rgb(c)) for p, c in stops]


def _wedge(x):
    """꼬리 모양: 오른쪽(몸 뒤, x=1.0)이 가장 두껍고 왼쪽(x=-1.2)으로 가늘어짐 → 0~1"""
    return max(0.0, min(1.0, (x + 1.2) / 2.2)) ** 1.15


class _Mound:
    """바닥을 따라 이어진 둔덕 (2026-11-20: 덩어리를 무작위로 흩지 말고 참고 그림처럼 한 덩어리 둔덕으로).
    아래 줄 = 바닥에 앉은 덩어리를 겹치게 촘촘히, 오른쪽일수록 큼 / 윗줄 = 오른쪽 절반 위에 얹은 덩어리 / 꼭대기 = 몸 바로 뒤 가장 큰 뭉게.
    시간: 처음 20%에 부풀고, 전체가 뒤(왼쪽)로 굴러가며(Y축 회전) 꼬리부터 녹아 흩어짐"""

    def __init__(self, name, stops, seed, smin, smax, layers=2, light=0.5, squash=0.8, peak=1.0):
        rnd = random.Random(seed)
        self.items = []
        G = -0.55
        x = 1.0
        i = 0
        while x > -1.2:
            w = _wedge(x)
            sz = smin + (smax - smin) * w
            self._add(name, stops, i, rnd, x, G + sz * 0.42, sz, light, squash, w); i += 1
            if layers >= 2 and w > 0.4:
                self._add(name, stops, i, rnd, x - sz * 0.3, G + sz * 0.82, sz * 0.7, light, squash, w); i += 1
            if layers >= 3 and w > 0.8:
                self._add(name, stops, i, rnd, x - sz * 0.55, G + sz * 1.15, sz * 0.5, light, squash, w); i += 1
            x -= sz * 0.62                                         # 겹치게 촘촘히
        if peak > 0:
            sz = smax * 1.0 * peak
            self._add(name, stops, i, rnd, 0.78, G + sz * 0.55, sz, light, squash, 1.0)

    def _add(self, name, stops, i, rnd, x, z, sz, light, squash, w):
        m = toon_mat(f"{name}_m{i}", _hex_bands(stops), light=light)
        o = lumpy(f"{name}_m{i}", 900 + i * 7 + int(sz * 100), m)
        o["x"] = x + rnd.uniform(-0.04, 0.04); o["z"] = z; o["s"] = sz * rnd.uniform(0.9, 1.1); o["w"] = w
        o["y"] = rnd.uniform(-0.12, 0.12); o["roll"] = rnd.uniform(0, 6.28); o["sq"] = squash
        o["fade0"] = 0.22 + 0.48 * w + rnd.uniform(-0.04, 0.04)
        self.items.append((o, m))

    def pose(self, t, drift=0.35, rise=0.12):
        for o, m in self.items:
            grow = ease_out(min(1, t / 0.2))
            fade = min(1.0, max(0.0, (t - o["fade0"]) / 0.32))
            dx = drift * ease_out(t, 2)
            o.location = (o["x"] - dx, o["y"], o["z"] + rise * t * (0.3 + o["w"]))
            g = o["s"] * (0.5 + 0.5 * grow) * (1 + 0.25 * t) * (1 - fade) ** 1.3
            o.scale = (g * 1.06, g, g * (0.75 + 0.25 * o["sq"]))      # 거의 둥글게 (굴러도 세로로 늘어나 보이지 않게)
            o.rotation_euler = (0, o["roll"] - dx / max(0.1, o["s"]) * 0.8, 0)   # 뒤로 구름 (혹이 돌아감)
            o.hide_render = fade > 0.85
            setv(m, "heat", 1 - 0.45 * t)
            setv(m, "dissolve", min(0.55, max(0, (t - o["fade0"] + 0.08) / 0.45)))


class _Debris:
    """튀는 조각들: 몸 뒤(오른쪽)에서 뒤 · 위로 튀어 포물선. 종류 gear · shard · drop · star · rod"""

    def __init__(self, name, kind, stops, count, size, seed, vx=(1.2, 2.6), vz=(0.6, 1.8), x0=(0.45, 1.0), z0=(0.0, 0.45), life=(0.5, 0.8), light=0.35):
        rnd = random.Random(seed)
        self.items = []
        G = -0.55
        for j in range(count):
            m = toon_mat(f"{name}_{kind}{j}", _hex_bands(stops), light=light)
            if kind == "gear":
                o = gear_obj(f"{name}_g{j}", m, teeth=rnd.choice((6, 8, 10)), r=0.2, t=0.07)
            elif kind == "shard":
                o = shard_obj(f"{name}_s{j}", m)
            elif kind == "drop":
                o = drop_obj(f"{name}_d{j}", m)
            elif kind == "star":
                o = star_obj(f"{name}_t{j}", m)
            else:
                o = laser_rod(f"{name}_r{j}", m, 1.0, 0.05, 0.2, 6)
            o["kind"] = kind; o["size"] = size * rnd.uniform(0.75, 1.25)
            o["p0"] = Vector((rnd.uniform(*x0), rnd.uniform(-0.2, 0.2), G + rnd.uniform(*z0)))
            o["v"] = Vector((-rnd.uniform(*vx), 0, rnd.uniform(*vz)))
            o["lag"] = rnd.uniform(0, 0.18); o["life"] = rnd.uniform(*life); o["spin"] = rnd.uniform(4, 10) * rnd.choice((-1, 1))
            self.items.append((o, m))

    def pose(self, t):
        G = -0.55
        for o, m in self.items:
            k = (t - o["lag"]) / o["life"]
            if k <= 0 or k >= 1:
                o.hide_render = True
                continue
            o.hide_render = False
            tt = k * o["life"]
            v0 = Vector(o["v"])
            pos = Vector(o["p0"]) + v0 * tt + Vector((0, 0, -4.0 * tt * tt))
            pos.z = max(G, pos.z)
            s_ = o["size"] * (1 - max(0, (k - 0.7) / 0.3))
            if o["kind"] == "rod":
                _place_rod(o, pos, v0, o["size"] * 3 * (1 - k * 0.6), 0.6 + 0.4 * (1 - k))
            elif o["kind"] == "drop":
                v = v0 + Vector((0, 0, -8 * tt))
                o.location = pos
                o.rotation_euler = (0, -math.atan2(v.z, v.x), 0)
                o.scale = (s_ * 1.7, s_, s_)
            else:
                o.location = pos
                o.scale = (s_,) * 3
                if o["kind"] == "gear":
                    o.rotation_euler = (0.45, o["spin"] * tt, 0.3)
                elif o["kind"] == "star":
                    tw = 0.65 + 0.35 * abs(math.sin(k * 9))
                    o.scale = (s_ * tw,) * 3; o.rotation_euler = (0, k * 2, 0)
                else:
                    o.rotation_euler = (o["spin"] * tt, o["spin"] * 0.6 * tt, o["spin"] * 0.3 * tt)
            setv(m, "heat", 1 - 0.5 * k)


class _Streaks:
    """수평 속도선: 몸 뒤(오른쪽 끝)에 붙어 왼쪽으로 길게 → 왼쪽 끝부터 오그라들어 0.6에 사라짐"""

    def __init__(self, name, stops, count, thick, seed, zr=(0.06, 0.6), lr=(1.2, 2.0)):
        rnd = random.Random(seed)
        self.items = []
        for j in range(count):
            m = toon_mat(f"{name}_sl{j}", _hex_bands(stops))
            o = laser_rod(f"{name}_sl{j}", m, 1.0, thick * rnd.uniform(0.75, 1.25), 0.12, 10)
            o["z"] = -0.55 + rnd.uniform(*zr); o["len"] = rnd.uniform(*lr); o["lag"] = rnd.uniform(0, 0.1)
            self.items.append((o, m))

    def pose(self, t):
        for o, m in self.items:
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            kk = min(1.0, k / 0.6)
            L = o["len"] * (1 - ease_out(kk, 2))
            _place_rod(o, (1.1 - 0.25 * kk, 0, o["z"]), (1, 0, 0), max(0.01, L), 1.0 - 0.7 * kk)
            o.hide_render = kk >= 0.97


def _dash_scene(ink_rgb, thick=1.8):
    sc = reset(); set_line_color(sc, ink_rgb)
    sc.render.line_thickness = thick
    for s_ in sc.view_layers[0].freestyle_settings.linesets:
        s_.linestyle.thickness = thick
    sc.camera.data.ortho_scale = 2.9
    sc.camera.location = (0.0, -10, 0.05)              # 바닥(z −0.55)이 그림 아래 30%쯤, 오른쪽 큰 덩어리가 잘리지 않게
    return sc


def _dash_run(sc, set_name, poses):
    for f in range(FRAMES):
        t = (f + 1) / FRAMES
        for p_ in poses:
            p_(t)
        render(sc, set_name, f)


def bake_dash_sand():
    """1장 돌진 꼬리 (참고: docs/images/fx_src/dash_sand.png): 바닥에 깔린 모래 둔덕 — 몸 뒤가 높고 꼬리로 낮아짐, 위 면은 밝은 모래 · 아래는 짙은 황토,
    뒤로 구르며 꼬리부터 흩어짐 + 굴러 튀는 황동 톱니 2 + 모래 알갱이"""
    sc = _dash_scene((0.23, 0.14, 0.08))
    SAND = [(0.0, "FBEBC2"), (0.22, "F2D59A"), (0.46, "D9A35B"), (0.74, "9C6232")]
    mound = _Mound("dash_sand", SAND, 11, 0.16, 0.5, layers=3, squash=0.78)
    gears = _Debris("dash_sand", "gear", [(0.0, "FFE7A0"), (0.3, "E7B04A"), (0.7, "8A5A1C")], 2, 0.8, 21, vz=(1.0, 1.6), z0=(0.3, 0.6))
    grains = _Debris("dash_sand", "drop", [(0.0, "F2D59A"), (0.5, "B97A3E")], 14, 0.055, 22, vx=(1.5, 3.2), vz=(0.4, 1.4))
    _dash_run(sc, "dash_sand", [mound.pose, gears.pose, grains.pose])


def bake_dash_steam():
    """2장 돌진 꼬리 (참고: dash_steam.png): 크게 부푼 증기 둔덕(흰 · 회청, 몸 뒤가 높게 솟음) + 바닥을 긁는 주황 레일 불꽃 줄 + 석탄 조각"""
    sc = _dash_scene((0.17, 0.16, 0.23))
    STEAM = [(0.0, "FFFFFF"), (0.26, "EEF0F6"), (0.5, "D6D9E2"), (0.76, "9CA1B3")]
    mound = _Mound("dash_steam", STEAM, 12, 0.18, 0.56, layers=3, squash=0.92, peak=1.15)
    sparks = _Streaks("dash_steam", [(0.0, "FFF6C8"), (0.3, "FFD25A"), (0.7, "FF7A1E")], 5, 0.028, 23, zr=(0.0, 0.08), lr=(0.7, 1.5))
    flicks = _Debris("dash_steam", "rod", [(0.0, "FFF6C8"), (0.25, "FFD25A"), (0.6, "FF7A1E")], 8, 0.11, 24, vz=(0.1, 0.9), z0=(0.0, 0.08))
    coal = _Debris("dash_steam", "shard", [(0.0, "4A4652"), (0.5, "26232C")], 5, 0.14, 25)
    _dash_run(sc, "dash_steam", [mound.pose, sparks.pose, flicks.pose, coal.pose])


def bake_dash_water():
    """3장 돌진 꼬리 (참고: dash_water.png): 몸 뒤에서 솟아 뒤로 말리는 물마루(물거품 덩어리를 호를 따라 늘어놓음) + 바닥 물거품 둔덕 +
    물마루 끝에서 흩날리는 물방울 + 장미 · 하늘 유리 반짝임"""
    sc = _dash_scene((0.11, 0.23, 0.35))
    WATER = [(0.0, "FFFFFF"), (0.2, "E9FBFF"), (0.46, "9EE3F5"), (0.76, "4FA9D2")]
    foam = _Mound("dash_water_f", WATER, 13, 0.12, 0.32, layers=1, squash=0.6, peak=0)
    rnd = random.Random(31)
    crest = []
    for i in range(11):
        u = i / 10                                                  # 0 = 몸 뒤 바닥, 1 = 꼭대기에서 뒤(왼쪽)로 말려 숙인 끝
        a = math.radians(5 + 125 * u)
        m = toon_mat(f"dash_water_c{i}", _hex_bands(WATER), light=0.55)
        o = lumpy(f"dash_water_c{i}", 300 + i, m)
        r = 0.62 * (1 - 0.2 * u)
        crest.append(dict(o=o.name, m=m.name, p=(0.35 + r * math.cos(a), -0.05, -0.55 + r * math.sin(a) * 1.1),
                          s=0.3 * (1 - 0.5 * u) + 0.07, a=a, u=u))

    def crest_pose(t):
        grow = ease_out(min(1, t / 0.22))
        for c in crest:
            o = bpy.data.objects[c["o"]]; m = bpy.data.materials[c["m"]]        # 이름으로 다시 찾음 (붙잡아 둔 참조가 굽는 중 끊김)
            show = grow > c["u"] * 0.9                                 # 아래부터 차례로 솟아 말림
            fall = min(1.0, max(0.0, (t - 0.4 - 0.1 * (1 - c["u"])) / 0.45))
            px, py, pz = c["p"]
            o.location = (px - 0.35 * ease_out(t, 2), py, -0.55 + (pz + 0.55) * (1 - 0.7 * fall))
            g = c["s"] * (0.6 + 0.4 * grow) * (1 - fall) ** 1.2
            o.scale = (g * 1.5, g, g * 0.85)
            o.rotation_euler = (0, -c["a"] + math.pi / 2, 0)            # 호를 따라 늘인 방향
            o.hide_render = not show or fall > 0.9
            setv(m, "heat", 1 - 0.45 * t)
            setv(m, "dissolve", min(0.55, fall * 0.7))

    spray = _Debris("dash_water", "drop", [(0.0, "FFFFFF"), (0.25, "BFEFFF"), (0.6, "4FA9D2")], 18, 0.075, 32, vx=(1.0, 2.4), vz=(0.6, 1.6), x0=(0.1, 0.5), z0=(0.45, 0.75))
    glint1 = _Debris("dash_water", "star", [(0.0, "FFFFFF"), (0.3, "FFB7DE"), (0.7, "E07AB8")], 2, 0.24, 33, vx=(0.6, 1.2), vz=(0.2, 0.6), z0=(0.5, 0.8))
    glint2 = _Debris("dash_water", "star", [(0.0, "FFFFFF"), (0.3, "BFEFFF"), (0.7, "6FBFE0")], 2, 0.22, 34, vx=(0.6, 1.2), vz=(0.2, 0.6), z0=(0.2, 0.6))
    _dash_run(sc, "dash_water", [foam.pose, crest_pose, spray.pose, glint1.pose, glint2.pose])


def bake_dash_laser():
    """4장 돌진 꼬리 (참고: dash_laser.png, 느와르 흑백 + 빨강만): 굵은 빨간 레이저 속도선 여러 겹(흰 심지) + 낮게 깔린 흰 연기 둔덕(검은 잉크) +
    은빛 톱니 · 바늘 조각"""
    sc = _dash_scene((0.03, 0.02, 0.02), 2.0)
    SMOKE = [(0.0, "FFFFFF"), (0.4, "E6E6EA"), (0.72, "8E8E98")]
    smoke = _Mound("dash_laser", SMOKE, 14, 0.1, 0.3, layers=2, squash=0.7, peak=0.8)
    beams = _Streaks("dash_laser", [(0.0, "FFFFFF"), (0.07, "FF7A7A"), (0.2, "FF2A2A"), (0.55, "7A0710")], 6, 0.07, 26, zr=(0.05, 0.55), lr=(1.3, 2.1))
    gear = _Debris("dash_laser", "gear", [(0.0, "FFFFFF"), (0.45, "F2F2F2"), (0.8, "8C8C96")], 2, 0.85, 27, vz=(0.8, 1.4), z0=(0.3, 0.5))
    shards = _Debris("dash_laser", "shard", [(0.0, "FFFFFF"), (0.4, "D0D0D8"), (0.8, "6A6A74")], 7, 0.2, 28)
    _dash_run(sc, "dash_laser", [smoke.pose, beams.pose, gear.pose, shards.pose])


def _jagged_lens(name, mat, L=1.0, H=0.5, teeth=14, seed=3):
    """찢어진 허공 렌즈: 가로로 긴 렌즈 윤곽에 톱니처럼 들쭉날쭉한 테, 앞뒤로 볼록 (카메라 쪽 = −Y)"""
    rnd = random.Random(seed)
    bm = bmesh.new()
    ring_f, ring_b = [], []
    n = teeth * 2
    for i in range(n):
        a = math.tau * i / n
        r = 1.0 if i % 2 == 0 else rnd.uniform(0.72, 0.88)       # 들쭉날쭉
        x = math.cos(a) * L * r
        z = math.sin(a) * H * r * (0.35 + 0.65 * abs(math.sin(a)) ** 0.5)
        ring_f.append(bm.verts.new((x, -0.04, z)))
        ring_b.append(bm.verts.new((x, 0.04, z)))
    cf = bm.verts.new((0, -0.18, 0)); cb = bm.verts.new((0, 0.18, 0))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((ring_f[i], ring_f[j], cf)); bm.faces.new((ring_b[j], ring_b[i], cb))
        bm.faces.new((ring_f[j], ring_f[i], ring_b[i], ring_b[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bake_dash_rift():
    """5장 돌진 꼬리 (참고: dash_rift.png): 몸 뒤에서 꼬리 쪽으로 길게 찢어진 허공 렌즈(속 보라 허공 · 테 자홍, 들쭉날쭉) 안에 흰 별 +
    뒤로 흩날리는 보라 유리 조각 + 금 불티 줄"""
    sc = _dash_scene((0.08, 0.03, 0.14))
    lm_name = toon_mat("dash_rift_lens", _c5_bands("void"), light=0.15).name
    lens_name = _jagged_lens("dash_rift_lens", bpy.data.materials[lm_name]).name
    stars = []
    srnd = random.Random(41)
    for i in range(5):
        m = toon_mat(f"dash_rift_st{i}", _c5_bands("white"))
        o = star_obj(f"dash_rift_st{i}", m, 4, 0.5, 0.1, 0.05)
        stars.append(dict(o=o.name, p=(srnd.uniform(-0.6, 0.6), srnd.uniform(-0.12, 0.12)), s=srnd.uniform(0.07, 0.12)))

    def lens_pose(t):
        lens = bpy.data.objects[lens_name]; lm = bpy.data.materials[lm_name]   # 이름으로 다시 찾음
        grow = ease_out(min(1, t / 0.2))
        close = min(1.0, max(0.0, (t - 0.35) / 0.6))
        cx = 0.0 - 0.3 * ease_out(t, 2)
        lens.location = (cx, 0, -0.3)
        lens.scale = (1.15 * (0.4 + 0.6 * grow) * (1 - 0.35 * close), 1.0, 0.42 * grow * (1 - close) ** 1.3)
        lens.hide_render = close > 0.95
        setv(lm, "heat", 1 - 0.35 * t)
        setv(lm, "dissolve", min(0.5, close * 0.55))
        for st in stars:
            o = bpy.data.objects[st["o"]]
            o.location = (cx + st["p"][0] * lens.scale.x, -0.2, -0.3 + st["p"][1] * 0.7 * grow * (1 - close))
            tw = 0.6 + 0.4 * abs(math.sin(t * 20 + st["p"][0] * 7))
            o.scale = (st["s"] * tw * 2,) * 3
            o.hide_render = close > 0.7 or grow < 0.5

    shards = _Debris("dash_rift", "shard", [(0.0, "FFFFFF"), (0.15, "EAD9FF"), (0.4, "FF6FD0"), (0.8, "3A1A6A")], 10, 0.24, 42, z0=(0.0, 0.5))
    gold = _Streaks("dash_rift", [(0.0, "FFF7DB"), (0.3, "FFC64A"), (0.8, "B77722")], 3, 0.025, 43, zr=(0.0, 0.5), lr=(0.6, 1.2))
    _dash_run(sc, "dash_rift", [lens_pose, shards.pose, gold.pose])


def bake_dash_mirror():
    """거울 고양이 돌진 꼬리 (참고: dash_mirror.png): 몸 뒤에 큰 거울 조각 덩이 → 꼬리로 갈수록 잘게 흩어지는 은빛 조각 + 은빛 속도선 + 흰 반짝임"""
    sc = _dash_scene((0.13, 0.15, 0.23))
    big = _Debris("dash_mirror_b", "shard", [(0.0, "FFFFFF"), (0.2, "F4F7FF"), (0.55, "B9C6E0"), (0.85, "6F7C9A")], 6, 0.5, 51, vx=(0.5, 1.2), vz=(0.2, 0.9), x0=(0.6, 1.0), z0=(0.05, 0.5), life=(0.55, 0.8))
    small = _Debris("dash_mirror_s", "shard", [(0.0, "FFFFFF"), (0.2, "F4F7FF"), (0.55, "B9C6E0"), (0.85, "6F7C9A")], 14, 0.22, 52, vx=(1.6, 3.0), vz=(0.3, 1.2), x0=(0.2, 0.9))
    glints = _Debris("dash_mirror", "star", [(0.0, "FFFFFF"), (0.4, "E8EEFF")], 4, 0.28, 53, vx=(0.4, 1.0), vz=(0.2, 0.8), z0=(0.3, 0.8))
    lines = _Streaks("dash_mirror", [(0.0, "FFFFFF"), (0.4, "E8EEFF"), (0.8, "9AA6C4")], 4, 0.03, 54, zr=(0.1, 0.6))
    _dash_run(sc, "dash_mirror", [big.pose, small.pose, glints.pose, lines.pose])


SETS = {"dash_sand": bake_dash_sand, "dash_steam": bake_dash_steam, "dash_water": bake_dash_water, "dash_laser": bake_dash_laser, "dash_rift": bake_dash_rift, "dash_mirror": bake_dash_mirror, "dustclump": bake_dustclump, "dustring": bake_dustring, "dustpuff": bake_dustpuff, "c5orb": bake_c5orb, "c5shard": bake_c5shard, "c5spark": bake_c5spark, "c5rift": bake_c5rift, "c5strip": bake_c5strip,
        "c4grind": bake_c4grind, "c4scrap": bake_c4scrap, "c4orb": bake_c4orb, "c4needle": bake_c4needle, "c4spark": bake_c4spark, "c4slash": bake_c4slash, "c4strip": bake_c4strip,
        "fire": bake_fire, "smoke": bake_smoke, "spark": bake_spark, "mist": bake_mist, "dodge": bake_dodge, "memclock": bake_memclock, "memshatter": bake_memshatter,
        "c3orb": bake_c3orb, "c3shard": bake_c3shard, "c3spark": bake_c3spark, "c3water": bake_c3water}
only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SETS)
for n in only:
    SETS[n]()
    print("BAKED", n)
print("DONE")
