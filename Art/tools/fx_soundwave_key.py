# 종지기 음파 띠 그림: Codex 그림(docs/images/fx_src/c4_soundwave.png, 마젠타 바탕) → 투명 PNG (Resources/Kit/Ch4/FX_SoundWave.png)
#   python Art/tools/fx_soundwave_key.py
# - 마젠타와 가까운 색일수록 투명 (가장자리 부드럽게) + 남은 분홍기 빼기
# - 위아래 빈 여백을 잘라 띠가 V 0~1을 꽉 채우게 (그림 위 = 고리 바깥, 아래 = 안쪽)
import os
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
src = os.path.join(ROOT, 'docs', 'images', 'fx_src', 'c4_soundwave.png')
dst = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'Kit', 'Ch4', 'FX_SoundWave.png')
a = np.asarray(Image.open(src).convert('RGB')).astype(np.float32) / 255.0
r, g, b = a[..., 0], a[..., 1], a[..., 2]
mag = np.clip((np.minimum(r, b) - g) * 1.6, 0, 1)            # 마젠타 정도 (빨강 · 파랑은 크고 초록이 작음)
alpha = 1.0 - np.clip((mag - 0.35) / 0.4, 0, 1)
# 분홍기 빼기: 마젠타가 섞인 만큼 파랑을 줄임
b2 = np.clip(b - mag * 0.9, 0, 1)
out = np.dstack([r, g, b2, alpha])
ys = np.where(alpha.max(axis=1) > 0.05)[0]
top, bot = max(0, ys.min() - 6), min(a.shape[0], ys.max() + 7)
out = out[top:bot]
Image.fromarray((out * 255).astype(np.uint8), 'RGBA').resize((2048, 256), Image.LANCZOS).save(dst)
print('FX_SoundWave.png', top, bot)
