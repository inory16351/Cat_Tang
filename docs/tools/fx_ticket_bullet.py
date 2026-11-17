# 2장 태엽 차장 '구멍 난 승차권' 적 탄 스프라이트 (Resources/FX/bullets/ticket.png)
# 다른 적 탄처럼 머리가 오른쪽, 왼쪽으로 진홍 꼬리. 몸 = 연분홍 종이 + 진홍 테두리 + 펀치 구멍 2개 + 절취선
# 실행: python docs/tools/fx_ticket_bullet.py
import os
from PIL import Image, ImageDraw, ImageFilter

W, H, S = 150, 48, 4          # 최종 크기, 4배로 그려서 줄임 (부드러운 테두리)
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Game", "Assets", "Resources", "FX", "bullets", "ticket.png")

big = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
d = ImageDraw.Draw(big)

# 꼬리: 진홍 → 투명, 세 가닥
for i, (y, th) in enumerate([(0.5, 0.36), (0.3, 0.14), (0.7, 0.14)]):
    for x in range(0, int(W * S * 0.55)):
        k = x / (W * S * 0.55)
        a = int(200 * k ** 1.6)
        hh = th * H * S * (0.35 + 0.65 * k) / 2
        cy = y * H * S
        d.line([(x, cy - hh), (x, cy + hh)], fill=(255, 40 + 30 * i, 110, a))
big = big.filter(ImageFilter.GaussianBlur(S * 1.5))
d = ImageDraw.Draw(big)

# 승차권 몸 (살짝 기울인 사각형): 진홍 테두리 + 연분홍 종이
x0, x1, y0, y1 = 0.48 * W * S, 0.97 * W * S, 0.16 * H * S, 0.84 * H * S
d.rounded_rectangle([x0, y0, x1, y1], radius=3 * S, fill=(120, 10, 50, 255))
pad = 2.2 * S
d.rounded_rectangle([x0 + pad, y0 + pad, x1 - pad, y1 - pad], radius=2 * S, fill=(255, 226, 232, 255))
# 진홍 띠 (승차권 왼쪽 띠)
d.rectangle([x0 + pad, y0 + pad, x0 + pad + 0.07 * W * S, y1 - pad], fill=(230, 40, 90, 255))
# 절취선
cx = x0 + (x1 - x0) * 0.62
for yy in range(int(y0 + pad), int(y1 - pad), int(3 * S)):
    d.line([(cx, yy), (cx, yy + 1.5 * S)], fill=(150, 20, 70, 255), width=int(0.8 * S))
# 글자 대신 줄 두 개
for t in (0.4, 0.6):
    yy = y0 + (y1 - y0) * t
    d.line([(x0 + 0.12 * W * S, yy), (cx - 2 * S, yy)], fill=(170, 60, 110, 255), width=int(1.1 * S))
# 펀치 구멍 2개 (뚫린 자리 = 투명)
for t in (0.36, 0.66):
    r = 2.6 * S
    hx, hy = x0 + (x1 - x0) * 0.8, y0 + (y1 - y0) * t
    d.ellipse([hx - r - 0.8 * S, hy - r - 0.8 * S, hx + r + 0.8 * S, hy + r + 0.8 * S], fill=(120, 10, 50, 255))
    d.ellipse([hx - r, hy - r, hx + r, hy + r], fill=(0, 0, 0, 0))

img = big.resize((W, H), Image.LANCZOS)
img.save(OUT)
print("saved", os.path.abspath(OUT), img.size)
