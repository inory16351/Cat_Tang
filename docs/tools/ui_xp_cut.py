# 경험치 UI 그림 (Codex CLI, 마젠타 바탕 — 프롬프트 docs/images/ui_src/prompts/xp_ui.txt) → Resources/UI
#  xp_bar.png    → UI/xp_bar.png    (내용 상자로 자름, 가로 1024 이하) + 안쪽 빈 홈 · 배지 위치를 재서 출력 (GameUI.XpSlot · XpBadge에 넣음)
#  heal_icon.png → UI/heal_icon.png (256 이하)
#  xp_orb.png    → UI/xp_orb.png    (128 이하)
# 실행: python docs/tools/ui_xp_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')


def keyed(path):
    return Image.fromarray(key_magenta(np.array(Image.open(path).convert('RGBA'))))


def cut(src, name, maxsize):
    im = keyed(os.path.join(src, name))
    im = im.crop(im.getbbox())
    im.thumbnail(maxsize, Image.LANCZOS)
    im.save(os.path.join(OUT, name))
    return im


def measure_bar(im):
    """어두운 보라(#1A1222 근처) 영역 = 빈 홈 · 배지 가운데. 오른쪽 큰 덩어리 = 홈, 왼쪽 = 배지"""
    a = np.array(im.convert('RGBA')).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    dark = (al > 200) & (r < 60) & (g < 45) & (b < 70) & (b >= g)
    h, w = dark.shape
    cols = dark.sum(axis=0)
    # 홈: 가운데 줄 근처에서 어두운 픽셀이 이어지는 가장 긴 가로 구간
    mid = dark[h // 2]
    best, cur, start, bs = 0, 0, 0, 0
    for x in range(w):
        if mid[x]:
            if cur == 0: start = x
            cur += 1
            if cur > best: best, bs = cur, start
        else:
            cur = 0
    x0, x1 = bs, bs + best
    rows = np.where(dark[:, x0 + best // 2 - 5: x0 + best // 2 + 5].sum(axis=1) > 5)[0]
    y0, y1 = rows.min(), rows.max()
    # 배지: 홈 왼쪽에서 어두운 픽셀 무게중심
    left = dark[:, :max(1, x0 - 10)]
    ys, xs = np.nonzero(left)
    bx, by = (xs.mean(), ys.mean()) if len(xs) else (w * 0.1, h * 0.5)
    print(f'XpSlot = new({x0 / w:.3f}f, {1 - y1 / h:.3f}f, {(x1 - x0) / w:.3f}f, {(y1 - y0) / h:.3f}f)')
    print(f'XpBadge = new({bx / w:.3f}f, {1 - by / h:.3f}f)')


if __name__ == '__main__':
    src = sys.argv[1]
    bar = cut(src, 'xp_bar.png', (1024, 1024))
    print('bar', bar.size)
    measure_bar(bar)
    print('heal', cut(src, 'heal_icon.png', (256, 256)).size)
    print('orb', cut(src, 'xp_orb.png', (128, 128)).size)
