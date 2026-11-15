# 벡터 + 셰이더 방식 VFX
# 도형을 거리장(벡터)으로 정의하고, 픽셀마다 셰이더처럼 색을 정한다.
#   - 색: 거리장 값 → 계단 램프 (흰 → 노랑 → 주황 → 빨강) + 가장자리 검은 잉크 띠
#   - 사라짐: 투명도 대신 노이즈 디졸브 (구멍이 뚫리며 타 들어감, 경계는 밝게)
#   - 글로우: 밝은 부분 뒤에 부드러운 빛 번짐
#   - 연기: 2단 음영 (왼쪽 위 빛)
# 엔진(Unity)에서는 같은 식을 프래그먼트 셰이더로 옮기면 됨. 입력은 진행도 t(0~1) 하나.
import math
import numpy as np
from PIL import Image
from scipy import ndimage as nd

INK = np.array([18, 12, 26]) / 255
WHITE = np.array([255, 248, 222]) / 255
YEL = np.array([255, 214, 74]) / 255
ORG = np.array([247, 140, 40]) / 255
RED = np.array([214, 58, 44]) / 255
GOLD = np.array([240, 178, 60]) / 255
GOLD_HI = np.array([255, 232, 150]) / 255
SMOKE_L = np.array([140, 122, 160]) / 255
SMOKE_D = np.array([96, 80, 116]) / 255
SS = 2
_noise_cache = {}


def clamp(x, a=0.0, b=1.0):
    return np.clip(x, a, b)


def smoothstep(a, b, x):
    t = clamp((x - a) / (b - a))
    return t * t * (3 - 2 * t)


def ease_out(t):
    return 1 - (1 - t) ** 3


def ease_back(t, s=1.7):
    t -= 1
    return t * t * ((s + 1) * t + s) + 1


def fbm(n, seed, octaves=4):
    key = (n, seed, octaves)
    if key in _noise_cache:
        return _noise_cache[key]
    rnd = np.random.default_rng(seed)
    out = np.zeros((n, n))
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        g = 4 * 2 ** o
        grid = rnd.random((g + 1, g + 1))
        up = nd.zoom(grid, n / g, order=3)[:n, :n]
        out += up * amp
        tot += amp
        amp *= 0.5
    out = (out / tot - out.min() / tot)
    out = out / out.max()
    _noise_cache[key] = out
    return out


def coords(n):
    v, u = np.mgrid[0:n, 0:n]
    u = (u + 0.5) / n * 2 - 1
    v = (v + 0.5) / n * 2 - 1
    return u, v, np.hypot(u, v), np.arctan2(u, -v)        # 각도 0 = 위, 시계 방향 +


def ramp(I, stops):
    """I(0~1)에 따라 계단 색. stops = [(문턱, 색)] 높은 것부터."""
    col = np.zeros(I.shape + (3,))
    done = np.zeros(I.shape, bool)
    for th, c in stops:
        sel = (I > th) & ~done
        col[sel] = c
        done |= sel
    return col


def compose(n, layers):
    """layers: [(rgb, alpha)] 아래부터. 결과 RGBA 이미지 (SS배 → 줄임)."""
    rgb = np.zeros((n, n, 3))
    a = np.zeros((n, n))
    for c, al in layers:
        al = clamp(al)
        rgb = c * al[..., None] + rgb * (1 - al[..., None])
        a = al + a * (1 - al)
    out = np.dstack([clamp(rgb), clamp(a)])
    im = Image.fromarray((out * 255).astype(np.uint8), "RGBA")
    return im.resize((n // SS, n // SS), Image.LANCZOS)


def glow(mask, n, strength, color):
    g = nd.gaussian_filter(mask.astype(float), n * 0.04) * strength
    return np.broadcast_to(color, (n, n, 3)), clamp(g)


def shot(t, size, seed=3):
    """총 쏠 때 폭발."""
    n = size * SS
    u, v, r, th = coords(n)
    N1, N2 = fbm(n, seed), fbm(n, seed + 7)
    layers = []
    # 연기 (맨 아래): 덩어리 거리장 + 디졸브 + 2단 음영
    if t > 0.38:
        tt = (t - 0.38) / 0.62
        f = np.full((n, n), -1.0)
        rng = np.random.default_rng(seed + 11)
        for i in range(6):
            a = i * 2 * math.pi / 6 + rng.uniform(-0.4, 0.4)
            rr = 0.12 + 0.32 * ease_out(tt)
            cx, cy = rr * math.sin(a), -rr * math.cos(a) - 0.25 * tt
            pr = (0.16 + 0.12 * tt) * rng.uniform(0.85, 1.15)
            f = np.maximum(f, 1 - np.hypot(u - cx, v - cy) / pr)
        f = f + (N2 - 0.5) * 0.35 - smoothstep(0.3, 1.0, tt) * 1.1      # 디졸브
        inside = f > 0
        edge = inside & (f < 0.09)
        lit = (nd.shift(f, (n * 0.03, n * 0.03), order=1, cval=-1) < f) | (f > 0.35)
        col = np.where(lit[..., None], SMOKE_L, SMOKE_D)
        col[edge] = INK
        layers.append((col, inside.astype(float)))
    # 글로우
    grow = ease_back(min(t / 0.28, 1.0))
    R = 0.62 * grow * (1 - 0.55 * smoothstep(0.35, 0.8, t))
    if t < 0.8:
        spikes = 1 + 0.28 * np.cos(10 * th + 1.3 + t * 2.5) + 0.22 * (N1 - 0.5)
        field = 1 - r / (R * spikes + 1e-4)
        burn = smoothstep(0.3, 0.8, t) * 1.05                            # 타 들어가는 정도
        I = field + (N1 - 0.5) * 0.35 * smoothstep(0.2, 0.6, t) - burn
        body = I > 0
        layers.append(glow(body, n, 0.55 * (1 - smoothstep(0.4, 0.8, t)), YEL))
        col = ramp(I / max(1e-4, 1 - burn * 0.6), [(0.62, WHITE), (0.42, YEL), (0.24, ORG), (0.07, RED), (0.0, INK)])
        layers.append((col, body.astype(float)))
    # 불똥 줄기
    if 0.06 < t < 0.85:
        tt = (t - 0.06) / 0.79
        r0 = 0.3 + 0.55 * ease_out(tt)
        r1 = r0 + 0.28 * (1 - tt)
        ang = (th * 9 / (2 * math.pi) + 0.37) % 1.0
        wid = 0.06 * (1 - tt) + 0.015
        streak = (np.abs(ang - 0.5) < wid) & (r > r0) & (r < r1)
        core = (np.abs(ang - 0.5) < wid * 0.45) & (r > r0) & (r < r1)
        col = np.where(core[..., None], YEL, INK)
        layers.append((col, streak.astype(float)))
    return compose(n, layers)


def load_pop(t, size, seed=21):
    """탄피 장전: 금빛 고리가 퍼지며 디졸브 + 반짝이."""
    n = size * SS
    u, v, r, th = coords(n)
    N = fbm(n, seed)
    layers = []
    Rr = 0.35 + 0.5 * ease_out(t)
    w = 0.13 * (1 - t) + 0.02
    f = 1 - np.abs(r - Rr) / w + (N - 0.5) * 0.9 * t - smoothstep(0.5, 1.0, t) * 0.8
    ring = f > 0
    layers.append(glow(ring, n, 0.5 * (1 - t), GOLD_HI))
    col = np.where((f < 0.22)[..., None], INK, np.where((f > 0.6)[..., None], WHITE, GOLD_HI))
    layers.append((col, ring.astype(float)))
    sc = math.sin(math.pi * min(t * 1.3, 1.0))
    if sc > 0.02:
        star = np.zeros((n, n), bool)
        edge = np.zeros((n, n), bool)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            cx, cy = (0.55 + 0.2 * t) * math.sin(a), -(0.55 + 0.2 * t) * math.cos(a)
            du, dv = np.abs(u - cx), np.abs(v - cy)
            s = np.sqrt(du) + np.sqrt(dv)                            # 4갈래 별 모양 거리장
            star |= s < 0.42 * sc
            edge |= s < 0.42 * sc + 0.08
        layers.append((np.broadcast_to(INK, (n, n, 3)), edge.astype(float)))
        layers.append((np.broadcast_to(WHITE, (n, n, 3)), star.astype(float)))
    return compose(n, layers)


def spin_arcs(angle_deg, strength, size, ring=0.82):
    """실린더 회전: 둘레를 도는 금빛 꼬리 (머리는 희고 꼬리로 갈수록 가늘고 투명)."""
    n = size * SS
    if strength <= 0.02:
        return Image.new("RGBA", (size, size), (0, 0, 0, 0))
    u, v, r, th = coords(n)
    layers = []
    body = np.zeros((n, n))
    head = np.zeros((n, n), bool)
    outline = np.zeros((n, n), bool)
    tail_len = math.radians(40 + 70 * strength)
    for k in range(3):
        hd = math.radians(angle_deg) + k * 2 * math.pi / 3
        d = (hd - th) % (2 * math.pi)                                 # 머리 뒤쪽으로 잰 각
        s = clamp(1 - d / tail_len) * (d < tail_len)
        w = 0.012 + 0.05 * s
        band = np.abs(r - ring) < w
        body = np.maximum(body, band * s)
        head |= band & (s > 0.8)
        outline |= (np.abs(r - ring) < w + 0.02) & (s > 0.05)
    layers.append(glow(body > 0.3, n, 0.45 * strength, GOLD_HI))
    layers.append((np.broadcast_to(INK, (n, n, 3)), outline * strength))
    col = np.where(head[..., None], WHITE, np.where((body > 0.5)[..., None], GOLD_HI, GOLD))
    layers.append((col, (body > 0.02) * strength))
    return compose(n, layers)


def ring_rays(t, size, ring=0.8):
    """장전 끝: 시계 둘레로 금빛이 터지며 디졸브."""
    n = size * SS
    u, v, r, th = coords(n)
    N = fbm(n, 33)
    layers = []
    ang = (th * 12 / (2 * math.pi)) % 1.0
    ln = 0.16 * math.sin(math.pi * min(t, 1.0))
    r0 = ring + 0.04 * ease_out(t)
    wid = 0.09 * (1 - t) + 0.03
    f = (wid - np.abs(ang - 0.5)) / wid
    inray = (r > r0) & (r < r0 + ln) & (f > 0)
    f = np.where(inray, f + (N - 0.5) * 0.6 * t - smoothstep(0.55, 1.0, t), -1.0)
    ray = f > 0
    layers.append(glow(ray, n, 0.5 * (1 - t), GOLD_HI))
    col = np.where((f < 0.25)[..., None], INK, np.where((f > 0.7)[..., None], WHITE, GOLD_HI))
    layers.append((col, ray.astype(float)))
    # 안쪽 번쩍임 고리
    band = 1 - np.abs(r - r0 * 0.98) / (0.03 * (1 - t) + 0.005)
    fl = np.where(band > 0, band - smoothstep(0.3, 0.8, t) * 1.2 + (N - 0.5) * 0.5, -1.0)
    layers.append((np.broadcast_to(WHITE, (n, n, 3)), (fl > 0) * 0.9))
    return compose(n, layers)


if __name__ == "__main__":
    sz = 200
    sheet = Image.new("RGBA", (sz * 8, sz * 2), (27, 23, 38, 255))
    for i in range(8):
        sheet.alpha_composite(shot((i + 1) / 8, sz), (i * sz, 0))
    for i in range(5):
        sheet.alpha_composite(load_pop((i + 1) / 6, sz), (i * sz, sz))
    sheet.alpha_composite(spin_arcs(30, 1.0, sz), (5 * sz, sz))
    sheet.alpha_composite(ring_rays(0.25, sz), (6 * sz, sz))
    sheet.alpha_composite(ring_rays(0.6, sz), (7 * sz, sz))
    sheet.convert("RGB").save(r"C:\Users\asdzx\AppData\Local\Temp\claude\d4134924d10d6a35\c9ebd708-145d-4dae-aafc-cb0e7b6a1c44\scratchpad\fxs_prev.png")
    print("ok")
