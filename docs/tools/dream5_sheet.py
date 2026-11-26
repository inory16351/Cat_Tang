# 꿈3 '11:59의 기원' (dream5) 미리보기 콘택트 시트: v2 재생 순서대로 한 줄씩 → docs/images/dream5/contact_sheet_v2.png
# 실행: python docs/tools/dream5_sheet.py [출력 경로] [칸 가로 px]
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = r"C:/Project/Cat_Tang"
D = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream5")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "docs/images/dream5/contact_sheet_v2.png")
TW = int(sys.argv[2]) if len(sys.argv) > 2 else 400
ROWS = [   # v2 (2026-11-26, 약 30초) — 재생 순서 · 판(크림 / _neg 음화 / _night 밤 회색 / 빨강 바탕)
    ("A 0~3.6 도입 · 크림", ["rain_a", "rain_b", "rain_c"]),
    ("B1 3.6~ 실 · 진자 그림자", ["hands_b", "hands_c_neg", "hands_c", "pshadow_a", "pshadow_b", "pshadow_c_neg"]),
    ("B2 바닥 그림자 · 열쇠 · 거울", ["floor_a", "floor_b_neg", "floor_c", "rain_c_neg", "key_hand", "veil_c_neg"]),
    ("B3 ~14.0 톱니 · 분할 · 분침", ["gear_ecu", "split_hands", "split_hands_neg", "minute_b_neg", "cat_a_neg", "minute_c"]),
    ("C 14.0 11:59 빨강 바탕", ["red_1159", "red_1159_b"]),
    ("D1 14.95~ 창빛 계절 · 밤 회색", ["light_a_night", "light_b_night", "light_c_night", "light_d_night", "light_e_night", "light_f_night"]),
    ("D2 ~21.15 빈 의자 · 병상", ["light_g_night", "back_chair_night", "back_chair_j_night", "bed_a_night", "bed_b_night"]),
    ("E 21.15~29.7 고양이 · 극접사", ["cat_a", "cat_b", "cat_c", "cat_c_j", "ecu_a", "ecu_b"]),
]
TH = round(TW * 816 / 1450)
LW, PAD = 280, 6
cols = max(len(r[1]) for r in ROWS)
sheet = Image.new("RGB", (LW + cols * (TW + PAD), len(ROWS) * (TH + PAD) + PAD), (24, 22, 24))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 17)
except OSError:
    font = ImageFont.load_default()
for r, (title, names) in enumerate(ROWS):
    y = PAD + r * (TH + PAD)
    d.text((8, y + 6), title, fill=(235, 230, 220), font=font)
    for c, n in enumerate(names):
        x = LW + c * (TW + PAD)
        p = os.path.join(D, n + ".jpg")
        if os.path.exists(p):
            sheet.paste(Image.open(p).convert("RGB").resize((TW, TH), Image.LANCZOS), (x, y))
        else:
            d.rectangle((x, y, x + TW, y + TH), outline=(120, 40, 40))
            d.text((x + 8, y + 8), "없음: " + n, fill=(200, 90, 90), font=font)
        d.text((x + 6, y + TH - 22), n, fill=(255, 60, 60), font=font)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
sheet.save(OUT)
print(OUT, sheet.size)
