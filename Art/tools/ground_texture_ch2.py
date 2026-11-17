# 2장 바닥 텍스처 (멈춘 기차역): 끝과 끝이 이어지는 툰 돌바닥 — 승강장 포석
#   python Art/tools/ground_texture_ch2.py → Game/Assets/Art/Env/Ch2/ground_ch2.png (1024)
# - 엇갈린 줄눈의 직사각 포석 8줄(줄마다 반 칸 밀림), 판석마다 색 · 밝기 조금씩 다르게
# - 줄눈 = 짙은 갈회색 선, 판석 가장자리는 툰 2단(살짝 밝은 윗변)
# - 검댕 얼룩 · 금 · 잔 자갈을 원환(타일링) 위치로 찍음. 맵은 따뜻한 색만 (몬스터가 차가운 보라라서)
import os
import numpy as np
from PIL import Image

N = 1024
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Art', 'Env', 'Ch2')
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(23)
yy, xx = np.mgrid[0:N, 0:N]

STONE = [np.array(c) for c in [(176, 158, 132), (166, 147, 121), (184, 166, 138), (158, 140, 116), (171, 150, 126)]]
GROUT = np.array((92, 78, 66))
LIGHT = np.array((14, 12, 9))           # 윗변 밝게 더함
SOOT = np.array((70, 60, 54))

rows = 8
rh = N // rows
cols = 5
cw = N // cols
img = np.zeros((N, N, 3), float)
for r in range(rows):
    off = (cw // 2) if r % 2 else 0
    y0 = r * rh
    for c in range(cols + 1):
        x0 = (c * cw + off) % N
        col = STONE[rng.integers(len(STONE))] * rng.uniform(0.95, 1.05)
        xs = (np.arange(cw) + x0) % N
        img[y0:y0 + rh][:, xs] = col
        # 윗변 밝게 (판석 두께감)
        img[y0 + 4:y0 + 10][:, xs] += LIGHT

# 줄눈: 가로 줄 + 세로 줄 (줄마다 밀림)
g = 5
for r in range(rows):
    y0 = r * rh
    img[y0:y0 + g, :] = GROUT
    off = (cw // 2) if r % 2 else 0
    for c in range(cols):
        x0 = (c * cw + off) % N
        xs = (np.arange(g) + x0) % N
        img[y0:y0 + rh][:, xs] = GROUT


def torus_dist(px, py):
    dx = np.abs(xx - px); dx = np.minimum(dx, N - dx)
    dy = np.abs(yy - py); dy = np.minimum(dy, N - dy)
    return np.sqrt(dx * dx + dy * dy)


# 검댕 얼룩 (부드러운 원 → 툰 2단으로 끊음)
for _ in range(7):
    px, py = rng.integers(0, N, 2)
    m = np.zeros((N, N), bool)
    for _k in range(5):                     # 원 몇 개를 겹친 불규칙한 얼룩, 한 단 톤
        ox, oy = rng.normal(0, 28, 2)
        m |= torus_dist((px + ox) % N, (py + oy) % N) < rng.uniform(18, 46)
    a = np.where(m, 0.2, 0.0)
    img = img * (1 - a[..., None]) + SOOT * a[..., None]

# 금 (짧은 꺾인 선)
for _ in range(14):
    x, y = rng.uniform(0, N, 2)
    ang = rng.uniform(0, np.pi)
    for _seg in range(rng.integers(3, 6)):
        ang += rng.uniform(-0.7, 0.7)
        L = rng.uniform(12, 30)
        for t in np.linspace(0, 1, int(L)):
            cx = int(x + np.cos(ang) * L * t) % N
            cy = int(y + np.sin(ang) * L * t) % N
            img[max(cy - 1, 0):cy + 1, max(cx - 1, 0):cx + 1] = GROUT
        x += np.cos(ang) * L; y += np.sin(ang) * L

# 잔 자갈 · 석탄 부스러기
for _ in range(160):
    px, py = rng.integers(0, N, 2)
    rad = rng.uniform(2, 5)
    d = torus_dist(px, py)
    m = d < rad
    img[m] = (40, 36, 34) if rng.random() < 0.4 else (205, 188, 160)

Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'ground_ch2.png'))
print('saved', os.path.join(OUT, 'ground_ch2.png'))
