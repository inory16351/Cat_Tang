# 하나로 합친 하단 HUD: 시트에서 잘라 셰이더 입히고 인게임 화면에 배치
import sys
sys.path.insert(0, r"D:\pro\Cat_Tang\docs\tools")
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as nd
from icon_toon import mask_of, toon, PRESETS

U = r"D:\pro\Cat_Tang\docs\images\ui"
SCREEN = r"D:\pro\Cat_Tang\docs\images\05_zone1_western.png"
WORK_W = 900


def load_flat():
    a = np.asarray(Image.open(U + r"\sheet_hud_unified_flat.png").convert("RGB"), np.float32)
    # 타일 이음선 (가로 449 부근, 세로 800 부근)을 양옆 값으로 보간
    for y0, y1 in ((446, 454),):
        for y in range(y0 + 1, y1):
            t = (y - y0) / (y1 - y0)
            a[y] = a[y0] * (1 - t) + a[y1] * t
    for x0, x1 in ((797, 803),):
        for x in range(x0 + 1, x1):
            t = (x - x0) / (x1 - x0)
            a[:, x] = a[:, x0] * (1 - t) + a[:, x1] * t
    m = mask_of(a)
    ys, xs = np.nonzero(m)
    p = 20
    box = (max(xs.min() - p, 0), max(ys.min() - p, 0), min(xs.max() + p, a.shape[1]), min(ys.max() + p, a.shape[0]))
    im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).crop(box)
    return im.resize((WORK_W, round(im.height * WORK_W / im.width)), Image.LANCZOS)


def rgba(src, flat):
    m = nd.binary_dilation(mask_of(np.asarray(flat, np.float32)), iterations=5)
    a = nd.gaussian_filter(m.astype(np.float32), 1.0)
    return Image.fromarray(np.dstack([np.asarray(src.convert("RGB")), (a * 255).astype(np.uint8)]))


def place(hud, h_ratio=0.15):
    bg = Image.open(SCREEN).convert("RGBA")
    W, H = bg.size
    h = int(H * h_ratio)
    hud = hud.resize((round(hud.width * h / hud.height), h), Image.LANCZOS)
    bg.alpha_composite(hud, ((W - hud.width) // 2, H - hud.height - 6))
    return bg.convert("RGB")


if __name__ == "__main__":
    flat = load_flat()
    flat.save(U + r"\hud_unified_flat.png")
    shaded = toon(flat, ui=True, radius=0.018, **PRESETS["hades"])
    shaded.save(U + r"\hud_unified_toon.png")
    fa, ta = rgba(flat, flat), rgba(shaded, flat)
    fa.save(U + r"\hud_unified_flat_rgba.png")
    ta.save(U + r"\hud_unified_toon_rgba.png")
    # 비교: 위 플랫, 아래 셰이더 (작업 해상도)
    f = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 26)
    out = Image.new("RGB", (WORK_W, flat.height * 2 + 100), (27, 23, 38))
    d = ImageDraw.Draw(out)
    d.text((16, 10), "플랫 원본", font=f, fill=(224, 166, 58))
    out.paste(flat, (0, 50))
    d.text((16, flat.height + 58), "셰이더 적용 (2단 음영 · 잉크 · 외곽선 · 해칭 · 림)", font=f, fill=(224, 166, 58))
    out.paste(shaded, (0, flat.height + 100))
    out.save(U + r"\hud_unified_compare.png")
    # 인게임 배치: 기획 기준 높이 25% (이전 기준 15%와 비교용)
    place(ta, 0.15).save(U + r"\hud_unified_screen_15.png")
    place(ta, 0.25).save(U + r"\hud_unified_screen_25.png")
    print("ok", flat.size)
