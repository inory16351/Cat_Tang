# 건카타 총구 섬광 2D 스프라이트 (툰 · 잉크 외곽선). 3D 불꽃 메시(GunkataFx) 앞에 한 프레임 번쩍이는 용도.
# 날카로운 가시 별 모양: 바깥 붉은 주황 → 노랑 → 흰 심지, 진한 잉크 외곽선. 앞쪽(+x) 가시가 길게.
# 실행: python docs/tools/fx_flash.py  →  Game/Assets/Resources/FX/flash_0~2.png
import math, os, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'FX')
S = 512
SS = 2  # 슈퍼샘플


def star(cx, cy, n, r_in, r_out_fn, rot, jitter, rng):
    pts = []
    for i in range(n * 2):
        a = rot + math.pi * i / n + rng.uniform(-jitter, jitter)
        r = r_out_fn(a) if i % 2 == 0 else r_in * rng.uniform(0.85, 1.15)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def flash(seed):
    rng = random.Random(seed)
    W = S * SS
    img = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W * 0.36, W * 0.5
    n = rng.choice([7, 8, 9])
    rot = rng.uniform(0, 0.4)
    lens = [rng.uniform(0.55, 1.0) for _ in range(n * 2)]

    def outer(scale):
        def f(a):
            # 앞쪽(+x, a≈0) 가시는 길게, 뒤쪽은 짧게
            fwd = (math.cos(a) + 1) / 2
            k = lens[int(((a - rot) % (2 * math.pi)) / (math.pi / n)) % len(lens)]
            return W * scale * (0.2 + 0.42 * fwd ** 1.6) * k
        return f

    layers = [
        (1.0, 0.13, (40, 8, 12, 255)),      # 잉크 외곽선 (조금 더 큼)
        (0.92, 0.11, (214, 38, 22, 255)),   # 붉은
        (0.74, 0.09, (255, 128, 24, 255)),  # 주황
        (0.52, 0.075, (255, 214, 64, 255)), # 노랑
        (0.3, 0.05, (255, 252, 230, 255)),  # 흰 심지
    ]
    state = rng.getstate()
    for scale, rin, col in layers:
        rng.setstate(state)  # 층마다 같은 가시 배치 → 동심 별
        pts = star(cx, cy, n, W * rin * (scale + 0.4), outer(scale), rot, 0.05, rng)
        d.polygon(pts, fill=col)
    # 가운데 둥근 흰 빛
    r = W * 0.06
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 255, 245, 255))
    img = img.resize((S, S), Image.LANCZOS)
    return img


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for i in range(3):
        p = os.path.join(OUT, f'flash_{i}.png')
        flash(11 + i * 7).save(p)
        print('saved', os.path.normpath(p))
