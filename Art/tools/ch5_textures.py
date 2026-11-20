# 5장 텍스처 변형 (2026-11-19, Tripo 크레딧 부족 → 강화형 · 중간 보스를 기존 모델 + 텍스처 변형 + 블렌더 파츠로)
#   python Art/tools/ch5_textures.py plus <원본 basecolor> <출력>    강화형: 더 어둡고 보랏빛 + 금빛 균열이 번진 텍스처
#   python Art/tools/ch5_textures.py mirror <원본 basecolor> <출력>  중간 보스 거울 고양이: 은청 거울빛 + 마젠타 눈 + 금 균열
#   python Art/tools/ch5_textures.py plusfbx <Ch5 폴더> <이름>       Ch5_<이름>.fbx → Ch5_<이름>_Plus.fbx 복사 + 강화 텍스처
# 균열은 UV 공간에 그리는 번개 모양 선(금 + 짙은 보라 테두리)이라 몸 여기저기 조각마다 금이 간 것처럼 보임
import os
import random
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

GOLD = (255, 214, 96)
EDGE = (60, 30, 90)


def cracks(im, count, seed, width):
    """번개처럼 꺾이며 가지 치는 금 균열을 그림 (금빛 심 + 어두운 테두리 + 옅은 빛 번짐)"""
    rnd = random.Random(seed)
    w, h = im.size
    lines = []
    for _ in range(count):
        x, y = rnd.uniform(0, w), rnd.uniform(0, h)
        ang = rnd.uniform(0, 6.283)
        pts = [(x, y)]
        for _ in range(rnd.randint(4, 9)):
            ang += rnd.uniform(-0.9, 0.9)
            step = rnd.uniform(0.012, 0.035) * w
            x += np.cos(ang) * step
            y += np.sin(ang) * step
            pts.append((x, y))
            if rnd.random() < 0.3:                                   # 가지
                bx, by, ba = x, y, ang + rnd.choice((-1, 1)) * rnd.uniform(0.6, 1.2)
                br = [(bx, by)]
                for _ in range(rnd.randint(2, 4)):
                    ba += rnd.uniform(-0.6, 0.6)
                    s = rnd.uniform(0.008, 0.02) * w
                    bx += np.cos(ba) * s
                    by += np.sin(ba) * s
                    br.append((bx, by))
                lines.append((br, max(1, width // 2)))
        lines.append((pts, width))
    glow = Image.new('RGB', im.size, (0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for pts, wd in lines:
        gd.line(pts, fill=(200, 140, 40), width=wd * 4, joint='curve')
    glow = glow.filter(ImageFilter.GaussianBlur(width * 1.5))
    out = Image.fromarray(np.clip(np.asarray(im, np.float32) + np.asarray(glow, np.float32) * 0.55, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(out)
    for pts, wd in lines:
        d.line(pts, fill=EDGE, width=wd + 2, joint='curve')
    for pts, wd in lines:
        d.line(pts, fill=GOLD, width=wd, joint='curve')
    return out


def plus(src, dst):
    im = Image.open(src).convert('RGB')
    a = np.asarray(im, np.float32) / 255.0
    a = a * 0.82                                                     # 조금 어둡게
    violet = np.array([0.42, 0.26, 0.62], np.float32)
    lum = a.mean(axis=2, keepdims=True)
    a = a * 0.8 + violet * lum * 0.35                                # 보랏빛으로 물듦
    im = Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8))
    im = cracks(im, 70, 51, max(3, im.size[0] // 450))
    im.save(dst, quality=92)
    print('plus', dst, im.size)


def mirror(src, dst):
    im = Image.open(src).convert('RGB')
    a = np.asarray(im, np.float32) / 255.0
    hsv = np.asarray(im.convert('HSV'), np.float32) / 255.0
    lum = (a * np.array([0.3, 0.55, 0.15], np.float32)).sum(axis=2, keepdims=True)
    lum = np.clip((lum - 0.08) / 0.8, 0, 1) ** 0.8
    dark = np.array([0.32, 0.34, 0.58], np.float32)
    light = np.array([0.95, 0.97, 1.0], np.float32)
    out = dark + (light - dark) * lum                                # 은청 거울빛 (명암만 남김)
    # 노란 눈 → 마젠타 (채도 높은 노랑 ~ 주황)
    hue, sat, val = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    eye = (hue > 0.1) & (hue < 0.19) & (sat > 0.55) & (val > 0.6)
    out[eye] = np.array([1.0, 0.25, 0.75], np.float32)
    # 빨간 스카프 · 띠 → 연보라 (거울 속에선 색이 바램)
    red = ((hue < 0.04) | (hue > 0.95)) & (sat > 0.5) & (val > 0.3)
    out[red] = out[red] * 0.5 + np.array([0.7, 0.55, 0.95], np.float32) * 0.5
    im = Image.fromarray(np.clip(out * 255, 0, 255).astype(np.uint8))
    im = cracks(im, 45, 77, max(3, im.size[0] // 500))
    im.save(dst)
    print('mirror', dst, im.size)


def plusfbx(folder, name):
    base = os.path.join(folder, f'Ch5_{name}')
    shutil.copy(base + '.fbx', base + '_Plus.fbx')
    plus(base + '_basecolor.jpg', base + '_Plus_basecolor.jpg')


if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'plus':
        plus(sys.argv[2], sys.argv[3])
    elif mode == 'mirror':
        mirror(sys.argv[2], sys.argv[3])
    elif mode == 'plusfbx':
        plusfbx(sys.argv[2], sys.argv[3])
