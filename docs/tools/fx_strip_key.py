# 가로 N칸 이펙트 시트(마젠타 바탕)를 칸을 자르지 않고 통째로 투명 처리 → 파티클 텍스처 시트용
# 실행: python docs/tools/fx_strip_key.py <시트.png> <이름>  →  Game/Assets/Resources/FX/<이름>.png
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'FX')

if __name__ == '__main__':
    src, name = sys.argv[1], sys.argv[2]
    im = Image.open(src).convert('RGBA')
    out = Image.fromarray(key_magenta(np.array(im)))
    p = os.path.join(OUT, name + '.png')
    out.save(p)
    print('saved', os.path.normpath(p), out.size)
