# 11:59 2장 해금 이펙트 플립북 (2026-11-25, Blender 5.x 백그라운드) — blender_bake_fx.py 의 화풍 · 도구 그대로
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_bake_ch2unlock.py
# 결과: Game/Assets/Resources/FX/anim/bk_c2steam_0~15.png — 하얀 증기 뭉게 (기관차 증기 돌진 · 마지막 기차 끝 폭발)
import os, sys, math, random
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
_argv = sys.argv
sys.argv = [_argv[0], "--"]                       # 도구만 빌려 옴 (기존 세트는 굽지 않음)
NS = {"__file__": os.path.join(HERE, "blender_bake_fx.py")}
exec(open(NS["__file__"], encoding="utf-8").read(), NS)
sys.argv = _argv
reset, set_line_color, toon_mat, lumpy, setv, ease_out, render, FRAMES, TAU = (NS[k] for k in ("reset", "set_line_color", "toon_mat", "lumpy", "setv", "ease_out", "render", "FRAMES", "TAU"))


def bake_c2steam():
    """증기 뭉게: 하얀 덩어리가 '퍽' 하고 한 번에 부풀어 바깥으로 밀려 나간 뒤, 위로 말려 올라가며 푸른 회색으로 식고 녹아 흩어짐"""
    sc = reset(); set_line_color(sc, (0.22, 0.27, 0.33))
    rnd = random.Random(2010)
    mats, obs = [], []
    for i in range(8):
        m = toon_mat(f"steam{i}", [(0.0, (1, 1, 1)), (0.24, (0.9, 0.93, 0.96)), (0.5, (0.7, 0.76, 0.82)), (0.78, (0.48, 0.55, 0.62))])
        o = lumpy(f"st{i}", 70 + i, m)
        d = Vector((rnd.uniform(-1, 1), rnd.uniform(-0.5, 0.5), rnd.uniform(-0.4, 0.8))).normalized()
        o["dir"] = d; o["s"] = rnd.uniform(0.45, 0.72); o["lag"] = rnd.uniform(0, 0.12); o["curl"] = rnd.uniform(-1, 1)
        mats.append(m); obs.append(o)
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        for o, m in zip(obs, mats):
            k = max(0.0, (t - o["lag"]) / (1 - o["lag"]))
            push = ease_out(min(1, k / 0.4), 3)
            o.location = Vector(o["dir"]) * 0.75 * push + Vector((o["curl"] * 0.25 * k * k, 0, 0.55 * k * k - 0.25))
            fade = min(1.0, max(0.0, (k - 0.62) / 0.38))
            grow = ease_out(min(1, k / 0.18)) * (1 + 0.5 * k)
            o.scale = Vector((1, 1, 1)) * o["s"] * max(0.001, grow * (1 - fade) ** 1.4)
            o.hide_render = fade >= 0.999 or grow <= 0.001
            o.rotation_euler = (o["curl"] * k * 1.5, o["curl"] * k, 0)
            setv(m, "heat", 1 - 0.75 * k)
            setv(m, "dissolve", min(0.55, max(0, (k - 0.4) / 0.6)))
        render(sc, "c2steam", f)


bake_c2steam()
print("BAKED c2steam")
