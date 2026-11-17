# 조준 커서 · 레벨 업 글자 (Codex, 마젠타 바탕) → Resources/UI
#  cursor_target.png → UI/cursor_target.png (표적지 커서, 가운데 맞춰 정사각, 256)   프롬프트 prompts/cursor_target.txt
#  level_up_text.png → UI/level_up_text.png ('LEVEL UP' 글자, 내용 상자, 가로 1024 이하) 프롬프트 prompts/level_up_text.txt
#  bullet_hole.png   → FX/bullet_hole.png   (총알 비 탄자국, 정사각 128)                  프롬프트 prompts/bullet_hole.txt
# 실행: python docs/tools/ui_cursor_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')


def keyed(path):
    return Image.fromarray(key_magenta(np.array(Image.open(path).convert('RGBA'))))


if __name__ == '__main__':
    src = sys.argv[1]
    p = os.path.join(src, 'cursor_target.png')
    if os.path.exists(p):
        im = keyed(p)
        l, t, r, b = im.getbbox()
        cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2
        im = im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))
        im.thumbnail((256, 256), Image.LANCZOS)
        im.save(os.path.join(OUT, 'cursor_target.png')); print('cursor', im.size)
    p = os.path.join(src, 'level_up_text.png')
    if os.path.exists(p):
        im = keyed(p)
        im = im.crop(im.getbbox())
        im.thumbnail((1024, 1024), Image.LANCZOS)
        im.save(os.path.join(OUT, 'level_up_text.png')); print('level_up', im.size)
    p = os.path.join(src, 'bullet_hole.png')
    if os.path.exists(p):
        im = keyed(p)
        l, t, r, b = im.getbbox()
        cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2
        im = im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))
        im.thumbnail((128, 128), Image.LANCZOS)
        im.save(os.path.join(OUT, '..', 'FX', 'bullet_hole.png')); print('bullet_hole', im.size)
