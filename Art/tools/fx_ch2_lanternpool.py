# 11:59 가스등 랜턴 빛 웅덩이 플립북 (2026-11-25) — 위에서 내려다본 바닥 그림, FxPart.flat 으로 바닥에 눕혀 4초 재생
#   python Art/tools/fx_ch2_lanternpool.py
# 결과: Game/Assets/Resources/FX/anim/bk_c2lanternpool_0~23.png (256 정사각, 투명 배경)
# 그림: 툰 계단 빛 (크림 심지 → 호박 → 짙은 주황 테) · 가장자리는 젖은 바닥처럼 일렁임 · 비 오는 승강장 결 — 웅덩이 안에 빗방울 물결이 톡톡
# 시간: 0~2 프레임 확 켜짐(살짝 넘침) → 가스 불꽃처럼 숨 쉬듯 일렁임 → 마지막 4프레임 지직거리다 꺼짐
import os, math, random
import numpy as np
from PIL import Image

OUT = r"C:/Project/Cat_Tang/Game/Assets/Resources/FX/anim"
N, RES = 24, 256
rnd = random.Random(1159)

y, x = np.mgrid[0:RES, 0:RES].astype(np.float32)
u = (x + 0.5) / RES * 2 - 1
v = (y + 0.5) / RES * 2 - 1
r = np.sqrt(u * u + v * v)
ang = np.arctan2(v, u)

# 밴드 색 (sRGB): 심지 · 밝은 호박 · 호박 · 짙은 주황 · 테
# 바닥(적 · 탄)이 비쳐 보이게 반투명, 테두리만 진하게 — 장판 범위가 한눈에
BANDS = [(0.2, (255, 250, 226), 0.7), (0.42, (255, 236, 168), 0.6), (0.66, (255, 216, 118), 0.52), (0.9, (255, 194, 84), 0.46), (0.94, (255, 228, 150), 0.7), (1.0, (255, 156, 52), 0.88)]

# 빗방울 물결: (프레임, 중심, 최대 반지름)
drops = []
for f0 in range(1, N - 4):
    for _ in range(rnd.randint(1, 2)):
        a, d = rnd.uniform(0, math.tau), math.sqrt(rnd.uniform(0.0, 0.55))
        drops.append((f0, d * math.cos(a), d * math.sin(a), rnd.uniform(0.07, 0.12)))


def frame(f):
    t = f / (N - 1)
    # 밝기 · 크기: 켜짐 → 숨 쉼 → 지직 · 꺼짐
    if f <= 2:
        k = [0.55, 1.12, 1.0][f]
    elif f >= N - 4:
        k = [0.8, 0.35, 0.7, 0.0][f - (N - 4)]
    else:
        k = 1.0 + 0.05 * math.sin(f * 1.7) + 0.03 * math.sin(f * 4.1)
    if k <= 0:
        return Image.new("RGBA", (RES, RES), (0, 0, 0, 0))
    wob = 1 + 0.06 * np.sin(ang * 5 + f * 0.9) + 0.04 * np.sin(ang * 9 - f * 1.3) + 0.03 * np.sin(ang * 13 + 2.0)
    rr = r / (0.94 * min(1.0, k) * wob)
    img = np.zeros((RES, RES, 4), np.float32)
    lo = 0.0
    for edge, col, al in BANDS:
        m = (rr >= lo) & (rr < edge)
        img[m, :3] = col
        img[m, 3] = al * 255 * min(1.0, k)
        lo = edge
    # 빗방울 물결 고리: 밝은 크림 가는 고리가 퍼지며 옅어짐 (2프레임)
    for f0, cx, cy, rm in drops:
        age = f - f0
        if 0 <= age < 3:
            rad = rm * (0.45 + 0.55 * age / 2)
            d = np.sqrt((u - cx) ** 2 + (v - cy) ** 2)
            ring = np.abs(d - rad) < (0.012 + 0.004 * (2 - age))
            ring &= rr < 0.9
            a = [0.75, 0.5, 0.25][age]
            img[ring, :3] = img[ring, :3] * (1 - a) + np.array([255, 250, 230]) * a
            img[ring, 3] = np.maximum(img[ring, 3], 200 * a)
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGBA")


os.makedirs(OUT, exist_ok=True)
for f in range(N):
    frame(f).save(os.path.join(OUT, f"bk_c2lanternpool_{f}.png"))
print("DONE", N)
