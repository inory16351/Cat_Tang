# 로비 '그림이 그려지는' 연출 재료 (2026-11-18 사용자: 로비 그림이 그려지는 듯한 연출)
#  Resources/UI/Lobby/key.png 에서
#   key_ink.png      — 잉크 선만 (검은 선 + 투명 바탕, 해칭 · 윤곽): 양피지 위에 먼저 그려짐
#   reveal_ink.png   — 선이 그려지는 순서 (흑백, 0 먼저 → 1 나중): 영화 오프닝처럼 펜 여러 자루가 선을 따라 한 줄씩 그어 나감
#                      (잉크 선 위로만 퍼지는 거리 = 선을 따라가는 시간. 주인공 → 시계 → 마을 → 하늘 · 땅 순서로 펜이 출발)
#   reveal_color.png — 색이 번지는 순서: 주인공 가슴 · 하늘 시계 쪽에서 바깥으로 번짐 + 물감 얼룩 결
# 실행: python docs/tools/ui_lobby_ink.py
import os
import numpy as np
from PIL import Image, ImageFilter

D = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI', 'Lobby')


def fractal(w, h, seed, octaves=5, stretch=(1.0, 1.0)):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        gw = max(2, int(4 * 2 ** o / stretch[0])); gh = max(2, int(4 * 2 ** o / stretch[1]))
        g = rng.random((gh, gw)).astype(np.float32)
        layer = np.array(Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)).astype(np.float32) / 255
        out += layer * amp; tot += amp; amp *= 0.5
    out /= tot
    return (out - out.min()) / (out.max() - out.min() + 1e-6)


FACE = (0.255, 0.33)   # key.png 속 고양이 얼굴 (LobbyScreen.Face와 같게)


def stroke_order(ink, mw, mh, lum):
    """영화 오프닝처럼 '선이 하나씩 그어지는' 순서.
    1) 진한 윤곽선(아주 어두운 잉크)만 골라 조각(연결된 선)마다 나눔 → 주인공에 가까운 조각부터 차례로 펜이 출발,
       조각 안에서는 선 위로만 퍼지는 거리(8방향)를 따라 그어 나감 (가는 선이라 '선을 긋는' 모양이 됨)
    2) 나머지 잉크(해칭 · 결)는 가장 가까운 윤곽선이 그어진 직후 조금씩 채워짐
    선 밖 픽셀은 가장 가까운 잉크의 시각으로 채움"""
    import heapq
    from scipy import ndimage
    rs = lambda arr: np.array(Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8)).resize((mw, mh), Image.BILINEAR)).astype(np.float32) / 255
    a, l = rs(ink), rs(lum)
    on = a > 0.3
    dark = on & (l < 0.11)
    it = max(3, round(3 * mw / 960))
    line = dark & ~ndimage.binary_erosion(dark, iterations=it)   # 굵은 어둠(하늘 · 그림자 덩어리)은 빼고 가는 선만
    line = line & ~ndimage.binary_dilation(ndimage.binary_erosion(dark, iterations=it), iterations=2 * it // 3)
    lab, n = ndimage.label(line, structure=np.ones((3, 3)))
    sizes = ndimage.sum(line, lab, range(1, n + 1))
    keep = np.zeros(n + 1, bool); keep[1:] = sizes >= 25 * (mw / 960) ** 2
    line = keep[lab]
    lab, n = ndimage.label(line, structure=np.ones((3, 3)))
    print('strokes', n)
    hx, hy = FACE[0] * mw, FACE[1] * mh                             # 고양이 얼굴에서 바깥으로 (갱플랭크 로그인 화면처럼 얼굴부터)
    coms = ndimage.center_of_mass(line, lab, range(1, n + 1))
    order = None   # 아래에서 얼굴과 가장 가까운 점 거리로 정함
    INF = 1e9
    T = np.full((mh, mw), INF, np.float64)
    speed = 1.0 / (mw * 2.2)
    nb = [(-1, -1, 1.414), (-1, 0, 1), (-1, 1, 1.414), (0, -1, 1), (0, 1, 1), (1, -1, 1.414), (1, 0, 1), (1, 1, 1.414)]
    ys, xs = np.nonzero(line)
    labs = lab[ys, xs]
    first = {}
    dist = np.hypot((xs - hx) / mw * 1.6, (ys - hy) / mh)
    best = {}
    for y, x, k, dd in zip(ys, xs, labs, dist):                   # 조각마다 얼굴에 가장 가까운 점에서 출발 → 선이 바깥으로 뻗어 나감
        if k not in best or dd < best[k]:
            best[k] = dd; first[k] = (y, x)
    order = sorted(first.keys(), key=lambda k: best[k])
    heap = []
    for rank, k in enumerate(order):
        st = 0.62 * (best[k] / max(best.values())) ** 0.9                # 얼굴에서 멀수록 늦게 출발
        y, x = first[k]
        T[y, x] = st
        heapq.heappush(heap, (st, y, x))
    while heap:
        t, y, x = heapq.heappop(heap)
        if t > T[y, x]:
            continue
        k = lab[y, x]
        for dy, dx, w in nb:
            ny, nx = y + dy, x + dx
            if 0 <= ny < mh and 0 <= nx < mw and lab[ny, nx] == k:
                nt = t + w * speed
                if nt < T[ny, nx]:
                    T[ny, nx] = nt
                    heapq.heappush(heap, (nt, ny, nx))
    lineT = np.where(line, T, INF)
    _, (ly, lx) = ndimage.distance_transform_edt(~line, return_indices=True)
    near = ndimage.gaussian_filter(lineT[ly, lx], 8 * mw / 960)              # 가까운 선 시각을 부드럽게 (네모난 경계 방지)
    jitter = fractal(mw, mh, 21, 6, stretch=(0.5, 1.4)) * 0.6 + fractal(mw, mh, 5, 7) * 0.4
    fillT = near + 0.12 + jitter * 0.3                           # 해칭 · 어두운 칠: 근처 선이 그어진 뒤 번지듯
    T = np.where(line, lineT, fillT)
    T = np.where(on | line, T, near + 0.1)
    T = T / max(1e-6, np.percentile(T, 99.7))
    return np.clip(T, 0, 1)


def save_gray(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), 'L').save(os.path.join(D, name))
    print(name)


if __name__ == '__main__':
    key = Image.open(os.path.join(D, 'key.png')).convert('RGB')
    w, h = key.size
    g = np.array(key.convert('L')).astype(np.float32) / 255
    # 잉크: 아주 어두운 칠 + 밝기 경계(윤곽 · 해칭)
    edges = np.array(key.convert('L').filter(ImageFilter.FIND_EDGES)).astype(np.float32) / 255
    dark = np.clip((0.22 - g) / 0.18, 0, 1)
    ink = np.clip(dark * 0.9 + np.clip((edges - 0.08) * 3.0, 0, 1), 0, 1)
    ink = np.array(Image.fromarray((ink * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32) / 255
    rgba = np.zeros((h, w, 4), np.uint8)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 26, 14, 20
    rgba[..., 3] = (ink * 255).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(os.path.join(D, 'key_ink.png'))
    print('key_ink.png', key.size)

    # 크기 반으로 (가면은 부드러워서 충분)
    mw, mh = w // 2, h // 2
    xs = np.linspace(0, 1, mw)[None, :].repeat(mh, 0)
    ys = np.linspace(0, 1, mh)[:, None].repeat(mw, 1)
    save_gray(stroke_order(ink, w, h, g), 'reveal_ink.png')   # 선 순서는 원본 해상도 (줌인 상태에서 계단이 안 보이게)
    # 색: 고양이 얼굴 · 하늘 시계(0.43, 0.25)에서 번짐
    d1 = np.hypot((xs - FACE[0]) * 1.6, ys - FACE[1])
    d2 = np.hypot((xs - 0.43) * 1.6, ys - 0.25)
    d = np.minimum(d1, d2 * 1.1)
    blots = fractal(mw, mh, 11, 6)
    m = d / d.max() * 0.7 + blots * 0.3
    save_gray((m - m.min()) / (m.max() - m.min()), 'reveal_color.png')
