# 메인 마을 텍스처 2장 (2026-11-18 사용자: 1장 재활용 X, '소멸의 여로' 느낌 — 새벽에 사라져 가는 서부 마을, 허공의 큰 섬)
#   python Art/tools/ground_texture_village.py → Game/Assets/Art/Env/Village/ground_village.png (1024, 끝과 끝이 이어짐)
#                                               · void_village.png (2048, 섬 아래 허공 판 한 장)
# - ground_village: 새벽빛 받은 서부 흙. 복숭아 · 모래 흙을 툰 3단으로 끊은 얼룩 + 마른 풀 결 + 조약돌 + 반짝이는 모래 알갱이(새벽 빛)
# - void_village: 섬 아래로 보이는 허공. 깊은 남색 → 위쪽(화면 먼 쪽 = 동쪽 지평선)으로 갈수록 복숭아 · 금빛 새벽.
#   은하수 띠(대각) + 별 3크기 + 은하수 안 옅은 빛구름, 툰처럼 단계로 끊음. 차가운 색은 하늘 · 허공에만 (물체는 따뜻하게)
import os
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Art', 'Env', 'Village')
os.makedirs(OUT, exist_ok=True)


def pnoise(N, octaves, seed):
    """이어지는 노이즈: 정수 주파수 사인 합 (가장자리가 맞음)"""
    yy, xx = np.mgrid[0:N, 0:N] / N
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


# ---------- 바닥 ----------
N = 1024
rng = np.random.default_rng(1159)
base = np.array([226, 178, 136], float)      # 서부 흙 (2026-11-18 사용자: 파란 바닥은 서부 느낌이 빠져서 원래 색으로)
light = np.array([240, 202, 160], float)
dark = np.array([214, 164, 126], float)
darker = np.array([202, 152, 118], float)
v = pnoise(N, [(3, 1.0), (6, 0.6), (12, 0.35)], 3)
img = np.empty((N, N, 3))
img[:] = base
img[v > 0.64] = light
img[v < 0.32] = dark
img[v < 0.16] = darker
# 바람 결: 가로로 길쭉한 옅은 줄 (툰 한 단)
w = pnoise(N, [(1, 0.4), (16, 1.0)], 9)
img[(w > 0.78) & (v > 0.36)] = light * 1.02
# 조약돌 · 마른 풀
yy, xx = np.mgrid[0:N, 0:N]
for _ in range(90):
    cx, cy, r = rng.integers(0, N), rng.integers(0, N), rng.integers(4, 11)
    dx = (xx - cx + N // 2) % N - N // 2
    dy = (yy - cy + N // 2) % N - N // 2
    d = np.hypot(dx, dy * 1.3)
    img[d < r] = (170, 128, 108)
    img[(d < r * 0.55) & (dy < 0)] = (214, 176, 148)
for _ in range(160):
    cx, cy = rng.integers(0, N), rng.integers(0, N)
    ang = rng.uniform(-1.2, -0.4)
    ln = rng.integers(10, 24)
    for t in range(ln):
        x = int(cx + np.cos(ang) * t) % N
        y = int(cy + np.sin(ang) * t) % N
        img[y, x] = (168, 140, 92)
        img[y, (x + 1) % N] = (168, 140, 92)
# 새벽 빛을 받아 반짝이는 모래 알갱이 (작은 + 모양)
for _ in range(220):
    cx, cy = rng.integers(0, N), rng.integers(0, N)
    c = (255, 236, 190) if rng.random() < 0.75 else (236, 244, 255)   # 새벽 빛 모래 알갱이 + 가끔 별빛
    for ox, oy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
        img[(cy + oy) % N, (cx + ox) % N] = c
# 부서진 시간: 바닥 곳곳에 옅게 새겨진 깨진 시계판 고리 + 눈금 (끊긴 호)
for _ in range(5):
    cx, cy, R = rng.integers(0, N), rng.integers(0, N), rng.integers(60, 120)
    dx = (xx - cx + N // 2) % N - N // 2
    dy = (yy - cy + N // 2) % N - N // 2
    d = np.hypot(dx, dy); ang = np.arctan2(dy, dx)
    gap = (np.sin(ang * 3 + rng.uniform(0, 6)) > -0.35)          # 군데군데 끊긴 고리
    img[(np.abs(d - R) < 2.2) & gap] = (246, 222, 186)
    for k in range(12):
        a = k * np.pi / 6
        tick = (np.abs(ang - a) < 0.025) | (np.abs(ang - a + 2 * np.pi) < 0.025)
        img[tick & (d > R - 14) & (d < R - 4) & gap] = (246, 222, 186)
Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'ground_village.png'))
print('saved ground_village.png')

# ---------- 허공 = 뒤집힌 밤하늘 (2026-11-18 사용자: 바깥은 밤하늘 뒤집힌 것처럼) ----------
# 섬 아래를 내려다보면 밤하늘이 펼쳐져 있음: 깊은 남색 → 가장자리 보랏빛, 은하수(구름처럼 겹친 빛 3단), 별 4종(점 · + · 4갈래 · 색별),
# 큰 초승달(뒤집혀 아래에 떠 있음), 별자리 점선, 먼 쪽 가장자리에만 얇은 새벽빛. 툰처럼 단계로 끊음
M = 2048
rng = np.random.default_rng(2359)
yy, xx = np.mgrid[0:M, 0:M] / M              # yy 0 = 그림 위 = 먼 쪽
img = np.empty((M, M, 3))
rad = np.hypot(xx - 0.5, (yy - 0.55) * 1.1)
wob = pnoise(M, [(2, 1.0), (5, 0.4)], 77) * 0.06
rr = rad + wob
steps = [(9, (44, 34, 96)), (0.62, (34, 28, 82)), (0.5, (24, 22, 66)), (0.38, (16, 16, 50)), (0.24, (11, 12, 40))]
img[:] = steps[0][1]
for lim, col in steps:
    img[rr < lim] = col
# 먼 쪽 가장자리만 얇은 새벽빛 (장밋빛 → 복숭아)
tt = yy + pnoise(M, [(3, 1.0)], 12) * 0.03
img[tt < 0.07] = (110, 62, 112)
img[tt < 0.04] = (190, 110, 120)
img[tt < 0.018] = (246, 170, 128)
# 은하수: 화면을 가로지르는 넓은 띠, 가장자리 들쭉날쭉 + 빛구름 3단 + 어두운 먼지 줄
band = np.abs((xx - 0.5) * 0.55 + (yy - 0.5) * 0.85)
cloud = pnoise(M, [(3, 1.0), (7, 0.7), (15, 0.4)], 5)
edge = pnoise(M, [(9, 1.0), (19, 0.5)], 41)
bw = band + (edge - 0.5) * 0.12
for lim, cl, a, col in ((0.13, 0.42, 0.22, (70, 64, 150)), (0.09, 0.52, 0.32, (120, 112, 200)), (0.055, 0.6, 0.42, (190, 184, 240)), (0.03, 0.7, 0.5, (240, 232, 255))):
    m = (bw < lim) & (cloud > cl)
    img[m] = img[m] * (1 - a) + np.array(col) * a
dust = pnoise(M, [(11, 1.0), (23, 0.5)], 63)
m = (bw < 0.05) & (dust > 0.72)
img[m] = img[m] * 0.55 + np.array([20, 16, 46]) * 0.45
# 별자리: 별 몇 개를 옅은 점선으로 이음
def dotted(p0, p1, col):
    n = int(np.hypot(p1[0] - p0[0], p1[1] - p0[1]) / 6)
    for k in range(n):
        if k % 2: continue
        x = int(p0[0] + (p1[0] - p0[0]) * k / n); y = int(p0[1] + (p1[1] - p0[1]) * k / n)
        img[y:y + 2, x:x + 2] = col
def star(cx, cy, size, col):
    for k in range(-size, size + 1):
        for (x, y) in ((cx + k, cy), (cx, cy + k)):
            if 0 <= x < M and 0 <= y < M:
                img[y, x] = col
for c in range(6):
    pts = [(int(rng.integers(200, M - 200)), int(rng.integers(300, M - 200)))]
    for _ in range(rng.integers(3, 6)):
        px, py = pts[-1]
        pts.append((int(np.clip(px + rng.integers(-150, 150), 50, M - 50)), int(np.clip(py + rng.integers(-150, 150), 120, M - 50))))
    for i in range(len(pts) - 1):
        dotted(pts[i], pts[i + 1], (150, 160, 220))
    for (x, y) in pts:
        star(x, y, 5, (230, 236, 255)); star(x, y, 1, (255, 255, 255))
# 별
cols = [(255, 250, 236), (220, 232, 255), (255, 226, 190), (200, 214, 255)]
for _ in range(9000):
    cx, cy = rng.integers(0, M), rng.integers(int(M * 0.03), M)
    near = abs((cx / M - 0.5) * 0.55 + (cy / M - 0.5) * 0.85) < 0.12
    r = rng.random()
    col = cols[rng.integers(0, 4)]
    if r < 0.82:
        img[cy, cx] = col
        if near and cx + 1 < M: img[cy, cx + 1] = col
    elif r < 0.975:
        star(cx, cy, 2, col)
    else:
        star(cx, cy, 7, col); star(cx, cy, 2, (255, 255, 255))
# 큰 초승달 (뒤집힌 하늘이라 아래쪽에 떠 있음) + 달무리
mx, my, mr = int(M * 0.74), int(M * 0.66), 120
d = np.hypot(xx * M - mx, yy * M - my)
for k, (rr2, col, a) in enumerate(((3.2, (70, 70, 140), 0.3), (2.3, (100, 100, 170), 0.3), (1.6, (150, 150, 210), 0.3))):
    m = d < mr * rr2
    img[m] = img[m] * (1 - a) + np.array(col) * a
moon = d < mr
cut = np.hypot(xx * M - (mx + 55), yy * M - (my - 40)) < mr * 0.92
img[moon & ~cut] = (246, 244, 226)
crater = (np.hypot(xx * M - (mx - 60), yy * M - (my + 30)) < 18) | (np.hypot(xx * M - (mx - 30), yy * M - (my + 75)) < 11)
img[moon & ~cut & crater] = (214, 212, 200)
Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(os.path.dirname(__file__), '..', '..', 'docs', 'images', 'village_src', 'void_village_procedural.png'))   # 지금 쓰는 밤하늘은 Codex 그림(docs/images/village_src/night_sky.png)
print('saved void_village.png')

# ---------- 구름층 (섬과 밤하늘 사이, 반투명 · 천천히 흐름) ----------
C = 1024
cl = pnoise(C, [(2, 1.0), (4, 0.7), (8, 0.35), (16, 0.15)], 909)
yy2, xx2 = np.mgrid[0:C, 0:C] / C
rgba = np.zeros((C, C, 4), np.uint8)
for lim, col, a in ((0.55, (96, 104, 180), 70), (0.64, (140, 146, 214), 110), (0.72, (190, 194, 240), 150), (0.8, (232, 230, 255), 175)):
    m = cl > lim
    rgba[m, 0], rgba[m, 1], rgba[m, 2], rgba[m, 3] = col[0], col[1], col[2], a
Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, 'cloud_village.png'))
print('saved cloud_village.png')
