# GPT 시트(단색 회색 배경 · 격자 배치)를 물건별로 자름 → Tripo 입력
# 사용: python Art/tools/sheet_cut.py <시트.png> <열> <행> <이름1,이름2,...> [출력 폴더]
# 배경과 다른 픽셀을 덩어리(연결 성분)로 묶어 큰 덩어리 N개를 고르고, 위→아래 · 왼→오 순서로 이름을 붙임.
# 덩어리마다 여백을 두고 자른 뒤 정사각 캔버스(배경색) 가운데에 놓고, 다른 덩어리 픽셀은 배경색으로 지움.
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage


def cut(sheet: Path, cols: int, rows: int, names, out: Path, pad: int = 28):
    im = Image.open(sheet).convert("RGB")
    a = np.asarray(im).astype(int)
    H, W = a.shape[:2]
    edge = np.concatenate([a[:6].reshape(-1, 3), a[-6:].reshape(-1, 3), a[:, :6].reshape(-1, 3), a[:, -6:].reshape(-1, 3)])
    bg = np.median(edge, axis=0)
    fg = np.abs(a - bg).sum(axis=2) > 40
    grown = ndimage.binary_dilation(fg, iterations=12)          # 가까운 조각(불꽃 · 물방울)을 한 덩어리로
    lab, n = ndimage.label(grown)
    sizes = ndimage.sum(grown, lab, range(1, n + 1))
    keep = list(np.argsort(sizes)[::-1][: len(names)] + 1)
    boxes = ndimage.find_objects(lab)
    # 격자 순서: 행(세로 중심을 rows로 나눔) → 열(가로 중심)
    def key(l):
        sl = boxes[l - 1]
        cy, cx = (sl[0].start + sl[0].stop) / 2, (sl[1].start + sl[1].stop) / 2
        return (int(cy / H * rows), cx)
    keep.sort(key=key)
    out.mkdir(parents=True, exist_ok=True)
    bgc = tuple(int(v) for v in bg)
    for name, l in zip(names, keep):
        sl = boxes[l - 1]
        y0, y1 = max(0, sl[0].start - pad), min(H, sl[0].stop + pad)
        x0, x1 = max(0, sl[1].start - pad), min(W, sl[1].stop + pad)
        crop = a[y0:y1, x0:x1].copy()
        other = (lab[y0:y1, x0:x1] != l) & fg[y0:y1, x0:x1]
        crop[other] = bg
        cimg = Image.fromarray(crop.astype(np.uint8))
        s = max(cimg.size)
        canvas = Image.new("RGB", (s, s), bgc)
        canvas.paste(cimg, ((s - cimg.size[0]) // 2, (s - cimg.size[1]) // 2))
        p = out / f"{name}.png"
        canvas.save(p)
        print(p, cimg.size)


if __name__ == "__main__":
    sheet = Path(sys.argv[1])
    out = Path(sys.argv[5]) if len(sys.argv) > 5 else sheet.parent
    cut(sheet, int(sys.argv[2]), int(sys.argv[3]), sys.argv[4].split(","), out)
