# 4장 바닥 텍스처 (시간의 신전 대리석 판석, 느와르 — 2026-11-19 사용자: 공장 철판 말고 신전): 옆 창 ChatGPT 그림(4×4 대리석 체커 + 은빛 문자판 · 별 상감, 이전 철판 원본은 ground_ch4_ironplate_gpt.png) → 게임 바닥
#   python Art/tools/ground_texture_ch4.py [원본] → Game/Assets/Art/Env/Ch4/ground_ch4.png (1024)
# - 원본은 Art/env_src/ch4/ground_ch4_gpt.png 에 보관
# - 바깥 테두리 줄눈을 안쪽 줄눈과 같은 굵기로 맞춰 반복해도 이음매가 두꺼워지지 않게: 바깥 반 줄눈만큼 잘라 4판 주기로 맞춤
import os
import sys
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'Art', 'env_src', 'ch4', 'ground_ch4_gpt.png')
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Env', 'Ch4')
os.makedirs(OUT, exist_ok=True)

im = Image.open(SRC).convert('RGB')
w, h = im.size
cut = round(w * 0.0025)               # 바깥 줄눈 절반쯤 (1254px 기준 3px)
im = im.crop((cut, cut, w - cut, h - cut)).resize((1024, 1024), Image.LANCZOS)
im.save(os.path.join(OUT, 'ground_ch4.png'))
print('ground_ch4.png', im.size)
