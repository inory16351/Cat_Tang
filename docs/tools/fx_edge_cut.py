# 궁 준비 화면 테두리 불길 (Codex 그림, 마젠타 바탕) → 투명 PNG
# 실행: python docs/tools/fx_edge_cut.py docs/images/fx_src/edge0.png docs/images/fx_src/edge1.png ...
#  → Game/Assets/Resources/FX/edge/edge_<i>.png (1280x720)
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'FX', 'edge')

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for i, src in enumerate(sys.argv[1:]):
        im = Image.open(src).convert('RGBA').resize((1280, 720), Image.LANCZOS)
        out = Image.fromarray(key_magenta(np.array(im)))
        p = os.path.join(OUT, f'edge_{i}.png')
        out.save(p)
        print('saved', os.path.normpath(p))
