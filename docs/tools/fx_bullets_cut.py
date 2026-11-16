# 적 탄 스프라이트 시트(가로 N칸, 마젠타 #FF00FF 바탕) → 칸별 투명 PNG
# 탄이 보라 · 분홍 계열이라 '마젠타 정도'로 빼면 몸통까지 빠짐 → 정확한 #FF00FF와의 색 거리로만 투명 처리.
# 실행: python docs/tools/fx_bullets_cut.py <시트.png> <칸 수> <이름1,이름2,...>
#   → Game/Assets/Resources/FX/bullets/<이름>.png (내용 상자로 잘라 여백 6px, 진행 방향 = +X)
import os
import sys
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'FX', 'bullets')


def key_exact_magenta(a):
    rgb = a[..., :3].astype(np.float32)
    d = np.sqrt(((rgb - np.array([255, 0, 255], np.float32)) ** 2).sum(axis=2))
    alpha = np.clip((d - 35) / 70, 0, 1)
    # 반투명 가장자리: 마젠타가 섞인 만큼 빼서 원래 색을 되살림 (c = a*src + (1-a)*mag)
    a3 = np.maximum(alpha, 1e-3)[..., None]
    src = (rgb - (1 - alpha)[..., None] * np.array([255, 0, 255], np.float32)) / a3
    out = np.dstack([np.clip(src, 0, 255), alpha * 255]).astype(np.uint8)
    out[alpha < 0.05] = 0
    return out


def main():
    src, n, names = sys.argv[1], int(sys.argv[2]), sys.argv[3].split(',')
    im = np.array(Image.open(src).convert('RGBA'))
    H, W = im.shape[:2]
    os.makedirs(OUT, exist_ok=True)
    for i, name in enumerate(names[:n]):
        cell = key_exact_magenta(im[:, round(i * W / n):round((i + 1) * W / n)])
        ys, xs = np.nonzero(cell[..., 3] > 10)
        if len(xs) == 0:
            continue
        pad = 6
        y0, y1 = max(0, ys.min() - pad), min(cell.shape[0], ys.max() + pad + 1)
        x0, x1 = max(0, xs.min() - pad), min(cell.shape[1], xs.max() + pad + 1)
        # 세로는 가운데 대칭으로 (회전 중심 = 탄 중심선)
        cy = cell.shape[0] // 2
        half = max(cy - y0, y1 - cy)
        y0, y1 = max(0, cy - half), min(cell.shape[0], cy + half)
        out = Image.fromarray(cell[y0:y1, x0:x1])
        p = os.path.join(OUT, name + '.png')
        out.save(p)
        print(os.path.normpath(p), out.size)


if __name__ == '__main__':
    main()
