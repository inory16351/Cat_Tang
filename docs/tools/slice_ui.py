# UI 스킨 시트를 부품별 투명 PNG로 자름 (배경 #1B1726 제거, 검은 외곽선 포함)
# 사용: slice_ui.py <sheet.png> <cols> <rows> <out_dir> <name1,name2,...>
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as nd

sys.path.insert(0, os.path.dirname(__file__))
from icon_toon import mask_of


def slice_ui(sheet, cols, rows, out_dir, names, dilate=5):
    os.makedirs(out_dir, exist_ok=True)
    a = np.asarray(Image.open(sheet).convert("RGB"), np.float32)
    H, W = a.shape[:2]
    cw, ch = W / cols, H / rows
    m = mask_of(a)
    blob = nd.binary_dilation(m, iterations=6)
    lab, n = nd.label(blob)
    sizes = nd.sum(blob, lab, range(1, n + 1))
    cents = nd.center_of_mass(blob, lab, range(1, n + 1))
    owner = np.full(n + 1, -1)
    for k in range(n):
        if sizes[k] < 200:
            continue
        cy, cx = cents[k]
        owner[k + 1] = min(int(cy // ch), rows - 1) * cols + min(int(cx // cw), cols - 1)
    cell_of = owner[lab]
    out = {}
    for idx, name in enumerate(names):
        mine = (cell_of == idx) & m
        mine = nd.binary_fill_holes(nd.binary_closing(mine, iterations=2))
        mine = nd.binary_dilation(mine, iterations=dilate)          # 검은 외곽선까지
        ys, xs = np.nonzero(mine)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        alpha = nd.gaussian_filter(mine.astype(np.float32), 0.7)[y0:y1, x0:x1]
        rgb = a[y0:y1, x0:x1]
        img = np.dstack([rgb, alpha * 255]).astype(np.uint8)
        Image.fromarray(img, "RGBA").save(os.path.join(out_dir, name + ".png"))
        out[name] = (x1 - x0, y1 - y0)
    return out


if __name__ == "__main__":
    s, c, r, o, names = sys.argv[1:6]
    res = slice_ui(s, int(c), int(r), o, names.split(","))
    for k, v in res.items():
        print(k, v)
