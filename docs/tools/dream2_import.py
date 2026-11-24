# 꿈2 그림 정리: 크롬 다운로드 폴더의 d2_*.png → 원본 Art/story_src/dream2/ · 게임용 Resources/Story/Dreams/dream2/ (접두사 d2_ 뺌)
#   배경 · 셀 = 가로 1450으로 줄임 (꿈1과 같은 크기)
#   오브젝트(마젠타 바탕) = 마젠타를 알파로 빼고 그림 둘레로 자름: chase_cat_a~f (추적 원테이크 셀) · chase_tendrils · tower_hands → hand_min · hand_hour (둘로 나눔)
#   그 뒤 docs/tools/dream2_bake_hands.py (비계 바늘 굽기) → dream2_palettes.py (배색 판본) 순서로
# 실행: python docs/tools/dream2_import.py
import glob, os, shutil
import numpy as np
from PIL import Image

ROOT = r"C:/Project/Cat_Tang"
DL = os.path.expanduser("~/Downloads")
SRC = os.path.join(ROOT, "Art/story_src/dream2")
DST = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream2")
OBJECTS = {"tower_hands", "chase_tendrils"} | {f"chase_cat_{c}" for c in "abcdef"}   # 손(hand_*)은 통짜 그림 bolt_*로 바뀌어 안 씀
SKIP = {"hand_adult", "hand_young", "hand_old", "bolt_bg"}
os.makedirs(SRC, exist_ok=True)
os.makedirs(DST, exist_ok=True)


def key_magenta(im):
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # 마젠타 거리 → 알파 (가장자리 부드럽게) + 테두리 마젠타 번짐 빼기
    d = np.sqrt((r - 255) ** 2 + g ** 2 + (b - 255) ** 2)
    alpha = np.clip((d - 60) / 70, 0, 1)
    spill = np.minimum(r, b) - g
    fix = np.clip(spill, 0, None) * np.clip(1.6 - alpha, 0, 1)
    out = np.dstack([np.clip(r - fix, 0, 255), g, np.clip(b - fix, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def crop_alpha(im, pad=6):
    box = im.getchannel("A").point(lambda v: 255 if v > 20 else 0).getbbox()
    if not box:
        return im
    l, t, r, b = box
    return im.crop((max(0, l - pad), max(0, t - pad), min(im.width, r + pad), min(im.height, b + pad)))


def split_columns(im):
    """세로로 나란한 두 바늘을 빈 열 기준으로 나눔 (긴 쪽 = 분침)"""
    a = np.asarray(im.getchannel("A")) > 20
    cols = a.any(axis=0)
    runs, start = [], None
    for x, on in enumerate(cols):
        if on and start is None:
            start = x
        if not on and start is not None:
            runs.append((start, x)); start = None
    if start is not None:
        runs.append((start, len(cols)))
    runs = sorted(runs, key=lambda r: r[1] - r[0], reverse=True)[:2]
    parts = [crop_alpha(im.crop((x0, 0, x1, im.height))) for x0, x1 in sorted(runs)]
    parts.sort(key=lambda p: p.height, reverse=True)
    return parts


n = 0
for f in sorted(glob.glob(os.path.join(DL, "d2_*.png"))):
    name = os.path.basename(f)[3:-4]
    if name in SKIP:
        shutil.copy2(f, os.path.join(SRC, name + ".png")); continue
    shutil.copy2(f, os.path.join(SRC, name + ".png"))
    im = Image.open(f)
    if name in OBJECTS:
        cut = crop_alpha(key_magenta(im))
        if name == "tower_hands":
            parts = split_columns(cut)
            if len(parts) == 2:
                parts[0].save(os.path.join(DST, "hand_min.png")); parts[1].save(os.path.join(DST, "hand_hour.png"))
            else:
                print("tower_hands: 바늘 둘로 못 나눔", len(parts))
        elif name == "chase_tendrils":
            cut.save(os.path.join(DST, name + ".png"))   # 비율 그대로 (2026-11-24 사용자: 비율 깨짐 금지 — 늘리지 않음)
        else:
            cut.save(os.path.join(DST, name + ".png"))
    else:
        if im.width < im.height * 1.3:                     # 배경 · 프레임은 가로 그림이어야 함 (세로로 나온 건 다시 뽑기)
            print("세로 비율 — 다시 뽑을 것:", name, im.size)
        im = im.convert("RGB")
        w = 1450
        im.resize((w, round(im.height * w / im.width)), Image.LANCZOS).save(os.path.join(DST, name + ".jpg"), quality=92)   # 배경은 JPG (저장소 크기)
    n += 1
print("imported", n)
