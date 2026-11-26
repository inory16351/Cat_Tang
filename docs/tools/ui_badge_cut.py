# 보스 패턴 숫자 배지 테두리 (ChatGPT, 초록 바탕 2×2) → Resources/UI/badge/badge_{hour,timer,mark,boss}.png
#  + 블렌더 톱니 배지(badge_gear.png)에 어두운 외곽선 한 줄 (나무 바닥 위에서 묻히지 않게)
# 원본: docs/images/ui_src/badge_frames_sheet.png (2026-11-26, 크롬 ChatGPT)
# 실행: python docs/tools/ui_badge_cut.py
import os
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
SRC = os.path.join(ROOT, 'docs', 'images', 'ui_src', 'badge_frames_sheet.png')
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'UI', 'badge')
NAMES = [['hour', 'timer'], ['mark', 'boss']]
SIZE = 256


def key_green(a):
    rgb = a[..., :3].astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    green = g - np.maximum(r, b)                         # 초록이 얼마나 튀는지
    alpha = np.clip(1.0 - (green - 40) / 80.0, 0, 1)    # 40 아래 = 불투명, 120 위 = 투명
    # 테두리 초록 번짐 빼기
    spill = np.clip(green, 0, None) * (alpha > 0)
    rgb[..., 1] = np.where(alpha < 1, np.minimum(g, np.maximum(r, b)), g)
    out = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    return out


def outline(im, px=4, color=(26, 13, 12)):
    a = im.split()[3]
    grown = a.filter(ImageFilter.MaxFilter(px * 2 + 1))
    base = Image.new('RGBA', im.size, color + (0,))
    base.putalpha(grown)
    base.alpha_composite(im)
    return base


def square(im):
    l, t, r, b = im.getbbox()
    cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2 + 4
    return im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.fromarray(key_green(np.array(Image.open(SRC).convert('RGBA'))))
    w, h = sheet.size
    for row in range(2):
        for col in range(2):
            cell = sheet.crop((col * w // 2, row * h // 2, (col + 1) * w // 2, (row + 1) * h // 2))
            a = np.array(cell)
            a[..., 3] = np.where(a[..., 3] < 24, 0, a[..., 3])   # 바탕 잔털 정리
            cell = square(Image.fromarray(a))
            cell = cell.resize((SIZE, SIZE), Image.LANCZOS)
            p = os.path.join(OUT, f'badge_{NAMES[row][col]}.png')
            cell.save(p); print(p, cell.getbbox())
    g = os.path.join(OUT, 'badge_gear.png')
    if os.path.exists(g):
        im = Image.open(g).convert('RGBA')
        if im.size[0] > SIZE:
            # 가운데 법랑을 더 어둡게 (숫자 대비) — 다른 배지 가운데 색(#1a0d0c)에 맞춤
            a = np.array(im).astype(np.float32)
            n = im.size[0]; yy, xx = np.mgrid[0:n, 0:n]
            inside = (np.hypot(xx - n / 2, yy - n / 2) < n * 0.285) & (a[..., :3].max(-1) < 110)
            a[inside, :3] *= 0.42
            im = Image.fromarray(a.astype(np.uint8))
            im = outline(im, 8).resize((SIZE, SIZE), Image.LANCZOS)
            im.save(g); print(g)
