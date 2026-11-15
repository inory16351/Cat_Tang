# HUD 부품(플랫 / 셰이더 적용)을 인게임 화면 하단 가운데에 배치해 비교 이미지를 만든다
import sys
sys.path.insert(0, r"D:\pro\Cat_Tang\docs\tools")
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as nd
from icon_toon import mask_of

UI = r"D:\pro\Cat_Tang\docs\images\ui"
BGIMG = r"D:\pro\Cat_Tang\docs\images\05_zone1_western.png"


def cutout(name, kind):
    flat = Image.open(f"{UI}/flat/hud_{name}.png").convert("RGB")
    src = flat if kind == "flat" else Image.open(f"{UI}/toon/hud_{name}.png").convert("RGB")
    m = mask_of(np.asarray(flat, np.float32))
    m = nd.binary_dilation(m, iterations=5)
    a = nd.gaussian_filter(m.astype(np.float32), 1.0)
    rgba = np.dstack([np.asarray(src), (a * 255).astype(np.uint8)])
    ys, xs = np.nonzero(m)
    return Image.fromarray(rgba).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def fit_h(im, h):
    return im.resize((round(im.width * h / im.height), h), Image.LANCZOS)


def compose(kind):
    bg = Image.open(BGIMG).convert("RGBA")
    W, H = bg.size
    hud_h = int(H * 0.15)
    clock = fit_h(cutout("clock", kind), int(hud_h * 0.86))
    right = fit_h(cutout("cylinder_loaded", kind), int(hud_h * 0.70))
    left = fit_h(cutout("cylinder_empty", kind), int(hud_h * 0.70))
    gauge = fit_h(cutout("heat_gauge", kind), int(hud_h * 0.30))
    base = H - 10
    gy = base - gauge.height
    row_bottom = gy + 6
    cx = W // 2
    bg.alpha_composite(clock, (cx - clock.width // 2, row_bottom - clock.height))
    bg.alpha_composite(left, (cx - clock.width // 2 - 12 - left.width, row_bottom - left.height))
    bg.alpha_composite(right, (cx + clock.width // 2 + 12, row_bottom - right.height))
    bg.alpha_composite(gauge, (cx - gauge.width // 2, gy))
    return bg.convert("RGB")


if __name__ == "__main__":
    flat, toon = compose("flat"), compose("toon")
    toon.save(f"{UI}/hud_mock_toon.png")
    flat.save(f"{UI}/hud_mock_flat.png")
    # 하단 가운데 확대 비교
    W, H = flat.size
    box = (W // 2 - 260, H - 175, W // 2 + 260, H)
    z = 2
    fz = flat.crop(box).resize(((box[2] - box[0]) * z, (box[3] - box[1]) * z), Image.LANCZOS)
    tz = toon.crop(box).resize(fz.size, Image.LANCZOS)
    out = Image.new("RGB", (fz.width, fz.height * 2 + 120), (27, 23, 38))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 30)
    d.text((20, 12), "플랫 원본", font=f, fill=(224, 166, 58))
    out.paste(fz, (0, 60))
    d.text((20, fz.height + 72), "셰이더 적용 (2단 음영 · 잉크 · 외곽선 · 해칭 · 림)", font=f, fill=(224, 166, 58))
    out.paste(tz, (0, fz.height + 120))
    out.save(f"{UI}/hud_compare_zoom.png")
    print("ok", out.size)
