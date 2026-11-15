# 벡터 이펙트: 매 프레임 도형(원 · 다각형 · 선)을 다시 그려 부드럽게 움직이게 한다. (이미지 프레임 대신)
# 플랫 스타일: 단색 면 + 검은 외곽선. 3배 크기로 그린 뒤 줄여서 계단 현상 제거.
# 엔진에서는 같은 규칙을 메시/라인/셰이더로 그리면 된다 (t = 0~1 진행도 하나로 모든 값이 정해짐).
import math, random
from PIL import Image, ImageDraw

INK = (18, 12, 26)
WHITE, YEL, ORG, RED = (255, 248, 222), (255, 214, 74), (247, 140, 40), (214, 58, 44)
GOLD, GOLD_HI = (240, 178, 60), (255, 230, 140)
SMOKE = (122, 104, 140)
SS = 3                                              # 슈퍼샘플 배율


def ease_out(t):
    return 1 - (1 - t) ** 3


def ease_back(t, s=1.7):
    t -= 1
    return t * t * ((s + 1) * t + s) + 1


def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


def star_pts(cx, cy, ro, ri, n, rot, jit=0.0, seed=0):
    rnd = random.Random(seed)
    pts = []
    for i in range(n * 2):
        r = (ro if i % 2 == 0 else ri) * (1 + rnd.uniform(-jit, jit))
        a = rot + math.pi * i / n
        pts.append((cx + r * math.sin(a), cy - r * math.cos(a)))
    return pts


class Layer:
    """투명 레이어 하나에 그리고 불투명도를 곱해 합성."""
    def __init__(self, size):
        self.im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def faded(self, a):
        if a >= 1:
            return self.im
        r, g, b, al = self.im.split()
        al = al.point(lambda v: int(v * clamp(a)))
        return Image.merge("RGBA", (r, g, b, al))


def _finish(layers, size):
    S = size * SS
    out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    for im in layers:
        out.alpha_composite(im)
    return out.resize((size, size), Image.LANCZOS)


def shot(t, size):
    """총 쏠 때 폭발. t: 0(섬광 시작) → 1(연기 사라짐)."""
    S = size * SS
    c = S / 2
    ink = int(S * 0.018)
    layers = []
    # 4) 연기 (위로 흩어지며 사라짐)
    if t > 0.45:
        tt = (t - 0.45) / 0.55
        L = Layer(S)
        rnd = random.Random(9)
        for i in range(6):
            a = i * 2 * math.pi / 6 + rnd.uniform(-0.4, 0.4)
            rr = S * (0.08 + 0.2 * ease_out(tt))
            x, y = c + rr * math.sin(a), c - rr * math.cos(a) - S * 0.12 * tt
            pr = S * 0.075 * (0.7 + 0.6 * tt) * rnd.uniform(0.8, 1.2)
            L.d.ellipse((x - pr - ink, y - pr - ink, x + pr + ink, y + pr + ink), fill=INK)
            L.d.ellipse((x - pr, y - pr, x + pr, y + pr), fill=SMOKE)
        layers.append(L.faded(1 - tt))
    # 1) 충격파 고리
    if t < 0.7:
        tt = t / 0.7
        L = Layer(S)
        r = S * 0.46 * ease_out(tt)
        w = max(2, S * 0.05 * (1 - tt))
        L.d.ellipse((c - r - ink, c - r - ink, c + r + ink, c + r + ink), outline=INK, width=int(w + ink * 2))
        L.d.ellipse((c - r, c - r, c + r, c + r), outline=YEL, width=int(w))
        layers.append(L.faded(1 - tt))
    # 2) 불꽃 별 (튀어나왔다 줄어듦)
    if t < 0.75:
        grow = ease_back(clamp(t / 0.3))
        shrink = 1 - 0.75 * clamp((t - 0.4) / 0.35)
        R = S * 0.36 * grow * shrink
        if R > 2:
            L = Layer(S)
            rot = 0.15 + t * 0.8
            L.d.polygon(star_pts(c, c, R + ink * 1.6, R * 0.5 + ink * 1.6, 10, rot, 0.15, 3), fill=INK)
            for k, (sc, col) in enumerate(((1.0, RED), (0.78, ORG), (0.55, YEL), (0.28, WHITE))):
                L.d.polygon(star_pts(c, c, R * sc, R * sc * 0.5, 10, rot + k * 0.06, 0.15, 3 + k), fill=col)
            layers.append(L.faded(1 - clamp((t - 0.55) / 0.2)))
    # 3) 불똥 선
    if 0.05 < t < 0.85:
        tt = (t - 0.05) / 0.8
        L = Layer(S)
        rnd = random.Random(5)
        for i in range(9):
            a = i * 2 * math.pi / 9 + rnd.uniform(-0.2, 0.2)
            r0 = S * (0.18 + 0.3 * ease_out(tt))
            r1 = r0 + S * 0.16 * (1 - tt)
            p0 = (c + r0 * math.sin(a), c - r0 * math.cos(a))
            p1 = (c + r1 * math.sin(a), c - r1 * math.cos(a))
            w = max(2, S * 0.025 * (1 - tt))
            L.d.line([p0, p1], fill=INK, width=int(w + ink * 1.6))
            L.d.line([p0, p1], fill=YEL, width=int(w))
        layers.append(L.faded(1 - clamp((tt - 0.7) / 0.3)))
    return _finish(layers, size)


def load_pop(t, size):
    """탄피가 약실에 들어갈 때: 금색 고리 + 반짝이 4개."""
    S = size * SS
    c = S / 2
    ink = int(S * 0.025)
    L = Layer(S)
    r = S * (0.22 + 0.24 * ease_out(t))
    w = max(2, S * 0.06 * (1 - t))
    L.d.ellipse((c - r - ink, c - r - ink, c + r + ink, c + r + ink), outline=INK, width=int(w + ink * 2))
    L.d.ellipse((c - r, c - r, c + r, c + r), outline=GOLD_HI, width=int(w))
    sc = math.sin(math.pi * clamp(t * 1.2))
    for i in range(4):
        a = math.pi / 4 + i * math.pi / 2
        rr = S * (0.3 + 0.12 * t)
        x, y = c + rr * math.sin(a), c - rr * math.cos(a)
        ro = S * 0.11 * sc
        if ro > 1:
            L.d.polygon(star_pts(x, y, ro + ink, ro * 0.3 + ink, 4, 0), fill=INK)
            L.d.polygon(star_pts(x, y, ro, ro * 0.3, 4, 0), fill=GOLD_HI)
    return _finish([L.faded(1 - clamp((t - 0.6) / 0.4))], size)


def spin_arcs(angle_deg, strength, size, ring=0.42):
    """실린더가 돌 때 둘레의 금색 움직임 선. strength 0~1 (회전 속도)."""
    S = size * SS
    c = S / 2
    ink = int(S * 0.012)
    L = Layer(S)
    if strength <= 0.02:
        return _finish([], size)
    for k in range(3):
        start = angle_deg + k * 120 - 90
        ext = 30 + 45 * strength
        for j, (rr, w) in enumerate(((ring, 0.022), (ring + 0.045, 0.014))):
            r = S * rr
            box = (c - r, c - r, c + r, c + r)
            L.d.arc(box, start - ext, start, fill=INK, width=int(S * w + ink * 2))
            L.d.arc(box, start - ext, start, fill=GOLD_HI if j == 0 else GOLD, width=int(S * w))
    return _finish([L.faded(strength)], size)


def ring_rays(t, size, ring=0.44):
    """장전 끝: 시계 둘레로 짧은 금색 빛줄기가 퍼짐."""
    S = size * SS
    c = S / 2
    ink = int(S * 0.008)
    L = Layer(S)
    for i in range(12):
        a = i * math.pi / 6
        r0 = S * (ring + 0.03 * ease_out(t))
        r1 = r0 + S * 0.07 * math.sin(math.pi * clamp(t))
        if r1 - r0 < 1:
            continue
        p0 = (c + r0 * math.sin(a), c - r0 * math.cos(a))
        p1 = (c + r1 * math.sin(a), c - r1 * math.cos(a))
        L.d.line([p0, p1], fill=INK, width=int(S * 0.016 + ink * 2))
        L.d.line([p0, p1], fill=GOLD_HI, width=int(S * 0.016))
    return _finish([L.faded(1 - clamp((t - 0.5) / 0.5))], size)


if __name__ == "__main__":
    # 미리보기: 사격 8단계, 장전 팝 6단계
    sz = 200
    sheet = Image.new("RGBA", (sz * 8, sz * 2), (27, 23, 38, 255))
    for i in range(8):
        sheet.alpha_composite(shot(i / 7, sz), (i * sz, 0))
    for i in range(6):
        sheet.alpha_composite(load_pop(i / 5, sz), (i * sz, sz))
    sheet.alpha_composite(spin_arcs(30, 1.0, sz), (6 * sz, sz))
    sheet.alpha_composite(ring_rays(0.4, sz), (7 * sz, sz))
    sheet.convert("RGB").save(r"C:\Users\asdzx\AppData\Local\Temp\claude\d4134924d10d6a35\c9ebd708-145d-4dae-aafc-cb0e7b6a1c44\scratchpad\fxv_prev.png")
    print("ok")
