# 3장 바닥 텍스처 (신기루 사막 = 거울 세계): 끝과 끝이 이어지는 툰 바닥 2장
#   python Art/tools/ground_texture_ch3.py → Game/Assets/Art/Env/Ch3/ground_ch3.png · ground_ch3_cloud.png (1024)
# - ground_ch3: 노을 하늘을 비추는 거울 호수 판석. 큰 정사각 판(4×4, 판 하나 = 2m) + 가는 금빛 줄눈,
#   판마다 밝기 조금씩, 하늘 반사 = 주기 노이즈로 만든 구름 띠(복숭아 · 장미 · 크림, 툰 3단으로 끊음), 판 위 대각 반짝임 선
# - ground_ch3_cloud: 4스테이지 '뒤집힌 하늘' 구름 바닥. 크림 · 분홍 구름 덩어리(툰 2단) + 옅은 그림자
# 맵은 따뜻한 색만 (몬스터가 차가운 보라 · 청회색 + 빨간 테두리라서, Art/README.md 6절)
import os
import numpy as np
from PIL import Image

N = 1024
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Art', 'Env', 'Ch3')
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(31)
yy, xx = np.mgrid[0:N, 0:N] / N          # 0..1


def pnoise(octaves, seed):
    """이어지는 노이즈: 정수 주파수 사인 합 (가장자리가 맞음)"""
    r = np.random.default_rng(seed)
    v = np.zeros((N, N))
    for f, amp in octaves:
        for _ in range(3):
            kx, ky = r.integers(-f, f + 1, 2)
            if kx == 0 and ky == 0:
                kx = f
            ph = r.uniform(0, 2 * np.pi)
            v += amp * np.sin(2 * np.pi * (kx * xx + ky * yy) + ph)
    v -= v.min()
    return v / v.max()


def band(v, cuts, cols):
    """툰 단계: v(0..1)를 cuts 기준으로 끊어 cols 색"""
    out = np.zeros((N, N, 3))
    idx = np.digitize(v, cuts)
    for i, c in enumerate(cols):
        out[idx == i] = c
    return out


# ---------- 거울 호수 판석 ----------
sky = pnoise([(1, 1.0), (2, 0.6), (3, 0.35), (5, 0.18)], 7)
img = band(sky, [0.38, 0.62, 0.8], [(186, 200, 238), (212, 196, 236), (240, 196, 216), (252, 214, 206)])   # 장미 → 복숭아 → 크림
# 구름 반사: 밝은 덩어리 (가로로 긴 띠 노이즈)
cl = pnoise([(1, 0.5), (2, 1.0), (4, 0.5), (7, 0.25)], 11)
cl = cl * 0.7 + pnoise([(3, 1.0), (6, 0.4)], 13) * 0.3
m = cl > 0.66
img[m] = img[m] * 0.25 + np.array((255, 250, 248)) * 0.75
m2 = (cl > 0.6) & (cl <= 0.66)
img[m2] = img[m2] * 0.6 + np.array((255, 244, 246)) * 0.4

# 판마다 밝기 ±
tiles = 4
tw = N // tiles
for i in range(tiles):
    for j in range(tiles):
        img[j * tw:(j + 1) * tw, i * tw:(i + 1) * tw] *= rng.uniform(0.98, 1.02)

# 판 가장자리 툰 2단: 윗변 · 왼변 밝게, 아랫변 · 오른변 살짝 어둡게
px = (np.arange(N) % tw)
edge_lo = (px[None, :] < 9) | (px[:, None] < 9)
edge_hi = (px[None, :] > tw - 8) | (px[:, None] > tw - 8)
img[edge_lo] = img[edge_lo] * 0.9 + 255 * 0.1
img[edge_hi] *= 0.93
# 줄눈: 가는 금빛 선
g = 3
grout = (px[None, :] < g) | (px[:, None] < g)
img[np.broadcast_to(grout, (N, N))] = (255, 255, 255)
gold = ((px[None, :] >= g) & (px[None, :] < g + 2)) | ((px[:, None] >= g) & (px[:, None] < g + 2))
img[np.broadcast_to(gold, (N, N))] = (232, 196, 132)

# 반짝임 선: 판마다 짧은 대각선 1~2개 (거울 광택)
for i in range(tiles):
    for j in range(tiles):
        for _ in range(rng.integers(1, 3)):
            cx, cy = i * tw + rng.uniform(0.2, 0.8) * tw, j * tw + rng.uniform(0.2, 0.8) * tw
            L = rng.uniform(30, 70)
            for t in np.linspace(-0.5, 0.5, int(L * 2)):
                x, y = int(cx + t * L) % N, int(cy - t * L) % N
                img[max(y - 2, 0):y + 2, max(x - 2, 0):x + 2] = img[max(y - 2, 0):y + 2, max(x - 2, 0):x + 2] * 0.4 + 255 * 0.6

# 금빛 반짝 점 (빛가루)
for _ in range(90):
    x, y = rng.integers(0, N, 2)
    r = rng.uniform(1.5, 3.5)
    d = np.hypot(np.minimum(np.abs(xx * N - x), N - np.abs(xx * N - x)), np.minimum(np.abs(yy * N - y), N - np.abs(yy * N - y)))
    img[d < r] = (255, 236, 180)

Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'ground_ch3.png'))
print('saved ground_ch3.png')

# ---------- 뒤집힌 하늘: 구름 바닥 ----------
c1 = pnoise([(2, 1.0), (3, 0.7), (5, 0.4), (8, 0.2)], 21)
c2 = pnoise([(2, 0.6), (4, 1.0), (7, 0.3)], 23)
v = c1 * 0.65 + c2 * 0.35
img = band(v, [0.35, 0.5, 0.66], [(230, 176, 182), (242, 200, 198), (250, 222, 214), (255, 242, 234)])
# 구름 덩어리 아래쪽 그림자 (같은 노이즈를 아래로 밀어 빼기)
sh = np.roll(v, 10, axis=0)
m = (sh > 0.66) & (v <= 0.66)
img[m] *= 0.9
Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'ground_ch3_cloud.png'))
print('saved ground_ch3_cloud.png')
