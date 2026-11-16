# 아이콘 시트를 칸마다 잘라 개별 PNG로 저장
# 같은 크기 격자로 자르면 칸을 넘친 그림이 잘리거나 옆 그림이 끼므로,
# 그림 덩어리(배경과 다른 영역)를 찾아 가장 가까운 칸에 배정한 뒤 덩어리 범위로 자른다.
# 사용: slice_icons.py <sheet.png> <cols> <rows> <out_dir> <id1,id2,...> <prefix>
# 저장: 배경을 투명으로 뺀 RGBA PNG (게임에서 아이콘 뒤에 남색 네모가 보이지 않게)
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as nd
from icon_toon import bg_alpha

OUT = 256
PAD = 0.08        # 그림 둘레 여백 (긴 변 대비)


def slice_sheet(sheet, cols, rows, out_dir, ids, prefix):
    os.makedirs(out_dir, exist_ok=True)
    im = Image.open(sheet).convert("RGB")
    a = np.asarray(im, np.float32)
    H, W = a.shape[:2]
    cw, ch = W / cols, H / rows

    # 배경색: 가장자리 띠의 중앙값
    edge = np.concatenate([a[:6].reshape(-1, 3), a[-6:].reshape(-1, 3), a[:, :6].reshape(-1, 3), a[:, -6:].reshape(-1, 3)])
    bg = np.median(edge, axis=0)
    fg = np.linalg.norm(a - bg, axis=2) > 38
    fg = nd.binary_opening(fg, iterations=1)
    blob = nd.binary_dilation(fg, iterations=6)          # 검은 외곽선 · 가까운 조각을 한 덩어리로
    lab, n = nd.label(blob)
    sizes = nd.sum(blob, lab, range(1, n + 1))
    cents = nd.center_of_mass(blob, lab, range(1, n + 1))

    # 덩어리마다 중심이 속한 칸 (너무 작은 티끌은 버림)
    owner = np.full(n + 1, -1)
    for k in range(n):
        if sizes[k] < 150:
            continue
        cy, cx = cents[k]
        c = min(int(cx // cw), cols - 1)
        r = min(int(cy // ch), rows - 1)
        owner[k + 1] = r * cols + c
    cell_of = owner[lab]                                  # 픽셀별 소속 칸 (-1 = 배경)

    # 칸이 비거나 덩어리가 칸보다 지나치게 크면(빛 번짐 많은 그림) 격자 자르기로 대신함
    boxes = []
    for idx in range(len(ids)):
        ys, xs = np.nonzero(cell_of == idx)
        if len(ys) == 0 or (np.ptp(ys) > ch * 1.6 or np.ptp(xs) > cw * 1.6):
            boxes = None
            break
        boxes.append((ys.min(), ys.max() + 1, xs.min(), xs.max() + 1))
    if boxes is None:
        print("fallback grid", sheet)
        boxes = [(round((k // cols) * ch), round((k // cols + 1) * ch), round((k % cols) * cw), round((k % cols + 1) * cw))
                 for k in range(len(ids))]
        keep = None
    else:
        keep = True

    for idx, i in enumerate(ids):
        y0, y1, x0, x1 = boxes[idx]
        side = int(max(y1 - y0, x1 - x0) * (1 + (PAD * 2 if keep else 0)))
        cy, cx = (y0 + y1) // 2, (x0 + x1) // 2
        if keep:
            # 자기 그림 덩어리 밖은 전부 배경색 (옆 그림 · 옆 그림자 제거)
            own = nd.gaussian_filter(nd.binary_dilation(cell_of == idx, iterations=3).astype(np.float32), 1.2)[..., None]
            clean = a * own + bg * (1 - own)
        else:
            # 격자 모드: 칸 밖은 옆 그림이므로 복사하지 않음
            clean = np.empty_like(a)
            clean[:] = bg
            clean[y0:y1, x0:x1] = a[y0:y1, x0:x1]
        canvas = np.empty((side, side, 3), np.float32)
        canvas[:] = bg
        sy0, sx0 = cy - side // 2, cx - side // 2
        ty0, tx0 = max(sy0, 0), max(sx0, 0)
        ty1, tx1 = min(sy0 + side, H), min(sx0 + side, W)
        canvas[ty0 - sy0:ty1 - sy0, tx0 - sx0:tx1 - sx0] = clean[ty0:ty1, tx0:tx1]
        alpha = bg_alpha(canvas, bg)                          # 배경색 영역 → 투명
        rgba = np.dstack([np.clip(canvas, 0, 255), alpha[..., None] * 255]).astype(np.uint8)
        out = Image.fromarray(rgba, "RGBA")
        out.resize((OUT, OUT), Image.LANCZOS).save(os.path.join(out_dir, f"{prefix}_{i}.png"))
    print("sliced", len(ids), sheet)


if __name__ == "__main__":
    s, c, r, o, ids, p = sys.argv[1:7]
    slice_sheet(s, int(c), int(r), o, ids.split(","), p)
