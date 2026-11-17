# 1장 바닥 텍스처: 끝과 끝이 이어지는(타일링) 툰 흙바닥 — 이음매 없이 방 전체에 한 장으로 반복
#   python Art/tools/ground_texture.py → Game/Assets/Art/Env/Ch1/ground_ch1.png (1024)
# - 주기적인 사인 합(정수 주파수)이라 가장자리가 정확히 이어짐
# - 갈라진 흙: 원환(토러스) 거리로 잰 보로노이 셀 경계 = 가는 짙은 금
# - 툰 색 띠 4단으로 끊고, 자갈 · 얼룩을 원환 위치로 찍음
import os
import numpy as np
from PIL import Image

N = 1024
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Art', 'Env', 'Ch1')
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(11)
y, x = np.mgrid[0:N, 0:N] / N


def periodic_noise(octaves):
    f = np.zeros((N, N))
    for (freq, amp) in octaves:
        for _ in range(6):
            kx, ky = rng.integers(-freq, freq + 1, 2)
            if kx == 0 and ky == 0:
                continue
            ph = rng.uniform(0, 2 * np.pi)
            f += amp * np.sin(2 * np.pi * (kx * x + ky * y) + ph)
    f -= f.min(); f /= f.max()
    return f


base = periodic_noise([(2, 1.0), (4, 0.6), (8, 0.35), (16, 0.2)])

# 보로노이 금 (원환 거리)
pts = rng.random((26, 2))
d1 = np.full((N, N), 9.0); d2 = np.full((N, N), 9.0)
for px, py in pts:
    dx = np.abs(x - px); dx = np.minimum(dx, 1 - dx)
    dy = np.abs(y - py); dy = np.minimum(dy, 1 - dy)
    d = np.sqrt(dx * dx + dy * dy)
    d2 = np.where(d < d1, d1, np.minimum(d2, d))
    d1 = np.minimum(d1, d)
edge = d2 - d1
wobble = periodic_noise([(12, 1.0), (24, 0.5)])
crack = (edge < 0.0022 + 0.0025 * wobble) & (periodic_noise([(3, 1.0)]) > 0.6)   # 금은 군데군데만

# 툰 색 띠 (밝은 모래 → 주황 흙 → 짙은 흙)
pal = np.array([[236, 170, 104], [222, 148, 86], [204, 126, 70], [178, 104, 58]], dtype=np.float32)
v = base * 0.8 + 0.2 * (1 - np.clip(edge * 18, 0, 1))      # 셀 가장자리로 갈수록 조금 짙게
band = np.clip((v * 4).astype(int), 0, 3)
band = 3 - band
img = pal[band]

# 자갈 · 얼룩
for _ in range(240):
    cx, cy = rng.random(2); r = rng.uniform(0.0025, 0.007)
    dx = np.abs(x - cx); dx = np.minimum(dx, 1 - dx)
    dy = np.abs(y - cy); dy = np.minimum(dy, 1 - dy)
    d = np.sqrt(dx * dx + (dy * 1.25) ** 2)
    m = d < r
    img[m] = [150, 86, 52]
    hl = (d < r * 0.55) & m & ((y - cy) < 0)
    img[hl] = [214, 150, 98]
for _ in range(30):
    cx, cy = rng.random(2); r = rng.uniform(0.02, 0.05)
    dx = np.abs(x - cx); dx = np.minimum(dx, 1 - dx)
    dy = np.abs(y - cy); dy = np.minimum(dy, 1 - dy)
    m = np.sqrt(dx * dx + dy * dy) < r * (0.8 + 0.4 * wobble)
    img[m] = img[m] * 0.93

img[crack] = [120, 66, 40]
Image.fromarray(img.astype(np.uint8)).save(os.path.join(OUT, 'ground_ch1.png'))
print('saved', os.path.join(OUT, 'ground_ch1.png'))
