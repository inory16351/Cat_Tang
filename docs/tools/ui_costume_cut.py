# 의상실 카드 초상화 (크롬 ChatGPT, 2026-11-24) — 4×2 칸 한 장(마젠타 바탕) → 코스튬별 그림 8장
# 원본: Art/ui_src/costume/cos_wardrobe_grid.png (시안 lineup_a/b 첨부 → "waist-up bust portrait" 요청)
# 칸 순서: 위 4001~4004, 아래 4005~4008 → Resources/UI/Costumes/cos_<Id>.png (가로 3 : 세로 2 칸에 맞춰 여백 채움)
# 실행: python docs/tools/ui_costume_cut.py
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
SRC = os.path.join(ROOT, 'Art', 'ui_src', 'costume', 'cos_wardrobe_grid.png')
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'UI', 'Costumes')
H_OUT = 360                                   # 카드 그림 칸 270×180의 2배

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    a = key_magenta(np.array(Image.open(SRC).convert('RGBA')))
    H, W = a.shape[:2]
    alpha = a[..., 3] > 40
    # 줄 나눔: 두 줄 사이 가장 빈 가로줄
    rows = alpha.sum(1)
    mid = H // 2 - 60 + int(np.argmin(rows[H // 2 - 60:H // 2 + 60]))
    for r, (y0, y1) in enumerate([(0, mid), (mid, H)]):
        cols = alpha[y0:y1].sum(0)
        cuts = [0] + [c * W // 4 - 40 + int(np.argmin(cols[c * W // 4 - 40:c * W // 4 + 40])) for c in (1, 2, 3)] + [W]
        for c in range(4):
            cell = a[y0:y1, cuts[c]:cuts[c + 1]]
            m = cell[..., 3] > 40
            ys, xs = np.nonzero(m)
            cell = cell[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
            im = Image.fromarray(cell)
            k = H_OUT / im.height
            im = im.resize((round(im.width * k), H_OUT), Image.LANCZOS)
            cid = 4001 + r * 4 + c
            im.save(os.path.join(OUT, f'cos_{cid}.png'))
            print(cid, im.size)
