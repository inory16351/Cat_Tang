# 로비 '선이 한 획씩 더해지며 그림이 완성되는' 순서 (2026-11-18 사용자: 색이 더해지는 느낌 말고 선이 하나씩 추가되는 느낌)
#  key_ink.png(잉크 선 + 해칭)의 뼈대를 따서 갈림길에서 끊어 '획'으로 나눔 → 펜 여러 자루가 사람처럼 한 획씩 그어 나감
#   - 획 안에서는 한쪽 끝에서 다른 쪽 끝으로 그어짐 (펜이 지나가는 모양)
#   - 1부: 굵고 진한 외곽선 획을 얼굴에서 가까운 것부터, 펜은 방금 끝난 자리에서 가까운 다음 획으로 (손이 이어서 그리는 느낌)
#   - 2부: 해칭 · 결 획을 펜 여러 자루가 빠르게 쌓음
#  → Resources/UI/Lobby/reveal_strokes.png (흑백, 0 먼저 → 1 나중. 선 밖은 1)
# 실행: python docs/tools/ui_lobby_strokes.py
import os, sys, heapq
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(__file__))
from ui_lobby_ink import FACE  # noqa: E402

D = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI', 'Lobby')


def thin(img):
    """Zhang-Suen 세선화 (numpy)"""
    a = np.pad(img.astype(np.uint8), 1)
    while True:
        changed = False
        for step in (0, 1):
            P2 = a[:-2, 1:-1]; P3 = a[:-2, 2:]; P4 = a[1:-1, 2:]; P5 = a[2:, 2:]
            P6 = a[2:, 1:-1]; P7 = a[2:, :-2]; P8 = a[1:-1, :-2]; P9 = a[:-2, :-2]
            c = a[1:-1, 1:-1]
            B = P2 + P3 + P4 + P5 + P6 + P7 + P8 + P9
            seq = [P2, P3, P4, P5, P6, P7, P8, P9, P2]
            A = sum(((seq[i] == 0) & (seq[i + 1] == 1)).astype(np.uint8) for i in range(8))
            if step == 0:
                m = (c == 1) & (B >= 2) & (B <= 6) & (A == 1) & (P2 * P4 * P6 == 0) & (P4 * P6 * P8 == 0)
            else:
                m = (c == 1) & (B >= 2) & (B <= 6) & (A == 1) & (P2 * P4 * P8 == 0) & (P2 * P6 * P8 == 0)
            if m.any():
                c[m] = 0
                changed = True
        if not changed:
            break
    return a[1:-1, 1:-1].astype(bool)


def neighbors(sk):
    k = np.ones((3, 3), int); k[1, 1] = 0
    return ndimage.convolve(sk.astype(int), k, mode='constant') * sk


def line_alpha(name):
    """선 그림의 '선' 세기 (RGBA면 알파, 흰 종이 위 선이면 어두운 정도)"""
    im = Image.open(os.path.join(D, name))
    if im.mode == 'RGBA':
        return np.array(im)[..., 3].astype(np.float32) / 255
    g = np.array(im.convert('L')).astype(np.float32) / 255
    return np.clip((np.percentile(g, 90) - g) * 1.6, 0, 1)


def trace_order(sk, W, H, fx, fy, pens=8, speed=1800.0, gap=0.02, stagger=0.25):
    """'트레이스' 모드: 뼈대를 갈림길 사이 선(조각)으로 나누고, 갈림길로 이어진 조각끼리 이웃으로 묶음.
    펜 여러 자루가 얼굴 가까운 조각부터 출발해, 방금 끝낸 자리와 이어진 이웃 조각을 따라 계속 그어 나감
    (이웃이 다 그려졌으면 얼굴에서 가까운 남은 조각 중 손에서 가까운 것으로 건너뜀). 조각 하나는 손에 가까운 끝에서 반대 끝으로.
    반환: 뼈대 픽셀별 시각 (np.inf = 없음), 전체 시간"""
    nb = neighbors(sk)
    jun = sk & (nb >= 3)
    junb = ndimage.binary_dilation(jun, iterations=1) & sk
    seg = sk & ~junb
    lab, n = ndimage.label(seg, structure=np.ones((3, 3)))
    jl, jn = ndimage.label(ndimage.binary_dilation(junb, iterations=1), structure=np.ones((3, 3)))
    idx = ndimage.find_objects(lab)
    segs = []
    adj = [set() for _ in range(n + 1)]
    for k in range(1, n + 1):
        sl = idx[k - 1]
        ys, xs = np.nonzero(lab[sl] == k)
        ys = ys + sl[0].start; xs = xs + sl[1].start
        pts = list(zip(ys.tolist(), xs.tolist()))
        # 이 조각이 닿는 갈림길 묶음
        js = set(jl[ys, xs].tolist()) - {0}
        segs.append(dict(k=k, pts=pts, js=js, cx=xs.mean(), cy=ys.mean(), d=np.hypot((xs.mean() - fx) / W * 1.6, (ys.mean() - fy) / H)))
    byj = {}
    for sgi, sg in enumerate(segs):
        for j in sg['js']:
            byj.setdefault(j, []).append(sgi)
    for lst in byj.values():
        for a_ in lst:
            for b_ in lst:
                if a_ != b_:
                    adj[a_].add(b_)
    order = sorted(range(len(segs)), key=lambda i: segs[i]['d'])
    done = [False] * len(segs)
    T = np.full((H, W), np.inf, np.float32)
    pen = [(i * stagger, (fy, fx), -1) for i in range(pens)]
    heapq.heapify(pen)
    ptr = 0; left = len(segs); tmax = 0.0

    def draw(i, t, pos):
        sg = segs[i]
        pts = sg['pts']
        pset = set(pts)
        start = min(pts, key=lambda p: (p[0] - pos[0]) ** 2 + (p[1] - pos[1]) ** 2)
        dist = {start: 0}; q = [start]
        while q:
            nq = []
            for (y, x) in q:
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        pp = (y + dy, x + dx)
                        if pp in pset and pp not in dist:
                            dist[pp] = dist[(y, x)] + 1; nq.append(pp)
            q = nq
        L = max(dist.values()) + 1
        dur = L / speed
        for (y, x), d in dist.items():
            T[y, x] = t + dur * d / max(1, L - 1)
        end = max(dist, key=dist.get)
        return t + dur, end

    while left:
        t, pos, last = heapq.heappop(pen)
        nxt = None
        if last >= 0:
            cands = [j for j in adj[last] if not done[j]]
            if cands:
                nxt = min(cands, key=lambda j: (segs[j]['cx'] - pos[1]) ** 2 + (segs[j]['cy'] - pos[0]) ** 2)
        if nxt is None:
            while ptr < len(order) and done[order[ptr]]:
                ptr += 1
            window = [order[q] for q in range(ptr, min(len(order), ptr + 12)) if not done[order[q]]]
            nxt = min(window, key=lambda j: (segs[j]['cx'] - pos[1]) ** 2 + (segs[j]['cy'] - pos[0]) ** 2)
        done[nxt] = True; left -= 1
        t1, end = draw(nxt, t, pos)
        tmax = max(tmax, t1)
        heapq.heappush(pen, (t1 + gap, end, nxt))
    # 갈림길 픽셀 = 이웃 조각 중 이른 시각
    known = np.isfinite(T)
    if known.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
        T = np.where(sk & ~known, T[iy, ix], T)
    print('trace segments', len(segs), 'time', round(tmax, 2))
    return T, tmax


if __name__ == '__main__':
    # 사용: python ui_lobby_strokes.py [선 그림 이름=key_ink.png] [출력=reveal_strokes.png] [선 문턱=0.32] [cut|join|trace] [trace 펜 수] [trace 속도]
    #  trace = 갈림길 사이 선을 펜이 이어진 길을 따라 한 줄씩 (2026-11-18 사용자: 맨 종이부터 천천히 — 큰 선 덩어리가 한순간에 그려지던 문제)
    #  cut  = 갈림길마다 끊은 짧은 획이 흩뿌려지듯 쌓임 (2026-11-18 사용자가 고른 연필 스케치 느낌)
    #  join = 이어진 선 덩어리 하나를 한 번에 그음
    src = sys.argv[1] if len(sys.argv) > 1 else 'key_ink.png'
    out_name = sys.argv[2] if len(sys.argv) > 2 else 'reveal_strokes.png'
    thr = float(sys.argv[3]) if len(sys.argv) > 3 else 0.32
    cut = (sys.argv[4] if len(sys.argv) > 4 else 'join') == 'cut'
    ink = line_alpha(src)
    H, W = ink.shape
    key = 1 - ink
    on = ink > thr
    on = ndimage.binary_opening(on, iterations=1) | (ink > min(0.95, thr * 2))
    print('ink px', on.sum(), 'of', on.size)
    sk = thin(on)
    if len(sys.argv) > 4 and sys.argv[4] == 'trace':
        pens = int(sys.argv[5]) if len(sys.argv) > 5 else 8
        speed = float(sys.argv[6]) if len(sys.argv) > 6 else 1800.0
        T, tot = trace_order(sk, W, H, FACE[0] * W, FACE[1] * H, pens=pens, speed=speed)
        T = T / tot
        known = np.isfinite(T)
        _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
        full = T[iy, ix]
        out = np.where(ink > 0.05, full, 1.0)
        Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'L').save(os.path.join(D, out_name))
        print(out_name)
        sys.exit(0)
    # 이어진 선 덩어리 하나 = 한 번에 긋는 단위 (갈림길에서 자르면 짧은 조각이 흩뿌려져 '긋는' 느낌이 안 남)
    if cut:
        nbc = neighbors(sk)
        sk = sk & ~ndimage.binary_dilation(sk & (nbc >= 3), iterations=1)
    lab, n = ndimage.label(sk, structure=np.ones((3, 3)))
    print('strokes', n)
    idx = ndimage.find_objects(lab)
    fx, fy = FACE[0] * W, FACE[1] * H
    strokes = []
    for k in range(1, n + 1):
        sl = idx[k - 1]
        sub = lab[sl] == k
        ys, xs = np.nonzero(sub)
        if len(xs) < 4:
            continue
        ys = ys + sl[0].start; xs = xs + sl[1].start
        pts = set(zip(ys.tolist(), xs.tolist()))
        def deg(p):
            y, x = p
            return sum((y + dy, x + dx) in pts for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)
        ends = [p for p in pts if deg(p) <= 1] or list(pts)
        start = min(ends, key=lambda p: np.hypot(p[1] - fx, p[0] - fy))   # 얼굴 쪽 끝에서 시작
        dist = {start: 0}
        q = [start]
        while q:
            nq = []
            for (y, x) in q:
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        p = (y + dy, x + dx)
                        if p in pts and p not in dist:
                            dist[p] = dist[(y, x)] + 1
                            nq.append(p)
            q = nq
        reach = max(dist.values()) + 1
        end = max(dist, key=dist.get)
        strokes.append(dict(pix=dist, L=len(pts), reach=reach, start=start, end=end, cx=xs.mean(), cy=ys.mean(), strength=1.0,
                            dface=np.hypot((start[1] - fx) / W * 1.6, (start[0] - fy) / H) * 0.7 + np.hypot((xs.mean() - fx) / W * 1.6, (ys.mean() - fy) / H) * 0.3))
    print('kept', len(strokes))
    # 외곽선 = 길고 진한 획, 나머지 = 해칭 · 결
    if cut:   # 길이 순 위 18% = 1부, 나머지 = 2부 (첫 버전)
        s_sorted = sorted(strokes, key=lambda s: -s['L'])
        n_out = max(1, int(len(strokes) * 0.18))
        outline, hatch = s_sorted[:n_out], s_sorted[n_out:]
    else:
        outline = [st for st in strokes if st['L'] >= 70]       # 1부: 큰 윤곽선 덩어리
        hatch = [st for st in strokes if st['L'] < 70]          # 2부: 작은 결 · 디테일
    def schedule(group, pens, speed, gap, window):
        """펜 여러 자루가 얼굴에서 가까운 순서(window개 후보) 중 자기 손에서 가까운 획을 골라 그음. 반환: 끝난 시각"""
        group = sorted(group, key=lambda s: s['dface'])
        done = [False] * len(group)
        pen = [(0.0, (fy, fx)) for _ in range(pens)]
        heapq.heapify(pen)
        ptr = 0; left = len(group); tmax = 0
        while left:
            t, pos = heapq.heappop(pen)
            while ptr < len(group) and done[ptr]:
                ptr += 1
            cands = [i for i in range(ptr, min(len(group), ptr + window)) if not done[i]]
            i = min(cands, key=lambda i: np.hypot(group[i]['cx'] - pos[1], group[i]['cy'] - pos[0]))
            s = group[i]; done[i] = True; left -= 1
            dur = (s['L'] if cut else min(s['L'], 900)) / speed                   # 아주 큰 선 덩어리도 너무 오래 끌지 않게
            s['t0'] = t; s['dur'] = dur
            tmax = max(tmax, t + dur)
            heapq.heappush(pen, (t + dur + gap, (s['end'][0], s['end'][1])))
        return tmax

    if cut:
        t_out = schedule(outline, pens=4, speed=900.0, gap=0.035, window=10)
        t_hat = schedule(hatch, pens=16, speed=1400.0, gap=0.01, window=60)
    else:
        t_out = schedule(outline, pens=3, speed=1100.0, gap=0.04, window=6)
        t_hat = schedule(hatch, pens=12, speed=1600.0, gap=0.01, window=40)
    print('outline time', round(t_out, 2), 'hatch time', round(t_hat, 2))
    # 1부 0 ~ 0.58, 2부 0.45 ~ 1 (조금 겹침)
    T = np.full((H, W), np.inf, np.float32)
    for grp, a, b, tot in ((outline, 0.0, 0.58, t_out), (hatch, 0.45, 1.0, t_hat)) if cut else ((outline, 0.0, 0.72, t_out), (hatch, 0.55, 1.0, t_hat)):
        for s in grp:
            for (y, x), d in s['pix'].items():
                tt = s['t0'] + s['dur'] * (d / max(1, s['reach'] - 1))
                T[y, x] = a + (b - a) * tt / tot
    known = np.isfinite(T)
    _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
    full = T[iy, ix]                                        # 선 굵기 전체에 가장 가까운 뼈대 시각
    out = np.where(ink > 0.05, full, 1.0)
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'L').save(os.path.join(D, out_name))
    print(out_name)
