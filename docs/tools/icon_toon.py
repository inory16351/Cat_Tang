# 플랫 아이콘에 툰 셰이더 효과를 2D로 입힌다 (Unity UI 셰이더로 옮길 수식의 시험판)
# 단계: 마스크 → 원본 선으로 부품 분리 → 부품별 높이(거리장) → 노멀 → 2단 음영 + 잉크 경계 + 해칭 + 림 + 굵은 외곽선
# 사용: icon_toon.py <in.png|in_dir> <out.png|out_dir> [preset] [ui]
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as nd

BG = np.array([27, 23, 38], np.float32)
INK = np.array([18, 12, 26], np.float32) / 255
SHADE = np.array([0.60, 0.52, 0.76], np.float32)   # 그늘 곱하기 색 (보랏빛)
RIM = np.array([1.0, 0.93, 0.78], np.float32)

PRESETS = {
    # 이름: (음영, 잉크 경계, 해칭, 림, 외곽선 px)
    "shade":   dict(shade=1, ink=0, hatch=0, rim=0, outline=0),
    "ink":     dict(shade=1, ink=1, hatch=0, rim=0, outline=6),
    "hades":   dict(shade=1, ink=1, hatch=1, rim=1, outline=8),
}


def mask_of(rgb):
    d = np.linalg.norm(rgb - BG, axis=2)
    m = d > 22
    m = nd.binary_closing(m, iterations=2)
    m = nd.binary_fill_holes(m)
    m = nd.binary_opening(m, iterations=1)
    return m


def height_of(m, radius, d=None):
    # 가장자리에서 안쪽으로의 거리 → 둥근 단면 (스티커를 부풀린 모양)
    if d is None:
        d = nd.distance_transform_edt(m)
    x = np.clip(d / radius, 0, 1)
    h = np.sqrt(1 - (1 - x) ** 2)
    return nd.gaussian_filter(h, radius * 0.15) * radius


def merged_distance(m, parts, alb, size):
    """UI용: 숫자 · 눈금 같은 가는 검은 선으로 갈라진 같은 색 면을 하나로 묶어 거리장을 만든다."""
    dark = m & ~parts
    k = max(1, int(size * 0.008))
    thick = nd.binary_opening(dark, iterations=k)          # 굵은 검은 면(빈 약실 등)은 그대로 면
    tl, tn = nd.label(thick)
    if tn:                                                  # 숫자 '8' · '0' 같은 작은 덩어리는 면이 아님
        ta = nd.sum(thick, tl, range(1, tn + 1))
        thick = np.concatenate([[False], ta >= (size * 0.04) ** 2])[tl]
    base = parts | thick
    lab0, n0 = nd.label(base)
    if n0 == 0:
        return nd.distance_transform_edt(m)
    idx = nd.distance_transform_edt(lab0 == 0, return_distances=False, return_indices=True)
    full = np.where(m, lab0[tuple(idx)], 0)                # 가는 선은 가까운 면에 붙임
    ids = np.arange(1, n0 + 1)
    col = np.stack([nd.mean(alb[..., c], lab0, ids) for c in range(3)], 1)
    parent = np.arange(n0 + 1)

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x
    pairs = set()
    for a, b in ((full[:, :-1], full[:, 1:]), (full[:-1, :], full[1:, :])):
        sel = (a != b) & (a > 0) & (b > 0)
        pairs.update(zip(a[sel].tolist(), b[sel].tolist()))
    area = nd.sum(m, full, np.arange(0, n0 + 1))
    small_lim = (size * 0.05) ** 2
    best = {}                                              # 작은 면 → 붙어 있는 가장 큰 면
    for a, b in pairs:
        for x, y in ((a, b), (b, a)):
            if area[x] < small_lim and (x not in best or area[y] > area[best[x]]):
                best[x] = y
    for a, b in pairs:
        if np.linalg.norm(col[a - 1] - col[b - 1]) < 0.15:
            ra, rb = find(a), find(b)
            if ra != rb:
                parent[rb] = ra
    for x, y in best.items():                              # 숫자 속 작은 조각 등은 색과 상관없이 바깥 면에 합침
        rx, ry = find(x), find(y)
        if rx != ry:
            parent[rx] = ry
    root = np.array([find(i) for i in range(n0 + 1)])
    merged = root[full]
    d = np.zeros(m.shape, np.float32)
    for r in np.unique(merged[merged > 0]):
        reg = merged == r
        ys, xs = np.nonzero(reg)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        crop = np.pad(reg[y0:y1, x0:x1], 1)
        dc = nd.distance_transform_edt(crop)[1:-1, 1:-1]
        d[y0:y1, x0:x1] = np.where(reg[y0:y1, x0:x1], dc, d[y0:y1, x0:x1])
    return d


def normals(h):
    gy, gx = np.gradient(h)
    n = np.dstack([-gx, gy, np.ones_like(h)])   # y는 화면 위가 +
    return n / np.linalg.norm(n, axis=2, keepdims=True)


def toon(img, shade=1, ink=1, hatch=1, rim=1, outline=8, scale=2, ui=False, radius=0.04):
    im = img.convert("RGB")
    im = im.resize((im.width * scale, im.height * scale), Image.LANCZOS)
    rgb = np.asarray(im, np.float32)
    m = mask_of(rgb)
    alb = rgb / 255
    size = max(m.shape)
    lum = alb @ np.array([0.3, 0.59, 0.11], np.float32)
    # 원본의 검은 선을 경계로 부품을 나눔 → 부품마다 따로 둥글게
    if ui:
        # UI: 경계값 근처 어두운 면(빈 게이지 칸 등)이 깜빡이며 잔선이 생기지 않게 밝기를 다듬고 기준을 올림
        parts = m & (nd.median_filter(lum, size=5) > 0.23)
    else:
        parts = m & (lum > 0.17)
    parts = nd.binary_opening(parts, iterations=1)
    # 어두운 면 속 잔 티끌은 부품이 아님 (림 · 잉크 점이 박히는 원인)
    lab, n = nd.label(parts)
    if n:
        area = nd.sum(parts, lab, range(1, n + 1))
        keep = np.concatenate([[False], area >= (size * 0.012) ** 2])
        parts = keep[lab]
    R = size * radius                            # 둥글림 폭 (그림 긴 변 대비)
    if ui:
        # UI 부품: 숫자 · 눈금으로 갈라진 같은 색 면을 하나로 보고 둥글림
        n = normals(height_of(parts, R, merged_distance(m, parts, alb, size)))
    else:
        n = normals(height_of(parts, R))
    L = np.array([-0.6, 0.6, 0.5]); L /= np.linalg.norm(L)   # 왼쪽 위에서 오는 빛
    ndl = (n * L).sum(2)
    th = 0.30
    shadow = (ndl < th) & parts

    out = alb.copy()
    if shade:
        sf = nd.gaussian_filter(shadow.astype(np.float32), 0.6)[..., None]
        out = alb * (1 - sf) + alb * SHADE * sf
    if ink:
        band = (np.abs(ndl - th) < 0.06) & nd.binary_erosion(parts, iterations=3)
        bf = nd.gaussian_filter(band.astype(np.float32), 0.7)[..., None] * 0.8
        out = out * (1 - bf) + INK * bf
    if hatch:
        yy, xx = np.mgrid[0:m.shape[0], 0:m.shape[1]]
        period = 5.5 * scale
        line = ((xx + yy) % period) < period * 0.3
        deep = nd.binary_erosion(shadow, iterations=2)
        hf = nd.gaussian_filter((line & deep).astype(np.float32), 0.6)[..., None] * 0.5
        out = out * (1 - hf) + INK * hf
    if rim:
        k = int(1.5 * scale)
        up_left = np.roll(parts, (k, k), axis=(0, 1))
        r = parts & ~up_left & ~shadow
        rf = nd.gaussian_filter(r.astype(np.float32), 0.6)[..., None] * 0.55
        out = out * (1 - rf) + RIM * rf

    bg = BG / 255
    res = np.where(m[..., None], out, bg)
    if outline:
        ring = nd.binary_dilation(m, iterations=outline) & ~m
        rf = nd.gaussian_filter(ring.astype(np.float32), 0.8)[..., None]
        res = np.where(m[..., None], res, bg * (1 - rf) + INK * rf)
    res = np.clip(res * 255, 0, 255).astype(np.uint8)
    return Image.fromarray(res).resize(img.size, Image.LANCZOS)


if __name__ == "__main__":
    # 파일 하나: icon_toon.py a.png b.png [preset] / 폴더 전체: icon_toon.py in_dir out_dir [preset]
    import os, glob
    src, dst = sys.argv[1], sys.argv[2]
    preset = sys.argv[3] if len(sys.argv) > 3 else "hades"
    ui = len(sys.argv) > 4 and sys.argv[4] == "ui"
    if os.path.isdir(src):
        os.makedirs(dst, exist_ok=True)
        files = sorted(glob.glob(os.path.join(src, "*.png")))
        for f in files:
            toon(Image.open(f), ui=ui, **PRESETS[preset]).save(os.path.join(dst, os.path.basename(f)))
        print("toon", len(files), "->", dst)
    else:
        toon(Image.open(src), ui=ui, **PRESETS[preset]).save(dst)
        print(dst)
