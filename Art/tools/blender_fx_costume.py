# 11:59 코스튬 효과 VFX (2026-11-24 확정 코스튬 4벌) — 미리보기 장면 + Unity용 플립북 굽기
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_costume.py [-- undertaker mirror sand pajama] [--bake-only | --preview-only]
# 결과
#   미리보기 프레임: Art/fx/costume/preview/<효과>/f_000.png … (640×480, 3/4 탑다운 원근 카메라, 바닥 · 고양이 · 적 포함)
#   플립북:          Art/fx/costume/anim/bk_cos_<세트>_<프레임>.png (256 정사각, 투명 배경, 같은 카메라 각도의 직교 투영)
#   GIF:             python docs/tools/costume_fx_gif.py  → docs/images/costume_fx/<효과>.gif
# 화풍: blender_bake_fx.py와 같은 규칙 — 조명 없는 Emission 색 띠(계단식) + Freestyle 잉크 외곽선 + 노이즈 구멍으로 녹아 사라짐.
#   리본 띠는 UV(길이 · 가운데로부터 거리)로 색 띠. 오라처럼 시야를 가리면 안 되는 것은 잉크 없이 반투명(BLENDED).
# 효과 (발동할 때만, 아이콘형 X — 타격 모양 · 움직임으로 개성)
#   undertaker 장의사 정장: 처형 → 잉크가 터지며 영혼 줄기 3가닥이 휘어 날아와 고양이 가슴으로 빨려 듦 → 박하색 회복 섬광
#   mirror     거울 망토:   극한 회피 → 제자리에 남은 보라 · 검정 그림자 고양이가 잔상 꼬리를 끌고 다른 적에게 날아가 X자로 두 번 베고 안개로 흩어짐
#   sand       모래 순례자: 판정 0 이하 탄 명중 → 바닥 모래 소용돌이(나선 팔 5 + 깔때기 띠 3 + 알갱이 + 가장자리 뭉게), 적은 둔화 · 0.5초마다 지속 피해 → 풀리며 흩어짐
#   pajama     노인의 잠옷: 체력 40% 미만 진입 → 푸른 펄스(바닥 고리 + 솟는 가시) 한 번 → 몸 둘레 희미한 푸른 잔불 오라(상시)
import bpy, bmesh, math, random, os, sys, bisect
from mathutils import Vector, noise, Matrix, Euler

ROOT = r"C:/Project/Cat_Tang/Art/fx/costume"
FPS = 30
TAU = math.tau
CAM_LOC = Vector((0.9, -6.4, 7.2))
CAM_AIM = Vector((0.9, 0.7, 0.25))


def lin(h):
    """sRGB 16진 → 선형 (Standard 뷰에서 적은 색 그대로 보이게)"""
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def bands(stops):
    return [(p, lin(c)) for p, c in stops]


def ease_out(t, p=3):
    t = min(1.0, max(0.0, t))
    return 1 - (1 - t) ** p


def ease_io(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def seg(t, a, b):
    """t가 a~b 사이에서 0~1"""
    return min(1.0, max(0.0, (t - a) / (b - a)))


# ---------------------------------------------------------------- 장면
class Scene:
    def __init__(self, ink=(0.12, 0.08, 0.14), aim=(0.9, 0.7, 0.25)):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = self.sc = bpy.context.scene
        for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
            try:
                sc.render.engine = eng
                break
            except TypeError:
                continue
        sc.render.resolution_x, sc.render.resolution_y = 640, 480
        sc.render.image_settings.file_format = 'PNG'
        sc.render.image_settings.color_mode = 'RGBA'
        sc.view_settings.view_transform = 'Standard'
        try:
            sc.eevee.taa_render_samples = 16
        except Exception:
            pass
        w = bpy.data.worlds.new("w"); sc.world = w
        w.color = lin("1E1824")
        # 잉크 외곽선 (noink 모음은 뺌)
        sc.render.use_freestyle = True
        sc.render.line_thickness_mode = 'ABSOLUTE'
        sc.render.line_thickness = 1.6
        self.noink = bpy.data.collections.new("noink"); sc.collection.children.link(self.noink)
        fs = sc.view_layers[0].freestyle_settings
        ls = fs.linesets.new("ink") if not fs.linesets else fs.linesets[0]
        ls.select_by_visibility = True; ls.select_silhouette = True; ls.select_border = True; ls.select_crease = False
        ls.select_by_collection = True; ls.collection = self.noink; ls.collection_negation = 'EXCLUSIVE'
        if ls.linestyle is None:
            ls.linestyle = bpy.data.linestyles.new("ink")
        ls.linestyle.color = ink; ls.linestyle.thickness = 1.6
        cd = bpy.data.cameras.new("cam"); cd.lens = 52
        cam = self.cam = bpy.data.objects.new("cam", cd); sc.collection.objects.link(cam)
        self.aim = Vector(aim)
        cam.location = self.aim + (CAM_LOC - CAM_AIM)
        cam.rotation_euler = (CAM_AIM - CAM_LOC).to_track_quat('-Z', 'Y').to_euler()
        sc.camera = cam
        self.poses = []      # f(t) 목록
        self.groups = {}     # 굽기 묶음 이름 → 오브젝트
        self.actors = []     # 바닥 · 고양이 · 적 · 그림자 (굽기 때 숨김)

    def link(self, ob, noink=False, grp=None, actor=False):
        (self.noink if noink else self.sc.collection).objects.link(ob)
        if grp:
            self.groups.setdefault(grp, []).append(ob)
        if actor:
            self.actors.append(ob)
        return ob

    def run(self, name, dur, out_dir=None):
        out = out_dir or os.path.join(ROOT, "preview", name)
        os.makedirs(out, exist_ok=True)
        self.sc.render.film_transparent = False
        n = int(round(dur * FPS))
        for f in range(n):
            t = f / FPS
            for p in self.poses:
                p(t)
            self.sc.render.filepath = os.path.join(out, f"f_{f:03d}.png")
            bpy.ops.render.render(write_still=True)
        print("PREVIEW", name, n)

    def bake(self, set_name, groups, center, scale, t0, t1, frames=16, loop=False):
        """그 묶음만 보이게, 같은 각도의 직교 카메라로 center를 가운데 두고 256 정사각 · 투명"""
        out = os.path.join(ROOT, "anim"); os.makedirs(out, exist_ok=True)
        keep = set()
        for g in groups:
            keep.update(o.name for o in self.groups.get(g, []))
        saved = {}
        for o in bpy.data.objects:
            if o.type in ('MESH', 'CURVE') and o.name not in keep:
                saved[o.name] = o.hide_render
                o.hide_render = True
        sc, cam = self.sc, self.cam
        r0 = (sc.render.resolution_x, sc.render.resolution_y, cam.data.type, cam.location.copy())
        sc.render.resolution_x = sc.render.resolution_y = 256
        sc.render.film_transparent = True
        cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale
        fwd = (CAM_AIM - CAM_LOC).normalized()
        cam.location = Vector(center) - fwd * 9.0
        for f in range(frames):
            t = t0 + (t1 - t0) * (f / frames if loop else f / max(1, frames - 1))
            for p in self.poses:
                p(t)
            # pose가 숨김을 바꿨을 수 있으니 묶음 밖은 다시 숨김
            for o in bpy.data.objects:
                if o.name in saved:
                    o.hide_render = True
            sc.render.filepath = os.path.join(out, f"bk_cos_{set_name}_{f}.png")
            bpy.ops.render.render(write_still=True)
        for k, v in saved.items():
            bpy.data.objects[k].hide_render = v
        sc.render.resolution_x, sc.render.resolution_y, cam.data.type, cam.location = r0[0], r0[1], r0[2], r0[3]
        print("BAKED", set_name, frames)


# ---------------------------------------------------------------- 재질
def _finish(m, nt, color_out, blend=False):
    """dissolve(노이즈 구멍) · alpha(반투명) 값 노드 + 출력"""
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.0
    nt.links.new(color_out, em.inputs["Color"])
    tex = nt.nodes.new("ShaderNodeTexNoise"); tex.inputs["Scale"].default_value = 3.0; tex.inputs["Detail"].default_value = 3.0
    dis = nt.nodes.new("ShaderNodeValue"); dis.name = "dissolve"; dis.outputs[0].default_value = 0.0
    cmp = nt.nodes.new("ShaderNodeMath"); cmp.operation = 'GREATER_THAN'
    nt.links.new(tex.outputs["Fac"], cmp.inputs[0]); nt.links.new(dis.outputs[0], cmp.inputs[1])
    al = nt.nodes.new("ShaderNodeValue"); al.name = "alpha"; al.outputs[0].default_value = 1.0
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'
    nt.links.new(cmp.outputs[0], mul.inputs[0]); nt.links.new(al.outputs[0], mul.inputs[1])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(mul.outputs[0], mix.inputs["Fac"]); nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    try:
        m.surface_render_method = 'BLENDED' if blend else 'DITHERED'
    except Exception:
        m.blend_method = 'BLEND' if blend else 'CLIP'
    return m


def _ramp(nt, bnds):
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = 'CONSTANT'
    els = ramp.color_ramp.elements
    els[0].position = bnds[0][0]; els[0].color = (*bnds[0][1], 1)
    els[1].position = bnds[1][0] if len(bnds) > 1 else 1.0; els[1].color = (*(bnds[1][1] if len(bnds) > 1 else bnds[0][1]), 1)
    for pos, col in bnds[2:]:
        e = els.new(pos); e.color = (*col, 1)
    return ramp


def toon(name, stops, blend=False):
    """덩어리용: 카메라 정면 = 심지 색, 가장자리 = 테 색. 'heat'(1 = 뜨거움, 0 = 전체를 테 쪽으로)"""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.35
    heat = nt.nodes.new("ShaderNodeValue"); heat.name = "heat"; heat.outputs[0].default_value = 1.0
    inv = nt.nodes.new("ShaderNodeMath"); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    nt.links.new(heat.outputs[0], inv.inputs[1])
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'; mul.inputs[1].default_value = 0.8
    nt.links.new(inv.outputs[0], mul.inputs[0])
    add = nt.nodes.new("ShaderNodeMath"); add.operation = 'ADD'; add.use_clamp = True
    nt.links.new(lw.outputs["Facing"], add.inputs[0]); nt.links.new(mul.outputs[0], add.inputs[1])
    ramp = _ramp(nt, bands(stops))
    nt.links.new(add.outputs[0], ramp.inputs["Fac"])
    return _finish(m, nt, ramp.outputs["Color"], blend)


def strip(name, stops, along=0.55, across=0.75, blend=False):
    """리본 띠용: UV.x = 머리(0) → 꼬리(1), UV.y = 가장자리(0 · 1). 값 = max(along·x, across·|y−0.5|·2) → 색 띠"""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    uv = nt.nodes.new("ShaderNodeUVMap")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(uv.outputs["UV"], sep.inputs[0])
    a = nt.nodes.new("ShaderNodeMath"); a.operation = 'MULTIPLY'; a.inputs[1].default_value = along
    nt.links.new(sep.outputs["X"], a.inputs[0])
    c = nt.nodes.new("ShaderNodeMath"); c.operation = 'SUBTRACT'; c.inputs[1].default_value = 0.5
    nt.links.new(sep.outputs["Y"], c.inputs[0])
    ab = nt.nodes.new("ShaderNodeMath"); ab.operation = 'ABSOLUTE'; nt.links.new(c.outputs[0], ab.inputs[0])
    b = nt.nodes.new("ShaderNodeMath"); b.operation = 'MULTIPLY'; b.inputs[1].default_value = 2 * across
    nt.links.new(ab.outputs[0], b.inputs[0])
    mx = nt.nodes.new("ShaderNodeMath"); mx.operation = 'MAXIMUM'; mx.use_clamp = True
    nt.links.new(a.outputs[0], mx.inputs[0]); nt.links.new(b.outputs[0], mx.inputs[1])
    ramp = _ramp(nt, bands(stops))
    nt.links.new(mx.outputs[0], ramp.inputs["Fac"])
    return _finish(m, nt, ramp.outputs["Color"], blend)


def flat(name, hexc, blend=False):
    return toon(name, [(0.0, hexc), (1.0, hexc)], blend)


def glow(name, hexc, soft=1.6, tail=1.2, head=6.0, whiten=0.85):
    """단색 광 띠 (2026-11-24 사용자: 그림 느낌 줄이고 단색 · 광 효과). 잉크 없음 · 반투명.
    UV.y 가운데 = 하얗게 달아오른 심지 → 가장자리로 부드럽게 사라짐, UV.x = 머리(0, 짧게 켜짐) → 꼬리(1, 사라짐)"""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    M = lambda op, a=None, b=None, clamp=False: _math(nt, op, a, b, clamp)
    uv = nt.nodes.new("ShaderNodeUVMap")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(uv.outputs["UV"], sep.inputs[0])
    c = M('SUBTRACT', sep.outputs["Y"], 0.5); c = M('ABSOLUTE', c); c = M('MULTIPLY', c, 2.0)
    c = M('SUBTRACT', 1.0, c, True)                                    # 1 = 가운데, 0 = 가장자리
    e = M('POWER', c, soft)
    a = e
    if tail > 0:                                                       # tail=0 → 꼬리 사라짐 없음 (pow(0,0) 이음매 방지)
        tl = M('SUBTRACT', 1.0, sep.outputs["X"], True); a = M('MULTIPLY', a, M('POWER', tl, tail))
    if head > 0:                                                       # 고리(닫힌 띠)는 head=0 → 이음매 없음
        a = M('MULTIPLY', a, M('MULTIPLY', sep.outputs["X"], head, True))
    al = nt.nodes.new("ShaderNodeValue"); al.name = "alpha"; al.outputs[0].default_value = 1.0
    a = M('MULTIPLY', a, al.outputs[0], True)
    core = M('MULTIPLY', M('POWER', c, 5.0), whiten, True)
    mixc = nt.nodes.new("ShaderNodeMix"); mixc.data_type = 'RGBA'
    nt.links.new(core, mixc.inputs["Factor"])
    mixc.inputs["A"].default_value = (*lin(hexc), 1); mixc.inputs["B"].default_value = (1, 1, 1, 1)
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.0
    nt.links.new(mixc.outputs["Result"], em.inputs["Color"])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(a, mix.inputs["Fac"]); nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
    out = nt.nodes.new("ShaderNodeOutputMaterial"); nt.links.new(mix.outputs[0], out.inputs["Surface"])
    try:
        m.surface_render_method = 'BLENDED'
    except Exception:
        m.blend_method = 'BLEND'
    return m


def _math(nt, op, a, b=None, clamp=False):
    n = nt.nodes.new("ShaderNodeMath"); n.operation = op; n.use_clamp = clamp
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            n.inputs[i].default_value = v
        else:
            nt.links.new(v, n.inputs[i])
    return n.outputs[0]

def slash_mat(name, stops=None):
    """초승달 참격 재질 (2026-11-24 레퍼런스 sunpixels Elemental Slashes 프레임 분석).
    UV.x = 칼끝(0) → 꼬리(1), UV.y = 바깥 날(0) → 안쪽(1). 색 = 계단식 단색 띠 (바깥 하얀 날 → 연보라 → 보라 → 짙은 보라)
    'erode': 0 = 꽉 찬 초승달 · 0.2~0.45 = 몸통이 안쪽 · 꼬리부터 갈라짐 · 0.45~0.8 = 결 따라 가는 평행선 3~4가닥(끊긴 점선)만 남아 오래 머묾 · 1 = 사라짐"""
    stops = stops or [(0.0, "FFFFFF"), (0.1, "E4D8FF"), (0.3, "B49BFF"), (0.58, "7652D6"), (0.82, "3E2378")]
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    M = lambda op, a=None, b=None, clamp=False: _math(nt, op, a, b, clamp)
    uv = nt.nodes.new("ShaderNodeUVMap")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(uv.outputs["UV"], sep.inputs[0])
    X, Y = sep.outputs["X"], sep.outputs["Y"]
    er = nt.nodes.new("ShaderNodeValue"); er.name = "erode"; er.outputs[0].default_value = 0.0
    E = er.outputs[0]
    nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 5.0; nz.inputs["Detail"].default_value = 2.0
    nt.links.new(uv.outputs["UV"], nz.inputs["Vector"])
    # 몸통: 바깥일수록 · 칼끝일수록 오래 버팀 (안쪽 · 꼬리부터 갈라짐)
    body = M('ADD', M('MULTIPLY', M('SUBTRACT', 1.0, Y), 0.8), M('MULTIPLY', M('SUBTRACT', 1.0, X), 0.35))
    body = M('ADD', body, M('MULTIPLY', M('SUBTRACT', nz.outputs["Fac"], 0.5), 0.5))
    keep_body = M('GREATER_THAN', body, M('MULTIPLY', E, 2.6))
    # 결 선: 폭 방향 평행선 3.5가닥 (sin 봉우리를 뾰족하게) × 길이 방향으로 끊긴 점선
    st = M('MULTIPLY', M('ADD', M('SINE', M('MULTIPLY', Y, 22.0)), 1.0), 0.5)
    st = M('POWER', st, 10.0)
    dn = nt.nodes.new("ShaderNodeTexNoise"); dn.inputs["Scale"].default_value = 3.2; dn.inputs["Detail"].default_value = 1.0
    sc = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(M('MULTIPLY', X, 11.0), sc.inputs["X"]); nt.links.new(M('MULTIPLY', Y, 1.5), sc.inputs["Y"])
    nt.links.new(sc.outputs["Vector"], dn.inputs["Vector"])
    dash = M('GREATER_THAN', dn.outputs["Fac"], M('ADD', 0.42, M('MULTIPLY', M('MAXIMUM', M('SUBTRACT', E, 0.6), 0.0), 1.6)))   # 길이 방향으로 더 자주 끊긴 점선 → 갈수록 더 끊김
    line_on = M('MULTIPLY', M('GREATER_THAN', E, 0.18), M('LESS_THAN', E, 0.995))
    keep_line = M('MULTIPLY', M('MULTIPLY', M('GREATER_THAN', st, 0.35), dash), line_on)
    keep = M('MAXIMUM', keep_body, keep_line)
    # 색: 몸통은 띠 색, 남은 선은 밝게 (레퍼런스처럼 선은 하얗게 빛남)
    ramp = _ramp(nt, bands(stops)); nt.links.new(Y, ramp.inputs["Fac"])
    lc = nt.nodes.new("ShaderNodeMix"); lc.data_type = 'RGBA'
    nt.links.new(M('MULTIPLY', keep_line, M('SUBTRACT', 1.0, keep_body)), lc.inputs["Factor"])
    nt.links.new(ramp.outputs["Color"], lc.inputs["A"]); lc.inputs["B"].default_value = (*lin("EDE6FF"), 1)
    al = nt.nodes.new("ShaderNodeValue"); al.name = "alpha"; al.outputs[0].default_value = 1.0
    a = M('MULTIPLY', keep, al.outputs[0])
    wv = nt.nodes.new("ShaderNodeValue"); wv.name = "white"; wv.outputs[0].default_value = 0.0   # 1 = 휘두르는 동안 새하얀 단색 (랫키호테 칼빛)
    wc = nt.nodes.new("ShaderNodeMix"); wc.data_type = 'RGBA'
    nt.links.new(wv.outputs[0], wc.inputs["Factor"]); nt.links.new(lc.outputs["Result"], wc.inputs["A"]); wc.inputs["B"].default_value = (1, 1, 1, 1)
    em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(wc.outputs["Result"], em.inputs["Color"])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mx = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(a, mx.inputs["Fac"]); nt.links.new(tr.outputs[0], mx.inputs[1]); nt.links.new(em.outputs[0], mx.inputs[2])
    out = nt.nodes.new("ShaderNodeOutputMaterial"); nt.links.new(mx.outputs[0], out.inputs["Surface"])
    try:
        m.surface_render_method = 'BLENDED'
    except Exception:
        m.blend_method = 'BLEND'
    return m


def cam_right():
    return (CAM_AIM - CAM_LOC).normalized().cross(Vector((0, 0, 1))).normalized()


class GlowStreak:
    """심지(가늘고 밝음) + 번짐(넓고 옅음) 두 겹 띠 = 광"""
    def __init__(self, S, name, hexc, grp, core_w=1.0, halo_w=4.0, halo_a=0.32, tail=1.2, whiten=0.85):
        self.cm = glow(name + "_c", hexc, soft=1.2, tail=tail, whiten=whiten)
        self.hm = glow(name + "_h", hexc, soft=2.2, tail=tail, whiten=0.25)
        self.core = Strip(S, name + "_c", self.cm, noink=True, grp=grp)
        self.halo = Strip(S, name + "_h", self.hm, noink=True, grp=grp)
        self.cw, self.hw, self.ha = core_w, halo_w, halo_a

    def set(self, pts, widths, alpha=1.0, side=None):
        if alpha <= 0.003 or len(pts) < 2:
            self.core.set([], [0]); self.halo.set([], [0]); return
        self.core.set(pts, [w * self.cw for w in widths], side)
        self.halo.set(pts, [w * self.hw for w in widths], side)
        setv(self.cm, "alpha", min(1.0, alpha)); setv(self.hm, "alpha", min(1.0, alpha * self.ha))


def setv(mat, node, v):
    mat.node_tree.nodes[node].outputs[0].default_value = v


# ---------------------------------------------------------------- 도형
def mesh_ob(name, bm, mat, smooth=True):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    ob = bpy.data.objects.new(name, me); ob.data.materials.append(mat)
    return ob


def sphere(name, mat, r=0.5, sub=3):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)
    return mesh_ob(name, bm, mat)


def cone(name, mat, r1, r2, h, seg_=12):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg_, radius1=r1, radius2=r2, depth=h)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, h / 2))
    return mesh_ob(name, bm, mat, smooth=False)


def disk(name, mat, r, seg_=40):
    bm = bmesh.new(); bmesh.ops.create_circle(bm, cap_ends=True, segments=seg_, radius=r)
    return mesh_ob(name, bm, mat, smooth=False)


def ring(name, mat, r, w, seg_=48):
    bm = bmesh.new()
    vs = []
    for i in range(seg_):
        a = TAU * i / seg_
        vs.append((bm.verts.new((math.cos(a) * (r - w / 2), math.sin(a) * (r - w / 2), 0)), bm.verts.new((math.cos(a) * (r + w / 2), math.sin(a) * (r + w / 2), 0))))
    for i in range(seg_):
        j = (i + 1) % seg_
        bm.faces.new((vs[i][0], vs[j][0], vs[j][1], vs[i][1]))
    return mesh_ob(name, bm, mat, smooth=False)


def lumpy(name, mat, seed, sub=3):
    rnd = random.Random(seed)
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=0.5)
    lobes = [(Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1))).normalized(), rnd.uniform(0.18, 0.34), rnd.uniform(0.35, 0.6)) for _ in range(rnd.randint(4, 6))]
    for v in bm.verts:
        d = v.co.normalized(); bulge = 0.0
        for (c, h, w) in lobes:
            bulge = max(bulge, h * max(0.0, (d.dot(c) - (1 - w)) / w) ** 1.5)
        v.co = d * 0.5 * (0.82 + bulge + 0.05 * noise.noise(d * 2.2 + Vector((seed, 0, 0))))
    return mesh_ob(name, bm, mat)


def flame(name, mat, seed):
    """불꽃 혀: 아래가 둥글고 위로 뾰족 (지름 1, 높이 약 1.6, 바닥 z=0)"""
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=3, radius=0.5)
    for v in bm.verts:
        z = v.co.z + 0.5
        k = 1 - 0.92 * max(0.0, (z - 0.25) / 0.75) ** 0.9
        w = 0.06 * noise.noise(Vector((v.co.x * 3, v.co.y * 3, seed)))
        v.co = Vector((v.co.x * k + w, v.co.y * k, z * 1.6))
    return mesh_ob(name, bm, mat)


def spike(name, mat, w=0.06, L=1.0):
    """+Z로 뻗는 가는 가시"""
    bm = bmesh.new(); ring_ = []
    for k in range(8):
        a = TAU * k / 8; r = w if k % 2 == 0 else w * 0.45
        ring_.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0.12 * L)))
    tip = bm.verts.new((0, 0, L)); base = bm.verts.new((0, 0, 0))
    for k in range(8):
        l = (k + 1) % 8
        bm.faces.new((ring_[k], ring_[l], tip)); bm.faces.new((ring_[l], ring_[k], base))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return mesh_ob(name, bm, mat, smooth=False)


class Strip:
    """매 프레임 다시 짓는 리본 띠. pts = 머리 → 꼬리. side=None이면 바닥에 눕힘(위 = +Z와 직교), Vector면 그 방향으로 폭"""
    def __init__(self, S, name, mat, noink=False, grp=None):
        self.ob = bpy.data.objects.new(name, bpy.data.meshes.new(name)); self.ob.data.materials.append(mat)
        S.link(self.ob, noink, grp)

    def set(self, pts, widths, side=None):
        bm = bmesh.new(); uvl = bm.loops.layers.uv.new()
        n = len(pts)
        if n < 2 or max(widths) <= 1e-4:
            bm.to_mesh(self.ob.data); bm.free(); self.ob.hide_render = True; return
        self.ob.hide_render = False
        rows = []
        for i, (p, w) in enumerate(zip(pts, widths)):
            tng = (pts[min(n - 1, i + 1)] - pts[max(0, i - 1)])
            if side is None:
                sd = tng.cross(Vector((0, 0, 1)))
                if sd.length < 1e-6:
                    sd = Vector((1, 0, 0))
            elif isinstance(side, list):
                sd = Vector(side[i])                              # 점마다 폭 방향 (초승달 = 바깥쪽)
            else:
                sd = Vector(side)
            sd = sd.normalized() * (w / 2)
            rows.append((bm.verts.new(p + sd), bm.verts.new(p - sd)))
        for i in range(n - 1):
            f = bm.faces.new((rows[i][0], rows[i + 1][0], rows[i + 1][1], rows[i][1]))
            u0, u1 = i / (n - 1), (i + 1) / (n - 1)
            for lp, uv in zip(f.loops, ((u0, 0), (u1, 0), (u1, 1), (u0, 1))):
                lp[uvl].uv = uv
        bm.to_mesh(self.ob.data); bm.free()


def make_cat(S, name, body_stops, loc=(0, 0, 0), noink=False, blend=False, grp=None, actor=False, facing=0.0):
    """고양이 자리 실루엣: 몸통 · 머리 · 귀 2 · 카우보이 모자 · 꼬리. 반환 = (빈 부모, [재질])"""
    root = bpy.data.objects.new(name, None); S.sc.collection.objects.link(root)
    root.location = loc; root.rotation_euler = (0, 0, facing)
    m = toon(name + "_m", body_stops, blend)
    parts = []
    b = sphere(name + "_body", m, 0.5); b.scale = (0.62, 0.52, 0.78); b.location = (0, 0, 0.42); parts.append(b)
    h = sphere(name + "_head", m, 0.27); h.location = (0, -0.02, 0.98); parts.append(h)
    for sx in (-1, 1):
        e = cone(name + f"_ear{sx}", m, 0.1, 0.0, 0.2, 6); e.location = (0.15 * sx, 0, 1.13); e.rotation_euler = (0, 0.35 * sx, 0); parts.append(e)
    br = cone(name + "_brim", m, 0.33, 0.33, 0.03, 24); br.location = (0, 0.02, 1.2); br.scale = (1, 0.8, 1); parts.append(br)
    cr = cone(name + "_crown", m, 0.17, 0.13, 0.17, 16); cr.location = (0, 0.02, 1.21); parts.append(cr)
    t = cone(name + "_tail", m, 0.07, 0.02, 0.55, 8); t.location = (0, 0.3, 0.25); t.rotation_euler = (-0.9, 0, 0); parts.append(t)
    for p in parts:
        p.parent = root
        S.link(p, noink, grp, actor)
    return root, m


def make_enemy(S, name, loc, seed=1):
    m = toon(name + "_m", [(0.0, "B04A55"), (0.3, "7A2E3A"), (0.62, "4A1824"), (0.85, "26101A")])
    root = bpy.data.objects.new(name, None); S.sc.collection.objects.link(root); root.location = loc
    b = lumpy(name + "_b", m, seed); b.scale = (0.75, 0.65, 1.15); b.location = (0, 0, 0.6); b.parent = root; S.link(b, actor=True)
    em = flat(name + "_eye", "FFE07A")
    for sx in (-1, 1):
        e = sphere(name + f"_e{sx}", em, 0.06, 2); e.location = (0.12 * sx, -0.3, 0.85); e.parent = root; S.link(e, actor=True)
    return root, m


def ground(S):
    m = bpy.data.materials.new("ground"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexNoise"); tex.inputs["Scale"].default_value = 4.0
    ramp = _ramp(nt, bands([(0.0, "54432F"), (0.5, "5A4737"), (0.66, "5F4B3A")]))
    nt.links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
    _finish(m, nt, ramp.outputs["Color"])
    bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=14)
    g = mesh_ob("ground", bm, m, smooth=False); S.link(g, noink=True, actor=True)
    # 판자 이음 줄 (서부 바닥 느낌, 아주 옅게)
    lm = flat("plank", "4A392C")
    for i in range(-6, 8):
        bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=1)
        p = mesh_ob(f"plank{i}", bm, lm, smooth=False); p.scale = (14, 0.012, 1); p.location = (0, i * 0.9 + 0.3, 0.002)
        S.link(p, noink=True, actor=True)


def shadow(S, loc, r=0.45):
    m = flat("shadow", "1A1210", blend=True); setv(m, "alpha", 0.45)
    d = disk("shadow", m, r); d.scale = (1, 0.8, 1); d.location = (loc[0], loc[1], 0.004)
    S.link(d, noink=True, actor=True)
    return d


CAT = [(0.0, "F6C27A"), (0.3, "E39A4A"), (0.6, "B5652C"), (0.86, "6B3618")]


# ================================================================ 1. 장의사 정장
def fx_undertaker():
    S = Scene(aim=(1.15, 0.8, 0.4)); ground(S)
    C = Vector((0, 0, 0)); E = Vector((2.3, 1.6, 0))
    cat, _ = make_cat(S, "cat", CAT, C, actor=True, facing=0.6); shadow(S, C)
    en, enm = make_enemy(S, "en", E, 3); esh = shadow(S, E, 0.5)
    T_EXE, T_FLY, T_ARR = 0.15, 0.28, 0.72
    INK = [(0.0, "5B4A6E"), (0.25, "2E2440"), (0.55, "17121F"), (0.85, "08060B")]
    # 처형: 적이 하얗게 번쩍 → 짜부라짐 + 잉크 덩어리가 터지며 녹음
    def pose_enemy(t):
        k = seg(t, T_EXE, T_EXE + 0.18)
        setv(enm, "heat", 1.0 if t < T_EXE else 0.0 if t < T_EXE + 0.05 else 1.0)
        en.scale = (1 + 0.4 * k, 1 + 0.4 * k, max(0.001, 1 - k))
        en.hide_render = k >= 1
        for c in en.children:
            c.hide_render = k >= 1
        esh.hide_render = k >= 1
    S.poses.append(pose_enemy)
    flash = flat("exe_flash", "FFFFFF"); fl = sphere("exe_fl", flash, 0.5); fl.location = E + Vector((0, 0, 0.6)); S.link(fl, grp="ink")
    def pose_fl(t):
        k = seg(t, T_EXE, T_EXE + 0.1)
        fl.hide_render = not (T_EXE <= t < T_EXE + 0.1)
        fl.scale = Vector((1, 1, 1)) * (0.5 + 1.0 * k)
        setv(flash, "dissolve", k * 0.8)
    S.poses.append(pose_fl)
    rnd = random.Random(5); inks = []
    for i in range(7):
        m = toon(f"ink{i}", INK); o = lumpy(f"ink{i}", m, 40 + i); S.link(o, grp="ink")
        d = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(0.2, 1))).normalized()
        inks.append((o, m, d, rnd.uniform(0.35, 0.55), rnd.uniform(0, 0.06)))
    def pose_ink(t):
        for o, m, d, s, lag in inks:
            k = seg(t, T_EXE + 0.04 + lag, T_EXE + 0.5)
            o.hide_render = k <= 0 or k >= 1
            o.location = E + Vector((0, 0, 0.5)) + d * 0.9 * ease_out(k, 2)
            o.scale = Vector((1, 1, 1.3)) * s * 1.4 * ease_out(min(1, k / 0.3)) * (1 - 0.5 * k)
            setv(m, "heat", 1 - k); setv(m, "dissolve", max(0, (k - 0.35) / 0.65) * 0.6)
    S.poses.append(pose_ink)
    # 영혼 줄기 3가닥: 적 가슴 → 휘어 → 고양이 가슴. 머리에 밝은 구슬, 꼬리는 잉크
    SOUL = [(0.0, "F2FFF8"), (0.2, "A8F0D0"), (0.45, "4FC3A0"), (0.7, "1F6E62"), (0.9, "0E2A2A")]
    src = E + Vector((0, 0, 0.7)); dst = C + Vector((0, 0, 0.62))
    streams = []
    for i, (side_, up, lag) in enumerate(((-1.5, 0.6, 0.0), (0.2, 1.5, 0.05), (1.4, 0.4, 0.09))):
        st = Strip(S, f"soul{i}", strip(f"soul{i}", SOUL), grp="soul")
        hm = flat(f"soulhead{i}", "F2FFF8"); hd = sphere(f"soulhead{i}", hm, 0.13, 2); S.link(hd, grp="soul")
        mid = (src + dst) / 2
        perp = (dst - src).cross(Vector((0, 0, 1))).normalized()
        ctrl = mid + perp * side_ + Vector((0, 0, up))
        streams.append((st, hd, ctrl, lag))
    def bez(a, c, b, u):
        return a * (1 - u) ** 2 + c * 2 * u * (1 - u) + b * u * u
    def pose_soul(t):
        for st, hd, ctrl, lag in streams:
            k = seg(t, T_FLY + lag, T_ARR + lag * 0.5)
            head = ease_io(k) ** 1.4
            tail = max(0.0, head - 0.45) if k < 1 else 1.0
            if t > T_ARR + lag * 0.5:                     # 꼬리까지 빨려 들어감
                tail = min(1.0, head - 0.45 + seg(t, T_ARR + lag * 0.5, T_ARR + 0.15 + lag * 0.5) * 0.45)
            if k <= 0 or tail >= 0.999:
                st.set([], [0]); hd.hide_render = True; continue
            n = 18
            pts = [bez(src, ctrl, dst, head - (head - tail) * j / (n - 1)) for j in range(n)]
            wob = [Vector((0, 0, 0.05 * math.sin(9 * t + j * 0.7))) for j in range(n)]
            pts = [p + w for p, w in zip(pts, wob)]
            ws = [0.34 * (1 - j / (n - 1)) ** 0.8 + 0.015 for j in range(n)]
            st.set(pts, ws)
            hd.hide_render = head >= 0.999
            hd.location = pts[0]
    S.poses.append(pose_soul)
    # 도착: 고양이 몸이 박하색으로 번쩍 + 위로 튀는 작은 불씨 + 바닥 고리가 안으로 조임
    HEAL = [(0.0, "F2FFF8"), (0.3, "A8F0D0"), (0.6, "4FC3A0"), (0.85, "1F6E62")]
    hm = toon("heal_shell", HEAL, blend=True); shell = sphere("heal_shell", hm, 0.5); shell.location = C + Vector((0, 0, 0.6)); shell.scale = (0.8, 0.8, 1.2)
    S.link(shell, noink=True, grp="heal")
    rm = strip("heal_ring", [(0.0, "F2FFF8"), (0.4, "8FE3C0"), (0.75, "2E8F7A")], along=0, across=1.0)
    rg = Strip(S, "heal_ring", rm, grp="heal")
    sparks = []
    for i in range(7):
        m = toon(f"hs{i}", HEAL); o = spike(f"hs{i}", m, 0.05, 0.4); S.link(o, grp="heal")
        a = TAU * i / 7 + 0.3
        sparks.append((o, m, a, 0.25 + 0.1 * (i % 3), 0.02 * i))
    def pose_heal(t):
        k = seg(t, T_ARR, T_ARR + 0.3)
        shell.hide_render = not (0 < k < 1)
        shell.scale = Vector((0.8, 0.8, 1.2)) * (1 + 0.35 * ease_out(k))
        setv(hm, "alpha", 0.55 * (1 - k))
        kr = seg(t, T_ARR - 0.12, T_ARR + 0.05)
        if 0 < kr < 1:
            r = 1.1 * (1 - ease_io(kr)) + 0.3
            pts = [Vector((math.cos(a) * r, math.sin(a) * r, 0.03)) for a in [TAU * j / 40 for j in range(41)]]
            rg.set(pts, [0.07 * (1 - kr) + 0.03] * 41)
        else:
            rg.set([], [0])
        for o, m, a, r, lag in sparks:
            ks = seg(t, T_ARR + 0.03 + lag, T_ARR + 0.4 + lag)
            o.hide_render = not (0 < ks < 1)
            o.location = C + Vector((math.cos(a) * r, math.sin(a) * r, 0.3 + 1.0 * ease_out(ks, 2)))
            o.scale = (1, 1, 1.0 - 0.6 * ks)
            setv(m, "heat", 1 - ks); setv(m, "dissolve", max(0, ks - 0.5))
    S.poses.append(pose_heal)
    return S, 1.3, [("undertaker_ink", ["ink"], E + Vector((0, 0, 0.6)), 2.6, T_EXE, T_EXE + 0.5, 12, False),
                    ("undertaker_heal", ["heal"], C + Vector((0, 0, 0.7)), 2.4, T_ARR - 0.12, T_ARR + 0.45, 12, False)]


# ================================================================ 2. 거울 망토
def fx_mirror():
    S = Scene(ink=(0.1, 0.05, 0.16), aim=(1.5, 1.7, 0.4)); ground(S)
    C0 = Vector((0, 0, 0)); C1 = Vector((-1.4, -0.4, 0))
    cat, _ = make_cat(S, "cat", CAT, C0, actor=True, facing=0.4); csh = shadow(S, C0)
    A, am = make_enemy(S, "atk", Vector((1.1, 1.1, 0)), 3); shadow(S, (1.1, 1.1, 0), 0.5)          # 공격해 온 적
    T, tm = make_enemy(S, "tgt", Vector((3.2, 2.6, 0)), 7); shadow(S, (3.2, 2.6, 0), 0.5)          # 무작위로 고른 적
    O, om = make_enemy(S, "oth", Vector((-0.6, 3.0, 0)), 9); shadow(S, (-0.6, 3.0, 0), 0.5)
    T_DODGE = 0.12
    SH = [(0.0, "8A63D6"), (0.12, "3A2463"), (0.4, "1A102E"), (0.75, "07040C")]
    # 적의 공격 예고 (주황 선) → 고양이가 옆으로 빠짐
    wm = strip("warn", [(0.0, "FFB347"), (0.6, "E06A1E")], along=0.3, across=0.6, blend=True)
    warn = Strip(S, "warn", wm, noink=True)
    def pose_cat(t):
        k = ease_out(seg(t, T_DODGE, T_DODGE + 0.14), 3)
        p = C0.lerp(C1, k); cat.location = p; csh.location = (p.x, p.y, 0.004)
        cat.rotation_euler = (0, -0.5 * math.sin(k * math.pi), 0.4)
        if t < T_DODGE + 0.05:
            a = 1 - seg(t, T_DODGE, T_DODGE + 0.05)
            pts = [Vector((1.1, 1.1, 0.03)).lerp(Vector((-0.2, -0.2, 0.03)), j / 9) for j in range(10)]
            warn.set(pts, [0.18] * 10); setv(wm, "alpha", 0.85 * a)
        else:
            warn.set([], [0])
    S.poses.append(pose_cat)
    # 극한 회피 섬광: 제자리에서 퍼지는 파란 고리
    dm = strip("dodge_ring", [(0.0, "F0FAFF"), (0.45, "8FD3FF"), (0.8, "2F7FD6")], along=0, across=1.0)
    dr = Strip(S, "dodge_ring", dm)
    def pose_dr(t):
        k = seg(t, T_DODGE, T_DODGE + 0.22)
        if 0 < k < 1:
            r = 0.3 + 1.1 * ease_out(k)
            dr.set([Vector((math.cos(a) * r, math.sin(a) * r, 0.04)) for a in [TAU * j / 48 for j in range(49)]], [0.12 * (1 - k) + 0.01] * 49)
        else:
            dr.set([], [0])
    S.poses.append(pose_dr)
    # ---- 2026-11-24 (v4) 사용자: 대상에게 자객 잔상이 떠서 벤다 (세피리아 혼령봉인연구회 제2형)
    #   극한 회피 한 번 = 범위 안 무작위 적 최대 3명, 한 대상엔 자객 한 명씩 (살짝 엇박으로 동시에)
    #   자객 = 모델 그대로(어둡게 물든 질감 + 보라 테) · 스쿼시 앤 스트레치(피어남 → 움츠림 → 폭발 가속 · 늘어남 → 도착 찌그러짐 · 반동)
    #   돌진은 선 대신 잔상으로 (같은 시간 간격 → 출발 쪽은 벌어지고 끝쪽은 촘촘 = 가감속) · 리본 꼬리 초승달 베기 · 섬광 가시 · 꼬리 달린 불똥
    GS = 1.35
    T_APPEAR0 = 0.24
    A_ANT, A_DASH, A_ARR, A_END = 0.12, 0.26, 0.36, 0.74          # 자객마다 피어난 뒤 시각
    rnd = random.Random(8)
    UP = Vector((0, 0, 1)); RT = cam_right(); BKD = (CAM_AIM - CAM_LOC).normalized()

    def dash_u(k):                                               # 폭발적으로 출발 → 길게 감속
        return 1 - (1 - k) ** 4

    def dash_v(k):
        return (1 - k) ** 3

    # 모델: Hyper3D GLB (질감 포함) — blender_assassin_pose.py와 같은 정규화 (움츠린 자세 높이 0.8)
    def load_glb(pose, scale):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=f"C:/Project/Cat_Tang/Art/models/incoming/hyper3d_assassin/{pose}/base.glb")
        obs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
        img = None
        for o in obs:
            for ms in o.material_slots:
                if ms.material and ms.material.node_tree:
                    for n in ms.material.node_tree.nodes:
                        if n.type == 'TEX_IMAGE' and n.image:
                            img = n.image
        ws = []
        for o in obs:
            ws += [o.matrix_world @ v.co for v in o.data.vertices]
        mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
        mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
        if scale is None:
            scale = 0.8 / (mx.z - mn.z)
        c = (mn + mx) / 2
        for o in obs:
            mw = o.matrix_world.copy()
            for v in o.data.vertices:
                p = mw @ v.co
                v.co = Vector(((p.x - c.x) * scale, (p.y - c.y) * scale, (p.z - mn.z) * scale))
            o.parent = None
            o.matrix_world.identity()
        for o in list(bpy.data.objects):
            if o not in before and o.type != 'MESH':
                bpy.data.objects.remove(o)
        ob = obs[0]
        if len(obs) > 1:
            bpy.ops.object.select_all(action='DESELECT')
            for o in obs:
                o.select_set(True)
            bpy.context.view_layer.objects.active = ob
            bpy.ops.object.join()
        for col in list(ob.users_collection):
            col.objects.unlink(ob)
        return ob, img, scale

    def ghost_tex(name, img):
        """자객 질감을 어둡게 물들이고(눈 같은 밝은 곳만 살짝 남음) 가장자리에 보라 테 — 모델 형태가 읽히는 그림자"""
        m = bpy.data.materials.new(name); m.use_nodes = True
        nt = m.node_tree; nt.nodes.clear()
        tx = nt.nodes.new("ShaderNodeTexImage"); tx.image = img
        gm = nt.nodes.new("ShaderNodeGamma"); gm.inputs["Gamma"].default_value = 1.7
        nt.links.new(tx.outputs["Color"], gm.inputs["Color"])
        tint = nt.nodes.new("ShaderNodeMix"); tint.data_type = 'RGBA'; tint.blend_type = 'MULTIPLY'
        tint.inputs["Factor"].default_value = 1.0; tint.inputs["B"].default_value = (*lin("6A55A8"), 1)
        nt.links.new(gm.outputs["Color"], tint.inputs["A"])
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.3
        rim = _math(nt, 'POWER', lw.outputs["Facing"], 2.5)
        rim = _math(nt, 'MULTIPLY', rim, 1.3, True)
        mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = 'RGBA'
        nt.links.new(rim, mix.inputs["Factor"]); nt.links.new(tint.outputs["Result"], mix.inputs["A"])
        mix.inputs["B"].default_value = (*lin("B394FF"), 1)
        # 디졸브 (2026-11-24 사용자: 연기 말고 디졸브로 나타남, 레퍼런스처럼) — 노이즈 > dissolve 인 곳만 남고, 경계 띠는 밝은 보라 광
        nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 9.0; nz.inputs["Detail"].default_value = 4.0
        dis = nt.nodes.new("ShaderNodeValue"); dis.name = "dissolve"; dis.outputs[0].default_value = 0.0
        keep = _math(nt, 'GREATER_THAN', nz.outputs["Fac"], dis.outputs[0])
        edge_hi = _math(nt, 'ADD', dis.outputs[0], 0.07)
        edge = _math(nt, 'LESS_THAN', nz.outputs["Fac"], edge_hi)
        edge = _math(nt, 'MULTIPLY', edge, _math(nt, 'GREATER_THAN', dis.outputs[0], 0.001))
        ec = nt.nodes.new("ShaderNodeMix"); ec.data_type = 'RGBA'
        nt.links.new(edge, ec.inputs["Factor"]); nt.links.new(mix.outputs["Result"], ec.inputs["A"])
        ec.inputs["B"].default_value = (*lin("E2D4FF"), 1)
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(ec.outputs["Result"], em.inputs["Color"])
        al = nt.nodes.new("ShaderNodeValue"); al.name = "alpha"; al.outputs[0].default_value = 1.0
        a_ = _math(nt, 'MULTIPLY', keep, al.outputs[0])
        tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mx_ = nt.nodes.new("ShaderNodeMixShader")
        nt.links.new(a_, mx_.inputs["Fac"]); nt.links.new(tr.outputs[0], mx_.inputs[1]); nt.links.new(em.outputs[0], mx_.inputs[2])
        nt.links.new(mx_.outputs[0], out.inputs["Surface"])
        try:
            m.surface_render_method = 'BLENDED'
        except Exception:
            m.blend_method = 'BLEND'
        return m

    SHM = [(0.0, "0B0713"), (0.5, "1A1030"), (0.72, "3A2466"), (0.86, "8A66E0")]   # 돌진 잔상 = 검은 몸 · 보라 테
    # 2026-11-24 사용자: 칼 휘두르는 동작을 리깅해서 연결 · 동작과 이펙트가 이어지게
    #   자객 = 리깅 모델(Art/tools/blender_assassin_rig.py: 세움 · 키 0.8 · 오른손 IK 목표) — 움츠린 자세(쉬는 자세)에서 오른손 IK 목표를 반달 길로 휘두름
    RIG_D = r"C:/Project/Cat_Tang/Art/models/incoming/hyper3d_assassin/rigged"
    with bpy.data.libraries.load(RIG_D + "/assassin_rig.blend") as (src_, dst_):
        dst_.objects = list(src_.objects)
    lib = [o for o in dst_.objects if o]
    rig0 = next(o for o in lib if o.type == 'ARMATURE')
    rmesh0 = max((o for o in lib if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
    rimg = bpy.data.images.load(RIG_D + "/shaded.png")
    S_LOCAL = rig0.data.bones["RightArm"].head_local.copy()     # 오른 어깨 (모델 기준)
    REST_HAND = Vector(rig0["rest_hand"])
    L_ARM = (REST_HAND - S_LOCAL).length
    D0 = (REST_HAND - S_LOCAL).normalized()                      # 쉬는 자세: 칼 든 손이 머리 뒤 위
    DM = Vector((0.8, 0.1, -0.95)).normalized()                  # 가운데: 칼 든 쪽 아래로 크게 내리쳐 (몸 옆 · 아래)
    D1 = Vector((0.05, -1.0, 0.18)).normalized()                 # 끝: 아래에서 감아 올라 눈앞에서 멈춤 (사용자: 이펙트가 눈앞까지 이어져 종결)
    EXT = 0.55                                                   # 참격이 칼끝 너머로 뻗는 길이 (모델 단위, 팔 길이 0.165)

    def make_rig(idx):
        rg = rig0.copy(); rg.data = rig0.data
        me = rmesh0.copy(); me.data = rmesh0.data; me.parent = rg
        for md in me.modifiers:
            if md.type == 'ARMATURE':
                md.object = rg
        mat = ghost_tex(f"ar{idx}", rimg)
        me.material_slots[0].link = 'OBJECT'; me.material_slots[0].material = mat
        S.link(rg, noink=True, grp="shade"); S.link(me, noink=True, grp="shade")
        tg = bpy.data.objects.new(f"ht{idx}", None); S.sc.collection.objects.link(tg)
        for c in rg.pose.bones["RightForeArm"].constraints:
            if c.type == 'IK':
                c.target = tg
        return rg, me, mat, tg

    # 대상 3 (범위 안 무작위) — 대상마다 자객 하나, 엇박 0.07초
    targets = [(T, tm, Vector((3.2, 2.6, 0)), Vector((-1.0, 0.25, 0))),
               (A, am, Vector((1.1, 1.1, 0)), Vector((0.9, 0.6, 0))),
               (O, om, Vector((-0.6, 3.0, 0)), Vector((0.3, -1.0, 0)))]
    if "--closeup" in sys.argv:
        targets = targets[:1]                                    # 가까이 보기: 자객 하나만 (사용자: 잔상이 왜 두 개냐)
    assassins = []
    for idx, (eo, em_, ep, side) in enumerate(targets):
        side = side.normalized()
        a = dict(eo=eo, em=em_, ep=ep, t0=T_APPEAR0 + 0.07 * idx, tilt=1.0 if idx % 2 == 0 else -1.0, idx=idx)
        a["PA"] = ep + side * 1.5; a["PB"] = ep - side * 1.35     # 2026-11-24 사용자 그림(3): 분신이 적을 완전히 통과해 베어 넘김 (반대편까지 돌진)
        a["DIR"] = (a["PB"] - a["PA"]).normalized()
        a["YAW"] = math.atan2(a["DIR"].x, -a["DIR"].y)           # 모델 앞 = -Y
        a["HC"] = ep + Vector((0, 0, 0.75))
        a["rig"], a["rme"], a["rmat"], a["htg"] = make_rig(idx)
        a["snaps"] = {}                                          # 잔상 = 그 순간 자세를 굳힌 메쉬
        assassins.append(a)

    SW_R0, SW_R1 = A_DASH + 0.004, A_ARR + 0.05                  # 휘두름: 적에 닿기 직전 시작 → 통과해 멈출 때 눈앞에서 끝남 (그 전엔 칼을 뒤로 든 채)
    HS_T, HS = A_DASH + 0.019, 0.05                              # 히트스톱 = 몸이 적을 통과하는 순간 (돌진 u≈0.57)

    def hs(r):
        return r if r < HS_T else (HS_T if r < HS_T + HS else r - HS)

    def pos_at(a, r):                                            # r = 자객이 피어난 뒤 시각
        PA, PB, D = a["PA"], a["PB"], a["DIR"]
        if r < A_ANT:
            return PA.copy()
        if r < A_DASH:                                           # 예비동작: 뒤로 살짝 물러남
            return PA - D * 0.28 * ease_io(seg(r, A_ANT, A_DASH))
        if r < A_ARR:
            return (PA - D * 0.28).lerp(PB, dash_u(seg(r, A_DASH, A_ARR)))
        tau = r - A_ARR                                          # 도착 뒤 미끄러졌다 되돌아옴
        return PB + D * 0.22 * math.exp(-9 * tau) * math.sin(14 * tau + 0.6)

    def squash_at(r):
        """(앞뒤 늘어남, 위아래) — 옆은 부피 유지 비슷하게 1/√"""
        if r < A_ANT:                                            # 피어남: 납작 → 툭 솟음 → 제자리
            k = seg(r, 0, A_ANT)
            return 1.0, 0.8 + 0.2 * ease_out(k, 3) + 0.08 * math.sin(math.pi * k)
        if r < A_DASH:                                           # 움츠림: 낮고 넓게
            k = ease_io(seg(r, A_ANT, A_DASH))
            return 1.0 + 0.08 * k, 1 - 0.2 * k
        if r < A_ARR:                                            # 돌진: 속도만큼 앞뒤로 늘어남
            v = dash_v(seg(r, A_DASH, A_ARR))
            return 1 + 0.6 * v, 1 - 0.15 * v
        tau = r - A_ARR                                          # 도착: 찌그러졌다 튕기며 제자리
        w = math.exp(-11 * tau) * math.cos(24 * tau)
        return 1 - 0.25 * w, 1 - 0.28 * w

    def sq_scale(r):
        st, sz = squash_at(r)
        side_ = 1 / math.sqrt(max(0.2, st * sz))
        return Vector((GS * side_, GS * st, GS * sz))                # 로컬 Y = 앞뒤

    def apply_sq(o, a, r):
        o.location = pos_at(a, r); o.rotation_euler = (0, 0, a["YAW"]); o.scale = sq_scale(r)


    def hand_dir(u):
        v = D0 * (1 - u) ** 2 + DM * (2 * u * (1 - u)) + D1 * (u * u)
        return v.normalized()

    def tip_path(a, v):
        """칼끝 길 (사용자 그림(3) 빨간 선): 적 너머 출발 쪽 위 → 적 앞(카메라 쪽)으로 내리쳐 → 아래를 가로질러 → 멈춘 분신 눈앞으로 감아 올라 끝 (3차 베지어, v 0 → 1)"""
        D = a["DIR"]; HCp = a["HC"]
        NC = -Vector((BKD.x, BKD.y, 0)).normalized()             # 카메라 쪽 (화면 아래)
        P0 = HCp - D * 0.75 - NC * 0.55 + Vector((0, 0, 0.55))
        P1 = HCp - D * 1.05 + NC * 1.35 - Vector((0, 0, 0.45))
        P2 = HCp + D * 0.95 + NC * 1.45 - Vector((0, 0, 0.45))
        P3 = a["PB"] + D * 0.55 + Vector((0, 0, 0.75))
        w = 1 - v
        return P0 * w ** 3 + P1 * (3 * w * w * v) + P2 * (3 * w * v * v) + P3 * v ** 3

    def rig_points(a, r):
        """그 순간 (손 · 칼끝 · 어깨) 월드. 돌진 중엔 칼을 뒤로 든 채(쉬는 자세) → 휘두름 동안 칼끝은 tip_path를 따르고 팔은 칼끝 쪽으로 뻗음"""
        M = Matrix.LocRotScale(pos_at(a, r), Euler((0, 0, a["YAW"])), sq_scale(r))
        Sw = M @ S_LOCAL
        if r < SW_R0:
            d = (M @ (S_LOCAL + D0) - Sw).normalized()
            return Sw + d * (L_ARM * GS), Sw + d * ((L_ARM + EXT) * GS), Sw
        v = 1 - (1 - seg(r, SW_R0, SW_R1)) ** 2.2                # 관성: 폭발 출발 → 길게 감속, 한 방향만
        Tt = tip_path(a, v)
        d = (Tt - Sw).normalized()
        return Sw + d * (L_ARM * GS), Tt, Sw

    def set_pose(a, r):
        apply_sq(a["rig"], a, r)
        a["htg"].location = rig_points(a, r)[0]

    def snapshot(a, r, key, mat):
        """r 순간 자세를 굳힌 메쉬 (잔상)"""
        set_pose(a, r); bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        ev = a["rme"].evaluated_get(dg)
        mesh = bpy.data.meshes.new_from_object(ev)
        o = bpy.data.objects.new(f"snap{a['idx']}_{key}", mesh); o.matrix_world = a["rme"].matrix_world.copy()
        o.data.materials.clear(); o.data.materials.append(mat)
        S.link(o, noink=True, grp="shade")
        a["snaps"][key] = (o, mat)
        return o

    GH = [A_DASH + 0.004 + (A_ARR - A_DASH) * 0.8 * (i + 0.5) / 5 for i in range(5)]   # 돌진 동안 같은 시간 간격 = 가감속 따라 벌어짐 (옅게)

    def pose_assassins(t):
        for a in assassins:
            r = hs(t - a["t0"])
            # 잔상: 지나간 순간이면 (처음 한 번) 그 자세를 굳혀 둠
            for i, rg_ in enumerate(GH):
                if r >= rg_ and i not in a["snaps"]:
                    snapshot(a, rg_, i, toon(f"dg{a['idx']}_{i}", SHM, blend=True))
            vis = 0 <= r < A_END
            a["rme"].hide_render = not vis
            if vis:
                set_pose(a, r)
                setv(a["rmat"], "alpha", 0.95)
                d_in = 0.95 * (1 - ease_out(seg(r, 0, 0.16), 2))          # 디졸브로 차오름
                d_out = 0.95 * ease_io(seg(r, A_END - 0.2, A_END))        # 디졸브로 사라짐
                setv(a["rmat"], "dissolve", max(d_in, d_out))
            for key, (o, m) in a["snaps"].items():
                rg_ = GH[key] if key != "w" else A_DASH - 0.03
                life = 0.22 if key != "w" else 0.28
                k = seg(r, rg_, rg_ + life)
                o.hide_render = not (r >= rg_ and k < 1)
                base = 0.7 if key == "w" else 0.12 + 0.04 * key
                setv(m, "alpha", base * (1 - k) ** 1.5)
    S.poses.append(pose_assassins)

    # 피어나는 자리: 검은 안개가 안쪽으로 빨려 듦
    gath = []
    for ai, a in enumerate(assassins):
        for i in range(6):
            gm_ = strip(f"gath{ai}_{i}", [(0.0, "8E6FD0"), (0.35, "3A2463"), (0.8, "0B0714")], along=0.9, across=0.6, blend=True)
            gath.append((Strip(S, f"gath{ai}_{i}", gm_, noink=True, grp="shade"), gm_, TAU * i / 6, a))

    def pose_gath(t):
        for st_, m, ang, a in gath:
            k = seg(t - a["t0"], -0.12, 0.08)
            if not (0 < k < 1):
                st_.set([], [0]); continue
            pts = []
            for j in range(6):
                kk = max(0.0, ease_io(k) - 0.07 * j)
                rr = 1.0 * (1 - kk) + 0.1
                aa = ang + 1.4 * kk
                pts.append(a["PA"] + Vector((math.cos(aa) * rr, math.sin(aa) * rr, 0.3 + 0.35 * (1 - kk))))
            st_.set(pts, [0.18 * (1 - j / 5) + 0.02 for j in range(6)]); setv(m, "alpha", 0.85 * math.sin(math.pi * k))
    # S.poses.append(pose_gath)   # 2026-11-24 사용자: 연기 말고 디졸브

    # 피한 자리의 고양이 그림자 (잠깐)
    cs_root, cs_m = make_cat(S, "cshade", SH, C0, noink=True, blend=True, grp="shade", facing=0.4)

    def pose_cshade(t):
        k = seg(t, T_DODGE, T_DODGE + 0.3)
        for c in cs_root.children:
            c.hide_render = not (0 < k < 1)
        setv(cs_m, "alpha", 0.7 * (1 - k))
    S.poses.append(pose_cshade)

    # ---- 칼날 초승달 (2026-11-24 사용자 그림 + 레퍼런스 생성 방식: 가는 획이 그어짐 → 굵은 초승달로 꽉 참 → 안쪽부터 갈라져 가는 선 · 조각으로 흩어짐)
    #   자객 앞을 감싸는 기울어진 원호를 칼이 휘두르는 만큼 그려 나감 · 자객 몸을 따라 같이 움직임
    def crescent_pts(C, D, B, R, head, tail, wmax, n=28):
        pts, ws, sides = [], [], []
        for j in range(n):
            q = j / (n - 1)                                      # 0 = 칼끝(머리), 1 = 꼬리
            ang = head + (tail - head) * q
            rad = D * math.cos(ang) + B * math.sin(ang)
            w = wmax * math.sin(math.pi * (0.04 + 0.96 * q)) ** 0.7 * (1 - 0.35 * q) + 0.006
            Rq = R * (0.78 + 0.22 * min(1.0, q / 0.35))           # 칼끝 쪽 반지름을 줄여 안으로 말린 갈고리 (레퍼런스)
            pts.append(C + rad * (Rq - w / 2))                   # 바깥 날 = 원 둘레에 맞춤 (안쪽으로 굵어짐)
            ws.append(w); sides.append(rad)
        return pts, ws, sides

    blades = []
    for ai, a in enumerate(assassins):
        PERP = Vector((-a["DIR"].y, a["DIR"].x, 0))
        m = slash_mat(f"blade{ai}")
        st_ = Strip(S, f"blade{ai}", m, noink=True, grp="shade")
        blades.append((st_, m, a, PERP))

    def swing_u(k):
        """휘두름 진행 (0 → 1): 살짝 뒤로 젖힘 → 폭발 가속 → 타격 지나 감속하며 넘어감(팔로우 스루) → 제자리"""
        if k < 0.15:
            return -0.08 * math.sin(math.pi * k / 0.15 * 0.5)                 # 예비동작
        if k < 0.55:
            q = (k - 0.15) / 0.4
            return -0.08 + 1.08 * q ** 2.2                                     # 가속 (끝에서 가장 빠름 = 타격)
        q = (k - 0.55) / 0.45
        return 1.0 + 0.16 * math.sin(math.pi * min(1, q * 1.3)) * (1 - q) + 0.06 * q   # 팔로우 스루 넘어감 → 조금 돌아옴

    def swing_speed(k):
        dk = 0.01
        return abs(swing_u(min(1, k + dk)) - swing_u(max(0, k - dk))) / (2 * dk)

    def erode_at(rc):
        """완성 뒤 시간 rc → erode (레퍼런스 타이밍: 꽉 참 0.07초 → 갈라짐 0.1초 → 선 0.32초 머묾 → 0.12초 사라짐)"""
        if rc < 0.15:                                             # 완성된 꽉 찬 초승달을 잠깐 보여 줌 (사용자: 완결 상태 더 오래)
            return 0.0
        if rc < 0.25:
            return 0.45 * ease_out((rc - 0.15) / 0.1, 2)
        if rc < 0.57:
            return 0.45 + 0.3 * (rc - 0.25) / 0.32
        return min(1.0, 0.75 + 0.25 * (rc - 0.57) / 0.12)

    # 2026-11-24 사용자: 돌진할 땐 칼을 뒤로 들고 있다가 관성으로 앞으로 베어 넘기고, 궤적이 자연스럽게 칼끝을 따라옴
    #   칼빛 = 휘두르는 동안 실제 칼끝(손 → 팔 방향으로 EXT)을 매 순간 기록한 띠 (랫키호테 평타) · 칼끝이 멈추면 그 자리에 남음
    #   한 방향으로만 휘둘러 접히지 않음 · 멈춘 자리 앞(대상)을 지나며 생김
    SL_R0, SL_R1 = SW_R0, SW_R1

    def pose_blades(t):
        for st_, m, a, PERP in blades:
            r = hs(t - a["t0"])
            rc = r - SL_R1
            e = erode_at(max(0.0, rc))
            if r < SL_R0 + 0.004 or e >= 1:
                st_.set([], [0]); continue
            rend = min(r, SL_R1)
            samp = []
            tau = SL_R0
            while tau <= rend + 1e-6:
                Hh, Tt, Sh = rig_points(a, tau)
                inn = (a["HC"] - Tt); inn.normalize()                    # 띠 안쪽 = 적 쪽 (U자 안쪽으로 굵어짐)
                samp.append((Tt, inn, 1.0))
                tau += 0.0006
            if len(samp) < 3:
                st_.set([], [0]); continue
            cum = [0.0]
            for i in range(1, len(samp)):
                cum.append(cum[-1] + (samp[i][0] - samp[i - 1][0]).length)
            Lt = cum[-1]
            if Lt < 0.02:
                st_.set([], [0]); continue
            done = seg(rc, -0.02, 0.06)
            K = 160; pts, ws, sides = [], [], []
            for kk in range(K + 1):
                q = 1 - kk / K                                       # 머리(지금 칼끝) → 꼬리(휘두르기 시작) 순서 (UV.x 0 = 칼끝)
                dd = q * Lt
                j = max(0, min(len(cum) - 2, bisect.bisect_right(cum, dd) - 1))
                span = cum[j + 1] - cum[j]; u_ = 0 if span < 1e-6 else max(0.0, min(1.0, (dd - cum[j]) / span))
                T0, i0_, b0 = samp[j]; T1, i1_, b1 = samp[j + 1]
                Tq = T0.lerp(T1, u_); inn = i0_.lerp(i1_, u_).normalized(); bl = b0 + (b1 - b0) * u_
                cap = math.sqrt(max(0.0, 1 - ((q - 0.94) / 0.06) ** 2)) if q > 0.94 else 1.0
                swing_prof = (0.06 + 0.94 * q ** 0.9) * cap          # 휘두르는 중: 지금 칼끝이 가장 굵음
                final_prof = math.sin(math.pi * min(1.0, q ** 1.35)) ** 0.75   # 완성: 시작 쪽 꼬리는 길고 가늘게 → 굵은 몸 → 끝 뾰족
                w = bl * 0.62 * (swing_prof * (1 - done) + final_prof * done) + 0.004
                sabs = dd * 5.0 + a["idx"] * 13.7                     # 찢긴 모양 (출발점 기준 거리 → 흔들리지 않음)
                n_in = noise.noise(Vector((sabs, 0.3, 1.7)))
                n_out = noise.noise(Vector((sabs * 1.7, 2.1, 0.4)))
                tear = noise.noise(Vector((sabs * 0.6, 5.3, 3.3)))
                cut = 0.35 if tear > 0.34 else 1.0
                w_in = w * (0.8 + 0.3 * n_in) * cut
                off = w * 0.08 * n_out
                pts.append(Tq - inn * off + inn * (w_in / 2) - BKD * 0.3); ws.append(max(0.004, w_in)); sides.append(-inn)   # 바깥 날(UV.y 0) = 칼끝 길
            st_.set(pts, ws, side=sides)
            setv(m, "erode", e)
            setv(m, "white", 1.0 if rc < 0 else 1 - seg(rc, 0.0, 0.06))
    S.poses.append(pose_blades)

    # ---- 타격 지점 X 베기 (같은 참격 재질 · 생성 방식, 획 가운데 = 타격 지점) — 칼날 초승달이 지나간 직후 엇갈린 한 획 더
    slashes = []
    for ai, a in enumerate(assassins):
        for si in range(2):
            m = slash_mat(f"xs{ai}_{si}")
            slashes.append((Strip(S, f"xs{ai}_{si}", m, noink=True, grp="slash"), m, a, A_DASH + 0.02 + 0.07 * si, a["tilt"] * (1 if si == 0 else -1)))

    def pose_slash(t):
        for st_, m, a, r0, tilt in slashes:
            r = t - a["t0"]
            k = seg(r, r0, r0 + 0.08)
            e = seg(r, r0 + 0.1, r0 + 0.32)
            if r < r0 or e >= 1:
                st_.set([], [0]); continue
            D = (RT * 1.0 + UP * (-tilt)).normalized()           # 화면 평면 대각선
            N = (UP * 1.0 + RT * tilt).normalized()
            R = 1.5
            C = a["HC"] - N * (R - 0.25)                         # 원호 가운데가 타격 지점을 지나게
            head = -1.0 + 2.0 * ease_out(k, 2.4); tail = head - 2.0 * ease_out(min(1, k * 1.5), 2)
            pts, ws, sides = crescent_pts(C + N * 0, N, D, R, head + math.pi / 2 - math.pi / 2, tail, 0.12 + 0.34 * ease_out(min(1, k * 1.4), 2))
            st_.set(pts, ws, side=sides)
            setv(m, "erode", e)
    # S.poses.append(pose_slash)   # 2026-11-24: 두 획 X는 뿔처럼 보여 뺌 — 칼날 초승달이 대상을 꿰뚫는 것으로 충분

    # 부서진 조각: 칼날 · 베기 바깥 날에서 떨어져 나가 흩날리는 작은 마름모 (보라 · 흰)
    chips = []
    for ai, a in enumerate(assassins):
        for i in range(14):
            cm = flat(f"chip{ai}_{i}", rnd.choice(["FFFFFF", "E4D8FF", "B49BFF", "7652D6"]), blend=True)
            o = cone(f"chip{ai}_{i}", cm, 0.05, 0.0, 0.12, 4); S.link(o, noink=True, grp="slash")
            v0 = a["DIR"] * rnd.uniform(0.6, 2.2) + Vector((rnd.uniform(-1.4, 1.4), rnd.uniform(-1.4, 1.4), rnd.uniform(0.4, 1.8)))
            chips.append((o, cm, a, v0, A_DASH + 0.12 + rnd.uniform(0, 0.12), rnd.uniform(0.25, 0.45), rnd.uniform(0, TAU)))

    def pose_chips(t):
        for o, cm, a, v0, r0, life, sp in chips:
            el = t - a["t0"] - r0; k = el / life
            o.hide_render = not (0 < k < 1)
            if 0 < k < 1:
                o.location = a["HC"] + v0 * el + Vector((0, 0, -2.5 * el * el))
                o.rotation_euler = (sp + 9 * el, 0.7, sp)
                o.scale = Vector((1, 1, 1)) * (1 - k) ** 0.6
                setv(cm, "alpha", 1 - k ** 2)
    S.poses.append(pose_chips)
    twinkles = []
    for ai, a in enumerate(assassins):
        for i in range(6):
            tm_ = flat(f"twk{ai}_{i}", "F4EEFF", blend=True)
            o = spike(f"twk{ai}_{i}", tm_, 0.03, 0.16); S.link(o, noink=True, grp="slash")
            off = Vector((rnd.uniform(-0.9, 0.9), rnd.uniform(-0.9, 0.9), rnd.uniform(0.2, 1.0)))
            twinkles.append((o, tm_, a, off, rnd.uniform(0.12, 0.4)))

    def pose_twinkles(t):                                         # 타격 지점 둘레에 잠깐씩 반짝 (별 = 가시 두 개 교차 대신 하나를 빙글)
        for o, tm_, a, off, d0 in twinkles:
            r = t - a["t0"] - (HS_T + d0)
            k = r / 0.14
            o.hide_render = not (0 < k < 1)
            if 0 < k < 1:
                o.location = a["HC"] + off
                o.rotation_euler = (math.pi / 2, 0, 6 * k)
                sc_ = math.sin(math.pi * k)
                o.scale = (sc_, sc_, sc_ * 1.4)
                setv(tm_, "alpha", sc_)
    S.poses.append(pose_twinkles)

    # 타격 섬광 가시 8 + 꼬리 달린 불똥 12 (휘어 날아가며 떨어짐) — 자객마다
    rays, sparks = [], []
    for ai, a in enumerate(assassins):
        for i in range(10):
            rays.append((GlowStreak(S, f"ray{ai}_{i}", "E8DEFF", "slash", core_w=1.0, halo_w=2.6, halo_a=0.7, tail=1.0, whiten=1.0), a, TAU * i / 10 + rnd.uniform(-0.2, 0.2), rnd.uniform(0.9, 1.6) * (1.0 if i % 2 == 0 else 0.55)))
        for i in range(12):
            v0 = a["DIR"] * rnd.uniform(1.5, 4.0) + Vector((rnd.uniform(-2.5, 2.5), rnd.uniform(-2.5, 2.5), rnd.uniform(1.0, 3.5)))
            sparks.append((GlowStreak(S, f"spk{ai}_{i}", "C9B6FF", "slash", core_w=1.0, halo_w=2.8, halo_a=0.55, tail=0.8, whiten=0.9), a, v0, HS_T + rnd.uniform(0, 0.09), rnd.uniform(0.25, 0.4)))

    def pose_hitfx(t):
        for gs, a, ang, L in rays:
            k = seg(t - a["t0"], HS_T, A_DASH + 0.175)
            if not (0 < k < 1):
                gs.set([], [0], 0); continue
            d = RT * math.cos(ang) + UP * math.sin(ang)
            r0 = 0.15 + 0.3 * k; r1 = r0 + L * ease_out(k) * (1 - 0.6 * k)
            gs.set([a["HC"] + d * (r1 - (r1 - r0) * j / 5) for j in range(6)], [0.2 * (1 - k)] * 6, 1 - k)   # 머리 = 바깥
        for gs, a, v0, r0, life in sparks:
            el = t - a["t0"] - r0
            k = el / life
            if not (0 < k < 1):
                gs.set([], [0], 0); continue
            pts = []
            for j in range(7):                                   # 꼬리 = 지난 자리
                tt = max(0.0, el - 0.018 * j)
                pts.append(a["HC"] + v0 * tt + Vector((0, 0, -6.0 * tt * tt)))
            gs.set(pts, [0.06 * (1 - k) + 0.01] * 7, 1 - k ** 2)
    S.poses.append(pose_hitfx)
    flashes = []
    for ai, a in enumerate(assassins):
        fm = glow(f"hitflash{ai}", "F2ECFF", soft=1.2, tail=0, head=0, whiten=1.0)
        flashes.append((Strip(S, f"hitflash{ai}", fm, noink=True, grp="slash"), fm, a))

    def pose_flash(t):                                           # 카메라를 향한 납작한 빛 덩어리: 타격 순간 확 커졌다 줄어듦
        for st_, fm, a in flashes:
            k = seg(t - a["t0"], HS_T, HS_T + 0.08)
            if not (0 < k < 1):
                st_.set([], [0]); continue
            sz = 0.55 * (1 - k) ** 0.5
            st_.set([a["HC"] + UP * sz * (1 - 2 * j / 6) for j in range(7)], [sz * 2.2 * math.sin(math.pi * (0.05 + 0.9 * j / 6)) for j in range(7)], side=RT)
            setv(fm, "alpha", 0.85 * (1 - k))
    S.poses.append(pose_flash)

    def pose_targets(t):
        for a in assassins:
            r = t - a["t0"]
            h1 = HS_T; h2 = h1 + 0.08
            hit = (h1 <= r < h1 + 0.05) or (h2 <= r < h2 + 0.05)
            setv(a["em"], "heat", 0.0 if hit else 1.0)
            kb = seg(r, h1, h2 + 0.25)
            on = 1.0 if r >= h1 else 0.0
            shake = 0.06 * math.sin(t * 110) * (1 - seg(r, h1, h2 + 0.2)) * on
            a["eo"].location = a["ep"] + a["DIR"] * 0.12 * ease_out(kb) + Vector((shake, 0, 0))
            a["eo"].scale = (1 + 0.15 * (1 - kb) * on, 1, 1 - 0.15 * (1 - kb) * on)
    S.poses.append(pose_targets)

    # 흩어짐: 보라 · 검정 안개 (자객마다)
    mists = []
    for ai, a in enumerate(assassins):
        for i in range(6):
            m = toon(f"smist{ai}_{i}", SH); o = lumpy(f"smist{ai}_{i}", m, 70 + i + 10 * ai); S.link(o, grp="shatter"); o.hide_render = True   # 안 씀 (디졸브로 바뀜)
            d = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(0.3, 1))).normalized()
            mists.append((o, m, d, rnd.uniform(0.3, 0.45), rnd.uniform(0, 0.05), a))

    def pose_mist(t):
        for o, m, d, s, lag, a in mists:
            k = seg(t - a["t0"], A_END - 0.1 + lag, A_END + 0.35)
            o.hide_render = not (0 < k < 1)
            o.location = a["PB"] + Vector((0, 0, 0.6)) + d * 0.7 * ease_out(k, 2) + Vector((0, 0, 0.4 * k))
            o.scale = Vector((1, 1, 1.4)) * s * ease_out(min(1, k / 0.25)) * (1 - 0.4 * k)
            setv(m, "heat", 1 - 0.7 * k); setv(m, "dissolve", max(0, (k - 0.3) / 0.7) * 0.6)
    # S.poses.append(pose_mist)   # 사라짐도 디졸브
    # 굽기: 첫 자객 기준 (나머지는 같은 모양) — 베기 · 흩어짐만 따로 숨기기 어려우므로 그 자객의 것만 남기고 다른 자객 것은 잠시 숨김
    a0 = assassins[0]
    for st_, m, a, r0, tilt in slashes:
        if a is not a0:
            S.groups["slash"].remove(st_.ob)
    for o, cm, a, *_ in chips:
        if a is not a0:
            S.groups["slash"].remove(o)
    for o, tm_, a, *_ in twinkles:
        if a is not a0:
            S.groups["slash"].remove(o)
    for gs, a, *_ in rays + sparks:
        if a is not a0:
            S.groups["slash"].remove(gs.core.ob); S.groups["slash"].remove(gs.halo.ob)
    for o, m, d, s, lag, a in mists:
        if a is not a0:
            S.groups["shatter"].remove(o)
    t0 = a0["t0"]
    if "--closeup" in sys.argv:                                  # 크게 보기: 첫 자객 · 대상 쪽으로 당겨서 960x720
        S.sc.render.resolution_x, S.sc.render.resolution_y = 960, 720
        S.cam.data.lens = 95
        S.cam.location = a0["HC"] + (CAM_LOC - CAM_AIM) - Vector((0, 0, 0.3))
    return S, 1.6, [("mirror_slash", ["slash"], a0["HC"], 3.8, t0 + HS_T, t0 + HS_T + 0.32, 14, False)]   # 흩어짐 안개(mirror_shatter)는 디졸브로 대체


# ================================================================ 3. 모래 순례자 로브
def fx_sand():
    S = Scene(ink=(0.22, 0.13, 0.07), aim=(1.1, 0.8, 0.4)); ground(S)
    C = Vector((0, 0, 0)); V = Vector((2.0, 1.5, 0))
    cat, _ = make_cat(S, "cat", CAT, C, actor=True, facing=-0.7); shadow(S, C)
    en, enm = make_enemy(S, "en", V, 4); esh = shadow(S, V, 0.5)
    T_HIT, T_FULL, T_FADE, T_GONE = 0.16, 0.48, 2.35, 2.8
    W = 2.1                                    # 회전 속도 (rad/s). 팔 5개 → 한 주기 = TAU/5/W
    R = 1.35
    SAND = [(0.0, "FFF3D2"), (0.2, "F2D59A"), (0.45, "D9A35B"), (0.7, "A8703A"), (0.9, "5E3A1E")]
    # 탄: 0 이하 판정 탄 = 짙은 남색 심지에 모래빛 테 (총구 → 적)
    bm_ = toon("bullet", [(0.0, "FFF3D2"), (0.35, "E8B860"), (0.7, "3A2A5A")]); bl = sphere("bullet", bm_, 0.09, 2); S.link(bl)
    bt = Strip(S, "btrail", strip("btrail", [(0.0, "FFF3D2"), (0.4, "E8B860"), (0.8, "8A5A2C")], along=0.9, across=0.5))
    a0 = C + Vector((0.25, 0.2, 0.7)); a1 = V + Vector((0, 0, 0.7))
    def pose_bullet(t):
        k = seg(t, 0.0, T_HIT)
        bl.hide_render = k >= 1
        p = a0.lerp(a1, k); bl.location = p
        if k < 1:
            bt.set([p, a0.lerp(a1, max(0, k - 0.35))], [0.1, 0.01])
        else:
            bt.set([], [0])
    S.poses.append(pose_bullet)
    def grow(t):
        return ease_out(seg(t, T_HIT, T_FULL), 2) * (1 - ease_io(seg(t, T_FADE, T_GONE)))
    def unwind(t):
        return seg(t, T_FADE, T_GONE)
    # ---- 2026-11-24 다시 깎음 (사용자: 그림 느낌 줄이고 모래바람이 확실한 단색 광): 잉크 없는 호박색 한 가지 + 하얀 심지 광
    #   깔때기를 휘감아 오르는 바람 줄기 16 (심지 + 번짐 두 겹) · 바닥 나선 바람 5 · 감겨 도는 모래 점 · 발밑 옅은 빛 안개
    #   한 바퀴 = P초, 모든 움직임이 P 주기라 sand_loop가 이음매 없이 반복됨
    P = 0.8; OM = TAU / P
    AMB = "E8AE52"
    def env(t):                                  # 자라남(0→1) · 풀림
        return grow(t), unwind(t)
    haze_m = glow("haze", AMB, soft=1.4, tail=0.0, head=0, whiten=0.1)
    haze = Strip(S, "haze", haze_m, noink=True, grp="vortex")
    def pose_haze(t):
        g, u = env(t)
        if g <= 0.01:
            haze.set([], [0]); return
        r = R * 0.55 * g * (1 + 0.4 * u)
        haze.set([V + Vector((math.cos(a) * r, math.sin(a) * r, 0.01)) for a in [TAU * j / 48 for j in range(49)]], [R * 1.1 * g] * 49)
        setv(haze_m, "alpha", 0.4 * g * (1 - u) * (0.9 + 0.1 * math.sin(OM * t * 2)))
    S.poses.append(pose_haze)
    rnd = random.Random(29)
    gust = []
    for i in range(24):
        gs = GlowStreak(S, f"gust{i}", AMB, "vortex", core_w=1.0, halo_w=4.0, halo_a=0.45)
        gust.append((gs, TAU * i / 24 + rnd.uniform(-0.2, 0.2), rnd.uniform(0.0, 1.3), rnd.uniform(1.6, 2.8), rnd.random(), rnd.uniform(0.09, 0.15)))
    def pose_gust(t):
        g, u = env(t)
        for gs, a0, h0, L, o, w in gust:
            ph = (t / P + o) % 1.0
            al = math.sin(math.pi * ph) ** 1.3 * g * (1 - u)
            if al <= 0.01:
                gs.set([], [0], 0); continue
            head = a0 + OM * t + 1.2 * ph
            pts, ws = [], []
            for j in range(18):
                s = j / 17
                h = (h0 + 0.35 * ph - 0.22 * s) * g ** 1.5 + 0.9 * u * (h0 + 0.3)        # 바닥에서 솟아오름 → 풀릴 때 위로 흩날림
                rr = (0.42 + 0.55 * max(0.0, h) / 1.4) * (1.6 - 0.6 * g) * (1 + 0.7 * u)
                a = head - L * s * (0.6 + 0.4 * g)
                pts.append(V + Vector((math.cos(a) * rr, math.sin(a) * rr, 0.06 + max(0.0, h))))
                ws.append(w * (1 - 0.5 * s))
            gs.set(pts, ws, al)
    S.poses.append(pose_gust)
    swirl = [GlowStreak(S, f"swirl{i}", AMB, "vortex", core_w=1.0, halo_w=3.5, halo_a=0.5, tail=1.6) for i in range(5)]
    def pose_swirl(t):
        g, u = env(t)
        for i, gs in enumerate(swirl):
            if g <= 0.02:
                gs.set([], [0], 0); continue
            base = TAU * i / 5 + OM * t
            pts, ws = [], []
            for j in range(26):
                s = j / 25                              # 0 = 안쪽 머리, 1 = 바깥 꼬리
                rr = (0.15 + (R - 0.15) * s) * g * (1 + 0.6 * u)
                a = base + 2.8 * s * (1 - 0.6 * u)
                pts.append(V + Vector((math.cos(a) * rr, math.sin(a) * rr, 0.02 + 0.004 * i)))
                ws.append(0.17 * (1 - s) ** 0.6 + 0.02)
            gs.set(pts, ws, g * (1 - u))
    S.poses.append(pose_swirl)
    # 모래 점: 작은 광 점이 짧은 꼬리를 끌며 감겨 돎 (정수 바퀴 → 루프)
    motes = []
    for i in range(28):
        gs = GlowStreak(S, f"mote{i}", AMB, "vortex", core_w=1.0, halo_w=3.0, halo_a=0.4, tail=0.8)
        motes.append((gs, rnd.uniform(0, TAU), rnd.uniform(0.3, R * 1.05), rnd.uniform(0.05, 1.3), rnd.choice([1, 1, 2]), rnd.random()))
    def pose_motes(t):
        g, u = env(t)
        for gs, a0, r0, h0, k, o in motes:
            ph = (t / P + o) % 1.0
            al = math.sin(math.pi * ph) * g * (1 - u)
            if al <= 0.01:
                gs.set([], [0], 0); continue
            rr = r0 * (1.5 - 0.5 * g) * (1 + 1.2 * u)
            pts = []
            for j in range(5):
                a = a0 + OM * k * t - 0.09 * j * k
                pts.append(V + Vector((math.cos(a) * rr, math.sin(a) * rr, 0.06 + h0 * (0.7 + 0.3 * ph) * g)))
            gs.set(pts, [0.07] * 5, al)
    S.poses.append(pose_motes)
    # 명중 순간: 바닥으로 퍼지는 광 고리 하나 (시작 굽기에만)
    ring_m = glow("hitring", AMB, soft=1.3, tail=0.0, head=0)
    hring = Strip(S, "hitring", ring_m, noink=True, grp="burst")
    def pose_hring(t):
        k = seg(t, T_HIT, T_HIT + 0.3)
        if not (0 < k < 1):
            hring.set([], [0]); return
        r = 0.3 + R * 1.1 * ease_out(k, 2)
        hring.set([V + Vector((math.cos(a) * r, math.sin(a) * r, 0.03)) for a in [TAU * j / 56 for j in range(57)]], [0.3 * (1 - k) + 0.02] * 57)
        setv(ring_m, "alpha", 1 - k)
    # S.poses.append(pose_hring)    # 2026-11-24 사용자: 원형 고리 뺌
    # 적: 둔화(느린 흔들림) + 지속 피해 번쩍
    ticks = [T_FULL + 0.15 + 0.5 * i for i in range(4)]
    def pose_en(t):
        g = grow(t)
        hit = (T_HIT <= t < T_HIT + 0.05) or any(tk <= t < tk + 0.05 for tk in ticks)
        setv(enm, "heat", 0.0 if hit else 1.0 - 0.25 * g)
        en.location = V + Vector((0.03 * math.sin(t * 7) * g, 0, 0)); en.rotation_euler = (0, 0.06 * math.sin(t * 5) * g, 0)
    S.poses.append(pose_en)
    # 2026-11-24 사용자: 반복 대신 '모래바람이 일어났다가 없어지는' 한 번 → sand_full 한 벌(명중 ~ 다 흩어짐)을 소용돌이 수명에 맞춰 틂
    return S, 3.0, [("sand_full", ["vortex", "burst"], V + Vector((0, 0, 0.7)), 4.4, T_HIT, T_GONE, 40, False)]


# ================================================================ 4. 노인의 잠옷
def fx_pajama():
    S = Scene(ink=(0.06, 0.1, 0.2), aim=(0.2, 0.2, 0.5)); ground(S)
    C = Vector((0, 0, 0))
    cat, catm = make_cat(S, "cat", CAT, C, actor=True, facing=0.3); shadow(S, C)
    make_enemy(S, "en", Vector((2.4, 1.8, 0)), 5); shadow(S, (2.4, 1.8, 0), 0.5)
    T_ON, AURA_P = 0.3, 1.0                    # 오라는 1초 주기로 반복 (굽기 루프)
    BLUE = [(0.0, "F2FBFF"), (0.25, "B8E2FF"), (0.5, "6FB7FF"), (0.75, "2F6FD0"), (0.92, "16346E")]
    # 체력이 깎이는 순간: 고양이 붉게 번쩍 두 번 → 푸른 펄스
    def pose_cat(t):
        hurt = (0.05 <= t < 0.09) or (0.18 <= t < 0.22)
        setv(catm, "heat", 0.0 if hurt else 1.0)
        cat.location = C + Vector((0.05 * math.sin(t * 80) * (1 if t < 0.25 else 0), 0, 0))
    S.poses.append(pose_cat)
    # ---- 펄스 (2026-11-24 사용자: 번개 · 가시 말고 '정신수양처럼 온화하게 기가 퍼지는' 느낌)
    #   들숨: 옅은 빛 점들이 몸으로 천천히 모여듦 → 날숨: 부드럽게 일렁이는 기의 물결 고리 3겹이 느리게 번져 나감 + 몸에서 감겨 오르는 기 줄기
    QI = "5FA8FF"
    rnd = random.Random(17)
    T_IN = T_ON - 0.2                                             # 들숨 시작
    waves = []
    for i in range(3):
        wm = glow(f"qiwave{i}", QI, soft=2.2, tail=0, head=0, whiten=0.35)
        waves.append((Strip(S, f"qiwave{i}", wm, noink=True, grp="pulse"), wm, i))
    def pose_waves(t):
        for st, wm, i in waves:
            k = seg(t, T_ON + 0.14 * i, T_ON + 0.14 * i + 0.75)
            if not (0 < k < 1):
                st.set([], [0]); continue
            r = 0.4 + (2.0 - 0.35 * i) * ease_out(k, 2.2)
            pts = []
            for j in range(73):
                a = TAU * j / 72
                rr = r * (1 + 0.03 * math.sin(3 * a + 3 * k + i * 1.7) + 0.018 * math.sin(5 * a - 2 * k + i))   # 물결처럼 일렁이는 가장자리
                pts.append(C + Vector((math.cos(a) * rr, math.sin(a) * rr, 0.03 + 0.01 * i)))
            st.set(pts, [(0.55 - 0.12 * i) * (1 - 0.5 * k)] * 73)
            setv(wm, "alpha", 0.6 * math.sin(math.pi * min(1, k * 1.4)) ** 0.8 * (1 - k))
    S.poses.append(pose_waves)
    gather = []
    for i in range(14):
        gather.append((GlowStreak(S, f"qig{i}", QI, "pulse", core_w=1.0, halo_w=3.0, halo_a=0.5, tail=1.0, whiten=0.6), TAU * i / 14 + rnd.uniform(-0.2, 0.2), rnd.uniform(1.0, 1.6), rnd.uniform(0.2, 1.3)))
    def pose_gather(t):
        k = seg(t, T_IN, T_ON + 0.05)
        for gs, a0, r0, h0 in gather:
            if not (0 < k < 1):
                gs.set([], [0], 0); continue
            pts = []
            for j in range(6):                                    # 머리 = 몸 쪽, 꼬리 = 바깥
                kk = max(0.0, k - 0.06 * j)
                rr = r0 * (1 - ease_io(kk)) + 0.25
                a = a0 + 0.8 * kk
                pts.append(C + Vector((math.cos(a) * rr, math.sin(a) * rr, h0 * (1 - 0.4 * kk) + 0.1)))
            gs.set(pts, [0.06] * 6, math.sin(math.pi * k))
    S.poses.append(pose_gather)
    rise = []
    for i in range(6):
        rise.append((GlowStreak(S, f"qir{i}", QI, "pulse", core_w=1.0, halo_w=3.4, halo_a=0.45, tail=1.0, whiten=0.5), TAU * i / 6 + rnd.uniform(-0.3, 0.3), i * 0.05))
    def pose_rise(t):
        for gs, a0, lag in rise:
            k = seg(t, T_ON + lag, T_ON + lag + 0.8)
            if not (0 < k < 1):
                gs.set([], [0], 0); continue
            pts = []
            for j in range(12):                                   # 머리 = 위
                s_ = j / 11
                h = (0.2 + 1.6 * ease_out(k, 2)) * (1 - 0.7 * s_)
                a = a0 + 2.2 * (1 - s_) * k
                rr = 0.35 + 0.25 * math.sin(math.pi * (1 - s_)) * (0.5 + k)
                pts.append(C + Vector((math.cos(a) * rr, math.sin(a) * rr, h)))
            gs.set(pts, [0.09 * (1 - 0.5 * k)] * 12, math.sin(math.pi * k) ** 0.8)
    S.poses.append(pose_rise)
    qshell_m = glow("qi_body", "8CC6FF", soft=1.8, tail=1.0, head=2.2, whiten=0.2)
    qshell = Strip(S, "qi_body", qshell_m, noink=True, grp="pulse")
    RT = cam_right()
    BK = Vector(((CAM_AIM - CAM_LOC).x, (CAM_AIM - CAM_LOC).y, 0)).normalized() * 0.35
    def pose_qshell(t):                                           # 몸 뒤 후광이 숨 쉬듯 부풀었다 가라앉음
        k = seg(t, T_IN + 0.1, T_ON + 0.6)
        if not (0 < k < 1):
            qshell.set([], [0]); return
        qshell.set([C + BK + Vector((0, 0, 1.8 - 1.75 * s_ / 9)) for s_ in range(10)], [1.2 + 0.6 * ease_out(k)] * 10, side=RT)
        setv(qshell_m, "alpha", 0.5 * math.sin(math.pi * k))
    S.poses.append(pose_qshell)
    # 상시 오라: 몸 둘레 잔불 혀 7 (잉크 없음, 반투명) + 발밑 옅은 빛 + 떠오르는 불씨   (사용자: 처음 것이 나음 → 되돌림)
    AUR = [(0.0, "F4FBFF"), (0.3, "C8E8FF"), (0.65, "8EC4FF")]
    def aura_on(t):
        return seg(t, T_ON + 0.1, T_ON + 0.45)
    gm = strip("aura_glow", [(0.0, "CFEBFF"), (0.45, "8CC6FF"), (0.8, "4A86E0")], along=0, across=1.0, blend=True)
    aglow = Strip(S, "aura_glow", gm, noink=True, grp="aura")
    aglow.set([C + Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, 0.01)) for a in [TAU * j / 48 for j in range(49)]], [0.09] * 49)   # 발밑 가는 빛 고리 (면을 깔면 바닥과 섞여 잿빛이 됨)
    tongues = []
    for i in range(7):
        m = toon(f"tg{i}", AUR, blend=True); o = flame(f"tg{i}", m, 150 + i); S.link(o, noink=True, grp="aura")
        tongues.append((o, m, TAU * i / 7, i))
    embers = []
    for i in range(10):
        m = flat(f"em{i}", "D8F0FF", blend=True); o = sphere(f"em{i}", m, 0.03, 1); S.link(o, noink=True, grp="aura")
        embers.append((o, m, rnd.uniform(0, TAU), rnd.uniform(0.25, 0.5), i / 10))
    def pose_aura(t):
        on = aura_on(t)
        aglow.ob.hide_render = on <= 0
        setv(gm, "alpha", 0.55 * on * (0.85 + 0.15 * math.sin(TAU * t / AURA_P * 2)))
        for o, m, a, i in tongues:
            ph = (t / AURA_P + i / 7) % 1.0                         # 혀 하나가 솟았다 사그라드는 위상
            o.hide_render = on <= 0
            r = 0.4
            o.location = C + Vector((math.cos(a) * r, math.sin(a) * r, 0.05 + 0.5 * ph))
            h = math.sin(ph * math.pi)
            o.scale = Vector((0.32, 0.32, 0.75)) * (0.55 + 0.6 * h) * on
            o.rotation_euler = (0.25 * math.sin(a), -0.25 * math.cos(a), 0)
            setv(m, "alpha", 0.62 * h * on); setv(m, "heat", 1 - 0.6 * ph); setv(m, "dissolve", 0.35 * ph)
        for o, m, a, r, ph0 in embers:
            ph = (t / AURA_P + ph0) % 1.0
            o.hide_render = on <= 0
            o.location = C + Vector((math.cos(a + ph * 1.5) * r, math.sin(a + ph * 1.5) * r, 0.1 + 1.5 * ph))
            setv(m, "alpha", on * math.sin(ph * math.pi))
    S.poses.append(pose_aura)
    return S, 2.4, [("pajama_pulse", ["pulse"], C + Vector((0, 0, 0.6)), 5.0, T_IN, T_ON + 0.95, 24, False),
                    ("pajama_aura", ["aura"], C + Vector((0, 0, 0.9)), 2.6, 1.0, 1.0 + AURA_P, 16, True)]


FX = {"undertaker": fx_undertaker, "mirror": fx_mirror, "sand": fx_sand, "pajama": fx_pajama}
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
names = [a for a in args if not a.startswith("--")] or list(FX)
for n in names:
    S, dur, bakes = FX[n]()
    if "--bake-only" not in args:
        S.run(n + ("_closeup" if "--closeup" in args else ""), dur)
    if "--preview-only" not in args:
        for b in bakes:
            S.bake(*b)
print("DONE")
