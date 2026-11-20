# 칼빛 리본 결 텍스처 굽기 (2026-11-20 사용자: "레이저 궤적 블렌더로 깔끔하게 다듬어 — 레퍼런스(스타일라이즈 에너지 이펙트)의 색감 · 텍스처 참고")
# 레퍼런스 결: 노이즈 · 보로노이로 찢긴 가장자리가 길이 방향으로 흐르고, 그 결대로 갈라지며 사라짐.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_slash_tex.py
# 결과: Game/Assets/Resources/FX/Tex/slash_flow.png (1024 × 256, u = 길이 방향 이음매 없음 / v = 폭)
#   R = 길게 늘인 보로노이 결 (칼날이 쓸고 간 줄무늬), G = fbm 노이즈 (갈라져 사라지는 문턱), B = 굵은 덩어리 보로노이 (가장자리 찢김)
import bpy, math, os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Game", "Assets", "Resources", "FX", "Tex")
W, H = 1024, 256

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try: sc.render.engine = eng; break
        except TypeError: pass
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "BW"
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("w"); world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0, 0, 0, 1)
    sc.world = world
    cam = bpy.data.cameras.new("cam"); cam.type = "ORTHO"; cam.ortho_scale = 4.0
    co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = (0, 0, 5)
    bpy.ops.mesh.primitive_plane_add(size=1)
    pl = bpy.context.active_object; pl.scale = (4.0, 1.0, 1)
    return sc, pl

def flow_material(kind):
    m = bpy.data.materials.new(kind); m.use_nodes = True
    nt = m.node_tree; n = nt.nodes; l = nt.links
    for x in list(n): n.remove(x)
    out = n.new("ShaderNodeOutputMaterial"); em = n.new("ShaderNodeEmission")
    l.new(em.outputs[0], out.inputs[0])
    tc = n.new("ShaderNodeTexCoord"); sep = n.new("ShaderNodeSeparateXYZ"); l.new(tc.outputs["UV"], sep.inputs[0])
    # u를 원 둘레로 감아 이음매 없이: (cos 2πu, sin 2πu) · R, v는 z
    def math(op, a, b=None):
        mn = n.new("ShaderNodeMath"); mn.operation = op
        if isinstance(a, float): mn.inputs[0].default_value = a
        else: l.new(a, mn.inputs[0])
        if b is not None:
            if isinstance(b, float): mn.inputs[1].default_value = b
            else: l.new(b, mn.inputs[1])
        return mn.outputs[0]
    ang = math("MULTIPLY", sep.outputs["X"], 2 * 3.14159265)
    R = {"streak": 0.45, "erode": 1.1, "chunk": 0.7}[kind]
    cx = math("MULTIPLY", math("COSINE", ang), R)
    cy = math("MULTIPLY", math("SINE", ang), R)
    vz = math("MULTIPLY", sep.outputs["Y"], {"streak": 13.0, "erode": 2.4, "chunk": 1.6}[kind])
    comb = n.new("ShaderNodeCombineXYZ"); l.new(cx, comb.inputs[0]); l.new(cy, comb.inputs[1]); l.new(vz, comb.inputs[2])
    val = None
    if kind == "streak":
        # 폭 방향(z)으로 촘촘한 보로노이 경계 = 길이 방향으로 길게 늘어진 결
        vo = n.new("ShaderNodeTexVoronoi"); vo.voronoi_dimensions = "3D"; vo.feature = "DISTANCE_TO_EDGE"
        vo.inputs["Scale"].default_value = 2.2; vo.inputs["Randomness"].default_value = 1.0
        l.new(comb.outputs[0], vo.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.0; cr.inputs[2].default_value = 0.22
        l.new(vo.outputs["Distance"], cr.inputs[0]); val = cr.outputs[0]
    elif kind == "erode":
        no = n.new("ShaderNodeTexNoise"); no.noise_dimensions = "3D"
        no.inputs["Scale"].default_value = 2.6; no.inputs["Detail"].default_value = 6.0; no.inputs["Roughness"].default_value = 0.62
        l.new(comb.outputs[0], no.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.28; cr.inputs[2].default_value = 0.72
        l.new(no.outputs["Fac"], cr.inputs[0]); val = cr.outputs[0]
    else:
        vo = n.new("ShaderNodeTexVoronoi"); vo.voronoi_dimensions = "3D"; vo.feature = "F1"
        vo.inputs["Scale"].default_value = 1.6
        l.new(comb.outputs[0], vo.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.05; cr.inputs[2].default_value = 0.85
        l.new(vo.outputs["Distance"], cr.inputs[0]); val = cr.outputs[0]
    l.new(val, em.inputs["Color"]); em.inputs["Strength"].default_value = 1.0
    return m

def main():
    os.makedirs(OUT, exist_ok=True)
    chans = {}
    for kind in ("streak", "erode", "chunk"):
        sc, pl = reset()
        pl.data.materials.append(flow_material(kind))
        path = os.path.join(OUT, "_tmp_" + kind + ".png")
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True)
        img = bpy.data.images.load(path)
        px = [0.0] * (W * H * 4); img.pixels.foreach_get(px)
        chans[kind] = px[0::4]
    out = bpy.data.images.new("slash_flow", W, H, alpha=True)
    px = [0.0] * (W * H * 4)
    for i in range(W * H):
        px[i * 4] = chans["streak"][i]; px[i * 4 + 1] = chans["erode"][i]; px[i * 4 + 2] = chans["chunk"][i]; px[i * 4 + 3] = 1.0
    out.pixels.foreach_set(px)
    out.filepath_raw = os.path.join(OUT, "slash_flow.png"); out.file_format = "PNG"; out.save()
    for kind in chans: os.remove(os.path.join(OUT, "_tmp_" + kind + ".png"))
    print("slash_flow.png saved")

main()
