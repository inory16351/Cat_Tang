# 플랫 폭발 이펙트 4프레임 (작은 섬광 → 커지는 폭발 → 가장 큰 폭발 → 흩어지는 연기 · 불똥)
# 단색 면 + 굵은 검은 외곽선. 결과: docs/images/ui/sheet_fx_explosion_flat.png (2x2, 배경 #1B1726)
import math, random
from PIL import Image, ImageDraw

BG = (27, 23, 38)
INK = (18, 12, 26)
Y, O, R, W = (255, 214, 74), (247, 140, 40), (214, 58, 44), (255, 246, 214)
SMOKE, SMOKE2 = (122, 104, 140), (88, 74, 104)
S = 800                                      # 칸 크기 (2x2 → 1600x1600, 나중에 줄임)


def star(cx, cy, r_out, r_in, n, rot=0.0, jitter=0.0, seed=0):
    rnd = random.Random(seed)
    pts = []
    for i in range(n * 2):
        r = (r_out if i % 2 == 0 else r_in) * (1 + rnd.uniform(-jitter, jitter))
        a = rot + math.pi * i / n
        pts.append((cx + r * math.sin(a), cy - r * math.cos(a)))
    return pts


def draw_star(d, cx, cy, r, n, seed, layers):
    # layers: [(크기 비율, 안쪽 비율, 색)] 바깥부터
    out = star(cx, cy, r * 1.0 + 14, r * layers[0][1] + 14, n, 0.1, 0.18, seed)
    d.polygon(out, fill=INK)                 # 외곽선 = 한 겹 크게 그린 검정
    for k, (sc, inner, col) in enumerate(layers):
        d.polygon(star(cx, cy, r * sc, r * sc * inner, n, 0.1 + k * 0.05, 0.18, seed + k), fill=col)


def puff(d, cx, cy, r, col):
    d.ellipse((cx - r - 12, cy - r - 12, cx + r + 12, cy + r + 12), fill=INK)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=col)


def frame(i):
    im = Image.new("RGB", (S, S), BG)
    d = ImageDraw.Draw(im)
    c = S / 2
    if i == 0:
        draw_star(d, c, c, 120, 6, 3, [(1.0, 0.35, Y), (0.5, 0.4, W)])
    elif i == 1:
        draw_star(d, c, c, 230, 8, 5, [(1.0, 0.45, O), (0.7, 0.45, Y), (0.32, 0.5, W)])
    elif i == 2:
        draw_star(d, c, c, 340, 11, 7, [(1.0, 0.5, R), (0.78, 0.5, O), (0.52, 0.5, Y), (0.22, 0.6, W)])
    else:
        rnd = random.Random(11)
        for _ in range(9):                    # 연기 덩어리
            a, rr = rnd.uniform(0, 2 * math.pi), rnd.uniform(40, 190)
            puff(d, c + rr * math.cos(a), c + rr * math.sin(a), rnd.uniform(70, 120), SMOKE)
        for _ in range(5):
            a, rr = rnd.uniform(0, 2 * math.pi), rnd.uniform(20, 140)
            r0 = rnd.uniform(40, 70)
            d.ellipse((c + rr * math.cos(a) - r0, c + rr * math.sin(a) - r0, c + rr * math.cos(a) + r0, c + rr * math.sin(a) + r0), fill=SMOKE2)
        for _ in range(7):                    # 불똥
            a, rr = rnd.uniform(0, 2 * math.pi), rnd.uniform(260, 340)
            x, y = c + rr * math.cos(a), c + rr * math.sin(a)
            d.polygon(star(x, y, 46, 16, 4, a, 0, 1), fill=INK)
            d.polygon(star(x, y, 30, 10, 4, a, 0, 1), fill=Y)
    return im


if __name__ == "__main__":
    sheet = Image.new("RGB", (S * 2, S * 2), BG)
    for i in range(4):
        sheet.paste(frame(i), ((i % 2) * S, (i // 2) * S))
    sheet = sheet.resize((1600, 1600), Image.LANCZOS)
    sheet.save(r"D:\pro\Cat_Tang\docs\images\ui\sheet_fx_explosion_flat.png")
    sheet.resize((800, 800)).save(r"C:\Users\asdzx\AppData\Local\Temp\claude\d4134924d10d6a35\c9ebd708-145d-4dae-aafc-cb0e7b6a1c44\scratchpad\fx_prev.png")
    print("ok")
