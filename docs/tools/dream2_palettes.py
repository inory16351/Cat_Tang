# 꿈2 배색 변주 (2026-11-24 사용자: "배색이 다 비슷함 → 지금 뽑은 장면들 배색을 다양하게"):
#   붓 결 · 구도는 그대로, 명도를 새 색 조합(4색 그라디언트 맵)으로 다시 입힌 판본을 만듦 → <이름>_<배색>.png
#   같은 셀 묶음은 같은 배색 (움직임이 이어지게), 컷 배치에서 이웃 컷끼리 색이 확 대비되게 고름 (DreamCuts2)
# 실행: python docs/tools/dream2_palettes.py  (원본 = Resources/Story/Dreams/dream2/<이름>.png)
import os
import numpy as np
from PIL import Image

D = r"C:/Project/Cat_Tang/Game/Assets/Resources/Story/Dreams/dream2"

def hx(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)

PAL = {
    "mt": ["23001f", "c2187f", "19c3b1", "f4fffd"],   # 자홍 · 청록
    "sr": ["1c0d05", "7a3b1e", "e0532c", "fbe3c3"],   # 세피아 · 주홍
    "pl": ["1a0430", "7a1fa2", "b6f23a", "fbffe8"],   # 보라 · 라임
    "iy": ["070720", "2c2a7a", "f6d34a", "fffbe5"],   # 남색 · 노랑 (달밤)
    "cc": ["200008", "a0122b", "36d1d6", "f0fffd"],   # 진홍 · 청록
    "vp": ["0b2b26", "1f7a5c", "f28fb1", "fff1e0"],   # 비리디언 · 분홍
    "bg": ["050403", "4a3410", "e0a92a", "fff4cf"],   # 검정 · 금
    "uo": ["0a0f3c", "2438c9", "ff7a1a", "ffe9c7"],   # 군청 · 주황
    "cy": ["04163a", "1f5fa8", "9cc9e8", "f3f8fb"],   # 청사진
    "og": ["102000", "3f6b12", "ff6a3d", "fff0d8"],   # 올리브 · 다홍
}
STOPS = [0.0, 0.38, 0.7, 1.0]

GROUPS = {   # 이름 묶음 → 만들 배색들
    ("spiral_a", "spiral_b", "spiral_c", "spiral_d", "spiral_e", "spiral_f"): ["mt", "sr", "og"],
    ("scaffold_a", "scaffold_b", "scaffold_c", "scaffold_d", "scaffold_d2", "scaffold_d3"): ["pl", "iy"],
    ("bolt_adult", "bolt_young", "bolt_old_a", "bolt_old_b"): ["cc", "vp", "uo"],
    ("bell_a", "bell_b", "bell_c", "bell_d"): ["cy", "pl", "og"],
    ("vane_a", "vane_b", "vane_c"): ["vp", "sr", "mt"],
    ("windows_a", "windows_b", "windows_c", "windows_d", "windows_e"): ["bg", "mt", "og"],
    ("lantern_a", "lantern_b", "lantern_c", "lantern_d", "lantern_e"): ["uo", "og", "cy"],
    ("fall_a", "fall_b", "fall_c"): ["pl", "cc", "og"],
    ("ink_a", "ink_b", "ink_c", "ink_d"): ["cc", "bg", "sr"],
    ("xp_spiral", "xp_hands", "xp_bell", "xp_tower", "xp_fall", "xp_lantern", "xp_twin", "xp_grip", "xp_abstract", "xp_gun"): ["cy", "vp", "bg", "mt"],
}


def gradient_map(im, pal, keep=0.12):
    a = np.asarray(im.convert("RGB")).astype(np.float32) / 255
    lum = 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]
    lo, hi = np.percentile(lum, 1.5), np.percentile(lum, 98.5)   # 묶음마다 명도를 끝까지 펼침 (대비)
    lum = np.clip((lum - lo) / max(1e-3, hi - lo), 0, 1)
    cols = [hx(c) for c in PAL[pal]]
    out = np.zeros(a.shape, np.float32)
    for i in range(len(STOPS) - 1):
        t0, t1 = STOPS[i], STOPS[i + 1]
        m = (lum >= t0) & (lum <= t1)
        k = ((lum - t0) / (t1 - t0))[..., None]
        out = np.where(m[..., None], cols[i] * (1 - k) + cols[i + 1] * k, out)
    out = out * (1 - keep) + a * 255 * keep                          # 원래 색을 조금 남겨 물감 얼룩이 살아 있게
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGB")


n = 0
for names, pals in GROUPS.items():
    for name in names:
        src = os.path.join(D, name + ".jpg")
        if not os.path.exists(src):
            print("없음", name); continue
        im = Image.open(src)
        for p in pals:
            gradient_map(im, p).save(os.path.join(D, f"{name}_{p}.jpg"), quality=92)
            n += 1
print("variants", n)

# 확인용 시트: 묶음마다 원본 + 배색 판본 (첫 장)
rows = []
for names, pals in GROUPS.items():
    first = names[0]
    tiles = [Image.open(os.path.join(D, first + ".jpg")).convert("RGB")] + [Image.open(os.path.join(D, f"{first}_{p}.jpg")) for p in pals]
    rows.append(tiles)
W, H = 300, 180
sheet = Image.new("RGB", (W * 4, H * len(rows)), (20, 20, 20))
for r, tiles in enumerate(rows):
    for c, t in enumerate(tiles):
        sheet.paste(t.resize((W, H)), (c * W, r * H))
os.makedirs(r"C:/Project/Cat_Tang/docs/images/dream2", exist_ok=True)
sheet.save(r"C:/Project/Cat_Tang/docs/images/dream2/palette_variants.jpg", quality=88)
print("sheet saved")
