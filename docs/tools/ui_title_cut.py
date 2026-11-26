# 로비 11:59 제목 로고 (Codex CLI, 마젠타 바탕) → Game/Assets/Resources/UI/Lobby/title.png
# 원본: docs/images/ui_src/title_v2/title_{a,b,c}.png (2026-11-26 사용자: 엠블럼형 로고로 다시, Codex CLI)
# 실행: python docs/tools/ui_title_cut.py <a|b|c> [--preview]
#   --preview: 로비 배경 위에 얹은 확인 그림만 docs/images/ui_src/title_v2/preview_<v>.png로 (title.png는 안 바꿈)
import os
import sys
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
SRC_DIR = os.path.join(ROOT, 'docs', 'images', 'ui_src', 'title_v2')
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'UI', 'Lobby', 'title.png')
KEY = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'UI', 'Lobby', 'key.png')
SHADOW = (22, 8)   # 미리보기 그림자 (화면 px, 아래가 +). LobbyScreen 제목 DropShadow(new Vector2(22, -8))와 맞춤


def lobby_vignette(size):
    """LobbyScreen.BuildVignette와 같은 식 (192 × 108 → 확대)"""
    W, H = 192, 108
    x = np.linspace(-1, 1, W)[None, :]
    y = np.linspace(-1, 1, H)[:, None]
    d = np.sqrt(x * x * 0.75 + y * y)
    def edge(e0, e1, t):
        k = np.clip((t - e0) / (e1 - e0), 0, 1)
        return k * k * (3 - 2 * k)
    a = edge(0.62, 1.45, d) * 0.78
    a = np.maximum(a, edge(0.2, 1.0, x) * 0.32 + 0 * y)
    a = np.maximum(a, edge(-0.6, -1.0, y) * 0.4 + 0 * x)
    a = a[::-1]                                              # 텍스처 y는 아래부터
    img = np.zeros((H, W, 4), np.uint8)
    img[..., 0], img[..., 1], img[..., 2] = 10, 3, 9
    img[..., 3] = (np.clip(a, 0, 1) * 255).astype(np.uint8)
    return Image.fromarray(img, 'RGBA').resize(size, Image.BILINEAR)


def key_magenta(im):
    a = np.asarray(im.convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = np.minimum(r, b) - g                           # 마젠타가 얼마나 튀는지
    alpha = np.clip(1.0 - (mag - 60) / 90.0, 0, 1)       # 60 아래 = 불투명, 150 위 = 투명
    # 반투명 가장자리의 마젠타 번짐 빼기: 빨강 · 파랑을 초록 쪽으로 당김
    edge = alpha < 1
    k = np.where(edge, 1 - alpha, 0)
    a[..., 0] = np.where(edge, r - (r - g) * k * 0.85, r)
    a[..., 2] = np.where(edge, b - (b - g) * k * 0.85, b)
    out = np.dstack([np.clip(a, 0, 255), alpha * 255]).astype(np.uint8)
    o = Image.fromarray(out, 'RGBA')
    # 외딴 작은 얼룩 정리: 알파가 아주 낮은 건 0으로
    arr = np.asarray(o).copy()
    arr[..., 3][arr[..., 3] < 12] = 0
    return Image.fromarray(arr, 'RGBA')


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def grade(im):
    """로비 최종 그림(key.png)에 맞춤 (2026-11-26 사용자: 로고의 비네트 · 그림자를 화면에 맞춰)
    - 해는 로고 왼쪽 위(시계 뒤) → 왼쪽 가장자리에 주황 역광, 오른쪽 · 아래로 갈수록 자두색 검정 비네트
    - 어두운 면(외곽선 · 돌출 그림자)은 그림의 그늘색(짙은 자두 · 적갈)으로
    - 전체는 붉은 노을빛을 살짝 곱함"""
    a = np.asarray(im).astype(np.float32) / 255.0
    rgb, al = a[..., :3], a[..., 3]
    h, w = al.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = xx / w, yy / h
    lum = rgb @ np.array([0.299, 0.587, 0.114], np.float32)

    # 그늘색: 어두운 면을 자두 · 적갈 쪽으로
    shade = np.array([0.16, 0.035, 0.07], np.float32)
    s = smooth(0.42, 0.06, lum)[..., None]
    rgb = rgb * (1 - s * 0.55) + shade * s * 0.55

    # 노을빛 곱하기 (key의 붉은 주변광)
    rgb = rgb * np.array([1.0, 0.9, 0.84], np.float32)

    # 비네트: 해(왼쪽 위) 반대쪽인 오른쪽 · 아래로 자두색 검정
    d = np.sqrt(((u - 0.18) * 1.0) ** 2 + ((v - 0.25) * 0.9) ** 2)
    vig = (smooth(0.35, 1.05, d) * 0.5)[..., None]
    plum = np.array([0.04, 0.012, 0.03], np.float32)
    rgb = rgb * (1 - vig) + plum * vig

    # 역광: 왼쪽(해 쪽)을 향한 가장자리 안쪽에 주황 테 (모양의 왼쪽 경계 = 왼쪽 몇 px가 비어 있는 곳)
    pil_a = Image.fromarray((al * 255).astype(np.uint8))
    from PIL import ImageChops, ImageFilter
    shifted = ImageChops.offset(pil_a, 9, 6)                  # 오른쪽 아래로 민 알파 = 왼쪽 위 경계가 빈 곳
    rim = np.clip(al - np.asarray(shifted).astype(np.float32) / 255.0, 0, 1)
    rim = np.asarray(Image.fromarray((rim * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2.2))).astype(np.float32) / 255.0
    rim = rim * al * (1 - smooth(0.25, 0.9, u))              # 해에 가까운 왼쪽일수록 강하게
    glow = np.array([1.0, 0.55, 0.2], np.float32)
    rgb = rgb + glow * rim[..., None] * 0.75
    out = np.dstack([np.clip(rgb, 0, 1), al])
    return Image.fromarray((out * 255).astype(np.uint8), 'RGBA')


def main():
    v = sys.argv[1] if len(sys.argv) > 1 else 'a'
    src = Image.open(os.path.join(SRC_DIR, f'title_{v}.png'))
    im = key_magenta(src)
    im = im.crop(im.getbbox())
    pad = 12
    canvas = Image.new('RGBA', (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    canvas.alpha_composite(im, (pad, pad))
    im = canvas
    if im.width > 1400:
        im = im.resize((1400, round(im.height * 1400 / im.width)), Image.LANCZOS)
    if '--raw' not in sys.argv:
        im = grade(im)
    if '--preview' in sys.argv:
        bg = Image.open(KEY).convert('RGBA')
        bg.alpha_composite(lobby_vignette(bg.size))
        # LobbyScreen: 제목 = 위쪽 가운데 기준 (470, −250), 700 × 400 상자에 비율 유지 · 그림자 = 해 반대쪽(오른쪽 아래)
        box_w, box_h = 700, 450
        s = min(box_w / im.width, box_h / im.height)
        t = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
        cx, cy = 960 + 470, 250
        x0, y0 = cx - t.width // 2, cy - t.height // 2
        sa = np.asarray(t.split()[3]).astype(np.float32)
        for k, (ox, oy, aa) in enumerate([(SHADOW[0] * 1.7, SHADOW[1] * 1.7, 0.15), (SHADOW[0] * 1.35, SHADOW[1] * 1.35, 0.3), (SHADOW[0], SHADOW[1], 0.6)]):
            sh = np.zeros((t.height, t.width, 4), np.uint8)
            sh[..., 0], sh[..., 1], sh[..., 2] = 8, 0, 5
            sh[..., 3] = (sa * 0.75 * aa).astype(np.uint8)
            bg.alpha_composite(Image.fromarray(sh, 'RGBA'), (x0 + int(ox), y0 + int(oy)))
        bg.alpha_composite(t, (x0, y0))
        bg.convert('RGB').save(os.path.join(SRC_DIR, f'preview_{v}.png'))
        im.save(os.path.join(SRC_DIR, f'cut_{v}.png'))
        print('preview', v, im.size)
        return
    im.save(OUT)
    print('saved', OUT, im.size)


if __name__ == '__main__':
    main()
