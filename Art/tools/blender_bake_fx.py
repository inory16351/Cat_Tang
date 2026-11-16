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


SETS = {"fire": bake_fire, "smoke": bake_smoke, "spark": bake_spark}
only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SETS)
for n in only:
    SETS[n]()
    print("BAKED", n)
print("DONE")
