# 대시 HUD 그림 (Codex, 마젠타 바탕) → Resources/UI
#  dash_icon.png → UI/dash_icon.png (내용 상자로 자름)
#  (캡슐 게이지 dash_cell.png는 세피리아식 아이콘 칸으로 바꾸며 안 씀)
# 실행: python docs/tools/ui_dash_cut.py docs/images/ui_src
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
    icon = keyed(os.path.join(src, 'dash_icon.png'))
    icon = icon.crop(icon.getbbox())
    icon.thumbnail((256, 256), Image.LANCZOS)
    icon.save(os.path.join(OUT, 'dash_icon.png'))
    print('icon', icon.size)
