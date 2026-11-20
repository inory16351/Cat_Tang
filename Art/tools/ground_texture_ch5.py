# 5장 바닥 텍스처 v2 (2026-11-19 사용자: 4장 체커 바닥 재활용 말고 깨지고 일그러진 느낌을 훨씬 강하게, 이음매는 불규칙 · 일그러진 모양)
#   python Art/tools/ground_texture_ch5.py [원본] → Game/Assets/Art/Env/Ch5/ground_ch5.png (2048)
# - 원본: Codex 그림 Art/env_src/ch5/ground_ch5_broken_codex.png (프롬프트 Art/tools/specs/ground_ch5_v2_prompt.txt) — 깨진 거울 판석 + 허공 틈 + 금 균열,
#   이미 상하좌우로 이어지는 그림이라 자르지 않고 그대로 (자르면 이음매가 생김)
# - 예전 4×4 보라 체커 원본(ChatGPT)은 Art/env_src/ch5/ground_ch5_gpt.png 에 남김
# - 게임에서는 셰이더 CatTang/RiftFloor가 이 그림을 일렁이게 일그러뜨리고 금 균열을 맥동시킴
import os
import sys
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Art", "env_src", "ch5", "ground_ch5_surreal_codex.png")   # v3 초현실 패치워크 (2026-11-20). v2 깨진 판석 = ground_ch5_broken_codex.png
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Env', 'Ch5')
os.makedirs(OUT, exist_ok=True)

im = Image.open(SRC).convert('RGB')
if im.size != (2048, 2048):
    im = im.resize((2048, 2048), Image.LANCZOS)
im.save(os.path.join(OUT, 'ground_ch5.png'))
print('ground_ch5.png', im.size)
