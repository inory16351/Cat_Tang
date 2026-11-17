# 치명타 피해 숫자 (Codex, 마젠타 바탕 — 프롬프트 docs/images/ui_src/prompts/crit_digits.txt, 참고 prompts/ref_digit.png)
#  crit_digits.png (5칸 × 2줄: 위 0~4, 아래 5~9) → Resources/UI/digits_crit/d0~d9.png (칸마다 내용 상자로 자름, 높이 일반 숫자와 같게)
# 실행: python docs/tools/ui_crit_digits_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

RES = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')

if __name__ == '__main__':
    src = sys.argv[1]
    im = Image.fromarray(key_magenta(np.array(Image.open(os.path.join(src, 'crit_digits.png')).convert('RGBA'))))
    w, h = im.size
    ref_h = Image.open(os.path.join(RES, 'digits', 'd0.png')).size[1]
    out = os.path.join(RES, 'digits_crit')
    os.makedirs(out, exist_ok=True)
    for n in range(10):
        r, c = divmod(n, 5)
        cell = im.crop((c * w // 5, r * h // 2, (c + 1) * w // 5, (r + 1) * h // 2))
        a = np.array(cell)[..., 3]
        # 칸 안에서 가장 큰 덩어리만 (옆 칸 글자 끝이 살짝 들어온 것 무시): 알파 > 0 열 · 행 범위
        ys, xs = np.nonzero(a > 40)
        g = cell.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        g = g.resize((max(1, round(g.size[0] * ref_h / g.size[1])), ref_h), Image.LANCZOS)
        g.save(os.path.join(out, f'd{n}.png'))
        print(n, g.size)
