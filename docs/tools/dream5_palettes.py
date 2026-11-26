# 꿈3 (dream5) 배색 판본 — 꿈2 dream2_palettes.py처럼 스크립트로 (2026-11-26 사용자: 배색이 반복돼 지루함 → 구간마다 판을 바꿈)
#   <이름>_neg.jpg   = 흑백 음화 판: 밝기만 뒤집어 검은 종이에 흰 칼자국 (빨강은 빨강 그대로 — MGrade.Negative처럼 청록으로 안 뒤집힘)
#   <이름>_night.jpg = 푸르스름한 밤 회색 판: 밝기를 짙은 청회색 → 옅은 청회색으로 (빨강은 살짝 눌러 그대로)
#   빨강 바탕 + 검은 선 판(red_1159)은 선이 달라야 해서 ChatGPT로 따로 뽑음
# 실행: python docs/tools/dream5_palettes.py   (dream5_import.py 다음)
import os, uuid
import numpy as np
from PIL import Image

D = r"C:/Project/Cat_Tang/Game/Assets/Resources/Story/Dreams/dream5"
META = r"C:/Project/Cat_Tang/Game/Assets/Resources/Story/Dreams/dream2/bell_a.jpg.meta"
meta_text = open(META, encoding="utf-8").read()

NEG = ["rain_a", "rain_b", "rain_c", "hands_b", "hands_c", "pshadow_a", "pshadow_b", "pshadow_c", "floor_a", "floor_b", "floor_c",
       "key_hand", "veil_c", "gear_ecu", "split_hands", "minute_b", "cat_a"]
NIGHT = ["light_a", "light_b", "light_c", "light_d", "light_e", "light_f", "light_g", "back_chair", "back_chair_j", "bed_a", "bed_b"]
# 선 떨림 짝: ChatGPT로 다시 뽑으면 구도가 어긋나는 컷은 원본을 살짝 일그러뜨려 "다시 판 듯한" 둘째 판을 만듦 (<이름>_j)
BOIL = ["back_chair", "cat_c"]


def write_meta(path):
    m = path + ".meta"
    if os.path.exists(m):
        return
    lines = ["guid: " + uuid.uuid4().hex + "\n" if l.startswith("guid:") else l for l in meta_text.splitlines(True)]
    open(m, "w", encoding="utf-8", newline="\n").write("".join(lines))


def split(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    red = np.clip((r - np.maximum(g, b) - 35) / 70, 0, 1)[..., None]       # 빨강 잉크 가면
    lum = (0.3 * r + 0.59 * g + 0.11 * b)[..., None] / 255
    return red, lum


def ramp(lum, c0, c1, gamma=1.0):
    t = np.clip(lum, 0, 1) ** gamma
    return np.array(c0, np.float32) * (1 - t) + np.array(c1, np.float32) * t


def neg(a):
    red, lum = split(a)
    out = ramp(1 - lum, (10, 9, 10), (238, 232, 220), 1.15)               # 검은 종이 · 흰 칼자국
    return out * (1 - red) + np.array((214, 30, 34), np.float32) * red


def night(a):
    red, lum = split(a)
    out = ramp(lum, (9, 13, 22), (176, 190, 204), 1.1)                     # 짙은 청회색 → 옅은 청회색 (달빛)
    return out * (1 - red) + a * 0.85 * red


def split_hands():
    """좌우 분할: 진자를 잡은 손(pend_c) | 분침을 누른 손가락(minute_c) — 두 그림을 반씩 잘라 가운데 검은 칼자국 띠로 붙임"""
    W, H = 1450, 816
    def crop(name, cx, cy, k):
        im = Image.open(os.path.join(D, name + ".jpg")).convert("RGB")
        w, h = W * 0.5 * k, H * k
        x0 = min(max(0, cx * W - w / 2), W - w); y0 = min(max(0, cy * H - h / 2), H - h)
        return im.crop((round(x0), round(y0), round(x0 + w), round(y0 + h))).resize((W // 2, H), Image.LANCZOS)
    out = Image.new("RGB", (W, H), (12, 10, 10))
    out.paste(crop("pend_c", 0.52, 0.66, 0.78), (0, 0))
    out.paste(crop("minute_c", 0.36, 0.42, 0.78), (W // 2, 0))
    a = np.asarray(out).astype(np.float32)
    rnd = np.random.default_rng(1159)
    for y in range(H):                                                       # 가운데 띠: 칼로 판 듯 들쭉날쭉한 검은 줄
        hw = 7 + int(rnd.integers(0, 5))
        a[y, W // 2 - hw: W // 2 + hw] = (12, 10, 10)
    path = os.path.join(D, "split_hands.jpg")
    Image.fromarray(a.astype(np.uint8)).save(path, quality=92); write_meta(path)


def boil(name, seed=7):
    """선 떨림 둘째 판: 부드러운 무작위 변위(약 ±2.5px) + 선 굵기를 조금 바꿈 → 같은 그림을 다시 판 듯이"""
    from scipy.ndimage import map_coordinates, zoom, grey_erosion
    im = np.asarray(Image.open(os.path.join(D, name + ".jpg")).convert("RGB")).astype(np.float32)
    H, W = im.shape[:2]
    rnd = np.random.default_rng(seed)
    def field():
        f = rnd.normal(0, 1, (18, 32))
        return zoom(f, (H / 18, W / 32), order=3)[:H, :W] * 2.5
    dy, dx = field(), field()
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    out = np.dstack([map_coordinates(im[..., c], [yy + dy, xx + dx], order=1, mode="reflect") for c in range(3)])
    out = out * 0.9 + grey_erosion(out, size=(2, 2, 1)) * 0.1                      # 검은 선이 아주 살짝 굵어짐 (밝기는 거의 그대로 — 12fps 교대에서 깜빡이지 않게)
    path = os.path.join(D, name + "_j.jpg")
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(path, quality=92); write_meta(path)


split_hands()
for b in BOIL:
    boil(b)
n = 0
for names, tag, fn in ((NEG, "neg", neg), (NIGHT, "night", night)):
    for name in names:
        src = os.path.join(D, name + ".jpg")
        if not os.path.exists(src):
            print("없음:", name); continue
        a = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
        out = os.path.join(D, f"{name}_{tag}.jpg")
        Image.fromarray(np.clip(fn(a), 0, 255).astype(np.uint8)).save(out, quality=92)
        write_meta(out); n += 1
print("palettes", n)
