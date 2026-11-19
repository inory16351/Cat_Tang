# 추억 시계 v2 굽기 함수 (blender_bake_fx.py가 불러 씀)
# 2026-11-18 사용자: 평평한 그림 느낌 → 입체 · 반투명 · 자연스럽게
import bpy, math, random
from mathutils import Vector

TAU = math.tau


def _blended(m):
    try: m.surface_render_method = 'BLENDED'
    except Exception: m.blend_method = 'BLEND'


def _finish(nt, em, fac_socket, alpha, name_alpha="alpha"):
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    al = nt.nodes.new("ShaderNodeValue"); al.name = name_alpha; al.outputs[0].default_value = alpha
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'
    nt.links.new(fac_socket, mul.inputs[0]); nt.links.new(al.outputs[0], mul.inputs[1])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(mul.outputs[0], mix.inputs["Fac"]); nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])


def lit_mat(name, bands, alpha=1.0, rim=(0.12, 0.05, 0.01), opaque=False):
    """한쪽 빛 툰: 월드 노멀 · 빛 방향 내적 → 계단 색 (돌면 음영이 바뀜), 가장자리 짙은 테, 'alpha' 반투명, 'dissolve' 노이즈 녹음.
    opaque = 시계 몸 (반투명끼리 정렬이 꼬여 눈금 · 바늘이 가려지던 문제 → 몸은 불투명으로 굽고 반투명은 Unity FxPart.opacity로)"""
    m = bpy.data.materials.new(name); m.use_nodes = True
    if opaque:
        try: m.surface_render_method = 'DITHERED'
        except Exception: m.blend_method = 'CLIP'
    else: _blended(m)
    nt = m.node_tree; nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    L = nt.nodes.new("ShaderNodeCombineXYZ")
    lv = Vector((-0.55, -0.65, 0.6)).normalized()
    L.inputs[0].default_value, L.inputs[1].default_value, L.inputs[2].default_value = lv.x, lv.y, lv.z
    dot = nt.nodes.new("ShaderNodeVectorMath"); dot.operation = 'DOT_PRODUCT'
    nt.links.new(geo.outputs["Normal"], dot.inputs[0]); nt.links.new(L.outputs[0], dot.inputs[1])
    remap = nt.nodes.new("ShaderNodeMapRange"); remap.inputs[1].default_value = -1; remap.inputs[2].default_value = 1
    nt.links.new(dot.outputs["Value"], remap.inputs[0])
    ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = 'CONSTANT'
    els = ramp.color_ramp.elements
    els[0].position = bands[0][0]; els[0].color = (*bands[0][1], 1)
    els[1].position = bands[1][0]; els[1].color = (*bands[1][1], 1)
    for pos, col in bands[2:]:
        e = els.new(pos); e.color = (*col, 1)
    nt.links.new(remap.outputs[0], ramp.inputs["Fac"])
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.2
    rimc = nt.nodes.new("ShaderNodeMath"); rimc.operation = 'GREATER_THAN'; rimc.inputs[1].default_value = 0.82
    nt.links.new(lw.outputs["Facing"], rimc.inputs[0])
    mixc = nt.nodes.new("ShaderNodeMixRGB"); mixc.inputs["Color2"].default_value = (*rim, 1)
    nt.links.new(rimc.outputs[0], mixc.inputs["Fac"]); nt.links.new(ramp.outputs["Color"], mixc.inputs["Color1"])
    em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(mixc.outputs[0], em.inputs["Color"])
    tex = nt.nodes.new("ShaderNodeTexNoise"); tex.inputs["Scale"].default_value = 3.0; tex.inputs["Detail"].default_value = 3.0
    dis = nt.nodes.new("ShaderNodeValue"); dis.name = "dissolve"; dis.outputs[0].default_value = 0.0
    cmp = nt.nodes.new("ShaderNodeMath"); cmp.operation = 'GREATER_THAN'
    nt.links.new(tex.outputs["Fac"], cmp.inputs[0]); nt.links.new(dis.outputs[0], cmp.inputs[1])
    _finish(nt, em, cmp.outputs[0], alpha)
    return m


def soft_mat(name, rgb, alpha=0.6, mode="radial", inner=0.0, outer=1.0):
    """부드러운 빛 (외곽선 없음): radial = 가운데서 옅어지는 원판 / ring = inner~outer 고리 / fresnel = 공 모양 빛알"""
    m = bpy.data.materials.new(name); m.use_nodes = True; _blended(m)
    nt = m.node_tree; nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (*rgb, 1)
    if mode == "fresnel":
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.5
        inv = nt.nodes.new("ShaderNodeMath"); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1
        nt.links.new(lw.outputs["Facing"], inv.inputs[1]); fac = inv.outputs[0]
    else:
        tc = nt.nodes.new("ShaderNodeTexCoord")
        ln = nt.nodes.new("ShaderNodeVectorMath"); ln.operation = 'LENGTH'
        nt.links.new(tc.outputs["Object"], ln.inputs[0])
        ramp = nt.nodes.new("ShaderNodeValToRGB"); e = ramp.color_ramp.elements
        if mode == "radial":
            e[0].position = 0.0; e[0].color = (1, 1, 1, 1); e[1].position = 1.0; e[1].color = (0, 0, 0, 1)
        else:
            e[0].position = inner; e[0].color = (0, 0, 0, 1); e[1].position = outer; e[1].color = (0, 0, 0, 1)
            x = e.new((inner + outer) / 2); x.color = (1, 1, 1, 1)
        nt.links.new(ln.outputs["Value"], ramp.inputs["Fac"]); fac = ramp.outputs["Color"]
    _finish(nt, em, fac, alpha)
    return m


def glass_mat(name):
    """유리 덮개: 가장자리 흰 테 + 'sweep' 위치에 대각선 빛줄기"""
    m = bpy.data.materials.new(name); m.use_nodes = True; _blended(m)
    nt = m.node_tree; nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (1, 1, 1, 1)
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.3
    rim = nt.nodes.new("ShaderNodeMath"); rim.operation = 'POWER'; rim.inputs[1].default_value = 3
    nt.links.new(lw.outputs["Facing"], rim.inputs[0])
    tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    diag = nt.nodes.new("ShaderNodeMath"); diag.operation = 'ADD'
    nt.links.new(sep.outputs[0], diag.inputs[0]); nt.links.new(sep.outputs[2], diag.inputs[1])
    sw = nt.nodes.new("ShaderNodeValue"); sw.name = "sweep"; sw.outputs[0].default_value = -3
    sub = nt.nodes.new("ShaderNodeMath"); sub.operation = 'SUBTRACT'
    nt.links.new(diag.outputs[0], sub.inputs[0]); nt.links.new(sw.outputs[0], sub.inputs[1])
    ab = nt.nodes.new("ShaderNodeMath"); ab.operation = 'ABSOLUTE'; nt.links.new(sub.outputs[0], ab.inputs[0])
    band = nt.nodes.new("ShaderNodeMath"); band.operation = 'LESS_THAN'; band.inputs[1].default_value = 0.09
    nt.links.new(ab.outputs[0], band.inputs[0])
    b2 = nt.nodes.new("ShaderNodeMath"); b2.operation = 'MULTIPLY'; b2.inputs[1].default_value = 0.75
    nt.links.new(band.outputs[0], b2.inputs[0])
    r2 = nt.nodes.new("ShaderNodeMath"); r2.operation = 'MULTIPLY'; r2.inputs[1].default_value = 0.8
    nt.links.new(rim.outputs[0], r2.inputs[0])
    tot = nt.nodes.new("ShaderNodeMath"); tot.operation = 'ADD'; tot.use_clamp = True
    nt.links.new(b2.outputs[0], tot.inputs[0]); nt.links.new(r2.outputs[0], tot.inputs[1])
    _finish(nt, em, tot.outputs[0], 1.0)
    return m


def bake(sc, H):
    """H = blender_bake_fx 도우미 (set_line_color, box_obj, gear_obj, setv, ease_out, render)"""
    sc.render.resolution_x = sc.render.resolution_y = 384
    H.set_line_color(sc, (0.16, 0.08, 0.04))
    sc.render.line_thickness = 1.8
    ink = bpy.data.collections.new("ink"); sc.collection.children.link(ink)
    for s_ in sc.view_layers[0].freestyle_settings.linesets:
        s_.select_by_collection = True; s_.collection = ink; s_.linestyle.thickness = 1.8; s_.linestyle.alpha = 0.85

    def link_ink(o):
        for c in list(o.users_collection): c.objects.unlink(o)
        ink.objects.link(o); return o

    setv, ease_out = H.setv, H.ease_out
    brass = [(0.0, (0.16, 0.07, 0.012)), (0.42, (0.42, 0.2, 0.035)), (0.62, (0.72, 0.41, 0.08)), (0.84, (1.0, 0.82, 0.42))]
    face_b = [(0.0, (0.62, 0.52, 0.36)), (0.45, (0.86, 0.78, 0.6)), (0.7, (0.98, 0.95, 0.86))]
    dark_b = [(0.0, (0.08, 0.04, 0.04)), (0.6, (0.2, 0.12, 0.1))]
    A = 1.0                                                    # 시계 몸은 불투명 (반투명은 Unity FxPart.opacity)
    mb, mc, md = lit_mat("brass", brass, A, opaque=True), lit_mat("face", face_b, A, (0.4, 0.3, 0.2), opaque=True), lit_mat("dark", dark_b, A, opaque=True)
    watch = bpy.data.objects.new("watch", None); sc.collection.objects.link(watch)

    def add(o, inked=True):
        o.parent = watch
        if inked: link_ink(o)
        return o

    R = 0.62
    bpy.ops.mesh.primitive_cylinder_add(radius=R, depth=0.2, vertices=64, rotation=(math.pi / 2, 0, 0)); case = add(bpy.context.active_object); case.data.materials.append(mb)
    bv = case.modifiers.new("bevel", 'BEVEL'); bv.width = 0.06; bv.segments = 3
    bpy.ops.mesh.primitive_torus_add(major_radius=R * 0.95, minor_radius=0.07, major_segments=64, minor_segments=12, rotation=(math.pi / 2, 0, 0), location=(0, -0.1, 0)); bez = add(bpy.context.active_object); bez.data.materials.append(mb)
    for p in bez.data.polygons: p.use_smooth = True
    bpy.ops.mesh.primitive_cylinder_add(radius=R * 0.84, depth=0.02, vertices=64, rotation=(math.pi / 2, 0, 0), location=(0, -0.108, 0)); face = add(bpy.context.active_object); face.data.materials.append(mc)
    for k in range(12):
        a = TAU * k / 12; big = k % 3 == 0
        w = 0.035 if big else 0.018
        tk = add(H.box_obj(f"tick{k}", md, -w, w, R * (0.6 if big else 0.68), R * 0.78, 0.012, -0.125))
        tk.rotation_euler = (0, a, 0)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.09, depth=0.15, vertices=24, location=(0, 0, R + 0.09)); crown = add(bpy.context.active_object); crown.data.materials.append(mb)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.15, minor_radius=0.04, major_segments=32, minor_segments=10, rotation=(math.pi / 2, 0, 0), location=(0, 0, R + 0.3)); bow = add(bpy.context.active_object); bow.data.materials.append(mb)
    for p in bow.data.polygons: p.use_smooth = True
    hm = add(H.box_obj("minute", md, -0.024, 0.024, -0.07, R * 0.7, 0.012, -0.135))
    hh = add(H.box_obj("hour", md, -0.034, 0.034, -0.05, R * 0.44, 0.012, -0.13))
    ghosts = []
    for i in range(3):
        gm = soft_mat(f"ghost{i}", (0.95, 0.7, 0.3), 0.0, "fresnel")
        g = add(H.box_obj(f"ghost{i}", gm, -0.03, 0.03, 0.0, R * 0.68, 0.006, -0.133), False); ghosts.append((g, gm, i))
    bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.05, vertices=20, rotation=(math.pi / 2, 0, 0), location=(0, -0.16, 0)); cap = add(bpy.context.active_object); cap.data.materials.append(mb)
    gl = glass_mat("glass")
    bpy.ops.mesh.primitive_uv_sphere_add(radius=R * 0.86, segments=48, ring_count=24, location=(0, -0.1, 0))
    dome = add(bpy.context.active_object, False); dome.scale = (1, 0.28, 1); dome.data.materials.append(gl)
    for p in dome.data.polygons: p.use_smooth = True
    watch_mats = [mb, mc, md]
    halo_m = soft_mat("halo", (1.0, 0.75, 0.35), 0.0, "radial")
    bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.0, fill_type='NGON', rotation=(math.pi / 2, 0, 0), location=(0, 0.5, 0)); halo = bpy.context.active_object; halo.data.materials.append(halo_m)
    waves = []
    for i in range(2):
        wm = soft_mat(f"wave{i}", (1.0, 0.88, 0.55), 0.0, "ring", 0.82, 1.0)
        bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.0, fill_type='NGON', rotation=(math.pi / 2, 0, 0), location=(0, 0.4, 0)); w = bpy.context.active_object; w.data.materials.append(wm)
        waves.append((w, wm, 0.1 + i * 0.16))
    rnd = random.Random(59)
    gears = []
    for i in range(4):
        m = lit_mat(f"gear{i}", brass, 1.0, opaque=True)
        g = link_ink(H.gear_obj(f"gear{i}", m, teeth=rnd.choice((7, 8, 10)), r=rnd.uniform(0.12, 0.18)))
        a = TAU * (i + rnd.uniform(-0.15, 0.15)) / 4 + 0.6
        g["dir"] = (math.cos(a), math.sin(a)); g["spin"] = rnd.choice((-1, 1)) * rnd.uniform(4, 7); g["lag"] = 0.14 + i * 0.03
        gears.append((g, m))
    motes = []
    for i in range(14):
        m = soft_mat(f"mote{i}", (1.0, 0.92, 0.65), 0.0, "fresnel")
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=rnd.uniform(0.035, 0.07))
        o = bpy.context.active_object; o.data.materials.append(m)
        for p in o.data.polygons: p.use_smooth = True
        a = rnd.uniform(0, TAU); r = rnd.uniform(0.5, 1.2)
        o["p0"] = (math.cos(a) * r, -0.4, math.sin(a) * r * 0.9); o["v"] = (math.cos(a) * 0.25, rnd.uniform(0.35, 0.7)); o["lag"] = rnd.uniform(0.05, 0.4)
        motes.append((o, m))
    N = 24
    for f in range(N):
        t = f / (N - 1)
        k = min(1.0, t / 0.18)
        pop = (0.25 + 0.95 * ease_out(k, 3)) if t < 0.18 else 1.2 - 0.2 * min(1, (t - 0.18) / 0.12)
        fade = max(0.0, 1 - t / 0.75)
        sw = math.sin(t * 24) * 0.34 * fade
        tilt = 1 - ease_out(k, 2)
        watch.scale = (pop, pop, pop)
        watch.rotation_euler = (tilt * 1.1 + 0.12, sw, tilt * -1.4 + math.sin(t * 24 + 1.2) * 0.22 * fade)
        rw = ease_out(min(1, max(0, (t - 0.08) / 0.6)), 2)
        ang = 0.6 + rw * TAU * 2.5
        hm.rotation_euler = (0, ang, 0); hh.rotation_euler = (0, 2.2 + rw * TAU * 0.4, 0)
        spd = max(0.0, 1 - abs(t - 0.3) / 0.3)
        for g, gm, i in ghosts:
            g.rotation_euler = (0, ang - (i + 1) * 0.28, 0); setv(gm, "alpha", 0.55 * spd * (1 - i * 0.3))
        setv(gl, "sweep", -1.6 + 3.4 * min(1, max(0, (t - 0.12) / 0.4)))
        dz = max(0.0, (t - 0.66) / 0.34)
        for m in watch_mats:
            setv(m, "dissolve", dz * 0.75)
        setv(gl, "alpha", 1 - dz)
        for o in watch.children:
            o.hide_render = dz > 0.85
        hp = math.sin(min(1, t / 0.2) * math.pi / 2) * (1 - max(0, (t - 0.45) / 0.55))
        halo.scale = (1.1 + 0.25 * t,) * 3; setv(halo_m, "alpha", 0.55 * max(0, hp))
        for w, wm, lag in waves:
            q = max(0.0, (t - lag) / 0.6)
            s = 0.6 + 1.0 * ease_out(min(1, q), 3); w.scale = (s, s, s)
            setv(wm, "alpha", 0.7 * (1 - min(1, q)) if q > 0 else 0)
        for g, m in gears:
            q = (t - g["lag"]) / 0.6
            g.hide_render = q <= 0 or q >= 1
            d = 0.4 + 0.95 * ease_out(max(0, min(1, q)), 2)
            g.location = (g["dir"][0] * d, 0.15, g["dir"][1] * d)
            g.rotation_euler = (0, t * g["spin"], 0)
            setv(m, "dissolve", max(0, (q - 0.55) / 0.45) * 0.85)
        for o, m in motes:
            q = (t - o["lag"]) / 0.55
            v = math.sin(min(1, max(0, q)) * math.pi) if 0 <= q <= 1 else 0
            u = max(0.0, q)
            o.location = (o["p0"][0] + o["v"][0] * u, o["p0"][1], o["p0"][2] + o["v"][1] * u)
            setv(m, "alpha", 0.9 * v); o.hide_render = v <= 0.01
        H.render(sc, "memclock", f)
