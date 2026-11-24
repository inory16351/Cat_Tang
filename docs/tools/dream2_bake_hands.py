# 꿈2 비계 컷: 오려 낸 시곗바늘을 문자판에 '구워' 넣음 (2026-11-24 사용자: 시계탑 바늘도 비네트 맞춰야 — 따로 얹으면 붙여 놓은 티)
#   문자판은 비스듬히 보임 → 12시 · 3시 눈금 방향(그림에서 잰 벡터)으로 바늘을 기울여 펴고,
#   바늘 명도를 문자판 눈금 색으로 다시 칠한 뒤 문자판 결(물감 얼룩)을 곱하고, 아래쪽으로 옅은 그림자
#   scaffold_a~c = 11:59 · scaffold_d = 11:59(불 켜짐) · scaffold_d2 · d3 = 바늘이 다시 가기 시작 (12:01 · 12:03)
# 실행: python docs/tools/dream2_bake_hands.py → Resources/.../dream2/scaffold_*.jpg (원본 Art/story_src/dream2/scaffold_*.png)
import math, os
import numpy as np
from PIL import Image, ImageFilter

SRC = r"C:/Project/Cat_Tang/Art/story_src/dream2"
D = r"C:/Project/Cat_Tang/Game/Assets/Resources/Story/Dreams/dream2"
W = 1450
# 1450 폭 기준으로 잰 문자판 (scaffold_c): 축 · 12시 방향 · 3시 방향 (눈금 = 반지름의 약 0.92)
C = np.array([832.0, 360.0])
E12 = np.array([22.0, -331.0]) / 0.92
E3 = np.array([282.0, -85.0]) / 0.92
TICK = np.array([38, 92, 88], np.float32)        # 눈금 색 (짙은 청록)


def hand_layer(size, hand, theta_deg, length_frac, C=C):
    """바늘 그림을 문자판 평면에 펴서 size 크기 RGBA로"""
    hw, hh = hand.size
    a = np.asarray(hand).astype(np.float32)
    alpha = a[..., 3] / 255
    # 축 = 아래쪽 고리 가운데 (고리 맨 아래에서 위로 고리 반지름만큼)
    rows = np.where(alpha.max(axis=1) > 0.3)[0]
    px, py = hw / 2, rows.max() - hw * 0.42
    k = length_frac / (py - rows.min())             # 바늘 그림 px → 반지름 단위
    t = math.radians(theta_deg)                     # 12시 기준 시계 방향
    A = k * (math.cos(t) * E3 - math.sin(t) * E12)  # 바늘 가로(dx) → 그림
    B = k * (-math.sin(t) * E3 - math.cos(t) * E12) # 바늘 세로(dy, 아래 +) → 그림
    M = np.array([[A[0], B[0]], [A[1], B[1]]])
    Mi = np.linalg.inv(M)
    # PIL AFFINE: 출력 (x, y) → 입력 = P + Mi (out - C)
    c0 = px - (Mi[0, 0] * C[0] + Mi[0, 1] * C[1])
    c1 = py - (Mi[1, 0] * C[0] + Mi[1, 1] * C[1])
    return hand.transform(size, Image.AFFINE, (Mi[0, 0], Mi[0, 1], c0, Mi[1, 0], Mi[1, 1], c1), resample=Image.BICUBIC)


def bake(bg, hands):
    base = np.asarray(bg).astype(np.float32)
    out = base.copy()
    for layer in hands:
        la = np.asarray(layer).astype(np.float32)
        al = la[..., 3:4] / 255
        lum = (la[..., :3] @ np.array([0.299, 0.587, 0.114], np.float32))[..., None] / 255
        # 그림자: 아래 · 오른쪽으로 밀어 흐리게 → 문자판을 어둡게
        sh = Image.fromarray((al[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))
        sh = np.roll(np.roll(np.asarray(sh).astype(np.float32) / 255, 9, axis=0), 6, axis=1)[..., None]
        out = out * (1 - 0.28 * sh)
        # 바늘 색: 눈금 색 + 밝은 곳은 문자판 색으로 살짝 (금속 결) · 문자판 물감 결을 곱함
        grain = (base / np.maximum(1, base.mean(axis=(0, 1), keepdims=True))) ** 0.35
        col = TICK * (0.75 + 0.5 * lum) * grain
        out = out * (1 - al) + col * al
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGB")


hmin = Image.open(os.path.join(D, "hand_min.png")).convert("RGBA")
hhour = Image.open(os.path.join(D, "hand_hour.png")).convert("RGBA")
# (이름, 원본, 분 각도, 시 각도)  11:59 = 분 -6° · 시 -0.5°
CD = np.array([805.0, 362.0])                  # 불 켜진 d는 축(빨간 마개)이 조금 왼쪽
plan = [("scaffold_a", "scaffold_a", -6, -0.5, C), ("scaffold_b", "scaffold_b", -6, -0.5, C), ("scaffold_c", "scaffold_c", -6, -0.5, C),
        ("scaffold_d", "scaffold_d", -6, -0.5, CD), ("scaffold_d2", "scaffold_d", 6, 0.5, CD), ("scaffold_d3", "scaffold_d", 18, 1.5, CD)]
for name, src, mdeg, hdeg, cc in plan:
    bg = Image.open(os.path.join(SRC, src + ".png")).convert("RGB")
    bg = bg.resize((W, round(bg.height * W / bg.width)), Image.LANCZOS)
    layers = [hand_layer(bg.size, hhour, hdeg, 0.52, cc), hand_layer(bg.size, hmin, mdeg, 0.82, cc)]
    bake(bg, layers).save(os.path.join(D, name + ".jpg"), quality=92)
    print("baked", name)
