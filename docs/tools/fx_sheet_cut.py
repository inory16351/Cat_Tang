# Codex/ChatGPT 이펙트 시트(마젠타 바탕, 가로 N칸) → 투명 PNG 프레임
# 실행: python docs/tools/fx_sheet_cut.py <시트.png> <이름> <칸 수> [--pivot left|center] [--strict (빨강 · 진홍 이펙트)]
#  → Game/Assets/Resources/FX/anim/<이름>_<i>.png (정사각, 모든 프레임 같은 크기 · 같은 기준점)
#  pivot left = 총구가 왼쪽 가운데 (프레임 왼쪽 끝을 기준으로 맞춤), center = 가운데
import os, sys
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'FX', 'anim')


def key_magenta(a):
    rgb = a[..., :3].astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    # 마젠타 정도: R · B 높고 G 낮음
    mag = np.clip(((r + b) / 2 - g - 60) / 90, 0, 1)
    alpha = 1 - mag
    # 경계 마젠타 번짐 제거: 남은 픽셀의 분홍기를 줄임
    spill = np.clip(np.minimum(r, b) - g, 0, None) * (1 - alpha) * 0.9
    rgb[..., 0] -= spill
    rgb[..., 2] -= spill
    out = np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8)
    out[alpha < 0.08] = 0
    return out


def key_magenta_strict(a):
    """빨강 · 진홍 이펙트용 (2026-11-18 처형 시트): 보통 키는 R · B가 높은 진홍도 반쯤 지워서, B까지 높을 때(진짜 마젠타)만 지움"""
    rgb = a[..., :3].astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mag = np.clip((np.minimum(r, b) - g - 100) / 60, 0, 1)
    alpha = 1 - mag
    spill = np.clip(b - np.maximum(g, r * 0.35), 0, None) * (1 - alpha) * 0.9      # 가장자리 남은 보랏빛만 덜어냄
    rgb[..., 2] -= spill
    out = np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8)
    out[alpha < 0.08] = 0
    return out


def main():
    src, name, n = sys.argv[1], sys.argv[2], int(sys.argv[3])
    pivot = sys.argv[sys.argv.index('--pivot') + 1] if '--pivot' in sys.argv else 'center'
    im = Image.open(src).convert('RGBA')
    W, H = im.size
    cw = W / n
    frames = []
    for i in range(n):
        cell = im.crop((round(i * cw), 0, round((i + 1) * cw), H))
        frames.append(Image.fromarray((key_magenta_strict if '--strict' in sys.argv else key_magenta)(np.array(cell))))
    # 모든 프레임 공통 크기: 가장 큰 내용 상자
    boxes = [f.getbbox() for f in frames]
    cw_i, ch_i = frames[0].size
    if pivot == 'left':
        x0 = min(b[0] for b in boxes if b)
        x1 = max(b[2] for b in boxes if b)
    else:
        x0, x1 = 0, cw_i
    side = max(x1 - x0, ch_i)
    os.makedirs(OUT, exist_ok=True)
    for i, f in enumerate(frames):
        canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        crop = f.crop((x0, 0, x1, ch_i))
        if pivot == 'left':
            canvas.paste(crop, (0, (side - ch_i) // 2))           # 총구 = 왼쪽 가운데
        else:
            canvas.paste(crop, ((side - (x1 - x0)) // 2, (side - ch_i) // 2))
        canvas = canvas.resize((256, 256), Image.LANCZOS)
        p = os.path.join(OUT, f'{name}_{i}.png')
        canvas.save(p)
    print('saved', n, 'frames', name, 'side', side)


if __name__ == '__main__':
    main()
