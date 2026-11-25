# slice_icons 결과(마젠타 바탕에서 뺀 RGBA 아이콘)를 기존 게임 아이콘 가장자리에 맞춘다
# 1) 가장자리 띠에 남은 마젠타 번짐을 잉크색으로 (안쪽 보라색 연기 · 리본은 건드리지 않음)
# 2) 바깥에 잉크 외곽선을 몇 px 더 둘러 기존 아이콘(외곽 검은 띠 7~8px)과 두께를 맞춤
# 사용: icon_despill.py <in.png|in_dir> <out.png|out_dir> [ring_px=2]
import sys, os, glob
import numpy as np
from PIL import Image
from scipy import ndimage as nd

INK = np.array([18, 12, 26], np.float32)


def despill(img, ring=2):
    a = np.asarray(img.convert("RGBA"), np.float32)
    rgb, al = a[..., :3], a[..., 3] / 255
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    # 그림 속에 갇힌 작은 마젠타 구멍(사슬 고리 안 · 불똥 사이)은 bg_alpha가 못 빼므로 여기서 투명으로
    pocket = (r > 170) & (b > 170) & (g < 90) & (np.abs(r - b) < 70)
    pocket = nd.binary_dilation(pocket, iterations=1) & (np.minimum(r, b) - g > 60)
    al = np.where(pocket, 0.0, al)
    solid = al > 0.5
    # 가장자리 띠: 투명한 곳에서 3px 안
    band = nd.binary_dilation(~solid, iterations=3) & (al > 0)
    mag = (np.minimum(r, b) - g - 25) / 70                 # 마젠타스러움 0~1
    mag = np.clip(mag, 0, 1) * (np.abs(r - b) < 90)
    k = np.where(band, np.maximum(mag, (al < 0.35).astype(np.float32)), 0)[..., None]
    rgb = rgb * (1 - k) + INK * k
    # 바깥 잉크 고리
    if ring:
        grown = nd.binary_dilation(solid, iterations=ring)
        ra = nd.gaussian_filter(grown.astype(np.float32), 0.7)
        out_a = np.maximum(al, ra)
        w = np.clip((out_a - al) / np.maximum(out_a, 1e-4), 0, 1)[..., None]   # 새로 덮인 만큼 잉크
        rgb = rgb * (1 - w) + INK * w
        al = out_a
    res = np.dstack([np.clip(rgb, 0, 255), np.clip(al, 0, 1) * 255]).astype(np.uint8)
    return Image.fromarray(res, "RGBA")


if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    ring = int(sys.argv[3]) if len(sys.argv) > 3 else 2
    if os.path.isdir(src):
        os.makedirs(dst, exist_ok=True)
        fs = sorted(glob.glob(os.path.join(src, "*.png")))
        for f in fs:
            despill(Image.open(f), ring).save(os.path.join(dst, os.path.basename(f)))
        print("despill", len(fs), "->", dst)
    else:
        despill(Image.open(src), ring).save(dst)
        print(dst)
