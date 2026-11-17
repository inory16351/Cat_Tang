# 시트에서 잘라 낸 캐릭터 이미지 정리: 배경(가장자리 색)과 다른 덩어리 중 가장 큰 것(주인공)만 남기고
# 옆 캐릭터가 끼어든 조각은 배경색으로 지움. 이어서 여백을 조금 두고 잘라 냄
# 사용: python Art/tools/keep_main.py <입력.png> <출력.png>
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

src, dst = sys.argv[1], sys.argv[2]
im = np.asarray(Image.open(src).convert("RGB")).astype(np.int16)
edge = np.concatenate([im[0], im[-1], im[:, 0], im[:, -1]])
bg = np.median(edge, axis=0)
diff = np.abs(im - bg).sum(axis=2)
fg = diff > 40
fg = ndimage.binary_closing(fg, iterations=3)
lab, n = ndimage.label(fg)
if n == 0:
    raise SystemExit("no foreground")
sizes = ndimage.sum(fg, lab, range(1, n + 1))
main = 1 + int(np.argmax(sizes))
keep = lab == main
# 주인공에 붙은 작은 덩어리(떠 있는 소품 등)는 주인공 경계 상자 안이면 남김
ys, xs = np.where(keep)
y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
for k in range(1, n + 1):
    if k == main:
        continue
    yy, xx = np.where(lab == k)
    if yy.min() >= y0 and yy.max() <= y1 and xx.min() >= x0 and xx.max() <= x1:
        keep |= lab == k
keep = ndimage.binary_dilation(keep, iterations=2)
out = im.copy()
out[~keep] = bg
pad = 24
H, W = keep.shape
crop = out[max(0, y0 - pad):min(H, y1 + pad), max(0, x0 - pad):min(W, x1 + pad)]
Image.fromarray(crop.astype(np.uint8)).save(dst)
print(dst, crop.shape[1], crop.shape[0], "parts", n)
