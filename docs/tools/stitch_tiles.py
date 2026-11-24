# 브라우저 화면 조각(줌 캡처)을 이어 원본 그림 하나로 (ChatGPT 생성 그림 받기용)
# 사용: python stitch_tiles.py <out.png> <원본 가로> <원본 세로> <뷰포트 가로 css> <뷰포트 세로 css> <조각 가로 px> <조각1> <조각2> ...
#   조각은 왼→오, 위→아래 순서. 각 조각은 원본을 (열 × 뷰포트 가로, 행 × 뷰포트 세로)만큼 옮겨 띄운 화면
import sys, math
from PIL import Image

out, W, H, vw, vh, tw = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
tiles = sys.argv[7:]
k = tw / vw
cols = math.ceil(W / vw)
canvas = Image.new("RGB", (round(W * k), round(H * k)), "white")
for i, t in enumerate(tiles):
    c, r = i % cols, i // cols
    im = Image.open(t).convert("RGB")
    if im.width != tw: im = im.resize((tw, round(tw * vh / vw)), Image.LANCZOS)   # 축소 캡처 조각
    canvas.paste(im, (round(c * vw * k), round(r * vh * k)))
canvas = canvas.crop((0, 0, round(W * k), round(H * k)))
canvas.save(out)
print(out, canvas.size)
