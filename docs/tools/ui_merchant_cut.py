# 상인 화면 밖 화살표 (Codex, 마젠타 바탕, 프롬프트 prompts/merchant_arrow.txt) → Resources/UI/merchant_arrow.png
# 회전 중심 = 그림 가운데라서 자르지 않고 정사각 그대로 256으로 줄임 (EdgeMarker가 메달 가운데에 코인을 얹음)
# 실행: python docs/tools/ui_merchant_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')

if __name__ == '__main__':
    im = Image.fromarray(key_magenta(np.array(Image.open(os.path.join(sys.argv[1], 'merchant_arrow.png')).convert('RGBA'))))
    im = im.resize((256, 256), Image.LANCZOS)
    im.save(os.path.join(OUT, 'merchant_arrow.png'))
    print('merchant_arrow', im.size)
