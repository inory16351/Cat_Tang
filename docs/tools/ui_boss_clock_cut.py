# 1장 보스 '시각 패턴' 그림 (Codex, 마젠타 바탕) → Resources
#  boss_floor_clock.png → FX/boss_floor_clock.png (바닥 시계판, 1024 이하 정사각, 가운데 비움)
#  boss_floor_hand.png  → FX/boss_floor_hand.png  (바닥 큰 시곗바늘, 오른쪽을 가리킴, 축이 왼쪽 12%)
# 프롬프트: docs/images/ui_src/prompts/boss_floor_clock.txt
# 실행: python docs/tools/ui_boss_clock_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

RES = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources')


def keyed(path):
    return Image.fromarray(key_magenta(np.array(Image.open(path).convert('RGBA'))))


def square_crop(im):
    """내용 상자를 가운데 정사각으로 (시계판 · 고리는 가운데가 원의 중심이어야 함)"""
    l, t, r, b = im.getbbox()
    cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2
    return im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))


if __name__ == '__main__':
    src = sys.argv[1]
    os.makedirs(os.path.join(RES, 'FX'), exist_ok=True)
    p = os.path.join(src, 'boss_floor_clock.png')
    if os.path.exists(p):
        im = square_crop(keyed(p)); im.thumbnail((1024, 1024), Image.LANCZOS)
        im.save(os.path.join(RES, 'FX', 'boss_floor_clock.png')); print('dial', im.size)
    p = os.path.join(src, 'boss_floor_hand.png')
    if os.path.exists(p):
        im = keyed(p)  # 축 위치(왼쪽 12%)를 지키려고 자르지 않고 그대로 (가로 1024)
        im.thumbnail((1024, 1024), Image.LANCZOS)
        im.save(os.path.join(RES, 'FX', 'boss_floor_hand.png')); print('hand', im.size, im.getbbox())
