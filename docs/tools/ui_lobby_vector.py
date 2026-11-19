# 로비 '선이 하나하나 그어지는' 연출용 획 경로 (2026-11-18 사용자: 마스크로 드러내지 말고 선 하나하나가 그려지는 느낌)
#  선 그림(Resources/UI/Lobby/stage2.png 연필 · stage3.png 잉크 선화)을 1픽셀 뼈대로 깎고 → 갈림길 사이 선을 경로(점 목록)로 → 점을 줄이고(RDP)
#  → 굵기(선 두께) 재고 → 펜 여러 자루 순서(얼굴부터, 이어진 선을 따라) → 텍스트로 저장. 게임(LobbyStrokes.cs)이 이 경로를 따라 실제로 선을 그음
#  출력 형식 (한 줄 = 한 획): t0 dur w x0 y0 x1 y1 ...   (t0 · dur = 0~1, 좌표 = key.png 1920x1080 픽셀)
# 실행: python docs/tools/ui_lobby_vector.py stage2.png strokes_pencil.txt 0.22 8 1800
#       python docs/tools/ui_lobby_vector.py stage3.png strokes_ink.txt 0.3 6 1500
import os, sys, heapq
import numpy as np
from scipy import ndimage

sys.path.insert(0, os.path.dirname(__file__))
from ui_lobby_strokes import thin, neighbors, line_alpha, D  # noqa: E402
from ui_lobby_ink import FACE  # noqa: E402


def walk(pset, start):
    """한 획(갈림길 없는 선)의 픽셀을 끝에서 끝으로 따라감 (4이웃 먼저)"""
    path = [start]; seen = {start}; cur = start
    while True:
        y, x = cur; nxt = None
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)):
            p = (y + dy, x + dx)
            if p in pset and p not in seen:
                nxt = p; break
        if nxt is None:
            return path
        path.append(nxt); seen.add(nxt); cur = nxt


def rdp(pts, eps):
    if len(pts) < 3:
        return pts
    a, b = np.array(pts[0], float), np.array(pts[-1], float)
    ab = b - a; n = np.hypot(*ab)
    P = np.array(pts, float)
    q = P - a
    d = np.abs(ab[0] * q[:, 1] - ab[1] * q[:, 0]) / n if n > 0 else np.hypot(q[:, 0], q[:, 1])
    i = int(np.argmax(d))
    if d[i] > eps:
        return rdp(pts[:i + 1], eps)[:-1] + rdp(pts[i:], eps)
    return [pts[0], pts[-1]]


if __name__ == '__main__':
    src, out = sys.argv[1], sys.argv[2]
    MINLEN = int(sys.argv[6]) if len(sys.argv) > 6 else 12           # 이보다 짧은 부스러기 선은 빼고 색 단계에서 채움
    thr = float(sys.argv[3]); pens = int(sys.argv[4]); speed = float(sys.argv[5])
    ink = line_alpha(src)
    H, W = ink.shape
    on = ink > thr
    on = ndimage.binary_opening(on, iterations=1) | (ink > min(0.95, thr * 2))
    thick = ndimage.distance_transform_edt(on)
    sk = thin(on)
    nb = neighbors(sk)
    jun = sk & (nb >= 3)
    junb = ndimage.binary_dilation(jun, iterations=1) & sk
    seg = sk & ~junb
    lab, n = ndimage.label(seg, structure=np.ones((3, 3)))
    jl, _ = ndimage.label(ndimage.binary_dilation(junb, iterations=1), structure=np.ones((3, 3)))
    jys, jxs = np.nonzero(junb)
    jpts = set(zip(jys.tolist(), jxs.tolist()))
    idx = ndimage.find_objects(lab)
    fx, fy = FACE[0] * W, FACE[1] * H
    segs = []
    for k in range(1, n + 1):
        sl = idx[k - 1]
        ys, xs = np.nonzero(lab[sl] == k)
        if len(xs) < MINLEN:
            continue
        ys = ys + sl[0].start; xs = xs + sl[1].start
        pset = set(zip(ys.tolist(), xs.tolist()))
        def deg(p):
            y, x = p
            return sum((y + dy, x + dx) in pset for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)
        ends = [p for p in pset if deg(p) <= 1] or [next(iter(pset))]
        path = walk(pset, ends[0])
        # 갈림길 쪽 끝을 갈림길 픽셀까지 이어 붙임 (틈 없이)
        for side in (0, -1):
            y, x = path[side]
            for dy in (-2, -1, 0, 1, 2):
                for dx in (-2, -1, 0, 1, 2):
                    if (y + dy, x + dx) in jpts:
                        if side == 0: path.insert(0, (y + dy, x + dx))
                        else: path.append((y + dy, x + dx))
                        break
                else:
                    continue
                break
        w = float(np.clip(np.median([thick[p] for p in path]) * 2.0, 1.2, 7.0))
        js = set(jl[ys, xs].tolist()) - {0}
        pts = rdp([(p[1], p[0]) for p in path], 0.9)
        L = len(path)
        segs.append(dict(pts=pts, L=L, w=w, js=js, cx=xs.mean(), cy=ys.mean(),
                         d=np.hypot((xs.mean() - fx) / W * 1.6, (ys.mean() - fy) / H)))
    # 이웃 (같은 갈림길에 닿은 선)
    byj = {}
    for i, s in enumerate(segs):
        for j in s['js']:
            byj.setdefault(j, []).append(i)
    adj = [set() for _ in segs]
    for lst in byj.values():
        for a in lst:
            adj[a].update(b for b in lst if b != a)
    order = sorted(range(len(segs)), key=lambda i: segs[i]['d'])
    done = [False] * len(segs)
    rng = np.random.default_rng(1159)
    pen = [(i * 0.25, -i - 1, np.array((fx, fy), float), -1) for i in range(pens)]
    cnt = 0
    heapq.heapify(pen)
    ptr = 0; left = len(segs); tmax = 0.0
    while left:
        t, _, pos, last = heapq.heappop(pen)
        nxt = None
        lift = 0.03                                             # 붓을 떼고 다른 곳으로 옮기는 시간
        if last >= 0:
            c = [j for j in adj[last] if not done[j]]
            if c:
                # 갈림길에서 가장 자연스럽게 이어지는 방향의 선 (붓을 떼지 않고 계속)
                lp = segs[last]['pts']
                d0 = np.array(lp[-1], float) - np.array(lp[max(0, len(lp) - 3)], float)
                d0 /= (np.hypot(*d0) + 1e-6)
                def turn(j):
                    q = segs[j]['pts']
                    a_, b_ = np.array(q[0], float), np.array(q[-1], float)
                    near, far = (a_, b_) if np.hypot(*(a_ - pos)) <= np.hypot(*(b_ - pos)) else (b_, a_)
                    v = far - near; v /= (np.hypot(*v) + 1e-6)
                    return -float(d0 @ v) + np.hypot(*(near - pos)) * 0.05
                nxt = min(c, key=turn)
                lift = 0.0
        if nxt is None:
            # 펜을 떼고 그림 어딘가 다른 자리로 (2026-11-18 사용자: 원으로 퍼지지 말고 여기저기서 그려지며 합쳐지게)
            # 남은 선 중 무작위 후보 40개 → 긴 선 · (초반엔) 고양이 근처를 조금 더 좋아함
            undone = [j for j in range(len(segs)) if not done[j]]
            cand = [undone[q] for q in rng.choice(len(undone), size=min(40, len(undone)), replace=False)]
            early = max(0.0, 1.0 - (len(segs) - left) / max(1, len(segs) * 0.35))
            nxt = max(cand, key=lambda j: rng.random() + 0.25 * np.log1p(segs[j]['L']) / 5 + early * 0.6 * (1 - min(1, segs[j]['d'] / 0.5)))
        done[nxt] = True; left -= 1
        s = segs[nxt]
        p0, p1 = s['pts'][0], s['pts'][-1]
        if (p1[0] - pos[0]) ** 2 + (p1[1] - pos[1]) ** 2 < (p0[0] - pos[0]) ** 2 + (p0[1] - pos[1]) ** 2:
            s['pts'] = s['pts'][::-1]                       # 손에 가까운 끝에서 시작
        dur = s['L'] / speed
        t += lift
        s['t0'] = t; s['dur'] = dur
        tmax = max(tmax, t + dur)
        cnt += 1
        heapq.heappush(pen, (t + dur + 0.004, cnt, np.array(s['pts'][-1], float), nxt))
    segs.sort(key=lambda s: s['t0'])
    with open(os.path.join(D, out), 'w', encoding='utf-8') as f:
        for s in segs:
            f.write(f"{s['t0'] / tmax:.5f} {max(1e-4, s['dur'] / tmax):.5f} {s['w']:.2f} " + ' '.join(f'{x:.1f} {y:.1f}' for x, y in s['pts']) + '\n')
    print(out, 'strokes', len(segs), 'points', sum(len(s['pts']) for s in segs), 'time', round(tmax, 2))
