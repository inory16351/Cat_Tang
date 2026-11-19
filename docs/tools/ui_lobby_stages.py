# 로비 '그림을 그리는 과정' 단계 그림 (2026-11-18 사용자: 선이 균일하게 그어지면 그림 같지 않음 → 그리는 과정을 이미지 몇 장으로 뽑아 겹치기)
#  Resources/UI/Lobby/ 에 저장. 모두 key.png(1920x1080)와 같은 자리
#   stage1.png  파란 연필 밑그림 (Codex, docs/images/ui_src/stage1_construction.png → 선만 남긴 RGBA)
#   stage2.png  연필 스케치     (Codex, stage2_pencil.png → 선만 남긴 RGBA)
#   stage3.png  잉크 외곽선만   (key에서 계산: 진하고 가는 윤곽선)
#   stage4.png  잉크 + 해칭     (= key_ink.png, ui_lobby_ink.py)
#   stage5.png  평면 색칠       (key에서 계산: 부드럽게 뭉갠 뒤 색 수를 줄임 + 잉크 선)
#   reveal_radial.png  밑그림 · 스케치 · 외곽선이 얼굴에서 퍼져 나가는 순서 (+ 붓 결)
# 실행: python docs/tools/ui_lobby_stages.py docs/images/ui_src   (ui_lobby_cut.py · ui_lobby_ink.py 다음에)
import os, sys
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

sys.path.insert(0, os.path.dirname(__file__))
from ui_lobby_cut import crop169  # noqa: E402
from ui_lobby_ink import fractal, FACE  # noqa: E402

D = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI', 'Lobby')
W, H = 1920, 1080


def sketch_layer(path, tint=None, gain=1.6):
    """종이 위 스케치 → 선만 (알파 = 종이보다 어두운 정도). tint가 있으면 그 색으로"""
    im = crop169(Image.open(path).convert('RGB')).resize((W, H), Image.LANCZOS)
    a = np.array(im).astype(np.float32) / 255
    lum = a.mean(-1)
    paper = np.percentile(lum, 92)                       # 종이 밝기
    alpha = np.clip((paper - lum) / max(0.05, paper) * gain, 0, 1)
    alpha[alpha < 0.06] = 0
    rgb = a if tint is None else np.broadcast_to(np.array(tint, np.float32), a.shape)
    if tint is None:                                     # 연필 · 파란 선 색은 살리되 조금 진하게
        rgb = np.clip(a * 0.75, 0, 1)
    out = np.dstack([rgb, alpha])
    return Image.fromarray((out * 255).astype(np.uint8), 'RGBA')


def save(im, name):
    im.save(os.path.join(D, name)); print(name, im.size)


if __name__ == '__main__':
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), '..', 'images', 'ui_src')
    key = Image.open(os.path.join(D, 'key.png')).convert('RGB')
    k = np.array(key).astype(np.float32) / 255
    lum = k.mean(-1) * 0.0 + (k[..., 0] * 0.3 + k[..., 1] * 0.59 + k[..., 2] * 0.11)

    # 1 · 2: Codex 스케치
    for i, (fn, gain) in enumerate([('stage1_construction.png', 1.3), ('stage2_pencil.png', 1.7)], 1):
        p = os.path.join(src, fn)
        if os.path.exists(p):
            save(sketch_layer(p, gain=gain), f'stage{i}.png')

    # 3: 잉크 선화 — Codex 선화(stage3_lineart.png)가 있으면 그것, 없으면 key에서 계산한 외곽선
    la = os.path.join(src, 'stage3_lineart.png')
    if os.path.exists(la):
        save(sketch_layer(la, tint=(22 / 255, 12 / 255, 18 / 255), gain=2.2), 'stage3.png')
    # (계산 외곽선은 선화가 없을 때만 씀)
    dark = lum < 0.13
    it = 6
    thin = dark & ~ndimage.binary_dilation(ndimage.binary_erosion(dark, iterations=it), iterations=4)
    around = ndimage.uniform_filter(lum, 41)
    thin &= around > 0.2                                      # 어두운 하늘 · 그늘 덩어리 속 얼룩은 빼고 밝은 곳의 선만
    lab, n = ndimage.label(thin, structure=np.ones((3, 3)))
    sizes = ndimage.sum(thin, lab, range(1, n + 1))
    keep = np.zeros(n + 1, bool); keep[1:] = sizes >= 120
    thin = keep[lab]
    a3 = np.array(Image.fromarray((thin * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))).astype(np.float32) / 255
    rgba = np.zeros((H, W, 4), np.uint8); rgba[..., 0], rgba[..., 1], rgba[..., 2] = 22, 12, 18
    rgba[..., 3] = (np.clip(a3 * 1.4, 0, 1) * 255).astype(np.uint8)
    if not os.path.exists(la):
        save(Image.fromarray(rgba, 'RGBA'), 'stage3.png')

    # 4: 잉크 + 해칭 = key_ink
    ink = Image.open(os.path.join(D, 'key_ink.png'))
    save(ink, 'stage4.png')

    # 5: 평면 색칠 — 뭉갠 뒤 색 수를 줄이고 잉크 선을 얹음
    flat = key.filter(ImageFilter.MedianFilter(11)).filter(ImageFilter.MedianFilter(9))
    hsv = np.array(flat.convert('HSV')).astype(np.float32)
    hsv[..., 2] = np.clip(110 + hsv[..., 2] * 0.55, 0, 255)                # 명암을 눌러 평면 칠처럼 (그늘 · 빛은 마지막 단계에서)
    hsv[..., 1] = np.clip(hsv[..., 1] * 0.95, 0, 255)
    flat = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB')
    flat = flat.quantize(colors=16, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert('RGB')
    f = np.array(flat).astype(np.float32) / 255
    ia = np.array(ink).astype(np.float32)[..., 3:4] / 255 * 0.85
    f = f * (1 - ia) + np.array([22, 12, 18], np.float32) / 255 * ia
    save(Image.fromarray((f * 255).astype(np.uint8)), 'stage5.png')

    # 얼굴에서 퍼지는 순서 (+ 붓 결)
    mw, mh = W // 2, H // 2
    xs = np.linspace(0, 1, mw)[None, :].repeat(mh, 0)
    ys = np.linspace(0, 1, mh)[:, None].repeat(mw, 1)
    d = np.hypot((xs - FACE[0]) * 1.7, ys - FACE[1])
    d = d / d.max()
    m = d * 0.75 + fractal(mw, mh, 33, 6, stretch=(0.5, 1.5)) * 0.25
    m = (m - m.min()) / (m.max() - m.min())
    Image.fromarray((m * 255).astype(np.uint8), 'L').save(os.path.join(D, 'reveal_radial.png')); print('reveal_radial.png')
