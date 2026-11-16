# 게임 이펙트 스프라이트 (툰 스타일: 굵은 잉크 외곽선 + 계단 색). 아이콘 · HUD와 같은 잉크색 · 금빛 팔레트.
# - fire_puff_0~2.png : 총구 폭발 불덩이 뭉치 (둥근 덩어리가 겹친 콜리플라워 모양, 가운데 흰빛 → 바깥 그을린 갈색)
# - smoke_0~1.png     : 연기 뭉치 (회색 뭉게구름, 부드러운 가장자리)
# - casing.png       : 탄피 (세로)
# 사용: fx_sprites.py [out_dir]   (기본: Game/Assets/Resources/FX)
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

INK = (18, 12, 26)
SS = 4                                   # 슈퍼샘플 배율 (그린 뒤 줄여서 계단 없이)


def mask_poly(size, pts):
    """정규화 좌표(0~1) 다각형 → L 마스크"""
    w, h = size
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).polygon([(x * w, y * h) for x, y in pts], fill=255)
    return m


def mask_ellipse(size, box):
    w, h = size
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).ellipse([box[0] * w, box[1] * h, box[2] * w, box[3] * h], fill=255)
    return m


def dilate(m, px):
    return m.filter(ImageFilter.MaxFilter(px * 2 + 1)) if px > 0 else m


def scale_pts(pts, origin, k):
    ox, oy = origin
    return [(ox + (x - ox) * k, oy + (y - oy) * k) for x, y in pts]


def layer(canvas, mask, color):
    """mask 자리에 color를 덮음"""
    solid = Image.new("RGBA", canvas.size, color + (255,))
    canvas.paste(solid, (0, 0), mask)


def smooth(m, r):
    """다각형 모서리를 둥글게 (흐린 뒤 다시 자름) → 각진 화살표가 아니라 불꽃 덩어리처럼"""
    return m.filter(ImageFilter.GaussianBlur(r)).point(lambda v: 255 if v > 127 else 0)


def blob_cluster(n, seed, count=(5, 8), spread=(0.12, 0.21), rad=(0.09, 0.15), core=0.19):
    """둥근 덩어리 여러 개를 겹친 뭉치. 반환: 높이장(덩어리 표면), 1등 · 2등 높이(이음선용), 덩어리 안쪽 마스크"""
    rng = np.random.default_rng(seed)
    blobs = [(0.5, 0.52, core)]
    for i in range(rng.integers(*count)):
        a = rng.uniform(0, 2 * np.pi)
        d = rng.uniform(*spread)
        r = rng.uniform(*rad)
        cx, cy = 0.5 + np.cos(a) * d, 0.52 + np.sin(a) * d * 0.85
        m = 0.08 + r                                     # 외곽선이 들어올 여백
        blobs.append((float(np.clip(cx, m, 1 - m)), float(np.clip(cy, m, 1 - m)), r))
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) / n
    h1 = np.zeros((n, n), np.float32)
    h2 = np.zeros((n, n), np.float32)
    for cx, cy, r in blobs:
        h = np.sqrt(np.clip(1 - ((xx - cx) ** 2 + (yy - cy) ** 2) / (r * r), 0, 1)) * r
        h2 = np.maximum(h2, np.minimum(h, h1))
        h1 = np.maximum(h1, h)
    return h1, h2, h1 > 0, xx, yy


def shade_of(h, n):
    """높이장 → 왼쪽 위 앞에서 오는 빛의 밝기"""
    gy, gx = np.gradient(h * n)
    nrm = np.dstack([-gx, -gy, np.ones_like(h) * 1.2])
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    L = np.array([-0.5, -0.6, 0.62]); L /= np.linalg.norm(L)
    return (nrm * L).sum(2)


def fire_puff(out, seed, S=128):
    """총구 폭발 불덩이 뭉치 (콜리플라워처럼 둥근 덩어리가 겹침).
    가운데가 가장 뜨겁고(흰빛) 바깥 덩어리로 갈수록 노랑 → 주황 → 그을린 갈색. 덩어리마다 입체 음영 + 덩어리 사이 이음선 + 잉크 외곽선"""
    n = S * SS
    h1, h2, inside, xx, yy = blob_cluster(n, seed)
    ndl = shade_of(h1, n)
    heat = 1 - np.clip(np.hypot(xx - 0.5, yy - 0.52) / 0.36, 0, 1)        # 가운데 1 → 가장자리 0
    heat = heat + (ndl - 0.5) * 0.25                                      # 빛 받는 면이 조금 더 뜨겁게
    rgb = np.zeros((n, n, 3), np.float32)
    rgb[:] = (128, 58, 32)                                                # 그을린 갈색
    rgb[heat > 0.22] = (226, 104, 36)                                     # 주황
    rgb[heat > 0.45] = (255, 168, 52)                                     # 밝은 주황
    rgb[heat > 0.66] = (255, 222, 104)                                    # 노랑
    rgb[heat > 0.84] = (255, 250, 222)                                    # 흰빛 심지
    shadow = ndl < 0.42
    rgb[shadow] *= np.array([0.72, 0.62, 0.68], np.float32)               # 그늘: 보랏빛으로 어둡게
    seam = inside & (h2 > 0) & ((h1 - h2) < 0.006)                        # 덩어리 사이 이음선
    seam_m = Image.fromarray(np.where(seam, 255, 0).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))
    body = Image.fromarray(np.where(inside, 255, 0).astype(np.uint8))
    burnt = (52, 22, 18)
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    layer(img, dilate(body, 3 * SS), burnt)
    img.paste(Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).convert("RGBA"), (0, 0), body)
    seam_col = Image.new("RGBA", (n, n), (90, 36, 22, 255))
    seam_a = seam_m.point(lambda v: int(v * 0.7))
    img.paste(seam_col, (0, 0), seam_a)
    img.resize((S, S), Image.LANCZOS).save(out)


def smoke(out, seed, S=128):
    """연기 뭉치: 회색 뭉게구름 (외곽선 없이 부드러운 가장자리, 왼쪽 위가 밝음)"""
    n = S * SS
    h1, h2, inside, xx, yy = blob_cluster(n, seed + 100, count=(6, 9), spread=(0.12, 0.22), rad=(0.10, 0.16), core=0.20)
    ndl = shade_of(h1, n)
    rgb = np.zeros((n, n, 3), np.float32)
    rgb[:] = (104, 98, 112)
    rgb[ndl > 0.35] = (150, 145, 156)
    rgb[ndl > 0.70] = (196, 192, 200)
    a = Image.fromarray(np.where(inside, 255, 0).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3 * SS))
    a = a.point(lambda v: int(v * 0.85))
    img = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).convert("RGBA")
    img.putalpha(a)
    img.resize((S, S), Image.LANCZOS).save(out)


def casing(out, W=32, H=64):
    size = (W * SS, H * SS)
    body = mask_poly(size, [(0.24, 0.16), (0.76, 0.16), (0.76, 0.84), (0.24, 0.84)])
    rim = mask_poly(size, [(0.18, 0.80), (0.82, 0.80), (0.82, 0.92), (0.18, 0.92)])
    mouth = mask_ellipse(size, (0.24, 0.10, 0.76, 0.22))
    hole = mask_ellipse(size, (0.32, 0.12, 0.68, 0.20))
    shine = mask_poly(size, [(0.32, 0.24), (0.42, 0.24), (0.42, 0.76), (0.32, 0.76)])
    shade = mask_poly(size, [(0.62, 0.20), (0.76, 0.20), (0.76, 0.84), (0.62, 0.84)])
    allm = Image.new("L", size, 0)
    for m in (body, rim, mouth):
        allm.paste(255, (0, 0), m)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    layer(img, dilate(allm, 3 * SS), INK)
    layer(img, body, (226, 168, 62))       # 놋쇠
    layer(img, mouth, (226, 168, 62))
    layer(img, shade, (176, 116, 42))
    layer(img, rim, (186, 124, 44))
    layer(img, shine, (255, 232, 160))
    layer(img, hole, INK)
    img.resize((W, H), Image.LANCZOS).save(out)


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    dst = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "..", "Game", "Assets", "Resources", "FX")
    os.makedirs(dst, exist_ok=True)
    for i in range(3):
        fire_puff(os.path.join(dst, f"fire_puff_{i}.png"), seed=11 + i * 7)
    for i in range(2):
        smoke(os.path.join(dst, f"smoke_{i}.png"), seed=5 + i * 13)
    casing(os.path.join(dst, "casing.png"))
    print("fx sprites ->", os.path.abspath(dst))
