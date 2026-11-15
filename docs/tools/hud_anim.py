# 하단 HUD 사격 연출 (레이어 합성)
# 레이어: 바탕(실린더 뺀 HUD, 게이지 빈/가득 2벌) · 실린더 2개 · 탄피 · 시계 바늘 · 폭발 4프레임
# - 오른 실린더 = 홀수 칸(1 · 3 · ... · 11), 왼 실린더 = 짝수 칸(2 · ... · 12), 각 실린더 안에서는 맨 위부터 시계 방향
# - 한 발: 그 칸 탄피가 빠지며 폭발, 바늘이 한 칸(30도) 감, 열기 게이지가 1/4칸 참 (12발 = 3칸)
# - 12발 뒤 장전: 바늘과 두 실린더가 함께 한 바퀴 돌며 탄피가 다시 채워지고 바늘은 1시로
# 결과: docs/images/ui/hud_parts/ (레이어 · hud_layout.json), hud_fire_anim_*.gif, hud_fire_frames_*.png
import sys, os, json, math
sys.path.insert(0, r"D:\pro\Cat_Tang\docs\tools")
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as nd
from icon_toon import mask_of, toon, PRESETS, BG
import fx_shader as fxv                     # 벡터 거리장 + 셰이더식 색칠 이펙트

U = r"D:\pro\Cat_Tang\docs\images\ui"
OUT = os.path.join(U, "hud_parts")
WORK_W = 900
BGC = tuple(int(v) for v in BG)
HEAT_PER_SHOT = 0.25          # 칸 단위


# ---------- 불러오기 ----------
def fix_seams(a):
    for y0, y1 in ((446, 454),):
        for y in range(y0 + 1, y1):
            t = (y - y0) / (y1 - y0)
            a[y] = a[y0] * (1 - t) + a[y1] * t
    for x0, x1 in ((797, 803),):
        for x in range(x0 + 1, x1):
            t = (x - x0) / (x1 - x0)
            a[:, x] = a[:, x0] * (1 - t) + a[:, x1] * t
    return a


def crop_to_mask(a, pad=20):
    m = mask_of(a)
    lab, n = nd.label(nd.binary_dilation(m, iterations=4))
    big = np.argmax(nd.sum(m, lab, range(1, n + 1))) + 1
    ys, xs = np.nonzero(lab == big)
    y0, y1 = max(ys.min() - pad, 0), min(ys.max() + pad, a.shape[0])
    x0, x1 = max(xs.min() - pad, 0), min(xs.max() + pad, a.shape[1])
    return a[y0:y1, x0:x1]


def load_base():
    a = fix_seams(np.asarray(Image.open(os.path.join(U, "sheet_hud_base_flat.png")).convert("RGB"), np.float32))
    im = Image.fromarray(crop_to_mask(a).astype(np.uint8))
    return im.resize((WORK_W, round(im.height * WORK_W / im.width)), Image.LANCZOS)


def load_sprites():
    a = np.asarray(Image.open(os.path.join(U, "sheet_hud_sprites_flat.png")).convert("RGB"), np.float32)
    half = a.shape[1] // 2
    return crop_to_mask(a[:, :half].copy(), 12), crop_to_mask(a[:, half:].copy(), 12)


def load_explosions():
    a = np.asarray(Image.open(os.path.join(U, "sheet_fx_explosion_flat.png")).convert("RGB"), np.float32)
    H, W = a.shape[:2]
    cells = []
    for r in range(2):
        for c in range(2):
            cells.append(crop_to_mask(a[r * H // 2:(r + 1) * H // 2, c * W // 2:(c + 1) * W // 2].copy(), 10))
    return cells


# ---------- 위치 찾기 ----------
def find_layout(base):
    a = np.asarray(base, np.float32)
    m = mask_of(a)
    alb = a / 255
    lum = alb @ np.array([0.3, 0.59, 0.11])
    dark = nd.binary_opening(m & (lum < 0.13), iterations=2)
    lab, n = nd.label(dark)
    holes = []
    for k in range(1, n + 1):
        reg = lab == k
        area = reg.sum()
        ys, xs = np.nonzero(reg)
        h, w = np.ptp(ys) + 1, np.ptp(xs) + 1
        fill = area / (math.pi * (max(h, w) / 2) ** 2)
        if area > 150 and 0.75 < h / w < 1.33 and fill > 0.7:
            holes.append((area, xs.mean(), ys.mean(), (h + w) / 4))
    holes.sort(reverse=True)
    holes = holes[:12]
    if len(holes) != 12:
        raise SystemExit(f"약실 {len(holes)}개만 찾음")
    W = base.width

    def order(group):
        cx = np.mean([g[1] for g in group]); cy = np.mean([g[2] for g in group])
        ang = lambda g: (math.degrees(math.atan2(g[1] - cx, -(g[2] - cy))) + 360) % 360
        return [dict(x=float(g[1]), y=float(g[2]), r=float(g[3])) for g in sorted(group, key=ang)], (float(cx), float(cy))
    lch, lc = order([h for h in holes if h[1] < W / 2])
    rch, rc = order([h for h in holes if h[1] >= W / 2])
    ivory = m & (lum > 0.8) & (alb.max(2) - alb.min(2) < 0.36) & (alb[..., 2] > 0.6)
    lab, n = nd.label(nd.binary_closing(ivory, iterations=3))
    big = np.argmax(nd.sum(ivory, lab, range(1, n + 1))) + 1
    dial = nd.binary_fill_holes(lab == big)
    cy, cx = nd.center_of_mass(dial)
    slots = {}
    for s in range(1, 13):
        side, ch = ("right", rch[(s - 1) // 2]) if s % 2 else ("left", lch[s // 2 - 1])
        slots[s] = dict(side=side, index=(s - 1) // 2 if s % 2 else s // 2 - 1, **ch)
    gauge = find_gauge(a, m, lum)
    return dict(size=[base.width, base.height], clock=dict(x=float(cx), y=float(cy), r=float(math.sqrt(dial.sum() / math.pi))),
                cylinder_left=dict(x=lc[0], y=lc[1]), cylinder_right=dict(x=rc[0], y=rc[1]), slots=slots, gauge_cells=gauge)


def find_gauge(a, m, lum):
    # 게이지 칸: 아래쪽 가운데의 검은 선으로 둘러싸인 3칸 (불길 · 빈 칸 모두 포함)
    H, W = lum.shape
    box = np.zeros_like(m)
    box[int(H * 0.68):, int(W * 0.25):int(W * 0.75)] = True
    inner = box & m & (lum > 0.12)
    lab, n = nd.label(inner)
    cells = []
    for k in range(1, n + 1):
        reg = lab == k
        ys, xs = np.nonzero(reg)
        w, h = np.ptp(xs) + 1, np.ptp(ys) + 1
        if reg.sum() > 2500 and 1.2 < w / h < 3.5:
            cells.append((xs.min(), xs.max(), ys.min(), ys.max(), reg))
    cells.sort(key=lambda c: c[0])
    if len(cells) != 3:
        raise SystemExit(f"게이지 칸 {len(cells)}개")
    return cells


# ---------- 레이어 만들기 ----------
def cylinder_mask(a, center, ring_r):
    """실린더 몸통(회색) + 약실 구멍 + 자기 외곽선만. 레일의 검은 선은 넣지 않는다. 회전 중심도 몸통에서 다시 구함."""
    lum = (a / 255) @ np.array([0.3, 0.59, 0.11])
    sat = (a.max(2) - a.min(2)) / 255
    m = mask_of(a)
    H, W = lum.shape
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx - center[0], yy - center[1])
    gray = m & (sat < 0.16) & (lum > 0.22) & (d < ring_r * 2.2)
    lab, n = nd.label(gray)
    big = np.argmax(nd.sum(gray, lab, range(1, n + 1))) + 1
    disk = nd.binary_fill_holes(nd.binary_closing(lab == big, iterations=3))
    cy, cx = nd.center_of_mass(disk)
    body = nd.binary_dilation(disk, iterations=6) & m          # 자기 외곽선 두께만큼만 넓힘
    R = float(np.hypot(xx[disk] - cx, yy[disk] - cy).max())
    return body, R, (float(cx), float(cy))


def fill_cells(a, cells, mode, tex=None):
    out = a.copy()
    empty_col = np.array([45, 38, 55], np.float32)
    for x0, x1, y0, y1, reg in cells:
        if mode == "empty":
            out[reg] = empty_col
        else:
            # 첫 칸의 불길을 각 칸 크기로 늘여 채움
            t = cv2.resize(tex, (x1 - x0 + 1, y1 - y0 + 1), interpolation=cv2.INTER_LINEAR)
            sub = out[y0:y1 + 1, x0:x1 + 1]
            r = reg[y0:y1 + 1, x0:x1 + 1]
            sub[r] = t[r]
    return out


def make_layers(base, layout):
    a = np.asarray(base, np.float32)
    ring = np.mean([math.hypot(s["x"] - layout["cylinder_" + s["side"]]["x"], s["y"] - layout["cylinder_" + s["side"]]["y"])
                    for s in layout["slots"].values()])
    cyl = {}
    hole_mask = np.zeros(a.shape[:2], bool)
    for side in ("left", "right"):
        c = layout["cylinder_" + side]
        mk, R, ctr = cylinder_mask(a, (c["x"], c["y"]), ring)
        c["x"], c["y"] = ctr                                     # 회전 중심 = 몸통 중심
        cyl[side] = (mk, R)
        hole_mask |= nd.binary_dilation(mk, iterations=3)
    # 실린더 자리를 주변으로 메움 (회전할 때 홈 사이로 보이는 뒤쪽)
    filled = cv2.inpaint(a.astype(np.uint8), hole_mask.astype(np.uint8) * 255, 7, cv2.INPAINT_TELEA).astype(np.float32)
    cells = layout["gauge_cells"]
    x0, x1, y0, y1, reg = cells[0]
    tex = a[y0:y1 + 1, x0:x1 + 1].copy()
    full = fill_cells(filled, cells, "full", tex)
    empty = fill_cells(filled, cells, "empty")
    return full, empty, cyl, a


def rgba_from(arr_rgb, mask, feather=1.0):
    al = (nd.gaussian_filter(mask.astype(np.float32), feather) * 255).astype(np.uint8)
    return Image.fromarray(np.dstack([np.clip(arr_rgb, 0, 255).astype(np.uint8), al]))


def shade(arr, radius):
    return np.asarray(toon(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), ui=True, radius=radius, **PRESETS["hades"]).convert("RGB"), np.float32)


def sprite(arr, do_shade, radius=0.05, dil=3):
    m = nd.binary_dilation(mask_of(arr), iterations=dil)
    src = shade(arr, radius) if do_shade else arr
    return rgba_from(src, m, 0.8)


def hand_pivot(arr):
    m = mask_of(arr)
    ys, xs = np.nonzero(m)
    bottom = ys.max()
    widths = m.sum(1)
    zone = range(int(bottom - (bottom - ys.min()) * 0.3), bottom + 1)
    hub_w = max(widths[y] for y in zone)
    return float(xs[ys > bottom - hub_w * 0.5].mean()), float(bottom - hub_w / 2 - 3), float(bottom - ys.min())


def paste_rot(dst, im, pivot, at, angle):
    pad = int(max(im.size) * 1.5)
    cv = Image.new("RGBA", (pad * 2, pad * 2), (0, 0, 0, 0))
    cv.alpha_composite(im, (round(pad - pivot[0]), round(pad - pivot[1])))
    rot = cv.rotate(-angle, resample=Image.BICUBIC, center=(pad, pad))
    dst.alpha_composite(rot, (round(at[0] - pad), round(at[1] - pad)))


# ---------- 조립 ----------
def build(do_shade):
    tag = "toon" if do_shade else "flat"
    base = load_base()
    layout = find_layout(base)
    full, empty, cyl, orig = make_layers(base, layout)
    hand_arr, cap_arr = load_sprites()

    hole_d = 2 * np.mean([v["r"] for v in layout["slots"].values()]) * 1.08
    # 탄피
    cap_im = Image.fromarray(cap_arr.astype(np.uint8)).resize((round(hole_d * 2),) * 2, Image.LANCZOS)
    cap = sprite(np.asarray(cap_im, np.float32), do_shade).resize((round(hole_d),) * 2, Image.LANCZOS)
    # 바늘 (2배로 만들고 절반으로)
    px, py, length = hand_pivot(hand_arr)
    sc = layout["clock"]["r"] * 0.80 / length * 2
    hand_im = Image.fromarray(hand_arr.astype(np.uint8))
    hand_arr = np.asarray(hand_im.resize((round(hand_im.width * sc), round(hand_im.height * sc)), Image.LANCZOS), np.float32)
    hand = sprite(hand_arr, do_shade)
    hand = hand.resize((hand.width // 2, hand.height // 2), Image.LANCZOS)
    piv = (px * sc / 2, py * sc / 2)
    fx_size = int(hole_d * 3.6)
    # 바탕 2벌
    bm = nd.binary_dilation(mask_of(orig), iterations=5)
    full_s = shade(full, 0.018) if do_shade else full
    empty_s = shade(empty, 0.018) if do_shade else empty
    base_full = rgba_from(full_s, bm)
    base_empty = rgba_from(empty_s, bm)
    # 실린더 (원래 위치에서 잘라 회전용 스프라이트로)
    cyl_sp = {}
    src_for_cyl = shade(orig, 0.018) if do_shade else orig
    for side, (mk, R) in cyl.items():
        c = layout["cylinder_" + side]
        r = int(R + 12)
        x0, y0 = int(c["x"]) - r, int(c["y"]) - r
        sub = src_for_cyl[y0:y0 + 2 * r, x0:x0 + 2 * r]
        msub = mk[y0:y0 + 2 * r, x0:x0 + 2 * r]
        cyl_sp[side] = (rgba_from(sub, msub, 0.8), (c["x"] - x0, c["y"] - y0))

    # 게이지 채움 마스크
    cells = layout["gauge_cells"]
    H, W = full.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W]

    def gauge_mask(level):
        mk = np.zeros((H, W), bool)
        for i, (x0, x1, y0, y1, reg) in enumerate(cells):
            f = min(max(level - i, 0.0), 1.0)
            if f <= 0:
                continue
            edge = x0 + f * (x1 - x0 + 1) + 4 * np.sin(yy / 5.0) * (f < 1)
            mk |= reg & (xx < edge)
        return mk

    full_arr = np.asarray(base_full)

    def frame(loaded, hand_ang, heat, cyl_ang=0.0, overlays=(), under=()):
        fr = base_empty.copy()
        gm = gauge_mask(heat)
        if gm.any():
            g = np.asarray(fr).copy()
            g[gm] = full_arr[gm]
            fr = Image.fromarray(g)
        for im, (x, y) in under:                                # 실린더 뒤에 깔리는 이펙트
            fr.alpha_composite(im, (round(x - im.width / 2), round(y - im.height / 2)))
        for side in ("left", "right"):
            im, p = cyl_sp[side]
            c = layout["cylinder_" + side]
            paste_rot(fr, im, p, (c["x"], c["y"]), cyl_ang)
        for s, info in layout["slots"].items():
            if int(s) in loaded:
                c = layout["cylinder_" + info["side"]]
                t = math.radians(cyl_ang)
                dx, dy = info["x"] - c["x"], info["y"] - c["y"]
                x = c["x"] + dx * math.cos(t) - dy * math.sin(t)
                y = c["y"] + dx * math.sin(t) + dy * math.cos(t)
                fr.alpha_composite(cap, (round(x - cap.width / 2), round(y - cap.height / 2)))
        cc = layout["clock"]
        paste_rot(fr, hand, piv, (cc["x"], cc["y"]), hand_ang)
        for im, (x, y) in overlays:                              # 맨 위 이펙트
            fr.alpha_composite(im, (round(x - im.width / 2), round(y - im.height / 2)))
        return fr

    def slot_pos(s, cyl_ang):
        info = layout["slots"][s]
        c = layout["cylinder_" + info["side"]]
        t = math.radians(cyl_ang)
        dx, dy = info["x"] - c["x"], info["y"] - c["y"]
        return (c["x"] + dx * math.cos(t) - dy * math.sin(t), c["y"] + dx * math.sin(t) + dy * math.cos(t))
        return fr

    # 타임라인
    frames = []
    allslots = set(range(1, 13))
    heat = 0.0
    frames.append((frame(allslots, 30, heat), 600))
    n_fx = 7
    for k in range(1, 13):                                      # k발째: 벡터 폭발 7프레임, 바늘 · 게이지는 앞 3프레임에 이동
        loaded = set(range(k + 1, 13))
        a0, a1 = 30 * k, 30 * (k + 1) if k < 12 else 360
        h0, h1 = heat, heat + HEAT_PER_SHOT
        pos = (layout["slots"][k]["x"], layout["slots"][k]["y"])
        for i in range(1, n_fx + 1):
            t = min(i / 3, 1.0)
            ov = [(fxv.shot(i / n_fx, fx_size), pos)]
            frames.append((frame(loaded, a0 + (a1 - a0) * t, h0 + (h1 - h0) * t, overlays=ov), 40))
        heat = h1
        frames.append((frame(loaded, a1, heat), 120))

    # 장전: 실린더 두 바퀴, 맨 위를 지나는 약실에 한 발씩. 바늘도 두 바퀴 + 1시
    N = 40
    turns = 720.0
    smooth = lambda t: t * t * (3 - 2 * t)
    loaded = set()
    pops = []                                                   # (slot, 시작 프레임)
    events = {"right": [0.10 + 0.13 * j for j in range(6)], "left": [0.165 + 0.13 * j for j in range(6)]}
    cyl_size = int(np.mean([math.hypot(s["x"] - layout["cylinder_" + s["side"]]["x"], s["y"] - layout["cylinder_" + s["side"]]["y"])
                            for s in layout["slots"].values()]) * 3.3)
    pop_size = int(hole_d * 2.2)
    prev_ang = 0.0
    for f in range(1, N + 1):
        t = f / N
        ang = turns * smooth(t)
        speed = (ang - prev_ang) / (turns / N * 1.5)
        prev_ang = ang
        for side in ("left", "right"):
            while events[side] and events[side][0] <= t:
                events[side].pop(0)
                cand = [s for s in range(1, 13) if layout["slots"][s]["side"] == side and s not in loaded]
                c = layout["cylinder_" + side]
                def top_dist(s):
                    x, y = slot_pos(s, ang)
                    return abs((math.degrees(math.atan2(x - c["x"], -(y - c["y"]))) + 180) % 360 - 180)
                s_new = min(cand, key=top_dist)
                loaded.add(s_new)
                pops.append((s_new, f))
        ov = []
        for s, f0 in pops:
            tt = (f - f0) / 6
            if 0 <= tt <= 1:
                ov.append((fxv.load_pop(tt, pop_size), slot_pos(s, ang)))
        under = []
        frames.append((frame(set(loaded), 360 + (turns + 30) * smooth(t), heat, cyl_ang=ang, overlays=ov, under=under), 45))
    cc = layout["clock"]
    ray_size = int(cc["r"] * 2.9)
    for i in range(1, 9):                                       # 장전 끝: 시계 둘레 금빛
        frames.append((frame(allslots, 30, heat, overlays=[(fxv.ring_rays(i / 8, ray_size), (cc["x"], cc["y"]))]), 45))
    frames.append((frame(allslots, 30, heat), 500))

    # 레이어 · 위치 저장
    os.makedirs(OUT, exist_ok=True)
    base_empty.save(os.path.join(OUT, f"base_gauge_empty_{tag}.png"))
    base_full.save(os.path.join(OUT, f"base_gauge_full_{tag}.png"))
    for side in ("left", "right"):
        cyl_sp[side][0].save(os.path.join(OUT, f"cylinder_{side}_{tag}.png"))
    cap.save(os.path.join(OUT, f"cartridge_{tag}.png"))
    hand.save(os.path.join(OUT, f"hand_{tag}.png"))
    for old in ("base_flat.png", "base_toon.png"):
        p = os.path.join(OUT, old)
        if os.path.exists(p):
            os.remove(p)
    lay = {k: v for k, v in layout.items() if k != "gauge_cells"}
    lay["gauge_cells"] = [dict(x0=int(c[0]), x1=int(c[1]), y0=int(c[2]), y1=int(c[3])) for c in cells]
    lay["hand_pivot"] = list(piv)
    lay["cylinder_pivot"] = {s: list(cyl_sp[s][1]) for s in cyl_sp}
    lay["heat_per_shot"] = HEAT_PER_SHOT
    lay["note"] = ("좌표는 base 이미지 픽셀 기준. hand: pivot을 clock 중심에 두고 시각 h에 시계 방향 30*h도. "
                   "cylinder_*: pivot을 cylinder 중심에. 게이지는 gauge_full을 칸 안에서 왼쪽부터 열기 양만큼 보여 줌")
    with open(os.path.join(OUT, "hud_layout.json"), "w", encoding="utf-8") as f:
        json.dump(lay, f, ensure_ascii=False, indent=1)
    return frames


def flat_bg(im):
    bg = Image.new("RGBA", im.size, BGC + (255,))
    bg.alpha_composite(im)
    return bg.convert("RGB")


if __name__ == "__main__":
    for do_shade in (False, True):
        tag = "toon" if do_shade else "flat"
        frames = build(do_shade)
        g = [flat_bg(f) for f, _ in frames]
        gw = 640
        gs = [x.resize((gw, round(x.height * gw / x.width)), Image.LANCZOS) for x in g]
        gs[0].save(os.path.join(U, f"hud_fire_anim_{tag}.gif"), save_all=True, append_images=gs[1:],
                   duration=[d for _, d in frames], loop=0, disposal=2, optimize=True)
        # 정지 화면: 쏘기 전 · 1발 폭발 · 1발 뒤 · 6발 뒤 · 12발 뒤 · 장전 중 · 장전 끝
        per = 8
        rl = per * 12 + 1
        pick = [(0, "쏘기 전"), (2, "1발: 폭발"), (4, "1발: 연기"), (per * 6, "6발 뒤"), (per * 12, "12발 뒤 (열기 3칸)"),
                (rl + 12, "장전 중"), (rl + 26, "장전 중 (한 발씩)"), (rl + 42, "장전 끝 (금빛)")]
        th = [(g[i].resize((g[i].width // 2, g[i].height // 2), Image.LANCZOS), t) for i, t in pick]
        cols = 4
        W, H = th[0][0].size
        sheet = Image.new("RGB", (cols * W, ((len(th) + cols - 1) // cols) * (H + 34)), (27, 23, 38))
        d = ImageDraw.Draw(sheet)
        f = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 20)
        for j, (im, t) in enumerate(th):
            x, y = (j % cols) * W, (j // cols) * (H + 34)
            d.text((x + 10, y + 6), t, font=f, fill=(224, 166, 58))
            sheet.paste(im, (x, y + 34))
        sheet.save(os.path.join(U, f"hud_fire_frames_{tag}.png"))
        print(tag, len(frames), "frames")
